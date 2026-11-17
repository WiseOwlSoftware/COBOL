// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Runtime.CompilerServices;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Common;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Binding.Bound;

/// <summary>The receiver facts an expression's lanes depend on (numeric design D16 and D2): a floating-point receiver takes every
/// operation onto binary64, the receiver's scale sets a nested quotient's working scale, and a floating-point numeric-edited receiver
/// makes every quotient a final one. <see cref="KeepsSignificance"/> is an argument a floating-point receiver takes the function's value
/// of (<see cref="NumericLanes.ArgumentKeepsSignificance"/>, kb/Work PB5893): a quotient in it that may not terminate is final.
/// <see cref="Argument"/> is an expression with no receiver: an intrinsic function's argument at an integer position as the type 6
/// classifier reads it (kb/Work PB5891 review R3-1).</summary>
internal readonly record struct LaneContext(ArithmeticMode Arithmetic, bool FloatReceiver, int ReceiverScale, bool FloatEdited,
    bool KeepsSignificance = false)
{
    public static LaneContext Argument(ArithmeticMode arithmetic) => new(arithmetic, false, 0, false);

    /// <summary>⛔ THE CONTEXT OF A FUNCTION'S ARGUMENT AT <paramref name="integerPosition"/> RENDERED IN THIS ONE, the bound twin of
    /// <c>NumericRenderer.ArgumentReceiver</c> (kb/Work PB5893 review R2-1): no floating-point receiver facts, the same scale, and
    /// significance by the one rule <see cref="NumericLanes.ArgumentKeepsSignificance"/> over this context's floating-point receiver (a
    /// binary64 one or an edited one) or its own significance.</summary>
    public LaneContext ArgumentContext(bool integerPosition) => this with
    {
        FloatReceiver = false, FloatEdited = false,
        KeepsSignificance = NumericLanes.ArgumentKeepsSignificance(FloatReceiver || FloatEdited || KeepsSignificance, integerPosition),
    };

    /// <summary>Is a quotient by <paramref name="divisor"/> in this context the final one (<see cref="NumericLanes.QuotientIsFinal"/>)?</summary>
    public bool FinalQuotient(BoundExpr divisor) =>
        NumericLanes.QuotientIsFinal(FloatEdited, KeepsSignificance, NumericLanes.TerminatesWithinGuardDigits(divisor));

    public bool DecimalEngine => ArithmeticModes.IsDecimalEngine(Arithmetic);
}

/// <summary>⛔ THE LANE OF ONE BOUND NODE (kb/Work PB5891 review R3-1): the set of carriers the code generator may hold its value on
/// (one, wherever the node's facts decide it; several where a fact only the generator knows decides it: a function value's scale and
/// digit bound), and, where it is one exact-lane carrier the facts decide, its scale, its digit bound as the generator states it
/// (<c>NumX.Digits</c>, 0 = none stated) and its fraction precision (<c>NumX.Precision</c>).</summary>
internal readonly record struct NodeLane(IReadOnlySet<NumericLane> Lanes, bool Known, int Scale, int Digits, int? Precision)
{
    public static NodeLane Of(NumericLane lane, int scale = 0, int digits = 0, int? precision = null) =>
        new(new HashSet<NumericLane> { lane }, true, scale, digits, precision);

    public static NodeLane Any(params NumericLane[] lanes) => new(new HashSet<NumericLane>(lanes), false, 0, 0, null);

    public NumericLane Single => Lanes.Single();

    /// <summary><c>NumX.FractionPrecision</c> of an exact-lane value: its precision, else its scale.</summary>
    public int FractionPrecision => Precision ?? Scale;

    /// <summary>Is the value an integer BY VALUE on the Int128 lane (<see cref="NumericLanes.IsIntegerByValue"/>), for certain?</summary>
    public bool IntegerByValue => Known && NumericLanes.IsIntegerByValue(Single, Scale);
}

internal static partial class NumericLanes
{
    private static readonly ConditionalWeakTable<BoundExpr, Dictionary<LaneContext, NodeLane>> Memo = new();

