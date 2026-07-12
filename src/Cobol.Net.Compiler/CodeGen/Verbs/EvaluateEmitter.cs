// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

/// <summary>The EVALUATE verb emitter (P7 Step 9c — a real collaborator over the per-unit
/// <see cref="EmitContext"/>, extracted from the CSharpEmitter partial of the same name).</summary>
internal sealed class EvaluateEmitter(EmitContext ctx, ConditionRenderer cond)
{
    /// <summary>The statement dispatcher — property-wired by <see cref="UnitEmitters"/> (the WHEN arms nest
    /// arbitrary statement lists, a cyclic edge no ctor order can satisfy).</summary>
    internal StatementEmitter Statements { get; set; } = null!;

    /// <summary>EVALUATE → a chained <c>if / else if / else</c> (ISO §14.9.13 GR1–3: the WHEN arms are tested in
    /// source order, the FIRST true arm's statements run, WHEN OTHER is the else tail; no arm matching = no
    /// statements). The matches were composed at bind time (COBOLNET_DESIGN §5.3 — no dispatch tables, readable
    /// generated C#).</summary>
    public void Emit(BoundEvaluate ev)
    {
        var w = ctx.Writer;
        bool first = true;
        foreach (var arm in ev.Whens)
        {
            string match = cond.Render(arm.Match);
            using (w.Block(first ? $"if ({match})" : $"else if ({match})"))
                Statements.EmitStatementList(arm.Statements);
            first = false;
        }
        if (ev.Other is { } other)
        {
            if (first) { Statements.EmitStatementList(other); return; }   // only WHEN OTHER — unconditional
            using (w.Block("else"))
                Statements.EmitStatementList(other);
        }
    }
}
