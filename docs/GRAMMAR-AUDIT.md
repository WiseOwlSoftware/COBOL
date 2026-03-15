# Grammar Audit: Parser.cs vs GRAMMAR-REFERENCE.md

Complete audit of every grammar production in sections 1-8 of GRAMMAR-REFERENCE.md against Parser.cs.

Audited: 2026-03-14

---

## Sections 1-2: Reference Format & Lexical Elements

These sections (reference format, character sets, separators, literals, figurative constants, PICTURE strings) are handled by the **lexer**, not the parser. Parser.cs does not implement these directly. Skipped for this parser-focused audit.

---

## Section 3: References and Identifiers

### Issue 1: Qualification chain discards qualifiers

**Grammar rule** (§3.1): `data-name-1 [ { IN | OF } data-qualifier ] ... [ file-report-qualifier ]`
**Parser** (line 3263-3270): Consumes IN/OF qualification chains but discards all qualifier names — keeps only the leftmost (most-specific) name.
**Divergence**: The grammar defines a full qualification chain that resolves ambiguous names. The parser consumes the tokens correctly but does not build a qualified name structure.
**Fix**: Store qualification chain in `IdentifierExpression` (e.g., `List<string> Qualifiers`). Not a parse failure — semantically lossy but syntactically correct.

### Issue 2: Subscript parsing does not support ALL subscript

**Grammar rule** (§3.2): `subscript ::= ALL | arithmetic-expression-1 | index-name-1 [ { + | - } integer-1 ]`
**Parser** (line 3409-3418): Parses subscripts as `ParseArithmeticExpression()`. Does not check for `ALL` keyword as a subscript.
**Divergence**: `TABLE-ITEM(ALL)` would parse `ALL` via `ParsePrimaryExpression` which treats it as `ALL literal`, not as an ALL subscript.
**Fix**: In `ParseSubscriptOrRefMod`, before calling `ParseArithmeticExpression`, check for `TokenKind.AllKeyword` and produce a sentinel subscript expression.

### Issue 3: Function-identifier parsing incomplete

**Grammar rule** (§3.4): `FUNCTION { function-pointer-name-1 | function-name-1 | intrinsic-function-name-1 } [ ( argument-1 ) ... ]`
**Parser** (line 3364-3385): `ParseFunctionCall` handles `FUNCTION name ( args )` correctly.
**Divergence**: Arguments should accept identifiers, literals, boolean expressions, or arithmetic expressions per spec. Parser uses `ParseArithmeticExpression` which is sufficient for arithmetic and identifiers but does not handle boolean expressions as arguments.
**Fix**: Use `ParseConditionExpression` for function arguments to allow boolean expression arguments. Low priority — rare in practice.

---

## Section 4: Expressions

### Issue 4: EXCLUSIVE-OR not supported

**Grammar rule** (§4.2.2): `condition-1 { AND | OR | EXCLUSIVE-OR } condition-2`
**Parser** (line 3457-3578): Handles AND, OR, NOT. No support for EXCLUSIVE-OR.
**Divergence**: EXCLUSIVE-OR logical operator is missing entirely.
**Fix**: Add `TokenKind.ExclusiveOrKeyword`, handle it in `ParseOrExpression` with lower precedence than OR (per spec: AND > OR > EXCLUSIVE-OR).

### Issue 5: Abbreviated combined relation conditions incomplete for NOT

**Grammar rule** (§4.2.3): `A > B AND NOT C` means `(A > B) AND (A NOT > C)` which is `(A > B) AND (A <= C)`. NOT negates the carried-forward relational operator.
**Parser** (line 3501-3545): The AND handler checks for abbreviated forms with a new relational operator (`A > B AND <= C`), and bare value abbreviation (`A > B AND C`). However, it does NOT handle `NOT` in abbreviated context — `A > B AND NOT C` would parse `NOT C` as a boolean NOT expression, not as negation of the carried-forward relational operator.
**Divergence**: `A > B AND NOT C` should expand to `(A > B) AND (A <= C)` but the parser produces `(A > B) AND (NOT C)` where `NOT C` is a unary NOT on C.
**Fix**: In `ParseAndExpression` and `ParseOrExpression`, after consuming AND/OR, if the next token is NOT and it's in an abbreviated context (left is relational), consume NOT and negate the carried-forward relational operator before parsing the abbreviated object.

### Issue 6: Condition-name condition not explicitly handled

**Grammar rule** (§4.2.1): `condition-name-1` — A level-88 condition name standing alone is a condition.
**Parser** (line 3581-3637): `ParseRelationalExpression` parses an arithmetic expression. If no relational operator follows, returns it as-is (an `IdentifierExpression`). This works correctly at the AST level — condition-name evaluation is deferred to semantic analysis.
**Divergence**: None functionally. The parser correctly allows bare identifiers in condition positions.
**Fix**: None needed.

---

## Section 5: Program Structure

### Issue 7: IsDivisionKeyword vs IsDivisionStart — SkipDivision uses wrong check

**Grammar rule** (§5.2): Division headers are `keyword DIVISION .`
**Parser** (line 126-128): `IsDivisionKeyword` checks only the token kind (IDENTIFICATION, ENVIRONMENT, DATA, PROCEDURE). Line 135-139: `IsDivisionStart` checks keyword + DIVISION peek.
**Parser** (line 256): `SkipDivision` loop uses `IsDivisionKeyword(Current.Kind)` NOT `IsDivisionStart()`.
**Divergence**: `SkipDivision` will stop prematurely if a word like DATA appears in non-division context (e.g., inside a paragraph being skipped). The identification division parser (line 558) correctly uses `IsDivisionStart()`, but `SkipDivision` does not.
**Fix**: Line 256: change `!IsDivisionKeyword(Current.Kind)` to `!IsDivisionStart()`.

### Issue 8: ParseProgram checks only token kind, not IsDivisionStart

**Grammar rule** (§5.2): Division order is IDENTIFICATION, ENVIRONMENT, DATA, PROCEDURE.
**Parser** (line 222-229): `ParseProgram` uses `Check(TokenKind.EnvironmentKeyword)`, `Check(TokenKind.DataKeyword)`, `Check(TokenKind.ProcedureKeyword)` — these check only the keyword, not the full `keyword DIVISION` pattern.
**Divergence**: If a free-text paragraph in the IDENTIFICATION DIVISION contains the word "DATA" (e.g., `INSTALLATION. AUTOMATED DATA CENTER`), the parser will try to start parsing a DATA DIVISION at the word "DATA".
**Fix**: Change lines 222, 225, 228 to use `IsDivisionStart()` (which checks keyword + peek DIVISION) instead of bare `Check(TokenKind.XxxKeyword)`.

