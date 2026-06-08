CobolSharp COBOL Runtime — ExecutionContext, StorageBlocks, ObjectTable, FileManager & Engine Integration Architecture (CIL‑Only)
===============================================================================================================================

> **STATUS BANNER (2026-06-07).** This is a **design-reference / target-architecture** document for the CobolSharp
> runtime, **consolidated from 5 prior runtime essays** (see *Provenance* below). It is an aspirational unified model,
> **not** a description of the code as built. **Actual implementation status (verified against `src/CobolSharp.Runtime/`):
> the runtime is largely implemented but does NOT match this essay's `ExecutionContext` shape.** Reality:
> - There is **no `ExecutionContext` class** and no `*Engine` instance objects. Generated programs subclass
>   **`CobolProgram`** (`src/CobolSharp.Runtime/CobolProgram.cs`) with **static** runtime helpers; per-program state is a
>   **`ProgramState`** holding three `byte[]` areas — `WorkingStorage` / `FileSection` / `LocalStorage`
>   (`src/CobolSharp.Runtime/StorageArea.cs`). The "engines" below are real subsystems but are realized as static
>   facades / helper classes, not fields of a context object: `FileRuntime` → `CobolFileManager` + `IFileHandler`
>   (`Sequential`/`Indexed`/`Relative` handlers), `SortRuntime`, `InspectRuntime`, `ReportWriterRuntime`,
>   `PicRuntime`, `AcceptRuntime`, `IntrinsicFunctions`, `ExternalStorage`, `GlobalUseDeclarativeRegistry`,
>   `SwitchRuntime`, the Screen/Terminal subsystem. **Read every "ExecutionContext / Engine" reference below as
>   *conceptual* — substitute `CobolProgram` + `ProgramState` + static runtime facades.**
> - **File I/O, SORT/MERGE, INSPECT/STRING/UNSTRING, numeric/PIC, REPORT WRITER, declaratives/USE, intrinsics (94),
>   ACCEPT/DISPLAY, EXTERNAL storage** are **implemented (~production)**. **JSON/XML PARSE/GENERATE** runtime is
>   **DESIGN-ONLY** (grammar overlay only — Phase C). **Debugger/PDB inspection, AOT/WASM publishing** are
>   **DESIGN-ONLY** (Phase E). `SortRuntime` is **in-memory** (external merge sort deferred), not the "external merge
>   sort" claimed below.
> - **Pointers / OO objects:** there is **no integer-indexed `ObjectTable`** (Section 3 below is design-only/superseded).
>   Pointers are a **single managed carrier — `ManagedPointer`** (`src/CobolSharp.Runtime/ManagedPointer.cs`; renamed
>   from `CobolDataPointer`); **no 8-byte handle, no PointerRegistry** — settled. OO objects map to **.NET classes**
>   (grammar done; semantic/emit pending), not an index table.
> - **Data model:** the byte/`StorageBlock` storage model described here is **being SUPERSEDED-in-part** by the
>   **typed-native** data model (`PIC X`→`string`, numeric→`long`/`decimal`, groups→`record struct`, `OCCURS`→`T[]`,
>   pointers→`ManagedPointer`), gated behind **`EnableTypedFields` (default OFF)**, CORE done through Stage-4; the
>   byte/`StorageBlock` engine is being **islanded** as the permanent fallback floor. See the LIVE design docs:
>   `docs/DATA_MODEL_ARCHITECTURE.md` (ADR), `docs/RECORD_STRUCT_STORAGE_DESIGN.md` (staged build),
>   `docs/DATA_MODEL_REVIEW.md` (review).
> - **Stack:** **.NET 10 / C# 14**. **Backend = CIL-only via Mono.Cecil** — there is **NO custom VM / bytecode
>   interpreter**; a Roslyn C# backend is a *future additive* option (Stage-5, Cecil = oracle).
> - **Plan SSOT:** `docs/MASTER_PLAN.md`. **Doctrine:** `PROMPT.md`. Guard at last update: **1196 unit / 509
>   integration / 364 NIST.**
>
> **Provenance — consolidated 2026-06-07 from:**
> - *CobolSharp COBOL Runtime Engine, ExecutionContext & Subsystem Integration Architecture.md*
> - *CobolSharp COBOL Runtime Execution Model & ExecutionContext Architecture.md*
> - *CobolSharp COBOL Runtime ExecutionContext, Storage & Engine Integration Architecture.md*
> - *CobolSharp Runtime Library Design.md*
> - (this file — the most complete member, kept as canonical)

