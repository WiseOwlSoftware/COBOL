// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// ⛔ THE ARITHMETIC AN INTRINSIC FUNCTION'S EQUIVALENT ARITHMETIC EXPRESSION IS EVALUATED IN on the <see cref="CobolDec"/>
/// carrier — the ONE parameter that makes a decimal body (<c>CobolIntrinsics.*Dec</c>) one of the two standard models
/// (kb/Work PB5575):
/// <list type="bullet">
///   <item><b>The standard-decimal intermediate data item</b> (<see cref="Sdidi"/>; §2023:8.8.1.5.2, and the 2014 STANDARD-DECIMAL
///         mode): each operation's result rounded to 34 digits with the INTERMEDIATE ROUNDING mode, <see cref="Mode"/>.</item>
///   <item><b>The standard intermediate data item of ARITHMETIC IS STANDARD</b> (<see cref="Standard"/>; §2002:8.8.1.3, the same in
///         §2014:8.8.1.3): each operation's result "the exact sum truncated to 32 significant digits" (§2002:8.8.1.3.2, and
///         likewise the product and the quotient, §2002:8.8.1.3.4 and §2002:8.8.1.3.5), in the magnitude range of
///         §2002:8.8.1.3.1.1 — <see cref="CobolDec.ToSidi"/>, the one spelling of that item, after the engine's own operation at
///         <see cref="Mode"/> (TRUNCATION, or PROHIBITED; truncation composes, so the 34-digit truncation cut to 32 is the exact
///         result's 32-digit truncation). An integer power develops as its equivalent expression, each multiplication a
///         standard intermediate data item (<see cref="CobolDec.PowStandard"/>, §2002:8.8.1.3.6).</item>
/// </list>
/// The returned value of a function under standard arithmetic "shall equal the value of the equivalent arithmetic
/// expression" (§2002:15.3.1), so a body that spells its expression through these members IS the function's value in both
/// models, and the next body written that way is right in both without a second copy.
/// </summary>
public readonly struct DecArithmetic
{
    private DecArithmetic(CobolRounding mode, bool standardIntermediate) { Mode = mode; StandardIntermediate = standardIntermediate; }

    /// <summary>The mode the engine's own operation rounds each result by (the INTERMEDIATE ROUNDING mode; under ARITHMETIC IS
    /// STANDARD, TRUNCATION or PROHIBITED).</summary>
    public CobolRounding Mode { get; }

    /// <summary>Whether this is the 32-digit standard intermediate data item of ARITHMETIC IS STANDARD (§2002:8.8.1.3.1.1).</summary>
    public bool StandardIntermediate { get; }

    /// <summary>The standard-decimal model (§2023:8.8.1.5.2): 34 digits, each result rounded by <paramref name="mode"/>.</summary>
    public static DecArithmetic Sdidi(CobolRounding mode) => new(mode, standardIntermediate: false);

    /// <summary>The ARITHMETIC IS STANDARD model (§2002:8.8.1.3): 32 digits, each result truncated; <paramref name="mode"/> is the
    /// engine's own operation mode, TRUNCATION or PROHIBITED.</summary>
    public static DecArithmetic Standard(CobolRounding mode) => new(mode, standardIntermediate: true);

    /// <summary>A value as this model's intermediate holds it: unchanged on the SDIDI, the 32-digit standard intermediate data item
    /// otherwise (<see cref="CobolDec.ToSidi"/>).</summary>
    public CobolDec Item(CobolDec v) => StandardIntermediate ? v.ToSidi(Mode) : v;

    /// <summary>operand-1 + operand-2 (§2002:8.8.1.3.2; §2023:8.8.1.5.3).</summary>
    public CobolDec Add(CobolDec a, CobolDec b) => Item(CobolDec.Add(a, b, Mode));

    /// <summary>operand-1 - operand-2, the sum of operand-1 and the negated operand-2 (§2002:8.8.1.3.3).</summary>
    public CobolDec Sub(CobolDec a, CobolDec b) => Item(CobolDec.Sub(a, b, Mode));

    /// <summary>operand-1 * operand-2 (§2002:8.8.1.3.4).</summary>
    public CobolDec Mul(CobolDec a, CobolDec b) => Item(CobolDec.Mul(a, b, Mode));

    /// <summary>operand-1 / operand-2 (§2002:8.8.1.3.5).</summary>
    public CobolDec Div(CobolDec a, CobolDec b) => Item(CobolDec.Div(a, b, Mode));

    /// <summary>operand-1 ** operand-2 (§2002:8.8.1.3.6; §2023:8.8.1.5.4).</summary>
    public CobolDec Pow(CobolDec b, CobolDec e) =>
        StandardIntermediate ? Item(CobolDec.PowStandard(b, e, Mode)) : CobolDec.Pow(b, e, Mode);

    /// <summary>The square root: the 34-digit root rounded by <see cref="Mode"/> on the SDIDI (§2023:15.84.4 RV2), and under
    /// ARITHMETIC IS STANDARD "the absolute value of the exact square root of argument-1 truncated to 32 digits"
    /// (§2002:15.68.3 RV2).</summary>
    public CobolDec Sqrt(CobolDec v) => Item(CobolDec.Sqrt(v, Mode));
}
