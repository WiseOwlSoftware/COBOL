// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Collation;
using WiseOwl.Cobol.Runtime.Collation.Cldr;
using WiseOwl.Cobol.Runtime.Globalization;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Collation;

/// <summary>
/// The ONE canonical spelling of a locale tag (<see cref="LocaleTag"/>, kb/Work PB2763; DESIGN-locale-facility L1 item
/// 5): the rules that were written in seven places — the root's names, <c>_</c> for <c>-</c>, the subtag case, the base
/// tag against the <c>-u-</c> extension — and every reader that now asks it.
/// </summary>
[Collection("process-globals")]   // resolves locales through the engine's process-global cache
public sealed class LocaleTagTests
{
    [Theory]
    [InlineData("es-ES", "es-ES", "", "es-ES")]
    [InlineData(" es_es ", "es-ES", "", "es-ES")]
    [InlineData("ZH_hant_tw", "zh-Hant-TW", "", "zh-Hant-TW")]
    [InlineData("es-419", "es-419", "", "es-419")]
    [InlineData("de-DE-u-CO-Phonebk", "de-DE", "co-phonebk", "de-DE-u-co-phonebk")]
    [InlineData("de-u--kf-upper", "de", "kf-upper", "de-u-kf-upper")]
    [InlineData("root", "", "", "")]
    [InlineData("UND", "", "", "")]
    [InlineData("", "", "", "")]
    [InlineData(null, "", "", "")]
    [InlineData("und-u-co-search", "", "co-search", "und-u-co-search")]
    public void Parse_GivesTheCanonicalForm(string? text, string @base, string extension, string canonical)
    {
        var tag = LocaleTag.Parse(text);
        Assert.Equal(@base, tag.Base);
        Assert.Equal(extension, tag.Extension);
        Assert.Equal(canonical, tag.Canonical);
        Assert.Equal(canonical, LocaleTag.Parse(tag.Canonical).Canonical);   // idempotent: a cache key parses to itself
        Assert.Equal(@base.Length == 0, tag.IsRoot);
    }

    [Fact]
    public void Language_IsTheFirstSubtag()
    {
        Assert.Equal("es", LocaleTag.Parse("es-MX").Language);
        Assert.Equal("sr", LocaleTag.Parse("sr_Latn_RS").Language);
        Assert.Equal("", LocaleTag.Root.Language);
    }

    /// <summary>RFC 5646 §2.1: every subtag is one to eight letters or digits. A spelling that is not names no locale,
    /// even when its language subtag has a <c>.tailor</c> (es.tailor is embedded): L1 makes an <c>@modifier</c> that is
    /// no CLDR collation type unavailable, and the language fallback of the tailoring lookup used to make
    /// <c>es_ES@foo</c> Spanish.</summary>
    [Fact]
    public void AnIllFormedTag_NamesNoLocale()
    {
        Assert.True(LocaleTag.Parse("de-DE-u-co-phonebk").IsWellFormed);
        Assert.False(LocaleTag.Parse("es-ES@foo").IsWellFormed);
        Assert.False(LocaleTag.Parse("es-ES.UTF-8").IsWellFormed);
        Assert.False(LocaleTag.Parse("NO SUCH TABLE").IsWellFormed);
        Assert.False(LocaleTag.Parse("en-verylongsubtag").IsWellFormed);
        Assert.False(LocaleIdentification.IsAvailable("es_ES@foo"));
        Assert.False(LocaleIdentification.IsAvailable("de_DE@foo"));
        Assert.True(LocaleIdentification.IsAvailable("es_ES@traditional"));      // a collation type: es-ES-u-co-traditional
        Assert.False(CollationEngine.TryGetOrderingTable("es_ES.UTF-8", out _));   // an ORDER TABLE literal is a tag, never L1-normalized
        Assert.True(CollationEngine.TryGetOrderingTable("es_ES", out _));
    }

    /// <summary>Every reader of a locale's spelling asks <see cref="LocaleTag"/>: the CLDR tag, its file name, the parent
    /// chain, L1's normalization and the engine's cache key agree on one spelling.</summary>
    [Fact]
    public void EveryReader_AgreesOnTheOneSpelling()
    {
        Assert.Equal("sr-Latn-RS", CldrLocaleTag.Parse("SR_latn_rs-u-co-standard").BaseTag);
        Assert.Equal("sr_Latn_RS", CldrLocaleTag.FileName("sr-latn-rs"));
        Assert.Equal("root", CldrLocaleTag.FileName("und"));
        Assert.Equal(new[] { "sr_Latn_RS", "sr_Latn", "sr", "root" }, CldrLocaleLoader.Chain("sr_latn_RS"));
        Assert.Equal(new[] { "root" }, CldrLocaleLoader.Chain("UND-u-co-search"));
        Assert.Equal("es-ES", LocaleIdentification.Normalize("es_es.UTF-8"));
        Assert.Equal("de-DE-u-kf-upper-co-phonebook", LocaleIdentification.Normalize("de_DE-u-KF-upper@phonebook"));
        Assert.Equal("", LocaleIdentification.Normalize("POSIX"));
        Assert.Equal("es-ES", CollationEngine.ResolveLocale("ES_es").Tag);
    }
}
