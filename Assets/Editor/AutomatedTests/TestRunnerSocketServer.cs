using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace RepoTooling.Editor.AutomatedTests
{
    /// Lets tools/unity_test.py run tests through an already-open Editor instead of
    /// spawning a second, exclusive-lock batchmode process. Unity does not allow two
    /// processes on the same project, so this is the only way to test while the Editor is open.
    [InitializeOnLoad]
    internal static class TestRunnerSocketServer
    {
        private const int Port = 17930;

        private static TcpListener _listener;
        private static Thread _acceptThread;
        private static readonly Queue<PendingRun> PendingRuns = new Queue<PendingRun>();
        private static readonly object QueueLock = new object();
        private static bool _isRunning;

        static TestRunnerSocketServer()
        {
            StartListener();
            EditorApplication.update += ProcessQueue;
            EditorApplication.quitting += StopListener;
        }

        private static void StartListener()
        {
            try
            {
                _listener = new TcpListener(IPAddress.Loopback, Port);
                _listener.Start();
                _acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "UnityTestSocketServer" };
                _acceptThread.Start();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[AutomatedTests] Could not start test runner socket server on port {Port}: {exception.Message}");
            }
        }

        private static void StopListener()
        {
            try
            {
                _listener?.Stop();
            }
            catch (Exception)
            {
                // Listener is already gone; nothing to clean up.
            }
        }

        private static void AcceptLoop()
        {
            while (true)
            {
                TcpClient client;
                try
                {
                    client = _listener.AcceptTcpClient();
                }
                catch (Exception)
                {
                    return; // Listener was stopped.
                }
                HandleClient(client);
            }
        }

        private static void HandleClient(TcpClient client)
        {
            var pending = new PendingRun(client);
            try
            {
                var reader = new StreamReader(client.GetStream(), Encoding.UTF8);
                var requestLine = reader.ReadLine();
                if (string.IsNullOrEmpty(requestLine))
                {
                    client.Close();
                    return;
                }
                pending.Request = JsonUtility.FromJson<RunRequest>(requestLine);
                lock (QueueLock)
                {
                    PendingRuns.Enqueue(pending);
                }
                pending.Completed.WaitOne();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[AutomatedTests] Test runner socket client failed: {exception.Message}");
            }
            finally
            {
                try
                {
                    client.Close();
                }
                catch (Exception)
                {
                    // Already closed.
                }
            }
        }

        private static void ProcessQueue()
        {
            if (_isRunning)
            {
                return;
            }
            PendingRun pending;
            lock (QueueLock)
            {
                if (PendingRuns.Count == 0)
                {
                    return;
                }
                pending = PendingRuns.Dequeue();
            }
            _isRunning = true;
            RunTests(pending);
        }

        private static void RunTests(PendingRun pending)
        {
            var request = pending.Request;
            var logBuilder = new StringBuilder();
            Application.LogCallback logHandler = (condition, stackTrace, type) =>
            {
                logBuilder.AppendLine($"[{type}] {condition}");
            };

            TestMode testMode;
            if (!Enum.TryParse(request.platform, out testMode))
            {
                SendResponse(pending, false, $"Unsupported platform '{request.platform}'");
                _isRunning = false;
                return;
            }

            var filter = new Filter
            {
                testMode = testMode,
                groupNames = (request.filters != null && request.filters.Length > 0) ? request.filters : null,
                categoryNames = (request.categories != null && request.categories.Length > 0) ? request.categories : null,
                assemblyNames = (request.assemblies != null && request.assemblies.Length > 0) ? request.assemblies : null,
            };

            // Hier test runner api
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var callbacks = new RunCallbacks();
            callbacks.Finished += result =>
            {
                Application.logMessageReceived -= logHandler;
                try
                {
                    File.WriteAllText(request.resultsPath, result.ToXml().OuterXml, Encoding.UTF8);
                    File.WriteAllText(request.logPath, logBuilder.ToString(), Encoding.UTF8);
                    SendResponse(pending, true, null);
                }
                catch (Exception exception)
                {
                    SendResponse(pending, false, $"Failed to write test results: {exception.Message}");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(api);
                    _isRunning = false;
                }
            };

            api.RegisterCallbacks(callbacks);
            Application.logMessageReceived += logHandler;

            try
            {
                // Start the test run. This will call RunFinished when done.
                api.Execute(new ExecutionSettings(filter));
            }
            catch (Exception exception)
            {
                Application.logMessageReceived -= logHandler;
                api.UnregisterCallbacks(callbacks);
                UnityEngine.Object.DestroyImmediate(api);
                _isRunning = false;
                SendResponse(pending, false, $"Failed to start test run: {exception.Message}");
            }
        }

        private static void SendResponse(PendingRun pending, bool ok, string error)
        {
            try
            {
                var response = new RunResponse { ok = ok, error = error };
                var json = JsonUtility.ToJson(response) + "\n";
                var bytes = Encoding.UTF8.GetBytes(json);
                pending.Client.GetStream().Write(bytes, 0, bytes.Length);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[AutomatedTests] Could not send test runner response: {exception.Message}");
            }
            finally
            {
                pending.Completed.Set();
            }
        }

        private sealed class PendingRun
        {
            public readonly TcpClient Client;
            public readonly ManualResetEvent Completed = new ManualResetEvent(false);
            public RunRequest Request;

            public PendingRun(TcpClient client)
            {
                Client = client;
            }
        }

        private sealed class RunCallbacks : ICallbacks
        {
            public event Action<ITestResultAdaptor> Finished;

            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                Finished?.Invoke(result);
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }
        }

        [Serializable]
        private sealed class RunRequest
        {
            public string platform;
            public string[] filters;
            public string[] categories;
            public string[] assemblies;
            public string resultsPath;
            public string logPath;
        }

        [Serializable]
        private sealed class RunResponse
        {
            public bool ok;
            public string error;
        }
    }
}
