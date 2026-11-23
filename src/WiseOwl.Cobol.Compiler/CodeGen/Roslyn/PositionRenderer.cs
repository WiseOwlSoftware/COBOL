// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.CodeGen;

/// <summary>
/// ⛔ THE ONE RENDERING OF A <see cref="Position"/> (kb/Work PB2151; DESIGN-binder-bound-tree.md §3.9.3) — a typed
/// subscript, reference-modifier position, offset or ordinal to the C# <c>long</c> expression that computes it. Every
/// reader of a position field renders through here, so the text of a position is decided in exactly one place.
/// <para>Only the code generator calls it: no binder code renders a position, and no carrier of the place model
/// holds position text (<c>PositionCarrierDriftTests</c>).</para>
/// </summary>
internal static class PositionRenderer
{
    /// <summary>The C# expression of <paramref name="p"/>.</summary>
    public static string Render(Position p) => p switch
    {
        PositionConstant c => c.Value.ToString(CultureInfo.InvariantCulture),
        PositionWideConstant w => RuntimeApi.OccurrenceOfLiteral(w.Value.ToString(CultureInfo.InvariantCulture)),
        PositionIndexCell ix => ix.Cell,
        PositionItemRead r => r.Form switch
        {
            PositionReadForm.Subscript => RuntimeApi.PositionRead(PlaceRenderer.RenderPath(r.Path, AccessDir.Sending), r.Item.ProfileName, refMod: false),
            PositionReadForm.RefMod => RuntimeApi.PositionRead(PlaceRenderer.RenderPath(r.Path, AccessDir.Sending), r.Item.ProfileName, refMod: true),
            PositionReadForm.Digits => RuntimeApi.PositionDigits(PlaceRenderer.RenderPath(r.Path, AccessDir.Sending)),
            _ => throw new System.InvalidOperationException($"PositionRenderer has no arm for read form {r.Form}"),
        },
        PositionLocal l => l.AsLong ? $"(long){LocalNames.Of(l.Local)}" : LocalNames.Of(l.Local),
        PositionLocalElement e => $"{LocalNames.Of(e.Vector.Local)}[{e.Index.ToString(CultureInfo.InvariantCulture)}]",
        PositionBinary b => $"{Render(b.Left)} {Operator(b.Op)} {Render(b.Right)}",
        PositionNegate n => $"- {Render(n.Operand)}",
        PositionGroup g => $"({Render(g.Inner)})",
        PositionPointerOffset ptr => RuntimeApi.PtrOffsetOf(ptr.PointerField),
        PositionPointerDynBase ptr => RuntimeApi.PtrDynBaseOf(ptr.PointerField),
        PositionOffset o => o.Terms.Aggregate(Render(o.Origin),
            (text, t) => $"{text} + ({RenderIndex(t)} - 1) * {t.Stride.ToString(CultureInfo.InvariantCulture)}"),
        _ => throw new System.InvalidOperationException($"PositionRenderer has no arm for {p.GetType().Name}"),
    };

    /// <summary>A term's one-based index, tested against the range its <see cref="OffsetTerm.Bound"/> carries (ISO §2023:8.4.2.3.4 GR2,
    /// §2023:13.18.38.4 GR7; kb/Work PB5762, PB5605) — the window's twin of the <c>CobolTable.At</c> a table step renders.</summary>
    private static string RenderIndex(OffsetTerm t) => t.Bound is { } b
        ? RuntimeApi.WindowSubscript(Render(t.Index), b.Highest, b.Odo is { } o ? (PlaceRenderer.CountRead(o.Depending), o.MinOccurs, o.MaxOccurs) : null)
        : Render(t.Index);

    /// <summary>The one-based occurrence <c>(local + 1)</c> of a zero-based loop local — the code generator's own
    /// enumerations (a table SORT's element loop) address the table through it.</summary>
    public static Position OneBased(string zeroBasedLocal) =>
        new PositionGroup(new PositionBinary(CSharpLocal.Position(zeroBasedLocal), PositionOperator.Add, new PositionConstant(1)));

    private static string Operator(PositionOperator op) => op switch
    {
        PositionOperator.Add => "+",
        PositionOperator.Subtract => "-",
        PositionOperator.Multiply => "*",
        _ => throw new System.InvalidOperationException($"PositionRenderer has no arm for operator {op}"),
    };
}