Purpose
-------
Define the **target** architecture for:
- ExecutionContext lifecycle (conceptual; today = `CobolProgram` + `ProgramState`)
- StorageBlocks (WORKING‑STORAGE, LOCAL‑STORAGE, LINKAGE, FD buffers)
- ObjectTable (COBOL object references) — *design-only; superseded by .NET classes + `ManagedPointer`*
- FileManager integration
- Subsystem engines (NumericEngine, StringEngine, JsonEngine, XmlEngine, SortEngine, ReportEngine, ConsoleEngine,
  DateTimeEngine, CollationEngine, ExceptionEngine, IntrinsicFunctionLibrary)
- CALL stack and PERFORM stack integration
- Declaratives and ExceptionState routing
- Program activation, nested program contexts, ENTRY points
- Program registry, runtime initialization/shutdown
- AOT/WASM‑safe runtime design
- CIL‑friendly lowering, debugger integration

This document governs how CobolSharp **intends** to execute COBOL programs at runtime. (For the implemented shape, see
the banner.)

------------------------------------------------------------
SECTION 1 — EXECUTIONCONTEXT OVERVIEW
------------------------------------------------------------

ExecutionContext is the central runtime object containing:
- StorageBlocks for all data divisions
- ObjectTable for OO and .NET interop *(superseded — see banner: OO → .NET classes; pointers → `ManagedPointer`)*
- FileManager instance
- Subsystem engines
- CALL stack
- PERFORM stack
- ExceptionState
- Program registry reference
- Runtime flags (debug, tracing, breakpoints)
- Random number generator state (see Section 9)
- Report state (see Section 10)
- Program parameters / Environment descriptor (command‑line args, environment variables)

Each program activation receives its own ExecutionContext (unless COMMON WORKING‑STORAGE is shared).

> **As-built mapping.** Generated program ⇒ a class subclassing `CobolProgram`; per‑activation state ⇒ a static
> `ProgramState State` field (`CilProgramStateEmitter.EmitProgramState`) = three `byte[]` areas. "Engines" ⇒ static
> runtime facades. "ExecutionContext passed to every method" ⇒ generated methods are static methods on the program
> class operating on `State` + static facades.

------------------------------------------------------------
SECTION 2 — STORAGEBLOCK ARCHITECTURE
------------------------------------------------------------

2.1 StorageBlock structure
--------------------------
StorageBlock contains:
- byte[] Buffer
- FieldOffset[] table
- FieldMetadata[] table
- OCCURS metadata
- REDEFINES metadata

> **As-built:** `ProgramState` (three `byte[]`) + a `StorageLocation` `(Area, Offset, Length, PicDescriptor)` quad per
> `DataSymbol` computed by `StorageLayoutComputer`; `PicRuntime`/`StorageHelpers` slice `area[offset..offset+length]`.
> The typed-native migration replaces flipped items with native fields of an emitted `record struct` — see
> `docs/RECORD_STRUCT_STORAGE_DESIGN.md`.

2.2 StorageBlock types / storage regions
----------------------------------------
- WORKING‑STORAGE block — global data
- LOCAL‑STORAGE block
- LINKAGE block
- FD record buffers
- Temporary blocks (SORT, JSON, XML)

