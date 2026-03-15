CobolSharp COBOL Runtime ExecutionContext, Storage & Engine Integration Architecture (CIL‑Only)
==============================================================================================

Purpose
-------
Define the authoritative architecture for:
- ExecutionContext lifecycle
- StorageBlock and explicit‑layout memory model
- Runtime engines (NumericEngine, StringEngine, JsonEngine, XmlEngine, FileManager, ReportEngine)
- PERFORM stack and call stack
- ExceptionState and declarative routing
- Program activation and nested program contexts
- CIL‑friendly runtime APIs
- AOT/WASM‑safe runtime behavior

This document governs how CobolSharp executes compiled COBOL programs on .NET.

------------------------------------------------------------
SECTION 1 — EXECUTIONCONTEXT OVERVIEW
------------------------------------------------------------

ExecutionContext is the central runtime object for every COBOL program.

It contains:
- Storage regions (WORKING‑STORAGE, LOCAL‑STORAGE, LINKAGE)
- File control blocks (FCBs)
- Runtime engines
- PERFORM stack
- Call stack
- Declarative handlers
- ExceptionState
- Random number generator
- Report state
- Program parameters

ExecutionContext is:
- Created at program start
- Passed to every generated method
- Destroyed at program termination

------------------------------------------------------------
SECTION 2 — STORAGE MODEL
------------------------------------------------------------

2.1 StorageBlock
----------------
Each COBOL storage region is represented by a StorageBlock:
- Backed by a byte[] array
- Explicit offsets for each field
- Supports PIC X, PIC N, COMP, COMP‑3, COMP‑5

2.2 Field access
----------------
StorageBlock provides:
- GetBytes(offset, length)
- SetBytes(offset, length)
- GetString(offset, length)
- SetString(offset, length)
- GetPackedDecimal(offset, digits, scale)
- SetPackedDecimal(offset, digits, scale)
- GetBinary(offset, width)
- SetBinary(offset, width)

2.3 REDEFINES
-------------
Multiple fields share the same offset.

2.4 OCCURS
----------
Arrays represented as:
- Contiguous memory
- Offset = base + index * elementSize

2.5 OCCURS DEPENDING ON
------------------------
Logical length checked at runtime.

------------------------------------------------------------
SECTION 3 — RUNTIME ENGINES
------------------------------------------------------------

ExecutionContext contains the following engines:

3.1 NumericEngine
-----------------
Handles:
- Decimal arithmetic
- Packed decimal encoding/decoding
- Overflow detection
- ROUNDED semantics

3.2 StringEngine
----------------
Handles:
- STRING
- UNSTRING
- INSPECT
- Unicode‑safe slicing

3.3 JsonEngine
--------------
Handles:
- JSON PARSE
- JSON GENERATE
- Mapping COBOL records ↔ JSON

3.4 XmlEngine
-------------
Handles:
- XML PARSE
- XML GENERATE
- SAX‑style event routing

3.5 FileManager
---------------
Handles:
- OPEN/CLOSE
- READ/WRITE/REWRITE/DELETE
- START
- File status codes
- Locking

3.6 ReportEngine
----------------
Handles:
- REPORT SECTION
- Page management
- Control breaks
- Field formatting

------------------------------------------------------------
SECTION 4 — PERFORM STACK
------------------------------------------------------------

4.1 Purpose
-----------
Tracks:
- Active PERFORM ranges
- Return addresses
- Loop state (for PERFORM UNTIL/VARYING/TIMES)

4.2 Stack entries
-----------------
Each entry contains:
- Return label
- Loop variables (if applicable)
- Loop condition metadata

4.3 Push/pop rules
------------------
PERFORM → push  
EXIT PERFORM / end of range → pop  
GOBACK / EXIT PROGRAM → pop all  

4.4 GO TO interactions
----------------------
GO TO may exit PERFORM range:
- Backend unwinds stack automatically

------------------------------------------------------------
SECTION 5 — CALL STACK
------------------------------------------------------------

5.1 CALL program
----------------
CALL "PROG" USING args:
- Creates new ExecutionContext
- Allocates LOCAL‑STORAGE
- Shares WORKING‑STORAGE if COMMON
- Maps LINKAGE SECTION to caller

5.2 GOBACK
----------
Returns from program or method.

5.3 EXIT PROGRAM
----------------
Unwinds entire call stack for current program.

