// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>Who holds the record lock a statement has run into (ISO §9.1.16) — the answer the RETRY discipline
/// needs as well as the conflict itself, because only a holder OUTSIDE the run unit can give the lock back while a
/// statement waits (§14.7.9.3; <c>RetryAttempt.HolderOutsideRunUnit</c>, kb/Work PB1163).</summary>
internal enum RecordLockHolder
{
    /// <summary>No other file connector holds it.</summary>
    None,
    /// <summary>Another file connector of THIS run unit holds it (this table says so).</summary>
    ThisRunUnit,
    /// <summary>A file connector of ANOTHER run unit holds it (the host's byte-range lock says so — kb/Work
    /// PB2660).</summary>
    AnotherRunUnit,
}

/// <summary>
/// The physical-file sharing/record-lock registry (ISO §9.1.15 sharing / §9.1.16 record locking; design D1 —
/// the sharing/locking statuses 51/52/61 are defined over <em>"another file connector"</em> (§9.1.13.9), and two
/// SELECTs bound to one resolved host path are two connectors over one physical file within one run unit). One
/// instance owned by the <see cref="FileRegistry"/>: the per-host open-connector table (sharing mode + open mode
/// — the Table-19 conflict inputs) and the record locks (owner connector per record id).
/// <para>⛔ THE TABLE IS KEYED BY THE PHYSICAL FILE, NOT BY ITS PATH (kb/Work PB3576; the twin of <see cref="StoreTable"/>'s rule,
/// kb/Work PB3533): a path names a physical file only at the instant it is asked. A state remembers the host's identity of the
/// file it stands for (<see cref="State.Identity"/>) and is found by it (<see cref="For"/>): a file removed or renamed away and
/// another made at its path outside the run unit — which a host that lets a file be unlinked under an open handle (Linux)
/// permits — gets a state of its own, so the new file meets none of the old file's Table 19 entries, file locks or record locks;
/// and the old file, reached under its new name, meets them all. The path finds a state only for a file that is absent or whose
/// identity the host cannot tell. A state stays what its connectors reach through their association
/// (<see cref="FileConnector.Physical"/>) and what a CLOSE under commit control kept (<see cref="KeptFile.Physical"/>).</para>
/// </summary>
internal sealed class PhysicalFileTable
{
    /// <summary>The live state of one physical file (<see cref="For"/>).</summary>
    internal sealed class State
    {
        /// <summary>The host's identity of the physical file this state stands for (<see cref="HostFile.IdentityOf"/>), or null
        /// while no connector's handle or the path has told it (an absent file, or a host that cannot tell). Set once, by
        /// <see cref="PhysicalFileTable.Learn"/>, which also makes the state findable by it.</summary>
        public PhysicalFileIdentity? Identity { get; internal set; }

        /// <summary>connector name → the (sharing mode, open mode) it is currently open under — Table 19's two
        /// conflict inputs. EVERY successful OPEN is here, not only the connectors that wrote a SHARING or LOCK
        /// MODE clause (§9.1.15's gate is written over the physical file, kb/Work PB321); a connector with
        /// neither clause nor phrase is recorded under the implementor default of its open mode
        /// (<see cref="FileRegistry.ImplementorDefaultSharing"/>).</summary>
        public readonly Dictionary<string, (FileSharing Sharing, FileOpenMode Mode)> Open =
            new(CobolNames.Comparer);
        /// <summary>record-id → the connector name that holds its lock.</summary>
        public readonly Dictionary<string, string> RecordLocks = new(StringComparer.Ordinal);

        /// <summary>record-id → the handle through which that lock is PUBLISHED to other run units
        /// (<see cref="HostRegionLocks.RecordLockByte"/>, kb/Work PB2660) — a subset of <see cref="RecordLocks"/>:
        /// a lock taken through a connector with no <c>RecordLocks</c>, or on a host that cannot carry the
        /// region lock, is this run unit's alone. Every release gives the published byte back.</summary>
        public readonly Dictionary<string, RegionLocks> Published = new(StringComparer.Ordinal);

