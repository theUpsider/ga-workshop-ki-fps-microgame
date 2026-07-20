@docs/ai/testing.md

# Repository agent instructions

Before modifying Unity C# code, scenes, prefabs, packages, assembly definitions
or project settings, read:

- docs/ai/testing.md

## Test-driven development

For every testable behavior change or bug fix, use red-green-refactor:

1. **Red:** Write the smallest automated test that expresses the required observable
   behavior, then run it and confirm that it fails for the expected reason.
2. **Green:** Write only enough production code to make that test pass.
3. **Refactor:** Improve the code and tests without changing behavior, rerunning the
   focused tests and the required regression suite.

Tests must be deterministic, independent, automated and clear when they fail. Cover
relevant boundaries and error cases. Prefer the narrowest scope that proves the behavior,
but add integration or acceptance coverage when components or requirements cross
boundaries; unit tests do not replace those tests. For untested legacy behavior, add a
characterization test before changing it. If a test cannot reasonably be automated,
document why and identify the required manual validation; do not count a manual check as
an automated test.

Run all Unity tests through:

```
python tools/unity_test.py ...
```

This command is required for every TDD stage, focused rerun and regression run.

Never invoke the Unity editor directly.
Never report a task as validated when required tests were not executed or failed.
