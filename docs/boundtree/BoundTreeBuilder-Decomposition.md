# BoundTreeBuilder Decomposition (M004)

**Status:** IMPLEMENTATION COMPLETE (DEVLOG 441). The decomposition described here has been fully implemented as of 2026-06-07.
**Date:** 2026-03-30 (design) · **COMPLETED:** 2026-06-07
**Ledger item:** M004 — Break BoundTreeBuilder.cs god class into focused binders
**Prerequisites:** M001 (IrExpression) — complete, M002 (Binder) — complete, M003 (CilEmitter → 11 emitters) — complete

> The 9 focused binder classes (ArithmeticStatementBinder, CallBinder, ConditionBinder, ControlFlowBinder, DataStatementBinder, ExpressionBinder, FileIoBinder, StringStatementBinder, ProcedureNameResolver) + the slim ~234-line BoundTreeBuilder orchestrator are live in `src/CobolSharp.Compiler/Semantics/Bound/Binding/` and passing all guard tests.

---

## 1. Current Responsibilities

`BoundTreeBuilder.cs` is a 4,428-line sealed class containing ~110 methods and 4 fields.
It is the sole binding pass in the compiler: it takes an ANTLR parse tree and produces
a `BoundProgram` (typed, symbol-resolved bound tree). The class extends
`CobolParserCoreBaseVisitor<object?>` and uses the visitor pattern for paragraphs and
declaratives, but dispatches statement binding via a 40-case `BindStatement` method.

The class currently owns all of the following concerns:

- **Orchestration** — Build(), VisitParagraphDefinition, VisitDeclarativeParagraph,
  VisitDeclarativeSection, BindStatement dispatch
- **Procedure name resolution** — ResolveProcedureName, ResolveProcedureNameForThruEnd,
  ResolveProcedureNameForPerform, ExtractProcedureNameText
- **Statement binding** — 30+ statement-specific Bind* methods covering DISPLAY, MOVE,
  PERFORM, SET, INITIALIZE, SEARCH, STRING, UNSTRING, INSPECT, ACCEPT, MULTIPLY, ADD,
  SUBTRACT, DIVIDE, COMPUTE, and misc (STOP, GOBACK, EXIT, NEXT SENTENCE, CONTINUE)
- **Control flow binding** — IF, EVALUATE, GO TO, ALTER, plus SIZE ERROR clauses
- **File I/O binding** — OPEN, CLOSE, READ, WRITE, REWRITE, DELETE, START, RETURN,
  SORT, MERGE, RELEASE, USE, plus key/file resolution helpers
- **CALL binding** — CALL with BY REFERENCE/CONTENT/VALUE, RETURNING, CANCEL, ENTRY
- **Condition binding** — BindCondition, BindLogicalOr/And, BindComparison,
  abbreviated conditions, sign/class conditions, condition-name resolution
- **Expression binding** — arithmetic expressions, literals, figurative constants,
  function calls, data references with subscripts/ref-mod, qualified names
- **Validation** — ValidateStringStatement, ValidateUnstringStatement,
  ValidateInspectStatement, ValidateSearchStatement, ValidateSearchAllStatement
- **Diagnostics helpers** — DiagAt, MakeLocation, MakeSpan

This violates single-responsibility. The class is difficult to navigate, test in
isolation, or extend without risk of unrelated regressions.

---

## 2. Method Inventory

### A. Orchestration (stays in BoundTreeBuilder)

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 1 | `BoundTreeBuilder()` | 23–28 | Constructor | **BoundTreeBuilder** |
| 2 | `Build(ParserRuleContext)` | 192–195 | Entry point | **BoundTreeBuilder** |
| 3 | `VisitDeclarativeSection(...)` | 198–223 | Visitor | **BoundTreeBuilder** |
| 4 | `VisitParagraphDefinition(...)` | 226–250 | Visitor | **BoundTreeBuilder** |
| 5 | `VisitDeclarativeParagraph(...)` | 253–277 | Visitor | **BoundTreeBuilder** |
| 6 | `BindStatement(StatementContext)` | 284–343 | 40-case dispatch | **BoundTreeBuilder** |
| 7 | `MakeLocation(ParserRuleContext)` | 30 | Helper | **BoundTreeBuilder** |
| 8 | `MakeSpan(ParserRuleContext)` | 32–35 | Helper | **BoundTreeBuilder** |
| 9 | `Typed<T>(T)` | 37–65 | Expression helper | **BoundTreeBuilder** |
| 10 | `DiagAt(int)` | 3702–3703 | Diagnostic helper | **BoundTreeBuilder** |

