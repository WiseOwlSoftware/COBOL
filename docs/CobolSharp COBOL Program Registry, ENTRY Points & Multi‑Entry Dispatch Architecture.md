CobolSharp COBOL Program Registry, ENTRY Points & Multi‑Entry Dispatch Architecture (CIL‑Only)
==============================================================================================

Purpose
-------
Define the authoritative architecture for:
- COBOL ENTRY statements
- Multi‑entry program dispatch
- Program registry and lookup
- CALL literal vs CALL identifier resolution
- Parameter mapping for each ENTRY
- RETURNING semantics per ENTRY
- Activation of nested programs
- Integration with ExecutionContext
- CIL‑friendly dispatch tables
- AOT/WASM‑safe program lookup

This document governs how CobolSharp resolves, dispatches, and executes COBOL programs with multiple ENTRY points.

------------------------------------------------------------
SECTION 1 — PROGRAM REGISTRY OVERVIEW
------------------------------------------------------------

CobolSharp maintains a **Program Registry** containing:
- Program name → ProgramDescriptor
- ENTRY name → EntryDescriptor
- Metadata for USING/RETURNING
- Activation rules (COMMON, INITIAL)
- Assembly-qualified .NET type

The registry is:
- Built at compile time
- Embedded into the assembly
- Loaded at runtime into ExecutionContext.Runtime.ProgramRegistry

------------------------------------------------------------
SECTION 2 — PROGRAMDESCRIPTOR STRUCTURE
------------------------------------------------------------

Each program has:
- ProgramName
- EntryPoints (list of EntryDescriptor)
- HasCommonWorkingStorage
- HasInitialClause
- .NET Type reference
- MainEntry (default ENTRY)

EntryDescriptor contains:
- EntryName
- Parameter metadata
- RETURNING metadata
- MethodInfo for entry method

------------------------------------------------------------
SECTION 3 — ENTRY STATEMENT SEMANTICS
------------------------------------------------------------

3.1 Basic form
--------------
ENTRY "AltName" USING a b c RETURNING r.

3.2 ENTRY names
---------------
- Are case‑insensitive
- Must be unique within program
- May differ from PROGRAM-ID

3.3 ENTRY parameters
--------------------
Each ENTRY may define:
- Different USING parameters
- Different RETURNING type
- Different linkage mapping

3.4 ENTRY execution
-------------------
CALL "AltName":
- Activates program
- Dispatches to ENTRY "AltName"
- Maps USING parameters
- Executes ENTRY procedure division

------------------------------------------------------------
SECTION 4 — CALL RESOLUTION
------------------------------------------------------------

4.1 CALL literal
----------------
CALL "PROG":
- Lookup ProgramDescriptor by name
- Use MainEntry if no ENTRY matches

CALL "ENTRYNAME":
- Lookup EntryDescriptor by name
- Use associated program

4.2 CALL identifier
-------------------
CALL var:
- Evaluate var at runtime
- Lookup in registry
- Same resolution rules

4.3 CALL not found
------------------
Runtime error:
PROGRAM-NOT-FOUND

------------------------------------------------------------
SECTION 5 — PARAMETER MAPPING
------------------------------------------------------------

5.1 USING BY REFERENCE
----------------------
Caller passes:
- Pointer to storage block
- Callee LINKAGE SECTION points to same memory

5.2 USING BY CONTENT
--------------------
Caller passes:
- Copy of data
- Callee receives independent buffer

5.3 USING OMITTED
-----------------
Allowed if ENTRY parameter is optional.

5.4 RETURNING
-------------
Callee sets:
- RETURNING value in ExecutionContext.ReturnValue

Caller receives:
- Value assigned to target

------------------------------------------------------------
SECTION 6 — PROGRAM ACTIVATION MODEL
------------------------------------------------------------

6.1 Activation steps
--------------------
1. Create new ExecutionContext  
2. Allocate WORKING‑STORAGE (unless COMMON)  
3. Allocate LOCAL‑STORAGE  
4. Map LINKAGE SECTION  
5. Initialize subsystems  
6. Dispatch to ENTRY method  

6.2 COMMON WORKING‑STORAGE
--------------------------
Shared across activations.

6.3 INITIAL clause
------------------
Reinitializes WORKING‑STORAGE on each activation.

------------------------------------------------------------
SECTION 7 — MULTI‑ENTRY DISPATCH
------------------------------------------------------------

7.1 Dispatch table
------------------
Compiler generates:
- A static dictionary: EntryName → MethodInfo
- A switch table for fast dispatch

7.2 Lowering
------------
CALL "EntryName":
- Load EntryName
- Lookup EntryDescriptor
- newobj ProgramType
- call EntryMethod(ctx, args)

7.3 ENTRY with no USING
-----------------------
Method signature:
void EntryName(ExecutionContext ctx)

7.4 ENTRY with USING
--------------------
Method signature:
void EntryName(ExecutionContext ctx, object[] args)

------------------------------------------------------------
SECTION 8 — NESTED PROGRAM ACTIVATION
------------------------------------------------------------

8.1 CALL inside program
-----------------------
CALL "ChildProg":
- Creates new ExecutionContext
- Child program runs independently
- GOBACK returns to parent

8.2 ENTRY inside nested program
-------------------------------
Allowed:
- Each nested program may have multiple ENTRY points

8.3 RECURSIVE CALL
------------------
Allowed:
- Each activation receives its own ExecutionContext

------------------------------------------------------------
SECTION 9 — CIL LOWERING RULES
------------------------------------------------------------

9.1 ENTRY lowering
------------------
ENTRY "Name" USING a b:
- Compiler generates:
public void Entry_Name(ExecutionContext ctx, object[] args)

9.2 CALL lowering
-----------------
Generated IL:
- ldstr entryName
- call ProgramRegistry.Lookup
- newobj ProgramType
- callvirt EntryMethod

9.3 RETURNING lowering
----------------------
Callee:
ctx.ReturnValue = value  
ret

Caller:
store ctx.ReturnValue into target

------------------------------------------------------------
SECTION 10 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Active ENTRY name
- USING parameters
- RETURNING value
- Program activation stack
- LINKAGE SECTION mapping
- COMMON vs non‑COMMON storage

------------------------------------------------------------
SECTION 11 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

11.1 ENTRY not reached
----------------------
If ENTRY is never CALLed:
- Program never activates

11.2 CALL to program with no ENTRY
----------------------------------
Main entry is PROGRAM-ID.

11.3 CALL to ENTRY with wrong parameter count
---------------------------------------------
Runtime error:
PARAMETER-MISMATCH

11.4 ENTRY inside declarative
-----------------------------
Illegal.

11.5 ENTRY with same name as program
------------------------------------
Allowed; program name is default ENTRY.

11.6 CALL to ENTRY inside same program
--------------------------------------
Allowed; creates new activation.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Program Registry & ENTRY Architecture:
- Implements full COBOL multi‑entry program semantics
- Supports independent USING/RETURNING signatures per ENTRY
- Provides deterministic dispatch via registry and lookup tables
- Integrates tightly with ExecutionContext and CALL stack
- Generates clean, verifiable, debugger‑friendly CIL
- Ensures correctness across CoreCLR, AOT, and WASM
