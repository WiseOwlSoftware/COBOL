// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Collation;

/// <summary>
/// One collation element of COBOL.NET's derived multi-level collation table: the (primary, secondary, tertiary)
/// weight triple of the Unicode Collation Algorithm (UTS #10) — the same three-level shape ISO/IEC 14651's Common
/// Template Table uses — plus the element's VARIABLE marking, which decides its treatment under
/// <see cref="AlternateHandling.Shifted"/> (space, punctuation and symbols are variable; letters and digits are not).
/// <para>Weights are DERIVED values, not raw source values: the generator re-scales every primary by
/// <c>1 &lt;&lt; CollationTable.PrimaryShift</c> so a tailoring can place a character strictly between two adjacent root
/// primaries (Spanish ñ between n and o) without renumbering the table; secondaries and tertiaries carry the
/// source values. Order is preserved exactly. A weight of 0 at a level means "ignorable at that level" — a completely
/// ignorable element (all three zero, e.g. a control character) contributes nothing to any key level.</para>
/// </summary>
/// <param name="Primary">The level-1 weight (script/base letter identity); 0 = primary-ignorable (accents, most format characters).</param>
/// <param name="Secondary">The level-2 weight (diacritic/accent identity); 0 = secondary-ignorable.</param>
/// <param name="Tertiary">The level-3 weight (case/width/variant identity); 0 = tertiary-ignorable.</param>
/// <param name="IsVariable">True for a VARIABLE element (whitespace, punctuation, symbols) — under
/// <see cref="AlternateHandling.Shifted"/> its first three levels are ignored and its primary moves to level 4, which is
/// the ISO/IEC 14651 default treatment (the template table's IGNORE;IGNORE;IGNORE;&lt;position&gt; entries).</param>
public readonly record struct CollationElement(int Primary, int Secondary, int Tertiary, bool IsVariable = false)
{
    /// <summary>The element every level ignores — controls, most format characters, U+0000.</summary>
    public static readonly CollationElement Ignorable = new(0, 0, 0);

    /// <summary>True when every level is zero: the element contributes nothing at any level.</summary>
    public bool IsCompletelyIgnorable => Primary == 0 && Secondary == 0 && Tertiary == 0;

    /// <summary>True when the primary is zero: an accent-like element that only distinguishes at level 2/3.</summary>
    public bool IsPrimaryIgnorable => Primary == 0;

    public override string ToString() =>
        $"[{(IsVariable ? '*' : '.')}{Primary:X4}.{Secondary:X4}.{Tertiary:X4}]";
}

/// <summary>
/// The comparison depth of a collation: how many weight levels decide the order before two strings are called equal.
/// The value is the level NUMBER (STANDARD-COMPARE's argument-4 "ordering level" maps onto it directly).
/// </summary>
public enum CollationStrength
{
    /// <summary>Base letters only — "a", "A", "á" and "Á" are all equal.</summary>
    Primary = 1,
    /// <summary>Base letters, then accents — "a" = "A" but "a" ≠ "á".</summary>
    Secondary = 2,
    /// <summary>Base letters, accents, then case/width/variant — the CLDR root and ICU default.</summary>
    Tertiary = 3,
    /// <summary>The fourth level: under <see cref="AlternateHandling.Shifted"/> the position weight of the variable
    /// (space/punctuation/symbol) elements the first three levels ignored — the ISO/IEC 14651 default four-level
    /// ordering ("ISO 14651_2020_TABLE1" as STANDARD-COMPARE names it). Under
    /// <see cref="AlternateHandling.NonIgnorable"/> the fourth level never distinguishes anything (every element
    /// already counted at level 1) and this strength behaves as <see cref="Tertiary"/>.</summary>
    Quaternary = 4,
    /// <summary>All four levels, then the canonically-decomposed code point sequence as a final tie-break — a TOTAL
    /// order over distinct (canonically inequivalent) strings.</summary>
    Identical = 5,
}

/// <summary>How VARIABLE collation elements (space, punctuation, symbols) take part in a comparison (UTS #10 §4).</summary>
public enum AlternateHandling
{
    /// <summary>Variable elements keep their primary weights and sort like any other character — the CLDR / ICU
    /// default: "di Silva" ≠ "diSilva" at level 1, and "a-b" sorts by the hyphen's own primary.</summary>
    NonIgnorable = 0,
    /// <summary>Variable elements are ignored at levels 1–3 and weighted only at level 4 (their old primary), together
    /// with the ignorable elements that follow them — the ISO/IEC 14651 default: "a-b" = "ab" through level 3 and
    /// differs from it only at level 4.</summary>
    Shifted = 1,
}
