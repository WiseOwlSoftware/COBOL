# Parser Architecture Review

Reviewer: Claude (architectural review for parser rewrite)
File: `src/CobolSharp/Compiler/Parsing/Parser.cs` (3265 lines)
Date: 2026-03-14

---

## 1. Infinite Loop Risks

Every loop in the parser that could fail to make progress. The global `MaxIterations` guard (line 19, 1M iterations) is a safety net but should not be relied upon.

### 1.1 ParseStatements (line 1216)

```csharp
while (Current.Kind != TokenKind.EndOfFile)
{
    foreach (var t in terminators) if (Check(t)) return statements;
    if (Check(TokenKind.Period)) return statements;
    var stmt = ParseStatement();
    if (stmt != null) statements.Add(stmt);
}
```

**Risk**: If `ParseStatement()` returns `null` WITHOUT advancing the position, this loops forever. This happens when `HandleUnknownStatement` (line 1291) is called, which does `Advance(); SkipToPeriodOrKeyword()`. That method (line 112) stops at periods, statement keywords, scope terminators, and EOF. If the current token after skipping is one of the terminators passed to `ParseStatements`, control returns normally. But if the current token is a **scope terminator that is NOT in the terminators array**, `SkipToPeriodOrKeyword` stops, `ParseStatements` does not find it in its terminator list, it is not a period, and `ParseStatement` is called again. `ParseStatement` does not recognize scope terminators (they are not in `IsStatementStart`), so `HandleUnknownStatement` fires, advances one token, and `SkipToPeriodOrKeyword` stops at the same or next scope terminator.

**Triggering sequence**: An orphaned `END-IF` inside a PERFORM body:
```cobol
PERFORM UNTIL X > 10
    DISPLAY "HELLO"
    END-IF
END-PERFORM
```
`ParseStatements(EndPerformKeyword)` encounters `END-IF`. It is not in the terminators. It is not a period. `ParseStatement` calls `HandleUnknownStatement`, which advances past `END-IF`, then `SkipToPeriodOrKeyword` stops at `END-PERFORM` (a scope terminator). Back in `ParseStatements`, `END-PERFORM` is in the terminators, so it returns. **This particular case is safe**, but only by luck.

A worse case: mismatched nesting like:
```cobol
IF X > 1
    DISPLAY "A"
    END-PERFORM
END-IF
```
`ParseStatements(ElseKeyword, EndIfKeyword)` encounters `END-PERFORM`. Not in terminators, not a period. `HandleUnknownStatement` advances past it, `SkipToPeriodOrKeyword` stops at `END-IF`. `END-IF` IS in the terminators, so it returns. **Also safe by luck.**

**Actual infinite loop scenario**: A token that is not a statement start, not a scope terminator, not a period, and not EOF, AND that `HandleUnknownStatement` cannot skip past. This cannot happen because `HandleUnknownStatement` always calls `Advance()` first (line 1295), guaranteeing at least one token of progress per iteration. However, `SkipToPeriodOrKeyword` could stop immediately if the next token IS a statement start, causing `ParseStatement` to be called on a statement keyword that somehow fails to consume its keyword. This is prevented because every statement parser calls `Advance()` on its keyword. **Verdict: safe due to defense in depth, but fragile.**

### 1.2 ParseParagraph (line 1191)

```csharp
while (Current.Kind != TokenKind.EndOfFile &&
       !IsParagraphHeader() && !IsSectionHeader() &&
       !IsDivisionKeyword(Current.Kind))
{
    if (Check(TokenKind.Period)) { Advance(); continue; }
    var stmt = ParseStatement();
    if (stmt != null) statements.Add(stmt);
}
```

**Risk**: Same as ParseStatements but with different exit conditions. If `ParseStatement` returns null without advancing... but it always advances (see above). If it returns a non-null statement but the position didn't actually advance (impossible given every parser calls `Advance()` at minimum). **Safe but relies on every statement parser advancing.**

