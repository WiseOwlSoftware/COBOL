CobolSharp Memory Model — StorageBlocks, Offsets, REDEFINES, OCCURS & DEPENDING‑ON Architecture (CIL‑Only)
==========================================================================================================

Purpose
-------
Define the authoritative architecture for:
- StorageBlocks (WORKING‑STORAGE, LOCAL‑STORAGE, LINKAGE, FD buffers)
- Field offsets and metadata
- REDEFINES overlays
- OCCURS tables
- OCCURS DEPENDING ON (ODO)
- Alignment and packing rules
- Group items and elementary items
- Substringing and reference modification
- Deterministic memory layout across platforms
- CIL‑friendly lowering

This document governs how CobolSharp implements COBOL’s memory model on .NET.

------------------------------------------------------------
SECTION 1 — STORAGEBLOCK OVERVIEW
------------------------------------------------------------

StorageBlock is the fundamental memory container for COBOL data:
- byte[] Buffer
- FieldOffset[] table
- FieldMetadata[] table
- OCCURS metadata
- REDEFINES metadata

Each program activation has:
- WORKING‑STORAGE block
- LOCAL‑STORAGE block
- LINKAGE block
- FD record buffers
- Temporary blocks (SORT, JSON, XML)

------------------------------------------------------------
SECTION 2 — FIELD METADATA
------------------------------------------------------------

2.1 FieldOffset
---------------
Contains:
- Absolute byte offset
- Length in bytes
- PIC category (DISPLAY, NATIONAL, COMP, COMP‑3, COMP‑5)
- Scale (for numeric)
- Sign rules
- OCCURS index (if applicable)
- REDEFINES parent

2.2 FieldMetadata
-----------------
Contains:
- Name
- Level number
- IsGroup flag
- Children (for groups)
- Redefines target
- Occurs count
- ODO variable reference

------------------------------------------------------------
SECTION 3 — MEMORY LAYOUT RULES
------------------------------------------------------------

3.1 Sequential packing
----------------------
Fields are packed:
- Sequentially
- Without alignment padding
- Except NATIONAL (2‑byte units)

3.2 Group items
---------------
Group item size = sum of children.

3.3 Elementary items
--------------------
DISPLAY:
- 1 byte per character

NATIONAL:
- 2 bytes per UTF‑16 code unit

COMP‑5:
- 2/4/8 bytes depending on PIC

COMP‑3:
- (digits + 1) / 2 bytes

------------------------------------------------------------
SECTION 4 — REDEFINES ARCHITECTURE
------------------------------------------------------------

4.1 Basic rule
--------------
REDEFINES overlays memory:
- Same offset as target
- Same length as target
- No additional storage allocated

4.2 REDEFINES of group
----------------------
Entire group overlayed.

4.3 REDEFINES of elementary
---------------------------
Overlayed at byte level.

4.4 REDEFINES with OCCURS
--------------------------
Allowed:
- Overlay entire table
- No per‑element overlay

4.5 REDEFINES with ODO
-----------------------
Overlay uses maximum size.

------------------------------------------------------------
SECTION 5 — OCCURS ARCHITECTURE
------------------------------------------------------------

5.1 Fixed OCCURS
----------------
OCCURS n TIMES:
- n contiguous copies
- Offset = base + (index * elementSize)

5.2 OCCURS DEPENDING ON (ODO)
-----------------------------
OCCURS n TO m DEPENDING ON var.

Rules:
- Memory allocated for maximum (m)
- Active length determined by var
- var must be numeric DISPLAY or COMP
- var read at runtime

5.3 ODO evaluation
------------------
Evaluated:
- On READ
- On WRITE
- On MOVE CORRESPONDING
- On JSON/XML GENERATE
- On SORT input/output

5.4 ODO bounds
--------------
If var < n → use n  
If var > m → use m

------------------------------------------------------------
SECTION 6 — SUBSTRINGING & REFERENCE MODIFICATION
------------------------------------------------------------

6.1 Syntax
----------
identifier(start:length)

