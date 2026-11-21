// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Numerics;
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Binding.Procedure;

namespace WiseOwl.Cobol.Binding;

/// <summary>⛔ THE TYPE 6 CLASSIFIER: "is this arithmetic expression always an integer", answered PROVEN, REFUTED or UNDECIDED by a
/// polynomial normal form (kb/Work PB5886, PB5891, PB5889; docs/INTRINSICS_DESIGN.md, the type-6 intake, which states the design this
/// file builds).</summary>
internal static partial class IntrinsicArgumentRules
{
    /// <summary>⛔ THE ONE ANSWER TO "IS THIS ARITHMETIC EXPRESSION ALWAYS AN INTEGER" (kb/Work PB5886 round 2, R2-1; PB5891):
    /// <c>IntrinsicResultType.IsIntegerArgument</c> reads it for the type tables of the functions whose type depends on their arguments, so
    /// <c>FUNCTION MAX (N * (N + 1) / 2)</c> is an integer function as <c>N * (N + 1) / 2</c> is an integer argument (§2023:15.3 AR6;
    /// §1989:A.2.2 AR4). Only a PROVEN expression is one; an undecided one is a numeric argument there.</summary>
    internal static bool IsAlwaysIntegral(BoundExpr expr, ArithmeticMode arithmetic) =>
        ClassifyTypeSix(expr, arithmetic).Outcome is TypeSixOutcome.Proven;

    /// <summary>Does this argument at an integer position need the run-time check (kb/Work PB5889): an arithmetic expression the
    /// classifier neither proves an integer nor refutes? The code generator asks it of every integer position it renders
    /// (<c>IntrinsicRenderer.RawWideArg</c>), so an UNDECIDED argument is tested at its evaluation, naming the function and the
    /// argument (<c>CobolIntrinsics.CheckedIntegerArgument</c>).</summary>
    internal static bool NeedsRunTimeIntegerCheck(BoundOperand op, ArithmeticMode arithmetic) =>
        op is BoundComputedOperand { Expr: not BoundIntrinsicCall } computed
        && ClassifyTypeSix(computed.Expr, arithmetic).Outcome is TypeSixOutcome.Undecided;

    /// <summary>⛔ THE TYPE 6 SCREEN OF AN ARITHMETIC-EXPRESSION ARGUMENT HAS THREE OUTCOMES (kb/Work PB5889, the controller's ruling of
    /// 2026-11-21): PROVEN (accept), REFUTED (refuse, naming a value of the operands at which the expression is not an integer) and
    /// UNDECIDED (accept, and check the value at run time). A refusal needs a counterexample; the absence of a proof is never one.
    /// <para>§2023:15.3 AR6: "An arithmetic expression that will always result in an integer value or an integer data item shall be
    /// specified"; §1989:A.2.2 AR4: "An arithmetic expression which will always result in an integer value must be specified" (both
    /// cite.py --check OK). The requirement is on the PROGRAM and sets no method of proof, and a program that meets it must be accepted
    /// (§2023:4.2.2; §1985:I.1.5.2). Detecting a breach is the implementor's (§2023:4.2.2 "it is left to the implementor's discretion to
    /// determine what is syntactically distinguishable"; at 1985 the result is undefined, §1985:I.1.6), and at run time a value that is
    /// not an integer is an incorrect argument (§2023:15.3 AR14, EC-ARGUMENT-FUNCTION; at 1985 Amendment 2 lets the run terminate).</para>
    /// <para>PROOF (<see cref="Build"/> as the arithmetic computes it): the expression's value as a <see cref="Polynomial"/> in its atoms,
    /// every intermediate shown exact on the way (<see cref="Intermediate"/>), no real atom left and the polynomial integer-valued
    /// (<see cref="Polynomial.FractionDigits"/> is 0): <c>N * (N + 1) / 2</c> and <c>FUNCTION SQRT (N) - FUNCTION SQRT (N) + 5</c>.</para>
    /// <para>REFUTATION (<see cref="Refutation"/>): the expression's exact value at a value of its operands, every operand a data item free
    /// to hold it, is not an integer: <c>N / 2</c> at N = 1, <c>N * (N + 1) / 3</c> at N = 1, <c>N ** -1</c> at N = 2, <c>N / M</c> at N = 1,
    /// M = 2. A value of a FUNCTION or a register is no value of the operands (its range is the function's), so
    /// <c>FUNCTION ORD (A) / 2</c> and <c>S2 - FUNCTION MAX (S2)</c> are undecided, as is <c>WN / 3 * 3</c> (the proof fails on the inexact
    /// quotient, and the exact value is WN). The verdict is the same at every edition (the requirement is the same in both texts); the
    /// ARITHMETIC in force decides which intermediates are exact.</para></summary>
    private static TypeSixVerdict ClassifyTypeSix(BoundExpr expr, ArithmeticMode arithmetic)
    {
        // ARITHMETIC IS STANDARD-BINARY is declined at bind (COBOL0806, ArithmeticModes): no expression is evaluated under it, so nothing
        // more is said of an argument; asking its intermediate precision crashed the compiler (kb/Work PB5891 review R4-2).
        if (arithmetic is ArithmeticMode.StandardBinary) return new(TypeSixOutcome.Proven, null);
        var proof = new TypeSixContext(arithmetic);
        if (Build(expr, proof) is { } proved && proved.FirstRealAtom(proof.IsReal) is null && proved.FractionDigits(out _) == 0)
            return new(TypeSixOutcome.Proven, null);
        return Refutation(expr, arithmetic) is { } why ? new(TypeSixOutcome.Refuted, why) : new(TypeSixOutcome.Undecided, null);
    }

