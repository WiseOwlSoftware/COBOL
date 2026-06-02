// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace CobolSharp.Runtime.IO;

/// <summary>
/// Indexed file handler for COBOL ORGANIZATION IS INDEXED.
/// Uses a SortedDictionary as an in-memory B+ tree equivalent.
/// Records are persisted to a flat file with a separate index file.
/// Phase 4 implementation — sufficient for correctness testing.
/// </summary>
public class IndexedFileHandler : IFileHandler
{
    private readonly int _recordLength;
    private readonly int _keyOffset;
    private readonly int _keyLength;
    private readonly List<AlternateKeyDescriptor> _alternateKeys = [];
    private SortedDictionary<string, byte[]>? _records;
    private readonly List<SortedDictionary<string, List<byte[]>>> _alternateIndices = [];
    // Sequential position is tracked by key, not a live enumerator: a SortedDictionary enumerator is
    // invalidated the moment an interleaved WRITE/REWRITE/DELETE mutates the collection (the common
    // DYNAMIC READ-NEXT-with-positioned-write pattern), which threw "Collection was modified". Each
    // READ NEXT re-derives the next key from _records, so it is robust to mutation between reads.
    private string? _currentKey;     // key of the last record returned (null = positioned before the first)
    private bool _readNextInclusive; // true after START: the next READ NEXT returns the record AT _currentKey, not after it
    private bool _pastEnd;           // true after a READ NEXT hit AT END (drives ReadPrevious's first-back behavior)
    // ACCESS SEQUENTIAL: DELETE/REWRITE act on the last-read record and require an immediately preceding
    // successful READ (ISO §9.1.13.6). _prevOpWasSuccessfulRead is true only right after a successful
    // READ; any WRITE/REWRITE/DELETE/START clears it, so a sequential DELETE/REWRITE not directly after a
    // READ returns 43. _lastReadUnsuccessful drives the 46 on a sequential READ after an at-end READ
    // (ISO §14.9.30 GR). RANDOM/DYNAMIC access ignores both (it positions by key).
    public bool SequentialAccess { get; set; } = true;
    private bool _prevOpWasSuccessfulRead;
    private bool _lastReadUnsuccessful;
    private string? _dataFilePath;
    private FileOpenMode _openMode;

    public string ExternalName { get; }
    public bool IsOpen => _records != null;

    /// <summary>Indexed records are fixed-length; the record length is constant.</summary>
    public int LastRecordLength => _recordLength;

    /// <summary>Indexed records are fixed-length — variable write is an ordinary write.</summary>
    public string WriteVariable(byte[] recordData) => Write(recordData);

    /// <summary>When true (SELECT OPTIONAL), OPEN INPUT on a missing file returns "05" instead of "35".</summary>
    public bool IsOptional { get; set; }

    public IndexedFileHandler(string externalName, int recordLength, int keyOffset, int keyLength)
    {
        ExternalName = externalName;
        _recordLength = recordLength;
        _keyOffset = keyOffset;
        _keyLength = keyLength;
    }

    /// <summary>Register an alternate key for this indexed file.</summary>
    public void AddAlternateKey(int keyOffset, int keyLength, bool allowDuplicates)
    {
        _alternateKeys.Add(new AlternateKeyDescriptor(keyOffset, keyLength, allowDuplicates));
    }

    internal sealed record AlternateKeyDescriptor(int Offset, int Length, bool AllowDuplicates);

    public string Open(FileOpenMode mode)
    {
        if (IsOpen) return FileStatus.FileAlreadyOpen;

        _openMode = mode;
        _dataFilePath = ExternalName;
        _records = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);

        // Initialize alternate indices
        _alternateIndices.Clear();
        foreach (var _ in _alternateKeys)
            _alternateIndices.Add(new SortedDictionary<string, List<byte[]>>(StringComparer.Ordinal));

