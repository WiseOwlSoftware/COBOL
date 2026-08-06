// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// The arithmetic-verb binder (P7 Step 10p): ADD (ISO §14.9.1) / SUBTRACT (§14.9.44) / MULTIPLY (§14.9.26) /
/// DIVIDE (§14.9.12, all five formats incl. REMAINDER SR6) / COMPUTE (§14.9.8 Format 1 + the Format-2 boolean
/// compute with the F1→F2 sole-boolean re-route — the "ANTLR alternative-order reality" precedent). The CORR
/// formats (ADD/SUBTRACT Format 3) retarget <see cref="CorrespondingBinder"/> via the host accessor. The shared
/// receiving machinery — BindSizeError/BuildSizeError, Receivers, RoundingOf, ResolveReceiving — and the
/// expression spine (BindExpr) RIDE THE HOST until 10q (ExpressionBinder), when those edges flip; the remaining
/// host edges flip at 10t. Boolean-channel edges (BindBoolExpr / Gr3Width / SoleDataRef) go to
/// <see cref="ConditionBinder"/> (statics direct, the instance via the host forwarder — 10o deviation (a)).
/// The COBOLNET1511 Format-2 SR bodies stay in-binder pending the 10t pure-lift sweep (10o deviation (b)).
/// </summary>
internal sealed class ArithmeticBinder(BinderContext ctx, StatementBinder host)
{
    public BoundStatement BindAdd(Core.AddStatementContext add)
    {
        if (add.addOperandList() is not { } operands) return host.Corr.BindAddCorresponding(add);   // Format 3 (§14.9.2.2)
        var addends = operands.addOperand().Select(host.Expr.BindExpr).ToList();
        var sizeErr = host.BindSizeError(add.arithmeticOnSizeError());
        if (add.addGivingPhrase() is { } giving)
        {
            // ADD a… [TO b] GIVING c…  →  c = (b +) Σa  (ISO §14.9.1 Format 3: the TO operand is an addend, NOT a
            // receiver; only the GIVING operands receive). Previously the TO operand was dropped from the sum.
            if (add.addToPhrase() is { } toAddend)
                addends.AddRange(StatementBinder.DataRefs(toAddend).Select(host.Expr.BindExpr));
            var givingRecv = host.Expr.Receivers(giving.receivingArithmeticOperand());
            // §14.9.2.3 SR1b: the ADD Format-2 composite is "all of the operands ... excluding the data items that
            // follow the word GIVING" — the resultant identifiers are NOT superimposed into the composite (§14.7.7
            // rule 2). Pass no receivers so a wide GIVING target does not spuriously push the composite past 31
            // (COBOLNET0805). MULTIPLY (§14.9.26.3 SR4 counts the GIVING receiver) is correctly unchanged.
            ctx.Validation.CheckComposite("ADD", addends, []);
            return new BoundAddGiving(addends, givingRecv, sizeErr);
        }
        if (add.addToPhrase() is { } to)
        {
            var recv = host.Expr.Receivers(to.receivingArithmeticOperand());
            ctx.Validation.CheckComposite("ADD", addends, recv);
            return new BoundAddTo(addends, recv, sizeErr);
        }
        return new BoundUnsupported("ADD form");
    }

    public BoundStatement BindSubtract(Core.SubtractStatementContext sub)
    {
        if (sub.subtractOperandList() is not { } operands) return host.Corr.BindSubtractCorresponding(sub);   // Format 3 (§14.9.44.2)
        var minuends = operands.subtractOperand().Select(host.Expr.BindExpr).ToList();
        var sizeErr = host.BindSizeError(sub.arithmeticOnSizeError());
        if (sub.subtractGivingPhrase() is { } giving && sub.subtractFromPhrase()?.subtractFromOperand() is { } from)
        {
            var fromX = host.Expr.BindExpr(from);
            var recv = host.Expr.Receivers(giving.receivingArithmeticOperand());
            // §14.9.44.3 SR1b: the SUBTRACT Format-2 composite excludes the data items following GIVING (§14.7.7
            // rule 2) — the resultants are not superimposed. Pass no receivers (see the ADD GIVING note above).
            ctx.Validation.CheckComposite("SUBTRACT", [.. minuends, fromX], []);
            return new BoundSubtractGiving(minuends, fromX, recv, sizeErr);
        }
        if (sub.subtractFromPhrase()?.subtractFromOperand() is { } targets)
        {
            var recv = host.Expr.Receivers(targets.receivingArithmeticOperand());
            ctx.Validation.CheckComposite("SUBTRACT", minuends, recv);
            return new BoundSubtractFrom(minuends, recv, sizeErr);
        }
        return new BoundUnsupported("SUBTRACT form");
    }

