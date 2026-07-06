// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// The COBOL-2002 file-sharing / record-locking machinery (Phase 4d / M2-FILE-1; ISO/IEC 1989:2023 §9.1.15
/// sharing, §9.1.16 record locking, §14.9.27 OPEN sharing, §14.9.47 UNLOCK, §14.7.9 RETRY). Design decision D1
/// (docs/PHASE4_RECONCILIATION.md §M2-FILE-1): the sharing/locking status codes 51/52/61 are defined over
/// <em>"another file connector"</em> (§9.1.13.9), NOT another run unit — and two <c>SELECT</c>s bound to the same
/// resolved host path are two distinct connectors over one physical file, opened concurrently in one
/// single-threaded run unit. So the machinery is built <b>real</b>: a physical-file registry keyed by host path
/// carries the open connectors (their sharing modes → Table-19 open conflicts, status 61) and the record locks
/// (owner connector per record → status 51 for another connector's access). UNLOCK releases a connector's locks;
/// the RETRY loop re-checks n+1 times (SECONDS/FOREVER cannot block productively in one run unit, so an
/// unsatisfiable conflict deadlock-bails to 52 — never a real sleep).
/// </summary>
/// <remarks>
/// STAGED single-run-unit residue (each with a loud guard, per the D1 design): cross-run-unit / cross-OS-process
/// locks never arise (the registry is process-local by construction); RETRY FOR n SECONDS / FOREVER never sleep
/// (no external releaser ⇒ 52); APPLY COMMIT is unimplemented (its exclusion SRs are vacuous); the implementor
/// default for a connector without a SHARING/LOCK-MODE clause is legacy exclusive behavior <b>outside</b> this
/// registry (a connector is "sharing-active" only once <see cref="RegisterSharing"/> is emitted for it), which
/// keeps the whole pre-2002 corpus byte-invariant.
/// </remarks>
public static partial class CobolFile
{
    /// <summary>A connector's declared sharing posture (from its SELECT's SHARING / LOCK MODE clauses), registered
    /// at program start for every file that carries either clause.</summary>
    private readonly record struct ConnectorShare(string Host, FileSharing Sharing, FileLockMode LockMode, bool Multiple);

    /// <summary>The live state of one physical file (keyed by resolved host path): which connectors have it open
    /// (with what sharing mode + open mode — the Table-19 conflict inputs), and which records each connector has
    /// locked (§9.1.16).</summary>
    private sealed class PhysicalFileState
    {
        public readonly Dictionary<string, (FileSharing Sharing, FileOpenMode Mode)> Open =
            new(StringComparer.OrdinalIgnoreCase);
        /// <summary>record-id → the connector name that holds its lock.</summary>
        public readonly Dictionary<string, string> RecordLocks = new(StringComparer.Ordinal);
    }

    /// <summary>The per-connector declared sharing/lock metadata (a connector is "sharing-active" iff present).</summary>
    private static readonly Dictionary<string, ConnectorShare> ConnectorShares = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The physical-file registry, keyed by resolved host path.</summary>
    private static readonly Dictionary<string, PhysicalFileState> Physical = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The per-file-connector record-lock ceiling (§12.4.5.9 GR7 — implementor max, ≥15) → status 54.</summary>
    private const int ConnectorLockMax = 15;
    /// <summary>The per-run-unit record-lock ceiling (§12.4.5.9 GR7 — implementor max, ≥255) → status 53.</summary>
    private const int RunUnitLockMax = 255;

    /// <summary>Reset the sharing/locking registries (called from <see cref="Init"/> at program start).</summary>
    private static void LocksInit()
    {
        ConnectorShares.Clear();
        Physical.Clear();
    }

    /// <summary>Register a SELECTed file's declared SHARING / LOCK MODE (emitted at program start, right after
    /// <see cref="Register"/>/<c>RegisterRelative</c>/<c>RegisterIndexed</c>, only for a file that carries either
    /// clause). Marks the connector sharing-active so its OPEN routes through the physical-file registry.</summary>
    public static void RegisterSharing(string name, FileSharing sharing, FileLockMode lockMode, bool multiple)
    {
        string host = HostPathOf(name);
        ConnectorShares[name] = new ConnectorShare(host, sharing, lockMode, multiple);
    }

    /// <summary>True when <paramref name="name"/> participates in the sharing subsystem (has a SHARING/LOCK MODE
    /// clause). A non-sharing-active connector uses the legacy open/read path untouched.</summary>
    private static bool IsSharingActive(string name) => ConnectorShares.ContainsKey(name);

