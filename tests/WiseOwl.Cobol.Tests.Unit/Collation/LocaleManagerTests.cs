// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Collation;
using WiseOwl.Cobol.Runtime.Collation.Locale;
using WiseOwl.Cobol.Runtime.Globalization;
using Xunit;
using WiseOwl.Cobol.Tests.Shared;

namespace WiseOwl.Cobol.Tests.Unit.Collation;

/// <summary>
/// The host's locale selection (Runtime/Collation/Locale/LocaleManager — kb/Work PB101 section D, PB105, PB2763):
/// selecting a locale normalizes the name by the L1 rule a COBOL LOCALE clause uses, reports the engine's ONE cached
/// resolution of it (the CLDR collation of the tag, then its .tailor layer), refuses a locale that is not available,
/// and records the selection on the run unit's ONE locale state — so the LOCALE-based collating sequence a COBOL
/// program runs under sees it; the default is the run unit's L2 user default; and CollationRuntime's initialization /
/// warm-up report what they did.
/// </summary>
[Collection("process-globals")]   // was "site-tailoring-directory"; widened (kb/Work PB126) — clears the
                                  // process-global locale/collation caches and identity-asserts TableForLocale.
public sealed class LocaleManagerTests
{
    [Fact]
    public void GetLocale_ReportsTheCldrCollation_ItsSettings_AndWhatIsUnsupported()
    {
        var da = LocaleManager.GetLocale("da");
        Assert.True(da.Cldr.HasRules);
        Assert.Equal("da", da.Cldr.Found!.Tag);
        Assert.Equal("standard", da.Cldr.Type);
        Assert.Equal(CaseFirst.Upper, da.Options.CaseFirst);
        Assert.True(da.IsTailored);
        Assert.Null(da.Tailoring);
        Assert.Empty(da.Unsupported);
        Assert.Contains("CLDR da/standard", da.ToString());
        var nb = LocaleManager.GetLocale("nb-NO");                        // parentLocales: nb → no
        Assert.Equal("no", nb.Cldr.Found!.Tag);
        Assert.True(nb.Collator.Compare("z", "æ") < 0);
        var phone = LocaleManager.GetLocale("de-DE-u-co-phonebk");
        Assert.Equal("phonebook", phone.Cldr.Type);
        Assert.Equal("de-DE-u-co-phonebk", phone.Tag);
        var kh = LocaleManager.GetLocale("en-u-kh-true");                 // a key the engine does not implement
        Assert.Single(kh.Unsupported, u => u.Contains("hiraganaQ"));      // once, not once for the tag and once for the builder
        Assert.False(kh.IsTailored);
        var kn = LocaleManager.GetLocale("en-u-kn-true");                 // numericOrdering is implemented (kb/Work PB107)
        Assert.Empty(kn.Unsupported);
        Assert.True(kn.Options.NumericOrdering);
        Assert.False(kn.IsTailored);                                      // the root TABLE, read with the setting
        Assert.Contains("da", LocaleManager.CldrLocales);
        Assert.Contains("zh-Hant", LocaleManager.CldrLocales);
        Assert.Contains("da", LocaleManager.SupportedLocales);
        Assert.True(CollationEngine.IsKnownLocale("es-419"));             // a culture .NET recognizes; collates by es.xml
        Assert.False(CollationEngine.IsKnownLocale("no-Such-TABLE"));     // the CLDR parent chain alone never makes a tag known
        Assert.True(CollationEngine.IsKnownLocale("nb-NO"));
        Assert.True(CollationEngine.IsKnownLocale("de-u-co-phonebk"));
    }

    [Fact]
    public void CollationRuntime_Initializes_AndWarmsUp()
    {
        Assert.True(CollationRuntime.IsInitialized || RunUnit.Current is not null);
        Assert.True(CollationRuntime.IsInitialized);                    // RunUnit's constructor initialized it
        var status = CollationRuntime.Warmup("es-ES");
        Assert.True(status.Warmed);
        Assert.True(status.DefaultLocaleResolved);
        Assert.Null(status.Warning);
        Assert.Equal("17.0.0", status.UcaVersion);
        Assert.Equal("17.0.0", status.UnicodeVersion);
        Assert.Equal("release-48-2", status.CldrRelease);
        Assert.True(status.CldrFiles >= 130);
        Assert.NotNull(status.WarmupTime);
        Assert.Same(status, CollationRuntime.Status);
        var bad = CollationRuntime.Warmup("qq-XX-nonsense");             // an unselectable locale is a warning, not an exception
        Assert.False(bad.DefaultLocaleResolved);
        Assert.NotNull(bad.Warning);
    }

