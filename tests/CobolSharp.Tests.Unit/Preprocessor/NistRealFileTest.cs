using CobolSharp.Compiler;
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Lexing;
using CobolSharp.Compiler.Parsing;
using CobolSharp.Compiler.Preprocessor;
using Xunit;

namespace CobolSharp.Tests.Unit.Preprocessor;

public class NistRealFileTest
{
    [Fact]
    public void IF101A_Preprocesses_And_Lexes()
    {
        string path = @"E:\CobolSharp\tests\nist\programs\IF101A.cob";
        if (!File.Exists(path)) return; // skip if NIST files not available

        string raw = File.ReadAllText(path);
        string normalized = ReferenceFormatProcessor.NormalizeToFreeForm(raw);

        var copyProc = new CopyProcessor();
        string processed = copyProc.Process(normalized, Path.GetDirectoryName(path)!);

        var source = SourceText.From(processed, path);
        var diagnostics = new DiagnosticBag();

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var lexer = new Lexer(source, diagnostics);
        var tokens = lexer.Tokenize();
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 2000, $"Lexing took {sw.ElapsedMilliseconds}ms");
        Assert.True(tokens.Count > 100);
    }

    [Fact]
    public void IF101A_Parses()
    {
        string path = @"E:\CobolSharp\tests\nist\programs\IF101A.cob";
        if (!File.Exists(path)) return;

        string raw = File.ReadAllText(path);
        string normalized = ReferenceFormatProcessor.NormalizeToFreeForm(raw);
        var copyProc = new CopyProcessor();
        string processed = copyProc.Process(normalized, Path.GetDirectoryName(path)!);
        var source = SourceText.From(processed, path);
        var diagnostics = new DiagnosticBag();
        var lexer = new Lexer(source, diagnostics);
        var tokens = lexer.Tokenize();

        // Check token count first
        Assert.True(tokens.Count < 100000, $"Too many tokens: {tokens.Count}");

        try
        {
            var parser = new Parser(tokens, diagnostics, source);
            var ast = parser.ParseCompilationUnit();
            // If we get here, parsing completed
            Assert.NotNull(ast);
        }
        catch (StackOverflowException)
        {
            Assert.Fail("Stack overflow during parsing — deep recursion detected");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("iterations"))
        {
            Assert.Fail(ex.Message);
        }
    }
}
