# Section 3: Non-production-quality and Duplication Findings

Audit date: 2026-03-22
Branch: nist-phase-d
Codebase: 97 C# source files (excluding generated ANTLR code)

---

## 1. Meaningless Wrappers

### 1.1 `BindDataReference` delegates to `BindDataReferenceWithSubscripts`

- **Location**: `src/CobolSharp.Compiler/Semantics/Bound/BoundTreeBuilder.cs:2512`
- **Delegates to**: `BindDataReferenceWithSubscripts(idCtx)` on the same object
- **Callers**: 1 (line 316, in `BindMove`)
- **Assessment**: Pure delegation with no added invariants, no null check, no error handling. Inline at the call site and delete.

### 1.2 `BindFullExpression` delegates to `BindAdditiveExpression`

- **Location**: `src/CobolSharp.Compiler/Semantics/Bound/BoundTreeBuilder.cs:2236`
- **Body**: `return BindAdditiveExpression(ctx.additiveExpression());`
- **Callers**: 12 (PERFORM, EVALUATE, COMPUTE, condition contexts)
- **Assessment**: This is the "good" expression walker. It could be inlined, but the name provides abstraction value by marking the entry point into the recursive expression grammar. **Borderline** -- retain for readability but document that it is a structural entry point, not a semantic layer.

### 1.3 `DiagnosticBag.ReportError` / `ReportWarning` wrap `Report`

- **Location**: `src/CobolSharp.Compiler/Diagnostics/DiagnosticBag.cs:23-30`
- **Assessment**: These are convenience overloads that set the severity enum. They serve their purpose -- **not a finding**. Mentioned only for completeness.

### 1.4 `SymbolTable.Declare` wraps `Scope.TryDeclare`

- **Location**: `src/CobolSharp.Compiler/Semantics/SymbolTable.cs:55-56`
- **Delegates to**: `CurrentScope.TryDeclare(symbol, out existing)`
- **Assessment**: Forwards to current scope without adding validation. However, it provides the convenience of not requiring callers to hold a scope reference. **Borderline** -- acceptable as API convenience.

### 1.5 `SymbolTable.Resolve` / `Resolve<T>` wrap `Scope.Resolve`

- **Location**: `src/CobolSharp.Compiler/Semantics/SymbolTable.cs:58-63`
- **Assessment**: Same pattern as 1.4. Acceptable API surface.

---

## 2. Duplicated Logic

### 2.1 CRITICAL: Two complete expression binding implementations

- **Location A** ("full expression" path): `BoundTreeBuilder.cs:2236-2330`
  - `BindFullExpression` -> `BindAdditiveExpression` -> `BindMultiplicativeExpression` -> `BindPowerExpression` -> `BindUnaryExpression` -> `BindPrimaryExpression`
- **Location B** ("arithmetic expr" path): `BoundTreeBuilder.cs:2994-3082`
  - `BindArithmeticExpr` -> `BindAdditiveExpr` -> `BindMultiplicativeExpr` -> `BindPowerExpr` -> `BindUnaryExpr` -> `BindPrimaryExpr`

**Both implementations** walk the same grammar rule (`arithmeticExpression -> additiveExpression -> multiplicativeExpression -> powerExpression -> unaryExpression -> primaryExpression`), produce the same `BoundBinaryExpression` trees, and handle the same operator mapping. The only difference: path B accepts nullable input and returns a zero literal for null.

- **Path A callers**: COMPUTE, PERFORM, EVALUATE, condition contexts (12 sites)
- **Path B callers**: SET, subscripts, reference modification (6 sites)

**Recommendation**: Delete path B entirely. Replace `BindArithmeticExpr` with a null-guarded call to `BindFullExpression`:
```csharp
private BoundExpression BindArithmeticExpr(ArithmeticExpressionContext? ctx)
    => ctx != null ? BindFullExpression(ctx) : new BoundLiteralExpression(0m, ...);
```

