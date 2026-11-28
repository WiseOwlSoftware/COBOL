// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Numerics;
using WiseOwl.Cobol.Compiler.Binding;
using WiseOwl.Cobol.Compiler.Binding.Bound;
using WiseOwl.Cobol.Compiler.Binding.Model;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The integer-valued test, its cap, the digit bound and the term comparer of the type 6 classifier (kb/Work PB5891;
/// docs/INTRINSICS_DESIGN.md, the type-6 intake, rules 1, 3 and 4).
/// </summary>
/// <remarks>
/// Every expected value is derived from the polynomial by hand: x (x + 1) is a product of two consecutive integers, so it is even
/// and x (x + 1) / 2 is an integer at every x; x (x + 1) / 3 is 2/3 at x = 1; x / 2 is 0.5 at x = 1 (one fraction digit);
/// (x^13 − x) / 13 is an integer by Fermat's little theorem but has degree 13, past the grid cap of 12, and its coefficients 1/13
/// have no finite decimal, so the test cannot show it and answers null.
/// </remarks>
public sealed class ArgumentPolynomialTests
{
    private static readonly ExactRational Half = ExactRational.One / ExactRational.Of(2);

    private static Polynomial X(int atom = 0) => Polynomial.OfAtom(atom, ExactRational.One);

    private static Polynomial C(int value) => Polynomial.Constant(ExactRational.Of(value));

    private static Polynomial Times(Polynomial a, Polynomial b) => a.Multiply(b)!;

    private static Polynomial Over(Polynomial a, int divisor) => a.Scale(ExactRational.One / ExactRational.Of(divisor));

    [Fact]
    public void TriangularNumber_IsIntegerValued()
    {
        var p = Over(Times(X(), X().Add(C(1), ExactRational.One)), 2);
        Assert.Equal(0, p.FractionDigits(out var witness));
        Assert.Null(witness);
    }

    [Fact]
    public void ProductOfThreeConsecutiveOverSix_IsIntegerValued()
    {
        var p = Over(Times(Times(X(), X().Add(C(1), ExactRational.One)), X().Add(C(2), ExactRational.One)), 6);
        Assert.Equal(0, p.FractionDigits(out _));
    }

    [Fact]
    public void ProductOfTwoConsecutiveOverThree_HasNoFiniteDecimalAtOne()
    {
        var p = Over(Times(X(), X().Add(C(1), ExactRational.One)), 3);
        Assert.Null(p.FractionDigits(out var witness));
        Assert.NotNull(witness);
        Assert.Equal(ExactRational.Of(2) / ExactRational.Of(3), p.Evaluate(witness!));
    }

    [Fact]
    public void HalfOfAnAtom_HasOneFractionDigit()
    {
        var p = Over(X(), 2);
        Assert.Equal(1, p.FractionDigits(out var witness));
        Assert.Equal(Half, p.Evaluate(witness!));
    }

    [Fact]
    public void TwoAtoms_TheGridCoversEachAtomsDegree()
    {
        // x (x + 1) y (y + 1) / 4 is a product of two even numbers over 4; x y / 2 is 1/2 at x = y = 1.
        var xy = Times(Times(X(0), X(0).Add(C(1), ExactRational.One)), Times(X(1), X(1).Add(C(1), ExactRational.One)));
        Assert.Equal(0, Over(xy, 4).FractionDigits(out _));
        Assert.Equal(1, Over(Times(X(0), X(1)), 2).FractionDigits(out _));
    }

    [Fact]
    public void DegreePastTheCap_IsNotShown()
    {
        var p = Over(X().Power(13)!.Add(X(), -ExactRational.One), 13);
        Assert.Null(p.FractionDigits(out var witness));
        Assert.Null(witness);
        // The same identity at degree 11 (x^11 − x is a multiple of 11) is within the cap and shown.
        Assert.Equal(0, Over(X().Power(11)!.Add(X(), -ExactRational.One), 11).FractionDigits(out _));
    }

    [Fact]
    public void LikeTermsCancel_ToTheConstant()
    {
        var p = X().Scale(Half).Add(X().Scale(Half), -ExactRational.One).Add(C(5), ExactRational.One);
        Assert.Equal(0, p.FractionDigits(out _));
        Assert.Single(p.Terms);
    }

    [Fact]
    public void DigitHigh_IsTheLargestMonomialPlusTheCarry()
    {
        // 0.5 x^2 + 0.5 x over |x| < 10^2: 0.5 ≤ 10^0, so each monomial is below 10^4, and two of them sum below 10^5.
        var p = Over(Times(X(), X().Add(C(1), ExactRational.One)), 2);
        Assert.Equal(5, p.DigitHigh(_ => 2));
        Assert.Null(p.DigitHigh(_ => null));
        Assert.Equal(0, Polynomial.Zero.DigitHigh(_ => null));
        // The constant 10 is not below 10^1, so it needs 10^2.
        Assert.Equal(2, C(10).DigitHigh(_ => null));
    }

    private static BoundNumRef Ref(DataItem item) =>
        new(new MemberPlace(new AccessPath([new RootFieldSegment(item.ClrName)]), item));

    private static DataItem Item(string name) => new()
    {
        Level = 1, CobolName = name, ClrName = name,
        Pic = new PicInfo(PicCategory.Numeric, Usage.Display, Length: 2, Digits: 2, Scale: 1, Signed: false),
    };

    private static BoundIntrinsicCall Call(string name, params BoundOperand[] args)
    {
        Assert.True(IntrinsicCatalog.TryGet(name, out var sig));
        return new BoundIntrinsicCall(sig, args, PicCategory.Numeric) { Arithmetic = ArithmeticMode.Native };
    }

    /// <summary>⛔ THE GRID TEST RUNS ONLY WHERE IT IS READ, ONCE PER POLYNOMIAL (kb/Work PB5891 review R1-2): forty additions of 1
    /// above the quotient N * (N + 1) / 2 are Int128-lane intermediates under native arithmetic, which read no fraction digits, so
    /// the classification evaluates the grid for the quotient and for the answer, and for nothing else. A count, never a time.</summary>
    [Fact]
    public void ADeepSum_EvaluatesTheGridOnlyWhereItIsRead()
    {
        var n = Ref(IntegerItem("N"));
        BoundExpr e = new BoundBinary(new BoundBinary(n, '*', new BoundBinary(n, '+', new BoundNumLiteral("1"))), '/', new BoundNumLiteral("2"));
        for (int i = 0; i < 40; i++) e = new BoundBinary(e, '+', new BoundNumLiteral("1"));
        int before = Polynomial.GridEvaluations;
        Assert.True(IntrinsicArgumentRules.IsAlwaysIntegral(e, ArithmeticMode.Native));
        Assert.InRange(Polynomial.GridEvaluations - before, 1, 2);
    }

    private static DataItem IntegerItem(string name) => new()
    {
        Level = 1, CobolName = name, ClrName = name,
        Pic = new PicInfo(PicCategory.Numeric, Usage.Display, Length: 2, Digits: 2, Scale: 0, Signed: false),
    };

    [Fact]
    public void SameTerm_OnePlaceIsOneTerm_TwoItemsAreTwo()
    {
        var s = Item("S");
        Assert.True(IntrinsicArgumentRules.SameTerm(Ref(s), Ref(s)));
        Assert.False(IntrinsicArgumentRules.SameTerm(Ref(s), Ref(Item("S"))));
    }

    private static BoundNumRef Element(DataItem table, DataItem subscript) =>
        new(new MemberPlace(new AccessPath([new RootFieldSegment(table.ClrName),
            new FixedTableSegment(new PositionItemRead(new AccessPath([new RootFieldSegment(subscript.ClrName)]), subscript, PositionReadForm.Subscript))]),
            table));

    /// <summary>kb/Work PB5891 review R1-1: a subscript is part of the place, compared structurally: T (I) twice is one term (each
    /// reference binds its own access path), T (I) and T (J) are two.</summary>
    [Fact]
    public void SameTerm_ASubscriptedElement_IsOneTermOnlyUnderTheSameSubscript()
    {
        var t = Item("T");
        var (i, j) = (IntegerItem("I"), IntegerItem("J"));
        Assert.True(IntrinsicArgumentRules.SameTerm(Element(t, i), Element(t, i)));
        Assert.False(IntrinsicArgumentRules.SameTerm(Element(t, i), Element(t, j)));
    }

    [Fact]
    public void SameTerm_APureFunctionOverOneArgumentIsOneTerm_AnImpureOneNever()
    {
        var s = Item("S");
        Assert.True(IntrinsicArgumentRules.SameTerm(Call("SQRT", new BoundFieldOperand(Ref(s).Place)),
            Call("SQRT", new BoundFieldOperand(Ref(s).Place))));
        Assert.False(IntrinsicArgumentRules.SameTerm(Call("SQRT", new BoundFieldOperand(Ref(s).Place)),
            Call("SQRT", new BoundFieldOperand(Ref(Item("T")).Place))));
        var random = Call("RANDOM");
        Assert.False(IntrinsicArgumentRules.SameTerm(random, random));
        Assert.False(IntrinsicArgumentRules.SameTerm(Call("ABS", new BoundComputedOperand(Call("RANDOM"))),
            Call("ABS", new BoundComputedOperand(Call("RANDOM")))));
    }
}
