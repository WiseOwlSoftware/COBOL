CobolSharp COBOL SORT/MERGE, File‑Based Pipeline & Collation Architecture (CIL‑Only)
====================================================================================

Purpose
-------
Define the authoritative architecture for:
- SORT statement
- MERGE statement
- USING / GIVING pipelines
- Input/output procedures
- Collating sequences
- Key definitions (ASCENDING / DESCENDING)
- File‑based vs memory‑based sort
- Record buffer integration
- Exception routing
- AOT/WASM‑safe sorting
- CIL‑friendly lowering

This document governs how CobolSharp implements COBOL’s SORT and MERGE facilities on .NET.

------------------------------------------------------------
SECTION 1 — SORT ENGINE OVERVIEW
------------------------------------------------------------

CobolSharp implements SORT/MERGE using:
- A streaming, file‑backed sort engine
- Optional in‑memory sort for small datasets
- Stable sorting
- Multi‑key comparison
- Collation tables
- Deterministic behavior across platforms

ExecutionContext.SortEngine provides:
- Sort(inputRecords, keys, collation)
- Merge(inputFiles, keys, collation)
- Input/Output procedure orchestration
- ExceptionState population

------------------------------------------------------------
SECTION 2 — SORT STATEMENT
------------------------------------------------------------

2.1 Basic form
--------------
SORT sortFile
    ON ASCENDING KEY key1 key2
    USING inFile1 inFile2
    GIVING outFile
    WITH DUPLICATES IN ORDER.

2.2 Modes
---------
CobolSharp supports:
- USING → input files
- GIVING → output file
- INPUT PROCEDURE → procedural input
- OUTPUT PROCEDURE → procedural output

2.3 Record format
-----------------
sortFile must have:
- FD with record layout
- Key fields defined in FD or SORT statement

------------------------------------------------------------
SECTION 3 — MERGE STATEMENT
------------------------------------------------------------

3.1 Basic form
--------------
MERGE sortFile
    ON DESCENDING KEY key1
    USING inFile1 inFile2 inFile3
    GIVING outFile.

3.2 Semantics
-------------
- Multi‑way merge
- Stable ordering
- Collation applied to keys

------------------------------------------------------------
SECTION 4 — KEY HANDLING
------------------------------------------------------------

4.1 Key types
-------------
Keys may be:
- DISPLAY
- NATIONAL
- COMP/COMP‑5
- COMP‑3

4.2 ASCENDING / DESCENDING
--------------------------
Comparison direction applied per key.

4.3 Multi‑key comparison
------------------------
Keys evaluated in order:
- First key decides
- If equal, next key
- If all equal, record order preserved

4.4 Collation
-------------
DISPLAY keys use:
- Collating sequence (default ASCII)
- Custom sequence via:
  COLLATING SEQUENCE IS alphabetName

NATIONAL keys use:
- Unicode code point order

------------------------------------------------------------
SECTION 5 — USING / GIVING PIPELINE
------------------------------------------------------------

5.1 USING
---------
Input files read sequentially:
- FileManager.ReadSequential
- Records fed into SortEngine

5.2 GIVING
----------
Sorted records written to:
- FileManager.Write

5.3 Input/Output procedures
---------------------------
INPUT PROCEDURE proc:
- Compiler generates call to proc
- proc must WRITE records to sortFile

OUTPUT PROCEDURE proc:
- proc must READ records from sortFile

5.4 Restrictions
----------------
- INPUT PROCEDURE cannot read USING files
- OUTPUT PROCEDURE cannot write GIVING files

------------------------------------------------------------
SECTION 6 — SORT ENGINE IMPLEMENTATION
------------------------------------------------------------

6.1 In‑memory sort
------------------
Used when:
- Record count < threshold
- Memory available

Algorithm:
- Stable merge sort
- Key extraction cached
- Collation applied per key

6.2 File‑backed sort
--------------------
Used when:
- Large datasets
- Memory insufficient

Algorithm:
- External merge sort
- Runs written to temp files
- Multi‑way merge

6.3 MERGE implementation
------------------------
- Multi‑file priority queue
- Key comparison per record
- Stable ordering

------------------------------------------------------------
SECTION 7 — RECORD BUFFER INTEGRATION
------------------------------------------------------------

7.1 Input record
----------------
Record read from USING file:
- Stored in sortFile’s record buffer
- Key extracted via offset table

7.2 Output record
-----------------
Sorted record:
- Written to GIVING file’s record buffer

7.3 Temporary buffers
---------------------
SortEngine allocates:
- Temporary StorageBlocks
- Key extraction buffers

------------------------------------------------------------
SECTION 8 — COLLATION ARCHITECTURE
------------------------------------------------------------

8.1 Default collation
---------------------
ASCII:
- 0–127
- Case‑sensitive

8.2 Custom collation
--------------------
COLLATING SEQUENCE IS alphabetName.

Compiler embeds:
- Lookup table: char → rank
- Used for DISPLAY keys only

8.3 NATIONAL collation
----------------------
Unicode code point order.

------------------------------------------------------------
SECTION 9 — ERROR HANDLING & EXCEPTIONSTATE
------------------------------------------------------------

9.1 SORT errors
---------------
- Invalid key type
- Missing FD
- USING file not open
- GIVING file not open
- Collation mismatch

9.2 MERGE errors
----------------
- Inconsistent record lengths
- Invalid key extraction
- File read/write errors

9.3 ExceptionState
------------------
Populated with:
- Operation type
- File name
- Key name
- Error message

9.4 Routing
-----------
1. ON EXCEPTION  
2. USE AFTER EXCEPTION ON file  
3. USE AFTER STANDARD EXCEPTION  

------------------------------------------------------------
SECTION 10 — CIL LOWERING RULES
------------------------------------------------------------

10.1 SORT lowering
------------------
Generated IL:
- Build SortDescriptor
- Load USING files
- Call SortEngine.Sort
- Write GIVING file

10.2 MERGE lowering
-------------------
Generated IL:
- Build MergeDescriptor
- Load USING files
- Call SortEngine.Merge
- Write GIVING file

10.3 Input/Output procedures
----------------------------
Compiler generates:
- Calls to procedural paragraphs
- Integration with SortEngine

------------------------------------------------------------
SECTION 11 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Current key values
- Collation table
- Input record
- Output record
- Run files (for external sort)
- ExceptionState

------------------------------------------------------------
SECTION 12 — AOT/WASM‑SAFE SORTING
------------------------------------------------------------

12.1 No unsafe code
-------------------
- No pointers
- No stackalloc

12.2 No dynamic codegen
-----------------------
- Pure managed sorting

12.3 Deterministic behavior
---------------------------
- Same results across platforms
- No locale‑dependent ordering

------------------------------------------------------------
SECTION 13 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

13.1 Duplicate keys
-------------------
Stable ordering preserved.

13.2 Missing keys
-----------------
Runtime error.

13.3 USING with zero records
----------------------------
GIVING file created empty.

13.4 INPUT PROCEDURE with no WRITE
----------------------------------
GIVING file empty.

13.5 OUTPUT PROCEDURE with no READ
----------------------------------
Allowed.

13.6 Collation with duplicate ranks
-----------------------------------
Runtime error.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp SORT/MERGE Architecture:
- Implements full COBOL SORT and MERGE semantics
- Supports USING/GIVING pipelines and procedural input/output
- Provides deterministic, stable, multi‑key sorting
- Integrates tightly with FileManager and StorageBlocks
- Generates clean, verifiable, debugger‑friendly CIL
- Ensures correctness across CoreCLR, AOT, and WASM
