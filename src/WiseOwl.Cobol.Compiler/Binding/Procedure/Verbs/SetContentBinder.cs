// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Frontend.Cst;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Frontend.Parsing;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions.Diagnostics;

namespace WiseOwl.Cobol.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>The SET Format 15, numeric content (ISO §2023:14.9.39.2; COBOL-2014; kb/Work PB452): one binder of the SET format families <see cref="SetBinder"/> dispatches to (kb/Work
/// PB2314; DESIGN-architecture-review §8.3, R0-0019). <see cref="SetFormatSelection"/> chose the format before this binder runs.</summary>
internal sealed class SetContentBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary><c>SET CONTENT OF { identifier-14 } … TO { FARTHEST-FROM-ZERO [IN-ARITHMETIC-RANGE] |
    /// FLOAT-INFINITY | FLOAT-NOT-A-NUMBER | FLOAT-NOT-A-NUMBER-SIGNALING | NEAREST-TO-ZERO
    /// [IN-ARITHMETIC-RANGE] } [SIGN {NEGATIVE|POSITIVE}]</c> — ISO §2023:14.9.39.2 Format 15 (numeric-content),
    /// COBOL-2014; kb/Work PB452.
    /// <para>⛔ MANDATORY BASE LANGUAGE, not an optional module: Annex A.3's processor-dependent list does not
    /// contain it and Annex A.4's ENTIRE SET inventory is three items — format 6 (A.4.2 #24), format 14
    /// (A.4.4 #3) and formats 11/12 (A.4.9 #9) — so §2023:4.2.7 cannot decline it either.</para>
    /// <para>The value is a compile-time property of EACH receiver's own data description (and, under
    /// IN-ARITHMETIC-RANGE, of the arithmetic mode), so the statement resolves here to one store per receiver —
    /// which is also why several receivers of different descriptions each get a DIFFERENT value. The extremes
    /// come from <see cref="AlgebraicRanges"/>, the same evaluator the §2023:15.43/§2023:15.58/§2023:15.83 intrinsics read:
    /// Annex D.32 states the equivalence outright ("<c>SET CONTENT OF numeric-item TO FARTHEST-FROM-ZERO</c> …
    /// the same as … <c>MOVE HIGHEST-ALGEBRAIC (numeric-item) TO numeric-item</c>"), so a second computation
    /// here would be one rule written down twice.</para>
    /// <para>SCREENS: SR31 — FARTHEST-FROM-ZERO / NEAREST-TO-ZERO take a NUMERIC data item (§2023:8.5.2.12), which a
    /// numeric-EDITED item is not (§2023:8.5.2.13), so this is NARROWER than the intrinsics' §15.x.3 r1 →
    /// COBOL1938; SR31 a) — the SIGN phrase is required when the receiver's two farthest-from-zero magnitudes
    /// differ → COBOL1939; SR32 — the three float words take a STANDARD floating-point usage (§2023:3.166/§2023:3.167,
    /// NOT COMP-1/COMP-2/FLOAT-SHORT/-LONG/-EXTENDED) → COBOL1940.</para></summary>
    internal BoundStatement BindSetContent(Core.SetContentStatementContext sc)
    {
        var v = sc.setContentValue();
        bool farthest = v.FARTHEST_FROM_ZERO() is not null;
        bool nearest = v.NEAREST_TO_ZERO() is not null;
        bool inArithmeticRange = v.IN_ARITHMETIC_RANGE() is not null;
        // GR32 c / GR36 c / GR33-GR35 all end the same way: "If the SIGN phrase is specified, the sign … is set
        // according to the SIGN specification; otherwise … positive." One reading, applied to every leg.
        bool negative = v.setContentSign()?.NEGATIVE() is not null;
        bool signWritten = v.setContentSign() is not null;

        var stores = new List<SetContentStore>();
        foreach (var dref in SetFormatSelection.Receivers(sc.setReceivingIdentifier()))
        {
            if (host.Expr.ResolveReceiving(dref) is not { } place)
                return BoundRejected.Reported(ctx.Edition);   // the receiving chokepoint reported it — not a deferral (kb/Work PB236, PB881)
            string name = place.Item.CobolName ?? WrittenSource.Of(dref);
            var pic = place.Item.Pic;

            if (farthest || nearest)
            {
                // SR31: "identifier-14 shall reference a numeric data item" — §2023:8.5.2.12's category, so an
                // EDITED item, a group, an index item and a reference-modified reference are all refused.
                if (place is RefModPlace || place.Item.IsGroup
                    || pic is not { Category: PicCategory.Numeric } || pic.Usage is Usage.Index
                    || AlgebraicRanges.Of(pic) is not { } range)
                {
                    return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.SetContentNotNumeric,
                        $"SET CONTENT OF '{name}' TO {(farthest ? "FARTHEST-FROM-ZERO" : "NEAREST-TO-ZERO")}: "
                        + "identifier-14 shall reference a numeric data item (ISO §2023:14.9.39.3 syntax rule 31)");
                }
                // SR31 a): the receiver's positive and negative extremes differ in magnitude — the two's-
                // complement containers (§2023:13.18.60.4 GR12 / DOC-A.1-208) — so "the value farthest away from zero permitted
                // by the specifications of identifier-14" (GR32 a) does not name one value, and the SIGN phrase
                // resolves it. Measured on the range itself, never on a usage list, so a future capacity
                // discipline with the same asymmetry inherits the rule.
                // ⚠ SR31 b) (the NEAREST-TO-ZERO twin) has NO violating input in this implementation and is
                // deliberately not written as a second test: the nonzero value nearest to zero is 10^(−scale)
                // for every fixed-point description and the carrier's subnormal minimum for every float one,
                // both of them MAGNITUDES that are identical in the two directions. It would become writable
                // only if AlgebraicRange gained a directional Nearest, and inventing one now would be a lookup
                // nothing reads (feedback_a_dead_lookup_is_also_unverified).
                if (farthest && !signWritten && range.FarthestNegative is { } fneg
                    && AlgebraicRanges.CompareMagnitude(range.Farthest, fneg) != 0)
                {
                    return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.SetContentSignRequired,
                        $"SET CONTENT OF '{name}' TO FARTHEST-FROM-ZERO: this item's positive and negative "
                        + $"values farthest from zero are {range.Farthest} and {fneg}, whose absolute values "
                        + "differ, so the SIGN phrase shall be specified (ISO §2023:14.9.39.3 syntax rule 31 a)");
                }
                // GR32 a / GR36 a — the extreme the receiver's own description permits, in the direction the
                // SIGN phrase selects. An UNSIGNED receiver has no negative extreme: GR32 a still yields its
                // farthest-from-zero value and GR32 c then asks for a negative sign the item cannot hold, so
                // the literal is negated and the ORDINARY store rule drops the sign — no special case here.
                string? magnitude = farthest
                    ? (negative ? range.FarthestNegative ?? "-" + range.Farthest : range.Farthest)
                    : (negative ? (range.Nearest is { } n ? "-" + n : null) : range.Nearest);
                // Null only for a FLOATING-POINT numeric-edited description, whose nearest-to-zero value is
                // deliberately unmodelled — and SR31 has already refused every edited receiver above (category
                // numeric-edited is not category numeric). CHECKED rather than asserted, so this arm can never
                // become a silent null dereference if that screen ever moves.
                if (magnitude is null)
                {
                    return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.SetContentNotNumeric,
                        $"SET CONTENT OF '{name}' TO NEAREST-TO-ZERO: identifier-14 shall reference a numeric "
                        + "data item (ISO §2023:14.9.39.3 syntax rule 31)");
                }
                string value = inArithmeticRange ? ClampToArithmeticRange(magnitude, farthest) : magnitude;
                // The store is BOUND here, not synthesized at emission: BoundMove classifies its per-target
                // dispatch at construction and the binder's storage-fact passes only see nodes that exist by
                // the end of binding (kb/Work PB348 — an emitter-built move escaped both and aborted a run).
                stores.Add(new SetContentStore(place,
                    new BoundMove(new BoundComputedOperand(new BoundNumLiteral(value)), [place]), Ieee: null));
                continue;
            }

            // SR32: FLOAT-INFINITY / FLOAT-NOT-A-NUMBER / FLOAT-NOT-A-NUMBER-SIGNALING take "a data item
            // described with a STANDARD floating-point usage" — §2023:3.166's float-binary-32/-64/-128 and §2023:3.167's
            // float-decimal-16/-34 and nothing else. The two families come from the ONE place they are written
            // down (UsageFamilies), so a future member is admitted here automatically (kb/Work PB174).
            if (place is RefModPlace || pic is not { } fpic
                || !(UsageFamilies.IsStandardBinaryFloat(fpic.Usage) || UsageFamilies.IsStandardDecimalFloat(fpic.Usage)))
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.SetContentNotStandardFloat,
                    $"SET CONTENT OF '{name}' TO {FloatWordOf(v)}: identifier-14 shall reference a data item "
                    + "described with a standard floating-point usage — FLOAT-BINARY-32/-64/-128 (ISO §2023:3.166) or "
                    + "FLOAT-DECIMAL-16/-34 (§2023:3.167). FLOAT-SHORT, FLOAT-LONG, FLOAT-EXTENDED, COMP-1 and COMP-2 "
                    + "are floating-point but not STANDARD floating-point, and carry no ISO/IEC 60559:2020 basic "
                    + "interchange format for the canonical representation to be taken from "
                    + "(ISO §2023:14.9.39.3 syntax rule 32)");
            }
            stores.Add(new SetContentStore(place, Store: null,
                Ieee: IeeeSpecialOf(v), NegativeSign: negative));
        }
        return new BoundSetContent(stores);
    }

    /// <summary>§2023:14.9.39.4 GR32b / GR36 b) — the IN-ARITHMETIC-RANGE phrase: the content is set either to the
    /// receiver's own extreme or to "the value … permitted by the specifications appropriate to the mode of
    /// arithmetic", whichever is CLOSER to zero for FARTHEST-FROM-ZERO and FARTHER from zero for NEAREST-TO-ZERO.
    /// The mode's extremes are <see cref="ArithmeticModes.IntermediateExtremes"/>; the comparison is exact
    /// scaled-BigInteger, because these magnitudes reach 10^±6176 and no CLR numeric type spans that.
    /// <para>⚠ LATENT BY MEASUREMENT, NOT BY ASSUMPTION: for every data description WiseOwl COBOL can declare today
    /// the receiver's bound wins or ties (the widest carrier is binary64, whose extremes ARE the native
    /// intermediate's, and the standard modes' SDIDI is wider still), so this method currently always returns
    /// its argument. It is written as the real comparison anyway — the clamp starts biting the moment a wider
    /// carrier lands (a true IEEE binary128 FLOAT-BINARY-128 reaches 1.19E+4932, past the native intermediate),
    /// and then it bites without anyone having to remember it.</para></summary>
    private string ClampToArithmeticRange(string value, bool farthest)
    {
        var (modeFarthest, modeNearest) = ArithmeticModes.IntermediateExtremes(ctx.Options.Arithmetic);
        string bound = farthest ? modeFarthest : modeNearest;
        int cmp = AlgebraicRanges.CompareMagnitude(value, bound);
        // FARTHEST: take the smaller magnitude. NEAREST: take the larger.
        bool takeBound = farthest ? cmp > 0 : cmp < 0;
        if (!takeBound) return value;
        return value.StartsWith('-') ? "-" + bound : bound;
    }

    /// <summary>Which of the three float value words §2023:14.9.39.2 Format 15 wrote — for the SR32 diagnostic.</summary>
    private static string FloatWordOf(Core.SetContentValueContext v) =>
        v.FLOAT_INFINITY() is not null ? "FLOAT-INFINITY"
        : v.FLOAT_NOT_A_NUMBER() is not null ? "FLOAT-NOT-A-NUMBER"
        : "FLOAT-NOT-A-NUMBER-SIGNALING";

    /// <summary>§2023:14.9.39.4 GR33/GR34/GR35 — WHICH canonical value Format 15 wrote. The parse tree is read
    /// exactly here and nowhere further in; the ENCODINGS and the Annex A.1 item 176 payload determination they
    /// stand for belong to <see cref="IeeeSpecials"/> (docs/CONFORMANCE.md §7, DOC-A.1-176), and the spelling
    /// belongs to the backend.</summary>
    private static IeeeSpecial IeeeSpecialOf(Core.SetContentValueContext v) =>
        v.FLOAT_INFINITY() is not null ? IeeeSpecial.Infinity
        : v.FLOAT_NOT_A_NUMBER() is not null ? IeeeSpecial.QuietNaN
        : IeeeSpecial.SignalingNaN;
}
