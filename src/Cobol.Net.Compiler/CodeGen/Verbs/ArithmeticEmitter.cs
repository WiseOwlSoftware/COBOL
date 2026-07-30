// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>The arithmetic emitter (P7 Step 9h — a real collaborator over the per-unit
/// <see cref="EmitContext"/>, extracted from the orchestrator partial): ADD/SUBTRACT/MULTIPLY/DIVIDE/COMPUTE
/// bodies plus the shared services every numeric-storing verb consumes — the <c>EmitArith</c> ON SIZE ERROR /
/// EC-SIZE two-phase wrapper (writes the <see cref="EcState"/> statement scratch the checked stores read — the
/// EC↔arithmetic interlock) and the <c>StoreArith</c> store funnel.</summary>
internal sealed class ArithmeticEmitter(EmitContext ctx, NumericRenderer num, EcState ecState, EcEmitter ec)
{
    /// <summary>The statement dispatcher — property-wired by <see cref="UnitEmitters"/> (the phrase bodies
    /// nest arbitrary statement lists, a cyclic edge no ctor order can satisfy).</summary>
    internal StatementEmitter Statements { get; set; } = null!;

    /// <summary>In-place arithmetic (ADD TO / SUBTRACT FROM / MULTIPLY BY): each receiver ← receiver op Σoperands,
    /// rounded by the receiver's ROUNDED mode (ISO §14.7.4), under the statement's ON SIZE ERROR phrase if any.</summary>
    public void EmitInPlace(IReadOnlyList<Receiver> targets, string op, IReadOnlyList<BoundExpr> operands, SizeErrorPhrase? sizeErr)
        => EmitArith(sizeErr, ise =>
        {
            // The operand sum is the ONE initial evaluation (ISO §14.7.7 GR4 + NOTE 3): with several receivers it
            // must be materialized, or a receiver that aliases an operand (ADD A TO B A C) would poison the
            // receivers stored after it when the inlined expression re-reads the field. The fold itself is
            // receiver-INDEPENDENT (GR4 -- the initial evaluation precedes any receiver's involvement): None.
            NumX value = num.Fold(operands, ReceiverContext.None with { InSizeError = ise });
            if (targets.Count > 1) value = Snapshot(value);
            // Each receiver's FieldNum read is ALSO a sending reference (A = A + value; §14.6.13.2 NOTE 2), so a
            // non-finite float receiver is caught by its CobolFloat.Sending wrap. Precise follow-on (parallel to the
            // "precise ContainsRefMod filter" note): with MULTIPLE float receivers under EC-DATA-NOT-FINITE checking, a
            // later receiver's non-finite read raises AFTER earlier receivers already stored — a half-commit that is
            // only observable under USE-F3 + RESUME NEXT STATEMENT, where a fatal-EC-interrupted statement leaves
            // undefined results anyway (§14.6.13.1.3). Pre-snapshotting all receiver reads before the first store would
            // close it; deferred to avoid restructuring the byte-critical arithmetic store loop for an undefined case.
            foreach (var r in targets)
                StoreArith(r.Place, num.Combine(num.FieldNum(r.Place), op, value, RcvFor(r, ise)), r.Rounding);
        });

    /// <summary>GIVING arithmetic: the value is computed once and stored into each receiver, rounded by that
    /// receiver's own ROUNDED mode (ISO §14.7.5 rule 4 — one value, stored left-to-right into each resultant).</summary>
    public void EmitGiving(IReadOnlyList<Receiver> targets, Func<ReceiverContext, NumX> value, SizeErrorPhrase? sizeErr)
        => EmitArith(sizeErr, ise =>
        {
            // The RHS is computed FOR the receiver SET -- the widest receiver scale, Real only when EVERY
            // receiver is float -- the §14.7.7 GR4 one-initial-evaluation shape EmitCompute's multi-target path
            // established (D16). Pre-P7.3 this RHS rendered under the PREVIOUS statement's leftover Target*
            // state (EmitGiving never called SetTarget) -- the H1 staleness ReceiverContext kills.
            var rcv = new ReceiverContext(targets.Max(t => ScaleOf(t.Place)),
                targets.All(t => t.Place.Item.Pic is { IsFloat: true }), CobolRounding.Truncation, ise);
            NumX v = value(rcv);
            // ONE initial evaluation (§14.7.7 GR4 + NOTE 3): materialized with several receivers so a receiver
            // aliasing a sender cannot change the value the remaining receivers store.
            if (targets.Count > 1) v = Snapshot(v);
            foreach (var r in targets) StoreArith(r.Place, v, r.Rounding);
        });

