// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Runtime.ExceptionServices;
using CobolNet.Runtime.IO.Store;

namespace CobolNet.Runtime.IO;

/// <summary>
/// The run-unit file-connector registry (DESIGN-runtime-library §2.2): ONE lookup keyed by COBOL file-name over
/// the polymorphic <see cref="FileConnector"/> — no sequential-first probe, no <c>Keyed*</c> fallthrough (the
/// former second dispatch mechanism, deleted per the singular-pattern rule). Owns the GC deferred-close queue
/// (per-object connectors, §9.1.4), the per-object instance-key mint (M2-OO-1i), the CLOSE-WITH-LOCK set, the
/// per-connector declared sharing postures, and the <see cref="PhysicalFileTable"/> (§9.1.15/§9.1.16). The
/// static <see cref="CobolFile"/> facade (the emitted surface) is a pure delegator onto this instance.
/// Verbs whose statement forms are organization-specific keep their organization checks here: a sequential-only
/// verb (WRITE ADVANCING, LINAGE, plain READ) no-ops on a keyed connector exactly as the split registries did —
/// the emitter routes keyed files through the keyed verb entries.
/// </summary>
public sealed class FileRegistry
{
    private readonly Dictionary<string, FileConnector> _files = new(CobolNames.Comparer);
    private readonly HashSet<string> _locked = new(CobolNames.Comparer);
    private int _instSeq;   // the per-object instance-file connector-key sequence (M2-OO-1i; reset for determinism)
    // The GC finalizer thread (~CobolObject) can request a per-object CLOSE at any moment, but the registry is a
    // single-thread structure — so the finalizer only ENQUEUES the key (thread-safe); the actual CloseAndDrop
    // runs on the mutator thread when it next drains (Reset / Open / CloseAll). §9.1.4's NOTE licenses this
    // GC-deferred close.
    private readonly System.Collections.Concurrent.ConcurrentQueue<string> _pendingObjectClose = new();

    /// <summary>A connector's declared sharing posture (from its SELECT's SHARING / LOCK MODE clauses).
    /// <see cref="Sharing"/> is NULL when the connector carries a LOCK MODE clause but no SHARING clause — a LOCK
    /// MODE clause is not a sharing specification, so such a connector's sharing mode is the same undetermined
    /// implementor default a clause-less connector has (<see cref="ImplementorDefaultSharing"/>).
    /// <para>⛔ It holds NO host path. It used to cache one, taken at RegisterSharing time, and every
    /// physical-file-table lookup below read that copy — which was only ever right because the host path could
    /// not change. ISO §12.4.5.3 GR3 makes it change at every OPEN (dynamic file assignment, §9.1.21), so the
    /// connector's own <see cref="FileConnector.HostPath"/> is now the ONE answer and the cache is gone
    /// (kb/Work PB324).</para></summary>
    private readonly record struct ConnectorShare(FileSharing? Sharing, FileLockMode LockMode, bool Multiple);

    /// <summary>The per-connector declared sharing/lock metadata, from the SELECT's SHARING and LOCK MODE clauses
    /// (an absent entry reads as <see cref="ImplementorDefaultShare"/>). Presence is NOT record-locking
    /// participation: that is <see cref="RecordLocksSetUnder"/>, which asks for a LOCK MODE clause (kb/Work PB3372).</summary>
    private readonly Dictionary<string, ConnectorShare> _connectorShares = new(CobolNames.Comparer);

    /// <summary>The physical-file sharing/record-lock registry (§9.1.15/§9.1.16).</summary>
    private readonly PhysicalFileTable _physical = new();

    /// <summary>⛔ WHAT A CLOSE UNDER COMMIT CONTROL KEPT THAT NO CONNECTOR HOLDS ANY MORE (<see cref="KeptFile"/>): the connector
    /// left <see cref="_files"/> (displaced by a re-registration, dropped with its object; kb/Work PB3484) or an ASSIGN USING
    /// associated it with another physical file (<see cref="Associate"/>; kb/Work PB3494), so the handle that is its file lock,
    /// its attachment to the run unit's store and the transaction in it, and its record locks belong to the file they were kept
    /// on and are found by the name of the file-name that kept them. §12.4.6.3.4 GR5 keeps an APPLY COMMIT clause active after its runtime element is canceled for as long as it
    /// <i>"has not been deactivated by a COMMIT or ROLLBACK statement"</i>, and §14.9.7.4 GR3-GR4 and §9.1.16 give the COMMIT the changes, the
    /// file locks and the record locks of the files those clauses name — so a canceled element's connector is no less
    /// the COMMIT's and the ROLLBACK's for being replaced by its element's next activation. They are found here by
    /// <see cref="TransactionHolders"/> and given back with the rest (<see cref="ReleaseCommitLocks"/>,
    /// <see cref="CloseAll"/>); before kb/Work PB3484 they were dropped with the handle open, which no COMMIT, ROLLBACK or
    /// run-unit end could ever reach. A connector's OPEN of the same physical file under the same name takes the state over
    /// instead when it can (<see cref="ReclaimRetired"/>).</summary>
    private readonly List<(string Name, KeptFile Kept)> _retired = [];

    /// <summary>⛔ A COVERED SEQUENTIAL CONNECTOR THAT LEFT <see cref="_files"/> (displaced, or dropped with its object) still
    /// holding what its CLOSE under commit control kept (<see cref="SequentialConnector.ReleaseKept"/>): its handle is the file
    /// lock until the COMMIT or ROLLBACK and the handle a ROLLBACK writes the file through, so it is kept here, as
    /// <see cref="_retired"/> keeps a keyed connector's (DESIGN-runtime-library.md 2.2.2.9; kb/Work PB3340).</summary>
    private readonly List<SequentialConnector> _retiredSequential = [];

    /// <summary>⛔ THE RUN UNIT'S HOST MEDIUM (<see cref="IHostMedium"/>; DESIGN-runtime-library.md 2.2.2.12): every sequential
    /// connector of the run unit and every transaction journal writes through it. The production medium is
    /// <see cref="HostMedium.Production"/>; a witness gives the fault-injecting one.</summary>
    internal IHostMedium Medium { get; init; } = HostMedium.Production;

    /// <summary>The run unit's transactions on sequential physical files (<see cref="SequentialTransactions"/>).</summary>
    private SequentialTransactions Sequential => _sequential ??= new SequentialTransactions(Medium, _stores.NextOrder);

    private SequentialTransactions? _sequential;

    /// <summary>The per-physical-file KEYED record stores (kb/Work PB143 — §14.9.10.4 GR5): one record store
    /// per resolved host path, attached by every relative/indexed connector over it, so mutations are visible
    /// across connectors and the close order cannot pick a surviving private view.</summary>
    private readonly StoreTable _stores = new();

    // ── Run-unit lifecycle ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>⛔ THE DOCUMENTED RUNTIME OPT-OUT FROM THE INDEXED KEY CHECK — GnuCOBOL's <c>COB_KEYCHECK=OFF</c>
    /// under this repository's own naming, and part of the Annex A.1 item 129 determination rather than an
    /// escape from it (docs/CONFORMANCE.md row <c>DOC-A.1-129</c>; owner decision 2026-09-07, kb/Work PB802).
    /// With it off, the key table leaves §14.9.27.4 GR10's validated set for the run and the organization,
    /// record type and record sizes stay in it.
    /// <para>⛔ <c>COBOLNET_</c>, NEVER <c>COBOL_</c>: <see cref="SwitchStore.Prefix"/> is <c>COBOL_</c>, so a
    /// <c>COBOL_KEYCHECK</c> would fall inside the §12.3.7 external-switch FAMILY that
    /// <see cref="RuntimeConfig"/> registers as a pattern. It belongs with <see cref="SystemClock.PinVariable"/>
    /// in the <c>COBOLNET_</c> family, and <see cref="RuntimeConfig"/> lists it like every other variable the
    /// runtime reads (<c>RuntimeConfigTests</c> refuses an unregistered one).</para></summary>
    public const string KeyCheckVariable = "COBOLNET_KEYCHECK";

    /// <summary>Whether this run unit validates an INDEXED file's key table on OPEN — see
    /// <see cref="KeyCheckVariable"/>. Read ONCE per run unit: at construction and again in <see cref="Reset"/>,
    /// which IS the run-unit boundary the runtime has (the emitted driver calls <c>CobolFile.Init()</c>), so an
    /// OPEN never re-reads the environment and a run unit's answer cannot change under it mid-run.</summary>
    public bool KeyCheck { get; private set; } = ReadKeyCheck();

    /// <summary>Off for <c>OFF</c>, <c>0</c>, <c>FALSE</c> or <c>NO</c> (case-insensitive, trimmed) — the same
    /// vocabulary <see cref="CacheConfig"/> and <see cref="CollationRuntime"/> already accept; on for anything
    /// else and for an unset variable, because the check is the default and only an explicit request removes
    /// it.</summary>
    private static bool ReadKeyCheck() =>
        Environment.GetEnvironmentVariable(KeyCheckVariable)?.Trim() is not { } v
        || !(v.Equals("OFF", StringComparison.OrdinalIgnoreCase) || v == "0"   // not a COBOL word: an environment variable value
             || v.Equals("FALSE", StringComparison.OrdinalIgnoreCase)   // not a COBOL word: an environment variable value
             || v.Equals("NO", StringComparison.OrdinalIgnoreCase));   // not a COBOL word: an environment variable value

    /// <summary>Reset the registry (run-unit start): a connector an earlier run unit left open gets that run unit's
    /// implicit CLOSE (<see cref="CloseAll"/>, every organization alike: the keyed connectors used to be dropped
    /// still open, their records unpersisted and their §9.1.15 file lock held against every later run unit,
    /// kb/Work PB603), then everything is cleared, the instance-key sequence restarts and the sharing registries
    /// empty, whether or not a close failed.</summary>
    public void Reset()
    {
        KeyCheck = ReadKeyCheck();   // a NEW run unit re-reads the environment (see KeyCheckVariable)
        try { CloseAll(); }
        finally
        {
            _files.Clear();
            _retired.Clear();   // CloseAll gave back what they kept
            _retiredSequential.Clear();
            _locked.Clear();
            _instSeq = 0;
            while (_pendingObjectClose.TryDequeue(out _)) { }   // a new run unit starts with a clean queue
            _connectorShares.Clear();
            _physical.Clear();
            _stores.Clear();   // kb/Work PB143 — a new run unit re-reads every physical file
        }
    }

    /// <summary>Request the §9.1.4 deletion-time CLOSE of a per-object connector FROM THE GC FINALIZER THREAD:
    /// only enqueue the key; the mutator thread performs the real close in <see cref="DrainPendingObjectCloses"/>.</summary>
    public void EnqueueInstanceClose(string key) => _pendingObjectClose.Enqueue(key);

    /// <summary>Perform any GC-requested per-object closes on the MUTATOR thread (called at the top of Open, a safe
    /// point where no other registry mutation is in flight; <see cref="CloseAll"/> drains the queue itself, one
    /// close at a time under its §14.6.11 2) guard).</summary>
    private void DrainPendingObjectCloses()
    {
        while (_pendingObjectClose.TryDequeue(out var k)) CloseAndDrop(k);
    }

