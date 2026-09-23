// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ISO §5.5 1) — "When the term 'integer-n' (n = 1, 2, …) is used in a general format and associated rules, it
/// refers to a fixed-point integer literal that shall be unsigned and nonzero unless otherwise specified in the
/// associated rules" — enforced by the ONE screen, <c>Validation/IntegerOperandPass</c> (COBOLNET2386, kb/Work
/// PB859). The rejections sample the DEFAULT across divisions (a data clause, a file clause, a statement, a
/// report clause); the acceptances are the positions whose own rule otherwise-specifies, each named by its rule.
/// The goldens <c>85/pb859_integer_n_zero_permitted</c> and <c>negative/pb859-occurs-zero-times</c> run the rest.
/// </summary>
public sealed class IntegerOperandZeroTests
{
    private static string Prog(string pid, string fileControl, string fileSection, string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT PRTF ASSIGN TO "w56iz.tmp".
        {fileControl}
        DATA DIVISION.
        FILE SECTION.
        FD PRTF{fileSection}.
        01 PR PIC X(20).
        WORKING-STORAGE SECTION.
        01 N PIC 9 VALUE 1.
        {ws}
        PROCEDURE DIVISION.
            {proc}
            STOP RUN.
        """;

    [Theory]
    [InlineData("PB859R1", "", "", "01 T.\n    05 E PIC X OCCURS 00 TIMES.", "DISPLAY N", "OCCURS 00 TIMES: the integer operand 00 shall be nonzero")]
    [InlineData("PB859R2", "", "\n    BLOCK CONTAINS 0 RECORDS", "", "DISPLAY N", "BLOCK CONTAINS 0 RECORDS: the integer operand 0 shall be nonzero")]
    [InlineData("PB859R3", "", "\n    LINAGE IS 0 LINES", "", "DISPLAY N", "LINAGE IS 0 LINES: the integer operand 0 shall be nonzero")]
    [InlineData("PB859R4", "", "", "", "PERFORM 0 TIMES\n DISPLAY N\n END-PERFORM", "0 TIMES: the integer operand 0 shall be nonzero")]
    [InlineData("PB859R5", "", "\n    RECORD CONTAINS 0 CHARACTERS", "", "DISPLAY N", "RECORD CONTAINS 0 CHARACTERS: the integer operand 0 shall be nonzero")]
    public void ZeroWhereNoRulePermitsIt_IsCOBOLNET2386(string pid, string fc, string fd, string ws, string proc, string expected)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Prog(pid, fc, fd, ws, proc), 2023);
        Assert.False(ok, $"[{pid}] must be REJECTED");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET2386");
        EditionHarness.AssertHasDiagnostic(errors, expected);
    }

    [Theory]
    [InlineData("PB859A1", "\n    LINAGE IS 10 LINES LINES AT TOP 0 LINES AT BOTTOM 0", "", "DISPLAY N")]  // §13.18.34.3 SR4
    [InlineData("PB859A2", "\n    RECORD CONTAINS 0 TO 20 CHARACTERS", "", "DISPLAY N")]                   // §13.18.43.3 SR8
    [InlineData("PB859A3", "\n    RECORD IS VARYING IN SIZE FROM 0 TO 20", "", "DISPLAY N")]              // §13.18.43.3 SR7
    [InlineData("PB859A4", "", "01 T.\n    05 E PIC X OCCURS 0 TO 3 DEPENDING ON N.", "DISPLAY N")]    // §13.18.38.3 SR16
    [InlineData("PB859A5", "\n    LINAGE IS 10 LINES", "", "OPEN OUTPUT PRTF\n WRITE PR AFTER ADVANCING 0 LINES\n CLOSE PRTF")] // §14.9.51.3 SR15
    public void ZeroWhereItsRulePermitsIt_IsAccepted(string pid, string fd, string ws, string proc)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Prog(pid, "", fd, ws, proc), 2023);
        Assert.True(ok, $"[{pid}] must compile: {string.Join(" | ", errors)}");
    }
}