**10 methods, ~200 lines**

### B. Procedure Name Resolution

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 11 | `ExtractProcedureNameText(...)` | 74–81 | Name extraction | **ProcedureNameResolver** |
| 12 | `ResolveProcedureName(string)` | 84–114 | Name resolution | **ProcedureNameResolver** |
| 13 | `ResolveProcedureNameForThruEnd(string)` | 122–152 | THRU resolution | **ProcedureNameResolver** |
| 14 | `ResolveProcedureNameForPerform(string)` | 154–189 | Section/PERFORM resolution | **ProcedureNameResolver** |

**4 methods, ~120 lines**

### C. Arithmetic Statement Binding

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 15 | `BindMultiply(...)` | 2425–2474 | MULTIPLY | **ArithmeticStatementBinder** |
| 16 | `BindAdd(...)` | 2475–2532 | ADD | **ArithmeticStatementBinder** |
| 17 | `BindSubtract(...)` | 2533–2616 | SUBTRACT | **ArithmeticStatementBinder** |
| 18 | `BindDivide(...)` | 2617–2692 | DIVIDE | **ArithmeticStatementBinder** |
| 19 | `BindCompute(...)` | 2693–2716 | COMPUTE | **ArithmeticStatementBinder** |
| 20 | `BindCorresponding(...)` | 458–517 | CORRESPONDING | **ArithmeticStatementBinder** |
| 21 | `ValidatedArithmetic(...)` | 3679–3696 | Construction + validation | **ArithmeticStatementBinder** |
| 22 | `BindArithmeticTargets(...)` | 3877–3888 | Target list | **ArithmeticStatementBinder** |
| 23 | `BindSizeErrorClause(...)` | 3621–3674 | SIZE ERROR | **ArithmeticStatementBinder** |

**9 methods, ~470 lines**

### D. Data Movement Statement Binding

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 24 | `BindDisplay(...)` | 347–371 | DISPLAY | **DataStatementBinder** |
| 25 | `BindMove(...)` | 375–456 | MOVE | **DataStatementBinder** |
| 26 | `BindMoveSendingOperand(...)` | 519–532 | MOVE source | **DataStatementBinder** |
| 27 | `BindSet(...)` | 2222–2241 | SET dispatch | **DataStatementBinder** |
| 28 | `BindSetSwitch(...)` | 2243–2295 | SET switch | **DataStatementBinder** |
| 29 | `BindSetBoolean(...)` | 2297–2313 | SET boolean | **DataStatementBinder** |
| 30 | `BindSetToValue(...)` | 2315–2342 | SET TO value | **DataStatementBinder** |
| 31 | `BindSetIndex(...)` | 2344–2361 | SET UP/DOWN | **DataStatementBinder** |
| 32 | `BindInitialize(...)` | 2365–2393 | INITIALIZE | **DataStatementBinder** |
| 33 | `ClassifyReplacingItem(...)` | 2395–2407 | INITIALIZE helper | **DataStatementBinder** |
| 34 | `BindReplacingValue(...)` | 2408–2424 | INITIALIZE helper | **DataStatementBinder** |
| 35 | `BindAccept(...)` | 1545–1565 | ACCEPT | **DataStatementBinder** |

**12 methods, ~420 lines**

