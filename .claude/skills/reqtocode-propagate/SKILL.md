---
name: reqtocode-propagate
description: Propagate a requirement change through the codebase when ReqToCode reports violations. Use whenever [ReqToCode] errors appear (Unity console errors after compile, failing ReqToCodeTests, compile errors on missing SWR members, obsolete-warnings on deprecated SWR members), whenever the pre-commit hook (Tools/reqtocode/Check-ReqToCode.ps1) blocks a commit, or whenever files under Docs/requirements changed and the code has not been updated to match yet. The result is the requirement change carried through implementation, traces, and tests, ending in a passing commit.
---

# ReqToCode change propagation

Requirements under `Docs/requirements/**` are the source of truth; the code links to
them via `[Traces(SWR.SWR_<n>)]` (implementation) and `[Verifies(SWR.SWR_<n>)]`
(tests). When a requirement changes, ReqToCode surfaces the drift as errors — this
skill is the standard playbook to resolve them by updating the code, **never** by
weakening the requirement or deleting traces to silence the check. Read
[Docs/requirements/README.md](../../../Docs/requirements/README.md) if any concept
is unclear.

## Step 1 — Identify what changed in the requirements

```
git diff HEAD -- Docs/requirements/
git diff --cached -- Docs/requirements/
```

If both are empty (change already committed), find it via
`git log --oneline -5 -- Docs/requirements/` and `git show <commit> -- Docs/requirements/`.

For every affected file note the `req-id` (SWR-<n>) and classify the change:

| Change in the diff | Meaning for the code |
| --- | --- |
| Body text / acceptance criteria changed | Implementation behind `[Traces(SWR.SWR_<n>)]` must be reviewed and adjusted; tests behind `[Verifies(...)]` must assert the new behaviour |
| `status:` → `deprecated` | Referencing code produces obsolete-warnings; rework or remove the implementation and its tests (or document explicitly why it stays for now) |
| Requirement file / `req-id` removed | Every `SWR.SWR_<n>` reference is now a compile error; remove or replace the referencing code and tests |
| New requirement with `status: approved` | Implement it, add `[Traces]` on the code and a `[Verifies]` test |
| `trace:`/`test:` flipped to `required` | Add the missing `[Traces]` reference or `[Verifies]` test |

Read the **full new requirement text**, not just the diff — the diff tells you what
moved, the document tells you what the code must do now.

## Step 2 — Regenerate traceables and get the violation list

```
powershell -NoProfile -File Tools/reqtocode/Check-ReqToCode.ps1 -Fix
powershell -NoProfile -File Tools/reqtocode/Check-ReqToCode.ps1
```

`-Fix` regenerates `Assets/FPS/Scripts/Game/Requirements/SWR.g.cs` from the sources
(stage it later — never edit it by hand). The second run prints the remaining
violations; treat that list as your work queue. Exit 0 with no violations and no
code changes pending means there is nothing to propagate.

## Step 3 — Find every affected code location

For each affected SWR-<n>:

```
grep -rn "SWR_<n>" Assets --include=*.cs
```

(Exclude `SWR.g.cs` itself.) Hits inside `[Traces(...)]` are implementation sites,
hits inside `[Verifies(...)]` are the covering tests. For a **removed** requirement
these hits are exactly the locations that no longer compile.

## Step 4 — Rework each implementation site

For every `[Traces(SWR.SWR_<n>)]` location, compare the current behaviour against
the new requirement text and change the implementation to match. Keep the
`[Traces]` attribute on whatever code implements the requirement afterwards; if
code is deleted or moved, move the attribute with the behaviour. Follow the
existing code style of the file.

## Step 5 — Update or create tests, then run them

Adjust the `[Verifies(SWR.SWR_<n>)]` tests in `Assets/Tests/EditMode/` so they
assert the **new** behaviour (a test that still passes without your implementation
change is a red flag). Create a new test if none exists. Edit Mode tests set up
Unity lifecycle wiring via reflection — copy the patterns in
`BarrierFeatureTests.cs` / `AlarmSystemTests.cs`.

Run the suite via the `run-unity-tests` skill (requires the Unity Editor to be
open):

```
powershell -NoProfile -File Tools/agent-tests/Invoke-AgentTests.ps1
```

The bridge regenerates traceables before testing, so `ReqToCodeTests` also
re-checks traceability. Iterate on steps 4–5 until exit code 0.

## Step 6 — Final gate and commit

1. `powershell -NoProfile -File Tools/reqtocode/Check-ReqToCode.ps1` must exit 0.
2. Stage the requirement document(s), `SWR.g.cs`, the reworked code, and the tests
   together — they are one logical change.
3. Commit with a message that names the requirement, e.g.
   `feat(requirements): propagate SWR-<n> <short summary>`. The pre-commit hook
   runs the same check; if it rejects the commit, a violation slipped through —
   go back to step 2, do not bypass the hook (`--no-verify` is not an option).

Done means: requirement document, generated traceables, implementation, and tests
all tell the same story, the full Edit Mode suite passes, and the change is
committed as one unit.
