// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// EXACT-COUNT witnesses for the Step-14g.5 gates — the FUNCTION-ID … IS PROTOTYPE unit (§11.5, bound-arm over
/// <c>CallUnit.IsPrototype</c>), the REPOSITORY CLASS/INTERFACE/PROPERTY specifiers (§12.3.8, parse-arm), and the two
/// PICTURE-shape gates — the floating-point numeric-edited picture (symbol E; LIVE since data-model design D21 /
/// kb/Work PB66, keyed on <c>PicInfo.IsFloatEdited</c>) and the recognized-but-unimplemented national-edited skeleton
/// (<c>PicInfo.SkeletonGate</c>; its category is recovered to Alphanumeric) — which gate BOUND-arm through the ONE
/// <c>VersionConformancePass.PictureConstructId</c>. The version matrix + <c>DataSkeletonEditionTests</c> verify
/// PRESENCE; these pin the FIRING COUNT — exactly ONE per construct.
/// </summary>
public sealed class RepositoryPrototypeEditionTests
{
    private static int Count0900(string source, int edition, string whereFragment)
    {
        var (_, errors, _) = EditionHarness.CompileFull(source, edition);
        return errors.Count(e => e.Contains("COBOLNET0900", StringComparison.OrdinalIgnoreCase)
            && e.Contains(whereFragment, StringComparison.OrdinalIgnoreCase));
    }

    private static string Prog(string wsLines) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. RPE.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {wsLines}
        PROCEDURE DIVISION.
        MAIN.
            STOP RUN.
        """;

    private const string Prototype = """
        IDENTIFICATION DIVISION.
        FUNCTION-ID. SQ IS PROTOTYPE.
        DATA DIVISION.
        LINKAGE SECTION.
        01 L-X PIC 9(4).
        01 L-R PIC 9(4).
        PROCEDURE DIVISION USING L-X RETURNING L-R.
        """;

    private const string Repository = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. RP.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS MYCLS
            INTERFACE MYIF
            PROPERTY MYPROP.
        PROCEDURE DIVISION.
        MAIN.
            STOP RUN.
        """;

    /// <summary>A FUNCTION-ID … IS PROTOTYPE unit gates EXACTLY ONCE at 85 (bound-arm over CallUnit.IsPrototype),
    /// never at 2002.</summary>
    [Fact]
    public void Prototype_At85_ExactlyOne0900()
        => Assert.Equal(1, Count0900(Prototype, 85, "IS PROTOTYPE (function prototype)"));

    [Fact]
    public void Prototype_At2002_NoGate()
        => Assert.Equal(0, Count0900(Prototype, 2002, "IS PROTOTYPE (function prototype)"));

    /// <summary>Each REPOSITORY CLASS / INTERFACE / PROPERTY specifier gates EXACTLY ONCE at 85, naming itself.</summary>
    [Fact]
    public void RepositoryClass_At85_ExactlyOne0900()
        => Assert.Equal(1, Count0900(Repository, 85, "REPOSITORY CLASS 'MYCLS'"));

    [Fact]
    public void RepositoryInterface_At85_ExactlyOne0900()
        => Assert.Equal(1, Count0900(Repository, 85, "REPOSITORY INTERFACE 'MYIF'"));

    [Fact]
    public void RepositoryProperty_At85_ExactlyOne0900()
        => Assert.Equal(1, Count0900(Repository, 85, "REPOSITORY PROPERTY 'MYPROP'"));

    /// <summary>A floating-point numeric-edited PICTURE (symbol E) gates EXACTLY ONCE at 85 — keyed on
    /// <c>PicInfo.IsFloatEdited</c> through <c>PictureConstructId</c> (kb/Work PB66).</summary>
    [Fact]
    public void FloatEditedPicture_At85_ExactlyOne0900()
        => Assert.Equal(1, Count0900(Prog("01 WS-EF PIC +9.99E+99."), 85, "floating-point numeric-edited PICTURE"));

    /// <summary>A national-edited PICTURE gates EXACTLY ONCE at 85 (the same SkeletonGate carrier).</summary>
    [Fact]
    public void NationalEditedPicture_At85_ExactlyOne0900()
        => Assert.Equal(1, Count0900(Prog("01 WS-M PIC NN0NN."), 85, "national-edited data"));

    // A report with a NON-PRINTABLE SUM counter carrying an external-float picture. The SUM-counter scale-derivation
    // Analyze is a DISTINCT call off the ConformanceForest AND the printable-item walk, so its 0900 must ride
    // ReportSumModel.SkeletonGate through GateData's report-Sums walk (DEVLOG 740 — the 14g.5 review found it dropped).
    private const string ReportSumExternalFloat = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. RSUM.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT RPT ASSIGN TO "RPTF".
        DATA DIVISION.
        FILE SECTION.
        FD RPT REPORT IS R-1.
        WORKING-STORAGE SECTION.
        01 WS-KEY PIC 9 VALUE 1.
        01 WS-AMT PIC 9 VALUE 1.
        REPORT SECTION.
        RD R-1 CONTROL IS WS-KEY.
        01 DET-1 TYPE DE LINE PLUS 1.
            03 COLUMN 1 PIC X(2) VALUE "DE".
        01 CF-1 TYPE CF WS-KEY LINE PLUS 1.
            03 TOT PIC 9.99E+99 SUM WS-AMT.
        PROCEDURE DIVISION.
        MAIN.
            STOP RUN.
        """;

    /// <summary>DEVLOG-740 regression: a floating-point numeric-edited PICTURE on a NON-printable report SUM counter
    /// still gates EXACTLY ONCE at 85 — the SUM-counter Analyze PicInfo is discarded (only its scale is used), so the
    /// 0900 must be carried on ReportSumModel.SkeletonGate (PictureConstructId) and fired by GateData's report-Sums
    /// walk, else it is silently dropped.</summary>
    [Fact]
    public void ReportSumExternalFloat_At85_ExactlyOne0900()
        => Assert.Equal(1, Count0900(ReportSumExternalFloat, 85, "floating-point numeric-edited PICTURE"));
}
