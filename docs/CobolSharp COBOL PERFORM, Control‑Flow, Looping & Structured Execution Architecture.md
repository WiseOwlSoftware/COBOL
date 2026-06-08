CobolSharp COBOL PERFORM, Control‑Flow, Looping & Structured Execution Architecture (CIL‑Only)
==============================================================================================

> **STATUS BANNER — design reference, 2026-06-07.**
> This is a **target-design / architecture reference** for PERFORM and structured control‑flow lowering.
> **ACTUAL implementation status: ~80–90% implemented and LIVE.** PERFORM (paragraph/section, THRU, UNTIL,
> VARYING incl. nested AFTER, TIMES, WITH TEST BEFORE/AFTER), EXIT PERFORM/PARAGRAPH/SECTION/CYCLE, GO TO,
> GO TO DEPENDING ON, EVALUATE, and IF/ELSE all lower in `src/CobolSharp.Compiler/CodeGen/Lowering/ControlFlowLowerer.cs`
> (bound nodes in `Semantics/Bound/`, IR in `IR/IrInstruction.cs`, emit via the Cecil emitter). Verify any
> specific claim against `src/` before relying on it — some sub-behaviors below (e.g. a first-class PERFORM
> "stack/frame" runtime object, full declarative re-entry, AOT/WASM guarantees) describe the **target model**;
> the live engine lowers PERFORM to structured IR loops/calls rather than a runtime frame stack.
> **Stack: .NET 10 / C# 14.** Backend is **CIL‑only via Mono.Cecil** (no custom VM / no bytecode interpreter;
> a Roslyn C# backend is a FUTURE additive Stage‑5, with Cecil as the oracle).
> Plan SSOT: `docs/MASTER_PLAN.md`. Doctrine: `PROMPT.md`. Live lowering: `CodeGen/Lowering/`.

Purpose
-------
Define the authoritative architecture for:
- PERFORM paragraph/section
- PERFORM THRU
- PERFORM UNTIL
- PERFORM VARYING (incl. nested AFTER)
- PERFORM TIMES
- PERFORM WITH TEST BEFORE/AFTER
- EXIT PARAGRAPH / EXIT SECTION / EXIT PERFORM / EXIT PERFORM CYCLE
- GO TO and GO TO DEPENDING ON
- EVALUATE (COBOL switch/case) lowering
- IF/ELSE lowering
- Structured control‑flow lowering
- PERFORM stack model
- Declarative interaction
- CIL‑friendly lowering
- Debugger integration
- AOT/WASM‑safe execution

This document governs how CobolSharp implements COBOL’s structured execution and looping semantics,
transforming COBOL control flow into structured, verifiable .NET CIL.

------------------------------------------------------------
SECTION 0 — CONTROL‑FLOW MODEL OVERVIEW
------------------------------------------------------------

CobolSharp uses a **structured control‑flow model** that preserves COBOL semantics while generating clean,
verifiable CIL.

Key principles:
- PERFORM is lowered to structured loops and calls.
- PERFORM THRU becomes a structured block with explicit end.
- PERFORM UNTIL becomes a while‑loop.
- PERFORM VARYING becomes a for‑loop.
- PERFORM TIMES becomes a counted for‑loop.
- GO TO is allowed but must not break structured (verifiable) semantics.
- Paragraphs and sections become callable blocks.
- A PERFORM stack/frame model tracks active PERFORM context for debugging and EXIT handling.
- Debugger sees paragraph/section boundaries.

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
- A PERFORM stack/frame model (target design; the live engine lowers to structured IR)
- Structured entry/exit labels
- Deterministic control‑flow

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

then execution continues to the next paragraph. CobolSharp preserves this behavior.

2.4 Paragraph boundaries
------------------------
A paragraph ends at:
- The next paragraph label
- End of PROCEDURE DIVISION

------------------------------------------------------------
SECTION 3 — PERFORM PARAGRAPH / SECTION
------------------------------------------------------------

