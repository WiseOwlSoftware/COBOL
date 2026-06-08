CobolSharp COBOL EVALUATE, Branching, Control‑Flow & Condition Resolution Architecture (CIL‑Only)
================================================================================================

> **STATUS BANNER**
>
> - **Type:** Authoritative design/architecture reference for COBOL branching, control flow, EVALUATE
>   multi‑branch selection, and condition/boolean‑expression resolution.
> - **Implementation status:** LANGUAGE‑FEATURE doc — **~85‑90% implemented** (verify against `src/`).
>   EVALUATE, IF/ELSE, PERFORM UNTIL/VARYING, GO TO (incl. DEPENDING ON), paragraph/section fall‑through,
>   class/sign tests, and boolean short‑circuiting are live across the binder → lowering → emitter pipeline:
>   `src/CobolSharp.Compiler/Semantics/Bound/Binding/ControlFlowBinder.cs`,
>   `src/CobolSharp.Compiler/CodeGen/Lowering/ConditionLowerer.cs`,
>   `src/CobolSharp.Compiler/CodeGen/Lowering/ControlFlowLowerer.cs`,
>   `src/CobolSharp.Compiler/CodeGen/Emission/CilControlFlowEmitter.cs`,
>   `src/CobolSharp.Compiler/Semantics/ProcedureGraph.cs`. Treat any feature note here as a TARGET to verify
>   against the source, not a guarantee.
> - **Stack:** .NET 10 / C# 14.
> - **Backend:** CIL‑only via Mono.Cecil (NO custom VM / NO bytecode interpreter). A Roslyn C# backend is a
>   FUTURE additive Stage‑5; Cecil is the oracle.
> - **Plans / SSOT:** top‑level plan `docs/MASTER_PLAN.md`; doctrine `PROMPT.md`.

Purpose
-------
Define the authoritative architecture for:
- EVALUATE statement (multi‑branch selection): EVALUATE TRUE / EVALUATE expression / multiple subjects (ALSO)
- WHEN, WHEN OTHER, WHEN ANY, WHEN value THRU/THROUGH value, WHEN condition, WHEN TRUE/FALSE
- Branching and fall‑through rules
- IF/ELSE semantics
- PERFORM UNTIL / PERFORM VARYING control flow
- GO TO and paragraph/section flow
- Condition resolution (class tests, sign tests, range tests) and boolean expression lowering
- NOT / AND / OR / short‑circuiting
- Numeric, alphanumeric, and national comparisons
- Structured IL generation (no irreducible flow)
- AOT/WASM‑safe branching

This document governs how CobolSharp implements COBOL’s branching, control‑flow, and conditional model on .NET.

------------------------------------------------------------
SECTION 1 — EVALUATE STATEMENT OVERVIEW
------------------------------------------------------------

EVALUATE is COBOL’s multi‑branch selection construct. CobolSharp implements full
ISO/IEC 1989:2023 EVALUATE semantics.

Forms supported:
1. EVALUATE TRUE
2. EVALUATE expression
3. EVALUATE subject‑1 ALSO subject‑2 ALSO subject‑3 …

Each WHEN clause may contain:
- Single value
- Value range (THRU / THROUGH)
- Condition
- Multiple comma‑separated / space‑separated values
- WHEN ANY
- WHEN TRUE / WHEN FALSE
- ALSO combinations

CobolSharp guarantees:
- Deterministic matching, top‑to‑bottom
- First‑match wins
- No fall‑through between WHEN clauses
- Structured IL (no irreducible flow)

EVALUATE is lowered to a structured if/else chain with short‑circuit evaluation and a
deterministic matching order.

------------------------------------------------------------
SECTION 2 — EVALUATE FORMS
------------------------------------------------------------

2.1 EVALUATE TRUE
-----------------
EVALUATE TRUE
    WHEN condition‑1
    WHEN condition‑2
    WHEN OTHER
END‑EVALUATE.

Each WHEN is evaluated as a boolean expression. Equivalent to:

IF condition‑1 THEN …
ELSE IF condition‑2 THEN …
ELSE …
END‑IF.

2.2 EVALUATE expression
-----------------------
2.2.1 Numeric expression
EVALUATE x
    WHEN 1
    WHEN 2 THRU 5
    WHEN OTHER

Lowering: evaluate x once into a temporary; compare against each WHEN value/range.

2.2.2 Alphanumeric expression
EVALUATE str
    WHEN "A"
    WHEN "B" "C"
    WHEN OTHER

Lowering: `String.CompareOrdinal` (ASCII lexicographic for DISPLAY).

2.2.3 National expression
EVALUATE nstr
    WHEN N"A"
    WHEN OTHER

Lowering: UTF‑16 binary / code‑point comparison.

2.3 EVALUATE multiple subjects (ALSO)
-------------------------------------
EVALUATE a ALSO b
    WHEN 1 ALSO 2
    WHEN 3 ALSO ANY
    WHEN OTHER

Each subject is matched independently against its corresponding WHEN value
(all positions must match for the WHEN to fire). ANY matches any value; ranges and
conditions are allowed per position.

