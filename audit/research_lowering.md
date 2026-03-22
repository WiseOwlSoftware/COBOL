# Lowering/IR Gap Research

Date: 2026-03-22

## 1. Subscripted DIVIDE GIVING (NC121M) -- ALREADY FIXED

**Status**: NC121M is at 100% (39/39 + 2 inspect). Fixed in DEVLOG Entry 126 (2026-03-20).

**Original bug** (from DEVLOG Entry 126): `DIVIDE 3 INTO TABLE1-NUM(INDEX1) GIVING NUM-9V9`
computed 0 instead of 1. The dividend `TABLE1-NUM(INDEX1)` was being read without its
subscript -- always reading element 1. The BoundTreeBuilder was creating a fresh
`BoundIdentifierExpression` from `targets[0].Target.Symbol` (discarding subscripts) instead
of using `targets[0].Target` (the original expression with subscripts).

**Fix**: One-line change: `dividend = targets[0].Target;`

**Current code analysis**: All arithmetic lowering methods (ADD, SUBTRACT, MULTIPLY, DIVIDE)
resolve targets via `ResolveLocation(target.Target)` where `target.Target` is
`BoundIdentifierExpression`. The `ResolveLocation(BoundIdentifierExpression)` overload at
line 373 handles subscripts correctly, creating `IrElementRef` for variable subscripts or
folding constant subscripts to `IrStaticLocation`.

**Key difference**: MULTIPLY uses `ResolveExpressionLocation(target.Target)` (line 1712)
while ADD/SUBTRACT/DIVIDE use `ResolveLocation(target.Target)` (lines 1975, 1781, 1836,
1883). Both resolve to the same subscript-aware path for `BoundIdentifierExpression`, so
this difference is cosmetic, not a bug. However, `ResolveExpressionLocation` also handles
`BoundReferenceModificationExpression`, so MULTIPLY would work with ref-mod targets while
ADD/SUBTRACT/DIVIDE would silently drop them. This is a minor consistency gap.

**Files**:
- `/home/user/CobolSharp/src/CobolSharp.Compiler/CodeGen/Binder.cs` lines 1815-1921 (LowerDivide)
- `/home/user/CobolSharp/src/CobolSharp.Compiler/CodeGen/Binder.cs` lines 373-455 (ResolveLocation with subscripts)
- `/home/user/CobolSharp/src/CobolSharp.Compiler/CodeGen/Binder.cs` lines 495-503 (ResolveExpressionLocation)

---

## 2. NC220M Infinite Loop at Runtime

**Status**: Hang confirmed (DEVLOG Entry 125). Root cause not definitively identified.

**What NC220M does**: Tests indexed identifiers and qualified datanames with MULTIPLY,
PERFORM (UNTIL and VARYING), and DISPLAY statements.

**Test structure** (in execution order):
1. MULTIPLY tests with indexed operands (lines ~383-919)
2. PERFORM UNTIL with subscripted conditions (lines 920-968):
   - `PERFORM PARAGRAPH-A UNTIL TABLE7-NUM(INDEX7) = TABLE8-NUM(INDEX8)`
   - PARAGRAPH-A: `ADD 1 TO TABLE7-NUM(INDEX7)`
3. PERFORM VARYING with subscripted UNTIL (lines 970-1025):
   - `PERFORM PARAGRAPH-B VARYING NUM-9 FROM 1 BY 1 UNTIL NUM-9 > TABLE8-NUM(INDEX8)`
   - PARAGRAPH-B: `MOVE NUM-9 TO TABLE7-NUM(INDEX7)`

**Binder analysis** -- PERFORM VARYING lowering (lines 823-875):
```csharp
// Line 846-851: TEST BEFORE only (hardcoded)
LowerCondition(v.UntilCondition, condVal, loopStart);
loopStart.Instructions.Add(new IrBranchIfFalse(condVal, loopBody));
loopStart.Instructions.Add(new IrJump(loopEnd));
```

**Key findings**:
- **No TEST BEFORE/AFTER support**: `BoundPerformVarying` has no `IsTestBefore`/`IsTestAfter`
  property (file `BoundNodes.cs` lines 322-339). The binder always generates TEST BEFORE
  semantics. NC220M does NOT use TEST AFTER syntax, so this is not the cause.
