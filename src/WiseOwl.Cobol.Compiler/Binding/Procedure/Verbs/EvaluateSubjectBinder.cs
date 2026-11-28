// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>EVALUATE's selection SUBJECTS (kb/Work PB2338; docs/rearchitecture/DESIGN-architecture-review.md §8.3):
/// one <see cref="SubjectSlot"/> per subject for the whole statement, its bare-operand resolution, its assigned value or
/// truth value (§2023:14.9.13.4 GR3) and its Table 15 COLUMN before §2023:14.9.13.3 SR6. The pairing with a WHEN
/// phrase's objects is <see cref="WhenPhraseBinder"/>'s, which reads a subject only through its slot.</summary>
internal sealed class EvaluateSubjectBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>Bind the statement's selection subjects: one slot each, every slot's value (and a condition-1 subject's
    /// truth value) bound before any WHEN arm binds.</summary>
    public SubjectSlot[] Bind(Core.EvaluateStatementContext ev)
    {
        var subjects = ev.evaluateSubject();
        // ⛔ ONE SLOT PER SELECTION SUBJECT, FOR THE WHOLE STATEMENT (ISO §2023:14.9.13.4 GR3, kb/Work PB394): "At the
        // beginning of the execution of the EVALUATE statement, each selection subject is evaluated and assigned
        // a value, a range of values, or a truth value". The subject used to be re-BOUND inside BindWhenItem —
        // per selection PAIR, and therefore per WHEN — so the subject EXPRESSION was emitted once per relational
        // use, twice for a THRU arm. A slot binds it once and every pair reads the same node.
        var slots = new SubjectSlot[subjects.Length];
        for (int i = 0; i < subjects.Length; i++) slots[i] = new SubjectSlot(this, subjects[i], SubjectUsageOf(ev, i));
        // ⛔ GR3 IS AN OBLIGATION OF THE STATEMENT, NOT OF WHICHEVER ARM HAPPENS TO READ THE SUBJECT (kb/Work
        // PB396). "At the beginning of the execution of the EVALUATE statement, each selection subject is
        // evaluated and assigned a value, a range of values, or a truth value" (ISO §2023:14.9.13.4 GR3). The slots
        // were bound LAZILY, from inside the pair binder, so a statement no arm read the subject in never
        // evaluated it: `WHEN ANY` against every subject — and, before the §2023:14.9.13.2 grammar repair, an
        // EVALUATE whose only clause was WHEN OTHER — compiled the subject away entirely. Measured: an
        // undefined selection subject lost its COBOL1639 (§2023:8.4.2.1), `EVALUATE R (9)` over `OCCURS 3` lost
        // EC-BOUND-SUBSCRIPT, and `EVALUATE A / B` with B = 0 lost EC-SIZE-ZERO-DIVIDE — all three fatal on the
        // identical program with one dead WHEN arm added. Touching every slot HERE, before any arm binds, both
        // resolves the operand and — through SubjectUsage.NeedsIntermediate, which is true for every shape but
        // "one use, in the first arm" — registers the evaluation as the statement's PRE-op, which is the
        // generated-code position of "the beginning of the execution".
        // ⛔ AND THE SAME FOR A TRUTH VALUE (kb/Work PB842 / PB912): a subject that is condition-1 by its FORM —
        // a written condition, or a bare level-88 / switch-status condition-name — is assigned its truth value here,
        // once, before any arm binds (GR3 e). SR6 b)'s one-boolean-character boolean subject is condition-1 only
        // for a TRUE/FALSE pairing (a per-WHEN reclassification), so its truth value binds on first demand.
        foreach (var slot in slots) { _ = slot.Value; if (slot.IsCondition1ByForm) _ = slot.Truth; }
        return slots;
    }

    /// <summary>The subject's Table-15 COLUMN before SR6, or null when the shape cannot be named with
    /// certainty. The two subject-only forms are grammatical (TRUE/FALSE, and a written condition — condition-1
    /// per §2023:14.9.13.4 GR3e); everything else is the SHARED bare-operand classification.</summary>
    private static EvaluateSubjectOperand? SubjectKind(Core.EvaluateSubjectContext subject, in BareOperandAnalysis bare)
    {
        if (subject.booleanLiteral() is not null) return EvaluateSubjectOperand.TrueOrFalse;
        if (subject.condition() is not null) return EvaluateSubjectOperand.Condition;  // EVALUATE X > 1, NOT BW, X NUMERIC
        // An address-identifier IS identifier-1 (§2023:8.4.3.1.2 Format 9; kb/Work PB1060) — Table 15's Identifier column.
        if (subject.addressIdentifier() is not null) return EvaluateSubjectOperand.Identifier;
        // §2023:14.9.13.2's boolean-expression-1 (Table 15's Boolean-expression COLUMN) before SR6 reclassifies it by its
        // result length (kb/Work PB1412).
        if (BooleanOperatorBearing(subject) is not null)
            return EvaluateOperandCombinations.AsSubjectOperand(EvaluateObjectOperand.BooleanExpression);
        if (BareOperand(subject) is not { } vo) return null;
        return EvaluateOperands.BareOperandKind(vo, bare) is { } row ? EvaluateOperandCombinations.AsSubjectOperand(row) : null;
    }

    /// <summary>§2023:14.9.13.3 SR6e's antecedent — "the selection subject is a data item of the class boolean or
    /// numeric". A DATA ITEM, so a literal, an arithmetic expression, a function result and a condition-name are
    /// all excluded by construction (the reference must reduce to one data reference); the CLASS is read from the
    /// ONE category reader, <c>DataItem.OperandPic</c>, so a bit GROUP or a national group's §2023:13.18.29.4 GR1b/GR2b
    /// as-if PICTURE answers exactly as its elementary twin does — reading raw <c>Pic</c> would return null for
    /// every group and silently drop them (the kb/Work PB728/PB741 shape).</summary>
    private bool IsDataItemOfClassBooleanOrNumeric(Core.EvaluateSubjectContext subject, in BareOperandAnalysis bare)
    {
        if (bare.IsConditionName) return false;   // a condition-name is not a data item in this position
        if (subject.condition() is not null || subject.booleanLiteral() is not null) return false;
        if (subject.valueOperand()?.arithmeticExpression() is not { } expr) return false;
        if (ConditionBinder.SoleDataRef(expr) is not { } dref) return false;
        // Probe — a routing predicate is diagnostic-free (R30); an unresolvable subject reports through the bind.
        return ctx.Refs.Probe(dref) is { Item.OperandPic.Category: PicCategory.Boolean or PicCategory.Numeric };
    }

    // ⛔ A SELECTION OPERAND HAS TWO PARSE SPELLINGS, ASKED THROUGH ONE PAIR OF READERS PER SIDE (kb/Work PB1412). The
    // grammar's `valueOperand` alternative takes every operand it can; its `{boolExprAhead()}? booleanExpression` one
    // takes an operand carrying a boolean operator (`A B-AND C`) — and, because the discriminator's scan is
    // condition-shaped, can also take an OPERATOR-FREE one (`(B"1")`), which is the bare operand it encloses. So the
    // bare operand is `valueOperand`, or the one an operator-free booleanExpression reduces to; the operator-bearing
    // expression is the booleanExpression when it has an operator. The subject and the object are different parse
    // contexts with the same two members, so each side has its own pair (WhenPhraseBinder holds the object's).
    private static Core.ValueOperandContext? BareOperand(Core.EvaluateSubjectContext subject) =>
        subject.valueOperand() ?? ConditionBinder.BareOperandOf(subject.booleanExpression());

    private static Core.BooleanExpressionContext? BooleanOperatorBearing(Core.EvaluateSubjectContext subject) =>
        ConditionBinder.OperatorBearing(subject.booleanExpression());

    /// <summary>Bind (and, when more than one pair reads it, MATERIALIZE) one selection subject's assigned
    /// TRUTH value — ISO §2023:14.9.13.4 GR3e, "Any selection subject specified by condition-1 is assigned a truth
    /// value according to the rules for evaluating conditional expressions". Null when the subject has no
    /// condition form (a value subject that is not SR6 b)'s one-boolean-character boolean).
    /// <para>The condition is a written <c>condition</c> — bound by THE ONE condition binder, so a class test, a
    /// relation, a sign test, <c>NOT</c> over a boolean item (§2023:8.8.4.3.2) and every combined condition mean here
    /// exactly what they mean in an IF — or, through the SHARED bare-operand resolution the classifier already
    /// made, a level-88 condition-name (§2023:8.8.4.2.7 GR2), a switch-status condition-name (§2023:8.8.4.6), or SR6 b)'s
    /// boolean subject as a §2023:8.8.4.3 simple boolean condition (kb/Work PB400: one resolution, consulted twice).</para>
    /// <para>⛔ BOUND ONCE, NEVER PER WHEN (kb/Work PB842 / PB912). This used to run inside the pair binder, so the
    /// condition was re-bound — and re-emitted — once per WHEN arm, and a user-defined function inside it was
    /// refused (COBOL1509) because a per-WHEN re-bind would have over-activated it. With the truth value in an
    /// intermediate the re-bind and the refusal are both gone. The intermediate is taken on the SAME
    /// <see cref="SubjectUsage.NeedsIntermediate"/> test as a value subject's: a single read in the first arm is
    /// itself the one evaluation at "the beginning of the execution of the EVALUATE statement".</para>
    /// <para>A class test reads the ORIGINAL operand's character content here (the condition is bound over the
    /// written operand, never over a value intermediate), so <c>IS NUMERIC</c> still sees invalid content that a
    /// numeric intermediate would have normalized (§2023:8.8.4.4) — only the resulting TRUTH value is stored.</para></summary>
    private BoundCondition? BindSubjectTruth(SubjectSlot slot)
    {
        var truth = slot.Node.condition() is { } cond ? host.Cond.BindCondition(cond)
            : host.Cond.AsCondition(slot.Bare);
        if (truth is null) return null;
        return slot.NeedsIntermediate ? host.SendingValue.MaterializeTruth(truth, "evaluate") : truth;
    }

    /// <summary>
    /// ⛔ ONE SELECTION SUBJECT, BOUND ONCE FOR THE WHOLE STATEMENT — ISO §2023:14.9.13.4 GR3, "At the beginning of
    /// the execution of the EVALUATE statement, each selection subject is evaluated and assigned a value, a
    /// range of values, or a truth value" (kb/Work PB394).
    /// <para>The chained-selection lowering pairs the subject against every WHEN's object, and a RANGE object
    /// pairs against it TWICE (GR4 a) 5. — "selection-subject &gt;= left-part AND selection-subject &lt;=
    /// right-part"). Binding the subject inside that loop meant the subject EXPRESSION was emitted once per
    /// relational USE, so a value-varying subject gave each use a different value — measured: an EVALUATE with
    /// two THRU arms consumed FOUR values of <c>FUNCTION RANDOM</c> and printed a branch no single subject value
    /// can select. The slot binds it once and hands the same node to every pair.</para>
    /// <para><b>Both halves are lazy on purpose.</b> The bare-operand analysis is the resolution
    /// <c>ClassifyPair</c> used to redo per pair; the VALUE is bound only if some pair actually asks for one, so
    /// a condition-name / class-test / TRUE-FALSE subject never runs a value bind (which would be a symbol
    /// lookup no rule asks for, and a diagnostic no rule licenses).</para>
    /// </summary>
    internal sealed class SubjectSlot(EvaluateSubjectBinder owner, Core.EvaluateSubjectContext node, SubjectUsage usage)
    {
        private BareOperandAnalysis _bare;
        private bool _bareBound;
        private BoundOperand? _value;
        private bool _valueBound;
        private BoundCondition? _truth;
        private bool _truthBound;
        private BoundOperand? _inPlace;

        /// <summary>The subject's parse node — the classifier and the condition path still read it.</summary>
        public Core.EvaluateSubjectContext Node => node;

        /// <summary>Whether GR3's evaluation needs the implementor's intermediate result item — the
        /// §2023:14.9.25.4 GR1 argument applied to GR3: at ONE use IN THE FIRST ARM the single render already IS the
        /// one evaluation, so no intermediate is created. EVALUATE is a hot verb. Every other shape (no use at
        /// all, a use only in a later arm, more than one use) needs the store. See <see cref="SubjectUsage"/>.</summary>
        public bool NeedsIntermediate => usage.NeedsIntermediate;

        /// <summary>The subject's §2023:14.9.13.3 SR6 bare-operand resolution, made once for the statement.</summary>
        public BareOperandAnalysis Bare
        {
            get
            {
                if (!_bareBound) { _bare = owner.AnalyzeSubjectBare(node); _bareBound = true; }
                return _bare;
            }
        }

        /// <summary>The subject's assigned VALUE (GR3 a/b/c/d) — materialized into the implementor's
        /// intermediate result item when more than one pair reads it. Null when the subject has no value form
        /// (TRUE/FALSE, or a condition-name this classifier already resolved as condition-1).</summary>
        public BoundOperand? Value
        {
            get
            {
                if (!_valueBound) { _value = owner.BindSubjectValue(this, out _inPlace); _valueBound = true; }
                return _value;
            }
        }

        /// <summary>The subject's operand as BOUND, before any materialization — the one bind of the written
        /// operand, read by a shape that must see the item's own content (a partial-expression class test).
        /// Null when the subject has no value form.</summary>
        public BoundOperand? InPlaceValue { get { _ = Value; return _inPlace; } }

        /// <summary>The subject's Table-15 COLUMN before §2023:14.9.13.3 SR6 (<see cref="SubjectKind"/>), from the
        /// statement's one bare-operand resolution.</summary>
        public EvaluateSubjectOperand? Kind => SubjectKind(node, Bare);

        /// <summary>§2023:14.9.13.3 SR6e's antecedent, "the selection subject is a data item of the class boolean or
        /// numeric" (<see cref="IsDataItemOfClassBooleanOrNumeric"/>).</summary>
        public bool IsDataItemOfClassBooleanOrNumeric => owner.IsDataItemOfClassBooleanOrNumeric(node, Bare);

        /// <summary>The subject's bare value operand: its <c>valueOperand</c>, or the one an operator-free boolean
        /// expression reduces to (null for a boolean-EXPRESSION subject, which has none — kb/Work PB1412).</summary>
        public Core.ValueOperandContext? BareValueOperand => BareOperand(node);

        /// <summary>The subject's operator-bearing boolean expression (boolean-expression-1), or null.</summary>
        public Core.BooleanExpressionContext? OperatorBearingExpression => BooleanOperatorBearing(node);

        /// <summary>True when the subject is condition-1 by its FORM, whatever it is paired with: a written
        /// condition, or a bare level-88 / switch-status condition-name. (SR6 b)'s boolean subject is condition-1
        /// only for a TRUE/FALSE pairing, so it is not included.)</summary>
        public bool IsCondition1ByForm =>
            node.condition() is not null || Bare.Form is BareOperandForm.ConditionName or BareOperandForm.SwitchStatus;

        /// <summary>The subject's assigned TRUTH value (GR3 e) — bound once, materialized into a one-position
        /// boolean intermediate when more than one pair reads it. Null when the subject has no condition form.</summary>
        public BoundCondition? Truth
        {
            get
            {
                if (!_truthBound) { _truth = owner.BindSubjectTruth(this); _truthBound = true; }
                return _truth;
            }
        }
    }

    /// <summary>The subject's bare-operand analysis (§2023:14.9.13.3 SR6) — analyzed only when the subject really IS
    /// a bare operand: under a written condition-1 the operands belong to the condition, and resolving one as a
    /// condition-name would be a symbol lookup no rule asks for (and a diagnostic no rule licenses).</summary>
    private BareOperandAnalysis AnalyzeSubjectBare(Core.EvaluateSubjectContext subject) =>
        BooleanOperatorBearing(subject) is { } be ? host.Cond.AnalyzeBooleanExpression(be)
        : BareOperand(subject) is { } svo ? host.Cond.AnalyzeBareOperand(svo)
        : default;

    /// <summary>Bind (and, when more than one pair reads it, MATERIALIZE) one selection subject's assigned value
    /// — ISO §2023:14.9.13.4 GR3a–GR3d. Null when the subject has no value form.
    /// <para>⛔ A written CONDITION-1 subject's operands are NOT materialized, and that is a rule, not an
    /// omission: GR3 e) assigns the subject <b>condition-1</b> a TRUTH value, not its operand a data value, and a
    /// class test reads the operand's CHARACTER CONTENT — copying a numeric-DISPLAY item through an intermediate
    /// would normalize content that <c>IS NUMERIC</c> exists to find invalid (§2023:8.8.4.4). The TRUTH value is what
    /// is held once (<see cref="BindSubjectTruth"/>).</para>
    /// <para>⛔ A bare CONDITION-NAME / switch-status subject has no value form at all (§2023:8.8.4.2.7 GR2 /
    /// §2023:8.8.4.6 make it condition-1), so it returns null rather than being bound as an operand.</para></summary>
    private BoundOperand? BindSubjectValue(SubjectSlot slot, out BoundOperand? inPlace)
    {
        inPlace = null;
        var subject = slot.Node;
        // ⛔ boolean-expression-1 (§2023:14.9.13.4 GR3d) — "assigned a boolean value according to the rules for evaluating
        // boolean expressions" (kb/Work PB1412). The slot's ONE bound tree is the value, held in a boolean intermediate of
        // its §2023:8.8.2 GR10 result length through the same store a boolean function argument takes
        // (SendingValueTemp.MaterializeBoolean) when more than one pair reads it. A length no intermediate item can
        // describe (positionless, or a run-time function length) stays un-held, as ArgumentOrder.StoreAt leaves it.
        if (BooleanOperatorBearing(subject) is not null && slot.Bare.Boolean is { } boolean)
        {
            // ExpressionAsWritten: an operator-bearing boolean expression is not identifier-1 of a class partial (§2023:8.8.4.4.2; kb/Work PB4763).
            var written = inPlace = new BoundBoolOperand(boolean) { ExpressionAsWritten = true };
            int? positions = ConditionBinder.BoolResultLength(boolean);
            if (!slot.NeedsIntermediate || positions is not > 0
                || host.SendingValue.MaterializeBoolean(boolean, positions.Value, "evaluate") is not { } held)
                return written;
            return new BoundFieldOperand(held) { ExpressionAsWritten = true };
        }
        BoundOperand value;
        // An address-identifier subject (kb/Work PB1060) is a VALUE subject of class pointer, through the ONE
        // address-identifier operand binder the relation operand uses; GR3's materialization below applies to it as
        // to any identifier (a pointer the intermediate cannot model stays the bound operand).
        if (subject.addressIdentifier() is { } sai)
            value = inPlace = host.Ptr.AddressIdentifierOperand(sai, "an EVALUATE selection subject");
        else
        {
            if (BareOperand(subject) is not { } vo) return null;
            if (slot.Bare.Form is BareOperandForm.ConditionName or BareOperandForm.SwitchStatus) return null;
            value = inPlace = EvaluateOperands.BindValueOperand(host, vo);
        }
        // ⛔ NeedsIntermediate, NOT `Uses > 1` (kb/Work PB396). The §2023:14.9.25.4 GR1 argument this slot already
        // carried — at ONE use the single render IS the one evaluation, so nothing is observable and no
        // intermediate is created — holds only when that use is the FIRST arm's. At ZERO uses (every object
        // paired with this subject is ANY) it has no premise at all: nothing renders the subject, so without
        // the intermediate GR3's evaluation never happens. And at one use in a LATER arm the render sits in an
        // `else if` an earlier arm can skip, which is not "the beginning of the execution of the EVALUATE
        // statement". Materializing gives it the one PRE-op store §2023:14.9.13.4 GR3 requires, at the head.
        if (!slot.NeedsIntermediate || host.SendingValue.Materialize(value, "evaluate") is not { } frozen) return value;
        // The intermediate is the implementor's device; every §2023:8.8.4.2 operand rule is about the operand WRITTEN,
        // so the relation checkpoint is told which one this stands for (kb/Work PB1468 — an index-name subject's
        // intermediate is a numeric item, and `EVALUATE I1 WHEN I2 WHEN IDA` was refused as numeric-vs-index).
        // ExpressionAsWritten follows the written operand: the intermediate stands for it, and an arithmetic or
        // parenthesized subject is not identifier-1 of a class partial (§2023:8.8.4.4.2; kb/Work PB4804, PB4763).
        var intermediate = new BoundFieldOperand(frozen) { ExpressionAsWritten = value.ExpressionAsWritten };
        host.Cond.RecordIntermediate(intermediate, value);
        return intermediate;
    }

    /// <summary>How the statement's WHEN phrases USE selection subject <c>index</c> — the two facts that
    /// together decide whether §2023:14.9.13.4 GR3's evaluation needs the implementor's intermediate result item.
    /// <para><c>Uses</c> counts the RELATIONAL uses: GR4 a) 5.'s range form pairs the subject against BOTH
    /// bounds, so it counts twice, and <c>WHEN OTHER</c> pairs with nothing at all. ⛔ <c>ANY</c> counts ZERO
    /// (kb/Work PB396): GR4 a) 1. makes the result true WITHOUT consulting the subject, so an ANY object emits
    /// no render of it — the comment this replaced claimed counting ANY as a use "only over-materializes, never
    /// under-", and the opposite was true: an EVALUATE whose every object is ANY counted one use, skipped the
    /// intermediate, and then evaluated the subject NOWHERE.</para>
    /// <para><c>ReadByFirstPhrase</c> (the use is in the first phrase AND every earlier pair of it is ANY, kb/Work PB6232) is the other half, and it is what makes a single use safe to leave
    /// un-hoisted: the one render then sits in the FIRST arm's condition, which the emitted if/else-if chain
    /// always executes, so it happens "at the beginning of the execution of the EVALUATE statement". A single
    /// use in a LATER arm does not — an earlier arm can select and the render is skipped — so that shape takes
    /// the intermediate too.</para></summary>
    private static SubjectUsage SubjectUsageOf(Core.EvaluateStatementContext ev, int index)
    {
        int uses = 0;
        bool readByFirst = false, seenFirst = false;
        foreach (var clause in ev.evaluateWhenClause())
            foreach (var phrase in clause.evaluateWhenPhrase())
            {
                var objects = phrase.evaluateWhenItem();
                var item = index < objects.Length ? objects[index] : null;
                bool reads = item is not null && item.ANY() is null;
                // The pairs of a phrase are AND-ed and stop at the first false one (GR4 c; kb/Work PB6232), so the render of subject
                // `index` runs unconditionally only when every pair before it is ANY, which is true without a render.
                if (!seenFirst) { readByFirst = reads && AllAnyBefore(objects, index); seenFirst = true; }
                if (reads) uses += item!.valueRange() is not null ? 2 : 1;
            }
        return new SubjectUsage(uses, readByFirst);
    }

    private static bool AllAnyBefore(Core.EvaluateWhenItemContext[] objects, int index)
    {
        for (int j = 0; j < index && j < objects.Length; j++) if (objects[j].ANY() is null) return false;
        return true;
    }

    /// <summary>The §2023:14.9.13.4 GR3 usage profile of one selection subject — see <see cref="SubjectUsageOf"/>.</summary>
    internal readonly record struct SubjectUsage(int Uses, bool ReadByFirstPhrase)
    {
        /// <summary>True unless the subject's ONE relational use is the first arm's, which is the only shape in
        /// which the un-hoisted render is itself GR3's evaluation.</summary>
        public bool NeedsIntermediate => !(Uses == 1 && ReadByFirstPhrase);
    }
}
