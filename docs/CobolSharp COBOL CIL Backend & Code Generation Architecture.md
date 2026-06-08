CobolSharp COBOL CIL Backend & Code Generation Architecture (CIL-Only)
=====================================================================

> **STATUS BANNER — design reference for the CIL backend.**
> This is a **target-design** document for the CIL backend (the final stage of the
> compilation pipeline). **Implementation status: substantially implemented (~80-90%)**
> for the COBOL-85 core — the real backend is `src/CobolSharp.Compiler/CodeGen/CilEmitter.cs`
> plus the 11-file `CodeGen/Emission/` emitter set (M003 decomposition, see below).
> JSON/XML lowering and the AOT/WASM publish/optimization axes described here remain
> **design-only / aspirational**; verify any specific claim against `src/` before relying on it.
>
> **Stack: .NET 10 / C# 14.** Backend is **CIL-only, emitted via Mono.Cecil** — there is
> **NO custom VM, NO bytecode interpreter**. (A Roslyn C# backend is a *future, additive*
> Stage-5; Cecil remains the oracle.) Pointers are ONE managed-ref carrier =
> **`ManagedPointer`** (`src/CobolSharp.Runtime/ManagedPointer.cs`) — no 8-byte native handle,
> no PointerRegistry. The data model is migrating to typed-native representations
> (CORE done through Stage-4; `EnableTypedFields` default OFF; byte engine being islanded).
> The CIL backend (`CilEmitter`) is **already decomposed into focused emitters** — it is NOT a god class.
>
> Plan SSOT: **`docs/MASTER_PLAN.md`**. Live design context for this subsystem:
> `docs/IL-BYTECODE-GENERATION-DESIGN.md` (IL data structures) and
> `docs/cilemitter/CilEmitter-Decomposition.md` (the actual M003 decomposition record).
> Doctrine: `PROMPT.md`.

Purpose
-------
Define the authoritative target architecture for:
- CIL code generation
- Method and type emission
- Storage layout and FieldOffset emission
- Control-flow lowering
- Exception region generation
- Debugger sequence point emission
- Integration with ExecutionContext
- Integration with IR and optimizer
- AOT/WASM-safe IL generation

This document governs how CobolSharp emits verifiable, efficient, and debugger-friendly CIL.

------------------------------------------------------------
SECTION 0 — HIGH-LEVEL GOALS & PIPELINE
------------------------------------------------------------

0.1 Goals
---------
- Transform the fully-resolved semantic model into verifiable, efficient .NET CIL assemblies.
- Treat .NET IL (emitted via Mono.Cecil) as the *only* backend target.
- Produce:
  - .dll or .exe assemblies
  - PDB debug symbols
  - Optional single-file or AOT-compiled artifacts (via dotnet tooling)
- Preserve COBOL semantics:
  - Packed decimal arithmetic
  - File I/O semantics
  - PERFORM/GO TO control flow
  - REDEFINES aliasing
  - OCCURS DEPENDING ON dynamic bounds
  - OO and generics
  - JSON/XML operations (design-only)
- Integrate cleanly with the CobolSharp runtime library.

