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
        TokenKind.StartKeyword or TokenKind.SortKeyword or
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
            else if (Check(TokenKind.CompilerDirective))
            {
                // Compiler directives at the file level are ignored (consumed as no-ops)
                Advance();
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

        // Issue 8: Check keyword + DIVISION to avoid false-matching reserved words
        // like DATA in identification division free text
        EnvironmentDivision? environment = null;
        if (Current.Kind == TokenKind.EnvironmentKeyword && Peek().Kind == TokenKind.DivisionKeyword)
            environment = ParseEnvironmentDivision();

        DataDivision? data = null;
        if (Current.Kind == TokenKind.DataKeyword && Peek().Kind == TokenKind.DivisionKeyword)
            data = ParseDataDivision();

        ProcedureDivision? procedure = null;
        if (Current.Kind == TokenKind.ProcedureKeyword && Peek().Kind == TokenKind.DivisionKeyword)
            procedure = ParseProcedureDivision();

        // Optional END PROGRAM program-name.
        if (Check(TokenKind.EndKeyword))
        {
            Advance(); // END
            if (Check(TokenKind.Identifier) && Current.Text.Equals("PROGRAM", StringComparison.OrdinalIgnoreCase))
            {
                Advance(); // PROGRAM
                // program-name can be an identifier or a string literal
                if (Check(TokenKind.Identifier)) Advance();
                else if (Check(TokenKind.StringLiteral)) Advance();
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
        // Issue 7: Use IsDivisionStart() to avoid false-stopping on reserved words
        // like DATA appearing in non-division contexts
        while (Current.Kind != TokenKind.EndOfFile && !IsDivisionStart())
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

        // Parse sections until next division (use IsDivisionStart to avoid false
        // matches on reserved words like DATA appearing in free text)
        while (Current.Kind != TokenKind.EndOfFile && !IsDivisionStart())
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
                    while (Current.Kind != TokenKind.EndOfFile && !IsDivisionStart() &&
                           !(Check(TokenKind.InputKeyword)) && !(Check(TokenKind.I_OKeyword)) &&
                           !(Check(TokenKind.FileKeyword)) &&
                           !(Check(TokenKind.Identifier) && (
                               Current.Text.Equals("FILE-CONTROL", StringComparison.OrdinalIgnoreCase) ||
                               Current.Text.Equals("INPUT-OUTPUT", StringComparison.OrdinalIgnoreCase))))
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

        string programId = "";
        string? classId = null;
        string? methodId = null;
        string? interfaceId = null;

        if (Check(TokenKind.ProgramIdKeyword))
        {
            Advance(); // PROGRAM-ID
            Expect(TokenKind.Period);
            var nameToken = Expect(TokenKind.Identifier, "Expected program name");
            programId = nameToken.Text;
            Match(TokenKind.Period);
        }
        else if (Check(TokenKind.ClassIdKeyword))
        {
            // CLASS-ID. class-name.
            Advance();
            Expect(TokenKind.Period);
            var nameToken = Expect(TokenKind.Identifier, "Expected class name after CLASS-ID");
            classId = nameToken.Text;
            programId = classId;
            Match(TokenKind.Period);
        }
        else if (Check(TokenKind.MethodIdKeyword))
        {
            // METHOD-ID. method-name.
            Advance();
            Expect(TokenKind.Period);
            var nameToken = Expect(TokenKind.Identifier, "Expected method name after METHOD-ID");
            methodId = nameToken.Text;
            programId = methodId;
            Match(TokenKind.Period);
        }
        else if (Check(TokenKind.InterfaceIdKeyword))
        {
            // INTERFACE-ID. interface-name.
            Advance();
            Expect(TokenKind.Period);
            var nameToken = Expect(TokenKind.Identifier, "Expected interface name after INTERFACE-ID");
            interfaceId = nameToken.Text;
            programId = interfaceId;
            Match(TokenKind.Period);
        }
        else
        {
            // Fallback: attempt PROGRAM-ID
            Expect(TokenKind.ProgramIdKeyword, "Expected PROGRAM-ID, CLASS-ID, METHOD-ID, or INTERFACE-ID");
            Expect(TokenKind.Period);
            var nameToken = Expect(TokenKind.Identifier, "Expected program name");
            programId = nameToken.Text;
            Match(TokenKind.Period);
        }

        // Skip any remaining identification paragraphs (AUTHOR, DATE-WRITTEN, etc.)
        // Must check for "keyword DIVISION" pattern, not just the keyword alone,
        // because reserved words like DATA appear in free text (e.g., INSTALLATION paragraph)
        while (Current.Kind != TokenKind.EndOfFile && !IsDivisionStart())
        {
            // Check for next paragraph-like heading: identifier + period
            if (Check(TokenKind.Identifier) && Peek().Kind == TokenKind.Period)
            {
                // Could be an identification paragraph name — consume it
                string upperText = Current.Text.ToUpperInvariant();
                if (upperText == "AUTHOR" || upperText == "DATE-WRITTEN" ||
                    upperText == "DATE-COMPILED" || upperText == "INSTALLATION" ||
                    upperText == "SECURITY" || upperText == "REMARKS")
                {
                    Advance(); // paragraph name
                    Advance(); // period
                    // Issue 13: Use IsDivisionStart() for consistency with outer loop
                    while (!Check(TokenKind.Period) && Current.Kind != TokenKind.EndOfFile &&
                           !IsDivisionStart())
                        Advance();
                    Match(TokenKind.Period);
                    continue;
                }
            }
            break;
        }

        int end = Current.Span.Start;
        return new IdentificationDivision(programId, TextSpan.FromBounds(start, end),
            classId: classId, methodId: methodId, interfaceId: interfaceId);
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
        ReportSection? reportSection = null;
        ScreenSection? screenSection = null;

        // Parse sections in order (FILE, WORKING-STORAGE, LINKAGE, REPORT, SCREEN, etc.)
        // Issue 9: Use IsDivisionStart() to avoid false-stopping on reserved words
        while (Current.Kind != TokenKind.EndOfFile && !IsDivisionStart())
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
            else if (Check(TokenKind.ReportKeyword))
            {
                reportSection = ParseReportSection();
            }
            else if (Check(TokenKind.ScreenKeyword))
            {
                screenSection = ParseScreenSection();
            }
            else
            {
                // Skip unrecognized section header (e.g., LOCAL-STORAGE)
                Advance();
                if (Check(TokenKind.SectionKeyword)) Advance();
                if (Check(TokenKind.Period)) Advance();
                while (Current.Kind != TokenKind.EndOfFile && !IsDivisionStart() &&
                       !Check(TokenKind.WorkingStorageKeyword) && !Check(TokenKind.LinkageKeyword) &&
                       !Check(TokenKind.FileKeyword) && !Check(TokenKind.ReportKeyword) &&
                       !Check(TokenKind.ScreenKeyword))
                    Advance();
            }
        }

        int end = Current.Span.Start;
        return new DataDivision(fileSection, ws, linkage, TextSpan.FromBounds(start, end),
            reportSection: reportSection, screenSection: screenSection);
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
                Advance(); // PIC/PICTURE keyword
                // The lexer now produces a PictureString token after PIC
                // (it also consumes optional IS internally)
                if (Check(TokenKind.PictureString))
                {
                    pic = Advance().Text;
                }
                else
                {
                    // Fallback: old-style parsing if lexer didn't produce PictureString
                    Match(TokenKind.IsKeyword); // optional IS
                    pic = ParsePictureString();
                }
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
            else if (Check(TokenKind.NationalKeyword))
            {
                Advance();
                usage = UsageType.National;
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
                // OCCURS n TO m — range form
                if (Check(TokenKind.ToKeyword))
                {
                    Advance(); // TO
                    if (Check(TokenKind.IntegerLiteral))
                    {
                        occursCount = (int)(long)Advance().Value!; // use max
                    }
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
                    // Multiple key names
                    while (Check(TokenKind.Identifier) &&
                           !Check(TokenKind.AscendingKeyword) && !Check(TokenKind.DescendingKeyword) &&
                           !Check(TokenKind.IndexedKeyword))
                        Advance();
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
        // optional ARE (would be lexed as identifier)
        if (Check(TokenKind.Identifier) && Current.Text.Equals("ARE", StringComparison.OrdinalIgnoreCase))
            Advance();

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
        if (Match(TokenKind.NationalKeyword))
            return UsageType.National;
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
        while (Current.Kind != TokenKind.EndOfFile && !IsDivisionStart())
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
        // Optional segment/priority number (archaic feature)
        if (Check(TokenKind.IntegerLiteral))
            Advance();
        Expect(TokenKind.Period);

        var paragraphs = new List<Paragraph>();
        // Issue 11: Use IsDivisionStart() to avoid false-stopping on reserved words
        while (Current.Kind != TokenKind.EndOfFile && !IsSectionHeader() &&
               !IsDivisionStart())
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
            if (IsParagraphHeader() || IsSectionHeader() || IsDivisionStart())
                break;

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
               !IsDivisionStart())
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

    // ── DISPLAY ──

    private DisplayStatement ParseDisplayStatement()
    {
        int start = Current.Span.Start;
        Advance(); // DISPLAY

        var operands = new List<Expression>();
        while (!Check(TokenKind.Period) && !Check(TokenKind.EndOfFile) &&
               !IsStatementStart(Current.Kind) && !IsScopeTerminator(Current.Kind) &&
               !Check(TokenKind.EndDisplayKeyword))
        {
            // Skip optional UPON clause
            if (Check(TokenKind.Identifier) && Current.Text.Equals("UPON", StringComparison.OrdinalIgnoreCase))
            {
                Advance(); // UPON
                if (Check(TokenKind.Identifier)) Advance(); // device-name
                continue;
            }
            // Skip optional WITH NO ADVANCING or NO ADVANCING
            if (Check(TokenKind.WithKeyword))
            {
                Advance();
                if (Check(TokenKind.Identifier) && Current.Text.Equals("NO", StringComparison.OrdinalIgnoreCase))
                    Advance();
                Match(TokenKind.AdvancingKeyword);
                continue;
            }
            if (Check(TokenKind.Identifier) && Current.Text.Equals("NO", StringComparison.OrdinalIgnoreCase) &&
                Peek().Kind == TokenKind.AdvancingKeyword)
            {
                Advance(); // NO
                Advance(); // ADVANCING
                continue;
            }
            operands.Add(ParseExpression());
        }
        Match(TokenKind.EndDisplayKeyword);

        return new DisplayStatement(operands, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── STOP RUN ──

    private StopRunStatement ParseStopStatement()
    {
        int start = Current.Span.Start;
        Advance(); // STOP
        Expect(TokenKind.RunKeyword, "Expected RUN after STOP");
        return new StopRunStatement(TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── MOVE ──

    private MoveStatement ParseMoveStatement()
    {
        int start = Current.Span.Start;
        Advance(); // MOVE

        // MOVE CORRESPONDING/CORR — skip the keyword, treat as regular MOVE
        Match(TokenKind.CorrespondingKeyword);

        var source = ParseExpression();
        Expect(TokenKind.ToKeyword, "Expected TO after MOVE source");

        var targets = new List<IdentifierExpression>();
        while (!Check(TokenKind.Period) && !Check(TokenKind.EndOfFile) &&
               !IsStatementStart(Current.Kind) && !IsScopeTerminator(Current.Kind) &&
               !Check(TokenKind.OnKeyword) && !Check(TokenKind.NotKeyword))
        {
            if (!Check(TokenKind.Identifier))
                break;
            var id = Advance();
            // Handle IN/OF qualification
            while (Check(TokenKind.InKeyword) || Check(TokenKind.OfKeyword))
            {
                Advance(); // IN or OF
                if (Check(TokenKind.Identifier)) Advance(); // qualifier
            }
            // Handle subscripts: NAME(expr1, expr2, ...)
            if (Check(TokenKind.LeftParen))
            {
                var subId = ParseSubscriptOrRefMod(id);
                targets.Add(subId);
            }
            else
            {
                targets.Add(new IdentifierExpression(id.Text, id.Span));
            }
        }

        return new MoveStatement(source, targets, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── ADD ──

    private AddStatement ParseAddStatement()
    {
        int start = Current.Span.Start;
        Advance(); // ADD

        // ADD CORRESPONDING/CORR
        Match(TokenKind.CorrespondingKeyword);

        var operands = new List<Expression>();
        while (!Check(TokenKind.ToKeyword) && !Check(TokenKind.GivingKeyword) &&
               !Check(TokenKind.Period) && !Check(TokenKind.EndOfFile))
        {
            operands.Add(ParseExpression());
        }

        Expect(TokenKind.ToKeyword, "Expected TO in ADD statement");

        var targets = new List<IdentifierExpression>();
        while (!Check(TokenKind.Period) && !Check(TokenKind.EndOfFile) &&
               !Check(TokenKind.GivingKeyword) &&
               !IsStatementStart(Current.Kind) && !IsScopeTerminator(Current.Kind) &&
               !Check(TokenKind.OnKeyword) && !Check(TokenKind.NotKeyword) &&
               !Check(TokenKind.EndAddKeyword))
        {
            if (!Check(TokenKind.Identifier))
                break;
            var id = Advance();
            // Handle IN/OF qualification
            while (Check(TokenKind.InKeyword) || Check(TokenKind.OfKeyword))
            {
                Advance(); // IN or OF
                if (Check(TokenKind.Identifier)) Advance();
            }
            if (Check(TokenKind.LeftParen))
            {
                var subId = ParseSubscriptOrRefMod(id);
                Match(TokenKind.RoundedKeyword);
                targets.Add(subId);
            }
            else
            {
                Match(TokenKind.RoundedKeyword); // optional ROUNDED
                targets.Add(new IdentifierExpression(id.Text, id.Span));
            }
        }

        // Optional GIVING
        if (Match(TokenKind.GivingKeyword))
        {
            while (Check(TokenKind.Identifier) && !IsStatementStart(Current.Kind))
            {
                var id = Advance();
                if (Check(TokenKind.LeftParen))
                {
                    var subId = ParseSubscriptOrRefMod(id);
                    Match(TokenKind.RoundedKeyword);
                    targets.Add(subId);
                }
                else
                {
                    Match(TokenKind.RoundedKeyword);
                    targets.Add(new IdentifierExpression(id.Text, id.Span));
                }
            }
        }

        SkipSizeErrorPhrases();
        Match(TokenKind.EndAddKeyword);

        return new AddStatement(operands, targets, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── SUBTRACT ──

    private SubtractStatement ParseSubtractStatement()
    {
        int start = Current.Span.Start;
        Advance(); // SUBTRACT

        // SUBTRACT CORRESPONDING/CORR
        Match(TokenKind.CorrespondingKeyword);

        var operands = new List<Expression>();
        while (!Check(TokenKind.FromKeyword) && !Check(TokenKind.Period) && !Check(TokenKind.EndOfFile))
        {
            operands.Add(ParseExpression());
        }

        Expect(TokenKind.FromKeyword, "Expected FROM in SUBTRACT statement");

        var targets = new List<IdentifierExpression>();
        while (!Check(TokenKind.Period) && !Check(TokenKind.EndOfFile) &&
               !Check(TokenKind.GivingKeyword) &&
               !IsStatementStart(Current.Kind) && !IsScopeTerminator(Current.Kind) &&
               !Check(TokenKind.OnKeyword) && !Check(TokenKind.NotKeyword) &&
               !Check(TokenKind.EndSubtractKeyword))
        {
            if (!Check(TokenKind.Identifier))
            {
                break;
            }
            var id = Advance();
            while (Check(TokenKind.InKeyword) || Check(TokenKind.OfKeyword))
            {
                Advance();
                if (Check(TokenKind.Identifier)) Advance();
            }
            if (Check(TokenKind.LeftParen))
            {
                var subId = ParseSubscriptOrRefMod(id);
                Match(TokenKind.RoundedKeyword);
                targets.Add(subId);
            }
            else
            {
                Match(TokenKind.RoundedKeyword);
                targets.Add(new IdentifierExpression(id.Text, id.Span));
            }
        }

        if (Match(TokenKind.GivingKeyword))
        {
            while (Check(TokenKind.Identifier) && !IsStatementStart(Current.Kind))
            {
                var id = Advance();
                if (Check(TokenKind.LeftParen))
                {
                    var subId = ParseSubscriptOrRefMod(id);
                    Match(TokenKind.RoundedKeyword);
                    targets.Add(subId);
                }
                else
                {
                    Match(TokenKind.RoundedKeyword);
                    targets.Add(new IdentifierExpression(id.Text, id.Span));
                }
            }
        }

        SkipSizeErrorPhrases();
        Match(TokenKind.EndSubtractKeyword);

        return new SubtractStatement(operands, targets, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── COMPUTE ──

    private ComputeStatement ParseComputeStatement()
    {
        int start = Current.Span.Start;
        Advance(); // COMPUTE

        // Issue 28: COMPUTE accepts multiple targets: COMPUTE A B C = expr
        // Parse first target (required)
        var targetToken = Expect(TokenKind.Identifier, "Expected target identifier in COMPUTE");
        // Handle IN/OF qualification
        while (Check(TokenKind.InKeyword) || Check(TokenKind.OfKeyword))
        {
            Advance();
            if (Check(TokenKind.Identifier)) Advance();
        }
        IdentifierExpression target;
        if (Check(TokenKind.LeftParen))
        {
            target = ParseSubscriptOrRefMod(targetToken);
        }
        else
        {
            target = new IdentifierExpression(targetToken.Text, targetToken.Span);
        }
        Match(TokenKind.RoundedKeyword); // optional ROUNDED

        // Consume additional targets until = sign (Issue 28)
        while (Check(TokenKind.Identifier) && !Check(TokenKind.Equals))
        {
            Advance(); // additional target identifier
            // Handle IN/OF qualification on additional targets
            while (Check(TokenKind.InKeyword) || Check(TokenKind.OfKeyword))
            {
                Advance();
                if (Check(TokenKind.Identifier)) Advance();
            }
            if (Check(TokenKind.LeftParen))
            {
                // Skip subscript/refmod on additional targets
                Advance(); // (
                int depth = 1;
                while (depth > 0 && Current.Kind != TokenKind.EndOfFile)
                {
                    if (Check(TokenKind.LeftParen)) depth++;
                    else if (Check(TokenKind.RightParen)) depth--;
                    Advance();
                }
            }
            Match(TokenKind.RoundedKeyword); // optional ROUNDED on additional target
        }

        Expect(TokenKind.Equals, "Expected = in COMPUTE statement");

        var value = ParseArithmeticExpression();

        SkipSizeErrorPhrases();
        Match(TokenKind.EndComputeKeyword);

        return new ComputeStatement(target, value, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── IF ──

    private IfStatement ParseIfStatement()
    {
        int start = Current.Span.Start;
        Advance(); // IF

        var condition = ParseConditionExpression();

        // Optional THEN keyword
        Match(TokenKind.ThenKeyword);

        // Parse then-statements until ELSE, END-IF, or period
        var thenStatements = ParseImperativeStatements(TokenKind.ElseKeyword, TokenKind.EndIfKeyword);

        var elseStatements = new List<Statement>();
        if (Match(TokenKind.ElseKeyword))
        {
            elseStatements = ParseImperativeStatements(TokenKind.EndIfKeyword);
        }

        if (Check(TokenKind.EndIfKeyword))
        {
            Advance();
        }
        // else: period-terminated IF — period consumed by caller

        return new IfStatement(condition, thenStatements, elseStatements,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── PERFORM ──

    private PerformStatement ParsePerformStatement()
    {
        int start = Current.Span.Start;
        Advance(); // PERFORM

        // Parse optional [WITH] TEST BEFORE/AFTER
        bool testAfter = false;
        if (Check(TokenKind.WithKeyword) && Peek().Kind == TokenKind.TestKeyword)
            Advance(); // consume WITH (only when followed by TEST)
        if (Check(TokenKind.TestKeyword))
        {
            Advance(); // TEST
            if (Check(TokenKind.AfterKeyword)) { Advance(); testAfter = true; }
            else Match(TokenKind.BeforeKeyword); // default
        }

        // PERFORM VARYING identifier FROM expr BY expr UNTIL condition
        if (Check(TokenKind.VaryingKeyword))
        {
            return ParsePerformVarying(start, testAfter, null, null);
        }

        // PERFORM UNTIL condition [inline body] END-PERFORM
        if (Check(TokenKind.UntilKeyword))
        {
            Advance();
            var until = ParseConditionExpression();
            var body = ParseImperativeStatements(TokenKind.EndPerformKeyword);
            Expect(TokenKind.EndPerformKeyword);
            return new PerformStatement(null, body, null, until,
                TextSpan.FromBounds(start, Current.Span.Start), testAfter: testAfter);
        }

        // PERFORM n TIMES [inline body] END-PERFORM
        if ((Check(TokenKind.IntegerLiteral) || Check(TokenKind.Identifier)) &&
            Peek().Kind == TokenKind.TimesKeyword)
        {
            var times = ParseExpression();
            Expect(TokenKind.TimesKeyword);
            var body = ParseImperativeStatements(TokenKind.EndPerformKeyword);
            Expect(TokenKind.EndPerformKeyword);
            return new PerformStatement(null, body, times, null,
                TextSpan.FromBounds(start, Current.Span.Start), testAfter: testAfter);
        }

        // Out-of-line: PERFORM paragraph-name [THRU paragraph-name] [modifier]
        if (Check(TokenKind.Identifier))
        {
            string name = Advance().Text;
            string? thruName = null;
            if (Match(TokenKind.ThruKeyword))
            {
                var thruToken = Expect(TokenKind.Identifier, "Expected paragraph name after THRU");
                thruName = thruToken.Text;
            }

            // Out-of-line with TIMES
            if ((Check(TokenKind.IntegerLiteral) || Check(TokenKind.Identifier)) &&
                Peek().Kind == TokenKind.TimesKeyword)
            {
                var times = ParseExpression();
                Expect(TokenKind.TimesKeyword);
                return new PerformStatement(name, new List<Statement>(), times, null,
                    TextSpan.FromBounds(start, Current.Span.Start),
                    thruParagraphName: thruName, testAfter: testAfter);
            }

            // Out-of-line with UNTIL
            if (Check(TokenKind.UntilKeyword))
            {
                Advance();
                var until = ParseConditionExpression();
                return new PerformStatement(name, new List<Statement>(), null, until,
                    TextSpan.FromBounds(start, Current.Span.Start),
                    thruParagraphName: thruName, testAfter: testAfter);
            }

            // Out-of-line with VARYING
            if (Check(TokenKind.VaryingKeyword))
            {
                return ParsePerformVarying(start, testAfter, name, thruName);
            }

            // Out-of-line with TEST BEFORE/AFTER
            if (Check(TokenKind.TestKeyword))
            {
                Advance();
                if (Check(TokenKind.AfterKeyword)) { Advance(); testAfter = true; }
                else Match(TokenKind.BeforeKeyword);

                if (Check(TokenKind.UntilKeyword))
                {
                    Advance();
                    var until = ParseConditionExpression();
                    return new PerformStatement(name, new List<Statement>(), null, until,
                        TextSpan.FromBounds(start, Current.Span.Start),
                        thruParagraphName: thruName, testAfter: testAfter);
                }
                if (Check(TokenKind.VaryingKeyword))
                {
                    return ParsePerformVarying(start, testAfter, name, thruName);
                }
            }

            return new PerformStatement(name, new List<Statement>(), null, null,
                TextSpan.FromBounds(start, Current.Span.Start),
                thruParagraphName: thruName, testAfter: testAfter);
        }

        // Bare inline PERFORM ... END-PERFORM
        var stmts = ParseImperativeStatements(TokenKind.EndPerformKeyword);
        Expect(TokenKind.EndPerformKeyword);
        return new PerformStatement(null, stmts, null, null,
            TextSpan.FromBounds(start, Current.Span.Start), testAfter: testAfter);
    }

    private PerformStatement ParsePerformVarying(int start, bool testAfter,
        string? paraName, string? thruName)
    {
        Advance(); // VARYING

        var varyToken = Expect(TokenKind.Identifier, "Expected identifier after VARYING");
        var varyId = new IdentifierExpression(varyToken.Text, varyToken.Span);

        Expect(TokenKind.FromKeyword, "Expected FROM in PERFORM VARYING");
        var from = ParseExpression();

        Expect(TokenKind.ByKeyword, "Expected BY in PERFORM VARYING");
        var by = ParseExpression();

        Expect(TokenKind.UntilKeyword, "Expected UNTIL in PERFORM VARYING");
        var until = ParseConditionExpression();

        var varying = new PerformVarying(varyId, from, by,
            TextSpan.FromBounds(varyToken.Span.Start, Current.Span.Start));

        if (paraName != null)
        {
            // Out-of-line PERFORM para VARYING
            return new PerformStatement(paraName, new List<Statement>(), null, until,
                TextSpan.FromBounds(start, Current.Span.Start),
                thruParagraphName: thruName, varying: varying, testAfter: testAfter);
        }
        else
        {
            // Inline PERFORM VARYING ... END-PERFORM
            var body = ParseImperativeStatements(TokenKind.EndPerformKeyword);
            Expect(TokenKind.EndPerformKeyword);
            return new PerformStatement(null, body, null, until,
                TextSpan.FromBounds(start, Current.Span.Start),
                varying: varying, testAfter: testAfter);
        }
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
            return new GoToDependingStatement(names, expr,
                TextSpan.FromBounds(start, Current.Span.Start));
        }

        // Bare GO TO (no paragraph) is valid — target set by ALTER at runtime
        string? targetName = names.Count > 0 ? names[0] : null;
        return new GoToStatement(targetName, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── ALTER ──

    private AlterStatement ParseAlterStatement()
    {
        int start = Current.Span.Start;
        Advance(); // ALTER

        var alterations = new List<(string, string)>();
        while (Check(TokenKind.Identifier))
        {
            string fromPara = Advance().Text;
            Match(TokenKind.ToKeyword); // TO
            // Optional PROCEED TO
            if (Check(TokenKind.Identifier) && Current.Text.Equals("PROCEED", StringComparison.OrdinalIgnoreCase))
            {
                Advance(); // PROCEED
                Match(TokenKind.ToKeyword); // TO
            }
            string toPara = Expect(TokenKind.Identifier, "Expected paragraph name after ALTER TO").Text;
            alterations.Add((fromPara, toPara));

            // Comma separator between multiple alterations
            Match(TokenKind.Comma);
        }

        return new AlterStatement(alterations, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── CONTINUE ──

    private ContinueStatement ParseContinueStatement()
    {
        int start = Current.Span.Start;
        Advance(); // CONTINUE
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
                // Issue 23: Consume YYYYMMDD/YYYYDDD modifier after DATE/DAY
                if ((fromSource == "DATE" || fromSource == "DAY") && Check(TokenKind.Identifier))
                {
                    string modifier = Current.Text.ToUpperInvariant();
                    if (modifier == "YYYYMMDD" || modifier == "YYYYDDD")
                        Advance();
                }
            }
        }

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

        return new OpenStatement(clauses, TextSpan.FromBounds(start, Current.Span.Start));
    }

    private CloseStatement ParseCloseStatement()
    {
        int start = Current.Span.Start;
        Advance(); // CLOSE
        var fileNames = new List<string>();
        while (Check(TokenKind.Identifier))
            fileNames.Add(Advance().Text);
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
            atEnd = ParseImperativeStatements(TokenKind.NotKeyword, TokenKind.EndReadKeyword);
        }
        if (Check(TokenKind.NotKeyword) && Peek().Kind == TokenKind.AtKeyword)
        {
            Advance(); Advance(); // NOT AT
            Match(TokenKind.EndKeyword);
            notAtEnd = ParseImperativeStatements(TokenKind.EndReadKeyword);
        }
        // INVALID KEY / NOT INVALID KEY
        if (Check(TokenKind.InvalidKeyword))
        {
            Advance(); Match(TokenKind.KeyKeyword);
            atEnd = ParseImperativeStatements(TokenKind.NotKeyword, TokenKind.EndReadKeyword);
        }

        Match(TokenKind.EndReadKeyword);

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

        // INVALID KEY / NOT INVALID KEY / AT END-OF-PAGE / NOT AT END-OF-PAGE
        SkipExceptionPhrases(TokenKind.EndWriteKeyword);
        Match(TokenKind.EndWriteKeyword);
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
        SkipExceptionPhrases(TokenKind.EndRewriteKeyword);
        Match(TokenKind.EndRewriteKeyword);
        return new RewriteStatement(recordName, from, TextSpan.FromBounds(start, Current.Span.Start));
    }

    private DeleteStatement ParseDeleteStatement()
    {
        int start = Current.Span.Start;
        Advance(); // DELETE
        var fileToken = Expect(TokenKind.Identifier, "Expected file name");
        Match(TokenKind.RecordKeyword); // optional RECORD
        SkipExceptionPhrases(TokenKind.EndDeleteKeyword);
        Match(TokenKind.EndDeleteKeyword);
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
        SkipExceptionPhrases(TokenKind.EndStartKeyword);
        Match(TokenKind.EndStartKeyword);
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

        // ON EXCEPTION / NOT ON EXCEPTION
        SkipExceptionPhrases(TokenKind.EndCallKeyword);
        Match(TokenKind.EndCallKeyword);

        return new CallStatement(programName, parameters, returning,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── CANCEL ──

    private CancelStatement ParseCancelStatement()
    {
        int start = Current.Span.Start;
        Advance(); // CANCEL
        var programName = ParseExpression();
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

        // ON OVERFLOW / NOT ON OVERFLOW
        SkipExceptionPhrases(TokenKind.EndStringKeyword);
        Match(TokenKind.EndStringKeyword);

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
            Match(TokenKind.InKeyword); // optional IN (now a keyword)
            var tallyToken = Expect(TokenKind.Identifier, "Expected tally counter");
            tallying = new IdentifierExpression(tallyToken.Text, tallyToken.Span);
        }

        SkipExceptionPhrases(TokenKind.EndUnstringKeyword);
        Match(TokenKind.EndUnstringKeyword);

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

    // ── INITIATE (Phase 5.2) ──

    private InitiateStatement ParseInitiateStatement()
    {
        int start = Current.Span.Start;
        Advance(); // INITIATE
        var names = new List<string>();
        while (Check(TokenKind.Identifier))
            names.Add(Advance().Text);
        return new InitiateStatement(names, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── GENERATE (Phase 5.2) ──

    private GenerateStatement ParseGenerateStatement()
    {
        int start = Current.Span.Start;
        Advance(); // GENERATE
        var nameToken = Expect(TokenKind.Identifier, "Expected report-group name after GENERATE");
        return new GenerateStatement(nameToken.Text, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── TERMINATE (Phase 5.2) ──

    private TerminateStatement ParseTerminateStatement()
    {
        int start = Current.Span.Start;
        Advance(); // TERMINATE
        var names = new List<string>();
        while (Check(TokenKind.Identifier))
            names.Add(Advance().Text);
        return new TerminateStatement(names, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── INVOKE (Phase 5.4) ──

    private InvokeStatement ParseInvokeStatement()
    {
        int start = Current.Span.Start;
        Advance(); // INVOKE

        // object-ref: identifier or SELF/SUPER
        var objectRef = ParseExpression();

        // method-name: literal or identifier
        var methodName = ParseExpression();

        // USING clause
        var args = new List<Expression>();
        if (Match(TokenKind.UsingKeyword))
        {
            while (!Check(TokenKind.Period) && !Check(TokenKind.ReturningKeyword) &&
                   Current.Kind != TokenKind.EndOfFile &&
                   !IsStatementStart(Current.Kind) && !IsScopeTerminator(Current.Kind))
            {
                Match(TokenKind.ByKeyword);       // optional BY
                Match(TokenKind.ReferenceKeyword); // optional REFERENCE
                Match(TokenKind.ContentKeyword);   // optional CONTENT
                args.Add(ParseExpression());
            }
        }

        // RETURNING clause
        IdentifierExpression? returning = null;
        if (Match(TokenKind.ReturningKeyword))
        {
            var retToken = Expect(TokenKind.Identifier, "Expected identifier after RETURNING");
            returning = new IdentifierExpression(retToken.Text, retToken.Span);
        }

        return new InvokeStatement(objectRef, methodName, args, returning,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── RAISE (Phase 5.5) ──

    private RaiseStatement ParseRaiseStatement()
    {
        int start = Current.Span.Start;
        Advance(); // RAISE
        var nameToken = Expect(TokenKind.Identifier, "Expected exception name after RAISE");
        return new RaiseStatement(nameToken.Text, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── RESUME (Phase 5.5) ──

    private ResumeStatement ParseResumeStatement()
    {
        int start = Current.Span.Start;
        Advance(); // RESUME

        string? atLabel = null;
        if (Match(TokenKind.AtKeyword))
        {
            // AT NEXT STATEMENT  or  AT paragraph-name
            if (Check(TokenKind.NextKeyword))
            {
                Advance(); // NEXT
                // Skip optional STATEMENT
                if (Check(TokenKind.Identifier) &&
                    Current.Text.Equals("STATEMENT", StringComparison.OrdinalIgnoreCase))
                    Advance();
            }
            else if (Check(TokenKind.Identifier))
            {
                atLabel = Advance().Text;
            }
        }

        return new ResumeStatement(atLabel, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── >>SOURCE FORMAT (Phase 5.6-5.10) ──

    private SourceFormatDirective ParseCompilerDirective()
    {
        int start = Current.Span.Start;
        string directiveText = Current.Value is string s ? s : Current.Text;
        Advance(); // CompilerDirective token

        // Parse >>SOURCE FORMAT IS FREE | FIXED from the directive text
        bool isFree = directiveText.Contains("FREE", StringComparison.OrdinalIgnoreCase);

        return new SourceFormatDirective(isFree, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── REPORT SECTION (Phase 5.2) ──

    private ReportSection ParseReportSection()
    {
        int start = Current.Span.Start;
        Expect(TokenKind.ReportKeyword);
        Expect(TokenKind.SectionKeyword);
        Expect(TokenKind.Period);

        var entries = new List<ReportDescriptionEntry>();
        while (Check(TokenKind.RdKeyword))
        {
            entries.Add(ParseReportDescriptionEntry());
        }

        return new ReportSection(entries, TextSpan.FromBounds(start, Current.Span.Start));
    }

    private ReportDescriptionEntry ParseReportDescriptionEntry()
    {
        int start = Current.Span.Start;
        Advance(); // RD

        var nameToken = Expect(TokenKind.Identifier, "Expected report name after RD");
        string reportName = nameToken.Text;

        // Skip any RD clauses (CONTROL IS, PAGE LIMIT, etc.) until the period
        while (!Check(TokenKind.Period) && Current.Kind != TokenKind.EndOfFile)
            Advance();
        Expect(TokenKind.Period);

        // Parse report record descriptions (01-level entries under RD)
        var records = new List<DataDescriptionEntry>();
        while (IsLevelNumber())
            records.Add(ParseDataDescriptionEntry());

        return new ReportDescriptionEntry(reportName, records,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── SCREEN SECTION (Phase 5.3) ──

    private ScreenSection ParseScreenSection()
    {
        int start = Current.Span.Start;
        Expect(TokenKind.ScreenKeyword);
        Expect(TokenKind.SectionKeyword);
        Expect(TokenKind.Period);

        var entries = new List<DataDescriptionEntry>();
        while (IsLevelNumber())
            entries.Add(ParseDataDescriptionEntry());

        return new ScreenSection(entries, TextSpan.FromBounds(start, Current.Span.Start));
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

    // ── EVALUATE ──

    private EvaluateStatement ParseEvaluateStatement()
    {
        int start = Current.Span.Start;
        Advance(); // EVALUATE

        // Subject can be: identifier, literal, expression, TRUE, FALSE, condition
        var subject = ParseConditionExpression();
        var whenClauses = new List<WhenClause>();
        var whenOtherStatements = new List<Statement>();

        while (Check(TokenKind.WhenKeyword))
        {
            int whenStart = Current.Span.Start;
            Advance(); // WHEN

            // WHEN OTHER
            if (Check(TokenKind.OtherKeyword))
            {
                Advance(); // OTHER
                whenOtherStatements = ParseImperativeStatements(TokenKind.EndEvaluateKeyword);
                break;
            }

            // Parse WHEN objects. Per §7.7 EVALUATE, objects can be:
            // value-1 [THRU value-2] | TRUE | FALSE | ANY | condition
            var objects = new List<Expression>();
            var whenObj = ParseExpression();
            // Handle THRU/THROUGH for range: WHEN 1 THRU 10
            if (Check(TokenKind.ThruKeyword))
            {
                Advance(); // THRU
                var rangeEnd = ParseExpression();
                // Represent range as a BinaryExpression with a special operator
                // For now, just keep the start value (range semantics handled at runtime)
                // For parsing, consume THRU range. Code gen uses only start value for now.
            }
            objects.Add(whenObj);

            // Handle additional WHEN clauses that share the same statement block
            // (WHEN value1 WHEN value2 ... statements)
            while (Check(TokenKind.WhenKeyword) && Peek().Kind != TokenKind.OtherKeyword)
            {
                Advance(); // WHEN
                var nextObj = ParseExpression();
                if (Check(TokenKind.ThruKeyword))
                {
                    Advance();
                    ParseExpression(); // consume range end
                }
                objects.Add(nextObj);
            }

            // Parse statements for this WHEN clause
            var stmts = ParseImperativeStatements(TokenKind.WhenKeyword, TokenKind.EndEvaluateKeyword);
            whenClauses.Add(new WhenClause(objects, stmts,
                TextSpan.FromBounds(whenStart, Current.Span.Start)));
        }

        Match(TokenKind.EndEvaluateKeyword);

        return new EvaluateStatement(subject, whenClauses, whenOtherStatements,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── MULTIPLY ──

    private MultiplyStatement ParseMultiplyStatement()
    {
        int start = Current.Span.Start;
        Advance(); // MULTIPLY

        var operand = ParseExpression();
        Expect(TokenKind.ByKeyword, "Expected BY in MULTIPLY statement");

        var targets = new List<IdentifierExpression>();
        while (!Check(TokenKind.Period) && !Check(TokenKind.EndOfFile) &&
               !Check(TokenKind.GivingKeyword) &&
               !IsStatementStart(Current.Kind) && !IsScopeTerminator(Current.Kind) &&
               !Check(TokenKind.OnKeyword) && !Check(TokenKind.NotKeyword) &&
               !Check(TokenKind.EndMultiplyKeyword))
        {
            if (!Check(TokenKind.Identifier))
            {
                ReportError("CS0100", "Expected identifier in MULTIPLY");
                Advance();
                break;
            }
            var tok = Advance();
            while (Check(TokenKind.InKeyword) || Check(TokenKind.OfKeyword))
            {
                Advance();
                if (Check(TokenKind.Identifier)) Advance();
            }
            if (Check(TokenKind.LeftParen))
            {
                var subId = ParseSubscriptOrRefMod(tok);
                Match(TokenKind.RoundedKeyword);
                targets.Add(subId);
            }
            else
            {
                Match(TokenKind.RoundedKeyword); // optional ROUNDED
                targets.Add(new IdentifierExpression(tok.Text, tok.Span));
            }
        }

        Expression? giving = null;
        if (Match(TokenKind.GivingKeyword))
        {
            giving = ParseExpression();
            Match(TokenKind.RoundedKeyword);
        }

        // Skip ON SIZE ERROR / NOT ON SIZE ERROR
        SkipSizeErrorPhrases();
        Match(TokenKind.EndMultiplyKeyword);

        return new MultiplyStatement(operand, targets, giving,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── DIVIDE ──

    private DivideStatement ParseDivideStatement()
    {
        int start = Current.Span.Start;
        Advance(); // DIVIDE

        var operand = ParseExpression();

        // DIVIDE x INTO y  or  DIVIDE x BY y GIVING z
        bool hasInto = Match(TokenKind.IntoKeyword);
        bool hasBy = !hasInto && Match(TokenKind.ByKeyword);

        var targets = new List<IdentifierExpression>();
        Expression? giving = null;
        IdentifierExpression? remainder = null;

        if (hasBy)
        {
            // DIVIDE x BY y GIVING z [REMAINDER r]
            var divisor = ParseExpression();
            // For now, store the divisor as a target — semantic analysis resolves
            targets.Add(new IdentifierExpression(
                divisor is IdentifierExpression id ? id.Name : "?",
                divisor.Span));
            Expect(TokenKind.GivingKeyword, "Expected GIVING in DIVIDE ... BY");
            giving = ParseExpression();
            Match(TokenKind.RoundedKeyword);
        }
        else
        {
            // DIVIDE x INTO y [GIVING z] [REMAINDER r]
            while (!Check(TokenKind.Period) && !Check(TokenKind.EndOfFile) &&
                   !Check(TokenKind.GivingKeyword) && !Check(TokenKind.RemainderKeyword) &&
                   !IsStatementStart(Current.Kind) && !IsScopeTerminator(Current.Kind) &&
                   !Check(TokenKind.OnKeyword) && !Check(TokenKind.NotKeyword) &&
                   !Check(TokenKind.EndDivideKeyword))
            {
                if (!Check(TokenKind.Identifier))
                {
                    ReportError("CS0100", "Expected identifier in DIVIDE");
                    Advance();
                    break;
                }
                var tok = Advance();
                while (Check(TokenKind.InKeyword) || Check(TokenKind.OfKeyword))
                {
                    Advance();
                    if (Check(TokenKind.Identifier)) Advance();
                }
                if (Check(TokenKind.LeftParen))
                {
                    var subId = ParseSubscriptOrRefMod(tok);
                    Match(TokenKind.RoundedKeyword);
                    targets.Add(subId);
                }
                else
                {
                    Match(TokenKind.RoundedKeyword);
                    targets.Add(new IdentifierExpression(tok.Text, tok.Span));
                }
            }

            if (Match(TokenKind.GivingKeyword))
            {
                giving = ParseExpression();
                Match(TokenKind.RoundedKeyword);
            }
        }

        if (Match(TokenKind.RemainderKeyword))
        {
            var remToken = Expect(TokenKind.Identifier, "Expected identifier after REMAINDER");
            remainder = new IdentifierExpression(remToken.Text, remToken.Span);
        }

        SkipSizeErrorPhrases();
        Match(TokenKind.EndDivideKeyword);

        return new DivideStatement(operand, targets, giving, remainder,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── SET ──

    private SetStatement ParseSetStatement()
    {
        int start = Current.Span.Start;
        Advance(); // SET

        var targets = new List<IdentifierExpression>();
        while (Check(TokenKind.Identifier))
        {
            var tok = Advance();
            targets.Add(new IdentifierExpression(tok.Text, tok.Span));
        }

        SetAction action = SetAction.To;
        if (Match(TokenKind.ToKeyword))
        {
            action = SetAction.To;
        }
        else if (Check(TokenKind.Identifier) && Current.Text.Equals("UP", StringComparison.OrdinalIgnoreCase))
        {
            Advance(); // UP
            Match(TokenKind.ByKeyword);
            action = SetAction.UpBy;
        }
        else if (Check(TokenKind.Identifier) && Current.Text.Equals("DOWN", StringComparison.OrdinalIgnoreCase))
        {
            Advance(); // DOWN
            Match(TokenKind.ByKeyword);
            action = SetAction.DownBy;
        }

        var value = ParseExpression();

        return new SetStatement(targets, value, action,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── SEARCH ──

    private SearchStatement ParseSearchStatement()
    {
        int start = Current.Span.Start;
        Advance(); // SEARCH

        // SEARCH ALL identifier — binary search form
        bool isAll = Match(TokenKind.AllKeyword);

        var tableToken = Expect(TokenKind.Identifier, "Expected table name after SEARCH");
        var tableName = new IdentifierExpression(tableToken.Text, tableToken.Span);

        // Optional VARYING identifier
        if (Check(TokenKind.VaryingKeyword))
        {
            Advance();
            if (Check(TokenKind.Identifier)) Advance(); // varying identifier
        }

        var atEnd = new List<Statement>();
        if (Check(TokenKind.AtKeyword))
        {
            Advance(); // AT
            Match(TokenKind.EndKeyword);
            atEnd = ParseImperativeStatements(TokenKind.WhenKeyword, TokenKind.EndSearchKeyword);
        }

        var whenClauses = new List<SearchWhenClause>();
        while (Check(TokenKind.WhenKeyword))
        {
            int whenStart = Current.Span.Start;
            Advance(); // WHEN
            var condition = ParseConditionExpression();
            var stmts = ParseImperativeStatements(TokenKind.WhenKeyword, TokenKind.EndSearchKeyword);
            whenClauses.Add(new SearchWhenClause(condition, stmts,
                TextSpan.FromBounds(whenStart, Current.Span.Start)));
        }

        Match(TokenKind.EndSearchKeyword);

        return new SearchStatement(tableName, whenClauses, atEnd,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── GOBACK ──

    private GobackStatement ParseGobackStatement()
    {
        int start = Current.Span.Start;
        Advance(); // GOBACK
        return new GobackStatement(TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── NEXT SENTENCE ──

    private ContinueStatement ParseNextSentenceStatement()
    {
        int start = Current.Span.Start;
        Advance(); // NEXT
        // Consume optional SENTENCE
        if (Check(TokenKind.Identifier) && Current.Text.Equals("SENTENCE", StringComparison.OrdinalIgnoreCase))
            Advance();
        return new ContinueStatement(TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── RETURN (sort output procedure) ──

    private Statement ParseReturnSortStatement()
    {
        int start = Current.Span.Start;
        Advance(); // RETURN
        if (Check(TokenKind.Identifier)) Advance(); // file-name
        if (Check(TokenKind.RecordKeyword)) Advance(); // optional RECORD
        // INTO identifier
        IdentifierExpression? into = null;
        if (Match(TokenKind.IntoKeyword))
        {
            var intoToken = Expect(TokenKind.Identifier, "Expected identifier after INTO");
            into = new IdentifierExpression(intoToken.Text, intoToken.Span);
        }
        // AT END / NOT AT END
        SkipExceptionPhrases(TokenKind.EndReturnKeyword);
        Match(TokenKind.EndReturnKeyword);
        // Use ContinueStatement as placeholder — no AST node exists
        return new ContinueStatement(TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── RELEASE (sort input procedure) ──

    private Statement ParseReleaseSortStatement()
    {
        int start = Current.Span.Start;
        Advance(); // RELEASE
        if (Check(TokenKind.Identifier)) Advance(); // record-name
        if (Match(TokenKind.FromKeyword))
            ParseExpression(); // discard
        return new ContinueStatement(TextSpan.FromBounds(start, Current.Span.Start));
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

    // ═══════════════════════════════════════════════════
    // Expression parsing
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Parse a general expression (used in DISPLAY operands, MOVE source, VALUE, etc.)
    /// </summary>
    private Expression ParseExpression()
    {
        // Handle unary +/- for signed literals (VALUE +123, VALUE -45.6)
        if ((Check(TokenKind.Plus) || Check(TokenKind.Minus)) &&
            (Peek().Kind == TokenKind.IntegerLiteral || Peek().Kind == TokenKind.DecimalLiteral))
        {
            bool negate = Check(TokenKind.Minus);
            Advance(); // consume +/-
            var lit = ParsePrimaryExpression();
            if (negate && lit is NumericLiteralExpression numLit)
                return new NumericLiteralExpression(-numLit.Value,
                    TextSpan.FromBounds(numLit.Span.Start - 1, numLit.Span.End));
            return lit;
        }
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
        if (Check(TokenKind.Plus))
        {
            // Unary plus — consume and return the operand as-is
            Advance();
            return ParsePrimaryExpression();
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
                // Consume IN/OF qualification chain: NAME IN PARENT OF GRANDPARENT
                // Keep only the most specific (leftmost) name for now
                while (Check(TokenKind.InKeyword) || Check(TokenKind.OfKeyword))
                {
                    Advance(); // IN or OF
                    if (Check(TokenKind.Identifier))
                        Advance(); // qualifier name — consumed but not stored
                }
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

            case TokenKind.TrueKeyword:
                Advance();
                return new NumericLiteralExpression(1, token.Span); // TRUE → 1

            case TokenKind.FalseKeyword:
                Advance();
                return new NumericLiteralExpression(0, token.Span); // FALSE → 0

            case TokenKind.FunctionKeyword:
                return ParseFunctionCall();

            case TokenKind.HexLiteral:
            case TokenKind.BooleanLiteral:
            case TokenKind.NationalLiteral:
                Advance();
                return new StringLiteralExpression((string)token.Value!, token.Span);

            case TokenKind.AllKeyword:
                // ALL literal — figurative constant meaning "fill with literal"
                Advance(); // ALL
                if (Check(TokenKind.StringLiteral))
                {
                    var allLit = Advance();
                    return new StringLiteralExpression((string)allLit.Value!,
                        TextSpan.FromBounds(token.Span.Start, allLit.Span.End));
                }
                else if (Check(TokenKind.ZeroKeyword) || Check(TokenKind.SpaceKeyword) ||
                         Check(TokenKind.HighValueKeyword) || Check(TokenKind.LowValueKeyword) ||
                         Check(TokenKind.QuoteKeyword))
                {
                    // ALL ZEROS, ALL SPACES, etc. — parse the figurative constant
                    return ParsePrimaryExpression();
                }
                else if (Check(TokenKind.IntegerLiteral) || Check(TokenKind.DecimalLiteral))
                {
                    var allNum = Advance();
                    return new NumericLiteralExpression(Convert.ToDecimal(allNum.Value),
                        TextSpan.FromBounds(token.Span.Start, allNum.Span.End));
                }
                // ALL by itself (e.g., in INSPECT TALLYING ALL)
                return new StringLiteralExpression("ALL", token.Span);

            case TokenKind.Comma:
                // Commas are separators in COBOL — skip and parse the next expression
                Advance();
                return ParsePrimaryExpression();

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
    private FunctionCallExpression ParseFunctionCall()
    {
        int start = Current.Span.Start;
        Advance(); // FUNCTION

        string funcName = Expect(TokenKind.Identifier, "Expected function name after FUNCTION").Text;

        var args = new List<Expression>();
        if (Match(TokenKind.LeftParen))
        {
            while (!Check(TokenKind.RightParen) && Current.Kind != TokenKind.EndOfFile)
            {
                args.Add(ParseArithmeticExpression());
                if (!Check(TokenKind.RightParen))
                    Match(TokenKind.Comma); // optional comma separator
            }
            Expect(TokenKind.RightParen, "Expected ) after function arguments");
        }

        return new FunctionCallExpression(funcName.ToUpperInvariant(), args,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

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

        // Subscripts: NAME(expr1, expr2, ...) or NAME(expr1 expr2 ...)
        // COBOL subscripts can be separated by commas OR spaces
        var subscripts = new List<Expression> { first };
        Match(TokenKind.Comma); // optional comma separator
        while (!Check(TokenKind.RightParen) && !Check(TokenKind.Colon) &&
               Current.Kind != TokenKind.EndOfFile && !Check(TokenKind.Period))
        {
            subscripts.Add(ParseArithmeticExpression());
            Match(TokenKind.Comma); // optional comma separator
        }

        // Check for reference modification after subscripts: NAME(sub1, sub2)(start:len)
        // or colon in the middle if it's actually ref-mod: NAME(start : length)
        if (Check(TokenKind.Colon))
        {
            // This was actually reference modification, not subscripts
            // The first subscript is the ref-mod start
            Advance(); // consume :
            Expression? length = null;
            if (!Check(TokenKind.RightParen))
            {
                length = ParseArithmeticExpression();
            }
            Expect(TokenKind.RightParen, "Expected ) after reference modification");
            var spanRefMod = TextSpan.FromBounds(nameToken.Span.Start, Current.Span.Start);
            // If we had multiple "subscripts" before the colon, the last one is
            // actually the ref-mod start and the preceding ones are real subscripts
            if (subscripts.Count > 1)
            {
                var refStart = subscripts[^1];
                var realSubs = subscripts.GetRange(0, subscripts.Count - 1);
                return new IdentifierExpression(nameToken.Text, spanRefMod,
                    subscripts: realSubs, refModStart: refStart, refModLength: length);
            }
            return new IdentifierExpression(nameToken.Text, spanRefMod,
                refModStart: first, refModLength: length);
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

            // Check for abbreviated combined relation: A > B OR <= C
            if (left is BinaryExpression leftBin && IsRelationalOp(leftBin.Operator))
            {
                int saved = _position;
                var abbrevOp = TryParseRelationalOperator();
                if (abbrevOp.HasValue)
                {
                    var abbrevRight = ParseArithmeticExpression();
                    var right = new BinaryExpression(leftBin.Left, abbrevOp.Value, abbrevRight,
                        TextSpan.FromBounds(abbrevRight.Span.Start, abbrevRight.Span.End));
                    left = new BinaryExpression(left, BinaryOperator.Or, right,
                        TextSpan.FromBounds(left.Span.Start, right.Span.End));
                    continue;
                }
                _position = saved;
            }

            // Check for abbreviated: A > B OR C → A > B OR A > C
            var rightExpr = ParseAndExpression();
            if (rightExpr is not BinaryExpression && left is BinaryExpression leftBin2 &&
                IsRelationalOp(leftBin2.Operator))
            {
                rightExpr = new BinaryExpression(leftBin2.Left, leftBin2.Operator, rightExpr,
                    TextSpan.FromBounds(rightExpr.Span.Start, rightExpr.Span.End));
            }

            left = new BinaryExpression(left, BinaryOperator.Or, rightExpr,
                TextSpan.FromBounds(left.Span.Start, rightExpr.Span.End));
        }
        return left;
    }

    private Expression ParseAndExpression()
    {
        var left = ParseNotExpression();
        while (Check(TokenKind.AndKeyword))
        {
            Advance();

            // Check for abbreviated combined relation: A > B AND <= C
            // If left is a relational expression and we see a relational operator,
            // carry the subject from the left side
            if (left is BinaryExpression leftBin && IsRelationalOp(leftBin.Operator))
            {
                // Try to parse a relational operator at the current position
                int saved = _position;
                var abbrevOp = TryParseRelationalOperator();
                if (abbrevOp.HasValue)
                {
                    // Abbreviated form: A > B AND <= C → A > B AND A <= C
                    var abbrevRight = ParseArithmeticExpression();
                    var right = new BinaryExpression(leftBin.Left, abbrevOp.Value, abbrevRight,
                        TextSpan.FromBounds(abbrevRight.Span.Start, abbrevRight.Span.End));
                    left = new BinaryExpression(left, BinaryOperator.And, right,
                        TextSpan.FromBounds(left.Span.Start, right.Span.End));
                    continue;
                }
                _position = saved;
            }

            var rightExpr = ParseNotExpression();

            // If right is just a bare value (not a relational/logical expression),
            // and left is a relational expression, expand the abbreviation
            // A > B AND C → A > B AND A > C
            if (rightExpr is not BinaryExpression && left is BinaryExpression leftBin2 &&
                IsRelationalOp(leftBin2.Operator))
            {
                rightExpr = new BinaryExpression(leftBin2.Left, leftBin2.Operator, rightExpr,
                    TextSpan.FromBounds(rightExpr.Span.Start, rightExpr.Span.End));
            }

            left = new BinaryExpression(left, BinaryOperator.And, rightExpr,
                TextSpan.FromBounds(left.Span.Start, rightExpr.Span.End));
        }
        return left;
    }

    private static bool IsRelationalOp(BinaryOperator op) => op is
        BinaryOperator.Equal or BinaryOperator.NotEqual or
        BinaryOperator.LessThan or BinaryOperator.GreaterThan or
        BinaryOperator.LessThanOrEqual or BinaryOperator.GreaterThanOrEqual;

    private Expression ParseNotExpression()
    {
        if (Check(TokenKind.NotKeyword))
        {
            int start = Current.Span.Start;
            Advance();
            var operand = ParseNotExpression(); // NOT can apply to parenthesized conditions
            return new UnaryExpression(UnaryOperator.Not, operand,
                TextSpan.FromBounds(start, operand.Span.End));
        }
        // Per §8.8.4.9: "Parentheses may override precedence" in conditions.
        // (condition) is valid wherever a simple condition appears.
        if (Check(TokenKind.LeftParen))
        {
            // Try parsing as a parenthesized condition.
            int saved = _position;
            Advance(); // consume (
            var inner = ParseConditionExpression();
            if (Check(TokenKind.RightParen))
            {
                Advance(); // consume )
                return inner;
            }
            // Not a parenthesized condition — backtrack
            _position = saved;
        }
        return ParseRelationalExpression();
    }

    private Expression ParseRelationalExpression()
    {
        var left = ParseArithmeticExpression();

        // Check for class condition: identifier IS [NOT] NUMERIC/ALPHABETIC
        if (Check(TokenKind.IsKeyword) || Check(TokenKind.NotKeyword))
        {
            int saved = _position;
            bool isNot = false;

            if (Check(TokenKind.IsKeyword)) Advance();
            if (Check(TokenKind.NotKeyword)) { isNot = true; Advance(); }

            if (Check(TokenKind.NumericKeyword) || Check(TokenKind.AlphabeticKeyword) ||
                Check(TokenKind.PositiveKeyword) || Check(TokenKind.NegativeKeyword) ||
                Check(TokenKind.ZeroKeyword))
            {
                // Class or sign condition — represent as a binary expression
                // using Equal/NotEqual with the class as a figurative constant
                var classToken = Advance();
                Expression classExpr;
                if (classToken.Kind == TokenKind.NumericKeyword)
                    classExpr = new StringLiteralExpression("NUMERIC", classToken.Span);
                else if (classToken.Kind == TokenKind.AlphabeticKeyword)
                    classExpr = new StringLiteralExpression("ALPHABETIC", classToken.Span);
                else if (classToken.Kind == TokenKind.PositiveKeyword)
                    classExpr = new StringLiteralExpression("POSITIVE", classToken.Span);
                else if (classToken.Kind == TokenKind.NegativeKeyword)
                    classExpr = new StringLiteralExpression("NEGATIVE", classToken.Span);
                else // ZeroKeyword
                    classExpr = new FigurativeConstantExpression(FigurativeConstant.Zero, classToken.Span);

                var op2 = isNot ? BinaryOperator.NotEqual : BinaryOperator.Equal;
                return new BinaryExpression(left, op2, classExpr,
                    TextSpan.FromBounds(left.Span.Start, classToken.Span.End));
            }

            // Not a class/sign condition — restore position and try relational
            _position = saved;
        }

        // Check for relational operator
        BinaryOperator? op = TryParseRelationalOperator();
        if (op.HasValue)
        {
            var right = ParseArithmeticExpression();
            var result = new BinaryExpression(left, op.Value, right,
                TextSpan.FromBounds(left.Span.Start, right.Span.End));

            // Check for abbreviated combined relations: A > B AND C → A > B AND A > C
            // If next token is AND/OR followed by something that's NOT a statement keyword,
            // and there's no relational operator after the AND/OR operand, it's abbreviated.
            return result;
        }

        return left;
    }

    private BinaryOperator? TryParseRelationalOperator()
    {
        // = , < , > , <= , >= , NOT = , NOT < , NOT >
        // EQUAL TO, GREATER THAN, LESS THAN, etc.
        // Use lookahead to avoid consuming IS/NOT when no operator follows
        int saved = _position;
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
            // GREATER THAN OR EQUAL TO — only consume OR if followed by EQUAL
            if (Check(TokenKind.OrKeyword) && Peek().Kind == TokenKind.EqualKeyword)
            {
                Advance(); // OR
                Advance(); // EQUAL
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
            if (Check(TokenKind.OrKeyword) && Peek().Kind == TokenKind.EqualKeyword)
            {
                Advance(); // OR
                Advance(); // EQUAL
                Match(TokenKind.ToKeyword);
                op = BinaryOperator.LessThanOrEqual;
            }
            else
            {
                op = BinaryOperator.LessThan;
            }
        }

        // If we consumed IS/NOT but found no operator, restore position
        if (!op.HasValue && _position != saved)
        {
            _position = saved;
            return null;
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
