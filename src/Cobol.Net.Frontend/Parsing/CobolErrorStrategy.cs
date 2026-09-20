// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using CobolNet.Editions;
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Parsing;

/// <summary>
/// COBOL-aware error strategy that replaces ANTLR's generic messages with
/// actionable, human-readable diagnostics. Pattern-matches common COBOL
/// mistakes and suggests fixes.
/// </summary>
public sealed class CobolErrorStrategy : DefaultErrorStrategy
{
    /// <summary>
    /// Structured diagnostic hint with a COBOL-level code and priority.
    /// Lower priority values sort first (higher importance).
    /// </summary>
    private readonly record struct DiagnosticHint(Diagnostics.DiagnosticDescriptor Descriptor, string Message, int Priority)
    {
        public string Code => Descriptor.Code;
    }

    protected override void ReportNoViableAlternative(Parser recognizer, NoViableAltException e)
    {
        var token = e.OffendingToken ?? recognizer.CurrentToken;
        var message = BuildMessage(recognizer, token, e, ErrorKind.NoViableAlternative);
        recognizer.NotifyErrorListeners(token, message, e);
    }

    protected override void ReportInputMismatch(Parser recognizer, InputMismatchException e)
    {
        var token = e.OffendingToken ?? recognizer.CurrentToken;
        string expected = e.GetExpectedTokens().ToString(recognizer.Vocabulary);
        var message = BuildMessage(recognizer, token, e, ErrorKind.InputMismatch, expected);
        recognizer.NotifyErrorListeners(token, message, e);
    }

    protected override void ReportMissingToken(Parser recognizer)
    {
        if (errorRecoveryMode) return;
        BeginErrorCondition(recognizer);

        var token = recognizer.CurrentToken;
        string expected = GetExpectedTokens(recognizer).ToString(recognizer.Vocabulary);
        var message = BuildMessage(recognizer, token, null, ErrorKind.MissingToken, expected);
        recognizer.NotifyErrorListeners(token, message, null);
    }

    protected override void ReportUnwantedToken(Parser recognizer)
    {
        if (errorRecoveryMode) return;
        BeginErrorCondition(recognizer);

        var token = recognizer.CurrentToken;
        string expected = GetExpectedTokens(recognizer).ToString(recognizer.Vocabulary);
        var message = BuildMessage(recognizer, token, null, ErrorKind.UnwantedToken, expected);
        recognizer.NotifyErrorListeners(token, message, null);
    }

    // ── Message construction ──

    private enum ErrorKind { NoViableAlternative, InputMismatch, MissingToken, UnwantedToken }

    private static string BuildMessage(
        Parser recognizer, IToken token, RecognitionException? e,
        ErrorKind kind, string? expectedTokens = null)
    {
        string baseMsg = kind switch
        {
            ErrorKind.NoViableAlternative => $"cannot parse construct near '{Truncate(token.Text, 40)}'",
            ErrorKind.InputMismatch => $"unexpected '{Truncate(token.Text, 40)}'",
            ErrorKind.MissingToken => $"missing token before '{Truncate(token.Text, 40)}'",
            ErrorKind.UnwantedToken => $"unexpected '{Truncate(token.Text, 40)}'",
            _ => "syntax error"
        };

        var hints = GuessCobolIntent(recognizer, token, expectedTokens);

        if (hints.Count == 0)
            return baseMsg;

        // Deduplicate by code prefix (first 8 chars), sort by priority, cap at 2
        var seen = new HashSet<string>();
        var filtered = new List<DiagnosticHint>();
        foreach (var h in hints.OrderBy(h => h.Priority))
        {
            string prefix = h.Code.Length >= 8 ? h.Code[..8] : h.Code;
            if (seen.Add(prefix) && filtered.Count < 2)
                filtered.Add(h);
        }

        string code = filtered[0].Code;
        string hintText = string.Join(" ", filtered.Select(h => h.Message));
        return $"[{code}] {baseMsg}. {hintText}";
    }