    public void EmitDivide(IReadOnlyList<Receiver> targets, BoundExpr? dividend, BoundExpr divisor, SizeErrorPhrase? sizeErr)
        => EmitArith(sizeErr, ise =>
        {
            // The SENDERS are identified/evaluated ONCE (ISO §14.9.12.4 GR5 → §14.7.7 GR4 + NOTE 3): with several
            // receivers both operands are materialized, so a receiver that aliases the dividend or the divisor
            // (DIVIDE b INTO a GIVING x a y — NC172A/NC173A) cannot poison the quotients stored after it. The
            // DIVISION itself still renders per receiver, at that receiver's scale + ROUNDED mode — equal to
            // rounding the spec's intermediate to each resultant. Sub-expression quotients inside the operands
            // render at the widest receiver scale (the intermediate must not lose receiver-visible digits).
            var opRcv = new ReceiverContext(targets.Max(t => ScaleOf(t.Place)),
                targets.All(t => t.Place.Item.Pic is { IsFloat: true }), CobolRounding.Truncation, ise);
            NumX divisorX = num.Render(divisor, opRcv);
            NumX? dividendX = dividend is not null ? num.Render(dividend, opRcv) : null;
            if (targets.Count > 1)
            {
                divisorX = Snapshot(divisorX);
                if (dividendX is { } dx) dividendX = Snapshot(dx);
            }
            foreach (var r in targets)
            {
                // The quotient renders at the receiver's OWN scale + ROUNDED mode.
                NumX q = dividendX ?? num.FieldNum(r.Place);            // INTO-no-GIVING divides the target
                // The DIVIDE top-level quotient IS the final transfer to r — outermost, so it rounds at r's scale +
                // ROUNDED mode (§14.9.12.4 / §14.7.4). Operand sub-divisions (rendered above at :75-76) stay nested.
                StoreArith(r.Place, num.Combine(q, "/", divisorX, RcvFor(r, ise), outermost: true), r.Rounding);
            }
        });

