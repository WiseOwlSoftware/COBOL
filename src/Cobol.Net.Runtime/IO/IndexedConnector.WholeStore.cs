// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers;
using Microsoft.Win32.SafeHandles;

namespace CobolNet.Runtime.IO;

/// <summary>⛔ THE WHOLE-STORE MODEL OF THE INDEXED ORGANIZATION — loaded whole at OPEN and at every coherent entry
/// whose generation stamp moved, persisted whole by every statement that changed a coherent store and by the CLOSE
/// (kb/Work PB143, PB2660, PB771). The RELATIVE organization left it for the keyed store engine
/// (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1, kb/Work PB3333); the INDEXED organization follows
/// (kb/Work PB3334), and this file is deleted with the model (PB3335, 2.2.1.13). Nothing outside
/// <see cref="IndexedConnector"/> reaches it.</summary>
public sealed partial class IndexedConnector
{
    /// <inheritdoc/>
    private protected override StoreFormat RecordedAttributes(out FixedFileAttributes? recorded) =>
        RecordFraming.ReadHeader(HostPath, out recorded);

    /// <inheritdoc/>
    /// <remarks>⛔ A STATEMENT THAT CHANGED THE STORE PERSISTS IT AT THE STATEMENT WHENEVER ANOTHER CONNECTOR MAY BE ON
    /// THE FILE — a reader as well as a writer (kb/Work PB1587). §14.9.51.4 GR12 releases a written record to the
    /// operating environment, and §9.1.15 2) admits the INPUT connector of another run unit beside a
    /// <c>SHARING WITH READ ONLY</c> writer, so a record this connector answered '00' for must be in the file for that
    /// reader: until PB1587 only <c>SHARING WITH ALL OTHER</c> was coherent, and a READ ONLY writer's records reached
    /// the file at its CLOSE (the reader met '10'). The same predicate the paged engine asks
    /// (<see cref="FileLockPosture.AdmitsAnotherRunUnit"/>, 2.2.1.7), which this model leaves with PB3334.</remarks>
    private protected override bool CoherentUnder(FileShare share) => FileLockPosture.AdmitsAnotherRunUnit(share);

    /// <summary>⛔ THIS CONNECTOR'S INPUT-OUTPUT AREAS — ISO §12.4.5.14.3 GR1, <i>"If the RESERVE clause is specified,
    /// the number of input-output areas allocated is equal to the value of integer-1"</i> — allocated by the OPEN that
    /// takes the file lock (<see cref="TakeFileLock"/>; a run unit that cannot supply them gets '30' on that OPEN,
    /// <c>FileConnector.Open</c>) and released by the CLOSE. Every byte of the store passes through them: the load
    /// reads the file one buffer of areas at a time (<see cref="ReadImage"/>) and the persist writes it the same way
    /// (<see cref="Persist"/>), positionally on the handle, because a stream buffer would hand back bytes another
    /// run unit has since replaced (kb/Work PB2660). Until kb/Work PB2693 the areas were the HANDLE's stream buffer,
    /// which no store byte ever passed through, so RESERVE allocated memory nothing used. Null while the connector
    /// holds no file lock.</summary>
    private byte[]? _areas;

    /// <inheritdoc/>
    private protected override void AllocateAreas() => _areas = new byte[HostFile.InputOutputAreaBuffer(InputOutputAreas)];

    /// <inheritdoc/>
    private protected override void ReleaseAreas() => _areas = null;

    /// <inheritdoc/>
    /// <remarks>The whole store's entry only reads (the generation stamp, then a reload), so it is the same under either
    /// hold.</remarks>
    private protected override bool EnterStore(bool exclusive)
    {
        if (ReadGeneration() is { } stamped && stamped != _st.Generation) Reload(_st);
        _st.VersionAtEntry = _st.Version;
        return true;
    }

