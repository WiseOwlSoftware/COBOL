// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Preprocessor;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A QUALIFIER WRITTEN AFTER AN ALL SUBSCRIPT IS REPORTED ONCE, AS WHAT IT IS (kb/Work PB5485, PB5509). `TE (ALL) OF TT` writes the
/// qualifier after the subscripts, which §2023:8.4.2.3.2 Format 1 does not allow (the subscripts follow the whole qualified name); that is
/// COBOL2776 at every edition. The ALL bind of a function argument used to give the argument up for it, so the ordinary bind then refused
/// the ALL itself as COBOL2363 — a message saying ALL is permitted only in a function argument, about an ALL that sits in one. The conformance
/// suite's refusal file asserts a diagnostic is reported but cannot assert another is NOT, so this is the test of the absence.
/// <para>The same holds when the qualifier written after the ALL subscript carries a reference modifier (TE (ALL) OF TT (1:1),
/// kb/Work PB5544): the walk refused a qualifier with a modifier and no subscripts even when the subscript group came before it. The
/// ALL subscript is in the 1985 edition too (Amendment 1, §1985:IV.4.3.8.2.3 SR7), and its identifier format puts the subscripts after
/// the qualifiers as 2023's does (§1985:IV.4.3.8.5.2), so each spelling is asked at 85 and at 2023.</para>
/// </summary>
public sealed class AllSubscriptQualifierAfterSubscriptTests : WiseOwlCobolTestBase
{
    [Theory]
    [InlineData(85, "COMPUTE W = FUNCTION MAX (TE (ALL) OF TT)")]
    [InlineData(2023, "COMPUTE W = FUNCTION MAX (TE (ALL) OF TT)")]
    [InlineData(85, "MOVE FUNCTION MAX (TE (ALL) OF TT (1:1)) TO R")]
    [InlineData(2023, "MOVE FUNCTION MAX (TE (ALL) OF TT (1:1)) TO R")]
    [InlineData(85, "MOVE FUNCTION MAX (TE (ALL) OF G OF TT (1:1)) TO R")]
    [InlineData(2023, "MOVE FUNCTION MAX (TE (ALL) OF G OF TT (1:1)) TO R")]
    public void AQualifierAfterAnAllSubscript_IsCobol2776Alone(int edition, string statement)
    {
        string source = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB5485N.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TT.
               02 G.
                  05 TE PIC 9 OCCURS 3.
            01 W PIC 9(4).
            01 R PIC X.
            PROCEDURE DIVISION.
            MAIN.
                {statement}
                STOP RUN.
            """;
        string srcPath = Path.Combine(TempDir, $"pb5485n{edition}-{statement.Length}.cob");
        File.WriteAllText(srcPath, source);
        var compiled = CompilerDriver.Compile(new CompilerDriver.Options(
            srcPath, EditionInfo.Of(edition), Path.ChangeExtension(srcPath, ".dll"), SourceFormat: InitialReferenceFormat.Free));
        Assert.False(compiled.Success);
        Assert.Contains(compiled.Errors, e => e.Contains("COBOL2776"));
        Assert.DoesNotContain(compiled.Errors, e => e.Contains("COBOL2363"));
        Assert.Single(compiled.Errors);
    }
}
