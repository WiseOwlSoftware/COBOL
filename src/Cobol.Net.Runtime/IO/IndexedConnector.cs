// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// A typed-native INDEXED-organization file connector (ISO/IEC 1989:2023 §9.1.7.4): records identified by a unique
/// PRIME record key plus optional ALTERNATE record keys (each a byte range of the record's character image,
/// §12.4.5.12/§12.4.5.6). Ports the legacy <c>IndexedFileHandler</c>'s NIST-IX-proven design: the record list is the
/// SOLE source of truth (alternate orderings are derived on demand — a cloned index goes stale), each record carries
/// an ARRIVAL sequence number realizing the duplicate-alternate retrieval order (§14.9.30 GR26/GR32, §14.9.51 GR40,
/// §14.9.35 GR24b), and the position is the (key-of-reference value, arrival) pair re-derived per READ — robust to
/// interleaved REWRITE/DELETE (the FPI survives deletion of the current record, §14.9.10 GR9).
/// Key comparisons are ordinal over the Latin-1 character image: record keys are category alphanumeric/national
/// (§12.4.5.12 SR2), for which ordinal IS the native collating sequence — correct for COBOL-85 NIST; the file-level
/// COLLATING SEQUENCE clause (§12.4.5.7, WRITE GR35/GR42) is the SPECIAL-NAMES/alphabet subsystem's seam.
/// </summary>
public sealed class IndexedConnector : FileConnector
{
    /// <summary>One stored record: its fixed-width character image and its arrival sequence (write order).</summary>
    private sealed class KeyedRec
    {
        public string Image = "";
        public long Arrival;
    }

    private readonly KeyedAccess _access;
    private readonly int _primeOff, _primeLen;
    private readonly CobolCollation? _primeCollation;   // §12.4.5.7 prime-key collating sequence (the CobolCollation carrier); null = native ordinal
    private readonly List<(int Off, int Len, bool Dups, CobolCollation? Collation, string? Suppress)> _alts = [];
    private readonly List<KeyedRec> _recs = [];   // arrival order — the persisted order
    private long _nextArrival = 1;

    private int _refKey = -1;            // key of reference: -1 prime, i = i-th alternate (§14.9.30 GR30/GR31)
    private string _fpiKey = "";         // file position indicator: (key-of-reference value, arrival) (§9.1.11)
    private long _fpiArrival;
    private bool _fpiValid;
    private bool _inclusive;             // FPI set by OPEN/START — positioned record itself is next (§14.9.30 GR21d)
    private char _positioner = 'O';      // 'O' OPEN / 'S' START / 'R' READ
    private string? _lastWrittenPrime;   // sequential-access ascending check (§14.9.51 GR38; EXTEND seeds highest)
    private string? _lastReadPrime;      // sequential-access REWRITE/DELETE target (§14.9.35 GR22 / §14.9.10 GR2)

    /// <summary>The prime record key of the most recently read record — the record-lock identity for §9.1.16
    /// record locking (Phase 4d M2-FILE-1). Null before the first successful READ.</summary>
    public string? LastReadPrime => _lastReadPrime;
    private string? _lastWrittenPrimeId;   // the record released by the last successful Write (§14.9.51 GR11)
    private bool _open;

    // ── Record-lock identity (ISO §9.1.16) — an indexed record's identity is its PRIME record key ────────────

    /// <inheritdoc/>
    public override string LastReadRecordId => _lastReadPrime ?? "";

    /// <inheritdoc/>  (sequential access targets the last-read record, §14.9.35 GR22 / §14.9.10 GR2;
    /// random/dynamic the record whose prime key is the record area's key slice, §14.9.35 GR23 / §14.9.10 GR3)
    public override string MutationTargetRecordId(string recordImage) => _access == KeyedAccess.Sequential
        ? LastReadRecordId
        : KeyOf(Fit(recordImage), -1);