    /// <summary>⛔ THE LANE OF A NUMERIC NODE, ONE PURE FUNCTION OF THE BOUND NODE (kb/Work PB5891 review R3-1, the coordinator's ruling):
    /// the code generator asserts every rendered operation's carrier lies in it (<c>NumericRenderer.AssertLane</c>, an internal compiler
    /// error otherwise), and the type 6 classifier bounds each intermediate's exactness by every lane in it. Computed once per node and
    /// context. It states the generator's rules (<c>NumericRenderer.CombineCore</c>, <c>CombineAdditive</c>, <c>Multiply</c>,
    /// <c>Divide</c>, <c>Power</c>, <c>FieldNum</c>, <c>LiteralNum</c>) for a node that is not the final transfer of a statement.</summary>
    internal static NodeLane LaneOf(BoundExpr e, LaneContext ctx)
    {
        var byContext = Memo.GetOrCreateValue(e);
        lock (byContext)
        {
            if (byContext.TryGetValue(ctx, out var known)) return known;
        }
        var lane = Compute(e, ctx);
        lock (byContext) byContext[ctx] = lane;
        return lane;
    }

    private static NodeLane Compute(BoundExpr e, LaneContext ctx) => e switch
    {
        BoundNumLiteral l => LiteralLane(l, ctx),
        BoundNumRef r => PlaceLane(r.Place),
        BoundIndexRef or BoundLinageCounterRef or BoundReportCounterRef or BoundReportVaryingRef or BoundOdoExtent
            or BoundExprError or BoundUdfEvaluatedExpr => NodeLane.Of(NumericLane.Scaled),
        BoundIntrinsicCall call => CallLane(call, ctx),
        BoundNegate n => NegateLane(LaneOf(n.Operand, ctx), ctx),
        BoundBinary b => BinaryLane(LaneOf(b.Left, ctx), b.Op, LaneOf(b.Right, ctx), ctx, b.Op == '/' && ctx.FinalQuotient(b.Right)),
        BoundPower pw => PowerLane(pw, ctx),
        _ => NodeLane.Any(NumericLane.Scaled, NumericLane.Wide, NumericLane.Exact, NumericLane.Sdidi, NumericLane.Binary64,
            NumericLane.Approximation),
    };

    /// <summary><c>NumericRenderer.Visit(BoundNumLiteral)</c> / <c>LiteralNum</c> / <c>EmitText.UnscaledLit</c>.</summary>
    private static NodeLane LiteralLane(BoundNumLiteral l, LaneContext ctx)
    {
        if (l.Carrier is not null && ctx.FloatReceiver && !ctx.DecimalEngine) return NodeLane.Of(NumericLane.Binary64);
        if (NumericLiteral.IsFloatingPointForm(l.Text) && NumericLiteral.TryParseExact(l.Text, out var sig, out int exp10))
            return NodeLane.Of(NumericLane.Sdidi, precision: CobolDec.FractionDigits(new CobolDec(sig, exp10)));
        if (!NumericLiteral.TryUnscaledParts(l.Text, out string digits, out int scale)) return NodeLane.Of(NumericLane.Binary64);
        return NodeLane.Of(NumericLane.Scaled, scale, NumericLiteral.UnscaledDigitBound(digits));
    }

    /// <summary><c>NumericRenderer.FieldNum</c> / <c>FieldNumCore</c> / <c>WindowedNum</c>.</summary>
    private static NodeLane PlaceLane(Place p)
    {
        // ⛔ NO STORAGE DECISION IS READ (StoreAsImage is laid out after binding): a numeric item's read states its description's scale
        // and digit bound whether storage holds it as a field or an image (NumericRenderer.WindowedNum states them too); an edited item
        // is de-edited onto the SDIDI or the Int128 lane by how it is stored, so both are allowed.
        if (p is TableAllPlace all) return PlaceLane(all.Element);
        if (p is RefModPlace) return NodeLane.Of(NumericLane.Scaled);
        return p.Item.Pic switch
        {
            null => NodeLane.Of(NumericLane.Scaled),
            { IsFloat: true } => NodeLane.Of(p.Item.HoldsApproximation ? NumericLane.Approximation : NumericLane.Binary64),
            { Category: PicCategory.NumericEdited, IsFloatEdited: true } => NodeLane.Any(NumericLane.Sdidi, NumericLane.Scaled),
            { Category: PicCategory.NumericEdited } or { LocaleEdit: not null } => NodeLane.Any(NumericLane.Scaled),
            { Category: PicCategory.Alphanumeric or PicCategory.National or PicCategory.Boolean } => NodeLane.Of(NumericLane.Scaled),
            { } pic => NodeLane.Of(NumericLane.Scaled, pic.Scale, pic.UnscaledDigitBound),
        };
    }

