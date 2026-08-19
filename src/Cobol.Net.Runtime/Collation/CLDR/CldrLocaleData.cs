// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Collation.Cldr;

/// <summary>
/// The collation data of ONE CLDR locale file (<c>common/collation/&lt;locale&gt;.xml</c>, LDML Part 5, or its JSON
/// mirror — <see cref="CldrParser"/>): the locale identity, the default collation type, and every
/// <see cref="CldrCollation"/> the file defines (<c>standard</c>, <c>search</c>, <c>phonebook</c>, <c>traditional</c>,
/// …), each with its settings, imports and parsed tailoring rules. Immutable once parsed; produced by
/// <see cref="CldrLocaleLoader.Load"/>.
/// <para>A CLDR collation file carries RULES, not weights: a rule places a string relative to another
/// ("<c>&amp;N&lt;ñ&lt;&lt;&lt;Ñ</c>" — ñ right after n at level 1, Ñ after ñ at level 3). The WEIGHTS come from the
/// derived root table; <see cref="CldrTailoringBuilder"/> turns the rules into a tailored <see cref="CollationTable"/>
/// and a <see cref="CollationOptions"/>.</para>
/// </summary>
public sealed class CldrLocaleData
{
    internal CldrLocaleData(string tag, string language, string? script, string? territory, string? variant,
        string source, string? version, string? defaultCollation, IReadOnlyList<CldrCollation> collations)
    {
        Tag = tag;
        Language = language;
        Script = script;
        Territory = territory;
        Variant = variant;
        Source = source;
        Version = version;
        DefaultCollation = defaultCollation;
        Collations = collations;
    }

    /// <summary>The locale tag in BCP 47 form ("es", "de", "fr-CA", "zh-Hant", "root").</summary>
    public string Tag { get; }

    /// <summary>The <c>&lt;identity&gt;</c> language ("root" for the root file).</summary>
    public string Language { get; }

    /// <summary>The <c>&lt;identity&gt;</c> script, if any ("Hant").</summary>
    public string? Script { get; }

    /// <summary>The <c>&lt;identity&gt;</c> territory, if any ("CA").</summary>
    public string? Territory { get; }

    /// <summary>The <c>&lt;identity&gt;</c> variant, if any ("POSIX").</summary>
    public string? Variant { get; }

    /// <summary>Where the data came from — a file path, or the embedded pack entry ("cldr:collation/es.xml").</summary>
    public string Source { get; }

    /// <summary>The <c>&lt;version number="…"&gt;</c> value, if the file states one ("$Revision$" in the sources).</summary>
    public string? Version { get; }

    /// <summary>The <c>&lt;defaultCollation&gt;</c> type, if the file states one; else "standard" applies.</summary>
    public string? DefaultCollation { get; }

    /// <summary>Every <c>&lt;collation&gt;</c> element of the file, in file order.</summary>
    public IReadOnlyList<CldrCollation> Collations { get; }

    /// <summary>The type <see cref="Find"/> falls back to: <see cref="DefaultCollation"/> or "standard".</summary>
    public string EffectiveDefaultType => DefaultCollation ?? "standard";

    /// <summary>The collation of a type ("standard", "phonebook", …; a BCP 47 <c>-u-co-</c> value such as
    /// "phonebk" is accepted too), preferring the non-<c>alt</c>, non-draft entry when several share the type; null
    /// when the file has none. An absent "standard" means "the parent's / the root order" — see
    /// <see cref="CldrLocaleLoader.ResolveCollation"/>.</summary>
    public CldrCollation? Find(string type)
    {
        string t = CldrCollation.CanonicalType(type);
        CldrCollation? best = null;
        foreach (var c in Collations)
        {
            if (!string.Equals(c.Type, t, StringComparison.OrdinalIgnoreCase)) continue;
            if (best is null || (best.Alt is not null && c.Alt is null) || (best.Alt == c.Alt && best.IsDraft && !c.IsDraft)) best = c;
        }
        return best;
    }

    /// <summary>The types the file defines (distinct, file order).</summary>
    public IEnumerable<string> Types => Collations.Select(c => c.Type).Distinct(StringComparer.OrdinalIgnoreCase);

