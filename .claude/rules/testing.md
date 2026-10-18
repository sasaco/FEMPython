# Testing Rules

## Principles

- Prefer a failing regression test before a behavior fix.
- Test the changed component at the narrowest useful scope, then run its
  integration/build gate when risk warrants it.
- Cover happy paths, boundaries, malformed input, and error propagation that
  matter to the changed contract.
- Keep tests deterministic and independent of execution order.
- Mock network, filesystem, clock, or process boundaries only when isolation is
  part of the test's purpose.
- Do not invent a repository-wide coverage threshold or weaken existing tests.

## Canonical Commands

Run from the repository root in PowerShell:

```powershell
# Python
uv --directory FrameWeb run --locked --extra dev python -m pytest tests -q
uv --directory FrameWeb run --locked --extra dev python -m pytest tests/path/test_file.py -q

# Angular
npm --prefix FrameWebforJS run test -- --watch=false --browsers=ChromeHeadless
npm --prefix FrameWebforJS run build

# .NET
dotnet test FrameWeb.sln
dotnet build FrameWeb.sln

# Agent infrastructure
& .agents/check.ps1
```

Run only gates relevant to the change unless a plan or release gate explicitly
requires the full matrix. Report environment failures separately from product
regressions.