    /// <summary>A function value: the carrier the bind-time prediction gives it (<see cref="FunctionValueLane"/>, from the call's catalog
    /// facts and the carriers of its arguments), its scale and digit bound the code generator's alone. An exact value may be on the Int128
    /// lane or its wide and run-time-scaled forms.
    /// <para>⛔ The prediction reads the arguments in the context the renderer gives them (<see cref="LaneContext.ArgumentContext"/>, kb/Work
    /// PB5893 reviews R2-1, R3-1) and is the one decider of the call's family: <c>IntrinsicRenderer.RenderNum</c> takes the float lane exactly
    /// when <see cref="FunctionValueLane.IsBinary64"/> says so.</para></summary>
    private static NodeLane CallLane(BoundIntrinsicCall call, LaneContext ctx) =>
        ctx.DecimalEngine
            ? NodeLane.Any(NumericLane.Scaled, NumericLane.Wide, NumericLane.Exact, NumericLane.Sdidi)
            : FunctionValueLane.LaneOf(call, ctx) switch
            {
                NumericLane.Scaled => NodeLane.Any(NumericLane.Scaled, NumericLane.Wide, NumericLane.Exact),
                NumericLane.Sdidi => NodeLane.Any(NumericLane.Sdidi, NumericLane.Exact, NumericLane.Wide, NumericLane.Scaled),
                var lane => NodeLane.Of(lane),
            };

    /// <summary><c>NumericRenderer.Visit(BoundNegate)</c> / <c>Negate</c>: the carrier is kept (an unsigned-wide value under a decimal
    /// engine enters the SDIDI).</summary>
    private static NodeLane NegateLane(NodeLane o, LaneContext ctx) =>
        ctx.DecimalEngine && o.Lanes.Contains(NumericLane.Scaled) ? new(new HashSet<NumericLane>(o.Lanes) { NumericLane.Sdidi }, false, 0, 0, null) : o;

    /// <summary>One binary operation over operands that may each be on several lanes: the union of the single-lane answers.</summary>
    private static NodeLane BinaryLane(NodeLane l, char op, NodeLane r, LaneContext ctx, bool finalQuotient)
    {
        if (ctx.DecimalEngine) return NodeLane.Of(NumericLane.Sdidi);
        if (l.Known && r.Known) return Binary(l, op, r, ctx, finalQuotient);
        var lanes = new HashSet<NumericLane>();
        foreach (var a in l.Lanes)
            foreach (var b in r.Lanes)
                lanes.UnionWith(Binary(Unknown(a, l), op, Unknown(b, r), ctx, finalQuotient).Lanes);
        return new(lanes, false, 0, 0, null);
    }

    private static NodeLane Unknown(NumericLane lane, NodeLane of) => of.Known ? of : new(new HashSet<NumericLane> { lane }, false, 0, 0, null);

    /// <summary><c>NumericRenderer.CombineCore</c> over two operands on one lane each.</summary>
    private static NodeLane Binary(NodeLane l, char op, NodeLane r, LaneContext ctx, bool finalQuotient)
    {
        var (a, b) = (l.Lanes.Single(), r.Lanes.Single());
        var lane = Combine(a, b, ctx.FloatReceiver, decimalEngine: false);
        bool known = l.Known && r.Known;
        switch (lane)
        {
            case NumericLane.Binary64:
                return NodeLane.Of(NumericLane.Binary64);
            case NumericLane.Sdidi:
                return NodeLane.Of(NumericLane.Sdidi);
            case NumericLane.Exact when a is NumericLane.Exact || b is NumericLane.Exact || Lifted(a) is NumericLane.Sdidi
                || Lifted(b) is NumericLane.Sdidi:
                // ExactBinary / ExactQuotient; a final quotient (a floating-point edited receiver) is the SDIDI.
                return op == '/' && finalQuotient ? NodeLane.Of(NumericLane.Sdidi) : NodeLane.Of(NumericLane.Exact);
        }
        // CombineNative: the Int128 lane and its wide form, decided by the digit bounds.
        if (!known) return NodeLane.Any(NumericLane.Scaled, NumericLane.Wide, NumericLane.Exact);
        return op switch
        {
            '+' or '-' => Additive(l, r),
            '*' => Product(l, r),
            '/' => Quotient(l, r, ctx, finalQuotient),
            _ => l,
        };
    }

