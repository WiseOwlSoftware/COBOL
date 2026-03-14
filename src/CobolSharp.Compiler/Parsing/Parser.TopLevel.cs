using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Lexing;

namespace CobolSharp.Compiler.Parsing;

public sealed partial class Parser
{
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
}