    /// <inheritdoc/>
    /// <remarks>A failed reload leaves the store as <see cref="Reload"/>'s contract says (untouched, or stamped with a
    /// generation the file no longer has and owing the file nothing), so the statement owes nothing either.</remarks>
    private protected override void EnterStoreFailed() => _st.VersionAtEntry = _st.Version;

    /// <inheritdoc/>
    private protected override string CompleteStore(string status)
    {
        if (_st.Version != _st.VersionAtEntry) Persist(_st);
        return status;
    }

    /// <summary>The handle the store travels through.</summary>
    private SafeFileHandle Handle => StoreHandle!;


    /// <summary>The bytes of this connector's store header — measured once (the declared attributes are fixed for
    /// the connector's life), and -1 until first asked.</summary>
    private long _storeHeaderBytes = -1;

    /// <summary>⛔ THE ONE BOUNDARY TEST OF A RELATIVE OR INDEXED FILE (kb/Work PB1192): would a store whose frames
    /// occupy <paramref name="framedBytes"/> bytes still fit the format's capacity,
    /// <see cref="RecordFraming.MaxStoreBytes"/>? False is §14.9.51.4 GR33 b) / GR42 d)'s <i>"outside the
    /// externally defined boundaries"</i> — I-O status '24', the invalid key condition — decided HERE, at the
    /// WRITE or REWRITE, before anything is released: the store lives in memory while the file is open and is
    /// written whole at CLOSE, so a record it cannot hold used to be reported '00' and then fail at the CLOSE
    /// (or, for a relative key in the billions, kill the run unit there). Annex A.1 item 107 is the
    /// determination (docs/CONFORMANCE.md <c>DOC-A.1-107</c>).</summary>
    private bool StoreHolds(long framedBytes)
    {
        if (_storeHeaderBytes < 0) _storeHeaderBytes = RecordFraming.HeaderBytes(DeclaredAttributes);
        return _storeHeaderBytes + framedBytes <= RecordFraming.MaxStoreBytes;
    }

    /// <summary>Whether a CLOSE that owes a persist still holds the handle it persists through. Every writable
    /// arm of every keyed OPEN takes it (<see cref="TakeFileLock"/>), so false is an invariant breach, and
    /// §9.1.13.6 item 1's '30' is the answer: the records this connector holds cannot be written, and reporting
    /// a successful CLOSE over them would be the silent loss kb/Work PB771 exists to remove.
    /// </summary>
    private bool PersistIsReachable(bool owed) => !owed || StoreHandle is not null;

    /// <summary>The generation stamped in the physical file now — null for a file holding no store header this
    /// build reads (an empty file, before its creation arm writes one).</summary>
    private ulong? ReadGeneration()
    {
        Span<byte> head = stackalloc byte[RecordFraming.GenerationProbeBytes];
        int got = ReadAt(Handle, head, 0);
        return RecordFraming.DecodeGeneration(head[..got]);
    }

    /// <summary>⛔ A RELOAD THAT FAILS LEAVES THE STORE EITHER UNTOUCHED OR UNTRUSTED, NEVER HALF-RELOADED AND TRUSTED
    /// (kb/Work PB2842, review round 1). The file is read and decoded FIRST, touching nothing of the store: a failure
    /// there (a file past <see cref="RecordFraming.MaxStoreBytes"/>, an allocation the host refuses) leaves the
    /// records, the generation and the owed persist exactly as they were. Only <see cref="Fill"/> replaces records,
    /// and a failure inside it leaves a partial store, which then owes the file nothing
    /// (<see cref="IndexedStore.PersistedVersion"/> = <see cref="IndexedStore.Version"/>), so a CLOSE cannot write the
    /// partial records over the other run unit's. The generation the store reflects is stamped only AFTER the records
    /// are in, so in both cases it still differs from the file's stamp and every coherent statement entry of the run
    /// unit — this connector's or another connector's on the same store — reloads again.</summary>
    private void Reload(IndexedStore store)
    {
        var frames = ReadImage(out long releaseMint, out ulong generation);
        HostFaultObserver.Value?.Invoke(HostFaultPoint.ReloadDecoded);
        try
        {
            Fill(store, frames, releaseMint);
        }
        catch
        {
            store.PersistedVersion = store.Version;
            throw;
        }
        store.Generation = generation;
        store.PersistedVersion = store.Version;
    }

