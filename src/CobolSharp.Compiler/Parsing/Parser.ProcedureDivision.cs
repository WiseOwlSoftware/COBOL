using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Lexing;

namespace CobolSharp.Compiler.Parsing;

public sealed partial class Parser
{
    // ═══════════════════════════════════════════════════
    // PROCEDURE DIVISION (§14)
    // ═══════════════════════════════════════════════════

    private ProcedureDivision ParseProcedureDivision()
    {
        int start = Current.Span.Start;

        Expect(TokenKind.ProcedureKeyword);
        Expect(TokenKind.DivisionKeyword);

        // Skip optional USING/RETURNING clause before the period
        if (Check(TokenKind.UsingKeyword))
        {
            Advance(); // USING
            while (Check(TokenKind.Identifier) || Check(TokenKind.ByKeyword) ||
                   Check(TokenKind.ReferenceKeyword) || Check(TokenKind.ValueKeyword) ||
                   Check(TokenKind.ContentKeyword))
                Advance();
        }
        if (Check(TokenKind.ReturningKeyword))
        {
            Advance(); // RETURNING
            if (Check(TokenKind.Identifier)) Advance();
        }

        Expect(TokenKind.Period);

        // Handle DECLARATIVES section
        if (Check(TokenKind.Identifier) && Current.Text.Equals("DECLARATIVES", StringComparison.OrdinalIgnoreCase))
        {
            Advance(); // DECLARATIVES
            Match(TokenKind.Period);
            // Skip everything until END DECLARATIVES
            while (Current.Kind != TokenKind.EndOfFile)
            {
                if (Check(TokenKind.EndKeyword))
                {
                    int saved = _position;
                    Advance(); // END
                    if (Check(TokenKind.Identifier) && Current.Text.Equals("DECLARATIVES", StringComparison.OrdinalIgnoreCase))
                    {
                        Advance(); // DECLARATIVES
                        Match(TokenKind.Period);
                        break;
                    }
                    _position = saved;
                }
                Advance();
            }
        }

        // Parse statements before the first paragraph/section (if any)
        var initialStatements = new List<Statement>();
        var paragraphs = new List<Paragraph>();
        var sections = new List<Section>();

        // Issue 12: Use IsDivisionStart() to avoid false-stopping on reserved words
        while (Current.Kind != TokenKind.EndOfFile && !IsAtProcedureDivisionEnd())
        {
            if (IsSectionHeader())
                break;
            if (IsParagraphHeader())
                break;
            if (Check(TokenKind.Period))
            {
                Advance();
                continue;
            }
            if (IsStatementStart(Current.Kind))
            {
                var sentenceStmts = ParseSentence();
                initialStatements.AddRange(sentenceStmts);
            }
            else
            {
                // Unknown token before first paragraph/section
                Advance();
            }
        }

        // Parse sections and/or standalone paragraphs
        while (Current.Kind != TokenKind.EndOfFile)
        {
            if (IsSectionHeader())
            {
                sections.Add(ParseSection());
                // Paragraphs within a section are added to the flat list too
                if (sections.Count > 0)
                {
                    foreach (var p in sections[^1].Paragraphs)
                        paragraphs.Add(p);
                }
            }
            else if (IsParagraphHeader())
            {
                paragraphs.Add(ParseParagraph(null));
            }
            else
            {
                break;
            }
        }

        int end = Current.Span.Start;
        return new ProcedureDivision(initialStatements, paragraphs, sections,
            TextSpan.FromBounds(start, end));
    }

    private bool IsParagraphHeader()
    {
        // Issue 22 (§6.3): Paragraph names can be identifiers or certain keyword tokens
        // that serve as user-defined names in context
        if (Current.Kind == TokenKind.Identifier)
            return Peek().Kind == TokenKind.Period;
        // Allow keyword tokens as paragraph names when followed by period
        // (context-dependent: reserved words used as paragraph names in legacy code)
        if (IsUserDefinableKeyword(Current.Kind) && Peek().Kind == TokenKind.Period)
            return true;
        return false;
    }

    private bool IsSectionHeader()
    {
        // Issue 21 (§6.3): Section names can be identifiers or certain keyword tokens
        if (Current.Kind == TokenKind.Identifier)
            return Peek().Kind == TokenKind.SectionKeyword;
        if (IsUserDefinableKeyword(Current.Kind) && Peek().Kind == TokenKind.SectionKeyword)
            return true;
        return false;
    }