        /// <summary>How many of <see cref="Published"/>'s locks each endpoint holds — the endpoint holds
        /// <see cref="HostRegionLocks.LockPresenceByte"/> while its count is above zero.</summary>
        public readonly Dictionary<RegionLocks, int> PublishedPerEndpoint = [];

        /// <summary>⭐ The SEQUENTIAL release-ordinal mint for this physical file (kb/Work PB739): the ordinal
        /// of the record most recently released to the operating environment through any sharing-active
        /// connector over this path, so the next release takes <c>++ReleasedOrdinal</c>.
        /// <para>It is here, on the ONE object that stands for the physical file, for the reason §14.9.51.4
        /// GR19 gives — <i>"If two or more file connectors for a sequential file add records by sharing the
        /// physical file after opening it in extend mode, the added records follow the records present in the
        /// physical file when it was opened"</i>: the records present are the records EVERY connector has
        /// released, so a per-connector counter cannot name them. Two shared <c>OPEN EXTEND</c> connectors each
        /// kept their own base plus their own count, so both called their first appended record ordinal 2 —
        /// and §9.1.16's <i>"While locked by a given file connector, a record is not accessible to another file
        /// connector"</i> is written over exactly that identity, so the locks landed on the wrong records.</para>
        /// <para>Seeded by a sharing-active <c>OPEN EXTEND</c> from the records already in the physical file
        /// (§14.9.51.4 GR18) and reset to 0 by a sharing-active <c>OPEN OUTPUT</c>, which truncates. The keyed
        /// organizations do not use it: their release identity is a key or an RRN minted from the shared record
        /// store, which is their physical file.</para>
        /// <para>⛔ ONLY THE FILE'S ONLY WRITER READS IT (kb/Work PB2692). The mint is one run unit's, and a writer
        /// whose file lock admits another writer may meet one in ANOTHER run unit, whose appends this mint never
        /// counts; such a writer numbers its release off the medium (<c>SequentialConnector.ReleaseRecord</c>), and
        /// so does every writer Table 19 admits beside it, so this field is neither seeded nor read for them.</para></summary>
        public long ReleasedOrdinal;

        /// <summary>⭐ The RELEASE GENERATION of this physical file (kb/Work PB753): a count of the logical
        /// records that have been released to the operating environment THROUGH THIS PATH and have reached the
        /// physical file. A reader records the value its read-ahead was filled at and re-anchors when they
        /// differ, so it can never serve an image a sibling connector has since replaced.
        /// <para>It is the READ-SIDE twin of <see cref="ReleasedOrdinal"/>, and it counts BOTH verbs that
        /// release, because the standard gives them the same sentence: §14.9.51.4 GR12 — <i>"The successful
        /// execution of a WRITE statement releases a logical record to the operating environment"</i> — and
        /// §14.9.35.4 GR4 — <i>"The successful execution of the REWRITE statement releases a logical record to
        /// the operating environment"</i>. What a READ owes against that is §14.9.30.4 GR21 c) and d): the
        /// record selected is <i>"the first existing record IN THE PHYSICAL FILE whose relative key number is
        /// greater than the file position indicator"</i> and it is that record that <i>"is made available in
        /// the record area"</i> — the physical file as it stands at the READ, never a snapshot of what it said
        /// when a buffer was filled.</para>
        /// <para>⛔ IT COUNTS RELEASES THAT HAVE REACHED THE FILE, not WRITE statements. A connector whose
        /// §9.1.15 file lock admits no other writer keeps its buffered writer and flushes at CLOSE (see
        /// <c>SequentialConnector.ReleaseRecord</c>), so its records are not in the physical file yet and a
        /// reader told to re-anchor for them would read a half-written frame instead of a stale whole one.
        /// The generation moves exactly where the bytes do.</para>
        /// <para>The keyed organizations do not use it, for the reason that names the fix: their record images
        /// live in the ONE <see cref="StoreTable"/> store for the path (kb/Work PB143), so a sibling's
        /// REWRITE is visible to every attached connector the instant it happens — measured, not assumed.
        /// This is the same rule for the organization whose medium is the host file itself.</para></summary>
        public long ReleaseGeneration;
    }

