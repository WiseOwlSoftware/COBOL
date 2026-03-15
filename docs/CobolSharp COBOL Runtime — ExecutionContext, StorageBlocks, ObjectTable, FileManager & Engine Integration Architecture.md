CobolSharp COBOL Runtime — ExecutionContext, StorageBlocks, ObjectTable, FileManager & Engine Integration Architecture (CIL‑Only)
===============================================================================================================================

Purpose
-------
Define the authoritative architecture for:
- ExecutionContext lifecycle
- StorageBlocks (WORKING‑STORAGE, LOCAL‑STORAGE, LINKAGE, FD buffers)
- ObjectTable (COBOL object references)
- FileManager integration
- Subsystem engines (NumericEngine, StringEngine, JsonEngine, XmlEngine, SortEngine, ReportEngine, ConsoleEngine)
- CALL stack and PERFORM stack integration
- Declaratives and ExceptionState routing
- AOT/WASM‑safe runtime design
- CIL‑friendly lowering

This document governs how CobolSharp executes COBOL programs at runtime.

------------------------------------------------------------
SECTION 1 — EXECUTIONCONTEXT OVERVIEW
------------------------------------------------------------

ExecutionContext is the central runtime object containing:
- StorageBlocks for all data divisions
- ObjectTable for OO and .NET interop
- FileManager instance
- Subsystem engines
- CALL stack
- PERFORM stack
- ExceptionState
- Program registry reference
- Runtime flags (debug, tracing, breakpoints)

Each program activation receives its own ExecutionContext.

------------------------------------------------------------
SECTION 2 — STORAGEBLOCK ARCHITECTURE
------------------------------------------------------------

2.1 StorageBlock structure
--------------------------
StorageBlock contains:
- byte[] Buffer
- FieldOffset[] table
- FieldMetadata[] table
- OCCURS metadata
- REDEFINES metadata

2.2 StorageBlock types
----------------------
- WORKING‑STORAGE block
- LOCAL‑STORAGE block
- LINKAGE block
- FD record buffers
- Temporary blocks (SORT, JSON, XML)

2.3 Allocation rules
--------------------
WORKING‑STORAGE:
- Allocated once per program activation
- Reinitialized if INITIAL

LOCAL‑STORAGE:
- Allocated on each ENTRY
- Cleared on exit

LINKAGE:
- Allocated based on USING parameters

FD buffers:
- Allocated on OPEN

------------------------------------------------------------
SECTION 3 — OBJECTTABLE ARCHITECTURE
------------------------------------------------------------

3.1 Purpose
-----------
Stores:
- COBOL OO objects
- .NET objects returned from INVOKE
- NEW object instances

3.2 Structure
-------------
ObjectTable contains:
- List<object> references
- Free list for reuse
- Null slot at index 0

3.3 Reference model
-------------------
COBOL object reference = integer index into ObjectTable.

3.4 Lifetime
------------
Objects remain alive until:
- Program terminates
- Explicitly set to null
- Garbage collected by .NET

------------------------------------------------------------
SECTION 4 — FILEMANAGER INTEGRATION
------------------------------------------------------------

4.1 FileManager responsibilities
--------------------------------
- Open/close files
- Read/write/rewind
- Indexed/relative access
- File status codes
- Locking and sharing
- START positioning

4.2 ExecutionContext integration
--------------------------------
ExecutionContext contains:
- FileManager instance
- FD metadata
- Record buffers

4.3 FD binding
--------------
On OPEN:
- FileManager binds FD to file handle
- Allocates record buffer
- Initializes cursor

------------------------------------------------------------
SECTION 5 — SUBSYSTEM ENGINES
------------------------------------------------------------

ExecutionContext contains the following engines:

5.1 NumericEngine
-----------------
- Decimal arithmetic
- ROUNDED logic
- SIZE ERROR detection
- COMP/COMP‑3/COMP‑5 conversion

5.2 StringEngine
----------------
- STRING/UNSTRING/INSPECT
- UTF‑8/UTF‑16 bridging
- Padding/truncation

5.3 JsonEngine
--------------
- SAX‑style JSON parsing
- JSON GENERATE
- WITH DETAIL support

5.4 XmlEngine
-------------
- SAX‑style XML parsing
- XML GENERATE
- Namespace resolution

5.5 SortEngine
--------------
- External merge sort
- Indexed merge
- USING/GIVING pipeline

5.6 ReportEngine
----------------
- Page/line control
- Control‑break logic
- Accumulators

5.7 ConsoleEngine
-----------------
- ACCEPT/DISPLAY
- Date/time/environment
- Command‑line arguments

