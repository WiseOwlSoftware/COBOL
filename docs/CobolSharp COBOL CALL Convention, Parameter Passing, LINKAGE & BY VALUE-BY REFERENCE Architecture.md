CobolSharp COBOL CALL Convention, Parameter Passing, LINKAGE & BY VALUE/BY REFERENCE Architecture (CIL‑Only)
============================================================================================================

Purpose
-------
Define the authoritative architecture for:
- CALL literal and CALL identifier
- Parameter passing semantics
- BY REFERENCE, BY CONTENT, BY VALUE
- LINKAGE SECTION mapping
- RETURNING semantics
- Nested program activation
- COMMON vs INITIAL WORKING‑STORAGE
- CALL stack model
- CIL‑friendly lowering
- AOT/WASM‑safe invocation

This document governs how CobolSharp implements COBOL’s CALL conventions and inter‑program communication.

------------------------------------------------------------
SECTION 1 — CALL OVERVIEW
------------------------------------------------------------

CobolSharp supports:
- CALL "ProgramName"
- CALL identifier
- CALL with USING parameters
- RETURNING result
- Nested CALLs
- Recursive CALLs
- Multi‑ENTRY dispatch

CALL activates:
- A new program instance
- With its own ExecutionContext
- With its own WORKING‑STORAGE (unless COMMON)
- With mapped LINKAGE SECTION

------------------------------------------------------------
SECTION 2 — PARAMETER PASSING MODES
------------------------------------------------------------

2.1 BY REFERENCE (default)
--------------------------
Caller passes:
- Pointer to caller’s StorageBlock
- Callee’s LINKAGE SECTION field overlays same memory

Effects:
- Mutations visible to caller
- No copying
- Fastest mode

2.2 BY CONTENT
--------------
Caller passes:
- Copy of data
- Callee receives independent buffer

Effects:
- Mutations NOT visible to caller
- Copy created at CALL time
- Copy destroyed at RETURN

2.3 BY VALUE
------------
Caller passes:
- Primitive value (Decimal, int, bool)
- Callee receives boxed or unboxed value

Effects:
- No shared memory
- No copying of StorageBlock
- Used for numeric parameters

2.4 Mixed modes
---------------
CALL "P" USING BY REFERENCE A BY CONTENT B BY VALUE C.

Compiler generates:
- Parameter descriptor array
- Per‑parameter mapping logic

------------------------------------------------------------
SECTION 3 — LINKAGE SECTION ARCHITECTURE
------------------------------------------------------------

3.1 LINKAGE SECTION fields
--------------------------
Represent:
- Parameters passed to program
- BY REFERENCE pointers
- BY CONTENT copies
- BY VALUE primitives

3.2 Mapping rules
-----------------
For each USING parameter:
- BY REFERENCE → pointer to caller’s StorageBlock
- BY CONTENT → allocate new StorageBlock, copy bytes
- BY VALUE → store primitive in local variable

3.3 LINKAGE SECTION lifetime
----------------------------
- Allocated at program activation
- Destroyed at program termination
- BY CONTENT buffers freed automatically

------------------------------------------------------------
SECTION 4 — RETURNING SEMANTICS
------------------------------------------------------------

4.1 RETURNING value
-------------------
CALL "P" USING A B RETURNING R.

Callee:
- Sets ctx.ReturnValue

Caller:
- Assigns ctx.ReturnValue to R

4.2 RETURNING types
-------------------
Allowed:
- DISPLAY
- NATIONAL
- Numeric
- Object reference (OO COBOL)

4.3 RETURNING with BY REFERENCE
-------------------------------
Allowed; RETURNING is separate from USING.

------------------------------------------------------------
SECTION 5 — PROGRAM ACTIVATION MODEL
------------------------------------------------------------

5.1 Activation steps
--------------------
1. Lookup program in registry  
2. Create new ExecutionContext  
3. Allocate WORKING‑STORAGE  
4. Allocate LOCAL‑STORAGE  
5. Map LINKAGE SECTION  
6. Initialize subsystems  
7. Dispatch to ENTRY method  

