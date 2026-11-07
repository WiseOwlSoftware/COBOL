// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding.Bound;

/// <summary>
/// ⛔ THE ONE PLACE THE X3.23-1985 USE FOR DEBUGGING TRIGGER RULES ARE APPLIED TO A STATEMENT (general rules 5, 6, 7 and 8;
/// docs/CONTROL_FLOW_DESIGN.md D20). <see cref="StatementBinder"/>'s one funnel hands it each bound statement and the program's
/// data-name, ALL REFERENCES OF, cd-name and file-name targets; it answers every <see cref="DebugFire"/> the statement causes, and the emitter
/// only places them. The rules, each cited to the 1985 text (USE FOR DEBUGGING general rules; quoted briefly):
/// <list type="bullet">
/// <item>Rule 5 — ALL REFERENCES OF identifier-1: the section runs "for every statement that explicitly references identifier-1".
/// Rule 6 — identifier-1 alone: it runs after a statement that "explicitly references and causes the content of the data item
/// … to be changed" (<see cref="BoundStores.DirectStoreOf"/>: a store the statement's own ON SIZE ERROR path skipped is still a
/// store, as GnuCOBOL decides).</item>
/// <item>Rule 5 a / 6 a — WRITE and REWRITE: immediately before the I-O and after the implicit move of FROM
/// (<see cref="DebugWhen.BeforeIo"/>), for either kind of target and whether or not the item changes.</item>
/// <item>Rule 5 b — GO TO DEPENDING ON: immediately before control transfers (<see cref="DebugWhen.Before"/>), ALL REFERENCES only.</item>
/// <item>Rule 5 c / 6 b — PERFORM: after each initialization, modification or evaluation of a VARYING, AFTER or UNTIL phrase that
/// references the item (<see cref="DebugWhen.LoopInit"/>, <see cref="DebugWhen.LoopAugment"/>, <see cref="DebugWhen.LoopTest"/>);
/// a TIMES operand is a plain reference (rule 5 d).</item>
/// <item>Rule 7 — "not … more than once for a specific operand as a result of the execution of a single statement"; "within an
/// imperative statement, each individual occurrence of an imperative verb identifies a separate statement": the planner reads the
/// statement ITSELF (<see cref="BoundReferences"/>, <see cref="BoundStores.DirectStoreOf"/>), and a statement nested in it is planned
/// by its own funnel call.</item>
/// <item>Rule 2 — file-name-1: after OPEN, CLOSE, READ (that delivered a record), DELETE and START (<see cref="PlanFile"/>).</item>
/// <item>Rule 8 — cd-name-1: after ENABLE, DISABLE, SEND and ACCEPT MESSAGE COUNT, and after a RECEIVE that "does not result in the
/// execution of the NO DATA imperative-statement" (<see cref="DebugWhen.AfterAction"/>). PURGE is not listed and runs nothing.</item>
/// </list>
/// A reference that is only a qualifier is no reference (rule 9): a qualifier names no place of a statement, so it never appears here.
/// </summary>
internal static class DebugTriggerPlanner
{
    /// <summary>The fires <paramref name="statement"/> causes for <paramref name="targets"/>, empty when none.</summary>
    public static IReadOnlyList<DebugFire> Plan(BoundStatement statement, IReadOnlyList<BoundDebugTarget> targets)
    {
        var fires = new List<DebugFire>();
        foreach (var leaf in Leaves(statement))
            foreach (var target in targets)
                PlanLeaf(leaf, target, fires);
        return OncePerOperand(fires);
    }

    /// <summary>The statements one written statement is made of: the transparent wrappers opened, each node that does a verb's work
    /// left whole (a hoisted operand-evaluation step is such a node: it evaluates an operand of the statement).</summary>
    private static IEnumerable<BoundStatement> Leaves(BoundStatement s)
    {
        switch (s)
        {
            case BoundSequence seq:
                foreach (var step in seq.Steps) foreach (var l in Leaves(step)) yield return l;
                break;
            case BoundImplicitSeries series:
                foreach (var member in series.Members) foreach (var l in Leaves(member)) yield return l;
                break;
            case BoundEcChecked ec: foreach (var l in Leaves(ec.Inner)) yield return l; break;
            case BoundActivationSite site: foreach (var l in Leaves(site.Inner)) yield return l; break;
            case BoundReceiverBrackets brackets: foreach (var l in Leaves(brackets.Inner)) yield return l; break;
            case BoundDebugTriggered debug: foreach (var l in Leaves(debug.Inner)) yield return l; break;
            default: yield return s; break;
        }
    }