    /// <inheritdoc/>
    public override string LastWrittenRecordId => _lastWrittenPrimeId ?? "";

    /// <summary>True between a successful OPEN and the matching CLOSE.</summary>
    public override bool IsOpen => _open;

    // RECORD IS VARYING: the RecordFraming store carries each record's exact length; key slices pad on demand
    // (KeyOf), so a varying record persists at its written length (§13.18.43 GR13) and reports it on READ
    // (GR15). Out-of-bounds WRITE/REWRITE is the GR14/§14.9.35 GR20 '44'.

    public IndexedConnector(string hostPath, int recordWidth, KeyedAccess access, int primeOffset, int primeLength,
        int varyMin = -1, int varyMax = -1, CobolCollation? primeCollation = null)
        : base(hostPath, recordWidth, varyMin, varyMax)
    {
        _access = access;
        _primeOff = primeOffset;
        _primeLen = primeLength;
        _primeCollation = primeCollation;
    }

    /// <summary>Register one ALTERNATE RECORD KEY's (offset, length, WITH DUPLICATES) geometry (§12.4.5.6), with
    /// its optional §12.4.5.7 collating-weight table (null = native ordinal) and §12.4.5.6.4 GR6 SUPPRESS WHEN
    /// value (null = no suppression).</summary>
    public void AddAlternateKey(int offset, int length, bool duplicates, CobolCollation? collation = null, string? suppress = null) =>
        _alts.Add((offset, length, duplicates, collation, suppress));

    /// <summary>True when this record's <paramref name="keyIndex"/> alternate key equals that key's SUPPRESS WHEN
    /// value (ISO §12.4.5.6.4 GR6): the alternate access path to the record is NOT provided under this key, and
    /// the record "is not considered to exist" for READ/START (the GR6 NOTE). The comparison is the §14.9.51 GR35
    /// relation condition — under this key's collating sequence (null weights = ordinal), the shorter operand
    /// space-extended (<see cref="KeyEq"/>). The prime key (keyIndex &lt; 0) is NEVER suppressible (GR6 scopes
    /// suppression to alternate keys).</summary>
    private bool IsSuppressed(string image, int keyIndex)
    {
        if (keyIndex < 0 || _alts[keyIndex].Suppress is not { } lit) return false;
        return KeyEq(KeyOf(image, keyIndex), lit, keyIndex);
    }

    /// <summary>Compare two full key values under the key of reference's collating sequence (ISO §12.4.5.7.4 /
    /// §14.9.41 GR17e / §12.4.5.12.4 GR1). <paramref name="keyIndex"/> &lt; 0 selects the prime key. With no
    /// COLLATING SEQUENCE clause the key's weights are null and the comparison is native ordinal — byte-identical
    /// to the pre-§12.4.5.7 engine (the NIST-IX baseline), since keys are fixed-length so no space-extension
    /// differs. A declared alphabet routes through the §8.8.4.2.7 weighted relation-condition compare.</summary>
    private int KeyCompare(string a, string b, int keyIndex)
    {
        var c = keyIndex < 0 ? _primeCollation : _alts[keyIndex].Collation;
        return c is null ? string.CompareOrdinal(a, b) : c.Compare(a, b);
    }

    /// <summary>Key equality under the key of reference's collating sequence: two keys that differ in bytes but
    /// share collating weights are EQUAL (ISO §12.4.5.12.4 GR1 — "based on the collating sequence … according to
    /// the rules for a relation condition"). Drives uniqueness, random match, target lookup, and the '02'
    /// look-ahead.</summary>
    private bool KeyEq(string a, string b, int keyIndex) => KeyCompare(a, b, keyIndex) == 0;

    // ── OPEN / CLOSE (ISO §14.9.27 / §14.9.6) ────────────────────────────────────────────────────────────────

