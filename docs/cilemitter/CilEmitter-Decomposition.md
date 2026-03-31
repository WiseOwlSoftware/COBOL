# CilEmitter Decomposition (M003)

**Status:** Design document — awaiting approval
**Date:** 2026-03-30
**Ledger item:** M003 — Break CilEmitter.cs god class into focused emitters
**Prerequisites:** M001 (IrExpression) — complete, M002 (Binder decomposition) — complete

---

## 1. Current Responsibilities

`CilEmitter.cs` is a 4,083-line sealed class containing 87 methods and 16 fields.
It is the sole CIL emission pass in the compiler: it takes an `IrModule` (basic blocks,
IR instructions) and produces a .NET assembly via Mono.Cecil.

The class currently owns all of the following concerns:

- **Orchestration** — assembly creation, multi-program emission, module pipeline
- **Module setup** — type definitions, field definitions, method signatures, primitive
  type mapping, Entry method creation, LINKAGE field setup
- **Program state initialization** — ProgramState allocation, VALUE clause init,
  EXTERNAL storage init, ALTER table init, LOCAL-STORAGE snapshot, INITIAL reset
- **Instruction dispatch** — 65-case switch routing each `IrInstruction` to its handler
- **Control flow emission** — branches, PERFORM (simple, TIMES, inline TIMES, THRU),
  paragraph dispatch loops, GO TO DEPENDING, ALTER, STOP RUN, EXIT PROGRAM, GOBACK
- **Data movement emission** — MOVE string/figurative/field-to-field, MOVE ALL,
  loads/stores/moves of locals and fields
- **Arithmetic emission** — PIC add/subtract/multiply/divide (field and literal variants),
  accumulators, COMPUTE store, COBOL remainder, arithmetic status management
- **Comparison emission** — PIC compare (numeric, literal, accumulator), decimal compare,
  string compare (field, literal, with collating sequence), class/user-class conditions,
  compare-result-to-bool conversion
- **Expression emission** — IrExpression tree evaluation (M001 expressions), intrinsic
  function calls, decimal/PicDescriptor/byte-array literal emission
- **Location emission** — backing array loading (WS/LS/FS/EXTERNAL), location args
  (static, element, ref-mod, cached), LINKAGE location args, element address computation,
  reference modification address computation
- **String operation emission** — STRING statement, UNSTRING statement, INSPECT
  (tally/replace/convert), ACCEPT
- **File I/O emission** — WRITE, REWRITE, READ (sequential, previous, by key), DELETE,
  START, file status, AT END / INVALID KEY checks
- **Sort/Merge emission** — SORT init/release/sort/return/close, MERGE
- **Runtime call dispatch** — stringly-typed IrRuntimeCall routing (file operations,
  display, self-entry, program registration)
- **DISPLAY emission** — PIC display with operand concatenation
- **CALL emission** — CALL program (static/dynamic), CANCEL, exception checking

This violates single-responsibility. The class is difficult to navigate, test in
isolation, or extend without risk of unrelated regressions.

---

## 2. Method Inventory

### A. Orchestration / Entry Points (stays in CilEmitter)

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 1 | `CilEmitter()` | 37–41 | Constructor | **CilEmitter** |
| 2 | `EmitAssembly(IrModule, string, SemanticModel?)` | 45–49 | Static API | **CilEmitter** |
| 3 | `EmitAssembly(List<(IrModule, SemanticModel?)>, string)` | 56–84 | Static API | **CilEmitter** |
| 4 | `EmitModule(IrModule)` | 86–124 | Module pipeline | **CilEmitter** |
| 5 | `EmitEntryMethodBody(IrModule)` | 159–223 | Entry body orchestration | **CilEmitter** |
| 6 | `EmitMethodBody(IrMethod)` | 656–702 | Method body orchestration | **CilEmitter** |
| 7 | `EmitInstruction(...)` | 706–1202 | 65-case dispatch | **CilEmitter** |
| 8 | `EmitParagraphDispatchInline(...)` | 253–297 | Dispatch loop | **CilEmitter** |
| 9 | `EmitParagraphDispatch(...)` | 3442–3493 | Dispatch loop | **CilEmitter** |
| 10 | `EmitCall(...)` | 1328–1346 | IR call dispatch | **CilEmitter** |
| 11 | `EmitCallProgram(...)` | 1354–1468 | CALL program | **CilEmitter** |
| 12 | `EmitCheckCallException(...)` | 1470–1482 | CALL exception | **CilEmitter** |
| 13 | `EmitRuntimeCall(...)` | 3907–4016 | Runtime dispatch | **CilEmitter** |

**13 methods, ~1100 lines**

