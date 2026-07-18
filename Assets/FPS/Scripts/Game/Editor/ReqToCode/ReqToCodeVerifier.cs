using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Unity.FPS.Game.Editor
{
    /// <summary>
    /// ReqToCode traceability verification: checks that every approved requirement whose
    /// trace is required is referenced by at least one [Traces] attribute in code, that
    /// every approved requirement whose test is required is referenced by at least one
    /// [Verifies] attribute in a test assembly, that the generated traceables match the
    /// requirement sources, and reports deprecated requirements that are still referenced.
    /// Violations are logged as console errors after every script reload and fail player
    /// builds (see <see cref="ReqToCodeBuildCheck"/>).
    /// </summary>
    public static class ReqToCodeVerifier
    {
        public sealed class Report
        {
            public readonly List<string> Errors = new List<string>();
            public readonly List<string> Warnings = new List<string>();
        }

        public sealed class ReferenceMap
        {
            /// <summary>[Traces] references (implementation code), per requirement.</summary>
            public readonly Dictionary<SWR, List<string>> Traces = new Dictionary<SWR, List<string>>();

            /// <summary>[Verifies] references found in test assemblies, per requirement.</summary>
            public readonly Dictionary<SWR, List<string>> Verifies = new Dictionary<SWR, List<string>>();
        }

        /// <summary>Runs all traceability checks. Does not log; callers decide how to surface results.</summary>
        public static Report Verify()
        {
            var report = new Report();

            ReqToCodeGenerator.ParseRequirements(out List<string> parseErrors);
            report.Errors.AddRange(parseErrors);

            if (parseErrors.Count == 0 && !ReqToCodeGenerator.IsUpToDate(out string staleReason))
                report.Errors.Add($"[ReqToCode] {staleReason}");

            ReferenceMap references = CollectReferences();

            foreach (FieldInfo field in typeof(SWR).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var meta = field.GetCustomAttribute<RequirementAttribute>();
                if (meta == null)
                    continue;

                var value = (SWR)field.GetValue(null);
                bool traced = references.Traces.TryGetValue(value, out List<string> traceLocations) && traceLocations.Count > 0;
                bool verified = references.Verifies.TryGetValue(value, out List<string> testLocations) && testLocations.Count > 0;

                if (meta.Status == RequirementStatus.Approved && meta.TraceRequired && !traced)
                {
                    report.Errors.Add(
                        $"[ReqToCode] {meta.Id} \"{meta.Title}\" is approved but not traced in code. " +
                        $"Add [Traces(SWR.{field.Name})] to the implementing code element (source: {meta.SourcePath}).");
                }

                if (meta.Status == RequirementStatus.Approved && meta.TestRequired && !verified)
                {
                    report.Errors.Add(
                        $"[ReqToCode] {meta.Id} \"{meta.Title}\" is approved but has no test coverage. " +
                        $"Add [Verifies(SWR.{field.Name})] to a test in a test assembly (source: {meta.SourcePath}).");
                }

                if (meta.Status == RequirementStatus.Deprecated && (traced || verified))
                {
                    var locations = new List<string>();
                    if (traced) locations.AddRange(traceLocations);
                    if (verified) locations.AddRange(testLocations);
                    report.Warnings.Add(
                        $"[ReqToCode] {meta.Id} is deprecated but still referenced by: {string.Join(", ", locations)} " +
                        $"(source: {meta.SourcePath}). Rework or remove the referencing code.");
                }
            }

            return report;
        }

        /// <summary>
        /// Sweeps all assemblies referencing fps.Game and maps every SWR to the code elements
        /// carrying [Traces] (any assembly) and [Verifies] (test assemblies only) for it.
        /// </summary>
        public static ReferenceMap CollectReferences()
        {
            var map = new ReferenceMap();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic || !ReferencesAssembly(assembly, "fps.Game"))
                    continue;

                bool isTestAssembly = ReferencesAssembly(assembly, "nunit.framework");

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).ToArray();
                }

                foreach (Type type in types)
                {
                    try
                    {
                        CollectFrom(map, type.GetCustomAttributes(false), type.FullName, isTestAssembly);

                        foreach (MemberInfo member in type.GetMembers(
                                     BindingFlags.Public | BindingFlags.NonPublic |
                                     BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                        {
                            CollectFrom(map, member.GetCustomAttributes(false),
                                $"{type.FullName}.{member.Name}", isTestAssembly);
                        }
                    }
                    catch (Exception)
                    {
                        // Types whose attributes cannot be loaded are irrelevant to traceability.
                    }
                }
            }

            return map;
        }

        static void CollectFrom(ReferenceMap map, object[] attributes, string location, bool isTestAssembly)
        {
            foreach (object attribute in attributes)
            {
                if (attribute is TracesAttribute traces)
                    Add(map.Traces, traces.Requirements, location);
                else if (attribute is VerifiesAttribute verifies && isTestAssembly)
                    Add(map.Verifies, verifies.Requirements, location);
            }
        }

        static void Add(Dictionary<SWR, List<string>> map, SWR[] requirements, string location)
        {
            foreach (SWR requirement in requirements)
            {
                if (!map.TryGetValue(requirement, out List<string> locations))
                    map[requirement] = locations = new List<string>();
                locations.Add(location);
            }
        }

        static bool ReferencesAssembly(Assembly assembly, string assemblyName)
        {
            if (assembly.GetName().Name == assemblyName)
                return true;

            return assembly.GetReferencedAssemblies().Any(reference => reference.Name == assemblyName);
        }

        [MenuItem("Tools/ReqToCode/Verify Traceability")]
        public static void VerifyMenu()
        {
            Report report = Verify();
            LogReport(report);
            if (report.Errors.Count == 0)
            {
                ReferenceMap references = CollectReferences();
                Debug.Log($"[ReqToCode] Traceability OK ({references.Traces.Count} requirement(s) traced, " +
                          $"{references.Verifies.Count} covered by tests).");
            }
        }

        public static void LogReport(Report report)
        {
            foreach (string warning in report.Warnings)
                Debug.LogWarning(warning);
            foreach (string error in report.Errors)
                Debug.LogError(error);
        }
    }

    /// <summary>
    /// Runs ReqToCode after every script reload: regenerates stale traceables (which triggers
    /// a recompile) and otherwise verifies traceability, surfacing violations as console errors.
    /// </summary>
    static class ReqToCodeHooks
    {
        [DidReloadScripts]
        static void OnScriptsReloaded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            EditorApplication.delayCall += () =>
            {
                bool regenerated = ReqToCodeGenerator.RegenerateIfStale(out List<string> parseErrors);
                foreach (string error in parseErrors)
                    Debug.LogError(error);

                if (parseErrors.Count > 0)
                    return;

                if (regenerated)
                {
                    Debug.Log("[ReqToCode] Requirement sources changed, regenerated " +
                              $"{ReqToCodeGenerator.GeneratedFilePath} (recompile pending).");
                    return;
                }

                ReqToCodeVerifier.LogReport(ReqToCodeVerifier.Verify());
            };
        }
    }
}
