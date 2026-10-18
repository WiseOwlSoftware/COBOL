// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB1629 — a fault of PLACEMENT born at a TYPE or SAME AS reference is positioned at the entry that wrote
/// the clause, never at the declaration the description is copied from. ISO §13.18.57.4 GR1 and §13.18.49.4 GR1 make
/// the clause's effect "as though" the description "had been coded in place", so the rule broken (§8.5.1.9.1 3): a
/// dynamic-capacity table "may be defined in any place, other than the file section"; §13.18.38.3 SR1(b)/SR10/SR17/
/// SR20/SR22, §13.18.44.3 SR5 and SR10's seven-subscript limit for an OCCURS DEPENDING ON table or a nest of tables)
/// is the reference's, and the declaration, legal on its own, is not where the user edits. The codes are pinned by
/// <see cref="OccursDynamicGuardTests"/> and the conformance suite; this class pins WHERE they are reported, for
/// every route a description reaches a record by (TYPE on a record, TYPE on a group, SAME AS on a record, SAME AS on a
/// group) and for an entry written in place, which stays at itself.
/// </summary>
public sealed class ReferenceSitePositionTests
{
    private static int LineOf(string[] lines, string marker)
    {
        int at = Array.FindIndex(lines, l => l.Contains(marker, StringComparison.Ordinal));
        Assert.True(at >= 0, $"the test source holds no line with '{marker}'");
        return at + 1;   // diagnostics are 1-based
    }

    /// <summary>Compile fixed-form <paramref name="source"/> and require exactly the diagnostics of
    /// <paramref name="code"/> to be positioned on the line holding <paramref name="siteMarker"/> and none on the line
    /// holding <paramref name="declarationMarker"/> (null when the entry is written in place).</summary>
    private static void AssertReportedAt(string source, int edition, string code, string siteMarker,
        string? declarationMarker)
    {
        var lines = source.Split('\n');
        int site = LineOf(lines, siteMarker);
        var (ok, errors) = EditionHarness.Compile(source, edition);
        Assert.False(ok, $"{code} must refuse the program");
        var mine = errors.Where(e => e.Contains(code, StringComparison.Ordinal)).ToList();
        Assert.True(mine.Count > 0, $"expected {code}; got:\n{string.Join("\n", errors)}");
        Assert.All(mine, e => Assert.True(e.Contains($"({site},", StringComparison.Ordinal),
            $"{code} must be positioned at line {site} ('{siteMarker.Trim()}'); got: {e}"));
        if (declarationMarker is not null)
        {
            int declaration = LineOf(lines, declarationMarker);
            Assert.DoesNotContain(mine, e => e.Contains($"({declaration},", StringComparison.Ordinal));
        }
    }

    private const string Head =
        "       IDENTIFICATION DIVISION.\n" +
        "       PROGRAM-ID. {0}.\n" +
        "       ENVIRONMENT DIVISION.\n" +
        "       INPUT-OUTPUT SECTION.\n" +
        "       FILE-CONTROL.\n" +
        "           SELECT F ASSIGN TO \"{0}.dat\" ORGANIZATION IS SEQUENTIAL.\n" +
        "           SELECT S ASSIGN TO \"{0}.srt\".\n" +
        "       DATA DIVISION.\n" +
        "       FILE SECTION.\n";

    private const string FileTail =
        "       WORKING-STORAGE SECTION.\n" +
        "       01 TT TYPEDEF.\n" +
        "          05 TDECL OCCURS DYNAMIC FROM 2 TO 5.\n" +
        "             10 E PIC X(3).\n" +
        "       01 TT-W.\n" +
        "          05 TWDECL OCCURS DYNAMIC FROM 2 TO 5.\n" +
        "             10 E2 PIC X(3).\n" +
        "       PROCEDURE DIVISION.\n" +
        "       MAIN-PARA.\n" +
        "           STOP RUN.\n";

    /// <summary>§8.5.1.9.1 3): COBOLNET1526 at the entry that put the table in the file section, on each route.</summary>
    [Theory]
    [InlineData("PB1629F1", "       FD F.\n       01 R-TYPE TYPE TT.\n       SD S.\n       01 SR PIC X.\n", "R-TYPE TYPE TT", "TDECL OCCURS")]
    [InlineData("PB1629F2", "       FD F.\n       01 R.\n          05 A PIC X(2).\n          05 G-TYPE TYPE TT.\n       SD S.\n       01 SR PIC X.\n", "G-TYPE TYPE TT", "TDECL OCCURS")]
    [InlineData("PB1629F3", "       FD F.\n       01 R-SAME SAME AS TT-W.\n       SD S.\n       01 SR PIC X.\n", "R-SAME SAME AS", "TWDECL OCCURS")]
    [InlineData("PB1629F4", "       FD F.\n       01 R.\n          05 A PIC X(2).\n          05 G-SAME SAME AS TT-W.\n       SD S.\n       01 SR PIC X.\n", "G-SAME SAME AS", "TWDECL OCCURS")]
    [InlineData("PB1629F5", "       FD F.\n       01 R PIC X.\n       SD S.\n       01 SR-TYPE TYPE TT.\n", "SR-TYPE TYPE TT", "TDECL OCCURS")]
    [InlineData("PB1629F6", "       FD F.\n       01 R PIC X.\n       SD S.\n       01 SR.\n          05 A PIC X(2).\n          05 G-SAME SAME AS TT-W.\n", "G-SAME SAME AS", "TWDECL OCCURS")]
    public void FileSectionDynamicTable_IsReportedAtTheEntryThatWroteTheClause(string id, string fileSection,
        string siteMarker, string declarationMarker) =>
        AssertReportedAt(string.Format(Head, id) + fileSection + FileTail, 2023, "COBOLNET1526", siteMarker,
            declarationMarker);

