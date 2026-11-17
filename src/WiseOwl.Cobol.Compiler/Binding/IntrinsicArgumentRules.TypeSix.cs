// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Numerics;
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Binding.Procedure;

namespace WiseOwl.Cobol.Binding;

/// <summary>⛔ THE TYPE 6 CLASSIFIER: "is this arithmetic expression always an integer", decided exactly by a polynomial normal form
/// (kb/Work PB5886, PB5891; docs/INTRINSICS_DESIGN.md, the type-6 intake, which states the design this file builds).</summary>
internal static partial class IntrinsicArgumentRules
{
    /// <summary>⛔ THE ONE ANSWER TO "IS THIS ARITHMETIC EXPRESSION ALWAYS AN INTEGER" (kb/Work PB5886 round 2, R2-1; PB5891): the type 6
    /// screen refuses an argument exactly when this is false, and <c>IntrinsicResultType.IsIntegerArgument</c> reads it for the type tables
    /// of the functions whose type depends on their arguments, so <c>FUNCTION MAX (N * (N + 1) / 2)</c> is an integer function as
    /// <c>N * (N + 1) / 2</c> is an integer argument (§2023:15.3 AR6; §1989:A.2.2 AR4).</summary>
    internal static bool IsAlwaysIntegral(BoundExpr expr, ArithmeticMode arithmetic) => NotAlwaysIntegral(expr, arithmetic) is null;

    /// <summary>⛔ THE TYPE 6 SCREEN OF AN ARITHMETIC-EXPRESSION ARGUMENT FAILS CLOSED AND DECIDES EXACTLY (kb/Work PB5886, PB5891): the
    /// reason the expression is not SHOWN to result in an integer for every valuation of its terms, or null when it is.
    /// <para>§2023:15.3 AR6: "An arithmetic expression that will always result in an integer value or an integer data item shall be
    /// specified"; §1989:A.2.2 AR4: "An arithmetic expression which will always result in an integer value must be specified" (both
    /// cite.py --check OK). The requirement is on the PROGRAM and sets no method of proof. The classifier builds the expression's value
    /// as a <see cref="Polynomial"/> in its atoms (<see cref="Build"/>), showing every intermediate exact on the way
    /// (<see cref="Intermediate"/>), and admits it when no real atom is left and the polynomial is integer-valued
    /// (<see cref="Polynomial.FractionDigits"/> is 0): <c>N * (N + 1) / 2</c> and <c>FUNCTION SQRT (N) - FUNCTION SQRT (N) + 5</c> are
    /// admitted, <c>FUNCTION ORD (A) / 2</c> and <c>S2 - FUNCTION MAX (S2)</c> refused. The verdict is the same at every edition (the
    /// requirement is the same in both texts); the ARITHMETIC in force decides which intermediates are exact.</para></summary>
    private static string? NotAlwaysIntegral(BoundExpr expr, ArithmeticMode arithmetic)
    {
        var ctx = new TypeSixContext(arithmetic);
        var built = Build(expr, ctx);
        if (built.Why is { } why) return why;
        var value = built.Value!;
        if (value.FirstRealAtom(ctx.IsReal) is { } real) return RealTermWhy(ctx.Atoms[real].Term);
        int? digits = value.FractionDigits(out var witness);
        if (digits == 0) return null;
        return witness switch
        {
            null => "is an arithmetic expression whose value the classifier cannot show to be an integer: its fractional coefficients "
                + $"would need the integer-valued test past its cap (degree {Polynomial.MaxGridDegree} in a term, "
                + $"{Polynomial.MaxGridPoints} points) — " + TypeSixExpressionRule,
            { Count: 0 } => $"is an arithmetic expression whose literal terms sum to {value.Evaluate(witness)} and whose every other term "
                + "cancels or is an integer, so its value is never an integer — " + TypeSixExpressionRule,
            _ => $"is an arithmetic expression whose value is not an integer for every value of its terms: at {ctx.PointText(witness)} "
                + $"it is {value.Evaluate(witness)} — " + TypeSixExpressionRule,
        };
    }

