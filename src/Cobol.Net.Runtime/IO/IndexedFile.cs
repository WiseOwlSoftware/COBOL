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
public sealed class IndexedFile
{
    /// <summary>One stored record: its fixed-width character image and its arrival sequence (write order).</summary>
    private sealed class KeyedRec
    {
        public string Image = "";
        public long Arrival;
    }

    private readonly int _recordWidth;
    private readonly KeyedAccess _access;
    private readonly int _primeOff, _primeLen;
    private readonly List<(int Off, int Len, bool Dups)> _alts = [];
    private readonly List<KeyedRec> _recs = [];   // arrival order — the persisted order
    private long _nextArrival = 1;

    private FileOpenMode _mode;
    private bool _optionalAbsent;
    private int _refKey = -1;            // key of reference: -1 prime, i = i-th alternate (§14.9.30 GR30/GR31)
    private string _fpiKey = "";         // file position indicator: (key-of-reference value, arrival) (§9.1.11)
    private long _fpiArrival;
    private bool _fpiValid;
    private bool _inclusive;             // FPI set by OPEN/START — positioned record itself is next (§14.9.30 GR21d)
    private char _positioner = 'O';      // 'O' OPEN / 'S' START / 'R' READ
    private string? _lastWrittenPrime;   // sequential-access ascending check (§14.9.51 GR38; EXTEND seeds highest)
    private string? _lastReadPrime;      // sequential-access REWRITE/DELETE target (§14.9.35 GR22 / §14.9.10 GR2)
    private bool _lastReadUnsuccessful;  // → '46' (§14.9.30 GR21)
    private bool _prevOpWasSuccessfulRead;

    /// <summary>The resolved host path of the physical file.</summary>
    public string HostPath { get; }

    /// <summary>The latest I-O status (ISO §9.1.13).</summary>
    public string Status { get; private set; } = FileStatusCode.Success;

    /// <summary>True for a SELECT OPTIONAL file.</summary>
    public bool IsOptional { get; init; }

    /// <summary>True between a successful OPEN and the matching CLOSE.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>Set the I-O status directly (facade-level conditions).</summary>
    public void SetStatus(string status) => Status = status;

    /// <summary>The open-mode view for USE-declarative mode scoping (ISO 14.9.49.4 GR6b-e): (int) of the mode
    /// while open OR the ATTEMPTED mode of a failed OPEN; -1 after a successful CLOSE / before any OPEN.</summary>
    public int OpenModeView => _modeKnown ? (int)_mode : -1;
    private bool _modeKnown;

    // RECORD IS VARYING bounds (ISO §13.18.43 GR9/GR10); (-1,-1) = fixed-length. The KeyedFrames store carries
    // each record's exact length; key slices pad on demand (KeyOf), so a varying record persists at its written
    // length (GR13) and reports it on READ (GR15). Out-of-bounds WRITE/REWRITE is the GR14/§14.9.35 GR20 '44'.
    private readonly int _varyMin = -1, _varyMax = -1;
    private bool IsVarying => _varyMin >= 0;

    /// <summary>The length of the most recently read record (ISO §13.18.43 GR15).</summary>
    public int LastReadLength { get; private set; }

    public IndexedFile(string hostPath, int recordWidth, KeyedAccess access, int primeOffset, int primeLength,
        int varyMin = -1, int varyMax = -1)
    {
        HostPath = hostPath;
        _recordWidth = recordWidth < 1 ? 1 : recordWidth;
        _access = access;
        _primeOff = primeOffset;
        _primeLen = primeLength;
        _varyMin = varyMin;
        _varyMax = varyMax;
    }

    /// <summary>The stored image of a record being written: a varying record keeps exactly its declared length
    /// (§13.18.43 GR13); a fixed record fills the record width. Null (→ '44') when a varying length violates the
    /// declared bounds (GR14 / §14.9.35 GR20).</summary>
    private string? Stored(string image, int length)
    {
        if (!IsVarying) return Fit(image);
        int len = length >= 0 ? length : image.Length;
        if (len < _varyMin || len > _varyMax) return null;
        return image.Length == len ? image : image.Length > len ? image[..len] : image.PadRight(len, ' ');
    }

    /// <summary>Register one ALTERNATE RECORD KEY's (offset, length, WITH DUPLICATES) geometry (§12.4.5.6).</summary>
    public void AddAlternateKey(int offset, int length, bool duplicates) => _alts.Add((offset, length, duplicates));

