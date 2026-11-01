// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime.IO.Store;

namespace WiseOwl.Cobol.Runtime.IO;

/// <summary>
/// ⛔ THE DEAD-OWNER STEP OF A CONNECTOR THAT HAS NO WRITABLE HANDLE (DESIGN-runtime-library.md 2.2.1.10, "The dead owner's
/// rollback is every reader's too"; kb/Work PB4097). §9.1.18.5: <i>"When a run unit terminates abnormally, all file changes made
/// since the previous commit or, if none, the beginning of the run unit are rolled back for all files for which there are
/// active APPLY COMMIT clauses."</i> — and only <i>"In both cases all file and record locks are then released"</i>. A dead
/// owner's locks are gone with its handles, so no run unit may observe the file between the death and the rollback: the
/// uncommitted change is not in the file once the locks are not. A writable connector rolls the owner back at its OPEN and at
/// every statement's entry (<see cref="KeyedConnector.Enter"/>, <see cref="KeyedStore.RollBackDeadOwners"/>); an INPUT
/// connector's handle is read-only and its entry holds the mutex shared (Linux refuses an exclusive lock through a read-only
/// descriptor), so it settles through a handle of its own, opened for the purpose and closed with the settle — the keyed
/// counterpart of <see cref="SequentialJournal.AuxiliaryData"/> (2.2.2.7), which a sequential reader's OPEN already used.
/// </summary>
public abstract partial class KeyedConnector
{
    /// <summary>A reader's coherent entry has taken the mutex shared and read page 2: whether a transaction of another run unit
    /// stands whose owner is dead (<see cref="KeyedStore.FindDeadOwner"/>; nothing is read while page 2 names no transaction).</summary>
    private StoreStatus ReaderEntryFindsDeadOwner(KeyedStore store, out bool found) => store.FindDeadOwner(_regions!, out found);

    /// <summary>⛔ A READER'S STATEMENT SETTLES THE DEAD OWNERS BEFORE IT READS (§9.1.18.5): the shared mutex is let go — the settle
    /// takes it exclusively through its own handle, which is another open file description than this connector's — the dead
    /// owners are rolled back (<see cref="SettleDeadOwnersBeside"/>), and the statement enters again, page 2 read anew: the settle
    /// moved it, so the store adopts it and drops its page cache (2.2.1.7). A host refusal of the settle is the entry's host
    /// failure, a permanent error (<see cref="Enter"/>). Reached only by a connector whose share mode admits another run unit's
    /// writer, since only then can an owner die beside it; the owners dead before its OPEN were settled by the OPEN.</summary>
    private void SettleAtTheEntry(KeyedStore store)
    {
        store.MutexHolder?.Release(HostRegionLocks.StoreMutexByte);
        store.MutexHolder = null;
        SettleDeadOwnersBeside();
        store.MutexHolder = HostRegionLocks.HoldStoreMutex(_regions!);
        if (store.File is { } file && file.Enter(exclusive: false) != StoreStatus.Ok) SetStatus(FileStatusCode.PermanentError);
    }

    /// <summary>⛔ THE OPEN OF A CONNECTOR WITH NO WRITABLE HANDLE SETTLES THE DEAD OWNERS BEFORE ANY RECORD IS READ (§9.1.18.5;
    /// kb/Work PB4097): the store the OPEN just attached is entered under the mutex (a live store another connector of the run unit
    /// holds is not re-read by the OPEN) and asked for a dead owner. None: the OPEN proceeds. One: the OPEN's own handle goes first
    /// when its share mode could refuse the settle's writable one (a host whose share modes are mandatory refuses a writer beside
    /// a handle that excludes writers; <see cref="HostFile.ShareModesAreMandatory"/>), the owners are rolled back, and the OPEN
    /// takes its handle and attaches again — the Table 19 column (the file lock between run units) stays held throughout, so no
    /// run unit the table refuses gets in between. A connector holding the handle a CLOSE under commit control kept keeps it
    /// (that handle is the transaction's file lock and liveness byte). A host refusal of the settle is the OPEN's own: '37' for
    /// the authority it lacks (§14.9.27.4 GR3), '61' for a handle another run unit's share mode refuses, otherwise '30' — the
    /// statuses of every OPEN whose host open is refused (<see cref="FileConnector.Open"/>). Returns the failing I-O status of the
    /// attach, or null.</summary>
    private string? SettleDeadOwnersOfTheOpen()
    {
        if (StoreHandleWritable || !_attached) return null;
        var store = AttachedStore;
        bool found = false;
        var status = InStoreMutex(() =>
        {
            var entered = store.File is { } file ? file.Enter(exclusive: false) : StoreStatus.Ok;
            return entered != StoreStatus.Ok ? entered : store.FindDeadOwner(_regions!, out found);
        });
        if (status != StoreStatus.Ok) return Failed(status);
        if (!found) return null;
        if (_kept is not null)
        {
            SettleDeadOwnersBeside();
            return Failed(InStoreMutex(() => store.File is { } file ? file.Enter(exclusive: false) : StoreStatus.Ok));
        }
        try { _ = Detach(); }
        finally { ReleaseFileLock(); }
        SettleDeadOwnersBeside();
        return Lock(FileTaking.Existing) ?? Attach(create: false);
    }

    /// <summary>⛔ ROLL BACK EVERY DEAD OWNER'S TRANSACTION THROUGH A WRITABLE HANDLE OPENED FOR THE PURPOSE (§9.1.18.5; 2.2.1.10): a
    /// scratch store of this organization (<see cref="NewStore"/>, which no table and no connector holds) is adopted through the
    /// new handle as every writable OPEN adopts (2.2.1.6 R6 and its fence), under the exclusive store mutex taken through it, its
    /// dead-owner step runs with this connector's declared trees (<see cref="Restoring"/>), and its promotion lands (the
    /// store's CLOSE, 2.2.1.6 R5) so that a reader's next adoption of the committed state finds no dead transaction either.
    /// The run unit's own store is not touched: its next coherent entry reads page 2 and adopts what the settle wrote. The handle
    /// shares with every other run unit as the sharing modes that admit another run unit's writer do; a host that refuses it
    /// (the file is read-only to this process, or a handle with an incompatible share mode is open) throws what an OPEN's host
    /// open throws, for the caller's status. The mutex wait is the mutex's: a live writer's statement is one step and ends.</summary>
    /// <exception cref="IOException">The host refused the handle, or the medium refused a page of the settle.</exception>
    private void SettleDeadOwnersBeside()
    {
        using var stream = HostFile.OpenConnectorStore(HostPath, FileMode.Open, FileAccess.ReadWrite, FileLockPosture.ForSettling);
        var handle = stream.SafeFileHandle;
        using var regions = HostRegionLocks.Open(handle, HostPath, writable: true);
        var scratch = NewStore();
        scratch.AttachHandle(handle, writable: true, HostFile.InputOutputAreaBuffer(InputOutputAreas) / PageHeader.PageSize);
        var status = HostRegionLocks.InStoreMutex(regions, () =>
        {
            var settled = scratch.Open(writable: true, coherent: true, DeclaredAttributes, HoldingsOrder);
            if (settled == StoreStatus.Ok) settled = scratch.RollBackDeadOwners(regions, Restoring);
            if (settled == StoreStatus.Ok && scratch.File is { Writable: true } file) settled = file.Close();
            return settled;
        });
        if (status != StoreStatus.Ok) throw new IOException($"the medium refused the rollback of a dead run unit's transaction in '{HostPath}' (I-O status 30)");
    }
}
