# Remediation Plan: Phase 1 (Grammar/Parsing), Phase 2 (Semantic Analysis), Phase 3 (Lowering/IR)

## Phase 1: Grammar and Parsing Hardening (GRA-xx)

### GRA-01: Add VALUE THRU Grammar Support

**Goal:** Support `VALUE value-1 THRU value-2` in level-88 condition-name definitions.

**Scope:**
- `src/CobolSharp.Compiler/Grammar/CobolParserCore.g4` (level-88 value clause rule)
- `src/CobolSharp.Compiler/Semantics/SemanticBuilder.cs` (visit level-88 with THRU range)
- `src/CobolSharp.Compiler/Semantics/Symbols/ConditionNameSymbol.cs` (store value ranges)

**Required changes:**
1. Extend the `conditionNameValue` grammar rule to accept `value THRU value` as an alternative.
2. Update `SemanticBuilder` to parse THRU ranges and store them in `ConditionNameSymbol`.
3. Update `BoundTreeBuilder` condition-name evaluation to generate range checks (value >= low AND value <= high).
4. Regenerate ANTLR files.

**Acceptance criteria:**
- NC201A, NC250A, NC252A NIST tests pass.
- Level-88 items with VALUE THRU compile and execute correctly.
- All existing tests continue to pass.

---

### GRA-02: Add ASCENDING/DESCENDING KEY in OCCURS

**Goal:** Parse and store ASCENDING/DESCENDING KEY clauses on OCCURS for use with SEARCH ALL.

**Scope:**
- `src/CobolSharp.Compiler/Grammar/CobolParserCore.g4` (OCCURS clause rule)
- `src/CobolSharp.Compiler/Semantics/SemanticBuilder.cs` (visit OCCURS keys)
- `src/CobolSharp.Compiler/Semantics/Symbols/DataItemSymbol.cs` (store key info)

**Required changes:**
1. Extend the `occursClause` grammar rule to include `{ASCENDING|DESCENDING} KEY IS data-name-list`.
2. Add `AscendingKeys` and `DescendingKeys` properties to `DataItemSymbol` (or the OCCURS metadata).
3. Update `SemanticBuilder` to populate key lists when visiting OCCURS clauses.
4. Update `BoundTreeBuilder.BindSearchAll` to validate that the searched table has ASCENDING/DESCENDING KEY defined.
5. Regenerate ANTLR files.

**Acceptance criteria:**
- NC233A, NC237A, NC238A, NC247A NIST tests pass.
- SEARCH ALL validates against declared keys.
- All existing tests continue to pass.

---

### GRA-03: Allow Reserved Words as Paragraph Names

**Goal:** Allow STATUS and PROGRAM (and other context-sensitive reserved words) to be used as paragraph names.

**Scope:**
- `src/CobolSharp.Compiler/Grammar/CobolParserCore.g4` (paragraph name rule)
- `src/CobolSharp.Compiler/Grammar/CobolLexer.g4` (if lexer modes involved)

**Required changes:**
1. Extend the `paragraphName` rule to accept a broader set of identifiers, including context-sensitive reserved words like STATUS and PROGRAM.
2. Use semantic predicates or an expanded identifier rule that permits these words in paragraph-name position.
3. Regenerate ANTLR files.

**Acceptance criteria:**
- Programs using STATUS or PROGRAM as paragraph names parse and compile correctly.
- No regression in reserved word handling elsewhere.
- All existing tests continue to pass.

---

### GRA-04: Add SORT/MERGE Statement Grammar

**Goal:** Complete the SORT and MERGE statement grammar to support full parsing of these statements.

**Scope:**
- `src/CobolSharp.Compiler/Grammar/CobolParserCore.g4` (SORT/MERGE rules)
- `src/CobolSharp.Compiler/Semantics/SemanticBuilder.cs` (visit SORT/MERGE)

