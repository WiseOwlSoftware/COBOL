// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Numerics;
using CobolNet.Binding.Model;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE DRIFT TEST kb/Work R10 (F74) DEMANDED: the HIGHEST/LOWEST-ALGEBRAIC fold and the runtime's
/// BinaryCapacity discipline are ONE rule — "the item owns its full container range" (ISO §13.18.60.4 GR12;
/// §15.43.4 r2 / §15.58.4 r2 make the fold exactly that range's ends). The fold used to carry its own
/// hand-maintained usage list while <c>PicInfo.Truncation</c> carried the same list again, and the two runtime
/// halves (<c>WrapBinary</c>/<c>InBinaryRange</c>) disagreed with both at the 16-byte tier (the shift-mask bug:
/// modulus 2^128 computed as 1). The fold now READS <c>PicInfo.Truncation</c>; this suite pins the residual
/// agreements the type system cannot:
/// <list type="bullet">
///   <item>the BinaryCapacity usage SET itself (a new usage entering the table must come through here);</item>
///   <item>every container's fold bound is exactly the runtime's TryStore acceptance boundary;</item>
///   <item>the 16-byte container-bits store contract behind the <c>unchecked((UInt128))</c> emitted cast;</item>
///   <item>the carrier table (<c>PicInfo.ClrType</c>) that makes an unsigned item's range representable.</item>
/// </list>
/// Every expected value is COMPUTED from the container width and signedness — never from observed output.
/// </summary>
public sealed class AlgebraicFoldContainerAgreementTests
{
    private static PicInfo Pic(Usage usage, int digits, bool signed) =>
        new(PicCategory.Numeric, usage, Length: digits, Digits: digits, Scale: 0, Signed: signed);

    private static NumProfile Prof(Usage usage, int digits, bool signed)
    {
        var pic = Pic(usage, digits, signed);
        return new NumProfile
        {
            Digits = digits,
            FractionDigits = 0,
            Signed = signed,
            SignKind = NumericSign.BinaryMinus,
            Truncation = pic.Truncation,
            ByteForm = pic.ByteForm,
            StorageLength = pic.StorageWidth,
        };
    }

    // ── 1. The ONE capacity table: BinaryCapacity is exactly the COMP-5 / BINARY-CHAR..DOUBLE set ──────────

    [Fact]
    public void BinaryCapacityUsageSet_IsExactlyTheContainerFamily()
    {
        var expected = new[] { Usage.Comp5, Usage.BinaryChar, Usage.BinaryShort, Usage.BinaryLong, Usage.BinaryDouble };
        foreach (Usage usage in System.Enum.GetValues<Usage>())
        {
            bool inFamily = System.Array.IndexOf(expected, usage) >= 0;
            // Digits chosen so StorageWidth is meaningful for the digit-laddered usages.
            Assert.Equal(inFamily, Pic(usage, 9, signed: true).Truncation == NumericTruncation.BinaryCapacity);
        }
    }

    // ── 2. Fold bound == runtime acceptance boundary, for every container tier ─────────────────────────────
    // The fold computes 2^(bits−1)−1 / −2^(bits−1) signed and 2^bits−1 / 0 unsigned from PicInfo.StorageWidth
    // (§13.18.60.4 GR12); the runtime's TryStore must accept exactly that range. Each row derives bits the
    // same way the fold does and probes the boundary from BOTH sides where a wider carrier can express it.

    public static TheoryData<Usage, int, bool> Containers()
    {
        var d = new TheoryData<Usage, int, bool>();
        foreach (bool signed in new[] { false, true })
        {
            d.Add(Usage.BinaryChar, 3, signed);      // 1 byte
            d.Add(Usage.BinaryShort, 5, signed);     // 2 bytes
            d.Add(Usage.BinaryLong, 10, signed);     // 4 bytes
            d.Add(Usage.BinaryDouble, signed ? 19 : 20, signed);   // 8 bytes
            d.Add(Usage.Comp5, 4, signed);           // 2 bytes  (digit ladder)
            d.Add(Usage.Comp5, 9, signed);           // 4 bytes
            d.Add(Usage.Comp5, 18, signed);          // 8 bytes
            d.Add(Usage.Comp5, 19, signed);          // 16 bytes (the F73/F74 tier)
        }
        return d;
    }

    [Theory]
    [MemberData(nameof(Containers))]
    public void FoldBound_IsTheTryStoreBoundary(Usage usage, int digits, bool signed)
    {
        var pic = Pic(usage, digits, signed);
        var prof = Prof(usage, digits, signed);
        Assert.Equal(NumericTruncation.BinaryCapacity, prof.Truncation);
        int bits = 8 * pic.StorageWidth;

        BigInteger highest = signed ? (BigInteger.One << (bits - 1)) - 1 : (BigInteger.One << bits) - 1;
        BigInteger lowest = signed ? -(BigInteger.One << (bits - 1)) : BigInteger.Zero;

        // The HIGHEST bound is accepted…
        if (highest <= (BigInteger)Int128.MaxValue)
            Assert.True(CobolNum.TryStore((Int128)highest, 0, prof, CobolRounding.Truncation, out _));
        else
            Assert.True(CobolNum.TryStoreU((UInt128)highest, 0, prof, CobolRounding.Truncation, out _));

        // …and one past it is rejected, wherever a carrier can express that value (2^128 exceeds every
        // carrier — there the container IS the UInt128 domain, and the bits round-trip below is the pin).
        BigInteger past = highest + 1;
        if (past <= (BigInteger)Int128.MaxValue)
            Assert.False(CobolNum.TryStore((Int128)past, 0, prof, CobolRounding.Truncation, out _));
        else if (past <= (BigInteger)UInt128.MaxValue)
            Assert.False(CobolNum.TryStoreU((UInt128)past, 0, prof, CobolRounding.Truncation, out _));

        // The LOWEST bound is accepted and one below is rejected (Int128 expresses both for every tier:
        // the deepest signed bound is −2^127 = Int128.MinValue, and an unsigned item's −1 is just −1).
        Assert.True(CobolNum.TryStore((Int128)lowest, 0, prof, CobolRounding.Truncation, out _));
        if (lowest - 1 >= (BigInteger)Int128.MinValue)
        {
            // ⚠ An UNSIGNED receiver's TryStore applies the §14.9.25.4 GR6d2b magnitude rule, so −1 stores
            // as 1 (in range): the "one below zero rejects" leg exists only for a SIGNED container.
            if (signed)
                Assert.False(CobolNum.TryStore((Int128)(lowest - 1), 0, prof, CobolRounding.Truncation, out _));
        }
    }

