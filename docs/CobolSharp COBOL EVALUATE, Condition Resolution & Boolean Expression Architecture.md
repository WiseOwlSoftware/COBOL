CobolSharp COBOL EVALUATE, Condition Resolution & Boolean Expression Architecture (CIL‑Only)
============================================================================================

Purpose
-------
Define the authoritative architecture for:
- EVALUATE statement
- Boolean expression lowering
- Condition resolution (class tests, sign tests, range tests)
- Subject/value matching
- TRUE/FALSE evaluation
- ANY, OTHER, THROUGH semantics
- NOT, AND, OR, short‑circuiting
- Numeric, alphanumeric, national comparisons
- CIL‑friendly lowering
- AOT/WASM‑safe branching

This document governs how CobolSharp implements COBOL’s conditional logic and multi‑branch evaluation.

------------------------------------------------------------
SECTION 1 — EVALUATE OVERVIEW
------------------------------------------------------------

CobolSharp implements full ISO/IEC 1989:2023 EVALUATE semantics:
- EVALUATE subject(s)
- WHEN value(s)
- WHEN ANY
- WHEN OTHER
- WHEN value1 THROUGH value2
- WHEN condition
- WHEN TRUE/FALSE
- WHEN multiple subjects
- WHEN ALSO chaining

EVALUATE is lowered to:
- A structured if/else chain
- With short‑circuit evaluation
- With deterministic matching order

------------------------------------------------------------
SECTION 2 — EVALUATE FORMS
------------------------------------------------------------

2.1 EVALUATE TRUE
-----------------
EVALUATE TRUE
    WHEN condition1
    WHEN condition2
    WHEN OTHER
END‑EVALUATE.

Equivalent to:
IF condition1
ELSE IF condition2
ELSE
END‑IF.

2.2 EVALUATE subject
--------------------
EVALUATE x
    WHEN 1
    WHEN 2
    WHEN OTHER
END‑EVALUATE.

2.3 EVALUATE multiple subjects
------------------------------
EVALUATE x ALSO y
    WHEN 1 ALSO 2
    WHEN 3 ALSO ANY
    WHEN OTHER
END‑EVALUATE.

2.4 EVALUATE with ranges
------------------------
WHEN 1 THROUGH 5  
WHEN "A" THROUGH "Z"

2.5 EVALUATE with conditions
----------------------------
WHEN x > 10  
WHEN x = y

------------------------------------------------------------
SECTION 3 — MATCHING RULES
------------------------------------------------------------

3.1 Matching order
------------------
WHEN clauses evaluated:
- Top to bottom
- First match wins
- WHEN OTHER is fallback

3.2 Subject/value matching
--------------------------
Match succeeds if:
- subject == value
- subject in range
- subject matches class test
- ANY always matches

3.3 Multiple subjects
---------------------
All subjects must match corresponding WHEN values:
EVALUATE A ALSO B
    WHEN 1 ALSO 2  → A=1 AND B=2

3.4 THROUGH ranges
------------------
Numeric:
value1 ≤ subject ≤ value2

Alphanumeric:
Lexicographic comparison

National:
UTF‑16 code point comparison

------------------------------------------------------------
SECTION 4 — BOOLEAN EXPRESSION ARCHITECTURE
------------------------------------------------------------

4.1 Operators
-------------
- NOT
- AND
- OR
- =, <>, >, <, >=, <=
- Class tests (NUMERIC, ALPHABETIC)
- Sign tests (POSITIVE, NEGATIVE, ZERO)

4.2 Short‑circuiting
--------------------
A AND B:
- If A is false → skip B

A OR B:
- If A is true → skip B

4.3 NOT
-------
NOT applies to:
- Boolean expressions
- Class tests
- Sign tests

4.4 Parentheses
---------------
Fully supported; compiler builds AST.

------------------------------------------------------------
SECTION 5 — CLASS TESTS
------------------------------------------------------------

5.1 NUMERIC
-----------
True if:
- All characters are digits
- Optional sign allowed
- No spaces unless allowed by PIC

5.2 ALPHABETIC
--------------
True if:
- All characters A–Z or a–z
- NATIONAL → Unicode letters

5.3 ALPHABETIC‑LOWER / UPPER
----------------------------
True if:
- All characters are lowercase/uppercase letters

5.4 NATIONAL tests
------------------
Use Unicode categories.

------------------------------------------------------------
SECTION 6 — SIGN TESTS
------------------------------------------------------------

6.1 POSITIVE
------------
True if:
- Numeric value > 0

6.2 NEGATIVE
------------
True if:
- Numeric value < 0

6.3 ZERO
--------
True if:
- Numeric value = 0

------------------------------------------------------------
SECTION 7 — COMPARISON RULES
------------------------------------------------------------

7.1 Numeric comparison
----------------------
- Decimal comparison
- COMP/COMP‑3/COMP‑5 converted to Decimal

7.2 Alphanumeric comparison
---------------------------
DISPLAY:
- ASCII lexicographic

NATIONAL:
- UTF‑16 lexicographic

7.3 Mixed types
---------------
DISPLAY vs NATIONAL:
- NATIONAL converted to DISPLAY if ASCII
- Else runtime error

------------------------------------------------------------
SECTION 8 — CIL LOWERING RULES
------------------------------------------------------------

8.1 EVALUATE lowering
---------------------
Compiler generates:
- Temporary locals for subjects
- If/else chain
- Branches for each WHEN
- Final branch for WHEN OTHER

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

8.2 Boolean expression lowering
-------------------------------
A AND B:
- Evaluate A
- brfalse end
- Evaluate B

A OR B:
- Evaluate A
- brtrue end
- Evaluate B

8.3 Class test lowering
-----------------------
Call:
StringEngine.IsNumeric  
StringEngine.IsAlphabetic  
etc.

8.4 Range lowering
------------------
value1 ≤ subject ≤ value2:
- Compare subject >= value1
- Compare subject <= value2
- AND

8.5 Multiple subjects lowering
------------------------------
Each subject/value pair lowered independently.

------------------------------------------------------------
SECTION 9 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Subject values
- WHEN clause matched
- Boolean expression tree
- Class/sign test results
- Range boundaries
- Short‑circuit behavior

------------------------------------------------------------
SECTION 10 — AOT/WASM‑SAFE EXECUTION
------------------------------------------------------------

10.1 No dynamic codegen
-----------------------
All branching static.

10.2 No unsafe code
-------------------
No pointers or stackalloc.

10.3 Deterministic evaluation
-----------------------------
Same results across platforms.

------------------------------------------------------------
SECTION 11 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

11.1 Multiple WHEN matches
--------------------------
First match wins.

11.2 WHEN OTHER missing
------------------------
No action taken if no match.

11.3 Range with reversed bounds
-------------------------------
value1 > value2 → range never matches.

11.4 Class test on COMP/COMP‑3
-------------------------------
False.

11.5 NATIONAL with surrogate pairs
----------------------------------
Counted as single character.

11.6 EVALUATE TRUE with no WHEN TRUE
------------------------------------
Falls through to WHEN OTHER.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp EVALUATE & Condition Architecture:
- Implements full COBOL conditional semantics
- Provides deterministic, short‑circuit boolean evaluation
- Supports multi‑subject matching, ranges, class tests, and sign tests
- Generates clean, verifiable, debugger‑friendly CIL
- Ensures correctness across CoreCLR, AOT, and WASM