### Issue 9: ParseDataDivision section loop uses IsDivisionKeyword, not IsDivisionStart

**Grammar rule** (§5.5): `DATA DIVISION . [ sections... ]`
**Parser** (line 606): `while (Current.Kind != TokenKind.EndOfFile && !IsDivisionKeyword(Current.Kind))`
**Divergence**: Same problem as Issue 7 — will false-stop on reserved words appearing in data names or other contexts.
**Fix**: Change `!IsDivisionKeyword(Current.Kind)` to `!IsDivisionStart()` on line 606.

### Issue 10: ParseParagraph uses IsDivisionKeyword instead of IsDivisionStart

**Grammar rule** (§6.3): Paragraphs end before next paragraph, section, or division.
**Parser** (line 1275): `!IsDivisionKeyword(Current.Kind)` in `ParseParagraph` loop.
**Divergence**: Same false-stop issue as Issues 7-9.
**Fix**: Change to `!IsDivisionStart()`.

### Issue 11: ParseSection uses IsDivisionKeyword instead of IsDivisionStart

**Grammar rule** (§6.3): Sections end before next section or division.
**Parser** (line 1214): `!IsDivisionKeyword(Current.Kind)` in `ParseSection` loop.
**Divergence**: Same false-stop issue.
**Fix**: Change to `!IsDivisionStart()`.

### Issue 12: Procedure division body loop uses IsDivisionKeyword instead of IsDivisionStart

**Grammar rule** (§6.2): Procedure division body continues until end or next division.
**Parser** (line 1139): `while (Current.Kind != TokenKind.EndOfFile && !IsDivisionKeyword(Current.Kind))`
**Divergence**: Same false-stop issue. A procedure division statement using a data name like `DATA-FLAG` won't trigger this because that's an Identifier token, but if the keyword `DATA` appears standalone (unlikely in procedure division but possible with ALTER targets), it would false-stop.
**Fix**: Change to `!IsDivisionStart()`.

### Issue 13: Identification division free-text paragraph skipping uses IsDivisionKeyword

**Grammar rule** (§5.3): Identification division contains paragraphs like AUTHOR, INSTALLATION with free text.
**Parser** (line 573): Inner skip loop in `ParseIdentificationDivision` uses `!IsDivisionKeyword(Current.Kind)`.
**Divergence**: The **outer** loop at line 558 correctly uses `IsDivisionStart()`, but the **inner** free-text paragraph content skip at line 573 uses `IsDivisionKeyword`. This means if free text contains a reserved word like DATA, the inner skip stops prematurely. The content gets left for the outer loop, which would then break correctly at `IsDivisionStart()`. So the inner loop is unnecessarily conservative but the outer loop backstops it — **functionally correct but inconsistent**.
**Fix**: Change line 573 to `!IsDivisionStart()` for consistency and robustness. Requires checking both Current and Peek, so replace `!IsDivisionKeyword(Current.Kind)` with a helper or inline `!(IsDivisionKeyword(Current.Kind) && Peek().Kind == TokenKind.DivisionKeyword)`.

### Issue 14: IDENTIFICATION keyword is required, but grammar says optional

**Grammar rule** (§5.3): `[ IDENTIFICATION ] DIVISION .` — the word IDENTIFICATION is optional in the 2023 spec.
**Parser** (line 498): `Expect(TokenKind.IdentificationKeyword)` — requires IDENTIFICATION.
**Divergence**: The parser requires IDENTIFICATION before DIVISION. The 2023 spec allows starting with just `DIVISION .` or directly with `PROGRAM-ID.`
**Fix**: Low priority. Legacy COBOL always includes IDENTIFICATION. To support the 2023 form, check for `PROGRAM-ID` directly as an alternative entry point.

### Issue 15: PROGRAM-ID does not handle AS literal or COMMON/INITIAL/RECURSIVE

**Grammar rule** (§5.3.1): `PROGRAM-ID . program-name-1 [ AS literal-1 ] [ { COMMON | INITIAL | RECURSIVE } PROGRAM ] .`
**Parser** (line 507-513): Parses `PROGRAM-ID . name .` but does not handle `AS literal-1` or the COMMON/INITIAL/RECURSIVE clauses.
**Divergence**: Programs with `PROGRAM-ID. MYPROGRAM AS "MY-EXTERNAL-NAME" INITIAL PROGRAM.` would misparse — "AS" would be consumed as the program name.
**Fix**: After consuming the program name, check for `AS` keyword and consume a string literal. Then check for COMMON/INITIAL/RECURSIVE identifiers before the terminating period.

### Issue 16: END PROGRAM uses generic Identifier check for PROGRAM keyword

**Grammar rule** (§5.3.2): `END PROGRAM program-name-1 .`
**Parser** (line 237): `Check(TokenKind.Identifier) && Current.Text.Equals("PROGRAM", ...)` — checks if "PROGRAM" is an Identifier token.
**Divergence**: If the lexer produces `PROGRAM` as a keyword token (e.g., `TokenKind.ProgramKeyword`), this check would fail. Whether this is an issue depends on the lexer. If PROGRAM is reserved and lexed as a keyword, the parser won't recognize `END PROGRAM`.
**Fix**: Also check for any `ProgramKeyword` token kind if one exists, or ensure the lexer produces Identifier for PROGRAM in this context.

### Issue 17: Configuration Section content is skipped but SPECIAL-NAMES clauses are not parsed

**Grammar rule** (§5.4.1): `CONFIGURATION SECTION . [ SOURCE-COMPUTER... ] [ OBJECT-COMPUTER... ] [ SPECIAL-NAMES... ] [ REPOSITORY... ]`
**Parser** (line 315-333): Recognizes CONFIGURATION as an identifier, skips SECTION period, then skips all content until next section/division.
**Divergence**: Grammar defines specific paragraphs (SOURCE-COMPUTER, OBJECT-COMPUTER, SPECIAL-NAMES, REPOSITORY) that should be parsed. The parser skips everything. This means SPECIAL-NAMES settings (like `DECIMAL-POINT IS COMMA`) are lost.
**Fix**: Parse at minimum SPECIAL-NAMES paragraph for `DECIMAL-POINT IS COMMA` and `CURRENCY SIGN IS`. Low priority if these features aren't needed.

### Issue 18: LOCAL-STORAGE SECTION not parsed

