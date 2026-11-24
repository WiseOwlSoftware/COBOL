// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// kb/Work PB2746 — the receiving store of ONE carried component of a variable-length group, and the one builder of the
/// group's record layout, each written once in the runtime and called by both lanes (a declared group's emitted
/// <c>FromVarImage</c> / layout field, and a cell-backed group's <c>StorageCell.StoreVarGroupAt</c> /
/// <c>StorageCell.LayoutOf</c>). Expected values are the cited rules', computed by hand.
/// </summary>
public sealed class VariableLengthComponentStoreTests
{
    private static CobolDynTable<string> Table(int capacity, int elementWidth) =>
        new(_ => new string('Z', elementWidth), 0, null, null, capacity);

    private static readonly Func<string, string, string> PassThrough = static (_, x) => x;

    /// <summary>ISO §2023:8.5.1.10.4: the new content replaces the old, truncated on the right at the maximum size, never
    /// padded; a formal's copy-in (kb/Work PB1937) keeps the whole content.</summary>
    [Fact]
    public void ADynamicLengthComponent_IsTruncatedAtItsMaximum_UnlessItIsAFormalsCopyIn()
    {
        var v = new CobolVarGroup("", ["ABCDEFG"]);
        Assert.Equal("ABCD", v.ReceivedDyn(0, 4, storage: false));
        Assert.Equal("ABCDEFG", v.ReceivedDyn(0, 4, storage: true));
        Assert.Equal("", v.ReceivedDyn(1, 4, storage: false));   // a component the sender did not carry is length zero
    }

    /// <summary>A NATIONAL component travels as its UTF-16BE image (kb/Work PB3441): it is decoded first, so the maximum
    /// counts POSITIONS and the store never cuts a position's pair.</summary>
    [Fact]
    public void ANationalComponent_IsDecoded_ThenTruncatedInPositions()
    {
        var v = new CobolVarGroup("", [NationalImage.Bytes("XYZ")]);
        Assert.Equal("XY", v.ReceivedDyn(0, 2, storage: false, national: true));
    }

    /// <summary>ISO §2023:14.6.9.2: a CARRIED table recreates the receiving table from the carried occurrences — an empty
    /// carried component recreates it at capacity zero.</summary>
    [Fact]
    public void ACarriedTable_IsRecreated_FromItsOccurrences()
    {
        var table = Table(capacity: 3, elementWidth: 2);
        new CobolVarGroup("", ["AABB"]).StoreTableComponent(0, table, 2, 1, PassThrough);
        Assert.Equal("AABB", table.CurrentImage(static e => e));

        new CobolVarGroup("", [""]).StoreTableComponent(0, table, 2, 1, PassThrough);
        Assert.Equal("", table.CurrentImage(static e => e));
    }

    /// <summary>ISO §2023:14.6.9.4 (the §2023:14.9.25.4 GR9b excess part, kb/Work PB393): a table the sender did not carry
    /// keeps its capacity and each element is space-filled — a national element with the national space, the pair 00 20.</summary>
    [Fact]
    public void AnUncarriedTable_KeepsItsCapacity_AndEachElementIsSpaceFilledInItsUnit()
    {
        var table = Table(capacity: 3, elementWidth: 2);
        CobolVarGroup.Empty.StoreTableComponent(0, table, 2, 1, PassThrough);
        Assert.Equal("      ", table.CurrentImage(static e => e));

        var national = Table(capacity: 2, elementWidth: 4);
        CobolVarGroup.Empty.StoreTableComponent(0, national, 4, NationalImage.BytesPerNational, PassThrough);
        Assert.Equal(NationalImage.Bytes("    "), national.CurrentImage(static e => e));
    }

    /// <summary>A table whose elements are variable-length groups (kb/Work PB2496): carried, it is recreated from the
    /// occurrence carriers; not carried, each element at an unaffected capacity takes the empty carrier.</summary>
    [Fact]
    public void ANestedTable_IsRecreatedFromItsCarriers_OrSpaceFilledFromTheEmptyCarrier()
    {
        var table = new CobolDynTable<CobolVarGroup>(_ => new CobolVarGroup("seed", []), 0, null, null, 2);
        Func<CobolVarGroup, CobolVarGroup, CobolVarGroup> take = static (_, x) => x;
        var one = new CobolVarGroup("A", []);
        new CobolVarGroup("", [""], [[one]]).StoreGroupTableComponent(0, table, take);
        Assert.Equal("A", table.CurrentImage(static e => e.Fixed));

        var keep = new CobolDynTable<CobolVarGroup>(_ => new CobolVarGroup("seed", []), 0, null, null, 3);
        CobolVarGroup.Empty.StoreGroupTableComponent(0, keep, take);
        Assert.Equal("", keep.CurrentImage(static e => e.Fixed));
        Assert.Equal(3, keep.CurrentOccurrencesAs(static e => e).Length);
    }

    /// <summary>The one layout builder: a table's unit is its element width and its maximum its capacity; a dynamic-length
    /// item's unit is the image characters of a position (two for a national item) and its maximum that many positions of
    /// its image-character maximum; an OCCURS DEPENDING table of fixed-image elements is the LAST component, taken out of
    /// the fixed run (§2023:13.18.38.3 SR22). The layout built from the component list behaves as the one stated by hand:
    /// a fixed run "K" (7 characters with the OCCURS DEPENDING table of 3 two-character occurrences at its maximum), a
    /// national item of at most 4 positions (8 image characters), a table of two-character elements of capacity at most 5.</summary>
    [Fact]
    public void TheLayoutBuilder_StatesEachComponentsUnitAndMaximum_AndTheOdoTableLast()
    {
        int nat = CobolDynStructure.PlainCode(NationalImage.BytesPerNational);
        var tail = new OdoTail(2, 3);
        var built = CobolContiguousLayout.Of(7, [1, 1], [8, 5], [nat, 0], [0, 2], tail);
        var stated = new CobolContiguousLayout(1, [1, 1, 1], [2, 2, 2], [4L, 5L, 3L], [nat, 0, 0], tail);

        string record = "K" + NationalImage.Bytes("AB") + "1122" + "xyxy";
        var extents = stated.ExtentsFrom([4, 4, 4]);
        var a = built.Decompose(record, built.ExtentsFrom([4, 4, 4]));
        var b = stated.Decompose(record, extents);
        Assert.Equal(b.Fixed, a.Fixed);
        Assert.Equal(b.Dynamic, a.Dynamic);
        // the fixed form pads each component to its maximum in its own unit: the national item with the national space
        Assert.Equal(stated.ToFixedForm(record, extents), built.ToFixedForm(record, built.ExtentsFrom([4, 4, 4])));
        Assert.Equal("K" + NationalImage.Bytes("AB  ") + "1122      " + "xyxy  ", built.ToFixedForm(record, built.ExtentsFrom([4, 4, 4])));
    }
}
