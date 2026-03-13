using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Lexing;

namespace CobolSharp.Compiler.Parsing;

/// <summary>
/// Hand-written recursive descent parser for COBOL.
/// Phase 1 subset: IDENTIFICATION DIVISION, DATA DIVISION (WORKING-STORAGE),
/// PROCEDURE DIVISION with DISPLAY, STOP RUN, MOVE, ADD, SUBTRACT, COMPUTE, IF, PERFORM.
/// </summary>
public sealed class Parser
{
    private readonly List<Token> _tokens;
    private readonly DiagnosticBag _diagnostics;
    private int _position;

    public Parser(List<Token> tokens, DiagnosticBag diagnostics)
    {
        _tokens = tokens;
        _diagnostics = diagnostics;
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
        // Derive location from current token's span if we have a source.
        // For now, use a placeholder location.
        _diagnostics.ReportError(code, message,
            new SourceLocation("<unknown>", Current.Span.Start, 0, 0), Current.Span);
    }

    /// <summary>
    /// Error recovery: skip tokens until we reach a period (sentence terminator) or a known keyword.
    /// </summary>
    private void SkipToPeriodOrKeyword()
    {
        while (Current.Kind != TokenKind.EndOfFile)
        {
            if (Current.Kind == TokenKind.Period)
            {
                Advance();
                return;
            }
            // Stop at known division/section/statement keywords
            if (IsStatementStart(Current.Kind) || IsDivisionKeyword(Current.Kind))
                return;
            Advance();
        }
    }

    private static bool IsDivisionKeyword(TokenKind kind) => kind is
        TokenKind.IdentificationKeyword or TokenKind.EnvironmentKeyword or
        TokenKind.DataKeyword or TokenKind.ProcedureKeyword;

    private static bool IsStatementStart(TokenKind kind) => kind is
        TokenKind.DisplayKeyword or TokenKind.StopKeyword or TokenKind.MoveKeyword or
        TokenKind.AddKeyword or TokenKind.SubtractKeyword or TokenKind.ComputeKeyword or
        TokenKind.IfKeyword or TokenKind.PerformKeyword or TokenKind.EvaluateKeyword or
        TokenKind.GoKeyword or TokenKind.AcceptKeyword or TokenKind.CallKeyword or
        TokenKind.ContinueKeyword or TokenKind.ExitKeyword or TokenKind.InitializeKeyword;

    private static bool IsScopeTerminator(TokenKind kind) => kind is
        TokenKind.ElseKeyword or TokenKind.EndIfKeyword or TokenKind.EndPerformKeyword or
        TokenKind.EndEvaluateKeyword or TokenKind.WhenKeyword;

    // ═══════════════════════════════════════════════════
    // Top-level parsing
    // ═══════════════════════════════════════════════════

    public CompilationUnit ParseCompilationUnit()
    {
        int start = Current.Span.Start;
        var programs = new List<ProgramNode>();
        programs.Add(ParseProgram());
        int end = Current.Span.End;
        return new CompilationUnit(programs, TextSpan.FromBounds(start, end));
    }

    private ProgramNode ParseProgram()
    {
        int start = Current.Span.Start;

        var identification = ParseIdentificationDivision();

        // Optional ENVIRONMENT DIVISION (skip for Phase 1)
        if (Check(TokenKind.EnvironmentKeyword))
            SkipDivision();

        DataDivision? data = null;
        if (Check(TokenKind.DataKeyword))
            data = ParseDataDivision();

        ProcedureDivision? procedure = null;
        if (Check(TokenKind.ProcedureKeyword))
            procedure = ParseProcedureDivision();

        int end = Current.Span.End;
        return new ProgramNode(identification, data, procedure, TextSpan.FromBounds(start, end));
    }

    private void SkipDivision()
    {
        // Skip ENVIRONMENT DIVISION. (or any other division we don't parse yet)
        Advance(); // keyword
        if (Check(TokenKind.DivisionKeyword)) Advance();
        if (Check(TokenKind.Period)) Advance();

        // Skip until next division keyword
        while (Current.Kind != TokenKind.EndOfFile && !IsDivisionKeyword(Current.Kind))
            Advance();
    }

