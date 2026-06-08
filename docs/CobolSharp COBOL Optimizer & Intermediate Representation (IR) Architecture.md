CobolSharp COBOL Optimizer & Intermediate Representation (IR) Architecture (CIL-Only)
====================================================================================

> **STATUS — authoritative design reference for the IR and the (future)
> optimizer.** **Implementation status:** the **IR layer is REAL and current** —
> `src/CobolSharp.Compiler/IR/` (`IrExpression.cs`, `IrInstruction.cs`, `IrModule.cs`,
> `IrMethod.cs`, `IrType.cs`, `IrLocationExtensions.cs`) plus the
> `CodeGen/Lowering/*Lowerer.cs` family (Arithmetic / Condition / ControlFlow /
> DataMovement / Expression / FileIo / String + LocationResolver + LoweringContext).
> The **M001 "IR Expression Contract" refactor is COMPLETE** (BoundExpression no longer
> leaks into IR; see the LIVE doc `docs/ir/IR-Expression-Contract.md`). The dedicated
> **optimizer described in §§5–9 below is DESIGN-ONLY (~0 lines)** — there is **no**
> `Optimizer`/`ConstantFold`/`DeadCode`/`Peephole`/`Cfg`/`DataFlow` pass and **no**
> separate `ILModule`/`ILBasicBlock` optimization stage in `src/`. The compiler today
> lowers **Bound tree → IR → CIL directly** (no intermediate optimization pipeline).
> Treat §§5–9 as a target design to be added in a later phase, not as current behavior.
>
> **Stack:** .NET 10 / C# 14. **Backend:** CIL-only via **Mono.Cecil** — there is NO
> custom VM and NO bytecode interpreter (a Roslyn C# backend is a *future additive*
> option, Stage-5; Cecil remains the oracle).
>
> **SSOT:** the plan is `docs/MASTER_PLAN.md`; doctrine is `PROMPT.md`. The **LIVE** IR
> doc is `docs/ir/IR-Expression-Contract.md` (defer to it for the implemented IR
> expression contract).

Purpose
-------
Define the authoritative architecture for:
- CobolSharp's Intermediate Representation (IR)
- Control-flow graph (CFG) construction
- Data-flow analysis
- Constant folding & propagation
- Dead-code elimination
- Loop normalization
- PERFORM lowering optimizations
- Expression simplification
- Branch optimization
- CIL-friendly lowering
- Debugger-safe transformations

This document governs how CobolSharp represents COBOL programs as IR before CIL
generation, and how a future optimizer would transform that IR while preserving
COBOL semantics and debugging fidelity.

------------------------------------------------------------
SECTION 1 — OPTIMIZER OVERVIEW (target design)
------------------------------------------------------------

CobolSharp's planned optimizer is a **multi-stage** design built for:
- Deterministic transformations
- Debugger-safe behavior
- CIL-friendly output
- Preservation of COBOL semantics

Optimization stages (design):
1. IR construction
2. CFG construction
3. Data-flow analysis
4. Constant folding
5. Dead-code elimination
6. Loop normalization
7. Branch optimization
8. Expression simplification
9. CIL-specific lowering

> **Implementation note.** Of the above, only **IR construction (1)** and
> **CIL lowering (9, via the `*Lowerer` classes + the Cecil emitter)** exist today.
> Stages 2–8 are unimplemented. The optimizer is therefore a *forward design*; the
> sections that follow describe its intended shape.

------------------------------------------------------------
SECTION 2 — INTERMEDIATE REPRESENTATION (IR) — IMPLEMENTED
------------------------------------------------------------

2.1 IR goals
------------
CobolSharp IR must:
- Represent COBOL semantics precisely
- Support structured control flow
- Support PERFORM, GO TO, and declaratives
- Support numeric/string/JSON/XML operations
- Be easily lowered to CIL

2.2 IR structure
----------------
IR is composed of:
- IRProgram / IrModule
- IRSection
- IRParagraph
- IRBasicBlock
- IRInstruction (`IrInstruction.cs`)

2.3 IRInstruction categories
----------------------------
- Move
- Arithmetic
- Compare
- Branch
- Call/Invoke
- PerformEnter / PerformExit
- LoopBegin / LoopEnd
- JsonParse / JsonGenerate
- XmlParse / XmlGenerate
- FileIO operations
- Runtime service calls

2.4 SSA-like properties (optional)
----------------------------------
CobolSharp does **not** use full SSA, but:
- Temporary values are immutable
- Data items remain mutable (COBOL semantics)
- Expression trees may be SSA-like internally