    /// <summary>⛔ THE COUNTEREXAMPLE OF THE REFUTED OUTCOME (kb/Work PB5889): a value of the operands at which the expression's EXACT value
    /// is not an integer, as the diagnostic states it, or null when none is found. The OPERANDS are the expression's data items
    /// (<see cref="CollectOperands"/>). Its other leaves may be INTEGER terms (an integer function, a counter register, an index-name, a
    /// report VARYING counter, an OCCURS DEPENDING extent), whose range the screen does not know (<c>FUNCTION FACTORIAL (N + 2) / 2</c>
    /// is never odd): at a value of the operands the expression is a polynomial in them (<see cref="ValueAt"/>), and it is refuted there
    /// only when that polynomial is a non-integer constant plus a part that is an integer for EVERY integer value of those terms
    /// (<c>I2 + 0.5 + FUNCTION INTEGER (S2)</c>, <c>LINAGE-COUNTER + 0.5</c>), so no value they could take makes it an integer. A leaf
    /// that is a numeric function leaves no counterexample (<c>S2 - FUNCTION MAX (S2)</c>); a floating-point item is an operand whose value
    /// steps by 0.5 (<c>F + 1</c> at F = 0.5).
    /// Two or more operands must hold storage of their own, and two references to one item must be distinct constant elements of it
    /// (<see cref="AreIndependent"/>): <c>T (1) - T (2) + 1</c> is refuted, <c>T (I) - T (J) + 5</c> is not. The points tried are every
    /// combination of k + 1 consecutive values of each operand's least unit (0, 1, ... unsigned; 0, 1, -1, ... signed; k fits the
    /// description, and at most <see cref="MaxRefutationPoints"/> points), in increasing order, so the first found is the simplest: for
    /// a polynomial of degree at most k in each operand that grid holds a point where it is not an integer whenever one exists (the
    /// type-6 intake's rule 3). The value is the algebraic one: an expression whose exact value is not an integer does not "always
    /// result in an integer value", whatever digits the arithmetic keeps.</summary>
    private static string? Refutation(BoundExpr expr, ArithmeticMode arithmetic)
    {
        var operands = new List<BoundNumRef>();
        if (!CollectOperands(expr, operands) || !AreIndependent(operands)) return null;
        int top = 0;
        while (top < MaxRefutationValue && BigInteger.Pow(top + 2, operands.Count) <= MaxRefutationPoints) top++;
        // The k-th value of an operand's least unit: 0, 1, 2, ... for an unsigned item, 0, 1, -1, 2, -2, ... for a signed one (k + 1
        // consecutive integers either way, which is what the grid argument needs), within its description's digits. A floating-point
        // item's unit is 0.5, which binary64 holds exactly (§2023:14.6.8.3: its value set holds non-integers).
        var pics = operands.Select(o => o.Place.DescribedItem!.Pic!).ToList();
        int[] limit = [.. pics.Select(p => p.IsFloat ? top : (int)BigInteger.Min(top, (BigInteger.Pow(10, p.Digits) - 1) * (p.Signed ? 2 : 1)))];
        var values = new ExactRational[operands.Count];
        int[] x = new int[operands.Count];
        var terms = new TypeSixContext(arithmetic);
        while (true)
        {
            for (int i = 0; i < x.Length; i++)
            {
                bool signed = pics[i].Signed || pics[i].IsFloat;
                int unit = signed && x[i] % 2 == 0 ? -x[i] / 2 : signed ? (x[i] + 1) / 2 : x[i];
                values[i] = pics[i].IsFloat ? ExactRational.Of(unit) / ExactRational.Of(2) : ExactRational.FromScaled(unit, -pics[i].Scale);
            }
            if (ValueAt(expr, operands, values, terms) is { } value && value.ConstantTerm is { IsInteger: false } c
                && value.Add(Polynomial.Constant(c), -ExactRational.One) is var rest && (rest.IsZero || rest.FractionDigits(out _) == 0))
            {
                string plus = rest.IsZero ? "" : " plus an integer";
                return operands.Count == 0
                    ? $"is an arithmetic expression whose literal terms sum to {c} and whose every other term cancels or is an integer, so "
                        + "its value is never an integer"
                    : "is an arithmetic expression whose value is not an integer for every value of its terms: at "
                        + string.Join(", ", operands.Select((o, i) => $"{ExprText(o)} = {values[i]}")) + $" it is {c}{plus}";
            }
            int j = 0;
            while (j < x.Length && x[j] == limit[j]) x[j++] = 0;
            if (j == x.Length) return null;
            x[j]++;
        }
    }