    // ═══════════════════════════════════════════════════
    // IDENTIFICATION DIVISION (§11)
    // ═══════════════════════════════════════════════════

    private IdentificationDivision ParseIdentificationDivision()
    {
        int start = Current.Span.Start;

        Expect(TokenKind.IdentificationKeyword, "Expected IDENTIFICATION DIVISION");
        Expect(TokenKind.DivisionKeyword);
        Expect(TokenKind.Period);

        Expect(TokenKind.ProgramIdKeyword, "Expected PROGRAM-ID");
        Expect(TokenKind.Period);

        var nameToken = Expect(TokenKind.Identifier, "Expected program name");
        string programId = nameToken.Text;

        // Optional period after program ID, or period might be on same line
        Match(TokenKind.Period);

        int end = Current.Span.Start;
        return new IdentificationDivision(programId, TextSpan.FromBounds(start, end));
    }

    // ═══════════════════════════════════════════════════
    // DATA DIVISION (§13)
    // ═══════════════════════════════════════════════════

    private DataDivision ParseDataDivision()
    {
        int start = Current.Span.Start;

        Expect(TokenKind.DataKeyword);
        Expect(TokenKind.DivisionKeyword);
        Expect(TokenKind.Period);

        WorkingStorageSection? ws = null;
        if (Check(TokenKind.WorkingStorageKeyword))
            ws = ParseWorkingStorageSection();

        int end = Current.Span.Start;
        return new DataDivision(ws, TextSpan.FromBounds(start, end));
    }

    private WorkingStorageSection ParseWorkingStorageSection()
    {
        int start = Current.Span.Start;

        Expect(TokenKind.WorkingStorageKeyword);
        Expect(TokenKind.SectionKeyword);
        Expect(TokenKind.Period);

        var entries = new List<DataDescriptionEntry>();
        while (IsLevelNumber())
        {
            entries.Add(ParseDataDescriptionEntry());
        }

        int end = Current.Span.Start;
        return new WorkingStorageSection(entries, TextSpan.FromBounds(start, end));
    }

