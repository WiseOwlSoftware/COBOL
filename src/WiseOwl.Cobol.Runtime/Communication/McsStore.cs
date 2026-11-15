// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32.SafeHandles;
using WiseOwl.Cobol.Runtime.IO;
using WiseOwl.Cobol.Runtime.IO.Store;

namespace WiseOwl.Cobol.Runtime.Communication;

/// <summary>
/// ⛔ THE MCS'S PAGED STORE, one per run unit's session (docs/rearchitecture/DESIGN-communication-facility.md §6 "The store" and "The transaction"): the file
/// <c>mcs.store</c> of the MCS directory on the paged store engine's one statement entry (DESIGN-runtime-library.md §2.2.1, §8 of the design). The session holds
/// the store's handle, the region-lock endpoint on it and the engine's <see cref="PagedFile"/> from its first statement to <see cref="Dispose"/> (the end of the run unit,
/// <c>RunUnit.Terminate</c>), and the byte <see cref="HostRegionLocks.McsPresenceByte"/> shared for that whole time.
/// <para>Each <see cref="Statement"/> is one statement of the engine, under the store mutex (<see cref="HostRegionLocks.StoreMutexByte"/>, exclusively, waiting for another run
/// unit's statement): enter the store coherently, act, transmit to the devices, land the edited tree as one page statement (<see cref="StatementLanding.Complete"/>, page 2
/// last), release. A host whose locks answer <see cref="RegionLockResult.Unavailable"/> is refused with status 92 before the statement reads anything, and the MCS never runs
/// unlocked: it never relies on a share-none open, which .NET expresses on Unix as an advisory <c>flock</c> that a file system may silently decline.</para>
/// <para>⛔ STATUS 92 TAKES NO ACTION, BY CONSTRUCTION. For a refusal the engine reports, <see cref="Refused"/> decides whether the statement landed before it answers
/// (the order is the design's §6 "Status 92 takes no action"): the session discards its <see cref="PagedFile"/> and, still inside the one mutex hold, opens a new READ-ONLY
/// one over the same handle, which runs the adoption rule (2.2.1.6 R6) in memory and writes nothing; the statement landed exactly when the adopted page 2 carries the
/// generation the statement stamped and names a tree other than the one the statement began on. A refused reopen is status 93, whose outcome is not known.</para>
/// </summary>
internal sealed class McsStore : IDisposable
{
    private readonly string _root;
    private readonly FileStream _stream;
    private readonly SafeFileHandle _handle;
    private readonly RegionLocks _regions;
    private PagedFile? _file;

    /// <summary>This session's run unit id: the owner of the partly received messages it holds (<see cref="McsTables.PutPartialReceive"/>), alive for as long as the session holds
    /// <see cref="HostRegionLocks.McsRunUnitLivenessByte"/>; 0 when the host let it take none (<see cref="DrawRunUnit"/>), and every statement then answers 92.</summary>
    private readonly ulong _runUnit;

    /// <summary>Whether the run unit's first statement has landed: it seeds a new or changed store and cuts each device to its committed end.</summary>
    private bool _prepared;

    private McsStore(string root, FileStream stream)
    {
        _root = root;
        _stream = stream;
        _handle = stream.SafeFileHandle;
        _regions = HostRegionLocks.Open(_handle, stream.Name, writable: true);
        _regions.Hold(HostRegionLocks.McsPresenceByte, exclusive: false, wait: false);
        _runUnit = DrawRunUnit(_regions);
    }

    /// <summary>The most run unit ids <see cref="DrawRunUnit"/> tries. A fresh id is held by another run unit only by a collision, of probability at most (live run units) x 2^-60 per
    /// draw, so a second Held in a row is already no collision: it is a host that cannot tell a free byte from a held one (the lock-file host over a lock directory this user may enter
    /// but not list answers every non-waiting hold Held, <c>SidecarRegionLocks</c>' <c>Conflict.Unobservable</c>; kb/Work PB5364). The bound keeps the draw finite there.</summary>
    private const int MaxRunUnitDraws = 4;