    /// <summary>The rule every type 6 expression refusal cites.</summary>
    private const string TypeSixExpressionRule =
        "ISO §2023:15.3 type 6 (at COBOL-85 §1989:A.2.2 AR4) admits an arithmetic expression only when it ALWAYS results in an integer value";

    /// <summary>The atoms of one classification and the arithmetic in force: whether it is a decimal engine (every operation on the
    /// SDIDI) and its significant-digit cap (null for native arithmetic, whose lanes say their own, <see cref="NumericLanes"/>).</summary>
    private sealed class TypeSixContext(ArithmeticMode arithmetic)
    {
        public readonly List<PolynomialAtom> Atoms = [];
        public readonly int? Cap = ArithmeticModes.IntermediateSignificantDigits(arithmetic);
        public readonly bool DecimalEngine = ArithmeticModes.IsDecimalEngine(arithmetic);

        /// <summary>The lane a leaf arrives on: the SDIDI under a decimal engine, else the bind-time carrier of the leaf
        /// (<see cref="FunctionValueLane.LaneOf(BoundExpr)"/>).</summary>
        public NumericLane LeafLane(BoundExpr leaf) => DecimalEngine ? NumericLane.Sdidi : FunctionValueLane.LaneOf(leaf);

        /// <summary>The lane of an operation over operands on <paramref name="left"/> and <paramref name="right"/> (an argument has no
        /// receiver).</summary>
        public NumericLane Combine(NumericLane left, NumericLane right) =>
            NumericLanes.Combine(left, right, floatReceiver: false, DecimalEngine);

        /// <summary>The SDIDI lane as a diagnostic names it: the ARITHMETIC clause in force, or native arithmetic's SDIDI lane.</summary>
        public readonly string LaneName = arithmetic switch
        {
            ArithmeticMode.Standard => "under ARITHMETIC IS STANDARD",
            ArithmeticMode.StandardDecimal => "under ARITHMETIC IS STANDARD-DECIMAL",
            _ => "on the SDIDI, which native arithmetic evaluates an operation on beside a floating-point literal, a numeric "
                + "function's value or another SDIDI value (CONFORMANCE.md A.1 item 82)",
        };

        public bool IsReal(int atom) => !Atoms[atom].Integer;

        public int? DigitsOf(int atom) => Atoms[atom].Digits;

        /// <summary>The index of the atom <paramref name="term"/> is: an earlier atom that is the same pure term
        /// (<see cref="SameTerm"/>), or a new one.</summary>
        public int AtomOf(BoundExpr term, bool integer, int? digits, int scale)
        {
            for (int i = 0; i < Atoms.Count; i++)
                if (SameTerm(Atoms[i].Term, term)) return i;
            Atoms.Add(new PolynomialAtom(term, integer, digits, scale));
            return Atoms.Count - 1;
        }

        /// <summary>A new integer atom no other occurrence is: an intermediate whose integer value the arithmetic rounds (it stays an
        /// integer of unknown magnitude).</summary>
        public int FreshInteger(BoundExpr term)
        {
            Atoms.Add(new PolynomialAtom(term, Integer: true, Digits: null, Scale: 0));
            return Atoms.Count - 1;
        }

        /// <summary>A valuation as a diagnostic names it: each atom's term and value (an item's value at its scale). Two atoms that
        /// print alike (T (I) and T (J) both print T) are told apart by their order of reference.</summary>
        public string PointText(IReadOnlyDictionary<int, BigInteger> point)
        {
            var texts = point.Keys.Select(k => ExprText(Atoms[k].Term)).ToList();
            var seen = new Dictionary<string, int>(StringComparer.Ordinal);
            return string.Join(", ", point.Select((kv, i) =>
            {
                string text = texts[i];
                if (texts.Count(t => t == text) > 1) text += $" (reference {seen[text] = seen.GetValueOrDefault(text) + 1})";
                return $"{text} = {ExactRational.FromScaled(kv.Value, -Atoms[kv.Key].Scale)}";
            }));
        }
    }

