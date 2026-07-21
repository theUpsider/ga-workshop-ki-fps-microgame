---
name: run-unity-tests
description: Run this project's Unity Edit Mode tests after making code changes in Assets/. Use whenever you've finished implementing or fixing something in this Unity project and want to verify it against the test suite before reporting the task as done. Requires the Unity Editor to already be open on this project.
---

# Running Unity tests via AgentTestBridge

This project cannot run Unity tests via a CLI/batchmode invocation from an agent
session directly — instead, `Unity.FPS.Game.Editor.AgentTestBridge`
([AgentTestBridge.cs](../../../Assets/FPS/Scripts/Game/Editor/AgentTestBridge.cs))
runs inside an already-open Unity Editor and polls for a request file. This skill
drives that bridge from the terminal.

The bridge and this skill are shared across coding agents on this project — the
same script backs this Claude Code skill, the root [AGENTS.md](../../../AGENTS.md)
(read automatically by Codex), and the GitHub Copilot `Test-Runner` agent
(`.github/agents/test-runner.agent.md`), so results are consistent no matter which
tool made the change.

> **Agent-specific invocation**: If you are a **VS Code / GitHub Copilot** agent
> with no terminal shell tool, use the VS Code task instead of the PowerShell
> command below: run `workbench.action.tasks.runTask` with argument
> `"Unity: Run Edit Mode Tests"`, then read `Temp/agent-test-result.json`.
> Terminal text-injection (`sendSequence`) is unreliable here — the task runner
> is the supported path for VS Code agents.

## Prerequisites

- The Unity Editor must be open with this project loaded (`AgentTestBridge` is an
  `[InitializeOnLoad]` static class — its `EditorApplication.update` hook only runs
  while the Editor process is alive).
- If the Editor is not open, the script below will time out after ~5 minutes with
  a clear message. Tell the user to open the project in Unity and re-run.

## Steps

1. After finishing a code change, run:

   ```
   powershell -NoProfile -File Tools/agent-tests/Invoke-AgentTests.ps1
   ```

   Optionally scope to a single test (fully qualified name/namespace, matching
   NUnit's `Filter.testNames`):

   ```
   powershell -NoProfile -File Tools/agent-tests/Invoke-AgentTests.ps1 -TestName "Unity.FPS.Tests.BarrierTests"
   ```

   Note: use `powershell` (Windows PowerShell), not `pwsh` — PowerShell 7 isn't
   installed on this machine, only Windows PowerShell 5.1. `powershell` works from
   both a PowerShell tool and a Bash/git-bash tool.

   Under the hood this writes the (optional) test name to `Temp/agent-test-request.txt`,
   which `AgentTestBridge` picks up on its next `EditorApplication.update` tick,
   runs the Edit Mode suite through `TestRunnerApi`, and writes the outcome to
   `Temp/agent-test-result.json`. The script polls that file and prints a summary.

2. Read the exit code to decide what to do next:

   | Exit code | Meaning                                                                   |
   | --------- | ------------------------------------------------------------------------- |
   | `0`       | Tests ran, all passed. Safe to report the task as done.                   |
   | `1`       | Tests ran, at least one failed. Fix the failure(s) before reporting done. |
   | `2`       | Timed out — Editor likely not open, or still compiling/importing.         |
   | `3`       | The bridge itself errored (e.g. couldn't read the request file).          |

3. On failure (exit `1`), read `Temp/agent-test-result.json` and/or the Unity
   Editor log (`Editor.log` / the Console window) for the specific failing test
   names before making further changes.

## Notes

- This only covers **Edit Mode** tests (`TestMode.EditMode` in `AgentTestBridge`).
  Play Mode tests need the Editor to enter play mode and aren't wired up here.
- Don't edit `Temp/agent-test-request.txt` / `Temp/agent-test-result.json` by hand
  outside of this flow — the bridge owns their lifecycle (it deletes the request
  file itself once a run finishes).
- If the script keeps timing out even though the Editor is open, check the Unity
  Console for compile errors — the bridge can't run tests while scripts are
  reloading/broken.
