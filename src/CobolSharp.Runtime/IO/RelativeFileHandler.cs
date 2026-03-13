namespace CobolSharp.Runtime.IO;

/// <summary>
/// Relative file handler for COBOL ORGANIZATION IS RELATIVE.
/// Records are accessed by relative record number (1-based).
/// Stored as fixed-length records in a flat file.
/// </summary>
public class RelativeFileHandler : IFileHandler
{
    private readonly int _recordLength;
    private FileStream? _stream;
    private int _currentRecord; // 1-based current position
    private FileOpenMode _openMode;

    public string ExternalName { get; }
    public bool IsOpen => _stream != null;

    public RelativeFileHandler(string externalName, int recordLength)
    {
        ExternalName = externalName;
        _recordLength = recordLength;
    }

    public string Open(FileOpenMode mode)
    {
        if (IsOpen) return FileStatus.FileAlreadyOpen;
        _openMode = mode;
        _currentRecord = 0;

        try
        {
            _stream = mode switch
            {
                FileOpenMode.Input => new FileStream(ExternalName, FileMode.Open, FileAccess.Read),
                FileOpenMode.Output => new FileStream(ExternalName, FileMode.Create, FileAccess.Write),
                FileOpenMode.InputOutput => new FileStream(ExternalName, FileMode.OpenOrCreate, FileAccess.ReadWrite),
                FileOpenMode.Extend => new FileStream(ExternalName, FileMode.Append, FileAccess.Write),
                _ => null
            };
            return _stream != null ? FileStatus.Success : FileStatus.PermanentError;
        }
        catch (FileNotFoundException)
        {
            return FileStatus.FileNotFound;
        }
        catch (IOException)
        {
            return FileStatus.PermanentError;
        }
    }

    public string Close()
    {
        if (!IsOpen) return FileStatus.FileNotOpen;
        _stream!.Dispose();
        _stream = null;
        return FileStatus.Success;
    }

    public string ReadNext(byte[] recordBuffer)
    {
        if (!IsOpen) return FileStatus.FileNotOpen;
        _currentRecord++;
        return ReadRecord(_currentRecord, recordBuffer);
    }

    public string ReadByKey(byte[] recordBuffer, byte[] keyValue)
    {
        if (!IsOpen) return FileStatus.FileNotOpen;
        int recordNum = BitConverter.ToInt32(keyValue, 0);
        _currentRecord = recordNum;
        return ReadRecord(recordNum, recordBuffer);
    }

    public string Write(byte[] recordData)
    {
        if (!IsOpen) return FileStatus.FileNotOpen;
        try
        {
            _stream!.Write(recordData, 0, _recordLength);
            return FileStatus.Success;
        }
        catch (IOException) { return FileStatus.PermanentError; }
    }

    public string Rewrite(byte[] recordData)
    {
        if (!IsOpen || _currentRecord == 0) return FileStatus.FileNotOpen;
        try
        {
            _stream!.Seek((long)(_currentRecord - 1) * _recordLength, SeekOrigin.Begin);
            _stream.Write(recordData, 0, _recordLength);
            return FileStatus.Success;
        }
        catch (IOException) { return FileStatus.PermanentError; }
    }

    public string Delete()
    {
        // Mark record as deleted by filling with high-values
        if (!IsOpen || _currentRecord == 0) return FileStatus.FileNotOpen;
        try
        {
            byte[] deleted = new byte[_recordLength];
            Array.Fill(deleted, (byte)0xFF);
            _stream!.Seek((long)(_currentRecord - 1) * _recordLength, SeekOrigin.Begin);
            _stream.Write(deleted, 0, _recordLength);
            return FileStatus.Success;
        }
        catch (IOException) { return FileStatus.PermanentError; }
    }

    public string Start(byte[] keyValue, StartCondition condition) =>
        FileStatus.PermanentError; // Not meaningful for relative files

    private string ReadRecord(int recordNum, byte[] buffer)
    {
        try
        {
            long offset = (long)(recordNum - 1) * _recordLength;
            if (offset >= _stream!.Length) return FileStatus.AtEnd;
            _stream.Seek(offset, SeekOrigin.Begin);
            int read = _stream.Read(buffer, 0, _recordLength);
            if (read < _recordLength)
                Array.Fill(buffer, (byte)' ', read, _recordLength - read);
            return FileStatus.Success;
        }
        catch (IOException) { return FileStatus.PermanentError; }
    }

    public void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }
}
