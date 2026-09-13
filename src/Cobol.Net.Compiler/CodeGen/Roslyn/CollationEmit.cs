// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Runtime;
using Microsoft.CodeAnalysis.CSharp;

namespace CobolNet.CodeGen;

/// <summary>
/// THE renderer of a runtime <see cref="CobolCollation"/> carrier from its bound description (kb/Work PB101;
/// DESIGN-locale-facility §4.4): the ONE place that knows which runtime arm an <see cref="AlphabetDef"/> /
/// <see cref="NationalAlphabetDef"/> becomes — an <see cref="AlphanumericCollation"/> / <see cref="NationalCollation"/>
/// for a literal-phrase table (positions + the §15.15.4 r2 representative array + NextFree + the §12.3.7.4 GR8/GR9
/// extremes), a <see cref="LocaleCollation"/> for the LOCALE phrase. The program emitter renders the PROGRAM COLLATING
/// SEQUENCE once as <c>__COLLATE</c> / <c>__COLLATE_NAT</c>; the SORT/MERGE and indexed-file emitters render a
/// statement or file alphabet that is NOT the PCS inline. Nothing else spells a carrier's constructor.
/// </summary>
internal static class CollationEmit
{
    /// <summary>The C# expression constructing the carrier of a NON-identity alphanumeric alphabet.</summary>
    public static string New(AlphabetDef def) => New(def.Table, def.Locale, national: false);

    /// <summary>The C# expression constructing the carrier of a NON-identity national alphabet.</summary>
    public static string New(NationalAlphabetDef def) => New(def.Table, def.Locale, national: true);

    /// <summary>⛔ THE carrier constructor, over the two components EVERY alphabet definition reduces to — the
    /// §12.3.7.4 GR7 k table or the LOCALE arm — and the ONE bit that chooses the runtime class. The two overloads
    /// above differed by exactly that bit and were otherwise identical text; a third caller
    /// (<c>RangeCollationCarrier</c>, which holds the components rather than either def record — §14.7.8 rule 2's
    /// <c>IN alphabet-name-1</c> phrase) is what made the duplication a drift hazard instead of a curiosity.</summary>
    public static string New(CollatingTable? table, LocaleCollatingSpec? locale, bool national)
    {
        if (table is { } t)
            return $"new {(national ? nameof(NationalCollation) : nameof(AlphanumericCollation))}("
                + $"new ushort[] {{ {string.Join(", ", t.Codes)} }}, "
                + $"new ushort[] {{ {string.Join(", ", t.Positions)} }}, "
                + $"new ushort[] {{ {string.Join(", ", t.RepByPos)} }}, {t.NextFree}, "
                + $"{SymbolDisplay.FormatLiteral(t.HighValue, quote: true)}, {SymbolDisplay.FormatLiteral(t.LowValue, quote: true)})";
        if (locale is { } l) return Locale(l);
        throw new InvalidOperationException("an identity alphabet has no carrier — the native fast path emits nothing");
    }

    /// <summary>The LOCALE arm: the shared current-locale instance (§12.3.7.4 GR7e — resolved at each use) or a
    /// carrier bound to one named locale.</summary>
    private static string Locale(LocaleCollatingSpec l) =>
        l.Locale.Tag is not { } tag
            ? $"{nameof(LocaleCollation)}.{nameof(LocaleCollation.Current)}"
            : $"new {nameof(LocaleCollation)}({SymbolDisplay.FormatLiteral(tag, quote: true)})";   // the locale-name's L1-normalized tag; resolved (and EC-LOCALE-MISSING) at use
}