### E. Control Flow Binding

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 36 | `BindPerform(...)` | 536–671 | PERFORM (all forms) | **ControlFlowBinder** |
| 37 | `BindPerformVaryingOption(...)` | 673–705 | VARYING option | **ControlFlowBinder** |
| 38 | `ValidatePerformIndex(...)` | 711–721 | VARYING validation | **ControlFlowBinder** |
| 39 | `BindEvaluate(...)` | 725–822 | EVALUATE | **ControlFlowBinder** |
| 40 | `BindEvaluateWhenGroup(...)` | 824–894 | WHEN clause | **ControlFlowBinder** |
| 41 | `BindValueOperand(...)` | 896–901 | Value operand | **ControlFlowBinder** |
| 42 | `BindIf(...)` | 2877–2912 | IF/ELSE | **ControlFlowBinder** |
| 43 | `BindGoTo(...)` | 2914–2955 | GO TO | **ControlFlowBinder** |
| 44 | `BindAlter(...)` | 2956–3002 | ALTER | **ControlFlowBinder** |
| 45 | `BindSearch(...)` | 1848–1897 | SEARCH | **ControlFlowBinder** |
| 46 | `BindSearchAll(...)` | 1898–1941 | SEARCH ALL | **ControlFlowBinder** |
| 47 | `ExtractSearchIndex(...)` | 1942–1952 | SEARCH helper | **ControlFlowBinder** |
| 48 | `FindSubscriptOnTable(...)` | 1953–1997 | SEARCH helper | **ControlFlowBinder** |
| 49 | `IsTableElement(...)` | 1998–2010 | SEARCH helper | **ControlFlowBinder** |

**14 methods, ~700 lines**

### F. File I/O Binding

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 50 | `BindWrite(...)` | 905–981 | WRITE | **FileIoBinder** |
| 51 | `BindOpen(...)` | 985–1016 | OPEN | **FileIoBinder** |
| 52 | `BindClose(...)` | 1020–1043 | CLOSE | **FileIoBinder** |
| 53 | `BindRead(...)` | 1047–1127 | READ | **FileIoBinder** |
| 54 | `BindRewrite(...)` | 1131–1170 | REWRITE | **FileIoBinder** |
| 55 | `BindDelete(...)` | 1174–1202 | DELETE | **FileIoBinder** |
| 56 | `BindStart(...)` | 1206–1239 | START | **FileIoBinder** |
| 57 | `BindReturn(...)` | 1243–1281 | RETURN | **FileIoBinder** |
| 58 | `BindSort(...)` | 1285–1334 | SORT | **FileIoBinder** |
| 59 | `BindMerge(...)` | 1338–1369 | MERGE | **FileIoBinder** |
| 60 | `BindRelease(...)` | 1373–1396 | RELEASE | **FileIoBinder** |
| 61 | `BindSortKeys(...)` | 1400–1415 | Key phrases | **FileIoBinder** |
| 62 | `BindMergeKeys(...)` | 1417–1432 | Key phrases | **FileIoBinder** |
| 63 | `ResolveFileList(...)` | 1434–1444 | File resolution | **FileIoBinder** |
| 64 | `BindUse(...)` | 4411–4427 | USE declarative | **FileIoBinder** |

**15 methods, ~630 lines**

### G. CALL / ENTRY Binding

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 65 | `BindCall(...)` | 1448–1541 | CALL | **CallBinder** |
| 66 | `BindCancel(...)` | 3003–3017 | CANCEL | **CallBinder** |
| 67 | `BindEntry(...)` | 3018–3043 | ENTRY | **CallBinder** |

**3 methods, ~120 lines**

### H. String Operation Binding

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 68 | `BindInspect(...)` | 1569–1671 | INSPECT | **StringStatementBinder** |
| 69 | `ExtractInspectPattern(...)` | 1672–1697 | Pattern helper | **StringStatementBinder** |
| 70 | `BindInspectBeforeAfter(...)` | 1698–1725 | Region helper | **StringStatementBinder** |
| 71 | `ExtractStringValue(...)` | 1726–1735 | Value helper | **StringStatementBinder** |
| 72 | `ExtractNthStringValue(...)` | 1736–1763 | Value helper | **StringStatementBinder** |
| 73 | `ExtractLiteralString(...)` | 1764–1799 | Literal helper | **StringStatementBinder** |
| 74 | `BindInspectDelimiters(...)` | 1800–1847 | Delimiters | **StringStatementBinder** |
| 75 | `BindString(...)` | 2011–2115 | STRING | **StringStatementBinder** |
| 76 | `BindUnstring(...)` | 2116–2221 | UNSTRING | **StringStatementBinder** |
| 77 | `ValidateStringStatement(...)` | 3705–3717 | Validation | **StringStatementBinder** |
| 78 | `ValidateUnstringStatement(...)` | 3719–3735 | Validation | **StringStatementBinder** |
| 79 | `ValidateInspectStatement(...)` | 3737–3754 | Validation | **StringStatementBinder** |

