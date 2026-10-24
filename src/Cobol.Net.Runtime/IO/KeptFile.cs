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
/// <para>⛔ <see cref="Identity"/> is the host's identity of the physical file the CLOSE kept it on (kb/Work PB3533), taken from the
/// CLOSE's handle: the path alone cannot tell that file from another one made at the same path once it is removed or renamed away
/// outside the run unit (<see cref="IsOfTheFileAt"/>).</para>
/// </summary>
internal sealed record KeptFile(KeptHandle? Handle, bool Adopted, RunUnitFileLock? FileLock, RegionLocks Endpoint,
    string HostPath, PhysicalFileIdentity? Identity, PhysicalFileTable.State? Physical, KeyedStore Store, StoreTable? Table,
    RestoringTreesFactory Restoring)
{
    /// <summary>⛔ WHETHER <paramref name="hostPath"/> NAMES THE PHYSICAL FILE THIS STATE WAS KEPT ON, NOW (kb/Work PB3533): the same
    /// path (<see cref="HostFile.PhysicalFileComparer"/>) and, where the host can tell, the same file
    /// (<see cref="HostFile.IdentityAt"/>) — false for a file made at the path after the kept one was removed or renamed away outside
    /// the run unit, which a host that unlinks a file under an open handle (Linux) permits whatever the kept handle's share mode.
    /// The kept file's identity cannot pass to the new one meanwhile: the kept handle, or the descriptor of the column lock kept
    /// with it, keeps the kept file alive. Where the host cannot tell (no identity, or a path that names nothing), the path
    /// decides.</summary>
    internal bool IsOfTheFileAt(string hostPath) =>
        HostFile.PhysicalFileComparer.Equals(HostPath, hostPath)
        && (Identity is not { } kept || HostFile.IdentityAt(hostPath) is not { } now || now == kept);

    /// <summary>Whether the state still holds the file against another opener: its handle, or its published file lock.</summary>
    internal bool HoldsTheFile => Handle is not null || FileLock?.Column is not null;

    /// <summary>The Table 19 column of the OPEN whose CLOSE kept this state (<c>FileConnector.KeptOpenColumn</c>).</summary>
    internal ExistingSharingColumn? OpenColumn { get; init; }

    /// <summary>The Table 19 column the state holds the file in: its file lock's, or — the host publishing none (a NO OTHER
    /// handle's share mode is its lock) — its OPEN's, while its handle stands.</summary>
    internal ExistingSharingColumn? Column => FileLock?.Column ?? (Handle is not null ? OpenColumn : null);

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
    /// by a re-OPEN, else the dead-owner step's to settle), then the endpoint disposed, which unlocks every byte it holds,
    /// the liveness byte among them, while the handle is still open, and only then the handle closed: a byte left to the
    /// close outlives it wherever another copy of the handle lives (a <c>fork</c> not yet at its <c>exec</c>), and the next
    /// writable OPEN would read the abandoned transaction's owner as alive (<see cref="HandleRegionLocks"/>; kb/Work
    /// PB3549). The attachment is untouched.</summary>
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
            finally { Table?.Detach(HostPath, Store); }
        }
    }
}