    /// <summary>
    /// Issues 21-22: Keywords that can serve as user-defined paragraph/section names
    /// in legacy COBOL code. These are keywords that appear in statements but may
    /// also be used as procedure names.
    /// </summary>
    private static bool IsUserDefinableKeyword(TokenKind kind) => kind is
        TokenKind.InputKeyword or TokenKind.OutputKeyword or TokenKind.ExtendKeyword or
        TokenKind.ErrorKeyword or TokenKind.StatusKeyword or TokenKind.FileKeyword or
        TokenKind.RecordKeyword or TokenKind.LineKeyword or TokenKind.SequentialKeyword or
        TokenKind.DynamicKeyword or TokenKind.RandomKeyword;

    private Section ParseSection()
    {
        int start = Current.Span.Start;
        string name = Advance().Text; // section name
        Expect(TokenKind.SectionKeyword);
        // Optional segment/priority number (archaic feature)
        if (Check(TokenKind.IntegerLiteral))
            Advance();
        Expect(TokenKind.Period);

        var paragraphs = new List<Paragraph>();
        // Issue 11: Use IsDivisionStart() to avoid false-stopping on reserved words
        while (Current.Kind != TokenKind.EndOfFile && !IsSectionHeader() &&
               !IsAtProcedureDivisionEnd())
        {
            if (IsParagraphHeader())
            {
                paragraphs.Add(ParseParagraph(name));
            }
            else
            {
                break;
            }
        }

        int end = Current.Span.Start;
        return new Section(name, paragraphs, TextSpan.FromBounds(start, end));
    }

    /// <summary>
    /// Parse a sentence: a sequence of statements terminated by a period.
    /// This is the ONLY place periods are consumed in the procedure division.
    /// </summary>
    private List<Statement> ParseSentence()
    {
        var statements = new List<Statement>();

        while (Current.Kind != TokenKind.EndOfFile && !Check(TokenKind.Period))
        {
            // Stop at structural boundaries (paragraph/section/division headers)
            if (IsParagraphHeader() || IsSectionHeader() || IsAtProcedureDivisionEnd())
                break;

            // Per §8.3.5: comma-space is a separator equivalent to space — skip it
            if (Check(TokenKind.Comma))
            {
                Advance();
                continue;
            }

            if (IsStatementStart(Current.Kind))
            {
                var stmt = ParseStatement();
                if (stmt != null)
                    statements.Add(stmt);
            }
            else
            {
                // Error recovery: unknown token in sentence
                ReportError("CS0200", $"Unexpected token '{Current.Text}' — expected a statement");
                Advance(); // ensure progress
                SkipToPeriodOrKeyword();
            }
        }

        // SENTENCE OWNS THE PERIOD — consume it
        if (Check(TokenKind.Period))
            Advance();

        return statements;
    }

    private Paragraph ParseParagraph(string? sectionName)
    {
        int start = Current.Span.Start;
        string name = Advance().Text;
        Expect(TokenKind.Period);

        var statements = new List<Statement>();
        // Issue 10: Use IsDivisionStart() to avoid false-stopping on reserved words
        while (Current.Kind != TokenKind.EndOfFile &&
               !IsParagraphHeader() && !IsSectionHeader() &&
               !IsAtProcedureDivisionEnd())
        {
            // Skip stray periods (empty sentences)
            if (Check(TokenKind.Period))
            {
                Advance();
                continue;
            }

            // If we see a statement start, parse a sentence
            if (IsStatementStart(Current.Kind))
            {
                var sentenceStmts = ParseSentence();
                statements.AddRange(sentenceStmts);
            }
            else
            {
                // Unknown token — try to recover
                ReportError("CS0200", $"Unexpected token '{Current.Text}' in paragraph");
                Advance();
            }
        }

        int end = Current.Span.Start;
        return new Paragraph(name, statements, TextSpan.FromBounds(start, end), sectionName);
    }

