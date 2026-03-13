using System.Text;

namespace CobolSharp.Runtime.IO;

/// <summary>
/// Sequential file handler for COBOL ORGANIZATION IS SEQUENTIAL.
/// Supports fixed-length and line-sequential record formats.
/// </summary>
public class SequentialFileHandler : IFileHandler
{
    private readonly int _recordLength;
    private readonly bool _lineSequential;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private FileStream? _stream;
    private FileOpenMode _openMode;

    public string ExternalName { get; }
    public bool IsOpen => _stream != null || _reader != null || _writer != null;

    public SequentialFileHandler(string externalName, int recordLength, bool lineSequential = false)
    {
        ExternalName = externalName;
        _recordLength = recordLength;
        _lineSequential = lineSequential;
    }

    public string Open(FileOpenMode mode)
    {
        if (IsOpen) return FileStatus.FileAlreadyOpen;

        _openMode = mode;
        try
        {
            switch (mode)
            {
                case FileOpenMode.Input:
                    if (!File.Exists(ExternalName))
                        return FileStatus.FileNotFound;
                    if (_lineSequential)
                        _reader = new StreamReader(ExternalName, Encoding.ASCII);
                    else
                        _stream = new FileStream(ExternalName, FileMode.Open, FileAccess.Read);
                    break;

                case FileOpenMode.Output:
                    if (_lineSequential)
                        _writer = new StreamWriter(ExternalName, false, Encoding.ASCII);
                    else
                        _stream = new FileStream(ExternalName, FileMode.Create, FileAccess.Write);
                    break;

                case FileOpenMode.Extend:
                    if (_lineSequential)
                        _writer = new StreamWriter(ExternalName, true, Encoding.ASCII);
                    else
                        _stream = new FileStream(ExternalName, FileMode.Append, FileAccess.Write);
                    break;

                case FileOpenMode.InputOutput:
                    _stream = new FileStream(ExternalName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                    break;
            }
            return FileStatus.Success;
        }
        catch (UnauthorizedAccessException)
        {
            return FileStatus.PermissionDenied;
        }
        catch (IOException)
        {
            return FileStatus.PermanentError;
        }
    }

    public string Close()
    {
        if (!IsOpen) return FileStatus.FileNotOpen;
        _reader?.Dispose();
        _writer?.Dispose();
        _stream?.Dispose();
        _reader = null;
        _writer = null;
        _stream = null;
        return FileStatus.Success;
    }

    public string ReadNext(byte[] recordBuffer)
    {
        if (!IsOpen) return FileStatus.FileNotOpen;

        try
        {
            if (_lineSequential && _reader != null)
            {
                string? line = _reader.ReadLine();
                if (line == null) return FileStatus.AtEnd;

                // Pad or truncate to record length
                Array.Fill(recordBuffer, (byte)' ');
                byte[] lineBytes = Encoding.ASCII.GetBytes(line);
                Array.Copy(lineBytes, 0, recordBuffer, 0, Math.Min(lineBytes.Length, recordBuffer.Length));
                return FileStatus.Success;
            }

            if (_stream != null)
            {
                int bytesRead = _stream.Read(recordBuffer, 0, _recordLength);
                if (bytesRead == 0) return FileStatus.AtEnd;
                if (bytesRead < _recordLength)
                {
                    // Pad remaining with spaces
                    Array.Fill(recordBuffer, (byte)' ', bytesRead, _recordLength - bytesRead);
                }
                return FileStatus.Success;
            }

            return FileStatus.PermanentError;
        }
        catch (IOException)
        {
            return FileStatus.PermanentError;
        }
    }

    public string ReadByKey(byte[] recordBuffer, byte[] keyValue) =>
        FileStatus.PermanentError; // Not supported for sequential files

    public string Write(byte[] recordData)
    {
        if (!IsOpen) return FileStatus.FileNotOpen;

        try
        {
            if (_lineSequential && _writer != null)
            {
                string line = Encoding.ASCII.GetString(recordData).TrimEnd();
                _writer.WriteLine(line);
                return FileStatus.Success;
            }

            if (_stream != null)
            {
                _stream.Write(recordData, 0, _recordLength);
                return FileStatus.Success;
            }

            return FileStatus.PermanentError;
        }
        catch (IOException)
        {
            return FileStatus.PermanentError;
        }
    }

    public string Rewrite(byte[] recordData)
    {
        // For sequential files, rewrite replaces the last-read record
        if (!IsOpen || _stream == null) return FileStatus.FileNotOpen;

        try
        {
            _stream.Seek(-_recordLength, SeekOrigin.Current);
            _stream.Write(recordData, 0, _recordLength);
            return FileStatus.Success;
        }
        catch (IOException)
        {
            return FileStatus.PermanentError;
        }
    }

    public string Delete() => FileStatus.PermanentError; // Not supported for sequential files

    public string Start(byte[] keyValue, StartCondition condition) =>
        FileStatus.PermanentError; // Not supported for sequential files

    public void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }
}
