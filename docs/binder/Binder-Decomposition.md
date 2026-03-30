# Binder Decomposition (M002)

**Status:** Design document — approved for implementation
**Date:** 2026-03-30
**Ledger item:** M002 — Break Binder.cs god class into focused lowerers
**Prerequisite:** M001 (IrExpression) — complete

---

## 1. Current Responsibilities

`Binder.cs` is a 4,266-line sealed class containing 101 methods. It is the sole
lowering pass in the compiler: it takes a `BoundProgram` (typed, symbol-resolved
bound tree) and produces an `IrModule` (basic blocks, IR instructions, control flow).

The class currently owns all of the following concerns:

- **Orchestration** — entry point, paragraph stubs, module metadata, Main/Entry bootstrap
- **Symbol/location resolution** — resolving `DataSymbol` and `BoundIdentifierExpression`
  to `IrLocation` with subscript and OCCURS support
- **Expression lowering** — `BoundExpression` to `IrExpression` (M001, already implemented)
- **Statement dispatch** — 58-case switch routing each `BoundStatement` to its handler
- **Condition lowering** — boolean conditions, comparisons, class/sign tests, condition names
- **Control flow** — PERFORM (simple, TIMES, VARYING, inline), IF, EVALUATE, GO TO, ALTER,
  EXIT, SEARCH, SEARCH ALL
- **Arithmetic** — ADD, SUBTRACT, MULTIPLY, DIVIDE, COMPUTE, SIZE ERROR
- **Data movement** — MOVE, MOVE CORRESPONDING, INITIALIZE, SET
- **File I/O** — OPEN, CLOSE, READ, WRITE, REWRITE, DELETE, START, SORT, MERGE, RELEASE, RETURN
- **String operations** — STRING, UNSTRING, INSPECT
- **Miscellaneous** — DISPLAY, ACCEPT, CALL

This violates single-responsibility. The class is difficult to navigate, test in
isolation, or extend without risk of unrelated regressions.

---

## 2. Method Inventory

### A. Orchestration (stays in Binder)

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 1 | `Binder()` | 76–82 | Constructor | **Binder** |
| 2 | `Bind()` | 87–114 | Entry point | **Binder** |
| 3 | `CreateParagraphStubs` | 116–129 | Module setup | **Binder** |
| 4 | `ScanAlterTargets` | 131–146 | Pre-scan | **Binder** |
| 5 | `LowerAllParagraphs` | 148–195 | Paragraph loop | **Binder** |
| 6 | `PopulateModuleMetadata` | 197–210 | Module metadata | **Binder** |
| 7 | `BuildRecordTypes` | 214–221 | Record types | **Binder** |
| 8 | `CreateEntryPoint` | 225–376 | Main/Entry bootstrap | **Binder** |
| 9 | `LowerStatement` | 380–497 | 58-case dispatch | **Binder** |

**9 methods, ~500 lines**

### B. Symbol / Location Resolution

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 10 | `ResolveLocation(BoundIdentifierExpression)` | 509–593 | Subscript resolution | **LocationResolver** |
| 11 | `ComputeMultipliers` | 605–612 | OCCURS multipliers | **LocationResolver** |
| 12 | `ResolveLocation(DataSymbol)` | 620–625 | Simple data resolution | **LocationResolver** |
| 13 | `ResolveExpressionLocation` | 633–641 | Expression dispatch | **LocationResolver** |
| 14 | `ResolveRefModLocation` | 643–660 | Ref-mod resolution | **LocationResolver** |

**5 methods, ~150 lines**

### C. Expression Lowering

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 15 | `LowerExpression` | 695–782 | BoundExpr → IrExpr | **ExpressionLowerer** |
| 16 | `TryEvalConstant` | 2425–2450 | Constant folding | **ExpressionLowerer** |
| 17 | `TryExtractNegativeLiteral` | 1223–1235 | Negative literal detection | **ExpressionLowerer** |
| 18 | `FormatLiteralForAlphanumeric(string)` | 668–678 | Literal formatting | **ExpressionLowerer** |
| 19 | `FormatLiteralForAlphanumeric(decimal)` | 680–685 | Literal formatting | **ExpressionLowerer** |

