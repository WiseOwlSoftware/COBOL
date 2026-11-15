// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol;
using WiseOwl.Cobol.Frontend.Preprocessor;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A QUALIFIER WRITTEN AFTER AN ALL SUBSCRIPT IS REPORTED ONCE, AS WHAT IT IS (kb/Work PB5485, PB5509). `TE (ALL) OF TT` writes the
/// qualifier after the subscripts, which §2023:8.4.2.3.2 Format 1 does not allow (the subscripts follow the whole qualified name); that is
/// COBOL2776 at every edition. The ALL bind of a function argument used to give the argument up for it, so the ordinary bind then refused
/// the ALL itself as COBOL2363 — a message saying ALL is permitted only in a function argument, about an ALL that sits in one. The conformance
/// suite's refusal file asserts a diagnostic is reported but cannot assert another is NOT, so this is the test of the absence.
/// </summary>
public sealed class AllSubscriptQualifierAfterSubscriptTests : WiseOwlCobolTestBase
{
    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void AQualifierAfterAnAllSubscript_IsCobol2776Alone(int edition)
    {
        const string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB5485N.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TT.
               05 TE PIC 9 OCCURS 3.
            01 W PIC 9(4).
            PROCEDURE DIVISION.
            MAIN.
                COMPUTE W = FUNCTION MAX (TE (ALL) OF TT)
                STOP RUN.
            """;
        string srcPath = Path.Combine(TempDir, $"pb5485n{edition}.cob");
        File.WriteAllText(srcPath, source);
        var compiled = CompilerDriver.Compile(new CompilerDriver.Options(
            srcPath, Path.ChangeExtension(srcPath, ".dll"), DialectLevel: edition, SourceFormat: InitialReferenceFormat.Free));
        Assert.False(compiled.Success);
        Assert.Contains(compiled.Errors, e => e.Contains("COBOL2776"));
        Assert.DoesNotContain(compiled.Errors, e => e.Contains("COBOL2363"));
        Assert.Single(compiled.Errors);
    }
}
