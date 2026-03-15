CobolSharp Testing & Verification Architecture — Unit Tests, Golden Files, Deterministic Snapshots & Runtime Validation (CIL‑Only)
=================================================================================================================================

Purpose
-------
Define the authoritative architecture for:
- Compiler unit tests
- Runtime unit tests
- Golden‑file verification
- Deterministic snapshot testing
- ExecutionContext validation
- StorageBlock validation
- File I/O simulation
- JSON/XML/SORT/REPORT verification
- WASM and AOT cross‑target verification
- Reproducibility guarantees
- CIL‑friendly test harness generation

This document governs how CobolSharp ensures correctness, determinism, and cross‑platform reproducibility.

------------------------------------------------------------
SECTION 1 — TESTING OVERVIEW
------------------------------------------------------------

CobolSharp uses a multi‑layered testing strategy:

1. **Compiler tests**  
   - Lexing  
   - Parsing  
   - Semantic binding  
   - CFG/DFG  
   - IL generation  

2. **Runtime tests**  
   - ExecutionContext  
   - StorageBlocks  
   - NumericEngine  
   - StringEngine  
   - FileManager  
   - JsonEngine / XmlEngine  
   - SortEngine  
   - ReportEngine  

3. **Golden‑file tests**  
   - IL output  
   - WASM output  
   - AOT output  
   - ProgramRegistry  

4. **Snapshot tests**  
   - Execution traces  
   - StorageBlock dumps  
   - File I/O sequences  

5. **Cross‑target verification**  
   - CoreCLR vs AOT vs WASM  
   - Identical outputs required  

------------------------------------------------------------
SECTION 2 — COMPILER UNIT TESTS
------------------------------------------------------------

2.1 Lexing tests
----------------
Verify:
- Tokenization  
- PIC parsing  
- Literal parsing  
- Error detection  

2.2 Parsing tests
-----------------
Verify:
- AST structure  
- Grammar coverage  
- Error recovery  

2.3 Semantic binding tests
--------------------------
Verify:
- Symbol tables  
- PIC resolution  
- REDEFINES/OCCURS layout  
- Type checking  
- File metadata  

2.4 IL generation tests
-----------------------
Verify:
- Deterministic IL  
- Correct branching  
- Correct StorageBlock access  
- Correct numeric conversions  

------------------------------------------------------------
SECTION 3 — RUNTIME UNIT TESTS
------------------------------------------------------------

3.1 ExecutionContext tests
--------------------------
Verify:
- Activation/deactivation  
- CALL/ENTRY mapping  
- ReturnValue behavior  
- COMMON/INITIAL semantics  

3.2 StorageBlock tests
----------------------
Verify:
- Offsets  
- REDEFINES overlays  
- OCCURS indexing  
- ODO behavior  
- Numeric encoding/decoding  

3.3 NumericEngine tests
-----------------------
Verify:
- Decimal arithmetic  
- ROUNDED logic  
- Overflow detection  
- COMP‑3 encoding  

3.4 StringEngine tests
----------------------
Verify:
- STRING/UNSTRING  
- INSPECT  
- NATIONAL handling  
- Surrogate‑pair safety  

3.5 FileManager tests
---------------------
Verify:
- Sequential/Indexed/Relative  
- START  
- REWRITE  
- DELETE  
- File status codes  

3.6 JsonEngine / XmlEngine tests
--------------------------------
Verify:
- SAX parsing  
- Error routing  
- JSON/XML GENERATE  

3.7 SortEngine tests
--------------------
Verify:
- Stable merge sort  
- Key extraction  
- Collation  

3.8 ReportEngine tests
----------------------
Verify:
- Page/line control  
- Control‑break logic  
- Accumulators  

------------------------------------------------------------
SECTION 4 — GOLDEN‑FILE VERIFICATION
------------------------------------------------------------

4.1 IL golden files
-------------------
Compiler emits:
- IL  
- Metadata  
- Sequence points  

Golden files stored in:
- tests/golden/il/

4.2 WASM golden files
---------------------
WASM bundle compared byte‑for‑byte.

