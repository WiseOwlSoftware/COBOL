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
/// control passes to the debugging section"; rule 11: no conversion), the CD's record-area image for a cd-name (rule 23 e), the file's record area for a READ and spaces for every other file-name fire (rule 23 c, d); DEBUG-SUB-n are the occurrence numbers of
/// the reference's subscripted levels, outermost first (rule 14).</para>
/// </summary>
internal sealed class DebugTriggerEmitter(EmitContext ctx, PlaceTable places)
{
    private IReadOnlyList<DebugFire> _beforeIo = [];
    private IReadOnlyList<DebugFire> _afterAction = [];
    private LoopFires? _loop;
    private PhraseFlags? _phrases;
    private int _line;
    /// <summary>The after-statement fires of the statements now being emitted, innermost first: what a nested transfer of control must run first.</summary>
    private readonly Stack<(IReadOnlyList<DebugFire> Fires, int Line, PhraseFlags Flags)> _owedAfter = new();

    /// <summary>The "this phrase was evaluated" flags of one EVALUATE or SEARCH statement's fires (<see cref="DebugFire.Phrases"/>): a local per phrase
    /// index, declared before the statement, set by the verb's emitter where the phrase's condition begins to be evaluated
    /// (<see cref="Mark"/>) and read by the fires that name the phrase.</summary>
    internal sealed class PhraseFlags(IReadOnlyDictionary<int, string> names)
    {
        /// <summary>No phrase is flagged: the statement has no fire that depends on one.</summary>
        public static readonly PhraseFlags None = new(new Dictionary<int, string>());

        /// <summary><paramref name="condition"/> with the assignment that records phrase <paramref name="index"/> as evaluated in front of it
        /// (unchanged when no fire depends on the phrase). The assignment is part of the condition's own expression, so it runs exactly when
        /// the condition does.</summary>
        public string Mark(int index, string condition) =>
            names.TryGetValue(index, out var flag) ? $"({flag} = true) && ({condition})" : condition;

        /// <summary>The C# condition under which <paramref name="fire"/> runs: "any phrase it names was evaluated", or null for an unguarded fire.</summary>
        public string? Guard(DebugFire fire) =>
            fire.Phrases is null ? null : string.Join(" || ", fire.Phrases.Select(k => names[k]));
    }

    /// <summary>The loop-phase fires a PERFORM emitter takes, with the PERFORM statement's source line (DEBUG-LINE).</summary>
    internal sealed record LoopFires(IReadOnlyList<DebugFire> Fires, int Line);

    /// <summary>Emit one triggering statement: the fires before it, the statement through <paramref name="emitInner"/> (which returns whether it
    /// transfers control out of the paragraph case), the fires after it. Returns what <paramref name="emitInner"/> returned.</summary>
    public bool Emit(BoundDebugTriggered n, Func<BoundStatement, bool> emitInner)
    {
        var (savedIo, savedAction, savedLoop, savedPhrases, savedLine) = (_beforeIo, _afterAction, _loop, _phrases, _line);
        _line = n.SourceLine;
        var phraseIndices = n.Fires.Where(f => f.Phrases is not null).SelectMany(f => f.Phrases!).Distinct().Order().ToList();
        var flags = PhraseFlags.None;
        if (phraseIndices.Count > 0)
        {
            int id = ctx.Names.NextDebugPhrases();
            flags = new PhraseFlags(phraseIndices.ToDictionary(k => k, k => $"__dbgPhrase{id}_{k}"));
            foreach (var k in phraseIndices) ctx.Writer.Line($"bool __dbgPhrase{id}_{k} = false;");
        }
        _phrases = phraseIndices.Count > 0 ? flags : null;
        _beforeIo = Of(n, DebugWhen.BeforeIo);
        _afterAction = Of(n, DebugWhen.AfterAction);
        var loop = n.Fires.Where(f => f.When is DebugWhen.LoopInit or DebugWhen.LoopAugment or DebugWhen.LoopTest).ToList();
        _loop = loop.Count > 0 ? new LoopFires(loop, n.SourceLine) : null;
        var after = Of(n, DebugWhen.After);
        bool owes = after.Count > 0;
        if (owes) _owedAfter.Push((after, n.SourceLine, flags));
        try
        {
            foreach (var fire in Of(n, DebugWhen.Before)) EmitFire(fire, n.SourceLine, flags);
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
            (_beforeIo, _afterAction, _loop, _phrases, _line) = (savedIo, savedAction, savedLoop, savedPhrases, savedLine);
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

    private static List<DebugFire> Of(BoundDebugTriggered n, DebugWhen when) => n.Fires.Where(f => f.When == when).ToList();

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

    /// <summary>A READ, RETURN, RECEIVE, DELETE or START emitter calls this where the verb's own action is done, before the phrases' imperative
    /// statements (general rules 2 and 8 b; a RETURN's and RECEIVE's only on the path that delivered data; a READ's on that path and on the one that delivered none and ran no AT END or INVALID KEY statement).</summary>
    public void LandAfterAction()
    {
        LandAfterActionKeeping();
        _afterAction = [];
    }

    /// <summary>As <see cref="LandAfterAction"/>, but the fires stay held for a later arm of the same verb: a READ lands its file-name section on
    /// the arm that delivered a record and again on the arm that delivered none and ran no AT END or INVALID KEY statement (rule 2 b), so the
    /// first arm keeps and the last consumes.</summary>
    public void LandAfterActionKeeping()
    {
        foreach (var fire in _afterAction) EmitFire(fire, _line, PhraseFlags.None);
    }

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
    /// its storage image, the one channel (<see cref="OperandText.AsStorageImage"/>) a binary, packed or float item's bytes travel in.</summary>
    private static string DataItemImage(Place referenced) =>
        OperandText.AsStorageImage(referenced.Undecorated, "the DEBUG-CONTENTS of");

    /// <summary>The occurrence number each subscripted level of the reference computes, outermost first (general rule 14: "the occurrence
    /// number of each level is entered in DEBUG-SUB-1, DEBUG-SUB-2, DEBUG-SUB-3"), as C# <c>long</c> expressions read when the section runs.</summary>
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
            string? occurrence = segment switch
            {
                FixedTableSegment f => PositionRenderer.Render(f.OneBasedIndex),
                DynTableSegment d => PositionRenderer.Render(d.OneBasedIndex),
                _ => null,
            };
            if (occurrence is null) continue;
            if (++emitted > WiseOwl.Cobol.Runtime.DebugItem.MaxSubscriptLevels) yield break;
            yield return occurrence;
        }
    }
}
