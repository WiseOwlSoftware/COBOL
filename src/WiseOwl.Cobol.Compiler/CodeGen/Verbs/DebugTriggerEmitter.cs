// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.CodeGen.Emit;

namespace WiseOwl.Cobol.CodeGen;

using static WiseOwl.Cobol.CodeGen.Emit.EmitText;

/// <summary>
/// ⛔ THE BACKEND HALF OF THE X3.23-1985 DATA-NAME, ALL REFERENCES OF, FILE-NAME AND CD-NAME USE FOR DEBUGGING TRIGGERS (general rules 2, 5-8, 11, 14,
/// 15, 23, 24; docs/CONTROL_FLOW_DESIGN.md D20). The binder decided every <see cref="DebugFire"/> (<c>DebugTriggerPlanner</c>); this
/// places them. Each fire renders ONE guarded call, <c>if (__DebugReady) __RunDebugRef(line, name, contents, section, occurrences…)</c>:
/// the register is space-filled and set (<c>DebugItem.PopulateReference</c>) and the debugging section runs over its pc range. The guard
/// comes first, so a program run with the object-time switch off evaluates no operand it would not otherwise read.
/// <para>WHERE A FIRE LANDS is its <see cref="DebugWhen"/>. <see cref="DebugWhen.Before"/> and <see cref="DebugWhen.After"/> bracket the
/// statement here. <see cref="DebugWhen.BeforeIo"/> (WRITE, REWRITE: after the FROM move) and <see cref="DebugWhen.AfterAction"/> (a RECEIVE that
/// took its data, a READ that delivered a record, the I-O of a DELETE or START) are interior to a verb, so the wrapper only HOLDS them and the verb's emitter lands them at its own point
/// (<see cref="LandBeforeIo"/>, <see cref="LandAfterAction"/>); the loop phases are held for the PERFORM emitter the same way
/// (<see cref="TakeLoopFires"/>). A fire the verb never landed is an internal error raised at the wrapper — never a trigger that silently
/// did not run.</para>
/// <para>DEBUG-CONTENTS is the item's storage image (rule 24 c: "the content of the data item referenced by identifier-1 at the time that
/// control passes to the debugging section"; rule 11: no conversion), a counter register's storage image (LINAGE-COUNTER, LINE-COUNTER, PAGE-COUNTER: no place stands for it), the CD's record-area image for a cd-name (rule 23 e), the file's record area for a READ and spaces for every other file-name fire (rule 23 c, d); DEBUG-SUB-n are the occurrence numbers of
/// the reference's subscripted levels, outermost first (rule 14).</para>
/// </summary>
internal sealed class DebugTriggerEmitter(EmitContext ctx, PlaceTable places, NumericRenderer num)
{
    private IReadOnlyList<DebugFire> _beforeIo = [];
    private IReadOnlyList<DebugFire> _afterAction = [];
    private LoopFires? _loop;
    private PhraseFlags? _phrases;
    private IReadOnlyDictionary<DebugFire, string> _stored = new Dictionary<DebugFire, string>();
    private int _line;
    /// <summary>The after-statement fires of the statements now being emitted, innermost first: what a nested transfer of control must run first.</summary>
    private readonly Stack<(IReadOnlyList<DebugFire> Fires, int Line, PhraseFlags Flags)> _owedAfter = new();

    /// <summary>The "this phrase was evaluated" flags of one EVALUATE or SEARCH statement's fires (<see cref="DebugFire.Phrases"/>): a local per phrase
    /// index, declared before the statement, set by the verb's emitter where the phrase's condition begins to be evaluated
    /// (<see cref="Mark"/>) and read by the fires that name the phrase.</summary>
    internal sealed class PhraseFlags(IReadOnlyDictionary<int, string> names, IReadOnlyDictionary<DebugFire, string>? stored = null)
    {
        /// <summary>No phrase is flagged: the statement has no fire that depends on one.</summary>
        public static readonly PhraseFlags None = new(new Dictionary<int, string>());