    public BoundStatement BindMultiply(Core.MultiplyStatementContext mul)
    {
        if (mul.multiplyOperand() is not { } aCtx) return new BoundUnsupported("MULTIPLY form");
        var a = host.Expr.BindExpr(aCtx);
        var byOps = mul.multiplyByOperand();
        var sizeErr = host.BindSizeError(mul.arithmeticOnSizeError());
        if (mul.multiplyGivingPhrase() is { } giving && byOps.Length > 0)
        {
            var b = host.Expr.BindExpr(byOps[0]);
            var recv = host.Expr.Receivers(giving.receivingArithmeticOperand());
            ctx.Validation.CheckComposite("MULTIPLY", [a, b], recv);
            return new BoundMultiplyGiving(a, b, recv, sizeErr);
        }
        // In-place: each BY operand is itself the receiver (target ← target × a).
        var byRecv = host.Expr.Receivers(byOps);
        ctx.Validation.CheckComposite("MULTIPLY", [a], byRecv);
        return new BoundMultiplyBy(a, byRecv, sizeErr);
    }

    public BoundStatement BindDivide(Core.DivideStatementContext div)
    {
        if (div.divideOperand() is not { } aCtx) return new BoundUnsupported("DIVIDE form");
        var a = host.Expr.BindExpr(aCtx);   // INTO: the divisor; BY: the dividend
        var sizeErr = host.BindSizeError(div.arithmeticOnSizeError());

        // DIVIDE … GIVING q REMAINDER r (ISO §14.9.12 Formats 4–5): exactly one GIVING receiver (SR6).
        if (div.divideRemainderPhrase() is { } rem)
        {
            if (div.divideGivingPhrase() is not { } g) return new BoundUnsupported("DIVIDE REMAINDER without GIVING");
            var quotients = host.Expr.Receivers(g.receivingArithmeticOperand());
            if (quotients.Count != 1) return new BoundUnsupported("DIVIDE REMAINDER quotient receiver");
            if (ctx.Refs.Resolve(rem.dataReference()) is not { } r)
                return new BoundUnsupported($"DIVIDE REMAINDER receiver '{rem.dataReference().GetText()}'");
            BoundExpr dividend = div.divideIntoPhrase() is { } i ? host.Expr.BindExpr(i.divideIntoOperand())
                : div.divideByPhrase() is not null ? a
                : a;
            BoundExpr divisor = div.divideIntoPhrase() is not null ? a
                : div.divideByPhrase() is { } b ? host.Expr.BindExpr(b.divideOperand())
                : a;
            ctx.Validation.CheckComposite("DIVIDE", [dividend, divisor], quotients);
            return new BoundDivideRemainder(dividend, divisor, quotients[0], r, sizeErr);
        }

        if (div.divideIntoPhrase() is { } into)
        {
            if (div.divideGivingPhrase() is { } giving)
            {
                var dividendX = host.Expr.BindExpr(into.divideIntoOperand());
                var recv = host.Expr.Receivers(giving.receivingArithmeticOperand());
                ctx.Validation.CheckComposite("DIVIDE", [dividendX, a], recv);
                return new BoundDivideGiving(dividendX, a, recv, sizeErr);
            }
            var intoRecv = host.Expr.Receivers(into.divideIntoOperand().receivingArithmeticOperand());
            ctx.Validation.CheckComposite("DIVIDE", [a], intoRecv);
            return new BoundDivideInto(a, intoRecv, sizeErr);   // target ← target ÷ a
        }
        if (div.divideByPhrase() is { } byPhrase && div.divideGivingPhrase() is { } gv)
        {
            var divisorX = host.Expr.BindExpr(byPhrase.divideOperand());
            var recv = host.Expr.Receivers(gv.receivingArithmeticOperand());
            ctx.Validation.CheckComposite("DIVIDE", [a, divisorX], recv);
            return new BoundDivideGiving(a, divisorX, recv, sizeErr);
        }
        return new BoundUnsupported("DIVIDE form");
    }

