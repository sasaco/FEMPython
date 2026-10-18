# Delegation and Collaboration Rule

Codex is the main agent. It owns task understanding, authority boundaries,
integration, verification, and the final user response.

## When to Collaborate

Use native collaboration when the user or an active workflow calls for agent
teams, or when the task naturally contains independent owned workstreams. Keep
simple edits, named commands, and user interaction with the lead.

Good collaboration candidates include:

- independent implementation modules with disjoint file ownership;
- code investigation and impact analysis that can run in parallel;
- specialized security, quality, and test-coverage review;
- a separately bounded research or reproduction task.

Do not delegate a decision that belongs to the user, and do not spawn a worker
without a concrete bounded task.

## Route by Capability

Choose available collaborators by capability rather than copied runtime-specific
names:

| Work | Capability |
|---|---|
| Routine scoped implementation | implementation worker |
| Architecture, planning, algorithms, or complex review | high-reasoning Codex worker or `codex-system` consultation |
| Unknown root cause | debugging/investigation worker |
| External research | research worker with web access |
| Ship review | independent security, quality, and test reviewers |

If a route is unavailable, continue with Codex using the same scope and checks;
do not assume a plugin or named model exists.

## Prompt Contract

Every delegated task states:

1. Objective.
2. Owned paths and explicit exclusions.
3. Relevant inputs and rules.
4. Read/write authority.
5. Exact acceptance checks.
6. Required output shape and durable artifact path.

Run independent units concurrently only when ownership is disjoint. The lead
verifies returned work and inspects the combined diff before reporting success.
