CobolSharp COBOL Program Lifecycle, STOP RUN, GOBACK & Runtime Termination Architecture (CIL‑Only)
================================================================================================

Purpose
-------
Define the authoritative architecture for:
- COBOL program lifecycle
- STOP RUN semantics
- GOBACK semantics
- EXIT PROGRAM / EXIT PARAGRAPH / EXIT SECTION
- Runtime termination model
- Nested program activation and teardown
- ExecutionContext lifetime rules
- File and report finalization
- CIL‑friendly lowering
- AOT/WASM‑safe termination behavior

This document governs how CobolSharp manages program startup, execution, and termination on .NET.

------------------------------------------------------------
SECTION 1 — PROGRAM LIFECYCLE OVERVIEW
------------------------------------------------------------

A COBOL program’s lifecycle consists of:
1. Program activation  
2. Storage allocation  
3. File/report initialization  
4. Procedure Division execution  
5. Termination via STOP RUN, GOBACK, or EXIT PROGRAM  
6. Cleanup and resource release  

CobolSharp models each program as:
- A .NET class
- With a static entry method
- Backed by an ExecutionContext instance

------------------------------------------------------------
SECTION 2 — PROGRAM ACTIVATION
------------------------------------------------------------

2.1 Main program
----------------
The main program:
- Creates a new ExecutionContext
- Allocates WORKING‑STORAGE
- Allocates LOCAL‑STORAGE
- Initializes FileManager, ReportEngine, NumericEngine, etc.
- Executes Procedure Division

2.2 Nested programs
-------------------
CALL "PROG":
- Creates new ExecutionContext
- Allocates LOCAL‑STORAGE
- Allocates WORKING‑STORAGE unless COMMON
- Maps LINKAGE SECTION to caller

2.3 COMMON WORKING‑STORAGE
--------------------------
If program declares COMMON:
- WORKING‑STORAGE is shared across activations
- Equivalent to static storage

2.4 Non‑COMMON WORKING‑STORAGE
------------------------------
Fresh allocation per activation.

------------------------------------------------------------
SECTION 3 — STOP RUN SEMANTICS
------------------------------------------------------------

3.1 STOP RUN terminates entire runtime
--------------------------------------
STOP RUN:
- Ends the entire COBOL runtime
- Unwinds all call stacks
- Closes all files
- Flushes all reports
- Releases all ExecutionContexts

3.2 STOP RUN in nested program
------------------------------
Even if called inside a nested program:
- Terminates the entire application
- Not just the current program

3.3 STOP RUN with RETURNING (COBOL 2023)
----------------------------------------
STOP RUN RETURNING value:
- Returns exit code to host environment

3.4 Lowering
------------
Generated IL:
ctx.Runtime.Terminate(exitCode)

------------------------------------------------------------
SECTION 4 — GOBACK SEMANTICS
------------------------------------------------------------

4.1 GOBACK returns from current program
---------------------------------------
If program was CALLed:
- Returns to caller
- Restores caller’s ExecutionContext
- Does NOT terminate runtime

4.2 GOBACK in main program
--------------------------
Equivalent to STOP RUN.

4.3 GOBACK with RETURNING
-------------------------
GOBACK RETURNING value:
- Returns value to caller
- Caller receives value in RETURNING target

4.4 Lowering
------------
Generated IL:
return returnValue

------------------------------------------------------------
SECTION 5 — EXIT PROGRAM SEMANTICS
------------------------------------------------------------

5.1 EXIT PROGRAM
----------------
Equivalent to GOBACK:
- Returns from current program
- Does NOT terminate runtime

5.2 EXIT PROGRAM in main program
--------------------------------
Equivalent to STOP RUN.

5.3 EXIT PROGRAM with USING
---------------------------
Not allowed.

------------------------------------------------------------
SECTION 6 — EXIT PARAGRAPH / EXIT SECTION
------------------------------------------------------------

6.1 EXIT PARAGRAPH
------------------
Returns from current paragraph:
- Ends current PERFORM
- Pops PERFORM stack

6.2 EXIT SECTION
----------------
Returns from current section:
- Ends PERFORM THRU
- Pops PERFORM stack

6.3 Lowering
------------
Generated IL:
br to performReturnLabel

------------------------------------------------------------
SECTION 7 — FILE & REPORT FINALIZATION
------------------------------------------------------------

7.1 FileManager cleanup
-----------------------
On program termination:
- All open files closed
- Buffers flushed
- Locks released
- File status updated

7.2 ReportEngine cleanup
------------------------
On program termination:
- Emit final REPORT FOOTING
- Flush pending pages
- Close output files

7.3 Declarative cleanup
-----------------------
Declaratives do NOT run during termination.

------------------------------------------------------------
SECTION 8 — EXECUTIONCONTEXT LIFECYCLE
------------------------------------------------------------

8.1 Creation
------------
ExecutionContext created:
- At program activation
- With fresh LOCAL‑STORAGE
- With WORKING‑STORAGE depending on COMMON

8.2 Destruction
---------------
ExecutionContext destroyed:
- On GOBACK (for nested programs)
- On STOP RUN (for all programs)

8.3 Resource release
--------------------
ExecutionContext.Dispose:
- Releases file handles
- Releases report buffers
- Clears ExceptionState
- Clears PERFORM stack

------------------------------------------------------------
SECTION 9 — CIL LOWERING RULES
------------------------------------------------------------

9.1 STOP RUN lowering
---------------------
Emit:
ctx.Runtime.Terminate(exitCode)
ret

9.2 GOBACK lowering
-------------------
Emit:
ret returnValue

9.3 EXIT PROGRAM lowering
-------------------------
Same as GOBACK.

9.4 EXIT PARAGRAPH / EXIT SECTION lowering
------------------------------------------
Emit:
br to performReturnLabel

9.5 CALL/RETURN integration
---------------------------
CALL:
- Push new ExecutionContext
- Invoke program method

RETURN:
- Pop ExecutionContext
- Resume caller

------------------------------------------------------------
SECTION 10 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Program activation stack
- STOP RUN termination
- GOBACK return values
- EXIT PROGRAM transitions
- PERFORM stack unwinding
- File/report cleanup events

------------------------------------------------------------
SECTION 11 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

11.1 STOP RUN inside declarative
--------------------------------
Terminates entire runtime immediately.

11.2 GOBACK inside declarative
------------------------------
Returns from program, not declarative.

11.3 EXIT PROGRAM inside declarative
------------------------------------
Same as GOBACK.

11.4 STOP RUN inside nested PERFORM
-----------------------------------
Unwinds all PERFORM stacks.

11.5 GOBACK inside deeply nested CALL chain
-------------------------------------------
Returns only one level.

11.6 EXIT PARAGRAPH with no active PERFORM
------------------------------------------
No effect.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Program Lifecycle Architecture:
- Implements full COBOL STOP RUN, GOBACK, EXIT PROGRAM, and EXIT PARAGRAPH semantics
- Provides deterministic program activation and teardown
- Ensures correct cleanup of files, reports, and runtime engines
- Integrates tightly with ExecutionContext and PERFORM stack
- Generates clean, verifiable, debugger‑friendly CIL
- Ensures correctness across CoreCLR, AOT, and WASM
