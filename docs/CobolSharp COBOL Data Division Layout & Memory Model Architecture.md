CobolSharp COBOL Data Division Layout & Memory Model Architecture (CIL‑Only)
===========================================================================

Purpose
-------
Define the authoritative rules for:
- Data Division layout
- Storage allocation
- Field offsets
- REDEFINES overlays
- OCCURS and OCCURS DEPENDING ON
- Alignment and padding
- Numeric storage formats
- Record layout generation
- CIL‑friendly explicit layout classes
- Runtime memory model

This document governs how CobolSharp represents COBOL data in memory and how it maps to .NET types.

------------------------------------------------------------
SECTION 1 — MEMORY MODEL OVERVIEW
------------------------------------------------------------

CobolSharp uses a **byte‑accurate, deterministic memory model** that mirrors traditional COBOL runtimes while mapping cleanly to .NET CIL.

Key principles:
- Every COBOL data item has a fixed offset within its containing record.
- Group items are contiguous byte regions.
- REDEFINES items share the same byte region.
- OCCURS items are repeated contiguous regions.
- OCCURS DEPENDING ON uses max allocation + logical length.
- Packed decimal and binary formats follow COBOL rules exactly.
- All layouts are represented as **explicit‑layout .NET classes**.

------------------------------------------------------------
SECTION 2 — STORAGE BLOCKS
------------------------------------------------------------

CobolSharp uses **StorageBlocks** to represent memory regions.

Types of StorageBlocks:
- WORKING‑STORAGE block
- LOCAL‑STORAGE block
- LINKAGE block
- FILE record blocks
- Object instance blocks (for OO classes)

Each StorageBlock:
- Is a contiguous byte array
- Has a fixed size determined at compile time
- Contains metadata describing fields and offsets
- Is wrapped by generated .NET classes for type‑safe access

------------------------------------------------------------
SECTION 3 — FIELD LAYOUT RULES
------------------------------------------------------------

3.1 Offsets
-----------
Offsets are assigned:
- Top‑down
- Left‑to‑right
- Without gaps unless alignment rules require

3.2 Alignment
-------------
COBOL traditionally does **not** require alignment.

CobolSharp rules:
- DISPLAY items: no alignment
- COMP‑3: no alignment
- COMP: align to natural boundary (optional, configurable)
- COMP‑5: align to natural boundary (recommended)
- RECORD layout always deterministic regardless of alignment settings

3.3 Padding
-----------
DISPLAY items:
- Space‑padded
- Truncated on overflow

Numeric items:
- Zero‑padded
- Sign stored per USAGE rules

------------------------------------------------------------
SECTION 4 — GROUP ITEMS
------------------------------------------------------------

Group items:
- Do not allocate storage themselves
- Represent the combined storage of their children
- Have size = sum of child sizes
- Are always contiguous

Example:
01 CUSTOMER.
   05 ID        PIC 9(5).        (5 bytes)
   05 NAME      PIC X(30).       (30 bytes)
   05 BALANCE   PIC S9(7)V99.    (10 bytes)

Total size = 45 bytes.

------------------------------------------------------------
SECTION 5 — REDEFINES
------------------------------------------------------------

REDEFINES rules:
- Redefined item shares the same offset as original
- No additional storage allocated
- All children of both items map to the same byte region
- Assignments update all views

Example:
01 A PIC X(10).
01 B REDEFINES A PIC 9(10).

A and B share the same 10 bytes.

Edge cases:
- REDEFINES of larger over smaller allowed
- REDEFINES of OCCURS allowed
- REDEFINES of COMP‑3 over DISPLAY allowed

------------------------------------------------------------
SECTION 6 — OCCURS
------------------------------------------------------------

6.1 Fixed OCCURS
----------------
OCCURS n TIMES:
- Allocate n * elementSize bytes
- Elements stored contiguously
- Subscripts map to offset = base + (index‑1) * elementSize