    /// <summary>A point in a keyed connector's host I/O at which a test makes the host refuse
    /// (<see cref="HostFaultObserver"/>) — failures a real file cannot be made to produce on demand (an allocation the
    /// host refuses while decoding or composing a store near <see cref="RecordFraming.MaxStoreBytes"/>).</summary>
    internal enum HostFaultPoint
    {
        /// <summary>The file has been read and decoded; nothing of the store has changed yet.</summary>
        ReloadDecoded,
        /// <summary>The store has just been emptied for a reload (<see cref="Fill"/>, after its Clear) and is about to
        /// be refilled: a refusal here leaves a partial store.</summary>
        ReloadFilling,
        /// <summary>The persist is about to compose the store's image.</summary>
        PersistComposing,
    }

    /// <summary>Test seam, per async flow (the <see cref="FileRegistry.PauseObserver"/> precedent): called at each
    /// <see cref="HostFaultPoint"/>; the observer throws what the host would.</summary>
    internal static readonly AsyncLocal<Action<HostFaultPoint>?> HostFaultObserver = new();

    /// <summary>The organizations' <see cref="Fill"/> calls this right after it empties the store.</summary>
    private static void FaultPointReloadFilling() => HostFaultObserver.Value?.Invoke(HostFaultPoint.ReloadFilling);

    /// <summary>⛔ THE ONE LOAD — the first opener's (the <c>KeyedStoreTable</c> attach callback) and every coherent
    /// reload's. It reads under the store mutex, so it never sees a store another run unit is half-way through
    /// rewriting; an absent OPTIONAL file (no handle) loads nothing.</summary>
    private void Load(IndexedStore into)
    {
        if (StoreHandle is null)
        {
            Fill(into, [], releaseMint: 0);
            into.PersistedVersion = into.Version;
            return;
        }
        if (into.StatementDepth > 0) { Reload(into); return; }   // already inside the mutex
        var holder = HoldStoreMutex(exclusive: StoreHandleWritable);
        try { Reload(into); }
        finally { holder?.Release(HostRegionLocks.StoreMutexByte); }
    }

    /// <summary>⛔ THE ONE PERSIST — every statement that changed a coherent store, every CLOSE that owes one, and the
    /// creation of a new store. It writes the whole image positionally through the connector's own handle, stamped
    /// with one more than the generation the file carries NOW, and truncates whatever a longer previous image left;
    /// then the store agrees with the file again. The caller holds the mutex or takes it here.</summary>
    private void Persist(IndexedStore store)
    {
        var handle = Handle;
        var holder = store.StatementDepth == 0 ? HoldStoreMutex(exclusive: true) : null;
        try
        {
            ulong generation = (ReadGeneration() ?? store.Generation) + 1;
            HostFaultObserver.Value?.Invoke(HostFaultPoint.PersistComposing);
            using var image = RecordFraming.ComposeStore(DeclaredAttributes, PersistFrames(), generation, ReleaseMint,
                CodeSet);
            WriteThroughAreas(handle, new ReadOnlySpan<byte>(image.GetBuffer(), 0, (int)image.Length));
            RandomAccess.SetLength(handle, image.Length);
            store.Generation = generation;
            store.PersistedVersion = store.Version;
        }
        finally { holder?.Release(HostRegionLocks.StoreMutexByte); }
    }

    /// <summary>Persist at the CLOSE when this run unit holds records the file does not — the whole-store model's
    /// one write, and a no-op for a coherent connector, whose statements already persisted.</summary>
    private void PersistIfChanged(IndexedStore store)
    {
        if (store.Version != store.PersistedVersion) Persist(store);
    }

