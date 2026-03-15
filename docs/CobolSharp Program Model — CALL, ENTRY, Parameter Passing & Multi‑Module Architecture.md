CobolSharp Program Model — CALL, ENTRY, Parameter Passing & Multi‑Module Architecture (CIL‑Only)
================================================================================================

Purpose
-------
Define the authoritative architecture for:
- CALL statement semantics
- ENTRY points and multi‑entry programs
- Parameter passing (BY REFERENCE, BY CONTENT, BY VALUE)
- LINKAGE SECTION mapping
- Program activation and deactivation
- COMMON vs INITIAL programs
- Recursion rules
- Multi‑module program registry
- CIL‑friendly lowering
- AOT/WASM‑safe program invocation

This document governs how CobolSharp implements COBOL program invocation and inter‑module execution.

------------------------------------------------------------
SECTION 1 — PROGRAM MODEL OVERVIEW
------------------------------------------------------------

A COBOL program in CobolSharp consists of:
- IDENTIFICATION DIVISION
- ENVIRONMENT DIVISION
- DATA DIVISION
- PROCEDURE DIVISION
- Optional ENTRY points

Each CALL creates:
- A new ExecutionContext
- New StorageBlocks (except COMMON)
- New PERFORM stack
- New ExceptionState

Execution is:
- Deterministic
- Single‑threaded
- Non‑preemptive

------------------------------------------------------------
SECTION 2 — PROGRAM ACTIVATION
------------------------------------------------------------

2.1 Activation steps
--------------------
CALL "P" USING a b c:

1. Lookup program P in ProgramRegistry  
2. Allocate new ExecutionContext  
3. Allocate WORKING‑STORAGE (unless COMMON)  
4. Allocate LOCAL‑STORAGE  
5. Map LINKAGE SECTION to USING parameters  
6. Initialize subsystems  
7. Dispatch ENTRY point  
8. Execute PROCEDURE DIVISION  

2.2 Deactivation
----------------
Occurs on:
- GOBACK
- EXIT PROGRAM
- End of PROCEDURE DIVISION

Deactivation:
- Restores caller ExecutionContext  
- Restores PERFORM stack  
- Returns RETURNING value (if any)  

------------------------------------------------------------
SECTION 3 — ENTRY POINTS
------------------------------------------------------------

3.1 ENTRY syntax
----------------
ENTRY "AltName" USING x y z.

3.2 Multiple ENTRY points
-------------------------
A program may define:
- Multiple ENTRY names
- Each with its own USING signature

3.3 ENTRY dispatch
------------------
CALL "AltName" → dispatches to ENTRY "AltName".

3.4 ENTRY and WORKING‑STORAGE
-----------------------------
WORKING‑STORAGE shared across ENTRY points.

------------------------------------------------------------
SECTION 4 — PARAMETER PASSING RULES
------------------------------------------------------------

4.1 BY REFERENCE (default)
--------------------------
- Caller passes address of StorageBlock region  
- Callee LINKAGE item overlays caller’s memory  
- Mutations visible to caller  

4.2 BY CONTENT
--------------
- Caller passes a copy  
- Callee receives independent buffer  
- Mutations NOT visible to caller  

4.3 BY VALUE
------------
- Caller passes primitive value  
- Stored in local variable, not StorageBlock  
- Used for numeric and simple types  

4.4 Mixed modes
---------------
CALL "P" USING BY REFERENCE A BY CONTENT B BY VALUE C.

Compiler enforces:
- Correct mapping  
- Correct LINKAGE SECTION order  

------------------------------------------------------------
SECTION 5 — LINKAGE SECTION MODEL
------------------------------------------------------------

5.1 Mapping rules
-----------------
Each USING parameter maps to:
- A LINKAGE SECTION item  
- In declared order  

5.2 BY REFERENCE mapping
------------------------
LINKAGE item overlays caller’s StorageBlock:
- Same offset  
- Same length  
- Same PIC  

5.3 BY CONTENT mapping
----------------------
Compiler allocates:
- Temporary StorageBlock  
- Copies caller’s data  

5.4 BY VALUE mapping
--------------------
Compiler allocates:
- Local variable  
- No StorageBlock entry  

