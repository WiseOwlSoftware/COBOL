// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace CobolSharp.Runtime;

/// <summary>
/// Holds backing storage for a COBOL program's data divisions.
/// Pure data holder — no behavior. Helpers are in StorageHelpers.
/// </summary>
public sealed class ProgramState
{
    public byte[] WorkingStorage { get; }
    public byte[] FileSection { get; }

    public ProgramState(int wsSize, int fileSize)
    {
        WorkingStorage = new byte[wsSize];
        FileSection = new byte[fileSize];

        // COBOL default: fill with spaces (DISPLAY items)
        Array.Fill(WorkingStorage, (byte)' ');
        Array.Fill(FileSection, (byte)' ');
    }
}

/// <summary>
/// Static helpers for COBOL data movement and file I/O.
/// The CIL emitter calls these directly with byte[] + offset + size.
/// Later, PicRuntime-aware versions will replace the byte-copy helpers.
/// </summary>
public static class StorageHelpers
{
    /// <summary>
    /// MOVE "literal" TO field. Left-justified, space-padded.
    /// </summary>
    public static void MoveStringToField(byte[] area, int offset, int size, string value)
    {
        int copyLen = Math.Min(value.Length, size);
        for (int i = 0; i < size; i++)
            area[offset + i] = i < copyLen ? (byte)value[i] : (byte)' ';
    }

    /// <summary>
    /// MOVE field TO field. Left-justified, space-padded (alphanumeric).
    /// </summary>
    public static void MoveFieldToField(
        byte[] dest, int destOffset, int destSize,
        byte[] src, int srcOffset, int srcSize)
    {
        int copyLen = Math.Min(srcSize, destSize);
        Array.Copy(src, srcOffset, dest, destOffset, copyLen);
        for (int i = copyLen; i < destSize; i++)
            dest[destOffset + i] = (byte)' ';
    }

    /// <summary>
    /// Read a field as a trimmed ASCII string. Used by DISPLAY.
    /// </summary>
    public static string ReadFieldAsString(byte[] area, int offset, int size)
    {
        return Encoding.ASCII.GetString(area, offset, size).TrimEnd();
    }

    /// <summary>
    /// WRITE record: write record bytes to file.
    /// Routes through FileRuntime.WriteRecord which handles both binary (data files)
    /// and text (PRINT-FILE) modes.
    /// </summary>
    public static void WriteRecordToFile(string fileName, byte[] area, int offset, int size)
    {
        FileRuntime.WriteRecord(fileName, area, offset, size);
    }

    /// <summary>
    /// READ: read next record from file into storage area.
    /// Returns true if a record was read, false if at end-of-file.
    /// </summary>
    public static bool ReadRecordFromFile(string fileName, byte[] area, int offset, int size)
    {
        return FileRuntime.ReadRecord(fileName, area, offset, size);
    }

    /// <summary>
    /// Compare a field's bytes (as ASCII) to a string literal.
    /// Returns -1/0/1 like string.Compare. Trailing spaces ignored (COBOL semantics).
    /// </summary>
    public static int CompareFieldToString(byte[] area, int offset, int length, string value)
    {
        var field = Encoding.ASCII.GetString(area, offset, length).TrimEnd();
        var trimmedValue = value.TrimEnd();
        return string.Compare(field, trimmedValue, StringComparison.Ordinal);
    }
}
