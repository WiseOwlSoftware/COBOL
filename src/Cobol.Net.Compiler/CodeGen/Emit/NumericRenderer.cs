// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Bound;

namespace CobolNet.CodeGen.Emit;

/// <summary>
/// Renders a bound numeric expression / operand to a scale-tracked native-integer C# expression (<see cref="NumX"/>).
/// COBOL fixed-point arithmetic operates on the algebraic value regardless of representation (ISO §8.8.1): operands
/// are unscaled longs carrying their scale, the renderer aligns scales for ±, adds them for ×, and computes a
/// quotient at the working scale (<see cref="EmissionContext.TargetScale"/>) for ÷. The receiver's store
/// (truncation / capacity) is applied later by <c>CobolNum.Store</c>.
/// </summary>
internal sealed class NumericRenderer(EmissionContext ctx)
{
    /// <summary>Render a bound numeric expression as a scaled long.</summary>
    public NumX Render(BoundExpr e) => e switch
    {
        BoundNumLiteral l => EmitText.UnscaledLit(l.Text),
        BoundNumRef r => FieldNum(r.Place),
        BoundBinary b => Combine(Render(b.Left), b.Op.ToString(), Render(b.Right)),
        BoundNegate n => Negate(Render(n.Operand)),
        BoundPower p => new NumX($"(long)System.Math.Pow((double)({Real(Render(p.Base))}), (double)({Real(Render(p.Exp))}))", 0),
        BoundExprError err => new NumX(EmitText.LoudValue("long", err.Feature), 0),
        _ => new NumX(EmitText.LoudValue("long", $"bound expression '{e.GetType().Name}'"), 0),
    };

    /// <summary>Render a bound operand as a scaled native-integer value.</summary>
    public NumX AsNum(BoundOperand op) => op switch
    {
        BoundNumericLiteral n => EmitText.UnscaledLit(n.Text),
        BoundFieldOperand f => FieldNum(f.Place),
        BoundComputedOperand c => Render(c.Expr),
        BoundFigurative { Kind: 'Z' } => EmitText.UnscaledLit("0"),   // ZERO in a numeric context
        BoundFigurative f => new NumX(EmitText.LoudValue("long", $"figurative '{f.Kind}' in a numeric context"), 0),
        BoundStringLiteral => new NumX(EmitText.LoudValue("long", "alphanumeric literal in a numeric context"), 0),
        BoundOperandError e => new NumX(EmitText.LoudValue("long", e.Feature), 0),
        _ => new NumX(EmitText.LoudValue("long", $"bound operand '{op.GetType().Name}'"), 0),
    };

    /// <summary>The scaled value of a data item place (its unscaled <c>long</c> value + its scale). A float item is
    /// truncated to <c>long</c> for now (mixed float/fixed arithmetic is a later slice). A non-numeric place (a group
    /// or an alphanumeric item used in a numeric context) fails loud rather than crashing the compiler (§1.4).</summary>
    public static NumX FieldNum(Place p) => p.Item.Pic switch
    {
        null => new NumX(EmitText.LoudValue("long", $"numeric use of group item '{p.Item.CobolName ?? p.Read()}'"), 0),
        { IsFloat: true } => new NumX($"(long){p.Read()}", 0),
        { } pic => new NumX(p.Read(), pic.Scale),
    };

    /// <summary>Left-fold a list of bound expressions with <c>+</c> (the addends of an ADD / minuends of a SUBTRACT).</summary>
    public NumX Fold(IReadOnlyList<BoundExpr> xs)
    {
        if (xs.Count == 0) return new NumX("0L", 0);
        NumX acc = Render(xs[0]);
        for (int i = 1; i < xs.Count; i++) acc = Combine(acc, "+", Render(xs[i]));
        return acc;
    }

    /// <summary>Combine two scaled values with a COBOL operator, tracking the result scale (ISO §8.8.1).</summary>
    public NumX Combine(NumX a, string op, NumX b) => op switch
    {
        "+" or "-" => CombineAdditive(a, op, b),
        "*" => new NumX($"({a.Expr} * {b.Expr})", a.Scale + b.Scale),     // multiplication: scales add (exact)
        "/" => new NumX(                                                  // division: quotient at the working scale
            $"CobolNum.Divide({a.Expr}, {a.Scale}, {b.Expr}, {b.Scale}, {DivScale(a, b)}, CobolRounding.Truncation)",
            DivScale(a, b)),
        _ => a,
    };

    private static NumX CombineAdditive(NumX a, string op, NumX b)
    {
        int s = Math.Max(a.Scale, b.Scale);
        return new NumX($"({Align(a, s)} {op} {Align(b, s)})", s);
    }

    private int DivScale(NumX a, NumX b) => Math.Max(ctx.TargetScale, Math.Max(a.Scale, b.Scale));

    /// <summary>Rescale a value's unscaled long up to <paramref name="toScale"/> (widening only here → exact).</summary>
    public static string Align(NumX x, int toScale) =>
        toScale == x.Scale ? x.Expr : $"CobolNum.Rescale({x.Expr}, {x.Scale}, {toScale}, CobolRounding.Truncation)";

    private static NumX Negate(NumX x) => new($"(-{x.Expr})", x.Scale);

    private static string Real(NumX x) => x.Scale == 0 ? $"(double){x.Expr}" : $"({x.Expr} / {Pow10D(x.Scale)})";

    private static string Pow10D(int n) { double r = 1; for (int i = 0; i < n; i++) r *= 10; return $"{r}d"; }
}
