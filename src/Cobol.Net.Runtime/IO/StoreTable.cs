// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// ⭐ THE RUN UNIT'S OPEN STORES, ONE PER PHYSICAL FILE (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.7, "Within a
/// run unit"; kb/Work PB143, PB3335; ISO §14.9.10.4 GR5 — "the identified record has been logically removed from THE
/// PHYSICAL FILE and can no longer be accessed"). Every connector of the run unit over one physical file attaches to the
/// ONE keyed store engine handle for it (<see cref="KeyedStore"/>: the run unit's page I/O, the page cache, the ledger and
/// the trees), keyed by resolved host path — the same key the <see cref="PhysicalFileTable"/> arbitrates sharing and locks
/// by — so a sibling's change is visible at once: there is one cache and one set of trees. The FIRST opener opens the
/// store through its handle, later openers ATTACH to the live store, and the LAST detach drops the entry, so a later OPEN
/// opens the file again. Position and key state (the file position indicator, the key of reference, the sequential-WRITE
/// slot, GR38's high key) stays per CONNECTOR. Two SELECTs to one ASSIGN target need no SHARING clause to reach this, so
/// the table is unconditional for the keyed organizations; sequential connectors keep their streams (the file system is
/// their shared store). Across run units the FILE is the truth, and each run unit's store follows it through page 2
/// (2.2.1.7). Owned by the <see cref="FileRegistry"/>; cleared at run-unit Reset.
/// </summary>
internal sealed class StoreTable
{
    private sealed class Entry
    {
        public required KeyedStore Store;
        public int Attached;
    }

    private readonly Dictionary<string, Entry> _byHost = new(HostFile.PhysicalFileComparer);   // kb/Work PB2748

    /// <summary>The last <see cref="KeyedStore.OpenOrder"/> stamped: every store that becomes the run unit's for its physical
    /// file takes the next one, so the stamps are the order of the run unit's open stores.</summary>
    private long _opened;

    /// <summary>Make <paramref name="store"/> the run unit's for <paramref name="host"/>, stamped with its place in the order of
    /// the open stores.</summary>
    private T Enter<T>(string host, T store) where T : KeyedStore
    {
        store.OpenOrder = ++_opened;
        _byHost[host] = new Entry { Store = store, Attached = 1 };
        return store;
    }

    /// <summary>Attach to the store of organization <typeparamref name="T"/> for <paramref name="host"/>: the live store
    /// when one exists (its trees are the truth — nothing is re-read), else a new store with no file, which the attaching
    /// connector opens through its own handle (<see cref="KeyedStore.Open"/>; a failed OPEN detaches again).</summary>
    public T Attach<T>(string host) where T : KeyedStore, new()
    {
        if (_byHost.TryGetValue(host, out var e))
        {
            if (e.Store is not T live)
                throw new InvalidOperationException(
                    $"physical file '{host}' is open under two different organizations ({e.Store.GetType().Name} vs {typeof(T).Name}) — a compiler/registration defect (kb/Work PB143)");
            e.Attached++;
            return live;
        }
        return Enter(host, new T());
    }

    /// <summary>⛔ ATTACH TO A STORE THAT IS BEING CREATED — <c>OPEN OUTPUT</c>, §14.9.27.4 GR18: a new, EMPTY store
    /// for <paramref name="host"/> that no other connector of this run unit holds (kb/Work PB754).
    /// <para>Emptying a store is a change every attached connector would see instantly (kb/Work PB143), so it is
    /// only ever done HERE, where the precondition that makes it safe is checked rather than remembered: Table 19
    /// refuses every OUTPUT open beside any other open connector — §9.1.13.9 1) e), <i>"An attempt is made to open a
    /// physical file in the output mode and the physical file is currently open by another file connector"</i> —
    /// and <c>FileRegistry.OpenShared</c> runs that arbitration BEFORE the connector's OPEN body. An entry that
    /// still exists here therefore means the arbitration was bypassed, and the answer is a loud defect, never a
    /// silent emptying of records another connector is reading (the analogous impossible state in
    /// <see cref="Attach{T}"/> is answered the same way).</para></summary>
    public T AttachCreated<T>(string host, T created) where T : KeyedStore
    {
        if (_byHost.TryGetValue(host, out var e))
            throw new InvalidOperationException(
                $"physical file '{host}' is being created by OPEN OUTPUT while {e.Attached} other connector(s) of this "
                + "run unit hold it open; Table 19 refuses that OPEN (§9.1.13.9 1) e)), so reaching here is an "
                + "arbitration defect (kb/Work PB754)");
        return Enter(host, created);
    }

    /// <summary>Detach one connector from <paramref name="host"/>'s store; the LAST detach drops the entry, so a later
    /// OPEN opens the file again. Unbalanced detaches are ignored (a failed OPEN never attached).</summary>
    public void Detach(string host)
    {
        if (!_byHost.TryGetValue(host, out var e)) return;
        if (--e.Attached <= 0) _byHost.Remove(host);
    }

    /// <summary>Run-unit start hygiene.</summary>
    public void Clear() => _byHost.Clear();
}
