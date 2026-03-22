# Grammar/Parsing Gap Research

Date: 2026-03-22

## 1. VALUE THRU in Level-88

### Finding: FULLY IMPLEMENTED -- Not a grammar gap

The grammar, semantic builder, bound tree, and codegen all handle VALUE THRU correctly.

**Grammar** (`src/CobolSharp.Compiler/Grammar/Core/CobolData.g4`):
- `valueClause` (line 206-208): `(VALUE | VALUES) (IS | ARE)? valueItem (COMMA? valueItem)*`
- `valueItem` (line 210-213): `valueRange | valueOperand+`
- `valueRange` (`Core/CobolExpressions.g4` line 26-27): `valueOperand (THRU | THROUGH) valueOperand`

**Semantic builder** (`Semantics/SemanticBuilder.cs` lines 437-481):
- Level-88 detection at line 438
- Iterates `valClause.valueItem()`, checks for `item.valueRange()` (THRU) vs individual `valueOperand`s
- Calls `ConditionValue.FromObject(ParseConditionValueOperand(...))` for both FROM and TO
- Creates `ConditionSymbol.AddRange(from, to)` for ranges

**Bound nodes** (`Semantics/Bound/BoundNodes.cs` lines 202-217):
- `BoundConditionNameExpression` carries `ConditionSymbol` with `ValueRanges`

**Codegen** (`CodeGen/Binder.cs` lines 2601-2700):
- `LowerConditionName()` handles both single values (parent == from) and THRU ranges (parent >= from AND parent <= to)
- Supports both numeric and string comparisons

**DEVLOG correction**: The DEVLOG entries listing NC201A, NC250A, NC252A under "VALUE THRU" are misleading. These tests are blocked by other issues:
- NC201A: period-terminated inline PERFORM and other parse issues
- NC250A: similar multi-issue parse failures
- NC252A: uses RENAMES THRU (not VALUE THRU) -- test is about REDEFINES/RENAMES

### Changes needed: None for VALUE THRU itself

---

## 2. ASCENDING/DESCENDING KEY in OCCURS

### Finding: FULLY PARSED AND STORED -- Runtime gap only

**Grammar** (`src/CobolSharp.Compiler/Grammar/Core/CobolData.g4`):
- `occursClause` (lines 179-184): includes `occursKeyClause*`
- `occursKeyClause` (lines 186-188): `(ASCENDING | DESCENDING) KEY? IS? dataReference+`

**Semantic builder** (`Semantics/SemanticBuilder.cs` lines 518-532):
- Iterates `occClause.occursKeyClause()`, checks `keyClause.ASCENDING()` vs DESCENDING
- Extracts key data-names into `ascKeys` / `descKeys` lists
- Stored in `OccursInfo` constructor (line 542-546)

**Data model** (`Semantics/DataSymbol.cs` lines 24-28):
- `OccursInfo.AscendingKeys` and `OccursInfo.DescendingKeys` are `IReadOnlyList<string>`

**Validation** (`Semantics/DataItemClassifier.cs` lines 57-80):
- `ValidateOccursKeys()` resolves key names, checks subordination, checks not group item

**SEARCH ALL usage** (`Semantics/Bound/BoundTreeBuilder.cs` lines 2916-2925):
- `ValidateSearchAllStatement()` checks that table has KEY clause (AscendingKeys or DescendingKeys count > 0)

**NIST test status per DEVLOG**:
- NC238A: 10/10 (100%) -- already passing
- NC237A: compiles but hangs at runtime (PERFORM VARYING negative step issue, not KEY issue)
- NC233A, NC247A: partially unblocked, may have remaining runtime issues

### Changes needed: None for grammar/semantics

The CLAUDE.md "known gap" entry for ASCENDING/DESCENDING KEY is stale. The grammar parses it, the semantic builder stores it, and validation uses it. Remaining failures in NC233A/NC237A/NC247A are caused by other issues (runtime PERFORM VARYING, SEARCH index management).

---

## 3. Reserved Words as Paragraph Names (STATUS, PROGRAM)

### Finding: TWO DISTINCT ISSUES

#### 3a. STATUS in SPECIAL-NAMES implementor switch

**Grammar** (`src/CobolSharp.Compiler/Grammar/Core/CobolSpecialNames.g4` lines 28-32):
```
implementorSwitchEntry
    : IDENTIFIER IS IDENTIFIER
      (ON IDENTIFIER)?
      (OFF IS? IDENTIFIER)?
    ;
```

**Lexer** (`Grammar/CobolLexer.g4` line 271): `STATUS : 'STATUS' ;`

**The conflict**: COBOL-85 standard format is:
```
implementor-name IS mnemonic-name
  ON STATUS IS condition-name
  OFF STATUS IS condition-name
```
The `STATUS` token is a keyword, not an `IDENTIFIER`. The current rule `(ON IDENTIFIER)?` cannot match `ON STATUS IS condition-name` because `STATUS` lexes as the `STATUS` token.

