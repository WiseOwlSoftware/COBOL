// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;

namespace CobolNet.Runtime.IO.Store;

/// <summary>The state of a page the LEDGER lists (docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.6 R2).</summary>
internal enum LedgerState : byte
{
    /// <summary>Allocated since the committed generation and referenced by the work tree.</summary>
    Fresh = 1,

    /// <summary>Allocated since the committed generation and no longer referenced: reusable by any LATER statement
    /// than the one that released it.</summary>
    Released = 2,

    /// <summary>A committed page released since the committed generation: QUARANTINED until the promotion that makes
    /// its freeing durable (R4), because the committed slot still references it.</summary>
    Freed = 3,
}

/// <summary>One page's ledger entry: the generation of its last write and its state.</summary>
internal readonly record struct LedgerEntry(ulong Generation, LedgerState State);

/// <summary>⛔ THE GENERATION LEDGER (2.2.1.6 R2): every page allocated since the committed generation (the work meta's
/// base), with the generation of its last write and its <see cref="LedgerState"/>, plus the committed pages released
/// since (<see cref="LedgerState.Freed"/>) — held as a RESIDENT <see cref="BTree"/> of the engine (page number, 4 bytes
/// big-endian → generation (8) and state (1)), path-copied by every statement like every tree, its root in the work meta.
/// <para><b>It does not list its own pages</b>: they are verified through their stamped pointers when it is read. So the
/// pages its own write takes and supersedes change what it holds, and <see cref="Write"/> SETTLES that before it writes a
/// page: the pages it supersedes are <c>released</c> (they were allocated since the base), a reserved page it takes for
/// itself leaves the list, and a reserved FREE page nobody took is <c>released</c>, never lost — repeated until nothing
/// moves, with the tree's rebalancing off so that its page count only grows and the settling ends (kb/Work PB3332; the
/// interim chain of PB3331 rewrote the whole list at every statement).</para></summary>
internal sealed class Ledger
{
    private const int ValueBytes = sizeof(ulong) + 1;

    private readonly SortedDictionary<uint, LedgerEntry> _entries;
    private readonly BTree _tree;
    private readonly HashSet<uint> _changed;

    /// <summary>An empty ledger over <paramref name="file"/>: a new store's, and the one a retirement starts from.</summary>
    public Ledger(PagedFile file) : this([], BTree.EmptyResident(file, KeyOrders.Ordinal), [])
    {
    }

    private Ledger(SortedDictionary<uint, LedgerEntry> entries, BTree tree, HashSet<uint> changed)
    {
        _entries = entries;
        _tree = tree;
        _changed = changed;
    }

    /// <summary>The entries, by page.</summary>
    public IReadOnlyDictionary<uint, LedgerEntry> Entries => _entries;

    /// <summary>The number of pages listed (R5's checkpoint compares it with <see cref="PagedFile.LedgerCheckpoint"/>).</summary>
    public int Count => _entries.Count;

    /// <summary>The ledger tree's own pages as its last write (or load) left them — referenced by the meta that names its
    /// root, so R1 protects them and, after a promotion, R4 quarantines them.</summary>
    public List<PagePointer> OwnPages => _tree.ResidentPages();

    /// <summary>The pages in state <paramref name="state"/>, ascending.</summary>
    public IEnumerable<uint> Pages(LedgerState state) =>
        _entries.Where(e => e.Value.State == state).Select(e => e.Key);

    /// <summary>Record <paramref name="page"/>.</summary>
    public void Set(uint page, ulong generation, LedgerState state)
    {
        _entries[page] = new(generation, state);
        _changed.Add(page);
    }

    /// <summary>Stop listing <paramref name="page"/>.</summary>
    public void Remove(uint page)
    {
        if (_entries.Remove(page)) _changed.Add(page);
    }

    /// <summary>The entry of <paramref name="page"/>, if listed.</summary>
    public bool TryGet(uint page, out LedgerEntry entry) => _entries.TryGetValue(page, out entry);

    /// <summary>A copy a statement works on, so a refused statement leaves the store's ledger as it was (the tree's
    /// nodes are shared and copied on change).</summary>
    public Ledger Clone() => new(new SortedDictionary<uint, LedgerEntry>(_entries), _tree.Fork(), []);

    /// <summary>An upper bound of the pages <see cref="Write"/> takes when the statement reserves
    /// <paramref name="reservedPages"/> pages (each a ledger change at most twice: written or left over, and its
    /// settling) and stops referencing <paramref name="releases"/> pages (each one change: <c>released</c> or
    /// <c>freed</c>; finding R1-1 — a dropped overflow chain of thousands of pages is thousands of entries): every page of
    /// the tree copied and one split per copied node; for the entries the write adds, one new leaf per hundred (a split
    /// leaf keeps more than a hundred 19-byte entries) and above them one new internal node per hundred children (an
    /// internal entry is at most 18 bytes), level by level, each level able to raise a new root.</summary>
    public int WriteBound(int reservedPages, int releases)
    {
        int own = _tree.ResidentPages().Count;
        int inserts = _changed.Count + 2 * reservedPages + releases + own;
        int bound = 2 * own + 2;
        for (int added = inserts / 100 + 1; added > 0; added /= 100) bound += added + 1;
        return bound;
    }

