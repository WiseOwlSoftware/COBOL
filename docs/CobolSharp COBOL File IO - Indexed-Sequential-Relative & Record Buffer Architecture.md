CobolSharp COBOL File I/O, Indexed/Sequential/Relative & Record Buffer Architecture (CIL‑Only)
==============================================================================================

Purpose
-------
Define the authoritative architecture for:
- COBOL file organizations (SEQUENTIAL, RELATIVE, INDEXED)
- Access modes (INPUT, OUTPUT, I-O, EXTEND)
- Record buffers and FD layout
- File status handling
- OPEN, CLOSE, READ, WRITE, REWRITE, DELETE, START
- Key handling (primary, alternate)
- Locking and concurrency model
- Exception routing (INVALID KEY, AT END, ON EXCEPTION)
- Declarative integration
- CIL‑friendly lowering
- AOT/WASM‑safe file operations

This document governs how CobolSharp implements COBOL file I/O on .NET.

------------------------------------------------------------
SECTION 1 — FILE ORGANIZATION OVERVIEW
------------------------------------------------------------

CobolSharp supports:
- SEQUENTIAL files
- RELATIVE files
- INDEXED files

Each file is represented by:
- A FileControlEntry (from ENVIRONMENT DIVISION)
- A FileDescriptor (from FD)
- A RecordBuffer (StorageBlock)
- A FileHandle (runtime object)

File operations are performed by:
ExecutionContext.FileManager

------------------------------------------------------------
SECTION 2 — FD RECORD BUFFER ARCHITECTURE
------------------------------------------------------------

2.1 FD defines record layout
----------------------------
FD MyFile.
  01 MyRecord.
     05 Field-A PIC X(10).
     05 Field-B PIC 9(5).

Compiler generates:
- A StorageBlock for record buffer
- Offset table for fields
- PIC/USAGE metadata

2.2 Record buffer lifetime
--------------------------
Allocated:
- At OPEN
- Reused for each READ/WRITE

2.3 READ populates buffer
-------------------------
READ MyFile:
- FileManager reads raw bytes
- Populates StorageBlock

2.4 WRITE uses buffer
---------------------
WRITE MyRecord:
- FileManager writes raw bytes from StorageBlock

------------------------------------------------------------
SECTION 3 — FILE STATUS HANDLING
------------------------------------------------------------

3.1 FILE STATUS clause
----------------------
FILE STATUS IS fs.

fs updated after:
- OPEN
- READ
- WRITE
- REWRITE
- DELETE
- START
- CLOSE

3.2 Status categories
---------------------
00 = success  
02 = duplicate key  
10 = end of file  
21 = key not found  
30 = permanent error  
90 = runtime error  

3.3 ExceptionState integration
------------------------------
On error:
- ExceptionState populated
- INVALID KEY / AT END / ON EXCEPTION evaluated
- Declaratives invoked if needed

------------------------------------------------------------
SECTION 4 — OPEN STATEMENT SEMANTICS
------------------------------------------------------------

4.1 OPEN INPUT
--------------
- File must exist
- Read‑only

4.2 OPEN OUTPUT
---------------
- Creates new file
- Overwrites existing file

4.3 OPEN I-O
------------
- Read/write
- File must exist

4.4 OPEN EXTEND
---------------
- Appends to end of file

4.5 Lowering
------------
call ctx.FileManager.Open(fileHandle, mode)

------------------------------------------------------------
SECTION 5 — READ STATEMENT SEMANTICS
------------------------------------------------------------

5.1 READ SEQUENTIAL
-------------------
READ MyFile
    AT END ...
    NOT AT END ...

Lowering:
- FileManager.ReadSequential
- Populate record buffer
- Set file status
- Branch to AT END if needed

5.2 READ RELATIVE
-----------------
READ MyFile RECORD key
    INVALID KEY ...

Lowering:
- FileManager.ReadRelative(key)

5.3 READ INDEXED
----------------
READ MyFile KEY IS key
    INVALID KEY ...

Lowering:
- FileManager.ReadIndexed(key)

5.4 READ NEXT / READ PREVIOUS
-----------------------------
Supported for INDEXED and RELATIVE.