        /// <summary>The same phrase flags, with the "the statement stored into the item" flag of each <see cref="DebugFire.WhenStored"/> fire.</summary>
        public PhraseFlags WithStored(IReadOnlyDictionary<DebugFire, string> flags) => new(names, flags);

        /// <summary>The "stored" flag of each fire that runs only when the statement stored into its item (<see cref="DebugFire.WhenStored"/>), by fire.</summary>
        public IReadOnlyDictionary<DebugFire, string> Stored { get; } = stored ?? new Dictionary<DebugFire, string>();

        /// <summary><paramref name="condition"/> with the assignment that records phrase <paramref name="index"/> as evaluated in front of it
        /// (unchanged when no fire depends on the phrase). The assignment is part of the condition's own expression, so it runs exactly when
        /// the condition does.</summary>
        public string Mark(int index, string condition) =>
            names.TryGetValue(index, out var flag) ? $"({flag} = true) && ({condition})" : condition;

        /// <summary>The C# condition under which <paramref name="fire"/> runs: "any phrase it names was evaluated" and "the statement stored into the item",
        /// or null for an unguarded fire.</summary>
        public string? Guard(DebugFire fire)
        {
            string? phrases = fire.Phrases is null ? null : $"({string.Join(" || ", fire.Phrases.Select(k => names[k]))})";
            string? stored = Stored.TryGetValue(fire, out var flag) ? flag : null;
            return phrases is not null && stored is not null ? $"{phrases} && {stored}" : phrases ?? stored;
        }
    }

    /// <summary>The loop-phase fires a PERFORM emitter takes, with the PERFORM statement's source line (DEBUG-LINE).</summary>
    internal sealed record LoopFires(IReadOnlyList<DebugFire> Fires, int Line);

    /// <summary>Emit one triggering statement: the fires before it, the statement through <paramref name="emitInner"/> (which returns whether it
    /// transfers control out of the paragraph case), the fires after it. Returns what <paramref name="emitInner"/> returned.</summary>
    public bool Emit(BoundDebugTriggered n, Func<BoundStatement, bool> emitInner)
    {
        var (savedIo, savedAction, savedLoop, savedPhrases, savedLine, savedStored) = (_beforeIo, _afterAction, _loop, _phrases, _line, _stored);
        _line = n.SourceLine;
        var fires = IdentifiedOnce(n.Fires);
        var phraseIndices = fires.Where(f => f.Phrases is not null).SelectMany(f => f.Phrases!).Distinct().Order().ToList();
        var flags = PhraseFlags.None;
        if (phraseIndices.Count > 0)
        {
            int id = ctx.Names.NextDebugPhrases();
            flags = new PhraseFlags(phraseIndices.ToDictionary(k => k, k => $"__dbgPhrase{id}_{k}"));
            foreach (var k in phraseIndices) ctx.Writer.Line($"bool __dbgPhrase{id}_{k} = false;");
        }
        _phrases = phraseIndices.Count > 0 ? flags : null;
        // The fires of the item alone run only when the statement stored into it: each owns a flag, true unless the verb's emitter finds its store skipped
        // (StoreFlagFor: a receiver whose ON SIZE ERROR path kept its content), so a verb with no such path needs no cooperation (kb/Work PB5296).
        var stored = new Dictionary<DebugFire, string>(ReferenceEqualityComparer.Instance);
        foreach (var f in fires.Where(f => f.WhenStored)) stored[f] = $"__dbgStored{ctx.Names.NextDebugPhrases()}";
        foreach (var name in stored.Values) ctx.Writer.Line($"bool {name} = true;");
        _stored = stored;
        if (stored.Count > 0) flags = flags.WithStored(stored);
        _beforeIo = Of(fires, DebugWhen.BeforeIo);
        _afterAction = Of(fires, DebugWhen.AfterAction);
        var loop = fires.Where(f => f.When is DebugWhen.LoopInit or DebugWhen.LoopAugment or DebugWhen.LoopTest).ToList();
        _loop = loop.Count > 0 ? new LoopFires(loop, n.SourceLine) : null;
        var after = Of(fires, DebugWhen.After);
        bool owes = after.Count > 0;
        if (owes) _owedAfter.Push((after, n.SourceLine, flags));
        try
        {
            foreach (var fire in Of(fires, DebugWhen.Before)) EmitFire(fire, n.SourceLine, flags);
            bool terminated = emitInner(n.Inner);
            if (_beforeIo.Count > 0 || _afterAction.Count > 0 || _loop is not null || _phrases is not null)
                throw new InvalidOperationException(
                    $"the debug fires of a statement at line {n.SourceLine} were never landed by its verb's emitter "
                    + $"(BeforeIo {_beforeIo.Count}, AfterAction {_afterAction.Count}, loop phases {_loop?.Fires.Count ?? 0}, "
                    + $"phrase flags {_phrases is not null}); "
                    + $"{n.Inner.GetType().Name} has no emission point for them (docs/CONTROL_FLOW_DESIGN.md D20)");
            if (!terminated)
                foreach (var fire in after) EmitFire(fire, n.SourceLine, flags);
            return terminated;
        }
        finally
        {
            if (owes) _owedAfter.Pop();
            (_beforeIo, _afterAction, _loop, _phrases, _line, _stored) = (savedIo, savedAction, savedLoop, savedPhrases, savedLine, savedStored);
        }
    }