### B. Module / Type / Method Setup

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 14 | `SeedPrimitiveTypes()` | 572–583 | Type mapping | **CilModuleSetup** |
| 15 | `DefineType(IrType)` | 587–610 | Type definition | **CilModuleSetup** |
| 16 | `DefineGlobal(IrGlobal)` | 612–621 | Global definition | **CilModuleSetup** |
| 17 | `GetTypeRef(IrType)` | 623–629 | Type lookup | **CilModuleSetup** |
| 18 | `DefineMethodSignature(IrMethod)` | 633–652 | Method signature | **CilModuleSetup** |
| 19 | `CreateEntryMethodSignature(IrModule)` | 132–154 | Entry + LINKAGE fields | **CilModuleSetup** |
| 20 | `EmitAlternateEntryMethod(...)` | 229–247 | ENTRY statement | **CilModuleSetup** |

**7 methods, ~130 lines**

### C. Program State Initialization

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 21 | `EmitProgramState(IrModule)` | 302–339 | .cctor orchestration | **CilProgramStateEmitter** |
| 22 | `EmitProgramStateAllocation(...)` | 342–353 | ProgramState ctor | **CilProgramStateEmitter** |
| 23 | `EmitValueClauseInitialization(...)` | 356–426 | VALUE clause init | **CilProgramStateEmitter** |
| 24 | `ComputeOccursExtent(...)` | 429–451 | OCCURS helper | **CilProgramStateEmitter** |
| 25 | `EmitAlterTableInitialization(...)` | 454–474 | ALTER table init | **CilProgramStateEmitter** |
| 26 | `EmitResetStateMethod(...)` | 477–494 | INITIAL program reset | **CilProgramStateEmitter** |
| 27 | `EmitLocalStorageDefaultsSnapshot(...)` | 501–507 | LS defaults snapshot | **CilProgramStateEmitter** |
| 28 | `EmitExternalStorageInitialization(...)` | 513–543 | EXTERNAL init | **CilProgramStateEmitter** |
| 29 | `TryGetExternalField(...)` | 556–570 | EXTERNAL lookup | **CilProgramStateEmitter** |

**9 methods, ~270 lines**

### D. Control Flow Emission

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 30 | `EmitBranch(...)` | 1307–1315 | Branch | **CilControlFlowEmitter** |
| 31 | `EmitReturn(...)` | 1317–1326 | Return | **CilControlFlowEmitter** |
| 32 | `EmitPerform(...)` | 1534–1541 | PERFORM simple | **CilControlFlowEmitter** |
| 33 | `EmitPerformTimes(...)` | 1547–1595 | PERFORM TIMES | **CilControlFlowEmitter** |
| 34 | `EmitPerformInlineTimes(...)` | 1602–1642 | PERFORM inline TIMES | **CilControlFlowEmitter** |
| 35 | `EmitPerformThru(...)` | 1653–1722 | PERFORM THRU | **CilControlFlowEmitter** |
| 36 | `EmitGoToDepending(...)` | 2036–2077 | GO TO DEPENDING | **CilControlFlowEmitter** |

**7 methods, ~250 lines**

Note: Inline cases in `EmitInstruction` for IrJump, IrBranchIfFalse, IrReturnConst,
IrReturnAlterable, IrAlter, IrStopRun, IrExitProgram, IrGoBack, IrSetSwitch, IrTestSwitch
(~130 lines) will be delegated to CilControlFlowEmitter methods.

### E. Data Movement Emission

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 37 | `EmitLoadConst(...)` | 1204–1228 | Load constant | **CilDataEmitter** |
| 38 | `EmitLoadField(...)` | 1230–1241 | Load field | **CilDataEmitter** |
| 39 | `EmitStoreField(...)` | 1243–1251 | Store field | **CilDataEmitter** |
| 40 | `EmitMove(...)` | 1253–1261 | Move local | **CilDataEmitter** |
| 41 | `EmitMoveStringToField(...)` | 1728–1784 | MOVE string | **CilDataEmitter** |
| 42 | `EmitMoveWithStandardSignature(...)` | 1792–1806 | MOVE helper | **CilDataEmitter** |
| 43 | `EmitMoveFigurative(...)` | 1808–1819 | MOVE figurative | **CilDataEmitter** |
| 44 | `EmitMoveAllLiteral(...)` | 1824–1845 | MOVE ALL | **CilDataEmitter** |
| 45 | `EmitMoveFieldToField(...)` | 2249–2322 | MOVE field routing | **CilDataEmitter** |
| 46 | `EmitPicMoveLiteralNumeric(...)` | 2512–2523 | MOVE numeric literal | **CilDataEmitter** |
| 47 | `EmitPicDisplay(...)` | 3852–3887 | DISPLAY | **CilDataEmitter** |
| 48 | `EmitDisplayOperand(...)` | 3889–3905 | DISPLAY helper | **CilDataEmitter** |
| 49 | `EmitAccept(...)` | 2081–2090 | ACCEPT | **CilDataEmitter** |
| 50 | `EmitLocationLength(...)` | 1484–1490 | Location helper | **CilDataEmitter** |
| 51 | `EmitDefaultPicDescriptor(...)` | 1492–1523 | PIC helper | **CilDataEmitter** |
| 52 | `GetCobolDataPointerCtor()` | 1526–1532 | Ctor helper | **CilDataEmitter** |
| 53 | `EmitOptionalString(...)` | 2237–2243 | String helper | **CilDataEmitter** |