- **No COBOL0500+ diagnostics emitted**: The comparison and subscript resolution code paths
  all look correct for the NC220M patterns.
- **Likely root cause**: The hang probably occurs in the PERFORM UNTIL section (before
  VARYING), not the VARYING section. If `ADD 1 TO TABLE7-NUM(INDEX7)` with an index-based
  subscript writes to the wrong location, the UNTIL condition never becomes true. This would
  be a CilEmitter issue in how `IrPicAddLiteral` handles `IrElementRef` destinations, not a
  Binder issue. Needs runtime debugging to confirm.
- **Alternative cause**: The MULTIPLY tests before the PERFORM section might contain an
  infinite loop if any PERFORM UNTIL or PERFORM VARYING is embedded there (not likely based
  on grep results showing PERFORM UNTIL only in the later section).

**Files**:
- `/home/user/CobolSharp/src/CobolSharp.Compiler/CodeGen/Binder.cs` lines 754-875 (LowerPerform, LowerPerformVarying)
- `/home/user/CobolSharp/src/CobolSharp.Compiler/Semantics/Bound/BoundNodes.cs` lines 297-339 (BoundPerformStatement, BoundPerformVarying)
- `/home/user/CobolSharp/tests/nist/programs/NC220M.cob` lines 920-1025 (PERFORM UNTIL/VARYING tests)

---

## 3. WRITE FROM / REWRITE FROM Lowering

**WRITE FROM -- Fully Implemented** (Binder.cs lines 1069-1112):

```csharp
// Line 1078-1085: WRITE FROM handling
if (wr.From != null)
{
    var fromLoc = ResolveExpressionLocation(wr.From);
    if (fromLoc != null)
        block.Instructions.Add(new IrMoveFieldToField(
            fromLoc, recordLoc,
            GetPicForLocation(fromLoc), GetPicForLocation(recordLoc)));
}
```

The FROM clause is correctly lowered as a MOVE from the FROM source to the record buffer,
followed by the actual WRITE. `BoundWriteStatement.From` is a `BoundExpression?` property
(BoundNodes.cs line 346).

**REWRITE FROM -- NOT Implemented** (Binder.cs lines 1240-1249):

```csharp
private void LowerRewrite(BoundRewriteStatement rw, IrBasicBlock block)
{
    string cobolName = rw.File.Name;
    var recordLoc = ResolveLocation(rw.Record);
    if (recordLoc != null)
    {
        block.Instructions.Add(new IrRewriteRecordFromStorage(cobolName, recordLoc));
    }
    EmitFileStatus(rw.File, block);
}
```

The `BoundRewriteStatement` has a `From` property (BoundNodes.cs line 493), but
`LowerRewrite` never checks or uses it. The FROM clause is bound and validated but
silently ignored during lowering. The fix is to add the same MOVE pattern used in
`LowerWrite` lines 1078-1085.

**Files**:
- `/home/user/CobolSharp/src/CobolSharp.Compiler/CodeGen/Binder.cs` lines 1069-1112 (LowerWrite -- correct)
- `/home/user/CobolSharp/src/CobolSharp.Compiler/CodeGen/Binder.cs` lines 1240-1249 (LowerRewrite -- missing FROM)
- `/home/user/CobolSharp/src/CobolSharp.Compiler/Semantics/Bound/BoundNodes.cs` lines 489-503 (BoundRewriteStatement with From property)

---

## 4. INVALID KEY / AT END Branching Duplication

Five methods contain similar two-way branching patterns. Three use the full IR branching
pattern (READ, DELETE, START); two use a stub-only approach (RETURN, CALL).

### Full branching pattern (3 methods, ~18 lines each)

**LowerRead** (lines 1188-1216) -- AT END / NOT AT END:
```csharp
var atEndResult = _valueFactory.Next(IrPrimitiveType.Bool);
block.Instructions.Add(new IrCheckFileAtEnd(cobolName, atEndResult));
var atEndBlock = method.CreateBlock("read_at_end");
var notAtEndBlock = method.CreateBlock("read_not_at_end");
var afterBlock = method.CreateBlock("read_after");
block.Instructions.Add(new IrBranchIfFalse(atEndResult, notAtEndBlock));
block.Instructions.Add(new IrJump(atEndBlock));
// AT END block: lower statements, jump to after
// NOT AT END block: lower statements, jump to after
```