**5 methods, ~120 lines**

### D. Condition / Comparison Lowering

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 20 | `LowerCondition` | 3116–3234 | Condition dispatch | **ConditionLowerer** |
| 21 | `LowerComparison` | 3239–3365 | Comparison matrix | **ConditionLowerer** |
| 22 | `NormalizeOperand` | 3045–3087 | Operand normalization | **ConditionLowerer** |
| 23 | `ComparisonOperandKind` (enum) | 3014 | Nested type | **ConditionLowerer** |
| 24 | `ComparisonOperand` (class + factories) | 3016–3040 | Nested type | **ConditionLowerer** |
| 25 | `IsNumericComparison` | 3374–3381 | Classification | **ConditionLowerer** |
| 26 | `IsStrictlyNumeric` | 3386–3399 | Classification | **ConditionLowerer** |
| 27 | `EmitLocationVsFigurative` | 3404–3421 | Figurative comparison | **ConditionLowerer** |
| 28 | `EvaluateComparisonResult` | 3426–3438 | Compile-time eval | **ConditionLowerer** |
| 29 | `FlipComparisonOp` | 3440–3450 | Operator flip | **ConditionLowerer** |
| 30 | `MakeFigurativeString` | 3092–3114 | Figurative padding | **ConditionLowerer** |
| 31 | `LowerSignCondition` | 3458–3482 | SIGN IS test | **ConditionLowerer** |
| 32 | `LowerClassCondition` | 3484–3507 | IS NUMERIC/ALPHABETIC | **ConditionLowerer** |
| 33 | `LowerUserClassCondition` | 3509–3531 | User CLASS | **ConditionLowerer** |
| 34 | `LowerConditionName` | 3535–3651 | 88-level conditions | **ConditionLowerer** |
| 35 | `LowerConditionalBranch` | 1587–1618 | IF/WHEN branch emit | **ConditionLowerer** |

**16 methods + 2 nested types, ~650 lines**

### E. Control Flow Lowering

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 36 | `LowerPerform` | 967–1050 | PERFORM dispatch | **ControlFlowLowerer** |
| 37 | `LowerPerformVarying` | 1058–1160 | PERFORM VARYING | **ControlFlowLowerer** |
| 38 | `EmitVaryingMove` | 1162–1187 | VARYING index init | **ControlFlowLowerer** |
| 39 | `EmitVaryingAdd` | 1189–1217 | VARYING step | **ControlFlowLowerer** |
| 40 | `LowerPerformBody` | 1237–1254 | PERFORM body | **ControlFlowLowerer** |
| 41 | `LowerPerformSimple` | 1256–1296 | Simple PERFORM | **ControlFlowLowerer** |
| 42 | `LowerPerformTimes` | 1302–1357 | PERFORM N TIMES | **ControlFlowLowerer** |
| 43 | `LowerInlinePerformTimes` | 1369–1389 | Inline PERFORM N TIMES | **ControlFlowLowerer** |
| 44 | `LowerIf` | 2778–2813 | IF/ELSE | **ControlFlowLowerer** |
| 45 | `LowerEvaluate` | 2817–2857 | EVALUATE | **ControlFlowLowerer** |
| 46 | `LowerEvaluateWhenMatch` | 2864–2911 | WHEN clause | **ControlFlowLowerer** |
| 47 | `LowerEvaluateSubjectMatch` | 2917–2998 | Subject/object match | **ControlFlowLowerer** |
| 48 | `LowerEvaluateComparison` | 3000–3006 | EVALUATE comparison | **ControlFlowLowerer** |
| 49 | `LowerGoTo` | 3669–3735 | GO TO | **ControlFlowLowerer** |
| 50 | `LowerAlter` | 3655–3665 | ALTER | **ControlFlowLowerer** |
| 51 | `LowerNextSentence` | 3739–3752 | NEXT SENTENCE | **ControlFlowLowerer** |
| 52 | `LowerExitPerform` | 3756–3786 | EXIT PERFORM | **ControlFlowLowerer** |
| 53 | `LowerExitParagraph` | 3788–3801 | EXIT PARAGRAPH | **ControlFlowLowerer** |
| 54 | `LowerExitSection` | 3803–3818 | EXIT SECTION | **ControlFlowLowerer** |
| 55 | `LowerSearch` | 4005–4097 | SEARCH (linear) | **ControlFlowLowerer** |
| 56 | `LowerSearchAll` | 4101–4149 | SEARCH ALL (binary) | **ControlFlowLowerer** |
| 57 | `EmitBinarySearchNode` | 4156–4303 | Binary search tree | **ControlFlowLowerer** |
| 58 | `ExtractFirstRelationalComparison` | 4310–4325 | SEARCH ALL extraction | **ControlFlowLowerer** |

