// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;

namespace CobolNet.Runtime.IO.Store;

/// <summary>The relation of a <see cref="BTree.Seek"/>: START's relational operators (§14.9.41.4 GR17 c) to e)) and READ's
/// key match.</summary>
internal enum SeekRelation
{
    /// <summary>The first entry whose key (or key prefix) equals the probe.</summary>
    Equal,

    /// <summary>The first entry whose key is above the probe.</summary>
    GreaterThan,

    /// <summary>The first entry whose key is at or above the probe (NOT LESS, GREATER OR EQUAL).</summary>
    GreaterOrEqual,

    /// <summary>The last entry whose key is below the probe (the file "searched in reverse order", GR17 d)).</summary>
    LessThan,

    /// <summary>The last entry whose key is at or below the probe (NOT GREATER, LESS OR EQUAL).</summary>
    LessOrEqual,
}

/// <summary>Where a tree's changed pages go: a statement's reservation (2.2.1.6 R2), which stamps each page with the
/// statement's generation, and the pages the tree stops referencing (released or freed by the ledger's rule).</summary>
internal interface IPageSink
{
    /// <summary>Write a new page; its POINTER, or <see cref="PagePointer.None"/> once the medium has refused a write.</summary>
    PagePointer Write(PageKind kind, ReadOnlySpan<byte> body, byte flags, ushort entries, uint next);

    /// <summary>A page the tree no longer references.</summary>
    void Release(PagePointer page);
}

/// <summary>⛔ THE B+ TREE OF THE KEYED STORE ENGINE (docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.3 decision 3,
/// 2.2.1.4; kb/Work PB3332): variable-length keys and values over <see cref="PagedFile"/>'s pages, ordered by one
/// <see cref="IKeyOrder"/>. Every tree of the format is an instance — the prime and alternate trees of an INDEXED file,
/// the record-number tree of a RELATIVE file, the FREE tree and the LEDGER tree (2.2.1.6 R2), and the TRANSACTIONS,
/// UNDO and HOLDINGS trees of 2.2.1.10.
/// <para><b>PATH COPYING, ONE WRITE PER PAGE PER STATEMENT (2.2.1.6 R1, R3).</b> A change never touches a page: it copies
/// the nodes on its path in memory (the edit), and <see cref="Flush"/> writes each changed node ONCE, children before
/// parents, to pages of the statement's reservation, then hands the statement every page the tree stopped referencing.
/// So no page either slot references is ever written, and no (page, generation) pair is written twice however many
/// changes a statement makes to one tree. Because every change is in memory until the flush, the pages a statement will
/// write (<see cref="PagesToWrite"/>) are known BEFORE it begins, and its reservation is sized exactly.</para>
/// <para><b>STAMPED POINTERS (R3).</b> Every child reference, root and overflow link is a <see cref="PagePointer"/>; every
/// read verifies the page against its stamp, and a page that is torn, stale or misplaced answers '30' (§9.1.13.6 item 1)
/// through <see cref="StoreStatus.PermanentError"/>. A failed read or write DISCARDS the pending edit: the tree is then as
/// its last flush left it.</para>
/// <para><b>No sibling pointers.</b> A leaf's <c>next</c> is 0: a rewritten leaf's left neighbour would still name the
/// superseded page, and re-pointing it would copy every leaf to its left (R1 forbids the fix in place). A cursor crosses
/// leaves through its root-to-leaf path instead (<see cref="Cursor"/>).</para>
/// <para><b>Sizes.</b> A key is at most <see cref="MaxKeyBytes"/>; a value is inline when it is at most
/// <see cref="MaxInlineValueBytes"/> and its whole entry fits half a page body, otherwise it is an OVERFLOW chain
/// (<see cref="OverflowChain"/>) — so any two entries fit one page and a split always yields two pages. A node splits when
/// it passes a page body and is merged with, or borrows from, a sibling when it falls below a quarter of one.</para>
/// <para>A RESIDENT tree (<see cref="LoadResident"/>) holds every node in memory — the LEDGER, read whole at every adoption
/// anyway; any other tree reads the nodes a walk needs and keeps none past a flush.</para></summary>
internal sealed partial class BTree
{
    /// <summary>The longest key a tree holds (2.2.1.4: a longer key is refused at the OPEN with '39').</summary>
    public const int MaxKeyBytes = 1016;