### 2.2 `GetPicForLocation` duplicated between Binder and CilEmitter

- **Binder copy**: `src/CobolSharp.Compiler/CodeGen/Binder.cs:523-532`
- **CilEmitter copy**: `src/CobolSharp.Compiler/CodeGen/CilEmitter.cs:2597-2606`

Both are `static` methods with identical logic (switch on `IrStaticLocation`, `IrElementRef`, `IrRefModLocation`). The only difference is the exception type (`InvalidOperationException` vs `NotSupportedException`).

**Recommendation**: Move to `IrLocation` as a static method or an instance property, or to a shared `IrLocationExtensions` utility class.

### 2.3 INVALID KEY branching pattern duplicated 3x in Binder

The following three methods contain structurally identical branching logic:
- `LowerDelete`: `Binder.cs:1260-1286`
- `LowerStart`: `Binder.cs:1317-1343`
- `LowerRead` (AT END variant): `Binder.cs:1188-1216`

Pattern in each:
```
var result = _valueFactory.Next(IrPrimitiveType.Bool);
block.Instructions.Add(new IrCheckFile<Status>(name, result));
var trueBlock = method.CreateBlock("...");
var falseBlock = method.CreateBlock("...");
var afterBlock = method.CreateBlock("...");
block.Instructions.Add(new IrBranchIfFalse(result, falseBlock));
block.Instructions.Add(new IrJump(trueBlock));
// Lower true-path statements
// Lower false-path statements
// Both jump to afterBlock
```

**Recommendation**: Extract a `LowerConditionalBranch(IrInstruction checkInst, IReadOnlyList<BoundStatement> truePath, IReadOnlyList<BoundStatement> falsePath, IrMethod method, IrBasicBlock block, string label)` helper.

### 2.4 Receiving arithmetic target binding pattern repeated 6x

The pattern:
```csharp
foreach (var gt in givingPhrase.receivingArithmeticOperand())
{
    var sym = BindDataReferenceWithSubscripts(gt.dataReference());
    if (sym is BoundIdentifierExpression boundGt)
        targets.Add(new BoundArithmeticTarget(boundGt, gt.ROUNDED() != null));
}
```
appears in:
- `BindMultiply`: `BoundTreeBuilder.cs:1914-1919`
- `BindAdd`: `BoundTreeBuilder.cs:1976-1981, 1998-2003`
- `BindSubtract`: `BoundTreeBuilder.cs:2056-2061, 2094-2098`
- `BindDivide`: `BoundTreeBuilder.cs:2136-2141, 2178-2183`
- `BindCompute`: `BoundTreeBuilder.cs:2214-2218`

**Recommendation**: Extract `BindArithmeticTargets(receivingArithmeticOperand[])` returning `List<BoundArithmeticTarget>`.

### 2.5 `BindSimpleOperand` uses type-dispatch with identical bodies

- **Location**: `BoundTreeBuilder.cs:2944-2980`

The method dispatches on `AddOperandContext`, `SubtractOperandContext`, `MultiplyOperandContext`, `DivideOperandContext` -- but each branch does exactly the same thing: check `dataReference()`, then check `literal()`. The only reason for the dispatch is that each grammar rule is a separate type despite having identical structure.

**Recommendation**: Use a common grammar interface or extract a `BindIdentifierOrLiteral(ParserRuleContext)` that uses reflection or the ANTLR tree API to find `dataReference()` / `literal()` children generically.

### 2.6 Fake source location construction repeated 69+ times

- `new Common.SourceLocation("<source>", 0, 0, 0)` with `new Common.TextSpan(0, 0)`: **36 occurrences** across Binder.cs (17), BoundTreeBuilder.cs (16), and others
- The `BoundTreeValidator` has its own `Report` helper (line 433) that constructs the same pair
- `BoundTreeBuilder` has `DiagAt(int line)` (line 2857) that wraps the same pattern

**Recommendation**: Create a static factory `SourceLocation.None` or `SourceLocation.Unknown` and `TextSpan.Empty` to eliminate the repeated construction. Propagate actual source locations from parse tree nodes where possible.