------------------------------------------------------------
SECTION 6 — COMMON vs INITIAL PROGRAMS
------------------------------------------------------------

6.1 COMMON
----------
COMMON program:
- WORKING‑STORAGE allocated once  
- Retained across CALLs  
- Not reinitialized  

6.2 INITIAL
-----------
INITIAL program:
- WORKING‑STORAGE reinitialized on each CALL  

6.3 Default
-----------
If neither specified:
- Program is COMMON  

------------------------------------------------------------
SECTION 7 — RECURSION RULES
------------------------------------------------------------

7.1 Allowed
-----------
CobolSharp allows recursion:
- Each CALL creates new ExecutionContext  
- LOCAL‑STORAGE allocated per activation  
- WORKING‑STORAGE shared only if COMMON  

7.2 COMMON recursion
--------------------
Shared WORKING‑STORAGE across recursive calls:
- Deterministic  
- Caller/callee share state  

7.3 INITIAL recursion
---------------------
Each activation gets fresh WORKING‑STORAGE.

------------------------------------------------------------
SECTION 8 — RETURNING VALUES
------------------------------------------------------------

8.1 Syntax
----------
CALL "P" USING a b RETURNING r.

8.2 RETURNING rules
-------------------
- RETURNING item must be elementary  
- BY VALUE semantics  
- Returned via ExecutionContext.ReturnValue  

8.3 GOBACK RETURNING
--------------------
GOBACK RETURNING x.

Lowering:
- ctx.ReturnValue = x  
- Deactivate program  

------------------------------------------------------------
SECTION 9 — MULTI‑MODULE ARCHITECTURE
------------------------------------------------------------

9.1 ProgramRegistry
-------------------
Maps:
- Program names → .NET types  
- ENTRY names → methods  

9.2 Static linking
------------------
All modules resolved at compile time.

9.3 No dynamic loading
----------------------
CobolSharp forbids:
- CALL literal with runtime‑computed name  
- Reflection‑based loading  

------------------------------------------------------------
SECTION 10 — CIL LOWERING RULES
------------------------------------------------------------

10.1 CALL lowering
------------------
Generated IL:
- new ExecutionContext  
- Map USING parameters  
- Call ENTRY method  
- Retrieve ReturnValue  

10.2 LINKAGE lowering
---------------------
BY REFERENCE:
- Pass pointer to caller’s StorageBlock region  

BY CONTENT:
- Allocate temp buffer  
- Copy bytes  

BY VALUE:
- Pass primitive  

10.3 ENTRY lowering
-------------------
ENTRY "X":
- Compiler generates method Entry_X  

10.4 EXIT PROGRAM / GOBACK lowering
-----------------------------------
- Set return value  
- Restore caller ExecutionContext  
- Branch to return label  

------------------------------------------------------------
SECTION 11 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- CALL stack  
- ENTRY name  
- USING parameters  
- LINKAGE SECTION mapping  
- ReturnValue  
- COMMON/INITIAL state  

------------------------------------------------------------
SECTION 12 — AOT/WASM‑SAFE PROGRAM MODEL
------------------------------------------------------------

12.1 No reflection
------------------
All program bindings static.

12.2 No dynamic codegen
-----------------------
All ENTRY methods compiled ahead‑of‑time.

12.3 Deterministic activation
-----------------------------
ExecutionContext creation identical across platforms.

------------------------------------------------------------
SECTION 13 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

13.1 Too many USING parameters
------------------------------
Compile‑time error.

13.2 BY REFERENCE with mismatched PIC
-------------------------------------
Runtime error.

13.3 CALL to program with no ENTRY
----------------------------------
ENTRY = PROCEDURE DIVISION.

13.4 RETURNING with group item
------------------------------
Compile‑time error.

13.5 Recursive COMMON program modifies shared state
---------------------------------------------------
Allowed; deterministic.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Program Model:
- Implements full CALL/ENTRY semantics with deterministic parameter passing
- Supports BY REFERENCE, BY CONTENT, BY VALUE with precise LINKAGE mapping
- Provides COMMON/INITIAL program behavior and safe recursion
- Uses static ProgramRegistry for multi‑module linking
- Generates clean, verifiable, debugger‑friendly CIL
- Ensures correctness across CoreCLR, AOT, and WASM