    [Fact]
    public void SupportedLocales_AreTheRootAndTheTailoredOnes_AndKnownCulturesAreSelectable()
    {
        Assert.Contains("", LocaleManager.SupportedLocales);
        Assert.Contains("es-ES", LocaleManager.SupportedLocales);
        Assert.Contains("en-US", LocaleManager.SupportedLocales);
        Assert.Contains("fr-FR", LocaleManager.SupportedLocales);
        Assert.Contains("es", LocaleManager.TailoredLocales);
        Assert.True(CollationEngine.IsKnownLocale("es_ES"));
        Assert.True(CollationEngine.IsKnownLocale("es-MX"));       // language fallback to es
        Assert.True(CollationEngine.IsKnownLocale("de-DE"));       // a known culture — root order
        Assert.True(CollationEngine.IsKnownLocale("root"));
        Assert.True(CollationEngine.IsKnownLocale(""));
        Assert.True(CollationEngine.IsKnownLocale(null));
        Assert.False(CollationEngine.IsKnownLocale("xx-NOWHERE"));
        Assert.Equal(LocaleManager.SupportedLocales, LocaleManager.SupportedLocales.Select(t => LocaleTag.Parse(t).Canonical));   // canonical tags only
    }

    [Fact]
    public void GetLocale_DescribesTheTailoringSource_AndTheTable()
    {
        var es = LocaleManager.GetLocale("es_es");
        Assert.Equal("es-ES", es.Tag);                        // the canonical tag (LocaleTag)
        Assert.True(es.IsTailored);
        Assert.StartsWith("resource:Collation/Tailoring/", es.Tailoring!.Source);
        Assert.Same(CollationEngine.TableForLocale("es-ES"), es.Table);
        Assert.NotSame(CollationTable.Root, es.Table);
        Assert.True(es.Collator.Compare("ñu", "nz") > 0);
        var de = LocaleManager.GetLocale("de-DE");
        Assert.False(de.IsTailored);
        Assert.Null(de.Tailoring);
        Assert.Same(CollationTable.Root, de.Table);
        Assert.True(de.IsRecognizedCulture);
        var root = LocaleManager.GetLocale("root");
        Assert.Equal("", root.Tag);
        Assert.Same(CollationTable.Root, root.Table);
        Assert.Contains("root order", root.ToString());
        Assert.Throws<CultureNotFoundException>(() => LocaleManager.GetLocale("xx-NOWHERE"));
        Assert.False(LocaleManager.TryGetLocale("xx-NOWHERE", out var none));
        Assert.Null(none);
        Assert.True(LocaleManager.TryGetLocale("fr-FR", out var fr));
        Assert.False(fr!.IsTailored);                         // fr-FR ships a header-only tailoring: the root order…
        Assert.NotNull(fr.Tailoring);                         // …but IS resolved to that file
        Assert.Same(CollationTable.Root, fr.Table);
    }

    /// <summary>kb/Work PB2763: the host API and a COBOL program read a locale's spelling by ONE rule (L1,
    /// <see cref="LocaleIdentification.Normalize"/>) and the engine resolves each locale ONCE, keyed by its canonical
    /// tag. The host used to apply its own trim-and-replace, so <c>"es_ES.UTF-8"</c> — the spelling a LOCALE clause
    /// selects es-ES by — was a locale named "es-es.UTF-8" there, a second resolution of a second key; and the engine
    /// kept "es_es" and "es-ES" as one entry under whichever spelling came first.</summary>
    [Fact]
    public void EverySpellingOfALocale_IsOneLocale_ResolvedOnce()
    {
        var es = LocaleManager.GetLocale("es-ES");
        Assert.Same(es, LocaleManager.GetLocale("es_ES.UTF-8"));          // the POSIX spelling of a LOCALE clause
        Assert.Same(es, LocaleManager.GetLocale(" es_es "));
        Assert.Same(es, CollationEngine.ResolveLocale("ES-es"));
        Assert.Equal(LocaleIdentification.Normalize("es_ES.UTF-8"), es.Tag);
        var phone = LocaleManager.GetLocale("de_DE@phonebook");          // a CLDR collation modifier → -u-co-
        Assert.Equal("de-DE-u-co-phonebook", phone.Tag);
        Assert.Equal("phonebook", phone.Cldr.Type);
        Assert.Same(LocaleManager.GetLocale("C"), LocaleManager.GetLocale("root"));   // the POSIX root
        Assert.Throws<CultureNotFoundException>(() => LocaleManager.GetLocale("de_DE@nosuchmodifier"));
    }

    /// <summary>kb/Work PB2763: a NAMED locale collating sequence decides its locale's availability and collator once,
    /// not at every comparison — the same collator from use to use — and resolves again after the caches are cleared.
    /// An unavailable named locale is still EC-LOCALE-MISSING at every use (§2023:8.2.1).</summary>
    [Fact]
    public void NamedLocaleSequence_ResolvesOncePerCacheGeneration()
    {
        RunUnit.Run(_ =>
        {
            var sequence = new LocaleCollation("es-ES");
            var first = sequence.Resolve();
            Assert.Same(first, sequence.Resolve());
            Assert.Same(CollationEngine.ResolveLocale("es-ES").Collator, first);
            Assert.True(sequence.Compare("ñu", "nz") > 0);
            CollationEngine.ClearLocaleCache();
            var second = sequence.Resolve();
            Assert.NotSame(first, second);                               // a new resolution after the clear
            Assert.Same(CollationEngine.ResolveLocale("es-ES").Collator, second);
            Assert.True(sequence.Compare("ñu", "nz") > 0);
        });
    }