**23 methods, ~1350 lines**

### F. Arithmetic Lowering

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 59 | `LowerArithmetic` | 2454–2463 | Dispatch | **ArithmeticLowerer** |
| 60 | `LowerAdd` | 2699–2743 | ADD | **ArithmeticLowerer** |
| 61 | `LowerSubtract` | 2511–2566 | SUBTRACT | **ArithmeticLowerer** |
| 62 | `LowerMultiply` | 2465–2507 | MULTIPLY | **ArithmeticLowerer** |
| 63 | `LowerDivide` | 2570–2674 | DIVIDE / REMAINDER | **ArithmeticLowerer** |
| 64 | `LowerCompute` | 2678–2695 | COMPUTE | **ArithmeticLowerer** |
| 65 | `LowerSizeError` | 2747–2774 | ON SIZE ERROR | **ArithmeticLowerer** |

**7 methods, ~330 lines**

### G. Data Movement Lowering

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 66 | `LowerMove` | 877–963 | MOVE | **DataMovementLowerer** |
| 67 | `LowerCorresponding` | 833–873 | MOVE CORRESPONDING | **DataMovementLowerer** |
| 68 | `LowerInitialize` | 2150–2154 | INITIALIZE | **DataMovementLowerer** |
| 69 | `InitializeDataItem` | 2156–2203 | Recursive init | **DataMovementLowerer** |
| 70 | `ClassifyInitializeCategory` | 2205–2215 | Category mapping | **DataMovementLowerer** |
| 71 | `EmitInitializeAssignment` | 2217–2243 | Init value emit | **DataMovementLowerer** |
| 72 | `LowerSetCondition` | 2314–2355 | SET condition | **DataMovementLowerer** |
| 73 | `LowerSetIndex` | 2357–2418 | SET index | **DataMovementLowerer** |
| 74 | `FigurativeToStringHelper` | 3822–3834 | Figurative helper | **DataMovementLowerer** |

**9 methods, ~400 lines**

### H. File I/O Lowering

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 75 | `LowerOpen` | 1444–1469 | OPEN | **FileIoLowerer** |
| 76 | `LowerClose` | 1473–1497 | CLOSE | **FileIoLowerer** |
| 77 | `LowerRead` | 1501–1579 | READ | **FileIoLowerer** |
| 78 | `LowerWrite` | 1393–1440 | WRITE | **FileIoLowerer** |
| 79 | `LowerRewrite` | 1700–1719 | REWRITE | **FileIoLowerer** |
| 80 | `LowerDelete` | 1723–1738 | DELETE | **FileIoLowerer** |
| 81 | `LowerStart` | 1742–1786 | START | **FileIoLowerer** |
| 82 | `EmitFileStatus` | 1624–1635 | File status emit | **FileIoLowerer** |
| 83 | `EmitUseDeclarative` | 1642–1696 | USE AFTER handler | **FileIoLowerer** |
| 84 | `LowerSort` | 1858–1908 | SORT | **FileIoLowerer** |
| 85 | `EmitSortUsingFile` | 1910–1956 | SORT USING | **FileIoLowerer** |
| 86 | `EmitSortGivingFile` | 1958–2003 | SORT GIVING | **FileIoLowerer** |
| 87 | `BuildKeysSpec` | 2005–2028 | SORT key spec | **FileIoLowerer** |
| 88 | `LowerMerge` | 2032–2072 | MERGE | **FileIoLowerer** |
| 89 | `LowerRelease` | 2076–2095 | RELEASE | **FileIoLowerer** |
| 90 | `LowerReturn` | 1790–1854 | RETURN | **FileIoLowerer** |

