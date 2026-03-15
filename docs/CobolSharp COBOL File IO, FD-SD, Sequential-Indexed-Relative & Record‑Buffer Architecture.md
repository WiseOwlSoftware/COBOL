CobolSharp COBOL File I/O, FD/SD, Sequential/Indexed/Relative & Record‑Buffer Architecture (CIL‑Only)
====================================================================================================

Purpose
-------
Define the authoritative architecture for:
- FD and SD file descriptions
- Record buffers and StorageBlocks
- Sequential, Indexed, and Relative file organizations
- ACCESS MODE (SEQUENTIAL, RANDOM, DYNAMIC)
- OPEN, CLOSE, READ, WRITE, REWRITE, DELETE, START
- File status codes
- Key handling (primary, alternate)
- Locking and sharing modes
- Integration with ExecutionContext.FileManager
- AOT/WASM‑safe file I/O
- CIL‑friendly lowering

This document governs how CobolSharp implements COBOL file I/O semantics on .NET.

------------------------------------------------------------
SECTION 1 — FILEMANAGER OVERVIEW
------------------------------------------------------------

ExecutionContext.FileManager provides:
- File open/close
- Sequential read/write
- Indexed read/write/delete
- Relative read/write/delete
- START positioning
- File status population
- Locking and sharing
- Record buffer management

All file operations are:
- Pure managed
- Deterministic
- AOT/WASM‑safe

------------------------------------------------------------
SECTION 2 — FD / SD ARCHITECTURE
------------------------------------------------------------

2.1 FD structure
----------------
FD defines:
- Record layout (group item)
- File organization
- Access mode
- Record length
- Keys (primary, alternate)
- Collation (for indexed files)

Compiler generates:
- RecordBuffer StorageBlock
- Offset table
- Key metadata

2.2 SD structure
----------------
SD used for:
- SORT/MERGE temporary files
- Same structure as FD
- Managed by SortEngine

------------------------------------------------------------
SECTION 3 — FILE ORGANIZATIONS
------------------------------------------------------------

3.1 SEQUENTIAL
--------------
Records stored in order of writing.

Supported operations:
- OPEN INPUT/OUTPUT/I-O/EXTEND
- READ NEXT
- WRITE
- REWRITE (only if I-O)
- DELETE (not allowed)

3.2 INDEXED
-----------
Records stored by primary key.

Supported operations:
- READ NEXT/PREVIOUS
- READ KEY = value
- START KEY >= value
- WRITE
- REWRITE
- DELETE

3.3 RELATIVE
------------
Records stored by relative record number (RRN).

Supported operations:
- READ RRN
- WRITE RRN
- REWRITE RRN
- DELETE RRN

------------------------------------------------------------
SECTION 4 — ACCESS MODES
------------------------------------------------------------

4.1 SEQUENTIAL
--------------
READ NEXT only.

4.2 RANDOM
----------
READ/WRITE by key or RRN only.

4.3 DYNAMIC
-----------
Both sequential and random allowed.

------------------------------------------------------------
SECTION 5 — OPEN/CLOSE SEMANTICS
------------------------------------------------------------

5.1 OPEN INPUT
--------------
- File must exist
- Position at first record

5.2 OPEN OUTPUT
---------------
- Create new file
- Truncate existing file

5.3 OPEN I-O
------------
- File must exist
- Allows READ/WRITE/REWRITE/DELETE

5.4 OPEN EXTEND
---------------
- Append to end of sequential file

5.5 CLOSE
---------
- Flush buffers
- Release locks
- Reset file status

------------------------------------------------------------
SECTION 6 — RECORD BUFFER ARCHITECTURE
------------------------------------------------------------

6.1 RecordBuffer
----------------
Each FD has:
- A StorageBlock for record
- Offset table for fields
- Key metadata

6.2 READ
--------
- FileManager loads record into RecordBuffer
- StorageBlock updated in place

6.3 WRITE
---------
- FileManager writes bytes from RecordBuffer

6.4 REWRITE
-----------
- FileManager overwrites current record

6.5 DELETE
----------
- FileManager marks record deleted (indexed/relative)

------------------------------------------------------------
SECTION 7 — KEY HANDLING
------------------------------------------------------------

