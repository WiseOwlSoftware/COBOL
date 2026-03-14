using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;

namespace CobolSharp.Compiler.Lexing;

/// <summary>
/// Free-form COBOL lexer. Produces tokens from source text.
/// Case-insensitive keyword matching. Handles free-form comments (*>).
/// Spec-driven: period handling per §8.3.5, PICTURE string tokenization,
/// hex/boolean/national literal prefixes.
/// </summary>
public sealed class Lexer
{
    private readonly SourceText _source;
    private readonly DiagnosticBag _diagnostics;
    private int _position;

    /// <summary>
    /// After emitting a PicKeyword, this flag tells the next call to NextToken()
    /// to consume the optional IS and then read the PICTURE character-string as
    /// a single PictureString token.
    /// </summary>
    private bool _expectPictureString;

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
        // Handle PICTURE string state: after PIC/PICTURE keyword,
        // consume optional IS, then read the picture character-string as one token.
        if (_expectPictureString)
        {
            _expectPictureString = false;
            return ReadPictureString();
        }

        SkipWhitespaceAndComments();

        if (_position >= _source.Length)
            return MakeToken(TokenKind.EndOfFile, _position, 0);

        char c = Current;

        // Compiler directive: >>SOURCE FORMAT IS FREE/FIXED
        if (c == '>' && Peek(1) == '>')
        {
            return ReadCompilerDirective();
        }

        // Numeric literal: starts with digit, or period followed by digit
        // Check this BEFORE period handling so ".5" is correctly parsed as decimal
        if (c == '.' && IsDigit(Peek(1)))
            return ReadNumericLiteral();

        // Period (sentence terminator) — spec §8.3.5:
        // A period followed by a space, EOL, or EOF is a separator period.
        // A period followed by a digit is part of a decimal literal (handled above).
        // A period in other contexts (e.g., followed by a letter) is still treated
        // as a separator period for practical purposes in free-form COBOL.
        if (c == '.')
            return MakeToken(TokenKind.Period, _position++, 1);

        if (IsDigit(c))
            return ReadNumericLiteral();

        // Hex/Boolean/National prefixed literals: X"...", B"...", N"...", Z"...",
        // BX"...", NX"..."
        if ((c == 'X' || c == 'x' || c == 'B' || c == 'b' || c == 'N' || c == 'n' || c == 'Z' || c == 'z')
            && (Peek(1) == '"' || Peek(1) == '\''))
        {
            return ReadPrefixedLiteral();
        }
        // Two-char prefixes: BX, NX
        if ((c == 'B' || c == 'b' || c == 'N' || c == 'n')
            && (Peek(1) == 'X' || Peek(1) == 'x')
            && (Peek(2) == '"' || Peek(2) == '\''))
        {
            return ReadPrefixedLiteral();
        }

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
            case ',':
                return MakeToken(TokenKind.Comma, _position++, 1);
            case ';':
                // Semicolon is a valid separator in COBOL (treated like a space)
                _position++;
                return NextToken(); // skip it
            case ':':
                return MakeToken(TokenKind.Colon, _position++, 1);
            case '$':
                // Currency symbol — valid in PICTURE strings, emit as its own token
                return MakeToken(TokenKind.Identifier, _position++, 1);
            case '&':
                // Ampersand — skip (appears in some COBOL string contexts)
                _position++;
                return NextToken();
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

    private Token ReadCompilerDirective()
    {
        int start = _position;
        // Consume entire line as a CompilerDirective token (>> ... end of line)
        while (_position < _source.Length && Current != '\n' && Current != '\r')
            _position++;
        string text = _source.GetText(start, _position - start);
        return new Token(TokenKind.CompilerDirective, text, new TextSpan(start, _position - start), text.Trim());
    }

    private Token ReadWord()
    {
        int start = _position;
        while (_position < _source.Length && IsWordChar(Current))
            _position++;

        string text = _source.GetText(start, _position - start);
        string upper = text.ToUpperInvariant();

        // Check keyword map
        if (Keywords.TryGetValue(upper, out var kind))
        {
            // If this is PIC/PICTURE, set the flag so the next token reads the picture string
            if (kind == TokenKind.PicKeyword)
                _expectPictureString = true;

            return new Token(kind, text, new TextSpan(start, _position - start));
        }

        return new Token(TokenKind.Identifier, text, new TextSpan(start, _position - start));
    }

