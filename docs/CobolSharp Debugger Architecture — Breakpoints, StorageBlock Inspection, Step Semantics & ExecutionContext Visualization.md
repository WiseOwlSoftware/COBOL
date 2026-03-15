CobolSharp Debugger Architecture — Breakpoints, StorageBlock Inspection, Step Semantics & ExecutionContext Visualization (CIL‑Only)
=================================================================================================================================

Purpose
-------
Define the authoritative architecture for:
- Breakpoints (line, paragraph, conditional)
- Step semantics (STEP INTO, STEP OVER, STEP OUT)
- ExecutionContext visualization
- StorageBlock inspection (decoded and raw)
- ObjectTable inspection
- FileManager state inspection
- PERFORM stack and CALL stack visualization
- Declarative and exception tracing
- IL‑level mapping and sequence points
- AOT/WASM‑safe debugging

This document governs how CobolSharp integrates with .NET debugging infrastructure to provide a full COBOL‑aware debugging experience.

------------------------------------------------------------
SECTION 1 — DEBUGGER OVERVIEW
------------------------------------------------------------

CobolSharp debugging provides:
- Source‑level stepping
- Paragraph‑level stepping
- Breakpoints on COBOL lines
- Breakpoints on paragraphs/sections
- Inspection of:
  - StorageBlocks (decoded)
  - ObjectTable
  - FileManager state
  - ExecutionContext
  - PERFORM stack
  - CALL stack
  - ExceptionState
- Deterministic stepping across CoreCLR, AOT, and WASM

Debugger integration is:
- Pure managed
- Sequence‑point based
- Platform‑independent

------------------------------------------------------------
SECTION 2 — BREAKPOINT ARCHITECTURE
------------------------------------------------------------

2.1 Line breakpoints
--------------------
Set on:
- Any PROCEDURE DIVISION statement
- ENTRY statements
- Declarative procedures

Compiler emits:
- Sequence points for each statement
- Mapping from COBOL line → IL offset

2.2 Paragraph breakpoints
-------------------------
Breakpoint on:
- Paragraph label
- SECTION label

Compiler emits:
- Sequence point at paragraph entry

2.3 Conditional breakpoints
---------------------------
Supported conditions:
- Numeric comparisons
- String comparisons
- Boolean expressions

Lowering:
- Debugger evaluates condition using locals and StorageBlocks

2.4 Breakpoint binding
----------------------
Bound at:
- Compile time (preferred)
- Runtime (via PDB)

------------------------------------------------------------
SECTION 3 — STEP SEMANTICS
------------------------------------------------------------

3.1 STEP INTO
-------------
Steps into:
- Paragraphs
- Sections
- ENTRY points
- Declaratives
- INVOKE (instance/static)
- CALL statements

3.2 STEP OVER
-------------
Executes:
- Entire paragraph
- Entire CALL
- Entire INVOKE
- Entire PERFORM range

3.3 STEP OUT
------------
Returns to:
- Caller paragraph
- Caller program (if stepping out of CALL)
- Caller of declarative

3.4 Deterministic stepping
--------------------------
Stepping behavior identical across:
- CoreCLR
- AOT
- WASM

------------------------------------------------------------
SECTION 4 — EXECUTIONCONTEXT VISUALIZATION
------------------------------------------------------------

4.1 Context fields shown
------------------------
Debugger displays:
- Program name
- ENTRY name
- COMMON/INITIAL flag
- ReturnValue
- ExceptionState
- Active declarative (if any)
- Current PERFORM frame
- Current CALL frame

4.2 Context switching
---------------------
On CALL:
- Debugger switches to callee context

On GOBACK:
- Debugger returns to caller context

------------------------------------------------------------
SECTION 5 — STORAGEBLOCK INSPECTION
------------------------------------------------------------

5.1 Raw view
------------
Debugger shows:
- byte[] Buffer
- Offsets
- Lengths

5.2 Decoded view
----------------
Debugger decodes:
- DISPLAY → ASCII
- NATIONAL → UTF‑16
- COMP‑3 → packed decimal
- COMP‑5 → binary integer
- Numeric DISPLAY → Decimal

