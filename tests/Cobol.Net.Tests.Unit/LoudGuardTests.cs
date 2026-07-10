// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Editions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The W2 loud-guard sweep at the unit level (roadmap Phase 2; architecture rec 2 — every silent misbind is a
/// wrong-answer bug): <see cref="PicInfo.ParseUsage"/> recognizes EVERY grammar-accepted usage keyword explicitly
/// (ISO §13.18.60 — the 2002 inventory previously fell through a catch-all to DISPLAY), and
/// <see cref="PicInfo.Analyze"/> enforces the §13.18.40.3 SR2 PICTURE symbol whitelist (the 2002+ symbols
/// N/1/E previously fell through to "pure numeric, zero digits").
/// <para><b>Edition-gate note (Step 14g.1):</b> the USAGE / PICTURE-category INTRODUCTION gates (national /
/// boolean / pointer / object-reference / binary-char-family / float-trio 2002 introductions) no longer fire at
/// the parse layer — they moved to the post-bind <c>VersionConformancePass</c> data-attribute enumerator (keyed
/// on the RESOLVED item), which fires on RECOGNITION once per source declaration. So the UNIT facts here assert
/// only <c>ParseUsage</c>'s MAPPING + <c>Analyze</c>'s CLASSIFICATION; the below-2002 rejection is verified by
/// the version matrix (usage-*-2002 rows) + the pipeline exact-count witnesses (<c>UsageDataEditionTests</c>).
/// The E-symbol external-float + national-edited PICTURE skeletons likewise moved their 0900 to <c>GateData</c> at
/// Step 14g.5 — carried on <c>PicInfo.SkeletonGate</c>; <c>Analyze</c> keeps only the ≥2002 not-implemented 0899.</para>
/// </summary>
public sealed class LoudGuardTests
{
    private static EditionContext Ed(int level) => new(level);

    // ── ParseUsage: the implemented set still maps cleanly (zero-regression, ISO §13.18.60) ────────────────

    [Theory]
    [InlineData(null, Usage.Display)]
    [InlineData("DISPLAY", Usage.Display)]
    [InlineData("COMP", Usage.Binary)]
    [InlineData("COMPUTATIONAL", Usage.Binary)]
    [InlineData("COMP-4", Usage.Binary)]
    [InlineData("BINARY", Usage.Binary)]
    [InlineData("COMP-3", Usage.Packed)]
    [InlineData("COMPUTATIONAL-3", Usage.Packed)]
    [InlineData("PACKED-DECIMAL", Usage.Packed)]
    [InlineData("COMP-5", Usage.Comp5)]
    [InlineData("COMP-1", Usage.Float)]
    [InlineData("COMP-2", Usage.Double)]
    [InlineData("INDEX", Usage.Index)]
    public void ParseUsage_ImplementedKeywords_MapWithoutDiagnostics(string? keyword, Usage expected)
    {
        var ed = Ed(85);
        Assert.Equal(expected, PicInfo.ParseUsage(keyword, ed, "data item 'T'"));
        Assert.False(ed.HasErrors);
        Assert.Empty(ed.Warnings);
    }

    /// <summary>Every 2002-introduced USAGE keyword maps to its OWN <see cref="Usage"/> member with NO parse-layer
    /// diagnostic at ANY edition (the introduction gate moved to <c>VersionConformancePass</c>, Step 14g.1 — the
    /// mapping itself is edition-invariant). OBJECT REFERENCE / the BINARY-CHAR family / the float trio /
    /// NATIONAL / BIT / POINTER (ISO §13.18.60.4).</summary>
    [Theory]
    [InlineData("OBJECT REFERENCE", Usage.ObjectReference)]
    [InlineData("BINARY-CHAR", Usage.BinaryChar)]
    [InlineData("BINARY-SHORT", Usage.BinaryShort)]
    [InlineData("BINARY-LONG", Usage.BinaryLong)]
    [InlineData("BINARY-DOUBLE", Usage.BinaryDouble)]
    [InlineData("FLOAT-SHORT", Usage.FloatShort)]
    [InlineData("FLOAT-LONG", Usage.FloatLong)]
    [InlineData("FLOAT-EXTENDED", Usage.FloatExtended)]
    [InlineData("NATIONAL", Usage.National)]
    [InlineData("BIT", Usage.Bit)]
    [InlineData("POINTER", Usage.Pointer)]
    public void ParseUsage_Post85Keywords_MapCleanly_NoParseLayerGate(string keyword, Usage expected)
    {
        foreach (int level in new[] { 85, 2002, 2014, 2023 })
        {
            var ed = Ed(level);
            Assert.Equal(expected, PicInfo.ParseUsage(keyword, ed, "data item 'T'"));
            // NO parse-layer diagnostic at any edition — the below-2002 introduction gate is the pass's job now.
            Assert.False(ed.HasErrors, $"USAGE {keyword} @ {level}: ParseUsage must be gate-free (14g.1): "
                + string.Join("; ", ed.Diagnostics));
        }
    }

