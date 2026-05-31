// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace CobolSharp.Runtime.IO;

/// <summary>
/// Relative file handler for COBOL ORGANIZATION IS RELATIVE.
/// Records are addressed by a 1-based relative record number ("slot"). A relative file is a set of
/// fixed-size slots, each either OCCUPIED (holds a record) or EMPTY; slots may be sparse (gaps).
/// Modelled in memory as a slot→record map (like the indexed handler) so occupancy is tracked
/// exactly — enabling INVALID KEY (status 23) on an absent slot, duplicate (22) on a WRITE to an
/// occupied slot, and READ NEXT that skips gaps. Persisted to a sparse flat file (slot N at byte
/// offset (N-1)*recordLength; gap slots written as 0xFF) so it round-trips across OPEN/CLOSE.
/// </summary>
public class RelativeFileHandler : IFileHandler
{
    private readonly int _recordLength;
    private readonly int _relativeKeyDigits;   // digit capacity of the RELATIVE KEY (0 = unknown)
    private SortedDictionary<int, byte[]>? _records;  // occupied slots; null when closed
    private int _currentRecord;                // last slot read/positioned (0 = none)
    private int _pendingKey;                   // relative key value for the next keyed WRITE/REWRITE/DELETE
    private FileOpenMode _openMode;
    private string? _dataFilePath;

    public string ExternalName { get; }
    public bool IsOpen => _records != null;

    /// <summary>Relative records are fixed-length; the record length is constant.</summary>
    public int LastRecordLength => _recordLength;

    /// <summary>When true (ACCESS MODE SEQUENTIAL) WRITE appends to the next slot and REWRITE/DELETE
    /// act on the current record; when false (RANDOM/DYNAMIC) they act on the slot given by the
    /// RELATIVE KEY (the pending key set just before the operation).</summary>
    public bool SequentialAccess { get; set; } = true;

    /// <summary>When true (SELECT OPTIONAL), OPEN INPUT on a missing file returns "05" instead of "35".</summary>
    public bool IsOptional { get; set; }

    /// <summary>Slot the most recent successful keyed/sequential WRITE or READ acted on (for the
    /// caller to store back into the RELATIVE KEY data item after a sequential WRITE / READ NEXT).</summary>
    public int CurrentSlot => _currentRecord;

    public RelativeFileHandler(string externalName, int recordLength, int relativeKeyDigits = 0)
    {
        ExternalName = externalName;
        _recordLength = recordLength;
        _relativeKeyDigits = relativeKeyDigits;
    }

    /// <summary>Set the relative record number for the next keyed WRITE/REWRITE/DELETE
    /// (RANDOM/DYNAMIC access). Read from the RELATIVE KEY data item by the caller.</summary>
    public void SetPendingKey(int key) => _pendingKey = key;

    public string Open(FileOpenMode mode)
    {
        if (IsOpen) return FileStatus.FileAlreadyOpen;
        _openMode = mode;
        _currentRecord = 0;
        _dataFilePath = ExternalName;
        _records = new SortedDictionary<int, byte[]>();

        bool exists = File.Exists(ExternalName);
        try
        {
            switch (mode)
            {
                case FileOpenMode.Input:
                    if (!exists)
                    {
                        _records = null;
                        return IsOptional ? FileStatus.OptionalFileNotFound : FileStatus.FileNotFound;
                    }
                    LoadFromFile();
                    break;

                case FileOpenMode.InputOutput:
                case FileOpenMode.Extend:
                    if (exists) LoadFromFile();
                    // A missing optional file is created on first open (status 05); otherwise created
                    // empty here (the standard permits creating a relative I-O/EXTEND file).
                    if (!exists && IsOptional) return FileStatus.OptionalFileNotFound;
                    break;

                case FileOpenMode.Output:
                    // Fresh, empty file (existing content discarded).
                    break;
            }
            return FileStatus.Success;
        }
        catch (IOException)
        {
            _records = null;
            return FileStatus.PermanentError;
        }
    }

    private void LoadFromFile()
    {
        using var stream = new FileStream(_dataFilePath!, FileMode.Open, FileAccess.Read);
        byte[] buf = new byte[_recordLength];
        int slot = 0;
        while (stream.Read(buf, 0, _recordLength) == _recordLength)
        {
            slot++;
            // A slot is occupied unless it is all-0x00 (never written / OS zero-fill) or all-0xFF
            // (a deleted/empty marker).
            if (!IsEmptySlot(buf))
                _records![slot] = (byte[])buf.Clone();
        }
    }

