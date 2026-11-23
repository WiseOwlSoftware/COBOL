// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using WiseOwl.Cobol.Runtime.Collation.Cldr;
using WiseOwl.Cobol.Runtime.Globalization;

namespace WiseOwl.Cobol.Runtime.Collation.Locale;

/// <summary>
/// The HOST's locale selection (kb/Work PB101 section D, PB105, PB2763): select a locale by any spelling a COBOL
/// LOCALE clause accepts, see what its collation is, and list the locales this system knows by name. It adds no
/// second rule and no second store:
/// <list type="bullet">
/// <item><b>One spelling rule.</b> A name is normalized by <see cref="LocaleIdentification.Normalize"/> (determination
/// L1) — the rule a SPECIAL-NAMES <c>LOCALE</c> clause and <c>SET LOCALE</c> use — so <c>"es_ES.UTF-8"</c>,
/// <c>"es-ES"</c> and <c>"es_es"</c> select the one locale <c>es-ES</c> here as they do in a program.</item>
/// <item><b>One description, one cache.</b> What a locale is — its table, settings, CLDR collation, <c>.tailor</c>
/// layer, availability — is the engine's <see cref="ResolvedLocaleCollation"/> (<see cref="CollationEngine.ResolveLocale"/>,
/// cached once per canonical tag); <see cref="GetLocale"/> returns it.</item>
/// <item><b>One availability rule.</b> A locale is selectable when <see cref="CollationEngine.IsKnownLocale"/> says
/// it is available: the root, a locale with a CLDR collation file of its own, a <c>.tailor</c> for the tag or its
/// language, or a culture .NET recognizes ("es-419", "nb-NO" — it then collates by its CLDR parent chain); never the
/// CLDR parent chain alone, or "no-Such-TABLE" would be Norwegian.</item>
/// <item><b>One current-locale state.</b> The run unit's <see cref="LocaleState"/> (<c>RunUnit.Current.Locale</c>,
/// ISO/IEC 1989:2023 §2023:8.2.1 / §2023:14.6.6 — DESIGN-locale-facility §4.3): <see cref="SetLocale"/> writes it (every
/// category, the <c>SET LOCALE LC_ALL</c> shape) and <see cref="CurrentLocale"/> reads its LC_COLLATE category; the
/// LOCALE-based collating sequence (<see cref="LocaleCollation.Current"/>) resolves that same state at each use.</item>
/// </list>
/// <para><b>Start-up.</b> No call is required: a run unit's <see cref="LocaleState"/> initializes itself from the L2
/// defaults (owner decision Q2 — <see cref="LocaleState.UserDefaultNow"/>) when the run unit is created. A host that
/// wants to override that for the program it is about to run calls <c>LocaleManager.SetLocale("es-ES")</c> before
/// invoking the program's entry point.</para>
/// </summary>
public static class LocaleManager
{
    /// <summary>The locale currently selected for collation in the ambient run unit — its LC_COLLATE locale.</summary>
    public static ResolvedLocaleCollation CurrentLocale => GetLocale(RunUnit.Current.Locale.Current(LocaleCategory.Collate));

    /// <summary>Select <paramref name="localeName"/> for the ambient run unit — every category (LC_ALL). Validates the
    /// locale, builds its collation (cached), and records it on <c>RunUnit.Current.Locale</c>. Null, "" or "root"
    /// selects the root order.</summary>
    /// <exception cref="CultureNotFoundException">The locale is not available (<see cref="GetLocale"/>).</exception>
    /// <exception cref="LocaleCollationException">The locale's site collation data (a CLDR or <c>.tailor</c> file) cannot
    /// be used — malformed, unreadable or stale; the message names the file and, where it has one, the line.</exception>
    public static void SetLocale(string? localeName) =>
        RunUnit.Current.Locale.Set(LocaleCategory.All, GetLocale(localeName).Tag);

    /// <summary>Restore the run unit's L2 user default (the locale it started with).</summary>
    public static void ResetLocale() => RunUnit.Current.Locale.Set(LocaleCategory.All, null);

    /// <summary>What a locale is, WITHOUT selecting it: the engine's resolution of the name's L1-normalized tag.</summary>
    /// <exception cref="CultureNotFoundException">The locale has no CLDR collation data of its own, no tailoring, and is
    /// not a culture .NET recognizes.</exception>
    /// <exception cref="LocaleCollationException">The locale's site collation data cannot be used (kb/Work PB2773).</exception>
    public static ResolvedLocaleCollation GetLocale(string? localeName)
    {
        var resolved = CollationEngine.ResolveLocale(LocaleIdentification.Normalize(localeName));
        // Site data that cannot be used is its own failure, named by its file (kb/Work PB2773), not "no data".
        if (resolved.Failure is { } failure) throw failure;
        if (!resolved.IsAvailable)
            throw new CultureNotFoundException(nameof(localeName), resolved.Tag,
                $"locale '{resolved.Tag}' is not selectable: no CLDR collation data of its own ({CldrLocaleLoader.DescribeSearch()}), no tailoring file ({TailoringRules.DescribeSearch(resolved.Tag)}) and not a culture .NET recognizes");
        return resolved;
    }

    /// <summary>Try-form of <see cref="GetLocale"/>: false (and null) for an unselectable locale.</summary>
    public static bool TryGetLocale(string? localeName, out ResolvedLocaleCollation? info)
    {
        try { info = GetLocale(localeName); return true; }
        catch (Exception ex) when (ex is CultureNotFoundException or LocaleCollationException) { info = null; return false; }
    }

    /// <summary>The locales that carry TAILORING rules: the tailorings embedded in this assembly plus every
    /// <c>.tailor</c> file in the searched directories (<c>COBOL_COLLATION_DIR</c>, <c>Collation/</c> and
    /// <c>Collation/Locales/</c> beside the application). Canonical tags, sorted, distinct.</summary>
    public static IReadOnlyList<string> TailoredLocales =>
        Canonical(TailoringRules.EmbeddedNames().Concat(TailoringRules.SearchDirectories().SelectMany(TailoringRules.TailoringsIn)));

    /// <summary>The locales with CLDR collation data of their own — every file in the embedded CLDR pack ("de-AT",
    /// "sr-Latn", "zh-Hant") plus every <c>.xml</c>/<c>.json</c> in a site CLDR directory (<c>COBOL_CLDR_DIR</c>,
    /// <c>Collation/CLDR/</c> beside the application); the root's own file is not listed. Canonical tags, sorted, distinct.</summary>
    public static IReadOnlyList<string> CldrLocales => Canonical(CldrLocaleLoader.AvailableLocales());

    /// <summary>The locales <see cref="SetLocale"/> accepts BY NAME: the root (""), every CLDR locale, every tailored
    /// locale. Every culture .NET recognizes is selectable too; that set is large, so it is not listed here
    /// (<see cref="CollationEngine.IsKnownLocale"/> is the rule).</summary>
    public static IReadOnlyList<string> SupportedLocales => [.. new[] { "" }.Concat(CldrLocales).Concat(TailoredLocales).Distinct(StringComparer.Ordinal)];

    private static string[] Canonical(IEnumerable<string> names) =>
        [.. names.Select(n => LocaleTag.Parse(n)).Where(t => !t.IsRoot).Select(t => t.Canonical).Distinct(StringComparer.Ordinal).OrderBy(n => n, StringComparer.OrdinalIgnoreCase)];
}
