namespace CobolSharp.Runtime;

/// <summary>
/// Holds backing storage for a COBOL program's data divisions.
/// WorkingStorage and FileSection are flat byte arrays.
/// Each field is accessed via offset + size from RecordLayout.
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

    /// <summary>
    /// Move a string literal into a field. Left-justified, space-padded.
    /// Called by emitter for: MOVE "literal" TO field
    /// </summary>
    public static void MoveStringToField(byte[] area, int offset, int size, string value)
    {
        int copyLen = Math.Min(value.Length, size);
        for (int i = 0; i < size; i++)
            area[offset + i] = i < copyLen ? (byte)value[i] : (byte)' ';
    }

    /// <summary>
    /// Move bytes from one field to another. Left-justified, space-padded.
    /// Called by emitter for: MOVE alpha-field TO alpha-field
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
    /// Read a field as a trimmed ASCII string.
    /// Called by emitter for: DISPLAY field
    /// </summary>
    public static string ReadFieldAsString(byte[] area, int offset, int size)
    {
        return System.Text.Encoding.ASCII.GetString(area, offset, size).TrimEnd();
    }

    /// <summary>
    /// Write a record's bytes as ASCII to a file.
    /// Called by emitter for: WRITE record
    /// </summary>
    public static void WriteRecordToFile(string fileName, byte[] area, int offset, int size)
    {
        string text = System.Text.Encoding.ASCII.GetString(area, offset, size).TrimEnd();
        FileRuntime.WriteText(fileName, text);
    }
}
