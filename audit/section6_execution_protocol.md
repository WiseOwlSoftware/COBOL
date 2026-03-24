# Section 6: Constraints and Execution Protocol for Future Sessions

## Overview

This document governs how future Claude sessions interact with the CobolSharp codebase. It
exists because the project spans many sessions, and without rigid process discipline, sessions
drift, contradict earlier decisions, introduce regressions, or duplicate work. Every rule below
was derived from patterns observed across 13+ sessions of building this compiler.

The project has completed its 9-phase modernization (see MIGRATION_LEDGER.md) and is now in
active feature development on the `main` branch, with 31 NIST tests at 100% (in guard script) and known
gaps documented in CLAUDE.md. The rules below apply to all future work: new features, bug fixes,
NIST test expansion, and any further modernization.

---

## Part A: Constraints for Future Sessions

### A1. Task Independence

Each task must be implementable independently without re-designing architecture.

- A task is a single, well-scoped unit of work: fix a bug, add support for a COBOL statement,
  wire a diagnostic, pass a NIST test, modernize a specific file.
- A task must not require changes to the compiler pipeline architecture (lexer/parser/binder/IR/
  codegen layering) to complete. If it does, that is a plan revision, not a task.
- A task must not introduce new abstractions that span multiple compiler phases unless the plan
  explicitly calls for it.
- A task must not remove or rename public APIs used by other compiler phases unless the plan
  explicitly calls for it.

### A2. Sequential Task Execution

Future Claude sessions must follow the plan task-by-task.

- Read CLAUDE.md, PROMPT.md, and CONSTRAINTS.md at session start. Always.
- Pick the next Planned task from the tracking table (see Part B, Section B8).
- Do not skip tasks. Do not reorder tasks unless a dependency requires it (and document why).
- Do not work on tasks that are already marked Done.
- Do not work on more than one task at a time unless they are tightly coupled (e.g., a bound
  node and its unit test).

### A3. No Alternative Architectures

No alternative architectures unless the plan itself is updated in a "Plan Revision" section.

- The compiler architecture is: Preprocess -> Lex/Parse -> SemanticModel -> Validate -> Bind ->
  IR Lower -> CIL Emit. This pipeline is settled.
- The layering is: Common, Diagnostics, Preprocessing, Parsing, Semantics, Semantics/Bound,
  IR, FlowAnalysis, CodeGen, Runtime, CLI.
- If a task seems to require architectural change, STOP. Document the conflict in DEVLOG.md
  under a "Plan Revision Proposal" heading. Do not implement the change in the same session.
  The next session must evaluate the proposal before proceeding.
- Never reintroduce anti-patterns that were eliminated in Phases 1-9 of the modernization
  (see MIGRATION_LEDGER.md for the full list).

### A4. Coverage Matrix and Status Tracking

Always update the coverage matrix and task status.

- CLAUDE.md "Current State" section must reflect the actual test counts after every session.
- The NIST test list in CLAUDE.md must be updated when tests newly pass at 100%.
- Known gaps in CLAUDE.md must be updated when gaps are closed or new gaps are discovered.
- DEVLOG.md must receive a new entry for every session (see Section B5).
- MIGRATION_LEDGER.md is updated only if modernization-related changes are made.

### A5. Regression Gates

No session may end with fewer passing tests than it started with.

- Run `dotnet test` for both unit and integration projects before and after changes.
- If any test fails after a change, fix the regression before proceeding to other work.
- If a regression cannot be fixed in the current session, revert the change that caused it.
  Document the failure in DEVLOG.md. Do not leave the codebase in a broken state.
- Never skip tests. Never use `[Skip]` to hide failures. Never comment out assertions.

### A6. Frozen Interfaces

The following interfaces and signatures are frozen and must not be changed without a Plan
Revision:

- `PicDescriptor` 17-parameter constructor (used by CIL newobj emission)
- `IFileHandler` interface methods
- `AcceptRuntime.Accept` signature (AcceptSourceKind parameter)
- `FigurativeKind` enum values (cast to int at CIL boundary)
- Generated parser files (`CobolLexer.cs`, `CobolParserCore.cs`) -- only regenerated from
  `.g4` grammar changes

---

## Part B: Execution Protocol for Future Sessions

### B1. Read the Plan

At the start of every session, before writing any code:

