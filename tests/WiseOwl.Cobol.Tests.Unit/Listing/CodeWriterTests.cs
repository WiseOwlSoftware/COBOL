// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol;
using WiseOwl.Cobol.CodeGen;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// The Roslyn backend's reading of where a line's first sequence point lies (DESIGN-listing.md section 6.9;
/// <see cref="CodeWriter.FirstSequencePoint(string)"/>): the fast reading of a simple statement (<see cref="CodeWriter.SimpleStatement"/>)
/// is held equal to Roslyn's parse (<see cref="CodeWriter.ParsedSequencePoint"/>) on every line of a real program's generated source and on
/// hand-written shapes, so the fast path can never place a point the parser would not.
/// </summary>
public sealed class CodeWriterTests : WiseOwlCobolTestBase
{
    [Theory]
    [InlineData("    B = (long)(CobolNum.Store(A, 0, _P_1, CobolRounding.Truncation));", 4, 69)]
    [InlineData("Foo(a, b);", 0, 10)]
    [InlineData("    X.Y[3] = Z == W;  ", 4, 20)]
    public void ASimpleStatement_IsTheWholeLine_AsTheParserReadsIt(string line, int start, int end)
    {
        Assert.Equal((start, end), CodeWriter.SimpleStatement(line));
        Assert.Equal((start, end), CodeWriter.ParsedSequencePoint(line));
    }

    [Theory]
    [InlineData("bool __sizeErr0 = false;")]          // a declaration: the parser decides (a constant initializer has no point)
    [InlineData("__pc = 5; goto __xfer;")]           // two statements
    [InlineData("x = F(() => { y = 2; return y; });")] // a lambda
    [InlineData("s = \"a;b\";")]                    // a string
    [InlineData("Foo<int, long>(x);")]               // a generic call with a space before its parenthesis
    [InlineData("if (A > 1L)")]                      // a header
    [InlineData("x++;")]                             // neither an assignment nor a call
    [InlineData("return;")]
    public void AnythingElse_IsLeftToTheParser(string line) => Assert.Null(CodeWriter.SimpleStatement(line));

    /// <summary>A line that continues a statement an earlier line began (kb/Work PB5925 review R1-1: an EVALUATE's second WHEN compiles to
    /// an <c>else if (...)</c> line, which a parse of the line alone reads as no statement) is read as Roslyn compiles it: the expected point
    /// is the first visible sequence point Roslyn itself writes on that line, in an optimized build with a portable PDB, read back here with
    /// <c>System.Reflection.Metadata</c>; none when Roslyn writes none.</summary>
    [Theory]
    [InlineData("if (a == 1)\n{\nF();\n}", "else if (a == 2)", "{\nF();\n}")]
    [InlineData("if (a == 1) G = 2;", "else G = 3;", "")]
    [InlineData("if (a == 1) G = 2;", "else F();", "")]
    [InlineData("if (a == 1) {\nF();", "} else {", "F();\n}")]
    [InlineData("if (a == 1) {\nF();", "} else if (a == 3) {", "F();\n}")]
    [InlineData("if (a == 1) G = 2;", "else", "F();")]
    [InlineData("try { F(); }", "catch (System.InvalidOperationException e) when (e.Message == \"x\")", "{ G = 1; }")]
    [InlineData("try { F(); }", "catch (System.InvalidOperationException) { G = 1; }", "")]
    [InlineData("try { F(); }", "catch (System.InvalidOperationException)", "{ G = 1; }")]
    [InlineData("try { F(); }", "catch { G = 1; }", "")]
    [InlineData("try {\nF();", "} catch (System.InvalidOperationException e) {", "G = 1;\n}")]
    [InlineData("try {\nF();", "} finally {", "G = 2;\n}")]
    [InlineData("if (a == 1)\n{\nG = 1;", "}", "")]
    [InlineData("while (a == 1)\n{\nG = 1;", "}", "")]
    [InlineData("for (int i = 0; i < a; i++)\n{\nG = 1;", "}", "")]
    [InlineData("foreach (var c in \"ab\")\n{\nG = 1;", "}", "")]
    [InlineData("switch (a)\n{\ncase 1:\n{\nG = 1;", "}", "break;\n}")]
    [InlineData("{\nG = 1;", "}", "")]
    [InlineData("try { F(); }", "finally { G = 2; }", "")]
    [InlineData("try { F(); }\ncatch (System.InvalidOperationException) { G = 1; }", "finally", "{ G = 2; }")]
    [InlineData("do\n{\nF();", "} while (G < 3);", "")]
    [InlineData("switch (a)\n{", "case 1: G = 3; break;", "default: break;\n}")]
    [InlineData("switch (a)\n{", "case 1:", "G = 3; break;\ndefault: break;\n}")]
    [InlineData("switch (a)\n{\ncase 1: G = 3; break;", "default: G = 4; break;", "}")]
    [InlineData("", "elsewhere = 3;", "")]   // a name that begins with a keyword is no continuation
    public void AContinuationLine_IsReadAsRoslynCompilesIt(string before, string line, string after)
    {
        string[] head = before.Length == 0 ? [] : before.Split('\n');
        string source = "class C\n{\nstatic int G;\nstatic int elsewhere;\nstatic void F() { G++; }\nstatic void M(int a)\n{\n"
            + string.Join("", head.Select(l => l + "\n")) + line + "\n" + after + "\n}\n}\n";
        int lineNumber = 7 + head.Length + 1;   // 1-based: the class's seven lines, the lines before, then the line
        var onLine = VisiblePoints(source)
            .Where(p => p.Line == lineNumber)
            .Select(p => (Start: p.Column - 1, End: p.EndLine == lineNumber ? p.EndColumn - 1 : line.Length))
            .OrderBy(p => p.Start).ToList();
        (int Start, int End)? expected = onLine.Count == 0 ? null : onLine[0];
        Assert.Equal(expected, CodeWriter.FirstSequencePoint(line));
    }