**17 methods, ~450 lines**

Note: Inline case for IrSetBool (~10 lines) will be delegated to CilDataEmitter.

### F. Arithmetic Emission

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 54 | `EmitBinary(...)` | 1263–1305 | Binary ops | **CilArithmeticEmitter** |
| 55 | `EmitPicMultiply(...)` | 2324–2340 | MULTIPLY | **CilArithmeticEmitter** |
| 56 | `EmitPicMultiplyLiteral(...)` | 2525–2540 | MULTIPLY literal | **CilArithmeticEmitter** |
| 57 | `EmitPicAdd(...)` | 2542–2555 | ADD | **CilArithmeticEmitter** |
| 58 | `EmitPicAddLiteral(...)` | 2557–2569 | ADD literal | **CilArithmeticEmitter** |
| 59 | `EmitPicSubtract(...)` | 2571–2584 | SUBTRACT | **CilArithmeticEmitter** |
| 60 | `EmitPicSubtractLiteral(...)` | 2586–2598 | SUBTRACT literal | **CilArithmeticEmitter** |
| 61 | `EmitAddAccumulatedToTarget(...)` | 2600–2614 | Accumulator add | **CilArithmeticEmitter** |
| 62 | `EmitMoveAccumulatedToTarget(...)` | 2616–2630 | Accumulator store | **CilArithmeticEmitter** |
| 63 | `EmitSubtractAccumulatedFromTarget(...)` | 2632–2646 | Accumulator sub | **CilArithmeticEmitter** |
| 64 | `EmitPicDivide(...)` | 2648–2664 | DIVIDE | **CilArithmeticEmitter** |
| 65 | `EmitPicDivideLiteral(...)` | 2666–2681 | DIVIDE literal | **CilArithmeticEmitter** |
| 66 | `EmitComputeStore(...)` | 2688–2701 | COMPUTE store | **CilArithmeticEmitter** |
| 67 | `EmitCobolRemainder(...)` | 2752–2773 | REMAINDER | **CilArithmeticEmitter** |
| 68 | `EnsureArithmeticStatusLocal(...)` | 3502–3511 | Status helper | **CilArithmeticEmitter** |
| 69 | `EmitInitArithmeticStatus(...)` | 3516–3522 | Status init | **CilArithmeticEmitter** |
| 70 | `EmitLoadArithmeticStatusRef(...)` | 3527–3531 | Status ref | **CilArithmeticEmitter** |

**17 methods, ~380 lines**

Note: Inline cases for IrBinaryLogical, IrInitArithmeticStatus, IrLoadSizeError,
IrInitAccumulator, IrAccumulateField, IrAccumulateLiteral, IrComputeIntoAccumulator
(~100 lines) will be delegated to CilArithmeticEmitter methods.

### G. Comparison Emission

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 71 | `EmitClassCondition(...)` | 2342–2377 | IS NUMERIC/ALPHABETIC | **CilComparisonEmitter** |
| 72 | `EmitUserClassCondition(...)` | 2379–2397 | User CLASS | **CilComparisonEmitter** |
| 73 | `EmitPicCompare(...)` | 2415–2434 | Numeric compare | **CilComparisonEmitter** |
| 74 | `EmitPicCompareLiteral(...)` | 2916–2935 | Numeric vs literal | **CilComparisonEmitter** |
| 75 | `EmitPicCompareAccumulator(...)` | 2937–2957 | Numeric vs accumulator | **CilComparisonEmitter** |
| 76 | `EmitDecimalCompare(...)` | 2959–2973 | Decimal compare | **CilComparisonEmitter** |
| 77 | `EmitDecimalCompareLiteral(...)` | 2975–2989 | Decimal vs literal | **CilComparisonEmitter** |
| 78 | `EmitCompareResultToBool(...)` | 2995–3035 | Result conversion | **CilComparisonEmitter** |
| 79 | `EmitStringCompareLiteral(...)` | 3037–3055 | String vs literal | **CilComparisonEmitter** |
| 80 | `EmitStringCompare(...)` | 3057–3076 | String vs string | **CilComparisonEmitter** |
| 81 | `EmitStringCompareWithSequence(...)` | 3078–3098 | String + sequence | **CilComparisonEmitter** |
| 82 | `EmitStringCompareLiteralWithSequence(...)` | 3100–3127 | String lit + seq | **CilComparisonEmitter** |

**12 methods, ~310 lines**

### H. Expression Emission

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 83 | `EmitIrExpression(...)` | 2779–2869 | Expression tree | **CilExpressionEmitter** |
| 84 | `EmitIrIntrinsicCall(...)` | 2874–2914 | Intrinsic call | **CilExpressionEmitter** |
| 85 | `EmitFunctionCall(...)` | 2708–2750 | Function call dispatch | **CilExpressionEmitter** |
| 86 | `EmitLoadDecimal(...)` | 2493–2508 | Decimal literal | **CilExpressionEmitter** |
| 87 | `EmitLoadPicDescriptor(...)` | 2439–2484 | PIC descriptor | **CilExpressionEmitter** |
| 88 | `EmitByteArrayLiteral(...)` | 2402–2413 | Byte array literal | **CilExpressionEmitter** |