    /// <summary><c>DIVIDE … GIVING q REMAINDER r</c> (ISO §14.9.12 GR7): the remainder is defined from the
    /// INTERMEDIATE quotient TRUNCATED at the quotient receiver's scale — even when the stored quotient is ROUNDED
    /// — as <c>remainder = dividend − (intermediate quotient × divisor)</c>; the subtraction aligns scales exactly.
    /// The quotient stores with its OWN rounding (recomputed at the receiver's mode when not truncation).
    /// Under STANDARD / STANDARD-DECIMAL (P10 Step 12) the subsidiary quotient stays this EXACT receiver-scale
    /// kernel division: GR6c truncates it at the receiver's digits, and the exact integer-remainder truncation
    /// equals the SDIDI-quotient-then-truncate result for every case except a true quotient carrying 34+
    /// consecutive nines at the rounding boundary (the same documented extra-precision residue class as the
    /// >34-digit exact intrinsics — CobolIntrinsics.Exact.cs header); the back-multiply/subtract DO evaluate
    /// in SDIDI form through the mode-aware <c>Combine</c>.</summary>
    public void EmitDivideRemainder(BoundDivideRemainder d)
        => EmitArith(d.SizeError, ise =>
        {
            var w = ctx.Writer;
            int qs = ScaleOf(d.Quotient.Place);
            // The senders render FOR the quotient receiver (its scale governs the intermediate, GR6c/GR7);
            // pre-P7.3 they rendered under the previous statement's leftover Target* state (H1).
            var rcv = new ReceiverContext(qs, d.Quotient.Place.Item.Pic is { IsFloat: true },
                CobolRounding.Truncation, ise);
            // Both senders are materialized (§14.9.12.4 GR5 — one item identification/evaluation): each appears in
            // SEVERAL emitted expressions (kernel call(s) + the remainder back-multiply), and the quotient stores
            // BEFORE the remainder is formed — a quotient receiver aliasing a sender must not poison the remainder.
            NumX dividend = Snapshot(num.Render(d.Dividend, rcv)), divisor = Snapshot(num.Render(d.Divisor, rcv));
            // The SUBSIDIARY quotient is truncated to the GIVING receiver's digits/scale (ISO §14.9.12 GR6c) —
            // a DIRECT kernel call at EXACTLY the receiver scale, not the renderer's working-scale promotion
            // (which yields the quotient at the dividend's higher scale and poisons the remainder multiply).
            string qt = $"__q{ctx.Names.NextStoreTmp()}";
            w.Line($"Int128 {qt} = {RuntimeApi.NumDivide(ise, dividend.Expr, $"{dividend.Scale}", divisor.Expr, $"{divisor.Scale}", $"{qs}", CobolRounding.Truncation)};");
            var product = new NumX($"({qt} * {divisor.Expr})", qs + divisor.Scale);
            NumX remainder = num.Combine(dividend, "-", product, rcv);   // GR7: dividend − subsidiaryQuotient × divisor
            StoreArith(d.Quotient.Place,
                d.Quotient.Rounding == CobolRounding.Truncation
                    ? new NumX(qt, qs)
                    : new NumX(RuntimeApi.NumDivide(ise, dividend.Expr, $"{dividend.Scale}", divisor.Expr, $"{divisor.Scale}", $"{qs}", d.Quotient.Rounding), qs),
                d.Quotient.Rounding);
            StoreArith(d.Remainder, remainder, CobolRounding.Truncation);   // REMAINDER has no ROUNDED phrase
        });

    /// <summary>COMPUTE: the RHS is rendered per receiver (so a quotient is computed at that receiver's scale + mode)
    /// then stored, rounded by the receiver's ROUNDED mode, under the ON SIZE ERROR phrase if any.</summary>
    /// <summary>COMPUTE Format 2 — boolean-compute (ISO §14.9.8): render the boolean RHS ONCE, resize to the
    /// GR3 width (the max static boolean-ITEM positions in the expression; 0 = all-literal, no intermediate
    /// resize — the per-receiver store fits it), then store into each elementary boolean receiver with the
    /// §14.6.8.6 left-align / zero-fill / truncate discipline (the string store, pad '0'; JUSTIFIED honored).
    /// A multi-receiver COMPUTE materializes the value once (the §14.7.7-shaped once-evaluation).</summary>
    public void EmitComputeBoolean(BoundComputeBoolean cb)
    {
        string value = BooleanRenderer.Render(cb.Rhs, num);
        if (cb.Gr3Width > 0) value = RuntimeApi.BoolResize(value, $"{cb.Gr3Width}");
        // One evaluation for multiple receivers (a boolean expr can read an item a prior receiver aliases).
        if (cb.Targets.Count > 1)
        {
            string tmp = $"__be{ctx.Names.NextStoreTmp()}";
            ctx.Writer.Line($"string {tmp} = {value};");
            value = tmp;
        }
        foreach (var t in cb.Targets)
        {
            int width = t is RefModPlace ? -1 : t.Item.Pic?.Length ?? 0;
            string store = width < 0
                ? value   // a ref-mod boolean receiver — the slice write fits via SpliceInto (pad '0')
                : RuntimeApi.StrStoreBoolean(value, $"{width}", t.Item.Justified);
            ctx.Writer.Line(PlaceRenderer.Write(t, store));
        }
    }