2.5 The IR Expression Contract (M001) — IMPLEMENTED, see LIVE doc
----------------------------------------------------------------
A foundational invariant of the IR is that **`Semantics.Bound.BoundExpression`
nodes do NOT leak into the IR layer**. Arithmetic expressions, subscripts,
reference-modification boundaries, loop counts, and intrinsic-function arguments are
carried as the IR-native `IrExpression` hierarchy (`IrExpression.cs`):

```
IrExpression (abstract)
  +-- IrLiteral(decimal Value)
  +-- IrLoadNumeric(IrLocation Source)
  +-- IrBinaryExpr(IrArithmeticOp Op, IrExpression Left, IrExpression Right)
  +-- IrUnaryExpr(IrUnaryOp Op, IrExpression Operand)
  +-- IrIntrinsicCall(string FunctionName, IrFunctionArg[] Arguments)

IrFunctionArg (abstract)
  +-- IrNumericArg(IrExpression Expression)
  +-- IrAlphanumericArg(IrLocation Source)
  +-- IrLiteralStringArg(string Value)

enum IrArithmeticOp { Add, Subtract, Multiply, Divide, Remainder, Power }
enum IrUnaryOp      { Negate }
enum IrCompareOp    { Equal, NotEqual, Less, LessOrEqual, Greater, GreaterOrEqual }
```

`IrLoadNumeric` embeds a fully-resolved `IrLocation` (static / element / ref-mod /
cached), which **replaced** the old `ResolvedLocations` sidecar dictionaries — IR
instruction identity is no longer coupled to bound-node reference identity. The Binder
lowers `BoundExpression` to `IrExpression` (`Binder.LowerExpression`) and constructs
`IrBinaryExpr` directly for MULTIPLY/SUBTRACT/DIVIDE GIVING (no synthetic
`BoundBinaryExpression`). The CIL emitter walks the 5 `IrExpression` node types via
`EmitIrExpression`. The `InspectTallyKind`, `InspectReplaceKind`, `ClassConditionKind`
enums and `IrCompareOp` live in the IR namespace (no int-cast round-trip through
`BoundBinaryOperatorKind`).

**Full detail, verification grep-gates, and the staged completion record are in the
LIVE doc `docs/ir/IR-Expression-Contract.md` (M001 COMPLETE, all 4 stages).** This
section is a pointer; do not duplicate that doc here.

------------------------------------------------------------
SECTION 3 — CONTROL-FLOW GRAPH (CFG) — design (not yet built)
------------------------------------------------------------

3.1 CFG construction
--------------------
Each paragraph becomes:
- A node in the CFG
- With edges for:
  - Fall-through
  - PERFORM calls
  - GO TO targets
  - IF/EVALUATE branches

3.2 Structured regions
----------------------
CobolSharp identifies:
- Loops
- Conditionals
- PERFORM ranges
- Declarative handlers

3.3 CFG invariants
------------------
- No unreachable nodes (after DCE)
- No irreducible loops
- No critical edges (split if needed)

------------------------------------------------------------
SECTION 4 — DATA-FLOW ANALYSIS — design (not yet built)
------------------------------------------------------------

4.1 Analyses performed
----------------------
- Live variable analysis
- Reaching definitions
- Constant propagation
- Copy propagation
- Dead store elimination
- Nullability analysis (OO only)
- PERFORM stack correctness

4.2 COBOL-specific constraints
------------------------------
- Data items may alias via REDEFINES
- OCCURS DEPENDING ON affects bounds
- File buffers treated as opaque
- Packed decimal operations treated as side-effecting

> **Data-model note.** The data substrate is migrating to typed-native values
> (char→`string`, numeric→`long`/`decimal` via `CobolNum`/`CobolDecimal`,
> groups→`record struct`, OCCURS→`T[]`, pointers→`ManagedPointer`), gated behind
> `EnableTypedFields` (default OFF), with the byte/`StorageBlock` engine being
> *islanded*. A future data-flow analyzer must understand BOTH representations:
> typed-native fields have ordinary value/reference semantics, while byte-image fields
> (the REDEFINES/file/edited/ref-mod/EXTERNAL fallback) retain the aliasing constraints
> in §4.2. See `docs/DATA_MODEL_ARCHITECTURE.md` and `docs/RECORD_STRUCT_STORAGE_DESIGN.md`.

------------------------------------------------------------
SECTION 5 — CONSTANT FOLDING & PROPAGATION — design
------------------------------------------------------------

5.1 Foldable operations
-----------------------
- Numeric literals
- Arithmetic on literals
- Boolean expressions
- LENGTH OF literal
- FUNCTION calls with literal arguments (safe subset)
- numeric literal conversions; boolean comparisons with constants

5.2 Propagation rules
---------------------
- Propagate constants through expressions
- Propagate through MOVE
- Do not propagate through REDEFINES
- Do not propagate through OCCURS
- Supports local variables, temporaries, and simple static/literal field loads

