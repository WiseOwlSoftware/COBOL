# Semantic Analysis / Binding Gap Research

Date: 2026-03-22

## 1. Duplicate Expression Binding

**Files:**
- `/home/user/CobolSharp/src/CobolSharp.Compiler/Semantics/Bound/BoundTreeBuilder.cs`

**Two parallel implementations exist:**

### Set A: `BindFullExpression` chain (lines 2236-2330)
```
BindFullExpression           (line 2236)
  -> BindAdditiveExpression  (line 2241)
  -> BindMultiplicativeExpression (line 2258)
  -> BindPowerExpression     (line 2275)
  -> BindUnaryExpression     (line 2293)
  -> BindPrimaryExpression   (line 2312)
```

**Callers:** COMPUTE (line 2225), PERFORM VARYING FROM/BY (lines 556-569), EVALUATE subjects
(line 615), EVALUATE WHEN fallback (line 683), value operand (line 729), CALL BY VALUE (line 1088),
parenthesized recursion (line 2323), DIVIDE target (line 2769).

### Set B: `BindArithmeticExpr` chain (lines 2994-3082)
```
BindArithmeticExpr           (line 2994)
  -> BindAdditiveExpr        (line 3003)
  -> BindMultiplicativeExpr  (line 3016)
  -> BindPowerExpr           (line 3029)
  -> BindUnaryExpr           (line 3040)
  -> BindPrimaryExpr         (line 3056)
```

**Callers:** condition binding arithmetic (lines 1783, 1812), subscript binding (line 3152),
reference modification (lines 3241, 3245), parenthesized recursion (line 3074).

### Are they truly duplicates?

**Functionally yes, with minor differences:**

1. **Null-safety:** `BindArithmeticExpr` accepts `null` and returns literal 0 (line 2996-2997);
   `BindFullExpression` does not accept null.

2. **Unary expression order:** `BindUnaryExpression` (Set A) checks for `addOp` first, then falls
   through to `BindPrimaryExpression`. `BindUnaryExpr` (Set B) checks `primaryExpression()` first,
   then handles unary. Functionally equivalent since the grammar makes these mutually exclusive
   alternatives.

3. **Primary expression fallback:** `BindPrimaryExpression` (Set A, line 2317) checks
   `dataReference` before `arithmeticExpression`; `BindPrimaryExpr` (Set B, line 3077) checks
   `arithmeticExpression` before `dataReference`. Order doesn't matter since only one can be
   non-null.

4. **Array access style:** Set A uses `ctx.multiplicativeExpression()` returning array; Set B uses
   `ctx.multiplicativeExpression(0)` with indexer. Both are valid ANTLR accessor patterns.

**Verdict:** These are true duplicates that should be unified. The Set B versions are slightly more
robust (null-safe entry point, uses `dataReference` for subscript binding). Both share the same
function-call and unresolved-identifier fallback bugs.

---

## 2. Silent Zero for Function Calls

**File:** `BoundTreeBuilder.cs`

**Two locations produce silent zero for intrinsic function calls:**

### Location 1: `BindPrimaryExpression` (Set A), line 2325-2327
```csharp
// functionCall -- bind as identifier for now
if (ctx.functionCall() != null)
    return new BoundLiteralExpression(0m, CobolCategory.Numeric);
```

### Location 2: `BindPrimaryExpr` (Set B), lines 3063-3068
```csharp
// functionCall
var funcCall = ctx.functionCall();
if (funcCall != null)
{
    // TODO: proper function binding
    return new BoundLiteralExpression(0m, CobolCategory.Numeric);
}
```

### Code path

Any COBOL arithmetic expression containing a FUNCTION reference (e.g., `FUNCTION LENGTH(X)`,
`FUNCTION MAX(A B)`, `FUNCTION MOD(X Y)`) hits `primaryExpression` during recursive descent.
The grammar's `functionCall` rule matches `FUNCTION functionName ( arguments )`. Instead of
binding the function semantics, both paths silently return decimal `0`.

