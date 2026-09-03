// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.Runtime;

namespace CobolNet.CodeGen.Emit;

/// <summary>
/// Renders a bound numeric expression / operand to a scale-tracked native-integer C# expression (<see cref="NumX"/>).
/// COBOL fixed-point arithmetic operates on the algebraic value regardless of representation (ISO §8.8.1): operands
/// are unscaled longs carrying their scale, the renderer aligns scales for ±, adds them for ×, and computes a
/// quotient at the RECEIVER's working scale (<see cref="ReceiverContext.Scale"/> — P7 Step 3: the receiver
/// travels by parameter into every public entry, never mutable context state) for ÷. The receiver's store
/// (truncation / capacity) is applied later by <c>CobolNum.Store</c>.
/// </summary>
internal sealed class NumericRenderer(EmitContext ctx, EcState ecState) : IBoundExprVisitor<NumX>, IBoundOperandVisitor<NumX>
{
    /// <summary>THE checked-kernel decision (kb/Work PB91): a render under an ON SIZE ERROR / EC-SIZE-checked
    /// ARITHMETIC statement (<c>_rcv.InSizeError</c>, threaded by ArithmeticEmitter) — and, since PB91, any
    /// RECEIVER-LESS render (a condition, an argument, a subscript, …) inside a statement that has EC-SIZE-*
    /// checking enabled (<see cref="EcState.SizeChecking"/>): §14.7.5 no-phrase rule 3 makes such an
    /// intermediate overflow EC-SIZE-OVERFLOW, and §14.6.13.1.3 its disposition fatal.</summary>
    private bool Checked => _rcv.InSizeError || (_rcv.Receiverless && ecState.SizeChecking);

    /// <summary>The intrinsic-function render dispatch (ISO §15; IntrinsicRenderer.cs) — created lazily because
    /// the two renderers are mutually recursive (an intrinsic renders its numeric arguments through THIS).</summary>
    internal IntrinsicRenderer Intrinsics => _intrinsics ??= new IntrinsicRenderer(ctx, this);
    private IntrinsicRenderer? _intrinsics;

    /// <summary>The declared PROGRAM COLLATING SEQUENCE tables (§12.3.7) — exposed so a bare figurative in a value
    /// context materializes the runtime-collating extreme (§8.3.3.6.4 GR6/GR7), not the native pin. Null when no
    /// non-native sequence is declared, in which case <c>FigurativeConstants.Fill</c> returns the native pin
    /// (the byte-stable common case). The MOVE and relation paths already thread the same tables.</summary>
    internal AlphabetDef? Collating => ctx.Data.Collating;
    internal NationalAlphabetDef? NationalCollating => ctx.Data.NationalCollating;

    // Render/AsNum dispatch through the generated exhaustive visitors (PHASE-07 Step 6d): every BoundExpr / BoundOperand
    // leaf has a Visit below, so a new leaf is a COMPILE error here — the former loud `_ =>` defaults are gone.

    // ── The receiver context (P7 Step 3). Every PUBLIC entry REQUIRES the caller's ReceiverContext and
    // stores it here; the private Visit recursion reads the field. The generated single-arg visitors cannot
    // thread a parameter (Accept<T> has no TArg), and one render tree serves exactly ONE receiver — the context
    // is constant across the recursion — so a freshly-assigned-at-every-entry field is equivalent to a
    // parameter: the H1 hazard (a render inheriting the PREVIOUS statement's receiver) is impossible because no
    // entry exists that does not take the context. (DESIGN-codegen-backend §2.5, updated for the landed
    // generated visitor.) ──
    private ReceiverContext _rcv = ReceiverContext.None;

    /// <summary>True while rendering the operand sub-tree of an EC-DATA-NOT-FINITE-EXEMPT context (a sign condition
    /// §14.6.13.2 dash-2, or the same-usage MOVE source dash-3): the float read chokepoint (<see cref="FieldNumCore"/>
    /// line 140) then emits the RAW <c>(double)(Read(p))</c> instead of the checked <c>CobolFloat.Sending(…)</c> wrap.
    /// Re-entrant save/restore (like <see cref="_rcv"/>): propagates through the direct <c>.Accept</c> recursion of the
    /// exempt operand, and resets at a fresh <see cref="Render"/>/<see cref="AsNum"/> entry (an intrinsic argument read
    /// is a non-exempt reference — the narrow reading of the dash-2/dash-3 exemption).</summary>
    private bool _floatSendingExempt = false;

    /// <summary>True while rendering a division node whose quotient is transferred DIRECTLY to a single resultant
    /// identifier — the "final transfer" of §14.7.7 rule 3 NOTE 1. Set by the emit sites that own that transfer
    /// (a single-receiver COMPUTE RHS, a DIVIDE top-level division); an outermost division computes at the
    /// receiver's scale + ROUNDED mode in one exact step. A division NESTED inside a larger expression is NOT the
    /// final transfer (ROUNDED applies only to that final transfer, §14.7 NOTE 1; PROHIBITED tests only the
    /// resultant identifier, §14.7.4.3 GR7), so it always computes at the D2 guard scale with TRUNCATION — never
    /// inheriting the receiver's mode. Re-entrant save/restore like <see cref="_rcv"/>; <see cref="Visit(BoundBinary)"/>
    /// clears it for a node's children (they are never the final transfer) and restores it for the node's own
    /// combine. The OLD <c>ds == _rcv.Scale</c> heuristic in <see cref="Divide"/> was a wrong proxy for this — it
    /// let a nested integer-operand division inherit PROHIBITED (spurious size error) and let a multi-receiver root
    /// division miss PROHIBITED (CA5).</summary>
    private bool _outermost = false;

    /// <summary>The receiver of the render currently in progress — read by the mutually-recursive
    /// <see cref="IntrinsicRenderer"/> (an intrinsic's working scale/mode derive from the SAME receiver).</summary>
    internal ReceiverContext Receiver => _rcv;

    /// <summary>Render a bound numeric expression as a scaled long, computed FOR <paramref name="rcv"/>.
    /// Save/restore makes every public entry RE-ENTRANT (P7 Step 12): the instance string channel renders an
    /// intrinsic's numeric arguments under <see cref="ReceiverContext.None"/> MID-render, and the outer render
    /// must resume under its own receiver — the H1 staleness class stays closed by construction.</summary>
    public NumX Render(BoundExpr e, in ReceiverContext rcv, bool floatSendingExempt = false, bool outermost = false)
    {
        var saved = _rcv;
        var savedEx = _floatSendingExempt;
        var savedOut = _outermost;
        _rcv = rcv;
        _floatSendingExempt = floatSendingExempt;
        _outermost = outermost;
        try { return e.Accept(this); }
        finally { _rcv = saved; _floatSendingExempt = savedEx; _outermost = savedOut; }
    }

    /// <summary>Render a bound expression that the FORMAT admits only as an OPERAND — PERFORM VARYING's FROM / BY
    /// (ISO §14.9.28.2: identifier, index-name or literal; the binder types them BoundExpr for convenience). A bare
    /// floating-point literal keeps its EXACT value (kb/Work PB99: `FROM 1.0E+3` set a 20-decimal item to
    /// 999.999…9161, the binary64 1E+23 product) — which is now what <see cref="LiteralNum"/> gives it in EVERY
    /// position, so this is plain <see cref="Render"/> under <see cref="ReceiverContext.None"/>.
    /// <para>⛔ THE OPERAND-vs-EXPRESSION SPLIT IS GONE (owner decision D-B, kb/Work PB156/PB195). It existed only
    /// because a literal rendered differently in the two positions; one rendering means one path, and the named
    /// method survives solely to document WHY the format admits no expression here.</para></summary>
    public NumX RenderOperandLike(BoundExpr e) => Render(e, ReceiverContext.None);

    /// <summary>Render a bound operand as a scaled native-integer value, computed FOR <paramref name="rcv"/>
    /// (re-entrant — see <see cref="Render"/>). <paramref name="floatSendingExempt"/> suppresses the float
    /// finiteness wrap for an EC-DATA-NOT-FINITE-exempt operand (a same-usage MOVE source).</summary>
    public NumX AsNum(BoundOperand op, in ReceiverContext rcv, bool floatSendingExempt = false, bool outermost = false)
    {
        var saved = _rcv;
        var savedEx = _floatSendingExempt;
        var savedOut = _outermost;
        _rcv = rcv;
        _floatSendingExempt = floatSendingExempt;
        _outermost = outermost;
        try { return op.Accept(this); }
        finally { _rcv = saved; _floatSendingExempt = savedEx; _outermost = savedOut; }
    }

    // ── IBoundExprVisitor<NumX> ──────────────────────────────────────────────────────────────────────────────
    public NumX Visit(BoundNumLiteral n) => LiteralNum(n.Text);

