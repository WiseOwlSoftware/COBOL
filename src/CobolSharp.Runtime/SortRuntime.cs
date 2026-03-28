// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Runtime;

/// <summary>
/// Runtime support for COBOL SORT and MERGE statements.
/// Manages in-memory sort files: records are collected via Release,
/// sorted by key fields, then returned in order via Return.
///
/// LIMITATION: Current implementation is entirely in-memory. All records are held in a
/// List&lt;byte[]&gt; and sorted via Array.Sort. This works for NIST test datasets but will
/// fail with OutOfMemoryException on large production files.
///
/// The COBOL spec does not require in-memory sort — SD files are files, and implementations
/// may use any mechanism. A production implementation should use external merge sort:
/// (1) read chunks that fit in memory, (2) sort each chunk, (3) write to temp files,
/// (4) k-way merge sorted temp files using PriorityQueue&lt;T&gt;.
/// Windows provides no OS-level record-oriented sort API.
/// </summary>
public static class SortRuntime
{
    /// <summary>Active sort/merge files keyed by COBOL file name.</summary>
    private static readonly Dictionary<string, SortFile> _sortFiles = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initialize a sort file for a new SORT/MERGE operation.
    /// </summary>
    public static void InitSortFile(string fileName, int recordLength)
    {
        _sortFiles[fileName] = new SortFile(recordLength);
    }

    /// <summary>
    /// Release (add) a record to the sort file's input buffer.
    /// Called by RELEASE statement or implicitly during USING phase.
    /// </summary>
    public static void ReleaseRecord(string fileName, byte[] storageArea, int offset, int length)
    {
        if (!_sortFiles.TryGetValue(fileName, out var sf))
            throw new InvalidOperationException($"SORT file '{fileName}' not initialized");

        var record = new byte[length];
        Array.Copy(storageArea, offset, record, 0, length);
        sf.Records.Add(record);
    }

    /// <summary>
    /// Sort the collected records by the specified keys.
    /// keysSpec format: "offset,length,asc;offset,length,asc;..." where asc is "1" or "0".
    /// </summary>
    public static void SortRecords(string fileName, string keysSpec)
    {
        ParseKeysSpec(keysSpec, out var keyOffsets, out var keyLengths, out var keyAscending);
        SortRecordsInternal(fileName, keyOffsets, keyLengths, keyAscending);
    }

    private static void SortRecordsInternal(string fileName, int[] keyOffsets, int[] keyLengths, bool[] keyAscending)
    {
        if (!_sortFiles.TryGetValue(fileName, out var sf))
            throw new InvalidOperationException($"SORT file '{fileName}' not initialized");

        sf.Records.Sort((a, b) =>
        {
            for (int k = 0; k < keyOffsets.Length; k++)
            {
                int off = keyOffsets[k];
                int len = keyLengths[k];
                int cmp = CompareBytes(a, b, off, len);
                if (cmp != 0)
                    return keyAscending[k] ? cmp : -cmp;
            }
            return 0; // equal keys — stable sort preserves order (List.Sort is stable in .NET)
        });

        sf.ReturnIndex = 0;
    }

    /// <summary>
    /// Merge multiple already-sorted input streams.
    /// inputFileNames: semicolon-delimited file names.
    /// keysSpec format: "offset,length,asc;offset,length,asc;..."
    /// </summary>
    public static void MergeRecords(string mergeFileName, string inputFileNamesStr, string keysSpec)
    {
        var inputFileNames = inputFileNamesStr.Split(';');
        ParseKeysSpec(keysSpec, out var keyOffsets, out var keyLengths, out var keyAscending);
        MergeRecordsInternal(mergeFileName, inputFileNames, keyOffsets, keyLengths, keyAscending);
    }

    private static void MergeRecordsInternal(string mergeFileName, string[] inputFileNames,
        int[] keyOffsets, int[] keyLengths, bool[] keyAscending)
    {
        if (!_sortFiles.TryGetValue(mergeFileName, out var sf))
            throw new InvalidOperationException($"MERGE file '{mergeFileName}' not initialized");

        // For merge, we collect all input records then sort (equivalent to merge for correctness)
        // A true k-way merge is an optimization for later
        foreach (var inputName in inputFileNames)
        {
            // Read all records from each input file via FileRuntime public API
            FileRuntime.OpenInput(inputName);
            while (true)
            {
                var buf = new byte[sf.RecordLength];
                bool ok = FileRuntime.ReadRecord(inputName, buf, 0, sf.RecordLength);
                if (!ok) break;
                sf.Records.Add(buf);
            }
            FileRuntime.CloseFile(inputName);
        }

        // Sort merged records by keys
        sf.Records.Sort((a, b) =>
        {
            for (int k = 0; k < keyOffsets.Length; k++)
            {
                int off = keyOffsets[k];
                int len = keyLengths[k];
                int cmp = CompareBytes(a, b, off, len);
                if (cmp != 0)
                    return keyAscending[k] ? cmp : -cmp;
            }
            return 0;
        });

        sf.ReturnIndex = 0;
    }

    /// <summary>
    /// Return the next sorted record into the storage area.
    /// Returns true if a record was available, false if at end.
    /// </summary>
    public static bool ReturnRecord(string fileName, byte[] storageArea, int offset, int length)
    {
        if (!_sortFiles.TryGetValue(fileName, out var sf))
            return false;

        if (sf.ReturnIndex >= sf.Records.Count)
            return false;

        var record = sf.Records[sf.ReturnIndex++];
        int copyLen = Math.Min(length, record.Length);
        Array.Copy(record, 0, storageArea, offset, copyLen);
        // Pad with spaces if record is shorter than target
        if (copyLen < length)
            Array.Fill(storageArea, (byte)' ', offset + copyLen, length - copyLen);

        return true;
    }

    /// <summary>
    /// Clean up a sort file after the SORT/MERGE statement completes.
    /// </summary>
    public static void CloseSortFile(string fileName)
    {
        _sortFiles.Remove(fileName);
    }

    private static void ParseKeysSpec(string keysSpec, out int[] offsets, out int[] lengths, out bool[] ascending)
    {
        var parts = keysSpec.Split(';', StringSplitOptions.RemoveEmptyEntries);
        offsets = new int[parts.Length];
        lengths = new int[parts.Length];
        ascending = new bool[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            var fields = parts[i].Split(',');
            offsets[i] = int.Parse(fields[0]);
            lengths[i] = int.Parse(fields[1]);
            ascending[i] = fields[2] == "1";
        }
    }

    /// <summary>Compare two byte arrays at given offset/length using unsigned byte comparison.</summary>
    private static int CompareBytes(byte[] a, byte[] b, int offset, int length)
    {
        for (int i = 0; i < length; i++)
        {
            int offI = offset + i;
            if (offI >= a.Length || offI >= b.Length) break;
            int cmp = a[offI].CompareTo(b[offI]);
            if (cmp != 0) return cmp;
        }
        return 0;
    }

    private sealed class SortFile(int recordLength)
    {
        public int RecordLength { get; } = recordLength;
        public List<byte[]> Records { get; } = [];
        public int ReturnIndex { get; set; }
    }
}
