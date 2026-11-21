// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ISO §2023:15.3 type 6 — "An arithmetic expression that will always result in an integer value or an integer data item
/// shall be specified" — judged on the expression's VALUE when its non-integral part is a literal constant
/// (kb/Work PB617). <c>IntrinsicArgumentRules.CollectAdditive</c> used to accumulate data items only, so a literal's
/// fraction never entered the integrality net: <c>FUNCTION CHAR(1.5 + 1)</c> (= 2.5) compiled clean while the bare
/// <c>CHAR(1.5)</c> was refused. Each row is an argument operand and whether the screen must refuse it; the admitted
/// rows pin what the screen shows integral (the constant is a SUM — <c>1.5 + 0.5</c> is 2 — and a product by a literal
/// cancels an item's scale), and since kb/Work PB5886 a term it cannot show integral is refused (it fails closed).
/// </summary>
public sealed class IntegerArgumentConstantWitnessTests
{
    private static BoundExpr Lit(string text) => new BoundNumLiteral(text);

    private static BoundExpr Bin(BoundExpr l, char op, BoundExpr r) => new BoundBinary(l, op, r);

    /// <summary>A reference to an elementary numeric item of <paramref name="digits"/> / <paramref name="scale"/>.</summary>
    private static BoundExpr Item(string name, int digits, int scale) =>
        new BoundNumRef(new MemberPlace(
            new AccessPath([new RootFieldSegment(name.Replace('-', '_'))]),
            new DataItem
            {
                Level = 1, CobolName = name, ClrName = name.Replace('-', '_'),
                Pic = new PicInfo(PicCategory.Numeric, Usage.Display, Length: digits, Digits: digits, Scale: scale, Signed: false),
            }));

    private static readonly BoundExpr IntItem = Item("W-I", 3, 0);
    private static readonly BoundExpr ScaledItem = Item("W-S", 2, 1);