    /// <summary>⛔ THE ONE numeric-literal → <see cref="NumX"/> rendering, for EVERY position and EVERY arithmetic
    /// mode (owner decision D-B, 2026-08-30; kb/Work PB156 + PB195). A FLOATING-POINT literal is its EXACT
    /// ISO §8.3.3.3.3 rule-5 value — "the algebraic product of the value of its significand and the quantity derived
    /// by raising ten to the power of the exponent" — carried on the Dec (SDIDI) lane; a fixed-point literal is the
    /// scaled-integer rendering of <see cref="EmitText.UnscaledLit"/>, unchanged.
    /// <para>WHY THE NATIVE ARM IS EXACT TOO. §14.9.2.4 GR4 (and its §14.9.44.4 GR4 SUBTRACT twin) says: "When
    /// native arithmetic is in effect and none of the operands is described with usage binary-char, binary-short,
    /// binary-long, binary-double, float-short, float-long, or float-extended, enough places shall be carried so as
    /// not to lose any significant digits during execution." A LITERAL is not "described with usage" anything, and
    /// §14.7.7 rule 2 proves the drafters spell a float literal out as its OWN bullet when they mean to include it —
    /// GR4 does not. So `ADD 1.0E+0 TO W` over a PIC 9(20) holding 12345678901234567890 owed …891 and a binary64
    /// operand delivered …168. Numeric design D16's §8.8.1.3 latitude is therefore NARROWED to what it always
    /// described: an expression touching a float ITEM or a float RECEIVER still evaluates in IEEE binary64 (the
    /// <c>Real</c> lane keeps precedence in <see cref="CombineCore"/> — untouched by D-B).</para>
    /// <para>⛔ THE INVARIANT THIS BUYS, STATED EXACTLY (it was written as "a literal's NOTATION never changes the
    /// arithmetic — only its VALUE does", and that sentence is FALSE — kb/Work PB274): the VALUE a literal
    /// contributes is its exact §8.3.3.3.3 rule-5 value, in every position and every arithmetic mode, and no
    /// operand ever arrives rounded to binary64. `ADD 1.0E+0` computes what `ADD 1.0` computes and
    /// `2 ** 0.5E+0` what `2 ** 0.5` computes, because those are the same VALUES.</para>
    /// <para>What the notation still selects, under NATIVE arithmetic, is the INTERMEDIATE LANE, and that is a
    /// published §8.8.1.3 determination rather than an invariant (CONFORMANCE.md A.1 item 82): a float literal
    /// operand routes the statement onto the decimal128 lane (34 significant digits) where a fixed-point literal
    /// keeps the Int128 one (~38), so `A * B + 0.0E+0` and `A * B + 0.0` differ for 18-digit A and B — and
    /// INTERMEDIATE ROUNDING (§11.9.11) reaches the first and not the second. See <see cref="CombineCore"/>.</para>
    /// <para>Residual, documented (CONFORMANCE.md §7 / A.1 item 82): §8.3.3.3.3 rule 2 admits a 36-digit
    /// significand and the SDIDI carrier holds 34, so a 35th/36th significand digit rounds at
    /// <c>CobolDec.FromParsed</c>.</para>
    /// <para>Every consumer of the Dec lane already exists — the PB84 landing/store funnel; <see cref="Real"/>
    /// converts it where a binary64 body wants a double; <see cref="DecOperand"/> where an SDIDI body does.</para></summary>
    private NumX LiteralNum(string text) =>
        CobolNet.Common.NumericLiteral.IsFloatingPointForm(text)
            && CobolNet.Common.NumericLiteral.TryParseExact(text, out var sig, out int exp10)
        ? new NumX(RuntimeApi.DecFromParsedLiteral(sig, exp10, IntermediateMode), 0, Dec: true)
        : EmitText.UnscaledLit(text);
    public NumX Visit(BoundNumRef n) => FieldNum(n.Place);
    public NumX Visit(BoundIndexRef n) => new(n.IndexField, 0);   // an index IS its 1-based occurrence number (§3.5)
    // The LINAGE-COUNTER register (ISO §8.4.3.14 GR1): an unsigned INTEGER read from the file connector —
    // runtime-sourced (only the I-O control system modifies it, §13.18.34 GR7b), scale 0.
    public NumX Visit(BoundLinageCounterRef n) => new($"CobolFile.LinageCounter({EmitText.FileKeyExpr(n.File)})", 0);
    // LINE-COUNTER / PAGE-COUNTER (ISO §8.4.3.15 GR1): unsigned integers read from the report's engine instance —
    // runtime-sourced (only the RWCS maintains them), scale 0. This ONE case serves both the relation-condition and
    // MOVE-source paths (both route through the renderer).
    public NumX Visit(BoundReportCounterRef n) =>
        new($"__RPT_{n.Report.CsIndex}.{(n.IsPage ? "PageCounter" : "LineCounter")}", 0);
    // A SUM counter read (ISO §13.18.54.4 GR4 — the counter is its printable entry's source item): an unscaled
    // integer at the counter's PICTURE-derived scale (GR1), engine-sourced.
    public NumX Visit(BoundReportSumRef n) => new($"__RPT_{n.Report.CsIndex}.SumValue({EmitText.CsLiteral(n.Id)})", n.Scale);
    // A report VARYING counter read (ISO §13.18.64.4 GR3/GR4): the compose-local integer counter, scale 0.
    public NumX Visit(BoundReportVaryingRef n) => new(n.CsName, 0);
    // An OCCURS DEPENDING table's current extent (ISO §13.18.38 GR8; the §15.50.4 r4b / §15.14.4 r2b channel,
    // kb/Work PB61): CobolTable.OdoExtent over data-name-1's current count — scale 0, EC-BOUND-ODO inside.
    public NumX Visit(BoundOdoExtent n)
    {
        string extent = RuntimeApi.TableOdoExtent(RuntimeApi.TableOcc(PlaceRenderer.Read(n.Depending)),
            n.MinOccurs, n.MaxOccurs, n.FixedWidth, n.ElemWidth);
        // A BASED entry not associated with actual data answers GR8b's maximum (§15.50.4 r4a / §15.14.4 r2a —
        // kb/Work PB80); the null test comes FIRST, before data-name-1 (possibly inside the entry) is read.
        return new(n.BasedAddress is { } addr
            ? $"({RuntimeApi.PtrIsNull(addr)} ? (long){n.FixedWidth + (long)n.MaxOccurs * n.ElemWidth} : {extent})"
            : extent, 0);
    }
    public NumX Visit(BoundBinary n)
    {
        // A binary node's CHILDREN are never the final transfer (a sub-expression operand), so they render with
        // _outermost cleared; the node's OWN combine (which may be the final-transfer division) sees the value
        // that reached this node. §14.7.7 rule 3 NOTE 1 — ROUNDED/PROHIBITED bind to the final transfer only.
        bool outer = _outermost;
        _outermost = false;
        var l = n.Left.Accept(this);
        var r = n.Right.Accept(this);
        _outermost = outer;
        return CombineCore(l, n.Op.ToString(), r);
    }
    // ⛔ EVERY recursion clears _outermost, not only BoundBinary's (kb/Work PB129 — GR-8.8.1.2-1): a division
    // under a unary minus, an exponent base, or a computed-operand wrapper is NOT the final transfer, so it
    // must compute at the D2 guard scale with truncation — the receiver's ROUNDED mode binds to the final
    // transfer only (§14.7.4.3 GR3–GR10; §8.8.1.2 r1 "treated as a single operand"). The leak reinstated the
    // exact pre-CA5 defect for `(1 / 3) ** 0` under PROHIBITED, and made `(A / B) ** 2` differ from
    // `1 * (A / B) ** 2` — multiplying by one changed the value.
    public NumX Visit(BoundNegate n)
    {
        bool outer = _outermost;
        _outermost = false;
        var v = n.Operand.Accept(this);
        _outermost = outer;
        return Negate(v);
    }
    public NumX Visit(BoundPower n)
    {
        bool outer = _outermost;
        _outermost = false;
        var b = IntegerValuedLiteral(n.Base) ?? n.Base.Accept(this);
        var e = IntegerValuedLiteral(n.Exp) ?? n.Exp.Accept(this);
        _outermost = outer;
        return Power(b, e, NonNegativeIntegerLiteral(n.Exp));
    }

    /// <summary>A numeric LITERAL whose algebraic VALUE is an integer the native carrier can hold, as a scale-0
    /// operand — whatever notation it was written in. <c>null</c> for anything else.
    /// <para>⛔ THE EXACT-INTEGER <c>**</c> ARM IS KEYED ON THE VALUE, NEVER ON THE SPELLING (kb/Work PB272).
    /// <see cref="Power"/>'s exact arm asks <c>e.Scale == 0 &amp;&amp; !e.Dec &amp;&amp; !e.Real</c>, all three of
    /// which are properties of how the operand was WRITTEN: <c>10 ** 30</c> took the exact Int128 power and
    /// <c>10 ** 30.0</c> (scale 1) and <c>10 ** 3.0E+1</c> (a Dec since D-B, a Real before it) both dropped to the
    /// §8.8.1.2 binary64 approximation — the same expression, three notations, and the answers differed from the
    /// 17th significant digit up. §8.3.3.3.3 rule 5 makes a floating-point literal's value "the algebraic product
    /// of the value of its significand and the quantity derived by raising ten to the power of the exponent" and
    /// nothing more (cite.py-verified), and §8.3.1.2's fixed-point form is likewise a value — so trailing zeros
    /// past the point are notation, not precision, and 30, 30.0 and 3.0E+1 are ONE operand.</para>
    /// <para>Not applied under a standard arithmetic mode: there every operand is an SDIDI by §8.8.1.5.2 r1 and
    /// this arm does not exist. Not applied past <see cref="Int128"/>: the exact arm's carrier cannot hold it, and
    /// the Dec lane it would otherwise reach is the right home.</para></summary>
    private NumX? IntegerValuedLiteral(BoundExpr x)
    {
        if (StandardDecimal) return null;
        // `BoundNumLiteral` is the ONE literal shape an arithmetic expression's operands take — the operand-level
        // `BoundNumericLiteral` never reaches a `BoundPower`, which is why `NonNegativeIntegerLiteral` matches the
        // same single node.
        string? text = x is BoundNumLiteral l ? l.Text : null;
        if (text is null || !CobolNet.Common.NumericLiteral.TryParseExact(text, out Int128 sig, out int exp10))
            return null;
        while (exp10 < 0 && sig % 10 == 0) { sig /= 10; exp10++; }   // trailing zeros are notation, not scale
        if (exp10 < 0) return null;                                   // a genuinely non-integer value
        for (int i = 0; i < exp10; i++)
        {
            if (Int128.Abs(sig) > Int128.MaxValue / 10) return null;   // past the exact arm's own carrier
            sig *= 10;
        }
        return new NumX(EmitText.IntLiteral(sig.ToString(System.Globalization.CultureInfo.InvariantCulture)), 0);
    }