    /// <summary>⛔ A run unit id nobody else holds the liveness byte of, taken exclusively without waiting: random over 60 bits, a Held id (a collision) redrawn, at most
    /// <see cref="MaxRunUnitDraws"/> times. 0 when no liveness byte can be taken: the host answers <see cref="RegionLockResult.Unavailable"/>, or Held for every draw. A session
    /// with no id refuses every statement with 92 before it reads anything (<see cref="Statement"/>): it cannot own a partly received message, since without its byte every
    /// other run unit reads the owner as dead and drops the row (DESIGN-communication-facility.md §6 "A host whose locks answer Unavailable").</summary>
    private static ulong DrawRunUnit(RegionLocks regions)
    {
        for (int draw = 0; draw < MaxRunUnitDraws; draw++)
        {
            ulong id = BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(sizeof(ulong))) & ((1UL << 60) - 1);
            if (id == 0) continue;
            switch (regions.Hold(HostRegionLocks.McsRunUnitLivenessByte(id), exclusive: true, wait: false))
            {
                case RegionLockResult.Ok: return id;
                case RegionLockResult.Unavailable: return 0;
            }
        }
        return 0;
    }

    /// <summary>Whether the run unit <paramref name="owner"/> is still alive: its liveness byte is held by another endpoint. A host that cannot tell is read as alive, so nothing is dropped on a guess.</summary>
    private bool OwnerIsAlive(ulong owner) =>
        owner == _runUnit || _regions.HeldByAnother(HostRegionLocks.McsRunUnitLivenessByte(owner)) != RegionLockResult.Ok;

    /// <summary>The session's store of the MCS directory <paramref name="root"/>: <c>mcs.store</c> opened, created when absent, and shared with every other run unit's handle.
    /// Throws what the host throws for a file it refuses, which <see cref="MessageStore"/> answers with status 92.</summary>
    internal static McsStore Open(string root) =>
        new(root, HostFile.OpenConnectorStore(Path.Combine(root, MessageStore.StoreFile), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite));

    /// <summary>The engine's medium over the session's handle, through the one fault seam of the engine's media (<see cref="PageMediumObserver"/>).</summary>
    private IPageMedium Medium() => PageMediumObserver.Observe(new HandlePageMedium(_handle));

    /// <summary>One statement's transaction. Returns the action's status key, or the 9x this store reports instead of acting: 92 (no action) and 93 (the outcome is not known).</summary>
    internal string Statement(DefinitionFile definitions, Func<MessageStoreState, string> action)
    {
        if (_runUnit == 0 || HostRegionLocks.HoldStoreMutex(_regions) != RegionLockResult.Ok) return MessageStore.StoreRefused;
        try
        {
            return Held(definitions, action);
        }
        finally
        {
            _regions.Release(HostRegionLocks.StoreMutexByte);
        }
    }

    /// <summary>The statement, the store mutex held.</summary>
    private string Held(DefinitionFile definitions, Func<MessageStoreState, string> action)
    {
        if (!Enter()) return MessageStore.StoreRefused;
        var file = _file!;
        ulong claimed = file.LastClaim;
        var before = file.Meta.PrimeRoot;
        string? status = null;
        List<Written> written = [];
        try
        {
            var tables = new McsTables(new BTree(file, KeyOrders.Ordinal, before));
            if (!_prepared)
            {
                tables = Prepare(file, tables, definitions);
                claimed = file.LastClaim;
                before = file.Meta.PrimeRoot;
            }
            var state = new MessageStoreState(definitions.Parsed, tables, _runUnit, OwnerIsAlive);
            status = action(state);
            tables.FlushMeta();
            if (tables.Tree.HasPendingChanges || state.HasTransmissions)
            {
                var transmissions = state.Deliver();
                Transmit(tables, transmissions, written);
                tables.FlushMeta();
                if (StatementLanding.Complete(file, [new TreeChange(tables.Tree, TreeChange.PrimeRoot)]) != StoreStatus.Ok)
                    throw new McsRefusalException("the store did not land the statement");
            }
            _prepared = true;
            return status;
        }
        catch (Exception e) when (MessageStore.IsUnusable(e))
        {
            string answer = Refused(file, claimed, before, status);
            if (answer == MessageStore.StoreRefused) CutBack(written);
            return answer;
        }
    }

    /// <summary>A device a statement wrote to: its file, how long it was (the committed end) and whether it existed.</summary>
    private readonly record struct Written(string Device, long End, bool Existed);

    /// <summary>The statement's entry (§6 "The transaction", step 2): the run unit's first statement opens the store, creating it when <c>mcs.store</c> holds no bytes and otherwise
    /// running the adoption rule (2.2.1.6 R6); a later one reads page 2 once and drops the page cache when another run unit changed it (<see cref="PagedFile.Enter"/>).
    /// False when the store refuses its entry, which discards the session's <see cref="PagedFile"/>: the next statement opens it again.</summary>
    private bool Enter()
    {
        if (_file is { } live)
        {
            if (live.Enter() == StoreStatus.Ok) return true;
            _file = null;
            return false;
        }
        var medium = Medium();
        bool creating = RandomAccess.GetLength(_handle) == 0;
        var status = creating
            ? PagedFile.Create(medium, StoreKind.Message, coherent: true, KeyOrders.Ordinal, out var opened, out _)
            : PagedFile.Open(medium, writable: true, coherent: true, KeyOrders.Ordinal, out opened, messageStore: true);
        _file = status == StoreStatus.Ok ? opened : null;
        if (_file is null && creating) EmptyUncreatedStore();
        return _file is not null;
    }

    /// <summary>A creation the host refused part-way (a write or the sync that makes it durable) has written pages that name no committed meta, which the engine refuses to adopt, so
    /// left there it would refuse every later statement of every run unit. The creation ran under the store mutex, so no other run unit has seen those bytes, and the file goes back
    /// to the no-bytes state the next statement creates from. Best effort: a host that refuses this too is the same as a death during the creation, which the file system itself decides.</summary>
    private void EmptyUncreatedStore()
    {
        try { RandomAccess.SetLength(_handle, 0); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    /// <summary>The run unit's first statement, before its action (§6 "Seeding", "Devices"). A store that is new or whose definitions changed is seeded and the seeding LANDS
    /// ON ITS OWN, before the action runs and before any device is written: every device the definitions name without a <c>DeviceEnds</c> row takes the length its file has
    /// as its committed end (it is output already delivered, which a new store or a new definition must not destroy, and a dead statement's tail can then never be taken for
    /// it, because the row is durable before the statement writes: the seeding is PROMOTED, which syncs it to the medium, so a power loss returns the store to a state with the row or to one in which no device was written yet).
    /// A row of a device the definitions no longer name is retired, so the store stops claiming its file: a device renamed only by case is one file on a case-insensitive host, and its old row would
    /// cut the delivered output of the new name. Then every device the definitions name that is longer than its committed end is cut, so a reader of
    /// a device file sees a dead statement's tail only until then. A file at or below its committed end is never opened for write. The cut is best effort like <see cref="CutBack"/>:
    /// the positional protocol of <see cref="Transmit"/> does not depend on it, so a device the host refuses to cut never refuses a statement that does not transmit to it.
    /// Returns the tables on the tree as seeding left it.</summary>
    private McsTables Prepare(PagedFile file, McsTables tables, DefinitionFile definitions)
    {
        if (tables.Meta is not { } meta || !meta.DefinitionsHash.AsSpan().SequenceEqual(definitions.Hash))
        {
            McsSeeding.Seed(tables, definitions, RunUnit.Current.Clock.Now());
            var defined = definitions.Parsed.Devices.ToHashSet(StringComparer.Ordinal);
            var known = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (device, end) in tables.DeviceEnds())
            {
                if (defined.Contains(device)) known.Add(device);
                else
                {
                    CutDevice(device, end);   // a dead statement's tail is cut before the row goes, so it is never taken for delivered output
                    tables.RetireDeviceEnd(device);
                }
            }
            foreach (var device in defined.Where(d => !known.Contains(d)))
                tables.SetDeviceEnd(device, File.Exists(DeviceFile(device)) ? new FileInfo(DeviceFile(device)).Length : 0);
            tables.FlushMeta();
            if (StatementLanding.Complete(file, [new TreeChange(tables.Tree, TreeChange.PrimeRoot)]) != StoreStatus.Ok)
                throw new McsRefusalException("the store did not land the seeding");
            if (file.Promote() != StoreStatus.Ok)
                throw new McsRefusalException("the store did not promote the seeding");
            tables = new McsTables(new BTree(file, KeyOrders.Ordinal, file.Meta.PrimeRoot));
        }
        foreach (var (device, end) in tables.DeviceEnds()) CutDevice(device, end);
        return tables;
    }

    /// <summary>Cuts the device file to its committed end when it is longer. Best effort (see <see cref="Prepare"/>).</summary>
    private void CutDevice(string device, long end)
    {
        string path = DeviceFile(device);
        try
        {
            if (!File.Exists(path) || new FileInfo(path).Length <= end) return;
            using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            RandomAccess.SetLength(handle, end);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private string DeviceFile(string device) => Path.Combine(_root, device + ".out");

    /// <summary>⛔ A DEVICE, §6 "Devices": the file <c>&lt;device&gt;.out</c>, oriented to variable-length messages (A.2.6.3 GR2), so vertical positioning "is not applicable" and the
    /// MCS ignores it (GR12). Each device's messages are written positionally at its committed end, <c>DeviceEnds</c>, and the file's length is set to the end of what was
    /// written, so any tail a dead or refused statement left past the committed end is overwritten or cut; the device is flushed to the medium
    /// (<see cref="RandomAccess.FlushToDisk"/>), which orders it before page 2; and the committed end moves in the same store statement.</summary>
    private void Transmit(McsTables tables, IReadOnlyList<(string Device, StoredMessage Message)> transmissions, List<Written> written)
    {
        foreach (var device in transmissions.Select(t => t.Device).Distinct(StringComparer.Ordinal))
        {
            byte[] text = Encoding.UTF8.GetBytes(string.Concat(transmissions.Where(t => t.Device == device)
                .Select(t => string.Concat(t.Message.Segments) + Environment.NewLine)));
            string file = DeviceFile(device);
            bool existed = File.Exists(file);
            long length = existed ? new FileInfo(file).Length : 0;
            string? row = CommittedRow(tables, device);
            long end = Math.Min(row is null ? length : tables.DeviceEnd(row)!.Value, length);   // a file shorter than its committed end was changed outside the MCS; a device with no row (a run unit holding definitions a later seeding retired) keeps what its file holds, as seeding does
            written.Add(new Written(device, end, existed));
            using (var handle = File.OpenHandle(file, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
            {
                RandomAccess.Write(handle, text, end);
                RandomAccess.SetLength(handle, end + text.Length);
                RandomAccess.FlushToDisk(handle);
            }
            if (row is not null) tables.SetDeviceEnd(row, end + text.Length);   // a retired device gets no row back: seeding retired it, and a row it recreated would claim a file another row owns
        }
    }

    /// <summary>The <c>DeviceEnds</c> row that owns the file <paramref name="device"/> names, or null when none does. The row of the same spelling; else, on a host whose file names ignore
    /// case, the row of the spelling that is the same file, so a run unit still holding a definition that differs only by case from the stored one advances the stored row
    /// instead of leaving it short of the file (which the next run unit's cut loop would cut) or recreating a second row for one file.</summary>
    private string? CommittedRow(McsTables tables, string device)
    {
        if (tables.DeviceEnd(device) is not null) return device;
        if (!FileNamesIgnoreCase) return null;
        return tables.DeviceEnds().Select(r => r.Device).FirstOrDefault(d => string.Equals(d, device, StringComparison.OrdinalIgnoreCase));   // not a COBOL word: a file name, which the host's file system folds, not COBOL
    }

    private bool? _fileNamesIgnoreCase;

    /// <summary>Whether the MCS directory's file system treats names that differ only by case as one file, probed once with the store file's own name in the other case.</summary>
    private bool FileNamesIgnoreCase => _fileNamesIgnoreCase ??= File.Exists(Path.Combine(_root, MessageStore.StoreFile.ToUpperInvariant()));

    /// <summary>A statement that answers 92 took no action, so the devices it wrote to go back to their committed ends, and a device it created goes. Best effort: the
    /// positional protocol of <see cref="Transmit"/> does not depend on it (the next transmission to a device starts at its committed end and the run unit's first statement
    /// cuts a longer file), so a host that refuses this too leaves a dead statement's tail only until then.</summary>
    private void CutBack(List<Written> written)
    {
        foreach (var (device, end, existed) in written)
        {
            try
            {
                if (!existed)
                {
                    File.Delete(DeviceFile(device));
                    continue;
                }
                using var handle = File.OpenHandle(DeviceFile(device), FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
                RandomAccess.SetLength(handle, end);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    /// <summary>A statement the engine or the host refused: decide whether it landed, inside the mutex hold. Nothing was stamped when the generation claimed is the one the
    /// statement began with, and nothing landed. Otherwise the session's <see cref="PagedFile"/> is discarded and a READ-ONLY one adopts page 2 in memory (a writable open
    /// promotes at a fence above the stamp, so its generation is never the stamp). The statement landed when the adopted page 2 carries the stamped generation and names
    /// another prime tree than the one the statement began on; its <paramref name="status"/> is then its answer. A reopen that is refused too is 93.</summary>
    private string Refused(PagedFile file, ulong claimedBefore, PagePointer rootBefore, string? status)
    {
        ulong claim = file.LastClaim;
        _file = null;
        if (claim == claimedBefore) return MessageStore.StoreRefused;
        if (PagedFile.Open(Medium(), writable: false, coherent: true, KeyOrders.Ordinal, out var adopted, messageStore: true) != StoreStatus.Ok)
            return MessageStore.UnknownOutcome;
        bool landed = status is not null && adopted!.Meta.Generation == claim && adopted.Meta.PrimeRoot != rootBefore;
        if (landed) _prepared = true;
        return landed ? status! : MessageStore.StoreRefused;
    }

    /// <summary>The end of the session: the store promoted under the mutex (<see cref="PagedFile.Close"/>), the byte <see cref="HostRegionLocks.McsPresenceByte"/> and the endpoint
    /// given back, the handle closed. A refusal answers nothing: every statement already answered.</summary>
    public void Dispose()
    {
        try
        {
            if (_file is { } file && HostRegionLocks.HoldStoreMutex(_regions) == RegionLockResult.Ok)
            {
                try { file.Close(); }
                finally { _regions.Release(HostRegionLocks.StoreMutexByte); }
            }
        }
        catch (Exception e) when (MessageStore.IsUnusable(e)) { }
        finally
        {
            _file = null;
            try { _regions.Dispose(); }
            finally { _stream.Dispose(); }
        }
    }
}
