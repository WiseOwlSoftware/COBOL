CobolSharp Performance Architecture — IL Optimizations, StorageBlock Access Patterns & Engine Throughput (CIL‑Only)
===================================================================================================================

Purpose
-------
Define the authoritative architecture for:
- IL‑level performance optimizations
- StorageBlock access patterns
- NumericEngine and StringEngine throughput
- FileManager performance
- JSON/XML/SORT/REPORT engine performance
- CALL/ENTRY overhead minimization
- PERFORM loop optimization
- AOT/WASM performance constraints
- Deterministic, stable performance across platforms
- CIL‑friendly, verifiable optimizations

This document governs how CobolSharp achieves high performance while preserving strict determinism.

------------------------------------------------------------
SECTION 1 — PERFORMANCE MODEL OVERVIEW
------------------------------------------------------------

CobolSharp performance goals:
- Predictable  
- Deterministic  
- High throughput  
- Low allocation  
- No JIT‑specific optimizations  
- AOT/WASM‑friendly  

Performance domains:
- IL generation  
- StorageBlock access  
- Numeric operations  
- String operations  
- File I/O  
- JSON/XML parsing  
- SORT engine  
- REPORT engine  

------------------------------------------------------------
SECTION 2 — IL OPTIMIZATION STRATEGIES
------------------------------------------------------------

2.1 Branch flattening
---------------------
Compiler flattens:
- Nested IFs  
- EVALUATE trees  
- PERFORM UNTIL loops  

2.2 Basic block merging
-----------------------
Adjacent blocks merged to reduce:
- Branch instructions  
- Sequence points  

2.3 Constant folding
--------------------
Compiler folds:
- Numeric literals  
- PIC scaling  
- LENGTH of fixed fields  

2.4 Dead store elimination
--------------------------
Compiler removes:
- Writes to unused fields  
- Temporary variables  

2.5 Tail‑call optimization
--------------------------
CALL → ENTRY tail‑calls when:
- No RETURNING  
- No cleanup required  

------------------------------------------------------------
SECTION 3 — STORAGEBLOCK ACCESS OPTIMIZATION
------------------------------------------------------------

3.1 Direct byte[] access
------------------------
All field access lowered to:
- ldloc buffer  
- ldind.u1 / stind.u1  
- Span‑like loops (safe)  

3.2 Precomputed offsets
-----------------------
Offsets computed at compile time:
- No runtime PIC parsing  
- No runtime REDEFINES resolution  

3.3 Loop unrolling
------------------
For fixed‑size fields:
- Compiler unrolls byte loops  
- Especially for MOVE and STRING  

3.4 Zero‑allocation substringing
--------------------------------
Reference modification uses:
- Offset + length  
- No new strings unless DISPLAY/NATIONAL conversion required  

------------------------------------------------------------
SECTION 4 — NUMERICENGINE PERFORMANCE
------------------------------------------------------------

4.1 Decimal arithmetic
----------------------
Optimized via:
- Pre‑scaled operands  
- Avoiding Decimal.Parse  
- Avoiding BigInteger  

4.2 COMP‑3 decoding
-------------------
Optimized nibble extraction:
- No branching  
- Table‑driven  

4.3 COMP‑5 operations
---------------------
Binary operations use:
- Native integer ops  
- Overflow detection via checked blocks  

4.4 ROUNDED logic
-----------------
Compiler emits:
- Inline rounding  
- No helper calls for simple cases  

------------------------------------------------------------
SECTION 5 — STRINGENGINE PERFORMANCE
------------------------------------------------------------

5.1 STRING/UNSTRING
-------------------
Optimized via:
- Span‑like loops  
- No intermediate allocations  
- Precomputed padding  

5.2 INSPECT
-----------
Optimized via:
- Vectorized search (when available)  
- Fallback to byte loop  

5.3 NATIONAL handling
---------------------
UTF‑16 operations:
- Surrogate‑pair safe  
- No transcoding unless required  

------------------------------------------------------------
SECTION 6 — FILEMANAGER PERFORMANCE
------------------------------------------------------------