3.1 Basic form
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

3.2 Paragraph call lowering
---------------------------
    call ParaA:
    - Branch to ParaA entry label
    - ParaA ends with:
          leave returnLabel

------------------------------------------------------------
SECTION 4 — PERFORM THRU LOWERING
------------------------------------------------------------

4.1 Semantics
-------------
    PERFORM A THRU B:
    - Execute paragraph A
    - Continue through paragraphs until B
    - Stop after B completes

4.2 Lowering strategy
---------------------
CobolSharp lowers PERFORM THRU to:
1. Push PERFORM frame
2. Jump to paragraph A
3. Execute paragraphs until B
4. Jump to return label
5. Pop PERFORM frame

The compiler marks the end paragraph of the range and inserts a return label after the end paragraph.
(Live IR: `IrPerformThru(startIdx, endIdx, methods)`.)

4.3 Return label
----------------
Each PERFORM THRU generates a unique return label.

4.4 GO TO inside PERFORM THRU
-----------------------------
Allowed.
- If GO TO jumps outside the range, the PERFORM frame must unwind.
- If GO TO jumps inside the range, execution continues normally.

------------------------------------------------------------
SECTION 5 — PERFORM UNTIL LOWERING
------------------------------------------------------------

5.1 Semantics
-------------
    PERFORM ParaA UNTIL condition:
    - Test condition at top of loop (default = WITH TEST BEFORE)
    - If false → execute body
    - Repeat until condition true

5.2 Lowering strategy (TEST BEFORE, default)
--------------------------------------------
    loopStart:
        if (condition) br loopEnd
        body / call ParaA
        br loopStart
    loopEnd:

5.3 WITH TEST AFTER
-------------------
Condition checked after the first iteration:
    loopStart:
        body / call ParaA
        if (condition) br loopEnd
        br loopStart

(Live: `BoundPerformStatement.IsTestAfter` drives the lowering branch in `ControlFlowLowerer`.)

5.4 Condition evaluation
------------------------
- Boolean expressions lowered to NumericEngine or string comparison
- Condition evaluated before (BEFORE) or after (AFTER) each iteration

------------------------------------------------------------
SECTION 6 — PERFORM TIMES LOWERING
------------------------------------------------------------

6.1 Semantics
-------------
    PERFORM ParaA n TIMES   ≡   for i = 1 to n: body

6.2 Lowering strategy
---------------------
    counter = 1
    loopStart:
        if (counter > n) br loopEnd
        body / call ParaA
        counter++
        br loopStart
    loopEnd:

6.3 TIMES with variable
-----------------------
    PERFORM ParaA n TIMES.
`n` is evaluated **once** at loop start.

(Live IR: `IrPerformTimes(...)`; an inline form is generated by `LowerInlinePerformTimes` when the body
is inline rather than a named paragraph.)

------------------------------------------------------------
SECTION 7 — PERFORM VARYING LOWERING
------------------------------------------------------------

7.1 Basic form
--------------
    PERFORM ParaA VARYING i FROM 1 BY 1 UNTIL i > 10

Equivalent to:
    i = 1
    while (NOT condition): body; i = i + step

Lowering:
    i = 1
    loopStart:
        if (i > 10) br loopEnd
        body / call ParaA
        i = i + 1
        br loopStart
    loopEnd:

7.2 Nested VARYING (AFTER)
--------------------------
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
        body / call ParaA
        j = j + 2
        br loopStart
    loopEnd:

Nested loops generate nested structured loops. (Live: `LowerPerformVarying` recurses over the linked
`BoundPerformVarying.Next` AFTER levels and re-initializes inner indices via `EmitAfterReinitialization`.)

7.3 VARYING with multiple AFTER phrases
---------------------------------------
Supported; the compiler generates nested increment / re-initialization logic per AFTER level.