    /// <summary>An unrecognized keyword (a compiler defect — the grammar admits nothing outside the map) is a
    /// loud internal error, never a silent Display misbind (ISO §13.18.60).</summary>
    [Fact]
    public void ParseUsage_UnrecognizedKeyword_LoudInternalError()
    {
        var ed = Ed(2023);
        PicInfo.ParseUsage("BINARY-CHARSIGNED", ed, "data item 'T'");   // the historical glued-text shape
        Assert.True(ed.HasErrors);
        Assert.Contains(ed.Diagnostics, d => d.Contains("unrecognized USAGE keyword"));
    }

    // ── Analyze: the §13.18.40.3 SR2 whitelist — N / 1 / E gate loud; anything else is invalid ─────────────

    /// <summary>The external-float PICTURE symbol E (ISO §13.18.40.4 GR13b) — its introduction gate (COBOLNET0900)
    /// MOVED to the post-bind <c>VersionConformancePass</c> GateData enumerator (Step 14g.5), carried on the recovered
    /// item's <c>PicInfo.SkeletonGate</c> (the category is recovered to Alphanumeric, erasing the parse identity). So
    /// <c>Analyze</c> at 85 emits NO picture-layer diagnostic — it only STAMPS the gate; the 0900 firing is verified in
    /// <c>RepositoryPrototypeEditionTests</c> + the version matrix. (The national/boolean category gates moved the same
    /// way at 14g.1 — <c>UsageDataEditionTests</c>.)</summary>
    [Fact]
    public void Analyze_ExternalFloat_At85_CarriesSkeletonGate()
    {
        var ed = Ed(85);
        var pic = PicInfo.Analyze("9V99E+99", Usage.Display, ed, "data item 'T'");
        Assert.False(ed.HasErrors, "the 0900 moved to GateData (14g.5) — Analyze is silent at the picture layer below 2002");
        Assert.Equal(Constructs.PicExternalFloat2002, pic.SkeletonGate);
    }

    [Theory]
    // N(4) and 1(8) left this set at Phase 4a M2-DATA-3/4 (LIVE — the positive facts below); the external
    // float symbol E stays a Phase-6 skeleton.
    [InlineData("9V99E+99")]
    public void Analyze_2002Symbol_NotImplementedErrorAt2023(string picture)
    {
        var ed = Ed(2023);
        PicInfo.Analyze(picture, Usage.Display, ed, "data item 'T'");
        Assert.True(ed.HasErrors, $"PIC {picture} must not classify silently (§13.18.40.4 — no implementation yet)");
        Assert.Contains(ed.Diagnostics, d => d.Contains("COBOLNET0899") && d.Contains("not yet implemented"));
    }

    /// <summary>PIC N classifies category NATIONAL with the SR13a IMPLIED usage NATIONAL (no USAGE clause;
    /// ISO §13.18.60.4 SR13a / §13.18.40.4 GR9) — silent at 2002+, one character position per N.</summary>
    [Fact]
    public void Analyze_PicN_ClassifiesNational_UsageImplied()
    {
        var ed = Ed(2002);
        var pic = PicInfo.Analyze("N(4)", Usage.Display, ed, "data item 'T'");
        Assert.False(ed.HasErrors, string.Join("; ", ed.Diagnostics));
        Assert.Equal(PicCategory.National, pic.Category);
        Assert.Equal(Usage.National, pic.Usage);
        Assert.Equal(4, pic.Length);
    }

