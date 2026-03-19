C# 12+, .NET 9, Multi‑Session Continuity, Explicit Anti‑Patterns
You are tasked with performing a full‑scale, multi‑stage modernization of an existing COBOL‑80 compiler codebase written in C#.
Your mission is to transform the entire codebase into the cleanest, most maintainable, most comprehensible, most efficient, and most production‑quality compiler implementation possible, suitable for decades of maintenance.
This is not a compatibility‑preserving refactor.
This is a ground‑truth architectural audit and rewrite, performed in stages, with correctness verified at every step.
You must stay focused on this mission and never drift into unrelated tasks.

0. Multi‑Session Continuity Requirement (Critical)
This modernization will occur across multiple Claude sessions.
You must:
A. Persist the architectural plan across sessions
- Remember the long‑term goals
- Remember the modernization stages
- Remember the architectural direction
- Remember the constraints and principles
- Remember the current stage of the migration
- Remember what has been completed and what remains
B. Reconstruct context at the start of each new session
When a new session begins, you must:
- Re‑establish the modernization plan
- Re‑state the current stage
- Re‑state the next required actions
- Re‑state any pending regressions or unresolved issues
- Re‑state the architectural principles guiding the rewrite
C. Never regress or contradict earlier architectural decisions
- Never undo previous improvements
- Never reintroduce anti‑patterns
- Never forget the modernization direction
- Never revert to legacy constraints
D. Maintain a consistent, long‑term memory of the migration
Even if the user does not provide context, you must:
- Rebuild the plan
- Rebuild the stage
- Rebuild the architectural constraints
- Rebuild the modernization goals
- Rebuild the compiler design principles
This is essential for a multi‑session rewrite.

1. Core Mission
Analyze the entire C# source code base of the COBOL‑80 compiler.
Identify:
- Architectural anti‑patterns
- Code smells
- Redundant abstractions
- Overly complex or fragile logic
- Violations of single‑responsibility
- Leaky abstractions
- Inconsistent naming or layering
- Areas where modern compiler design patterns should replace ad‑hoc logic
- Any place where the code can be simplified, clarified, or made more robust
Your goal is to produce a modern, clean, principled compiler architecture that:
- Is easy to understand
- Is easy to maintain
- Is easy to extend
- Has clear, well‑defined boundaries between phases
- Uses canonical compiler patterns (AST, IR, lowering, passes, visitors, etc.)
- Eliminates unnecessary complexity
- Eliminates hacks, workarounds, and technical debt
- Uses modern C# language features (C# 12+)
- Targets .NET 9
- Has zero dead code
- Has zero duplicated logic
- Has zero hidden coupling
Backward compatibility is not required.
This is a new product, and you are free to redesign anything.

2. Required Architectural Principles
You must enforce:
- Single pipeline for PIC semantics
- Clear separation of compiler phases
- No semantic logic in the parser
- No codegen logic in semantic analysis
- No runtime logic in compile‑time structures
- No duplicated logic across passes
- No global state unless explicitly justified
- No hidden side effects
- No ad‑hoc string manipulation where structured data is appropriate
- No magic numbers or magic strings
- No deeply nested conditionals where pattern matching or polymorphism is appropriate
- No monolithic classes
- No “god objects”
- No circular dependencies
- No mutable shared state across phases
Replace anti‑patterns with:
- Clean, composposable abstractions
- Immutable data structures where appropriate
- Clear ownership and lifetime rules
- Canonical compiler patterns
- Well‑factored modules
- Strong typing
- Exhaustive pattern matching
- Declarative logic where possible
- Modern C# features (records, spans, switch expressions, primary constructors, required members, file‑scoped types, etc.)
- .NET 9 APIs and performance primitives

