// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Binding.Validation;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Frontend.Parsing;

namespace WiseOwl.Cobol.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// The arithmetic-verb binder (P7 Step 10p): ADD (ISO §2023:14.9.2) / SUBTRACT (§2023:14.9.44) / MULTIPLY (§2023:14.9.26) /
/// DIVIDE (§2023:14.9.12, all five formats incl. REMAINDER's single GIVING receiver, §2023:14.9.12.2 Formats 4–5) / COMPUTE (§2023:14.9.8 Format 1 + the Format-2 boolean
/// compute with the F1→F2 sole-boolean re-route — the "ANTLR alternative-order reality" precedent). The CORR
/// formats (ADD/SUBTRACT Format 3) retarget <see cref="CorrespondingBinder"/> via the host accessor. The shared
/// receiving machinery — BindSizeError/BuildSizeError, Receivers, RoundingOf, ResolveReceiving — and the
/// expression spine (BindExpr) RIDE THE HOST until 10q (ExpressionBinder), when those edges flip; the remaining
/// host edges flip at 10t. Boolean-channel edges (BindBoolExpr / IsSoleBooleanOperand /
/// BindSoleBooleanOperand) go to <see cref="ConditionBinder"/> through the host's <c>Cond</c> accessor.
/// The COBOL1511 Format-2 SR bodies stay in-binder pending the 10t pure-lift sweep (10o deviation (b)).
/// </summary>
internal sealed class ArithmeticBinder(BinderContext ctx, StatementBinder host)
{
    public BoundStatement BindAdd(Core.AddStatementContext add)
    {
        if (add.sendingOperand() is not { Length: > 0 } operands) return host.Corr.BindAddCorresponding(add);   // Format 3 (§2023:14.9.2.2)
        var addends = operands.Select(host.Expr.BindExpr).ToList();
        var sizeErr = host.BindSizeError(add.conditionalPhrases());
        var to = add.addToPhrase()?.arithmeticPhraseOperand();
        if (add.arithmeticGivingPhrase() is { } giving)
        {
            // ADD a… [TO b] GIVING c…  →  c = (b +) Σa  (ISO §2023:14.9.2.2 Format 2: the TO operand is an addend,
            // NOT a receiver — ONE `{identifier-2 | literal-2}` with no ROUNDED).
            if (to is not null) addends.Add(GivingSendingOperand("ADD", "TO", "§2023:14.9.2.2 Format 2", to));
            // The braces after the optional word TO make `{identifier-2 | literal-2}` required (§2023:5.2.6.3), so
            // Format 2 prints two addends at least: `ADD A GIVING D` is no ADD format, never D = A (kb/Work PB5203).
            if (addends.Count < 2)
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.ArithmeticFormatOperand,
                    $"ADD with GIVING writes one operand, '{DataBinder.WrittenText(operands[0])}', before GIVING: Format 2 "
                    + "prints identifier-1 or literal-1 and then identifier-2 or literal-2, the second after the optional "
                    + "word TO (ISO §2023:14.9.2.2 Format 2)");
            var givingRecv = host.Expr.Receivers(giving.receivingArithmeticOperand(), editedOk: true, "§2023:14.9.2.3 SR4");
            // §2023:14.9.2.3 SR1b: the ADD Format-2 composite is "all of the operands ... excluding the data items that
            // follow the word GIVING" — the resultant identifiers are NOT superimposed into the composite (§2023:14.7.7
            // rule 2). Pass no receivers so a wide GIVING target does not spuriously push the composite past 31
            // (COBOL0805). MULTIPLY (§2023:14.9.26.3 SR4 counts the GIVING receiver) is correctly unchanged.
            ctx.Validation.CheckComposite("ADD", addends, []);
            return new BoundAddGiving(addends, givingRecv, sizeErr);
        }
        if (to is not null)
        {
            if (!Format1Receivers("ADD", "§2023:14.9.2.2", to))
                return BoundRejected.Reported(ctx.Edition);
            var recv = host.Expr.Receivers(to, editedOk: false, "§2023:14.9.2.3 SR2");
            ctx.Validation.CheckComposite("ADD", addends, recv);
            return new BoundAddTo(addends, recv, sizeErr);
        }
        // §2023:14.9.2.2 (kb/Work PB134): no ADD format prints operands with BOTH phrases absent — `ADD A B.` is
        // illegal source, and the old BoundUnsupported staged it to a RUNTIME loud.
        return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.ArithmeticFormatOperand,
            "ADD without a TO or GIVING phrase: no ADD format prints operands alone (ISO §2023:14.9.2.2)");
    }

    public BoundStatement BindSubtract(Core.SubtractStatementContext sub)
    {
        if (sub.sendingOperand() is not { Length: > 0 } operands) return host.Corr.BindSubtractCorresponding(sub);   // Format 3 (§2023:14.9.44.2)
        var minuends = operands.Select(host.Expr.BindExpr).ToList();
        var sizeErr = host.BindSizeError(sub.conditionalPhrases());
        if (sub.subtractFromPhrase()?.arithmeticPhraseOperand() is not { } from)
            // §2023:14.9.44.2 (kb/Work PB134): every SUBTRACT format prints FROM.
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.ArithmeticFormatOperand,
                "SUBTRACT without a FROM phrase: no SUBTRACT format prints operands alone (ISO §2023:14.9.44.2)");
        if (sub.arithmeticGivingPhrase() is { } giving)
        {
            var fromX = GivingSendingOperand("SUBTRACT", "FROM", "§2023:14.9.44.2 Format 2", from);
            var recv = host.Expr.Receivers(giving.receivingArithmeticOperand(), editedOk: true, "§2023:14.9.44.3 SR4");
            // §2023:14.9.44.3 SR1b: the SUBTRACT Format-2 composite excludes the data items following GIVING (§2023:14.7.7
            // rule 2) — the resultants are not superimposed. Pass no receivers (see the ADD GIVING note above).
            ctx.Validation.CheckComposite("SUBTRACT", [.. minuends, fromX], []);
            return new BoundSubtractGiving(minuends, fromX, recv, sizeErr);
        }
        if (!Format1Receivers("SUBTRACT", "§2023:14.9.44.2", from))
            return BoundRejected.Reported(ctx.Edition);
        var targets = host.Expr.Receivers(from, editedOk: false, "§2023:14.9.44.3 SR2");
        ctx.Validation.CheckComposite("SUBTRACT", minuends, targets);
        return new BoundSubtractFrom(minuends, targets, sizeErr);
    }

    public BoundStatement BindMultiply(Core.MultiplyStatementContext mul)
    {
        if (mul.sendingOperand() is not { } aCtx)
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementFormatShape, "MULTIPLY with no multiplicand: every MULTIPLY format prints identifier-1 or "
                + "literal-1 after the verb (ISO §2023:14.9.26.2)");
        var a = host.Expr.BindExpr(aCtx);
        var byOps = mul.arithmeticPhraseOperand();
        var sizeErr = host.BindSizeError(mul.conditionalPhrases());
        if (mul.arithmeticGivingPhrase() is { } giving)
        {
            var b = GivingSendingOperand("MULTIPLY", "BY", "§2023:14.9.26.2 Format 2", byOps);
            var recv = host.Expr.Receivers(giving.receivingArithmeticOperand(), editedOk: true, "§2023:14.9.26.3 SR2");
            ctx.Validation.CheckComposite("MULTIPLY", [a, b], recv, CompositeMembers.ReceivingItems85);
            return new BoundMultiplyGiving(a, b, recv, sizeErr);
        }
        // In-place: each BY operand is itself the receiver (target ← target × a).
        if (!Format1Receivers("MULTIPLY", "§2023:14.9.26.2", byOps))
            return BoundRejected.Reported(ctx.Edition);
        var byRecv = host.Expr.Receivers(byOps, editedOk: false, "§2023:14.9.26.3 SR1");
        ctx.Validation.CheckComposite("MULTIPLY", [a], byRecv, CompositeMembers.ReceivingItems85);
        return new BoundMultiplyBy(a, byRecv, sizeErr);
    }

    public BoundStatement BindDivide(Core.DivideStatementContext div)
    {
        if (div.sendingOperand() is not { } aCtx) return DivideShape();
        var a = host.Expr.BindExpr(aCtx);   // INTO: the divisor; BY: the dividend
        var sizeErr = host.BindSizeError(div.conditionalPhrases());
        var into = div.divideIntoPhrase()?.arithmeticPhraseOperand();
        var by = div.divideByPhrase()?.arithmeticPhraseOperand();

        // DIVIDE … GIVING q REMAINDER r (ISO §2023:14.9.12 Formats 4–5): exactly one GIVING receiver (§2023:14.9.12.2 — identifier-3 carries no ellipsis in either format).
        if (div.divideRemainderPhrase() is { } rem)
        {
            if (div.arithmeticGivingPhrase() is not { } g)
            {
                // §2023:14.9.12.2 Formats 4–5 print GIVING before REMAINDER — no format has REMAINDER without it.
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.ArithmeticFormatOperand,
                    "DIVIDE REMAINDER without GIVING: Formats 4–5 print GIVING identifier-3 before "
                    + "REMAINDER (ISO §2023:14.9.12.2)");
            }
            // Format 4 prints ONE sending `INTO {identifier-2 | literal-2}` and Format 5 ONE `BY {identifier-1 |
            // literal-1}`, neither with ROUNDED: the same discipline as every GIVING form (kb/Work PB2144 — this
            // arm used to bind the phrase whole, so `INTO B C` or `INTO B ROUNDED` compiled clean).
            BoundExpr dividend, divisor;
            if (into is not null) (dividend, divisor) = (GivingSendingOperand("DIVIDE", "INTO", "§2023:14.9.12.2 Format 4", into), a);
            else if (by is not null) (dividend, divisor) = (a, GivingSendingOperand("DIVIDE", "BY", "§2023:14.9.12.2 Format 5", by));
            else return DivideShape();
            var quotients = host.Expr.Receivers(g.receivingArithmeticOperand(), editedOk: true, "§2023:14.9.12.3 SR2");
            if (quotients.Count != 1)
            {
                // §2023:14.9.12.2 Formats 4–5 print exactly ONE identifier-3. It is a GENERAL FORMAT fact and
                // there is no syntax rule to pair with it — §2023:14.9.12.3 has four (kb/Work PB388).
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.ArithmeticFormatOperand,
                    $"DIVIDE REMAINDER with {quotients.Count} GIVING receivers: Formats 4–5 print exactly one quotient receiver "
                    + "(ISO §2023:14.9.12.2)");
            }
            // kb/Work PB128: identifier-4 rides the ONE receiving chokepoint like every other resultant —
            // the direct Refs.Resolve bypass skipped the CONSTANT RECORD / CAPACITY-register / constant-name
            // screens, and §2023:14.9.12.3 SR2 fixes its category (numeric or numeric-edited). It is identified when the remainder
            // is stored (§1985:VI.6.11.4 GR4; kb/Work PB5386), like every arithmetic receiver.
            if (host.Expr.ResolveReceivingAtStore(rem.receivingIdentifier()) is not { } r0
                || host.Expr.ScreenResultant(r0, rem.receivingIdentifier(), editedOk: true, "§2023:14.9.12.3 SR2") is not { } r)
                return new BoundUnsupported($"DIVIDE REMAINDER receiver '{rem.receivingIdentifier().GetText()}'");
            ctx.Validation.CheckComposite("DIVIDE", [dividend, divisor], quotients, CompositeMembers.ReceivingItems85);
            ctx.Data.Procedure.ClaimInterleavedReceiver(r);   // the quotient was claimed with the other Receivers (ExpressionBinder.ReceiverOf)
            return new BoundDivideRemainder(dividend, divisor, quotients[0], r, sizeErr);
        }

        if (into is not null)
        {
            if (div.arithmeticGivingPhrase() is { } giving)
            {
                var dividendX = GivingSendingOperand("DIVIDE", "INTO", "§2023:14.9.12.2 Format 2", into);
                var recv = host.Expr.Receivers(giving.receivingArithmeticOperand(), editedOk: true, "§2023:14.9.12.3 SR2");
                ctx.Validation.CheckComposite("DIVIDE", [dividendX, a], recv, CompositeMembers.ReceivingItems85);
                return new BoundDivideGiving(dividendX, a, recv, sizeErr);
            }
            if (!Format1Receivers("DIVIDE", "§2023:14.9.12.2", into))
                return BoundRejected.Reported(ctx.Edition);   // a receiver-less BoundDivideInto crashed the emitter (targets.Max on empty)
            var intoRecv = host.Expr.Receivers(into, editedOk: false, "§2023:14.9.12.3 SR1");
            ctx.Validation.CheckComposite("DIVIDE", [a], intoRecv, CompositeMembers.ReceivingItems85);
            return new BoundDivideInto(a, intoRecv, sizeErr);   // target ← target ÷ a
        }
        if (by is not null && div.arithmeticGivingPhrase() is { } gv)
        {
            var divisorX = GivingSendingOperand("DIVIDE", "BY", "§2023:14.9.12.2 Format 3", by);
            var recv = host.Expr.Receivers(gv.receivingArithmeticOperand(), editedOk: true, "§2023:14.9.12.3 SR2");
            ctx.Validation.CheckComposite("DIVIDE", [a, divisorX], recv, CompositeMembers.ReceivingItems85);
            return new BoundDivideGiving(a, divisorX, recv, sizeErr);
        }
        if (by is not null)
        {
            // §2023:14.9.12.2: every BY format (3–5) prints GIVING — `DIVIDE A BY B.` exists in no format.
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.ArithmeticFormatOperand,
                "DIVIDE BY without GIVING: every BY format of DIVIDE prints the GIVING phrase "
                + "(ISO §2023:14.9.12.2 Formats 3–5)");
        }
        return DivideShape();
    }

    /// <summary>The ONE refusal for a DIVIDE the grammar parsed but no DIVIDE format prints (kb/Work PB909) —
    /// every one of the union parse's defensive arms, so they cannot drift apart.</summary>
    private BoundRejected DivideShape() => BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementFormatShape, "DIVIDE: every DIVIDE format prints "
        + "identifier-1 or literal-1 followed by an INTO or a BY phrase (ISO §2023:14.9.12.2)");

    /// <summary>The GIVING half of §2023:14.9.2.2 / §2023:14.9.44.2 / §2023:14.9.26.2 / §2023:14.9.12.2's ONE operand discipline
    /// (kb/Work PB134, PB2144): in every GIVING format — ADD, SUBTRACT and MULTIPLY Format 2, DIVIDE Formats 2–5 — the
    /// TO / FROM / BY / INTO phrase prints ONE sending `{identifier | literal}` with no ROUNDED; the repetition and the
    /// rounding belong to Format 1's receiver role and to the GIVING receivers. The grammar parses every shape at
    /// every one of these positions (<c>arithmeticPhraseOperand+</c>), so each violation is named here, for all four
    /// verbs, and never a parse error at one verb and a refusal at another. Returns the bound sending operand (the
    /// first; a statement with a violation is already refused).</summary>
    private BoundExpr GivingSendingOperand(string verb, string phrase, string format,
                                          Core.ArithmeticPhraseOperandContext[] operands)
    {
        if (operands.Length > 1)
            ctx.Edition.Error(DiagnosticCatalog.ArithmeticFormatOperand,
                $"{verb} with GIVING writes {operands.Length} {phrase} operands "
                + $"({string.Join(", ", operands.Select(o => $"'{DataBinder.WrittenText(Written(o))}'"))}): the GIVING form "
                + $"prints ONE sending operand, an identifier or a literal, after {phrase} (ISO {format})");
        foreach (var operand in operands)
            if (operand.roundedPhrase() is not null)
                ctx.Edition.Error(DiagnosticCatalog.ArithmeticFormatOperand,
                    $"{verb} with GIVING writes ROUNDED after the {phrase} operand '{DataBinder.WrittenText(Written(operand))}': "
                    + $"that operand is sending, and only the GIVING receivers take a rounded-phrase (ISO {format})");
        // The WRAPPER is bound, so the operand walk's function-call arm fires: passing a raw FunctionCallContext
        // re-springs PB45's trap (the walk descends into the ARGUMENT and drops the function; `ADD 1 TO
        // FUNCTION SQRT(9) GIVING X` computed 1 + 9).
        return host.Expr.BindExpr(operands[0]);
    }

    /// <summary>The operand an <c>arithmeticPhraseOperand</c> writes, without its ROUNDED phrase.</summary>
    private static Antlr4.Runtime.ParserRuleContext Written(Core.ArithmeticPhraseOperandContext operand) =>
        (Antlr4.Runtime.ParserRuleContext)operand.GetChild(0);

    /// <summary>The Format-1 half of the same discipline: without GIVING, the TO/FROM/BY/INTO operands are
    /// RECEIVERS — Format 1 prints <c>{identifier-2 [rounded-phrase]}</c> only — so a literal, a
    /// function-identifier (§2023:8.4.3.2.3 SR1) or an inline method invocation (§2023:8.4.3.4.3 SR1) there is illegal.
    /// ALL FOUR VERBS ask the ONE <see cref="ArithmeticOperandRole"/> classification (kb/Work PB1142): the
    /// hand-enumerated per-verb lists this replaced missed the inline-invocation arm, which dropped the operand
    /// silently (ADD / SUBTRACT / MULTIPLY) or crashed the emitter on an empty receiver list (DIVIDE). Reports
    /// every offending operand, so a MULTIPLY with two bad BY operands names both. False = rejected.</summary>
    private bool Format1Receivers(string verb, string cite, Core.ArithmeticPhraseOperandContext[] operands)
    {
        bool ok = true;
        foreach (var operand in operands)
            if (ArithmeticOperandRole.FirstNonReceiver(operand) is { } bad)
            {
                ctx.Edition.Error(DiagnosticCatalog.ArithmeticFormatOperand,
                    $"{verb} without GIVING: Format 1 prints receiving identifiers only (ISO {cite}), and "
                    + $"'{DataBinder.WrittenText(bad)}' is {ArithmeticOperandRole.Describe(bad)} — a sending operand belongs to "
                    + "the GIVING form");
                ok = false;
            }
        return ok;
    }

    public BoundStatement BindCompute(Core.ComputeStatementContext compute)
    {
        // COMPUTE Format 2 — boolean-compute (ISO §2023:14.9.8). The .g4 alternative is predicate-free; the
        // 2002 introduction gate lives in VersionConformancePass.VisitComputeStatement (GateBooleanOperators
        // — kb/Work PB157 corrected the stale {is2002()}? claim here).
        if (compute.booleanExpression() is { } boolExpr) return BindComputeBoolean(compute, boolExpr);
        if (compute.arithmeticExpression() is not { } expr)
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementFormatShape, "COMPUTE with no expression after '=': both COMPUTE formats print an "
                + "arithmetic or a boolean expression (ISO §2023:14.9.8.2)");
        // F1 → F2 re-route: `COMPUTE bool-item = bool-item` parses as Format 1 (a sole-identifier RHS predicts
        // the arithmetic alt), so a boolean receiver or a sole boolean-category RHS re-routes to the boolean
        // bind (the "ANTLR alternative-order reality" precedent). A boolean RHS/receiver never reaches the
        // numeric channel.
        // OperandPic, BOTH probes, over EVERY receiver (kb/Work PB157): a GROUP-USAGE BIT receiver has Pic
        // null but IS an elementary boolean for these rules (§2023:13.18.29.4 GR1b — OperandPic carries the as-if
        // PICTURE 1(m)), and probing only computeStore(0) let `COMPUTE N B = 1` miss the boolean receiver
        // (the reroute then judged it a pure Format 1).
        bool receiverBoolean = compute.computeStore()
            .Any(s => s.receivingIdentifier().dataReference() is { } r
                && ctx.Refs.Probe(r) is { Item.OperandPic.Category: PicCategory.Boolean });
        // The sole right-hand operand is classified and bound by the ONE reader the boolean expression's own leaf
        // uses (ConditionBinder.IsSoleBooleanOperand / BindSoleBooleanOperand: a category-boolean data reference, a
        // boolean constant-name or a boolean function-identifier, each through enclosing parentheses, §2023:8.8.2; kb/Work
        // PB3565). This arm used to read an unparenthesized data reference only, so `COMPUTE B8 = FUNCTION
        // BOOLEAN-OF-INTEGER(5, 8)`, `COMPUTE B8 = (C8)` and `COMPUTE B8 = BOOLEAN-CONSTANT` were refused.
        bool rhsBoolean = host.Cond.IsSoleBooleanOperand(expr);
        if (receiverBoolean || rhsBoolean)
        {
            // Not a sole identifier (`COMPUTE B = N + 1`): the PB68 sweep's sixth site (kb/Work PB157) — this arm
            // built the error NODE without the DIAGNOSTIC, so the statement compiled clean and threw at run time.
            var rerouted = host.Cond.BindSoleBooleanOperand(expr) ?? RefuseNonBooleanComputeOperand(expr);
            return BuildComputeBoolean(compute, rerouted);
        }
        var rhs = host.Expr.BindExpr(expr);
        return new BoundCompute(rhs, host.Expr.Receivers(compute.computeStore()), host.BindSizeError(compute.conditionalPhrases()));
    }

    /// <summary>The refusal of a boolean COMPUTE whose right-hand operand is no boolean expression (ISO §2023:14.9.8
    /// Format 2 / §2023:8.8.2): the diagnostic AND the error node, so the statement never compiles clean.</summary>
    private BoundBoolExpr RefuseNonBooleanComputeOperand(Core.ArithmeticExpressionContext expr)
    {
        ctx.Edition.Error(DiagnosticCatalog.BooleanExpressionRule, $"a boolean COMPUTE receiver takes a boolean expression, "
            + $"not '{DataBinder.WrittenText(expr)}' (ISO §2023:14.9.8 Format 2 / §2023:8.8.2)");
        return BoundBoolError.Refused(ctx.Edition, $"COMPUTE boolean receiver takes a boolean expression, not '{DataBinder.WrittenText(expr)}' "
            + "(ISO §2023:14.9.8 Format 2)");
    }

    private BoundStatement BindComputeBoolean(Core.ComputeStatementContext compute, Core.BooleanExpressionContext boolExpr)
    {
        // ⛔ F2 → F1 RE-ROUTE, THE MIRROR OF THE ONE ABOVE (fix-queue PB51). `COMPUTE <numeric> = ZERO` parsed
        // as Format 2 and was REJECTED as "a boolean COMPUTE expression shall not consist solely of an ALL
        // literal" — on a statement §2023:8.8.1.1 makes legal arithmetic ("An arithmetic expression may be … the
        // figurative constant ZERO"), with a diagnostic naming a construct the source does not contain.
        // WHY IT LANDS HERE: a bare `ZERO` is not adjacent to an operator or paren, so ZeroTokenRewriter leaves
        // it figurative and F1's `arithmeticExpression` cannot match it — the parser takes F2, whose
        // `valueOperand` leaf admits the figurative. `BindBoolExpr` then normalises it to `BoundBoolAll("0")`
        // (§2023:8.3.3.6.4 GR4's boolean reading), indistinguishable from `ALL B"0"` by the time SR3 runs.
        // ⚠ THE TEST IS ON THE PARSE TREE, NOT THE BOUND NODE, precisely because that normalisation is lossy:
        // `ComputeFormats.SoleFigurativeZero` asks whether the SOURCE wrote a sole figurative ZERO, so `ALL B"0"`
        // keeps its Format-2 verdict. It is the ONE reading: §2023:14.5.3.3's phrase association asks it too, so the SIZE
        // ERROR pair a Format-1 reading admits was associated with this COMPUTE at parse (kb/Work PB3830 review
        // R2-1). The operand is then read by the ONE arithmetic-operand reading (ComputeZeroOperand), so `ALL ZERO`
        // meets §2023:8.3.3.6.3 SR1 a)'s edition gate exactly as `ADD ALL ZERO TO X` does ("the only figurative constant
        // permitted is ZERO (ZEROS, ZEROES) without the ALL phrase": COBOL0902 from 2002; kb/Work PB1415) — the
        // re-route built a bare zero and compiled `COMPUTE X = ALL ZERO` clean at 2023.
        // ⚠ AND IT IS GATED ON A NON-BOOLEAN RECEIVER: with a boolean receiver GR4's boolean reading is the right
        // one and Format 2 genuinely applies, so that path is untouched.
        // ⚙ MEASURED SCOPE: every OTHER arithmetic position already accepts a bare ZERO — ADD/SUBTRACT/MULTIPLY,
        // the GIVING forms, IF and MOVE all have their own operand rules that admit the figurative. COMPUTE is
        // the one verb whose RHS is a bare `arithmeticExpression`, which is why this is a targeted re-route and
        // not the grammar re-architecture the queue entry proposed.
        if (ComputeFormats.SoleFigurativeZero(boolExpr) is { } zero
            && compute.computeStore().Length > 0
            // OperandPic (kb/Work PB157): a GROUP-USAGE BIT receiver is boolean (§2023:13.18.29.4 GR1b) — raw Pic
            // read it as non-boolean and misrouted `COMPUTE <bit-group> = ZERO` to the arithmetic channel.
            && (compute.computeStore(0).receivingIdentifier().dataReference() is not { } r0
                || ctx.Refs.Probe(r0) is not { Item.OperandPic.Category: PicCategory.Boolean }))
        {
            // §2023:8.3.3.6.4 GR4 — "the numeric value '0' … depending on context"; a numeric receiver IS that context.
            return new BoundCompute(host.Expr.ComputeZeroOperand(zero), host.Expr.Receivers(compute.computeStore()),
                host.BindSizeError(compute.conditionalPhrases()));
        }

        // The COBOL-2002 boolean-operator introduction gate on COMPUTE Format 2 (BooleanOperators2002) fires on
        // RECOGNITION in the VersionConformancePass parse-arm (VisitComputeStatement, HasBoolOp on the F2
        // booleanExpression); Step 14h.4b.
        var rhs = host.Cond.BindBoolExpr(boolExpr);
        // SR3 (§2023:14.9.8 :26575): the expression shall not consist solely of THE FIGURATIVE CONSTANT ALL
        // LITERAL — Format 6, the only construction that sets BoundBoolAll.IsAllLiteral. Figurative ZERO —
        // bare or `ALL ZERO`, whose ALL is Format 1's OPTIONAL word (§2023:8.3.3.6.2) — is a DISJOINT §2023:8.8.2
        // operand alternative and legal here, as is a B-NOT fold (the expression is then not "solely" the
        // literal). The old bound-node test conflated all three with the ALL literal and rejected them
        // (kb/Work PB157 — the PB51 reading, decided by the spec; §2023:8.3.3.6.3 SR2 excludes figuratives from
        // Format 6's literal-1).
        if (rhs is BoundBoolAll { IsAllLiteral: true })
            ctx.Edition.Error(DiagnosticCatalog.BooleanExpressionRule, "a boolean COMPUTE expression shall not consist solely of an ALL "
                + "literal (ISO §2023:14.9.8.3 Format 2 SR3)");
        return BuildComputeBoolean(compute, rhs);
    }

    /// <summary>Shared tail for both the direct Format-2 bind and the F1→F2 re-route: receiver conformance
    /// (SR2 — elementary boolean), the ROUNDED / SIZE-ERROR prohibition (F2 has neither), the GR3 store width. The
    /// SIZE ERROR arm is reached only by a right side BOTH formats print — a sole identifier, or a sole figurative
    /// ZERO (<see cref="ComputeFormats"/>) — that a boolean receiver makes Format 2: §2023:14.5.3.3 associates the phrase
    /// with such a COMPUTE, since it may be Format 1. A right side only Format 2 prints never owns the phrase
    /// (ConditionalPhraseAssociation's BooleanCompute selector; COBOL2269 at the phrase, kb/Work PB3830).</summary>
    private BoundStatement BuildComputeBoolean(Core.ComputeStatementContext compute, BoundBoolExpr rhs)
    {
        if (ConditionalPhrases.HasFamily(compute.conditionalPhrases(), ConditionalPhraseFamily.SizeError))
            ctx.Edition.Error(DiagnosticCatalog.BooleanExpressionRule, "ON SIZE ERROR may not be specified on a boolean COMPUTE "
                + "(ISO §2023:14.9.8 Format 2 — no size-error phrase)");
        var targets = new List<Place>();
        foreach (var store in compute.computeStore())
        {
            if (store.roundedPhrase() is not null)
                ctx.Edition.Error(DiagnosticCatalog.BooleanExpressionRule, "ROUNDED may not be specified on a boolean COMPUTE "
                    + "(ISO §2023:14.9.8 Format 2)");
            // kb/Work PB128: the Format-2 boolean receivers ride the ONE receiving chokepoint (the direct
            // Refs.Resolve bypass skipped the CONSTANT RECORD / CAPACITY / constant-name screens); the
            // ARITHMETIC category screen deliberately does not apply — §2023:14.9.8.3 SR2's boolean-receiver rule
            // is BuildComputeBoolean's own (kb/Work PB157).
            // Null is REPORTED by the chokepoint (an undefined name, a non-receivable identifier form, a constant): a
            // second "is unresolvable" here contradicted the rule it had just named.
            if (host.Expr.ResolveReceiving(store.receivingIdentifier()) is not { } p) continue;
            // OperandPic — THE ONE category reader (D20/PB79; kb/Work PB157): a GROUP-USAGE BIT receiver IS
            // an elementary boolean for SR2 (§2023:13.18.29.4 GR1b's as-if PICTURE 1(m)); raw Pic rejected it
            // while the SENDING side of the same statement accepted it.
            // (kb/Work PB173 LANDED: the PB157 containment that staged a reference-modified BIT-GROUP receiver
            // loud is DELETED, together with DiagnosticCatalog.RefModBitGroupSlice. Its premise — "the boolean
            // channel sizes in BOOLEAN positions while the group ref-mod substrate slices the PACKED BYTE
            // image" — was a true description of a SUBSTRATE defect, and the substrate is now a BitImagePlace
            // over the unpacked boolean string (§2023:8.4.3.3.4 GR5a). The units agree, so the receiver is an
            // ordinary boolean one and needs no exclusion.)
            var cat = p is RefModPlace rm ? rm.Category : p.Item.OperandPic?.Category;
            if (cat is not PicCategory.Boolean)
                ctx.Edition.Error(DiagnosticCatalog.BooleanExpressionRule, $"the receiver '{store.receivingIdentifier().GetText()}' of a boolean "
                    + "COMPUTE shall be an elementary boolean item (ISO §2023:14.9.8.3 Format 2 SR2)");
            targets.Add(p);
        }
        return new BoundComputeBoolean(rhs, targets);
    }
}
