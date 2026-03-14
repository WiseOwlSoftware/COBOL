using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Lexing;

namespace CobolSharp.Compiler.Parsing;

public sealed partial class Parser
{
    /// <summary>
    /// Issues 39-40: Parse REPLACING phrases: {ALL|LEADING|FIRST|CHARACTERS} pattern BY replacement ...
    /// </summary>
    private void ParseInspectReplacingPhrases(ref InspectType inspectKind,
        ref Expression? searchFor, ref Expression? replaceWith)
    {
        // Loop for multiple replacing phrases
        bool first = true;
        while (!Check(TokenKind.Period) && Current.Kind != TokenKind.EndOfFile &&
               !IsStatementStart(Current.Kind) && !IsScopeTerminator(Current.Kind))
        {
            if (Check(TokenKind.Identifier) && Current.Text.Equals("CHARACTERS", StringComparison.OrdinalIgnoreCase))
            {
                Advance(); // CHARACTERS
                inspectKind = InspectType.ReplacingAll;
                Match(TokenKind.ByKeyword);
                var rep = ParseExpression();
                if (first) { replaceWith = rep; first = false; }
            }
            else if (Check(TokenKind.AllKeyword))
            {
                Advance(); inspectKind = InspectType.ReplacingAll;
                var pat = ParseExpression();
                if (first) { searchFor = pat; first = false; }
                Match(TokenKind.ByKeyword);
                var rep = ParseExpression();
                if (replaceWith == null) replaceWith = rep;
            }
            else if (Check(TokenKind.Identifier) && Current.Text.Equals("LEADING", StringComparison.OrdinalIgnoreCase))
            {
                Advance(); inspectKind = InspectType.ReplacingLeading;
                var pat = ParseExpression();
                if (first) { searchFor = pat; first = false; }
                Match(TokenKind.ByKeyword);
                var rep = ParseExpression();
                if (replaceWith == null) replaceWith = rep;
            }
            else if (Check(TokenKind.Identifier) && Current.Text.Equals("FIRST", StringComparison.OrdinalIgnoreCase))
            {
                Advance(); inspectKind = InspectType.ReplacingFirst;
                var pat = ParseExpression();
                if (first) { searchFor = pat; first = false; }
                Match(TokenKind.ByKeyword);
                var rep = ParseExpression();
                if (replaceWith == null) replaceWith = rep;
            }
            else
            {
                break;
            }

            ConsumeBeforeAfterPhrases();

            // Check for additional ALL/LEADING/FIRST phrases
            if (!Check(TokenKind.AllKeyword) &&
                !(Check(TokenKind.Identifier) && (
                    Current.Text.Equals("LEADING", StringComparison.OrdinalIgnoreCase) ||
                    Current.Text.Equals("FIRST", StringComparison.OrdinalIgnoreCase) ||
                    Current.Text.Equals("CHARACTERS", StringComparison.OrdinalIgnoreCase))))
                break;
        }
    }

    /// <summary>
    /// Issues 39-40: Consume BEFORE/AFTER INITIAL phrases in INSPECT.
    /// </summary>
    private void ConsumeBeforeAfterPhrases()
    {
        while ((Check(TokenKind.BeforeKeyword) || Check(TokenKind.AfterKeyword)) &&
               !(Check(TokenKind.AfterKeyword) && Peek().Kind == TokenKind.AdvancingKeyword))
        {
            Advance(); // BEFORE or AFTER
            // Optional INITIAL keyword
            if (Check(TokenKind.Identifier) && Current.Text.Equals("INITIAL", StringComparison.OrdinalIgnoreCase))
                Advance();
            // The delimiter value
            if (!Check(TokenKind.Period) && !IsStatementStart(Current.Kind) &&
                !IsScopeTerminator(Current.Kind) && !Check(TokenKind.BeforeKeyword) &&
                !Check(TokenKind.AfterKeyword))
                ParseExpression();
        }
    }

    /// <summary>Skip remaining tokens in a statement to the period or next statement boundary.</summary>
    private void SkipToEndOfStatement()
    {
        while (!Check(TokenKind.Period) && Current.Kind != TokenKind.EndOfFile &&
               !IsStatementStart(Current.Kind) && !IsScopeTerminator(Current.Kind))
        {
            Advance();
        }
        // DO NOT consume period — it belongs to ParseSentence
    }

