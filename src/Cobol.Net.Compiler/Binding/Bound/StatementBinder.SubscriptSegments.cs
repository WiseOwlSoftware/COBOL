// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Runtime;

namespace CobolNet.Binding.Bound;

/// <summary>
/// D18 — a FUNCTION-IDENTIFIER in a SUBSCRIPT or REFERENCE-MODIFIER position (fix-queue PB17).
///
/// <para><b>The problem.</b> <c>MOVE W-E(FUNCTION INTEGER(3)) TO W-R</c> and
/// <c>MOVE W-A(FUNCTION INTEGER(3):2) TO W-R</c> are LEGAL SOURCE that compiled clean and threw
/// <c>NotImplementedCobolFeatureException</c> at run time — the PB7/DA7 wrong-stage family. The chain, every
/// link mechanically checked: <b>§8.4.3.1.2</b> Format 1 makes a function-identifier an identifier →
/// <b>§15.4</b> "the evaluation of a function produces a returned value in a temporary elementary data item" →
/// <b>§15.2</b> items 5–6 put integer and numeric functions "of the class and category numeric" →
/// <b>§8.8.1.1</b> "an arithmetic expression may be an identifier referencing a numeric data item" →
/// <b>§8.4.2.3.2</b> + <b>§8.4.2.3.4 GR1b</b> admit <c>arithmetic-expression-1</c> as a subscript, and
/// <b>§8.4.3.3.3 SR4</b> as a leftmost-position/length.</para>
///
/// <para>⚠ <b>§8.4.3.2.3 SR11/SR12 do NOT bar it.</b> They bar functions where an <i>integer</i> or
/// <i>unsigned integer</i> is REQUIRED, and a subscript is neither: GR1b sets EC-BOUND-SUBSCRIPT when the
/// expression does not evaluate to an integer, a RUN-TIME condition that would be pointless if the position
/// required one syntactically (SR14 confirms it from the other side, having to impose that restriction
/// <i>specially</i> for a BY REFERENCE bit item).</para>
///
/// <para><b>The decision.</b> Materialize what §15.4 already describes, rather than teaching
/// <c>ReferenceResolver.RenderSegment</c> — a hand-rolled token renderer that emits C# text at BIND time — to
/// render function calls, which would make it a THIRD expression compiler beside <c>ExpressionBinder</c> and
/// <c>IntrinsicRenderer</c>. The segment's verbatim text re-parses through the isolated
/// <c>subscriptExpressionFragment</c> rule and binds through the ONE <c>ExpressionBinder.BindExpr</c> (already
/// documented as the entry "for COMPUTE, the arithmetic verbs, subscripts, reference-modifier offsets"), so a
/// USER-defined function, a nested function, and a keyword-omitted reference all fall out of the same change.
/// The value lands in a compiler temp via <c>DataBinder.CreateCompilerTemp</c> — "THE ONE synthesized-
/// compiler-temp constructor" — whose store registers as a statement-scoped pending PRE-op on the shared
/// <c>DataBinder.PendingPreOps</c>, drained at the <c>BindStatement</c> chokepoint by the mark-on-entry /
/// drain-own-suffix protocol that already served the UDF and object-property clients.</para>
///
/// <para>⛔ <b>The alternative that is FORBIDDEN, not merely worse:</b> migrating
/// <c>RefModPlace.Start</c>/<c>Length</c> to <c>BoundExpr</c>. They are the documented <b>D10 TRANSITIONAL
/// carrier</b>, deliberately the same shape as <c>RefModSpec</c> "so PHASE 15 migrates both in one move rather
/// than leaving a second, differently-shaped ref-mod behind", and D10 is an owner ruling relocated to PHASE 15
/// §"CUT 2.5", blocked while the frozen legacy compiler still shares <c>SUB_*</c>/<c>SubscriptEntryContext</c>.
/// The string carrier is deliberate sequencing, not decay — and when CUT 2.5 lands, THIS temp path is deleted
/// with it. It is a mechanism designed to be deleted, which is precisely why it must not grow a second carrier
/// in the meantime.</para>
/// </summary>
public sealed partial class StatementBinder
{
    /// <summary>The §15.4 temporary's description. §15.4 gives the returned value "a temporary elementary data
    /// item", and §15.4.1 leaves its "characteristics and representation … defined by the implementor" when
    /// native arithmetic is in effect — so this shape is a genuine implementor choice, and the ONE constraint on
    /// it is that it must not destroy the fact §8.4.2.3.4 GR1b tests.
    /// <para>⛔ <b>WHY IT CARRIES A FRACTION AT ALL.</b> The obvious choice is an integer temp — a subscript is an
    /// occurrence number, after all. But GR1b says a subscript whose expression "does not result in an integer"
    /// SETS EC-BOUND-SUBSCRIPT, and §8.4.3.3.4 item 5c says the same for a ref-mod position. A scale-0 temp
    /// TRUNCATES the value on the way in, so <c>W-E(FUNCTION SQRT(2))</c> would silently index occurrence 1
    /// instead of raising — legal source turned into a wrong answer by the temp's own description. Carrying the
    /// fraction keeps the question answerable at the position read (<c>ReferenceResolver.PositionRead</c>), which
    /// is the ONE place both this temp and an ordinary scaled data-name subscript resolve it (fix-queue PB41).</para>
    /// <para>21 integer digits × 9 fraction digits — 30 total, so the item takes the <c>Int128</c> wide tier
    /// (<c>PicInfo.IsWide</c>, &gt;18 digits) and no subscript a program can express overflows it. That matters:
    /// high-order truncation could WRAP an out-of-range subscript into an in-range one, converting a detectable
    /// error into a silent wrong answer. The temp is synthetic and never enters
    /// <c>DataBinder.ConformanceForest</c>, so the edition digit-capacity gates (18 at COBOL-85) do not apply to
    /// it — it is not a PICTURE the programmer wrote.</para></summary>
    private static readonly PicInfo SegmentTempPic =
        new(PicCategory.Numeric, Usage.Display, Length: 30, Digits: 30, Scale: 9, Signed: true);

