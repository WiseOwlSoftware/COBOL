// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB1154 — every syntax rule of the exception-checking (Format-3) PERFORM is reported AT THE OFFENDING
/// STATEMENT or WHEN operand, never at the PERFORM that contains it. The bans (COBOL1604-1617) run after
/// imperative-statement-1 has bound, when the diagnostic cursor is back at the PERFORM, so before the one reporter
/// (<c>EcBinder.Ban</c>) every one of them printed the PERFORM's line and column: a PERFORM with a multi-file CLOSE on
/// line 40 reported "line 12". Each ban is asserted here at the line and column of its statement, with its legal
/// twin (the single-operand statement, or the statement outside the region the rule bans) compiling clean, because a
/// position stamped on the wrong statement and a ban that no longer fires are failures the success path cannot see.
/// </summary>
public sealed class ExceptionPerformBanPositionTests
{
    private const string Files =
        "IDENTIFICATION DIVISION.\nPROGRAM-ID. PB1154P.\nENVIRONMENT DIVISION.\nINPUT-OUTPUT SECTION.\nFILE-CONTROL.\n" +
        "  SELECT F1 ASSIGN \"pb1154p1.dat\" ORGANIZATION LINE SEQUENTIAL.\n" +
        "  SELECT F2 ASSIGN \"pb1154p2.dat\" ORGANIZATION LINE SEQUENTIAL.\n" +
        "  SELECT F3 ASSIGN \"pb1154p3.dat\" ORGANIZATION LINE SEQUENTIAL.\n" +
        "  SELECT SF ASSIGN \"pb1154ps.tmp\".\n" +
        "DATA DIVISION.\nFILE SECTION.\nFD F1.\n01 R1 PIC X(4).\nFD F2.\n01 R2 PIC X(4).\nFD F3.\n01 R3 PIC X(4).\n" +
        "SD SF.\n01 SR.\n   03 SK PIC X(4).\n" +
        "WORKING-STORAGE SECTION.\n01 N PIC 9 VALUE 0.\n01 M PIC 9 VALUE 0.\nPROCEDURE DIVISION.\nMAIN.\n";

    private const string Reports =
        "IDENTIFICATION DIVISION.\nPROGRAM-ID. PB1154R.\nENVIRONMENT DIVISION.\nINPUT-OUTPUT SECTION.\nFILE-CONTROL.\n" +
        "  SELECT RPTA ASSIGN \"pb1154ra.rpt\".\n  SELECT RPTB ASSIGN \"pb1154rb.rpt\".\n" +
        "DATA DIVISION.\nFILE SECTION.\nFD RPTA REPORT IS R-A.\nFD RPTB REPORT IS R-B.\nREPORT SECTION.\n" +
        "RD R-A.\n01 DA TYPE DE LINE PLUS 1.\n   02 COLUMN 1 PIC X(2) VALUE \"DA\".\n" +
        "RD R-B.\n01 DB TYPE DE LINE PLUS 1.\n   02 COLUMN 1 PIC X(2) VALUE \"DB\".\n" +
        "PROCEDURE DIVISION.\nMAIN.\n";

    private static string Perform(string imp1, string whens = "WHEN EC-SIZE DISPLAY \"x\"") =>
        $"    PERFORM\n        {imp1}\n    {whens}\n    END-PERFORM.\n    STOP RUN.\n";

    /// <summary>The 1-based line and column of the first occurrence of <paramref name="needle"/>, as the compiler
    /// prints them.</summary>
    private static (int Line, int Col) Where(string source, string needle)
    {
        int at = source.IndexOf(needle, StringComparison.Ordinal);
        Assert.True(at >= 0, $"'{needle}' is not in the program");
        int line = source.Take(at).Count(c => c == '\n') + 1;
        int col = at - (source.LastIndexOf('\n', at) + 1) + 1;
        return (line, col);
    }

