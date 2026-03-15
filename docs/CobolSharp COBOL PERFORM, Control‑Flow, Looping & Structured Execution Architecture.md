CobolSharp COBOL PERFORM, Control‑Flow, Looping & Structured Execution Architecture (CIL‑Only)
==============================================================================================

Purpose
-------
Define the authoritative architecture for:
- PERFORM paragraph/section
- PERFORM THRU
- PERFORM UNTIL
- PERFORM VARYING
- PERFORM TIMES
- PERFORM WITH TEST BEFORE/AFTER
- EXIT PARAGRAPH / EXIT SECTION
- Structured control‑flow lowering
- PERFORM stack model
- GO TO interaction
- Declarative interaction
- CIL‑friendly lowering
- AOT/WASM‑safe execution

This document governs how CobolSharp implements COBOL’s structured execution and looping semantics.

------------------------------------------------------------
SECTION 1 — PERFORM OVERVIEW
------------------------------------------------------------

CobolSharp supports all PERFORM forms:
- PERFORM paragraph
- PERFORM paragraph THRU paragraph
- PERFORM UNTIL condition
- PERFORM WITH TEST BEFORE/AFTER
- PERFORM VARYING
- PERFORM TIMES
- Nested PERFORMs
- Recursive PERFORMs

PERFORM uses:
- A PERFORM stack
- Structured entry/exit labels
- Deterministic control‑flow

------------------------------------------------------------
SECTION 2 — PERFORM PARAGRAPH / SECTION
------------------------------------------------------------

2.1 Basic form
--------------
PERFORM ParaA.

Semantics:
- Push return address on PERFORM stack
- Branch to ParaA
- Execute until:
  - EXIT PARAGRAPH
  - EXIT SECTION
  - End of paragraph
- Pop PERFORM stack
- Return to caller

2.2 PERFORM THRU
----------------
PERFORM ParaA THRU ParaC.

Semantics:
- Execute ParaA, ParaB, ParaC
- Return after ParaC completes

2.3 Paragraph boundaries
------------------------
Paragraph ends at:
- Next paragraph label
- End of PROCEDURE DIVISION

------------------------------------------------------------
SECTION 3 — PERFORM UNTIL
------------------------------------------------------------

3.1 Basic form
--------------
PERFORM ParaA UNTIL condition.

Lowering:
loopStart:
    if (condition) br loopEnd
    call ParaA
    br loopStart
loopEnd:

3.2 WITH TEST BEFORE (default)
------------------------------
Condition checked before first iteration.

3.3 WITH TEST AFTER
-------------------
Condition checked after first iteration:
loopStart:
    call ParaA
    if (condition) br loopEnd
    br loopStart

------------------------------------------------------------
SECTION 4 — PERFORM TIMES
------------------------------------------------------------

4.1 Basic form
--------------
PERFORM ParaA 5 TIMES.

Lowering:
counter = 1
loopStart:
    if (counter > 5) br loopEnd
    call ParaA
    counter++
    br loopStart
loopEnd:

4.2 TIMES with variable
-----------------------
PERFORM ParaA n TIMES.

n evaluated once at loop start.

------------------------------------------------------------
SECTION 5 — PERFORM VARYING
------------------------------------------------------------

5.1 Basic form
--------------
PERFORM ParaA
    VARYING i FROM 1 BY 1
    UNTIL i > 10.

Lowering:
i = 1
loopStart:
    if (i > 10) br loopEnd
    call ParaA
    i = i + 1
    br loopStart
loopEnd:

5.2 Nested VARYING
------------------
PERFORM ParaA
    VARYING i FROM 1 BY 1 UNTIL i > 10
        AFTER j FROM 1 BY 2 UNTIL j > 20.

Lowering:
i = 1
j = 1
loopStart:
    if (i > 10) br loopEnd
    if (j > 20) {
        j = 1
        i = i + 1
        br loopStart
    }
    call ParaA
    j = j + 2
    br loopStart
loopEnd:

5.3 VARYING with multiple AFTER phrases
---------------------------------------
Supported; compiler generates nested increment logic.

------------------------------------------------------------
SECTION 6 — EXIT PARAGRAPH / EXIT SECTION
------------------------------------------------------------

6.1 EXIT PARAGRAPH
------------------
- Pops PERFORM stack
- Branches to return label

6.2 EXIT SECTION
----------------
- Pops PERFORM stack
- Branches to return label
- Skips remaining paragraphs in section

6.3 EXIT PERFORM (not ISO)
--------------------------
Not supported.

------------------------------------------------------------
SECTION 7 — PERFORM STACK ARCHITECTURE
------------------------------------------------------------

7.1 PERFORM frame
-----------------
Contains:
- Return label
- Range (for THRU)
- Loop variables (for VARYING)
- Loop bounds (for TIMES)
- Test mode (BEFORE/AFTER)

7.2 Push
--------
On PERFORM:
- Push frame with return label

7.3 Pop
-------
On EXIT PARAGRAPH/SECTION or end of range:
- Pop frame
- Branch to return label

7.4 GO TO interaction
---------------------
GO TO may:
- Jump out of PERFORM range
- Unwind PERFORM stack
- Skip return label

------------------------------------------------------------
SECTION 8 — CONTROL‑FLOW LOWERING
------------------------------------------------------------

8.1 Paragraph call lowering
---------------------------
call ParaA:
- Branch to ParaA entry label
- ParaA ends with:
    leave returnLabel

8.2 THRU lowering
-----------------
Compiler marks:
- End paragraph of range
- Inserts return label after end paragraph

8.3 UNTIL lowering
------------------
Condition lowered to:
- Boolean expression
- Branch to loopEnd

8.4 VARYING lowering
--------------------
Compiler generates:
- Initialization block
- Loop condition block
- Body block
- Increment block

8.5 TEST BEFORE/AFTER lowering
------------------------------
TEST BEFORE:
    if (cond) exit

TEST AFTER:
    body
    if (cond) exit

------------------------------------------------------------
SECTION 9 — DECLARATIVE INTERACTION
------------------------------------------------------------

9.1 Declarative triggered inside PERFORM
----------------------------------------
- Declarative runs
- Returns to statement after failing statement
- PERFORM stack preserved

9.2 EXIT PARAGRAPH inside declarative
-------------------------------------
- Pops PERFORM stack
- Returns to caller of PERFORM

------------------------------------------------------------
SECTION 10 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- PERFORM stack
- Loop variables
- Loop bounds
- Return label
- Current paragraph
- THRU range
- TEST BEFORE/AFTER mode

------------------------------------------------------------
SECTION 11 — AOT/WASM‑SAFE EXECUTION
------------------------------------------------------------

11.1 No recursion in IL
-----------------------
PERFORM uses:
- Structured loops
- No dynamic codegen

11.2 No unsafe code
-------------------
- No pointers
- No stackalloc

11.3 Deterministic control‑flow
-------------------------------
- Identical across platforms

------------------------------------------------------------
SECTION 12 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

12.1 PERFORM with zero iterations
---------------------------------
UNTIL true → zero iterations  
TIMES 0 → zero iterations

12.2 VARYING with negative BY
-----------------------------
Allowed; may create infinite loop.

12.3 GO TO into middle of PERFORM
---------------------------------
Compiler restructures blocks to preserve verifiability.

12.4 EXIT PARAGRAPH inside nested PERFORM
-----------------------------------------
Pops only top frame.

12.5 PERFORM THRU with empty range
----------------------------------
Executes only start paragraph.

12.6 PERFORM inside declarative
-------------------------------
Allowed.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp PERFORM Architecture:
- Implements full COBOL structured control‑flow semantics
- Supports all PERFORM forms with precise loop semantics
- Provides deterministic, verifiable CIL lowering
- Integrates tightly with ExecutionContext and PERFORM stack
- Ensures correctness across CoreCLR, AOT, and WASM