2.3 Allocation rules
--------------------
WORKING‑STORAGE:
- Allocated once per program activation
- Reinitialized if INITIAL
- Lives for entire program execution
- Allocated **once per program instance unless COMMON** (then shared across activations)

LOCAL‑STORAGE:
- Allocated on each program invocation / ENTRY
- Reinitialized (cleared) on each call; cleared on exit

LINKAGE:
- Allocated based on USING parameters
- Backed by caller‑provided memory (BY REFERENCE) or a copy (BY CONTENT)
- Mapped to a StorageBlock via marshaling

FD buffers:
- Allocated on OPEN
- Each file descriptor has: record buffer, key buffer, file status variable, runtime handle

2.4 Field access API (from *Runtime ExecutionContext, Storage & Engine* essay)
-----------------------------------------------------------------------------
StorageBlock provides:
- GetBytes(offset, length) / SetBytes(offset, length)
- GetString(offset, length) / SetString(offset, length)
- GetPackedDecimal(offset, digits, scale) / SetPackedDecimal(offset, digits, scale)
- GetBinary(offset, width) / SetBinary(offset, width)

Supports PIC X, PIC N, COMP, COMP‑3, COMP‑5.

2.5 REDEFINES
-------------
Multiple fields share the same offset. StorageBlock treats overlapping fields as raw bytes.

2.6 OCCURS
----------
Arrays represented as contiguous memory; `offset = base + index * elementSize`.

2.7 OCCURS DEPENDING ON
-----------------------
Logical length checked / validated at runtime; raw storage always preserved.

2.8 RENAMES
-----------
Synthetic field ranges (from *Runtime Library Design*).

------------------------------------------------------------
SECTION 3 — OBJECTTABLE ARCHITECTURE  *(DESIGN-ONLY — SUPERSEDED)*
------------------------------------------------------------

> **SUPERSEDED.** This integer-index ObjectTable model is **not** how OO/pointers are implemented. OO objects map to
> **.NET classes** (grammar done, semantic/emit pending — see `docs/OO_IMPLEMENTATION_DESIGN.md`); data pointers use
> the single managed carrier **`ManagedPointer`** (no handle table, no index, no `PointerRegistry`). The historical
> design text is retained below only for reference.

3.1 Purpose *(historical)*
--------------------------
Stores: COBOL OO objects, .NET objects returned from INVOKE, NEW object instances.

3.2 Structure *(historical)*
----------------------------
ObjectTable contains: `List<object>` references, a free list for reuse, a null slot at index 0.

3.3 Reference model *(historical)*
----------------------------------
COBOL object reference = integer index into ObjectTable. *(Replaced by direct managed references.)*

3.4 Lifetime *(historical)*
---------------------------
Objects remain alive until: program terminates, explicitly set to null, or garbage collected by .NET.
*(In the as-built model, OO/pointer references are GC-tracked managed refs directly — no table.)*

------------------------------------------------------------
SECTION 4 — FILEMANAGER INTEGRATION
------------------------------------------------------------

> **As-built (implemented):** `FileRuntime` (static facade) → `CobolFileManager` + `IFileHandler` with
> `SequentialFileHandler` / `IndexedFileHandler` / `RelativeFileHandler` (`src/CobolSharp.Runtime/IO/`). File status
> codes, locking/sharing, START positioning, variable-length records, and the COBOL declaratives integration are real.

4.1 FileManager responsibilities
--------------------------------
- Open/close files
- Read/write/rewrite/delete
- Indexed/relative/sequential access
- File status codes
- Locking and sharing
- START positioning

4.2 ExecutionContext integration
--------------------------------
ExecutionContext contains: FileManager instance, FD metadata, record buffers.

4.3 FD binding
--------------
On OPEN: FileManager binds FD to file handle, allocates record buffer, initializes cursor.

