// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Collation.Locale;

/// <summary>
/// What the locale-selection system knows about one selectable locale (kb/Work PB101, section D): its tag, where
/// its collation tailoring came from, and the (cached, immutable) collation table the engine uses for it.
/// Immutable; produced by <see cref="LocaleManager"/> / <see cref="LocaleConfig"/>.
/// </summary>
public sealed class LocaleInfo
{
    internal LocaleInfo(string name, string? tailorFilePath, CollationTable table, bool recognizedCulture)
    {
        Name = name;
        TailorFilePath = tailorFilePath;
        Table = table;
        IsRecognizedCulture = recognizedCulture;
    }

    /// <summary>The locale tag as selected, normalized to the BCP-47 hyphen form ("es_ES" → "es-ES"); "" is the root.</summary>
    public string Name { get; }

    /// <summary>Where the locale's tailoring rules were loaded from — a file path (the <c>COBOL_COLLATION_DIR</c>
    /// directory, or <c>Collation/</c> / <c>Collation/Locales/</c> beside the application) or the embedded resource
    /// name (<c>resource:Collation/Tailoring/es-ES.tailor</c>) — or null when the locale has no tailoring and
    /// collates by the root order (which IS the CLDR order for English, French, German, …).</summary>
    public string? TailorFilePath { get; }

    /// <summary>The collation table the engine uses for this locale: the root table with the tailoring layered on,
    /// or the root table itself. Never mutated — a tailoring produces a NEW table (see <c>CollationTable.WithTailoring</c>).</summary>
    public CollationTable Table { get; }

    /// <summary>True when the locale's table DIFFERS from the root — its tailoring rules changed something. A
    /// header-only tailoring file (en-US, fr-FR: "the root order is valid") resolves (<see cref="TailorFilePath"/>
    /// non-null) but tailors nothing.</summary>
    public bool IsTailored => !ReferenceEquals(Table, CollationTable.Root);

    /// <summary>True when a tailoring file (or embedded tailoring) was found for the tag or its language.</summary>
    public bool HasTailoringFile => TailorFilePath is not null;

    /// <summary>True when .NET recognizes the tag as a culture (a locale without a tailoring is still selectable
    /// when it is a known culture — it collates by the root order).</summary>
    public bool IsRecognizedCulture { get; }

    /// <summary>The engine's collator for this locale at its CLDR defaults (tertiary, non-ignorable — determination L11).</summary>
    public Collator Collator => CollationEngine.ForLocale(Name);

    public override string ToString() =>
        $"{(Name.Length == 0 ? "root" : Name)} ({(IsTailored ? "tailored: " + TailorFilePath : "root order")})";
}
