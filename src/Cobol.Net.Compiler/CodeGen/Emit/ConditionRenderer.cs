// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Bound;

namespace CobolNet.CodeGen.Emit;

/// <summary>
/// Renders a bound condition to a side-effect-free C# boolean expression (COBOLNET_DESIGN §11): relational
/// comparisons (numeric scale-aligned, or alphanumeric via <c>CobolString.Compare</c>), logical AND/OR/XOR/NOT,
/// level-88 membership over the conditional variable, and sign conditions. An unbound condition fails loud (§1.4).
/// </summary>
internal sealed class ConditionRenderer(NumericRenderer num)
{
    /// <summary>Render a bound condition as a C# boolean expression.</summary>
    public string Render(BoundCondition c) => c switch
    {
        BoundRelational r => RenderRelational(r),
        BoundLogical l => "(" + string.Join($" {l.Op} ", l.Operands.Select(Render)) + ")",
        BoundNot n => $"!({Render(n.Operand)})",
        BoundCondition88 c88 => RenderCondition88(c88),
        BoundSignCondition s => RenderSign(s),
        BoundConditionError e => EmitText.LoudValue("bool", e.Feature),
        _ => EmitText.LoudValue("bool", $"bound condition '{c.GetType().Name}'"),
    };

    private string RenderRelational(BoundRelational r)
    {
        if (OperandText.IsString(r.Left) || OperandText.IsString(r.Right))
            return $"CobolString.Compare({OperandText.AsString(r.Left)}, {OperandText.AsString(r.Right)}) {r.Op} 0";
        NumX l = num.AsNum(r.Left), rr = num.AsNum(r.Right);
        int s = Math.Max(l.Scale, rr.Scale);
        return $"{NumericRenderer.Align(l, s)} {r.Op} {NumericRenderer.Align(rr, s)}";
    }

    private string RenderSign(BoundSignCondition s)
    {
        NumX v = num.Render(s.Expr);
        string test = s.Kind switch { 'P' => $"{v.Expr} > 0", 'N' => $"{v.Expr} < 0", _ => $"{v.Expr} == 0" };
        return s.Negated ? $"!({test})" : $"({test})";
    }

    private static string RenderCondition88(BoundCondition88 c)
    {
        string read = c.Parent.Read();
        bool isString = c.Parent.Item.IsGroup || c.Parent.Item.Pic?.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited;
        var tests = c.Condition.Values.Select(v => RenderMembershipTest(read, c.Parent.Item, isString, v.Low, v.High));
        return "(" + string.Join(" || ", tests) + ")";
    }

    /// <summary>One VALUE-set membership test: equality for a singleton, an inclusive bound test for a THRU range.</summary>
    private static string RenderMembershipTest(string read, DataItem parent, bool isString, string low, string? high)
    {
        if (isString)
        {
            string lo = EmitText.CsLiteral(EmitText.DecodeCobolString(low));
            if (high is null) return $"CobolString.Compare({read}, {lo}) == 0";
            return $"(CobolString.Compare({read}, {lo}) >= 0 && CobolString.Compare({read}, {EmitText.CsLiteral(EmitText.DecodeCobolString(high))}) <= 0)";
        }
        int scale = parent.Pic?.Scale ?? 0;
        string loN = EmitText.UnscaledAtScale(low, scale);
        if (high is null) return $"{read} == {loN}";
        return $"({read} >= {loN} && {read} <= {EmitText.UnscaledAtScale(high, scale)})";
    }
}
