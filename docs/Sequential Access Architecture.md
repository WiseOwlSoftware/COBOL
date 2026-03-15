CobolSharp COBOL File I/O, Indexed/Relative/Sequential Access Architecture (CIL‑Only)
====================================================================================

Purpose
-------
Define the authoritative architecture for:
- COBOL file organizations (SEQUENTIAL, RELATIVE, INDEXED)
- File control blocks (FCBs)
- Record buffers and key buffers
- OPEN/CLOSE/READ/WRITE/REWRITE/DELETE/START semantics
- File status codes
- INVALID KEY / AT END / ON EXCEPTION routing
- Locking and concurrency rules
- Integration with ExecutionContext and FileManager
- CIL‑friendly lowering
- AOT/WASM‑safe file operations

This document governs how CobolSharp implements COBOL file I/O on .NET.

------------------------------------------------------------
SECTION 1 — FILE ORGANIZATION OVERVIEW
------------------------------------------------------------

CobolSharp supports all COBOL file organizations:

1. SEQUENTIAL  
2. RELATIVE  
3. INDEXED  

Each file is represented by:
- A FileControlBlock (FCB)
- A record buffer
- A key buffer (indexed files)
- A file status variable (optional)
- A runtime handle managed by FileManager

------------------------------------------------------------
SECTION 2 — FILE CONTROL BLOCK (FCB)
------------------------------------------------------------

2.1 FCB structure
-----------------
Each FCB contains:
- File name
- Organization (SEQ/REL/IDX)
- Access mode (INPUT/OUTPUT/I-O/EXTEND)
- Record length
- Key definitions (indexed)
- Current record position
- File status
- Runtime handle

2.2 FCB lifetime
----------------
- Created at program start
- Bound to ExecutionContext
- Destroyed at program termination

------------------------------------------------------------
SECTION 3 — RECORD BUFFERS
------------------------------------------------------------

3.1 Record buffer
-----------------
Each FD defines:
- A record description
- Backed by a StorageBlock
- Used for READ/WRITE/REWRITE

3.2 Key buffer (indexed)
------------------------
Contains:
- Primary key
- Alternate keys (if defined)
- Used for START, READ KEY, WRITE, REWRITE, DELETE

3.3 Buffer semantics
--------------------
READ:
- Populates record buffer

WRITE:
- Writes record buffer to file

REWRITE:
- Writes record buffer to current record position

DELETE:
- Deletes current record

------------------------------------------------------------
SECTION 4 — OPEN/CLOSE SEMANTICS
------------------------------------------------------------

4.1 OPEN INPUT
--------------
- File must exist
- Read‑only
- Position at first record

4.2 OPEN OUTPUT
---------------
- Creates new file
- Overwrites existing file
- Position at start

4.3 OPEN I-O
------------
- Read/write
- File must exist

4.4 OPEN EXTEND
---------------
- Appends to end of file
- Creates file if missing

4.5 CLOSE
---------
- Flushes buffers
- Releases locks
- Updates file status

------------------------------------------------------------
SECTION 5 — READ SEMANTICS
------------------------------------------------------------

5.1 READ NEXT (sequential)
--------------------------
- Reads next record
- AT END if EOF

5.2 READ PREVIOUS (optional)
----------------------------
- Reads previous record
- AT END if BOF

5.3 READ KEY (indexed)
----------------------
READ record KEY IS key-name
- Uses key buffer
- INVALID KEY if not found

5.4 READ RELATIVE
-----------------
READ record RELATIVE KEY IS rk
- INVALID KEY if rk invalid
- AT END if beyond last record

------------------------------------------------------------
SECTION 6 — WRITE / REWRITE / DELETE
------------------------------------------------------------

6.1 WRITE
---------
Sequential:
- Appends record

Relative:
- Writes to relative record number
- INVALID KEY if out of range

Indexed:
- Inserts new record
- INVALID KEY if duplicate key

6.2 REWRITE
-----------
- Requires prior successful READ
- Rewrites current record
- INVALID KEY if key changed illegally

6.3 DELETE
----------
- Requires prior successful READ
- Deletes current record
- INVALID KEY if no current record