0.2 Overall pipeline
--------------------
    SemanticModel
        ↓
    ILBuilder (CobolSharp IR — see docs/IL-BYTECODE-GENERATION-DESIGN.md)
        ↓
    Optimization Pipeline
        ↓
    CILBackend (CilEmitter + CodeGen/Emission/* emitters, via Mono.Cecil)
        ↓
    .NET Assembly (DLL/EXE) + PDB
        ↓
    Optional: dotnet publish (AOT, single-file, WASM, native)

There is **no VM**, no bespoke bytecode format, and no interpreter. The CobolSharp IR
is lowered directly to verifiable .NET CIL through Mono.Cecil.

Backend goals:
- Deterministic IL
- Verifiable IL
- Debugger-friendly IL
- AOT/WASM compatible
- No dynamic code generation

------------------------------------------------------------
SECTION 1 — CORE BACKEND COMPONENTS
------------------------------------------------------------

The conceptual backend components are:

1. CILBackend
2. TypeEmitter
3. MethodEmitter
4. FieldEmitter
5. ControlFlowEmitter
6. InstructionLowerer
7. MetadataEmitter
8. DebugInfoEmitter

> **Implementation note (actual code):** these conceptual roles map onto the real
> `CilEmitter` orchestrator plus the focused emitters in
> `src/CobolSharp.Compiler/CodeGen/Emission/` (`CilModuleSetup`, `CilProgramStateEmitter`,
> `CilControlFlowEmitter`, `CilDataEmitter`, `CilArithmeticEmitter`, `CilComparisonEmitter`,
> `CilExpressionEmitter`, `CilLocationEmitter`, `CilStringEmitter`, `CilFileIoEmitter`,
> plus `EmissionContext`). See `docs/cilemitter/CilEmitter-Decomposition.md`.

CILBackend
----------
The central backend that consumes the optimized ILModule and emits .NET types, methods,
fields, IL instructions, metadata, and PDB debug symbols.

Uses:
- Mono.Cecil (the production emitter / verification oracle)
- Portable PDB writer
- (System.Reflection.Metadata / System.Reflection.Emit are alternatives historically
  considered, but the shipping backend is Cecil-based)

TypeEmitter
-----------
Maps CobolSharp IR types to .NET types (`ILType → TypeDefinitionHandle`). Supports:
- Program types (static classes)
- Class types (OO COBOL)
- Interface types
- Record types (01-level structures)
- Array types (OCCURS)
- Generic type definitions and instantiations

Record layout:
- Each COBOL 01-level group becomes a .NET class with explicit layout.
- Fields use `FieldOffset` attributes to match COBOL storage (byte-image path).
- REDEFINES groups share offsets.

> **Data-model migration note:** under `EnableTypedFields`, elementary items are migrating
> to typed-native representations (`string`/`long`/`decimal`/`bool`, nested `record struct`s,
> `T[]` for fixed OCCURS). The explicit-FieldOffset byte image remains as the
> classifier-scoped fallback (REDEFINES/RENAMES/edited/file/EXTERNAL/LINKAGE/ref-mod/ODO/etc.)
> and is being islanded, not removed.

MethodEmitter
-------------
Maps `ILMethod → MethodDefinitionHandle`. Handles parameters, return types, local
variables, generic parameters, and attributes (static/virtual/override). Generates the
method body (CIL), local variable signatures, and exception-handling blocks.

FieldEmitter
------------
Maps `ILField → FieldDefinitionHandle`. Handles:
- Static fields (WORKING-STORAGE, CLASS-DATA)
- Instance fields (OBJECT-DATA)
- Explicit layout for record fields
- Packed decimal storage (byte arrays)

ControlFlowEmitter / InstructionLowerer — see Sections 4 and 8.
MetadataEmitter / DebugInfoEmitter — see Sections 6 and the metadata notes below.

------------------------------------------------------------
SECTION 2 — TYPE / STORAGE EMISSION
------------------------------------------------------------

2.1 Storage classes
-------------------
Each COBOL storage region becomes an explicit-layout class (byte-image path):

    [StructLayout(LayoutKind.Explicit)]
    public class WORKING_STORAGE {
        [FieldOffset(0)]  byte[] FieldA;
        [FieldOffset(10)] byte[] FieldB;
    }

Regions:
- WORKING-STORAGE
- LOCAL-STORAGE
- LINKAGE
- File record buffers
- OCCURS arrays (as nested types / arrays)

2.2 Packed decimal fields
-------------------------
Packed-decimal fields emitted (byte-image path) as a fixed-size `byte[]`, accessed via
the runtime NumericEngine.

2.3 Group items
---------------
Group items become nested classes with explicit offsets for their children.

2.4 Data layout (Data Division → IL) — detail
---------------------------------------------
- Each 01-level group → a Record ILType; each elementary item → an ILField.
- PIC/USAGE determines storage size, encoding (binary / packed decimal / display),
  and sign representation.
- OCCURS: fixed → IL array field; OCCURS DEPENDING ON → IL dynamic array with runtime bounds.
- REDEFINES: multiple ILFields share the same offset; the ILType tracks aliasing metadata.
- RENAMES: the ILType stores a synthetic field representing the renamed range.

------------------------------------------------------------
SECTION 3 — METHOD EMISSION
------------------------------------------------------------

3.1 Program entry method
------------------------
Generated as `public static void Main(ExecutionContext ctx)`. Each COBOL program becomes
a .NET class with one main entry method plus additional ENTRY methods. The standard
generated method signature is:

    void MethodName(ExecutionContext ctx)

3.2 Paragraph/section methods
-----------------------------
Two modes:
- **Mode A:** Paragraphs as methods.
- **Mode B:** Paragraphs as basic blocks inside a single method.

CobolSharp defaults to paragraphs as basic blocks, sections as labels.

3.3 Declarative handlers
------------------------
Declaratives become separate methods, registered in ExecutionContext.

------------------------------------------------------------
SECTION 4 — CONTROL-FLOW LOWERING
------------------------------------------------------------

4.1 IF/ELSE
-----------
    brfalse → else
    then block
    br → end
    else block
    end:

4.2 EVALUATE
------------
- If numeric (and dense): lower to a `switch` jump table.
- Else: lower to an if-chain / decision tree.

4.3 PERFORM THRU
----------------
- Label for start, label for end.
- br to start, fall-through until end, br to return label.

4.4 PERFORM UNTIL
-----------------
    loop_start:
        if (condition) br loop_end
        body
        br loop_start
    loop_end:

4.5 PERFORM VARYING
-------------------
    init
    loop_start:
        if (condition) br loop_end
        body
        increment
        br loop_start
    loop_end:

(Nested PERFORM VARYING lowers to nested loops with explicit increments.)

4.6 GO TO
---------
- Direct `br target_label`.
- If GO TO crosses paragraph boundaries, method splitting / PERFORM-stack unwinding is
  applied as needed.

4.7 PERFORM-graph mapping (IR detail)
-------------------------------------
- Semantic analysis produces a control-flow graph; each basic block maps to an `ILBasicBlock`.
- PERFORM paragraph → call-like branch to block; PERFORM THRU → branch to range start,
  return at range end.
- EXIT PERFORM/SECTION/PROGRAM → branch to the appropriate exit block.

------------------------------------------------------------
SECTION 5 — EXCEPTION REGION EMISSION
------------------------------------------------------------

CIL exception blocks represent COBOL exception constructs (ON EXCEPTION, INVALID KEY,
AT END, SIZE ERROR).

5.1 ON EXCEPTION
----------------
    try {
        operation
    } catch (Exception ex) {
        ctx.ExceptionState = ...
        br handler_label
    }

5.2 INVALID KEY / AT END
------------------------
    call FileManager.Read
    ldloc status
    switch {
        case INVALID_KEY → handler
        case AT_END → handler
    }

5.3 SIZE ERROR
--------------
    call NumericEngine
    if (!success) br size_error_handler

5.4 Declarative return
----------------------
A declarative ends with `leave continuationLabel`.

------------------------------------------------------------
SECTION 6 — DEBUGGER SEQUENCE POINTS
------------------------------------------------------------

6.1 Placement
-------------
Sequence points emitted for: paragraph entry, section entry, statement start,
PERFORM entry/exit, CALL/RETURN, exception-handler entry, branch targets.

6.2 Mapping to original source
------------------------------
Sequence points map to the original source file and original line/column, with
COPY/REPLACE mapping preserved (original source → preprocessed source → IL; copybook
lines map back to their origin files).

6.3 Hidden sequence points
--------------------------
Used for compiler-generated scaffolding, loop boundaries, and exception-region boundaries.

6.4 Local variable scopes
-------------------------
Each COBOL variable maps to a debugger symbol; temporary locals are hidden unless needed.

------------------------------------------------------------
SECTION 7 — INTEGRATION WITH EXECUTIONCONTEXT
------------------------------------------------------------

7.1 Passing ExecutionContext
----------------------------
All generated methods have signature `void MethodName(ExecutionContext ctx)`.

7.2 Storage access (byte-image path)
------------------------------------
- DISPLAY: `ctx.Storage.GetString(offset, length)`
- COMP:    `ctx.Storage.GetBinary(offset, width)`
- COMP-3:  `ctx.Storage.GetPackedDecimal(offset, digits, scale)`

7.3 File operations
-------------------
    ctx.FileManager.Read(...)
    ctx.FileManager.Write(...)
    ctx.FileManager.Start(...)

7.4 JSON/XML operations *(design-only)*
---------------------------------------
    ctx.JsonEngine.Parse(...)
    ctx.XmlEngine.Generate(...)

------------------------------------------------------------
SECTION 8 — CIL EMISSION RULES & INSTRUCTION LOWERING
------------------------------------------------------------

8.1 Verifiable IL
-----------------
CobolSharp guarantees:
- No unverifiable opcodes
- No unbalanced stacks
- No overlapping exception regions
- No unverifiable branching

8.2 AOT/WASM compatibility
--------------------------
Backend avoids: Reflection.Emit, DynamicMethod, unmanaged pointers, runtime code generation.

8.3 Optimization-friendly IL
----------------------------
Backend emits: structured loops, structured branches, minimal stack depth, minimal
temporary locals.

8.4 Instruction lowering (opcode map)
-------------------------------------
Loads/stores:
- LOAD_LOCAL → ldloc · STORE_LOCAL → stloc · LOAD_FIELD → ldfld · STORE_FIELD → stfld

Arithmetic:
- ADD_INT → add · SUB_INT → sub · MUL_INT → mul · DIV_INT → div
- Packed decimal ops → call into CobolSharp.Runtime.NumericEngine

Branches:
- BR → br · BR_TRUE → brtrue · BR_FALSE → brfalse · BR_EQ → beq · BR_NE → bne.un · SWITCH → switch

Object creation:
- NEW_OBJECT → newobj · INIT_OBJECT → call constructor

Method calls:
- CALL → call · CALL_VIRTUAL → callvirt · CALL_STATIC → call

String / JSON·XML / File:
- STRING/UNSTRING → runtime calls
- JSON/XML PARSE/GENERATE → runtime calls *(design-only)*
- READ/WRITE/REWRITE/DELETE → runtime FileManager calls

------------------------------------------------------------
SECTION 9 — FILE I/O LOWERING
------------------------------------------------------------

9.1 READ:    `call FileManager.Read` → check status → branch to handlers
9.2 WRITE:   `call FileManager.Write`
9.3 REWRITE: `call FileManager.Rewrite`
9.4 DELETE:  `call FileManager.Delete`
9.5 START:   `call FileManager.Position`

CALL semantics:
- Static call → direct IL call; dynamic call → runtime dispatch.
- BY VALUE / BY REFERENCE / BY CONTENT → argument marshalling.

------------------------------------------------------------
SECTION 10 — JSON/XML LOWERING *(design-only)*
------------------------------------------------------------

10.1 JSON PARSE:    `call JsonEngine.Parse`
10.2 JSON GENERATE: `call JsonEngine.Generate`
10.3 XML PARSE:     `call XmlEngine.Parse`
10.4 XML GENERATE:  `call XmlEngine.Generate`

------------------------------------------------------------
SECTION 11 — EDGE-CASE BEHAVIOR
------------------------------------------------------------

11.1 GO TO into middle of PERFORM — allowed; backend preserves block boundaries.
11.2 EXIT PROGRAM inside nested PERFORM — emit PERFORM-stack unwind, then return from entry method.
11.3 REDEFINES overlapping fields — shared FieldOffset; accessors respect raw bytes.
11.4 OCCURS DEPENDING ON — max array size emitted; logical length checked at runtime.
11.5 Packed decimal overflow — NumericEngine call + SIZE ERROR branch.
11.6 Paragraph with no statements — emit `nop`, and `ret` if standalone.

------------------------------------------------------------
SECTION 12 — IL GENERATION PIPELINE, VERIFICATION & OPTIMIZATION
------------------------------------------------------------

12.1 IL generation pipeline overview
------------------------------------
1. AST → Control-Flow Graph (CFG)
2. CFG → IL Block Graph
3. IL Block Graph → Verified IL Stream
4. IL Stream → Portable PDB
5. IL Stream → .NET assembly

Goals: structured IL (no irreducible flow), verifiable IL (no stack imbalance),
debugger-friendly sequence points, AOT/WASM-safe instructions only, deterministic output.

12.2 Temporary locals & register allocation
-------------------------------------------
- Temporary locals used for arithmetic intermediates, STRING/UNSTRING buffers, JSON/XML
  temporaries, loop counters, condition evaluation.
- Local types: `decimal`, `int32`, `int64`, `string`, object reference, `bool`.
- Allocation strategy: linear-scan allocator; reuse locals when lifetimes do not overlap;
  no dynamic locals (AOT-safe).

12.3 Stack discipline & verifiability
-------------------------------------
Ensures: stack height known at compile time; no unbalanced branches; no fall-through into
exception blocks; no unverifiable instructions.

Forbidden IL patterns: unverifiable tailcalls, unaligned access, unverifiable pointer ops,
unverifiable constrained calls, unverifiable exception filters.

Allowed IL subset: `ldc`, `ldloc`, `stloc`, `call`, `callvirt`, `br`, `brtrue`, `brfalse`,
`newobj`, `leave`, `endfinally`, `try`/`catch`/`finally`.

12.4 Expression tree lowering
-----------------------------
Arithmetic — `COMPUTE x = a + b * c` lowers to:
    ld a · ld b · ld c · mul · add · store x

Boolean (short-circuit):
    A AND B:  ld A · brfalse end · ld B · end:
    A OR  B:  ld A · brtrue  end · ld B · end:

String:
    STRING   → call ctx.StringEngine.Concat
    UNSTRING → call ctx.StringEngine.Split

Numeric conversions: PIC/USAGE conversions inserted automatically.

12.5 CALL / RETURN lowering
---------------------------
- CALL literal:    `call Program.Main`
- CALL identifier: `call ProgramRegistry.Lookup` → `callvirt Program.Main`
- RETURNING:       store return value in local → `ret`
- GOBACK:          `ret`
- STOP RUN:        return from entry point
- INVOKE (OO):     instance → virtual call; static → direct call; SUPER → base call;
  RETURNING → store result.

12.6 Optimization passes
------------------------
- **Peephole:** remove redundant ldloc/stloc pairs, remove dead branches, collapse
  br-to-br, inline trivial temporaries, remove unreachable blocks.
- **Constant folding:** numeric literals, boolean literals, string literals (ASCII only).
- **Loop:** hoist invariant expressions, remove dead increments, loop simplification,
  strength reduction.
- **COPY/REPLACE:** remove dead code introduced by REPLACE, collapse empty paragraphs.
- **General:** dead-code elimination, inline small paragraphs, remove redundant MOVE statements.

12.7 AOT/WASM-safe IL patterns
------------------------------
- **No reflection:** no DynamicMethod, Type.GetType, Activator.CreateInstance.
- **No unsafe code:** no pointers, stackalloc, or unmanaged memory.
- **No platform-specific instructions:** no cpblk, initblk, unverifiable opcodes.
- **Deterministic IL:** no runtime codegen, no JIT-dependent behavior.

12.8 Verification
-----------------
Optional ILVerifier checks: type correctness, stack correctness, control-flow validity,
no unreachable blocks (optional). In the shipping backend, Mono.Cecil is the emission and
verification oracle.

------------------------------------------------------------
SECTION 13 — GENERICS SUPPORT *(design-only)*
------------------------------------------------------------

COBOL generics map to .NET generics:
- TYPEDEF GENERIC → generic type definition
- Generic methods → generic method definitions
- Instantiations → constructed generic types
- `OF type` → .NET generic constraints
- Method specialization → ILMethod with substituted types

------------------------------------------------------------
SECTION 14 — RUNTIME INTEGRATION
------------------------------------------------------------

All complex COBOL semantics live in `CobolSharp.Runtime.dll`. The CIL backend emits calls
to the runtime for:
- Packed decimal arithmetic (NumericEngine)
- File I/O (FileManager — indexed/relative/sequential)
- SORT/MERGE (SortEngine/MergeEngine)
- JSON/XML (JsonParser/JsonGenerator, XmlParser/XmlGenerator) *(design-only)*
- String operations (STRING/UNSTRING helpers)
- Date/time functions
- Collating sequences
- Intrinsic functions
- The exception model
- Pointers → the single `ManagedPointer` managed-ref carrier (no native heap, no handle table)

------------------------------------------------------------
SECTION 15 — DEPLOYMENT OPTIONS
------------------------------------------------------------

Since the output is pure .NET assemblies, developers can use:
- `dotnet run`
- `dotnet publish`
- `dotnet publish /p:PublishAot=true` (native AOT)
- `dotnet publish -blazorwasm` (WASM via .NET)

CobolSharp does **not** implement its own WASM or native backend — it relies on the .NET
toolchain. (These publish paths are an aspirational deployment surface, not all currently
validated.)

------------------------------------------------------------
SECTION 16 — TESTING STRATEGY
------------------------------------------------------------

- Golden IL tests
- CIL verification tests (Mono.Cecil)
- Reflection-based type/method/field validation
- Runtime behavior tests
- Debug symbol tests
- Cross-compiler tests (CobolSharp vs GnuCOBOL/Micro Focus)
- The NIST CCVS guard suite (kernel COBOL-85 compliance) + the conformance suite for
  post-1985 features

------------------------------------------------------------
SECTION 17 — THE ACTUAL EMITTER DECOMPOSITION (implemented)
------------------------------------------------------------

The shipping CIL backend is **already decomposed** (M003, complete) — it is not a god class.
`CilEmitter.cs` is the ~1,299-line orchestrator; the per-concern emitters live in
`src/CobolSharp.Compiler/CodeGen/Emission/` and share a single `EmissionContext`:

- `CilModuleSetup` — type/field/method-signature definition, entry + LINKAGE setup
- `CilProgramStateEmitter` — ProgramState alloc, VALUE init, EXTERNAL/ALTER/LOCAL-STORAGE init
- `CilControlFlowEmitter` — branches, PERFORM (simple/TIMES/inline/THRU), GO TO DEPENDING, ALTER, STOP/EXIT/GOBACK
- `CilDataEmitter` — MOVE variants, DISPLAY, ACCEPT, PIC descriptors
- `CilArithmeticEmitter` — ADD/SUB/MUL/DIV/COMPUTE/REMAINDER, accumulators, SIZE-ERROR status
- `CilComparisonEmitter` — numeric/decimal/string compares, class/user-class conditions
- `CilExpressionEmitter` — IrExpression trees, intrinsic calls, decimal/PIC/byte-array literals
- `CilLocationEmitter` — backing-array loads, element/ref-mod address computation, LINKAGE locations
- `CilStringEmitter` — STRING, UNSTRING, INSPECT (tally/replace/convert)
- `CilFileIoEmitter` — READ/WRITE/REWRITE/DELETE/START, file status, SORT/MERGE

Invariants of that decomposition: no behavioral change (bit-identical CIL), all emitters
`internal sealed`, instruction dispatch stays in `CilEmitter.EmitInstruction`, and all
shared mutable state is owned by `EmissionContext`. Full record:
`docs/cilemitter/CilEmitter-Decomposition.md`.

------------------------------------------------------------
SUMMARY
------------------------------------------------------------

The CobolSharp CIL backend:
- Is the **only** backend (CIL emitted via Mono.Cecil; no VM, no bytecode interpreter).
- Emits verifiable, structured, debugger-friendly .NET IL assemblies.
- Uses .NET metadata, PDBs, and runtime integration.
- Implements full COBOL semantics for control flow, storage, exceptions, file I/O,
  and arithmetic; OO/generics/JSON-XML are design-or-in-progress.
- Preserves paragraph/section structure and source mapping.
- Ensures correctness across CoreCLR, AOT, and WASM (via the .NET toolchain).
- Forms the final stage of the CobolSharp compilation pipeline, and is already
  decomposed into focused emitters (not a god class).
