CobolSharp Build System — Project Structure, Multi‑Targeting, AOT/WASM Pipelines & Deterministic Packaging (CIL‑Only)
====================================================================================================================

Purpose
-------
Define the authoritative architecture for:
- CobolSharp project structure
- Multi‑targeting (CoreCLR, AOT, WASM)
- Build pipeline stages
- Deterministic IL generation
- Deterministic packaging and reproducible builds
- Assembly layout and metadata
- Program registry generation
- PDB and sequence‑point generation
- WASM bundling and host integration
- AOT constraints and link‑time trimming
- CIL‑friendly, platform‑independent build outputs

This document governs how CobolSharp produces reproducible, cross‑platform binaries.

------------------------------------------------------------
SECTION 1 — PROJECT STRUCTURE
------------------------------------------------------------

1.1 Standard layout
-------------------
A CobolSharp project contains:
- src/  
  - *.cbl source files  
  - *.cpy copybooks  
- obj/  
  - Intermediate AST, CFG, DFG  
  - Semantic metadata  
  - IL fragments  
- bin/  
  - Final assemblies  
  - WASM bundles  
  - AOT artifacts  

1.2 Multi‑module projects
-------------------------
Supported:
- Multiple programs per project  
- Multiple ENTRY points per program  
- Shared copybooks  
- Shared COMMON programs  

1.3 Build manifest
------------------
CobolSharp generates:
- ProgramRegistry.json  
- Metadata tables  
- Deterministic hash of source tree  

------------------------------------------------------------
SECTION 2 — MULTI‑TARGETING
------------------------------------------------------------

2.1 Supported targets
---------------------
- net8.0 (CoreCLR)  
- net8.0‑aot (Native AOT)  
- net8.0‑wasm (WASM AOT)  

2.2 Target selection
--------------------
Configured via:
- csproj  
- CLI flags  
- Build manifest  

2.3 Cross‑target consistency
----------------------------
CobolSharp guarantees:
- Identical semantics  
- Identical numeric behavior  
- Identical file I/O semantics (WASM uses virtual FS)  

------------------------------------------------------------
SECTION 3 — BUILD PIPELINE
------------------------------------------------------------

3.1 Pipeline stages
-------------------
1. Lexing  
2. Parsing  
3. AST normalization  
4. Semantic binding  
5. CFG/DFG generation  
6. IL generation  
7. Assembly emission  
8. AOT/WASM compilation (optional)  
9. Packaging  

3.2 Deterministic pipeline
--------------------------
- No parallel compilation  
- No nondeterministic ordering  
- No hash‑based ordering  
- Stable metadata ordering  

------------------------------------------------------------
SECTION 4 — IL GENERATION & ASSEMBLY EMISSION
------------------------------------------------------------

4.1 IL generation
-----------------
Compiler emits:
- Verifiable CIL  
- Deterministic method ordering  
- Deterministic field ordering  
- Deterministic type ordering  

4.2 Assembly emission
---------------------
CobolSharp uses:
- System.Reflection.Metadata  
- Deterministic metadata writer  
- Stable GUIDs  
- Stable MVID (unless source changes)  

4.3 PDB generation
------------------
PDB contains:
- Sequence points  
- Local variable names  
- Paragraph/method names  
- StorageBlock field names  

------------------------------------------------------------
SECTION 5 — PROGRAM REGISTRY GENERATION
------------------------------------------------------------

5.1 Registry contents
---------------------
ProgramRegistry contains:
- Program name → .NET type  
- ENTRY name → method  
- COMMON/INITIAL flags  
- Metadata hashes  

5.2 Deterministic ordering
--------------------------
Registry sorted:
- Alphabetically by program name  
- Then by ENTRY name  

5.3 No runtime reflection
-------------------------
Registry used instead of:
- Type.GetType  
- Assembly scanning  

------------------------------------------------------------
SECTION 6 — AOT PIPELINE
------------------------------------------------------------

6.1 AOT compilation
-------------------
CobolSharp emits:
- IL  
- Metadata  
- AOT‑friendly patterns  

Then .NET AOT toolchain produces:
- Native binary  
- Deterministic layout  

6.2 AOT constraints
-------------------
CobolSharp forbids:
- Reflection  
- Dynamic codegen  
- Runtime type discovery  
- Dynamic assembly loading  

6.3 Link‑time trimming
----------------------
CobolSharp marks:
- All ENTRY methods as roots  
- All DECLARATIVES as roots  
- All subsystems as roots  

Everything else trimmed.

------------------------------------------------------------
SECTION 7 — WASM PIPELINE
------------------------------------------------------------

7.1 WASM AOT
------------
CobolSharp emits:
- IL  
- Metadata  
- WASM‑safe patterns  

.NET WASM AOT produces:
- wasm binary  
- js glue  
- virtual FS  

7.2 WASM host integration
-------------------------
CobolSharp provides:
- ConsoleEngine → JS console  
- FileManager → virtual FS  
- Timer events → JS timers  
- Async interop → JS promises  

7.3 WASM packaging
------------------
Output bundle:
- index.html  
- runtime.js  
- dotnet.wasm  
- app.wasm  
- ProgramRegistry.json  

------------------------------------------------------------
SECTION 8 — DETERMINISTIC PACKAGING
------------------------------------------------------------

8.1 Reproducible builds
-----------------------
CobolSharp ensures:
- Same source → same IL  
- Same IL → same AOT/WASM output  
- Same metadata ordering  
- Same PDB ordering  

8.2 Build hash
--------------
CobolSharp computes:
- SHA‑256 of source tree  
- SHA‑256 of metadata  
- SHA‑256 of IL  

8.3 Build manifest
------------------
Contains:
- Target frameworks  
- Build hash  
- Program registry  
- File list  

------------------------------------------------------------
SECTION 9 — COPYBOOK HANDLING
------------------------------------------------------------

9.1 Deterministic expansion
---------------------------
Copybooks expanded:
- At parse time  
- With stable ordering  
- With stable whitespace normalization  

9.2 Copybook caching
--------------------
Compiler caches:
- AST fragments  
- Semantic metadata  

------------------------------------------------------------
SECTION 10 — ERROR REPORTING
------------------------------------------------------------

10.1 Build errors
-----------------
- Lexical  
- Syntax  
- Semantic  
- PIC mismatch  
- File definition error  
- OO inheritance error  
- AOT/WASM incompatibility  

10.2 Build warnings
-------------------
- Unused variable  
- Unreachable code  
- Implicit conversion  
- Truncation warning  

------------------------------------------------------------
SECTION 11 — DEBUGGER SUPPORT
------------------------------------------------------------

11.1 PDB mapping
----------------
Debugger can show:
- COBOL source  
- IL  
- StorageBlocks  
- ExecutionContext  

11.2 WASM debugging
-------------------
Supported via:
- Source maps  
- PDB → WASM mapping  

------------------------------------------------------------
SECTION 12 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

12.1 Copybook redefinition
--------------------------
Compile‑time error.

12.2 Duplicate program name
---------------------------
Compile‑time error.

12.3 ENTRY name collision
-------------------------
Compile‑time error.

12.4 AOT‑unsafe intrinsic
-------------------------
Compile‑time error.

12.5 WASM‑unsafe file path
--------------------------
Runtime error.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Build System:
- Produces deterministic, reproducible binaries across CoreCLR, AOT, and WASM
- Ensures stable IL, metadata, and registry generation
- Supports multi‑module projects and multi‑targeting
- Integrates tightly with the compiler pipeline and ExecutionContext model
- Generates clean, verifiable, debugger‑friendly assemblies