    /// <summary>What <see cref="Build"/> learns of a node: its value polynomial, or the reason it is not shown; and the lane the code
    /// generator evaluates it on (<see cref="NumericLanes"/>, the one rule <c>NumericRenderer.CombineCore</c> dispatches on).</summary>
    private readonly record struct Built(Polynomial? Value, string? Why, NumericLane Lane)
    {
        public static Built Fail(string why, NumericLane lane) => new(null, why, lane);
    }

    /// <summary>The lane the code generator evaluates <paramref name="expr"/>'s root operation on, as the classifier predicts it (kb/Work
    /// PB5891 review R1-3): the hook <c>NumericLaneDriftTests</c> holds to the lane the rendered C# shows.</summary>
    internal static NumericLane ArgumentLane(BoundExpr expr, ArithmeticMode arithmetic) => Build(expr, new TypeSixContext(arithmetic)).Lane;

    /// <summary>⛔ THE POLYNOMIAL OF A NODE (docs/INTRINSICS_DESIGN.md, the type-6 intake, rules 1 and 2): every concrete
    /// <see cref="BoundExpr"/> kind is named, and the default refuses (<c>TypeSixWalkCoverageDriftTests</c>).</summary>
    private static Built Build(BoundExpr e, TypeSixContext ctx)
    {
        // A LITERAL-ONLY SUBTREE is a constant at its exact fold, trusted only when the arithmetic stores every quotient in it exactly
        // (kb/Work PB4758: `1 / 3 * 3` folds to 1 and the arithmetic stores 0.9999).
        if (ExactValue(e) is { } constant)
            return QuotientsAreExact(e) ? new(Polynomial.Constant(constant), null, ctx.LeafLane(e)) : Built.Fail(InexactLiteralWhy(constant), ctx.LeafLane(e));
        switch (e)
        {
            case BoundBinary { Op: '+' or '-' } b:
            {
                var (l, r) = (Build(b.Left, ctx), Build(b.Right, ctx));
                var lane = ctx.Combine(l.Lane, r.Lane);
                if (l.Why is not null) return l with { Lane = lane };
                if (r.Why is not null) return r with { Lane = lane };
                var k = b.Op == '-' ? -ExactRational.One : ExactRational.One;
                return Intermediate(e, l.Value!.Add(r.Value!, k), lane, quotient: false, ctx);
            }
            case BoundBinary { Op: '*' } b:
            {
                var (l, r) = (Build(b.Left, ctx), Build(b.Right, ctx));
                var lane = ctx.Combine(l.Lane, r.Lane);
                if (l.Why is not null) return l with { Lane = lane };
                if (r.Why is not null) return r with { Lane = lane };
                return l.Value!.Multiply(r.Value!) is { } product ? Intermediate(e, product, lane, quotient: false, ctx) : Unexpanded(e, lane, ctx);
            }
            // ⛔ A QUOTIENT BY A NONZERO LITERAL c IS THE PRODUCT BY 1/c, exact when its own value keeps within the guard digits a nested
            // quotient keeps (Intermediate): `WN / 2 * 2` and `N * (N + 1) / 2` are admitted, `WN / 3 * 3` refused.
            case BoundBinary { Op: '/', Left: var dividend, Right: var divisor } when ExactValue(divisor) is { Sign: not 0 } c
                && QuotientsAreExact(divisor):
            {
                var d = Build(dividend, ctx);
                if (d.Why is not null) return d;
                return Intermediate(e, d.Value!.Scale(ExactRational.One / c), ctx.Combine(d.Lane, ctx.LeafLane(divisor)), quotient: true, ctx);
            }
            // ⛔ ANY OTHER QUOTIENT IS NO POLYNOMIAL (kb/Work PB5886): a divisor that is an item, a function or an expression moves with
            // the valuation (A / A is a size error at A = 0).
            case BoundBinary { Op: '/', Left: var dividend, Right: var divisor }:
                return Built.Fail($"is an arithmetic expression that divides {TermText(dividend)} by {TermText(divisor)}; a quotient "
                    + "is a polynomial only by a nonzero literal, so the expression is not shown to always result in an integer value — "
                    + TypeSixExpressionRule, ctx.LeafLane(e));
            case BoundBinary:
                return Built.Fail(NotShownWhy(e), ctx.LeafLane(e));
            // A negation is exact whatever it negates.
            case BoundNegate n:
            {
                var o = Build(n.Operand, ctx);
                return o.Why is null ? o with { Value = o.Value!.Scale(-ExactRational.One) } : o;
            }
            // A power to a non-negative integer literal is the product of that many bases.
            case BoundPower pw when ExactValue(pw.Exp) is { IsInteger: true, Sign: >= 0 } n && QuotientsAreExact(pw.Exp)
                && n.Numerator <= MaxFoldedExponent:
            {
                var b = Build(pw.Base, ctx);
                if (b.Why is not null) return b;
                // The renderer reads an integer-valued literal exponent as a scale-0 Int128 operand (NumericRenderer.IntegerValuedLiteral).
                var lane = NumericLanes.Power(b.Lane, NumericLane.Scaled, exponentIsIntegerByValue: true, literalOne: n.Numerator.IsOne, ctx.DecimalEngine);
                return b.Value!.Power((int)n.Numerator) is { } power
                    ? Intermediate(e, power, lane, quotient: false, ctx) : Unexpanded(e, lane, ctx);
            }
            // ⛔ THE ONE RULE OF ANY OTHER INTEGER POWER (kb/Work PB5886 round 2): an integer-valued base to an exponent that cannot be
            // negative, or the literal 1 to an integer exponent, is an integer atom (`N ** U`, `1 ** M`); any other power is not shown.
            case BoundPower pw:
            {
                var b = Build(pw.Base, ctx);
                if (b.Why is not null) return b;
                var x = Build(pw.Exp, ctx);
                bool exponentIntegral = x is { Why: null, Value: { } xv } && IsIntegerValued(xv, ctx);
                // ⛔ THE LANE ASKS THE EXPONENT'S CARRIER, AS THE RENDERER DOES (kb/Work PB5891 review R2-1): an exponent integer by its
                // polynomial but not by its carrier (`N ** (U / 2 * 2)`) is evaluated as the engine's binary64 approximation, which no
                // integer atom may stand for.
                var lane = NumericLanes.Power(b.Lane, x.Lane, !ctx.DecimalEngine && FunctionValueLane.IsIntegerCarrier(pw.Exp),
                    literalOne: false, ctx.DecimalEngine);
                if (IsIntegerValued(b.Value!, ctx) && IsIntegerPower(pw.Base, pw.Exp, exponentIntegral) && !NumericLanes.IsBinary64(lane))
                    return new(Polynomial.OfAtom(ctx.AtomOf(e, integer: true, digits: null, scale: 0), ExactRational.One), null, lane);
                if (NumericLanes.IsBinary64(lane) && exponentIntegral && IsIntegerValued(b.Value!, ctx))
                    return Built.Fail($"is an arithmetic expression that raises {TermText(pw.Base)} to the power {TermText(pw.Exp)}, an "
                        + "integer whose carrier is not one, which native arithmetic evaluates as a binary64 approximation, so its value "
                        + "is not shown to be an integer — " + TypeSixExpressionRule, lane);
                return Built.Fail($"is an arithmetic expression that raises {TermText(pw.Base)} to the power {TermText(pw.Exp)}, which can "
                    + "be negative or is not an integer, so its value is not shown to be an integer — " + TypeSixExpressionRule, ctx.LeafLane(e));
            }
            // A FIXED-POINT ITEM of scale s is 10^(−s) × x, x an integer atom below 10^digits; a FLOATING-POINT item is a real atom (its
            // declared value set holds non-integers, §2023:14.6.8.3, kb/Work PB248) and puts its operation on the binary64 lane.
            case BoundNumRef { Place.DescribedItem: { IsGroup: false, Pic: { Category: PicCategory.Numeric } p } } item:
                return p.IsFloat
                    ? new(Polynomial.OfAtom(ctx.AtomOf(item, integer: false, digits: null, scale: 0), ExactRational.One), null, ctx.LeafLane(item))
                    : new(Polynomial.OfAtom(ctx.AtomOf(item, integer: true, p.Usage is Usage.Index ? null : p.Digits, p.Scale),
                        ExactRational.FromScaled(1, -p.Scale)), null, ctx.LeafLane(item));
            // ⛔ A FUNCTION IS A TERM, judged by its type (kb/Work PB5886): an integer function is an integer atom, any other a real atom
            // that must cancel (§2023:8.4.3.2.3 SR11 reads a numeric function's value as no integer, whatever this reference yields).
            case BoundIntrinsicCall { ResultType: IntrinsicType.Integer } call:
                return new(Polynomial.OfAtom(ctx.AtomOf(call, integer: true, digits: null, scale: 0), ExactRational.One), null, ctx.LeafLane(call));
            case BoundIntrinsicCall call:
                return new(Polynomial.OfAtom(ctx.AtomOf(call, integer: false, digits: null, scale: 0), ExactRational.One), null, ctx.LeafLane(call));
            // An index-name's value is an occurrence number (§2023:13.18.38); a report VARYING counter and an OCCURS DEPENDING extent are
            // scale-0 integers by construction; a counter register is an unsigned integer data item (§2023:8.4.3.14.4 GR1).
            case BoundIndexRef or BoundReportVaryingRef or BoundOdoExtent:
            case BoundLinageCounterRef or BoundReportCounterRef when AlgebraicRanges.IsCounterRegister(e):
                return new(Polynomial.OfAtom(ctx.AtomOf(e, integer: true, digits: null, scale: 0), ExactRational.One), null, ctx.LeafLane(e));
            // The windows that carry a user function's activations hold the expression they evaluate.
            case BoundUdfEvaluatedExpr udf:
                return Build(udf.Inner, ctx);
            // An operand already refused where it was bound: its site reported the rule it breaks, and nothing more is said of it.
            case BoundExprError:
                return new(Polynomial.Zero, null, NumericLane.Scaled);
            // ⛔ THE DEFAULT REFUSES (review R4-1): a BoundNumRef that is no numeric item, a BoundNumLiteral the fold cannot read, a
            // register that is no counter register, and any node kind added later are not shown.
            case BoundNumRef or BoundNumLiteral or BoundLinageCounterRef or BoundReportCounterRef:
            default:
                return Built.Fail(NotShownWhy(e), ctx.LeafLane(e));
        }
    }