        if (mode == FileOpenMode.Input || mode == FileOpenMode.InputOutput)
        {
            if (!File.Exists(_dataFilePath))
            {
                if (mode == FileOpenMode.Input)
                    return IsOptional ? FileStatus.OptionalFileNotFound : FileStatus.FileNotFound;
                return FileStatus.Success;
            }

            // Load all records from file
            try
            {
                using var stream = new FileStream(_dataFilePath, FileMode.Open, FileAccess.Read);
                byte[] buffer = new byte[_recordLength];
                while (stream.Read(buffer, 0, _recordLength) == _recordLength)
                {
                    string key = ExtractKey(buffer);
                    _records[key] = (byte[])buffer.Clone();
                    IndexAlternateKeys(buffer);
                }
            }
            catch (IOException)
            {
                return FileStatus.PermanentError;
            }
        }

        ResetEnumerator();
        return FileStatus.Success;
    }

    public string Close()
    {
        if (!IsOpen) return FileStatus.FileNotOpen;

        // Persist to file
        if (_openMode != FileOpenMode.Input && _dataFilePath != null)
        {
            try
            {
                using var stream = new FileStream(_dataFilePath, FileMode.Create, FileAccess.Write);
                foreach (var record in _records!.Values)
                {
                    stream.Write(record, 0, _recordLength);
                }
            }
            catch (IOException)
            {
                return FileStatus.PermanentError;
            }
        }

        _records = null;
        _currentKey = null;
        _readNextInclusive = false;
        _pastEnd = false;
        _prevOpWasSuccessfulRead = false;
        _lastReadUnsuccessful = false;
        return FileStatus.Success;
    }

    public string ReadNext(byte[] recordBuffer)
    {
        // READ on a file connector not open in input/I-O mode is status 47 (ISO §9.1.13.7), not 42.
        if (!IsOpen || _records == null) return FileStatus.ReadNotOpenForInput;
        if (_openMode == FileOpenMode.Output || _openMode == FileOpenMode.Extend)
            return FileStatus.ReadNotOpenForInput;

        // A sequential READ NEXT after an unsuccessful (at-end) READ with no intervening reposition is
        // itself unsuccessful — no valid next record — status 46 (ISO §14.9.30 GR). A START/READ-by-key
        // repositions and clears the flag.
        if (_lastReadUnsuccessful)
            return FileStatus.NoValidNextRecord;

        // Find the next record in key order: the smallest key > _currentKey (or >= when positioned by
        // START), or the smallest key overall when positioned before the first. Re-derived from _records
        // each call so an interleaved WRITE/REWRITE/DELETE never corrupts the position (no live enumerator).
        string? target = null;
        foreach (var k in _records.Keys)
        {
            if (_currentKey == null)
            {
                target = k; break;
            }
            int cmp = string.Compare(k, _currentKey, StringComparison.Ordinal);
            if (cmp > 0 || (_readNextInclusive && cmp == 0))
            {
                target = k; break;
            }
        }
        _readNextInclusive = false;

        if (target == null)
        {
            _pastEnd = true;
            _lastReadUnsuccessful = true;
            _prevOpWasSuccessfulRead = false;
            return FileStatus.AtEnd;
        }

        var record = _records[target];
        Array.Copy(record, recordBuffer, Math.Min(record.Length, recordBuffer.Length));
        _currentKey = target;
        _pastEnd = false;
        _prevOpWasSuccessfulRead = true;
        return HasDuplicateAlternateKey(record) ? FileStatus.DuplicateAlternateKey : FileStatus.Success;
    }

    /// <summary>
    /// Read the previous record (reverse sequential access for DYNAMIC mode).
    /// </summary>
    public string ReadPrevious(byte[] recordBuffer)
    {
        if (!IsOpen) return FileStatus.ReadNotOpenForInput;
        if (_openMode == FileOpenMode.Output || _openMode == FileOpenMode.Extend)
            return FileStatus.ReadNotOpenForInput;

        if (_currentKey == null)
        {
            // No current position — start from the last record
            var lastEntry = _records!.LastOrDefault();
            if (lastEntry.Key == null) return FileStatus.AtEnd;
            Array.Copy(lastEntry.Value, recordBuffer, Math.Min(lastEntry.Value.Length, recordBuffer.Length));
            _currentKey = lastEntry.Key;
            _pastEnd = false;
            return HasDuplicateAlternateKey(lastEntry.Value) ? FileStatus.DuplicateAlternateKey : FileStatus.Success;
        }

        // If we just hit AT END on a forward read, the current key still points to the
        // last successfully read record. Return that record first, then go backward.
        if (_pastEnd)
        {
            _pastEnd = false;
            if (_records!.TryGetValue(_currentKey, out var currentRecord))
            {
                Array.Copy(currentRecord, recordBuffer, Math.Min(currentRecord.Length, recordBuffer.Length));
                return HasDuplicateAlternateKey(currentRecord) ? FileStatus.DuplicateAlternateKey : FileStatus.Success;
            }
            return FileStatus.AtEnd;
        }

        // Find the record before _currentKey
        string? prevKey = null;
        byte[]? prevRecord = null;
        foreach (var entry in _records!)
        {
            if (string.Compare(entry.Key, _currentKey, StringComparison.Ordinal) >= 0)
                break;
            prevKey = entry.Key;
            prevRecord = entry.Value;
        }

        if (prevKey == null || prevRecord == null)
            return FileStatus.AtEnd;

        Array.Copy(prevRecord, recordBuffer, Math.Min(prevRecord.Length, recordBuffer.Length));
        _currentKey = prevKey;
        return HasDuplicateAlternateKey(prevRecord) ? FileStatus.DuplicateAlternateKey : FileStatus.Success;
    }

    public string ReadByKey(byte[] recordBuffer, byte[] keyValue)
        => ReadByKey(recordBuffer, keyValue, keyIndex: -1);

    /// <summary>
    /// Read by key with optional alternate key index.
    /// keyIndex = -1 means primary key; 0+ means alternate key index.
    /// </summary>
    public string ReadByKey(byte[] recordBuffer, byte[] keyValue, int keyIndex)
    {
        if (!IsOpen) return FileStatus.ReadNotOpenForInput;
        if (_openMode == FileOpenMode.Output || _openMode == FileOpenMode.Extend)
            return FileStatus.ReadNotOpenForInput;

        string key = Encoding.ASCII.GetString(keyValue);

        if (keyIndex < 0)
        {
            // Primary key lookup
            if (!_records!.TryGetValue(key, out var record))
                return FileStatus.RecordNotFound;

            Array.Copy(record, recordBuffer, Math.Min(record.Length, recordBuffer.Length));
            _currentKey = ExtractKey(record);
            _readNextInclusive = false; // a following READ NEXT continues from the record after this one
            _pastEnd = false;
            _prevOpWasSuccessfulRead = true; // a keyed READ satisfies the sequential DELETE/REWRITE prerequisite
            _lastReadUnsuccessful = false;
            return HasDuplicateAlternateKey(record) ? FileStatus.DuplicateAlternateKey : FileStatus.Success;
        }

        // Alternate key lookup
        if (keyIndex >= _alternateIndices.Count)
            return FileStatus.RecordNotFound;

        if (!_alternateIndices[keyIndex].TryGetValue(key, out var records) || records.Count == 0)
            return FileStatus.RecordNotFound;

        var found = records[0]; // First matching record
        Array.Copy(found, recordBuffer, Math.Min(found.Length, recordBuffer.Length));
        _currentKey = ExtractKey(found);
        _readNextInclusive = false; // a following READ NEXT continues from the record after this one
        _pastEnd = false;
        return HasDuplicateAlternateKey(found) ? FileStatus.DuplicateAlternateKey : FileStatus.Success;
    }

    public string Write(byte[] recordData)
    {
        // WRITE on a file connector not open in the correct mode is status 48 (ISO §9.1.13.7), not 42.
        if (!IsOpen) return FileStatus.WriteNotOpenForOutput;
        if (_openMode == FileOpenMode.Input)
            return FileStatus.WriteNotOpenForOutput;

        string key = ExtractKey(recordData);
        if (_records!.ContainsKey(key))
            return FileStatus.DuplicateKey;

        // Check alternate key uniqueness (for non-DUPLICATES keys)
        for (int i = 0; i < _alternateKeys.Count; i++)
        {
            if (!_alternateKeys[i].AllowDuplicates)
            {
                string altKey = ExtractAlternateKey(recordData, i);
                if (_alternateIndices[i].ContainsKey(altKey))
                    return FileStatus.DuplicateKey;
            }
        }

        // Check if any alternate key with DUPLICATES allowed already has a matching value.
        // If so, the WRITE succeeds but returns status "02" (duplicate alternate key exists).
        bool hasDuplicateAlt = false;
        for (int i = 0; i < _alternateKeys.Count; i++)
        {
            if (_alternateKeys[i].AllowDuplicates)
            {
                string altKey = ExtractAlternateKey(recordData, i);
                if (_alternateIndices[i].TryGetValue(altKey, out var existing) && existing.Count > 0)
                    hasDuplicateAlt = true;
            }
        }

        _records[key] = (byte[])recordData.Clone();
        IndexAlternateKeys(recordData);
        _prevOpWasSuccessfulRead = false; // a WRITE is not a READ — a following sequential DELETE/REWRITE is 43
        return hasDuplicateAlt ? FileStatus.DuplicateAlternateKey : FileStatus.Success;
    }

    public string Rewrite(byte[] recordData)
    {
        // REWRITE on a file connector not open in I-O mode is status 49 (ISO §9.1.13.7), not 42.
        if (!IsOpen || _openMode != FileOpenMode.InputOutput)
            return FileStatus.DeleteRewriteNotOpenForIO;

        if (SequentialAccess)
        {
            // ACCESS SEQUENTIAL: REWRITE replaces the last-read record and requires the immediately
            // preceding operation to have been a successful READ (status 43 if not, ISO §9.1.13.6); the
            // primary key may not change (status 21, ISO §14.9.35).
            if (!_prevOpWasSuccessfulRead || _currentKey == null)
            {
                _prevOpWasSuccessfulRead = false;
                return FileStatus.NoSuccessfulReadBeforeDeleteRewrite;
            }
            string seqKey = ExtractKey(recordData);
            _prevOpWasSuccessfulRead = false; // the REWRITE consumes the read position
            if (seqKey != _currentKey)
                return FileStatus.KeyOutOfSequence;
            _records![_currentKey] = (byte[])recordData.Clone();
            return FileStatus.Success;
        }

        // ACCESS RANDOM/DYNAMIC: REWRITE the record identified by the primary key in the record area; no
        // prior read is required. The record must already exist (status 23 — invalid key — if not).
        string newKey = ExtractKey(recordData);
        if (!_records!.ContainsKey(newKey))
            return FileStatus.RecordNotFound;
        _records[newKey] = (byte[])recordData.Clone();
        _currentKey = newKey;
        return FileStatus.Success;
    }

    public string Delete()
    {
        // DELETE on a file connector not open in I-O mode is status 49 (ISO §9.1.13.7), not 42.
        if (!IsOpen || _openMode != FileOpenMode.InputOutput)
            return FileStatus.DeleteRewriteNotOpenForIO;

        if (SequentialAccess)
        {
            // ACCESS SEQUENTIAL: DELETE removes the last-read record and requires the immediately
            // preceding operation to have been a successful READ (status 43 if not, ISO §9.1.13.6).
            if (!_prevOpWasSuccessfulRead || _currentKey == null)
            {
                _prevOpWasSuccessfulRead = false;
                return FileStatus.NoSuccessfulReadBeforeDeleteRewrite;
            }
            _records!.Remove(_currentKey);
            _prevOpWasSuccessfulRead = false; // the DELETE consumes the read position
            return FileStatus.Success;
        }

        // ACCESS RANDOM/DYNAMIC: DELETE the record identified by the primary key (set into the RECORD KEY
        // data item before the statement); no prior read is required. Status 23 if no such record.
        if (_currentKey == null || !_records!.ContainsKey(_currentKey))
            return FileStatus.RecordNotFound;
        _records.Remove(_currentKey);
        return FileStatus.Success;
    }

    public string Start(byte[] keyValue, StartCondition condition)
    {
        // START on a file connector not open in input/I-O mode is status 47 (ISO §9.1.13.7), not 42.
        if (!IsOpen) return FileStatus.ReadNotOpenForInput;
        if (_openMode == FileOpenMode.Output || _openMode == FileOpenMode.Extend)
            return FileStatus.ReadNotOpenForInput;

        string targetKey = Encoding.ASCII.GetString(keyValue);

        // Find the first key that satisfies the condition
        string? firstKey = null;
        foreach (var entry in _records!)
        {
            int cmp = string.Compare(entry.Key, targetKey, StringComparison.Ordinal);
            bool matches = condition switch
            {
                StartCondition.Equal => cmp == 0,
                StartCondition.GreaterThan => cmp > 0,
                StartCondition.GreaterThanOrEqual => cmp >= 0,
                StartCondition.LessThan => cmp < 0,
                StartCondition.LessThanOrEqual => cmp <= 0,
                _ => false
            };
            if (matches)
            {
                firstKey = entry.Key;
                break;
            }
        }

        if (firstKey == null)
            return FileStatus.RecordNotFound;

        // Position so the next READ NEXT returns the record AT firstKey (START does not itself read —
        // ISO §14.9.41 GR8: it sets the file position indicator to the first record satisfying the relation).
        // START is not a READ, so it does not satisfy a sequential DELETE/REWRITE's read prerequisite, but
        // it does clear the at-end state (the position is re-established).
        _currentKey = firstKey;
        _readNextInclusive = true;
        _pastEnd = false;
        _prevOpWasSuccessfulRead = false;
        _lastReadUnsuccessful = false;
        return FileStatus.Success;
    }

    private string ExtractKey(byte[] record)
    {
        return Encoding.ASCII.GetString(record, _keyOffset, _keyLength);
    }

    private string ExtractAlternateKey(byte[] record, int altKeyIndex)
    {
        var desc = _alternateKeys[altKeyIndex];
        return Encoding.ASCII.GetString(record, desc.Offset, desc.Length);
    }

    private void IndexAlternateKeys(byte[] record)
    {
        for (int i = 0; i < _alternateKeys.Count; i++)
        {
            string altKey = ExtractAlternateKey(record, i);
            if (!_alternateIndices[i].TryGetValue(altKey, out var list))
            {
                list = [];
                _alternateIndices[i][altKey] = list;
            }
            list.Add((byte[])record.Clone());
        }
    }

    /// <summary>
    /// Check if any alternate key (with DUPLICATES) has more than one record for this record's alt key value.
    /// Returns true if status "02" should be returned (duplicate alternate key exists).
    /// </summary>
    private bool HasDuplicateAlternateKey(byte[] record)
    {
        for (int i = 0; i < _alternateKeys.Count; i++)
        {
            if (_alternateKeys[i].AllowDuplicates)
            {
                string altKey = ExtractAlternateKey(record, i);
                if (_alternateIndices[i].TryGetValue(altKey, out var list) && list.Count > 1)
                    return true;
            }
        }
        return false;
    }

    private void ResetEnumerator()
    {
        // Reset the sequential position to before the first record (OPEN INPUT/I-O establishes the
        // file position indicator at the first record; the first READ NEXT then returns it).
        _currentKey = null;
        _readNextInclusive = false;
        _pastEnd = false;
        _prevOpWasSuccessfulRead = false;
        _lastReadUnsuccessful = false;
    }

    public void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }
}