    /// <summary>The per-file-connector record-lock ceiling (§12.4.5.9.4 GR7 — implementor max, ≥15) → status 54.</summary>
    private const int ConnectorLockMax = 15;
    /// <summary>The per-run-unit record-lock ceiling (§12.4.5.9.4 GR7 — implementor max, ≥255) → status 53.</summary>
    private const int RunUnitLockMax = 255;

    /// <summary>The state of a path whose file is absent, or whose identity the host cannot tell (kb/Work PB2748: the path is
    /// compared under the host's case rule).</summary>
    private readonly Dictionary<string, State> _byHost = new(HostFile.PhysicalFileComparer);

    /// <summary>The state of every physical file the host could tell, by the file, whatever it is named.</summary>
    private readonly Dictionary<PhysicalFileIdentity, State> _byIdentity = [];

    /// <summary>⛔ THE STATE OF THE PHYSICAL FILE <paramref name="host"/> NAMES AT THIS INSTANT, whose host identity is
    /// <paramref name="identity"/> (<see cref="HostFile.IdentityAt"/>; null: the path names no file, or the host cannot tell),
    /// created on first reference. A file the table knows by its identity has ITS state, under whatever name; a file it does not
    /// know takes the state the path held for it while it was absent, or a new one. A path that names no file has the state of
    /// the absent path (one the table never learned a file for: the file this state's file was is no longer there).</summary>
    public State For(string host, PhysicalFileIdentity? identity)
    {
        identity = null;   // TEMP PROBE: the old path-only behaviour
        if (identity is { } id)
        {
            if (_byIdentity.TryGetValue(id, out var known)) return known;
            var named = _byHost.TryGetValue(host, out var held) && held.Identity is null ? held : _byHost[host] = new State();
            Learn(named, id);
            return named;
        }
        if (_byHost.TryGetValue(host, out var st) && st.Identity is null) return st;
        return _byHost[host] = new State();
    }

    /// <summary>Record that <paramref name="st"/> stands for the physical file of <paramref name="identity"/> (null: the host
    /// cannot tell) unless it already knows its file, and make it findable by it. A connector's open handle tells it once the
    /// OPEN has made the file (<c>FileRegistry.SharedOpenAttempt</c>), which an OPEN OUTPUT of an absent path creates. A file the
    /// table already holds a state for stays with that state (an identity two states claim cannot arise from one host's answers
    /// unless a file was made while its path was probed: the first state keeps it).</summary>
    public void Learn(State st, PhysicalFileIdentity? identity)
    {
        if (st.Identity is not null || identity is not { } id) return;
        if (_byIdentity.TryAdd(id, st)) st.Identity = id;
    }

    /// <summary>Drop every physical-file state (run-unit start hygiene).</summary>
    public void Clear()
    {
        _byHost.Clear();
        _byIdentity.Clear();
    }

    /// <summary>Acquire a lock on <paramref name="recId"/> for connector <paramref name="name"/>; re-locking a
    /// record the connector already holds is idempotent (§9.1.16: <i>"A locked record may be re-accessed by the same
    /// file connector that holds the lock"</i>). Enforces the connector ceiling (54) and the run-unit ceiling (53).
    /// Returns 00 on grant.
    /// <para>⛔ AND PUBLISHES IT to every other run unit through <paramref name="holder"/>'s
    /// <c>RecordLocks</c> (kb/Work PB2660): §9.1.16 makes a locked record inaccessible to a file connector
    /// <i>"in the same or a different run unit"</i>, and this table is one run unit's. A byte another run unit
    /// already holds refuses the grant with '51' — the host's answer is the arbiter between run units, exactly as
    /// this table is within one.</para></summary>
    public string LockRecord(State st, string name, string recId, FileConnector? holder)
    {
        if (st.RecordLocks.TryGetValue(recId, out var owner))
            return CobolNames.Same(owner, name)
                ? FileStatusCode.Success                     // self re-lock (§9.1.16) — idempotent
                : FileStatusCode.RecordLocked;               // 51 — another connector holds it
        // The commit posture's READ locks have no ceiling but the host's (DOC-A.1-154/155; DESIGN-runtime-library.md
        // 2.2.1.10): §12.4.5.9.4 GR7 lets the implementor set the maximum, and D.2.5's deferred commits make it unbounded.
        if (holder is not KeyedConnector { CommitControlled: true })
        {
            int mine = 0;
            foreach (var o in st.RecordLocks.Values)
                if (CobolNames.Same(o, name)) mine++;
            if (mine >= ConnectorLockMax) return FileStatusCode.ConnectorLockLimit;   // 54 (§12.4.5.9.4 GR7)
            if (TotalRunUnitLocks() >= RunUnitLockMax) return FileStatusCode.RunUnitLockLimit;   // 53 (§12.4.5.9.4 GR7)
        }
        if (holder?.RecordLocks is { } regions)
        {
            switch (Publish(regions, HostRegionLocks.RecordLockByte(recId)))
            {
                case RegionLockResult.Held: return FileStatusCode.RecordLocked;   // 51 — another run unit holds it
                case RegionLockResult.Ok: Published(st, recId, regions); break;
            }
        }
        st.RecordLocks[recId] = name;
        return FileStatusCode.Success;
    }

