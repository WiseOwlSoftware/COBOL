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
        return hints.Count > 0
            ? $"{baseMsg}. {string.Join(" ", hints)}"
            : baseMsg;
    }

    // ── COBOL-specific heuristics ──

    private static List<string> GuessCobolIntent(
        Parser recognizer, IToken token, string? expectedTokens)
    {
        var hints = new List<string>();
        var stream = (ITokenStream)recognizer.InputStream;
        var prev = GetToken(stream, token.TokenIndex - 1);
        var ruleStack = recognizer.GetRuleInvocationStack().ToArray();
        string tokenUpper = token.Text?.ToUpperInvariant() ?? "";

        // 1. Missing space before string literal
        if (token.Text?.StartsWith('"') == true && prev != null && IsIdentifier(prev))
            hints.Add("Missing space before string literal.");

        // 2. Missing space after string literal
        if (prev?.Text?.EndsWith('"') == true && IsIdentifier(token))
            hints.Add("Missing space after string literal.");

        // 3. Missing TO in MOVE statement
        if (IsInRule(ruleStack, "moveStatement") && IsIdentifier(token) && prev != null && IsLiteral(prev))
            hints.Add("In a MOVE statement, did you forget TO before the target?");

        // 4. Missing period after paragraph name (shows as qualified name)
        if (token.Text?.Contains(".MOVE", StringComparison.OrdinalIgnoreCase) == true ||
            token.Text?.Contains(".ADD", StringComparison.OrdinalIgnoreCase) == true ||
            token.Text?.Contains(".SUBTRACT", StringComparison.OrdinalIgnoreCase) == true ||
            token.Text?.Contains(".PERFORM", StringComparison.OrdinalIgnoreCase) == true ||
            token.Text?.Contains(".IF", StringComparison.OrdinalIgnoreCase) == true)
            hints.Add("Missing period after paragraph name — the parser is treating it as a qualified reference.");

        // 5. STATUS where IDENTIFIER expected
        if (tokenUpper == "STATUS" && expectedTokens?.Contains("IDENTIFIER") == true)
            hints.Add("STATUS is a reserved word here. For file status, use 'FILE STATUS IS <data-name>'.");

        // 6. PROGRAM where IDENTIFIER expected
        if (tokenUpper == "PROGRAM" && expectedTokens?.Contains("IDENTIFIER") == true)
            hints.Add("PROGRAM is a reserved word. If this is a paragraph name, it cannot be named PROGRAM.");

        // 7. Unrecognized keyword in SPECIAL-NAMES
        if (IsInRule(ruleStack, "specialNamesParagraph") && !IsIdentifier(token) && !IsLiteral(token))
            hints.Add("Unexpected token in SPECIAL-NAMES. Check implementor-name or mnemonic-name syntax.");

        // 8. ASCENDING/DESCENDING KEY not parsed in OCCURS
        if ((tokenUpper == "ASCENDING" || tokenUpper == "DESCENDING") &&
            expectedTokens?.Contains("'.'") == true)
            hints.Add("ASCENDING/DESCENDING KEY clause in OCCURS is not yet supported.");

        // 9. BLANK WHEN ZERO as separate tokens
        if (tokenUpper == "BLANK" && expectedTokens?.Contains("'.'") == true)
            hints.Add("BLANK WHEN ZERO may not be recognized. Check that it appears as a single clause on the data item.");

        // 10. SET statement forms
        if (tokenUpper == "SET" && IsInRule(ruleStack, "procedureDivision"))
            hints.Add("This SET form may not be supported. Supported forms: SET condition TO TRUE/FALSE.");

        // 11. SEARCH statement
        if (tokenUpper == "SEARCH" && IsInRule(ruleStack, "procedureDivision"))
            hints.Add("SEARCH statement may not be fully supported.");

        // 12. THROUGH/THRU in unexpected context
        if ((tokenUpper == "THROUGH" || tokenUpper == "THRU") && expectedTokens?.Contains("'.'") == true)
            hints.Add("THROUGH/THRU is not recognized in this context. Check PERFORM or VALUE THROUGH syntax.");

        // 13. Misplaced END-xxx terminators
        if (tokenUpper.StartsWith("END-"))
        {
            string stmt = tokenUpper[4..]; // "IF", "PERFORM", etc.
            if (!IsInMatchingRule(ruleStack, stmt))
                hints.Add($"{tokenUpper} appears without a matching {stmt} statement.");
        }

        // 14. Missing period at end of sentence
        if (expectedTokens?.Contains("'.'") == true && !tokenUpper.StartsWith("END-"))
            hints.Add("A period may be missing at the end of the previous sentence.");

        // 15. Literal where identifier expected
        if (IsLiteral(token) && expectedTokens?.Contains("IDENTIFIER") == true)
            hints.Add("A data-name is expected here, not a literal.");

        // 16. Identifier where literal expected
        if (IsIdentifier(token) &&
            (expectedTokens?.Contains("STRINGLIT") == true || expectedTokens?.Contains("INTEGERLIT") == true))
            hints.Add("A literal value is expected here, not a data-name.");

        // 17. Missing BY in INDEXED BY
        if (tokenUpper != "BY" && expectedTokens?.Contains("'BY'") == true &&
            IsInRule(ruleStack, "dataDescriptionEntry"))
            hints.Add("Missing BY keyword. INDEXED BY requires 'INDEXED BY <index-name>'.");

        // 18. DEPENDING ON not supported
        if (tokenUpper == "DEPENDING" || (tokenUpper == "ON" && prev?.Text?.Equals("DEPENDING", StringComparison.OrdinalIgnoreCase) == true))
            hints.Add("OCCURS DEPENDING ON (variable-length tables) is not yet supported.");

        // 19. CONVERTING in INSPECT
        if (tokenUpper == "CONVERTING" && IsInRule(ruleStack, "inspectStatement"))
            hints.Add("INSPECT CONVERTING is not yet supported.");

        // 20. REPLACING in INITIALIZE
        if (tokenUpper == "REPLACING" && IsInRule(ruleStack, "initializeStatement"))
            hints.Add("INITIALIZE REPLACING is not yet supported.");

        // 21. Complex EVALUATE forms
        if (tokenUpper == "ALSO" && IsInRule(ruleStack, "evaluateStatement"))
            hints.Add("EVALUATE with ALSO (multi-subject) may not be fully supported.");

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