    /// <summary>An entry WRITTEN in the file section is its own site: the position the diagnostic always had.</summary>
    [Fact]
    public void FileSectionDynamicTable_WrittenInPlace_StaysAtItsOwnEntry() =>
        AssertReportedAt(string.Format(Head, "PB1629F7")
            + "       FD F.\n       01 R.\n          05 TIP OCCURS DYNAMIC FROM 2 TO 5.\n             10 E PIC X(3).\n"
            + FileTail, 2023, "COBOLNET1526", "TIP OCCURS", null);

    /// <summary>The message names the entry the user must change when the table is a member the clause copied in.</summary>
    [Fact]
    public void FileSectionDynamicTable_ViaTypeClause_NamesTheSubject()
    {
        var (ok, errors) = EditionHarness.Compile(string.Format(Head, "PB1629F8")
            + "       FD F.\n       01 R-NAMED TYPE TT.\n" + FileTail, 2023);
        Assert.False(ok);
        Assert.Contains(errors, e => e.Contains("COBOLNET1526", StringComparison.Ordinal)
            && e.Contains("'R-NAMED' copies in through its TYPE or SAME AS clause", StringComparison.Ordinal));
    }

    private const string WsHead =
        "       IDENTIFICATION DIVISION.\n" +
        "       PROGRAM-ID. {0}.\n" +
        "       DATA DIVISION.\n" +
        "       WORKING-STORAGE SECTION.\n";

    private const string WsTail =
        "       PROCEDURE DIVISION.\n" +
        "       MAIN-PARA.\n" +
        "           STOP RUN.\n";

    private const string TypeWithCounter =   // a counter of its own: legal as a declaration
        "       01 TTC TYPEDEF.\n" +
        "          05 NC PIC 9.\n" +
        "          05 TC OCCURS 1 TO 5 DEPENDING ON NC.\n" +
        "             10 EC PIC X.\n";

    private const string TypeNeedingCounter =   // the counter is the use site's: legal as a declaration
        "       01 TTN TYPEDEF.\n" +
        "          05 TN OCCURS 1 TO 5 DEPENDING ON CNT.\n" +
        "             10 EN PIC X.\n";

    /// <summary>§13.18.38.3 SR22, SR1(b)/SR10 + SR2, §13.18.44.3 SR5, SR20 and SR17 of the OCCURS DEPENDING ON rules
    /// that ask what SURROUNDS the table, and SR2's counter lookup: each is reported at the clause's subject.</summary>
    [Theory]
    // SR22: the table is followed by a non-subordinate entry in the record the copy joins.
    [InlineData("PB1629W1", "COBOLNET0856", TypeWithCounter + "       01 R.\n          05 GA TYPE TTC.\n          05 XA PIC X.\n", "GA TYPE TTC", "TC OCCURS")]
    [InlineData("PB1629W2", "COBOLNET0856", "       01 SRC.\n          05 NS PIC 9.\n          05 TS OCCURS 1 TO 5 DEPENDING ON NS.\n             10 ES PIC X.\n       01 R.\n          05 GB SAME AS SRC.\n          05 XB PIC X.\n", "GB SAME AS SRC", "TS OCCURS")]
    // SR1(b)/SR10: the copy joins an OCCURS item (and the counter is then subscripted, SR2).
    [InlineData("PB1629W3", "COBOLNET0854", TypeWithCounter + "       01 R.\n          05 OC OCCURS 2.\n             10 GC TYPE TTC.\n", "GC TYPE TTC", "TC OCCURS")]
    [InlineData("PB1629W4", "COBOLNET0853", TypeWithCounter + "       01 R.\n          05 OD OCCURS 2.\n             10 GD TYPE TTC.\n", "GD TYPE TTC", "TC OCCURS")]
    // §13.18.44.3 SR5: the copy lies within a REDEFINES area.
    [InlineData("PB1629W5", "COBOLNET0855", TypeWithCounter + "       01 R.\n          05 GE TYPE TTC.\n          05 HE REDEFINES GE.\n             10 HX PIC X(3).\n", "GE TYPE TTC", "TC OCCURS")]
    // DEPENDING ON data-name-1 is looked up where the copy is PLACED: nothing there defines it.
    [InlineData("PB1629W6", "COBOLNET0851", TypeNeedingCounter + "       01 R.\n          05 GF TYPE TTN.\n", "GF TYPE TTN", "TN OCCURS")]
    // SR17: the placement supplies a counter that is not an integer.
    [InlineData("PB1629W7", "COBOLNET0852", TypeNeedingCounter + "       01 CNT PIC X.\n       01 R.\n          05 GG TYPE TTN.\n", "GG TYPE TTN", "TN OCCURS")]
    // SR20: the placement supplies a counter inside the table's range.
    [InlineData("PB1629W8", "COBOLNET0857", TypeNeedingCounter + "       01 R.\n          05 GH TYPE TTN.\n          05 CNT PIC 9.\n", "GH TYPE TTN", "TN OCCURS")]
    public void OccursDependingOnPlacementFault_IsReportedAtTheEntryThatWroteTheClause(string id, string code,
        string data, string siteMarker, string declarationMarker) =>
        AssertReportedAt(string.Format(WsHead, id) + data + WsTail, 2023, code, siteMarker, declarationMarker);

