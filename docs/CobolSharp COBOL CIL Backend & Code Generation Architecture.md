CobolSharp COBOL CIL Backend & Code Generation Architecture (CIL‑Only)
=====================================================================

Purpose
-------
Define the authoritative architecture for:
- CIL code generation
- Method and type emission
- Storage layout and FieldOffset emission
- Control‑flow lowering
- Exception region generation
- Debugger sequence point emission
- Integration with ExecutionContext
- Integration with IR and optimizer
- AOT/WASM‑safe IL generation

This document governs how CobolSharp emits verifiable, efficient, and debugger‑friendly CIL.

------------------------------------------------------------
SECTION 1 — BACKEND OVERVIEW
------------------------------------------------------------

CobolSharp’s backend is a **structured CIL generator** that:
- Emits explicit‑layout types for COBOL storage
- Emits static classes for programs
- Emits instance classes for OO programs
- Emits methods for paragraphs/sections (configurable)
- Emits structured IL for loops, branches, and PERFORM
- Emits try/catch regions for ON EXCEPTION, INVALID KEY, SIZE ERROR
- Emits PDB sequence points for debugging

Backend goals:
- Deterministic IL
- Verifiable IL
- Debugger‑friendly IL
- AOT/WASM compatible
- No dynamic code generation

------------------------------------------------------------
SECTION 2 — TYPE EMISSION
------------------------------------------------------------

2.1 Storage classes
-------------------
Each COBOL storage region becomes an explicit‑layout class:

[StructLayout(LayoutKind.Explicit)]
public class WORKING_STORAGE {
    [FieldOffset(0)]  byte[] FieldA;
    [FieldOffset(10)] byte[] FieldB;
}

Regions:
- WORKING‑STORAGE
- LOCAL‑STORAGE
- LINKAGE
- File record buffers
- OCCURS arrays (as nested types)

2.2 Packed decimal fields
-------------------------
Packed decimal fields emitted as:
- byte[] with fixed size
- Accessed via NumericEngine

2.3 Group items
---------------
Group items become:
- Nested classes
- With explicit offsets for children

------------------------------------------------------------
SECTION 3 — METHOD EMISSION
------------------------------------------------------------

3.1 Program entry method
------------------------
Generated as:
public static void Main(ExecutionContext ctx)

3.2 Paragraph/section methods
-----------------------------
Two modes:

Mode A: Paragraphs as methods  
Mode B: Paragraphs as basic blocks inside a single method  

CobolSharp defaults to:
- Paragraphs as basic blocks
- Sections as labels

3.3 Declarative handlers
------------------------
Declaratives become:
- Separate methods
- Registered in ExecutionContext

------------------------------------------------------------
SECTION 4 — CONTROL‑FLOW LOWERING
------------------------------------------------------------

4.1 IF/ELSE
-----------
Lowered to:
brfalse → else  
then block  
br → end  
else block  
end:

4.2 EVALUATE
------------
If numeric:
- Lower to switch

Else:
- Lower to if‑chain

4.3 PERFORM THRU
----------------
Lowered to:
- Label for start
- Label for end
- br to start
- Fall‑through until end
- br to return label

4.4 PERFORM UNTIL
-----------------
Lowered to:
loop_start:
    if (condition) br loop_end
    body
    br loop_start
loop_end:

4.5 PERFORM VARYING
-------------------
Lowered to:
init
loop_start:
    if (condition) br loop_end
    body
    increment
    br loop_start
loop_end:

4.6 GO TO
---------
Lowered to:
br target_label

------------------------------------------------------------
SECTION 5 — EXCEPTION REGION EMISSION
------------------------------------------------------------

5.1 ON EXCEPTION
----------------
Lowered to:
try {
    operation
} catch (Exception ex) {
    ctx.ExceptionState = ...
    br handler_label
}

5.2 INVALID KEY / AT END
------------------------
Lowered to:
call FileManager.Read
ldloc status
switch {
    case INVALID_KEY → handler
    case AT_END → handler
}