**12 methods, ~480 lines**

### I. Condition Binding

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 80 | `BindCondition(...)` | 3170–3177 | Entry point | **ConditionBinder** |
| 81 | `BindLogicalOr(...)` | 3179–3210 | OR | **ConditionBinder** |
| 82 | `BindLogicalAnd(...)` | 3212–3242 | AND | **ConditionBinder** |
| 83 | `BindAbbreviatedRelation(...)` | 3250–3262 | Abbreviated | **ConditionBinder** |
| 84 | `BindAbbreviatedAndChain(...)` | 3269–3281 | Abbreviated AND | **ConditionBinder** |
| 85 | `BindUnaryLogical(...)` | 3283–3297 | NOT | **ConditionBinder** |
| 86 | `BindPrimaryCondition(...)` | 3299–3323 | Primary | **ConditionBinder** |
| 87 | `BindSignConditionFromComparison(...)` | 3324–3344 | Sign condition | **ConditionBinder** |
| 88 | `BindComparison(...)` | 3345–3436 | Comparison | **ConditionBinder** |
| 89 | `ParseComparisonOperator(...)` | 3437–3466 | Operator parse | **ConditionBinder** |
| 90 | `NegateOperator(...)` | 3467–3500 | Operator negate | **ConditionBinder** |
| 91 | `ExpandAbbreviatedConditions(...)` | 3501–3503 | Expansion entry | **ConditionBinder** |
| 92 | `ExpandAbbrev(...)` | 3504–3598 | Recursive expansion | **ConditionBinder** |
| 93 | `ExtractContext(...)` | 3579–3597 | Context extraction | **ConditionBinder** |
| 94 | `IsRelational(...)` | 3599–3606 | Classification | **ConditionBinder** |
| 95 | `IsArithmeticOp(...)` | 3607–3613 | Classification | **ConditionBinder** |
| 96 | `BindComparisonOperand(...)` | 3614–3615 | Operand binding | **ConditionBinder** |
| 97 | `TryResolveConditionName(...)` | 3044–3060 | Condition-name | **ConditionBinder** |

**18 methods, ~500 lines**

