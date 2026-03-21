// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Numeric sub-kind for expression type inference.
/// </summary>
public enum NumericKind
{
    Integer,
    Decimal,
    Floating,
}

/// <summary>
/// Precision/scale information for numeric expression results.
/// </summary>
public sealed class NumericType
{
    public int Precision { get; }
    public int Scale { get; }
    public bool IsSigned { get; }
    public NumericKind Kind { get; }

    public NumericType(int precision, int scale, bool isSigned, NumericKind kind)
    {
        Precision = precision;
        Scale = scale;
        IsSigned = isSigned;
        Kind = kind;
    }

    public static NumericType Integer(int precision, bool isSigned = true)
        => new(precision, 0, isSigned, NumericKind.Integer);

    public static NumericType Decimal(int precision, int scale, bool isSigned = true)
        => new(precision, scale, isSigned, NumericKind.Decimal);

    public static NumericType Floating()
        => new(18, 0, true, NumericKind.Floating);
}

/// <summary>
/// Category of an expression result for type checking.
/// </summary>
public enum ExpressionTypeKind
{
    Numeric,
    Alphanumeric,
    Boolean,
    Group,
    Unknown,
}

/// <summary>
/// Type of an expression result. Carries category and optional numeric detail.
/// </summary>
public sealed class ExpressionType
{
    public ExpressionTypeKind Kind { get; }
    public NumericType? Numeric { get; }

    private ExpressionType(ExpressionTypeKind kind, NumericType? numeric = null)
    {
        Kind = kind;
        Numeric = numeric;
    }

    public bool IsNumeric => Kind == ExpressionTypeKind.Numeric;
    public bool IsAlphanumeric => Kind == ExpressionTypeKind.Alphanumeric;
    public bool IsBoolean => Kind == ExpressionTypeKind.Boolean;
    public bool IsInteger => Numeric?.Kind == NumericKind.Integer;

    public static ExpressionType MakeNumeric(NumericType numeric) => new(ExpressionTypeKind.Numeric, numeric);
    public static ExpressionType MakeAlphanumeric() => new(ExpressionTypeKind.Alphanumeric);
    public static ExpressionType MakeBoolean() => new(ExpressionTypeKind.Boolean);
    public static ExpressionType MakeGroup() => new(ExpressionTypeKind.Group);
    public static ExpressionType MakeUnknown() => new(ExpressionTypeKind.Unknown);

    // ── Singleton instances for common types ──
    public static readonly ExpressionType Alphanumeric = MakeAlphanumeric();
    public static readonly ExpressionType Boolean = MakeBoolean();
    public static readonly ExpressionType Group = MakeGroup();
    public static readonly ExpressionType Unknown = MakeUnknown();

    /// <summary>
    /// Derive ExpressionType from a DataSymbol's resolved type information.
    /// </summary>
    public static ExpressionType FromDataSymbol(DataSymbol sym)
    {
        if (sym.IsGroup)
            return Group;

        var resolved = sym.ResolvedType;
        if (resolved == null)
            return Unknown;

        if (resolved.IsNumeric && resolved.Pic != null)
        {
            var pic = resolved.Pic;
            var kind = pic.FractionDigits > 0 ? NumericKind.Decimal : NumericKind.Integer;
            return MakeNumeric(new NumericType(
                pic.IntegerDigits + pic.FractionDigits,
                pic.FractionDigits,
                pic.IsSigned,
                kind));
        }

        if (resolved.IsNumeric)
            return MakeNumeric(NumericType.Integer(9, true));

        if (resolved.IsAlphanumeric)
            return Alphanumeric;

        if (resolved.IsBoolean)
            return Boolean;

        // Numeric edited, alphanumeric edited → alphanumeric for expression purposes
        if (resolved.Pic?.IsEdited == true)
            return Alphanumeric;

        return Unknown;
    }

    /// <summary>
    /// Promote two numeric types for a binary arithmetic operation.
    /// Result has enough precision/scale to hold either operand.
    /// </summary>
    public static ExpressionType Promote(ExpressionType left, ExpressionType right)
    {
        if (!left.IsNumeric || !right.IsNumeric)
            return Unknown;

        var ln = left.Numeric!;
        var rn = right.Numeric!;

        // Floating wins
        if (ln.Kind == NumericKind.Floating || rn.Kind == NumericKind.Floating)
            return MakeNumeric(NumericType.Floating());

        int scale = Math.Max(ln.Scale, rn.Scale);
        int intDigits = Math.Max(ln.Precision - ln.Scale, rn.Precision - rn.Scale);
        int precision = intDigits + scale;
        bool signed = ln.IsSigned || rn.IsSigned;
        var kind = scale > 0 ? NumericKind.Decimal : NumericKind.Integer;

        return MakeNumeric(new NumericType(precision, scale, signed, kind));
    }
}
