CobolSharp COBOL PERFORM & Control‑Flow Lowering Architecture (CIL‑Only)
=======================================================================

Purpose
-------
Define the authoritative architecture for:
- PERFORM statement semantics
- PERFORM THRU range execution
- PERFORM UNTIL loop lowering
- PERFORM VARYING loop lowering
- PERFORM TIMES lowering
- GO TO interactions
- Paragraph/section call semantics
- Structured control‑flow generation
- CIL‑friendly lowering rules
- PERFORM stack behavior
- Debugger integration

This document governs how CobolSharp transforms COBOL control flow into structured, verifiable .NET CIL.

------------------------------------------------------------
SECTION 1 — CONTROL‑FLOW MODEL OVERVIEW
------------------------------------------------------------

CobolSharp uses a **structured control‑flow model** that preserves COBOL semantics while generating clean, verifiable CIL.

Key principles:
- PERFORM is lowered to structured loops and calls.
- PERFORM THRU becomes a structured block with explicit end.
- PERFORM UNTIL becomes a while‑loop.
- PERFORM VARYING becomes a for‑loop.
- GO TO is allowed but must not break structured semantics.
- Paragraphs and sections become callable blocks.
- PERFORM stack tracks active PERFORM frames.
- Debugger sees paragraph/section boundaries.

------------------------------------------------------------
SECTION 2 — PARAGRAPH & SECTION EXECUTION MODEL
------------------------------------------------------------

2.1 Paragraphs
--------------
Paragraphs are:
- Named blocks of code
- Invoked by PERFORM or GO TO
- May fall through to next paragraph unless terminated

Lowering:
- Each paragraph becomes a CIL basic block with a label
- Optionally lifted into a helper method (configurable)

2.2 Sections
------------
Sections:
- Contain multiple paragraphs
- Execute paragraphs in order
- May be PERFORMed as a unit

Lowering:
- Section entry label
- Paragraph labels inside
- Fall‑through preserved

2.3 Paragraph fall‑through
--------------------------
If a paragraph ends without:
- EXIT PARAGRAPH
- GO TO
- PERFORM
- STOP RUN
- GOBACK

Then execution continues to the next paragraph.

CobolSharp preserves this behavior.

------------------------------------------------------------
SECTION 3 — PERFORM THRU LOWERING
------------------------------------------------------------

3.1 Semantics
-------------
PERFORM A THRU B:
- Execute paragraph A
- Continue through paragraphs until B
- Stop after B completes

3.2 Lowering strategy
---------------------
CobolSharp lowers PERFORM THRU to:

1. Push PERFORM frame  
2. Jump to paragraph A  
3. Execute paragraphs until B  
4. Jump to return label  
5. Pop PERFORM frame  

3.3 Return label
----------------
Each PERFORM THRU generates a unique return label.

3.4 GO TO inside PERFORM THRU
-----------------------------
Allowed.

Rules:
- If GO TO jumps outside the range, PERFORM frame must unwind.
- If GO TO jumps inside the range, execution continues normally.

------------------------------------------------------------
SECTION 4 — PERFORM UNTIL LOWERING
------------------------------------------------------------

4.1 Semantics
-------------
PERFORM UNTIL condition:
- Test condition at top of loop
- If false → execute body
- Repeat until condition true

4.2 Lowering strategy
---------------------
Lowered to:

loop_start:
    if (condition) goto loop_end
    body
    goto loop_start
loop_end:

4.3 Condition evaluation
------------------------
- Boolean expressions lowered to NumericEngine or string comparison
- Condition evaluated before each iteration

4.4 EXIT PERFORM
----------------
Lowered to:
- Jump to loop_end

------------------------------------------------------------
SECTION 5 — PERFORM VARYING LOWERING
------------------------------------------------------------

5.1 Semantics
-------------
PERFORM VARYING var FROM start BY step UNTIL condition

Equivalent to:

var = start
while (NOT condition):
    body
    var = var + step

5.2 Lowering strategy
---------------------
Lowered to:

var = start
loop_start:
    if (condition) goto loop_end
    body
    var = var + step
    goto loop_start
loop_end:

5.3 Nested VARYING
------------------
Nested loops generate nested structured loops.

5.4 EXIT PERFORM
----------------
Jumps to loop_end of the innermost PERFORM.