    /// <summary>OPEN with an explicit SHARING override and/or a RETRY phrase (§14.9.27) — the emitter's entry point
    /// when the OPEN statement itself carries a sharing/retry phrase. <paramref name="hasSharingOverride"/> gates
    /// <paramref name="sharingOverride"/> (a struct can't be null); the RETRY loop re-attempts the open on a 61
    /// conflict (which, in one run unit, cannot clear — so n TIMES exhausts to 61 and SECONDS/FOREVER bail to 52).</summary>
    public static void OpenShared(string name, FileOpenMode mode, bool hasSharingOverride, FileSharing sharingOverride,
        FileRetryKind retryKind, int retryAmount)
    {
        // A sharing/retry phrase on the OPEN makes the connector sharing-active even without a SELECT clause.
        if (!ConnectorShares.ContainsKey(name))
            ConnectorShares[name] = new ConnectorShare(HostPathOf(name), FileSharing.AllOther, FileLockMode.None, false);
        FileSharing? ov = hasSharingOverride ? sharingOverride : null;
        string status = RetryLoop(() => SharedOpenAttempt(name, mode, ov), retryKind, retryAmount);
        // SharedOpenAttempt already set the connector status on the terminal attempt; a deadlock-bail overrides it.
        if (status == FileStatusCode.Deadlock) SetStatusOf(name, FileStatusCode.Deadlock);
    }

    /// <summary>The sharing-aware OPEN body (used both by <see cref="OpenShared"/> and by the plain
    /// OpenInput/Output/Extend/IO path for a connector that is sharing-active via its SELECT clause). Returns the
    /// resulting I-O status; on a Table-19 conflict returns 61 without opening the connector.</summary>
    private static string SharedOpenAttempt(string name, FileOpenMode mode, FileSharing? sharingOverride)
    {
        if (!ResolveConnector(name, out var c)) return FileStatusCode.PermanentError;
        if (Locked.Contains(name)) { c.SetStatus(FileStatusCode.FileLocked); return FileStatusCode.FileLocked; }  // ≤2014 CLOSE WITH LOCK
        FileSharing sharing = sharingOverride ?? ConnectorShares[name].Sharing;
        var st = PhysicalFor(c.Host);
        foreach (var (other, existing) in st.Open)
        {
            if (string.Equals(other, name, StringComparison.OrdinalIgnoreCase)) continue;
            if (Conflicts(existing, (sharing, mode)))
            {
                c.SetStatus(FileStatusCode.FileSharingConflict);   // 61 — §9.1.13.9
                return FileStatusCode.FileSharingConflict;
            }
        }
        c.Open(mode);
        if (c.IsOpen()) st.Open[name] = (sharing, mode);   // register only a successful open
        return c.Status();
    }

    /// <summary>Table-19 open-conflict classification (§9.1.13.9 sub-cases a–e): an existing open connector
    /// <paramref name="ex"/> and an incoming OPEN <paramref name="inc"/> conflict when either side is exclusive
    /// (NO OTHER) or a READ-ONLY sharer meets a non-INPUT opener.</summary>
    public static bool Conflicts((FileSharing Sharing, FileOpenMode Mode) ex, (FileSharing Sharing, FileOpenMode Mode) inc)
    {
        if (ex.Sharing == FileSharing.NoOther || inc.Sharing == FileSharing.NoOther) return true;       // (a)/(b)
        if (ex.Sharing == FileSharing.ReadOnly && inc.Mode != FileOpenMode.Input) return true;          // (c)
        if (inc.Sharing == FileSharing.ReadOnly && ex.Mode != FileOpenMode.Input) return true;          // (d)
        return false;                                                                                    // (e) ALL OTHER
    }

