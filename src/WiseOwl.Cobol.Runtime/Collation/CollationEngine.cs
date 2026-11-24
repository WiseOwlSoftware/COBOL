// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Concurrent;
using System.Collections.Frozen;
using WiseOwl.Cobol.Runtime.Collation.Cldr;
using WiseOwl.Cobol.Runtime.Globalization;

namespace WiseOwl.Cobol.Runtime.Collation;

/// <summary>
/// Everything the engine resolved for one locale tag: the tailored table, the settings, and where each came from —
/// the CLDR collation (<see cref="Cldr"/>: which file, which type, the <c>-u-</c> keys) that
/// <see cref="CldrTailoringBuilder"/> applied FIRST, then the site's numeric <c>.tailor</c> layer
/// (<see cref="Tailoring"/>) on top. <see cref="Unsupported"/> lists what the tag or its CLDR collation asked for
/// that the engine does not honor (empty for the vast majority of locales). <see cref="Failure"/> is set when the
/// locale's collation DATA could not be used at all (kb/Work PB2773): the resolution is then the root order — the
/// nearest available order — and every consumer reads the failure to reach its own defined outcome.
/// <para>It is also what the host's locale selection reports (<see cref="Locale.LocaleManager.GetLocale"/>): the one
/// description of a locale, resolved once per canonical tag and cached by the engine (kb/Work PB2763).</para>
/// </summary>
/// <param name="Tag">The canonical tag (<see cref="LocaleTag.Canonical"/>: "es-ES", "de-u-co-phonebk"); "" is the root.</param>
/// <param name="Table">The collation table: the root table with the CLDR collation and the <c>.tailor</c> layer applied, or the root table itself. Never mutated.</param>
/// <param name="Options">The settings the locale collates with — the CLDR collation's own with the tag's <c>-u-</c> keys applied.</param>
/// <param name="Cldr">The CLDR collation selected for the tag: the file it was found in, the type in effect and the tag's <c>-u-</c> keys.</param>
/// <param name="Tailoring">The site's numeric <c>.tailor</c> layer (its <see cref="TailoringRules.Source"/> names the file or resource), or null.</param>
/// <param name="Unsupported">What the tag or its CLDR collation asked for that the engine does not honor.</param>
/// <param name="Notes">What the builder noted while building the table.</param>
public sealed record ResolvedLocaleCollation(string Tag, CollationTable Table, CollationOptions Options,
    CldrCollationSelection Cldr, TailoringRules? Tailoring, IReadOnlyList<string> Unsupported, IReadOnlyList<string> Notes)
{
    /// <summary>Why the locale's collation data could not be used, or null when it resolved (kb/Work PB2773).</summary>
    public LocaleCollationException? Failure { get; init; }

    /// <summary>Is the locale available in this operating environment — the ONE known-locale rule
    /// (<see cref="CollationEngine.IsKnownLocale"/>), decided once when the tag is resolved.</summary>
    public bool IsAvailable { get; init; }

    /// <summary>The collator for the resolution's own settings (determination L11), built once with the resolution, so a
    /// comparison under the locale costs no collator lookup and no <see cref="CollationOptions"/> (kb/Work PB2763).</summary>
    public Collator Collator { get; } = CollationEngine.For(Table, Options);

    /// <summary>True when the table differs from the root's (a CLDR tailoring or a <c>.tailor</c> changed something).</summary>
    public bool IsTailored => !ReferenceEquals(Table, CollationTable.Root);

    /// <summary>True when .NET recognizes the tag's base as a culture (<see cref="LocaleTag.PredefinedCulture"/>); the root
    /// counts as one.</summary>
    public bool IsRecognizedCulture => Cldr.Tag.BaseTag.Length == 0 || LocaleTag.PredefinedCulture(Cldr.Tag.BaseTag) is not null;

    public override string ToString() =>
        $"{(Tag.Length == 0 ? "root" : Tag)} ({(IsTailored ? "tailored: " : "root order")}{(Cldr.HasRules ? $"CLDR {Cldr.Found!.Tag}/{Cldr.Type}" : "")}{(Tailoring is not null ? $"{(Cldr.HasRules ? " + " : "")}{Tailoring.Source}" : "")})";
}

