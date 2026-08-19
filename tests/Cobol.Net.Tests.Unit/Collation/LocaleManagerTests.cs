// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using CobolNet.Runtime;
using CobolNet.Runtime.Collation;
using CobolNet.Runtime.Collation.Locale;
using Xunit;

namespace CobolNet.Tests.Unit.Collation;

/// <summary>
/// The locale-selection system (Runtime/Collation/Locale/: LocaleManager, LocaleInfo, LocaleConfig — kb/Work PB101
/// section D, PB105): selecting a locale validates the tag, loads and applies its collation (the CLDR collation of
/// the tag, then its .tailor layer) through the engine's cached tables, and records it on the run unit's ONE locale
/// state — so the LOCALE-based collating sequence a COBOL program runs under sees the selection; the default is the
/// run unit's L2 user default; unknown tags are refused; and CollationRuntime's initialization / warm-up report what
/// they did.
/// </summary>
public sealed class LocaleManagerTests
{
    [Fact]
    public void GetLocale_ReportsTheCldrCollation_ItsSettings_AndWhatIsUnsupported()
    {
        var da = LocaleManager.GetLocale("da");
        Assert.True(da.HasCldrRules);
        Assert.Equal("da", da.Cldr.Found!.Tag);
        Assert.Equal("standard", da.Cldr.Type);
        Assert.Equal(CaseFirst.Upper, da.Options.CaseFirst);
        Assert.True(da.IsTailored);
        Assert.False(da.HasTailoringFile);
        Assert.Empty(da.Unsupported);
        Assert.Contains("CLDR da/standard", da.ToString());
        var nb = LocaleManager.GetLocale("nb-NO");                        // parentLocales: nb → no
        Assert.Equal("no", nb.Cldr.Found!.Tag);
        Assert.True(nb.Collator.Compare("z", "æ") < 0);
        var phone = LocaleManager.GetLocale("de-DE-u-co-phonebk");
        Assert.Equal("phonebook", phone.Cldr.Type);
        Assert.Equal("de-DE-u-co-phonebk", phone.Name);
        var kn = LocaleManager.GetLocale("en-u-kn-true");                 // a key the engine does not implement
        Assert.Contains(kn.Unsupported, u => u.Contains("numeric"));
        Assert.False(kn.IsTailored);
        Assert.Contains("da", LocaleConfig.CldrLocales);
        Assert.Contains("zh-Hant", LocaleConfig.CldrLocales);
        Assert.Contains("da", LocaleConfig.SupportedLocales);
        Assert.True(LocaleConfig.IsSupported("es-419"));                  // a culture .NET recognizes; collates by es.xml
        Assert.False(LocaleConfig.IsSupported("no-Such-TABLE"));          // the CLDR parent chain alone never makes a tag known
        Assert.True(LocaleConfig.IsSupported("nb-NO"));
        Assert.True(LocaleConfig.IsSupported("de-u-co-phonebk"));
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
        Assert.Contains("", LocaleConfig.SupportedLocales);
        Assert.Contains("es-ES", LocaleConfig.SupportedLocales);
        Assert.Contains("en-US", LocaleConfig.SupportedLocales);
        Assert.Contains("fr-FR", LocaleConfig.SupportedLocales);
        Assert.Contains("es", LocaleConfig.TailoredLocales);
        Assert.True(LocaleConfig.IsSupported("es_ES"));
        Assert.True(LocaleConfig.IsSupported("es-MX"));       // language fallback to es
        Assert.True(LocaleConfig.IsSupported("de-DE"));       // a known culture — root order
        Assert.True(LocaleConfig.IsSupported("root"));
        Assert.True(LocaleConfig.IsSupported(""));
        Assert.False(LocaleConfig.IsSupported("xx-NOWHERE"));
        Assert.False(LocaleConfig.IsSupported(null));
        Assert.Equal(LocaleManager.SupportedLocales, LocaleConfig.SupportedLocales);
    }

    [Fact]
    public void GetLocale_DescribesTheTailoringSource_AndTheTable()
    {
        var es = LocaleManager.GetLocale("es_es");
        Assert.Equal("es-ES", es.Name);                       // normalized to the tailoring's declared @locale spelling
        Assert.True(es.IsTailored);
        Assert.StartsWith("resource:Collation/Tailoring/", es.TailorFilePath);
        Assert.Same(CollationEngine.TableForLocale("es-ES"), es.Table);
        Assert.NotSame(CollationTable.Root, es.Table);
        Assert.True(es.Collator.Compare("ñu", "nz") > 0);
        var de = LocaleManager.GetLocale("de-DE");
        Assert.False(de.IsTailored);
        Assert.Null(de.TailorFilePath);
        Assert.Same(CollationTable.Root, de.Table);
        Assert.True(de.IsRecognizedCulture);
        var root = LocaleManager.GetLocale("root");
        Assert.Equal("", root.Name);
        Assert.Same(CollationTable.Root, root.Table);
        Assert.Contains("root order", root.ToString());
        Assert.Throws<CultureNotFoundException>(() => LocaleManager.GetLocale("xx-NOWHERE"));
        Assert.False(LocaleManager.TryGetLocale("xx-NOWHERE", out var none));
        Assert.Null(none);
        Assert.True(LocaleManager.TryGetLocale("fr-FR", out var fr));
        Assert.False(fr!.IsTailored);                         // fr-FR ships a header-only tailoring: the root order…
        Assert.True(fr.HasTailoringFile);                     // …but IS resolved to that file
        Assert.NotNull(fr.TailorFilePath);
        Assert.Same(CollationTable.Root, fr.Table);
        Assert.True(es.HasTailoringFile);
        Assert.False(de.HasTailoringFile);
    }