5.3 Overflow detection
----------------------
Constant folding must:
- Detect overflow
- Trigger SIZE ERROR if applicable
- Emit diagnostic if compile-time overflow

5.4 Special COBOL handling
--------------------------
- Numeric literals folded using the numeric engine (`CobolNum`/`CobolDecimal`; the
  packed-decimal byte path is being islanded — fold against the typed substrate first)
- STRING/UNSTRING literal operations folded only when provably side-effect-free

------------------------------------------------------------
SECTION 6 — DEAD-CODE ELIMINATION (DCE) — design
------------------------------------------------------------

6.1 Removable constructs
------------------------
- Unreachable paragraphs
- Unreachable blocks after GO TO
- Dead temporary values
- Dead branches (IF TRUE / IF FALSE)
- Empty paragraphs (optional)
- Unused local variables; redundant assignments; no-op instructions

6.2 Non-removable constructs
----------------------------
- Declaratives
- Paragraphs referenced by ENTRY
- Paragraphs referenced by debugging metadata
- (COBOL-aware) statements with side effects — file I/O, JSON/XML, STRING/UNSTRING
- Moves that affect REDEFINES overlays
- Moves that affect condition names (88-levels)

------------------------------------------------------------
SECTION 7 — LOOP NORMALIZATION & LOOP OPTIMIZATION — design
------------------------------------------------------------

7.1 PERFORM UNTIL
-----------------
Normalized to:
```
loop:
    if (condition) break
    body
    goto loop
```

7.2 PERFORM VARYING
-------------------
Normalized to:
```
init
loop:
    if (condition) break
    body
    increment
    goto loop
```

7.3 PERFORM TIMES
-----------------
Normalized to:
```
i = 1
loop:
    if (i > n) break
    body
    i++
    goto loop
```

7.4 Loop optimizations
----------------------
On loops generated from PERFORM UNTIL / PERFORM VARYING:
- Loop-invariant code motion
- Induction-variable simplification
- Bounds-check hoisting (when safe)
- Early-exit detection

COBOL-aware: must preserve PERFORM semantics exactly; must not reorder file I/O or
runtime calls.

7.5 Benefits
------------
- Easier CFG analysis
- Cleaner CIL lowering
- Better branch optimization

------------------------------------------------------------
SECTION 8 — BRANCH OPTIMIZATION — design
------------------------------------------------------------

8.1 Simplifications
-------------------
- IF TRUE → unconditional branch
- IF FALSE → fall-through
- Remove redundant comparisons
- Merge consecutive / collapse nested branches
- Convert IF/ELSE to switch when possible
- Remove branches to the next instruction

8.2 EVALUATE optimization
-------------------------
If all WHEN values are numeric:
- Lower to a switch table
- Remove redundant comparisons

8.3 GO TO optimization
----------------------
- Remove GO TO to next paragraph
- Inline trivial GO TO chains

------------------------------------------------------------
SECTION 9 — EXPRESSION SIMPLIFICATION & STRENGTH REDUCTION — design
------------------------------------------------------------

9.1 Arithmetic simplification
-----------------------------
- x + 0 → x
- x - 0 → x
- x * 1 → x
- x * 0 → 0
- x / 1 → x

9.2 Boolean simplification
--------------------------
- TRUE AND x → x
- FALSE AND x → FALSE
- TRUE OR x → TRUE
- FALSE OR x → x

9.3 String simplification
-------------------------
- "" & x → x
- x & "" → x

9.4 Strength reduction
----------------------
- Multiplication by 2 → shift left (binary items only)
- Division by 2 → shift right (binary items only)
- Repeated ADD → ADD with constant

Packed-decimal operations are NOT strength-reduced unless provably safe.

9.5 Redundant move elimination
------------------------------
COBOL emits many semantically redundant MOVEs:
- `MOVE A TO A`
- `MOVE literal TO field` where literal equals the default value
- `MOVE field TO field` where both refer to the same REDEFINES region

These are removed only when provably safe (no 88-level / REDEFINES observer).

9.6 Copy propagation
--------------------
```
t1 = x ; y = t1      →      y = x
```

9.7 Peephole optimization
-------------------------
Local, pattern-based IL/IR sequence rewrites:
- `LOAD x; STORE x` → NOP
- `LOAD literal; ADD 0` → `LOAD literal`
- `LOAD x; LOAD x; COMPARE` → `DUP; COMPARE`

COBOL-aware peepholes cover packed-decimal ops, string slicing, and condition-name
evaluation.

------------------------------------------------------------
SECTION 10 — CIL-FRIENDLY LOWERING — IMPLEMENTED (Cecil)
------------------------------------------------------------

10.1 Structured lowering
------------------------
IR guarantees for the emitter:
- No irreducible loops
- No unstructured branches
- No overlapping exception regions
- No ambiguous PERFORM ranges

