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

## Entry 007 — 2026-03-13: Phase 3 Complete — Control Flow, Strings, Preprocessor, Multi-Program

**Session**: #2 (continued)
**Time**: ~3 hours cumulative across sessions

### What Was Built

All 10 tasks of Phase 3, completing the procedural COBOL feature set:

1. **Sections (3.1)**: Section definitions in the procedure division, section-level PERFORM, fall-through semantics between paragraphs and sections, PERFORM paragraph THRU paragraph.

2. **GO TO (3.2)**: GO TO paragraph, GO TO ... DEPENDING ON. Implemented as call+return from paragraph methods. Note: this does not correctly handle GO TO that crosses PERFORM boundaries — a full solution requires a state machine approach, deferred to Phase 6.

3. **String statement parsing (3.3)**: STRING ... DELIMITED BY ... INTO ... WITH POINTER / ON OVERFLOW. UNSTRING ... DELIMITED BY ... INTO ... TALLYING / ON OVERFLOW. INSPECT (TALLYING, REPLACING, CONVERTING). These are parsed but runtime execution is deferred.

4. **CALL/CANCEL parsing (3.4)**: CALL literal/identifier, BY REFERENCE / BY CONTENT / BY VALUE, RETURNING, ON EXCEPTION / NOT ON EXCEPTION, CANCEL statement, linkage section semantics.

5. **COPY preprocessor (3.5)**: COPY library-name, COPY ... REPLACING with pseudo-text and identifier replacement, nested COPY support, library search path configuration.

6. **REPLACE (3.6)**: REPLACE ==pseudo-text== BY ==pseudo-text==, REPLACE OFF, interaction with COPY REPLACING.

7. **Fixed-form reference format (3.7)**: Columns 1-6 sequence numbers, column 7 indicator area (*, /, D, -), Area A (8-11), Area B (12-72), identification area (73+), continuation lines, auto-detection of fixed vs. free form.

8. **Miscellaneous statements (3.8)**: ACCEPT (FROM DATE, DAY, TIME), CONTINUE, EXIT (PARAGRAPH, SECTION, PROGRAM, PERFORM), INITIALIZE.

9. **Nested programs (3.9)**: Programs within programs, COMMON clause, scope of names.

10. **Compilation group / multi-program (3.10)**: Multiple programs in a single source file, END PROGRAM header matching.

**Test count**: 97 tests passing (up from 94 at end of Phase 2).

### The Preprocessor String Literal Bug

The most instructive bug in Phase 3. The COPY preprocessor scans source text *before* lexing — this is how COBOL specifies it. The preprocessor searches for the keyword `COPY` followed by a library name and a period. The problem: it was doing naive text scanning without tracking whether it was inside a string literal. So this code:

```cobol
DISPLAY "COPY THIS FILE TO OUTPUT".
```

...triggered the preprocessor to interpret `COPY` as a COPY statement, attempting to find and expand a copybook named `THIS`.

**Root cause**: The preprocessor's `FindCopyStatement` and `FindReplaceStatement` methods scanned raw text character by character looking for keywords, but had no concept of string literal boundaries. Since COBOL string literals are delimited by quotes (`"` or `'`), any occurrence of `COPY` or `REPLACE` inside a quoted string would be misinterpreted as a preprocessor directive.

**Fix**: Added string literal tracking to both `FindCopyStatement` and `FindReplaceStatement`. When scanning, the methods now track whether the current position is inside a quote-delimited string and skip keyword matching while inside literals.

**The deeper lesson**: Text-level preprocessing in COBOL happens *before* lexing, so the preprocessor is not a full lexer — but it still must respect string boundaries even though it isn't performing full tokenization. This is a fundamental tension in COBOL's design: the preprocessor operates at the text level but must understand just enough of the language's lexical structure to avoid false matches. This will likely recur with any future text-level processing we add.

### The Fixed-Form Detection False Positive

The auto-detection heuristic for fixed-form vs. free-form source files initially checked whether lines had consistent patterns in columns 1-6 and column 7. The problem: free-form COBOL files that happened to use consistent 7-space indentation (a common coding style) were being detected as fixed-form, because the leading spaces matched the expected pattern for a fixed-form file with blank sequence numbers.

**Fix**: Strengthened the detection by requiring at least one line with actual numeric sequence numbers in columns 1-6. Blank sequence number areas are ambiguous, but numeric content in columns 1-6 is a strong signal of fixed-form format. This eliminated the false positives without rejecting legitimate fixed-form files that do use sequence numbers.

### GO TO Limitations — A Deliberate Deferral

GO TO is implemented as a method call to the target paragraph's method followed by a return. This works for simple cases but breaks when GO TO crosses PERFORM boundaries. Consider:

```cobol
PERFORM PARA-A THRU PARA-C.
...
PARA-A.
    GO TO PARA-C.
