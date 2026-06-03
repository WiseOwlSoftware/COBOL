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
    // The single source of truth: prime key -> record. All alternate-key views (lookup, duplicate checks,
    // alternate key-of-reference sequencing) are derived from this on demand, so a REWRITE/DELETE can never
    // leave a stale secondary index (an earlier clone-based _alternateIndices did).
    private SortedDictionary<string, byte[]>? _records;
    // Sequential position is tracked by key, not a live enumerator: a SortedDictionary enumerator is
    // invalidated the moment an interleaved WRITE/REWRITE/DELETE mutates the collection (the common
    // DYNAMIC READ-NEXT-with-positioned-write pattern), which threw "Collection was modified". Each
    // READ NEXT re-derives the next key from _records, so it is robust to mutation between reads.
    private string? _currentKey;     // PRIME key of the last record returned (null = positioned before the first)
    private string? _currentRefKey;  // its key-of-reference value, cached so the position survives a DELETE
                                     // of the current record (it is no longer in _records to re-extract from)
    private bool _readNextInclusive; // true after START: the next READ NEXT returns the record AT _currentKey, not after it
    private bool _pastEnd;           // true after a READ NEXT hit AT END (drives ReadPrevious's first-back behavior)
    // Key of reference (ISO §9.1.13): -1 = prime record key, 0+ = alternate record key index. Set by START
    // and by a keyed READ; it governs the ordering of a subsequent sequential READ NEXT — by prime key, or
    // by the chosen alternate key (ascending alt value, then prime key for records sharing an alt value).
    private int _keyOfReference = -1;
    // ACCESS SEQUENTIAL: DELETE/REWRITE act on the last-read record and require an immediately preceding
    // successful READ (ISO §9.1.13.6). _prevOpWasSuccessfulRead is true only right after a successful
    // READ; any WRITE/REWRITE/DELETE/START clears it, so a sequential DELETE/REWRITE not directly after a
    // READ returns 43. _lastReadUnsuccessful drives the 46 on a sequential READ after an at-end READ
    // (ISO §14.9.30 GR). RANDOM/DYNAMIC access ignores both (it positions by key).
    public bool SequentialAccess { get; set; } = true;
    private bool _prevOpWasSuccessfulRead;
    private bool _lastReadUnsuccessful;
    // ACCESS SEQUENTIAL WRITE releases records in ascending primary-key order; a key not greater than the
    // previously written record's key is the invalid-key condition, status 21 (ISO §14.9.51 GR). Tracks the
    // last successfully written key (reset at OPEN). RANDOM/DYNAMIC WRITE has no ordering requirement.
    private string? _lastWrittenKey;
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
        _lastWrittenKey = null; // ascending-order WRITE check restarts each OPEN

        if (mode == FileOpenMode.Input || mode == FileOpenMode.InputOutput || mode == FileOpenMode.Extend)
        {
            if (!File.Exists(_dataFilePath))
            {
                if (mode == FileOpenMode.Input)
                    return IsOptional ? FileStatus.OptionalFileNotFound : FileStatus.FileNotFound;
                if (mode == FileOpenMode.Extend)
                {
                    // EXTEND on a missing file: an optional file is created (status 05); a non-optional file
                    // is a permanent error (35, ISO §9.1.13.2). The new file starts empty.
                    if (!IsOptional) return FileStatus.FileNotFound;
                    ResetEnumerator();
                    return FileStatus.OptionalFileNotFound;
                }
                return FileStatus.Success; // I-O on a missing file: create empty
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
                }
            }
            catch (IOException)
            {
                return FileStatus.PermanentError;
            }

            // EXTEND positions after the last logical record — the highest prime key (ISO §14.9.30 GR15) —
            // so the ascending-order WRITE check (status 21) requires appended keys to exceed it.
            if (mode == FileOpenMode.Extend && _records.Count > 0)
                _lastWrittenKey = _records.Keys.Last();
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
        _currentRefKey = null;
        _readNextInclusive = false;
        _pastEnd = false;
        _prevOpWasSuccessfulRead = false;
        _lastReadUnsuccessful = false;
        _keyOfReference = -1;
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

        // Find the next record in KEY-OF-REFERENCE order. Re-derived from _records each call so an
        // interleaved WRITE/REWRITE/DELETE never corrupts the position (no live enumerator). Records are
        // ordered by the (reference-key value, prime key) tuple, so an alternate key of reference walks
        // alternate-key order and breaks ties between duplicate alternate keys by prime key.
        string? target;
        if (_readNextInclusive && _currentKey != null && _records.ContainsKey(_currentKey))
        {
            // START positioned the file at _currentKey; the next READ NEXT returns that very record.
            target = _currentKey;
        }
        else
        {
            // Current position tuple = (reference key of the last record, its prime key). Use the cached
            // reference value (_currentRefKey) so a DELETE of the current record — which removes it from
            // _records — does not lose the position and restart the scan from the beginning.
            string? curRef = _currentKey == null ? null : _currentRefKey;
            string? bestPrime = null, bestRef = null;
            foreach (var kv in _records)
            {
                string r = KeyForReference(kv.Value);
                string p = kv.Key;
                if (_currentKey != null)
                {
                    int c = string.Compare(r, curRef, StringComparison.Ordinal);
                    bool after = c > 0 || (c == 0 && string.Compare(p, _currentKey, StringComparison.Ordinal) > 0);
                    if (!after) continue;
                }
                if (bestRef == null)
                {
                    bestRef = r; bestPrime = p;
                }
                else
                {
                    int c = string.Compare(r, bestRef, StringComparison.Ordinal);
                    if (c < 0 || (c == 0 && string.Compare(p, bestPrime, StringComparison.Ordinal) < 0))
                    {
                        bestRef = r; bestPrime = p;
                    }
                }
            }
            target = bestPrime;
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
        _currentRefKey = KeyForReference(record);
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

    /// <summary>
    /// Read by key with optional alternate key index.
    /// keyIndex = -1 means primary key; 0+ means alternate key index.
    /// </summary>
    public string ReadByKey(byte[] recordBuffer, byte[] keyValue, int keyIndex = -1)
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
            _keyOfReference = -1; // a primary-key READ makes the prime key the key of reference
            _currentRefKey = _currentKey;
            _readNextInclusive = false; // a following READ NEXT continues from the record after this one
            _pastEnd = false;
            _prevOpWasSuccessfulRead = true; // a keyed READ satisfies the sequential DELETE/REWRITE prerequisite
            _lastReadUnsuccessful = false;
            return HasDuplicateAlternateKey(record) ? FileStatus.DuplicateAlternateKey : FileStatus.Success;
        }

        // Alternate key lookup. Derived from _records (not the write-time clone index, which a REWRITE/
        // DELETE would leave stale): the record whose alternate[keyIndex] value equals the search key, with
        // the smallest prime key among duplicates — consistent with the alternate key-of-reference READ NEXT
        // ordering, so READ … KEY then READ NEXT walk the same sequence.
        if (keyIndex >= _alternateKeys.Count)
            return FileStatus.RecordNotFound;

        string? foundPrime = null;
        byte[]? found = null;
        foreach (var kv in _records!)
        {
            if (ExtractAlternateKey(kv.Value, keyIndex) != key) continue;
            if (foundPrime == null || string.Compare(kv.Key, foundPrime, StringComparison.Ordinal) < 0)
            {
                foundPrime = kv.Key; found = kv.Value;
            }
        }
        if (found == null) return FileStatus.RecordNotFound;

        Array.Copy(found, recordBuffer, Math.Min(found.Length, recordBuffer.Length));
        _currentKey = foundPrime;
        _keyOfReference = keyIndex; // an alternate-key READ makes that alternate the key of reference
        _currentRefKey = key;       // the matched alternate value
        _readNextInclusive = false; // a following READ NEXT continues from the record after this one
        _pastEnd = false;
        _prevOpWasSuccessfulRead = true;
        _lastReadUnsuccessful = false;
        return HasDuplicateAlternateKey(found) ? FileStatus.DuplicateAlternateKey : FileStatus.Success;
    }

    public string Write(byte[] recordData)
    {
        // WRITE on a file connector not open in the correct mode is status 48 (ISO §9.1.13.7), not 42.
        if (!IsOpen) return FileStatus.WriteNotOpenForOutput;
        if (_openMode == FileOpenMode.Input)
            return FileStatus.WriteNotOpenForOutput;

        string key = ExtractKey(recordData);

        // ACCESS SEQUENTIAL: records must be released in ascending primary-key order; a key not greater
        // than the previously written one is the invalid-key condition, status 21 (ISO §14.9.51 GR). This
        // precedes the duplicate-key check because an equal key is also out-of-sequence in sequential access.
        if (SequentialAccess && _lastWrittenKey != null
            && string.Compare(key, _lastWrittenKey, StringComparison.Ordinal) <= 0)
            return FileStatus.KeyOutOfSequence;

        if (_records!.ContainsKey(key))
            return FileStatus.DuplicateKey;

        // Alternate key uniqueness: writing a value that already exists under an alternate key declared
        // WITHOUT DUPLICATES is the invalid-key condition, status 22 (ISO §14.9.51 GR). An alternate key
        // declared WITH DUPLICATES instead allows it but the WRITE completes with status 02.
        bool hasDuplicateAlt = false;
        for (int i = 0; i < _alternateKeys.Count; i++)
        {
            string altKey = ExtractAlternateKey(recordData, i);
            if (CountByAlternate(i, altKey, excludePrime: null) > 0)
            {
                if (!_alternateKeys[i].AllowDuplicates) return FileStatus.DuplicateKey;
                hasDuplicateAlt = true;
            }
        }

        _records[key] = (byte[])recordData.Clone();
        _lastWrittenKey = key; // for the ACCESS SEQUENTIAL ascending-order check (status 21)
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
            if (CheckRewriteAlternateKeys(recordData, _currentKey) is { } seqDup)
                return seqDup;
            _records![_currentKey] = (byte[])recordData.Clone();
            return FileStatus.Success;
        }

        // ACCESS RANDOM/DYNAMIC: REWRITE the record identified by the primary key in the record area; no
        // prior read is required. The record must already exist (status 23 — invalid key — if not).
        string newKey = ExtractKey(recordData);
        if (!_records!.ContainsKey(newKey))
            return FileStatus.RecordNotFound;
        if (CheckRewriteAlternateKeys(recordData, newKey) is { } dup)
            return dup;
        _records[newKey] = (byte[])recordData.Clone();
        _currentKey = newKey;
        return FileStatus.Success;
    }

    /// <summary>Validate alternate-key uniqueness for a REWRITE: a value that already exists on another
    /// record (excluding the one at <paramref name="primeKey"/> being replaced) under an alternate key
    /// declared WITHOUT DUPLICATES is the invalid-key condition, status 22 (ISO §14.9.35 GR). Returns the
    /// status to report, or null if the rewrite is permitted.</summary>
    private string? CheckRewriteAlternateKeys(byte[] recordData, string primeKey)
    {
        for (int i = 0; i < _alternateKeys.Count; i++)
        {
            if (_alternateKeys[i].AllowDuplicates) continue;
            string altKey = ExtractAlternateKey(recordData, i);
            if (CountByAlternate(i, altKey, excludePrime: primeKey) > 0)
                return FileStatus.DuplicateKey;
        }
        return null;
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

    public string Start(byte[] keyValue, StartCondition condition, int keyIndex = -1)
    {
        // START on a file connector not open in input/I-O mode is status 47 (ISO §9.1.13.7), not 42.
        if (!IsOpen) return FileStatus.ReadNotOpenForInput;
        if (_openMode == FileOpenMode.Output || _openMode == FileOpenMode.Extend)
            return FileStatus.ReadNotOpenForInput;

        // The START establishes the key of reference (prime or an alternate key, ISO §14.9.41); it governs
        // both the comparison here and the subsequent READ NEXT ordering. The search key value is the
        // current value of the named key data item, supplied by the caller.
        _keyOfReference = keyIndex >= 0 && keyIndex < _alternateKeys.Count ? keyIndex : -1;
        string targetKey = Encoding.ASCII.GetString(keyValue);

        // Find the record positioned by the relation, ordered by the (reference-key value, prime key) tuple:
        // the smallest such record satisfying the relation. For EQUAL/GREATER[-OR-EQUAL] that is the first
        // matching record in key-of-reference order; for LESS[-OR-EQUAL] it is the smallest matching record
        // (a subsequent ascending READ NEXT then proceeds from there).
        string? firstPrime = null, firstRef = null;
        foreach (var kv in _records!)
        {
            string r = KeyForReference(kv.Value);
            int cmp = string.Compare(r, targetKey, StringComparison.Ordinal);
            bool matches = condition switch
            {
                StartCondition.Equal => cmp == 0,
                StartCondition.GreaterThan => cmp > 0,
                StartCondition.GreaterThanOrEqual => cmp >= 0,
                StartCondition.LessThan => cmp < 0,
                StartCondition.LessThanOrEqual => cmp <= 0,
                _ => false
            };
            if (!matches) continue;
            if (firstRef == null)
            {
                firstRef = r; firstPrime = kv.Key;
            }
            else
            {
                int c = string.Compare(r, firstRef, StringComparison.Ordinal);
                if (c < 0 || (c == 0 && string.Compare(kv.Key, firstPrime, StringComparison.Ordinal) < 0))
                {
                    firstRef = r; firstPrime = kv.Key;
                }
            }
        }

        if (firstPrime == null)
        {
            _keyOfReference = -1;
            return FileStatus.RecordNotFound;
        }

        // Position so the next READ NEXT returns the record AT firstPrime (START does not itself read —
        // ISO §14.9.41 GR8: it sets the file position indicator to the first record satisfying the relation).
        // START is not a READ, so it does not satisfy a sequential DELETE/REWRITE's read prerequisite, but
        // it does clear the at-end state (the position is re-established).
        _currentKey = firstPrime;
        _currentRefKey = firstRef;
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

    /// <summary>The value of the current key of reference for a record: the prime key when
    /// <see cref="_keyOfReference"/> is -1, otherwise the indexed alternate key's value. Guards against a
    /// stale/out-of-range index by falling back to the prime key.</summary>
    private string KeyForReference(byte[] record)
        => _keyOfReference >= 0 && _keyOfReference < _alternateKeys.Count
            ? ExtractAlternateKey(record, _keyOfReference)
            : ExtractKey(record);

    /// <summary>Number of records whose alternate-key <paramref name="altKeyIndex"/> value equals
    /// <paramref name="altValue"/>, optionally excluding the record with prime key <paramref name="excludePrime"/>.
    /// Derived from <see cref="_records"/> so it reflects every WRITE/REWRITE/DELETE.</summary>
    private int CountByAlternate(int altKeyIndex, string altValue, string? excludePrime)
    {
        int n = 0;
        foreach (var kv in _records!)
        {
            if (excludePrime != null && string.Equals(kv.Key, excludePrime, StringComparison.Ordinal)) continue;
            if (ExtractAlternateKey(kv.Value, altKeyIndex) == altValue) n++;
        }
        return n;
    }

    /// <summary>
    /// True if a record returned by READ should carry status "02": some alternate key declared WITH
    /// DUPLICATES has another record sharing this record's value for it (ISO §14.9.30 — duplicate alternate
    /// key indicator). The record itself is already in <see cref="_records"/>, so it is excluded by prime key.
    /// </summary>
    private bool HasDuplicateAlternateKey(byte[] record)
    {
        string prime = ExtractKey(record);
        for (int i = 0; i < _alternateKeys.Count; i++)
        {
            if (!_alternateKeys[i].AllowDuplicates) continue;
            string altKey = ExtractAlternateKey(record, i);
            if (CountByAlternate(i, altKey, excludePrime: prime) >= 1)
                return true;
        }
        return false;
    }

    private void ResetEnumerator()
    {
        // Reset the sequential position to before the first record (OPEN INPUT/I-O establishes the
        // file position indicator at the first record; the first READ NEXT then returns it). The key of
        // reference defaults to the prime record key (ISO §9.1.13).
        _currentKey = null;
        _currentRefKey = null;
        _readNextInclusive = false;
        _pastEnd = false;
        _prevOpWasSuccessfulRead = false;
        _lastReadUnsuccessful = false;
        _keyOfReference = -1;
    }

    public void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }
}
