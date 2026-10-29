// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.CodeGen.Emit;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.CodeGen;

using static WiseOwl.Cobol.CodeGen.Emit.EmitText;

/// <summary>The arithmetic emitter (P7 Step 9h — a real collaborator over the per-unit
/// <see cref="EmitContext"/>, extracted from the orchestrator partial): ADD/SUBTRACT/MULTIPLY/DIVIDE/COMPUTE
/// bodies plus the shared services every numeric-storing verb consumes — the <c>EmitArith</c> ON SIZE ERROR /
/// EC-SIZE two-phase wrapper (writes the <see cref="EcState"/> statement scratch the checked stores read — the
/// EC↔arithmetic interlock) and the <c>StoreArith</c> store funnel.</summary>
internal sealed class ArithmeticEmitter(EmitContext ctx, NumericRenderer num, EcState ecState, EcEmitter ec,
    ReceiverBracketEmitter brackets)
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
            // receivers stored after it when the inlined expression re-reads the field — and with one receiver that
            // identifies at run time it is materialized too, so the operands are evaluated before the receiver is
            // read or identified (MaterializesInitialEvaluation, kb/Work PB3261). The fold itself is
            // receiver-INDEPENDENT (GR4 -- the initial evaluation precedes any receiver's involvement): None.
            NumX value = num.Fold(operands, ReceiverContext.None with { InSizeError = ise });
            if (MaterializesInitialEvaluation(targets)) value = Snapshot(value);
            // Each receiver's FieldNum read is ALSO a sending reference (A = A + value; §14.6.13.2 NOTE 2), so a
            // non-finite float receiver is caught by its CobolFloat.Sending wrap. Precise follow-on (parallel to the
            // "precise ContainsRefMod filter" note): with MULTIPLE float receivers under EC-DATA-NOT-FINITE checking, a
            // later receiver's non-finite read raises AFTER earlier receivers already stored — a half-commit that is
            // only observable under USE-F3 + RESUME NEXT STATEMENT, where a fatal-EC-interrupted statement leaves
            // undefined results anyway (§14.6.13.1.3). Pre-snapshotting all receiver reads before the first store would
            // close it; deferred to avoid restructuring the byte-critical arithmetic store loop for an undefined case.
            foreach (var r in targets) StoreInPlace(r, op, value, ise);
        });

    /// <summary>ONE in-place resultant — <c>receiver ← receiver op value</c> — computed and stored inside its own
    /// size-error guard (<see cref="GuardedStore"/>). The ONE shape of the in-place store, asked by
    /// <see cref="EmitInPlace"/> per receiver and by <see cref="CorrespondingEmitter.Emit"/> per corresponding pair.
    /// <para>The combine IS the final transfer to the receiver: outermost, so a MULTIPLY BY product past the carrier rounds
    /// once at the receiver's scale with its mode (<see cref="NumericRenderer.Combine"/>).</para>
    /// <para>⛔ A CORRESPONDING PAIR IS A SEPARATE STATEMENT, SO IT GETS THE SEPARATE STATEMENT'S STORE (kb/Work PB229).
    /// ISO §14.9.2.4 GR5 (and §14.9.44.4 GR5 for SUBTRACT): "The results are the same as if the user had referred to each
    /// pair of corresponding identifiers in separate ADD statements", and §14.7.6 runs the SIZE ERROR imperative "after
    /// all of the implied statements are completed". The CORRESPONDING emitter used to write its own combine-and-store
    /// with no per-pair guard, inside the statement's one outer try, so a raise while one pair's sum was EVALUATED (a
    /// binary64 sum past the range, EC-SIZE-OVERFLOW) abandoned every later pair, measured: <c>ADD CORRESPONDING</c>
    /// over a FLOAT-LONG pair at 1E308 and a <c>PIC 9(3)</c> pair 5 + 10 left the second at 10. It also passed no
    /// <c>outermost</c>, so its final transfer was not the one an explicit ADD makes.</para></summary>
    public void StoreInPlace(Receiver r, string op, NumX value, bool inSizeError)
        => GuardedStore(inSizeError, r.Place, r.Rounding, () =>
            StoreArith(r.Place, num.Combine(num.FieldNum(r.Place), op, value, RcvFor(r, inSizeError), outermost: true), r.Rounding));

    /// <summary>GIVING arithmetic: the value is computed once and stored into each receiver, rounded by that
    /// receiver's own ROUNDED mode (ISO §14.7.5 rule 4 — one value, stored left-to-right into each resultant).</summary>
    public void EmitGiving(IReadOnlyList<Receiver> targets, Func<ReceiverContext, NumX> value, SizeErrorPhrase? sizeErr)
        => EmitArith(sizeErr, ise =>
        {
            // The RHS is computed FOR the receiver SET -- the widest receiver scale, Real only when EVERY
            // receiver is float -- the §14.7.7 GR4 one-initial-evaluation shape EmitCompute's multi-target path
            // established (D16). Pre-P7.3 this RHS rendered under the PREVIOUS statement's leftover Target*
            // state (EmitGiving never called SetTarget) -- the H1 staleness ReceiverContext kills.
            // The float-quantization headroom is the MOST CONSTRAINING receiver's — the widest integer part —
            // since one rendered value has to land in all of them (PB13).
            var rcv = SetRcv(targets, ise);
            NumX v = value(rcv);
            // ONE initial evaluation (§14.7.7 GR4 + NOTE 3): materialized with several receivers so a receiver
            // aliasing a sender cannot change the value the remaining receivers store.
            if (MaterializesInitialEvaluation(targets)) v = Snapshot(v);
            foreach (var r in targets) GuardedStore(ise, r.Place, r.Rounding, () => StoreArith(r.Place, v, r.Rounding));
        });

    /// <summary><c>MULTIPLY a BY b GIVING r…</c> (ISO §14.9.26): the product is each receiver's FINAL TRANSFER, so — like the
    /// quotient of <see cref="EmitDivide"/> — it renders PER RECEIVER at that receiver's scale and ROUNDED mode
    /// (<see cref="NumericRenderer.Multiply"/>'s outermost arm: a product past the Int128 carrier is the exact product
    /// rounded once, whatever width the receiver is — kb/Work PB1143's review finding N1), from operands that are
    /// identified and evaluated ONCE (§14.7.7 GR4 + NOTE 3), materialized with several receivers so a receiver that
    /// aliases an operand cannot poison the products stored after it.</summary>
    public void EmitMultiplyGiving(IReadOnlyList<Receiver> targets, BoundExpr a, BoundExpr b, SizeErrorPhrase? sizeErr)
        => EmitArith(sizeErr, ise =>
        {
            // Operand sub-expressions render for the receiver SET (the widest scale, Real only when EVERY receiver is
            // float — D16), as EmitDivide's operands do.
            var opRcv = targets.Count == 1 ? RcvFor(targets[0], ise)
                : SetRcv(targets, ise);
            NumX ax = num.Render(a, opRcv), bx = num.Render(b, opRcv);
            if (MaterializesInitialEvaluation(targets)) { ax = Snapshot(ax); bx = Snapshot(bx); }
            foreach (var r in targets)
                GuardedStore(ise, r.Place, r.Rounding, () =>
                    StoreArith(r.Place, num.Combine(ax, "*", bx, RcvFor(r, ise), outermost: true), r.Rounding));
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
            var opRcv = SetRcv(targets, ise);
            NumX divisorX = num.Render(divisor, opRcv);
            NumX? dividendX = dividend is not null ? num.Render(dividend, opRcv) : null;
            if (MaterializesInitialEvaluation(targets))
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
                // Each receiver's quotient can raise its OWN size error (a PROHIBITED-inexact at r's scale) —
                // guarded per receiver so the raise does not abandon the receivers to its right (PB129).
                GuardedStore(ise, r.Place, r.Rounding, () =>
                    StoreArith(r.Place, FinalQuotient(q, divisorX, RcvFor(r, ise)), r.Rounding));
            }
        });

    /// <summary>⛔ THE ONE FORMER OF A DIVIDE QUOTIENT AS THE FINAL TRANSFER INTO A RESULTANT (ISO §14.9.12.4 1) and 5) b) /
    /// 6) b): "The quotient is then formed as specified in General rule 1 and stored in identifier-3 as specified in 14.7.7")
    /// - <c>DIVIDE ... GIVING</c> and <c>DIVIDE ... GIVING ... REMAINDER</c> store the SAME quotient, because both ask this
    /// method, so adding the REMAINDER phrase cannot change what identifier-3 holds (kb/Work PB2923: the REMAINDER arm used
    /// to form it a second way, at the receiver's fixed scale, which a floating-point identifier-3 does not have). The
    /// subsidiary quotient of GR6 c) is derived FROM this quotient and never the other way round
    /// (<see cref="CarriedSubsidiaryQuotient"/>).</summary>
    private NumX FinalQuotient(NumX dividend, NumX divisor, ReceiverContext into) =>
        num.Combine(dividend, "/", divisor, into, outermost: true);

    /// <summary><c>DIVIDE … GIVING q REMAINDER r</c> (ISO §14.9.12.4 GR6 and GR7): the remainder is defined from the
    /// INTERMEDIATE quotient TRUNCATED into identifier-3's SHAPE (<see cref="SubsidiaryShapeOf"/>) - even when the stored
    /// quotient is ROUNDED - as <c>remainder = dividend − (subsidiary quotient × divisor)</c>; the subtraction aligns
    /// scales exactly. The quotient stores with its OWN rounding (recomputed at the receiver's mode when not truncation),
    /// formed by the very <see cref="FinalQuotient"/> the statement without REMAINDER stores.
    /// <para>⛔ ONE KERNEL PER LANE, AND THE SDIDI LANE'S IS THE SDIDI'S (kb/Work PB3017). GR1 makes the quotient "the
    /// result of the arithmetic expression (dividend / divisor)" under standard-decimal arithmetic and GR7 makes the
    /// remainder "(dividend - (subsidiary-quotient * divisor))", both SDIDI expressions, and GR6 c) derives the
    /// subsidiary quotient from THAT quotient by truncation. The statement used to run an Int128 kernel in every mode
    /// and let only the back-multiply and the subtraction ride the SDIDI, so an operand the Int128 carrier cannot hold
    /// — an unsigned 16-byte dividend in [2^127, 2^128), a float or SDIDI dividend past 1.7E+38 — was a size error
    /// (or a saturated sentinel quotient) where §8.8.1.5.2 1) rounds it into the SDIDI and the quotient fits the
    /// receiver. The SDIDI lane now forms the quotient, the subsidiary quotient and the remainder on
    /// <c>CobolDec</c>, from the OPERANDS as they are (<see cref="NumericRenderer.DecOperand"/> lifts each); the native
    /// lane keeps the exact Int128 kernel for two scaled operands and a FIXED-POINT identifier-3, whose intermediate range
    /// is checked (§14.7.5 5)), and divides a binary64 or SDIDI operand as it is (<see cref="CarriedSubsidiaryQuotient"/>,
    /// kb/Work PB3829). A FLOATING-POINT identifier-3 (a float item, a floating-point numeric-edited item) has no fixed
    /// scale for that kernel to truncate at, so in EVERY mode it takes <see cref="CarriedSubsidiaryQuotient"/> (kb/Work
    /// PB2923).</para></summary>
    public void EmitDivideRemainder(BoundDivideRemainder d)
        => EmitArith(d.SizeError, ise =>
        {
            var w = ctx.Writer;
            var shape = SubsidiaryShapeOf(d.Quotient.Place);
            // The senders render FOR the quotient receiver (its scale governs the intermediate, GR6c/GR7) - by the ONE
            // context rule, ReceiverContext.Of, so a floating-point numeric-edited identifier-3 is flagged as such and
            // a nested quotient in an operand is not cut at its mask's working-scale hint (kb/Work PB2694, PB2923);
            // pre-P7.3 they rendered under the previous statement's leftover Target* state (H1).
            var rcv = ReceiverContext.Of(d.Quotient.Place.Item.Pic, CobolRounding.Truncation, ise);
            var (dividend, divisor, subsidiary, quotientFor) = num.StandardDecimal || shape.Form != SubsidiaryForm.FixedPoint
                ? CarriedSubsidiaryQuotient(d, Snapshot(num.Render(d.Dividend, rcv)), Snapshot(num.Render(d.Divisor, rcv)), ise, shape)
                : NativeSubsidiaryQuotient(d, rcv, ise, shape);
            // The back-multiply rides Combine — the native product's digit-bound arms, the Dec lane under a standard
            // mode - instead of a raw C# splice whose Int128 product wrapped silently (PB129). A product past the Int128
            // carrier is the exact wide form, KEPT exact into the subtraction (keepWide) so the remainder is formed from
            // the exact back-multiply and settled once, by the outer Combine (kb/Work PB3538).
            NumX remainder = num.Combine(dividend, "-",
                num.Combine(subsidiary, "*", divisor, rcv, keepWide: true), rcv);   // GR7: dividend − subsidiaryQuotient × divisor
            // The quotient STORE sees the whole quotient, so an over-wide one meets ITS size error (§14.7.5) and takes the
            // statement's disposition there.
            GuardedStore(ise, d.Quotient.Place, d.Quotient.Rounding, () => StoreArith(d.Quotient.Place,
                quotientFor(d.Quotient.Rounding), d.Quotient.Rounding));
            // GR6c stores identifier-4 only "if the size error condition is not raised": under a SIZE ERROR phrase (ON, NOT
            // or both, kb/Work PB3989's one disposition for every [NOT] SIZE ERROR statement) or EC-SIZE checking the store
            // is gated on the statement's flag, so the quotient's size error leaves the remainder untouched. With checking
            // NOT enabled and no phrase, the size error condition is §14.6.13.1.3 8)'s, whose receivers are the
            // implementor's: docs/CONFORMANCE.md DOC-A.1-70 follows GnuCOBOL 3.2 (libcob cob_div_quotient /
            // cob_div_remainder), which stores the remainder whatever the quotient met, formed from the quotient truncated
            // at identifier-3's scale with every integer digit (SubsidiaryShape; kb/Work PB3975). A zero divisor raises
            // before either store and leaves both.
            if (ise)
                using (w.Block($"if (!{ecState.SizeErrVar})"))
                    GuardedStore(true, d.Remainder, CobolRounding.Truncation, () => StoreArith(d.Remainder, remainder, CobolRounding.Truncation));
            else
                brackets.Receive(d.Remainder, null, () => StoreArith(d.Remainder, remainder, CobolRounding.Truncation));   // REMAINDER has no ROUNDED phrase
        });

    /// <summary>The forms identifier-3 of <c>DIVIDE ... REMAINDER</c> can take, as far as its SUBSIDIARY QUOTIENT is
    /// concerned (ISO §14.9.12.4 6) c)).</summary>
    private enum SubsidiaryForm
    {
        /// <summary>A fixed-point numeric or numeric-edited item (or a PICTURE-less one): a decimal point location and a
        /// digit count.</summary>
        FixedPoint,
        /// <summary>A floating-point numeric-edited item: a significand of a fixed digit count at the quotient's own
        /// exponent (§13.18.40.4 13) b)).</summary>
        FloatEdited,
        /// <summary>A binary floating-point usage (COMP-1/2, FLOAT-SHORT/-LONG/-EXTENDED, FLOAT-BINARY-32/-64; the wider
        /// FLOAT-BINARY-128 and FLOAT-DECIMAL-16/-34 are refused at the data description, COBOL1564): a format.</summary>
        BinaryFloat,
    }

    /// <summary>⛔ IDENTIFIER-3'S SHAPE UNDER TRUNCATION - the ONE description of what "the same number of digits and the same
    /// decimal point location as the data item referenced by identifier-3" (ISO §14.9.12.4 6) c)) means for each form of
    /// identifier-3 (kb/Work PB2923). It replaced a (scale, digit cap) pair that every form was forced into: a
    /// floating-point numeric-edited item took its mask's working-scale HINT as a decimal point location it does not
    /// have, so a quotient below 0.005 truncated to zero in <c>+9.99E+99</c> and the remainder became the whole dividend.
    /// <para><see cref="SubsidiaryForm.FixedPoint"/> carries <see cref="Scale"/> (the receiver-scale rule,
    /// <see cref="ScaleOf"/>): the quotient truncated at identifier-3's decimal point location. It carries NO digit cap.
    /// GR6 c) applies only "If the size error condition is not raised", and then the quotient fits identifier-3, so its
    /// truncation (never further from zero than the stored quotient, whatever the ROUNDED mode) has identifier-3's digits
    /// already. When the quotient DID meet a size error and the remainder is still stored (checking not enabled and no SIZE
    /// ERROR phrase: §14.6.13.1.3 8), docs/CONFORMANCE.md DOC-A.1-70), the subsidiary quotient keeps every integer digit, as
    /// GnuCOBOL 3.2's <c>cob_div_quotient</c> does: <c>DIVIDE 1003 BY 4 GIVING B REMAINDER R</c> with B <c>PIC 99</c> stores
    /// R = 3 (1003 - 250 x 4), where the digit cap kb/Work PB129 used to apply gave 803 (kb/Work PB3975).
    /// The two floating forms carry the item's <see cref="Pic"/> and nothing else: their shape is the item's own.</para></summary>
    private readonly record struct SubsidiaryShape(SubsidiaryForm Form, PicInfo? Pic, int Scale = 0);

    private SubsidiaryShape SubsidiaryShapeOf(Place p) => p.Item.Pic switch
    {
        { IsFloatEdited: true } pic => new(SubsidiaryForm.FloatEdited, pic),
        { IsFloat: true } pic => new(SubsidiaryForm.BinaryFloat, pic),
        null => new(SubsidiaryForm.FixedPoint, null),
        { } pic => new(SubsidiaryForm.FixedPoint, pic, ScaleOf(p)),
    };

    /// <summary>The native lane of <see cref="EmitDivideRemainder"/> for a FIXED-POINT identifier-3: two scaled senders
    /// (an unsigned-wide read funnels through Widen) divide in the exact Int128 kernel, and the subsidiary quotient is a
    /// DIRECT kernel call at EXACTLY the receiver scale — not the renderer's working-scale promotion, which yields the
    /// quotient at the dividend's higher scale and poisons the remainder multiply. A sender on the binary64 or SDIDI
    /// carrier goes to <see cref="CarriedSubsidiaryQuotient"/> instead, which divides it as it is.
    /// Both senders are materialized (§14.9.12.4 GR5 — one item identification/evaluation): each appears in SEVERAL
    /// emitted expressions (kernel call(s) + the remainder back-multiply), and the quotient stores BEFORE the
    /// remainder is formed — a quotient receiver aliasing a sender must not poison the remainder.</summary>
    private (NumX Dividend, NumX Divisor, NumX Subsidiary, Func<CobolRounding, NumX> QuotientFor) NativeSubsidiaryQuotient(
        BoundDivideRemainder d, ReceiverContext rcv, bool ise, SubsidiaryShape shape)
    {
        var w = ctx.Writer;
        int qs = shape.Scale;
        NumX dividendX = num.Render(d.Dividend, rcv), divisorX = num.Render(d.Divisor, rcv);
        if (dividendX.Real || dividendX.Dec || divisorX.Real || divisorX.Dec)
            return CarriedSubsidiaryQuotient(d, Snapshot(dividendX), Snapshot(divisorX), ise, shape);
        NumX dividend = Snapshot(NumericRenderer.DeU(dividendX)),
             divisor = Snapshot(NumericRenderer.DeU(divisorX));
        // The SUBSIDIARY quotient is the quotient truncated at the GIVING receiver's scale (ISO §14.9.12.4 6) c);
        // SubsidiaryShape says why it carries no digit cap). It is also the stored quotient under TRUNCATION.
        string qt = $"__q{ctx.Names.NextStoreTmp()}";
        w.Line($"Int128 {qt} = {RuntimeApi.NumDivide(dividend.Expr, $"{dividend.Scale}", divisor.Expr, $"{divisor.Scale}", $"{qs}", CobolRounding.Truncation)};");
        // Its digit bound is the quotient's own, so the back-multiply is decided on it rather than on the carrier's 39.
        int qDigits = Math.Min(NumericRenderer.QuotientDigitBound(dividend, divisor, qs), PicInfo.CarrierDigitBound);
        return (dividend, divisor, new NumX(qt, qs, Digits: qDigits), mode => mode == CobolRounding.Truncation
            ? new NumX(qt, qs)
            : new NumX(RuntimeApi.NumDivide(dividend.Expr, $"{dividend.Scale}", divisor.Expr, $"{divisor.Scale}", $"{qs}", mode), qs));
    }

    /// <summary>The quotient of <see cref="EmitDivideRemainder"/> for the STANDARD-DECIMAL lane (kb/Work PB3017), for a
    /// FLOATING-POINT identifier-3 in every mode (kb/Work PB2923), and for a NATIVE sender that is a binary64 (a
    /// FLOAT-LONG item, a float function's returned value) or an SDIDI value (a float literal, a NUMVAL value): the
    /// quotient is the SAME division the statement without REMAINDER performs, <see cref="FinalQuotient"/> as the final
    /// transfer into identifier-3 (§14.9.12.4 1): under standard-decimal "the result of the arithmetic expression
    /// (dividend / divisor)"), and the subsidiary quotient is that quotient truncated into identifier-3's shape
    /// (§14.9.12.4 6) c): "a subsidiary quotient is developed that is signed and derived from the quotient by
    /// truncation of digits at the least significant end and that has the same number of digits and the same decimal
    /// point location as the data item referenced by identifier-3"; <see cref="SubsidiaryShapeOf"/>). A quotient on the
    /// SDIDI or binary64 carrier is formed ONCE and the store rounds it by the receiver's mode; one the division already
    /// landed at identifier-3's scale is formed again at the receiver's mode when that is not TRUNCATION.
    /// <para>⛔ THE STORED QUOTIENT IS NEVER DERIVED FROM THE SUBSIDIARY ONE (§14.9.12.4 6) b) forms and stores the quotient;
    /// 6) c) only feeds the remainder). A floating-point identifier-3 stores the quotient with all its digits and
    /// exponent, and its subsidiary quotient is the shape arm's (<see cref="FloatEditedSubsidiary"/>,
    /// <see cref="BinaryFloatSubsidiary"/>).</para>
    /// <para>⛔ NO SENDER IS QUANTIZED BEFORE THE DIVISION (kb/Work PB3829's sweep). The native lane used to land such a sender
    /// TRUNCATED at the receiver's float working scale first, so the ROUNDED decision saw a value already cut:
    /// <c>DIVIDE FL BY 1 GIVING Q ROUNDED MODE AWAY-FROM-ZERO REMAINDER R</c> with FL = 0.12340000000001 stored 0.1234
    /// where the same statement without REMAINDER stored 0.1235 (§14.7.4.3 3)), and <c>DIVIDE 0.09 BY FB GIVING Q
    /// REMAINDER R</c> with FB = 0.30000000000001 stored 0.3000 where the quotient 0.2999999999999 truncates to 0.2999.
    /// One quotient, one rounding, whichever format of DIVIDE asks for it.</para></summary>
    private (NumX Dividend, NumX Divisor, NumX Subsidiary, Func<CobolRounding, NumX> QuotientFor) CarriedSubsidiaryQuotient(
        BoundDivideRemainder d, NumX dividend, NumX divisor, bool ise, SubsidiaryShape shape)
    {
        var pic = d.Quotient.Place.Item.Pic;
        NumX QuotientAt(CobolRounding mode) => FinalQuotient(dividend, divisor, ReceiverContext.Of(pic, mode, ise));
        NumX truncated = Snapshot(QuotientAt(CobolRounding.Truncation));
        NumX subsidiary = shape.Form switch
        {
            SubsidiaryForm.FloatEdited => FloatEditedSubsidiary(truncated, shape.Pic!),
            SubsidiaryForm.BinaryFloat => BinaryFloatSubsidiary(truncated, shape.Pic!),
            _ => FixedPointSubsidiary(truncated, ise, shape),
        };
        return (dividend, divisor, subsidiary,
            mode => mode == CobolRounding.Truncation || truncated.Dec || truncated.Real ? truncated : QuotientAt(mode));
    }

    /// <summary>The subsidiary quotient for a FIXED-POINT identifier-3: the quotient truncated at its scale (kb/Work PB3017;
    /// no digit cap, <see cref="SubsidiaryShape"/>, kb/Work PB3975).</summary>
    private NumX FixedPointSubsidiary(NumX truncated, bool ise, SubsidiaryShape shape)
    {
        int qs = shape.Scale;
        if (truncated.Dec)
        {
            string sq = $"__sq{ctx.Names.NextStoreTmp()}";
            ctx.Writer.Line($"CobolDec {sq} = {RuntimeApi.DecSubsidiaryQuotient(truncated.Expr, qs)};");
            return new NumX(sq, 0, Dec: true);
        }
        // A binary64 quotient lands at identifier-3's scale by truncation (the checked form under ON SIZE ERROR /
        // EC-SIZE); a scaled one is already there.
        if (truncated.Real)
            return Snapshot(new NumX(RuntimeApi.FloatToScaled(truncated.Expr, $"{qs}", CobolRounding.Truncation, ise), qs));
        return truncated.Scale == qs ? truncated
            : throw new InvalidOperationException(
                $"DIVIDE REMAINDER: a final-transfer quotient at scale {truncated.Scale}, not identifier-3's {qs}");
    }

    /// <summary>The subsidiary quotient for a FLOATING-POINT numeric-edited identifier-3 (ISO §14.9.12.4 6) c)): the quotient
    /// as the item holds it under truncation, <c>CobolEdit.TruncateToFloatShape</c> - its significand's digits at the
    /// quotient's own exponent (§13.18.40.4 13) b), §14.6.8.4 1)), the normalization its store performs. An SDIDI value
    /// (<c>CobolDec</c>, exact), whatever carrier the quotient arrived on, so the back-multiply keeps every digit.</summary>
    private NumX FloatEditedSubsidiary(NumX truncated, PicInfo pic)
    {
        string sq = $"__sq{ctx.Names.NextStoreTmp()}";
        ctx.Writer.Line($"CobolDec {sq} = {RuntimeApi.EditTruncateToFloatShape(pic, num.DecOperand(truncated), EditCfg(pic))};");
        return new NumX(sq, 0, Dec: true);
    }

    /// <summary>The subsidiary quotient for a BINARY floating-point identifier-3 (ISO §14.9.12.4 6) c)): the quotient as the
    /// item holds it under truncation, which is the ONE transfer of an arithmetic value into a floating-point resultant,
    /// <c>FloatResultant</c> under implied TRUNCATION (§14.7.4.3 10): "rounded to the nearest value nearer to zero that can
    /// be represented in the resultant identifier") - the very landing the item's own store performs when the statement
    /// has no ROUNDED phrase, so the subsidiary quotient and the stored one cannot disagree (kb/Work PB2923, PB1196). A
    /// native binary64 quotient stays binary64 and the remainder is formed in binary64 (DOC-A.1-123); an SDIDI quotient
    /// lands exactly and the format's value is lifted back into the SDIDI by the ONE float lift, so the remainder is the
    /// standard-decimal expression GR7 gives.</summary>
    private NumX BinaryFloatSubsidiary(NumX truncated, PicInfo pic)
    {
        var w = ctx.Writer;
        string sq = $"__sq{ctx.Names.NextStoreTmp()}";
        string held = RuntimeApi.FloatResultantStore(truncated, CobolRounding.Truncation, pic.IsSingle);
        if (truncated.Real)
        {
            w.Line($"double {sq} = {held};");
            return new NumX(sq, 0, Real: true);
        }
        w.Line($"CobolDec {sq} = {num.DecOperand(new NumX(held, 0, Real: true))};");
        return new NumX(sq, 0, Dec: true);
    }

    /// <summary>COMPUTE Format 2 — boolean-compute (ISO §14.9.8): render the boolean RHS ONCE at the §14.9.8.4
    /// GR3 width (the largest boolean ITEM referenced, carried at run time — <see cref="BooleanRenderer.RenderAtItemWidth"/>;
    /// an all-literal expression keeps its own length and the per-receiver store fits it), then store into each
    /// elementary boolean receiver with the §14.6.8.6 left-align / zero-fill / truncate discipline (the string
    /// store, pad '0'; JUSTIFIED honored). A multi-receiver COMPUTE materializes the value once (the
    /// §14.7.7-shaped once-evaluation).</summary>
    public void EmitComputeBoolean(BoundComputeBoolean cb)
    {
        string value = BooleanRenderer.RenderAtItemWidth(cb.Rhs, num);
        // One evaluation for multiple receivers (a boolean expr can read an item a prior receiver aliases).
        if (cb.Targets.Count > 1)
        {
            string tmp = $"__be{ctx.Names.NextStoreTmp()}";
            ctx.Writer.Line($"string {tmp} = {value};");
            value = tmp;
        }
        foreach (var t in cb.Targets)
        {
            // OperandPic (kb/Work PB157): a GROUP-USAGE BIT receiver's store width is its as-if PICTURE 1(m)
            // length (§13.18.29.4 GR1b) — raw Pic gave 0 and the store truncated the whole value.
            int width = t is RefModPlace ? -1 : t.Item.OperandPic?.Length ?? 0;
            // A SOLE positionless RHS — figurative ZERO, or a B-NOT fold of one (`COMPUTE B = B-NOT ZERO`,
            // reachable since kb/Work PB157 un-conflated ZERO from the ALL literal) — REPLICATES its pattern
            // to the receiver's width (§8.3.3.6.4 GR2 "repeated as necessary"); the '0'-padding store would
            // turn all-ones into 100…0.
            string store = cb.Rhs is BoundBoolAll all && width > 0
                ? EmitText.CsLiteral(ReplicateBits(all.Bits, width))
                : width < 0
                    ? value   // a ref-mod boolean receiver — the slice write fits via SpliceInto (pad '0')
                    : RuntimeApi.StrStoreBoolean(value, $"{width}", t.Item.Justified);
            ctx.Writer.Line(PlaceRenderer.Write(t, store));
        }
    }

    /// <summary>COMPUTE: the RHS is rendered per receiver (so a quotient is computed at that receiver's scale + mode)
    /// then stored, rounded by the receiver's ROUNDED mode, under the ON SIZE ERROR phrase if any.</summary>
    public void EmitCompute(BoundCompute c)
        => EmitArith(c.SizeError, ise =>
        {
            if (c.Targets.Count > 1)
            {
                // ONE initial evaluation (§14.7.7 GR4 + NOTE 3): the RHS renders ONCE — at the widest receiver
                // scale so no receiver-visible digit is lost — is materialized, and every receiver stores from the
                // temp with its own ROUNDED mode. Re-rendering per receiver would re-read senders a prior
                // receiver may alias. Real only when EVERY target is float (D16).
                var rcv = SetRcv(c.Targets, ise);
                // A PRODUCT or QUOTIENT at the root of the RHS is each receiver's FINAL TRANSFER, so it renders per
                // receiver like MULTIPLY GIVING's and DIVIDE GIVING's (NumericRenderer.Multiply's and Divide's outermost
                // arms: exact, rounded once at the receiver's scale with its mode — whatever width it is — or, for a
                // floating-point edited receiver, the round-to-odd SDIDI its significand rounds from), from operands
                // that are still evaluated ONCE. A root quotient rendered ONCE for the set (kb/Work PB2694) was
                // truncated at the widest fixed receiver's scale plus guard digits: `COMPUTE FE ROUNDED, X = 1 / HUGE`
                // stored +0.00E+00 where +3.33E-25 is owed, and any receiver's ROUNDED tie or PROHIBITED test saw a
                // truncated tail.
                if (c.Rhs is BoundBinary { Op: '*' or '/' } root)
                {
                    string rootOp = root.Op.ToString();
                    NumX ax = Snapshot(num.Render(root.Left, rcv)), bx = Snapshot(num.Render(root.Right, rcv));
                    foreach (var r in c.Targets)
                        GuardedStore(ise, r.Place, r.Rounding, () =>
                            StoreArith(r.Place, num.Combine(ax, rootOp, bx, RcvFor(r, ise), outermost: true), r.Rounding));
                    return;
                }
                // An exact WIDE value (a nested product past the Int128 carrier, kb/Work PB1900) is snapshotted AS ITSELF and
                // settled per receiver, at that receiver's scale and with its mode — the one initial evaluation keeps
                // every digit, and each receiver's ROUNDED phrase sees them. Any other value settles to itself.
                NumX v = Snapshot(num.Render(c.Rhs, rcv, keepWide: true));
                foreach (var r in c.Targets)
                    GuardedStore(ise, r.Place, r.Rounding, () =>
                        StoreArith(r.Place, num.Settle(v, RcvFor(r, ise), outermost: true), r.Rounding));
                return;
            }
            foreach (var r in c.Targets)
            {
                // Single receiver: the RHS's top-level division (if any) IS the final transfer to r — render it
                // outermost so an outermost quotient rounds at r's scale + mode and a nested quotient does not
                // inherit r's mode (CA5; §14.7.7 rule 3 NOTE 1). A receiver that identifies at run time takes the
                // value materialized first (§14.7.7 rule 4; MaterializesInitialEvaluation).
                // The materialized value is this receiver's final transfer too, so a PROHIBITED-inexact one raises while
                // it is formed, before the store: the rule-7 keep spans both (KeepsOnProhibitedInexact).
                if (MaterializesInitialEvaluation(c.Targets))
                    KeepsOnProhibitedInexact(ise, r.Rounding, () =>
                    {
                        NumX v = Snapshot(num.Render(c.Rhs, RcvFor(r, ise), outermost: true));
                        ReceiverStore(ise, r.Place, () => StoreArith(r.Place, v, r.Rounding));
                    });
                else
                    GuardedStore(ise, r.Place, r.Rounding, () =>
                        StoreArith(r.Place, num.Render(c.Rhs, RcvFor(r, ise), outermost: true), r.Rounding));
            }
        });

    /// <summary>A subscript / reference-modifier bound's pre-operation (kb/Work PB1890): the expression renders
    /// RECEIVER-LESS — no resultant scale truncates it, a nested quotient keeps its guard digits — and its value
    /// reaches the position intake on its own carrier (<see cref="NumericRenderer.IntakeCarrier"/>), which raises
    /// EC-BOUND-SUBSCRIPT / EC-BOUND-REF-MOD for a non-integer (§8.4.2.3.4 GR1b, §8.4.3.3.4 rule 5)c)) and yields the
    /// saturated integer position the integer temporary stores. It rides <see cref="EmitArith"/> with no phrase, so
    /// the expression's own size-error conditions (a zero divisor) keep the arithmetic statements' discipline.</summary>
    public void EmitPositionValue(BoundPositionValue p)
        => EmitArith(null, ise =>
        {
            var (carrier, value) = NumericRenderer.IntakeCarrier(
                num.Render(p.Value, ReceiverContext.None with { InSizeError = ise }));
            var position = new NumX(RuntimeApi.PositionValueOf(carrier, value.Expr, value.Scale, p.RefMod), 0);
            GuardedStore(ise, p.Temp, CobolRounding.Truncation, () => StoreArith(p.Temp, position, CobolRounding.Truncation));
        });

    /// <summary>The size-error catch pair (kb/Work PB129 — factored so the outer statement try and every
    /// per-receiver <see cref="GuardedStore"/> emit the identical discipline).</summary>
    private void WriteSizeCatches(string flag, string? ecnVar)
    {
        var w = ctx.Writer;
        if (ecnVar is not null)
        {
            int cid = ctx.Names.NextEc();
            w.Line($"catch (CobolSizeError __cse{cid}) {{ {flag} = true; {ecnVar} = __cse{cid}.EcName; }}");
            w.Line($"catch (global::System.OverflowException) {{ {flag} = true; {ecnVar} = \"EC-SIZE-OVERFLOW\"; }}");
        }
        else
        {
            w.Line($"catch (CobolSizeError) {{ {flag} = true; }}");
            w.Line($"catch (global::System.OverflowException) {{ {flag} = true; }}");
        }
    }

    /// <summary>Emit ONE receiver's compute+store inside its own try when size-error checking is on (kb/Work
    /// PB129): §14.7.7 r4b makes a raised size error PER-RECEIVER — "processing proceeds to the next resulting
    /// data item to the right" — and §14.7.5 storing rule 2 requires the untouched receivers to hold the values
    /// they would have had. The old single statement-wide try let one receiver's PROHIBITED/overflow raise
    /// abandon every receiver to its right. <paramref name="mode"/> is the receiver's ROUNDED mode: a PROHIBITED
    /// receiver of an unchecked statement keeps its value on an inexact transfer (<see cref="KeepsOnProhibitedInexact"/>).</summary>
    private void GuardedStore(bool inSizeError, Place receiver, CobolRounding mode, Action emitOne)
        => KeepsOnProhibitedInexact(inSizeError, mode, () => ReceiverStore(inSizeError, receiver, emitOne));

    /// <summary>One receiver's compute+store with its object-property accessors around it
    /// (<see cref="ReceiverBracketEmitter.Receive"/>), inside its own size-error try when checking is on.</summary>
    private void ReceiverStore(bool inSizeError, Place receiver, Action emitOne)
        => brackets.Receive(receiver, inSizeError ? ecState.SizeErrVar : null, () =>
        {
            if (!inSizeError) { emitOne(); return; }
            using (ctx.Writer.Block("try")) emitOne();
            WriteSizeCatches(ecState.SizeErrVar!, ecState.SizeErrEcVar);
        });

    /// <summary>⛔ §14.7.4.3 RULE 7 IN A STATEMENT WITH NO SIZE ERROR PHRASE AND CHECKING OFF (kb/Work PB3990). "If the
    /// PROHIBITED phrase is specified, and the arithmetic value cannot be represented exactly in the resultant identifier,
    /// the EC-SIZE-TRUNCATION exception condition is set to exist, the size error condition exists, and the content of the
    /// resultant identifier is unchanged": the rule keeps the resultant whether or not checking is enabled, and with
    /// checking off §14.6.13.1.3 8) leaves only whether and how execution continues to the implementor — it continues
    /// (docs/CONFORMANCE.md DOC-A.1-70, GnuCOBOL 3.2's disposition). Every landing raises
    /// <c>CobolSizeError.ProhibitedInexact</c> before it stores (StoreArith), so the receiver's compute+store — and its
    /// accessors, whose SET a size error skips as under checking — runs in a try that catches THAT condition alone and
    /// goes on to the next receiver: a zero divisor or any other size error keeps its own disposition. A statement with
    /// a SIZE ERROR phrase or EC-SIZE checking (<paramref name="inSizeError"/>) needs none: its own catch takes the raise
    /// and sets the flag. A receiver of any other mode gets nothing, so the common store is unchanged.</summary>
    private void KeepsOnProhibitedInexact(bool inSizeError, CobolRounding mode, Action emit)
    {
        if (inSizeError || mode != CobolRounding.Prohibited) { emit(); return; }
        EmitProhibitedInexactKeep(ctx.Writer, ctx.Names, emit);
    }

    /// <summary>⛔ THE ONE EMITTED SHAPE OF §14.7.4.3 RULE 7'S UNCHECKED KEEP (kb/Work PB3990): <paramref name="emit"/>'s
    /// compute-and-store inside a try whose one catch (<see cref="RuntimeApi.ProhibitedInexactKeepCatch"/>) takes
    /// <c>CobolSizeError.ProhibitedInexact</c>'s condition alone and does nothing, so the resultant keeps its content
    /// and execution continues (docs/CONFORMANCE.md DOC-A.1-70). Its two callers are the two places an implicit or
    /// explicit COMPUTE lands a PROHIBITED resultant with checking off: an arithmetic statement's receiver
    /// (<see cref="KeepsOnProhibitedInexact"/>) and a report printable item's §13.18.53.4 GR2 / §13.18.54.4 GR4
    /// implicit COMPUTE (<c>ReportWriterEmitter.FieldImage</c>, kb/Work PB3990 review finding R1-1).</summary>
    internal static void EmitProhibitedInexactKeep(CodeWriter w, NameAllocator names, Action emit)
    {
        using (w.Block("try")) emit();
        w.Line(RuntimeApi.ProhibitedInexactKeepCatch($"__cse{names.NextEc()}"));
    }

    /// <summary>The <see cref="ReceiverContext"/> a statement with several resultants renders its once-evaluated
    /// operands FOR — <see cref="ReceiverContext.OfSet"/>, the one rule (kb/Work PB2694). Each receiver's own final
    /// transfer is <see cref="RcvFor"/>.</summary>
    private static ReceiverContext SetRcv(IReadOnlyList<Receiver> targets, bool inSizeError) =>
        ReceiverContext.OfSet(targets.Select(t => t.Place.Item.Pic).ToList(), inSizeError);

    /// <summary>The <see cref="ReceiverContext"/> for receiver <paramref name="r"/> (P7 Step 3 — the pure
    /// factory replacing the mutable <c>SetTarget</c> context writes).</summary>
    /// <remarks>⛔ <c>FloatEdited</c> is carried because <see cref="ScaleOf"/> gives a FLOATING-POINT
    /// numeric-edited receiver its mask's SIGNIFICAND scale — a working-scale hint, not a scale the ROUNDED
    /// transfer rounds at (that resultant normalizes into the mask instead, D21/PB66). Only
    /// <see cref="ReceiverContext.RoundsAtItsScale"/> and <see cref="ReceiverContext.FloatLanding"/> read it
    /// (kb/Work PB647, PB1900).</remarks>
    public ReceiverContext RcvFor(Receiver r, bool inSizeError) =>
        ReceiverContext.Of(r.Place.Item.Pic, r.Rounding, inSizeError);

    /// <summary>The optional <c>blankWhenZero</c> argument text for a numeric-edited store when the receiver
    /// carries BLANK WHEN ZERO (ISO §13.18.8 — zero stores all spaces, MOVE and arithmetic alike).</summary>
    public static string BwzFlag(DataItem item) => item.BlankWhenZero ? ", blankWhenZero: true" : "";

    /// <summary>The receiver's editing-config arguments (<see cref="EmitContext.EditCfg"/> — the mask's currency
    /// string + the unit's DECIMAL-POINT IS COMMA).</summary>
    private string EditCfg(PicInfo? pic) => ctx.EditCfg(pic);

    /// <summary>⛔ IS THE INITIAL EVALUATION MATERIALIZED BEFORE THE FIRST RECEIVER IS STORED? ISO §14.7.7 rule 4 orders an
    /// arithmetic statement: a) "The initial evaluation of the statement is done and the result of this operation is placed
    /// in an intermediate data item", then b) "Item identification for the receiving data items is done as each data item is
    /// accessed unless it was already done in step a". Two things make the order observable, and either one materializes:
    /// more than one receiver (NOTE 3 — a receiver that shares storage with a sender must not change the value the receivers
    /// after it store), and a receiver that identifies at run time
    /// (<see cref="PlaceIdentification.IdentifiesAtRunTime(Place)"/>; kb/Work PB3261): a store renders as
    /// <c>&lt;receiver&gt; = &lt;value&gt;</c> and C# evaluates the receiver's address first, so
    /// <c>COMPUTE N(I) = FUNCTION NUMVAL(X(1:L))</c> range-tested I before the reference modifier, and the in-place
    /// <c>ADD FUNCTION NUMVAL(X(1:L)) TO N(I)</c> read N(I) before the operand written to its left, selecting
    /// EC-BOUND-SUBSCRIPT where §14.9.49.4 GR11 selects the first condition in the order of evaluation.</summary>
    private static bool MaterializesInitialEvaluation(IReadOnlyList<Receiver> targets) =>
        targets.Count > 1 || PlaceIdentification.IdentifiesAtRunTime(targets[0].Place);

    /// <summary>Materialize a rendered sender/initial-evaluation into a local temp (ISO §14.7.7 GR4 + NOTE 3 —
    /// ONE initial evaluation; results independent of sender/receiver storage overlap). Inlining the expression
    /// into each receiver's store would re-read its fields after earlier receivers stored.</summary>
    private NumX Snapshot(NumX v)
    {
        string t = $"__ie{ctx.Names.NextStoreTmp()}";
        // Total over the carriers (kb/Work PB85): a float sender snapshotted as `Int128 t = double` was CS0266.
        string cs = v.Wide ? "CobolWide" : v.Dec ? "CobolDec" : v.Real ? "double" : v.U ? "UInt128" : "Int128";
        ctx.Writer.Line($"{cs} {t} = {v.Expr};");
        return v with { Expr = t, WithPrecision = null };   // the snapshot is stored, never re-evaluated through another spelling
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
        using (w.Block("try")) emitStores(true);   // checked renders: AddChecked / SubChecked / the checked landings (§14.7.5)
        // A zero divisor / PROHIBITED-inexact quotient raises CobolSizeError; an intermediate that overflows the
        // long engine raises OverflowException (the checked(...) the store wraps the value in). Both are the
        // statement's size error condition (ISO §14.7.5 — the phrase ENABLES checking, incl. case 5 intermediate
        // overflow). >long-range overflow still needs the Int128 carrier (G3). This OUTER try now catches only
        // the INITIAL evaluation's raises (a statement with no formable initial value has no results to store);
        // each receiver's compute+store sits in its own GuardedStore try (kb/Work PB129 — §14.7.7 r4b:
        // "processing proceeds to the next resulting data item to the right", §14.7.5 storing rule 2).
        WriteSizeCatches(flag, ecnVar);
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
        // ⛔ A LONE FLOATING-POINT OPERAND IS AN SDIDI TOO (kb/Work PB1606). Under a standard-decimal mode every operation
        // yields the SDIDI, so a float ITEM reaches this funnel as a binary64 only when it is the whole expression
        // (`COMPUTE R = F`, `ADD F GIVING R`): §8.8.1.5.1 makes standard-decimal the method of evaluating "an arithmetic
        // expression, an arithmetic statement", and §8.8.1.5.2 r1 converts "any operand of an arithmetic expression", a
        // lone one included. It lifts through the ONE conversion (CobolDec.FromBinary64) every other float operand uses,
        // instead of landing natively where `F + 0` converted: the two statements then store one value.
        if (num.StandardDecimal && value.Real)
            value = new NumX(num.DecOperand(value), 0, Dec: true);
        // ⛔ §14.7.4.3 RULE 7 HAS NO GATE HERE, AND NEEDS NONE (kb/Work PB3990, replacing PB654's gate). "If the PROHIBITED
        // phrase is specified, and the arithmetic value cannot be represented exactly in the resultant identifier, the
        // EC-SIZE-TRUNCATION exception condition is set to exist, the size error condition exists, and the content of the
        // resultant identifier is unchanged": every landing an arm below writes asks it in its own rounding decision,
        // checked or not — a Try form answers false, any other raises CobolSizeError.ProhibitedInexact before a value
        // exists to store (CobolNum.BumpsMagnitude, CobolDec.RoundFromRemainder, FloatResultant, CobolEdit's floating
        // store). A gate written here once tested a binary64 value before the store landed it, which wrote the value
        // twice (PB654) and covered only the checked statement; the unchecked one stored the value cut. GuardedStore
        // keeps the receiver of an unchecked statement unchanged (KeepsOnProhibitedInexact). The value is read ONCE.
        //
        // A numeric-edited receiver stores the EDITED image of the result (ISO §14.7.7 — arithmetic results store
        // per the MOVE editing rules). ROUNDED applies BEFORE editing: the value is rescaled to the mask's
        // fraction scale with the receiver's mode (§14.7.4), then formatted.
        if (target.Item.Pic is { Category: PicCategory.NumericEdited, IsFloatEdited: true } fpic)
        {
            // A FLOATING-POINT numeric-edited resultant (D21/PB66): the exact result normalizes into the mask, and the
            // receiver's §14.7.4.3 mode rounds the SIGNIFICAND to the mask's digits (kb/Work PB2638) — a fixed-point
            // receiver rounds at its fraction scale, this one at its significand's last digit, by the same rules 3 to
            // 10 (rule 4: "rounded to the nearest value that can be represented in the resultant identifier"). The
            // PROHIBITED test (rule 7) is the same question asked of the same significand: TryFormatFloat answers it
            // false, and the unchecked FormatFloatStore raises it. Under ON SIZE ERROR / EC-SIZE both directions out of
            // range, and an inexact PROHIBITED, are the size error condition (§14.7.5 cases 3 AND 4 — the receiver
            // unchanged; EC-SIZE-TRUNCATION like every sibling arm); without them the MOVE disposition (overflow → the
            // pinned image, underflow → zero) stands, rounded, and an inexact PROHIBITED keeps the receiver (rule 7).
            string fcfg = BwzFlag(target.Item) + EditCfg(target.Item.Pic);
            if (ecState.SizeErrVar is { } fflag)
            {
                string fimg = $"__sv{ctx.Names.NextStoreTmp()}";
                string fOnFail = ecState.SizeErrEcVar is { } fecn ? $"{{ {fflag} = true; {fecn} = \"EC-SIZE-TRUNCATION\"; }}" : $"{fflag} = true;";
                w.Line($"if (!{RuntimeApi.EditTryFormatFloat(fpic, value, mode, fimg, fcfg)}) {fOnFail}");
                w.Line($"else {PlaceRenderer.Write(target, fimg)}");
                return;
            }
            w.Line(PlaceRenderer.Write(target, RuntimeApi.EditFormatFloat(fpic, value, mode, fcfg)));
            return;
        }
        // A format-2 (LOCALE) numeric-edited resultant (kb/Work PB64 T6): the edit is the locale's LC_MONETARY at
        // store time (§13.18.40.5 r9–r15) and the scale the PICTURE's. Under ON SIZE ERROR / EC-SIZE the digit
        // CAPACITY (the picture's integer positions) is the size error condition — EC-SIZE-TRUNCATION — while
        // EC-LOCALE-SIZE (r14 b's character truncation into the SIZE-declared item) is a DISTINCT condition the
        // edit itself raises; both can arise on one store. This arm sits BEFORE the mask arm: a locale item has
        // no EditMask, and without it the store fell to the "arithmetic into a non-fixed-point target" LoudStmt.
        if (target.Item.Pic is { LocaleEdit: not null } lpic)
        {
            int ls = ScaleOf(target);
            string lcfg = BwzFlag(target.Item);   // no EditCfg, no EditsArg — a locale item takes neither
            if (ecState.SizeErrVar is { } lflag)
            {
                string limg = $"__sv{ctx.Names.NextStoreTmp()}";
                string lOnFail = ecState.SizeErrEcVar is { } lecn ? $"{{ {lflag} = true; {lecn} = \"EC-SIZE-TRUNCATION\"; }}" : $"{lflag} = true;";
                w.Line($"if (!{RuntimeApi.EditTryFormatLocale(lpic, NumericRenderer.EditedLanding(value, ls, mode, checkedLanding: true), $"{ls}", limg, lcfg)}) {lOnFail}");
                w.Line($"else {PlaceRenderer.Write(target, limg)}");
                return;
            }
            w.Line(PlaceRenderer.Write(target, RuntimeApi.EditFormatFor(lpic, value, NumericRenderer.EditedLanding(value, ls, mode, checkedLanding: false), $"{ls}", lcfg)));
            return;
        }
        if (target.Item.Pic is { Category: PicCategory.NumericEdited, EditMask: { } mask })
        {
            // The ONE receiver-scale rule (PicInfo.ReceiverScale: the MASK's fraction scale) and the ONE edited
            // landing, NumericRenderer.EditedLanding, which the LOCALE arm above shares (kb/Work PB2163): under ON SIZE
            // ERROR / EC-SIZE it is the CHECKED landing, so a PROHIBITED-inexact narrowing (ISO §14.7.4.3 r7) and a
            // value past the carrier are size errors and the receiver stays UNCHANGED, as for every receiver category;
            // the no-phrase landing keeps the low-order digits, as the numeric store does.
            int ms = ScaleOf(target);
            // Under ON SIZE ERROR an edited resultant is capacity-checked too (ISO §14.7.5 case 3 + storing rule
            // 2): an aligned |value| exceeding the mask's digit positions sets the flag and leaves the receiver
            // UNCHANGED — Format's silent high-order truncation is MOVE behavior only (§14.9.25).
            if (ecState.SizeErrVar is { } eflag)
            {
                string img = $"__sv{ctx.Names.NextStoreTmp()}";
                // EC-SIZE checking latches the Table 13 condition: a store whose significant digits do not fit
                // the receiver is EC-SIZE-TRUNCATION ("significant digits truncated in store").
                string onFail = ecState.SizeErrEcVar is { } ecn1 ? $"{{ {eflag} = true; {ecn1} = \"EC-SIZE-TRUNCATION\"; }}" : $"{eflag} = true;";
                w.Line($"if (!{RuntimeApi.EditTryFormat(NumericRenderer.EditedLanding(value, ms, mode, checkedLanding: true), $"{ms}", CsLiteral(mask), img, BwzFlag(target.Item) + EditCfg(target.Item.Pic))}) {onFail}");
                w.Line($"else {PlaceRenderer.Write(target, img)}");
                return;
            }
            w.Line(PlaceRenderer.Write(target, RuntimeApi.EditFormat(NumericRenderer.EditedLanding(value, ms, mode, checkedLanding: false), $"{ms}", CsLiteral(mask), BwzFlag(target.Item) + EditCfg(target.Item.Pic))));
            return;
        }
        // A FLOATING-POINT resultant identifier (COMP-1/2, FLOAT-*, D16) — the float arm of THIS store, sharing the
        // fixed arm's two rules rather than restating them (kb/Work PB1196): the receiver's §14.7.4.3 mode rounds
        // the value into the binary format (rule 10's implied TRUNCATION, rules 3–9, rule 7's PROHIBITED as an
        // exactness test), and under ON SIZE ERROR / EC-SIZE checking a value further from zero than the format
        // permits is the §14.7.5 case-3 size error, receiver unchanged, EC-SIZE-TRUNCATION (no-phrase rule 4) —
        // the SAME condition name the fixed arm latches below. It used to be a bare IEEE round-to-nearest cast that
        // ignored the mode and never raised. The rounding itself is ONE runtime rule, FloatResultant.
        if (target.Item.Pic is { IsFloat: true } floatPic)
        {
            // A WINDOWED float receiver (Tier-B / image-stored — the Step D arm-1 dissolution) re-encodes the
            // landed value as its IEEE window bytes; a native receiver keeps the bare cast (the scout's pre-loaded
            // CS1503: a float expression assigned into a string window).
            string Encode(string landed)
            {
                string algebraic = $"({floatPic.ClrType})({landed})";
                return target.Item.StoreAsImage
                    ? RuntimeApi.NumFormatImageFloat(algebraic, target.Item.ProfileName, floatPic.IsSingle)
                    : algebraic;
            }
            if (ecState.SizeErrVar is { } fflag)
            {
                string tmp = $"__sv{ctx.Names.NextStoreTmp()}";
                string onFail = ecState.SizeErrEcVar is { } fecn ? $"{{ {fflag} = true; {fecn} = \"EC-SIZE-TRUNCATION\"; }}" : $"{fflag} = true;";
                w.Line($"if (!{RuntimeApi.FloatResultantStore(value, mode, floatPic.IsSingle, tmp)}) {onFail}");
                w.Line($"else {PlaceRenderer.Write(target, Encode(tmp))}");
                return;
            }
            w.Line(PlaceRenderer.Write(target, Encode(RuntimeApi.FloatResultantStore(value, mode, floatPic.IsSingle))));
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
        // The ONE carrier switch (PB84); the landing's form is the statement's (PB77) — CHECKED under ON SIZE ERROR /
        // EC-SIZE checking (the branch below), the low-order digits for the no-phrase store.
        string args = NumericRenderer.StoreArgs(value, recvScale, mode, profile, checkedLanding: ecState.SizeErrVar is not null);
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
            // ROUNDED MODE PROHIBITED and an inexact transfer is a size error that leaves the receiver UNCHANGED
            // (§14.7.4.3 rule 7 — the ROUNDED phrase's rule, not §14.7.5's): TryStore answers false for a scaled value,
            // and a float or SDIDI value's landing raises it inside the statement's catch (kb/Work PB3990).
            w.Line($"if (!{RuntimeApi.NumTryStore(args, mode, tmp, value.U)}) {onFail}");
            // On success store the value (a whole-group-aliased numeric-DISPLAY receiver stores its character image).
            w.Line($"else {PlaceRenderer.Write(target, target.Item.StoreAsImage ? RuntimeApi.NumFormatImage(tmp, profile) : Narrow(tmp, target.Item))}");
            return;
        }
        string stored = RuntimeApi.NumStoreRounded(args, mode, value.U);
        w.Line(PlaceRenderer.Write(target, target.Item.StoreAsImage ? RuntimeApi.NumFormatImage(stored, profile) : Narrow(stored, target.Item)));
    }

    /// <summary>Wrap a wide (Int128) stored value for assignment into the receiver's CARRIER type: a ≤18-digit
    /// item stores as native <c>long</c> (the value is already truncated/rounded to the receiver's digits, so the
    /// cast is exact); an unsigned 8-byte BinaryCapacity item casts to its <c>ulong</c> carrier (the stored
    /// residue is in [0, 2^64) — exact); a 16-byte UNSIGNED BinaryCapacity item reinterprets the store's
    /// container BITS as <c>UInt128</c> (<c>CobolNum.WrapBinary</c>'s documented 16-byte contract — kb/Work R10);
    /// a signed 19+-digit item (the 2002+ wide tier) stores the Int128 directly. Keyed on the CARRIER, never the
    /// digit count — the digit-count key is exactly how the F75 fold/storage disagreement shipped.</summary>
    public static string Narrow(string expr, DataItem item) => item.Pic switch
    {
        { IsUnsignedWideBinary: true } => $"unchecked((UInt128)({expr}))",
        { IsUnsignedLongBinary: true } => $"(ulong)({expr})",
        { Digits: > 18 } => expr,
        _ => $"(long)({expr})",
    };

    /// <summary>The receiver's working scale: a masked edited receiver's is its MASK's fraction scale (a `.`-pointed
    /// mask has PicInfo.Scale 0 — the point lives in the mask, not in V); a LOCALE-edited or numeric item's is its PIC
    /// scale. The edited landing of StoreArith aligns at it (kb/Work PB2163).</summary>
    private int ScaleOf(Place p) =>
        // The ONE receiver-scale rule (PicInfo.ReceiverScale; PB64 T6 — this copy and MoveEmitter's
        // SenderContext were the same rule written twice, and both fell to pic.Scale = 0 for a LOCALE item).
        p.Item.Pic is { } pic ? pic.ReceiverScale() : 0;

    /// <summary>A positionless boolean pattern repeated to <paramref name="width"/> positions and truncated
    /// on the right (ISO §8.3.3.6.4 GR2 — "repeated as necessary"); compile-time, the pattern is a bound
    /// constant (kb/Work PB157).</summary>
    private static string ReplicateBits(string bits, int width)
    {
        var sb = new System.Text.StringBuilder(width);
        while (sb.Length < width) sb.Append(bits);
        return sb.ToString(0, width);
    }
}
