// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// ⛔ THE ARITHMETIC OPERATOR <c>**</c> ON THE BINARY64 CARRIER, AND THE ONE §2023:8.8.1.2 RULE-6 SCREEN EVERY CARRIER'S POWER ASKS
/// (kb/Work PB2628). Exponentiation is an operator of arithmetic-expression evaluation, not an intrinsic function, so it lives in the
/// value library beside the other carriers' powers (<see cref="CobolDec.Pow"/>, <see cref="CobolExact.Pow"/>,
/// <see cref="CobolExact.PowReal"/>), and each of them screens rule 6 through <see cref="ScreenRule6"/>.
/// </summary>
/// <remarks>
/// <para>Rule 6 is a GENERAL rule of arithmetic-expression evaluation (its clause is "Native, standard-binary, and standard-decimal
/// arithmetic"), so it binds the native power exactly as it binds the standard-decimal one. Two of its three parts are mandatory with a
/// named exception condition: §2023:8.8.1.2 GR6a ("the exponent shall have a value greater than zero" for a zero base) and GR6c ("the
/// evaluation of the exponent shall result in an integer" for a negative base), each EC-SIZE-EXPONENTIATION. GR6b (the positive of two
/// real results) is a selection rule, not a screen: each carrier yields one value. Before kb/Work PB28 the native arms went straight to
/// <see cref="Math.Pow"/> (<c>0 ** 0</c> returned 1, <c>-2 ** 0.5</c> returned 0); before kb/Work PB2628 the screen was written twice, here
/// and inside <see cref="CobolDec.Pow"/>, with two message texts for one condition.</para>
/// <para>⛔ GR6c IS THE RULE FROM COBOL-2002 (kb/Work PB5186): §1985:VI.6.2.3 GR5c refuses a negative base only when no real number is the
/// result, which is <see cref="Binary64(double, CobolExact)"/>; that edition's zero-base rule (§1985:VI.6.2.3 GR5a) is GR6a's.</para>
/// </remarks>
public static class CobolPower
{
    /// <summary>Which §2023:8.8.1.2 rule-6 refusal, if any, the four facts of an operand pair state: GR6a for a zero base whose exponent
    /// is not greater than zero (which includes §2023:8.8.1.5.4 GR4's <c>0 ** 0</c>), GR6c for a negative base whose exponent is not an
    /// integer. The one place the rule's condition is written; each carrier states the four facts of its own operands.</summary>
    private static Rule6Refusal Classify(bool zeroBase, bool exponentNotPositive, bool negativeBase, bool exponentNotInteger) =>
        zeroBase && exponentNotPositive ? Rule6Refusal.ZeroBase
        : negativeBase && exponentNotInteger ? Rule6Refusal.NegativeBase
        : Rule6Refusal.None;

    private enum Rule6Refusal { None, ZeroBase, NegativeBase }

    /// <summary>The refusal <paramref name="which"/>, naming the base and the exponent as the program holds them, so the diagnosis says
    /// which operand broke the rule (kb/Work PB6126). The texts are formed only here, on the refusal, never on the success path.</summary>
    private static CobolSizeError Rule6Error(Rule6Refusal which, string b, string e) => which == Rule6Refusal.ZeroBase
        ? new CobolSizeError(
            $"exponentiation: a zero base requires an exponent greater than zero, not {e} (ISO §2023:8.8.1.2 GR6a)", "EC-SIZE-EXPONENTIATION")
        : new CobolSizeError(
            $"exponentiation: a negative base ({b}) requires an integer exponent, not {e} (ISO §2023:8.8.1.2 GR6c)", "EC-SIZE-EXPONENTIATION");