### Impact

- No diagnostic is emitted -- the compiler silently produces wrong results.
- Any COBOL program using intrinsic functions in COMPUTE, conditions, subscripts, or reference
  modification gets incorrect behavior at runtime.
- Common functions affected: LENGTH, MAX, MIN, MOD, ORD, ORD-MIN, ORD-MAX, INTEGER, INTEGER-PART,
  NUMVAL, UPPER-CASE, LOWER-CASE, REVERSE, TRIM, WHEN-COMPILED, etc.

---

## 3. Unresolved Identifiers as String Literals

**File:** `BoundTreeBuilder.cs`

### Location 1: `BindPrimaryExpr` (Set B), line 3081
```csharp
return new BoundLiteralExpression(ctx.GetText(), CobolCategory.Alphanumeric);
```
This is the terminal fallback in `BindPrimaryExpr`. It fires when none of the recognized
alternatives (numericLiteral, functionCall, arithmeticExpression, dataReference) match. In practice
this catches parse tree nodes that the binder doesn't understand. The identifier name becomes a
string literal value.

### Location 2: `BindDataReferenceWithSubscripts`, line 3136-3137
```csharp
if (sym == null)
    return new BoundLiteralExpression(name, CobolCategory.Alphanumeric);
```
When `_semantic.ResolveData(name)` returns null (identifier not found in any scope), the unresolved
name is silently converted to a string literal containing the identifier's text.

### Location 3: `BindDataReferenceOrLiteral`, line 2991
```csharp
return new BoundLiteralExpression(text, CobolCategory.Alphanumeric);
```
Same pattern: if the text isn't a decimal and isn't a known data symbol, it becomes a string literal.

### Impact

- No "undefined identifier" diagnostic is emitted.
- Runtime behavior is silently wrong: MOVE of an undefined variable moves the variable's *name* as
  a string rather than failing.
- This masks typos and missing data declarations.

---

## 4. Hardcoded START Condition

**File:** `/home/user/CobolSharp/src/CobolSharp.Compiler/CodeGen/Binder.cs`, line 1293-1312

### The code
```csharp
private IrBasicBlock LowerStart(BoundStartStatement start, IrMethod method, IrBasicBlock block)
{
    string cobolName = start.File.Name;
    var recordKey = start.File.RecordKey;
    if (recordKey != null)
    {
        var keySym = _semantic.ResolveData(recordKey);
        if (keySym != null)
        {
            var keyLoc = ResolveLocation(keySym);
            if (keyLoc != null)
            {
                // Default to Equal if no KEY IS clause
                int condition = 0; // StartCondition.Equal
                block.Instructions.Add(new IrStartFile(cobolName, keyLoc, condition));
            }
        }
    }
    // ...
}
```

### What `condition = 0` means

The runtime enum `StartCondition` (in `src/CobolSharp.Runtime/IO/IFileHandler.cs`, line 61) is:
```
Equal = 0, GreaterThan = 1, GreaterThanOrEqual = 2, LessThan = 3, LessThanOrEqual = 4
```

So `condition = 0` means `StartCondition.Equal`. This is hardcoded regardless of what the COBOL
source says.

### What BoundStartStatement already carries

`BoundStartStatement` has a `KeyCondition` property (type `BoundExpression?`, line 1148 of
BoundNodes.cs) that is properly bound from the parse tree's `startKeyPhrase` -> `comparisonExpression`
(BoundTreeBuilder.cs line 972-973). This is a `BoundBinaryExpression` with a comparison operator
(e.g., `GreaterThan`, `Equal`, etc.).

### What should happen

`LowerStart` should read `start.KeyCondition`, extract the comparison operator from the
`BoundBinaryExpression`, map it to the `StartCondition` enum, and pass it as the `condition`
parameter. Currently the bound key condition is computed but completely ignored during IR lowering.

---

## 5. PROCEDURE DIVISION USING / RETURNING

