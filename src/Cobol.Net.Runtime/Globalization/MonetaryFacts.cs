// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

namespace CobolNet.Runtime.Globalization;

/// <summary>
/// The ONE LC_MONETARY model (ISO/IEC 1989:2023 §8.2.2's field table + §8.2.1's ISO/IEC 9945:2009 clause-7
/// incorporation; DESIGN-locale-facility §4.6/§8; kb/Work PB64 T6): the resolved snapshot of one locale's
/// monetary fields, shared by BOTH consumers — <c>CobolLocaleEdit</c> (PICTURE format 2, §13.18.40.5 rules 9/12)
/// and the NUMVAL-C / TEST-NUMVAL-C LOCALE scan (§15.68.3 rule 5b) — so editing and recognition read one table
/// and cannot drift. Built from <see cref="LocaleFacts"/> (the ONE place a <see cref="CultureInfo"/> is read),
/// cached per <see cref="LocaleFacts"/> instance.
/// <para>⚖ Every field is a STRING, not a char — currency symbols, separators and sign strings are routinely
/// multi-character in CLDR data — and every string is normalized by <see cref="LocaleFacts.NormalizeLocaleText"/>
/// (DETERMINATION L12: strip Unicode Cf, map U+00A0/U+202F/U+2009 to the plain space). Unlike LC_TIME patterns
/// these strings consume CHARACTER POSITIONS of a fixed-width integer-1 item, so a host-varying byte changes the
/// §13.18.40.5 r14 b) truncation arithmetic and EC-LOCALE-SIZE itself — PB112's disease in monetary data
/// (measured: fr-FR's mon_thousands_sep moved U+00A0 → U+202F at ICU 72; every ar-* currency_symbol carries a
/// trailing U+200F). U+2212 is NOT mapped — a genuinely different character, not a spacing artifact.</para>
/// <para>⚖ Documented .NET-mapping determinations (CONFORMANCE.md): <c>int_curr_symbol</c> is
/// <see cref="RegionInfo.ISOCurrencySymbol"/> + one space, and is NULL for a neutral or invariant culture (no
/// region — the §15.68.3 r5b.3 international alternative can then never match); <c>int_frac_digits</c> has no
/// .NET carrier and equals <c>frac_digits</c>; <c>positive_sign</c> is .NET's NUMERIC positive sign ("+" —
/// POSIX locales usually leave it empty; without it §15.68.3 r5b.4's positive half and §13.18.40.5 r13's
/// positive half are dead); the placement conventions come from <see cref="MonetaryPlacement"/>'s derived
/// tables with <c>p_sign_posn</c> = 1.</para>
/// </summary>
public sealed class MonetaryFacts
{
    private MonetaryFacts(LocaleFacts facts)
    {
        var nf = facts.NumberFormat;
        CurrencySymbol = LocaleFacts.NormalizeLocaleText(nf.CurrencySymbol);
        IntCurrencySymbol = facts.Region is { ISOCurrencySymbol.Length: 3 } r ? r.ISOCurrencySymbol + " " : null;
        DecimalPoint = LocaleFacts.NormalizeLocaleText(nf.CurrencyDecimalSeparator);
        ThousandsSep = LocaleFacts.NormalizeLocaleText(nf.CurrencyGroupSeparator);
        // mon_grouping — .NET CurrencyGroupSizes → the POSIX right-to-left list (measured semantics): {3} = 3
        // repeating; {3,2} = 3 then 2 repeating; a TRAILING 0 = "no further grouping" (POSIX CHAR_MAX); {0} = no
        // grouping at all.
        int[] sizes = nf.CurrencyGroupSizes;
        GroupStops = sizes.Length > 0 && sizes[^1] == 0;
        GroupSizes = GroupStops ? sizes[..^1] : sizes;
        FracDigits = nf.CurrencyDecimalDigits;
        PositiveSign = LocaleFacts.NormalizeLocaleText(nf.PositiveSign);
        NegativeSign = LocaleFacts.NormalizeLocaleText(nf.NegativeSign);
        int pp = nf.CurrencyPositivePattern, np = nf.CurrencyNegativePattern;
        Positive = pp >= 0 && pp < MonetaryPlacement.PositiveByPattern.Length
            ? MonetaryPlacement.PositiveByPattern[pp]
            : new MonetaryConvention(true, 0, 1);
        Negative = np >= 0 && np < MonetaryPlacement.NegativeByPattern.Length
            ? MonetaryPlacement.NegativeByPattern[np]
            : new MonetaryConvention(true, 0, 1);
    }