    /// <summary>The indexed OPEN body (§14.9.27). GR14: INPUT/I-O set the FPI to the lowest collating position
    /// and the PRIME key becomes the key of reference; GR15: EXTEND positions after the highest prime key
    /// (seeding the GR38 ascending-sequence check). An absent NON-optional file on INPUT/I-O/EXTEND is '35'
    /// (§9.1.13.6 item 5) — pinned to the spec; the legacy created a missing file on I-O with '00' (brief §2.3
    /// #3, version-invariant). The '41' guard, mode bookkeeping, position reset, and '37'/'30' exception mapping
    /// live on <see cref="FileConnector.Open"/>.</summary>
    protected override string OpenCore(FileOpenMode mode)
    {
        _recs.Clear();
        _nextArrival = 1;
        _refKey = -1;                                                       // §14.9.27 GR14 — prime key of reference
        _fpiKey = ""; _fpiArrival = 0; _inclusive = true; _positioner = 'O';
        _lastWrittenPrime = null;
        _lastReadPrime = null;
        _lastWrittenPrimeId = null;
        bool exists = File.Exists(HostPath);
        string status = FileStatusCode.Success;
        switch (mode)
        {
            case FileOpenMode.Input:
                if (!exists)
                {
                    if (!IsOptional) return FileStatusCode.FileNotFound;
                    OptionalAbsent = true;
                    status = FileStatusCode.OptionalFileNotFound;
                    break;
                }
                Load();
                break;
            case FileOpenMode.Output:
                RecordFraming.WriteStore(HostPath, []);
                break;
            case FileOpenMode.IO:
                if (!exists)
                {
                    if (!IsOptional) return FileStatusCode.FileNotFound;   // '35' — spec-pinned
                    RecordFraming.WriteStore(HostPath, []);                // §14.9.27 GR17
                    status = FileStatusCode.OptionalFileNotFound;
                    break;
                }
                Load();
                break;
            case FileOpenMode.Extend:
                if (!exists)
                {
                    if (!IsOptional) return FileStatusCode.FileNotFound;
                    RecordFraming.WriteStore(HostPath, []);
                    status = FileStatusCode.OptionalFileNotFound;
                }
                else Load();
                if (_recs.Count > 0)
                {
                    var ordered = Ordered(-1);
                    _lastWrittenPrime = KeyOf(ordered[^1].Image, -1);   // §14.9.51 GR38 — highest existing
                }
                break;
        }
        _open = true;
        _fpiValid = mode is FileOpenMode.Input or FileOpenMode.IO;
        return status;
    }

    /// <summary>The indexed CLOSE body (§14.9.6): a writable mode persists the store in ARRIVAL order — so the
    /// duplicate-alternate retrieval order (§14.9.30 GR26) survives a CLOSE/OPEN cycle and run-unit termination.
    /// The not-open '42' guard lives on <see cref="FileConnector.Close"/>.</summary>
    protected override string CloseCore()
    {
        try
        {
            if (!OptionalAbsent && Mode is not FileOpenMode.Input)
                RecordFraming.WriteStore(HostPath, _recs.OrderBy(r => r.Arrival).Select(r => (string?)r.Image).ToList());
        }
        catch (IOException) { _open = false; return FileStatusCode.PermanentError; }
        _open = false;
        OptionalAbsent = false;
        ModeKnown = false;   // 9.1.4 - after a successful CLOSE the file is in no open mode
        return FileStatusCode.Success;
    }

    // ── READ (ISO §14.9.30) ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>Sequential <c>READ [NEXT]</c> in (key-of-reference, arrival) order (§14.9.30 GR21 indexed rules
    /// d–g; GR26 — duplicates in WRITE-release order). Sets '02' when the key of reference is an alternate and
    /// the FOLLOWING record duplicates the key just read (GR27a).</summary>
    public string ReadNext(out string image) => ReadSequential(out image, previous: false);