    /// <summary>Selecting a locale writes the run unit's ONE locale state, so the LOCALE-based collating sequence
    /// (LocaleCollation.Current — an ALPHABET … IS LOCALE program collating sequence) sees it at its next use.</summary>
    [Fact]
    public void SetLocale_DrivesTheRunUnitsCurrentLocale_AndTheLocaleCollation()
    {
        RunUnit.Run(ru =>
        {
            string initial = LocaleManager.CurrentLocale.Tag;
            Assert.Equal(ru.Locale.UserDefault.Collate, initial);
            LocaleManager.SetLocale("es-ES");
            Assert.Equal("es-ES", LocaleManager.CurrentLocale.Tag);
            Assert.Equal("es-ES", ru.Locale.Current(LocaleCategory.Collate));
            Assert.Equal("es-ES", ru.Locale.Current(LocaleCategory.Time));   // LC_ALL: every category
            Assert.True(LocaleCollation.Current.Compare("ñu", "nz") > 0);
            Assert.True(LocaleManager.CurrentLocale.Collator.Compare("ñu", "nz") > 0);
            LocaleManager.SetLocale("es_ES.UTF-8");                          // the spelling a LOCALE clause accepts
            Assert.Equal("es-ES", ru.Locale.Current(LocaleCategory.Collate));
            LocaleManager.SetLocale("root");
            Assert.Equal("", LocaleManager.CurrentLocale.Tag);
            Assert.True(LocaleCollation.Current.Compare("ñu", "nz") < 0);
            Assert.Throws<CultureNotFoundException>(() => LocaleManager.SetLocale("xx-NOWHERE"));
            Assert.Equal("", LocaleManager.CurrentLocale.Tag);   // a refused selection changes nothing
            LocaleManager.ResetLocale();
            Assert.Equal(initial, LocaleManager.CurrentLocale.Tag);
        });
    }

    [Fact]
    public void DefaultLocale_IsTheL2UserDefault()
    {
        Assert.Equal(LocaleState.Determine(LocaleState.UserDefaultVariable, () => CultureInfo.CurrentCulture), LocaleState.UserDefaultNow);
        RunUnit.Run(ru => Assert.Equal(ru.Locale.UserDefault.Collate, LocaleState.UserDefaultNow));
    }

    /// <summary>A site tailoring in a searched directory (Collation/Locales/ beside the application, or
    /// COBOL_COLLATION_DIR) is discovered, listed and selectable — the owner's sample line shape
    /// (0x-prefixed code point) parses.</summary>
    [Fact]
    public void SiteTailoringDirectory_IsDiscoveredAndSelectable()
    {
        string dir = ScratchDirectory.Create("wiseowl-cobol-locales-");
        string saved = Environment.GetEnvironmentVariable(TailoringRules.EnvDirectory) ?? "";
        try
        {
            int afterZ = CollationTable.Root.Lookup('z').Primary + 1;
            File.WriteAllText(Path.Combine(dir, "xx-SV.tailor"),
                $"@locale xx-SV\n0x00E4 {afterZ:X} 0020 0002\n0x00C4 {afterZ:X} 0020 0008\n");
            Environment.SetEnvironmentVariable(TailoringRules.EnvDirectory, dir);
            CollationEngine.ClearLocaleCache();
            Assert.Contains(dir, TailoringRules.SearchDirectories());
            Assert.Contains("xx-SV", TailoringRules.TailoringsIn(dir));
            Assert.Contains("xx-SV", LocaleManager.TailoredLocales);
            Assert.True(CollationEngine.IsKnownLocale("xx-SV"));
            var info = LocaleManager.GetLocale("xx-SV");
            Assert.True(info.IsTailored);
            Assert.Equal(Path.Combine(dir, "xx-SV.tailor"), info.Tailoring!.Source);
            Assert.True(info.Collator.Compare("ä", "z") > 0);       // ä after z under the site tailoring
            Assert.True(CollationEngine.Root.Compare("ä", "z") < 0);
            Assert.Contains("xx-SV.tailor", TailoringRules.DescribeSearch("xx-SV"));
        }
        finally
        {
            Environment.SetEnvironmentVariable(TailoringRules.EnvDirectory, saved.Length == 0 ? null : saved);
            CollationEngine.ClearLocaleCache();
            ScratchDirectory.Remove(dir);
        }
    }
}