    /// <summary>The longest value kept inline in its leaf (a quarter of a page body); a longer one is an overflow chain.</summary>
    public const int MaxInlineValueBytes = 1016;

    /// <summary>The largest leaf entry: half a page body, so that any two entries share a page.</summary>
    public const int MaxLeafEntryBytes = PageHeader.BodyBytes / 2;

    /// <summary>A node smaller than this (a quarter of a page body) is merged with a sibling or borrows from one.</summary>
    public const int UnderflowBytes = PageHeader.BodyBytes / 4;

    private const int LeafEntryHeaderBytes = 2 + 4;                  // key length, value length
    private const int InternalEntryOverheadBytes = 2 + PagePointer.Bytes;
    private const int OverflowReferenceBytes = PagePointer.Bytes + 4;  // head POINTER, total length
    private const uint OverflowBit = 0x8000_0000;
    private const int MaxDepth = 64;

    private readonly PagedFile _file;
    private readonly IKeyOrder _order;
    private readonly bool _resident;
    private readonly List<PagePointer> _superseded = [];
    private Child _root;
    private Child _flushed;

    /// <summary>A tree whose root is <paramref name="root"/> (<see cref="PagePointer.None"/>: empty), read page by page.</summary>
    public BTree(PagedFile file, IKeyOrder order, PagePointer root) : this(file, order, new Child(root, null), resident: false)
    {
    }

    private BTree(PagedFile file, IKeyOrder order, Child root, bool resident)
    {
        _file = file;
        _order = order;
        _resident = resident;
        _root = _flushed = root;
    }

    /// <summary>An empty RESIDENT tree.</summary>
    public static BTree EmptyResident(PagedFile file, IKeyOrder order) => new(file, order, default, resident: true);

    /// <summary>Read the whole tree at <paramref name="root"/> into memory, every page verified through its stamp.</summary>
    public static StoreStatus LoadResident(PagedFile file, IKeyOrder order, PagePointer root, out BTree? tree)
    {
        var loaded = new BTree(file, order, default, resident: true);
        try
        {
            loaded._root = loaded._flushed = root.IsNone ? default : loaded.LoadAll(root, 0);
            tree = loaded;
            return StoreStatus.Ok;
        }
        catch (TornPage)
        {
            tree = null;
            return StoreStatus.PermanentError;
        }
    }

    /// <summary>A second handle on this RESIDENT tree as its last flush left it, whose edits leave this one untouched
    /// (the nodes are shared and never changed in place).</summary>
    public BTree Fork()
    {
        if (!_resident) throw new InvalidOperationException("only a resident tree forks");
        return new BTree(_file, _order, _flushed, resident: true);
    }

    /// <summary>The root as the last flush left it.</summary>
    public PagePointer Root => _flushed.Pointer;

    /// <summary>Whether an edit is pending.</summary>
    public bool HasPendingChanges => _root != _flushed;

    /// <summary>The pages the pending edit stopped referencing, in the order it stopped (tree pages and overflow pages).</summary>
    public IReadOnlyList<PagePointer> Superseded => _superseded;

    /// <summary>Whether a shrunken node is merged with or borrows from a sibling. The LEDGER's own bookkeeping turns it off
    /// while it settles the pages it writes itself (<see cref="Ledger"/>), so its page count only grows.</summary>
    internal bool Rebalancing { get; set; } = true;

    /// <summary>The pages <see cref="Flush"/> will write: every changed node and the overflow chains of new values.</summary>
    public int PagesToWrite => Count(_root);

    /// <summary>The pages of a RESIDENT tree, as its last flush left it.</summary>
    public List<PagePointer> ResidentPages()
    {
        if (!_resident) throw new InvalidOperationException("only a resident tree lists its pages");
        var pages = new List<PagePointer>();
        CollectPages(_flushed, pages);
        return pages;
    }