    /// <summary>Sequential <c>READ PREVIOUS</c> (COBOL-2002+; compiler-gated) — the (key, arrival) walk in reverse
    /// (§14.9.30 GR21 d–g apply symmetrically — the legacy ignored key-of-reference/arrival on PREVIOUS, brief
    /// §2.3 #5; this connector orders both directions identically). Immediately after OPEN → at end (the ISO-2023
    /// behavior, VERSION_CHANGE_REFERENCE row 29).</summary>
    public string ReadPrevious(out string image) => ReadSequential(out image, previous: true);

    private string ReadSequential(out string image, bool previous)
    {
        image = new string(' ', RecordWidth);
        PrevOpWasSuccessfulRead = false;
        if (!IsOpen || Mode is FileOpenMode.Output or FileOpenMode.Extend)
            return Status = FileStatusCode.ReadNotOpenForInput;            // '47' §9.1.13.7 item 7
        if (OptionalAbsent) { LastReadUnsuccessful = true; return Status = FileStatusCode.AtEnd; }
        if (LastReadUnsuccessful) return Status = FileStatusCode.NoValidNextRecord;   // '46' GR21
        if (!_fpiValid) return Status = FileStatusCode.NoValidNextRecord;

        var seq = Ordered(_refKey);
        KeyedRec? found = null;
        int foundIdx = -1;
        if (previous && _positioner == 'O')
            found = null;   // READ PREVIOUS immediately after OPEN → at end (row 29, 2023)
        else if (!previous && _positioner == 'O')
        {
            // §14.9.27 GR14 — OPEN INPUT/I-O positions the FPI at the LOWEST record in the key-of-reference's
            // collating sequence, so the first READ NEXT yields it. Read it off the ordered sequence directly
            // rather than comparing against the empty-string OPEN sentinel: under a §12.4.5.7 alphabet where the
            // pad SPACE weighs high, that sentinel is NOT the lowest value, so a PositionCompare walk would find
            // nothing (the collating-sequence AT-END bug). Ordinal order is unaffected — seq[0] is still lowest.
            if (seq.Count > 0) { found = seq[0]; foundIdx = 0; }
        }
        else if (!previous)
        {
            for (int i = 0; i < seq.Count; i++)
            {
                int c = PositionCompare(seq[i]);
                if (c > 0 || (_inclusive && c == 0)) { found = seq[i]; foundIdx = i; break; }
            }
        }
        else
        {
            for (int i = seq.Count - 1; i >= 0; i--)
            {
                int c = PositionCompare(seq[i]);
                if (c < 0 || (_inclusive && c == 0)) { found = seq[i]; foundIdx = i; break; }
            }
        }
        if (found is null)
        {
            LastReadUnsuccessful = true;
            _fpiValid = false;                                             // §14.9.30 GR24b
            return Status = FileStatusCode.AtEnd;
        }
        // §14.9.30 GR27 — '02' look-ahead: alternate key of reference and the adjacent record duplicates it.
        string status = FileStatusCode.Success;
        if (_refKey >= 0)
        {
            KeyedRec? adjacent = previous
                ? (foundIdx > 0 ? seq[foundIdx - 1] : null)
                : (foundIdx + 1 < seq.Count ? seq[foundIdx + 1] : null);
            if (adjacent is not null && KeyEq(KeyOf(adjacent.Image, _refKey), KeyOf(found.Image, _refKey), _refKey))
                status = FileStatusCode.DuplicateAlternateKey;
        }
        _fpiKey = KeyOf(found.Image, _refKey);                             // GR21 rule g
        _fpiArrival = found.Arrival;
        _fpiValid = true; _inclusive = false; _positioner = 'R';
        _lastReadPrime = KeyOf(found.Image, -1);
        PrevOpWasSuccessfulRead = true;
        LastReadUnsuccessful = false;
        LastReadLength = found.Image.Length;   // §13.18.43 GR15 — the stored frame length
        image = Fit(found.Image);
        return Status = status;

        int PositionCompare(KeyedRec rec)
        {
            int c = KeyCompare(KeyOf(rec.Image, _refKey), _fpiKey, _refKey);
            return c != 0 ? c : rec.Arrival.CompareTo(_fpiArrival);
        }
    }