------------------------------------------------------------
SECTION 6 — CALL STACK ARCHITECTURE
------------------------------------------------------------

6.1 CALL frame
--------------
Contains:
- Caller ExecutionContext
- Return address
- RETURNING target
- Parameter descriptors

6.2 CALL activation
-------------------
Steps:
1. Lookup program
2. Create new ExecutionContext
3. Allocate StorageBlocks
4. Map LINKAGE SECTION
5. Initialize subsystems
6. Dispatch ENTRY

6.3 GOBACK
----------
- Pops CALL frame
- Restores caller ExecutionContext

6.4 STOP RUN
------------
- Clears entire CALL stack
- Terminates runtime

------------------------------------------------------------
SECTION 7 — PERFORM STACK ARCHITECTURE
------------------------------------------------------------

7.1 PERFORM frame
-----------------
Contains:
- Return label
- THRU range
- Loop variables
- Loop bounds
- TEST BEFORE/AFTER flag

7.2 EXIT PARAGRAPH/SECTION
--------------------------
- Pops PERFORM frame
- Branches to return label

------------------------------------------------------------
SECTION 8 — EXCEPTIONSTATE ARCHITECTURE
------------------------------------------------------------

8.1 ExceptionState fields
-------------------------
- Category (SIZE ERROR, I/O ERROR, JSON ERROR, XML ERROR, .NET EXCEPTION)
- Message
- Source subsystem
- Raw token (JSON/XML)
- File name (I/O)
- Key value (indexed)
- Stack trace (optional)

8.2 Routing
-----------
1. ON EXCEPTION  
2. USE AFTER EXCEPTION ON file/json/xml  
3. USE AFTER STANDARD EXCEPTION  

8.3 Reset
---------
ExceptionState cleared:
- After ON EXCEPTION block
- After declarative completes

------------------------------------------------------------
SECTION 9 — PROGRAM REGISTRY
------------------------------------------------------------

9.1 Purpose
-----------
Maps:
- Program names → .NET types
- ENTRY names → methods

9.2 No reflection at runtime
----------------------------
Registry built at compile time.

9.3 Lookup
----------
CALL "P" → registry["P"]

------------------------------------------------------------
SECTION 10 — RUNTIME INITIALIZATION
------------------------------------------------------------

10.1 Startup
------------
- Create root ExecutionContext
- Load program registry
- Initialize ConsoleEngine
- Initialize FileManager
- Call main program

10.2 Shutdown
-------------
- Close all files
- Flush reports
- Clear ObjectTable
- Dispose engines

------------------------------------------------------------
SECTION 11 — CIL LOWERING RULES
------------------------------------------------------------

11.1 ExecutionContext lowering
------------------------------
Compiler generates:
- ctx = new ExecutionContext(programId)

11.2 StorageBlock lowering
--------------------------
Compiler generates:
- new StorageBlock(size)
- FieldOffset table
- Metadata table

11.3 Engine calls
-----------------
All subsystem calls lowered to:
- callvirt ctx.Engine.Method

11.4 ObjectTable lowering
-------------------------
Object reference stored as:
- int index

11.5 CALL lowering
------------------
- new ExecutionContext
- call Entry method

------------------------------------------------------------
SECTION 12 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- All StorageBlocks (decoded)
- ObjectTable contents
- FileManager state
- CALL stack
- PERFORM stack
- ExceptionState
- Engine state (JSON/XML/SORT/REPORT)

------------------------------------------------------------
SECTION 13 — AOT/WASM‑SAFE RUNTIME
------------------------------------------------------------

13.1 No reflection
------------------
All binding static.

13.2 No dynamic codegen
-----------------------
No IL emit.

13.3 No unsafe code
-------------------
No pointers or stackalloc.

13.4 Deterministic behavior
---------------------------
Identical across platforms.

------------------------------------------------------------
SECTION 14 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

14.1 CALL inside declarative
----------------------------
Allowed.

14.2 File left open on STOP RUN
-------------------------------
Runtime closes automatically.

14.3 Object reference to disposed .NET object
---------------------------------------------
Runtime error.

14.4 StorageBlock overflow
--------------------------
Impossible; bounds checked.

14.5 Recursive CALL with COMMON WORKING‑STORAGE
-----------------------------------------------
Shared across activations.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Runtime Architecture:
- Provides a deterministic, safe, AOT/WASM‑compatible execution environment
- Integrates StorageBlocks, ObjectTable, FileManager, and subsystem engines
- Implements CALL/PERFORM stacks, declaratives, and ExceptionState routing
- Generates clean, verifiable, debugger‑friendly CIL