    /// <summary>Is this exponent a literal integer that is not negative? Read from the BOUND TREE, never from the
    /// rendered expression text — a first cut did <c>long.TryParse(e.Expr)</c> and silently stopped matching,
    /// because an operand does not render as its bare digits, which put every literal exponent back on the
    /// approximation arm and re-opened the defect the arm exists to close. See <see cref="Power"/>.</summary>
    /// <remarks>A <c>BoundNegate</c> wrapper is deliberately NOT unwrapped: it can only make the exponent
    /// negative, which is precisely the case that must stay on the approximation arm.</remarks>
    private static bool NonNegativeIntegerLiteral(BoundExpr e) =>
        e is BoundNumLiteral { Text: var t }
        && long.TryParse(t, System.Globalization.NumberStyles.AllowLeadingSign,
                         System.Globalization.CultureInfo.InvariantCulture, out long v)
        && v >= 0;
    public NumX Visit(BoundIntrinsicCall n) => Intrinsics.RenderNum(n);   // FUNCTION call (ISO §15)
    public NumX Visit(BoundExprError n) => new(EmitText.LoudValue("long", n.Feature), 0);

    // ── IBoundOperandVisitor<NumX> ───────────────────────────────────────────────────────────────────────────
    public NumX Visit(BoundNumericLiteral n) => LiteralNum(n.Text);   // the SAME rendering as the expression arm (D-B)
    public NumX Visit(BoundFieldOperand n) => FieldNum(n.Place);
    public NumX Visit(BoundComputedOperand n)
    {
        // An operand-wrapped expression is a nested operand, never the final transfer (PB129 — see the
        // BoundNegate note above).
        bool outer = _outermost;
        _outermost = false;
        var v = n.Expr.Accept(this);
        _outermost = outer;
        return v;
    }
    public NumX Visit(BoundFigurative n) => n.Kind == 'Z'
        ? EmitText.UnscaledLit("0")   // ZERO in a numeric context
        : new NumX(EmitText.LoudValue("long", $"figurative '{n.Kind}' in a numeric context"), 0);
    // An alphanumeric literal in a numeric context is an UNSIGNED integer (§14.9.25.3 Table 16 — the
    // alphanumeric→numeric move; NC105A's MOVE "12345" TO MOVE1), decoded exactly like an alphanumeric field. A
    // NATIONAL literal decodes the same way (§14.9.25.4 GR6d3 — national→numeric as an unsigned integer under the
    // Latin-1 identity); class BOOLEAN is not a numeric operand (§8.8.1) — loud.
    public NumX Visit(BoundStringLiteral n) => n.Category == PicCategory.Boolean
        ? new NumX(EmitText.LoudValue("long", "boolean literal in a numeric context (ISO §8.8.1 — class boolean is not a numeric operand)"), 0)
        : new NumX($"CobolNum.FromAlphanumeric({EmitText.CsLiteral(n.Value)})", 0);
    public NumX Visit(BoundOperandError n) => new(EmitText.LoudValue("long", n.Feature), 0);
    // BoundAllLiteral (ALL "x" in a numeric context) and BoundBoolOperand (a class-boolean operand) are not numeric
    // operands — the former loud `_ =>` default handled them; now explicit (byte-identical loud value; §8.8.1).
    public NumX Visit(BoundAllLiteral n) => new(EmitText.LoudValue("long", $"bound operand '{nameof(BoundAllLiteral)}'"), 0);
    public NumX Visit(BoundBoolOperand n) => new(EmitText.LoudValue("long", $"bound operand '{nameof(BoundBoolOperand)}'"), 0);

    /// <summary>The scaled value of a data item place (its unscaled <c>long</c> value + its scale). A float item is
    /// truncated to <c>long</c> for now (mixed float/fixed arithmetic is a later slice). A non-numeric place (a group
    /// or an alphanumeric item used in a numeric context) fails loud rather than crashing the compiler (§1.4).
    /// The instance entry adds ONLY the numeric-edited de-edit (it needs the SPECIAL-NAMES emission config);
    /// every other branch lives in the context-free <see cref="FieldNumCore"/> so the static string-channel
    /// intrinsic renderer reads fields through the SAME single implementation (singular-pattern rule).</summary>
    public NumX FieldNum(Place p) =>
        // A table(ALL) intrinsic argument (ISO §15.3; kb/Work PB62) renders as its ELEMENT — the index variable's
        // slot in the subscript — which is what its enumeration lambda wraps; the list is never a single value.
        p is TableAllPlace all ? FieldNum(all.Element) :
        p is not RefModPlace && !p.Item.StoreAsImage
            // A FLOATING-POINT numeric-edited sender de-edits to its EXACT value — a CobolDec on the Dec lane (D21/PB66:
            // a significand with its own power of ten; never a double); EC-DATA-INCOMPATIBLE for impossible content.
            && p.Item.Pic is { Category: PicCategory.NumericEdited, IsFloatEdited: true, EditMask: { } fem }
        ? new NumX($"CobolEdit.DeEditFloat({PlaceRenderer.Read(p)}, {EmitText.CsLiteral(fem)}{(ctx.Data.DecimalPointIsComma ? ", commaMode: true" : "")})", 0, Dec: true)
        : p is not RefModPlace && !p.Item.StoreAsImage
            // A format-2 (LOCALE) sender DE-EDITS through CobolLocaleEdit under the locale current NOW
            // (§13.18.40.5 r11; §14.6.13.2 r4 — impossible content is EC-DATA-INCOMPATIBLE); the scale is the
            // PICTURE's (PB64 T6). The second of the two dispatch sites PicInfo.LocaleEdit's doc names.
            && p.Item.Pic is { LocaleEdit: not null } lpic
        ? new NumX(RuntimeApi.LocaleDeEdit(lpic, PlaceRenderer.Read(p), p.Item.BlankWhenZero), lpic.Scale)
        : p is not RefModPlace && !p.Item.StoreAsImage
            // A numeric-edited sender DE-EDITS to its numeric value at the mask's scale (ISO §14.9.25.4 GR5 — the
            // COBOL-85 de-editing move; the runtime walks the image against the mask's digit positions).
            && p.Item.Pic is { Category: PicCategory.NumericEdited, EditMask: { } dem }
        ? new NumX($"CobolEdit.DeEdit({PlaceRenderer.Read(p)}, {EmitText.CsLiteral(dem)}{ctx.EditCfg(p.Item.Pic)}{RuntimeApi.EditsArg(p.Item.Pic!.EditingRules)}{(p.Item.BlankWhenZero ? ", blankWhenZero: true" : "")})",
            CobolNet.Runtime.CobolEdit.MaskScale(dem, '$', ctx.Data.DecimalPointIsComma))
        : FieldNumCore(p, floatCheck: !_floatSendingExempt);