10.2 CIL emission
-----------------
IR is lowered to:
- CIL opcodes (emitted via **Mono.Cecil** — no custom VM, no interpreter)
- Runtime service calls
- Structured try/catch blocks
- Debugger sequence points

10.3 Debugger-safe transformations
----------------------------------
The optimizer must NOT:
- Reorder statements across paragraph boundaries
- Remove paragraph labels
- Remove sequence points
- Inline paragraphs (unless explicitly allowed)

------------------------------------------------------------
SECTION 11 — DEBUGGER INTEGRATION — design
------------------------------------------------------------

The debugger sees:
- Optimized but source-aligned code
- Preserved paragraph/section boundaries
- Preserved PERFORM structure
- Accurate sequence points
- Accurate variable lifetimes

------------------------------------------------------------
SECTION 12 — EDGE-CASE BEHAVIOR
------------------------------------------------------------

12.1 REDEFINES aliasing
-----------------------
Optimizer must assume:
- Any write may affect aliased fields
- No constant propagation across REDEFINES

12.2 OCCURS DEPENDING ON
------------------------
Bounds must be:
- Treated as dynamic
- Not constant-folded

12.3 GO TO into the middle of a paragraph
-----------------------------------------
Allowed; the optimizer must preserve block boundaries.

12.4 Declaratives
-----------------
Never removed or reordered.

12.5 JSON/XML operations
------------------------
Treated as side-effecting; cannot be reordered.

------------------------------------------------------------
SECTION 13 — PLANNED OPTIMIZATION PIPELINE (forward design)
------------------------------------------------------------

> This is the intended staged pipeline a future optimizer would slot between the IR and
> the Cecil backend. **None of these passes exist today** (the compiler lowers Bound → IR
> → CIL directly).

13.1 Pipeline placement
-----------------------
```
SemanticModel → Binder/Lowering → IR (unoptimized)
              → [Optimization Pipeline]            ← FUTURE
              → IR (optimized)
              → CIL backend (Mono.Cecil) → .NET Assembly + PDB
```
(The optimized object is the **IR** — there is no separate `ILModule` type.)

13.2 Passes, in execution order
-------------------------------
1. Control-flow simplification — remove unreachable/merge trivial blocks, remove
   redundant jumps, normalize PERFORM lowering, convert linear PERFORM chains to
   structured loops where safe.
2. Constant folding (§5)
3. Constant propagation (§5)
4. Copy propagation (§9.6)
5. Dead-code elimination (§6)
6. Redundant move elimination (§9.5)
7. Strength reduction (§9.4)
8. Loop optimization (§7.4)
9. Branch optimization (§8)
10. Peephole optimization (§9.7)
11. Generic specialization (optional) — ahead-of-time specialize COBOL-2023 generic
    methods/types when type args are known and code size does not explode.
12. Data-layout optimization (metadata-only) — collapse contiguous fields, drop unused
    metadata, normalize REDEFINES groups, precompute OCCURS bounds. Does **not** change
    runtime layout; improves debugger/tooling performance only.

Each pass would operate on IR basic blocks / instructions and be independently testable.

13.3 CIL-aware constraints (must always hold)
---------------------------------------------
- Verifiable IL
- Stack balance
- Type correctness
- Exception-region boundaries
- PDB sequence-point integrity

13.4 COBOL-aware constraints (must never change)
------------------------------------------------
- File-I/O ordering
- Numeric precision / rounding
- Packed-decimal semantics
- REDEFINES aliasing behavior
- OCCURS DEPENDING ON bounds
- Condition-name evaluation
- PERFORM / GO TO control flow

13.5 Testing strategy (when the pipeline is built)
--------------------------------------------------
- Per-pass unit tests
- Golden IR tests (before/after)
- CIL verification tests (Cecil/peverify oracle)
- Semantic-equivalence tests
- Cross-compiler behavior tests
- Regression suite (guard: unit + integration + NIST)

13.6 Performance strategy
-------------------------
- Operate on SSA-like IR where possible
- Cache the CFG between passes
- Compile peephole patterns into a fast matcher
- Parallelize across methods

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Optimizer & IR Architecture:
- Provides a structured, deterministic, **implemented** IR for COBOL (with the M001
  IrExpression contract complete; see `docs/ir/IR-Expression-Contract.md`)
- **Designs** (not yet built) CFG + data-flow analysis, constant folding, DCE, loop
  normalization, branch optimization, and a 12-pass optimization pipeline
- Preserves COBOL semantics and debugging fidelity
- Lowers cleanly to verifiable CIL via **Mono.Cecil** (CIL-only; no custom VM)
- Targets correctness across CoreCLR, AOT, and WASM
