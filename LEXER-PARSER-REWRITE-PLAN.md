# Plan: Rewrite Lexer and Parser from ISO Spec Grammar

## Context

The existing lexer and parser were built from AI assumptions, not the ISO/IEC 1989:2023 spec.
NIST test suite exposed 6+ bugs, all stemming from the same root cause: the grammar was never
systematically extracted and followed. We have now extracted the complete grammar into
`docs/GRAMMAR-REFERENCE.md`. This plan rewrites the lexer and parser to match the spec precisely.

**Invariant**: `Ast.cs` must NOT be modified except for minimal additions (new statement types).
All downstream consumers (SemanticAnalyzer, CilEmitter, tests) depend on the existing AST types.

## Files to Modify

| File | Action |
|------|--------|
| `src/CobolSharp.Compiler/Lexing/Lexer.cs` | Rewrite |
| `src/CobolSharp.Compiler/Lexing/TokenKind.cs` | Extend (add missing keywords) |
| `src/CobolSharp.Compiler/Parsing/Parser.cs` | Rewrite |
| `src/CobolSharp.Compiler/Parsing/Ast.cs` | Add EvaluateStatement, MultiplyStatement, DivideStatement, SetStatement (minimal) |
| `src/CobolSharp.Compiler/Semantics/SemanticAnalyzer.cs` | Minor: handle new statement types |
| `src/CobolSharp.Compiler/CodeGen/CilEmitter.cs` | Minor: handle new statement types |
| `tests/CobolSharp.Tests.Unit/Lexing/LexerTests.cs` | Update + add new tests |
| `tests/CobolSharp.Tests.Unit/Parsing/ParserTests.cs` | Update + add new tests |

## Approach: Incremental, Test-at-Each-Step

Not a big-bang rewrite. Fix lexer first (smaller, lower risk), then progressively replace
parser subsystems. Run tests after each step.

---

## Phase 1: Lexer Fixes (~30 min)

### 1.1 Fix Period Handling (spec §8.3.5)
Current: `.` is a period unless followed by a digit.
Spec: `.` followed by space/EOL/EOF is a separator period. `.` followed by a digit is part
of a decimal literal. `.` followed by a letter is NOT a standard separator period.

### 1.2 Add Missing Scope Terminator Keywords
Add to `TokenKind.cs` and `BuildKeywordMap()`:
- END-ADD, END-SUBTRACT, END-MULTIPLY, END-DIVIDE, END-COMPUTE
- END-CALL, END-STRING, END-UNSTRING
- END-ACCEPT, END-DISPLAY, END-SEARCH, END-RETURN, END-REWRITE
- THEN (for IF...THEN)
- GOBACK
- IN, OF (for qualification)

### 1.3 PICTURE String Tokenization
Move PICTURE string handling from parser to lexer:
- After emitting PicKeyword, set `_expectPictureString` flag
- On next `NextToken()`, if flag set, consume optional IS, then read entire PICTURE
  character-string as a single `TokenKind.PictureString` token
- Terminates at separator space not inside parens, period, or EOL

### 1.4 Hex/Boolean/National Literal Support
Handle `X"..."`, `B"..."`, `N"..."`, `Z"..."`, `BX"..."`, `NX"..."` prefixed literals.

### 1.5 Testing Checkpoint
- All existing LexerTests pass
- New tests for period handling, PICTURE tokenization, hex literals, scope terminator keywords

---

## Phase 2: Parser Infrastructure (~1 hr)

### 2.1 Scope Termination Engine (spec §14.5.3)
Replace ad-hoc scope handling with spec-driven architecture:

```csharp
// Track what scopes are open
private readonly Stack<ScopeKind> _scopeStack = new();

// Period terminates ALL open scopes
private void HandlePeriodTermination() { _scopeStack.Clear(); }
```

### 2.2 Core Parsing Methods
Replace `ParseStatements(params TokenKind[] terminators)` with:

```csharp
// Parse one or more imperative statements until a terminating condition
private List<Statement> ParseImperativeStatements(Func<bool> shouldStop)

// Parse a single statement, handling scope push/pop
private Statement? ParseSingleStatement()
```

`shouldStop` returns true when: period encountered, matching END-xxx found,
ELSE/WHEN/other conditional phrase of a containing statement found.

### 2.3 Sentence-Aware Paragraph Parsing
Restructure paragraph parsing to treat periods as first-class events:
- A paragraph contains sentences
- A sentence is statements terminated by a period
- Period clears the scope stack and advances

---

## Phase 3: Expression and Condition Rewrite (~45 min)