    /// <summary>Every visible sequence point Roslyn writes for <paramref name="source"/>, compiled optimized with a portable PDB: its document,
    /// 1-based start and end, and its method and IL offset.</summary>
    private static List<(string Document, int Line, int Column, int EndLine, int EndColumn, int Method, int Offset)> VisiblePoints(string source)
    {
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
        var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("probe", [tree],
            [Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: Microsoft.CodeAnalysis.OptimizationLevel.Release));
        using var pe = new MemoryStream();
        using var pdb = new MemoryStream();
        var emit = compilation.Emit(pe, pdb, options: new Microsoft.CodeAnalysis.Emit.EmitOptions(
            debugInformationFormat: Microsoft.CodeAnalysis.Emit.DebugInformationFormat.PortablePdb));
        Assert.True(emit.Success, string.Join("\n", emit.Diagnostics));
        pdb.Position = 0;
        using var provider = System.Reflection.Metadata.MetadataReaderProvider.FromPortablePdbStream(pdb);
        var reader = provider.GetMetadataReader();
        return [.. reader.MethodDebugInformation.SelectMany(m => reader.GetMethodDebugInformation(m).GetSequencePoints()
                .Where(p => !p.IsHidden)
                .Select(p => (reader.GetString(reader.GetDocument(p.Document).Name), p.StartLine, p.StartColumn, p.EndLine, p.EndColumn,
                    System.Reflection.Metadata.Ecma335.MetadataTokens.GetRowNumber(m), p.Offset)))];
    }

    /// <summary>The brace that ends a try, catch, finally, lock or using block is a sequence point of an optimized build (the leave, the
    /// release, the dispose), which a reading of the line alone cannot tell from an if's; the writer reads it from the blocks its lines
    /// opened. A statement that writes such a block around a nested statement owns that code: the brace takes the statement's point once
    /// more, after the nested statement's (kb/Work PB5925 review R1-1's sweep). After an if block, nothing of the statement's follows.</summary>
    [Theory]
    [InlineData("try", "finally { G = 2; }", true)]
    [InlineData("lock (typeof(C))", "", true)]
    [InlineData("using (var d = new System.IO.MemoryStream())", "", true)]
    [InlineData("try   // a comment", "catch (System.InvalidOperationException) { }", true)]
    [InlineData("if (G == 1)", "", false)]
    [InlineData("while (G == 1)", "", false)]
    public void TheBraceEndingABlock_IsTheEnclosingStatementsCode_WhenRoslynGivesItAPoint(string header, string trailer, bool resumed)
    {
        string file = Path.Combine(TempDir, "brace.cob");
        var outer = new WiseOwl.Cobol.Binding.Bound.StatementSpan(file, 1, 8, 1, 20, 12);
        var inner = new WiseOwl.Cobol.Binding.Bound.StatementSpan(file, 2, 12, 2, 24, 16);
        var w = new CodeWriter();
        w.HideUnmappedLines(TempDir);
        using (w.Block("class C"))
        {
            w.Line("static int G;");
            using (w.Block("static void M()"))
            using (w.MapStatement(outer))
            {
                w.Line("G = 0;");
                using (w.Block(header))
                using (w.MapStatement(inner))
                    w.Line("G = 1;");
                if (trailer.Length > 0) w.Line(trailer);
            }
        }
        var points = VisiblePoints(w.ToString());
        Assert.All(points, p => Assert.Equal("brace.cob", p.Document));
        var atOuter = points.Where(p => (p.Line, p.Column) == (1, 8)).OrderBy(p => p.Offset).ToList();
        var atInner = Assert.Single(points, p => (p.Line, p.Column) == (2, 12));
        Assert.Equal(points.Count, atOuter.Count + 1);
        Assert.Equal(resumed ? 2 : 1, atOuter.Count);
        Assert.True(atOuter[0].Offset < atInner.Offset);
        if (resumed) Assert.True(atInner.Offset < atOuter[1].Offset && atOuter[1].EndColumn < atOuter[0].EndColumn);   // the later point spans the verb
    }

    [Fact]
    public void OnAProgramsGeneratedSource_TheFastReadingAgreesWithTheParse()
    {
        string dll = Path.Combine(TempDir, "NC101A.dll");
        var result = CompilerDriver.Compile(new CompilerDriver.Options(TestRepo.Nist("programs", "NC101A.cob"), dll, NistTestName: "NC101A",
            DialectLevel: 85));
        Assert.True(result.Success);
        int simple = 0;
        foreach (string line in File.ReadLines(result.GeneratedCsPath!))
        {
            if (line.TrimStart().StartsWith('#') || CodeWriter.SimpleStatement(line) is not { } fast) continue;
            simple++;
            Assert.True(CodeWriter.ParsedSequencePoint(line) == fast, line);
        }
        Assert.True(simple > 500, $"only {simple} simple lines were compared");
    }
}
