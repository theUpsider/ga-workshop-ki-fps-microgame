# Agent instructions

This is a Unity project (FPS Microgame-based). C# scripts live under `Assets/FPS/Scripts/`
(runtime code split into `AI`, `Game`, `Gameplay`, `UI` assemblies) and `Assets/Tests/`
(`EditMode`/`PlayMode` NUnit test assemblies).

## Requirements traceability (ReqToCode)

Requirements live as markdown files with YAML frontmatter (`req-id`, `status`, `trace`,
`title`) under `Docs/requirements/` — see [Docs/requirements/README.md](Docs/requirements/README.md).
From these, `Assets/FPS/Scripts/Game/Requirements/SWR.g.cs` is generated (never edit it
manually; regenerate via the Unity menu Tools ▸ ReqToCode ▸ Regenerate Traceables, or let
the automatic reload/test-run hook do it).

Rules when working in this repo:

- Code implementing a requirement must carry `[Traces(SWR.SWR_<n>)]` on the implementing
  class/method. Approved requirements with `trace: required` that are untraced cause
  console errors after compilation, failing EditMode tests, and aborted player builds.
- Tests covering a requirement must carry `[Verifies(SWR.SWR_<n>)]` on the test
  method/class (only counts inside test assemblies). Approved requirements with
  `test: required` and no `[Verifies]` reference fail verification the same way.
  When implementing a new requirement, add both the `[Traces]` on the code and a
  `[Verifies]` test.
- When a requirement document changes, the traceables regenerate and the compiler/IDE
  flags affected code: obsolete-warnings for deprecated requirements, compile errors for
  removed ones. Fix the flagged code as part of the change.
- New requirements get a new unique `req-id` (`SWR-x00` = feature epic, `SWR-x01+` =
  individual requirements; `1xx` lockable-barrier, `2xx` alarm-system, `3xx`
  interaction-system, `4xx` stateful-hazard).
- A pre-commit hook (`Tools/git-hooks/pre-commit`, enable once per clone with
  `git config core.hooksPath Tools/git-hooks`) runs
  `Tools/reqtocode/Check-ReqToCode.ps1` and blocks commits on parse errors, a stale
  `SWR.g.cs`, or missing required traces — no Unity Editor needed. If it reports a
  stale `SWR.g.cs`, regenerate (Editor menu or `Check-ReqToCode.ps1 -Fix`) and stage
  the result; do not edit the file by hand.
- When ReqToCode reports violations (console errors, failing `ReqToCodeTests`,
  compile errors on removed `SWR` members, or a hook-blocked commit), follow the
  change-propagation playbook in
  [.claude/skills/reqtocode-propagate/SKILL.md](.claude/skills/reqtocode-propagate/SKILL.md):
  read the requirement diff, regenerate, find all `[Traces]`/`[Verifies]` reference
  sites, rework implementation and tests to match the new requirement text, run the
  Edit Mode suite, and commit everything as one unit. Never silence a violation by
  deleting traces or weakening the requirement.

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

### How to invoke (by agent type)

- **VS Code / GitHub Copilot agent**: Use the VS Code task. Run the command
  `workbench.action.tasks.runTask` with args `["Unity: Run Edit Mode Tests"]`.
  This is the most reliable method — terminal text-injection (`sendSequence`)
  is unreliable because the terminal may not be ready. After the task completes,
  read `Temp/agent-test-result.json` for the outcome.

- **Claude Code / terminal-capable agent**: Run the PowerShell command directly:
  `powershell -NoProfile -File Tools/agent-tests/Invoke-AgentTests.ps1`.

- **Either agent**: Both methods write the request to `Temp/agent-test-request.txt`
  and poll `Temp/agent-test-result.json`. Results are identical regardless of
  invocation method.
