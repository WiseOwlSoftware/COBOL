// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
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
    private readonly Dictionary<string, FileConnector> _files = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _locked = new(StringComparer.OrdinalIgnoreCase);
    private int _instSeq;   // the per-object instance-file connector-key sequence (M2-OO-1i; reset for determinism)
    // The GC finalizer thread (~CobolObject) can request a per-object CLOSE at any moment, but the registry is a
    // single-thread structure — so the finalizer only ENQUEUES the key (thread-safe); the actual CloseAndDrop
    // runs on the mutator thread when it next drains (Reset / Open / CloseAll). §9.1.4's NOTE licenses this
    // GC-deferred close.
    private readonly System.Collections.Concurrent.ConcurrentQueue<string> _pendingObjectClose = new();

    /// <summary>A connector's declared sharing posture (from its SELECT's SHARING / LOCK MODE clauses).</summary>
    private readonly record struct ConnectorShare(string Host, FileSharing Sharing, FileLockMode LockMode, bool Multiple);

    /// <summary>The per-connector declared sharing/lock metadata (a connector is "sharing-active" iff present).</summary>
    private readonly Dictionary<string, ConnectorShare> _connectorShares = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The physical-file sharing/record-lock registry (§9.1.15/§9.1.16).</summary>
    private readonly PhysicalFileTable _physical = new();

    /// <summary>The per-physical-file KEYED record stores (kb/Work PB143 — §14.9.10.4 GR5): one record store
    /// per resolved host path, attached by every relative/indexed connector over it, so mutations are visible
    /// across connectors and the close order cannot pick a surviving private view.</summary>
    private readonly KeyedStoreTable _stores = new();

    // ── Run-unit lifecycle ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>Reset the registry (run-unit start): drain GC closes, close the sequential connectors (the
    /// pre-registry semantics — keyed connectors are dropped without a close, exactly as before), clear
    /// everything, restart the instance-key sequence, and clear the sharing registries.</summary>
    public void Reset()
    {
        DrainPendingObjectCloses();
        foreach (var c in _files.Values)
            if (c is SequentialConnector s)
                s.Close();
        _files.Clear();
        _locked.Clear();
        _instSeq = 0;
        while (_pendingObjectClose.TryDequeue(out _)) { }   // a new run unit starts with a clean queue
        _connectorShares.Clear();
        _physical.Clear();
        _stores.Clear();   // kb/Work PB143 — a new run unit re-reads every physical file
    }

    /// <summary>Request the §9.1.4 deletion-time CLOSE of a per-object connector FROM THE GC FINALIZER THREAD:
    /// only enqueue the key; the mutator thread performs the real close in <see cref="DrainPendingObjectCloses"/>.</summary>
    public void EnqueueInstanceClose(string key) => _pendingObjectClose.Enqueue(key);

    /// <summary>Perform any GC-requested per-object closes on the MUTATOR thread (called at the top of
    /// Reset / Open / CloseAll — safe points where no other registry mutation is in flight).</summary>
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
        Close(key);
        _files.Remove(key);
        _locked.Remove(key);
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
        bool optional, int varyMin, int varyMax, string? selectName = null)
    {
        if (cobolName.StartsWith("::EXT::", StringComparison.Ordinal) && _files.ContainsKey(cobolName))
            return;   // the run-unit EXTERNAL connector already exists (§13.18.22.4 GR4a)
        CloseDisplaced(cobolName);
        _files[cobolName] = new SequentialConnector(CobolFile.ResolveHostPath(assignTarget), recordWidth,
            lineSequential, varyMin, varyMax) { IsOptional = optional, SelectName = selectName ?? KeyTail(cobolName) };
    }

    /// <summary>Close a still-open INTERNAL connector a registration is about to replace (kb/Work PB168):
    /// NO normal path replaces an open connector — a unit-scoped (RECURSIVE) unit registers once per run
    /// unit behind its static guard, an INITIAL unit's files are implicitly closed at its termination, and
    /// CANCEL closes before its post-CANCEL re-registration — so an open one here was abandoned by an
    /// abnormally-ended activation. Closing flushes its buffered writes and frees the OS handle instead of
    /// leaking both. The status result is deliberately dropped: this is registry hygiene, not a COBOL CLOSE
    /// — there is no statement to report to.</summary>
    private void CloseDisplaced(string cobolName)
    {
        if (_files.TryGetValue(cobolName, out var old) && old.IsOpen) old.Close();
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
        int accessMode, int relativeKeyDigits, int varyMin, int varyMax, string? selectName = null)
    {
        if (cobolName.StartsWith("::EXT::", StringComparison.Ordinal) && _files.ContainsKey(cobolName))
            return;   // §13.18.22.4 GR4a
        CloseDisplaced(cobolName);
        _files[cobolName] = new RelativeConnector(CobolFile.ResolveHostPath(assignTarget), recordWidth,
            (KeyedAccess)accessMode, relativeKeyDigits, varyMin, varyMax)
        { IsOptional = optional, SelectName = selectName ?? KeyTail(cobolName), SharedStores = _stores };
    }

    /// <summary>Register a SELECTed INDEXED file with its PRIME key's (offset, length) range (§12.4.5.12).</summary>
    public void RegisterIndexed(string cobolName, string assignTarget, int recordWidth, bool optional,
        int accessMode, int primeOffset, int primeLength, int varyMin, int varyMax, CobolCollation? primeCollation = null,
        string? selectName = null)
    {
        if (cobolName.StartsWith("::EXT::", StringComparison.Ordinal) && _files.ContainsKey(cobolName))
            return;   // §13.18.22.4 GR4a
        CloseDisplaced(cobolName);
        _files[cobolName] = new IndexedConnector(CobolFile.ResolveHostPath(assignTarget), recordWidth,
            (KeyedAccess)accessMode, primeOffset, primeLength, varyMin, varyMax, primeCollation)
        { IsOptional = optional, SelectName = selectName ?? KeyTail(cobolName), SharedStores = _stores };
    }

    /// <summary>Register one ALTERNATE RECORD KEY (§12.4.5.6), in declaration order, with its optional
    /// §12.4.5.7 collating-weight table (null = native ordinal) and §12.4.5.6.4 GR6 SUPPRESS WHEN value.</summary>
    public void AddAlternateKey(string name, int offset, int length, bool duplicates, CobolCollation? collation = null, string? suppress = null)
    {
        if (_files.TryGetValue(name, out var c) && c is IndexedConnector ix)
            ix.AddAlternateKey(offset, length, duplicates, collation, suppress);
    }

    // ── OPEN / CLOSE ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>OPEN in <paramref name="mode"/> (ISO §14.9.27): a sharing-active connector routes through the
    /// physical-file registry (Table-19 → 61); every other connector keeps the exclusive path (GR23 implementor
    /// default) — one polymorphic dispatch for all three organizations.</summary>
    public void Open(string name, FileOpenMode mode)
    {
        DrainPendingObjectCloses();   // reclaim any GC-finalized per-object connectors on this (mutator) thread first
        if (IsSharingActive(name)) { SharedOpenAttempt(name, mode, null); return; }
        if (_locked.Contains(name)) Require(name).SetStatus(FileStatusCode.FileLocked);
        else Require(name).Open(mode);
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
    /// §9.1.16).</summary>
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
                + "which require a reel/unit-structured medium — COBOL.NET supports none (docs/CONFORMANCE.md "
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
        // c) Close file: "Closing operations specified by the implementor are executed." COBOL.NET's are
        // documented at docs/CONFORMANCE.md §7, A.1 item 24 (which makes them a required documented
        // item) — flush/persist/dispose in CloseCore, plus the §14.9.6.4 GR9 lock release here.
        SharedClose(name);   // no-op for a non-sharing-active connector
        c.Close();
        // g) Optional phrases ignored: "The CLOSE statement is executed as if none of the optional phrases were
        // present. The I-O status indicator for the file connector is set to '07'." — §9.1.13.2 item 6's
        // phrase-on-a-non-reel-medium warning, which rides a SUCCESSFUL close only (an unsuccessful '30' keeps
        // its own status) and is not owed for the GR6 absent-optional case (kb/Work PB141).
        if (cell.HasFlag(CloseSymbol.PhrasesIgnored) && !absent && c.Status[0] == '0')
            c.SetStatus(FileStatusCode.PhraseOnNonReelMedium);
    }

    /// <summary>The IMPLICIT close surface (§14.9.5 GR9's "each … file connector that is open"): a closed
    /// connector is skipped — no '42' stamp, no shared-close bookkeeping — and so is a name the registry
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
    /// equivalent). On the Non-unit medium COBOL.NET supports that cell is symbol e alone: the file REMAINS
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

    /// <summary>Close every open file (run-unit termination, ISO §14.6 — flushes print streams; keyed stores
    /// persist, e.g. NIST RL208A).</summary>
    public void CloseAll()
    {
        DrainPendingObjectCloses();
        foreach (var c in _files.Values)
        {
            // The pre-registry semantics, preserved exactly: sequential connectors close unconditionally
            // (a closed one just re-reports '42' into a cleared registry); keyed connectors close only if open.
            if (c is SequentialConnector s) s.Close();
            else if (c.IsOpen) c.Close();
        }
    }

    // ── Sequential-surface verbs (keyed connectors are reached via the keyed entries below) ─────────────────

    /// <summary>Plain <c>WRITE record</c> (ISO §14.9.46); <paramref name="length"/> is the varying-record length
    /// (§13.18.43 GR13a), -1 = the record's own size.</summary>
    public void Write(string name, string image, int length)
    { if (_files.TryGetValue(name, out var c) && c is SequentialConnector f) f.Write(image, length); }

    /// <summary><c>WRITE record {BEFORE|AFTER} ADVANCING {n LINES | PAGE}</c>; <paramref name="lines"/> = -1 is PAGE.</summary>
    public void WriteAdvancing(string name, string image, int lines, bool before)
    { if (_files.TryGetValue(name, out var c) && c is SequentialConnector f) f.WriteAdvancing(image, lines, before); }

    /// <summary>COBOL-2023 combined <c>WRITE record BEFORE ADVANCING n AFTER ADVANCING m</c> (ISO §14.9.51 GR25e/f).</summary>
    public void WriteBeforeAndAfter(string name, string image, int beforeLines, int afterLines)
    { if (_files.TryGetValue(name, out var c) && c is SequentialConnector f) f.WriteBeforeAndAfter(image, beforeLines, afterLines); }

    /// <summary>Install a LINAGE file's logical-page evaluator (ISO §13.18.34 GR6).</summary>
    public void SetLinage(string name, Func<(int Body, int Footing, int Top, int Bottom)> eval)
    { if (_files.TryGetValue(name, out var c) && c is SequentialConnector f) f.SetLinage(eval); }

    /// <summary>The file's LINAGE-COUNTER register (ISO §8.4.3.14 / §13.18.34 GR7).</summary>
    public long LinageCounter(string name) =>
        _files.TryGetValue(name, out var c) && c is SequentialConnector f ? f.LinageCounter : 0;

    /// <summary>The end-of-page condition of the file's most recent WRITE (ISO §14.9.51 GR26a/b).</summary>
    public bool EndOfPage(string name) =>
        _files.TryGetValue(name, out var c) && c is SequentialConnector f && f.EndOfPage;

    /// <summary>Sequential <c>READ … NEXT</c> — the record image and whether a record was obtained.</summary>
    public bool Read(string name, out string image)
    {
        if (_files.TryGetValue(name, out var c) && c is SequentialConnector f) return f.Read(out image);
        image = "";
        return false;
    }

    /// <summary>Sequential <c>REWRITE record</c> (ISO §14.9.35).</summary>
    public void Rewrite(string name, string image, int length)
    { if (_files.TryGetValue(name, out var c) && c is SequentialConnector f) f.Rewrite(image, length); }

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

    /// <summary>The open-mode view for USE-declarative mode scoping (ISO §14.9.49.4 GR6b–e); −1 unknown/closed.</summary>
    public int OpenModeOf(string name) => _files.TryGetValue(name, out var c) ? c.OpenModeView : -1;

    // ── Keyed verbs (ISO §14.9.51 / §14.9.35 / §14.9.30 / §14.9.41 / §14.9.10) ───────────────────────────────

    /// <summary>Stage the RELATIVE KEY item's value for the next keyed verb.</summary>
    public void SetRelativeKey(string name, long rrn)
    { if (_files.TryGetValue(name, out var c) && c is RelativeConnector r) r.SetPendingKey(rrn); }

    /// <summary>The RRN last made available/released — the §14.9.30 GR25 / §14.9.51 GR29a MOVE-back source.</summary>
    public long RelativeSlot(string name) =>
        _files.TryGetValue(name, out var c) && c is RelativeConnector r ? r.LastSlot : 0;

    /// <summary>Keyed WRITE (§14.9.51) — returns the I-O status.</summary>
    public string WriteKeyed(string name, string image, int length) => Require(name) switch
    {
        RelativeConnector r => r.Write(image, length),
        IndexedConnector ix => ix.Write(image, length),
        var other => throw MisroutedVerb("keyed WRITE", name, other),
    };

    /// <summary>Keyed REWRITE (§14.9.35) — returns the I-O status.</summary>
    public string RewriteKeyed(string name, string image, int length) => Require(name) switch
    {
        RelativeConnector r => r.Rewrite(image, length),
        IndexedConnector ix => ix.Rewrite(image, length),
        var other => throw MisroutedVerb("keyed REWRITE", name, other),
    };

    /// <summary>DELETE RECORD (§14.9.10 F1); for indexed random/dynamic the prime key is sliced from
    /// <paramref name="keyedRecordImage"/> (GR3) — relative uses the staged relative key (GR4).</summary>
    public string DeleteRecord(string name, string keyedRecordImage) => Require(name) switch
    {
        RelativeConnector r => r.Delete(),
        IndexedConnector ix => ix.Delete(keyedRecordImage),
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
        return Require(name) switch
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
        return Require(name) switch
        {
            RelativeConnector r => r.ReadPrevious(out image),
            IndexedConnector ix => ix.ReadPrevious(out image),
            var other => throw MisroutedVerb("keyed READ PREVIOUS", name, other),
        };
    }

    /// <summary>Random keyed READ (§14.9.30 F2): indexed slices the key value from
    /// <paramref name="keyedRecordImage"/> (GR30–GR32); relative uses the staged relative key (GR29).</summary>
    public string ReadKeyed(string name, int keyIndex, string keyedRecordImage, out string image)
    {
        image = "";
        return Require(name) switch
        {
            RelativeConnector r => r.ReadRandom(out image),
            IndexedConnector ix => ix.ReadRandom(keyIndex, keyedRecordImage, out image),
            var other => throw MisroutedVerb("keyed READ", name, other),
        };
    }

    /// <summary>START on a relative file (§14.9.41 GR8–GR12) — a numeric RRN comparison.</summary>
    public string StartRelative(string name, string op, long rrn) =>
        Require(name) is RelativeConnector r ? r.Start(op, rrn)
        : throw MisroutedVerb("START (relative)", name, Require(name));

    /// <summary>START on an indexed file (§14.9.41 GR13–GR17) — a leftmost-length partial-key comparison.</summary>
    public string StartIndexed(string name, int keyIndex, string op, string operand, int compareLength) =>
        Require(name) is IndexedConnector ix ? ix.Start(keyIndex, op, operand, compareLength)
        : throw MisroutedVerb("START (indexed)", name, Require(name));

    /// <summary>START FIRST/LAST (COBOL-2002+; §14.9.41 GR11/GR12), either keyed organization.</summary>
    public string StartFirstLast(string name, bool last) => Require(name) switch
    {
        RelativeConnector r => r.StartFirstLast(last),
        IndexedConnector ix => ix.StartFirstLast(last),
        var other => throw MisroutedVerb("START FIRST/LAST", name, other),
    };

    /// <summary>DELETE FILE (§14.9.10 Format 2, COBOL-2023): an OPEN connector → '41' (GR13); the physical file
    /// currently open by ANOTHER file connector → the file sharing conflict, '62' (GR15 / §9.1.13.9 item 2),
    /// re-attempted under a RETRY phrase (GR15 → §14.7.9; in one run unit the other connector cannot close
    /// mid-loop, so EVERY retry form exhausts to '62' — §9.1.13.9 defines no deadlock value for a file-sharing
    /// conflict, see <see cref="ExhaustionStatus"/>); an ABSENT physical file is
    /// a SUCCESSFUL completion, status '05' (GR14); insufficient authority → '37' (GR16) — ONE polymorphic body
    /// over <see cref="FileConnector"/> for all three organizations.</summary>
    public string DeleteFile(string name, bool overridden = false) =>
        DeleteFile(name, FileRetryKind.None, 0, overridden);

    /// <summary>DELETE FILE with a RETRY phrase (§14.9.10 GR15 / §14.7.9) and the GR18 OVERRIDE flag.</summary>
    public string DeleteFile(string name, FileRetryKind retryKind, int retryAmount, bool overridden = false)
    {
        var c = Require(name);
        // (a DELETE FILE accesses the connector — FUNCTION EXCEPTION-FILE r2a, §15.28.4 — recorded by the status
        // assignment below, the ONE access-recording path)
        string status;
        string sharing = RetryLoop(() => OpenByAnotherConnector(name, c.HostPath)
            ? FileStatusCode.DeleteFileSharing : FileStatusCode.Success, retryKind, retryAmount);
        if (c.IsOpen) status = FileStatusCode.FileAlreadyOpen;             // '41' GR13
        else if (ValidateFixedFileAttributes(c, overridden) is { } conflict)
            status = conflict;                                             // '39' GR18 — see the method
        else if (sharing != FileStatusCode.Success)
            status = sharing;   // '62' GR15/§9.1.13.9 item 2 — the file is not deleted, under every retry form
        else
            try
            {
                // GR14's '05' is only for a file that is ABSENT. File.Exists swallows every access error and
                // answers false, classifying a PRESENT-but-unobservable file as gone — GR16 requires '37'
                // there — so the probe is File.GetAttributes, whose failures DISTINGUISH the two (PB140).
                File.GetAttributes(c.HostPath);
                File.Delete(c.HostPath);
                status = FileStatusCode.Success;
            }
            catch (FileNotFoundException) { status = FileStatusCode.OptionalFileNotFound; }      // '05' GR14 — successful
            catch (DirectoryNotFoundException) { status = FileStatusCode.OptionalFileNotFound; } // '05' GR14
            catch (UnauthorizedAccessException) { status = FileStatusCode.PermissionDenied; }    // '37' GR16
            catch (IOException ex) { status = FileStatusCode.ForDeleteFileFailure(ex); }         // '37' GR17 / '30'
        // The physical file is gone, so its §9.1.6 fixed file attributes are gone with it: drop the catalog
        // sidecar too, or it would outlive the file and be compared (§14.9.27.4 GR10) against a DIFFERENT file
        // later created at the same path by something other than a COBOL.NET OPEN OUTPUT — which is exactly the
        // "attributes not recorded" state FixedFileAttributes.Load answers null for. This is the ONE place a
        // data file is deleted in the runtime (swept). The condition is the whole SUCCESS FAMILY, not just
        // '00': GR14 makes '05' — the file was already absent — a SUCCESSFUL completion, and a sidecar left
        // beside an already-absent file is the same stale catalog by another route. '37', '39', '41', '62' and
        // '30' all leave the file in place, so they leave its attributes in place too.
        if (status[0] == '0') FixedFileAttributes.Remove(c.HostPath);
        c.SetStatus(status);
        return status;
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
    /// persisted physical-attribute catalog OPEN still needs and does not have (kb/Work PB193) — a change to ONE
    /// method instead of a silent behaviour change for every program that wrote OVERRIDE to opt out (kb/Work
    /// PB196). A non-empty set plugs in below the guard, and nowhere else.</para></summary>
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
            if (!string.Equals(other, name, StringComparison.OrdinalIgnoreCase)
                && c.IsOpen && string.Equals(c.HostPath, host, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    // ── COBOL-2002 file sharing / record locking (ISO §9.1.15/§9.1.16/§14.9.27/§14.9.47/§14.7.9) ─────────────
    // Design D1: the 51/52/61 statuses are defined over "another file connector" (§9.1.13.9) — two SELECTs bound
    // to one resolved host path are two distinct connectors over one physical file within one run unit, so the
    // machinery is REAL. Single-run-unit residue (loud, documented): no RETRY form can block productively (no
    // external releaser exists), so an unsatisfiable conflict lands on the conflict's OWN §9.1.13 status —
    // never a sleep, and never a manufactured one. See D8 in docs/COBOLNET_FILES_DESIGN.md and ExhaustionStatus.

    /// <summary>Register a SELECTed file's declared SHARING / LOCK MODE (emitted right after registration, only
    /// for a file that carries either clause). Marks the connector sharing-active so its OPEN routes through the
    /// physical-file registry.</summary>
    public void RegisterSharing(string name, FileSharing sharing, FileLockMode lockMode, bool multiple)
    {
        _connectorShares[name] = new ConnectorShare(HostPathOf(name), sharing, lockMode, multiple);
        // A sharing participant's physical streams must admit the other connectors' handles (§9.1.15) — the
        // Table-19 registry, not the OS handle, arbitrates; unshared connectors keep the exclusive OS posture.
        if (_files.TryGetValue(name, out var c)) c.SharedStreams = true;
    }

    /// <summary>True when <paramref name="name"/> participates in the sharing subsystem.</summary>
    private bool IsSharingActive(string name) => _connectorShares.ContainsKey(name);

    /// <summary>OPEN with an explicit SHARING override and/or a RETRY phrase (§14.9.27) — the emitter's entry
    /// point when the OPEN statement itself carries a sharing/retry phrase.</summary>
    public void OpenShared(string name, FileOpenMode mode, bool hasSharingOverride, FileSharing sharingOverride,
        FileRetryKind retryKind, int retryAmount)
    {
        // A sharing/retry phrase on the OPEN makes the connector sharing-active even without a SELECT clause.
        if (!_connectorShares.ContainsKey(name))
            RegisterSharing(name, FileSharing.AllOther, FileLockMode.None, false);
        FileSharing? ov = hasSharingOverride ? sharingOverride : null;
        // SharedOpenAttempt sets the connector status on the terminal attempt, and RetryLoop now lands an
        // exhausted retry on the CONFLICT'S OWN status (§14.7.9.3 closing paragraph → §9.1.13.9 item 1 = '61'),
        // so there is nothing left to override here — the former `if (status == Deadlock) SetStatusOf(…)`
        // line existed only to re-assert the '52' RetryLoop used to manufacture (kb/Work PB142).
        _ = RetryLoop(() => SharedOpenAttempt(name, mode, ov), retryKind, retryAmount);
    }

    /// <summary>The sharing-aware OPEN body. Returns the resulting I-O status; on a Table-19 conflict returns 61
    /// without opening the connector.</summary>
    private string SharedOpenAttempt(string name, FileOpenMode mode, FileSharing? sharingOverride)
    {
        if (!_files.TryGetValue(name, out var c)) return FileStatusCode.PermanentError;
        if (_locked.Contains(name)) { c.SetStatus(FileStatusCode.FileLocked); return FileStatusCode.FileLocked; }  // ≤2014 CLOSE WITH LOCK
        FileSharing sharing = sharingOverride ?? _connectorShares[name].Sharing;
        var st = _physical.For(c.HostPath);
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
        if (c.IsOpen)
        {
            st.Open[name] = (sharing, mode);   // register only a successful open
            // A sharing-active SEQUENTIAL connector needs its write-ordinal base (§9.1.16 lock identity for
            // records it releases; §14.9.51 GR18 — EXTEND appends continue the existing numbering). Seeded here,
            // never for an unshared connector, so unshared sequential I/O is untouched by the lock subsystem.
            if (c is SequentialConnector f) f.SeedSharedWriteBase();
        }
        return c.Status;
    }

    /// <summary>Table-19 open-conflict classification (§9.1.13.9 sub-cases a–e).</summary>
    public static bool Conflicts((FileSharing Sharing, FileOpenMode Mode) ex, (FileSharing Sharing, FileOpenMode Mode) inc)
    {
        if (ex.Sharing == FileSharing.NoOther || inc.Sharing == FileSharing.NoOther) return true;       // (a)/(b)
        if (ex.Sharing == FileSharing.ReadOnly && inc.Mode != FileOpenMode.Input) return true;          // (c)
        if (inc.Sharing == FileSharing.ReadOnly && ex.Mode != FileOpenMode.Input) return true;          // (d)
        return false;                                                                                    // (e) ALL OTHER
    }

    /// <summary>Record-lock governance for a just-completed keyed READ (§9.1.16; called right after the physical
    /// read for any sharing-active file). Returns the effective status (and stores it on the connector).
    /// (The keyed shape is post-read because a NEXT/PREVIOUS walk's record identity is only known after the read;
    /// the sequential-organization twin is <see cref="ReadShared"/>, whose next ordinal is knowable BEFORE the
    /// read, keeping the file position indicator untouched on a conflict — §14.9.30 GR10a.)</summary>
    public string ReadLockGovern(string name, string statusJustRead, FileRecordLock phrase,
        FileRetryKind retryKind, int retryAmount)
    {
        if (statusJustRead.Length == 0 || statusJustRead[0] != '0') return statusJustRead;   // an unsuccessful read: no locking
        if (!_connectorShares.TryGetValue(name, out var meta)) return statusJustRead;         // not sharing-active
        if (!_files.TryGetValue(name, out var c)) return statusJustRead;
        string recId = c.LastReadRecordId;
        if (recId.Length == 0) return statusJustRead;   // no record was identified
        var st = _physical.For(meta.Host);

        if (phrase != FileRecordLock.Ignoring)
        {
            // Another connector's lock blocks the read (§14.9.30 GR9). RETRY re-checks; in one run unit the
            // holder cannot release mid-loop, so n TIMES exhausts to 51 and SECONDS/FOREVER bail to 52 (GR4a).
            string conflict = RetryLoop(
                () => PhysicalFileTable.IsLockedByOther(st, name, recId) ? FileStatusCode.RecordLocked : FileStatusCode.Success,
                retryKind, retryAmount);
            if (conflict != FileStatusCode.Success)
            {
                SetStatusOf(name, conflict);   // the status assignment drops the '43' gate (PB140); FPI kept (GR10a)
                return conflict;
            }
        }
        return ApplyReadLockDiscipline(meta, st, name, recId, phrase) is { } denied ? denied : statusJustRead;
    }

    /// <summary>The §14.9.30 GR11 post-read lock actions, shared by the keyed and sequential read paths:
    /// GR11a — single record locking releases any prior lock this connector holds (§12.4.5.9 GR6: any I-O
    /// statement except START releases it); GR11c/d — the lock on the accessed record is set under AUTOMATIC,
    /// or under MANUAL only with the WITH LOCK phrase; GR11b — under multiple locking WITH NO LOCK releases a
    /// lock this connector already held on the accessed record. Returns a denial status (53/54, §12.4.5.9 GR7)
    /// or null.</summary>
    private string? ApplyReadLockDiscipline(ConnectorShare meta, PhysicalFileTable.State st, string name,
        string recId, FileRecordLock phrase)
    {
        if (!meta.Multiple) PhysicalFileTable.ReleaseAllForConnector(st, name);   // GR11a / §12.4.5.9 GR6
        bool wantLock = phrase switch
        {
            FileRecordLock.Ignoring => false,
            FileRecordLock.WithNoLock => false,
            FileRecordLock.WithLock => true,
            _ => meta.LockMode == FileLockMode.Automatic,   // no phrase: AUTOMATIC auto-locks (§12.4.5.9 GR4), MANUAL does not (GR5)
        };
        if (wantLock && LocksEffective(meta, st, name))
        {
            string acq = _physical.LockRecord(st, name, recId);
            if (acq != FileStatusCode.Success) { SetStatusOf(name, acq); return acq; }
        }
        else if (meta.Multiple && phrase == FileRecordLock.WithNoLock)
            PhysicalFileTable.ReleaseSingle(st, name, recId);   // GR11b — an already-held lock on this record releases
        return null;
    }

    /// <summary>Whether this connector SETS record locks: §12.4.5.9 GR1a/b1 — with no LOCK MODE clause a
    /// SHARING clause/phrase means NO record locks are set (and the implementor default here is likewise none);
    /// GR3 — a connector open in the sharing-with-no-other mode has exclusive access, so its LOCK MODE has no
    /// effect. (Conflict CHECKS against locks OTHER connectors hold are never disabled — §9.1.16: a locked
    /// record is inaccessible to another file connector regardless of that connector's own lock mode.)</summary>
    private static bool LocksEffective(ConnectorShare meta, PhysicalFileTable.State st, string name)
    {
        if (meta.LockMode == FileLockMode.None) return false;                                   // GR1a/b1
        if (st.Open.TryGetValue(name, out var open) && open.Sharing == FileSharing.NoOther)
            return false;                                                                        // GR3
        return true;
    }

    /// <summary>Sequential-organization governed READ (§9.1.16 on sequential files — the READ lock rules
    /// §14.9.30 GR7–GR12 are ALL-FORMATS rules). The next record's ordinal is knowable BEFORE the read, so the
    /// conflict check precedes the physical read: on a record-operation conflict the file position indicator is
    /// UNCHANGED (GR10a) and the record area untouched (GR10c refined to "unchanged", the documented COBOL.NET
    /// refinement). ADVANCING ON LOCK (GR22) skip-scans locked records — each is read-and-discarded "as if the
    /// locked record were read and the same READ statement were executed" — with no conflict condition; reaching
    /// end-of-file is the ordinary at-end. Returns true iff a record was made available (the emitted contract of
    /// the plain <see cref="Read"/>).</summary>
    public bool ReadShared(string name, FileRecordLock phrase, bool advancingOnLock,
        FileRetryKind retryKind, int retryAmount, out string image)
    {
        if (!_files.TryGetValue(name, out var c) || c is not SequentialConnector f) { image = ""; return false; }
        if (!_connectorShares.TryGetValue(name, out var meta)) return f.Read(out image);   // not sharing-active — phrases inert (§12.4.5.9 GR1)
        var st = _physical.For(meta.Host);
        while (true)
        {
            if (phrase != FileRecordLock.Ignoring && f.ReadEligible)   // a mode/position failure keeps its own status
            {
                string recId = f.NextReadOrdinal.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (PhysicalFileTable.IsLockedByOther(st, name, recId))
                {
                    if (advancingOnLock)
                    {
                        // §14.9.30 GR22 — skip the locked record (read + discard) and repeat; no conflict raised.
                        if (!f.Read(out image)) return false;   // end of file → at end ('10'), GR22 tail
                        continue;
                    }
                    string conflict = RetryLoop(
                        () => PhysicalFileTable.IsLockedByOther(st, name, recId) ? FileStatusCode.RecordLocked : FileStatusCode.Success,
                        retryKind, retryAmount);
                    if (conflict != FileStatusCode.Success)
                    {
                        f.SetStatus(conflict);   // the status assignment drops the '43' gate (PB140); FPI kept (GR10a)
                        image = "";
                        return false;
                    }
                }
            }
            bool ok = f.Read(out image);
            if (ok && ApplyReadLockDiscipline(meta, st, name, f.LastReadRecordId, phrase) is not null)
            {
                image = "";   // a 53/54 lock-denial is an unsuccessful READ (§12.4.5.9 GR7) — no record available
                return false;
            }
            return ok;
        }
    }

    /// <summary>Governed WRITE for a sharing-active connector, any organization (§14.9.51 GR10/GR11 + §14.7.9).
    /// No record-operation conflict is defined for WRITE — §9.1.13.8's 51 covers "an attempt to ACCESS a record",
    /// the WRITE general rules define no 51 leg, and GR33/GR42 state the invalid-key checks ignore record locks;
    /// GR16's RETRY governs implementor "resources … locked by another run unit", which cannot arise in-process,
    /// so the first attempt decides. Returns the I-O status.</summary>
    public string WriteShared(string name, string image, int length, FileRecordLock phrase,
        FileRetryKind retryKind, int retryAmount)
    {
        _ = retryKind; _ = retryAmount;   // §14.9.51 GR16 — see the summary; kept in the signature as the bound RETRY carrier
        if (!_files.TryGetValue(name, out var c)) return FileStatusCode.PermanentError;
        if (!_connectorShares.TryGetValue(name, out var meta)) return WriteAnyOrg(c, image, length);
        var st = _physical.For(meta.Host);
        if (!meta.Multiple) PhysicalFileTable.ReleaseAllForConnector(st, name);   // GR10 / §12.4.5.9 GR6
        bool wantLock = phrase == FileRecordLock.WithLock && LocksEffective(meta, st, name);   // GR11
        if (wantLock)
        {
            string pf = _physical.PreflightNewLock(st, name);   // §12.4.5.9 GR7 — the statement fails BEFORE the write (§14.9.51 GR15)
            if (pf != FileStatusCode.Success) { c.SetStatus(pf); return pf; }
        }
        string status = WriteAnyOrg(c, image, length);
        if (wantLock && status.Length > 0 && status[0] == '0' && c.LastWrittenRecordId is { Length: > 0 } recId)
            _physical.LockRecord(st, name, recId);   // GR11 — the just-released record's lock is set
        return status;
    }

    /// <summary>Governed REWRITE for a sharing-active connector, any organization (§14.9.35 GR11/GR12 + §14.7.9):
    /// the pre-operation conflict check against the record identified for rewriting (locked by another connector
    /// → RETRY re-checks, else 51 with the record NOT rewritten, the record area unaffected and the FPI unchanged
    /// — GR11a-c/GR14), then the GR12 lock actions. Returns the I-O status.</summary>
    public string RewriteShared(string name, string image, int length, FileRecordLock phrase,
        FileRetryKind retryKind, int retryAmount)
    {
        if (!_files.TryGetValue(name, out var c)) return FileStatusCode.PermanentError;
        if (!_connectorShares.TryGetValue(name, out var meta)) return RewriteAnyOrg(c, image, length);
        var st = _physical.For(meta.Host);
        string target = c.MutationTargetRecordId(image);
        if (!meta.Multiple) PhysicalFileTable.ReleaseAllExcept(st, name, target);   // GR12a2 — released at the beginning
        if (target.Length > 0)
        {
            string conflict = RetryLoop(
                () => PhysicalFileTable.IsLockedByOther(st, name, target) ? FileStatusCode.RecordLocked : FileStatusCode.Success,
                retryKind, retryAmount);
            if (conflict != FileStatusCode.Success) { c.SetStatus(conflict); return conflict; }   // GR11
            // (post-conflict-check any surviving lock on the target is this connector's own — re-lock is
            // idempotent per §9.1.16 GR8 self-access, so pre-flight only a genuinely NEW lock)
            if (phrase == FileRecordLock.WithLock && LocksEffective(meta, st, name)
                && !st.RecordLocks.ContainsKey(target))
            {
                string pf = _physical.PreflightNewLock(st, name);   // §12.4.5.9 GR7 pre-flight (§14.9.35 GR14 — no update on failure)
                if (pf != FileStatusCode.Success) { c.SetStatus(pf); return pf; }
            }
        }
        string status = RewriteAnyOrg(c, image, length);
        if (status.Length > 0 && status[0] == '0' && target.Length > 0)
        {
            if (phrase == FileRecordLock.WithLock && LocksEffective(meta, st, name))
                _physical.LockRecord(st, name, target);                      // GR12c — set at completion
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
    public string DeleteShared(string name, string keyedRecordImage, FileRetryKind retryKind, int retryAmount)
    {
        if (!_files.TryGetValue(name, out var c)) return FileStatusCode.PermanentError;
        if (!_connectorShares.TryGetValue(name, out var meta)) return DeleteRecord(name, keyedRecordImage);
        var st = _physical.For(meta.Host);
        string target = c.MutationTargetRecordId(keyedRecordImage);
        if (!meta.Multiple) PhysicalFileTable.ReleaseAllExcept(st, name, target);   // GR7a2 — released at the beginning
        if (target.Length > 0)
        {
            string conflict = RetryLoop(
                () => PhysicalFileTable.IsLockedByOther(st, name, target) ? FileStatusCode.RecordLocked : FileStatusCode.Success,
                retryKind, retryAmount);
            if (conflict != FileStatusCode.Success) { c.SetStatus(conflict); return conflict; }   // GR6
        }
        string status = DeleteRecord(name, keyedRecordImage);
        if (status.Length > 0 && status[0] == '0' && target.Length > 0)
            PhysicalFileTable.ReleaseSingle(st, name, target);   // GR7a1/GR7b — the deleted record's lock releases at completion
        return status;
    }

    /// <summary>The plain WRITE body over any organization (one polymorphic dispatch — the governed entry's
    /// operation half; the sequential arm is the same connector call the ungoverned <see cref="Write"/> makes).</summary>
    private static string WriteAnyOrg(FileConnector c, string image, int length) => c switch
    {
        SequentialConnector f => f.Write(image, length),
        RelativeConnector r => r.Write(image, length),
        IndexedConnector ix => ix.Write(image, length),
        _ => FileStatusCode.PermanentError,
    };

    /// <summary>The plain REWRITE body over any organization (the governed entry's operation half).</summary>
    private static string RewriteAnyOrg(FileConnector c, string image, int length) => c switch
    {
        SequentialConnector f => f.Rewrite(image, length),
        RelativeConnector r => r.Rewrite(image, length),
        IndexedConnector ix => ix.Rewrite(image, length),
        _ => FileStatusCode.PermanentError,
    };

    /// <summary>UNLOCK file [RECORD[S]] (§14.9.47 GR1): release every record lock this connector holds and set
    /// status 00; UNLOCK of a file not open is status 42.</summary>
    public void Unlock(string name, bool records)
    {
        _ = records;
        if (!_files.TryGetValue(name, out var c)) return;
        if (!c.IsOpen) { c.SetStatus(FileStatusCode.FileNotOpen); return; }
        if (_physical.TryGet(c.HostPath, out var st)) PhysicalFileTable.ReleaseAllForConnector(st, name);
        c.SetStatus(FileStatusCode.Success);
    }

    // ── The record-lock primitives (also the CobolFileLockTests surface) ─────────────────────────────────────

    /// <summary>Acquire a lock on <paramref name="recId"/> for connector <paramref name="name"/> (§12.4.5.9 GR7
    /// ceilings enforced). Returns 00 on grant.</summary>
    public string LockRecord(string name, string recId) =>
        _physical.LockRecord(_physical.For(HostPathOf(name)), name, recId);

    /// <summary>True when <paramref name="recId"/> is locked by a connector OTHER than <paramref name="name"/>.</summary>
    public bool IsLockedByOther(string name, string recId) =>
        PhysicalFileTable.IsLockedByOther(_physical.For(HostPathOf(name)), name, recId);

    /// <summary>Release every record lock held by <paramref name="name"/> on its physical file (UNLOCK, CLOSE).</summary>
    public void ReleaseAllForConnector(string name)
    {
        if (_physical.TryGet(HostPathOf(name), out var st)) PhysicalFileTable.ReleaseAllForConnector(st, name);
    }

    /// <summary>Release a single record lock a connector holds (the LOCK MODE single-lock discipline, GR6).</summary>
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
    /// timeout period to the implementor's maximum meaningful value, which COBOL.NET defines as ZERO (A.1 item
    /// 166, docs/CONFORMANCE.md §7), so its period is zero-length and it likewise makes none. GR3: FOREVER waits
    /// until the operation completes. Never sleeps — the ground for the GR2 determination is that a lock here is
    /// held only by a file connector of the EXECUTING run unit, which cannot release it while this statement
    /// runs, so no positive timeout could change the outcome.</para>
    /// <para>Every landing goes through <see cref="ExhaustionStatus"/> — the status is a function of the
    /// conflict's own class, NEVER a literal at a call site.</para></summary>
    public static string RetryLoop(Func<string> attempt, FileRetryKind kind, int amount)
    {
        string s = attempt();
        if (!IsConflict(s)) return s;   // GR4 — success, or an unsuccessful status that is not a conflict
        if (kind == FileRetryKind.Times)
            // GR1 — n further attempts after the initial failure; a zero or negative n makes none (GR4a).
            for (int i = 0; i < amount && IsConflict(s); i++) s = attempt();
        else if (kind == FileRetryKind.Forever)
            // GR3 — "until the input-output operation has been completed". A conflict here is held by a
            // connector of this run unit, which cannot release while this statement executes, so one attempt
            // settles it; the wait that can never complete is the deadlock ExhaustionStatus names.
            s = attempt();
        // The two arms with no `else` are deliberate, not forgotten: FileRetryKind.None makes no further
        // attempt by GR4a, and FileRetryKind.Seconds makes none because GR2 clamps its period to this
        // implementation's maximum meaningful value of ZERO (A.1 item 166) — a zero-length timeout period
        // during which no retry can be attempted. Seconds still RECEIVES its amount because that value is
        // GR4a's screen input, even though the clamp then makes it inert.
        return IsConflict(s) ? ExhaustionStatus(s, kind) : s;
    }

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
    /// the implementor defines (A.1 item 109, recorded in docs/CONFORMANCE.md §7): COBOL.NET detects a deadlock
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

    private string HostPathOf(string name) => _files.TryGetValue(name, out var c) ? c.HostPath : name;

    private void SetStatusOf(string name, string status)
    {
        if (_files.TryGetValue(name, out var c)) c.SetStatus(status);
    }

    /// <summary>Deregister a connector's open entry on CLOSE and release its record locks (the sharing-registry
    /// side of CLOSE; no-op for a non-sharing-active connector).</summary>
    private void SharedClose(string name)
    {
        if (!_connectorShares.TryGetValue(name, out var meta)) return;
        if (_physical.TryGet(meta.Host, out var st))
        {
            st.Open.Remove(name);
            PhysicalFileTable.ReleaseAllForConnector(st, name);
        }
    }
}