------------------------------------------------------------
SECTION 6 — EXCEPTIONSTATE
------------------------------------------------------------

6.1 Structure
-------------
ExceptionState contains:
- Exception category
- File status (if applicable)
- Error message
- Runtime exception object (optional)

6.2 Categories
--------------
- INVALID KEY
- AT END
- SIZE ERROR
- JSON/XML EXCEPTION
- STANDARD EXCEPTION
- FILE ERROR
- RUNTIME ERROR

6.3 Reset rules
---------------
ExceptionState is cleared:
- After handler executes
- Before next statement

------------------------------------------------------------
SECTION 7 — DECLARATIVE HANDLER INTEGRATION
------------------------------------------------------------

7.1 Registration
----------------
ExecutionContext stores:
- USE AFTER EXCEPTION handler
- USE AFTER ERROR handler
- USE AFTER STANDARD EXCEPTION handler

7.2 Invocation
--------------
When exception occurs:
- Local handler checked first
- If none, declarative invoked
- Execution resumes after failing statement

------------------------------------------------------------
SECTION 8 — PROGRAM ACTIVATION
------------------------------------------------------------

8.1 Main program
----------------
ExecutionContext created with:
- WORKING‑STORAGE
- File table
- Report table
- Engines

8.2 Nested programs
-------------------
Each nested program:
- Has its own ExecutionContext
- Shares WORKING‑STORAGE if COMMON
- Has independent LOCAL‑STORAGE

8.3 ENTRY points
----------------
ENTRY "name" USING args:
- Maps to generated .NET method
- Shares ExecutionContext

------------------------------------------------------------
SECTION 9 — RANDOM NUMBER GENERATOR
------------------------------------------------------------

9.1 RNG state
-------------
ExecutionContext contains:
- Deterministic PRNG
- Seeded by RANDOM‑SEED or default

9.2 RANDOM function
-------------------
Uses ExecutionContext RNG.

------------------------------------------------------------
SECTION 10 — REPORT STATE
------------------------------------------------------------

10.1 Report descriptors
-----------------------
ExecutionContext stores:
- Report definitions
- Page counters
- Line counters
- Control break state

10.2 Output routing
-------------------
ReportEngine writes to:
- FileManager
- DISPLAY target

------------------------------------------------------------
SECTION 11 — CIL INTEGRATION
------------------------------------------------------------

11.1 Method signatures
----------------------
Every generated method has:
void MethodName(ExecutionContext ctx)

11.2 Engine calls
-----------------
STRING → ctx.StringEngine.String(...)  
ADD → ctx.NumericEngine.Add(...)  
JSON PARSE → ctx.JsonEngine.Parse(...)  
READ → ctx.FileManager.Read(...)  

11.3 Storage access
-------------------
Generated IL uses:
ctx.Storage.GetBytes  
ctx.Storage.SetBytes  
etc.

11.4 Exception routing
----------------------
Backend emits:
try/catch → ctx.ExceptionState → handler

------------------------------------------------------------
SECTION 12 — AOT/WASM SAFETY
------------------------------------------------------------

12.1 No reflection
------------------
Runtime avoids:
- Reflection.Emit
- DynamicMethod
- Unmanaged pointers

12.2 No dynamic code generation
-------------------------------
All IL is static.

12.3 No platform‑specific APIs
------------------------------
FileManager uses:
- Stream abstractions
- No OS‑specific syscalls

------------------------------------------------------------
SECTION 13 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

13.1 REDEFINES aliasing
------------------------
StorageBlock treats overlapping fields as raw bytes.

13.2 OCCURS DEPENDING ON
------------------------
Logical length validated at runtime.

13.3 Exception inside declarative
---------------------------------
Triggers STANDARD EXCEPTION declarative if present.

13.4 Nested PERFORM inside declarative
--------------------------------------
Allowed; PERFORM stack isolated.

13.5 CALL inside declarative
----------------------------
Allowed; new ExecutionContext created.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Runtime ExecutionContext Architecture:
- Provides deterministic, isolated runtime state for COBOL programs
- Integrates all storage, engines, file I/O, and report systems
- Implements PERFORM stack, call stack, and declarative routing
- Supports full COBOL semantics across CoreCLR, AOT, and WASM
- Generates clean, verifiable, debugger‑friendly CIL
