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
    /// trace is required is referenced by at least one [Traces] attribute in code, that the
    /// generated traceables match the requirement sources, and reports deprecated
    /// requirements that are still traced.
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

        /// <summary>Runs all traceability checks. Does not log; callers decide how to surface results.</summary>
        public static Report Verify()
        {
            var report = new Report();

            ReqToCodeGenerator.ParseRequirements(out List<string> parseErrors);
            report.Errors.AddRange(parseErrors);

            if (parseErrors.Count == 0 && !ReqToCodeGenerator.IsUpToDate(out string staleReason))
                report.Errors.Add($"[ReqToCode] {staleReason}");

            Dictionary<SWR, List<string>> traceLocations = CollectTraceLocations();

            foreach (FieldInfo field in typeof(SWR).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var meta = field.GetCustomAttribute<RequirementAttribute>();
                if (meta == null)
                    continue;

                var value = (SWR)field.GetValue(null);
                bool traced = traceLocations.TryGetValue(value, out List<string> locations) && locations.Count > 0;

                if (meta.Status == RequirementStatus.Approved && meta.TraceRequired && !traced)
                {
                    report.Errors.Add(
                        $"[ReqToCode] {meta.Id} \"{meta.Title}\" is approved but not traced in code. " +
                        $"Add [Traces(SWR.{field.Name})] to the implementing code element (source: {meta.SourcePath}).");
                }

                if (meta.Status == RequirementStatus.Deprecated && traced)
                {
                    report.Warnings.Add(
                        $"[ReqToCode] {meta.Id} is deprecated but still traced by: {string.Join(", ", locations)} " +
                        $"(source: {meta.SourcePath}). Rework or remove the traced code.");
                }
            }

            return report;
        }

        /// <summary>Maps each traced SWR to the code elements carrying a [Traces] attribute for it.</summary>
        public static Dictionary<SWR, List<string>> CollectTraceLocations()
        {
            var map = new Dictionary<SWR, List<string>>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic || !ReferencesGameAssembly(assembly))
                    continue;

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
                        AddTraces(map, type.GetCustomAttributes(typeof(TracesAttribute), false), type.FullName);

                        foreach (MemberInfo member in type.GetMembers(
                                     BindingFlags.Public | BindingFlags.NonPublic |
                                     BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                        {
                            AddTraces(map, member.GetCustomAttributes(typeof(TracesAttribute), false),
                                $"{type.FullName}.{member.Name}");
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

        static void AddTraces(Dictionary<SWR, List<string>> map, object[] attributes, string location)
        {
            foreach (TracesAttribute traces in attributes.OfType<TracesAttribute>())
            {
                foreach (SWR requirement in traces.Requirements)
                {
                    if (!map.TryGetValue(requirement, out List<string> locations))
                        map[requirement] = locations = new List<string>();
                    locations.Add(location);
                }
            }
        }

        static bool ReferencesGameAssembly(Assembly assembly)
        {
            string name = assembly.GetName().Name;
            if (name == "fps.Game")
                return true;

            return assembly.GetReferencedAssemblies().Any(reference => reference.Name == "fps.Game");
        }

        [MenuItem("Tools/ReqToCode/Verify Traceability")]
        public static void VerifyMenu()
        {
            Report report = Verify();
            LogReport(report);
            if (report.Errors.Count == 0)
                Debug.Log($"[ReqToCode] Traceability OK ({CollectTraceLocations().Count} requirement(s) traced).");
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
