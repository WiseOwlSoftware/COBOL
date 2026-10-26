// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime.IO.Store;

/// <summary>⛔ THE ORDER OF ONE B+ TREE'S KEYS (docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.4, "Keys are BYTES in
/// the file's channel character set"): chosen once per tree at OPEN, and the only thing <see cref="BTree"/> asks about a
/// key. Keys are the channel bytes the frame payload carries (2.2.1.5), so an order that is not the bytes' own decodes them
/// first. The four orders are in <see cref="KeyOrders"/>; the order of the transactions' UNDO and HOLDINGS trees, which
/// composes the file's own, is <see cref="HoldingsKeyOrder"/>.
/// <para><see cref="ComparePrefix"/> is the order of a START's partial key (§14.9.41.4 GR17 b) sets the length of the
/// first temporary area; GR17 c) and d): <i>"This second temporary area is truncated to the same length as the first"</i>):
/// the leading <c>prefix.Length</c> byte positions of a stored key's KEY PART (a composite key's part before its ordinal)
/// against the prefix. GR17 c) and d) define the answer as a SEQUENTIAL search — <i>"the file is searched sequentially"</i>,
/// or <i>"in reverse order"</i> for the LESS relations — so a seek may narrow it by binary search only when
/// <see cref="PrefixNarrows"/> says the qualifying entries are one run of the key order; otherwise
/// <see cref="BTree.Seek"/> walks the tree in the search's own direction, from the PRIMARY-LEVEL BOUND
/// (<see cref="BoundOf"/>, <see cref="CompareBound"/>) that the tree's high-water marks (<see cref="MarksOf"/>) make exact,
/// and only through the run of entries that tie the bound (2.2.1.4, the partial-key START).</para>
/// </summary>
internal interface IKeyOrder
{
    /// <summary>⛔ Whether the entries a prefix of <paramref name="prefixLength"/> positions qualifies, under every relation,
    /// are one run of the key order, so a binary search finds its end. True for an order that compares character position
    /// by character position from the left — the bytes, and the STANDARD comparison of §8.8.4.2.7 1), which §14.9.41.4
    /// GR17 e) names for operands of equal length — at every length: a key below another never has a greater prefix.
    /// Under a LOCALE-BASED sequence (§8.8.4.2.11, <i>"not necessarily performed character-by-character"</i>) true only for
    /// a prefix as long as the key itself: GR17 c) then truncates nothing, so <see cref="ComparePrefix"/> IS
    /// <see cref="Compare"/>. A shorter one is not: under the CLDR/UCA engine "áa" is below "ab" (primary a,a &lt; a,b)
    /// while its one-character prefix "á" is above "a" (secondary), so a binary search would land past a qualifying
    /// entry.</summary>
    bool PrefixNarrows(int prefixLength);

    /// <summary>The order of two whole stored keys: negative, zero or positive.</summary>
    int Compare(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right);

    /// <summary>The order of the leading <c>prefix.Length</c> positions of <paramref name="stored"/>'s key part against
    /// <paramref name="prefix"/> (a stored key part shorter than the prefix compares on its whole length).</summary>
    int ComparePrefix(ReadOnlySpan<byte> stored, ReadOnlySpan<byte> prefix);

    /// <summary>The high-water marks one stored key's key part raises its tree's to (<see cref="TreeMarks"/>): its
    /// prefix shape under the order's collating sequence when <see cref="PrefixNarrows"/> can be false, zeros otherwise.
    /// Asked once per inserted key.</summary>
    TreeMarks MarksOf(ReadOnlySpan<byte> key);

    /// <summary>The PRIMARY-LEVEL BOUND B of a partial key <paramref name="prefix"/> in a tree whose marks are
    /// <paramref name="marks"/>: the prefix's first level-1 weights, at most <see cref="TreeMarks.Margin"/> of them, which
    /// every stored key's truncation to the prefix's length (§14.9.41.4 GR17 c) and d)) shares with the stored key's own
    /// leading weights — so an entry whose leading weights are below B has a truncation below the prefix and one above
    /// B a truncation above it. Empty (no bound) when the marks leave no margin, and for an order whose prefixes
    /// narrow.</summary>
    int[] BoundOf(ReadOnlySpan<byte> prefix, TreeMarks marks);

    /// <summary>The leading level-1 weights of <paramref name="stored"/>'s key part, as many as <paramref name="bound"/>
    /// holds, against it: negative, zero or positive, monotone in <see cref="Compare"/>'s order; 0 against an empty
    /// bound.</summary>
    int CompareBound(ReadOnlySpan<byte> stored, int[] bound);
}
