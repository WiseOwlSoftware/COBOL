// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.CodeGen.Emit;

namespace WiseOwl.Cobol.CodeGen;

using static WiseOwl.Cobol.CodeGen.Emit.EmitText;

/// <summary>The ALTER / alterable-GO-TO / SET-external-switch emitter (P7 Step 9c — a real collaborator over
/// the per-unit <see cref="EmitContext"/>, extracted from the CSharpEmitter.AlterSwitches partial).</summary>
internal sealed class AlterSwitchEmitter(EmitContext ctx, DispatchState dispatch)
{
    // ── ALTER + the alterable GO TO (ANSI X3.23-1985; CONTROL_FLOW_DESIGN D4) ───────────────────────

    /// <summary>Declare one mutable <c>private static int _alter_&lt;para&gt; = &lt;defaultPc&gt;;</c> per altered
    /// paragraph (D4 — the C#-native replacement for the legacy slot table), collected from every
    /// <see cref="BoundGoToAlterable"/> in the bound program. Called from the dispatcher emission BEFORE the
    /// paragraph bodies render, so the dispatcher cases can reference the fields. Default = the written GO TO
    /// target's pc, or −1 for a never-written target-less <c>GO TO.</c> (undefined until ALTERed; −1 exits the
    /// <c>while ((uint)__pc &lt; (uint)__N)</c> dispatcher loop — D4's run-unit-end realization).</summary>
    public void EmitFields(BoundProgram bound, CodeWriter w)
    {
        var fields = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var para in bound.Paragraphs)
            foreach (var s in para.Statements)
                CollectFields(s, fields);
        foreach (var (field, defaultPc) in fields)
            w.Line($"private int {field} = {defaultPc};   // ALTERable GO TO target pc (control-flow design D4; instance — ALTER state is per-program-instance, ISO §2023:14.6.2.3.2 resets it via a fresh instance)");
    }

    /// <summary>⛔ AN INDEPENDENT SEGMENT'S INITIAL STATE (kb/Work PB5084; CONTROL_FLOW_DESIGN D4). §1985:XVI.1.4.3: "On subsequent
    /// transfers of control to the segment, an independent segment is also in its initial state when" control reaches it
    /// from a segment with a different segment-number by falling through, by a SORT or MERGE entering its procedure, or by
    /// an explicit transfer; it is in its LAST-USED state when control returns to it implicitly (the end of a PERFORM range,
    /// a SORT or MERGE procedure, a declarative) or by EXIT PROGRAM. §1985:VI.6.7.4 GR1 names what the initial state
    /// changes: "Modified GO TO statements in independent segments may, under some circumstances, be returned to their
    /// initial states". So the dispatcher keeps the segment control is in (<c>__seg</c>): every pc it enters whose segment
    /// differs from <c>__seg</c> calls <c>__EnterSegment</c>, which returns that segment's alterable GO TOs to the targets
    /// they were written with; each <c>__Dispatch</c> call restores the caller's <c>__seg</c> when it returns, which is
    /// the last-used return (an EXIT PROGRAM returns to another program instance, whose <c>__seg</c> nothing changed).
    /// §1985:XVI.3.3.1 lets only an ALTER of the segment's own sections set such a GO TO (COBOL6877), so the reset never
    /// undoes an ALTER executed in the segment since control entered it. Emitted only where an independent segment holds
    /// an alterable GO TO; returns whether it emitted.</summary>
    public bool EmitSegmentState(BoundProgram bound, CodeWriter w)
    {
        if (bound.Segments is not { } segments) return false;
        var bySegment = new SortedDictionary<int, Dictionary<string, int>>();
        for (int pc = 0; pc < bound.Paragraphs.Count; pc++)
        {
            if (!SegmentNumber.IsIndependent(segments[pc])) continue;
            var fields = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var s in bound.Paragraphs[pc].Statements) CollectFields(s, fields);
            if (fields.Count == 0) continue;
            if (!bySegment.TryGetValue(segments[pc], out var into)) bySegment[segments[pc]] = into = new(StringComparer.Ordinal);
            foreach (var (field, defaultPc) in fields) into.TryAdd(field, defaultPc);
        }
        if (bySegment.Count == 0) return false;
        w.Line($"private static ReadOnlySpan<byte> __SegOf => [{string.Join(", ", segments)}];   // each pc's segment-number (§1985:XVI.3.2.4 GR1)");
        // The instance starts in the segment of its first nondeclarative procedure, where every segment is still in its initial
        // state, and the activation's own __Dispatch restores that segment when it returns. So a later CALL of the same
        // instance enters its program in the last-used state (a re-entered program is not a transfer from "a segment with a
        // different segment-number"), and only a fresh instance (CANCEL, an INITIAL program) starts initial again.
        int entrySegment = bound.EntryPc < segments.Count ? segments[bound.EntryPc] : 0;
        w.Line($"private int __seg = {entrySegment};   // the segment control is in (§1985:XVI.1.4.3; control-flow design D4)");
        using (w.Block("private void __EnterSegment(int segment)"))
        {
            w.Line("__seg = segment;");
            using (w.Block("switch (segment)"))
                foreach (var (segment, fields) in bySegment)
                    using (w.Block($"case {segment}:   // an independent segment enters in its initial state (§1985:XVI.1.4.3; §1985:VI.6.7.4 GR1)"))
                    {
                        foreach (var (field, defaultPc) in fields) w.Line($"{field} = {defaultPc};");
                        w.Line("break;");
                    }
        }
        return true;
    }

    /// <summary>Collect alterable-GO-TO fields from a statement and every nested statement — recursing over the
    /// generated <see cref="BoundStatementTree.StatementChildren"/> (PHASE-07 Step 6g), the ONE drift-proof
    /// enumeration of every nesting container the binder produces, so a new container node is covered automatically.
    /// A missed container would reference an undeclared field and fail the generated-C# compile LOUDLY, never run
    /// silently wrong. (Replaces the former hand-maintained walker, which had missed SEQUENCE/CALL/WRITE/keyed/RETURN
    /// phrase bodies.)</summary>
    private static void CollectFields(BoundStatement s, Dictionary<string, int> fields)
    {
        if (s is BoundGoToAlterable g) fields.TryAdd(g.AlterField, g.DefaultPc);
        foreach (var child in s.StatementChildren())
            CollectFields(child, fields);
    }

    /// <summary>The alterable GO TO transfers to the CURRENT field value (D4:
    /// <c>__pc = _alter_X; goto __xfer;</c>) — the written target until an ALTER executes, then the most recent
    /// ALTER's destination; −1 (a never-ALTERed target-less GO TO, undefined per ANSI-85) exits the dispatcher
    /// loop. Like every transfer out of a paragraph it leaves through <see cref="DispatchState.TransferOut(string,string)"/>,
    /// never a bare <c>break</c> a lowered container could capture (kb/Work PB405).</summary>
    public void EmitGoTo(BoundGoToAlterable g)
    {
        var w = ctx.Writer;
        // X3.23-1985 USE FOR DEBUGGING (VCR 7.17): an altered GO TO transfer is DEBUG-CONTENTS SPACES (Transfer),
        // DEBUG-LINE the GO TO statement's own line.
        dispatch.EmitDebugCause(w, "Transfer", g.SourceLine);
        w.Line(dispatch.ExplicitTransferOut(g.AlterField));
    }

    /// <summary>ALTER assigns each entry's new destination pc into the target paragraph's field at the ALTER site
    /// (ANSI X3.23-1985 ALTER GR — subsequent executions of the GO TO transfer to proc-2).</summary>
    public void EmitAlter(BoundAlter al)
    {
        foreach (var e in al.Entries)
            ctx.Writer.Line($"{e.AlterField} = {e.NewPc};");
        EmitDebugTriggers(al);
    }

    /// <summary>X3.23-1985 USE FOR DEBUGGING, §1985:XV.3.2.4 GR3 b: a debugging section whose subject is procedure-name-1 runs
    /// "Immediately after the execution of an ALTER statement which references procedure-name-1" - the paragraph subject, not
    /// a section that contains it (GR9: a qualifier is no reference). GR17: DEBUG-LINE identifies the ALTER statement,
    /// DEBUG-NAME is procedure-name-1 and DEBUG-CONTENTS "the applicable procedure-name associated with the TO phrase".
    /// GR7: once per statement for a name written twice, under the TO phrase last in effect.</summary>
    private void EmitDebugTriggers(BoundAlter al)
    {
        if (!dispatch.DebugActive) return;
        var w = ctx.Writer;
        foreach (var altered in al.Entries.GroupBy(e => e.AlteredPc))
            foreach (var subject in dispatch.DebugByPc[altered.Key].Where(s => !s.IsSection))
            {
                string name = CsLiteral(subject.SubjectName), contents = CsLiteral(altered.Last().NewName);
                w.Line($"if (__DebugReady) __RunDebugRef({al.SourceLine}, {name}, {contents}, {subject.SectionStartPc}, {subject.SectionEndPc});");
            }
    }

    // ── SET Format 3 — external switches (ISO §2023:14.9.39 F3 GR5; §2023:12.3.7 GR3) ──────────────────────────────────

    /// <summary>Each (switch, position) pair assigns the run-unit switch store in source order — the switch is
    /// modified so its associated condition-names evaluate per the ON/OFF phrase (GR5).</summary>
    public void EmitSetSwitches(BoundSetSwitches s)
    {
        foreach (var (name, on) in s.Switches)
            ctx.Writer.Line($"ExternalSwitches.Set({CsLiteral(name)}, {(on ? "true" : "false")});");
    }
}