### 1.3 ParseEnvironmentDivision (line 259)

```csharp
while (Current.Kind != TokenKind.EndOfFile && !IsDivisionStart())
```

The loop body has multiple `if/else if` branches. The final `else` at line 332 calls `Advance()`, ensuring progress. **Safe.**

### 1.4 ParseDataDescriptionEntry (line 814)

```csharp
while (!Check(TokenKind.Period) && Current.Kind != TokenKind.EndOfFile)
```

The clause-parsing loop at line 814. Every branch either matches a keyword and advances, or falls through to the `else` at line 946 which calls `Advance()`. **Safe.**

### 1.5 ParseSelectEntry (line 384)

```csharp
while (!Check(TokenKind.Period) && Current.Kind != TokenKind.EndOfFile &&
       !Check(TokenKind.SelectKeyword))
```

The `else` at line 451 calls `Advance()`. **Safe.**

### 1.6 ParseLevel88Values (line 996)

```csharp
while (!Check(TokenKind.Period) && Current.Kind != TokenKind.EndOfFile)
{
    var val = ParseExpression();
```

**Risk**: If `ParseExpression()` fails to advance. Looking at `ParsePrimaryExpression` (line 2907), the `default` case (line 2977) reports an error and calls `Advance()`, then returns a synthetic expression. So it always advances. However, if the expression is an identifier followed by `IN`/`OF` qualification (line 2929), the loop `while (Check(InKeyword) || Check(OfKeyword))` consumes qualifiers. If the token after the qualifier is a period, the outer loop exits. **Safe.**

### 1.7 DisplayStatement operand loop (line 1308)

```csharp
while (!Check(TokenKind.Period) && !Check(TokenKind.EndOfFile) &&
       !IsStatementStart(Current.Kind) && !IsScopeTerminator(Current.Kind) &&
       !Check(TokenKind.EndDisplayKeyword))
```

The `UPON` and `WITH NO ADVANCING` checks inside the loop advance and `continue`. The fallthrough calls `ParseExpression()`, which always advances. **Safe.**

### 1.8 SkipExceptionPhrases (line 2700)

```csharp
while (Current.Kind != TokenKind.Period && Current.Kind != TokenKind.EndOfFile &&
       Current.Kind != endScopeTerminator)
```

**Risk**: The inner loops at lines 2715, 2735, 2758, 2775, 2779 call `ParseStatement()` or `Advance()`. However, the outer loop's `break` at line 2790 is reached when none of the `if` conditions match. If the current token is not `ON`, `NOT`, `INVALID`, or `AT`, the loop breaks. **Safe, but the `NOT` branch (line 2727) uses `_position = saved` to backtrack, which could theoretically loop if the saved position is the same as current. However, the `break` at line 2750 prevents re-entry.** **Safe.**

### 1.9 SkipSizeErrorPhrases (line 2795)

The NOT ON SIZE ERROR branch (line 2811) backtracks with `_position = saved` if the pattern doesn't match. This is not in a loop, so no infinite loop risk. **Safe.**

### 1.10 OCCURS clause INDEXED BY (line 914)

```csharp
while (Check(TokenKind.IndexedKeyword))
{
    Advance();
    Match(TokenKind.ByKeyword);
    while (Check(TokenKind.Identifier)) Advance();
}
```

If `INDEXED` is not followed by `BY` and the next token is not `Identifier`, the outer loop checks `IndexedKeyword` again. Since we consumed `INDEXED`, the next token won't be `INDEXED` unless the source has consecutive `INDEXED INDEXED`. **Safe in practice.**

### 1.11 Function call argument loop (line 3000)

```csharp
while (!Check(TokenKind.RightParen) && Current.Kind != TokenKind.EndOfFile)
{
    args.Add(ParseArithmeticExpression());
    if (!Check(TokenKind.RightParen)) Match(TokenKind.Comma);
}
```

