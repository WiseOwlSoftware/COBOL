# CobolSharp ANTLR4 Grammar Architecture

> **Status (2026-06-07): LIVE canonical design + implementation reference for the front-end.**
> This is the authoritative grammar/parsing doc. The ANTLR4 front-end is **implemented** —
> the grammar IS the parser (no hand-written recursive-descent drift). M1 (COBOL-85) is complete;
> guard 1196 unit / 509 integration / 364 NIST. OO grammar is factored and done (semantic/emit
> pending); JSON/XML and generics grammar overlays exist but their lowering/runtime are design-only.
> **Stack: .NET 10 / C# 14. Backend is CIL-only via Mono.Cecil (no custom VM, no bytecode interpreter;
> a Roslyn C# backend is a future additive option).**
> Plan SSOT: `docs/MASTER_PLAN.md`. Doctrine: `PROMPT.md`. Companion reference: `docs/GRAMMAR-REFERENCE.md`.
> Consolidated from 2 prior docs (this LIVE doc + the redundant "CobolSharp COBOL Grammar & Parsing
> Architecture" essay), 2026-06-07.

Production-ready reference for the ANTLR4-based front-end of the CobolSharp COBOL-to-.NET compiler.

---

## 1. Pipeline

```
Raw COBOL Source (.cob / .cbl)
        │
        ▼
┌──────────────────────────┐
│ 1. Reference Format      │  ReferenceFormatProcessor
│    Normalization          │  Fixed-form → free-form, column stripping,
│                          │  continuation line joining, comment conversion
└──────────┬───────────────┘
           ▼
┌──────────────────────────┐
│ 2. COPY/REPLACE          │  CopyProcessor
│    Preprocessor           │  Copybook expansion, REPLACING, pseudo-text,
│                          │  nested COPY, REPLACE OFF
└──────────┬───────────────┘
           ▼
┌──────────────────────────┐
│ 3. ANTLR4 Lexer          │  CobolLexer.g4
│                          │  5 modes: DEFAULT, COPYMODE, REPLACEMODE,
│                          │  PSEUDOTEXT, COMMENT_MODE
└──────────┬───────────────┘
           ▼
┌──────────────────────────┐
│ 4. ANTLR4 Parser         │  CobolParserCore.g4 + extension grammars
│                          │  Produces parse tree (CST)
│                          │  Dialect-gated (85/2002/2014/2023)
└──────────┬───────────────┘
           ▼
┌──────────────────────────┐
│ 5. Semantic Analysis     │  Symbol tables, type checking, name resolution,
│                          │  PIC/USAGE validation, PERFORM flow graph,
│                          │  OO method resolution, generics instantiation
└──────────┬───────────────┘
           ▼
┌──────────────────────────┐
│ 6. CIL Code Generation   │  Walks parse tree → .NET assembly via Mono.Cecil
└──────────┬───────────────┘
           ▼
      .NET Assembly (.dll / .exe)
```

---

## 2. Grammar Files

All grammar files are in `src/CobolSharp.Compiler/Grammar/`.

| File | Type | Lines | Purpose |
|------|------|-------|---------|
| `CobolLexer.g4` | Lexer | ~175 | Shared lexer, 5 modes, all tokens |
| `CobolParserCore.g4` | Parser | ~500 | Full procedural core |
| `CobolParserOO.g4` | Parser | ~120 | OO classes, methods, INVOKE |
| `CobolParserGenerics.g4` | Parser | ~80 | TYPEDEF GENERIC, type specifiers |
| `CobolParserJsonXml.g4` | Parser | ~100 | JSON/XML PARSE/GENERATE |
| `CobolDialect.g4` | Parser | ~90 | Dialect overlay system |
| `CobolPreprocessor.g4` | Parser | ~80 | COPY/REPLACE/pseudo-text |

### Import Hierarchy

```
CobolLexer.g4 (standalone — all grammars reference via tokenVocab)

CobolParserCore.g4 (base)
  ├── CobolParserOO.g4 (imports Core)
  ├── CobolParserGenerics.g4 (imports Core)
  ├── CobolParserJsonXml.g4 (imports Core)
  ├── CobolDialect.g4 (imports Core)
  └── CobolPreprocessor.g4 (imports Core)
```

---

## 3. Lexer Modes

| Mode | Trigger | Purpose | Exit |
|------|---------|---------|------|
| `DEFAULT_MODE` | — | Normal COBOL tokens | — |
| `COPYMODE` | `COPY` keyword | Capture copy name, REPLACING clauses | Period (`.`) |
| `REPLACEMODE` | `REPLACE` keyword | Capture replace clauses, OFF | Period (`.`) |
| `PSEUDOTEXT` | `==` in COPY/REPLACE | Capture pseudo-text content | `==` |
| `COMMENT_MODE` | `*>` | Skip comment text to end of line | Newline |

### Token Ordering (critical)

ANTLR4 matches the **first** rule that fits. Order matters:

1. **END-xxx terminators** (e.g., `END-IF`) — before `END`
2. **Hyphenated keywords** (e.g., `PROGRAM-ID`, `WORKING-STORAGE`) — before `IDENTIFIER`
3. **Compound tokens** (e.g., `NEXT SENTENCE`, `BY REFERENCE`) — before component keywords
4. **Single keywords** (e.g., `READ`, `WRITE`, `IF`)
5. **IDENTIFIER** — last among word-like tokens
6. **Literals** — `DECIMALLIT` before `INTEGERLIT` (longer match)
7. **Multi-char operators** — `<=`, `>=`, `<>`, `**` before single-char `<`, `>`, `=`, `*`

### IDENTIFIER Rule

```antlr
IDENTIFIER
    : [A-Za-z0-9] [A-Za-z0-9-]* [A-Za-z0-9]
    | [A-Za-z0-9]
    ;
```

COBOL identifiers: 1-30 chars, letters/digits/hyphens, cannot start or end with hyphen.
Because IDENTIFIER comes **after** all keywords, `READ` matches `READ` (not `IDENTIFIER`), but `READ-FLAG` matches `IDENTIFIER`.

---

## 4. Parser Structure

### Compilation Unit

```
compilationUnit → compilationGroup* EOF
compilationGroup → programUnit+
programUnit → identificationDivision environmentDivision? dataDivision? procedureDivision?
```

### Identification Division

```
identificationDivision → IDENTIFICATION DIVISION . identificationBody
identificationBody → programIdParagraph identificationParagraph*
programIdParagraph → PROGRAM-ID . programName programIdAttributes? .

programIdAttribute:
  INITIAL | COMMON | RECURSIVE | GLOBAL | literal | identifier

identificationParagraph:
  authorParagraph | installationParagraph | dateWrittenParagraph |
  dateCompiledParagraph | securityParagraph | remarksParagraph |
  genericIdentificationParagraph (vendor extensions)
```

### Environment Division

```
environmentDivision → ENVIRONMENT DIVISION .
  configurationSection? inputOutputSection?

configurationSection → identifier SECTION .
  sourceComputerParagraph | objectComputerParagraph |
  specialNamesParagraph | genericConfigurationParagraph

inputOutputSection → identifier SECTION .
  fileControlParagraph? ioControlParagraph?

fileControlEntry → SELECT fileName
  [ASSIGN TO assignTarget]
  [ORGANIZATION IS {SEQUENTIAL|RELATIVE|INDEXED}]
  [ACCESS MODE? IS? {SEQUENTIAL|RANDOM|DYNAMIC}]
  [RECORD KEY IS identifier]
  [ALTERNATE KEY IS identifier [WITH? DUPLICATES]]
  [FILE STATUS IS identifier]
  .
```

### Data Division

```
dataDivision → DATA DIVISION .
  fileSection? workingStorageSection? localStorageSection? linkageSection?

fileSection → FILE SECTION . fileDescriptionEntry*
fileDescriptionEntry → FD fileName fileDescriptionClauses? . dataDescriptionEntry*

workingStorageSection → WORKING-STORAGE SECTION . dataDescriptionEntry*
localStorageSection → LOCAL-STORAGE SECTION . dataDescriptionEntry*
linkageSection → LINKAGE SECTION . linkageEntry*

linkageEntry → dataDescriptionEntry | linkageProcedureParameter
```

### Data Description Entries

```
dataDescriptionEntry → levelNumber dataName? dataDescriptionBody .

dataDescriptionBody:
  dataDescriptionClauses | renamesClause | conditionEntry88

dataDescriptionClause:
  pictureClause       → PIC|PICTURE pictureString
  usageClause         → USAGE IS? {DISPLAY|COMP|COMP-1|COMP-2|COMP-3|BINARY|PACKED-DECIMAL|identifier}
  occursClause        → OCCURS integer [TO integer] [TIMES] [DEPENDING ON id] [INDEXED BY idList]
  redefinesClause     → REDEFINES identifier
  valueClause         → VALUE literal | VALUES literal [, literal]*
  signClause          → SIGN IS? {LEADING|TRAILING} [SEPARATE CHARACTER]
  syncClause          → SYNCHRONIZED | SYNC
  justifiedClause     → JUSTIFIED | JUST RIGHT
  blankWhenZeroClause → BLANK WHEN ZERO
  typeClause          → TYPE IS? {typeName | genericTypeSpecifier}  (COBOL-2023)
  genericDataClause   → identifier [identifier|literal]*  (fallback)

renamesClause → RENAMES identifier [THRU identifier]  (level 66)
conditionEntry88 → 88 conditionName valueSet  (level 88)
valueSet → valueRange [, valueRange]*
valueRange → literal [THRU literal]
```

### Procedure Division

```
procedureDivision → PROCEDURE DIVISION [USING idList] [RETURNING id] .
  declarativePart* procedureSectionOrParagraph*

declarativePart → DECLARATIVES . declarativeSection+ END DECLARATIVES .

sectionDeclaration → sectionName SECTION . paragraphDeclaration*
paragraphDeclaration → paragraphName . statement*
```

---

## 5. Statement Set

### Statement Dispatcher

All statements plug into a single `statement` rule. The full set:

| Category | Statements |
|----------|------------|
| **Arithmetic** | ADD, SUBTRACT, MULTIPLY, DIVIDE, COMPUTE |
| **Data movement** | MOVE (simple + CORRESPONDING) |
| **String** | STRING, UNSTRING |
| **Control flow** | IF/END-IF, PERFORM/END-PERFORM, EVALUATE/END-EVALUATE |
| **File I/O** | READ/END-READ, WRITE/END-WRITE, OPEN, CLOSE, REWRITE/END-REWRITE, DELETE/END-DELETE, START/END-START |
| **Table** | SEARCH/END-SEARCH, SEARCH ALL |
| **Inter-program** | CALL/END-CALL, CANCEL |
| **SET** | TO value, TO TRUE/FALSE, ADDRESS OF, object reference, UP/DOWN BY |
| **Sort/Merge** | SORT/END-SORT, MERGE/END-MERGE, RETURN/END-RETURN, RELEASE |
| **Flow** | CONTINUE, NEXT SENTENCE, EXIT, GOBACK, GO TO, STOP |
| **Other** | ACCEPT, DISPLAY, INITIALIZE, INSPECT |
| **2023** | JSON PARSE/GENERATE, XML PARSE/GENERATE, inline method invocation, DELETE FILE |
| **OO** | INVOKE/END-INVOKE |
| **Fallback** | genericStatement (vendor extensions) |

### Exception/Error Phrases

Many statements share paired exception phrases:

```
[ON SIZE ERROR imperativeStatement]
[NOT ON SIZE ERROR imperativeStatement]

[AT END imperativeStatement]
[NOT AT END imperativeStatement]

[INVALID KEY imperativeStatement]
[NOT INVALID KEY imperativeStatement]

[ON EXCEPTION imperativeStatement]
[NOT ON EXCEPTION imperativeStatement]

[ON OVERFLOW imperativeStatement]
[NOT ON OVERFLOW imperativeStatement]
```

### COBOL-85 Compatibility

In COBOL-85, `END` was a valid standalone imperative statement (no-op).
`READ file RECORD AT END END` uses `END` as both the AT END trigger and the minimal imperative.
COBOL-2023 removed standalone `END` as an imperative.
CobolSharp accepts this via `dialect85Imperative` in `CobolDialect.g4`.

### Imperative Statement

```
imperativeStatement → statement+
```

Used inside all exception/error phrases, WHEN clauses, and conditional branches.

---

## 6. Expression Grammar

### Precedence (highest to lowest)

| Level | Operator | Production |
|-------|----------|------------|
| 1 | NOT | `logicalNotExpression` |
| 2 | AND | `logicalAndExpression` |
| 3 | OR | `logicalOrExpression` |
| 4 | = <> < <= > >= | `relationalExpression` |
| 5 | + - | `additiveExpression` |
| 6 | * / | `multiplicativeExpression` |
| 7 | ** | `powerExpression` |
| 8 | unary + - | `unaryExpression` |
| 9 | literal, identifier, (expr) | `primaryExpression` |

### Condition

```
condition → logicalOrExpression
logicalOrExpression → logicalAndExpression (OR logicalAndExpression)*
logicalAndExpression → logicalNotExpression (AND logicalNotExpression)*
logicalNotExpression → NOT logicalNotExpression | relationalExpression
relationalExpression → arithmeticExpression [relationalOperator arithmeticExpression]
```

### Arithmetic

```
arithmeticExpression → additiveExpression
additiveExpression → multiplicativeExpression ([+|-] multiplicativeExpression)*
multiplicativeExpression → powerExpression ([*|/] powerExpression)*
powerExpression → unaryExpression [** unaryExpression]
unaryExpression → [+|-] unaryExpression | primaryExpression
primaryExpression → literal | identifier | ( arithmeticExpression )
```

---

## 7. OO Layer (COBOL-2002+)

### CLASS-ID

```
classIdParagraph → CLASS-ID . className classAttributes? .

classAttribute:
  FINAL | ABSTRACT | INHERITS className | IMPLEMENTS interfaceNameList
```

### METHOD-ID / END-METHOD

```
methodDeclaration → METHOD-ID . methodName methodAttributes? .
  environmentDivision? dataDivision? procedureDivision
  END-METHOD methodName .

methodAttribute:
  STATIC | FINAL | OVERRIDE | PRIVATE | PROTECTED | PUBLIC |
  genericParameterList  (COBOL-2023)
```

### INVOKE

```
invokeStatement → INVOKE invokeTarget invokeMethod
  [USING invokeArgument+]
  [RETURNING identifier]
  [ON EXCEPTION imperativeStatement [NOT ON EXCEPTION imperativeStatement]]
  [END-INVOKE]

invokeTarget → objectReference | className | interfaceName
invokeMethod → methodName genericInvocationArguments?
invokeArgument:
  BY VALUE arithmeticExpression |
  BY REFERENCE identifier |
  BY CONTENT (identifier | literal) |
  identifier | literal

objectReference → identifier | SELF | SUPER | NULL
```

### Data Divisions

```
classDataDivision → DATA DIVISION . CLASS SECTION . dataDescriptionEntry*
objectDataDivision → DATA DIVISION . OBJECT SECTION . dataDescriptionEntry*
```

---

## 8. Generics (COBOL-2023)

### TYPEDEF GENERIC

```
typeDefinitionEntry → TYPEDEF [GENERIC] [genericParameterList] dataDescriptionEntry

genericParameterList → < genericParameter [, genericParameter]* >
genericParameter → typeParameterName [OF typeName]
```

### Generic Type Specifier

```
genericTypeSpecifier → typeName < typeArgument [, typeArgument]* >
typeArgument → typeName | genericTypeSpecifier   (supports nesting)
```

### Integration Points

- `typeClause` in `dataDescriptionClause`: `TYPE IS? (typeName | genericTypeSpecifier)`
- `genericParameterList` in `methodAttribute`: `METHOD-ID. AddItem<T>.`
- `genericInvocationArguments` in `invokeMethod`: `INVOKE obj::Method<INTEGER>`
- `genericCallTarget` in `callTarget`: `CALL Factory<Customer>::Create`

---

## 9. JSON/XML (COBOL-2014+)

### JSON

```
jsonParseStatement → JSON PARSE jsonSource INTO jsonTarget
  [WITH DETAIL]
  [ON EXCEPTION imperativeStatement [NOT ON EXCEPTION imperativeStatement]]
  [END-JSON]

jsonGenerateStatement → JSON GENERATE jsonOutput FROM jsonInput
  [SUPPRESS SPACES]
  [ON EXCEPTION imperativeStatement [NOT ON EXCEPTION imperativeStatement]]
  [END-JSON]
```

### XML

```
xmlParseStatement → XML PARSE xmlSource
  PROCESSING PROCEDURE IS procedureName
  [ON EXCEPTION imperativeStatement [NOT ON EXCEPTION imperativeStatement]]
  [END-XML]

xmlGenerateStatement → XML GENERATE xmlOutput FROM xmlInput
  [COUNT IN identifier]
  [ON EXCEPTION imperativeStatement [NOT ON EXCEPTION imperativeStatement]]
  [END-XML]
```

---

## 10. Dialect System

### Dialect Enum

```csharp
public enum CobolDialect { Cobol85, Cobol2002, Cobol2014, Cobol2023 }
```

### Predicate Pattern

```csharp
public bool is85()   => _dialect >= CobolDialect.Cobol85;
public bool is2002() => _dialect >= CobolDialect.Cobol2002;
public bool is2014() => _dialect >= CobolDialect.Cobol2014;
public bool is2023() => _dialect >= CobolDialect.Cobol2023;
```

The `>=` pattern means each standard includes all previous standards.

### Feature Gates

| Feature | Gate |
|---------|------|
| Standalone `END` imperative | `is85()` |
| ALTER statement | `is85()` |
| OO classes, methods, INVOKE | `is2002()` |
| JSON/XML | `is2014()` |
| Generics, DELETE FILE, inline methods, END-xxx everywhere | `is2023()` |

The grammar accepts the superset. Dialect validation happens in the semantic phase.

---

## 11. COPY/REPLACE Subsystem

### Pipeline Position

COPY/REPLACE runs **before** the main parser. The preprocessor:

1. Detects COPY directives in the expanded source
2. Resolves copybook file paths (7 extension variants)
3. Expands copybook content recursively (depth limit: 20)
4. Applies REPLACING clauses (pseudo-text or identifier substitution)
5. Processes REPLACE directives (global text replacement)
6. Feeds expanded text to the ANTLR4 lexer

### Grammar

```
copyDirective → COPY copyName [REPLACING replaceClause+] .
replaceDirective → REPLACE replaceClause+ .
replaceOffDirective → REPLACE OFF .

replaceClause → replaceableText BY replacementText
replaceableText → pseudoText | tokenSequence
pseudoText → == pseudoTextBody ==
```

### Lexer Modes

- `COPY` keyword → pushMode(COPYMODE)
- `REPLACE` keyword → pushMode(REPLACEMODE)
- `==` inside COPY/REPLACE → pushMode(PSEUDOTEXT)
- Period (`.`) → popMode back to DEFAULT

---

## 12. Error Recovery

### Strategy

Statement-level synchronization: on parse error, skip to the nearest statement boundary.

```csharp
private void syncToStatementBoundary() {
    while (_input.LA(1) != DOT
        && !_input.LT(1).getText().startsWith("END-")
        && _input.LA(1) != EOF) {
        _input.consume();
    }
}
```

### Error Nodes

```csharp
// In parser options:
contextSuperClass = CobolParserContext;

// In AST builder:
public override void VisitErrorNode(IErrorNode node)
{
    ast.Add(new ErrorNode(node.GetText(), node.Symbol.Line));
}
```

This provides:
- IDE-friendly diagnostics with line/column positions
- LSP integration for real-time error reporting
- Graceful handling of malformed code (parse continues after errors)
- Error nodes in the AST for downstream analysis

---

## 13. END-xxx Paired Terminators

Complete list of explicit scope terminators:

| Terminator | Statement | Required? |
|------------|-----------|-----------|
| END-IF | IF | Optional (period also terminates) |
| END-PERFORM | PERFORM (inline) | Required for inline |
| END-EVALUATE | EVALUATE | Optional |
| END-READ | READ | Optional |
| END-WRITE | WRITE | Optional |
| END-SEARCH | SEARCH | Optional |
| END-CALL | CALL | Optional |
| END-SORT | SORT | Optional |
| END-MERGE | MERGE | Optional |
| END-RETURN | RETURN | Optional |
| END-REWRITE | REWRITE | Optional |
| END-DELETE | DELETE | Optional |
| END-START | START | Optional |
| END-INVOKE | INVOKE | Optional |
| END-JSON | JSON | Optional |
| END-XML | XML | Optional |
| END-METHOD | METHOD | Required |

Without END-xxx, a statement is terminated by:
1. A separator period (terminates ALL open scopes)
2. The next phrase of a containing statement (ELSE, NOT ON SIZE ERROR, etc.)

---

## 14. Key Design Decisions

### Why ANTLR4 (not hand-written recursive descent)

The hand-written parser (Parser.cs, ~4200 lines across 9 partial class files) suffered from
systematic drift between the grammar specification and the implementation. Every fix required
manually checking the grammar reference, and repeated failures to do so were documented in
DEVLOG entries 011, 013, 016, 017, 020, 022, and 023.

With ANTLR4, **the grammar IS the parser**. There is no drift to manage. The grammar files
are the single source of truth, and ANTLR4 generates the parser code automatically.

### Why layered grammars (not monolithic)

COBOL-2023 includes OO, generics, JSON/XML, and other features that most programs don't use.
A monolithic grammar would be ~2000+ lines and difficult to maintain. Layered grammars via
ANTLR4's `import` mechanism allow:
- Each feature family in its own file
- Independent testing of each layer
- Dialect gates that enable/disable features
- Clean separation of concerns

### Why preprocessor before parser (not integrated)

COPY/REPLACE operates on text, not tokens. It cannot be handled by the parser because:
- COPY expands entire files into the source stream
- REPLACE performs global text substitution
- Pseudo-text `== ... ==` can contain arbitrary text including partial tokens
- Nested COPY requires recursive file expansion

The preprocessor runs before the ANTLR4 lexer sees the source.

### Why dialect gates in semantic phase (not parser)

The grammar accepts the superset of all dialects. Dialect-specific validation (e.g., "INVOKE
is not valid in COBOL-85") happens in the semantic phase because:
- It keeps the grammar LL(1) and unambiguous
- It produces better error messages ("INVOKE requires COBOL-2002 or later")
- It avoids grammar duplication for each dialect
- It matches how IBM, Micro Focus, and GnuCOBOL handle dialect selection

---

## 15. Salvaged Design Notes (consolidated from prior "Grammar & Parsing Architecture" essay)

> The following sections preserve unique, still-current framing from the earlier essay-style doc
> that is not already covered above. Where the essay and the implementation disagree, the
> implementation (sections 1–14) wins; stale claims (e.g., "REPORT SECTION ignored", custom-VM
> phrasing) have been corrected to match CURRENT TRUTH.

### 15.1 Grammar file layout — actual on-disk structure (correction)

The grammar lives in `src/CobolSharp.Compiler/Grammar/`. Per owner directive
([[feedback_grammar_version_factoring]]), post-1985 features and large feature families are
factored into `Grammar/Core/*.g4` fragment files imported into the parser core, rather than
inlined into the COBOL-85 base. Current on-disk fragment set (verify against src/ before quoting
in detail):

- **`Grammar/Core/CobolLexer.g4`** — the shared lexer (the live pipeline diagram's
  `CobolLexer.g4`; physically under `Core/`).
- **`Grammar/Core/`** procedural fragments: `CobolData.g4`, `CobolExpressions.g4`,
  `CobolControlFlow.g4`, `CobolIO.g4`, `CobolSpecialNames.g4`, `CobolReportWriter.g4`,
  `CobolScreen.g4`, `CobolOO.g4`, `CobolExtensionsJsonXml.g4`.
- **`Grammar/` root** overlays: `CobolParserCore.g4`, `CobolParserOO.g4`, `CobolParserGenerics.g4`,
  `CobolParserJsonXml.g4`, `CobolDialect.g4`, `CobolPreprocessor.g4`.

Add rules INCREMENTALLY and run the fast guard after each — inlining a whole feature family at once
has regressed NC tests in the past (DEVLOG 438). The factored `Core/CobolOO.g4` is the live OO
grammar; the root `CobolParserOO.g4`/`CobolParserGenerics.g4`/`CobolParserJsonXml.g4`/`CobolDialect.g4`
overlays may be partially or wholly unwired — treat section 2's import-hierarchy table as the
*design intent* and confirm wiring in src/ before relying on it.

### 15.2 Lexer-mode semantics (conceptual)

Beyond the five physical modes in section 3, the front-end conceptually distinguishes:

- **Comment handling** — `*>` inline comments (free-form, anywhere) and fixed-form column-7
  comment indicators (`*`, `/`). Handled via `COMMENT_MODE` / reference-format normalization.
- **String/literal handling** — quoted alphanumeric, national (`N"…"`), and boolean (`B"…"`)
  literals, including doubled-quote escaping. `DECIMALLIT` is ordered before `INTEGERLIT` for
  longest-match (section 3).
- **Directive handling** — compiler directives (`>>IF`, `>>DEFINE`, `>>EVALUATE`, `>>SOURCE
  FORMAT`, `*>` directive forms) processed by the conditional-compilation preprocessor pass
  (no-op on clean source).

### 15.3 Reference-format handling (fixed vs free form)

- **Fixed-form:** columns 1–6 sequence area (ignored), column 7 indicator
  (`*`/`/` comment, `-` continuation, `D` debug), columns 8–72 source area, columns 73–80 ignored.
- **Free-form:** no column restrictions; `*>` comments anywhere; continuation via trailing hyphen.

Reference-format normalization (`ReferenceFormatProcessor`) runs first in the pipeline (section 1),
before COPY/REPLACE and the lexer.

### 15.4 AST construction (custom builder, no CST leakage)

CobolSharp does **not** leak the ANTLR4 parse tree (CST) into downstream phases. A custom AST
builder walks the CST and produces typed AST nodes carrying: node kind, children, source span
(original + expanded, for COPY/REPLACE mapping — section 11), dialect flags, and semantic hints.

AST node-category families include: `ProgramNode`, `ClassNode`, `MethodNode`, `DataItemNode`,
`FileDescriptorNode`, `SectionNode`, `ParagraphNode`, `StatementNode`, `ExpressionNode`,
`ConditionNode`, plus `JsonNode`/`XmlNode` for the 2014+ overlays (design-only lowering).

AST invariants: no null children; optional COBOL constructs represented explicitly (not by absence);
no grammar-/CST-specific artifacts; `ErrorNode`s inserted in place for malformed input (section 12)
so downstream analysis and the LSP can keep going.

### 15.5 CIL-friendly lowering constraints on the AST

The parser/AST is shaped so the CIL backend (CIL-only via Mono.Cecil — **no custom VM**) can lower
it to **structured** .NET control flow:

- **Structured control flow** — IF/EVALUATE/PERFORM lower to structured conditionals/loops/blocks.
- **Unambiguous `PERFORM … THRU` ranges** — paragraph ranges and section boundaries are resolved
  (paragraph-dispatch engine, see [[project_fileio_remaining]] DEVLOG 259–260), not left ambiguous.
- **Expression normalization** — the precedence grammar (section 6) yields a fully-parenthesized,
  precedence-unambiguous expression AST (`NOT > AND > OR`; `**` > `* /` > `+ -`).

### 15.6 Testing strategy

The front-end is exercised by: grammar-fragment and tokenization **unit tests**; **golden tests**
(AST/parse-tree dumps + diagnostics); **fuzzing** (random token streams and random COBOL fragments
for never-crash / never-infinite-loop guarantees, section 12); and **conformance** against
ISO/IEC 1989:2023 grammar plus the NIST corpus (364 baselines in the guard). Every grammar change
is followed by the fast guard (`scripts/guard-fast.sh`).