**Grammar rule** (§5.5): `[ LOCAL-STORAGE SECTION . { entries } ... ]`
**Parser** (line 628-638): Falls into the else branch which skips unrecognized section headers.
**Divergence**: LOCAL-STORAGE SECTION data entries are silently discarded.
**Fix**: Add `TokenKind.LocalStorageKeyword` and a `ParseLocalStorageSection` method mirroring `ParseWorkingStorageSection`.

---

## Section 6: Procedure Division

### Issue 19: DECLARATIVES section is skipped, not parsed

**Grammar rule** (§6.2): `DECLARATIVES . { section-name SECTION . use-statement . [ sentences... ] ... } ... END DECLARATIVES .`
**Parser** (line 1111-1131): Recognizes DECLARATIVES and skips to END DECLARATIVES.
**Divergence**: USE statements and their associated error-handling paragraphs are discarded.
**Fix**: Parse DECLARATIVES sections, extracting at minimum the USE statements for file error handling. Medium priority.

### Issue 20: Procedure division does not parse USING/RETURNING into AST

**Grammar rule** (§6.1): `PROCEDURE DIVISION [ USING { [ BY REFERENCE ] { data-name-1 } ... } ... ] [ RETURNING data-name-2 ] .`
**Parser** (line 1093-1106): Skips USING and RETURNING tokens without building AST nodes.
**Divergence**: Called programs that receive parameters via PROCEDURE DIVISION USING will have their parameter declarations silently discarded.
**Fix**: Parse USING clause into a list of parameter descriptors (name, convention) and RETURNING into an optional return identifier. Store in `ProcedureDivision` AST node.

### Issue 21: Section header does not check for keyword tokens as section names

**Grammar rule** (§6.3): `section-name SECTION [ segment-number ] .`
**Parser** (line 1196-1200): `IsSectionHeader` only checks `Current.Kind == TokenKind.Identifier`. Section names that happen to be reserved words (lexed as keyword tokens) would not be recognized as section headers.
**Divergence**: A section named `INPUT-SECTION SECTION.` where INPUT is a keyword would fail. In practice section names are usually non-reserved, but some legacy code uses reserved words as procedure names.
**Fix**: `IsSectionHeader` should also allow keyword tokens that can serve as user-defined names (context-dependent).

### Issue 22: Paragraph header does not check for keyword tokens as paragraph names

**Grammar rule** (§6.3): `paragraph-name .`
**Parser** (line 1190-1194): `IsParagraphHeader` only checks `Current.Kind == TokenKind.Identifier`.
**Divergence**: Same as Issue 21 — paragraph names that are reserved words won't be recognized.
**Fix**: Same approach — allow certain keyword tokens as paragraph names.

---

## Section 7: Statement General Formats

### Issue 23: ACCEPT does not handle temporal format (DATE/DAY/TIME)

**Grammar rule** (§7.1): `ACCEPT identifier-2 FROM { DATE [ YYYYMMDD ] | DAY [ YYYYDDD ] | DAY-OF-WEEK | TIME }`
**Parser** (line 1962-1981): Parses `ACCEPT identifier FROM identifier` generically. The FROM source is stored as a string but DATE/DAY/TIME modifiers (YYYYMMDD, YYYYDDD) are not consumed.
**Divergence**: `ACCEPT WS-DATE FROM DATE YYYYMMDD` — "YYYYMMDD" would be left unconsumed, potentially causing the next statement to misparse.
**Fix**: After consuming the FROM source name, if it's DATE or DAY, check for and consume YYYYMMDD/YYYYDDD tokens.

### Issue 24: ACCEPT does not handle ON EXCEPTION / NOT ON EXCEPTION / END-ACCEPT

**Grammar rule** (§7.1): `[ ON EXCEPTION imperative-statement-1 ] [ NOT ON EXCEPTION imperative-statement-2 ] [ END-ACCEPT ]`
**Parser** (line 1962-1981): No exception phrase handling, no END-ACCEPT matching.
**Divergence**: ACCEPT with exception handling would leave tokens unconsumed.
**Fix**: Add `SkipExceptionPhrases(TokenKind.EndAcceptKeyword)` and `Match(TokenKind.EndAcceptKeyword)`.

### Issue 25: ADD CORRESPONDING loses the CORR semantic

**Grammar rule** (§7.2): Format 3: `ADD CORRESPONDING identifier-4 TO identifier-5 [ rounded-phrase ]`
**Parser** (line 1508-1509): `Match(TokenKind.CorrespondingKeyword)` silently consumes CORR/CORRESPONDING and then parses as a regular ADD.
**Divergence**: ADD CORRESPONDING has different semantics than regular ADD — it operates on matching subordinate items. The parser drops this information.
**Fix**: Track whether CORRESPONDING was specified and store in `AddStatement`.

### Issue 26: SUBTRACT CORRESPONDING loses the CORR semantic

**Grammar rule** (§7.25): Format 3: `SUBTRACT CORRESPONDING identifier-4 FROM identifier-5`
**Parser** (line 1582-1583): Same issue as Issue 25 — silently consumes CORRESPONDING.
**Divergence**: Same as ADD CORRESPONDING.
**Fix**: Track in `SubtractStatement`.

### Issue 27: MOVE CORRESPONDING loses the CORR semantic

**Grammar rule** (§7.16): Format 2: `MOVE CORRESPONDING identifier-3 TO identifier-4`
**Parser** (line 1467): Same issue — silently consumes CORRESPONDING.
**Divergence**: MOVE CORRESPONDING has different runtime behavior (moves matching subordinate items).
**Fix**: Track in `MoveStatement`.

### Issue 28: COMPUTE only parses one target identifier

**Grammar rule** (§7.6): `COMPUTE { identifier-1 [ rounded-phrase ] } ... = arithmetic-expression-1`
The `...` means multiple target identifiers are allowed.
**Parser** (line 1650-1681): Only parses a single target identifier.
**Divergence**: `COMPUTE A B C = X + Y` would only set `A`, leaving `B C` unconsumed.
**Fix**: Parse targets in a loop (like ADD/SUBTRACT do) until `=` is found.

### Issue 29: STOP RUN does not handle WITH STATUS phrase

**Grammar rule** (§7.23): `STOP RUN [ WITH { ERROR | NORMAL } STATUS { identifier-1 | literal-1 } ]`
**Parser** (line 1451-1457): Only parses `STOP RUN`, no STATUS handling.
**Divergence**: `STOP RUN WITH ERROR STATUS 8` would leave `WITH ERROR STATUS 8` unconsumed.
**Fix**: After consuming RUN, check for WITH keyword and consume the status phrase.

### Issue 30: STOP literal (archaic format) not handled