    /// <summary>The most points <see cref="Refutation"/> evaluates, and the largest value of an operand's least unit it tries: 0..12 for
    /// up to three operands, as the proof's grid test (<see cref="Polynomial.MaxGridDegree"/>, <see cref="Polynomial.MaxGridPoints"/>).</summary>
    private const int MaxRefutationPoints = Polynomial.MaxGridPoints, MaxRefutationValue = Polynomial.MaxGridDegree;

    /// <summary>Can the operands hold any values at once? One always can. Two or more each hold storage of their own
    /// (<see cref="HoldsIndependentStorage"/>), and two references to one item are distinct elements of it at CONSTANT subscripts
    /// (<c>T (1)</c>, <c>T (2)</c>); <c>T (I)</c> and <c>T (J)</c> are one element when I = J.</summary>
    private static bool AreIndependent(List<BoundNumRef> operands) =>
        operands.Count <= 1
        || (operands.All(o => HoldsIndependentStorage(o.Place.DescribedItem!))
            && operands.GroupBy(o => o.Place.DescribedItem).All(g => g.Count() == 1 || g.All(o => IsConstantElement(o.Place))));

    /// <summary>Is the place an element of a table at constant subscripts only (every segment a root, a member, or a fixed table
    /// segment whose index is a constant)?</summary>
    private static bool IsConstantElement(Place place) =>
        place is MemberPlace { Path.Segments: var segments }
        && segments.Any(s => s is FixedTableSegment)
        && segments.All(s => s is RootSegment or MemberSegment or FixedTableSegment { OneBasedIndex: PositionConstant, Odo: null });

