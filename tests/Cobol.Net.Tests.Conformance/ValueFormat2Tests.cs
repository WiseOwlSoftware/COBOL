// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The Format 2 (table) VALUE clause (ISO/IEC 1989:2023 §13.18.63.2, COBOL-2002): a literal list keyed to occurrence
/// subscripts by a MANDATORY FROM (subscript) phrase with an optional TO. Per-occurrence initialization — the
/// odometer fill (GR12), cyclic literal reuse under TO (GR13), no-TO = fill to the maximum (GR14), later-FROM-wins on
/// overlap (GR15), and the dynamic-capacity computation (GR16). LANDABLE scope: a single-dimension table on its own
/// OCCURS entry (fixed or dynamic); a multi-dimension odometer or a subordinate-item table VALUE is staged loud
/// (COBOLNET0899, P14 GAP). The paired glued-multi-literal reject (COBOLNET1585) closes the Format-1 gluing defect.
/// Per §13.18.63.4 the initial value of occurrences OUTSIDE every FROM..TO range is UNDEFINED — never asserted here.
/// </summary>
public sealed class ValueFormat2Tests
{
    private static string Prog(string ws, string body) => """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. VF2.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        """ + "\n" + ws + "\n" + """
        PROCEDURE DIVISION.
        MAIN-PARA.
        """ + "\n" + body + "\n" + """
            STOP RUN.
        """;

    // ── Fixed tables ──

