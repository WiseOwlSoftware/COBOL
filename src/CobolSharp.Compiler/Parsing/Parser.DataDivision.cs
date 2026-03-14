using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Lexing;

namespace CobolSharp.Compiler.Parsing;

public sealed partial class Parser
{
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
            // Issue 18 (§5.5): LOCAL-STORAGE SECTION — parse as WORKING-STORAGE
            else if (Check(TokenKind.Identifier) &&
                     Current.Text.Equals("LOCAL-STORAGE", StringComparison.OrdinalIgnoreCase))
            {
                Advance(); // LOCAL-STORAGE
                Match(TokenKind.SectionKeyword);
                Match(TokenKind.Period);
                // Parse entries and merge into working storage
                var localEntries = new List<DataDescriptionEntry>();
                while (IsLevelNumber())
                    localEntries.Add(ParseDataDescriptionEntry());
                if (ws == null)
                    ws = new WorkingStorageSection(localEntries,
                        TextSpan.FromBounds(start, Current.Span.Start));
                else
                {
                    // Merge local-storage entries into working-storage
                    foreach (var e in localEntries)
                        ws.Entries.Add(e);
                }
            }
            else
            {
                // Skip unrecognized section header
                Advance();
                if (Check(TokenKind.SectionKeyword)) Advance();
                if (Check(TokenKind.Period)) Advance();
                while (Current.Kind != TokenKind.EndOfFile && !IsDivisionStart() &&
                       !Check(TokenKind.WorkingStorageKeyword) && !Check(TokenKind.LinkageKeyword) &&
                       !Check(TokenKind.FileKeyword) && !Check(TokenKind.ReportKeyword) &&
                       !Check(TokenKind.ScreenKeyword) &&
                       !(Check(TokenKind.Identifier) && Current.Text.Equals("LOCAL-STORAGE", StringComparison.OrdinalIgnoreCase)))
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
            // Issue 75 (§5.5): LINAGE clause
            else if (Check(TokenKind.LinageKeyword))
            {
                Advance(); // LINAGE
                Match(TokenKind.IsKeyword);
                // integer or identifier (number of lines)
                if (Check(TokenKind.IntegerLiteral) || Check(TokenKind.Identifier))
                    Advance();
                // Optional LINES
                if (Check(TokenKind.Identifier) && Current.Text.Equals("LINES", StringComparison.OrdinalIgnoreCase))
                    Advance();
                // Optional WITH FOOTING AT / LINES AT TOP / LINES AT BOTTOM
                while (Check(TokenKind.WithKeyword) || Check(TokenKind.Identifier))
                {
                    string w = Current.Text.ToUpperInvariant();
                    if (w == "WITH" || w == "FOOTING" || w == "LINES" || w == "TOP" || w == "BOTTOM")
                    {
                        Advance();
                        Match(TokenKind.AtKeyword);
                        if (Check(TokenKind.IntegerLiteral) || Check(TokenKind.Identifier))
                            Advance();
                    }
                    else break;
                }
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
                    // Issue 80 (§5.5.1): Multiple key names — stop at clause keywords
                    while (Check(TokenKind.Identifier) &&
                           !Check(TokenKind.AscendingKeyword) && !Check(TokenKind.DescendingKeyword) &&
                           !Check(TokenKind.IndexedKeyword) && !IsDataClauseKeyword(Current.Kind))
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
                // Issue 79 (§5.5.1): SYNC [LEFT|RIGHT] — only consume if LEFT or RIGHT
                if (Check(TokenKind.Identifier))
                {
                    string syncDir = Current.Text.ToUpperInvariant();
                    if (syncDir == "LEFT" || syncDir == "RIGHT")
                        Advance();
                }
            }
            // Issue 77 (§5.5.1): SIGN IS LEADING/TRAILING [SEPARATE CHARACTER]
            else if (Check(TokenKind.Identifier) && Current.Text.Equals("SIGN", StringComparison.OrdinalIgnoreCase))
            {
                Advance(); // SIGN
                Match(TokenKind.IsKeyword); // optional IS
                if (Check(TokenKind.Identifier))
                {
                    string signDir = Current.Text.ToUpperInvariant();
                    if (signDir == "LEADING" || signDir == "TRAILING")
                    {
                        Advance();
                        // SEPARATE [CHARACTER]
                        if (Check(TokenKind.Identifier) && Current.Text.Equals("SEPARATE", StringComparison.OrdinalIgnoreCase))
                        {
                            Advance();
                            if (Check(TokenKind.Identifier) && Current.Text.Equals("CHARACTER", StringComparison.OrdinalIgnoreCase))
                                Advance();
                        }
                    }
                }
            }
            // Issue 77 (§5.5.1): LEADING/TRAILING without SIGN prefix (shorthand)
            else if (Check(TokenKind.Identifier) &&
                     (Current.Text.Equals("LEADING", StringComparison.OrdinalIgnoreCase) ||
                      Current.Text.Equals("TRAILING", StringComparison.OrdinalIgnoreCase)))
            {
                Advance();
                if (Check(TokenKind.Identifier) && Current.Text.Equals("SEPARATE", StringComparison.OrdinalIgnoreCase))
                {
                    Advance();
                    if (Check(TokenKind.Identifier) && Current.Text.Equals("CHARACTER", StringComparison.OrdinalIgnoreCase))
                        Advance();
                }
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
}
