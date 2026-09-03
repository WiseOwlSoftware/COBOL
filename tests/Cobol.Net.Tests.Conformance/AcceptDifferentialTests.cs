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

    // §14.9.1.4 GR2: one transfer is EXACTLY one 80-character record — an input line longer than 80
    // characters is CONSECUTIVE records, each padded to the card image, so line characters 81+ land at
    // position 81 and their record PADS to 80 before the next line contributes. (The old whole-line append
    // put the next line flush at position 91 — the GR2 deviation kb/Work PB139 measured.)
    [Fact]
    public void Device_LongLine_SplitsAtRecordBoundary()
        => AssertOutputs(
            Program("ACCDEV6", "01 WS-X PIC X(170).", """
                ACCEPT WS-X.
                DISPLAY WS-X "]".
            """),
            expected: new string('A', 80) + new string('B', 10) + new string(' ', 70) + "C" + new string(' ', 9) + "]",
            stdin: new string('A', 80) + new string('B', 10) + "\nC\n");

    // §14.9.1.4 GR4b: a receiver needing only record 1 ignores the long line's REMAINING records along with
    // the rest of record 1 — the next ACCEPT reads a fresh input line, never leftover characters 81+.
    [Fact]
    public void Device_LongLine_NarrowReceiver_RemainingRecordsIgnored()
        => AssertOutputs(
            Program("ACCDEV7", """
                01 WS-A PIC X(5).
                01 WS-B PIC XX.
                """, """
                ACCEPT WS-A.
                ACCEPT WS-B.
                DISPLAY WS-A "]" WS-B "]".
            """),
            expected: "ABCDE]XY]",
            stdin: "ABCDE" + new string('Z', 85) + "\nXY\n");

    // §14.9.1.3 SR1's COMPLEMENT: the DEVICE format permits a class-alphabetic receiver — only SR3 (the
    // temporal format) excludes class alphabetic. The screen must not over-reject Format 1.
    [Fact]
    public void Device_AlphabeticReceiver_Permitted()
        => AssertOutputs(
            Program("ACCDEV8", "01 WS-A PIC A(6).", """
                ACCEPT WS-A.
                DISPLAY WS-A "]".
            """),
            expected: "WORD  ]",
            stdin: "WORD\n");

    // §14.9.1.4 GR1: the device-to-receiver conversion is implementor-defined; for a BOOLEAN receiver (which
    // SR1 does NOT exclude) ours converts each transferred '1' to boolean one and every other character —
    // including the card image's pad spaces — to boolean zero, keeping the §13.18.40.4 GR14 '0'/'1'
    // representation invariant. (The old default-arm raw store put pad SPACES into boolean storage.)
    [Fact]
    public void Device_BooleanReceiver_ConvertsToBooleanCharacters()
        => AssertOutputs(
            Program("ACCDEV9", "01 WS-B PIC 1(4).", """
                ACCEPT WS-B.
                DISPLAY WS-B "]".
            """),
            expected: "1000]",
            stdin: "10\n",
            dialect: 2002);

    // §14.9.1.4 GR6 + §14.6.8.5: a NATIONAL receiver takes the temporal digit image exactly like
    // alphanumeric on the character substrate — left-justified, space-filled (TIME's 8-digit image into
    // N(10)). NATIONAL is 2002+. The old emitter had no national arm and staged a loud reject of the
    // SR3-legal receiver (kb/Work PB139).
    [Fact]
    public void Time_NationalReceiver_StoresDigitImage()
        => AssertOutputs(
            Program("ACCTIM2", "01 WS-N PIC N(10).", """
                ACCEPT WS-N FROM TIME.
                DISPLAY WS-N "]".
            """),
            expected: "14304567  ]",
            clock: "2026-06-10T14:30:45.67",
            dialect: 2002);

    // §14.9.1.4 GR6 via §14.9.25.4 GR6c / §13.18.34: a JUSTIFIED receiver right-justifies the temporal
    // image — left space-fill when larger, LEFT truncation when smaller (DAY's image "26161" keeps its
    // low-order characters "161" in X(3)). The old emitter ignored JUSTIFIED (kb/Work PB139).
    [Fact]
    public void Day_JustifiedReceiver_RightJustifiesAndLeftTruncates()
        => AssertOutputs(
            Program("ACCDAY2", """
                01 WS-J PIC X(8) JUSTIFIED RIGHT.
                01 WS-K PIC X(3) JUSTIFIED RIGHT.
                """, """
                ACCEPT WS-J FROM DAY.
                ACCEPT WS-K FROM DAY.
                DISPLAY "[" WS-J "]" WS-K "]".
            """),
            expected: "[   26161]161]",
            clock: "2026-06-10T14:30:45.67");

    // kb/Work PB180 — a BINARY member of a REDEFINES class (a Tier-B byte-form window) accepts by the
    // SAME §14.9.1.4 GR1 conversion every numeric receiver takes: four typed device characters decode to
    // the VALUE 1234 and re-encode as the window's TWO radix-2 bytes (04 D2 — pinned via FUNCTION ORD on
    // the redefined X view, §15.70). The old image arm spliced the raw characters ("1234" → bytes 31 32 =
    // the value 12594) — a silent wrong answer on shipping Tier B, and the discriminating first line
    // would have read 12594 with ORD 50/51.
    [Fact]
    public void Device_TierBBinaryWindow_ConvertsThenReencodes()
        => AssertOutputs(
            Program("ACCPB180", """
                01 WS-G.
                   05 WS-G-N PIC 9(4) COMP.
                   05 WS-G-A PIC X(3).
                01 WS-R REDEFINES WS-G PIC X(5).
                """, """
                MOVE "abc" TO WS-G-A.
                ACCEPT WS-G-N.
                DISPLAY WS-G-N.
                DISPLAY FUNCTION ORD(WS-R(1:1)) " " FUNCTION ORD(WS-R(2:1)).
                DISPLAY WS-G-A.
            """),
            expected: "1234\n5 211\nabc",
            stdin: "1234",
            dialect: 2023);

    // kb/Work PB173 — the OMITTED-LENGTH ref-mod width over a GROUP-USAGE BIT receiver, the THIRD arm of the
    // pad family (PlaceRenderer.Write's boolean pad and MoveEmitter's RefModSlice fill were the first two).
    // §8.4.3.3.4 GR5c: "If length is not specified, the unique data item extends from and includes the position
    // identified by leftmost-position up to and including the rightmost position of the data item referenced by
    // identifier-1"; GR5a: "If the usage of identifier-1 is bit, positions used in evaluation are bit positions".
    // So the omitted-length transfer size is m − start + 1 in BIT positions (m = 8 by §13.18.29.4 GR1b's as-if
    // PICTURE 1(8)), and the omitted form must equal its explicit-length twin. The emitter read raw `Pic`, which
    // is NULL for any group, and fell back to ImageWidth = ceil(8/8) = 1 PACKED CHARACTER: `(3:)` computed
    // 1 − 3 + 1 = −1 and `(1:)` computed 1, so EVERY start under-transferred and zero-filled the remainder
    // (measured before the fix: 11000000 for the (3:) leg, against the 11101010 both other legs produce).
    // Each transferred character converts by the same §14.9.1.4 GR1 boolean conversion
    // Device_BooleanReceiver_ConvertsToBooleanCharacters pins.
    [Fact]
    public void Device_OmittedLengthBitGroupSlice_TransfersBitPositions()
        => AssertOutputs(
            Program("ACCPB173", """
                01 WS-XM GROUP-USAGE BIT.
                   05 WS-XM1 PIC 1(4) VALUE B"1100".
                   05 WS-XM2 PIC 1(4) VALUE B"1010".
                01 WS-XN GROUP-USAGE BIT.
                   05 WS-XN1 PIC 1(4) VALUE B"1100".
                   05 WS-XN2 PIC 1(4) VALUE B"1010".
                01 WS-XP GROUP-USAGE BIT.
                   05 WS-XP1 PIC 1(4) VALUE B"1100".
                   05 WS-XP2 PIC 1(4) VALUE B"1010".
                """, """
                ACCEPT WS-XM(3:).
                ACCEPT WS-XN(3:6).
                ACCEPT WS-XP(1:).
                DISPLAY WS-XM "]" WS-XN "]" WS-XP "]".
            """),
            expected: "11101010]11101010]10101010]",
            stdin: "101010\n101010\n10101010\n",
            dialect: 2023);

    // §14.9.1.4 GR3: "If a device is capable of transferring data of the same size as the receiving data
    // item, the transferred data is stored in the receiving data item." This implementation's transfer size
    // is one 80-character card-image record (GR2 — "the implementor shall define, for each device, the size
    // of a data transfer"), so an X(80) receiver is EXACTLY the same size as one transfer: the record is
    // stored WHOLE — not truncated, not edited, and no additional data is requested. Every other device
    // fact here lands in GR4a or GR4b (a receiver smaller or larger than the transfer); the exact-size case
    // is the one GR3 governs, and no other test in this class sits on the boundary (widths 2, 3, 4, 5, 10,
    // 100 and 170 all miss it).
    [Fact]
    public void Device_ExactSizeReceiver_StoresTheWholeTransfer()
        => AssertOutputs(
            Program("ACCDEV10", "01 WS-X PIC X(80).", """
                ACCEPT WS-X.
                DISPLAY WS-X "]".
            """),
            expected: new string('A', 40) + new string('B', 40) + "]",
            stdin: new string('A', 40) + new string('B', 40) + "\n");

    // §14.9.1.4 GR3 + GR2, the DISCRIMINATING leg: the transferred data is one RECORD — a short input line
    // padded to the full card image — so it is still the same size as an X(80) receiver and GR3 applies,
    // never GR4a. The proof is the SECOND ACCEPT: because GR3 stored the transfer whole and requested no
    // additional data, WS-Y reads a FRESH input line. Had the transfer been sized to the five typed
    // characters instead, GR4a would have pulled line 2 into positions 6..80 of WS-X and WS-Y would have
    // met end-of-input (two spaces).
    [Fact]
    public void Device_ExactSizeReceiver_ShortLine_ConsumesExactlyOneRecord()
        => AssertOutputs(
            Program("ACCDEV11", """
                01 WS-X PIC X(80).
                01 WS-Y PIC XX.
                """, """
                ACCEPT WS-X.
                ACCEPT WS-Y.
                DISPLAY WS-X "]" WS-Y "]".
            """),
            expected: "HELLO" + new string(' ', 75) + "]ZZ]",
            stdin: "HELLO\nZZ\n");

    // §14.9.1.4 GR10 + the version-gating rule: DAY with the phrase YYYYDDD "behaves as if it had been
    // described as an unsigned elementary integer data item of usage display SEVEN digits in length",
    // character positions 1-4 "four numeric characters of the year in the Gregorian calendar" and 5-7
    // "three numeric characters of the day of the year in the range 001 through 366". 2026-06-10 is day
    // 161 (31+28+31+30+31+10 — 2026 is not a leap year), so the conceptual value is 2026161 and an
    // exact-width 9(7) receiver shows precisely that seven-digit format. The YYYYDDD phrase is an ISO 2002
    // introduction (the 1985 Format 2 lists only bare DATE / DAY / DAY-OF-WEEK / TIME), so --std 85 rejects
    // it. The gate's message ternary has TWO arms and only the DATE one was ever exercised
    // (DateYYYYMMDD_RejectedAt85_RunsAt2002), so the DAY spelling is asserted explicitly.
    [Fact]
    public void DayYYYYDDD_RejectedAt85_RunsAt2002()
    {
        string source = Program("ACCDAY3", "01 WS-D PIC 9(7).", """
                ACCEPT WS-D FROM DAY YYYYDDD.
                DISPLAY WS-D.
            """);
        var (ok85, diags) = EditionHarness.Compile(source, 85);
        Assert.False(ok85, "ACCEPT FROM DAY YYYYDDD must be rejected at --std 85 (introduced by ISO 2002, §14.9.1)");
        EditionHarness.AssertHasDiagnostic(diags, "COBOLNET0815");
        EditionHarness.AssertHasDiagnostic(diags, "ACCEPT FROM DAY YYYYDDD");

        AssertOutputs(source, expected: "2026161", clock: "2026-06-10T14:30:45.67", dialect: 2002);
    }
}
