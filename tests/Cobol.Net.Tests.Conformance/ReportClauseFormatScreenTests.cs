// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ ONE SHAPE PER VERDICT for the report-writer / SIGN clause format screens (kb/Work PB483, PB537). The
/// negative goldens hold several violations each and so only prove that SOMETHING rejects; these tests compile
/// each shape ALONE, at every edition, so a screen that stops seeing one arm goes red here by name.
/// <list type="bullet">
/// <item>COBOLNET2421 — ISO §13.18.16.2 prints <c>FINAL [ data-name-1 ] …</c>: FINAL once, first (§5.2.7).</item>
/// <item>COBOLNET2423 — §13.14.2 / §13.18.39.2 print each RD clause and PAGE phrase in its own bracket with no
/// ellipsis (§5.2.6.2, §5.2.7); §13.14.3 SR2 and §13.18.39.3 SR4 license ORDER only.</item>
/// <item>COBOLNET2422 — §13.18.52.3 SR1 (the subject: a numeric entry with 'S', or an alphanumeric / national /
/// strongly-typed group) and SR2 (an elementary subject's usage is display or national).</item>
/// </list>
/// </summary>
public sealed class ReportClauseFormatScreenTests
{
    public static readonly TheoryData<int> Editions = new() { 85, 2002, 2014, 2023 };

    private static string Report(string id, string rdClauses, string detailColumn = "02 COLUMN 1 PIC 9 SOURCE WK.") => $"""
       IDENTIFICATION DIVISION.
       PROGRAM-ID. {id}.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "{id}.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 WK PIC 9 VALUE 1.
       01 WJ PIC 9 VALUE 2.
       01 WN PIC S9(3) VALUE -5.
       REPORT SECTION.
       RD R-1 {rdClauses}.
       01 DET-A TYPE DE LINE PLUS 1.
          {detailColumn}
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
""";

    private static string Data(string id, string entries) => $"""
       IDENTIFICATION DIVISION.
       PROGRAM-ID. {id}.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
{entries}
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
""";

