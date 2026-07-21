// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;

namespace CobolNet.Frontend.Expressions;

/// <summary>The category of a compile-time value (a compilation-variable value / a compile-time operand result;
/// ISO/IEC 1989:2023 §7.3). Drives the §7.3.8.2 SR1a category-match rule and the §7.3.8.3 GR2 comparison
/// dispatch.</summary>
public enum CtCategory
{
    /// <summary>A numeric value (§7.3.6 arithmetic result or a numeric literal), compared by value.</summary>
    Numeric,
    /// <summary>An alphanumeric literal value (STRING/hex), compared by binary character value.</summary>
    Alphanumeric,
    /// <summary>A national literal value, compared by binary character value.</summary>
    National,
    /// <summary>A boolean value (§7.3.7 / §8.8.2 result or a boolean literal), compared by bit value.</summary>
    Boolean,
}

/// <summary>
/// One compile-time value — the result of evaluating a compile-time operand (§7.3.6/§7.3.7 result, or a literal),
/// and the stored value of a compilation variable. Category-tagged so the constant-conditional comparison
/// (§7.3.8.3 GR2) and the SR2 redefinition check (§7.3.11.3) apply the right equality.
/// </summary>
/// <remarks>
/// Record member-wise equality is REPLACED by a hand-written <see cref="Equals(CtValue?)"/> that dispatches on
/// <see cref="Category"/>: Numeric compares <see cref="Number"/> ONLY (so <c>AS 1</c> / <c>AS 01</c> / <c>AS 1.0</c>
/// are the same value and SR2 does not fire on spelling); Alphanumeric/National compare <see cref="Text"/> (binary,
/// length-sensitive); Boolean compares <see cref="Bits"/> by <see cref="BitString"/> value-equality
/// (DESIGN-compile-time-expressions.md §9).
/// </remarks>
public sealed record CtValue
{
    public CtCategory Category { get; }
    /// <summary>The numeric value (meaningful only for <see cref="CtCategory.Numeric"/>).</summary>
    public decimal Number { get; }
    /// <summary>The character value (meaningful for <see cref="CtCategory.Alphanumeric"/>/<see cref="CtCategory.National"/>;
    /// for Numeric it is the canonical value text — the §7.3.6/GR5 substitution form).</summary>
    public string Text { get; }
    /// <summary>The bit value (meaningful only for <see cref="CtCategory.Boolean"/>).</summary>
    public BitString? Bits { get; }

    private CtValue(CtCategory category, decimal number, string text, BitString? bits)
    {
        Category = category; Number = number; Text = text; Bits = bits;
    }

    public static CtValue Numeric(decimal number, string text) => new(CtCategory.Numeric, number, text, null);
    public static CtValue Alphanumeric(string text) => new(CtCategory.Alphanumeric, 0m, text, null);
    public static CtValue National(string text) => new(CtCategory.National, 0m, text, null);
    public static CtValue Boolean(BitString bits) => new(CtCategory.Boolean, 0m, "", bits);

    public bool Equals(CtValue? other) => other is not null && Category == other.Category && Category switch
    {
        CtCategory.Numeric => Number == other.Number,
        CtCategory.Boolean => Bits is not null && other.Bits is not null && Bits.Equals(other.Bits),
        _ => string.Equals(Text, other.Text, StringComparison.Ordinal),   // Alphanumeric / National — binary, length-sensitive
    };

    /// <summary>RELATION equality (a cce relation §7.3.8.2 SR1a → §8.8.4.2 / an EVALUATE §7.3.13.4 GR4a match):
    /// numeric by value; boolean per §8.8.4.2.8 (shorter operand RIGHT-zero-extended — NOT length-sensitive);
    /// alphanumeric/national per §7.3.8.3 GR2 (binary, LENGTH-sensitive). Distinct from <see cref="Equals(CtValue?)"/>,
    /// which is the §7.3.11.3 SR2 redefinition equality (boolean length-sensitive).</summary>
    public bool RelationalEquals(CtValue other) => Category == other.Category && Category switch
    {
        CtCategory.Numeric => Number == other.Number,
        CtCategory.Boolean => Bits is not null && other.Bits is not null && BitString.EqualExtended(Bits, other.Bits),
        _ => string.Equals(Text, other.Text, StringComparison.Ordinal),
    };

    public override int GetHashCode() => Category switch
    {
        CtCategory.Numeric => HashCode.Combine(Category, Number),
        CtCategory.Boolean => HashCode.Combine(Category, Bits?.Bits),
        _ => HashCode.Combine(Category, Text),
    };
}