**Affected tests**: NC174A, NC211A, NC254A (per DEVLOG)

**Required changes**:
1. `CobolSpecialNames.g4` line 28-32: Change rule to:
```
implementorSwitchEntry
    : IDENTIFIER IS IDENTIFIER
      (ON STATUS? IS? IDENTIFIER)?
      (OFF STATUS? IS? IDENTIFIER)?
    ;
```
This allows both `ON STATUS IS cond-name` and the shortened `ON cond-name` form.

2. `SemanticBuilder.cs`: The `VisitImplementorSwitchEntry` (or equivalent) must extract the ON/OFF condition names from the new rule shape.

#### 3b. PROGRAM COLLATING SEQUENCE in OBJECT-COMPUTER

**Grammar** (`CobolParserCore.g4` lines 206-208):
```
objectComputerParagraph
    : OBJECT_COMPUTER DOT computerName computerAttributes? DOT
    ;
```
where `computerAttributes` (line 214-216) is `(IDENTIFIER | STRINGLIT | INTEGERLIT)+`.

**Lexer** (`Grammar/CobolLexer.g4` line 249): `PROGRAM : 'PROGRAM' ;`

**The conflict**: `PROGRAM COLLATING SEQUENCE IS alphabet-name` appears in the OBJECT-COMPUTER paragraph. `PROGRAM` is a keyword token, so `computerAttributes` (which only accepts `IDENTIFIER | STRINGLIT | INTEGERLIT`) cannot consume it. The parser chokes when it encounters `PROGRAM` in this position.

**Affected tests**: NC215A, NC219A, NC114M, NC214M (per DEVLOG)

**Required changes**:
1. Option A (minimal): Add `PROGRAM` to the `computerAttributes` alternative list:
   `CobolParserCore.g4` line 215: `(IDENTIFIER | STRINGLIT | INTEGERLIT | PROGRAM)+`
   This is a hack but matches the existing fallback pattern.