    public BoundStatement BindCompute(Core.ComputeStatementContext compute)
    {
        // COMPUTE Format 2 — boolean-compute (ISO §14.9.8; the {is2002()}? grammar alternative).
        if (compute.booleanExpression() is { } boolExpr) return BindComputeBoolean(compute, boolExpr);
        if (compute.arithmeticExpression() is not { } expr) return new BoundUnsupported("COMPUTE without an expression");
        // F1 → F2 re-route: `COMPUTE bool-item = bool-item` parses as Format 1 (a sole-identifier RHS predicts
        // the arithmetic alt), so a boolean receiver or a sole boolean-category RHS re-routes to the boolean
        // bind (the "ANTLR alternative-order reality" precedent). A boolean RHS/receiver never reaches the
        // numeric channel.
        bool receiverBoolean = compute.computeStore().Length > 0
            && ctx.Refs.Resolve(compute.computeStore(0).dataReference()) is { Item.Pic.Category: PicCategory.Boolean };
        bool rhsBoolean = ConditionBinder.SoleDataRef(expr) is { } d && ctx.Refs.Resolve(d) is { Item.Pic.Category: PicCategory.Boolean };
        if (receiverBoolean || rhsBoolean)
        {
            BoundBoolExpr rerouted = ConditionBinder.SoleDataRef(expr) is { } sd && ctx.Refs.Resolve(sd) is { } sp
                    && (sp is RefModPlace rm2 ? rm2.Inner.Item.Pic?.Category : sp.Item.Pic?.Category) is PicCategory.Boolean
                ? new BoundBoolRef(sp)
                : new BoundBoolError($"COMPUTE boolean receiver takes a boolean expression, not '{expr.GetText()}' "
                    + "(ISO §14.9.8 Format 2)");
            return BuildComputeBoolean(compute, rerouted);
        }
        var rhs = host.Expr.BindExpr(expr);
        return new BoundCompute(rhs, host.Expr.Receivers(compute.computeStore()), host.BindSizeError(compute.computeOnSizeError()));
    }

    /// <summary>The <c>booleanExpression</c> parse tree reduced to a BARE figurative ZERO (<c>ZERO</c>/
    /// <c>ZEROS</c>/<c>ZEROES</c>), or null for anything else — including <c>ALL ZERO</c> and <c>ALL B"…"</c>.</summary>
    /// <remarks>Read off the TREE, not the bound node: <c>BindBoolExpr</c> normalises a figurative ZERO to
    /// <c>BoundBoolAll("0")</c>, the same node <c>ALL B"0"</c> produces, so by bind time the two are
    /// indistinguishable — the loss that made <c>COMPUTE n = ZERO</c> report an ALL-literal rule (PB51).</remarks>
    private static Core.FigurativeConstantContext? SoleFigurativeZero(Core.BooleanExpressionContext? b)
    {
        if (b?.booleanXorTerm() is not [{ } xor]) return null;
        if (xor.booleanAndTerm() is not [{ } and]) return null;
        if (and.booleanShiftTerm() is not [{ } shift]) return null;
        if (shift.booleanShiftSuffix().Length != 0) return null;
        if (shift.booleanFactor() is not { } factor) return null;
        var fig = factor.valueOperand()?.nonNumericLiteral()?.figurativeConstant();
        return fig is not null && fig.ALL() is null && fig.ZERO() is not null ? fig : null;
    }