7.4 TEST BEFORE/AFTER with VARYING
----------------------------------
TEST BEFORE: condition tested before the body.
TEST AFTER:  body executed once, then condition tested (`perf.IsTestAfter`).

------------------------------------------------------------
SECTION 8 — EXIT STATEMENTS
------------------------------------------------------------

8.1 EXIT PARAGRAPH
------------------
- Pops PERFORM stack
- Branches to return label

8.2 EXIT SECTION
----------------
- Pops PERFORM stack
- Branches to return label
- Skips remaining paragraphs in the section

8.3 EXIT PERFORM
----------------
**SUPPORTED.** (ISO 2002+; live: `BoundExitPerformStatement` → `LowerExitPerform`.)
Lowered to a jump to the `loopEnd` of the innermost in‑line PERFORM. It is implemented and exercised by the
test corpus.

8.4 EXIT PERFORM CYCLE
----------------------
**SUPPORTED.** Lowered to a jump to the increment/condition step of the innermost in‑line PERFORM
(continue the loop), rather than the loop end.

8.5 Nested‑loop exit semantics
------------------------------
- EXIT PERFORM exits only the **innermost** PERFORM.
- EXIT PARAGRAPH inside a nested PERFORM pops only the top frame.

------------------------------------------------------------
SECTION 9 — GO TO LOWERING
------------------------------------------------------------

9.1 Semantics
-------------
    GO TO label:
    - Unconditional branch
    - May cross paragraph boundaries
    - May exit PERFORM ranges

9.2 Lowering strategy
---------------------
- `br` instruction to the target label
- PERFORM stack unwinding logic where the branch leaves an active range

9.3 GO TO DEPENDING ON
----------------------
Lowered to a `switch`-style dispatch:
- Evaluate the selector
- Jump to the appropriate label by 1‑based index

(Live IR: `IrGoToDepending(selectorLoc, targetIndices)`.)

------------------------------------------------------------
SECTION 10 — EVALUATE LOWERING
------------------------------------------------------------

10.1 Semantics
--------------
EVALUATE is COBOL’s switch/case (subject/object, WHEN, WHEN OTHER, ranges, TRUE/FALSE).

10.2 Lowering strategy
----------------------
- If all WHEN values are numeric → lower to a `switch`.
- Otherwise → lower to an if/else chain.

(Live: `LowerEvaluate` in `ControlFlowLowerer`, with WHEN-clause helpers.)

------------------------------------------------------------
SECTION 11 — IF/ELSE LOWERING
------------------------------------------------------------

11.1 Semantics
--------------
    IF condition THEN statements ELSE statements END-IF

11.2 Lowering strategy
----------------------
    if (!condition) goto else_label
    then_block
    goto end_label
    else_label:
    else_block
    end_label:

------------------------------------------------------------
SECTION 12 — PERFORM STACK ARCHITECTURE
------------------------------------------------------------

CobolSharp models a PERFORM stack/frame for:
- Debugging
- Exception handling
- EXIT PERFORM / EXIT PARAGRAPH / EXIT SECTION
- GO TO interactions

(Target design. The live engine realizes these semantics through structured IR loops, return labels,
and index/condition blocks rather than a separate runtime frame‑stack object.)

12.1 PERFORM frame contents
---------------------------
- Type (THRU, UNTIL, VARYING, TIMES)
- Return label
- Loop variables (for VARYING)
- Loop bounds (for TIMES)
- Termination condition (for UNTIL)
- Paragraph range (for THRU)
- Test mode (BEFORE/AFTER)

12.2 Push/pop rules
-------------------
Push: at the start of PERFORM.
Pop:
- At natural end of range
- At EXIT PERFORM / EXIT PARAGRAPH / EXIT SECTION
- At a GO TO that leaves the range

12.3 GO TO interactions
-----------------------
- GO TO inside PERFORM → no frame change
- GO TO outside PERFORM → pop frame(s) for every exited range

