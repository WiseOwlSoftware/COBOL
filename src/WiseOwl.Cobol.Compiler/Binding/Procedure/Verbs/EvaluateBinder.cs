// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>The EVALUATE verb binder (P7 Step 10d — a real collaborator over <see cref="BinderContext"/>,
/// extracted from the <c>StatementBinder.Evaluate</c> partial; the condition/relation/expression spine is
/// reached through transitional host edges until batches 10o/10q). The <c>BoundEvaluate</c>/
/// <c>BoundEvaluateWhen</c> records stayed in <c>Binding/Bound/BoundEvaluate.cs</c> — the generated visitor
/// and <c>StatementChildren</c> key on them.
/// <para>It binds the STATEMENT: the WHEN clauses and their bodies, WHEN OTHER, and the edition's rule for when an
/// object is evaluated. The selection subjects are <see cref="EvaluateSubjectBinder"/>'s (one slot per subject,
/// evaluated once, §2023:14.9.13.4 GR3) and each WHEN phrase's objects <see cref="WhenPhraseBinder"/>'s (the Table 15
/// pairing, §2023:14.9.13.3 SR10, through <see cref="EvaluateOperandCombinations"/>; kb/Work PB2338,
/// docs/rearchitecture/DESIGN-architecture-review.md §8.3).</para></summary>
internal sealed class EvaluateBinder(BinderContext ctx, StatementBinder host)
{
    private readonly EvaluateSubjectBinder _subjects = new(ctx, host);
    private readonly WhenPhraseBinder _whens = new(ctx, host);

    /// <summary>Bind EVALUATE (ISO §2023:14.9.13). Subjects: TRUE/FALSE, an identifier/literal, an arithmetic
    /// expression, or a condition (condition-1); objects per WHEN: ANY, [NOT] operand [THRU operand], or a
    /// condition (against a TRUE/FALSE subject). The subject↔object pairing is positional across ALSO (SR
    /// — the object count must equal the subject count); each pair lowers to an equality / range / condition
    /// term and the WHEN's terms AND together.</summary>
    public BoundStatement Bind(Core.EvaluateStatementContext ev)
    {
        // One slot per selection subject, bound and evaluated before any arm binds (§2023:14.9.13.4 GR3; EvaluateSubjectBinder.Bind).
        var slots = _subjects.Bind(ev);
        // ⛔ WHEN AN OBJECT IS EVALUATED IS AN EDITION'S RULE (kb/Work PB5183; DialectBehavior.EvaluateObjectsInWhenPhraseOrder).
        // From 2002 an object is evaluated when its phrase is analyzed and "no more pairs in that WHEN phrase are evaluated"
        // after a false one (§2023:14.9.13.4 GR4), which the emitted if / else-if chain and its && realize. §1985:VI.6.13.4 GR1
        // instead "operates as if each selection subject and selection object were evaluated" before GR2 compares them, so at
        // 85 a pair whose object references a function (the one object evaluation a program can observe: RANDOM's next number)
        // is analyzed at the beginning of the statement, its truth value held in the intermediate a condition subject takes
        // (SendingValueTemp.MaterializeTruth), in source order. Every other object reads values nothing in the statement
        // changes, so its place in the chain is the same evaluation.
        bool everyObjectFirst = !DialectBehaviors.IsActive(DialectBehavior.EvaluateObjectsInWhenPhraseOrder, ctx.Edition.Edition.Year);
        var whens = new List<BoundEvaluateWhen>();

        foreach (var clause in ev.evaluateWhenClause())
        {
            var body = host.BindBlocks([clause.statementBlock()]);
            // Consecutive WHEN phrases share the body: each keeps its own match, OR-ed at emission so a phrase an earlier one
            // short-circuited is never evaluated or flagged evaluated (§2023:14.9.13.4 GR4: each WHEN phrase is processed left to right; the 1985 form; USE FOR DEBUGGING rules 5 and 6).
            var phraseMatches = new List<BoundCondition>();
            foreach (var phrase in clause.evaluateWhenPhrase())
                phraseMatches.Add(_whens.Bind(phrase, slots, everyObjectFirst));
            whens.Add(new BoundEvaluateWhen(phraseMatches, body));
        }
        // `[ WHEN OTHER imperative-statement-2 ]` — ONE optional phrase, AFTER the `{ … } …` repetition
        // (§2023:14.9.13.2 + §2023:5.2.7), so the grammar now admits it once and only last. The rule used to live in a
        // COMMENT on an `other = body` assignment inside this loop, which meant a second WHEN OTHER overwrote
        // the first and the first's imperative-statement-2 became dead source, never emitted and never
        // reported; §2023:14.9.13.4 GR5b is written for exactly one such phrase and says nothing about two.
        var other = ev.evaluateWhenOther() is { } o ? host.BindBlocks([o.statementBlock()]) : null;
        return new BoundEvaluate(whens, other);
    }
}
