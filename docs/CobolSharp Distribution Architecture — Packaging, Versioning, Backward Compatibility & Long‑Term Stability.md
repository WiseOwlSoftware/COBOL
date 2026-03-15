CobolSharp Distribution Architecture — Packaging, Versioning, Backward Compatibility & Long‑Term Stability (CIL‑Only)
=====================================================================================================================

Purpose
-------
Define the authoritative architecture for:
- CobolSharp distribution packaging
- Versioning strategy (compiler, runtime, standard library)
- Backward compatibility guarantees
- Forward compatibility constraints
- Long‑term stability model
- Assembly layout and strong‑naming
- ProgramRegistry versioning
- WASM and AOT distribution packaging
- Deterministic build reproducibility across versions
- Upgrade and migration rules

This document governs how CobolSharp ensures stable, predictable, long‑term compatibility for production COBOL workloads.

------------------------------------------------------------
SECTION 1 — DISTRIBUTION OVERVIEW
------------------------------------------------------------

CobolSharp is distributed as:
- Compiler package (NuGet + standalone CLI)
- Runtime package (NuGet)
- Standard library package (NuGet)
- WASM runtime bundle
- AOT runtime bundle
- Tools package (debugger helpers, test harness)

Distribution goals:
- Deterministic builds
- Stable ABI
- Stable metadata
- Long‑term compatibility
- Zero‑surprise upgrades

------------------------------------------------------------
SECTION 2 — VERSIONING MODEL
------------------------------------------------------------

2.1 Semantic versioning
-----------------------
CobolSharp uses:
MAJOR.MINOR.PATCH

MAJOR:
- Breaking changes  
- New language features requiring new runtime  

MINOR:
- Backward‑compatible features  
- New intrinsics  
- New compiler optimizations  

PATCH:
- Bug fixes  
- Performance improvements  
- No behavior changes  

2.2 Compiler/runtime lockstep
-----------------------------
Compiler version X.Y.Z requires:
- Runtime version X.Y.*

2.3 Standard library versioning
-------------------------------
Standard library version matches:
- Compiler major/minor  
- Runtime major/minor  

------------------------------------------------------------
SECTION 3 — BACKWARD COMPATIBILITY GUARANTEES
------------------------------------------------------------

CobolSharp guarantees:
- Programs compiled with version X.Y run on runtime X.Y+N  
- No breaking changes in:
  - Numeric semantics  
  - File I/O semantics  
  - JSON/XML semantics  
  - SORT semantics  
  - REPORT semantics  
  - StorageBlock layout rules  
  - ExceptionState categories  

3.1 IL stability
----------------
Generated IL is stable across:
- Patch versions  
- Minor versions (unless new features used)  

3.2 Metadata stability
----------------------
Metadata tables remain:
- Sorted  
- Deterministic  
- Backward compatible  

3.3 ProgramRegistry stability
-----------------------------
Registry format is:
- Versioned  
- Backward compatible  
- Forward compatible (fields ignored if unknown)  

------------------------------------------------------------
SECTION 4 — FORWARD COMPATIBILITY RULES
------------------------------------------------------------

CobolSharp ensures:
- Programs compiled with older compiler run on newer runtime  
- Programs compiled with newer compiler may require newer runtime  

Forward compatibility constraints:
- New PIC categories forbidden  
- New numeric types forbidden  
- New file organizations forbidden  
- New declarative types forbidden  

------------------------------------------------------------
SECTION 5 — STRONG‑NAMING & ASSEMBLY LAYOUT
------------------------------------------------------------

5.1 Strong‑naming
-----------------
All CobolSharp assemblies:
- Strong‑named  
- Versioned  
- Signed  

5.2 Assembly layout
-------------------
Assemblies include:
- Compiler metadata  
- ProgramRegistry  
- Sequence points  
- Deterministic GUIDs  

5.3 Assembly identity
---------------------
Identity includes:
- Name  
- Version  
- Public key token  

------------------------------------------------------------
SECTION 6 — WASM DISTRIBUTION PACKAGING
------------------------------------------------------------

6.1 WASM bundle contents
------------------------
- dotnet.wasm  
- runtime.js  
- app.wasm  
- ProgramRegistry.json  
- index.html  
- Virtual FS seed  

6.2 WASM versioning
-------------------
WASM runtime version must match:
- Compiler major/minor  
- Runtime major/minor  

6.3 WASM backward compatibility
-------------------------------
Guaranteed:
- Virtual FS format  
- JSON/XML behavior  
- Numeric behavior  
- Event loop behavior  

------------------------------------------------------------
SECTION 7 — AOT DISTRIBUTION PACKAGING
------------------------------------------------------------

7.1 AOT bundle contents
-----------------------
- Native binary  
- Metadata tables  
- ProgramRegistry  
- Debug symbols (optional)  

7.2 AOT versioning
------------------
AOT runtime version must match:
- Compiler major/minor  

7.3 AOT backward compatibility
------------------------------
Guaranteed:
- Numeric semantics  
- File I/O semantics  
- StorageBlock layout  

------------------------------------------------------------
SECTION 8 — MIGRATION & UPGRADE RULES
------------------------------------------------------------

8.1 Minor version upgrade
-------------------------
Safe:
- No code changes required  
- No rebuild required (runtime only)  

8.2 Patch version upgrade
-------------------------
Always safe.

8.3 Major version upgrade
-------------------------
Requires:
- Rebuild  
- Review of release notes  
- Possible migration of deprecated features  

------------------------------------------------------------
SECTION 9 — DEPRECATION MODEL
------------------------------------------------------------

9.1 Deprecation stages
----------------------
Stage 1: Warning  
Stage 2: Disabled by default  
Stage 3: Removed in next major version  

9.2 Deprecation categories
--------------------------
- Intrinsics  
- Compiler directives  
- Obsolete syntax  
- Runtime APIs  

------------------------------------------------------------
SECTION 10 — DETERMINISTIC PACKAGING
------------------------------------------------------------

10.1 Build reproducibility
--------------------------
CobolSharp ensures:
- Same source → same binary  
- Same metadata ordering  
- Same ProgramRegistry ordering  

10.2 Hashing
------------
Distribution includes:
- SHA‑256 of IL  
- SHA‑256 of metadata  
- SHA‑256 of ProgramRegistry  

------------------------------------------------------------
SECTION 11 — LONG‑TERM STABILITY MODEL
------------------------------------------------------------

11.1 Stability goals
--------------------
CobolSharp guarantees:
- 10‑year backward compatibility window  
- No silent behavior changes  
- No nondeterministic changes  

11.2 Stability domains
----------------------
Stable:
- Numeric semantics  
- File I/O  
- JSON/XML  
- SORT  
- REPORT  
- StorageBlock layout  

Evolving:
- Optimizations  
- Debugger features  
- WASM host integration  

------------------------------------------------------------
SECTION 12 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

12.1 Runtime older than compiler
--------------------------------
Runtime error:
INCOMPATIBLE RUNTIME VERSION

12.2 Compiler older than runtime
--------------------------------
Allowed.

12.3 WASM bundle missing ProgramRegistry
----------------------------------------
Runtime error.

12.4 AOT binary missing metadata
--------------------------------
Runtime error.

12.5 Deprecated feature used
----------------------------
Compiler warning.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Distribution Architecture:
- Ensures long‑term stability and backward compatibility
- Provides deterministic packaging for CoreCLR, AOT, and WASM
- Uses strong versioning and reproducible builds
- Guarantees stable semantics across upgrades
- Supports safe, predictable evolution of the compiler and runtime