    /// <summary>⛔ EXACTNESS FIRST, ON THE LANE THE CODE GENERATOR EVALUATES IT ON (docs/INTRINSICS_DESIGN.md, the type-6 intake, rule 4;
    /// kb/Work PB5886 rounds 3 to 5; PB5891 review R1-3): the intermediate <paramref name="e"/>, whose exact value is
    /// <paramref name="value"/>, as <paramref name="lane"/> (<see cref="NumericLanes.Combine"/>, the rule <c>NumericRenderer.CombineCore</c>
    /// dispatches on) computes it. (a) A real monomial is not shown exact. (b) A quotient's value keeps within the
    /// <see cref="ExactFractionDigits"/> guard digits every nested quotient keeps. (c) On binary64 the value is an integer (binary64
    /// holds an integer below 10^15 exactly, and rounds a larger one to an integer). (d) On the SDIDI (every operation under a decimal
    /// engine; natively, an operation beside a floating-point literal, a numeric function's value or another SDIDI value) a value with
    /// fraction digits fits the lane's significant digits (<see cref="NumericLanes.SignificantDigits"/>), and an integer that does not
    /// is rounded to an integer. An integer the arithmetic rounds is a fresh integer atom from here up: its value is SOME integer, no
    /// longer the polynomial. The integer-valued test runs only where (b), (c) or (d) reads it (review R1-2), once per polynomial.</summary>
    private static Built Intermediate(BoundExpr e, Polynomial value, NumericLane lane, bool quotient, TypeSixContext ctx)
    {
        if (value.FirstRealAtom(ctx.IsReal) is { } real) return Built.Fail(RealIntermediateWhy(e, ctx.Atoms[real].Term), lane);
        bool binary64 = NumericLanes.IsBinary64(lane);
        int? cap = NumericLanes.SignificantDigits(lane, ctx.Cap);
        if (!quotient && !binary64 && cap is null) return new(value, null, lane);
        int? digits = value.FractionDigits(out var witness);
        if (quotient && (digits is null || digits > ExactFractionDigits))
            return Built.Fail($"is an arithmetic expression whose quotient {ExprText(e)} has a value with more than the "
                + $"{ExactFractionDigits} fraction digits a nested quotient is certain to keep, or no finite decimal value"
                + (witness is { Count: > 0 } ? $" (at {ctx.PointText(witness)} it is {value.Evaluate(witness)})" : "")
                + ", so the arithmetic does not compute it exactly and the value is not shown to be an integer — " + TypeSixExpressionRule, lane);
        if (binary64)
        {
            if (digits != 0)
                return Built.Fail($"is an arithmetic expression whose intermediate {ExprText(e)} is computed in binary64 (a floating-point "
                    + "operand is beneath it) and is not always an integer, which binary64 does not hold exactly, so the value is not "
                    + "shown to be an integer — " + TypeSixExpressionRule, lane);
            return value.DigitHigh(ctx.DigitsOf) is { } high && high <= Binary64ExactDigits
                ? new(value, null, lane) : new(Polynomial.OfAtom(ctx.FreshInteger(e), ExactRational.One), null, lane);
        }
        if (cap is { } kept)
        {
            int? high = value.DigitHigh(ctx.DigitsOf);
            if (digits != 0 && (digits is null || high is null || high + digits > kept))
                return Built.Fail($"is an arithmetic expression whose intermediate {ExprText(e)} needs more than the {kept} significant "
                    + $"digits an intermediate keeps {ctx.LaneName}, so its fraction digits are not kept and the value is not shown to be "
                    + "an integer — " + TypeSixExpressionRule, lane);
            if (digits == 0 && (high is null || high > kept)) return new(Polynomial.OfAtom(ctx.FreshInteger(e), ExactRational.One), null, lane);
        }
        return new(value, null, lane);
    }