    private DataDescriptionEntry ParseDataDescriptionEntry()
    {
        int start = Current.Span.Start;

        int level = ConsumeLevelNumber();

        // Data name or FILLER
        string? name = null;
        if (Check(TokenKind.Identifier))
        {
            name = Advance().Text;
        }
        else if (Match(TokenKind.FillerKeyword))
        {
            name = null; // FILLER
        }

        // Level 66 (RENAMES) — special syntax
        if (level == 66)
        {
            return ParseRenamesEntry(start, name);
        }

        // Parse clauses until period
        string? pic = null;
        var usage = UsageType.Display;
        Expression? initialValue = null;
        string? redefinesName = null;
        int occursCount = 0;
        string? occursDependingOn = null;
        bool blankWhenZero = false;
        bool justifiedRight = false;
        List<ConditionValueClause>? conditionValues = null;

        // Level 88 (condition-name) — special syntax: VALUE/VALUES
        if (level == 88)
        {
            conditionValues = ParseLevel88Values();
            Expect(TokenKind.Period);
            int end88 = Current.Span.Start;
            return new DataDescriptionEntry(level, name, null, usage, null,
                TextSpan.FromBounds(start, end88),
                conditionValues: conditionValues);
        }

        while (!Check(TokenKind.Period) && Current.Kind != TokenKind.EndOfFile)
        {
            if (Check(TokenKind.PicKeyword))
            {
                Advance();
                Match(TokenKind.IsKeyword); // optional IS
                pic = ParsePictureString();
            }
            else if (Check(TokenKind.UsageKeyword))
            {
                Advance();
                Match(TokenKind.IsKeyword); // optional IS
                usage = ParseUsage();
            }
            else if (Check(TokenKind.CompKeyword))
            {
                Advance();
                usage = UsageType.Binary;
            }
            else if (Check(TokenKind.Comp3Keyword))
            {
                Advance();
                usage = UsageType.PackedDecimal;
            }
            else if (Check(TokenKind.IndexKeyword) &&
                     Peek().Kind != TokenKind.ByKeyword) // INDEX as USAGE, not INDEX BY
            {
                Advance();
                usage = UsageType.Index;
            }
            else if (Check(TokenKind.PointerKeyword))
            {
                Advance();
                usage = UsageType.Pointer;
            }
            else if (Check(TokenKind.FunctionPointerKeyword))
            {
                Advance();
                usage = UsageType.FunctionPointer;
            }
            else if (Check(TokenKind.ProcedurePointerKeyword))
            {
                Advance();
                usage = UsageType.ProcedurePointer;
            }
            else if (Check(TokenKind.ValueKeyword) || Check(TokenKind.ValuesKeyword))
            {
                Advance();
                Match(TokenKind.IsKeyword); // optional IS
                initialValue = ParseExpression();
            }
            else if (Check(TokenKind.RedefinesKeyword))
            {
                Advance();
                var redToken = Expect(TokenKind.Identifier, "Expected data-name after REDEFINES");
                redefinesName = redToken.Text;
            }
            else if (Check(TokenKind.OccursKeyword))
            {
                Advance();
                // OCCURS n TIMES or OCCURS n TO m DEPENDING ON identifier
                if (Check(TokenKind.IntegerLiteral))
                {
                    var countToken = Advance();
                    occursCount = (int)(long)countToken.Value!;
                }
                Match(TokenKind.TimesKeyword); // optional TIMES

                // Check for DEPENDING ON
                if (Check(TokenKind.DependingKeyword))
                {
                    Advance();
                    Match(TokenKind.OnKeyword);
                    var depToken = Expect(TokenKind.Identifier, "Expected identifier after DEPENDING ON");
                    occursDependingOn = depToken.Text;
                }

                // Skip ASCENDING/DESCENDING KEY IS, INDEXED BY (consume but don't model yet)
                while (Check(TokenKind.AscendingKeyword) || Check(TokenKind.DescendingKeyword))
                {
                    Advance();
                    Match(TokenKind.KeyKeyword);
                    Match(TokenKind.IsKeyword);
                    if (Check(TokenKind.Identifier)) Advance(); // key name
                }
                while (Check(TokenKind.IndexedKeyword))
                {
                    Advance();
                    Match(TokenKind.ByKeyword);
                    while (Check(TokenKind.Identifier)) Advance(); // index names
                }
            }
            else if (Check(TokenKind.BlankKeyword))
            {
                Advance();
                Match(TokenKind.WhenKeyword); // BLANK WHEN ZERO
                Match(TokenKind.ZeroKeyword);
                blankWhenZero = true;
            }
            else if (Check(TokenKind.JustifiedKeyword))
            {
                Advance();
                Match(TokenKind.Identifier); // optional RIGHT
                justifiedRight = true;
            }
            else if (Check(TokenKind.SynchronizedKeyword))
            {
                Advance();
                // Skip optional LEFT/RIGHT
                if (Check(TokenKind.Identifier)) Advance();
            }
            else if (Check(TokenKind.GlobalKeyword) || Check(TokenKind.ExternalKeyword))
            {
                Advance(); // consume, not modeled yet
            }
            else
            {
                // Unknown clause — skip this token
                Advance();
            }
        }

        Expect(TokenKind.Period);

        int end = Current.Span.Start;
        return new DataDescriptionEntry(level, name, pic, usage, initialValue,
            TextSpan.FromBounds(start, end),
            redefinesName: redefinesName,
            occursCount: occursCount,
            occursDependingOn: occursDependingOn,
            isBlankWhenZero: blankWhenZero,
            isJustifiedRight: justifiedRight);
    }

    private DataDescriptionEntry ParseRenamesEntry(int start, string? name)
    {
        // 66 data-name RENAMES data-name-1 [THRU data-name-2].
        Expect(TokenKind.RenamesKeyword, "Expected RENAMES after level 66");
        var startName = Expect(TokenKind.Identifier, "Expected data-name after RENAMES");

        string? endName = null;
        if (Match(TokenKind.ThruKeyword))
        {
            var endToken = Expect(TokenKind.Identifier, "Expected data-name after THRU");
            endName = endToken.Text;
        }

        Expect(TokenKind.Period);
        int end = Current.Span.Start;
        return new DataDescriptionEntry(66, name, null, UsageType.Display, null,
            TextSpan.FromBounds(start, end),
            renamesStartName: startName.Text,
            renamesEndName: endName);
    }