**16 methods, ~700 lines**

### I. String Lowering

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 91 | `LowerInspect` | 2256–2294 | INSPECT | **StringLowerer** |
| 92 | `LowerInspectPattern` | 2300–2310 | INSPECT pattern | **StringLowerer** |
| 93 | `LowerString` | 3836–3908 | STRING | **StringLowerer** |
| 94 | `LowerUnstring` | 3912–4001 | UNSTRING | **StringLowerer** |

**4 methods, ~250 lines**

### J. Stays in Binder (simple, not worth extracting)

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 95 | `LowerDisplay` | 786–829 | DISPLAY | **Binder** |
| 96 | `LowerAccept` | 2247–2252 | ACCEPT | **Binder** |
| 97 | `LowerCall` | 2099–2146 | CALL | **Binder** |

**3 methods, ~100 lines — too simple or cross-cutting to extract**

### Summary

| Destination | Methods | Lines | % of total |
|-------------|---------|-------|------------|
| **Binder** (orchestrator) | 12 | ~600 | 14% |
| **LocationResolver** | 5 | ~150 | 4% |
| **ExpressionLowerer** | 5 | ~120 | 3% |
| **ConditionLowerer** | 16 | ~650 | 15% |
| **ControlFlowLowerer** | 23 | ~1350 | 32% |
| **ArithmeticLowerer** | 7 | ~330 | 8% |
| **DataMovementLowerer** | 9 | ~400 | 9% |
| **FileIoLowerer** | 16 | ~700 | 16% |
| **StringLowerer** | 4 | ~250 | 6% |
| **Total** | 97 | ~4550 | — |

(4 remaining methods are nested types/factories counted with their parent class.)

---

## 3. Target Architecture

