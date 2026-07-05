// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The W2 loud-guard sweep at the unit level (roadmap Phase 2; architecture rec 2 — every silent misbind is a
/// wrong-answer bug): <see cref="PicInfo.ParseUsage"/> recognizes EVERY grammar-accepted usage keyword explicitly
/// (ISO §13.18.60 — the 2002 inventory previously fell through a catch-all to DISPLAY), and
/// <see cref="PicInfo.Analyze"/> enforces the §13.18.40.3 SR2 PICTURE symbol whitelist (the 2002+ symbols
/// N/1/E previously fell through to "pure numeric, zero digits"). The skeleton enum members themselves are
/// guarded: any storage-mapping access throws rather than silently defaulting.
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

    /// <summary>USAGE OBJECT REFERENCE went LIVE with the Phase-3 OO spine: only the introduction gate
    /// remains — Usage.ObjectReference, silent at 2002+, COBOLNET0900 naming COBOL-2002 at 85 (the registry
    /// row usage-object-reference-2002; ISO §13.18.60.4).</summary>
    [Fact]
    public void ParseUsage_ObjectReference_LiveAt2002_IntroductionGatedAt85()
    {
        var ok = Ed(2002);
        Assert.Equal(Usage.ObjectReference, PicInfo.ParseUsage("OBJECT REFERENCE", ok, "data item 'T'"));
        Assert.False(ok.HasErrors);
        var ed85 = Ed(85);
        Assert.Equal(Usage.ObjectReference, PicInfo.ParseUsage("OBJECT REFERENCE", ed85, "data item 'T'"));
        Assert.True(ed85.HasErrors);
        Assert.Contains(ed85.Diagnostics, d => d.Contains("COBOLNET0900") && d.Contains("COBOL-2002"));
    }

    // ── ParseUsage: the 2002+ skeleton inventory is NEVER silent (ISO §13.18.60 general format) ────────────

    [Theory]
    [InlineData("NATIONAL")]
    [InlineData("BIT")]
    [InlineData("POINTER")]
    [InlineData("BINARY-CHAR")]
    [InlineData("BINARY-SHORT")]
    [InlineData("BINARY-LONG")]
    [InlineData("BINARY-DOUBLE")]
    [InlineData("FLOAT-SHORT")]
    [InlineData("FLOAT-LONG")]
    [InlineData("FLOAT-EXTENDED")]
    public void ParseUsage_SkeletonKeyword_0900At85_NamingCobol2002(string keyword)
    {
        var ed = Ed(85);
        PicInfo.ParseUsage(keyword, ed, "data item 'T'");
        Assert.True(ed.HasErrors, $"USAGE {keyword} must be rejected at COBOL-85 (ISO §13.18.60 — a 2002 introduction)");
        Assert.Contains(ed.Diagnostics, d => d.Contains("COBOLNET0900") && d.Contains("COBOL-2002"));
    }

    [Theory]
    [InlineData("NATIONAL", "phase: Phase 4a)")]
    [InlineData("BIT", "phase: Phase 4a)")]
    // POINTER left this set at Phase-4b increment 1 (LIVE, DEVLOG 613) — it binds at 2002+, no 0899.
    [InlineData("BINARY-CHAR", "phase: Phase 4)")]
    [InlineData("FLOAT-SHORT", "phase: Phase 6)")]
    [InlineData("FLOAT-LONG", "phase: Phase 6)")]
    [InlineData("FLOAT-EXTENDED", "phase: Phase 6)")]
    public void ParseUsage_SkeletonKeyword_NotImplementedErrorAt2023_NamingOwningPhase(string keyword, string phase)
    {
        var ed = Ed(2023);
        PicInfo.ParseUsage(keyword, ed, "data item 'T'");
        Assert.True(ed.HasErrors, $"USAGE {keyword} must not compile silently (ISO §13.18.60; not yet implemented)");
        Assert.Contains(ed.Diagnostics,
            d => d.Contains("COBOLNET0899") && d.Contains("not yet implemented") && d.Contains(phase));
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

    [Theory]
    [InlineData("N(4)", "national")]     // category national, §8.5.2.10 / §13.18.40.4 GR9
    [InlineData("1(8)", "boolean")]      // category boolean, §8.5.2.5 / §13.18.40.4 GR8
    [InlineData("9V99E+99", "floating")] // external float, §13.18.40.4 GR13b
    public void Analyze_2002Symbol_0900At85_NamingCobol2002(string picture, string display)
    {
        var ed = Ed(85);
        PicInfo.Analyze(picture, Usage.Display, ed, "data item 'T'");
        Assert.True(ed.HasErrors, $"PIC {picture} ({display}) must be rejected at COBOL-85 (§13.18.40)");
        Assert.Contains(ed.Diagnostics, d => d.Contains("COBOLNET0900") && d.Contains("COBOL-2002"));
    }

    [Theory]
    [InlineData("N(4)")]
    [InlineData("1(8)")]
    [InlineData("9V99E+99")]
    public void Analyze_2002Symbol_NotImplementedErrorAt2023(string picture)
    {
        var ed = Ed(2023);
        PicInfo.Analyze(picture, Usage.Display, ed, "data item 'T'");
        Assert.True(ed.HasErrors, $"PIC {picture} must not classify silently (§13.18.40.4 — no implementation yet)");
        Assert.Contains(ed.Diagnostics, d => d.Contains("COBOLNET0899") && d.Contains("not yet implemented"));
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
    public void SkeletonUsage_StorageMappingMembers_ThrowLoud()
    {
        // USAGE POINTER left the skeleton set at Phase-4b increment 1 (LIVE, DEVLOG 613); FLOAT-SHORT stays
        // a skeleton (Phase 6), so it exercises the storage-mapping loud guard for a skeleton USAGE.
        var flt = new PicInfo(PicCategory.Numeric, Usage.FloatShort, Length: 0, Digits: 0, Scale: 0, Signed: false);
        Assert.Throws<InvalidOperationException>(() => flt.ClrType);
        Assert.Throws<InvalidOperationException>(() => flt.DefaultInitializer);
        Assert.Throws<InvalidOperationException>(() => flt.StorageWidth);
        Assert.Throws<InvalidOperationException>(() => flt.ProfileInitializer);
    }

    [Fact]
    public void SkeletonCategory_StorageMappingMembers_ThrowLoud()
    {
        var national = new PicInfo(PicCategory.National, Usage.Display, Length: 4, Digits: 0, Scale: 0, Signed: false);
        Assert.Throws<InvalidOperationException>(() => national.ClrType);
        Assert.Throws<InvalidOperationException>(() => national.DefaultInitializer);
        var boolean = new PicInfo(PicCategory.Boolean, Usage.Bit, Length: 8, Digits: 0, Scale: 0, Signed: false);
        Assert.Throws<InvalidOperationException>(() => boolean.ClrType);
        Assert.Throws<InvalidOperationException>(() => boolean.DefaultInitializer);
    }
}
