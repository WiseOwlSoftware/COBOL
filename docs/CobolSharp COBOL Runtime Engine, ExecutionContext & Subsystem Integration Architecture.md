CobolSharp COBOL Runtime Engine, ExecutionContext & Subsystem Integration Architecture (CIL‑Only)
================================================================================================

Purpose
-------
Define the authoritative architecture for:
- ExecutionContext lifecycle
- Runtime subsystem orchestration
- FileManager, ReportEngine, JsonEngine, XmlEngine, NumericEngine, StringEngine
- PERFORM stack management
- CALL stack and nested program activation
- ExceptionState propagation
- Declarative dispatch
- Program termination (STOP RUN, GOBACK)
- AOT/WASM‑safe runtime design
- CIL‑friendly integration points

This document governs how CobolSharp executes COBOL programs at runtime.

------------------------------------------------------------
SECTION 1 — EXECUTIONCONTEXT OVERVIEW
------------------------------------------------------------

ExecutionContext is the central runtime object containing:
- Storage blocks (WORKING‑STORAGE, LOCAL‑STORAGE, LINKAGE)
- Runtime subsystem instances
- PERFORM stack
- CALL stack metadata
- ExceptionState
- Report state
- File handles
- Program registry
- Runtime termination flags

Each program activation receives:
- A fresh ExecutionContext (unless COMMON WORKING‑STORAGE)
- Its own subsystem instances
- Its own ExceptionState

------------------------------------------------------------
SECTION 2 — EXECUTIONCONTEXT LIFECYCLE
------------------------------------------------------------

2.1 Creation
------------
Created when:
- Main program starts
- Nested program is CALLed

Constructor initializes:
- Storage allocator
- Subsystems
- PERFORM stack
- ExceptionState
- File table
- Report table

2.2 Destruction
---------------
Occurs when:
- Program returns via GOBACK or EXIT PROGRAM
- STOP RUN terminates runtime

Destructor:
- Closes files
- Flushes reports
- Clears ExceptionState
- Releases buffers

------------------------------------------------------------
SECTION 3 — STORAGE MANAGEMENT
------------------------------------------------------------

3.1 WORKING‑STORAGE
-------------------
Allocated once per program activation unless COMMON.

3.2 LOCAL‑STORAGE
-----------------
Allocated fresh for each program activation.

3.3 LINKAGE SECTION
-------------------
Points to caller’s storage (BY REFERENCE) or copy (BY CONTENT).

3.4 StorageBlock
----------------
Backed by:
- byte[] buffer
- Offset table
- PIC metadata

------------------------------------------------------------
SECTION 4 — RUNTIME SUBSYSTEMS
------------------------------------------------------------

ExecutionContext contains:

4.1 FileManager
---------------
Handles:
- OPEN, CLOSE
- READ, WRITE, REWRITE, DELETE
- START
- File status
- Locking
- Indexed/relative/sequential logic

4.2 ReportEngine
----------------
Handles:
- Page/line control
- Control breaks
- Accumulators
- Rendering
- Output file writing

4.3 JsonEngine
--------------
Handles:
- JSON PARSE
- JSON GENERATE
- UTF‑8/UTF‑16 conversion
- WITH DETAIL population

4.4 XmlEngine
-------------
Handles:
- XML PARSE (SAX)
- XML GENERATE
- Namespace handling
- UTF‑8/UTF‑16 conversion

4.5 NumericEngine
-----------------
Handles:
- Decimal arithmetic
- COMP‑3 unpack/pack
- COMP/COMP‑5 conversion
- ROUNDED
- SIZE ERROR detection

4.6 StringEngine
----------------
Handles:
- STRING
- UNSTRING
- INSPECT
- ASCII/UTF‑16 conversions

------------------------------------------------------------
SECTION 5 — PERFORM STACK ARCHITECTURE
------------------------------------------------------------

5.1 Purpose
-----------
Tracks:
- PERFORM entry point
- PERFORM exit point
- Loop variables (for VARYING)
- Nested PERFORMs

5.2 Push
--------
On PERFORM:
- Push frame with return label

5.3 Pop
-------
On EXIT PARAGRAPH / EXIT SECTION / reaching end:
- Pop frame
- Branch to return label

