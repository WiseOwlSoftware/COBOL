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

    /// <summary>Collect alterable-GO-TO fields from a statement and every nested statement — recursing over the
    /// generated <see cref="BoundStatementTree.StatementChildren"/> (PHASE-07 Step 6g), the ONE drift-proof
    /// enumeration of every nesting container the binder produces, so a new container node is covered automatically.
    /// A missed container would reference an undeclared field and fail the generated-C# compile LOUDLY, never run
    /// silently wrong. (Replaces the former hand-maintained walker, which had missed SEQUENCE/CALL/WRITE/keyed/RETURN
    /// phrase bodies.)</summary>
    private static void AlterCollectFields(BoundStatement s, Dictionary<string, int> fields)
    {
        if (s is BoundGoToAlterable g) fields.TryAdd(g.AlterField, g.DefaultPc);
        foreach (var child in s.StatementChildren())
            AlterCollectFields(child, fields);
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
