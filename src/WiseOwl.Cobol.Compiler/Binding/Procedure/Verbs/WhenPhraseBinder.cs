// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Frontend.Parsing;

namespace WiseOwl.Cobol.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>One EVALUATE WHEN phrase (kb/Work PB2338; docs/rearchitecture/DESIGN-architecture-review.md §8.3): its
/// selection objects paired positionally with the statement's subject slots (§2023:14.9.13.3 SR2), each pair
/// classified on Table 15's axes after SR6 and screened by SR10 through <see cref="EvaluateOperandCombinations"/>, then
/// lowered to a boolean term (§2023:14.9.13.4 GR4); the phrase's match is the terms AND-ed together. A subject is read
/// only through its <see cref="EvaluateSubjectBinder.SubjectSlot"/>, the one evaluation GR3 assigns it.</summary>
internal sealed class WhenPhraseBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>Bind one WHEN phrase against the statement's subject slots: the SR2 count screen, one term per pair, the
    /// surplus objects of a count breach bound standalone; the terms AND together.</summary>
    /// <param name="everyObjectFirst">The edition evaluates an object referencing a function at the beginning of the
    /// statement (§1985:VI.6.13.4 GR1; kb/Work PB5183), so its truth value is held in an intermediate.</param>
    public BoundCondition Bind(Core.EvaluateWhenPhraseContext phrase, EvaluateSubjectBinder.SubjectSlot[] slots, bool everyObjectFirst)
    {
        var objects = phrase.evaluateWhenItem();
        ScreenObjectCount(phrase, objects.Length, slots.Length);
        var terms = new List<BoundCondition>();
        // ⛔ THE COUNT IS A SYNTAX RULE, SCREENED ABOVE — this clamp is pure RECOVERY (kb/Work PB399).
        // It used to be `if (i >= subjects.Length) return new BoundUnsupported(…)`: a defensive index
        // guard wearing a rule's clothes, which delivered §2023:14.9.13.3 SR2 as a run-time abort in the
        // MORE-objects direction and not at all in the FEWER-objects direction, where the loop simply
        // ran out of written objects and the surplus SUBJECTS were never paired with anything — illegal
        // source silently branching on a strict subset of its own selection subjects. With the screen
        // above, an over- or under-long phrase has already failed the compile; binding the pairs the
        // programmer DID write keeps the rest of the statement's diagnostics flowing instead of
        // truncating the bind at the first bad phrase.
        // ⛔ …AND THE SURPLUS OBJECTS ARE STILL THE PROGRAM'S OWN OPERANDS (kb/Work PB4446). SR2 says the
        // COUNT is wrong; it says nothing about an object that has no subject to pair with, whose reference
        // faults (§2023:8.4.2.1, §2023:8.4.2.3.3 SR2 …) are independent syntax-rule violations that §2023:4.2.2 owes an
        // indication, so each is bound standalone by its own Table 15 row and contributes no term.
        int paired = System.Math.Min(objects.Length, slots.Length);
        for (int i = 0; i < paired; i++)
        {
            var term = BindSelectionObject(slots[i], objects[i]);
            terms.Add(everyObjectFirst && ReferencesAFunction(objects[i]) ? host.SendingValue.MaterializeTruth(term, "evaluate") : term);
        }
        for (int i = paired; i < objects.Length; i++)
            BindSurplusObject(objects[i]);
        return terms.Count == 1 ? terms[0] : new BoundLogical("&&", terms);
    }

    /// <summary>Whether a selection object references a function anywhere in its operands (subscripts and reference modifiers
    /// included): the evaluation whose time a program can observe (kb/Work PB5183).</summary>
    private static bool ReferencesAFunction(Core.EvaluateWhenItemContext item) =>
        Antlr4.Runtime.Tree.Trees.FindAllRuleNodes(item, Core.RULE_functionCall).Count > 0;

    /// <summary>One subject↔object pair → a boolean term (§2023:14.9.13.4 GR4–7):
    /// ANY → always true; a TRUE/FALSE subject pairs with a CONDITION object (TRUE → the condition, FALSE → its
    /// negation); a value subject pairs with an operand (equality), a THRU range (inclusive bounds), or — when
    /// the object IS a condition over the same item (the grammar's escape) — the condition itself. The bracketed
    /// NOT of §2023:14.9.13.2's five operand alternatives negates the whole term (GR4 a) 5. and 6. — "selection-subject
    /// [NOT] = selection-object"); a condition-2 or partial-expression-1 carries its own NOT inside its parse.</summary>
    private BoundCondition BindSelectionObject(EvaluateSubjectBinder.SubjectSlot subject, Core.EvaluateWhenItemContext item)
    {
        // ONE selection-object per position (§2023:14.9.13.2 general format — objects repeat only through ALSO, which is
        // this method's CALLER; §2023:14.9.13.3 SR2 fixes the count against the subjects). The grammar enforces the arity,
        // so there is no list to fold here.
        // ⚠ This used to iterate an `evaluateWhenItem+` list and AND the terms together — a semantics with no clause
        // behind it, invented to give the unlicensed repetition a meaning. Legal source can never produce a second
        // item, so the only thing that rule ever bound was PB45's peeled misparse of
        // `WHEN FUNCTION SQRT(X) > 1` (item 1 = `FUNCTION SQRT`, item 2 = `(X) > 1`), which is exactly how a
        // function-identifier object reached the "value WHEN object" error below instead of binding as a condition.
        if (!ScreenObjectNegation(item)) return Constant(false);
        BoundCondition cond = BindWhenItem(subject, item);
        return item.NOT() is not null ? new BoundNot(cond) : cond;
    }

    /// <summary>⛔ ISO §2023:14.9.13.2 prints the bracketed NOT on five selection-object alternatives only — the figure
    /// note: "The bracketed `NOT` applies only to the first five alternatives, exactly as stacked" (identifier-2,
    /// literal-2, arithmetic-expression-2, boolean-expression-2, range-expression) — so a NOT before ANY, TRUE or
    /// FALSE is source no format admits, and §2023:4.2.2 owes it a compile-time indication ("to indicate violations of the
    /// general formats", kb/Work PB2722). The grammar parses the
    /// two words with a NOT only so this screen can NAME the mistake; before it, `WHEN NOT ANY` compiled clean and
    /// bound to the negation of GR4 a) 1.'s constant true, a WHEN arm that could never run. condition-2 and
    /// partial-expression-1 are not asked: their NOT is inside their own parse (§2023:8.8.4.10.1's negated condition; the
    /// `NOT >` operator or class / sign condition SR5 names), never this one.
    /// <para>Returns false when the object has been refused: the caller binds nothing more of it, because Table 15
    /// would only read the same mistake a second time.</para></summary>
    private bool ScreenObjectNegation(Core.EvaluateWhenItemContext item)
    {
        if (item.NOT() is not { } negation) return true;
        string? word = item.ANY() is not null ? "ANY"
            : item.booleanLiteral() is { } literal ? (literal.TRUE_() is not null ? "TRUE" : "FALSE")
            : null;
        if (word is null) return true;
        using var _ = ctx.Edition.At(negation.Symbol);
        ctx.Edition.Error(DiagnosticCatalog.EvaluateObjectNegationNotPermitted,
            $"the selection object '{WrittenSource.Of(item)}' writes NOT before the word {word}; "
            + ctx.Edition.Edition.Cite(
                "§1985:VI.6.13.2 admits the bracketed NOT only before identifier-3, literal-3 and arithmetic-expression-1, "
                + $"with the THROUGH phrase that ends their range, never before {word}",
                "ISO §2023:14.9.13.2 admits the bracketed NOT only before identifier-2, literal-2, arithmetic-expression-2, "
                + $"boolean-expression-2 and a range-expression, never before {word}"));
        return false;
    }

    /// <summary>⛔ AN OBJECT WITH NO SUBJECT TO PAIR WITH (kb/Work PB4446): the written objects past the subject count of
    /// a phrase that breaks §2023:14.9.13.3 SR2. <see cref="ScreenObjectCount"/> has reported the count, so the object has
    /// no pair, no Table 15 verdict and no term; its operands are bound standalone
    /// (<see cref="BindObjectOperandsStandalone"/>) so their own faults are reported.</summary>
    private void BindSurplusObject(Core.EvaluateWhenItemContext item)
    {
        if (!ScreenObjectNegation(item)) return;
        var mark = host.Window.Mark;
        BindObjectOperandsStandalone(item, AnalyzeObjectBare(item), mark);
    }

    /// <summary>⛔ THE OPERANDS OF A SELECTION OBJECT THAT HAS NO PAIR TO BE BOUND AS (kb/Work PB4446) — the object of a
    /// pair Table 15 (§2023:14.9.13.3 SR10) refused, and a surplus object of an SR2 breach. The refusal, or the missing
    /// subject, ends the PAIR's checks: the SR7 a) relation checkpoint, a TRUE/FALSE word bound as a condition (the
    /// COBOL2318 cascade <see cref="ScreenPairing"/> exists to prevent), and a partial-expression's splice of a subject
    /// there is none of. It does not end the operand's own reference and expression faults — an undefined or ambiguous
    /// name (§2023:8.4.2.1), a subscript on a scalar (§2023:8.4.2.3.3 SR2), an unsubscripted table element (SR5), a bad
    /// reference modification, an alphanumeric operand in an arithmetic expression (§2023:8.8.1.1) or a bad function
    /// argument — which are violations of the general formats and syntax rules whatever the object is paired with, and
    /// §2023:4.2.2 requires the warning mechanism "to indicate" each. The compile has failed, so the result is discarded
    /// and the activations the operands registered are drained with it, never left in the statement's scope.
    /// <para>The dispatch is on the object's own ROW, one arm per kind <see cref="ObjectKind"/> names, and
    /// each arm binds exactly what the pair's own arm in <see cref="BindWhenItem"/> would have bound of that object.</para></summary>
    /// <param name="bare">The object's one bare-operand resolution (<see cref="AnalyzeObjectBare"/>): an operand it
    /// already bound (a condition-name, a switch-status, a boolean expression) is not bound a second time.</param>
    /// <param name="mark">The user-function window opened before that resolution.</param>
    private void BindObjectOperandsStandalone(Core.EvaluateWhenItemContext item, in BareOperandAnalysis bare, OperandEvaluationWindow.PendingMark mark)
    {
        if (item.valueRange() is { } range) _ = BindRangeObject(range);
        else if (item.addressIdentifier() is { } address) _ = host.Ptr.AddressIdentifierOperand(address, "an EVALUATE selection object");
        else if (item.partialExpression() is { } partial) host.Cond.BindPartialOperandsStandalone(partial);
        else if (item.condition() is { } cond)
        {
            if (host.Cond.LeadingBareClassWord(cond) is not null) host.Cond.BindPartialOperandsStandalone(cond);
            else _ = host.Cond.BindCondition(cond);
        }
        // ANY, TRUE and FALSE have no operand; an operator-bearing boolean expression was bound by the resolution.
        else if (BooleanOperatorBearing(item) is null && BareOperand(item) is { } value && bare.Form is BareOperandForm.Value)
            _ = EvaluateOperands.BindValueOperand(host, value);
        _ = host.Window.DrainPerEvaluation(mark);
    }

    /// <summary>The Table-15 coordinates of ONE selection pair, after §2023:14.9.13.3 SR6's per-WHEN
    /// reclassification, together with each side's already-resolved bare operand — so the bind path below reads
    /// the SAME resolution the screen classified rather than resolving the symbol a second time.</summary>
    private readonly record struct EvaluatePairing(
        EvaluateSubjectOperand? Subject,
        EvaluateObjectOperand? Object,
        BareOperandAnalysis SubjectBare,
        BareOperandAnalysis ObjectBare);

    /// <summary>⛔ THE ONE CLASSIFIER for a selection pair (ISO §2023:14.9.13.3 SR6 + SR10). Each side is named on
    /// Table 15's own axes through the SAME bare-operand classifier, and SR6 then decides a boolean operand's
    /// kind from the OTHER side of the pair, because that is what SR6 says: "The classification of some
    /// selection subjects or selection objects is changed for a particular WHEN phrase".
    /// <para>⛔ ONE FUNCTION, NOT TWO (kb/Work PB400). The subject and the object used to be classified by two
    /// independently written methods, and every difference between them was a Table-15 asymmetry: the object
    /// method recognised a switch-status condition-name and the subject method did not, so
    /// <c>EVALUATE W-ON WHEN TRUE</c> was refused as identifier × TRUE-or-FALSE while the level-88 spelling in
    /// the identical position compiled and ran. <see cref="EvaluateOperands.BareOperandKind"/> is now the shared body and
    /// <c>EvaluateOperandCombinations.AsSubjectOperand</c> the row→column mapping, so the next condition form
    /// added is recognised on both sides by construction.</para>
    /// <para>GR3 a) — "If the selection subject is a numeric data item or a boolean data item whose length is
    /// one boolean position, the selection subject for this evaluation is treated as identifier-1 and not an
    /// arithmetic or boolean expression" — is a GENERAL rule scoped to that evaluation: it fixes how the
    /// subject's VALUE is obtained (the item's value, not the result of running an expression evaluator), which
    /// this compiler realizes by binding a bare boolean reference to a <c>BoundBoolRef</c> read. It does not
    /// move the operand out of Table 15's boolean-expression column, because §2023:8.8.2 makes "an identifier
    /// referencing a boolean data item" a boolean expression and SR6 — a SYNTAX rule, explicitly "for a
    /// particular WHEN phrase" — is what classifies it. Reading it the other way would leave SR6 b) with no
    /// effect on the shape it plainly mirrors (SR6 a), which the object side has always honoured).</para>
    /// </summary>
    private EvaluatePairing ClassifyPair(EvaluateSubjectBinder.SubjectSlot slot, Core.EvaluateWhenItemContext item)
    {
        // The subject's bare-operand analysis is the SLOT's — resolved once for the statement, not once per pair
        // (kb/Work PB400's "one resolution, consulted twice", now consulted N times from one resolution).
        var subjectBare = slot.Bare;
        var objectBare = AnalyzeObjectBare(item);

        var s = slot.Kind;
        var o = ObjectKind(item, objectBare);

        // §2023:14.9.13.3 SR6a — a TRUE/FALSE subject makes a one-boolean-character boolean object condition-2.
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
        // SR6 e) — "If the selection object is a partial expression and the selection subject is a data item of
        // the class boolean or numeric, the selection subject is treated as an identifier." It runs AFTER a)–d)
        // because it is the one arm keyed on the OBJECT being a partial expression, and it OVERRIDES d): a
        // one-boolean-character boolean data item facing a non-TRUE/FALSE object is boolean-expression-1 by d),
        // and e) names the narrower case (that object being a partial expression) and makes it identifier-1. The
        // rule is a no-op for a numeric data item, which BareOperandKind already calls an identifier — stated,
        // not assumed, because "the class boolean or numeric" is one sentence and implementing half of it would
        // be the two-arm shape again. A LITERAL or an arithmetic-expression subject is untouched: e) says "a
        // data item". (kb/Work PB398; unexercisable until this note landed a partial-expression object.)
        if (o is EvaluateObjectOperand.PartialExpression && slot.IsDataItemOfClassBooleanOrNumeric)
            s = EvaluateSubjectOperand.Identifier;
        return new EvaluatePairing(s, o, subjectBare, objectBare);
    }

    /// <summary>SR6's "results in one boolean character", over the §2023:8.8.2 GR9/GR10 result length. A
    /// POSITIONLESS operand (figurative ZERO / <c>ALL B"…"</c>, whose length is its sibling's — §2023:8.3.3.6.4 GR4)
    /// and a boolean function whose length argument is a run-time value both report null, and both answer YES:
    /// against a TRUE-or-FALSE counterpart one boolean position is the only length that gives the pair a
    /// meaning, and the §2023:8.8.4.3.3 SR1 screen on the resulting condition still refuses the rest. Failing OPEN is
    /// the direction that cannot reject legal source.</summary>
    private static bool IsOneBooleanCharacter(int? length) => length is null or 1;

    /// <summary>ISO §2023:14.9.13.3 SR10 — screen the pairing against Table 15 BEFORE binding it, so an invalid
    /// combination is a compile-time diagnostic rather than the run-time fault it used to be (kb/Work PB47).
    /// <para>⚠ The classifier still abstains — a null on either side means NO diagnostic — but only where it
    /// genuinely cannot name the shape (a bare operand that is none of identifier, literal or arithmetic
    /// expression). Neither the PARTIAL-EXPRESSION row (kb/Work PB398) nor the BOOLEAN rows are abstentions: SR6
    /// names a boolean operand's kind from the other side of the pair, and <see cref="ClassifyPair"/> implements
    /// that rule instead of declining to answer it (kb/Work PB400), for a boolean item, literal or an
    /// operator-bearing boolean expression alike (kb/Work PB1412).</para>
    /// <para>Returns false when the pairing is invalid and has been reported: the caller binds none of the PAIR's
    /// checks, because every further diagnostic the pair would draw is the same mistake read a second time
    /// (<c>EVALUATE BW WHEN TRUE</c> over a wide boolean item drew COBOL1634 and then COBOL2318 "'TRUE' is used
    /// as a condition" about the very object the table had just refused). The object's OPERANDS are not the pair's:
    /// the caller still binds them standalone (<see cref="BindObjectOperandsStandalone"/>, kb/Work PB4446).</para></summary>
    private bool ScreenPairing(in EvaluatePairing pair, Core.EvaluateWhenItemContext item)
    {
        if (pair.Subject is not { } s || pair.Object is not { } o) return true;
        if (EvaluateOperandCombinations.IsPermitted(s, o, ctx.Edition.Edition.Year)) return true;
        // §1985 has no Table 15: SR6 b asks that a condition, TRUE or FALSE selection object correspond to a conditional
        // expression or TRUE or FALSE subject, and SR6 a that any other object be a valid operand for comparison to its subject.
        string rule85 = o is EvaluateObjectOperand.Condition or EvaluateObjectOperand.TrueOrFalse
            ? "§1985:VI.6.13.3 SR6b" : "§1985:VI.6.13.3 SR6a";
        ctx.Edition.Error(DiagnosticCatalog.EvaluateOperandCombinationInvalid,
            $"the selection subject is {EvaluateOperandCombinations.Label(s)} and the selection object "
            + $"'{WrittenSource.Of(item)}' is {EvaluateOperandCombinations.Label(o)}; "
            + ctx.Edition.Edition.CiteRule($"{rule85} does not admit that combination.",
                "§2023:14.9.13.3 SR10 Table 15 marks that combination invalid."));
        return false;
    }

    /// <summary>ISO §2023:14.9.13.3 SR2 — "The number of selection objects within each set of selection objects shall
    /// be equal to the number of selection subjects." A SYNTAX rule, so a compile-time diagnostic (§2023:4.2.2), and
    /// an EQUALITY, so BOTH directions (kb/Work PB399).
    /// <para>⛔ IT LIVES BESIDE <see cref="ScreenPairing"/> AND NOT INSIDE THE PAIRING LOOP, because that is the
    /// difference between a rule and an index guard. The loop can only notice the direction in which it runs out
    /// of SUBJECTS; the other direction — fewer objects than subjects — is invisible to it by construction, and
    /// that was the half with no diagnostic at any stage. Counting the two sets against each other sees both.</para>
    /// <para>The count is per WHEN PHRASE, which is what "each set of selection objects" names: consecutive WHEN
    /// phrases sharing one imperative-statement are separate sets and are screened separately.</para></summary>
    private void ScreenObjectCount(Core.EvaluateWhenPhraseContext phrase, int objects, int subjects)
    {
        if (objects == subjects) return;
        ctx.Edition.Error(DiagnosticCatalog.EvaluateSelectionObjectCount,
            $"the WHEN phrase '{WrittenSource.Of(phrase)}' writes {objects} selection "
            + (objects == 1 ? "object" : "objects") + $" but the EVALUATE statement has {subjects} selection "
            + (subjects == 1 ? "subject" : "subjects")
            + $"; {ctx.Edition.Edition.CiteRule("§1985:VI.6.13.3 SR5", "§2023:14.9.13.3 SR2")} requires the two counts to be equal (write ANY for a position the phrase "
            + "does not test — " + ctx.Edition.Edition.Cite("§1985:VI.6.13.3 SR6c", "SR7 c") + ")");
    }

    /// <summary>⛔ THE ONE ADMISSIBILITY SCREEN OVER A RANGE-EXPRESSION'S PAIR OF OPERANDS (kb/Work PB399) —
    /// ISO §2023:14.9.13.3 SR4, "The two operands in a range-expression shall be of the same class and shall not be
    /// of class boolean, message-tag, object, or pointer", and SR9, "Neither identifier-3 nor identifier-4 shall
    /// reference a variable-length group". Both are SYNTAX rules, so both are compile-time (§2023:4.2.2).
    /// <para>ONE screen and not three `if`s, because the two rules ask ONE question — "may these two operands be
    /// the ends of a range?" — of the SAME pair, at the SAME moment, and <see cref="ScreenPairing"/> structurally
    /// cannot ask it: <see cref="ObjectKind"/> names a <c>valueRange</c> as a single
    /// <see cref="EvaluateObjectOperand.RangeExpression"/> row and never looks inside it, which is correct,
    /// because Table 15 is about the subject↔object PAIRING and not about a range's internals.</para>
    /// <para>CLASS here is §2023:8.5.2.1 Table 2's, read through the ONE Table-2 lattice
    /// (<see cref="IntrinsicArgumentRules.ClassOf"/>, projected onto the class column by
    /// <see cref="IntrinsicArgumentRules.TableTwoClass"/>) — never <see cref="CollatingSelection.ForComparison"/>,
    /// which answers the different question of which §2023:8.8.4.2 comparison rule a RELATION selects and deliberately
    /// collapses alphabetic onto alphanumeric and numeric-against-alphanumeric onto alphanumeric. A null class is
    /// a figurative constant (§2023:8.3.3.6.4 GR1 gives it its context's category, so the lattice answers a SET and
    /// not a class) or an already-reported error node, and abstains — the direction that cannot reject legal
    /// source. Class MESSAGE-TAG, SR4's fourth exclusion, is a lattice member since kb/Work PB4740.</para>
    /// <para>The pointer case is the sharpest, and it is why SR4 cannot be left to the relation checkpoint: a
    /// range lowers to `subject &gt;= low AND subject &lt;= high` (§2023:14.9.13.4 GR4a.5), and where that pair is
    /// NOT what gets emitted — an inverted or IN-alphabet range builds a <c>BoundRangeMembership</c> instead —
    /// the checkpoint is never reached at all. SR4 forbids the range outright, one phase earlier than any
    /// question about the comparison.</para></summary>
    private bool ScreenRangeOperands(Core.ValueRangeContext range, BoundOperand lo, BoundOperand hi)
    {
        bool ok = true;
        var loClass = TableTwoClassOf(lo);
        var hiClass = TableTwoClassOf(hi);
        // SR4, second half — the classes a range-expression may not be of, asked of EACH end so the diagnostic
        // can name the offender even when the other end abstains.
        bool excluded = false;
        for (int end = 0; end < 2; end++)
        {
            if ((end == 0 ? loClass : hiClass) is not { } c) continue;
            if (c is not (CobolClass.Boolean or CobolClass.MessageTag or CobolClass.Object or CobolClass.Pointer)) continue;
            excluded = true;
            ok = false;
            ctx.Edition.Error(DiagnosticCatalog.EvaluateRangeOperandInvalid,
                $"the {(end == 0 ? "first" : "second")} operand of the range-expression "
                + $"'{WrittenSource.Of(range)}' is of class {IntrinsicArgumentRules.Name(c)}; ISO §2023:14.9.13.3 SR4 "
                + "excludes class boolean, message-tag, object and pointer from a range-expression");
        }
        // SR4, first half — the same class. Reported only when neither end is already excluded, so one written
        // range cannot draw two diagnostics about the same operand.
        // ⛔ A STRONGLY-TYPED GROUP'S CLASS IS ITS TYPE-NAME (§2023:8.5.2.1, kb/Work PB3355): the lattice answers
        // alphanumeric for it (§2023:15.3 AR2, a function ARGUMENT), so the pair asks the one same-class identity.
        if (!excluded && loClass is { } lc && hiClass is { } hc)
        {
            if (!IntrinsicArgumentRules.SameClass(lo, lc, hi, hc))
            {
                ok = false;
                ctx.Edition.Error(DiagnosticCatalog.EvaluateRangeOperandInvalid,
                    $"the two operands of the range-expression '{WrittenSource.Of(range)}' are of class "
                    + $"{IntrinsicArgumentRules.ClassNameOf(lo, lc)} and class {IntrinsicArgumentRules.ClassNameOf(hi, hc)}; "
                    + ctx.Edition.Edition.CiteRule("§1985:VI.6.13.3 SR4", "§2023:14.9.13.3 SR4")
                    + " requires them to be of the same class (" + ctx.Edition.Edition.Cite("§1985:IV.4.3.3", "§2023:8.5.2.1 Table 2") + " — CLASS, not "
                    + "category, so alphabetic and alphanumeric are two classes)");
            }
        }
        // ⛔ A FIGURATIVE END ABSTAINS FROM TableTwoClassOf BUT IS NOT CLASS-FREE (kb/Work PB5796): SPACE, QUOTE,
        // HIGH-VALUE and LOW-VALUE are character values (§2023:8.3.3.6.4 GR1; §1985:IV.4.2.2.2.3 — nonnumeric literals,
        // category alphanumeric by §1985:IV.4.2.2.2.1.3 GR3), so `SPACE THRU 9` and `1 THRU HIGH-VALUE` join two
        // classes exactly as `"A" THRU 9` does. Only ZERO also stands for the numeric value 0 and so pairs with any class (§2023:8.3.3.6.4 GR4: "The zero format represents the numeric value '0'").
        else if (!excluded && (loClass is null || hiClass is null) && !IntrinsicArgumentRules.CanShareClass(lo, hi))
        {
            ok = false;
            ctx.Edition.Error(DiagnosticCatalog.EvaluateRangeOperandInvalid,
                $"the two operands of the range-expression '{WrittenSource.Of(range)}' are of class "
                + $"{IntrinsicArgumentRules.DescribeClasses(lo)} and class {IntrinsicArgumentRules.DescribeClasses(hi)}, "
                + "which share no class; "
                + ctx.Edition.Edition.CiteRule("§1985:VI.6.13.3 SR4", "§2023:14.9.13.3 SR4")
                + " requires them to be of the same class (a figurative "
                + "constant other than ZERO represents a character value, " + ctx.Edition.Edition.Cite("§1985:IV.4.2.2.2.3", "§2023:8.3.3.6.4 GR1") + ")");
        }
        // SR9 — a variable-length group is §2023:8.5.1.12.1's: "a group item whose data description has at least one
        // dynamic-length elementary item or dynamic-capacity table as a subordinate item". ⛔ NOT an
        // occurs-depending group, whose size is its maximum — through the ONE definition
        // (DataItem.HasVariableLengthSubordinate) so this screen cannot drift from the REDEFINES, VALUE
        // and report screens that ask the same question.
        for (int end = 0; end < 2; end++)
        {
            if ((end == 0 ? lo : hi) is not BoundFieldOperand { Place.Item: { IsGroup: true } item }) continue;
            if (!DataItem.HasVariableLengthSubordinate(item)) continue;
            ok = false;
            ctx.Edition.Error(DiagnosticCatalog.EvaluateRangeOperandInvalid,
                $"the {(end == 0 ? "first" : "second")} operand of the range-expression '{WrittenSource.Of(range)}' "
                + $"references '{item.CobolName ?? item.ClrName}', a variable-length group (ISO §2023:8.5.1.12.1 — a "
                + "dynamic-length elementary item or a dynamic-capacity table is subordinate to it); ISO "
                + "§2023:14.9.13.3 SR9 forbids either end of a range-expression to reference one");
        }
        return ok;
    }

    /// <summary>The operand's ISO §2023:8.5.2.1 Table 2 CLASS COLUMN — the lattice's answer projected through
    /// <see cref="IntrinsicArgumentRules.TableTwoClass"/>, so category numeric-edited answers with the class
    /// Table 2 gives it (alphanumeric) rather than with the refined member the lattice keeps for the
    /// category-worded rules. §2023:14.9.13.3 SR4 says CLASS, and §2023:8.5.2.1's closing sentence is why the projection
    /// is not optional: "Use of the name of a data class or data category in the rules of COBOL refers to the
    /// category unless class is specifically indicated."</summary>
    private static CobolClass? TableTwoClassOf(BoundOperand o) =>
        IntrinsicArgumentRules.ClassOf(o) is { } c ? IntrinsicArgumentRules.TableTwoClass(c) : null;

    // The object's pair of the two parse spellings of a selection operand (kb/Work PB1412; EvaluateSubjectBinder holds the
    // subject's and the comment that explains them).
    private static Core.ValueOperandContext? BareOperand(Core.EvaluateWhenItemContext item) =>
        item.valueOperand() ?? ConditionBinder.BareOperandOf(item.booleanExpression());

    private static Core.BooleanExpressionContext? BooleanOperatorBearing(Core.EvaluateWhenItemContext item) =>
        ConditionBinder.OperatorBearing(item.booleanExpression());

    /// <summary>The selection object's bare-operand analysis (§2023:14.9.13.3 SR6): an operator-bearing boolean
    /// expression is analysed as a boolean operand with its result length, any other bare operand through the shared
    /// classifier, and a written condition / range / partial-expression / ANY has none.</summary>
    private BareOperandAnalysis AnalyzeObjectBare(Core.EvaluateWhenItemContext item) =>
        BooleanOperatorBearing(item) is { } be ? host.Cond.AnalyzeBooleanExpression(be)
        : BareOperand(item) is { } ovo ? host.Cond.AnalyzeBareOperand(ovo)
        : default;

    /// <summary>The object's Table-15 ROW before SR6, or null when the shape cannot be named with certainty.
    /// The object-only forms are grammatical (ANY, the words TRUE and FALSE, a THRU range, an explicit condition);
    /// everything else is the SHARED bare-operand classification.</summary>
    private EvaluateObjectOperand? ObjectKind(Core.EvaluateWhenItemContext item, in BareOperandAnalysis bare)
    {
        if (item.ANY() is not null) return EvaluateObjectOperand.Any;
        if (item.booleanLiteral() is not null) return EvaluateObjectOperand.TrueOrFalse;
        if (item.valueRange() is not null) return EvaluateObjectOperand.RangeExpression;
        // An address-identifier IS identifier-2 (§2023:8.4.3.1.2 Format 9; kb/Work PB1060) — Table 15's Identifier row.
        if (item.addressIdentifier() is not null) return EvaluateObjectOperand.Identifier;
        // §2023:14.9.13.3 SR5 — the object's leftmost portion is a relational operator, a class condition without the
        // identifier, or a sign condition without its operand. The GRAMMAR decides it (that is what SR5 is: a
        // statement about the written form's left edge), so this row needs no symbol resolution — and with it
        // Table 15's Partial-expression row stops being a lookup no operand can reach (kb/Work PB398).
        if (item.partialExpression() is not null) return EvaluateObjectOperand.PartialExpression;
        if (item.condition() is { } c)
            // SR5 again, where the grammar's `condition` claimed the object: its leftmost simple condition is a
            // BARE class-name / alphabet-name, i.e. "a class condition without the identifier" (kb/Work PB843).
            return host.Cond.LeadingBareClassWord(c) is not null ? EvaluateObjectOperand.PartialExpression
                : EvaluateObjectOperand.Condition;
        // §2023:14.9.13.2's [NOT] boolean-expression-2 (Table 15's Boolean-expression ROW) before SR6 reclassifies it by
        // its result length (kb/Work PB1412).
        if (BooleanOperatorBearing(item) is not null) return EvaluateObjectOperand.BooleanExpression;
        return BareOperand(item) is { } vo ? EvaluateOperands.BareOperandKind(vo, bare) : null;
    }

    private BoundCondition BindWhenItem(EvaluateSubjectBinder.SubjectSlot slot, Core.EvaluateWhenItemContext item)
    {
        var subject = slot.Node;
        // ⛔ CLASSIFY ONCE, THEN BIND FROM THAT CLASSIFICATION (kb/Work PB400). The screen used to run its own
        // resolution and the bind path a second, identical one — so a level-88 whose reference is ambiguous
        // reported §2023:8.4.2.2 twice, and the two could disagree about what the operand IS with nothing to catch it.
        // The mark precedes the classification because a boolean-expression OBJECT is bound BY it (the one analysis,
        // PB1412): its activations belong to this object's own evaluation, never to the statement's beginning.
        var classifyMark = host.Window.Mark;
        var pair = ClassifyPair(slot, item);
        // ⛔ AN INVALID PAIR ENDS THE PAIR'S CHECKS: one Table 15 diagnostic, not that and every secondary one the same
        // pair would draw (the SR7 a) relation checkpoint, a TRUE/FALSE word bound as a condition). The pair's value is
        // moot — the compile has failed — so it is the constant false.
        // ⛔ IT DOES NOT END THE OBJECT'S OWN (kb/Work PB4446): the object's operands still draw their own reference and
        // expression faults (§2023:4.2.2), bound standalone by their own row.
        if (!ScreenPairing(pair, item))
        {
            BindObjectOperandsStandalone(item, pair.ObjectBare, classifyMark);
            return Constant(false);
        }
        if (item.ANY() is not null) return Constant(true);   // GR4 a) 1. — the word ANY is true

        bool subjTrue = subject.booleanLiteral()?.TRUE_() is not null;
        bool subjFalse = subject.booleanLiteral()?.FALSE_() is not null;

        // User-function evaluation cardinality (§2023:8.4.3.2.4 GR1/GR6a) over the EVALUATE windows:
        // — a VALUE SUBJECT is evaluated ONCE "at the beginning of the execution of the EVALUATE statement"
        //   (§2023:14.9.13.4 GR3) and now IS: the slot binds it once and materializes its value into the
        //   implementor's intermediate result item, so a subject activation hoists to statement scope exactly
        //   as the rule describes (kb/Work PB394);
        // — a CONDITION SUBJECT (§2023:14.9.13.4 GR3 e, "assigned a truth value") is the same: the slot binds the
        //   condition once and, where more than one arm reads it, materializes its TRUTH value into a
        //   one-position boolean intermediate (SendingValueTemp.MaterializeTruth — the condition→boolean bridge,
        //   kb/Work PB842 / PB912), so its activations too hoist to statement scope exactly once;
        // — an OBJECT is evaluated only when its WHEN phrase is considered, pairs left-to-right with a
        //   false pair stopping the phrase (GR4a–d) — its activations attach per-evaluation to the object
        //   term, and the composed &&/|| chain's C# short-circuit realizes GR4c exactly. A partial-expression
        //   object splices the subject's one VALUE in, so SR8's rewrite activates nothing of the subject's.
        // No subject window stages COBOL1509 any more: the narrowed residue stage had exactly these two
        // callers, and it is deleted with them.

        // A CONDITION subject (§2023:14.9.13.4 GR3e, Table 15's Condition column) pairs with TRUE/FALSE objects:
        // the term is the subject's ONE truth value (or its negation). Four spellings reach it — a written
        // condition (`EVALUATE X NUMERIC`, `EVALUATE WS-N > 1`, `EVALUATE NOT BW`, any combined condition), a
        // level-88, a switch-status condition-name, and SR6 b)'s one-boolean-character boolean subject — and the
        // CLASSIFIER decided which, so no shape can be recognised here and missed by the screen (or the reverse,
        // which is what refused `EVALUATE W-ON WHEN TRUE`).
        if (pair.Subject is EvaluateSubjectOperand.Condition && slot.Truth is { } subjCond)
        {
            // §2023:14.9.13.4 GR4a.4 — a TRUE/FALSE object: "the selection subject is condition-1 … if the truth
            // value of the selection subject and selection object match, the result of the analysis is true",
            // which for a constant object IS the subject condition or its negation.
            if (item.booleanLiteral() is { } objWord)
                return objWord.TRUE_() is not null ? subjCond : new BoundNot(subjCond);
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
                if (ObjectAsCondition(item, pair, classifyMark) is { } objCond2)
                    return new BoundNot(new BoundLogical("^", [subjCond, objCond2]));
            }
            return host.Cond.Refused($"EVALUATE condition-subject paired with non-boolean WHEN '{WrittenSource.Of(item)}'");
        }

        // ⛔ TRUE-or-FALSE × TRUE-or-FALSE (Table 15's two 'Y' cells for the words) IS A CONSTANT, AND NO CONDITION
        // IS BOUND FOR IT (kb/Work PB1668). §2023:14.9.13.4 GR3f gives the subject word its truth value, GR4 a) 4. makes
        // the pair "true" exactly when the two truth values match — so `EVALUATE TRUE WHEN TRUE` is true and
        // `EVALUATE FALSE WHEN TRUE` is false. The object's word used to go through BindCondition, whose bare-word
        // arm is not a condition (§2023:8.8.4.2.1) and refused conforming source through the COBOL2319 net.
        if (pair.Subject is EvaluateSubjectOperand.TrueOrFalse && item.booleanLiteral() is { } wordObject)
            return Constant(subjTrue == (wordObject.TRUE_() is not null));

        // A `condition` the classifier re-read as SR5's partial-expression (a bare class-name leftmost — PB843) is
        // NOT condition-2: it falls through to the partial arm below, which splices the subject in.
        if (pair.Object is not EvaluateObjectOperand.PartialExpression && item.condition() is { } cond)
        {
            var objMark = host.Window.Mark;
            var bound = host.Window.AttachPerEvaluation(host.Cond.BindCondition(cond), objMark);
            return subjFalse ? new BoundNot(bound) : bound;   // EVALUATE TRUE/FALSE WHEN <condition>
        }

        // §2023:14.9.13.4 GR4a.3 — "If the selection object is condition-2, the selection subject is either TRUE or
        // FALSE… If the truth value of the selection subject and selection object match, the result of the
        // analysis is true" — and §2023:14.9.13.3 SR10 Table 15 admits ONLY a condition, TRUE/FALSE or ANY against a
        // TRUE/FALSE subject, never identifier-2 or a value.
        // ⛔ A BARE CONDITION-NAME IS condition-2 (§2023:8.8.4.2.7 GR2) BUT ARRIVES THROUGH THE valueOperand ARM, because a
        // bare word is equally an arithmeticExpression and the grammar cannot tell them apart — only the resolved
        // SYMBOL can. Without this arm the commonest EVALUATE idiom there is —
        //     EVALUATE TRUE  WHEN VALID-CODE …
        // — compiled clean and threw "value WHEN object" at RUN TIME (fix-queue PB45). Reordering
        // evaluateWhenItem's alternatives is NOT the fix: Table 15 makes the object's legality depend on the
        // SUBJECT, so putting `condition` first would retarget `EVALUATE X WHEN <88>` (a VALUE subject, where the
        // grammar reads the same name as an equality operand) in the opposite direction. The question is asked by
        // the CLASSIFIER above, once, and this arm reads its answer — SR6 a) is what routes a one-boolean-
        // character boolean object here as well.
        if (pair.Object is EvaluateObjectOperand.Condition && ObjectAsCondition(item, pair, classifyMark) is { } objectCondition)
            return subjFalse ? new BoundNot(objectCondition) : objectCondition;

        // §2023:14.9.13.3 SR8 + §2023:14.9.13.4 GR4a.2 — partial-expression-1. The object is "treated as though it were
        // specified as condition-2, where condition-2 is the conditional expression that results from preceding
        // partial-expression-1 by the selection subject", and "the corresponding selection subject is treated as
        // though it were specified by the word TRUE" — so the pair's term IS that condition, with no comparison
        // against the subject wrapped around it.
        // ⛔ THE SUBJECT SPLICED IN IS THE SLOT'S ONE VALUE, NOT A RE-BIND OF ITS NODE (kb/Work PB912's sibling).
        // GR3 assigns the subject its value "at the beginning of the execution of the EVALUATE statement", and SR8
        // then forms a condition FROM that subject — nothing in SR8 re-evaluates it. This arm used to pass the
        // subject's PARSE NODE into the rewrite, which bound it again once per partial-expression object on top of
        // the statement's own evaluation: a user function in the subject was activated 1 + N times and the arms
        // compared N different values (measured: WHEN OTHER selected where WHEN = 1 was the answer), while the
        // residue stage meant to refuse the shape never fired. The class shape reads CONTENT (see
        // PartialSubjectOperand), so an in-place data item is handed over un-materialized for it.
        // ⛔ THREE PARSES, ONE ARM (kb/Work PB843): the grammar's partialExpression; a BARE class-name /
        // alphabet-name object — SR5's "class condition without the identifier" in the one spelling the grammar
        // cannot tell from identifier-2; and a `condition` whose LEFTMOST leaf is that bare word (`WHEN MY-CLASS
        // AND …`). The classifier named all three PartialExpression, the last two by their resolved symbol.
        if (pair.Object is EvaluateObjectOperand.PartialExpression)
        {
            // ⛔ THE EDITION GATE STANDS ON THIS ONE ARM, NOT ON A PARSE ALTERNATIVE (kb/Work PB5179): the three parses
            // above are one selection-object form, which the COBOL-85 object list (§1985:VI.6.13.2: ANY, a condition,
            // TRUE, FALSE, or [NOT] an identifier, literal or arithmetic expression) does not print and ISO/IEC
            // 1989:2002 Annex F.2 item 48 adds ("A partial expression may now be used as a selection object").
            using (ctx.Edition.At(item))
                ConstructRegistry.Check(ctx.Edition.Edition, ctx.Edition.Sink, Constructs.EvaluatePartialExpression2002,
                    $"the partial expression '{WrittenSource.Of(item)}' as an EVALUATE selection object");
            // The subject's operand node is null for a boolean-EXPRESSION subject (PB1412), which has none; it is a
            // subject all the same (Table 15's Partial-expression row is 'Y' under the Boolean-expression column).
            var subjOp = slot.BareValueOperand;
            if (slot.Value is not { } subjValue || (subjOp is null && slot.OperatorBearingExpression is null))
                return host.Cond.Refused("EVALUATE TRUE/FALSE paired with a value WHEN object");
            var content = slot.InPlaceValue is BoundFieldOperand inPlace ? inPlace : subjValue;
            var spliced = new ConditionBinder.PartialSubjectOperand(subjOp, subjValue, content);
            var objMark = host.Window.Mark;
            var partialCond = item.partialExpression() is { } partial
                ? host.Cond.BindPartialExpression(partial, spliced)
                : item.condition() is { } leadingClass
                    ? host.Cond.BindPartialExpression(leadingClass, spliced)
                : pair.ObjectBare.ClassWord is { } classWord
                    ? host.Cond.BindPartialClassName(classWord, spliced)
                    : host.Cond.Refused($"EVALUATE partial-expression '{WrittenSource.Of(item)}'");
            return host.Window.AttachPerEvaluation(partialCond, objMark);
        }

        // Value subject vs operand / range: equality or inclusive bounds (§2023:14.9.13.4 GR5b/c).
        // ⛔ THE SLOT'S ONE VALUE (§2023:14.9.13.4 GR3, kb/Work PB394) — NEVER a fresh bind of the subject's node. The
        // subject is "evaluated and assigned a value" at the beginning of the statement, and a RANGE object reads
        // that one value TWICE ("selection-subject >= left-part AND selection-subject <= right-part", GR4 a) 5.):
        // before this, the two halves of ONE range test saw two DIFFERENT values of a value-varying subject, an
        // answer no single subject value can produce. No residue stage here any more — a subject activation
        // hoists to statement scope with the value, which is what the rule asks for.
        if (slot.Value is not { } left)
            return host.Cond.Refused("EVALUATE TRUE/FALSE paired with a value WHEN object");

        if (item.valueRange() is { } range)
        {
            var objMark = host.Window.Mark;
            // ⛔ A REJECTED RANGE CONTRIBUTES AN ERROR NODE, NOT A COMPARISON. Both lowerings below — the
            // BoundRangeMembership one and the inclusive CheckedRelational pair — ask questions these operands
            // have already failed to be admissible for, and the relation checkpoint would draw the §2023:8.8.4.2.2
            // Format 3 diagnostic TWICE more about the same two operands after SR4 has said the range may not
            // exist at all.
            if (BindRangeObject(range) is not { } bound)
                return host.Cond.Refused(
                    $"EVALUATE range-expression '{WrittenSource.Of(range)}'");
            var (lo, hi, rangeClass, collation) = bound;
            bool check = ctx.EcState.Turn.Enabled("EC-RANGE-INVALID", null, item.Start.Line);
            // §2023:14.7.8 GR2: an inverted alphanumeric/national THRU range sets the nonfatal EC-RANGE-INVALID, and
            // then "execution proceeds as if the range of values were empty". The EC is a property of the RANGE's
            // CLASS, exactly as the collating sequence one sentence above it is — never of the written FORM of its
            // ends.
            // ⛔ THIS GATE USED TO READ `lo is BoundStringLiteral{…} && hi is BoundStringLiteral` (kb/Work PB401),
            // which raised the exception ONLY where a reader of the source can already see the inversion and NEVER
            // in the run-time case §2023:14.7.8 GR2's own "in the collating sequence in effect at runtime" is written for:
            // `EVALUATE X WHEN WS-M THRU WS-A` set nothing while `EVALUATE X WHEN "M" THRU "A"` over the identical
            // values set EC-RANGE-INVALID, and the level-88 VALUE range — the OTHER clause §2023:14.7.8's first sentence
            // governs — already asked the CLASS. One rule, one antecedent, both clauses.
            bool ecApplies = check && CollatingSelection.IsCollatedThroughRange(rangeClass);
            // ⛔ AN IN PHRASE ROUTES HERE WHETHER OR NOT CHECKING IS ON: a relation pair derives its sequence from
            // its operands' categories, which is the very rule the phrase overrides, so the named sequence has
            // nowhere to ride on that lowering. The unchecked render of this node is that same inclusive pair.
            // ⛔ …BUT ONLY FOR A PAIR THAT COMPARES AS ONE ITEM EACH (kb/Work PB1469 / PB1467 sibling arm). The node
            // compares character IMAGES, which is §2023:8.8.4.2.7 for a Flat pair and wrong for the other two shapes: a
            // strongly-typed range orders element by element (§2023:8.8.4.2.12 — `EVALUATE A WHEN LO THRU HI` over float
            // leaves answered OUT for -10 <= -5 <= 3 the moment EC-RANGE-INVALID checking routed it here), and a
            // variable-length group has no image at all (§2023:8.8.4.2.17). Those take the inclusive relation pair, which
            // is §2023:14.9.13.4 GR4a 5.'s own lowering and passes through the ONE relation checkpoint.
            bool flatRange = RelationPair.Classify(left, lo) is RelationPairShape.Flat
                && RelationPair.Classify(left, hi) is RelationPairShape.Flat;
            if (flatRange && (collation is not null || ecApplies))
                return host.Window.AttachPerEvaluation(
                    host.Cond.CheckedRangeMembership(left, lo, hi, checkInvalid: ecApplies, collation),
                    objMark);
            return host.Window.AttachPerEvaluation(new BoundLogical("&&",
                [host.Cond.CheckedRelational(left, ">=", lo), host.Cond.CheckedRelational(left, "<=", hi)]),
                objMark);
        }
        // boolean-expression-2 (§2023:14.9.13.4 GR4a.6): "selection-subject [NOT] = selection-object" with the object a
        // boolean expression — the boolean relation of §2023:8.8.4.2.2 Format 2, built through the ONE relation checkpoint
        // exactly as the IF form `subject = A B-AND C` is (ConditionBinder.BindPrimaryBoolean). The expression was bound
        // ONCE, by the classification, so this reads that node.
        if (BooleanOperatorBearing(item) is not null && pair.ObjectBare.Boolean is { } objectBoolean)
            return host.Window.AttachPerEvaluation(
                host.Cond.CheckedRelational(left, "==", new BoundBoolOperand(objectBoolean)), classifyMark);
        // An address-identifier object (kb/Work PB1060): "selection-subject = selection-object" over two data
        // items of class pointer — §2023:8.8.4.2's pointer relation, screened by the ONE relation checkpoint.
        if (item.addressIdentifier() is { } oai)
        {
            var objMark = host.Window.Mark;
            return host.Window.AttachPerEvaluation(host.Cond.CheckedRelational(left, "==",
                host.Ptr.AddressIdentifierOperand(oai, "an EVALUATE selection object")), objMark);
        }
        if (BareOperand(item) is { } v)
        {
            var objMark = host.Window.Mark;
            return host.Window.AttachPerEvaluation(
                host.Cond.CheckedRelational(left, "==", EvaluateOperands.BindValueOperand(host, v)), objMark);
        }
        return host.Cond.Refused($"EVALUATE WHEN object '{WrittenSource.Of(item)}'");
    }

    /// <summary>A range-expression's two bound operands with the facts the rules below ask of the PAIR: its class and its
    /// <c>IN alphabet-name-1</c> collating sequence (null when absent or refused).</summary>
    private readonly record struct BoundRangeObject(BoundOperand Lo, BoundOperand Hi, CollatingClass Class, RangeCollationId? Collation);

    /// <summary>⛔ THE ONE BINDING OF A RANGE-EXPRESSION'S OPERANDS AND PHRASE — for the pair that compares against it
    /// and for the object a refused or surplus pairing binds standalone (kb/Work PB4446), so the range's own syntax
    /// rules cannot be asked on one path and not the other. Null when SR4 / SR9 refused the range (reported once).
    /// <para>⛔ SR4 + SR9 BEFORE THE CLASS IS ASKED (kb/Work PB399). CollatingSelection.ForComparison's own remark says
    /// the message-tag / object / pointer categories "never reach here" — and they DID, by this route, because nothing
    /// screened the range's operands: a pointer pair answered ALPHANUMERIC, turned EC-RANGE-INVALID checking on, and had
    /// the emitter order raw addresses. The screen makes that remark true again, one phase earlier.</para>
    /// <para>⛔ THE RANGE'S CLASS, ASKED ONCE, OF BOTH ENDS (§2023:14.9.13.3 SR4 gives the pair one class to have) — and it
    /// is what BOTH of §2023:14.7.8's halves are keyed on: GR1 (numeric) is algebraic, names no sequence and sets no
    /// exception; GR2 (alphanumeric or national) has the sequence, the IN alphabet-name-1 phrase AND the EC. Two
    /// separately-written tests of one antecedent is how the two halves of GR2 came to disagree about the same
    /// range (kb/Work PB401).</para>
    /// <para>§2023:14.9.13.2's range-expression ends `[ IN alphabet-name-1 ]`, and §2023:14.7.8 GR2 makes that alphabet THE
    /// collating sequence the range is evaluated in — overriding the no-phrase arm's "defined by the implementor"
    /// default, which for this compiler is the PROGRAM COLLATING SEQUENCE. The SR3 screens and the carrier registration
    /// are the ONE resolver §2023:14.7.8's opening sentence asks for ("This specification applies to THROUGH phrases
    /// specified in the VALUE clause and the EVALUATE statement"), so the VALUE clause's identical phrase reaches the
    /// same code (kb/Work PB398).</para></summary>
    private BoundRangeObject? BindRangeObject(Core.ValueRangeContext range)
    {
        var lo = EvaluateOperands.BindValueOperand(host, range.valueOperand(0));
        var (hi, alphabetWord) = BindRangeHigh(range);
        if (!ScreenRangeOperands(range, lo, hi)) return null;
        var rangeClass = CollatingSelection.ThroughRangeClass(lo, hi);
        return new BoundRangeObject(lo, hi, rangeClass, RangeAlphabet(alphabetWord, rangeClass));
    }

    /// <summary>The range's <c>IN alphabet-name-1</c> phrase (ISO §2023:14.9.13.2's range-expression), screened by
    /// §2023:14.9.13.3 SR3 and registered as a runtime carrier — or null when the phrase is absent or was refused, in
    /// which case §2023:14.7.8 GR2's no-phrase arm applies and the sequence stays the implementor's.
    /// <para>The range's CLASS is asked of the ONE comparison-class rule over BOTH bounds, never of one of them:
    /// SR3's two sentences are a test on the pair ("the literals or identifiers specified in the THROUGH phrase",
    /// then "if literal-3 or identifier-3 is of class national"), and §2023:14.9.13.3 SR4 already requires the two to be
    /// of the same class — so the pair has one class to have. It is COMPUTED BY THE CALLER and passed in, because
    /// the EC gate one line below keys on the SAME antecedent and the two must not be able to disagree
    /// (kb/Work PB401).</para></summary>
    private RangeCollationId? RangeAlphabet(Core.CobolWordContext? word, CollatingClass rangeClass) =>
        word is null ? null : ctx.Data.SpecialNames.TryResolveRangeAlphabet(word, rangeClass, "an EVALUATE WHEN THROUGH range");

    /// <summary>The range's right-hand operand and its <c>IN alphabet-name-1</c> word, with the one ambiguity the
    /// grammar cannot settle settled by SYMBOL (kb/Work PB843). §2023:14.9.13.3 SR3 admits the phrase over identifiers
    /// ("the literals or identifiers specified in the THROUGH phrase"), but <c>IN</c> is also the qualification
    /// connective, so over an identifier-4 the parser's greedy <c>dataReferenceSuffix*</c> loop takes
    /// <c>WS-HI IN AL</c> as a qualified reference and the phrase is never seen.
    /// <para>The symbol decides it: §2023:8.3.2.2 — "a given user-defined word may be used as only one type of
    /// user-defined word" — makes an alphabet-name a different type from every name a qualifier can be
    /// (§2023:8.4.2.2.1: qualifiers are "superordinate names from the hierarchy to which a user-defined name belongs"),
    /// so a trailing <c>IN word</c> that names a declared alphabet IS the phrase, and the reference is bound without
    /// it. Only the LAST suffix, only the <c>IN</c> spelling (the phrase prints no <c>OF</c>) and only a bare word
    /// (the phrase takes no subscript) can be it; any other qualifier keeps its qualification reading.</para>
    /// <para>⛔ THE ARGUMENT HOLDS WITHIN ONE SOURCE ELEMENT ONLY (kb/Work PB3153). A contained program or a method
    /// also sees the alphabet-names its container declares, and "a source element's reference to such a name, even
    /// when it is a different type of user-defined word, is to the item which that source element describes"
    /// (§2023:8.4.6.1): over an inherited alphabet GRP, <c>HI IN GRP</c> with the element's own group GRP is a qualified
    /// reference. So the qualified reference is probed first, and only when it identifies nothing is the trailing
    /// word the phrase (whose resolution records the §2023:8.3.2.2 use). In one element an alphabet-name and a qualifier
    /// cannot share a spelling, so the probe changes no answer there.</para></summary>
    private (BoundOperand Hi, Core.CobolWordContext? AlphabetWord) BindRangeHigh(Core.ValueRangeContext range)
    {
        var hiNode = range.valueOperand(1);
        if (range.cobolWord() is { } written) return (EvaluateOperands.BindValueOperand(host, hiNode), written);
        if (hiNode.arithmeticExpression() is { } expr && ConditionBinder.SoleDataRef(expr) is { } dref
            && dref.dataReferenceSuffix() is { Length: > 0 } suffixes
            && suffixes[^1].qualification() is { } q && q.IN() is not null && q.ChildCount == 2
            && ctx.Data.SpecialNames.IsAlphabetName(q.cobolWord().GetText()) && !ctx.Refs.DescribesOwnItem(dref))
            return (host.Expr.FieldOperand(ReferenceResolver.WithoutTrailingSuffix(dref)), q.cobolWord());
        return (EvaluateOperands.BindValueOperand(host, hiNode), null);
    }

    /// <summary>The selection OBJECT's condition when <see cref="ClassifyPair"/> put it in Table 15's Condition
    /// ROW: an explicit <c>condition</c> object, or the bare form (a level-88, a switch-status condition-name,
    /// or — through §2023:14.9.13.3 SR6a — a one-boolean-character boolean object) that the SHARED analysis
    /// already resolved. One helper, so the two arms that need condition-2 cannot recognise different
    /// shapes.
    /// <para>⛔ THE CONDITION COMES BACK WITH ITS PER-EVALUATION WINDOW ALREADY APPLIED (kb/Work PB6228, §2023:14.9.13.4
    /// GR4 c/d: an object is evaluated only when its WHEN phrase is analyzed). A bare object is bound BY
    /// <see cref="ClassifyPair"/>, so its activations (a function-bearing subscript of a level-88's item) were taken
    /// after <paramref name="classifyMark"/>, never after a mark taken here; attaching from the caller's mark means no
    /// arm can take its mark after the bind and leave them to hoist to the head of the EVALUATE.</para></summary>
    private BoundCondition? ObjectAsCondition(Core.EvaluateWhenItemContext item, in EvaluatePairing pair, OperandEvaluationWindow.PendingMark classifyMark) =>
        (item.condition() is { } cond ? host.Cond.BindCondition(cond) : host.Cond.AsCondition(pair.ObjectBare)) is { } c
            ? host.Window.AttachPerEvaluation(c, classifyMark)
            : null;

    /// <summary>A pair whose truth value is known at bind time: the empty conjunction is true (it renders as the
    /// constant true), its negation false.</summary>
    private static BoundCondition Constant(bool value) =>
        value ? new BoundLogical("&&", []) : new BoundNot(new BoundLogical("&&", []));
}