5.3 Group view
--------------
Debugger shows:
- Group hierarchy
- Nested fields
- OCCURS tables
- ODO active length

5.4 REDEFINES view
------------------
Debugger highlights:
- Overlaid regions
- Conflicting interpretations

------------------------------------------------------------
SECTION 6 — OBJECTTABLE INSPECTION
------------------------------------------------------------

6.1 Object references
---------------------
Debugger shows:
- Index
- .NET type
- COBOL class name (if applicable)
- Null vs non‑null

6.2 Instance fields
-------------------
Debugger displays:
- FACTORY fields (static)
- OBJECT fields (instance)

6.3 Lifetime
------------
Objects remain visible until:
- ExecutionContext destroyed
- Reference overwritten

------------------------------------------------------------
SECTION 7 — FILEMANAGER STATE INSPECTION
------------------------------------------------------------

7.1 File state
--------------
Debugger shows:
- Open mode (INPUT/OUTPUT/I‑O/EXTEND)
- Organization (SEQUENTIAL/INDEXED/RELATIVE)
- Access mode (SEQUENTIAL/RANDOM/DYNAMIC)
- Current record position
- File status code

7.2 Record buffer
-----------------
Debugger displays:
- Raw bytes
- Decoded fields
- Key values

------------------------------------------------------------
SECTION 8 — PERFORM STACK VISUALIZATION
------------------------------------------------------------

8.1 PERFORM frame fields
------------------------
Debugger shows:
- Return label
- THRU range
- Loop variables
- Loop bounds
- TEST BEFORE/AFTER flag

8.2 Nested PERFORMs
-------------------
Displayed as:
- Stack of frames
- Top = current PERFORM

------------------------------------------------------------
SECTION 9 — CALL STACK VISUALIZATION
------------------------------------------------------------

9.1 CALL frame fields
---------------------
Debugger shows:
- Program name
- ENTRY name
- USING parameters
- RETURNING target
- Caller location

9.2 Multi‑module calls
----------------------
Debugger displays:
- Full chain of CALLs
- Cross‑module transitions

------------------------------------------------------------
SECTION 10 — DECLARATIVE & EXCEPTION TRACING
------------------------------------------------------------

10.1 Declarative entry
----------------------
Debugger highlights:
- Declarative triggered
- Source of error
- ExceptionState contents

10.2 Resumption
---------------
Debugger shows:
- Resume location after declarative
- Cleared ExceptionState

------------------------------------------------------------
SECTION 11 — IL‑LEVEL MAPPING
------------------------------------------------------------

11.1 Sequence points
--------------------
Compiler emits:
- One sequence point per COBOL statement
- Additional points for paragraph labels

11.2 IL correlation
-------------------
Debugger can show:
- IL for current statement
- IL for paragraph
- IL for ENTRY method

11.3 WASM mapping
-----------------
Sequence points preserved in AOT → WASM pipeline.

------------------------------------------------------------
SECTION 12 — AOT/WASM‑SAFE DEBUGGING
------------------------------------------------------------

12.1 No dynamic codegen
-----------------------
All debug info static.

12.2 No reflection
------------------
Debugger uses PDB metadata only.

12.3 Deterministic stepping
---------------------------
Identical stepping behavior across platforms.

------------------------------------------------------------
SECTION 13 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

13.1 Breakpoint inside REDEFINES
--------------------------------
Debugger shows both interpretations.

13.2 Breakpoint inside OCCURS DEPENDING ON
------------------------------------------
Debugger shows active and max elements.

13.3 Stepping into declarative
------------------------------
Debugger enters declarative normally.

13.4 Stepping out of nested PERFORM
-----------------------------------
Debugger unwinds PERFORM frames correctly.

13.5 CALL to program with no ENTRY
----------------------------------
Debugger enters PROCEDURE DIVISION directly.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Debugger Architecture:
- Provides full COBOL‑aware debugging with StorageBlock, ObjectTable, FileManager, and ExecutionContext visualization
- Supports breakpoints, stepping, declarative tracing, and IL mapping
- Ensures deterministic debugging across CoreCLR, AOT, and WASM
- Generates clean, verifiable, debugger‑friendly CIL with precise sequence points