1. Read `CLAUDE.md` -- contains current branch, test counts, known gaps, last session summary.
2. Read `PROMPT.md` -- contains architectural doctrine and development rules.
3. Read `CONSTRAINTS.md` -- contains anti-pattern catalog, migration phases, session rituals.
4. Read `DEVLOG.md` (latest 2-3 entries) -- contains recent decisions, failures, rationale.
5. Read `PROJECT_PLAN.md` (relevant sections) -- if the task touches a specific COBOL feature,
   read the corresponding phase section.

Produce a brief summary stating:
- Current branch and test counts
- What the last session accomplished
- What gaps remain
- What the current session will work on

### B2. Pick the Next Planned Task

Consult the task tracking table (Section B8) or CLAUDE.md "Next" field.

- If the table has a task marked "Planned", pick the first one.
- If CLAUDE.md specifies a "Next" item, that takes priority.
- If the user provides explicit instructions, those override both.
- Never invent tasks that are not in the plan or requested by the user.

### B3. Implement Strictly as Specified

For each task:

1. **Analyze first.** Read the relevant source files. Understand the existing patterns.
   Follow the conventions already established (naming, file organization, test structure).

2. **Bound nodes.** If the task requires a new statement type:
   - Add `BoundXxxStatement` to `Semantics/Bound/BoundNodes.cs`
   - Add binder method in `Semantics/Bound/BoundTreeBuilder.cs`
   - Add validation in `Semantics/Bound/BoundTreeValidator.cs`
   - Add IR lowering in `CodeGen/Binder.cs`
   - Add CIL emission in `CodeGen/CilEmitter.cs` (or IR stub if full codegen is deferred)

3. **Diagnostics.** If the task requires a new diagnostic:
   - Add the code to `Diagnostics/DiagnosticDescriptors.cs`
   - Wire it through the appropriate validator
   - Add a unit test that triggers the diagnostic

4. **Tests.** Every change must have corresponding test coverage:
   - Unit tests for diagnostics, validation logic, and semantic rules
   - Integration tests for end-to-end compile-and-run scenarios
   - NIST tests are not written -- they are run against existing `.cob` files

5. **No partial implementations.** If a task cannot be completed fully, implement a stub that:
   - Emits a DISPLAY message at runtime (e.g., "CALL not implemented")
   - Takes the exception/error path if one exists
   - Documents the gap in CLAUDE.md "Known gaps"

### B4. Update Tests and Status

After implementing a task:

1. Run all unit tests: `dotnet test src/CobolSharp.Tests.Unit/`
2. Run all integration tests: `dotnet test src/CobolSharp.Tests.Integration/`
3. If NIST tests are relevant, run the NIST guard script or individual tests.
4. Record the counts: X unit pass, Y integration pass, Z skip.
5. Compare against the session-start counts. If any test regressed, fix it before proceeding.

### B5. Session Start and End Rituals

#### Session Start

1. Read the documents listed in B1.
2. State: branch name, unit test count, integration test count, NIST pass count.
3. State: what the last session did (from CLAUDE.md or DEVLOG.md).
4. State: what this session will do.
5. Confirm with the user before making changes.

#### Session End

1. Run all tests one final time. Record counts.
2. Update `CLAUDE.md`:
   - "Current State" section: branch, test counts, NIST list.
   - "What was done this session": bullet list of changes.
   - "Key architectural decisions": any non-obvious choices made.
   - "Known gaps": add new gaps, remove closed gaps.
   - "Next": what the next session should do.
3. Add a new entry to `DEVLOG.md` describing what was done and why.
4. Commit all changes (code + documentation) with a descriptive message.

### B6. How to Handle Blockers or Plan Deviations

**If a task is blocked by a grammar gap:**
- Document the gap in CLAUDE.md "Known gaps" with a note like "grammar gap -- needs .g4 change".
- Implement as much as possible (bound node, validator, IR stub) without the grammar fix.
- Mark the task as "Blocked" in the tracking table with a reason.
- Move on to the next task.

**If a task requires architecture changes:**
- STOP. Do not implement the architecture change.
- Write a "Plan Revision Proposal" entry in DEVLOG.md explaining:
  - What the task requires
  - Why the current architecture cannot support it
  - What the proposed change is
  - What the impact would be (files, tests, other tasks)
- Mark the task as "Blocked -- Plan Revision Required".
- Move on to the next task.