    /// <summary>The integer digits binary64 holds exactly: every integer below 10^15 is below 2^53.</summary>
    private const int Binary64ExactDigits = 15;

    /// <summary>A product or power past <see cref="Polynomial.MaxMonomials"/>: an integer atom when its structure alone shows it an
    /// integer term (<see cref="ProvablyIntegral"/>), otherwise not shown.</summary>
    private static Built Unexpanded(BoundExpr e, NumericLane lane, TypeSixContext ctx) => ProvablyIntegral(e)
        ? new(Polynomial.OfAtom(ctx.AtomOf(e, integer: true, digits: null, scale: 0), ExactRational.One), null, lane)
        : Built.Fail($"is an arithmetic expression whose product {ExprText(e)} expands past {Polynomial.MaxMonomials} terms and is "
            + "not an integer term by its structure, so its value is not shown to be an integer — " + TypeSixExpressionRule, lane);

    /// <summary>Is the polynomial in integer atoms alone and integer-valued?</summary>
    private static bool IsIntegerValued(Polynomial value, TypeSixContext ctx) =>
        value.FirstRealAtom(ctx.IsReal) is null && value.FractionDigits(out _) == 0;

    private static string NotShownWhy(BoundExpr e) =>
        $"is an arithmetic expression with {TermText(e)}, a term that is not shown to be an integer, so the expression is not shown to "
        + "always result in an integer value — " + TypeSixExpressionRule;