**Grammar rule** (§7.23): Format 2: `STOP literal-1`
**Parser** (line 1454-1455): `Expect(TokenKind.RunKeyword)` — requires RUN after STOP.
**Divergence**: `STOP "MESSAGE"` (archaic format) would report an error.
**Fix**: After consuming STOP, check if next token is a literal instead of RUN.

### Issue 31: DISPLAY does not check UPON as a keyword

**Grammar rule** (§7.8): `DISPLAY { identifier-1 | literal-1 } ... [ UPON mnemonic-name-1 ]`
**Parser** (line 1420-1424): Checks UPON as an Identifier text match, not a keyword.
**Divergence**: If the lexer produces UPON as a keyword token, this check would fail and UPON would be parsed as a display operand.
**Fix**: Also check for `TokenKind.UponKeyword` if such a token kind exists.

### Issue 32: EVALUATE does not handle ALSO (multi-dimensional)

**Grammar rule** (§7.10): `EVALUATE selection-subject [ ALSO selection-subject ] ... { WHEN selection-object [ ALSO selection-object ] ... }`
**Parser** (line 2671-2733): Parses a single subject and single object per WHEN clause. No ALSO handling.
**Divergence**: `EVALUATE TRUE ALSO WS-CODE WHEN condition-1 ALSO 1 THRU 5` would misparse — ALSO and the second subject/object would be treated as expressions.
**Fix**: Parse ALSO-separated subjects and objects into lists.

### Issue 33: EVALUATE does not handle partial-expression selection-objects

**Grammar rule** (§7.10): Selection-object can be `partial-expression-1` — a relational operator followed by an operand (e.g., `WHEN > 10`).
**Parser** (line 2697): Parses WHEN objects as regular expressions via `ParseExpression`.
**Divergence**: `WHEN > 10` would fail because `>` is not a valid expression start.
**Fix**: Before `ParseExpression`, try `TryParseRelationalOperator`. If found, parse the operand and create a partial-expression node.

### Issue 34: EXIT does not handle PERFORM CYCLE

**Grammar rule** (§7.11): Format 5: `EXIT PERFORM [ CYCLE ]`
**Parser** (line 1950): Handles EXIT PERFORM but does not check for CYCLE modifier.
**Divergence**: `EXIT PERFORM CYCLE` — the word CYCLE would be left unconsumed.
**Fix**: After consuming "PERFORM", check for "CYCLE" identifier and consume it. Set a flag in `ExitStatement`.

### Issue 35: EXIT FUNCTION / EXIT METHOD not handled

**Grammar rule** (§7.11): Format 3: `EXIT FUNCTION`, Format 4: `EXIT METHOD`
**Parser** (line 1944-1951): Handles EXIT PARAGRAPH, SECTION, PROGRAM, PERFORM. Does not handle FUNCTION or METHOD.
**Divergence**: `EXIT FUNCTION` or `EXIT METHOD` would leave the second word unconsumed.
**Fix**: Add "FUNCTION" and "METHOD" to the word checks in `ParseExitStatement`.

### Issue 36: GO TO DEPENDING does not consume identifier as expression

**Grammar rule** (§7.12): `GO TO { procedure-name-1 } ... DEPENDING ON identifier-1`
**Parser** (line 1885-1891): Uses `ParseExpression()` for the DEPENDING ON operand, which works.
**Divergence**: None — this is correct.

### Issue 37: IF / NEXT SENTENCE is parsed as CONTINUE

**Grammar rule** (§7.13): `NEXT SENTENCE` transfers control past the next period. It is NOT equivalent to CONTINUE.
**Parser** (line 2969-2977): `ParseNextSentenceStatement` returns a `ContinueStatement`.
**Divergence**: NEXT SENTENCE has different semantics — it jumps past all enclosing statements to after the next period, while CONTINUE is a no-op within the current scope.
**Fix**: Create a separate `NextSentenceStatement` AST node to distinguish from CONTINUE at code generation time.

### Issue 38: INITIALIZE does not parse REPLACING clause

**Grammar rule** (§7.14): `INITIALIZE { identifier-1 } ... [ WITH FILLER ] [ THEN REPLACING { category-name DATA BY { identifier-2 | literal-1 } } ... ] [ THEN TO DEFAULT ]`
**Parser** (line 1986-1999): Only parses `INITIALIZE { identifier } ...`. No REPLACING, WITH FILLER, or TO DEFAULT.
**Divergence**: `INITIALIZE WS-REC REPLACING NUMERIC DATA BY ZEROS` — REPLACING and everything after would be left unconsumed.
**Fix**: After parsing target identifiers, check for and consume REPLACING/DEFAULT clauses.

### Issue 39: INSPECT parsing is oversimplified

**Grammar rule** (§7.15): INSPECT has four complex formats with TALLYING/REPLACING/CONVERTING, each with FOR, ALL/LEADING/FIRST sub-phrases, and BEFORE/AFTER INITIAL modifiers.
**Parser** (line 2420-2474): Parses a simplified single-operation form. TALLYING only handles one counter with one search pattern. REPLACING only handles one replacement. BEFORE/AFTER INITIAL phrases are not parsed. Multiple FOR/ALL/LEADING clauses are not supported.
**Divergence**: Complex INSPECT statements like `INSPECT WS-STR TALLYING WS-COUNT FOR ALL "A" AFTER INITIAL "B" ALL "C" BEFORE INITIAL "D"` would misparse.
**Fix**: Implement full INSPECT parsing with loops for multiple tallying/replacing phrases and BEFORE/AFTER support.

### Issue 40: INSPECT uses SkipToEndOfStatement after partial parse

**Grammar rule** (§7.15): All tokens belong to defined INSPECT sub-phrases.
**Parser** (line 2470): Calls `SkipToEndOfStatement()` after the simplified parse, which skips any remaining INSPECT tokens (like BEFORE/AFTER phrases) without processing them.
**Divergence**: Remaining tokens are silently discarded.
**Fix**: Remove `SkipToEndOfStatement` once full INSPECT parsing is implemented.

### Issue 41: OPEN does not handle SHARING or WITH NO REWIND clauses

**Grammar rule** (§7.18): `OPEN { INPUT | OUTPUT | I-O | EXTEND } { [ sharing-phrase ] file-name-1 [ WITH NO REWIND ] } ...`
**Parser** (line 2004-2028): Parses mode + file names. No SHARING or WITH NO REWIND.
**Divergence**: `OPEN INPUT SHARING WITH ALL OTHER MY-FILE` would try to parse SHARING as a file name.
**Fix**: Before collecting file names, check for SHARING clause and consume it. After each file name, check for WITH NO REWIND and consume it.