```
Binder (orchestrator, ~600 lines)
  │
  │  Owns: Bind(), LowerStatement() dispatch, module bootstrap,
  │        paragraph stubs, LowerDisplay, LowerAccept, LowerCall
  │
  │  Constructs LoweringContext, passes to all lowerers.
  │
  ├── LoweringContext (record / sealed class)
  │     Shared mutable state:
  │       - SemanticModel _semantic
  │       - IrValueFactory _valueFactory
  │       - DiagnosticBag _diagnostics
  │       - Dictionary<string, IrMethod> _paragraphMethods
  │       - Dictionary<string, int> _paragraphIndices
  │       - List<string> _paragraphsByIndex
  │       - Dictionary<string, int> _alterSlots
  │       - List<int> _alterDefaults
  │       - Stack<IrBasicBlock> _performExitStack
  │       - Stack<IrBasicBlock> _performContinueStack
  │       - IrBasicBlock? _currentSentenceEnd
  │       - IrBasicBlock? _paragraphEndBlock
  │       - int? _sectionExitIndex
  │       - string? _currentParagraphName
  │       - CompilationOptions _options
  │     References to lowerers (for cross-calls):
  │       - LocationResolver Location
  │       - ExpressionLowerer Expression
  │       - ConditionLowerer Condition
  │       - ControlFlowLowerer ControlFlow
  │       - ArithmeticLowerer Arithmetic
  │       - DataMovementLowerer DataMovement
  │       - FileIoLowerer FileIo
  │       - StringLowerer String
  │     Helper:
  │       - LowerStatement(stmt, method, block) delegate
  │         (for recursive lowering from within extractd classes)
  │
  ├── LocationResolver (~150 lines)
  │     ResolveLocation (2 overloads), ResolveExpressionLocation,
  │     ResolveRefModLocation, ComputeMultipliers
  │     Dependencies: LoweringContext (for _semantic)
  │
  ├── ExpressionLowerer (~120 lines)
  │     LowerExpression, TryEvalConstant, TryExtractNegativeLiteral,
  │     FormatLiteralForAlphanumeric (2 overloads)
  │     Dependencies: LocationResolver
  │
  ├── ConditionLowerer (~650 lines)
  │     LowerCondition, LowerComparison, NormalizeOperand,
  │     ComparisonOperand (nested type), LowerSignCondition,
  │     LowerClassCondition, LowerUserClassCondition,
  │     LowerConditionName, LowerConditionalBranch,
  │     comparison helpers (IsNumericComparison, FlipComparisonOp, etc.)
  │     Dependencies: LocationResolver, ExpressionLowerer
  │
  ├── ControlFlowLowerer (~1350 lines)
  │     LowerPerform (all variants), LowerIf, LowerEvaluate (all),
  │     LowerGoTo, LowerAlter, LowerNextSentence,
  │     LowerExitPerform/Paragraph/Section,
  │     LowerSearch, LowerSearchAll, EmitBinarySearchNode
  │     Dependencies: ConditionLowerer, ExpressionLowerer,
  │                   LocationResolver, LowerStatement delegate
  │
  ├── ArithmeticLowerer (~330 lines)
  │     LowerArithmetic (dispatch), LowerAdd, LowerSubtract,
  │     LowerMultiply, LowerDivide, LowerCompute, LowerSizeError
  │     Dependencies: ExpressionLowerer, LocationResolver,
  │                   ConditionLowerer (for SIZE ERROR branch)
  │
  ├── DataMovementLowerer (~400 lines)
  │     LowerMove, LowerCorresponding, LowerInitialize (+ recursive),
  │     LowerSetCondition, LowerSetIndex, FigurativeToStringHelper
  │     Dependencies: LocationResolver, ExpressionLowerer
  │
  ├── FileIoLowerer (~700 lines)
  │     LowerOpen, LowerClose, LowerRead, LowerWrite, LowerRewrite,
  │     LowerDelete, LowerStart, EmitFileStatus, EmitUseDeclarative,
  │     LowerSort (+ Using/Giving helpers), LowerMerge,
  │     LowerRelease, LowerReturn, BuildKeysSpec
  │     Dependencies: LocationResolver, ConditionLowerer (for READ AT END)
  │
  └── StringLowerer (~250 lines)
        LowerInspect (+ pattern helper), LowerString, LowerUnstring
        Dependencies: LocationResolver
```

---

## 4. Dependency Graph

```
                    LoweringContext
                         │
         ┌───────────────┼───────────────┐
         │               │               │
    LocationResolver  IrValueFactory  SemanticModel
         │
    ┌────┴────┐
    │         │
ExpressionLowerer    (leaf)
    │
    ├─────────────────┐
    │                 │
ConditionLowerer   DataMovementLowerer    (leaf)
    │
    ├─────────────┐
    │             │
ControlFlowLowerer  ArithmeticLowerer
    │
    ├──────────┐
    │          │
FileIoLowerer  StringLowerer    (leaf)
```

**No circular dependencies.** `LocationResolver` and `ExpressionLowerer` are at the
bottom of the dependency graph. `ControlFlowLowerer` is the most connected class
because PERFORM/EVALUATE/SEARCH all need conditions, expressions, and recursive
statement lowering.