**LowerDelete** (lines 1260-1286) -- INVALID KEY / NOT INVALID KEY:
Identical structure, using `IrCheckFileInvalidKey`, block names "delete.invalid" etc.

**LowerStart** (lines 1317-1343) -- INVALID KEY / NOT INVALID KEY:
Identical structure, using `IrCheckFileInvalidKey`, block names "start.invalid" etc.

### Stub-only pattern (2 methods)

**LowerReturn** (lines 1350-1365):
No branching -- always takes AT END path linearly. Emits display stub.

**LowerCall** (lines 1369-1384):
No branching -- always takes ON EXCEPTION path linearly. Emits display stub.

### Duplication count

The DELETE and START branching patterns are structurally identical (18 lines each, differing
only in variable names and block label prefixes). READ uses the same shape but with
`IrCheckFileAtEnd` instead of `IrCheckFileInvalidKey`. Total: ~54 lines of near-duplicate
branching logic across 3 methods. A helper like `LowerTwoWayBranch(condition, trueStmts,
falseStmts, ...)` would reduce this to ~15 lines + 3 one-line call sites.

**Files**:
- `/home/user/CobolSharp/src/CobolSharp.Compiler/CodeGen/Binder.cs` lines 1188-1216 (LowerRead branching)
- `/home/user/CobolSharp/src/CobolSharp.Compiler/CodeGen/Binder.cs` lines 1260-1286 (LowerDelete branching)
- `/home/user/CobolSharp/src/CobolSharp.Compiler/CodeGen/Binder.cs` lines 1317-1343 (LowerStart branching)
- `/home/user/CobolSharp/src/CobolSharp.Compiler/CodeGen/Binder.cs` lines 1350-1365 (LowerReturn stub)
- `/home/user/CobolSharp/src/CobolSharp.Compiler/CodeGen/Binder.cs` lines 1369-1384 (LowerCall stub)

**Note**: `BoundWriteStatement` does NOT have InvalidKey/NotInvalidKey properties at all
(BoundNodes.cs lines 342-362). WRITE INVALID KEY is not supported at the bound tree level.

---

## 5. Duplicate GetPicForLocation

**Two identical implementations exist**:

**Binder.cs** (lines 523-532):
```csharp
private static Runtime.PicDescriptor GetPicForLocation(IrLocation loc)
{
    return loc switch
    {
        IrStaticLocation s => s.Location.Pic,
        IrElementRef e => e.ElementPic,
        IrRefModLocation r => GetPicForLocation(r.Base),
        _ => throw new InvalidOperationException($"Unknown IrLocation type: {loc.GetType().Name}")
    };
}
```

**CilEmitter.cs** (lines 2597-2606):
```csharp
private static Runtime.PicDescriptor GetPicForLocation(IR.IrLocation loc)
{
    return loc switch
    {
        IR.IrStaticLocation s => s.Location.Pic,
        IR.IrElementRef e => e.ElementPic,
        IR.IrRefModLocation r => GetPicForLocation(r.Base),
        _ => throw new NotSupportedException($"Unknown IrLocation type: {loc.GetType().Name}")
    };
}
```

**Differences**:
1. Exception type: `InvalidOperationException` vs `NotSupportedException` (in the default arm)
2. Namespace qualifiers: Binder uses unqualified names; CilEmitter prefixes with `IR.`

**Both are `private static`**, so they cannot share. The function should be a public static
method on `IrLocation` itself, or a static utility method in a shared helper class.

**What it does**: Extracts the `PicDescriptor` from any `IrLocation` variant. For static
locations, returns the storage PIC. For element refs (subscripted), returns the element PIC.
For ref-mod locations, recurses into the base location.

**Usage**: Called 18 times in Binder.cs, 3 times in CilEmitter.cs.