    private static bool IsEmptySlot(byte[] slot)
    {
        bool allZero = true, allFf = true;
        foreach (byte b in slot)
        {
            if (b != 0x00) allZero = false;
            if (b != 0xFF) allFf = false;
            if (!allZero && !allFf) return false;
        }
        return true;
    }

    public string Close()
    {
        if (!IsOpen) return FileStatus.FileNotOpen;
        try
        {
            if (_openMode != FileOpenMode.Input && _dataFilePath != null)
                PersistToFile();
        }
        catch (IOException)
        {
            _records = null;
            return FileStatus.PermanentError;
        }
        _records = null;
        _currentRecord = 0;
        return FileStatus.Success;
    }

    private void PersistToFile()
    {
        using var stream = new FileStream(_dataFilePath!, FileMode.Create, FileAccess.Write);
        if (_records!.Count == 0) return;
        int max = 0;
        foreach (var k in _records.Keys) if (k > max) max = k;
        byte[] gap = new byte[_recordLength];
        Array.Fill(gap, (byte)0xFF);
        for (int slot = 1; slot <= max; slot++)
            stream.Write(_records.TryGetValue(slot, out var rec) ? rec : gap, 0, _recordLength);
    }

    public string ReadNext(byte[] recordBuffer)
    {
        // READ on a file connector not open in input/I-O mode is status 47 (ISO §9.1.13.7).
        if (!IsOpen) return FileStatus.ReadNotOpenForInput;
        if (_openMode is FileOpenMode.Output or FileOpenMode.Extend)
            return FileStatus.ReadNotOpenForInput;

        // The next occupied slot after the current position (READ NEXT skips gaps; ISO §14.9.30).
        foreach (var kv in _records!)
        {
            if (kv.Key <= _currentRecord) continue;
            // §14.9.30 GR: if the selected record's relative number needs more significant digits than
            // the RELATIVE KEY data item holds, status 14 (an at-end condition; ISO §9.1.13/§14.7.4).
            if (_relativeKeyDigits > 0 && kv.Key >= Pow10(_relativeKeyDigits))
                return FileStatus.RelativeKeyOverflow; // 14
            _currentRecord = kv.Key;
            CopyToBuffer(kv.Value, recordBuffer);
            return FileStatus.Success;
        }
        return FileStatus.AtEnd; // 10 — no next existing record
    }

    public string ReadPrevious(byte[] recordBuffer)
    {
        if (!IsOpen) return FileStatus.ReadNotOpenForInput;
        if (_openMode is FileOpenMode.Output or FileOpenMode.Extend)
            return FileStatus.ReadNotOpenForInput;
        int best = 0;
        foreach (var k in _records!.Keys)
        {
            if (_currentRecord != 0 && k >= _currentRecord) break;
            best = k;
        }
        if (best == 0) return FileStatus.AtEnd;
        _currentRecord = best;
        CopyToBuffer(_records[best], recordBuffer);
        return FileStatus.Success;
    }

    public string ReadByKey(byte[] recordBuffer, byte[] keyValue)
    {
        if (!IsOpen) return FileStatus.ReadNotOpenForInput;
        if (_openMode is FileOpenMode.Output or FileOpenMode.Extend)
            return FileStatus.ReadNotOpenForInput;
        int slot = ParseKey(keyValue);
        if (!_records!.TryGetValue(slot, out var rec))
            return FileStatus.RecordNotFound; // 23 — no record at the relative key (INVALID KEY)
        _currentRecord = slot;
        CopyToBuffer(rec, recordBuffer);
        return FileStatus.Success;
    }