    /// <summary>Selecting a locale writes the run unit's ONE locale state, so the LOCALE-based collating sequence
    /// (LocaleCollation.Current — an ALPHABET … IS LOCALE program collating sequence) sees it at its next use.</summary>
    [Fact]
    public void SetLocale_DrivesTheRunUnitsCurrentLocale_AndTheLocaleCollation()
    {
        RunUnit.Run(ru =>
        {
            string initial = LocaleManager.CurrentLocale.Name;
            Assert.Equal(ru.Locale.UserDefault, initial);
            LocaleManager.SetLocale("es-ES");
            Assert.Equal("es-ES", LocaleManager.CurrentLocale.Name);
            Assert.Equal("es-ES", ru.Locale.Current(LocaleCategory.Collate));
            Assert.Equal("es-ES", ru.Locale.Current(LocaleCategory.Time));   // LC_ALL: every category
            Assert.True(LocaleCollation.Current.Compare("ñu", "nz") > 0);
            Assert.True(LocaleManager.CurrentCollator.Compare("ñu", "nz") > 0);
            LocaleManager.SetLocale("root");
            Assert.Equal("", LocaleManager.CurrentLocale.Name);
            Assert.True(LocaleCollation.Current.Compare("ñu", "nz") < 0);
            Assert.Throws<CultureNotFoundException>(() => LocaleManager.SetLocale("xx-NOWHERE"));
            Assert.Equal("", LocaleManager.CurrentLocale.Name);   // a refused selection changes nothing
            LocaleManager.ResetLocale();
            Assert.Equal(initial, LocaleManager.CurrentLocale.Name);
        });
    }

    [Fact]
    public void DefaultLocale_IsTheL2UserDefault()
    {
        Assert.Equal(LocaleState.Determine(LocaleState.UserDefaultVariable, () => CultureInfo.CurrentCulture), LocaleConfig.DefaultLocale);
        Assert.Equal(LocaleState.UserDefaultVariable, LocaleConfig.DefaultLocaleVariable);
        RunUnit.Run(ru => Assert.Equal(ru.Locale.UserDefault, LocaleConfig.DefaultLocale));
    }

    /// <summary>A site tailoring in a searched directory (Collation/Locales/ beside the application, or
    /// COBOL_COLLATION_DIR) is discovered, listed and selectable — the owner's sample line shape
    /// (0x-prefixed code point) parses.</summary>
    [Fact]
    public void SiteTailoringDirectory_IsDiscoveredAndSelectable()
    {
        string dir = Path.Combine(Path.GetTempPath(), "cobolnet-locales-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string saved = Environment.GetEnvironmentVariable(LocaleConfig.TailoringDirectoryVariable) ?? "";
        try
        {
            int afterZ = CollationTable.Root.Lookup('z').Primary + 1;
            File.WriteAllText(Path.Combine(dir, "xx-SV.tailor"),
                $"@locale xx-SV\n0x00E4 {afterZ:X} 0020 0002\n0x00C4 {afterZ:X} 0020 0008\n");
            Environment.SetEnvironmentVariable(LocaleConfig.TailoringDirectoryVariable, dir);
            LocaleManager.ClearCache();
            Assert.Contains(dir, TailoringRules.SearchDirectories());
            Assert.Contains("xx-SV", TailoringRules.TailoringsIn(dir));
            Assert.Contains("xx-SV", LocaleConfig.TailoredLocales);
            Assert.True(LocaleConfig.IsSupported("xx-SV"));
            var info = LocaleManager.GetLocale("xx-SV");
            Assert.True(info.IsTailored);
            Assert.Equal(Path.Combine(dir, "xx-SV.tailor"), info.TailorFilePath);
            Assert.True(info.Collator.Compare("ä", "z") > 0);       // ä after z under the site tailoring
            Assert.True(CollationEngine.Root.Compare("ä", "z") < 0);
            Assert.Contains("xx-SV.tailor", TailoringRules.DescribeSearch("xx-SV"));
        }
        finally
        {
            Environment.SetEnvironmentVariable(LocaleConfig.TailoringDirectoryVariable, saved.Length == 0 ? null : saved);
            LocaleManager.ClearCache();
            Directory.Delete(dir, recursive: true);
        }
    }
}