    /// <summary>⛔ The statement emitter calls this immediately before it emits a statement that transfers control away
    /// (<see cref="IBoundTransfersControl"/>) from inside the statements now being emitted: a statement that triggers an after-statement
    /// debugging section and holds the transfer in one of its imperative phrases (<c>IF D = 1 GO TO P</c>, <c>ADD 1 TO E NOT ON SIZE ERROR
    /// GO TO P</c>) has finished its own action when the nested statement runs (rule 7: the nested statement is another statement), and
    /// the code after the transfer is never reached, so its section runs here, innermost statement first. A path that does not take the
    /// transfer reaches the statement's ordinary after-statement fires, so each path runs each section once.</summary>
    public void LandBeforeTransfer()
    {
        foreach (var text in OwedTexts()) ctx.Writer.Line(text);
    }

    /// <summary>The same sections as <see cref="LandBeforeTransfer"/>, as the text of their statements joined into one string, for a transfer
    /// the emitter spells as one string (the RESUME landing, <c>DispatchState.ResumeTransfer</c>); empty when nothing is owed.</summary>
    public string OwedBeforeTransferText() => string.Join(" ", OwedTexts());

    /// <summary>The statement of each section owed, innermost statement first.</summary>
    private IEnumerable<string> OwedTexts() =>
        _owedAfter.SelectMany(owed => owed.Fires.Select(fire => FireText(fire, owed.Line, owed.Flags)));

    private static List<DebugFire> Of(IReadOnlyList<DebugFire> fires, DebugWhen when) => fires.Where(f => f.When == when).ToList();

    /// <summary>⛔ THE REFERENCE A FIRE DESCRIBES IS IDENTIFIED ONCE, WHEN THE STATEMENT BEGINS. A subscript is "evaluated only once as the first operation of
    /// the execution of that statement" (§1985:IV.4.3.8), and DEBUG-SUB-n and DEBUG-CONTENTS describe "the occurrence number of each level" of that reference
    /// (§1985:XV.3.2.4 GR14) and the content of the item it names "at the time that control passes to the debugging section" (§1985:XV.3.2.4 GR24). A <see cref="Place"/> renders its subscripts at each point of use, so a fire that
    /// runs after <c>MOVE TE (SUB) TO SUB</c> would describe the occurrence the NEW SUB names; each data fire's place is therefore frozen into statement locals
    /// before anything of the statement runs (<see cref="PlaceIdentification.Freeze(Place, Func{string, string})"/>, the mechanism INSPECT, STRING, UNSTRING and
    /// RECEIVE identify their operands by; kb/Work PB5294, PB5279). Not frozen: a loop phase (each evaluation of a VARYING, AFTER or UNTIL phrase is the
    /// statement that references the item, so its own subscripts are read then), a fire guarded by a phrase (the phrase may never be evaluated, and
    /// identifying its operands first could raise an exception the statement would not), and a fire with no data place (file-name, cd-name, counter register).</summary>
    private IReadOnlyList<DebugFire> IdentifiedOnce(IReadOnlyList<DebugFire> fires)
    {
        if (!fires.Any(Identifiable)) return fires;
        var hoist = PlaceIdentification.Hoister(ctx);
        return [.. fires.Select(f => Identifiable(f) ? f with { Referenced = PlaceIdentification.Freeze(f.Referenced!, hoist) } : f)];

        static bool Identifiable(DebugFire f) =>
            f is { Referenced: not null, Phrases: null, Contents: DebugContents.Referenced }
            && f.Referenced.Undecorated is MemberPlace or DynTablePlace
            && f.Target.Kind is DebugTargetKind.DataName or DebugTargetKind.AllReferences
            && f.When is not (DebugWhen.LoopInit or DebugWhen.LoopAugment or DebugWhen.LoopTest);
    }