    private static string RealTermWhy(BoundExpr term) =>
        $"is an arithmetic expression with the term {TermText(term)}, "
        + (term is BoundIntrinsicCall ? "a numeric function" : "a floating-point numeric data item")
        + ", which does not cancel, so the expression is not shown to always result in an integer value — " + TypeSixExpressionRule;

    private static string RealIntermediateWhy(BoundExpr e, BoundExpr term) =>
        $"is an arithmetic expression whose intermediate {ExprText(e)} holds the term {TermText(term)}, "
        + (term is BoundIntrinsicCall ? "a numeric function" : "a floating-point numeric data item")
        + ", which does not cancel there, so neither the intermediate's exact value nor an integer value is shown — "
        + TypeSixExpressionRule;

    /// <summary>⛔ ARE TWO TERMS ONE PURE TERM (docs/INTRINSICS_DESIGN.md, the type-6 intake, rule 1; kb/Work PB5891)? Compared as bound
    /// trees: the same item reference (<see cref="SamePlace"/>), the same function whose value its arguments fix
    /// (<see cref="IntrinsicSig.ValueSource"/>) over the same arguments, the same operation over the same operands. A kind that carries
    /// activations, an operand already refused and any kind added later are never one term with another: two atoms where there is one
    /// only refuses more. Every concrete <see cref="BoundExpr"/> kind is named (<c>TypeSixWalkCoverageDriftTests</c>).</summary>
    internal static bool SameTerm(BoundExpr a, BoundExpr b) => (a, b) switch
    {
        (BoundNumLiteral x, BoundNumLiteral y) => x == y,
        (BoundNumRef x, BoundNumRef y) => SamePlace(x.Place, y.Place),
        (BoundBinary x, BoundBinary y) => x.Op == y.Op && SameTerm(x.Left, y.Left) && SameTerm(x.Right, y.Right),
        (BoundNegate x, BoundNegate y) => SameTerm(x.Operand, y.Operand),
        (BoundPower x, BoundPower y) => x.NegativeBaseRealRoots == y.NegativeBaseRealRoots && SameTerm(x.Base, y.Base) && SameTerm(x.Exp, y.Exp),
        (BoundIntrinsicCall x, BoundIntrinsicCall y) => IsValueFixedByArguments(x) && x with { Args = y.Args } == y
            && x.Args.Count == y.Args.Count && x.Args.Zip(y.Args).All(p => SameOperand(p.First, p.Second)),
        (BoundIndexRef x, BoundIndexRef y) => x == y,
        (BoundLinageCounterRef x, BoundLinageCounterRef y) => x == y,
        (BoundReportCounterRef x, BoundReportCounterRef y) => x == y,
        (BoundReportVaryingRef x, BoundReportVaryingRef y) => x == y,
        (BoundOdoExtent x, BoundOdoExtent y) => SamePlace(x.Depending, y.Depending) && x with { Depending = y.Depending } == y,
        (BoundUdfEvaluatedExpr or BoundExprError, _) => false,
        _ => false,
    };

