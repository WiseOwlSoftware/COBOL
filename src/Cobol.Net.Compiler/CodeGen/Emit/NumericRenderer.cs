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
internal sealed class NumericRenderer(EmitContext ctx) : IBoundExprVisitor<NumX>, IBoundOperandVisitor<NumX>
{
    /// <summary>The intrinsic-function render dispatch (ISO §15; IntrinsicRenderer.cs) — created lazily because
    /// the two renderers are mutually recursive (an intrinsic renders its numeric arguments through THIS).</summary>
    internal IntrinsicRenderer Intrinsics => _intrinsics ??= new IntrinsicRenderer(ctx, this);
    private IntrinsicRenderer? _intrinsics;

    /// <summary>The declared PROGRAM COLLATING SEQUENCE tables (§12.3.7) — exposed so a bare figurative in a value
    /// context materializes the runtime-collating extreme (§8.3.3.6.4 GR6/GR7), not the native pin. Null when no
    /// non-native sequence is declared, in which case <c>FigurativeConstants.Fill</c> returns the native pin
    /// (the byte-stable common case). The MOVE and relation paths already thread the same tables.</summary>
    internal CollatingTable? Collating => ctx.Data.Collating;
    internal NationalCollatingTable? NationalCollating => ctx.Data.NationalCollating;

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
    public NumX Visit(BoundNumLiteral n) => EmitText.UnscaledLit(n.Text);
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
    public NumX Visit(BoundOdoExtent n) => new(RuntimeApi.TableOdoExtent(RuntimeApi.TableOcc(PlaceRenderer.Read(n.Depending)),
        n.MinOccurs, n.MaxOccurs, n.FixedWidth, n.ElemWidth), 0);
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
    public NumX Visit(BoundNegate n) => Negate(n.Operand.Accept(this));
    public NumX Visit(BoundPower n) => Power(n.Base.Accept(this), n.Exp.Accept(this), NonNegativeIntegerLiteral(n.Exp));

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
    public NumX Visit(BoundNumericLiteral n) => EmitText.UnscaledLit(n.Text);
    public NumX Visit(BoundFieldOperand n) => FieldNum(n.Place);
    public NumX Visit(BoundComputedOperand n) => n.Expr.Accept(this);
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
            // A numeric-edited sender DE-EDITS to its numeric value at the mask's scale (ISO §14.9.25.4 GR5 — the
            // COBOL-85 de-editing move; the runtime walks the image against the mask's digit positions).
            && p.Item.Pic is { Category: PicCategory.NumericEdited, EditMask: { } dem }
        ? new NumX($"CobolEdit.DeEdit({PlaceRenderer.Read(p)}, {EmitText.CsLiteral(dem)}{ctx.EditCfg(p.Item.Pic)}{RuntimeApi.EditsArg(p.Item.Pic!.EditingRules)})",
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
        // alphanumeric IMAGE as an UNSIGNED integer (a group is category alphanumeric, §8.8.4.1.1; the
        // deterministic digit decode §14.6.13.2 permits for incompatible content). NOTE: a MOVE never reaches
        // this branch — a group SENDER makes the move a GROUP move (§14.9.25.4 GR4: no conversion; classified
        // at EmitMove → EmitGroupToElementaryMove), never a numeric decode of the image (the pre-fix NC105A
        // MOVE MOVE43 TO MOVE3 mis-derivation). A mixed-usage (COMP-leaf) group stays loud (Tier-C).
        // ⛔ IsImageCapable, and correct ONLY because strict conformance now REJECTS a group arithmetic operand
        // (DA6 — §8.8.1.1, COBOLNET0844 in ExpressionBinder). Reaching here therefore means --permissive was
        // requested, and the leniency must be CONSISTENT: before, a group of PIC X leaves computed while a group of
        // PIC 9 leaves threw at run time, so the operand whose digits were unambiguous failed and the merely-textual
        // one succeeded. Migrating this arm while strict still ACCEPTED the construct would have extended acceptance
        // of illegal source instead of fixing anything — the two changes are correct only together. A
        // float/COMP-5/INDEX group has no image at all and stays loud even under --permissive.
        null when p.Item.IsImageCapable =>
            new NumX($"CobolNum.FromAlphanumeric({(p is RedefViewPlace ? PlaceRenderer.Read(p) : $"{PlaceRenderer.Read(p)}.AsImage()")})", 0),
        null => new NumX(EmitText.LoudValue("long", $"numeric use of group item '{p.Item.CobolName ?? PlaceRenderer.Read(p)}'"), 0),
        // A float leaf (COMP-1/COMP-2/FLOAT-SHORT/-LONG/-EXTENDED, D16) enters the arithmetic pipeline as a native
        // IEEE double — NOT truncated to (long) at scale 0 (the pre-D16 stub that silently dropped the fraction). The
        // sending read is wrapped in CobolFloat.Sending (raises EC-DATA-NOT-FINITE for NaN/±Inf under checking, §14.6.13.2
        // item 3) UNLESS this is an exempt context (sign condition / same-usage MOVE — floatCheck false = raw read).
        { IsFloat: true } => new NumX(
            floatCheck ? RuntimeApi.FloatSending($"(double)({PlaceRenderer.Read(p)})") : $"(double)({PlaceRenderer.Read(p)})",
            0, Real: true),
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
        if (StandardDecimal)
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
    internal bool StandardDecimal => ctx.Data.Options.Arithmetic is ArithmeticMode.StandardDecimal or ArithmeticMode.Standard;

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
        "*" => new NumX(_rcv.InSizeError ? $"CobolNum.MulChecked({a.Expr}, {b.Expr})" : $"((Int128)({a.Expr}) * ({b.Expr}))", a.Scale + b.Scale),
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
        string fn = _rcv.InSizeError ? "DivideOrThrow" : "Divide";
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
        if (_rcv.InSizeError)
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
        x.Real ? RuntimeApi.FloatToScaled(x.Expr, $"{toScale}", CobolRounding.Truncation)
        : x.Dec ? RuntimeApi.DecToUnscaled(x.Expr, $"{toScale}", CobolRounding.Truncation)
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

    /// <summary>Render a STOP RUN / GOBACK termination-status phrase to a C# <c>long</c> exit-status expression
    /// (ISO §14.9.42.4 GR5 / §14.9.18.4 GR10): the status VALUE truncated to an integer at scale 0 when present
    /// (SR3 — an integer is passed to the OS), else the implementor error/normal indication ERROR ⇒ 1 / NORMAL ⇒ 0
    /// (§14.9.42.4 GR2/GR3; docs/CONFORMANCE.md §4.2.16). The value renders receiver-less (scale 0) exactly as a
    /// boolean-shift count or an intrinsic integer argument does.</summary>
    public string ExitStatus(TerminationStatus st) =>
        st.Value is { } v ? $"(long)({Align(Render(v, ReceiverContext.None), 0)})" : st.Error ? "1L" : "0L";

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
        bool integerOperands = !b.Real && !e.Real && !b.Dec && !e.Dec && b.Scale == 0 && e.Scale == 0;
        if (integerOperands)
            return expIsNonNegativeLiteral
                ? new NumX(RuntimeApi.Intrinsic("PowNativeInt", $"{b.Expr}, {e.Expr}"), 0)
                : new NumX(RuntimeApi.Intrinsic("PowNativeIntDec", $"{b.Expr}, {e.Expr}"), 0, Dec: true);
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
            $"{RuntimeApi.Intrinsic("PowNativeReal", $"{Real(b)}, {Real(e)}")}, {ws}"), ws);
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

    // Int128 has no implicit conversion to double, so the cast is explicit before the floating divide.
    // Internal (not private): the intrinsic renderer converts float-family arguments to double through THIS
    // one scaled-value→double conversion (ISO §15.4.1 native-arithmetic family; singular-pattern rule).
    internal static string Real(NumX x) =>
        x.Real ? x.Expr                                   // already a double-typed float intermediate (D16)
        : x.Dec ? $"({x.Expr}).ToDouble()"
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