    /// <summary>The flag that records whether the statement now being emitted stored into <paramref name="receiver"/>'s item, when a fire of the item alone
    /// waits on it (<see cref="DebugFire.WhenStored"/>), else null. An arithmetic statement with an ON SIZE ERROR path clears it where the receiver's store was
    /// skipped (<see cref="ArithmeticEmitter"/>), so §1985:XV.3.2.4 GR6's "causes the content … to be changed" is asked of the path the statement took.</summary>
    public string? StoreFlagFor(Place receiver) =>
        _stored.Where(kv => ReferenceEquals(kv.Key.Target.Item, receiver.Item)).Select(kv => kv.Value).FirstOrDefault();

    /// <summary>Every flag <see cref="StoreFlagFor"/> can answer for the statement now being emitted: the ones a statement that failed before it stored anywhere clears.</summary>
    public IEnumerable<string> StoreFlags => _stored.Values;

    /// <summary>A WRITE or REWRITE emitter calls this immediately after its FROM phrase's implicit move and before the I-O
    /// (general rules 5 a and 6 a).</summary>
    public void LandBeforeIo()
    {
        var fires = _beforeIo;
        _beforeIo = [];
        foreach (var fire in fires) EmitFire(fire, _line, PhraseFlags.None);
    }

    /// <summary>An EVALUATE or SEARCH emitter takes the phrase flags of the statement it emits, once, before it emits anything nested (a nested
    /// statement is another statement, with its own fires), and wraps each phrase's condition in <see cref="PhraseFlags.Mark"/>.</summary>
    public PhraseFlags TakePhraseFlags()
    {
        var flags = _phrases ?? PhraseFlags.None;
        _phrases = null;
        return flags;
    }

    /// <summary>Whether the statement now being emitted holds a fire its verb's emitter has yet to land at <see cref="LandAfterAction"/>:
    /// a verb whose success block exists only for its phrases (the keyed READ) opens it for these too.</summary>
    public bool HasAfterAction => _afterAction.Count > 0;

    /// <summary>A READ, RETURN, RECEIVE, DELETE or START emitter calls this where the verb's own action AND its phrases' imperative statements are
    /// done (general rules 2 and 8 b: "immediately after execution of that statement", and the phrases are inside it, §1985:XIV.3.5.4 GR4; a RETURN's
    /// and RECEIVE's only on the path that delivered data; a READ's on that path, and on the one that delivered none through
    /// <see cref="LandFileNameAfterFailure"/>). While the phrases are emitted the fires are owed (<see cref="OweAfterAction"/>).</summary>
    public void LandAfterAction()
    {
        LandAfterActionKeeping();
        _afterAction = [];
    }

    /// <summary>Held after-action fires are owed while the verb's phrases are emitted, as an enclosing statement's after-statement fires are: a
    /// transfer of control nested in a phrase (<see cref="LandBeforeTransfer"/>) runs them first, so each path runs each section once. The verb's
    /// emitter wraps the phrases the section follows in <c>using (Debug.OweAfterAction())</c> and lands the fires after it.</summary>
    public IDisposable OweAfterAction()
    {
        if (_afterAction.Count == 0) return NoOwed.Instance;
        _owedAfter.Push((_afterAction, _line, PhraseFlags.None));
        return new Owed(_owedAfter);
    }