2.4 EVALUATE with ranges
------------------------
WHEN 1 THROUGH 5
WHEN "A" THROUGH "Z"

2.5 EVALUATE with conditions
----------------------------
WHEN x > 10
WHEN x = y

------------------------------------------------------------
SECTION 3 — WHEN CLAUSE & MATCHING SEMANTICS
------------------------------------------------------------

3.1 Matching order
------------------
WHEN clauses are evaluated top‑to‑bottom; first match wins; WHEN OTHER is the fallback.

3.2 Subject/value matching
--------------------------
A match succeeds if:
- subject == value
- subject ∈ range (value1 ≤ subject ≤ value2)
- subject matches a class test
- ANY (always matches)

3.3 WHEN clause forms
---------------------
- WHEN literal — `WHEN 5` → match if subject = 5
- WHEN literal range — `WHEN 1 THRU 10` → inclusive range
- WHEN condition — `WHEN x > 10` → boolean expression
- WHEN multiple values — `WHEN 1 2 3` → match if subject ∈ {1,2,3}
- WHEN ANY — matches any value
- WHEN OTHER — executed only if no other WHEN matches

3.4 Multiple subjects
---------------------
All subjects must match the corresponding WHEN values:

EVALUATE A ALSO B
    WHEN 1 ALSO 2   → A = 1 AND B = 2

3.5 THROUGH ranges by type
--------------------------
- Numeric: value1 ≤ subject ≤ value2
- Alphanumeric: lexicographic comparison
- National: UTF‑16 code‑point comparison

------------------------------------------------------------
SECTION 4 — IF / ELSE SEMANTICS
------------------------------------------------------------

4.1 Basic form
--------------
IF condition
    statement
ELSE
    statement
END‑IF.

4.2 Short‑circuit evaluation
----------------------------
AND / OR follow short‑circuit rules (see Section 7).

4.3 Nested IF
-------------
Compiler generates structured IL:
- No dangling‑ELSE ambiguity
- No irreducible flow

------------------------------------------------------------
SECTION 5 — PERFORM UNTIL / PERFORM VARYING
------------------------------------------------------------

5.1 PERFORM UNTIL
-----------------
PERFORM para UNTIL condition

Lowering:
loop_start:
    if (condition) goto loop_end;
    call para;
    goto loop_start;
loop_end:

5.2 PERFORM VARYING
-------------------
PERFORM para
    VARYING i FROM 1 BY 1 UNTIL i > 10

Lowering:
i = 1;
loop_start:
    if (i > 10) goto loop_end;
    call para;
    i = i + 1;
    goto loop_start;
loop_end:

5.3 Nested VARYING
------------------
Compiler generates nested loops with structured IL.

------------------------------------------------------------
SECTION 6 — GO TO SEMANTICS
------------------------------------------------------------

6.1 GO TO paragraph
-------------------
Transfers control to the paragraph entry.

6.2 GO TO DEPENDING ON
----------------------
GO TO para‑1 para‑2 para‑3 DEPENDING ON x

Lowering:
switch(x):
    case 1: goto para‑1;
    case 2: goto para‑2;
    case 3: goto para‑3;
    default: fall through (no transfer when x out of range, per ISO)

6.3 Interaction with PERFORM stack
----------------------------------
GO TO may exit a PERFORM range:
- Compiler unwinds the PERFORM stack
- Ensures correct return behavior

------------------------------------------------------------
SECTION 7 — BOOLEAN EXPRESSION ARCHITECTURE
------------------------------------------------------------

7.1 Operators
-------------
- NOT
- AND
- OR
- =, <>, >, <, >=, <=
- Class tests (NUMERIC, ALPHABETIC, ALPHABETIC‑LOWER/UPPER)
- Sign tests (POSITIVE, NEGATIVE, ZERO)

7.2 Short‑circuiting
--------------------
A AND B: if A is false → skip B.
A OR B:  if A is true  → skip B.

7.3 NOT
-------
NOT applies to:
- Boolean expressions
- Class tests
- Sign tests

7.4 Parentheses
---------------
Fully supported; the compiler builds an AST and lowers it structurally.

------------------------------------------------------------
SECTION 8 — CLASS TESTS
------------------------------------------------------------

8.1 NUMERIC
-----------
True if all characters are digits; optional sign allowed; no spaces unless allowed by PIC.

8.2 ALPHABETIC
--------------
True if all characters are A–Z or a–z. For NATIONAL data → Unicode letters.

8.3 ALPHABETIC‑LOWER / ALPHABETIC‑UPPER
---------------------------------------
True if all characters are lowercase / uppercase letters respectively.

8.4 NATIONAL tests
------------------
Use Unicode categories.

------------------------------------------------------------
SECTION 9 — SIGN TESTS
------------------------------------------------------------

9.1 POSITIVE — true if numeric value > 0
9.2 NEGATIVE — true if numeric value < 0
9.3 ZERO     — true if numeric value = 0

------------------------------------------------------------
SECTION 10 — COMPARISON RULES
------------------------------------------------------------