**Risk**: If `ParseArithmeticExpression()` fails to advance. It bottoms out at `ParsePrimaryExpression()`, which always advances (the default case at line 2977 calls `Advance()`). **Safe.**

### Summary of Infinite Loop Risks

No actual infinite loops exist today due to the defense-in-depth of `HandleUnknownStatement` always advancing and `ParsePrimaryExpression` always advancing. However, the architecture is **fragile**: any new statement parser that forgets to call `Advance()` on its keyword would create a loop. The `MaxIterations` guard is the only backstop.

---

## 2. Period Consumption Model

### 2.1 Who Consumes Periods?

The parser has **two different period consumption models** that conflict:

**Model A — Paragraph-level consumption (line 1198-1210)**:
`ParseParagraph` has a loop that checks for periods and consumes them directly: `if (Check(TokenKind.Period)) { Advance(); continue; }`. This means the paragraph loop "owns" period consumption and statement parsers should NOT consume periods.

**Model B — Statement-level consumption**:
Several statement parsers consume periods via `Match(TokenKind.Period)` or `Expect(TokenKind.Period)`:
- `ParseSelectEntry` (line 455): `Match(TokenKind.Period)` — consumes the sentence-terminating period
- `ParseDataDescriptionEntry` (line 951): `Expect(TokenKind.Period)` — consumes period
- `ParseFileDescriptionEntry` (line 720): `Expect(TokenKind.Period)` — consumes period

These are in the data division, where periods are entry terminators, not sentence terminators. This is correct for data division entries.

**Model C — ParseStatements returns at period (line 1228)**:
`ParseStatements` returns when it sees a period WITHOUT consuming it. The caller is responsible for consuming it. But some callers (like inline PERFORM at line 1532) call `ParseStatements` followed by `Expect(EndPerformKeyword)` — they never consume the period at all.

### 2.2 The Core Problem

In COBOL, a **sentence** is one or more statements terminated by a period. The period terminates ALL open scopes. The parser has no `ParseSentence` method. Instead:

1. `ParseParagraph` (line 1191) loops, consuming periods it finds.
2. `ParseStatements` (line 1216) returns at a period without consuming it.
3. `ParseParagraph` calls `ParseStatement` directly, not `ParseStatements`.

This means `ParseParagraph` effectively acts as both the paragraph parser and the sentence parser. Periods are consumed at line 1203, between statement parses. This works for simple cases but breaks the COBOL semantic that a period terminates ALL open scopes.

### 2.3 Where It Breaks

**Scenario: Period inside a compound statement**

```cobol
MAIN-PARA.
    IF X > 1
        DISPLAY "A".
    DISPLAY "B".
```

Per the spec, the period after `"A"` terminates the IF statement AND the sentence. `DISPLAY "B"` starts a new sentence. But what happens in the parser:

1. `ParseParagraph` calls `ParseStatement()`, which calls `ParseIfStatement()`.
2. `ParseIfStatement` calls `ParseStatements(ElseKeyword, EndIfKeyword)` (line 1487).
3. `ParseStatements` parses `DISPLAY "A"`, then sees the period at line 1228 and returns `[DISPLAY "A"]`.
4. Back in `ParseIfStatement`, there is no ELSE or END-IF, so `Match(EndIfKeyword)` fails silently.
5. `ParseIfStatement` returns the IF node.
6. Back in `ParseParagraph`, the period is still unconsumed (ParseStatements left it). The loop at line 1202 checks `Check(TokenKind.Period)` and consumes it.
7. `ParseParagraph` continues and parses `DISPLAY "B"`.

**This actually works correctly by accident!** The period terminates the IF because `ParseStatements` returns at the period, and the IF parser finishes. The period is then consumed by the paragraph loop. `DISPLAY "B"` is correctly parsed as a separate statement in the paragraph.

**But consider nested IFs:**

```cobol
MAIN-PARA.
    IF X > 1
        IF Y > 2
            DISPLAY "A".
    DISPLAY "B".
```

