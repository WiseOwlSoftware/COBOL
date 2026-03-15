CobolSharp Security Architecture — Sandboxing, Capability Restrictions, WASM Isolation & Safe Interop (CIL‑Only)
===============================================================================================================

Purpose
-------
Define the authoritative architecture for:
- CobolSharp’s security model
- Sandboxing rules (CoreCLR, AOT, WASM)
- Capability restrictions
- File I/O permissions
- Network restrictions
- Safe .NET interop
- Memory safety guarantees
- Deterministic, non‑elevating runtime behavior
- WASM isolation model
- AOT hardening
- CIL‑friendly, verifiable code generation

This document governs how CobolSharp ensures safe, predictable, non‑privileged execution across all platforms.

------------------------------------------------------------
SECTION 1 — SECURITY MODEL OVERVIEW
------------------------------------------------------------

CobolSharp enforces:
- **No elevation of privilege**
- **No arbitrary system access**
- **No unsafe code**
- **No reflection**
- **No dynamic codegen**
- **No unmanaged interop**
- **Deterministic, sandboxed execution**

Security is enforced at:
- Compile time  
- IL generation time  
- Runtime (ExecutionContext)  
- WASM host boundary  
- AOT link‑time  

------------------------------------------------------------
SECTION 2 — MEMORY SAFETY
------------------------------------------------------------

2.1 No unsafe code
------------------
CobolSharp forbids:
- pointers  
- stackalloc  
- fixed blocks  
- unmanaged memory  

2.2 StorageBlocks
-----------------
StorageBlocks are:
- Pure managed byte[]  
- Bounds‑checked  
- Never pinned  
- Never exposed directly to user code  

2.3 ObjectTable
---------------
Object references:
- Are integer indices  
- Cannot escape ExecutionContext  
- Cannot be forged  

2.4 No buffer overflows
-----------------------
All substringing, OCCURS indexing, and REDEFINES overlays are bounds‑checked.

------------------------------------------------------------
SECTION 3 — FILE I/O SECURITY
------------------------------------------------------------

3.1 Allowed operations
----------------------
CobolSharp allows:
- Sequential files  
- Indexed files  
- Relative files  

3.2 Forbidden operations
------------------------
CobolSharp forbids:
- Arbitrary directory traversal  
- Access outside configured root  
- Raw file handles  
- Memory‑mapped files  
- Symbolic link traversal  

3.3 Virtual FS (WASM)
---------------------
WASM uses:
- In‑memory virtual filesystem  
- No host disk access  
- No host network access  

3.4 File path normalization
---------------------------
CobolSharp enforces:
- Canonical paths  
- No “..” traversal  
- No absolute paths unless explicitly allowed  

------------------------------------------------------------
SECTION 4 — NETWORK SECURITY
------------------------------------------------------------

4.1 No network access
---------------------
CobolSharp forbids:
- TCP/UDP sockets  
- HTTP/HTTPS  
- DNS  
- Raw sockets  
- Named pipes  

4.2 Rationale
-------------
Network access introduces:
- Nondeterminism  
- Security risk  
- Platform differences  

------------------------------------------------------------
SECTION 5 — .NET INTEROP SECURITY
------------------------------------------------------------

5.1 Allowed interop
-------------------
INVOKE may call:
- Public .NET methods  
- Public .NET constructors  
- Public .NET properties  

5.2 Forbidden interop
---------------------
CobolSharp forbids:
- Reflection (MethodInfo, Type.GetType, Activator.CreateInstance)  
- Dynamic invocation  
- Unsafe APIs  
- P/Invoke  
- COM interop  
- File IO outside FileManager  
- Threading APIs  

5.3 Assembly whitelist
----------------------
CobolSharp only allows interop with:
- System.*  
- CobolSharp.Runtime.*  
- User‑provided assemblies explicitly referenced at compile time  

5.4 No dynamic assembly loading
-------------------------------
CobolSharp forbids:
- Assembly.Load  
- Assembly.LoadFrom  
- Assembly.LoadFile  