4.4 FileHandle model (from *Runtime Library Design*)
---------------------------------------------------
FileHandle:
- FileId
- Organization (Sequential, Indexed, Relative)
- AccessMode (Input, Output, I‑O, Extend)
- RecordLength
- RecordBuffer (StorageBlock)
- StatusField (reference to COBOL FILE STATUS)
- Backend: IFileBackend / IFileHandler

APIs: Open, Close, ReadNext / ReadPrevious, ReadKey, Write, Rewrite, Delete, Start, Return, Release.

Backends:
- SequentialFileBackend (System.IO)
- IndexedFileBackend (B+‑tree or database)
- RelativeFileBackend (record‑indexed)

------------------------------------------------------------
SECTION 5 — SUBSYSTEM ENGINES
------------------------------------------------------------

> **As-built mapping.** "Engines" are static runtime facades/helpers, not context-held instances. The listed numeric
> engine semantics now live across `PicRuntime` + the typed substrate (`CobolNum`/`CobolDecimal`); string ops in
> `InspectRuntime` + `StringLowerer`/`CilStringEmitter`; sort in `SortRuntime` (in-memory); report in
> `ReportWriterRuntime`; console/date in `AcceptRuntime` + `IntrinsicFunctions`. JSON/XML engines are **design-only**.

ExecutionContext (conceptually) contains the following engines:

5.1 NumericEngine
-----------------
- Decimal arithmetic
- Packed decimal encoding/decoding (COMP‑3)
- Binary and display numeric arithmetic
- COMP/COMP‑3/COMP‑5 conversion
- ROUNDED logic
- SIZE ERROR / overflow detection
- Sign handling
- APIs: Add/Subtract/Multiply/Divide, Compare, Convert (PIC/USAGE conversions), Pack/Unpack decimal.
- *Typed-substrate note:* signed-scaled → `decimal` (`CobolDecimal`), unsigned-int → `long` (`CobolNum`); the
  packed-decimal byte path is being islanded.

5.2 StringEngine
----------------
- STRING / UNSTRING / INSPECT (TALLYING + REPLACING)
- Concatenation, padding/truncation
- Unicode‑safe slicing; UTF‑8/UTF‑16 bridging
- APIs: StringConcat(List<StringSegment>, PointerDescriptor), Unstring(input, UnstringDescriptor),
  InspectTallying, InspectReplacing.
- Supports delimiters, ALL literal, POINTER and TALLYING, OVERFLOW handling.

5.3 JsonEngine  *(DESIGN-ONLY — Phase C)*
-----------------------------------------
- SAX‑style JSON PARSE, JSON GENERATE, mapping COBOL records ↔ JSON, WITH DETAIL.
- Intended impl: System.Text.Json. *Runtime not yet implemented.*

5.4 XmlEngine  *(DESIGN-ONLY — Phase C)*
----------------------------------------
- SAX‑style XML PARSE, XML GENERATE, namespace resolution, processing procedures, COUNT IN.
- Intended impl: System.Xml. *Runtime not yet implemented.*

5.5 SortEngine
--------------
- USING/GIVING pipeline, INPUT/OUTPUT PROCEDURE callbacks, stable sorting, key comparison via CollationEngine.
- SortContext: Keys, USING files, GIVING files, InputProcedure callback, OutputProcedure callback.
- *As-built:* `SortRuntime` is **in-memory** sort; external merge sort is **deferred** (not yet built).

5.6 ReportEngine
----------------
- REPORT SECTION, page/line control, control‑break logic, accumulators, field formatting.
- *As-built:* `ReportWriterRuntime` — Report Writer is **fully implemented** (see `docs/REPORT_WRITER_CONTROL_DESIGN.md`).

5.7 ConsoleEngine
-----------------
- ACCEPT / DISPLAY, date/time/environment, command‑line arguments.
- *As-built:* `AcceptRuntime` + Screen/Terminal subsystem (`src/CobolSharp.Runtime/Terminal`, `.../Screen`).

