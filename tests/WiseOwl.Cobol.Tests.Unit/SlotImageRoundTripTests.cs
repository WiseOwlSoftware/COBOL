// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Exceptions;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// kb/Work PB4838: a FILE record carries a pointer or message-tag leaf as its 8-position token, and READ, RETURN and SORT
/// GIVING decode it back to the reference it names within the run unit (owner decision 2026-11-06; docs/CONFORMANCE.md §7
/// DOC-A.1-212 and DOC-A.1-216). These pin the inverse the conformance goldens cannot see in-process: the same-run round trip
/// compares equal for every category, and a token the run unit never imaged (the file came from another run unit) keeps its
/// image, is never NULL, and raises on dereference. ISO §2023:13.18.5.4 GR4: "If the subject of the entry is referenced while
/// its address is not NULL and not a valid address of storage, the EC-BOUND-PTR exception condition is set to exist."
/// (cite.py --check 13.18.5.4 OK, General rules 4.) The standard leaves a pointer's and a message tag's representation to the
/// implementor (§2023:13.18.60.4 GR9, GR23, GR24, GR26), so the token shapes are the documented determinations.
/// </summary>
public sealed class SlotImageRoundTripTests
{
    [Fact]
    public void DataPointerOf_IsTheInverseOfOf_InTheSameRunUnit()
    {
        var cell = new StorageCell { Ref = "ABCDEFGH" };
        foreach (long offset in new long[] { 0, 3, 7, -2 })
        {
            var written = ManagedPointer.At(cell, offset);
            var read = PointerImage.DataPointerOf(PointerImage.Of(written));
            Assert.True(ManagedPointer.SameTarget(written, read), $"offset {offset}");
            Assert.Equal(PointerImage.Of(written), PointerImage.Of(read));
        }
        Assert.True(PointerImage.DataPointerOf(PointerImage.NullImage).IsNull);
    }

    [Fact]
    public void DataPointerOf_AnAddresslessPointer_ReadsBackAsItself()
    {
        var carrier = ManagedPointer<string>.Cell("x");
        Assert.Same(carrier, PointerImage.DataPointerOf(PointerImage.Of(carrier)));
    }

    [Fact]
    public void DataPointerOf_APointerIntoEndedStorage_StaysDangling()
    {
        var cell = new StorageCell { Ref = "ABCD" };
        string image = PointerImage.Of(ManagedPointer.At(cell, 1));
        cell.End(StorageEnd.Freed);
        var read = PointerImage.DataPointerOf(image);
        Assert.Equal("EC-BOUND-PTR", Assert.Throws<CobolFatalException>(() => CobolPtr.Deref(read, 1)).EcName);   // §2023:13.18.5.4 GR4
    }

    [Fact]
    public void ProgramAndFunctionPointers_ReadBackEqual_AndNeverAsTheOtherCategory()
    {
        var program = new ProgramPointer("PROGA");
        var function = new FunctionPointer("PROGA");
        Assert.True(ProgramPointer.SameTarget(program, PointerImage.ProgramPointerOf(PointerImage.Of(program))));
        Assert.Equal(function.Name, PointerImage.FunctionPointerOf(PointerImage.Of(function)).Name);
        Assert.True(PointerImage.ProgramPointerOf(PointerImage.NullImage).IsNull);
        Assert.True(PointerImage.FunctionPointerOf(PointerImage.NullImage).IsNull);
        // The function-pointer image of a name is not the program-pointer image of it: decoded as the other category it is foreign, not a program.
        var crossed = PointerImage.ProgramPointerOf(PointerImage.Of(function));
        Assert.False(crossed.IsNull);
        Assert.NotEqual("PROGA", crossed.Name);
    }

    [Theory]
    [InlineData("        ")]                // the spaces a short record pads with
    [InlineData("\u007f\0\0\0\0\0\0\u0001")] // a base this run unit never handed out
    public void DataPointerOf_AForeignToken_IsNeitherNullNorAnAddress_AndKeepsItsImage(string image)
    {
        var read = PointerImage.DataPointerOf(image);
        Assert.False(read.IsNull);                                                                       // never read as NULL
        Assert.Equal(image, PointerImage.Of(read));                                                       // keeps its eight positions
        Assert.True(ManagedPointer.SameTarget(read, PointerImage.DataPointerOf(image)));
        Assert.False(ManagedPointer.SameTarget(read, ManagedPointer.Null));
        Assert.Equal("EC-BOUND-PTR", Assert.Throws<CobolFatalException>(() => CobolPtr.Deref(read, 1)).EcName);   // §2023:13.18.5.4 GR4
    }