---

## 3. Ad-hoc and Hacky Code Paths

### 3.1 Ad-hoc diagnostic codes (magic strings)

The Binder (`Binder.cs`) uses 17 ad-hoc diagnostic codes (`COBOL0500`-`COBOL0513`) as inline string literals, while the rest of the compiler uses the structured `DiagnosticDescriptors` registry:

| Code | Location | Description |
|------|----------|-------------|
| COBOL0500 | Binder.cs:829 | PERFORM VARYING index no storage |
| COBOL0501 | Binder.cs:932 | PERFORM target not found |
| COBOL0502 | Binder.cs:983 | PERFORM TIMES no target |
| COBOL0503 | Binder.cs:2390 | MOVE target no storage |
| COBOL0504 | Binder.cs:2406 | MOVE source no storage |
| COBOL0505 | Binder.cs:2482 | Computed expression no location |
| COBOL0506 | Binder.cs:2726,2761 | ADD/SUB accumulator no location |
| COBOL0507 | Binder.cs:2736 | Arithmetic target no storage |
| COBOL0508 | Binder.cs:2746 | COMPUTE store target no storage |
| COBOL0509 | Binder.cs:2795,2814,2832 | Operand resolution failed |
| COBOL0510-0513 | Binder.cs:1591-1652 | SET target/value resolution |
| COBOL0600 | Compilation.cs:182 | Internal compiler error |

Similarly, `BoundTreeBuilder.cs` uses 13 ad-hoc codes (`COBOL0400`-`COBOL0412`), and `CobolErrorStrategy.cs` uses 20+ ad-hoc codes (`COBOL0001`, `COBOL0100`-`COBOL0312`).

**Total**: 50+ diagnostic codes as inline string literals outside the DiagnosticDescriptors registry.

**Recommendation**: Migrate all ad-hoc codes to `DiagnosticDescriptors` with format templates. This enables centralized documentation, severity management, and diagnostic suppression.

### 3.2 IR stubs for RETURN and CALL

- **RETURN stub**: `Binder.cs:1350-1364` -- emits a DISPLAY message and always takes the AT END path
- **CALL stub**: `Binder.cs:1369-1383` -- emits a DISPLAY message and always takes the ON EXCEPTION path

These stubs produce incorrect runtime behavior: CALL always fails, RETURN always signals end-of-file. Any test that uses CALL or RETURN will produce wrong results.

**Recommendation**: At minimum, emit a runtime-level diagnostic/warning. For CALL, consider implementing the inter-program dispatch table (the bound tree already has `BoundCallStatement` with `BoundCallArgument` list and `ParameterMode`). For RETURN, block until SORT/MERGE is implemented.

### 3.3 Function calls return constant zero

- **Location**: `BoundTreeBuilder.cs:3067-3068`
- **Current behavior**: `// TODO: proper function binding` followed by `return new BoundLiteralExpression(0m, CobolCategory.Numeric);`
- **Impact**: Any COBOL intrinsic function call (FUNCTION LENGTH, FUNCTION CURRENT-DATE, etc.) is silently replaced with zero. No diagnostic is emitted.

**Recommendation**: At minimum, emit a diagnostic warning. Better: implement the most common intrinsic functions (LENGTH, WHEN-COMPILED, CURRENT-DATE) since the runtime already has `IntrinsicFunctions.cs`.

### 3.4 Power operator uses `BoundBinaryOperatorKind.Power` with no CIL binary op mapping

- **Location**: `BoundTreeBuilder.cs:2283-2288`
- **Comment in code**: "Power is not a standard BoundBinaryOperatorKind; use Multiply as placeholder"
- **Impact**: The `Power` enum value is produced by the bound tree builder. The CilEmitter's `EmitBinary` (line 818-829) does **not** handle `Power` -- it falls through to `throw new NotSupportedException`. COMPUTE expressions with `**` may work if routed through `EmitExpression` rather than `EmitBinary`, but this is a latent crash for some code paths.