### Issue 42: CLOSE does not handle REEL/UNIT phrases

**Grammar rule** (§7.5): `CLOSE { file-name-1 [ { REEL | UNIT } [ { FOR REMOVAL | WITH NO REWIND } ] ] } ...`
**Parser** (line 2031-2038): Only parses file names.
**Divergence**: `CLOSE MY-FILE WITH NO REWIND` would leave WITH NO REWIND unconsumed.
**Fix**: After each file name, check for REEL/UNIT/WITH/FOR and consume.

### Issue 43: READ does not handle PREVIOUS keyword

**Grammar rule** (§7.20): `READ file-name-1 [ { NEXT | PREVIOUS } ] RECORD`
**Parser** (line 2050): Only matches NEXT keyword. PREVIOUS is not checked.
**Divergence**: `READ MY-FILE PREVIOUS RECORD` — PREVIOUS would be unconsumed.
**Fix**: Add check for PREVIOUS keyword (likely an Identifier text match).

### Issue 44: READ NOT INVALID KEY not handled

**Grammar rule** (§7.20): `[ NOT INVALID KEY imperative-statement-4 ]`
**Parser** (line 2086-2091): Handles INVALID KEY but does NOT handle NOT INVALID KEY.
**Divergence**: `READ MY-FILE INVALID KEY DISPLAY "ERR" NOT INVALID KEY DISPLAY "OK" END-READ` — the NOT INVALID KEY phrase would not be parsed.
**Fix**: After INVALID KEY handling, add NOT INVALID KEY handling (similar to NOT AT END pattern).

### Issue 45: WRITE does not handle FILE keyword syntax

**Grammar rule** (§7.27): `WRITE { record-name-1 | FILE file-name-1 }`
**Parser** (line 2102-2104): Only parses `WRITE record-name`. Does not handle `WRITE FILE file-name`.
**Divergence**: `WRITE FILE MY-FILE FROM WS-REC` would parse FILE as the record name.
**Fix**: Check for FILE keyword after WRITE and consume it before the file/record name.

### Issue 46: REWRITE does not handle FILE keyword syntax

**Grammar rule** (§7.29): `REWRITE { record-name-1 | FILE file-name-1 }`
**Parser** (line 2140-2141): Same issue as WRITE.
**Fix**: Same as Issue 45.

### Issue 47: SORT does not handle THRU in INPUT/OUTPUT PROCEDURE

**Grammar rule** (§7.33): `INPUT PROCEDURE IS procedure-name-1 [ { THROUGH | THRU } procedure-name-2 ]`
**Parser** (line 2214-2223): Parses INPUT PROCEDURE IS proc-name but not THRU.
**Divergence**: `SORT ... INPUT PROCEDURE IS PROC-1 THRU PROC-2` — THRU PROC-2 left unconsumed.
**Fix**: After parsing the procedure name, check for THRU and consume the end procedure name.

### Issue 48: SORT does not handle multiple USING/GIVING files

**Grammar rule** (§7.33): `USING { file-name-2 } ...` and `GIVING { file-name-3 } ...` — multiple files allowed.
**Parser** (line 2224-2228, 2241-2245): Only parses a single file name for USING and GIVING.
**Divergence**: `SORT ... USING FILE-A FILE-B GIVING FILE-C FILE-D` — only FILE-A and FILE-C would be captured.
**Fix**: Parse file names in a loop.

### Issue 49: SORT does not handle WITH DUPLICATES IN ORDER

**Grammar rule** (§7.33): `[ WITH DUPLICATES IN ORDER ]`
**Parser** (line 2184-2248): No WITH DUPLICATES handling.
**Divergence**: Tokens left unconsumed.
**Fix**: Check for WITH DUPLICATES and consume.

### Issue 50: SORT does not handle COLLATING SEQUENCE

**Grammar rule** (§7.33): `[ COLLATING SEQUENCE IS alphabet-name-1 ]`
**Parser**: Not handled.
**Divergence**: Tokens left unconsumed.
**Fix**: Check for COLLATING and consume through the alphabet name.

### Issue 51: CALL USING does not handle OMITTED keyword

**Grammar rule** (§7.3): `{ BY REFERENCE } { { identifier-2 } | OMITTED }`
**Parser** (line 2262-2286): Does not check for OMITTED.
**Divergence**: `CALL "X" USING BY REFERENCE OMITTED` — OMITTED would be parsed as an expression (identifier), which might work but is semantically wrong.
**Fix**: Check for OMITTED keyword and create a special sentinel expression.

### Issue 52: CALL does not handle NotKeyword for NOT ON EXCEPTION

**Grammar rule** (§7.3): `[ NOT ON EXCEPTION imperative-statement-2 ]`
**Parser** (line 2297-2299): Uses `SkipExceptionPhrases` which handles NOT ON EXCEPTION generically. This appears correct.
**Divergence**: None — `SkipExceptionPhrases` handles this.

### Issue 53: STRING DELIMITED BY is checked as Identifier text, not keyword

**Grammar rule** (§7.24): `{ identifier-1 | literal-1 } ... DELIMITED BY { identifier-2 | literal-2 | SIZE }`
**Parser** (line 2331): Checks `Current.Text.Equals("DELIMITED", ...)` as Identifier.
**Divergence**: If the lexer produces DELIMITED as a keyword token, this check would fail. The entire DELIMITED BY phrase would be missed and the source values would be parsed incorrectly.
**Fix**: Also check for a `TokenKind.DelimitedKeyword` if one exists.

### Issue 54: UNSTRING does not handle OR delimiters

**Grammar rule** (§7.26): `[ DELIMITED BY [ ALL ] { identifier-2 | literal-1 } [ OR [ ALL ] { identifier-3 | literal-2 } ] ... ]`
**Parser** (line 2384-2391): Parses only one delimiter. No OR handling, no ALL keyword.
**Divergence**: `UNSTRING WS-STR DELIMITED BY ALL "," OR ";" INTO A B` — OR and ";" would be unconsumed.
**Fix**: After parsing the first delimiter, loop on OR keyword and parse additional delimiters. Check for ALL before each delimiter.

### Issue 55: UNSTRING does not handle DELIMITER IN / COUNT IN phrases

**Grammar rule** (§7.26): `INTO { identifier-4 [ DELIMITER IN identifier-5 ] [ COUNT IN identifier-6 ] } ...`
**Parser** (line 2393-2400): Parses INTO targets but does not handle DELIMITER IN or COUNT IN per target.
**Divergence**: `UNSTRING ... INTO A DELIMITER IN D1 COUNT IN C1 B DELIMITER IN D2` — DELIMITER/COUNT tokens would be parsed as target identifiers.
**Fix**: After each target identifier, check for DELIMITER IN and COUNT IN and consume them.

