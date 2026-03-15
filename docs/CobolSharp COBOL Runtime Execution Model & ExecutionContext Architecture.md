CobolSharp COBOL Runtime Execution Model & ExecutionContext Architecture (CIL‑Only)
=================================================================================

Purpose
-------
Define the authoritative architecture for:
- COBOL program execution
- ExecutionContext lifecycle
- Storage management (WORKING‑STORAGE, LOCAL‑STORAGE, LINKAGE)
- Call stack and PERFORM stack
- Exception handling
- Runtime services (numeric, string, JSON/XML, file I/O)
- Integration with .NET execution model
- Deterministic, CIL‑only runtime behavior

This document governs how CobolSharp executes COBOL programs on .NET.

------------------------------------------------------------
SECTION 1 — EXECUTION MODEL OVERVIEW
------------------------------------------------------------

CobolSharp uses a **pure CIL execution model** with a structured runtime environment.

Execution is governed by:
- ExecutionContext (per program instance)
- StorageBlocks (data regions)
- Call stack frames (paragraphs, sections, methods)
- PERFORM stack (loop and range tracking)
- Runtime service engines (numeric, file, JSON/XML, etc.)

Execution is:
- Deterministic
- Reentrant
- Thread‑safe (per ExecutionContext)
- Compatible with CoreCLR, AOT, and WASM

------------------------------------------------------------
SECTION 2 — EXECUTIONCONTEXT
------------------------------------------------------------

ExecutionContext is the central runtime object.

Fields include:
- Program metadata
- StorageBlocks:
  - WorkingStorage
  - LocalStorage
  - LinkageStorage
- FileTable (open file handles)
- PERFORM stack
- Call stack
- Runtime service instances
- Exception state
- Debugging hooks

ExecutionContext is:
- Created at program start
- Passed to all runtime operations
- Destroyed at program termination

------------------------------------------------------------
SECTION 3 — STORAGE MANAGEMENT
------------------------------------------------------------

3.1 WORKING‑STORAGE
-------------------
- Allocated once per program instance
- Lives for entire program execution
- Backed by a StorageBlock
- Contains global data

3.2 LOCAL‑STORAGE
-----------------
- Allocated on each program invocation
- Reinitialized on each call
- Backed by a StorageBlock

3.3 LINKAGE SECTION
-------------------
- Backed by caller‑provided memory
- Used for parameters and return values
- Mapped to StorageBlock via marshaling

3.4 FILE SECTION
----------------
- Each file descriptor has:
  - Record buffer
  - Key buffer
  - File status variable
  - Runtime handle

------------------------------------------------------------
SECTION 4 — CALL STACK MODEL
------------------------------------------------------------

CobolSharp uses a **dual stack model**:

1. **COBOL call stack**  
   - Tracks program, paragraph, section, and method calls  
   - Used for debugging and exception mapping  

2. **PERFORM stack**  
   - Tracks PERFORM ranges  
   - Tracks PERFORM UNTIL loop state  
   - Tracks PERFORM VARYING iteration variables  

4.1 Call stack frames
---------------------
Each frame contains:
- Program/class name
- Paragraph/section/method name
- Source span
- Return address (CIL offset)
- Local variables (if OO)

4.2 PERFORM stack frames
------------------------
Each frame contains:
- PERFORM type (THRU, UNTIL, VARYING)
- Loop variables
- Termination condition
- Target paragraph range

------------------------------------------------------------
SECTION 5 — PROGRAM INVOCATION MODEL
------------------------------------------------------------

5.1 PROGRAM-ID invocation
-------------------------
Programs compile to:
- Static .NET classes
- With a Main‑like entry point

Invocation:
CobolProgram.Main(args)

5.2 CALL program
----------------
CALL "PROGRAM" USING args:
- Creates new ExecutionContext
- Allocates LOCAL‑STORAGE
- Shares WORKING‑STORAGE (unless COMMON)
- Maps LINKAGE SECTION to caller’s memory

5.3 OO invocation
-----------------
INVOKE object::Method:
- Uses .NET method dispatch
- ExecutionContext passed implicitly

------------------------------------------------------------
SECTION 6 — EXCEPTION MODEL
------------------------------------------------------------