    /// <summary>The §2023:8.8.1.2 rule-6 screen over two binary64 operands.</summary>
    internal static void ScreenRule6(double b, double e)
    {
        Rule6Refusal refusal = Classify(zeroBase: b == 0, exponentNotPositive: e <= 0, negativeBase: b < 0, exponentNotInteger: e != Math.Floor(e));
        if (refusal != Rule6Refusal.None)
            throw Rule6Error(refusal, b.ToString(CultureInfo.InvariantCulture), e.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>The §2023:8.8.1.2 rule-6 screen over two standard-decimal operands; <paramref name="exponentIsInteger"/> is the
    /// exponent's integrality, which the caller has already read (<c>CobolDec.TryIntegerValue</c>).</summary>
    internal static void ScreenRule6(CobolDec b, CobolDec e, bool exponentIsInteger)
    {
        Rule6Refusal refusal = Classify(zeroBase: b.Sig == 0, exponentNotPositive: e.Sig <= 0, negativeBase: b.Sig < 0,
            exponentNotInteger: !exponentIsInteger);
        if (refusal != Rule6Refusal.None) throw Rule6Error(refusal, b.ToFunctionText(), e.ToFunctionText());
    }

    /// <summary>Native <c>**</c> on the FLOATING carrier: the §2023:8.8.1.3 implementor-defined approximation, screened by rule 6 first.
    /// From COBOL-2002; the COBOL-85 rule for a negative base is <see cref="Binary64(double, CobolExact)"/>'s.</summary>
    public static double Binary64(double b, double e)
    {
        ScreenRule6(b, e);
        return Math.Pow(b, e);
    }

    /// <summary><see cref="Binary64(double, double)"/> where a range condition's checking is ENABLED: the power is an operation on the
    /// binary64 intermediate, so its result's range is §2023:14.7.5 case 5 through the ONE check the other checked native operators use
    /// (<see cref="CobolFloat.InBinary64Range"/>, kb/Work PB1581): <c>C ** 2</c> over a COMP-2 holding 1.0E+300 is EC-SIZE-OVERFLOW, not
    /// +Infinity. A nonzero base's power is never zero. <paramref name="taken"/>: the conditions whose checking is enabled
    /// (kb/Work PB4092).</summary>
    public static double Binary64Checked(double b, double e, SizeChecks taken) =>
        InExponentiationRange(Binary64(b, e), b, e, taken);

    /// <summary>⛔ NATIVE <c>**</c> AT COBOL-85, WHERE A NEGATIVE BASE IS REFUSED ONLY WHEN NO REAL RESULT EXISTS (kb/Work PB5186;
    /// <c>DialectBehavior.NegativeBaseRequiresIntegerExponent</c>). §1985:VI.6.2.3 GR5c: "If no real number exists as the result of the
    /// evaluation, the size error condition exists"; §2002:8.8.1.1 replaced it with the integer-exponent rule <see cref="ScreenRule6(double, double)"/>
    /// screens (§2002:F.1 item 14 records the change). So at 85 <c>(-32) ** 0.2</c> is the real number −2, the fifth root of −32, and
    /// <c>(-32) ** 0.4</c> is 4, while <c>(-4) ** 0.5</c> has no real result and is the size error. Whether a real result exists is a
    /// property of the exponent's EXACT value, which a binary64 cannot carry (0.2 is not 1/5 in binary), so the exponent arrives as the
    /// <see cref="CobolExact"/> the program wrote (<see cref="CobolExact.HasOddDenominator"/>). The zero-base rule (§1985:VI.6.2.3 GR5a)
    /// is the one rule 6 states, and a non-negative base is <see cref="Binary64(double, double)"/> unchanged. GR5b (the positive of two
    /// real results) needs no step: for an odd denominator the real root is unique, and <c>Math.Pow</c> of a positive base returns the
    /// positive one.</summary>
    public static double Binary64(double b, CobolExact exponent)
    {
        double e = exponent.AsBinary64();
        if (!(b < 0)) return Binary64(b, e);
        if (!exponent.HasOddDenominator(out bool oddNumerator))
            throw new CobolSizeError(
                $"exponentiation: no real number is the result of the negative base ({b.ToString(CultureInfo.InvariantCulture)}) raised to "
                + $"{e.ToString(CultureInfo.InvariantCulture)} (§1985:VI.6.2.3 GR5c)", "EC-SIZE-EXPONENTIATION");
        double magnitude = Math.Pow(-b, e);
        return oddNumerator ? -magnitude : magnitude;
    }

    /// <summary><see cref="Binary64(double, CobolExact)"/> where a range condition's checking is ENABLED, through the one range check
    /// <see cref="Binary64Checked(double, double, SizeChecks)"/> applies.</summary>
    public static double Binary64Checked(double b, CobolExact exponent, SizeChecks taken) =>
        InExponentiationRange(Binary64(b, exponent), b, exponent.AsBinary64(), taken);

    /// <summary>The §2023:14.7.5 case 5 range of a binary64 power <paramref name="r"/> of <paramref name="b"/> and <paramref name="e"/>
    /// (kb/Work PB1581), for both checked entries.</summary>
    private static double InExponentiationRange(double r, double b, double e, SizeChecks taken) =>
        double.IsFinite(b) && double.IsFinite(e) ? CobolFloat.InBinary64Range(r, nonzero: b != 0, "exponentiation", taken) : r;
}
