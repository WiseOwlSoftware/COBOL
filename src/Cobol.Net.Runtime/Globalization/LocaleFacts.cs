// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Concurrent;
using System.Globalization;
using CobolNet.Runtime.Collation;
using CobolNet.Runtime.Collation.Cldr;
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime.Globalization;

/// <summary>
/// The resolved SNAPSHOT of one locale's COBOL-relevant categories (ISO/IEC 1989:2023 §8.2.1 — LC_COLLATE,
/// LC_CTYPE, LC_MONETARY, LC_TIME; DESIGN-locale-facility seam S5 / §8 "The .NET mapping and its documented
/// limits"): the ONE place a <see cref="CultureInfo"/> is read for a locale, cached per tag. LC_COLLATE is NOT read
/// from .NET — it is COBOL.NET's own derived CLDR/UCA engine (<see cref="CollationEngine"/>; <see cref="Collate"/>
/// is the tag the engine resolves); the other three categories are .NET culture data: LC_CTYPE the
/// <see cref="TextInfo"/> (DETERMINATION L9 — simple 1:1 case mapping), LC_MONETARY the <see cref="NumberFormatInfo"/>
/// currency fields + <see cref="RegionInfo"/>, LC_TIME the <see cref="DateTimeFormatInfo"/> with DETERMINATION L10:
/// ISO 9945's <c>d_fmt</c> is the culture's <see cref="DateTimeFormatInfo.ShortDatePattern"/> and <c>t_fmt</c> its
/// <see cref="DateTimeFormatInfo.LongTimePattern"/> (the pattern that carries seconds — §15.53.4 r2 requires "hours,
/// minutes, and seconds").
/// <para><b>Availability vs content.</b> §8.2.1: "If the locale is not found during an operation requiring a locale,
/// the EC-LOCALE-MISSING exception condition is set to exist … If the locale content is invalid or incomplete during
/// an operation using a locale, the EC-LOCALE-INVALID exception condition is set to exist". A locale is AVAILABLE by
/// the ONE known-locale rule (<see cref="LocaleIdentification.IsAvailable"/> — its own CLDR collation data, a site
/// tailoring, or a culture .NET recognizes); its CULTURE DATA (LC_CTYPE / LC_MONETARY / LC_TIME) comes from the
/// nearest .NET culture of its tag (the tag, then its parents — <c>sr-Latn-RS</c> → <c>sr-Latn</c> → <c>sr</c>),
/// and when no ancestor is a predefined .NET culture the content is INCOMPLETE: <see cref="HasCultureData"/> is
/// false, the invariant culture's fields stand in, and an operation that needs the category raises
/// EC-LOCALE-INVALID (gated; with checking off the invariant-formatted result stands). Under .NET's invariant
/// globalization mode every culture collapses to the invariant one — detected once (<see cref="InvariantMode"/>),
/// and every non-root locale's culture data is then INCOMPLETE for the same reason. <see cref="Require"/> is the
/// ONE place both conditions are raised — every operation that needs a category (the LOCALE functions, the case
/// functions and class tests under a CHARACTER CLASSIFICATION, PICTURE format 2) asks it at USE.</para>
/// </summary>
public sealed class LocaleFacts
{
    private static readonly ConcurrentDictionary<string, LocaleFacts> s_cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>True when the process runs in .NET invariant globalization mode (every culture is the invariant
    /// one — probed once through the runtime switch and a culture-data probe).</summary>
    public static bool InvariantMode { get; } = DetectInvariantMode();

    private LocaleFacts(string tag, CultureInfo culture, bool hasCultureData)
    {
        Collate = tag;
        Culture = culture;
        HasCultureData = hasCultureData;
        IsAvailable = tag.Length == 0 || LocaleIdentification.IsAvailable(tag);
        DateTimeFormat = culture.DateTimeFormat;
        NumberFormat = culture.NumberFormat;
        TextInfo = culture.TextInfo;
        try { Region = culture.IsNeutralCulture || culture.Equals(CultureInfo.InvariantCulture) ? null : new RegionInfo(culture.Name); }
        catch (ArgumentException) { Region = null; }
    }

    /// <summary>The facts for a locale tag (L1-normalized or not — normalized here). Cached.</summary>
    public static LocaleFacts For(string? tag)
    {
        string t = LocaleIdentification.Normalize(tag);
        return s_cache.GetOrAdd(t, static k => Resolve(k));
    }

    /// <summary>The root / invariant locale's facts.</summary>
    public static LocaleFacts Root => For("");

    /// <summary>The L1-normalized tag — what LC_COLLATE resolves through <see cref="CollationEngine.ForLocale"/>.</summary>
    public string Collate { get; }

    /// <summary>The .NET culture the LC_CTYPE / LC_MONETARY / LC_TIME data comes from (the invariant culture when
    /// <see cref="HasCultureData"/> is false).</summary>
    public CultureInfo Culture { get; }

    /// <summary>True when a predefined .NET culture backs the tag (itself or an ancestor) and the process is not in
    /// invariant globalization mode — i.e. the category data is the locale's own, not the invariant stand-in.</summary>
    public bool HasCultureData { get; }

    /// <summary>True when the locale IS AVAILABLE in this operating environment (the ONE known-locale rule,
    /// <see cref="LocaleIdentification.IsAvailable"/>; the root always is). A CHARACTER CLASSIFICATION or LOCALE
    /// phrase may name a declared locale no environment provides — the compiler never resolves an external
    /// identification (DETERMINATION L1) — so its facts exist, unavailable, until an operation asks <see cref="Require"/>.</summary>
    public bool IsAvailable { get; }

    /// <summary>⛔ THE ONE §8.2.1 GATE — "If the locale is not found during an operation requiring a locale, the
    /// EC-LOCALE-MISSING exception condition is set to exist and the operation is unsuccessful. If the locale content
    /// is invalid or incomplete during an operation using a locale, the EC-LOCALE-INVALID exception condition is set
    /// to exist and the operation is unsuccessful." Called by every operation that needs <paramref name="category"/>
    /// of this locale, AT USE (a class test, a case function, a LOCALE function, an edit): an UNAVAILABLE locale
    /// raises EC-LOCALE-MISSING (checking-gated, §14.6.13.1.1) and returns null — the caller's "no locale content"
    /// arm, the coded character set's behavior, stands when checking is off (the implementor's determination under
    /// §14.6.13.1.3 #8); an available locale WITHOUT culture data for the category raises EC-LOCALE-INVALID (gated)
    /// and returns itself — the invariant stand-in fields are what the caller then reads. <paramref name="operation"/>
    /// names the operation for the message ("FUNCTION LOCALE-DATE", "class condition ALPHABETIC"), <paramref
    /// name="rule"/> the rule that makes the locale required there ("ISO §15.52.4 r1").</summary>
    public LocaleFacts? Require(LocaleCategory category, string operation, string rule)
    {
        if (!IsAvailable)
        {
            ExceptionState.LocaleMissingError($"{operation}: the locale '{Collate}' is not available in this operating environment ({rule}; ISO §8.2.1)");
            return null;
        }
        if (!HasCultureData && Collate.Length > 0)
            ExceptionState.LocaleInvalidError($"{operation}: the locale '{Collate}' has no {CategoryName(category)} content in this environment — its locale content is incomplete ({rule}; ISO §8.2.1)");
        return this;
    }

    private static string CategoryName(LocaleCategory category) => category switch
    {
        LocaleCategory.Collate => "LC_COLLATE",
        LocaleCategory.Ctype => "LC_CTYPE",
        LocaleCategory.Messages => "LC_MESSAGES",
        LocaleCategory.Monetary => "LC_MONETARY",
        LocaleCategory.Numeric => "LC_NUMERIC",
        LocaleCategory.Time => "LC_TIME",
        _ => "category",
    };

    /// <summary>LC_TIME — the culture's date/time formats (<c>d_fmt</c> = <see cref="DateTimeFormatInfo.ShortDatePattern"/>,
    /// <c>t_fmt</c> = <see cref="DateTimeFormatInfo.LongTimePattern"/>; L10).</summary>
    public DateTimeFormatInfo DateTimeFormat { get; }

    /// <summary>ISO 9945 <c>d_fmt</c> — the culture's short date pattern (L10).</summary>
    public string DateFormat => DateTimeFormat.ShortDatePattern;

    /// <summary>ISO 9945 <c>t_fmt</c> — the culture's long time pattern, hours, minutes AND seconds (L10).</summary>
    public string TimeFormat => DateTimeFormat.LongTimePattern;

    /// <summary>LC_MONETARY — the culture's number format (currency symbol, separators, grouping, digits, signs,
    /// patterns; the PICTURE format 2 / NUMVAL-C increment T6 reads these).</summary>
    public NumberFormatInfo NumberFormat { get; }

    /// <summary>LC_MONETARY <c>int_curr_symbol</c> — the region's ISO 4217 code, or null for a neutral / invariant culture.</summary>
    public RegionInfo? Region { get; }

    /// <summary>LC_CTYPE — the culture's case mapping (DETERMINATION L9: simple 1:1; the T5 increment reads it).</summary>
    public TextInfo TextInfo { get; }

    /// <summary>Forget the cached facts (tests that change the process's culture data; a host that toggles modes).</summary>
    public static void ClearCache() => s_cache.Clear();

    private static LocaleFacts Resolve(string tag)
    {
        if (tag.Length == 0) return new LocaleFacts("", CultureInfo.InvariantCulture, hasCultureData: true);
        if (InvariantMode) return new LocaleFacts(tag, CultureInfo.InvariantCulture, hasCultureData: false);
        // The .NET culture: the tag's base (no -u- extension), then its ancestors by truncation.
        string baseTag = CldrLocaleTag.Parse(tag).BaseTag;
        for (string t = baseTag; t.Length > 0; t = ParentTag(t))
        {
            try
            {
                var ci = CultureInfo.GetCultureInfo(t, predefinedOnly: true);
                return new LocaleFacts(tag, ci, hasCultureData: true);
            }
            catch (CultureNotFoundException) { }
            catch (ArgumentException) { }
        }
        return new LocaleFacts(tag, CultureInfo.InvariantCulture, hasCultureData: false);
    }

    private static string ParentTag(string t)
    {
        int i = t.LastIndexOf('-');
        return i > 0 ? t[..i] : "";
    }

    private static bool DetectInvariantMode()
    {
        if (AppContext.TryGetSwitch("System.Globalization.Invariant", out bool inv) && inv) return true;
        // The probe: in invariant mode every culture collapses to the invariant one, so a culture whose data
        // certainly differs from the invariant's (French month names) comes back identical.
        try
        {
            var fr = CultureInfo.GetCultureInfo("fr-FR", predefinedOnly: true);
            return string.Equals(fr.DateTimeFormat.MonthNames[0], CultureInfo.InvariantCulture.DateTimeFormat.MonthNames[0], StringComparison.Ordinal);
        }
        catch (CultureNotFoundException) { return true; }
        catch (ArgumentException) { return true; }
    }

    public override string ToString() => $"{(Collate.Length == 0 ? "root" : Collate)} → {Culture.Name switch { "" => "invariant", var n => n }}{(HasCultureData ? "" : " (no culture data)")}";
}