### J. Expression Binding

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 98 | `BindAdditiveExpression(...)` | 2723–2738 | Additive expr | **ExpressionBinder** |
| 99 | `BindMultiplicativeExpression(...)` | 2740–2755 | Multiplicative | **ExpressionBinder** |
| 100 | `BindPowerExpression(...)` | 2757–2773 | Power expr | **ExpressionBinder** |
| 101 | `BindUnaryExpression(...)` | 2775–2792 | Unary +/- | **ExpressionBinder** |
| 102 | `BindPrimaryExpression(...)` | 2794–2818 | Primary expr | **ExpressionBinder** |
| 103 | `BindFunctionCall(...)` | 2832–2873 | Function call | **ExpressionBinder** |
| 104 | `BindLiteral(...)` | 3061–3075 | Literal dispatch | **ExpressionBinder** |
| 105 | `BindNumericLiteral(...)` | 3076–3084 | Numeric literal | **ExpressionBinder** |
| 106 | `BindNonNumericLiteral(...)` | 3085–3109 | String literal | **ExpressionBinder** |
| 107 | `BindFigurativeConstantExpression(...)` | 3111–3160 | Figuratives | **ExpressionBinder** |
| 108 | `BindDataReferenceWithSubscripts(...)` | 3894–4017 | Data ref + subscripts | **ExpressionBinder** |
| 109 | `InterpretSubscriptTokens(...)` | 4024–4053 | Subscript tokens | **ExpressionBinder** |
| 110 | `CollectLeafTokens(...)` | 4055–4064 | Token helper | **ExpressionBinder** |
| 111 | `SplitSubscriptTokens(...)` | 4067–4127 | Token split | **ExpressionBinder** |
| 112 | `BindSubscriptSegment(...)` | 4130–4216 | Subscript segment | **ExpressionBinder** |
| 113 | `BindSubscriptTokensAsArithmetic(...)` | 4219–4281 | Token arithmetic | **ExpressionBinder** |
| 114 | `BindSubscriptEntry(...)` | 4287–4355 | Subscript entry | **ExpressionBinder** |
| 115 | `ResolveQualifiedName(...)` | 4361–4376 | Qualified name | **ExpressionBinder** |
| 116 | `FindChild(...)` | 4382–4393 | Name helper | **ExpressionBinder** |
| 117 | `BindReferenceModification(...)` | 4395–4407 | Ref-mod | **ExpressionBinder** |
| 118 | `BindReceivingOperand(...)` | 3803–3810 | Receiving operand | **ExpressionBinder** |
| 119 | `BindSimpleOperand(...)` | 3816–3852 | Simple operand | **ExpressionBinder** |
| 120 | `BindDataReferenceOrLiteral(...)` | 3854–3867 | Data/literal | **ExpressionBinder** |
| 121 | `BindArithmeticExpr(...)` | 3869–3870 | Arithmetic entry | **ExpressionBinder** |

**24 methods, ~750 lines**

### K. Search Validation (stays in ControlFlowBinder with SEARCH)

| # | Method | Lines | Category | Destination |
|---|--------|-------|----------|-------------|
| 122 | `ValidateSearchStatement(...)` | 3756–3762 | Validation | **ControlFlowBinder** |
| 123 | `ValidateSearchAllStatement(...)` | 3764–3781 | Validation | **ControlFlowBinder** |
| 124 | `IsSearchAllEqualityCondition(...)` | 3788–3798 | Validation | **ControlFlowBinder** |

**3 methods, ~40 lines**

### Summary

| Destination | Methods | Lines | % of total |
|-------------|---------|-------|------------|
| **BoundTreeBuilder** (orchestrator) | 10 | ~200 | 5% |
| **ProcedureNameResolver** | 4 | ~120 | 3% |
| **ArithmeticStatementBinder** | 9 | ~470 | 11% |
| **DataStatementBinder** | 12 | ~420 | 9% |
| **ControlFlowBinder** | 17 | ~740 | 17% |
| **FileIoBinder** | 15 | ~630 | 14% |
| **CallBinder** | 3 | ~120 | 3% |
| **StringStatementBinder** | 12 | ~480 | 11% |
| **ConditionBinder** | 18 | ~500 | 11% |
| **ExpressionBinder** | 24 | ~750 | 17% |
| **Total** | 124 | ~4430 | — |

### Legacy Bound* Check

All methods produce `BoundStatement` / `BoundExpression` subtypes. No IR leakage.
No CIL emission. This is a pure syntax-to-bound-tree transformation layer.

---

## 3. Target Architecture

