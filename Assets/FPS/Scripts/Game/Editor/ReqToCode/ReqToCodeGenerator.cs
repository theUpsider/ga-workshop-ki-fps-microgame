using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Unity.FPS.Game.Editor
{
    /// <summary>
    /// ReqToCode: generates compile-time traceables (the <see cref="SWR"/> enum) from the
    /// markdown requirement documents under Docs/requirements.
    ///
    /// A requirement document is any .md file whose YAML frontmatter contains a `req-id`
    /// field (format SWR-&lt;number&gt;). Supported frontmatter fields:
    ///   req-id: SWR-101          (required, unique)
    ///   status: approved         (draft | approved | deprecated; default draft)
    ///   trace:  required         (required | optional; default required)
    ///   test:   required         (required | optional; default required)
    ///   title:  Short title      (default: first markdown heading)
    /// </summary>
    public static class ReqToCodeGenerator
    {
        public const string RequirementsRoot = "Docs/requirements";
        public const string GeneratedFilePath = "Assets/FPS/Scripts/Game/Requirements/SWR.g.cs";

        static readonly Regex IdPattern = new Regex(@"^SWR-(\d+)$");

        public sealed class ParsedRequirement
        {
            public string Id;
            public int Number;
            public RequirementStatus Status;
            public string Title;
            public string SourcePath;
            public bool TraceRequired;
            public bool TestRequired;
            public string ContentHash;
        }

        public static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);

        public static List<ParsedRequirement> ParseRequirements(out List<string> errors)
        {
            errors = new List<string>();
            var result = new List<ParsedRequirement>();

            string root = Path.Combine(ProjectRoot, RequirementsRoot);
            if (!Directory.Exists(root))
            {
                errors.Add($"[ReqToCode] Requirements folder not found: {RequirementsRoot}");
                return result;
            }

            foreach (string file in Directory.GetFiles(root, "*.md", SearchOption.AllDirectories)
                         .OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                string relPath = file.Substring(ProjectRoot.Length + 1).Replace('\\', '/');
                string text = File.ReadAllText(file);
                Dictionary<string, string> frontmatter = ParseFrontmatter(text);
                if (frontmatter == null || !frontmatter.TryGetValue("req-id", out string id))
                    continue;

                id = id.Trim();
                Match match = IdPattern.Match(id);
                if (!match.Success)
                {
                    errors.Add($"[ReqToCode] {relPath}: invalid req-id '{id}' (expected format SWR-<number>).");
                    continue;
                }

                RequirementStatus status = RequirementStatus.Draft;
                if (frontmatter.TryGetValue("status", out string statusText))
                {
                    switch (statusText.Trim().ToLowerInvariant())
                    {
                        case "draft": status = RequirementStatus.Draft; break;
                        case "approved": status = RequirementStatus.Approved; break;
                        case "deprecated": status = RequirementStatus.Deprecated; break;
                        default:
                            errors.Add($"[ReqToCode] {relPath}: invalid status '{statusText}' (draft | approved | deprecated).");
                            continue;
                    }
                }

                bool traceRequired = true;
                if (frontmatter.TryGetValue("trace", out string traceText))
                {
                    switch (traceText.Trim().ToLowerInvariant())
                    {
                        case "required": traceRequired = true; break;
                        case "optional": traceRequired = false; break;
                        default:
                            errors.Add($"[ReqToCode] {relPath}: invalid trace '{traceText}' (required | optional).");
                            continue;
                    }
                }

                bool testRequired = true;
                if (frontmatter.TryGetValue("test", out string testText))
                {
                    switch (testText.Trim().ToLowerInvariant())
                    {
                        case "required": testRequired = true; break;
                        case "optional": testRequired = false; break;
                        default:
                            errors.Add($"[ReqToCode] {relPath}: invalid test '{testText}' (required | optional).");
                            continue;
                    }
                }

                string title = frontmatter.TryGetValue("title", out string titleText) && !string.IsNullOrWhiteSpace(titleText)
                    ? titleText.Trim()
                    : FirstHeading(text) ?? id;

                result.Add(new ParsedRequirement
                {
                    Id = id,
                    Number = int.Parse(match.Groups[1].Value),
                    Status = status,
                    Title = title,
                    SourcePath = relPath,
                    TraceRequired = traceRequired,
                    TestRequired = testRequired,
                    ContentHash = ShortHash(NormalizeLineEndings(text))
                });
            }

            foreach (var group in result.GroupBy(r => r.Number).Where(g => g.Count() > 1))
            {
                errors.Add($"[ReqToCode] Duplicate req-id SWR-{group.Key} in: " +
                           string.Join(", ", group.Select(r => r.SourcePath)));
            }

            result.Sort((a, b) => a.Number.CompareTo(b.Number));
            return result;
        }

        public static string GenerateSource(IReadOnlyList<ParsedRequirement> requirements)
        {
            string globalHash = ShortHash(string.Join("\n",
                requirements.Select(r => $"{r.Id}|{r.Status}|{r.TraceRequired}|{r.TestRequired}|{r.Title}|{r.SourcePath}|{r.ContentHash}")));

            var sb = new StringBuilder();
            sb.Append("// <auto-generated>\n");
            sb.Append("//     ReqToCode traceables, generated from Docs/requirements. DO NOT EDIT MANUALLY.\n");
            sb.Append("//     Source of truth: the markdown file referenced on each member.\n");
            sb.Append("//     Regenerate: menu \"Tools/ReqToCode/Regenerate Traceables\" (also runs automatically on script reload).\n");
            sb.Append($"//     requirements-hash: {globalHash}\n");
            sb.Append("// </auto-generated>\n");
            sb.Append("using System;\n");
            sb.Append("\n");
            sb.Append("namespace Unity.FPS.Game\n");
            sb.Append("{\n");
            sb.Append("    /// <summary>\n");
            sb.Append("    /// Software requirements (SWR) as compile-time traceables (ReqToCode).\n");
            sb.Append("    /// Removing a requirement removes its member, so every [Traces] reference breaks the build.\n");
            sb.Append("    /// Deprecating a requirement raises obsolete-warnings at every reference site.\n");
            sb.Append("    /// </summary>\n");
            sb.Append("    public enum SWR\n");
            sb.Append("    {\n");

            for (int i = 0; i < requirements.Count; i++)
            {
                ParsedRequirement req = requirements[i];
                if (i > 0)
                    sb.Append("\n");

                string statusLabel = req.Status.ToString().ToLowerInvariant();
                sb.Append($"        /// <summary>[{statusLabel}] {XmlEscape(req.Title)} ({req.SourcePath})</summary>\n");

                if (req.Status == RequirementStatus.Deprecated)
                {
                    sb.Append($"        [Obsolete(\"{CsEscape(req.Id)} is deprecated: {CsEscape(req.Title)} (see {CsEscape(req.SourcePath)})\")]\n");
                }

                sb.Append($"        [Requirement(\"{CsEscape(req.Id)}\", RequirementStatus.{req.Status}, \"{CsEscape(req.Title)}\", " +
                          $"\"{CsEscape(req.SourcePath)}\", {(req.TraceRequired ? "true" : "false")}, \"{req.ContentHash}\", " +
                          $"{(req.TestRequired ? "true" : "false")})]\n");
                sb.Append($"        {req.Id.Replace('-', '_')} = {req.Number},\n");
            }

            sb.Append("    }\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>True when SWR.g.cs on disk matches what the current requirement sources generate.</summary>
        public static bool IsUpToDate(out string reason)
        {
            List<ParsedRequirement> requirements = ParseRequirements(out List<string> errors);
            if (errors.Count > 0)
            {
                reason = "requirement sources have errors: " + string.Join("; ", errors);
                return false;
            }

            string targetPath = Path.Combine(ProjectRoot, GeneratedFilePath);
            if (!File.Exists(targetPath))
            {
                reason = $"{GeneratedFilePath} does not exist.";
                return false;
            }

            string expected = NormalizeLineEndings(GenerateSource(requirements));
            string actual = NormalizeLineEndings(File.ReadAllText(targetPath));
            if (expected != actual)
            {
                reason = $"{GeneratedFilePath} does not match the requirement sources (regenerate via Tools/ReqToCode).";
                return false;
            }

            reason = null;
            return true;
        }

        /// <summary>Regenerates SWR.g.cs if it is stale. Returns true when the file was rewritten.</summary>
        public static bool RegenerateIfStale(out List<string> parseErrors)
        {
            List<ParsedRequirement> requirements = ParseRequirements(out parseErrors);
            if (parseErrors.Count > 0)
                return false;

            string targetPath = Path.Combine(ProjectRoot, GeneratedFilePath);
            string expected = GenerateSource(requirements);

            if (File.Exists(targetPath) &&
                NormalizeLineEndings(File.ReadAllText(targetPath)) == NormalizeLineEndings(expected))
                return false;

            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
            File.WriteAllText(targetPath, expected, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(GeneratedFilePath);
            return true;
        }

        [MenuItem("Tools/ReqToCode/Regenerate Traceables")]
        public static void RegenerateMenu()
        {
            bool regenerated = RegenerateIfStale(out List<string> errors);
            foreach (string error in errors)
                Debug.LogError(error);

            if (errors.Count > 0)
                Debug.LogError("[ReqToCode] Regeneration aborted, fix the requirement sources first.");
            else if (regenerated)
                Debug.Log($"[ReqToCode] Regenerated {GeneratedFilePath} from {RequirementsRoot}.");
            else
                Debug.Log("[ReqToCode] Traceables already up to date.");
        }

        static Dictionary<string, string> ParseFrontmatter(string text)
        {
            string[] lines = NormalizeLineEndings(text.TrimStart('\uFEFF')).Split('\n');
            int start = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim().Length == 0)
                    continue;
                start = lines[i].Trim() == "---" ? i : -1;
                break;
            }

            if (start < 0)
                return null;

            var frontmatter = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = start + 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Trim() == "---")
                    return frontmatter;

                int colon = line.IndexOf(':');
                if (colon > 0)
                    frontmatter[line.Substring(0, colon).Trim()] = line.Substring(colon + 1).Trim();
            }

            return null; // frontmatter never closed
        }

        static string FirstHeading(string text)
        {
            foreach (string line in NormalizeLineEndings(text).Split('\n'))
            {
                string trimmed = line.TrimStart();
                if (trimmed.StartsWith("# "))
                    return trimmed.Substring(2).Trim();
            }

            return null;
        }

        static string NormalizeLineEndings(string text) => text.Replace("\r\n", "\n").Replace("\r", "\n");

        static string ShortHash(string text)
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
                var sb = new StringBuilder(16);
                for (int i = 0; i < 8; i++)
                    sb.Append(hash[i].ToString("x2"));
                return sb.ToString();
            }
        }

        static string CsEscape(string text) => text.Replace("\\", "\\\\").Replace("\"", "\\\"");

        static string XmlEscape(string text) => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }
}