    private static int Bound(NodeLane x) => x.Digits > 0 ? x.Digits : PicInfo.CarrierDigitBound;

    private static int? Carried(NodeLane a, NodeLane b, int value) => a.Precision is null && b.Precision is null ? null : value;

    /// <summary><c>NumericRenderer.CombineAdditive</c>.</summary>
    private static NodeLane Additive(NodeLane a, NodeLane b)
    {
        int s = Math.Max(a.Scale, b.Scale);
        int? precision = Carried(a, b, Math.Max(a.FractionPrecision, b.FractionPrecision));
        int digits = a.Digits > 0 && b.Digits > 0 ? Math.Max(a.Digits + (s - a.Scale), b.Digits + (s - b.Scale)) + 1 : 0;
        if (a.Single == NumericLane.Wide || b.Single == NumericLane.Wide)
        {
            int wideDigits = Math.Max(Bound(a) + (s - a.Scale), Bound(b) + (s - b.Scale)) + 1;
            return wideDigits <= CobolWide.MaxDigits ? NodeLane.Of(NumericLane.Wide, s, wideDigits, precision) : NodeLane.Of(NumericLane.Exact);
        }
        bool shifted = a.Scale != s || b.Scale != s;
        int alignedBound = Math.Max(Bound(a) + (s - a.Scale), Bound(b) + (s - b.Scale));
        bool alignmentLeavesLane = shifted && alignedBound > PicInfo.ScaledCarrierDigits;
        bool knownSumLeavesLane = digits > PicInfo.ScaledCarrierDigits && a.Digits <= PicInfo.ScaledCarrierDigits
            && b.Digits <= PicInfo.ScaledCarrierDigits;
        if (alignmentLeavesLane || knownSumLeavesLane) return NodeLane.Of(NumericLane.Exact);
        if (digits > PicInfo.ScaledCarrierDigits) digits = 0;
        return NodeLane.Of(NumericLane.Scaled, s, digits, precision);
    }

    /// <summary><c>NumericRenderer.Multiply</c> (a node that is not the final transfer).</summary>
    private static NodeLane Product(NodeLane a, NodeLane b)
    {
        int digits = Bound(a) + Bound(b);
        int? precision = Carried(a, b, a.FractionPrecision + b.FractionPrecision);
        if (a.Single == NumericLane.Wide || b.Single == NumericLane.Wide || digits > PicInfo.ScaledCarrierDigits)
            return digits <= CobolWide.MaxDigits ? NodeLane.Of(NumericLane.Wide, a.Scale + b.Scale, digits, precision) : NodeLane.Of(NumericLane.Exact);
        return NodeLane.Of(NumericLane.Scaled, a.Scale + b.Scale, digits, precision);
    }

    /// <summary><c>NumericRenderer.Divide</c> over two exact operands (a node that is not the final transfer).</summary>
    private static NodeLane Quotient(NodeLane a, NodeLane b, LaneContext ctx, bool finalQuotient)
    {
        if (finalQuotient) return NodeLane.Of(NumericLane.Sdidi);   // ExactFinalQuotient: no nested quotient
        int precision = Math.Max(a.FractionPrecision, b.FractionPrecision);
        int w = CobolNum.NestedQuotientScale(ctx.ReceiverScale, precision);
        int digits = Math.Max(0, Bound(a) - a.Scale + b.Scale) + w;
        bool wide = a.Single == NumericLane.Wide || b.Single == NumericLane.Wide;
        if (!wide && digits <= PicInfo.ScaledCarrierDigits) return NodeLane.Of(NumericLane.Scaled, w, digits, precision);
        int divisorDigits = Bound(b) + Math.Max(0, a.Scale - b.Scale - w);
        if (digits <= CobolWide.MaxDigits && divisorDigits <= CobolWide.MaxDigits) return NodeLane.Of(NumericLane.Wide, w, digits, precision);
        return NodeLane.Of(NumericLane.Exact);
    }

