// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The BinaryCapacity truncation discipline (ISO §13.18.60.4 GR12 — COMP-5 / the BINARY-CHAR family): a store
/// bounds the value by the NATIVE two's-complement range of the item's byte width, not its digit count. Phase 4
/// M2-DATA-1 implemented the previously-stubbed path in <see cref="CobolNum"/>. <c>Store</c> WRAPS an
/// out-of-range value — the deterministic no-ON-SIZE-ERROR truncation, the width analog of high-order digit
/// truncation; <c>TryStore</c> (the ON SIZE ERROR path) reports a SIZE ERROR (returns <c>false</c>) when the
/// value leaves the range. Signed and unsigned differ by range (§14.9.25 GR8 — an unsigned receiver stores the
/// magnitude).
/// </summary>
public sealed class BinaryCapacityTests
{
    private static NumProfile Bin(int bytes, bool signed) => new()
    {
        Digits = bytes switch { 1 => 3, 2 => 5, 4 => 10, _ => signed ? 19 : 20 },
        FractionDigits = 0,
        Signed = signed,
        Truncation = NumericTruncation.BinaryCapacity,
        ByteForm = NumericByteForm.Binary,   // COMP-5 / BINARY-CHAR..DOUBLE: radix 2, `bytes` wide (GR4/GR12)
        StorageLength = bytes,
    };

    /// <summary>Store WRAPS a signed out-of-range value by two's complement (exactly a native
    /// sbyte/short/int/long cast): the byte-width modulus folds the high half to the negative range.</summary>
    [Theory]
    [InlineData(1, 127, 127)]              // in range (max)
    [InlineData(1, -128, -128)]            // in range (min)
    [InlineData(1, 128, -128)]             // one past max wraps to min
    [InlineData(1, 200, -56)]              // 200 - 256
    [InlineData(1, -129, 127)]             // one below min wraps to max
    [InlineData(1, 256, 0)]                // full period
    [InlineData(2, 32767, 32767)]
    [InlineData(2, 40000, -25536)]         // 40000 - 65536
    [InlineData(4, 2147483647, 2147483647)]
    public void Store_Signed_WrapsTwosComplement(int bytes, long value, long expected)
        => Assert.Equal((Int128)expected, CobolNum.Store(value, 0, Bin(bytes, signed: true)));

    /// <summary>An UNSIGNED receiver stores the MAGNITUDE (ISO §14.9.25 GR8) reduced modulo 2^bits.</summary>
    [Theory]
    [InlineData(1, 255, 255)]              // in range (max)
    [InlineData(1, 256, 0)]                // wraps
    [InlineData(1, 300, 44)]               // 300 mod 256
    [InlineData(1, -5, 5)]                 // magnitude, not two's-complement 251
    [InlineData(2, 65535, 65535)]
    public void Store_Unsigned_MagnitudeModWidth(int bytes, long value, long expected)
        => Assert.Equal((Int128)expected, CobolNum.Store(value, 0, Bin(bytes, signed: false)));

    /// <summary>TryStore (ON SIZE ERROR) reports SIZE ERROR (<c>false</c>) exactly when the value leaves the
    /// native range; an in-range value stores unchanged.</summary>
    [Theory]
    [InlineData(1, true, 127, true, 127)]
    [InlineData(1, true, -128, true, -128)]
    [InlineData(1, true, 128, false, 0)]
    [InlineData(1, true, -129, false, 0)]
    [InlineData(1, false, 255, true, 255)]
    [InlineData(1, false, 256, false, 0)]
    [InlineData(2, true, 32767, true, 32767)]
    [InlineData(2, true, 32768, false, 0)]
    public void TryStore_SizeErrorOnOverflow(int bytes, bool signed, long value, bool fits, long expected)
    {
        bool ok = CobolNum.TryStore(value, 0, Bin(bytes, signed), CobolRounding.Truncation, out Int128 stored);
        Assert.Equal(fits, ok);
        if (fits) Assert.Equal((Int128)expected, stored);
    }

    /// <summary>Zero-regression: an IN-RANGE store on the shared BinaryCapacity discipline (COMP-5) is unchanged
    /// — the wrap is the identity on values already within the byte-width range (PIC S9(4) COMP-5 = 2 bytes,
    /// −300 stays −300; the SignedDisplayDifferentialTests golden).</summary>
    [Fact]
    public void Store_InRange_IsIdentity()
        => Assert.Equal((Int128)(-300), CobolNum.Store(-300, 0, Bin(2, signed: true)));

    /// <summary>The 8-byte UNSIGNED range reaches above <see cref="long"/> (2^64−1) — the Int128 substrate
    /// carries it (ISO §13.18.60.4 GR12: BINARY-DOUBLE UNSIGNED 0..2^64−1).</summary>
    [Fact]
    public void Store_UnsignedDouble_HoldsAboveLongMax()
    {
        Int128 twoPow63 = (Int128)1 << 63;            // long.MaxValue + 1 — out of signed long, in 8-byte unsigned
        Assert.Equal(twoPow63, CobolNum.Store(twoPow63, 0, Bin(8, signed: false)));
    }
}