    /// <summary>The context-free numeric read of a place (every branch of <see cref="FieldNum"/> except the
    /// numeric-edited de-edit, which stays loud here — it requires the instance emission config).
    /// <paramref name="floatCheck"/> (default on) wraps a float sending read in the checked
    /// <see cref="CobolNet.Runtime.CobolFloat.Sending(double)"/>; the exempt callers (sign condition, same-usage MOVE)
    /// pass <c>false</c>. The static string-channel intrinsic-arg reuse keeps the default (a non-exempt reference).</summary>
    internal static NumX FieldNumCore(Place p, bool floatCheck = true) => p is TableAllPlace all ? FieldNumCore(all.Element, floatCheck) : p is RefModPlace
        // A reference-modified result is ALPHANUMERIC (ISO §8.4.3.3.4 GR6) — in a numeric context it decodes as an
        // unsigned integer exactly like an alphanumeric field (§14.9.25.3 Table 16).
        ? new NumX($"CobolNum.FromAlphanumeric({PlaceRenderer.Read(p)})", 0)
        : p.Item.Pic switch
    {
        // A GROUP operand in a remaining numeric context (an arithmetic operand, a subscript…) decodes its
        // alphanumeric IMAGE as an UNSIGNED integer (an alphanumeric group item "has class and category
        // alphanumeric", §8.5.2.1 — NOT the phantom §8.8.4.1.1, kb/Work PB182; the
        // deterministic digit decode §14.6.13.2 permits for incompatible content). NOTE: a MOVE never reaches
        // this branch — a group SENDER makes the move a GROUP move (§14.9.25.4 GR4: no conversion; classified
        // at EmitMove → EmitGroupToElementaryMove), never a numeric decode of the image (the pre-fix NC105A
        // MOVE MOVE43 TO MOVE3 mis-derivation). A mixed-usage (COMP-leaf) group is an ORDINARY image-capable
        // group now — its leaves' pinned bytes ARE the image (the Step D arm-1 dissolution).
        // ⛔ IsImageCapable, and correct ONLY because strict conformance now REJECTS a group arithmetic operand
        // (DA6 — §8.8.1.1, COBOLNET0844 in ExpressionBinder). Reaching here therefore means --permissive was
        // requested, and the leniency must be CONSISTENT: before, a group of PIC X leaves computed while a group of
        // PIC 9 leaves threw at run time, so the operand whose digits were unambiguous failed and the merely-textual
        // one succeeded. Migrating this arm while strict still ACCEPTED the construct would have extended acceptance
        // of illegal source instead of fixing anything — the two changes are correct only together. A
        // group with a pointer/object-class leaf (or a variable-length group) still has no image and stays
        // loud even under --permissive (kb/Work PB164 + R40 — an INDEX-leaf group images now).
        // ⛔ THROUGH THE ONE SENDING READER, never a self-spelled `.AsImage()` (kb/Work PB178, the sibling of
        // OperandText.AsStorageImage's defect and found in the same sweep). This site used to inline
        // `p is RedefViewPlace ? Read(p) : Read(p).AsImage()`, which had TWO independent holes:
        //   (a) NO OdoGroupPlace unwrap — `Read(OdoGroupPlace o) => Read(o.Inner)`, so an ODO wrapper over a
        //       Tier-B / BASED string-canonical window (what `ReferenceResolver`'s
        //       `WrapIfOdoGroup(new RedefViewPlace(...))` builds for a BASED record carrying an ODO table)
        //       rendered `<string window>.AsImage()` → backend CS1061. MEASURED, not deduced: strict
        //       conformance rejects a group arithmetic operand (COBOLNET0844 below), so the reach is
        //       `--permissive`, which is a SUPPORTED mode — `COMPUTE X = <BASED ODO group> + 1 --permissive`
        //       was the probe.
        //   (b) the MAXIMUM image where §13.18.38.4 GR8 wants the CURRENT-count part. A numeric decode of a
        //       group's image is a SENDING reference of that group, and GR8a/GR8b both give a sending operand
        //       the current extent — which its sibling `OperandText.FieldAsString` already got right. A
        //       wrong ANSWER, independent of (a)'s crash.
        // The capability guard now lives in the ONE reader too (it stages the same Tier-C loud), so the
        // former `when p.Item.IsImageCapable` arm and its hand-written twin below collapse into this one.
        null => new NumX($"CobolNum.FromAlphanumeric({PlaceRenderer.SendingGroupImage(p, "numeric use of group item")})", 0),
        // A float leaf (COMP-1/COMP-2/FLOAT-SHORT/-LONG/-EXTENDED, D16) enters the arithmetic pipeline as a native
        // IEEE double — NOT truncated to (long) at scale 0 (the pre-D16 stub that silently dropped the fraction). The
        // sending read is wrapped in CobolFloat.Sending (raises EC-DATA-NOT-FINITE for NaN/±Inf under checking, §14.6.13.2
        // item 3) UNLESS this is an exempt context (sign condition / same-usage MOVE — floatCheck false = raw read).
        // ⛔ THE WINDOWED-FLOAT ARM PRECEDES THE NATIVE FLOAT ARM (the Step D arm-1 dissolution — the design
        // scout's pre-loaded CS0030: with the order reversed, a Tier-B float view rendered
        // `(double)(<string window>)`). The window's IEEE bytes decode through the distinctly-named lane.
        { IsFloat: true } when p.Item.StoreAsImage => new NumX(
            floatCheck
                ? RuntimeApi.FloatSending(RuntimeApi.NumParseImageFloat(PlaceRenderer.Read(p), p.Item.ProfileName))
                : RuntimeApi.NumParseImageFloat(PlaceRenderer.Read(p), p.Item.ProfileName),
            0, Real: true),
        { IsFloat: true } => new NumX(
            floatCheck ? RuntimeApi.FloatSending($"(double)({PlaceRenderer.Read(p)})") : $"(double)({PlaceRenderer.Read(p)})",
            0, Real: true),
        // The windowed 16-byte UNSIGNED arm precedes the signed decode (the scout's silent-wrong-answer: a
        // wide unsigned COMP-5 window decoded SIGNED above Int128.MaxValue's bit pattern) — ParseImageU128
        // reinterprets bit-identically into the UInt128 lane, which keeps the item's full [0, 2^128) container
        // range (§13.18.60.4 GR12).
        // ⛔ THERE IS NO 8-BYTE (IsUnsignedLongBinary) TWIN HERE, and adding one back would be duplication:
        // ParseBinaryImage already returns an unsigned item's FULL width as a non-negative Int128, and every
        // ulong value fits Int128 — so the generic StoreAsImage arm below decodes a `PIC 9(10..18) COMP-5`
        // window identically. Only the 16-byte range genuinely exceeds the signed carrier.
        { IsUnsignedWideBinary: true } pic when p.Item.StoreAsImage =>
            new NumX(RuntimeApi.NumParseImageU128(PlaceRenderer.Read(p), p.Item.ProfileName), pic.Scale, U: true),
        // A numeric leaf stored as its character image (whole-group-aliased / Tier-B): decode the STORED BYTES to
        // its unscaled value for numeric use — zoned digits for DISPLAY, radix-2 / BCD for BINARY / PACKED (V59;
        // ISO §14.6.13.2 — incompatible content decodes deterministically).
        { } pic when p.Item.StoreAsImage =>
            new NumX($"CobolNum.ParseImage({PlaceRenderer.Read(p)}, {p.Item.ProfileName})", pic.Scale),
        // An alphanumeric operand in a numeric context is an UNSIGNED integer (ISO §14.9.25.4 GR6) — never the raw
        // string read (which would emit uncompilable C#, the bind-success ⇒ compilable invariant). A NATIONAL
        // operand decodes identically (GR6d3 — its digit characters are the Latin-1 digits under D-N4);
        // class BOOLEAN is not a numeric operand (§8.8.1; Table 16 Boolean→Numeric = No) — loud.
        { Category: PicCategory.Alphanumeric or PicCategory.National } =>
            new NumX($"CobolNum.FromAlphanumeric({PlaceRenderer.Read(p)})", 0),
        { Category: PicCategory.Boolean } =>
            new NumX(EmitText.LoudValue("long", $"boolean operand '{p.Item.CobolName}' in a numeric context (ISO §8.8.1 — class boolean is not a numeric operand)"), 0),
        // The numeric-edited de-edit lives on the INSTANCE entry (it needs the SPECIAL-NAMES config); a static
        // caller (the string-channel intrinsic renderer) reaching one is a staged-out shape — loud (§1.4).
        { Category: PicCategory.NumericEdited } =>
            new NumX(EmitText.LoudValue("long", $"numeric-edited operand '{p.Item.CobolName}' in a context-free numeric read"), 0),
        // A 16-byte UNSIGNED BinaryCapacity item (UInt128 carrier — kb/Work R10): the read enters the renderer
        // on the unsigned-wide lane. Value paths (store/display/relation) keep the full [0, 2^128) range via the
        // runtime's UInt128 overloads; arithmetic funnels through CobolNum.Widen (loud beyond the Int128
        // intermediate), never a silent wrap.
        { IsUnsignedWideBinary: true } pic => new NumX(PlaceRenderer.Read(p), pic.Scale, U: true),
        // An 8-byte UNSIGNED BinaryCapacity item (ulong carrier): every ulong value fits the Int128 engine — the
        // read is lifted at THIS one site so downstream text (comparisons against long literals, raw-expr
        // alignment) never mixes ulong with long, which C# rejects as ambiguous.
        { IsUnsignedLongBinary: true } pic => new NumX($"(Int128)({PlaceRenderer.Read(p)})", pic.Scale),
        { } pic => new NumX(PlaceRenderer.Read(p), pic.Scale),
    };

    /// <summary>Left-fold a list of bound expressions with <c>+</c> (the addends of an ADD / minuends of a SUBTRACT).
    /// <para>⛔ SAVES AND RESTORES the ambient receiver, exactly as <see cref="Render"/> and <see cref="AsNum"/> do.
    /// It did not, and that was a real defect rather than a tidiness point: <c>_rcv</c> is a per-unit MUTABLE field,
    /// so an ADD left its receiver LATCHED on the renderer and the next receiver-less render — a numeric FUNCTION in
    /// a DISPLAY or a text MOVE — silently inherited it. `DISPLAY FUNCTION SQRT(2)` printed `1.414213562`, then
    /// `1.414213562373` after an unrelated `ADD 1 TO R`, because the intrinsic's working scale is
    /// <c>max(Receiver.Scale, 9)</c>. A public entry that mutates ambient state must restore it or the
    /// next caller reads someone else's context.</para></summary>
    public NumX Fold(IReadOnlyList<BoundExpr> xs, in ReceiverContext rcv)
    {
        var saved = _rcv; bool savedOut = _outermost;
        _rcv = rcv;
        _outermost = false;   // an ADD/SUBTRACT operand is never a final-transfer division (its result feeds the fold)
        try
        {
            if (xs.Count == 0) return new NumX("0L", 0);
            NumX acc = xs[0].Accept(this);
            for (int i = 1; i < xs.Count; i++) acc = CombineCore(acc, "+", xs[i].Accept(this));
            return acc;
        }
        finally { _rcv = saved; _outermost = savedOut; }
    }

    /// <summary>Combine two scaled values with a COBOL operator, tracking the result scale (ISO §8.8.1). EVERY
    /// operation runs in <see cref="Int128"/> — the carrier (COBOLNET_DESIGN §18 #4 / numeric design D1): a product
    /// of two 18-digit operands is 36 digits and an aligned sum 19+, both past the long range MID-computation even
    /// when the final receiver fits. The leading <c>(Int128)</c> cast forces wide arithmetic whatever the leaf
    /// types; storage stays narrow (the store path truncates/rounds once, at the receiver).</summary>
    public NumX Combine(NumX a, string op, NumX b, in ReceiverContext rcv, bool outermost = false)
    {
        // Saves/restores the ambient receiver for the same reason Fold does — see its remark.
        var saved = _rcv; bool savedOut = _outermost;
        _rcv = rcv;
        _outermost = outermost;
        try { return CombineCore(a, op, b); }
        finally { _rcv = saved; _outermost = savedOut; }
    }

