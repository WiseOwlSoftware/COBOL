// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolSharp.Runtime;
using Xunit;

namespace CobolSharp.Tests.Unit;

/// <summary>
/// Item 10 (DEVLOG 309): runtime argument guards. A runtime support routine called with malformed
/// arguments (a code-generation defect) now throws a diagnosable <see cref="CobolRuntimeException"/> with
/// operation/target context, instead of an opaque framework exception. The guards never trip on valid input.
/// </summary>
public class RuntimeGuardTests
{
    [Fact]
    public void Buffer_NullArea_Throws()
        => Assert.Throws<CobolRuntimeException>(() => RuntimeGuard.Buffer(null!, 0, 1, "test"));

    [Theory]
    [InlineData(-1, 2)] // negative offset
    [InlineData(0, -1)] // negative length
    [InlineData(2, 3)]  // 2 + 3 > 4
    [InlineData(4, 1)]  // 4 + 1 > 4
    public void Buffer_OutOfRange_Throws(int offset, int length)
        => Assert.Throws<CobolRuntimeException>(() => RuntimeGuard.Buffer(new byte[4], offset, length, "test", "FLD"));

    [Theory]
    [InlineData(0, 4)]
    [InlineData(0, 0)]
    [InlineData(4, 0)] // empty slice at the very end is in range
    [InlineData(1, 3)]
    public void Buffer_InRange_DoesNotThrow(int offset, int length)
        => RuntimeGuard.Buffer(new byte[4], offset, length, "test"); // must not throw

    [Fact]
    public void FileRuntime_WriteRecord_BadOffset_ThrowsTypedRuntimeException()
    {
        var ex = Assert.Throws<CobolRuntimeException>(
            () => FileRuntime.WriteRecord("F", new byte[4], offset: 4, length: 10));
        Assert.Equal("RT0001", ex.Code);
        Assert.Equal("WRITE", ex.Operation);
        Assert.Equal("F", ex.Target);
    }

    [Fact]
    public void PicRuntime_DecodeNumeric_BadRange_ThrowsTypedRuntimeException()
    {
        var pic = PicDescriptorFactory.FromPicBody("9(4)");
        Assert.Throws<CobolRuntimeException>(() => PicRuntime.DecodeNumeric(new byte[4], 2, 8, pic));
    }

    [Fact]
    public void PicRuntime_DecodeNumeric_ValidRange_DecodesWithoutTripping()
    {
        var pic = PicDescriptorFactory.FromPicBody("9(4)");
        var area = Encoding.ASCII.GetBytes("1234");
        Assert.Equal(1234m, PicRuntime.DecodeNumeric(area, 0, 4, pic)); // guard is invisible on valid input
    }

    [Fact]
    public void SortRuntime_ReleaseRecord_Uninitialized_ThrowsTypedRuntimeException()
    {
        var ex = Assert.Throws<CobolRuntimeException>(
            () => SortRuntime.ReleaseRecord("NOTINIT-" + Guid.NewGuid().ToString("N"), new byte[4], 0, 4));
        Assert.Equal("RT0002", ex.Code);
        Assert.Equal("RELEASE", ex.Operation);
    }
}