```
BoundTreeBuilder (orchestrator, ~200 lines)
  │
  │  Owns: Build(), VisitParagraphDefinition, VisitDeclarativeParagraph,
  │        VisitDeclarativeSection, BindStatement (dispatch),
  │        Typed<T>(), MakeLocation, MakeSpan, DiagAt
  │
  │  Constructs BindingContext, passes to all binders.
  │
  ├── BindingContext (record / sealed class)
  │     Shared state:
  │       - SemanticModel _semantic
  │       - DiagnosticBag _diagnostics
  │       - CompilationOptions _options
  │       - List<BoundParagraph> _paragraphs
  │       - HashSet<string> _alphanumericFunctions (static)
  │     References to binders (for cross-calls):
  │       - ProcedureNameResolver ProcedureName
  │       - ExpressionBinder Expression
  │       - ConditionBinder Condition
  │       - ArithmeticStatementBinder Arithmetic
  │       - DataStatementBinder Data
  │       - ControlFlowBinder ControlFlow
  │       - FileIoBinder FileIo
  │       - CallBinder Call
  │       - StringStatementBinder String
  │     Helper delegates:
  │       - Func<StatementContext, BoundStatement?> BindStatement
  │         (for recursive binding from IF/EVALUATE/PERFORM bodies)
  │       - Func<BoundExpression, BoundExpression> Typed
  │         (for expression type attachment)
  │
  ├── ProcedureNameResolver (~120 lines)
  │     ExtractProcedureNameText, ResolveProcedureName,
  │     ResolveProcedureNameForThruEnd, ResolveProcedureNameForPerform
  │     Dependencies: BindingContext (for _semantic, _diagnostics)
  │
  ├── ExpressionBinder (~750 lines)
  │     BindAdditiveExpression, BindMultiplicativeExpression,
  │     BindPowerExpression, BindUnaryExpression, BindPrimaryExpression,
  │     BindFunctionCall, BindLiteral, BindNumericLiteral,
  │     BindNonNumericLiteral, BindFigurativeConstantExpression,
  │     BindDataReferenceWithSubscripts, InterpretSubscriptTokens,
  │     CollectLeafTokens, SplitSubscriptTokens, BindSubscriptSegment,
  │     BindSubscriptTokensAsArithmetic, BindSubscriptEntry,
  │     ResolveQualifiedName, FindChild, BindReferenceModification,
  │     BindReceivingOperand, BindSimpleOperand,
  │     BindDataReferenceOrLiteral, BindArithmeticExpr
  │     Dependencies: BindingContext (for _semantic, Typed delegate)
  │
  ├── ConditionBinder (~500 lines)
  │     BindCondition, BindLogicalOr, BindLogicalAnd,
  │     BindAbbreviatedRelation, BindAbbreviatedAndChain,
  │     BindUnaryLogical, BindPrimaryCondition,
  │     BindSignConditionFromComparison, BindComparison,
  │     ParseComparisonOperator, NegateOperator,
  │     ExpandAbbreviatedConditions, ExpandAbbrev, ExtractContext,
  │     IsRelational, IsArithmeticOp, BindComparisonOperand,
  │     TryResolveConditionName
  │     Dependencies: ExpressionBinder (for operand binding)
  │
  ├── ArithmeticStatementBinder (~470 lines)
  │     BindMultiply, BindAdd, BindSubtract, BindDivide, BindCompute,
  │     BindCorresponding, ValidatedArithmetic, BindArithmeticTargets,
  │     BindSizeErrorClause
  │     Dependencies: ExpressionBinder, ConditionBinder (for SIZE ERROR)
  │
  ├── DataStatementBinder (~420 lines)
  │     BindDisplay, BindMove, BindMoveSendingOperand,
  │     BindSet (dispatch), BindSetSwitch, BindSetBoolean,
  │     BindSetToValue, BindSetIndex, BindInitialize,
  │     ClassifyReplacingItem, BindReplacingValue, BindAccept
  │     Dependencies: ExpressionBinder
  │
  ├── ControlFlowBinder (~740 lines)
  │     BindPerform (all forms), BindPerformVaryingOption,
  │     ValidatePerformIndex, BindEvaluate, BindEvaluateWhenGroup,
  │     BindValueOperand, BindIf, BindGoTo, BindAlter,
  │     BindSearch, BindSearchAll, ExtractSearchIndex,
  │     FindSubscriptOnTable, IsTableElement,
  │     ValidateSearchStatement, ValidateSearchAllStatement,
  │     IsSearchAllEqualityCondition
  │     Dependencies: ProcedureNameResolver, ExpressionBinder,
  │                   ConditionBinder, BindStatement delegate
  │
  ├── FileIoBinder (~630 lines)
  │     BindWrite, BindOpen, BindClose, BindRead, BindRewrite,
  │     BindDelete, BindStart, BindReturn, BindSort, BindMerge,
  │     BindRelease, BindSortKeys, BindMergeKeys, ResolveFileList,
  │     BindUse
  │     Dependencies: ExpressionBinder, ConditionBinder (for AT END)
  │
  ├── CallBinder (~120 lines)
  │     BindCall, BindCancel, BindEntry
  │     Dependencies: ExpressionBinder
  │
  └── StringStatementBinder (~480 lines)
        BindInspect, ExtractInspectPattern, BindInspectBeforeAfter,
        ExtractStringValue, ExtractNthStringValue, ExtractLiteralString,
        BindInspectDelimiters, BindString, BindUnstring,
        ValidateStringStatement, ValidateUnstringStatement,
        ValidateInspectStatement
        Dependencies: ExpressionBinder
```

