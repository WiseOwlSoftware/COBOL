namespace CobolSharp.Runtime;

/// <summary>
/// Backing storage for a COBOL record or working-storage section.
/// Each 01-level group gets its own StorageArea with byte-accurate layout.
/// Fields are accessed via offset + size.
/// </summary>
public sealed class StorageArea
{
    public byte[] Bytes { get; }
    public int Size => Bytes.Length;

    public StorageArea(int size)
    {
        Bytes = new byte[size];
        // COBOL default: fill with spaces (DISPLAY items)
        Array.Fill(Bytes, (byte)' ');
    }

    /// <summary>
    /// Get a span for a specific field within this storage area.
    /// </summary>
    public Span<byte> GetFieldSpan(int offset, int size)
        => new Span<byte>(Bytes, offset, size);

    /// <summary>
    /// Get a read-only span for the entire record.
    /// </summary>
    public ReadOnlySpan<byte> AsReadOnlySpan()
        => new ReadOnlySpan<byte>(Bytes);

    /// <summary>
    /// Move a string value into a field (alphanumeric MOVE).
    /// Left-justified, space-padded.
    /// </summary>
    public void MoveString(int offset, int size, string value)
    {
        var span = GetFieldSpan(offset, size);
        span.Fill((byte)' ');
        int copyLen = Math.Min(value.Length, size);
        for (int i = 0; i < copyLen; i++)
            span[i] = (byte)value[i];
    }

    /// <summary>
    /// Read a field as a trimmed ASCII string.
    /// </summary>
    public string ReadString(int offset, int size)
    {
        return System.Text.Encoding.ASCII.GetString(Bytes, offset, size).TrimEnd();
    }
}

/// <summary>
/// Holds all storage areas for a running COBOL program.
/// One area per 01-level record/group in WORKING-STORAGE, FILE SECTION, etc.
/// </summary>
public sealed class ProgramState
{
    private readonly Dictionary<string, StorageArea> _areas =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Allocate or get a named storage area.
    /// </summary>
    public StorageArea GetOrCreate(string name, int size)
    {
        if (!_areas.TryGetValue(name, out var area))
        {
            area = new StorageArea(size);
            _areas[name] = area;
        }
        return area;
    }

    /// <summary>
    /// Get an existing storage area by name.
    /// </summary>
    public StorageArea? Get(string name)
        => _areas.TryGetValue(name, out var area) ? area : null;

    /// <summary>
    /// Move a string literal into a named field.
    /// Used by the emitter for MOVE "literal" TO field.
    /// </summary>
    public static void MoveStringToField(
        byte[] dest, int destOffset, int destSize, string value)
    {
        int copyLen = Math.Min(value.Length, destSize);
        for (int i = 0; i < destSize; i++)
            dest[i + destOffset] = i < copyLen ? (byte)value[i] : (byte)' ';
    }

    /// <summary>
    /// Move bytes from one field to another (alphanumeric MOVE).
    /// </summary>
    public static void MoveFieldToField(
        byte[] dest, int destOffset, int destSize,
        byte[] src, int srcOffset, int srcSize)
    {
        int copyLen = Math.Min(srcSize, destSize);
        Array.Copy(src, srcOffset, dest, destOffset, copyLen);
        // Pad with spaces
        for (int i = copyLen; i < destSize; i++)
            dest[destOffset + i] = (byte)' ';
    }
}
