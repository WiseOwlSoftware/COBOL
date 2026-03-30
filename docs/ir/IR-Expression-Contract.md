# IR Expression Contract (M001)

**Status:** COMPLETE (M001 fully implemented, all 3 stages done)
**Date:** 2026-03-30
**Ledger item:** M001 -- Eliminate BoundExpression leakage from IR layer

---

## Problem

The IR layer has no expression representation. Arithmetic expressions, subscripts,
reference-modification boundaries, loop counts, and intrinsic function arguments are
carried through the IR as `Semantics.Bound.BoundExpression` objects -- AST nodes that
belong to the semantic analysis phase, not the code generation phase.

This creates three concrete problems:

1. **CilEmitter contains a 137-line recursive bound-tree evaluator** (`EmitExpression`,
   lines 2862-2999) that walks `BoundLiteralExpression`, `BoundIdentifierExpression`,
   `BoundBinaryExpression`, `BoundFunctionCallExpression`, etc. The emitter is
   re-compiling the bound tree instead of translating pre-lowered IR.

2. **The Binder constructs *new* BoundExpression nodes** (synthetic
   `BoundBinaryExpression` trees at lines 2417, 2494, 2525) during lowering --
   the lowering pass is *building AST nodes* rather than IR nodes.

3. **`ResolvedLocations` dictionaries** (`Dictionary<BoundExpression, IrLocation>`)
   are threaded through IR instructions as a sidecar, keyed by BoundExpression
   reference identity. This couples IR instruction identity to specific bound node
   instances and exists only because the IR cannot express "load the numeric value
   at this location."

### Leakage inventory

17 `BoundExpression`-typed properties across 8 IR instruction classes, plus
7 `ResolvedLocations` dictionaries:

| IR instruction | BoundExpression properties | ResolvedLocations dicts |
|----------------|---------------------------|------------------------|
| `IrComputeStore` | `Expression` | 1 |
| `IrComputeIntoAccumulator` | `Expression` | 1 |
| `IrCobolRemainder` | `Dividend`, `Divisor` | 2 |
| `IrPerformTimes` | `CountExpression` | 1 |
| `IrPerformInlineTimes` | `CountExpression` | 1 |
| `IrFunctionCall` | `Arguments` (list) | 1 |
| `IrElementRef` | `Subscripts` (list) | -- |
| `IrRefModLocation` | `Start`, `Length` | -- |

Additionally, 4 enums defined in `Semantics.Bound.BoundNodes` are referenced by
the IR or CilEmitter: `InspectTallyKind`, `InspectReplaceKind`, `ClassConditionKind`,
`BoundBinaryOperatorKind`. These are data definitions, not expression trees.

---

## Design: IrExpression hierarchy

Introduce a sealed abstract `IrExpression` type with five concrete subtypes.
All arithmetic evaluation, subscript computation, ref-mod boundary calculation,
loop-count evaluation, and function-argument preparation will be expressed
entirely within this IR-native hierarchy.

```
IrExpression (abstract)
  |
  +-- IrLiteral(decimal Value)
  |     Compile-time constant. Covers numeric literals,
  |     figurative constants resolved to decimal, and
  |     negated literals (no need for unary minus on literals).
  |
  +-- IrLoadNumeric(IrLocation Source)
  |     Read the numeric value of a COBOL data item at runtime.
  |     The IrLocation is already fully resolved (static, element,
  |     ref-mod, or cached). This replaces the ResolvedLocations
  |     sidecar -- location resolution is embedded in the node.
  |
  +-- IrBinaryExpr(IrArithmeticOp Op, IrExpression Left, IrExpression Right)
  |     Arithmetic: Add, Subtract, Multiply, Divide, Remainder, Power.
  |     The Binder constructs this instead of synthetic BoundBinaryExpression.
  |     Named IrArithmeticOp (not IrBinaryOp) to avoid collision with the
  |     existing register-level IrBinaryOp enum in IrInstruction.cs.
  |
  +-- IrUnaryExpr(IrUnaryOp Op, IrExpression Operand)
  |     Negate. Covers unary minus on expressions (not just literals).
  |
  +-- IrIntrinsicCall(string FunctionName, IrFunctionArg[] Arguments)
        Intrinsic function evaluation. Each IrFunctionArg carries
        either an IrExpression (for numeric args) or an IrLocation
        (for alphanumeric args read as strings), replacing the
        current BoundExpression argument list.
```

### IrFunctionArg

Intrinsic function arguments have two forms -- numeric expressions evaluated
to decimal, and alphanumeric fields read as strings. The current code handles
this split inside `EmitIntrinsicCall` by pattern-matching on BoundExpression
subtypes. The IR should make this explicit:

```
IrFunctionArg (abstract)
  +-- IrNumericArg(IrExpression Expression)
  +-- IrAlphanumericArg(IrLocation Source)
  +-- IrLiteralStringArg(string Value)
```

### IrArithmeticOp and IrUnaryOp enums

