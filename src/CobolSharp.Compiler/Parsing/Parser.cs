using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Lexing;

namespace CobolSharp.Compiler.Parsing;

/// <summary>
/// Hand-written recursive descent parser for COBOL.
/// Phase 1 subset: IDENTIFICATION DIVISION, DATA DIVISION (WORKING-STORAGE),
/// PROCEDURE DIVISION with DISPLAY, STOP RUN, MOVE, ADD, SUBTRACT, COMPUTE, IF, PERFORM.
/// </summary>
public sealed partial class Parser
{
    private readonly List<Token> _tokens;
    private readonly DiagnosticBag _diagnostics;
    private readonly SourceText? _source;
    private int _position;
    private int _iterationCount;
    private const int MaxIterations = 1_000_000;

    public Parser(List<Token> tokens, DiagnosticBag diagnostics, SourceText? source = null)
    {
        _tokens = tokens;
        _diagnostics = diagnostics;
        _source = source;
        _position = 0;
    }

    // ═══════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════

    private Token Current => _position < _tokens.Count
        ? _tokens[_position]
        : _tokens[^1]; // EOF

    private Token Peek(int offset = 1)
    {
        int idx = _position + offset;
        return idx < _tokens.Count ? _tokens[idx] : _tokens[^1];
    }

    private Token Advance()
    {
        _iterationCount++;
        if (_iterationCount > MaxIterations)
            throw new InvalidOperationException($"Parser exceeded {MaxIterations} iterations — likely infinite loop at position {_position}, token '{Current.Text}')");
        var token = Current;
        if (_position < _tokens.Count - 1) _position++;
        return token;
    }

    private Token Expect(TokenKind kind, string? contextMessage = null)
    {
        if (Current.Kind == kind)
            return Advance();

        var msg = contextMessage ?? $"Expected {kind}, found {Current.Kind} '{Current.Text}'";
        ReportError("CS0100", msg);
        // Return a synthetic token to allow parsing to continue
        return new Token(kind, "", Current.Span);
    }

    private bool Match(TokenKind kind)
    {
        if (Current.Kind == kind)
        {
            Advance();
            return true;
        }
        return false;
    }

    private bool Check(TokenKind kind) => Current.Kind == kind;

    /// <summary>
    /// Checks if the current token is a valid level number (integer 1-49, 66, 77, or 88).
    /// Level numbers are lexed as IntegerLiteral; this method provides context-sensitive recognition.
    /// </summary>
    private bool IsLevelNumber()
    {
        if (Current.Kind == TokenKind.LevelNumber) return true; // legacy support
        if (Current.Kind != TokenKind.IntegerLiteral) return false;
        if (Current.Value is long lv)
        {
            int v = (int)lv;
            return (v >= 1 && v <= 49) || v == 66 || v == 77 || v == 88;
        }
        return false;
    }

    private int ConsumeLevelNumber()
    {
        var token = Advance();
        if (token.Value is long lv) return (int)lv;
        if (token.Value is int iv) return iv;
        return 1;
    }

    private void ReportError(string code, string message)
    {
        var location = _source != null
            ? _source.GetLocation(Current.Span.Start)
            : new SourceLocation("<unknown>", Current.Span.Start, 0, 0);
        _diagnostics.ReportError(code, message, location, Current.Span);
    }

    /// <summary>
    /// Error recovery: skip tokens until we reach a period (sentence terminator) or a known keyword.
    /// Stops WITHOUT consuming the period or keyword — the caller decides what to do.
    /// </summary>
    private void SkipToPeriodOrKeyword()
    {
        while (Current.Kind != TokenKind.EndOfFile)
        {
            if (Current.Kind == TokenKind.Period)
                return; // DO NOT consume — period belongs to ParseSentence
            // Stop at known division/section/statement keywords or scope terminators
            if (IsStatementStart(Current.Kind) || IsDivisionKeyword(Current.Kind) ||
                IsScopeTerminator(Current.Kind))
                return;
            Advance();
        }
    }

    private static bool IsDivisionKeyword(TokenKind kind) => kind is
        TokenKind.IdentificationKeyword or TokenKind.EnvironmentKeyword or
        TokenKind.DataKeyword or TokenKind.ProcedureKeyword;

