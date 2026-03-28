// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
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

    /// <summary>When true (SELECT OPTIONAL), OPEN INPUT on a missing file returns "05" instead of "35".</summary>
    public bool IsOptional { get; set; }

    /// <summary>LINAGE body line count (0 = no LINAGE clause).</summary>
    public int LinageBody { get; set; }
    /// <summary>LINAGE FOOTING line (0 = no footing).</summary>
    public int LinageFooting { get; set; }
    /// <summary>LINAGE LINES AT TOP (default 0).</summary>
    public int LinageTop { get; set; }
    /// <summary>LINAGE LINES AT BOTTOM (default 0).</summary>
    public int LinageBottom { get; set; }
    /// <summary>Current LINAGE-COUNTER value (1-based line within current page body).</summary>
    public int LinageCounter { get; set; }

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
                        return IsOptional ? FileStatus.OptionalFileNotFound : FileStatus.FileNotFound;
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
            // Initialize LINAGE-COUNTER for files with LINAGE clause
            if (LinageBody > 0)
                LinageCounter = 1;
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
        if (_openMode == FileOpenMode.Output || _openMode == FileOpenMode.Extend)
            return FileStatus.ReadNotOpenForInput;

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
        if (_openMode == FileOpenMode.Input)
            return FileStatus.WriteNotOpenForOutput;

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
        if (_openMode != FileOpenMode.InputOutput)
            return FileStatus.DeleteRewriteNotOpenForIO;

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

    /// <summary>
    /// Write raw text directly to the underlying stream without record formatting.
    /// Used for WRITE AFTER ADVANCING (print-control semantics: newlines BEFORE text).
    /// </summary>
    public void WriteRawText(string text)
    {
        if (_lineSequential && _writer != null)
            _writer.Write(text);
        else if (_stream != null)
        {
            var bytes = Encoding.ASCII.GetBytes(text);
            _stream.Write(bytes, 0, bytes.Length);
        }
    }

    /// <summary>
    /// After a WRITE with LINAGE active, indicates whether the END-OF-PAGE condition was triggered.
    /// The END-OF-PAGE condition is raised when LINAGE-COUNTER equals or exceeds the FOOTING value.
    /// </summary>
    public bool EndOfPage { get; private set; }

    /// <summary>
    /// Advance the LINAGE-COUNTER by the given number of lines.
    /// Returns true if END-OF-PAGE was triggered (counter crossed footing line).
    /// </summary>
    public bool AdvanceLinageCounter(int lines)
    {
        EndOfPage = false;
        if (LinageBody <= 0) return false;

        LinageCounter += lines;
        if (LinageFooting > 0 && LinageCounter >= LinageFooting)
            EndOfPage = true;

        if (LinageCounter > LinageBody)
        {
            // Page overflow — emit bottom margin + top margin blank lines, reset counter
            LinageCounter = 1;
            EndOfPage = true;
        }
        return EndOfPage;
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
