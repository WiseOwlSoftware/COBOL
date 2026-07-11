// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

public sealed partial class CSharpEmitter
{
    /// <summary>
    /// MOVE/ADD/SUBTRACT CORRESPONDING (ISO §14.7.6): render the bind-time-expanded pairs as the per-pair implied
    /// statements, in D1 declaration order. MOVE pairs are ordinary MOVEs (MOVE GR11 §14.9.25.4 — "the same as if
    /// the user had referred to each pair … in separate MOVE statements"), reusing the one MOVE emission path.
    /// ADD/SUBTRACT pairs run inside ONE statement-level ON SIZE ERROR region (§14.7.6): the shared
    /// <see cref="EmitArith"/> flag latches across the per-pair checked stores, an erring pair's receiver stays
    /// UNCHANGED while the remaining pairs still execute (§14.7.5), the ON imperative dispatches ONCE after all
    /// pairs complete, and the NOT branch is suppressed when any pair erred — never a per-pair phrase.
    /// </summary>
    private void EmitCorresponding(BoundCorresponding c)
    {
        if (c.Verb is CorrVerb.Move)
        {
            EmitCorrespondingHoists(c.Hoists);
            foreach (var p in c.Pairs)
                EmitMove(new BoundMove(new BoundFieldOperand(p.Source), [p.Target]));
            return;
        }
        // ADD GR3 (§14.9.2.4): target ← target + source. SUBTRACT GR3 (§14.9.44.4): "data items in identifier-4
        // are subtracted from and stored in corresponding items in identifier-5" — target ← target − source (GR5's
        // reduction to separate `SUBTRACT a FROM b` statements settles the operand order over the inverted-looking
        // standard-arithmetic print at GR3).
        string op = c.Verb is CorrVerb.Add ? "+" : "-";
        EmitArith(c.SizeError, ise =>
        {
            EmitCorrespondingHoists(c.Hoists);
            foreach (var p in c.Pairs)
            {
                // ONE rounded-phrase mode for every pair (§14.7.4).
                var rcv = RcvFor(new Receiver(p.Target, c.Rounding), ise);
                StoreArith(p.Target,
                    _num.Combine(_num.FieldNum(p.Target), op, _num.FieldNum(p.Source), rcv),
                    c.Rounding);
            }
        });
    }

    /// <summary>Anchor each group operand ONCE before the first pair (§14.7.6 — all item identification, including
    /// the group operands' subscripts, is done at the START of the statement, not per implied statement): a
    /// <c>ref var</c> local aliases a member-path group (evaluating any <c>CobolTable.At</c> subscript exactly
    /// once — <c>At</c> is ref-returning, so the alias is a true lvalue the pair stores write through); a
    /// <c>long</c> local pins a Tier-B REDEFINES view group's computed window offset.</summary>
    private void EmitCorrespondingHoists(IReadOnlyList<CorrespondingHoist> hoists)
    {
        foreach (var h in hoists)
            _ctx.Writer.Line(h.IsRef ? $"ref var {h.Local} = ref {h.Init};" : $"long {h.Local} = {h.Init};");
    }
}
