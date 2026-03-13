using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;

namespace CobolSharp.Compiler.Lexing;

/// <summary>
/// Free-form COBOL lexer. Produces tokens from source text.
/// Case-insensitive keyword matching. Handles free-form comments (*>).
/// </summary>
public sealed class Lexer
{
    private readonly SourceText _source;
    private readonly DiagnosticBag _diagnostics;
    private int _position;

    private static readonly Dictionary<string, TokenKind> Keywords = BuildKeywordMap();

    public Lexer(SourceText source, DiagnosticBag diagnostics)
    {
        _source = source;
        _diagnostics = diagnostics;
        _position = 0;
    }

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();

        while (true)
        {
            var token = NextToken();
            tokens.Add(token);
            if (token.Kind == TokenKind.EndOfFile)
                break;
        }

        return tokens;
    }

    private Token NextToken()
    {
        SkipWhitespaceAndComments();

        if (_position >= _source.Length)
            return MakeToken(TokenKind.EndOfFile, _position, 0);

        char c = Current;

        // Period (sentence terminator) — but not if followed by a digit (decimal literal)
        if (c == '.' && !IsDigit(Peek(1)))
            return MakeToken(TokenKind.Period, _position++, 1);

        // Numeric literal: starts with digit, or period followed by digit
        if (IsDigit(c) || (c == '.' && IsDigit(Peek(1))))
            return ReadNumericLiteral();

        // String literal
        if (c == '"' || c == '\'')
            return ReadStringLiteral();

        // Operators and punctuation
        switch (c)
        {
            case '(':
                return MakeToken(TokenKind.LeftParen, _position++, 1);
            case ')':
                return MakeToken(TokenKind.RightParen, _position++, 1);
            case '+':
                return MakeToken(TokenKind.Plus, _position++, 1);
            case '-':
                // Could be minus or part of a keyword like END-IF
                // If next char is a letter, this might be a hyphenated word — fall through to identifier
                if (IsLetter(Peek(1)))
                    break; // fall through to identifier handling below
                return MakeToken(TokenKind.Minus, _position++, 1);
            case '/':
                return MakeToken(TokenKind.Divide, _position++, 1);
            case '=':
                return MakeToken(TokenKind.Equals, _position++, 1);
            case '<':
                if (Peek(1) == '=')
                {
                    _position += 2;
                    return MakeToken(TokenKind.LessThanOrEqual, _position - 2, 2);
                }
                return MakeToken(TokenKind.LessThan, _position++, 1);
            case '>':
                if (Peek(1) == '=')
                {
                    _position += 2;
                    return MakeToken(TokenKind.GreaterThanOrEqual, _position - 2, 2);
                }
                return MakeToken(TokenKind.GreaterThan, _position++, 1);
            case '*':
                if (Peek(1) == '*')
                {
                    _position += 2;
                    return MakeToken(TokenKind.Power, _position - 2, 2);
                }
                return MakeToken(TokenKind.Multiply, _position++, 1);
        }

        // Identifier or keyword (COBOL words: letters, digits, hyphens)
        if (IsLetter(c) || c == '-')
            return ReadWord();

        // Unrecognized character
        var location = _source.GetLocation(_position);
        var span = new TextSpan(_position, 1);
        _diagnostics.ReportError("CS0001", $"Unexpected character '{c}'", location, span);
        return MakeToken(TokenKind.Bad, _position++, 1);
    }

    private Token ReadWord()
    {
        int start = _position;
        while (_position < _source.Length && IsWordChar(Current))
            _position++;

        string text = _source.GetText(start, _position - start);
        string upper = text.ToUpperInvariant();

        // Check if it's a level number (01-49, 66, 77, 88)
        if (text.Length <= 2 && int.TryParse(text, out int level) &&
            ((level >= 1 && level <= 49) || level == 66 || level == 77 || level == 88))
        {
            return new Token(TokenKind.LevelNumber, text, new TextSpan(start, _position - start), level);
        }

        // Check keyword map
        if (Keywords.TryGetValue(upper, out var kind))
            return new Token(kind, text, new TextSpan(start, _position - start));

        return new Token(TokenKind.Identifier, text, new TextSpan(start, _position - start));
    }

    private Token ReadNumericLiteral()
    {
        int start = _position;
        bool hasDot = false;

        if (Current == '.')
        {
            hasDot = true;
            _position++;
        }

        while (_position < _source.Length && IsDigit(Current))
            _position++;

        if (!hasDot && _position < _source.Length && Current == '.' && IsDigit(Peek(1)))
        {
            hasDot = true;
            _position++;
            while (_position < _source.Length && IsDigit(Current))
                _position++;
        }

        string text = _source.GetText(start, _position - start);
        var kind = hasDot ? TokenKind.DecimalLiteral : TokenKind.IntegerLiteral;
        object value = hasDot ? decimal.Parse(text) : long.Parse(text);

        return new Token(kind, text, new TextSpan(start, _position - start), value);
    }

    private Token ReadStringLiteral()
    {
        char quote = Current;
        int start = _position;
        _position++; // skip opening quote

        var sb = new System.Text.StringBuilder();
        while (_position < _source.Length)
        {
            if (Current == quote)
            {
                _position++;
                // Doubled quote is an escape: "" inside "..." means literal "
                if (_position < _source.Length && Current == quote)
                {
                    sb.Append(quote);
                    _position++;
                    continue;
                }
                break;
            }
            sb.Append(Current);
            _position++;
        }

        string text = _source.GetText(start, _position - start);
        return new Token(TokenKind.StringLiteral, text, new TextSpan(start, _position - start), sb.ToString());
    }

    private void SkipWhitespaceAndComments()
    {
        while (_position < _source.Length)
        {
            if (char.IsWhiteSpace(Current))
            {
                _position++;
                continue;
            }

            // Free-form comment: *>
            if (Current == '*' && Peek(1) == '>')
            {
                // Skip to end of line
                while (_position < _source.Length && Current != '\n' && Current != '\r')
                    _position++;
                continue;
            }

            break;
        }
    }

    private char Current => _position < _source.Length ? _source[_position] : '\0';

    private char Peek(int offset)
    {
        int pos = _position + offset;
        return pos < _source.Length ? _source[pos] : '\0';
    }

    private Token MakeToken(TokenKind kind, int start, int length)
    {
        string text = length > 0 && start < _source.Length
            ? _source.GetText(start, length)
            : string.Empty;
        return new Token(kind, text, new TextSpan(start, length));
    }

    private static bool IsLetter(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
    private static bool IsDigit(char c) => c >= '0' && c <= '9';
    private static bool IsWordChar(char c) => IsLetter(c) || IsDigit(c) || c == '-';

    private static Dictionary<string, TokenKind> BuildKeywordMap()
    {
        return new Dictionary<string, TokenKind>(StringComparer.OrdinalIgnoreCase)
        {
            // Division/section structure
            ["IDENTIFICATION"] = TokenKind.IdentificationKeyword,
            ["ID"] = TokenKind.IdentificationKeyword,
            ["PROGRAM-ID"] = TokenKind.ProgramIdKeyword,
            ["DIVISION"] = TokenKind.DivisionKeyword,
            ["SECTION"] = TokenKind.SectionKeyword,
            ["ENVIRONMENT"] = TokenKind.EnvironmentKeyword,
            ["DATA"] = TokenKind.DataKeyword,
            ["PROCEDURE"] = TokenKind.ProcedureKeyword,
            ["WORKING-STORAGE"] = TokenKind.WorkingStorageKeyword,

            // Data description
            ["PIC"] = TokenKind.PicKeyword,
            ["PICTURE"] = TokenKind.PicKeyword,
            ["USAGE"] = TokenKind.UsageKeyword,
            ["VALUE"] = TokenKind.ValueKeyword,
            ["COMP"] = TokenKind.CompKeyword,
            ["COMPUTATIONAL"] = TokenKind.CompKeyword,
            ["BINARY"] = TokenKind.CompKeyword,
            ["COMP-3"] = TokenKind.Comp3Keyword,
            ["COMPUTATIONAL-3"] = TokenKind.Comp3Keyword,
            ["PACKED-DECIMAL"] = TokenKind.Comp3Keyword,
            ["FILLER"] = TokenKind.FillerKeyword,

            // Procedure division statements
            ["DISPLAY"] = TokenKind.DisplayKeyword,
            ["STOP"] = TokenKind.StopKeyword,
            ["RUN"] = TokenKind.RunKeyword,
            ["MOVE"] = TokenKind.MoveKeyword,
            ["TO"] = TokenKind.ToKeyword,
            ["ADD"] = TokenKind.AddKeyword,
            ["GIVING"] = TokenKind.GivingKeyword,
            ["SUBTRACT"] = TokenKind.SubtractKeyword,
            ["FROM"] = TokenKind.FromKeyword,
            ["MULTIPLY"] = TokenKind.MultiplyKeyword,
            ["BY"] = TokenKind.ByKeyword,
            ["DIVIDE"] = TokenKind.DivideKeyword,
            ["INTO"] = TokenKind.IntoKeyword,
            ["REMAINDER"] = TokenKind.RemainderKeyword,
            ["COMPUTE"] = TokenKind.ComputeKeyword,
            ["IF"] = TokenKind.IfKeyword,
            ["ELSE"] = TokenKind.ElseKeyword,
            ["END-IF"] = TokenKind.EndIfKeyword,
            ["PERFORM"] = TokenKind.PerformKeyword,
            ["END-PERFORM"] = TokenKind.EndPerformKeyword,
            ["UNTIL"] = TokenKind.UntilKeyword,
            ["VARYING"] = TokenKind.VaryingKeyword,
            ["TIMES"] = TokenKind.TimesKeyword,
            ["THRU"] = TokenKind.ThruKeyword,
            ["THROUGH"] = TokenKind.ThruKeyword,
            ["EVALUATE"] = TokenKind.EvaluateKeyword,
            ["WHEN"] = TokenKind.WhenKeyword,
            ["OTHER"] = TokenKind.OtherKeyword,
            ["END-EVALUATE"] = TokenKind.EndEvaluateKeyword,
            ["GO"] = TokenKind.GoKeyword,
            ["NOT"] = TokenKind.NotKeyword,
            ["AND"] = TokenKind.AndKeyword,
            ["OR"] = TokenKind.OrKeyword,
            ["TRUE"] = TokenKind.TrueKeyword,
            ["FALSE"] = TokenKind.FalseKeyword,
            ["SPACE"] = TokenKind.SpaceKeyword,
            ["SPACES"] = TokenKind.SpaceKeyword,
            ["ZERO"] = TokenKind.ZeroKeyword,
            ["ZEROS"] = TokenKind.ZeroKeyword,
            ["ZEROES"] = TokenKind.ZeroKeyword,
            ["HIGH-VALUE"] = TokenKind.HighValueKeyword,
            ["HIGH-VALUES"] = TokenKind.HighValueKeyword,
            ["LOW-VALUE"] = TokenKind.LowValueKeyword,
            ["LOW-VALUES"] = TokenKind.LowValueKeyword,
            ["QUOTE"] = TokenKind.QuoteKeyword,
            ["QUOTES"] = TokenKind.QuoteKeyword,
            ["ALL"] = TokenKind.AllKeyword,

            // Reserved for later phases
            ["ACCEPT"] = TokenKind.AcceptKeyword,
            ["CALL"] = TokenKind.CallKeyword,
            ["CANCEL"] = TokenKind.CancelKeyword,
            ["CONTINUE"] = TokenKind.ContinueKeyword,
            ["COPY"] = TokenKind.CopyKeyword,
            ["DELETE"] = TokenKind.DeleteKeyword,
            ["EXIT"] = TokenKind.ExitKeyword,
            ["INITIALIZE"] = TokenKind.InitializeKeyword,
            ["INSPECT"] = TokenKind.InspectKeyword,
            ["MERGE"] = TokenKind.MergeKeyword,
            ["OPEN"] = TokenKind.OpenKeyword,
            ["READ"] = TokenKind.ReadKeyword,
            ["RELEASE"] = TokenKind.ReleaseKeyword,
            ["REPLACE"] = TokenKind.ReplaceKeyword,
            ["RETURN"] = TokenKind.ReturnKeyword,
            ["REWRITE"] = TokenKind.RewriteKeyword,
            ["SEARCH"] = TokenKind.SearchKeyword,
            ["SET"] = TokenKind.SetKeyword,
            ["SORT"] = TokenKind.SortKeyword,
            ["START"] = TokenKind.StartKeyword,
            ["STRING"] = TokenKind.StringKeyword,
            ["UNSTRING"] = TokenKind.UnstringKeyword,
            ["WRITE"] = TokenKind.WriteKeyword,
            ["CLOSE"] = TokenKind.CloseKeyword,
            ["ALTER"] = TokenKind.AlterKeyword,

            // Modifiers
            ["ROUNDED"] = TokenKind.RoundedKeyword,
            ["SIZE"] = TokenKind.SizeKeyword,
            ["ERROR"] = TokenKind.ErrorKeyword,
            ["ON"] = TokenKind.OnKeyword,
            ["CORRESPONDING"] = TokenKind.CorrespondingKeyword,
            ["CORR"] = TokenKind.CorrespondingKeyword,

            // Conditions
            ["IS"] = TokenKind.IsKeyword,
            ["THAN"] = TokenKind.ThanKeyword,
            ["EQUAL"] = TokenKind.EqualKeyword,
            ["GREATER"] = TokenKind.GreaterKeyword,
            ["LESS"] = TokenKind.LessKeyword,
            ["POSITIVE"] = TokenKind.PositiveKeyword,
            ["NEGATIVE"] = TokenKind.NegativeKeyword,
            ["NUMERIC"] = TokenKind.NumericKeyword,
            ["ALPHABETIC"] = TokenKind.AlphabeticKeyword,

            // PERFORM modifiers
            ["TEST"] = TokenKind.TestKeyword,
            ["BEFORE"] = TokenKind.BeforeKeyword,
            ["AFTER"] = TokenKind.AfterKeyword,
        };
    }
}
