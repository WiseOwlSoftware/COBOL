// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>The INITIALIZE verb emitter (P7 Step 9c — a real collaborator over the per-unit
/// <see cref="EmitContext"/>, extracted from the CSharpEmitter partial of the same name).</summary>
internal sealed class InitializeEmitter(EmitContext ctx, MoveEmitter move)
{
    /// <summary>INITIALIZE (ISO §14.9.20) — render the bind-time expansion: each <see cref="InitializeStore"/> IS
    /// the spec's implicit elementary MOVE (GR4), emitted through the ONE MOVE store path (<c>EmitMove</c> →
    /// <c>ConvertSource</c> — so REPLACING/VALUE/default senders get identical conversion, editing, padding and
    /// truncation to an explicit MOVE; e.g. a numeric sender into a numeric-edited receiver edits, and the GR6c
    /// ZEROES default produces the EDITED zero, never spaces); each <see cref="InitializeLoop"/> is one OCCURS
    /// dimension (GR5b2 — every occurrence), nested outermost-first. Actions are already in GR3/GR8 order.</summary>
    public void Emit(BoundInitialize ini)
    {
        foreach (var action in ini.Actions) EmitAction(action);
    }

    private void EmitAction(InitializeAction action)
    {
        var w = ctx.Writer;
        switch (action)
        {
            case InitializeStore { Target.Item.Pic: { IsFloat: true } fp } s:
                // A COMP-1/COMP-2 receiver: the GR6c ZEROES default is the IEEE zero (its declared default
                // initializer); the float MOVE path (REPLACING/VALUE senders) is deferred backend-wide → loud.
                w.Line(s.Source is BoundFigurative { Kind: 'Z' }
                    ? PlaceRenderer.Write(s.Target, fp.DefaultInitializer)
                    : LoudStmt($"INITIALIZE REPLACING/VALUE into floating-point item " +
                               $"'{s.Target.Item.CobolName ?? PlaceRenderer.Read(s.Target)}' (float MOVE path deferred)"));
                break;
            case InitializeStore s:
                move.Emit(new BoundMove(s.Source, [s.Target]));   // §14.9.20 GR4 — an implicit MOVE, one code path
                break;
            case InitializeLoop l:
                using (w.Block($"for (long {l.Var} = 1; {l.Var} <= {l.Count}; {l.Var}++)"))
                    foreach (var b in l.Body)
                        EmitAction(b);
                break;
            case InitializeDynLoop l:
                // §14.9.20 GR10 (D9): initialize every occurrence 1‥current-capacity (a run-time bound) with the
                // INITIALIZE statement's own stores; the capacity is unchanged (RefReceiving within bounds).
                using (w.Block($"for (long {l.Var} = 1; {l.Var} <= {l.CapacityExpr}; {l.Var}++)"))
                    foreach (var b in l.Body) EmitAction(b);
                break;
            case InitializeErrorAction e:
                w.Line(LoudStmt(e.Feature));
                break;
        }
    }
}