The recursive `LowerStatement` call (needed when `ControlFlowLowerer` lowers IF
bodies or PERFORM inline statements) is handled via a delegate on `LoweringContext`,
not by making `ControlFlowLowerer` depend on `Binder`.

---

## 5. Migration Strategy

### ~~Stage 1: Introduce `LoweringContext` and class skeletons~~

**DONE (2026-03-30):** Created `CodeGen/Lowering/` with 9 files:
`LoweringContext.cs`, `LocationResolver.cs`, `ExpressionLowerer.cs`,
`ConditionLowerer.cs`, `ControlFlowLowerer.cs`, `ArithmeticLowerer.cs`,
`DataMovementLowerer.cs`, `FileIoLowerer.cs`, `StringLowerer.cs`.
`LoweringContext` has all shared state fields, lowerer references, and
`LowerStatement` delegate. Binder constructor creates context + all lowerers.
TODO markers added to all 30+ method sections that will move.
76 structural tests in `BinderDecompositionTests.cs`.
529 unit + 287 integration tests pass.

### ~~Stage 2: Extract `LocationResolver` and `ExpressionLowerer`~~

**DONE (2026-03-30):** Moved 5 methods to `LocationResolver` (ResolveLocation x2,
ResolveExpressionLocation, ResolveRefModLocation, ComputeMultipliers) and 5 methods
to `ExpressionLowerer` (LowerExpression, TryEvalConstant, TryExtractNegativeLiteral,
FormatLiteralForAlphanumeric x2). Binder retains thin forwarding methods that
delegate to `_ctx.Location.*` and `_ctx.Expression.*`. 8 structural tests added.
537 unit + 287 integration + 95 NIST guard all pass.

### ~~Stage 3: Extract `ConditionLowerer`, `ArithmeticLowerer`, `DataMovementLowerer`~~

**DONE (2026-03-30):** Moved 16 condition methods + 2 nested types to `ConditionLowerer`
(615 lines). Moved 7 arithmetic methods to `ArithmeticLowerer` (292 lines). Moved 9
data-movement methods to `DataMovementLowerer` (335 lines). Binder reduced from 4266
to 2726 lines (-36%). 28 structural tests added. 560 unit + 287 integration + 95 NIST
guard all pass.

### ~~Stage 4: Extract `ControlFlowLowerer`, `FileIoLowerer`, `StringLowerer`~~

**DONE (2026-03-30):** Moved 23 methods to `ControlFlowLowerer` (1154 lines), 16
methods to `FileIoLowerer` (688 lines), 4 methods to `StringLowerer` (240 lines).
Binder reduced from 2726 to 688 lines (total reduction from 4266: **84%**).
Orchestration fields (`_paragraphMethods`, `_alterSlots`, stacks, tracking vars)
now accessed via `_ctx.*` in both Binder and lowerers. 31 structural tests added.
591 unit + 287 integration + 95 NIST guard all pass.

### ~~Stage 5: Cleanup~~

**DONE (2026-03-30):** Removed all 35 forwarding wrappers from Binder. `LowerStatement`
now dispatches directly to `_ctx.ControlFlow.*`, `_ctx.FileIo.*`, `_ctx.String.*`,
`_ctx.DataMovement.*`, `_ctx.Arithmetic.*`. Binder reduced to **579 lines** — contains
only: constructor, `Bind()`, `CreateParagraphStubs`, `ScanAlterTargets`,
`LowerAllParagraphs`, `PopulateModuleMetadata`, `BuildRecordTypes`, `CreateEntryPoint`,
`LowerStatement` (dispatch), `LowerDisplay`, `LowerAccept`, `LowerCall`.
25 structural tests added (no-wrapper + no-Binder-reference). 596 unit + 287
integration + 95 NIST guard all pass. M002 is **fully closed**.

---

## 6. Invariants

The following must remain true after **every stage**:

1. **No behavioral change.** The IR output for any COBOL program must be
   bit-identical before and after each extraction. This is a pure refactor.

