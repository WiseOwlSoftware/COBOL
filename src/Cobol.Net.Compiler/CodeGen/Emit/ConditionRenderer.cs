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
        // An EMPTY logical is the tautology (EVALUATE's ANY object composes as an AND over zero terms).
        BoundLogical { Operands.Count: 0 } => "true",
        BoundLogical l => "(" + string.Join($" {l.Op} ", l.Operands.Select(Render)) + ")",
        BoundNot n => $"!({Render(n.Operand)})",
        BoundCondition88 c88 => RenderCondition88(c88),
        BoundSignCondition s => RenderSign(s),
        BoundClassCondition cc => RenderClass(cc),
        BoundConditionError e => EmitText.LoudValue("bool", e.Feature),
        _ => EmitText.LoudValue("bool", $"bound condition '{c.GetType().Name}'"),
    };

    private string RenderRelational(BoundRelational r)
    {
        // A figurative operand (a single-character constant OR an ALL "literal") is materialized against the OTHER
        // operand's width (ISO §8.3.3.6.4 GR2), so it routes through the width-aware figurative path.
        if (r.Left is BoundFigurative or BoundAllLiteral || r.Right is BoundFigurative or BoundAllLiteral)
            return RenderFigurativeRelational(r);
        if (OperandText.IsString(r.Left) || OperandText.IsString(r.Right))
            // A signed numeric compared against an alphanumeric operand drops its sign (ISO §8.8.4.2.5 → §14.9.25.4 GR6a).
            return $"CobolString.Compare({OperandText.AsString(r.Left, deSign: true)}, {OperandText.AsString(r.Right, deSign: true)}) {r.Op} 0";
        NumX l = num.AsNum(r.Left), rr = num.AsNum(r.Right);
        // A STANDARD-DECIMAL intermediate compares algebraically in SDIDI form (§8.8.1.5).
        if (l.Dec || rr.Dec)
            return $"CobolDec.Compare({num.DecOperand(l)}, {num.DecOperand(rr)}) {r.Op} 0";
        int s = Math.Max(l.Scale, rr.Scale);
        return $"{NumericRenderer.Align(l, s)} {r.Op} {NumericRenderer.Align(rr, s)}";
    }

    /// <summary>A relational comparison where one side is a figurative constant — it materializes to the other
    /// operand's category and width (ISO §8.8.4.1.1): a numeric anchor → ZERO is 0; an alphanumeric/group anchor →
    /// the figurative is a string of the anchor's width.</summary>
    private string RenderFigurativeRelational(BoundRelational r)
    {
        static bool IsFig(BoundOperand o) => o is BoundFigurative or BoundAllLiteral;
        // A NON-NUMERIC figurative (SPACE/QUOTE/HIGH/LOW-VALUE — anything but ZERO) or an ALL "literal" makes the
        // comparison ALPHANUMERIC even against a numeric item (ISO §8.8.4.2.1 — the figurative is alphanumeric
        // class, so the numeric operand participates via its character image, at its own width).
        static bool NonNumericFig(BoundOperand o) => o is BoundFigurative { Kind: not 'Z' } or BoundAllLiteral;
        BoundOperand anchor = IsFig(r.Left) ? r.Right : r.Left;
        if (IsFig(anchor) || OperandText.IsString(anchor) || NonNumericFig(r.Left) || NonNumericFig(r.Right))
        {
            int width = AnchorWidth(anchor);
            return $"CobolString.Compare({FigOrString(r.Left, width)}, {FigOrString(r.Right, width)}) {r.Op} 0";
        }
        NumX l = FigOrNum(r.Left), rr = FigOrNum(r.Right);
        int s = Math.Max(l.Scale, rr.Scale);
        return $"{NumericRenderer.Align(l, s)} {r.Op} {NumericRenderer.Align(rr, s)}";
    }

    private static int AnchorWidth(BoundOperand op) => op switch
    {
        BoundFieldOperand f => f.Place.Item.Pic?.Length ?? f.Place.Item.ImageWidth,
        BoundStringLiteral s => Math.Max(s.Value.Length, 1),
        BoundAllLiteral a => Math.Max(a.Literal.Length, 1),
        _ => 1,
    };

    private static string FigOrString(BoundOperand op, int width) => op switch
    {
        BoundFigurative f => $"new string({EmitText.FigurativeFill(f.Kind)}, {width})",
        BoundAllLiteral a => EmitText.CsLiteral(EmitText.RepeatToWidth(a.Literal, width)),   // ALL "literal" → repeated to width (GR2)
        _ => OperandText.AsString(op),
    };

    private NumX FigOrNum(BoundOperand op) => op switch
    {
        BoundFigurative { Kind: 'Z' } => EmitText.UnscaledLit("0"),
        BoundFigurative f => new NumX(EmitText.LoudValue("long", $"figurative '{f.Kind}' in a numeric comparison"), 0),
        _ => num.AsNum(op),
    };

    private string RenderSign(BoundSignCondition s)
    {
        NumX v = num.Render(s.Expr);
        string test = s.Kind switch { 'P' => $"{v.Expr} > 0", 'N' => $"{v.Expr} < 0", _ => $"{v.Expr} == 0" };
        return s.Negated ? $"!({test})" : $"({test})";
    }

    /// <summary>A class condition (ISO §8.8.4.1.4). A typed-numeric operand IS NUMERIC folds to <c>true</c> (it can
    /// only hold digits — COBOLNET_DESIGN §6.6); every other case checks the operand's character image at run time.</summary>
    private string RenderClass(BoundClassCondition c)
    {
        bool numericField = c.Operand is BoundFieldOperand f && f.Place.Item.Pic?.Category is PicCategory.Numeric;
        string arg = OperandText.AsString(c.Operand);
        string test = c.ClassKind switch
        {
            'N' => numericField ? "true" : $"CobolClass.IsNumeric({arg})",
            'A' => $"CobolClass.IsAlphabetic({arg})",
            'U' => $"CobolClass.IsAlphabeticUpper({arg})",
            'L' => $"CobolClass.IsAlphabeticLower({arg})",
            _ => EmitText.LoudValue("bool", "class condition"),
        };
        return c.Negated ? $"!({test})" : $"({test})";
    }

    private static string RenderCondition88(BoundCondition88 c)
    {
        bool isString = c.Parent.Item.IsGroup || c.Parent.Item.Pic?.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited;
        // ISO §8.8.4.5 GR2: a condition-name test compares the conditional variable by the RELATION-CONDITION rules, so
        // the variable is rendered as a comparison operand exactly as a relation condition renders it — an alphanumeric
        // GROUP is treated as an elementary alphanumeric data item (§8.8.4.1), i.e. its character IMAGE, not the raw
        // struct. (The numeric branch reads the scaled value directly; a numeric view stored as its image is a later
        // slice.)
        // A NUMERIC conditional variable goes through the ONE numeric read path (NumericRenderer.FieldNum) — a
        // whole-group-aliased / Tier-B-view leaf is string-STORED (StoreAsImage) and must decode via ParseDisplay,
        // never compare its raw image to an unscaled long (diagnosis B3).
        string read = isString ? OperandText.AsString(new BoundFieldOperand(c.Parent)) : NumericRenderer.FieldNum(c.Parent).Expr;
        var tests = c.Condition.Values.Select(v => RenderMembershipTest(read, c.Parent.Item, isString, v.Low, v.High));
        return "(" + string.Join(" || ", tests) + ")";
    }

    /// <summary>One VALUE-set membership test: equality for a singleton, an inclusive bound test for a THRU range.</summary>
    private static string RenderMembershipTest(string read, DataItem parent, bool isString, string low, string? high)
    {
        if (isString)
        {
            // A level-88 VALUE compares against the conditional variable, so a figurative ALL "literal" is repeated to
            // the variable's width (ISO §8.3.3.6.4 GR2); a plain literal is decoded as-is.
            int width = parent.Pic?.Length ?? parent.ImageWidth;
            string lo = EmitText.CsLiteral(StringMembershipValue(low, width));
            if (high is null) return $"CobolString.Compare({read}, {lo}) == 0";
            return $"(CobolString.Compare({read}, {lo}) >= 0 && CobolString.Compare({read}, {EmitText.CsLiteral(StringMembershipValue(high, width))}) <= 0)";
        }
        int scale = parent.Pic?.Scale ?? 0;
        string loN = NumericMembershipValue(low, scale);
        if (high is null) return $"{read} == {loN}";
        return $"({read} >= {loN} && {read} <= {NumericMembershipValue(high, scale)})";
    }

    /// <summary>A string level-88 VALUE operand's character value: a figurative <c>ALL "literal"</c> repeated to the
    /// conditional variable's <paramref name="width"/> (ISO §8.3.3.6.4 GR2), a bare figurative WORD (QUOTE / SPACE /
    /// HIGH-VALUE / LOW-VALUE / ZERO — §8.3.1.2, materialized to the variable's width, NC250A IF--TEST-26/27),
    /// else the decoded literal.</summary>
    private static string StringMembershipValue(string raw, int width) =>
        EmitText.AllLiteralText(raw) is { } lit ? EmitText.RepeatToWidth(lit, width)
        : FigurativeFillChar(raw) is { } fill ? new string(fill, width)
        : EmitText.DecodeCobolString(raw);

    /// <summary>The fill character of a bare figurative-constant word (with or without a leading <c>ALL</c> —
    /// the same figurative either way, ISO §8.3.1.2), or null when the text is not a figurative word. The fill
    /// characters match <see cref="EmitText.FigurativeFill"/> (HIGH/LOW = U+00FF/U+0000, COBOLNET_DESIGN §14.9).</summary>
    private static char? FigurativeFillChar(string raw)
    {
        string t = raw.Trim();
        if (t.StartsWith("ALL", StringComparison.OrdinalIgnoreCase) && t.Length > 3 && char.IsWhiteSpace(t[3]))
            t = t[3..].Trim();
        return t.ToUpperInvariant() switch
        {
            "SPACE" or "SPACES" => ' ',
            "QUOTE" or "QUOTES" => '"',
            "HIGH-VALUE" or "HIGH-VALUES" => '\u00ff',
            "LOW-VALUE" or "LOW-VALUES" => '\u0000',
            "ZERO" or "ZEROS" or "ZEROES" => '0',
            _ => null,
        };
    }

    /// <summary>A numeric level-88 VALUE operand → its unscaled-<c>long</c> text. A figurative ZERO maps to <c>0</c>
    /// (ISO §8.3.1.2 — a valid numeric operand); otherwise the literal is scaled. Without this a figurative VALUE word
    /// (e.g. <c>88 IS-ZERO VALUE ZERO</c>) would reach <c>UnscaledAtScale("ZERO", …)</c> and emit a bare identifier.</summary>
    private static string NumericMembershipValue(string raw, int scale) =>
        raw.ToUpperInvariant() is "ZERO" or "ZEROS" or "ZEROES" ? "0L" : EmitText.UnscaledAtScale(raw, scale);
}