    /// <summary>
    /// Skip exception/error handler phrases: ON EXCEPTION, ON OVERFLOW,
    /// INVALID KEY, AT END, NOT ON EXCEPTION, NOT ON OVERFLOW, etc.
    /// These appear in CALL, STRING, UNSTRING, READ, WRITE, DELETE, START, REWRITE.
    /// </summary>
    private void SkipExceptionPhrases(TokenKind endScopeTerminator)
    {
        // Skip phrases like ON EXCEPTION, ON OVERFLOW, INVALID KEY, AT END-OF-PAGE
        while (Current.Kind != TokenKind.Period && Current.Kind != TokenKind.EndOfFile &&
               Current.Kind != endScopeTerminator)
        {
            // ON EXCEPTION / ON OVERFLOW
            if (Check(TokenKind.OnKeyword) &&
                (Peek().Kind == TokenKind.ErrorKeyword || Peek().Kind == TokenKind.SizeKeyword ||
                 (Peek().Kind == TokenKind.Identifier &&
                  (Peek().Text.Equals("EXCEPTION", StringComparison.OrdinalIgnoreCase) ||
                   Peek().Text.Equals("OVERFLOW", StringComparison.OrdinalIgnoreCase)))))
            {
                Advance(); Advance(); // ON + keyword
                // Skip imperative statements
                while (Current.Kind != TokenKind.Period && Current.Kind != TokenKind.EndOfFile &&
                       Current.Kind != endScopeTerminator && !Check(TokenKind.NotKeyword))
                {
                    if (IsStatementStart(Current.Kind))
                        ParseStatement(); // discard
                    else
                        Advance();
                }
                continue;
            }

            // NOT ON EXCEPTION / NOT ON OVERFLOW / NOT INVALID KEY / NOT AT END
            if (Check(TokenKind.NotKeyword))
            {
                int saved = _position;
                Advance(); // NOT
                if (Check(TokenKind.OnKeyword) || Check(TokenKind.InvalidKeyword) || Check(TokenKind.AtKeyword))
                {
                    Advance(); // ON/INVALID/AT
                    // Skip the rest of the phrase keyword(s)
                    while (Current.Kind != TokenKind.Period && Current.Kind != TokenKind.EndOfFile &&
                           Current.Kind != endScopeTerminator && !IsStatementStart(Current.Kind))
                        Advance();
                    // Skip imperative statements
                    while (Current.Kind != TokenKind.Period && Current.Kind != TokenKind.EndOfFile &&
                           Current.Kind != endScopeTerminator)
                    {
                        if (IsStatementStart(Current.Kind))
                            ParseStatement();
                        else
                            Advance();
                    }
                    continue;
                }
                _position = saved;
                break;
            }

            // INVALID KEY
            if (Check(TokenKind.InvalidKeyword))
            {
                Advance();
                Match(TokenKind.KeyKeyword);
                while (Current.Kind != TokenKind.Period && Current.Kind != TokenKind.EndOfFile &&
                       Current.Kind != endScopeTerminator && !Check(TokenKind.NotKeyword))
                {
                    if (IsStatementStart(Current.Kind))
                        ParseStatement();
                    else
                        Advance();
                }
                continue;
            }

            // AT END / AT END-OF-PAGE
            if (Check(TokenKind.AtKeyword))
            {
                Advance(); // AT
                if (Check(TokenKind.EndKeyword)) Advance();
                // Skip optional "-OF-PAGE" etc.
                while (Current.Kind != TokenKind.Period && Current.Kind != TokenKind.EndOfFile &&
                       Current.Kind != endScopeTerminator && !Check(TokenKind.NotKeyword) &&
                       !IsStatementStart(Current.Kind))
                    Advance();
                while (Current.Kind != TokenKind.Period && Current.Kind != TokenKind.EndOfFile &&
                       Current.Kind != endScopeTerminator && !Check(TokenKind.NotKeyword))
                {
                    if (IsStatementStart(Current.Kind))
                        ParseStatement();
                    else
                        Advance();
                }
                continue;
            }

            break;
        }
    }

    /// <summary>
    /// Issue 72 (§8.1): Consume ROUNDED [MODE IS {AWAY-FROM-ZERO|NEAREST-AWAY-FROM-ZERO|...}]
    /// </summary>
    private void ConsumeRoundedPhrase()
    {
        if (Match(TokenKind.RoundedKeyword))
        {
            // MODE IS mode-name
            if (Check(TokenKind.Identifier) && Current.Text.Equals("MODE", StringComparison.OrdinalIgnoreCase))
            {
                Advance(); // MODE
                Match(TokenKind.IsKeyword); // IS
                if (Check(TokenKind.Identifier)) Advance(); // mode-name
            }
        }
    }

    /// <summary>Skip ON SIZE ERROR / NOT ON SIZE ERROR phrases.</summary>
    private void SkipSizeErrorPhrases()
    {
        // ON SIZE ERROR imperative-statements
        if (Check(TokenKind.OnKeyword) && Peek().Kind == TokenKind.SizeKeyword)
        {
            Advance(); Advance(); // ON SIZE
            Match(TokenKind.ErrorKeyword);
            // Skip statements until NOT or END-xxx or period
            while (!Check(TokenKind.NotKeyword) && !Check(TokenKind.Period) &&
                   Current.Kind != TokenKind.EndOfFile && !IsScopeTerminator(Current.Kind))
            {
                var stmt = ParseStatement();
                // discard — we don't model size error handlers yet
            }
        }
        // NOT ON SIZE ERROR imperative-statements
        if (Check(TokenKind.NotKeyword))
        {
            int saved = _position;
            Advance(); // NOT
            if (Check(TokenKind.OnKeyword)) Advance();
            if (Check(TokenKind.SizeKeyword))
            {
                Advance(); // SIZE
                Match(TokenKind.ErrorKeyword);
                while (!Check(TokenKind.Period) && Current.Kind != TokenKind.EndOfFile &&
                       !IsScopeTerminator(Current.Kind))
                {
                    ParseStatement();
                }
            }
            else
            {
                _position = saved; // wasn't NOT ON SIZE ERROR, restore
            }
        }
    }
}
