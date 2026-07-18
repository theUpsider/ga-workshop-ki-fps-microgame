using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
namespace Unity.FPS.Game.Editor
{

    [InitializeOnLoad]
    public static class AgentTestBridge
    {
        private static readonly string RequestPath =
            Path.Combine(Application.dataPath, "../Temp/agent-test-request.txt");

        private static readonly string ResultPath =
            Path.Combine(Application.dataPath, "../Temp/agent-test-result.json");

        private static bool testRunInProgress;

        static AgentTestBridge()
        {
            EditorApplication.update += CheckForRequest;
        }

        private static void CheckForRequest()
        {
            if (testRunInProgress || !File.Exists(RequestPath))
                return;

            string testName;

            try
            {
                testName = File.ReadAllText(RequestPath).Trim();
            }
            catch (Exception ex)
            {
                WriteResult($"{{\"status\":\"error\",\"message\":\"could not read request: {Escape(ex.Message)}\"}}");
                return;
            }

            // Pick up file changes made while the editor was unfocused and make sure the
            // ReqToCode traceables match the requirement sources before running tests.
            AssetDatabase.Refresh();
            if (ReqToCodeGenerator.RegenerateIfStale(out _))
                return; // recompile pending; the request stays queued and is retried after reload

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            RunEditModeTests(testName);
        }

        private static void RunEditModeTests(string testName)
        {
            testRunInProgress = true;
            WriteResult("{\"status\":\"running\"}");

            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new TestCallbacks());

            var filter = new Filter
            {
                testMode = TestMode.EditMode,
                testNames = string.IsNullOrWhiteSpace(testName)
                    ? null
                    : new[] { testName }
            };

            api.Execute(new ExecutionSettings(filter));
        }

        private static void WriteResult(string json)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ResultPath)!);
            File.WriteAllText(ResultPath, json + Environment.NewLine);
        }

        private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        private sealed class TestCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                testRunInProgress = false;
                WriteResult(
                    "{\"status\":\"finished\",\"state\":\"" + result.ResultState +
                    "\",\"passCount\":" + result.PassCount +
                    ",\"failCount\":" + result.FailCount + "}");

                try
                {
                    if (File.Exists(RequestPath))
                        File.Delete(RequestPath);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"AgentTestBridge: could not delete request file: {ex.Message}");
                }
            }
        }
    }
}