10.1 Numeric comparison
-----------------------
- Algebraic numeric comparison.
- COMP / COMP‑3 / COMP‑5 are converted to the numeric substrate for comparison.
  (In the typed‑native data model these flow through the typed numeric carriers
  `long`/`decimal`; the byte engine materializes equivalently. Verify dispatch vs `src/`.)

10.2 Alphanumeric comparison
----------------------------
- DISPLAY: ASCII lexicographic.
- NATIONAL: UTF‑16 lexicographic.

10.3 Mixed types
----------------
- DISPLAY vs NATIONAL: NATIONAL converted to DISPLAY if ASCII; else runtime error.

------------------------------------------------------------
SECTION 11 — PARAGRAPH & SECTION FLOW
------------------------------------------------------------

11.1 Paragraph entry
--------------------
A paragraph label defines a sequence point and a branch target.

11.2 Fall‑through
-----------------
Allowed:
Para‑A.
    …
Para‑B.
    …

Execution flows from A into B unless interrupted by EXIT PARAGRAPH, GO TO, or PERFORM.

11.3 EXIT PARAGRAPH
-------------------
Returns to the caller of PERFORM.

11.4 EXIT SECTION
-----------------
Returns to the caller of PERFORM THRU.

------------------------------------------------------------
SECTION 12 — CIL LOWERING RULES
------------------------------------------------------------

12.1 EVALUATE lowering
----------------------
Compiler emits:
- Temporary locals for subjects (evaluated once)
- Comparison / if‑else chain (branch table when numeric and dense)
- A branch per WHEN
- A final branch for WHEN OTHER

Example:
EVALUATE x
    WHEN 1
    WHEN 2
    WHEN OTHER
END‑EVALUATE.

Lowered to:
temp = x
if (temp == 1) goto when1
if (temp == 2) goto when2
goto whenOther

12.2 IF / boolean lowering
--------------------------
Emit `brfalse` / `brtrue`; no stack manipulation.
- A AND B → evaluate A; `brfalse end`; evaluate B.
- A OR B  → evaluate A; `brtrue end`;  evaluate B.

12.3 PERFORM lowering
---------------------
Emit loop blocks + PERFORM stack push/pop + structured IL.

12.4 GO TO lowering
-------------------
Emit `br` to the paragraph label; unwind the PERFORM stack if needed.

12.5 WHEN OTHER lowering
------------------------
Emit `goto default_label`.

12.6 Class test lowering
------------------------
Dispatch to the runtime classification helpers (e.g. IsNumeric / IsAlphabetic) for the
operand’s category/usage. Verify the concrete helper surface vs `src/`.

12.7 Range lowering
-------------------
value1 ≤ subject ≤ value2 → compare `subject >= value1` AND `subject <= value2`.

12.8 Multiple‑subjects lowering
-------------------------------
Each subject/value pair is lowered independently and combined with AND.

------------------------------------------------------------
SECTION 13 — DEBUGGER INTEGRATION
------------------------------------------------------------

The debugger surfaces:
- Current EVALUATE subject values
- The matched WHEN clause
- IF / boolean‑expression results and the expression tree
- Class/sign test results and range boundaries
- Short‑circuit behavior
- Loop variables for PERFORM VARYING
- PERFORM stack state
- Paragraph/section transitions

------------------------------------------------------------
SECTION 14 — AOT / WASM‑SAFE EXECUTION
------------------------------------------------------------

14.1 No dynamic codegen — all branching is static.
14.2 No unsafe code — no pointers or stackalloc in the lowered branch logic.
14.3 Deterministic evaluation — identical results across CoreCLR, AOT, and WASM.

------------------------------------------------------------
SECTION 15 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

15.1 Multiple WHEN matches — only the first match executes.
15.2 EVALUATE with no WHEN OTHER — if no match, execution continues after END‑EVALUATE.
15.3 GO TO into the middle of a PERFORM — allowed; PERFORM stack unwound.
15.4 PERFORM VARYING with negative BY — allowed; loop may never terminate.
15.5 EVALUATE with mixed (incompatible) types — illegal.
15.6 Range with reversed bounds — value1 > value2 → range never matches.
15.7 Class test on COMP / COMP‑3 — false (binary/packed are not character class‑testable).
15.8 NATIONAL with surrogate pairs — counted as a single character.
15.9 EVALUATE TRUE with no matching WHEN — falls through to WHEN OTHER (or past END‑EVALUATE if absent).

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Control‑Flow & Condition Architecture:
- Implements full COBOL EVALUATE, IF, PERFORM, and GO TO semantics
- Supports multi‑subject EVALUATE with ANY, ranges, conditions, and TRUE/FALSE
- Provides deterministic, short‑circuit boolean evaluation, plus class and sign tests
- Resolves numeric, alphanumeric, and national comparisons consistently
- Ensures structured, verifiable IL with no irreducible flow
- Integrates with the ExecutionContext and PERFORM stack
- Generates clean, debugger‑friendly, CIL‑only output (via Mono.Cecil)
- Ensures correctness across CoreCLR, AOT, and WASM