    public static TheoryData<string, BoundExpr, Screen> Cases() => new()
    {
        // Refused: the literal constant is not an integer and every other term is one.
        { "1.5 + 1", Bin(Lit("1.5"), '+', Lit("1")), Refuted },
        { "(1.5) parenthesized", Lit("1.5"), Refuted },
        { "W-I + 0.5", Bin(IntItem, '+', Lit("0.5")), Refuted },
        { "W-I * 2 + 0.5", Bin(Bin(IntItem, '*', Lit("2")), '+', Lit("0.5")), Refuted },
        { "3 / 2 + 1", Bin(Bin(Lit("3"), '/', Lit("2")), '+', Lit("1")), Refuted },
        { "1 / 3", Bin(Lit("1"), '/', Lit("3")), Refuted },
        { "W-S - W-S + 0.5", Bin(Bin(ScaledItem, '-', ScaledItem), '+', Lit("0.5")), Refuted },
        { "- 0.25 + 2", Bin(new BoundNegate(Lit("0.25")), '+', Lit("2")), Refuted },
        { "2 ** -1", new BoundPower(Lit("2"), new BoundNegate(Lit("1")), NegativeBaseRealRoots: false), Refuted },
        // kb/Work PB5832: a product or quotient by a literal scales the spine, so an item's net coefficient is a rational and a step of
        // the item (0.1 for W-S, 1 for W-I) moves the sum by 0.2 or 0.5: W-S * 2 + 65.5 is 65.7 at W-S = 0.1, W-I / 2 + 0.5 is 1.5 at W-I = 2.
        { "W-S * 2 + 65.5", Bin(Bin(ScaledItem, '*', Lit("2")), '+', Lit("65.5")), Refuted },
        { "W-I / 2 + 0.5", Bin(Bin(IntItem, '/', Lit("2")), '+', Lit("0.5")), Refuted },
        { "W-I * 0.5", Bin(IntItem, '*', Lit("0.5")), Refuted },
        { "0.5 * W-I", Bin(Lit("0.5"), '*', IntItem), Refuted },
        // kb/Work PB5832 review R1-1: a product of two items (both at one granule) under a non-integral factor.
        { "W-S * W-S * 0.5", Bin(Bin(ScaledItem, '*', ScaledItem), '*', Lit("0.5")), Refuted },
        { "W-I * W-I / 2", Bin(Bin(IntItem, '*', IntItem), '/', Lit("2")), Refuted },
        // kb/Work PB5889: neither proven nor refuted, so checked at run time: a zero divisor gives no value at all, the reciprocal of 3 does not terminate (W-I / 3 * 3 is exactly W-I), and a quotient by an item is no polynomial.
        { "W-I / 3 * 3", Bin(Bin(IntItem, '/', Lit("3")), '*', Lit("3")), Checked },
        { "W-I / W-I", Bin(IntItem, '/', IntItem), Checked },
        { "1 / 0 + 0.5", Bin(Bin(Lit("1"), '/', Lit("0")), '+', Lit("0.5")), Checked },
        { "1 / 3 * 3", Bin(Bin(Lit("1"), '/', Lit("3")), '*', Lit("3")), Checked },
        // Admitted: the expression is shown to be an integer for every valuation.
        { "1.5 + 0.5", Bin(Lit("1.5"), '+', Lit("0.5")), Proven },
        { "3 - 1", Bin(Lit("3"), '-', Lit("1")), Proven },
        { "6.5E1", Bin(Lit("6.5E1"), '+', Lit("0")), Proven },
        { "W-I + 1.5 + 1.5", Bin(Bin(IntItem, '+', Lit("1.5")), '+', Lit("1.5")), Proven },
        { "0.5 * 4", Bin(Lit("0.5"), '*', Lit("4")), Proven },
        { "2 ** 6 + 1", Bin(new BoundPower(Lit("2"), Lit("6"), NegativeBaseRealRoots: false), '+', Lit("1")), Proven },
        // The products by a literal that cancel to a whole net coefficient (kb/Work PB5832).
        { "W-I * 0.5 * 2", Bin(Bin(IntItem, '*', Lit("0.5")), '*', Lit("2")), Proven },
        { "W-I * 0.5 + W-I * 0.5", Bin(Bin(IntItem, '*', Lit("0.5")), '+', Bin(IntItem, '*', Lit("0.5"))), Proven },
        { "W-S * 10", Bin(ScaledItem, '*', Lit("10")), Proven },
        // kb/Work PB5886 round 2: a quotient by a literal whose reciprocal terminates is the product by that reciprocal.
        { "W-I / 2 * 2", Bin(Bin(IntItem, '/', Lit("2")), '*', Lit("2")), Proven },
        { "W-S / 0.1", Bin(ScaledItem, '/', Lit("0.1")), Proven },
        { "W-I * 0", Bin(IntItem, '*', Lit("0")), Proven },
        { "W-I * W-I * 2", Bin(Bin(IntItem, '*', IntItem), '*', Lit("2")), Proven },
        { "W-I * W-I", Bin(IntItem, '*', IntItem), Proven },
        { "4 / 2 + W-I", Bin(Bin(Lit("4"), '/', Lit("2")), '+', IntItem), Proven },
    };

    /// <summary>The three outcomes of the screen (kb/Work PB5889): proven, refuted with a counterexample, or checked at run time.</summary>
    public enum Screen { Proven, Refuted, Checked }

    private const Screen Proven = Screen.Proven, Refuted = Screen.Refuted, Checked = Screen.Checked;

