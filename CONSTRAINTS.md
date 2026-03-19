# CobolSharp Modernization — Constraints and Anti-Pattern Catalog

This document captures the full set of anti-patterns, migration phases, process rituals,
and behavioral constraints governing the modernization effort. Referenced from PROMPT.md.

---

## Anti-Pattern Catalog (Non-Exhaustive)

You must actively search for and correct anti-patterns. The list below is illustrative,
not exhaustive — you must also identify and fix any other anti-patterns you find.

### Architectural and Layering Anti-Patterns

- **God objects / God classes** `[GodObject]`
  Classes that do too many things (e.g., mixing parsing, semantic analysis, and codegen).
  Action: Split into focused components with clear responsibilities and interfaces.

- **Leaky abstractions and cross-layer reach-through** `[LayerViolation]`
  Lower layers depending on higher layers (e.g., runtime knowing about parser internals).
  Action: Enforce strict layering (Lexer -> Parser -> Semantic Model -> IR -> Codegen -> Runtime).

- **Hidden global state / singletons / static mutable state** `[GlobalState]`
  Static mutable fields, implicit global configuration, or shared mutable caches.
  Action: Replace with explicit configuration objects, dependency injection, or immutable data.

- **Ad-hoc feature flags and scattered dialect checks** `[ScatteredFlags]`
  Random `if (isCobol80)` or similar checks scattered across the codebase.
  Action: Centralize dialect and feature gating in a dedicated configuration or environment object.

### Code-Level Anti-Patterns

- **Deeply nested conditionals and switch pyramids** `[DeepNesting]`
  Hard-to-follow logic with many nested if/else or switch statements.
  Action: Refactor into smaller methods, pattern matching, or data-driven tables.

- **Copy-paste logic / duplicated code** `[Duplication]`
  Repeated logic across modules (e.g., numeric formatting, PIC parsing, control flow lowering).
  Action: Extract shared helpers or canonical implementations.

- **Primitive obsession** `[PrimitiveObsession]`
  Using raw string, int, bool instead of domain types (e.g., PIC descriptors, token kinds, numeric formats).
  Action: Introduce domain-specific types and enums.

- **Magic constants and undocumented invariants** `[MagicValues]`
  Unexplained numeric or string constants, or implicit assumptions.
  Action: Replace with named constants, enums, or documented configuration.

- **Overuse of null and weak null-safety** `[NullHazard]`
  Unclear nullability, frequent null checks, or null-driven control flow.
  Action: Use nullable reference types, clear contracts, and avoid null where possible.

- **Exception misuse** `[ExceptionMisuse]`
  Using exceptions for control flow or swallowing exceptions silently.
  Action: Use explicit result types, error objects, or diagnostics pipelines.

- **Tight coupling to I/O or environment** `[IOBinding]`
  Compiler logic directly reading/writing files, console, or environment.
  Action: Abstract I/O behind interfaces; keep core compiler logic pure and testable.

### Performance and Memory Anti-Patterns

- **Excessive allocations in hot paths** `[HotAlloc]`
  Repeated string concatenations, boxing, or temporary collections in tight loops.
  Action: Use Span<T>, pooling, or more efficient data structures where appropriate.

- **Inefficient data structures** `[DataStructureMisfit]`
  Using List<T> or Dictionary<TKey,TValue> where a more suitable structure exists.
  Action: Choose structures based on access patterns and complexity.

- **Unbounded caches or collections** `[UnboundedGrowth]`
  Collections that grow without clear limits or eviction policies.
  Action: Add bounds, eviction, or redesign to avoid unbounded growth.

---

## Migration Phases

### Phase 1: Project and Build Modernization
- Update target frameworks to net9.0
- Update C# language version to 13
- Ensure dependencies are compatible
- Introduce nullable reference types if not already enabled
- Introduce central package management
- Add global.json to pin SDK
- Establish baseline test runs

### Phase 2: Lexer and Tokenization
- Remove ad-hoc tokenization logic
- Introduce clear token types and domain types
- Use modern C# features where appropriate
- Ensure tests for lexical edge cases

### Phase 3: Parser and Grammar
- Clarify grammar representation (recursive descent, parser combinators, or table-driven)
- Remove deeply nested conditionals and magic constants
- Centralize grammar rules and error handling

### Phase 4: Semantic Model and Symbol Tables
- Introduce or refine symbol tables, type systems, and semantic passes
- Remove global state and implicit context
- Make semantic invariants explicit and testable

### Phase 5: IR and Lowering
- Define a clear IR model
- Centralize lowering patterns (control flow, arithmetic, PIC handling)
- Remove duplication and scattered lowering logic

### Phase 6: Code Generation and Runtime
- Cleanly separate codegen from runtime
- Use modern C# features in runtime types
- Optimize hot paths and memory usage where justified

### Phase 7: Numeric, PIC, and Editing Subsystems
- Centralize PIC parsing and formatting
- Remove duplicated numeric logic
- Ensure behavior is spec-true and well-documented

### Phase 8: Diagnostics, Logging, and Tooling
- Standardize diagnostic reporting
- Remove ad-hoc logging
- Provide structured, testable diagnostics

### Phase 9: Final Consolidation and Cleanup
- Sweep for remaining anti-patterns
- Normalize naming, documentation, and structure
- Ensure the Migration Ledger reflects a stable, long-term architecture

---

## Session Rituals

### Session-Start Ritual
At the start of every session:
1. Load and summarize the Migration Ledger
2. Identify: current phase, last session's focus, outstanding TODOs, known regressions
3. Produce a brief summary: current phase/status, key decisions, top 3-5 TODOs
4. Confirm scope with user (phase, files/modules, constraints)
5. Restate goals and commit to maintaining test passing status and updating the ledger

### Session-End Ritual
At the end of every session:
1. Summarize changes: files touched, anti-patterns addressed, key refactors, new invariants
2. Report test status: which tests ran, pass/fail, regressions and handling
3. Update the Migration Ledger: append session log, update phase status, update TODOs
4. Propose next steps: prioritized list tied to current phase and remaining anti-patterns

---

## Behavioral Constraints

- Stay focused on: modernization, architectural clarity, anti-pattern removal, test-driven staged migration
- Avoid: unrelated tangents, speculation not grounded in the codebase
- Explain decisions: what was wrong, what was done, why the new approach is better
- Ask targeted clarification questions when ambiguity or missing context exists
- Propose concrete options with tradeoffs rather than making silent assumptions
- Make changes diff-friendly and attributable to specific sessions
