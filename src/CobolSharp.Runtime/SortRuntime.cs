// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Runtime;

/// <summary>
/// Runtime support for COBOL SORT and MERGE statements.
/// Manages in-memory sort files: records are collected via Release,
/// sorted by key fields, then returned in order via Return.
///
/// LIMITATION: Current implementation is entirely in-memory. All records are held in a
/// List&lt;byte[]&gt; and sorted via LINQ OrderBy (which is stable). This works for NIST test
/// datasets but will fail with OutOfMemoryException on large production files.
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
    /// keysSpec format: "offset,length,asc,isNumeric,usage,isSigned,signStorage,fractionDigits,totalDigits,leadingScale,trailingScale;..."
    /// Legacy format "offset,length,asc;..." is also accepted (treated as alphanumeric byte comparison).
    /// </summary>
    public static void SortRecords(string fileName, string keysSpec)
    {
        var keys = ParseKeysSpec(keysSpec);
        SortRecordsInternal(fileName, keys);
    }

    private static void SortRecordsInternal(string fileName, SortKeySpec[] keys)
    {
        if (!_sortFiles.TryGetValue(fileName, out var sf))
            throw new InvalidOperationException($"SORT file '{fileName}' not initialized");

        // Use LINQ OrderBy/ThenBy which is guaranteed stable (preserves original order for equal keys).
        // This is required by COBOL's WITH DUPLICATES IN ORDER semantics.
        if (keys.Length == 0)
        {
            sf.ReturnIndex = 0;
            return;
        }

        IOrderedEnumerable<byte[]> ordered;
        var firstKey = keys[0];
        if (firstKey.IsAscending)
            ordered = sf.Records.OrderBy(r => r, new SortKeyComparer(firstKey));
        else
            ordered = sf.Records.OrderByDescending(r => r, new SortKeyComparer(firstKey));

        for (int k = 1; k < keys.Length; k++)
        {
            var key = keys[k];
            if (key.IsAscending)
                ordered = ordered.ThenBy(r => r, new SortKeyComparer(key));
            else
                ordered = ordered.ThenByDescending(r => r, new SortKeyComparer(key));
        }

        // Materialize before clearing — LINQ is lazy, so the source list must survive enumeration
        var sorted = ordered.ToList();
        sf.Records.Clear();
        sf.Records.AddRange(sorted);
        sf.ReturnIndex = 0;
    }

    /// <summary>
    /// Merge multiple already-sorted input streams.
    /// inputFileNames: semicolon-delimited file names.
    /// keysSpec format: "offset,length,asc,isNumeric,usage,isSigned,signStorage,fractionDigits,totalDigits,leadingScale,trailingScale;..."
    /// </summary>
    public static void MergeRecords(string mergeFileName, string inputFileNamesStr, string keysSpec)
    {
        var inputFileNames = inputFileNamesStr.Split(';');
        var keys = ParseKeysSpec(keysSpec);
        MergeRecordsInternal(mergeFileName, inputFileNames, keys);
    }

    private static void MergeRecordsInternal(string mergeFileName, string[] inputFileNames, SortKeySpec[] keys)
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

        // Sort merged records by keys (stable sort)
        if (keys.Length == 0)
        {
            sf.ReturnIndex = 0;
            return;
        }

        IOrderedEnumerable<byte[]> ordered;
        var firstKey = keys[0];
        if (firstKey.IsAscending)
            ordered = sf.Records.OrderBy(r => r, new SortKeyComparer(firstKey));
        else
            ordered = sf.Records.OrderByDescending(r => r, new SortKeyComparer(firstKey));

        for (int k = 1; k < keys.Length; k++)
        {
            var key = keys[k];
            if (key.IsAscending)
                ordered = ordered.ThenBy(r => r, new SortKeyComparer(key));
            else
                ordered = ordered.ThenByDescending(r => r, new SortKeyComparer(key));
        }

        // Materialize before clearing — LINQ is lazy, so the source list must survive enumeration
        var sorted = ordered.ToList();
        sf.Records.Clear();
        sf.Records.AddRange(sorted);
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

    private static SortKeySpec[] ParseKeysSpec(string keysSpec)
    {
        var parts = keysSpec.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var keys = new SortKeySpec[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            var fields = parts[i].Split(',');
            int offset = int.Parse(fields[0]);
            int length = int.Parse(fields[1]);
            bool ascending = fields[2] == "1";

            if (fields.Length >= 11)
            {
                // Extended format with PIC info
                bool isNumeric = fields[3] == "1";
                var usage = (UsageKind)int.Parse(fields[4]);
                bool isSigned = fields[5] == "1";
                var signStorage = (SignStorageKind)int.Parse(fields[6]);
                int fractionDigits = int.Parse(fields[7]);
                int totalDigits = int.Parse(fields[8]);
                int leadingScale = int.Parse(fields[9]);
                int trailingScale = int.Parse(fields[10]);

                PicDescriptor? pic = null;
                if (isNumeric)
                {
                    pic = new PicDescriptor(
                        totalDigits: totalDigits,
                        fractionDigits: fractionDigits,
                        isSigned: isSigned,
                        isNumeric: true,
                        isAlphanumeric: false,
                        hasEditing: false,
                        storageLength: length,
                        usage: usage,
                        category: CobolCategory.Numeric,
                        signStorage: signStorage,
                        editing: EditingKind.None,
                        blankWhenZero: false,
                        leadingScaleDigits: leadingScale,
                        trailingScaleDigits: trailingScale);
                }

                keys[i] = new SortKeySpec(offset, length, ascending, isNumeric, pic);
            }
            else
            {
                // Legacy 3-field format: treat as alphanumeric byte comparison
                keys[i] = new SortKeySpec(offset, length, ascending, false, null);
            }
        }
        return keys;
    }

    /// <summary>Comparer for a single sort key, used with LINQ OrderBy/ThenBy.</summary>
    private sealed class SortKeyComparer(SortKeySpec key) : IComparer<byte[]>
    {
        public int Compare(byte[]? a, byte[]? b)
        {
            if (a is null && b is null) return 0;
            if (a is null) return -1;
            if (b is null) return 1;

            if (key.IsNumeric && key.Pic != null)
            {
                // Decode numeric values and compare as decimals
                decimal valA = PicRuntime.DecodeNumeric(a, key.Offset, key.Length, key.Pic);
                decimal valB = PicRuntime.DecodeNumeric(b, key.Offset, key.Length, key.Pic);
                return valA.CompareTo(valB);
            }

            // Alphanumeric: unsigned byte-by-byte comparison (EBCDIC/ASCII collating sequence)
            return CompareBytes(a, b, key.Offset, key.Length);
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

    /// <summary>Parsed sort key specification including PIC info for numeric keys.</summary>
    private sealed class SortKeySpec(int offset, int length, bool isAscending, bool isNumeric, PicDescriptor? pic)
    {
        public int Offset { get; } = offset;
        public int Length { get; } = length;
        public bool IsAscending { get; } = isAscending;
        public bool IsNumeric { get; } = isNumeric;
        public PicDescriptor? Pic { get; } = pic;
    }

    private sealed class SortFile(int recordLength)
    {
        public int RecordLength { get; } = recordLength;
        public List<byte[]> Records { get; } = [];
        public int ReturnIndex { get; set; }
    }
}