**Files:**
- Grammar: `CobolParserCore.g4`, line 222-234
- SemanticBuilder: `SemanticBuilder.cs`, line 659-662
- Symbols: `ProgramSymbol.cs`, lines 170-212

### Grammar coverage

The grammar parses `PROCEDURE DIVISION USING dataReferenceList` and
`RETURNING dataReference` (COBOL-2002 only, gated by `{is2002()}?`):
```
procedureDivision
    : PROCEDURE DIVISION usingClause? ({is2002()}? returningClause)? DOT
      declarativePart*
      procedureUnit*
    ;

usingClause : USING dataReferenceList ;
returningClause : RETURNING dataReference ;
```

### SemanticBuilder coverage

`VisitProcedureDivision` (line 659) only pushes the procedure division scope and calls
`base.VisitProcedureDivision`. There is **no** `VisitUsingClause` or `VisitReturningClause` method.
The USING/RETURNING parameters are parsed by ANTLR but completely ignored during semantic analysis.

### Symbol infrastructure

`ProcedureSymbol` (line 182) and `ProcedureParameter` (line 200) are defined with full
`ParameterMode`, `DataSymbol`, and `Returning` support. But these types are only used for
`BoundCallStatement` argument validation (outbound CALL), not for binding the current program's
own parameter declarations.

### What's missing

1. No visitor processes `usingClause` / `returningClause` in `SemanticBuilder`.
2. The program's own `ProcedureParameter` list is never populated.
3. LINKAGE SECTION items referenced in USING are never validated (CBL3108, CBL3109 are dormant).
4. No IR lowering generates parameter-receiving code.

---

## 6. Dormant Diagnostics

The following 47 diagnostic descriptors are defined in `DiagnosticDescriptors.cs` but never emitted
anywhere in the codebase (excluding the descriptor definition file itself):

### MOVE enforcement (2 dormant / 5 defined)
| Code | Description | Category |
|------|-------------|----------|
| CBL0902 | MOVE CORRESPONDING: source must be group item | MOVE CORR not implemented |
| CBL0903 | MOVE CORRESPONDING: target must be group item | MOVE CORR not implemented |
| CBL0904 | MOVE figurative constant to numeric target | Missing validation |
| CBL0905 | MOVE to level-88 condition name | Missing validation |

### VALUE clause (2 dormant / 4 defined)
| Code | Description | Category |
|------|-------------|----------|
| CBL1003 | Extra VALUE items ignored | Missing validation |
| CBL1004 | Condition value incompatible with parent | Missing validation |

### OCCURS / DEPENDING ON (1 dormant / 5 defined)
| Code | Description | Category |
|------|-------------|----------|
| CBL1102 | DEPENDING ON must be declared before table | Missing validation |

### SEARCH (3 dormant / 5 defined)
| Code | Description | Category |
|------|-------------|----------|
| CBL1201 | SEARCH VARYING must be index or integer | Missing validation |
| CBL1203 | KEY not an OCCURS key of table | Missing validation |
| CBL1205 | SEARCH ALL WHEN must be simple key comparison | Missing validation |

### STRING (2 dormant / 4 defined)
| Code | Description | Category |
|------|-------------|----------|
| CBL1302 | STRING source must be alphanumeric or group | Missing validation |
| CBL1303 | STRING source cannot be numeric | Missing validation |

### UNSTRING (3 dormant / 6 defined)
| Code | Description | Category |
|------|-------------|----------|
| CBL1402 | UNSTRING INTO target must be alphanumeric or group | Missing validation |
| CBL1403 | UNSTRING DELIMITER must be alphanumeric | Missing validation |
| CBL1404 | UNSTRING COUNT must be integer numeric | Missing validation |

### INSPECT (1 dormant / 3 defined)
| Code | Description | Category |
|------|-------------|----------|
| CBL1503 | INSPECT character operand must be alphanumeric | Missing validation |