    /// <summary>Take the record's byte for this run unit: exclusively wherever the host allows it — through a
    /// writable handle, and through any handle on Windows; through a read-only handle where an exclusive lock needs a
    /// writable one (an <c>OPEN INPUT</c> connector on Linux, <see cref="RegionLocks.CanHoldExclusive"/>)
    /// as a SHARED hold that is then TESTED, and given back if another handle holds the byte too — publish, then test,
    /// the <see cref="RunUnitFileLock"/> pattern, so two run units can never both believe they hold it.</summary>
    private static RegionLockResult Publish(RegionLocks regions, long recordByte)
    {
        bool exclusive = regions.CanHoldExclusive;
        var taken = regions.Hold(recordByte, exclusive, wait: false);
        if (taken != RegionLockResult.Ok || exclusive) return taken;
        if (regions.HeldByAnother(recordByte) != RegionLockResult.Held) return taken;
        regions.Release(recordByte);
        return RegionLockResult.Held;
    }

    /// <summary>Record a published lock, and announce the first one of an endpoint on the presence byte.</summary>
    private static void Published(State st, string recId, RegionLocks regions)
    {
        st.Published[recId] = regions;
        int held = st.PublishedPerEndpoint.GetValueOrDefault(regions);
        st.PublishedPerEndpoint[regions] = held + 1;
        if (held == 0)
            _ = regions.Hold(HostRegionLocks.LockPresenceByte, exclusive: false, wait: false);
    }

    /// <summary>Whether a file connector of ANOTHER run unit holds any record lock on the physical file
    /// <paramref name="asker"/>'s handle is open on — the presence byte, one host question (kb/Work PB2660).</summary>
    public static bool AnotherRunUnitHoldsLocks(FileConnector asker) =>
        asker.RecordLocks is { } regions
        && regions.HeldByAnother(HostRegionLocks.LockPresenceByte) == RegionLockResult.Held;

    /// <summary>⛔ THE ONE QUESTION "is <paramref name="recId"/> locked by a file connector OTHER than
    /// <paramref name="name"/>?" — and by whom. This run unit's table answers for this run unit; for a record it
    /// does not list, the host answers for every other run unit, asked through <paramref name="asker"/>'s
    /// <c>RecordLocks</c> (§9.1.16, <i>"in the same or a different run unit"</i>; kb/Work PB2660). A connector
    /// with no such endpoint can only learn about its own run unit.</summary>
    public static RecordLockHolder HolderOf(State st, string name, string recId, FileConnector? asker)
    {
        if (st.RecordLocks.TryGetValue(recId, out var owner))
            return CobolNames.Same(owner, name)
                ? RecordLockHolder.None : RecordLockHolder.ThisRunUnit;
        return asker?.RecordLocks is { } regions
            && regions.HeldByAnother(HostRegionLocks.RecordLockByte(recId)) == RegionLockResult.Held
            ? RecordLockHolder.AnotherRunUnit
            : RecordLockHolder.None;
    }