    /// <summary>The (line, column) of every diagnostic carrying <paramref name="code"/>.</summary>
    private static List<(int Line, int Col)> Positions(IEnumerable<string> diagnostics, string code) =>
        diagnostics.Where(d => d.Contains($"error {code}:", StringComparison.Ordinal))
            .Select(d => Regex.Match(d, @"\((\d+),(\d+)\): error"))
            .Where(m => m.Success)
            .Select(m => (int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value))).ToList();

    private static void AssertAt(string source, string code, string needle)
    {
        var (ok, diag) = EditionHarness.Compile(source, 2023);
        Assert.False(ok, $"expected {code}; the program compiled");
        var at = Positions(diag, code);
        Assert.True(at.Count == 1, $"expected exactly one {code}; got [{string.Join(" | ", diag)}]");
        Assert.Equal(Where(source, needle), at[0]);
    }

    /// <summary>The legal twin compiles clean, so a rule that over-fires is a failure too.</summary>
    private static void AssertCompiles(string source)
    {
        var (ok, diag) = EditionHarness.Compile(source, 2023);
        Assert.True(ok, $"the legal twin did not compile: [{string.Join(" | ", diag)}]");
    }

    // ── region A / B bans: at the offending statement inside imperative-statement-1 ──────────────────────────

    [Theory]
    [InlineData("COBOL1607", "VALIDATE N M")]
    [InlineData("COBOL1612", "CLOSE F1 F2")]
    [InlineData("COBOL1613", "DELETE FILE F1 F2")]
    [InlineData("COBOL1614", "INITIALIZE N M")]
    [InlineData("COBOL1616", "OPEN OUTPUT F1 F2")]
    [InlineData("COBOL1617", "SORT SF ASCENDING KEY SK USING F1 GIVING F2")]
    [InlineData("COBOL1615", "MERGE SF ASCENDING KEY SK USING F1 F2 GIVING F3")]
    public void Imp1Ban_IsReportedAtTheOffendingStatement(string code, string statement) =>
        AssertAt(Files + Perform("ADD 1 TO N\n        " + statement), code, statement);

    [Fact]   // §14.9.21.3 SR3 — the INITIATE is NOT the PERFORM's first statement
    public void InitiateMulti_IsReportedAtTheInitiate() => AssertAt(
        Reports + Perform("TERMINATE R-A\n        INITIATE R-A R-B", "WHEN EC-REPORT DISPLAY \"x\""),
        "COBOL1605", "INITIATE R-A R-B");

    [Fact]   // §14.9.46.3 SR3
    public void TerminateMulti_IsReportedAtTheTerminate() => AssertAt(
        Reports + Perform("INITIATE R-A\n        TERMINATE R-A R-B", "WHEN EC-REPORT DISPLAY \"x\""),
        "COBOL1606", "TERMINATE R-A R-B");

    // ── region C / D bans: at the offending statement inside a WHEN or FINALLY phrase ────────────────────────

    [Fact]   // §14.9.17.3 SR3 — region C includes WHEN OTHER
    public void GoToInWhenOther_IsReportedAtTheGoTo() => AssertAt(
        Files + "    PERFORM\n        ADD 1 TO N\n    WHEN EC-SIZE DISPLAY \"s\"\n    WHEN OTHER\n        GO TO SKIP\n"
        + "    END-PERFORM.\n    STOP RUN.\nSKIP.\n    CONTINUE.\n", "COBOL1608", "GO TO SKIP");

    [Fact]   // §14.9.29.3 SR4 — region D includes FINALLY
    public void RaiseInFinally_IsReportedAtTheRaise() => AssertAt(
        Files + "    PERFORM\n        ADD 1 TO N\n    WHEN EC-SIZE DISPLAY \"s\"\n    FINALLY\n"
        + "        RAISE EXCEPTION EC-SIZE\n    END-PERFORM.\n    STOP RUN.\n", "COBOL1611", "RAISE EXCEPTION EC-SIZE");

    [Fact]   // §14.9.14.3 SR8 sentence 2 and §14.9.33.3 SR1 are bound at their own statements by the verb binders
    public void ExitPerformCycle_AndResumeAt_AreReportedAtTheirStatements()
    {
        AssertAt(Files + Perform("ADD 1 TO N\n        EXIT PERFORM CYCLE"), "COBOL1604", "EXIT PERFORM CYCLE");
        AssertAt(Files + "    PERFORM\n        ADD 1 TO N\n    WHEN EC-SIZE\n        RESUME AT SKIP\n    END-PERFORM.\n"
            + "    STOP RUN.\nSKIP.\n    CONTINUE.\n", "COBOL1610", "RESUME AT SKIP");
    }

    [Fact]   // §14.9.28.4 GR16 reports the FINALLY transfer at its statement, at the same column the others use
    public void FinallyTransferOut_IsReportedAtTheStatement_NotOneColumnLate() => AssertAt(
        Files + "    PERFORM\n        ADD 1 TO N\n    WHEN EC-SIZE DISPLAY \"s\"\n    FINALLY\n        GO TO SKIP\n"
        + "    END-PERFORM.\n    STOP RUN.\nSKIP.\n    CONTINUE.\n", "COBOL2927", "GO TO SKIP");

    // ── the WHEN-operand rules: at the operand, not the PERFORM ──────────────────────────────────────────────

    [Fact]   // §14.9.28.3 SR15 — the REPEAT is the violation
    public void Sr15ExceptionNameRepeat_IsReportedAtTheSecondInstance()
    {
        string src = Files + "    PERFORM\n        ADD 1 TO N\n    WHEN EC-SIZE DISPLAY \"a\"\n    WHEN EC-SIZE DISPLAY \"b\"\n"
            + "    END-PERFORM.\n    STOP RUN.\n";
        var (ok, diag) = EditionHarness.Compile(src, 2023);
        Assert.False(ok);
        var at = Positions(diag, "COBOL1600");
        Assert.Single(at);
        Assert.Equal(Where(src, "WHEN EC-SIZE DISPLAY \"b\"").Line, at[0].Line);
    }

    [Fact]   // §14.9.28.3 SR14 — a bare file-name repeated in a second WHEN phrase
    public void Sr14FileNameRepeat_IsReportedAtTheSecondInstance()
    {
        string src = Files + "    PERFORM\n        ADD 1 TO N\n    WHEN EXCEPTION F1 DISPLAY \"a\"\n    WHEN EXCEPTION F1 DISPLAY \"b\"\n"
            + "    END-PERFORM.\n    STOP RUN.\n";
        var (ok, diag) = EditionHarness.Compile(src, 2023);
        Assert.False(ok);
        var at = Positions(diag, "COBOL1599");
        Assert.Single(at);
        Assert.Equal(Where(src, "WHEN EXCEPTION F1 DISPLAY \"b\"").Line, at[0].Line);
    }

    // With three instances "the second" is not the offender: the operand that FIRST breaks the rule is (review R1-1).
    // Each case asserts line AND column, and its legal twin (the third instance changed so the rule holds) compiles.

    [Fact]   // §14.9.28.3 SR15 — F1, F2 are distinct pairings; the second F1 is the repeat
    public void Sr15ExceptionNameRepeat_WithThreeInstances_IsReportedAtTheInstanceThatBreaksTheRule()
    {
        const string head = "    PERFORM\n        ADD 1 TO N\n    WHEN EC-I-O FILE F1 DISPLAY \"a\"\n"
            + "    WHEN EC-I-O FILE F2 DISPLAY \"b\"\n";
        AssertAt(Files + head + "    WHEN EC-I-O FILE F1 DISPLAY \"c\"\n    END-PERFORM.\n    STOP RUN.\n",
            "COBOL1600", "F1 DISPLAY \"c\"");
        AssertCompiles(Files + head + "    WHEN EC-I-O FILE F3 DISPLAY \"c\"\n    END-PERFORM.\n    STOP RUN.\n");
    }

    [Fact]   // §14.9.28.3 SR14 — two paired F1 instances are legal; the bare third one breaks the rule
    public void Sr14FileNameRepeat_WithThreeInstances_IsReportedAtTheBareInstance()
    {
        const string head = "    PERFORM\n        ADD 1 TO N\n    WHEN EC-I-O-AT-END FILE F1 DISPLAY \"a\"\n"
            + "    WHEN EC-I-O-INVALID-KEY FILE F1 DISPLAY \"b\"\n";
        AssertAt(Files + head + "    WHEN EXCEPTION F1 DISPLAY \"c\"\n    END-PERFORM.\n    STOP RUN.\n",
            "COBOL1599", "F1 DISPLAY \"c\"");
        AssertCompiles(Files + head + "    WHEN EC-I-O-PERMANENT-ERROR FILE F1 DISPLAY \"c\"\n    END-PERFORM.\n"
            + "    STOP RUN.\n");
    }

    [Fact]   // §14.9.28.3 SR16 — the non-EC-I-O name paired with FILE, on its own WHEN line
    public void Sr16NonIoNamePairedWithFile_IsReportedAtTheWhenItem()
    {
        string src = Files + "    PERFORM\n        ADD 1 TO N\n    WHEN EC-SIZE DISPLAY \"a\"\n"
            + "    WHEN EC-BOUND-SUBSCRIPT FILE F1 DISPLAY \"b\"\n    END-PERFORM.\n    STOP RUN.\n";
        var (ok, diag) = EditionHarness.Compile(src, 2023);
        Assert.False(ok);
        var at = Positions(diag, "COBOL1601");
        Assert.Single(at);
        Assert.Equal(Where(src, "EC-BOUND-SUBSCRIPT FILE F1"), at[0]);
    }

    // ── two offenders are two facts; a nested PERFORM's offender is one fact ──────────────────────────────

    [Fact]   // before PB1154 both reports carried the PERFORM's position and text, so the second was dropped as a repeat
    public void TwoOffendingStatements_InOnePerform_AreTwoReports()
    {
        string src = Files + Perform("CLOSE F1 F2\n        CLOSE F2 F3");
        var (ok, diag) = EditionHarness.Compile(src, 2023);
        Assert.False(ok);
        var at = Positions(diag, "COBOL1612");
        Assert.Equal(2, at.Count);
        Assert.Equal(Where(src, "CLOSE F1 F2").Line, at[0].Line);
        Assert.Equal(Where(src, "CLOSE F2 F3").Line, at[1].Line);
    }

    [Fact]   // the outer and the inner Format-3 PERFORM both walk the inner statement; it is reported once
    public void OffenderInANestedFormat3Perform_IsReportedOnce()
    {
        string src = Files + "    PERFORM\n        PERFORM\n            CLOSE F1 F2\n        WHEN EC-SIZE DISPLAY \"i\"\n"
            + "        END-PERFORM\n    WHEN EC-SIZE DISPLAY \"o\"\n    END-PERFORM.\n    STOP RUN.\n";
        AssertAt(src, "COBOL1612", "CLOSE F1 F2");
    }

    // ── the legal twins: one operand in imp-1, the same statements outside the banned region ──────────────────

    [Fact]
    public void LegalTwins_Compile()
    {
        var (ok, diag) = EditionHarness.Compile(
            Files + Perform("CLOSE F1\n        DELETE FILE F2\n        OPEN OUTPUT F3\n        INITIALIZE N",
                "WHEN EC-SIZE CLOSE F1 F2\n    WHEN EC-I-O DELETE FILE F1 F2"), 2023);
        Assert.True(ok, "single-operand statements in imperative-statement-1 are conforming: " + string.Join(" | ", diag));
    }
}