    /// <summary>POSIX <c>currency_symbol</c> — the LOCAL currency string, normalized (L12). ⚖ DETERMINATION: this
    /// (never <c>int_curr_symbol</c>) is what a format-2 EDIT emits — §13.18.40.5 r9 gives the locale "the
    /// position, length, and character(s)" without choosing between §8.2.2's two symbols; recognition
    /// (§15.68.3 r5b.3) matches EITHER.</summary>
    public string CurrencySymbol { get; }

    /// <summary>POSIX <c>int_curr_symbol</c> — the region's ISO 4217 code + one space, or NULL for a neutral or
    /// invariant culture. §15.68.3 r5b.3 only ever matches "the first three characters".</summary>
    public string? IntCurrencySymbol { get; }

    /// <summary>POSIX <c>mon_decimal_point</c> (§8.2.2 "decimal delimiter"), normalized.</summary>
    public string DecimalPoint { get; }

    /// <summary>POSIX <c>mon_thousands_sep</c> (§8.2.2 "string used to group digits to the left of the decimal
    /// delimiter"), normalized; may be empty (no grouping separator exists then).</summary>
    public string ThousandsSep { get; }

    /// <summary>POSIX <c>mon_grouping</c> as group sizes applied RIGHT-TO-LEFT from the decimal delimiter; the
    /// LAST element repeats unless <see cref="GroupStops"/>. Empty = no grouping.</summary>
    public int[] GroupSizes { get; }

    /// <summary>True when the size list terminates (POSIX CHAR_MAX; .NET trailing 0): digits left of the listed
    /// groups are NOT grouped further.</summary>
    public bool GroupStops { get; }

    /// <summary>POSIX <c>frac_digits</c> / <c>int_frac_digits</c> (one .NET carrier for both — documented limit).
    /// ⛔ A RECOGNITION input only (§15.68.3 r5b.5): NO format-2 editing rule reads it — §13.18.40.5 r12 takes
    /// only the separators and group sizes from LC_MONETARY, and the fraction WIDTH is the picture's.</summary>
    public int FracDigits { get; }

    /// <summary>POSIX <c>positive_sign</c> (§8.2.2), normalized — ⚖ .NET's numeric positive sign standing in.</summary>
    public string PositiveSign { get; }

    /// <summary>POSIX <c>negative_sign</c> (§8.2.2), normalized. NOT always U+002D (sv-SE: U+2212) — and when the
    /// negative convention's <c>SignPosn</c> is 0, the parentheses ARE the convention and this string is unused.</summary>
    public string NegativeSign { get; }

    /// <summary>The nonnegative placement convention — (<c>p_cs_precedes</c>, <c>p_sep_by_space</c>) derived from
    /// <see cref="NumberFormatInfo.CurrencyPositivePattern"/>, <c>p_sign_posn</c> the determined constant 1.</summary>
    public MonetaryConvention Positive { get; }

    /// <summary>The negative placement convention — (<c>n_cs_precedes</c>, <c>n_sep_by_space</c>,
    /// <c>n_sign_posn</c>) derived from <see cref="NumberFormatInfo.CurrencyNegativePattern"/>.</summary>
    public MonetaryConvention Negative { get; }

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<LocaleFacts, MonetaryFacts> s_cache = new();

    /// <summary>The monetary snapshot of one locale's facts (cached per <see cref="LocaleFacts"/> instance).</summary>
    public static MonetaryFacts Of(LocaleFacts facts) => s_cache.GetValue(facts, static f => new MonetaryFacts(f));

    /// <summary>Resolve the named-else-current LC_MONETARY locale AT USE (§13.18.40.5 r11 / §14.6.6 r6 for the
    /// edit; §15.68.3 r5a for NUMVAL-C — "If locale-name-1 is not specified, category LC_MONETARY in the current
    /// locale is used") through the ONE §8.2.1 gate (<see cref="LocaleFacts.Require"/>): an unavailable locale is
    /// EC-LOCALE-MISSING (checking-gated; the ROOT's monetary content stands when checking is off), incomplete
    /// content EC-LOCALE-INVALID (the invariant stand-in stands).</summary>
    public static MonetaryFacts Require(string? localeTag, string operation, string rule)
    {
        string tag = localeTag ?? RunUnit.Current.Locale.Current(LocaleCategory.Monetary);
        var facts = LocaleFacts.For(tag).Require(LocaleCategory.Monetary, operation, rule) ?? LocaleFacts.Root;
        return Of(facts);
    }
}