5.8 DateTimeEngine  *(from Runtime Library Design / Execution Model essays)*
---------------------------------------------------------------------------
- CurrentDate, CurrentTime, FormatDate, ParseDate, DateDiff (CURRENT-DATE, WHEN-COMPILED).
- *As-built:* covered by `IntrinsicFunctions` (94 intrinsics implemented).

5.9 CollationEngine  *(from Runtime Library Design / Execution Model essays)*
----------------------------------------------------------------------------
- CompareStrings(a, b, CollationDescriptor), SortKey(input, CollationDescriptor).
- Supports Standard ASCII, EBCDIC‑compatible tables, custom collations, national character ordering.
- *As-built:* the program collating sequence is honored across comparisons, SORT/MERGE keys, and FUNCTION CHAR/ORD.

5.10 ExceptionEngine  *(from Runtime Library Design)*
-----------------------------------------------------
- Maps runtime exceptions to ON EXCEPTION / INVALID KEY / AT END.
- APIs: RaiseFileException, RaiseJsonException, RaiseXmlException, RaiseGenericException.
- *As-built:* `CobolRuntimeException` + declarative dispatch (`GlobalUseDeclarativeRegistry`); full ISO EC exception
  model is Phase C.

5.11 IntrinsicFunctionLibrary  *(from Runtime Library Design)*
--------------------------------------------------------------
- COBOL intrinsic functions: NUMVAL, NUMVAL‑C, INTEGER, INTEGER‑PART, FRACTION‑PART, LENGTH, TRIM, LOWER‑CASE,
  UPPER‑CASE, RANDOM, CURRENT‑DATE, WHEN‑COMPILED, …
- API: EvaluateIntrinsic(name, List<Value> args, ExecutionContext).
- *As-built:* `IntrinsicFunctions` — **94/94 implemented and dispatched.**

------------------------------------------------------------
SECTION 6 — CALL STACK ARCHITECTURE
------------------------------------------------------------

6.1 CALL frame / ActivationRecord
---------------------------------
Contains: caller ExecutionContext, return address (IL offset, for stepping/debugging), RETURNING target,
parameter descriptors. (Runtime Library Design names this `ActivationRecord` with: ProgramOrMethod, Locals
(StorageBlock for WS/LS/LINKAGE/OBJECT‑DATA), Parameters (`object[]` BY VALUE/REFERENCE/CONTENT), ReturnAddress,
ExceptionHandlers stack.)

6.2 CALL activation
-------------------
Steps:
1. Lookup program (literal or identifier; runtime lookup for `CALL identifier`)
2. Create new ExecutionContext
3. Allocate StorageBlocks (LOCAL‑STORAGE fresh; share WORKING‑STORAGE if COMMON)
4. Map LINKAGE SECTION to caller
5. Initialize subsystems
6. Dispatch ENTRY

6.3 RETURNING
-------------
Caller receives the RETURNING value from the callee (also `GOBACK RETURNING`).

6.4 GOBACK
----------
- Pops CALL frame
- Restores caller ExecutionContext

6.5 EXIT PROGRAM
----------------
Same as GOBACK; unwinds entire call stack for the current program.

6.6 STOP RUN
------------
- Clears entire CALL stack
- Closes files, flushes reports
- Terminates runtime / process
- *As-built:* `StopRunException` unwinds the run unit.

------------------------------------------------------------
SECTION 7 — PERFORM STACK ARCHITECTURE
------------------------------------------------------------

7.1 PERFORM frame
-----------------
Contains: return label, THRU range, loop variables, loop bounds, TEST BEFORE/AFTER flag, termination condition,
PERFORM type (THRU / UNTIL / VARYING / TIMES).

7.2 Push/pop rules
------------------
- PERFORM → push frame with return label.
- EXIT PERFORM / end of range / EXIT PARAGRAPH / EXIT SECTION → pop frame, branch to return label.
- GOBACK / EXIT PROGRAM → pop all.

