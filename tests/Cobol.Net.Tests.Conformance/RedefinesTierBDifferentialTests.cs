// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// REDEFINES Tier B (ISO/IEC 1989:2023 §13.18.44; COBOLNET_DESIGN §4.2): a DISPLAY-homogeneous class (alphanumeric /
/// DISPLAY-numeric / numeric-edited views over one storage area). The canonical is ONE <see cref="string"/> backing of
/// class-max width; each view is a typed <c>(offset,width)</c> window over it — a numeric view decodes/encodes via
/// <c>CobolNum.ParseDisplay</c>/<c>FormatDisplay</c>. A write through any view is visible through every other (one
/// backing, NO byte[]). Pinned to the legacy oracle (spec-pinned where the legacy DISPLAY trailing-trim quirk shows).
/// </summary>
public sealed class RedefinesTierBDifferentialTests
{
    private static readonly ICompilerUnderTest Legacy = new LegacyCompiler();
    private static readonly ICompilerUnderTest CobolNet = new CobolNetCompiler();

    private static void AssertSameAsLegacy(string source)
    {
        var (lok, lout, ldetail) = Legacy.CompileAndRun(source);
        Assert.True(lok, $"legacy oracle failed: {ldetail}");
        var (cok, cout, cdetail) = CobolNet.CompileAndRun(source);
        Assert.True(cok, $"COBOL.NET failed: {cdetail}");
        Assert.Equal(lout, cout);
    }

    private static string Program(string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. REDEFB.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {proc}
            STOP RUN.
        """;

    [Fact]
    public void NumericViewWrite_VisibleThroughAlphanumericOriginal()
        => AssertSameAsLegacy(Program(
            "01 WS-A PIC X(6).\n01 WS-B REDEFINES WS-A PIC 9(6).",
            "    MOVE 42 TO WS-B.\n    DISPLAY WS-A."));   // 000042 — numeric view formats into the shared backing

    [Fact]
    public void NumericViewRead_DecodesTheBacking()
        => AssertSameAsLegacy(Program(
            "01 WS-A PIC X(4) VALUE \"0025\".\n01 WS-B REDEFINES WS-A PIC 9(4).\n01 WS-C PIC 9(4).",
            "    MOVE WS-B TO WS-C.\n    ADD 5 TO WS-C.\n    DISPLAY WS-C."));   // 0030 — view read via ParseDisplay, then +5

    [Fact]
    public void AlphanumericView_OverNumericCanonical()
        => AssertSameAsLegacy(Program(
            "01 WS-A PIC 9(6) VALUE 123456.\n01 WS-B REDEFINES WS-A PIC X(6).",
            "    DISPLAY WS-B."));   // 123456 — the numeric canonical's image read as characters

    [Fact]
    public void WriteAlphanumericView_ReadNumericView_Coherent()
        => AssertSameAsLegacy(Program(
            "01 WS-A PIC X(6).\n01 WS-B REDEFINES WS-A PIC 9(6).",
            "    MOVE \"012345\" TO WS-A.\n    DISPLAY WS-B."));   // 012345 — one backing, both views agree

    [Fact]
    public void GroupView_PartialFields_AlphaAndNumeric()
        => AssertSameAsLegacy(Program("""
            01 WS-A PIC X(6) VALUE "AB1234".
            01 WS-B REDEFINES WS-A.
               05 WS-B1 PIC X(2).
               05 WS-B2 PIC 9(4).
            """, "    DISPLAY WS-B1 \"|\" WS-B2."));   // AB|1234 — windows at offsets 0 and 2

    [Fact]
    public void LargerRedefiner_SR8_ClassMaxWidth()
        // SR8: a level-01 non-EXTERNAL item may be redefined larger; the backing is sized to the class max (8).
        => AssertSameAsLegacy(Program(
            "01 WS-A PIC X(4) VALUE \"ABCD\".\n01 WS-B REDEFINES WS-A PIC X(8).",
            "    MOVE \"ABCDEFGH\" TO WS-B.\n    DISPLAY WS-B \"|\" WS-A."));   // ABCDEFGH|ABCD

    [Fact]
    public void InGroup_TierBBacking_NestedImage()
        // The CCVS COMPUTED-X shape: a REDEFINES class nested inside a group — the backing is a struct member, and the
        // outer group's AsImage counts it once (not the views).
        => AssertSameAsLegacy(Program("""
            01 WS-REC.
               05 WS-HEAD PIC X(2) VALUE "HD".
               05 WS-X.
                  10 WS-A PIC X(4) VALUE "0042".
                  10 WS-B REDEFINES WS-A PIC 9(4).
               05 WS-TAIL PIC X(2) VALUE "TL".
            """, "    DISPLAY WS-REC."));   // HD0042TL

    [Fact]
    public void OriginalValueSeedsTheBacking()
        => AssertSameAsLegacy(Program(
            "01 WS-A PIC X(6) VALUE \"SEEDED\".\n01 WS-B REDEFINES WS-A PIC X(3).",
            "    DISPLAY WS-A \"|\" WS-B."));   // SEEDED|SEE — only the original's VALUE inits (SR9)
}
