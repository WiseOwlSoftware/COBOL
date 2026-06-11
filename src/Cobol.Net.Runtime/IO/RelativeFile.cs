// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;
using System.Text;

namespace CobolNet.Runtime.IO;

/// <summary>The access mode of a keyed file connector (ISO/IEC 1989:2023 §12.4.5.5 ACCESS MODE clause). The
/// ordinals mirror the compiler's <c>FileAccessMode</c> enum — registration passes the raw int.</summary>
public enum KeyedAccess
{
    /// <summary>ACCESS SEQUENTIAL — records in ascending RRN / key-of-reference order (§12.4.5.5 GR2).</summary>
    Sequential = 0,
    /// <summary>ACCESS RANDOM — records selected by key value (§9.1.8.3).</summary>
    Random = 1,
    /// <summary>ACCESS DYNAMIC — both, chosen per statement form (§9.1.8.4).</summary>
    Dynamic = 2,
}

/// <summary>The keyed connectors' on-disk framing: a sequence of records, each a 4-byte little-endian length
/// prefix followed by that many Latin-1 payload bytes; an EMPTY slot is the gap tag 0xFFFFFFFF with no payload
/// (the legacy varying-relative gap marker). Explicit length framing carries occupancy soundly — the legacy's
/// fixed-format all-0x00/0xFF gap HEURISTIC could vanish a legitimate all-zero binary record (brief §2.3 #6), so
/// the greenfield store frames every record explicitly. The physical format is implementor-defined by the spec
/// (§9.1.13.6 boundaries are "externally defined"); both producer and consumer of a NIST chain run on this same
/// connector, so the format needs only self-consistency.</summary>
internal static class KeyedFrames
{
    private const uint GapTag = 0xFFFFFFFF;

    /// <summary>Write the whole store: one frame per ordinal position; null = an empty (gap) slot.</summary>
    public static void Write(string path, IReadOnlyList<string?> frames)
    {
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        Span<byte> len = stackalloc byte[4];
        foreach (string? frame in frames)
        {
            if (frame is null)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(len, GapTag);
                fs.Write(len);
                continue;
            }
            byte[] payload = Encoding.Latin1.GetBytes(frame);
            BinaryPrimitives.WriteUInt32LittleEndian(len, (uint)payload.Length);
            fs.Write(len);
            fs.Write(payload, 0, payload.Length);
        }
    }

    /// <summary>Read the whole store back: one entry per frame, null for a gap. A torn tail ends the store.</summary>
    public static List<string?> Read(string path)
    {
        var frames = new List<string?>();
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        var len = new byte[4];
        while (FillExactly(fs, len, 4))
        {
            uint n = BinaryPrimitives.ReadUInt32LittleEndian(len);
            if (n == GapTag) { frames.Add(null); continue; }
            var payload = new byte[n];
            if (!FillExactly(fs, payload, (int)n)) break;
            frames.Add(Encoding.Latin1.GetString(payload));
        }
        return frames;
    }

    private static bool FillExactly(Stream s, byte[] buf, int count)
    {
        int got = 0;
        while (got < count)
        {
            int n = s.Read(buf, got, count - got);
            if (n == 0) return false;
            got += n;
        }
        return true;
    }
}

/// <summary>
/// A typed-native RELATIVE-organization file connector (ISO/IEC 1989:2023 §9.1.7.3): a sparse 1-based slot model —
/// each record is identified by its relative record number (RRN); slots between records "do not exist" for READ.
/// The behavioral state machine ports the legacy <c>RelativeFileHandler</c> (NIST RL-suite-proven) onto the
/// character-image substrate: the file position indicator (§9.1.11) is a slot number + an inclusive flag (set by
/// OPEN/START, advanced exclusively by READ), '46' poisoning after an unsuccessful READ/START (§14.9.30 GR21 /
/// §9.1.13.7 item 6), and the '43' previous-op-was-READ rule for sequential-access REWRITE/DELETE (§14.9.35 GR5 /
/// §14.9.10 GR2). Records cross this boundary as their character image (a <see cref="string"/>).
/// </summary>
public sealed class RelativeFile
{
    private readonly int _recordWidth;
    private readonly KeyedAccess _access;
    private readonly int _keyDigits;   // RELATIVE KEY digit capacity (0 = no RELATIVE KEY clause)