**6 methods, ~210 lines**

### I. Location / Address Emission

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 89 | `EmitLocationArgs(...)` | 3645–3685 | Location dispatcher | **CilLocationEmitter** |
| 90 | `EmitLocationArgsWithPic(...)` | 3730–3734 | Location + PIC | **CilLocationEmitter** |
| 91 | `EmitCachedLocationArgs(...)` | 3692–3725 | Cached location | **CilLocationEmitter** |
| 92 | `EmitElementAddress(...)` | 3741–3773 | Subscript address | **CilLocationEmitter** |
| 93 | `EmitRefModAddress(...)` | 3779–3850 | Ref-mod address | **CilLocationEmitter** |
| 94 | `EmitLoadBackingArray(...)` | 3596–3617 | Backing array | **CilLocationEmitter** |
| 95 | `EmitLoadBackingArrayOrExternal(...)` | 3624–3634 | External-aware load | **CilLocationEmitter** |
| 96 | `EmitLinkageLocationArgs(...)` | 3537–3594 | LINKAGE location | **CilLocationEmitter** |

**8 methods, ~220 lines**

### J. String Operation Emission

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 97 | `EmitStringStatement(...)` | 3132–3246 | STRING | **CilStringEmitter** |
| 98 | `EmitUnstringStatement(...)` | 3248–3435 | UNSTRING | **CilStringEmitter** |
| 99 | `EmitInspectTally(...)` | 2128–2172 | INSPECT TALLYING | **CilStringEmitter** |
| 100 | `EmitInspectReplace(...)` | 2174–2217 | INSPECT REPLACING | **CilStringEmitter** |
| 101 | `EmitInspectConvert(...)` | 2219–2235 | INSPECT CONVERTING | **CilStringEmitter** |
| 102 | `EmitIrInspectPatternValue(...)` | 2098–2112 | INSPECT helper | **CilStringEmitter** |
| 103 | `EmitIrInspectPatternValueAsOptionalString(...)` | 2119–2126 | INSPECT helper | **CilStringEmitter** |

**7 methods, ~350 lines**

### K. File I/O Emission

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 104 | `EmitWriteRecordFromStorage(...)` | 1851–1865 | WRITE | **CilFileIoEmitter** |
| 105 | `EmitRewriteRecordFromStorage(...)` | 1873–1883 | REWRITE | **CilFileIoEmitter** |
| 106 | `EmitWriteAdvancing(...)` | 1885–1913 | WRITE ADVANCING | **CilFileIoEmitter** |
| 107 | `EmitReadRecordToStorage(...)` | 1915–1927 | READ | **CilFileIoEmitter** |
| 108 | `EmitReadPreviousToStorage(...)` | 1929–1941 | READ PREVIOUS | **CilFileIoEmitter** |
| 109 | `EmitReadByKey(...)` | 1943–1957 | READ BY KEY | **CilFileIoEmitter** |
| 110 | `EmitStoreFileStatus(...)` | 1962–1980 | File status | **CilFileIoEmitter** |
| 111 | `EmitCheckFileAtEnd(...)` | 1982–1998 | AT END | **CilFileIoEmitter** |
| 112 | `EmitDeleteRecord(...)` | 2002–2009 | DELETE | **CilFileIoEmitter** |
| 113 | `EmitStartFile(...)` | 2011–2020 | START | **CilFileIoEmitter** |
| 114 | `EmitCheckFileInvalidKey(...)` | 2022–2032 | INVALID KEY | **CilFileIoEmitter** |
| 115 | `EmitSortInit(...)` | 4020–4028 | SORT init | **CilFileIoEmitter** |
| 116 | `EmitSortRelease(...)` | 4030–4038 | SORT release | **CilFileIoEmitter** |
| 117 | `EmitSortSort(...)` | 4040–4048 | SORT sort | **CilFileIoEmitter** |
| 118 | `EmitSortReturn(...)` | 4050–4062 | SORT return | **CilFileIoEmitter** |
| 119 | `EmitSortClose(...)` | 4064–4071 | SORT close | **CilFileIoEmitter** |
| 120 | `EmitSortMerge(...)` | 4073–4082 | MERGE | **CilFileIoEmitter** |

**17 methods, ~280 lines**

### Summary

