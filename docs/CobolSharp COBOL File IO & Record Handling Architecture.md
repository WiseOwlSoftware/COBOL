CobolSharp COBOL File I/O & Record Handling Architecture (CIL‑Only)
==================================================================

Purpose
-------
Define the authoritative architecture for:
- COBOL file semantics
- Record layout and buffering
- Sequential, relative, and indexed file handling
- File status codes
- Key handling
- START, READ, WRITE, REWRITE, DELETE
- OPEN/CLOSE semantics
- Locking and concurrency
- CIL‑friendly lowering
- Integration with CobolSharp.Runtime.FileManager

This document governs how CobolSharp implements COBOL file I/O on .NET.

------------------------------------------------------------
SECTION 1 — FILE ORGANIZATION MODEL
------------------------------------------------------------

CobolSharp supports the three standard COBOL file organizations:

1. Sequential  
2. Relative  
3. Indexed  

Each organization maps to a specific .NET storage model.

1.1 Sequential files
--------------------
Characteristics:
- Records stored in order
- No random access
- READ NEXT only
- WRITE appends
- REWRITE requires prior READ

CobolSharp maps sequential files to:
- .NET FileStream
- Line‑based or fixed‑length record encoding
- Optional JSON/XML record serialization (configurable)

1.2 Relative files
------------------
Characteristics:
- Records addressed by relative record number (RRN)
- Holes allowed
- DELETE marks record as inactive

CobolSharp maps relative files to:
- Structured binary files with fixed‑size slots
- Direct addressing via RRN * recordSize

1.3 Indexed files
-----------------
Characteristics:
- Records stored with primary key
- Optional alternate keys
- Random access via key
- READ NEXT/PREVIOUS supported

CobolSharp maps indexed files to:
- B+‑tree index + data file pair
- .NET implementation of B+‑tree optimized for sequential and random access

------------------------------------------------------------
SECTION 2 — FILE DESCRIPTORS & RECORD BUFFERS
------------------------------------------------------------

2.1 File descriptors
--------------------
Each COBOL file is represented by a FileDescriptor:

Fields:
- Organization (Sequential/Relative/Indexed)
- Access mode (INPUT/OUTPUT/I‑O/EXTEND)
- Record layout metadata
- Key definitions
- File status variable
- Runtime handle to FileManager

2.2 Record buffers
------------------
Each file has:
- Input buffer (for READ)
- Output buffer (for WRITE/REWRITE)
- Optional key buffer (for indexed access)

Record buffers are:
- Explicit‑layout .NET classes
- Backed by StorageBlocks
- Mapped to file records byte‑for‑byte

------------------------------------------------------------
SECTION 3 — OPEN/CLOSE SEMANTICS
------------------------------------------------------------

3.1 OPEN INPUT
--------------
Rules:
- File must exist
- Position at first record
- READ allowed
- WRITE/REWRITE/DELETE not allowed

3.2 OPEN OUTPUT
---------------
Rules:
- Create or truncate file
- Position at beginning
- WRITE allowed
- READ not allowed

3.3 OPEN I‑O
------------
Rules:
- File must exist
- READ/WRITE/REWRITE/DELETE allowed

3.4 OPEN EXTEND
----------------
Rules:
- Create file if missing
- Position at end
- WRITE allowed
- READ not allowed

3.5 CLOSE
---------
Rules:
- Flush buffers
- Release locks
- Release file handles

------------------------------------------------------------
SECTION 4 — READ SEMANTICS
------------------------------------------------------------

4.1 READ NEXT (Sequential)
--------------------------
Rules:
- Read next record
- On EOF → AT END
- On success → populate input buffer

4.2 READ NEXT/PREVIOUS (Indexed)
--------------------------------
Rules:
- Use index order
- PREVIOUS allowed only for indexed files
- AT END on EOF/BOF

4.3 READ KEY
------------
Rules:
- Key must exist
- INVALID KEY on failure
- On success → populate input buffer

4.4 READ RRN (Relative)
-----------------------
Rules:
- RRN must be valid
- Deleted records → INVALID KEY
- On success → populate input buffer

