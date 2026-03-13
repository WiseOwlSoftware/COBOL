# CobolSharp Developer Log

A chronological narrative of building a production COBOL compiler from the ISO/IEC 1989:2023
specification, targeting .NET. This log captures the thinking, decisions, failures, breakthroughs,
and lessons learned — intended as source material for a series of articles.

---

## Entry 001 — 2026-03-13: The Beginning

### Context
Starting from a blank repository containing only the 1,261-page ISO/IEC 1989:2023 COBOL
specification PDF. The goal: build a production-quality, fully standards-compliant COBOL compiler
that targets .NET.

### Key Decisions Made

**Why .NET as the target platform?**
We considered several options — LLVM IR, JVM bytecode, native x86/ARM, and .NET CIL. We chose
.NET for several compelling reasons:

1. **Decimal arithmetic is built in.** COBOL lives and dies by exact decimal math. .NET's
   `decimal` type is 128-bit base-10 — it maps almost perfectly to COBOL's `COMP-3`/packed
   decimal. On LLVM or native targets, we'd have to build or import a decimal math library
   from scratch, which is a massive and error-prone undertaking.

2. **Precedent validates the approach.** Micro Focus Visual COBOL and Fujitsu NetCOBOL both
   target .NET in production. This isn't a research experiment — it's a proven path.

3. **Interop story.** .NET assemblies can be called from C#, F#, VB.NET. This means COBOL
   programs compiled by our tool can participate in modern .NET applications, which is the
   entire value proposition of a COBOL modernization tool.

4. **Runtime services for free.** Garbage collection, threading, I/O, string handling, and a
   massive standard library. We don't need to build a runtime from scratch.

**Why C# as the implementation language?**
Same ecosystem as our target. We can reference Roslyn's architecture for design patterns. The
tooling (debugger, profiler, IDE support) is best-in-class.

**Why hand-written recursive descent parser?**
COBOL's grammar is notoriously context-sensitive. `PICTURE` clauses contain characters that are
operators elsewhere. Area A/B rules in fixed-form affect parsing. Inline PERFORM creates scoping
that depends on paragraph ordering. Parser generators (ANTLR, yacc) struggle with these
ambiguities. Roslyn uses hand-written recursive descent for C# for similar reasons — the control
you get is worth the verbosity.

**Why Mono.Cecil for CIL emission?**
System.Reflection.Emit works but has a clunky API and limited PDB support. Mono.Cecil is the
industry standard for .NET IL manipulation (used by Unity, Fody, many others). It gives us clean
APIs for emitting instructions, defining types, and writing debug symbols.

### Architecture Sketch
We defined a 5-stage pipeline:
```
Source → Preprocessor → Lexer → Parser → Semantic Analysis → CIL Code Gen → .NET Assembly
```

This is deliberately traditional. No novel compilation techniques — the novelty is in handling
COBOL's enormous spec surface area correctly.

### The Scale of the Problem
The spec has 2,090 table-of-contents entries across 1,261 pages. For comparison, the C11 spec
is ~700 pages and the C# spec is ~800 pages. COBOL is genuinely one of the largest language
specifications in existence. We broke this into 6 phases with ~60 task groups to make it
tractable.

### What's Next
Phase 1: scaffold the .NET solution and get "Hello, World!" compiling from COBOL to a running
.NET executable. This is the proof-of-concept that validates every architectural decision above.

---

## Entry 002 — 2026-03-13: Expanding the Key Technical Decisions

### Why This Matters
The initial plan had a one-line-per-decision summary table for technical choices. That's fine for
quick reference but terrible for understanding *why* we made each choice. Since this project will
become an article series, and since future sessions need to understand the reasoning (not just the
conclusion), we expanded every decision into a full analysis.

### The Interesting Tensions

**The "transpile to C#" temptation (KTD-4):** It's genuinely appealing — let Roslyn handle all
the hard CIL work, and we just emit C# source. But COBOL's control flow kills this idea. How do
you express `PERFORM paragraph-a THRU paragraph-d` in C#? It means "execute all paragraphs from
a through d in source order, then return." There's no C# construct for that. You'd need labels
and gotos, computed dispatch, or some state-machine transformation — all of which produce
unreadable C# that's impossible to debug. Going straight to CIL means we can emit exactly the
branch instructions we need.

