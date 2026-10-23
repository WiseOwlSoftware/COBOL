// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO.Store;
using Microsoft.Win32.SafeHandles;

namespace CobolNet.Runtime.IO;

/// <summary>The handle a CLOSE under commit control kept: its stream, its region endpoint — through which the run unit's
/// transaction's liveness byte may be held — and the access and share mode it was taken with (what
/// <c>KeyedConnector.RetakeKeptHandle</c> takes it again with).</summary>
internal sealed record KeptHandle(FileStream Stream, SafeFileHandle Handle, RegionLocks Regions, FileAccess Access, FileShare Share);

/// <summary>
/// ⛔ WHAT A CLOSE UNDER COMMIT CONTROL KEPT, KEYED BY THE PHYSICAL FILE IT BELONGS TO (§9.1.15, §14.9.6.4 GR9, §14.9.7.4 GR4;
/// DESIGN-runtime-library.md 2.2.1.10; kb/Work PB3494): the run unit's attachment to the file's store — and in it the run
/// unit's transaction on the file — until the next COMMIT or ROLLBACK or the run unit's end (<see cref="Release"/>), or the
/// next OPEN of a connector on the SAME physical file, which carries it on (<c>KeyedConnector.CarryKeptOver</c>,
/// <c>InheritKept</c>). It names the file (<see cref="HostPath"/>, <see cref="Physical"/>, <see cref="Store"/>) and the
/// order its trees restore by (<see cref="Restoring"/>, the declaration of the OPEN that kept it), so it does not depend on
/// the connector that kept it: a connector that is displaced by its element's next activation (<c>FileRegistry.Retire</c>),
/// or that an ASSIGN USING (§12.4.5.3 GR3 b) associates with ANOTHER physical file, hands it to the registry, which ends it
/// with the rest at the COMMIT, the ROLLBACK or the run unit's end.
/// <para><see cref="Handle"/> is null once given back early (a re-OPEN whose share mode it refused and could not be taken
/// again) and <see cref="Adopted"/> is true while a re-OPEN that would take the same handle uses it as its own; the Table 19
/// run-unit <see cref="FileLock"/> is null where the host publishes none, or while a re-OPEN runs that has replaced it by its
/// own (<c>FileConnector.TakeKeptFileLockOver</c>, put back by <c>TryRestoreKeptFileLock</c> when the re-OPEN is
/// unsuccessful); and the <see cref="Endpoint"/> the record locks (§14.9.6.4 GR9) are published through and are its to find:
/// the CLOSE's, or the one a retake took, which stays named when the <see cref="Handle"/> is given back.</para>
/// </summary>
internal sealed record KeptFile(KeptHandle? Handle, bool Adopted, RunUnitFileLock? FileLock, RegionLocks Endpoint,
    string HostPath, PhysicalFileTable.State? Physical, KeyedStore Store, StoreTable? Table, RestoringTreesFactory Restoring)
{
    /// <summary>The run unit's transaction on the physical file, 0 while none.</summary>
    internal ulong OpenTransaction => Store.Transaction;

    /// <summary>⛔ COMMIT the run unit's transaction on this file (§14.9.7.4 GR3; 2.2.1.10), under the store mutex through
    /// the kept endpoint (a kept handle given back early has none, and the body runs as a NO OTHER file's does).</summary>
    internal CommitOutcome CommitTransaction() => HostRegionLocks.InStoreMutex(Handle?.Regions, () => Store.Commit(Restoring));

    /// <summary>⛔ ROLLBACK the run unit's transaction on this file (§14.9.36.4 GR3; 2.2.1.10), under the store mutex through
    /// the kept endpoint.</summary>
    internal StoreStatus RollbackTransaction() => HostRegionLocks.InStoreMutex(Handle?.Regions, () => Store.Rollback(Restoring));

    /// <summary>Give a kept handle back: off <paramref name="store"/>'s medium under the store mutex its endpoint holds, the
    /// liveness byte it may hold going with it (<see cref="KeyedStore.LivenessGoneWith"/>: what a death does — taken again
    /// by a re-OPEN, else the dead-owner step's to settle), then the endpoint and the handle disposed. The attachment is
    /// untouched.</summary>
    internal static void GiveBack(KeyedStore store, KeptHandle kept)
    {
        try
        {
            _ = HostRegionLocks.InStoreMutex(kept.Regions, () => store.DetachHandle(kept.Handle));
            store.LivenessGoneWith(kept.Regions);
        }
        finally
        {
            try { kept.Regions.Dispose(); }
            finally { kept.Stream.Dispose(); }
        }
    }

    /// <summary>Give back everything a CLOSE under commit control kept — §9.1.15, <i>"the file lock is removed by the COMMIT
    /// statement"</i>; §9.1.18.5, at the run unit's end <i>"all file and record locks are then released"</i>; and an
    /// <c>OPEN OUTPUT</c>, which creates the file anew: the record locks published through the kept endpoint (out of this
    /// run unit's table as well as off the host: the endpoint is the one owner of them), the handle, the run-unit file lock
    /// and the run unit's attachment to the store. A transaction still standing at that moment (a ROLLBACK refused) is left
    /// as a death leaves it, for the dead-owner step.</summary>
    internal void Release()
    {
        try
        {
            if (Physical is { } physical) PhysicalFileTable.ReleaseThrough(physical, Endpoint);
            if (Handle is { } handle && !Adopted) GiveBack(Store, handle);
        }
        finally
        {
            try { FileLock?.Dispose(); }
            finally { Table?.Detach(HostPath); }
        }
    }
}
