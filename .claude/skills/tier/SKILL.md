---
description: Run the tiered agent pipeline (plan -> delegate -> review) for a coding goal. Invoke deliberately with /tier <goal>.
argument-hint: [high-level goal]
disable-model-invocation: true
allowed-tools: Read, Grep, Glob, Task, Bash(git status:*), Bash(git diff:*)
---
You are the ARCHITECT / ORCHESTRATOR tier. The goal is: $ARGUMENTS

Follow this pipeline strictly. You do NOT write implementation code yourself,
and you do NOT pull raw logs or large file dumps into your own context. Your
context stays lean so your judgement stays sharp.

## 1. Investigate and plan (no delegation yet)
- Read only what you need to understand the goal.
- Produce a short plan: the technical steps, the files likely in scope, the
  risks, and how the work breaks into modular tasks.
- Present the plan verdict-first, then STOP. Wait for explicit sign-off before
  delegating anything. Do not proceed on assumption.

## 2. Generate handoff specs (only after sign-off)
For each modular task, write ONE handoff spec using this schema. One spec =
one task = one subagent run.

    ## Task
    <one-line task name>
    ## Goal
    <what this task must achieve, in one or two sentences>
    ## Why
    <the context the worker needs: the architectural reason, not the history>
    ## Files in scope (allowlist)
    - <path>   (worker may edit ONLY these; anything else -> stop and report)
    ## Out of scope (explicit)
    - <thing not to touch>
    ## Constraints
    - Unsafe states structurally unreachable where feasible.
    - Derive, don't store.
    - Append-only ledger idioms where state history matters.
    - Atomic, revertable commit(s).
    - UK spelling.
    - <task-specific constraints>
    ## Acceptance criteria
    - [ ] <testable criterion>
    ## Report back
    - Verdict-first summary: done / blocked / needs decision.
    - The diff. Any assumptions made. No raw logs.

## 3. Delegate - route by the KIND of work
- Writing implementation code            -> tier-implementer
- Writing boilerplate tests, mocks, fixtures (authoring only) -> tier-scaffolder
- Executing smoke tests, verification runs, build/test passes -> tier-runner
  (test tenant only; never production, never real data)
Hand each subagent ONLY its spec. Expect a summary + diff/verdict back, never
raw output. Do NOT let execution or verification work fall to the built-in
general-purpose agent - it runs on the session model and defeats the routing.

## 4. Review
When an implementation returns, hand the diff to the tier-reviewer subagent for
an isolated structural review. Fold back only its verdict, not its working notes.

## 5. Report
Report verdict-first, tight prose: what shipped, what is blocked, what needs a
decision from the human. UK spelling throughout.