    /// <summary>Format-2 random READ (§14.9.30 GR30–GR32): <paramref name="keyIndex"/> becomes the key of
    /// reference (persisting for subsequent dynamic sequential reads, GR30/GR31); the key VALUE is sliced from
    /// the record area image (<paramref name="keyedRecordImage"/> — GR32, the program stored it there). Among
    /// duplicate alternates the FIRST RELEASED record is made available (GR32 — smallest arrival); no record →
    /// invalid key '23'; an absent optional file → '23' (GR28).</summary>
    public string ReadRandom(int keyIndex, string keyedRecordImage, out string image)
    {
        image = new string(' ', RecordWidth);
        PrevOpWasSuccessfulRead = false;
        if (!IsOpen || Mode is FileOpenMode.Output or FileOpenMode.Extend)
            return Status = FileStatusCode.ReadNotOpenForInput;
        _refKey = keyIndex;                                                // GR30/GR31
        if (OptionalAbsent) { LastReadUnsuccessful = true; return Status = FileStatusCode.RecordNotFound; }
        string value = KeyOf(Fit(keyedRecordImage), keyIndex);
        KeyedRec? found = null;
        foreach (var rec in _recs)
            if (!IsSuppressed(rec.Image, keyIndex) && KeyEq(KeyOf(rec.Image, keyIndex), value, keyIndex)   // §14.9.30 GR32 + §12.4.5.6.4 GR6
                && (found is null || rec.Arrival < found.Arrival))
                found = rec;
        if (found is null)
        {
            LastReadUnsuccessful = true;
            return Status = FileStatusCode.RecordNotFound;                 // '23' GR32
        }
        _fpiKey = value; _fpiArrival = found.Arrival; _fpiValid = true; _inclusive = false; _positioner = 'R';
        _lastReadPrime = KeyOf(found.Image, -1);
        PrevOpWasSuccessfulRead = true;
        LastReadUnsuccessful = false;
        LastReadLength = found.Image.Length;   // §13.18.43 GR15 — the stored frame length
        image = Fit(found.Image);
        return Status = FileStatusCode.Success;
    }

    // ── WRITE / REWRITE / DELETE (ISO §14.9.51 / §14.9.35 / §14.9.10) ───────────────────────────────────────

    /// <summary>WRITE (§14.9.51 GR34–GR42): sequential access requires strictly ascending prime keys ('21',
    /// GR38/GR42a); a duplicate prime key → '22' (GR36/GR42b); a duplicate no-DUPLICATES alternate → '22'
    /// (GR40/GR42c); a permitted duplicate alternate succeeds with '02' (§9.1.13.2 2c) and takes the next arrival
    /// number — sequential retrieval order is the actual write order (GR40). Open-mode legality per §9.1.13.7
    /// item 8.</summary>
    public string Write(string image, int length = -1)
    {
        PrevOpWasSuccessfulRead = false;
        if (Stored(image, length) is not { } stored)
            return Status = FileStatusCode.RecordSizeViolation;            // '44' §13.18.43 GR14a
        image = Fit(image);   // key slices come from the record-area image (KeyOf pads on demand)
        bool sequential = _access == KeyedAccess.Sequential || Mode == FileOpenMode.Extend;
        if (sequential)
        {
            if (!IsOpen || Mode is not (FileOpenMode.Output or FileOpenMode.Extend))
                return Status = FileStatusCode.WriteNotOpenForOutput;      // '48' 8a
        }
        else if (!IsOpen || Mode is not (FileOpenMode.IO or FileOpenMode.Output))
            return Status = FileStatusCode.WriteNotOpenForOutput;          // '48' 8b
        string prime = KeyOf(image, -1);
        if (sequential && _lastWrittenPrime is { } lastPrime && KeyCompare(prime, lastPrime, -1) <= 0)
            return Status = FileStatusCode.SequenceError;                  // '21' GR38/GR42a
        if (_recs.Any(r => KeyEq(KeyOf(r.Image, -1), prime, -1)))
            return Status = FileStatusCode.DuplicateKey;                   // '22' GR36/GR42b
        bool duplicateAlt = false;
        for (int i = 0; i < _alts.Count; i++)
        {
            if (IsSuppressed(image, i)) continue;   // §14.9.51 GR41 — a suppressed alternate key provides no access path and never duplicates
            string value = KeyOf(image, i);
            bool exists = _recs.Any(r => !IsSuppressed(r.Image, i) && KeyEq(KeyOf(r.Image, i), value, i));
            if (exists && !_alts[i].Dups) return Status = FileStatusCode.DuplicateKey;   // '22' GR40/GR42c
            if (exists) duplicateAlt = true;
        }
        _recs.Add(new KeyedRec { Image = stored, Arrival = _nextArrival++ });
        if (sequential) _lastWrittenPrime = prime;
        _lastWrittenPrimeId = prime;   // §9.1.16 lock identity of the record just released (§14.9.51 GR11)
        return Status = duplicateAlt ? FileStatusCode.DuplicateAlternateKey : FileStatusCode.Success;
    }

