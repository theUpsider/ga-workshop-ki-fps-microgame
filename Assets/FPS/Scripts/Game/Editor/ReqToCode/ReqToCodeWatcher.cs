using System;
using System.IO;
using System.Text;
using UnityEditor;

namespace Unity.FPS.Game.Editor
{
    /// <summary>
    /// Watches the requirement markdown sources under
    /// <see cref="ReqToCodeGenerator.RequirementsRoot"/> and reconciles the generated
    /// traceables whenever they change. Those files live outside the <c>Assets/</c> tree, so
    /// the AssetDatabase never raises an import event for them and the
    /// <c>[DidReloadScripts]</c> hook (which fires only on C# recompilation) never sees a
    /// markdown-only edit. Without this watcher a deleted or renamed requirement would leave a
    /// stale member in <c>SWR.g.cs</c> — its <c>[Traces]</c> references keep compiling — until
    /// the next manual verify, recompile, build, or commit.
    ///
    /// A lightweight fingerprint (path + last-write time + size of every <c>*.md</c>) is polled
    /// on <see cref="EditorApplication.update"/> about once per second; when it changes the same
    /// regenerate-or-verify logic the reload hook uses runs, surfacing the change within ~1s.
    /// Polling runs entirely on the main thread, so there is no file-watcher thread to marshal
    /// or dispose across domain reloads.
    /// </summary>
    [InitializeOnLoad]
    static class ReqToCodeWatcher
    {
        const double PollIntervalSeconds = 1.0;

        static double s_nextPollTime;
        static string s_lastFingerprint;

        static ReqToCodeWatcher()
        {
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < s_nextPollTime)
                return;

            s_nextPollTime = EditorApplication.timeSinceStartup + PollIntervalSeconds;

            // Skip while the editor is busy or playing: acting mid-compile/import is unsafe, and
            // the reload hook already reconciles once compilation settles.
            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            string fingerprint = ComputeFingerprint();

            // First tick after each domain reload only seeds the baseline; the reload hook has
            // already reconciled any load-time staleness, so we must not fire spuriously here.
            if (s_lastFingerprint == null)
            {
                s_lastFingerprint = fingerprint;
                return;
            }

            if (fingerprint == s_lastFingerprint)
                return;

            s_lastFingerprint = fingerprint;

            // Defer out of the update loop to avoid reentrancy during regeneration/import.
            EditorApplication.delayCall += ReqToCodeHooks.RunAfterChange;
        }

        /// <summary>
        /// Cheap change signal for the requirement sources: the sorted relative path, last-write
        /// tick, and length of every <c>*.md</c> under the requirements root. A missing folder
        /// yields a distinct sentinel so deleting the whole tree is still detected.
        /// </summary>
        static string ComputeFingerprint()
        {
            string root = Path.Combine(ReqToCodeGenerator.ProjectRoot, ReqToCodeGenerator.RequirementsRoot);
            if (!Directory.Exists(root))
                return "<missing>";

            string[] files = Directory.GetFiles(root, "*.md", SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);

            var sb = new StringBuilder();
            foreach (string file in files)
            {
                var info = new FileInfo(file);
                sb.Append(file).Append('|')
                  .Append(info.LastWriteTimeUtc.Ticks).Append('|')
                  .Append(info.Length).Append('\n');
            }

            return sb.ToString();
        }
    }
}