2. Option B (proper): Add a dedicated `programCollatingSequenceClause` rule:
   ```
   objectComputerParagraph
       : OBJECT_COMPUTER DOT computerName
         programCollatingSequence?
         computerAttributes?
         DOT
       ;

   programCollatingSequenceClause
       : PROGRAM COLLATING SEQUENCE IS? IDENTIFIER
       ;
   ```
   This requires adding `COLLATING` and `SEQUENCE` as lexer tokens (or allowing them as IDENTIFIER since they aren't currently reserved).

3. Semantic builder: If Option B, add a visitor to capture the collating sequence alphabet name.

---

## 4. SORT/MERGE

### Finding: PARSED BUT NO BINDING OR IR

**Grammar** -- fully defined:

`src/CobolSharp.Compiler/Grammar/Core/CobolIO.g4`:
- `sortStatement` (lines 256-265): SORT sortFileName sortKeyPhrase* sortUsingPhrase? sortGivingPhrase? sortInputProcedurePhrase? sortOutputProcedurePhrase? END_SORT?
- `mergeStatement` (lines 295-303): MERGE mergeFileName mergeKeyPhrase+ mergeUsingPhrase mergeOutputProcedurePhrase? mergeGivingPhrase? END_MERGE?
- Supporting rules: `sortFileName`, `sortKeyPhrase`, `sortUsingPhrase`, `sortGivingPhrase`, `sortInputProcedurePhrase`, `sortOutputProcedurePhrase` (lines 267-289)
- Same pattern for MERGE (lines 305-323)

`CobolParserCore.g4`:
- `sortStatement` wired into `statement` at line 362
- `mergeStatement` wired into `statement` at line 350

**Lexer tokens**: `SORT` (line 118), `MERGE` (line 107), `END_SORT` (line 26), `END_MERGE` (line 27) all defined in `CobolLexer.g4`.

**Bound nodes**: NONE. No `BoundSortStatement` or `BoundMergeStatement` in `BoundNodes.cs`.

**Binder**: NONE. No `BindSort()` or `BindMerge()` methods in `BoundTreeBuilder.cs`. The `BindStatement()` method (lines 230-267) has no case for `ctx.sortStatement()` or `ctx.mergeStatement()`. When encountered, these fall through to the "not recognized" warning at line 262.

**Semantic builder**: No visitor for sort/merge statement contexts.

**Related nodes that DO exist**:
- `BoundReturnStatement` (RETURN ... AT END) -- bound + IR stub
- `releaseStatement` -- grammar exists, no binding

### Required changes for SORT:

1. **BoundNodes.cs**: Add new types:
   - `BoundSortStatement` with properties: SortFile (BoundIdentifierExpression), Keys (list of key+direction), UsingFiles (list), GivingFiles (list), InputProcedure (procedure name?), OutputProcedure (procedure name?)
   - `BoundSortKeyEntry` record with DataName + IsAscending

2. **BoundTreeBuilder.cs**:
   - Add `if (ctx.sortStatement() is { } sortCtx) return BindSort(sortCtx);` in BindStatement
   - Implement `BindSort()` method
   - Add `if (ctx.mergeStatement() is { } mergeCtx) return BindMerge(mergeCtx);` in BindStatement
   - Implement `BindMerge()` method

3. **BoundTreeValidator.cs**: Add validation rules (file must be SD, keys must be subordinate, etc.)

4. **CodeGen/Binder.cs**: Add IR lowering (stub initially, like CALL/RETURN)

5. **SemanticBuilder.cs**: May need SD (Sort Description) support in FILE SECTION -- currently only FD is handled.

### Required changes for MERGE: Same pattern as SORT.

---

## 5. ALTERNATE KEY

### Finding: PARSED BUT IGNORED IN SEMANTICS

**Grammar** (`src/CobolSharp.Compiler/Grammar/Core/CobolIO.g4` lines 70-73):
```
alternateKeyClause
    : ALTERNATE KEY IS dataReference
      (WITH? DUPLICATES)?
    ;
```

Also wired into:
- `fileControlClauses` (line 40): `alternateKeyClause` is an alternative
- `fileDescriptionClause` (`CobolData.g4` line 43): `alternateKeyClause` is an alternative

**Lexer**: `ALTERNATE` token defined at `CobolLexer.g4` line 141.

**Semantic builder** (`Semantics/SemanticBuilder.cs` lines 244-262):
The `VisitFileControlClauseGroup` iterates `fileControlClauses()` and checks for:
- `organizationClause` -- handled
- `accessModeClause` -- handled
- `recordKeyClause` -- handled (stores in `fileSym.RecordKey`)
- `fileStatusClause` -- handled
- **`alternateKeyClause`** -- NOT checked. There is no `clause.alternateKeyClause()` call anywhere.

**FileSymbol** (`Semantics/ProgramSymbol.cs` lines 63-91):
No property for alternate keys. The class has `RecordKey` (string?) but no `AlternateKeys` collection.

**BoundTreeValidator** (`Semantics/Bound/BoundTreeValidator.cs` line 377):
Comment mentions "CBL1703: READ KEY not a record/alternate key of file" but there's no alternate key data to validate against.

### Required changes:

1. **ProgramSymbol.cs** (`FileSymbol` class, after line 78):
   Add:
   ```csharp
   /// <summary>ALTERNATE KEY identifier names (for INDEXED files).</summary>
   public List<AlternateKeyInfo> AlternateKeys { get; } = [];
   ```
   Add supporting record:
   ```csharp
   public sealed record AlternateKeyInfo(string DataName, bool AllowDuplicates);
   ```

2. **SemanticBuilder.cs** (inside `VisitFileControlClauseGroup`, after line 259):
   Add:
   ```csharp
   if (clause.alternateKeyClause() is { } altKeyClause)
   {
       string keyName = altKeyClause.dataReference().GetText();
       bool allowDups = altKeyClause.DUPLICATES() != null;
       fileSym.AlternateKeys.Add(new AlternateKeyInfo(keyName, allowDups));
   }
   ```

3. **BoundTreeValidator.cs**: Update READ KEY validation (CBL1703) to check alternate keys in addition to record key.

4. **CodeGen/Binder.cs**: Pass alternate key info when lowering READ/WRITE/REWRITE/START for indexed files.

---

## Summary Table

| Gap | Grammar | Semantics | Bound | IR/Codegen | Actual Status |
|-----|---------|-----------|-------|------------|---------------|
| VALUE THRU | Done | Done | Done | Done | **Complete** -- DEVLOG entry is stale |
| ASCENDING/DESCENDING KEY | Done | Done | N/A (data, not stmt) | Validated in SEARCH ALL | **Complete** -- DEVLOG entry is stale |
| STATUS in SPECIAL-NAMES | Missing STATUS in rule | Missing | N/A | N/A | Needs grammar fix |
| PROGRAM COLLATING SEQ | Missing PROGRAM in attrs | Missing | N/A | N/A | Needs grammar fix + optional semantics |
| SORT statement | Done | Not visited | No BoundSortStatement | No IR | Needs bind + IR stub |
| MERGE statement | Done | Not visited | No BoundMergeStatement | No IR | Needs bind + IR stub |
| ALTERNATE KEY | Done | Ignored | N/A | N/A | Needs semantic extraction + FileSymbol storage |

### Priority ordering for remediation:
1. **STATUS in SPECIAL-NAMES** (small grammar fix, unblocks 3 NIST tests)
2. **PROGRAM COLLATING SEQUENCE** (small grammar fix, unblocks 4 NIST tests)
3. **ALTERNATE KEY semantic extraction** (small code change, improves READ KEY validation)
4. **SORT/MERGE binding** (medium effort, full pipeline from bound nodes through IR stub)