1. `ParseParagraph` → `ParseIfStatement` (outer IF).
2. Outer IF calls `ParseStatements(ElseKeyword, EndIfKeyword)`.
3. `ParseStatements` calls `ParseStatement` → `ParseIfStatement` (inner IF).
4. Inner IF calls `ParseStatements(ElseKeyword, EndIfKeyword)`.
5. Inner `ParseStatements` parses `DISPLAY "A"`, sees period, returns.
6. Inner IF finishes (no END-IF found).
7. Back in outer `ParseStatements`, the period is the current token. `ParseStatements` sees it and returns.
8. Outer IF finishes.
9. Back in `ParseParagraph`, period is consumed.

**This works correctly.** The period terminates both IFs because `ParseStatements` returns at the period for each level. The key insight is that returning at a period cascades up through all recursive `ParseStatements` calls, effectively terminating all open scopes.

### 2.4 Inconsistency in Procedure Division Initial Statements

`ParseProcedureDivision` (line 1079) has its own loop for statements before the first paragraph:

```csharp
while (Current.Kind != TokenKind.EndOfFile && !IsDivisionKeyword(Current.Kind))
{
    if (IsSectionHeader()) break;
    if (IsParagraphHeader()) break;
    if (Check(TokenKind.Period)) { Advance(); continue; }
    var stmt = ParseStatement();
    if (stmt != null) initialStatements.Add(stmt);
}
```

This duplicates the paragraph loop logic but at the procedure division level. Same period model.

### 2.5 Section Statement Loop (line 1154)

```csharp
while (Current.Kind != TokenKind.EndOfFile && !IsSectionHeader() &&
       !IsDivisionKeyword(Current.Kind) && !IsParagraphHeader())
{
    if (Check(TokenKind.Period)) { Advance(); continue; }
    if (IsStatementStart(Current.Kind))
    {
        var stmt = ParseStatement();
        if (stmt != null) sectionStatements.Add(stmt);
    }
    else { break; }
}
```

This has a third variant: it breaks on non-statement tokens. The other loops call `HandleUnknownStatement` via `ParseStatement` instead. **Inconsistent.**

### 2.6 Verdict

The period model works by accident for most cases because `ParseStatements` uniformly returns at periods and the cascade of returns terminates open scopes. But:

- There is no explicit `ParseSentence` concept
- The paragraph/section/procedure-division loops duplicate the same period-handling logic with slight variations
- Data division entries correctly consume their own periods (they are entry terminators, not sentence terminators)
- Statement parsers in the procedure division correctly do NOT consume periods

---

## 3. Error Recovery: HandleUnknownStatement

### 3.1 Does It Guarantee Forward Progress?

```csharp
private Statement? HandleUnknownStatement()
{
    ReportError("CS0200", ...);
    Advance();                   // line 1295 — always advances at least one token
    SkipToPeriodOrKeyword();     // line 1296 — skips to recovery point
    return null;
}
```

**Yes**, it guarantees forward progress because `Advance()` is called unconditionally before `SkipToPeriodOrKeyword()`. Even if `SkipToPeriodOrKeyword` makes no progress (the very next token is already a period or keyword), the parser has moved forward by one token.

### 3.2 Quality of Recovery

The recovery strategy is: advance past the offending token, then skip to the nearest period or known keyword. This is reasonable but has issues:

**Problem 1: Over-skipping**. If the error is a simple typo in a keyword (e.g., `MOVI` instead of `MOVE`), the parser skips everything until the next period or keyword, potentially losing many valid statements in a multi-statement sentence.

**Problem 2: Under-reporting**. Only one error is reported per unknown token. If a long sequence of gibberish tokens exists before the next recovery point, they are silently skipped.

**Problem 3: No synchronization with compound statements**. If recovery skips past an `END-IF` or `END-PERFORM`, the containing compound statement will not find its terminator and may silently produce incorrect AST structure.

### 3.3 SkipToPeriodOrKeyword Stops