    private List<ConditionValueClause> ParseLevel88Values()
    {
        // VALUE/VALUES IS/ARE literal [THRU literal] [literal [THRU literal]] ...
        if (!Match(TokenKind.ValueKeyword) && !Match(TokenKind.ValuesKeyword))
        {
            Expect(TokenKind.ValueKeyword, "Expected VALUE or VALUES for level 88");
        }
        Match(TokenKind.IsKeyword);   // optional IS
        Match(TokenKind.Identifier);  // optional ARE (would be lexed as identifier)

        var values = new List<ConditionValueClause>();

        while (!Check(TokenKind.Period) && Current.Kind != TokenKind.EndOfFile)
        {
            int valStart = Current.Span.Start;
            var val = ParseExpression();
            Expression? thru = null;

            if (Match(TokenKind.ThruKeyword))
            {
                thru = ParseExpression();
            }

            values.Add(new ConditionValueClause(val, thru,
                TextSpan.FromBounds(valStart, Current.Span.Start)));
        }

        return values;
    }

    private string ParsePictureString()
    {
        // PICTURE strings can contain characters like 9, X, A, V, S, (, ), etc.
        // They appear as identifiers or special tokens. We consume tokens until we hit
        // a clause keyword, a period, or a level number.
        var sb = new System.Text.StringBuilder();

        while (Current.Kind != TokenKind.EndOfFile &&
               Current.Kind != TokenKind.Period &&
               Current.Kind != TokenKind.LevelNumber &&
               !IsDataClauseKeyword(Current.Kind))
        {
            sb.Append(Current.Text);
            Advance();
        }

        return sb.ToString();
    }

    private static bool IsDataClauseKeyword(TokenKind kind) => kind is
        TokenKind.UsageKeyword or TokenKind.ValueKeyword or TokenKind.ValuesKeyword or
        TokenKind.PicKeyword or TokenKind.CompKeyword or TokenKind.Comp3Keyword or
        TokenKind.RedefinesKeyword or TokenKind.OccursKeyword or TokenKind.BlankKeyword or
        TokenKind.JustifiedKeyword or TokenKind.SynchronizedKeyword or
        TokenKind.GlobalKeyword or TokenKind.ExternalKeyword;

    private UsageType ParseUsage()
    {
        if (Match(TokenKind.CompKeyword))
            return UsageType.Binary;
        if (Match(TokenKind.Comp3Keyword))
            return UsageType.PackedDecimal;
        if (Match(TokenKind.DisplayKeyword))
            return UsageType.Display;
        if (Match(TokenKind.IndexKeyword))
            return UsageType.Index;
        if (Match(TokenKind.PointerKeyword))
            return UsageType.Pointer;
        if (Match(TokenKind.FunctionPointerKeyword))
            return UsageType.FunctionPointer;
        if (Match(TokenKind.ProcedurePointerKeyword))
            return UsageType.ProcedurePointer;
        // Default
        return UsageType.Display;
    }

    // ═══════════════════════════════════════════════════
    // PROCEDURE DIVISION (§14)
    // ═══════════════════════════════════════════════════

    private ProcedureDivision ParseProcedureDivision()
    {
        int start = Current.Span.Start;

        Expect(TokenKind.ProcedureKeyword);
        Expect(TokenKind.DivisionKeyword);
        Expect(TokenKind.Period);

        // Parse statements before the first paragraph (if any)
        var initialStatements = new List<Statement>();
        var paragraphs = new List<Paragraph>();

        while (Current.Kind != TokenKind.EndOfFile)
        {
            // Check if this is a paragraph header: IDENTIFIER followed by PERIOD
            if (IsParagraphHeader())
            {
                break; // start parsing paragraphs
            }

            // Skip stray periods
            if (Check(TokenKind.Period))
            {
                Advance();
                continue;
            }

            var stmt = ParseStatement();
            if (stmt != null)
                initialStatements.Add(stmt);
        }

        // Parse paragraphs
        while (Current.Kind != TokenKind.EndOfFile)
        {
            if (IsParagraphHeader())
            {
                paragraphs.Add(ParseParagraph());
            }
            else
            {
                break; // end of procedure division
            }
        }

        int end = Current.Span.Start;
        return new ProcedureDivision(initialStatements, paragraphs, TextSpan.FromBounds(start, end));
    }