    public override string ToString() => $"{Tag} ({Source}): {string.Join(", ", Types)}";
}

/// <summary>
/// One <c>&lt;collation type="…"&gt;</c> of a CLDR locale: its rules text (the <c>&lt;cr&gt;</c> CDATA — CLDR/ICU rule
/// syntax), parsed into <see cref="Rules"/> (resets and relations in order), its <see cref="Settings"/> (the
/// <c>[strength …]</c>, <c>[alternate …]</c>, <c>[reorder …]</c> … options that were in the rules text) and its
/// <see cref="Imports"/> (the <c>[import …]</c> directives, in place: an import inserts the imported collation's rules
/// at that point). <see cref="Unsupported"/> lists what this engine cannot honor from it.
/// </summary>
public sealed class CldrCollation
{
    internal CldrCollation(string type, string? alt, string? draft, string? references, string rulesText,
        IReadOnlyList<CldrRule> rules, CldrSettings settings, IReadOnlyList<CldrImport> imports, IReadOnlyList<string> unsupported)
    {
        Type = type;
        Alt = alt;
        Draft = draft;
        References = references;
        RulesText = rulesText;
        Rules = rules;
        Settings = settings;
        Imports = imports;
        Unsupported = unsupported;
    }

    /// <summary>The collation type ("standard", "search", "phonebook", "traditional", "eor", "emoji", "private-kana", …).</summary>
    public string Type { get; }

    /// <summary>The <c>alt</c> attribute ("proposed"), if any — an alternative entry of the same type.</summary>
    public string? Alt { get; }

    /// <summary>The <c>draft</c> attribute ("unconfirmed", "provisional", "contributed"), if any.</summary>
    public string? Draft { get; }

    /// <summary>True when the entry is marked draft — <see cref="CldrLocaleData.Find"/> prefers a non-draft one.</summary>
    public bool IsDraft => Draft is not null && !Draft.Equals("approved", StringComparison.OrdinalIgnoreCase);

    /// <summary>The <c>references</c> attribute (a bibliography key), if any.</summary>
    public string? References { get; }

    /// <summary>The rules exactly as the file gives them (comments included).</summary>
    public string RulesText { get; }

    /// <summary>The parsed rules — resets, relations, and the positions of the imports (see <see cref="CldrImportRule"/>)
    /// — in file order; the settings are extracted into <see cref="Settings"/>.</summary>
    public IReadOnlyList<CldrRule> Rules { get; }

    /// <summary>The settings the rules text declared.</summary>
    public CldrSettings Settings { get; }

    /// <summary>The <c>[import …]</c> directives, in file order.</summary>
    public IReadOnlyList<CldrImport> Imports { get; }

    /// <summary>What the engine cannot honor from this collation, each a one-line description — a setting it does not
    /// implement (caseLevel, numericOrdering, hiraganaQuaternary), a quaternary relation, … Empty for the vast
    /// majority of locales. Applied collations still order correctly for everything else.</summary>
    public IReadOnlyList<string> Unsupported { get; }

    /// <summary>The number of resets + relations (imports excluded).</summary>
    public int RuleCount => Rules.Count(r => r is not CldrImportRule);

    /// <summary>The BCP 47 <c>-u-co-</c> value of a type ("phonebk", "trad", "dict", "gb2312", "searchjl", …) → the
    /// LDML type name ("phonebook", "traditional", "dictionary", "gb2312han", …), from CLDR's
    /// <c>bcp47/collation.xml</c> aliases (pinned under <c>data/unicode/cldr/bcp47/</c>; the alias list is small and
    /// stable, so it is spelled here and pinned by a drift test against that file). Any other value is its own type.</summary>
    public static string CanonicalType(string type) => type.ToLowerInvariant() switch
    {
        "phonebk" => "phonebook",
        "trad" => "traditional",
        "dict" => "dictionary",
        "gb2312" => "gb2312han",
        _ => type.ToLowerInvariant(),
    };

    public override string ToString() => $"{Type}{(Alt is null ? "" : "/" + Alt)}: {RuleCount} rules{(Unsupported.Count == 0 ? "" : $", {Unsupported.Count} unsupported")}";
}

