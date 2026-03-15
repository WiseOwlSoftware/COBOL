CobolSharp COBOL EVALUATE, Branching & Control‑Flow Semantics Architecture (CIL‑Only)
====================================================================================

Purpose
-------
Define the authoritative architecture for:
- EVALUATE statement (multi‑branch selection)
- WHEN, WHEN OTHER
- EVALUATE TRUE
- EVALUATE expression
- EVALUATE with multiple subjects
- Branching and fall‑through rules
- IF/ELSE semantics
- PERFORM UNTIL / PERFORM VARYING control flow
- GO TO and paragraph/section flow
- Structured IL generation
- AOT/WASM‑safe branching

This document governs how CobolSharp implements COBOL’s branching and control‑flow model on .NET.

------------------------------------------------------------
SECTION 1 — EVALUATE STATEMENT OVERVIEW
------------------------------------------------------------

EVALUATE is COBOL’s multi‑branch selection construct.

Forms supported:
1. EVALUATE TRUE  
2. EVALUATE expression  
3. EVALUATE subject‑1 ALSO subject‑2 ALSO subject‑3 …

Each WHEN clause may contain:
- Single value
- Value range
- Condition
- Multiple comma‑separated values
- ALSO combinations

CobolSharp implements:
- Deterministic matching
- First‑match wins
- No fall‑through
- Structured IL (no irreducible flow)

------------------------------------------------------------
SECTION 2 — EVALUATE TRUE
------------------------------------------------------------

2.1 Semantics
-------------
EVALUATE TRUE
    WHEN condition‑1
    WHEN condition‑2
    WHEN OTHER

Each WHEN is evaluated as a boolean expression.

2.2 Lowering
------------
Equivalent to:
IF condition‑1 THEN …
ELSE IF condition‑2 THEN …
ELSE …

------------------------------------------------------------
SECTION 3 — EVALUATE expression
------------------------------------------------------------

3.1 Numeric expression
----------------------
EVALUATE x
    WHEN 1
    WHEN 2 THRU 5
    WHEN OTHER

Lowering:
- Evaluate x once
- Compare against each WHEN value/range

3.2 Alphanumeric expression
---------------------------
EVALUATE str
    WHEN "A"
    WHEN "B" "C"
    WHEN OTHER

Lowering:
- String.CompareOrdinal

3.3 National expression
-----------------------
EVALUATE nstr
    WHEN N"A"
    WHEN OTHER

Lowering:
- UTF‑16 binary comparison

------------------------------------------------------------
SECTION 4 — MULTIPLE SUBJECTS (ALSO)
------------------------------------------------------------

4.1 Example
-----------
EVALUATE a ALSO b
    WHEN 1 ALSO 2
    WHEN 3 ALSO ANY
    WHEN OTHER

4.2 Semantics
-------------
Each subject is matched independently:
- ANY matches any value
- Ranges allowed
- Conditions allowed

4.3 Lowering
------------
Compiler generates:
if (match(a,1) && match(b,2)) goto WHEN1;
if (match(a,3) && match(b,ANY)) goto WHEN2;
goto OTHER;

------------------------------------------------------------
SECTION 5 — WHEN CLAUSE SEMANTICS
------------------------------------------------------------

5.1 WHEN literal
----------------
WHEN 5 → match if subject = 5

5.2 WHEN literal range
----------------------
WHEN 1 THRU 10 → inclusive range

5.3 WHEN condition
------------------
WHEN x > 10 → boolean expression

5.4 WHEN multiple values
------------------------
WHEN 1 2 3 → match if subject ∈ {1,2,3}

5.5 WHEN ANY
------------
Matches any value.

5.6 WHEN OTHER
--------------
Executed only if no other WHEN matches.

------------------------------------------------------------
SECTION 6 — IF / ELSE SEMANTICS
------------------------------------------------------------

6.1 Basic form
--------------
IF condition
    statement
ELSE
    statement

6.2 Short‑circuit evaluation
----------------------------
AND / OR follow short‑circuit rules.

6.3 Nested IF
-------------
Compiler generates structured IL:
- No dangling ELSE ambiguity
- No irreducible flow

------------------------------------------------------------
SECTION 7 — PERFORM UNTIL / PERFORM VARYING
------------------------------------------------------------

7.1 PERFORM UNTIL
-----------------
PERFORM para UNTIL condition

Lowering:
loop_start:
    if (condition) goto loop_end;
    call para;
    goto loop_start;
loop_end:

7.2 PERFORM VARYING
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

7.3 Nested VARYING
------------------
Compiler generates nested loops with structured IL.

------------------------------------------------------------
SECTION 8 — GO TO SEMANTICS
------------------------------------------------------------

8.1 GO TO paragraph
-------------------
Transfers control to paragraph entry.

8.2 GO TO DEPENDING ON
-----------------------
GO TO para‑1 para‑2 para‑3 DEPENDING ON x

Lowering:
switch(x):
    case 1: goto para‑1;
    case 2: goto para‑2;
    case 3: goto para‑3;
    default: runtime error

8.3 Interaction with PERFORM stack
----------------------------------
GO TO may exit PERFORM range:
- Compiler unwinds PERFORM stack
- Ensures correct return behavior

------------------------------------------------------------
SECTION 9 — PARAGRAPH & SECTION FLOW
------------------------------------------------------------

9.1 Paragraph entry
-------------------
Paragraph label defines:
- Sequence point
- Branch target

9.2 Fall‑through
----------------
Allowed:
Para‑A.
    …
Para‑B.
    …

Execution flows from A into B unless:
- EXIT PARAGRAPH
- GO TO
- PERFORM

9.3 EXIT PARAGRAPH
------------------
Returns to caller of PERFORM.

9.4 EXIT SECTION
----------------
Returns to caller of PERFORM THRU.

------------------------------------------------------------
SECTION 10 — CIL LOWERING RULES
------------------------------------------------------------

10.1 EVALUATE lowering
----------------------
Compiler emits:
- Expression evaluation
- Comparison chain
- Branch table (if numeric and dense)
- Structured blocks

10.2 IF lowering
----------------
Emit:
brfalse / brtrue  
No stack manipulation

10.3 PERFORM lowering
---------------------
Emit:
- Loop blocks
- PERFORM stack push/pop
- Structured IL

10.4 GO TO lowering
-------------------
Emit:
br to paragraph label  
Unwind PERFORM stack if needed

10.5 WHEN OTHER lowering
------------------------
Emit:
goto default_label

------------------------------------------------------------
SECTION 11 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Current EVALUATE subject values
- Matched WHEN clause
- IF condition results
- Loop variables for PERFORM VARYING
- PERFORM stack state
- Paragraph/section transitions

------------------------------------------------------------
SECTION 12 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

12.1 Multiple WHEN matches
--------------------------
Only first match executes.

12.2 EVALUATE with no WHEN OTHER
--------------------------------
If no match:
- Execution continues after END‑EVALUATE

12.3 GO TO into middle of PERFORM
---------------------------------
Allowed; PERFORM stack unwound.

12.4 PERFORM VARYING with negative BY
-------------------------------------
Allowed; loop may never terminate.

12.5 EVALUATE with mixed types
------------------------------
Illegal.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Control‑Flow Architecture:
- Implements full COBOL EVALUATE, IF, PERFORM, and GO TO semantics
- Supports multi‑subject EVALUATE with ANY, ranges, and conditions
- Ensures structured, verifiable IL with no irreducible flow
- Provides deterministic branching and loop behavior
- Integrates tightly with ExecutionContext and PERFORM stack
- Generates clean, debugger‑friendly CIL
- Ensures correctness across CoreCLR, AOT, and WASM
