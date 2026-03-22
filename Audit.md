You are acting as a senior compiler architect performing a full, in‑depth audit of the COBOL compiler implementation in this repository (CobolSharp). Treat this as a production‑grade, spec‑true COBOL‑85 compiler that must be hardened for real‑world use.

Your job has two parts:

Exhaustive analysis and gap identification

A binding, implementation‑ready remediation plan that future Claude sessions must follow without deviation

1. Scope and expectations
Perform a repository‑wide analysis covering at least:

Language coverage:

Grammar (COBOL‑85, including all divisions, sections, paragraphs, statements, clauses, and data types)

Parsing

Semantic analysis

Name/binding resolution

Control‑flow and data‑flow modeling

Lowering / IR design

CIL (or other backend) emission

Runtime / library dependencies

Code quality and architecture:

Non‑production‑quality constructs (ad‑hoc hacks, partial implementations, TODOs, speculative code, dead code)

Duplicative logic (same behavior implemented in multiple places)

Meaningless wrappers that simply delegate to another method without adding semantics, invariants, or abstraction value

Leaky abstractions and cross‑layer violations (e.g., parser knowing about backend details)

Inconsistent naming, layering, or responsibility boundaries

Error handling (diagnostics quality, missing checks, silent failures)

Test coverage (unit, integration, conformance tests, negative tests)

COBOL‑85 feature gaps:
For each of these, identify whether support is:

Missing entirely

Partially implemented

Implemented but incorrect or non‑spec‑true

Include, at minimum:

Divisions/sections/paragraphs: IDENTIFICATION, ENVIRONMENT, DATA, PROCEDURE; file sections; working‑storage; linkage; local‑storage; report section if applicable.

Data description: PIC clauses, USAGE, SIGN, JUSTIFIED, OCCURS, REDEFINES, RENAMES, VALUE, 88‑level condition names, alignment and storage semantics.

Numeric behavior: DISPLAY, COMP, COMP‑3, COMP‑5, binary/packed/decimal semantics, rounding, truncation, overflow behavior.

Control flow: PERFORM variants (inline, out‑of‑line, THRU, VARYING), GO TO, ALTER (if supported), fall‑through rules, section/paragraph entry/exit semantics.

Expressions and statements: arithmetic, relational, conditional expressions, IF/ELSE, EVALUATE, MOVE, STRING/UNSTRING, INSPECT, CALL, EXIT, STOP RUN, GOBACK, etc.

File I/O: SELECT/ASSIGN, ORGANIZATION, ACCESS MODE, RECORD KEY, ALTERNATE KEY, OPEN/CLOSE, READ/WRITE/REWRITE/DELETE, file status handling.

CALL/USING/RETURNING: parameter passing, BY REFERENCE / BY CONTENT / BY VALUE, linkage section semantics, returning values, interop expectations.

Diagnostics and conformance: how well errors map to COBOL‑85 rules; missing or misleading diagnostics.

2. Analysis deliverables
Produce a structured, concrete analysis, not general commentary. At minimum, deliver:

High‑level architecture map

List the main subsystems (grammar, parser, binder/semantic analyzer, IR/lowering, backend/CIL emitter, runtime).

For each subsystem, describe:

Responsibilities

Key types/classes

How control and data flow between subsystems

COBOL‑85 feature coverage matrix

Create a table‑like description in text where each row is a feature or construct (e.g., “PERFORM VARYING”, “OCCURS with DEPENDING ON”, “CALL USING BY CONTENT”, “COMP‑3 arithmetic”).

For each feature, specify:

Status: Not implemented, Partially implemented, or Implemented

Where implemented: file(s)/type(s)/method(s)

Quality: Spec‑true, Likely incorrect, or Unknown

Notes: brief explanation of gaps or concerns

Non‑production‑quality and duplication findings

Identify:

Meaningless wrappers: methods that simply delegate without adding invariants, validation, or abstraction value. For each, state:

Method name and location

What it delegates to

Why it should be inlined, removed, or given real responsibility

Duplicated logic: similar or identical code in multiple places (e.g., repeated parsing patterns, repeated semantic checks, repeated lowering logic). For each duplication cluster:

List locations

Describe the shared behavior