7.3 GO TO interactions
----------------------
GO TO may exit a PERFORM range and unwind the PERFORM stack (backend unwinds automatically).

> **As-built note.** The implemented engine uses a symbol-based paragraph-dispatch / return-address model
> (`ControlFlowLowerer` + `CilControlFlowEmitter`) rather than a literal runtime PERFORM-frame object stack;
> PERFORM…THRU inverted ranges and duplicate paragraph names are handled there.

------------------------------------------------------------
SECTION 8 — EXCEPTIONSTATE ARCHITECTURE
------------------------------------------------------------

8.1 ExceptionState fields
-------------------------
- Category (SIZE ERROR, I/O ERROR, JSON ERROR, XML ERROR, .NET EXCEPTION, INVALID KEY, AT END, STANDARD EXCEPTION,
  FILE ERROR, RUNTIME ERROR)
- Message
- Source subsystem
- Raw token (JSON/XML)
- File name (I/O)
- Key value (indexed)
- Numeric overflow metadata
- Stack trace / raw exception object (optional)

8.2 Routing
-----------
1. ON EXCEPTION (and INVALID KEY / AT END inline phrases)
2. USE AFTER EXCEPTION ON file/json/xml
3. USE AFTER STANDARD EXCEPTION

If none of the inline phrases match, dispatch the declarative (Section "Declarative dispatch"). If no handler exists
→ propagate to caller; if unhandled → throw a .NET exception.

8.3 Reset
---------
ExceptionState cleared:
- After ON EXCEPTION block
- After declarative completes
- After a successful operation / before the next statement

------------------------------------------------------------
SECTION 8b — DECLARATIVE HANDLER INTEGRATION  *(from ExecutionContext, Storage & Engine essay)*
------------------------------------------------------------

Registration: ExecutionContext stores USE AFTER EXCEPTION / USE AFTER ERROR / USE AFTER STANDARD EXCEPTION handlers.
Invocation: on exception, the local handler is checked first; if none, the declarative is invoked; execution resumes
after the failing statement. Declaratives may re‑enter, nest, and trigger other declaratives; an exception inside a
declarative is routed to the STANDARD EXCEPTION declarative. A GOBACK inside a declarative returns from the program,
not the declarative.

> **As-built:** declaratives / USE AFTER are implemented; cross-program GLOBAL USE dispatch goes through
> `GlobalUseDeclarativeRegistry`. The full ISO exception-condition (EC) model is Phase C.

------------------------------------------------------------
SECTION 9 — PROGRAM REGISTRY & RANDOM NUMBER GENERATOR
------------------------------------------------------------

9.1 Program registry
--------------------
Maps program names → .NET types and ENTRY names → methods. Built at **compile time** (no reflection at runtime).
Lookup: `CALL "P"` → `registry["P"]`.
- *As-built:* `CobolProgramRegistry`.

9.2 Random number generator
---------------------------
ExecutionContext contains a deterministic PRNG, seeded by RANDOM‑SEED or a default; the RANDOM function uses it.

------------------------------------------------------------
SECTION 10 — REPORT STATE & RUNTIME INITIALIZATION
------------------------------------------------------------

10.1 Report state  *(from ExecutionContext, Storage & Engine essay)*
--------------------------------------------------------------------
ExecutionContext stores report definitions, page counters, line counters, and control‑break state. ReportEngine
writes to FileManager or a DISPLAY target.

10.2 Runtime startup
--------------------
- Create root ExecutionContext
- Load program registry
- Initialize ConsoleEngine and FileManager
- Call main program

10.3 Runtime shutdown
---------------------
- Close all files
- Flush reports
- Clear ObjectTable *(superseded — no table; GC handles managed refs)*
- Dispose engines

------------------------------------------------------------
SECTION 11 — CIL LOWERING RULES
------------------------------------------------------------