5.2 COMMON WORKING‑STORAGE
--------------------------
Shared across activations:
- No reallocation
- No reinitialization unless INITIAL

5.3 INITIAL clause
------------------
Reinitializes WORKING‑STORAGE on each activation.

------------------------------------------------------------
SECTION 6 — CALL STACK MODEL
------------------------------------------------------------

6.1 CALL frame
--------------
Contains:
- Caller ExecutionContext
- Return address
- RETURNING target
- Parameter descriptors

6.2 GOBACK
----------
- Pops CALL frame
- Restores caller ExecutionContext

6.3 STOP RUN
------------
- Clears entire CALL stack
- Terminates runtime

6.4 Recursive CALL
------------------
Allowed:
- Each activation receives its own ExecutionContext

------------------------------------------------------------
SECTION 7 — CALL IDENTIFIER RESOLUTION
------------------------------------------------------------

7.1 CALL identifier
-------------------
CALL var:
- Evaluate var at runtime
- Lookup program or ENTRY by name
- Same rules as CALL literal

7.2 Dynamic dispatch
--------------------
If var contains:
- Program name → dispatch to main ENTRY
- ENTRY name → dispatch to specific ENTRY

7.3 Not found
-------------
Runtime error:
PROGRAM-NOT-FOUND

------------------------------------------------------------
SECTION 8 — CIL LOWERING RULES
------------------------------------------------------------

8.1 CALL lowering
-----------------
Generated IL:
- Evaluate parameters
- Build ParameterDescriptor[]
- Load program name
- Call ProgramRegistry.Lookup
- newobj ProgramType
- callvirt EntryMethod(ctx, args)

8.2 BY REFERENCE lowering
-------------------------
Pass:
- Pointer to caller StorageBlock
- Offset metadata

8.3 BY CONTENT lowering
-----------------------
- Allocate new StorageBlock
- Copy bytes
- Pass pointer to copy

8.4 BY VALUE lowering
---------------------
- Load primitive
- Box if needed
- Pass as object

8.5 RETURNING lowering
----------------------
Callee:
ctx.ReturnValue = value

Caller:
store ctx.ReturnValue into target

------------------------------------------------------------
SECTION 9 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- CALL stack
- USING parameters
- BY REFERENCE pointers
- BY CONTENT copies
- BY VALUE primitives
- LINKAGE SECTION mapping
- RETURNING value

------------------------------------------------------------
SECTION 10 — AOT/WASM‑SAFE CALL MODEL
------------------------------------------------------------

10.1 No reflection
------------------
Program lookup uses:
- Prebuilt registry
- No Type.GetType

10.2 No dynamic codegen
-----------------------
Entry methods compiled statically.

10.3 No unsafe code
-------------------
Pointers simulated via:
- StorageBlock references
- Offset tables

10.4 Deterministic behavior
---------------------------
CALL semantics identical across platforms.

------------------------------------------------------------
SECTION 11 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

11.1 CALL with too few parameters
---------------------------------
Runtime error:
PARAMETER-MISMATCH

11.2 CALL with too many parameters
----------------------------------
Runtime error:
PARAMETER-MISMATCH

11.3 BY REFERENCE to overlapping fields
---------------------------------------
Allowed; callee sees live view.

11.4 BY CONTENT of OCCURS DEPENDING ON
--------------------------------------
Copies maximum size; callee uses DEPENDING ON value.

11.5 RETURNING with no target
-----------------------------
Value discarded.

11.6 CALL inside declarative
----------------------------
Allowed.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp CALL Architecture:
- Implements full COBOL CALL, USING, RETURNING, and LINKAGE semantics
- Supports BY REFERENCE, BY CONTENT, and BY VALUE with precise memory mapping
- Provides deterministic, safe, AOT/WASM‑compatible invocation
- Integrates tightly with ExecutionContext and ProgramRegistry
- Generates clean, verifiable, debugger‑friendly CIL
