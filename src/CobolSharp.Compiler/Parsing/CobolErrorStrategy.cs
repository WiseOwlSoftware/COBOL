// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolSharp.Compiler.Generated;

namespace CobolSharp.Compiler.Parsing;

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
    private readonly record struct DiagnosticHint(string Code, string Message, int Priority);

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
        var next = GetToken(stream, token.TokenIndex + 1);
        var ruleStack = recognizer.GetRuleInvocationStack().ToArray();
        string tokenUpper = token.Text?.ToUpperInvariant() ?? "";
        string prevUpper = prev?.Text?.ToUpperInvariant() ?? "";

        // 1. Missing space before string literal
        if (token.Text?.StartsWith('"') == true && prev != null && IsIdentifier(prev))
            hints.Add(new("COBOL0301", "Missing space before string literal.", 20));

        // 2. Missing space after string literal
        if (prev?.Text?.EndsWith('"') == true && IsIdentifier(token))
            hints.Add(new("COBOL0302", "Missing space after string literal.", 20));

        // 3. Missing TO in MOVE statement
        if (IsInRule(ruleStack, "moveStatement") && IsIdentifier(token) && prev != null && IsLiteral(prev))
            hints.Add(new("COBOL0303", "In a MOVE statement, did you forget TO before the target?", 10));

        // 4. Missing period after paragraph name (shows as qualified name)
        if (token.Text?.Contains(".MOVE", StringComparison.OrdinalIgnoreCase) == true ||
            token.Text?.Contains(".ADD", StringComparison.OrdinalIgnoreCase) == true ||
            token.Text?.Contains(".SUBTRACT", StringComparison.OrdinalIgnoreCase) == true ||
            token.Text?.Contains(".PERFORM", StringComparison.OrdinalIgnoreCase) == true ||
            token.Text?.Contains(".IF", StringComparison.OrdinalIgnoreCase) == true)
            hints.Add(new("COBOL0304", "Missing period after paragraph name — the parser is treating it as a qualified reference.", 5));

        // 5. STATUS where IDENTIFIER expected
        if (tokenUpper == "STATUS" && expectedTokens?.Contains("IDENTIFIER") == true)
            hints.Add(new("COBOL0200", "STATUS is a reserved word here. For file status, use 'FILE STATUS IS <data-name>'.", 10));

        // 6. PROGRAM where IDENTIFIER expected
        if (tokenUpper == "PROGRAM" && expectedTokens?.Contains("IDENTIFIER") == true)
            hints.Add(new("COBOL0201", "PROGRAM is a reserved word. If this is a paragraph name, it cannot be named PROGRAM.", 10));

        // 7. Unrecognized keyword in SPECIAL-NAMES
        if (IsInRule(ruleStack, "specialNamesParagraph") && !IsIdentifier(token) && !IsLiteral(token))
            hints.Add(new("COBOL0305", "Unexpected token in SPECIAL-NAMES. Check implementor-name or mnemonic-name syntax.", 15));

        // 8. ASCENDING/DESCENDING KEY not parsed in OCCURS
        if ((tokenUpper == "ASCENDING" || tokenUpper == "DESCENDING") &&
            expectedTokens?.Contains("'.'") == true)
            hints.Add(new("COBOL0100", "ASCENDING/DESCENDING KEY clause in OCCURS is not yet supported. Table created without sort key.", 5));

        // 9. BLANK WHEN ZERO as separate tokens
        if (tokenUpper == "BLANK" && expectedTokens?.Contains("'.'") == true)
            hints.Add(new("COBOL0101", "BLANK WHEN ZERO may not be recognized. Check that it appears as a single clause on the data item.", 15));

        // 10. SET statement forms
        if (tokenUpper == "SET" && IsInRule(ruleStack, "procedureDivision"))
            hints.Add(new("COBOL0102", "This SET form may not be supported. Supported forms: SET identifier TO value, SET condition TO TRUE/FALSE, SET index UP/DOWN BY integer.", 15));

        // 11. SEARCH statement
        if (tokenUpper == "SEARCH" && IsInRule(ruleStack, "procedureDivision"))
            hints.Add(new("COBOL0103", "SEARCH statement may not be fully supported.", 15));

        // 12. THROUGH/THRU in unexpected context
        if ((tokenUpper == "THROUGH" || tokenUpper == "THRU") && expectedTokens?.Contains("'.'") == true)
            hints.Add(new("COBOL0300", "THROUGH/THRU is not recognized in this context. Check PERFORM or VALUE THROUGH syntax.", 10));

        // 13. Misplaced END-xxx terminators
        if (tokenUpper.StartsWith("END-"))
        {
            string stmt = tokenUpper[4..]; // "IF", "PERFORM", etc.
            if (!IsInMatchingRule(ruleStack, stmt))
                hints.Add(new("COBOL0306", $"{tokenUpper} appears without a matching {stmt} statement.", 5));
        }

        // 14. Missing period at end of sentence
        if (expectedTokens?.Contains("'.'") == true && !tokenUpper.StartsWith("END-"))
            hints.Add(new("COBOL0307", "A period may be missing at the end of the previous sentence.", 25));

        // 15. Literal where identifier expected
        if (IsLiteral(token) && expectedTokens?.Contains("IDENTIFIER") == true)
            hints.Add(new("COBOL0308", "A data-name is expected here, not a literal.", 20));

        // 16. Identifier where literal expected
        if (IsIdentifier(token) &&
            (expectedTokens?.Contains("STRINGLIT") == true || expectedTokens?.Contains("INTEGERLIT") == true))
            hints.Add(new("COBOL0309", "A literal value is expected here, not a data-name.", 20));

        // 17. Missing BY in INDEXED BY
        if (tokenUpper != "BY" && expectedTokens?.Contains("'BY'") == true &&
            IsInRule(ruleStack, "dataDescriptionEntry"))
            hints.Add(new("COBOL0310", "Missing BY keyword. INDEXED BY requires 'INDEXED BY <index-name>'.", 10));

        // 18. DEPENDING ON not supported
        if (tokenUpper == "DEPENDING" || (tokenUpper == "ON" && prevUpper == "DEPENDING"))
            hints.Add(new("COBOL0104", "OCCURS DEPENDING ON (variable-length tables) is not yet supported.", 5));

        // 19. CONVERTING in INSPECT
        if (tokenUpper == "CONVERTING" && IsInRule(ruleStack, "inspectStatement"))
            hints.Add(new("COBOL0105", "INSPECT CONVERTING is not yet supported.", 5));

        // 20. REPLACING in INITIALIZE
        if (tokenUpper == "REPLACING" && IsInRule(ruleStack, "initializeStatement"))
            hints.Add(new("COBOL0106", "INITIALIZE REPLACING is not yet supported.", 5));

        // 21. Complex EVALUATE forms
        if (tokenUpper == "ALSO" && IsInRule(ruleStack, "evaluateStatement"))
            hints.Add(new("COBOL0107", "EVALUATE with ALSO (multi-subject) may not be fully supported.", 10));

        // 22. NOT = / NOT > / NOT < abbreviated conditions
        // Parser chokes because relationalOperator has NOT EQUAL (word) but not NOT EQUALS (= symbol).
        // Detect both: offending token is '=' after NOT, or offending token is 'NOT' before '='.
        // No rule-stack check — the NOT + operator-symbol pattern is distinctive enough.
        if (token.Type is CobolLexer.EQUALS or CobolLexer.GT or CobolLexer.LT &&
            prevUpper == "NOT")
        {
            hints.Add(new("COBOL0311",
                $"NOT {token.Text} (abbreviated condition) is not yet supported. Use the word form instead: NOT EQUAL TO, NOT GREATER THAN, NOT LESS THAN.",
                3));
        }
        if (tokenUpper == "NOT" && next != null &&
            next.Type is CobolLexer.EQUALS or CobolLexer.GT or CobolLexer.LT)
        {
            string sym = next.Text ?? "=";
            hints.Add(new("COBOL0311",
                $"NOT {sym} (abbreviated condition) is not yet supported. Use the word form instead: NOT EQUAL TO, NOT GREATER THAN, NOT LESS THAN.",
                3));
        }

        // 23. Multi-target SET (multiple identifiers before TO/UP/DOWN)
        // The grammar allows only one identifier before TO/UP/DOWN.
        // NoViableAlternative doesn't provide expected tokens, so also check rule context.
        if (IsIdentifier(token) &&
            (IsInRule(ruleStack, "setToValueStatement") || IsInRule(ruleStack, "setIndexStatement") ||
             IsInRule(ruleStack, "setStatement")))
        {
            bool expectsTo = expectedTokens?.Contains("'TO'") == true ||
                             expectedTokens?.Contains("'UP'") == true ||
                             expectedTokens?.Contains("'DOWN'") == true;
            // Fire either when expected tokens include TO/UP/DOWN, or when we're in a SET
            // rule context (NoViableAlternative case, where expectedTokens is null)
            if (expectsTo || expectedTokens == null)
                hints.Add(new("COBOL0108",
                    "Multi-target SET (SET id1 id2 TO value) is not yet supported. Use separate SET statements for each target.",
                    5));
        }

        // 24. PERFORM VARYING with AFTER clause (detected when AFTER appears in perform context)
        if (tokenUpper == "AFTER" && IsInRule(ruleStack, "performStatement") &&
            !IsInRule(ruleStack, "inspectStatement"))
            hints.Add(new("COBOL0109",
                "PERFORM VARYING with AFTER clause (nested varying) is not yet supported.",
                5));

        // 25. FILE CONTROL / SELECT context errors
        if (IsInRule(ruleStack, "fileControlParagraph") &&
            !IsIdentifier(token) && !IsLiteral(token) &&
            tokenUpper != "SELECT" && tokenUpper != "ASSIGN" && tokenUpper != "FILE_CONTROL")
            hints.Add(new("COBOL0312",
                "Unexpected token in FILE-CONTROL paragraph. Check SELECT/ASSIGN TO syntax.",
                10));

        return hints;
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