    /// <summary>PIC 1 without a USAGE clause classifies category BOOLEAN at usage DISPLAY (ISO §13.18.60.4
    /// SR13b / §13.18.40.4 GR8) — silent at 2002+, one boolean position per 1; PIC 1 USAGE BIT takes
    /// <see cref="Usage.Bit"/> over the SAME representation (GR14 R14, D-B1).</summary>
    [Fact]
    public void Analyze_Pic1_ClassifiesBoolean_UsageDisplay()
    {
        var ed = Ed(2002);
        var pic = PicInfo.Analyze("1(8)", Usage.Display, ed, "data item 'T'");
        Assert.False(ed.HasErrors, string.Join("; ", ed.Diagnostics));
        Assert.Equal(PicCategory.Boolean, pic.Category);
        Assert.Equal(Usage.Display, pic.Usage);
        Assert.Equal(8, pic.Length);

        var edBit = Ed(2002);
        var bit = PicInfo.Analyze("1(4)", Usage.Bit, edBit, "data item 'T'", explicitUsage: true);
        Assert.False(edBit.HasErrors, string.Join("; ", edBit.Diagnostics));
        Assert.Equal(PicCategory.Boolean, bit.Category);
        Assert.Equal(Usage.Bit, bit.Usage);
    }

    /// <summary>The SR20/SR5/SR12 usage×picture conformance shapes (ISO §13.18.60.4): PIC N with an explicit
    /// non-NATIONAL usage and USAGE BIT with a non-boolean picture are declaration errors (0881); the
    /// SR12-legal national FORMS (numeric/boolean pictures under NATIONAL) stage 0899.</summary>
    [Fact]
    public void Analyze_UsagePictureConformance_0881And0899Shapes()
    {
        var ed = Ed(2002);
        PicInfo.Analyze("N(4)", Usage.Display, ed, "data item 'T'", explicitUsage: true);   // SR20
        Assert.Contains(ed.Diagnostics, d => d.Contains("COBOLNET0881"));

        var ed2 = Ed(2002);
        PicInfo.Analyze("X(4)", Usage.Bit, ed2, "data item 'T'", explicitUsage: true);      // SR5
        Assert.Contains(ed2.Diagnostics, d => d.Contains("COBOLNET0881"));

        var ed3 = Ed(2002);
        PicInfo.Analyze("9(4)", Usage.National, ed3, "data item 'T'", explicitUsage: true); // SR12 staged
        Assert.Contains(ed3.Diagnostics, d => d.Contains("COBOLNET0899") && d.Contains("national-form numeric"));

        var ed4 = Ed(2002);
        PicInfo.Analyze("1(4)", Usage.National, ed4, "data item 'T'", explicitUsage: true); // SR12 staged
        Assert.Contains(ed4.Diagnostics, d => d.Contains("COBOLNET0899") && d.Contains("national-form boolean"));
    }

    /// <summary>A symbol outside the §13.18.40.3 SR2 set is an invalid PICTURE (COBOLNET0808) at every
    /// edition — previously it fell through to "pure numeric, zero digits".</summary>
    [Theory]
    [InlineData("9Q9", 'Q')]
    [InlineData("9R", 'R')]     // a lone 'R' — legal only as the second half of the CR pair (SR12 NOTE 2)
    [InlineData("D9", 'D')]     // a lone 'D' — legal only opening the DB pair
    public void Analyze_InvalidSymbol_0808AtEveryEdition(string picture, char bad)
    {
        foreach (int level in new[] { 85, 2002, 2014, 2023 })
        {
            var ed = Ed(level);
            PicInfo.Analyze(picture, Usage.Display, ed, "data item 'T'");
            Assert.True(ed.HasErrors, $"PIC {picture} must be rejected at COBOL-{level} (§13.18.40.3 SR2)");
            Assert.Contains(ed.Diagnostics, d => d.Contains("COBOLNET0808") && d.Contains($"'{bad}'"));
        }
    }

    // ── Analyze: zero-regression classification facts (the '85 corpus symbol repertoire, §13.18.40.4) ──────

    [Fact]
    public void Analyze_Alphanumeric_Unchanged()
    {
        var ed = Ed(85);
        var pic = PicInfo.Analyze("X(4)", Usage.Display, ed, "data item 'T'");
        Assert.False(ed.HasErrors);
        Assert.Equal(PicCategory.Alphanumeric, pic.Category);
        Assert.Equal(4, pic.Length);
    }

    [Fact]
    public void Analyze_SignedScaledNumeric_Unchanged()
    {
        var ed = Ed(85);
        var pic = PicInfo.Analyze("S9(4)V99", Usage.Display, ed, "data item 'T'");
        Assert.False(ed.HasErrors);
        Assert.Equal(PicCategory.Numeric, pic.Category);
        Assert.Equal(6, pic.Digits);
        Assert.Equal(2, pic.Scale);
        Assert.True(pic.Signed);
    }

