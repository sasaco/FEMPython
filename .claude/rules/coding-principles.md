# Coding Principles

Cross-language rules for the Python, TypeScript, and C# components.

## Prefer Clarity

- Choose readable, local solutions over speculative abstractions.
- Keep each function or class focused on one responsibility.
- Follow the style already established in the component being changed.
- Use early returns when they reduce nesting without hiding control flow.

## Preserve Contracts

- Identify public, serialized, and cross-process boundaries before changing
  them. Maintain backward compatibility unless the task approves a migration.
- Use Python type hints, TypeScript types, and C# nullable annotations at public
  or non-obvious boundaries.
- Validate external input and make failure states explicit.

## Minimize Mutation and Duplication

- Prefer immutable values at shared/stateful boundaries, but do not clone large
  objects without a correctness reason.
- Reuse existing helpers when their contract matches. Do not create a generic
  abstraction for a single call site.
- Name constants for domain values; avoid unexplained literals.

## Keep Diffs Reviewable

- Change only files required by the task.
- Do not mix formatting churn, dependency upgrades, or generated output into a
  behavior change unless required.
- Never weaken tests or swallow failures to obtain a green result.