11.1 ExecutionContext lowering
------------------------------
Compiler generates: `ctx = new ExecutionContext(programId)`. *(As-built: a `ProgramState State` static field is
initialized per `CilProgramStateEmitter`; generated methods are static methods on the program class.)*

11.2 StorageBlock lowering
--------------------------
Compiler generates `new StorageBlock(size)` + a FieldOffset table + a metadata table. *(As-built: byte areas +
`StorageLocation` quads; flipped items become `record struct` fields under `EnableTypedFields`.)*

11.3 Engine / storage / method-signature conventions
----------------------------------------------------
- Subsystem calls lower to `callvirt ctx.Engine.Method` *(as-built: static calls into runtime facades,
  e.g. `FileRuntime.ReadNext`, `InspectRuntime.…`, `IntrinsicFunctions.…`)*.
- The *Execution Model* essay specified `void MethodName(ExecutionContext ctx)` signatures and storage accessors
  (`ctx.Storage.GetString/GetBinary/GetPackedDecimal`); as-built these are `ProgramState` + `PicRuntime` slices.
- Branching: IF / EVALUATE / PERFORM lower to `brtrue`/`brfalse`, `switch`, and structured loops.
- Exception lowering: ON EXCEPTION → wrap block in try/catch, set exception state, branch to handler.

11.4 ObjectTable lowering *(superseded)*
----------------------------------------
Historical: object reference stored as `int` index. *(As-built: direct managed reference / `ManagedPointer`.)*

11.5 CALL lowering
------------------
`new ExecutionContext` + call the Entry method. *(As-built: emit a call to the callee program class’s entry; share
WS if COMMON; map LINKAGE.)*

------------------------------------------------------------
SECTION 12 — DEBUGGER INTEGRATION  *(DESIGN-ONLY — Phase E)*
------------------------------------------------------------

Intended debugger surface (via PDB sequence points):
- All StorageBlocks (decoded — PIC/USAGE values, OCCURS arrays, REDEFINES overlays)
- ObjectTable contents *(superseded)*
- FileManager state
- CALL stack and PERFORM stack
- ExceptionState
- Engine state (JSON/XML/SORT/REPORT)
- Current paragraph/section, current PERFORM/CALL frame

> Status: design-only. Sequence points are emitted, but the interactive debugger / storage-inspection tooling is not
> built.

------------------------------------------------------------
SECTION 13 — AOT/WASM‑SAFE RUNTIME DESIGN  *(DESIGN-ONLY publishing — Phase E)*
------------------------------------------------------------

13.1 No reflection / no dynamic codegen
---------------------------------------
All binding static; no Reflection.Emit, no DynamicMethod, no dynamic IL. (The CobolSharp **compiler** emits CIL via
Mono.Cecil at build time; the **runtime** is pure managed code.)

13.2 No unsafe code
-------------------
No raw pointers, no `stackalloc`, no unmanaged memory. (Data pointers use the managed `ManagedPointer` — no native
heap, no handle table, no `unsafe`.)

13.3 Deterministic, platform‑neutral behavior
---------------------------------------------
- FileManager uses Stream abstractions; no OS‑specific syscalls or OS‑level file locks.
- No platform‑specific APIs, no JIT‑dependent optimizations.
- Intended to be identical across CoreCLR, .NET AOT, and .NET WASM.

> Status: the runtime is written to be AOT/WASM-compatible, but the AOT/WASM **publish pipeline** is not yet a
> validated product surface (Phase E).

------------------------------------------------------------
SECTION 14 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

