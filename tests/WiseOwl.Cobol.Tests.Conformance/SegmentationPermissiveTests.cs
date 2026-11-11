// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// kb/Work PB5223 and PB5224 — under <c>--permissive</c> at 2002 and later a section's segment-number is a warned, removed
/// construct (COBOL0902) that keeps the meaning COBOL-85 gives it: an independent segment (§1985:XVI.3.2.4 GR3, 50 through
/// 99) enters in its initial state (§1985:XVI.1.4.3). The rules that make that meaning well-defined are asked in the mode
/// too: the number's range, §1985:XVI.3.2.3 SR1 "The segment-number must be an integer ranging in value from 0 through 99."
/// (COBOL6819, PB5223: a larger number used to reach the emitter's byte table and fail the generated C#), and §1985:XVI.3.3.1
/// "A GO TO statement in a section whose segment-number is greater than or equal to 50 must not be referred to by an ALTER
/// statement in a section with a different segment-number." (COBOL6877, PB5224: the initial-state reset silently undid the
/// ALTER). Strict 2002 and later refuse the number (COBOL0902) and ask neither rule. The COBOL-85 cases of both rules are the
/// conformance-suite diagnostics pb5084-section-segment-number-out-of-range and pb5084-alter-from-another-segment; this class
/// is their --permissive half, which the suite's diagnostics file cannot express.
/// </summary>
public sealed class SegmentationPermissiveTests
{
    private const string AlterFromOtherSegment = """
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5224A.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M-1.
           ALTER A-SW TO PROCEED TO A-ALT.
           GO TO A-SW.
       IND-A SECTION 60.
       A-SW.
           GO TO A-INIT.
       A-INIT.
           DISPLAY "A INITIAL".
           STOP RUN.
       A-ALT.
           DISPLAY "A ALTERED".
           STOP RUN.
       """;

    // The ALTER is in the GO TO's own independent segment: §1985:XVI.3.3.1 allows it, and control never re-enters the segment
    // from another, so the GO TO keeps the ALTERed target. Expected output: M (segment 0 falls into 60), then A ALTERED.
    private const string AlterInOwnSegment = """
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5224B.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M-1.
           DISPLAY "M".
       IND-A SECTION 60.
       A-1.
           ALTER A-SW TO PROCEED TO A-ALT.
           GO TO A-SW.
       A-SW.
           GO TO A-INIT.
       A-INIT.
           DISPLAY "A INITIAL".
           STOP RUN.
       A-ALT.
           DISPLAY "A ALTERED".
           STOP RUN.
       """;

    private const string ThreeDigitSegment = """
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5223A.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M-1.
           ALTER A-SW TO PROCEED TO A-ALT.
           GO TO A-SW.
       IND-A SECTION 100.
       A-SW.
           GO TO A-INIT.
       A-INIT.
           DISPLAY "A INITIAL".
           STOP RUN.
       A-ALT.
           DISPLAY "A ALTERED".
           STOP RUN.
       """;

    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void AnAlter_FromAnotherSegment_OfAnIndependentSegmentGoTo_IsRefusedUnderPermissive(int edition)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(AlterFromOtherSegment, edition, permissive: true);
        Assert.False(ok, "§1985:XVI.3.3.1: the reset would silently undo this ALTER");
        EditionHarness.AssertHasDiagnostic(errors, "COBOL6877");
        EditionHarness.AssertEveryDiagnosticPositioned(errors, "COBOL6877 under --permissive");
    }

    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void AnAlter_InTheGoTosOwnIndependentSegment_RunsAlteredUnderPermissive(int edition)
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(AlterInOwnSegment, edition, permissive: true);
        Assert.True(ok, detail);
        Assert.Equal(new[] { "M", "A ALTERED" }, stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void AThreeDigitSegmentNumber_IsARangeErrorUnderPermissive(int edition)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(ThreeDigitSegment, edition, permissive: true);
        Assert.False(ok, "§1985:XVI.3.2.3 SR1: 100 is outside 0 through 99");
        EditionHarness.AssertHasDiagnostic(errors, "COBOL6819");
        EditionHarness.AssertNoDiagnostic(errors, "CS0029");
        EditionHarness.AssertEveryDiagnosticPositioned(errors, "COBOL6819 under --permissive");
    }

    [Theory]
    [InlineData(2002)]
    [InlineData(2023)]
    public void Strict_RefusesTheNumber_AndAsksNeitherRule(int edition)
    {
        foreach (string source in new[] { AlterFromOtherSegment, ThreeDigitSegment })
        {
            var (ok, errors, _) = EditionHarness.CompileFull(source, edition);
            Assert.False(ok);
            EditionHarness.AssertHasDiagnostic(errors, "COBOL0902");
            EditionHarness.AssertNoDiagnostic(errors, "error COBOL6877");
            EditionHarness.AssertNoDiagnostic(errors, "error COBOL6819");
        }
    }
}