**Required changes:**
1. Verify the SORT statement grammar covers: `SORT file-name ON ASCENDING/DESCENDING KEY data-name... USING file-name... / INPUT PROCEDURE IS section-name THRU section-name, GIVING file-name... / OUTPUT PROCEDURE IS section-name THRU section-name`.
2. Verify the MERGE statement grammar covers analogous syntax.
3. Add `SemanticBuilder` visit methods for SORT/MERGE if missing.
4. Regenerate ANTLR files if grammar changes are needed.

**Acceptance criteria:**
- SORT and MERGE statements parse without errors for valid COBOL-85 programs.
- Parsed information is available for downstream binding.

---

### GRA-05: Add Alternate Record Key Grammar

**Goal:** Parse ALTERNATE RECORD KEY clauses in SELECT statements.

**Scope:**
- `src/CobolSharp.Compiler/Grammar/CobolParserCore.g4` (SELECT clause rules)
- `src/CobolSharp.Compiler/Semantics/SemanticBuilder.cs` (visit alternate keys)
- `src/CobolSharp.Compiler/Semantics/Symbols/FileSymbol.cs` (store alternate keys)

**Required changes:**
1. Add `ALTERNATE RECORD KEY IS data-name [WITH DUPLICATES]` to the SELECT clause grammar.
2. Add `AlternateKeys` property to `FileSymbol` as a list of key descriptors.
3. Update `SemanticBuilder` to populate alternate keys.
4. Regenerate ANTLR files.

**Acceptance criteria:**
- SELECT statements with ALTERNATE RECORD KEY parse correctly.
- Alternate key information is stored in `FileSymbol`.
- All existing tests continue to pass.

---

## Phase 2: Semantic Analysis and Binding (SEM-xx)

### SEM-01: Fix Duplicate Expression Binding

**Goal:** Eliminate the duplicate arithmetic expression binding code in `BoundTreeBuilder.cs`.

**Scope:**
- `src/CobolSharp.Compiler/Semantics/Bound/BoundTreeBuilder.cs` (lines ~2236-2330 and ~2994-3082)

**Required changes:**
1. Identify the two parallel expression binding implementations.
2. Determine which is the canonical version (likely the one with more complete handling).
3. Remove the duplicate, routing all callers through the single canonical method.
4. Verify both call sites produce identical bound trees for test programs.

**Acceptance criteria:**
- Only one expression binding code path exists.
- All 184 integration tests pass. All 217 unit tests pass. All 31 NIST tests pass.

---

### SEM-02: Fix Silent Zero Return for Function Calls

**Goal:** Implement intrinsic function evaluation (e.g., FUNCTION LENGTH) instead of returning literal zero.

**Scope:**
- `src/CobolSharp.Compiler/Semantics/Bound/BoundTreeBuilder.cs` (line ~3067)
- New file or class for intrinsic function binding

**Required changes:**
1. Create a `BoundFunctionCallExpression` node (or use existing one) that carries the function name and arguments.
2. In the expression binding code, recognize intrinsic function calls and produce `BoundFunctionCallExpression` instead of `BoundLiteralExpression(0)`.
3. At minimum, implement FUNCTION LENGTH (returns byte length of argument) and FUNCTION CURRENT-DATE.
4. Add IR instruction `IrIntrinsicCall` and CIL emission for the supported functions.

**Acceptance criteria:**
- `FUNCTION LENGTH(data-item)` returns the correct byte length at runtime.
- No intrinsic function silently returns zero.
- Diagnostic emitted for unrecognized function names.

---

### SEM-03: Fix Silent String Literal Fallback for Unresolved Identifiers

**Goal:** Emit a diagnostic instead of silently converting unresolved identifiers to string literals.

**Scope:**
- `src/CobolSharp.Compiler/Semantics/Bound/BoundTreeBuilder.cs` (line ~3137)