### START (3 dormant / 5 defined)
| Code | Description | Category |
|------|-------------|----------|
| CBL1602 | START KEY must be comparison expression | Missing validation |
| CBL1604 | START KEY comparison operands incompatible | Missing validation |
| CBL1605 | START requires KEY phrase for file | Missing validation |

### READ (1 dormant / 4 defined)
| Code | Description | Category |
|------|-------------|----------|
| CBL1704 | READ INTO target must be alphanumeric or group | Missing validation |

### WRITE (2 dormant / 3 defined)
| Code | Description | Category |
|------|-------------|----------|
| CBL1802 | WRITE ADVANCING value must be numeric | Missing validation |
| CBL1803 | WRITE ADVANCING item must be integer numeric | Missing validation |

### RETURN (1 dormant / 2 defined)
| Code | Description | Category |
|------|-------------|----------|
| CBL2102 | RETURN INTO target must be alphanumeric or group | Missing validation |

### RELEASE (1 dormant / 1 defined)
| Code | Description | Category |
|------|-------------|----------|
| CBL2201 | RELEASE not a record for sort/merge file | Missing validation |

### PERFORM (1 dormant / 8 defined)
| Code | Description | Category |
|------|-------------|----------|
| CBL2301 | PERFORM paragraph not found | Missing validation |

### IF / comparison (1 dormant / 2 defined)
| Code | Description | Category |
|------|-------------|----------|
| CBL2402 | Comparison operands incompatible | Missing validation |

### Arithmetic (1 dormant / 5 defined)
| Code | Description | Category |
|------|-------------|----------|
| CBL2604 | SIZE ERROR phrase requires a numeric operation | Missing validation |

### Flow analysis (1 dormant / 4 defined)
| Code | Description | Category |
|------|-------------|----------|
| CBL3003 | Paragraph must terminate with EXIT | Missing validation |

### Scope & symbols (4 dormant / 14 defined)
| Code | Description | Category |
|------|-------------|----------|
| CBL3105 | GLOBAL not allowed in this context | Missing validation |
| CBL3106 | LOCAL shadows GLOBAL | Missing validation |
| CBL3108 | USING parameter not in LINKAGE SECTION | Blocked by item 5 above |
| CBL3109 | RETURNING item not in LINKAGE SECTION | Blocked by item 5 above |
| CBL3114 | REDEFINES not allowed for subordinate to OCCURS | Missing validation |

### File status (2 dormant / 6 defined)
| Code | Description | Category |
|------|-------------|----------|
| CBL3205 | File has more than one FILE STATUS | Missing validation |
| CBL3206 | FILE STATUS not checked before next I/O | Missing flow analysis |

### CALL (5 dormant / 6 defined)
| Code | Description | Category |
|------|-------------|----------|
| CBL3301 | CALL argument count mismatch | No inter-program validation |
| CBL3302 | Argument not valid for parameter mode | No inter-program validation |
| CBL3303 | Argument type incompatible with parameter | No inter-program validation |
| CBL3304 | RETURNING item not in LINKAGE SECTION | No inter-program validation |
| CBL3305 | CALL RETURNING type incompatible | No inter-program validation |

### Report Writer (5 dormant / 6 defined)
| Code | Description | Category |
|------|-------------|----------|
| CBL3402 | REPORT group missing TYPE | Not implemented |
| CBL3403 | SUM source not numeric | Not implemented |
| CBL3404 | CONTROL must be data-name | Not implemented |
| CBL3405 | CONTROL item not defined | Not implemented |
| CBL3406 | SUM item never referenced | Not implemented |

### Strict COBOL-85 (2 dormant / 2 defined)
| Code | Description | Category |
|------|-------------|----------|
| CBL3501 | Feature not allowed in strict COBOL-85 mode | Mode not implemented |
| CBL3502 | Feature not part of COBOL-85 | Mode not implemented |

### Summary

- **Total defined:** 107 diagnostic descriptors
- **Actually emitted:** 60
- **Dormant (never emitted):** 47 (44%)
