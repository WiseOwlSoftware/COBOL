// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;

namespace CobolNet.Runtime.IO.Store;

/// <summary>The KEY ORDERS of docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.4: <see cref="Ordinal"/> (the bytes;
/// also the NUMERIC order of a relative record number and of the page-number keys of the FREE and LEDGER trees, stored
/// big-endian so that byte order IS numeric order), <see cref="WeightedKeyOrder"/> (decode, then the key's collating
/// sequence) and <see cref="CompositeKeyOrder"/> (a key order, then the trailing big-endian ordinal of an alternate
/// tree's entry), and the key codecs of the numeric keys.</summary>
internal static class KeyOrders
{
    /// <summary>The ORDINAL order: unsigned bytes, left to right, a shorter key below a longer one it begins.</summary>
    public static IKeyOrder Ordinal { get; } = new OrdinalKeyOrder();

    /// <summary>A page number as a key (the FREE and LEDGER trees): big-endian, so that <see cref="Ordinal"/> is the
    /// numeric order.</summary>
    public static byte[] PageKey(uint page)
    {
        var key = new byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32BigEndian(key, page);
        return key;
    }

    /// <summary>The inverse of <see cref="PageKey"/>.</summary>
    public static uint PageOf(ReadOnlySpan<byte> key) => BinaryPrimitives.ReadUInt32BigEndian(key);

    /// <summary>A relative record number as its key (2.2.1.4, "A RELATIVE record is one entry"): 8 bytes big-endian.</summary>
    public static byte[] RecordNumberKey(ulong recordNumber)
    {
        var key = new byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64BigEndian(key, recordNumber);
        return key;
    }
}

/// <summary>The ORDINAL key order: unsigned bytes. With no CODE-SET clause the channel byte of a character position is
/// its ISO/IEC 8859-1 code under the storage-byte law (<see cref="FileCharacterSet"/>; HIGH-VALUE is 0xFF), so this IS
/// the native order of a key no COLLATING SEQUENCE clause names (§12.4.5.7.4 GR4: <i>"the native alphanumeric collating
/// sequence applies"</i>) — the order <c>IndexedConnector.KeyCompare</c> gives
/// such a key with <see cref="string.CompareOrdinal(string, string)"/>.</summary>
internal sealed class OrdinalKeyOrder : IKeyOrder
{
    /// <inheritdoc/>
    public bool PrefixConsistent => true;

    /// <inheritdoc/>
    public int Compare(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right) => left.SequenceCompareTo(right);

    /// <inheritdoc/>
    public int ComparePrefix(ReadOnlySpan<byte> stored, ReadOnlySpan<byte> prefix) =>
        stored[..Math.Min(stored.Length, prefix.Length)].SequenceCompareTo(prefix);
}

/// <summary>The WEIGHTED key order: the channel bytes decoded to the native character set — the medium's one byte per
/// character position, then the CODE-SET conversion's input rule (§13.18.13.4 GR6 a)), then the storage-byte law
/// (<see cref="FileCharacterSet.FromChannel"/>) — and compared under the key's collating sequence (§12.4.5.7.4) exactly as
/// <c>IndexedConnector.KeyCompare</c> compares today: native ordinal with no sequence; a locale sequence through its
/// materialized keys, which its collator's key cache builds once per distinct value (kb/Work PB106); any other declared
/// sequence by its weights. A file with a CODE-SET clause and no COLLATING SEQUENCE takes this order with no collation,
/// because its channel byte order is the coded set's and not the native one.</summary>
internal sealed class WeightedKeyOrder(CodeSetConversion? codeSet, CobolCollation? collation) : IKeyOrder
{
    /// <summary>The decoding is one character per byte position, so the order is prefix-consistent exactly when its
    /// comparison is: native ordinal, or a sequence compared by the standard comparison
    /// (<see cref="CobolCollation.UsesStandardComparison"/>).</summary>
    public bool PrefixConsistent { get; } = collation is null || collation.UsesStandardComparison;

    /// <inheritdoc/>
    public int Compare(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right) => CompareNative(Native(left), Native(right));

    /// <inheritdoc/>
    public int ComparePrefix(ReadOnlySpan<byte> stored, ReadOnlySpan<byte> prefix) =>
        CompareNative(Native(stored[..Math.Min(stored.Length, prefix.Length)]), Native(prefix));

    private string Native(ReadOnlySpan<byte> channel) =>
        FileCharacterSet.FromChannel(FileCharacterSet.Medium.GetString(channel), codeSet);

    private int CompareNative(string left, string right)
    {
        if (collation is null) return string.CompareOrdinal(left, right);
        if (collation.SupportsKeys) return collation.KeyOf(left)!.CompareTo(collation.KeyOf(right));
        return collation.Compare(left, right);
    }
}

/// <summary>The COMPOSITE key order of an ALTERNATE tree's entry (2.2.1.4): the alternate key's own order over the key
/// part, then the trailing 8-byte big-endian release ordinal (§14.9.51.4 GR40), so the duplicates of one key value are
/// in the order their ordinals were minted (§14.9.35.4 GR24). A prefix compares the key part only.</summary>
internal sealed class CompositeKeyOrder(IKeyOrder keyOrder) : IKeyOrder
{
    /// <summary>The bytes of the trailing ordinal.</summary>
    public const int OrdinalBytes = sizeof(ulong);

    /// <inheritdoc/>
    public bool PrefixConsistent => keyOrder.PrefixConsistent;

    /// <inheritdoc/>
    public int Compare(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        int c = keyOrder.Compare(left[..^OrdinalBytes], right[..^OrdinalBytes]);
        return c != 0 ? c : left[^OrdinalBytes..].SequenceCompareTo(right[^OrdinalBytes..]);
    }

    /// <inheritdoc/>
    public int ComparePrefix(ReadOnlySpan<byte> stored, ReadOnlySpan<byte> prefix) =>
        keyOrder.ComparePrefix(stored[..^OrdinalBytes], prefix);

    /// <summary>The entry key of an alternate key value <paramref name="keyPart"/> and its release ordinal.</summary>
    public static byte[] Compose(ReadOnlySpan<byte> keyPart, ulong ordinal)
    {
        var key = new byte[keyPart.Length + OrdinalBytes];
        keyPart.CopyTo(key);
        BinaryPrimitives.WriteUInt64BigEndian(key.AsSpan(keyPart.Length), ordinal);
        return key;
    }
}