5.4 GO TO interaction
---------------------
GO TO may:
- Exit PERFORM range
- Unwind PERFORM stack

------------------------------------------------------------
SECTION 6 — CALL STACK & PROGRAM ACTIVATION
------------------------------------------------------------

6.1 CALL literal
----------------
CALL "PROG":
- Lookup program
- Create new ExecutionContext
- Map USING parameters
- Invoke program entry

6.2 CALL identifier
-------------------
CALL variable:
- Runtime lookup
- Same activation rules

6.3 RETURNING
-------------
Caller receives:
- RETURNING value from callee

6.4 GOBACK
----------
Returns from current program:
- Destroy ExecutionContext
- Restore caller’s ExecutionContext

6.5 STOP RUN
------------
Destroys:
- All ExecutionContexts
- All subsystems
- Entire runtime

------------------------------------------------------------
SECTION 7 — EXCEPTIONSTATE & ERROR PROPAGATION
------------------------------------------------------------

7.1 ExceptionState fields
-------------------------
- Category
- Message
- File name
- JSON/XML metadata
- Numeric overflow metadata
- Raw exception (optional)

7.2 Setting ExceptionState
--------------------------
Set by:
- FileManager
- NumericEngine
- JsonEngine
- XmlEngine
- ReportEngine

7.3 Clearing ExceptionState
---------------------------
Cleared:
- After ON EXCEPTION block
- After declarative completes
- After successful operation

------------------------------------------------------------
SECTION 8 — DECLARATIVE DISPATCH
------------------------------------------------------------

8.1 Triggering
--------------
On exception:
- Check ON EXCEPTION / INVALID KEY / AT END
- If none, dispatch declarative

8.2 Dispatcher
--------------
DeclarativeDispatcher:
- Selects correct declarative
- Jumps to declarative entry
- Returns to continuation label

8.3 Re‑entry
------------
Declaratives may:
- Re‑enter
- Nest
- Trigger other declaratives

------------------------------------------------------------
SECTION 9 — PROGRAM TERMINATION MODEL
------------------------------------------------------------

9.1 STOP RUN
------------
- Sets Runtime.Terminated = true
- Unwinds all ExecutionContexts
- Closes files
- Flushes reports

9.2 GOBACK
----------
- Returns from current program
- Restores caller’s ExecutionContext

9.3 EXIT PROGRAM
----------------
Same as GOBACK.

9.4 EXIT PARAGRAPH / EXIT SECTION
---------------------------------
- Pops PERFORM stack
- Returns to PERFORM caller

------------------------------------------------------------
SECTION 10 — AOT/WASM‑SAFE RUNTIME DESIGN
------------------------------------------------------------

10.1 No dynamic codegen
-----------------------
Runtime uses:
- Pure managed code
- No reflection emit
- No dynamic IL

10.2 No unsafe code
-------------------
- No pointers
- No stackalloc
- No unmanaged memory

10.3 Deterministic behavior
---------------------------
- No platform‑specific APIs
- No OS‑level file locks
- No JIT‑dependent optimizations

------------------------------------------------------------
SECTION 11 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Current ExecutionContext
- PERFORM stack
- CALL stack
- ExceptionState
- File handles
- Report state
- Storage blocks (decoded)

------------------------------------------------------------
SECTION 12 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

12.1 Exception inside declarative
---------------------------------
Routed to STANDARD EXCEPTION declarative.

12.2 STOP RUN inside nested CALL
--------------------------------
Terminates entire runtime.

12.3 GOBACK inside declarative
------------------------------
Returns from program, not declarative.

12.4 PERFORM VARYING with negative BY
-------------------------------------
Allowed; may create infinite loop.

12.5 JSON/XML exception during cleanup
--------------------------------------
Ignored.

12.6 CALL recursion
-------------------
Allowed; each activation gets its own ExecutionContext.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Runtime Architecture:
- Provides a unified ExecutionContext for all COBOL runtime behavior
- Integrates file I/O, report writing, JSON/XML, numeric, and string engines
- Implements full PERFORM, CALL, declarative, and exception semantics
- Ensures deterministic, verifiable, AOT/WASM‑safe execution
- Generates clean, debugger‑friendly runtime behavior