    private sealed class NoOwed : IDisposable
    {
        public static readonly NoOwed Instance = new();
        public void Dispose() { }
    }

    private sealed class Owed(Stack<(IReadOnlyList<DebugFire> Fires, int Line, PhraseFlags Flags)> stack) : IDisposable
    {
        public void Dispose() => stack.Pop();
    }

    /// <summary>As <see cref="LandAfterAction"/>, but the fires stay held for the READ's other arm (<see cref="LandFileNameAfterFailure"/>), which
    /// lands the file-name section and then <see cref="DiscardAfterAction"/>s the rest.</summary>
    public void LandAfterActionKeeping()
    {
        foreach (var fire in _afterAction) EmitFire(fire, _line, PhraseFlags.None);
    }

    /// <summary>Whether a held after-action fire is a file-name section (rule 2): the only kind a READ that delivered no record runs.</summary>
    public bool HasFileNameAfterAction => _afterAction.Any(f => f.Target.Kind is DebugTargetKind.FileName);

    /// <summary>The READ arm that delivered no record and ran no AT END or INVALID KEY statement: rule 2 b still runs the file-name section, but
    /// nothing else held for the statement. An INTO receiver's section does not run ("If identifier-1 is specified in a phrase that is not
    /// executed or evaluated, the associated debugging section is not executed", rule 5; no content changed, rule 6 c), and DEBUG-CONTENTS holds
    /// spaces, no record having been read (rules 23 c, 11). The fires stay held for <see cref="DiscardAfterAction"/>.</summary>
    public void LandFileNameAfterFailure()
    {
        foreach (var fire in _afterAction.Where(f => f.Target.Kind is DebugTargetKind.FileName))
            EmitFire(fire with { Contents = DebugContents.Spaces }, _line, PhraseFlags.None);
    }

    /// <summary>The READ's last arm has landed what it owes: the held fires are done.</summary>
    public void DiscardAfterAction() => _afterAction = [];

    /// <summary>A PERFORM emitter takes the loop-phase fires of the statement it emits, once, before it emits anything nested (a nested
    /// statement is another statement, with its own fires).</summary>
    public LoopFires? TakeLoopFires()
    {
        var loop = _loop;
        _loop = null;
        return loop;
    }

    /// <summary>Emit the fires of <paramref name="loop"/> that belong to <paramref name="when"/> at PERFORM level <paramref name="level"/>.</summary>
    public void EmitLoopPhase(LoopFires? loop, DebugWhen when, int level)
    {
        if (loop is null) return;
        foreach (var fire in loop.Fires)
            if (fire.When == when && fire.Level == level) EmitFire(fire, loop.Line, PhraseFlags.None);
    }

    /// <summary>Whether <paramref name="loop"/> has a fire for <paramref name="when"/> at <paramref name="level"/>.</summary>
    public static bool HasLoopPhase(LoopFires? loop, DebugWhen when, int level) =>
        loop is not null && loop.Fires.Any(f => f.When == when && f.Level == level);

    private void EmitFire(DebugFire fire, int line, PhraseFlags flags) => ctx.Writer.Line(FireText(fire, line, flags));

    private string FireText(DebugFire fire, int line, PhraseFlags flags)
    {
        string contents = fire.Contents switch
        {
            DebugContents.Register => OperandText.CounterRegisterStorageImage(new BoundComputedOperand(fire.Target.Register!), num),   // rule 24 c: the register has no place
            DebugContents.Spaces => "\"\"",   // rule 23 d: the register is space-filled, and nothing is moved into it
            DebugContents.FileRecord => places.RecordArea(fire.Target.File!) is { } area ? OperandText.RecordAreaImage(area) : "\"\"",   // rule 23 c
            _ when fire.Target.Kind is DebugTargetKind.CdName => OperandText.RecordAreaImage(fire.Referenced!),
            _ => DataItemImage(fire.Referenced!),
        };
        string occurrences = fire.Referenced is null ? "" : string.Concat(OccurrenceNumbers(fire.Referenced).Select(o => ", " + o));
        string guard = flags.Guard(fire) is { } phrases ? $"__DebugReady && ({phrases})" : "__DebugReady";
        return $"if ({guard}) __RunDebugRef({line}, {CsLiteral(fire.Target.Name)}, {contents}, "
            + $"{fire.Target.SectionStartPc}, {fire.Target.SectionEndPc}{occurrences});";
    }

