CobolSharp COBOL Program Linking, CALL/RETURN & Multi‑Module Architecture (CIL‑Only)
===================================================================================

Purpose
-------
Define the authoritative architecture for:
- CALL literal and CALL identifier
- Static vs dynamic CALL resolution
- Parameter passing (BY REFERENCE / BY CONTENT / BY VALUE)
- RETURNING semantics
- Multi‑module program linking
- COMMON vs non‑COMMON WORKING‑STORAGE
- Nested program activation
- ENTRY points
- CIL‑friendly lowering
- AOT/WASM‑safe linking model

This document governs how CobolSharp links COBOL programs together and executes CALL/RETURN semantics on .NET.

------------------------------------------------------------
SECTION 1 — PROGRAM LINKING OVERVIEW
------------------------------------------------------------

CobolSharp supports:
- Static linking (compile‑time known programs)
- Dynamic linking (CALL identifier)
- Multi‑module assemblies
- Shared WORKING‑STORAGE via COMMON
- Independent LOCAL‑STORAGE per activation
- ENTRY points with USING parameters

Each COBOL program becomes:
- A .NET class
- With a static entry method
- With optional ENTRY methods

------------------------------------------------------------
SECTION 2 — CALL STATEMENT SEMANTICS
------------------------------------------------------------

2.1 CALL literal
----------------
CALL "PROG" USING args

- Program name known at compile time
- Linked statically
- Fastest invocation path

2.2 CALL identifier
-------------------
CALL program-name USING args

- Program name determined at runtime
- Resolved via ProgramRegistry
- Allows dynamic dispatch

2.3 CALL with RETURNING
-----------------------
CALL "PROG" USING args RETURNING result

Lowered to:
result = Prog.Main(ctx, args)

2.4 CALL with no USING
----------------------
CALL "PROG"

Equivalent to:
CALL "PROG" USING NOTHING

------------------------------------------------------------
SECTION 3 — PARAMETER PASSING
------------------------------------------------------------

CobolSharp supports:
- BY REFERENCE (default)
- BY CONTENT
- BY VALUE (OO only)

3.1 BY REFERENCE
----------------
- Passes pointer to caller’s storage
- Callee modifies caller’s data

3.2 BY CONTENT
--------------
- Passes a copy of the data
- Callee cannot modify caller’s data

3.3 BY VALUE (OO only)
----------------------
- Passes primitive value
- No reference to caller’s storage

3.4 LINKAGE SECTION mapping
---------------------------
Each USING parameter maps to:
- A LINKAGE SECTION item
- Bound to caller’s storage or copy

------------------------------------------------------------
SECTION 4 — RETURNING SEMANTICS
------------------------------------------------------------

4.1 RETURNING value
-------------------
RETURNING value:
- Returns a value to caller
- Equivalent to function return

4.2 RETURNING with BY REFERENCE
-------------------------------
Allowed; RETURNING is separate from USING.

4.3 RETURNING with no value
---------------------------
Illegal.

------------------------------------------------------------
SECTION 5 — PROGRAM ACTIVATION MODEL
------------------------------------------------------------

5.1 ExecutionContext creation
-----------------------------
CALL "PROG":
- Creates new ExecutionContext
- Allocates LOCAL‑STORAGE
- Allocates WORKING‑STORAGE unless COMMON
- Allocates LINKAGE SECTION
- Initializes FileManager and engines

5.2 COMMON WORKING‑STORAGE
--------------------------
If program declares:
COMMON.

Then:
- WORKING‑STORAGE shared across activations
- Equivalent to static storage

5.3 Non‑COMMON WORKING‑STORAGE
------------------------------
Allocated fresh for each CALL.

5.4 LOCAL‑STORAGE
-----------------
Always fresh per activation.

------------------------------------------------------------
SECTION 6 — ENTRY POINTS
------------------------------------------------------------

6.1 ENTRY statement
-------------------
ENTRY "name" USING args

Generates:
public static void Entry_name(ctx, args)

6.2 ENTRY resolution
--------------------
CALL "PROG" USING args:
- Calls default entry point

CALL "PROG" USING args AT ENTRY "name":
- Calls named entry point

6.3 Multiple ENTRY points
-------------------------
Allowed; each becomes a separate .NET method.

------------------------------------------------------------
SECTION 7 — MULTI‑MODULE LINKING
------------------------------------------------------------

7.1 ProgramRegistry
-------------------
CobolSharp maintains:
- A registry of available programs
- Loaded from assemblies
- Supports dynamic lookup

7.2 Static linking
------------------
CALL "PROG":
- Bound to known class at compile time
- No runtime lookup needed

7.3 Dynamic linking
-------------------
CALL identifier:
- Lookup at runtime
- Throws ON EXCEPTION if not found

7.4 Assembly boundaries
-----------------------
Programs may reside in:
- Same assembly
- Different assemblies
- External libraries

------------------------------------------------------------
SECTION 8 — NESTED PROGRAMS
------------------------------------------------------------

8.1 Structure
-------------
PROGRAM-ID. Outer.
    PROGRAM-ID. Inner.
    END PROGRAM Inner.
END PROGRAM Outer.

8.2 Activation
--------------
CALL "Inner":
- Creates new ExecutionContext
- Shares WORKING‑STORAGE if COMMON
- Has independent LOCAL‑STORAGE

8.3 Visibility
--------------
Inner programs:
- Can access outer WORKING‑STORAGE
- Cannot access outer LOCAL‑STORAGE

------------------------------------------------------------
SECTION 9 — CIL LOWERING RULES
------------------------------------------------------------

9.1 CALL lowering
-----------------
CALL "PROG" → direct call to Prog.Main  
CALL identifier → ProgramRegistry.Lookup + call  

9.2 USING lowering
------------------
BY REFERENCE → pass pointer to StorageBlock  
BY CONTENT → allocate temp buffer + copy  
BY VALUE → pass primitive  

9.3 RETURNING lowering
----------------------
RETURNING → ret value

9.4 ENTRY lowering
------------------
ENTRY "name" → static method with signature:
void Entry_name(ExecutionContext ctx, ...)

------------------------------------------------------------
SECTION 10 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Current program name
- ENTRY point used
- USING parameters
- RETURNING value
- CALL stack
- COMMON vs non‑COMMON storage

Sequence points emitted for:
- CALL
- ENTRY
- RETURNING
- EXIT PROGRAM
- GOBACK

------------------------------------------------------------
SECTION 11 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

11.1 CALL to missing program
----------------------------
Triggers ON EXCEPTION.

11.2 CALL with too many USING parameters
----------------------------------------
Compile‑time error.

11.3 CALL with too few USING parameters
---------------------------------------
Warning; missing parameters treated as uninitialized.

11.4 RECURSIVE CALL
-------------------
Allowed; each activation gets new LOCAL‑STORAGE.

11.5 COMMON WORKING‑STORAGE recursion
-------------------------------------
Shared across recursive calls.

11.6 CALL inside declarative
----------------------------
Allowed; new ExecutionContext created.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Program Linking & CALL Architecture:
- Implements full COBOL CALL, RETURNING, ENTRY, and multi‑module semantics
- Supports static and dynamic linking
- Provides deterministic parameter passing and storage allocation
- Integrates tightly with ExecutionContext and runtime engines
- Generates clean, verifiable, debugger‑friendly CIL
- Ensures correctness across CoreCLR, AOT, and WASM
