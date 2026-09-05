// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// ⭐ The per-PHYSICAL-FILE record stores (kb/Work PB143; ISO §14.9.10.4 GR5 — "the identified record has been
/// logically removed from THE PHYSICAL FILE and can no longer be accessed"). Every keyed connector used to load
/// a PRIVATE snapshot at OPEN and persist its WHOLE view at CLOSE, so a record DELETEd through one connector
/// stayed readable through another over the same host path, and the CLOSE ORDER decided which view survived on
/// disk — silent undeletion / silent data loss. Now the record images live HERE, keyed by resolved host path
/// (the same key the <see cref="PhysicalFileTable"/> arbitrates sharing and locks by): the FIRST opener loads
/// from disk, later openers ATTACH to the live store (never reload — the in-memory store IS the truth while any
/// connector holds it), every mutation is instantly visible to every attached connector, any CLOSE persists the
/// ONE shared state (order no longer matters), and the LAST detach drops the entry so a later OPEN re-reads the
/// disk. Position/key state (FPI, key of reference, sequential-WRITE slot, GR38 high-key) stays per-CONNECTOR.
/// Two SELECTs to one ASSIGN target need no SHARING clause to reach this, so the store is unconditional for the
/// keyed organizations; sequential connectors keep their OS-backed streams (the file system is their shared
/// store). Owned by the <see cref="FileRegistry"/>; cleared at run-unit Reset.
/// </summary>
internal sealed class KeyedStoreTable
{
    private sealed class Entry
    {
        public required object Store;
        public int Attached;
    }

    private readonly Dictionary<string, Entry> _byHost = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Attach to the RELATIVE store for <paramref name="host"/>: the live store when one exists (its
    /// content is the truth — no reload), else a fresh store populated by <paramref name="loadFirst"/>.</summary>
    public RelativeStore AttachRelative(string host, Action<RelativeStore> loadFirst)
        => Attach(host, () => { var s = new RelativeStore(); loadFirst(s); return s; });

    /// <summary>Attach to the INDEXED store for <paramref name="host"/> — same contract.</summary>
    public IndexedStore AttachIndexed(string host, Action<IndexedStore> loadFirst)
        => Attach(host, () => { var s = new IndexedStore(); loadFirst(s); return s; });

    private T Attach<T>(string host, Func<T> create) where T : class
    {
        if (_byHost.TryGetValue(host, out var e))
        {
            if (e.Store is not T live)
                throw new InvalidOperationException(
                    $"physical file '{host}' is open under two different organizations ({e.Store.GetType().Name} vs {typeof(T).Name}) — a compiler/registration defect (kb/Work PB143)");
            e.Attached++;
            return live;
        }
        var store = create();
        _byHost[host] = new Entry { Store = store, Attached = 1 };
        return store;
    }

    /// <summary>Detach one connector from <paramref name="host"/>'s store; the LAST detach drops the entry so a
    /// later OPEN reloads from disk. Unbalanced detaches are ignored (a failed OPEN never attached).</summary>
    public void Detach(string host)
    {
        if (!_byHost.TryGetValue(host, out var e)) return;
        if (--e.Attached <= 0) _byHost.Remove(host);
    }

    /// <summary>Run-unit start hygiene.</summary>
    public void Clear() => _byHost.Clear();
}

/// <summary>The shared RELATIVE record store: RRN (1-based, §12.4.5.13 GR1) → record image.</summary>
internal sealed class RelativeStore
{
    public readonly SortedDictionary<long, string> Slots = new();
}

/// <summary>One stored indexed record: its character image and its PER-KEY release ordinals — lifted out of
/// <see cref="IndexedConnector"/> when the store became shared (kb/Work PB143).
/// <para>⛔ THE RELEASE ORDINAL IS PER KEY OF REFERENCE, NOT PER RECORD (kb/Work PB341). ISO §14.9.30.4 GR26
/// names the retrieval order of duplicates under "an alternate record key that IS THE KEY OF REFERENCE", and
/// §14.9.35.4 GR24 a) — "When the value of a specific alternate record key is not changed, the order of
/// retrieval when that key is the key of reference remains unchanged" — makes each key's order independent of
/// every other key's: a REWRITE repositions the record ONLY in the duplicate sets of the keys it actually
/// changed (GR24 b). One number per record could not express that, so a REWRITE that changed one alternate key
/// silently reordered every OTHER alternate key's duplicate sequence. <see cref="Ordinals"/> is therefore a
/// VECTOR: slot 0 the prime key (assigned once at release and never re-stamped — a prime key value cannot
/// change, §14.9.35.4 GR22/GR23 identify the record BY it — so it doubles as the record's release order in the
/// physical file), slot <c>i + 1</c> the i-th alternate key.</para></summary>
internal sealed class KeyedRec
{
    public string Image = "";
    public long[] Ordinals = [];
}

/// <summary>The shared INDEXED record store: the records plus the release-ordinal mint — shared so a WRITE
/// through one connector takes the next GLOBAL ordinal and §14.9.30.4 GR26's duplicate-alternate retrieval
/// order holds across connectors.</summary>
internal sealed class IndexedStore
{
    public readonly List<KeyedRec> Recs = [];
    public long NextOrdinal = 1;
}
