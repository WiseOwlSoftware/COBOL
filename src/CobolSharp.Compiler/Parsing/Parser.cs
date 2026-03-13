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
        TokenKind.ContinueKeyword or TokenKind.ExitKeyword or TokenKind.InitializeKeyword or
        TokenKind.StringKeyword or TokenKind.UnstringKeyword or TokenKind.InspectKeyword or
        TokenKind.OpenKeyword or TokenKind.CloseKeyword or TokenKind.ReadKeyword or
        TokenKind.WriteKeyword or TokenKind.RewriteKeyword or TokenKind.DeleteKeyword or
        TokenKind.StartKeyword or TokenKind.SortKeyword;

    private static bool IsScopeTerminator(TokenKind kind) => kind is
        TokenKind.ElseKeyword or TokenKind.EndIfKeyword or TokenKind.EndPerformKeyword or
        TokenKind.EndEvaluateKeyword or TokenKind.WhenKeyword or
        TokenKind.EndReadKeyword or TokenKind.EndWriteKeyword or
        TokenKind.EndDeleteKeyword or TokenKind.EndStartKeyword;

    // ═══════════════════════════════════════════════════
    // Top-level parsing
    // ═══════════════════════════════════════════════════

    public CompilationUnit ParseCompilationUnit()
    {
        int start = Current.Span.Start;
        var programs = new List<ProgramNode>();

        while (Current.Kind != TokenKind.EndOfFile)
        {
            if (Check(TokenKind.IdentificationKeyword))
            {
                programs.Add(ParseProgram());
            }
            else
            {
                break;
            }
        }

        if (programs.Count == 0)
        {
            ReportError("CS0050", "Expected at least one program");
        }

        int end = Current.Span.End;
        return new CompilationUnit(programs, TextSpan.FromBounds(start, end));
    }

    private ProgramNode ParseProgram()
    {
        int start = Current.Span.Start;

        var identification = ParseIdentificationDivision();

        EnvironmentDivision? environment = null;
        if (Check(TokenKind.EnvironmentKeyword))
            environment = ParseEnvironmentDivision();

        DataDivision? data = null;
        if (Check(TokenKind.DataKeyword))
            data = ParseDataDivision();

        ProcedureDivision? procedure = null;
        if (Check(TokenKind.ProcedureKeyword))
            procedure = ParseProcedureDivision();

        // Optional END PROGRAM program-name.
        if (Check(TokenKind.Identifier) && Current.Text.Equals("END", StringComparison.OrdinalIgnoreCase))
        {
            Advance(); // END
            if (Check(TokenKind.Identifier) && Current.Text.Equals("PROGRAM", StringComparison.OrdinalIgnoreCase))
            {
                Advance(); // PROGRAM
                if (Check(TokenKind.Identifier)) Advance(); // program-name
                Match(TokenKind.Period);
            }
        }

        int end = Current.Span.End;
        return new ProgramNode(identification, environment, data, procedure, TextSpan.FromBounds(start, end));
    }

    private void SkipDivision()
    {
        Advance(); // keyword
        if (Check(TokenKind.DivisionKeyword)) Advance();
        if (Check(TokenKind.Period)) Advance();
        while (Current.Kind != TokenKind.EndOfFile && !IsDivisionKeyword(Current.Kind))
            Advance();
    }

    // ═══════════════════════════════════════════════════
    // ENVIRONMENT DIVISION (§12)
    // ═══════════════════════════════════════════════════

    private EnvironmentDivision ParseEnvironmentDivision()
    {
        int start = Current.Span.Start;
        Expect(TokenKind.EnvironmentKeyword);
        Expect(TokenKind.DivisionKeyword);
        Expect(TokenKind.Period);

        FileControlSection? fileControl = null;

        // Parse sections until next division
        while (Current.Kind != TokenKind.EndOfFile && !IsDivisionKeyword(Current.Kind))
        {
            if (Check(TokenKind.InputKeyword) || Check(TokenKind.I_OKeyword))
            {
                // INPUT-OUTPUT SECTION.
                Advance(); // INPUT or I-O
                if (Current.Text.Equals("OUTPUT", StringComparison.OrdinalIgnoreCase) ||
                    Current.Text.Equals("-OUTPUT", StringComparison.OrdinalIgnoreCase))
                    Advance();
                Match(TokenKind.SectionKeyword);
                Match(TokenKind.Period);
                continue;
            }

            if (Check(TokenKind.FileKeyword))
            {
                // FILE-CONTROL.
                Advance(); // FILE
                // Skip "-CONTROL" which would be lexed as part of the identifier
                // Actually, "FILE-CONTROL" is a single hyphenated word lexed as an identifier
                // But "FILE" is now a keyword. The text after might be "-CONTROL" or we might
                // see FILE-CONTROL as a single token depending on lexer behavior.
                // Let's handle both cases:
                if (Check(TokenKind.Identifier) && Current.Text.Equals("-CONTROL", StringComparison.OrdinalIgnoreCase))
                    Advance();
                Match(TokenKind.Period);
                fileControl = ParseFileControlSection();
                continue;
            }

            // FILE-CONTROL as a single identifier (if the lexer produces it that way)
            if (Check(TokenKind.Identifier) &&
                Current.Text.Equals("FILE-CONTROL", StringComparison.OrdinalIgnoreCase))
            {
                Advance();
                Match(TokenKind.Period);
                fileControl = ParseFileControlSection();
                continue;
            }

            // I-O-CONTROL or CONFIGURATION SECTION — skip for now
            if (Check(TokenKind.Identifier))
            {
                string text = Current.Text.ToUpperInvariant();
                if (text == "CONFIGURATION" || text == "I-O-CONTROL" || text == "INPUT-OUTPUT")
                {
                    Advance();
                    Match(TokenKind.SectionKeyword);
                    Match(TokenKind.Period);
                    // Skip section contents
                    while (Current.Kind != TokenKind.EndOfFile && !IsDivisionKeyword(Current.Kind) &&
                           !(Check(TokenKind.FileKeyword)) &&
                           !(Check(TokenKind.Identifier) && Current.Text.Equals("FILE-CONTROL", StringComparison.OrdinalIgnoreCase)))
                        Advance();
                    continue;
                }
            }

            // Skip unrecognized tokens
            Advance();
        }

        int end = Current.Span.Start;
        return new EnvironmentDivision(fileControl, TextSpan.FromBounds(start, end));
    }

    private FileControlSection ParseFileControlSection()
    {
        int start = Current.Span.Start;
        var entries = new List<FileControlEntry>();

        while (Check(TokenKind.SelectKeyword))
        {
            entries.Add(ParseSelectEntry());
        }

        int end = Current.Span.Start;
        return new FileControlSection(entries, TextSpan.FromBounds(start, end));
    }

    private FileControlEntry ParseSelectEntry()
    {
        int start = Current.Span.Start;
        Advance(); // SELECT

        // Optional OPTIONAL keyword
        if (Check(TokenKind.Identifier) && Current.Text.Equals("OPTIONAL", StringComparison.OrdinalIgnoreCase))
            Advance();

        var fileNameToken = Expect(TokenKind.Identifier, "Expected file name after SELECT");
        string fileName = fileNameToken.Text;

        // ASSIGN TO external-name
        Expect(TokenKind.AssignKeyword, "Expected ASSIGN after file name");
        Match(TokenKind.ToKeyword); // optional TO
        string assignTo = "";
        if (Check(TokenKind.Identifier) || Check(TokenKind.StringLiteral))
        {
            var assignToken = Advance();
            assignTo = assignToken.Kind == TokenKind.StringLiteral
                ? (string)assignToken.Value! : assignToken.Text;
        }

        var organization = FileOrganization.Sequential;
        var accessMode = AccessMode.Sequential;
        string? recordKey = null;
        var alternateKeys = new List<AlternateKeyInfo>();
        string? relativeKey = null;
        string? fileStatus = null;

        // Parse optional clauses until period
        while (!Check(TokenKind.Period) && Current.Kind != TokenKind.EndOfFile &&
               !Check(TokenKind.SelectKeyword))
        {
            if (Check(TokenKind.OrganizationKeyword))
            {
                Advance();
                Match(TokenKind.IsKeyword);
                organization = ParseFileOrganization();
            }
            else if (Check(TokenKind.AccessKeyword))
            {
                Advance();
                Match(TokenKind.ModeKeyword);
                Match(TokenKind.IsKeyword);
                accessMode = ParseAccessMode();
            }
            else if (Check(TokenKind.RecordKeyword))
            {
                Advance();
                Match(TokenKind.KeyKeyword);
                Match(TokenKind.IsKeyword);
                var keyToken = Expect(TokenKind.Identifier, "Expected key name");
                recordKey = keyToken.Text;
            }
            else if (Check(TokenKind.AlternateKeyword))
            {
                Advance(); // ALTERNATE
                Match(TokenKind.RecordKeyword);
                Match(TokenKind.KeyKeyword);
                Match(TokenKind.IsKeyword);
                var keyToken = Expect(TokenKind.Identifier, "Expected alternate key name");
                bool dupes = false;
                if (Check(TokenKind.WithKeyword))
                {
                    Advance();
                    Match(TokenKind.DuplicatesKeyword);
                    dupes = true;
                }
                alternateKeys.Add(new AlternateKeyInfo(keyToken.Text, dupes));
            }
            else if (Check(TokenKind.RelativeKeyword))
            {
                Advance();
                Match(TokenKind.KeyKeyword);
                Match(TokenKind.IsKeyword);
                var keyToken = Expect(TokenKind.Identifier, "Expected relative key name");
                relativeKey = keyToken.Text;
            }
            else if (Check(TokenKind.FileKeyword))
            {
                // FILE STATUS IS
                Advance();
                Match(TokenKind.StatusKeyword);
                Match(TokenKind.IsKeyword);
                var statusToken = Expect(TokenKind.Identifier, "Expected status name");
                fileStatus = statusToken.Text;
            }
            else if (Check(TokenKind.StatusKeyword))
            {
                // STATUS IS (without FILE)
                Advance();
                Match(TokenKind.IsKeyword);
                var statusToken = Expect(TokenKind.Identifier, "Expected status name");
                fileStatus = statusToken.Text;
            }
            else
            {
                Advance(); // skip unrecognized clause words
            }
        }

        Match(TokenKind.Period);
        int end = Current.Span.Start;

        return new FileControlEntry(fileName, assignTo, organization, accessMode,
            recordKey, alternateKeys, relativeKey, fileStatus,
            TextSpan.FromBounds(start, end));
    }

    private FileOrganization ParseFileOrganization()
    {
        if (Match(TokenKind.SequentialKeyword)) return FileOrganization.Sequential;
        if (Check(TokenKind.LineKeyword))
        {
            Advance();
            Match(TokenKind.SequentialKeyword);
            return FileOrganization.LineSequential;
        }
        if (Check(TokenKind.IndexedKeyword)) { Advance(); return FileOrganization.Indexed; }
        if (Match(TokenKind.RelativeKeyword)) return FileOrganization.Relative;
        return FileOrganization.Sequential;
    }

    private AccessMode ParseAccessMode()
    {
        if (Match(TokenKind.SequentialKeyword)) return AccessMode.Sequential;
        if (Match(TokenKind.RandomKeyword)) return AccessMode.Random;
        if (Match(TokenKind.DynamicKeyword)) return AccessMode.Dynamic;
        return AccessMode.Sequential;
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

        FileSection? fileSection = null;
        WorkingStorageSection? ws = null;
        LinkageSection? linkage = null;

        // Parse sections in order (FILE, WORKING-STORAGE, LINKAGE, etc.)
        while (Current.Kind != TokenKind.EndOfFile && !IsDivisionKeyword(Current.Kind))
        {
            if (Check(TokenKind.FileKeyword))
            {
                fileSection = ParseFileSection();
            }
            else if (Check(TokenKind.WorkingStorageKeyword))
            {
                ws = ParseWorkingStorageSection();
            }
            else if (Check(TokenKind.LinkageKeyword))
            {
                linkage = ParseLinkageSection();
            }
            else
            {
                // Skip unrecognized section header (e.g., LOCAL-STORAGE, SCREEN SECTION)
                Advance();
                if (Check(TokenKind.SectionKeyword)) Advance();
                if (Check(TokenKind.Period)) Advance();
                while (Current.Kind != TokenKind.EndOfFile && !IsDivisionKeyword(Current.Kind) &&
                       !Check(TokenKind.WorkingStorageKeyword) && !Check(TokenKind.LinkageKeyword) &&
                       !Check(TokenKind.FileKeyword))
                    Advance();
            }
        }

        int end = Current.Span.Start;
        return new DataDivision(fileSection, ws, linkage, TextSpan.FromBounds(start, end));
    }

    private FileSection ParseFileSection()
    {
        int start = Current.Span.Start;

        Expect(TokenKind.FileKeyword);
        Expect(TokenKind.SectionKeyword);
        Expect(TokenKind.Period);

        var entries = new List<FileDescriptionEntry>();
        while (Check(TokenKind.FdKeyword) || Check(TokenKind.SdKeyword))
        {
            entries.Add(ParseFileDescriptionEntry());
        }

        int end = Current.Span.Start;
        return new FileSection(entries, TextSpan.FromBounds(start, end));
    }

    private FileDescriptionEntry ParseFileDescriptionEntry()
    {
        int start = Current.Span.Start;
        bool isSD = Check(TokenKind.SdKeyword);
        Advance(); // FD or SD

        var fileNameToken = Expect(TokenKind.Identifier, "Expected file name after FD/SD");
        string fileName = fileNameToken.Text;

        int blockContains = 0;
        int recordMin = 0;
        int recordMax = 0;

        // Parse FD clauses until period
        while (!Check(TokenKind.Period) && Current.Kind != TokenKind.EndOfFile)
        {
            if (Check(TokenKind.BlockKeyword))
            {
                Advance();
                Match(TokenKind.ContainsKeyword);
                if (Check(TokenKind.IntegerLiteral))
                    blockContains = (int)(long)Advance().Value!;
                // skip optional RECORDS/CHARACTERS
                if (Check(TokenKind.RecordsKeyword) || Check(TokenKind.Identifier))
                    Advance();
            }
            else if (Check(TokenKind.RecordKeyword))
            {
                Advance();
                Match(TokenKind.ContainsKeyword);
                if (Check(TokenKind.IntegerLiteral))
                {
                    recordMin = (int)(long)Advance().Value!;
                    recordMax = recordMin;
                    // TO max
                    if (Check(TokenKind.ToKeyword))
                    {
                        Advance();
                        if (Check(TokenKind.IntegerLiteral))
                            recordMax = (int)(long)Advance().Value!;
                    }
                }
                // skip optional CHARACTERS
                if (Check(TokenKind.Identifier)) Advance();
            }
            else if (Check(TokenKind.LabelKeyword))
            {
                // LABEL RECORDS ARE STANDARD/OMITTED — archaic, skip
                Advance();
                while (!Check(TokenKind.Period) && Current.Kind != TokenKind.EndOfFile &&
                       !Check(TokenKind.RecordKeyword) && !Check(TokenKind.BlockKeyword) &&
                       !Check(TokenKind.LinageKeyword) && !Check(TokenKind.DataKeyword))
                    Advance();
            }
            else
            {
                Advance(); // skip unrecognized clause
            }
        }

        Expect(TokenKind.Period);

        // Parse record descriptions (01-level entries under this FD)
        var records = new List<DataDescriptionEntry>();
        while (IsLevelNumber())
        {
            records.Add(ParseDataDescriptionEntry());
        }

        int end = Current.Span.Start;
        return new FileDescriptionEntry(isSD, fileName, blockContains, recordMin, recordMax,
            records, TextSpan.FromBounds(start, end));
    }

    private LinkageSection ParseLinkageSection()
    {
        int start = Current.Span.Start;
        Expect(TokenKind.LinkageKeyword);
        Expect(TokenKind.SectionKeyword);
        Expect(TokenKind.Period);

        var entries = new List<DataDescriptionEntry>();
        while (IsLevelNumber())
        {
            entries.Add(ParseDataDescriptionEntry());
        }

        int end = Current.Span.Start;
        return new LinkageSection(entries, TextSpan.FromBounds(start, end));
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

        // Parse statements before the first paragraph/section (if any)
        var initialStatements = new List<Statement>();
        var paragraphs = new List<Paragraph>();
        var sections = new List<Section>();

        while (Current.Kind != TokenKind.EndOfFile)
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
            var stmt = ParseStatement();
            if (stmt != null)
                initialStatements.Add(stmt);
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
        if (Current.Kind != TokenKind.Identifier) return false;
        return Peek().Kind == TokenKind.Period;
    }

    private bool IsSectionHeader()
    {
        if (Current.Kind != TokenKind.Identifier) return false;
        return Peek().Kind == TokenKind.SectionKeyword;
    }

    private Section ParseSection()
    {
        int start = Current.Span.Start;
        string name = Advance().Text; // section name
        Expect(TokenKind.SectionKeyword);
        Expect(TokenKind.Period);

        var paragraphs = new List<Paragraph>();
        while (Current.Kind != TokenKind.EndOfFile && !IsSectionHeader())
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

    private Paragraph ParseParagraph(string? sectionName)
    {
        int start = Current.Span.Start;
        string name = Advance().Text;
        Expect(TokenKind.Period);

        var statements = new List<Statement>();
        while (Current.Kind != TokenKind.EndOfFile &&
               !IsParagraphHeader() && !IsSectionHeader())
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
        return new Paragraph(name, statements, TextSpan.FromBounds(start, end), sectionName);
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
            TokenKind.GoKeyword => ParseGoToStatement(),
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

        // Out-of-line: PERFORM paragraph-name [THRU paragraph-name]
        if (Check(TokenKind.Identifier))
        {
            string name = Advance().Text;
            string? thruName = null;
            if (Match(TokenKind.ThruKeyword))
            {
                var thruToken = Expect(TokenKind.Identifier, "Expected paragraph name after THRU");
                thruName = thruToken.Text;
            }
            Match(TokenKind.Period);
            return new PerformStatement(name, new List<Statement>(), null, null,
                TextSpan.FromBounds(start, Current.Span.Start), thruParagraphName: thruName);
        }

        // Bare inline PERFORM ... END-PERFORM
        var stmts = ParseStatements(TokenKind.EndPerformKeyword);
        Expect(TokenKind.EndPerformKeyword);
        Match(TokenKind.Period);
        return new PerformStatement(null, stmts, null, null,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── GO TO ──

    private Statement ParseGoToStatement()
    {
        int start = Current.Span.Start;
        Advance(); // GO
        Match(TokenKind.ToKeyword); // optional TO

        var names = new List<string>();
        while (Check(TokenKind.Identifier))
        {
            names.Add(Advance().Text);
        }

        // GO TO ... DEPENDING ON
        if (Check(TokenKind.DependingKeyword))
        {
            Advance();
            Match(TokenKind.OnKeyword);
            var expr = ParseExpression();
            Match(TokenKind.Period);
            return new GoToDependingStatement(names, expr,
                TextSpan.FromBounds(start, Current.Span.Start));
        }

        Match(TokenKind.Period);

        if (names.Count == 0)
        {
            ReportError("CS0210", "GO TO requires a paragraph name");
            return new ContinueStatement(TextSpan.FromBounds(start, Current.Span.Start));
        }

        return new GoToStatement(names[0], TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── CONTINUE ──

    private ContinueStatement ParseContinueStatement()
    {
        int start = Current.Span.Start;
        Advance(); // CONTINUE
        Match(TokenKind.Period);
        return new ContinueStatement(TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── EXIT ──

    private ExitStatement ParseExitStatement()
    {
        int start = Current.Span.Start;
        Advance(); // EXIT

        ExitType kind = ExitType.Paragraph; // default
        if (Check(TokenKind.Identifier))
        {
            string word = Current.Text.ToUpperInvariant();
            if (word == "PARAGRAPH") { Advance(); kind = ExitType.Paragraph; }
            else if (word == "SECTION") { Advance(); kind = ExitType.Section; }
            else if (word == "PROGRAM") { Advance(); kind = ExitType.Program; }
            else if (word == "PERFORM") { Advance(); kind = ExitType.Perform; }
        }
        else if (Check(TokenKind.ProcedureKeyword))
        {
            // EXIT PROGRAM uses PROGRAM which isn't a generic identifier
        }

        Match(TokenKind.Period);
        return new ExitStatement(kind, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── ACCEPT ──

    private AcceptStatement ParseAcceptStatement()
    {
        int start = Current.Span.Start;
        Advance(); // ACCEPT

        var targetToken = Expect(TokenKind.Identifier, "Expected identifier after ACCEPT");
        var target = new IdentifierExpression(targetToken.Text, targetToken.Span);

        string? fromSource = null;
        if (Check(TokenKind.FromKeyword))
        {
            Advance();
            if (Check(TokenKind.Identifier))
            {
                fromSource = Advance().Text.ToUpperInvariant();
            }
        }

        Match(TokenKind.Period);
        return new AcceptStatement(target, fromSource,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── INITIALIZE ──

    private InitializeStatement ParseInitializeStatement()
    {
        int start = Current.Span.Start;
        Advance(); // INITIALIZE

        var targets = new List<IdentifierExpression>();
        while (Check(TokenKind.Identifier))
        {
            var tok = Advance();
            targets.Add(new IdentifierExpression(tok.Text, tok.Span));
        }

        Match(TokenKind.Period);
        return new InitializeStatement(targets,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── File I/O Statements ──

    private OpenStatement ParseOpenStatement()
    {
        int start = Current.Span.Start;
        Advance(); // OPEN

        var clauses = new List<OpenClause>();
        while (!Check(TokenKind.Period) && Current.Kind != TokenKind.EndOfFile &&
               !IsStatementStart(Current.Kind) && !IsScopeTerminator(Current.Kind))
        {
            OpenMode mode;
            if (Match(TokenKind.InputKeyword)) mode = OpenMode.Input;
            else if (Match(TokenKind.OutputKeyword)) mode = OpenMode.Output;
            else if (Check(TokenKind.I_OKeyword)) { Advance(); mode = OpenMode.InputOutput; }
            else if (Match(TokenKind.ExtendKeyword)) mode = OpenMode.Extend;
            else break;

            var fileNames = new List<string>();
            while (Check(TokenKind.Identifier))
            {
                fileNames.Add(Advance().Text);
            }
            clauses.Add(new OpenClause(mode, fileNames));
        }

        Match(TokenKind.Period);
        return new OpenStatement(clauses, TextSpan.FromBounds(start, Current.Span.Start));
    }

    private CloseStatement ParseCloseStatement()
    {
        int start = Current.Span.Start;
        Advance(); // CLOSE
        var fileNames = new List<string>();
        while (Check(TokenKind.Identifier))
            fileNames.Add(Advance().Text);
        Match(TokenKind.Period);
        return new CloseStatement(fileNames, TextSpan.FromBounds(start, Current.Span.Start));
    }

    private ReadStatement ParseReadStatement()
    {
        int start = Current.Span.Start;
        Advance(); // READ

        var fileNameToken = Expect(TokenKind.Identifier, "Expected file name after READ");
        string fileName = fileNameToken.Text;

        // Optional NEXT RECORD
        Match(TokenKind.NextKeyword);
        if (Check(TokenKind.RecordKeyword)) Advance();

        // INTO identifier
        IdentifierExpression? into = null;
        if (Match(TokenKind.IntoKeyword))
        {
            var intoToken = Expect(TokenKind.Identifier, "Expected identifier after INTO");
            into = new IdentifierExpression(intoToken.Text, intoToken.Span);
        }

        // KEY IS
        Expression? keyIs = null;
        if (Check(TokenKind.KeyKeyword))
        {
            Advance();
            Match(TokenKind.IsKeyword);
            keyIs = ParseExpression();
        }

        // AT END / NOT AT END
        var atEnd = new List<Statement>();
        var notAtEnd = new List<Statement>();

        if (Check(TokenKind.AtKeyword))
        {
            Advance(); // AT
            Match(TokenKind.EndKeyword);
            atEnd = ParseStatements(TokenKind.NotKeyword, TokenKind.EndReadKeyword);
        }
        if (Check(TokenKind.NotKeyword) && Peek().Kind == TokenKind.AtKeyword)
        {
            Advance(); Advance(); // NOT AT
            Match(TokenKind.EndKeyword);
            notAtEnd = ParseStatements(TokenKind.EndReadKeyword);
        }
        // INVALID KEY / NOT INVALID KEY
        if (Check(TokenKind.InvalidKeyword))
        {
            Advance(); Match(TokenKind.KeyKeyword);
            atEnd = ParseStatements(TokenKind.NotKeyword, TokenKind.EndReadKeyword);
        }

        Match(TokenKind.EndReadKeyword);
        Match(TokenKind.Period);

        return new ReadStatement(fileName, into, atEnd, notAtEnd, keyIs,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    private WriteStatement ParseWriteStatement()
    {
        int start = Current.Span.Start;
        Advance(); // WRITE

        var recToken = Expect(TokenKind.Identifier, "Expected record name after WRITE");
        var recordName = new IdentifierExpression(recToken.Text, recToken.Span);

        Expression? from = null;
        if (Match(TokenKind.FromKeyword))
            from = ParseExpression();

        WriteAdvancing? advancing = null;
        if (Check(TokenKind.BeforeKeyword) || Check(TokenKind.AfterKeyword))
        {
            bool isBefore = Check(TokenKind.BeforeKeyword);
            Advance();
            Match(TokenKind.AdvancingKeyword);
            Expression? lines = null;
            if (Check(TokenKind.PageKeyword))
            {
                Advance();
            }
            else if (Check(TokenKind.IntegerLiteral) || Check(TokenKind.Identifier))
            {
                lines = ParseExpression();
                if (Check(TokenKind.LineKeyword) || Check(TokenKind.Identifier)) Advance(); // LINES
            }
            advancing = new WriteAdvancing(isBefore, lines);
        }

        // Skip INVALID KEY / END-WRITE
        SkipToEndOfStatement();
        return new WriteStatement(recordName, from, advancing,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    private RewriteStatement ParseRewriteStatement()
    {
        int start = Current.Span.Start;
        Advance(); // REWRITE
        var recToken = Expect(TokenKind.Identifier, "Expected record name");
        var recordName = new IdentifierExpression(recToken.Text, recToken.Span);
        Expression? from = null;
        if (Match(TokenKind.FromKeyword))
            from = ParseExpression();
        SkipToEndOfStatement();
        return new RewriteStatement(recordName, from, TextSpan.FromBounds(start, Current.Span.Start));
    }

    private DeleteStatement ParseDeleteStatement()
    {
        int start = Current.Span.Start;
        Advance(); // DELETE
        var fileToken = Expect(TokenKind.Identifier, "Expected file name");
        Match(TokenKind.RecordKeyword); // optional RECORD
        SkipToEndOfStatement();
        return new DeleteStatement(fileToken.Text, TextSpan.FromBounds(start, Current.Span.Start));
    }

    private StartStatement ParseStartStatement()
    {
        int start = Current.Span.Start;
        Advance(); // START
        var fileToken = Expect(TokenKind.Identifier, "Expected file name");
        BinaryOperator? keyCondition = null;
        Expression? keyIs = null;
        if (Check(TokenKind.KeyKeyword))
        {
            Advance();
            Match(TokenKind.IsKeyword);
            keyCondition = TryParseRelationalOperator();
            keyIs = ParseExpression();
        }
        SkipToEndOfStatement();
        return new StartStatement(fileToken.Text, keyCondition, keyIs,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── SORT ──

    private SortStatement ParseSortStatement()
    {
        int start = Current.Span.Start;
        Advance(); // SORT

        var fileToken = Expect(TokenKind.Identifier, "Expected sort file name");
        var keys = new List<SortKey>();

        // ON ASCENDING/DESCENDING KEY
        while (Check(TokenKind.OnKeyword) || Check(TokenKind.AscendingKeyword) || Check(TokenKind.DescendingKeyword))
        {
            Match(TokenKind.OnKeyword);
            bool asc = true;
            if (Match(TokenKind.DescendingKeyword)) asc = false;
            else Match(TokenKind.AscendingKeyword);
            Match(TokenKind.KeyKeyword);
            Match(TokenKind.IsKeyword);
            while (Check(TokenKind.Identifier) &&
                   !Current.Text.Equals("INPUT", StringComparison.OrdinalIgnoreCase) &&
                   !Current.Text.Equals("OUTPUT", StringComparison.OrdinalIgnoreCase) &&
                   !Current.Text.Equals("USING", StringComparison.OrdinalIgnoreCase) &&
                   !Current.Text.Equals("GIVING", StringComparison.OrdinalIgnoreCase))
            {
                keys.Add(new SortKey(asc, Advance().Text));
            }
        }

        string? inputProc = null, usingFile = null, outputProc = null, givingFile = null;

        // INPUT PROCEDURE / USING
        if (Check(TokenKind.InputKeyword))
        {
            Advance();
            if (Check(TokenKind.Identifier) && Current.Text.Equals("PROCEDURE", StringComparison.OrdinalIgnoreCase))
            {
                Advance();
                Match(TokenKind.IsKeyword);
                inputProc = Expect(TokenKind.Identifier, "Expected procedure name").Text;
            }
        }
        else if (Check(TokenKind.UsingKeyword))
        {
            Advance();
            usingFile = Expect(TokenKind.Identifier, "Expected file name after USING").Text;
        }

        // OUTPUT PROCEDURE / GIVING
        if (Check(TokenKind.OutputKeyword))
        {
            Advance();
            if (Check(TokenKind.Identifier) && Current.Text.Equals("PROCEDURE", StringComparison.OrdinalIgnoreCase))
            {
                Advance();
                Match(TokenKind.IsKeyword);
                outputProc = Expect(TokenKind.Identifier, "Expected procedure name").Text;
            }
        }
        else if (Check(TokenKind.GivingKeyword))
        {
            Advance();
            givingFile = Expect(TokenKind.Identifier, "Expected file name after GIVING").Text;
        }

        Match(TokenKind.Period);
        return new SortStatement(fileToken.Text, keys, inputProc, usingFile, outputProc, givingFile,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── CALL ──

    private CallStatement ParseCallStatement()
    {
        int start = Current.Span.Start;
        Advance(); // CALL

        // Program name: literal or identifier
        var programName = ParseExpression();

        // USING clause
        var parameters = new List<CallParameter>();
        if (Match(TokenKind.UsingKeyword))
        {
            var convention = CallConvention.ByReference; // default
            while (!Check(TokenKind.Period) && !Check(TokenKind.ReturningKeyword) &&
                   !Check(TokenKind.OnKeyword) && Current.Kind != TokenKind.EndOfFile &&
                   !IsStatementStart(Current.Kind) && !IsScopeTerminator(Current.Kind))
            {
                // Check for BY REFERENCE/CONTENT/VALUE
                if (Match(TokenKind.ByKeyword))
                {
                    if (Match(TokenKind.ReferenceKeyword))
                        convention = CallConvention.ByReference;
                    else if (Match(TokenKind.ContentKeyword))
                        convention = CallConvention.ByContent;
                    else if (Check(TokenKind.ValueKeyword) || Check(TokenKind.ValuesKeyword))
                    {
                        Advance();
                        convention = CallConvention.ByValue;
                    }
                }

                var paramExpr = ParseExpression();
                parameters.Add(new CallParameter(paramExpr, convention));
            }
        }

        // RETURNING clause
        IdentifierExpression? returning = null;
        if (Match(TokenKind.ReturningKeyword))
        {
            var retToken = Expect(TokenKind.Identifier, "Expected identifier after RETURNING");
            returning = new IdentifierExpression(retToken.Text, retToken.Span);
        }

        // Skip ON EXCEPTION / NOT ON EXCEPTION
        SkipToEndOfStatement();

        return new CallStatement(programName, parameters, returning,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── CANCEL ──

    private CancelStatement ParseCancelStatement()
    {
        int start = Current.Span.Start;
        Advance(); // CANCEL
        var programName = ParseExpression();
        Match(TokenKind.Period);
        return new CancelStatement(programName,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── STRING ──

    private StringStatement ParseStringStatement()
    {
        int start = Current.Span.Start;
        Advance(); // STRING

        var sources = new List<StringSource>();
        while (!Check(TokenKind.IntoKeyword) && Current.Kind != TokenKind.EndOfFile &&
               Current.Kind != TokenKind.Period)
        {
            var value = ParseExpression();
            Expression? delim = null;

            // DELIMITED BY SIZE or DELIMITED BY literal/identifier
            if (Check(TokenKind.Identifier) && Current.Text.Equals("DELIMITED", StringComparison.OrdinalIgnoreCase))
            {
                Advance(); // DELIMITED
                Match(TokenKind.ByKeyword); // BY
                if (Check(TokenKind.SizeKeyword))
                {
                    Advance(); // SIZE
                    delim = null; // SIZE means use full value
                }
                else
                {
                    delim = ParseExpression();
                }
            }

            sources.Add(new StringSource(value, delim));
        }

        Expect(TokenKind.IntoKeyword, "Expected INTO in STRING statement");
        var targetToken = Expect(TokenKind.Identifier, "Expected target identifier");
        var target = new IdentifierExpression(targetToken.Text, targetToken.Span);

        IdentifierExpression? pointer = null;
        // WITH POINTER
        if (Check(TokenKind.Identifier) && Current.Text.Equals("WITH", StringComparison.OrdinalIgnoreCase))
        {
            Advance(); // WITH
            if (Check(TokenKind.PointerKeyword))
            {
                Advance(); // POINTER
                var ptrToken = Expect(TokenKind.Identifier, "Expected pointer identifier");
                pointer = new IdentifierExpression(ptrToken.Text, ptrToken.Span);
            }
        }

        // Skip ON OVERFLOW / NOT ON OVERFLOW for now
        SkipToEndOfStatement();

        return new StringStatement(sources, target, pointer,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── UNSTRING ──

    private UnstringStatement ParseUnstringStatement()
    {
        int start = Current.Span.Start;
        Advance(); // UNSTRING

        var sourceToken = Expect(TokenKind.Identifier, "Expected source identifier");
        var source = new IdentifierExpression(sourceToken.Text, sourceToken.Span);

        Expression? delimiter = null;
        // DELIMITED BY
        if (Check(TokenKind.Identifier) && Current.Text.Equals("DELIMITED", StringComparison.OrdinalIgnoreCase))
        {
            Advance();
            Match(TokenKind.ByKeyword);
            delimiter = ParseExpression();
        }

        Expect(TokenKind.IntoKeyword, "Expected INTO in UNSTRING statement");

        var targets = new List<IdentifierExpression>();
        while (Check(TokenKind.Identifier) && !Current.Text.Equals("TALLYING", StringComparison.OrdinalIgnoreCase))
        {
            var tok = Advance();
            targets.Add(new IdentifierExpression(tok.Text, tok.Span));
        }

        IdentifierExpression? tallying = null;
        if (Check(TokenKind.Identifier) && Current.Text.Equals("TALLYING", StringComparison.OrdinalIgnoreCase))
        {
            Advance();
            // Skip optional IN
            if (Check(TokenKind.Identifier) && Current.Text.Equals("IN", StringComparison.OrdinalIgnoreCase))
                Advance();
            var tallyToken = Expect(TokenKind.Identifier, "Expected tally counter");
            tallying = new IdentifierExpression(tallyToken.Text, tallyToken.Span);
        }

        SkipToEndOfStatement();

        return new UnstringStatement(source, delimiter, targets, tallying,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── INSPECT ──

    private InspectStatement ParseInspectStatement()
    {
        int start = Current.Span.Start;
        Advance(); // INSPECT

        var targetToken = Expect(TokenKind.Identifier, "Expected identifier after INSPECT");
        var target = new IdentifierExpression(targetToken.Text, targetToken.Span);

        InspectType inspectKind = InspectType.ReplacingAll;
        Expression? searchFor = null;
        Expression? replaceWith = null;
        IdentifierExpression? tallyCounter = null;

        // TALLYING or REPLACING or CONVERTING
        if (Check(TokenKind.Identifier))
        {
            string verb = Current.Text.ToUpperInvariant();
            if (verb == "TALLYING")
            {
                Advance();
                var counterToken = Expect(TokenKind.Identifier, "Expected counter");
                tallyCounter = new IdentifierExpression(counterToken.Text, counterToken.Span);
                Match(TokenKind.Identifier); // FOR
                if (Check(TokenKind.AllKeyword)) { Advance(); inspectKind = InspectType.TallyingAll; }
                else if (Check(TokenKind.Identifier) && Current.Text.Equals("LEADING", StringComparison.OrdinalIgnoreCase))
                { Advance(); inspectKind = InspectType.TallyingLeading; }
                searchFor = ParseExpression();
            }
            else if (verb == "REPLACING")
            {
                Advance();
                if (Check(TokenKind.AllKeyword)) { Advance(); inspectKind = InspectType.ReplacingAll; }
                else if (Check(TokenKind.Identifier) && Current.Text.Equals("LEADING", StringComparison.OrdinalIgnoreCase))
                { Advance(); inspectKind = InspectType.ReplacingLeading; }
                else if (Check(TokenKind.Identifier) && Current.Text.Equals("FIRST", StringComparison.OrdinalIgnoreCase))
                { Advance(); inspectKind = InspectType.ReplacingFirst; }
                searchFor = ParseExpression();
                Match(TokenKind.ByKeyword);
                replaceWith = ParseExpression();
            }
            else if (verb == "CONVERTING")
            {
                Advance();
                inspectKind = InspectType.Converting;
                searchFor = ParseExpression();
                Match(TokenKind.ToKeyword);
                replaceWith = ParseExpression();
            }
        }

        SkipToEndOfStatement();

        return new InspectStatement(target, inspectKind, searchFor, replaceWith, tallyCounter,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    /// <summary>Skip remaining tokens in a statement to the period.</summary>
    private void SkipToEndOfStatement()
    {
        while (!Check(TokenKind.Period) && Current.Kind != TokenKind.EndOfFile &&
               !IsStatementStart(Current.Kind) && !IsScopeTerminator(Current.Kind))
        {
            Advance();
        }
        Match(TokenKind.Period);
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
                // Check for subscripts or reference modification: NAME(...)
                if (Check(TokenKind.LeftParen))
                {
                    return ParseSubscriptOrRefMod(token);
                }
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
    /// Parse subscripts or reference modification after an identifier.
    /// Subscripts: NAME(expr1, expr2, ...)
    /// Reference modification: NAME(start : length)
    /// Distinguishing: if we see a colon, it's ref-mod; otherwise subscripts.
    /// </summary>
    private IdentifierExpression ParseSubscriptOrRefMod(Token nameToken)
    {
        Advance(); // consume (

        // Parse first expression
        var first = ParseArithmeticExpression();

        // Check for colon → reference modification: NAME(start : length)
        if (Check(TokenKind.Colon))
        {
            Advance(); // consume :
            Expression? length = null;
            if (!Check(TokenKind.RightParen))
            {
                length = ParseArithmeticExpression();
            }
            Expect(TokenKind.RightParen, "Expected ) after reference modification");
            var span = TextSpan.FromBounds(nameToken.Span.Start, Current.Span.Start);
            return new IdentifierExpression(nameToken.Text, span,
                refModStart: first, refModLength: length);
        }

        // Subscripts: NAME(expr1, expr2, ...)
        var subscripts = new List<Expression> { first };
        while (Check(TokenKind.Comma))
        {
            Advance(); // consume comma
            subscripts.Add(ParseArithmeticExpression());
        }

        Expect(TokenKind.RightParen, "Expected ) after subscripts");

        var span2 = TextSpan.FromBounds(nameToken.Span.Start, Current.Span.Start);
        return new IdentifierExpression(nameToken.Text, span2, subscripts: subscripts);
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
