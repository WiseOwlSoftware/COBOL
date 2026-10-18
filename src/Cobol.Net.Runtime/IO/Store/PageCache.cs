// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO.Store;

/// <summary>⛔ THE PAGE CACHE OF ONE STORE (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.13, kb/Work PB3333): the
/// most recently used pages of the physical file, each as the bytes a stamped read verified or a statement of this store
/// wrote, evicted least recently used first. It is the run unit's one cache for the file — every connector of the run
/// unit reads through the one <see cref="PagedFile"/> — and it is DROPPED whenever the store adopts page 2 from another
/// run unit (2.2.1.7), so it never serves a page another run unit's statement may have superseded.
/// <para>A cached page is still read through its stamped pointer (2.2.1.6 R3): a hit counts only when the cached page's
/// <c>self</c> and <c>generation</c> are the pointer's, so a page reused since it was cached is read from the medium
/// again. The checksum is not recomputed on a hit: the bytes were verified when they entered.</para>
/// <para>Its capacity is §12.4.5.14.3 GR1's floor — <i>"If the RESERVE clause is specified, the number of input-output
/// areas allocated is equal to the value of integer-1"</i> — at least one page per input-output area of the attached
/// connectors, and never below <see cref="DefaultPages"/>, the run unit's default (1 MiB), which is above the engine's
/// working set (the tree's depth plus three) for every tree the format can hold.</para></summary>
internal sealed class PageCache
{
    /// <summary>The run unit's default capacity, in pages: 256 pages of 4 096 bytes, 1 MiB (2.2.1.13).</summary>
    public const int DefaultPages = 256;

    private readonly Dictionary<uint, LinkedListNode<(uint Page, byte[] Bytes)>> _byPage = [];
    private readonly LinkedList<(uint Page, byte[] Bytes)> _recency = new();

    /// <summary>A cache of <paramref name="capacity"/> pages (at least <see cref="DefaultPages"/>).</summary>
    public PageCache(int capacity = DefaultPages) => Capacity = Math.Max(DefaultPages, capacity);

    /// <summary>The pages the cache holds at most.</summary>
    public int Capacity { get; private set; }

    /// <summary>The pages the cache holds now.</summary>
    public int Count => _byPage.Count;

    /// <summary>Raise the capacity to at least <paramref name="pages"/> (a connector attached with a larger RESERVE).</summary>
    public void Reserve(int pages) => Capacity = Math.Max(Capacity, pages);

    /// <summary>The cached bytes of <paramref name="page"/>, made the most recently used; null when not cached.</summary>
    public byte[]? Get(uint page)
    {
        if (!_byPage.TryGetValue(page, out var node)) return null;
        _recency.Remove(node);
        _recency.AddFirst(node);
        return node.Value.Bytes;
    }

    /// <summary>Hold <paramref name="bytes"/> (owned by the cache from now on) as page <paramref name="page"/>.</summary>
    public void Put(uint page, byte[] bytes)
    {
        if (_byPage.TryGetValue(page, out var node)) _recency.Remove(node);
        node = _recency.AddFirst((page, bytes));
        _byPage[page] = node;
        while (_byPage.Count > Capacity)
        {
            var oldest = _recency.Last!;
            _recency.RemoveLast();
            _byPage.Remove(oldest.Value.Page);
        }
    }

    /// <summary>Forget every page (another run unit's statement was adopted, or a witness asks for a cold cache).</summary>
    public void Clear()
    {
        _byPage.Clear();
        _recency.Clear();
    }
}
