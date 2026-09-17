Use instructions in `AGENTS.md`

## Agents

Do not delegate work to subagents (the Agent/Task tool) — they consume tokens
at a high rate relative to the work done. Do the work directly in this
session instead.

If a task is large, warn the user up front so they can switch to a weaker
model if needed, then chunk the work and check in with the user for
verification as you progress, rather than running it all unsupervised in one
pass.
