// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// ⛔ AN SDIDI VALUE WITH THE FRACTION DIGITS ITS OPERANDS CARRY, WHEN ONLY THE RUN TIME KNOWS THEM (kb/Work PB3538 review
/// findings R3-1 and R4-1; numeric design D2; CONFORMANCE.md DOC-A.1-123). A NESTED native quotient is truncated at the
/// working scale <see cref="CobolNum.NestedQuotientScale"/> derives from its operands' PRECISION, and the compiler knows that
/// precision for every operand except an SDIDI value whose fraction digits are a run-time fact: a NUMVAL value, a power
/// formed on the SDIDI, a function's returned value. Such a value's precision is the places it is HELD at
/// (<see cref="Own"/>, <see cref="CobolDec.FractionDigits"/>): a NUMVAL value's written places (`NUMVAL ("2.50")` has 2, as
/// `2.50` does), a computed value's as its operation formed it — never a count of the value's significant digits, which
/// dropped the trailing zero (R4-1). This pair carries it through the sums, differences,
/// products, negations and quotients above it by the same rules the compiler applies to a known precision (the larger for
/// a sum, difference or quotient, the sum for a product), so a quotient's precision is never its guard digits and the
/// working scale does not grow with the expression's depth. Before it, such an operand was given the precision 0, and
/// `IF FUNCTION NUMVAL ("0.000000000000001") / 2 > 0` truncated the quotient at 14 places, to zero.
/// <para>The compiler emits this form only where a nested quotient reads the precision; every other consumer reads the
/// same value from the plain <see cref="CobolDec"/> expression, so the pair never reaches a store, a relation or an
/// intrinsic.</para>
/// </summary>
/// <param name="Value">The SDIDI value.</param>
/// <param name="Precision">The fraction digits its operands carry (never a quotient's guard digits).</param>
public readonly record struct CobolDecWithPrecision(CobolDec Value, int Precision)
{
    /// <summary>A value whose precision is the fraction digits it is held at (<see cref="CobolDec.FractionDigits"/>).</summary>
    public static CobolDecWithPrecision Own(CobolDec value) => new(value, CobolDec.FractionDigits(value));

    /// <summary>The sum at the INTERMEDIATE ROUNDING <paramref name="mode"/> (<see cref="CobolDec.Add"/>); the larger
    /// precision.</summary>
    public static CobolDecWithPrecision Add(CobolDecWithPrecision a, CobolDecWithPrecision b, CobolRounding mode) =>
        new(CobolDec.Add(a.Value, b.Value, mode), Math.Max(a.Precision, b.Precision));

    /// <summary>The difference (<see cref="CobolDec.Sub"/>); the larger precision.</summary>
    public static CobolDecWithPrecision Sub(CobolDecWithPrecision a, CobolDecWithPrecision b, CobolRounding mode) =>
        new(CobolDec.Sub(a.Value, b.Value, mode), Math.Max(a.Precision, b.Precision));

    /// <summary>The product (<see cref="CobolDec.Mul"/>); the sum of the precisions, as a product's scale is.</summary>
    public static CobolDecWithPrecision Mul(CobolDecWithPrecision a, CobolDecWithPrecision b, CobolRounding mode) =>
        new(CobolDec.Mul(a.Value, b.Value, mode), a.Precision + b.Precision);

    /// <summary>The negation (<see cref="CobolDec.Negate"/>); the same precision.</summary>
    public static CobolDecWithPrecision Negate(CobolDecWithPrecision a) => new(CobolDec.Negate(a.Value), a.Precision);

    /// <summary>The NESTED native quotient: truncated at <see cref="CobolNum.NestedQuotientScale"/> of
    /// <paramref name="receiverScale"/> and the operands' larger precision, held to the SDIDI's 34 significant digits by
    /// round-to-odd (<see cref="CobolDec.DivTruncatedToOdd"/>), carrying that precision (not the working scale) to the
    /// operations above it. A zero divisor raises <see cref="CobolSizeError.ZeroDivide"/> (ISO §14.7.5 case 2).</summary>
    public static CobolDecWithPrecision DivTruncatedToOdd(CobolDecWithPrecision a, CobolDecWithPrecision b, int receiverScale)
    {
        int precision = Math.Max(a.Precision, b.Precision);
        return new(CobolDec.DivTruncatedToOdd(a.Value, b.Value, CobolNum.NestedQuotientScale(receiverScale, precision)), precision);
    }
}