    /// <summary>The entries of a RESIDENT tree in key order, as its last flush left it; an overflow value (which a
    /// resident tree's short values never are) is <c>null</c>.</summary>
    public IEnumerable<(byte[] Key, byte[]? Value)> ResidentEntries()
    {
        if (!_resident) throw new InvalidOperationException("only a resident tree enumerates in memory");
        return Entries(_flushed);
    }

    // ── Reads ───────────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The value under <paramref name="key"/> (the pending edit's view); <c>null</c> when there is none.</summary>
    public StoreStatus TryGet(ReadOnlySpan<byte> key, out byte[]? value)
    {
        value = null;
        try
        {
            if (IsEmpty(_root)) return StoreStatus.Ok;
            var path = PathTo(key, out bool found);
            if (found) value = ValueOf(path[^1].Node.Slots[path[^1].Index]);
            return StoreStatus.Ok;
        }
        catch (TornPage)
        {
            return StoreStatus.PermanentError;
        }
    }

    // ── Changes (in memory until Flush) ─────────────────────────────────────────────────────────────────────────

    private enum ChangeKind { Insert, Put, Replace, Delete }

    /// <summary>Insert <paramref name="key"/> → <paramref name="value"/> when the key is absent
    /// (<paramref name="inserted"/> false when it is present: nothing changes).</summary>
    public StoreStatus Insert(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value, out bool inserted) =>
        Change(key, value, ChangeKind.Insert, out inserted);

    /// <summary>Insert or replace <paramref name="key"/> → <paramref name="value"/>.</summary>
    public StoreStatus Put(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value) => Change(key, value, ChangeKind.Put, out _);

    /// <summary>Replace the value of a present <paramref name="key"/> (<paramref name="replaced"/> false when absent).</summary>
    public StoreStatus Replace(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value, out bool replaced) =>
        Change(key, value, ChangeKind.Replace, out replaced);

    /// <summary>Delete <paramref name="key"/> (<paramref name="deleted"/> false when absent).</summary>
    public StoreStatus Delete(ReadOnlySpan<byte> key, out bool deleted) => Change(key, [], ChangeKind.Delete, out deleted);

    private StoreStatus Change(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value, ChangeKind kind, out bool done)
    {
        done = false;
        if (key.Length > MaxKeyBytes)
            throw new ArgumentException($"a key holds at most {MaxKeyBytes} bytes, not {key.Length}; the OPEN refuses the declaration first", nameof(key));
        var slot = kind == ChangeKind.Delete ? default : MakeSlot(key, value);
        try
        {
            if (IsEmpty(_root))
            {
                if (kind is ChangeKind.Replace or ChangeKind.Delete) return StoreStatus.Ok;
                var first = new Node(leaf: true);
                first.Keys.Add(key.ToArray());
                first.Slots.Add(slot);
                _root = new Child(PagePointer.None, first);
                done = true;
                return StoreStatus.Ok;
            }
            var path = PathTo(key, out bool found);
            if (found ? kind == ChangeKind.Insert : kind is ChangeKind.Replace or ChangeKind.Delete) return StoreStatus.Ok;
            var (original, at) = path[^1];
            if (found) Supersede(original.Slots[at]);
            var before = path.ConvertAll(p => p.Node.Bytes);
            // An APPEND — a new key above every key of the tree — splits off only what it added, so a sequential load
            // fills its pages (Berkeley DB's rule for a split at the right edge; the 2.2.1.4 fan-out arithmetic).
            bool append = !found && at == original.Keys.Count
                && path.Take(path.Count - 1).All(p => p.Index == p.Node.Children.Count - 1);
            var leaf = Own(original);
            if (kind == ChangeKind.Delete)
            {
                leaf.Keys.RemoveAt(at);
                leaf.Slots.RemoveAt(at);
            }
            else if (found) leaf.Slots[at] = slot;
            else
            {
                leaf.Keys.Insert(at, key.ToArray());
                leaf.Slots.Insert(at, slot);
            }
            Rebuild(path, leaf, before, append);
            done = true;
            return StoreStatus.Ok;
        }
        catch (TornPage)
        {
            Discard();
            done = false;
            return StoreStatus.PermanentError;
        }
    }