    /// <summary>
    /// Checks if current position is the start of a division: keyword followed by DIVISION.
    /// More reliable than IsDivisionKeyword alone, because reserved words like DATA
    /// appear in free-text paragraphs (e.g., "AUTOMATED DATA AND TELECOMMUNICATION").
    /// </summary>
    private bool IsDivisionStart()
    {
        if (!IsDivisionKeyword(Current.Kind)) return false;
        return Peek().Kind == TokenKind.DivisionKeyword;
    }

    /// <summary>
    /// Checks for END PROGRAM (§5.3.2) which terminates a compilation unit.
    /// END is EndKeyword; PROGRAM is an Identifier in the keyword map but
    /// may also be lexed as an identifier depending on context.
    /// </summary>
    /// <summary>
    /// Issue 16 (§5.3.2): END PROGRAM program-name .
    /// PROGRAM is typically lexed as Identifier since it's context-sensitive.
    /// </summary>
    private bool IsEndProgram()
    {
        if (Current.Kind != TokenKind.EndKeyword) return false;
        var next = Peek();
        return next.Kind == TokenKind.Identifier &&
               next.Text.Equals("PROGRAM", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the current position is at a structural boundary that ends
    /// the procedure division: division start or END PROGRAM.
    /// </summary>
    private bool IsAtProcedureDivisionEnd()
    {
        return IsDivisionStart() || IsEndProgram();
    }

    private static bool IsStatementStart(TokenKind kind) => kind is
        TokenKind.DisplayKeyword or TokenKind.StopKeyword or TokenKind.MoveKeyword or
        TokenKind.AddKeyword or TokenKind.SubtractKeyword or TokenKind.ComputeKeyword or
        TokenKind.MultiplyKeyword or TokenKind.DivideKeyword or
        TokenKind.IfKeyword or TokenKind.PerformKeyword or TokenKind.EvaluateKeyword or
        TokenKind.GoKeyword or TokenKind.AcceptKeyword or TokenKind.CallKeyword or
        TokenKind.ContinueKeyword or TokenKind.ExitKeyword or TokenKind.InitializeKeyword or
        TokenKind.StringKeyword or TokenKind.UnstringKeyword or TokenKind.InspectKeyword or
        TokenKind.OpenKeyword or TokenKind.CloseKeyword or TokenKind.ReadKeyword or
        TokenKind.WriteKeyword or TokenKind.RewriteKeyword or TokenKind.DeleteKeyword or
        TokenKind.StartKeyword or TokenKind.SortKeyword or TokenKind.MergeKeyword or
        TokenKind.NextKeyword or // NEXT SENTENCE
        TokenKind.ReturnKeyword or // RETURN (sort output)
        TokenKind.ReleaseKeyword or // RELEASE (sort input)
        TokenKind.SetKeyword or TokenKind.SearchKeyword or TokenKind.GobackKeyword or
        // Phase 5.2 — Report Writer
        TokenKind.InitiateKeyword or TokenKind.GenerateKeyword or TokenKind.TerminateKeyword or
        // Phase 5.4 — OO COBOL
        TokenKind.InvokeKeyword or
        // Phase 5.5 — Exception handling
        TokenKind.RaiseKeyword or TokenKind.ResumeKeyword or
        // Phase 5.6-5.10 — Compiler directives
        TokenKind.CompilerDirective or
        // Archaic
        TokenKind.AlterKeyword;

    private static bool IsScopeTerminator(TokenKind kind) => kind is
        TokenKind.ElseKeyword or TokenKind.EndIfKeyword or TokenKind.EndPerformKeyword or
        TokenKind.EndEvaluateKeyword or TokenKind.WhenKeyword or
        TokenKind.EndReadKeyword or TokenKind.EndWriteKeyword or
        TokenKind.EndDeleteKeyword or TokenKind.EndStartKeyword or
        TokenKind.EndAddKeyword or TokenKind.EndSubtractKeyword or
        TokenKind.EndMultiplyKeyword or TokenKind.EndDivideKeyword or
        TokenKind.EndComputeKeyword or TokenKind.EndCallKeyword or
        TokenKind.EndStringKeyword or TokenKind.EndUnstringKeyword or
        TokenKind.EndAcceptKeyword or TokenKind.EndDisplayKeyword or
        TokenKind.EndSearchKeyword or TokenKind.EndReturnKeyword or
        TokenKind.EndRewriteKeyword;
}
