CobolSharp COBOL Debugger, Sequence Point & Symbol Mapping Architecture (CIL‑Only)
=================================================================================

Purpose
-------
Define the authoritative architecture for:
- Debugger integration
- Sequence point generation
- Source → IL mapping
- COPY/REPLACE mapping
- Declarative and exception mapping
- Paragraph/section stepping
- Variable inspection (including REDEFINES and OCCURS)
- Breakpoint resolution
- Call stack and PERFORM stack visualization
- Integration with PDB generation and .NET debugging APIs

This document governs how CobolSharp provides a first‑class debugging experience for COBOL on .NET.

------------------------------------------------------------
SECTION 1 — DEBUGGER MODEL OVERVIEW
------------------------------------------------------------

CobolSharp’s debugger integration is built on:
- Portable PDBs
- Sequence points for every COBOL statement
- Symbol mapping for COPY/REPLACE
- Paragraph/section metadata
- ExecutionContext inspection
- StorageBlock visualization
- PERFORM stack and call stack introspection

Debugger goals:
- Accurate stepping through COBOL source
- Accurate variable inspection
- Accurate exception reporting
- Zero ambiguity between expanded and original source
- Full compatibility with Visual Studio, VS Code, Rider, and any .NET debugger

------------------------------------------------------------
SECTION 2 — SEQUENCE POINT ARCHITECTURE
------------------------------------------------------------

2.1 Sequence point placement
----------------------------
Sequence points are emitted for:
- Paragraph entry
- Section entry
- Every COBOL statement
- PERFORM entry and exit
- INVOKE and CALL
- JSON/XML operations
- File I/O operations
- Exception handler entry
- Declarative entry

2.2 Hidden sequence points
--------------------------
Used for:
- Loop boundaries
- Compiler‑generated scaffolding
- Exception region boundaries

2.3 Mapping to original source
------------------------------
Each sequence point maps to:
- Original file path
- Original line/column
- Expanded source span (if COPY/REPLACE applied)

------------------------------------------------------------
SECTION 3 — COPY/REPLACE SOURCE MAPPING
------------------------------------------------------------

3.1 Mapping table
-----------------
Preprocessor produces:
- A mapping from expanded text → original source
- Nested mappings for nested COPYs
- REPLACE mapping (original span preserved)

3.2 Debugger behavior
---------------------
Debugger displays:
- Original source file
- Original line numbers
- Not the expanded COPY text

3.3 Breakpoint resolution
-------------------------
Breakpoints set in:
- COPY books → map to expanded code
- Main source → map directly

------------------------------------------------------------
SECTION 4 — PARAGRAPH & SECTION DEBUGGING
------------------------------------------------------------

4.1 Paragraph stepping
----------------------
Debugger shows:
- Current paragraph name
- Current section name

4.2 Paragraph entry sequence point
----------------------------------
Generated at:
Para-A.

4.3 Section entry sequence point
--------------------------------
Generated at:
Section-A SECTION.

4.4 GO TO behavior
------------------
Debugger jumps to:
- Target paragraph’s entry sequence point

------------------------------------------------------------
SECTION 5 — VARIABLE INSPECTION
------------------------------------------------------------

5.1 StorageBlock visualization
------------------------------
Debugger shows:
- Field name
- PIC clause
- Offset
- Length
- Raw bytes
- Decoded value (DISPLAY, COMP, COMP‑3)

5.2 REDEFINES
-------------
Debugger shows:
- All overlapping fields
- Active view (if known)
- Raw bytes shared

5.3 OCCURS
----------
Debugger shows:
- Array length (logical)
- Max length (physical)
- Each element as child node

5.4 OCCURS DEPENDING ON
------------------------
Debugger shows:
- Logical length
- DEPENDING ON value

5.5 Condition names (88‑levels)
-------------------------------
Debugger shows:
- TRUE/FALSE
- Parent field value

------------------------------------------------------------
SECTION 6 — CALL STACK & PERFORM STACK
------------------------------------------------------------

6.1 Call stack
--------------
Debugger shows:
- Program name
- Paragraph/section
- ENTRY point (if applicable)

6.2 PERFORM stack
-----------------
Debugger shows:
- Active PERFORM ranges
- Loop variables (for VARYING)
- Return labels

6.3 Declarative stack
---------------------
Debugger shows:
- Active declarative handler
- USE condition that triggered it

------------------------------------------------------------
SECTION 7 — EXCEPTION REPORTING
------------------------------------------------------------

7.1 ExceptionState integration
------------------------------
Debugger shows:
- Exception category
- File status (if applicable)
- Error message
- Raw .NET exception (if present)

7.2 Declarative routing
-----------------------
Debugger highlights:
- Declarative section entered
- Failing statement
- ExceptionState contents

7.3 SIZE ERROR
--------------
Debugger shows:
- Overflow details
- Target field
- Operation that overflowed

------------------------------------------------------------
SECTION 8 — BREAKPOINT RESOLUTION
------------------------------------------------------------

8.1 Breakpoints in COPY books
-----------------------------
Mapped to:
- Expanded code
- Correct IL offsets

8.2 Breakpoints in REPLACE regions
----------------------------------
Mapped to:
- Original source span
- Replacement text inherits mapping

8.3 Breakpoints in declaratives
-------------------------------
Fully supported.

8.4 Breakpoints in nested programs
----------------------------------
Mapped to:
- Correct ExecutionContext
- Correct program class

------------------------------------------------------------
SECTION 9 — CIL EMISSION FOR DEBUGGING
------------------------------------------------------------

9.1 PDB generation
------------------
CobolSharp emits:
- Portable PDBs
- Sequence points
- Local variable scopes
- Custom metadata for COBOL constructs

9.2 Local variable mapping
--------------------------
Locals represent:
- Temporary values
- Loop variables
- PERFORM metadata
- Function arguments

9.3 IL structure
----------------
Backend ensures:
- Structured control flow
- No irreducible loops
- No overlapping exception regions
- Debugger‑friendly branching

------------------------------------------------------------
SECTION 10 — WATCH, LOCALS & INSPECTION
------------------------------------------------------------

10.1 WATCH window
-----------------
Supports:
- Data items
- Group items
- OCCURS elements
- Condition names
- OO object fields

10.2 LOCALS window
------------------
Shows:
- Temporary locals
- Loop counters
- RETURNING values
- INVOKE arguments

10.3 RAW VIEW
-------------
Debugger can show:
- Raw bytes of any field
- Packed decimal representation
- Binary representation

------------------------------------------------------------
SECTION 11 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

11.1 Stepping into COPY book
----------------------------
Debugger steps into:
- Original COPY source file

11.2 Stepping through REDEFINES
-------------------------------
Debugger shows:
- All overlapping fields
- Raw bytes unchanged

11.3 Stepping through declaratives
----------------------------------
Debugger shows:
- Declarative entry
- USE condition
- Return to failing statement

11.4 Stepping through PERFORM VARYING
-------------------------------------
Debugger shows:
- Loop variable changes
- Loop boundaries

11.5 Stepping through JSON/XML
------------------------------
Debugger shows:
- Parsed values
- Event callbacks (XML)
- Exception details

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Debugger, Sequence Point & Symbol Mapping Architecture:
- Provides full COBOL‑aware debugging on .NET
- Preserves original source mapping across COPY/REPLACE
- Supports paragraph/section stepping and PERFORM stack visualization
- Enables accurate variable inspection, including REDEFINES and OCCURS
- Integrates tightly with ExecutionContext and runtime engines
- Generates clean, verifiable, debugger‑friendly CIL
- Ensures correctness across CoreCLR, AOT, and WASM
