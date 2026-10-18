// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO.Store;

/// <summary>⛔ THE ORDER OF ONE B+ TREE'S KEYS (docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.4, "Keys are BYTES in
/// the file's channel character set"): chosen once per tree at OPEN, and the only thing <see cref="BTree"/> asks about a
/// key. Keys are the channel bytes the frame payload carries (2.2.1.5), so an order that is not the bytes' own decodes them
/// first. The four orders are in <see cref="KeyOrders"/>.
/// <para><see cref="ComparePrefix"/> is the order of a START's partial key (§14.9.41.4 GR17 b) sets the length of the
/// first temporary area; GR17 c) and d): <i>"This second temporary area is truncated to the same length as the first"</i>):
/// the leading <c>prefix.Length</c> byte positions of a stored key's KEY PART (a composite key's part before its ordinal)
/// against the prefix. A seek under a prefix is a seek in the same tree, so the order must be PREFIX-CONSISTENT — a key
/// below another never has a greater prefix — which every order here is, because each compares character position by
/// character position from the left (GR17 e): operands of equal length, as §8.8.4.2.7 compares them).</para></summary>
internal interface IKeyOrder
{
    /// <summary>The order of two whole stored keys: negative, zero or positive.</summary>
    int Compare(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right);

    /// <summary>The order of the leading <c>prefix.Length</c> positions of <paramref name="stored"/>'s key part against
    /// <paramref name="prefix"/> (a stored key part shorter than the prefix compares on its whole length).</summary>
    int ComparePrefix(ReadOnlySpan<byte> stored, ReadOnlySpan<byte> prefix);
}