/// <summary>
/// The static entry point of WiseOwl COBOL's collation subsystem — the configurations a COBOL program reaches, each a
/// cached, thread-safe <see cref="Collator"/> over the derived <see cref="CollationTable"/>:
/// <list type="bullet">
/// <item><see cref="Compare(string?,string?)"/> / <see cref="Root"/> — the CLDR root order (tertiary, non-ignorable),
/// the order ICU gives an untailored locale.</item>
/// <item><see cref="Standard"/> — the ISO/IEC 14651-style four-level ordering STANDARD-COMPARE's default table
/// "ISO 14651_2020_TABLE1" names: variable characters ignored through level 3 and weighted at level 4
/// (<see cref="AlternateHandling.Shifted"/>, <see cref="CollationStrength.Quaternary"/>); <see cref="Standard"/>
/// with an explicit level for its ordering-level argument.</item>
/// <item><see cref="ForLocale"/> / <see cref="ResolveLocale"/> — a locale's collation: the CLDR collation of the
/// tag (its file along the parent chain, its <c>-u-co-</c> type and <c>-u-</c> settings — <see cref="CldrLocaleLoader"/>,
/// <see cref="CldrTailoringBuilder"/>) over the root table, then the site's numeric <see cref="TailoringRules"/>
/// (<c>.tailor</c>) layered on top; the root order itself for a locale whose CLDR order IS the root order.</item>
/// <item><see cref="TryGetOrderingTable"/> — the resolution of an <c>ORDER TABLE … IS literal</c> name.</item>
/// </list>
/// ⚖ Conformance statement, verbatim (owner decision Q4, 2026-08-18): "Implements collation behavior consistent
/// with ISO/IEC 14651 through derived tables and CLDR/UCA data."
/// </summary>
public static class CollationEngine
{
    /// <summary>The name of the default ordering table STANDARD-COMPARE uses in the current edition (ISO/IEC 1989:2023
    /// §2023:15.85.3 AR5) — the spelling the standard gives; matched through <see cref="IsDefaultOrderingTableName"/>.</summary>
    public const string DefaultOrderingTableName = "ISO 14651_2020_TABLE1";

    /// <summary>The default ordering table's name in EACH edition that has STANDARD-COMPARE and the ORDER TABLE clause — the name
    /// the edition's own text gives, because the edition names the table of ITS ISO/IEC 14651: 'ISO14651_2002_TABLE1' of
    /// ISO/IEC 14651:2001 (§2002:15.69.2 AR4, the 2002 text of FUNCTION STANDARD-COMPARE), 'ISO14651_2010_TABLE1' of ISO/IEC
    /// 14651:2011 (§2014:15.79.2 AR5; its SPECIAL-NAMES NOTE writes 'ISO_14651_2010_TABLE1', the same name with the separator)
    /// and 'ISO 14651_2020_TABLE1' (<see cref="DefaultOrderingTableName"/>). One derived table (<see cref="CollationTable.Root"/>,
    /// the CLDR/UCA data ISO/IEC 14651's Common Template Table is kept synchronized with) is the default table of every
    /// edition (owner decision Q4, 2026-08-18; kb/Work PB5720): the clause's literal-9 content is the implementor's
    /// (§2002:12.2.6.3 GR17, §2014:12.3.6.3 GR17, §2023:12.3.7.4 GR17), so every edition's name resolves.</summary>
    public static readonly IReadOnlyList<string> DefaultOrderingTableNames =
        ["ISO14651_2002_TABLE1", "ISO14651_2010_TABLE1", DefaultOrderingTableName];

    private static readonly Lazy<Collator> s_standard = new(
        () => new Collator(CollationTable.Root, CollationStrength.Quaternary, AlternateHandling.Shifted),
        LazyThreadSafetyMode.ExecutionAndPublication);
    /// <summary>Each locale's resolution, keyed by its canonical tag (<see cref="LocaleTag.Canonical"/>) and compared
    /// ordinally: the one spelling of a locale is the one key (kb/Work PB2763).</summary>
    private static readonly ConcurrentDictionary<string, ResolvedLocaleCollation> s_locales = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<(CollationTable Table, CollationOptions Options), Collator> s_collators = new();

    /// <summary>Bumped by every <see cref="ClearLocaleCache"/>: a holder of a resolution (a named <c>IS LOCALE</c>
    /// sequence) keeps it while the generation it was resolved in is current.</summary>
    private static int s_generation;