    /// <summary>
    /// After PIC/PICTURE keyword, read the optional IS and then consume the entire
    /// PICTURE character-string as a single PictureString token.
    /// PICTURE strings can contain: 9, X, A, V, S, P, Z, B, 0, /, comma, period,
    /// CR, DB, *, +, -, $, and parenthesized repetition counts like (5).
    /// Terminates at separator space (not inside parens), period followed by space/EOL/EOF,
    /// or another data clause keyword.
    /// </summary>
    private Token ReadPictureString()
    {
        SkipWhitespaceAndComments();

        // Skip optional IS
        if (_position < _source.Length)
        {
            int saved = _position;
            // Read a word to check for IS
            if (IsLetter(Current))
            {
                int wordStart = _position;
                while (_position < _source.Length && IsWordChar(Current))
                    _position++;
                string word = _source.GetText(wordStart, _position - wordStart);
                if (!word.Equals("IS", StringComparison.OrdinalIgnoreCase))
                {
                    // Not IS — restore position; this word is the start of the picture string
                    _position = saved;
                }
                else
                {
                    // Consumed IS, skip whitespace before the actual picture string
                    SkipWhitespaceAndComments();
                }
            }
        }

        int start = _position;
        int parenDepth = 0;

        // Read the picture character-string
        while (_position < _source.Length)
        {
            char c = Current;

            if (c == '(')
            {
                parenDepth++;
                _position++;
                continue;
            }
            if (c == ')' && parenDepth > 0)
            {
                parenDepth--;
                _position++;
                continue;
            }

            // Period: if followed by space/EOL/EOF, it's a sentence terminator — stop
            if (c == '.' && parenDepth == 0)
            {
                char next = Peek(1);
                if (next == '\0' || char.IsWhiteSpace(next))
                    break;
                // Period followed by digit (e.g., PIC 9.99) is part of the picture
                _position++;
                continue;
            }

            // Separator space outside parens terminates the picture string
            if (char.IsWhiteSpace(c) && parenDepth == 0)
                break;

            _position++;
        }

        string text = _source.GetText(start, _position - start);
        return new Token(TokenKind.PictureString, text, new TextSpan(start, _position - start), text);
    }