    // ── 3. The 16-byte container-bits contract (behind the emitted unchecked((UInt128)) cast) ──────────────

    [Fact]
    public void SixteenByteUnsigned_StoresFullRange_AsContainerBits()
    {
        var prof = Prof(Usage.Comp5, 19, signed: false);

        // The container max round-trips: 2^128−1 arrives as UInt128, lands as bits −1, reinterprets back.
        Int128 bits = CobolNum.StoreU(UInt128.MaxValue, 0, prof);
        Assert.Equal(UInt128.MaxValue, unchecked((UInt128)bits));

        // An Int128-lane store of MinValue is the magnitude 2^127 (GR6d2b), whose bits are MinValue itself.
        Assert.Equal((UInt128)1 << 127, unchecked((UInt128)CobolNum.Store(Int128.MinValue, 0, prof)));

        // Ordinary magnitudes are themselves (the F74 regression pin — modulus-1 stored 0 for everything).
        Assert.Equal((Int128)5, CobolNum.Store((Int128)5, 0, prof));
        Assert.Equal((Int128)5, CobolNum.Store((Int128)(-5), 0, prof));
    }

    [Fact]
    public void SixteenByteSigned_RangeIsGenuine()
    {
        var prof = Prof(Usage.Comp5, 19, signed: true);
        // The F74 regression pins: the signed 16-byte range read as EMPTY (half = 0), so every checked store
        // failed and every unchecked store collapsed to 0.
        Assert.Equal((Int128)5, CobolNum.Store((Int128)5, 0, prof));
        Assert.True(CobolNum.TryStore((Int128)0, 0, prof, CobolRounding.Truncation, out _));
        Assert.True(CobolNum.TryStore(Int128.MaxValue, 0, prof, CobolRounding.Truncation, out _));
        Assert.True(CobolNum.TryStore(Int128.MinValue, 0, prof, CobolRounding.Truncation, out _));
        // One past the signed max IS expressible in UInt128 — the boundary rejects it.
        Assert.False(CobolNum.TryStoreU((UInt128)1 << 127, 0, prof, CobolRounding.Truncation, out _));
    }

    // ── 4. The Widen funnel and the unsigned relation lane ─────────────────────────────────────────────────

    [Fact]
    public void Widen_IsExactToTheIntermediate_AndLoudBeyond()
    {
        Assert.Equal(Int128.MaxValue, CobolNum.Widen((UInt128)Int128.MaxValue));
        Assert.Throws<CobolSizeError>(() => CobolNum.Widen((UInt128)Int128.MaxValue + 1));
    }

    [Fact]
    public void CompareU_ComparesAlgebraicValues_AcrossLanesAndScales()
    {
        Assert.True(CobolNum.CompareU(UInt128.MaxValue, 0, Int128.MaxValue, 0) > 0);
        Assert.True(CobolNum.CompareU(UInt128.MaxValue, 0, (Int128)(-1), 0) > 0);
        Assert.Equal(0, CobolNum.CompareU((UInt128)5, 1, (UInt128)50, 2));       // 0.5 == 0.50
        Assert.True(CobolNum.CompareU((Int128)5, 0, (UInt128)49, 1) > 0);        // 5 > 4.9 (mirrored order)
        Assert.Equal(0, CobolNum.CompareU(UInt128.MaxValue, 2, UInt128.MaxValue, 2));
    }

    // ── 5. The carrier table (PicInfo.ClrType) that makes the range representable ──────────────────────────

    [Theory]
    [InlineData(Usage.Comp5, 9, false, "long")]      // 4-byte container fits long
    [InlineData(Usage.Comp5, 18, false, "ulong")]    // 8-byte unsigned owns [0, 2^64) — the F75 tier
    [InlineData(Usage.Comp5, 18, true, "long")]      // 8-byte signed IS long
    [InlineData(Usage.Comp5, 19, false, "UInt128")]  // 16-byte unsigned owns [0, 2^128) — the F73 tier
    [InlineData(Usage.Comp5, 19, true, "Int128")]    // 16-byte signed IS Int128
    [InlineData(Usage.BinaryDouble, 20, false, "ulong")]
    // Signed BINARY-DOUBLE synthesizes 19 digits (BinaryItem), so it rides the wide tier's Int128 — wider
    // than its 8-byte container needs, but the range [−2^63, 2^63) is fully representable, which is the
    // carrier criterion this table pins.
    [InlineData(Usage.BinaryDouble, 19, true, "Int128")]
    [InlineData(Usage.Binary, 18, false, "long")]    // DigitCount usages never need the unsigned carriers
    public void CarrierTable_MatchesTheContainer(Usage usage, int digits, bool signed, string carrier)
    {
        Assert.Equal(carrier, Pic(usage, digits, signed).ClrType);
    }
}