    /// <summary>The per-(table, level) shifted collators <see cref="OrderingAtLevel"/> hands out: STANDARD-COMPARE asks once
    /// per call, so the configuration is built once per pair, never a <see cref="CollationOptions"/> per call (kb/Work PB2763).</summary>
    private static readonly ConcurrentDictionary<(CollationTable Table, int Level), Collator> s_orderingLevels = new();

    /// <summary>Each <c>ORDER TABLE</c> literal's resolution (<see cref="TryGetOrderingTable"/>), keyed by the literal as
    /// written: null is "not a supported ordering table". The literal is a compile-time constant, so a program resolves it
    /// once, not once per STANDARD-COMPARE call; <see cref="ClearLocaleCache"/> forgets it with the locales (kb/Work PB2763).</summary>
    private static readonly ConcurrentDictionary<string, CollationTable?> s_orderingTables = new(StringComparer.Ordinal);

    /// <summary>The canonical forms of the <see cref="DefaultOrderingTableNames"/>, computed once (kb/Work PB2763, PB5720).</summary>
    private static readonly FrozenSet<string> s_defaultOrderingTableCanons =
        DefaultOrderingTableNames.Select(CanonicalOrderingTableName).ToFrozenSet(StringComparer.Ordinal);

    /// <summary>The CLDR root default — root table, tertiary strength, non-ignorable variables.</summary>
    public static Collator Root => Collator.Root;

    /// <summary>The ISO/IEC 14651-style default: root table, all four levels, variables shifted to level 4.</summary>
    public static Collator Standard => s_standard.Value;

    /// <summary>Compare two texts under the CLDR root default: &lt;0, 0, &gt;0.</summary>
    public static int Compare(string? a, string? b) => Root.Compare(a, b);

    /// <summary>Compare two texts under the root table at an explicit strength and alternate handling.</summary>
    public static int Compare(string? a, string? b, CollationStrength strength, AlternateHandling alternate = AlternateHandling.NonIgnorable) =>
        Cached(CollationTable.Root, strength, alternate).Compare(a, b);

    /// <summary>The 14651-style ordering of <paramref name="table"/> at ordering level <paramref name="level"/> (1–4;
    /// STANDARD-COMPARE's argument-4), variables shifted: the levels above <paramref name="level"/> do not distinguish.
    /// The root table's level 4 is <see cref="Standard"/> itself. Cached per (table, level).</summary>
    public static Collator OrderingAtLevel(CollationTable table, int level)
    {
        ArgumentNullException.ThrowIfNull(table);
        if (level is < 1 or > 4) throw new ArgumentOutOfRangeException(nameof(level), level, "ordering level must be 1..4");
        return s_orderingLevels.GetOrAdd((table, level),
            static k => Cached(k.Table, (CollationStrength)k.Level, AlternateHandling.Shifted));
    }

    /// <summary>The materialized key of <paramref name="text"/> under a locale's collation (null/empty/"root" = the
    /// root order), through that collator's <see cref="Cache.CollationKeyCache"/> — build once, compare many times.</summary>
    public static CollationKey GetKey(string? text, string? localeTag = null) =>
        Cache.CollationKeyCache.For(ForLocale(localeTag)).GetKey(text);