    /// <summary>Write the pending edit through <paramref name="sink"/> — overflow chains, then each changed node once,
    /// children before parents — and release every page it superseded; the new root. A refused write discards the edit
    /// and answers '30' (the statement is refused; nothing it wrote is reachable, R1).</summary>
    public StoreStatus Flush(IPageSink sink, out PagePointer root)
    {
        if (!HasPendingChanges)
        {
            root = _root.Pointer;
            return StoreStatus.Ok;
        }
        bool refused = false;
        var written = IsEmpty(_root) ? default : FlushChild(_root, sink, ref refused);
        if (refused)
        {
            Discard();
            root = PagePointer.None;
            return StoreStatus.PermanentError;
        }
        foreach (var page in _superseded) sink.Release(page);
        _superseded.Clear();
        _root = _flushed = _resident ? written : new Child(written.Pointer, null);
        root = written.Pointer;
        return StoreStatus.Ok;
    }

    /// <summary>Drop the pending edit: the tree is as its last flush left it.</summary>
    public void Discard()
    {
        _root = _flushed;
        _superseded.Clear();
    }

    /// <summary>Take <paramref name="root"/> — the root the store adopted at a coherent entry (2.2.1.7: another run unit's
    /// statement moved page 2) — as this tree's. A cursor positioned under the old root re-seeks from its key at its
    /// next step, so it never walks the superseded path, whose pages that run unit may already have reused.</summary>
    public void Adopt(PagePointer root)
    {
        if (_resident) throw new InvalidOperationException("a resident tree is reloaded, not re-rooted");
        if (HasPendingChanges) throw new InvalidOperationException("a tree with a pending edit adopts nothing");
        if (root != _flushed.Pointer) _root = _flushed = new Child(root, null);
    }

    // ── The edit's shape: copy the path, fix every level, set the root ──────────────────────────────────────────

    /// <summary>The path to <paramref name="key"/>: every internal node with the child taken, then the leaf with the
    /// key's position (its lower bound) and whether it is there.</summary>
    private List<(Node Node, int Index)> PathTo(ReadOnlySpan<byte> key, out bool found)
    {
        var path = new List<(Node Node, int Index)>();
        var node = Load(_root);
        while (!node.IsLeaf)
        {
            if (path.Count > MaxDepth) throw new TornPage(PageCheck.ChecksumFails);
            int child = Bound(node.Keys, key, orEqual: true, prefix: false);
            path.Add((node, child));
            node = Load(node.Children[child]);
        }
        int at = Bound(node.Keys, key, orEqual: false, prefix: false);
        found = at < node.Keys.Count && _order.Compare(node.Keys[at], key) == 0;
        path.Add((node, at));
        return path;
    }

    /// <summary>The number of <paramref name="keys"/> below <paramref name="probe"/> — or at or below it when
    /// <paramref name="orEqual"/> — under the order or (with <paramref name="prefix"/>) its prefix comparison: a lower or
    /// upper bound, by binary search.</summary>
    private int Bound(List<byte[]> keys, ReadOnlySpan<byte> probe, bool orEqual, bool prefix)
    {
        int lo = 0, hi = keys.Count;
        while (lo < hi)
        {
            int mid = (lo + hi) >>> 1;
            int c = prefix ? _order.ComparePrefix(keys[mid], probe) : _order.Compare(keys[mid], probe);
            if (c < 0 || (orEqual && c == 0)) lo = mid + 1;
            else hi = mid;
        }
        return lo;
    }

    /// <summary>The node <paramref name="node"/> as this edit may change it: itself when this edit already copied it,
    /// otherwise a copy, its page superseded.</summary>
    private Node Own(Node node)
    {
        if (node.Dirty) return node;
        _superseded.Add(node.Page);
        return node.Clone();
    }