### 3.5 `StartCondition` is a magic number

- **Location**: `Binder.cs:1308`
- **Code**: `int condition = 0; // StartCondition.Equal`
- **Assessment**: Hard-coded integer instead of an enum. The START KEY comparison condition from the bound tree is ignored; it always emits "equal" regardless of the `KEY IS GREATER THAN` / `KEY IS NOT LESS THAN` clauses specified in the COBOL source.

### 3.6 WRITE FROM lowering omits the FROM move

- **Location**: `Binder.cs` -- `LowerWrite` method
- **Assessment**: The `BoundWriteStatement` has a `From` property (added in session 2026-03-21), and `BoundTreeValidator.ValidateWrite` validates it. However, `LowerWrite` in the Binder does not perform the FROM-to-record MOVE before the write operation. The validation exists but the codegen is missing.

### 3.7 String literal fallback for unresolved identifiers

- **Location**: `BoundTreeBuilder.cs:3137`
- **Code**: `return new BoundLiteralExpression(name, CobolCategory.Alphanumeric);`
- **Impact**: When `BindDataReferenceWithSubscripts` cannot resolve a symbol, it silently creates a string literal containing the identifier name. No diagnostic is emitted. This masks typos and missing data declarations -- the program will silently use the identifier name as a literal string value at runtime.

### 3.8 Three duplicate XML doc comment blocks on `EmitElementAddress`

- **Location**: `CilEmitter.cs:2651-2660`
- Three consecutive `<summary>` blocks for a single method, each from a different revision. Only the last one reflects the current implementation. The first two are stale.

---

## 4. Dead Code

### 4.1 `CompilationOptions` class -- never instantiated or referenced

- **Location**: `src/CobolSharp.Compiler/Semantics/CompilationOptions.cs`
- **Assessment**: Defines `DialectMode` enum and `CompilationOptions` class. Neither is referenced anywhere in the codebase. Pure dead code. The `DialectMode.StrictCobol85` mode was intended to pair with `CBL3501`/`CBL3502` descriptors, but none of that wiring exists.

### 4.2 `ReportWriterValidator` -- empty stub, never called

- **Location**: `src/CobolSharp.Compiler/Semantics/ReportWriterValidator.cs`
- **Assessment**: Contains an empty `Validate` method body. Not called from `Compilation.Compile()` or anywhere else. The diagnostic descriptors it was designed to use (CBL3401-3406) are also unused.

### 4.3 `GetDataReferenceName` -- never called

- **Location**: `BoundTreeBuilder.cs:2521-2524`
- **Assessment**: `private static string GetDataReferenceName(...)` has zero callers. Dead code.

### 4.4 Unused diagnostic descriptors

The following descriptors in `DiagnosticDescriptors.cs` are defined but never referenced in any diagnostic emission:
- `CBL3105` (GLOBAL not allowed) -- unreferenced
- `CBL3106` (LOCAL shadows GLOBAL) -- unreferenced
- `CBL3107` (Name conflicts with symbol) -- unreferenced
- `CBL3301`-`CBL3305` (CALL argument validation) -- CALL validation only uses `CBL3310`
- `CBL3401`-`CBL3406` (Report Writer) -- entirely unused (tied to dead `ReportWriterValidator`)
- `CBL3501`-`CBL3502` (Strict COBOL-85 mode) -- entirely unused (tied to dead `CompilationOptions`)

---

## 5. TODO / FIXME / HACK Comments

| Location | Text |
|----------|------|
| `src/CobolSharp.CLI/Program.cs:148` | `// TODO: pass standard to Compilation when grammar overlays are wired up` |
| `src/CobolSharp.Compiler/Semantics/Bound/BoundTreeBuilder.cs:3067` | `// TODO: proper function binding` |

**Count**: 2 TODO comments. No FIXME or HACK comments found.

---

