// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The data-pointer runtime rules (Phase-4b increment 2; ISO §8.8.4.2 pointer equality, §13.18.5 GR3/GR4,
/// §14.9.3 GR1/GR2/GR6, §14.9.15 GR1, §14.9.39 F10 GR18/GR20). End-to-end behavior rides the three pointer
/// goldens; these lock the carrier/cell semantics the emitters wire to.
/// </summary>
public sealed class CobolPtrTests
{
    [Fact]
    public void SameTarget_IsStructural_OverCellAndOffset()
    {
        var cell = new StorageCell { Ref = "ABCDEFGHIJ" };
        // Two INDEPENDENT window pointers to the same address compare EQUAL (§8.8.4.2 :9772 — "equal if
        // they reference the same address"; increment 1's instance identity could not express this).
        Assert.True(ManagedPointer.SameTarget(ManagedPointer.At(cell, 3), ManagedPointer.At(cell, 3)));
        Assert.False(ManagedPointer.SameTarget(ManagedPointer.At(cell, 3), ManagedPointer.At(cell, 4)));
        Assert.False(ManagedPointer.SameTarget(ManagedPointer.At(cell, 0), ManagedPointer.At(new StorageCell { Ref = "ABCDEFGHIJ" }, 0)));
        Assert.True(ManagedPointer.SameTarget(ManagedPointer.Null, null));
        Assert.False(ManagedPointer.SameTarget(ManagedPointer.At(cell, 0), ManagedPointer.Null));
    }

    [Fact]
    public void UpBy_MovesByBytes_AndComposes()
    {
        var cell = new StorageCell { Ref = "ABCDEFGHIJ" };
        var p = ManagedPointer.At(cell, 0);
        var p4 = CobolPtr.UpBy(p, 4);
        var p2 = CobolPtr.UpBy(p4, -2);
        Assert.Equal(4, ((CellPointer)p4).Offset);      // §14.9.39 F10 GR20 — byte-granular
        Assert.Equal(2, ((CellPointer)p2).Offset);
        Assert.Equal(0, ((CellPointer)p).Offset);       // pointers are VALUES — UpBy never mutates its operand
    }

    [Fact]
    public void UpByScaled_IsTheExactGr19ValueRule()
    {
        var cell = new StorageCell { Ref = "ABCDEFGHIJ" };
        var p = ManagedPointer.At(cell, 0);
        // 2.0 at scale 1 (scaled 20) IS an integer value — moves by 2 (§14.9.39 F10 GR19 is a VALUE rule).
        Assert.Equal(2, ((CellPointer)CobolPtr.UpByScaled(p, 20, 1)).Offset);
        // 2.5 at scale 1 (scaled 25) is NOT an integer — EC-SIZE-ADDRESS (Fatal), never a silent truncation.
        Assert.Equal("EC-SIZE-ADDRESS",
            Assert.Throws<CobolFatalException>(() => CobolPtr.UpByScaled(p, 25, 1)).EcName);
    }

    [Fact]
    public void UpBy_Null_IsEcDataPtrNull()
    {
        var ex = Assert.Throws<CobolFatalException>(() => CobolPtr.UpBy(ManagedPointer.Null, 1));
        Assert.Equal("EC-DATA-PTR-NULL", ex.EcName);    // F10 GR18
    }

    [Fact]
    public void Deref_Null_And_OutOfBounds_AreLoud()
    {
        Assert.Equal("EC-DATA-PTR-NULL",
            Assert.Throws<CobolFatalException>(() => CobolPtr.Deref(ManagedPointer.Null, 1)).EcName);   // §13.18.5 GR3
        var cell = new StorageCell { Ref = "ABCDE" };
        Assert.Equal("EC-BOUND-PTR",
            Assert.Throws<CobolFatalException>(() => CobolPtr.Deref(ManagedPointer.At(cell, 4), 5)).EcName);   // GR4 — window past the end
        Assert.Same(cell, CobolPtr.Deref(ManagedPointer.At(cell, 4), 1));   // the last byte is addressable
    }

    [Fact]
    public void Allocate_SizeRules_And_InitializedFill()
    {
        Assert.True(CobolPtr.Allocate(0).IsNull);        // §14.9.3 GR2 — ≤0 ⇒ NULL, no exception condition
        Assert.True(CobolPtr.Allocate(-5).IsNull);
        var p = (CellPointer)CobolPtr.Allocate(4);
        Assert.Equal("    ", p.Cell.Ref);                // GR8 undefined — this implementation space-fills
        Assert.True(p.Cell.Allocated);
        var z = (CellPointer)CobolPtr.Allocate(3, zeroFill: true);
        Assert.Equal("\0\0\0", z.Cell.Ref);              // GR6 INITIALIZED — binary zeros
    }

    [Fact]
    public void Free_ThreeWay_And_DanglingAliasIsLoud()
    {
        // (a) start-of-allocation: released, operand nulls; a dangling alias trips Deref loud (GR1a).
        var p = CobolPtr.Allocate(5);
        var alias = CobolPtr.UpBy(p, 1);
        Assert.True(CobolPtr.Free(p, out bool na1).IsNull);
        Assert.False(na1);
        Assert.Equal("EC-BOUND-PTR", Assert.Throws<CobolFatalException>(() => CobolPtr.Deref(alias, 1)).EcName);
        // (b) NULL: no-op.
        Assert.True(CobolPtr.Free(ManagedPointer.Null, out bool na2).IsNull);
        Assert.False(na2);
        // (c) not the start of an allocation (a mid-block window / an ADDRESS OF cell): unchanged + notAlloc.
        var q = CobolPtr.UpBy(CobolPtr.Allocate(5), 2);
        Assert.Same(q, CobolPtr.Free(q, out bool na3));
        Assert.True(na3);                                // GR1c — EC-STORAGE-NOT-ALLOC (nonfatal)
        var addr = ManagedPointer.At(new StorageCell { Ref = "HELLO" }, 0);   // not Allocated
        Assert.Same(addr, CobolPtr.Free(addr, out bool na4));
        Assert.True(na4);
        // Double FREE: the second is GR1c (already freed — not an allocation start anymore).
        var r = CobolPtr.Allocate(2);
        CobolPtr.Free(r, out _);
        Assert.Same(r, CobolPtr.Free(r, out bool na5));
        Assert.True(na5);
    }
}