    /// <summary>Re-link the changed <paramref name="leaf"/> up <paramref name="path"/>: each ancestor copied, a node past
    /// a page body split into its parent, a node that SHRANK below a quarter of one (or emptied) rebalanced with a
    /// sibling, and the root set. <paramref name="before"/> holds each path node's size before the change: a node that
    /// grew is never rebalanced, so an append's small right page is left to fill.</summary>
    private void Rebuild(List<(Node Node, int Index)> path, Node leaf, List<int> before, bool append)
    {
        var child = leaf;
        for (int level = path.Count - 2; level >= 0; level--)
        {
            var (original, index) = path[level];
            var parent = Own(original);
            parent.Children[index] = new Child(PagePointer.None, child);
            FixChild(parent, index, before[level + 1], append);
            child = parent;
        }
        SetRoot(child, append);
    }

    private void FixChild(Node parent, int index, int sizeBefore, bool append)
    {
        var child = parent.Children[index].Node!;
        int size = child.Bytes;
        if (size > PageHeader.BodyBytes)
        {
            var (separator, right) = Split(child, append);
            parent.Keys.Insert(index, separator);
            parent.Children.Insert(index + 1, new Child(PagePointer.None, right));
        }
        else if (Rebalancing && parent.Children.Count > 1 && (child.Keys.Count == 0 || (size < sizeBefore && size < UnderflowBytes)))
            Rebalance(parent, index);
    }

    /// <summary>Merge the child at <paramref name="index"/> with a sibling when both fit one page, otherwise share their
    /// entries between two (a borrow); the parent's separator follows.</summary>
    private void Rebalance(Node parent, int index)
    {
        int l = index + 1 < parent.Children.Count ? index : index - 1, r = l + 1;
        var left = Own(Load(parent.Children[l]));
        var right = Own(Load(parent.Children[r]));
        if (left.IsLeaf)
        {
            left.Keys.AddRange(right.Keys);
            left.Slots.AddRange(right.Slots);
        }
        else
        {
            left.Keys.Add(parent.Keys[l]);
            left.Keys.AddRange(right.Keys);
            left.Children.AddRange(right.Children);
        }
        parent.Children[l] = new Child(PagePointer.None, left);
        if (left.Bytes <= PageHeader.BodyBytes)
        {
            parent.Keys.RemoveAt(l);
            parent.Children.RemoveAt(r);
            return;
        }
        var (separator, newRight) = Split(left, append: false);
        parent.Keys[l] = separator;
        parent.Children[r] = new Child(PagePointer.None, newRight);
    }

    /// <summary>Split <paramref name="node"/>, which keeps the left part: where the larger half is smallest, or — for an
    /// <paramref name="append"/> at the right edge — just past what fitted before the append (a leaf's last entry, an
    /// internal node's last key with its two children). A leaf's separator is the right part's first key; an internal
    /// node promotes the key between the parts.</summary>
    private static (byte[] Separator, Node Right) Split(Node node, bool append)
    {
        int n = node.Keys.Count;
        var right = new Node(node.IsLeaf);
        if (node.IsLeaf)
        {
            int total = node.Bytes, left = 0, best = 1, bestMax = int.MaxValue;
            for (int m = 1; m < n; m++)
            {
                left += LeafEntryBytes(node.Keys[m - 1], node.Slots[m - 1]);
                int larger = Math.Max(left, total - left);
                if (larger < bestMax) (best, bestMax) = (m, larger);
            }
            if (append && total - LeafEntryBytes(node.Keys[n - 1], node.Slots[n - 1]) <= PageHeader.BodyBytes) best = n - 1;
            right.Keys.AddRange(node.Keys.GetRange(best, n - best));
            right.Slots.AddRange(node.Slots.GetRange(best, n - best));
            node.Keys.RemoveRange(best, n - best);
            node.Slots.RemoveRange(best, n - best);
            return (right.Keys[0], right);
        }
        int all = node.Bytes - PagePointer.Bytes, before = 0, mid = 1, midMax = int.MaxValue;
        for (int m = 1; m < n - 1; m++)
        {
            before += InternalEntryOverheadBytes + node.Keys[m - 1].Length;
            int after = all - before - InternalEntryOverheadBytes - node.Keys[m].Length;
            int larger = Math.Max(before, after);
            if (larger < midMax) (mid, midMax) = (m, larger);
        }
        if (append && n >= 3) mid = n - 2;
        var separator = node.Keys[mid];
        right.Keys.AddRange(node.Keys.GetRange(mid + 1, n - mid - 1));
        right.Children.AddRange(node.Children.GetRange(mid + 1, n - mid));
        node.Keys.RemoveRange(mid, n - mid);
        node.Children.RemoveRange(mid + 1, n - mid);
        return (separator, right);
    }

