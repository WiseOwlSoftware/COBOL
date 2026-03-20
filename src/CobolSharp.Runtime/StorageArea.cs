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
    /// MOVE "literal" TO JUSTIFIED RIGHT field. Right-justified, left-padded/left-truncated.
    /// ISO §13.16.35: when source > target, truncate from the left (keep rightmost chars).
    /// </summary>
    public static void MoveStringToJustifiedField(byte[] area, int offset, int size, string value)
    {
        if (value.Length > size)
        {
            int skipLeft = value.Length - size;
            for (int i = 0; i < size; i++)
                area[offset + i] = (byte)value[skipLeft + i];
        }
        else
        {
            int pad = size - value.Length;
            for (int i = 0; i < pad; i++)
                area[offset + i] = (byte)' ';
            for (int i = 0; i < value.Length; i++)
                area[offset + pad + i] = (byte)value[i];
        }
    }

    /// <summary>
    /// Initialize all occurrences of an OCCURS field with the same VALUE.
    /// Replicates the value into each element slot: baseOffset, baseOffset+elementSize, etc.
    /// For non-OCCURS items (occursCount=1), behaves identically to MoveStringToField.
    /// </summary>
    public static void MoveStringToOccursField(
        byte[] area, int baseOffset, int elementSize, int occursCount, string value)
    {
        int copyLen = Math.Min(value.Length, elementSize);

        for (int occ = 0; occ < occursCount; occ++)
        {
            int offset = baseOffset + occ * elementSize;
            for (int i = 0; i < elementSize; i++)
                area[offset + i] = i < copyLen ? (byte)value[i] : (byte)' ';
        }
    }

    /// <summary>
    /// MOVE string TO alphanumeric-edited field. Applies edit pattern:
    /// A/X = data position (takes next input character), B = space, 0 = zero, / = slash.
    /// </summary>
    public static void MoveStringToEditedField(byte[] area, int offset, int size,
        string value, string editPattern)
    {
        int srcIdx = 0;
        for (int i = 0; i < editPattern.Length && i < size; i++)
        {
            char editChar = editPattern[i];
            switch (editChar)
            {
                case 'A':
                case 'X':
                    area[offset + i] = srcIdx < value.Length ? (byte)value[srcIdx++] : (byte)' ';
                    break;
                case 'B':
                    area[offset + i] = (byte)' ';
                    break;
                case '0':
                    area[offset + i] = (byte)'0';
                    break;
                case '/':
                    area[offset + i] = (byte)'/';
                    break;
                default:
                    area[offset + i] = srcIdx < value.Length ? (byte)value[srcIdx++] : (byte)' ';
                    break;
            }
        }
        for (int i = editPattern.Length; i < size; i++)
            area[offset + i] = (byte)' ';
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
    /// Read a field as a raw ASCII string WITHOUT trimming. Used by INSPECT
    /// where trailing spaces in patterns are significant.
    /// </summary>
    public static string ReadFieldAsRawString(byte[] area, int offset, int size)
    {
        return Encoding.ASCII.GetString(area, offset, size);
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
    /// Compare a field's bytes to a string literal.
    /// Returns -1/0/1 like string.Compare. Trailing spaces ignored (COBOL semantics).
    /// Uses Latin1 encoding to preserve the full byte range 0x00-0xFF
    /// (ASCII only handles 0x00-0x7F, replacing 0x80-0xFF with '?').
    /// </summary>
    public static int CompareFieldToString(byte[] area, int offset, int length, string value)
    {
        var field = Encoding.Latin1.GetString(area, offset, length).TrimEnd();
        var trimmedValue = value.TrimEnd();
        return string.Compare(field, trimmedValue, StringComparison.Ordinal);
    }

    // ── STRING runtime ──

    /// <summary>
    /// STRING statement runtime: concatenate one sending value into dest at the given pointer.
    /// Pointer is 1-based. Returns false if all characters fit, true if overflow occurred.
    /// On return, pointer is updated to position after the last character written + 1.
    /// </summary>
    public static bool StringConcat(
        byte[] destArea, int destOffset, int destLength,
        byte[] srcArea, int srcOffset, int srcLength,
        string? delimiter, bool delimitedBySize,
        ref int pointer)
    {
        int index = pointer - 1; // convert to 0-based within dest window

        // Compute effective source length
        int effectiveLength;
        if (delimitedBySize || delimiter == null)
        {
            effectiveLength = srcLength;
        }
        else
        {
            effectiveLength = FindDelimitedLength(srcArea, srcOffset, srcLength, delimiter);
        }

        bool overflow = false;
        for (int j = 0; j < effectiveLength; j++)
        {
            if (index >= destLength)
            {
                overflow = true;
                break;
            }
            destArea[destOffset + index] = srcArea[srcOffset + j];
            index++;
        }

        pointer = index + 1; // back to 1-based
        return overflow;
    }

    /// <summary>
    /// STRING statement runtime for literal sending values.
    /// </summary>
    public static bool StringConcatLiteral(
        byte[] destArea, int destOffset, int destLength,
        string value,
        string? delimiter, bool delimitedBySize,
        ref int pointer)
    {
        int index = pointer - 1;

        string effectiveValue;
        if (delimitedBySize || delimiter == null)
        {
            effectiveValue = value;
        }
        else
        {
            int delimPos = value.IndexOf(delimiter, StringComparison.Ordinal);
            effectiveValue = delimPos >= 0 ? value[..delimPos] : value;
        }

        bool overflow = false;
        for (int j = 0; j < effectiveValue.Length; j++)
        {
            if (index >= destLength)
            {
                overflow = true;
                break;
            }
            destArea[destOffset + index] = (byte)effectiveValue[j];
            index++;
        }

        pointer = index + 1;
        return overflow;
    }

    private static int FindDelimitedLength(byte[] area, int offset, int length, string delimiter)
    {
        byte[] delimBytes = Encoding.ASCII.GetBytes(delimiter);
        int delimLen = delimBytes.Length;

        for (int i = 0; i <= length - delimLen; i++)
        {
            bool match = true;
            for (int k = 0; k < delimLen; k++)
            {
                if (area[offset + i + k] != delimBytes[k])
                {
                    match = false;
                    break;
                }
            }
            if (match) return i;
        }

        return length;
    }

    // ── UNSTRING runtime ──

    /// <summary>
    /// UNSTRING statement runtime: extract ONE field from the source at the current pointer position.
    /// Scans from pointer for the delimiter, copies the portion before it into dest (space-padded),
    /// advances pointer past the delimiter, and returns the count of characters extracted.
    /// If the source is exhausted before this call, sets overflow=true and returns 0.
    /// Pointer is 1-based and updated in place.
    /// </summary>
    public static int UnstringExtract(
        byte[] srcArea, int srcOffset, int srcLength,
        byte[] destArea, int destOffset, int destLength,
        string? delimiter, bool delimitedByAll,
        byte[]? delimOutArea, int delimOutOffset, int delimOutLength,
        ref int pointer,
        ref bool overflow)
    {
        int pos = pointer - 1; // convert to 0-based within source window

        // Source already exhausted — overflow
        if (pos >= srcLength)
        {
            overflow = true;
            // Space-fill destination
            for (int i = 0; i < destLength; i++)
                destArea[destOffset + i] = (byte)' ';
            return 0;
        }

        int extractLen;
        int delimLen = 0;
        string matchedDelim = "";

        if (delimiter == null)
        {
            // No delimiter — take the entire remaining source
            extractLen = srcLength - pos;
        }
        else
        {
            byte[] delimBytes = Encoding.ASCII.GetBytes(delimiter);
            delimLen = delimBytes.Length;

            // Scan for delimiter starting from current position
            int found = -1;
            for (int i = pos; i <= srcLength - delimLen; i++)
            {
                bool match = true;
                for (int k = 0; k < delimLen; k++)
                {
                    if (srcArea[srcOffset + i + k] != delimBytes[k])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    found = i;
                    break;
                }
            }

            if (found >= 0)
            {
                extractLen = found - pos;
                matchedDelim = delimiter;

                // For ALL: skip consecutive occurrences of the delimiter
                if (delimitedByAll)
                {
                    int skipPos = found + delimLen;
                    while (skipPos + delimLen <= srcLength)
                    {
                        bool match = true;
                        for (int k = 0; k < delimLen; k++)
                        {
                            if (srcArea[srcOffset + skipPos + k] != delimBytes[k])
                            {
                                match = false;
                                break;
                            }
                        }
                        if (!match) break;
                        skipPos += delimLen;
                    }
                    delimLen = skipPos - found; // total delimiter bytes consumed
                }
            }
            else
            {
                // No delimiter found — take remaining source
                extractLen = srcLength - pos;
                delimLen = 0;
                matchedDelim = "";
            }
        }

        // Copy extracted characters into destination, space-pad
        int copyLen = Math.Min(extractLen, destLength);
        for (int i = 0; i < destLength; i++)
        {
            if (i < copyLen)
                destArea[destOffset + i] = srcArea[srcOffset + pos + i];
            else
                destArea[destOffset + i] = (byte)' ';
        }

        // Write matched delimiter to DELIMITER IN field (if present)
        if (delimOutArea != null)
        {
            int delimCopy = Math.Min(matchedDelim.Length, delimOutLength);
            for (int i = 0; i < delimOutLength; i++)
            {
                if (i < delimCopy)
                    delimOutArea[delimOutOffset + i] = (byte)matchedDelim[i];
                else
                    delimOutArea[delimOutOffset + i] = (byte)' ';
            }
        }

        // Advance pointer past extracted data + delimiter
        pointer = pos + extractLen + delimLen + 1; // back to 1-based

        return extractLen;
    }

    /// <summary>
    /// Compare two fields as alphanumeric (ASCII string comparison).
    /// Returns -1/0/1 like string.Compare. Trailing spaces ignored (COBOL semantics).
    /// </summary>
    public static int CompareFieldToField(
        byte[] leftArea, int leftOffset, int leftLength,
        byte[] rightArea, int rightOffset, int rightLength)
    {
        var left = Encoding.Latin1.GetString(leftArea, leftOffset, leftLength).TrimEnd();
        var right = Encoding.Latin1.GetString(rightArea, rightOffset, rightLength).TrimEnd();
        return string.Compare(left, right, StringComparison.Ordinal);
    }
}