```csharp
private void SkipToPeriodOrKeyword()
{
    while (Current.Kind != TokenKind.EndOfFile)
    {
        if (Current.Kind == TokenKind.Period) return;
        if (IsStatementStart(Current.Kind) || IsDivisionKeyword(Current.Kind) ||
            IsScopeTerminator(Current.Kind)) return;
        Advance();
    }
}
```

It stops at scope terminators but does NOT stop at paragraph headers (Identifier followed by Period). This means if error recovery is triggered inside a paragraph, it could skip past the end of the paragraph into a paragraph header. The paragraph header's `Identifier` would be eaten if it happens to not be a statement keyword, and the period would be consumed, effectively merging two paragraphs. **This is a real bug.**

---

## 4. Statement Parser Consistency

### 4.1 Expected Pattern

Each statement parser should:
1. Record `start` position
2. `Advance()` to consume the statement keyword
3. Parse the statement's operands and clauses
4. Optionally consume scope terminator via `Match(TokenKind.EndXxxKeyword)`
5. NOT consume a trailing period
6. Return the AST node

### 4.2 Statements That Follow The Pattern

Most statement parsers follow this pattern correctly: DISPLAY, STOP RUN, MOVE, ADD, SUBTRACT, COMPUTE, IF, PERFORM, GO TO, ALTER, CONTINUE, EXIT, ACCEPT, INITIALIZE, OPEN, CLOSE, READ, WRITE, REWRITE, DELETE, START, SORT, CALL, CANCEL, STRING, UNSTRING, INSPECT, MULTIPLY, DIVIDE, EVALUATE, SET, SEARCH, GOBACK, INITIATE, GENERATE, TERMINATE, INVOKE, RAISE, RESUME.

### 4.3 Deviations

**INSPECT (line 2209)**: Calls `SkipToEndOfStatement()` at line 2259 after parsing only the first tallying/replacing/converting clause. INSPECT can have multiple tallying phrases, BEFORE/AFTER phrases, etc. The parser gives up and skips the rest. This is an incomplete implementation, not an architectural flaw.

**EVALUATE (line 2460)**: Uses `Match(TokenKind.EndEvaluateKeyword)` at line 2500, making the scope terminator optional. Per the spec, EVALUATE without END-EVALUATE is valid (period-terminated). This is correct but means EVALUATE can silently end without its terminator if there's a typo.

**SET (line 2608)**: The identifier loop at line 2614 `while (Check(TokenKind.Identifier))` will greedily consume identifiers including the `TO` target value if `TO` is not a keyword but an identifier. Since `TO` IS a keyword (`TokenKind.ToKeyword`), this is safe, but the pattern differs from other statements that use explicit keyword delimiters.

**MOVE (line 1347)**: The target loop at line 1356 calls `Expect(TokenKind.Identifier, ...)` inside the loop. If the current token is NOT an identifier, `Expect` produces a synthetic token and does NOT advance. The loop condition checks `!Check(TokenKind.Period) && ...`, and since position hasn't changed, the loop body calls `Expect` again, producing another error. **This is an infinite loop if a non-identifier, non-period, non-statement-start, non-scope-terminator token appears after TO**. Example: `MOVE 5 TO 123.` — the `123` is an IntegerLiteral, not an Identifier. `Expect` fails, position doesn't advance, loop repeats forever.

**ADD (line 1367)**: Same issue in the target loop at line 1382. `Expect(TokenKind.Identifier, ...)` inside a loop. Same infinite loop risk with `ADD 1 TO 2.` (the `2` is not an identifier).

**SUBTRACT (line 1412)**: Same issue at line 1426.

**MULTIPLY (line 2508)**: Same issue at line 2517.

**DIVIDE (line 2545)**: Same issue at line 2575.

**These are all real infinite loop bugs.** The `MaxIterations` guard catches them, but they produce 1 million error messages first.

### 4.4 Expect Without Advance — The Root Cause