    /// <summary>CR/DB are single two-character symbols (§13.18.40.3 SR12 NOTE 2) — the whitelist scan must
    /// consume the pair, and the classification stays NUMERIC-EDITED (NC104A MOVE-TEST-F1-14).</summary>
    [Theory]
    [InlineData("9(5)CR")]
    [InlineData("9(5)DB")]
    public void Analyze_CrDbPairs_StillNumericEdited(string picture)
    {
        var ed = Ed(85);
        var pic = PicInfo.Analyze(picture, Usage.Display, ed, "data item 'T'");
        Assert.False(ed.HasErrors);
        Assert.Equal(PicCategory.NumericEdited, pic.Category);
    }

    /// <summary>A custom currency PICTURE symbol (ISO §12.3.7 GR13 — NC107A's <c>W</c>, NC108M's <c>&lt;</c>)
    /// stays legal and classifies NUMERIC-EDITED exactly as before the whitelist landed (§13.18.40.4).</summary>
    [Theory]
    [InlineData("WWWW9", 'W')]
    [InlineData("<<<<9", '<')]
    public void Analyze_CustomCurrencySymbol_StillNumericEdited(string picture, char currency)
    {
        var ed = Ed(85);
        var pic = PicInfo.Analyze(picture, Usage.Display, ed, "data item 'T'", currency: currency);
        Assert.False(ed.HasErrors);
        Assert.Equal(PicCategory.NumericEdited, pic.Category);
    }

    [Theory]
    [InlineData("ZZ9.99")]
    [InlineData("$$$9")]
    [InlineData("****")]
    [InlineData("+999")]
    [InlineData("B(3)X(5)")]
    [InlineData("P(4)9")]
    [InlineData("A(6)")]
    [InlineData("99/99/99")]
    public void Analyze_85CorpusShapes_NoDiagnostics(string picture)
    {
        var ed = Ed(85);
        PicInfo.Analyze(picture, Usage.Display, ed, "data item 'T'");
        Assert.False(ed.HasErrors, string.Join("; ", ed.Diagnostics));
        Assert.Empty(ed.Warnings);
    }

    // ── The skeleton enum members fail LOUD if a storage-mapping switch is ever reached (item-4 audit) ─────

    /// <summary>By construction nothing creates a PicInfo carrying a skeleton usage/category (the bind-time
    /// gates reject first, ISO §13.18.60/§13.18.40) — if a future phase starts constructing them without
    /// implementing the storage mapping, every derived member throws instead of silently defaulting.</summary>
    [Fact]
    public void FloatTrio_StorageMappingMembers_Live()
    {
        // The float trio LEFT the skeleton set at Phase 6a (LIVE, D16): the storage-mapping members now answer the
        // native IEEE shapes (float/double, 0f/0d) instead of throwing the skeleton guard. IsUnimplementedSkeleton
        // is now always false — no USAGE is a skeleton — so the loud guard shell is retained only for the 6b family.
        var single = PicInfo.FloatItem(Usage.FloatShort);
        Assert.Equal("float", single.ClrType);
        Assert.Equal("0f", single.DefaultInitializer);
        Assert.True(single.IsFloat);
        Assert.True(single.IsSingle);
        foreach (var u in new[] { Usage.FloatLong, Usage.FloatExtended })
        {
            var dbl = PicInfo.FloatItem(u);
            Assert.Equal("double", dbl.ClrType);
            Assert.Equal("0d", dbl.DefaultInitializer);
            Assert.True(dbl.IsFloat);
            Assert.False(dbl.IsSingle);
        }
    }

    /// <summary>National/boolean LEFT the skeleton set at Phase 4a (M2-DATA-3/4): the storage-mapping members
    /// now answer the LIVE shapes — a C# string field, national space / boolean-zero defaults (D-N1/D-B1;
    /// §13.18.63) — instead of throwing the skeleton guard.</summary>
    [Fact]
    public void NationalBoolean_StorageMappingMembers_Live()
    {
        var national = new PicInfo(PicCategory.National, Usage.National, Length: 4, Digits: 0, Scale: 0, Signed: false);
        Assert.Equal("string", national.ClrType);
        Assert.Equal("new string(' ', 4)", national.DefaultInitializer);
        var boolean = new PicInfo(PicCategory.Boolean, Usage.Bit, Length: 8, Digits: 0, Scale: 0, Signed: false);
        Assert.Equal("string", boolean.ClrType);
        Assert.Equal("new string('0', 8)", boolean.DefaultInitializer);
    }
}
