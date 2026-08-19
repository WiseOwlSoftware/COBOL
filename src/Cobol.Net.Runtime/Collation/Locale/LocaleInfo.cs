// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Collation.Cldr;

namespace CobolNet.Runtime.Collation.Locale;

/// <summary>
/// What the locale-selection system knows about one selectable locale (kb/Work PB101 section D, PB105): its tag,
/// where its collation came from — the CLDR collation (<see cref="Cldr"/>: which file of the parent chain, which
/// type) the <see cref="CldrTailoringBuilder"/> applied first, and the site's numeric <c>.tailor</c> layer
/// (<see cref="TailorFilePath"/>) on top — the (cached, immutable) collation table and settings the engine uses for
/// it, and what, if anything, it asked for that the engine does not honor. Immutable; produced by
/// <see cref="LocaleManager"/> over <see cref="CollationEngine.ResolveLocale"/>.
/// </summary>
public sealed class LocaleInfo
{
    internal LocaleInfo(string name, ResolvedLocaleCollation resolved, bool recognizedCulture)
    {
        Name = name;
        Resolved = resolved;
        IsRecognizedCulture = recognizedCulture;
    }

    /// <summary>The locale tag as selected, normalized to the BCP-47 hyphen form ("es_ES" → "es-ES"); "" is the root.</summary>
    public string Name { get; }

    /// <summary>Everything the engine resolved for the tag.</summary>
    public ResolvedLocaleCollation Resolved { get; }

    /// <summary>The CLDR collation selected for the tag: the file it was found in (the tag's own, a parent's, or none →
    /// the root order), the collation type in effect ("standard", "phonebook", …) and the tag's <c>-u-</c> keys.</summary>
    public CldrCollationSelection Cldr => Resolved.Cldr;

    /// <summary>Where the locale's numeric <c>.tailor</c> rules were loaded from — a file path (the
    /// <c>COBOL_COLLATION_DIR</c> directory, or <c>Collation/</c> / <c>Collation/Locales/</c> beside the application) or
    /// the embedded resource name (<c>resource:Collation/Tailoring/es-ES.tailor</c>) — or null when the locale has no
    /// <c>.tailor</c> layer (the CLDR collation alone, or the root order).</summary>
    public string? TailorFilePath => Resolved.Tailoring?.Source;

    /// <summary>The collation table the engine uses for this locale: the root table with the CLDR collation and the
    /// <c>.tailor</c> layer applied, or the root table itself. Never mutated — a tailoring produces a NEW table.</summary>
    public CollationTable Table => Resolved.Table;

    /// <summary>The engine settings the locale collates with — the CLDR collation's own (<c>[caseFirst upper]</c> for
    /// Danish, <c>[backwards 2]</c> for Canadian French, <c>[alternate shifted]</c> for Thai …) with the tag's
    /// <c>-u-</c> keys applied.</summary>
    public CollationOptions Options => Resolved.Options;

    /// <summary>True when the locale's table DIFFERS from the root — its CLDR collation or its <c>.tailor</c> changed
    /// something. A locale whose CLDR order IS the root order (English, French, German standard …) resolves but tailors
    /// nothing; it may still carry non-default <see cref="Options"/> (Canadian French).</summary>
    public bool IsTailored => Resolved.IsTailored;

    /// <summary>True when a <c>.tailor</c> file (or embedded tailoring) was found for the tag or its language.</summary>
    public bool HasTailoringFile => Resolved.Tailoring is not null;

    /// <summary>True when a CLDR collation with rules was applied (the tag's file or a parent's defines the type).</summary>
    public bool HasCldrRules => Resolved.Cldr.HasRules;

    /// <summary>True when .NET recognizes the tag as a culture (a locale without any tailoring is still selectable
    /// when it is a known culture — it collates by the root order).</summary>
    public bool IsRecognizedCulture { get; }

    /// <summary>What the tag or its CLDR collation asked for that the engine does not honor (empty for the vast
    /// majority of locales; a <c>[caseLevel on]</c>, a <c>-u-kn-true</c>, a quaternary relation …).</summary>
    public IReadOnlyList<string> Unsupported => Resolved.Unsupported;

    /// <summary>Informational notes from the builder (canonical closure, renumbering, prefix contexts).</summary>
    public IReadOnlyList<string> Notes => Resolved.Notes;

    /// <summary>The engine's collator for this locale at its own settings (determination L11: the CLDR default of
    /// tertiary strength / non-ignorable variables unless the locale's CLDR collation says otherwise).</summary>
    public Collator Collator => Resolved.Collator;

    public override string ToString() =>
        $"{(Name.Length == 0 ? "root" : Name)} ({(IsTailored ? "tailored: " : "root order")}{(HasCldrRules ? $"CLDR {Cldr.Found!.Tag}/{Cldr.Type}" : "")}{(HasTailoringFile ? $"{(HasCldrRules ? " + " : "")}{TailorFilePath}" : "")})";
}
