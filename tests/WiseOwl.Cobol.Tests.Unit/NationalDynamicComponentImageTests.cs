// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// kb/Work PB3441 — a NATIONAL variable-length component rides a group's image as the UTF-16BE bytes it occupies (two image
/// characters a position, <c>CobolBits.NatBytes</c>), so the runtime's two image-side rules that were stated in characters
/// are stated in positions of the component's own unit: the layout code that carries the unit
/// (<see cref="CobolDynStructure.PlainCode"/>), the space a fixed-length file pads a member with and trims from it
/// (<see cref="CobolVarGroup.SpacesFor"/>, ISO §13.18.43.4 GR6 fixed-length records; the national space is the pair 00 20,
/// never the byte 0x20 twice, which is U+2020), and the space an excess-part table element is filled with (§14.6.9.4).
/// </summary>
public sealed class NationalDynamicComponentImageTests
{
    private static string Nat(string characters) => CobolBits.NatBytes(characters);

    [Fact]
    public void ALayoutCodeThatNamesOnlyAUnit_NamesNoStructure_AndSaysTheUnit()
    {
        Assert.Equal(0, CobolDynStructure.PlainCode(1));
        Assert.Equal(1, CobolDynStructure.UnitOf(0));
        int national = CobolDynStructure.PlainCode(2);
        Assert.Equal(2, CobolDynStructure.UnitOf(national));
        Assert.Null(CobolDynStructure.FromCode(national));
        // a structure's own code carries its unit as well, and still names the structure
        var shortPrefixed = new CobolDynStructure(2, delimited: false, unit: 2);
        Assert.Equal(2, CobolDynStructure.UnitOf(shortPrefixed.Code));
        Assert.NotNull(CobolDynStructure.FromCode(shortPrefixed.Code));
        // a layout whose components name no structure but a unit is an unstructured layout
        Assert.False(new CobolContiguousLayout(0, [0], [2], [4L], [national]).HasStructure);
        Assert.True(new CobolContiguousLayout(0, [0], [2], [4L], [shortPrefixed.Code]).HasStructure);
    }

    [Fact]
    public void TheSpaceOfANationalMember_IsTheNationalSpace_AndTrimmingTakesWholePositions()
    {
        Assert.Equal(Nat("   "), CobolVarGroup.SpacesFor(6, 2));
        Assert.Equal("   ", CobolVarGroup.SpacesFor(3, 1));
        Assert.Equal(Nat("ab"), CobolVarGroup.TrimTrailingSpaces(Nat("ab") + Nat("  "), 2));
        // U+2020 is the pair 20 20: two spaces of an alphanumeric item, one CHARACTER of a national one - data, not padding
        Assert.Equal(Nat("†"), CobolVarGroup.TrimTrailingSpaces(Nat("†") + Nat(" "), 2));
        Assert.Equal("", CobolVarGroup.TrimTrailingSpaces(Nat("   "), 2));
        // not whole positions: nothing of it is padding
        Assert.Equal(Nat("a") + "\0", CobolVarGroup.TrimTrailingSpaces(Nat("a") + "\0", 2));
        Assert.Equal("ab", CobolVarGroup.TrimTrailingSpaces("ab  ", 1));
    }

    [Fact]
    public void ANationalMemberOfAFixedLengthRecord_IsPaddedAndReadBackInWholePositions()
    {
        // Record: K X(2) then ONE national dynamic item (maximum 4 positions = 8 bytes) at fixed offset 2; its layout code
        // names the unit, so its space is the national space (a component of unit 2 with no such code - a table of
        // two-character elements - is padded with plain spaces, FixedFormAndWideTransferEdgeTests).
        var layout = new CobolContiguousLayout(2, [2], [2], [4L], [CobolDynStructure.PlainCode(2)]);
        foreach (string content in new[] { "xy", "†", "", "a†" })
        {
            var sent = new CobolVarGroup("KK", [Nat(content)]);
            var extents = layout.ExtentsOf(sent);
            string image = "KK" + Nat(content);
            string fixedForm = layout.ToFixedForm(image, extents);
            Assert.Equal("KK" + Nat(content) + Nat(new string(' ', 4 - content.Length)), fixedForm);
            Assert.Equal(Nat(content), layout.Decompose(fixedForm, null, fixedForm: true).Dyn(0));
        }
    }

    [Fact]
    public void AStructuredNationalMemberOfAGroup_CountsItsLengthFieldInPositions_AndTakesItBack()
    {
        // ISO §12.3.7.4 GR18: SHORT PREFIXED is a two-byte length field in CHARACTER POSITIONS (DOC-A.1-205: most
        // significant byte first), so N"AB" - two positions, four bytes - is framed 00 02 | 00 41 00 42 (kb/Work PB3441).
        var shortPrefixed = new CobolDynStructure(2, delimited: false, unit: 2);
        var layout = new CobolContiguousLayout(2, [2], [2], [5L], [shortPrefixed.Code]);
        string image = "KK" + Nat("AB");
        var extents = layout.ExtentsOf(new CobolVarGroup("KK", [Nat("AB")]));
        string medium = layout.MediumImage(image, extents);
        Assert.Equal("KK" + "\0\u0002" + Nat("AB"), medium);
        Assert.Equal(Nat("AB"), layout.Decompose(medium, null).Dyn(0));
    }

    [Fact]
    public void ATableOfNationalElementsInAnAreaTheCarrierLacks_IsSpaceFilledWithTheNationalSpace()
    {
        // §14.6.9.4: the capacity is unaffected and each element is space-filled - the national space (00 20) for a national
        // element, which the cell cannot tell from the width alone: its layout code (the unit) says so.
        var cell = new StorageCell { Ref = "" };
        var table = cell.DynTableAt(0, 0, 4);
        table.RefReceiving(1).Ref = Nat("ab");
        table.RefReceiving(2).Ref = Nat("cd");
        cell.StoreVarGroupAt(0, 4, 0, [0], [2], [CobolDynStructure.PlainCode(2)], [4], default, int.MaxValue, new CobolVarGroup("", []));
        Assert.Equal(2, table.Capacity);
        Assert.Equal(Nat("  "), table.RefSending(1).Ref);
        Assert.Equal(Nat("  "), table.RefSending(2).Ref);
        // an alphanumeric element of the same width is four plain spaces
        var plain = new StorageCell { Ref = "" };
        var plainTable = plain.DynTableAt(0, 0, 4);
        plainTable.RefReceiving(1).Ref = "abcd";
        plain.StoreVarGroupAt(0, 4, 0, [0], [2], [0], [4], default, int.MaxValue, new CobolVarGroup("", []));
        Assert.Equal("    ", plainTable.RefSending(1).Ref);
    }

    [Fact]
    public void ACellsLayout_CountsANationalItemInPositionsOfItsUnit()
    {
        // One national dynamic item, window 0 wide, maximum 4 positions = 8 image characters (the slot's unit): the image
        // "6 bytes" decomposes whole (3 positions), and an excess pair beyond the maximum is not taken.
        var cell = new StorageCell { Ref = "" };
        cell.StoreContiguousAt(0, 0, 0, [0], [8], [CobolDynStructure.PlainCode(2)], [0], default, Nat("abc"));
        Assert.Equal(Nat("abc"), cell.DynAt(0));
        cell.StoreContiguousAt(0, 0, 0, [0], [8], [CobolDynStructure.PlainCode(2)], [0], default, Nat("abcdef"));
        Assert.Equal(Nat("abcd"), cell.DynAt(0));
        Assert.Equal(Nat("abcd").Length, cell.ContiguousAt(0, 0, 0, [0], [0], default, 0).Length);
    }
}