6.2 Nested OCCURS
-----------------
Nested OCCURS produce multidimensional arrays:
- Row‑major layout
- Offset = base + (i * innerSize) + j * elementSize

6.3 OCCURS DEPENDING ON
------------------------
Rules:
- Allocate max size at compile time
- Logical length determined at runtime
- DEPENDING ON variable must be numeric
- Out‑of‑range values clamp to legal range
- CIL lowering uses List<T> wrappers for logical access

------------------------------------------------------------
SECTION 7 — NUMERIC STORAGE FORMATS
------------------------------------------------------------

7.1 DISPLAY numeric
-------------------
Stored as ASCII/Unicode characters:
- Right‑justified
- Zero‑padded
- Sign stored as trailing or leading character depending on SIGN clause

7.2 COMP (binary)
-----------------
CobolSharp uses fixed mapping:
- PIC 9(1)–9(4): Int16
- PIC 9(5)–9(9): Int32
- PIC 9(10)–9(18): Int64

Stored in little‑endian format.

7.3 COMP‑5 (native binary)
--------------------------
Same as COMP but:
- No truncation on assignment
- Always uses native integer width

7.4 COMP‑3 (packed decimal)
---------------------------
Rules:
- Two digits per byte
- Last nibble is sign
- Odd number of digits padded with leading zero
- Sign nibble: C/F = positive, D = negative

Example:
PIC S9(5)V99 COMP‑3 → 4 bytes

------------------------------------------------------------
SECTION 8 — STRING STORAGE
------------------------------------------------------------

8.1 Alphanumeric (PIC X)
------------------------
- Stored as bytes
- Space‑padded
- Truncated on overflow

8.2 National (PIC N)
--------------------
- Stored as UTF‑16 code units
- Two bytes per character
- Space‑padded

------------------------------------------------------------
SECTION 9 — RECORD LAYOUT GENERATION
------------------------------------------------------------

CobolSharp generates explicit‑layout .NET classes:

[StructLayout(LayoutKind.Explicit)]
public class CUSTOMER_RECORD {
    [FieldOffset(0)]  public int ID;
    [FieldOffset(4)]  public string NAME;
    [FieldOffset(34)] public decimal BALANCE;
}

Rules:
- Every field has a FieldOffset
- Group items become nested classes
- OCCURS become arrays or List<T>
- REDEFINES share offsets
- Packed decimal fields use byte arrays + accessors

------------------------------------------------------------
SECTION 10 — RUNTIME ACCESSORS
------------------------------------------------------------

CobolSharp.Runtime provides:
- GetNumeric
- SetNumeric
- GetPackedDecimal
- SetPackedDecimal
- GetString
- SetString
- GetBytes
- SetBytes

Accessors:
- Enforce COBOL semantics
- Handle padding/truncation
- Handle sign rules
- Handle decimal alignment
- Handle OCCURS indexing

------------------------------------------------------------
SECTION 11 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

11.1 Zero‑length items
----------------------
- Allowed
- Always empty
- Cannot be numeric

11.2 Overlapping REDEFINES with OCCURS
--------------------------------------
- Allowed
- Accessor logic must respect logical OCCURS length

11.3 COMP‑3 with invalid sign nibble
------------------------------------
- ON EXCEPTION

11.4 Numeric conversion of DISPLAY with spaces
----------------------------------------------
- Treated as zero

11.5 Mixed national/alphanumeric groups
---------------------------------------
- National takes precedence for encoding

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Data Division Layout & Memory Model Architecture:
- Defines byte‑accurate COBOL storage representation
- Supports REDEFINES, OCCURS, and OCCURS DEPENDING ON
- Implements packed decimal, binary, and DISPLAY formats
- Generates explicit‑layout .NET classes for CIL emission
- Ensures deterministic, COBOL‑correct memory behavior
- Integrates cleanly with the runtime, optimizer, and backend
- Forms the foundation for correct execution, debugging, and interop
