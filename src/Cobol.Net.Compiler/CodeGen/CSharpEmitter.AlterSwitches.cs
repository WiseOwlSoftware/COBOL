// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

public sealed partial class CSharpEmitter
{
    // ── ALTER + the alterable GO TO (ANSI X3.23-1985; COBOLNET_CONTROL_FLOW_DESIGN D4) ───────────────────────

    /// <summary>Declare one mutable <c>private static int _alter_&lt;para&gt; = &lt;defaultPc&gt;;</c> per altered
    /// paragraph (D4 — the C#-native replacement for the legacy slot table), collected from every
    /// <see cref="BoundGoToAlterable"/> in the bound program. Called from <c>EmitDispatcher</c> BEFORE the
    /// paragraph bodies render, so the dispatcher cases can reference the fields. Default = the written GO TO
    /// target's pc, or −1 for a never-written target-less <c>GO TO.</c> (undefined until ALTERed; −1 exits the
    /// <c>while ((uint)__pc &lt; (uint)__N)</c> dispatcher loop — D4's run-unit-end realization).</summary>
    private void AlterEmitFields(BoundProgram bound, CodeWriter w)
    {
        var fields = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var para in bound.Paragraphs)
            foreach (var s in para.Statements)
                AlterCollectFields(s, fields);
        foreach (var (field, defaultPc) in fields)
            w.Line($"private int {field} = {defaultPc};   // ALTERable GO TO target pc (control-flow design D4; instance — ALTER state is per-program-instance, ISO §14.6.2.3.2 resets it via a fresh instance)");
    }

    /// <summary>Collect alterable-GO-TO fields from a statement and every nested statement container the binder
    /// produces (IF branches, inline-PERFORM bodies, SEARCH/EVALUATE arms, READ phrases, ON SIZE ERROR phrases) —
    /// a missed container would reference an undeclared field and fail the generated-C# compile LOUDLY, never run
    /// silently wrong.</summary>
    private static void AlterCollectFields(BoundStatement s, Dictionary<string, int> fields)
    {
        switch (s)
        {
            case BoundEcChecked ec: AlterCollectFields(ec.Inner, fields); break;   // the EC wrapper is transparent
            case BoundGoToAlterable g: fields.TryAdd(g.AlterField, g.DefaultPc); break;
            case BoundIf i: AlterCollectLists(fields, i.Then, i.Else); break;
            case BoundInlinePerform p: AlterCollectLists(fields, p.Body); break;
            case BoundSearch se:
                if (se.AtEnd is { } at) AlterCollectLists(fields, at);
                foreach (var wn in se.Whens) AlterCollectLists(fields, wn.Statements);
                break;
            case BoundEvaluate ev:
                foreach (var wn in ev.Whens) AlterCollectLists(fields, wn.Statements);
                if (ev.Other is { } other) AlterCollectLists(fields, other);
                break;
            case BoundRead r:
                if (r.AtEnd is { } rAt) AlterCollectLists(fields, rAt);
                if (r.NotAtEnd is { } rNot) AlterCollectLists(fields, rNot);
                break;
            case BoundAddTo a: AlterCollectPhrase(a.SizeError, fields); break;
            case BoundAddGiving a: AlterCollectPhrase(a.SizeError, fields); break;
            case BoundSubtractFrom a: AlterCollectPhrase(a.SizeError, fields); break;
            case BoundSubtractGiving a: AlterCollectPhrase(a.SizeError, fields); break;
            case BoundMultiplyBy a: AlterCollectPhrase(a.SizeError, fields); break;
            case BoundMultiplyGiving a: AlterCollectPhrase(a.SizeError, fields); break;
            case BoundDivideInto a: AlterCollectPhrase(a.SizeError, fields); break;
            case BoundDivideGiving a: AlterCollectPhrase(a.SizeError, fields); break;
            case BoundDivideRemainder a: AlterCollectPhrase(a.SizeError, fields); break;
            case BoundCompute c: AlterCollectPhrase(c.SizeError, fields); break;
        }
    }

    private static void AlterCollectLists(Dictionary<string, int> fields, params IReadOnlyList<BoundStatement>[] lists)
    {
        foreach (var list in lists)
            foreach (var s in list)
                AlterCollectFields(s, fields);
    }

    private static void AlterCollectPhrase(SizeErrorPhrase? phrase, Dictionary<string, int> fields)
    {
        if (phrase?.OnError is { } on) AlterCollectLists(fields, on);
        if (phrase?.NotOnError is { } not) AlterCollectLists(fields, not);
    }

    /// <summary>The alterable GO TO transfers to the CURRENT field value (D4: <c>__pc = _alter_X; break;</c>) —
    /// the written target until an ALTER executes, then the most recent ALTER's destination; −1 (a never-ALTERed
    /// target-less GO TO, undefined per ANSI-85) exits the dispatcher loop.</summary>
    private void AlterEmitGoTo(BoundGoToAlterable g)
    {
        var w = _ctx.Writer;
        w.Line($"__pc = {g.AlterField};");
        w.Line("break;");
    }

    /// <summary>ALTER assigns each entry's new destination pc into the target paragraph's field at the ALTER site
    /// (ANSI X3.23-1985 ALTER GR — subsequent executions of the GO TO transfer to proc-2).</summary>
    private void AlterEmitAlter(BoundAlter al)
    {
        foreach (var e in al.Entries)
            _ctx.Writer.Line($"{e.AlterField} = {e.NewPc};");
    }

    // ── SET Format 3 — external switches (ISO §14.9.39 F3 GR5; §12.3.7 GR3) ──────────────────────────────────

    /// <summary>Each (switch, position) pair assigns the run-unit switch store in source order — the switch is
    /// modified so its associated condition-names evaluate per the ON/OFF phrase (GR5).</summary>
    private void SwitchEmitSet(BoundSetSwitches s)
    {
        foreach (var (name, on) in s.Switches)
            _ctx.Writer.Line($"ExternalSwitches.Set({CsLiteral(name)}, {(on ? "true" : "false")});");
    }
}