/// <summary>An <c>[import locale-u-co-type]</c> directive.</summary>
/// <param name="LocaleTag">The imported locale in BCP 47 form ("und" = root, "de", "zh").</param>
/// <param name="Type">The imported collation type ("search", "phonebook", "private-unihan", …; "standard" when the
/// directive names no <c>-u-co-</c>).</param>
public sealed record CldrImport(string LocaleTag, string Type)
{
    public override string ToString() => $"[import {LocaleTag}-u-co-{Type}]";
}

/// <summary>The settings a rules text can declare (UTS #35 Part 5 "Setting Options"). Null = not stated.</summary>
public sealed record CldrSettings
{
    public CollationStrength? Strength { get; init; }
    public AlternateHandling? Alternate { get; init; }
    public MaxVariable? MaxVariable { get; init; }
    public CaseFirst? CaseFirst { get; init; }
    /// <summary><c>[backwards 2]</c>.</summary>
    public bool? BackwardsSecondary { get; init; }
    /// <summary><c>[normalization on|off]</c> — accepted and irrelevant: the engine normalizes whenever a text holds a
    /// combining mark, which is what "on" asks for and what "off" merely permits skipping.</summary>
    public bool? Normalization { get; init; }
    /// <summary><c>[caseLevel on|off]</c> — recorded; not implemented (reported in <see cref="CldrCollation.Unsupported"/>).</summary>
    public bool? CaseLevel { get; init; }
    /// <summary><c>[numericOrdering on|off]</c> — recorded; not implemented.</summary>
    public bool? NumericOrdering { get; init; }
    /// <summary><c>[hiraganaQ on|off]</c> — recorded; not implemented (deprecated in CLDR).</summary>
    public bool? HiraganaQuaternary { get; init; }
    /// <summary><c>[reorder code …]</c> — the reorder codes in order (scripts, the special groups, "others"); empty
    /// list = <c>[reorder others]</c>-style reset to no reordering; null = not stated.</summary>
    public IReadOnlyList<string>? Reorder { get; init; }
    /// <summary><c>[suppressContractions [set]]</c> — the code points whose contractions are removed.</summary>
    public IReadOnlyList<int>? SuppressContractions { get; init; }
    /// <summary><c>[optimize [set]]</c> — a hint; accepted and ignored.</summary>
    public IReadOnlyList<int>? Optimize { get; init; }

    /// <summary>The engine settings these declare over <paramref name="defaults"/> (unstated ones keep the default).</summary>
    public CollationOptions ToOptions(CollationOptions defaults) => defaults with
    {
        Strength = Strength ?? defaults.Strength,
        Alternate = Alternate ?? defaults.Alternate,
        MaxVariable = MaxVariable ?? defaults.MaxVariable,
        CaseFirst = CaseFirst ?? defaults.CaseFirst,
        BackwardsSecondary = BackwardsSecondary ?? defaults.BackwardsSecondary,
    };

    /// <summary>These settings with <paramref name="over"/>'s stated values applied on top (an import's settings are
    /// overridden by the importing collation's own).</summary>
    public CldrSettings Merge(CldrSettings over) => new()
    {
        Strength = over.Strength ?? Strength,
        Alternate = over.Alternate ?? Alternate,
        MaxVariable = over.MaxVariable ?? MaxVariable,
        CaseFirst = over.CaseFirst ?? CaseFirst,
        BackwardsSecondary = over.BackwardsSecondary ?? BackwardsSecondary,
        Normalization = over.Normalization ?? Normalization,
        CaseLevel = over.CaseLevel ?? CaseLevel,
        NumericOrdering = over.NumericOrdering ?? NumericOrdering,
        HiraganaQuaternary = over.HiraganaQuaternary ?? HiraganaQuaternary,
        Reorder = over.Reorder ?? Reorder,
        SuppressContractions = over.SuppressContractions is null ? SuppressContractions
            : SuppressContractions is null ? over.SuppressContractions : SuppressContractions.Concat(over.SuppressContractions).Distinct().ToArray(),
        Optimize = over.Optimize ?? Optimize,
    };

