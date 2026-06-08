CobolSharp Debugger Architecture — Breakpoints, StorageBlock Inspection, Step Semantics, PDB/Symbol Generation & ExecutionContext Visualization (CIL‑Only)
=================================================================================================================================

> **STATUS BANNER (2026-06-07).** This is a **DESIGN REFERENCE** for the CobolSharp/COBOL.NET debugger subsystem.
> **Implementation status: DESIGN-ONLY (~0 lines).** There is no debugger, PDB-emission, sequence-point, LSP, or DAP
> code in `src/` today — the subsystem is **scheduled for Phase E** ("BUILD THE PRODUCT SURFACE") in
> `docs/MASTER_PLAN.md` (§Phase E: "IDE & debug — Debugger (PDB + sequence points + DAP adapter) → LSP/IDE
> integration"). Treat every "CobolSharp emits / supports / shows" sentence below as a **target design**, not a
> statement of current behavior.
>
> **Stack:** .NET 10 / C# 14. **Backend:** CIL-only via **Mono.Cecil** — there is **no custom VM and no bytecode
> interpreter**; the Roslyn C# backend is a *future additive* option (Stage-5, with Cecil as the oracle). All
> "CIL-only / no custom VM / deterministic across CoreCLR-AOT-WASM" language below is therefore correct in spirit;
> ignore any implication of a bespoke execution engine.
>
> **Data-model note (important).** Several sections describe inspecting **`StorageBlock` raw byte buffers**
> (`byte[]`, packed-decimal decode, REDEFINES overlays). This reflects the *legacy byte engine*, which is being
> **ISLANDED** by the typed-native data-model migration (see `docs/DATA_MODEL_ARCHITECTURE.md`,
> `docs/RECORD_STRUCT_STORAGE_DESIGN.md`). Under the new model, character → .NET `string`, numeric → `long`/`decimal`,
> groups → `record struct`, OCCURS → `T[]`, and pointers → a single **`ManagedPointer`** carrier (no 8-byte handle,
> no PointerRegistry). When this debugger is built, the variable-inspection layer must surface BOTH representations:
> the typed-native value (the default, when `EnableTypedFields` is on) **and** the raw `StorageBlock` bytes for the
> classifier-scoped byte fallback (REDEFINES / file / EXTERNAL / ref-mod / ODO). The byte-centric inspection sections
> here remain valid only for that islanded fallback.
>
> **Plan SSOT:** `docs/MASTER_PLAN.md` (Phase E owns this subsystem). **Doctrine:** `PROMPT.md`.
> **Provenance:** Consolidated from 4 prior debugger architecture essays on 2026-06-07
> (Breakpoints/StorageBlock/ExecutionContext [canonical base] + PDB-Symbol-Generation + Sequence-Point/Symbol-Mapping
> + Sequence-Point/Symbol/Storage-Visualization). All unique current content from the other three is merged below
> under clearly-marked "MERGED" sections.

Purpose
-------
Define the authoritative architecture for:
- Breakpoints (line, paragraph, conditional)
- Step semantics (STEP INTO, STEP OVER, STEP OUT)
- ExecutionContext visualization
- StorageBlock inspection (decoded and raw)
- ObjectTable inspection
- FileManager state inspection
- PERFORM stack and CALL stack visualization
- Declarative and exception tracing
- IL‑level mapping and sequence points
- AOT/WASM‑safe debugging

This document governs how CobolSharp integrates with .NET debugging infrastructure to provide a full COBOL‑aware debugging experience.

------------------------------------------------------------
SECTION 1 — DEBUGGER OVERVIEW
------------------------------------------------------------

CobolSharp debugging provides:
- Source‑level stepping
- Paragraph‑level stepping
- Breakpoints on COBOL lines
- Breakpoints on paragraphs/sections
- Inspection of:
  - StorageBlocks (decoded)
  - ObjectTable
  - FileManager state
  - ExecutionContext
  - PERFORM stack
  - CALL stack
  - ExceptionState
- Deterministic stepping across CoreCLR, AOT, and WASM

Debugger integration is:
- Pure managed
- Sequence‑point based
- Platform‑independent

------------------------------------------------------------
SECTION 2 — BREAKPOINT ARCHITECTURE
------------------------------------------------------------

2.1 Line breakpoints
--------------------
Set on:
- Any PROCEDURE DIVISION statement
- ENTRY statements
- Declarative procedures

Compiler emits:
- Sequence points for each statement
- Mapping from COBOL line → IL offset

2.2 Paragraph breakpoints
-------------------------
Breakpoint on:
- Paragraph label
- SECTION label

Compiler emits:
- Sequence point at paragraph entry

2.3 Conditional breakpoints
---------------------------
Supported conditions:
- Numeric comparisons
- String comparisons
- Boolean expressions

Lowering:
- Debugger evaluates condition using locals and StorageBlocks

2.4 Breakpoint binding
----------------------
Bound at:
- Compile time (preferred)
- Runtime (via PDB)

------------------------------------------------------------
SECTION 3 — STEP SEMANTICS
------------------------------------------------------------

3.1 STEP INTO
-------------
Steps into:
- Paragraphs
- Sections
- ENTRY points
- Declaratives
- INVOKE (instance/static)
- CALL statements

3.2 STEP OVER
-------------
Executes:
- Entire paragraph
- Entire CALL
- Entire INVOKE
- Entire PERFORM range

3.3 STEP OUT
------------
Returns to:
- Caller paragraph
- Caller program (if stepping out of CALL)
- Caller of declarative

3.4 Deterministic stepping
--------------------------
Stepping behavior identical across:
- CoreCLR
- AOT
- WASM

------------------------------------------------------------
SECTION 4 — EXECUTIONCONTEXT VISUALIZATION
------------------------------------------------------------

4.1 Context fields shown
------------------------
Debugger displays:
- Program name
- ENTRY name
- COMMON/INITIAL flag
- ReturnValue
- ExceptionState
- Active declarative (if any)
- Current PERFORM frame
- Current CALL frame

4.2 Context switching
---------------------
On CALL:
- Debugger switches to callee context

On GOBACK:
- Debugger returns to caller context

------------------------------------------------------------
SECTION 5 — STORAGEBLOCK INSPECTION
------------------------------------------------------------

5.1 Raw view
------------
Debugger shows:
- byte[] Buffer
- Offsets
- Lengths

5.2 Decoded view
----------------
Debugger decodes:
- DISPLAY → ASCII
- NATIONAL → UTF‑16
- COMP‑3 → packed decimal
- COMP‑5 → binary integer
- Numeric DISPLAY → Decimal

5.3 Group view
--------------
Debugger shows:
- Group hierarchy
- Nested fields
- OCCURS tables
- ODO active length

5.4 REDEFINES view
------------------
Debugger highlights:
- Overlaid regions
- Conflicting interpretations

------------------------------------------------------------
SECTION 6 — OBJECTTABLE INSPECTION
------------------------------------------------------------

6.1 Object references
---------------------
Debugger shows:
- Index
- .NET type
- COBOL class name (if applicable)
- Null vs non‑null

6.2 Instance fields
-------------------
Debugger displays:
- FACTORY fields (static)
- OBJECT fields (instance)

6.3 Lifetime
------------
Objects remain visible until:
- ExecutionContext destroyed
- Reference overwritten

------------------------------------------------------------
SECTION 7 — FILEMANAGER STATE INSPECTION
------------------------------------------------------------

7.1 File state
--------------
Debugger shows:
- Open mode (INPUT/OUTPUT/I‑O/EXTEND)
- Organization (SEQUENTIAL/INDEXED/RELATIVE)
- Access mode (SEQUENTIAL/RANDOM/DYNAMIC)
- Current record position
- File status code

7.2 Record buffer
-----------------
Debugger displays:
- Raw bytes
- Decoded fields
- Key values

------------------------------------------------------------
SECTION 8 — PERFORM STACK VISUALIZATION
------------------------------------------------------------

8.1 PERFORM frame fields
------------------------
Debugger shows:
- Return label
- THRU range
- Loop variables
- Loop bounds
- TEST BEFORE/AFTER flag

8.2 Nested PERFORMs
-------------------
Displayed as:
- Stack of frames
- Top = current PERFORM

------------------------------------------------------------
SECTION 9 — CALL STACK VISUALIZATION
------------------------------------------------------------

9.1 CALL frame fields
---------------------
Debugger shows:
- Program name
- ENTRY name
- USING parameters
- RETURNING target
- Caller location

9.2 Multi‑module calls
----------------------
Debugger displays:
- Full chain of CALLs
- Cross‑module transitions

------------------------------------------------------------
SECTION 10 — DECLARATIVE & EXCEPTION TRACING
------------------------------------------------------------

10.1 Declarative entry
----------------------
Debugger highlights:
- Declarative triggered
- Source of error
- ExceptionState contents

10.2 Resumption
---------------
Debugger shows:
- Resume location after declarative
- Cleared ExceptionState

------------------------------------------------------------
SECTION 11 — IL‑LEVEL MAPPING
------------------------------------------------------------

11.1 Sequence points
--------------------
Compiler emits:
- One sequence point per COBOL statement
- Additional points for paragraph labels

11.2 IL correlation
-------------------
Debugger can show:
- IL for current statement
- IL for paragraph
- IL for ENTRY method

11.3 WASM mapping
-----------------
Sequence points preserved in AOT → WASM pipeline.

------------------------------------------------------------
SECTION 12 — AOT/WASM‑SAFE DEBUGGING
------------------------------------------------------------

12.1 No dynamic codegen
-----------------------
All debug info static.

12.2 No reflection
------------------
Debugger uses PDB metadata only.

12.3 Deterministic stepping
---------------------------
Identical stepping behavior across platforms.

------------------------------------------------------------
SECTION 13 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

13.1 Breakpoint inside REDEFINES
--------------------------------
Debugger shows both interpretations.

13.2 Breakpoint inside OCCURS DEPENDING ON
------------------------------------------
Debugger shows active and max elements.

13.3 Stepping into declarative
------------------------------
Debugger enters declarative normally.

13.4 Stepping out of nested PERFORM
-----------------------------------
Debugger unwinds PERFORM frames correctly.

13.5 CALL to program with no ENTRY
----------------------------------
Debugger enters PROCEDURE DIVISION directly.

============================================================
MERGED SECTION A — PDB SYMBOL GENERATION & DEBUGGING MODEL
(from "CobolSharp COBOL Debugging & PDB Symbol Generation Architecture")
============================================================

A.1 Three pillars of the debugging model
----------------------------------------
1. **PDB symbol generation** — mapping COBOL source → CIL instructions.
2. **Runtime inspection** — mapping COBOL storage → .NET objects.
3. **IDE integration** — LSP + DAP providing breakpoints, stepping, variables, call stack.

The model is deterministic, fully CIL-based, compatible with CoreCLR / AOT / WASM (via `dotnet publish`),
and independent of any custom VM.

A.2 PDB format & emission
-------------------------
CobolSharp emits:
- **Portable PDBs** (cross-platform).
- Embedded source mapping.
- Custom metadata for COBOL constructs.
- Local variable signatures and scopes.
- Exception regions.
- Custom attributes carrying COBOL metadata.

A.3 Sequence points → CIL offsets
---------------------------------
Sequence points map COBOL source spans to CIL instruction offsets, emitted for:
statement start, paragraph entry, section entry, PERFORM entry/exit, branch targets, exception handlers.
Hidden sequence points cover compiler-generated code, PERFORM loop scaffolding, and exception-region boundaries.

A.4 Source mapping in the PDB
-----------------------------
The PDB preserves and stores: original source span, preprocessed source span, COPY/REPLACE mapping —
i.e. file path, line/column, and the expanded → original mapping.

A.5 Variable categories supported for inspection
------------------------------------------------
Elementary items, group items, OCCURS arrays, OCCURS DEPENDING ON logical length, REDEFINES overlays,
condition names (88-levels), file buffers, OO object fields, local variables (in methods).

A.6 Display formatting of inspected values
------------------------------------------
DISPLAY strings; numeric values (decimal/binary); packed-decimal decoded; boolean TRUE/FALSE; optional hex dump.
Group items render as a tree with offsets/sizes, REDEFINES relationships, and OCCURS bounds.
(Under the typed-native model these decode from the typed value directly; the byte-decode path applies only to the
islanded `StorageBlock` fallback — see banner.)

A.7 Breakpoint types (superset)
-------------------------------
Line, conditional, hit-count, paragraph, section. Breakpoints bind to sequence points / paragraph entry /
section entry points. If a line has no executable code, bind to the next executable statement or mark as unbound.

A.8 Stepping semantics (Step Over / Into / Out)
-----------------------------------------------
- **Step Over** — executes current statement; steps over PERFORM, CALL, INVOKE.
- **Step Into** — steps into paragraphs, sections, methods, PERFORM bodies, CALL/INVOKE targets.
- **Step Out** — steps out of paragraph, section, method, PERFORM.
- **Step Through PERFORM** — step into the PERFORM body; step over PERFORM loop scaffolding.

A.9 Exception mapping (COBOL ↔ .NET)
-----------------------------------
COBOL → .NET examples (target mapping; final categories to be reconciled with the Phase-C EC exception model):
- SIZE ERROR → ArithmeticException
- INVALID KEY → KeyNotFoundException
- AT END → EndOfStreamException
- JSON/XML errors → JsonException / XmlException

.NET → COBOL: a .NET exception raised inside COBOL code maps to ON EXCEPTION when applicable, otherwise propagates
as a .NET exception. The debugger shows the COBOL exception category, the underlying .NET exception, and the storage
state at the time of the exception.

A.10 LSP + DAP integration
--------------------------
- **LSP responsibilities:** hover info, symbol lookup, diagnostics, code actions.
- **DAP (Debug Adapter Protocol) responsibilities:** breakpoints, stepping, stack traces, variable inspection,
  exception reporting.
- **Combined IDE experience:** COBOL source view, variables panel with COBOL formatting, call stack showing
  paragraphs/sections, watch expressions, memory view.

A.11 Paragraph/section → CIL mapping option
-------------------------------------------
Paragraphs map either to CIL methods (optional) or to CIL basic blocks with labels.

============================================================
MERGED SECTION B — SYMBOL GENERATION BY STORAGE SECTION,
RECORD-BUFFER, JSON/XML & REPORT-WRITER VISUALIZATION
(from "CobolSharp COBOL Debugger, Sequence‑Point, Symbol & Storage Visualization Architecture")
============================================================

B.1 Symbol generation per storage section
-----------------------------------------
- **WORKING-STORAGE** — each field becomes a debugger symbol whose name equals the COBOL name, with offset metadata.
- **LOCAL-STORAGE** — allocated per method; the debugger shows local variables with method-scoped lifetime.
- **LINKAGE SECTION** — debugger shows BY REFERENCE pointers and BY CONTENT copies.
- **Temporary locals** — compiler-generated, hidden unless needed, named `temp_1`, `temp_2`, … .

B.2 Storage visualizer detail
-----------------------------
Per field: name, PIC/USAGE, offset, raw bytes, decoded value. Nested groups render as an expandable tree (each child
decoded). OCCURS tables render as an array of expandable elements. COMP-3 shows raw packed bytes + unpacked decimal.
NATIONAL shows UTF-16 code units + decoded string. (Byte-level detail applies to the islanded fallback — see banner.)

B.3 Record buffer visualization (FD)
------------------------------------
Debugger shows the record name, field offsets, field values, and raw bytes. For indexed files it shows the primary
key value and alternate key values. Deleted/invalid records show a deleted flag and status code.

B.4 JSON / XML visualization
----------------------------
(Note: JSON/XML PARSE/GENERATE lowering+runtime is itself DESIGN-ONLY — Phase C. This describes the eventual
debugger surface over it.)
- **JSON PARSE** — current event, path, value, error metadata.
- **XML PARSE** — event type, element name, attribute name/value, depth, character data.
- **JSON/XML GENERATE** — current field, output-buffer preview.

B.5 Report Writer visualization
-------------------------------
(Report Writer IS implemented — see `docs/REPORT_WRITER_CONTROL_DESIGN.md` / `docs/REPORT_WRITER_ROADMAP.md`.)
- **Page/line state** — current page, current line, page limit.
- **Control-break state** — current control keys, previous control keys, break level.
- **Accumulators** — SUM, COUNT, AVERAGE, MIN/MAX.
- **Rendered line preview** — ASCII preview of the line with column positions.

B.6 Additional edge cases
-------------------------
- **COPY inside COPY** — debugger steps into nested COPY.
- **REPLACE BEFORE/AFTER** — debugger shows the original source, not the replaced text.
- **PERFORM VARYING with multiple indices** — debugger shows all loop variables and the current iteration.
- **EXIT PARAGRAPH / EXIT SECTION** — debugger shows the corresponding PERFORM-stack pop.
- **STOP RUN during debugging** — debugger terminates the session.

============================================================
MERGED SECTION C — SOURCE-MAPPING DETAIL & WATCH/LOCALS WINDOWS
(from "CobolSharp COBOL Debugger, Sequence Point & Symbol Mapping Architecture")
============================================================

C.1 Sequence-point placement (full list)
----------------------------------------
Emitted for: paragraph entry, section entry, every COBOL statement, PERFORM entry and exit, INVOKE and CALL,
JSON/XML operations, file-I/O operations, exception-handler entry, declarative entry.
Hidden sequence points cover loop boundaries, compiler-generated scaffolding, and exception-region boundaries.
Each sequence point maps to the original file path, original line/column, and the expanded-source span (when
COPY/REPLACE applied).

C.2 COPY/REPLACE source-mapping table
-------------------------------------
The preprocessor produces: a mapping from expanded text → original source, nested mappings for nested COPYs, and a
REPLACE mapping (original span preserved). The debugger therefore displays the ORIGINAL source file and ORIGINAL line
numbers — never the expanded COPY text. Breakpoints set in COPY books map to the expanded code with correct IL
offsets; breakpoints in REPLACE regions map to the original source span (replacement text inherits the mapping);
breakpoints in declaratives and nested programs are fully supported (nested programs bind to the correct
ExecutionContext and program class).

C.3 SIZE ERROR reporting detail
-------------------------------
On SIZE ERROR the debugger shows overflow details, the target field, and the operation that overflowed.

C.4 WATCH window
----------------
Supports data items, group items, OCCURS elements, condition names, and OO object fields.

C.5 LOCALS window
-----------------
Shows temporary locals, loop counters, RETURNING values, and INVOKE arguments.

C.6 RAW VIEW
------------
Debugger can show raw bytes of any field, the packed-decimal representation, and the binary representation.
(Raw/byte views apply to the islanded `StorageBlock` fallback — see banner; typed-native fields surface their
.NET value directly.)

C.7 Stepping edge cases
-----------------------
- Stepping into a COPY book steps into the original COPY source file.
- Stepping through REDEFINES shows all overlapping fields with raw bytes unchanged.
- Stepping through declaratives shows declarative entry, the USE condition, and return to the failing statement.
- Stepping through PERFORM VARYING shows loop-variable changes and loop boundaries.
- Stepping through JSON/XML shows parsed values, event callbacks (XML), and exception details.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Debugger Architecture (consolidated):
- Provides full COBOL‑aware debugging with StorageBlock/typed-value, ObjectTable, FileManager, and ExecutionContext
  visualization
- Supports line/paragraph/section/conditional/hit-count breakpoints, Step Into/Over/Out, declarative tracing, and
  IL mapping
- Emits Portable PDBs with sequence points, local scopes, and COPY/REPLACE → original-source mapping
- Integrates with .NET debugging APIs via LSP + DAP, plus record-buffer, JSON/XML, and Report-Writer visualization
- Ensures deterministic debugging across CoreCLR, AOT, and WASM
- Generates clean, verifiable, debugger‑friendly CIL with precise sequence points
- Remains DESIGN-ONLY today; to be built in Phase E per docs/MASTER_PLAN.md