    private static void PlanLeaf(BoundStatement leaf, BoundDebugTarget target, List<DebugFire> fires)
    {
        switch (leaf)
        {
            case BoundInlinePerform { Control: var control } p:
                PlanPerform(p, control, target, fires);
                return;
            case BoundOutOfLinePerform { Control: var control } p:
                PlanPerform(p, control, target, fires);
                return;
        }
        if (target.Kind is DebugTargetKind.CdName)
        {
            PlanCd(leaf, target, fires);
            return;
        }
        if (target.Kind is DebugTargetKind.FileName)
        {
            PlanFile(leaf, target, fires);
            return;
        }
        // The INTO phrase of READ and RETURN executes only when the verb delivered a record ("not executed or evaluated", rules 5 and 6): it is
        // planned as its own moment, after the verb's action, and the rest of the statement is planned without it.
        var into = (leaf as IBoundIntoPhrase)?.IntoMove;
        if (into is not null) PlanInto(into, target, fires);
        // The conditional phrases of EVALUATE and SEARCH are moments of their own too: an operand written in a phrase the statement never evaluated
        // is not referenced by this execution (rules 5 and 6, the closing sentence), so each phrase is planned apart and guarded by its own flag.
        var phrases = (leaf as IBoundEvaluatedPhrases)?.PhraseOperands ?? [];
        if (phrases.Count > 0) PlanPhrases(phrases, target, fires);
        var referenced = Find(BoundReferences.Of(leaf, [into, .. phrases]), target);
        if (referenced is not { } hit) return;   // a statement that does not reference the item runs nothing (a qualifier is no reference, rule 9)
        switch (leaf)
        {
            // Rule 5 a / 6 a: WRITE and REWRITE run the section before the I-O for EITHER kind of target.
            case BoundWrite or BoundKeyedWrite or BoundRewrite or BoundKeyedRewrite:
                fires.Add(FireFor(target, hit, DebugWhen.BeforeIo));
                return;
        }
        if (target.Kind is DebugTargetKind.AllReferences)
        {
            // Rule 5 b: GO TO DEPENDING ON runs before control is transferred; rule 5 d: any other statement, after it.
            fires.Add(FireFor(target, hit, leaf is BoundGoToDepending ? DebugWhen.Before : DebugWhen.After));
            return;
        }
        // Rule 6 c: the item alone runs the section after a statement that CHANGES it.
        if (target.Item is { } item && BoundStores.DirectStoreOf(leaf, item) is { } stored && !(into?.Targets.Any(t => ReferenceEquals(t, stored)) ?? false))
            fires.Add(FireFor(target, new Hit(stored), DebugWhen.After));
    }

    /// <summary>The conditional phrases of an EVALUATE or SEARCH that reference an ALL REFERENCES target: ONE after-statement fire naming every phrase
    /// the operand is written in, which runs when the statement evaluated any of them (rule 7: once per operand per statement). A phrase holds
    /// conditions only, so the item alone (rule 6) has no store to run for.</summary>
    private static void PlanPhrases(IReadOnlyList<object> phrases, BoundDebugTarget target, List<DebugFire> fires)
    {
        if (target.Kind is not DebugTargetKind.AllReferences) return;
        Hit? first = null;
        var indices = new List<int>();
        for (int k = 0; k < phrases.Count; k++)
            if (Find(BoundReferences.OfOperand(phrases[k]), target) is { } found)
            {
                first ??= found;
                indices.Add(k);
            }
        if (first is { } head) fires.Add(FireFor(target, head, DebugWhen.After, phrases: indices));
    }

    /// <summary>The INTO phrase of a READ or RETURN (its implicit move of the record area into the receiver): the receiver is a written
    /// operand, run after the verb's action (<see cref="DebugWhen.AfterAction"/>, landed by the verb's emitter after the move) and only on the
    /// path that moved the record.</summary>
    private static void PlanInto(BoundMove into, BoundDebugTarget target, List<DebugFire> fires)
    {
        if (Find(BoundReferences.OfOperand(into.Targets), target) is not { } referenced) return;
        if (target.Kind is DebugTargetKind.AllReferences)
            fires.Add(FireFor(target, referenced, DebugWhen.AfterAction));
        else if (target.Item is { } item && BoundStores.DirectStoreOf(into, item) is { } stored)
            fires.Add(FireFor(target, new Hit(stored), DebugWhen.AfterAction));
    }

