using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Lexing;

namespace CobolSharp.Compiler.Parsing;

public sealed partial class Parser
{
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
            // Issue 15 (§5.3.1): PROGRAM-ID. name [AS literal] [{COMMON|INITIAL|RECURSIVE} PROGRAM].
            if (Check(TokenKind.Identifier) && Current.Text.Equals("AS", StringComparison.OrdinalIgnoreCase))
            {
                Advance(); // AS
                if (Check(TokenKind.StringLiteral)) Advance(); // literal
            }
            // COMMON, INITIAL, RECURSIVE modifiers
            while (Check(TokenKind.Identifier))
            {
                string mod = Current.Text.ToUpperInvariant();
                if (mod == "COMMON" || mod == "INITIAL" || mod == "RECURSIVE")
                    Advance();
                else break;
            }
            // Optional PROGRAM keyword
            if (Check(TokenKind.Identifier) && Current.Text.Equals("PROGRAM", StringComparison.OrdinalIgnoreCase))
                Advance();
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
}