    /// <summary>
    /// Parse imperative statements inside compound statement bodies (IF then/else,
    /// PERFORM body, EVALUATE when, etc.). Returns at period WITHOUT consuming it.
    /// Returns at explicit terminators WITHOUT consuming. Returns at mismatched
    /// scope terminators WITHOUT consuming.
    /// </summary>
    private List<Statement> ParseImperativeStatements(params TokenKind[] terminators)
    {
        var statements = new List<Statement>();

        while (Current.Kind != TokenKind.EndOfFile)
        {
            // Check explicit terminators (END-IF, ELSE, WHEN, etc.)
            foreach (var t in terminators)
                if (Check(t)) return statements;

            // Period terminates ALL open scopes — return WITHOUT consuming
            if (Check(TokenKind.Period))
                return statements;

            // Scope terminator not in our list = mismatched nesting — return without consuming
            if (IsScopeTerminator(Current.Kind))
                return statements;

            if (IsStatementStart(Current.Kind))
            {
                var stmt = ParseStatement();
                if (stmt != null)
                    statements.Add(stmt);
            }
            else
            {
                // Error recovery: unknown token in imperative statement context
                ReportError("CS0200", $"Unexpected token '{Current.Text}' — expected a statement");
                Advance(); // ensure progress
            }
        }

        return statements;
    }

    private Statement? ParseStatement()
    {
        return Current.Kind switch
        {
            TokenKind.DisplayKeyword => ParseDisplayStatement(),
            TokenKind.StopKeyword => ParseStopStatement(),
            TokenKind.MoveKeyword => ParseMoveStatement(),
            TokenKind.AddKeyword => ParseAddStatement(),
            TokenKind.SubtractKeyword => ParseSubtractStatement(),
            TokenKind.ComputeKeyword => ParseComputeStatement(),
            TokenKind.IfKeyword => ParseIfStatement(),
            TokenKind.PerformKeyword => ParsePerformStatement(),
            TokenKind.GoKeyword => ParseGoToStatement(),
            TokenKind.AlterKeyword => ParseAlterStatement(),
            TokenKind.ContinueKeyword => ParseContinueStatement(),
            TokenKind.ExitKeyword => ParseExitStatement(),
            TokenKind.AcceptKeyword => ParseAcceptStatement(),
            TokenKind.InitializeKeyword => ParseInitializeStatement(),
            TokenKind.OpenKeyword => ParseOpenStatement(),
            TokenKind.CloseKeyword => ParseCloseStatement(),
            TokenKind.ReadKeyword => ParseReadStatement(),
            TokenKind.WriteKeyword => ParseWriteStatement(),
            TokenKind.RewriteKeyword => ParseRewriteStatement(),
            TokenKind.DeleteKeyword => ParseDeleteStatement(),
            TokenKind.StartKeyword => ParseStartStatement(),
            TokenKind.SortKeyword => ParseSortStatement(),
            TokenKind.CallKeyword => ParseCallStatement(),
            TokenKind.CancelKeyword => ParseCancelStatement(),
            TokenKind.StringKeyword => ParseStringStatement(),
            TokenKind.UnstringKeyword => ParseUnstringStatement(),
            TokenKind.InspectKeyword => ParseInspectStatement(),
            TokenKind.MultiplyKeyword => ParseMultiplyStatement(),
            TokenKind.DivideKeyword => ParseDivideStatement(),
            TokenKind.EvaluateKeyword => ParseEvaluateStatement(),
            TokenKind.SetKeyword => ParseSetStatement(),
            TokenKind.SearchKeyword => ParseSearchStatement(),
            TokenKind.GobackKeyword => ParseGobackStatement(),
            TokenKind.NextKeyword => ParseNextSentenceStatement(),
            TokenKind.ReturnKeyword => ParseReturnSortStatement(),
            TokenKind.ReleaseKeyword => ParseReleaseSortStatement(),
            // Phase 5.2 — Report Writer
            TokenKind.InitiateKeyword => ParseInitiateStatement(),
            TokenKind.GenerateKeyword => ParseGenerateStatement(),
            TokenKind.TerminateKeyword => ParseTerminateStatement(),
            // Phase 5.4 — OO COBOL
            TokenKind.InvokeKeyword => ParseInvokeStatement(),
            // Phase 5.5 — Exception handling
            TokenKind.RaiseKeyword => ParseRaiseStatement(),
            TokenKind.ResumeKeyword => ParseResumeStatement(),
            // Phase 5.6-5.10 — Compiler directive
            TokenKind.CompilerDirective => ParseCompilerDirective(),
            _ => HandleUnknownStatement()
        };
    }

    private Statement? HandleUnknownStatement()
    {
        ReportError("CS0200", $"Unexpected token '{Current.Text}' — expected a statement");
        // MUST advance at least one token to prevent infinite loops
        Advance();
        SkipToPeriodOrKeyword();
        return null;
    }
}
