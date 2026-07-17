// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding.Bound;
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
        var items = group.evaluateWhenItem();
        // The grammar admits multiple items per group only for forms like `WHEN cond`-lists; the 85 surface
        // pairs ONE item per subject — additional items AND in (a faithful reading of consecutive operands).
        var terms = new List<BoundCondition>();
        foreach (var item in items)
            terms.Add(BindWhenItem(subject, item));
        BoundCondition cond = terms.Count == 1 ? terms[0] : new BoundLogical("&&", terms);
        return group.NOT() is not null ? new BoundNot(cond) : cond;
    }

    private BoundCondition BindWhenItem(Core.EvaluateSubjectContext subject, Core.EvaluateWhenItemContext item)
    {
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
            // A sole data-reference that names a level-88 IS the condition (§8.8.4.1.2); the reference's
            // subscripts identify the conditional variable's occurrence (§8.4.2.3 Format 2).
            if (vo.arithmeticExpression() is not { } expr || ConditionBinder.SoleDataRef(expr) is not { } dref
                || host.Cond.ConditionOf(dref) is not { } cond) return null;
            return ctx.Refs.ResolveForItem(dref, cond.Parent) is { } parent
                ? new BoundCondition88(parent, cond)
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
        // §8.8.3.3 GR3: a concatenation expression folds to (and selects as) the equivalent single literal.
        if (vo.nonNumericLiteral()?.concatenationExpression() is { } ce) return host.Expr.ConcatOperand(ce);
        if (vo.nonNumericLiteral()?.figurativeConstant() is { } fig) return ExpressionBinder.FigurativeOperand(fig);
        if (vo.nonNumericLiteral()?.STRINGLIT() is { } s) return new BoundStringLiteral(CobolLiteral.Decode(s.GetText()));
        if (vo.nonNumericLiteral()?.NATLIT() is { } nat) return host.Expr.NationalLiteralOperand(nat.GetText());
        if (vo.nonNumericLiteral()?.BOOLLIT() is { } bl) return host.Expr.BooleanLiteralOperand(bl.GetText());
        if (vo.arithmeticExpression() is { } expr)
            return ConditionBinder.SoleDataRef(expr) is { } dref ? host.Expr.FieldOperand(dref)
                : ConditionBinder.SoleNumLiteral(expr) is { } lit ? new BoundNumericLiteral(host.Expr.CheckLiteral(lit))
                : new BoundComputedOperand(host.Expr.BindExpr(expr));
        return new BoundOperandError("EVALUATE operand");
    }
}