### 3.1 Arithmetic Expression Parser (spec §8.8.3)
Keep current precedence structure (it's correct), but ensure it's used consistently
in all contexts (not just COMPUTE).

### 3.2 Condition Expression Parser (spec §8.8.4)
Rewrite with proper hierarchy:
1. OR conditions (lowest precedence)
2. AND conditions
3. NOT conditions
4. Simple conditions:
   - Relation condition: `operand relop operand`
   - Class condition: `identifier IS [NOT] NUMERIC/ALPHABETIC/...`
   - Sign condition: `expression IS [NOT] POSITIVE/NEGATIVE/ZERO`
   - Condition-name: bare identifier (level-88)

### 3.3 Abbreviated Combined Relations (spec §8.8.4.10)
Track last relation subject/operator. When AND/OR is followed by an object
(not a full condition), expand using carried-forward subject and operator:
`A > B AND C` -> `A > B AND A > C`

### 3.4 Relational Operator Parsing
Fix speculative consumption of IS/NOT. Use lookahead before consuming.
Support all forms: `IS GREATER THAN`, `IS NOT =`, `>=`, etc.

---

## Phase 4: Statement Parsers (~1.5 hr)

### 4.1 IF Statement (spec §14.9.19)
- Parse condition, optional THEN
- Push IF scope
- Parse then-statements (stop at ELSE, END-IF, or period)
- Optional ELSE: parse else-statements (stop at END-IF or period)
- END-IF pops scope explicitly; period pops ALL scopes

### 4.2 EVALUATE Statement (spec §14.9.13)
Add `EvaluateStatement` to Ast.cs (minimal addition):
```csharp
public sealed class EvaluateStatement : Statement
{
    public Expression Subject { get; }
    public List<WhenClause> WhenClauses { get; }
    public List<Statement> WhenOtherStatements { get; }
}
public sealed class WhenClause : AstNode
{
    public List<Expression> Objects { get; }
    public List<Statement> Statements { get; }
}
```
Parse EVALUATE subject, WHEN clauses with objects, WHEN OTHER, END-EVALUATE.
SemanticAnalyzer/CilEmitter handle it as if-else chain.

### 4.3 PERFORM Statement (spec §14.9.28)
Fix disambiguation between out-of-line and inline:
- If token after PERFORM (and optional TIMES/UNTIL/VARYING) resolves to a
  procedure-name -> out-of-line (no END-PERFORM)
- Otherwise -> inline (must have END-PERFORM)

Add VARYING support (desugar to MOVE+PERFORM UNTIL+ADD if AST lacks VARYING fields,
or add VaryingIdentifier/From/By fields to PerformStatement).

### 4.4 MULTIPLY Statement (spec §14.9.26)
Add to Ast.cs and parser. Same pattern as ADD/SUBTRACT.

### 4.5 DIVIDE Statement (spec §14.9.12)
Add to Ast.cs and parser. 5 formats including REMAINDER.

### 4.6 SET Statement (spec §14.9.39)
Add to Ast.cs and parser. Index assignment, UP BY/DOWN BY.

### 4.7 SEARCH Statement (spec §14.9.37)
Add to Ast.cs and parser. Serial search with WHEN clauses.

### 4.8 GOBACK Statement (spec §14.9.18)
Simple: `GOBACK` -> equivalent to STOP RUN.

### 4.9 Fix Existing Statement Parsers
Apply consistent scope terminator handling to ALL existing statements:
- ADD: parse ON SIZE ERROR, NOT ON SIZE ERROR, END-ADD
- SUBTRACT: same pattern
- COMPUTE: same pattern
- CALL: parse ON EXCEPTION, NOT ON EXCEPTION, END-CALL
- READ: AT END, NOT AT END, INVALID KEY, NOT INVALID KEY, END-READ
- WRITE: AT END-OF-PAGE, NOT AT END-OF-PAGE, INVALID KEY, NOT INVALID KEY, END-WRITE
- STRING: ON OVERFLOW, NOT ON OVERFLOW, END-STRING
- UNSTRING: ON OVERFLOW, NOT ON OVERFLOW, END-UNSTRING

### 4.10 Qualification (IN/OF)
After consuming an identifier, check for IN/OF qualification chain.
Store qualified name in IdentifierExpression (may need a Qualifiers property or
just use the most-specific name and let semantic analysis resolve).

---

## Phase 5: Testing & Validation (~30 min)

### 5.1 Unit Tests
- All existing tests pass (update expectations where old behavior was wrong)
- New tests for each new feature: scope termination, EVALUATE, conditions, etc.

### 5.2 NIST Regression
```bash
dotnet test --filter "NistCompileTests"
```

### 5.3 End-to-End
```bash
dotnet test tests/CobolSharp.Tests.Integration
```

### 5.4 Demo Programs
Compile and verify DEMO4.cob and other sample programs produce correct output.

---

## Key Architectural Decisions

1. **Scope stack, not ad-hoc terminators**: Central scope tracking replaces per-statement heuristics
2. **Period is king**: A period always clears the entire scope stack, no exceptions
3. **PICTURE in the lexer**: Single token, not parser-assembled from fragments
4. **New AST types for EVALUATE/MULTIPLY/DIVIDE**: Minimal additions to Ast.cs -- necessary because desugaring these to existing types loses too much semantic information for code generation
5. **Qualification consumed but simplified**: IN/OF chains parsed, base name kept, semantic analyzer resolves
6. **Incremental testing**: Tests run after every sub-phase, not just at the end
