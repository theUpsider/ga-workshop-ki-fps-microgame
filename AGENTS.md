# Agent instructions

This is a Unity project (FPS Microgame-based). C# scripts live under `Assets/FPS/Scripts/`
(runtime code split into `AI`, `Game`, `Gameplay`, `UI` assemblies) and `Assets/Tests/`
(`EditMode`/`PlayMode` NUnit test assemblies).

## Running tests after a code change

Unity tests cannot be run from a plain CLI/batchmode invocation in this workflow.
Instead, `Unity.FPS.Game.Editor.AgentTestBridge` ([Assets/FPS/Scripts/Game/Editor/AgentTestBridge.cs](Assets/FPS/Scripts/Game/Editor/AgentTestBridge.cs))
runs inside an already-open Unity Editor instance and polls for a request file, so
that any coding agent can ask the live Editor to run the Edit Mode suite without
relaunching Unity.

After changing code under `Assets/`, run:

```
powershell -NoProfile -File Tools/agent-tests/Invoke-AgentTests.ps1
```

Optionally scope to one test (fully qualified name/namespace):

```
powershell -NoProfile -File Tools/agent-tests/Invoke-AgentTests.ps1 -TestName "Unity.FPS.Tests.BarrierTests"
```

This requires the Unity Editor to be open on this project — the script writes to
`Temp/agent-test-request.txt`, which `AgentTestBridge` picks up on its next
`EditorApplication.update` tick, runs the tests via `TestRunnerApi`, and writes
the outcome to `Temp/agent-test-result.json`. The script polls that file and
prints a summary.

Exit codes: `0` = all tests passed, `1` = at least one test failed, `2` = timed
out (Editor not open, or still compiling), `3` = the bridge itself errored.

On failure, inspect `Temp/agent-test-result.json` and the Unity Console/`Editor.log`
for the specific failing tests before making further changes. Only Edit Mode
tests are covered by this bridge; Play Mode tests require entering play mode
manually.

This same script backs the Claude Code skill (`.claude/skills/run-unity-tests/`),
the VS Code task `Unity: Run Edit Mode Tests` (`.vscode/tasks.json`) used by the
`Test-Runner` GitHub Copilot agent (`.github/agents/test-runner.agent.md`), and
this file — so results are consistent regardless of which tool made the change.