------------------------------------------------------------
SECTION 6 — PERFORM TIMES LOWERING
------------------------------------------------------------

6.1 Semantics
-------------
PERFORM body TIMES n

Equivalent to:

for i = 1 to n:
    body

6.2 Lowering strategy
---------------------
Lowered to:

i = 1
loop_start:
    if (i > n) goto loop_end
    body
    i = i + 1
    goto loop_start
loop_end:

------------------------------------------------------------
SECTION 7 — PERFORM STACK ARCHITECTURE
------------------------------------------------------------

CobolSharp maintains a PERFORM stack for:
- Debugging
- Exception handling
- EXIT PERFORM
- GO TO interactions

7.1 PERFORM frame contents
--------------------------
- Type (THRU, UNTIL, VARYING, TIMES)
- Return label
- Loop variables (if applicable)
- Termination condition (if applicable)
- Paragraph range (if THRU)

7.2 Push/pop rules
------------------
Push:
- At start of PERFORM

Pop:
- At natural end
- At EXIT PERFORM
- At GO TO that leaves the range

7.3 GO TO interactions
----------------------
If GO TO jumps:
- Inside PERFORM → no change
- Outside PERFORM → pop frame

------------------------------------------------------------
SECTION 8 — GO TO LOWERING
------------------------------------------------------------

8.1 Semantics
-------------
GO TO label:
- Unconditional branch
- May cross paragraph boundaries
- May exit PERFORM ranges

8.2 Lowering strategy
---------------------
Lowered to:
- br instruction to target label
- PERFORM stack unwinding logic

8.3 GO TO DEPENDING ON
-----------------------
Lowered to:
- switch instruction
- Jump to appropriate label

------------------------------------------------------------
SECTION 9 — EVALUATE LOWERING
------------------------------------------------------------

9.1 Semantics
-------------
EVALUATE is COBOL’s switch/case.

9.2 Lowering strategy
---------------------
If all WHEN values are numeric:
- Lower to switch

Otherwise:
- Lower to if/else chain

------------------------------------------------------------
SECTION 10 — IF/ELSE LOWERING
------------------------------------------------------------

10.1 Semantics
--------------
IF condition THEN
    statements
ELSE
    statements
END-IF

10.2 Lowering strategy
----------------------
Lowered to:

if (!condition) goto else_label
then_block
goto end_label
else_label:
else_block
end_label:

------------------------------------------------------------
SECTION 11 — CIL‑FRIENDLY STRUCTURED CONTROL FLOW
------------------------------------------------------------

CobolSharp guarantees:
- No irreducible control flow
- No unstructured loops
- No backward GO TO without loop structure
- All loops have explicit entry/exit labels
- All PERFORMs become structured constructs

This ensures:
- Verifiable IL
- Debuggable IL
- Optimizable IL

------------------------------------------------------------
SECTION 12 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger sees:
- Paragraph entry points
- Section entry points
- PERFORM frames
- Loop variables
- Condition evaluation
- GO TO jumps
- Structured stepping

Sequence points emitted for:
- PERFORM entry
- PERFORM exit
- Loop start
- Loop end
- Condition evaluation

------------------------------------------------------------
SECTION 13 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

13.1 PERFORM THRU with empty range
----------------------------------
Allowed:
- Executes only the starting paragraph

13.2 PERFORM UNTIL with initial true condition
----------------------------------------------
- Executes zero times

13.3 PERFORM VARYING with negative step
---------------------------------------
Allowed:
- Loop executes until condition true

13.4 GO TO into middle of PERFORM
---------------------------------
Allowed:
- PERFORM frame remains active

13.5 GO TO out of nested PERFORMs
---------------------------------
- All exited frames are popped

13.6 EXIT PERFORM inside nested loops
-------------------------------------
- Exits only innermost PERFORM

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp PERFORM & Control‑Flow Lowering Architecture:
- Implements full COBOL control‑flow semantics
- Lowers PERFORM to structured loops and blocks
- Handles GO TO safely and predictably
- Maintains a PERFORM stack for correctness and debugging
- Generates clean, verifiable, optimizable CIL
- Preserves paragraph/section semantics
- Integrates deeply with the debugger and runtime
- Forms the backbone of COBOL execution on .NET