    /// <summary><c>NumericRenderer.Visit(BoundPower)</c> / <c>Power</c> (a node that is not the final transfer).</summary>
    private static NodeLane PowerLane(BoundPower pw, LaneContext ctx)
    {
        if (ctx.DecimalEngine) return NodeLane.Of(NumericLane.Sdidi);
        // An integer-valued literal base or exponent is read as a scale-0 Int128 operand with no stated bound (IntegerValuedLiteral).
        var b = NumericLiteral.TryGetIntegerValue((pw.Base as BoundNumLiteral)?.Text ?? "", out _) ? NodeLane.Of(NumericLane.Scaled)
            : LaneOf(pw.Base, ctx);
        bool literalExponent = pw.Exp is BoundNumLiteral el && NumericLiteral.TryGetIntegerValue(el.Text, out _);
        var e = literalExponent ? NodeLane.Of(NumericLane.Scaled) : LaneOf(pw.Exp, ctx);
        Int128? n = pw.Exp is BoundNumLiteral nl && NumericLiteral.TryGetIntegerValue(nl.Text, out Int128 v) ? v : null;
        var lanes = new HashSet<NumericLane>();
        foreach (var bl in b.Lanes)
            foreach (var elane in e.Lanes)
            foreach (bool integerScale in e.Known ? [e.Scale <= 0] : new[] { true, false })
            {
                // The exponent enters lowered (a wide or run-time exact value has no power form: it is the SDIDI); it is an integer by
                // value when it stays on the Int128 lane at scale 0 or below (IsIntegerByValue), which only a known scale decides.
                var lowered = elane is NumericLane.Wide or NumericLane.Exact ? NumericLane.Sdidi : elane;
                bool integer = IsIntegerByValue(lowered, integerScale ? 0 : 1);
                var baseLane = bl;
                if (!(baseLane is NumericLane.Wide or NumericLane.Exact) || !integer)
                    baseLane = baseLane is NumericLane.Wide or NumericLane.Exact ? NumericLane.Sdidi : baseLane;
                if (!IsBinary64(baseLane) && lowered == NumericLane.Scaled && integer)
                {
                    // The literal arm: a positive literal exponent over an Int128-lane or wide base with a stated bound is wide (the base
                    // itself for 1); every other integer power is the run-time exact power.
                    if (n is { } k && k >= 1 && b.Known && b.Digits > 0 && baseLane is NumericLane.Scaled or NumericLane.Wide
                        && (long)b.Digits * (long)k <= CobolWide.MaxDigits)
                        lanes.Add(k == 1 ? baseLane : NumericLane.Wide);
                    else if (n is { } k1 && k1 >= 1 && !b.Known && baseLane is NumericLane.Scaled or NumericLane.Wide)
                        lanes.UnionWith([baseLane, NumericLane.Wide, NumericLane.Exact]);
                    else lanes.Add(NumericLane.Exact);
                    continue;
                }
                // A non-integer power over a run-time exact operand is formed in that carrier; any other is the engine's binary64.
                if (bl == NumericLane.Exact || elane == NumericLane.Exact) { lanes.Add(NumericLane.Exact); continue; }
                lanes.Add(bl == NumericLane.Binary64 || elane == NumericLane.Binary64 ? NumericLane.Binary64 : NumericLane.Approximation);
            }
        if (lanes.Count == 1 && lanes.Single() == NumericLane.Wide && n is { } m && b.Known && m > 1)
            return NodeLane.Of(NumericLane.Wide, b.Scale * (int)m, b.Digits * (int)m, b.Precision is null ? null : b.FractionPrecision * (int)m);
        if (lanes.Count == 1 && n == 1 && b.Known && lanes.Single() == b.Single) return b;
        return lanes.Count == 1 && lanes.Single() is not (NumericLane.Scaled or NumericLane.Wide)
            ? NodeLane.Of(lanes.Single())
            : new(lanes, false, 0, 0, null);
    }
}