7.1 Primary key
---------------
- Must be unique
- Used for indexed access

7.2 Alternate keys
------------------
- May be duplicates
- May be ascending or descending

7.3 Key extraction
------------------
Compiler generates:
- Offset and length for each key
- Comparison logic

7.4 Collation
-------------
DISPLAY keys:
- ASCII or custom collation

NATIONAL keys:
- UTF‑16 code point order

------------------------------------------------------------
SECTION 8 — FILE OPERATIONS
------------------------------------------------------------

8.1 READ
--------
Forms:
- READ file INTO ws
- READ file NEXT
- READ file PREVIOUS
- READ file KEY IS value

On success:
- File status = "00"
- RecordBuffer updated

On failure:
- "10" end of file
- "23" key not found
- "35" file not open
- "92" logic error

8.2 WRITE
---------
- Writes RecordBuffer to file
- For indexed: inserts by key
- For sequential: appends

8.3 REWRITE
-----------
- Overwrites current record
- Requires successful READ prior

8.4 DELETE
----------
- Removes record (indexed/relative)
- Sequential: not allowed

8.5 START
---------
START file KEY >= value.
- Positions cursor
- Next READ NEXT returns first matching record

------------------------------------------------------------
SECTION 9 — LOCKING & SHARING
------------------------------------------------------------

9.1 Lock modes
--------------
- AUTOMATIC
- MANUAL
- EXCLUSIVE

9.2 Sharing modes
-----------------
- SHARE ALL
- SHARE READ
- SHARE NONE

9.3 Lock behavior
-----------------
- Indexed files support record‑level locking
- Sequential files support file‑level locking

------------------------------------------------------------
SECTION 10 — FILE STATUS CODES
------------------------------------------------------------

Common codes:
- "00" success
- "02" duplicate key (alternate)
- "04" boundary violation
- "10" end of file
- "21" key invalid
- "22" duplicate key (primary)
- "23" key not found
- "30" permanent error
- "35" file not found
- "37" permission denied
- "41" already open
- "42" not open
- "43" read not allowed
- "44" write not allowed
- "92" logic error

------------------------------------------------------------
SECTION 11 — CIL LOWERING RULES
------------------------------------------------------------

11.1 OPEN lowering
------------------
Generated IL:
- Call FileManager.Open
- Set file status

11.2 READ lowering
------------------
Generated IL:
- Call FileManager.Read
- Move bytes into RecordBuffer

11.3 WRITE lowering
-------------------
Generated IL:
- Call FileManager.Write

11.4 REWRITE lowering
---------------------
Generated IL:
- Call FileManager.Rewrite

11.5 DELETE lowering
--------------------
Generated IL:
- Call FileManager.Delete

11.6 START lowering
-------------------
Generated IL:
- Call FileManager.Start

------------------------------------------------------------
SECTION 12 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Current record buffer
- Key values
- File status
- Cursor position
- Lock state
- File organization
- Access mode

------------------------------------------------------------
SECTION 13 — AOT/WASM‑SAFE FILE I/O
------------------------------------------------------------

13.1 No unsafe code
-------------------
- No pointers
- No stackalloc

13.2 No dynamic codegen
-----------------------
- Static file operations

13.3 Deterministic behavior
---------------------------
- Same results across platforms

13.4 WASM file system
---------------------
- Uses virtual FS
- Indexed files stored as structured blobs

------------------------------------------------------------
SECTION 14 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

14.1 READ after end of file
---------------------------
File status "10".

14.2 WRITE duplicate primary key
--------------------------------
File status "22".

14.3 REWRITE without prior READ
-------------------------------
File status "92".

14.4 DELETE on sequential file
------------------------------
File status "92".

14.5 START on sequential file
-----------------------------
File status "21".

14.6 READ PREVIOUS on sequential file
-------------------------------------
File status "21".

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp File I/O Architecture:
- Implements full COBOL FD/SD, sequential/indexed/relative semantics
- Provides deterministic record‑buffer and key‑handling behavior
- Supports START, READ NEXT/PREVIOUS, REWRITE, DELETE with correct status codes
- Integrates tightly with ExecutionContext.FileManager
- Generates clean, verifiable, debugger‑friendly CIL
- Ensures correctness across CoreCLR, AOT, and WASM
