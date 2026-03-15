CobolSharp COBOL Debugging & PDB Symbol Generation Architecture (CIL‑Only)
========================================================================

Purpose
-------
Define the authoritative architecture for:
- Debug symbol generation (PDB)
- Sequence point mapping
- Variable inspection
- Paragraph/section stepping
- PERFORM flow visualization
- Exception mapping
- Integration with .NET debugging APIs
- LSP + DAP (Debug Adapter Protocol) integration
- CIL‑friendly debug metadata emission

This document governs how CobolSharp enables first‑class debugging for COBOL on .NET.

------------------------------------------------------------
SECTION 1 — DEBUGGING MODEL OVERVIEW
------------------------------------------------------------

CobolSharp debugging is built on three pillars:

1. **PDB symbol generation**  
   Mapping COBOL source → CIL instructions.

2. **Runtime inspection**  
   Mapping COBOL storage → .NET objects.

3. **IDE integration**  
   LSP + DAP providing breakpoints, stepping, variables, call stack.

The debugging model is:
- Deterministic
- Fully CIL‑based
- Compatible with CoreCLR, AOT, and WASM (via dotnet publish)
- Independent of any custom VM

------------------------------------------------------------
SECTION 2 — PDB SYMBOL GENERATION
------------------------------------------------------------

2.1 PDB format
--------------
CobolSharp emits:
- Portable PDBs (cross‑platform)
- Embedded source mapping
- Custom metadata for COBOL constructs

2.2 Sequence points
-------------------
Sequence points map:
- COBOL source spans
- To CIL instruction offsets

Sequence points are emitted for:
- Statement start
- Paragraph entry
- Section entry
- PERFORM entry/exit
- Branch targets
- Exception handlers

2.3 Source mapping
------------------
CobolSharp preserves:
- Original source span
- Preprocessed source span
- COPY/REPLACE mapping

PDB stores:
- File path
- Line/column
- Expanded → original mapping

2.4 Hidden sequence points
--------------------------
Used for:
- Compiler‑generated code
- PERFORM loop scaffolding
- Exception region boundaries

------------------------------------------------------------
SECTION 3 — VARIABLE & STORAGE INSPECTION
------------------------------------------------------------

3.1 Variable categories
-----------------------
CobolSharp supports inspection of:
- Elementary items
- Group items
- OCCURS arrays
- OCCURS DEPENDING ON logical length
- REDEFINES overlays
- Condition names (88‑levels)
- File buffers
- OO object fields
- Local variables (in methods)

3.2 StorageBlock inspection
---------------------------
Each COBOL data region is backed by a StorageBlock.

Debugger can:
- Read raw bytes
- Interpret bytes as DISPLAY, COMP, COMP‑3, COMP‑5
- Interpret OCCURS arrays
- Interpret REDEFINES overlays
- Interpret condition names

3.3 Display formatting
----------------------
Debugger formats values as:
- DISPLAY strings
- Numeric values (decimal/binary)
- Packed decimal decoded
- Boolean (TRUE/FALSE)
- Hex dump (optional)

3.4 Group item visualization
----------------------------
Debugger shows:
- Tree view of nested fields
- Offsets and sizes
- REDEFINES relationships
- OCCURS bounds

------------------------------------------------------------
SECTION 4 — CALL STACK & EXECUTION CONTEXT
------------------------------------------------------------

4.1 COBOL call stack frames
---------------------------
Each frame includes:
- Program/class name
- Paragraph/section name
- Method name (if OO)
- Source location
- PERFORM nesting depth

4.2 PERFORM flow visualization
------------------------------
Debugger reconstructs:
- PERFORM call chain
- PERFORM UNTIL loop state
- PERFORM VARYING iteration variables

4.3 Exception frames
--------------------
Debugger shows:
- ON EXCEPTION handler
- INVALID KEY handler
- AT END handler
- Underlying .NET exception (if any)

------------------------------------------------------------
SECTION 5 — BREAKPOINTS
------------------------------------------------------------