------------------------------------------------------------
SECTION 5 — WRITE/REWRITE/DELETE SEMANTICS
------------------------------------------------------------

5.1 WRITE
---------
Sequential:
- Append record

Relative:
- Write to RRN
- If RRN exists → overwrite
- If RRN beyond file → extend file

Indexed:
- Insert into B+‑tree
- Duplicate key → INVALID KEY

5.2 REWRITE
-----------
Rules:
- Requires successful READ first
- Overwrites current record
- Indexed: key must not change

5.3 DELETE
----------
Relative:
- Mark record as deleted

Indexed:
- Remove from B+‑tree
- Remove from data file

------------------------------------------------------------
SECTION 6 — START SEMANTICS
------------------------------------------------------------

START positions the file cursor for indexed files.

Modes:
- KEY = value
- KEY > value
- KEY >= value
- KEY < value
- KEY <= value

Rules:
- INVALID KEY if no matching record
- On success → next READ NEXT/PREVIOUS uses new position

------------------------------------------------------------
SECTION 7 — FILE STATUS CODES
------------------------------------------------------------

CobolSharp implements full ANSI file status semantics.

Examples:
"00" → Success  
"02" → Duplicate key  
"10" → End of file  
"21" → Key not found  
"22" → Invalid key  
"23" → Record locked  
"30" → Permanent error  
"90" → Runtime error  

File status is:
- Updated after every file operation
- Written into user‑defined FILE STATUS variable

------------------------------------------------------------
SECTION 8 — LOCKING & CONCURRENCY
------------------------------------------------------------

CobolSharp supports:
- Shared locks for READ
- Exclusive locks for WRITE/REWRITE/DELETE
- Optional optimistic concurrency for indexed files

Lock modes:
- AUTOMATIC (default)
- MANUAL (PLANNED)

------------------------------------------------------------
SECTION 9 — CIL LOWERING RULES
------------------------------------------------------------

9.1 READ lowering
-----------------
READ → call FileManager.Read  
READ NEXT → FileManager.ReadNext  
READ PREVIOUS → FileManager.ReadPrevious  
READ KEY → FileManager.ReadByKey  

9.2 WRITE lowering
------------------
WRITE → FileManager.Write  

9.3 REWRITE lowering
--------------------
REWRITE → FileManager.Rewrite  

9.4 DELETE lowering
-------------------
DELETE → FileManager.Delete  

9.5 START lowering
------------------
START → FileManager.Position  

------------------------------------------------------------
SECTION 10 — RUNTIME FILEMANAGER ARCHITECTURE
------------------------------------------------------------

FileManager responsibilities:
- Open/close files
- Maintain file handles
- Manage record buffers
- Perform key lookups
- Maintain B+‑tree indexes
- Handle locking
- Update file status
- Perform serialization/deserialization
- Enforce COBOL semantics

FileManager is:
- Pure managed code
- Cross‑platform
- Deterministic
- AOT/WASM compatible

------------------------------------------------------------
SECTION 11 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

11.1 READ after OPEN OUTPUT
---------------------------
- Illegal
- File status "30"

11.2 REWRITE without prior READ
-------------------------------
- INVALID KEY

11.3 DELETE on sequential file
------------------------------
- Illegal
- File status "30"

11.4 START on sequential file
-----------------------------
- Illegal
- File status "30"

11.5 Indexed file with duplicate key
------------------------------------
- INVALID KEY

11.6 Reading deleted relative record
------------------------------------
- INVALID KEY

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp File I/O & Record Handling Architecture:
- Implements full COBOL file semantics across sequential, relative, and indexed files
- Provides deterministic, byte‑accurate record handling
- Uses explicit‑layout record buffers
- Maps COBOL operations to FileManager runtime calls
- Supports START, READ, WRITE, REWRITE, DELETE, OPEN, CLOSE
- Implements full file status semantics
- Ensures compatibility with .NET CoreCLR, AOT, and WASM
- Is fully aligned with the CIL‑only architecture