------------------------------------------------------------
SECTION 6 — SANDBOXING (CORECLR)
------------------------------------------------------------

6.1 AppDomain isolation
-----------------------
CobolSharp uses:
- Single AppDomain  
- No cross‑domain execution  

6.2 Restricted APIs
-------------------
CobolSharp blocks:
- Environment.Exit  
- Process.Start  
- File.Delete (outside FS root)  
- Directory enumeration  
- Registry access  

6.3 Deterministic environment
-----------------------------
Environment variables:
- Not exposed  
- Not modifiable  

------------------------------------------------------------
SECTION 7 — AOT SECURITY
------------------------------------------------------------

7.1 No JIT
----------
AOT eliminates:
- JIT injection  
- Runtime code modification  
- Dynamic IL generation  

7.2 Link‑time trimming
----------------------
Trimming removes:
- Unused methods  
- Reflection entry points  
- Unsafe APIs  

7.3 Hardened binary
-------------------
AOT binary:
- Has no dynamic loader  
- Has no reflection metadata (unless required)  
- Has no dynamic dispatch tables  

------------------------------------------------------------
SECTION 8 — WASM SECURITY
------------------------------------------------------------

8.1 WASM sandbox
----------------
WASM provides:
- No direct OS access  
- No raw memory access  
- No syscalls  
- No threads (unless WASM threads enabled)  

8.2 Host boundary
-----------------
CobolSharp exposes:
- ConsoleEngine → JS console  
- Timer events → JS timers  
- Virtual FS → JS memory  

Nothing else crosses boundary.

8.3 No host escape
------------------
CobolSharp forbids:
- JS interop beyond approved APIs  
- Arbitrary JS eval  
- DOM access  

------------------------------------------------------------
SECTION 9 — CAPABILITY RESTRICTIONS
------------------------------------------------------------

9.1 Forbidden COBOL features
----------------------------
CobolSharp forbids:
- CALL literal with runtime‑computed name  
- CALL to external OS commands  
- EXEC SQL (future extension may sandbox)  
- EXEC CICS (not supported)  

9.2 Restricted intrinsics
-------------------------
Forbidden:
- RANDOM with system entropy  
- Locale‑dependent intrinsics  

Allowed:
- Deterministic PRNG  
- Deterministic case mapping  

------------------------------------------------------------
SECTION 10 — EXCEPTION SECURITY
------------------------------------------------------------

10.1 ExceptionState sanitization
--------------------------------
ExceptionState never exposes:
- Host file paths  
- Host environment variables  
- Host stack frames (unless allowed)  

10.2 Declarative isolation
--------------------------
Declaratives cannot:
- Modify ExecutionContext of caller  
- Access host resources  

------------------------------------------------------------
SECTION 11 — COMPILER SECURITY
------------------------------------------------------------

11.1 No code injection
----------------------
Compiler forbids:
- EXECUTE arbitrary text  
- Embedding IL  
- Embedding C#  
- Embedding JS  

11.2 Deterministic codegen
--------------------------
Compiler emits:
- Verifiable IL  
- No unverifiable instructions  
- No unsafe opcodes  

------------------------------------------------------------
SECTION 12 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

12.1 INVOKE attempts to call forbidden API
------------------------------------------
Compile‑time error.

12.2 File path escapes root
---------------------------
Runtime error.

12.3 Reflection attempt via .NET object
---------------------------------------
Runtime error.

12.4 WASM host attempts to inject JS
------------------------------------
Ignored.

12.5 AOT trimming removes required method
-----------------------------------------
Compile‑time error.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Security Architecture:
- Enforces strict sandboxing across CoreCLR, AOT, and WASM
- Eliminates unsafe code, reflection, dynamic loading, and unmanaged interop
- Provides deterministic, non‑privileged execution
- Ensures safe file I/O, safe .NET interop, and safe memory behavior
- Guarantees cross‑platform isolation and long‑term security stability
