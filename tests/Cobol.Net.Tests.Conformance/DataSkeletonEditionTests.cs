// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The W2 data-skeleton × edition matrix (roadmap Phase 2; VERSION_TEST_MATRIX introduction invariants). Every
/// 2002-introduced data construct that COBOL.NET recognizes but does not yet implement — the FLOAT-SHORT family
/// (ISO §13.18.60) and the PICTURE symbol E (ISO §13.18.40.4 GR13b) — must NEVER compile silently: below 2002
/// the ConstructRegistry introduction gate rejects (COBOLNET0900 naming COBOL-2002); at 2002/2014/2023 the
/// COBOLNET0899 not-implemented error names the owning roadmap phase. Before this sweep each of these silently
/// misbound to USAGE DISPLAY / "pure numeric, zero digits". The constructs that have since gone LIVE (USAGE
/// OBJECT REFERENCE, USAGE POINTER, the BINARY-CHAR family, and — Phase 4a M2-DATA-3/4 — NATIONAL data
/// (PIC N / USAGE NATIONAL, §8.5.2.10) and BOOLEAN data (PIC 1 / USAGE BIT, §8.5.2.5)) keep only the
/// introduction edge: they compile at 2002+ and 0900 at 85.
/// </summary>
public sealed class DataSkeletonEditionTests
{
    /// <summary>A minimal program whose WORKING-STORAGE carries the construct under test.</summary>
    private static string Prog(string pid, string wsEntry) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {wsEntry}
        PROCEDURE DIVISION.
        MAIN-P.
            STOP RUN.
        """;

    /// <summary>The skeleton constructs: id-suffix, WS entry, owning roadmap phase (per ConstructRegistry).
    /// USAGE OBJECT REFERENCE left this set at the Phase-3 OO spine (LIVE); USAGE POINTER left it at Phase-4b
    /// increment 1 (LIVE, DEVLOG 613); the BINARY-CHAR family left it at Phase 4 M2-DATA-1 (LIVE, DEVLOG 614 —
    /// native fixed-width integers). Each keeps the <c>{is2002()}?</c>/W1.5 0900 edition-naming hint below 2002;
    /// the live ones are exercised positively (BinaryCharFamily_CompilesAt2002Plus_RejectedAt85).</summary>
    public static TheoryData<string, string, string> SkeletonConstructs() => new()
    {
        // NATIONAL/BOOLEAN data went LIVE at Phase 4a (M2-DATA-3/4) — PIC N / PIC 1 / USAGE BIT left this
        // set (the positive NationalData_/BooleanData_ facts below). The staged SUB-LEGS of the live rows
        // remain skeleton shapes: national-form numerics (§13.18.60.4 SR12) and NATIONAL-EDITED pictures
        // (§13.18.40.4 GR10) — 0899 "Phase 4a residue" at 2002+, 0900 at 85.
        { "NAT1", "01 WS-A PIC 9(4) USAGE NATIONAL.", "Phase 4a residue" },    // national-form numeric, SR12
        { "NED1", "01 WS-M PIC NN0NN.", "Phase 4a residue" },                  // national-edited, GR10
        // The FLOAT-SHORT/-LONG/-EXTENDED trio went LIVE at Phase 6a (D16) — see the FloatUsage_* positive facts;
        // the floating-point numeric-edited PICTURE (symbol E) went LIVE with data-model design D21 (kb/Work PB66) —
        // see FloatEditedPicture_CompilesAt2002Plus_RejectedAt85. (Its former skeleton row, PIC 9V99E+99, was itself
        // an illegal picture: §13.18.40.6 Table 10 admits no V before the E.)
    };

    /// <summary>At COBOL-85 every skeleton construct is a 2002 introduction: rejected with the COBOLNET0900
    /// introduction diagnostic NAMING COBOL-2002 (ISO §13.18.60 / §13.18.40; VERSION_TEST_MATRIX introduction
    /// invariant) — never the historical silent DISPLAY misbind.</summary>
    [Theory]
    [MemberData(nameof(SkeletonConstructs))]
    public void SkeletonConstruct_0900At85_NamingCobol2002(string pid, string wsEntry, string phase)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Prog("DSKA" + pid, wsEntry), 85);
        Assert.False(ok, $"{wsEntry} must be rejected at --std 85 (a 2002 introduction owned by {phase}; "
            + "ISO §13.18.60/§13.18.40)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0900");
        EditionHarness.AssertHasDiagnostic(errors, "COBOL-2002");
    }

    /// <summary>At 2002/2014/2023 the construct is LEGAL but unimplemented: the compile fails with the
    /// COBOLNET0899 not-implemented error naming the owning roadmap phase — never a silent misbind
    /// (ISO §13.18.60 / §13.18.40.4).</summary>
    [Theory]
    [MemberData(nameof(SkeletonConstructs))]
    public void SkeletonConstruct_NotImplementedErrorAt2002Plus_NamingOwningPhase(
        string pid, string wsEntry, string phase)
    {
        foreach (int edition in new[] { 2002, 2014, 2023 })
        {
            var (ok, errors, _) = EditionHarness.CompileFull(Prog("DSKB" + pid, wsEntry), edition);
            Assert.False(ok, $"{wsEntry} must NOT compile silently at --std {edition} (not yet implemented)");
            EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0899");
            EditionHarness.AssertHasDiagnostic(errors, "not yet implemented");
            EditionHarness.AssertHasDiagnostic(errors, phase);
        }
    }

    /// <summary>USAGE OBJECT REFERENCE went LIVE with the Phase-3 OO spine (ISO §13.18.60.4 / §8.5.2.14): a
    /// universal (class-less) reference item compiles at every 2002+ edition — a nullable <c>object?</c> field,
    /// COBOL initial state NULL — and stays introduction-gated at 85 (the <c>{is2002()}?</c> grammar hook +
    /// the W1.5 0900 edition-naming hint). The former recognized-but-unimplemented 0899 posture is retired.</summary>
    [Fact]
    public void UsageObjectReference_Universal_CompilesAt2002Plus_0900At85()
    {
        foreach (int edition in new[] { 2002, 2014, 2023 })
        {
            var (ok, errors, _) = EditionHarness.CompileFull(
                Prog("DSKOREF" + edition, "01 WS-O USAGE OBJECT REFERENCE."), edition);
            Assert.True(ok, $"a universal USAGE OBJECT REFERENCE item must compile at --std {edition}: "
                + string.Join("\n", errors));
        }
        var (ok85, errors85, _) = EditionHarness.CompileFull(Prog("DSKOREF85", "01 WS-O USAGE OBJECT REFERENCE."), 85);
        Assert.False(ok85, "USAGE OBJECT REFERENCE is a 2002 introduction — rejected at --std 85");
        EditionHarness.AssertHasDiagnostic(errors85, "COBOLNET0900");
        EditionHarness.AssertHasDiagnostic(errors85, "COBOL-2002");
    }

    /// <summary>PICTURE is prohibited with USAGE OBJECT REFERENCE (§13.18.60.4 — the item is picture-less);
    /// the conflict diagnoses COBOLNET0812, never an incoherent picture-with-reference classification.</summary>
    [Fact]
    public void UsageObjectReference_WithPicture_Rejects0812()
    {
        var (ok, errors, _) = EditionHarness.CompileFull(
            Prog("DSKOREFP", "01 WS-O PIC X(4) USAGE OBJECT REFERENCE."), 2002);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0812");
    }

    /// <summary>The floating-point numeric-edited PICTURE went LIVE with data-model design D21 (kb/Work PB66; ISO
    /// §13.18.40.4 GR13 b — a significand character-string and an exponent +9{1..4} joined by E; §14.6.8.4 the
    /// store): it compiles at every 2002+ edition and stays introduction-gated at 85 (COBOLNET0900 naming
    /// COBOL-2002 — registry row pic-external-float-2002). The former 0899 not-implemented posture is retired.</summary>
    [Theory]
    [InlineData("01 WS-EF PIC +9.99E+99.")]
    [InlineData("01 WS-EF PIC -9(3).9(2)E+999 VALUE 1.5E+3.")]
    [InlineData("01 WS-EF PIC 9(3)E+9.")]
    public void FloatEditedPicture_CompilesAt2002Plus_RejectedAt85(string wsEntry)
    {
        foreach (int edition in new[] { 2002, 2014, 2023 })
        {
            var (ok, errors, _) = EditionHarness.CompileFull(Prog("DSKFE" + edition, wsEntry), edition);
            // (a VALUE numeric literal on a numeric-edited item is itself a 2023 capability — VCR row 86 — so the
            // VALUE-bearing shape is checked at 2023 only)
            if (wsEntry.Contains("VALUE") && edition < 2023) continue;
            Assert.True(ok, $"a floating-point numeric-edited picture must compile at --std {edition}: {string.Join("\n", errors)}");
        }
        var (ok85, errors85, _) = EditionHarness.CompileFull(Prog("DSKFE85", wsEntry), 85);
        Assert.False(ok85, "the floating-point numeric-edited PICTURE is a 2002 introduction — rejected at --std 85");
        EditionHarness.AssertHasDiagnostic(errors85, "COBOLNET0900");
        EditionHarness.AssertHasDiagnostic(errors85, "COBOL-2002");
    }

    /// <summary>The BINARY-CHAR family went LIVE with Phase 4 M2-DATA-1 (ISO §13.18.60.4 GR12): a PICTURE-less
    /// fixed-width binary item compiles at every 2002+ edition (a native 1/2/4/8-byte integer) and stays
    /// introduction-gated at 85 (COBOLNET0900 naming COBOL-2002). The former 0899 not-implemented posture is
    /// retired.</summary>
    [Theory]
    [InlineData("01 WS-C BINARY-CHAR SIGNED.")]
    [InlineData("01 WS-D USAGE IS BINARY-CHAR SIGNED.")]
    [InlineData("01 WS-E USAGE BINARY-SHORT.")]
    [InlineData("01 WS-F USAGE BINARY-LONG UNSIGNED.")]
    [InlineData("01 WS-G USAGE BINARY-DOUBLE.")]
    public void BinaryCharFamily_CompilesAt2002Plus_RejectedAt85(string wsEntry)
    {
        foreach (int edition in new[] { 2002, 2014, 2023 })
        {
            var (ok, errors, _) = EditionHarness.CompileFull(Prog("DSKBIN" + edition, wsEntry), edition);
            Assert.True(ok, $"a fixed-width binary usage must compile at --std {edition}: {string.Join("\n", errors)}");
        }
        var (ok85, errors85, _) = EditionHarness.CompileFull(Prog("DSKBIN85", wsEntry), 85);
        Assert.False(ok85, "the BINARY-CHAR family is a 2002 introduction — rejected at --std 85");
        EditionHarness.AssertHasDiagnostic(errors85, "COBOLNET0900");
        EditionHarness.AssertHasDiagnostic(errors85, "COBOL-2002");
    }

    /// <summary>NATIONAL data went LIVE at Phase 4a M2-DATA-3 (ISO §8.5.2.10 / §13.18.40.4 GR9 /
    /// §13.18.60.4 SR13a): a PIC N item (usage implied NATIONAL) compiles at every 2002+ edition — one UTF-16
    /// char per national position (D-N1) — and stays introduction-gated at 85 (COBOLNET0900 naming COBOL-2002).
    /// The former 0899 not-implemented posture is retired.</summary>
    [Theory]
    [InlineData("01 WS-N PIC N(4).")]
    [InlineData("01 WS-N PIC N(4) USAGE NATIONAL.")]
    public void NationalData_CompilesAt2002Plus_RejectedAt85(string wsEntry)
    {
        foreach (int edition in new[] { 2002, 2014, 2023 })
        {
            var (ok, errors, _) = EditionHarness.CompileFull(Prog("DSKNAT" + edition, wsEntry), edition);
            Assert.True(ok, $"a national item must compile at --std {edition}: {string.Join("\n", errors)}");
        }
        var (ok85, errors85, _) = EditionHarness.CompileFull(Prog("DSKNAT85", wsEntry), 85);
        Assert.False(ok85, "national data is a 2002 introduction — rejected at --std 85");
        EditionHarness.AssertHasDiagnostic(errors85, "COBOLNET0900");
        EditionHarness.AssertHasDiagnostic(errors85, "COBOL-2002");
    }

    /// <summary>BOOLEAN data went LIVE at Phase 4a M2-DATA-4 (ISO §8.5.2.5 / §13.18.40.4 GR8/GR14 R14 /
    /// §13.18.60.4 SR5): a PIC 1 item (display- or bit-form — the same one-'0'/'1'-character-per-position
    /// representation, D-B1) compiles at every 2002+ edition and stays introduction-gated at 85. The former
    /// 0899 not-implemented posture is retired.</summary>
    [Theory]
    [InlineData("01 WS-B PIC 1(8).")]
    [InlineData("01 WS-B PIC 1(4) USAGE BIT.")]
    public void BooleanData_CompilesAt2002Plus_RejectedAt85(string wsEntry)
    {
        foreach (int edition in new[] { 2002, 2014, 2023 })
        {
            var (ok, errors, _) = EditionHarness.CompileFull(Prog("DSKBOOL" + edition, wsEntry), edition);
            Assert.True(ok, $"a boolean item must compile at --std {edition}: {string.Join("\n", errors)}");
        }
        var (ok85, errors85, _) = EditionHarness.CompileFull(Prog("DSKBOOL85", wsEntry), 85);
        Assert.False(ok85, "boolean data is a 2002 introduction — rejected at --std 85");
        EditionHarness.AssertHasDiagnostic(errors85, "COBOLNET0900");
        EditionHarness.AssertHasDiagnostic(errors85, "COBOL-2002");
    }

    /// <summary>The bare and full USAGE spellings bind IDENTICALLY (ISO §13.18.60 general format — the USAGE
    /// word is optional): bare <c>BINARY-CHAR SIGNED</c> historically string-glued to "BINARY-CHARSIGNED" and
    /// silently misbound to DISPLAY; now both PICTURE-less spellings COMPILE cleanly at 2002+ (Phase 4
    /// M2-DATA-1) — restoring the parity the string-glue bug broke.</summary>
    [Fact]
    public void BareAndFullBinaryChar_BindIdentically()
    {
        var (okBare, bareErrors, _) = EditionHarness.CompileFull(Prog("DSKBARE1", "01 WS-C BINARY-CHAR SIGNED."), 2023);
        var (okFull, fullErrors, _) = EditionHarness.CompileFull(Prog("DSKFULL1", "01 WS-C USAGE IS BINARY-CHAR SIGNED."), 2023);
        Assert.True(okBare, "bare BINARY-CHAR SIGNED must compile: " + string.Join("\n", bareErrors));
        Assert.True(okFull, "USAGE IS BINARY-CHAR SIGNED must compile: " + string.Join("\n", fullErrors));
    }

    /// <summary>PICTURE is prohibited with the BINARY-CHAR family (ISO §13.16.3 SR8 — the item is picture-less);
    /// the conflict diagnoses COBOLNET0870, never an incoherent picture-with-binary classification (mirrors the
    /// USAGE OBJECT REFERENCE 0812 rule).</summary>
    [Fact]
    public void BinaryChar_WithPicture_Rejects0870()
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Prog("DSKBINP", "01 WS-C PIC S9(4) USAGE BINARY-CHAR."), 2002);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0870");
    }

    /// <summary>A symbol outside the §13.18.40.3 SR2 PICTURE whitelist is an invalid PICTURE (COBOLNET0808) at
    /// EVERY edition — previously it fell through the classifier to "pure numeric, zero digits".</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void InvalidPictureSymbol_0808AtEveryEdition(int edition)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(
            Prog("DSKQ" + edition, "01 WS-Q PIC 9Q9."), edition);
        Assert.False(ok, $"PIC 9Q9 must be rejected at --std {edition} (ISO §13.18.40.3 SR2)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0808");
        EditionHarness.AssertHasDiagnostic(errors, "invalid PICTURE symbol");
    }

    /// <summary>A class definition went LIVE with the Phase-3 OO spine part 2 (ISO §11.2/§11.3 — the ClassUnit
    /// collection + pass-1 class symbol table): a class-only compilation unit COMPILES clean at every 2002+
    /// edition (the emitted module carries the class and an empty Main — a class translation unit with no
    /// program is legal, §10.6), and stays grammar-gated at 85 (the <c>{is2002()}?</c> hook + the W1.5 0900
    /// mapping). The former recognized-but-unimplemented 0899 posture (and before that the SILENT DROP the W2
    /// loud-guard sweep caught) is retired.</summary>
    [Fact]
    public void ClassDefinition_CompilesAt2002Plus_RejectedAt85()
    {
        const string cls = """
            IDENTIFICATION DIVISION.
            CLASS-ID. DSKCLS1.
            END CLASS DSKCLS1.
            """;
        foreach (int edition in new[] { 2002, 2014, 2023 })
        {
            var (ok, errors, _) = EditionHarness.CompileFull(cls, edition);
            Assert.True(ok, $"a class-only compilation unit must compile at --std {edition} (ISO §10.6): "
                + string.Join("\n", errors));
        }
        var (ok85, _, _) = EditionHarness.CompileFull(cls, 85);
        Assert.False(ok85, "a class definition must be rejected at --std 85 (grammar-gated OO/2002)");
    }

    /// <summary>The zero-regression leg: the COBOL-85 corpus symbol/usage repertoire still compiles clean at
    /// every edition — the whitelist and the explicit usage map must not reject anything the '85 classifier
    /// accepted (ISO §13.18.40.4 / §13.18.60).</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void EightyFiveRepertoire_StillCompilesClean(int edition)
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(Prog("DSKOK" + edition, """
            01 WS-X PIC X(10).
            01 WS-N9 PIC S9(4)V99 COMP-3.
            01 WS-ED PIC ZZ9.99.
            01 WS-CR PIC 9(5)CR.
            01 WS-ST PIC ****.
            01 WS-SL PIC 99/99/99.
            01 WS-BI PIC 9(8) BINARY.
            """), edition);
        Assert.True(ok, $"--std {edition}: {string.Join("; ", errors)}");
        EditionHarness.AssertNoDiagnostic(warnings, "COBOLNET0899");
    }
}