    private NumX CombineCore(NumX a, string op, NumX b)
    {
        // The unsigned-wide funnel (kb/Work R10): an ARITHMETIC operand enters the engine's documented Int128
        // intermediate through CobolNum.Widen — a value beyond it raises the size-error condition (ON SIZE ERROR
        // catches it; without the phrase it is loud), never a silent two's-complement wrap. The VALUE paths
        // (store/display/relation) never come through here and keep the full [0, 2^128) range.
        a = DeU(a);
        b = DeU(b);
        // STANDARD / STANDARD-DECIMAL arithmetic (§8.8.1.5): every operation of an arithmetic expression
        // evaluates in SDIDI form (decimal128 semantics), rounded per-op to 34 significant digits with the
        // INTERMEDIATE ROUNDING mode (§11.9.11) and range-checked at the decimal128 bounds (§8.8.1.5.2 r2);
        // the receiver's ROUNDED applies only at the final transfer (§14.7 NOTE 1). This branch runs BEFORE
        // the D16 float branch: under a standard mode a FLOATING-POINT operand converts into SDIDI form via
        // the §8.8.1.5.1 implementor-defined conversion (CobolDec.FromDouble — the shortest-round-trip decimal
        // identity of the IEEE value; see DecOperand) and the operation itself is the SDIDI one.
        // ⛔ AND A NATIVE OPERATION WITH AN SDIDI-CARRIED OPERAND EVALUATES ON THE SDIDI TOO (kb/Work PB69). The
        // Dec producers under NATIVE arithmetic are THREE, not one (kb/Work PB273 — this sentence named only the
        // first for two waves, and D-B made the third the common case):
        //   · an integer power (PowNativeIntDec — exact while it fits the carrier, the owner-decided double
        //     approximation past it, the reciprocal for a negative exponent);
        //   · a floating-point numeric-EDITED sender, lifted onto the Dec lane by FieldNum in every mode;
        //   · a FLOATING-POINT LITERAL operand (LiteralNum — since PB99 for an operand, and since owner decision
        //     D-B for every position), which is now much the most common of the three.
        // The reason is the same for all three and is why the branch tests the CARRIER: a Dec has no compile-time
        // scale, and landing it into Int128 at the operation's static scale truncated `2 ** -2 + 1` to 1 (the
        // additive arm) and did not compile at all for `*` and `/` (a raw Dec into an Int128 slot — CS1503). The
        // SDIDI carries the exact power, the approximation and the literal's exact value alike, so the operation
        // computes there and the result lands ONCE at the receiver (TryStore(CobolDec), checked). The float lane
        // keeps precedence when a float operand or a float receiver is present (native float arithmetic is IEEE,
        // D16).
        // ⚠ COST, AND IT IS A PUBLISHED DETERMINATION (§8.8.1.3 leaves "the method of evaluating an arithmetic
        // statement" to the implementor; CONFORMANCE.md A.1 item 82): this lane carries 34 significant digits
        // where CombineNative's Int128 carries ~38, so an expression whose exact intermediate needs 35–38 digits
        // rounds here and not there. That is observable — `A * B + 0.0E+0` differs from `A * B + 0.0` for
        // 18-digit A and B — so it is NOT covered by "a literal's notation never changes the arithmetic"; the
        // literal's VALUE is exact either way, and the LANE is the determination. It also means INTERMEDIATE
        // ROUNDING (§11.9.11) reaches a native statement carrying a float literal, PROHIBITED included.
        // MOD/REM keep exact integers exact through their own integer fast path.
        if (StandardDecimal || ((a.Dec || b.Dec) && !a.Real && !b.Real && !_rcv.Real))
            return op switch
            {
                "+" => new NumX($"CobolDec.Add({DecOperand(a)}, {DecOperand(b)}, {IntermediateMode})", 0, Dec: true),
                "-" => new NumX($"CobolDec.Sub({DecOperand(a)}, {DecOperand(b)}, {IntermediateMode})", 0, Dec: true),
                "*" => new NumX($"CobolDec.Mul({DecOperand(a)}, {DecOperand(b)}, {IntermediateMode})", 0, Dec: true),
                "/" => new NumX($"CobolDec.Div({DecOperand(a)}, {DecOperand(b)}, {IntermediateMode})", 0, Dec: true),
                _ => a,
            };
        // D16 (NATIVE arithmetic): any expression with ≥1 float operand evaluates ENTIRELY in IEEE binary64 (a
        // single-precision operand widens exactly) — native COBOL float arithmetic is IEEE binary, never decimal
        // (§8.8.1.3 implementor-defined; STANDARD-BINARY is obsolete, 2023 §8.8.1.4.1 NOTE). +,-,*,/ are native
        // double ops.
        if (a.Real || b.Real || _rcv.Real) return new NumX($"({Real(a)} {op} {Real(b)})", 0, Real: true);
        return CombineNative(a, op, b);
    }

    // Plain STANDARD arithmetic (2002; obsolete 2014, removed 2023 — Annex E.2 item 21) uses the standard
    // intermediate data item, which for these operands IS the standard DECIMAL form — STANDARD routes to the
    // same CobolDec engine as STANDARD-DECIMAL (DataBinder.BindDeclarations documents the bind-side rationale).
    // ⛔ The SET lives in ONE place — ArithmeticModes.IsDecimalEngine (kb/Work PB194): two other sites had written
    // it out again and both had dropped ARITHMETIC IS STANDARD from it.
    internal bool StandardDecimal => ArithmeticModes.IsDecimalEngine(ctx.Data.Options.Arithmetic);

    internal string IntermediateMode => $"CobolRounding.{ctx.Data.Options.IntermediateRounding}";

    /// <summary>Render an operand in SDIDI form: an already-decimal intermediate passes through; a fixed-point
    /// value lifts EXACTLY via <c>CobolDec.From</c> (≤31 digits always representable, §8.8.1.5.2); a FLOAT
    /// (Real) operand converts via <c>CobolDec.FromDouble</c> — the §8.8.1.5.1 implementor-defined float→SDIDI
    /// conversion (the shortest round-trip decimal, ≤17 digits, always exact in the 34-digit significand).</summary>
    // An unsigned-wide operand funnels through DeU first: the SDIDI's 34-digit significand cannot hold a
    // 39-digit value either (§8.8.1.5.2 r2 would range-check it out), so the failure is the size-error condition.
    public string DecOperand(NumX x) => DeU(x) switch
    {
        { Dec: true } d => d.Expr,
        { Real: true } r => RuntimeApi.DecFromDouble(r.Expr),
        var v => $"CobolDec.From({v.Expr}, {v.Scale})",
    };

    private NumX CombineNative(NumX a, string op, NumX b) => op switch
    {
        "+" or "-" => CombineAdditive(a, op, b),
        // Multiplication: scales add (exact). Under an ON SIZE ERROR phrase the product is overflow-checked at the
        // Int128 ESCAPE boundary (~38 digits, design D1) → OverflowException maps to the size error condition
        // (§14.7.5 case 5); without the phrase it is unchecked wide multiplication.
        "*" => new NumX(Checked ? $"CobolNum.MulChecked({a.Expr}, {b.Expr})" : $"((Int128)({a.Expr}) * ({b.Expr}))", a.Scale + b.Scale),
        "/" => Divide(a, b),
        _ => a,
    };

    /// <summary>Guard digits past the deepest receiver/operand scale for a division NESTED inside a larger
    /// expression (numeric design D2): rounding happens ONCE, at the receiver, so the nested quotient must carry
    /// enough fraction headroom for the operations above it. 14 reproduces the legacy decimal accumulator's
    /// ~28-significant-digit behavior the golden corpus encodes.</summary>
    private const int DivGuardDigits = 14;

    /// <summary>Division quotient (ISO §8.8.1 / §14.7.4). An OUTERMOST division (<see cref="_outermost"/> — the
    /// quotient IS the value transferred to the single resultant identifier) is computed DIRECTLY at the receiver
    /// scale and rounded with the receiver's mode in ONE exact step (<c>CobolNum.Divide</c> → <c>RoundDiv</c> uses
    /// the true integer remainder, so no guard digits are needed; ROUNDED/PROHIBITED apply here, the final transfer,
    /// §14.7.7 rule 3 NOTE 1 / §14.7.4.3 GR7). A division NESTED inside a larger expression is NOT the final
    /// transfer — it ALWAYS computes at the D2 guard scale with TRUNCATION (never the receiver's mode), clamped so
    /// the Int128 radix alignment (dividend digits ≤ 18 + the alignment exponent) cannot exceed the wide engine's
    /// 38 digits — and the single receiver store performs the one rounding. The intermediate's precision is
    /// implementor-defined (§8.8.1.3); carrying full guard precision keeps it accurate and is size-error-free
    /// (§14.7.5 enumerates no native intermediate-inexactness case; only a zero divisor, case 2, still raises).</summary>
    private NumX Divide(NumX a, NumX b)
    {
        int ds;
        CobolRounding mode;
        if (_outermost)
        {
            // The final transfer: compute at the resultant's scale + ROUNDED mode. DivideOrThrow detects a
            // PROHIBITED-inexact quotient via the exact integer remainder (§14.7.4.3 GR7 — tests the resultant).
            ds = _rcv.Scale;
            mode = _rcv.Rounding;
        }
        else
        {
            // Nested intermediate: full guard precision + truncation, clamped to the wide engine's alignment
            // headroom (exponent = b.Scale + ds − a.Scale must keep dividend-digits + exponent ≤ 38; 18-digit
            // operands ⇒ exponent ≤ 20). A nested quotient never inherits the receiver's mode — rounding happens
            // once, at the receiver store (§14.7 NOTE 1).
            int baseScale = Math.Max(_rcv.Scale, Math.Max(a.Scale, b.Scale));
            int maxExp = 20;
            int guard = Math.Min(DivGuardDigits, maxExp - (b.Scale + baseScale - a.Scale));
            ds = baseScale + Math.Max(0, guard);
            mode = CobolRounding.Truncation;
        }
        // Under an ON SIZE ERROR phrase, a zero divisor must raise the size error (ISO §14.7.5 case 2): the checked
        // DivideOrThrow signals it (caught by the statement's try); otherwise Divide returns 0 unchanged.
        string fn = Checked ? "DivideOrThrow" : "Divide";
        return new NumX($"CobolNum.{fn}({a.Expr}, {a.Scale}, {b.Expr}, {b.Scale}, {ds}, CobolRounding.{mode})", ds);
    }

    private NumX CombineAdditive(NumX a, string op, NumX b)
    {
        int s = Math.Max(a.Scale, b.Scale);
        // Under an ON SIZE ERROR phrase the sum/difference is overflow-checked at the Int128 ENGINE boundary
        // (AddChecked/SubChecked → OverflowException → the size error condition, §14.7.5 case 5) — the exact
        // MulChecked contract. Reachable: HIGHEST-ALGEBRAIC of PIC S9(19) COMP-5 is Int128.MaxValue itself
        // (kb/Work R10), where an unchecked `+ 1` wraps to the container's far end and stores with no error.
        // Without the phrase it stays the bare unchecked operator, like every other engine op.
        if (Checked)
            return new NumX($"CobolNum.{(op == "+" ? "AddChecked" : "SubChecked")}({Align(a, s)}, {Align(b, s)})", s);
        return new NumX($"((Int128)({Align(a, s)}) {op} ({Align(b, s)}))", s);
    }