Propose a single canonical abstraction and where it should live

Ad‑hoc or hacky code paths: places where behavior is hard‑coded, special‑cased, or clearly incomplete. For each:

Location

What it currently does

What a production‑quality version should do

Validator and diagnostic gaps

Identify missing or weak validation in:

Symbol/binding validation

Control‑flow validation

File I/O validation

CALL/USING/RETURNING validation

For each gap:

Describe the COBOL‑85 rule that should be enforced

Point to the closest existing code that should own the check (or propose a new validator component)

Suggest diagnostic IDs/messages and where they should be raised

Test suite and conformance gaps

Describe the current test structure (unit tests, integration tests, sample programs).

Identify:

Missing test categories (e.g., no negative tests for invalid PERFORM ranges, no tests for COMP‑3 overflow).

Missing conformance coverage for COBOL‑85 constructs.

Propose a canonical test layout (e.g., by division, by feature, by numeric type, by statement kind) and how tests should be named and organized.

3. Binding remediation and implementation plan
You must now produce a detailed, directive, multi‑phase plan to bring this compiler to production‑quality, spec‑true COBOL‑85. This plan must be so explicit that future Claude sessions cannot reasonably deviate from it.

Structure the plan as phases, each with numbered tasks. For each task, include:

Task ID: a stable identifier (e.g., GRA-01, SEM-07, IO-03).

Goal: one sentence describing the outcome.

Scope: what files/subsystems are in play.

Required changes: concrete, imperative instructions (e.g., “Refactor X into Y”, “Introduce new enum Z with members A/B/C”, “Add validator method Foo.ValidatePerformRange(...) and call it from Bar.BindPerform(...)”).

Acceptance criteria: what must be true and testable when the task is complete (including specific test additions or updates).

Your plan must cover at least:

Grammar and parsing hardening

Add missing COBOL‑85 constructs to the grammar.

Remove or refactor ambiguous or ad‑hoc productions.

Ensure the parser produces a stable, well‑typed AST that can represent all COBOL‑85 constructs needed for production use.

Semantic analysis and binding

Introduce or refine symbol tables, scopes, and binding rules for sections, paragraphs, data items, and procedures.

Implement or fix CALL/USING/RETURNING binding, including parameter modes and linkage section semantics.

Add validator components (or strengthen existing ones) for:

Symbol correctness

Control‑flow correctness

File I/O correctness

Numeric usage and PIC/USAGE consistency

Lowering and IR design

Define a canonical IR (or refine the existing one) that:

Represents COBOL control flow (PERFORM, GO TO, sections/paragraphs) unambiguously.

Encodes data layout and numeric semantics sufficiently for correct backend emission.

Remove duplicated lowering logic and centralize transformations in a small number of well‑defined passes.

Backend / CIL emission

Ensure that:

Control flow in emitted code matches COBOL‑85 semantics (especially PERFORM and section/paragraph entry/exit).

Numeric operations respect COBOL‑85 rules for each USAGE and PIC.

File I/O and CALL semantics are correctly mapped to the runtime/host environment.

Diagnostics and test suite

Define a diagnostic catalog with stable IDs and clear messages.

For each major COBOL‑85 rule, ensure there is:

A validator enforcing it.

At least one positive test (valid program) and one negative test (invalid program with expected diagnostic).

Organize tests into a conformance matrix that can be extended over time.

4. Constraints for future sessions
When you produce the remediation plan:

Write it so that each task can be implemented independently in future sessions, without re‑designing the architecture.

Avoid vague instructions like “clean up code” or “improve design”. Every task must be concrete and actionable.

Explicitly state that future Claude sessions must:

Follow the plan task‑by‑task.

Not introduce alternative architectures unless the plan itself is updated in a clearly marked “Plan Revision” section.

Always update the coverage matrix and task status (e.g., Planned, In progress, Done) as tasks are completed.

At the end of your response, output:

The coverage matrix (even if partial).

The full remediation plan with task IDs and acceptance criteria.

A short “Execution Protocol for Future Sessions” section that tells future Claude instances exactly how to proceed:

“Read the plan”

“Pick the next Planned task”

“Implement it strictly as specified”

“Update tests and status”

Do not skip any of these deliverables.