    /// <summary>Make <paramref name="top"/> the root: a new root above it when it split, its only child when it is an
    /// internal node left without a key, and an empty tree when it is a leaf left without an entry (kept as an empty
    /// leaf while rebalancing is off, so the page count of the ledger's settling only grows).</summary>
    private void SetRoot(Node top, bool append)
    {
        while (true)
        {
            if (top.Bytes > PageHeader.BodyBytes)
            {
                var (separator, right) = Split(top, append);
                var root = new Node(leaf: false);
                root.Children.Add(new Child(PagePointer.None, top));
                root.Keys.Add(separator);
                root.Children.Add(new Child(PagePointer.None, right));
                top = root;
                continue;
            }
            if (!top.IsLeaf && top.Keys.Count == 0)
            {
                var only = top.Children[0];
                if (only.Node is { Dirty: true } changed)
                {
                    top = changed;
                    continue;
                }
                _root = only;
                return;
            }
            _root = top.IsLeaf && top.Keys.Count == 0 && Rebalancing ? default : new Child(PagePointer.None, top);
            return;
        }
    }

    // ── Values and overflow chains ──────────────────────────────────────────────────────────────────────────────

    private static Slot MakeSlot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value)
    {
        if (value.Length <= MaxInlineValueBytes && LeafEntryHeaderBytes + key.Length + value.Length <= MaxLeafEntryBytes)
            return new Slot(value.ToArray(), PagePointer.None, (uint)value.Length, null);
        return new Slot(null, PagePointer.None, (uint)value.Length, value.ToArray());
    }

    private byte[] ValueOf(Slot slot)
    {
        if ((slot.Inline ?? slot.Pending) is { } ready) return ready;
        var check = OverflowChain.Read(slot.Head, slot.Total, ReadPage, out var value);
        return check == PageCheck.Whole ? value : throw new TornPage(check);
    }

    /// <summary>An overflow value the edit replaces or deletes: its chain's pages are superseded (read to be found).</summary>
    private void Supersede(Slot slot)
    {
        if (slot.Head.IsNone) return;
        var pages = new List<PagePointer>();
        var check = OverflowChain.Pages(slot.Head, slot.Total, ReadPage, pages);
        if (check != PageCheck.Whole) throw new TornPage(check);
        _superseded.AddRange(pages);
    }

    // ── Loading, writing and counting nodes ─────────────────────────────────────────────────────────────────────

    private PageCheck ReadPage(PagePointer pointer, byte[] page) => _file.Read(pointer, page);

    private static bool IsEmpty(Child c) => c.Node is null && c.Pointer.IsNone;

    private Node Load(Child c) => c.Node ?? Fetch(c.Pointer);

    private Node Fetch(PagePointer pointer)
    {
        var page = new byte[PageHeader.PageSize];
        var check = _file.Read(pointer, page);
        if (check != PageCheck.Whole) throw new TornPage(check);
        var node = DecodeNode(page) ?? throw new TornPage(PageCheck.ChecksumFails);
        node.Page = pointer;
        return node;
    }

    private Child LoadAll(PagePointer pointer, int depth)
    {
        if (depth > MaxDepth) throw new TornPage(PageCheck.ChecksumFails);
        var node = Fetch(pointer);
        if (!node.IsLeaf)
            for (int i = 0; i < node.Children.Count; i++)
                node.Children[i] = LoadAll(node.Children[i].Pointer, depth + 1);
        return new Child(pointer, node);
    }

    private Child FlushChild(Child c, IPageSink sink, ref bool refused)
    {
        if (c.Node is not { Dirty: true } node) return c;
        if (node.IsLeaf)
        {
            for (int i = 0; i < node.Slots.Count; i++)
                if (node.Slots[i].Pending is { } value)
                {
                    var head = refused ? PagePointer.None : OverflowChain.Write(sink, value);
                    refused |= head.IsNone;
                    node.Slots[i] = new Slot(null, head, (uint)value.Length, null);
                }
        }
        else
            for (int i = 0; i < node.Children.Count; i++)
                node.Children[i] = FlushChild(node.Children[i], sink, ref refused);
        var page = refused ? PagePointer.None
            : sink.Write(node.IsLeaf ? PageKind.Leaf : PageKind.Internal, EncodeNode(node), 0, (ushort)node.Keys.Count, 0);
        refused |= page.IsNone;
        node.Page = page;
        return new Child(page, _resident ? node : null);
    }

    private static int Count(Child c)
    {
        if (c.Node is not { Dirty: true } node) return 0;
        int pages = 1;
        if (node.IsLeaf)
        {
            foreach (var slot in node.Slots)
                if (slot.Pending is { } value) pages += OverflowChain.PagesFor(value.Length);
        }
        else
            foreach (var child in node.Children) pages += Count(child);
        return pages;
    }

    private static void CollectPages(Child c, List<PagePointer> into)
    {
        if (c.Node is not { } node) return;
        into.Add(c.Pointer);
        if (!node.IsLeaf)
            foreach (var child in node.Children) CollectPages(child, into);
    }

    private static IEnumerable<(byte[] Key, byte[]? Value)> Entries(Child c)
    {
        if (c.Node is not { } node) yield break;
        if (node.IsLeaf)
        {
            for (int i = 0; i < node.Keys.Count; i++) yield return (node.Keys[i], node.Slots[i].Inline);
            yield break;
        }
        foreach (var child in node.Children)
            foreach (var entry in Entries(child)) yield return entry;
    }

    private static int LeafEntryBytes(byte[] key, Slot slot) => LeafEntryHeaderBytes + key.Length + slot.StoredBytes;

    // ── The page codec (2.2.1.4's INTERNAL and LEAF bodies) ─────────────────────────────────────────────────────

    /// <summary>INTERNAL: the leftmost child POINTER, then per key {key length (2), key, child POINTER}. LEAF: per entry
    /// {key length (2), value length (4), key, value}; a value length with bit 31 set is an OVERFLOW REFERENCE of
    /// <see cref="OverflowReferenceBytes"/> bytes: the chain's head POINTER and the value's total length.</summary>
    private static byte[] EncodeNode(Node node)
    {
        var body = new byte[PageHeader.BodyBytes];
        var span = body.AsSpan();
        int at = 0;
        if (!node.IsLeaf)
        {
            node.Children[0].Pointer.Write(span);
            at = PagePointer.Bytes;
        }
        for (int i = 0; i < node.Keys.Count; i++)
        {
            var key = node.Keys[i];
            BinaryPrimitives.WriteUInt16LittleEndian(span[at..], (ushort)key.Length);
            at += 2;
            if (node.IsLeaf)
            {
                var slot = node.Slots[i];
                BinaryPrimitives.WriteUInt32LittleEndian(span[at..],
                    slot.Inline is { } inline ? (uint)inline.Length : OverflowBit | OverflowReferenceBytes);
                at += 4;
                key.CopyTo(span[at..]);
                at += key.Length;
                if (slot.Inline is { } bytes) bytes.CopyTo(span[at..]);
                else
                {
                    slot.Head.Write(span[at..]);
                    BinaryPrimitives.WriteUInt32LittleEndian(span[(at + PagePointer.Bytes)..], slot.Total);
                }
                at += slot.StoredBytes;
            }
            else
            {
                key.CopyTo(span[at..]);
                at += key.Length;
                node.Children[i + 1].Pointer.Write(span[at..]);
                at += PagePointer.Bytes;
            }
        }
        return body;
    }

    /// <summary>The inverse of <see cref="EncodeNode"/> over a page that verified (2.2.1.6 R3); <c>null</c> for a body
    /// that is not a node of its kind (a page whose checksum holds over content no encoder wrote: damage).</summary>
    private static Node? DecodeNode(ReadOnlySpan<byte> page)
    {
        var header = PageHeader.Decode(page);
        if (header.Kind is not (PageKind.Leaf or PageKind.Internal)) return null;
        var body = page[PageHeader.Bytes..];
        var node = new Node(header.Kind == PageKind.Leaf);
        int at = 0;
        if (!node.IsLeaf)
        {
            node.Children.Add(new Child(PagePointer.Read(body), null));
            at = PagePointer.Bytes;
        }
        for (int i = 0; i < header.Entries; i++)
        {
            if (at + LeafEntryHeaderBytes > body.Length) return null;
            int keyLength = BinaryPrimitives.ReadUInt16LittleEndian(body[at..]);
            at += 2;
            if (keyLength > MaxKeyBytes) return null;
            if (node.IsLeaf)
            {
                uint word = BinaryPrimitives.ReadUInt32LittleEndian(body[at..]);
                at += 4;
                bool overflow = (word & OverflowBit) != 0;
                int stored = (int)(word & ~OverflowBit);
                if ((overflow && stored != OverflowReferenceBytes) || at + keyLength + stored > body.Length) return null;
                node.Keys.Add(body.Slice(at, keyLength).ToArray());
                at += keyLength;
                node.Slots.Add(overflow
                    ? new Slot(null, PagePointer.Read(body[at..]), BinaryPrimitives.ReadUInt32LittleEndian(body[(at + PagePointer.Bytes)..]), null)
                    : new Slot(body.Slice(at, stored).ToArray(), PagePointer.None, (uint)stored, null));
                at += stored;
            }
            else
            {
                if (at + keyLength + PagePointer.Bytes > body.Length) return null;
                node.Keys.Add(body.Slice(at, keyLength).ToArray());
                at += keyLength;
                node.Children.Add(new Child(PagePointer.Read(body[at..]), null));
                at += PagePointer.Bytes;
            }
        }
        return node;
    }

    // ── The in-memory shapes ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A reference to a child: its page, and the node itself when it is in memory (a resident tree's, or one
    /// this edit changed — then the page is <see cref="PagePointer.None"/> until the flush).</summary>
    private readonly record struct Child(PagePointer Pointer, Node? Node);

    /// <summary>A leaf's value: inline bytes, an overflow chain on the medium (head and total length), or a value this
    /// edit wrote that the flush will put in a chain (<see cref="Pending"/>).</summary>
    private readonly record struct Slot(byte[]? Inline, PagePointer Head, uint Total, byte[]? Pending)
    {
        public int StoredBytes => Inline?.Length ?? OverflowReferenceBytes;
    }

    /// <summary>A node. A node read from a page or flushed carries that page and is never changed again (edits copy
    /// it); a node this edit created has no page until the flush writes it.</summary>
    private sealed class Node(bool leaf)
    {
        public bool IsLeaf { get; } = leaf;

        public List<byte[]> Keys { get; } = [];

        public List<Slot> Slots { get; } = [];

        public List<Child> Children { get; } = [];

        public PagePointer Page;

        public bool Dirty => Page.IsNone;

        public int Bytes
        {
            get
            {
                int bytes = IsLeaf ? 0 : PagePointer.Bytes;
                for (int i = 0; i < Keys.Count; i++)
                    bytes += IsLeaf ? LeafEntryBytes(Keys[i], Slots[i]) : InternalEntryOverheadBytes + Keys[i].Length;
                return bytes;
            }
        }

        public Node Clone()
        {
            var copy = new Node(IsLeaf);
            copy.Keys.AddRange(Keys);
            copy.Slots.AddRange(Slots);
            copy.Children.AddRange(Children);
            return copy;
        }
    }

    /// <summary>A page read through a stamped pointer did not verify; caught at the tree's surface and answered '30'.</summary>
    private sealed class TornPage(PageCheck check) : Exception($"a page read through a stamped pointer was {check}");
}
