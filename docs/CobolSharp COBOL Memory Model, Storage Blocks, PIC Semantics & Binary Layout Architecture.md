CobolSharp COBOL Memory Model, Storage Blocks, PIC Semantics & Binary Layout Architecture (CIL‑Only)
===================================================================================================

Purpose
-------
Define the authoritative architecture for:
- COBOL memory model
- Storage blocks (WORKING‑STORAGE, LOCAL‑STORAGE, LINKAGE)
- PIC/USAGE semantics
- Binary layout rules
- Alignment and padding
- Group items and redefines
- OCCURS tables
- COMP, COMP‑3, COMP‑5 encoding
- NATIONAL vs DISPLAY storage
- Substringing and reference modification
- AOT/WASM‑safe memory operations

This document governs how CobolSharp lays out, stores, and manipulates COBOL data in memory.

------------------------------------------------------------
SECTION 1 — MEMORY MODEL OVERVIEW
------------------------------------------------------------

CobolSharp uses a **byte‑addressable memory model**:
- Each COBOL data item resides in a StorageBlock
- StorageBlock is a contiguous byte[] buffer
- Compiler assigns offsets for each field
- PIC/USAGE determines encoding
- Group items are structural overlays
- REDEFINES overlays bytes without copying

StorageBlocks exist for:
- WORKING‑STORAGE
- LOCAL‑STORAGE
- LINKAGE SECTION
- FD record buffers
- JSON/XML temporary buffers

------------------------------------------------------------
SECTION 2 — STORAGEBLOCK STRUCTURE
------------------------------------------------------------

StorageBlock contains:
- byte[] Buffer
- FieldOffset[] table
- FieldMetadata[] table
- OCCURS metadata
- REDEFINES metadata

Compiler generates:
- Offset for each field
- Length in bytes
- Encoding rules
- Alignment (if any)

------------------------------------------------------------
SECTION 3 — PIC/USAGE SEMANTICS
------------------------------------------------------------

3.1 DISPLAY (PIC X / PIC 9)
---------------------------
- ASCII bytes
- 1 byte per character
- Numeric DISPLAY stored as ASCII digits
- Sign stored as separate byte if SIGN IS SEPARATE

3.2 NATIONAL (PIC N)
--------------------
- UTF‑16
- 2 bytes per character
- No surrogate splitting
- Length = characters * 2

3.3 COMP (binary)
-----------------
- Big COBOL integer
- Stored as:
  - 2 bytes (short)
  - 4 bytes (int)
  - 8 bytes (long)
- Endianness: little‑endian (for .NET)

3.4 COMP‑5 (native binary)
--------------------------
- Same as COMP
- No truncation rules
- No scaling

3.5 COMP‑3 (packed decimal)
---------------------------
- 2 digits per byte
- Last nibble = sign (C/F positive, D negative)
- Odd digits padded with leading zero

3.6 FLOATING‑POINT (COMP‑1/COMP‑2)
----------------------------------
Not supported.

------------------------------------------------------------
SECTION 4 — GROUP ITEMS
------------------------------------------------------------

4.1 Group item rules
--------------------
Group items:
- Do not store data
- Are overlays of child fields
- Length = sum of children
- PIC clause ignored on group items

4.2 Alignment
-------------
No alignment:
- All fields packed tightly
- No padding unless explicitly defined

4.3 Nested groups
-----------------
Offsets accumulate recursively.

------------------------------------------------------------
SECTION 5 — REDEFINES
------------------------------------------------------------

5.1 REDEFINES semantics
-----------------------
REDEFINES overlays:
- Same offset
- Same length (or shorter)
- No copying

5.2 Type independence
---------------------
Redefined fields may:
- Have different PIC
- Have different USAGE
- Be numeric or alphanumeric

5.3 REDEFINES and OCCURS
------------------------
Allowed:
- REDEFINES may overlay table
- Table may REDEFINE scalar

------------------------------------------------------------
SECTION 6 — OCCURS TABLES
------------------------------------------------------------

6.1 Fixed OCCURS
----------------
OCCURS n TIMES:
- n * elementLength bytes
- Contiguous layout

6.2 OCCURS DEPENDING ON
------------------------
Supported:
- Maximum size allocated
- Actual size stored in runtime variable
- Bounds checked at runtime

