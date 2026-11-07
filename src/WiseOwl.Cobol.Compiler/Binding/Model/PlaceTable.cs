// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Model;

/// <summary>
/// ⛔ <b>THE PLACES OF ONE SOURCE ELEMENT'S DATA, BUILT WHEN BINDING IS DONE</b> (DESIGN-architecture-review §8.3 (4),
/// DESIGN-backend-abstraction §1.3 L6, kb/Work PB2375). A statement operand reaches the code generator as a
/// <see cref="Place"/> in the bound tree; the other places it needs are the ones named by an ITEM and no written
/// subscript: a file's record area and its RELATIVE KEY, FILE STATUS and ASSIGN USING items, a linkage formal and the
/// RETURNING item, an OCCURS DEPENDING item, the subject of an object property. Those used to be built by the emitters,
/// at emit time, by asking the reference resolver (24 calls, so the backend input carried the binder). The binder now
/// builds every one of them once, in the model layer (<see cref="PlaceBuilder"/>), in the pass that follows the last
/// fact the builder reads (the storage-form pass), and the backend reads the finished <see cref="Place"/>.
/// <para>A <see cref="Place"/> exists for an item that is not a table element (§2023:8.4.2.3.3 SR5: a table element is
/// referenced with a subscript, so by-item resolution has none to give) and whose declaration was not refused; every
/// other item has an entry that says so, and <see cref="Of"/> returns null for it. An item the table never saw is a
/// defect of the pass that fills it, and it is loud (<see cref="InvalidOperationException"/>), never a null that reads as
/// "no place".</para>
/// </summary>
internal sealed class PlaceTable
{
    private readonly Dictionary<DataItem, Place?> _places = new(ReferenceEqualityComparer.Instance);
    private readonly PlaceBuilder _builder;

    private PlaceTable(PlaceBuilder builder) => _builder = builder;

    /// <summary>The places of <paramref name="items"/>, each built once by <paramref name="builder"/> over the facts
    /// of the finished binding.</summary>
    public static PlaceTable Build(PlaceBuilder builder, IEnumerable<DataItem> items)
    {
        var table = new PlaceTable(builder);
        foreach (var item in items)
            if (!table._places.ContainsKey(item))
                table._places.Add(item, item.SubscriptArity == 0 ? builder.ResolveItem(item) : null);
        return table;
    }

    /// <summary>The <see cref="Place"/> of <paramref name="item"/> with no subscripts — view-aware, so a REDEFINES view
    /// resolves to its window or its canonical exactly as a verb operand does — or null for a table element or an
    /// unhandled view form.</summary>
    public Place? Of(DataItem item) =>
        _places.TryGetValue(item, out var place)
            ? place
            : throw new InvalidOperationException(
                $"no Place was built for data item '{item.CobolName ?? item.ClrName}' when binding finished — the pass that "
                + "fills the PlaceTable did not see it (kb/Work PB2375)");

    /// <summary>⛔ THE ONE RESOLUTION OF AN FD/SD's RECORD AREA (kb/Work PB355). ISO §2023:13.18.33.4 GR3 — "Multiple level 1
    /// entries subordinate to a FD or SD entry represent implicit redefinitions of the same area" — so the area is ONE
    /// place, and it is the LARGEST description's view (<see cref="FileModel.AreaRecord"/>). Null only for a file whose
    /// record area could not be resolved at all.</summary>
    public Place? RecordArea(FileModel file) => file.AreaRecord is { } area ? Of(area) : null;

    /// <summary>The <see cref="Place"/> of ONE OCCURRENCE of <paramref name="item"/> — one typed occurrence position per
    /// OCCURS level of its path, outermost first. The Format-2 table SORT of a table in shared storage windows its
    /// elements and keys at the element numbers of its own comparator (kb/Work PB599, PB1175): the positions are the
    /// statement's run-time locals, so they are not known when binding finishes, and the one builder answers them over
    /// the same frozen facts.</summary>
    public Place? Window(DataItem item, IReadOnlyList<Position> occurrence) => _builder.ResolveItemAt(item, occurrence);

    /// <summary>The CURRENT occurrence count of <paramref name="table"/> under the given outer occurrence positions (ISO
    /// §2023:14.6.9.1 via <see cref="OccurrenceCounts"/>) - the other windowed answer the shared-storage table SORT asks at its
    /// comparator's element numbers.</summary>
    public AllCount? CurrentOccurrenceCount(DataItem table, IReadOnlyList<Position> outer) =>
        _builder.CurrentOccurrenceCount(table, outer);
}
