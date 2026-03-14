using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Lexing;

namespace CobolSharp.Compiler.Parsing;

public sealed partial class Parser
{
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
}