6.3 Indexing
------------
Index = 1‑based  
Offset = base + (index‑1) * elementLength

------------------------------------------------------------
SECTION 7 — SUBSTRINGING & REFERENCE MODIFICATION
------------------------------------------------------------

7.1 Syntax
----------
field(start:length)

7.2 Offset calculation
----------------------
DISPLAY:
offset = base + (start‑1)

NATIONAL:
offset = base + (start‑1) * 2

7.3 Length calculation
----------------------
DISPLAY:
bytes = length

NATIONAL:
bytes = length * 2

7.4 Bounds checking
-------------------
Out‑of‑range:
- Runtime error
- ON EXCEPTION if present

------------------------------------------------------------
SECTION 8 — NUMERIC ENCODING RULES
------------------------------------------------------------

8.1 DISPLAY numeric
-------------------
"123" stored as:
31 32 33 (ASCII)

8.2 COMP
--------
PIC S9(4) COMP:
- 2 bytes (short)
PIC S9(9) COMP:
- 4 bytes (int)
PIC S9(18) COMP:
- 8 bytes (long)

8.3 COMP‑5
----------
Same as COMP but:
- No truncation
- No scaling

8.4 COMP‑3
----------
PIC S9(5) COMP‑3:
Digits: 5  
Bytes: ceil((5+1)/2) = 3  
Layout:
- Byte1: d1 d2
- Byte2: d3 d4
- Byte3: d5 sign

------------------------------------------------------------
SECTION 9 — STRING ENCODING RULES
------------------------------------------------------------

9.1 DISPLAY
-----------
ASCII only:
- Non‑ASCII → runtime error

9.2 NATIONAL
------------
UTF‑16:
- Surrogate pairs allowed
- Length in characters, not bytes

9.3 Mixed operations
--------------------
DISPLAY → NATIONAL:
- ASCII → UTF‑16

NATIONAL → DISPLAY:
- UTF‑16 → ASCII (error if non‑ASCII)

------------------------------------------------------------
SECTION 10 — MEMORY OPERATIONS
------------------------------------------------------------

10.1 MOVE
---------
MOVE source TO target:
- Type‑aware conversion
- Padding/truncation rules
- Sign handling
- Scaling for numeric

10.2 MOVE CORRESPONDING
-----------------------
Matches by name:
- Case‑insensitive
- Moves only matching fields

10.3 INITIALIZE
---------------
INITIALIZE group:
- Alphanumeric → spaces
- Numeric → zeros
- NATIONAL → UTF‑16 spaces

10.4 INSPECT
------------
Operates on:
- DISPLAY bytes
- NATIONAL UTF‑16 units

------------------------------------------------------------
SECTION 11 — AOT/WASM‑SAFE MEMORY MODEL
------------------------------------------------------------

11.1 No unsafe code
-------------------
- No pointers
- No stackalloc
- No unmanaged memory

11.2 Pure managed operations
----------------------------
- Span<byte>
- Span<char>
- Array slicing

11.3 Deterministic layout
-------------------------
- Same across all platforms
- No architecture‑dependent alignment

------------------------------------------------------------
SECTION 12 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Field name
- PIC/USAGE
- Offset
- Length
- Raw bytes
- Decoded value
- REDEFINES overlays
- OCCURS tables

------------------------------------------------------------
SECTION 13 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

13.1 COMP‑3 invalid sign nibble
-------------------------------
SIZE ERROR.

13.2 NATIONAL odd byte count
----------------------------
Runtime error.

13.3 REDEFINES shorter than original
-------------------------------------
Allowed; unused bytes ignored.

13.4 OCCURS DEPENDING ON < 1
----------------------------
Runtime error.

13.5 MOVE to smaller field
--------------------------
Truncation; SIZE ERROR if numeric overflow.

13.6 Substring beyond field
---------------------------
Runtime error.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Memory Model:
- Implements full COBOL PIC/USAGE semantics
- Provides deterministic binary layout for all data types
- Supports REDEFINES, OCCURS, NATIONAL, COMP, COMP‑3, COMP‑5
- Ensures safe, verifiable, AOT/WASM‑compatible memory operations
- Integrates tightly with ExecutionContext and debugger visualization