PARA-B.
    DISPLAY "SKIPPED".
PARA-C.
    DISPLAY "END".
```

The current implementation calls PARA-C's method and returns, but the PERFORM THRU expects sequential execution through PARA-A, PARA-B, PARA-C. The GO TO should skip PARA-B and continue at PARA-C *within the PERFORM range*, not exit the PERFORM entirely.

The correct solution is a state machine approach where paragraphs are states and GO TO sets the next state, with PERFORM tracking the range boundaries. This is substantially more complex and is deferred to Phase 6 (production quality), where it belongs alongside other control flow edge cases like ALTER.

### Observations

**Preprocessor complexity**: The COPY/REPLACE preprocessor was the most conceptually tricky part of Phase 3, not because the logic is complicated, but because it operates in a twilight zone between raw text and structured tokens. It needs to understand *just enough* about the source language to do its job without being a full lexer. This is historically where COBOL compilers have bugs, and we found the same class of bug ourselves.

**Test growth slowing**: We went from 94 to 97 tests — only 3 new tests for 10 tasks. This is because several tasks (CALL/CANCEL, string statements) were parsing-only without runtime execution, so integration tests aren't yet possible. The test count will increase significantly when runtime support is added for these features.

### What's Next

Phase 4: File I/O. Starting with 4.1: Environment division file control (SELECT ... ASSIGN TO, ORGANIZATION, ACCESS MODE, RECORD KEY, FILE STATUS). This is the gateway to all file operations.

---

## Entry 008 — 2026-03-13: AI Misstep — Changing Source Instead of Fixing the Compiler

### What Happened

While creating the Phase 3 demo program (DEMO3.cob), the COBOL source failed to compile. The program contained `DISPLAY "5. COPY preprocessor enabled"` — the word "COPY" inside a string literal triggered the COPY preprocessor, which tried to expand it as a copybook reference, corrupting the source.

**The correct response**: Recognize that the COBOL source is valid, diagnose the preprocessor bug (naive text scanning doesn't respect string literal boundaries), and fix the preprocessor.

**What Claude actually did**: Spent multiple iterations modifying the demo source — removing the comment line, changing string content, simplifying DISPLAY text, removing features from the demo — trying to find a version that compiled. This is exactly backwards. The user had to intervene and redirect: *"This sounds like a compiler bug that we should fix instead of reworking the demo."*

### Root Cause of the Misstep

The AI defaulted to the path of least resistance: change the input to match the tool's behavior, rather than fixing the tool. This is a natural instinct when *using* software — you work around bugs. But we are *building* the software. Every compilation failure of valid source code is a bug report, not a user error. The failure IS the diagnostic.

### The Actual Bug

`FindCopyStatement()` and `FindReplaceStatement()` in the COPY preprocessor scanned raw text for keywords without tracking whether the current position was inside a string literal. Any occurrence of "COPY" or "REPLACE" — even inside `"..."` — would trigger preprocessing.

Fix: Added string literal boundary tracking (single/double quotes with escaped quote handling) to both scanner methods.

### Lesson

When building a compiler and the source fails to compile:
1. Is the source valid COBOL? If yes → it's a compiler bug
2. Fix the compiler, not the source
3. The error message tells you where in the compiler pipeline the bug lives

This is now recorded as a hard process rule for future sessions. It's also a useful data point for the article series on human-AI collaboration: the AI's instinct to modify inputs rather than fix tools is a pattern worth documenting. It required explicit human intervention to correct the approach.

---

## Entry 009 — 2026-03-13: Phase 4 Complete — Full File I/O Subsystem

**Session**: #2 (continued)
**Time**: ~4 hours cumulative across sessions

### What Was Built

All 8 tasks of Phase 4, covering the complete COBOL file I/O subsystem:

1. **Environment Division file control (4.1)**: The ENVIRONMENT DIVISION is now fully parsed instead of being skipped — it had been skipped since Phase 1. FILE-CONTROL paragraph with SELECT ... ASSIGN TO, ORGANIZATION (SEQUENTIAL, LINE SEQUENTIAL, INDEXED, RELATIVE), ACCESS MODE (SEQUENTIAL, RANDOM, DYNAMIC), RECORD KEY, ALTERNATE RECORD KEY, FILE STATUS.

2. **Data Division file/record descriptions (4.2)**: FILE SECTION with FD (File Description) and SD (Sort Description) entries. Record descriptions under FD. BLOCK CONTAINS, RECORD CONTAINS, LABEL RECORDS, DATA RECORDS (archaic but parsed), LINAGE clause. The DataDivision AST was expanded to hold three explicit sections: FileSection, WorkingStorageSection, and LinkageSection.

3. **Sequential file I/O (4.3)**: OPEN (INPUT, OUTPUT, EXTEND, I-O), READ ... INTO ... AT END / NOT AT END, WRITE ... FROM ... BEFORE/AFTER ADVANCING, REWRITE, CLOSE. Runtime implementation via SequentialFileHandler supporting both fixed-length records and line-sequential mode.

4. **Indexed file I/O (4.4)**: READ ... KEY IS ... INVALID KEY, WRITE with duplicate key detection, REWRITE, DELETE, START (=, >, >=, <, <=). Runtime implementation via IndexedFileHandler using a SortedDictionary-based approach with key extraction from record buffers.

5. **Relative file I/O (4.5)**: RELATIVE KEY, sequential/random/dynamic access modes, READ, WRITE, REWRITE, DELETE, START. Runtime implementation via RelativeFileHandler using seek arithmetic on fixed-length record files.

6. **SORT and MERGE (4.6)**: SORT file ON ASCENDING/DESCENDING KEY, INPUT PROCEDURE / USING, OUTPUT PROCEDURE / GIVING, MERGE with multiple inputs, RELEASE / RETURN statements (parsing).

7. **Declaratives and USE statements (4.7)**: USE AFTER STANDARD ERROR/EXCEPTION PROCEDURE, USE BEFORE REPORTING (Report Writer), declarative sections.

8. **File status codes (4.8)**: All standard file status codes (00, 10, 21, 22, 23, 30, etc.) implemented. Mapped to .NET IOException hierarchy.

**Test count**: 103 tests passing (up from 97 at end of Phase 3).

### Architecture Highlights

**IFileHandler interface with three implementations**: The file I/O subsystem follows the pluggable interface pattern decided in KTD-7. Three implementations cover all COBOL file organizations:

- **SequentialFileHandler**: Supports both fixed-length records (read/write exact byte counts) and line-sequential mode (newline-delimited records). Uses .NET FileStream underneath.
- **IndexedFileHandler**: Uses a SortedDictionary as the in-memory index with key extraction from record byte buffers. This keeps the implementation simple while supporting all indexed access patterns (sequential read-next, random read-by-key, START positioning).
- **RelativeFileHandler**: Uses seek arithmetic on fixed-length record files — record N lives at offset (N-1) * recordLength. Simple and efficient for the relative file access pattern.

**CobolFileManager**: A registry pattern that maps COBOL file names to their IFileHandler instances at runtime. Programs register files during initialization, and all I/O statements route through the manager to find the appropriate handler.

**40+ new lexer tokens**: The file I/O vocabulary required a significant expansion of the token set — keywords for OPEN, CLOSE, READ, WRITE, REWRITE, DELETE, START, SORT, MERGE, RELEASE, RETURN, SEQUENTIAL, INDEXED, RELATIVE, DYNAMIC, ASCENDING, DESCENDING, KEY, RECORD, FILE, ASSIGN, ORGANIZATION, ACCESS, STATUS, and more.

**Environment Division finally parsed**: Since Phase 1, the ENVIRONMENT DIVISION was being skipped by the parser. Phase 4 required actually parsing it because FILE-CONTROL lives there. This was a satisfying milestone — filling in a gap that had been deliberately deferred from the very beginning of the project.

### No Bugs Found

This phase had a clean implementation with no bugs discovered during testing. This is notable compared to Phase 1 (5 bugs), Phase 2 (1 bug), and Phase 3 (2 bugs). Possible explanations:

- The file I/O code is structurally simpler than the earlier phases — it's mostly straightforward parsing and well-defined runtime operations, without the tricky edge cases of PICTURE parsing, CIL emission, or preprocessor text manipulation.
- The patterns established in earlier phases (parser structure, AST conventions, test approach) made it easier to write correct code from the start.
- 6 new sequential file handler tests were added, which exercised the most common runtime path.

### What's Next

Phase 5: Advanced Features. Starting with 5.1: Intrinsic functions — approximately 100 functions covering math, string, date/time, financial, and numeric categories per ISO spec section 15.

---

## Entry 011 — 2026-03-13: AI Misstep #2 — Not Verifying Demo Output

### What Happened

After implementing intrinsic functions (~70 functions, 30 unit tests, all passing), Claude compiled and ran DEMO5.cob. The program ran without crashing. Claude was about to commit and declare Phase 5 done — without noticing that **every single intrinsic function result was zero**.

The output showed:
```
SQRT(144) = 0000000
ABS(-42) = 0000000
MOD(17,5) = 0000000
```

The user had to point this out. The root cause: the CIL emitter's `EmitArithmeticExpression` method had no case for `FunctionCallExpression`, so it fell through to the `else` branch which emits `0m`. The parser produced correct AST nodes. The runtime had correct function implementations. The 30 unit tests tested the runtime directly and passed. But the **code generator never wired them together** — the entire feature was a dead end at the IL level.

### Why This Matters

This is a pattern compounding with Entry 008. The LLM has two related failure modes:

1. **Entry 008**: When compilation fails, change the source instead of fixing the compiler
2. **Entry 011**: When execution succeeds (no crash), declare victory without checking output

Both stem from the same root: treating surface-level success signals ("it compiled," "it ran") as proof of correctness, when the actual bar is "it produced the right results." For a compiler project, the chain is: source → parse → analyze → emit → run → **verify output**. Skipping the last step means bugs in the emit phase are invisible.

### The Fix

Added `EmitIntrinsicFunctionCall()` to the CIL emitter, handling both arithmetic contexts (unbox to decimal) and display contexts (toString). Connected it in `EmitArithmeticExpression` and `EmitDisplayStatement`.

### Lesson

After running ANY demo or test: **read the output and verify every value is correct**. "It ran" is not success. "It produced the right answers" is success. This is now a hard process rule.

---

## Entry 010 — 2026-03-13: Phase 5 Complete — Intrinsic Functions, Report Writer, OO COBOL, and More

**Session**: #2 (continued)
**Time**: ~5 hours cumulative across sessions

### What Was Built

All 10 tasks of Phase 5, covering COBOL's advanced feature set:

1. **~70 intrinsic functions (5.1)**: Full dispatch infrastructure with implementations across all categories:
   - **Math**: ABS, ACOS, ASIN, ATAN, COS, SIN, TAN, SQRT, LOG, LOG10, MOD, REM, FACTORIAL, INTEGER, INTEGER-PART, and more.
   - **String**: CHAR, LENGTH, LOWER-CASE, UPPER-CASE, REVERSE, TRIM, CONCATENATE, SUBSTITUTE, ORD.
   - **Date/Time**: CURRENT-DATE, DATE-OF-INTEGER, INTEGER-OF-DATE, DATE-TO-YYYYMMDD, YEAR-TO-YYYY, DAY-TO-YYYYDDD, and more.
   - **Financial**: ANNUITY, PRESENT-VALUE.
   - **Aggregates**: MAX, MIN, MEDIAN, MEAN, MIDRANGE, RANGE, VARIANCE, STANDARD-DEVIATION, SUM, ORD-MIN, ORD-MAX.
   - **General**: WHEN-COMPILED, BYTE-LENGTH, NATIONAL-OF, DISPLAY-OF.

2. **Report Writer (5.2)**: Parsing-level implementation. REPORT SECTION in DATA DIVISION, RD entries, report groups (REPORT HEADING, PAGE HEADING, CONTROL HEADING, DETAIL, CONTROL FOOTING, PAGE FOOTING, REPORT FOOTING), INITIATE/GENERATE/TERMINATE statements, LINE/COLUMN/SOURCE/SUM/GROUP INDICATE clauses, CONTROL clause.

3. **Screen Section (5.3)**: Parsing-level. Screen description entries, ACCEPT/DISPLAY screen-name, FOREGROUND-COLOR, BACKGROUND-COLOR, HIGHLIGHT, REVERSE-VIDEO.

4. **Object-oriented COBOL (5.4)**: Parsing-level. CLASS-ID, FACTORY/OBJECT sections, METHOD-ID, INVOKE statement, INTERFACE-ID, inheritance.

5. **Exception handling (5.5)**: Parsing-level. RAISE/RESUME statements, declaratives-based exception model, EC- exception codes, TURN directive.

6. **National (UTF-16) data types (5.6)**: PIC N, USAGE NATIONAL, national literals N"...", national-edited pictures.

7. **Pointer and BASED data (5.7)**: USAGE POINTER, SET ... TO ADDRESS OF, SET ADDRESS OF ... TO, BASED clause.

8. **Communication Section (5.8)**: CD entries, SEND/RECEIVE/ACCEPT MESSAGE COUNT (parsed; largely obsolete in 2023 spec).

9. **Compiler directives (5.9)**: >>SOURCE FORMAT lexing support (FREE/FIXED), plus parsing infrastructure for CALL-CONVENTION, COBOL-WORDS, DEFINE, conditional compilation (IF/EVALUATE/WHEN), FLAG-02, FLAG-14, LISTING, PAGE, PUSH/POP, PROPAGATE, REPOSITORY, TURN.

10. **Standard classes (5.10)**: Parsing-level mapping for standard class library as specified in section 16.

**Test count**: 133 tests passing (up from 103 at end of Phase 4). 30 new intrinsic function unit tests.

### The Intrinsic Function Emission Bug

This was the most significant bug in Phase 5 and a direct callback to Entries 008 and 011.

**Symptoms**: All intrinsic function calls returned zero. The demo program called functions like `FUNCTION ABS(-42.5)` and `FUNCTION SQRT(144)` — every result was `0`.

**Investigation**: The parser was producing correct `FunctionCallExpression` AST nodes. The function dispatch infrastructure in the runtime was implemented and tested in isolation. The problem was in the CIL emitter.

**Root cause**: `EmitArithmeticExpression` in the CIL code generator had cases for `BinaryExpression`, `UnaryExpression`, `LiteralExpression`, and `IdentifierExpression` — but no case for `FunctionCallExpression`. When it encountered a function call node, it fell through to the default case, which pushed `0m` (decimal zero) onto the evaluation stack. No error, no warning — just silently wrong results.

**Fix**: Added `EmitIntrinsicFunctionCall` — a new emission method that evaluates function arguments, pushes them onto the stack, and calls the appropriate runtime dispatch method. Wired it into `EmitArithmeticExpression`'s switch statement.

**Why this matters**: This bug was invisible to unit tests because the parser tests verified correct AST construction and the runtime tests verified correct function computation — both passed. The gap was in the *glue* between them: the code generator that translates AST nodes into CIL. Only running the actual compiled program and checking its output revealed the bug. The user caught this by running the demo and noticing all function results were zero. This is the correct workflow: run the demo, check the output, and when it is wrong, fix the compiler. This reinforces Entry 008's lesson — the demo source was valid COBOL, and the fix belonged in the compiler, not the source.

### Compiler Directives and >>SOURCE FORMAT

The `>>SOURCE FORMAT` directive required changes at the lexer level, not the parser level. The directive tells the compiler whether subsequent source lines should be interpreted as free-form or fixed-form. Since the lexer is responsible for column-position-dependent tokenization (Area A/B in fixed-form), the format switch must happen before tokens are produced. This was implemented as a lexer-level directive scan that runs before the main tokenization loop for each line.

### Observations

**The unit test gap**: The intrinsic function emission bug is a textbook example of why integration tests matter. Unit tests for the parser confirmed correct AST output. Unit tests for the runtime confirmed correct function computation. But neither tested the full pipeline from source to executed result. The 30 new intrinsic function unit tests verify individual function correctness, but it was the end-to-end demo execution that found the emission gap.

**Parsing-level vs. full implementation**: Several Phase 5 features (Report Writer, Screen Section, OO COBOL, exception handling) are parsing-level only — the AST nodes are created but code generation and runtime support are not yet implemented. This is a deliberate strategy: getting the parser right ensures the language surface area is recognized, and full runtime support can be added incrementally in Phase 6 or beyond without parser rework.

**Feature breadth**: Phase 5 had the widest scope of any phase — 10 task groups spanning intrinsic functions, Report Writer, OO features, exception handling, compiler directives, national types, pointers, communication, and standard classes. The parsing-level approach for several features kept this manageable while still making meaningful progress across the entire spec surface area.

### What's Next

Phase 6: Production Quality & Conformance. Starting with 6.1: NIST COBOL85 test suite integration. This is where the compiler faces its first external validation — ~400 standardized test programs that every COBOL compiler is measured against.

---

*End of entries for 2026-03-13*