------------------------------------------------------------
SECTION 13 — DECLARATIVE INTERACTION
------------------------------------------------------------

13.1 Declarative triggered inside PERFORM
-----------------------------------------
- Declarative runs
- Returns to the statement after the failing statement
- PERFORM stack preserved

13.2 EXIT PARAGRAPH inside a declarative
----------------------------------------
- Pops PERFORM stack
- Returns to caller of PERFORM

13.3 PERFORM inside a declarative
---------------------------------
Allowed.

------------------------------------------------------------
SECTION 14 — CIL‑FRIENDLY STRUCTURED CONTROL FLOW
------------------------------------------------------------

CobolSharp guarantees:
- No irreducible control flow
- No unstructured loops
- No backward GO TO without a loop structure
- All loops have explicit entry/exit labels
- All PERFORMs become structured constructs

This ensures verifiable, debuggable, and optimizable IL. Where a GO TO would create an irreducible region,
the compiler restructures blocks to preserve verifiability (see §16.3).

------------------------------------------------------------
SECTION 15 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger sees:
- Paragraph and section entry points
- PERFORM stack / frames
- Loop variables and bounds
- Return label
- THRU range
- TEST BEFORE/AFTER mode
- Condition evaluation
- GO TO jumps
- Structured stepping

Sequence points are emitted for:
- PERFORM entry / exit
- Loop start / loop end
- Condition evaluation

------------------------------------------------------------
SECTION 16 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

16.1 Zero iterations
--------------------
- PERFORM UNTIL with an initially‑true condition → executes zero times.
- PERFORM TIMES 0 → executes zero times.

16.2 Negative step
------------------
- PERFORM VARYING with a negative BY is allowed; the loop runs until the condition is true (may create
  an infinite loop if the condition is never reached).

16.3 GO TO into the middle of a PERFORM
---------------------------------------
- Allowed; the PERFORM frame remains active. The compiler restructures blocks to preserve verifiability.

16.4 GO TO out of nested PERFORMs
---------------------------------
- All exited frames are popped.

16.5 EXIT PERFORM inside nested loops
-------------------------------------
- Exits only the innermost PERFORM.

16.6 EXIT PARAGRAPH inside a nested PERFORM
-------------------------------------------
- Pops only the top frame.

16.7 PERFORM THRU with an empty range
-------------------------------------
- Executes only the starting paragraph.

16.8 PERFORM inside a declarative
---------------------------------
- Allowed.

------------------------------------------------------------
SECTION 17 — AOT/WASM‑SAFE EXECUTION
------------------------------------------------------------

17.1 No recursion in IL
-----------------------
PERFORM uses structured loops and no dynamic codegen.

17.2 No unsafe code
-------------------
No raw pointers, no `stackalloc`. (COBOL `USAGE POINTER` / ADDRESS OF / BASED are modeled as managed
references via `ManagedPointer`, not native pointers — see the pointer/data‑model design docs.)

17.3 Deterministic control‑flow
-------------------------------
Identical behavior across CoreCLR, AOT, and WASM.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp PERFORM & Control‑Flow Lowering Architecture:
- Implements full COBOL structured control‑flow semantics
- Supports all PERFORM forms (paragraph/section, THRU, UNTIL, VARYING incl. nested AFTER, TIMES,
  WITH TEST BEFORE/AFTER) with precise loop semantics
- Handles GO TO, GO TO DEPENDING ON, EVALUATE, and IF/ELSE safely and predictably
- Supports EXIT PERFORM / EXIT PERFORM CYCLE / EXIT PARAGRAPH / EXIT SECTION
- Models a PERFORM stack for correctness, EXIT handling, and debugging
- Generates clean, verifiable, optimizable, CIL‑only output (via Mono.Cecil)
- Preserves paragraph/section semantics and fall‑through
- Integrates with the debugger and runtime
- Ensures deterministic execution across CoreCLR, AOT, and WASM
- Forms the backbone of COBOL execution on .NET