    /// <summary>
    /// Checks if the current position looks like a paragraph header:
    /// an identifier (not a statement keyword) followed by a period.
    /// </summary>
    private bool IsParagraphHeader()
    {
        if (Current.Kind != TokenKind.Identifier) return false;
        return Peek().Kind == TokenKind.Period;
    }

    private Paragraph ParseParagraph()
    {
        int start = Current.Span.Start;
        string name = Advance().Text; // paragraph name
        Expect(TokenKind.Period);     // period after name

        var statements = new List<Statement>();
        while (Current.Kind != TokenKind.EndOfFile && !IsParagraphHeader())
        {
            if (Check(TokenKind.Period))
            {
                Advance();
                continue;
            }
            var stmt = ParseStatement();
            if (stmt != null)
                statements.Add(stmt);
        }

        int end = Current.Span.Start;
        return new Paragraph(name, statements, TextSpan.FromBounds(start, end));
    }

    private List<Statement> ParseStatements(params TokenKind[] terminators)
    {
        var statements = new List<Statement>();

        while (Current.Kind != TokenKind.EndOfFile)
        {
            // Check terminators
            foreach (var t in terminators)
                if (Check(t)) return statements;

            // Skip stray periods
            if (Check(TokenKind.Period))
            {
                Advance();
                continue;
            }

            var stmt = ParseStatement();
            if (stmt != null)
                statements.Add(stmt);
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
            _ => HandleUnknownStatement()
        };
    }

    private Statement? HandleUnknownStatement()
    {
        ReportError("CS0200", $"Unexpected token '{Current.Text}' — expected a statement");
        SkipToPeriodOrKeyword();
        return null;
    }

    // ── DISPLAY ──