---

## 4. Dependency Graph

```
                    BindingContext
                         │
         ┌───────────────┼───────────────┐
         │               │               │
  ProcedureNameResolver  SemanticModel  DiagnosticBag
                                         │
                                  ExpressionBinder    (leaf)
                                         │
                              ┌──────────┴──────────┐
                              │                     │
                       ConditionBinder         (leaf)
                              │
              ┌───────┬───────┼────────┬──────────┐
              │       │       │        │          │
          Arithmetic  Data  ControlFlow  FileIo  String
          Binder      Binder  Binder    Binder   Binder
                                │
                                ├── ProcedureNameResolver
                                ├── BindStatement delegate
                                │
                           CallBinder    (leaf)
```

**No circular dependencies.** `ExpressionBinder` is at the bottom of the graph.
`ConditionBinder` depends on `ExpressionBinder`. All statement binders depend on
`ExpressionBinder` and optionally `ConditionBinder`. `ControlFlowBinder` is the
most connected class because IF/EVALUATE/PERFORM need conditions, expressions,
procedure names, and recursive statement binding.

The recursive `BindStatement` call (needed when `ControlFlowBinder` binds IF
bodies or PERFORM inline statements) is handled via a delegate on `BindingContext`,
not by making `ControlFlowBinder` depend on `BoundTreeBuilder`.

---

## 5. Shared State (BindingContext)

### Fields by owner

| Field | Type | Accessed By | Notes |
|-------|------|-------------|-------|
| `_semantic` | `SemanticModel` | All binders | Name resolution, symbol lookup |
| `_diagnostics` | `DiagnosticBag` | All binders | Error/warning reporting |
| `_options` | `CompilationOptions` | Few binders | Compilation flags |
| `_paragraphs` | `List<BoundParagraph>` | Orchestrator only | Built during Visit* |
| `_alphanumericFunctions` | `HashSet<string>` | ExpressionBinder | Static, function classification |

Unlike CilEmitter's EmissionContext (18 mutable fields), BindingContext is much simpler:
only 4 instance fields plus 1 static set. The binding pass is stateless relative to the
emission pass — it doesn't accumulate per-method locals, field maps, or sync state.

---

## 6. Migration Strategy

### ~~Stage 1: Introduce `BindingContext` and class skeletons~~

**DONE (2026-03-30):** Created `Semantics/Bound/Binding/` with 10 files:
`BindingContext.cs` + 9 binder skeletons. BoundTreeBuilder constructor wires all
binders + delegates. 56 structural tests added. 821 unit + 287 integration + 95 NIST pass.

### ~~Stage 2: Extract `ExpressionBinder` and `ProcedureNameResolver`~~

**DONE (2026-03-30):** Moved 24 expression methods to `ExpressionBinder` (~780 lines)
and 4 procedure name methods to `ProcedureNameResolver` (~120 lines). 28 structural
tests added. 849 unit + 287 integration + 95 NIST pass.

### ~~Stage 3: Extract `ConditionBinder` and `ArithmeticStatementBinder`~~

**DONE (2026-03-30):** Moved 18 condition methods to `ConditionBinder` (~510 lines)
and 9 arithmetic methods to `ArithmeticStatementBinder` (~475 lines). 27 structural
tests added. 876 unit + 287 integration + 95 NIST pass.

### ~~Stage 4: Extract `ControlFlowBinder`, `FileIoBinder`, `DataStatementBinder`, `StringStatementBinder`, `CallBinder`~~