    /// <summary>REWRITE (§14.9.35): open mode I-O ('49'); sequential access requires the previous op to be a
    /// successful READ ('43', GR5) AND the prime key to equal the last-read prime ('21', GR22/GR25a);
    /// random/dynamic an existing prime ('23', GR23/GR25b); a no-DUPLICATES alternate conflict with ANOTHER
    /// record → '22' (GR25c); a CHANGED alternate key repositions the record LAST in its duplicate set (GR24b —
    /// it takes the next arrival number); a permitted duplicate alternate created → '02' (§9.1.13.2 2c).</summary>
    public string Rewrite(string image, int length = -1)
    {
        bool wasRead = PrevOpWasSuccessfulRead;
        PrevOpWasSuccessfulRead = false;
        if (!IsOpen || Mode != FileOpenMode.IO) return Status = FileStatusCode.DeleteRewriteNotOpenForIO;
        // §14.9.35 GR18 — an indexed record's size MAY differ from the replaced record's; GR20 still bounds it.
        if (Stored(image, length) is not { } stored)
            return Status = FileStatusCode.RecordSizeViolation;                                 // '44' GR20
        image = Fit(image);
        string prime = KeyOf(image, -1);
        if (_access == KeyedAccess.Sequential)
        {
            if (!wasRead) return Status = FileStatusCode.NoSuccessfulReadBeforeDeleteRewrite;   // '43' GR5
            // '21' §14.9.35 GR22 — the prime key of the replaced record must EQUAL that of the last record read;
            // equality is collating-sequence-based per §12.4.5.12.4 GR1 (KeyEq honors _primeCollation), not ordinal.
            if (_lastReadPrime is not { } lastPrime || !KeyEq(prime, lastPrime, -1))
                return Status = FileStatusCode.SequenceError;
        }
        KeyedRec? target = _recs.FirstOrDefault(r => KeyEq(KeyOf(r.Image, -1), prime, -1));
        if (target is null) return Status = FileStatusCode.RecordNotFound;                      // '23' GR23
        bool duplicateAlt = false, altChanged = false;
        for (int i = 0; i < _alts.Count; i++)
        {
            string newValue = KeyOf(image, i);
            // §14.9.35 GR24 + §12.4.5.6.4 GR6 — a key whose value equals its SUPPRESS WHEN value provides no
            // access path; entering OR leaving suppression re-positions the record (GR24b), but a suppressed key
            // is skipped for the uniqueness check (it can never duplicate, GR41 by parity with WRITE).
            bool nowSup = IsSuppressed(image, i), wasSup = IsSuppressed(target.Image, i);
            if (newValue != KeyOf(target.Image, i) || nowSup != wasSup) altChanged = true;
            if (nowSup) continue;
            bool exists = _recs.Any(r => !ReferenceEquals(r, target) && !IsSuppressed(r.Image, i) && KeyEq(KeyOf(r.Image, i), newValue, i));
            if (exists && !_alts[i].Dups) return Status = FileStatusCode.DuplicateKey;          // '22' GR25c
            if (exists) duplicateAlt = true;
        }
        target.Image = stored;
        if (altChanged) target.Arrival = _nextArrival++;                                         // GR24b
        return Status = duplicateAlt ? FileStatusCode.DuplicateAlternateKey : FileStatusCode.Success;
    }