    /// <summary>Materialize one function-bearing subscript / ref-mod segment (the D18 route; the
    /// <c>ReferenceResolver.MaterializeSegment</c> hook). Returns the §15.4 temporary the segment's value lands
    /// in, or <see langword="null"/> to leave the caller's loud posture untouched.
    ///
    /// <para><b>Ordering is why this binds HERE and not at the drain.</b> A nested segment
    /// (<c>W-E(FUNCTION INTEGER(W-F(FUNCTION INTEGER(2))))</c>) resolves its inner reference while THIS bind
    /// descends, so the inner temp's store registers on <c>PendingPreOps</c> BEFORE this one — exactly the
    /// property UdfBinder relies on ("a nested call registers while its consumer's arguments bind, so it precedes
    /// the consumer in the sequence"). Deferring the bind to the drain would append them in the wrong order.</para>
    ///
    /// <para><b>The store is a <c>BoundCompute</c>, not a bespoke node</b>, so the value crosses into the temp
    /// through the ONE arithmetic store path — scale alignment, truncation mode, and the wide tier all behave as
    /// they do for a COMPUTE the programmer wrote, and a future change to that path cannot forget this caller.
    /// No ON SIZE ERROR phrase: the temp is 30 digits wide precisely so a size error is unreachable.</para></summary>
    private DataItem? MaterializeSubscriptSegment(string text, int line)
    {
        if (Frontend.Parsing.SubscriptExpressionFragment.Parse(text, Ctx.Edition.Edition) is not { } frag)
            // A segment that does not parse as an arithmetic expression is NOT a diagnosis of the source: the
            // renderer also routes here for shapes it merely cannot render (SUB_ALL, a string literal), each of
            // which needs its own clause read before it is admitted or rejected by name. Staying null preserves
            // the pre-D18 behaviour for them exactly.
            return null;

        var value = Expr.BindExpr(frag.arithmeticExpression());
        if (value is BoundExprError) return null;   // already diagnosed by the expression binder

        // The MODEL is a description carrier only: CreateCompilerTemp clones its Pic (and, for a group, its
        // subtree) and mints the temp's own names, so the model's own names are never emitted or registered.
        var model = new DataItem { Level = 1, CobolName = "__SUBEXPR-MODEL", CsName = "__subexprModel",
                                  Pic = SegmentTempPic };
        var temp = data.CreateCompilerTemp(model, "__SUBEXPR-", "__subexpr", $"L{line}");
        if (Ctx.Refs.ResolveItem(temp) is not { } place) return null;

        data.PendingPreOps.Add(new BoundCompute(value, [new Receiver(place, CobolRounding.Truncation)], null));
        return temp;
    }
}