    /// <summary>The whole physical file, read positionally through the input-output areas under the caller's mutex,
    /// decoded; <paramref name="generation"/> receives the generation the image carries and <paramref name="releaseMint"/>
    /// its release mint. Touches nothing of the store — <see cref="Reload"/> stamps the generation once the records are in.</summary>
    private List<StoredFrame?> ReadImage(out long releaseMint, out ulong generation)
    {
        var handle = Handle;
        // ⛔ A FILE LONGER THAN ANY STORE THE FORMAT DESCRIBES IS NOT ONE THIS CONNECTOR CAN READ (kb/Work PB2748):
        // its header passed §14.9.27.4 GR10's comparison, but no store is ever written past MaxStoreBytes (StoreHolds
        // refuses the record that would take it there), so the bytes beyond are not records. It is §9.1.13.6 1)'s
        // permanent error, "no further information is available", answered '30' through OPEN's IOException mapping — a
        // checked cast used to kill the run unit with an OverflowException instead. A reload inside a later statement
        // (Enter) maps it to the same '30' (kb/Work PB2842).
        long length = RandomAccess.GetLength(handle);
        if (length > RecordFraming.MaxStoreBytes)
            throw new IOException($"'{HostPath}' is {length} bytes, longer than any store this format describes "
                + $"({RecordFraming.MaxStoreBytes} bytes)");
        int size = (int)length;
        // RENTED, not allocated: a keyed store is routinely past the 85 KB large-object threshold, and this runs at
        // every OPEN and every coherent reload.
        byte[] all = ArrayPool<byte>.Shared.Rent(Math.Max(size, 1));
        try
        {
            int got = ReadThroughAreas(handle, all.AsSpan(0, size));
            generation = RecordFraming.DecodeGeneration(all.AsSpan(0, got)) ?? 0;
            return RecordFraming.DecodeStore(all, got, out releaseMint, CodeSet);
        }
        finally { ArrayPool<byte>.Shared.Return(all); }
    }

    /// <summary>Fill <paramref name="into"/> from the start of the file through the input-output areas
    /// (<see cref="_areas"/>), one buffer of areas per positional read, until it is full or the file ends; returns the
    /// bytes read.</summary>
    private int ReadThroughAreas(SafeFileHandle handle, Span<byte> into)
    {
        var areas = _areas!;
        int total = 0;
        while (total < into.Length)
        {
            int n = RandomAccess.Read(handle, areas.AsSpan(0, Math.Min(areas.Length, into.Length - total)), total);
            if (n <= 0) break;
            areas.AsSpan(0, n).CopyTo(into[total..]);
            total += n;
        }
        return total;
    }

    /// <summary>Write <paramref name="from"/> at the start of the file through the input-output areas
    /// (<see cref="_areas"/>), one buffer of areas per positional write.</summary>
    private void WriteThroughAreas(SafeFileHandle handle, ReadOnlySpan<byte> from)
    {
        var areas = _areas!;
        for (int at = 0; at < from.Length; at += areas.Length)
        {
            int n = Math.Min(areas.Length, from.Length - at);
            from.Slice(at, n).CopyTo(areas);
            RandomAccess.Write(handle, new ReadOnlySpan<byte>(areas, 0, n), at);
        }
    }

    /// <summary>Fill <paramref name="into"/> from <paramref name="offset"/> until it is full or the file ends;
    /// returns the bytes read. Only the generation probe reads this way: it is the coherence stamp, not a transfer
    /// of the store.</summary>
    private static int ReadAt(SafeFileHandle handle, Span<byte> into, long offset)
    {
        int total = 0;
        while (total < into.Length)
        {
            int n = RandomAccess.Read(handle, into[total..], offset + total);
            if (n <= 0) break;
            total += n;
        }
        return total;
    }
}