    /// <summary>The outcome the screen gives the argument: COBOL1627 (<c>IntegerViolation</c>), the run-time check
    /// (<c>NeedsRunTimeIntegerCheck</c>), or neither.</summary>
    private static (Screen Outcome, string? Why) OutcomeOf(BoundExpr expr, ArithmeticMode arithmetic)
    {
        var op = new BoundComputedOperand(expr);
        return IntrinsicArgumentRules.IntegerViolation(op, arithmetic, EditionInfo.Latest) is { } why ? (Refuted, (string?)why)
            : IntrinsicArgumentRules.NeedsRunTimeIntegerCheck(op, arithmetic) ? (Checked, null) : (Proven, null);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void TheIntegerScreen_JudgesTheLiteralConstantByValue(string shape, BoundExpr expr, Screen expected)
    {
        var (outcome, why) = OutcomeOf(expr, ArithmeticMode.Native);
        Assert.True(outcome == expected,
            $"'{shape}': expected {expected}, got {outcome} {why} (kb/Work PB617: the sum folds its literal terms; PB5889: a refusal needs "
            + "a value of the operands at which the expression is not an integer, and an argument neither proven nor refuted is checked at run time).");
    }

    /// <summary>A 27-digit unsigned integer item, all nines at its largest.</summary>
    private static readonly BoundExpr Big27 = Item("B27", 27, 0);

    /// <summary><c>B27 / 0.0001 / 16384 * 16384 * 0.0001</c> (kb/Work PB5886 review R3-1): B27 for every value in exact arithmetic. Its
    /// quotient by 16384, B27 × 0.6103515625, has up to 27 integer and 10 fraction digits. Native arithmetic keeps them (the nested
    /// quotient's guard digits), and the expression prints B27. A standard-decimal intermediate keeps 34 significant digits
    /// (§2023:8.8.1.5.2), so at B27 = 999…9 the expression prints …998.9999. The 2002 standard intermediate keeps 32 (§2002:8.8.1.3.1.1)
    /// and, at B27 = 999…9, happens to print …999.0000 (review R4-2); the screen refuses there on the bound, which a value with fraction
    /// digits beyond 32 significant ones exceeds, not on that one valuation.</summary>
    private static readonly BoundExpr Big27Round =
        Bin(Bin(Bin(Bin(Big27, '/', Lit("0.0001")), '/', Lit("16384")), '*', Lit("16384")), '*', Lit("0.0001"));

    public static TheoryData<string, ArithmeticMode, BoundExpr, Screen> ArithmeticCases() => new()
    {
        { "B27 / 0.0001 / 16384 * 16384 * 0.0001, native", ArithmeticMode.Native, Big27Round, Proven },
        { "B27 / 0.0001 / 16384 * 16384 * 0.0001, standard-decimal", ArithmeticMode.StandardDecimal, Big27Round, Checked },
        { "B27 / 0.0001 / 16384 * 16384 * 0.0001, standard (2002)", ArithmeticMode.Standard, Big27Round, Checked },
        { "W-I / 2 * 2, standard-decimal", ArithmeticMode.StandardDecimal, Bin(Bin(IntItem, '/', Lit("2")), '*', Lit("2")), Proven },
        { "W-S * 10, standard (2002)", ArithmeticMode.Standard, Bin(ScaledItem, '*', Lit("10")), Proven },
        { "B27 * 3, standard-decimal (an integer intermediate may round and stays an integer)", ArithmeticMode.StandardDecimal,
            Bin(Big27, '*', Lit("3")), Proven },
        // Review R4-1: a power is bounded by its base, so ** 1 does not hide the base's rounded intermediate.
        { "(B27 / 0.0001 / 16384 * 16384 * 0.0001) ** 1, native", ArithmeticMode.Native,
            new BoundPower(Big27Round, Lit("1"), NegativeBaseRealRoots: false), Proven },
        { "(B27 / 0.0001 / 16384 * 16384 * 0.0001) ** 1, standard-decimal", ArithmeticMode.StandardDecimal,
            new BoundPower(Big27Round, Lit("1"), NegativeBaseRealRoots: false), Checked },
        { "(W-I / 2 * 2) ** 2, standard-decimal", ArithmeticMode.StandardDecimal,
            new BoundPower(Bin(Bin(IntItem, '/', Lit("2")), '*', Lit("2")), Lit("2"), NegativeBaseRealRoots: false), Proven },
    };

    [Theory]
    [MemberData(nameof(ArithmeticCases))]
    public void TheIntegerScreen_AsksTheArithmeticInForce(string shape, ArithmeticMode arithmetic, BoundExpr expr, Screen expected)
    {
        var (outcome, why) = OutcomeOf(expr, arithmetic);
        Assert.True(outcome == expected,
            $"'{shape}': expected {expected} under {arithmetic} arithmetic, whose intermediates keep "
            + $"{ArithmeticModes.IntermediateSignificantDigits(arithmetic)?.ToString() ?? "every integer"} digits (kb/Work PB5886 R3-1); got {outcome} {why}");
    }
}
