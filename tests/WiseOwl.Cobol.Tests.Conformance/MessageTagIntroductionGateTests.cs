// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ USAGE MESSAGE-TAG ANSWERS TO ONE ROW PER EDITION (kb/Work PB916): below COBOL-2023 the COBOL0900 introduction
/// gate (<c>usage-message-tag-2023</c>) is the whole edition answer and the Annex A.3 decline (COBOL1943) names a
/// facility the edition does not have; at 2023 the decline stands. The conformance harness's refusal files check that
/// a code is REPORTED — they cannot say that another is NOT — and COBOL1943's own message promises "below --std 2023
/// the word is a user-defined word and the entry draws the ordinary introduction gate", so the absence is asserted
/// here, over BOTH spellings of the clause (§13.18.60.2 prints <c>[ USAGE IS ]</c> as optional) and over the usage a
/// group hands down (§13.18.60.4 GR1).
/// </summary>
public sealed class MessageTagIntroductionGateTests
{
    private const string Written = "01 M USAGE MESSAGE-TAG.";
    private const string Bare = "01 M MESSAGE-TAG.";
    private const string Group = "01 G USAGE MESSAGE-TAG.\n   05 M PIC X.";

    private static IReadOnlyList<string> ErrorsAt(string entry, int edition) =>
        EditionHarness.CompileFull($"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MTIG.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            {entry}
            PROCEDURE DIVISION.
            MAIN-PARA.
                STOP RUN.
            """, edition).Errors;

    [Theory]
    [InlineData(Written, 85)]
    [InlineData(Written, 2002)]
    [InlineData(Written, 2014)]
    [InlineData(Bare, 85)]
    [InlineData(Bare, 2002)]
    [InlineData(Bare, 2014)]
    [InlineData(Group, 85)]
    [InlineData(Group, 2002)]
    [InlineData(Group, 2014)]
    public void BelowTheIntroduction_TheEntryDrawsTheIntroductionGateAndNotTheDecline(string entry, int edition)
    {
        var errors = ErrorsAt(entry, edition);
        EditionHarness.AssertHasDiagnostic(errors, "COBOL0900");
        EditionHarness.AssertHasDiagnostic(errors, "USAGE MESSAGE-TAG requires COBOL-2023");
        EditionHarness.AssertHasDiagnostic(errors, $"targeting COBOL-{edition}");
        EditionHarness.AssertNoDiagnostic(errors, "COBOL1943");
    }

    [Theory]
    [InlineData(Written)]
    [InlineData(Bare)]
    [InlineData(Group)]
    public void AtTheIntroduction_TheEntryIsDeclinedAndNeedsNoIntroductionGate(string entry)
    {
        var errors = ErrorsAt(entry, 2023);
        EditionHarness.AssertHasDiagnostic(errors, "COBOL1943");
        EditionHarness.AssertNoDiagnostic(errors, "COBOL0900");
    }

    /// <summary>The STATEMENT half (SET Format 17, COBOL2205) follows its operand's usage: it is declined at 2023 and,
    /// below it, the operands' own COBOL0900 is the answer — a Format-17 decline names the 2023 facility.</summary>
    [Theory]
    [InlineData(2002, false)]
    [InlineData(2014, false)]
    [InlineData(2023, true)]
    public void SetFormat17_IsDeclinedOnlyWhereTheUsageExists(int edition, bool declined)
    {
        var errors = EditionHarness.CompileFull("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MTIS.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 MT1 USAGE MESSAGE-TAG.
            01 MT2 USAGE MESSAGE-TAG.
            PROCEDURE DIVISION.
            MAIN-P.
                SET MT1 TO MT2.
                STOP RUN.
            """, edition).Errors;
        EditionHarness.AssertHasDiagnostic(errors, declined ? "COBOL2205" : "COBOL0900");
        if (!declined) EditionHarness.AssertNoDiagnostic(errors, "COBOL2205");
        else EditionHarness.AssertNoDiagnostic(errors, "COBOL0900");
    }

    /// <summary>The note's arm 2 from the other side: a group's message-tag usage handed to a leaf that wrote a
    /// PICTURE is refused as a MESSAGE-TAG usage at every edition, and never as a floating-point one.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void AnInheritedMessageTagUsageWithAPicture_IsNeverReportedAsFloatingPoint(int edition)
    {
        var errors = ErrorsAt(Group, edition);
        EditionHarness.AssertHasDiagnostic(errors, "PICTURE may not be specified with USAGE MESSAGE-TAG");
        EditionHarness.AssertNoDiagnostic(errors, "floating-point usage");
        EditionHarness.AssertNoDiagnostic(errors, "COBOL1521");
    }
}