    /// <summary>The settings this engine does not implement, as one-line descriptions.</summary>
    public IEnumerable<string> UnsupportedSettings()
    {
        if (CaseLevel == true) yield return "[caseLevel on] — a separate case level is not implemented; case decides at level 3 (or first, with caseFirst)";
        if (NumericOrdering == true) yield return "[numericOrdering on] — digit sequences collate by their digits, not by numeric value";
        if (HiraganaQuaternary == true) yield return "[hiraganaQ on] — the deprecated Hiragana quaternary distinction is not implemented";
    }
}

/// <summary>A parsed rule of a CLDR collation: a <see cref="CldrReset"/>, a <see cref="CldrRelation"/>, or the place
/// of an <see cref="CldrImportRule"/>.</summary>
public abstract record CldrRule
{
    /// <summary>The 1-based line of the rules text the rule starts on (diagnostics).</summary>
    public int Line { get; init; }
}

/// <summary>A reset — <c>&amp;text</c>, <c>&amp;[before n]text</c>, <c>&amp;[first regular]</c>: the following relations
/// place strings relative to this position.</summary>
/// <param name="Text">The reset string (null when <paramref name="Position"/> is a special position).</param>
/// <param name="Position">The logical position (<c>[first regular]</c>, <c>[last primary ignorable]</c> …), or null.</param>
/// <param name="BeforeLevel">1–3 for <c>[before n]</c> — the position just BEFORE the text at that level; 0 otherwise.</param>
public sealed record CldrReset(string? Text, CldrSpecialPosition? Position, int BeforeLevel) : CldrRule
{
    public override string ToString() => $"&{(BeforeLevel > 0 ? $"[before {BeforeLevel}]" : "")}{(Position is { } p ? $"[{CldrParser.PositionName(p)}]" : CldrParser.Quote(Text ?? ""))}";
}

/// <summary>A relation — <c>&lt;</c>, <c>&lt;&lt;</c>, <c>&lt;&lt;&lt;</c>, <c>&lt;&lt;&lt;&lt;</c>, <c>=</c>: the string
/// goes right after the current position at the given strength (identity: at the same position).</summary>
/// <param name="Strength">The level of the difference.</param>
/// <param name="Text">The tailored string (one code point, or a contraction).</param>
/// <param name="Prefix">The <c>prefix|</c> context (the mapping applies only after this string), or null.</param>
/// <param name="Extension">The <c>/extension</c> — the tailored string's elements are followed by this string's — or null.</param>
public sealed record CldrRelation(CldrRelationStrength Strength, string Text, string? Prefix, string? Extension) : CldrRule
{
    public override string ToString() =>
        $"{CldrParser.Operator(Strength)}{(Prefix is null ? "" : CldrParser.Quote(Prefix) + "|")}{CldrParser.Quote(Text)}{(Extension is null ? "" : "/" + CldrParser.Quote(Extension))}";
}

/// <summary>The place of an <c>[import …]</c> in the rule sequence (the imported collation's rules go here).</summary>
public sealed record CldrImportRule(CldrImport Import) : CldrRule
{
    public override string ToString() => Import.ToString();
}

/// <summary>The strength of a relation.</summary>
public enum CldrRelationStrength
{
    /// <summary><c>&lt;</c></summary>
    Primary = 1,
    /// <summary><c>&lt;&lt;</c></summary>
    Secondary = 2,
    /// <summary><c>&lt;&lt;&lt;</c></summary>
    Tertiary = 3,
    /// <summary><c>&lt;&lt;&lt;&lt;</c> — the quaternary difference (Japanese hiragana/katakana); the engine applies it
    /// as an identity at levels 1–3 and reports it as unsupported nuance.</summary>
    Quaternary = 4,
    /// <summary><c>=</c></summary>
    Identity = 5,
}

/// <summary>The logical positions a reset can name (UTS #35 Part 5 "Logical Reset Positions").</summary>
public enum CldrSpecialPosition
{
    FirstTertiaryIgnorable,
    LastTertiaryIgnorable,
    FirstSecondaryIgnorable,
    LastSecondaryIgnorable,
    FirstPrimaryIgnorable,
    LastPrimaryIgnorable,
    FirstVariable,
    LastVariable,
    FirstRegular,
    LastRegular,
    FirstImplicit,
    LastImplicit,
    FirstTrailing,
    LastTrailing,
}
