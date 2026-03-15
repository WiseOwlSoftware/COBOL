CobolSharp COBOL Debugger, Sequence‑Point, Symbol & Storage Visualization Architecture (CIL‑Only)
================================================================================================

Purpose
-------
Define the authoritative architecture for:
- Debugger integration
- Sequence‑point generation
- Symbol mapping for WORKING‑STORAGE, LOCAL‑STORAGE, LINKAGE
- COPY/REPLACE source mapping
- Paragraph/section stepping
- PERFORM stack visualization
- CALL stack visualization
- Record buffer visualization (FD)
- JSON/XML event visualization
- Report Writer visualization
- AOT/WASM‑safe debugging

This document governs how CobolSharp exposes COBOL program state to .NET debuggers.

------------------------------------------------------------
SECTION 1 — DEBUGGER INTEGRATION OVERVIEW
------------------------------------------------------------

CobolSharp emits:
- Portable PDB files
- Sequence points mapped to original COBOL source
- Local variable symbols
- Storage block visualizers
- Custom DebuggerDisplay attributes

Debugger features supported:
- Step into COPY books
- Step through PERFORM loops
- Step into ENTRY points
- Inspect storage blocks
- Inspect record buffers
- Inspect JSON/XML parse events
- Inspect Report Writer state

------------------------------------------------------------
SECTION 2 — SEQUENCE‑POINT ARCHITECTURE
------------------------------------------------------------

2.1 Sequence point per COBOL statement
--------------------------------------
Each COBOL statement generates:
- One sequence point
- Mapped to original file/line/column

2.2 COPY/REPLACE mapping
------------------------
Sequence points map to:
- Original COPY file
- Original line/column
- Even after REPLACE BEFORE/AFTER

2.3 Paragraph/section entry
---------------------------
Paragraph label generates:
- Sequence point
- Debugger stops at paragraph entry

2.4 Declarative entry
---------------------
Declarative sections generate:
- Sequence points
- Debugger can step into declaratives

------------------------------------------------------------
SECTION 3 — SYMBOL GENERATION
------------------------------------------------------------

3.1 WORKING‑STORAGE symbols
---------------------------
Each field becomes:
- A debugger symbol
- With name identical to COBOL name
- With offset metadata

3.2 LOCAL‑STORAGE symbols
-------------------------
Allocated per method:
- Debugger shows local variables
- Lifetime scoped to method

3.3 LINKAGE SECTION symbols
---------------------------
Debugger shows:
- BY REFERENCE pointers
- BY CONTENT copies

3.4 Temporary locals
--------------------
Compiler‑generated locals:
- Hidden unless needed
- Named: temp_1, temp_2, etc.

------------------------------------------------------------
SECTION 4 — STORAGE VISUALIZATION
------------------------------------------------------------

4.1 StorageBlock visualizer
---------------------------
Debugger displays:
- Field name
- PIC/USAGE
- Offset
- Raw bytes
- Decoded value

4.2 Nested groups
-----------------
Displayed as:
- Expandable tree
- Each child field shown with decoded value

4.3 OCCURS tables
-----------------
Displayed as:
- Array of elements
- Each element expandable

4.4 COMP‑3 visualization
------------------------
Shows:
- Raw packed bytes
- Unpacked decimal value

4.5 NATIONAL visualization
--------------------------
Shows:
- UTF‑16 code units
- Decoded string

------------------------------------------------------------
SECTION 5 — PERFORM STACK VISUALIZATION
------------------------------------------------------------

5.1 PERFORM frame
-----------------
Debugger shows:
- Paragraph entry
- Return label
- Loop variables (for VARYING)
- Nesting depth

5.2 PERFORM THRU
----------------
Shows:
- Start paragraph
- End paragraph

5.3 EXIT PARAGRAPH / EXIT SECTION
---------------------------------
Debugger shows:
- PERFORM stack pop

------------------------------------------------------------
SECTION 6 — CALL STACK VISUALIZATION
------------------------------------------------------------

6.1 CALL literal
----------------
Debugger shows:
- Program name
- ENTRY name
- USING parameters

6.2 CALL identifier
-------------------
Debugger shows:
- Resolved program name
- Resolved ENTRY

6.3 RETURNING
-------------
Debugger shows:
- Return value
- Caller’s receiving variable

------------------------------------------------------------
SECTION 7 — RECORD BUFFER VISUALIZATION (FD)
------------------------------------------------------------

7.1 FD structure
----------------
Debugger shows:
- Record name
- Field offsets
- Field values
- Raw bytes

7.2 Indexed file keys
---------------------
Debugger shows:
- Primary key value
- Alternate key values

7.3 Deleted/invalid records
---------------------------
Debugger shows:
- Deleted flag
- Status code

------------------------------------------------------------
SECTION 8 — JSON/XML VISUALIZATION
------------------------------------------------------------

8.1 JSON PARSE
--------------
Debugger shows:
- Current event
- Path
- Value
- Error metadata

8.2 XML PARSE
-------------
Debugger shows:
- Event type
- Element name
- Attribute name/value
- Depth
- Character data

8.3 JSON/XML GENERATE
---------------------
Debugger shows:
- Current field
- Output buffer preview

------------------------------------------------------------
SECTION 9 — REPORT WRITER VISUALIZATION
------------------------------------------------------------

9.1 Page/line state
-------------------
Debugger shows:
- Current page
- Current line
- Page limit

9.2 Control break state
-----------------------
Debugger shows:
- Current control keys
- Previous control keys
- Break level

9.3 Accumulators
----------------
Debugger shows:
- SUM
- COUNT
- AVERAGE
- MIN/MAX

9.4 Rendered line preview
-------------------------
Debugger shows:
- ASCII preview of line
- Column positions

------------------------------------------------------------
SECTION 10 — AOT/WASM‑SAFE DEBUGGING
------------------------------------------------------------

10.1 No dynamic IL
------------------
Debugger relies on:
- Static PDB
- Static IL

10.2 No reflection
------------------
Symbols generated at compile time.

10.3 Deterministic stepping
---------------------------
Sequence points guarantee:
- No JIT‑dependent stepping
- Identical behavior across platforms

------------------------------------------------------------
SECTION 11 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

11.1 COPY inside COPY
---------------------
Debugger steps into nested COPY.

11.2 REPLACE BEFORE/AFTER
-------------------------
Debugger shows:
- Original source
- Not replaced text

11.3 Declarative triggered during stepping
------------------------------------------
Debugger jumps to declarative entry.

11.4 PERFORM VARYING with multiple indices
------------------------------------------
Debugger shows:
- All loop variables
- Current iteration

11.5 JSON/XML exception
-----------------------
Debugger shows:
- ExceptionState
- Error location

11.6 STOP RUN during debugging
------------------------------
Debugger terminates session.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Debugger Architecture:
- Provides full visibility into COBOL storage, control flow, and runtime state
- Supports COPY/REPLACE mapping, PERFORM stack, CALL stack, and FD buffers
- Integrates JSON/XML and Report Writer visualization
- Emits deterministic, verifiable sequence points for AOT/WASM
- Ensures clean, intuitive debugging for complex COBOL programs
