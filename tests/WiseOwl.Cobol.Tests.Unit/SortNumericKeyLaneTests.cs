// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.IO;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>⛔ A NUMERIC SORT KEY ORDERS BY ITS ALGEBRAIC VALUE ACROSS ITS WHOLE CONTAINER (kb/Work PB186).
/// ISO §14.9.40.4 GR8 / GR19 compare key data items "according to the rules for comparison of operands in a
/// relation condition" — for numeric operands §8.8.4.2.4's algebraic value. An UNSIGNED 16-byte binary key holds
/// [0, 2^128), and the signed Int128 decode lane returns every value at or above 2^127 as a NEGATIVE number, so
/// both SORT forms need the unsigned lane: the runtime key column (Format 1 and MERGE) and the emitted table
/// comparer (Format 2), which now decodes through the ONE windowed reader instead of a private copy (the source
/// half, that no other CodeGen site decodes an image, is <see cref="NumericImageDecodeSiteDriftTests"/>).</summary>
public sealed class SortNumericKeyLaneTests
{
    private static NumProfile Binary(int width, bool signed) => new()
    {
        Digits = 31, FractionDigits = 0, Signed = signed, Truncation = NumericTruncation.BinaryCapacity,
        ByteForm = NumericByteForm.Binary, StorageLength = width,
    };

    /// <summary>The predicate is the IMAGE's: only an unsigned radix-2 image of 16 bytes exceeds Int128. An 8-byte
    /// unsigned image fits Int128 whole (ParseImage returns it non-negative); a signed one orders by its sign.</summary>
    [Theory]
    [InlineData(16, false, true)]
    [InlineData(16, true, false)]
    [InlineData(8, false, false)]
    public void ImageExceedsInt128_IsTheUnsignedSixteenByteImage(int width, bool signed, bool expected) =>
        Assert.Equal(expected, Binary(width, signed).ImageExceedsInt128);

    [Theory]
    [InlineData(false, new[] { "SEV", "HI8", "ALL" })]
    [InlineData(true, new[] { "ALL", "HI8", "SEV" })]
    public void UnsignedWideBinaryKey_OrdersByValue_AboveTwoToThe127(bool descending, string[] expected)
    {
        const string name = "PB186-UNIT-SD";
        static string Rec(byte[] key, string tag) => new string(key.Select(b => (char)b).ToArray()) + tag;
        var seven = new byte[16];
        seven[15] = 7;
        var keys = new[] { new CobolSort.Key(0, 16, descending, CobolSort.KeyClass.Numeric, Binary(16, signed: false)) };
        CobolSort.Init(name, keys);
        CobolSort.Release(name, Rec(Enumerable.Repeat((byte)0xFF, 16).ToArray(), "ALL"), 0, 0, int.MaxValue);   // 2^128 - 1
        CobolSort.Release(name, Rec(seven, "SEV"), 0, 0, int.MaxValue);
        CobolSort.Release(name, Rec(Enumerable.Repeat((byte)0x80, 16).ToArray(), "HI8"), 0, 0, int.MaxValue);   // >= 2^127
        CobolSort.Sort(name, duplicatesInOrder: true);
        var order = new List<string>();
        while (CobolSort.Return(name, out string rec)) order.Add(rec[16..]);
        CobolSort.Close(name);
        Assert.Equal(expected, order);
    }
}