5.3 SIZE ERROR
--------------
Lowered to:
call NumericEngine
if (!success) br size_error_handler

------------------------------------------------------------
SECTION 6 — DEBUGGER SEQUENCE POINTS
------------------------------------------------------------

6.1 Sequence point placement
----------------------------
Sequence points emitted for:
- Paragraph entry
- Section entry
- Statement start
- PERFORM entry/exit
- Exception handler entry
- Branch targets

6.2 Mapping to original source
------------------------------
Sequence points map to:
- Original source file
- Original line/column
- COPY/REPLACE mapping preserved

6.3 Hidden sequence points
--------------------------
Used for:
- Compiler‑generated scaffolding
- Loop boundaries
- Exception region boundaries

------------------------------------------------------------
SECTION 7 — INTEGRATION WITH EXECUTIONCONTEXT
------------------------------------------------------------

7.1 Passing ExecutionContext
----------------------------
All generated methods have signature:
void MethodName(ExecutionContext ctx)

7.2 Storage access
------------------
DISPLAY:
- ctx.Storage.GetString(offset, length)

COMP:
- ctx.Storage.GetBinary(offset, width)

COMP‑3:
- ctx.Storage.GetPackedDecimal(offset, digits, scale)

7.3 File operations
-------------------
Lowered to:
ctx.FileManager.Read(...)
ctx.FileManager.Write(...)
ctx.FileManager.Start(...)

7.4 JSON/XML operations
-----------------------
Lowered to:
ctx.JsonEngine.Parse(...)
ctx.XmlEngine.Generate(...)

------------------------------------------------------------
SECTION 8 — CIL EMISSION RULES
------------------------------------------------------------

8.1 Verifiable IL
-----------------
CobolSharp guarantees:
- No unverifiable opcodes
- No unbalanced stacks
- No overlapping exception regions
- No unverifiable branching

8.2 AOT/WASM compatibility
--------------------------
Backend avoids:
- Reflection.Emit
- DynamicMethod
- Unmanaged pointers
- Runtime code generation

8.3 Optimization‑friendly IL
----------------------------
Backend emits:
- Structured loops
- Structured branches
- Minimal stack depth
- Minimal temporary locals

------------------------------------------------------------
SECTION 9 — FILE I/O LOWERING
------------------------------------------------------------

9.1 READ
--------
Lowered to:
call FileManager.Read
check status
branch to handlers

9.2 WRITE
---------
call FileManager.Write

9.3 REWRITE
-----------
call FileManager.Rewrite

9.4 DELETE
----------
call FileManager.Delete

9.5 START
---------
call FileManager.Position

------------------------------------------------------------
SECTION 10 — JSON/XML LOWERING
------------------------------------------------------------

10.1 JSON PARSE
---------------
call JsonEngine.Parse

10.2 JSON GENERATE
------------------
call JsonEngine.Generate

10.3 XML PARSE
--------------
call XmlEngine.Parse

10.4 XML GENERATE
-----------------
call XmlEngine.Generate

------------------------------------------------------------
SECTION 11 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

11.1 GO TO into middle of PERFORM
---------------------------------
Allowed; backend preserves block boundaries.

11.2 EXIT PROGRAM inside nested PERFORM
---------------------------------------
Backend emits:
- PERFORM stack unwind
- Return from entry method

11.3 REDEFINES overlapping fields
---------------------------------
Backend emits:
- Shared FieldOffset
- Accessors respect raw bytes

11.4 OCCURS DEPENDING ON
-------------------------
Backend emits:
- Max array size
- Logical length checked at runtime

11.5 Packed decimal overflow
----------------------------
Backend emits:
- NumericEngine call
- SIZE ERROR branch

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp CIL Backend & Code Generation Architecture:
- Emits verifiable, structured, debugger‑friendly IL
- Implements full COBOL semantics for control flow, storage, exceptions, and file I/O
- Integrates tightly with ExecutionContext and runtime engines
- Preserves paragraph/section structure and source mapping
- Ensures correctness across CoreCLR, AOT, and WASM
- Forms the final stage of the CobolSharp compilation pipeline