3. Explicit Anti‑Patterns to Seek and Eliminate
(This list is not exhaustive — you must eliminate all anti‑patterns you find.)
A. Architectural Anti‑Patterns
- Parser performing semantic analysis
- Semantic analysis performing codegen
- Runtime logic embedded in compile‑time structures
- IR that mirrors AST too closely (no lowering)
- IR that is too low‑level (no structured operations)
- Codegen that depends on AST shape
- Cyclic dependencies between compiler layers
- Global mutable state
- Static singletons
- “Manager” classes with unclear responsibilities
- “Kitchen sink” modules with mixed concerns
B. Code Smells
- Deeply nested if/else chains
- Switch statements that should be polymorphism
- Repeated string parsing
- Repeated PIC parsing logic
- Repeated MOVE semantics logic
- Repeated numeric formatting logic
- Repeated error‑handling logic
- Repeated symbol‑table lookups
- Repeated type‑checking logic
- Repeated codegen patterns
C. Data Structure Anti‑Patterns
- Dictionaries used as ad‑hoc structs
- Tuples used where named types are needed
- Arrays used where spans or slices are appropriate
- Mutable shared collections
- Unbounded lists that should be immutable
D. C# Anti‑Patterns
- Old C# patterns that should be replaced with:
- record / record struct
- required members
- primary constructors
- pattern matching
- switch expressions
- file‑scoped types
- Span<T> / ReadOnlySpan<T>
- Memory<T>
- using declarations
- async streams
- IAsyncEnumerable<T>
- sealed classes where appropriate
- static abstract members in interfaces
- source generators (if beneficial)
E. .NET Anti‑Patterns
- Manual buffer management where Span<T> is appropriate
- Manual string slicing where AsSpan() is appropriate
- Reflection where generic constraints or interfaces suffice
- Exceptions used for control flow
- Blocking I/O where async is appropriate
F. Testing Anti‑Patterns
- Tests that depend on global state
- Tests that depend on ordering
- Tests that depend on side effects
- Tests that do not cover error paths
- Tests that do not cover edge cases

4. Required Process: Staged Migration With Regression Gates
You must perform the modernization in stages, not all at once.
For each stage:
- Analyze the subsystem
- Identify anti‑patterns and architectural issues
- Propose the improvements
- Apply the changes
- Run all regression tests, integration tests, and NIST tests
- If any test fails, STOP
- Diagnose and fix the regression
- Only proceed when the codebase is fully green
You must not proceed to the next stage until the current stage is 100% correct.
You must never introduce new regressions.
You must never skip tests.
You must never hand‑wave correctness.

5. Required Output Format
For each stage, produce:
A. Analysis
- What is wrong
- Why it is wrong
- What patterns should replace it
- What the new architecture should look like
B. Proposed Changes
- File‑by‑file
- Class‑by‑class
- Function‑by‑function
- With clear justification
C. Updated Code
- Only the relevant diffs
- Clean, modern, idiomatic C#
- Using C# 12+ features where appropriate
- Targeting .NET 9
- No partial edits that leave the system in an inconsistent state
D. Regression Report
- Which tests were run
- Which passed
- Which failed
- How failures were fixed
E. Next Steps
- What the next stage should address
- Why it matters
- How it improves the architecture

6. Guardrails (Do Not Deviate)
You must not:
- Drift into unrelated topics
- Add features not requested
- Remove features unless they are anti‑patterns
- Produce incomplete refactors
- Leave dead code
- Leave TODOs
- Leave partial migrations
- Skip tests
- Proceed with failing tests
- Produce speculative or hypothetical code
- Produce pseudocode instead of real code
- Produce code that does not compile
- Produce code that does not integrate cleanly
You must stay focused on:
- Code quality
- Architecture
- Maintainability
- Correctness
- Long‑term sustainability
- Modern C#
- .NET 9
- Multi‑session continuity

7. Final Objective
At the end of the multi‑stage migration, the codebase must be:
- Clean
- Modern
- Canonical
- Well‑architected
- Fully tested
- Fully maintainable
- Free of legacy constraints
- Free of anti‑patterns
- Using modern C#
- Targeting .NET 9
- Ready for decades of evolution
- And the modernization plan must remain consistent across sessions
This is your mission.
Do not deviate from it.

8. Supplementary Documents
Read CONSTRAINTS.md for the full anti-pattern catalog (with labels), migration phase breakdown,
session rituals (start and end), and behavioral constraints.
Read MIGRATION_LEDGER.md for the current migration state, phase status, session history,
and outstanding TODOs. Update it at the end of every session.