2. **No new public API.** All lowerer classes are `internal sealed`. Only `Binder`
   and `Binder.Bind()` are public.

3. **No circular dependencies.** The dependency graph in section 4 must hold.
   No lowerer may reference `Binder` directly — only through `LoweringContext`.

4. **Statement dispatch stays in Binder.** `LowerStatement` is the orchestration
   point. It delegates to lowerers but is not itself extracted.

5. **Shared mutable state is owned by `LoweringContext`.** No lowerer creates its
   own mutable collections. The stacks (`_performExitStack`, `_performContinueStack`),
   the current-paragraph tracking, and the value factory all live on the context.

6. **All tests pass at every stage boundary.** No stage may be committed with
   any test failure. The test suite is the correctness oracle.

7. **No method is deleted.** Every method moves to its target class. Logic is
   reorganized, not rewritten. If a method needs adaptation (e.g., accessing
   `_semantic` through `_ctx` instead of a field), that is a mechanical change.

---

## 7. Regression Test Plan

### Per-stage verification

After each class extraction:

| Suite | Count | What it covers |
|-------|-------|----------------|
| Unit tests | 453 | LowerExpression (19), IR contract (13), semantics, runtime |
| Integration tests | 287 | End-to-end COBOL compilation + execution, IrExpression pipeline (13) |
| NIST guard | 95 | Kernel COBOL-85 compliance (NC series) |

### Specific coverage per lowerer

| Lowerer | Key tests that exercise it |
|---------|---------------------------|
| LocationResolver | SubscriptRefModTests (10+), any test using subscripted data |
| ExpressionLowerer | LowerExpressionTests (19), IrExpressionPipelineTests (13) |
| ConditionLowerer | ConditionTests (30+), NC207A/NC208A/NC214A NIST tests |
| ControlFlowLowerer | ControlFlowTests (20+), NC202A/NC203A/NC204A NIST tests |
| ArithmeticLowerer | ArithmeticTests (20+), NC201A/NC206A NIST tests |
| DataMovementLowerer | DataTests (30+), NC101A/NC109A/NC110A NIST tests |
| FileIoLowerer | FileIOTests (10+), SortMergeTests (5+) |
| StringLowerer | StringTests (15+), NC218A/NC219A NIST tests |

### Risk-weighted test priority

If a stage fails tests, diagnose by category:

1. **Arithmetic wrong** → ArithmeticLowerer or ExpressionLowerer wiring
2. **Control flow wrong** → ControlFlowLowerer lost access to stacks or paragraph map
3. **File I/O wrong** → FileIoLowerer lost access to file symbols or status emission
4. **Condition wrong** → ConditionLowerer lost access to comparison normalization
5. **Crash / null ref** → LocationResolver not wired, or LoweringContext field missing

---

## 8. Validation Checklist

After all 5 stages are complete, verify:

- [ ] `Binder.cs` is under 700 lines
- [ ] `LowerStatement` is a thin switch that delegates to lowerer methods
- [ ] No lowerer class exceeds 1400 lines
- [ ] No lowerer imports `Binder` (only `LoweringContext`)
- [ ] No circular references between lowerer classes
- [ ] `LoweringContext` contains all shared mutable state
- [ ] All 8 lowerer classes are `internal sealed`
- [ ] 453 unit tests pass
- [ ] 287 integration tests pass
- [ ] 95 NIST guard tests pass (ALL GREEN)
- [ ] `grep -rn "class Binder" src/CobolSharp.Compiler/CodeGen/` shows exactly 1 result
- [ ] `wc -l src/CobolSharp.Compiler/CodeGen/Binder.cs` is under 700
- [ ] `ls src/CobolSharp.Compiler/CodeGen/Lowering/` shows 9 files (8 lowerers + LoweringContext)
- [ ] `modernization-ledger.json` has M002 status = "done"
- [ ] `docs/binder/Binder-Decomposition.md` has all stages marked complete