```csharp
public enum IrArithmeticOp { Add, Subtract, Multiply, Divide, Remainder, Power }
public enum IrUnaryOp { Negate }
```

Named `IrArithmeticOp` rather than `IrBinaryOp` because `IrBinaryOp` already exists
in `IrInstruction.cs` for register-level operations (Add, Sub, Mul, Div, And, Or,
Eq, Ne, Lt, Le, Gt, Ge). The expression-tree enum covers only arithmetic operators.

These replace the current round-trip where `BoundBinaryOperatorKind` is cast to
`int` by the Binder and cast back by the CilEmitter.

---

## IR instruction changes

Each affected IR instruction replaces its `BoundExpression` property with `IrExpression`
and **drops its `ResolvedLocations` dictionary entirely**:

| IR instruction | Before | After |
|----------------|--------|-------|
| `IrComputeStore` | `BoundExpression Expression` + `ResolvedLocations` | `IrExpression Expression` |
| `IrComputeIntoAccumulator` | `BoundExpression Expression` + `ResolvedLocations` | `IrExpression Expression` |
| `IrCobolRemainder` | `BoundExpression Dividend/Divisor` + 2 dicts | `IrExpression Dividend`, `IrExpression Divisor` |
| `IrPerformTimes` | `BoundExpression CountExpression` + `ResolvedLocations` | `IrExpression CountExpression` |
| `IrPerformInlineTimes` | `BoundExpression CountExpression` + `ResolvedLocations` | `IrExpression CountExpression` |
| `IrFunctionCall` | `List<BoundExpression> Arguments` + `ResolvedLocations` | `IrFunctionArg[] Arguments` |
| `IrElementRef` | `List<BoundExpression> Subscripts` | `IrExpression[] Subscripts` |
| `IrRefModLocation` | `BoundExpression Start/Length` | `IrExpression Start`, `IrExpression? Length` |

---

## Binder changes

The Binder gains a new internal method:

```csharp
IrExpression LowerExpression(BoundExpression expr)
```

This replaces `PreResolveExpressionLocations`. It recursively converts:

| BoundExpression type | IrExpression result |
|---------------------|---------------------|
| `BoundLiteralExpression` (decimal) | `IrLiteral(value)` |
| `BoundIdentifierExpression` | `IrLoadNumeric(ResolveLocation(id))` |
| `BoundReferenceModificationExpression` | `IrLoadNumeric(ResolveRefModLocation(...))` |
| `BoundBinaryExpression` | `IrBinaryExpr(op, LowerExpression(left), LowerExpression(right))` |
| `BoundFunctionCallExpression` | `IrIntrinsicCall(name, LowerFunctionArgs(args))` |
| Unary minus (0 - expr pattern) | `IrUnaryExpr(Negate, LowerExpression(inner))` |

The Binder stops constructing synthetic `BoundBinaryExpression` nodes for MULTIPLY
GIVING, SUBTRACT GIVING, and DIVIDE GIVING. Instead it directly constructs
`IrBinaryExpr` nodes.

`PreResolveExpressionLocations` and `WalkExpressionForLocations` are deleted.

---

## CilEmitter changes

`EmitExpression(ILProcessor, BoundExpression, ResolvedLocations?)` is replaced with:

```csharp
void EmitIrExpression(ILProcessor il, IrExpression expr)
```

This is a simple recursive walk over 5 IR node types:

| IrExpression type | CIL emission |
|-------------------|-------------|
| `IrLiteral` | `EmitLoadDecimal(value)` |
| `IrLoadNumeric` | `EmitLocationArgs(loc)` + `call DecodeNumeric` |
| `IrBinaryExpr` | Emit left, emit right, emit operator (`decimal.op_Addition`, etc.) |
| `IrUnaryExpr` | Emit operand, `call decimal.op_UnaryNegation` |
| `IrIntrinsicCall` | Build `object[]` args, `call IntrinsicFunctions.Call`, unbox |

`EmitIntrinsicCall(ILProcessor, string, List<BoundExpression>, ResolvedLocations?)`
is deleted. Its logic folds into the `IrIntrinsicCall` case of `EmitIrExpression`
plus a helper for `IrFunctionArg` emission.

The `EmitElementAddress` and `EmitRefModAddress` methods change from calling
`EmitExpression` on BoundExpression subscripts/start/length to calling
`EmitIrExpression` on `IrExpression` subscripts/start/length.

---

## Enum relocation

Move these 4 enums out of `Semantics.Bound.BoundNodes` to a shared location
accessible to both Bound and IR layers without coupling:

| Enum | Current location | New location |
|------|-----------------|--------------|
| `InspectTallyKind` | `Semantics/Bound/BoundNodes.cs:885` | `Common/CobolEnums.cs` or `IR/IrEnums.cs` |
| `InspectReplaceKind` | `Semantics/Bound/BoundNodes.cs:886` | Same |
| `ClassConditionKind` | `Semantics/Bound/BoundNodes.cs:234` | Same |
| `BoundBinaryOperatorKind` | `Semantics/Bound/BoundNodes.cs` | Replaced by `IrBinaryOp` and `IrCompareOp` in IR; bound layer keeps its own copy for binding |