6.1 COBOL exceptions
--------------------
CobolSharp supports:
- SIZE ERROR
- INVALID KEY
- AT END
- ON EXCEPTION (general)
- JSON/XML exceptions
- File I/O exceptions

6.2 Exception propagation
-------------------------
Rules:
- If handler exists → execute handler
- If no handler → propagate to caller
- If unhandled → throw .NET exception

6.3 Exception state
-------------------
ExecutionContext stores:
- Last exception type
- Last exception message
- Last file status (if applicable)

------------------------------------------------------------
SECTION 7 — RUNTIME SERVICE ENGINES
------------------------------------------------------------

CobolSharp.Runtime provides specialized engines:

7.1 NumericEngine
-----------------
Handles:
- Packed decimal arithmetic
- Decimal alignment
- ROUNDED
- SIZE ERROR detection
- Overflow detection

7.2 StringEngine
----------------
Handles:
- STRING
- UNSTRING
- INSPECT
- Concatenation
- Padding/truncation

7.3 FileManager
---------------
Handles:
- OPEN/CLOSE
- READ/WRITE/REWRITE/DELETE
- START
- File status updates
- Indexed/relative/sequential logic

7.4 JsonEngine
--------------
Handles:
- JSON PARSE
- JSON GENERATE
- Mapping to/from COBOL records

7.5 XmlEngine
-------------
Handles:
- XML PARSE
- XML GENERATE

7.6 DateTimeEngine
------------------
Handles:
- CURRENT-DATE
- Date/time formatting

7.7 CollationEngine
-------------------
Handles:
- STRING comparisons
- National character ordering

------------------------------------------------------------
SECTION 8 — CONTROL FLOW EXECUTION
------------------------------------------------------------

8.1 Paragraph execution
-----------------------
- Push call stack frame
- Execute statements
- Pop frame

8.2 Section execution
---------------------
- Same as paragraph
- May contain multiple paragraphs

8.3 PERFORM execution
---------------------
PERFORM THRU:
- Push PERFORM frame
- Execute range
- Pop frame

PERFORM UNTIL:
- Evaluate condition
- Loop until true

PERFORM VARYING:
- Initialize
- Test
- Execute
- Increment
- Repeat

------------------------------------------------------------
SECTION 9 — CIL LOWERING RULES
------------------------------------------------------------

9.1 ExecutionContext passing
----------------------------
All generated methods have signature:
void MethodName(ExecutionContext ctx)

9.2 Storage access
------------------
DISPLAY:
- ctx.Storage.GetString(offset, length)

COMP:
- ctx.Storage.GetBinary(offset, width)

COMP‑3:
- ctx.Storage.GetPackedDecimal(offset, digits, scale)

9.3 Branching
-------------
IF/EVALUATE/PERFORM lowered to:
- brtrue/brfalse
- switch
- structured loops

9.4 Exception lowering
----------------------
ON EXCEPTION:
- Wrap block in try/catch
- Set exception state
- Branch to handler

------------------------------------------------------------
SECTION 10 — DEBUGGING INTEGRATION
------------------------------------------------------------

ExecutionContext exposes:
- Current paragraph/section
- Current PERFORM frame
- Current call stack
- Current storage state
- Current exception state

Debugger uses:
- PDB sequence points
- StorageBlock inspection
- PERFORM stack visualization

------------------------------------------------------------
SECTION 11 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

11.1 Nested PERFORM with GO TO
------------------------------
- Allowed
- PERFORM stack must unwind correctly

11.2 EXIT PROGRAM inside nested calls
-------------------------------------
- Unwinds entire call chain
- Clears PERFORM stack

11.3 STOP RUN inside CALLed program
-----------------------------------
- Terminates entire process

11.4 REDEFINES with OCCURS DEPENDING ON
---------------------------------------
- Logical length must be respected
- Raw storage always preserved

11.5 Invalid numeric conversion
-------------------------------
- SIZE ERROR
- No assignment

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Runtime Execution Model & ExecutionContext Architecture:
- Defines the complete execution environment for COBOL on .NET
- Provides deterministic, CIL‑only execution
- Implements full COBOL semantics for control flow, storage, exceptions, and file I/O
- Uses ExecutionContext as the backbone of runtime state
- Integrates cleanly with debugging, optimization, and interop
- Ensures correctness across CoreCLR, AOT, and WASM
