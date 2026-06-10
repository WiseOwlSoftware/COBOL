// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

public sealed partial class CSharpEmitter
{
    /// <summary>INITIALIZE (ISO §14.9.20) — render the bind-time expansion: each <see cref="InitializeStore"/> IS
    /// the spec's implicit elementary MOVE (GR4), emitted through the ONE MOVE store path (<c>EmitMove</c> →
    /// <c>ConvertSource</c> — so REPLACING/VALUE/default senders get identical conversion, editing, padding and
    /// truncation to an explicit MOVE; e.g. a numeric sender into a numeric-edited receiver edits, and the GR6c
    /// ZEROES default produces the EDITED zero, never spaces); each <see cref="InitializeLoop"/> is one OCCURS
    /// dimension (GR5b2 — every occurrence), nested outermost-first. Actions are already in GR3/GR8 order.</summary>
    private void EmitInitialize(BoundInitialize ini)
    {
        foreach (var action in ini.Actions) EmitInitializeAction(action);
    }

    private void EmitInitializeAction(InitializeAction action)
    {
        var w = _ctx.Writer;
        switch (action)
        {
            case InitializeStore { Target.Item.Pic: { IsFloat: true } fp } s:
                // A COMP-1/COMP-2 receiver: the GR6c ZEROES default is the IEEE zero (its declared default
                // initializer); the float MOVE path (REPLACING/VALUE senders) is deferred backend-wide → loud.
                w.Line(s.Source is BoundFigurative { Kind: 'Z' }
                    ? s.Target.Write(fp.DefaultInitializer)
                    : LoudStmt($"INITIALIZE REPLACING/VALUE into floating-point item " +
                               $"'{s.Target.Item.CobolName ?? s.Target.Read()}' (float MOVE path deferred)"));
                break;
            case InitializeStore s:
                EmitMove(new BoundMove(s.Source, [s.Target]));   // §14.9.20 GR4 — an implicit MOVE, one code path
                break;
            case InitializeLoop l:
                using (w.Block($"for (long {l.Var} = 1; {l.Var} <= {l.Count}; {l.Var}++)"))
                    foreach (var b in l.Body)
                        EmitInitializeAction(b);
                break;
            case InitializeErrorAction e:
                w.Line(LoudStmt(e.Feature));
                break;
        }
    }
}