    /// <summary>Mint a UNIQUE per-object connector key for an instance file (M2-OO-1i, ISO §9.1.4 — one connector
    /// per object instance): <paramref name="baseKey"/> suffixed with a monotone <c>#N</c>. Deterministic within
    /// a run unit (the counter resets in <see cref="Reset"/>).</summary>
    public string MintInstanceKey(string baseKey) =>
        baseKey + "#" + System.Threading.Interlocked.Increment(ref _instSeq)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Close and remove a per-object instance-file connector (M2-OO-1i, ISO §9.1.4 — the implicit CLOSE
    /// executed when the owning object is deleted). Reuses <see cref="Close"/> for the spec-required close, then
    /// drops the connector from the registry.</summary>
    public void CloseAndDrop(string key)
    {
        Close(key);                     // deregisters from the physical-file table (SharedClose) while _files still holds it
        Retire(key);                    // what a CLOSE under commit control kept stays the COMMIT's and the ROLLBACK's (PB3484)
        _files.Remove(key);
        _locked.Remove(key);
        _connectorShares.Remove(key);   // the posture belonged to the dropped connector, not to its key
    }

    // ── Registration ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Register a SELECTed sequential file (one per SELECT). Re-registering an INTERNAL connector
    /// replaces it — a registration only runs from a §14.6.2.3.2 INITIAL-state activation (an INITIAL/fresh
    /// instance, or a unit-scoped RECURSIVE unit after run-unit start/CANCEL reset its static guard — kb/Work
    /// PB168), where "not ... in any open mode" (action 3) is exactly a fresh connector; anything the
    /// replacement displaces is closed first (<see cref="CloseDisplaced"/>). An EXTERNAL connector (the
    /// <c>"::EXT::"</c> key band) is ONE per run unit shared by every describing program (ISO §13.18.22.4
    /// GR4a) — a later describer keeps the existing live connector (IC227A).</summary>
    public void Register(string cobolName, string assignTarget, int recordWidth, bool lineSequential,
        bool optional, int varyMin, int varyMax, string? selectName = null, int edition = 2023, int recordMax = 0)
    {
        if (cobolName.StartsWith("::EXT::", StringComparison.Ordinal) && _files.ContainsKey(cobolName))
            return;   // the run-unit EXTERNAL connector already exists (§13.18.22.4 GR4a)
        CloseDisplaced(cobolName);
        _files[cobolName] = new SequentialConnector(CobolFile.ResolveHostPath(assignTarget), recordWidth,
            lineSequential, varyMin, varyMax)
        { IsOptional = optional, SelectName = selectName ?? KeyTail(cobolName), Edition = edition, RecordMax = recordMax, Transactions = Sequential };
    }

    /// <summary>Register a SELECTed REPORT FILE (§9.1.22; a file description entry with REPORT IS and, legally, no
    /// record description — §13.4.5.3 SR8). It is a <see cref="SequentialConnector"/> whose
    /// <paramref name="lineWidth"/> is the widest hosted RD's line width, NOT a record length, so the connector is
    /// told it has no record description (<c>recordDescribed: false</c>) and no rule that compares the file's bytes
    /// with a record length applies to it (kb/Work PB677). The one registration a record-less FD makes, beside
    /// <see cref="Register"/>'s one for every FD that has records.</summary>
    public void RegisterReport(string cobolName, string assignTarget, int lineWidth, bool optional,
        string? selectName = null, int edition = 2023)
    {
        if (cobolName.StartsWith("::EXT::", StringComparison.Ordinal) && _files.ContainsKey(cobolName))
            return;   // the run-unit EXTERNAL connector already exists (§13.18.22.4 GR4a)
        CloseDisplaced(cobolName);
        _files[cobolName] = new SequentialConnector(CobolFile.ResolveHostPath(assignTarget), lineWidth,
            lineSequential: false, recordDescribed: false)
        { IsOptional = optional, SelectName = selectName ?? KeyTail(cobolName), Edition = edition, Transactions = Sequential };
    }

    /// <summary>Close a still-open INTERNAL connector a registration is about to replace (kb/Work PB168):
    /// NO normal path replaces an open connector — a unit-scoped (RECURSIVE) unit registers once per run
    /// unit behind its static guard, an INITIAL unit's files are implicitly closed at its termination, and
    /// CANCEL closes before its post-CANCEL re-registration — so an open one here was abandoned by an
    /// abnormally-ended activation. Closing flushes its buffered writes and frees the OS handle instead of
    /// leaking both. It is the CLOSE dispatch's own close (<see cref="CloseIfOpen"/>, kb/Work PB603), so the
    /// displaced connector also leaves the physical-file table and gives back its file and record locks exactly
    /// as a CLOSE would; the status it records dies with the connector, because there is no statement to report to.
    /// <para>The displaced connector's declared posture goes too: a replacement is a NEW file connector (possibly
    /// on another host path and with other clauses), so leaving the old one arbitrating (§9.1.15) or its LOCK MODE
    /// in force would make the replacement inherit a posture no SELECT wrote — the same "the table outlived the
    /// connector" shape as kb/Work PB321.</para></summary>
    private void CloseDisplaced(string cobolName)
    {
        CloseIfOpen(cobolName);
        Retire(cobolName);   // what a CLOSE under commit control kept stays the COMMIT's and the ROLLBACK's (PB3484)
        _connectorShares.Remove(cobolName);
        _shareBeforeCommitControl.Remove(cobolName);
    }

    /// <summary>A connector is about to leave <see cref="_files"/> (displaced, or dropped with its object) after its CLOSE, or to
    /// be associated with another physical file (<see cref="Associate"/>): if that CLOSE kept the handle, the locks and the
    /// transaction a CLOSE under commit control keeps, they move to <see cref="_retired"/>, where the next COMMIT or ROLLBACK,
    /// or the run unit's end, ends them (<see cref="ReleaseCommitLocks"/>, <see cref="CloseAll"/>). Nothing else leaves a
    /// connector holding anything: every other CLOSE released it all (<see cref="SharedClose"/>).</summary>
    private void Retire(string name)
    {
        if (_files.TryGetValue(name, out var c) && c is KeyedConnector k && k.TakeKept() is { } kept) _retired.Add((name, kept));
        else if (c is SequentialConnector { CloseKeepsLocks: true, IsOpen: false } covered) _retiredSequential.Add(covered);
    }

    /// <summary>⛔ THE ELEMENT'S NEXT ACTIVATION OPENS WHAT ITS PREVIOUS ONE'S CLOSE KEPT (review finding R1-1): the
    /// connector a registration displaced is closed and of THIS run unit, and §9.1.13.9 1) owes '61' only against a physical
    /// file <i>"currently open by another file connector"</i>; the handle it kept is what is left of its file lock (§9.1.15),
    /// which prevents OTHER run units. So the OPEN of the replacement under the same name, on the same physical file and
    /// under commit control (<see cref="KeyedConnector.InheritKept"/>) takes the kept state over as a re-OPEN of the
    /// displaced connector would have, rather than meet the kept handle's share mode. It runs at the OPEN (the host path
    /// is the OPEN's: a dynamic ASSIGN, §12.4.5.3 GR3, fixes it only there), once: a retired connector that is not
    /// matched stays in <see cref="_retired"/> for the COMMIT, the ROLLBACK or the run unit's end.</summary>
    private void ReclaimRetired(string name, FileConnector c)
    {
        if (_retired.Count == 0 || c is not KeyedConnector next) return;
        int at = _retired.FindIndex(r => CobolNames.Same(r.Name, name) && next.InheritKept(r.Kept));
        if (at >= 0) _retired.RemoveAt(at);
    }

    /// <summary>The SELECT-spelled name of a registered connector (ISO §15.28.4 r1c/r2b — kb/Work PB63); for a key
    /// that names no connector (an SD, or a not-yet-registered name) the key's own tail, so the display never
    /// shows an emit-side band.</summary>
    public string SelectNameOf(string cobolName) =>
        _files.TryGetValue(cobolName, out var c) ? c.SelectName : KeyTail(cobolName);

    /// <summary>The part of a registry key after its emit-side bands — the fallback display name only.</summary>
    private static string KeyTail(string key)
    {
        int sep = key.LastIndexOf("::", StringComparison.Ordinal);
        string tail = sep >= 0 ? key[(sep + 2)..] : key;
        int hash = tail.IndexOf('#');
        return hash > 0 ? tail[..hash] : tail;
    }

    /// <summary>Register a SELECTed RELATIVE file (§12.4.5.13; <paramref name="relativeKeyDigits"/> drives the
    /// '14'/'24' RRN-digit statuses, 0 = no RELATIVE KEY clause).</summary>
    public void RegisterRelative(string cobolName, string assignTarget, int recordWidth, bool optional,
        int accessMode, int relativeKeyDigits, int varyMin, int varyMax, string? selectName = null,
        int edition = 2023)
    {
        if (cobolName.StartsWith("::EXT::", StringComparison.Ordinal) && _files.ContainsKey(cobolName))
            return;   // §13.18.22.4 GR4a
        CloseDisplaced(cobolName);
        _files[cobolName] = new RelativeConnector(CobolFile.ResolveHostPath(assignTarget), recordWidth,
            (KeyedAccess)accessMode, relativeKeyDigits, varyMin, varyMax)
        {
            IsOptional = optional, SelectName = selectName ?? KeyTail(cobolName), SharedStores = _stores,
            KeyCheck = KeyCheck, Edition = edition,
        };
    }

    /// <summary>Register a SELECTed INDEXED file with its PRIME key's byte windows (§12.4.5.12; one per data-name-2 of
    /// the SOURCE phrase, GR2 — <see cref="KeySegment"/>).</summary>
    public void RegisterIndexed(string cobolName, string assignTarget, int recordWidth, bool optional,
        int accessMode, KeySegment[] primeKey, int varyMin, int varyMax, CobolCollation? primeCollation = null,
        string? selectName = null, int edition = 2023)
    {
        if (cobolName.StartsWith("::EXT::", StringComparison.Ordinal) && _files.ContainsKey(cobolName))
            return;   // §13.18.22.4 GR4a
        CloseDisplaced(cobolName);
        _files[cobolName] = new IndexedConnector(CobolFile.ResolveHostPath(assignTarget), recordWidth,
            (KeyedAccess)accessMode, primeKey, varyMin, varyMax, primeCollation)
        {
            IsOptional = optional, SelectName = selectName ?? KeyTail(cobolName), SharedStores = _stores,
            KeyCheck = KeyCheck, Edition = edition,
        };
    }

    /// <summary>Register one ALTERNATE RECORD KEY (§12.4.5.6) by its byte windows, in declaration order, with its
    /// optional §12.4.5.7 collating-weight table (null = native ordinal) and §12.4.5.6.4 GR6 SUPPRESS WHEN value.</summary>
    public void AddAlternateKey(string name, KeySegment[] key, bool duplicates, CobolCollation? collation = null,
        string? suppress = null)
    {
        if (_files.TryGetValue(name, out var c) && c is IndexedConnector ix)
            ix.AddAlternateKey(key, duplicates, collation, suppress);
    }

    // ── OPEN / CLOSE ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>OPEN in <paramref name="mode"/> (ISO §14.9.27), with no sharing or retry phrase on the statement
    /// — <see cref="OpenCore"/> arbitrates it against the physical-file registry exactly like the phrase-bearing
    /// <see cref="OpenShared"/>, one polymorphic dispatch for all three organizations.</summary>
    public void Open(string name, FileOpenMode mode, string assign, bool assignDynamic, LinagePage? page)
        => OpenCore(name, mode, null, FileRetryKind.None, 0, OpenTapePhrase.None, assign, assignDynamic, page);

    /// <summary>OPEN … WITH NO REWIND (ISO §14.9.27) — the same arbitrated <see cref="OpenCore"/> with the
    /// phrase's flag set, so the '07' overlay is the ONE effect site whichever entry point the emitter picks.
    /// The OPEN twin of <see cref="CloseNoRewind"/>: the same phrase, the same medium model, the same '07'
    /// (§9.1.13.2 item 6). Before kb/Work PB317 the phrase was parsed and dropped, so an OPEN … WITH NO REWIND
    /// reported '00' while its CLOSE spelling reported '07'.</summary>
    public void OpenTape(string name, FileOpenMode mode, OpenTapePhrase tape, string assign, bool assignDynamic,
        LinagePage? page)
        => OpenCore(name, mode, null, FileRetryKind.None, 0, tape, assign, assignDynamic, page);

    /// <summary>⛔ THE ONE SITE for the OPEN statement's NO REWIND phrase — §14.9.27.4 GR11 and GR12, keyed on
    /// the SAME medium model the CLOSE arm's Table 14 is keyed on (<see cref="PhysicalFileCategory"/>), so the
    /// two statements cannot hold different positions on one phrase. Both open entry points call it, because
    /// SHARING/RETRY and NO REWIND are independent phrases of one general format and a statement may write both.
    ///
    /// <para>GR11: <i>"The NO REWIND phrase will be ignored if it does not apply to the storage medium on which
    /// the file resides. If the NO REWIND phrase is ignored, the OPEN statement is successful and the I-O status
    /// associated with file-name-1 is set to '07'."</i> §9.1.13.2 item 6 fixes what "does not apply to the
    /// storage medium" means — '07' is <i>"An OPEN or CLOSE statement … successfully executed but … an OPEN
    /// statement with the NO REWIND phrase references a physical file on a non-reel/unit medium"</i> — so the
    /// medium the phrase does not apply to is exactly category (a) <see cref="PhysicalFileCategory.NonUnit"/>,
    /// the only category any supported connector reports for a sequential file.</para>
    ///
    /// <para>GR12 governs the OTHER medium: <i>"If the storage medium for the file permits rewinding …"</i>,
    /// i.e. the unit-structured categories (b)/(c). No supported medium is in either — the same closure
    /// <c>CloseByFormat</c>'s <c>UnitStructuredOnly</c> guard rests on — so reaching GR12 b) here would mean a
    /// new medium had been added without implementing its suppress-the-repositioning arm, and that is LOUD
    /// rather than a silent plain open. Category (d) is unreachable from the other side: §14.9.27.3 SR5 rejects
    /// the phrase on a non-sequential file at bind time (COBOLNET1802).</para>
    ///
    /// <para>The '07' rides a SUCCESSFUL open only. §14.9.27.4 GR25 a) makes an unsuccessful OPEN place "a
    /// value … to indicate the condition that caused the OPEN statement to be unsuccessful", which GR11's
    /// warning must not displace — hence the '0' first-digit test, the same guard the CLOSE arm's symbol g
    /// carries. Within the successful values it DOES displace: §9.1.13.2 item 4 a)'s '05' (an OPTIONAL INPUT
    /// file that is not present) is a description in the status-value clause, while GR11 is an explicit
    /// assignment in the statement's own general rules, and §14.9.27.4 assigns '05' only in GR17, whose EXTEND/
    /// I-O modes SR6 excludes from carrying the phrase at all. The determination is recorded in
    /// docs/CONFORMANCE.md §3.</para></summary>
    private void NoRewindPhraseEffect(string name)
    {
        var c = Require(name);
        if (c.Category is not PhysicalFileCategory.NonUnit)
            throw new InvalidOperationException(
                $"OPEN … WITH NO REWIND reached a {c.Category} connector '{name}' — §14.9.27.4 GR11 answers only "
                + "for a non-reel/unit medium (§9.1.13.2 item 6) and §14.9.27.4 GR12 b)'s suppress-the-repositioning arm is "
                + "unimplemented because no supported medium permits rewinding (docs/CONFORMANCE.md §7, A.1 "
                + "item 24); a new medium must implement it here (kb/Work PB317)");
        if (c.Status[0] == '0') c.SetStatus(FileStatusCode.PhraseOnNonReelMedium);
    }

    /// <summary>⛔ THE ONE SITE for the OPEN statement's COBOL-85 REVERSED phrase — <i>the file is positioned at
    /// its END, and every subsequent READ makes the PRECEDING record available, the at end condition arising at
    /// the file's first record</i>. The phrase was deleted by ISO 2002 (VERSION_CHANGE_REFERENCE row 7.12,
    /// gate <c>open-reversed-removed-2002</c>/COBOLNET0902), so this runs only under <c>--std 85</c>; the
    /// repository holds no 1985 text, so the semantics are the VCR row's plus the surveyed implementations
    /// (GnuCOBOL implements REVERSED for sequential files), per the standing latitude decision.
    ///
    /// <para>⛔ IT IS A POSITIONING, NOT A SECOND READ PATH. §14.9.30.4 GR21 c) already defines retrieval in the
    /// decreasing direction — <i>"the first existing record in the physical file whose relative key number is
    /// … less than the file position indicator if PREVIOUS is specified"</i> — and
    /// <see cref="SequentialConnector"/> has implemented it since kb/Work PB334 for
    /// <c>READ … PREVIOUS</c>. REVERSED therefore sets the connector's retrieval DIRECTION and its file position
    /// indicator and adds no retrieval code at all: a second backward walk would be the same rule written twice,
    /// and the two would drift on the very edge cases (RECORD VARYING framing, the at-end boundary) the existing
    /// one has goldens for.</para>
    ///
    /// <para>The connector is a <see cref="SequentialConnector"/> by construction — the binder admits REVERSED
    /// for RECORD sequential organization only (COBOLNET2210), because the backward walk §14.9.30.3 SR7 denies
    /// to LINE SEQUENTIAL is exactly the walk this phrase establishes — so anything else here is a COMPILER
    /// defect and LOUD, the same posture <see cref="NoRewindPhraseEffect"/> takes for an unexpected medium.
    /// §14.9.27.4 GR25 a) owns an unsuccessful open's status and its "the file is not affected", so an
    /// unsuccessful OPEN is left alone rather than repositioned.</para></summary>
    private void ReversedPhraseEffect(string name)
    {
        var c = Require(name);
        if (c.Status[0] != '0') return;   // §14.9.27.4 GR25 a) — the file is not affected
        if (c is not SequentialConnector f)
            throw MisroutedVerb("OPEN … REVERSED", name, c);
        f.PositionReversed();
    }

    /// <summary>A keyed verb reached a connector of the wrong organization — the binder screens
    /// organizations at bind time (§14.9.10.3 SR2, §13.4.6.3 SR3 …), so this is a compiler defect and LOUD.
    /// The old '30'-without-SetStatus fall-throughs left the FILE STATUS item reading its STALE value while
    /// the statement's own branch local held '30' — one statement, two channels, opposite answers (PB140).</summary>
    private static InvalidOperationException MisroutedVerb(string verb, string name, FileConnector c) =>
        new($"{verb} reached a {c.GetType().Name} for '{name}' — the binder screens this (kb/Work PB140)");

    /// <summary>The registered connector for <paramref name="name"/> — an unknown name is a COMPILER defect
    /// (every SELECTed non-SD file registers at unit start, and the SD screens reject the rest at bind time),
    /// so it is LOUD, never a silently-invented status. The old fail-open miss arms reported the SUCCESSFUL
    /// '00' to the FILE STATUS item for a statement whose own local held '30' — one statement, two channels,
    /// opposite answers (kb/Work PB140).</summary>
    private FileConnector Require(string name) =>
        _files.TryGetValue(name, out var c) ? c
        : throw new InvalidOperationException(
            $"file connector '{name}' was never registered — a compiler defect, not a program error (kb/Work PB140)");

    /// <summary>CLOSE (ISO §14.9.6) — Table 14's <c>CLOSE</c> row, symbol c. For a sharing-active connector this
    /// also deregisters it from the physical-file registry and releases its record locks (§14.9.6.4 GR9 /
    /// §9.1.16) - those of a file in an APPLY COMMIT clause excepted, which only a COMMIT or ROLLBACK releases.</summary>
    public void Close(string name) => CloseByFormat(name, CloseFormat.Normal);

    /// <summary>⛔ THE ONE CLOSE DISPATCH — §14.9.6.4 GR3: <i>"The results of executing each type of CLOSE for
    /// each category of physical file are summarized in Table 14, Relationship of categories of physical files
    /// and the format of the CLOSE statement."</i> The cell is looked up in <see cref="Table14"/> from the
    /// written form and the connector's own <see cref="FileConnector.Category"/> (§14.9.6.4 GR2), and its
    /// symbols are then EXECUTED. Each written form used to carry a hand-copied transcription of its own
    /// Non-unit cell, which is why the (b)/(c) columns had no representation and the category placement lived
    /// in a doc comment (kb/Work PB235).
    ///
    /// <para>§14.9.6.4 GR1 is hoisted here, ahead of every symbol: <i>"The file connector referenced by
    /// file-name-1 shall be open. If the file connector is not open, the CLOSE statement is unsuccessful and
    /// the I-O status indicator for the file connector is set to '42'."</i> One rule, one place — and it is
    /// what keeps <see cref="SharedClose"/>'s lock release off the unsuccessful path, where the connector holds
    /// nothing to release and GR9's "execution of the CLOSE statement" has performed none of its actions.</para></summary>
    private void CloseByFormat(string name, CloseFormat format)
    {
        var c = Require(name);
        var cell = Table14.Cell(format, c.Category);
        // Table 14's 'N/A' cells are the non-sequential × phrase combinations §14.9.6.3 SR1 forbids at BIND
        // time ("The NO REWIND, REEL, and UNIT phrases may be used only with files that are of sequential
        // organization"), so reaching one is a COMPILER defect, never a program error — the old guard's
        // pattern-match miss silently skipped the status assignment and left the FILE STATUS item stale
        // (kb/Work PB140).
        if (cell.HasFlag(CloseSymbol.NotApplicable))
            throw new InvalidOperationException(
                $"CLOSE {format} reached a {c.Category} connector '{name}' — Table 14 marks that cell N/A and "
                + "§14.9.6.3 SR1 rejects it at bind time (kb/Work PB140/PB235)");
        // Symbols a, b, d and f manipulate a unit, a volume pointer or a reel position. No supported medium is
        // in category (b) or (c) (PhysicalFileCategory), so no reachable cell carries one; performing the rest
        // of such a cell and silently dropping these would be a partial CLOSE reported as a whole one.
        if ((cell & Table14.UnitStructuredOnly) != 0)
            throw new InvalidOperationException(
                $"CLOSE {format} on a {c.Category} file needs Table 14 symbols {cell & Table14.UnitStructuredOnly}, "
                + "which require a reel/unit-structured medium — WiseOwl COBOL supports none (docs/CONFORMANCE.md "
                + "§7, A.1 item 24); a new medium must implement them here (kb/Work PB235)");
        bool absent = c.OptionalNotPresent;   // read BEFORE the close (the FPI state survives it — PB140)
        // e) Close unit, non-unit-media branch: "Execution of this statement is considered successful. The file
        // remains in the open mode, the file position indicator is unchanged, the I-O status indicator for the
        // file connector is set to '07', and no other action takes place." (§14.9.6.4 GR3 symbol e). "No other
        // action" is also why this arm does NOT call SharedClose: the connector is still OPEN, so releasing the
        // file lock GR9 gave it — or its record locks — would leave an open connector arbitrating against
        // nothing (§14.9.6.4 GR9's release is the "Close file" symbol c's, which this cell does not contain).
        // §14.9.6.4 GR6: for an ABSENT optional input file "no end-of-file or unit processing is performed",
        // so the '07' that describes unit processing is not owed and the close is the plain-successful '00'.
        if (cell.HasFlag(CloseSymbol.CloseUnit))
        {
            c.SetStatus(!c.IsOpen ? FileStatusCode.FileNotOpen
                        : absent ? FileStatusCode.Success
                        : FileStatusCode.PhraseOnNonReelMedium);
            return;
        }
        // §14.9.6.4 GR1 — the not-open guard, ahead of any closing action.
        if (!c.IsOpen) { c.SetStatus(FileStatusCode.FileNotOpen); return; }
        // c) Close file: "Closing operations specified by the implementor are executed." WiseOwl COBOL's are
        // documented at docs/CONFORMANCE.md §7, A.1 item 24 (which makes them a required documented
        // item) — flush/persist/dispose in CloseCore, plus the §14.9.6.4 GR9 lock release here.
        SharedClose(name, c);   // every connector: every successful OPEN registered it (kb/Work PB321)
        c.Close();
        // g) Optional phrases ignored: "The CLOSE statement is executed as if none of the optional phrases were
        // present. The I-O status indicator for the file connector is set to '07'." — §9.1.13.2 item 6's
        // phrase-on-a-non-reel-medium warning, which rides a SUCCESSFUL close only (an unsuccessful '30' keeps
        // its own status) and is not owed for the GR6 absent-optional case (kb/Work PB141).
        if (cell.HasFlag(CloseSymbol.PhrasesIgnored) && !absent && c.Status[0] == '0')
            c.SetStatus(FileStatusCode.PhraseOnNonReelMedium);
    }

    /// <summary>The IMPLICIT close surface (§14.9.5 GR9's "each … file connector that is open", §14.6.11 2)'s
    /// "each file that is in the open mode" through <see cref="CloseAll"/>, and a displaced registration's): an
    /// open connector gets the plain CLOSE; a closed one is skipped — no '42' stamp, no shared-close
    /// bookkeeping — and so is a name the registry
    /// does not hold: registration happens at activation, so an activation that raised before registering
    /// leaves a canceled unit's names unregistered, and "never registered" is a fortiori "not open"
    /// (kb/Work PB154 — Require() here turned GR7's no-op into an InvalidOperationException).</summary>
    public void CloseIfOpen(string name)
    {
        if (_files.TryGetValue(name, out var c) && c.IsOpen) Close(name);
    }

    /// <summary>CLOSE … WITH LOCK — close, then prevent reopen (a subsequent OPEN is status 38). Only a
    /// SUCCESSFUL close locks: §14.9.6.4 GR1 makes a not-open CLOSE ('42') unsuccessful, and an unsuccessful
    /// CLOSE performs none of its closing actions — the old unconditional add poisoned every later OPEN of a
    /// file whose CLOSE WITH LOCK had failed (kb/Work PB140).</summary>
    public void CloseWithLock(string name)
    {
        Close(name);
        if (Require(name).Status[0] == '0') _locked.Add(name);
    }

    /// <summary>CLOSE … REEL/UNIT — Table 14's <c>CLOSE UNIT</c> row (§14.9.6.3 SR2 makes REEL and UNIT
    /// equivalent). On the Non-unit medium WiseOwl COBOL supports that cell is symbol e alone: the file REMAINS
    /// OPEN with status '07' and nothing else happens. <see cref="CloseByFormat"/> executes the cell.</summary>
    public void CloseReelUnit(string name) => CloseByFormat(name, CloseFormat.Unit);

    /// <summary>CLOSE … REEL/UNIT FOR REMOVAL — Table 14's <c>CLOSE UNIT FOR REMOVAL</c> row, a DIFFERENT row
    /// from <see cref="CloseReelUnit"/> even though its Non-unit cell is the same symbol e (the (b)/(c) cells
    /// add symbol d, unit removal). Folding the two forms into one entry was what let the FOR REMOVAL phrase
    /// have no consumer at all (kb/Work PB235).</summary>
    public void CloseReelUnitForRemoval(string name) => CloseByFormat(name, CloseFormat.UnitForRemoval);

    /// <summary>CLOSE … WITH NO REWIND — Table 14's <c>CLOSE WITH NO REWIND</c> row, whose Non-unit cell is
    /// c,g: the file IS closed exactly as if no phrase were present (symbol g) and a SUCCESSFUL close then
    /// reports '07'. <see cref="CloseByFormat"/> executes the cell (kb/Work PB141 gave the phrase its own bound
    /// kind; before that it bound to a plain CLOSE and reported '00').</summary>
    public void CloseNoRewind(string name) => CloseByFormat(name, CloseFormat.NoRewind);

    /// <summary>⛔ RUN-UNIT TERMINATION'S IMPLICIT CLOSE, ISO §14.6.11 2): <i>"An implicit CLOSE statement without any
    /// phrases is executed for each file that is in the open mode. These implicit close statements shall be executed
    /// for all open files in the run unit, even when an error occurs during the execution of one or more of the CLOSE
    /// statements."</i> (§14.6.12 runs it after an abnormal termination too.) Print streams flush and keyed stores
    /// persist (NIST RL208A).
    /// <para>Each file is closed by <see cref="CloseIfOpen"/>, the CLOSE dispatch's own entry, so the implicit CLOSE
    /// IS the plain CLOSE: Table 14's row, the physical-file deregistration and the §9.1.15 / §9.1.16 lock release
    /// cannot drift from the explicit statement's, and a file that is not open is not touched (kb/Work PB603: this
    /// loop used to call each connector's own <c>Close()</c>, stamping '42' on every closed sequential file). No
    /// exception procedure runs: the emitted CLOSE statement selects USE declaratives, this does not.</para>
    /// <para>"Even when an error occurs": a close whose failure is an I-O status ('30') is an ordinary completed
    /// CLOSE. A failure no status describes is a runtime defect, and it must neither stop the closes after it nor
    /// vanish: every file is closed first, then the failure is rethrown (several as one
    /// <see cref="AggregateException"/>). Idempotent: a second call finds nothing open.</para></summary>
    public void CloseAll()
    {
        // §9.1.18.5's first sentence: a run unit that reaches its implicit CLOSE without an abnormal ending commits (the
        // abnormal ending ran its file-only ROLLBACK first, RunUnit.Terminate, so nothing is left to commit then — and a
        // transaction whose ROLLBACK the medium refused there is owed a rollback, which this COMMIT refuses and retries:
        // KeyedStore.Commit, review finding R1-4).
        EndTransactions(abnormal: false);
        List<Exception>? failures = null;
        // §9.1.4's deletion-time closes the GC requested and the mutator has not yet performed: each object's file is
        // closed and dropped as at its deletion. One that fails stays registered, out of its open mode.
        while (_pendingObjectClose.TryDequeue(out var key))
        {
            try { CloseAndDrop(key); }
            catch (Exception failure) { (failures ??= []).Add(failure); }
        }
        foreach (string name in _files.Keys.ToArray())   // a snapshot: a held-line drain runs report-writer code mid-walk
        {
            try { CloseIfOpen(name); }
            catch (Exception failure) { (failures ??= []).Add(failure); }
        }
        // §9.1.18.5, after the commit or the rollback: "In both cases all file and record locks are then released" — the
        // file locks a CLOSE under commit control kept, the implicit CLOSEs' just above included (review finding R1-2). A
        // transaction whose rollback the medium refused is left as a death leaves it, for the dead-owner step.
        ReleaseAllKept(ref failures);
        if (failures is [var only]) ExceptionDispatchInfo.Throw(only);
        if (failures is not null)
            throw new AggregateException("the run unit's implicit CLOSE failed for more than one file (ISO §14.6.11 2))",
                failures);
    }

    // ── Sequential-surface verbs (keyed connectors are reached via the keyed entries below) ─────────────────

    /// <summary>Plain <c>WRITE record</c> (ISO §14.9.51); <paramref name="length"/> is the varying-record length
    /// (§13.18.43 GR13a), -1 = the record's own size.</summary>
    public void Write(string name, string image, int length, LinagePage? page) =>
        SequentialOf(name, "WRITE").Write(image, length, page);

    /// <summary><c>WRITE record {BEFORE|AFTER} ADVANCING {n LINES | PAGE}</c>; <paramref name="lines"/> = -1 is PAGE.</summary>
    public void WriteAdvancing(string name, string image, int lines, bool before, LinagePage? page) =>
        SequentialOf(name, "WRITE ADVANCING").WriteAdvancing(image, lines, before, page);

    /// <summary>Make <paramref name="drain"/> the holder of the connector's one held-back print line (see
    /// <see cref="FileConnector.HeldLineDrain"/>; kb/Work PB1247). A DIFFERENT holder is drained first: its line
    /// was produced earlier, so it reaches the medium before anything the new holder writes.</summary>
    internal void HoldLine(string name, Action drain)
    {
        var connector = SequentialOf(name, "REPORT");
        if (connector.HeldLineDrain is { } previous && !ReferenceEquals(previous, drain))
        {
            connector.HeldLineDrain = null;
            previous();
        }
        connector.HeldLineDrain = drain;
    }

    /// <summary>Does the print device of the connector of <paramref name="name"/> stand on a line that has been
    /// presented and not travelled past (<see cref="SequentialConnector.DeviceOnOpenLine"/>)?</summary>
    internal bool DeviceOnOpenLine(string name) => SequentialOf(name, "REPORT").DeviceOnOpenLine;

    /// <summary>The paged store the keyed connector of <paramref name="name"/> is attached to, or null
    /// (<see cref="KeyedConnector.PagedStore"/>; the seam of DESIGN-runtime-library.md 2.2.1.11's witnesses, kb/Work
    /// PB3336).</summary>
    internal PagedFile? PagedStoreOf(string name) =>
        _files.TryGetValue(name, out var c) && c is KeyedConnector k ? k.PagedStore : null;

    /// <summary>The sequential connector a sequential-surface entry acts on: an unregistered name and a keyed
    /// connector are both compiler defects (the binder routes keyed files to the keyed entries), so both are
    /// LOUD — the old <c>TryGetValue</c> guard wrote nothing and reported nothing (kb/Work PB360).</summary>
    private SequentialConnector SequentialOf(string name, string verb) =>
        Require(name) as SequentialConnector ?? throw MisroutedVerb(verb, name, Require(name));

    /// <summary>
    /// ISO §12.4.5.3 GR3, reached from §14.9.27.4 GR26 — THE ONE PLACE a statement establishes the connector's
    /// association with a physical file, with EXACTLY ONE caller — <see cref="OpenCore"/>, the single OPEN dispatch
    /// that <see cref="Open"/>, <see cref="OpenTape"/>, <see cref="OpenShared"/> and the emitted SORT/MERGE
    /// implicit opens (§14.9.40.4 GR12a/GR15a, §14.9.24.4 GR7a) all funnel through. <see cref="DeleteFile"/> does NOT
    /// associate: §12.4.5.3 GR3's list of associating statements is closed (OPEN, SORT, MERGE), so it reads the
    /// standing association and takes §14.9.10.4 GR14's '05' when there is none. Returns false when the association
    /// could not be made: the connector then carries the
    /// §9.1.13.6 item 2 '31' status and the statement does NOT proceed — GR3's closing sentence, "the OPEN, SORT, or
    /// MERGE statement is unsuccessful".
    /// <para>An unregistered name falls through to the caller's own <see cref="Require"/>, which reports the compiler
    /// defect loudly (kb/Work PB140); this method never invents a status for one.</para>
    /// <para>⛔ The specification is the STATEMENT'S argument, rendered by the emitter from the file control entry of
    /// the runtime element executing this OPEN — never connector state. GR3 a) and b) both name that element, and one
    /// file connector can be described by several of them (§13.18.22.4 GR4 a: an EXTERNAL file connector is ONE object
    /// per run unit); a connector-held source answered with whichever element installed it last (kb/Work PB673).</para>
    /// </summary>
    private bool Associate(string name, string assign, bool assignDynamic)
    {
        if (!_files.TryGetValue(name, out var c)) return true;
        if (c.Associate(assign, assignDynamic) is { } failed)
        {
            c.SetStatus(failed);   // '31' — the status assignment is also the §15.28.4 r2a access record (PB63)
            return false;
        }
        // ⛔ WHAT THE CLOSE KEPT BELONGS TO THE PHYSICAL FILE IT WAS KEPT ON (kb/Work PB3494; §9.1.15, §14.9.7.4 GR4): the
        // association has just moved to ANOTHER one, so the OPEN about to run is an ordinary OPEN of that file - it adopts neither
        // the old file's handle nor its store - while the old file's transaction, record locks and file lock stay the COMMIT's and
        // the ROLLBACK's, as a replaced connector's do (Retire).
        if (c is KeyedConnector { KeptIsOfAnotherPath: true }) Retire(name);
        return true;
    }

    /// <summary>The file's LINAGE-COUNTER register (ISO §8.4.3.14 / §13.18.34 GR7).</summary>
    public long LinageCounter(string name) =>
        _files.TryGetValue(name, out var c) && c is SequentialConnector f ? f.LinageCounter : 0;

    /// <summary>The end-of-page condition of the file's most recent WRITE (ISO §14.9.51 GR26a/b).</summary>
    public bool EndOfPage(string name) =>
        _files.TryGetValue(name, out var c) && c is SequentialConnector f && f.EndOfPage;

    /// <summary>The AT END condition for a sequential file (status 10).</summary>
    public bool AtEnd(string name) =>
        _files.TryGetValue(name, out var c) && c is SequentialConnector f && f.AtEnd;

    /// <summary>True when the last operation on a sequential file was unsuccessful (status not 00).</summary>
    public bool Failed(string name) =>
        _files.TryGetValue(name, out var c) && c is SequentialConnector f && f.Status != FileStatusCode.Success;

    // ── Organization-neutral accessors (ONE polymorphic read — the registry win) ────────────────────────────

    /// <summary>The file's current FILE STATUS two-character code (ISO §9.1.13). An unknown name is LOUD — the
    /// old fail-open '00' told the program a statement SUCCEEDED whose own branch local held '30' (kb/Work
    /// PB140; the reachable case was an unregistered SD file, now a bind-time rejection).</summary>
    public string Status(string name) => Require(name).Status;

    /// <summary>⛔ THE ONE PLACE THE EC-I-O CONDITION OF THE LAST I-O OPERATION IS DECIDED (ISO §9.1.13.1;
    /// kb/Work PB526) — the level-3 exception-name set to exist by the operation just executed on
    /// <paramref name="name"/>, or null when it set none. Two sources, ONE answer:
    /// <list type="number">
    /// <item>a name the operation's own rule NAMED (<see cref="FileConnector.IoConditionName"/>) — today
    ///   §13.18.34.4 GR6 b) 2's EC-I-O-LINAGE and §14.9.51.4 GR27 a)'s EC-I-O-EOP / EC-I-O-EOP-OVERFLOW, the
    ///   EC-I-O conditions with no I-O status of their own;</item>
    /// <item>otherwise §9.1.13.1's status→EC correspondence over the connector's I-O status.</item>
    /// </list>
    /// A generated hook asks HERE and never calls <c>ExceptionCatalog.IoEcOfStatus</c> itself: the correspondence
    /// is a DEFAULT, and a second caller applying only the default is how the named condition would be lost on
    /// one of the two paths.</summary>
    public string? IoConditionName(string name)
    {
        var c = Require(name);
        return c.IoConditionName ?? Exceptions.ExceptionCatalog.IoEcOfStatus(c.Status);
    }

    /// <summary>FUNCTION EXCEPTION-FILE(file-connector-name) (ISO §15.28.4 r2): two alphanumeric spaces when the
    /// named connector was never opened, attempted to be opened, or otherwise attempted to be accessed (r2a — or is
    /// unknown); else its two-character I-O status followed by the file-name "exactly as specified in the SELECT
    /// clause" (r2b — the connector's <see cref="FileConnector.SelectName"/>, carried from the compiler at
    /// registration; kb/Work PB63 — it used to be recovered from the registry KEY by a "::" strip, which
    /// upper-cased an EXTERNAL name and left an OBJECT file's per-instance "#N" suffix in place).</summary>
    public string ExceptionFile(string name)
    {
        if (!_files.TryGetValue(name, out var c) || !c.EverAccessed) return "  ";
        return c.Status + c.SelectName;
    }

    /// <summary>The length of the most recently read record (ISO §13.18.43 GR15).</summary>
    public int LastReadLength(string name) => _files.TryGetValue(name, out var c) ? c.LastReadLength : 0;

    /// <summary>The record the most recent successful READ made available, at its own length
    /// (<see cref="FileConnector.CurrentRecord"/>); "" for an unknown name.</summary>
    public string CurrentRecord(string name) => _files.TryGetValue(name, out var c) ? c.CurrentRecord : "";

    /// <summary>The EXTENT TABLE the current record was sent with (<see cref="FileConnector.CurrentRecordExtents"/>;
    /// determination D-FRA (v), kb/Work PB1053); null for an unknown name or a record that carries none.</summary>
    public RecordExtents? CurrentRecordExtents(string name) =>
        _files.TryGetValue(name, out var c) ? c.CurrentRecordExtents : null;

    /// <summary>The open-mode view for USE-declarative mode scoping (ISO §14.9.49.4 GR6b–e); −1 unknown/closed.</summary>
    public int OpenModeOf(string name) => _files.TryGetValue(name, out var c) ? c.OpenModeView : -1;

    /// <summary>The open mode of a connector that IS OPEN (ISO §9.1.4), null when it is not open, when a
    /// failed OPEN merely recorded an attempted mode, or when no connector of that name is registered
    /// (<see cref="FileConnector.OpenModeIfOpen"/>). The §14.9.21.4 GR3 report-file-mode test reads this.</summary>
    public FileOpenMode? OpenModeIfOpen(string name) =>
        _files.TryGetValue(name, out var c) ? c.OpenModeIfOpen : null;

    // ── Keyed verbs (ISO §14.9.51 / §14.9.35 / §14.9.30 / §14.9.41 / §14.9.10) ───────────────────────────────

    /// <summary>Stage the RELATIVE KEY item's value for the next keyed verb.</summary>
    public void SetRelativeKey(string name, long rrn)
    {
        // A RELATIVE KEY is staged only for a relative connector the binder resolved; a miss is a compiler defect
        // and a dropped key would make the next keyed verb act on the PREVIOUS record number (kb/Work PB360).
        if (Require(name) is RelativeConnector r) r.SetPendingKey(rrn);
        else throw MisroutedVerb("RELATIVE KEY staging", name, Require(name));
    }

    /// <summary>The RRN last made available/released — the §14.9.30 GR25 / §14.9.51 GR29a MOVE-back source.</summary>
    public long RelativeSlot(string name) =>
        _files.TryGetValue(name, out var c) && c is RelativeConnector r ? r.LastSlot : 0;

    /// <summary>Keyed WRITE (§14.9.51) — returns the I-O status.</summary>
    public string WriteKeyed(string name, string image, int length)
    {
        var c = Require(name);
        using var statement = c.BeginStatement();   // kb/Work PB2660
        return c switch
        {
            RelativeConnector r => r.Write(image, length),
            IndexedConnector ix => ix.Write(image, length),
            var other => throw MisroutedVerb("keyed WRITE", name, other),
        };
    }

    /// <summary>Keyed REWRITE (§14.9.35) — returns the I-O status.</summary>
    public string RewriteKeyed(string name, string image, int length)
    {
        var c = Require(name);
        using var statement = c.BeginStatement();   // kb/Work PB2660
        return c switch
        {
            RelativeConnector r => r.Rewrite(image, length),
            IndexedConnector ix => ix.Rewrite(image, length),
            var other => throw MisroutedVerb("keyed REWRITE", name, other),
        };
    }

    /// <summary>DELETE RECORD (§14.9.10 F1); for indexed random/dynamic the prime key is sliced from
    /// <paramref name="keyedRecordImage"/> (GR3) — relative uses the staged relative key (GR4).</summary>
    public string DeleteRecord(string name, string keyedRecordImage, RecordExtents? areaExtents = null)
    {
        var c = Require(name);
        using var statement = c.BeginStatement();   // kb/Work PB2660
        return DeleteRecordInStatement(c, name, keyedRecordImage, areaExtents);
    }

    private static string DeleteRecordInStatement(FileConnector c, string name, string keyedRecordImage,
        RecordExtents? areaExtents) => c switch
    {
        RelativeConnector r => r.Delete(),
        IndexedConnector ix => ix.Delete(keyedRecordImage, areaExtents),
        // §14.9.10.3 SR2 restricts DELETE RECORD to relative/indexed at BIND time — reaching here with a
        // sequential connector is a compiler defect (the old '30'-without-SetStatus arm left the FILE STATUS
        // item reading its stale value while the statement's own branch local held '30' — kb/Work PB140).
        var other => throw new InvalidOperationException(
            $"DELETE RECORD reached a {other.GetType().Name} for '{name}' — the binder screens this (kb/Work PB140)"),
    };

    /// <summary>Sequential keyed READ [NEXT] (§14.9.30 F1) — returns the I-O status and the record image.</summary>
    public string ReadKeyedNext(string name, out string image)
    {
        image = "";
        var c = Require(name);
        using var statement = c.BeginStatement();   // kb/Work PB2660
        return c switch
        {
            RelativeConnector r => r.ReadNext(out image),
            IndexedConnector ix => ix.ReadNext(out image),
            var other => throw MisroutedVerb("keyed READ NEXT", name, other),   // the PB140 sweep's missed arm
        };
    }

    /// <summary>Sequential keyed READ PREVIOUS (§14.9.30 F1, COBOL-2002+; compiler edition-gated).</summary>
    public string ReadKeyedPrevious(string name, out string image)
    {
        image = "";
        var c = Require(name);
        using var statement = c.BeginStatement();   // kb/Work PB2660
        return c switch
        {
            RelativeConnector r => r.ReadPrevious(out image),
            IndexedConnector ix => ix.ReadPrevious(out image),
            var other => throw MisroutedVerb("keyed READ PREVIOUS", name, other),
        };
    }

    /// <summary>⛔ A READ whose record has just landed in the record area and whose OCCURS DEPENDING ON item then
    /// makes it too long is unsuccessful with '34' (§9.1.13.6 item 4 b): <i>"A READ statement is unsuccessfully
    /// executed because the records are variable in length because of an OCCURS DEPENDING ON clause in the record
    /// in the associated record description entry and the associated DEPENDING ON item contains a value that makes
    /// the number of bytes in the record exceed the value specified or implied by the file description
    /// entry"</i> (kb/Work PB1513). The connector cannot see the record's layout — the generated READ evaluates the
    /// predicate where the record description is known and reports the outcome here, so the ONE I-O status
    /// assignment path assigns it (and, the connector being open, starts the permanent error of §9.1.13.1 —
    /// kb/Work PB1541). It is an unsuccessful READ, so §14.9.30.4 GR18's "no valid record position" applies.
    /// Returns the status.</summary>
    public string ReadExceedsRecordMaximum(string name)
    {
        var c = Require(name);
        // The governed READ already ran its record-lock actions (GR11 b)/c)/d) — they speak of the SUCCESSFUL execution)
        // before the verdict was known; an unsuccessful READ acquires none (the pre-flight denials above say the same),
        // so the record's lock goes back to what this READ FOUND — released when the READ took it, KEPT when an earlier
        // READ WITH LOCK took it under multiple record locking (§14.9.30.4 GR11 b)/GR9: another connector's READ of it
        // must still answer '51'), and re-taken when GR11 b)'s NO LOCK release had given it up.
        if (c.RecordLockHeldBeforeRead is { } held && c.LastReadRecordId is { Length: > 0 } lockedId)
        {
            var st = _physical.For(c.HostPath);
            if (held) _physical.LockRecord(st, name, lockedId, c);
            else PhysicalFileTable.ReleaseSingle(st, name, lockedId);
        }
        c.ApplyUnsuccessfulReadPosition();
        c.SetStatus(FileStatusCode.PermanentBoundary);
        return FileStatusCode.PermanentBoundary;
    }

    /// <summary>Random keyed READ (§14.9.30 F2): indexed slices the key value from
    /// <paramref name="keyedRecordImage"/> (GR30–GR32); relative uses the staged relative key (GR29).</summary>
    public string ReadKeyed(string name, int keyIndex, string keyedRecordImage, out string image,
        RecordExtents? areaExtents = null)
    {
        image = "";
        var c = Require(name);
        using var statement = c.BeginStatement();   // kb/Work PB2660
        return c switch
        {
            RelativeConnector r => r.ReadRandom(out image),
            IndexedConnector ix => ix.ReadRandom(keyIndex, keyedRecordImage, out image, areaExtents),
            var other => throw MisroutedVerb("keyed READ", name, other),
        };
    }

    /// <summary>START on a relative file (§14.9.41 GR8–GR12) — a numeric RRN comparison.</summary>
    public string StartRelative(string name, string op, long rrn)
    {
        var c = Require(name);
        using var statement = c.BeginStatement();   // kb/Work PB2660 — START selects from the file as it is now
        return c is RelativeConnector r ? r.Start(op, rrn) : throw MisroutedVerb("START (relative)", name, c);
    }

    /// <summary>START on an indexed file (§14.9.41 GR13–GR17) — a leftmost-length partial-key comparison whose
    /// search key is sliced out of <paramref name="keyedRecordImage"/>, the RECORD AREA (GR17 a); kb/Work
    /// PB355), exactly as the random READ's and DELETE's key values are.</summary>
    public string StartIndexed(string name, int keyIndex, string op, string keyedRecordImage, StartKeyLength length,
        RecordExtents? areaExtents = null)
    {
        var c = Require(name);
        using var statement = c.BeginStatement();   // kb/Work PB2660 — START selects from the file as it is now
        return c is IndexedConnector ix ? ix.Start(keyIndex, op, keyedRecordImage, length, areaExtents)
            : throw MisroutedVerb("START (indexed)", name, c);
    }

    /// <summary>START FIRST/LAST (COBOL-2002+), on EVERY organization — the standard writes the rule three
    /// times, once per organization heading: §14.9.41.4 GR11/GR12 (RELATIVE FILES), GR18/GR19 (INDEXED FILES)
    /// and GR20/GR21 (SEQUENTIAL FILES). The sequential arm is not an extension: §14.9.41.3 SR2 makes FIRST or
    /// LAST the REQUIRED phrase on a sequential-organization file, so this is the only shape a conforming START
    /// on one can have, and leaving the arm out made the statement the standard requires the one this switch
    /// threw on (kb/Work PB352).</summary>
    public string StartFirstLast(string name, bool last)
    {
        var c = Require(name);
        using var statement = c.BeginStatement();   // kb/Work PB2660 — START selects from the file as it is now
        return c switch
        {
            RelativeConnector r => r.StartFirstLast(last),
            IndexedConnector ix => ix.StartFirstLast(last),
            SequentialConnector s => s.StartFirstLast(last),
            var other => throw MisroutedVerb("START FIRST/LAST", name, other),
        };
    }

    /// <summary>DELETE FILE (§14.9.10 Format 2, COBOL-2023): an OPEN connector → '41' (GR13); the physical file
    /// currently open by ANOTHER file connector → the file sharing conflict, '62' (GR15 / §9.1.13.9 item 2),
    /// re-attempted under a RETRY phrase (GR15 → §14.7.9: another connector of this run unit cannot close
    /// mid-loop, so every retry form exhausts to '62' against it — §9.1.13.9 defines no deadlock value for a
    /// file-sharing conflict, see <see cref="ExhaustionStatus"/> — while another run unit's handle can be released,
    /// and RETRY FOREVER waits for it, kb/Work PB1163); an ABSENT physical file is
    /// a SUCCESSFUL completion, status '05' (GR14); insufficient authority → '37' (GR16) — ONE polymorphic body
    /// over <see cref="FileConnector"/> for all three organizations.</summary>
    public string DeleteFile(string name, bool overridden = false) =>
        DeleteFile(name, FileRetryKind.None, 0, overridden);

    /// <summary>DELETE FILE with a RETRY phrase (§14.9.10 GR15 / §14.7.9) and the GR18 OVERRIDE flag.</summary>
    public string DeleteFile(string name, FileRetryKind retryKind, long retryAmount, bool overridden = false)
    {
        var c = Require(name);
        // (a DELETE FILE accesses the connector — FUNCTION EXCEPTION-FILE r2a, §15.28.4 — recorded by the status
        // assignment below, the ONE access-recording path)
        // §14.9.10.4 GR14 — "If the file associated with file-name-1 is not present, the execution of the DELETE FILE
        // statement is successful and the I-O status value in the file connector referenced by file-name-1 is set to
        // '05'." A bare `ASSIGN USING data-name-1` connector has NO associated file until one runs: §12.4.5.3 GR3
        // establishes the association at an OPEN, SORT or MERGE and at no other statement, so before the first of
        // those there is no physical file for this statement to find, and GR14 is its own answer. ⛔ The alternative
        // — re-resolving data-name-1 here — would be an implementor EXTENSION to GR3's closed list of associating
        // statements, on the one verb where guessing wrong DESTROYS DATA; and the old behaviour was worse still,
        // deleting the registration-default `<file-name>.txt` the program never named. GR13's '41' is not skipped by
        // ordering: an unassociated connector cannot be open, because Open associates before it opens.
        if (c.HostPath.Length == 0) { c.SetStatus(FileStatusCode.OptionalFileNotFound); return FileStatusCode.OptionalFileNotFound; }
        string status;
        // '41' GR13 — but a permanent error in effect on the open connector (§9.1.13.1, kb/Work PB1541) answers first.
        if (c.IsOpen) status = c.PermanentErrorInEffect ?? FileStatusCode.FileAlreadyOpen;
        else if (ValidateFixedFileAttributes(c, overridden) is { } conflict)
            status = conflict;                                             // '39' GR18 — see the method
        else if (c is SequentialConnector { CommitControlled: true } covered)
            status = DeleteCoveredSequential(name, covered, retryKind, retryAmount);   // under commit control: the capture (2.2.2.9)
        // §9.1.13.9 2) asks about EVERY other file connector: this run unit's are the registry's to see, and
        // another run unit's only the host's (kb/Work PB860 — asked before the delete, because a Unix unlink
        // succeeds over an open file). Inside the retry loop, because another run unit CAN close mid-loop — and
        // each attempt says which holder refused it, since only one outside the run unit can (kb/Work PB1163).
        // ⛔ ASKED ONLY AFTER GR13: the host probe cannot tell THIS connector's own handle from a foreign one, so
        // for an open connector it would report an outside holder that never lets go, and RETRY FOREVER would
        // wait for ever on a statement GR13 has already answered '41'.
        else if (RetryLoop(() =>
                     OpenByAnotherConnector(name, c.HostPath) ? RetryAttempt.InRunUnit(FileStatusCode.DeleteFileSharing)
                     : HostFile.IsHeldByAnother(c.HostPath) ? DeleteFileHeld(name, c)
                     : RetryAttempt.InRunUnit(FileStatusCode.Success), retryKind, retryAmount) is var sharing
                 && sharing != FileStatusCode.Success)
            status = sharing;   // '62' GR15/§9.1.13.9 item 2 — the file is not deleted
        // GR14's '05' is only for a file that is ABSENT, and GR16's '37' is for one that is there but refused —
        // so the presence question has THREE answers, and it is asked through the ONE shared probe
        // (HostFile.Probe): File.Exists swallows every access error and answers false, classifying a
        // PRESENT-but-unobservable file as gone. This statement's twin — the OPEN presence decision in all
        // three connectors — carried exactly that bug until kb/Work PB323 brought both onto the shared probe;
        // the DELETE FILE arm has been right since PB140, and now the rule is written down only once.
        else
            switch (HostFile.Probe(c.HostPath))
            {
                case FilePresence.Absent:
                    status = FileStatusCode.OptionalFileNotFound;   // '05' GR14 — a SUCCESSFUL completion
                    break;
                case FilePresence.Unauthorized:
                    status = FileStatusCode.PermissionDenied;       // '37' GR16
                    break;
                default:
                    // Present at the probe. The catches remain: the file can still vanish or be locked down
                    // between the probe and the delete, and File.Delete has its own refusal (a read-only file,
                    // a write-protected medium) that no presence probe can see.
                    try
                    {
                        File.Delete(c.HostPath);
                        status = FileStatusCode.Success;
                    }
                    catch (FileNotFoundException) { status = FileStatusCode.OptionalFileNotFound; }      // '05' GR14
                    catch (DirectoryNotFoundException) { status = FileStatusCode.OptionalFileNotFound; } // '05' GR14
                    catch (UnauthorizedAccessException) { status = FileStatusCode.PermissionDenied; }    // '37' GR16
                    catch (IOException ex) { status = FileStatusCode.ForDeleteFileFailure(ex); }         // '37' GR17 / '30'
                    break;
            }
        // ⛔ NOTHING ELSE IS DELETED, and the absence is the design (kb/Work PB802). A physical file's §9.1.6
        // fixed file attributes live IN THE FILE — RecordFraming's store header for a keyed file, nothing at all
        // for a sequential one — so destroying the file destroys them, and there is no second artifact that
        // could outlive it and be compared (§14.9.27.4 GR10) against a DIFFERENT file later created at the same
        // path. Under the catalog sidecar this line had to delete a second file across the whole SUCCESS FAMILY
        // (GR14's '05' included, since a stale catalog beside an already-absent file is the same defect by
        // another route); with no sidecar the question does not arise.
        c.SetStatus(status);
        return status;
    }

    /// <summary>⛔ DELETE FILE OF A SEQUENTIAL FILE UNDER COMMIT CONTROL (DESIGN-runtime-library.md 2.2.2.9; kb/Work PB3340, PB3492).
    /// The NOTE under §14.9.10.4 GR15: <i>"Such files can still be deleted, however if they are, then in the event of a rollback
    /// they will be restored to the state they were in at the last commit or, if none, the start of the run unit"</i> — and
    /// GR15's conflict is a file <i>"locked by another file connector"</i>, which the connector's own kept handle is not. Under
    /// the entry mutex: the entry step, then GR15's sharing conflict ('62', §9.1.13.9 2), re-attempted under a RETRY phrase) —
    /// another live transaction on the file (it holds the file: a DELETE FILE needs no other transaction at all, 2.2.2.8),
    /// another connector of this run unit, or another handle than the connector's own kept one; then GR14's '05' for an
    /// absent file; then the transaction begun if none stands and the file CAPTURED in its base phase (R4), the kept handle
    /// closed, the file removed and its directory synced. The mutex is held from the successful test to the removal and given
    /// back between RETRY attempts, so the transaction it waits for can roll back meanwhile.</summary>
    private string DeleteCoveredSequential(string name, SequentialConnector covered, FileRetryKind retryKind, long retryAmount)
    {
        string path = Path.GetFullPath(covered.HostPath);
        RegionLocks? mutex = null;
        try
        {
            string sharing = RetryLoop(() =>
            {
                mutex = Sequential.HoldEntryMutex(path);
                var holdings = SequentialJournal.Enter(Medium, path, Sequential.Of(path)?.Id, mutex);
                var attempt =
                    holdings.Unlistable ? RetryAttempt.InRunUnit(FileStatusCode.PermissionDenied)   // '37' GR16: no authority to read the lock directory
                    : holdings.RefusesOpen(exclusive: true) is not null ? RetryAttempt.OutsideRunUnit(FileStatusCode.DeleteFileSharing)
                    : OpenByAnotherConnector(name, path) ? RetryAttempt.InRunUnit(FileStatusCode.DeleteFileSharing)
                    : covered.HeldByAnotherAtDelete() ? DeleteFileHeld(name, covered)
                    : RetryAttempt.InRunUnit(FileStatusCode.Success);
                if (attempt.Status != FileStatusCode.Success) { mutex.Dispose(); mutex = null; }
                return attempt;
            }, retryKind, retryAmount);
            if (sharing != FileStatusCode.Success) return sharing;   // '62' GR15 — the file is not deleted
            switch (HostFile.Probe(path))
            {
                case FilePresence.Absent: return FileStatusCode.OptionalFileNotFound;   // '05' GR14 — nothing to capture
                case FilePresence.Unauthorized: return FileStatusCode.PermissionDenied;   // '37' GR16
            }
            var journal = Sequential.Of(path) ?? Sequential.Begin(path, SequentialJournal.StartState.Present);
            covered.GiveBackForRemoval();   // the kept handle goes first: a Windows handle not opened for deletion blocks the removal
            if (!journal.Replaced)
                using (var copy = Medium.OpenAuxiliary(path, FileMode.Open, FileAccess.Read))
                    journal.Capture(SequentialJournal.CaptureVerb.DeleteFile, copy);
            Medium.DeleteFile(path);
            Medium.SyncDirectory(Path.GetDirectoryName(path)!);
            return FileStatusCode.Success;
        }
        catch (UnauthorizedAccessException) { return FileStatusCode.PermissionDenied; }   // '37' GR16
        catch (IOException ex) { return FileStatusCode.ForDeleteFileFailure(ex); }          // '37' GR17 / '30'
        finally { mutex?.Dispose(); }
    }

    /// <summary>§14.9.10.4 GR18 — the fixed-file-attribute match a DELETE FILE performs when the OVERRIDE phrase
    /// is NOT specified. Returns the attribute-conflict status ('39') or null when there is no conflict.
    /// <para>⛔ THIS IS THE ONE PLACE THE §14.9.10.4 GR19 VALIDATED SET IS DEFINED, and today that set is EMPTY,
    /// uniformly and by design — the owner determination recorded as the Annex A.1 item 50 row in
    /// docs/CONFORMANCE.md §7 (kb/Work PB192). GR19 obliges the implementor to DEFINE which fixed-file attributes
    /// are validated; it nowhere requires the set to be non-empty, so an empty definition discharges it. With
    /// nothing validated there is nothing to mismatch, so '39' is unreachable from DELETE FILE BY DEFINITION
    /// rather than by omission, and this method returns null on both arms.</para>
    /// <para>The <paramref name="overridden"/> arm is therefore not dead code and must not be "simplified" away:
    /// GR18's second sentence ("If the OVERRIDE phrase is specified, the file attributes are not checked") is a
    /// guarantee the program is entitled to, and carrying it here is what makes a future NON-EMPTY set — the
    /// OPEN statement's own set is non-empty and reads the physical file's format (kb/Work PB193, PB802) — a
    /// change to ONE method instead of a silent behaviour change for every program that wrote OVERRIDE to opt
    /// out (kb/Work PB196). A non-empty set plugs in below the guard, and nowhere else.</para></summary>
    private static string? ValidateFixedFileAttributes(FileConnector c, bool overridden)
    {
        if (overridden) return null;   // GR18 — "the file attributes are not checked"
        _ = c;                         // the GR19 validated set is empty (A.1 item 50): nothing to compare
        return null;
    }

    /// <summary>True when a file connector OTHER than <paramref name="name"/> currently has the physical file at
    /// <paramref name="host"/> open (§9.1.13.9 item 2 — the DELETE FILE sharing conflict is defined over "another
    /// file connector" plainly, so a non-sharing-registered open connector counts too).</summary>
    private bool OpenByAnotherConnector(string name, string host)
    {
        foreach (var (other, c) in _files)
            if (!CobolNames.Same(other, name)
                && c.IsOpen && HostFile.PhysicalFileComparer.Equals(c.HostPath, host))
                return true;
        return false;
    }

    // ── COBOL-2002 file sharing / record locking (ISO §9.1.15/§9.1.16/§14.9.27/§14.9.47/§14.7.9) ─────────────
    // Design D1: the 51/52/61 statuses are defined over "another file connector" (§9.1.13.9) — two SELECTs bound
    // to one resolved host path are two distinct connectors over one physical file within one run unit, so the
    // machinery is REAL. A conflict held by a connector of THIS run unit cannot clear while the statement runs, so
    // an exhausted retry lands on the conflict's OWN §9.1.13 status, never a manufactured one; a conflict held
    // OUTSIDE the run unit (the host's refusal, kb/Work PB860) can clear, and RetryLoop waits for it (kb/Work
    // PB1163). See D8 in docs/COBOLNET_FILES_DESIGN.md, RetryAttempt and ExhaustionStatus.

    /// <summary>⛔ THE ONE PLACE WiseOwl COBOL'S IMPLEMENTOR-DEFAULT SHARING MODE IS NAMED (ISO §9.1.15:
    /// <i>"If no specification is made in either location, the implementor defines the sharing mode in which the
    /// file is opened; the implementor-defined sharing mode may be one of the modes specified in this Working
    /// Draft International Standard or may be a mode completely specified by the implementor."</i>; §14.9.27.4
    /// GR23 sends the OPEN with neither a SHARING phrase nor a SHARING clause to the same implementor choice).
    /// <para>The determination (Annex A.1 items 77 and 131, <c>docs/CONFORMANCE.md</c> DOC-A.1-77 / DOC-A.1-131,
    /// kb/Work PB322): the default is <b>a function of the open mode</b> and is always one of §9.1.15's own three
    /// modes — <c>OPEN INPUT</c> establishes SHARING WITH READ ONLY and <c>OPEN OUTPUT</c>, <c>I-O</c> and
    /// <c>EXTEND</c> establish SHARING WITH NO OTHER. It is GnuCOBOL's posture (a file opened for input takes a
    /// shared lock, every other mode an exclusive one; CLAUDE.md rule 1 takes it because ISO leaves the choice to
    /// the implementor). Because it is a real mode, every arbitration is the plain <see cref="Table19"/> lookup:
    /// there is no undetermined side and no quantifier.</para>
    /// <para>⛔ DO NOT hard-code a mode at a call site instead. Two call sites used to: a LOCK-MODE-only SELECT
    /// was registered as ALL OTHER by the emitter and a RETRY-phrase-only OPEN was registered as ALL OTHER here,
    /// while a clause-less file was not registered at all — three arms of one determination, two of which had
    /// silently answered it (kb/Work PB321). The default is applied at ONE read, <see cref="SharedOpenAttempt"/>,
    /// where the OPEN statement's mode is known.</para></summary>
    public static FileSharing ImplementorDefaultSharing(FileOpenMode mode) =>
        mode == FileOpenMode.Input ? FileSharing.ReadOnly : FileSharing.NoOther;

    /// <summary>⛔ THE §12.4.5.9.4 GR1 b) 2. IMPLEMENTOR DEFAULT — the record-locking posture of a file
    /// connector whose file control entry writes NEITHER a LOCK MODE clause NOR a SHARING clause and whose OPEN
    /// carried no SHARING phrase. The standard hands the choice to the implementor in as many words —
    /// <i>"the type of record locking for that opening of a shared file associated with that file connector is
    /// defined by the implementor … or specify that the default is no record locking"</i> — and this
    /// compiler's determination is the third option it names: <see cref="FileLockMode.None"/>, so such a
    /// connector SETS no record lock even when an I-O statement writes an explicit LOCK phrase.
    /// <para>⛔ IT SAYS NOTHING ABOUT WHETHER THE CONNECTOR *SEES* OTHER CONNECTORS' LOCKS, and that asymmetry
    /// is the standard's own (kb/Work PB669). §9.1.16 states the visibility rule with no qualification at all:
    /// <i>"While locked by a given file connector, a record is not accessible to another file connector in the
    /// same or a different run unit, except by the execution of a READ statement with the IGNORING LOCK
    /// phrase."</i> §12.4.5.9.4 GR1 a) and b) 1. are careful to say only that no record locks are <b>set</b>,
    /// and GR2 — <i>"If the processor does not support record locking, record locks have no effect for the
    /// associated file connector"</i> — is the clause that would disable the CHECK, which does not apply here.
    /// So this default is the value of <see cref="ConnectorShare"/> a clause-less connector is governed under,
    /// NOT a licence to skip the governance: it is why every governed verb below reads
    /// <see cref="ShareOf"/> instead of returning early on a miss.</para></summary>
    private static readonly ConnectorShare ImplementorDefaultShare =
        new(Sharing: null, FileLockMode.None, Multiple: false);

    /// <summary>⛔ THE ONE READER of the opt-in posture map, and therefore the ONE place a connector that never
    /// opted in is given the §12.4.5.9.4 GR1 b) 2. default rather than a different code path. Every governed
    /// record verb starts here. It used to be a <c>TryGetValue</c> whose MISS returned the ungoverned body, and
    /// the effect was that a connector with no LOCK MODE clause could read, rewrite and DELETE a record another
    /// connector held locked, answering '00' where §9.1.16 makes the record inaccessible — the same
    /// opt-in-overlay shape kb/Work PB321 removed from OPEN, one layer down (kb/Work PB669).</summary>
    private ConnectorShare ShareOf(string name) =>
        _connectorShares.TryGetValue(name, out var meta) ? meta : ImplementorDefaultShare;

    /// <summary>Register a SELECTed file's declared SHARING / LOCK MODE (emitted right after registration, only
    /// for a file that carries either clause). <paramref name="sharing"/> is null for a file with a LOCK MODE
    /// clause and no SHARING clause: the file control entry then specifies no sharing and the OPEN's mode
    /// gives the connector its <see cref="ImplementorDefaultSharing"/>. This is the RECORD-LOCKING
    /// posture only; every connector, registered here or not, is arbitrated against Table 19 by
    /// <see cref="SharedOpenAttempt"/>.</summary>
    public void RegisterSharing(string name, FileSharing? sharing, FileLockMode lockMode, bool multiple) =>
        // ⛔ THE POSTURE IS RECORDED HERE AND DERIVED AT THE OPEN, never written to the connector here as well.
        // A registration that ran before its connector existed would otherwise leave this map and the connector
        // disagreeing silently, so `SharedOpenAttempt` reads everything it needs off this map immediately before
        // `c.Open(mode)`, which is the ONLY caller of it (kb/Work PB713 moved the write-ordinal seeding onto the
        // same read, which is what made a second answer worth removing).
        // ⛔ AND WHAT IT RECORDS IS NOT THE HOST HANDLE'S SHARE MODE (kb/Work PB740). The boolean this method
        // used to set — "did this SELECT write a SHARING or LOCK MODE clause?" — was spent on §9.1.15's OTHER
        // audience as well, the file lock against other RUN UNITS, and answered it backwards. That share mode is
        // now DERIVED, by `FileLockPosture` from the arbitrated sharing mode and the connectors Table 19 has
        // already admitted, and handed down as `FileConnector.HostShare`; this map's record-locking posture is
        // one of its inputs and never its author.
        _connectorShares[name] = new ConnectorShare(sharing, lockMode, multiple);

    // ── APPLY COMMIT, COMMIT and ROLLBACK (DESIGN-runtime-library.md 2.2.1.10; kb/Work PB3337) ─────────────────────

    /// <summary>⛔ PUT A KEYED FILE CONNECTOR UNDER COMMIT CONTROL — the activation of a runtime element whose APPLY COMMIT
    /// clause names it (§12.4.6.3.4; the compiler's half is kb/Work PB3338). §9.1.16: <i>"For files subject to commit and
    /// rollback the LOCK clause and the UNLOCK statement shall not be specified and instead an implicit LOCK MODE IS
    /// AUTOMATIC WITH LOCK ON MULTIPLE RECORDS is applied automatically."</i> — so the connector's record-locking posture
    /// is that one, keeping the file control entry's SHARING (a READ then locks its record in the region, §12.4.5.9.4 GR4);
    /// and its WRITE, REWRITE and DELETE keep the record's before-image and HOLD the record (and every unique key value the
    /// change frees) in the run unit's transaction on the physical file (<see cref="KeyedConnector.CommitControlled"/>).
    /// Its OPEN sets nothing in the file: commit control is a transaction in the TRANSACTIONS tree, not a property of the
    /// file. A SEQUENTIAL connector's changes are its run unit's transaction journal (2.2.2, kb/Work PB3340), and its posture
    /// with no SHARING clause is <c>SHARING WITH NO OTHER</c> for every open mode, INPUT included (2.2.2.4: the implementor's,
    /// §9.1.15 — the one handle serves every mode of the transaction, and only the stricter default serves OUTPUT).</summary>
    public void RegisterCommitControl(string name)
    {
        var c = Require(name);
        _shareBeforeCommitControl.TryAdd(name, _connectorShares.TryGetValue(name, out var declared) ? declared : null);
        var share = ShareOf(name) with { LockMode = FileLockMode.Automatic, Multiple = true };
        switch (c)
        {
            case KeyedConnector keyed:
                keyed.CommitControlled = true;
                break;
            case SequentialConnector sequential:
                sequential.CommitControlled = true;
                share = share with { Sharing = share.Sharing ?? FileSharing.NoOther };
                break;
            default:
                throw MisroutedVerb("APPLY COMMIT", name, c);
        }
        _connectorShares[name] = share;
    }

    /// <summary>The file control entry's own record-locking posture of each connector put under commit control
    /// (<see cref="RegisterCommitControl"/>), which <see cref="ReleaseCommitControl"/> puts back; null when the entry
    /// declared none (<see cref="ImplementorDefaultShare"/>).</summary>
    private readonly Dictionary<string, ConnectorShare?> _shareBeforeCommitControl = new(CobolNames.Comparer);

    /// <summary>⛔ TAKE A FILE CONNECTOR OUT OF COMMIT CONTROL — the inverse of <see cref="RegisterCommitControl"/>, run when
    /// the last APPLY COMMIT clause naming it is DEACTIVATED (kb/Work PB3541; <see cref="TransactionControl"/>): §14.9.7.4
    /// GR3, <i>"The COMMIT statement also deactivates any APPLY COMMIT clauses in exited initial programs and canceled
    /// runtime elements"</i>, and §14.9.36.4 GR6 for the ROLLBACK. From then on the file is no longer "specified in" an
    /// active clause, so its changes are no longer the next ROLLBACK's (§14.9.36.4 GR3: <i>"All files and data-items
    /// referenced in active APPLY COMMIT clauses are restored"</i>), §14.6.13.1.3 item 8 no longer applies to it, and
    /// §9.1.16's implicit AUTOMATIC posture gives way to the file control entry's own. The COMMIT or ROLLBACK that
    /// deactivates the clause has already ended the file's transaction and given back what its CLOSE kept; a transaction
    /// whose rollback the medium refused stays owed, found by its open transaction id and not by this flag
    /// (<see cref="TransactionHolders"/>).</summary>
    internal void ReleaseCommitControl(string name)
    {
        if (_files.TryGetValue(name, out var c) && c is KeyedConnector keyed) keyed.CommitControlled = false;
        if (c is SequentialConnector sequential) sequential.CommitControlled = false;
        if (!_shareBeforeCommitControl.Remove(name, out var declared)) return;
        if (declared is { } share) _connectorShares[name] = share;
        else _connectorShares.Remove(name);
    }

    /// <summary>Whether <paramref name="name"/>'s connector is under commit control — "a file subject to an active APPLY
    /// COMMIT clause" (§14.6.13.1.3 item 8, <see cref="TransactionControl.RollbackOnFatal"/>).</summary>
    internal bool IsCommitControlled(string name) =>
        _files.TryGetValue(name, out var c) && c is KeyedConnector { CommitControlled: true } or SequentialConnector { CommitControlled: true };

    /// <summary>The id of the run unit's open transaction on <paramref name="name"/>'s physical file, 0 while none (the
    /// witnesses' seam, 2.2.1.12's transaction cases).</summary>
    internal ulong TransactionOf(string name) => Require(name) is KeyedConnector k ? k.OpenTransaction : 0;

    /// <summary>The witnesses' seam of a death that lands while ANOTHER run unit's statement runs (2.2.1.17's round-4
    /// table): this run unit's liveness byte on <paramref name="name"/>'s physical file is released as the operating
    /// environment releases a dead process's, and nothing else of the run unit is touched.</summary>
    internal void ReleaseLivenessForWitness(string name)
    {
        if (Require(name) is KeyedConnector k) k.ReleaseLivenessForWitness();
    }

    /// <summary>The holders of this run unit's open transactions — one per physical file — in the order of the run unit's
    /// open stores (2.2.1.10, <i>"Files are committed one at a time, in the order of the run unit's open stores"</i>): the
    /// place <see cref="StoreTable"/> stamped on each file's store (<see cref="KeyedStore.OpenOrder"/>), whoever holds the
    /// transaction now, so a state a CLOSE kept and the registry retired (<see cref="_retired"/>: its connector displaced, or
    /// associated with another file and perhaps holding a later file's transaction open) keeps the place of its file. Neither
    /// the registration order nor the order of the two collections is that order (review finding R1-2 of kb/Work PB3494). A
    /// file both a retired state and a registered connector reach has ONE transaction, told by the retired state's name.
    /// <para>⛔ A file is told by its STORE, never by its path (kb/Work PB3533): the transaction lives in the run unit's store of
    /// the physical file, and two physical files can stand at one path in a run unit - one removed or renamed away outside it
    /// while a connector still holds it, and the one made at the path since (<see cref="StoreTable"/>) - each with a transaction
    /// of its own, which a path would have folded into one, leaving the other uncommitted.</para></summary>
    private List<TransactionHolder> TransactionHolders()
    {
        var seen = new HashSet<KeyedStore>(ReferenceEqualityComparer.Instance);
        var owners = new List<TransactionHolder>();
        foreach (var (name, kept) in _retired)
            if (kept.OpenTransaction != 0 && seen.Add(kept.Store)) owners.Add(new(name, null, kept, kept.Store.OpenOrder));
        foreach (var (name, c) in _files)
            if (c is KeyedConnector { TransactionStore: { Transaction: not 0 } store } k && seen.Add(store))
                owners.Add(new(name, k, null, store.OpenOrder));
        // ⛔ A SEQUENTIAL FILE'S TRANSACTION (2.2.2.6): the registry's journal, in the same order, told by the name of the
        // covered connector that holds the file (the one the program tests), or of any covered connector on its path.
        if (_sequential is { } sequential)
            foreach (var (journal, order) in sequential.Open)
                owners.Add(new(CoveredNameOf(journal.HostPath) ?? "", null, null, order, journal));
        return [.. owners.OrderBy(h => h.Order)];
    }

    /// <summary>The name of a covered sequential connector on the physical file at <paramref name="hostPath"/>: an open one first,
    /// then one that holds the handle its CLOSE kept, then any.</summary>
    private string? CoveredNameOf(string hostPath)
    {
        string? any = null;
        foreach (var (name, c) in _files)
        {
            if (c is not SequentialConnector { CommitControlled: true } s || s.HostPath.Length == 0
                || !HostFile.PhysicalFileComparer.Equals(Path.GetFullPath(s.HostPath), hostPath))
                continue;
            if (s.IsOpen) return name;
            any ??= name;
        }
        return any;
    }

    /// <summary>The covered sequential connectors on the physical file at <paramref name="hostPath"/>, the retired ones included:
    /// the open one, whose writer the COMMIT and ROLLBACK flush and whose handle they reach the file through, before the closed
    /// ones that kept theirs.</summary>
    private IEnumerable<SequentialConnector> CoveredOn(string hostPath) =>
        _files.Values.OfType<SequentialConnector>().Concat(_retiredSequential)
            .Where(s => s.CommitControlled || _retiredSequential.Contains(s))
            .Where(s => s.HostPath.Length > 0 && HostFile.PhysicalFileComparer.Equals(Path.GetFullPath(s.HostPath), hostPath))
            .OrderBy(s => s.IsOpen ? 0 : 1);

    /// <summary>⛔ COMMIT one sequential file's transaction (DESIGN-runtime-library.md 2.2.2.6): the covered writers' buffers
    /// released, then the journal's three regions through the handle of the covered connector that holds the file (null when a
    /// DELETE FILE removed it). A reversed decision (region 3) is rolled back at once; a COMMIT that leaves the transaction open
    /// answers <see cref="CommitOutcome.Refused"/> or <see cref="CommitOutcome.Unacknowledged"/>, and item 8's ROLLBACK follows.</summary>
    private CommitOutcome CommitSequential(SequentialJournal journal)
    {
        var covered = CoveredOn(journal.HostPath).ToList();
        try { foreach (var c in covered) c.FlushForCommit(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return CommitOutcome.Refused; }   // region 1
        var outcome = journal.Commit(covered.Select(c => c.HandleForCommit).FirstOrDefault(h => h is not null));
        switch (outcome)
        {
            case CommitOutcome.Committed:
                Sequential.End(journal);
                foreach (var c in covered) c.AfterCommit();
                break;
            case CommitOutcome.Reversed:
                _ = RollbackSequential(journal);
                break;
        }
        return outcome;
    }

    /// <summary>⛔ ROLL BACK one sequential file's transaction (2.2.2.7): under the entry mutex, the covered writers' buffers
    /// released first (their bytes are the transaction's own appends, which the rollback removes), the journal's one sequence
    /// through the handle of the covered connector that holds the file (or one opened for the purpose), then each covered
    /// connector's state put right (<see cref="SequentialConnector.AfterRollback"/>). A refused step leaves the rollback OWED:
    /// the transaction stays open for the next COMMIT, ROLLBACK or the run unit's end.</summary>
    private StoreStatus RollbackSequential(SequentialJournal journal)
    {
        var covered = CoveredOn(journal.HostPath).ToList();
        try
        {
            foreach (var c in covered) c.FlushForRollback();
            using var mutex = Sequential.HoldEntryMutex(journal.HostPath);
            IJournalData data = covered.FirstOrDefault() is { } holder ? holder : new ClosedFileData(Medium, journal.HostPath);
            if (!journal.Rollback(data)) return StoreStatus.PermanentError;
            (data as IDisposable)?.Dispose();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return StoreStatus.PermanentError; }
        Sequential.End(journal);
        foreach (var c in covered) c.AfterRollback();
        return StoreStatus.Ok;
    }

    /// <summary>The physical file as the ROLLBACK of a transaction no covered connector holds any more reaches it: a short-lived
    /// handle of the run unit's medium.</summary>
    private sealed class ClosedFileData(IHostMedium medium, string hostPath) : IJournalData, IDisposable
    {
        private FileStream? _handle;

        public FileStream Writable(bool create) =>
            _handle ??= medium.OpenAuxiliary(hostPath, create ? FileMode.OpenOrCreate : FileMode.Open, FileAccess.ReadWrite);

        public void Release()
        {
            _handle?.Dispose();
            _handle = null;
        }

        public void Dispose() => Release();
    }

    /// <summary>One physical file's transaction of this run unit, the name it is told by and its file's place in the order of
    /// the open stores: the connector that holds it open, or — a connector that is closed and associated with another file, or
    /// replaced — what its CLOSE kept (<see cref="_retired"/>).</summary>
    private readonly record struct TransactionHolder(string Name, KeyedConnector? Connector, KeptFile? Kept, long Order,
        SequentialJournal? Journal = null)
    {
        public CommitOutcome Commit(FileRegistry registry) =>
            Journal is { } j ? registry.CommitSequential(j) : Connector is { } c ? c.CommitTransaction() : Kept!.CommitTransaction();

        public StoreStatus Rollback(FileRegistry registry) =>
            Journal is { } j ? registry.RollbackSequential(j) : Connector is { } c ? c.RollbackTransaction() : Kept!.RollbackTransaction();
    }

    /// <summary>The file-name whose I-O status a refused COMMIT or ROLLBACK is told through (§9.1.13.1): the connector registered
    /// under <paramref name="name"/> now, which is the one the program tests.</summary>
    private void TellPermanentError(string name)
    {
        if (_files.TryGetValue(name, out var c)) c.SetStatus(FileStatusCode.PermanentError);
    }

    /// <summary>⛔ COMMIT (§14.9.7.4 GR3: <i>"The execution of the COMMIT statement permanently applies any changes made to
    /// the files specified in all the active APPLY COMMIT clauses and releases all record locks associated with those
    /// files."</i>; DESIGN-runtime-library.md 2.2.1.10). For every physical file with a transaction of this run unit, one at
    /// a time in the order of the run unit's open stores (per file, by the owner's decision of 2026-10-19, kb/Work PB3454):
    /// the promotion that decides it. Then the region's READ locks of every connector under commit control (§9.1.16:
    /// <i>"A COMMIT statement releases all record locks for files subject to commit and rollback"</i>) and the handles kept
    /// by connectors closed under commit control (§9.1.15: <i>"the file lock is removed by the COMMIT statement"</i>).
    /// <para>COMMIT has no I-O status (§9.1.13.1), so a COMMIT the medium refuses is told through the refused FILE: its status
    /// '30', the permanent error of §9.1.13.1 in effect, and the name returned — the caller raises EC-I-O-PERMANENT-ERROR
    /// for it, the fatal condition for which §14.6.13.1.3 item 8 executes a ROLLBACK unconditionally (the files after it
    /// are not committed; those before it are). Null when every file committed.</para></summary>
    public string? Commit()
    {
        foreach (var holder in TransactionHolders())
            if (holder.Commit(this) != CommitOutcome.Committed)
            {
                TellPermanentError(holder.Name);
                return holder.Name;
            }
        ReleaseCommitLocks();
        return null;
    }

    /// <summary>⛔ ROLLBACK (§14.9.36.4 GR3: <i>"All files and data-items referenced in active APPLY COMMIT clauses are
    /// restored to the state they were in at the execution of the last COMMIT statement or the start of the run unit if no
    /// COMMIT statements had previously been executed."</i>; 2.2.1.10) — the files' half (the data items are the compiler's,
    /// kb/Work PB3338, and the termination path's rollback restores files only, §14.6.12): for every physical file with a
    /// transaction of this run unit, ONE statement that re-applies the run unit's before-images and drops its holdings,
    /// touching no other run unit's record; then the region's READ locks (§9.1.16: <i>"A ROLLBACK statement releases all
    /// record locks for files subject to commit and rollback"</i>) and the kept handles. A file whose ROLLBACK the medium
    /// refuses keeps its transaction (the dead-owner step restores it once the run unit is gone) and is told as COMMIT's is:
    /// '30' on the file, its name returned; every other file is still rolled back.</summary>
    public string? Rollback()
    {
        string? refused = null;
        foreach (var holder in TransactionHolders())
            if (holder.Rollback(this) != StoreStatus.Ok)
            {
                TellPermanentError(holder.Name);
                refused ??= holder.Name;
            }
        ReleaseCommitLocks();
        return refused;
    }

    /// <summary>What COMMIT and ROLLBACK both release: every region record lock of a connector under commit control (its
    /// READ locks; a change's lock is a holding, already gone with the transaction) and every handle kept by a connector
    /// closed under commit control.</summary>
    private void ReleaseCommitLocks()
    {
        List<Exception>? failures = null;
        ReleaseAllKept(ref failures);
        if (failures is [var only]) ExceptionDispatchInfo.Throw(only);
        if (failures is not null)
            throw new AggregateException("the release of what a CLOSE under commit control kept failed for more than one file", failures);
    }

    /// <summary>Give back everything every connector under commit control holds past a CLOSE (<see cref="ReleaseKeptOf"/>), the
    /// retired ones included (<see cref="_retired"/>), whether or not one release fails: a failure is collected into
    /// <paramref name="failures"/>, never allowed to leave a later connector holding its handle. A release that threw has
    /// spent its kept state all the same (<see cref="KeyedConnector.ReleaseKept"/>), so the list ends empty.</summary>
    private void ReleaseAllKept(ref List<Exception>? failures)
    {
        foreach (var (name, c) in _files.ToArray())
        {
            try
            {
                if (c is KeyedConnector { CommitControlled: true } k) ReleaseKeptOf(name, k);
                else if (c is SequentialConnector { CommitControlled: true } s)
                {
                    ReleaseAllForConnector(name);
                    s.ReleaseKept();
                }
            }
            catch (Exception failure) { (failures ??= []).Add(failure); }
        }
        foreach (var s in _retiredSequential.ToArray())
        {
            try { s.ReleaseKept(); }
            catch (Exception failure) { (failures ??= []).Add(failure); }
        }
        _retiredSequential.Clear();
        foreach (var (name, kept) in _retired.ToArray())
        {
            try
            {
                // The record locks the host could not publish are this run unit's table alone and are its by name, as a registered
                // connector's are (ReleaseKeptOf) - a retired connector's key can leave the registry (CloseAndDrop) or name another
                // file by now (Associate), so the kept state's own physical-file state is the way to them (review finding R1-3).
                if (kept.Physical is { } physical) PhysicalFileTable.ReleaseAllForConnector(physical, name);
                kept.Release();
            }
            catch (Exception failure) { (failures ??= []).Add(failure); }
        }
        _retired.Clear();
    }

    /// <summary>⛔ THE ONE GIVING BACK of what a registered keyed connector holds past a CLOSE under commit control (§9.1.16, the
    /// COMMIT's and the ROLLBACK's; §9.1.18.5, the run unit's end): its record locks — the table's, by name, which also
    /// covers a lock no host published — and then everything the CLOSE kept (<see cref="KeyedConnector.ReleaseKept"/>: the
    /// locks its endpoint published, the handle, the file lock, the attachment). A connector retired from the registry is
    /// ended in <see cref="ReleaseAllKept"/> by its own physical-file state, because its key may no longer name it.</summary>
    private void ReleaseKeptOf(string name, KeyedConnector k)
    {
        ReleaseAllForConnector(name);
        k.ReleaseKept();
    }

    /// <summary>The run unit's transactions at its TERMINATION (§9.1.18.5: <i>"When a run unit terminates normally, all file
    /// changes are permanently committed. When a run unit terminates abnormally, all file changes made since the previous
    /// commit or, if none, the beginning of the run unit are rolled back for all files for which there are active APPLY
    /// COMMIT clauses."</i>): committed, or — abnormal — the file-only ROLLBACK of §14.6.12 (a COMMIT refused at the end is
    /// rolled back too, by §14.6.13.1.3 item 8's rule). What the runtime cannot reach (a kill, a power loss) is the
    /// dead-owner step's.</summary>
    internal void EndTransactions(bool abnormal)
    {
        if (abnormal || Commit() is not null) Rollback();
    }

    /// <summary>Declare a SELECTed file's record area NATIONAL (§14.9.30.4 GR15; emitted right after
    /// registration, only for a file whose record area is of category national — kb/Work PB327). It changes the
    /// short-record pad from the alphanumeric space to the national one.</summary>
    public void RegisterNationalArea(string name)
    {
        if (_files.TryGetValue(name, out var c)) c.NationalRecordArea = true;
    }

    /// <summary>Declare that the file's RECORD clause has a DEPENDING phrase (§14.9.51.4 GR21/GR22; emitted right after
    /// registration, only for such a file — kb/Work PB1191): its line sequential records are written at data-name-1's
    /// length, so the connector never trims their trailing spaces.</summary>
    public void RegisterRecordLengthFromDepending(string name)
    {
        if (_files.TryGetValue(name, out var c)) c.RecordLengthFromDepending = true;
    }

    /// <summary>Declare the file's RESERVE clause (ISO §12.4.5.14.3 GR1: <i>"If the RESERVE clause is specified, the
    /// number of input-output areas allocated is equal to the value of integer-1"</i>; emitted right after
    /// registration, only for a file control entry that writes the clause — kb/Work PB643). The compiler has
    /// already refused an integer-1 above <see cref="HostFile.MaxInputOutputAreas"/> (COBOLNET2897), so a value out
    /// of range here is a broken caller, not a program's error.</summary>
    public void RegisterReserve(string name, int areas)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(areas, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(areas, HostFile.MaxInputOutputAreas);
        if (_files.TryGetValue(name, out var c)) c.InputOutputAreas = areas;
    }

    /// <summary>Declare the file's §13.18.13 CODE-SET conversion (§13.18.13.4 GR2: the coded character set used
    /// to represent data on the storage medium, established at OPEN and constant thereafter).
    /// <paramref name="toNative"/> is §12.3.7.4 GR7 i's correspondence — the native character each medium code
    /// unit represents — which the COMPILER computed from the named alphabet's coded character set.</summary>
    public void RegisterCodeSet(string name, char[] toNative)
    {
        if (_files.TryGetValue(name, out var c)) c.CodeSet = new CodeSetConversion(toNative);
    }

    /// <summary>OPEN with an explicit SHARING override and/or a RETRY phrase (§14.9.27) — the emitter's entry
    /// point when the OPEN statement itself carries a sharing/retry phrase.</summary>
    public void OpenShared(string name, FileOpenMode mode, bool hasSharingOverride, FileSharing sharingOverride,
        FileRetryKind retryKind, long retryAmount, OpenTapePhrase tape, string assign, bool assignDynamic,
        LinagePage? page)
        => OpenCore(name, mode, hasSharingOverride ? sharingOverride : null, retryKind, retryAmount, tape,
            assign, assignDynamic, page);

    /// <summary>⛔ §14.9.27.4 GR2 IS DECIDED FIRST, AND ALONE (kb/Work PB2748): <i>"The file connector referenced by
    /// file-name-1 shall not be open. If it is open, the execution of the OPEN statement is unsuccessful and the I-O
    /// status associated with file-name-1 is set to '41'"</i>, and GR25: <i>"If the execution of the OPEN statement is
    /// unsuccessful, the file is not affected"</i>. An OPEN of an open connector therefore reaches nothing the OPEN
    /// would otherwise establish — no Table 19 arbitration (which answered '61' over GR2's '41' when a sibling
    /// connector's mode refused the new request; GR2 precedes the sharing rules, docs/CONFORMANCE.md DOC-A.1-104),
    /// no record-locking posture from an OPEN phrase, no REVERSED or LINAGE request, no physical-file association and
    /// no sharing mode: each of those used to be written onto the STILL-OPEN connector before
    /// <see cref="FileConnector.Open"/> refused it, so the unsuccessful OPEN changed how the open file shared, locked
    /// and released its records. The answer itself is the connector's own already-open arm (its §9.1.13.1 permanent
    /// error replay first, kb/Work PB1541, else '41').</summary>
    private bool RefusedAsAlreadyOpen(string name, FileOpenMode mode)
    {
        var c = Require(name);   // an unregistered name is a COMPILER defect and LOUD (kb/Work PB140)
        if (!c.IsOpen) return false;
        _ = c.Open(mode);
        return true;
    }

    /// <summary>⛔ THE ONE OPEN DISPATCH (ISO §14.9.27). Every OPEN — phrase-bearing or plain — arbitrates against
    /// the physical-file registry, because §9.1.15's gate is written over the physical file and not over the
    /// connectors that opted in: <i>"Before access to a shared physical file is allowed through an OPEN statement,
    /// the sharing mode and the open mode of that OPEN statement shall be allowed by ALL OTHER FILE CONNECTORS
    /// that are currently associated with the physical file"</i>. When the table was an opt-in overlay consulted
    /// only for connectors carrying a SHARING/LOCK MODE clause, declaring <c>SHARING WITH NO OTHER</c> made a file
    /// strictly LESS protected than declaring nothing — the clause turned the connector's OS handle shareable and
    /// then handed arbitration to a table that could not see the plain connector coming (kb/Work PB321).
    /// <para>The RETRY discipline wraps the whole attempt (§14.7.9.3): with no RETRY phrase GR4a makes no further
    /// attempt, so the plain <see cref="Open"/> is the same code path with <see cref="FileRetryKind.None"/>. The
    /// GC-deferred per-object close drains first, on this (mutator) thread, for both entries — it used to drain
    /// only for the plain one.</para>
    /// <para>⛔ A SHARING OR RETRY PHRASE REGISTERS NO RECORD-LOCKING POSTURE (kb/Work PB3372). Whether the opened
    /// connector is a record-locking participant is derived at <see cref="SharedOpenAttempt"/> from the posture the
    /// file control entry registered and the sharing mode THIS open is arbitrated under
    /// (<see cref="RecordLocksSetUnder"/>); an OPEN phrase can change the mode but cannot write a LOCK MODE clause, and
    /// §12.4.5.9.4 GR1 b) 1. says that with a SHARING phrase and no LOCK MODE clause <i>"no record locks are set"</i>.
    /// The phrase used to register a lock-less posture on the spot, which made the connector a participant (and so
    /// paid the whole-file record count of a sequential <c>OPEN EXTEND</c>) for the life of the run unit.</para>
    /// <para><paramref name="tape"/> carries the statement's WITH NO REWIND phrase, applied by
    /// <see cref="NoRewindPhraseEffect"/> AFTER the arbitrated open. It rides here rather than at either entry
    /// point because SHARING/RETRY and NO REWIND are independent phrases of one general format (§14.9.27.2), so
    /// an OPEN may write both and the '07' overlay has to reach the arbitrated path as well as the plain one
    /// (kb/Work PB317). The overlay is self-guarding: §14.9.27.4 GR25 a) owns an unsuccessful open's status, so
    /// <see cref="NoRewindPhraseEffect"/> writes '07' only over a status whose first digit is '0'.</para></summary>
    private void OpenCore(string name, FileOpenMode mode, FileSharing? sharingOverride,
        FileRetryKind retryKind, long retryAmount, OpenTapePhrase tape, string assign, bool assignDynamic,
        LinagePage? page)
    {
        DrainPendingObjectCloses();   // reclaim any GC-finalized per-object connectors on this (mutator) thread first
        if (RefusedAsAlreadyOpen(name, mode)) return;   // §14.9.27.4 GR2 — before anything below touches the connector
        // §14.9.27.4 GR26 → §12.4.5.3 GR3, and BEFORE the Table-19 arbitration: a sharing conflict is defined
        // over "another file connector" holding THE PHYSICAL FILE this OPEN names (§9.1.13.9), so the association
        // this statement establishes has to stand before the physical-file table is consulted. OUTSIDE the RETRY
        // loop: data-name-1's content is read once per OPEN statement (GR3 b), not once per retry attempt.
        // ⛔ ONE call site for BOTH entry points — the plain Open and OpenShared both funnel through here
        // (kb/Work PB324 landed on top of PB321's unified dispatch).
        if (!Associate(name, assign, assignDynamic)) return;
        // The REVERSED phrase reaches the connector BEFORE its OPEN body runs, because the position it asks for
        // needs a record count, and nothing after a successful open may read the physical file (the PB713 rule
        // below, in SharedOpenAttempt; kb/Work PB1512). Set on every open, so it describes THIS statement only.
        if (Require(name) is SequentialConnector sequential)
        {
            sequential.ReversedRequested = tape == OpenTapePhrase.Reversed;
            // The executing element's LINAGE operands are null exactly when its FD has no LINAGE clause
            // (LinageArg), and a LINAGE file's line end is the host newline, not the record sequential print
            // stream's line feed (SequentialConnector.SelectLineEnd, kb/Work PB1664).
            sequential.HasLinageClause = page is not null;
        }
        // SharedOpenAttempt sets the connector status on the terminal attempt, and RetryLoop lands an exhausted
        // retry on the CONFLICT'S OWN status (§14.7.9.3 closing paragraph → §9.1.13.9 item 1 = '61'), so there is
        // nothing left to override afterwards — the former `if (status == Deadlock) SetStatusOf(…)` line existed
        // only to re-assert the '52' RetryLoop used to manufacture (kb/Work PB142).
        _ = RetryLoop(() => SharedOpenAttempt(name, mode, sharingOverride, page), retryKind, retryAmount);
        // ⛔ THE ONE TAPE-PHRASE EFFECT SITE, one arm per §14.9.27.2 alternative and never a second copy
        // (kb/Work PB317 for NO REWIND, kb/Work PB668 for REVERSED). Both arms run AFTER the arbitrated open
        // because both describe what the OPEN leaves behind — a status overlay and a file position — and both
        // are self-guarding on an unsuccessful open, whose status and whose "file is not affected" §14.9.27.4
        // GR25 a) owns.
        switch (tape)
        {
            case OpenTapePhrase.NoRewind: NoRewindPhraseEffect(name); break;
            case OpenTapePhrase.Reversed: ReversedPhraseEffect(name); break;
        }
    }

    /// <summary>The arbitrated OPEN body — one attempt under the RETRY discipline. Returns the resulting I-O status
    /// and who holds a conflict; on a Table-19 conflict returns 61 without opening the connector, leaving the file
    /// <i>"not affected"</i> (§14.9.27.4 GR25).</summary>
    private RetryAttempt SharedOpenAttempt(string name, FileOpenMode mode, FileSharing? sharingOverride, LinagePage? page)
    {
        var c = Require(name);   // an unregistered name is a COMPILER defect and LOUD (kb/Work PB140)
        if (_locked.Contains(name)) { c.SetStatus(FileStatusCode.FileLocked); return RetryAttempt.InRunUnit(FileStatusCode.FileLocked); }  // ≤2014 CLOSE WITH LOCK
        // §9.1.15 / §14.9.27.4 GR23: the OPEN's SHARING phrase overrides the file control entry's SHARING
        // clause; with neither, the implementor default, which is a function of THIS open's mode (see
        // ImplementorDefaultSharing) and so is one of the three modes Table 19 prints.
        var posture = ShareOf(name);
        FileSharing sharing = sharingOverride ?? posture.Sharing ?? ImplementorDefaultSharing(mode);
        var st = _physical.For(c.HostPath);
        foreach (var (other, existing) in st.Open)
        {
            if (CobolNames.Same(other, name)) continue;
            if (Table19.Conflicts(request: (sharing, mode), existing))
            {
                c.SetStatus(FileStatusCode.FileSharingConflict);   // 61 — §9.1.13.9 item 1
                return RetryAttempt.InRunUnit(FileStatusCode.FileSharingConflict);   // held by this run unit
            }
        }
        // ⛔ The registration is gated on the STATUS, not on `IsOpen`. A re-OPEN of a connector that is already
        // open is unsuccessful with '41' (§9.1.13.7 item 1) and FileConnector.Open leaves the connector in its
        // ORIGINAL mode — but `IsOpen` is true throughout, so gating on it re-registered the connector under the
        // mode and sharing of the FAILED request and every later arbitration used them (kb/Work PB321).
        // §9.1.15 / §12.4.5.9.4 participation, derived from the ONE rule that says when record locks are set
        // (RecordLocksSetUnder — the question LocksEffective asks at every I-O statement), at the one moment it is
        // read: for THIS open's arbitrated sharing mode, so a SHARING clause alone, a SHARING or RETRY phrase and the
        // NO OTHER mode (GR1 a), GR1 b) 1., GR3) all leave the connector a non-participant (kb/Work PB3372).
        // OpenCore consults it for the release-ordinal mint (kb/Work PB713 put the write-ordinal seeding on it),
        // and this line is its only writer — so it cannot outlive, or lag, the posture map. What it hands over
        // is the physical file's own shared STATE, not a bare bit (kb/Work PB739): a participant needs the
        // §14.9.51.4 GR19 release-ordinal mint that lives on it, and participation IS this being non-null, so
        // the two cannot disagree. It no longer says anything about the HOST HANDLE — that is the file lock
        // below, derived rather than declared (kb/Work PB740).
        // ⛔ TWO FACTS, ONE WRITER (kb/Work PB753). The physical file's shared state goes to EVERY connector —
        // Table 19 has just arbitrated this open over it, and the read-coherence rule written on it
        // (State.ReleaseGeneration) is a rule about the MEDIUM, which a clause-less pair sharing one file
        // under kb/Work PB740 is on exactly as much as a SHARING WITH ALL OTHER pair. The second argument is
        // the RECORD-LOCKING posture, and only the §9.1.16 questions read it through `SharedPhysical`.
        ReclaimRetired(name, c);
        c.AssociatePhysical(st, RecordLocksSetUnder(posture, sharing));
        // ⛔ AND THE §9.1.15 FILE LOCK, BEFORE THE HANDLE THAT CARRIES IT EXISTS (kb/Work PB740). The share mode of
        // a host handle is fixed when the handle is created, so the posture of THIS connector's sharing mode is
        // handed down here, ahead of c.Open. It is the posture of the mode alone: Table 19 has just admitted this
        // open against every connector already open on the physical file, and for every pair of the three modes it
        // admits, each side's base posture already admits the other's access (FileLockPostureDriftTests proves
        // that over the printed table), so no sibling's handle is ever rebuilt (kb/Work PB322 removed the
        // widening that existed only for an undetermined default). The SHARING MODE itself is what is handed down:
        // the connector derives its handle's share mode from it (FileConnector.HostShare) and, in c.Open, the
        // cross-run-unit region lock (RunUnitFileLock, kb/Work PB833), so the two halves of the file lock cannot
        // disagree about which mode this OPEN is under.
        c.HostSharing = sharing;
        string status = c.Open(mode);
        if (status[0] == '0')   // the success family '00'/'05'/'07' — §9.1.13.2
        {
            st.Open[name] = (sharing, mode);   // register only a successful open
            // ⛔ NOTHING AFTER c.Open(mode) MAY TOUCH THE PHYSICAL FILE (kb/Work PB713). A record-locking
            // participant that is SEQUENTIAL needs its write-ordinal base (§9.1.16 lock identity for the records
            // it releases; §14.9.51.4 GR18/GR19 — EXTEND appends continue the numbering the file had "when it
            // was opened"), and that base used to be measured HERE, from a second handle on a path the connector
            // had just opened for WRITE. The measurement was refused by the operating environment, and because
            // this line is OUTSIDE FileConnector.Open's try the refusal escaped the run unit as an unhandled
            // IOException instead of as §9.1.13.6 item 1's '30'. It now lives in SequentialConnector.OpenCore,
            // before that connector's own writer exists; the ONLY work left after a successful open is
            // arbitration bookkeeping and the LINAGE page model, neither of which reads a byte off the host.
            // ISO §13.18.34 GR6 b) 1 — the LINAGE operand values are read "at the completion of an OPEN statement
            // with the OUTPUT phrase", so the page model is established HERE, with the page the EXECUTING element's
            // own LINAGE clause evaluated to (kb/Work PB673), and only for an open that actually succeeded.
            // ⛔ AND THE PAGE MODEL CAN MAKE THE OPEN UNSUCCESSFUL (kb/Work PB526). §13.18.34 GR6 b) 1 reads the
            // operands "at the completion of" this OPEN and GR6 b) 2 applies its two value rules to them: a
            // violation sets EC-I-O-LINAGE to exist, so the statement has to report an unsuccessful I-O status
            // for §9.1.12's exception processing to see it at all. The connector STAYS OPEN — GR6 b) 2's counter
            // "remains at that value until the file is closed" and its "all subsequent WRITE statements
            // referencing the file" both presuppose an open file connector — so this replaces only the status,
            // after every piece of successful-open bookkeeping above has run.
            if (mode == FileOpenMode.Output && page is { } pg && c is SequentialConnector sq
                && sq.BeginLinagePage(pg) is { } linageStatus)
                status = linageStatus;
        }
        // A '61' from the connector's own OPEN is the host refusing the handle (FileConnector.Open's
        // HostFile.IsSharingRefusal arm), a refused file lock, or the entry step's journal holding: the Table 19 arbiter
        // above has already admitted this open against every OPEN connector of this run unit, so the holder is outside
        // it and can release (kb/Work PB1163) — unless the run unit itself holds the file against this OPEN, through what
        // a CLOSE under commit control kept or through its own transaction (RunUnitRefusesOpen), which no wait releases.
        return new RetryAttempt(status,
            HolderOutsideRunUnit: status == FileStatusCode.FileSharingConflict && !RunUnitRefusesOpen(name, c, sharing, mode));
    }

    /// <summary>⛔ DOES THIS RUN UNIT ITSELF REFUSE AN OPEN of <paramref name="name"/> in (<paramref name="sharing"/>,
    /// <paramref name="mode"/>) (kb/Work PB3340; §14.7.9.3 GR3 and kb/Work PB1163: a RETRY waits only for a holder outside the
    /// run unit, the one kind that can release while the statement runs; DOC-A.1-109)? Through what a CLOSE under commit
    /// control kept, whose Table 19 column refuses the request (<see cref="RunUnitHoldsAgainst"/>), or through its own
    /// transaction on a sequential file (<see cref="OwnTransactionRefuses"/>), which only a writable OPEN meets: an OPEN
    /// INPUT runs no entry step.</summary>
    private bool RunUnitRefusesOpen(string name, FileConnector c, FileSharing sharing, FileOpenMode mode)
    {
        var row = Table19.Row(sharing, mode);
        return RunUnitHoldsAgainst(name, c.HostPath, column => column is { } held && Table19.Cell(row, held) == OpenSharingOutcome.UnsuccessfulOpen)
            || mode is not FileOpenMode.Input && OwnTransactionRefuses(c, exclusive: mode is FileOpenMode.Output);
    }

    /// <summary>⛔ THE RUN UNIT'S OWN HOLD ON A PHYSICAL FILE (§14.9.7.4 GR4: a file lock a CLOSE under commit control kept stands
    /// until the COMMIT or ROLLBACK): whether what a CLOSE kept on the physical file at <paramref name="host"/> — by any connector
    /// but <paramref name="name"/>, registered or retired (<see cref="Retire"/>) — still holds the file and its Table 19 column
    /// (null when not known) is one <paramref name="refuses"/> picks.</summary>
    private bool RunUnitHoldsAgainst(string name, string host, Func<ExistingSharingColumn?, bool> refuses)
    {
        foreach (var (other, x) in _files)
            if (!CobolNames.Same(other, name) && !x.IsOpen && x.KeepsTheFile && HostFile.PhysicalFileComparer.Equals(x.HostPath, host)
                && refuses(x.KeptColumn))
                return true;
        foreach (var s in _retiredSequential)
            if (s.KeepsTheFile && HostFile.PhysicalFileComparer.Equals(s.HostPath, host) && refuses(s.KeptColumn)) return true;
        foreach (var (_, kept) in _retired)
            if (kept.HoldsTheFile && HostFile.PhysicalFileComparer.Equals(kept.HostPath, host) && refuses(kept.Column)) return true;
        return false;
    }

    /// <summary>The run unit's own transaction on a SEQUENTIAL file refuses a writable OPEN, or with <paramref name="exclusive"/>
    /// an OPEN OUTPUT or a DELETE FILE, of a connector that does not cover it (DESIGN-runtime-library.md 2.2.2.8: a replaced
    /// transaction refuses every writable OPEN, and any live one an exclusive request); a covered connector's own transaction is
    /// the one it changes the file in.</summary>
    private bool OwnTransactionRefuses(FileConnector c, bool exclusive) =>
        c is SequentialConnector { CommitControlled: false } && Sequential.Of(c.HostPath) is { } own && (exclusive || own.Replaced);

    /// <summary>The holder of a DELETE FILE's sharing conflict the host reported ('62', §9.1.13.9 2)): this run unit's own when
    /// it holds the file through what a CLOSE under commit control kept or through its own transaction — no RETRY wait releases
    /// that (DOC-A.1-109) — otherwise another run unit, which can (kb/Work PB1163).</summary>
    private RetryAttempt DeleteFileHeld(string name, FileConnector c) =>
        RunUnitHoldsAgainst(name, c.HostPath, static _ => true) || OwnTransactionRefuses(c, exclusive: true)
            ? RetryAttempt.InRunUnit(FileStatusCode.DeleteFileSharing)
            : RetryAttempt.OutsideRunUnit(FileStatusCode.DeleteFileSharing);

    /// <summary>⛔ THE ONE §12.4.5.9.4 GR6 SITE — <i>"Execution of any I-O statement except START releases any
    /// previously locked record in that file for that file connector"</i> — for all four governed record verbs.
    /// The rule is tied to EXECUTION, not to success: §14.9.30.4 GR11 a) restates it for READ without any
    /// success qualifier, and the standard qualifies deliberately elsewhere in the very same rule (GR11 b) "at
    /// the completion of the successful execution", GR11 c)/d) "a successfully accessed record"). It was written
    /// out four times, and the READ copy alone sat on the success side of its caller's guard, so an at-end or
    /// otherwise failing READ kept a lock the standard had already released (kb/Work PB338).
    /// <para>GR7's multiple record locking releases nothing here — <i>"a file connector is permitted to have
    /// more than one record of a file locked"</i>. <paramref name="except"/> is the mutating verbs' target,
    /// whose own lock their statement rules release at COMPLETION instead (§14.9.35.4 GR12 a) 1.,
    /// §14.9.10.4 GR7 a) 1.).</para></summary>
    private static void ReleasePriorRecordLocks(ConnectorShare meta, PhysicalFileTable.State st, string name,
        string? except = null)
    {
        if (meta.Multiple) return;                                              // §12.4.5.9.4 GR7
        if (st.RecordLocks.Count == 0) return;   // nothing to release — and no LINQ walk on the hot path
        if (except is { Length: > 0 } keep) PhysicalFileTable.ReleaseAllExcept(st, name, keep);
        else PhysicalFileTable.ReleaseAllForConnector(st, name);
    }

    /// <summary>⛔ WHETHER A GOVERNED STATEMENT ASKS NO §9.1.16 LOCK QUESTION BEFORE ITS VERB: no record of the physical file
    /// is locked, or the connector's PERMANENT ERROR is in effect. §9.1.13.1 keeps that condition <i>"in effect for all
    /// subsequent input-output operations on the file"</i>, and docs/CONFORMANCE.md DOC-A.1-105 makes it outrank every other
    /// status the statement would have taken — a '51' for a record another connector holds among them — and says the
    /// statement <i>"does not touch the physical file"</i>: so no peek reads the record's identity from the file, no
    /// conflict check answers '51' or waits out a RETRY, and the verb replays the condition through its own precondition
    /// (<see cref="FileConnector.PermanentErrorReplay"/>; kb/Work PB3336 review R1-2). The READ peeks ask it here; the
    /// REWRITE and DELETE bodies reach it through <see cref="MutationTarget"/>, so the permanent-error half of the rule is
    /// written once, in <see cref="PermanentErrorOutranksLocks"/> (kb/Work PB3416).</summary>
    private static bool NoLockQuestion(PhysicalFileTable.State st, FileConnector c) =>
        PermanentErrorOutranksLocks(c) || NoRecordIsLocked(st, c);

    /// <summary>⛔ THE ONE WRITING OF "A PERMANENT ERROR IN EFFECT ASKS NO §9.1.16 LOCK QUESTION" (kb/Work PB3416): the
    /// condition <i>"remains in effect for all subsequent input-output operations on the file"</i> (§9.1.13.1) and
    /// docs/CONFORMANCE.md DOC-A.1-105 makes it outrank the '51' a locked record would answer. Every governed verb's
    /// lock question — the READ peeks (<see cref="NoLockQuestion"/>) and the REWRITE / DELETE target
    /// (<see cref="MutationTarget"/>) — begins here, before any lock table or host presence byte is consulted.</summary>
    private static bool PermanentErrorOutranksLocks(FileConnector c) => c.PermanentErrorInEffect is not null;

    /// <summary>⛔ THE LOCK IDENTITY OF THE RECORD A GOVERNED REWRITE OR DELETE TARGETS, taken in the one place either body
    /// takes it (§14.9.35.4 GR11 "the record identified for rewriting", §14.9.10.4 GR6 "the record identified for
    /// deletion"; kb/Work PB3416): "" when no §9.1.16 question can have a non-trivial answer (<see cref="RecordLocksGovern"/>,
    /// which keeps the key- or ordinal-string allocation off the plain path — kb/Work PB669) and while the connector's
    /// permanent error is in effect (<see cref="PermanentErrorOutranksLocks"/>: not even the host's presence byte is
    /// asked, and the verb then replays the condition through its own open-mode precondition,
    /// <see cref="FileConnector.MutationTargetRecordId"/>'s guard). A non-empty target therefore already means "no
    /// permanent error in effect", which the conflict check needs and no longer re-tests.</summary>
    private static string MutationTarget(ConnectorShare meta, PhysicalFileTable.State st, string name, FileConnector c,
        string image, RecordExtents? extents) =>
        !PermanentErrorOutranksLocks(c) && RecordLocksGovern(meta, st, name, c) ? c.MutationTargetRecordId(image, extents) : "";

    /// <summary>No record of this physical file is locked by ANY connector, so §14.9.30.4 GR9's conflict
    /// condition — <i>"the record identified for access is locked by another file connector"</i> — cannot exist
    /// however the read is going to select its record. The governed reads then skip the pre-read peek entirely:
    /// it is a second run of the organization's GR21/GR32 selection, and on a walk whose selection is already
    /// linear in the position that doubles the cost of every read on a sharing-active file for an answer that
    /// is fixed.
    /// <para>It does NOT skip the §12.4.5.9.4 GR7 ceiling, which the caller still tests after the retrieval:
    /// the RUN-UNIT limit counts locks on OTHER physical files, so an empty table here does not settle it. That
    /// is sound because GR7's denial is not the record operation conflict condition, so §14.9.30.4 GR10 a) never
    /// governed it — GR18 does, and the caller applies GR18 by invalidating the position the retrieval
    /// advanced.</para></summary>
    /// <para>Nor while a transaction may hold a record of the file IN THE FILE (a keyed store's HOLDINGS tree is not
    /// empty, DESIGN-runtime-library.md 2.2.1.10): those locks are pages, not region bytes, and the READ asks them through
    /// its peek (<see cref="HoldingsConflict"/>).</para>
    private static bool NoRecordIsLocked(PhysicalFileTable.State st, FileConnector c) =>
        st.RecordLocks.Count == 0 && !AnotherRunUnitMayLock(c) && !c.HoldingsMayExist;

    /// <summary>⛔ Does a file connector of ANOTHER run unit hold a lock on a record of this connector's physical
    /// file (kb/Work PB2660)? Only possible when this connector publishes and tests record locks across run units
    /// (<c>RecordLocks</c>) and its own sharing mode admits another connector at all — under
    /// <c>SHARING WITH NO OTHER</c> nobody else is there (§9.1.15 1), "exclusive access"), and §12.4.5.9.4 GR3 makes
    /// the LOCK MODE clause of no effect — and then the host's presence byte answers it in one question. When it
    /// does, the hot-path shortcuts below that read "this run unit's table is empty" as "no record is locked" no
    /// longer hold, and the governed verbs ask the host about the record itself.</summary>
    private static bool AnotherRunUnitMayLock(FileConnector c) =>
        c.HostSharing != FileSharing.NoOther && PhysicalFileTable.AnotherRunUnitHoldsLocks(c);

    /// <summary>⛔ THE HOT-PATH GATE of §9.1.16 governance: is there ANY record-lock question for this
    /// statement to answer? Only two things can make one — some connector holds a lock on this physical file
    /// (so a conflict, a GR21 e)-style release or a self re-access is possible), or THIS connector may acquire
    /// one (<see cref="LocksEffective"/>). When neither holds, every governed action below is provably a no-op:
    /// the conflict check finds an empty table, the releases release nothing, and the acquisition arms are shut
    /// by <see cref="LocksEffective"/>.
    /// <para>It exists because governance became UNCONDITIONAL (kb/Work PB669): every connector now consults
    /// the physical file's lock table, so the overwhelmingly common case — a program whose files nobody shares
    /// — has to cost a <c>Count</c> test and nothing more. In particular it keeps
    /// <c>FileConnector.MutationTargetRecordId</c> and <c>LastReadRecordId</c> off the plain REWRITE / DELETE /
    /// READ paths, which on the keyed organizations allocate a key or an ordinal string per statement.</para></summary>
    private static bool RecordLocksGovern(ConnectorShare meta, PhysicalFileTable.State st, string name, FileConnector c) =>
        !NoRecordIsLocked(st, c) || LocksEffective(meta, st, name);

    /// <summary>⛔ THE ONE RECORD-OPERATION-CONFLICT CHECK, for every verb that states it and every
    /// organization: is the record identified by the statement locked by ANOTHER file connector? The three
    /// statements word one rule identically — §14.9.30.4 GR9 "the record identified for access", §14.9.35.4 GR11
    /// "the record identified for rewriting", §14.9.10.4 GR6 "the record identified for deletion" — each
    /// continuing "the result of the operation depends on the presence or absence of the RETRY phrase" and each
    /// ending in the record operation conflict condition (§9.1.13.8). (WRITE states none: §9.1.13.8 item 1 is an
    /// attempt to ACCESS a record, the §14.9.51.4 general rules define no such leg, and GR33/GR42 say the
    /// invalid-key checks ignore record locks.)
    /// <para>GR9 gives the answer in two delegated halves — "If the RETRY phrase is specified, additional
    /// attempts may be made to read the record as specified in the rules in 14.7.9" and "The I-O status is set
    /// in accordance with the rules for the RETRY phrase" — so BOTH go to <see cref="RetryLoop"/>, and this
    /// method contributes only the conflict predicate and the status assignment. ⛔ It must never name a status
    /// per retry form: in one run unit the holder cannot release mid-loop, so every form exhausts, but WHICH
    /// status they exhaust to differs by form and by conflict class and is <see cref="ExhaustionStatus"/>'s to
    /// decide. Returns the conflict status — already stored on the connector — or null when the operation may
    /// proceed. IGNORING LOCK short-circuits it, and only READ has that phrase: §14.9.30.4 GR12 makes "the
    /// requested record … available, even if it is locked".</para>
    /// <para>⛔ EVERY CALLER ASKS IT BEFORE COMMITTING ANYTHING (kb/Work PB338) — that is what makes the three
    /// statements' "unchanged" rules (§14.9.30.4 GR10 a)/d), §14.9.35.4 GR14, §14.9.10.4 GR6 b)) true by
    /// construction rather than by repair.</para></summary>
    private string? ConflictOnLockedRecord(string name, PhysicalFileTable.State st, string recId,
        bool ignoringLock, FileRetryKind retryKind, long retryAmount)
    {
        if (ignoringLock) return null;                                       // §14.9.30.4 GR12
        var c = Require(name);
        if (PhysicalFileTable.HolderOf(st, name, recId, c) == RecordLockHolder.None) return null;
        // WHO holds it decides what a RETRY may wait for (kb/Work PB1163): a connector of this run unit cannot give
        // the lock back while this statement waits, a connector of another run unit can (kb/Work PB2660) — and the
        // wait lets go of the store's statement mutex, or that run unit could never execute the statement that
        // releases it.
        string conflict = RetryLoop(
            () => PhysicalFileTable.HolderOf(st, name, recId, c) switch
            {
                RecordLockHolder.None => RetryAttempt.InRunUnit(FileStatusCode.Success),
                RecordLockHolder.ThisRunUnit => RetryAttempt.InRunUnit(FileStatusCode.RecordLocked),
                _ => RetryAttempt.OutsideRunUnit(FileStatusCode.RecordLocked),
            },
            retryKind, retryAmount, waiter: c);
        if (conflict == FileStatusCode.Success) return null;
        SetStatusOf(name, conflict);   // the status assignment drops the '43' gate (PB140); every
        // caller decides BEFORE committing, so GR10 a)/d) hold with no action at all (kb/Work PB338)
        return conflict;
    }

    /// <summary>⛔ THE HOLDINGS HALF OF A READ'S LOCK TEST (DESIGN-runtime-library.md 2.2.1.10, "The lock TEST, for every
    /// writer and every reader not IGNORING LOCK"; kb/Work PB3337): a record a transaction changed is held IN THE FILE, not
    /// in the region, and a READ that does not ignore locks meets it with '51' while its owner lives (§9.1.16: <i>"The
    /// implementor may specify circumstances other than a locked logical record that result in the return of a locked
    /// record status."</i>; DOC-A.1-152) — its RETRY phrase applying — and rolls a dead owner back at the test
    /// (<paramref name="reevaluate"/>: the record to read is named again). IGNORING LOCK tests nothing (§14.9.30.4 GR12;
    /// D.2.5's tolerated read). Returns the conflict status — already stored on the connector — or null. A SEQUENTIAL
    /// file's transactions hold their records in their journals instead (<see cref="JournalConflict"/>).</summary>
    private string? HoldingsConflict(string name, FileConnector c, string recId, bool ignoringLock, out bool reevaluate)
    {
        reevaluate = false;
        if (ignoringLock || c is not KeyedConnector keyed || keyed.ReadHoldingsGate(recId, out reevaluate) is not { } held)
            return null;
        SetStatusOf(name, held);   // as ConflictOnLockedRecord: the status assignment drops the '43' gate (PB140)
        return held;
    }

    /// <summary>⛔ THE JOURNAL HALF OF A SEQUENTIAL READ'S LOCK TEST (DESIGN-runtime-library.md 2.2.2.3, 2.2.2.8; kb/Work PB3340):
    /// a record another live transaction on the file rewrote or appended is held in its JOURNAL, not in the region, so the
    /// record-lock tables (and the peek they gate) do not see it; a READ that does not ignore locks, through a connector whose
    /// file lock admits another run unit's writer, meets it with '51' (§9.1.13.8 1); §9.1.16's implementor-specified
    /// circumstance, DOC-A.1-152) and settles a dead owner's transaction first (<see cref="SequentialConnector.ReadHoldingsGate"/>).
    /// A connector whose permanent error is in effect asks no lock question (§9.1.13.1, PB3416). Returns the conflict status —
    /// stored on the connector — or null.</summary>
    private string? JournalConflict(string name, FileConnector c, bool previous, bool ignoringLock)
    {
        if (ignoringLock || c is not SequentialConnector sequential || PermanentErrorOutranksLocks(c)
            || sequential.ReadHoldingsGate(previous) is not { } held)
            return null;
        SetStatusOf(name, held);   // as ConflictOnLockedRecord: the status assignment drops the '43' gate (PB140)
        return held;
    }

    /// <summary>The governed statement's RETRY phrase, handed to its connector for the statement's length
    /// (<see cref="FileConnector.StatementRetry"/>: the holdings test applies it, 2.2.1.10) and cleared by the disposal.</summary>
    private RetryScope StatementRetryOf(string name, FileRetryKind kind, long amount)
    {
        var c = Require(name);
        c.StatementRetry = (kind, amount);
        return new RetryScope(c);
    }

    private readonly struct RetryScope(FileConnector c) : IDisposable
    {
        public void Dispose() => c.StatementRetry = default;
    }

    /// <summary>ISO §14.9.30.4 GR11 c)/d) — does this READ set a record lock on the record it makes available?
    /// GR11 c): under automatic locking every READ does; GR11 d): under manual locking only a READ carrying the
    /// LOCK phrase does.
    /// <para>⛔ RETENTION IS DECIDED BY THE RETENTION BRACKET ALONE (kb/Work PB331). IGNORING LOCK says nothing
    /// about whether THIS connector keeps a lock — it is §14.9.30.4 GR12, "the requested record is made
    /// available, even if it is locked", which the caller applies by skipping the conflict check. It used to be
    /// a fourth member of this switch mapping to "never lock", which happened to agree with GR11 d) only because
    /// §14.9.30.3 SR4 bars IGNORING LOCK under automatic locking; it also made the legal
    /// <c>IGNORING LOCK WITH NO LOCK</c> unable to reach GR11 b)'s release.</para></summary>
    private static bool ReadSetsRecordLock(ConnectorShare meta, FileRecordLock phrase) => phrase switch
    {
        FileRecordLock.WithNoLock => false,
        FileRecordLock.WithLock => true,
        _ => meta.LockMode == FileLockMode.Automatic,   // no phrase: AUTOMATIC auto-locks (§12.4.5.9.4 GR4), MANUAL does not (GR5)
    };

    /// <summary>The §12.4.5.9.4 GR7 record-lock CEILING, tested against the record a READ has IDENTIFIED but not
    /// yet made available — <i>"Any I-O statement that attempts to obtain a record lock that would exceed either
    /// limit is unsuccessful and receives an I-O status that indicates that condition"</i> ('53'/'54',
    /// §9.1.13.8 items 3/4). Returns the denial status — already stored on the connector — or null.
    /// <para>⛔ IT IS A PRE-FLIGHT, NOT A POST-READ REPAIR (kb/Work PB338), and the same shape
    /// <see cref="WriteShared"/> and <see cref="RewriteShared"/> already use. A '53'/'54' READ is unsuccessful
    /// but it is NOT the record operation conflict condition GR9 defines — that one is "the record identified
    /// for access is locked by ANOTHER file connector", where these two are this connector's own lock COUNT —
    /// so §14.9.30.4 GR10 a) does not exempt it and GR18 applies in full: the file position indicator is set to
    /// indicate that no valid record position has been established. Denying AFTER the physical step left a
    /// successfully-advanced position behind a failure status, and the next sequential READ silently skipped the
    /// record the denial had already consumed.</para>
    /// <para>A record this connector ALREADY holds is exempt: re-locking it changes no count (a conflict with
    /// another connector was refused before this point), exactly as the REWRITE arm reasons.</para>
    /// <para>The ceiling itself is <see cref="RecordLockCeiling"/>'s, with the statement's RETRY phrase (kb/Work
    /// PB707).</para></summary>
    private string? PreflightReadLock(ConnectorShare meta, PhysicalFileTable.State st, string name,
        string recId, FileRecordLock phrase, FileRetryKind retryKind, long retryAmount)
    {
        if (recId.Length == 0 || !ReadSetsRecordLock(meta, phrase) || !LocksEffective(meta, st, name)) return null;
        if (st.RecordLocks.ContainsKey(recId)) return null;
        // The commit posture's READ locks have no ceiling but the host's (DOC-A.1-154/155; 2.2.1.10): D.2.5's deferred
        // commits make them unbounded by design, and §12.4.5.9.4 GR7 lets the implementor set the maximum.
        if (Require(name) is KeyedConnector { CommitControlled: true }) return null;
        string pf = RecordLockCeiling(st, name, retryKind, retryAmount);
        if (pf == FileStatusCode.Success) return null;
        SetStatusOf(name, pf);   // the status assignment drops the '43' gate (PB140)
        return pf;
    }

    /// <summary>The §14.9.30.4 GR11 b)/c)/d) actions on the record a READ has just made available, shared by
    /// both governed formats: GR11 c)/d) set the lock (its ceiling already pre-flighted); GR11 b) — under
    /// multiple record locking, WITH NO LOCK releases a lock this connector already held on the record accessed,
    /// "at the completion of the successful execution of the READ statement".
    /// <para>GR11 a)'s release is NOT here: it is tied to EXECUTION, so it fires at the top of the statement —
    /// see <see cref="ReleasePriorRecordLocks"/>.</para></summary>
    private void ApplyPostReadLockActions(ConnectorShare meta, PhysicalFileTable.State st, string name,
        FileConnector c, string recId, FileRecordLock phrase)
    {
        // What this READ found — so an unsuccessful verdict reached AFTER these actions (ReadExceedsRecordMaximum) restores it.
        c.RecordLockHeldBeforeRead = st.RecordLocks.TryGetValue(recId, out var holder)
            && CobolNames.Same(holder, name);
        if (ReadSetsRecordLock(meta, phrase) && LocksEffective(meta, st, name))
            _physical.LockRecord(st, name, recId, c);           // GR11 c)/d)
        else if (meta.Multiple && phrase == FileRecordLock.WithNoLock)
            PhysicalFileTable.ReleaseSingle(st, name, recId);   // GR11 b)
    }

    /// <summary>Whether this connector SETS record locks: §12.4.5.9.4 GR1 a) and b) 1. — with no LOCK MODE clause a
    /// SHARING clause/phrase means NO record locks are set (and the implementor default here is likewise none);
    /// GR3 — a connector open in the sharing-with-no-other mode has exclusive access, so its LOCK MODE has no
    /// effect. With no clause at all the posture is <see cref="ImplementorDefaultShare"/>, whose
    /// <see cref="FileLockMode.None"/> takes the GR1 a)/b) 1. arm for the same reason.
    /// <para>⛔ IT GOVERNS ACQUISITION ONLY. Conflict CHECKS against locks OTHER connectors hold are never
    /// disabled — §9.1.16 makes a locked record inaccessible to another file connector with no qualification
    /// on that connector's own lock mode, and §12.4.5.9.4 GR1 a)/b) 1. are worded to say only that no locks are
    /// <b>set</b>. This comment said exactly that while the code two levels up contradicted it: the governed
    /// verbs returned to the ungoverned body whenever the connector was not in the posture map, so the check it
    /// promised was never reached (kb/Work PB669).</para>
    /// <para>⚠ DETERMINATION. §14.9.30.4 GR9, §14.9.35.4 GR11 and §14.9.10.4 GR6 each open <i>"If record
    /// locking is enabled for the … file connector"</i>, and §14.9.30.4 GR7 sends that question to §12.4.5.9.
    /// READ AS A PROPERTY OF THE ENVIRONMENT — the processor supports record locking (§12.4.5.9.4 GR2) and the
    /// physical file is not open in the sharing-with-no-other mode (GR3) — not as "this connector's own clause
    /// causes it to acquire locks". The rejected reading makes §9.1.16's unqualified sentence false for any
    /// program that simply omits LOCK MODE, i.e. record locking would be unenforceable against exactly the
    /// programs it exists to constrain, and it would give GR1 a)'s narrow "no record locks are set" a scope its
    /// words do not have.</para></summary>
    private static bool LocksEffective(ConnectorShare meta, PhysicalFileTable.State st, string name)
    {
        // A connector that is not open has no sharing mode in effect — §9.1.15, "The sharing mode specifies the
        // types of operations that may be performed on the shared physical file through other file connectors
        // throughout the duration of this OPEN" — so it sets no record lock: its statement fails on its own
        // open-mode rule ('47'/'48'/'49') before any lock is reached (kb/Work PB1194).
        if (!st.Open.TryGetValue(name, out var open)) return false;
        return RecordLocksSetUnder(meta, open.Sharing);
    }

    /// <summary>⛔ THE ONE RULE FOR WHEN A CONNECTOR SETS RECORD LOCKS (kb/Work PB3372), asked per statement by
    /// <see cref="LocksEffective"/> and per OPEN by <see cref="SharedOpenAttempt"/> (which hands the answer to
    /// <see cref="FileConnector.AssociatePhysical"/> as the connector's record-locking participation — the §9.1.16
    /// lock identity a sequential <c>OPEN EXTEND</c> numbers its releases by), so the two cannot disagree. §12.4.5.9.4
    /// GR1: with no LOCK MODE clause <i>"no record locks are set"</i> — whether the file control entry has a SHARING
    /// clause (a), or the OPEN has a SHARING phrase (b 1.), or neither (b 2., the implementor default, which here is
    /// <see cref="FileLockMode.None"/>); GR3: <i>"If a physical file is open in the sharing with no other mode, the
    /// LOCK MODE clause has no effect."</i> A SHARING or RETRY phrase therefore never makes a connector a
    /// participant; only a LOCK MODE clause under a sharing mode other than NO OTHER does.</summary>
    private static bool RecordLocksSetUnder(ConnectorShare meta, FileSharing openSharing) =>
        meta.LockMode != FileLockMode.None && openSharing != FileSharing.NoOther;   // GR1 a)/b) 1., GR3

    /// <summary>⛔ THE ONE GOVERNED FORMAT-1 (sequential-access) READ — sequential, relative AND indexed
    /// organization (§9.1.16; the READ lock rules §14.9.30.4 GR7–GR12 are ALL-FORMATS rules, and GR22 is a
    /// Format-1 rule because ADVANCING appears only in the Format-1 general format and §14.9.30.3 SR6 bars it
    /// under ACCESS MODE RANDOM). Returns the I-O status; a record was made available iff it begins '0'.
    /// <para>⛔ GOVERNANCE DECIDES BEFORE THE POSITION MOVES, ON EVERY ORGANIZATION (kb/Work PB338). Each
    /// connector NAMES the record its next read would make available —
    /// <see cref="FileConnector.PeekSequentialRecordId"/>, its own §14.9.30.4 GR21 selection run without
    /// committing — so the GR9 conflict check and the §12.4.5.9.4 GR7 ceiling both run BEFORE the physical
    /// step. That is what GR10 a) ("The file position indicator is unchanged"), GR10 d) ("The key of reference
    /// for indexed files is unchanged") and GR13 a) ("… and, IF the record operation conflict condition did not
    /// occur, the file position indicator is set") require. The keyed walk used to fall through to a post-read
    /// check on the ground that it "learns which record that is by reading it"; it does not — the selection is a
    /// lookup and the read is the COMMIT — and a '51' therefore left the position advanced, silently skipping
    /// the record it had failed on.</para>
    /// <para><b>ADVANCING ON LOCK (GR22) needs no peek</b>, which is why the skip-scan is written ONCE here
    /// rather than per-arm: the rule's own model IS post-read — "as if the locked record were read and then the
    /// same READ statement were executed", repeated "until either an unlocked record is read or the end of the
    /// file is encountered if NEXT is specified or implied, or the beginning of file is encountered if PREVIOUS
    /// is specified" — and "A record operation conflict condition does not exist", so GR10 a) never applies to
    /// it. While the loop lived on the sequential arm alone a relative or indexed READ ADVANCING ON LOCK
    /// answered '51', precisely the status GR22 says cannot arise (kb/Work PB340).</para>
    /// </summary>
    public string ReadShared(string name, bool previous, FileRecordLock phrase, bool advancingOnLock,
        bool ignoringLock, FileRetryKind retryKind, long retryAmount, out string image)
    {
        using var statement = Require(name).BeginStatement();   // kb/Work PB2660 — one step to every other run unit
        using var retry = StatementRetryOf(name, retryKind, retryAmount);
        return ReadSharedInStatement(name, previous, phrase, advancingOnLock, ignoringLock, retryKind, retryAmount, out image);
    }

    /// <summary>The body of <see cref="ReadShared"/>, inside its connector's store statement.</summary>
    private string ReadSharedInStatement(string name, bool previous, FileRecordLock phrase, bool advancingOnLock,
        bool ignoringLock, FileRetryKind retryKind, long retryAmount, out string image)
    {
        image = "";
        var c = Require(name);   // unregistered = compiler defect, never an invented '30' (kb/Work PB140/PB360)
        c.RecordLockHeldBeforeRead = null;   // this READ has run no lock action yet (ApplyPostReadLockActions sets it)
        var meta = ShareOf(name);             // §12.4.5.9.4 GR1 b) 2. for a clause-less connector — never an early exit
        var st = _physical.For(c.HostPath);   // the connector's LIVE association (§12.4.5.3 GR3), never a cached copy
        // §14.9.30.4 GR11 a) / §12.4.5.9.4 GR6 — released by the EXECUTION of the statement, so before anything
        // can make it unsuccessful, exactly as the three mutating verbs do it.
        ReleasePriorRecordLocks(meta, st, name);
        while (true)
        {
            // The pre-read legs. ADVANCING ON LOCK takes neither peek nor conflict check: GR22 rules the
            // conflict condition out and its skip-scan below is post-read on every organization. Nor does a READ on a
            // connector whose permanent error is in effect (NoLockQuestion).
            string peek = advancingOnLock || NoLockQuestion(st, c) ? "" : c.PeekSequentialRecordId(previous);
            if (!advancingOnLock && JournalConflict(name, c, previous, ignoringLock) is { } journaled)
            {
                image = "";
                return journaled;    // GR10 — nothing has moved
            }
            if (peek.Length > 0)
            {
                if (ConflictOnLockedRecord(name, st, peek, ignoringLock, retryKind, retryAmount) is { } pre)
                {
                    image = "";
                    return pre;      // GR10 — nothing has moved, so a),c),d) hold by construction
                }
                if (HoldingsConflict(name, c, peek, ignoringLock, out bool reevaluate) is { } held)
                {
                    image = "";
                    return held;     // GR10 — nothing has moved
                }
                if (reevaluate) continue;   // a dead holder was rolled back: the record to read is named again
                if (PreflightReadLock(meta, st, name, peek, phrase, retryKind, retryAmount) is { } predenied)
                {
                    image = "";
                    c.ApplyUnsuccessfulReadPosition();   // §12.4.5.9.4 GR7 unsuccessful → §14.9.30.4 GR18
                    return predenied;
                }
            }
            string status = ReadFormat1Step(name, c, previous, out image);
            if (status.Length == 0 || status[0] != '0') return status;   // at end (GR24) or a mode/position failure
            if (!RecordLocksGovern(meta, st, name, c)) return status;   // no lock on the file and none to set
            string recId = c.LastReadRecordId;
            if (recId.Length == 0) return status;                        // no record identity to govern
            // ⛔ §14.9.30.4 GR22 — THE ONE ADVANCING ON LOCK SKIP-SCAN, reached by all three organizations. The
            // locked record HAS been read, so the file position indicator has advanced, which is exactly what
            // "as if the locked record were read" requires; the same READ statement then runs again.
            if (advancingOnLock && !ignoringLock
                && PhysicalFileTable.HolderOf(st, name, recId, c) != RecordLockHolder.None) continue;
            // The ceiling is tested at the earliest point the target record is known: BEFORE the read where the
            // peek ran (this call is then a no-op), and here on the two paths that take no peek — GR22's
            // skip-scan, whose record is settled only now, and a physical file holding no locks at all, where
            // only the RUN-UNIT limit could still deny. Either way it is the same ONE rule, and GR18's
            // invalidation is then the only way a position the retrieval advanced can stop naming a record this
            // unsuccessful READ never made available.
            if (peek.Length == 0 && PreflightReadLock(meta, st, name, recId, phrase, retryKind, retryAmount) is { } denied)
            {
                image = "";   // a 53/54 lock denial is an unsuccessful READ (§12.4.5.9.4 GR7) — no record available
                c.ApplyUnsuccessfulReadPosition();
                return denied;
            }
            ApplyPostReadLockActions(meta, st, name, c, recId, phrase);   // GR11 b)/c)/d)
            return status;
        }
    }

    /// <summary>ONE physical Format-1 (sequential-access) retrieval step on ANY organization — the step
    /// §14.9.30.4 GR22's skip-scan repeats, and the COMMIT half of the peek-then-commit split
    /// <see cref="FileConnector.PeekSequentialRecordId"/> is the other half of. Returns the I-O status the
    /// connector assigned. (<paramref name="previous"/> is §14.9.30.2 Format 1's direction phrase on EVERY
    /// organization; the sequential arm carries it since kb/Work PB334 gave <c>BoundRead</c> a
    /// <c>ReadKind</c>.)</summary>
    private static string ReadFormat1Step(string name, FileConnector c, bool previous, out string image)
    {
        image = "";
        switch (c)
        {
            case SequentialConnector f: f.Read(previous, out image); return f.Status;
            case RelativeConnector r: return previous ? r.ReadPrevious(out image) : r.ReadNext(out image);
            case IndexedConnector ix: return previous ? ix.ReadPrevious(out image) : ix.ReadNext(out image);
            default: throw MisroutedVerb("governed READ (Format 1)", name, c);
        }
    }

    /// <summary>⛔ THE ONE GOVERNED FORMAT-2 (random-access) READ — relative and indexed organization
    /// (§9.1.16; §14.9.30.4 GR7–GR12 are ALL-FORMATS rules). Returns the I-O status.
    /// <para>It owns the physical retrieval for the same reason its Format-1 sibling does (kb/Work PB338): the
    /// record identified for access is knowable before the read — GR29's relative record number, GR32's key of
    /// reference — so the GR9 conflict check and the §12.4.5.9.4 GR7 ceiling run first and §14.9.30.4 GR10 a)/d) hold. It
    /// replaced a POST-read <c>ReadLockGovern</c> patch applied to a status the connector had already committed a
    /// position for; on an indexed '51' that also left <c>_refKey</c> assigned, against GR10 d).</para>
    /// <para>ADVANCING ON LOCK is not in the Format-2 general format at all (§14.9.30.2) and §14.9.30.3 SR6 bars
    /// it under ACCESS MODE RANDOM, so this entry carries no advancing-on-lock argument.</para></summary>
    public string ReadKeyedShared(string name, int keyIndex, string keyedRecordImage, FileRecordLock phrase,
        bool ignoringLock, FileRetryKind retryKind, long retryAmount, out string image, RecordExtents? areaExtents = null)
    {
        using var statement = Require(name).BeginStatement();   // kb/Work PB2660 — one step to every other run unit
        using var retry = StatementRetryOf(name, retryKind, retryAmount);
        return ReadKeyedSharedInStatement(name, keyIndex, keyedRecordImage, phrase, ignoringLock, retryKind, retryAmount, out image, areaExtents);
    }

    /// <summary>The body of <see cref="ReadKeyedShared"/>, inside its connector's store statement.</summary>
    private string ReadKeyedSharedInStatement(string name, int keyIndex, string keyedRecordImage, FileRecordLock phrase,
        bool ignoringLock, FileRetryKind retryKind, long retryAmount, out string image, RecordExtents? areaExtents)
    {
        image = "";
        var c = Require(name);   // unregistered = compiler defect, never an invented '30' (kb/Work PB140/PB360)
        c.RecordLockHeldBeforeRead = null;   // this READ has run no lock action yet (ApplyPostReadLockActions sets it)
        var meta = ShareOf(name);             // §12.4.5.9.4 GR1 b) 2. for a clause-less connector — never an early exit
        var st = _physical.For(c.HostPath);   // the connector's LIVE association (§12.4.5.3 GR3), never a cached copy
        ReleasePriorRecordLocks(meta, st, name);   // §14.9.30.4 GR11 a) / §12.4.5.9.4 GR6 — on EXECUTION
        string peek = NoLockQuestion(st, c) ? "" : c.PeekRandomReadRecordId(keyIndex, keyedRecordImage, areaExtents);
        if (peek.Length > 0)
        {
            if (ConflictOnLockedRecord(name, st, peek, ignoringLock, retryKind, retryAmount) is { } pre)
                return pre;                          // GR10 — nothing has moved
            if (HoldingsConflict(name, c, peek, ignoringLock, out bool reevaluate) is { } held)
                return held;                         // GR10 — nothing has moved
            if (reevaluate)                          // a dead holder was rolled back: the READ is evaluated again
                return ReadKeyedSharedInStatement(name, keyIndex, keyedRecordImage, phrase, ignoringLock, retryKind,
                    retryAmount, out image, areaExtents);
            if (PreflightReadLock(meta, st, name, peek, phrase, retryKind, retryAmount) is { } predenied)
            {
                c.ApplyUnsuccessfulReadPosition();   // §12.4.5.9.4 GR7 unsuccessful → §14.9.30.4 GR18
                return predenied;
            }
        }
        string status = ReadKeyed(name, keyIndex, keyedRecordImage, out image, areaExtents);
        if (status.Length == 0 || status[0] != '0') return status;   // invalid key (§9.1.14) or a mode failure
        if (!RecordLocksGovern(meta, st, name, c)) return status;   // no lock on the file and none to set
        string recId = c.LastReadRecordId;
        if (recId.Length > 0) ApplyPostReadLockActions(meta, st, name, c, recId, phrase);   // GR11 b)/c)/d)
        return status;
    }

    /// <summary>Governed WRITE for a sharing-active connector, any organization (§14.9.51 GR10/GR11 + §14.7.9).
    /// No '51' record-operation conflict is defined for WRITE — §9.1.13.8's 51 covers "an attempt to ACCESS a
    /// record", the WRITE general rules define no 51 leg, and GR33/GR42 state the invalid-key checks ignore record
    /// locks; GR16's RETRY governs implementor "resources … locked by another run unit", which cannot arise
    /// in-process, so no WRITE RETRY ever waits on one. The one conflict a WRITE WITH LOCK CAN meet is the
    /// §12.4.5.9.4 GR7 lock ceiling ('53'/'54', §9.1.13.8 items 3/4), a record operation conflict condition that
    /// §14.7.9.3 GR4 engages on, so the statement's RETRY phrase rides to <see cref="RecordLockCeiling"/> and the
    /// ceiling is judged by <see cref="RetryLoop"/> like every other conflict (kb/Work PB707). Returns the I-O
    /// status.</summary>
    public string WriteShared(string name, string image, int length, FileRecordLock phrase,
        FileRetryKind retryKind, long retryAmount, LinagePage? page, WriteAdvance advance = default,
        RecordExtents? extents = null, bool nationalRecord = false)
    {
        using var statement = Require(name).BeginStatement();   // kb/Work PB2660 — one step to every other run unit
        using var retry = StatementRetryOf(name, retryKind, retryAmount);
        return WriteSharedInStatement(name, image, length, phrase, retryKind, retryAmount, page, advance, extents, nationalRecord);
    }

    /// <summary>The body of <see cref="WriteShared"/>, inside its connector's store statement.</summary>
    private string WriteSharedInStatement(string name, string image, int length, FileRecordLock phrase,
        FileRetryKind retryKind, long retryAmount, LinagePage? page, WriteAdvance advance,
        RecordExtents? extents, bool nationalRecord)
    {
        var c = Require(name);   // unregistered = compiler defect, never an invented '30' (kb/Work PB140/PB360)
        var meta = ShareOf(name);             // §12.4.5.9.4 GR1 b) 2. for a clause-less connector — never an early exit
        var st = _physical.For(c.HostPath);   // the connector's LIVE association (§12.4.5.3 GR3), never a cached copy
        ReleasePriorRecordLocks(meta, st, name);   // §14.9.51.4 GR10 / §12.4.5.9.4 GR6
        // GR11. Under commit control the WRITE's implied lock (2.2.1.10, finding R2-3) is the transaction's HOLDING of the
        // record, written into the file by the WRITE itself, never a region byte that dies with the handle (finding R4-1).
        bool wantLock = phrase == FileRecordLock.WithLock && LocksEffective(meta, st, name)
            && c is not KeyedConnector { CommitControlled: true };
        // A file of FIXED-length records carries a variable-length record in its fixed form (D-FRA (vi)).
        var (medium, mediumExtents) = c.FixedForm(image, extents);
        // ⛔ §12.4.5.9.4 GR7's ceiling ('54' / '53') is the LAST check of a statement that requests a new lock, and the
        // statement still fails BEFORE the write (§14.9.51 GR15): the organization asks the gate once its own rules
        // have passed, immediately before it changes the file (FileConnector.CommitGate, kb/Work PB2848). It used to
        // be asked here, ahead of the organization, so a record that broke GR14's size rule answered '54' where
        // DOC-A.1-104 (2) gives '44'. A permanent error in effect (§9.1.13.1, kb/Work PB1541) is replayed by the
        // organization before the gate is reached, so no lock is pre-flighted for that WRITE either.
        string status;
        if (wantLock) c.CommitGate = () => LockCeilingRefusal(st, name, retryKind, retryAmount);
        try { status = WriteAnyOrg(c, medium, length, page, advance, mediumExtents, nationalRecord); }
        finally { c.CommitGate = null; }
        if (wantLock && status.Length > 0 && status[0] == '0' && c.LastWrittenRecordId is { Length: > 0 } recId)
            _physical.LockRecord(st, name, recId, c);   // GR11 — the just-released record's lock is set
        return status;
    }

    /// <summary>The <see cref="FileConnector.CommitGate"/> of a statement that requests a NEW record lock: §12.4.5.9.4
    /// GR7's '54' (this connector holds its maximum) or '53' (this run unit does), else null. Acquires nothing — the
    /// lock is set only when the statement has succeeded.</summary>
    private string? LockCeilingRefusal(PhysicalFileTable.State st, string name, FileRetryKind retryKind, long retryAmount) =>
        RecordLockCeiling(st, name, retryKind, retryAmount) is var refusal && refusal != FileStatusCode.Success ? refusal : null;

    /// <summary>⛔ THE ONE JUDGMENT OF THE §12.4.5.9.4 GR7 RECORD-LOCK CEILING for a statement that requests a new
    /// lock — READ, WRITE and REWRITE alike (kb/Work PB707): <see cref="PhysicalFileTable.PreflightNewLock"/>
    /// answers whether one more lock would exceed a limit, and <see cref="RetryLoop"/> lands it. '53' and '54' are
    /// record operation conflict conditions on the standard's own definition (§9.1.13.8 items 3/4, "Record
    /// operation conflict condition with unsuccessful completion"), and §14.7.9.3 GR4 engages on any such
    /// condition, so the statement's RETRY phrase is applied to them here and not decided at three call sites.
    /// The attempt is <see cref="RetryAttempt.InRunUnit"/>: the count is this connector's and this run unit's own,
    /// which no re-attempt inside the executing statement can lower, so every form exhausts to the ceiling's own
    /// status (<see cref="ExhaustionStatus"/> turns only a '51' under FOREVER into the deadlock '52') — the loop
    /// is the rule's one home, not a way to a different answer.</summary>
    private string RecordLockCeiling(PhysicalFileTable.State st, string name, FileRetryKind retryKind, long retryAmount) =>
        RetryLoop(() => RetryAttempt.InRunUnit(_physical.PreflightNewLock(st, name)), retryKind, retryAmount);

    /// <summary>Governed REWRITE for a sharing-active connector, any organization (§14.9.35 GR11/GR12 + §14.7.9):
    /// the pre-operation conflict check against the record identified for rewriting (locked by another connector
    /// → RETRY re-checks, else 51 with the record NOT rewritten, the record area unaffected and the FPI unchanged
    /// — GR11a-c/GR14), then the GR12 lock actions. Returns the I-O status.</summary>
    public string RewriteShared(string name, string image, int length, FileRecordLock phrase,
        FileRetryKind retryKind, long retryAmount, RecordExtents? extents = null, bool nationalRecord = false)
    {
        using var statement = Require(name).BeginStatement();   // kb/Work PB2660 — one step to every other run unit
        using var retry = StatementRetryOf(name, retryKind, retryAmount);
        return RewriteSharedInStatement(name, image, length, phrase, retryKind, retryAmount, extents, nationalRecord);
    }

    /// <summary>The body of <see cref="RewriteShared"/>, inside its connector's store statement.</summary>
    private string RewriteSharedInStatement(string name, string image, int length, FileRecordLock phrase,
        FileRetryKind retryKind, long retryAmount, RecordExtents? extents, bool nationalRecord)
    {
        var c = Require(name);   // unregistered = compiler defect, never an invented '30' (kb/Work PB140/PB360)
        var meta = ShareOf(name);             // §12.4.5.9.4 GR1 b) 2. for a clause-less connector — never an early exit
        var st = _physical.For(c.HostPath);   // the connector's LIVE association (§12.4.5.3 GR3), never a cached copy
        // The record identity costs an allocation on the keyed organizations, so it is taken only when some
        // §9.1.16 question can have a non-trivial answer; with no lock on the file and none to set, every arm
        // below is a no-op whatever the target is (kb/Work PB669).
        string target = MutationTarget(meta, st, name, c, image, extents);
        ReleasePriorRecordLocks(meta, st, name, target);   // §14.9.35.4 GR12 a) 2. — released at the beginning
        if (target.Length > 0)
        {
            // §14.9.35.4 GR11 — the ONE conflict check; GR14 then keeps the record, the record area and the file
            // position indicator untouched, which holds because nothing has been committed yet.
            if (ConflictOnLockedRecord(name, st, target, ignoringLock: false, retryKind, retryAmount) is { } conflict)
                return conflict;
        }
        var (medium, mediumExtents) = c.FixedForm(image, extents);   // D-FRA (vi), as WriteShared
        // §12.4.5.9.4 GR7's ceiling, the last check of the statement and still before the update (§14.9.35 GR14), asked
        // by the organization through the commit gate exactly as WriteShared does (kb/Work PB2848). Post-conflict-check
        // any surviving lock on the target is this connector's own — re-lock is idempotent per §9.1.16 (a locked record "may be re-accessed
        // by the same file connector that holds the lock"), so only a genuinely NEW lock is gated.
        string status;
        if (target.Length > 0 && phrase == FileRecordLock.WithLock && LocksEffective(meta, st, name)
            && !st.RecordLocks.ContainsKey(target))
            c.CommitGate = () => LockCeilingRefusal(st, name, retryKind, retryAmount);
        try { status = RewriteAnyOrg(c, medium, length, mediumExtents, nationalRecord); }
        finally { c.CommitGate = null; }
        if (status.Length > 0 && status[0] == '0' && target.Length > 0)
        {
            if (phrase == FileRecordLock.WithLock && LocksEffective(meta, st, name))
                _physical.LockRecord(st, name, target, c);                   // GR12c — set at completion
            else if (!meta.Multiple || phrase == FileRecordLock.WithNoLock)
                PhysicalFileTable.ReleaseSingle(st, name, target);           // GR12a1 (single) / GR12b (multiple + NO LOCK)
        }
        return status;
    }

    /// <summary>Governed DELETE RECORD for a sharing-active connector (§14.9.10 GR6/GR7 + §14.7.9): the
    /// pre-operation conflict check against the record identified for deletion (locked by another connector →
    /// RETRY re-checks, else 51 with the record NOT removed — GR6a-c), then the GR7 releases: a self-lock on
    /// another record at the beginning (GR7a2, single), the deleted record's lock at completion (GR7a1/GR7b).
    /// Returns the I-O status. Unlike its WRITE/REWRITE siblings this body consults no
    /// <see cref="LocksEffective"/> — DELIBERATE, not an omission (kb/Work PB143 verified the premise): the
    /// siblings gate their lock ACQUISITIONS on it, and DELETE acquires none; GR7's releases are conditioned
    /// on "record locks are in effect", but a connector for which locks are not effective HOLDS none, so the
    /// unconditional releases are correct by vacuity. The conflict CHECK is never disabled (§9.1.16 — a locked
    /// record is inaccessible to another connector regardless of that connector's own lock mode).</summary>
    public string DeleteShared(string name, string keyedRecordImage, FileRetryKind retryKind, long retryAmount,
        RecordExtents? areaExtents = null)
    {
        using var statement = Require(name).BeginStatement();   // kb/Work PB2660 — one step to every other run unit
        using var retry = StatementRetryOf(name, retryKind, retryAmount);
        return DeleteSharedInStatement(name, keyedRecordImage, retryKind, retryAmount, areaExtents);
    }

    /// <summary>The body of <see cref="DeleteShared"/>, inside its connector's store statement.</summary>
    private string DeleteSharedInStatement(string name, string keyedRecordImage, FileRetryKind retryKind, long retryAmount,
        RecordExtents? areaExtents)
    {
        var c = Require(name);   // unregistered = compiler defect, never an invented '30' (kb/Work PB140/PB360)
        var meta = ShareOf(name);             // §12.4.5.9.4 GR1 b) 2. for a clause-less connector — never an early exit
        var st = _physical.For(c.HostPath);   // the connector's LIVE association (§12.4.5.3 GR3), never a cached copy
        // The record identity is taken only when a §9.1.16 question can have a non-trivial answer (kb/Work PB669).
        string target = MutationTarget(meta, st, name, c, keyedRecordImage, areaExtents);
        ReleasePriorRecordLocks(meta, st, name, target);   // §14.9.10.4 GR7 a) 2. — released at the beginning
        if (target.Length > 0
            // §14.9.10.4 GR6 — the ONE conflict check; GR6 b)/c) then leave the record present and the record
            // area unaffected, which holds because nothing has been committed yet.
            && ConflictOnLockedRecord(name, st, target, ignoringLock: false, retryKind, retryAmount) is { } conflict)
            return conflict;
        string status = DeleteRecord(name, keyedRecordImage, areaExtents);
        // GR7a1/GR7b — the deleted record's lock releases at completion, "if record locks are in effect and the file is
        // not subject to an active APPLY COMMIT clause": under one, the lock stays until COMMIT or ROLLBACK (2.2.1.10).
        if (status.Length > 0 && status[0] == '0' && target.Length > 0 && c is not KeyedConnector { CommitControlled: true })
            PhysicalFileTable.ReleaseSingle(st, name, target);
        return status;
    }

    /// <summary>The WRITE body over any organization AND any print-control shape (one polymorphic dispatch — the
    /// governed entry's operation half; each arm is the same connector call the corresponding ungoverned entry
    /// makes). <paramref name="advance"/> reaches only the sequential arm: §14.9.51.3 SR2/SR3 put the ADVANCING
    /// phrase in Format 1 alone, which SR3 restricts to the sequential organization, so the binder screens a
    /// keyed WRITE that carries one and those arms cannot see a non-<c>None</c> kind.</summary>
    /// <para><paramref name="extents"/> — the record's EXTENT TABLE (determination D-FRA (v); kb/Work PB1053) —
    /// rides every arm that frames a record; a print-control WRITE sends a line, which carries none.</para>
    /// <para><paramref name="nationalRecord"/> — the category of record-name-1 (§14.9.51.4 GR21 / GR22) — reaches the
    /// sequential arm alone: it is the only organization with a rule keyed on it (a line sequential file's trailing
    /// spaces and its character set; kb/Work PB1191).</para>
    private static string WriteAnyOrg(FileConnector c, string image, int length, LinagePage? page,
        WriteAdvance advance, RecordExtents? extents, bool nationalRecord) => c switch
    {
        SequentialConnector f => advance.Kind is WriteAdvanceKind.None
            ? f.Write(image, length, page, extents, nationalRecord)
            // §14.9.51.4 GR25 e)/f) — ONE advance, placed by the statement's words; the combined COBOL-2023
            // BEFORE AFTER form arrives as Before, because GR25 f) puts its advance after the presentation
            // exactly as GR25 e) does (kb/Work PB712 deleted the third, two-amount arm). The record length rides
            // this arm too: §14.9.51.4 GR14's bound is an ALL FILES rule, not a plain-WRITE one (kb/Work PB1190).
            : f.WriteAdvancing(image, advance.Lines, advance.Kind == WriteAdvanceKind.Before, page, length, nationalRecord),
        RelativeConnector r => r.Write(image, length, extents),
        IndexedConnector ix => ix.Write(image, length, extents),
        _ => FileStatusCode.PermanentError,
    };

    /// <summary>The plain REWRITE body over any organization (the governed entry's operation half).</summary>
    private static string RewriteAnyOrg(FileConnector c, string image, int length, RecordExtents? extents, bool nationalRecord) => c switch
    {
        SequentialConnector f => f.Rewrite(image, length, extents, nationalRecord),
        RelativeConnector r => r.Rewrite(image, length, extents),
        IndexedConnector ix => ix.Rewrite(image, length, extents),
        _ => FileStatusCode.PermanentError,
    };

    /// <summary>UNLOCK file [RECORD[S]] (§14.9.47 GR1): release every record lock this connector holds and set
    /// status 00; UNLOCK of a file not open is status 42 (§9.1.13.7 item 2). §14.9.47.4 GR3 makes the I-O status
    /// update UNCONDITIONAL, so an unregistered name is a compiler defect and goes through <see cref="Require"/>
    /// like every other entry — the old <c>TryGetValue</c>-and-return set no status and raised nothing, a clean
    /// exit 0 for a statement that did nothing (kb/Work PB360).</summary>
    public void Unlock(string name, bool records)
    {
        _ = records;
        var c = Require(name);
        if (!c.IsOpen) { c.SetStatus(FileStatusCode.FileNotOpen); return; }
        // §9.1.13.1 — a permanent error in effect makes UNLOCK unsuccessful too, with the status that began it
        // (kb/Work PB1541); it releases nothing, like every statement the condition holds.
        if (c.PermanentErrorInEffect is { } stuck) { c.SetStatus(stuck); return; }
        if (_physical.TryGet(c.HostPath, out var st)) PhysicalFileTable.ReleaseAllForConnector(st, name);
        c.SetStatus(FileStatusCode.Success);
    }

    // ── The record-lock primitives (also the CobolFileLockTests surface) ─────────────────────────────────────

    /// <summary>Acquire a lock on <paramref name="recId"/> for connector <paramref name="name"/> (§12.4.5.9.4 GR7
    /// ceilings enforced). Returns 00 on grant.</summary>
    public string LockRecord(string name, string recId) =>
        _physical.LockRecord(_physical.For(HostPathOf(name)), name, recId, _files.GetValueOrDefault(name));

    /// <summary>True when <paramref name="recId"/> is locked by a connector OTHER than <paramref name="name"/>.</summary>
    public bool IsLockedByOther(string name, string recId) =>
        PhysicalFileTable.HolderOf(_physical.For(HostPathOf(name)), name, recId, _files.GetValueOrDefault(name))
            != RecordLockHolder.None;

    /// <summary>Release every record lock held by <paramref name="name"/> on its physical file (UNLOCK, CLOSE).</summary>
    public void ReleaseAllForConnector(string name)
    {
        if (_physical.TryGet(HostPathOf(name), out var st)) PhysicalFileTable.ReleaseAllForConnector(st, name);
    }

    /// <summary>Release a single record lock a connector holds (the LOCK MODE single-lock discipline, §12.4.5.9.4 GR6).</summary>
    public void ReleaseSingle(string name, string recId)
    {
        if (_physical.TryGet(HostPathOf(name), out var st)) PhysicalFileTable.ReleaseSingle(st, name, recId);
    }

    /// <summary>Evaluate <paramref name="attempt"/> under the RETRY discipline (ISO §14.7.9.3 — see
    /// docs/COBOLNET_FILES_DESIGN.md "D8. The RETRY phrase and the conflict-status class rule").
    /// <para>GR4 scopes the WHOLE discipline: it engages only "if the I/O operation is unsuccessful on the first
    /// attempt because of a file sharing conflict condition or a record operation conflict condition"
    /// (<see cref="IsConflict"/>). Success — and every other unsuccessful status, an OPEN's '35' included — is
    /// the statement's own answer and is returned untouched, un-retried.</para>
    /// <para>GR4a: no RETRY phrase, or an arithmetic-expression evaluating negative or zero, makes NO further
    /// attempt. GR1: n TIMES makes n further attempts after the initial failure. GR2: FOR n SECONDS clamps the
    /// timeout period to the implementor's maximum meaningful value, which WiseOwl COBOL defines as ZERO (A.1 item
    /// 166, docs/CONFORMANCE.md §7), so its period is zero-length and it likewise makes none. GR3: FOREVER attempts
    /// "until the input-output operation has been completed".</para>
    /// <para>⛔ WHO HOLDS THE LOCKED RESOURCE DECIDES WHETHER A LATER ATTEMPT CAN SUCCEED (kb/Work PB1163), and every
    /// attempt says (<see cref="RetryAttempt.HolderOutsideRunUnit"/>). A holder OUTSIDE the executing run unit —
    /// another run unit, or another process holding the host file — can let go while this statement waits, so a
    /// re-attempt against it waits <see cref="RetryInterval"/> first (A.1 item 165) and FOREVER keeps attempting
    /// until the operation completes. A holder INSIDE the run unit is one of its own file connectors, which cannot
    /// close a file or release a record while this statement executes: re-attempts against it are made back to back
    /// (waiting could only delay the same answer), and FOREVER makes one re-check and then stops, because that wait
    /// could never end — the deadlock <see cref="ExhaustionStatus"/> names (A.1 item 109). This used to be the ONLY
    /// arm: the loop assumed no releaser existed, so FOREVER gave up after one re-attempt on a file another run
    /// unit held and released moments later.</para>
    /// <para>Every landing goes through <see cref="ExhaustionStatus"/> — the status is a function of the
    /// conflict's own class, NEVER a literal at a call site.</para></summary>
    public static string RetryLoop(Func<RetryAttempt> attempt, FileRetryKind kind, long amount,
        FileConnector? waiter = null)
    {
        var a = attempt();
        if (!IsConflict(a.Status)) return a.Status;   // GR4 — success, or an unsuccessful status that is not a conflict
        if (kind == FileRetryKind.Times)
            // GR1 — n further attempts after the initial failure; a zero or negative n makes none (GR4a).
            for (long i = 0; i < amount && IsConflict(a.Status); i++) a = Reattempt(attempt, a, waiter);
        else if (kind == FileRetryKind.Forever)
            // GR3 — until the operation completes, for as long as the holder is one that can release.
            do a = Reattempt(attempt, a, waiter);
            while (IsConflict(a.Status) && a.HolderOutsideRunUnit);
        // The two arms with no `else` are deliberate, not forgotten: FileRetryKind.None makes no further
        // attempt by GR4a, and FileRetryKind.Seconds makes none because GR2 clamps its period to this
        // implementation's maximum meaningful value of ZERO (A.1 item 166) — a zero-length timeout period
        // during which no retry can be attempted. Seconds still RECEIVES its amount because that value is
        // GR4a's screen input, even though the clamp then makes it inert.
        return IsConflict(a.Status) ? ExhaustionStatus(a.Status, kind) : a.Status;
    }

    /// <summary>§14.7.9.3 GR1's <i>"The implementor determines the interval between these attempts"</i> — WiseOwl
    /// COBOL's interval (Annex A.1 item 165, docs/CONFORMANCE.md §7): 100 milliseconds before a re-attempt against a
    /// holder outside the executing run unit, and none against a holder inside it. The same interval paces a
    /// FOREVER wait (GR3).</summary>
    public static readonly TimeSpan RetryInterval = TimeSpan.FromMilliseconds(100);

    /// <summary>The next attempt after <paramref name="previous"/> failed: it waits <see cref="RetryInterval"/> first
    /// only when the previous conflict's holder is outside the run unit, the one case a wait can change the
    /// answer.</summary>
    private static RetryAttempt Reattempt(Func<RetryAttempt> attempt, RetryAttempt previous, FileConnector? waiter)
    {
        if (previous.HolderOutsideRunUnit)
        {
            if (waiter is not null) waiter.WaitOutsideStatement(Pause);
            else Pause();
        }
        return attempt();
    }

    /// <summary>One RETRY interval — observed instead of slept when a test is watching.</summary>
    private static void Pause()
    {
        if (PauseObserver.Value is { } observe) observe(RetryInterval);
        else Thread.Sleep(RetryInterval);
    }

    /// <summary>Test seam: when set, a retry pause is REPORTED instead of performed, so a test asserts WHETHER the
    /// discipline waits without timing a real sleep (the pattern of <c>CobolTiming.SuspensionObserver</c>, kb/Work
    /// PB1590). An <see cref="AsyncLocal{T}"/>, so only the calling test's own flow observes.</summary>
    internal static readonly AsyncLocal<Action<TimeSpan>?> PauseObserver = new();

    /// <summary>True when <paramref name="status"/> is one of the two conditions §14.7.9.3 GR4 names as the
    /// RETRY phrase's subject: a RECORD OPERATION conflict (§9.1.13.8, first digit '5') or a FILE SHARING
    /// conflict (§9.1.13.9, first digit '6'). The first digit IS the standard's own classification of these
    /// clauses (§9.1.13.1, which maps '5' → EC-I-O-RECORD-OPERATION and '6' → EC-I-O-FILE-SHARING).</summary>
    private static bool IsConflict(string status) =>
        status.Length == 2 && (status[0] == '5' || status[0] == '6');

    /// <summary>The status an exhausted retry lands on. §14.7.9.3 GR4a and that clause's closing paragraph give
    /// the SAME landing — "the appropriate value is placed in the I-O status associated with the file connector
    /// according to the rules for 9.1.13" — so the answer is the CONFLICT'S OWN status, with exactly one
    /// documented implementor exception.
    /// <para>⛔ THE EXCEPTION IS CLASS-SCOPED, and harmonizing the two classes breaks conformance in one
    /// direction or two green goldens in the other. §9.1.13.9 (file sharing) defines exactly TWO values — '61'
    /// for OPEN and '62' for DELETE FILE — and NO deadlock value, so a file-sharing conflict has no landing but
    /// its own; §14.9.10.4 GR15b is imperative there ("The value … is placed") where its record-conflict twin
    /// GR6b says only "A value". §9.1.13.8 item 2's '52' is a RECORD-conflict value whose detection conditions
    /// the implementor defines (A.1 item 109, recorded in docs/CONFORMANCE.md §7): WiseOwl COBOL detects a deadlock
    /// exactly when a FOREVER retry waits on a record locked by another file connector (§9.1.13.8 item 1), since
    /// that holder is inside the executing run unit and can never release while this statement runs, so GR3's
    /// "until the operation has been completed" would never terminate.</para>
    /// <para>Answering '52' for a FILE conflict would also raise the WRONG exception-name — §9.1.13.1 maps the
    /// first digit — so a USE declarative or exception-checking PERFORM keyed on EC-I-O-FILE-SHARING would
    /// silently not fire (kb/Work PB142).</para></summary>
    private static string ExhaustionStatus(string conflict, FileRetryKind kind) =>
        kind == FileRetryKind.Forever && conflict == FileStatusCode.RecordLocked
            ? FileStatusCode.Deadlock   // §9.1.13.8 item 2 — the implementor-detected deadlock
            : conflict;                 // §14.7.9.3 closing paragraph → §9.1.13

    // ── Internals ────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The connector's LIVE §12.4.5.3 GR3 association — the resolved host path it is associated with
    /// right now (an unknown name answers with the key itself, which the physical-file table treats as its own
    /// bucket). Public because the association is a queryable property of the connector, not registry-private
    /// state: it changes at every OPEN/SORT/MERGE, so nothing may cache it.</summary>
    public string HostPathOf(string name) => _files.TryGetValue(name, out var c) ? c.HostPath : name;

    private void SetStatusOf(string name, string status) => Require(name).SetStatus(status);

    /// <summary>Deregister a connector's open entry on CLOSE and release its record locks (§9.1.15's <i>"The file
    /// lock is removed by an explicit or implicit CLOSE statement executed for that file connector"</i> /
    /// §14.9.6.4 GR9). ⛔ UNCONDITIONAL: every successful OPEN registers, so every CLOSE must deregister — while
    /// this was gated on <c>_connectorShares</c> a plain connector could not have been registered in the first
    /// place, and making the registration total without making the release total would have left a closed
    /// connector arbitrating forever (kb/Work PB321). It runs only inside the CLOSE dispatch, which every close
    /// path enters (kb/Work PB603), with the connector the dispatch already holds — so a displaced registration is
    /// deregistered under its OWN host path, before its replacement is stored.</summary>
    private void SharedClose(string name, FileConnector c)
    {
        if (!_physical.TryGet(c.HostPath, out var st)) return;
        st.Open.Remove(name);
        // The lock release is guarded on there being locks at all: this now runs on EVERY close of EVERY file,
        // and ReleaseAllForConnector's Where/Select/ToList would otherwise allocate a closure and an empty list
        // per CLOSE for the overwhelming majority of connectors, which set no record locks — §12.4.5.9.4 GR1 a),
        // "If there is a SHARING clause in that file control entry, no record locks are set by the execution of
        // I-O statements through the associated file connector" (the general rules of the LOCK MODE clause are
        // §12.4.5.9.4, and every citation of a LOCK MODE rule in this file names it).
        // ... EXCEPT those of a file in an APPLY COMMIT clause: §14.9.6.4 GR9, "Except when the file is specified in an APPLY
        // COMMIT clause, the file lock and any record locks associated with the file connector referenced by file-name-1 are
        // released by the execution of the CLOSE statement", and §9.1.16 releases them at a COMMIT or ROLLBACK
        // (ReleaseCommitLocks), or the run unit's end (CloseAll) - the connector's own decision (FileConnector.CloseKeepsLocks),
        // the one its handle's keeping rests on, kb/Work PB3480.
        if (st.RecordLocks.Count > 0 && !c.CloseKeepsLocks) PhysicalFileTable.ReleaseAllForConnector(st, name);
    }
}
