// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
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

    public static TheoryData<string, BoundExpr, bool> Cases() => new()
    {
        // Refused: the literal constant is not an integer and every other term is one.
        { "1.5 + 1", Bin(Lit("1.5"), '+', Lit("1")), true },
        { "(1.5) parenthesized", Lit("1.5"), true },
        { "W-I + 0.5", Bin(IntItem, '+', Lit("0.5")), true },
        { "W-I * 2 + 0.5", Bin(Bin(IntItem, '*', Lit("2")), '+', Lit("0.5")), true },
        { "3 / 2 + 1", Bin(Bin(Lit("3"), '/', Lit("2")), '+', Lit("1")), true },
        { "1 / 3", Bin(Lit("1"), '/', Lit("3")), true },
        { "W-S - W-S + 0.5", Bin(Bin(ScaledItem, '-', ScaledItem), '+', Lit("0.5")), true },
        { "- 0.25 + 2", Bin(new BoundNegate(Lit("0.25")), '+', Lit("2")), true },
        { "2 ** -1", new BoundPower(Lit("2"), new BoundNegate(Lit("1")), NegativeBaseRealRoots: false), true },
        // kb/Work PB5832: a product or quotient by a literal scales the spine, so an item's net coefficient is a rational and a step of
        // the item (0.1 for W-S, 1 for W-I) moves the sum by 0.2 or 0.5: W-S * 2 + 65.5 is 65.7 at W-S = 0.1, W-I / 2 + 0.5 is 1.5 at W-I = 2.
        { "W-S * 2 + 65.5", Bin(Bin(ScaledItem, '*', Lit("2")), '+', Lit("65.5")), true },
        { "W-I / 2 + 0.5", Bin(Bin(IntItem, '/', Lit("2")), '+', Lit("0.5")), true },
        { "W-I * 0.5", Bin(IntItem, '*', Lit("0.5")), true },
        { "0.5 * W-I", Bin(Lit("0.5"), '*', IntItem), true },
        // kb/Work PB5832 review R1-1: a product of two items (both at one granule) under a non-integral factor.
        { "W-S * W-S * 0.5", Bin(Bin(ScaledItem, '*', ScaledItem), '*', Lit("0.5")), true },
        { "W-I * W-I / 2", Bin(Bin(IntItem, '*', IntItem), '/', Lit("2")), true },
        // kb/Work PB5886: the screen fails closed; a zero divisor gives no value at all, and the reciprocal of 3 does not terminate.
        { "W-I / 3 * 3", Bin(Bin(IntItem, '/', Lit("3")), '*', Lit("3")), true },
        { "W-I / W-I", Bin(IntItem, '/', IntItem), true },
        { "1 / 0 + 0.5", Bin(Bin(Lit("1"), '/', Lit("0")), '+', Lit("0.5")), true },
        { "1 / 3 * 3", Bin(Bin(Lit("1"), '/', Lit("3")), '*', Lit("3")), true },
        // Admitted: the expression is shown to be an integer for every valuation.
        { "1.5 + 0.5", Bin(Lit("1.5"), '+', Lit("0.5")), false },
        { "3 - 1", Bin(Lit("3"), '-', Lit("1")), false },
        { "6.5E1", Bin(Lit("6.5E1"), '+', Lit("0")), false },
        { "W-I + 1.5 + 1.5", Bin(Bin(IntItem, '+', Lit("1.5")), '+', Lit("1.5")), false },
        { "0.5 * 4", Bin(Lit("0.5"), '*', Lit("4")), false },
        { "2 ** 6 + 1", Bin(new BoundPower(Lit("2"), Lit("6"), NegativeBaseRealRoots: false), '+', Lit("1")), false },
        // The products by a literal that cancel to a whole net coefficient (kb/Work PB5832).
        { "W-I * 0.5 * 2", Bin(Bin(IntItem, '*', Lit("0.5")), '*', Lit("2")), false },
        { "W-I * 0.5 + W-I * 0.5", Bin(Bin(IntItem, '*', Lit("0.5")), '+', Bin(IntItem, '*', Lit("0.5"))), false },
        { "W-S * 10", Bin(ScaledItem, '*', Lit("10")), false },
        // kb/Work PB5886 round 2: a quotient by a literal whose reciprocal terminates is the product by that reciprocal.
        { "W-I / 2 * 2", Bin(Bin(IntItem, '/', Lit("2")), '*', Lit("2")), false },
        { "W-S / 0.1", Bin(ScaledItem, '/', Lit("0.1")), false },
        { "W-I * 0", Bin(IntItem, '*', Lit("0")), false },
        { "W-I * W-I * 2", Bin(Bin(IntItem, '*', IntItem), '*', Lit("2")), false },
        { "W-I * W-I", Bin(IntItem, '*', IntItem), false },
        { "4 / 2 + W-I", Bin(Bin(Lit("4"), '/', Lit("2")), '+', IntItem), false },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void TheIntegerScreen_JudgesTheLiteralConstantByValue(string shape, BoundExpr expr, bool refused)
    {
        string? why = IntrinsicArgumentRules.IntegerViolation(new BoundComputedOperand(expr), ArithmeticMode.Native);
        if (refused)
            Assert.True(why is not null,
                $"'{shape}' is not shown to be an integer for every valuation, so ISO §2023:15.3 type 6 does not admit it — but "
                + "the integer screen admitted it (kb/Work PB617: the sum must fold its literal terms; PB5886: the screen fails closed).");
        else
            Assert.True(why is null,
                $"'{shape}' must stay admitted (the screen shows it is an integer for every valuation) — but the screen "
                + $"refused it: {why}");
    }

    /// <summary>A 27-digit unsigned integer item, all nines at its largest.</summary>
    private static readonly BoundExpr Big27 = Item("B27", 27, 0);

    /// <summary><c>B27 / 0.0001 / 16384 * 16384 * 0.0001</c> (kb/Work PB5886 review R3-1): B27 for every value in exact arithmetic. Its
    /// quotient by 16384 has up to 32 integer and 14 fraction digits: the native quotient keeps its 14 fraction digits (the nested
    /// quotient's guard), a standard-decimal intermediate keeps 34 significant digits (§2023:8.8.1.5.2) and the 2002 standard one 32
    /// (§2002:8.8.1.3.1.1), so at B27 = 999…9 the value is …998.9999 under both standard arithmetics.</summary>
    private static readonly BoundExpr Big27Round =
        Bin(Bin(Bin(Bin(Big27, '/', Lit("0.0001")), '/', Lit("16384")), '*', Lit("16384")), '*', Lit("0.0001"));

    public static TheoryData<string, ArithmeticMode, BoundExpr, bool> ArithmeticCases() => new()
    {
        { "B27 / 0.0001 / 16384 * 16384 * 0.0001, native", ArithmeticMode.Native, Big27Round, false },
        { "B27 / 0.0001 / 16384 * 16384 * 0.0001, standard-decimal", ArithmeticMode.StandardDecimal, Big27Round, true },
        { "B27 / 0.0001 / 16384 * 16384 * 0.0001, standard (2002)", ArithmeticMode.Standard, Big27Round, true },
        { "W-I / 2 * 2, standard-decimal", ArithmeticMode.StandardDecimal, Bin(Bin(IntItem, '/', Lit("2")), '*', Lit("2")), false },
        { "W-S * 10, standard (2002)", ArithmeticMode.Standard, Bin(ScaledItem, '*', Lit("10")), false },
        { "B27 * 3, standard-decimal (an integer intermediate may round and stays an integer)", ArithmeticMode.StandardDecimal,
            Bin(Big27, '*', Lit("3")), false },
    };

    [Theory]
    [MemberData(nameof(ArithmeticCases))]
    public void TheIntegerScreen_AsksTheArithmeticInForce(string shape, ArithmeticMode arithmetic, BoundExpr expr, bool refused)
    {
        string? why = IntrinsicArgumentRules.IntegerViolation(new BoundComputedOperand(expr), arithmetic);
        Assert.True(refused == why is not null,
            $"'{shape}': expected {(refused ? "refused" : "admitted")} under {arithmetic} arithmetic, whose intermediates keep "
            + $"{ArithmeticModes.IntermediateSignificantDigits(arithmetic)?.ToString() ?? "every integer"} digits (kb/Work PB5886 R3-1); got {why ?? "admitted"}");
    }
}
