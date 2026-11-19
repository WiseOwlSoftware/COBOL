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