14.1 CALL inside declarative — allowed; new ExecutionContext created.
14.2 File left open on STOP RUN — runtime closes automatically.
14.3 Object reference to disposed .NET object — runtime error. *(model-dependent; OO via .NET classes)*
14.4 StorageBlock overflow — impossible; bounds checked.
14.5 Recursive CALL with COMMON WORKING‑STORAGE — WS shared across activations; non-COMMON gets its own.
14.6 Nested PERFORM with GO TO — allowed; PERFORM stack must unwind correctly.
14.7 EXIT PROGRAM inside nested calls — unwinds entire call chain; clears PERFORM stack.
14.8 STOP RUN inside a CALLed program — terminates the entire process/run unit.
14.9 REDEFINES with OCCURS DEPENDING ON — logical length respected; raw storage always preserved.
14.10 Invalid numeric conversion — SIZE ERROR; no assignment.
14.11 PERFORM VARYING with negative BY — allowed; may create an infinite loop.
14.12 Exception inside declarative — routed to STANDARD EXCEPTION declarative.
14.13 GOBACK inside declarative — returns from program, not declarative.
14.14 JSON/XML exception during cleanup — ignored.
14.15 CALL recursion — allowed; each activation gets its own ExecutionContext.

------------------------------------------------------------
SECTION 15 — RUNTIME-LIBRARY GOALS, PERFORMANCE & TESTING  *(from Runtime Library Design)*
------------------------------------------------------------

15.1 High‑level goals
---------------------
- Provide a complete, deterministic implementation of COBOL semantics for programs compiled to .NET CIL.
- Serve as the *only* runtime target for CobolSharp‑generated assemblies (namespace `CobolSharp.Runtime`).
- Implement all COBOL‑85 → COBOL‑2023 features: packed decimal arithmetic; file I/O (sequential/indexed/relative);
  SORT/MERGE; JSON/XML; STRING/UNSTRING/INSPECT; date/time and environment functions; collation; exception and
  condition handling; OO and generics.
- Fully managed, cross‑platform; compatible with CoreCLR, .NET AOT, .NET WASM.
- Integrate tightly with the CIL backend (runtime methods are public, static, purely managed, fully verifiable).

15.2 Performance strategy
-------------------------
- Packed decimal optimized with lookup tables; file I/O buffered; SORT/MERGE uses efficient algorithms; string
  operations optimized for slicing; collation tables cached; minimal allocations in hot paths.
- *Typed-native note:* the dominant cost in the legacy byte interpreter (ASCII parse → compute → reformat on every
  op) is eliminated for flipped items by storing decoded values — see `docs/DATA_MODEL_ARCHITECTURE.md`.

15.3 Testing strategy
---------------------
- Unit tests per engine; golden tests for numeric/string behavior; file‑I/O conformance tests; JSON/XML round‑trip
  tests; SORT/MERGE correctness tests; cross‑compiler behavior tests (GnuCOBOL / Micro Focus); regression suite.
- *As-built:* the guard runs unit + integration + NIST suites (1196 / 509 / 364 at last update) and post‑'85
  conformance tests in `tests/conformance/<version>/`.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Runtime Architecture (target design):
- Provides a unified runtime model integrating StorageBlocks, FileManager, and subsystem engines (numeric, string,
  JSON/XML, SORT, report, console, date/time, collation, exception, intrinsics).
- Implements CALL/PERFORM stacks, declaratives, and ExceptionState routing with full COBOL semantics.
- Targets deterministic, verifiable, AOT/WASM‑safe execution.
- **As built**, the runtime is realized as `CobolProgram` + `ProgramState` (three `byte[]`) + static runtime facades
  (no `ExecutionContext`/`*Engine` instances, no `ObjectTable`). File I/O, SORT (in‑memory), INSPECT/STRING/UNSTRING,
  numeric/PIC, Report Writer, declaratives/USE, intrinsics (94), ACCEPT/DISPLAY, and EXTERNAL storage are
  implemented; JSON/XML runtime, the interactive debugger, and the AOT/WASM publish pipeline are design‑only. The
  storage model is migrating to typed‑native (`EnableTypedFields`, default OFF), islanding the byte engine. See
  `docs/MASTER_PLAN.md`, `docs/DATA_MODEL_ARCHITECTURE.md`, and `docs/RECORD_STRUCT_STORAGE_DESIGN.md`.