------------------------------------------------------------
SECTION 7 — START SEMANTICS (INDEXED)
------------------------------------------------------------

7.1 START KEY = value
---------------------
Positions file cursor:
- EQUAL
- GREATER
- GREATER OR EQUAL
- LESS
- LESS OR EQUAL

7.2 START failure
-----------------
INVALID KEY if:
- No matching record
- Key out of range

------------------------------------------------------------
SECTION 8 — FILE STATUS CODES
------------------------------------------------------------

CobolSharp implements full ANSI file status codes:

"00" → Success  
"02" → Duplicate key  
"04" → Short record  
"05" → OPEN in progress  
"07" → CLOSE in progress  
"10" → End of file  
"14" → No current record  
"21" → Key not found  
"22" → Invalid key  
"23" → Record locked  
"30" → Permanent error  
"34" → Boundary violation  
"35" → File not found  
"37" → Permission denied  
"90" → Runtime error  
"91" → Lock conflict  
"92" → Logic error  
"93" → File integrity error  

------------------------------------------------------------
SECTION 9 — EXCEPTION ROUTING
------------------------------------------------------------

9.1 INVALID KEY
---------------
Triggered by:
- Failed READ KEY
- Failed REWRITE
- Failed DELETE
- Duplicate key on WRITE
- Invalid relative key

9.2 AT END
----------
Triggered by:
- READ NEXT at EOF
- READ PREVIOUS at BOF
- READ on empty file

9.3 ON EXCEPTION
----------------
Triggered by:
- Runtime errors
- File corruption
- Permission issues
- Lock conflicts

9.4 Declaratives
----------------
If no local handler:
- USE AFTER EXCEPTION
- USE AFTER ERROR
- USE AFTER STANDARD EXCEPTION

------------------------------------------------------------
SECTION 10 — LOCKING & CONCURRENCY
------------------------------------------------------------

10.1 Locking model
------------------
CobolSharp uses:
- Advisory locks
- Per‑record locking for indexed files
- File‑level locking for sequential files

10.2 Lock conflicts
-------------------
File status "23" or "91"

10.3 Deadlock avoidance
-----------------------
CobolSharp:
- Times out long waits
- Returns lock conflict status

------------------------------------------------------------
SECTION 11 — CIL LOWERING RULES
------------------------------------------------------------

11.1 READ lowering
------------------
call FileManager.Read(ctx, fcb)
check status
branch to handlers

11.2 WRITE lowering
-------------------
call FileManager.Write(ctx, fcb)

11.3 REWRITE lowering
---------------------
call FileManager.Rewrite(ctx, fcb)

11.4 DELETE lowering
--------------------
call FileManager.Delete(ctx, fcb)

11.5 START lowering
-------------------
call FileManager.Start(ctx, fcb, key, mode)

------------------------------------------------------------
SECTION 12 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- File name
- Organization
- Current record number
- Key buffer contents
- Record buffer contents
- File status
- Lock state
- ExceptionState

Sequence points emitted for:
- OPEN
- CLOSE
- READ
- WRITE
- REWRITE
- DELETE
- START

------------------------------------------------------------
SECTION 13 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

13.1 REWRITE without READ
-------------------------
INVALID KEY

13.2 DELETE without READ
------------------------
INVALID KEY

13.3 WRITE with missing key
---------------------------
INVALID KEY

13.4 START on sequential file
-----------------------------
Illegal; compile‑time error

13.5 READ NEXT after START GREATER
----------------------------------
Reads next record after positioned key

13.6 Relative file gaps
-----------------------
Reading a missing record returns:
- INVALID KEY

13.7 Indexed file with duplicate alternate key
----------------------------------------------
Allowed unless UNIQUE specified

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp File I/O Architecture:
- Implements full COBOL SEQUENTIAL, RELATIVE, and INDEXED semantics
- Provides deterministic, debuggable file operations
- Integrates tightly with ExecutionContext and FileManager
- Supports full file status, INVALID KEY, AT END, and ON EXCEPTION routing
- Generates clean, verifiable, AOT/WASM‑safe CIL
- Ensures correctness across CoreCLR, AOT, and WASM
