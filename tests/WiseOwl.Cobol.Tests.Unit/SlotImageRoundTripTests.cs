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
    public void DataPointerOf_APointerDisplacedBeyondTheWindow_ReadsBackAsItself_NeverAsAnotherArea()
    {
        // kb/Work PB4925: base + displacement names another area's window once the displacement leaves [-2^31, 2^31), so a far pointer takes a base of its
        // own. A + (B's base - A's base) and that + 5 would otherwise decode as B and B + 5. The displacement is read from the two images, never assumed:
        // bases are process-wide, so a test running in parallel may take bases between A's and B's (kb/Work PB5928).
        var a = new StorageCell { Ref = "AAAA" };
        var b = new StorageCell { Ref = "BBBB" };
        long baseOfA = AddressOf(PointerImage.Of(ManagedPointer.At(a, 0)));
        long toB = AddressOf(PointerImage.Of(ManagedPointer.At(b, 0))) - baseOfA;
        Assert.True(toB >= 1L << 32, $"B's base lies {toB} past A's");
        foreach (long offset in new long[] { toB, toB + 5, 1L << 32, 1L << 31, -(1L << 31) - 1, long.MaxValue, long.MinValue })
        {
            var written = ManagedPointer.At(a, offset);
            string image = PointerImage.Of(written);
            var read = PointerImage.DataPointerOf(image);
            var cell = Assert.IsType<CellPointer>(read);
            Assert.Same(a, cell.Cell);
            Assert.Equal(offset, cell.Offset);
            Assert.Equal(image, PointerImage.Of(read));                                 // equal pointers image equal (§2023:8.8.4.2.16)
            Assert.False(ManagedPointer.SameTarget(ManagedPointer.At(b, unchecked(offset - toB)), read), $"offset {offset}");
        }
        // The window's edges stay base + displacement: -2^31 and 2^31 - 1 decode back with no base of their own.
        foreach (long offset in new long[] { -(1L << 31), (1L << 31) - 1 })
            Assert.Equal(offset, Assert.IsType<CellPointer>(PointerImage.DataPointerOf(PointerImage.Of(ManagedPointer.At(a, offset)))).Offset);
    }

    [Fact]
    public void DataPointerOf_AFarPointer_ReadsBackAsItself_AfterACollection()
    {
        // kb/Work PB5928: the table held a far pointer's entry (a FarDisplacement nothing else references) as a WeakReference, so any collection between
        // the WRITE and the READ dropped it and the token read back as a ForeignAddress while its cell was alive. The image is taken in a frame of its own,
        // so no local keeps the entry alive, and the collection here is the one a long batch, or a test running in parallel, makes at any moment.
        var cell = new StorageCell { Ref = "AAAA" };
        RunUnit.Run(unit =>
        {
            foreach (long offset in new long[] { 1L << 32, long.MinValue })
            {
                string image = ImageOf(cell, offset);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                var read = Assert.IsType<CellPointer>(PointerImage.DataPointerOf(image));
                Assert.Same(cell, read.Cell);
                Assert.Equal(offset, read.Offset);
            }
        });
        GC.KeepAlive(cell);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static string ImageOf(StorageCell cell, long offset) => PointerImage.Of(ManagedPointer.At(cell, offset));

    [Fact]
    public void DataPointerOf_ASavedLocaleHandle_WhoseOnlyHolderIsItsToken_ReadsBackAsItself_AfterACollection()
    {
        // kb/Work PB5928 review R1-1 (probe locptr5d.cob): SET P TO LOCALE LC_ALL, WRITE P, SET P TO NULL, READ, SET LOCALE LC_ALL TO P. The handle's only
        // holder between the WRITE and the READ is the token on the file, so the table held it weakly and a collection dropped it: the READ restored a
        // ForeignAddress and SET LOCALE ended the run fatally (EC-LOCALE-INVALID-PTR). The token is the program still holding the handle.
        RunUnit.Run(unit =>
        {
            string image = ImageOfSavedLocale(unit);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var read = PointerImage.DataPointerOf(image);
            Assert.IsType<SavedLocalePointer>(read);
            Assert.True(unit.Locale.IsSavedLocale(read));
            Assert.Equal(image, PointerImage.Of(read));
        });
    }

    [Fact]
    public void DataPointerOf_AnAddresslessCarrier_WhoseOnlyHolderIsItsToken_ReadsBackAsItself_AfterACollection()
    {
        // kb/Work PB5928 review R1-1: every addressless identity, not only a saved-locale handle, can leave the program's hold for a file.
        RunUnit.Run(unit =>
        {
            string image = ImageOfCarrier();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var read = Assert.IsType<ManagedPointer<string>>(PointerImage.DataPointerOf(image));
            Assert.Equal("carried", read.Value);
        });
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static string ImageOfSavedLocale(RunUnit unit) => PointerImage.Of(unit.Locale.Save(userDefault: false));

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static string ImageOfCarrier() => PointerImage.Of(ManagedPointer<string>.Cell("carried"));

    [Fact]
    public void ACellOfManyLives_HoldsTheBasesOfItsCurrentLifeOnly()
    {
        // kb/Work PB5928 review R1-2: a static cell re-seeded at every CANCEL lives one life per CALL, and the process-wide memo kept a base (and a far
        // base) for every life it was imaged in. Structural, never timed: after 100000 lives the memo holds the current life's bases and no more.
        var cell = new StorageCell { Ref = "WXYZ" };
        RunUnit.Run(unit =>
        {
            for (int life = 0; life < 100_000; life++)
            {
                _ = PointerImage.Of(ManagedPointer.At(cell, 1));
                _ = PointerImage.Of(ManagedPointer.At(cell, 1L << 32));
                cell.End(StorageEnd.Cancelled);
            }
            var near = ManagedPointer.At(cell, 2);
            var far = ManagedPointer.At(cell, (1L << 32) + 2);
            string nearImage = PointerImage.Of(near), farImage = PointerImage.Of(far);
            Assert.Equal((1, 1), PointerImage.BasesHeldFor(cell));
            Assert.True(ManagedPointer.SameTarget(near, PointerImage.DataPointerOf(nearImage)));    // the current life still round-trips
            Assert.True(ManagedPointer.SameTarget(far, PointerImage.DataPointerOf(farImage)));
        });
    }

    [Fact]
    public void APointerIntoAnEndedLife_ImagesEqual_UntilTheCellIsImagedInALaterLife()
    {
        // kb/Work PB5928 review R1-2: an ended life's base is dropped when the cell is next imaged in a later life; until then equal pointers image equal.
        var cell = new StorageCell { Ref = "ABCD" };
        RunUnit.Run(unit =>
        {
            var stale = Assert.IsType<CellPointer>(ManagedPointer.At(cell, 1));
            string before = PointerImage.Of(stale);
            cell.End(StorageEnd.Cancelled);
            Assert.Equal(before, PointerImage.Of(new CellPointer(cell, 1, stale.Generation)));
            _ = PointerImage.Of(ManagedPointer.At(cell, 1));                                  // the cell's next life: the ended life's base goes
            Assert.Equal((1, 0), PointerImage.BasesHeldFor(cell));
            Assert.Equal("EC-BOUND-PTR", Assert.Throws<CobolFatalException>(() => CobolPtr.Deref(PointerImage.DataPointerOf(before), 1)).EcName);   // §2023:13.18.5.4 GR4
        });
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
    public void AMessageTagTokenAnotherRunUnitOfThisProcessWrote_IsForeign_EvenWhenThisRunUnitImagedItsOwn()
    {
        // Review R2-1: tag ids counted from 1 in every run unit, so run unit B's first tag shared run unit A's first id.
        string fromA = null!;
        RunUnit.Run(unit => fromA = MessageTagImage.Of(new MessageTag("mcs", MessageParty.Server, "PAYROLL-A", 4)));
        RunUnit.Run(unit =>
        {
            string own = MessageTagImage.Of(new MessageTag("mcs", MessageParty.Server, "OTHER-B", 9));
            Assert.NotEqual(fromA, own);
            var read = MessageTagImage.TagOf(fromA);
            Assert.Equal(MessageParty.Unresolved, read.Party);
            Assert.Equal(fromA, MessageTagImage.Of(read));
            Assert.Equal("OTHER-B", MessageTagImage.TagOf(own).Name);
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

    /// <summary>The eight positions read as the big-endian address they render, the inverse of <see cref="Big"/>.</summary>
    private static long AddressOf(string image)
    {
        long a = 0;
        foreach (char c in image) a = (a << 8) | StorageByte.ToByte(c);
        return a;
    }

    private static string Big(long v) => string.Create(8, v, static (span, a) =>
    {
        for (int i = 0; i < 8; i++) span[i] = StorageByte.ToChar((byte)(a >> (8 * (7 - i))));
    });
}
