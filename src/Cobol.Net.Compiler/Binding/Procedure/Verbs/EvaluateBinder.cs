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

    /// <summary>The Table-15 coordinates of ONE selection pair, after §14.9.13.3 SR6's per-WHEN
    /// reclassification, together with each side's already-resolved bare operand — so the bind path below reads
    /// the SAME resolution the screen classified rather than resolving the symbol a second time.</summary>
    private readonly record struct EvaluatePairing(
        EvaluateSubjectOperand? Subject,
        EvaluateObjectOperand? Object,
        BareOperandAnalysis SubjectBare,
        BareOperandAnalysis ObjectBare);

    /// <summary>⛔ THE ONE CLASSIFIER for a selection pair (ISO §14.9.13.3 SR6 + SR10). Each side is named on
    /// Table 15's own axes through the SAME bare-operand classifier, and SR6 then decides a boolean operand's
    /// kind from the OTHER side of the pair, because that is what SR6 says: "The classification of some
    /// selection subjects or selection objects is changed for a particular WHEN phrase".
    /// <para>⛔ ONE FUNCTION, NOT TWO (kb/Work PB400). The subject and the object used to be classified by two
    /// independently written methods, and every difference between them was a Table-15 asymmetry: the object
    /// method recognised a switch-status condition-name and the subject method did not, so
    /// <c>EVALUATE W-ON WHEN TRUE</c> was refused as identifier × TRUE-or-FALSE while the level-88 spelling in
    /// the identical position compiled and ran. <see cref="BareOperandKind"/> is now the shared body and
    /// <c>EvaluateOperandCombinations.AsSubjectOperand</c> the row→column mapping, so the next condition form
    /// added is recognised on both sides by construction.</para>
    /// <para>GR3 a) — "If the selection subject is a numeric data item or a boolean data item whose length is
    /// one boolean position, the selection subject for this evaluation is treated as identifier-1 and not an
    /// arithmetic or boolean expression" — is a GENERAL rule scoped to that evaluation: it fixes how the
    /// subject's VALUE is obtained (the item's value, not the result of running an expression evaluator), which
    /// this compiler realizes by binding a bare boolean reference to a <c>BoundBoolRef</c> read. It does not
    /// move the operand out of Table 15's boolean-expression column, because §8.8.2 makes "an identifier
    /// referencing a boolean data item" a boolean expression and SR6 — a SYNTAX rule, explicitly "for a
    /// particular WHEN phrase" — is what classifies it. Reading it the other way would leave SR6 b) with no
    /// effect on the shape it plainly mirrors (SR6 a), which the object side has always honoured).</para>
    /// </summary>
    private EvaluatePairing ClassifyPair(Core.EvaluateSubjectContext subject, Core.EvaluateWhenItemContext item)
    {
        // The subject's valueOperand is analyzed only when it really IS a bare operand: under a class-condition
        // subject it is the class test's operand, and resolving it as a condition-name would be a symbol lookup
        // no rule asks for (and a diagnostic no rule licenses).
        var subjectBare = subject.booleanLiteral() is null && subject.classCondition() is null
            && subject.valueOperand() is { } svo ? host.Cond.AnalyzeBareOperand(svo) : default;
        var objectBare = item.valueOperand() is { } ovo ? host.Cond.AnalyzeBareOperand(ovo) : default;

        var s = SubjectKind(subject, subjectBare);
        var o = ObjectKind(item, objectBare);

        // §14.9.13.3 SR6 a) — a TRUE/FALSE subject makes a one-boolean-character boolean object condition-2.
        if (s is EvaluateSubjectOperand.TrueOrFalse && o is EvaluateObjectOperand.BooleanExpression
            && IsOneBooleanCharacter(objectBare.BooleanLength))
            o = EvaluateObjectOperand.Condition;
        // SR6 b) — its MIRROR, and the arm that did not exist: a TRUE/FALSE object makes a one-boolean-character
        // boolean SUBJECT condition-1. Without it `EVALUATE BW WHEN TRUE` bound TRUE as a standalone condition
        // and the emitted program died at run time on conforming source (kb/Work PB400).
        else if (o is EvaluateObjectOperand.TrueOrFalse && s is EvaluateSubjectOperand.BooleanExpression
            && IsOneBooleanCharacter(subjectBare.BooleanLength))
            s = EvaluateSubjectOperand.Condition;
        // SR6 c) and d) are the IDENTITY here: an operand facing a counterpart that is not TRUE or FALSE stays
        // boolean-expression-2 / boolean-expression-1, which is the kind BareOperandKind already gave it.
        // SR6 e) (a partial-expression object over a boolean/numeric data-item subject) is unreachable while
        // partial expressions are staged residue — see ScreenPairing.
        return new EvaluatePairing(s, o, subjectBare, objectBare);
    }

    /// <summary>SR6's "results in one boolean character", over the §8.8.2 rules 9/10 result length. A
    /// POSITIONLESS operand (figurative ZERO / <c>ALL B"…"</c>, whose length is its sibling's — §8.3.3.6.4 GR4)
    /// and a boolean function whose length argument is a run-time value both report null, and both answer YES:
    /// against a TRUE-or-FALSE counterpart one boolean position is the only length that gives the pair a
    /// meaning, and the §8.8.4.3 SR1 screen on the resulting condition still refuses the rest. Failing OPEN is
    /// the direction that cannot reject legal source.</summary>
    private static bool IsOneBooleanCharacter(int? length) => length is null or 1;

    /// <summary>ISO §14.9.13.3 SR10 — screen the pairing against Table 15 BEFORE binding it, so an invalid
    /// combination is a compile-time diagnostic rather than the run-time fault it used to be (kb/Work PB47).
    /// <para>⚠ The classifier still abstains — a null on either side means NO diagnostic — but only where it
    /// genuinely cannot name the shape. That is now the PARTIAL-EXPRESSION row alone: the grammar stages the
    /// partial forms (see <c>comparisonExpression</c>'s DEVLOG-621 note), so no operand classifies into that row
    /// today and the table carries it for when one does. The BOOLEAN rows are no longer abstentions — SR6 names
    /// a boolean operand's kind from the other side of the pair, and <see cref="ClassifyPair"/> implements that
    /// rule instead of declining to answer it (kb/Work PB400).</para></summary>
    private void ScreenPairing(in EvaluatePairing pair, Core.EvaluateWhenItemContext item)
    {
        if (pair.Subject is not { } s || pair.Object is not { } o) return;
        if (EvaluateOperandCombinations.IsPermitted(s, o)) return;
        ctx.Edition.Error(DiagnosticCatalog.EvaluateOperandCombinationInvalid,
            $"the selection subject is {EvaluateOperandCombinations.Label(s)} and the selection object "
            + $"'{item.GetText()}' is {EvaluateOperandCombinations.Label(o)}; ISO §14.9.13.3 SR10 Table 15 marks "
            + "that combination invalid.");
    }

    /// <summary>The subject's Table-15 COLUMN before SR6, or null when the shape cannot be named with
    /// certainty. The two subject-only forms are grammatical (TRUE/FALSE, and the subject's own class test —
    /// condition-1 per §14.9.13.4 GR3 e)); everything else is the SHARED bare-operand classification.</summary>
    private static EvaluateSubjectOperand? SubjectKind(Core.EvaluateSubjectContext subject, in BareOperandAnalysis bare)
    {
        if (subject.booleanLiteral() is not null) return EvaluateSubjectOperand.TrueOrFalse;
        if (subject.classCondition() is not null) return EvaluateSubjectOperand.Condition;  // EVALUATE X NUMERIC
        if (subject.valueOperand() is not { } vo) return null;
        return BareOperandKind(vo, bare) is { } row ? EvaluateOperandCombinations.AsSubjectOperand(row) : null;
    }

    /// <summary>The object's Table-15 ROW before SR6, or null when the shape cannot be named with certainty.
    /// The object-only forms are grammatical (ANY, a THRU range, an explicit condition — including the
    /// TRUE/FALSE spelling of one); everything else is the SHARED bare-operand classification.</summary>
    private static EvaluateObjectOperand? ObjectKind(Core.EvaluateWhenItemContext item, in BareOperandAnalysis bare)
    {
        if (item.ANY() is not null) return EvaluateObjectOperand.Any;
        if (item.valueRange() is not null) return EvaluateObjectOperand.RangeExpression;
        if (item.condition() is { } c)
            return SoleBooleanLiteral(c) is not null
                ? EvaluateObjectOperand.TrueOrFalse : EvaluateObjectOperand.Condition;
        return item.valueOperand() is { } vo ? BareOperandKind(vo, bare) : null;
    }

    /// <summary>⛔ THE ONE bare-operand classification, named on Table 15's ROW axis and mapped to the COLUMN
    /// axis for a selection subject. A bare word that RESOLVES to a level-88 (§8.8.4.2.7 r2) or a switch-status
    /// (§8.8.4.6) condition-name IS a condition, not an identifier; a boolean operand (§8.8.2) is a boolean
    /// expression until SR6 says otherwise; everything else is <see cref="OperandKindOf"/>'s syntactic
    /// question.</summary>
    private static EvaluateObjectOperand? BareOperandKind(Core.ValueOperandContext vo, in BareOperandAnalysis bare) =>
        bare.Form switch
        {
            BareOperandForm.ConditionName or BareOperandForm.SwitchStatus => EvaluateObjectOperand.Condition,
            BareOperandForm.Boolean => EvaluateObjectOperand.BooleanExpression,
            _ => OperandKindOf(vo),
        };

    /// <summary>Classify a bare value operand as identifier / literal / arithmetic-expression, or null when the
    /// shape is not one of those with certainty — §14.9.13.4 GR1's question: "If an operand of the EVALUATE
    /// statement consists of a single literal, that operand is treated as a literal, not as an expression."
    /// <c>ConditionBinder.SoleNumLiteral</c> answers the "consists of a single literal" half, sign included
    /// (§8.3.3.3.2 rule 2), which is what makes <c>-5</c> a literal and the SEPARATED <c>- 5</c> an arithmetic
    /// expression.</summary>
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
        // ⛔ CLASSIFY ONCE, THEN BIND FROM THAT CLASSIFICATION (kb/Work PB400). The screen used to run its own
        // resolution and the bind path a second, identical one — so a level-88 whose reference is ambiguous
        // reported §8.4.2.2 twice, and the two could disagree about what the operand IS with nothing to catch it.
        var pair = ClassifyPair(subject, item);
        ScreenPairing(pair, item);
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

        // A CONDITION subject (§14.9.13.4 GR3 e), Table 15's Condition column) pairs with TRUE/FALSE objects:
        // the term is the subject condition (or its negation). Four spellings reach it — the subject's own class
        // test `EVALUATE X NUMERIC`, a level-88, a switch-status condition-name, and SR6 b)'s one-boolean-
        // character boolean subject — and the CLASSIFIER decided which, so no shape can be recognised here and
        // missed by the screen (or the reverse, which is what refused `EVALUATE W-ON WHEN TRUE`).
        if (pair.Subject is EvaluateSubjectOperand.Condition && SubjectAsCondition(subject, pair) is { } subjCond)
        {
            host.Udf.UdfStagePerEvaluationResidue(subjMark,
                "an EVALUATE selection subject (evaluated once per statement, §14.9.13.4 GR3 — this "
                + "lowering re-binds subjects per WHEN)");
            // §14.9.13.4 GR4 a) 4. — a TRUE/FALSE object: "the selection subject is condition-1 … if the truth
            // value of the selection subject and selection object match, the result of the analysis is true",
            // which for a constant object IS the subject condition or its negation.
            if (item.condition() is { } c && SoleBooleanLiteral(c) is { } objBool)
                return objBool ? subjCond : new BoundNot(subjCond);
            // ⛔ TABLE 15 ALSO ADMITS Condition × Condition, AND IT IS THE SAME ANALYSIS (kb/Work PB400). The
            // Condition ROW is 'Y' under BOTH the Condition and the TRUE-or-FALSE column, and GR4 a) 3. and
            // a) 4. state the analysis in one shared sentence — "If the truth value of the selection subject
            // and selection object match, the result of the analysis is true. If they do not match, the result
            // is false" — which GR3 e) makes meaningful for a condition-1 subject too ("any selection subject
            // specified by condition-1 is assigned a truth value"). Each rule's FIRST sentence names the form
            // the other side takes in the case it was written for; neither retracts the table's cell. Without
            // this arm `EVALUATE W-A-ON WHEN W-B-ON` compiled clean and threw at RUN TIME — the PB45/PB47 shape
            // one cell over, and reachable in three more spellings now that a switch-status and a
            // one-boolean-character boolean subject classify as condition-1.
            // NOT XOR, so both truth values are evaluated exactly once: a pair is analysed as a whole (GR4 c)'s
            // short-circuit is between PAIRS, never inside one).
            if (pair.Object is EvaluateObjectOperand.Condition)
            {
                int objMark2 = host.Udf.PendingCount;
                if (ObjectAsCondition(item, pair) is { } objCond2)
                    return new BoundNot(new BoundLogical("^",
                        [subjCond, host.Udf.UdfAttachPerEvaluation(objCond2, objMark2)]));
            }
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
        // grammar reads the same name as an equality operand) in the opposite direction. The question is asked by
        // the CLASSIFIER above, once, and this arm reads its answer — SR6 a) is what routes a one-boolean-
        // character boolean object here as well.
        if (pair.Object is EvaluateObjectOperand.Condition && ObjectAsCondition(item, pair) is { } objCond)
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

    /// <summary>The selection OBJECT's condition when <see cref="ClassifyPair"/> put it in Table 15's Condition
    /// ROW: an explicit <c>condition</c> object, or the bare form (a level-88, a switch-status condition-name,
    /// or — through §14.9.13.3 SR6 a) — a one-boolean-character boolean object) that the SHARED analysis
    /// already resolved. One helper, so the two arms that need condition-2 cannot recognise different
    /// shapes.</summary>
    private BoundCondition? ObjectAsCondition(Core.EvaluateWhenItemContext item, in EvaluatePairing pair) =>
        item.condition() is { } cond ? host.Cond.BindCondition(cond) : host.Cond.AsCondition(pair.ObjectBare);

    /// <summary>The subject's own condition when <see cref="ClassifyPair"/> put it in Table 15's Condition
    /// column (§14.9.13.4 GR3 e), selection subject <c>condition-1</c>): the subject's own class test
    /// <c>X [IS] [NOT] NUMERIC</c>, or — through the SHARED bare-operand resolution the classifier already
    /// made — a level-88 condition-name (§8.8.4.2.7 r2), a switch-status condition-name (§8.8.4.6), or SR6 b)'s
    /// one-boolean-character boolean subject as a §8.8.4.3 simple boolean condition.
    /// <para>⛔ THE NON-CLASS ARMS ARE NOT RE-DERIVED HERE (kb/Work PB400). This method used to carry its own
    /// copy of the level-88 arm — the same <c>BoundCondition88</c> construction <c>BareOperandAsCondition</c>
    /// makes — and that copy is exactly why the switch-status and boolean forms existed on the object side and
    /// not on this one. One resolution, consulted twice.</para></summary>
    private BoundCondition? SubjectAsCondition(Core.EvaluateSubjectContext subject, in EvaluatePairing pair)
    {
        if (subject.valueOperand() is not { } vo) return null;
        if (subject.classCondition() is not { } cls) return host.Cond.AsCondition(pair.SubjectBare);
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