    private BoundStatement BindComputeBoolean(Core.ComputeStatementContext compute, Core.BooleanExpressionContext boolExpr)
    {
        // ⛔ F2 → F1 RE-ROUTE, THE MIRROR OF THE ONE ABOVE (fix-queue PB51). `COMPUTE <numeric> = ZERO` parsed
        // as Format 2 and was REJECTED as "a boolean COMPUTE expression shall not consist solely of an ALL
        // literal" — on a statement §8.8.1.1 makes legal arithmetic ("An arithmetic expression may be … the
        // figurative constant ZERO"), with a diagnostic naming a construct the source does not contain.
        // WHY IT LANDS HERE: a bare `ZERO` is not adjacent to an operator or paren, so ZeroTokenRewriter leaves
        // it figurative and F1's `arithmeticExpression` cannot match it — the parser takes F2, whose
        // `valueOperand` leaf admits the figurative. `BindBoolExpr` then normalises it to `BoundBoolAll("0")`
        // (§8.3.3.6.4 GR4's boolean reading), indistinguishable from `ALL B"0"` by the time SR3 runs.
        // ⚠ THE TEST IS ON THE PARSE TREE, NOT THE BOUND NODE, precisely because that normalisation is lossy:
        // `SoleFigurativeZero` asks whether the SOURCE wrote a bare ZERO/ZEROS/ZEROES, so `ALL B"0"` and
        // `ALL ZERO` keep their existing verdicts (§8.3.3.6.3 SR1a — "the only figurative constant permitted is
        // ZERO … WITHOUT the ALL phrase").
        // ⚠ AND IT IS GATED ON A NON-BOOLEAN RECEIVER: with a boolean receiver GR4's boolean reading is the right
        // one and Format 2 genuinely applies, so that path is untouched.
        // ⚙ MEASURED SCOPE: every OTHER arithmetic position already accepts a bare ZERO — ADD/SUBTRACT/MULTIPLY,
        // the GIVING forms, IF and MOVE all have their own operand rules that admit the figurative. COMPUTE is
        // the one verb whose RHS is a bare `arithmeticExpression`, which is why this is a targeted re-route and
        // not the grammar re-architecture the queue entry proposed.
        if (SoleFigurativeZero(boolExpr) is not null
            && compute.computeStore().Length > 0
            && ctx.Refs.Resolve(compute.computeStore(0).dataReference()) is not { Item.Pic.Category: PicCategory.Boolean })
        {
            // §8.3.3.6.4 GR4 — "the numeric value '0' … depending on context"; a numeric receiver IS that context.
            return new BoundCompute(new BoundNumLiteral("0"), host.Expr.Receivers(compute.computeStore()),
                host.BindSizeError(compute.computeOnSizeError()));
        }

        // The COBOL-2002 boolean-operator introduction gate on COMPUTE Format 2 (BooleanOperators2002) fires on
        // RECOGNITION in the VersionConformancePass parse-arm (VisitComputeStatement, HasBoolOp on the F2
        // booleanExpression); Step 14h.4b.
        var rhs = host.Cond.BindBoolExpr(boolExpr);
        // SR3 (§14.9.8 :26575): the expression shall not consist solely of an ALL literal.
        if (rhs is BoundBoolAll)
            ctx.Edition.Error("COBOLNET1511", "a boolean COMPUTE expression shall not consist solely of an ALL "
                + "literal (ISO §14.9.8 Format 2 SR3)");
        return BuildComputeBoolean(compute, rhs);
    }

    /// <summary>Shared tail for both the direct Format-2 bind and the F1→F2 re-route: receiver conformance
    /// (SR2 — elementary boolean), the ROUNDED / SIZE-ERROR prohibition (F2 has neither), the GR3 store width.</summary>
    private BoundStatement BuildComputeBoolean(Core.ComputeStatementContext compute, BoundBoolExpr rhs)
    {
        if (compute.computeOnSizeError() is not null)
            ctx.Edition.Error("COBOLNET1511", "ON SIZE ERROR may not be specified on a boolean COMPUTE "
                + "(ISO §14.9.8 Format 2 — no size-error phrase)");
        var targets = new List<Place>();
        foreach (var store in compute.computeStore())
        {
            if (store.roundedPhrase() is not null)
                ctx.Edition.Error("COBOLNET1511", "ROUNDED may not be specified on a boolean COMPUTE "
                    + "(ISO §14.9.8 Format 2)");
            if (ctx.Refs.Resolve(store.dataReference()) is not { } p)
            {
                ctx.Edition.Error("COBOLNET1511", $"COMPUTE receiver '{store.dataReference().GetText()}' is unresolvable");
                continue;
            }
            var cat = p is RefModPlace rm ? rm.Inner.Item.Pic?.Category : p.Item.Pic?.Category;
            if (cat is not PicCategory.Boolean)
                ctx.Edition.Error("COBOLNET1511", $"the receiver '{store.dataReference().GetText()}' of a boolean "
                    + "COMPUTE shall be an elementary boolean item (ISO §14.9.8 Format 2 SR2)");
            targets.Add(p);
        }
        return new BoundComputeBoolean(rhs, targets, ConditionBinder.Gr3Width(rhs));
    }
}
