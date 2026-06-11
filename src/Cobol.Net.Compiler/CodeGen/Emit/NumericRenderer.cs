// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Bound;
using CobolNet.Runtime;

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
    /// <summary>The intrinsic-function render dispatch (ISO §15; IntrinsicRenderer.cs) — created lazily because
    /// the two renderers are mutually recursive (an intrinsic renders its numeric arguments through THIS).</summary>
    internal IntrinsicRenderer Intrinsics => _intrinsics ??= new IntrinsicRenderer(ctx, this);
    private IntrinsicRenderer? _intrinsics;

    /// <summary>Render a bound numeric expression as a scaled long.</summary>
    public NumX Render(BoundExpr e) => e switch
    {
        BoundNumLiteral l => EmitText.UnscaledLit(l.Text),
        BoundNumRef r => FieldNum(r.Place),
        BoundIndexRef ix => new NumX(ix.IndexField, 0),   // an index IS its 1-based occurrence number (§3.5)
        BoundBinary b => Combine(Render(b.Left), b.Op.ToString(), Render(b.Right)),
        BoundNegate n => Negate(Render(n.Operand)),
        BoundPower p => Power(Render(p.Base), Render(p.Exp)),
        BoundIntrinsicCall ic => Intrinsics.RenderNum(ic),   // FUNCTION call (ISO §15)
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
        // An alphanumeric literal in a numeric context is an UNSIGNED integer (§14.9.25.3 Table 16 — the
        // alphanumeric→numeric move; NC105A's MOVE "12345" TO MOVE1), decoded exactly like an alphanumeric field.
        BoundStringLiteral s => new NumX($"CobolNum.FromAlphanumeric({EmitText.CsLiteral(s.Value)})", 0),
        BoundOperandError e => new NumX(EmitText.LoudValue("long", e.Feature), 0),
        _ => new NumX(EmitText.LoudValue("long", $"bound operand '{op.GetType().Name}'"), 0),
    };

    /// <summary>The scaled value of a data item place (its unscaled <c>long</c> value + its scale). A float item is
    /// truncated to <c>long</c> for now (mixed float/fixed arithmetic is a later slice). A non-numeric place (a group
    /// or an alphanumeric item used in a numeric context) fails loud rather than crashing the compiler (§1.4).
    /// The instance entry adds ONLY the numeric-edited de-edit (it needs the SPECIAL-NAMES emission config);
    /// every other branch lives in the context-free <see cref="FieldNumCore"/> so the static string-channel
    /// intrinsic renderer reads fields through the SAME single implementation (singular-pattern rule).</summary>
    public NumX FieldNum(Place p) =>
        p is not RefModPlace && !p.Item.StoreAsImage
            // A numeric-edited sender DE-EDITS to its numeric value at the mask's scale (ISO §14.9.25.4 GR5 — the
            // COBOL-85 de-editing move; the runtime walks the image against the mask's digit positions).
            && p.Item.Pic is { Category: PicCategory.NumericEdited, EditMask: { } dem }
        ? new NumX($"CobolEdit.DeEdit({p.Read()}, {EmitText.CsLiteral(dem)}{ctx.EditCfgArgs})",
            CobolNet.Runtime.CobolEdit.MaskScale(dem, ctx.Data.CurrencyPicSymbol, ctx.Data.DecimalPointIsComma))
        : FieldNumCore(p);

    /// <summary>The context-free numeric read of a place (every branch of <see cref="FieldNum"/> except the
    /// numeric-edited de-edit, which stays loud here — it requires the instance emission config).</summary>
    internal static NumX FieldNumCore(Place p) => p is RefModPlace
        // A reference-modified result is ALPHANUMERIC (ISO §8.4.2.4) — in a numeric context it decodes as an
        // unsigned integer exactly like an alphanumeric field (§14.9.25.3 Table 16).
        ? new NumX($"CobolNum.FromAlphanumeric({p.Read()})", 0)
        : p.Item.Pic switch
    {
        // A GROUP operand in a numeric context is its alphanumeric IMAGE as an UNSIGNED integer (a group is
        // category alphanumeric, §8.8.4.1.1; the alphanumeric→numeric move is legal, §14.9.25.3 Table 16 /
        // GR6 — NC105A's MOVE MOVE43 TO MOVE3). A mixed-usage (COMP-leaf) group stays loud (Tier-C).
        null when p.Item.IsCharacterImage =>
            new NumX($"CobolNum.FromAlphanumeric({(p is RedefViewPlace ? p.Read() : $"{p.Read()}.AsImage()")})", 0),
        null => new NumX(EmitText.LoudValue("long", $"numeric use of group item '{p.Item.CobolName ?? p.Read()}'"), 0),
        { IsFloat: true } => new NumX($"(long){p.Read()}", 0),
        // A numeric-DISPLAY leaf stored as its character image (whole-group-aliased): decode the zoned image to its
        // unscaled value for numeric use (ISO §14.6.13.2 — incompatible content decodes deterministically).
        { } pic when p.Item.StoreAsImage =>
            new NumX($"CobolNum.ParseDisplay({p.Read()}, {p.Item.ProfileName})", pic.Scale),
        // An alphanumeric operand in a numeric context is an UNSIGNED integer (ISO §14.9.25.4 GR6) — never the raw
        // string read (which would emit uncompilable C#, the bind-success ⇒ compilable invariant).
        { Category: PicCategory.Alphanumeric } => new NumX($"CobolNum.FromAlphanumeric({p.Read()})", 0),
        // The numeric-edited de-edit lives on the INSTANCE entry (it needs the SPECIAL-NAMES config); a static
        // caller (the string-channel intrinsic renderer) reaching one is a staged-out shape — loud (§1.4).
        { Category: PicCategory.NumericEdited } =>
            new NumX(EmitText.LoudValue("long", $"numeric-edited operand '{p.Item.CobolName}' in a context-free numeric read"), 0),
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

    /// <summary>Combine two scaled values with a COBOL operator, tracking the result scale (ISO §8.8.1). EVERY
    /// operation runs in <see cref="Int128"/> — the carrier (COBOLNET_DESIGN §18 #4 / numeric design D1): a product
    /// of two 18-digit operands is 36 digits and an aligned sum 19+, both past the long range MID-computation even
    /// when the final receiver fits. The leading <c>(Int128)</c> cast forces wide arithmetic whatever the leaf
    /// types; storage stays narrow (the store path truncates/rounds once, at the receiver).</summary>
    public NumX Combine(NumX a, string op, NumX b)
    {
        // STANDARD-DECIMAL arithmetic (§8.8.1.5): every operation evaluates in SDIDI form (decimal128 semantics),
        // rounded per-op to 34 significant digits with the INTERMEDIATE ROUNDING mode (§11.9.11); the receiver's
        // ROUNDED applies only at the final transfer (§14.7 NOTE 1).
        if (StandardDecimal)
            return op switch
            {
                "+" => new NumX($"CobolDec.Add({DecOperand(a)}, {DecOperand(b)}, {IntermediateMode})", 0, Dec: true),
                "-" => new NumX($"CobolDec.Sub({DecOperand(a)}, {DecOperand(b)}, {IntermediateMode})", 0, Dec: true),
                "*" => new NumX($"CobolDec.Mul({DecOperand(a)}, {DecOperand(b)}, {IntermediateMode})", 0, Dec: true),
                "/" => new NumX($"CobolDec.Div({DecOperand(a)}, {DecOperand(b)}, {IntermediateMode})", 0, Dec: true),
                _ => a,
            };
        return CombineNative(a, op, b);
    }

    private bool StandardDecimal => ctx.Data.Options.Arithmetic == ArithmeticMode.StandardDecimal;

    private string IntermediateMode => $"CobolRounding.{ctx.Data.Options.IntermediateRounding}";

    /// <summary>Render an operand in SDIDI form: an already-decimal intermediate passes through; a fixed-point
    /// value lifts EXACTLY via <c>CobolDec.From</c> (≤31 digits always representable, §8.8.1.5.2).</summary>
    public string DecOperand(NumX x) => x.Dec ? x.Expr : $"CobolDec.From({x.Expr}, {x.Scale})";

    private NumX CombineNative(NumX a, string op, NumX b) => op switch
    {
        "+" or "-" => CombineAdditive(a, op, b),
        // Multiplication: scales add (exact). Under an ON SIZE ERROR phrase the product is overflow-checked at the
        // Int128 ESCAPE boundary (~38 digits, design D1) → OverflowException maps to the size error condition
        // (§14.7.5 case 5); without the phrase it is unchecked wide multiplication.
        "*" => new NumX(ctx.InSizeErrorContext ? $"CobolNum.MulChecked({a.Expr}, {b.Expr})" : $"((Int128)({a.Expr}) * ({b.Expr}))", a.Scale + b.Scale),
        "/" => Divide(a, b),
        _ => a,
    };

    /// <summary>Guard digits past the deepest receiver/operand scale for a division NESTED inside a larger
    /// expression (numeric design D2): rounding happens ONCE, at the receiver, so the nested quotient must carry
    /// enough fraction headroom for the operations above it. 14 reproduces the legacy decimal accumulator's
    /// ~28-significant-digit behavior the golden corpus encodes.</summary>
    private const int DivGuardDigits = 14;

    /// <summary>Division quotient (ISO §8.8.1 / §14.7.4). When the working scale equals the receiver scale
    /// (<see cref="EmissionContext.TargetScale"/> — the common outermost-division case), the quotient is computed
    /// directly at the receiver scale and rounded with the receiver's mode in ONE exact step (<c>CobolNum.Divide</c>
    /// → <c>RoundDiv</c> uses the true integer remainder, so no guard digits are needed). A division NESTED inside
    /// a larger expression computes at the D2 guard scale with TRUNCATION — clamped so the Int128 radix alignment
    /// (dividend digits ≤ 18 + the alignment exponent) cannot exceed the wide engine's 38 digits — and the single
    /// receiver store performs the rounding.</summary>
    private NumX Divide(NumX a, NumX b)
    {
        int baseScale = Math.Max(ctx.TargetScale, Math.Max(a.Scale, b.Scale));
        int ds = baseScale;
        if (baseScale != ctx.TargetScale || a.Scale > ctx.TargetScale || b.Scale > ctx.TargetScale)
        {
            // Nested / higher-precision case: add guard digits, clamped to the wide engine's alignment headroom
            // (exponent = b.Scale + ds − a.Scale must keep dividend-digits + exponent ≤ 38; 18-digit operands ⇒
            // exponent ≤ 20).
            int maxExp = 20;
            int guard = Math.Min(DivGuardDigits, maxExp - (b.Scale + baseScale - a.Scale));
            ds = baseScale + Math.Max(0, guard);
        }
        CobolRounding mode = ds == ctx.TargetScale ? ctx.TargetRounding : CobolRounding.Truncation;
        // Under an ON SIZE ERROR phrase, a zero divisor must raise the size error (ISO §14.7.5 case 2): the checked
        // DivideOrThrow signals it (caught by the statement's try); otherwise Divide returns 0 unchanged.
        string fn = ctx.InSizeErrorContext ? "DivideOrThrow" : "Divide";
        return new NumX($"CobolNum.{fn}({a.Expr}, {a.Scale}, {b.Expr}, {b.Scale}, {ds}, CobolRounding.{mode})", ds);
    }

    private static NumX CombineAdditive(NumX a, string op, NumX b)
    {
        int s = Math.Max(a.Scale, b.Scale);
        return new NumX($"((Int128)({Align(a, s)}) {op} ({Align(b, s)}))", s);
    }

    /// <summary>Rescale a value's unscaled long up to <paramref name="toScale"/> (widening only here → exact).</summary>
    public static string Align(NumX x, int toScale) =>
        toScale == x.Scale ? x.Expr : $"CobolNum.Rescale({x.Expr}, {x.Scale}, {toScale}, CobolRounding.Truncation)";

    /// <summary>Exponentiation (ISO §8.8.1.2: a native-arithmetic exponentiation whose result has no exact
    /// representation is an IMPLEMENTOR-DEFINED approximation): computed in double, quantized through the ONE
    /// <c>CobolIntrinsics.FromDouble</c> (rounding) at <c>max(TargetScale, 9)</c> fraction digits. The previous
    /// scale-0 <c>(long)</c> truncation lost every fractional power result and turned the double artifact in
    /// <c>SQRT(10) ** 2</c> = 9.999999988 into 9 (IF136A F-SQRT-25); the 9-digit floor mirrors the float-intrinsic
    /// working scale (hazard H1 — TargetScale is stale in receiver-less contexts).</summary>
    private NumX Power(NumX b, NumX e)
    {
        int ws = Math.Max(ctx.TargetScale, 9);
        return new NumX($"CobolIntrinsics.FromDouble(System.Math.Pow({Real(b)}, {Real(e)}), {ws})", ws);
    }

    private static NumX Negate(NumX x) =>
        x.Dec ? new NumX($"(new CobolDec(-({x.Expr}).Sig, ({x.Expr}).Exp))", 0, Dec: true) : new($"(-{x.Expr})", x.Scale);

    // Int128 has no implicit conversion to double, so the cast is explicit before the floating divide.
    // Internal (not private): the intrinsic renderer converts float-family arguments to double through THIS
    // one scaled-value→double conversion (ISO §15.4.1 native-arithmetic family; singular-pattern rule).
    internal static string Real(NumX x) =>
        x.Dec ? $"({x.Expr}).ToDouble()"
        : x.Scale == 0 ? $"(double)({x.Expr})" : $"((double)({x.Expr}) / {Pow10D(x.Scale)})";

    /// <summary>10^<paramref name="n"/> as a C# <c>double</c> literal. Handles a NEGATIVE scale (a PICTURE-P
    /// trailing-scaled operand): 10^−1 → <c>0.1d</c>, so <see cref="Real"/>'s <c>value / 10^scale</c> scales correctly.</summary>
    private static string Pow10D(int n)
    {
        double r = 1;
        for (int i = 0; i < System.Math.Abs(n); i++) r *= 10;
        return $"{(n < 0 ? 1 / r : r).ToString(System.Globalization.CultureInfo.InvariantCulture)}d";
    }
}