    /// <summary>Two intrinsic arguments that are one value: the same item, the same literal, the same expression.</summary>
    private static bool SameOperand(BoundOperand a, BoundOperand b) => (a, b) switch
    {
        (BoundFieldOperand x, BoundFieldOperand y) => SamePlace(x.Place, y.Place) && x with { Place = y.Place } == y,
        (BoundNumericLiteral x, BoundNumericLiteral y) => x == y,
        (BoundStringLiteral x, BoundStringLiteral y) => x == y,
        (BoundComputedOperand x, BoundComputedOperand y) => SameTerm(x.Expr, y.Expr) && x with { Expr = y.Expr } == y,
        _ => false,
    };

    /// <summary>⛔ TWO PLACES THAT ARE ONE STORAGE REFERENCE (kb/Work PB5891 review R1-1): the same item reached by access paths whose
    /// segments are the same, compared STRUCTURALLY, subscripts and reference modification included (a subscript's data item is
    /// itself a place, read at the same time), so <c>T (I)</c> and <c>T (I)</c> are one place and <c>T (I)</c> and <c>T (J)</c>, or
    /// <c>T (1)</c> and <c>T (2)</c>, two. A list inside a record compares by instance, which is new at every bind, so every list is
    /// walked here; a shape this does not walk compares as a record, where a fresh list makes it unequal: two atoms where there is
    /// one only refuses more.</summary>
    private static bool SamePlace(Place a, Place b) => ReferenceEquals(a, b) || a == b || (a, b) switch
    {
        (MemberPlace x, MemberPlace y) => SamePath(x.Path, y.Path) && x with { Path = y.Path } == y,
        (DynTablePlace x, DynTablePlace y) => SamePath(x.Path, y.Path) && x with { Path = y.Path } == y,
        (RefModPlace x, RefModPlace y) => SamePlace(x.Inner, y.Inner) && SamePosition(x.Start, y.Start)
            && (x.Length is null ? y.Length is null : y.Length is not null && SamePosition(x.Length, y.Length))
            && x with { Inner = y.Inner, Start = y.Start, Length = y.Length } == y,
        _ => false,
    };