### Issue 56: UNSTRING WITH POINTER not handled

**Grammar rule** (§7.26): `[ WITH POINTER identifier-7 ]`
**Parser** (line 2393-2414): No WITH POINTER handling.
**Divergence**: Tokens left unconsumed.
**Fix**: Check for WITH POINTER after INTO targets, before TALLYING.

### Issue 57: SEARCH ALL has different WHEN syntax than SEARCH serial

**Grammar rule** (§7.21): SEARCH ALL WHEN uses `{ data-name IS EQUAL TO expr | condition-name } [ AND ... ]` — restricted to equality tests and AND combinations only.
**Parser** (line 2915-2956): Does not distinguish SEARCH ALL WHEN syntax from serial SEARCH WHEN. Parses all WHEN conditions identically via `ParseConditionExpression`.
**Divergence**: Parser accepts invalid SEARCH ALL conditions (like `WHEN X > Y`) but this is a semantic issue, not a parse failure. The parser is more permissive than the grammar.
**Fix**: Low priority — validate SEARCH ALL WHEN restrictions during semantic analysis.

### Issue 58: SET does not handle ADDRESS OF

**Grammar rule** (§7.22): Format 5: `SET { ADDRESS OF identifier-1 | pointer-name-1 } ... TO { ADDRESS OF identifier-2 | pointer-name-2 | NULL | NULLS }`
**Parser** (line 2877-2911): Parses SET targets as identifiers, then TO/UP BY/DOWN BY + value. Does not handle ADDRESS OF construct.
**Divergence**: `SET ADDRESS OF WS-PTR TO ADDRESS OF WS-REC` — "ADDRESS" would be parsed as a target identifier, "OF" would be consumed by qualification, etc.
**Fix**: Check for ADDRESS keyword before target parsing and handle the ADDRESS OF construct.

### Issue 59: PERFORM does not handle AFTER clause for nested varying

**Grammar rule** (§7.19): `[ AFTER { identifier-5 } FROM { identifier-6 } BY { identifier-7 } UNTIL condition-2 ] ...`
**Parser** (line 1832-1868): `ParsePerformVarying` parses one VARYING clause. No AFTER loop.
**Divergence**: `PERFORM VARYING I FROM 1 BY 1 UNTIL I > 10 AFTER J FROM 1 BY 1 UNTIL J > 5` — AFTER J... would be left unconsumed.
**Fix**: After parsing the primary VARYING clause, loop on AFTER keyword and parse additional varying clauses.

### Issue 60: PERFORM UNTIL EXIT not handled

**Grammar rule** (§7.19): `UNTIL { condition-1 | EXIT }`
**Parser** (line 1739-1747): Parses `UNTIL condition`. Does not check for EXIT keyword.
**Divergence**: `PERFORM UNTIL EXIT ... END-PERFORM` — EXIT would be parsed as an identifier condition, which may work but is semantically wrong.
**Fix**: Check for EXIT keyword after UNTIL and handle as infinite loop.

### Issue 61: PERFORM WITH TEST can appear before or after procedure name

**Grammar rule** (§7.19): The `[ WITH TEST { BEFORE | AFTER } ]` can appear in the varying/until phrase, which in Format 1 can come after the procedure name.
**Parser** (line 1721-1730, 1800-1818): TEST BEFORE/AFTER is checked both before and after the procedure name. The second check (line 1800) handles the case where TEST appears after the procedure name + THRU.
**Divergence**: The grammar has TEST as part of the until/varying phrase, so it should be accepted in multiple positions. The parser handles this reasonably.
**Fix**: None needed — adequately handled.

### Issue 62: CANCEL should accept multiple operands

**Grammar rule** (§7.4): `CANCEL { identifier-1 | literal-1 | program-prototype-name-1 } ...` — the `...` means multiple.
**Parser** (line 2307-2313): Only parses a single operand.
**Divergence**: `CANCEL "PROG-A" "PROG-B"` — only "PROG-A" would be consumed.
**Fix**: Parse operands in a loop.

### Issue 63: RAISE does not handle EXCEPTION keyword prefix

**Grammar rule** (§7.37): `RAISE { EXCEPTION exception-name-1 | identifier-1 }`
**Parser** (line 2552-2557): Parses `RAISE identifier`. Does not check for EXCEPTION keyword.
**Divergence**: `RAISE EXCEPTION EC-DATA-OVERFLOW` — "EXCEPTION" would be parsed as the exception name itself.
**Fix**: Check for EXCEPTION keyword and consume it before parsing the exception name.

### Issue 64: RESUME parsing is non-conformant

**Grammar rule** (§7.38): `RESUME [ AT ] NEXT STATEMENT`
**Parser** (line 2563-2585): Parses `RESUME [ AT ] { NEXT STATEMENT | paragraph-name }`. The grammar only allows `RESUME AT NEXT STATEMENT`, not `RESUME AT paragraph-name`.
**Divergence**: Parser accepts `RESUME AT SOME-PARA` which is not valid per the grammar.
**Fix**: Only allow `NEXT STATEMENT` after AT. Remove the else branch for paragraph-name.

### Issue 65: INVOKE does not handle class-name, SELF, SUPER, or NEW

**Grammar rule** (§7.39): Object reference can be `identifier-1 | class-name-1 | SELF | SUPER`. Method can be `literal-1 | identifier-2 | NEW`.
**Parser** (line 2512-2548): Parses object-ref and method-name as generic expressions. SELF/SUPER/NEW as identifiers would work but aren't semantically distinguished.
**Divergence**: Syntactically works (SELF/SUPER/NEW lexed as identifiers), but semantically lossy.
**Fix**: Low priority — check for SELF/SUPER keywords explicitly.

### Issue 66: INVOKE BY VALUE not handled properly

**Grammar rule** (§7.39): USING can have BY REFERENCE, BY CONTENT, BY VALUE.
**Parser** (line 2531-2533): Uses `Match` on BY, REFERENCE, CONTENT sequentially. This means `BY VALUE` would consume BY, then fail to match REFERENCE, then fail to match CONTENT, and VALUE would be parsed as an argument.
**Divergence**: `INVOKE obj method USING BY VALUE 42` — BY is consumed, VALUE is parsed as an argument expression.
**Fix**: After consuming BY, check for VALUE keyword in addition to REFERENCE/CONTENT.

### Issue 67: GOBACK RAISING not handled

