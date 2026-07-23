// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// UNSTRING sender / receiver CATEGORY syntax rules (ISO §14.9.48.3). SR2: identifier-1 (the sender) shall be
/// category alphanumeric or national — a numeric (INCLUDING usage DISPLAY, whose zoned image would otherwise be
/// examined as characters), numeric-edited, or boolean sender is not permitted (CONFORMANCE-FIX-QUEUE CA20). SR4:
/// identifier-4 (an INTO receiver) shall be usage display + category alphabetic/alphanumeric/numeric, or usage
/// national + category national/numeric — an edited, COMP/packed/COMP-5, index, boolean, or float receiver is not
/// permitted (CA19). The compiler follows the STRING-side convention: a violating shape binds to a runtime-loud
/// (BoundUnsupported ⇒ StatementEmitter LoudStmt) naming the SR, never a silent wrong extraction (§4.2.2 flagging
/// duty). Positive controls prove the screen is category-specific, not over-broad (a display-numeric or
/// alphanumeric receiver, and an alphanumeric sender, still bind and run).
/// </summary>
public sealed class UnstringSrCategoryTests
{
    private static string Program(string programId, string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {programId}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN.
        {proc}
            STOP RUN.
        """;

    private static void AssertLouds(string programId, string ws, string proc, string sr)
    {
        var (ok, _, detail) = new CobolNetCompiler(2023).CompileAndRun(Program(programId, ws, proc));
        Assert.False(ok, $"the UNSTRING must fail loud (§14.9.48.3 {sr}), not extract silently");
        Assert.Contains(sr, detail);
    }

    private static void AssertRuns(string programId, string ws, string proc)
    {
        var (ok, _, detail) = new CobolNetCompiler(2023).CompileAndRun(Program(programId, ws, proc));
        Assert.True(ok, $"an SR-legal UNSTRING must bind and run: {detail}");
    }

    // ── CA19 · SR4 receiver category ─────────────────────────────────────────────────────────────────────────

    // A numeric-EDITED receiver (category numeric-edited) is outside SR4's {display + alphabetic/alphanumeric/
    // numeric} ∪ {national + national/numeric} allowed set ⇒ must loud.
    [Fact]
    public void Unstring_IntoNumericEditedReceiver_LoudsSr4()
        => AssertLouds("UNSR4A", """
            01 SRC PIC X(3) VALUE "123".
            01 R1  PIC ZZ9.
            """, "    UNSTRING SRC INTO R1.", "SR4");

    // A non-DISPLAY numeric receiver (COMP-3 / packed) has usage packed, not display or national ⇒ must loud.
    [Fact]
    public void Unstring_IntoPackedNumericReceiver_LoudsSr4()
        => AssertLouds("UNSR4B", """
            01 SRC PIC X(3) VALUE "123".
            01 R3  PIC 999 USAGE COMP-3.
            """, "    UNSTRING SRC INTO R3.", "SR4");

    // Positive control: a usage-DISPLAY numeric receiver (SR4 "display + numeric") and an alphanumeric receiver
    // (SR4 "display + alphanumeric") are both permitted ⇒ bind and run, no loud.
    [Fact]
    public void Unstring_IntoDisplayNumericAndAlphanumericReceivers_Runs()
        => AssertRuns("UNSR4C", """
            01 SRC PIC X(7) VALUE "12,ABCD".
            01 RN  PIC 999.
            01 RA  PIC XXXX.
            """, "    UNSTRING SRC DELIMITED BY \",\" INTO RN RA.");

    // ── CA20 · SR2 sender category ───────────────────────────────────────────────────────────────────────────

    // A usage-DISPLAY numeric sender is category numeric, not alphanumeric/national ⇒ must loud (the confirmed
    // core: the old guard exempted usage DISPLAY, silently examining the zoned image as characters).
    [Fact]
    public void Unstring_FromDisplayNumericSender_LoudsSr2()
        => AssertLouds("UNSR2A", """
            01 SRC PIC 9(5) VALUE 12345.
            01 R1  PIC X(5).
            """, "    UNSTRING SRC INTO R1.", "SR2");

    // A boolean sender is category boolean, not alphanumeric/national ⇒ must loud.
    [Fact]
    public void Unstring_FromBooleanSender_LoudsSr2()
        => AssertLouds("UNSR2B", """
            01 SRC PIC 1 USAGE BIT VALUE B"1".
            01 R1  PIC X.
            """, "    UNSTRING SRC INTO R1.", "SR2");

    // Positive control: an alphanumeric sender (SR2-legal) binds and runs.
    [Fact]
    public void Unstring_FromAlphanumericSender_Runs()
        => AssertRuns("UNSR2C", """
            01 SRC PIC X(5) VALUE "AB,CD".
            01 R1  PIC XX.
            01 R2  PIC XX.
            """, "    UNSTRING SRC DELIMITED BY \",\" INTO R1 R2.");
}