For comparison operators, define a separate `IrCompareOp` enum in the IR layer:

```csharp
public enum IrCompareOp { Equal, NotEqual, Less, LessOrEqual, Greater, GreaterOrEqual }
```

This eliminates the int-cast round-trip currently used by `IrCompareNumeric.OperatorKind`
and `EmitCompareResultToBool`.

---

## Verification plan

After implementation:

1. `grep -r "Semantics.Bound" src/CobolSharp.Compiler/IR/` returns **zero results**
2. `grep -r "BoundExpression" src/CobolSharp.Compiler/CodeGen/CilEmitter.cs` returns **zero results**
3. `grep -r "ResolvedLocations" src/CobolSharp.Compiler/IR/` returns **zero results**
4. `EmitExpression` method in CilEmitter is deleted
5. `EmitIntrinsicCall` method in CilEmitter is deleted (logic moved to `EmitIrExpression`)
6. `PreResolveExpressionLocations` in Binder is deleted
7. All existing tests pass (453 unit + 287 integration + 95 NIST guard)

---

## Scope and risk

**Scope:** Large -- touches IrInstruction.cs, Binder.cs, CilEmitter.cs, and creates
new IrExpression.cs. Every IR instruction constructor call that currently passes
BoundExpression must be updated.

**Risk:** High -- arithmetic evaluation is the core of COMPUTE, MULTIPLY, DIVIDE,
SUBTRACT, PERFORM TIMES, subscript access, and reference modification. Any
lowering bug will produce wrong computation results.

**Mitigation:** The existing test suite (790+ tests including NIST) provides strong
regression coverage for arithmetic correctness. Implement incrementally:

1. ~~Define `IrExpression` types and `LowerExpression` in Binder (new code, no deletions)~~
   **DONE (Stage 1, 2026-03-30):** `IrExpression.cs` created with 5 node types +
   `IrArithmeticOp`/`IrUnaryOp` enums + 3 `IrFunctionArg` types. `Binder.LowerExpression`
   implemented (97 lines). 19 unit tests in `LowerExpressionTests.cs`. 440 unit + 274
   integration tests pass.
2. ~~Migrate all 8 IR instruction types from BoundExpression to IrExpression~~
   **DONE (Stage 2, 2026-03-30):** All 8 IR instruction types updated:
   `IrComputeStore`, `IrComputeIntoAccumulator`, `IrCobolRemainder`, `IrPerformTimes`,
   `IrPerformInlineTimes`, `IrFunctionCall`, `IrElementRef`, `IrRefModLocation`.
   Binder now calls `LowerExpression` at all 17 creation sites — no more synthetic
   `BoundBinaryExpression` construction (MULTIPLY GIVING, SUBTRACT GIVING, DIVIDE
   GIVING now build `IrBinaryExpr` directly). CilEmitter gained `EmitIrExpression`
   (recursive IR expression evaluator) and `EmitIrIntrinsicCall`. `ResolvedLocations`
   dictionaries marked `[Obsolete]` on all 5 IR instructions that carry them.
   440 unit + 274 integration + 95 NIST guard all pass.
3. ~~Delete dead code + relocate enums~~
   **DONE (Stage 3, 2026-03-30):** Deleted `EmitExpression` (137 lines),
   `EmitIntrinsicCall` (75 lines), `PreResolveExpressionLocations`,
   `WalkExpressionForLocations`. Removed all `ResolvedLocations` dictionaries and
   `[Obsolete]` markers from 5 IR instructions. Moved 4 enums to IR namespace:
   `InspectTallyKind`, `InspectReplaceKind`, `ClassConditionKind`, `IrCompareOp`.
   CilEmitter now uses `IrCompareOp` instead of round-tripping through
   `BoundBinaryOperatorKind`. **Verification:** `grep -r "Semantics.Bound."
   src/CobolSharp.Compiler/IR/` and `grep -r "BoundExpression"
   src/CobolSharp.Compiler/CodeGen/CilEmitter.cs` both return zero results.
   440 unit + 274 integration + 95 NIST guard all pass.
4. ~~Add regression guard tests~~
   **DONE (Stage 4, 2026-03-30):** 13 reflection-based structural tests in
   `IrExpressionContractTests.cs` (no BoundExpression in any IR type, no
   EmitExpression/EmitIntrinsicCall in CilEmitter, LowerExpression exists in
   Binder, 4 enums in IR namespace). 13 integration pipeline tests in
   `IrExpressionPipelineTests.cs` (COMPUTE, PERFORM TIMES, inline PERFORM TIMES,
   PERFORM VARYING, FUNCTION MAX/MOD, ref-mod, variable subscripts, subscripted
   COMPUTE, DIVIDE REMAINDER, MULTIPLY GIVING, SUBTRACT GIVING).
   453 unit + 287 integration + 95 NIST guard all pass.