    /// <summary>The data items of the expression, each place once (<see cref="SamePlace"/>), fixed-point or floating-point, or false when a
    /// leaf may hold a value that is not an integer and is no data item (a numeric function) or is none the walk reads: every concrete
    /// <see cref="BoundExpr"/> kind is named (<c>TypeSixWalkCoverageDriftTests</c>).</summary>
    private static bool CollectOperands(BoundExpr e, List<BoundNumRef> operands)
    {
        if (ExactValue(e) is not null) return true;
        switch (e)
        {
            case BoundNumRef { Place.DescribedItem: { IsGroup: false, Pic: { Category: PicCategory.Numeric } p } } item
                when p.IsFloat || (p.Usage is not Usage.Index && p.Digits > 0):
                if (!operands.Any(o => SamePlace(o.Place, item.Place))) operands.Add(item);
                return true;
            case BoundBinary b:
                return CollectOperands(b.Left, operands) && CollectOperands(b.Right, operands);
            case BoundNegate n:
                return CollectOperands(n.Operand, operands);
            case BoundPower pw:
                return CollectOperands(pw.Base, operands) && CollectOperands(pw.Exp, operands);
            // An integer term: no operand, a term of the value's polynomial (ValueAt).
            case BoundIntrinsicCall { ResultType: IntrinsicType.Integer }:
            case BoundIndexRef or BoundReportVaryingRef or BoundOdoExtent:
                return true;
            case BoundLinageCounterRef or BoundReportCounterRef:
                return AlgebraicRanges.IsCounterRegister(e);
            case BoundUdfEvaluatedExpr:
            case BoundNumRef or BoundNumLiteral or BoundIntrinsicCall or BoundExprError:
            default:
                return false;
        }
    }

    /// <summary>The EXACT value of the expression when its operands (<see cref="CollectOperands"/>) hold <paramref name="values"/>, as a
    /// polynomial in its integer terms (<paramref name="terms"/>' atoms), or null where it has none the walk can state (a division by
    /// zero or by a term, a power whose exponent is no integer constant or is past <see cref="MaxFoldedExponent"/>): every concrete
    /// <see cref="BoundExpr"/> kind is named (<c>TypeSixWalkCoverageDriftTests</c>).</summary>
    private static Polynomial? ValueAt(BoundExpr e, List<BoundNumRef> operands, ExactRational[] values, TypeSixContext terms)
    {
        if (ExactValue(e) is { } constant) return Polynomial.Constant(constant);
        switch (e)
        {
            case BoundNumRef item:
                return operands.FindIndex(o => SamePlace(o.Place, item.Place)) is int at and >= 0 ? Polynomial.Constant(values[at]) : null;
            case BoundBinary { Op: '+' or '-' } b
                when ValueAt(b.Left, operands, values, terms) is { } l && ValueAt(b.Right, operands, values, terms) is { } r:
                return l.Add(r, b.Op == '-' ? -ExactRational.One : ExactRational.One);
            case BoundBinary { Op: '*' } b
                when ValueAt(b.Left, operands, values, terms) is { } l && ValueAt(b.Right, operands, values, terms) is { } r:
                return l.Multiply(r);
            case BoundBinary { Op: '/' } b when ValueAt(b.Right, operands, values, terms) is { } r && r.Terms.All(t => t.Factors.Length == 0)
                && r.ConstantTerm.Sign != 0:
                return ValueAt(b.Left, operands, values, terms)?.Scale(ExactRational.One / r.ConstantTerm);
            case BoundNegate n:
                return ValueAt(n.Operand, operands, values, terms)?.Scale(-ExactRational.One);
            case BoundPower pw when ValueAt(pw.Exp, operands, values, terms) is { } exp && exp.Terms.All(t => t.Factors.Length == 0)
                && exp.ConstantTerm is { IsInteger: true } ev && BigInteger.Abs(ev.Numerator) <= MaxFoldedExponent
                && ValueAt(pw.Base, operands, values, terms) is { } bv:
                if (bv.Terms.All(t => t.Factors.Length == 0))
                    return bv.ConstantTerm.Sign != 0 || ev.Sign > 0 ? Polynomial.Constant(bv.ConstantTerm.Pow((int)ev.Numerator)) : null;
                return ev.Sign >= 0 ? bv.Power((int)ev.Numerator) : null;
            case BoundIntrinsicCall { ResultType: IntrinsicType.Integer }:
            case BoundIndexRef or BoundReportVaryingRef or BoundOdoExtent:
            case BoundLinageCounterRef or BoundReportCounterRef when AlgebraicRanges.IsCounterRegister(e):
                return Polynomial.OfAtom(terms.AtomOf(e, integer: true, digits: null, scale: 0), ExactRational.One);
            case BoundBinary or BoundPower:
            case BoundNumLiteral or BoundIntrinsicCall or BoundLinageCounterRef or BoundReportCounterRef or BoundExprError
                or BoundUdfEvaluatedExpr:
            default:
                return null;
        }
    }

