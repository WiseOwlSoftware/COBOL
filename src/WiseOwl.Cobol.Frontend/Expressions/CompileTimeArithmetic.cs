// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Frontend.Expressions;

/// <summary>
/// THE mode of arithmetic in which a compile-time arithmetic expression is evaluated (ISO/IEC 1989:2023 §2023:7.3.6.3
/// GR2), SELECTED ONCE PER EDITION by <see cref="For"/> — the one selection every consumer of the shared
/// <see cref="CompileTimeExpressionEvaluator"/> (a constant entry's <c>AS</c>, <c>&gt;&gt;DEFINE</c>,
/// <c>&gt;&gt;IF</c>/<c>&gt;&gt;EVALUATE</c>) reaches, because the evaluator asks it and nothing else does
/// (kb/Work PB1592).
/// <para>⛔ THREE MODES, BECAUSE THE STANDARD HAS THREE RULES. §2002:7.2.5.2 GR1 says "Standard arithmetic shall be
/// used for all arithmetic operations" — <see cref="Standard2002"/>: the 2002 standard intermediate data item of 32
/// significant digits, each operation's result TRUNCATED to 32 (§2002:8.8.1.3.2, §2002:8.8.1.3.4, §2002:8.8.1.3.5) in the
/// range 10^-999 to 10^1000 - 10^968 (§2002:8.8.1.3.1.1). §2014:7.3.5.2 GR1 says "The results of all arithmetic operations
/// shall be as if ARITHMETIC IS STANDARD-DECIMAL were in effect at compile time" — <see cref="StandardDecimal"/>, the SDIDI
/// decimal engine this compiler already runs <c>ARITHMETIC IS STANDARD-DECIMAL</c> on (34 digits, decimal128 range).
/// From 2023 the mode is the implementor's documented choice, <see cref="SystemDecimal"/> (docs/CONFORMANCE.md
/// DOC-A.1-29): Annex E.2 item 6 says the mode "is now explicitly implementor defined" and that "The previous COBOL
/// Standard required the use of an arithmetic mode that is no longer supported". The two edition boundaries are
/// written once, in <see cref="DialectBehaviors"/> (<see cref="DialectBehavior.CompileTimeArithmeticIsStandardDecimal"/>
/// at 2014, <see cref="DialectBehavior.CompileTimeArithmeticImplementorDefined"/> at 2023). COBOL-85 has no compile-time
/// arithmetic expression (the §2023:7.3 directives and the constant entry are 2002 introductions); an expression reached
/// there only after its introduction gate has refused it takes the 2002 mode.</para>
/// <para>Values travel in the mode-independent carrier (<see cref="CtNumeric"/>); a mode governs only what happens
/// to a value that ENTERS an arithmetic expression and each operation on such values.</para>
/// </summary>
public abstract class CompileTimeArithmetic
{
    private CompileTimeArithmetic() { }

    /// <summary>COBOL-2002's standard arithmetic — the mode ISO/IEC 1989:2002 prescribes for compile-time arithmetic
    /// expressions (§2002:7.2.5.2 GR1): a standard intermediate data item of 32 significant digits whose every operation
    /// result is the exact result TRUNCATED to 32 digits (§2002:8.8.1.3.2, §2002:8.8.1.3.4, §2002:8.8.1.3.5), magnitude 10^-999
    /// through 10^1000 - 10^968 (§2002:8.8.1.3.1.1), a value outside it a size error (§2002:8.8.1.3.1 r2; at compile
    /// time the caller's range diagnostic).</summary>
    public static readonly CompileTimeArithmetic Standard2002 = new Standard2002Mode();

    /// <summary>The mode ISO/IEC 1989:2014 prescribes for compile-time arithmetic expressions (§2014:7.3.5.2 GR1: "as if
    /// ARITHMETIC IS STANDARD-DECIMAL were in effect at compile time"), on the SDIDI decimal engine (§2023:8.8.1.5: 34
    /// significant digits, the decimal128 range) with the standard-decimal default intermediate rounding,
    /// NEAREST-AWAY-FROM-ZERO (§2023:11.9.11.2 GR3 a) — a compile-time expression has no OPTIONS paragraph to name
    /// another).</summary>
    public static readonly CompileTimeArithmetic StandardDecimal = new StandardDecimalMode();

    /// <summary>The COBOL-2023 implementor-defined mode (§2023:7.3.6.3 GR2; docs/CONFORMANCE.md DOC-A.1-29): .NET
    /// <see cref="decimal"/> — at most 28 digits after the point, magnitude below about 7.9×10²⁸, an inexact result
    /// rounded to nearest with a tie to the even digit.</summary>
    public static readonly CompileTimeArithmetic SystemDecimal = new SystemDecimalMode();

    /// <summary>⛔ THE ONE SELECTION: the mode compile-time arithmetic expressions use at <paramref name="edition"/>.</summary>
    public static CompileTimeArithmetic For(EditionInfo edition) =>
        DialectBehaviors.IsActive(DialectBehavior.CompileTimeArithmeticImplementorDefined, edition.Year) ? SystemDecimal
        : DialectBehaviors.IsActive(DialectBehavior.CompileTimeArithmeticIsStandardDecimal, edition.Year) ? StandardDecimal
        : Standard2002;

    /// <summary>How a diagnostic names this mode's evaluation range, so a range message states the limit of the
    /// mode that actually refused the value.</summary>
    public abstract string RangeDescription { get; }

    /// <summary>A carrier value entering an arithmetic expression as an operand — an operand literal or a
    /// substituted name — in this mode's intermediate form, or <see langword="null"/> when it lies outside this
    /// mode's range.</summary>
    public abstract CobolDec? Enter(CobolDec value);

