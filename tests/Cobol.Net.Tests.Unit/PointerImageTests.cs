// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The ONE class-pointer storage-image codec (kb/Work PB970 arm 2; docs/CONFORMANCE.md §7 DOC-A.1-216) and its
/// CALL-boundary consumer. ISO §14.8.2.3.3 1): a program called with no program-specifier and no NESTED phrase
/// requires only that "the formal parameter shall be of the same length as the corresponding argument", and
/// §14.2.3 GR9 moves the argument "to this allocated record without conversion". The end-to-end witness is
/// conformance:2002/pb970_pointer_by_content_storage_image; these pin what the golden cannot see in-process —
/// absolute token shape, the FUNCTION-POINTER category, and the modes that must stay refused.
/// </summary>
public sealed class PointerImageTests
{
    private static long AsBigEndian(string image)
    {
        Assert.Equal(PointerImage.Width, image.Length);
        long v = 0;
        foreach (char c in image) { Assert.True(c <= 0xFF); v = (v << 8) | c; }
        return v;
    }

    [Fact]
    public void Null_OfEveryCategory_IsTheZeroAddress()
    {
        Assert.Equal(PointerImage.NullImage, PointerImage.Of(ManagedPointer.Null));
        Assert.Equal(PointerImage.NullImage, PointerImage.Of((ManagedPointer?)null));
        Assert.Equal(PointerImage.NullImage, PointerImage.Of(ProgramPointer.Null));
        Assert.Equal(PointerImage.NullImage, PointerImage.Of(FunctionPointer.Null));
        Assert.Equal(0, AsBigEndian(PointerImage.NullImage));
    }

    [Fact]
    public void DataPointer_IsAreaBasePlusDisplacement_BigEndian()
    {
        var cell = new StorageCell { Ref = "ABCDEFGH" };
        long at0 = AsBigEndian(PointerImage.Of(ManagedPointer.At(cell, 0)));
        Assert.NotEqual(0, at0);
        Assert.Equal(0, at0 & 0xFFFF_FFFFL);                                    // bases are k × 2^32
        Assert.Equal(at0 + 5, AsBigEndian(PointerImage.Of(CobolPtr.UpBy(ManagedPointer.At(cell, 0), 5))));
        Assert.Equal(PointerImage.Of(ManagedPointer.At(cell, 3)), PointerImage.Of(ManagedPointer.At(cell, 3)));
        // A different area never shares a base.
        long other = AsBigEndian(PointerImage.Of(ManagedPointer.At(new StorageCell { Ref = "ABCDEFGH" }, 0)));
        Assert.NotEqual(at0, other);
    }

    [Fact]
    public void ProgramAndFunctionPointers_AreStablePerName_AndDistinctPerCategory()
    {
        string p1 = PointerImage.Of(new ProgramPointer("PROGA"));
        Assert.Equal(p1, PointerImage.Of(new ProgramPointer("proga")));         // §8.3.2.2 — case-insensitive identity
        Assert.NotEqual(PointerImage.NullImage, p1);
        Assert.NotEqual(p1, PointerImage.Of(new FunctionPointer("PROGA")));     // same name, other category
        Assert.NotEqual(p1, PointerImage.Of(new ProgramPointer("PROGB")));
    }

    [Fact]
    public void ByContent_PointerIntoCharacterOrBinaryFormal_DeliversTheImage_Detached()
    {
        var cell = new StorageCell { Ref = "ABCDEFGH" };
        var slot = ManagedPointer<ManagedPointer>.Cell(ManagedPointer.At(cell, 2));
        var args = new[] { new CobolArg(CobolPassMode.Content, slot, null) };
        var view = CobolArgAdapt.Text(args, 0, 8);
        Assert.Equal(PointerImage.Of(ManagedPointer.At(cell, 2)), view.Value);
        view.Value = "ZZZZZZZZ";                                                // GR9 — a record of its own
        Assert.True(ManagedPointer.SameTarget(ManagedPointer.At(cell, 2), slot.Value));

        var fslot = ManagedPointer<FunctionPointer>.Cell(new FunctionPointer("F1"));
        Assert.Equal(PointerImage.Of(new FunctionPointer("F1")),
            CobolArgAdapt.Text([new CobolArg(CobolPassMode.Content, fslot, null)], 0, 8).Value);
    }

    [Fact]
    public void ByReference_PointerIntoCharacterFormal_StaysRefused()
    {
        // §14.8.2.3.2 — "If either the argument or the formal parameter is of class pointer, the corresponding
        // formal parameter or argument shall be of class pointer": no image is delivered BY REFERENCE.
        var slot = ManagedPointer<ManagedPointer>.Cell(ManagedPointer.Null);
        Assert.Throws<CobolCallException>(() =>
            CobolArgAdapt.Text([new CobolArg(CobolPassMode.Reference, slot, null)], 0, 8));
    }
}