    /// <summary>DELETE RECORD (§14.9.10): open mode I-O ('49', GR1); sequential access removes the prior READ's
    /// record ('43' without one, GR2); random/dynamic removes the record whose PRIME key equals the prime key
    /// item's content (sliced from the record area image — GR3; absent → invalid key '23'). The FPI is unaffected
    /// (GR9) — the (key, arrival) position survives because the next READ re-derives it.</summary>
    public string Delete(string keyedRecordImage)
    {
        bool wasRead = PrevOpWasSuccessfulRead;
        PrevOpWasSuccessfulRead = false;
        if (!IsOpen || Mode != FileOpenMode.IO) return Status = FileStatusCode.DeleteRewriteNotOpenForIO;
        string prime;
        if (_access == KeyedAccess.Sequential)
        {
            if (!wasRead) return Status = FileStatusCode.NoSuccessfulReadBeforeDeleteRewrite;
            prime = _lastReadPrime ?? "";
        }
        else
            prime = KeyOf(Fit(keyedRecordImage), -1);
        KeyedRec? target = _recs.FirstOrDefault(r => KeyEq(KeyOf(r.Image, -1), prime, -1));
        if (target is null) return Status = FileStatusCode.RecordNotFound;
        _recs.Remove(target);
        return Status = FileStatusCode.Success;
    }

    // ── START (ISO §14.9.41) ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>START KEY rel-op (GR16/GR17): the comparison takes the leftmost <paramref name="compareLength"/>
    /// characters of each stored key (the GR17 temporary-area partial-key model — a generic/shorter operand or
    /// the 2002+ WITH LENGTH count). An out-of-range length → '23' (GR14). Forward search (=, &gt;, &gt;=) takes
    /// the FIRST record in (key, arrival) order satisfying the comparison; reverse (&lt;, &lt;=) the LAST — the
    /// indexed analogue of the relative GR9b reverse search. Success establishes <paramref name="keyIndex"/> as
    /// the key of reference for subsequent sequential READs (GR16); failure invalidates the FPI and leaves the
    /// key of reference undefined (GR7).</summary>
    public string Start(int keyIndex, string op, string operand, int compareLength)
    {
        PrevOpWasSuccessfulRead = false;
        if (!IsOpen || Mode is FileOpenMode.Output or FileOpenMode.Extend)
            return Status = FileStatusCode.ReadNotOpenForInput;            // '47' §14.9.41 GR1
        if (OptionalAbsent) return StartFail();                           // '23' GR5
        int keyLength = keyIndex < 0 ? _primeLen : _alts[keyIndex].Len;
        if (compareLength < 1 || compareLength > keyLength) return StartFail();   // '23' GR14
        string value = operand.Length >= compareLength
            ? operand[..compareLength] : operand.PadRight(compareLength, ' ');
        var seq = Ordered(keyIndex);
        KeyedRec? found = null;
        bool forward = op is "==" or ">" or ">=";
        for (int i = 0; i < seq.Count; i++)
        {
            var rec = seq[forward ? i : seq.Count - 1 - i];
            string part = KeyOf(rec.Image, keyIndex)[..compareLength];
            int c = KeyCompare(part, value, keyIndex);   // §14.9.41 GR17e — the file's collating sequence
            bool satisfied = op switch
            {
                "==" => c == 0,
                ">" => c > 0,
                ">=" => c >= 0,
                "<" => c < 0,
                "<=" => c <= 0,
                _ => false,
            };
            if (satisfied) { found = rec; break; }
        }
        if (found is null) return StartFail();                             // '23' — comparison not satisfied
        _refKey = keyIndex;                                                // GR16
        _fpiKey = KeyOf(found.Image, keyIndex);
        _fpiArrival = found.Arrival;
        _fpiValid = true; _inclusive = true; _positioner = 'S';
        LastReadUnsuccessful = false;
        return Status = FileStatusCode.Success;
    }