`Expect` (line 53) returns a synthetic token without advancing when the expected token is not found:

```csharp
private Token Expect(TokenKind kind, string? contextMessage = null)
{
    if (Current.Kind == kind)
        return Advance();
    var msg = contextMessage ?? ...;
    ReportError("CS0100", msg);
    return new Token(kind, "", Current.Span); // NO ADVANCE
}
```

This is the correct behavior for `Expect` in general (the caller should handle recovery). But when `Expect` is called inside a loop without additional recovery logic, it creates infinite loops.

### 4.5 Statement Parsers That Use Expect In Loops

| Statement | Line | Loop Pattern | Vulnerable? |
|-----------|------|-------------|-------------|
| MOVE | 1358 | `Expect(Identifier)` in while loop | YES |
| ADD | 1388 | `Expect(Identifier)` in while loop | YES |
| SUBTRACT | 1432 | `Expect(Identifier)` in while loop | YES |
| MULTIPLY | 2523 | `Expect(Identifier)` in while loop | YES |
| DIVIDE | 2581 | `Expect(Identifier)` in while loop | YES |

---

## 5. Scope Termination: ParseStatements and Compound Statements

### 5.1 How ParseStatements Interacts With Compound Statements

`ParseStatements` (line 1216) accepts a `params TokenKind[]` of terminators. It returns when it sees any terminator or a period (without consuming either). The compound statements use it like this:

| Compound Statement | Call | Terminators |
|---|---|---|
| IF then-body | `ParseStatements(ElseKeyword, EndIfKeyword)` | ELSE, END-IF |
| IF else-body | `ParseStatements(EndIfKeyword)` | END-IF |
| PERFORM UNTIL body | `ParseStatements(EndPerformKeyword)` | END-PERFORM |
| PERFORM VARYING body | `ParseStatements(EndPerformKeyword)` | END-PERFORM |
| EVALUATE when body | `ParseStatements(WhenKeyword, EndEvaluateKeyword)` | WHEN, END-EVALUATE |
| EVALUATE when-other body | `ParseStatements(EndEvaluateKeyword)` | END-EVALUATE |
| SEARCH at-end | `ParseStatements(WhenKeyword, EndSearchKeyword)` | WHEN, END-SEARCH |
| SEARCH when body | `ParseStatements(WhenKeyword, EndSearchKeyword)` | WHEN, END-SEARCH |
| READ at-end | `ParseStatements(NotKeyword, EndReadKeyword)` | NOT, END-READ |
| READ not-at-end | `ParseStatements(EndReadKeyword)` | END-READ |

### 5.2 Problem: Period Terminates All Scopes (Correctly Handled)

When a period appears inside a nested compound statement, `ParseStatements` returns at every level because each level checks for Period. This correctly implements the COBOL rule that a period terminates all open scopes.

### 5.3 Problem: Terminators Leak Through Scopes

Consider:
```cobol
IF X > 1
    PERFORM UNTIL Y > 10
        IF Z > 5
            DISPLAY "A"
        END-IF
    END-PERFORM
END-IF
```

The inner `ParseStatements(ElseKeyword, EndIfKeyword)` for `IF Z > 5` lists `END-IF` as a terminator. When it sees `END-IF`, it returns. `ParseIfStatement` consumes it with `Match(EndIfKeyword)`. Then the inner `ParseStatements(EndPerformKeyword)` for the PERFORM sees `END-PERFORM` and returns. `ParsePerformStatement` consumes it. Then the outer `ParseStatements(ElseKeyword, EndIfKeyword)` sees `END-IF` and returns. The outer IF consumes it. **This works correctly.**

But consider a **missing END-IF**:
```cobol
IF X > 1
    PERFORM UNTIL Y > 10
        IF Z > 5
            DISPLAY "A"
    END-PERFORM
END-IF
```