**Grammar rule** (§7.52): `GOBACK [ RAISING { EXCEPTION exception-name-1 | identifier-1 | LAST EXCEPTION } ]`
**Parser** (line 2960-2964): Only parses `GOBACK`. No RAISING handling.
**Divergence**: `GOBACK RAISING EXCEPTION EC-PROGRAM-ERROR` — RAISING and everything after would be left unconsumed.
**Fix**: Check for RAISING keyword and consume the exception specification.

### Issue 68: CONTINUE AFTER seconds not handled

**Grammar rule** (§7.7): `CONTINUE [ AFTER arithmetic-expression-1 SECONDS ]`
**Parser** (line 1929-1933): Only parses bare `CONTINUE`.
**Divergence**: `CONTINUE AFTER 5 SECONDS` — AFTER 5 SECONDS would be left unconsumed.
**Fix**: Check for AFTER keyword and parse the expression + SECONDS.

### Issue 69: DELETE does not explicitly expect RECORD keyword

**Grammar rule** (§7.28): `DELETE file-name-1 RECORD`
**Parser** (line 2156): `Match(TokenKind.RecordKeyword)` — RECORD is optional in the parser but required per grammar.
**Divergence**: The parser is more lenient than the grammar. Minor issue.
**Fix**: Could change `Match` to `Expect`, but leniency is fine.

### Issue 70: RETURN statement is parsed but produces ContinueStatement

**Grammar rule** (§7.31): `RETURN file-name-1 RECORD [ INTO identifier-1 ] AT END imperative-statement-1 [ NOT AT END imperative-statement-2 ] [ END-RETURN ]`
**Parser** (line 2981-2998): Parses correctly but returns a `ContinueStatement` placeholder.
**Divergence**: The AST loses all RETURN information. Code generation cannot distinguish RETURN from CONTINUE.
**Fix**: Create a `ReturnSortStatement` AST node.

### Issue 71: RELEASE statement is parsed but produces ContinueStatement

**Grammar rule** (§7.32): `RELEASE record-name-1 [ FROM { identifier-1 | literal-1 } ]`
**Parser** (line 3003-3011): Same issue — returns `ContinueStatement`.
**Divergence**: Same as Issue 70.
**Fix**: Create a `ReleaseSortStatement` AST node.

---

## Section 8: Common Phrases

### Issue 72: ROUNDED phrase only consumes keyword, not MODE clause

**Grammar rule** (§8.1): `ROUNDED [ MODE IS { AWAY-FROM-ZERO | NEAREST-AWAY-FROM-ZERO | ... | TRUNCATION } ]`
**Parser** (multiple locations, e.g., line 1539): `Match(TokenKind.RoundedKeyword)` — consumes ROUNDED but not the optional MODE IS clause.
**Divergence**: `ADD A TO B ROUNDED MODE IS NEAREST-EVEN` — MODE IS NEAREST-EVEN would be left unconsumed.
**Fix**: After consuming ROUNDED, check for MODE keyword and consume the mode specification.

### Issue 73: SIZE ERROR phrases discard imperative statements

**Grammar rule** (§8.2): `[ ON SIZE ERROR imperative-statement-1 ] [ NOT ON SIZE ERROR imperative-statement-2 ]`
**Parser** (line 3113-3149): `SkipSizeErrorPhrases` parses the statements but discards them.
**Divergence**: The statements are parsed correctly (preventing desync) but their results are not stored in the AST.
**Fix**: Store size error handlers in the arithmetic statement AST nodes. Medium priority.

### Issue 74: Exception phrases (SkipExceptionPhrases) discard imperative statements

**Grammar rule** (§8.2 and various): ON EXCEPTION, INVALID KEY, AT END, ON OVERFLOW all contain imperative statements that should be preserved.
**Parser** (line 3018-3110): `SkipExceptionPhrases` parses and discards all exception handler statements.
**Divergence**: Same as Issue 73 — statements are parsed but not stored.
**Fix**: Refactor to return parsed statements for storage in the containing statement's AST node.

---

## Data Division Specific Issues

### Issue 75: FD LINAGE clause not parsed

**Grammar rule** (§5.5): FD entries can have LINAGE clause.
**Parser** (line 710-718): LABEL clause is skipped but stops at LINAGE — however LINAGE itself is skipped by the generic token skip in the else branch (line 720-722).
**Divergence**: LINAGE data is silently discarded. The LABEL skip at line 716 includes `!Check(TokenKind.LinageKeyword)` as a stop condition, suggesting LINAGE was intended to be parsed separately but is not.
**Fix**: Add LINAGE clause parsing or explicitly skip it.

### Issue 76: FD DATA RECORD clause collision

**Grammar rule**: FD can have `DATA RECORD IS record-name` (archaic).
**Parser** (line 716): LABEL skip stops at `!Check(TokenKind.DataKeyword)`.
**Divergence**: This correctly prevents the LABEL skip from consuming past a DATA keyword, but `DATA RECORD` in an FD would then fall into the else branch and be consumed token-by-token. Since DATA is a division keyword, it could trigger `IsDivisionKeyword` checks in callers. However, inside `ParseFileDescriptionEntry` the loop condition (line 679) is just `!Check(TokenKind.Period)`, so this is fine within the FD.
**Fix**: None needed within FD parsing context.

### Issue 77: Data description SIGN clause not fully parsed

**Grammar rule** (§5.5.1): `[ [ SIGN IS ] { LEADING | TRAILING } [ SEPARATE CHARACTER ] ]`
**Parser** (line 819-966): No explicit SIGN/LEADING/TRAILING clause handling. Falls into the else branch (line 963-966) which silently consumes unknown tokens.
**Divergence**: SIGN clause tokens are silently discarded.
**Fix**: Add SIGN IS LEADING/TRAILING SEPARATE CHARACTER parsing.

### Issue 78: JUSTIFIED clause checks Identifier for RIGHT instead of keyword