**DONE (2026-03-30):** Moved 17 methods to `ControlFlowBinder` (~740 lines), 15 to
`FileIoBinder` (~630 lines), 12 to `DataStatementBinder` (~420 lines), 12 to
`StringStatementBinder` (~480 lines), 3 to `CallBinder` (~120 lines). 59 structural
tests added. 935 unit + 287 integration + 95 NIST pass.

### ~~Stage 5: Cleanup~~

**DONE (2026-03-30):** Removed all forwarding wrappers from BoundTreeBuilder.
`BindStatement` dispatches directly to `_ctx.Data.*`, `_ctx.ControlFlow.*`,
`_ctx.FileIo.*`, `_ctx.String.*`, `_ctx.Call.*`, `_ctx.Arithmetic.*`. BoundTreeBuilder
reduced from 4,428 to **234 lines** (-95%). No forwarding wrappers remain. Updated
structural tests (no-wrapper verification). 922 unit + 287 integration + 95 NIST pass.
M004 is **fully closed**.

---

## 7. Invariants

The following must remain true after **every stage**:

1. **No behavioral change.** The bound tree for any COBOL program must be identical
   before and after each extraction. This is a pure refactor.

2. **No new public API.** All binder classes are `internal sealed`. Only
   `BoundTreeBuilder` and `BoundTreeBuilder.Build()` are public.

3. **No circular dependencies.** The dependency graph in section 4 must hold.
   No binder may reference `BoundTreeBuilder` directly — only through `BindingContext`.

4. **Statement dispatch stays in BoundTreeBuilder.** `BindStatement` is the
   orchestration point. It delegates to binders but is not itself extracted.

5. **Shared mutable state is owned by `BindingContext`.** No binder creates its
   own mutable collections. The `_paragraphs` list and diagnostic bag live on the context.

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
| Unit tests | ~765 | Bound tree structure, lowering, emission |
| Integration tests | ~287 | End-to-end COBOL compilation + execution |
| NIST guard | 95 | Kernel COBOL-85 compliance (NC series) |

### Specific coverage per binder

| Binder | Key tests that exercise it |
|--------|---------------------------|
| ExpressionBinder | All tests with arithmetic expressions, subscripts, ref-mod |
| ConditionBinder | NC207A/NC208A/NC214A, IF/EVALUATE tests |
| ArithmeticStatementBinder | NC201A/NC206A, ADD/SUBTRACT/MULTIPLY/DIVIDE tests |
| DataStatementBinder | NC101A/NC109A, MOVE/SET/INITIALIZE tests |
| ControlFlowBinder | NC202A/NC203A/NC204A, PERFORM/IF/EVALUATE/SEARCH tests |
| FileIoBinder | File I/O tests, SORT/MERGE tests |
| CallBinder | CALL tests, inter-program tests |
| StringStatementBinder | NC218A/NC219A, STRING/UNSTRING/INSPECT tests |
| ProcedureNameResolver | All PERFORM THRU, GO TO tests |

---

## 9. Validation Checklist

After all stages complete, verified:

- [x] `BoundTreeBuilder.cs` is 234 lines (under 300)
- [x] `BindStatement` is a thin switch that delegates to binder methods
- [x] No binder class exceeds 800 lines (largest: ExpressionBinder ~780)
- [x] No binder imports `BoundTreeBuilder` (only `BindingContext`)
- [x] No circular references between binder classes
- [x] `BindingContext` contains all shared mutable state
- [x] All 9 binder classes are `internal sealed`
- [x] 922 unit tests pass
- [x] 287 integration tests pass
- [x] 95 NIST guard tests pass (ALL GREEN)
- [x] `grep -rn "class BoundTreeBuilder" src/CobolSharp.Compiler/Semantics/` shows exactly 1 result
- [x] `wc -l src/CobolSharp.Compiler/Semantics/Bound/BoundTreeBuilder.cs` = 234
- [x] `ls src/CobolSharp.Compiler/Semantics/Bound/Binding/` shows 10 files (9 binders + BindingContext)
- [x] `modernization-ledger.json` has M004 status = "done"
- [x] `docs/boundtree/BoundTreeBuilder-Decomposition.md` has all stages marked complete
