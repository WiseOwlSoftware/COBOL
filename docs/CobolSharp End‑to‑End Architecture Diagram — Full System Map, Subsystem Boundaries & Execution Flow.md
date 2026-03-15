CobolSharp End‑to‑End Architecture Diagram — Full System Map, Subsystem Boundaries & Execution Flow (CIL‑Only)
==============================================================================================================

Purpose
-------
Define the authoritative, end‑to‑end architecture for CobolSharp:
- Compiler pipeline (lexing → IL)
- Runtime pipeline (ExecutionContext → event loop)
- Subsystem boundaries (NumericEngine, StringEngine, FileManager, JsonEngine, XmlEngine, SortEngine, ReportEngine)
- Memory model (StorageBlocks, ObjectTable)
- Program model (CALL/ENTRY, declaratives)
- Determinism model (cross‑platform guarantees)
- AOT/WASM integration
- Debugger integration
- Distribution and packaging flow

This document provides a complete system map and execution flow for CobolSharp.

------------------------------------------------------------
SECTION 1 — HIGH‑LEVEL SYSTEM MAP
------------------------------------------------------------

CobolSharp consists of three major layers:

1. **Compiler Layer**
   - Lexing
   - Parsing
   - Semantic binding
   - CFG/DFG
   - IL generation
   - Assembly emission
   - ProgramRegistry generation

2. **Runtime Layer**
   - ExecutionContext
   - StorageBlocks
   - ObjectTable
   - Event loop (cooperative scheduler)
   - Subsystems (NumericEngine, StringEngine, FileManager, JsonEngine, XmlEngine, SortEngine, ReportEngine)

3. **Platform Layer**
   - CoreCLR
   - AOT
   - WASM
   - Virtual FS
   - Host integration (console, timers, async interop)

------------------------------------------------------------
SECTION 2 — END‑TO‑END EXECUTION FLOW (COMPILER → RUNTIME)
------------------------------------------------------------

2.1 Source input
----------------
User provides:
- *.cbl files
- *.cpy copybooks
- Build manifest

2.2 Compiler pipeline
---------------------
1. Lexing  
2. Parsing  
3. AST normalization  
4. Semantic binding  
5. CFG/DFG generation  
6. IL generation  
7. Assembly emission  
8. ProgramRegistry generation  
9. AOT/WASM compilation (optional)  

2.3 Runtime activation
----------------------
1. Load assembly  
2. Load ProgramRegistry  
3. Create initial ExecutionContext  
4. Allocate StorageBlocks  
5. Initialize subsystems  
6. Dispatch ENTRY point  

2.4 Execution loop
------------------
Execution proceeds via:
- Paragraph execution  
- Statement execution  
- Subsystem calls  
- Event loop scheduling  

2.5 Program termination
-----------------------
Program ends via:
- GOBACK  
- EXIT PROGRAM  
- STOP RUN  

------------------------------------------------------------
SECTION 3 — SUBSYSTEM BOUNDARIES
------------------------------------------------------------

3.1 NumericEngine
-----------------
Responsibilities:
- Decimal arithmetic  
- COMP‑3 encoding/decoding  
- COMP‑5 operations  
- ROUNDED logic  

Inputs:
- Raw bytes  
- PIC metadata  

Outputs:
- Decimal values  
- Encoded bytes  

3.2 StringEngine
----------------
Responsibilities:
- STRING/UNSTRING  
- INSPECT  
- NATIONAL handling  
- Case mapping  

Inputs:
- StorageBlock slices  

Outputs:
- Modified StorageBlocks  
- Temporary strings  

3.3 FileManager
---------------
Responsibilities:
- Sequential/Indexed/Relative I/O  
- B‑tree management  
- File status codes  

Inputs:
- FD metadata  
- Record buffers  

Outputs:
- Updated StorageBlocks  
- Status codes  

3.4 JsonEngine / XmlEngine
--------------------------
Responsibilities:
- SAX parsing  
- JSON/XML GENERATE  
- Error routing  

Inputs:
- StorageBlocks  
- JSON/XML text  

Outputs:
- StorageBlocks  
- ExceptionState  

3.5 SortEngine
--------------
Responsibilities:
- External merge sort  
- Key extraction  
- Collation  

Inputs:
- Record buffers  
- Key metadata  

Outputs:
- Sorted output  

3.6 ReportEngine
----------------
Responsibilities:
- Page/line control  
- Control‑break logic  
- Rendering  

Inputs:
- Report metadata  
- StorageBlocks  