    /// <summary>Rescale a value's unscaled long up to <paramref name="toScale"/> (widening only here → exact).
    /// <para>⛔ TOTAL OVER THE CARRIER KINDS, and it has to be: a <c>Real</c> (binary64) operand reaches the
    /// receiver-less sites — a subscript, a SET amount, a PERFORM VARYING FROM/BY, a report VARYING, a RETRY
    /// count — and without this arm the double expression was handed straight to a caller expecting a scaled
    /// integral, so legal source produced uncompilable C# (the PB2 shape). It was already reachable through a
    /// COMP-2 operand; PB13 widened it by keeping a receiver-less FLOAT-FAMILY result in binary64 too, so the
    /// arm is landed at the ONE choke point rather than at each of the forty-odd call sites
    /// (feedback_change_the_dispatch_not_the_callers). A float lands through the same
    /// <c>CobolFloat.ToScaled</c> every other float→fixed transfer uses — the saturation-SAFE one, because it
    /// lands AT the requested scale, so an out-of-range magnitude stays above the caller's capacity check
    /// instead of being rescaled back into range. TRUNCATION matches this helper's existing contract (alignment
    /// is not a ROUNDED transfer; §14.7 NOTE 1 gives ROUNDED only to the final transfer).</para></summary>
    /// <para>⛔ AND THE <c>Dec</c> ARM IS THE ONE IT WAS MISSING (fix-queue PB32/PB14). The paragraph above
    /// declares this helper TOTAL over the carrier kinds; <see cref="NumX"/> has THREE — exact scaled
    /// <see cref="Int128"/>, the <c>CobolDec</c> SDIDI, and binary64 — and only two were written down. Under
    /// <c>ARITHMETIC IS STANDARD-DECIMAL</c> every arithmetic expression becomes a <c>Dec</c> carrier
    /// (<see cref="CombineCore"/>, <see cref="Power"/>), so a §15.3 type-10 arithmetic-expression argument — legal
    /// at 2014 and 2023 — reached <c>MaxScaled(params Int128[])</c> as a raw <c>CobolDec</c>, which has no
    /// conversion operator, and the user saw a raw Roslyn error on conforming COBOL:
    /// <c>COMPUTE R = FUNCTION MAX(A + B, B)</c> ⇒ <c>error CS1503: cannot convert from
    /// 'CobolNet.Runtime.CobolDec' to 'System.Int128'</c>. That is the PB2 shape on the Dec axis, and it is why
    /// this arm is placed BEFORE the <c>toScale == x.Scale</c> test rather than after: a <c>Dec</c> operand
    /// carries <c>Scale 0</c> by convention, so a scale-0 alignment would otherwise pass the <c>CobolDec</c>
    /// expression through untouched and reproduce the same failure.
    /// <para>⚠ THE LANDING IS EXACT ONLY TO <paramref name="toScale"/>. An SDIDI carries its exponent at RUN
    /// time, so there is no compile-time scale to preserve and the value lands at the argument list's common
    /// scale; a quotient with more fraction digits than that is truncated before the function sees it. That is a
    /// strictly smaller wrong than "does not compile" and it is not the end state — the §15.4.1 r1 answer is a
    /// Dec-carrier body, ledgered as PB38.</para></para></summary>
    public static string Align(NumX x, int toScale) =>
        // CHECKED: an intermediate consumer has no capacity check downstream, so a value past the carrier stays the
        // loud sentinel here — never the low-order digits a STORE may take (kb/Work PB77; the Dec arm below raises).
        x.Real ? RuntimeApi.FloatToScaled(x.Expr, $"{toScale}", CobolRounding.Truncation, checkedLanding: true)
        // A Dec that the Int128 carrier cannot hold at this scale is a SIZE ERROR condition (EC-SIZE-OVERFLOW —
        // §14.7.5 case 5, A.1 item 179 "checked"; kb/Work PB69), never the low-order-digits landing a STORE may
        // use: an intermediate consumer has no capacity check downstream to catch a truncated value.
        : x.Dec ? RuntimeApi.DecToUnscaledIntermediate(x.Expr, $"{toScale}", CobolRounding.Truncation)
        // Unsigned-wide (kb/Work R10): the receiver-less integral sites this helper feeds (a subscript, a SET
        // amount, PERFORM VARYING, RETRY, exit status) are Int128-lane consumers — the Widen funnel applies
        // (loud beyond the intermediate; a 39-digit subscript is not a computable position).
        : x.U ? Align(DeU(x), toScale)
        : toScale == x.Scale ? x.Expr
        // ⛔ ESCAPE-CHECKED (fix-queue PB65): Align's consumers are VALUE-semantics sites — intrinsic argument
        // alignment, comparisons, subscript/status intake — where a silent Int128 wrap on widening handed MIN a
        // negative result over two positive arguments. RescaleEscape raises the size-error condition at the D1
        // escape boundary instead; the arithmetic store path keeps its documented wrap (item 179) and does not
        // come through here.
        : RuntimeApi.NumRescaleEscape(x.Expr, $"{x.Scale}", $"{toScale}", CobolRounding.Truncation);

    /// <summary>Land an arithmetic-expression at scale 0 ROUNDED UP — the sibling of <see cref="Align"/> for the
    /// clauses that say "rounded up to the next whole number" rather than taking the value-semantics truncation.
    /// <para>⛔ THIS IS THE ONE PLACE THAT RULE IS WRITTEN, and its population is CLOSED, not sampled: a grep for
    /// "rounded up" over the whole standard returns exactly TWO normative sites — §14.7.9.3 GR1 (the RETRY
    /// phrase's arithmetic-expression-1, an n-TIMES count) and §14.9.3.4 GR1 (ALLOCATE's arithmetic-expression-1,
    /// a byte count). Both call here. Every other <c>Align(…, 0)</c> consumer was checked against its own clause
    /// and none of them says "rounded up" (EVALUATE / GO TO DEPENDING selectors, PERFORM VARYING and PERFORM n
    /// TIMES, relative RRN / START WITH LENGTH / key fields, SET amounts, boolean shift counts, report-writer
    /// counts, WRITE ADVANCING lines, and STOP RUN's exit status, whose §14.9.42 SR3 explicitly wants the integer
    /// truncation). A new site governed by a round-up clause belongs here, never in a second local ceiling —
    /// <c>NumericRoundUpSiteTests</c> is the drift test that keeps that true.</para>
    /// <para>The mode is <see cref="CobolRounding.TowardGreater"/>, which IS "rounded up to the next whole
    /// number" — round toward positive infinity — on every lane (<c>CobolFloat</c> uses <c>Math.Ceiling</c>,
    /// <c>CobolNum</c>/<c>CobolDec</c> add one only when the value is positive). It is deliberately NOT
    /// <c>AwayFromZero</c>, which agrees with it on positives but rounds −1.5 to −2 where the rule requires −1.
    /// Both clauses guard their own negatives — §14.9.3.4 GR2 shunts ≤ 0 to NULL and §14.7.9.3 GR4a shunts a
    /// negative or zero expression to the unsuccessful path before the rounded value is used — so the choice is
    /// behaviour-neutral at both of today's sites and simply correct at the next one.</para>
    /// <para>The lane structure mirrors <see cref="Align"/> exactly, including its checked landings: an
    /// intermediate consumer has no capacity check downstream, so a value past the carrier stays the loud
    /// sentinel here rather than becoming the low-order digits a STORE may take (kb/Work PB77/PB69/PB65).</para>
    /// <para>NOTE — ALLOCATE's NATIVE-FLOAT lane does NOT come through here, and that is not an oversight:
    /// <c>CobolPtr.AllocateReal</c> fuses GR1's <c>Math.Ceiling</c> with GR2's ≤ 0 ⇒ NULL and with the
    /// storage-not-available outcomes that only the ALLOCATE statement defines, so a NaN or an over-wide request
    /// answers "not available" instead of raising (kb/Work PB151). Routing it through a checked emitter-side
    /// landing would trade that defined outcome for a size-error condition. The ROUNDING RULE is still written
    /// once — here — and AllocateReal's ceiling is that rule realised inside the statement's own outcome
    /// machinery; the drift test knows about the exemption by name.</para></summary>
    public static string AlignRoundedUp(NumX x) =>
        x.Real ? RuntimeApi.FloatToScaled(x.Expr, "0", CobolRounding.TowardGreater, checkedLanding: true)
        : x.Dec ? RuntimeApi.DecToUnscaledIntermediate(x.Expr, "0", CobolRounding.TowardGreater)
        : x.U ? AlignRoundedUp(DeU(x))
        : x.Scale == 0 ? x.Expr
        : RuntimeApi.NumRescaleEscape(x.Expr, $"{x.Scale}", "0", CobolRounding.TowardGreater);

    /// <summary>Render a STOP RUN / GOBACK termination-status phrase to a C# <c>long</c> exit-status expression
    /// (ISO §14.9.42.4 GR5 / §14.9.18.4 GR10): the status VALUE truncated to an integer at scale 0 when present
    /// (SR3 — an integer is passed to the OS), else the implementor error/normal indication ERROR ⇒ 1 / NORMAL ⇒ 0
    /// (§14.9.42.4 GR2/GR3; docs/CONFORMANCE.md §4.2.16). The value renders receiver-less (scale 0) exactly as a
    /// boolean-shift count or an intrinsic integer argument does.
    /// <para>⛔ THE VALUE IS AN OPERAND, NOT AN EXPRESSION (kb/Work PB169) — §14.9.42.2 writes the slot
    /// <c>{identifier-1 | literal-1}</c>. The NUMERIC arm is byte-identical to the former
    /// <c>Render(BoundExpr)</c> text by construction: <c>Visit(BoundNumericLiteral)</c> and
    /// <c>Visit(BoundNumLiteral)</c> both reduce to <c>EmitText.UnscaledLit</c> for an integer literal (SR3
    /// admits no other), and <c>Visit(BoundFieldOperand)</c> and <c>Visit(BoundNumRef)</c> are the same
    /// <c>FieldNum(Place)</c> call.</para></summary>
    public string ExitStatus(TerminationStatus st) =>
        st.Value is { } v ? $"(long)({Align(StatusNum(v), 0)})" : st.Error ? "1L" : "0L";