    /// <summary>The cached collator for a table and full settings.</summary>
    public static Collator For(CollationTable table, CollationOptions options)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(options);
        return options.IsPlain ? Cached(table, options.Strength, options.Alternate)
            : s_collators.GetOrAdd((table, options), static k => new Collator(k.Table, k.Options));
    }

    /// <summary>The collator of a locale ("es-ES", "fr_FR", "en", "de-u-co-phonebk", …): the locale's CLDR collation
    /// plus its <c>.tailor</c> layer (<see cref="ResolveLocale"/>) at the locale's own settings, or at the given
    /// strength / alternate handling where stated. Null/empty/"root" is the root order. Cached.</summary>
    public static Collator ForLocale(string? localeTag, CollationStrength? strength = null, AlternateHandling? alternate = null)
    {
        var r = ResolveLocale(localeTag);
        if (strength is null && alternate is null) return r.Collator;
        return For(r.Table, r.Options with { Strength = strength ?? r.Options.Strength, Alternate = alternate ?? r.Options.Alternate });
    }

    /// <summary>The (cached) table of a locale — the root table when the locale has no tailoring.</summary>
    public static CollationTable TableForLocale(string? localeTag) => ResolveLocale(localeTag).Table;

    /// <summary>Resolve (once per locale; cached) what a locale tag means to the engine: the CLDR collation of the tag
    /// (<see cref="CldrLocaleLoader.ResolveCollation"/> → <see cref="CldrTailoringBuilder"/>) over the root table,
    /// then the site's numeric tailoring for the tag or its language (<see cref="TailoringRules.ForLocale"/>) on top,
    /// the settings the CLDR collation and the tag's <c>-u-</c> keys declare, and whether the locale is available
    /// (<see cref="IsKnownLocale"/>). Any spelling of one tag is one locale: the cache is keyed by
    /// <see cref="LocaleTag.Canonical"/>, so "es_es", "es-ES" and " ES-es " share one resolution, and a caller that already
    /// holds the canonical spelling (a run unit's current locale, a SPECIAL-NAMES locale's tag) pays one lookup and no
    /// parse. Null/empty/"root"/"und" is the root order. Collation data that cannot be used (a malformed or stale site
    /// file) is never a silent fallback and never an exception: the resolution carries it as
    /// <see cref="ResolvedLocaleCollation.Failure"/> over the root order.</summary>
    public static ResolvedLocaleCollation ResolveLocale(string? localeTag) =>
        localeTag is not null && s_locales.TryGetValue(localeTag, out var resolved) ? resolved
            : s_locales.GetOrAdd(LocaleTag.Parse(localeTag).Canonical, static t => Resolve(t));

    /// <summary>⛔ THE ONE CATCH OF THE RESOLUTION PATH (kb/Work PB2773). Every data problem below it — the CLDR loader,
    /// the tailoring builder, the <c>.tailor</c> reader, <see cref="CollationTable.WithTailoring"/> — raises
    /// <see cref="LocaleCollationException"/>, and this records it (cached like any resolution, so a broken file is
    /// read once) instead of letting a compile, a run unit's start-up or a comparison die of it.</summary>
    private static ResolvedLocaleCollation Resolve(string tag)
    {
        try
        {
            var selection = CldrLocaleLoader.ResolveCollation(tag);
            var built = CldrTailoringBuilder.Build(selection, tag.Length == 0 ? "root" : tag);
            var table = built.Table;
            string baseTag = selection.Tag.BaseTag;
            var rules = tag.Length == 0 ? null : TailoringRules.ForLocale(baseTag);
            if (rules is not null) table = table.WithTailoring(rules);
            return new ResolvedLocaleCollation(tag, table, built.Options, selection, rules, built.Unsupported, built.Notes)
            {
                IsAvailable = LocaleTag.Parse(tag).IsWellFormed && IsAvailable(baseTag, rules),
            };
        }
        catch (LocaleCollationException failure)
        {
            // A locale whose resolution failed is not available, WHICHEVER source would otherwise make it known: its own
            // CLDR file can be sound while the site's .tailor layered over it is stale or malformed, and answering
            // available then would compare in the root order this failed resolution carries, silently (kb/Work PB2773,
            // train 1055 review).
            var rootOrder = new CldrCollationSelection(CldrLocaleTag.Parse(tag), null, null, null, "standard", []);
            return new ResolvedLocaleCollation(tag, CollationTable.Root, CollationOptions.Default, rootOrder, null,
                [failure.Message], []) { Failure = failure, IsAvailable = false };
        }
    }

    /// <summary>The known-locale rule over a resolution that succeeded, for a well-formed tag (<see cref="LocaleTag.IsWellFormed"/>:
    /// "es-ES@foo" — an <c>@modifier</c> that is no collation type, L1 — names no locale even though its language has a
    /// <c>.tailor</c>; see <see cref="IsKnownLocale"/>): the root; a <c>.tailor</c> for the tag or its language
    /// (<paramref name="rules"/>, which the resolution already found); a CLDR collation file of the base tag's own (never
    /// the parent chain alone); a culture .NET recognizes.</summary>
    private static bool IsAvailable(string baseTag, TailoringRules? rules) =>
        baseTag.Length == 0 || rules is not null || CldrLocaleLoader.LoadExact(baseTag) is not null
        || LocaleTag.PredefinedCulture(baseTag) is not null;

    /// <summary>The generation of the locale caches: <see cref="ClearLocaleCache"/> moves it, so a resolution held across
    /// uses (a named <c>IS LOCALE</c> sequence's) is resolved again once it is no longer current.</summary>
    public static int LocaleGeneration => Volatile.Read(ref s_generation);

    /// <summary>Forget every resolved locale (a test that writes a tailoring or CLDR file at run time). Tables already
    /// handed out stay valid; the next resolution re-reads the sources.</summary>
    public static void ClearLocaleCache()
    {
        s_locales.Clear();
        s_orderingTables.Clear();
        CldrLocaleLoader.ClearCache();
        Interlocked.Increment(ref s_generation);
    }

    /// <summary>Resolve an <c>ORDER TABLE ordering-name IS literal</c> name (§2023:12.3.7 / §2023:15.85): the standard's
    /// default table name (→ the root table), or a locale tag — one with CLDR collation data (its file, or a parent's;
    /// a <c>-u-co-</c> type is honored), one with a site <c>.tailor</c>, or one .NET recognizes whose order is the root
    /// order. Anything else is not a supported ordering table (→ EC-ORDER-NOT-SUPPORTED at the reference, §2023:15.85.4 RV2) —
    /// and so is a locale whose site collation data cannot be used, whose reason is
    /// <c>ResolveLocale(name).</c><see cref="ResolvedLocaleCollation.Failure"/> (kb/Work PB2773).</summary>
    public static bool TryGetOrderingTable(string? name, out CollationTable table)
    {
        table = CollationTable.Root;
        if (string.IsNullOrWhiteSpace(name)) return false;
        if (s_orderingTables.GetOrAdd(name, static n => ResolveOrderingTable(n)) is not { } resolved) return false;
        table = resolved;
        return true;
    }

    /// <summary>The one resolution <see cref="TryGetOrderingTable"/> memoizes: the table, or null when the name is not a
    /// supported ordering table.</summary>
    private static CollationTable? ResolveOrderingTable(string name)
    {
        if (IsDefaultOrderingTableName(name)) return CollationTable.Root;
        var resolved = ResolveLocale(name);
        return resolved.IsAvailable ? resolved.Table : null;
    }

    /// <summary>Is <paramref name="localeTag"/> a locale this implementation KNOWS — one with a CLDR collation file of
    /// its own (or of its region-less form, since a site may pass "de-AT" where CLDR has "de_AT" and .NET agrees), a
    /// <c>.tailor</c> for the tag or its language, or a culture .NET recognizes? The CLDR parent CHAIN alone does not
    /// make a tag known: "NO SUCH TABLE" parses as the tag <c>no-Such-TABLE</c>, whose chain reaches Norwegian's
    /// file — an <c>ORDER TABLE</c> literal or a LOCALE name that names nothing must stay unknown
    /// (EC-ORDER-NOT-SUPPORTED; an unselectable locale). The ONE rule <see cref="TryGetOrderingTable"/>,
    /// <see cref="LocaleIdentification.IsAvailable"/> and <see cref="Locale.LocaleManager"/> share, decided once per
    /// locale by its resolution (<see cref="ResolvedLocaleCollation.IsAvailable"/>); a <c>-u-</c> extension is ignored
    /// for the test and honored by the resolution. Null/empty/"root" is known (the root order). A locale whose own site
    /// data cannot be read is NOT known — it is not available in this operating environment (kb/Work PB2773).</summary>
    public static bool IsKnownLocale(string? localeTag) => ResolveLocale(localeTag).IsAvailable;

    /// <summary>One of the <see cref="DefaultOrderingTableNames"/> — case-insensitive, with every space and underscore ignored
    /// ("ISO 14651_2020_TABLE1", "iso_14651_2020_table1" and "ISO14651_2002_TABLE1" all name a default table).</summary>
    public static bool IsDefaultOrderingTableName(string name) => s_defaultOrderingTableCanons.Contains(CanonicalOrderingTableName(name));

    private static string CanonicalOrderingTableName(string name) =>
        string.Concat(name.Split([' ', '_'], StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();

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
        return s_collators.GetOrAdd((table, new CollationOptions(strength, alternate)), static k => new Collator(k.Table, k.Options));
    }
}