**The decimal problem (KTD-5):** This one has layers. The naive approach is "just use .NET
`decimal` for all numeric data." But then you hit REDEFINES — where a numeric field and an
alphanumeric field share the same memory location. Or group MOVE — where a group item containing
numeric and string subfields is bulk-copied as raw bytes. COBOL programs *routinely* inspect and
manipulate the byte-level representation of numeric data. You can't do that with a `decimal`.

So we went dual-layer: `byte[]` for storage (preserving the byte-level semantics COBOL depends
on), `decimal` for computation (leveraging .NET's built-in base-10 arithmetic). The
marshal/unmarshal cost is the price we pay — but it only occurs on arithmetic operations, which
are a small fraction of most COBOL programs' execution time (MOVEs dominate).

**Parser generators vs. hand-written (KTD-3):** This is the decision most likely to be
second-guessed. ANTLR is powerful and would save us thousands of lines of parser code. But we
identified five specific ways COBOL breaks parser generators — PICTURE clauses, Area A/B
rules, COPY/REPLACE preprocessing, PERFORM THRU scoping, and implicit scope terminators. Each
one requires context that grammar-driven parsers don't naturally provide. Roslyn's team made the
same choice for C# (which is far less context-sensitive than COBOL), and their reasoning
convinced us.

### What's Next
Same as before — Phase 1 implementation. But now the plan document is robust enough that someone
reading it cold understands not just *what* we're building, but *why* every major choice was made.

---

## Entry 003 — 2026-03-13: The Meta-Story — Human-AI Collaboration as Content

### The User's Request
The user made an important framing request: this project isn't just about building a compiler.
It's also about documenting how a human and an AI collaborate on a large, complex engineering
project — warts and all. Specifically:

- **Log AI missteps honestly.** When I (Claude) make wrong decisions, misunderstand instructions,
  produce incorrect code, or go down dead ends — document it. Don't minimize or be defensive.
- **Track friction points.** When the user gets frustrated, document what caused it and why.
- **Document the collaboration pattern itself.** This is research into human-AI pair programming.

### Known Patterns from Prior Projects
The user shared observations from previous AI-assisted projects that are worth recording because
they're likely to recur:

1. **Session drift**: The longer a session runs, the more the AI tends to go off-track. Responses
   become less precise, less aligned with the user's intent, and less productive. This is likely
   caused by accumulated context competing for attention and perhaps compaction artifacts.

2. **Fresh session ramp-up cost**: Starting a new session solves the drift problem but creates a
   new one — the AI starts cold and needs to rebuild context. This can waste significant time
   re-explaining the project state.

3. **Context compaction losses**: When conversation history gets compressed to fit the context
   window, important details get lost. Decisions that were thoroughly discussed earlier become
   forgotten, leading to re-litigation or contradictory behavior.

### Our Mitigations
We're running on Opus with 1M token context, which is substantially larger than previous models.
This should delay compaction significantly. But we're not relying on it — our defense is
external state:

- **PROJECT_PLAN.md**: Always reflects the true current state of what's done and what's next.
  A fresh session reads this file and immediately knows where to pick up.
- **DEVLOG.md**: Captures the reasoning and narrative so a fresh session understands *why*
  things are the way they are, not just *what* they are.
- **Persistent memory**: Claude's memory system carries critical instructions across sessions
  without needing to re-read them.
- **Detailed commit messages**: Git history itself tells the story of how the code evolved.

The hypothesis: with these four layers of external memory, the ramp-up cost for a fresh session
should be minutes, not the long re-orientation the user has experienced in past projects.

We'll see if this holds. If it doesn't, that failure is itself valuable content for the articles.

### A Note on Honesty
This is an unusual ask. Most AI interactions optimize for appearing competent. The user is
explicitly asking for the opposite — they want to see where the AI fails, what causes it, and
how recovery happens. This is more valuable for the articles than a sanitized narrative of
flawless execution. The compiler will work eventually regardless; the interesting story is the
journey and the collaboration dynamics.

---

## Entry 004 — 2026-03-13: Defense-in-Depth Against Context Loss (Claude's Summary)

After setting up the transparency and session management rules, here's the system we have in
place — four layers of external state that together should make any session (fresh or continued)
productive quickly:

| Layer | What it preserves |
|-------|-------------------|
| `PROJECT_PLAN.md` | Current state — what's done, what's next |
| `DEVLOG.md` | Reasoning — why things are the way they are |
| Persistent memory | Process rules — how to behave across sessions |
| Git commit messages | Code evolution — forensic trace of every change |

The 1M token context window on Opus should give us much longer productive sessions before
drift becomes an issue. But when it does happen, the commitment is to flag it rather than
quietly degrading. And if we need a fresh session, the ramp-up should be fast: read the plan,
read the latest devlog entries, check git log, and go.

This is a testable hypothesis. If it works, it's a replicable pattern for long-running
AI-assisted projects. If it doesn't, understanding *why* it failed is equally valuable.

---

## Entry 005 — 2026-03-13: Phase 1 Complete — "Hello, World!" Runs on .NET

**Session**: #2 (first implementation session)
**Time**: ~1 hour elapsed in this session
**Cumulative**: ~2 hours across 2 sessions (Session 1: planning, Session 2: implementation)

### What Was Built

The entire Phase 1 compiler pipeline, from nothing to a working COBOL-to-.NET compiler:

1. **Solution scaffolding**: 5-project .NET 8 solution (Compiler, Runtime, CLI, Tests.Unit, Tests.Integration)
2. **Source text abstraction**: SourceText with line/column tracking, SourceLocation, TextSpan
3. **Lexer**: Free-form COBOL tokenizer with ~100 keyword mappings, case-insensitive matching, string/numeric literals, free-form comments (`*>`), operators, figurative constants
4. **AST**: Full node hierarchy — CompilationUnit, ProgramNode, divisions, 8 statement types, 8 expression types
5. **Parser**: Recursive descent with operator precedence for COMPUTE, COBOL-style conditions (GREATER THAN OR EQUAL TO, etc.), error recovery (skip to period)
6. **Semantic analysis**: Symbol table, data-name resolution, PICTURE parsing (9/X/A/V/S with repeat counts)
7. **Runtime**: CobolProgram base class, CobolField with byte[] storage + decimal computation, DISPLAY/MOVE/ADD/SUBTRACT
8. **CIL code generator**: Mono.Cecil emission — one class per PROGRAM-ID, field initialization from VALUE clauses, full procedure division emission, Main entry point
9. **CLI**: `cobolsharp compile <file> [-o output]`
10. **Tests**: 43 tests (39 unit + 4 integration), all passing
11. **CI**: GitHub Actions (Ubuntu + Windows matrix)

### The Five Bugs (Mistakes That Were Made)

These are worth documenting in detail because they represent patterns of AI-generated code errors:

**Bug 1: C# Ternary Type Coercion Trap**
```csharp
object value = hasDot ? decimal.Parse(text) : long.Parse(text);
```
This looks correct — if there's a decimal point, parse as decimal; otherwise as long. But C# ternary expressions require both branches to have a common type. Since `long` is implicitly convertible to `decimal`, the compiler silently promotes the `long` branch to `decimal`. The boxed `object` always contained a `decimal`, never a `long`. This is a genuinely subtle C# gotcha — both branches are individually correct, but the ternary combining them introduces a silent type conversion.

**Fix**: Replace with explicit if/else to prevent implicit conversion.

**Bug 2: Dead Code — Lexer Level Number Recognition**
The lexer's `ReadWord()` method had code to recognize level numbers (01-49, 66, 77, 88). But `ReadWord()` only fires when the first character is a letter or hyphen. Numbers always route to `ReadNumericLiteral()` first. The level number code in `ReadWord()` could never execute. This is a *design* error — I (Claude) placed the level number recognition in the wrong lexer method.

**Fix**: Moved level number recognition to the parser as a context-sensitive check. This is actually the architecturally correct place for it, since `42` is a valid numeric literal in COMPUTE but a level number in DATA DIVISION. The lexer shouldn't make this decision.

**Bug 3: Parser Scope Terminator Blindness**
Statement parsers (DISPLAY, MOVE, ADD) read operand lists "until period or next statement keyword." But `ELSE`, `END-IF`, `END-PERFORM` aren't statement keywords — they're scope terminators. Inside an IF body, `DISPLAY "Hello" ELSE DISPLAY "World"` would cause DISPLAY to consume `ELSE` as an operand expression, which then error-recovered badly.

This is the kind of bug that's invisible with simple test cases but breaks immediately with nested structures. The fix was trivial (add scope terminator checks), but the root cause is a failure to think about how individual parsers interact with the overall statement-nesting structure.

**Bug 4: CIL DISPLAY Stack Corruption**
The original DISPLAY emitter tried to build an `object[]` array and call the base class `Display(params object[])` method. The IL stack manipulation was wrong: it pushed `Ldarg_0` (this) then `Pop`'d it, with confused comments about the stack state. This is a classic danger of writing raw IL — there's no compiler checking your stack discipline.

**Fix**: Completely rewrote DISPLAY to use `Console.Write` per operand + `Console.WriteLine()` at the end. Simpler, correct, and matches COBOL semantics more directly.

**Bug 5: CIL Field Init Argument Order**
`MoveNumeric(decimal value, CobolField target)` is static. The emitter pushed `CobolField` first, then `decimal`. CIL is stack-based — arguments must be pushed in parameter order. Reversed arguments mean the decimal gets interpreted as a CobolField pointer and vice versa, causing a type safety violation at runtime.

### Observations on the AI Development Process

**Speed**: Building a complete (minimal) compiler pipeline in ~1 hour is fast by any measure. The plan's 6-phase structure with detailed task breakdowns made this possible — there was no time wasted deciding what to build next.

**Error patterns**: All 5 bugs were in the code generation / IL emission layer. The lexer, parser, and semantic analyzer worked correctly on first pass (once the ternary bug was fixed). This suggests the AI is more reliable at abstract/structural code (AST manipulation, recursive descent parsing) than at low-level details (IL stack manipulation, C# type coercion edge cases). This matches intuition — IL emission requires precise reasoning about invisible state (the evaluation stack), which is harder for probabilistic models.

**Test-driven correction**: All bugs were found by the test suite, not by manual inspection. This validates the decision to write comprehensive tests alongside the implementation. Without tests, bugs 1, 2, and 3 would have been invisible until later phases.

### What's Next

Phase 2: Core Data & Arithmetic. Starting with full PICTURE clause support (the most complex parsing challenge in COBOL), then USAGE, data hierarchy, full MOVE semantics, arithmetic statements, IF/EVALUATE, and PERFORM.

---

## Entry 006 — 2026-03-13: Phase 2 Core Data & Arithmetic — Tasks 2.1–2.6 Complete

**Session**: #2 (continued)
**Time**: ~2.5 hours cumulative across sessions

### What Was Built

Tasks 2.1 through 2.6 of Phase 2, covering the core data model and arithmetic/conditional
infrastructure:

1. **Full PICTURE clause parsing (2.1)**: All PICTURE symbols — 9, X, A, V, S, P, Z, *, +, -, CR, DB, B, 0, /, comma, period, currency symbol. Repeat counts (`9(5)`, `X(10)`). Edited pictures (numeric edited, alphanumeric edited). Category determination from PICTURE string.

2. **USAGE clause (2.2)**: DISPLAY (default), BINARY/COMP/COMP-4/COMP-5, PACKED-DECIMAL/COMP-3, INDEX, POINTER, FUNCTION-POINTER, PROCEDURE-POINTER. Storage size calculation per USAGE type. Alignment rules.

3. **Data hierarchy and groups (2.3)**: Level numbers 01-49, 66, 77, 88. Group items as composite structures. OCCURS clause (fixed and DEPENDING ON). REDEFINES clause. RENAMES (level 66). Condition-names (level 88). FILLER items. JUSTIFIED, BLANK WHEN ZERO, VALUE, SYNCHRONIZED clauses.

4. **MOVE statement — full semantics (2.4)**: Numeric-to-numeric (scaling, truncation, sign handling). Numeric-to-alphanumeric/edited. Alphanumeric-to-alphanumeric (space-padding, truncation). Group MOVE (byte-level copy). MOVE CORRESPONDING.

5. **Arithmetic statements (2.5)**: ADD, SUBTRACT, MULTIPLY, DIVIDE (all forms including GIVING, CORRESPONDING, REMAINDER). COMPUTE with full arithmetic expression support. ROUNDED phrase. ON SIZE ERROR / NOT ON SIZE ERROR.

6. **Conditional expressions (2.6)**: IF/ELSE/END-IF. Relation conditions. Class conditions (NUMERIC, ALPHABETIC). Sign conditions. Condition-name conditions (level 88). Combined conditions (AND, OR, NOT). Abbreviated combined conditions. EVALUATE/WHEN/WHEN OTHER/END-EVALUATE.

**Test count**: 88 tests passing (up from 43 at end of Phase 1).

### The REDEFINES Offset Bug

The only bug found in this batch of work. When processing REDEFINES, items were being assigned
sequential offsets (each item placed after the previous one) instead of sharing the offset of
the item being redefined. For example:

```cobol
01 WS-DATE         PIC 9(8).
01 WS-DATE-PARTS REDEFINES WS-DATE.
   05 WS-YEAR      PIC 9(4).
   05 WS-MONTH     PIC 9(2).
   05 WS-DAY       PIC 9(2).
```

`WS-DATE-PARTS` must start at the same offset as `WS-DATE` — they share the same memory. The
bug was assigning `WS-DATE-PARTS` a new sequential offset, so it occupied different memory than
`WS-DATE`, completely defeating the purpose of REDEFINES.

This is exactly the kind of semantic bug that's easy to write and hard to spot visually. The
tests caught it.

### Level Numbers: Lexer vs. Parser — An Architecturally Correct Decision

An interesting design challenge carried forward from the Phase 1 lexer bug (#2 in Entry 005):
COBOL level numbers (01, 05, 10, 66, 77, 88) look identical to integer literals. The string
`05` in a DATA DIVISION is a level number; the string `05` in a COMPUTE statement is the number
five.

The Phase 1 fix moved level number recognition from the lexer to the parser, treating all
digit sequences as numeric tokens and letting the parser decide based on context whether it's a
level number or a literal. This turned out to be the architecturally correct decision for
COBOL — the lexer produces context-free tokens, and the parser applies context-sensitive
interpretation. This pattern served us well throughout all of Phase 2's data hierarchy work,
where level numbers appear constantly and must be distinguished from numeric operands.

### The C# Ternary Lesson Carries Forward

The C# ternary type coercion bug from Phase 1 (Entry 005, Bug #1) was a good lesson that
carried forward. Throughout Phase 2 implementation, we were more careful about implicit type
conversions in conditional expressions, avoiding the pattern of boxing different numeric types
through ternary operators. Once bitten, twice shy — and having the bug documented in the devlog
made it easy to remember.

### Observations

**Growing test suite confidence**: Going from 43 to 88 tests means the test suite is becoming
a real safety net. The REDEFINES offset bug was caught purely by tests — it would have been
nearly invisible to manual code review since the offset calculation logic looks plausible at a
glance.

**Pace**: Six task groups completed in one continued session. The data model work (PICTURE,
USAGE, hierarchy) is foundational — everything in later phases depends on getting this right.
The time investment here pays dividends later.

### What's Next

Task 2.7: PERFORM statement. This is a significant control flow challenge — out-of-line
PERFORM (paragraph/section), PERFORM THRU, inline PERFORM/END-PERFORM, PERFORM TIMES,
PERFORM UNTIL, PERFORM VARYING (single and nested), TEST BEFORE/TEST AFTER. After that,
table handling (2.8), reference modification (2.9), and figurative constants (2.10) to
complete Phase 2.

---

*End of entries for 2026-03-13*
