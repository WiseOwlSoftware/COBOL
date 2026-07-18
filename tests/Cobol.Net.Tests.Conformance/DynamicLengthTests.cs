// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The DYNAMIC LENGTH elementary item (ISO §8.5.1.10 / §13.18.19, COBOL-2014; PHASE-12 wave 2): a variable-length,
/// minimum-length-zero PIC X/N string. The declaration-shape rules whose violation must be a LOUD bind-time
/// rejection, never a silent mis-compile (COBOLNET_DESIGN §1.4): §13.18.19.3 SR1 (PICTURE exactly one N or X →
/// COBOLNET1561), the dynamic-length-structure-name non-support (§13.18.19.3 SR2 / §12.3.7 → COBOLNET1562), and
/// §13.16.3 SR18 (only level-number/entry-name/PICTURE/USAGE/VALUE permitted → COBOLNET1563). The run behavior
/// (truncate-to-LIMIT, current-length MOVE, FUNCTION LENGTH) is the <c>dynamic_length_*</c> conformance corpus; the
/// edition-gating is the <c>dynamic-length-item-2014</c> version-matrix row — these assert the negative gating and
/// the well-formed positive.
/// </summary>
public sealed class DynamicLengthTests
{
    private static string Prog(string entry) => """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. DLG.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        """ + "\n" + entry + "\n" + """
        PROCEDURE DIVISION.
        MAIN-PARA.
            DISPLAY "X".
            STOP RUN.
        """;

    /// <summary>§13.18.19.3 SR1 — the PICTURE character-string shall be exactly one instance of 'N' or 'X'; a
    /// numeric PICTURE is a declaration error (COBOLNET1561), never a mis-typed variable-length item.</summary>
    [Fact]
    public void NumericPicture_Rejected1561()
    {
        var (ok, diag) = EditionHarness.Compile(Prog("01 WS-D PIC 9(3) DYNAMIC LENGTH."), 2014);
        Assert.False(ok, "a DYNAMIC LENGTH item with a numeric PICTURE must be rejected (ISO §13.18.19.3 SR1)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1561");
    }

    /// <summary>§13.18.19.3 SR1 — the boolean symbol '1' is NOT permitted (unlike ANY LENGTH); a dynamic-length
    /// item is alphanumeric or national only (§13.18.19.4 GR1). COBOLNET1561.</summary>
    [Fact]
    public void BooleanPicture_Rejected1561()
    {
        var (ok, diag) = EditionHarness.Compile(Prog("01 WS-D PIC 1 DYNAMIC LENGTH."), 2014);
        Assert.False(ok, "a DYNAMIC LENGTH item with a boolean PICTURE must be rejected (ISO §13.18.19.3 SR1)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1561");
    }

    /// <summary>§13.18.19.3 SR2 / §12.3.7 — a dynamic-length-structure-name references a SPECIAL-NAMES DYNAMIC LENGTH
    /// STRUCTURE (PREFIXED/DELIMITED/physical layout), unsupported today: staged loud (COBOLNET1562), never a
    /// silently-defaulted layout.</summary>
    [Fact]
    public void StructureName_Rejected1562()
    {
        var (ok, diag) = EditionHarness.Compile(Prog("01 WS-D PIC X DYNAMIC LENGTH MYSTRUCT LIMIT IS 10."), 2014);
        Assert.False(ok, "a DYNAMIC LENGTH clause naming a structure-name must be rejected (ISO §13.18.19.3 SR2)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1562");
    }

    /// <summary>§13.16.3 SR18 — with DYNAMIC LENGTH the only other clauses permitted are level-number, entry-name,
    /// PICTURE, USAGE, VALUE; OCCURS is excluded (COBOLNET1563).</summary>
    [Fact]
    public void OccursCoClause_Rejected1563()
    {
        var (ok, diag) = EditionHarness.Compile(Prog("01 WS-D PIC X DYNAMIC LENGTH OCCURS 3 TIMES."), 2014);
        Assert.False(ok, "OCCURS with DYNAMIC LENGTH must be rejected (ISO §13.16.3 SR18)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1563");
    }

