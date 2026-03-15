CobolSharp Future Extensions Roadmap — SQL, CICS, Distributed Files & Multi‑Tenant Runtime Architecture (CIL‑Only)
=================================================================================================================

Purpose
-------
Define the forward‑looking, non‑breaking roadmap for:
- SQL integration (embedded SQL, precompiler model)
- CICS‑style transactional extensions
- Distributed file systems and remote indexed files
- Multi‑tenant runtime architecture
- Cloud‑native execution model
- Extended debugging and observability
- Optional parallel SORT engine
- Optional vectorized numeric engine
- Optional incremental compilation
- Optional JIT‑assisted hot paths (without nondeterminism)
- Future WASM host capabilities

This document outlines *possible* future extensions that remain compatible with CobolSharp’s determinism, safety, and stability guarantees.

------------------------------------------------------------
SECTION 1 — GUIDING PRINCIPLES
------------------------------------------------------------

All future extensions must:
- Preserve determinism  
- Preserve safety (no unsafe code, no reflection)  
- Preserve reproducibility  
- Preserve backward compatibility  
- Preserve StorageBlock layout rules  
- Preserve numeric semantics  
- Preserve file I/O semantics  
- Preserve AOT/WASM compatibility  

Extensions must be:
- Opt‑in  
- Strictly additive  
- Fully sandboxed  
- Fully deterministic  

------------------------------------------------------------
SECTION 2 — EMBEDDED SQL (FUTURE EXTENSION)
------------------------------------------------------------

2.1 Precompiler model
---------------------
CobolSharp will use:
- SQL precompiler  
- SQL → .NET data access layer  
- Deterministic SQL parameter binding  

2.2 Supported SQL dialects
--------------------------
Initial target:
- ANSI SQL  
- Optional vendor modules (PostgreSQL, SQL Server)  

2.3 SQL host variables
----------------------
Host variables map to:
- LINKAGE SECTION  
- WORKING‑STORAGE  
- LOCAL‑STORAGE  

2.4 Deterministic SQL behavior
------------------------------
SQL operations must:
- Use deterministic transaction isolation  
- Avoid nondeterministic functions (NOW(), RANDOM())  
- Avoid server‑side nondeterminism  

2.5 SQL error routing
---------------------
SQL errors routed to:
- ON EXCEPTION  
- USE AFTER STANDARD EXCEPTION  

------------------------------------------------------------
SECTION 3 — CICS‑STYLE TRANSACTIONAL EXTENSIONS (FUTURE)
------------------------------------------------------------

3.1 EXEC CICS model
-------------------
CobolSharp may support:
- EXEC CICS SEND/RECEIVE  
- EXEC CICS READ/WRITE  
- EXEC CICS SYNCPOINT  
- EXEC CICS HANDLE CONDITION  

3.2 Deterministic transaction model
-----------------------------------
Transactions must:
- Be single‑threaded  
- Be deterministic  
- Use stable ordering  

3.3 CICS error routing
----------------------
Routed to:
- ON EXCEPTION  
- USE AFTER STANDARD EXCEPTION  

3.4 No distributed locks
------------------------
To preserve determinism.

------------------------------------------------------------
SECTION 4 — DISTRIBUTED FILE SYSTEMS (FUTURE)
------------------------------------------------------------

4.1 Remote indexed files
------------------------
CobolSharp may support:
- Remote sequential files  
- Remote indexed files  
- Remote relative files  

4.2 Deterministic network model
-------------------------------
Network operations must:
- Use deterministic retry logic  
- Use deterministic timeouts  
- Use deterministic ordering  

4.3 Virtual FS abstraction
--------------------------
Remote FS exposed as:
- FileManager provider  
- Same semantics as local FS  

4.4 Caching rules
-----------------
Caching must be:
- Deterministic  
- Explicit  
- Opt‑in  

------------------------------------------------------------
SECTION 5 — MULTI‑TENANT RUNTIME ARCHITECTURE (FUTURE)
------------------------------------------------------------

5.1 Tenant isolation
--------------------
Each tenant receives:
- Independent ExecutionContext  
- Independent ObjectTable  
- Independent FileManager  
- Independent PRNG seed  

5.2 Resource quotas
-------------------
Per‑tenant quotas:
- CPU time  
- Memory  
- File handles  
- JSON/XML depth  
- SORT size  

5.3 Deterministic scheduling
----------------------------
Tenants scheduled via:
- Cooperative event loop  
- Deterministic ordering  

------------------------------------------------------------
SECTION 6 — CLOUD‑NATIVE EXECUTION MODEL (FUTURE)
------------------------------------------------------------

6.1 Stateless execution
-----------------------
Programs may run:
- In serverless environments  
- With ephemeral ExecutionContexts  

6.2 Durable state
-----------------
State stored in:
- Deterministic key/value store  
- Deterministic file store  

6.3 Event‑driven COBOL
----------------------
Triggers:
- File arrival  
- Timer  
- Queue message  

------------------------------------------------------------
SECTION 7 — OPTIONAL PARALLEL SORT ENGINE (FUTURE)
------------------------------------------------------------

7.1 Deterministic parallelism
-----------------------------
Parallel SORT must:
- Produce identical output to serial SORT  
- Use deterministic merge ordering  

7.2 Parallel merge
------------------
Parallel merge uses:
- Fixed partitioning  
- Deterministic tie‑breaking  

7.3 WASM fallback
-----------------
WASM uses serial SORT.

------------------------------------------------------------
SECTION 8 — OPTIONAL VECTORIZED NUMERIC ENGINE (FUTURE)
------------------------------------------------------------

8.1 SIMD acceleration
---------------------
If platform supports SIMD:
- Decimal operations vectorized  
- COMP‑3 decoding vectorized  

8.2 Deterministic fallback
--------------------------
If SIMD unavailable:
- Scalar path produces identical results  

------------------------------------------------------------
SECTION 9 — OPTIONAL INCREMENTAL COMPILATION (FUTURE)
------------------------------------------------------------

9.1 Incremental pipeline
------------------------
Compiler may support:
- AST caching  
- Semantic metadata caching  
- IL fragment caching  

9.2 Deterministic rebuilds
--------------------------
Incremental builds must:
- Produce identical IL to full rebuild  

------------------------------------------------------------
SECTION 10 — OPTIONAL JIT‑ASSISTED HOT PATHS (FUTURE)
------------------------------------------------------------

10.1 JIT hints
--------------
Compiler may emit:
- AggressiveInlining  
- Loop unrolling hints  

10.2 Deterministic behavior
---------------------------
JIT optimizations must:
- Not change semantics  
- Not change ordering  
- Not introduce nondeterminism  

------------------------------------------------------------
SECTION 11 — FUTURE WASM HOST CAPABILITIES
------------------------------------------------------------

11.1 WASM threads (optional)
----------------------------
If WASM threads become stable:
- Parallel SORT may be enabled  
- Still deterministic  

11.2 WASM SIMD
--------------
If WASM SIMD stable:
- Vectorized numeric engine enabled  

11.3 WASM GC
------------
If WASM GC stable:
- Reduced memory overhead  

------------------------------------------------------------
SECTION 12 — EDGE‑CASE FUTURE BEHAVIOR
------------------------------------------------------------

12.1 SQL nondeterministic functions
-----------------------------------
Compile‑time error.

12.2 CICS distributed locks
---------------------------
Forbidden.

12.3 Remote FS