4.3 AOT golden files
--------------------
Native binary compared via:
- Symbol table  
- Metadata  
- Hash  

4.4 ProgramRegistry golden files
--------------------------------
Registry must match:
- Program names  
- ENTRY names  
- Metadata hashes  

------------------------------------------------------------
SECTION 5 — SNAPSHOT TESTING
------------------------------------------------------------

5.1 Execution trace snapshots
-----------------------------
Snapshots include:
- Paragraph execution order  
- PERFORM stack transitions  
- CALL stack transitions  
- Declarative triggers  

5.2 StorageBlock snapshots
--------------------------
Snapshots include:
- Raw bytes  
- Decoded fields  
- REDEFINES overlays  
- OCCURS tables  

5.3 File I/O snapshots
----------------------
Snapshots include:
- READ/WRITE sequences  
- File status codes  
- Cursor positions  

5.4 JSON/XML snapshots
----------------------
Snapshots include:
- Token sequences  
- Error events  
- Generated output  

------------------------------------------------------------
SECTION 6 — CROSS‑TARGET VERIFICATION
------------------------------------------------------------

6.1 CoreCLR vs AOT vs WASM
--------------------------
All outputs must match:
- Console output  
- File output  
- Report output  
- JSON/XML output  
- SORT output  

6.2 Deterministic PRNG
----------------------
RANDOM must match across targets.

6.3 Deterministic encodings
---------------------------
DISPLAY/NATIONAL identical across targets.

------------------------------------------------------------
SECTION 7 — TEST HARNESS ARCHITECTURE
------------------------------------------------------------

7.1 Harness responsibilities
----------------------------
- Compile COBOL  
- Run program  
- Capture output  
- Capture StorageBlocks  
- Capture FileManager state  
- Compare against golden files  

7.2 Harness isolation
---------------------
Each test runs with:
- Fresh ExecutionContext  
- Fresh virtual FS  
- Fresh PRNG seed  

7.3 Harness determinism
------------------------
Harness enforces:
- No parallel tests  
- No nondeterministic ordering  
- No system clock access (except CURRENT‑DATE tests)  

------------------------------------------------------------
SECTION 8 — ERROR VERIFICATION
------------------------------------------------------------

8.1 ExceptionState snapshots
----------------------------
Snapshots include:
- Category  
- Message  
- Source subsystem  
- File name  
- Key value  
- JSON/XML token  

8.2 Declarative verification
----------------------------
Verify:
- Correct declarative triggered  
- Correct resume location  

------------------------------------------------------------
SECTION 9 — WASM‑SPECIFIC TESTING
------------------------------------------------------------

9.1 Virtual FS tests
--------------------
Verify:
- File I/O  
- Indexed file behavior  
- Relative file behavior  

9.2 JS interop tests
--------------------
Verify:
- ConsoleEngine bridging  
- Timer events  
- Async interop  

------------------------------------------------------------
SECTION 10 — AOT‑SPECIFIC TESTING
------------------------------------------------------------

10.1 Link‑time trimming tests
-----------------------------
Verify:
- All required methods preserved  
- No missing ENTRY points  

10.2 Native binary tests
------------------------
Verify:
- Deterministic layout  
- Deterministic behavior  

------------------------------------------------------------
SECTION 11 — EDGE‑CASE TESTS
------------------------------------------------------------

11.1 REDEFINES + ODO + OCCURS
-----------------------------
Complex overlay tests.

11.2 COMP‑3 odd digits
----------------------
Leading zero insertion.

11.3 NATIONAL truncation
------------------------
Surrogate‑pair safety.

11.4 SORT with equal keys
-------------------------
Stable ordering.

11.5 RANDOM without seed
------------------------
Seed = 1.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Testing & Verification Architecture:
- Ensures correctness across compiler, runtime, and subsystems
- Uses golden files and deterministic snapshots for reproducibility
- Verifies identical behavior across CoreCLR, AOT, and WASM
- Provides deep validation of StorageBlocks, FileManager, JSON/XML, SORT, and REPORT
- Guarantees deterministic, production‑grade COBOL execution
