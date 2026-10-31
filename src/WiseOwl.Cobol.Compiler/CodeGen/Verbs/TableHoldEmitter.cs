// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.CodeGen.Emit;

namespace WiseOwl.Cobol.CodeGen;

/// <summary>The emitter of <see cref="BoundHoldTableAll"/> (ISO §8.4.3.2.4 GR2, §15.3; kb/Work PB4162): the held form of a
/// table(ALL) function argument.
/// <para>The walk is <c>CobolTable.AllArgs</c>'s - row-major over the ALL levels, outermost first, each level's range read when
/// the level is ENTERED as a function of the outer indices (§15.3: "the first … specification being the identifier with each
/// subscript specified by the word ALL replaced by one, the next … the rightmost subscript specified by the word ALL incremented
/// by one") - written as nested loops over the SAME index vector the source's element place names, with each element stored
/// by the bound element copy (<see cref="BoundHoldTableAll.Copy"/>, an ordinary MOVE) into the next occurrence of the held
/// table. The held enumeration then ranges over the copy exactly as the source's did over the table.</para>
/// <para>When the held table is a dynamic-capacity table (a source level has no allocated maximum), the walk runs twice: the first
/// counts the elements and sets the capacity to the count (§14.9.39 Format 14's <c>SET capacity TO</c>), the second stores them.</para></summary>
internal sealed class TableHoldEmitter(EmitContext ctx, SetEmitter set)
{
    /// <summary>The statement emitter, wired after construction (the copy is any bound statement).</summary>
    internal StatementEmitter Statements { get; set; } = null!;

    public void Emit(BoundHoldTableAll hold)
    {
        var w = ctx.Writer;
        string source = hold.Source.Indices.CsName;
        string held = hold.Held.Indices.CsName;
        string stored = $"{held}_n";
        using (w.Block("// hold the table(ALL) argument at its place in the argument list (ISO §8.4.3.2.4 GR2, §15.3)"))
        {
            w.Line($"long[] {source} = new long[{hold.Source.Counts.Count}];");
            w.Line($"long[] {held} = new long[1];");
            w.Line($"long {stored} = 0;");
            if (hold.Held.Counts is [AllCount.Capacity capacity])
            {
                EmitWalk(hold, 0, source, () => w.Line($"{stored}++;"));
                w.Line($"{PlaceRenderer.RenderPath(capacity.Register.Table, AccessDir.Sending)}.SetCapacity({stored});");
                w.Line($"{stored} = 0;");
            }
            EmitWalk(hold, 0, source, () =>
            {
                w.Line($"{held}[0] = ++{stored};");
                Statements.EmitStatement(hold.Copy);
            });
            if (hold.Count is { } count)
                set.StoreSetTarget(new SetPlaceTarget(count), new NumX(stored, 0));
            // §15.3 14): an ALL that yields no argument is EC-ARGUMENT-FUNCTION, raised where the argument is evaluated (GR2).
            w.Line(RuntimeApi.TableRequireOccurrences(stored));
        }
    }

    /// <summary>The nested loops over the source's ALL levels; <paramref name="leaf"/> is the body at the innermost level.</summary>
    private void EmitWalk(BoundHoldTableAll hold, int level, string source, Action leaf)
    {
        var w = ctx.Writer;
        if (level == hold.Source.Counts.Count)
        {
            leaf();
            return;
        }
        string i = $"{source}_i{level}";
        string n = $"{source}_n{level}";
        string range = PlaceRenderer.OccurrenceCount(hold.Source.Counts[level]);
        using (w.Block($"for (long {i} = 1, {n} = (long)({range}); {i} <= {n}; {i}++)"))
        {
            w.Line($"{source}[{level}] = {i};");
            EmitWalk(hold, level + 1, source, leaf);
        }
    }
}