    /// <summary>GR14 (no TO = fill to the maximum) + GR13 (cyclic reuse): six occurrences, two literals ⇒
    /// AAA BBB AAA BBB AAA BBB.</summary>
    [Fact]
    public void FixedTable_NoTo_CyclicFill()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 T-GRP.\n   03 E PIC X(3) OCCURS 6 VALUES ARE \"AAA\" \"BBB\" FROM (1).",
                 "    DISPLAY \"[\" E(1) E(2) E(3) E(4) E(5) E(6) \"]\"."), 2023);
        Assert.True(ok, detail);
        Assert.Contains("[AAABBBAAABBBAAABBB]", stdout);
    }

    /// <summary>GR12/GR13 explicit sub-range: only occurrences 2..4 are keyed (AA,BB,AA cyclic); occurrences outside
    /// the range are NOT asserted (§13.18.63.4 leaves them undefined).</summary>
    [Fact]
    public void FixedTable_ExplicitRange()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 T-GRP.\n   03 E PIC X(2) OCCURS 5 VALUES ARE \"AA\",\"BB\" FROM (2) TO (4).",
                 "    DISPLAY \"[\" E(2) E(3) E(4) \"]\"."), 2023);
        Assert.True(ok, detail);
        Assert.Contains("[AABBAA]", stdout);
    }

    /// <summary>GR15 (later FROM wins on overlap): "A" over 1..4 then "Z" over 2..3 ⇒ A Z Z A.</summary>
    [Fact]
    public void FixedTable_LastFromWins()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 T-GRP.\n   03 E PIC X OCCURS 4 VALUES ARE \"A\" FROM (1) TO (4) \"Z\" FROM (2) TO (3).",
                 "    DISPLAY \"[\" E(1) E(2) E(3) E(4) \"]\"."), 2023);
        Assert.True(ok, detail);
        Assert.Contains("[AZZA]", stdout);
    }

    /// <summary>A numeric element table: N(1..3) = 1,2,3 (GR12 sequential).</summary>
    [Fact]
    public void FixedTable_NumericElement()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 T-GRP.\n   03 N PIC 9 OCCURS 3 VALUES ARE 1 2 3 FROM (1).",
                 "    DISPLAY \"[\" N(1) N(2) N(3) \"]\"."), 2023);
        Assert.True(ok, detail);
        Assert.Contains("[123]", stdout);
    }

    // ── Dynamic-capacity table (Annex D.3.7) ──

    /// <summary>GR16a + Annex D.3.7: <c>OCCURS DYNAMIC FROM 1 TO 20 VALUES ARE "Leeds","Bordeaux","Pisa" FROM (1) TO
    /// (3)</c> opens the table at initial capacity 3 with the three names present — the ledger reject-valid-input
    /// proof (this exact clause was a raw parse error before).</summary>
    [Fact]
    public void DynamicTable_AnnexD37()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 T-GRP.\n   03 TOWN-NAME PIC X(9) OCCURS DYNAMIC FROM 1 TO 20\n"
               + "      VALUES ARE \"Leeds\" \"Bordeaux\" \"Pisa\" FROM (1) TO (3).",
                 "    DISPLAY \"[\" TOWN-NAME(1) \"|\" TOWN-NAME(2) \"|\" TOWN-NAME(3) \"]\"."), 2023);
        Assert.True(ok, detail);
        Assert.Contains("[Leeds    |Bordeaux |Pisa     ]", stdout);
    }

    // ── The glued-multi-literal reject (the paired defect fix) ──

    /// <summary>A Format-1 data-item VALUE takes exactly ONE literal (§13.18.63.2); a bare list with no FROM was
    /// silently GLUED into one corrupt value. Now rejected loud (COBOLNET1585).</summary>
    [Theory]
    [InlineData("01 W PIC 9(3) VALUE 1 2 3.")]
    [InlineData("01 W PIC XX VALUE \"A\" \"B\".")]
    public void GluedMultiLiteral_Rejected1585(string ws)
    {
        var (ok, diag) = EditionHarness.Compile(Prog(ws, "    DISPLAY \"X\"."), 2023);
        Assert.False(ok, "a bare multi-literal Format-1 VALUE must be rejected (ISO §13.18.63.2)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1585");
    }

    /// <summary>A single-literal Format-1 VALUE still compiles clean (the reject did not over-fire).</summary>
    [Fact]
    public void SingleLiteralValue_StillCompiles()
    {
        var (ok, diag) = EditionHarness.Compile(Prog("01 W PIC 9(3) VALUE 42.", "    DISPLAY W."), 2023);
        Assert.True(ok, $"a single-literal VALUE must still compile:\n{string.Join("\n", diag)}");
    }

    // ── Syntax rules ──

    /// <summary>SR20 — a FROM subscript beyond the table maximum is rejected (COBOLNET1586).</summary>
    [Fact]
    public void SubscriptOutOfRange_Rejected1586()
    {
        var (ok, diag) = EditionHarness.Compile(
            Prog("01 T-GRP.\n   03 E PIC X OCCURS 3 VALUE \"A\" FROM (4).", "    DISPLAY \"X\"."), 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1586");
    }

    /// <summary>SR21 — a TO subscript less than its FROM is rejected (COBOLNET1587).</summary>
    [Fact]
    public void ToLessThanFrom_Rejected1587()
    {
        var (ok, diag) = EditionHarness.Compile(
            Prog("01 T-GRP.\n   03 E PIC X OCCURS 5 VALUES ARE \"A\" \"B\" FROM (4) TO (2).", "    DISPLAY \"X\"."), 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1587");
    }

    /// <summary>A multi-dimension table VALUE (a subscript per nested OCCURS) is a documented P14 GAP — recognized,
    /// bound, then staged loud (COBOLNET0899).</summary>
    [Fact]
    public void MultiDimension_StagedGap0899()
    {
        var (ok, diag) = EditionHarness.Compile(
            Prog("01 T-GRP.\n   03 R OCCURS 2.\n      05 C PIC 9 OCCURS 3 VALUES ARE 1 2 3 4 5 6 FROM (1 1) TO (2 3).",
                 "    DISPLAY \"X\"."), 2023);
        Assert.False(ok, "a multi-dimension table VALUE is a P14 GAP (COBOLNET0899)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET0899");
    }

    // ── Edition gating ──

    /// <summary>The COBOL-2002 introduction gate (§13.18.63.2 Format 2): a table VALUE is rejected below 2002 with
    /// COBOLNET0900 (VersionConformancePass ParseArm.VisitValueClause).</summary>
    [Fact]
    public void BelowIntroduction_Rejected0900()
    {
        var (ok, diag) = EditionHarness.Compile(
            Prog("01 T-GRP.\n   03 E PIC X(3) OCCURS 6 VALUES ARE \"AAA\" \"BBB\" FROM (1).", "    DISPLAY \"X\"."), 85);
        Assert.False(ok, "the Format 2 table VALUE must be rejected at COBOL-85 (introduced 2002)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET0900");
    }

    // ── The ALL-FORMATS literal screen (kb/Work PB208) ──

    private static string Format1(string pic, string literal) => Prog($"01 W {pic} VALUE {literal}.", "    DISPLAY \"X\".");

    private static string Format2(string pic, string literal) =>
        Prog($"01 T-GRP.\n   03 W {pic} OCCURS 2 VALUE {literal} FROM (1) TO (2).", "    DISPLAY \"X\".");

    private static IReadOnlyList<string> Codes(IEnumerable<string> diagnostics) =>
        [.. diagnostics.SelectMany(d => Regex.Matches(d, @"COBOLNET\d{4}").Select(m => m.Value)).Distinct().Order()];

    /// <summary>⛔ THE DRIFT TEST FOR "ONE SCREEN, EVERY FORMAT" (kb/Work PB208). §13.18.63.3 SR2 is an ALL FORMATS
    /// rule — "If the category of the subject of the entry is numeric, all literals in the VALUE clause shall be
    /// numeric and shall be permissible values within the range indicated by the PICTURE clause or the USAGE
    /// clause" — and SR16 carries SRs 10–15 into format 2 as well, so a literal FORMAT 1 rejects, FORMAT 2 shall
    /// reject, with the same verdict. The two lanes used to disagree completely: the format-1 literal went through
    /// DataBinder's screen and BuildTableValueSpecs' per-occurrence literals went straight to the emitter, so
    /// <c>PIC 9(4) COMP VALUE "0012"</c> was COBOLNET1657 while
    /// <c>PIC 9(4) COMP OCCURS 2 VALUE "0012" FROM (1) TO (2)</c> compiled CLEAN at strict 2023 and seeded zeros.
    /// <para>This compares the two lanes' VERDICTS rather than asserting one code, which is the point: a rule added
    /// to the funnel (<c>DataBinder.ScreenValueLiteral</c>) reaches both formats by construction, and a future rule
    /// wired into only one of them reds HERE without anyone having to remember to extend a list. Both the strict
    /// and the --permissive axis are compared — the permissive REWRITE (a class-mismatched literal stored as the
    /// number) has to reach the emitter's per-occurrence override exactly as it reaches item.RawValue.</para></summary>
    [Theory]
    [InlineData("PIC 9(4) COMP", "\"0012\"")]   // SR2 class — an alphanumeric literal on a numeric item (byte form)
    [InlineData("PIC 99", "\"7\"")]             // SR2 class — the same on a zoned DISPLAY item
    [InlineData("PIC 99", "12345")]             // SR2 range — not representable without truncating nonzero digits
    [InlineData("PIC 9(4)", "-1")]              // SR3 — a signed literal on an unsigned subject
    [InlineData("PIC XX", "42")]                // SR4 — a numeric literal on an alphanumeric item
    [InlineData("PIC 99", "SPACE")]             // SR2 — a character figurative on a numeric item
    [InlineData("PIC 99", "12")]                // legal on BOTH — the screen must not over-fire
    [InlineData("PIC XX", "\"AB\"")]            // legal on BOTH
    [InlineData("PIC 99", "ZERO")]              // legal on BOTH — the figurative ZERO is numeric
    public void Format1AndFormat2_ScreenTheSameLiteralAlike(string pic, string literal)
    {
        var strict1 = Codes(EditionHarness.GetDiagnostics(Format1(pic, literal), 2023));
        var strict2 = Codes(EditionHarness.GetDiagnostics(Format2(pic, literal), 2023));
        Assert.Equal(strict1, strict2);

        var perm1 = EditionHarness.CompileFull(Format1(pic, literal), 2023, permissive: true);
        var perm2 = EditionHarness.CompileFull(Format2(pic, literal), 2023, permissive: true);
        Assert.Equal(Codes([.. perm1.Errors, .. perm1.Warnings]), Codes([.. perm2.Errors, .. perm2.Warnings]));
        Assert.Equal(perm1.Ok, perm2.Ok);
    }

    /// <summary>SR2 is EDITION-INDEPENDENT, so the format-2 screen fires at all four (§13.18.63.3 SR2 carries no
    /// version proviso). At COBOL-85 the COBOLNET0900 introduction gate fires as well — both, not either.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void Format2Sr2Screen_FiresAtEveryEdition(int edition)
    {
        var (ok, diag) = EditionHarness.Compile(Format2("PIC 9(4) COMP", "\"0012\""), edition);
        Assert.False(ok, "an alphanumeric literal on a numeric item is a §13.18.63.3 SR2 violation at every edition");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1657");
        if (edition == 85) EditionHarness.AssertHasDiagnostic(diag, "COBOLNET0900");
    }

    /// <summary>⛔ THE FORMAT 2 VALUE ON THE CHARACTER-IMAGE STORAGE LANE (kb/Work PB208 half 2), in the fast gate;
    /// the byte-level pin is <c>conformance:2023/pb208_table_value_image_seed</c>. GroupImageCodec.ImageInitOf —
    /// THE seeder for every image-stored backing since the PB164 consolidation — read only <c>item.RawValue</c>,
    /// which is null for a table VALUE, and its <c>StrRepeat(one, Occurs)</c> then seeded every occurrence with the
    /// VALUE-LESS default, DISCARDING the table VALUE. The REDEFINES alias is what puts B on that lane
    /// (§13.18.63.3 SR12 bars a VALUE in the redefinING entry, never in the redefined one, so this is conforming).
    /// §13.18.63.4 GR12 initializes occurrence 1 to literal-1 and GR13 reuses it for occurrence 2.</summary>
    [Fact]
    public void ImageStoredLeaf_TakesItsTableValue()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 G.\n   05 A PIC X(2) VALUE \"AA\".\n"
               + "   05 B PIC 9(4) COMP OCCURS 2 VALUE 12 FROM (1) TO (2).\n"
               + "   05 C PIC X(2) VALUE \"CC\".\n01 R REDEFINES G PIC X(8).",
                 "    DISPLAY \"[\" B(1) \"][\" B(2) \"][\" R(7:2) \"]\"."), 2023);
        Assert.True(ok, detail);
        Assert.Contains("[0012][0012][CC]", stdout);
    }

    /// <summary>The --permissive REWRITE reaches the emitter's PER-OCCURRENCE override, not just item.RawValue: a
    /// digits-only alphanumeric literal on a numeric item is read AS the numeric literal SR2 asked for (the CCVS
    /// leniency), so the image-stored table seeds the NUMBER — and warns rather than errors.</summary>
    [Fact]
    public void ImageStoredLeaf_PermissiveRewriteReachesEveryOccurrence()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 G.\n   05 A PIC X(2) VALUE \"AA\".\n"
               + "   05 B PIC 9(4) COMP OCCURS 2 VALUE \"0012\" FROM (1) TO (2).\n"
               + "   05 C PIC X(2) VALUE \"CC\".\n01 R REDEFINES G PIC X(8).",
                 "    DISPLAY \"[\" B(1) \"][\" B(2) \"][\" R(7:2) \"]\"."), 2023, permissive: true);
        Assert.True(ok, detail);
        Assert.Contains("[0012][0012][CC]", stdout);
    }
}