The inner `ParseStatements(ElseKeyword, EndIfKeyword)` does NOT list `END-PERFORM` as a terminator. It does not stop at `END-PERFORM`. Instead, `ParseStatement` is called, which calls `HandleUnknownStatement` (since `END-PERFORM` is not a statement keyword). `HandleUnknownStatement` advances past `END-PERFORM` and then `SkipToPeriodOrKeyword` stops at `END-IF` (a scope terminator). Back in `ParseStatements`, `END-IF` IS a terminator, so it returns. The inner IF gets `DISPLAY "A"` as its body. Then the PERFORM's `ParseStatements(EndPerformKeyword)` sees `END-IF` — not its terminator, not a period. It calls `ParseStatement`, which calls `HandleUnknownStatement`, which skips `END-IF`. **The AST is garbage and two tokens were consumed as errors.**

### 5.4 The Fundamental Problem

`ParseStatements` does not understand scope nesting. Each compound statement registers only its own terminators, but scope terminators from outer statements are not propagated. The current approach works for correctly-nested code but produces poor error recovery and confusing errors for mismatched nesting.

### 5.5 NEXT SENTENCE

The parser does not handle `NEXT SENTENCE` (archaic but still used). `NEXT` would be parsed as an identifier expression, and `SENTENCE` as another identifier, producing garbage.

---

## 6. Recommended Architecture

### 6.1 Key Insight From the Spec

The COBOL procedure division has this structure:

```
procedure-division = header "." { section | paragraph | sentence } ...
section            = section-name "SECTION" [segment] "." { paragraph | sentence } ...
paragraph          = paragraph-name "." { sentence } ...
sentence           = { statement } ... "."
```

A **sentence** is a sequence of statements terminated by a period. The period is consumed by the sentence parser. Compound statements (IF, PERFORM, EVALUATE) contain **imperative statements**, not sentences. Imperative statement sequences are terminated by scope terminators, not periods. A period inside an imperative statement sequence terminates the sentence (and thus all open scopes).

### 6.2 Recommended Structure

#### ParseParagraph

```
ParseParagraph(sectionName):
    name = Advance()        // paragraph name
    Expect(Period)
    sentences = []
    while not EOF
          and not IsParagraphHeader()
          and not IsSectionHeader()
          and not IsDivisionStart():
        sentences.append(ParseSentence())
    return Paragraph(name, flatten(sentences))
```

#### ParseSentence

```
ParseSentence():
    statements = []
    while not EOF and not Check(Period):
        if IsStatementStart(Current):
            stmt = ParseStatement()
            if stmt != null:
                statements.append(stmt)
        else:
            // Error recovery: unknown token in sentence
            ReportError(...)
            Advance()
            SkipToPeriodOrKeyword()
    if Check(Period):
        Advance()           // SENTENCE OWNS THE PERIOD
    return statements
```

**Critical**: The sentence consumes the period. Statement parsers and `ParseImperativeStatements` NEVER consume periods.

#### ParseImperativeStatements

This replaces the current `ParseStatements`. It is used inside compound statement bodies (IF then/else, PERFORM body, EVALUATE when, etc.).

```
ParseImperativeStatements(terminators: TokenKind[]):
    statements = []
    while not EOF:
        // Check explicit terminators (END-IF, ELSE, WHEN, etc.)
        if Current in terminators:
            return statements

        // Period terminates ALL open scopes — return WITHOUT consuming
        if Check(Period):
            return statements

        // Scope terminator not in our list = mismatched nesting
        if IsScopeTerminator(Current):
            ReportError("Mismatched scope terminator")
            return statements    // DO NOT CONSUME — let parent handle

        if IsStatementStart(Current):
            stmt = ParseStatement()
            if stmt != null:
                statements.append(stmt)
        else:
            ReportError(...)
            Advance()
    return statements
```

