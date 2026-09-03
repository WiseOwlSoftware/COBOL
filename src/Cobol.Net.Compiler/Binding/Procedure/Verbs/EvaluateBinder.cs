// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>The EVALUATE verb binder (P7 Step 10d — a real collaborator over <see cref="BinderContext"/>,
/// extracted from the <c>StatementBinder.Evaluate</c> partial; the condition/relation/expression spine is
/// reached through transitional host edges until batches 10o/10q). The <c>BoundEvaluate</c>/
/// <c>BoundEvaluateWhen</c> records stayed in <c>Binding/Bound/BoundEvaluate.cs</c> — the generated visitor
/// and <c>StatementChildren</c> key on them.</summary>
internal sealed class EvaluateBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>Bind EVALUATE (ISO §14.9.13). Subjects: TRUE/FALSE, an identifier/literal, an arithmetic
    /// expression, or an operand-with-class-test; objects per WHEN: ANY, [NOT] operand [THRU operand], or a
    /// condition (against a TRUE/FALSE subject). The subject↔object pairing is positional across ALSO (SR
    /// — the object count must equal the subject count); each pair lowers to an equality / range / condition
    /// term and the WHEN's terms AND together.</summary>
    public BoundStatement Bind(Core.EvaluateStatementContext ev)
    {
        var subjects = ev.evaluateSubject();
        var whens = new List<BoundEvaluateWhen>();
        List<BoundStatement>? other = null;

        foreach (var clause in ev.evaluateWhenClause())
        {
            var body = host.BindBlocks(clause.statementBlock());
            if (clause.OTHER() is not null)
            {
                other = body;   // WHEN OTHER — the else tail (SR: it must be last; later clauses would be dead)
                continue;
            }
            // Consecutive WHEN phrases share the body: OR their per-phrase matches (§14.9.13 — the 1985 form).
            var phraseMatches = new List<BoundCondition>();
            foreach (var phrase in clause.evaluateWhenPhrase())
            {
                var groups = phrase.evaluateWhenGroup();
                var terms = new List<BoundCondition>();
                for (int i = 0; i < groups.Length; i++)
                {
                    if (i >= subjects.Length)
                        return new BoundUnsupported("EVALUATE: more WHEN objects than subjects (ISO §14.9.13 SR)");
                    terms.Add(BindWhenGroup(subjects[i], groups[i]));
                }
                phraseMatches.Add(terms.Count == 1 ? terms[0] : new BoundLogical("&&", terms));
            }
            BoundCondition match = phraseMatches.Count == 1 ? phraseMatches[0] : new BoundLogical("||", phraseMatches);
            whens.Add(new BoundEvaluateWhen(match, body));
        }
        return new BoundEvaluate(whens, other);
    }

    /// <summary>One subject↔object pair → a boolean term (§14.9.13 GR4–7):
    /// ANY → always true; a TRUE/FALSE subject pairs with a CONDITION object (TRUE → the condition, FALSE → its
    /// negation); a value subject pairs with an operand (equality), a THRU range (inclusive bounds), or — when
    /// the object IS a condition over the same item (the grammar's escape) — the condition itself. A leading NOT
    /// on the group negates the whole term.</summary>
    private BoundCondition BindWhenGroup(Core.EvaluateSubjectContext subject, Core.EvaluateWhenGroupContext group)
    {
        // ONE selection-object per position (§14.9.13.2 general format — objects repeat only through ALSO, which is
        // this method's CALLER; §14.9.13.3 SR2 fixes the count against the subjects). The grammar enforces the arity,
        // so there is no list to fold here.
        // ⚠ This used to iterate an `evaluateWhenItem+` list and AND the terms together — a semantics with no clause
        // behind it, invented to give the unlicensed repetition a meaning. Legal source can never produce a second
        // item, so the only thing that rule ever bound was PB45's peeled misparse of
        // `WHEN FUNCTION SQRT(X) > 1` (item 1 = `FUNCTION SQRT`, item 2 = `(X) > 1`), which is exactly how a
        // function-identifier object reached the "value WHEN object" error below instead of binding as a condition.
        BoundCondition cond = BindWhenItem(subject, group.evaluateWhenItem());
        return group.NOT() is not null ? new BoundNot(cond) : cond;
    }

    /// <summary>ISO §14.9.13.3 SR10 — screen the pairing against Table 15 BEFORE binding it, so an invalid
    /// combination is a compile-time diagnostic rather than the run-time fault it used to be (fix-queue PB47).
    /// <para>⚠ DELIBERATELY CONSERVATIVE. Both classifiers return null when they cannot answer with certainty,
    /// and a null on either side means NO diagnostic: over-rejection here would turn legal source away, which is
    /// a worse failure than the wrong stage this closes. So this narrows what already failed; it does not widen
    /// what is refused. The partial-expression and boolean-expression rows of Table 15 are consequently not yet
    /// reachable — the grammar stages both (see <c>comparisonExpression</c>'s DEVLOG-621 note), so no operand
    /// classifies into them today, and the table carries them for when it does.</para></summary>
    private void ScreenPairing(Core.EvaluateSubjectContext subject, Core.EvaluateWhenItemContext item)
    {
        if (SubjectOperandKind(subject) is not { } s || ObjectOperandKind(item) is not { } o) return;
        if (EvaluateOperandCombinations.IsPermitted(s, o)) return;
        ctx.Edition.Error(DiagnosticCatalog.EvaluateOperandCombinationInvalid,
            $"the selection subject is {EvaluateOperandCombinations.Label(s)} and the selection object "
            + $"'{item.GetText()}' is {EvaluateOperandCombinations.Label(o)}; ISO §14.9.13.3 SR10 Table 15 marks "
            + "that combination invalid.");
    }

    /// <summary>The subject's Table-15 COLUMN, or null when it cannot be classified with certainty.
    /// §14.9.13.4 GR1 decides the literal-vs-expression case outright: "If an operand of the EVALUATE statement
    /// consists of a single literal, that operand is treated as a literal, not as an expression."</summary>
    private EvaluateSubjectOperand? SubjectOperandKind(Core.EvaluateSubjectContext subject)
    {
        if (subject.booleanLiteral() is not null) return EvaluateSubjectOperand.TrueOrFalse;
        if (subject.classCondition() is not null) return EvaluateSubjectOperand.Condition;  // EVALUATE X NUMERIC
        if (SubjectCondition(subject) is not null) return EvaluateSubjectOperand.Condition; // a level-88 subject
        if (subject.valueOperand() is not { } vo) return null;
        if (host.Cond.IsBooleanValueOperand(vo)) return null;   // §14.9.13.3 SR6b/d — the mirror of the object case
        return OperandKindOf(vo) switch
        {
            EvaluateObjectOperand.Identifier => EvaluateSubjectOperand.Identifier,
            EvaluateObjectOperand.Literal => EvaluateSubjectOperand.Literal,
            EvaluateObjectOperand.ArithmeticExpression => EvaluateSubjectOperand.ArithmeticExpression,
            _ => null,
        };
    }

    /// <summary>The object's Table-15 ROW, or null when it cannot be classified with certainty.</summary>
    private EvaluateObjectOperand? ObjectOperandKind(Core.EvaluateWhenItemContext item)
    {
        if (item.ANY() is not null) return EvaluateObjectOperand.Any;
        if (item.valueRange() is not null) return EvaluateObjectOperand.RangeExpression;
        if (item.condition() is { } c)
            return SoleBooleanLiteral(c) is not null
                ? EvaluateObjectOperand.TrueOrFalse : EvaluateObjectOperand.Condition;
        if (item.valueOperand() is not { } vo) return null;
        // ⛔ A BOOLEAN OPERAND IS NOT CLASSIFIABLE HERE, AND THIS SCREEN DECLINES RATHER THAN GUESS.
        // §14.9.13.3 SR6 RECLASSIFIES it by the SUBJECT and by whether it "results in one boolean character":
        // (a) against a TRUE/FALSE subject it becomes condition-2; (c) against any other subject it becomes
        // boolean-expression-2. Those land in different Table 15 rows, and the length test is not implemented.
        // ⚠ This is not hypothetical: classifying a boolean operand as a condition (which
        // BareOperandAsCondition's §8.8.4.3 arm reports) rejected the legal `EVALUATE BW WHEN B"01"` over a
        // `PIC 1(2)` item — identifier × condition is a blank cell. The wave-local gate caught it.
        if (host.Cond.IsBooleanValueOperand(vo)) return null;
        // A bare word that RESOLVES to a condition-name is condition-2 (§8.8.4.2.7 r2), not identifier-2 — the same
        // symbol-table question BindWhenItem asks below, asked once through the same helper.
        return host.Cond.BareOperandAsCondition(vo) is not null ? EvaluateObjectOperand.Condition : OperandKindOf(vo);
    }

    /// <summary>Classify a bare value operand as identifier / literal / arithmetic-expression, or null when the
    /// shape is not one of those with certainty. Shared by both sides so the subject and the object cannot drift
    /// into classifying the same text differently.</summary>
    private static EvaluateObjectOperand? OperandKindOf(Core.ValueOperandContext vo)
    {
        if (vo.nonNumericLiteral() is not null) return EvaluateObjectOperand.Literal;     // GR1
        if (vo.arithmeticExpression() is not { } expr) return null;
        if (ConditionBinder.SoleDataRef(expr) is not null) return EvaluateObjectOperand.Identifier;
        if (ConditionBinder.SoleNumLiteral(expr) is not null) return EvaluateObjectOperand.Literal;   // GR1
        return EvaluateObjectOperand.ArithmeticExpression;
    }

    private BoundCondition BindWhenItem(Core.EvaluateSubjectContext subject, Core.EvaluateWhenItemContext item)
    {
        ScreenPairing(subject, item);
        if (item.ANY() is not null) return new BoundLogical("&&", []);   // renders as true

        bool subjTrue = subject.booleanLiteral()?.TRUE_() is not null;
        bool subjFalse = subject.booleanLiteral()?.FALSE_() is not null;

        // User-function evaluation cardinality (§8.4.3.2.4 GR1/GR6a) over the EVALUATE windows:
        // — a SUBJECT is evaluated ONCE "at the beginning of the execution of the EVALUATE statement"
        //   (§14.9.13.4 GR3), but this chained-selection lowering RE-BINDS the subject expression per WHEN
        //   pair, so a once-per-statement hoist would activate a subject function once PER WHEN — staged
        //   loud (the narrowed 1509) rather than over-activating;
        // — an OBJECT is evaluated only when its WHEN phrase is considered, pairs left-to-right with a
        //   false pair stopping the phrase (GR4a–d) — its activations attach per-evaluation to the object
        //   term, and the composed &&/|| chain's C# short-circuit realizes GR4c exactly.
        int subjMark = host.Udf.PendingCount;

        // A CONDITION subject — `EVALUATE X NUMERIC WHEN TRUE …` (the subject's own class test) — pairs with
        // TRUE/FALSE objects: the term is the subject condition (or its negation).
        if (SubjectCondition(subject) is { } subjCond)
        {
            host.Udf.UdfStagePerEvaluationResidue(subjMark,
                "an EVALUATE selection subject (evaluated once per statement, §14.9.13.4 GR3 — this "
                + "lowering re-binds subjects per WHEN)");
            if (item.condition() is { } c && SoleBooleanLiteral(c) is { } objBool)
                return objBool ? subjCond : new BoundNot(subjCond);
            return new BoundConditionError($"EVALUATE condition-subject paired with non-boolean WHEN '{item.GetText()}'");
        }

        if (item.condition() is { } cond)
        {
            int objMark = host.Udf.PendingCount;
            var bound = host.Udf.UdfAttachPerEvaluation(host.Cond.BindCondition(cond), objMark);
            return subjFalse ? new BoundNot(bound) : bound;   // EVALUATE TRUE/FALSE WHEN <condition>
        }

        // §14.9.13.4 GR4a3 — "If the selection object is condition-2, the selection subject is either TRUE or
        // FALSE… If the truth value of the selection subject and selection object match, the result of the
        // analysis is true" — and §14.9.13.3 SR10 Table 15 admits ONLY a condition, TRUE/FALSE or ANY against a
        // TRUE/FALSE subject, never identifier-2 or a value.
        // ⛔ A BARE CONDITION-NAME IS condition-2 (§8.8.4.2.7 r2) BUT ARRIVES THROUGH THE valueOperand ARM, because a
        // bare word is equally an arithmeticExpression and the grammar cannot tell them apart — only the resolved
        // SYMBOL can. Without this arm the commonest EVALUATE idiom there is —
        //     EVALUATE TRUE  WHEN VALID-CODE …
        // — compiled clean and threw "value WHEN object" at RUN TIME (fix-queue PB45). Reordering
        // evaluateWhenItem's alternatives is NOT the fix: Table 15 makes the object's legality depend on the
        // SUBJECT, so putting `condition` first would retarget `EVALUATE X WHEN <88>` (a VALUE subject, where the
        // same name is an equality operand per GR4a6) in the opposite direction. The question is asked here, once,
        // through ConditionBinder.BareOperandAsCondition — the same resolution the abbreviated-relation path uses.
        if ((subjTrue || subjFalse) && item.valueOperand() is { } condObj
            && host.Cond.BareOperandAsCondition(condObj) is { } objCond)
        {
            int objMark = host.Udf.PendingCount;
            var bound = host.Udf.UdfAttachPerEvaluation(objCond, objMark);
            return subjFalse ? new BoundNot(bound) : bound;
        }

        // Value subject vs operand / range: equality or inclusive bounds (§14.9.13 GR5b/c).
        if (subject.valueOperand() is not { } subjOp)
            return new BoundConditionError("EVALUATE TRUE/FALSE paired with a value WHEN object");
        BoundOperand left = BindValueOperand(subjOp);
        host.Udf.UdfStagePerEvaluationResidue(subjMark,
            "an EVALUATE selection subject (evaluated once per statement, §14.9.13.4 GR3 — this lowering "
            + "re-binds subjects per WHEN)");

        if (item.valueRange() is { } range)
        {
            int objMark = host.Udf.PendingCount;
            var lo = BindValueOperand(range.valueOperand(0));
            var hi = BindValueOperand(range.valueOperand(1));
            // §14.7.8 rule 2: an inverted alphanumeric/national THRU range sets the nonfatal EC-RANGE-INVALID. The rule
            // is scoped to LITERAL alphanumeric/national ranges (rule 1's numeric ranges set no EC), so route only a
            // string-literal range to the ThruMember carrier under checking; everything else keeps the plain relation
            // pair (byte-identical when the directive is absent).
            if (ctx.EcState.Turn.Enabled("EC-RANGE-INVALID", null, item.Start.Line)
                && lo is BoundStringLiteral { Category: PicCategory.Alphanumeric or PicCategory.National }
                && hi is BoundStringLiteral)
                return host.Udf.UdfAttachPerEvaluation(new BoundRangeMembership(left, lo, hi, CheckInvalid: true), objMark);
            return host.Udf.UdfAttachPerEvaluation(new BoundLogical("&&",
                [host.Cond.CheckedRelational(left, ">=", lo), host.Cond.CheckedRelational(left, "<=", hi)]),
                objMark);
        }
        if (item.valueOperand() is { } v)
        {
            int objMark = host.Udf.PendingCount;
            return host.Udf.UdfAttachPerEvaluation(
                host.Cond.CheckedRelational(left, "==", BindValueOperand(v)), objMark);
        }
        return new BoundConditionError($"EVALUATE WHEN object '{item.GetText()}'");
    }

    /// <summary>The subject's own condition when a CONDITIONAL subject form is used (§14.9.13 — selection
    /// subject <c>condition-1</c>): a class test <c>X [IS] [NOT] NUMERIC</c>, or a level-88 condition-name
    /// (its membership test, resolved exactly as a condition-name condition). Else null (a value subject).</summary>
    private BoundCondition? SubjectCondition(Core.EvaluateSubjectContext subject)
    {
        if (subject.valueOperand() is not { } vo) return null;
        if (subject.classCondition() is not { } cls)
        {
            // A sole data-reference that names a level-88 IS the condition (§8.8.4.2.7 r2); the reference's
            // subscripts identify the conditional variable's occurrence (§8.4.2.3 Format 2).
            if (vo.arithmeticExpression() is not { } expr || ConditionBinder.SoleDataRef(expr) is not { } dref
                || host.Cond.ConditionOf(dref) is not { } cond) return null;
            return ctx.Refs.ResolveForItem(dref, cond.Parent) is { } parent
                ? new BoundCondition88(parent, cond,
                    ctx.EcState.Turn.Enabled("EC-RANGE-INVALID", null, dref.Start.Line))
                : new BoundConditionError($"condition-name '{cond.Name}' (unresolvable conditional variable)");
        }
        char? kind = cls.NUMERIC() is not null ? 'N'
            : cls.ALPHABETIC() is not null ? 'A'
            : cls.ALPHABETIC_UPPER() is not null ? 'U'
            : cls.ALPHABETIC_LOWER() is not null ? 'L'
            : null;
        if (kind is not { } k) return new BoundConditionError($"class condition '{cls.GetText()}'");
        var opnd = BindValueOperand(vo);
        host.Cond.CheckClassConditionOperand(opnd, k);   // §8.8.4.4.3 SR8/SR4 — boolean-operand guard
        return new BoundClassCondition(opnd, k, Negated: subject.NOT() is not null);
    }

    /// <summary>The boolean value of a condition that is a SOLE <c>TRUE</c>/<c>FALSE</c> literal, else null.</summary>
    private static bool? SoleBooleanLiteral(Core.ConditionContext cond)
    {
        Antlr4.Runtime.Tree.IParseTree n = cond;
        while (n is not Core.BooleanLiteralContext)
        {
            if (n.ChildCount != 1) return null;
            n = n.GetChild(0);
        }
        return ((Core.BooleanLiteralContext)n).TRUE_() is not null;
    }

    /// <summary>Bind a <c>valueOperand</c> (an arithmetic expression or a non-numeric literal) as a comparison
    /// operand — the same shapes <see cref="ComparisonOperand"/> produces.</summary>
    private BoundOperand BindValueOperand(Core.ValueOperandContext vo)
    {
        // ⛔ THROUGH THE ONE nonNumericLiteral MAPPING (kb/Work PB172's sweep). This was a FOURTH hand-written
        // copy of that dispatch — concatenation, figurative, STRINGLIT, NATLIT, BOOLLIT — and it was **missing
        // the HEXLIT arm**, so `EVALUATE X WHEN X"6162"` fell past all five, found no arithmeticExpression, and
        // bound to `BoundOperandError("EVALUATE operand")` → an unhandled NotImplementedCobolFeatureException at
        // RUN TIME on conforming source (§8.3.3.2.1 makes both formats of the alphanumeric literal class and
        // category alphanumeric). That is DA3's defect exactly, in the one copy DA3's extraction missed:
        // ExpressionBinder.NonNumericLiteralOperand's own remark lists the three it collapsed. Measured on
        // 9a89fbd1 before the fix, not deduced.
        if (host.Expr.NonNumericLiteralOperand(vo.nonNumericLiteral()) is { } litOp) return litOp;
        if (vo.arithmeticExpression() is { } expr)
            // ⛔ ARM FOR ARM IN THE SAME ORDER AS ConditionBinder.ComparisonOperandOf, DELIBERATELY (kb/Work
            // PB224). §14.9.13.4 GR2 makes an EVALUATE subject/object comparison "as if" the corresponding
            // relation condition were written, so the two operand binders answer ONE question and any divergence
            // between them is a latent Table-15-vs-§8.8.4.2 split. The order was data-ref → num-literal →
            // function-call here and data-ref → function-call → num-literal there; the alternatives are disjoint,
            // so the two agreed by luck, and "agrees by luck" is what this cluster keeps finding.
            return ConditionBinder.SoleDataRef(expr) is { } dref ? host.Expr.FieldOperand(dref)
                // A SOLE function-identifier is a §15.2 sending item of its own class, not an arithmetic term —
                // the same short-circuit ConditionBinder.ComparisonOperandOf makes, and for the same reason
                // (kb/Work PB172): `EVALUATE FUNCTION LOWER-CASE(X) WHEN "abc"` compares alphanumerically.
                : ConditionBinder.SoleFunctionCall(expr) is { } sfc
                    ? IntrinsicBinder.OperandOf(host.Intrinsic.BindIntrinsic(sfc))
                // A sole numeric LITERAL stays a literal operand — against an alphanumeric/group operand it
                // participates as its WRITTEN character form, leading zeros intact (ISO §8.8.4.2.1).
                : ConditionBinder.SoleNumLiteral(expr) is { } lit ? new BoundNumericLiteral(host.Expr.CheckLiteral(lit))
                // The ONE expression→operand mapping, as on the relation side: a user-function reference binds to
                // a BoundNumRef over its result temp and MUST surface as a FIELD operand so the temp's cloned
                // category (§8.4.3.2.4 GR1) drives the class dispatch; a raw BoundComputedOperand — which this
                // arm used to build — would compare an alphanumeric/national result NUMERICALLY. For every other
                // shape OperandOf returns the identical BoundComputedOperand, so the emit floor is unchanged.
                : IntrinsicBinder.OperandOf(host.Expr.BindIndexWindowExpr(expr));   // EVALUATE compares — a relation window (kb/Work R29)
        return new BoundOperandError("EVALUATE operand");
    }
}