Outputs:
- UTF‑16 output  

------------------------------------------------------------
SECTION 4 — MEMORY MODEL FLOW
------------------------------------------------------------

4.1 StorageBlock lifecycle
--------------------------
1. Allocation  
2. Field offset mapping  
3. REDEFINES overlay  
4. OCCURS indexing  
5. ODO evaluation  
6. Subsystem access  
7. Debugger inspection  

4.2 ObjectTable lifecycle
-------------------------
1. Allocation  
2. Object creation  
3. Method invocation  
4. Garbage eligibility  
5. Debugger inspection  

------------------------------------------------------------
SECTION 5 — CONTROL‑FLOW MODEL
------------------------------------------------------------

5.1 Paragraph execution
-----------------------
Paragraph → IL method  
Control flows via:
- PERFORM  
- GO TO  
- Declaratives  

5.2 CALL/ENTRY model
--------------------
CALL:
- New ExecutionContext  
- New StorageBlocks (except COMMON)  

ENTRY:
- Method dispatch  
- LINKAGE mapping  

5.3 Declaratives
----------------
Triggered by:
- File errors  
- JSON/XML errors  
- Standard exceptions  

------------------------------------------------------------
SECTION 6 — EVENT LOOP MODEL
------------------------------------------------------------

6.1 Scheduling points
---------------------
- File I/O  
- JSON/XML parsing  
- Async .NET interop  
- Timers  

6.2 Event queue
---------------
FIFO:
- I/O completions  
- Timer events  
- Async completions  

6.3 Resumption
--------------
Event loop resumes:
- ExecutionContext  
- Statement after failing statement  

------------------------------------------------------------
SECTION 7 — AOT/WASM EXECUTION FLOW
------------------------------------------------------------

7.1 AOT flow
------------
1. IL → native binary  
2. Link‑time trimming  
3. Deterministic metadata  
4. Native execution  

7.2 WASM flow
-------------
1. IL → WASM AOT  
2. Virtual FS  
3. JS host integration  
4. Deterministic event loop  

------------------------------------------------------------
SECTION 8 — DEBUGGER INTEGRATION FLOW
------------------------------------------------------------

8.1 Sequence points
-------------------
Compiler emits:
- One per statement  
- One per paragraph  

8.2 Debugger views
------------------
Debugger displays:
- StorageBlocks  
- ObjectTable  
- FileManager state  
- ExecutionContext  
- PERFORM stack  
- CALL stack  

8.3 Declarative tracing
-----------------------
Debugger shows:
- Triggered declarative  
- ExceptionState  
- Resume location  

------------------------------------------------------------
SECTION 9 — DISTRIBUTION & PACKAGING FLOW
------------------------------------------------------------

9.1 Build outputs
-----------------
- Assembly  
- PDB  
- ProgramRegistry  
- WASM bundle (optional)  
- AOT binary (optional)  

9.2 Deterministic packaging
---------------------------
- Stable metadata  
- Stable IL  
- Stable registry  

------------------------------------------------------------
SECTION 10 — END‑TO‑END ASCII DIAGRAM
------------------------------------------------------------

10.1 Full system map
--------------------

+---------------------------+
|        Source Files       |
+-------------+-------------+
              |
              v
+---------------------------+
|        Compiler           |
| Lex → Parse → Bind → IL  |
+-------------+-------------+
              |
              v
+---------------------------+
|     Assembly + PDB        |
|   ProgramRegistry.json    |
+-------------+-------------+
              |
              v
+---------------------------+
|       Runtime Loader      |
+-------------+-------------+
              |
              v
+---------------------------+
|    ExecutionContext       |
|  StorageBlocks / Objects  |
+-------------+-------------+
              |
              v
+---------------------------+
|       Event Loop          |
+-------------+-------------+
   |      |       |      |
   v      v       v      v
 File   JSON    SORT   Timers
 I/O    XML
   \      |       |      /
    \     |       |     /
     +----+-------+----+
              |
              v
+---------------------------+
|       Subsystems          |
| Numeric / String / File   |
| JSON / XML / Sort / Report|
+-------------+-------------+
              |
              v
+---------------------------+
|     Output / FS / Host    |
+---------------------------+

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp End‑to‑End Architecture:
- Defines a complete, deterministic pipeline from source → IL → runtime → output
- Establishes clear subsystem boundaries and execution flow
- Integrates compiler, runtime, event loop, and platform layers
- Ensures reproducibility across CoreCLR, AOT, and WASM
- Provides a unified mental model for the entire CobolSharp ecosystem