**Key differences from current `ParseStatements`**:
1. Explicitly checks for mismatched scope terminators and reports them
2. Returns at mismatched scope terminators WITHOUT consuming, allowing the correct scope to handle them
3. Does not consume periods (the caller's sentence loop does that)

#### IF Statement

```
ParseIfStatement():
    start = Current.Span.Start
    Advance()                // IF
    condition = ParseConditionExpression()
    Match(ThenKeyword)       // optional THEN

    thenBody = ParseImperativeStatements([ElseKeyword, EndIfKeyword])

    elseBody = []
    if Match(ElseKeyword):
        elseBody = ParseImperativeStatements([EndIfKeyword])

    // Explicit scope termination
    isDelimited = Match(EndIfKeyword)

    // If NOT delimited (period-terminated), the IF is a conditional statement.
    // The period will be consumed by ParseSentence when we return.

    return IfStatement(condition, thenBody, elseBody, isDelimited)
```

#### PERFORM (inline)

```
ParsePerformInline(start, testAfter, varying, until, times):
    body = ParseImperativeStatements([EndPerformKeyword])
    Expect(EndPerformKeyword)  // REQUIRED for inline PERFORM
    return PerformStatement(null, body, times, until, varying, testAfter)
```

**Note**: Inline PERFORM always requires END-PERFORM per the spec. If it is missing, `Expect` reports an error and parsing continues.

#### EVALUATE

```
ParseEvaluateStatement():
    start = Current.Span.Start
    Advance()                // EVALUATE
    subject = ParseExpression()

    whenClauses = []
    whenOther = []

    while Check(WhenKeyword):
        whenStart = Current.Span.Start
        Advance()            // WHEN

        if Check(OtherKeyword):
            Advance()        // OTHER
            whenOther = ParseImperativeStatements([EndEvaluateKeyword])
            break

        // Parse selection objects (multiple WHENs before statements)
        objects = [ParseExpression()]
        while Check(WhenKeyword) and Peek() != OtherKeyword:
            Advance()        // WHEN
            objects.append(ParseExpression())

        stmts = ParseImperativeStatements([WhenKeyword, EndEvaluateKeyword])
        whenClauses.append(WhenClause(objects, stmts))

    Match(EndEvaluateKeyword)  // optional scope terminator
    return EvaluateStatement(subject, whenClauses, whenOther)
```

### 6.3 Fixing the Expect-In-Loop Bug

For every loop that calls `Expect(Identifier)` (MOVE targets, ADD targets, etc.), add a guard:

```
// Instead of:
while (loopCondition)
{
    var id = Expect(TokenKind.Identifier, "...");
    targets.Add(...);
}

// Use:
while (loopCondition)
{
    if (!Check(TokenKind.Identifier))
    {
        ReportError("...");
        Advance();  // ensure progress
        break;      // exit the loop
    }
    var id = Advance();
    targets.Add(...);
}
```

Or create a helper:

```csharp
private Token? ExpectOrBreak(TokenKind kind, string message)
{
    if (Current.Kind == kind)
        return Advance();
    ReportError("CS0100", message);
    Advance(); // ensure progress
    return null;
}
```

### 6.4 Summary of Changes

1. **Add `ParseSentence`**: Owns period consumption. Called by paragraph/section/procedure-division loops.
2. **Rename `ParseStatements` to `ParseImperativeStatements`**: Used inside compound statement bodies. Returns at period WITHOUT consuming. Returns at mismatched scope terminators with error.
3. **Fix Expect-in-loop**: All loops using `Expect(Identifier)` must guard against non-identifier tokens.
4. **Fix SkipToPeriodOrKeyword**: Should also stop at paragraph headers (Identifier + Period lookahead) to prevent paragraph-crossing during error recovery.
5. **Handle NEXT SENTENCE**: Recognize `NEXT SENTENCE` as an archaic but valid construct.
6. **Track delimited vs. non-delimited**: IF, EVALUATE, and arithmetic statements should track whether they were terminated by an explicit scope terminator or implicitly by period/exhaustion. This affects semantic analysis (a non-delimited IF inside another statement is a conditional statement, not an imperative one).