    public void EmitCompute(BoundCompute c)
        => EmitArith(c.SizeError, ise =>
        {
            if (c.Targets.Count > 1)
            {
                // ONE initial evaluation (§14.7.7 GR4 + NOTE 3): the RHS renders ONCE — at the widest receiver
                // scale so no receiver-visible digit is lost — is materialized, and every receiver stores from the
                // temp with its own ROUNDED mode. Re-rendering per receiver would re-read senders a prior
                // receiver may alias. Real only when EVERY target is float (D16).
                var rcv = new ReceiverContext(c.Targets.Max(t => ScaleOf(t.Place)),
                    c.Targets.All(t => t.Place.Item.Pic is { IsFloat: true }), CobolRounding.Truncation, ise);
                NumX v = Snapshot(num.Render(c.Rhs, rcv));
                foreach (var r in c.Targets)
                    StoreArith(r.Place, v, r.Rounding);
                return;
            }
            foreach (var r in c.Targets)
                // Single receiver: the RHS's top-level division (if any) IS the final transfer to r — render it
                // outermost so an outermost quotient rounds at r's scale + mode and a nested quotient does not
                // inherit r's mode (CA5; §14.7.7 rule 3 NOTE 1).
                StoreArith(r.Place, num.Render(c.Rhs, RcvFor(r, ise), outermost: true), r.Rounding);
        });

    /// <summary>The <see cref="ReceiverContext"/> for receiver <paramref name="r"/> (P7 Step 3 — the pure
    /// factory replacing the mutable <c>SetTarget</c> context writes).</summary>
    public ReceiverContext RcvFor(Receiver r, bool inSizeError) =>
        new(ScaleOf(r.Place), r.Place.Item.Pic is { IsFloat: true }, r.Rounding, inSizeError);

    /// <summary>The optional <c>blankWhenZero</c> argument text for a numeric-edited store when the receiver
    /// carries BLANK WHEN ZERO (ISO §13.18.8 — zero stores all spaces, MOVE and arithmetic alike).</summary>
    public static string BwzFlag(DataItem item) => item.BlankWhenZero ? ", blankWhenZero: true" : "";

    /// <summary>The program's SPECIAL-NAMES editing-config arguments (<see cref="EmitContext.EditCfgArgs"/>).</summary>
    private string EditCfg() => ctx.EditCfgArgs;

    /// <summary>Materialize a rendered sender/initial-evaluation into a local temp (ISO §14.7.7 GR4 + NOTE 3 —
    /// ONE initial evaluation; results independent of sender/receiver storage overlap). Inlining the expression
    /// into each receiver's store would re-read its fields after earlier receivers stored.</summary>
    private NumX Snapshot(NumX v)
    {
        string t = $"__ie{ctx.Names.NextStoreTmp()}";
        ctx.Writer.Line(v.Dec ? $"CobolDec {t} = {v.Expr};" : $"Int128 {t} = {v.Expr};");
        return v with { Expr = t };
    }

