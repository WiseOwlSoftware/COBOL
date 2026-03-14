using CobolSharp.Compiler;
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Lexing;
using CobolSharp.Compiler.Preprocessor;
using Xunit;

namespace CobolSharp.Tests.Unit.Preprocessor;

public class NistCompileTests
{
    [Fact]
    public void Lexer_LargeFixedForm_Completes()
    {
        // Generate a realistic NIST-like fixed-form file
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("000100 IDENTIFICATION DIVISION.                                         TEST4.2");
        sb.AppendLine("000200 PROGRAM-ID.                                                      TEST4.2");
        sb.AppendLine("000300     BIGTEST.                                                     TEST4.2");
        sb.AppendLine("000400 DATA DIVISION.                                                   TEST4.2");
        sb.AppendLine("000500 WORKING-STORAGE SECTION.                                         TEST4.2");
        sb.AppendLine("000600 01 WS-VAR PIC X(10).                                             TEST4.2");
        sb.AppendLine("000700 PROCEDURE DIVISION.                                              TEST4.2");
        for (int i = 0; i < 500; i++)
        {
            string seq = ((i + 8) * 100).ToString("D6");
            sb.AppendLine($"{seq}     DISPLAY \"Line {i}\".                                          TEST4.2");
        }
        sb.AppendLine("999900     STOP RUN.                                                    TEST4.2");

        // Preprocess
        string normalized = ReferenceFormatProcessor.NormalizeToFreeForm(sb.ToString());
        var copyProc = new CopyProcessor();
        string processed = copyProc.Process(normalized, ".");
        var source = SourceText.From(processed, "test.cob");

        // Lex
        var diagnostics = new DiagnosticBag();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var lexer = new Lexer(source, diagnostics);
        var tokens = lexer.Tokenize();
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 2000, $"Lexing took {sw.ElapsedMilliseconds}ms");
        Assert.True(tokens.Count > 100, $"Only {tokens.Count} tokens");
    }
}