    private static void Rejects(string src, int ed, string code)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(src, ed);
        Assert.False(ok, $"must be rejected at --std {ed}");
        EditionHarness.AssertHasDiagnostic(errors, code);
    }

    private static void Accepts(string src, int ed)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(src, ed);
        Assert.True(ok, $"must compile at --std {ed}; got:\n{string.Join("\n", errors)}");
    }

    [Theory, MemberData(nameof(Editions))]
    public void ControlFinal_Twice_2421(int ed) =>
        Rejects(Report("RCF1", "CONTROLS ARE FINAL FINAL WK PAGE LIMIT 30 LINES"), ed, "COBOLNET2421");

    [Theory, MemberData(nameof(Editions))]
    public void ControlFinal_AfterDataName_2421(int ed) =>
        Rejects(Report("RCF2", "CONTROLS ARE WK FINAL PAGE LIMIT 30 LINES"), ed, "COBOLNET2421");

    [Theory, MemberData(nameof(Editions))]
    public void ControlFinal_FirstThenDataNames_Accepted(int ed) =>
        Accepts(Report("RCF3", "PAGE LIMIT 30 LINES FOOTING 28 HEADING 1 CONTROLS ARE FINAL WK WJ"), ed);

    [Theory, MemberData(nameof(Editions))]
    public void ControlClause_Twice_2423(int ed) =>
        Rejects(Report("RCF4", "CONTROL WK CONTROL WJ PAGE LIMIT 30 LINES"), ed, "COBOLNET2423");

    [Theory, MemberData(nameof(Editions))]
    public void PageClause_Twice_2423(int ed) =>
        Rejects(Report("RCF5", "PAGE LIMIT 30 LINES PAGE LIMIT 40 LINES"), ed, "COBOLNET2423");

    [Theory]
    [InlineData("HEADING 1 HEADING 2")]
    [InlineData("FIRST DETAIL 3 FIRST DETAIL 4")]
    [InlineData("LAST DETAIL 20 LAST DETAIL 21")]
    [InlineData("FOOTING 25 FOOTING 26")]
    public void PagePhrase_Twice_2423(string phrases) =>
        Rejects(Report("RCF6", $"PAGE LIMIT 30 LINES {phrases}"), 2023, "COBOLNET2423");

    [Theory]
    [InlineData("01 A PIC 9(3) SIGN IS LEADING.")]
    [InlineData("01 A PIC X(3) SIGN IS LEADING.")]
    [InlineData("01 A PIC ZZ9 SIGN IS LEADING.")]
    [InlineData("01 A PIC S9(4) USAGE COMP SIGN IS LEADING SEPARATE.")]
    [InlineData("01 A PIC S9(4) USAGE COMP-3 SIGN IS TRAILING.")]
    [InlineData("01 A USAGE INDEX SIGN IS LEADING.")]
    [InlineData("01 G USAGE COMP.\n          05 A PIC S9(4) SIGN IS LEADING.")]
    public void SignClause_IllegalSubject_2422_AllEditions(string entries)
    {
        foreach (int ed in new[] { 85, 2002, 2014, 2023 })
            Rejects(Data("SGN1", "       " + entries), ed, "COBOLNET2422");
    }

    [Theory]
    [InlineData("01 A PIC N(3) SIGN IS TRAILING.")]
    [InlineData("01 A PIC S9(4) USAGE COMP-5 SIGN IS LEADING SEPARATE.")]
    [InlineData("01 A USAGE FLOAT-LONG SIGN IS LEADING.")]
    [InlineData("01 A PIC +9.9E+99 SIGN IS LEADING.")]
    [InlineData("01 G GROUP-USAGE IS BIT SIGN IS LEADING.\n          05 A PIC 1(4).")]
    [InlineData("01 G GROUP-USAGE BIT.\n          05 H SIGN LEADING.\n             10 A PIC 1(4).")]
    [InlineData("01 T TYPEDEF.\n          05 A PIC X SIGN IS LEADING.")]
    public void SignClause_IllegalSubject_2422_2023(string entries) =>
        Rejects(Data("SGN2", "       " + entries), 2023, "COBOLNET2422");

    [Fact]
    public void SignClause_ReportEntryWithoutS_2422() =>
        Rejects(Report("SGN3", "PAGE LIMIT 30 LINES", "02 COLUMN 1 PIC 9(3) SIGN LEADING SEPARATE SOURCE WN."),
            2023, "COBOLNET2422");

    [Theory]
    [InlineData("01 A PIC S9(4) SIGN IS LEADING SEPARATE VALUE -12.")]
    [InlineData("01 A PIC S9(4) USAGE NATIONAL SIGN IS TRAILING SEPARATE.")]
    [InlineData("01 G SIGN IS LEADING SEPARATE.\n          05 A PIC S9(3).\n          05 B PIC X.")]
    [InlineData("01 G GROUP-USAGE IS NATIONAL SIGN IS LEADING.\n          05 A PIC S9(3).")]
    [InlineData("01 G SIGN IS LEADING.\n          05 A PIC S9(3) COMP.")]
    [InlineData("01 G USAGE COMP SIGN IS LEADING.\n          05 A PIC S9(3).")]
    [InlineData("01 G SIGN IS LEADING SEPARATE.\n          05 A PIC X(3).\n          05 N PIC S9(3).\n       01 Z SAME AS A.")]
    [InlineData("01 T TYPEDEF STRONG SIGN IS LEADING SEPARATE.\n          05 A PIC S9(3).\n       01 R TYPE T.")]
    public void SignClause_LegalSubject_Accepted(string entries) =>
        Accepts(Data("SGN4", "       " + entries), 2023);

    [Fact]
    public void SignClause_ReportEntryWithS_Accepted() =>
        Accepts(Report("SGN5", "PAGE LIMIT 30 LINES", "02 COLUMN 1 PIC S9(3) SIGN LEADING SEPARATE SOURCE WN."), 85);
}
