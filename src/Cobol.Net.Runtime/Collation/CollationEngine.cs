// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Concurrent;
using System.Globalization;

namespace CobolNet.Runtime.Collation;

/// <summary>
/// The static entry point of COBOL.NET's collation subsystem — the configurations a COBOL program reaches, each a
/// cached, thread-safe <see cref="Collator"/> over the derived <see cref="CollationTable"/>:
/// <list type="bullet">
/// <item><see cref="Compare(string?,string?)"/> / <see cref="Root"/> — the CLDR root order (tertiary, non-ignorable),
/// the order ICU gives an untailored locale.</item>
/// <item><see cref="Standard"/> — the ISO/IEC 14651-style four-level ordering STANDARD-COMPARE's default table
/// "ISO 14651_2020_TABLE1" names: variable characters ignored through level 3 and weighted at level 4
/// (<see cref="AlternateHandling.Shifted"/>, <see cref="CollationStrength.Quaternary"/>); <see cref="Standard"/>
/// with an explicit level for its ordering-level argument.</item>
/// <item><see cref="ForLocale"/> — a locale's collation: the root table with that locale's
/// <see cref="TailoringRules"/> layered on (none for locales whose CLDR order IS the root order).</item>
/// <item><see cref="TryGetOrderingTable"/> — the resolution of an <c>ORDER TABLE … IS literal</c> name.</item>
/// </list>
/// ⚖ Conformance statement, verbatim (owner decision Q4, 2026-08-18): "Implements collation behavior consistent
/// with ISO/IEC 14651 through derived tables and CLDR/UCA data."
/// </summary>
public static class CollationEngine
{
    /// <summary>The name of the default ordering table STANDARD-COMPARE uses (ISO/IEC 1989:2023 §15.85.3 r5) — the
    /// spelling the standard gives; matched case-insensitively with the space and the underscore interchangeable.</summary>
    public const string DefaultOrderingTableName = "ISO 14651_2020_TABLE1";

    private static readonly Lazy<Collator> s_standard = new(
        () => new Collator(CollationTable.Root, CollationStrength.Quaternary, AlternateHandling.Shifted),
        LazyThreadSafetyMode.ExecutionAndPublication);
    private static readonly ConcurrentDictionary<string, CollationTable> s_localeTables = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<(CollationTable Table, CollationStrength Strength, AlternateHandling Alternate), Collator> s_collators = new();

    /// <summary>The CLDR root default — root table, tertiary strength, non-ignorable variables.</summary>
    public static Collator Root => Collator.Root;

    /// <summary>The ISO/IEC 14651-style default: root table, all four levels, variables shifted to level 4.</summary>
    public static Collator Standard => s_standard.Value;

    /// <summary>Compare two texts under the CLDR root default: &lt;0, 0, &gt;0.</summary>
    public static int Compare(string? a, string? b) => Root.Compare(a, b);

    /// <summary>Compare two texts under the root table at an explicit strength and alternate handling.</summary>
    public static int Compare(string? a, string? b, CollationStrength strength, AlternateHandling alternate = AlternateHandling.NonIgnorable) =>
        Cached(CollationTable.Root, strength, alternate).Compare(a, b);

    /// <summary>The 14651-style ordering at ordering level <paramref name="level"/> (1–4; STANDARD-COMPARE's
    /// argument-4): the levels above <paramref name="level"/> do not distinguish.</summary>
    public static Collator StandardAtLevel(int level)
    {
        if (level is < 1 or > 4) throw new ArgumentOutOfRangeException(nameof(level), level, "ordering level must be 1..4");
        return Cached(CollationTable.Root, (CollationStrength)level, AlternateHandling.Shifted);
    }

    /// <summary>The collator of a locale ("es-ES", "fr_FR", "en", …): the root table plus the locale's tailoring
    /// (see <see cref="TailoringRules.ForLocale"/>) at the given strength/alternate handling. Null/empty/"root" is the
    /// root order. Cached per (locale, strength, alternate).</summary>
    public static Collator ForLocale(string? localeTag, CollationStrength strength = CollationStrength.Tertiary,
        AlternateHandling alternate = AlternateHandling.NonIgnorable) =>
        Cached(TableForLocale(localeTag), strength, alternate);

    /// <summary>The (cached) table of a locale — the root table when the locale has no tailoring.</summary>
    public static CollationTable TableForLocale(string? localeTag)
    {
        if (string.IsNullOrWhiteSpace(localeTag) || localeTag.Equals("root", StringComparison.OrdinalIgnoreCase))
            return CollationTable.Root;
        string tag = localeTag.Trim().Replace('_', '-');
        return s_localeTables.GetOrAdd(tag, static t =>
            TailoringRules.ForLocale(t) is { } rules ? CollationTable.Root.WithTailoring(rules) : CollationTable.Root);
    }

    /// <summary>Resolve an <c>ORDER TABLE ordering-name IS literal</c> name (§12.3.7 / §15.85): the standard's
    /// default table name (→ the root table), or a locale tag with a tailoring, or a locale tag .NET recognizes whose
    /// order is the root order. Anything else is not a supported ordering table (→ EC-ORDER-NOT-SUPPORTED at the
    /// reference, §15.85.4 r2).</summary>
    public static bool TryGetOrderingTable(string? name, out CollationTable table)
    {
        table = CollationTable.Root;
        if (string.IsNullOrWhiteSpace(name)) return false;
        string n = name.Trim();
        if (IsDefaultOrderingTableName(n)) return true;
        string tag = n.Replace('_', '-');
        if (TailoringRules.ForLocale(tag) is not null)
        {
            table = TableForLocale(tag);
            return true;
        }
        try
        {
            _ = CultureInfo.GetCultureInfo(tag, predefinedOnly: true);
            return true;   // a known locale whose CLDR order is the root order
        }
        catch (CultureNotFoundException) { return false; }
    }

    /// <summary>"ISO 14651_2020_TABLE1" — case-insensitive, space/underscore interchangeable, any run of them one.</summary>
    public static bool IsDefaultOrderingTableName(string name)
    {
        static string Canon(string s) => string.Join('_', s.Split([' ', '_'], StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();
        return Canon(name) == Canon(DefaultOrderingTableName);
    }

    /// <summary>True when the text is well-formed UTF-16 (no unpaired surrogate) — the derived table orders every
    /// well-formed text; see <see cref="Collator.IsWellFormed"/>.</summary>
    public static bool IsWellFormed(ReadOnlySpan<char> text) => Collator.IsWellFormed(text);

    private static Collator Cached(CollationTable table, CollationStrength strength, AlternateHandling alternate)
    {
        if (ReferenceEquals(table, CollationTable.Root))
        {
            if (strength == CollationStrength.Tertiary && alternate == AlternateHandling.NonIgnorable) return Collator.Root;
            if (strength == CollationStrength.Quaternary && alternate == AlternateHandling.Shifted) return Standard;
        }
        return s_collators.GetOrAdd((table, strength, alternate), static k => new Collator(k.Table, k.Strength, k.Alternate));
    }
}