    /// <summary>Rule 8: a cd-name target is referenced by the communication statements of its own description, and the section
    /// reads the content of the CD's area (rule 23 e).</summary>
    private static void PlanCd(BoundStatement leaf, BoundDebugTarget target, List<DebugFire> fires)
    {
        if (leaf is not BoundCommunicationStatement n || n.Cd.Key != target.CdKey) return;
        switch (n.Verb)
        {
            case CommunicationVerb.Receive:
                fires.Add(new DebugFire(target, n.Area, DebugWhen.AfterAction));
                break;
            case CommunicationVerb.Enable or CommunicationVerb.Disable or CommunicationVerb.Send or CommunicationVerb.AcceptMessageCount:
                fires.Add(new DebugFire(target, n.Area, DebugWhen.After));
                break;
            // PURGE is not among rule 8's statements.
        }
    }

    /// <summary>Rule 2: a file-name target is referenced by the OPEN, CLOSE, READ, DELETE and START of its own file, and the section runs after
    /// the statement's own action. OPEN and CLOSE have no imperative phrase, so the section runs after the statement (<see cref="DebugWhen.After"/>);
    /// READ, DELETE and START do, and "each individual occurrence of an imperative verb identifies a separate statement" (rule 7), so the section
    /// runs when the verb's own I-O is done and before its phrases (<see cref="DebugWhen.AfterAction"/>, landed by the verb's emitter). A READ
    /// runs it when it delivered a record, with the entire record read in DEBUG-CONTENTS (rule 23 c), and when it delivered none and ran no AT END
    /// or INVALID KEY statement ("not resulting in the execution of an associated AT END or INVALID KEY imperative statement", rule 2 b; the
    /// verb's emitter lands it on that arm through <c>LandFileNameAfterFailure</c>, spaces in DEBUG-CONTENTS, no record having been read);
    /// every other reference leaves spaces there (rule 23 d). WRITE and
    /// REWRITE are not listed, nor are the SORT and MERGE verbs, RELEASE and RETURN: they run no file-name section (GnuCOBOL's typeck.c agrees).
    /// DELETE FILE (ISO/IEC 1989:2023 §2023:14.9.10 Format 2, not in the 1985 text) is a DELETE statement that references the file-name, and runs it
    /// as GnuCOBOL's cb_emit_delete_file does, after the I-O and before the exception test.</summary>
    private static void PlanFile(BoundStatement leaf, BoundDebugTarget target, List<DebugFire> fires)
    {
        var file = target.File!;
        var fire = leaf switch
        {
            BoundOpen open when open.Files.Any(f => ReferenceEquals(f.File, file)) => (DebugWhen.After, DebugContents.Spaces),
            BoundClose close when close.Files.Any(f => ReferenceEquals(f.File, file)) => (DebugWhen.After, DebugContents.Spaces),
            BoundRead read when ReferenceEquals(read.File, file) => (DebugWhen.AfterAction, DebugContents.FileRecord),
            BoundKeyedRead read when ReferenceEquals(read.File, file) => (DebugWhen.AfterAction, DebugContents.FileRecord),
            BoundKeyedDelete del when ReferenceEquals(del.File, file) => (DebugWhen.AfterAction, DebugContents.Spaces),
            BoundKeyedDeleteFile del when ReferenceEquals(del.File, file) => (DebugWhen.AfterAction, DebugContents.Spaces),
            BoundKeyedStart start when ReferenceEquals(start.File, file) => (DebugWhen.AfterAction, DebugContents.Spaces),
            _ => ((DebugWhen, DebugContents)?)null,
        };
        if (fire is { } f) fires.Add(new DebugFire(target, null, f.Item1, Contents: f.Item2));
    }