    /// <summary>Is the item's storage its own: in WORKING-STORAGE or LOCAL-STORAGE, and nothing on its path to its record redefines,
    /// renames or bases it (a LINKAGE item may be passed another's storage, and a FILE SECTION record shares its file's area)?</summary>
    private static bool HoldsIndependentStorage(DataItem item)
    {
        if (item.Section is not (EntrySection.WorkingStorage or EntrySection.LocalStorage)) return false;
        for (DataItem? i = item; i is not null; i = i.Parent)
            if (i.RedefinesTarget is not null || i.Renames is not null || i.IsBased) return false;
        return true;
    }

    /// <summary>The three outcomes of the type 6 screen of an arithmetic expression (<see cref="ClassifyTypeSix"/>).</summary>
    private enum TypeSixOutcome { Proven, Refuted, Undecided }

    /// <summary>An outcome, and for REFUTED the counterexample the diagnostic names.</summary>
    private readonly record struct TypeSixVerdict(TypeSixOutcome Outcome, string? Refutation);

    /// <summary>The atoms of one classification and the arithmetic in force: whether it is a decimal engine (every operation on the
    /// SDIDI) and its significant-digit cap (null for native arithmetic, whose lanes say their own, <see cref="NumericLanes"/>).</summary>
    private sealed class TypeSixContext(ArithmeticMode arithmetic)
    {
        public readonly List<PolynomialAtom> Atoms = [];
        public readonly int? Cap = ArithmeticModes.IntermediateSignificantDigits(arithmetic);
        public readonly bool DecimalEngine = ArithmeticModes.IsDecimalEngine(arithmetic);

        /// <summary>The lanes the code generator may evaluate <paramref name="e"/> on: <see cref="NumericLanes.LaneOf"/>, the one function
        /// of the bound node the renderer asserts its carrier against (kb/Work PB5891 review R3-1), for an argument (no receiver).</summary>
        public IReadOnlySet<NumericLane> LanesOf(BoundExpr e) => NumericLanes.LaneOf(e, LaneContext.Argument(arithmetic)).Lanes;

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
    }

