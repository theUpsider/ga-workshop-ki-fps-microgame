using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Unity.FPS.Game.Editor
{
    /// <summary>
    /// ReqToCode build gate: a broken trace is a broken build. Player builds fail when
    /// requirement sources are invalid, generated traceables are stale, or an approved
    /// requirement with a required trace is not referenced by any [Traces] attribute.
    /// </summary>
    public sealed class ReqToCodeBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            ReqToCodeVerifier.Report result = ReqToCodeVerifier.Verify();
            ReqToCodeVerifier.LogReport(result);

            if (result.Errors.Count > 0)
            {
                throw new BuildFailedException(
                    $"[ReqToCode] Build aborted: {result.Errors.Count} traceability violation(s). See Console.");
            }
        }
    }
}
