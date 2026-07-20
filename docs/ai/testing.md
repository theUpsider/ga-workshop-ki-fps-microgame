# Unity testing contract

## Required command

Run Unity tests only through:

```
python tools/unity_test.py ...
```

Do not invoke the Unity executable directly.

## Before changing code

- Identify whether the affected behavior belongs in Edit Mode or Play Mode.
- For bug fixes, add or identify a regression test that fails before the fix.
- Do not weaken, skip, delete, ignore or quarantine tests merely to obtain a green result.

## After changing code

- Run the narrowest relevant test first.
- Then run the required regression suite from the test selection matrix below.
- Treat every non-zero exit code as a failed validation.
- Read `summary.json` and the Unity log when a run fails.
- Do not report success unless all required tests passed.

## Test design

- Use Arrange-Act-Assert.
- Prefer normal NUnit tests (`[Test]`, `[TestCase]`) for pure C# behavior.
- Use `[UnityTest]` only for frame-dependent Unity behavior.
- Keep tests deterministic and independent.
- Abstract time, randomness, network, filesystem and platform services behind interfaces.
- Test observable behavior rather than private methods.
- Name tests `MethodOrBehavior_Condition_ExpectedResult`.

## Failure handling

- Fix production code when a valid test exposes a regression.
- Fix test code only when the test is demonstrably incorrect.
- Do not change expected values merely to match current behavior.
- Report baseline failures separately from failures introduced by the change.

## Test selection matrix

| Change                                       | Required tests                                                              |
| --------------------------------------------- | ----------------------------------------------------------------------------- |
| Pure C# logic                                | Affected Edit Mode tests, then the full Edit Mode suite                     |
| Bug fix                                      | Failing regression test first, then the full Edit Mode suite                |
| `MonoBehaviour` or player loop               | Affected Play Mode tests and the full Edit Mode suite                       |
| Physics, scene, prefab, input or animation   | Affected Play Mode tests and the Play Mode smoke suite                      |
| `.asmdef` change                             | Full suite                                                                   |
| `Packages/manifest.json` or package lock     | Full suite                                                                   |
| `ProjectSettings`                            | Full suite                                                                   |
| Shared test utilities                        | Full suite                                                                   |
| Documentation only                           | No Unity tests required                                                     |
| Unclear impact                               | Full Edit Mode suite; add Play Mode if Unity runtime behavior is involved   |

Tests may be skipped only when exclusively documentation changed, or the required Unity
infrastructure is provably unavailable. In the latter case, state this explicitly and do not
describe the task as fully validated.

## Running tests while the Editor is open

Unity refuses a second process on the same project, so `-batchmode -runTests` cannot run
while you have the project open in the Editor. To make `python tools/unity_test.py` work in
that situation anyway, an Editor-only script
(`Assets/Editor/AutomatedTests/TestRunnerSocketServer.cs`) opens a localhost socket
(`127.0.0.1:17930`) and drives `TestRunnerApi` inside the running Editor. The wrapper tries
this connection first and transparently falls back to spawning a headless Unity process when
nothing is listening (CI, no Editor open, or the socket is disabled via
`connectedEditor.enabled: false` in `tools/unity_test_config.json`).

Known limitation: entering Play Mode triggers a Unity domain reload, which drops the socket
connection before a response can be sent. `--platform PlayMode` therefore only completes
through the connected-editor path when domain reload is disabled for Play Mode (Project
Settings > Editor > Enter Play Mode Options). Otherwise run PlayMode either with the Editor
closed (batch-mode fallback) or on the CI runner, where `connectedEditor` has nothing to
connect to and the batch-mode path is used automatically.

## Required final report

State:

- which test commands were executed,
- whether each command passed,
- number of executed and failed tests,
- paths to the result XML and Unity log,
- any validation that could not be performed.