    /// <summary>
    /// Read a prefixed literal: X"...", B"...", N"...", Z"...", BX"...", NX"..."
    /// </summary>
    private Token ReadPrefixedLiteral()
    {
        int start = _position;
        char prefix1 = char.ToUpperInvariant(Current);
        _position++;

        // Check for two-char prefix (BX, NX)
        char prefix2 = '\0';
        if ((prefix1 == 'B' || prefix1 == 'N') && (Current == 'X' || Current == 'x'))
        {
            prefix2 = 'X';
            _position++;
        }

        // Now we should be at the quote character
        char quote = Current;
        _position++; // skip opening quote

        var sb = new System.Text.StringBuilder();
        while (_position < _source.Length)
        {
            if (Current == quote)
            {
                _position++;
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
        string value = sb.ToString();

        TokenKind kind;
        if (prefix2 == 'X')
        {
            kind = prefix1 == 'B' ? TokenKind.HexLiteral : TokenKind.NationalLiteral;
        }
        else
        {
            kind = prefix1 switch
            {
                'X' => TokenKind.HexLiteral,
                'B' => TokenKind.BooleanLiteral,
                'N' => TokenKind.NationalLiteral,
                'Z' => TokenKind.StringLiteral, // Z"..." is null-terminated, treat as string
                _ => TokenKind.StringLiteral
            };
        }

        return new Token(kind, text, new TextSpan(start, _position - start), value);
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

        if (hasDot)
        {
            return new Token(TokenKind.DecimalLiteral, text,
                new TextSpan(start, _position - start), decimal.Parse(text));
        }
        else
        {
            return new Token(TokenKind.IntegerLiteral, text,
                new TextSpan(start, _position - start), long.Parse(text));
        }
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
            ["REDEFINES"] = TokenKind.RedefinesKeyword,
            ["OCCURS"] = TokenKind.OccursKeyword,
            ["DEPENDING"] = TokenKind.DependingKeyword,
            ["RENAMES"] = TokenKind.RenamesKeyword,
            ["BLANK"] = TokenKind.BlankKeyword,
            ["JUSTIFIED"] = TokenKind.JustifiedKeyword,
            ["JUST"] = TokenKind.JustifiedKeyword,
            ["SYNCHRONIZED"] = TokenKind.SynchronizedKeyword,
            ["SYNC"] = TokenKind.SynchronizedKeyword,
            ["INDEX"] = TokenKind.IndexKeyword,
            ["INDEXED"] = TokenKind.IndexedKeyword,
            ["POINTER"] = TokenKind.PointerKeyword,
            ["FUNCTION"] = TokenKind.FunctionKeyword,
            ["FUNCTION-POINTER"] = TokenKind.FunctionPointerKeyword,
            ["PROCEDURE-POINTER"] = TokenKind.ProcedurePointerKeyword,
            ["GLOBAL"] = TokenKind.GlobalKeyword,
            ["EXTERNAL"] = TokenKind.ExternalKeyword,
            ["ASCENDING"] = TokenKind.AscendingKeyword,
            ["DESCENDING"] = TokenKind.DescendingKeyword,
            ["KEY"] = TokenKind.KeyKeyword,
            ["VALUES"] = TokenKind.ValuesKeyword,

            // File I/O keywords
            ["SELECT"] = TokenKind.SelectKeyword,
            ["ASSIGN"] = TokenKind.AssignKeyword,
            ["ORGANIZATION"] = TokenKind.OrganizationKeyword,
            ["ACCESS"] = TokenKind.AccessKeyword,
            ["MODE"] = TokenKind.ModeKeyword,
            ["RECORD"] = TokenKind.RecordKeyword,
            ["ALTERNATE"] = TokenKind.AlternateKeyword,
            ["STATUS"] = TokenKind.StatusKeyword,
            ["FILE"] = TokenKind.FileKeyword,
            ["FD"] = TokenKind.FdKeyword,
            ["SD"] = TokenKind.SdKeyword,
            ["BLOCK"] = TokenKind.BlockKeyword,
            ["CONTAINS"] = TokenKind.ContainsKeyword,
            ["LABEL"] = TokenKind.LabelKeyword,
            ["RECORDS"] = TokenKind.RecordsKeyword,
            ["LINAGE"] = TokenKind.LinageKeyword,
            ["INPUT"] = TokenKind.InputKeyword,
            ["OUTPUT"] = TokenKind.OutputKeyword,
            ["EXTEND"] = TokenKind.ExtendKeyword,
            ["PAGE"] = TokenKind.PageKeyword,
            ["ADVANCING"] = TokenKind.AdvancingKeyword,
            ["DUPLICATES"] = TokenKind.DuplicatesKeyword,
            ["WITH"] = TokenKind.WithKeyword,
            ["LINKAGE"] = TokenKind.LinkageKeyword,
            ["LINE"] = TokenKind.LineKeyword,
            ["SEQUENTIAL"] = TokenKind.SequentialKeyword,
            ["RANDOM"] = TokenKind.RandomKeyword,
            ["DYNAMIC"] = TokenKind.DynamicKeyword,
            ["RELATIVE"] = TokenKind.RelativeKeyword,
            ["INVALID"] = TokenKind.InvalidKeyword,
            ["END-READ"] = TokenKind.EndReadKeyword,
            ["END-WRITE"] = TokenKind.EndWriteKeyword,
            ["END-DELETE"] = TokenKind.EndDeleteKeyword,
            ["END-START"] = TokenKind.EndStartKeyword,
            ["AT"] = TokenKind.AtKeyword,
            ["END"] = TokenKind.EndKeyword,
            ["NEXT"] = TokenKind.NextKeyword,
            ["I-O"] = TokenKind.I_OKeyword,

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
            ["USING"] = TokenKind.UsingKeyword,
            ["RETURNING"] = TokenKind.ReturningKeyword,
            ["REFERENCE"] = TokenKind.ReferenceKeyword,
            ["CONTENT"] = TokenKind.ContentKeyword,
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

            // Phase 5.2: Report Writer
            ["REPORT"] = TokenKind.ReportKeyword,
            ["RD"] = TokenKind.RdKeyword,
            ["INITIATE"] = TokenKind.InitiateKeyword,
            ["GENERATE"] = TokenKind.GenerateKeyword,
            ["TERMINATE"] = TokenKind.TerminateKeyword,

            // Phase 5.3: Screen Section
            ["SCREEN"] = TokenKind.ScreenKeyword,

            // Phase 5.4: OO COBOL
            ["CLASS-ID"] = TokenKind.ClassIdKeyword,
            ["METHOD-ID"] = TokenKind.MethodIdKeyword,
            ["INTERFACE-ID"] = TokenKind.InterfaceIdKeyword,
            ["INVOKE"] = TokenKind.InvokeKeyword,
            ["FACTORY"] = TokenKind.FactoryKeyword,
            ["OBJECT"] = TokenKind.ObjectKeyword,

            // Phase 5.5: Exception Handling
            ["RAISE"] = TokenKind.RaiseKeyword,
            ["RESUME"] = TokenKind.ResumeKeyword,

            // Phase 5.6–5.10: Compiler directives / national types
            ["SOURCE-FORMAT"] = TokenKind.SourceFormatKeyword,
            ["FREE"] = TokenKind.FreeKeyword,
            ["FIXED"] = TokenKind.FixedKeyword,
            ["NATIONAL"] = TokenKind.NationalKeyword,

            // Scope terminators
            ["END-ADD"] = TokenKind.EndAddKeyword,
            ["END-SUBTRACT"] = TokenKind.EndSubtractKeyword,
            ["END-MULTIPLY"] = TokenKind.EndMultiplyKeyword,
            ["END-DIVIDE"] = TokenKind.EndDivideKeyword,
            ["END-COMPUTE"] = TokenKind.EndComputeKeyword,
            ["END-CALL"] = TokenKind.EndCallKeyword,
            ["END-STRING"] = TokenKind.EndStringKeyword,
            ["END-UNSTRING"] = TokenKind.EndUnstringKeyword,
            ["END-ACCEPT"] = TokenKind.EndAcceptKeyword,
            ["END-DISPLAY"] = TokenKind.EndDisplayKeyword,
            ["END-SEARCH"] = TokenKind.EndSearchKeyword,
            ["END-RETURN"] = TokenKind.EndReturnKeyword,
            ["END-REWRITE"] = TokenKind.EndRewriteKeyword,

            // Additional keywords
            ["THEN"] = TokenKind.ThenKeyword,
            ["GOBACK"] = TokenKind.GobackKeyword,
            ["IN"] = TokenKind.InKeyword,
            ["OF"] = TokenKind.OfKeyword,
        };
    }
}
