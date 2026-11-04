// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ ONE VIOLATION, ONE DIAGNOSTIC (kb/Work PB4602). ISO §13.16.3 SR14 (TYPE) and SR12 (SAME AS) refuse a
/// co-clause, and the recovery DROPS that co-clause and KEEPS the reference
/// (<c>DataBinder.ScreenReferenceComposition</c>). The former recovery cleared the reference, so the entry reached the
/// later screens as a PICTURE-less leaf and <c>01 X TYPE T ALIGNED.</c> drew COBOL1942 and COBOL0881 beside the one
/// real refusal. The conformance-suite refusal goldens pin the code by substring; this class pins the COUNT, which
/// is what a cascade changes.
/// </summary>
public sealed class ReferenceCompositionRecoveryTests
{
    private const string Prologue = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. RCR.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 TG TYPEDEF STRONG.
           05 TGA PIC X.
        01 TP TYPEDEF PIC X(3).
        01 Z.
           05 ZA PIC X.
        01 Z1 PIC X(3).
        """;

    private const string Epilogue = """
        PROCEDURE DIVISION.
            STOP RUN.
        """;

    private static IReadOnlyList<string> Errors(string entry) =>
        EditionHarness.Compile($"{Prologue}\n{entry}\n{Epilogue}", 2023).Diagnostics;

    /// <summary>§13.16.3 SR14: every co-clause outside the permitted set draws COBOL2150 and nothing else.</summary>
    [Theory]
    [InlineData("01 X TYPE TG ALIGNED.")]
    [InlineData("01 X TYPE TG PIC 9(5).")]
    [InlineData("01 X TYPE TP PIC 9(5).")]
    [InlineData("01 X TYPE TG USAGE COMP.")]
    [InlineData("01 X TYPE TP USAGE COMP.")]
    [InlineData("01 X TYPE TP JUSTIFIED RIGHT.")]
    [InlineData("01 X TYPE TP BLANK WHEN ZERO.")]
    [InlineData("01 X TYPE TP SYNCHRONIZED.")]
    [InlineData("01 X TYPE TP SIGN IS LEADING SEPARATE.")]
    [InlineData("01 X TYPE TG GROUP-USAGE NATIONAL.")]
    [InlineData("01 X TYPE TP ALIGNED PIC 9 USAGE COMP.")]
    public void TypeWithForbiddenCoClause_DrawsOnlyCobol2150(string entry)
    {
        var errors = Errors(entry);
        Assert.Single(errors);
        EditionHarness.AssertHasDiagnostic(errors, "COBOL2150");
    }

    /// <summary>§13.16.3 SR12: every co-clause outside the permitted set draws COBOL1555 and nothing else.</summary>
    [Theory]
    [InlineData("01 X SAME AS Z ALIGNED.")]
    [InlineData("01 X SAME AS Z1 PIC 9(5).")]
    [InlineData("01 X SAME AS Z1 USAGE COMP.")]
    [InlineData("01 X SAME AS Z1 JUSTIFIED RIGHT.")]
    [InlineData("01 X SAME AS Z1 BLANK WHEN ZERO.")]
    [InlineData("01 X SAME AS Z1 SYNCHRONIZED.")]
    public void SameAsWithForbiddenCoClause_DrawsOnlyCobol1555(string entry)
    {
        var errors = Errors(entry);
        Assert.Single(errors);
        EditionHarness.AssertHasDiagnostic(errors, "COBOL1555");
    }

    /// <summary>One entry writing BOTH clauses violates both rules, so both are reported; the recovery keeps SAME AS
    /// (SR12 refuses TYPE, SR14 does not refuse SAME AS) so the entry still has a description and nothing else follows.</summary>
    [Fact]
    public void TypeAndSameAsTogether_DrawBothRefusalsOnly()
    {
        var errors = Errors("01 X TYPE TP SAME AS Z1.");
        Assert.Equal(2, errors.Count);
        EditionHarness.AssertHasDiagnostic(errors, "COBOL1555");
        EditionHarness.AssertHasDiagnostic(errors, "COBOL2150");
    }

    /// <summary>⛔ The recovery drops the DESCRIPTION clauses and KEEPS the ROLE clauses (review R1-1, kb/Work PB4602):
    /// SR12 refuses TYPEDEF and BASED beside SAME AS and reports it once, but the entry stays a type declaration / a
    /// based template, so references to it draw nothing further.</summary>
    [Fact]
    public void SameAsWithTypedef_ReferencesToTheTypeDrawNothingFurther()
    {
        var (_, errors) = EditionHarness.Compile($"{Prologue}\n01 X TYPEDEF SAME AS Z1.\n01 Y TYPE X.\n01 Y2 TYPE X.\n01 Y3 TYPE X.\n{Epilogue}", 2023);
        Assert.Single(errors);
        EditionHarness.AssertHasDiagnostic(errors, "COBOL1555");
    }

    [Fact]
    public void SameAsWithBased_ReferencesToTheTemplateDrawNothingFurther()
    {
        const string proc = """
            PROCEDURE DIVISION.
                ALLOCATE X.
                SET ADDRESS OF X TO PTR.
                STOP RUN.
            """;
        var (_, errors) = EditionHarness.Compile($"{Prologue}\n01 PTR USAGE POINTER.\n01 X BASED SAME AS Z1.\n{proc}", 2023);
        Assert.Single(errors);
        EditionHarness.AssertHasDiagnostic(errors, "COBOL1555");
    }

    /// <summary>The permitted co-clauses are untouched by the recovery: TYPE with VALUE and SAME AS with OCCURS at a
    /// level that admits it compile clean.</summary>
    [Fact]
    public void PermittedCoClauses_CompileClean()
    {
        var (ok, errors) = EditionHarness.Compile($"{Prologue}\n01 G.\n   05 X TYPE TP VALUE \"AB\".\n   05 Y SAME AS Z1 OCCURS 2.\n{Epilogue}", 2023);
        Assert.True(ok, string.Join("\n", errors));
    }
}
