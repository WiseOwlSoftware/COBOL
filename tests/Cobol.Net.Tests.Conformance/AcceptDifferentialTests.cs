// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using CobolNet.Tests.Shared;                             // ProcessObserver — the ONE child-process observer
using CobolNet;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ACCEPT (ISO §14.9.1) — SPEC-PINNED facts. These are not legacy-differential: the device form needs a piped
/// stdin and the temporal forms a pinned clock (two seams the shared <c>ICompilerUnderTest</c> runner does not
/// thread), and the temporal STORE rule is a place where the legacy deviates from the spec (it stored the
/// temporal text left-justified-raw for every receiver; §14.9.1.4 GR6 says BY THE MOVE RULES — a numeric receiver
/// decimal-aligns and keeps LOW-order digits). Every expected value below is derived from the cited general rule.
/// The clock pins through the <c>COBOLNET_CLOCK</c> process seam (<c>AcceptSource.Now</c>'s default).
/// </summary>
public sealed class AcceptDifferentialTests
{
    /// <summary>Compile (at <paramref name="dialect"/>) and run with <paramref name="stdin"/> piped to the
    /// program's standard input and, when given, <paramref name="clock"/> pinned via <c>COBOLNET_CLOCK</c>.</summary>
    private static (bool ok, string stdout, string detail) AcceptRun(
        string source, string stdin = "", string? clock = null, int dialect = 85)
    {
        string dir = CutRunner.NewTempDir("acc");
        try
        {
            string src = Path.Combine(dir, "prog.cob");
            string dll = Path.Combine(dir, "prog.dll");
            File.WriteAllText(src, source);
            var result = CompilerDriver.Compile(new CompilerDriver.Options(src, dll, DialectLevel: dialect));
            if (!result.Success)
                return (false, "", $"[compile] {result.Status}: {string.Join("\n", result.Errors)}");

            // The ONE child-process observer (tests/_shared/ProcessObservation.cs). This method used to carry
            // its own copy of the 30s-timeout-returns-empty-output shape; a timeout here surfaced as an
            // ACCEPT test seeing no input echoed back, which reads exactly like a semantic defect. See §11 A12.
            var psi = new ProcessStartInfo("dotnet", $"\"{dll}\"") { WorkingDirectory = dir };
            if (clock is not null) psi.Environment["COBOLNET_CLOCK"] = clock;
            var obs = ProcessObserver.ObserveOrThrow(psi, stdin);
            return (obs.ExitCode == 0, CutRunner.Normalize(obs.Stdout), CutRunner.Normalize(obs.Stderr));
        }
        finally { CutRunner.TryDelete(dir); }
    }

    private static void AssertOutputs(string source, string expected, string stdin = "", string? clock = null, int dialect = 85)
    {
        var (ok, stdout, detail) = AcceptRun(source, stdin, clock, dialect);
        Assert.True(ok, detail);
        Assert.Equal(expected, stdout);
    }

