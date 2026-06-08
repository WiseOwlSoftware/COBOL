CobolSharp COBOL CALL Convention, Program Model, Parameter Passing, LINKAGE & Multi‑Module Architecture (CIL‑Only)
================================================================================================================

> **STATUS BANNER (2026-06-07).** *Design reference for the inter‑program / CALL subsystem.*
> **Implementation status: LARGELY IMPLEMENTED (~85–90%).** CALL literal + CALL identifier, USING with
> BY REFERENCE / BY CONTENT / BY VALUE (transitive‑mode per ISO §14.8 GR5), RETURNING, ON EXCEPTION /
> NOT ON EXCEPTION, CANCEL (with §14.9.5 re‑init), ENTRY, COMMON/INITIAL, nested programs and recursion
> are all live and exercised by the NIST IC‑series + unit/integration suites (guard **1196 unit / 509
> integration / 364 NIST**, M1 COBOL‑85 COMPLETE). Verify any specific detail against `src/` before
> relying on it — this essay predates the current implementation and several of its mechanism details are
> **superseded** (see "Corrections vs the implemented design" at the end).
>
> **Stack: .NET 10 / C# 14.** Backend is **CIL‑only via Mono.Cecil — there is NO custom VM and NO bytecode
> interpreter.** (A Roslyn C# backend is a *future additive* option, Stage‑5, with Cecil as the oracle.)
>
> **Pointers / argument passing use the single `ManagedPointer` carrier** (GC‑tracked managed ref; renamed
> from `CobolDataPointer`; NO 8‑byte handle, NO PointerRegistry). A CALL's USING arguments lower to a
> `ManagedPointer[]`; the callee entry signature is `static int Entry(ManagedPointer[] args)`.
>
> **Plan SSOT: `docs/MASTER_PLAN.md`. Doctrine: `PROMPT.md`.** Data‑model context (the byte/StorageBlock
> engine being islanded in favor of typed‑native fields): `docs/DATA_MODEL_ARCHITECTURE.md`,
> `docs/RECORD_STRUCT_STORAGE_DESIGN.md`.
>
> *Consolidated from 3 prior docs, 2026-06-07:* "CALL Convention, Parameter Passing, LINKAGE & BY VALUE/BY
> REFERENCE Architecture" (canonical base), "Program Model — CALL, ENTRY, Parameter Passing & Multi‑Module
> Architecture", and "Program Linking, CALL/RETURN & Multi‑Module Architecture".

Purpose
-------
Define the authoritative architecture for COBOL inter‑program communication in CobolSharp:
- CALL literal and CALL identifier (static vs dynamic resolution)
- Parameter passing semantics: BY REFERENCE, BY CONTENT, BY VALUE
- LINKAGE SECTION mapping
- RETURNING semantics
- ENTRY points and multi‑entry programs
- Program activation / deactivation (CALL stack model)
- COMMON vs INITIAL WORKING‑STORAGE; CANCEL
- Nested program activation and multi‑module linking
- Recursive CALLs
- CIL‑friendly lowering
- AOT/WASM‑safe invocation

This document governs how CobolSharp implements COBOL’s CALL conventions, program model, and inter‑module
execution on .NET.

------------------------------------------------------------
SECTION 1 — CALL & PROGRAM-MODEL OVERVIEW
------------------------------------------------------------

CobolSharp supports:
- CALL "ProgramName" (static — name known at compile time, fastest path)
- CALL identifier (dynamic — name computed at runtime, resolved via the program registry)
- CALL with USING parameters and RETURNING result
- Nested CALLs, recursive CALLs, multi‑ENTRY dispatch
- CANCEL "ProgramName" / CANCEL identifier

A COBOL program in CobolSharp consists of the four divisions (IDENTIFICATION / ENVIRONMENT / DATA /
PROCEDURE) plus optional ENTRY points, and compiles to a **.NET class** with a static entry method (and
optional ENTRY methods).

CALL activates:
- A new program instance with its own ExecutionContext
- Its own WORKING‑STORAGE (unless COMMON), fresh LOCAL‑STORAGE every activation
- A mapped LINKAGE SECTION
- A new PERFORM stack and a new ExceptionState

Execution is deterministic, single‑threaded, and non‑preemptive.

------------------------------------------------------------
SECTION 2 — PARAMETER PASSING MODES
------------------------------------------------------------

The BY REFERENCE / BY CONTENT / BY VALUE phrase is **transitive** (ISO §14.8 CALL, general rule 5): it
applies to every argument that follows it until another such phrase appears. Before any phrase the default
is **BY REFERENCE**. The binder (`CallBinder.BindCall`) tracks the current `ParameterMode` and lets bare
arguments inherit the most recent explicit mode.

2.1 BY REFERENCE (default)
--------------------------
Caller passes a managed reference to the caller’s data region; the callee’s LINKAGE SECTION item overlays
the same storage.
Effects: mutations visible to caller; no copying; fastest mode.

2.2 BY CONTENT
--------------
Caller passes a copy of the data; the callee receives an independent buffer.
Effects: mutations NOT visible to caller; copy created at CALL time and discarded at RETURN.

2.3 BY VALUE
------------
Caller passes a primitive value (numeric); the callee receives the value, not a reference to caller storage.
Effects: no shared memory; used for numeric parameters. (Originally documented as "OO only"; the
implementation binds BY VALUE arguments from an arithmetic expression — `CallBinder` BindAdditiveExpression.)

2.4 Mixed modes
---------------
    CALL "P" USING BY REFERENCE A BY CONTENT B BY VALUE C.

Each argument is bound with its own `BoundCallArgument(ParameterMode, expr)`; the emitter produces the
appropriate per‑argument `ManagedPointer` (reference / by‑content copy) or by‑value encoding.

------------------------------------------------------------
SECTION 3 — LINKAGE SECTION ARCHITECTURE
------------------------------------------------------------

3.1 LINKAGE SECTION fields represent the parameters passed to the program: BY REFERENCE references,
BY CONTENT copies, BY VALUE primitives.

3.2 Mapping rules — each USING parameter maps, in declared order, to a LINKAGE SECTION item:
- BY REFERENCE → reference to the caller’s storage (same offset/length/PIC)
- BY CONTENT → a fresh buffer holding a copy of the caller’s data
- BY VALUE → a local value (no shared storage)

3.3 LINKAGE SECTION lifetime — allocated at program activation, released at termination; BY CONTENT
buffers are freed automatically (GC‑reclaimed).

------------------------------------------------------------
SECTION 4 — RETURNING SEMANTICS
------------------------------------------------------------

4.1 RETURNING value
    CALL "P" USING A B RETURNING R.
The callee produces the result; the caller stores it into R. (Implementation: the RETURNING item is bound
as a `BoundIdentifierExpression` target and, for user‑defined functions, passed as a trailing
BY‑REFERENCE scratch buffer that the callee writes through — see `CobolProgramRegistry.InvokeNumericFunction`.)

4.2 RETURNING types — DISPLAY, NATIONAL, numeric, object reference (OO COBOL). The RETURNING item must be
**elementary** (BY VALUE semantics); a group RETURNING item is a compile‑time error.

4.3 RETURNING with BY REFERENCE — allowed; RETURNING is independent of USING.

4.4 RETURNING with no caller target — value discarded.

4.5 GOBACK RETURNING x — sets the program’s return value and deactivates (see DEVLOG: `GOBACK RETURNING`
implemented under M2‑PROC‑6).

------------------------------------------------------------
SECTION 5 — PROGRAM ACTIVATION & DEACTIVATION MODEL
------------------------------------------------------------

5.1 Activation steps for `CALL "P" USING a b c`:
1. Resolve program P (`CobolProgramRegistry.Resolve`)
2. Create a new ExecutionContext
3. Allocate WORKING‑STORAGE (unless COMMON), LOCAL‑STORAGE (always fresh)
4. Map the LINKAGE SECTION to the USING parameters
5. Initialize subsystems (FileManager, engines)
6. Dispatch the ENTRY point and execute the PROCEDURE DIVISION

5.2 Deactivation occurs on GOBACK, EXIT PROGRAM, or end of PROCEDURE DIVISION:
- Restores the caller’s ExecutionContext and PERFORM stack
- Returns the RETURNING value, if any

5.3 COMMON / non‑COMMON / INITIAL WORKING‑STORAGE
- **COMMON**: WORKING‑STORAGE allocated once and retained across activations (static‑storage semantics);
  not reinitialized.
- **non‑COMMON (default)**: a normal program’s WORKING‑STORAGE persists in its last‑used state between
  CALLs per ISO §14.6.2.3.2 — it is NOT reinitialized on a plain re‑CALL.
- **INITIAL**: WORKING‑STORAGE is reset to its VALUE state on every activation.
- **After CANCEL**: the next CALL finds the program in its initial state (§14.9.5 GR3); see Section 6.

> Note: the prior essays stated "default program is COMMON". That is **incorrect** and corrected here —
> COBOL programs are non‑COMMON, non‑INITIAL by default. COMMON only affects *nested‑program visibility*,
> not storage persistence.

------------------------------------------------------------
SECTION 6 — CANCEL
------------------------------------------------------------

`CANCEL "P"` (or `CANCEL identifier`) severs the program’s logical relationship to the run unit; the next
CALL must find it in its initial state (ISO §14.9.5). Canceling a program that was never called or is
already canceled has no effect (GR7).

Implementation (`CobolProgramRegistry.Cancel`): removes the program from the active registry and sets a
**reinit flag**. The program’s Entry method consumes that flag on its next activation
(`ConsumeReinitFlag`), reinitializing WORKING‑STORAGE; a normal CALL leaves storage in its last state.

------------------------------------------------------------
SECTION 7 — CALL STACK MODEL & RECURSION
------------------------------------------------------------

7.1 CALL frame — caller ExecutionContext, return target, RETURNING target, parameter descriptors.

7.2 GOBACK — pops the CALL frame and restores the caller ExecutionContext.

7.3 STOP RUN — clears the entire CALL stack and terminates the run unit. (Implicit CLOSE of open files at
run‑unit termination is handled by the file subsystem.)

7.4 Recursion — allowed. Each activation gets its own ExecutionContext and fresh LOCAL‑STORAGE.
- COMMON recursion: WORKING‑STORAGE is shared across recursive activations (deterministic; caller/callee
  share state).
- INITIAL recursion: each activation gets a fresh WORKING‑STORAGE image.

------------------------------------------------------------
SECTION 8 — ENTRY POINTS
------------------------------------------------------------

8.1 Syntax: `ENTRY "AltName" USING x y z.` A program may declare multiple ENTRY names, each with its own
USING signature. (Bound by `CallBinder.BindEntry` → `BoundEntryStatement(name, usingNames)`.)

8.2 Dispatch: `CALL "AltName"` dispatches to `ENTRY "AltName"`. A CALL to a program that declares no ENTRY
targets the PROCEDURE DIVISION header as the implicit entry.

8.3 WORKING‑STORAGE is shared across all ENTRY points of a program.

------------------------------------------------------------
SECTION 9 — CALL IDENTIFIER RESOLUTION & MULTI‑MODULE LINKING
------------------------------------------------------------

9.1 CALL identifier — evaluate the identifier at runtime, look the name up via the program registry, then
apply the same USING/RETURNING rules as CALL literal.

9.2 Program registry (`CobolProgramRegistry`, `src/CobolSharp.Runtime`)
- A static, case‑insensitive `Dictionary<string, CobolProgramEntry>` mapping PROGRAM‑ID → entry delegate.
- Programs register themselves at startup via `Register`.
- `Resolve(name)` checks the registry, then **auto‑discovers** a matching type (a `static int
  Entry(ManagedPointer[])` method) in loaded assemblies or a `<name>.dll` in the application directory.

9.3 Static vs dynamic linking
- CALL literal → bound to a known class; fastest, no runtime lookup.
- CALL identifier → runtime lookup; an unresolved name triggers **ON EXCEPTION** (or a runtime error if no
  ON EXCEPTION phrase). Programs may live in the same assembly, separate assemblies, or external libraries.

> Note: the prior essays claimed CobolSharp "forbids reflection‑based loading / dynamic CALL with a
> runtime‑computed name" and "all bindings static". The **implemented** registry deliberately supports
> dynamic CALL via reflective auto‑discovery (`DiscoverProgram` / `FindEntryInAssembly`). The AOT/WASM
> section below is the *constrained‑deployment* profile, not the general model.

------------------------------------------------------------
SECTION 10 — NESTED PROGRAMS
------------------------------------------------------------

    PROGRAM-ID. Outer.
        PROGRAM-ID. Inner.
        END PROGRAM Inner.
    END PROGRAM Outer.

- `CALL "Inner"` creates a new ExecutionContext; shares WORKING‑STORAGE only where COMMON applies; has
  independent LOCAL‑STORAGE.
- Visibility: inner programs may access outer COMMON/GLOBAL WORKING‑STORAGE but never the outer
  LOCAL‑STORAGE. (Nested‑program scoping is handled by the binder’s nested‑program pass.)

------------------------------------------------------------
SECTION 11 — CIL LOWERING RULES
------------------------------------------------------------

11.1 CALL lowering (`CilEmitter` / `CilExpressionEmitter`)
- Evaluate each argument and materialize a `ManagedPointer[]` (one element per USING argument).
- Static CALL literal → direct call into the target class’s `Entry`.
- Dynamic CALL identifier → `CobolProgramRegistry.Resolve(name)?.Invoke(args)`; null result → ON EXCEPTION.

11.2 Per‑argument USING lowering
- BY REFERENCE → `ManagedPointer.CreateByReference(buffer, offset, length)` over the caller’s storage.
- BY CONTENT → allocate a fresh buffer, copy the caller’s bytes, wrap as a `ManagedPointer`.
- BY VALUE → encode the primitive value into a fresh buffer (`EncodeFunctionArg`‑style) / pass the value.

11.3 RETURNING lowering — the callee writes its result; the caller stores it into the RETURNING target.

11.4 ENTRY lowering — `ENTRY "X"` compiles to a static method reachable through the registry; the program
itself exposes `public static int Entry(ManagedPointer[] args)`.

11.5 EXIT PROGRAM / GOBACK lowering — set the return value, restore the caller ExecutionContext, branch to
the return label.

> Mechanism note: the essays variously described `Prog.Main(ctx, args)`, `Entry_name(ctx, …)`, a
> `ParameterDescriptor[]`, and "offset tables". The **implemented** ABI is a single uniform
> `static int Entry(ManagedPointer[] args)` taking a `ManagedPointer[]` (return code: 0 = normal).

------------------------------------------------------------
SECTION 12 — DEBUGGER INTEGRATION
------------------------------------------------------------

The debugger (design‑only, Phase E) surfaces: the CALL stack, current program name and ENTRY point, USING
parameters, BY REFERENCE references / BY CONTENT copies / BY VALUE primitives, LINKAGE SECTION mapping,
RETURNING value, and COMMON/INITIAL state. Sequence points are emitted for CALL, ENTRY, RETURNING,
EXIT PROGRAM, and GOBACK.

------------------------------------------------------------
SECTION 13 — AOT/WASM‑SAFE CALL MODEL (constrained‑deployment profile)
------------------------------------------------------------

For AOT/WASM targets the dynamic profile is restricted to stay trimming/AOT‑safe:
- **No reflective discovery** — program lookup uses the prebuilt static registry only (no `Type.GetType` /
  no reflective auto‑load).
- **No dynamic codegen** — all ENTRY methods are compiled ahead of time.
- **No unsafe code** — argument passing is via `ManagedPointer` (managed references over `byte[]` /
  typed‑native fields), never raw pointers.
- **Deterministic** — CALL/activation semantics are identical across CoreCLR, AOT, and WASM.

------------------------------------------------------------
SECTION 14 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

- Too many USING parameters → compile‑time error (PARAMETER‑MISMATCH).
- Too few USING parameters → diagnostic; missing parameters treated as uninitialized.
- CALL to a missing program (dynamic) → ON EXCEPTION (or runtime error if no phrase).
- BY REFERENCE to overlapping fields → allowed; callee sees a live view.
- BY REFERENCE with a mismatched PIC → runtime behavior per overlay (no copy).
- BY CONTENT of an OCCURS DEPENDING ON item → copies the maximum size; callee honors the DEPENDING‑ON value.
- RETURNING with a group item → compile‑time error.
- RETURNING with no caller target → value discarded.
- Recursive COMMON program modifying shared state → allowed; deterministic.
- CALL inside a declarative → allowed; a new ExecutionContext is created.
- CANCEL of a never‑called / already‑canceled program → no effect (§14.9.5 GR7).

------------------------------------------------------------
SECTION 15 — CORRECTIONS vs THE IMPLEMENTED DESIGN (provenance)
------------------------------------------------------------

The three source essays predate the current implementation. The following of their claims were **corrected**
during consolidation (verify against `src/` for the authoritative behavior):

1. **Entry ABI** — not `Prog.Main(ctx,args)` / `Entry_X(ctx,…)`; the uniform ABI is
   `static int Entry(ManagedPointer[] args)`.
2. **Argument carrier** — not "StorageBlock pointer + offset table" or `ParameterDescriptor[]`; arguments
   are a `ManagedPointer[]` (the single managed‑ref carrier; no 8‑byte handle, no PointerRegistry).
3. **Default program persistence** — default is non‑COMMON / non‑INITIAL; WORKING‑STORAGE *persists* between
   plain CALLs (§14.6.2.3.2), it is NOT reinitialized. The essays’ "default is COMMON" was wrong.
4. **Dynamic linking** — the registry **does** support runtime‑computed names via reflective
   auto‑discovery; the "no reflection / static only" statements describe only the AOT/WASM profile (§13).
5. **BY VALUE** — not "OO only"; bound generally from an arithmetic expression.
6. **CANCEL** — implemented (reinit‑flag model, §14.9.5); the essays under‑specified it.
7. **Stack** — .NET 10 / C# 14, CIL‑only via Mono.Cecil (no custom VM); any "net9.0 / C# 13 / interpreter"
   phrasing inherited from the essays is stale.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp CALL / Program Model:
- Implements full COBOL CALL, USING, RETURNING, ENTRY, CANCEL, and multi‑module semantics
- Supports BY REFERENCE, BY CONTENT, and BY VALUE with transitive‑mode binding (ISO §14.8 GR5)
- Provides COMMON/INITIAL persistence, CANCEL re‑init, nested programs, and safe recursion
- Uses a static + reflectively‑augmented `CobolProgramRegistry` for static and dynamic linking
- Lowers to a uniform `static int Entry(ManagedPointer[])` ABI and clean, verifiable, debugger‑friendly CIL
- Ensures deterministic behavior across CoreCLR, AOT, and WASM
