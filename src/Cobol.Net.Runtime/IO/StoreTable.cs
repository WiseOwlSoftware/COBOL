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
/// <para>⛔ A PATH NAMES A PHYSICAL FILE ONLY AT THE INSTANT IT IS ASKED (kb/Work PB3533). Each entry remembers the host's
/// identity of the file its store was opened on (<see cref="HostFile.IdentityOf"/>, from the attaching connector's handle), and
/// an attachment through a handle on ANOTHER file at the same path — the first one removed or renamed away and another made there
/// outside the run unit, which Linux permits under an open handle — gets a store of its own: the path's entry becomes the new
/// file's, and the old store stays its connectors' alone (they reach it through their own reference, and their
/// <see cref="Detach"/> no longer touches the path's entry). Attaching to the live store would have read the removed file's
/// records through the new file's connector.</para>
/// </summary>
internal sealed class StoreTable
{
    private sealed class Entry
    {
        public required KeyedStore Store;
        public int Attached;
        /// <summary>The host's identity of the physical file <see cref="Store"/> was opened on, or null while no attaching handle
        /// has told it (an absent OPTIONAL file's store has no file) or the host cannot.</summary>
        public PhysicalFileIdentity? Identity;
    }

    private readonly Dictionary<string, Entry> _byHost = new(HostFile.PhysicalFileComparer);   // kb/Work PB2748

    /// <summary>The last <see cref="KeyedStore.OpenOrder"/> stamped: every store that becomes the run unit's for its physical
    /// file takes the next one, so the stamps are the order of the run unit's open stores.</summary>
    private long _opened;

    /// <summary>The next place in the order of the run unit's open files: a store that becomes the run unit's takes one
    /// (<see cref="Enter{T}"/>), and so does a sequential file's transaction when it begins (<see cref="SequentialTransactions"/>;
    /// DESIGN-runtime-library.md 2.2.2.6), so a COMMIT and a ROLLBACK reach the keyed and the sequential files in one order.</summary>
    internal long NextOrder() => ++_opened;

    /// <summary>Make <paramref name="store"/> the run unit's for <paramref name="host"/>, the physical file of
    /// <paramref name="identity"/>, stamped with its place in the order of the open stores. An entry it displaces was of a file the
    /// path no longer names (<see cref="NamesAnotherFile"/>).</summary>
    private T Enter<T>(string host, PhysicalFileIdentity? identity, T store) where T : KeyedStore
    {
        store.OpenOrder = NextOrder();
        _byHost[host] = new Entry { Store = store, Attached = 1, Identity = identity };
        return store;
    }

    /// <summary>Whether <paramref name="identity"/>, the file an attaching handle is open on, is another physical file than the
    /// one <paramref name="e"/>'s store was opened on: both known, and different.</summary>
    private static bool NamesAnotherFile(Entry e, PhysicalFileIdentity? identity) =>
        e.Identity is { } held && identity is { } now && held != now;

    /// <summary>Attach to the store of organization <typeparamref name="T"/> for <paramref name="host"/>, through a handle on the
    /// physical file of <paramref name="identity"/>: the live store when one exists for that file (its trees are the truth —
    /// nothing is re-read), else a new store with no file, which the attaching connector opens through its own handle
    /// (<see cref="KeyedStore.Open"/>; a failed OPEN detaches again).</summary>
    public T Attach<T>(string host, PhysicalFileIdentity? identity) where T : KeyedStore, new()
    {
        if (_byHost.TryGetValue(host, out var e) && !NamesAnotherFile(e, identity))
        {
            if (e.Store is not T live)
                throw new InvalidOperationException(
                    $"physical file '{host}' is open under two different organizations ({e.Store.GetType().Name} vs {typeof(T).Name}) — a compiler/registration defect (kb/Work PB143)");
            e.Attached++;
            e.Identity ??= identity;
            return live;
        }
        return Enter(host, identity, new T());
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
    /// <see cref="Attach{T}"/> is answered the same way).</para>
    /// <para>⛔ WHERE TABLE 19 IS NOT ASKED THE OPEN IS ADMITTED, AND THE CREATION REPLACES THE PATH'S STORE (kb/Work PB670):
    /// at COBOL-85 (<paramref name="arbitrated"/> false: <see cref="FileConnector.SharingFacilityInEffect"/>) no sharing mode
    /// restricts another connector, so an OPEN OUTPUT beside connectors that hold the file is a normal open and creates the
    /// file under them (§14.9.27.4 GR18). The created store becomes the run unit's for the path, as it does for a file replaced
    /// at the path from outside (<see cref="Attach{T}"/>, kb/Work PB3533): the store the siblings hold stays theirs alone, their
    /// <see cref="Detach"/> no longer touches the path's entry, and what they find in the file from then on is the file's own
    /// word, the engine's statuses for a file that changed under an open connector (2.2.1.6).</para></summary>
    public T AttachCreated<T>(string host, PhysicalFileIdentity? identity, T created, bool arbitrated) where T : KeyedStore
    {
        if (arbitrated && _byHost.TryGetValue(host, out var e) && !NamesAnotherFile(e, identity))
            throw new InvalidOperationException(
                $"physical file '{host}' is being created by OPEN OUTPUT while {e.Attached} other connector(s) of this "
                + "run unit hold it open; Table 19 refuses that OPEN (§9.1.13.9 1) e)), so reaching here is an "
                + "arbitration defect (kb/Work PB754)");
        return Enter(host, identity, created);
    }

    /// <summary>Detach one connector from <paramref name="store"/>, its store for <paramref name="host"/>; the LAST detach drops
    /// the entry, so a later OPEN opens the file again. A store the path's entry no longer holds (its file was replaced at the
    /// path, <see cref="Attach{T}"/>) is its connectors' alone, and unbalanced detaches are ignored (a failed OPEN never
    /// attached).</summary>
    public void Detach(string host, KeyedStore store)
    {
        if (!_byHost.TryGetValue(host, out var e) || !ReferenceEquals(e.Store, store)) return;
        if (--e.Attached <= 0) _byHost.Remove(host);
    }

    /// <summary>Run-unit start hygiene.</summary>
    public void Clear() => _byHost.Clear();
}
