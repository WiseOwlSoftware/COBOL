// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// ⛔ THE RUN UNIT'S TRANSACTIONS ON SEQUENTIAL PHYSICAL FILES (docs/rearchitecture/DESIGN-runtime-library.md 2.2.2.3, 2.2.2.9;
/// kb/Work PB3340): ONE per physical file, told by its absolute host path, owned by the run unit's registry
/// (<see cref="FileRegistry"/>) and not by any connector — <i>"the holder file is the REGISTRY's, held from the first change to
/// the retirement whatever the connector does"</i> — so a connector that is closed, displaced or associated with another file
/// leaves its file's transaction standing for the COMMIT, the ROLLBACK and the run unit's end. Each carries its place in the order
/// of the run unit's open files (<see cref="StoreTable.NextOrder"/>), the one order a COMMIT and a ROLLBACK reach the keyed and the
/// sequential files in (2.2.2.6). It also carries the run unit's <see cref="IHostMedium"/>, which every covered connector and every
/// journal of the run unit writes through.
/// </summary>
internal sealed class SequentialTransactions(IHostMedium medium, Func<long> nextOrder)
{
    /// <summary>The run unit's host medium.</summary>
    internal IHostMedium Medium { get; } = medium;

    private readonly Dictionary<string, (SequentialJournal Journal, long Order)> _open = new(HostFile.PhysicalFileComparer);

    /// <summary>A table for a connector no registry holds (a unit test's standalone connector): the production medium, its own
    /// order.</summary>
    internal static SequentialTransactions Standalone()
    {
        long order = 0;
        return new SequentialTransactions(HostMedium.Production, () => ++order);
    }

    /// <summary>The run unit's open transaction on the physical file at <paramref name="hostPath"/>, or null.</summary>
    internal SequentialJournal? Of(string hostPath) =>
        hostPath.Length > 0 && _open.TryGetValue(Path.GetFullPath(hostPath), out var open) ? open.Journal : null;

    /// <summary>Begin the run unit's transaction on <paramref name="hostPath"/> (<see cref="SequentialJournal.Begin"/>), under the
    /// entry mutex the caller holds; <paramref name="ofKeyedFile"/> for a RELATIVE or INDEXED file's creation or capture
    /// (<see cref="SequentialJournal.OfKeyedFile"/>).</summary>
    internal SequentialJournal Begin(string hostPath, SequentialJournal.StartState start, bool ofKeyedFile = false)
    {
        var journal = SequentialJournal.Begin(Medium, hostPath, start, ofKeyedFile);
        _open[journal.HostPath] = (journal, nextOrder());
        return journal;
    }

    /// <summary>The transaction on <paramref name="journal"/>'s file is over (committed, or rolled back and retired).</summary>
    internal void End(SequentialJournal journal) => _open.Remove(journal.HostPath);

    /// <summary>Withdraw <paramref name="journal"/>, begun by an OPEN that is unsuccessful with the file not affected
    /// (<see cref="SequentialJournal.Withdraw"/>); it stays open when the medium refuses the withdrawal.</summary>
    internal void Withdraw(SequentialJournal journal)
    {
        journal.Withdraw();
        if (journal.Retired) End(journal);
    }

    /// <summary>Every open transaction with its place in the order of the run unit's open files.</summary>
    internal IEnumerable<(SequentialJournal Journal, long Order)> Open => _open.Values;

    /// <summary>⛔ HOLD THE ENTRY MUTEX of the physical file at <paramref name="hostPath"/> (<see cref="HostRegionLocks.EntryMutexByte"/>),
    /// waiting for another run unit's entry step: the returned endpoint holds it until disposed. A host that cannot hold it here (a
    /// lock directory this process may not create or list) is a refused first change: <see cref="IOException"/>, the statement's
    /// '30' (2.2.2.12: "a lock directory the process may not create — '30' at that statement (R1 refused), F unchanged").</summary>
    internal RegionLocks HoldEntryMutex(string hostPath) =>
        TryHoldEntryMutex(hostPath)
        ?? throw new IOException($"the entry mutex of '{hostPath}' cannot be held: its lock directory cannot be written here (DOC-A.1-75)");

    /// <summary>The entry mutex as <see cref="HoldEntryMutex"/> holds it, or null where this host cannot hold it here: the WAITING
    /// hold's rule for a connector whose statement changes no transaction (DOC-A.1-75: it <i>"proceeds without the cross-run-unit
    /// mutex for that statement"</i>; <see cref="SequentialConnector"/>'s uncovered writer under <c>SHARING WITH ALL OTHER</c>).</summary>
    internal RegionLocks? TryHoldEntryMutex(string hostPath)
    {
        var mutex = Medium.EntryMutex(hostPath);
        if (mutex.Hold(HostRegionLocks.EntryMutexByte, exclusive: true, wait: true) == RegionLockResult.Ok) return mutex;
        mutex.Dispose();
        return null;
    }
}
