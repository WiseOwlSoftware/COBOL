namespace CobolSharp.Runtime.IO;

/// <summary>
/// Manages file handlers for a COBOL program. Each file declared in
/// the FILE-CONTROL paragraph gets a handler registered here.
/// </summary>
public class CobolFileManager : IDisposable
{
    private readonly Dictionary<string, IFileHandler> _handlers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Register a file handler for a COBOL file name.</summary>
    public void RegisterFile(string cobolFileName, IFileHandler handler)
    {
        _handlers[cobolFileName] = handler;
    }

    /// <summary>Get the handler for a COBOL file name.</summary>
    public IFileHandler? GetHandler(string cobolFileName)
    {
        _handlers.TryGetValue(cobolFileName, out var handler);
        return handler;
    }

    /// <summary>Open a file by its COBOL name.</summary>
    public string Open(string cobolFileName, FileOpenMode mode)
    {
        var handler = GetHandler(cobolFileName);
        if (handler == null) return FileStatus.FileNotFound;
        return handler.Open(mode);
    }

    /// <summary>Close a file by its COBOL name.</summary>
    public string Close(string cobolFileName)
    {
        var handler = GetHandler(cobolFileName);
        if (handler == null) return FileStatus.FileNotOpen;
        return handler.Close();
    }

    /// <summary>Read the next record from a file.</summary>
    public string ReadNext(string cobolFileName, byte[] recordBuffer)
    {
        var handler = GetHandler(cobolFileName);
        if (handler == null) return FileStatus.FileNotOpen;
        return handler.ReadNext(recordBuffer);
    }

    /// <summary>Write a record to a file.</summary>
    public string Write(string cobolFileName, byte[] recordData)
    {
        var handler = GetHandler(cobolFileName);
        if (handler == null) return FileStatus.FileNotOpen;
        return handler.Write(recordData);
    }

    /// <summary>Rewrite the current record.</summary>
    public string Rewrite(string cobolFileName, byte[] recordData)
    {
        var handler = GetHandler(cobolFileName);
        if (handler == null) return FileStatus.FileNotOpen;
        return handler.Rewrite(recordData);
    }

    /// <summary>Delete the current record.</summary>
    public string Delete(string cobolFileName)
    {
        var handler = GetHandler(cobolFileName);
        if (handler == null) return FileStatus.FileNotOpen;
        return handler.Delete();
    }

    public void Dispose()
    {
        foreach (var handler in _handlers.Values)
            handler.Dispose();
        _handlers.Clear();
        GC.SuppressFinalize(this);
    }
}
