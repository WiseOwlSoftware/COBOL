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
internal sealed class ConditionRenderer(NumericRenderer num, EmissionContext ctx)
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
        // A switch-status condition (ISO §8.8.4.6 GR1): true when the external switch is at the posited position.
        BoundSwitchCondition sw => sw.TestsOn
            ? $"ExternalSwitches.Get({EmitText.CsLiteral(sw.ImplementorName)})"
            : $"!ExternalSwitches.Get({EmitText.CsLiteral(sw.ImplementorName)})",
        BoundSignCondition s => RenderSign(s),
        BoundClassCondition cc => RenderClass(cc),
        // A user-defined class (§8.8.4.1.4 / §12.3.7): operand consists entirely of the class's member characters.
        BoundUserClassCondition uc => uc.Negated
            ? $"!CobolClass.IsInClass({OperandText.AsString(uc.Operand)}, {EmitText.CsLiteral(uc.Members)})"
            : $"CobolClass.IsInClass({OperandText.AsString(uc.Operand)}, {EmitText.CsLiteral(uc.Members)})",
        BoundConditionError e => EmitText.LoudValue("bool", e.Feature),
        _ => EmitText.LoudValue("bool", $"bound condition '{c.GetType().Name}'"),
    };

    private string RenderRelational(BoundRelational r)
    {
        // Object relations FIRST (D-U8; §8.8.4.2.15 :9769 — reference IDENTITY): the figurative branch
        // below would materialize NULL against a width — nonsense for references. The only legal operand
        // shapes reached here are an object-reference field and the NULL figurative (bind-checked, 0868);
        // C# implicit upcasts cover typed-vs-universal mixes (both are CobolObject-rooted).
        static bool IsObj(BoundOperand o) =>
            o is BoundFieldOperand f && f.Place.Item.Pic?.Category == PicCategory.ObjectReference;
        if (IsObj(r.Left) || IsObj(r.Right))
        {
            static string ObjRead(BoundOperand o) => o is BoundFieldOperand f ? f.Place.Read() : "null";
            string core = $"object.ReferenceEquals({ObjRead(r.Left)}, {ObjRead(r.Right)})";
            return r.Op == "==" ? core : $"!({core})";
        }
        // Data-pointer relations (Phase-4b; §8.8.4.1.3 — ManagedPointer.SameTarget: both-NULL / same-storage;
        // the NULL figurative renders as the null carrier). Before the figurative branch (NULL must not
        // width-materialize against a pointer).
        static bool IsPtr(BoundOperand o) =>
            o is BoundFieldOperand f && f.Place.Item.Pic?.Category == PicCategory.Pointer;
        if (IsPtr(r.Left) || IsPtr(r.Right))
        {
            static string PtrRead(BoundOperand o) => o is BoundFieldOperand f ? f.Place.Read() : "null";
            string core = $"ManagedPointer.SameTarget({PtrRead(r.Left)}, {PtrRead(r.Right)})";
            return r.Op == "==" ? core : $"!({core})";
        }
        // A figurative operand (a single-character constant OR an ALL "literal") is materialized against the OTHER
        // operand's width (ISO §8.3.3.6.4 GR2), so it routes through the width-aware figurative path.
        if (r.Left is BoundFigurative or BoundAllLiteral || r.Right is BoundFigurative or BoundAllLiteral)
            return RenderFigurativeRelational(r);
        if (OperandText.IsString(r.Left) || OperandText.IsString(r.Right))
            // A signed numeric compared against an alphanumeric operand drops its sign (ISO §8.8.4.2.5 → §14.9.25.4 GR6a).
            return $"CobolString.Compare({OperandText.AsString(r.Left, deSign: true)}, {OperandText.AsString(r.Right, deSign: true)}{ctx.CollateArg}) {r.Op} 0";
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
            return $"CobolString.Compare({FigOrString(r.Left, width)}, {FigOrString(r.Right, width)}{ctx.CollateArg}) {r.Op} 0";
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

    private string FigOrString(BoundOperand op, int width) => op switch
    {
        // PCS-aware: HIGH-/LOW-VALUE materialize as the program sequence's extreme characters (§8.3.3.6 GR6/7).
        BoundFigurative f => $"new string({ctx.FigFill(f.Kind)}, {width})",
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

    /// <summary>A class condition (ISO §8.8.4.1.4). A typed-numeric operand IS NUMERIC folds to <c>true</c> ONLY
    /// when its storage is the native long/Int128 (it can only hold digits — COBOLNET_DESIGN §6.6); a numeric item
    /// whose storage is a CHARACTER window (a REDEFINES view, or a whole-group-aliased StoreAsImage leaf) can hold
    /// arbitrary characters and tests its image at run time — sign-aware for a signed zoned item (§8.8.4.4 r3,
    /// NC174A CLASS-TEST-GF-8/10: S9(18) REDEFINES X(18) holding letters is NOT numeric).</summary>
    private string RenderClass(BoundClassCondition c)
    {
        var fld = c.Operand as BoundFieldOperand;
        bool numericCategory = fld?.Place.Item.Pic?.Category is PicCategory.Numeric;
        bool numericField = numericCategory && fld!.Place is not RedefViewPlace && !fld.Place.Item.StoreAsImage;
        string arg = OperandText.AsString(c.Operand);
        string numericTest = numericCategory && fld!.Place.Item.Pic is { Signed: true } sp
            ? $"CobolClass.IsNumericZoned({arg}, {(sp.SignKind.Contains("Separate") ? "2" : "1")}, leading: {(sp.SignKind.Contains("Leading") ? "true" : "false")})"
            : $"CobolClass.IsNumeric({arg})";
        string test = c.ClassKind switch
        {
            'N' => numericField ? "true" : numericTest,
            'A' => $"CobolClass.IsAlphabetic({arg})",
            'U' => $"CobolClass.IsAlphabeticUpper({arg})",
            'L' => $"CobolClass.IsAlphabeticLower({arg})",
            _ => EmitText.LoudValue("bool", "class condition"),
        };
        return c.Negated ? $"!({test})" : $"({test})";
    }

    private string RenderCondition88(BoundCondition88 c)
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
        string read = isString ? OperandText.AsString(new BoundFieldOperand(c.Parent)) : num.FieldNum(c.Parent).Expr;
        var tests = c.Condition.Values.Select(v => RenderMembershipTest(read, c.Parent.Item, isString, v.Low, v.High));
        return "(" + string.Join(" || ", tests) + ")";
    }

    /// <summary>One VALUE-set membership test: equality for a singleton, an inclusive bound test for a THRU range.</summary>
    private string RenderMembershipTest(string read, DataItem parent, bool isString, string low, string? high)
    {
        if (isString)
        {
            // A level-88 VALUE compares against the conditional variable, so a figurative ALL "literal" is repeated to
            // the variable's width (ISO §8.3.3.6.4 GR2); a plain literal is decoded as-is.
            int width = parent.Pic?.Length ?? parent.ImageWidth;
            string lo = EmitText.CsLiteral(StringMembershipValue(low, width));
            if (high is null) return $"CobolString.Compare({read}, {lo}{ctx.CollateArg}) == 0";
            return $"(CobolString.Compare({read}, {lo}{ctx.CollateArg}) >= 0 && CobolString.Compare({read}, {EmitText.CsLiteral(StringMembershipValue(high, width))}{ctx.CollateArg}) <= 0)";
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
    private string StringMembershipValue(string raw, int width) =>
        EmitText.AllLiteralText(raw) is { } lit ? EmitText.RepeatToWidth(lit, width)
        : FigurativeFillChar(raw) is { } fill ? new string(fill, width)
        : EmitText.DecodeCobolString(raw);

    /// <summary>The fill character of a bare figurative-constant word (with or without a leading <c>ALL</c> —
    /// the same figurative either way, ISO §8.3.1.2), or null when the text is not a figurative word. The fill
    /// characters match <see cref="EmitText.FigurativeFill"/> (HIGH/LOW = U+00FF/U+0000, COBOLNET_DESIGN §14.9).</summary>
    private char? FigurativeFillChar(string raw)
    {
        string t = raw.Trim();
        if (t.StartsWith("ALL", StringComparison.OrdinalIgnoreCase) && t.Length > 3 && char.IsWhiteSpace(t[3]))
            t = t[3..].Trim();
        return t.ToUpperInvariant() switch
        {
            "SPACE" or "SPACES" => ' ',
            "QUOTE" or "QUOTES" => '"',
            "HIGH-VALUE" or "HIGH-VALUES" => ctx.Data.Collating?.HighValue ?? 'ÿ',
            "LOW-VALUE" or "LOW-VALUES" => ctx.Data.Collating?.LowValue ?? ' ',
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