    /// <summary>Two access paths of the same segments, compared structurally (see <see cref="SamePlace"/>).</summary>
    private static bool SamePath(AccessPath a, AccessPath b) =>
        a.Segments.Count == b.Segments.Count && a.Segments.Zip(b.Segments).All(p => SameSegment(p.First, p.Second));

    private static bool SameSegment(AccessSegment a, AccessSegment b) => a == b || (a, b) switch
    {
        (FixedTableSegment x, FixedTableSegment y) => SamePosition(x.OneBasedIndex, y.OneBasedIndex)
            && (x.Odo is null ? y.Odo is null : y.Odo is not null && SamePlace(x.Odo.Depending, y.Odo.Depending)
                && x.Odo with { Depending = y.Odo.Depending } == y.Odo),
        (DynTableSegment x, DynTableSegment y) => SamePosition(x.OneBasedIndex, y.OneBasedIndex),
        (CellTableSegment x, CellTableSegment y) => SamePosition(x.Ordinal, y.Ordinal) && x with { Ordinal = y.Ordinal } == y,
        _ => false,
    };

    /// <summary>Two subscript or reference-modification positions that are one value: the same constant, the same index, the same item
    /// read (by <see cref="SamePath"/>), and the same operations over such positions.</summary>
    private static bool SamePosition(Position a, Position b) => a == b || (a, b) switch
    {
        (PositionItemRead x, PositionItemRead y) => ReferenceEquals(x.Item, y.Item) && x.Form == y.Form && SamePath(x.Path, y.Path),
        (PositionBinary x, PositionBinary y) => x.Op == y.Op && SamePosition(x.Left, y.Left) && SamePosition(x.Right, y.Right),
        (PositionNegate x, PositionNegate y) => SamePosition(x.Operand, y.Operand),
        (PositionGroup x, PositionGroup y) => SamePosition(x.Inner, y.Inner),
        (PositionOffset x, PositionOffset y) => SamePosition(x.Origin, y.Origin) && x.Terms.Count == y.Terms.Count
            && x.Terms.Zip(y.Terms).All(t => t.First.Stride == t.Second.Stride && SamePosition(t.First.Index, t.Second.Index)),
        _ => false,
    };

    /// <summary>Is the call's value fixed by its arguments (<see cref="IntrinsicSig.ValueSource"/>)? RANDOM's next value, the clock and
    /// the exception register are not; DATE-TO-YYYYMMDD, DAY-TO-YYYYDDD and YEAR-TO-YYYY are only when argument-3 is written
    /// (§2023:15.100.3 AR5: an omitted argument-3 is the current year).</summary>
    private static bool IsValueFixedByArguments(BoundIntrinsicCall call) => call.Sig.ValueSource switch
    {
        IntrinsicValueSource.Arguments => true,
        IntrinsicValueSource.ArgumentsOrCurrentYear => call.Args.Count >= 3,
        IntrinsicValueSource.RunUnitState => false,
        _ => throw new InvalidOperationException($"FUNCTION {call.Sig.Name} has the value source {call.Sig.ValueSource}, which the type 6 classifier does not know"),
    };
}