| Destination | Methods | Lines | % of total |
|-------------|---------|-------|------------|
| **CilEmitter** (orchestrator) | 13 | ~1100 | 27% |
| **CilModuleSetup** | 7 | ~130 | 3% |
| **CilProgramStateEmitter** | 9 | ~270 | 7% |
| **CilControlFlowEmitter** | 7 | ~250 | 6% |
| **CilDataEmitter** | 17 | ~450 | 11% |
| **CilArithmeticEmitter** | 17 | ~380 | 9% |
| **CilComparisonEmitter** | 12 | ~310 | 8% |
| **CilExpressionEmitter** | 6 | ~210 | 5% |
| **CilLocationEmitter** | 8 | ~220 | 5% |
| **CilStringEmitter** | 7 | ~350 | 9% |
| **CilFileIoEmitter** | 17 | ~280 | 7% |
| **Inline in EmitInstruction** | — | ~240 | 6% |
| **Total** | 120 | ~4190 | — |

Note: "Inline in EmitInstruction" refers to switch cases handled directly in the
dispatch method without a separate Emit* method. During extraction, these will become
delegate calls to the appropriate emitter class.

### Legacy Bound* Evaluation Check

**Zero BoundExpression references in CilEmitter.** M001 is fully complete — all
expressions use IR-native `IrExpression` types. No bound tree leakage remains.

---

## 3. Target Architecture

```
CilEmitter (orchestrator, ~1100 lines)
  │
  │  Owns: EmitAssembly(), EmitModule(), EmitInstruction() dispatch,
  │        EmitMethodBody(), EmitEntryMethodBody(), paragraph dispatch,
  │        EmitCallProgram, EmitRuntimeCall
  │
  │  Constructs EmissionContext, passes to all emitters.
  │
  ├── EmissionContext (record / sealed class)
  │     Shared state:
  │       - ModuleDefinition _module
  │       - Dictionary<IrMethod, MethodDefinition> _methodMap
  │       - Dictionary<IrField, FieldDefinition> _fieldMap
  │       - Dictionary<IrType, TypeReference> _typeMap
  │       - TypeDefinition? _programType
  │       - FieldDefinition? _programStateField
  │       - FieldDefinition? _alterTableField
  │       - Dictionary<string, FieldDefinition> _linkageFields
  │       - Dictionary<string, FieldDefinition> _externalFields
  │       - List<(int, int, FieldDefinition)> _externalRanges
  │       - MethodDefinition? _currentMethodDef
  │       - VariableDefinition? _arithmeticStatusLocal
  │       - Dictionary<int, (...)> _cachedLocationLocals
  │       - SemanticModel? _semanticModel
  │       - MethodDefinition? _entryMethod
  │       - MethodDefinition? _initializeStateMethod
  │       - FieldDefinition? _lastCallResultField
  │       - MethodReference? _cobolDataPointerCtor
  │     References to emitters (for cross-calls):
  │       - CilExpressionEmitter Expression
  │       - CilLocationEmitter Location
  │       - CilArithmeticEmitter Arithmetic
  │       - CilComparisonEmitter Comparison
  │       - CilDataEmitter Data
  │       - CilControlFlowEmitter ControlFlow
  │       - CilStringEmitter String
  │       - CilFileIoEmitter FileIo
  │       - CilProgramStateEmitter ProgramState
  │     Helper delegate:
  │       - EmitInstruction delegate (for recursive emission from inline TIMES)
  │
  ├── CilModuleSetup (~130 lines)
  │     SeedPrimitiveTypes, DefineType, DefineGlobal, GetTypeRef,
  │     DefineMethodSignature, CreateEntryMethodSignature, EmitAlternateEntryMethod
  │     Note: Methods called only during module setup, before instruction emission.
  │     Could fold into CilEmitter itself if too small.
  │
  ├── CilProgramStateEmitter (~270 lines)
  │     EmitProgramState, EmitProgramStateAllocation, EmitValueClauseInitialization,
  │     ComputeOccursExtent, EmitAlterTableInitialization, EmitResetStateMethod,
  │     EmitLocalStorageDefaultsSnapshot, EmitExternalStorageInitialization,
  │     TryGetExternalField
  │     Dependencies: EmissionContext (for _semanticModel, _programType, field refs)
  │
  ├── CilLocationEmitter (~220 lines)
  │     EmitLocationArgs, EmitLocationArgsWithPic, EmitCachedLocationArgs,
  │     EmitElementAddress, EmitRefModAddress, EmitLoadBackingArray,
  │     EmitLoadBackingArrayOrExternal, EmitLinkageLocationArgs
  │     Dependencies: EmissionContext, CilExpressionEmitter (for subscript/ref-mod)
  │
  ├── CilExpressionEmitter (~210 lines)
  │     EmitIrExpression, EmitIrIntrinsicCall, EmitFunctionCall,
  │     EmitLoadDecimal, EmitLoadPicDescriptor, EmitByteArrayLiteral
  │     Dependencies: EmissionContext, CilLocationEmitter (for IrLoadNumeric)
  │     Note: Mutual dependency with CilLocationEmitter (expression needs
  │     location for IrLoadNumeric; location needs expression for subscripts).
  │     Resolved via EmissionContext references.
  │
  ├── CilDataEmitter (~450 lines)
  │     EmitLoadConst, EmitLoadField, EmitStoreField, EmitMove,
  │     EmitMoveStringToField, EmitMoveWithStandardSignature,
  │     EmitMoveFigurative, EmitMoveAllLiteral, EmitMoveFieldToField,
  │     EmitPicMoveLiteralNumeric, EmitPicDisplay, EmitDisplayOperand,
  │     EmitAccept, EmitLocationLength, EmitDefaultPicDescriptor,
  │     GetCobolDataPointerCtor, EmitOptionalString
  │     Dependencies: CilLocationEmitter, CilExpressionEmitter
  │
  ├── CilArithmeticEmitter (~380 lines)
  │     EmitBinary, EmitPicMultiply, EmitPicMultiplyLiteral,
  │     EmitPicAdd, EmitPicAddLiteral, EmitPicSubtract, EmitPicSubtractLiteral,
  │     EmitAddAccumulatedToTarget, EmitMoveAccumulatedToTarget,
  │     EmitSubtractAccumulatedFromTarget, EmitPicDivide, EmitPicDivideLiteral,
  │     EmitComputeStore, EmitCobolRemainder,
  │     EnsureArithmeticStatusLocal, EmitInitArithmeticStatus,
  │     EmitLoadArithmeticStatusRef
  │     Dependencies: CilLocationEmitter, CilExpressionEmitter
  │
  ├── CilComparisonEmitter (~310 lines)
  │     EmitClassCondition, EmitUserClassCondition,
  │     EmitPicCompare, EmitPicCompareLiteral, EmitPicCompareAccumulator,
  │     EmitDecimalCompare, EmitDecimalCompareLiteral,
  │     EmitCompareResultToBool,
  │     EmitStringCompareLiteral, EmitStringCompare,
  │     EmitStringCompareWithSequence, EmitStringCompareLiteralWithSequence
  │     Dependencies: CilLocationEmitter, CilExpressionEmitter
  │
  ├── CilControlFlowEmitter (~250 lines)
  │     EmitBranch, EmitReturn, EmitPerform, EmitPerformTimes,
  │     EmitPerformInlineTimes, EmitPerformThru, EmitGoToDepending
  │     + extracted inline cases: IrJump, IrBranchIfFalse, IrReturnConst,
  │       IrReturnAlterable, IrAlter, IrStopRun, IrExitProgram, IrGoBack,
  │       IrSetSwitch, IrTestSwitch
  │     Dependencies: CilExpressionEmitter (for PERFORM TIMES count),
  │                   CilLocationEmitter (for GO TO DEPENDING selector),
  │                   EmitInstruction delegate (for inline TIMES body)
  │
  ├── CilStringEmitter (~350 lines)
  │     EmitStringStatement, EmitUnstringStatement,
  │     EmitInspectTally, EmitInspectReplace, EmitInspectConvert,
  │     EmitIrInspectPatternValue, EmitIrInspectPatternValueAsOptionalString
  │     Dependencies: CilLocationEmitter
  │
  └── CilFileIoEmitter (~280 lines)
        EmitWriteRecordFromStorage, EmitRewriteRecordFromStorage,
        EmitWriteAdvancing, EmitReadRecordToStorage, EmitReadPreviousToStorage,
        EmitReadByKey, EmitStoreFileStatus, EmitCheckFileAtEnd,
        EmitDeleteRecord, EmitStartFile, EmitCheckFileInvalidKey,
        EmitSortInit, EmitSortRelease, EmitSortSort, EmitSortReturn,
        EmitSortClose, EmitSortMerge
        Dependencies: CilLocationEmitter
```