    /// <summary>START FIRST/LAST (COBOL-2002+): position at the first/last record under the CURRENT key of
    /// reference (the prime key after OPEN, §14.9.27 GR14); an empty or absent-optional file → invalid key.</summary>
    public string StartFirstLast(bool last)
    {
        PrevOpWasSuccessfulRead = false;
        if (!IsOpen || Mode is FileOpenMode.Output or FileOpenMode.Extend)
            return Status = FileStatusCode.ReadNotOpenForInput;
        if (OptionalAbsent || _recs.Count == 0) return StartFail();
        var seq = Ordered(_refKey);
        var rec = last ? seq[^1] : seq[0];
        _fpiKey = KeyOf(rec.Image, _refKey);
        _fpiArrival = rec.Arrival;
        _fpiValid = true; _inclusive = true; _positioner = 'S';
        LastReadUnsuccessful = false;
        return Status = FileStatusCode.Success;
    }

    private string StartFail()
    {
        _fpiValid = false;                  // §14.9.41 GR7 — no valid position; key of reference undefined
        LastReadUnsuccessful = true;       // → '46' on the next sequential READ (§9.1.13.7 6a)
        return Status = FileStatusCode.RecordNotFound;
    }

    // ── Internals ────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The records ordered by (key-of-reference value, arrival) under the key of reference's collating
    /// sequence (ISO §12.4.5.5.3 GR2c) — derived per call from the one record list (the legacy's lesson: a
    /// maintained clone index goes stale across REWRITE/DELETE). With no §12.4.5.7 COLLATING clause the key's
    /// weights are null and the ordering is native ordinal, byte-identical to the pre-clause engine.</summary>
    private List<KeyedRec> Ordered(int keyIndex) =>
        [.. _recs.Where(r => !IsSuppressed(r.Image, keyIndex))   // §12.4.5.6.4 GR6 — suppressed records are not provided under this key
                 .OrderBy(r => KeyOf(r.Image, keyIndex),
                Comparer<string>.Create((x, y) => KeyCompare(x, y, keyIndex))).ThenBy(r => r.Arrival)];

    /// <summary>The key value at <paramref name="keyIndex"/> (−1 = prime) — a fixed (offset, length) slice of the
    /// record's character image (§12.4.5.12 GR2 — the key IS its position range in the record).</summary>
    private string KeyOf(string image, int keyIndex)
    {
        var (off, len) = keyIndex < 0 ? (_primeOff, _primeLen) : (_alts[keyIndex].Off, _alts[keyIndex].Len);
        if (image.Length < off + len) image = image.PadRight(off + len, ' ');
        return image.Substring(off, len);
    }

    private void Load()
    {
        _recs.Clear();
        _nextArrival = 1;
        // A varying file's frames keep their exact stored lengths (§13.18.43 GR15 reports them on READ);
        // fixed frames normalize to the record width.
        foreach (string? frame in RecordFraming.ReadStore(HostPath))
            if (frame is not null)
                _recs.Add(new KeyedRec { Image = IsVarying ? frame : Fit(frame), Arrival = _nextArrival++ });
    }
}