    /// <summary>
    /// Run an arithmetic statement's per-receiver stores (<paramref name="emitStores"/>), wrapping them in the
    /// two-phase ON SIZE ERROR machinery (ISO §14.7.5) when <paramref name="sizeErr"/> is present: a <c>__sizeErr</c>
    /// flag is set by any per-receiver overflow (<c>TryStore</c> false — phase b, the other receivers still store,
    /// rule 2) or by a <c>CobolSizeError</c> raised during evaluation (e.g. a zero divisor — phase a, no receiver
    /// changes, rule 4); the ON / NOT ON SIZE ERROR imperative then runs once. With no phrase the stores run
    /// unchecked (the plain <c>Store</c> path) — behavior unchanged.
    /// </summary>
    public void EmitArith(SizeErrorPhrase? sizeErr, Action<bool> emitStores)
    {
        var w = ctx.Writer;
        // EC-SIZE checking (>>TURN … EC-SIZE … CHECKING ON, ISO §7.3.25): an ENABLED statement routes through
        // the same two-phase TryStore/try-catch shape even WITHOUT the phrase, latching WHICH Table 13 condition
        // occurred so the §14.9.49 F3 selection and the fatal default see the precise level-3 name. Checking off
        // + no phrase = the unchecked fast path, byte-identical (deep-dive D10 / SSOT §18.16).
        var ecSize = ec.EnabledSizeNames();
        if (sizeErr is null && ecSize.Count == 0) { emitStores(false); return; }

        string flag = $"__sizeErr{ctx.Names.NextSizeErr()}";
        w.Line($"bool {flag} = false;");
        string? ecnVar = null;
        if (ecSize.Count > 0)
        {
            ecnVar = $"__sizeEc{ctx.Names.NextEc()}";
            w.Line($"string {ecnVar} = \"\";");
            ecState.SizeErrEcVar = ecnVar;
        }
        ecState.SizeErrVar = flag;
        using (w.Block("try")) emitStores(true);   // checked renders: DivideOrThrow / MulChecked (§14.7.5)
        // A zero divisor / PROHIBITED-inexact quotient raises CobolSizeError; an intermediate that overflows the
        // long engine raises OverflowException (the checked(...) the store wraps the value in). Both are the
        // statement's size error condition (ISO §14.7.5 — the phrase ENABLES checking, incl. case 5 intermediate
        // overflow). >long-range overflow still needs the Int128 carrier (G3).
        if (ecnVar is not null)
        {
            int cid = ctx.Names.NextEc();
            w.Line($"catch (CobolSizeError __cse{cid}) {{ {flag} = true; {ecnVar} = __cse{cid}.EcName; }}");
            w.Line($"catch (System.OverflowException) {{ {flag} = true; {ecnVar} = \"EC-SIZE-OVERFLOW\"; }}");
        }
        else
        {
            w.Line($"catch (CobolSizeError) {{ {flag} = true; }}");
            w.Line($"catch (System.OverflowException) {{ {flag} = true; }}");
        }
        ecState.SizeErrVar = null;
        ecState.SizeErrEcVar = null;

        if (ecnVar is not null)
            ec.EmitSizeHandling(flag, ecnVar, ecSize, hasPhrase: sizeErr?.OnError is not null);

        if (sizeErr?.OnError is { } on)
        {
            using (w.Block($"if ({flag})")) Statements.EmitStatementList(on);
            if (sizeErr.NotOnError is { } notAlso)
                using (w.Block("else")) Statements.EmitStatementList(notAlso);
        }
        else if (sizeErr?.NotOnError is { } not)
            using (w.Block($"if (!{flag})")) Statements.EmitStatementList(not);
    }