**If a test unexpectedly fails:**
- Diagnose the root cause before proceeding.
- If the failure is in the current task's code, fix it.
- If the failure is in pre-existing code exposed by the current task, fix the pre-existing
  bug and note it in DEVLOG.md.
- If the failure cannot be resolved, revert the current task's changes and mark it as Blocked.

**If the user changes priorities mid-session:**
- Update the tracking table to reflect the new priority.
- Complete or revert the current in-progress task to a clean state.
- Begin the user-requested task.

### B7. How to Update the Coverage Matrix

The coverage matrix is the NIST test list in CLAUDE.md "Current State".

**When a new NIST test passes at 100%:**
1. Add its ID to the sorted list in CLAUDE.md (maintain alphabetical/numerical order).
2. Increment the count in parentheses (e.g., "(31 tests)" -> "(32 tests)").

**When a NIST test regresses:**
1. Remove it from the list.
2. Decrement the count.
3. Add it to "Known gaps" with a brief explanation.

**When a known gap is closed:**
1. Remove it from "Known gaps".
2. If it was a NIST test, add it to the passing list.

### B8. How to Mark Tasks as Done

In the tracking table below:

1. Change the status from "Planned" or "In Progress" to "Done".
2. Add the session date in the "Completed" column.
3. Add a one-line summary of what was done.

If a task turns out to be unnecessary (e.g., already implemented, or superseded), mark it
as "Skipped" with a reason.

---

## Task Status Tracking Template

Future sessions should maintain this table. Copy it into CLAUDE.md or a dedicated tracking
file and update it as tasks are completed.

```
| ID     | Task                                      | Status   | Completed  | Notes                                    |
|--------|-------------------------------------------|----------|------------|------------------------------------------|
| T-001  | Fix NC121M subscripted DIVIDE GIVING      | Done     | 2026-03-20 | DEVLOG Entry 126                         |
| T-002  | Fix NC220M infinite loop at runtime       | Planned  |            | Likely ODO/subscript issue               |
| T-003  | NC252A qualified RENAMES                  | Partial  |            | Compiles; 3 sub-tests fail               |
| T-004  | NC233A OCCURS key + validation            | Done     | 2026-03-24 | 100% — added to guard suite              |
| T-005  | STATUS/PROGRAM keyword conflicts          | Done     | 2026-03-22 | DEVLOG Entry 135-136                     |
| T-006  | CALL IR implementation (inter-program)    | Done     | 2026-03-23 | Full CALL/USING/RETURNING/ENTRY/CANCEL   |
| T-007  | SORT/MERGE full IR implementation         | Planned  |            | Currently parse only                     |
| T-008  | Alternate keys (ISAM)                     | Done     | 2026-03-23 | Parsed, stored, runtime indices          |
| T-009  | Condition-name conditions                 | Planned  |            | IF switch-condition (NC211A, NC254A)     |
| T-010  | ODO runtime truncation                    | Planned  |            | SEARCH/compare respect active count      |
| T-011  | Collating sequence (ALPHABET)             | Planned  |            | NC215A, NC219A                           |
```

### Column definitions

- **ID**: Sequential task identifier (T-NNN). Never reuse IDs.
- **Task**: Brief description of what needs to be done.
- **Status**: One of: `Planned`, `In Progress`, `Done`, `Blocked`, `Skipped`.
- **Completed**: Date the task was marked Done (YYYY-MM-DD).
- **Notes**: Blocking reason, affected tests, or completion summary.

### Rules for the tracking table

1. Only one task may be "In Progress" at a time.
2. A task moves to "Done" only when tests pass and documentation is updated.
3. New tasks are appended at the bottom with the next available ID.
4. Never delete rows. Use "Skipped" for tasks that become unnecessary.
5. When a task is blocked, the "Notes" column must explain why.
6. The table is the single source of truth for what has been done and what remains.

---

## Summary of Non-Negotiable Rules

1. Read CLAUDE.md, PROMPT.md, CONSTRAINTS.md before every session.
2. Pick the next Planned task. Do not invent work.
3. Implement exactly what is specified. No speculative features.
4. Run all tests before and after. No regressions allowed.
5. Update CLAUDE.md and DEVLOG.md at session end. Every time.
6. No architecture changes without a Plan Revision Proposal.
7. No frozen interface changes without a Plan Revision.
8. One task in progress at a time. Complete or revert before starting another.
9. Document blockers explicitly. Do not silently skip tasks.
10. The tracking table is the source of truth. Keep it current.