    /// <summary>Release every record lock held by <paramref name="name"/> on <paramref name="st"/> (UNLOCK, CLOSE).</summary>
    public static void ReleaseAllForConnector(State st, string name)
    {
        var mine = st.RecordLocks.Where(kv => CobolNames.Same(kv.Value, name))
            .Select(kv => kv.Key).ToList();
        foreach (var k in mine) Unlock(st, k);
    }

    /// <summary>⛔ THE RELEASE OF EVERY RECORD LOCK PUBLISHED THROUGH <paramref name="endpoint"/> — out of this run unit's
    /// table and given back to every other run unit — for the one owner of a lock that outlives its connector's CLOSE: the
    /// endpoint a CLOSE under commit control kept (§14.9.6.4 GR9, §9.1.16; <c>KeyedConnector.ReleaseKept</c>). It names the
    /// endpoint and not the connector, because the endpoint is what carries the lock to the other run units and the
    /// connector's registry key is not its own to know. A lock that was never published (a host that cannot carry it) is
    /// this run unit's table alone and is the registry's, by name (<see cref="ReleaseAllForConnector"/>).</summary>
    public static void ReleaseThrough(State st, RegionLocks endpoint)
    {
        foreach (var recId in PublishedThrough(st, endpoint)) Unlock(st, recId);
    }

    /// <summary>⛔ MOVE every record lock published through <paramref name="from"/> to <paramref name="to"/>, the endpoint of the
    /// handle a commit-controlled connector's re-OPEN took after its CLOSE kept the first (§14.9.6.4 GR9: the record locks
    /// of a file in an APPLY COMMIT clause are not released by the CLOSE; §9.1.16: only COMMIT and ROLLBACK release them): the
    /// kept handle is given back when the re-OPEN has succeeded, and every byte it holds goes with it, so the locks must
    /// already be the new handle's. The caller holds the store mutex, which every other run unit's lock-taking statement
    /// takes too (<c>KeyedConnector.BeginStatement</c>), so no other run unit takes a byte in the instant it is free; a byte
    /// it did take beforehand (the share-mode window of a kept handle given back early) leaves the lock recorded in this
    /// table alone, as a lock a host cannot carry is. The table entries stay: only the endpoint they are published through
    /// changes. <paramref name="from"/> may be a disposed endpoint (releasing through it is then a no-op).</summary>
    public static void Rehome(State st, RegionLocks from, RegionLocks to)
    {
        if (ReferenceEquals(from, to)) return;
        Republish(st, Unpublish(st, from), to);
    }

    /// <summary>⛔ PUBLISH AGAIN, through <paramref name="to"/>, the record locks <paramref name="ids"/> names that this run
    /// unit's table still holds and no endpoint publishes: the second half of <see cref="Rehome"/>, and what a re-OPEN's
    /// unsuccessful end does with the locks <see cref="Unpublish"/> took off its own handle once the handle a CLOSE under
    /// commit control kept is taken again (<c>KeyedConnector.RetakeKeptHandle</c>; §14.9.27.4 GR25, <i>"If the execution of
    /// the OPEN statement is unsuccessful, the file is not affected"</i>; §14.9.6.4 GR9). A byte another run unit took
    /// meanwhile leaves its lock recorded in this table alone, as a lock a host cannot carry is.</summary>
    public static void Republish(State st, IEnumerable<string> ids, RegionLocks to)
    {
        foreach (var recId in ids)
            if (st.RecordLocks.ContainsKey(recId) && !st.Published.ContainsKey(recId)
                && Publish(to, HostRegionLocks.RecordLockByte(recId)) == RegionLockResult.Ok)
                Published(st, recId, to);
    }

    /// <summary>⛔ GIVE BACK TO THE HOST every record lock published through <paramref name="endpoint"/> and leave this run
    /// unit's table as it is: the locks stay held by their connectors, unpublished, as a lock a host cannot carry is. It is
    /// the half of <see cref="Rehome"/> that frees the bytes, and what an unsuccessful OPEN does with the locks a CLOSE under
    /// commit control kept, when the handle it took over goes (§14.9.27.4 GR25, <i>"If the execution of the OPEN statement is
    /// unsuccessful, the file is not affected"</i>; §14.9.6.4 GR9): the handle goes, the locks do not - only the COMMIT or the
    /// ROLLBACK ends them - and the kept handle, once taken again, publishes them again (<see cref="Republish"/>). Returns the
    /// record ids that were published.</summary>
    public static List<string> Unpublish(State st, RegionLocks endpoint)
    {
        var ids = PublishedThrough(st, endpoint);
        if (ids.Count == 0) return ids;
        foreach (var recId in ids)
        {
            endpoint.Release(HostRegionLocks.RecordLockByte(recId));
            st.Published.Remove(recId);
        }
        st.PublishedPerEndpoint.Remove(endpoint);
        endpoint.Release(HostRegionLocks.LockPresenceByte);
        return ids;
    }