    [Fact]
    public void ATokenFromAnotherRunUnit_IsForeign_NotNull_AndRaisesOnDereference()
    {
        var cell = new StorageCell { Ref = "ABCDEFGH" };
        string pointerImage = null!, programImage = null!, tagImage = null!;
        RunUnit.Run(unit =>
        {
            pointerImage = PointerImage.Of(ManagedPointer.At(cell, 2));
            programImage = PointerImage.Of(new ProgramPointer("PROGA"));
            tagImage = MessageTagImage.Of(new MessageTag("mcs", MessageParty.Server, "PAYROLL", 4));
            Assert.True(ManagedPointer.SameTarget(ManagedPointer.At(cell, 2), PointerImage.DataPointerOf(pointerImage)));   // same run unit: the same reference
        });
        RunUnit.Run(unit =>
        {
            var pointer = PointerImage.DataPointerOf(pointerImage);
            Assert.False(pointer.IsNull);
            Assert.Equal(pointerImage, PointerImage.Of(pointer));
            Assert.Equal("EC-BOUND-PTR", Assert.Throws<CobolFatalException>(() => CobolPtr.Deref(pointer, 1)).EcName);
            var program = PointerImage.ProgramPointerOf(programImage);
            Assert.False(program.IsNull);
            Assert.Equal(programImage, PointerImage.Of(program));
            var tag = MessageTagImage.TagOf(tagImage);
            Assert.False(tag.IsNull);
            Assert.Equal(MessageParty.Unresolved, tag.Party);
            Assert.Equal(tagImage, MessageTagImage.Of(tag));
        });
    }

    [Fact]
    public void ATokenAnotherProcessWrote_NeverDecodesToALiveReferenceOfThisRunUnit()
    {
        // Review R1-1: every process counted its bases from the start, so a file written by an earlier job decoded to THIS run
        // unit's k-th reference. The bases start at a per-process epoch; a token of a process whose epoch differs by 2^29 holds
        // none of this run unit's bases, for a pointer and for a message tag.
        long otherEpoch = PointerImage.Epoch ^ (1L << 29);
        RunUnit.Run(unit =>
        {
            for (int k = 1; k <= 6; k++)
            {
                var own = ManagedPointer.At(new StorageCell { Ref = "ZZZZ" }, 0);
                Assert.True(ManagedPointer.SameTarget(own, PointerImage.DataPointerOf(PointerImage.Of(own))));
                string foreign = Big(((otherEpoch + k) << 32) + 1);
                var read = PointerImage.DataPointerOf(foreign);
                Assert.Equal("EC-BOUND-PTR", Assert.Throws<CobolFatalException>(() => CobolPtr.Deref(read, 1)).EcName);
                Assert.Equal(foreign, PointerImage.Of(read));
                string foreignTag = Big((otherEpoch << 32) | (uint)k);
                _ = MessageTagImage.Of(new MessageTag("mcs", MessageParty.Server, "S" + k, k));
                Assert.Equal(MessageParty.Unresolved, MessageTagImage.TagOf(foreignTag).Party);
            }
        });
    }

    [Fact]
    public void AnEndedCellLife_IsNotRetainedByTheRunUnitsTable()
    {
        // Review R1-2: the reverse table held every imaged cell strongly for the life of the run unit.
        WeakReference first = null!;
        RunUnit.Run(unit =>
        {
            first = ImageAndFree();
            for (int i = 0; i < 5000; i++) ImageAndFree();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Assert.False(first.IsAlive);
        });
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static WeakReference ImageAndFree()
    {
        var cell = new StorageCell { Ref = "ABCD" };
        _ = PointerImage.Of(ManagedPointer.At(cell, 0));
        cell.End(StorageEnd.Freed);
        return new WeakReference(cell);
    }

    private static string Big(long v) => string.Create(8, v, static (span, a) =>
    {
        for (int i = 0; i < 8; i++) span[i] = StorageByte.ToChar((byte)(a >> (8 * (7 - i))));
    });
}