    /// <summary>Store an arithmetic result into a numeric target place, rounding to the receiver scale with
    /// <paramref name="mode"/> (the receiver's ROUNDED phrase, ISO §14.7.4). Inside an ON SIZE ERROR statement
    /// (<see cref="EcState.SizeErrVar"/> set) it uses the checked <c>TryStore</c> — on overflow / PROHIBITED-inexact
    /// it sets the flag and leaves the receiver unchanged (§14.7.5); otherwise the plain <c>Store</c>.</summary>
    public void StoreArith(Place target, NumX value, CobolRounding mode)
    {
        var w = ctx.Writer;
        // A numeric-edited receiver stores the EDITED image of the result (ISO §14.7.7 — arithmetic results store
        // per the MOVE editing rules). ROUNDED applies BEFORE editing: the value is rescaled to the mask's
        // fraction scale with the receiver's mode (§14.7.4), then formatted.
        if (target.Item.Pic is { Category: PicCategory.NumericEdited, EditMask: { } mask })
        {
            int ms = RuntimeApi.MaskScale(mask, ctx.Data.CurrencyPicSymbol, ctx.Data.DecimalPointIsComma);
            // The narrowing rescale: under ON SIZE ERROR / EC-SIZE, a PROHIBITED-inexact transfer to an edited
            // receiver is a size error (ISO §14.7.4.3 r7 — the receiver stays UNCHANGED). The Dec path's
            // .ToUnscaled and the numeric path's TryStore already throw/flag on that; the Int128 edited path used
            // plain Rescale (silent truncation) — the DEVLOG-610-audited PROHIBITED leak. Use RescaleChecked in
            // the checked branch so all three receiver categories agree; the unchecked branch stays silent
            // (matching the numeric Store path's no-phrase behavior).
            string Aligned(bool checkedPath) =>
                // A float (Real) result lands at the mask scale via the runtime's ToScaled with the receiver's ROUNDED
                // mode (D16 review: the edited-receiver arithmetic path was missed by the Real integration → CS1503).
                value.Real ? RuntimeApi.FloatToScaled(value.Expr, $"{ms}", mode)
                : value.Dec ? RuntimeApi.DecToUnscaled(value.Expr, $"{ms}", mode)
                : value.Scale == ms ? value.Expr
                : RuntimeApi.NumRescale(value.Expr, $"{value.Scale}", $"{ms}", mode, checkedPath);
            // Under ON SIZE ERROR an edited resultant is capacity-checked too (ISO §14.7.5 case 3 + storing rule
            // 2): an aligned |value| exceeding the mask's digit positions sets the flag and leaves the receiver
            // UNCHANGED — Format's silent high-order truncation is MOVE behavior only (§14.9.25).
            if (ecState.SizeErrVar is { } eflag)
            {
                string img = $"__sv{ctx.Names.NextStoreTmp()}";
                // EC-SIZE checking latches the Table 13 condition: a store whose significant digits do not fit
                // the receiver is EC-SIZE-TRUNCATION ("significant digits truncated in store").
                string onFail = ecState.SizeErrEcVar is { } ecn1 ? $"{{ {eflag} = true; {ecn1} = \"EC-SIZE-TRUNCATION\"; }}" : $"{eflag} = true;";
                w.Line($"if (!{RuntimeApi.EditTryFormat(Aligned(true), $"{ms}", CsLiteral(mask), img, BwzFlag(target.Item) + EditCfg() + RuntimeApi.EditsArg(target.Item.Pic!.EditingRules))}) {onFail}");
                w.Line($"else {PlaceRenderer.Write(target, img)}");
                return;
            }
            w.Line(PlaceRenderer.Write(target, RuntimeApi.EditFormat(Aligned(false), $"{ms}", CsLiteral(mask), BwzFlag(target.Item) + EditCfg() + RuntimeApi.EditsArg(target.Item.Pic!.EditingRules))));
            return;
        }
        // A float RECEIVER (COMP-1/2/FLOAT-*, D16) takes the algebraic value as a native cast — no PICTURE, no
        // scaled store, no SIZE ERROR (IEEE overflow is Inf, a valid value; §14.6.8.3 GR1); ROUNDED is a no-op
        // (the receiver holds the exact algebraic value). BEFORE the fixed-point guard below.
        if (target.Item.Pic is { IsFloat: true })
        {
            w.Line(PlaceRenderer.Write(target, $"({target.Item.Pic.ClrType})({NumericRenderer.Real(value)})"));
            return;
        }
        if (target.Item.Pic is not { Category: PicCategory.Numeric, IsFloat: false })
        {
            w.Line(LoudStmt($"arithmetic into a non-fixed-point target '{target.Item.CobolName ?? PlaceRenderer.Read(target)}'"));
            return;
        }
        string profile = target.Item.ProfileName;
        // A float (Real) arithmetic result lands into this FIXED receiver via the runtime's ToScaled at the receiver
        // scale with the receiver's ROUNDED mode (D16), then flows through the ordinary store funnel (rescale
        // identity ⇒ no double-rounding; capacity + SIZE ERROR still apply). A STANDARD-DECIMAL intermediate stores
        // through the SDIDI overloads (the §14.7 final transfer).
        int recvScale = target.Item.Pic!.Scale;
        string valExprA = value.Real ? RuntimeApi.FloatToScaled(value.Expr, $"{recvScale}", mode) : value.Expr;
        string args = value.Dec ? $"{value.Expr}, {profile}"
            : value.Real ? $"{valExprA}, {recvScale}, {profile}"
            : $"{value.Expr}, {value.Scale}, {profile}";
        if (ecState.SizeErrVar is { } flag)
        {
            string tmp = $"__sv{ctx.Names.NextStoreTmp()}";
            // Intermediate long-engine overflow is detected upstream by the checked multiply the renderer emits in a
            // size-error context (the checked multiply → OverflowException, caught by the statement's try, §14.7.5
            // case 5). We do NOT wrap the value in checked(...) here: a constant subexpression would then overflow at
            // COMPILE time (CS0220) and reject valid COBOL — the runtime helper avoids that by not constant-folding.
            // Under EC-SIZE checking the receiver-capacity failure latches EC-SIZE-TRUNCATION (Table 13 —
            // "significant digits truncated in store"; the §14.7.5 size error on the final transfer).
            string onFail = ecState.SizeErrEcVar is { } ecn2 ? $"{{ {flag} = true; {ecn2} = \"EC-SIZE-TRUNCATION\"; }}" : $"{flag} = true;";
            // A float (Real) source under ROUNDED MODE PROHIBITED: an inexact transfer is a size error and leaves the
            // receiver UNCHANGED (§14.7.5 r7). ToScaled already truncated the fraction, so the store's own PROHIBITED
            // check cannot see it — gate on InexactAtScale first (D16 review finding).
            if (value.Real && mode == CobolRounding.Prohibited)
            {
                w.Line($"if ({RuntimeApi.FloatInexactAtScale(value.Expr, $"{recvScale}")}) {onFail}");
                w.Line($"else if (!{RuntimeApi.NumTryStore(args, mode, tmp)}) {onFail}");
            }
            else
                w.Line($"if (!{RuntimeApi.NumTryStore(args, mode, tmp)}) {onFail}");
            // On success store the value (a whole-group-aliased numeric-DISPLAY receiver stores its character image).
            w.Line($"else {PlaceRenderer.Write(target, target.Item.StoreAsImage ? RuntimeApi.NumFormatImage(tmp, profile) : Narrow(tmp, target.Item))}");
            return;
        }
        string stored = RuntimeApi.NumStoreRounded(args, mode);
        w.Line(PlaceRenderer.Write(target, target.Item.StoreAsImage ? RuntimeApi.NumFormatImage(stored, profile) : Narrow(stored, target.Item)));
    }

    /// <summary>The receiver's working scale: an edited receiver's is its MASK's fraction scale (a `.`-pointed
    /// mask has PicInfo.Scale 0 — the point lives in the mask, not in V); a numeric item's is its PIC scale.</summary>
    /// <summary>Wrap a wide (Int128) stored value for assignment into a NARROW receiver field: a ≤18-digit item
    /// stores as native <c>long</c> (the value is already truncated/rounded to the receiver's digits, so the cast
    /// is exact); a 19+-digit item (the 2002+ wide tier) stores the Int128 directly.</summary>
    public static string Narrow(string expr, DataItem item) =>
        item.Pic is { Digits: > 18 } ? expr : $"(long)({expr})";

    private int ScaleOf(Place p) =>
        p.Item.Pic is { Category: PicCategory.NumericEdited, EditMask: { } m }
            ? RuntimeApi.MaskScale(m, ctx.Data.CurrencyPicSymbol, ctx.Data.DecimalPointIsComma)
        : p.Item.Pic?.Scale ?? 0;
}