---

## 4. Dependency Graph

```
                     EmissionContext
                          │
          ┌───────────────┼────────────────┐
          │               │                │
   CilModuleSetup  CilProgramStateEmitter  (setup phase only)
                          │
               ┌──────────┴──────────┐
               │                     │
        CilLocationEmitter  ←→  CilExpressionEmitter
               │                     │
    ┌──────┬───┴───┬─────────────────┤
    │      │       │                 │
CilData  CilArith  CilComparison  CilControlFlow
Emitter  Emitter   Emitter        Emitter
    │                                │
    │                                ├── EmitInstruction delegate
    │                                │   (for inline TIMES body)
    │                                │
CilString    CilFileIo
Emitter      Emitter
```

**One mutual dependency:** `CilLocationEmitter` calls `CilExpressionEmitter` (subscript
and ref-mod expressions), and `CilExpressionEmitter` calls `CilLocationEmitter`
(`IrLoadNumeric` pushes location args). This is resolved by both accessing each other
through `EmissionContext` references — no circular constructor dependency.

All other edges are strictly downward. `CilLocationEmitter` and `CilExpressionEmitter`
are at the bottom of the graph. Leaf emitters (`CilStringEmitter`, `CilFileIoEmitter`)
depend only on `CilLocationEmitter`.

---

## 5. Shared State (EmissionContext)

### Fields by owner