    /// <summary>A table written in place stays reported at itself, so the anchoring changed only the copies.</summary>
    [Fact]
    public void OccursDependingOnPlacementFault_WrittenInPlace_StaysAtItsOwnEntry() =>
        AssertReportedAt(string.Format(WsHead, "PB1629W9")
            + "       01 R.\n          05 NW PIC 9.\n          05 TW OCCURS 1 TO 5 DEPENDING ON NW.\n"
            + "             10 EW PIC X.\n          05 XW PIC X.\n" + WsTail, 2023, "COBOLNET0856", "TW OCCURS", null);

    /// <summary>§13.18.63.3 SR14 (COBOLNET1702) reads the same anchor: the JUSTIFIED member is legal in the type
    /// declaration and wrong under the VALUE the reference adds, so the refusal is at the TYPE clause's subject.</summary>
    [Fact]
    public void GroupValueOverACopiedJustifiedMember_IsReportedAtTheEntryThatWroteTheClause() =>
        AssertReportedAt(string.Format(WsHead, "PB1629V1")
            + "       01 TJ TYPEDEF.\n          05 JM PIC X(2) JUSTIFIED RIGHT.\n       01 R-GV TYPE TJ VALUE \"AB\".\n"
            + WsTail, 2023, "COBOLNET1702", "R-GV TYPE TJ", "JM PIC X(2)");

    /// <summary>§13.18.38.3 SR10: the copy of a one-table type under seven tables is the eighth dimension (a refusal
    /// the written-entries-only walk never made), reported at the clause's subject and once.</summary>
    [Theory]
    [InlineData("PB1629N1", "       01 TT8 TYPEDEF.\n          02 T8 PIC X OCCURS 2.\n       01 R.\n          02 A1 OCCURS 2.\n           03 A2 OCCURS 2.\n            04 A3 OCCURS 2.\n             05 A4 OCCURS 2.\n              06 A5 OCCURS 2.\n               07 A6 OCCURS 2.\n                08 A7 OCCURS 2.\n                 09 G8 TYPE TT8.\n", "G8 TYPE TT8", "T8 PIC X OCCURS")]
    [InlineData("PB1629N2", "       01 SRC8.\n          02 S8 PIC X OCCURS 2.\n       01 R.\n          02 A1 OCCURS 2.\n           03 A2 OCCURS 2.\n            04 A3 OCCURS 2.\n             05 A4 OCCURS 2.\n              06 A5 OCCURS 2.\n               07 A6 OCCURS 2.\n                08 A7 OCCURS 2.\n                 09 G9 SAME AS SRC8.\n", "G9 SAME AS SRC8", "S8 PIC X OCCURS")]
    public void TableNestingDepth_OfACopy_IsReportedAtTheEntryThatWroteTheClause(string id, string data,
        string siteMarker, string declarationMarker)
    {
        string source = string.Format(WsHead, id) + data + WsTail;
        AssertReportedAt(source, 2023, "COBOLNET2646", siteMarker, declarationMarker);
        var (_, errors) = EditionHarness.Compile(source, 2023);
        Assert.Single(errors, e => e.Contains("COBOLNET2646", StringComparison.Ordinal));
    }

    /// <summary>A type declaration that itself nests eight deep is reported once, in the declaration, and its copy is
    /// not reported again (the declaration is where the user edits).</summary>
    [Fact]
    public void TableNestingDepth_OfADeclaration_IsReportedOnceInTheDeclaration()
    {
        string source = string.Format(WsHead, "PB1629N3")
            + "       01 TT9 TYPEDEF.\n          02 A1 OCCURS 2.\n           03 A2 OCCURS 2.\n            04 A3 OCCURS 2.\n"
            + "             05 A4 OCCURS 2.\n              06 A5 OCCURS 2.\n               07 A6 OCCURS 2.\n"
            + "                08 A7 OCCURS 2.\n                 09 A8 PIC X OCCURS 2.\n"
            + "       01 R9 TYPE TT9.\n" + WsTail;
        AssertReportedAt(source, 2023, "COBOLNET2646", "A8 PIC X OCCURS", "R9 TYPE TT9");
        var (_, errors) = EditionHarness.Compile(source, 2023);
        Assert.Single(errors, e => e.Contains("COBOLNET2646", StringComparison.Ordinal));
    }
}