    /// <summary>The sparse slot store: RRN (1-based, §12.4.5.13 GR1) → record image.</summary>
    private readonly SortedDictionary<long, string> _slots = new();

    private FileOpenMode _mode;
    private bool _optionalAbsent;        // OPEN INPUT of an absent OPTIONAL file (§14.9.27 GR13)
    private long _fpi;                   // file position indicator: the current slot (§9.1.11)
    private bool _fpiValid;
    private bool _inclusive;             // FPI set by OPEN/START — the positioned record itself is next (§14.9.30 GR21)
    private char _positioner = 'O';      // 'O' OPEN / 'S' START / 'R' READ — drives READ PREVIOUS-after-OPEN (row 29)
    private long _pendingKey;            // the RELATIVE KEY item's value, set by the compiler before keyed verbs
    private long _seqNext = 1;           // next sequential-access WRITE slot (§14.9.51 GR29a)
    private long _lastSlot;              // last slot read/written — the GR25/GR29a store-back + seq REWRITE/DELETE target
    private bool _lastReadUnsuccessful;  // → '46' on the next sequential READ (§14.9.30 GR21)
    private bool _prevOpWasSuccessfulRead;   // → '43' gate for sequential-access REWRITE/DELETE

    /// <summary>The resolved host path of the physical file.</summary>
    public string HostPath { get; }

    /// <summary>The latest I-O status (ISO §9.1.13). "00" until the first operation.</summary>
    public string Status { get; private set; } = FileStatusCode.Success;

    /// <summary>True for a SELECT OPTIONAL file (§14.9.27 GR13/GR17).</summary>
    public bool IsOptional { get; init; }

    /// <summary>True between a successful OPEN and the matching CLOSE.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>The RRN of the record last made available / released — the §14.9.30 GR25 and §14.9.51 GR29a
    /// MOVE-back source the generated code stores into the RELATIVE KEY item.</summary>
    public long LastSlot => _lastSlot;

    /// <summary>Set the I-O status directly (facade-level conditions, e.g. a locked-file OPEN).</summary>
    public void SetStatus(string status) => Status = status;

    /// <summary>The open-mode view for USE-declarative mode scoping (ISO 14.9.49.4 GR6b-e): (int) of the mode
    /// while open OR the ATTEMPTED mode of a failed OPEN; -1 after a successful CLOSE / before any OPEN.</summary>
    public int OpenModeView => _modeKnown ? (int)_mode : -1;
    private bool _modeKnown;

    /// <summary>Stage the RELATIVE KEY item's value for the next keyed operation (§14.9.30 GR29, §14.9.35 GR21,
    /// §14.9.51 GR29b, §14.9.10 GR4). The compiler decodes the TYPED key field — never raw bytes.</summary>
    public void SetPendingKey(long rrn) => _pendingKey = rrn;

    public RelativeFile(string hostPath, int recordWidth, KeyedAccess access, int relativeKeyDigits)
    {
        HostPath = hostPath;
        _recordWidth = recordWidth < 1 ? 1 : recordWidth;
        _access = access;
        _keyDigits = relativeKeyDigits;
    }

    // ── OPEN / CLOSE (ISO §14.9.27 / §14.9.6) ────────────────────────────────────────────────────────────────