| Field | Type | Current Owner | EmissionContext? | Notes |
|-------|------|---------------|-----------------|-------|
| `_module` | `ModuleDefinition` | CilEmitter | Yes | Used everywhere |
| `_typeMap` | `Dict<IrType, TypeRef>` | CilEmitter | Yes | Module setup + GetTypeRef |
| `_fieldMap` | `Dict<IrField, FieldDef>` | CilEmitter | Yes | Module setup + EmitLoadField |
| `_methodMap` | `Dict<IrMethod, MethodDef>` | CilEmitter | Yes | Module setup + all PERFORM/CALL |
| `_programType` | `TypeDefinition?` | CilEmitter | Yes | Module setup + field creation |
| `_programStateField` | `FieldDefinition?` | CilEmitter | Yes | ProgramState + backing array |
| `_initializeStateMethod` | `MethodDefinition?` | CilEmitter | Yes | INITIAL programs |
| `_alterTableField` | `FieldDefinition?` | CilEmitter | Yes | ALTER + IrReturnAlterable |
| `_linkageFields` | `Dict<string, FieldDef>` | CilEmitter | Yes | LINKAGE location args |
| `_externalFields` | `Dict<string, FieldDef>` | CilEmitter | Yes | EXTERNAL lookup |
| `_externalRanges` | `List<(int,int,FieldDef)>` | CilEmitter | Yes | EXTERNAL lookup |
| `_currentMethodDef` | `MethodDefinition?` | CilEmitter | Yes | Per-method tracking |
| `_arithmeticStatusLocal` | `VariableDefinition?` | CilEmitter | Yes | Per-method, lazy |
| `_cachedLocationLocals` | `Dict<int, (...)>` | CilEmitter | Yes | Per-method, cleared |
| `_semanticModel` | `SemanticModel?` | CilEmitter | Yes | ProgramState + location |
| `_entryMethod` | `MethodDefinition?` | CilEmitter | Yes | Entry + RuntimeCall |
| `_lastCallResultField` | `FieldDefinition?` | CilEmitter | Yes | CALL program |
| `_cobolDataPointerCtor` | `MethodReference?` | CilEmitter | Yes | Lazy cached |

All 18 fields move to `EmissionContext`. No emitter creates its own mutable
collections. The context is the single source of shared mutable state.

---

## 6. Migration Strategy

### Consolidation note: CilModuleSetup

`CilModuleSetup` has only 7 methods and ~130 lines. These methods are called
exclusively during the module setup phase (steps 0-4 of `EmitModule`) and never
during instruction emission. **Option A:** Extract as a separate class. **Option B:**
Keep in `CilEmitter` since they're orchestration-adjacent. Recommend Option A for
consistency with M002, but the user may choose Option B.

### ~~Stage 1: Introduce `EmissionContext` and class skeletons~~

**DONE (2026-03-30):** Created `CodeGen/Emission/` with 11 files:
`EmissionContext.cs`, `CilModuleSetup.cs`, `CilProgramStateEmitter.cs`,
`CilControlFlowEmitter.cs`, `CilDataEmitter.cs`, `CilArithmeticEmitter.cs`,
`CilComparisonEmitter.cs`, `CilExpressionEmitter.cs`, `CilLocationEmitter.cs`,
`CilStringEmitter.cs`, `CilFileIoEmitter.cs`. EmissionContext has all 18 shared
state fields and 10 emitter references. CilEmitter constructor creates context +
all emitters. 53 structural tests added. 674 unit + 287 integration + 95 NIST pass.

### ~~Stage 2: Extract `CilLocationEmitter` and `CilExpressionEmitter`~~

**DONE (2026-03-30):** Moved 9 methods to `CilLocationEmitter` (8 location +
TryGetExternalField) and 6 methods to `CilExpressionEmitter`. Added
SyncToContext/SyncFromContext to bridge CilEmitter local fields to EmissionContext.
Unified ArithmeticStatusLocal via `_ctx` as single source of truth. 15 structural
tests added. 689 unit + 287 integration + 95 NIST pass.

### ~~Stage 3: Extract `CilComparisonEmitter`, `CilArithmeticEmitter`, `CilDataEmitter`~~

**DONE (2026-03-30):** Moved 12 comparison methods to `CilComparisonEmitter` (~310
lines), 17 arithmetic methods to `CilArithmeticEmitter` (~340 lines), 17 data
movement methods to `CilDataEmitter` (~420 lines). Added MethodMap/FieldMap/TypeMap
sync to SyncToContext. 46 structural tests added. 735 unit + 287 integration +
95 NIST pass.

### ~~Stage 4: Extract `CilControlFlowEmitter`, `CilStringEmitter`, `CilFileIoEmitter`~~

**DONE (2026-03-30):** Moved 7 control flow methods + extracted 10 inline cases to
`CilControlFlowEmitter` (~375 lines), 7 string methods to `CilStringEmitter` (~465
lines), 17 file I/O methods to `CilFileIoEmitter` (~270 lines). EmitInstruction
inline cases now delegate to `_ctx.ControlFlow.*`. 41 structural tests added.
776 unit + 287 integration + 95 NIST pass.