## 6. NotSupportedException / NotImplementedException Throws

All 5 occurrences are in `CilEmitter.cs` and serve as exhaustive switch guards:

| Line | Context |
|------|---------|
| 745 | Default case in `EmitInstruction` switch -- catches unhandled IR node types |
| 829 | Default case in `EmitBinary` op switch -- catches unhandled binary operators |
| 2604 | Default case in `GetPicForLocation` -- catches unknown IrLocation subtypes |
| 2634 | Default case in `EmitLocationArgs` -- catches unknown IrLocation subtypes |
| 2748 | Default case in `EmitRefModAddress` -- catches unsupported base location types |

These are acceptable as defensive programming for "impossible state" detection. However, they should be distinguished from "not yet implemented" by using a more specific exception type.

---

## 7. Overly Complex Methods (>80 lines)

### CilEmitter.cs
| Method | Lines | Start Line |
|--------|-------|------------|
| `EmitInstruction` | 349 | :400 |
| `EmitProgramState` | 165 | :93 |
| `EmitUnstringStatement` | 145 | :2337 |
| `EmitExpression` | 131 | :1998 |
| `EmitStringStatement` | 95 | :2241 |

### Binder.cs
| Method | Lines | Start Line |
|--------|-------|------------|
| `LowerConditionName` | 114 | :2601 |
| `LowerDivide` | 107 | :1815 |
| `Bind` | 97 | :69 |
| `LowerStatement` | 90 | :272 |
| `LowerComparison` | 90 | :2399 |
| `CreateEntryPoint` | 89 | :180 |
| `LowerSearchAll` | 86 | :3062 |
| `ResolveLocation` | 83 | :373 |

### BoundTreeBuilder.cs
| Method | Lines | Start Line |
|--------|-------|------------|
| `BindPerform` | 118 | :422 |
| `BindDataReferenceWithSubscripts` | 108 | :3089 |
| `BindInspect` | 100 | :1149 |
| `BindSubtract` | 92 | :2017 |
| `BindDivide` | 91 | :2112 |
| `BindCall` | 85 | :1041 |

**Worst offender**: `CilEmitter.EmitInstruction` at 349 lines is a single switch statement dispatching on IR instruction type. This is standard in emitters and code generators. It could be decomposed using a visitor pattern or dispatch table, but the flat switch is a common pragmatic choice.

---

## 8. Summary of Priorities

### High Priority (correctness risk)
1. **Duplicate expression binding** (2.1) -- two parallel implementations risk divergent behavior as features are added to one but not the other. ~90 lines of exact duplication.
2. **Function calls silently return zero** (3.3) -- silent wrong results, no diagnostic
3. **Unresolved identifiers become string literals** (3.7) -- masks errors silently, no diagnostic
4. **START always uses Equal condition** (3.5) -- ignores KEY IS GREATER/LESS from source
5. **WRITE FROM not lowered** (3.6) -- validated but not emitted to IR/CIL

### Medium Priority (maintainability)
6. **Ad-hoc diagnostic codes** (3.1) -- 50+ string literal codes outside the descriptor registry
7. **Fake source locations** (2.6) -- 69+ `<source>` placeholders lose error position information
8. **`GetPicForLocation` duplication** (2.2) -- identical logic in two files
9. **Branching pattern duplication** (2.3) -- 3x copy of the same conditional block emission
10. **IR stubs with wrong behavior** (3.2) -- CALL/RETURN always take failure path

### Low Priority (cleanup)
11. **Dead code** (4.x) -- `CompilationOptions`, `ReportWriterValidator`, `GetDataReferenceName`, unused diagnostic descriptors
12. **Wrapper method** (1.1) -- `BindDataReference` single-use delegation
13. **Receiving target binding pattern** (2.4) -- 6x repetition of the same foreach loop
14. **`BindSimpleOperand` type dispatch** (2.5) -- 4 identical branches
15. **Stale XML doc comments** (3.8) -- 3 duplicate summary blocks on one method