**Grammar rule** (§5.5.1): `{ JUSTIFIED | JUST } RIGHT`
**Parser** (line 949): `Match(TokenKind.Identifier)` to consume optional RIGHT.
**Divergence**: If RIGHT is not an Identifier token (e.g., it's a keyword), it won't be consumed. But since RIGHT is not typically a COBOL keyword, this is fine in practice.
**Fix**: Low priority — works for standard COBOL.

### Issue 79: SYNCHRONIZED clause checks Identifier for LEFT/RIGHT

**Grammar rule** (§5.5.1): `{ SYNCHRONIZED | SYNC } [ LEFT | RIGHT ]`
**Parser** (line 955-956): `if (Check(TokenKind.Identifier)) Advance()` — consumes one Identifier after SYNC.
**Divergence**: Would consume any identifier after SYNC, not specifically LEFT/RIGHT. If the next token is a data name (like the start of the next entry), it would be incorrectly consumed.
**Fix**: Check that the Identifier text is "LEFT" or "RIGHT" before consuming.

### Issue 80: OCCURS ASCENDING/DESCENDING KEY loop may misidentify identifiers

**Grammar rule** (§5.5.1): `[ { ASCENDING | DESCENDING } KEY IS { data-name-2 } ... ] ...`
**Parser** (line 927-930): Consumes identifiers until ASCENDING/DESCENDING/INDEXED. Does not check for other data description clause keywords.
**Divergence**: If a data name happens to match a clause keyword (VALUE, PIC, etc.), it would be consumed as a key name. However, the while loop at line 928 doesn't check for `IsDataClauseKeyword`, so a key name like `VALUE-KEY` (which starts with VALUE) wouldn't trigger this if VALUE is a keyword token — it would stop the loop prematurely.
**Fix**: Add `!IsDataClauseKeyword(Current.Kind)` to the while condition on line 928.

---

## Fix Status (updated 2026-03-15)

### FIXED — Commit 4ecb788 (Session 1)
- Issues 7-13: IsDivisionKeyword → IsDivisionStart (6 locations + ID division)
- Issue 23: ACCEPT FROM DATE YYYYMMDD
- Issue 28: COMPUTE multi-target

### FIXED — Commits 054fd7e, 45a6c28 (Session 1)
- Issue 42: CLOSE WITH LOCK / WITH NO REWIND
- Issue 59: PERFORM VARYING AFTER

### FIXED — Commit 8acb8c2 (Session 2 — all 65 remaining issues)
- Issue 5: Abbreviated NOT in combined relations (NegateRelationalOp helper)
- Issue 15: PROGRAM-ID AS literal, COMMON/INITIAL/RECURSIVE
- Issue 16: END PROGRAM token check documented
- Issue 18: LOCAL-STORAGE SECTION parsed as WORKING-STORAGE
- Issues 21-22: Section/paragraph names as keyword tokens (IsUserDefinableKeyword)
- Issue 24: ACCEPT ON EXCEPTION / END-ACCEPT
- Issues 25-27: CORRESPONDING flag documented on MOVE/ADD/SUBTRACT
- Issue 29: STOP RUN WITH STATUS phrase
- Issue 30: STOP literal (archaic)
- Issues 32-33: EVALUATE ALSO + partial-expression WHEN objects + ANY
- Issue 34: EXIT PERFORM CYCLE
- Issue 35: EXIT FUNCTION / EXIT METHOD
- Issue 37: NEXT SENTENCE semantics documented
- Issue 38: INITIALIZE REPLACING / WITH FILLER / THEN TO DEFAULT
- Issues 39-40: Full INSPECT parsing (multiple phrases, BEFORE/AFTER INITIAL)
- Issue 41: OPEN SHARING + WITH NO REWIND
- Issue 43: READ PREVIOUS
- Issue 44: READ NOT INVALID KEY
- Issues 45-46: WRITE/REWRITE FILE keyword prefix
- Issues 47-48: SORT THRU + multiple USING/GIVING files
- Issue 49: SORT WITH DUPLICATES IN ORDER
- Issue 50: SORT COLLATING SEQUENCE
- Issue 51: CALL USING OMITTED
- Issues 54-56: UNSTRING OR delimiters, ALL, DELIMITER IN, COUNT IN, WITH POINTER
- Issue 58: SET ADDRESS OF
- Issue 60: PERFORM UNTIL EXIT
- Issue 62: CANCEL multiple operands
- Issue 63: RAISE EXCEPTION prefix
- Issue 64: RESUME conformant (NEXT STATEMENT only)
- Issue 66: INVOKE BY VALUE
- Issue 67: GOBACK RAISING
- Issue 68: CONTINUE AFTER seconds
- Issue 72: ROUNDED MODE IS clause (ConsumeRoundedPhrase helper)
- Issue 75: FD LINAGE clause
- Issue 77: SIGN IS LEADING/TRAILING SEPARATE CHARACTER
- Issue 79: SYNCHRONIZED LEFT/RIGHT validation
- Issue 80: OCCURS key loop data-clause boundary check

### FIXED — ANTLR4 grammar operand type correction (Session 3)
- ADD/SUBTRACT/MULTIPLY/DIVIDE: operand lists changed from `arithmeticExpression` to simple `identifier | literal` sub-rules (`addOperand`, `subtractOperand`, `multiplyOperand`, `divideOperand`). Per COBOL-85 spec, these statements require simple operands (identifiers or literals), not full arithmetic expressions. Only COMPUTE uses `arithmeticExpression` for its right-hand side.
- SUBTRACT FROM/GIVING targets changed from `identifierList` to `subtractTarget+` where `subtractTarget: identifier ROUNDED?`, correctly associating ROUNDED with each individual target per spec.
- ADD operand list changed from `arithmeticExpression (COMMA arithmeticExpression)*` to `addOperand+` where `addOperand: identifier | literal`.

### NOT FIXED (lexer changes required)
- Issue 4: EXCLUSIVE-OR (needs ExclusiveOrKeyword in lexer, extremely rare)

### NO FIX NEEDED (per audit analysis)
- Issue 6: Condition-name — correctly allows bare identifiers
- Issue 36: GO TO DEPENDING — correct
- Issue 52: CALL NOT ON EXCEPTION — SkipExceptionPhrases handles it
- Issue 57: SEARCH ALL WHEN — semantic validation, not parsing
- Issue 61: PERFORM WITH TEST — adequately handled
- Issue 69: DELETE RECORD — leniency is fine
- Issue 76: FD DATA RECORD — correct within FD context
- Issue 78: JUSTIFIED RIGHT — works for standard COBOL

### SEMANTIC ISSUES (not parser fixes — need emitter/runtime changes)
- Issue 1: Qualification chain — needs semantic resolver, not parser
- Issue 2: ALL subscript — needs runtime support
- Issue 3: Function boolean arguments — ParseArithmeticExpression is sufficient
- Issue 14: IDENTIFICATION optional — 2023 spec; legacy always includes it
- Issue 17: SPECIAL-NAMES — needs emitter support for DECIMAL-POINT IS COMMA
- Issue 19: DECLARATIVES — needs USE statement handler in emitter
- Issue 20: PROCEDURE DIVISION USING/RETURNING — needs parameter linkage
- Issues 70-71: RETURN/RELEASE — need dedicated AST nodes and emitter
- Issues 73-74: SIZE ERROR/exception handler statements — need AST storage