    // ── OPEN / CLOSE (ISO §14.9.27 / §14.9.6) ────────────────────────────────────────────────────────────────

    /// <summary>OPEN (§14.9.27). GR14: INPUT/I-O set the FPI to the lowest collating position and the PRIME key
    /// becomes the key of reference; GR15: EXTEND positions after the highest prime key (seeding the GR38
    /// ascending-sequence check). An absent NON-optional file on INPUT/I-O/EXTEND is '35' (§9.1.13.6 item 5) —
    /// pinned to the spec; the legacy created a missing file on I-O with '00' (brief §2.3 #3, version-invariant).</summary>
    public string Open(FileOpenMode mode)
    {
        if (IsOpen) return Status = FileStatusCode.FileAlreadyOpen;        // '41' §9.1.13.7 item 1
        _mode = mode;
        _modeKnown = true;   // a FAILED open still records the attempted mode (GR6b "being opened")
        _optionalAbsent = false;
        _lastReadUnsuccessful = false;
        _prevOpWasSuccessfulRead = false;
        _recs.Clear();
        _nextArrival = 1;
        _refKey = -1;                                                       // §14.9.27 GR14 — prime key of reference
        _fpiKey = ""; _fpiArrival = 0; _inclusive = true; _positioner = 'O';
        _lastWrittenPrime = null;
        _lastReadPrime = null;
        bool exists = File.Exists(HostPath);
        string status = FileStatusCode.Success;
        try
        {
            switch (mode)
            {
                case FileOpenMode.Input:
                    if (!exists)
                    {
                        if (!IsOptional) return Status = FileStatusCode.FileNotFound;
                        _optionalAbsent = true;
                        status = FileStatusCode.OptionalFileNotFound;
                        break;
                    }
                    Load();
                    break;
                case FileOpenMode.Output:
                    KeyedFrames.Write(HostPath, []);
                    break;
                case FileOpenMode.IO:
                    if (!exists)
                    {
                        if (!IsOptional) return Status = FileStatusCode.FileNotFound;   // '35' — spec-pinned
                        KeyedFrames.Write(HostPath, []);                                 // §14.9.27 GR17
                        status = FileStatusCode.OptionalFileNotFound;
                        break;
                    }
                    Load();
                    break;
                case FileOpenMode.Extend:
                    if (!exists)
                    {
                        if (!IsOptional) return Status = FileStatusCode.FileNotFound;
                        KeyedFrames.Write(HostPath, []);
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
        }
        catch (UnauthorizedAccessException) { return Status = FileStatusCode.PermissionDenied; }
        catch (IOException) { return Status = FileStatusCode.PermanentError; }
        IsOpen = true;
        _fpiValid = mode is FileOpenMode.Input or FileOpenMode.IO;
        return Status = status;
    }

    /// <summary>CLOSE (§14.9.6): not open → '42'; a writable mode persists the store in ARRIVAL order — so the
    /// duplicate-alternate retrieval order (§14.9.30 GR26) survives a CLOSE/OPEN cycle and run-unit termination.</summary>
    public string Close()
    {
        if (!IsOpen) return Status = FileStatusCode.FileNotOpen;
        try
        {
            if (!_optionalAbsent && _mode is not FileOpenMode.Input)
                KeyedFrames.Write(HostPath, _recs.OrderBy(r => r.Arrival).Select(r => (string?)r.Image).ToList());
        }
        catch (IOException) { IsOpen = false; return Status = FileStatusCode.PermanentError; }
        IsOpen = false;
        _optionalAbsent = false;
        _modeKnown = false;   // 9.1.4 - after a successful CLOSE the file is in no open mode
        return Status = FileStatusCode.Success;
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
        image = new string(' ', _recordWidth);
        _prevOpWasSuccessfulRead = false;
        if (!IsOpen || _mode is FileOpenMode.Output or FileOpenMode.Extend)
            return Status = FileStatusCode.ReadNotOpenForInput;            // '47' §9.1.13.7 item 7
        if (_optionalAbsent) { _lastReadUnsuccessful = true; return Status = FileStatusCode.AtEnd; }
        if (_lastReadUnsuccessful) return Status = FileStatusCode.NoValidNextRecord;   // '46' GR21
        if (!_fpiValid) return Status = FileStatusCode.NoValidNextRecord;

        var seq = Ordered(_refKey);
        KeyedRec? found = null;
        int foundIdx = -1;
        if (previous && _positioner == 'O')
            found = null;   // READ PREVIOUS immediately after OPEN → at end (row 29, 2023)
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
            _lastReadUnsuccessful = true;
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
            if (adjacent is not null && KeyOf(adjacent.Image, _refKey) == KeyOf(found.Image, _refKey))
                status = FileStatusCode.DuplicateAlternateKey;
        }
        _fpiKey = KeyOf(found.Image, _refKey);                             // GR21 rule g
        _fpiArrival = found.Arrival;
        _fpiValid = true; _inclusive = false; _positioner = 'R';
        _lastReadPrime = KeyOf(found.Image, -1);
        _prevOpWasSuccessfulRead = true;
        _lastReadUnsuccessful = false;
        LastReadLength = found.Image.Length;   // §13.18.43 GR15 — the stored frame length
        image = Fit(found.Image);
        return Status = status;

        int PositionCompare(KeyedRec rec)
        {
            int c = string.CompareOrdinal(KeyOf(rec.Image, _refKey), _fpiKey);
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
        image = new string(' ', _recordWidth);
        _prevOpWasSuccessfulRead = false;
        if (!IsOpen || _mode is FileOpenMode.Output or FileOpenMode.Extend)
            return Status = FileStatusCode.ReadNotOpenForInput;
        _refKey = keyIndex;                                                // GR30/GR31
        if (_optionalAbsent) { _lastReadUnsuccessful = true; return Status = FileStatusCode.RecordNotFound; }
        string value = KeyOf(Fit(keyedRecordImage), keyIndex);
        KeyedRec? found = null;
        foreach (var rec in _recs)
            if (KeyOf(rec.Image, keyIndex) == value && (found is null || rec.Arrival < found.Arrival))
                found = rec;
        if (found is null)
        {
            _lastReadUnsuccessful = true;
            return Status = FileStatusCode.RecordNotFound;                 // '23' GR32
        }
        _fpiKey = value; _fpiArrival = found.Arrival; _fpiValid = true; _inclusive = false; _positioner = 'R';
        _lastReadPrime = KeyOf(found.Image, -1);
        _prevOpWasSuccessfulRead = true;
        _lastReadUnsuccessful = false;
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
        _prevOpWasSuccessfulRead = false;
        if (Stored(image, length) is not { } stored)
            return Status = FileStatusCode.RecordSizeViolation;            // '44' §13.18.43 GR14a
        image = Fit(image);   // key slices come from the record-area image (KeyOf pads on demand)
        bool sequential = _access == KeyedAccess.Sequential || _mode == FileOpenMode.Extend;
        if (sequential)
        {
            if (!IsOpen || _mode is not (FileOpenMode.Output or FileOpenMode.Extend))
                return Status = FileStatusCode.WriteNotOpenForOutput;      // '48' 8a
        }
        else if (!IsOpen || _mode is not (FileOpenMode.IO or FileOpenMode.Output))
            return Status = FileStatusCode.WriteNotOpenForOutput;          // '48' 8b
        string prime = KeyOf(image, -1);
        if (sequential && _lastWrittenPrime is { } lastPrime && string.CompareOrdinal(prime, lastPrime) <= 0)
            return Status = FileStatusCode.SequenceError;                  // '21' GR38/GR42a
        if (_recs.Any(r => KeyOf(r.Image, -1) == prime))
            return Status = FileStatusCode.DuplicateKey;                   // '22' GR36/GR42b
        bool duplicateAlt = false;
        for (int i = 0; i < _alts.Count; i++)
        {
            string value = KeyOf(image, i);
            bool exists = _recs.Any(r => KeyOf(r.Image, i) == value);
            if (exists && !_alts[i].Dups) return Status = FileStatusCode.DuplicateKey;   // '22' GR40/GR42c
            if (exists) duplicateAlt = true;
        }
        _recs.Add(new KeyedRec { Image = stored, Arrival = _nextArrival++ });
        if (sequential) _lastWrittenPrime = prime;
        return Status = duplicateAlt ? FileStatusCode.DuplicateAlternateKey : FileStatusCode.Success;
    }

    /// <summary>REWRITE (§14.9.35): open mode I-O ('49'); sequential access requires the previous op to be a
    /// successful READ ('43', GR5) AND the prime key to equal the last-read prime ('21', GR22/GR25a);
    /// random/dynamic an existing prime ('23', GR23/GR25b); a no-DUPLICATES alternate conflict with ANOTHER
    /// record → '22' (GR25c); a CHANGED alternate key repositions the record LAST in its duplicate set (GR24b —
    /// it takes the next arrival number); a permitted duplicate alternate created → '02' (§9.1.13.2 2c).</summary>
    public string Rewrite(string image, int length = -1)
    {
        bool wasRead = _prevOpWasSuccessfulRead;
        _prevOpWasSuccessfulRead = false;
        if (!IsOpen || _mode != FileOpenMode.IO) return Status = FileStatusCode.DeleteRewriteNotOpenForIO;
        // §14.9.35 GR18 — an indexed record's size MAY differ from the replaced record's; GR20 still bounds it.
        if (Stored(image, length) is not { } stored)
            return Status = FileStatusCode.RecordSizeViolation;                                 // '44' GR20
        image = Fit(image);
        string prime = KeyOf(image, -1);
        if (_access == KeyedAccess.Sequential)
        {
            if (!wasRead) return Status = FileStatusCode.NoSuccessfulReadBeforeDeleteRewrite;   // '43' GR5
            if (prime != _lastReadPrime) return Status = FileStatusCode.SequenceError;          // '21' GR22
        }
        KeyedRec? target = _recs.FirstOrDefault(r => KeyOf(r.Image, -1) == prime);
        if (target is null) return Status = FileStatusCode.RecordNotFound;                      // '23' GR23
        bool duplicateAlt = false, altChanged = false;
        for (int i = 0; i < _alts.Count; i++)
        {
            string newValue = KeyOf(image, i);
            bool exists = _recs.Any(r => !ReferenceEquals(r, target) && KeyOf(r.Image, i) == newValue);
            if (exists && !_alts[i].Dups) return Status = FileStatusCode.DuplicateKey;          // '22' GR25c
            if (exists) duplicateAlt = true;
            if (newValue != KeyOf(target.Image, i)) altChanged = true;
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
        bool wasRead = _prevOpWasSuccessfulRead;
        _prevOpWasSuccessfulRead = false;
        if (!IsOpen || _mode != FileOpenMode.IO) return Status = FileStatusCode.DeleteRewriteNotOpenForIO;
        string prime;
        if (_access == KeyedAccess.Sequential)
        {
            if (!wasRead) return Status = FileStatusCode.NoSuccessfulReadBeforeDeleteRewrite;
            prime = _lastReadPrime ?? "";
        }
        else
            prime = KeyOf(Fit(keyedRecordImage), -1);
        KeyedRec? target = _recs.FirstOrDefault(r => KeyOf(r.Image, -1) == prime);
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
        _prevOpWasSuccessfulRead = false;
        if (!IsOpen || _mode is FileOpenMode.Output or FileOpenMode.Extend)
            return Status = FileStatusCode.ReadNotOpenForInput;            // '47' §14.9.41 GR1
        if (_optionalAbsent) return StartFail();                           // '23' GR5
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
            int c = string.CompareOrdinal(part, value);
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
        _lastReadUnsuccessful = false;
        return Status = FileStatusCode.Success;
    }

    /// <summary>START FIRST/LAST (COBOL-2002+): position at the first/last record under the CURRENT key of
    /// reference (the prime key after OPEN, §14.9.27 GR14); an empty or absent-optional file → invalid key.</summary>
    public string StartFirstLast(bool last)
    {
        _prevOpWasSuccessfulRead = false;
        if (!IsOpen || _mode is FileOpenMode.Output or FileOpenMode.Extend)
            return Status = FileStatusCode.ReadNotOpenForInput;
        if (_optionalAbsent || _recs.Count == 0) return StartFail();
        var seq = Ordered(_refKey);
        var rec = last ? seq[^1] : seq[0];
        _fpiKey = KeyOf(rec.Image, _refKey);
        _fpiArrival = rec.Arrival;
        _fpiValid = true; _inclusive = true; _positioner = 'S';
        _lastReadUnsuccessful = false;
        return Status = FileStatusCode.Success;
    }

    private string StartFail()
    {
        _fpiValid = false;                  // §14.9.41 GR7 — no valid position; key of reference undefined
        _lastReadUnsuccessful = true;       // → '46' on the next sequential READ (§9.1.13.7 6a)
        return Status = FileStatusCode.RecordNotFound;
    }

    // ── Internals ────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The records ordered by (key-of-reference value, arrival) — derived per call from the one record
    /// list (the legacy's lesson: a maintained clone index goes stale across REWRITE/DELETE).</summary>
    private List<KeyedRec> Ordered(int keyIndex) =>
        [.. _recs.OrderBy(r => KeyOf(r.Image, keyIndex), StringComparer.Ordinal).ThenBy(r => r.Arrival)];

    /// <summary>The key value at <paramref name="keyIndex"/> (−1 = prime) — a fixed (offset, length) slice of the
    /// record's character image (§12.4.5.12 GR2 — the key IS its position range in the record).</summary>
    private string KeyOf(string image, int keyIndex)
    {
        var (off, len) = keyIndex < 0 ? (_primeOff, _primeLen) : (_alts[keyIndex].Off, _alts[keyIndex].Len);
        if (image.Length < off + len) image = image.PadRight(off + len, ' ');
        return image.Substring(off, len);
    }

    private string Fit(string s) =>
        s.Length == _recordWidth ? s : s.Length > _recordWidth ? s[.._recordWidth] : s.PadRight(_recordWidth, ' ');

    private void Load()
    {
        _recs.Clear();
        _nextArrival = 1;
        // A varying file's frames keep their exact stored lengths (§13.18.43 GR15 reports them on READ);
        // fixed frames normalize to the record width.
        foreach (string? frame in KeyedFrames.Read(HostPath))
            if (frame is not null)
                _recs.Add(new KeyedRec { Image = IsVarying ? frame : Fit(frame), Arrival = _nextArrival++ });
    }
}

/// <summary>
/// The KEYED (relative/indexed) half of the <see cref="CobolFile"/> facade (one facade, partial across the
/// organization slices — the singular-pattern rule; the sequential half owns the registry plumbing and routes
/// OPEN/CLOSE/Status here when the name is not a sequential connector). The generated code calls these entry
/// points in the SSOT status-first shape: every verb RETURNS the two-character I-O status (§9.1.13) and the
/// emitter branches on its first character.
/// </summary>
public static partial class CobolFile
{
    private static readonly Dictionary<string, RelativeFile> RelativeFiles = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, IndexedFile> IndexedFiles = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Register a SELECTed RELATIVE file (emitted at program start). <paramref name="relativeKeyDigits"/>
    /// is the RELATIVE KEY item's digit capacity (statuses '14'/'24', §9.1.13.4 / §14.9.51 GR29a; 0 = no clause).
    /// <paramref name="varyMin"/>/<paramref name="varyMax"/> are the RECORD IS VARYING bounds (ISO §13.18.43
    /// GR9/GR10; -1,-1 = fixed-length).</summary>
    public static void RegisterRelative(string cobolName, string assignTarget, int recordWidth, bool optional,
        int accessMode, int relativeKeyDigits, int varyMin = -1, int varyMax = -1) =>
        RelativeFiles[cobolName] = new RelativeFile(ResolveHostPath(assignTarget), recordWidth,
            (KeyedAccess)accessMode, relativeKeyDigits, varyMin, varyMax) { IsOptional = optional };

    /// <summary>Register a SELECTed INDEXED file (emitted at program start) with its PRIME key's (offset, length)
    /// range of the record image (§12.4.5.12) and the RECORD IS VARYING bounds (-1,-1 = fixed-length).</summary>
    public static void RegisterIndexed(string cobolName, string assignTarget, int recordWidth, bool optional,
        int accessMode, int primeOffset, int primeLength, int varyMin = -1, int varyMax = -1) =>
        IndexedFiles[cobolName] = new IndexedFile(ResolveHostPath(assignTarget), recordWidth,
            (KeyedAccess)accessMode, primeOffset, primeLength, varyMin, varyMax) { IsOptional = optional };

    /// <summary>Register one ALTERNATE RECORD KEY (§12.4.5.6), in declaration order.</summary>
    public static void AddAlternateKey(string name, int offset, int length, bool duplicates)
    {
        if (IndexedFiles.TryGetValue(name, out var f)) f.AddAlternateKey(offset, length, duplicates);
    }

    /// <summary>Stage the RELATIVE KEY item's value for the next keyed verb (the compiler reads the TYPED field).</summary>
    public static void SetRelativeKey(string name, long rrn)
    {
        if (RelativeFiles.TryGetValue(name, out var f)) f.SetPendingKey(rrn);
    }

    /// <summary>The RRN last made available/released — the §14.9.30 GR25 / §14.9.51 GR29a MOVE-back source.</summary>
    public static long RelativeSlot(string name) =>
        RelativeFiles.TryGetValue(name, out var f) ? f.LastSlot : 0;

    /// <summary>Keyed WRITE (§14.9.51) — returns the I-O status. <paramref name="length"/> is the varying-record
    /// length (ISO §13.18.43 GR13a), -1 = the record's own size.</summary>
    public static string WriteKeyed(string name, string image, int length = -1) =>
        RelativeFiles.TryGetValue(name, out var r) ? r.Write(image, length)
        : IndexedFiles.TryGetValue(name, out var ix) ? ix.Write(image, length)
        : FileStatusCode.PermanentError;

    /// <summary>Keyed REWRITE (§14.9.35) — returns the I-O status. <paramref name="length"/> is the varying-record
    /// length (§13.18.43 GR13a; the keyed record size MAY differ from the replaced record's, §14.9.35 GR18).</summary>
    public static string RewriteKeyed(string name, string image, int length = -1) =>
        RelativeFiles.TryGetValue(name, out var r) ? r.Rewrite(image, length)
        : IndexedFiles.TryGetValue(name, out var ix) ? ix.Rewrite(image, length)
        : FileStatusCode.PermanentError;

    /// <summary>The length of the most recently read keyed record (ISO §13.18.43 GR15).</summary>
    private static int KeyedLastReadLength(string name) =>
        RelativeFiles.TryGetValue(name, out var r) ? r.LastReadLength
        : IndexedFiles.TryGetValue(name, out var ix) ? ix.LastReadLength
        : 0;

    /// <summary>DELETE RECORD (§14.9.10 F1); for indexed random/dynamic the prime key is sliced from
    /// <paramref name="keyedRecordImage"/> (GR3) — relative uses the staged relative key (GR4).</summary>
    public static string DeleteRecord(string name, string keyedRecordImage) =>
        RelativeFiles.TryGetValue(name, out var r) ? r.Delete()
        : IndexedFiles.TryGetValue(name, out var ix) ? ix.Delete(keyedRecordImage)
        : FileStatusCode.PermanentError;

    /// <summary>Sequential keyed READ [NEXT] (§14.9.30 F1) — returns the I-O status and the record image.</summary>
    public static string ReadKeyedNext(string name, out string image)
    {
        if (RelativeFiles.TryGetValue(name, out var r)) return r.ReadNext(out image);
        if (IndexedFiles.TryGetValue(name, out var ix)) return ix.ReadNext(out image);
        image = "";
        return FileStatusCode.PermanentError;
    }

    /// <summary>Sequential keyed READ PREVIOUS (§14.9.30 F1, COBOL-2002+; compiler edition-gated).</summary>
    public static string ReadKeyedPrevious(string name, out string image)
    {
        if (RelativeFiles.TryGetValue(name, out var r)) return r.ReadPrevious(out image);
        if (IndexedFiles.TryGetValue(name, out var ix)) return ix.ReadPrevious(out image);
        image = "";
        return FileStatusCode.PermanentError;
    }

    /// <summary>Random keyed READ (§14.9.30 F2): <paramref name="keyIndex"/> = −1 prime / i-th alternate (indexed,
    /// GR30–GR32; the key value is sliced from <paramref name="keyedRecordImage"/>); relative uses the staged
    /// relative key (GR29) and ignores both parameters.</summary>
    public static string ReadKeyed(string name, int keyIndex, string keyedRecordImage, out string image)
    {
        if (RelativeFiles.TryGetValue(name, out var r)) return r.ReadRandom(out image);
        if (IndexedFiles.TryGetValue(name, out var ix)) return ix.ReadRandom(keyIndex, keyedRecordImage, out image);
        image = "";
        return FileStatusCode.PermanentError;
    }

    /// <summary>START on a relative file (§14.9.41 GR8–GR12) — a numeric RRN comparison.</summary>
    public static string StartRelative(string name, string op, long rrn) =>
        RelativeFiles.TryGetValue(name, out var f) ? f.Start(op, rrn) : FileStatusCode.PermanentError;

    /// <summary>START on an indexed file (§14.9.41 GR13–GR17) — a leftmost-length partial-key comparison.</summary>
    public static string StartIndexed(string name, int keyIndex, string op, string operand, int compareLength) =>
        IndexedFiles.TryGetValue(name, out var f)
            ? f.Start(keyIndex, op, operand, compareLength) : FileStatusCode.PermanentError;

    /// <summary>START FIRST/LAST (COBOL-2002+; §14.9.41 GR11/GR12), either organization.</summary>
    public static string StartFirstLast(string name, bool last) =>
        RelativeFiles.TryGetValue(name, out var r) ? r.StartFirstLast(last)
        : IndexedFiles.TryGetValue(name, out var ix) ? ix.StartFirstLast(last)
        : FileStatusCode.PermanentError;

    /// <summary>DELETE FILE (§14.9.10 Format 2, COBOL-2023): an OPEN connector → '41' (GR13); an ABSENT physical
    /// file is a SUCCESSFUL completion, status '05' (GR14 — the legacy's '35' was a deviation; the spec wins);
    /// insufficient authority → '37' (GR16). Fixed-file-attribute matching (GR18, '39') awaits that model.</summary>
    public static string DeleteFile(string name)
    {
        string host;
        bool open;
        Action<string> setStatus;
        if (RelativeFiles.TryGetValue(name, out var r)) { host = r.HostPath; open = r.IsOpen; setStatus = r.SetStatus; }
        else if (IndexedFiles.TryGetValue(name, out var ix)) { host = ix.HostPath; open = ix.IsOpen; setStatus = ix.SetStatus; }
        else return FileStatusCode.PermanentError;
        string status;
        if (open) status = FileStatusCode.FileAlreadyOpen;                 // '41' GR13
        else
            try
            {
                if (!File.Exists(host)) status = FileStatusCode.OptionalFileNotFound;   // '05' GR14 — successful
                else { File.Delete(host); status = FileStatusCode.Success; }
            }
            catch (UnauthorizedAccessException) { status = FileStatusCode.PermissionDenied; }   // '37' GR16
            catch (IOException) { status = FileStatusCode.PermanentError; }
        setStatus(status);
        return status;
    }

    // ── Routing hooks the sequential half calls (registry init/open/close/status/close-all) ─────────────────

    private static void KeyedInit()
    {
        RelativeFiles.Clear();
        IndexedFiles.Clear();
    }

    private static void KeyedOpen(string name, FileOpenMode mode)
    {
        if (RelativeFiles.TryGetValue(name, out var r))
        {
            if (Locked.Contains(name)) r.SetStatus(FileStatusCode.FileLocked); else r.Open(mode);
        }
        else if (IndexedFiles.TryGetValue(name, out var ix))
        {
            if (Locked.Contains(name)) ix.SetStatus(FileStatusCode.FileLocked); else ix.Open(mode);
        }
    }

    private static void KeyedClose(string name)
    {
        if (RelativeFiles.TryGetValue(name, out var r)) r.Close();
        else if (IndexedFiles.TryGetValue(name, out var ix)) ix.Close();
    }

    private static int KeyedOpenModeOf(string name) =>
        RelativeFiles.TryGetValue(name, out var r) ? r.OpenModeView
        : IndexedFiles.TryGetValue(name, out var ix) ? ix.OpenModeView
        : -1;

    private static string KeyedStatus(string name) =>
        RelativeFiles.TryGetValue(name, out var r) ? r.Status
        : IndexedFiles.TryGetValue(name, out var ix) ? ix.Status
        : FileStatusCode.Success;

    /// <summary>Close (and so PERSIST) every open keyed connector at run-unit termination (ISO §14.6 — the
    /// implicit CLOSE; keyed chains depend on the store flushing at STOP RUN, e.g. NIST RL208A).</summary>
    private static void KeyedCloseAll()
    {
        foreach (var r in RelativeFiles.Values) if (r.IsOpen) r.Close();
        foreach (var ix in IndexedFiles.Values) if (ix.IsOpen) ix.Close();
    }
}
