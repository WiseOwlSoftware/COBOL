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
    private SortedDictionary<string, byte[]>? _records;
    private IEnumerator<KeyValuePair<string, byte[]>>? _enumerator;
    private string? _currentKey;
    private string? _dataFilePath;
    private FileOpenMode _openMode;

    public string ExternalName { get; }
    public bool IsOpen => _records != null;

    public IndexedFileHandler(string externalName, int recordLength, int keyOffset, int keyLength)
    {
        ExternalName = externalName;
        _recordLength = recordLength;
        _keyOffset = keyOffset;
        _keyLength = keyLength;
    }

    public string Open(FileOpenMode mode)
    {
        if (IsOpen) return FileStatus.FileAlreadyOpen;

        _openMode = mode;
        _dataFilePath = ExternalName;
        _records = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);

        if (mode == FileOpenMode.Input || mode == FileOpenMode.InputOutput)
        {
            if (!File.Exists(_dataFilePath))
                return mode == FileOpenMode.Input ? FileStatus.FileNotFound : FileStatus.Success;

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
        _enumerator = null;
        _currentKey = null;
        return FileStatus.Success;
    }

    public string ReadNext(byte[] recordBuffer)
    {
        if (!IsOpen || _enumerator == null) return FileStatus.FileNotOpen;

        if (!_enumerator.MoveNext())
            return FileStatus.AtEnd;

        var record = _enumerator.Current.Value;
        Array.Copy(record, recordBuffer, Math.Min(record.Length, recordBuffer.Length));
        _currentKey = _enumerator.Current.Key;
        return FileStatus.Success;
    }

    public string ReadByKey(byte[] recordBuffer, byte[] keyValue)
    {
        if (!IsOpen) return FileStatus.FileNotOpen;

        string key = Encoding.ASCII.GetString(keyValue).TrimEnd();
        if (!_records!.TryGetValue(key, out var record))
            return FileStatus.RecordNotFound;

        Array.Copy(record, recordBuffer, Math.Min(record.Length, recordBuffer.Length));
        _currentKey = key;
        return FileStatus.Success;
    }

    public string Write(byte[] recordData)
    {
        if (!IsOpen) return FileStatus.FileNotOpen;

        string key = ExtractKey(recordData);
        if (_records!.ContainsKey(key))
            return FileStatus.DuplicateKey;

        _records[key] = (byte[])recordData.Clone();
        return FileStatus.Success;
    }

    public string Rewrite(byte[] recordData)
    {
        if (!IsOpen || _currentKey == null) return FileStatus.FileNotOpen;

        string newKey = ExtractKey(recordData);
        if (newKey != _currentKey)
            return FileStatus.KeyOutOfSequence; // Key cannot change on rewrite

        _records![_currentKey] = (byte[])recordData.Clone();
        return FileStatus.Success;
    }

    public string Delete()
    {
        if (!IsOpen || _currentKey == null) return FileStatus.FileNotOpen;

        _records!.Remove(_currentKey);
        _currentKey = null;
        return FileStatus.Success;
    }

    public string Start(byte[] keyValue, StartCondition condition)
    {
        if (!IsOpen) return FileStatus.FileNotOpen;

        string targetKey = Encoding.ASCII.GetString(keyValue).TrimEnd();

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

        // Position enumerator at all records from firstKey onward.
        // ReadNext will call MoveNext to get the first record.
        _enumerator = _records
            .Where(r => string.Compare(r.Key, firstKey, StringComparison.Ordinal) >= 0)
            .GetEnumerator();
        _currentKey = firstKey;
        return FileStatus.Success;
    }

    private string ExtractKey(byte[] record)
    {
        return Encoding.ASCII.GetString(record, _keyOffset, _keyLength).TrimEnd();
    }

    private void ResetEnumerator()
    {
        _enumerator = _records?.GetEnumerator();
    }

    public void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }
}