5.1 Breakpoint types
--------------------
CobolSharp supports:
- Line breakpoints
- Conditional breakpoints
- Hit‑count breakpoints
- Paragraph breakpoints
- Section breakpoints

5.2 Breakpoint binding
----------------------
Breakpoints bind to:
- Sequence points
- Paragraph entry points
- Section entry points

5.3 Breakpoint resolution
-------------------------
If a line has no executable code:
- Bind to next executable statement
- Or mark as unbound

------------------------------------------------------------
SECTION 6 — STEPPING SEMANTICS
------------------------------------------------------------

6.1 Step Over
-------------
- Executes current statement
- Steps over PERFORM
- Steps over CALL/INVOKE

6.2 Step Into
-------------
- Steps into:
  - Paragraphs
  - Sections
  - Methods
  - PERFORM bodies
  - CALL/INVOKE targets

6.3 Step Out
------------
- Steps out of:
  - Paragraph
  - Section
  - Method
  - PERFORM

6.4 Step Through PERFORM
------------------------
CobolSharp provides:
- Step into PERFORM body
- Step over PERFORM loop scaffolding

------------------------------------------------------------
SECTION 7 — EXCEPTION MAPPING
------------------------------------------------------------

7.1 COBOL exceptions → .NET exceptions
--------------------------------------
Examples:
- SIZE ERROR → ArithmeticException
- INVALID KEY → KeyNotFoundException
- AT END → EndOfStreamException
- JSON/XML errors → JsonException/XmlException

7.2 .NET exceptions → COBOL exceptions
--------------------------------------
If a .NET exception occurs inside COBOL code:
- Map to ON EXCEPTION if applicable
- Otherwise propagate as .NET exception

7.3 Debugger display
--------------------
Debugger shows:
- COBOL exception category
- Underlying .NET exception
- Storage state at time of exception

------------------------------------------------------------
SECTION 8 — CIL BACKEND INTEGRATION
------------------------------------------------------------

8.1 Debuggable CIL emission
---------------------------
CIL backend emits:
- Sequence points
- Local variable signatures
- Exception regions
- Custom attributes for COBOL metadata

8.2 Local variable mapping
--------------------------
Locals correspond to:
- Temporary values
- Loop variables
- PERFORM scaffolding
- Method parameters

8.3 Paragraph/section mapping
-----------------------------
Paragraphs map to:
- CIL methods (optional)
- Or CIL basic blocks with labels

------------------------------------------------------------
SECTION 9 — LSP + DAP INTEGRATION
------------------------------------------------------------

9.1 LSP responsibilities
------------------------
- Hover info
- Symbol lookup
- Diagnostics
- Code actions

9.2 DAP responsibilities
------------------------
- Breakpoints
- Stepping
- Stack traces
- Variable inspection
- Exception reporting

9.3 Combined experience
-----------------------
IDE shows:
- COBOL source
- Variables panel with COBOL formatting
- Call stack with paragraphs/sections
- Watch expressions
- Memory view

------------------------------------------------------------
SECTION 10 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

10.1 COPY/REPLACE debugging
---------------------------
Debugger shows:
- Original source
- Expanded source
- COPYbook file path

10.2 REDEFINES inspection
-------------------------
Debugger shows:
- All views simultaneously
- Highlighting overlapping regions

10.3 OCCURS DEPENDING ON
------------------------
Debugger shows:
- Logical length
- Max length
- Hidden storage

10.4 Packed decimal corruption
------------------------------
Debugger shows:
- Raw bytes
- Decoded value (if valid)
- Error state (if invalid)

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Debugging & PDB Symbol Generation Architecture:
- Provides first‑class debugging for COBOL on .NET
- Emits rich PDB metadata with full source mapping
- Supports stepping, breakpoints, variable inspection, and call stacks
- Handles REDEFINES, OCCURS, packed decimals, and COBOL exceptions
- Integrates seamlessly with .NET debugging APIs and IDEs
- Ensures deterministic, CIL‑only debug behavior
- Forms the foundation for a modern COBOL development experience