    /// <summary>Rules 5 c / 6 b: the loop-control phrase's phases; everything else the PERFORM holds is an ordinary reference
    /// (the TIMES count, rule 5 d), planned like any statement's.</summary>
    private static void PlanPerform(BoundStatement perform, BoundPerformControl control, BoundDebugTarget target, List<DebugFire> fires)
    {
        if (target.Kind is DebugTargetKind.CdName or DebugTargetKind.FileName) return;
        switch (control)
        {
            case PerformTimes times:
                if (target.Kind is DebugTargetKind.AllReferences && Find(BoundReferences.OfOperand(times.Count), target) is { } counted)
                    fires.Add(FireFor(target, counted, DebugWhen.After));
                break;
            case PerformUntil until:
                Phase(0, DebugWhen.LoopTest, until.Until);
                break;
            case PerformVarying varying:
                for (int k = 0; k < varying.Levels.Count; k++)
                {
                    var level = varying.Levels[k];
                    Phase(k, DebugWhen.LoopInit, level.Var, level.From);
                    Phase(k, DebugWhen.LoopAugment, level.Var, level.By);
                    Phase(k, DebugWhen.LoopTest, level.Until);
                }
                break;
        }
        // The statement's other operands (an out-of-line PERFORM names none; an inline one's body is nested statements).
        void Phase(int level, DebugWhen when, params object[] parts)
        {
            foreach (var part in parts)
                if (Find(BoundReferences.OfOperand(part), target) is { } reference)
                {
                    fires.Add(FireFor(target, reference, when, level));
                    return;
                }
        }
    }

    /// <summary>How a statement references a target: through <see cref="Place"/>, or through none for a read of a counter register.</summary>
    private readonly record struct Hit(Place? Place);

    /// <summary>The first reference to <paramref name="target"/> in <paramref name="reach"/>, or null. A data-name target is its item itself, a window
    /// of it or an occurrence of it, but not a group that contains it (a group reference does not "explicitly reference" a member, rule 5). A counter
    /// register (LINAGE-COUNTER, LINE-COUNTER, PAGE-COUNTER) is no place: a read of it is a register reference (<see cref="Reach.Registers"/>), and
    /// PAGE-COUNTER's receiving place carries the report's counter item.</summary>
    private static Hit? Find(Reach reach, BoundDebugTarget target)
    {
        if (target.Item is { } item && reach.Places.FirstOrDefault(p => ReferenceEquals(p.Item, item)) is { } place) return new Hit(place);
        return reach.Registers.Any(target.NamesRegister) ? new Hit(null) : null;
    }

    /// <summary>The fire of <paramref name="target"/> for the reference <paramref name="hit"/>; a counter register's DEBUG-CONTENTS is its register image.</summary>
    private static DebugFire FireFor(BoundDebugTarget target, Hit hit, DebugWhen when, int level = 0, IReadOnlyList<int>? phrases = null) =>
        new(target, hit.Place, when, level, phrases, target.Register is null ? DebugContents.Referenced : DebugContents.Register);

    /// <summary>Rule 7: one run per operand per statement. A target a statement reaches by two routes (WRITE record and its FROM
    /// item, a hoisted step and the verb) keeps the fire that is not the plain after-statement one, else the first.</summary>
    private static List<DebugFire> OncePerOperand(List<DebugFire> fires)
    {
        var kept = new List<DebugFire>();
        foreach (var fire in fires)
        {
            bool loop = fire.When is DebugWhen.LoopInit or DebugWhen.LoopAugment or DebugWhen.LoopTest;
            int held = loop ? -1 : kept.FindIndex(k => ReferenceEquals(k.Target, fire.Target) && k.When is not (DebugWhen.LoopInit or DebugWhen.LoopAugment or DebugWhen.LoopTest));
            if (held < 0) { kept.Add(fire); continue; }
            var existing = kept[held];
            // Two phrase-guarded fires of one operand are one fire that runs when either phrase was evaluated; an unguarded fire (the statement
            // evaluates the operand outright) subsumes a guarded one.
            if (existing is { When: DebugWhen.After, Phrases: { } held1 } && fire is { When: DebugWhen.After, Phrases: { } held2 })
                kept[held] = existing with { Phrases = [.. held1.Union(held2)] };
            else if (existing.When is DebugWhen.After && ((existing.Phrases is not null && fire.Phrases is null) || fire.When is not DebugWhen.After))
                kept[held] = fire;
        }
        return kept;
    }
}
