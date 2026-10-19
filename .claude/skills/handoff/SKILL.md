---
name: handoff
description: Write or update a handoff document so the next agent with fresh context can continue this work.
---

Write or update a handoff document so the next agent with fresh context can continue this work.

Steps:
1. Check if HANDOFF.md already exists in the project
2. If it exists, read it first to understand prior context before updating
3. Create or update the document with:
   - **Goal**: What we're trying to accomplish
   - **Current Progress**: What's been done so far
   - **What Worked**: Approaches that succeeded
   - **What Didn't Work**: Approaches that failed (so they're not repeated)
   - **Next Steps**: Clear action items for continuing

Save as HANDOFF.md in the project root and tell the user the file path so they can start a fresh conversation with just that path.

What made a handoff executable without the author (2026-10-19, JRSNAP equivalence):

- **Verified facts with the command and date** that produced them (how to run the
  external tool, baseline test counts, which failures are known and why), kept
  apart from assumptions.
- **Numbered acceptance criteria** naming the exact gate command, the baseline to
  compare with, and the artifacts to produce (record path, test files, docs).
- **Explicit prohibitions** (no expected values from the product under test, fix
  tolerances before comparing, do not touch `C:\RTRI_Program`, do not recreate a
  removed directory) and the environment rules (PowerShell from the root, `uv`).
- **Scope questions for the user** listed up front so the next agent asks them
  before planning, and an instruction to ask before committing.
- **What is out of scope** by name, so partial work is reported as such instead
  of being quietly widened.
