// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol;
using WiseOwl.Cobol.CodeGen;
using WiseOwl.Cobol.Editions;
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
    [InlineData("bool __sizeErr0 = false;")]          // a declaration: the parser decides (a constant initializer's point is droppable)
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

    /// <summary>A statement whose C# begins with a local declared with a constant (an ADD ON SIZE ERROR's <c>bool __sizeErr0 = false;</c>,
    /// kb/Work PB5925 review R2-1), which an optimized build keeps or drops as the code after it reads the local: either way the statement's
    /// first point is at its first code, so the statement before it holds its own code alone (a call, five bytes of IL), and where the writer
    /// can show the store kept (the local read, and used in a try or catch block, an if's body or a loop) the statement has one point; with a nested statement, the
    /// statement's own first code is a call too, and so is the nested one's. The expected points are the
    /// ones Roslyn itself writes, read back from its PDB; the stops are each shape's: kept and shown (one), kept but not shown (two, the
    /// store's and the next line's), dropped (one: Roslyn keeps the last point of an IL offset). After a nested statement the same holds
    /// for the enclosing statement's resumed point.</summary>
    [Theory]
    [InlineData("try { if (G > 3) e = true; else G = 1; } catch (System.Exception) { e = true; }|if (e) F();", 1, false)]
    [InlineData("F();|if (G > 2) e = true;|if (e) F();", 1, false)]
    [InlineData("while (G < 3) { if (e) F(); G++; }", 1, false)]
    [InlineData("F();|if (e) F();", 2, false)]
    [InlineData("e = G > 5;|if (e) F();", 1, false)]
    [InlineData("try { F(); } catch (System.Exception) { e = true; }", 1, false)]
    [InlineData("try { if (G > 3) e = true; else G = 1; } catch (System.Exception) { e = true; }|if (e) F();", 1, true)]
    [InlineData("e = G > 5;|if (e) F();", 1, true)]
    public void AStatementBeginningWithAConstantStore_OwnsItsFirstCode(string lines, int stops, bool resumed)
    {
        string file = Path.Combine(TempDir, "flag.cob");
        var before = new WiseOwl.Cobol.Binding.Bound.StatementSpan(file, 1, 8, 1, 20, 12);
        var statement = new WiseOwl.Cobol.Binding.Bound.StatementSpan(file, 2, 8, 2, 30, 11);
        var nested = new WiseOwl.Cobol.Binding.Bound.StatementSpan(file, 3, 12, 3, 24, 16);
        var w = new CodeWriter();
        w.HideUnmappedLines(TempDir);
        using (w.Block("class C"))
        {
            w.Line("static int G;");
            w.Line("static void F() { G++; }");
            using (w.Block("static void M()"))
            {
                using (w.MapStatement(before)) w.Line("F();");   // call: 5 bytes of IL
                using (w.MapStatement(statement))
                {
                    if (resumed)
                    {
                        w.Line("F();");                              // the statement's own first code: 5 bytes
                        using (w.MapStatement(nested)) w.Line("F();");   // the nested statement's: 5 bytes
                    }
                    w.Line("bool e = false;");
                    foreach (string line in lines.Split('|')) w.Line(line);
                }
            }
        }
        var points = VisiblePoints(w.ToString());
        Assert.Equal(0, Assert.Single(points, p => p.Line == 1).Offset);
        if (resumed)
        {
            Assert.Equal(10, Assert.Single(points, p => p.Line == 3).Offset);
            var own = points.Where(p => p.Line == 2).OrderBy(p => p.Offset).ToList();
            Assert.Equal(5, own[0].Offset);   // the statement's first point spans the statement
            var verb = own.Skip(1).ToList();
            Assert.Equal(stops, verb.Count);
            Assert.Equal(15, verb[0].Offset);   // the resumed point: the store after the nested statement is the enclosing one's
            Assert.All(verb, p => Assert.Equal(11, p.EndColumn));   // spanning the verb
        }
        else
        {
            var own = points.Where(p => p.Line == 2).OrderBy(p => p.Offset).ToList();
            Assert.Equal(stops, own.Count);
            Assert.Equal(5, own[0].Offset);
            Assert.All(own, p => Assert.Equal(30, p.EndColumn));   // each spanning the statement
        }
    }

    /// <summary>The points of a copybook's statement at one position, paired with its copies' listing lines (DESIGN-listing.md section 6.9):
    /// "S" a first point (spanning the statement, end (1, 30)), "V" a later one (its verb, end (1, 15)). One first point per copy, or two
    /// (a droppable store kept and not shown kept, <see cref="AStatementBeginningWithAConstantStore_OwnsItsFirstCode"/>), each copy takes
    /// its own; a verb point goes with the first point before it; counts that do not divide fall back to k-th to k-th, the rest to the
    /// last.</summary>
    [Theory]
    [InlineData("S S", 2, "0 1")]
    [InlineData("S V S V", 2, "0 0 1 1")]
    [InlineData("S S V S S V", 2, "0 0 0 1 1 1")]
    [InlineData("S S S S", 2, "0 0 1 1")]
    [InlineData("S S S", 2, "0 1 1")]
    [InlineData("S S V", 1, "0 0 0")]
    public void ACopybooksPoints_GoToTheirCopies(string points, int lines, string expected)
    {
        var ends = points.Split(' ').Select(p => p == "S" ? (1, 30) : (1, 15)).ToList();
        Assert.Equal(expected, string.Join(" ", WiseOwl.Cobol.Listing.Building.ObjectCodeMapBuilder.Copies(ends, lines)));
    }

    [Fact]
    public void OnAProgramsGeneratedSource_TheFastReadingAgreesWithTheParse()
    {
        string dll = Path.Combine(TempDir, "NC101A.dll");
        var result = CompilerDriver.Compile(new CompilerDriver.Options(TestRepo.Nist("programs", "NC101A.cob"), EditionInfo.Of(85), dll, NistTestName: "NC101A"));
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