6.1 Buffered I/O
----------------
FileManager uses:
- Buffered reads  
- Buffered writes  
- Predictive prefetch  

6.2 Indexed file performance
----------------------------
B‑tree optimized for:
- Sequential access  
- Range scans  
- Key caching  

6.3 Relative file performance
-----------------------------
RRN lookup:
- O(1)  
- Cached record size  

6.4 START optimization
----------------------
START uses:
- Binary search for indexed files  
- Cached key positions  

------------------------------------------------------------
SECTION 7 — JSON/XML ENGINE PERFORMANCE
------------------------------------------------------------

7.1 SAX parser
--------------
Streaming parser:
- Zero‑allocation tokens  
- Deterministic state machine  
- No DOM building  

7.2 JSON GENERATE
-----------------
Optimized via:
- Precomputed field offsets  
- Precomputed numeric formats  

7.3 XML GENERATE
----------------
Optimized via:
- Precomputed element names  
- Precomputed attribute tables  

------------------------------------------------------------
SECTION 8 — SORT ENGINE PERFORMANCE
------------------------------------------------------------

8.1 External merge sort
-----------------------
Optimized via:
- Chunked runs  
- Multi‑way merge  
- Minimal allocations  

8.2 Key extraction
------------------
Compiler emits:
- Inline key extraction  
- No reflection  
- No dynamic dispatch  

8.3 Stable merge
----------------
Merge algorithm:
- Branch‑predictable  
- Deterministic  

------------------------------------------------------------
SECTION 9 — REPORT ENGINE PERFORMANCE
------------------------------------------------------------

9.1 Page/line control
---------------------
Optimized via:
- Precomputed line widths  
- Precomputed column offsets  

9.2 Control‑break logic
-----------------------
Optimized via:
- Inline comparisons  
- No dynamic dispatch  

9.3 Rendering
-------------
Rendering uses:
- Preallocated buffers  
- Zero‑allocation concatenation  

------------------------------------------------------------
SECTION 10 — CALL/ENTRY PERFORMANCE
------------------------------------------------------------

10.1 Lightweight ExecutionContext
---------------------------------
ExecutionContext:
- Allocated once per CALL  
- Reused for ENTRY transitions  

10.2 LINKAGE mapping
--------------------
BY REFERENCE:
- Zero‑copy  
- Direct overlay  

BY CONTENT:
- Single copy  

BY VALUE:
- No StorageBlock allocation  

------------------------------------------------------------
SECTION 11 — PERFORM LOOP PERFORMANCE
------------------------------------------------------------

11.1 Loop lowering
------------------
PERFORM UNTIL → while loop  
PERFORM VARYING → for loop  

11.2 Loop invariant hoisting
----------------------------
Compiler hoists:
- Bounds  
- Offsets  
- PIC metadata  

11.3 Branch prediction
----------------------
Compiler emits:
- Forward branches  
- Predictable patterns  

------------------------------------------------------------
SECTION 12 — AOT/WASM PERFORMANCE
------------------------------------------------------------

12.1 AOT optimizations
----------------------
AOT benefits from:
- No JIT warmup  
- Inlined helper methods  
- Dead code elimination  

12.2 WASM optimizations
-----------------------
WASM benefits from:
- Linear memory model  
- Predictable branching  
- No GC pressure from StorageBlocks  

12.3 WASM constraints
---------------------
WASM forbids:
- Threads  
- SIMD (unless enabled)  
- Unaligned memory access  

CobolSharp avoids all forbidden patterns.

------------------------------------------------------------
SECTION 13 — EDGE‑CASE PERFORMANCE BEHAVIOR
------------------------------------------------------------

13.1 Very large OCCURS tables
-----------------------------
Compiler emits:
- Bounds‑checked loops  
- No dynamic resizing  

13.2 Deep recursion
-------------------
ExecutionContext stack optimized for:
- Tail‑calls  
- Minimal frame size  

13.3 Large SORT operations
--------------------------
SORT engine uses:
- External merge  
- Minimal memory footprint