    /// <summary>Record-lock governance for a just-completed keyed READ (the emitter calls this right after the
    /// physical read, before the success block, for any sharing-active file). Given the status the read produced
    /// and the READ's explicit lock phrase, it (1) fails the read with 51 if another connector holds the record's
    /// lock — unless IGNORING LOCK — honoring the RETRY loop; (2) acquires the lock when WITH LOCK is specified or
    /// the LOCK MODE is AUTOMATIC (enforcing the 53/54 ceilings). Returns the effective status (and stores it on
    /// the connector); a non-success input status passes through unchanged.</summary>
    public static string ReadLockGovern(string name, string statusJustRead, FileRecordLock phrase,
        FileRetryKind retryKind, int retryAmount)
    {
        if (statusJustRead.Length == 0 || statusJustRead[0] != '0') return statusJustRead;   // an unsuccessful read: no locking
        if (!ConnectorShares.TryGetValue(name, out var meta)) return statusJustRead;         // not sharing-active
        string recId = CurrentRecordId(name);
        if (recId.Length == 0) return statusJustRead;   // no record identity for this organization (sequential — residue)
        var st = PhysicalFor(meta.Host);

        if (phrase != FileRecordLock.Ignoring)
        {
            // Another connector's lock blocks the read (§9.1.16 :11752). RETRY re-checks; in one run unit the
            // holder cannot release mid-loop, so n TIMES exhausts to 51 and SECONDS/FOREVER bail to 52 (GR4a).
            string conflict = RetryLoop(
                () => IsLockedByOther(st, name, recId) ? FileStatusCode.RecordLocked : FileStatusCode.Success,
                retryKind, retryAmount);
            if (conflict != FileStatusCode.Success) { SetStatusOf(name, conflict); return conflict; }
        }

        bool wantLock = phrase switch
        {
            FileRecordLock.Ignoring => false,
            FileRecordLock.WithNoLock => false,
            FileRecordLock.WithLock => true,
            _ => meta.LockMode == FileLockMode.Automatic,   // no phrase: AUTOMATIC auto-locks (GR4), MANUAL/None do not
        };
        if (wantLock)
        {
            // §12.4.5.9 GR6 — single lock discipline: unless WITH LOCK ON MULTIPLE was declared, a connector holds
            // at most one record lock, so acquiring a new one releases the prior. (The finer GR6 rule that ANY
            // non-START I-O releases the single lock even without a new acquire is documented residue.)
            if (!meta.Multiple) ReleaseAllForConnector(st, name);
            string acq = LockRecord(st, name, recId);
            if (acq != FileStatusCode.Success) { SetStatusOf(name, acq); return acq; }
        }
        return statusJustRead;
    }

    /// <summary>UNLOCK file [RECORD[S]] (§14.9.47 GR1): release every record lock this connector holds on the file
    /// and set status 00; an UNLOCK of a file not open is status 42 (§9.1.13 / :11579). The <c>records</c> flag is
    /// accepted for both the RECORD and RECORDS spellings (semantically identical — GR1 releases all).</summary>
    public static void Unlock(string name, bool records)
    {
        _ = records;
        if (!ResolveConnector(name, out var c)) return;
        if (!c.IsOpen()) { c.SetStatus(FileStatusCode.FileNotOpen); return; }
        if (Physical.TryGetValue(c.Host, out var st)) ReleaseAllForConnector(st, name);
        c.SetStatus(FileStatusCode.Success);
    }

    // ── The record-lock primitives (also the CobolFileLockTests surface) ─────────────────────────────────────

    /// <summary>Acquire a lock on <paramref name="recId"/> for connector <paramref name="name"/>; re-locking a
    /// record the connector already holds is idempotent (GR8 self-access). Enforces the connector ceiling (54) and
    /// the run-unit ceiling (53). Returns 00 on grant.</summary>
    public static string LockRecord(string name, string recId)
    {
        string host = HostPathOf(name);
        return LockRecord(PhysicalFor(host), name, recId);
    }

    private static string LockRecord(PhysicalFileState st, string name, string recId)
    {
        if (st.RecordLocks.TryGetValue(recId, out var owner))
            return string.Equals(owner, name, StringComparison.OrdinalIgnoreCase)
                ? FileStatusCode.Success                     // self re-lock (GR8) — idempotent
                : FileStatusCode.RecordLocked;               // 51 — another connector holds it
        int mine = 0;
        foreach (var o in st.RecordLocks.Values)
            if (string.Equals(o, name, StringComparison.OrdinalIgnoreCase)) mine++;
        if (mine >= ConnectorLockMax) return FileStatusCode.ConnectorLockLimit;   // 54 (GR7)
        if (TotalRunUnitLocks() >= RunUnitLockMax) return FileStatusCode.RunUnitLockLimit;   // 53 (GR7)
        st.RecordLocks[recId] = name;
        return FileStatusCode.Success;
    }

    /// <summary>True when <paramref name="recId"/> is locked by a connector OTHER than <paramref name="name"/>.</summary>
    public static bool IsLockedByOther(string name, string recId) =>
        IsLockedByOther(PhysicalFor(HostPathOf(name)), name, recId);

    private static bool IsLockedByOther(PhysicalFileState st, string name, string recId) =>
        st.RecordLocks.TryGetValue(recId, out var owner)
        && !string.Equals(owner, name, StringComparison.OrdinalIgnoreCase);

    /// <summary>Release every record lock held by <paramref name="name"/> on its physical file (UNLOCK, CLOSE).</summary>
    public static void ReleaseAllForConnector(string name)
    {
        if (Physical.TryGetValue(HostPathOf(name), out var st)) ReleaseAllForConnector(st, name);
    }

    private static void ReleaseAllForConnector(PhysicalFileState st, string name)
    {
        var mine = st.RecordLocks.Where(kv => string.Equals(kv.Value, name, StringComparison.OrdinalIgnoreCase))
            .Select(kv => kv.Key).ToList();
        foreach (var k in mine) st.RecordLocks.Remove(k);
    }

