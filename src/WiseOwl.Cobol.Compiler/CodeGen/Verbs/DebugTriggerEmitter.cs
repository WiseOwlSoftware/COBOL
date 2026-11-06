// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.CodeGen.Emit;

namespace WiseOwl.Cobol.CodeGen;

using static WiseOwl.Cobol.CodeGen.Emit.EmitText;

/// <summary>
/// ⛔ THE BACKEND HALF OF THE X3.23-1985 DATA-NAME, ALL REFERENCES OF AND CD-NAME USE FOR DEBUGGING TRIGGERS (general rules 5-8, 11, 14,
/// 15, 23, 24; docs/CONTROL_FLOW_DESIGN.md D20). The binder decided every <see cref="DebugFire"/> (<c>DebugTriggerPlanner</c>); this
/// places them. Each fire renders ONE guarded call, <c>if (__DebugReady) __RunDebugRef(line, name, contents, section, occurrences…)</c>:
/// the register is space-filled and set (<c>DebugItem.PopulateReference</c>) and the debugging section runs over its pc range. The guard
/// comes first, so a program run with the object-time switch off evaluates no operand it would not otherwise read.
/// <para>WHERE A FIRE LANDS is its <see cref="DebugWhen"/>. <see cref="DebugWhen.Before"/> and <see cref="DebugWhen.After"/> bracket the
/// statement here. <see cref="DebugWhen.BeforeIo"/> (WRITE, REWRITE: after the FROM move) and <see cref="DebugWhen.AfterAction"/> (a RECEIVE that
/// took its data) are interior to a verb, so the wrapper only HOLDS them and the verb's emitter lands them at its own point
/// (<see cref="LandBeforeIo"/>, <see cref="LandAfterAction"/>); the loop phases are held for the PERFORM emitter the same way
/// (<see cref="TakeLoopFires"/>). A fire the verb never landed is an internal error raised at the wrapper — never a trigger that silently
/// did not run.</para>
/// <para>DEBUG-CONTENTS is the item's character image (rule 24 c: "the content of the data item referenced by identifier-1 at the time that
/// control passes to the debugging section"), the CD's record-area image for a cd-name (rule 23 e); DEBUG-SUB-n are the occurrence numbers of
/// the reference's subscripted levels, outermost first (rule 14).</para>
/// </summary>
internal sealed class DebugTriggerEmitter(EmitContext ctx, NumericRenderer num)
{
    private IReadOnlyList<DebugFire> _beforeIo = [];
    private IReadOnlyList<DebugFire> _afterAction = [];
    private LoopFires? _loop;
    private int _line;

    /// <summary>The loop-phase fires a PERFORM emitter takes, with the PERFORM statement's source line (DEBUG-LINE).</summary>
    internal sealed record LoopFires(IReadOnlyList<DebugFire> Fires, int Line);

    /// <summary>Emit one triggering statement: the fires before it, the statement through <paramref name="emitInner"/> (which returns whether it
    /// transfers control out of the paragraph case), the fires after it. Returns what <paramref name="emitInner"/> returned.</summary>
    public bool Emit(BoundDebugTriggered n, Func<BoundStatement, bool> emitInner)
    {
        var (savedIo, savedAction, savedLoop, savedLine) = (_beforeIo, _afterAction, _loop, _line);
        _line = n.SourceLine;
        _beforeIo = Of(n, DebugWhen.BeforeIo);
        _afterAction = Of(n, DebugWhen.AfterAction);
        var loop = n.Fires.Where(f => f.When is DebugWhen.LoopInit or DebugWhen.LoopAugment or DebugWhen.LoopTest).ToList();
        _loop = loop.Count > 0 ? new LoopFires(loop, n.SourceLine) : null;
        try
        {
            foreach (var fire in Of(n, DebugWhen.Before)) EmitFire(fire, n.SourceLine);
            bool terminated = emitInner(n.Inner);
            if (_beforeIo.Count > 0 || _afterAction.Count > 0 || _loop is not null)
                throw new InvalidOperationException(
                    $"the debug fires of a statement at line {n.SourceLine} were never landed by its verb's emitter "
                    + $"(BeforeIo {_beforeIo.Count}, AfterAction {_afterAction.Count}, loop phases {_loop?.Fires.Count ?? 0}); "
                    + $"{n.Inner.GetType().Name} has no emission point for them (docs/CONTROL_FLOW_DESIGN.md D20)");
            if (!terminated)
                foreach (var fire in Of(n, DebugWhen.After)) EmitFire(fire, n.SourceLine);
            return terminated;
        }
        finally
        {
            (_beforeIo, _afterAction, _loop, _line) = (savedIo, savedAction, savedLoop, savedLine);
        }
    }

    private static List<DebugFire> Of(BoundDebugTriggered n, DebugWhen when) => n.Fires.Where(f => f.When == when).ToList();

    /// <summary>A WRITE or REWRITE emitter calls this immediately after its FROM phrase's implicit move and before the I-O
    /// (general rules 5 a and 6 a).</summary>
    public void LandBeforeIo()
    {
        var fires = _beforeIo;
        _beforeIo = [];
        foreach (var fire in fires) EmitFire(fire, _line);
    }

    /// <summary>A RECEIVE emitter calls this where the verb's own action is done and the data arrived, before the phrases' imperative
    /// statements (general rule 8 b).</summary>
    public void LandAfterAction()
    {
        var fires = _afterAction;
        _afterAction = [];
        foreach (var fire in fires) EmitFire(fire, _line);
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
            if (fire.When == when && fire.Level == level) EmitFire(fire, loop.Line);
    }

    /// <summary>Whether <paramref name="loop"/> has a fire for <paramref name="when"/> at <paramref name="level"/>.</summary>
    public static bool HasLoopPhase(LoopFires? loop, DebugWhen when, int level) =>
        loop is not null && loop.Fires.Any(f => f.When == when && f.Level == level);

    private void EmitFire(DebugFire fire, int line)
    {
        string contents = fire.Target.Kind is DebugTargetKind.CdName
            ? OperandText.RecordAreaImage(fire.Referenced)
            : OperandText.AsTransferString(new BoundFieldOperand(fire.Referenced), num, "the DEBUG-CONTENTS of");
        string occurrences = string.Concat(OccurrenceNumbers(fire.Referenced).Select(o => ", " + o));
        ctx.Writer.Line($"if (__DebugReady) __RunDebugRef({line}, {CsLiteral(fire.Target.Name)}, {contents}, "
            + $"{fire.Target.SectionStartPc}, {fire.Target.SectionEndPc}{occurrences});");
    }

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