------------------------------------------------------------
SECTION 6 — WRITE STATEMENT SEMANTICS
------------------------------------------------------------

6.1 WRITE
---------
WRITE MyRecord
    INVALID KEY ...

Lowering:
- FileManager.Write(recordBuffer)

6.2 WRITE for INDEXED
---------------------
- Primary key must be unique
- Alternate keys updated automatically

6.3 WRITE for RELATIVE
----------------------
- Writes to next available slot

------------------------------------------------------------
SECTION 7 — REWRITE STATEMENT SEMANTICS
------------------------------------------------------------

7.1 REWRITE
-----------
REWRITE MyRecord
    INVALID KEY ...

Lowering:
- FileManager.Rewrite(recordBuffer)

7.2 Requirements
----------------
- Must follow successful READ
- Key must not change (for INDEXED)

------------------------------------------------------------
SECTION 8 — DELETE STATEMENT SEMANTICS
------------------------------------------------------------

8.1 DELETE
----------
DELETE MyFile RECORD
    INVALID KEY ...

Lowering:
- FileManager.Delete(currentRecord)

8.2 INDEXED delete
------------------
- Removes primary key entry
- Removes alternate key entries

8.3 RELATIVE delete
-------------------
- Marks slot as deleted

------------------------------------------------------------
SECTION 9 — START STATEMENT SEMANTICS
------------------------------------------------------------

9.1 START
---------
START MyFile KEY >= key
    INVALID KEY ...

Lowering:
- FileManager.Start(key, comparison)

9.2 Purpose
-----------
Positions file for:
- READ NEXT
- READ PREVIOUS

------------------------------------------------------------
SECTION 10 — KEY HANDLING
------------------------------------------------------------

10.1 Primary key
----------------
Defined in:
RECORD KEY IS keyField

10.2 Alternate keys
-------------------
ALTERNATE RECORD KEY IS altKey WITH DUPLICATES

10.3 Key comparison
-------------------
- DISPLAY → lexicographic
- COMP/COMP‑3 → numeric
- NATIONAL → UTF‑16 binary

10.4 Key extraction
-------------------
Compiler generates:
- Offset and length for key fields
- Key comparison functions

------------------------------------------------------------
SECTION 11 — LOCKING & CONCURRENCY
------------------------------------------------------------

11.1 Locking model
------------------
CobolSharp uses:
- Cooperative locking
- No OS‑level file locks (AOT/WASM‑safe)

11.2 READ WITH LOCK
-------------------
Supported:
- Locks record until next READ/WRITE/REWRITE

11.3 UNLOCK
-----------
UNLOCK MyFile

------------------------------------------------------------
SECTION 12 — CIL LOWERING RULES
------------------------------------------------------------

12.1 OPEN lowering
------------------
call FileManager.Open

12.2 READ lowering
------------------
try {
    call FileManager.Read
} catch {
    populate ExceptionState
    dispatch declarative
}

12.3 WRITE/REWRITE lowering
---------------------------
call FileManager.Write/Rewrite

12.4 DELETE lowering
--------------------
call FileManager.Delete

12.5 START lowering
-------------------
call FileManager.Start

------------------------------------------------------------
SECTION 13 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- File status
- Current key value
- Record buffer (raw bytes + decoded fields)
- File position
- Lock state
- ExceptionState

------------------------------------------------------------
SECTION 14 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

14.1 READ after end‑of‑file
---------------------------
AT END triggered repeatedly.

14.2 WRITE with duplicate key
-----------------------------
INVALID KEY triggered.

14.3 REWRITE without prior READ
-------------------------------
Runtime error.

14.4 DELETE on nonexistent record
---------------------------------
INVALID KEY.

14.5 START with no matching key
-------------------------------
INVALID KEY.

14.6 READ NEXT after DELETE
---------------------------
Skips deleted records.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp File I/O Architecture:
- Implements full COBOL SEQUENTIAL, RELATIVE, and INDEXED file semantics
- Provides deterministic record buffer layout and key handling
- Integrates with ExceptionState and declaratives
- Generates clean, verifiable, debugger‑friendly CIL
- Ensures correctness across CoreCLR, AOT, and WASM