    /// <summary>Release a single record lock a connector holds (the LOCK MODE single-lock discipline, GR6 — any I-O
    /// except START releases the prior lock before acquiring the next).</summary>
    public static void ReleaseSingle(string name, string recId)
    {
        if (Physical.TryGetValue(HostPathOf(name), out var st)
            && st.RecordLocks.TryGetValue(recId, out var owner)
            && string.Equals(owner, name, StringComparison.OrdinalIgnoreCase))
            st.RecordLocks.Remove(recId);
    }

    private static int TotalRunUnitLocks()
    {
        int n = 0;
        foreach (var st in Physical.Values) n += st.RecordLocks.Count;
        return n;
    }

    // ── The RETRY loop (§14.7.9) ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Evaluate <paramref name="attempt"/> under the RETRY discipline. <see cref="FileRetryKind.None"/>
    /// is a single attempt; <see cref="FileRetryKind.Times"/> retries up to <paramref name="amount"/> extra times
    /// (n+1 total, GR1); SECONDS/FOREVER cannot block productively in one run unit, so a still-failing attempt
    /// deadlock-bails to status 52 (GR2/GR3 + §9.1.13.8 impl-detected deadlock). Never sleeps.</summary>
    public static string RetryLoop(Func<string> attempt, FileRetryKind kind, int amount)
    {
        string s = attempt();
        if (s == FileStatusCode.Success) return s;
        switch (kind)
        {
            case FileRetryKind.None:
                return s;
            case FileRetryKind.Times:
                for (int i = 0; i < amount && s != FileStatusCode.Success; i++) s = attempt();
                return s;
            default:   // Seconds / Forever — a single re-check, then deadlock-bail (no external releaser exists)
                s = attempt();
                return s == FileStatusCode.Success ? s : FileStatusCode.Deadlock;   // 52
        }
    }

    // ── Connector resolution across the three registries ─────────────────────────────────────────────────────

    /// <summary>A uniform view of a connector regardless of organization (sequential / relative / indexed).</summary>
    private readonly record struct ConnectorRef(
        string Host, Func<bool> IsOpen, Func<string> Status, Action<string> SetStatus, Action<FileOpenMode> Open);

    private static bool ResolveConnector(string name, out ConnectorRef c)
    {
        if (Files.TryGetValue(name, out var sq))
        {
            c = new ConnectorRef(sq.HostPath, () => sq.IsOpen, () => sq.Status, sq.SetStatus, m => sq.Open(m));
            return true;
        }
        if (RelativeFiles.TryGetValue(name, out var r))
        {
            c = new ConnectorRef(r.HostPath, () => r.IsOpen, () => r.Status, r.SetStatus, m => r.Open(m));
            return true;
        }
        if (IndexedFiles.TryGetValue(name, out var ix))
        {
            c = new ConnectorRef(ix.HostPath, () => ix.IsOpen, () => ix.Status, ix.SetStatus, m => ix.Open(m));
            return true;
        }
        c = default;
        return false;
    }

    private static string HostPathOf(string name) =>
        Files.TryGetValue(name, out var sq) ? sq.HostPath
        : RelativeFiles.TryGetValue(name, out var r) ? r.HostPath
        : IndexedFiles.TryGetValue(name, out var ix) ? ix.HostPath
        : name;

    private static void SetStatusOf(string name, string status)
    {
        if (Files.TryGetValue(name, out var sq)) sq.SetStatus(status);
        else if (RelativeFiles.TryGetValue(name, out var r)) r.SetStatus(status);
        else if (IndexedFiles.TryGetValue(name, out var ix)) ix.SetStatus(status);
    }

    /// <summary>The record-lock identity of the most-recently-accessed record: the RRN for a relative file, the
    /// prime record key for an indexed file. Sequential organization has no per-record identity in this model
    /// (documented residue) — an empty string, which suppresses locking.</summary>
    private static string CurrentRecordId(string name) =>
        RelativeFiles.TryGetValue(name, out var r) ? r.LastSlot.ToString(System.Globalization.CultureInfo.InvariantCulture)
        : IndexedFiles.TryGetValue(name, out var ix) ? ix.LastReadPrime ?? ""
        : "";

    private static PhysicalFileState PhysicalFor(string host)
    {
        if (!Physical.TryGetValue(host, out var st)) Physical[host] = st = new PhysicalFileState();
        return st;
    }

    /// <summary>Deregister a connector's open entry on CLOSE and release its record locks (the sharing-registry
    /// side of CLOSE; called from the CLOSE facade for a sharing-active connector).</summary>
    private static void SharedClose(string name)
    {
        if (!ConnectorShares.TryGetValue(name, out var meta)) return;
        if (Physical.TryGetValue(meta.Host, out var st))
        {
            st.Open.Remove(name);
            ReleaseAllForConnector(st, name);
        }
    }
}