    /// <summary>⛔ THE POLYNOMIAL OF A NODE (docs/INTRINSICS_DESIGN.md, the type-6 intake, rules 1 and 2), or null when it is not shown:
    /// every concrete <see cref="BoundExpr"/> kind is named, and the default is null (<c>TypeSixWalkCoverageDriftTests</c>).</summary>
    private static Polynomial? Build(BoundExpr e, TypeSixContext ctx)
    {
        // A LITERAL-ONLY SUBTREE is a constant at its exact fold, trusted as the computed value only when the arithmetic stores every
        // quotient in it exactly (kb/Work PB4758: `1 / 3 * 3` folds to 1 and the arithmetic stores 0.9999).
        if (ExactValue(e) is { } constant)
            return QuotientsAreExact(e) ? Polynomial.Constant(constant) : null;
        switch (e)
        {
            case BoundBinary { Op: '+' or '-' } b:
            {
                if (Build(b.Left, ctx) is not { } l || Build(b.Right, ctx) is not { } r) return null;
                var k = b.Op == '-' ? -ExactRational.One : ExactRational.One;
                return Intermediate(e, l.Add(r, k), quotient: false, ctx);
            }
            case BoundBinary { Op: '*' } b:
            {
                if (Build(b.Left, ctx) is not { } l || Build(b.Right, ctx) is not { } r) return null;
                return l.Multiply(r) is { } product ? Intermediate(e, product, quotient: false, ctx) : Unexpanded(e, ctx);
            }
            // ⛔ A QUOTIENT BY A NONZERO LITERAL c IS THE PRODUCT BY 1/c, exact when its own value keeps within the guard digits a nested
            // quotient keeps (Intermediate): `WN / 2 * 2` and `N * (N + 1) / 2` are proven, `WN / 3 * 3` is not.
            case BoundBinary { Op: '/', Left: var dividend, Right: var divisor } when ExactValue(divisor) is { Sign: not 0 } c
                && QuotientsAreExact(divisor):
                return Build(dividend, ctx) is { } d ? Intermediate(e, d.Scale(ExactRational.One / c), quotient: true, ctx) : null;
            // ⛔ ANY OTHER QUOTIENT IS NO POLYNOMIAL (kb/Work PB5886): a divisor that is an item, a function or an expression moves with
            // the valuation (A / A is a size error at A = 0).
            case BoundBinary:
                return null;
            // A negation is exact whatever it negates.
            case BoundNegate n:
                return Build(n.Operand, ctx)?.Scale(-ExactRational.One);
            // A power to a non-negative integer literal is the product of that many bases.
            case BoundPower pw when ExactValue(pw.Exp) is { IsInteger: true, Sign: >= 0 } n && QuotientsAreExact(pw.Exp)
                && n.Numerator <= MaxFoldedExponent:
            {
                if (Build(pw.Base, ctx) is not { } b) return null;
                return b.Power((int)n.Numerator) is { } power ? Intermediate(e, power, quotient: false, ctx) : Unexpanded(e, ctx);
            }
            // ⛔ THE ONE RULE OF ANY OTHER INTEGER POWER (kb/Work PB5886 round 2): an integer-valued base to an exponent that cannot be
            // negative, or the literal 1 to an integer exponent, is an integer atom (`N ** U`, `1 ** M`); any other power is not shown.
            case BoundPower pw:
            {
                if (Build(pw.Base, ctx) is not { } b || !IsIntegerValued(b, ctx)) return null;
                bool exponentIntegral = Build(pw.Exp, ctx) is { } xv && IsIntegerValued(xv, ctx);
                // ⛔ THE POWER'S LANE IS THE NODE'S (kb/Work PB5891 review R3-1): NumericLanes.LaneOf of the power node, the function the
                // renderer asserts its carrier against. A power the renderer may evaluate as a binary64 approximation (an exponent integer
                // by its polynomial but not by its carrier, `N ** (U / 2 * 2)`, or a wide one, `1 ** (X * X)`) is no integer atom.
                bool approximate = ctx.LanesOf(e).Any(NumericLanes.IsBinary64);
                return IsIntegerPower(pw.Base, pw.Exp, exponentIntegral) && !approximate
                    ? Polynomial.OfAtom(ctx.AtomOf(e, integer: true, digits: null, scale: 0), ExactRational.One)
                    : null;
            }
            // A FIXED-POINT ITEM of scale s is 10^(−s) × x, x an integer atom below 10^digits; a FLOATING-POINT item is a real atom (its
            // declared value set holds non-integers, §2023:14.6.8.3, kb/Work PB248) and puts its operation on the binary64 lane.
            case BoundNumRef { Place.DescribedItem: { IsGroup: false, Pic: { Category: PicCategory.Numeric } p } } item:
                return p.IsFloat
                    ? Polynomial.OfAtom(ctx.AtomOf(item, integer: false, digits: null, scale: 0), ExactRational.One)
                    : Polynomial.OfAtom(ctx.AtomOf(item, integer: true, p.Usage is Usage.Index ? null : p.Digits, p.Scale),
                        ExactRational.FromScaled(1, -p.Scale));
            // ⛔ A FUNCTION IS A TERM, judged by its type (kb/Work PB5886): an integer function is an integer atom, any other a real atom
            // that must cancel (§2023:8.4.3.2.3 SR11 reads a numeric function's value as no integer, whatever this reference yields).
            case BoundIntrinsicCall { ResultType: IntrinsicType.Integer } call:
                return Polynomial.OfAtom(ctx.AtomOf(call, integer: true, digits: null, scale: 0), ExactRational.One);
            case BoundIntrinsicCall call:
                return Polynomial.OfAtom(ctx.AtomOf(call, integer: false, digits: null, scale: 0), ExactRational.One);
            // An index-name's value is an occurrence number (§2023:13.18.38); a report VARYING counter and an OCCURS DEPENDING extent are
            // scale-0 integers by construction; a counter register is an unsigned integer data item (§2023:8.4.3.14.4 GR1).
            case BoundIndexRef or BoundReportVaryingRef or BoundOdoExtent:
            case BoundLinageCounterRef or BoundReportCounterRef when AlgebraicRanges.IsCounterRegister(e):
                return Polynomial.OfAtom(ctx.AtomOf(e, integer: true, digits: null, scale: 0), ExactRational.One);
            // The windows that carry a user function's activations hold the expression they evaluate.
            case BoundUdfEvaluatedExpr udf:
                return Build(udf.Inner, ctx);
            // An operand already refused where it was bound: its site reported the rule it breaks, and nothing more is said of it.
            case BoundExprError:
                return Polynomial.Zero;
            // ⛔ THE DEFAULT IS NOT SHOWN (review R4-1): a BoundNumRef that is no numeric item, a BoundNumLiteral the fold cannot read, a
            // register that is no counter register, and any node kind added later.
            case BoundNumRef or BoundNumLiteral or BoundLinageCounterRef or BoundReportCounterRef:
            default:
                return null;
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
    private static Polynomial? Intermediate(BoundExpr e, Polynomial value, bool quotient, TypeSixContext ctx)
    {
        if (value.FirstRealAtom(ctx.IsReal) is not null) return null;
        var lanes = ctx.LanesOf(e);
        bool binary64 = lanes.Any(NumericLanes.IsBinary64);
        var caps = lanes.Select(lane => NumericLanes.SignificantDigits(lane, ctx.Cap)).OfType<int>().Distinct().Order().ToList();
        if (!quotient && !binary64 && caps.Count == 0) return value;
        int? digits = value.FractionDigits(out _);
        if (quotient && (digits is null || digits > ExactFractionDigits)) return null;
        bool rounded = false;
        int? high = value.DigitHigh(ctx.DigitsOf);
        if (binary64)
        {
            if (digits != 0) return null;
            rounded |= !(high is { } h && h <= Binary64ExactDigits);
        }
        foreach (int kept in caps)
        {
            if (digits != 0 && (digits is null || high is null || high + digits > kept)) return null;
            rounded |= digits == 0 && (high is null || high > kept);
        }
        return rounded ? Polynomial.OfAtom(ctx.FreshInteger(e), ExactRational.One) : value;
    }

    /// <summary>The integer digits binary64 holds exactly: every integer below 10^15 is below 2^53.</summary>
    private const int Binary64ExactDigits = 15;

    /// <summary>A product or power past <see cref="Polynomial.MaxMonomials"/>: an integer atom when its structure alone shows it an
    /// integer term (<see cref="ProvablyIntegral"/>), otherwise not shown.</summary>
    private static Polynomial? Unexpanded(BoundExpr e, TypeSixContext ctx) =>
        ProvablyIntegral(e) && !ctx.LanesOf(e).Any(NumericLanes.IsBinary64)
            ? Polynomial.OfAtom(ctx.AtomOf(e, integer: true, digits: null, scale: 0), ExactRational.One)
            : null;

    /// <summary>Is the polynomial in integer atoms alone and integer-valued?</summary>
    private static bool IsIntegerValued(Polynomial value, TypeSixContext ctx) =>
        value.FirstRealAtom(ctx.IsReal) is null && value.FractionDigits(out _) == 0;

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