    private DisplayStatement ParseDisplayStatement()
    {
        int start = Current.Span.Start;
        Advance(); // DISPLAY

        var operands = new List<Expression>();
        while (!Check(TokenKind.Period) && !Check(TokenKind.EndOfFile) &&
               !IsStatementStart(Current.Kind) && !IsScopeTerminator(Current.Kind))
        {
            operands.Add(ParseExpression());
        }
        Match(TokenKind.Period);

        return new DisplayStatement(operands, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── STOP RUN ──

    private StopRunStatement ParseStopStatement()
    {
        int start = Current.Span.Start;
        Advance(); // STOP
        Expect(TokenKind.RunKeyword, "Expected RUN after STOP");
        Match(TokenKind.Period);
        return new StopRunStatement(TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── MOVE ──

    private MoveStatement ParseMoveStatement()
    {
        int start = Current.Span.Start;
        Advance(); // MOVE

        var source = ParseExpression();
        Expect(TokenKind.ToKeyword, "Expected TO after MOVE source");

        var targets = new List<IdentifierExpression>();
        while (!Check(TokenKind.Period) && !Check(TokenKind.EndOfFile) && !IsStatementStart(Current.Kind) && !IsScopeTerminator(Current.Kind))
        {
            var id = Expect(TokenKind.Identifier, "Expected identifier after TO");
            targets.Add(new IdentifierExpression(id.Text, id.Span));
        }
        Match(TokenKind.Period);

        return new MoveStatement(source, targets, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── ADD ──

    private AddStatement ParseAddStatement()
    {
        int start = Current.Span.Start;
        Advance(); // ADD

        var operands = new List<Expression>();
        while (!Check(TokenKind.ToKeyword) && !Check(TokenKind.Period) && !Check(TokenKind.EndOfFile))
        {
            operands.Add(ParseExpression());
        }

        Expect(TokenKind.ToKeyword, "Expected TO in ADD statement");

        var targets = new List<IdentifierExpression>();
        while (!Check(TokenKind.Period) && !Check(TokenKind.EndOfFile) && !IsStatementStart(Current.Kind) && !IsScopeTerminator(Current.Kind))
        {
            var id = Expect(TokenKind.Identifier, "Expected identifier after TO");
            targets.Add(new IdentifierExpression(id.Text, id.Span));
        }
        Match(TokenKind.Period);

        return new AddStatement(operands, targets, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── SUBTRACT ──

    private SubtractStatement ParseSubtractStatement()
    {
        int start = Current.Span.Start;
        Advance(); // SUBTRACT

        var operands = new List<Expression>();
        while (!Check(TokenKind.FromKeyword) && !Check(TokenKind.Period) && !Check(TokenKind.EndOfFile))
        {
            operands.Add(ParseExpression());
        }

        Expect(TokenKind.FromKeyword, "Expected FROM in SUBTRACT statement");

        var targets = new List<IdentifierExpression>();
        while (!Check(TokenKind.Period) && !Check(TokenKind.EndOfFile) && !IsStatementStart(Current.Kind) && !IsScopeTerminator(Current.Kind))
        {
            var id = Expect(TokenKind.Identifier, "Expected identifier after FROM");
            targets.Add(new IdentifierExpression(id.Text, id.Span));
        }
        Match(TokenKind.Period);

        return new SubtractStatement(operands, targets, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── COMPUTE ──

    private ComputeStatement ParseComputeStatement()
    {
        int start = Current.Span.Start;
        Advance(); // COMPUTE

        var targetToken = Expect(TokenKind.Identifier, "Expected target identifier in COMPUTE");
        var target = new IdentifierExpression(targetToken.Text, targetToken.Span);

        Expect(TokenKind.Equals, "Expected = in COMPUTE statement");

        var value = ParseArithmeticExpression();
        Match(TokenKind.Period);

        return new ComputeStatement(target, value, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── IF ──

    private IfStatement ParseIfStatement()
    {
        int start = Current.Span.Start;
        Advance(); // IF

        var condition = ParseConditionExpression();

        // Optional THEN (not required in COBOL)
        // Parse then-statements until ELSE or END-IF
        var thenStatements = ParseStatements(TokenKind.ElseKeyword, TokenKind.EndIfKeyword);

        var elseStatements = new List<Statement>();
        if (Match(TokenKind.ElseKeyword))
        {
            elseStatements = ParseStatements(TokenKind.EndIfKeyword);
        }

        Expect(TokenKind.EndIfKeyword, "Expected END-IF");
        Match(TokenKind.Period);

        return new IfStatement(condition, thenStatements, elseStatements,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── PERFORM ──

    private PerformStatement ParsePerformStatement()
    {
        int start = Current.Span.Start;
        Advance(); // PERFORM

        // Inline PERFORM: PERFORM ... END-PERFORM
        // Out-of-line PERFORM: PERFORM paragraph-name
        // PERFORM n TIMES ... END-PERFORM
        // PERFORM UNTIL condition ... END-PERFORM

        Expression? times = null;
        Expression? until = null;

        // Check for UNTIL
        if (Check(TokenKind.UntilKeyword))
        {
            Advance();
            until = ParseConditionExpression();
            var body = ParseStatements(TokenKind.EndPerformKeyword);
            Expect(TokenKind.EndPerformKeyword);
            Match(TokenKind.Period);
            return new PerformStatement(null, body, null, until,
                TextSpan.FromBounds(start, Current.Span.Start));
        }

        // Check for integer literal followed by TIMES
        if ((Check(TokenKind.IntegerLiteral) || Check(TokenKind.Identifier)) &&
            Peek().Kind == TokenKind.TimesKeyword)
        {
            times = ParseExpression();
            Expect(TokenKind.TimesKeyword);
            var body = ParseStatements(TokenKind.EndPerformKeyword);
            Expect(TokenKind.EndPerformKeyword);
            Match(TokenKind.Period);
            return new PerformStatement(null, body, times, null,
                TextSpan.FromBounds(start, Current.Span.Start));
        }

        // Out-of-line: PERFORM paragraph-name
        if (Check(TokenKind.Identifier))
        {
            string name = Advance().Text;
            Match(TokenKind.Period);
            return new PerformStatement(name, new List<Statement>(), null, null,
                TextSpan.FromBounds(start, Current.Span.Start));
        }

        // Bare inline PERFORM ... END-PERFORM
        var stmts = ParseStatements(TokenKind.EndPerformKeyword);
        Expect(TokenKind.EndPerformKeyword);
        Match(TokenKind.Period);
        return new PerformStatement(null, stmts, null, null,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ═══════════════════════════════════════════════════
    // Expression parsing
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Parse a general expression (used in DISPLAY operands, MOVE source, VALUE, etc.)
    /// </summary>
    private Expression ParseExpression()
    {
        return ParsePrimaryExpression();
    }

    /// <summary>
    /// Parse an arithmetic expression with operator precedence (for COMPUTE).
    /// </summary>
    private Expression ParseArithmeticExpression()
    {
        return ParseAddSubtract();
    }

    private Expression ParseAddSubtract()
    {
        var left = ParseMultiplyDivide();
        while (Check(TokenKind.Plus) || Check(TokenKind.Minus))
        {
            var op = Current.Kind == TokenKind.Plus ? BinaryOperator.Add : BinaryOperator.Subtract;
            Advance();
            var right = ParseMultiplyDivide();
            left = new BinaryExpression(left, op, right,
                TextSpan.FromBounds(left.Span.Start, right.Span.End));
        }
        return left;
    }

    private Expression ParseMultiplyDivide()
    {
        var left = ParsePower();
        while (Check(TokenKind.Multiply) || Check(TokenKind.Divide))
        {
            var op = Current.Kind == TokenKind.Multiply ? BinaryOperator.Multiply : BinaryOperator.Divide;
            Advance();
            var right = ParsePower();
            left = new BinaryExpression(left, op, right,
                TextSpan.FromBounds(left.Span.Start, right.Span.End));
        }
        return left;
    }

    private Expression ParsePower()
    {
        var left = ParseUnary();
        if (Check(TokenKind.Power))
        {
            Advance();
            var right = ParsePower(); // right-associative
            return new BinaryExpression(left, BinaryOperator.Power, right,
                TextSpan.FromBounds(left.Span.Start, right.Span.End));
        }
        return left;
    }

    private Expression ParseUnary()
    {
        if (Check(TokenKind.Minus))
        {
            int start = Current.Span.Start;
            Advance();
            var operand = ParsePrimaryExpression();
            return new UnaryExpression(UnaryOperator.Negate, operand,
                TextSpan.FromBounds(start, operand.Span.End));
        }
        return ParsePrimaryExpression();
    }

    private Expression ParsePrimaryExpression()
    {
        var token = Current;

        switch (token.Kind)
        {
            case TokenKind.IntegerLiteral:
                Advance();
                return new NumericLiteralExpression(Convert.ToDecimal(token.Value), token.Span);

            case TokenKind.DecimalLiteral:
                Advance();
                return new NumericLiteralExpression((decimal)token.Value!, token.Span);

            case TokenKind.StringLiteral:
                Advance();
                return new StringLiteralExpression((string)token.Value!, token.Span);

            case TokenKind.Identifier:
                Advance();
                return new IdentifierExpression(token.Text, token.Span);

            case TokenKind.ZeroKeyword:
                Advance();
                return new FigurativeConstantExpression(FigurativeConstant.Zero, token.Span);

            case TokenKind.SpaceKeyword:
                Advance();
                return new FigurativeConstantExpression(FigurativeConstant.Space, token.Span);

            case TokenKind.HighValueKeyword:
                Advance();
                return new FigurativeConstantExpression(FigurativeConstant.HighValue, token.Span);

            case TokenKind.LowValueKeyword:
                Advance();
                return new FigurativeConstantExpression(FigurativeConstant.LowValue, token.Span);

            case TokenKind.QuoteKeyword:
                Advance();
                return new FigurativeConstantExpression(FigurativeConstant.Quote, token.Span);

            case TokenKind.LeftParen:
                Advance();
                var expr = ParseArithmeticExpression();
                Expect(TokenKind.RightParen, "Expected closing parenthesis");
                return expr;

            default:
                ReportError("CS0300", $"Expected expression, found '{token.Text}'");
                Advance();
                return new NumericLiteralExpression(0, token.Span); // error recovery
        }
    }

    /// <summary>
    /// Parse a condition expression (for IF, PERFORM UNTIL).
    /// Handles relational operators and simple comparisons.
    /// </summary>
    private Expression ParseConditionExpression()
    {
        return ParseOrExpression();
    }

    private Expression ParseOrExpression()
    {
        var left = ParseAndExpression();
        while (Check(TokenKind.OrKeyword))
        {
            Advance();
            var right = ParseAndExpression();
            left = new BinaryExpression(left, BinaryOperator.Or, right,
                TextSpan.FromBounds(left.Span.Start, right.Span.End));
        }
        return left;
    }

    private Expression ParseAndExpression()
    {
        var left = ParseNotExpression();
        while (Check(TokenKind.AndKeyword))
        {
            Advance();
            var right = ParseNotExpression();
            left = new BinaryExpression(left, BinaryOperator.And, right,
                TextSpan.FromBounds(left.Span.Start, right.Span.End));
        }
        return left;
    }

    private Expression ParseNotExpression()
    {
        if (Check(TokenKind.NotKeyword))
        {
            int start = Current.Span.Start;
            Advance();
            var operand = ParseRelationalExpression();
            return new UnaryExpression(UnaryOperator.Not, operand,
                TextSpan.FromBounds(start, operand.Span.End));
        }
        return ParseRelationalExpression();
    }

    private Expression ParseRelationalExpression()
    {
        var left = ParseArithmeticExpression();

        // Check for relational operator
        BinaryOperator? op = TryParseRelationalOperator();
        if (op.HasValue)
        {
            var right = ParseArithmeticExpression();
            return new BinaryExpression(left, op.Value, right,
                TextSpan.FromBounds(left.Span.Start, right.Span.End));
        }

        return left;
    }

    private BinaryOperator? TryParseRelationalOperator()
    {
        // = , < , > , <= , >= , NOT = , NOT < , NOT >
        // EQUAL TO, GREATER THAN, LESS THAN, etc.
        bool negated = false;

        if (Check(TokenKind.IsKeyword))
            Advance(); // skip optional IS

        if (Check(TokenKind.NotKeyword))
        {
            negated = true;
            Advance();
        }

        BinaryOperator? op = null;

        if (Match(TokenKind.Equals) || Match(TokenKind.EqualKeyword))
        {
            Match(TokenKind.ToKeyword); // optional TO
            op = BinaryOperator.Equal;
        }
        else if (Match(TokenKind.LessThan))
        {
            op = BinaryOperator.LessThan;
        }
        else if (Match(TokenKind.GreaterThan))
        {
            op = BinaryOperator.GreaterThan;
        }
        else if (Match(TokenKind.LessThanOrEqual))
        {
            op = BinaryOperator.LessThanOrEqual;
        }
        else if (Match(TokenKind.GreaterThanOrEqual))
        {
            op = BinaryOperator.GreaterThanOrEqual;
        }
        else if (Match(TokenKind.GreaterKeyword))
        {
            Match(TokenKind.ThanKeyword); // optional THAN
            Match(TokenKind.OrKeyword); // GREATER THAN OR EQUAL TO
            if (Match(TokenKind.EqualKeyword))
            {
                Match(TokenKind.ToKeyword);
                op = BinaryOperator.GreaterThanOrEqual;
            }
            else
            {
                op = BinaryOperator.GreaterThan;
            }
        }
        else if (Match(TokenKind.LessKeyword))
        {
            Match(TokenKind.ThanKeyword);
            Match(TokenKind.OrKeyword);
            if (Match(TokenKind.EqualKeyword))
            {
                Match(TokenKind.ToKeyword);
                op = BinaryOperator.LessThanOrEqual;
            }
            else
            {
                op = BinaryOperator.LessThan;
            }
        }

        if (op.HasValue && negated)
        {
            op = op.Value switch
            {
                BinaryOperator.Equal => BinaryOperator.NotEqual,
                BinaryOperator.LessThan => BinaryOperator.GreaterThanOrEqual,
                BinaryOperator.GreaterThan => BinaryOperator.LessThanOrEqual,
                BinaryOperator.LessThanOrEqual => BinaryOperator.GreaterThan,
                BinaryOperator.GreaterThanOrEqual => BinaryOperator.LessThan,
                _ => op.Value
            };
        }

        return op;
    }
}
