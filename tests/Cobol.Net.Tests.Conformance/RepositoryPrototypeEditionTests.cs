// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// EXACT-COUNT witnesses for the Step-14g.5 gates — the FUNCTION-ID … IS PROTOTYPE unit (§11.5, bound-arm over
/// <c>CallUnit.IsPrototype</c>), the REPOSITORY CLASS/INTERFACE/PROPERTY specifiers (§12.3.8, parse-arm), and the two
/// recognized-but-unimplemented PICTURE skeletons — an external floating-point picture (symbol E) and national-edited
/// data — which gate BOUND-arm via <c>PicInfo.SkeletonGate</c> (their category is recovered to Alphanumeric, so
/// PicInfo's own detection carries the 0900 forward to <c>GateData</c>). The version matrix +
/// <c>DataSkeletonEditionTests</c> verify PRESENCE; these pin the FIRING COUNT — exactly ONE per construct.
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

    /// <summary>An external floating-point PICTURE (symbol E) gates EXACTLY ONCE at 85 — carried by
    /// <c>PicInfo.SkeletonGate</c> to the bound-arm after the category is recovered to Alphanumeric.</summary>
    [Fact]
    public void ExternalFloatPicture_At85_ExactlyOne0900()
        => Assert.Equal(1, Count0900(Prog("01 WS-EF PIC 9V99E+99."), 85, "external floating-point PICTURE"));

    /// <summary>A national-edited PICTURE gates EXACTLY ONCE at 85 (the same SkeletonGate carrier).</summary>
    [Fact]
    public void NationalEditedPicture_At85_ExactlyOne0900()
        => Assert.Equal(1, Count0900(Prog("01 WS-M PIC NN0NN."), 85, "national-edited data"));
}