### ~~Stage 5: Cleanup~~

**DONE (2026-03-30):** Removed all forwarding wrappers from CilEmitter.
`EmitInstruction` now dispatches directly to `_ctx.Data.*`, `_ctx.Arithmetic.*`,
`_ctx.Comparison.*`, `_ctx.ControlFlow.*`, `_ctx.String.*`, `_ctx.FileIo.*`,
`_ctx.Expression.*`. ProgramState and ModuleSetup methods remain in CilEmitter
(orchestration-adjacent). CilEmitter reduced from 4,083 to **1,299 lines** (-68%).
Updated structural tests (no-wrapper verification). 765 unit + 287 integration +
95 NIST pass. M003 is **fully closed**.

---

## 7. Invariants

The following must remain true after **every stage**:

1. **No behavioral change.** The CIL output for any COBOL program must be
   bit-identical before and after each extraction. This is a pure refactor.

2. **No new public API.** All emitter classes are `internal sealed`. Only
   `CilEmitter` and `CilEmitter.EmitAssembly()` are public.

3. **No circular constructor dependencies.** The mutual reference between
   `CilLocationEmitter` and `CilExpressionEmitter` is resolved via
   `EmissionContext` — not direct constructor injection.

4. **Instruction dispatch stays in CilEmitter.** `EmitInstruction` is the
   orchestration point. It delegates to emitters but is not itself extracted.

5. **Shared mutable state is owned by `EmissionContext`.** No emitter creates
   its own mutable collections. The per-method fields (`_arithmeticStatusLocal`,
   `_cachedLocationLocals`) and per-emission fields all live on the context.

6. **All tests pass at every stage boundary.** No stage may be committed with
   any test failure. The test suite is the correctness oracle.

7. **No method is deleted.** Every method moves to its target class. Logic is
   reorganized, not rewritten.

---

## 8. Regression Test Plan

### Per-stage verification

After each class extraction:

| Suite | Count | What it covers |
|-------|-------|----------------|
| Unit tests | ~600 | Expression emission, IR contract, semantics, runtime |
| Integration tests | ~287 | End-to-end COBOL compilation + execution |
| NIST guard | 95 | Kernel COBOL-85 compliance (NC series) |

### Specific coverage per emitter

| Emitter | Key tests that exercise it |
|---------|---------------------------|
| CilLocationEmitter | SubscriptRefModTests, any test using subscripted data |
| CilExpressionEmitter | IrExpressionPipelineTests (13), COMPUTE tests |
| CilComparisonEmitter | ConditionTests, NC207A/NC208A/NC214A NIST |
| CilArithmeticEmitter | ArithmeticTests, NC201A/NC206A NIST |
| CilDataEmitter | DataTests, NC101A/NC109A/NC110A NIST, DisplayTests |
| CilControlFlowEmitter | ControlFlowTests, NC202A/NC203A/NC204A NIST |
| CilStringEmitter | StringTests, NC218A/NC219A NIST, InspectTests |
| CilFileIoEmitter | FileIOTests, SortMergeTests |
| CilProgramStateEmitter | All tests (ProgramState is always initialized) |

### Risk-weighted test priority

If a stage fails tests, diagnose by category:

1. **Crash / null ref** → EmissionContext field missing, or emitter not wired
2. **Arithmetic wrong** → CilArithmeticEmitter or CilExpressionEmitter wiring
3. **Control flow wrong** → CilControlFlowEmitter lost access to method map
4. **File I/O wrong** → CilFileIoEmitter lost location emission access
5. **Data movement wrong** → CilDataEmitter lost PIC descriptor access
6. **Subscript wrong** → CilLocationEmitter lost expression emission access

---

## 9. Validation Checklist

After all stages complete, verified:

- [x] `CilEmitter.cs` is 1,299 lines (orchestration + module setup + program state)
- [x] `EmitInstruction` is a thin switch that delegates to emitter methods
- [x] No emitter class exceeds 500 lines (largest: CilStringEmitter ~465)
- [x] No emitter imports `CilEmitter` (only `EmissionContext`)
- [x] No circular constructor references between emitter classes
- [x] `EmissionContext` contains all shared mutable state
- [x] All emitter classes are `internal sealed`
- [x] 765 unit tests pass
- [x] 287 integration tests pass
- [x] 95 NIST guard tests pass (ALL GREEN)
- [x] `grep -rn "class CilEmitter" src/CobolSharp.Compiler/CodeGen/` shows exactly 1 result
- [x] `wc -l src/CobolSharp.Compiler/CodeGen/CilEmitter.cs` = 1,299
- [x] `ls src/CobolSharp.Compiler/CodeGen/Emission/` shows 11 files (10 emitters + EmissionContext)
- [x] `modernization-ledger.json` has M003 status = "done"
- [x] `docs/cilemitter/CilEmitter-Decomposition.md` has all stages marked complete