    /// <summary>The GR5 numeric interpretation of a status operand. A NUMERIC operand (an integer literal-1, a
    /// numeric identifier-1) renders through the ordinary numeric channel; a NON-NUMERIC literal-1 of ANY form
    /// is interpreted through its CHARACTERS — docs/CONFORMANCE.md item 192's published determination, "its
    /// digit characters decode as an unsigned integer, a non-digit position contributing no digit"
    /// (<c>CobolNum.FromAlphanumeric</c>), the same decode <see cref="FieldNumCore"/> gives an alphanumeric or
    /// national identifier-1.
    /// <para>⛔ ONE ARM FOR EVERY NON-NUMERIC LITERAL SHAPE, THROUGH THE ONE CHARACTER-IMAGE READER (kb/Work
    /// PB216). The first cut read only <see cref="AsNum"/>, whose figurative / ALL-literal / boolean arms are
    /// LOUD — correctly so, because they answer the §8.8.1.1 ARITHMETIC question, and the status slot is not an
    /// arithmetic position. The consequence was that <c>STOP RUN WITH ERROR STATUS SPACE</c>,
    /// <c>… STATUS ALL "5"</c>, <c>… STATUS B"01"</c> and <c>… STATUS B"1" &amp; B"0"</c> — all conforming
    /// (§8.3.3.6.3 SR1 admits a figurative wherever 'literal' appears, and §14.9.42.3 SR3's conditional "if
    /// literal-1 is numeric" is the proof the slot is not restricted to a numeric literal) — compiled clean and
    /// died with <c>NotImplementedCobolFeatureException</c>. <see cref="OperandText.AsString"/> is THE ONE
    /// operand→character-image reader and already carries §8.3.3.6.4 GR3 b (a non-ALL figurative is ONE
    /// character — GR3's NOTE 2 names the STOP statement by name) and GR3 c (an <c>ALL literal-1</c> is
    /// literal-1 once), PCS-aware for HIGH-/LOW-VALUE. Routing the whole literal family through it is what makes
    /// the NEXT literal form automatic; <c>NumericRendererStatusOperandTests</c> holds that true.</para></summary>
    private NumX StatusNum(BoundOperand v) =>
        v is BoundStringLiteral or BoundFigurative or BoundAllLiteral
            ? new NumX(RuntimeApi.NumFromAlphanumeric(OperandText.AsString(v, this)), 0)
            : AsNum(v, ReceiverContext.None);

    /// <summary>Exponentiation (ISO §8.8.1.2: a native-arithmetic exponentiation whose result has no exact
    /// representation is an IMPLEMENTOR-DEFINED approximation): computed in double, quantized through the ONE
    /// <c>CobolIntrinsics.FromDouble</c> (rounding) at <c>max(Receiver.Scale, 9)</c> fraction digits. The previous
    /// scale-0 <c>(long)</c> truncation lost every fractional power result and turned the double artifact in
    /// <c>SQRT(10) ** 2</c> = 9.999999988 into 9 (IF136A F-SQRT-25); the 9-digit floor mirrors the float-intrinsic
    /// working scale (a receiver-less context renders at scale 0 — the P7.3 <see cref="ReceiverContext.None"/>).</summary>
    private NumX Power(NumX b, NumX e, bool expIsNonNegativeLiteral = false)
    {
        b = DeU(b);   // exponentiation is arithmetic — the unsigned-wide Widen funnel applies (kb/Work R10)
        e = DeU(e);
        // STANDARD / STANDARD-DECIMAL: exponentiation follows §8.8.1.5.4 — an integer exponent evaluates by
        // repeated SDIDI multiplication (r2a–r2d exactly; r2e's implementor-defined form for larger integers,
        // every step per §8.8.1.5.3), r3's reciprocal for a negative exponent, and the EC-SIZE-EXPONENTIATION
        // legs of §8.8.1.2 r6 / §8.8.1.5.4 r4; a float operand converts in per §8.8.1.5.1 (DecOperand). The ONE
        // runtime implementation is CobolDec.Pow.
        if (StandardDecimal)
            return new NumX(RuntimeApi.DecPow(DecOperand(b), DecOperand(e), IntermediateMode), 0, Dec: true);
        // ⛔ AN INTEGER BASE RAISED TO AN INTEGER EXPONENT IS EXACT, AND THE RECEIVER PLAYS NO ROLE AT ALL
        // (owner decision 2026-08-03; fix-queue PB18 + PB32 + PB65/RV-15.64.4-1). Three things turn on this arm:
        //   · `COMPUTE R = 10 ** 30` returned 1000000000000000071935427891953 where Int128 holds 10^30 exactly —
        //     the native technique contradicting our OWN documented one (numeric design D3).
        //   · It is the ROOT of PB32's receiver-shape defect: `A ** 2` was binary64 under DISPLAY / an IF subject
        //     and exact under COMPUTE, routing FUNCTION MOD to a DIFFERENT BODY (930000008 vs 930000007). A
        //     function's value must not depend on the SHAPE of its receiver (§15.4).
        //   · PB32's fix left the RECEIVER-BEARING arm forcing pscale = FloatWorkingScale (≥ 9) onto a result
        //     that is an INTEGER by construction, so the ×10⁹ landing pushed a 30-digit exact power past Int128
        //     inside PowNativeInt, whose double fallback then SATURATED — and FUNCTION MOD consumed the sentinel:
        //     `COMPUTE R = FUNCTION MOD(A ** 2, B)` printed 320612800 (13657001 owed) into S9(9)/S9(18)/S9(28)
        //     and the right answer into S9(31), the receiver's PICTURE alone selecting the value. The arms had
        //     merely SWAPPED (feedback_two_arm_dispatch, fifth sighting). The scale a non-negative integer power
        //     needs is 0 — for EVERY receiver — so both arms now land identically and the receiver is not read.
        // A scale-0 base to an integer exponent is scale 0 for ANY exponent, so the result scale is known without
        // knowing the exponent's value — which is exactly why this arm is restricted to a scale-0 base (a
        // scale-s base to the n has scale s·n, with no compile-time scale to give it).
        // ⚠ A NEGATIVE OR RUNTIME-ITEM EXPONENT CANNOT LAND AT A COMPILE-TIME SCALE: §8.8.1.2's reciprocal for a
        // negative exponent is not an integer (`2 ** -2` at scale 0 ⇒ 0 instead of 0.25 — a measured regression),
        // and a data-item exponent's sign is a run-time fact, so NO fixed scale serves both regimes — scale 9
        // corrupts the big positive powers (measured: `A ** X` with X = 2 gave the same 320612800), scale 0
        // truncates the reciprocals. The carrier that owns its scale AT RUN TIME is the SDIDI, so those shapes
        // return Dec: PowNativeIntDec computes the same owner-decided values (exact Int128 loop while it fits,
        // the documented double approximation past it / for the reciprocal) on the carrier every downstream
        // consumer already handles (the PB32/PB14 carrier-total sweep). Receiver-independent by construction.
        // ⛔ ONE ARM FOR BOTH EXPONENT SHAPES, AND IT IS THE DEC ONE (kb/Work PB69). The literal-exponent arm used
        // to return Int128 from PowNativeInt, whose past-the-carrier fallback SATURATED to Int128.MaxValue —
        // fine above a STORE's capacity check (the receiver reports SIZE ERROR), poison at every VALUE-semantics
        // consumer: `FUNCTION MOD(A ** 3, B)` answered from the sentinel (639816141 for 980012199), `A ** 4 >
        // A ** 3` was FALSE and `A ** 3 = A ** 4` TRUE (both saturated), `A ** 3 / A ** 2` was 1.7e8, and the SAME
        // expression spelled `A ** X` (the Dec arm) gave a THIRD number. The value that leaves the carrier is the
        // owner-decided double approximation, and the carrier that can hold it AND the exact Int128 fast path is
        // the SDIDI: PowNativeIntDec is exact while the power fits (CobolDec.From keeps the full significand — no
        // 34-digit rounding on construction), the approximation past it, receiver-independent by construction.
        // Consumers: a relation compares Decs exactly (CobolDec.Compare); an intrinsic with a Dec argument routes
        // to its SDIDI body under native too (IntrinsicRenderer.RenderNum); an Int128 landing that cannot hold the
        // value raises EC-SIZE-OVERFLOW (CobolDec.ToUnscaledIntermediate — A.1 item 179, "checked") instead of
        // returning modular digits. `expIsNonNegativeLiteral` no longer selects a body — kept for the callers'
        // §8.8.1.2 rule-6 screening context only.
        _ = expIsNonNegativeLiteral;
        bool integerOperands = !b.Real && !e.Real && !b.Dec && !e.Dec && b.Scale == 0 && e.Scale == 0;
        if (integerOperands)
            return new NumX(RuntimeApi.Intrinsic("PowNativeIntDec", $"{b.Expr}, {e.Expr}"), 0, Dec: true);
        // D16 (NATIVE): a float base/exponent OR a float receiver keeps the result FLOATING (native double) — skip
        // the FromDouble quantize-back that a pure fixed-point power needs, so a float ** stays in the float pipeline.
        // A receiver-less exponentiation keeps the binary64 result for the same reason the float-intrinsic family
        // does (PB13): §8.8.1.2 already makes this an implementor-defined approximation computed in double, and
        // with no receiver there is no scale to quantize TO — the ws = 9 stand-in saturated, so `IF 10 ** 30 =
        // 10 ** 31` evaluated TRUE. A fixed-point receiver still quantizes, at the capacity-capped working scale.
        // ⛔ BOTH ARMS NOW SCREEN §8.8.1.2 RULE 6 (PB28) — `PowNativeReal`, not a bare `System.Math.Pow`. The rule
        // is a GENERAL rule of arithmetic-expression evaluation and binds native `**` exactly as it binds the
        // SDIDI one, which `CobolDec.Pow` above has always honoured while every native arm ignored it.
        if (b.Real || e.Real || _rcv.Real || _rcv.Receiverless)
            return new NumX(RuntimeApi.Intrinsic("PowNativeReal", $"{Real(b)}, {Real(e)}"), 0, Real: true);
        // ⛔ THE SAME QUANTIZER, SO THE SAME CAP (PB13's sibling — feedback_scan_all_similar). A flat
        // max(Scale, 9) here silently saturated `COMPUTE R = 10 ** 30` into a PIC 9(31) exactly as it did for the
        // float-intrinsic family; ReceiverContext.FloatWorkingScale is the one rule both consume.
        int ws = _rcv.FloatWorkingScale;
        return new NumX(RuntimeApi.Intrinsic("FromDouble",
            $"{RuntimeApi.Intrinsic("PowNativeReal", $"{Real(b)}, {Real(e)}")}, {ws}{CheckedFlag}"), ws);
    }

