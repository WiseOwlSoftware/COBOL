// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Language-level type information for COBOL data items.
/// Separates "what kind of data" from "how it's stored" (USAGE).
/// </summary>
public interface ITypeSymbol
{
    /// <summary>Human-readable type name (e.g., "PIC 9(5)V99 COMP" or "NUMERIC").</summary>
    string Name { get; }
    /// <summary>True if this type participates in arithmetic (PIC 9/S/V/P or numeric edited).</summary>
    bool IsNumeric { get; }
    /// <summary>True if this type holds general character data (PIC X or alphanumeric edited).</summary>
    bool IsAlphanumeric { get; }
    /// <summary>True if this type represents a boolean result (condition evaluation).</summary>
    bool IsBoolean { get; }
    /// <summary>Decoded PIC layout, or null for typeless/builtin types.</summary>
    PicLayout? Pic { get; }
    /// <summary>Storage format: DISPLAY, COMP, COMP-3, INDEX, etc.</summary>
    UsageKind Usage { get; }
    /// <summary>ISO 6.1.2 data category (Numeric, Alphanumeric, NumericEdited, etc.).</summary>
    CobolCategory Category { get; }
}

/// <summary>
/// Decoded PIC string layout: category, length, scale, sign, editing.
/// Uses CobolCategory (from Runtime) as the canonical category lattice.
/// </summary>
/// <param name="Category">ISO 6.1.2 data category derived from PIC character analysis.</param>
/// <param name="Length">Total storage length in characters (or bytes for DISPLAY usage).</param>
/// <param name="IntegerDigits">Count of digits before the decimal point (V).</param>
/// <param name="FractionDigits">Count of digits after the decimal point (V).</param>
/// <param name="LeadingPScaling">Count of leading P symbols (implied zeros before the integer part).</param>
/// <param name="TrailingPScaling">Count of trailing P symbols (implied zeros after the fraction part).</param>
/// <param name="IsSigned">True if the PIC contains S (signed numeric).</param>
/// <param name="IsEdited">True if the PIC contains editing symbols (Z, *, CR, DB, insertion chars, etc.).</param>
/// <param name="BlankWhenZero">True if BLANK WHEN ZERO clause is active.</param>
public sealed record PicLayout(
    CobolCategory Category,
    int Length,
    int IntegerDigits,
    int FractionDigits,
    int LeadingPScaling,
    int TrailingPScaling,
    bool IsSigned,
    bool IsEdited,
    bool BlankWhenZero = false);

/// <summary>
/// Concrete type for a COBOL data item, carrying PIC layout and USAGE.
/// Constructed during type resolution from a DataSymbol's PIC string and USAGE clause.
/// </summary>
public sealed record DataTypeSymbol(
    string Name,
    bool IsNumeric,
    bool IsAlphanumeric,
    bool IsBoolean,
    PicLayout? Pic,
    UsageKind Usage) : ITypeSymbol
{
    /// <summary>
    /// Derives the category from PIC layout when available; falls back to
    /// the boolean flags for PIC-less builtin types.
    /// </summary>
    public CobolCategory Category { get; } = Pic?.Category
        ?? (IsNumeric ? CobolCategory.Numeric
            : IsAlphanumeric ? CobolCategory.Alphanumeric
            : CobolCategory.Unknown);
}

/// <summary>
/// Singleton type instances for compiler-internal expressions and conditions
/// that have no PIC clause (e.g., arithmetic results, boolean conditions).
/// </summary>
public static class BuiltinTypes
{
    /// <summary>Type for arithmetic expressions and numeric literals.</summary>
    public static readonly DataTypeSymbol Numeric =
        new("NUMERIC", IsNumeric: true, IsAlphanumeric: false, IsBoolean: false, Pic: null, UsageKind.Display);
    /// <summary>Type for boolean condition results (IF, EVALUATE WHEN, etc.).</summary>
    public static readonly DataTypeSymbol Boolean =
        new("BOOLEAN", IsNumeric: false, IsAlphanumeric: false, IsBoolean: true, Pic: null, UsageKind.Display);
    /// <summary>Type for string literals and alphanumeric expressions.</summary>
    public static readonly DataTypeSymbol Alphanumeric =
        new("ALPHANUMERIC", IsNumeric: false, IsAlphanumeric: true, IsBoolean: false, Pic: null, UsageKind.Display);
    /// <summary>Fallback type when category cannot be determined.</summary>
    public static readonly DataTypeSymbol Unknown =
        new("UNKNOWN", IsNumeric: false, IsAlphanumeric: false, IsBoolean: false, Pic: null, UsageKind.Unknown);
}
