// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;

namespace CobolNet.CodeGen;

public sealed partial class CSharpEmitter
{
    /// <summary>EVALUATE → a chained <c>if / else if / else</c> (ISO §14.9.13 GR1–3: the WHEN arms are tested in
    /// source order, the FIRST true arm's statements run, WHEN OTHER is the else tail; no arm matching = no
    /// statements). The matches were composed at bind time (COBOLNET_DESIGN §5.3 — no dispatch tables, readable
    /// generated C#).</summary>
    private void EmitEvaluate(BoundEvaluate ev)
    {
        var w = _ctx.Writer;
        bool first = true;
        foreach (var arm in ev.Whens)
        {
            string cond = _cond.Render(arm.Match);
            using (w.Block(first ? $"if ({cond})" : $"else if ({cond})"))
                EmitStatementList(arm.Statements);
            first = false;
        }
        if (ev.Other is { } other)
        {
            if (first) { EmitStatementList(other); return; }   // only WHEN OTHER — unconditional
            using (w.Block("else"))
                EmitStatementList(other);
        }
    }
}