    /// <summary>Read the ledger tree at <paramref name="root"/>, every page verified through its stamp:
    /// <see cref="PageCheck.Whole"/>; <see cref="PageCheck.Unreadable"/> when the medium REFUSED a read (no verdict on
    /// the page); the failed check of a torn page, and <see cref="PageCheck.ChecksumFails"/> for a body that is not a
    /// ledger's (the OPEN's adoption rule tells a refusal from damage, 2.2.1.6 R6 step 3).</summary>
    public static PageCheck Load(PagedFile file, PagePointer root, out Ledger? ledger)
    {
        ledger = null;
        if (BTree.LoadResident(file, KeyOrders.Ordinal, root, out var tree) is var check && check != PageCheck.Whole) return check;
        var entries = new SortedDictionary<uint, LedgerEntry>();
        foreach (var (key, value) in tree!.ResidentEntries())
        {
            if (key.Length != sizeof(uint) || value is not { Length: ValueBytes }) return PageCheck.ChecksumFails;
            var state = (LedgerState)value[sizeof(ulong)];
            if (state is not (LedgerState.Fresh or LedgerState.Released or LedgerState.Freed)) return PageCheck.ChecksumFails;
            entries[KeyOrders.PageOf(key)] = new LedgerEntry(BinaryPrimitives.ReadUInt64LittleEndian(value), state);
        }
        ledger = new Ledger(entries, tree, []);
        return PageCheck.Whole;
    }

    /// <summary>Write the ledger LAST among statement <paramref name="statement"/>'s pages (R2: nothing is allocated while
    /// it is being written — every page it takes is the next of the reservation), path-copied from the tree the previous
    /// statement wrote, and SETTLED first (the type's remarks); the new root.</summary>
    internal StoreStatus Write(PageStatement statement, out PagePointer root)
    {
        root = PagePointer.None;
        foreach (uint page in _changed)
            Expect(_entries.TryGetValue(page, out var entry) ? _tree.Put(KeyOrders.PageKey(page), Encode(entry))
                : _tree.Delete(KeyOrders.PageKey(page), out _));
        _changed.Clear();
        var remaining = statement.Reservation.Remaining();
        int start = statement.Reservation.Position, taken = 0, superseded = 0;
        _tree.Rebalancing = false;
        try
        {
            bool moved;
            do
            {
                moved = false;
                for (; superseded < _tree.Superseded.Count; superseded++)
                {
                    var page = _tree.Superseded[superseded];
                    Record(page.Page, new LedgerEntry(page.Generation, LedgerState.Released));
                    moved = true;
                }
                for (int need = _tree.PagesToWrite; taken < need; taken++)
                {
                    if (taken == remaining.Count)
                        throw new InvalidOperationException("the ledger needs more pages than its statement reserved");
                    moved |= Forget(remaining[taken].Page);
                }
                for (int i = taken; i < remaining.Count; i++)
                    if (remaining[i].Origin == PageOrigin.Free && !_entries.ContainsKey(remaining[i].Page))
                    {
                        Record(remaining[i].Page, new LedgerEntry(0, LedgerState.Released));
                        moved = true;
                    }
            }
            while (moved);
        }
        finally
        {
            _tree.Rebalancing = true;
        }
        if (_tree.Flush(new OwnPageSink(statement), out root) != StoreStatus.Ok) return StoreStatus.PermanentError;
        if (statement.Reservation.Position != start + taken)
            throw new InvalidOperationException("the ledger wrote another number of pages than it settled on");
        return StoreStatus.Ok;
    }

    private void Record(uint page, LedgerEntry entry)
    {
        _entries[page] = entry;
        Expect(_tree.Put(KeyOrders.PageKey(page), Encode(entry)));
    }

    private bool Forget(uint page)
    {
        if (!_entries.Remove(page)) return false;
        Expect(_tree.Delete(KeyOrders.PageKey(page), out _));
        return true;
    }

    /// <summary>A resident tree reads nothing, so a change to it cannot be refused.</summary>
    private static void Expect(StoreStatus status)
    {
        if (status != StoreStatus.Ok) throw new InvalidOperationException("a resident tree's change was refused");
    }

    private static byte[] Encode(LedgerEntry entry)
    {
        var value = new byte[ValueBytes];
        BinaryPrimitives.WriteUInt64LittleEndian(value, entry.Generation);
        value[sizeof(ulong)] = (byte)entry.State;
        return value;
    }

    /// <summary>The ledger's own pages: written from the reservation and NOT listed (R2); the pages its tree supersedes
    /// were settled as <c>released</c> before the write, so a release here records nothing.</summary>
    private sealed class OwnPageSink(PageStatement statement) : IPageSink
    {
        public PagePointer Write(PageKind kind, ReadOnlySpan<byte> body, byte flags, ushort entries, uint next, TreeMarks marks) =>
            statement.WriteUnlisted(kind, body, flags, entries, next, marks);

        public void Release(PagePointer page)
        {
        }
    }
}