**Files**:
- `/home/user/CobolSharp/src/CobolSharp.Compiler/CodeGen/Binder.cs` lines 523-532
- `/home/user/CobolSharp/src/CobolSharp.Compiler/CodeGen/CilEmitter.cs` lines 2597-2606

---

## 6. Raw String Diagnostic Codes in Binder.cs

All diagnostic emissions in Binder.cs use raw strings instead of `DiagnosticDescriptor`
references. The rest of the compiler (BoundTreeBuilder, BoundTreeValidator, SymbolValidator,
etc.) uses `DiagnosticDescriptors` defined in
`/home/user/CobolSharp/src/CobolSharp.Compiler/Diagnostics/DiagnosticDescriptors.cs`.

### Complete list (17 instances):

| Line | Code | Context |
|------|------|---------|
| 829 | `"COBOL0500"` | PERFORM VARYING index has no storage location |
| 932 | `"COBOL0501"` | PERFORM target paragraph not found |
| 983 | `"COBOL0502"` | PERFORM TIMES target paragraph not found |
| 1591 | `"COBOL0510"` | SET target has no storage location |
| 1624 | `"COBOL0511"` | SET TO: cannot resolve value expression |
| 1638 | `"COBOL0512"` | SET UP BY: cannot resolve delta expression |
| 1652 | `"COBOL0513"` | SET DOWN BY: cannot resolve delta expression |
| 2390 | `"COBOL0503"` | Unsupported condition shape |
| 2406 | `"COBOL0504"` | Cannot normalize comparison operands |
| 2482 | `"COBOL0505"` | Unhandled comparison combination |
| 2726 | `"COBOL0506"` | GO TO target paragraph not found |
| 2736 | `"COBOL0507"` | GO TO DEPENDING ON requires selector |
| 2746 | `"COBOL0508"` | GO TO DEPENDING ON selector has no storage |
| 2761 | `"COBOL0506"` | GO TO DEPENDING target not found (reused code) |
| 2795 | `"COBOL0509"` | EXIT PERFORM outside active PERFORM |
| 2814 | `"COBOL0509"` | EXIT PARAGRAPH outside paragraph (reused code) |
| 2832 | `"COBOL0509"` | EXIT SECTION outside section (reused code) |

All 17 instances follow the same pattern:
```csharp
_diagnostics.ReportError("COBOL0NNN",
    "message string",
    new Common.SourceLocation("<source>", 0, 0, 0),
    new Common.TextSpan(0, 0));
```

**Problems**:
1. No `DiagnosticDescriptor` definitions for COBOL0500-COBOL0513
2. Hardcoded source location `"<source>", 0, 0, 0` -- no actual source position
3. Hardcoded empty text span `0, 0` -- no source highlighting
4. Duplicate code IDs: COBOL0506 used twice (GO TO target not found, GO TO DEPENDING
   target not found), COBOL0509 used three times (EXIT PERFORM, EXIT PARAGRAPH, EXIT SECTION)

**Files**:
- `/home/user/CobolSharp/src/CobolSharp.Compiler/CodeGen/Binder.cs` (all 17 instances)
- `/home/user/CobolSharp/src/CobolSharp.Compiler/Diagnostics/DiagnosticDescriptors.cs` (where descriptors should be defined)

---

## Summary of Remediation Tasks

| # | Issue | Severity | Effort |
|---|-------|----------|--------|
| 1 | NC121M subscripted DIVIDE | Already fixed | N/A |
| 2 | NC220M infinite loop | Bug | Medium -- needs runtime debugging |
| 3 | REWRITE FROM not lowered | Bug | Small -- 7-line addition |
| 4 | Branching duplication (READ/DELETE/START) | Tech debt | Small -- extract helper |
| 5 | Duplicate GetPicForLocation | Tech debt | Small -- move to IrLocation |
| 6 | Raw diagnostic strings (17 instances) | Tech debt | Medium -- define descriptors, wire source locations |
| 6a | ADD/SUBTRACT/DIVIDE use ResolveLocation not ResolveExpressionLocation for targets | Latent bug | Small -- change to ResolveExpressionLocation for ref-mod support |
| 7 | WRITE INVALID KEY not in bound tree | Feature gap | Medium -- add to BoundWriteStatement + builder + binder |