    // ── COBOL-specific heuristics ──

    private static List<DiagnosticHint> GuessCobolIntent(
        Parser recognizer, IToken token, string? expectedTokens)
    {
        var hints = new List<DiagnosticHint>();
        var stream = (ITokenStream)recognizer.InputStream;
        var prev = GetToken(stream, token.TokenIndex - 1);
        var ruleStack = recognizer.GetRuleInvocationStack().ToArray();
        string tokenUpper = token.Text?.ToUpperInvariant() ?? "";

        // 0. Vendor JSON/XML statements are NOT ISO/IEC 1989 constructs (0 spec occurrences; owner decision 2,
        // DEVLOG 581): the hard-reserved JSON/XML lexer tokens can only surface here as a misplaced vendor statement
        // behind a generic parse error → the vendor-extension hint (COBOL0313, priority 0 so its code wins the prefix),
        // never an edition gate. Every ISO reservation-word introduction gate now fires at BIND (superset parse +
        // bind-time ConstructRegistry.Check → the VersionConformancePass), so the former ReservedWordEditionHints
        // reverse-signature recogniser is gone (residue migration complete, DESIGN-version-conformance-pipeline.md).
        if (token.Type is CobolLexer.JSON or CobolLexer.XML)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0313,
                $"{(token.Type == CobolLexer.JSON ? "JSON" : "XML")} GENERATE/PARSE is not an ISO/IEC 1989 construct — "
                + "vendor-dialect extension, deferred (owner decision 2, DEVLOG 581).", 0));

        // ⛔ 0a. THE REQUIRED IMPERATIVE-STATEMENT ARM — ONE ARM FOR EVERY GENERAL FORMAT (kb/Work PB396).
        // Nine phrase rules carry an imperative-statement operand that the printed general format leaves
        // UNBRACKETED or stacks inside BRACES (IF's THEN and ELSE arms, EVALUATE's WHEN clause and its WHEN
        // OTHER tail, PERFORM's inline body and its WHEN / WHEN OTHER / WHEN COMMON / FINALLY bodies, SEARCH's
        // and SEARCH ALL's WHEN bodies), and every one of them is now spelled `statementBlock` — a rule that
        // cannot match empty. So the violation always arrives in the SAME shape: a parse position whose
        // expected set admits everything that can START a statement block, reached with a token that can start
        // none of them. That test is computed FROM THE ATN, not from a table of verbs, so a general format
        // added tomorrow inherits the diagnostic without editing this method — which is the whole reason the
        // cardinality lives in the grammar rather than in nine bind-time checks.
        if (IsEmptyRequiredStatementBlock(recognizer, token)
            && EnclosingStatementName(ruleStack) is { } stmtName)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOLNET2072,
                $"The {stmtName} statement's general format shows an imperative-statement here that is not "
                + "bracketed, so at least one statement shall be specified: brackets are what license an "
                + "omission (ISO §5.2.6.2), and one alternative of a brace group shall be explicitly specified "
                + "(ISO §5.2.6.3).", 0));

        // ⛔ 0b. THE WHEN OTHER PHRASE — BOTH OF ITS RULES, SEPARATED BY LOOKAHEAD (kb/Work PB396).
        // `[ WHEN OTHER imperative-statement-2 ]` follows the `{ … } …` repetition in EVALUATE's format, and
        // `[ WHEN OTHER EXCEPTION imperative-statement-3 ]` follows the WHEN group in PERFORM Format 3: ONE
        // bracketed phrase, LAST, whose imperative-statement is inside the bracket and therefore required once
        // the phrase is written. A syntax error at the phrase can only be one of those two rules, and which one
        // is decided by what follows OTHER — a token that can START a statement means the body is there and the
        // PHRASE is misplaced or repeated; anything else means the phrase is where it belongs and its
        // imperative-statement is missing. The parser reports at WHEN (the clause-repetition decision) or at
        // OTHER (a WHEN group cannot spell it), so both spellings are recognised here.
        int otherIndex = token.Type == CobolLexer.OTHER ? token.TokenIndex
            : token.Type == CobolLexer.WHEN && GetToken(stream, token.TokenIndex + 1)?.Type == CobolLexer.OTHER
                ? token.TokenIndex + 1
                : -1;
        if (otherIndex >= 0)
        {
            // PERFORM Format 3 prints `WHEN OTHER EXCEPTION imperative-statement-3` with EXCEPTION NOT
            // underlined — an optional word (§8.3.2.4.3), so it is never the body.
            var afterOther = GetToken(stream, otherIndex + 1);
            if (afterOther?.Type == CobolLexer.EXCEPTION) afterOther = GetToken(stream, otherIndex + 2);
            bool bodyPresent = afterOther is not null
                && StatementBlockStart(recognizer).Contains(afterOther.Type);
            hints.Add(bodyPresent
                ? new(Diagnostics.DiagnosticDescriptors.COBOLNET2073,
                    "A WHEN OTHER phrase may be specified at most once, and only after every WHEN phrase: the "
                    + "general format brackets it as a single phrase FOLLOWING the repeated WHEN group, and an "
                    + "ellipsis applies only to the portion between the delimiters immediately to its left "
                    + "(ISO §14.9.13.2 / §14.9.28.2 Format 3, with ISO §5.2.7).", 0)
                : new(Diagnostics.DiagnosticDescriptors.COBOLNET2072,
                    "The WHEN OTHER phrase carries an imperative-statement INSIDE its bracket, so writing the "
                    + "phrase obliges the statement: brackets license omitting the whole phrase, never emptying "
                    + "it (ISO §5.2.6.2, with §14.9.13.2 / §14.9.28.2 Format 3).", 0));
        }

        // ⛔ 0c. A WRITTEN SHAPE NO GENERAL FORMAT OF ITS CLAUSE PRINTS (kb/Work PB412, PB421). The grammar
        // rules for GO TO and MOVE each now carry ONE ALTERNATIVE PER PRINTED FORMAT, so their complement — the
        // shapes the old optional-everything union rules admitted — arrives here as a syntax error instead of
        // reaching a bind arm that discarded operands or emitted a loud stage. Both hints are read from the
        // PARSER STATE, not from a catalogue of bad source: the GO TO one asks the ONE format classifier what
        // the partially-built statement context actually holds, and the MOVE one is the position of a
        // statement-selecting keyword inside the rule that cannot spell it.
        if (GoToComplementMessage(recognizer, token, stream) is { } goToMsg)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOLNET2172, goToMsg, 0));

        if (token.Type is CobolLexer.CORRESPONDING or CobolLexer.CORR && IsInRule(ruleStack, "moveStatement"))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOLNET2173,
                "Both general formats of the MOVE statement put the whole sending specification directly after "
                + "the verb — Format 1 is 'MOVE { identifier-1 | literal-1 } TO { identifier-2 } …' and Format 2 "
                + "is 'MOVE { CORRESPONDING | CORR } identifier-3 TO identifier-4' — so no format admits a "
                + "CORRESPONDING phrase after a sending operand (ISO §14.9.25.2).", 0));

        // 1. Missing space before string literal
        if (token.Text?.StartsWith('"') == true && prev != null && IsIdentifier(prev))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0301, "Missing space before string literal.", 20));

        // 2. Missing space after string literal
        if (prev?.Text?.EndsWith('"') == true && IsIdentifier(token))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0302, "Missing space after string literal.", 20));

        // 3. Missing TO in MOVE statement
        if (IsInRule(ruleStack, "moveStatement") && IsIdentifier(token) && prev != null && IsLiteral(prev))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0303, "In a MOVE statement, did you forget TO before the target?", 10));

        // 4. Missing period after paragraph name (shows as qualified name)
        if (token.Text?.Contains(".MOVE", StringComparison.OrdinalIgnoreCase) == true ||
            token.Text?.Contains(".ADD", StringComparison.OrdinalIgnoreCase) == true ||
            token.Text?.Contains(".SUBTRACT", StringComparison.OrdinalIgnoreCase) == true ||
            token.Text?.Contains(".PERFORM", StringComparison.OrdinalIgnoreCase) == true ||
            token.Text?.Contains(".IF", StringComparison.OrdinalIgnoreCase) == true)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0304, "Missing period after paragraph name — the parser is treating it as a qualified reference.", 5));

        // 5. STATUS where IDENTIFIER expected
        if (tokenUpper == "STATUS" && expectedTokens?.Contains("IDENTIFIER") == true)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0200, "STATUS is a reserved word here. For file status, use 'FILE STATUS IS <data-name>'.", 10));

        // 6. PROGRAM where IDENTIFIER expected
        if (tokenUpper == "PROGRAM" && expectedTokens?.Contains("IDENTIFIER") == true)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0201, "PROGRAM is a reserved word. If this is a paragraph name, it cannot be named PROGRAM.", 10));

        // 7. Unrecognized keyword in SPECIAL-NAMES
        if (IsInRule(ruleStack, "specialNamesParagraph") && !IsIdentifier(token) && !IsLiteral(token))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0305, "Unexpected token in SPECIAL-NAMES. Check implementor-name or mnemonic-name syntax.", 15));

        // 8. ASCENDING/DESCENDING KEY not parsed in OCCURS
        if ((tokenUpper == "ASCENDING" || tokenUpper == "DESCENDING") &&
            expectedTokens?.Contains("'.'") == true)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0100, "ASCENDING/DESCENDING KEY clause in OCCURS is not yet supported. Table created without sort key.", 5));

        // 9. BLANK WHEN ZERO as separate tokens
        if (tokenUpper == "BLANK" && expectedTokens?.Contains("'.'") == true)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0101, "BLANK WHEN ZERO may not be recognized. Check that it appears as a single clause on the data item.", 15));

        // 10. SET statement forms
        if (tokenUpper == "SET" && IsInRule(ruleStack, "procedureDivision"))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0102, "This SET form may not be supported. Supported forms: SET identifier TO value, SET condition TO TRUE/FALSE, SET index UP/DOWN BY integer.", 15));

        // 11. SEARCH statement
        if (tokenUpper == "SEARCH" && IsInRule(ruleStack, "procedureDivision"))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0103, "SEARCH statement may not be fully supported.", 15));

        // 12. THROUGH/THRU in unexpected context
        if ((tokenUpper == "THROUGH" || tokenUpper == "THRU") && expectedTokens?.Contains("'.'") == true)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0300, "THROUGH/THRU is not recognized in this context. Check PERFORM or VALUE THROUGH syntax.", 10));

        // 13. Misplaced END-xxx terminators
        if (tokenUpper.StartsWith("END-"))
        {
            string stmt = tokenUpper[4..]; // "IF", "PERFORM", etc.
            if (!IsInMatchingRule(ruleStack, stmt))
                hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0306, $"{tokenUpper} appears without a matching {stmt} statement.", 5));
        }

        // 14. Missing period at end of sentence — a GUESS, and the lowest-priority one here, so it is suppressed
        // once a STRUCTURAL diagnosis has already named what the general format requires at this position
        // (priority 0). Otherwise `GO TO DEPENDING ON X.` read "…at least one procedure-name is required. A
        // period may be missing at the end of the previous sentence." — the second sentence contradicting the
        // first (kb/Work PB412).
        if (expectedTokens?.Contains("'.'") == true && !tokenUpper.StartsWith("END-")
            && hints.TrueForAll(h => h.Priority > 0))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0307, "A period may be missing at the end of the previous sentence.", 25));

        // 15. Literal where identifier expected
        if (IsLiteral(token) && expectedTokens?.Contains("IDENTIFIER") == true)
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0308, "A data-name is expected here, not a literal.", 20));

        // 16. Identifier where literal expected
        if (IsIdentifier(token) &&
            (expectedTokens?.Contains("STRINGLIT") == true || expectedTokens?.Contains("INTEGERLIT") == true))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0309, "A literal value is expected here, not a data-name.", 20));

        // 17. Missing BY in INDEXED BY
        if (tokenUpper != "BY" && expectedTokens?.Contains("'BY'") == true &&
            IsInRule(ruleStack, "dataDescriptionEntry"))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0310, "Missing BY keyword. INDEXED BY requires 'INDEXED BY <index-name>'.", 10));

        // 18. Complex EVALUATE forms
        if (tokenUpper == "ALSO" && IsInRule(ruleStack, "evaluateStatement"))
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0107, "EVALUATE with ALSO (multi-subject) may not be fully supported.", 10));

        // 19. FILE CONTROL / SELECT context errors
        if (IsInRule(ruleStack, "fileControlParagraph") &&
            !IsIdentifier(token) && !IsLiteral(token) &&
            tokenUpper != "SELECT" && tokenUpper != "ASSIGN" && tokenUpper != "FILE_CONTROL")
            hints.Add(new(Diagnostics.DiagnosticDescriptors.COBOL0312,
                "Unexpected token in FILE-CONTROL paragraph. Check SELECT/ASSIGN TO syntax.",
                10));

        return hints;
    }

    // ── The GO TO general-format complement (kb/Work PB412) ──

    /// <summary>The §14.9.17.2 message for a GO TO written in a shape NEITHER general format prints, or null.
    /// <para>Two parser states carry the two complement shapes, and each is read structurally:</para>
    /// <list type="bullet">
    ///   <item>The statement context is still on the rule-invocation stack — the parser committed to the
    ///         Format-2 alternative on seeing a LIST of procedure-names and then found no DEPENDING. The
    ///         half-built <c>GoToStatementContext</c> already holds the children matched so far, so the counts
    ///         handed to <see cref="GoToFormats.DiagnoseWrittenShape"/> are the ones actually written — no
    ///         re-scan of the token stream, and no second copy of the format rule.</item>
    ///   <item>The offending token IS <c>DEPENDING</c> immediately after <c>GO</c> or <c>GO TO</c> — the
    ///         target-less alternative matched and the statement ended, so the context has already been popped
    ///         and the shape has to be read off the stream. The <c>GO</c> anchor is what keeps this off an
    ///         OCCURS … DEPENDING ON, whose DEPENDING never follows a GO.</item>
    /// </list></summary>
    private static string? GoToComplementMessage(Parser recognizer, IToken token, ITokenStream stream)
    {
        for (RuleContext? c = recognizer.Context; c is not null; c = c.Parent)
            if (c is CobolParserCore.GoToStatementContext g)
                return GoToFormats.DiagnoseWrittenShape(g.procedureName().Length, g.DEPENDING() is not null);

        if (token.Type != CobolLexer.DEPENDING) return null;
        var prev = GetToken(stream, token.TokenIndex - 1);
        if (prev?.Type == CobolLexer.TO) prev = GetToken(stream, token.TokenIndex - 2);
        return prev?.Type == CobolLexer.GO ? GoToFormats.DiagnoseWrittenShape(0, hasDepending: true) : null;
    }

    // ── Parse-layer edition-gate rendering (rearch PHASE 02) ──

    /// <summary>Captures the single <see cref="EditionDiagnostic"/> a one-shot <see cref="ConstructRegistry.Check"/>
    /// reports, so the parse-layer error strategy can reuse the ONE funnel's exact COBOLNET0900 text (display /
    /// introduction edition / ISO citation all sourced from the construct's registry row, not hand-copied).</summary>
    private sealed class CaptureSink : IDiagnosticSink
    {
        public string? Message { get; private set; }
        public void Report(in EditionDiagnostic d) => Message = d.Message;
    }

    // ── The required-imperative test (kb/Work PB396) ──

    /// <summary>FIRST(<c>statementBlock</c>) — every token that can begin the grammar's imperative-statement
    /// operand — read ONCE from the ATN. Cached in a static because the ATN is a static of the generated parser;
    /// a benign race recomputes the same set.</summary>
    private static IntervalSet? _statementBlockStart;

    private static IntervalSet StatementBlockStart(Parser recognizer)
        => _statementBlockStart ??= recognizer.Atn.NextTokens(
               recognizer.Atn.ruleToStartState[CobolParserCore.RULE_statementBlock]);

    /// <summary>True when the parser failed AT a position where a statement block was required and had matched
    /// nothing: the expected set admits every token that can start one, and the offending token starts none.
    /// <para>The "expected ⊇ FIRST(statementBlock)" direction is what makes this specific — at a position where
    /// the block is one of several continuations the parse does not fail at all, and at a position inside a
    /// half-written statement (<c>MOVE TO Y</c>) the expected set is that statement's operand set, not the
    /// statement-start set.</para></summary>
    private static bool IsEmptyRequiredStatementBlock(Parser recognizer, IToken token)
    {
        var start = StatementBlockStart(recognizer);
        if (start.Contains(token.Type)) return false;          // the token CAN start a statement — a later error
        var expected = recognizer.GetExpectedTokens();
        if (expected is null) return false;
        foreach (int t in start.ToIntegerList())
            if (!expected.Contains(t)) return false;
        return true;
    }

    /// <summary>The enclosing statement's COBOL name, taken from the rule invocation stack and spelled from the
    /// RULE NAME rather than a table: <c>ifStatement</c> → IF, <c>searchAllStatement</c> → SEARCH ALL. Null when
    /// the failure is not inside a statement at all (a bad token at paragraph level), which is what keeps the
    /// required-imperative arm from firing outside a general format's operand.</summary>
    private static string? EnclosingStatementName(string[] ruleStack)
    {
        const string suffix = "Statement";
        foreach (string rule in ruleStack)
            if (rule.Length > suffix.Length && rule.EndsWith(suffix, StringComparison.Ordinal))
                return SplitCamelCase(rule[..^suffix.Length]).ToUpperInvariant();
        return null;
    }

    private static string SplitCamelCase(string name)
    {
        var sb = new System.Text.StringBuilder(name.Length + 4);
        foreach (char c in name)
        {
            if (char.IsUpper(c) && sb.Length > 0) sb.Append(' ');
            sb.Append(c);
        }
        return sb.ToString();
    }

    // ── Helpers ──

    private static IToken? GetToken(ITokenStream stream, int index)
        => index >= 0 && index < stream.Size ? stream.Get(index) : null;

    private static bool IsIdentifier(IToken token)
        => token.Type == CobolLexer.IDENTIFIER;

    private static bool IsLiteral(IToken token)
        => token.Type is CobolLexer.STRINGLIT or CobolLexer.INTEGERLIT or CobolLexer.DECIMALLIT;

    private static bool IsInRule(string[] ruleStack, string ruleName)
        => ruleStack.Any(r => string.Equals(r, ruleName, StringComparison.OrdinalIgnoreCase));

    private static bool IsInMatchingRule(string[] ruleStack, string stmtName)
    {
        string ruleName = stmtName.ToLowerInvariant() + "Statement";
        return ruleStack.Any(r => r.Equals(ruleName, StringComparison.OrdinalIgnoreCase));
    }

    private static string Truncate(string? text, int max)
    {
        if (text == null) return "<EOF>";
        return text.Length <= max ? text : text[..max] + "...";
    }
}
