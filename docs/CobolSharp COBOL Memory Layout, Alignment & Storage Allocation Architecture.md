CobolSharp COBOL Memory Layout, Alignment & Storage Allocation Architecture (CIL‑Only)
=====================================================================================

Purpose
-------
Define the authoritative architecture for:
- COBOL memory layout rules
- Explicit offsets for all data items
- Alignment rules for DISPLAY, COMP, COMP‑3, COMP‑5
- Group item layout
- REDEFINES overlay semantics
- OCCURS and OCCURS DEPENDING ON layout
- STRING/NATIONAL storage
- File record buffer layout
- LINKAGE SECTION binding
- CIL‑friendly explicit‑layout types
- AOT/WASM‑safe memory access

This document governs how CobolSharp lays out COBOL data in memory and exposes it to generated CIL.

------------------------------------------------------------
SECTION 1 — MEMORY MODEL OVERVIEW
------------------------------------------------------------

CobolSharp uses a **byte‑addressable, explicit‑layout memory model**:
- Every data item has a fixed offset
- Every group item defines a contiguous region
- REDEFINES overlays share the same offset
- OCCURS arrays are contiguous blocks
- Packed decimal (COMP‑3) uses BCD encoding
- Binary fields (COMP, COMP‑5) use fixed‑width integers
- DISPLAY fields use raw bytes (ASCII or UTF‑16 depending on PIC)

Memory is represented by:
- StorageBlock (byte[])
- Explicit offset tables
- Field metadata (PIC, USAGE, SIGN, OCCURS, REDEFINES)

------------------------------------------------------------
SECTION 2 — OFFSET ASSIGNMENT RULES
------------------------------------------------------------

2.1 Sequential allocation
-------------------------
Offsets assigned in declaration order:
offset = previous_offset + previous_size

2.2 Group items
---------------
Group size = sum of children sizes  
Group offset = first child offset

2.3 REDEFINES
-------------
REDEFINES item:
- Shares offset with target
- Size = size of redefining item
- Does NOT affect offset of subsequent items

2.4 OCCURS
----------
OCCURS n TIMES:
- Allocates n * element_size bytes
- Elements contiguous

2.5 OCCURS DEPENDING ON
------------------------
Physical size = max_occurs * element_size  
Logical size = runtime value  
Memory always reserves max size.

2.6 ALIGNMENT
-------------
CobolSharp uses **no implicit alignment**:
- All items byte‑packed
- No padding between fields
- Ensures deterministic offsets across platforms

------------------------------------------------------------
SECTION 3 — DATA TYPE LAYOUT RULES
------------------------------------------------------------

3.1 DISPLAY (PIC X)
-------------------
- 1 byte per character (ASCII)
- NATIONAL uses UTF‑16 (2 bytes per char)

3.2 Numeric DISPLAY (PIC 9)
---------------------------
- 1 byte per digit
- Optional sign stored as trailing or leading byte

3.3 COMP (binary)
-----------------
- 2, 4, or 8 bytes depending on PIC
- Big‑endian or little‑endian?  
  CobolSharp uses **little‑endian** (native .NET)

3.4 COMP‑5 (native binary)
--------------------------
- Same as COMP but:
- No truncation rules
- No decimal scaling

3.5 COMP‑3 (packed decimal)
---------------------------
- Two digits per byte
- Last nibble contains sign
- Size = ceil(digits / 2) bytes

3.6 SIGN rules
--------------
SIGN LEADING/TRAILING:
- Stored as ASCII sign for DISPLAY
- Stored as nibble for COMP‑3
- Stored as two’s complement for COMP/COMP‑5

------------------------------------------------------------
SECTION 4 — GROUP ITEM SEMANTICS
------------------------------------------------------------

4.1 Group items are untyped
---------------------------
A group item:
- Has no PIC
- Is a raw byte region
- Children define interpretation

4.2 Group MOVE
--------------
MOVE group‑A TO group‑B:
- Byte‑copy of entire region

4.3 Group REDEFINES
--------------------
Overlay semantics:
- All children share same bytes
- Debugger shows all interpretations

------------------------------------------------------------
SECTION 5 — STRING & NATIONAL STORAGE
------------------------------------------------------------

5.1 STRING (PIC X)
------------------
- Stored as raw bytes
- No null terminator
- Padding with spaces

5.2 NATIONAL (PIC N)
--------------------
- UTF‑16 encoding
- 2 bytes per character
- Padding with U+0020

5.3 MIXED groups
----------------
Illegal unless explicitly converted.

------------------------------------------------------------
SECTION 6 — FILE RECORD BUFFER LAYOUT
------------------------------------------------------------

6.1 FD record
-------------
FD defines:
- A record group item
- Backed by its own StorageBlock

6.2 READ/WRITE semantics
------------------------
READ:
- Populates record buffer bytes

WRITE:
- Writes record buffer bytes to file

6.3 Variable‑length records
---------------------------
Supported via:
- RECORD VARYING
- Runtime length field

------------------------------------------------------------
SECTION 7 — LINKAGE SECTION BINDING
------------------------------------------------------------

7.1 BY REFERENCE
----------------
LINKAGE item points to caller’s StorageBlock:
- Offset = caller offset
- No copy performed

7.2 BY CONTENT
--------------
LINKAGE item receives:
- A copy of caller’s bytes
- Stored in callee’s LOCAL‑STORAGE

7.3 BY VALUE (OO only)
----------------------
Primitive value passed directly.

------------------------------------------------------------
SECTION 8 — CIL LOWERING RULES
------------------------------------------------------------

8.1 StorageBlock access
-----------------------
Generated IL uses:
- ctx.Storage.GetBytes
- ctx.Storage.SetBytes
- ctx.Storage.GetPackedDecimal
- ctx.Storage.SetPackedDecimal
- ctx.Storage.GetBinary
- ctx.Storage.SetBinary

8.2 Field metadata
------------------
Compiler emits:
- Offset
- Length
- PIC info
- USAGE info
- OCCURS info
- REDEFINES target

8.3 No unsafe code
------------------
CobolSharp uses:
- Pure managed byte arrays
- No pointers
- No stackalloc
- AOT/WASM‑safe operations

------------------------------------------------------------
SECTION 9 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Field name
- PIC clause
- Offset
- Length
- Raw bytes
- Decoded value
- REDEFINES overlays
- OCCURS elements
- NATIONAL strings

------------------------------------------------------------
SECTION 10 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

10.1 REDEFINES of smaller/larger size
-------------------------------------
Allowed; larger redefining item may read beyond logical size.

10.2 OCCURS DEPENDING ON < 1
----------------------------
Logical length = 0  
Physical memory unchanged.

10.3 COMP‑3 odd digit count
---------------------------
High nibble padded with zero.

10.4 SIGN TRAILING for COMP‑3
-----------------------------
Stored in low nibble of last byte.

10.5 NATIONAL inside REDEFINES
------------------------------
Allowed; debugger shows raw bytes.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Memory Layout Architecture:
- Implements deterministic, byte‑accurate COBOL storage
- Supports DISPLAY, COMP, COMP‑3, COMP‑5, NATIONAL, OCCURS, REDEFINES
- Uses explicit offsets with no padding
- Provides AOT/WASM‑safe memory access
- Integrates tightly with ExecutionContext and runtime engines
- Generates clean, verifiable, debugger‑friendly CIL