    private static string Program(string progId, string data, string procedure) => $$"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {{progId}}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {{data}}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {{procedure}}
            STOP RUN.
        """;

    // §14.9.1.4 GR4a: a transfer SMALLER than the receiver stores aligned left — the receiver's tail is filled
    // by the (space-padded) remainder of the 80-character card image (GR2), not by MOVE-rule editing.
    [Fact]
    public void Device_ShortLine_StoresLeftAlignedSpaceFilled()
        => AssertOutputs(
            Program("ACCDEV1", "01 WS-X PIC X(10).", """
                ACCEPT WS-X.
                DISPLAY WS-X "]".
            """),
            expected: "ABC       ]",
            stdin: "ABC\n");

    // §14.9.1.4 GR4b: a transfer LARGER than the receiver keeps only the LEFTMOST characters that fit; the rest
    // of the record is IGNORED — the next ACCEPT starts a fresh record, never the leftover of this one.
    [Fact]
    public void Device_OverlongLine_KeepsLeftmost_RestOfRecordIgnored()
        => AssertOutputs(
            Program("ACCDEV2", """
                01 WS-A PIC X(3).
                01 WS-B PIC XX.
                """, """
                ACCEPT WS-A.
                ACCEPT WS-B.
                DISPLAY WS-A "]" WS-B "]".
            """),
            expected: "ABC]XY]",
            stdin: "ABCDEFGH\nXY\n");

    // §14.9.1.4 GR4a + GR2: a receiver larger than one 80-character record requests ADDITIONAL records until
    // full; each short record is padded to the full card image, so record 2 lands at position 81.
    [Fact]
    public void Device_WideReceiver_ReadsAdditionalRecords()
        => AssertOutputs(
            Program("ACCDEV3", "01 WS-X PIC X(100).", """
                ACCEPT WS-X.
                DISPLAY WS-X "]".
            """),
            expected: "1" + new string(' ', 79) + "2" + new string(' ', 19) + "]",
            stdin: "1\n2\n");

    // §14.9.1.4 GR4a end-of-input: when the device can supply no further data, the unfilled remainder of the
    // receiver stays spaces (the legacy's NIST-proven EOF behavior; the leading "[" pins the full field width).
    [Fact]
    public void Device_EndOfInput_LeavesSpaces()
        => AssertOutputs(
            Program("ACCDEV4", "01 WS-X PIC X(5).", """
                ACCEPT WS-X.
                DISPLAY "[" WS-X "]".
            """),
            expected: "[     ]",
            stdin: "");

    // §14.9.1.4 GR1: device data REPLACES the receiver's content with implementor-defined conversion — ours
    // decodes the zoned digit image into the native numeric value, so the item is immediately computable.
    [Fact]
    public void Device_NumericReceiver_ConvertsAndComputes()
        => AssertOutputs(
            Program("ACCDEV5", "01 WS-N PIC 9(4).", """
                ACCEPT WS-N.
                ADD 1 TO WS-N.
                DISPLAY WS-N.
            """),
            expected: "0043",
            stdin: "0042\n");

    // §14.9.1.4 GR7: DATE is the conceptual 6-digit unsigned integer YYMMDD — an exact-width numeric receiver
    // shows precisely that format.
    [Fact]
    public void Date_FormatIsYYMMDD()
        => AssertOutputs(
            Program("ACCDAT1", "01 WS-D PIC 9(6).", """
                ACCEPT WS-D FROM DATE.
                DISPLAY WS-D.
            """),
            expected: "260610",
            clock: "2026-06-10T14:30:45.67");

    // §14.9.1.4 GR6 + GR11: the temporal transfer follows THE MOVE RULES from a conceptual 8-digit integer
    // (TIME = HHMMSScc); a smaller NUMERIC receiver decimal-aligns and keeps the LOW-order digits (§14.9.25.4 —
    // 14304567 into 9(4) is 4567). The legacy's left-justified-raw store would give 1430 — the GR6 deviation
    // this fact pins to the spec.
    [Fact]
    public void Time_SmallNumericReceiver_KeepsLowOrderDigits()
        => AssertOutputs(
            Program("ACCTIM1", "01 WS-T PIC 9(4).", """
                ACCEPT WS-T FROM TIME.
                DISPLAY WS-T.
            """),
            expected: "4567",
            clock: "2026-06-10T14:30:45.67");

    // §14.9.1.4 GR6 + GR9: into an ALPHANUMERIC receiver the MOVE rules apply alphanumerically — the conceptual
    // DAY image "26161" (YYDDD; 2026-06-10 is day 161) left-justifies and truncates on the RIGHT into X(4).
    [Fact]
    public void Day_AlphanumericReceiver_TruncatesRight()
        => AssertOutputs(
            Program("ACCDAY1", "01 WS-A PIC X(4).", """
                ACCEPT WS-A FROM DAY.
                DISPLAY WS-A "]".
            """),
            expected: "2616]",
            clock: "2026-06-10T14:30:45.67");

    // §14.9.1.4 GR12: DAY-OF-WEEK is a 1-digit integer where 1 IS MONDAY … 7 is Sunday (NOT the .NET Sunday=0
    // convention): 2026-06-08 is a Monday, 2026-06-14 a Sunday.
    [Theory]
    [InlineData("2026-06-08T09:00:00", "1")]
    [InlineData("2026-06-14T09:00:00", "7")]
    public void DayOfWeek_MondayIs1_SundayIs7(string clock, string expected)
        => AssertOutputs(
            Program("ACCDOW1", "01 WS-W PIC 9.", """
                ACCEPT WS-W FROM DAY-OF-WEEK.
                DISPLAY WS-W.
            """),
            expected: expected,
            clock: clock);

    // §14.9.1.4 GR8 + the version-gating rule: the DATE YYYYMMDD (four-digit-year) phrase was introduced by
    // ISO/IEC 1989:2002 — rejected with the edition diagnostic at --std 85, and the 8-digit conceptual value at
    // 2002+ (feedback_version_test_matrix: a new construct is rejected below its introducing edition).
    [Fact]
    public void DateYYYYMMDD_RejectedAt85_RunsAt2002()
    {
        string source = Program("ACCDAT2", "01 WS-D PIC 9(8).", """
                ACCEPT WS-D FROM DATE YYYYMMDD.
                DISPLAY WS-D.
            """);
        var (ok85, diags) = EditionHarness.Compile(source, 85);
        Assert.False(ok85, "ACCEPT FROM DATE YYYYMMDD must be rejected at --std 85 (introduced by ISO 2002, §14.9.1)");
        EditionHarness.AssertHasDiagnostic(diags, "COBOLNET0815");

        AssertOutputs(source, expected: "20260610", clock: "2026-06-10T14:30:45.67", dialect: 2002);
    }
}
