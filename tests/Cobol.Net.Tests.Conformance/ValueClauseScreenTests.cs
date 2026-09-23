// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ THE VALUE-CLAUSE SYNTAX RULES THAT WERE SKIPPED PER FORMAT (kb/Work PB515, PB550, PB551, PB586) — one
/// family, four rules, each asked by the ONE screen its question belongs to, over every format the rule names:
/// <list type="bullet">
///   <item>§13.16.3 SR10 — "The VALUE clause shall not be specified for data items of class index, message-tag,
///     object, or pointer." USAGE INDEX was the class the set missed, written or inherited (COBOLNET2168).</item>
///   <item>§13.18.63.3 SR12 (format 1) and SR16 (format 2) — no VALUE in an entry that contains a REDEFINES
///     clause or is subordinate to one. The value was silently DISCARDED (COBOLNET2406).</item>
///   <item>§13.18.63.3 SR25 — no format 3 VALUE subordinate to a CONSTANT RECORD entry (COBOLNET2406).</item>
///   <item>§13.18.63.3 SR2/SR3 — ALL FORMATS: the range and sign of a numeric literal, which never reached a
///     level-88 (format 3) literal nor a report-section (format 4) one (COBOLNET1625).</item>
/// </list>
/// Every rejection has its OVER-REJECTION control beside it: each of these rules has a legal neighbour a careless
/// screen refuses (a level-88 under a REDEFINES entry, the redefined anchor's own VALUE, a CONSTANT RECORD's
/// format-1 content, the boundary literals of a range).
/// </summary>
public sealed class ValueClauseScreenTests
{
    private static string Prog(string id, string data, string body = "DISPLAY \"OK\"") => $$"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {{id}}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {{data}}
        PROCEDURE DIVISION.
        MAIN.
            {{body}}
            STOP RUN.
        """;

    private static readonly int[] Post85 = [2002, 2014, 2023];

    private static void Rejects(string source, string code, params int[] editions)
    {
        foreach (int ed in editions.Length == 0 ? EditionHarness.Editions : editions)
        {
            var (ok, errors, _) = EditionHarness.CompileFull(source, ed);
            Assert.False(ok, $"must be REJECTED at --std {ed}");
            EditionHarness.AssertHasDiagnostic(errors, code);
        }
    }

    private static void Accepts(string source, params int[] editions)
    {
        foreach (int ed in editions.Length == 0 ? EditionHarness.Editions : editions)
        {
            var (ok, errors, _) = EditionHarness.CompileFull(source, ed);
            Assert.True(ok, $"must COMPILE at --std {ed}; got:\n{string.Join("\n", errors)}");
        }
    }

    // ── §13.16.3 SR10 — class index (PB515) ─────────────────────────────────────────────────────────────────

    [Fact]
    public void IndexItem_WithValue_IsRejected_WrittenAndInherited()
    {
        Rejects(Prog("VCS515A", "77 I USAGE INDEX VALUE 7."), "COBOLNET2168");
        Rejects(Prog("VCS515B", "01 G.\n   05 I USAGE INDEX VALUE 1."), "COBOLNET2168");
        // §13.18.60.4 GR1: the group's USAGE clause IS the elementary item's — the same class, the same rule.
        Rejects(Prog("VCS515C", "01 G USAGE INDEX.\n   05 A VALUE 1.\n   05 B."), "COBOLNET2168");
    }

    [Fact]
    public void IndexItem_WithoutValue_Compiles() =>
        Accepts(Prog("VCS515D", "01 G USAGE INDEX.\n   05 A.\n   05 B.\n77 I USAGE INDEX."));

    // ── §13.18.63.3 SR12 / SR16 — REDEFINES (PB550) ─────────────────────────────────────────────────────────

    [Fact]
    public void Value_InOrUnderARedefiningEntry_IsRejected_AtEveryEdition()
    {
        Rejects(Prog("VCS550A", "01 R.\n   05 A PIC X(4) VALUE \"AAAA\".\n   05 B REDEFINES A PIC X(4) VALUE \"ZZZZ\"."),
            "COBOLNET2406");
        Rejects(Prog("VCS550B", "01 A PIC X(4) VALUE \"AAAA\".\n01 B REDEFINES A PIC X(4) VALUE \"ZZZZ\"."),
            "COBOLNET2406");
        Rejects(Prog("VCS550C", "01 R.\n   05 A PIC X(4).\n   05 B REDEFINES A.\n      10 C.\n         15 E PIC X(2) VALUE \"ZZ\".\n      10 D PIC X(2)."),
            "COBOLNET2406");
        // A GROUP-level VALUE on the redefining entry is the same clause in the same entry.
        Rejects(Prog("VCS550D", "01 R.\n   05 A PIC X(4).\n   05 B REDEFINES A VALUE \"ZZZZ\".\n      10 C PIC X(2).\n      10 D PIC X(2)."),
            "COBOLNET2406");
    }

    [Fact]
    public void Format2Value_UnderARedefiningEntry_IsRejected_BySr16()
    {
        Rejects(Prog("VCS550E", "01 R.\n   05 A PIC X(6).\n   05 B REDEFINES A PIC X(2) OCCURS 3 VALUE \"ZZ\" FROM (1) TO (3)."),
            "SR16", Post85);
        Rejects(Prog("VCS550F", "01 R.\n   05 A PIC X(6).\n   05 B REDEFINES A.\n      10 T PIC X(2) OCCURS 3 VALUE \"ZZ\" FROM (1) TO (3)."),
            "COBOLNET2406", Post85);
    }

    /// <summary>SR12 bars the redefinING entry and its subordinates — never the redefined anchor, never a level-88
    /// (SR24 imports SRs 10 and 17 into format 3, not SR12), never a REDEFINES entry with no VALUE, and never a
    /// later sibling that is not subordinate to the REDEFINES entry.</summary>
    [Fact]
    public void Sr12_Controls_Compile() =>
        Accepts(Prog("VCS550G",
            "01 R.\n   05 A PIC X(4) VALUE \"AAAA\".\n   05 B REDEFINES A PIC X(4).\n      88 BZ VALUE \"ZZZZ\".\n   05 C PIC X VALUE \"C\"."));

    // ── §13.18.63.3 SR25 — CONSTANT RECORD (PB551) ──────────────────────────────────────────────────────────

    [Fact]
    public void ConditionName_UnderAConstantRecord_IsRejected()
    {
        Rejects(Prog("VCS551A", "01 CR CONSTANT RECORD.\n   05 X PIC 9 VALUE 1.\n   88 F VALUE 1."), "COBOLNET2406", Post85);
        Rejects(Prog("VCS551B", "01 CR CONSTANT RECORD.\n   05 H.\n      10 X PIC 9 VALUE 1.\n         88 F VALUE 1."),
            "COBOLNET2406", Post85);
    }

    /// <summary>SR25 names formats 3, 4 and 5 only: the format-1 VALUEs that give a CONSTANT RECORD its content are
    /// what §13.18.15 is for.</summary>
    [Fact]
    public void ConstantRecord_Format1Content_Compiles() =>
        Accepts(Prog("VCS551C", "01 CR CONSTANT RECORD.\n   05 X PIC 9 VALUE 7.\n   05 Y PIC X(2) VALUE \"AB\"."), Post85);

    // ── §13.18.63.3 SR2 / SR3 — ALL FORMATS: format 3 (PB586) ───────────────────────────────────────────────

    [Fact]
    public void Level88Literal_OutsideTheConditionalVariablesRange_IsRejected()
    {
        Rejects(Prog("VCS586A", "01 W PIC 9(2).\n   88 C-BIG VALUE 12345."), "COBOLNET1625");
        Rejects(Prog("VCS586B", "01 W PIC 9(2).\n   88 C-R VALUE 1 THRU 12345."), "COBOLNET1625");
        Rejects(Prog("VCS586C", "01 W PIC 9(2).\n   88 C-N VALUE -1."), "SR3");
        Rejects(Prog("VCS586D", "01 W PIC 9(2)V9.\n   88 C-F VALUE 1.25."), "COBOLNET1625");
        Rejects(Prog("VCS586E", "01 W PIC 9(2).\n   88 C-T VALUE 1 WHEN SET TO FALSE 500."), "COBOLNET1625", Post85);
    }

    [Fact]
    public void Level88Literal_AtTheRangeBoundaries_Compiles() =>
        Accepts(Prog("VCS586F", "01 W PIC S9(2)V9.\n   88 C-ALL VALUE -99.9 THRU 99.9.\n   88 C-Z VALUE 0.\n01 U PIC 9(2).\n   88 U-TOP VALUE 99."));

    // ── §13.18.63.3 SR2 / SR3 / SR4 — ALL FORMATS: format 4, the report section (PB586's sibling) ────────────

    private static string Report(string id, string entries) => $$"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {{id}}.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT PRT ASSIGN TO "vcsrpt.txt".
        DATA DIVISION.
        FILE SECTION.
        FD  PRT REPORT IS RP.
        REPORT SECTION.
        RD  RP PAGE LIMIT IS 10 LINES.
        01  DET TYPE DE.
            02  LINE PLUS 1.
        {{entries}}
        PROCEDURE DIVISION.
        MAIN.
            OPEN OUTPUT PRT.
            INITIATE RP.
            GENERATE DET.
            TERMINATE RP.
            CLOSE PRT.
            STOP RUN.
        """;

    [Fact]
    public void ReportLiteral_OutsideThePrintableItemsRange_IsRejected()
    {
        Rejects(Report("VCSR1", "        03  COLUMN 1 PIC 9(2) VALUE 12345."), "COBOLNET1625");
        Rejects(Report("VCSR2", "        03  COLUMN 1 PIC 9(2) VALUE -3."), "SR3");
        // SR2's class half: before the funnel this leaked Roslyn CS0103 against the generated C#.
        Rejects(Report("VCSR3", "        03  COLUMN 1 PIC 9(2) VALUE \"AB\"."), "COBOLNET1657");
    }

    [Fact]
    public void ReportLiteral_InRange_Compiles() =>
        Accepts(Report("VCSR4",
            "        03  COLUMN 1 PIC 9(2) VALUE 99.\n        03  COLUMN 5 PIC S9(2) SIGN LEADING SEPARATE VALUE -3.\n        03  COLUMN 9 PIC X(3) VALUE \"XYZ\"."));
}
