// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding.Passes;

/// <summary>
/// ⛔ <b>EVERY PLACE THE CODE GENERATOR NEEDS IS IN THE BOUND TREE BEFORE IT STARTS</b> (DESIGN-architecture-review §8.3 (4),
/// DESIGN-backend-abstraction §1.3 L6, kb/Work PB2375). Requires <c>StorageComputed</c>, Produces <c>PlacesBuilt</c>: it
/// builds, once per source element, the <see cref="PlaceTable"/> of every data item and the place of every CONTROL
/// operand of a report, from the facts a finished binding fixed. It follows the storage-form pass because the places it
/// builds read the early-image facts that pass settles (<see cref="IPlaceFacts.IsImageBackedEarly"/>), and it precedes
/// nothing that changes one, so a place built here is the place the emitters used to build when they asked for it.
/// <para>A compilation that already carries a failing diagnostic is not emitted, so no table is built for it: a refused
/// declaration is a state the builder is not asked about.</para>
/// </summary>
internal static class PlaceTablePass
{
    public static void Run(GroupBindContext ctx)
    {
        if (ctx.Session.Edition.HasErrors) return;
        foreach (var unit in ctx.Units)
            unit.Places = TableOf(unit.Data, unit.Refs, ContainersOf(unit).SelectMany(c => c.Data.GlobalPlaceableItems()));
        foreach (var cls in ctx.Classes)
        {
            cls.Places = TableOf(cls.Data, cls.Refs, []);
            cls.FactoryPlaces = TableOf(cls.FactoryData, cls.FactoryRefs, []);
        }
    }

    /// <summary>The units that contain <paramref name="unit"/>, nearest first. A contained program names its containers'
    /// GLOBAL data (ISO §13.18.27.4 GR1) - a file's record area, a FILE STATUS or key item - and the place of such an item is
    /// built over THIS program's facts, so the containers' GLOBAL items (§8.4.6.2.2), and only those, join its table.</summary>
    private static IEnumerable<BoundUnit> ContainersOf(BoundUnit unit)
    {
        for (var anc = unit.Parent; anc is not null; anc = anc.Parent) yield return anc;
    }

    private static PlaceTable TableOf(DataBinder data, ReferenceResolver refs, IEnumerable<DataItem> inherited)
    {
        var builder = new PlaceBuilder(data);
        var table = PlaceTable.Build(builder, data.PlaceableItems().Concat(inherited));
        // A report's CONTROL operand is saved and compared as it is written (§13.18.16.4 GR3): a reference-modified
        // operand as its slice (§8.4.3.3.4 GR5), the others as the item. The positions are integer literals (SR4), so the
        // view is the ONE reference-modification view builder reached by item (kb/Work PB205).
        foreach (var report in data.Reports)
            foreach (var control in report.Controls)
                if (control.Item is { } item)
                    control.Place = control.Operand?.RefModStart is { } start
                        ? refs.RefMods.ResolveItemRefMod(item, start, control.Operand.RefModLength)
                        : table.Of(item);
        return table;
    }
}