    public string Write(byte[] recordData)
    {
        // WRITE on a file connector not open in the correct mode is status 48 (ISO §9.1.13.7).
        if (!IsOpen) return FileStatus.WriteNotOpenForOutput;
        if (_openMode == FileOpenMode.Input)
            return FileStatus.WriteNotOpenForOutput;

        int slot;
        if (SequentialAccess)
        {
            // §14.9.51 GR (sequential access): the operating environment assigns the next ascending
            // relative record number (OUTPUT starts at 1; EXTEND at highest+1). If that number needs
            // more significant digits than the RELATIVE KEY data item holds, status 24.
            slot = NextSequentialSlot();
            if (_relativeKeyDigits > 0 && slot >= Pow10(_relativeKeyDigits))
                return FileStatus.BoundaryViolation; // 24
        }
        else
        {
            // §14.9.51 GR (random/dynamic access): the program sets the relative key. A key below 1
            // is status 34; a key naming an already-occupied slot is the invalid-key duplicate, 22.
            slot = _pendingKey;
            if (slot < 1) return FileStatus.SequentialBoundaryViolation; // 34
            if (_records!.ContainsKey(slot))
                return FileStatus.DuplicateKey; // 22 (INVALID KEY)
        }
        _records![slot] = ToSlot(recordData);
        _currentRecord = slot;
        return FileStatus.Success;
    }

    private int NextSequentialSlot()
    {
        int max = 0;
        foreach (var k in _records!.Keys) if (k > max) max = k;
        return max + 1;
    }

    public string Rewrite(byte[] recordData)
    {
        // REWRITE on a file connector not open in I-O mode is status 49 (ISO §9.1.13.7).
        if (!IsOpen || _openMode != FileOpenMode.InputOutput)
            return FileStatus.DeleteRewriteNotOpenForIO;
        int slot = SequentialAccess ? _currentRecord : _pendingKey;
        if (SequentialAccess && _currentRecord == 0)
            return FileStatus.NoSuccessfulReadBeforeDeleteRewrite; // 43
        if (slot < 1 || !_records!.ContainsKey(slot))
            return FileStatus.RecordNotFound; // 23 — no record at the slot (INVALID KEY)
        _records[slot] = ToSlot(recordData);
        return FileStatus.Success;
    }

    public string Delete()
    {
        // DELETE on a file connector not open in I-O mode is status 49 (ISO §9.1.13.7).
        if (!IsOpen || _openMode != FileOpenMode.InputOutput)
            return FileStatus.DeleteRewriteNotOpenForIO;
        int slot = SequentialAccess ? _currentRecord : _pendingKey;
        if (SequentialAccess && _currentRecord == 0)
            return FileStatus.NoSuccessfulReadBeforeDeleteRewrite; // 43
        if (slot < 1 || !_records!.Remove(slot))
            return FileStatus.RecordNotFound; // 23 — no record at the slot (INVALID KEY)
        return FileStatus.Success;
    }

    /// <summary>Relative records are fixed-length — variable write is an ordinary write.</summary>
    public string WriteVariable(byte[] recordData) => Write(recordData);

    public string Start(byte[] keyValue, StartCondition condition)
    {
        if (!IsOpen) return FileStatus.ReadNotOpenForInput;
        int target = ParseKey(keyValue);
        // Position so the next READ NEXT returns the first record satisfying the condition.
        foreach (var slot in _records!.Keys)
        {
            bool matches = condition switch
            {
                StartCondition.Equal => slot == target,
                StartCondition.GreaterThan => slot > target,
                StartCondition.GreaterThanOrEqual => slot >= target,
                StartCondition.LessThan => slot < target,
                StartCondition.LessThanOrEqual => slot <= target,
                _ => false
            };
            if (matches) { _currentRecord = slot - 1; return FileStatus.Success; }
        }
        return FileStatus.RecordNotFound; // 23
    }

    private int ParseKey(byte[] keyValue)
    {
        string text = Encoding.ASCII.GetString(keyValue).Trim();
        return int.TryParse(text, out int n) ? n : 0;
    }

    private void CopyToBuffer(byte[] rec, byte[] buffer)
    {
        int n = Math.Min(rec.Length, buffer.Length);
        Array.Copy(rec, 0, buffer, 0, n);
        if (n < buffer.Length) Array.Fill(buffer, (byte)' ', n, buffer.Length - n);
    }

    /// <summary>Normalize a record to the file's fixed slot size (space-pad or truncate).</summary>
    private byte[] ToSlot(byte[] recordData)
    {
        byte[] slot = new byte[_recordLength];
        if (recordData.Length < _recordLength) Array.Fill(slot, (byte)' ');
        Array.Copy(recordData, 0, slot, 0, Math.Min(recordData.Length, _recordLength));
        return slot;
    }

    private static long Pow10(int n)
    {
        long v = 1;
        for (int i = 0; i < n; i++) v *= 10;
        return v;
    }

    public void Dispose()
    {
        if (IsOpen) Close();
        GC.SuppressFinalize(this);
    }
}