**Required changes:**
1. In the identifier resolution fallback, emit a diagnostic (e.g., `CBL0401` — "Unresolved data reference '{name}'") instead of creating a string literal.
2. Return an error expression node (e.g., `BoundErrorExpression`) to prevent downstream issues.
3. Ensure the error expression is handled gracefully by the binder and emitter (skip emission, don't crash).

**Acceptance criteria:**
- Typos in data names produce a clear diagnostic with source location.
- No unresolved identifier silently becomes a string literal.
- All existing tests pass (no false positives on currently valid programs).

---

### SEM-04: Fix Hardcoded START Key Condition

**Goal:** Use the actual KEY IS comparison operator from the COBOL source instead of always using Equal.

**Scope:**
- `src/CobolSharp.Compiler/CodeGen/Binder.cs` (line ~1308)

**Required changes:**
1. In `LowerStart`, read the key condition from `BoundStartStatement.KeyCondition` to determine the comparison type (EQUAL, GREATER, NOT LESS, etc.).
2. Pass the correct comparison operator to the IR instruction or runtime call.
3. Ensure the runtime's START implementation uses the comparison operator.

**Acceptance criteria:**
- `START file-name KEY IS GREATER THAN data-name` positions the file correctly (not at exact match).
- Integration test added for START with non-equal key conditions.

---

### SEM-05: Wire PROCEDURE DIVISION USING/RETURNING

**Goal:** Make `SemanticBuilder` populate `ProcedureParameter` lists from PROCEDURE DIVISION USING/RETURNING.

**Scope:**
- `src/CobolSharp.Compiler/Semantics/SemanticBuilder.cs` (procedure division header visit)
- `src/CobolSharp.Compiler/Semantics/SemanticModel.cs` (parameter storage)

**Required changes:**
1. Add a visit method for the PROCEDURE DIVISION USING clause in `SemanticBuilder`.
2. For each USING parameter, resolve the data-name against the LINKAGE SECTION and record the parameter mode (BY REFERENCE/CONTENT/VALUE).
3. Store the parameter list in `SemanticModel` (or a new `ProgramParameters` property).
4. Emit diagnostic if a USING parameter is not defined in LINKAGE SECTION.

**Acceptance criteria:**
- PROCEDURE DIVISION USING parameters are resolved and available for CALL validation.
- Diagnostic emitted for invalid parameter references.

---

### SEM-06: Activate Dormant Diagnostic Descriptors

**Goal:** Wire the 24 defined-but-never-emitted diagnostic descriptors to actual validation checks.

**Scope:**
- `src/CobolSharp.Compiler/Diagnostics/DiagnosticDescriptors.cs` (dormant descriptors)
- `src/CobolSharp.Compiler/Semantics/Validation/BoundTreeValidator.cs`
- `src/CobolSharp.Compiler/Semantics/Validation/SymbolValidator.cs`

**Required changes:**
1. For each dormant descriptor (CBL1704, CBL1802-1803, CBL3108-3109, CBL3301-3305, CBL3401-3406, CBL3501-3502):
   - Identify the COBOL-85 rule it should enforce.
   - Add the validation check in the appropriate validator.
   - Add a unit test for each diagnostic (positive and negative case).
2. Remove any descriptors that are truly unnecessary.

**Acceptance criteria:**
- Zero dormant diagnostic descriptors remain.
- Each wired descriptor has at least one unit test triggering it and one test confirming it is not triggered for valid code.

---

## Phase 3: Lowering and IR Design (LOW-xx)

### LOW-01: Fix Subscripted DIVIDE GIVING

**Goal:** Fix the subscripted operand handling in DIVIDE GIVING targets (NC121M failure).

**Scope:**
- `src/CobolSharp.Compiler/CodeGen/Binder.cs` (LowerDivide / LowerArithmetic)
- `src/CobolSharp.Compiler/CodeGen/CilEmitter.cs` (subscript emission for GIVING targets)

**Required changes:**
1. Identify where GIVING targets with subscripts (e.g., `TABLE-ITEM(INDEX)`) use the wrong location resolver.
2. Ensure the subscript expression is evaluated and used to compute the correct storage offset for the GIVING target.
3. Verify the fix works for all arithmetic statements with subscripted GIVING targets (ADD, SUBTRACT, MULTIPLY, DIVIDE, COMPUTE).

**Acceptance criteria:**
- NC121M NIST test passes.
- Subscripted GIVING targets store results at the correct table element.

---

### LOW-02: Fix NC220M Infinite Loop

**Goal:** Diagnose and fix the runtime hang in the NC220M NIST test.

**Scope:**
- `src/CobolSharp.Compiler/CodeGen/Binder.cs` (LowerPerform, PERFORM VARYING logic)
- `src/CobolSharp.Runtime/` (if runtime loop logic is involved)

**Required changes:**
1. Run NC220M under a debugger or with tracing to identify the infinite loop location.
2. Likely cause: TEST BEFORE vs TEST AFTER evaluation order in PERFORM VARYING. Verify the lowered IR matches COBOL-85 semantics for the test position.
3. Fix the loop condition evaluation order in the binder's PERFORM VARYING lowering.

**Acceptance criteria:**
- NC220M NIST test completes without hanging.
- All other PERFORM VARYING tests continue to pass.

---

### LOW-03: Implement WRITE FROM Lowering

**Goal:** Emit the implicit MOVE from the FROM data item to the record area before writing.

**Scope:**
- `src/CobolSharp.Compiler/CodeGen/Binder.cs` (LowerWrite)

**Required changes:**
1. In `LowerWrite`, check if the `BoundWriteStatement` has a FROM expression.
2. If present, emit an `IrMove` from the FROM source to the file's record area before the `IrWrite` instruction.
3. Apply the same pattern for REWRITE FROM in `LowerRewrite`.

**Acceptance criteria:**
- `WRITE record-name FROM data-item` correctly moves data before writing.
- `REWRITE record-name FROM data-item` correctly moves data before rewriting.
- Integration test added for WRITE FROM and REWRITE FROM.

---

### LOW-04: Centralize INVALID KEY / AT END Branching

**Goal:** Extract the repeated INVALID KEY / AT END / ON EXCEPTION branching pattern into a shared helper.

**Scope:**
- `src/CobolSharp.Compiler/CodeGen/Binder.cs` (LowerRead, LowerWrite, LowerDelete, LowerStart, LowerCall, LowerReturn)

**Required changes:**
1. Create a helper method `LowerConditionalBranch(IrBasicBlock block, IrMethod method, IReadOnlyList<BoundStatement> onCondition, IReadOnlyList<BoundStatement> notOnCondition, IrInstruction conditionCheck)` that:
   - Creates the branch/join blocks.
   - Lowers both statement lists.
   - Returns the join block.
2. Replace the 3+ copy-pasted branching patterns with calls to this helper.

**Acceptance criteria:**
- No duplicated branching pattern code in the binder.
- All I/O and CALL tests continue to pass.

---

### LOW-05: Eliminate Duplicate GetPicForLocation

**Goal:** Remove the duplicated `GetPicForLocation` method that exists in both `Binder.cs` and `CilEmitter.cs`.

**Scope:**
- `src/CobolSharp.Compiler/CodeGen/Binder.cs`
- `src/CobolSharp.Compiler/CodeGen/CilEmitter.cs`

**Required changes:**
1. Move `GetPicForLocation` to a shared utility class (e.g., `PicUtilities` or add to existing `PicDescriptor`).
2. Update both `Binder` and `CilEmitter` to call the shared method.
3. Verify both call sites use identical logic — if they differ, reconcile and document why.

**Acceptance criteria:**
- Single implementation of `GetPicForLocation`.
- All tests pass.

---

### LOW-06: Route All Diagnostics Through DiagnosticDescriptor

**Goal:** Eliminate raw string diagnostic codes (e.g., `"COBOL0500"`) and route everything through `DiagnosticDescriptors`.

**Scope:**
- `src/CobolSharp.Compiler/CodeGen/Binder.cs` (search for raw string diagnostic emissions)
- `src/CobolSharp.Compiler/Diagnostics/DiagnosticDescriptors.cs`

**Required changes:**
1. Search for all diagnostic emissions that use raw strings instead of `DiagnosticDescriptor` references.
2. For each, either map to an existing descriptor or create a new one in `DiagnosticDescriptors.cs`.
3. Update the emission site to use the descriptor.

**Acceptance criteria:**
- Zero raw string diagnostic codes in the codebase.
- All diagnostics use `DiagnosticDescriptor` for code, severity, and message template.
- All tests pass.