    /// <summary>OPEN in <paramref name="mode"/>. GR14: INPUT/I-O position the FPI at 1; GR15: EXTEND positions
    /// after the highest existing RRN; GR17: an absent OPTIONAL file on I-O/EXTEND is created ('05'); §9.1.13.6
    /// item 5: an absent NON-optional file on INPUT/I-O/EXTEND is '35'; §9.1.13.7 item 1: already open is '41'.</summary>
    public string Open(FileOpenMode mode)
    {
        if (IsOpen) return Status = FileStatusCode.FileAlreadyOpen;
        _mode = mode;
        _modeKnown = true;   // a FAILED open still records the attempted mode (GR6b "being opened")
        _optionalAbsent = false;
        _lastReadUnsuccessful = false;
        _prevOpWasSuccessfulRead = false;
        _slots.Clear();
        _pendingKey = 0;
        _lastSlot = 0;
        _positioner = 'O';
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
                        _optionalAbsent = true;               // positioned "not present" (§14.9.27 GR13)
                        status = FileStatusCode.OptionalFileNotFound;
                        break;
                    }
                    Load();
                    break;
                case FileOpenMode.Output:
                    KeyedFrames.Write(HostPath, []);          // a new physical file; records persist at CLOSE
                    _seqNext = 1;                              // §14.9.51 GR29a — first record released is 1
                    break;
                case FileOpenMode.IO:
                    if (!exists)
                    {
                        if (!IsOptional) return Status = FileStatusCode.FileNotFound;
                        KeyedFrames.Write(HostPath, []);      // created as if OPEN OUTPUT + CLOSE (§14.9.27 GR17)
                        status = FileStatusCode.OptionalFileNotFound;
                        break;
                    }
                    Load();
                    break;
                case FileOpenMode.Extend:
                    if (!exists)
                    {
                        if (!IsOptional) return Status = FileStatusCode.FileNotFound;
                        KeyedFrames.Write(HostPath, []);      // §14.9.27 GR17
                        status = FileStatusCode.OptionalFileNotFound;
                    }
                    else Load();
                    _seqNext = (_slots.Count == 0 ? 0 : _slots.Keys.Max()) + 1;   // §14.9.27 GR15 / §14.9.51 GR29a
                    break;
            }
        }
        catch (UnauthorizedAccessException) { return Status = FileStatusCode.PermissionDenied; }
        catch (IOException) { return Status = FileStatusCode.PermanentError; }
        IsOpen = true;
        _fpi = 1;                                              // §14.9.27 GR14 — FPI = 1 on INPUT/I-O
        _fpiValid = mode is FileOpenMode.Input or FileOpenMode.IO;
        _inclusive = true;
        return Status = status;
    }

    /// <summary>CLOSE (ISO §14.9.6): not open → '42' (§9.1.13.7 item 2); a writable mode persists the store —
    /// including via the run-unit-termination <c>CloseAll</c>, which keyed chains (RL208A) depend on.</summary>
    public string Close()
    {
        if (!IsOpen) return Status = FileStatusCode.FileNotOpen;
        try
        {
            if (!_optionalAbsent && _mode is not FileOpenMode.Input) Persist();
        }
        catch (IOException) { IsOpen = false; return Status = FileStatusCode.PermanentError; }
        IsOpen = false;
        _optionalAbsent = false;
        _modeKnown = false;   // 9.1.4 - after a successful CLOSE the file is in no open mode
        return Status = FileStatusCode.Success;
    }

    // ── READ (ISO §14.9.30) ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>Sequential <c>READ [NEXT]</c> — the first existing slot at/after the FPI (inclusive after
    /// OPEN/START, exclusive after a READ; §14.9.30 GR21 relative rules b/c). No such slot → at end '10' (GR24);
    /// an RRN whose significant digits exceed the RELATIVE KEY item's size → '14', at end (GR21 rule d).</summary>
    public string ReadNext(out string image) => ReadSequential(out image, previous: false);

    /// <summary>Sequential <c>READ PREVIOUS</c> (COBOL-2002+; the compiler edition-gates the phrase). Immediately
    /// after OPEN it is the at-end condition — the ISO-2023 behavior (VERSION_CHANGE_REFERENCE row 29; the ≤2014
    /// behavior gate is a noted deferral); after START the selected record itself is made available (GR21 rule b
    /// — "regardless of whether NEXT or PREVIOUS").</summary>
    public string ReadPrevious(out string image) => ReadSequential(out image, previous: true);

    private string ReadSequential(out string image, bool previous)
    {
        image = new string(' ', _recordWidth);
        _prevOpWasSuccessfulRead = false;
        if (!IsOpen || _mode is FileOpenMode.Output or FileOpenMode.Extend)
            return Status = FileStatusCode.ReadNotOpenForInput;            // '47' §9.1.13.7 item 7
        if (_optionalAbsent) { _lastReadUnsuccessful = true; return Status = FileStatusCode.AtEnd; }   // '10' §9.1.13.4 1c
        if (_lastReadUnsuccessful) return Status = FileStatusCode.NoValidNextRecord;   // '46' §14.9.30 GR21
        if (!_fpiValid) return Status = FileStatusCode.NoValidNextRecord;              // '46' §9.1.13.7 6a (failed START)

        long? slot = null;
        if (previous && _positioner == 'O')
            slot = null;   // READ PREVIOUS immediately after OPEN → at end (2023; VERSION_CHANGE_REFERENCE row 29)
        else if (previous)
        {
            long bound = _inclusive ? _fpi : _fpi - 1;
            foreach (long k in _slots.Keys) { if (k <= bound) slot = k; else break; }
        }
        else
        {
            long bound = _inclusive ? _fpi : _fpi + 1;
            foreach (long k in _slots.Keys) if (k >= bound) { slot = k; break; }
        }
        if (slot is not { } s)
        {
            _lastReadUnsuccessful = true;
            _fpiValid = false;                                             // §14.9.30 GR24b
            return Status = FileStatusCode.AtEnd;
        }
        if (_keyDigits > 0 && s.ToString().Length > _keyDigits)
        {
            // §14.9.30 GR21 relative rule d: significant digits of the RRN exceed the RELATIVE KEY item → '14',
            // the at-end condition, FPI invalidated.
            _lastReadUnsuccessful = true;
            _fpiValid = false;
            return Status = FileStatusCode.RelativeKeyOverflow;
        }
        _fpi = s; _fpiValid = true; _inclusive = false; _positioner = 'R';   // GR21 rule f
        _lastSlot = s;
        _prevOpWasSuccessfulRead = true;
        _lastReadUnsuccessful = false;
        image = Fit(_slots[s]);
        return Status = FileStatusCode.Success;
    }

    /// <summary>Format-2 random READ (§14.9.30 GR29): the FPI takes the RELATIVE KEY item's value (staged by
    /// <see cref="SetPendingKey"/>); no such record → invalid key '23'; an absent optional file → '23' (GR28).</summary>
    public string ReadRandom(out string image)
    {
        image = new string(' ', _recordWidth);
        _prevOpWasSuccessfulRead = false;
        if (!IsOpen || _mode is FileOpenMode.Output or FileOpenMode.Extend)
            return Status = FileStatusCode.ReadNotOpenForInput;
        if (_optionalAbsent) { _lastReadUnsuccessful = true; return Status = FileStatusCode.RecordNotFound; }
        if (!_slots.TryGetValue(_pendingKey, out string? rec))
        {
            _lastReadUnsuccessful = true;
            return Status = FileStatusCode.RecordNotFound;                 // '23' §9.1.13.5 3a
        }
        _fpi = _pendingKey; _fpiValid = true; _inclusive = false; _positioner = 'R';
        _lastSlot = _pendingKey;
        _prevOpWasSuccessfulRead = true;
        _lastReadUnsuccessful = false;
        image = Fit(rec);
        return Status = FileStatusCode.Success;
    }

    // ── WRITE / REWRITE / DELETE (ISO §14.9.51 / §14.9.35 / §14.9.10) ───────────────────────────────────────

    /// <summary>WRITE (§14.9.51 GR29): sequential access releases consecutive RRNs (OUTPUT from 1, EXTEND from
    /// highest+1); RRN digit overflow of the key item → invalid key '24' (GR29a/GR33c). Random/dynamic writes the
    /// slot staged in the key item: occupied → '22' (GR33a), key &lt; 1 → permanent error '34' (GR29b). Open-mode
    /// legality per §9.1.13.7 item 8 ('48').</summary>
    public string Write(string image)
    {
        _prevOpWasSuccessfulRead = false;
        bool sequential = _access == KeyedAccess.Sequential || _mode == FileOpenMode.Extend;
        if (sequential)
        {
            if (!IsOpen || _mode is not (FileOpenMode.Output or FileOpenMode.Extend))
                return Status = FileStatusCode.WriteNotOpenForOutput;      // '48' §9.1.13.7 8a
            long slot = _seqNext;
            if (_keyDigits > 0 && slot.ToString().Length > _keyDigits)
                return Status = FileStatusCode.BoundaryViolation;          // '24' §14.9.51 GR29a
            _slots[slot] = Fit(image);
            _seqNext = slot + 1;
            _lastSlot = slot;                                              // GR29a — MOVEd back into the key item
            return Status = FileStatusCode.Success;
        }
        if (!IsOpen || _mode is not (FileOpenMode.IO or FileOpenMode.Output))
            return Status = FileStatusCode.WriteNotOpenForOutput;          // '48' §9.1.13.7 8b
        long key = _pendingKey;
        if (key < 1) return Status = FileStatusCode.PermanentBoundary;     // '34' §14.9.51 GR29b
        if (_slots.ContainsKey(key)) return Status = FileStatusCode.DuplicateKey;   // '22' §14.9.51 GR33a
        _slots[key] = Fit(image);
        _lastSlot = key;
        return Status = FileStatusCode.Success;
    }

    /// <summary>REWRITE (§14.9.35): open mode must be I-O ('49', §9.1.13.7 item 9). Sequential access replaces
    /// the prior READ's record (no prior successful READ → '43', GR5); random/dynamic replaces the slot named by
    /// the key item (absent → '23', GR21). The FPI is unaffected (GR13).</summary>
    public string Rewrite(string image)
    {
        bool wasRead = _prevOpWasSuccessfulRead;
        _prevOpWasSuccessfulRead = false;
        if (!IsOpen || _mode != FileOpenMode.IO) return Status = FileStatusCode.DeleteRewriteNotOpenForIO;
        if (_access == KeyedAccess.Sequential)
        {
            if (!wasRead) return Status = FileStatusCode.NoSuccessfulReadBeforeDeleteRewrite;   // '43'
            _slots[_lastSlot] = Fit(image);
            return Status = FileStatusCode.Success;
        }
        if (!_slots.ContainsKey(_pendingKey)) return Status = FileStatusCode.RecordNotFound;    // '23' GR21
        _slots[_pendingKey] = Fit(image);
        return Status = FileStatusCode.Success;
    }

    /// <summary>DELETE RECORD (§14.9.10): open mode must be I-O ('49', GR1). Sequential access removes the prior
    /// READ's record ('43' without one, GR2); random/dynamic removes the slot named by the key item (absent →
    /// invalid key '23', GR4). The FPI is unaffected (GR9).</summary>
    public string Delete()
    {
        bool wasRead = _prevOpWasSuccessfulRead;
        _prevOpWasSuccessfulRead = false;
        if (!IsOpen || _mode != FileOpenMode.IO) return Status = FileStatusCode.DeleteRewriteNotOpenForIO;
        if (_access == KeyedAccess.Sequential)
        {
            if (!wasRead) return Status = FileStatusCode.NoSuccessfulReadBeforeDeleteRewrite;
            _slots.Remove(_lastSlot);
            return Status = FileStatusCode.Success;
        }
        if (!_slots.Remove(_pendingKey)) return Status = FileStatusCode.RecordNotFound;
        return Status = FileStatusCode.Success;
    }

    // ── START (ISO §14.9.41) ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>START KEY rel-op (GR9): a NUMERIC comparison over RRNs against <paramref name="operand"/> (the
    /// RELATIVE KEY item's value, GR10) — forward search for =, &gt;, &gt;=; REVERSE search for &lt;, &lt;= (the
    /// first record satisfying the comparison searching the file in reverse order, GR9b). Not satisfied → invalid
    /// key '23' with the FPI invalidated (GR7/GR9c). Open mode must be input or I-O (GR1 → '47').</summary>
    public string Start(string op, long operand)
    {
        _prevOpWasSuccessfulRead = false;
        if (!IsOpen || _mode is FileOpenMode.Output or FileOpenMode.Extend)
            return Status = FileStatusCode.ReadNotOpenForInput;
        if (_optionalAbsent) return StartFail();                           // '23' §14.9.41 GR5
        long? found = null;
        switch (op)
        {
            case "==": if (_slots.ContainsKey(operand)) found = operand; break;
            case ">": foreach (long k in _slots.Keys) if (k > operand) { found = k; break; } break;
            case ">=": foreach (long k in _slots.Keys) if (k >= operand) { found = k; break; } break;
            case "<": foreach (long k in _slots.Keys) { if (k < operand) found = k; else break; } break;
            case "<=": foreach (long k in _slots.Keys) { if (k <= operand) found = k; else break; } break;
        }
        return found is { } f ? StartAt(f) : StartFail();
    }

    /// <summary>START FIRST/LAST (COBOL-2002+; §14.9.41 GR11/GR12): the lowest / highest existing RRN; an empty
    /// (or absent optional) file → invalid key '23'.</summary>
    public string StartFirstLast(bool last)
    {
        _prevOpWasSuccessfulRead = false;
        if (!IsOpen || _mode is FileOpenMode.Output or FileOpenMode.Extend)
            return Status = FileStatusCode.ReadNotOpenForInput;
        if (_optionalAbsent || _slots.Count == 0) return StartFail();
        return StartAt(last ? _slots.Keys.Max() : _slots.Keys.Min());
    }

    private string StartAt(long slot)
    {
        _fpi = slot; _fpiValid = true; _inclusive = true; _positioner = 'S';
        _lastReadUnsuccessful = false;
        return Status = FileStatusCode.Success;
    }

    private string StartFail()
    {
        _fpiValid = false;                  // §14.9.41 GR7 — no valid record position established
        _lastReadUnsuccessful = true;       // → '46' on the next sequential READ (§9.1.13.7 6a)
        return Status = FileStatusCode.RecordNotFound;
    }

    // ── Persistence ──────────────────────────────────────────────────────────────────────────────────────────

    private void Load()
    {
        _slots.Clear();
        var frames = KeyedFrames.Read(HostPath);
        for (int i = 0; i < frames.Count; i++)
            if (frames[i] is { } rec)
                _slots[i + 1] = rec;        // slot ordinal = frame ordinal (1-based RRN, §12.4.5.13 GR1)
    }

    private void Persist()
    {
        long max = _slots.Count == 0 ? 0 : _slots.Keys.Max();
        var frames = new string?[max];
        foreach (var (slot, rec) in _slots) frames[slot - 1] = rec;
        KeyedFrames.Write(HostPath, frames);
    }

    /// <summary>Pad/truncate to the fixed record width (RECORD VARYING relative records are a later slice).</summary>
    private string Fit(string s) =>
        s.Length == _recordWidth ? s : s.Length > _recordWidth ? s[.._recordWidth] : s.PadRight(_recordWidth, ' ');
}