    /// <summary>§13.16.3 SR18 — JUSTIFIED is likewise excluded (COBOLNET1563).</summary>
    [Fact]
    public void JustifiedCoClause_Rejected1563()
    {
        var (ok, diag) = EditionHarness.Compile(Prog("01 WS-D PIC X DYNAMIC LENGTH JUSTIFIED RIGHT."), 2014);
        Assert.False(ok, "JUSTIFIED with DYNAMIC LENGTH must be rejected (ISO §13.16.3 SR18)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1563");
    }

    /// <summary>§13.16.3 SR18 — GLOBAL is NOT a permitted co-clause; it is decoded post-build for ordinary items, so
    /// the SR18 guard must capture it explicitly (the P12 adversarial-review fix). COBOLNET1563.</summary>
    [Fact]
    public void GlobalCoClause_Rejected1563()
    {
        var (ok, diag) = EditionHarness.Compile(Prog("01 WS-D PIC X DYNAMIC LENGTH LIMIT IS 10 IS GLOBAL."), 2014);
        Assert.False(ok, "GLOBAL with DYNAMIC LENGTH must be rejected (ISO §13.16.3 SR18)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1563");
    }

    /// <summary>§13.18.19.3 SR1 — a count of 1 in ANY spelling is still ONE instance: <c>PIC X</c>, <c>X(1)</c>,
    /// <c>X(01)</c>, <c>N(001)</c> all denote one X/N position and must be ACCEPTED (the P12 review fix — the former
    /// raw-string match falsely rejected the explicit-count forms).</summary>
    [Theory]
    [InlineData("01 WS-D PIC X(1) DYNAMIC LENGTH.")]
    [InlineData("01 WS-D PIC X(01) DYNAMIC LENGTH LIMIT IS 10.")]
    [InlineData("01 WS-D PIC N(001) DYNAMIC LENGTH.")]
    public void CountOneForm_Accepted(string entry)
    {
        var (ok, diag) = EditionHarness.Compile(Prog(entry), 2014);
        Assert.True(ok, $"a count-1 PICTURE must be accepted for DYNAMIC LENGTH:\n{string.Join("\n", diag)}");
    }

    /// <summary>§13.18.19.3 SR1 — a count &gt; 1 (two positions) IS rejected (COBOLNET1561), confirming the count-1
    /// acceptance did not weaken the rule.</summary>
    [Theory]
    [InlineData("01 WS-D PIC XX DYNAMIC LENGTH.")]
    [InlineData("01 WS-D PIC X(2) DYNAMIC LENGTH.")]
    public void CountGreaterThanOne_Rejected1561(string entry)
    {
        var (ok, diag) = EditionHarness.Compile(Prog(entry), 2014);
        Assert.False(ok, "a multi-position PICTURE must be rejected for DYNAMIC LENGTH (ISO §13.18.19.3 SR1)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1561");
    }

    /// <summary>A well-formed DYNAMIC LENGTH item (PIC X, LIMIT, VALUE — all §13.16.3 SR18-permitted) compiles clean
    /// at COBOL-2014; the positive facts must NOT trip a shape guard.</summary>
    [Theory]
    [InlineData("01 WS-D PIC X DYNAMIC LENGTH LIMIT IS 20.")]
    [InlineData("01 WS-D PIC N DYNAMIC LENGTH.")]
    [InlineData("01 WS-D PIC X DYNAMIC LENGTH VALUE \"SEED\".")]
    public void WellFormed_CompilesAt2014(string entry)
    {
        var (ok, diag) = EditionHarness.Compile(Prog(entry), 2014);
        Assert.True(ok, $"a well-formed DYNAMIC LENGTH item must compile at 2014:\n{string.Join("\n", diag)}");
    }

    /// <summary>The COBOL-2014 introduction gate (§8.5.1.10 / §13.18.19): DYNAMIC LENGTH is rejected below 2014 with
    /// the edition-band COBOLNET0900 (VersionConformancePass ParseArm.VisitDynamicLengthClause).</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    public void BelowIntroduction_Rejected0900(int edition)
    {
        var (ok, diag) = EditionHarness.Compile(Prog("01 WS-D PIC X DYNAMIC LENGTH LIMIT IS 20."), edition);
        Assert.False(ok, $"DYNAMIC LENGTH must be rejected at COBOL-{edition} (introduced 2014)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET0900");
    }
}