    /// <summary>⛔ DEBUG-CONTENTS of a data-name target: "the content of the data item referenced by identifier-1" (X3.23-1985 USE FOR
    /// DEBUGGING general rule 24 c) — the WHOLE item, never the window a reference-modified statement used (syntax rule 12 bars
    /// reference modification of identifier-1, and the item is what the rule moves), and moved "exactly as if it was an alphanumeric to
    /// alphanumeric elementary move with no conversion of data from one form of internal representation to another" (general rule 11):
    /// its storage image, the one channel (<see cref="OperandText.AsStorageImage"/>) a binary, packed or float item's bytes travel in.
    /// <para>A WHOLE table (a <see cref="TableAllPlace"/>: SEARCH's identifier-1, a table SORT's data-name-2 and keys, INITIALIZE of a
    /// whole dynamic-capacity table; kb/Work PB4855) is every current occurrence of the item, so its content is their storage images
    /// in storage order (<c>CobolTable.AllImage</c>, each level's range read as the walk enters it).</para></summary>
    private static string DataItemImage(Place referenced)
    {
        if (referenced is not TableAllPlace whole) return OperandText.AsStorageImage(WholeItem(referenced), "the DEBUG-CONTENTS of");
        string element = OperandText.AsStorageImage(whole.Element.Undecorated, "the DEBUG-CONTENTS of");
        return RuntimeApi.TableAllImage(PlaceRenderer.AllCountLambdas(whole), $"{LocalNames.Of(whole.Indices.Local)} => {element}");
    }

    /// <summary>The whole item <paramref name="place"/> references, without the window a statement used (a reference modifier, a slice) but WITH the
    /// current-extent wrapper of a variable-length group: its content is the occurrences the OCCURS DEPENDING ON item says it has, "only that part of the
    /// table area that is specified by the value of the data item referenced by data-name-1 at the start of the operation" (§1985:VI.5.8.4 GR3), and DEBUG-CONTENTS is
    /// moved as an alphanumeric item with the rest space-filled (§1985:XV.3.2.4 GR11, GR24 c; kb/Work PB5293).</summary>
    private static Place WholeItem(Place place) => place switch
    {
        OdoGroupPlace odo => odo with { Inner = WholeItem(odo.Inner) },
        PlaceDecorator d => WholeItem(d.Inner),
        _ => place,
    };

    /// <summary>The occurrence number each subscripted level of the reference computes, outermost first (general rule 14: "the occurrence
    /// number of each level is entered in DEBUG-SUB-1, DEBUG-SUB-2, DEBUG-SUB-3"), as C# <c>long</c> expressions read when the section runs.
    /// A whole table names no occurrence of the levels it ranges over (its trailing levels, enumerated by its own index vector), so those
    /// stay spaces (rule 11).</summary>
    private static IEnumerable<string> OccurrenceNumbers(Place place)
    {
        var path = place.Undecorated switch
        {
            MemberPlace m => m.Path,
            DynTablePlace d => d.Path,
            RedefViewPlace r => r.Backing,
            _ => null,
        };
        if (path is null) yield break;
        int emitted = 0;
        foreach (var segment in path.Segments)
        {
            Position? index = segment switch
            {
                FixedTableSegment f => f.OneBasedIndex,
                DynTableSegment d => d.OneBasedIndex,
                _ => null,
            };
            if (index is null) continue;
            if (place is TableAllPlace whole && index is PositionLocalElement e && e.Vector == whole.Indices) yield break;
            string occurrence = PositionRenderer.Render(index);
            if (++emitted > WiseOwl.Cobol.Runtime.DebugItem.MaxSubscriptLevels) yield break;
            yield return occurrence;
        }
    }
}