6.2 Lowering
------------
Compiler computes:
- baseOffset + (start‑1)
- length

6.3 Bounds
----------
If substring exceeds field:
- Truncated
- No exception

6.4 NATIONAL substringing
-------------------------
start and length count UTF‑16 code units.

------------------------------------------------------------
SECTION 7 — NUMERIC STORAGE RULES
------------------------------------------------------------

7.1 DISPLAY numeric
-------------------
- ASCII digits
- Optional sign
- Left‑padded or right‑padded depending on PIC

7.2 COMP‑5
----------
- Binary integer
- Little‑endian
- Overflow checked

7.3 COMP‑3
----------
- Packed decimal
- Last nibble = sign
- Invalid nibble → runtime error

7.4 Decimal conversion
----------------------
All numeric operations use Decimal.

------------------------------------------------------------
SECTION 8 — GROUP MOVE & OVERLAP RULES
------------------------------------------------------------

8.1 Group MOVE
--------------
MOVE group1 TO group2:
- Byte‑for‑byte copy
- Uses temporary buffer if overlapping

8.2 Overlapping MOVE
--------------------
MOVE A(1:5) TO A(3:5):
- Extract substring
- Write to target region

8.3 CORRESPONDING
-----------------
Matches by name, not offset.

------------------------------------------------------------
SECTION 9 — FD RECORD BUFFER MODEL
------------------------------------------------------------

9.1 FD buffer
-------------
Each FD has:
- RecordBuffer StorageBlock
- Key offsets
- Key lengths

9.2 READ
--------
- FileManager loads bytes into buffer

9.3 WRITE
---------
- FileManager writes bytes from buffer

9.4 REWRITE
-----------
- Overwrites current record

------------------------------------------------------------
SECTION 10 — LINKAGE SECTION MODEL
------------------------------------------------------------

10.1 BY REFERENCE
-----------------
LINKAGE field overlays caller’s StorageBlock.

10.2 BY CONTENT
---------------
LINKAGE field receives copy.

10.3 BY VALUE
-------------
Stored in local variable, not StorageBlock.

------------------------------------------------------------
SECTION 11 — CIL LOWERING RULES
------------------------------------------------------------

11.1 StorageBlock allocation
----------------------------
new StorageBlock(size)

11.2 Field access
-----------------
ldloc ctx  
ldfld StorageBlock.Buffer  
ldc.i4 offset  
ldc.i4 length  
call StringEngine / NumericEngine

11.3 REDEFINES lowering
-----------------------
Compiler assigns:
- Same offset
- Same length

11.4 OCCURS lowering
--------------------
Compiler generates:
- elementOffset = base + index * elementSize

11.5 ODO lowering
-----------------
Compiler generates:
- activeCount = clamp(var, min, max)

------------------------------------------------------------
SECTION 12 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Raw StorageBlock bytes
- Decoded fields
- REDEFINES overlays
- OCCURS tables
- ODO active length
- Offsets and lengths

------------------------------------------------------------
SECTION 13 — AOT/WASM‑SAFE MEMORY MODEL
------------------------------------------------------------

13.1 No unsafe code
-------------------
- No pointers
- No stackalloc

13.2 Deterministic layout
-------------------------
Same offsets across platforms.

13.3 No GC pinning
------------------
StorageBlocks pure managed.

------------------------------------------------------------
SECTION 14 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

14.1 ODO < minimum
------------------
Use minimum.

14.2 ODO > maximum
------------------
Use maximum.

14.3 REDEFINES of smaller field
-------------------------------
Overlay truncated.

14.4 COMP‑3 odd digits
----------------------
Leading zero inserted.

14.5 NATIONAL truncation
------------------------
Never splits surrogate pair.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Memory Model:
- Implements deterministic, platform‑independent StorageBlocks
- Supports REDEFINES, OCCURS, and ODO with precise offset rules
- Provides safe, verifiable, AOT/WASM‑compatible memory behavior
- Integrates tightly with StringEngine, NumericEngine, and FileManager
- Generates clean, predictable, debugger‑friendly CIL