    /// <summary>The record ids whose locks are published through <paramref name="endpoint"/>.</summary>
    private static List<string> PublishedThrough(State st, RegionLocks endpoint)
    {
        var ids = new List<string>();
        if (!st.PublishedPerEndpoint.ContainsKey(endpoint)) return ids;   // the common case: it published none
        foreach (var (recId, through) in st.Published)
            if (ReferenceEquals(through, endpoint)) ids.Add(recId);
        return ids;
    }

    /// <summary>Release a single record lock a connector holds (the LOCK MODE single-lock discipline, §12.4.5.9.4 GR6).</summary>
    public static void ReleaseSingle(State st, string name, string recId)
    {
        if (st.RecordLocks.TryGetValue(recId, out var owner)
            && CobolNames.Same(owner, name))
            Unlock(st, recId);
    }

    /// <summary>The ONE release of a record lock: out of this run unit's table, and its published byte given back to
    /// every other run unit.</summary>
    private static void Unlock(State st, string recId)
    {
        st.RecordLocks.Remove(recId);
        if (!st.Published.Remove(recId, out var regions)) return;
        regions.Release(HostRegionLocks.RecordLockByte(recId));
        int left = st.PublishedPerEndpoint[regions] - 1;
        if (left > 0) { st.PublishedPerEndpoint[regions] = left; return; }
        st.PublishedPerEndpoint.Remove(regions);
        regions.Release(HostRegionLocks.LockPresenceByte);
    }

    /// <summary>Release every record lock <paramref name="name"/> holds EXCEPT one on <paramref name="keepId"/>
    /// — the single-lock begin-of-statement discipline (§12.4.5.9.4 GR6): a REWRITE/DELETE releases a self-lock
    /// held on a record OTHER than its target at the beginning of execution (§14.9.35 GR12a2 / §14.9.10 GR7a2);
    /// the target's own lock survives to the completion rules.</summary>
    public static void ReleaseAllExcept(State st, string name, string keepId)
    {
        var mine = st.RecordLocks
            .Where(kv => CobolNames.Same(kv.Value, name)
                && !string.Equals(kv.Key, keepId, StringComparison.Ordinal))
            .Select(kv => kv.Key).ToList();
        foreach (var k in mine) Unlock(st, k);
    }

    /// <summary>Would acquiring one NEW lock (a record <paramref name="name"/> does not already hold) exceed a
    /// §12.4.5.9.4 GR7 ceiling? Returns 54/53/00 WITHOUT acquiring — the pre-flight for the mutating verbs, whose
    /// statement must be unsuccessful with the operation NOT performed (§14.9.51 GR15 / §14.9.35 GR14) rather
    /// than lock-fail after the record already changed.</summary>
    public string PreflightNewLock(State st, string name)
    {
        int mine = 0;
        foreach (var o in st.RecordLocks.Values)
            if (CobolNames.Same(o, name)) mine++;
        if (mine >= ConnectorLockMax) return FileStatusCode.ConnectorLockLimit;     // 54 (§12.4.5.9.4 GR7)
        if (TotalRunUnitLocks() >= RunUnitLockMax) return FileStatusCode.RunUnitLockLimit;   // 53 (§12.4.5.9.4 GR7)
        return FileStatusCode.Success;
    }

    private int TotalRunUnitLocks()
    {
        int n = 0;
        foreach (var st in _byIdentity.Values) n += st.RecordLocks.Count;
        foreach (var st in _byHost.Values)
            if (st.Identity is null) n += st.RecordLocks.Count;   // the others are counted above
        return n;
    }
}