    private static NumX Negate(NumX x) =>
        x.Real ? new NumX($"(-({Real(x)}))", 0, Real: true)
        : x.Dec ? new NumX($"(new CobolDec(-({x.Expr}).Sig, ({x.Expr}).Exp))", 0, Dec: true)
        : x.U ? new NumX($"(-{DeU(x).Expr})", x.Scale)   // negation is arithmetic — the Widen funnel applies
        : new($"(-{x.Expr})", x.Scale);

    /// <summary>The unsigned-wide → Int128-lane funnel (kb/Work R10): a <c>U</c> operand narrows through
    /// <c>CobolNum.Widen</c> (loud beyond the documented Int128 intermediate, CONFORMANCE.md §4.2.16); every
    /// other operand passes through unchanged. The ONE chokepoint every arithmetic path shares (internal: the
    /// CALL BY CONTENT computed-argument site funnels through the same rule).</summary>
    internal static NumX DeU(NumX x) => x.U ? new NumX(RuntimeApi.NumWiden(x.Expr), x.Scale) : x;

    /// <summary>
    /// Land a rendered intermediate into the exact <c>Int128</c> lane at the receiver's working scale — the ONE
    /// landing every value-semantics consumer of a <c>(Expr, Scale)</c> pair shares: an intrinsic argument, a CALL
    /// BY VALUE arithmetic-expression argument, a SET pointer UP/DOWN BY amount, an ALLOCATE character count. A
    /// native intermediate is already there; an SDIDI (<c>Dec</c>) intermediate — every arithmetic expression under
    /// a standard mode, and under NATIVE arithmetic an integer power (kb/Work PB69) — lands CHECKED at
    /// <c>rcv.WorkingScale(NumvalScaleFloor)</c> (EC-SIZE-OVERFLOW past the carrier — §14.7.5 case 5, A.1 item 179
    /// "checked" — never the modular low-order digits a STORE may use, because a value-semantics consumer has no
    /// capacity check downstream); a float under a STANDARD mode is converted in first (§8.8.1.5.1 — the mode
    /// beats the float branch, COBOLNET_NUMERIC_DESIGN.md D3) and lands the same way. A float under NATIVE
    /// arithmetic stays binary64 — the consumer's own float arm applies (see <see cref="FixedLane"/> for the
    /// consumers that have none).
    /// <para>⛔ kb/Work PB84: after PB69 made <c>A ** 2</c> an SDIDI intermediate under native arithmetic, every
    /// consumer that read <c>x.Expr</c> as a native carrier — <c>(long)(x.Expr)</c> in the pointer and CALL BY
    /// VALUE emitters, the sign condition's <c>x.Expr &gt; 0</c> (NIST NC250A), the INVOKE BY CONTENT store —
    /// became a Roslyn error on conforming COBOL. The same consumers were ALREADY wrong for every STANDARD-DECIMAL
    /// expression; the SDIDI arm was written once here and the consumers now funnel through it, so the next carrier
    /// (or the next consumer) has one place to be right in.</para>
    /// </summary>
    public NumX Landed(NumX x, ReceiverContext rcv)
    {
        if (x.Real && StandardDecimal)
            x = new NumX(DecOperand(x), 0, Dec: true);
        if (!x.Dec) return x;
        int ws = rcv.WorkingScale(ReceiverContext.NumvalScaleFloor);
        return new NumX(RuntimeApi.DecToUnscaledIntermediate(x.Expr, ws.ToString(), CobolRounding.Truncation), ws);
    }

    /// <summary><see cref="Landed"/> for a consumer that computes ONLY in the exact <c>Int128</c> lane and has no
    /// float arm of its own — the DIVIDE … REMAINDER subsidiary-quotient kernel (§14.9.12.4 GR7 — kb/Work PB85: a
    /// FLOAT-LONG dividend was a Roslyn error). A native float lands TRUNCATED at the receiver's float working scale
    /// (<see cref="ReceiverContext.FloatWorkingScale"/> — the D16 quantizer's one rule); an unsigned-wide read
    /// funnels through <see cref="DeU"/>; everything else is <see cref="Landed"/>.</summary>
    public NumX FixedLane(NumX x, ReceiverContext rcv) =>
        x.Real && !StandardDecimal ? new NumX(Align(x, rcv.FloatWorkingScale), rcv.FloatWorkingScale)
        : Landed(DeU(x), rcv);

    /// <summary>The value/scale/profile argument run that stores a rendered intermediate into a fixed-point
    /// receiver's <c>NumProfile</c> — the ONE place the carriers are told apart at a store (kb/Work PB84: the
    /// arithmetic store, the numeric MOVE and the INVOKE BY CONTENT expression each spelled it, and the third had
    /// no <c>Dec</c> arm): an SDIDI intermediate takes the <c>CobolNum.Store(CobolDec, profile)</c> overload (the
    /// §14.7 final transfer — whose <c>Store</c>/<c>TryStore</c> pair already tells the two landings apart, PB74); a
    /// float lands at the receiver scale through <c>ToScaled</c> (CHECKED — <paramref name="checkedLanding"/>, an
    /// ON SIZE ERROR / EC-SIZE store) or <c>ToScaledUnchecked</c> (a MOVE, the no-phrase store, INVOKE BY CONTENT —
    /// the low-order digits past the carrier, kb/Work PB77) with the receiver's ROUNDED mode (D16) and then stores as
    /// a native at that scale (rescale identity ⇒ no double rounding); a native passes its own scale (an exact-family
    /// intrinsic chose ITS landing form at the render — <c>IntrinsicRenderer.CheckedFlag</c>).</summary>
    public static string StoreArgs(NumX value, int recvScale, CobolRounding mode, string profile, bool checkedLanding) =>
        value.Dec ? $"{value.Expr}, {profile}"
        : value.Real ? $"{RuntimeApi.FloatToScaled(value.Expr, $"{recvScale}", mode, checkedLanding)}, {recvScale}, {profile}"
        : $"{value.Expr}, {value.Scale}, {profile}";

    /// <summary>The unchecked store of a rendered intermediate into a fixed-point receiver — <see cref="StoreArgs"/>
    /// (the UNCHECKED landing: a MOVE's §14.6.8.2 r4 truncation, INVOKE BY CONTENT) through <c>CobolNum.Store</c>
    /// (<c>StoreU</c> on the unsigned-wide lane, by name) with <paramref name="mode"/> (MOVE truncation by default,
    /// §14.6.8.2). Returns the receiver's stored unscaled integer expression.</summary>
    public static string StoreExpr(NumX value, int recvScale, string profile, CobolRounding mode = CobolRounding.Truncation) =>
        RuntimeApi.NumStoreRounded(StoreArgs(value, recvScale, mode, profile, checkedLanding: false), mode, value.U);

    /// <summary>The trailing <c>checkedLanding: true</c> argument for a runtime quantizer / exact-family parse rendered
    /// under ON SIZE ERROR / EC-SIZE checking (kb/Work PB77): the value's landing past the Int128 carrier is then the
    /// SATURATING one (the receiver's capacity check raises, PB13); a MOVE sender or a no-phrase store — the default —
    /// takes the low-order digits. ONE spelling for the float family's <c>FromDouble</c>, native <c>**</c>, and the
    /// NUMVAL family (<c>IntrinsicRenderer</c> reads it through <c>ReceiverContext.InSizeError</c>).</summary>
    internal string CheckedFlag => Checked ? ", checkedLanding: true" : "";

    // Internal (not private): the intrinsic renderer converts float-family arguments to double through THIS
    // one scaled-value→double conversion (ISO §15.4.1 native-arithmetic family; singular-pattern rule).
    // ⛔ A scaled operand routes through the runtime's CORRECTLY-ROUNDED CobolFloat.ScaledToDouble (kb/Work
    // PB115): the former `(double)(x) / <10^scale literal>` built its divisor by repeated multiplication —
    // exact only through 1e22 — so at scale ≥ 23 a LEGAL ASIN(|x| ≤ 1) argument (§15.10.3 r2) arrived one ulp
    // above 1.0 and evaluated NaN; and even a correct literal divisor rounds twice (the Int128 cast, then the
    // divide), which a boundary-sitting argument cannot afford.
    internal static string Real(NumX x) =>
        x.Real ? x.Expr                                   // already a double-typed float intermediate (D16)
        : x.Dec ? $"({x.Expr}).ToDouble()"
        : x.Scale == 0 ? $"(double)({x.Expr})"
        : RuntimeApi.ScaledToDouble(x.Expr, x.Scale);
}