    /// <summary>One binary operation (<c>+ - * /</c>) on two values <see cref="Enter"/> produced, or
    /// <see langword="null"/> when the result leaves this mode's range. A zero divisor is the caller's §2023:7.3.6.2 SR1c
    /// screen and never reaches here.</summary>
    public abstract CobolDec? Apply(char op, CobolDec left, CobolDec right);

    /// <summary>The SDIDI engine's §2023:8.8.1.5.2 r2 range check raises <see cref="CobolSizeError"/> (EC-SIZE-OVERFLOW /
    /// EC-SIZE-UNDERFLOW); at compile time that is the caller's range diagnostic, never a run-time condition.</summary>
    protected static CobolDec? Guard(Func<CobolDec> operation)
    {
        try { return operation(); }
        catch (CobolSizeError) { return null; }
    }

    private sealed class Standard2002Mode : CompileTimeArithmetic
    {
        public override string RangeDescription =>
            "the standard-arithmetic range (the standard intermediate data item: 32 significant digits, magnitude 10^-999 "
            + "through 10^1000 - 10^968 — ISO §2002:8.8.1.3.1.1, prescribed for compile-time arithmetic by §2002:7.2.5.2 GR1)";

        /// <summary>An operand not already in a standard intermediate data item is converted into one
        /// (§2002:8.8.1.3.1 r1); a 2002 fixed-point numeric literal has at most 31 digits, so the truncation only
        /// serves a value that arrives wider. The item is <see cref="CobolDec.ToSidi"/>, the ONE spelling of the model that
        /// ARITHMETIC IS STANDARD runs at run time too (kb/Work PB5575).</summary>
        public override CobolDec? Enter(CobolDec value) => Guard(() => value.ToSidi());

        /// <summary>Each sum, product and quotient is the exact result truncated to 32 significant digits
        /// (§2002:8.8.1.3.2, §2002:8.8.1.3.4, §2002:8.8.1.3.5) and a subtraction is the sum with the negated second operand
        /// (§2002:8.8.1.3.3). The engine truncates to its own 34 digits first; truncation composes, so cutting that to 32
        /// is the exact result's 32-digit truncation. A result outside the range is §2002:8.8.1.3.1 r2's size error
        /// ("Underflow is treated as a SIZE ERROR and is not rounded to zero"), the caller's range diagnostic here.</summary>
        public override CobolDec? Apply(char op, CobolDec left, CobolDec right) => Guard(() => (op switch
        {
            '+' => CobolDec.Add(left, right, CobolRounding.Truncation),
            '-' => CobolDec.Sub(left, right, CobolRounding.Truncation),
            '*' => CobolDec.Mul(left, right, CobolRounding.Truncation),
            '/' => CobolDec.Div(left, right, CobolRounding.Truncation),
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, "not a compile-time arithmetic operator"),
        }).ToSidi());
    }

    private sealed class StandardDecimalMode : CompileTimeArithmetic
    {
        private const CobolRounding Rounding = CobolRounding.NearestAwayFromZero;

        public override string RangeDescription =>
            "the standard-arithmetic range (the standard-decimal intermediate data item: 34 significant digits, "
            + "magnitude up to 9.99E+6144 — ISO §2023:8.8.1.5.2, prescribed for compile-time arithmetic at 2014 by "
            + "§2014:7.3.5.2 GR1)";

        public override CobolDec? Enter(CobolDec value) => Guard(() => CobolDec.FromParsed(value.Sig, value.Exp, Rounding));

        public override CobolDec? Apply(char op, CobolDec left, CobolDec right) => Guard(() => op switch
        {
            '+' => CobolDec.Add(left, right, Rounding),
            '-' => CobolDec.Sub(left, right, Rounding),
            '*' => CobolDec.Mul(left, right, Rounding),
            '/' => CobolDec.Div(left, right, Rounding),
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, "not a compile-time arithmetic operator"),
        });
    }

    private sealed class SystemDecimalMode : CompileTimeArithmetic
    {
        private const NumberStyles LiteralStyles =
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;

        public override string RangeDescription =>
            "the .NET decimal evaluation range (96-bit, 28–29 significant digits — the documented §2023:7.3.6.2 SR2 "
            + "implementor limit)";

        public override CobolDec? Enter(CobolDec value) =>
            decimal.TryParse(CtNumeric.ToScientificText(value), LiteralStyles, CultureInfo.InvariantCulture, out decimal d)
                ? CtNumeric.FromDecimal(d) : null;

        public override CobolDec? Apply(char op, CobolDec left, CobolDec right)
        {
            decimal l = ToDecimal(left), r = ToDecimal(right);
            try
            {
                return CtNumeric.FromDecimal(op switch
                {
                    '+' => l + r,
                    '-' => l - r,
                    '*' => l * r,
                    '/' => l / r,
                    _ => throw new ArgumentOutOfRangeException(nameof(op), op, "not a compile-time arithmetic operator"),
                });
            }
            catch (OverflowException)
            {
                return null;
            }
        }

        /// <summary>A value <see cref="Enter"/> or <see cref="Apply"/> produced back as the <see cref="decimal"/> it
        /// came from — exact, because it IS a decimal's coefficient and scale.</summary>
        private static decimal ToDecimal(CobolDec v)
        {
            Int128 magnitude = Int128.Abs(v.Sig);
            if (v.Exp > 0 || v.Exp < -28 || magnitude >> 96 != 0)
                throw new InvalidOperationException(
                    $"compile-time System.Decimal operand {CtNumeric.ToScientificText(v)} did not enter through Enter");
            return new decimal((int)(uint)magnitude, (int)(uint)(magnitude >> 32), (int)(uint)(magnitude >> 64),
                v.Sig < 0, (byte)-v.Exp);
        }
    }
}
