// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;
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
    public bool IsOpen => _stream != null || _reader != null || _writer != null || _optionalAbsentInput;

    // True after OPEN INPUT on a SELECT OPTIONAL file that was not present: the open is successful
    // (status 05) and the connector is positioned at end-of-file, so the first (and every) READ raises
    // the AT END condition with status 10 (ISO §9.1.13.2 — optional file not present on OPEN INPUT).
    // No host stream is opened; this flag keeps IsOpen true and routes READ to AT END.
    private bool _optionalAbsentInput;

    /// <summary>Character length of the most recently read record (for RECORD VARYING DEPENDING ON).</summary>
    public int LastRecordLength { get; private set; }

    /// <summary>True for a record-sequential file with variable-length records (RECORD IS VARYING or
    /// multiple 01 sizes). Each record is stored length-framed: a 4-byte little-endian length prefix
    /// followed by the data bytes. Fixed-length record-sequential files store contiguous fixed-size
    /// records. Has no effect on line-sequential files (which frame by newline). Set at registration.</summary>
    public bool IsRecordVarying { get; set; }

    // For a variable-length REWRITE: the file offset of the most recently read record's length prefix,
    // and that record's data length — so REWRITE can replace it in place and enforce ISO §14.9.35 GR16.
    private long _varyReadFrameStart;
    private int _varyReadDataLen;

    // Sequential read-position state (ISO §14.9.30 GR21 / §14.9.35 GR5). _lastReadUnsuccessful is set
    // when a READ fails (at-end or error) and no reposition has occurred since — a subsequent sequential
    // READ then returns '46'. _prevOpWasSuccessfulRead is true only immediately after a successful READ;
    // a sequential REWRITE requires it (else '43'). Both reset at OPEN; any non-READ op clears the latter.
    private bool _lastReadUnsuccessful;
    private bool _prevOpWasSuccessfulRead;

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
        // OPEN re-establishes the file position; clear the sequential read-position state (§14.9.30/§14.9.35).
        _lastReadUnsuccessful = false;
        _prevOpWasSuccessfulRead = false;
        _optionalAbsentInput = false;
        bool fileExists = File.Exists(ExternalName);
        try
        {
            switch (mode)
            {
                case FileOpenMode.Input:
                    if (!fileExists)
                    {
                        if (!IsOptional) return FileStatus.FileNotFound;
                        // OPTIONAL file not present: OPEN INPUT succeeds (05) and is positioned at
                        // end-of-file so the first READ raises AT END (ISO §9.1.13.2).
                        _optionalAbsentInput = true;
                        return FileStatus.OptionalFileNotFound;
                    }
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
                    // OPEN EXTEND/I-O on a non-optional file that is not present is status 35
                    // (ISO §9.1.13.4 item 5); an optional missing file is created with status 05.
                    if (!fileExists && !IsOptional)
                        return FileStatus.FileNotFound;
                    if (_lineSequential)
                        _writer = new StreamWriter(ExternalName, true, Encoding.ASCII);
                    else
                        _stream = new FileStream(ExternalName, FileMode.Append, FileAccess.Write);
                    break;

                case FileOpenMode.InputOutput:
                    if (!fileExists && !IsOptional)
                        return FileStatus.FileNotFound;
                    _stream = new FileStream(ExternalName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                    break;
            }
            // Initialize LINAGE-COUNTER for files with LINAGE clause
            if (LinageBody > 0)
                LinageCounter = 1;
            // Optional file created on first I-O/EXTEND open → status 05 (ISO §9.1.13.2 item 5a).
            if (!fileExists && IsOptional &&
                (mode == FileOpenMode.Extend || mode == FileOpenMode.InputOutput))
                return FileStatus.OptionalFileNotFound;
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
        _optionalAbsentInput = false;
        return FileStatus.Success;
    }

    public string ReadNext(byte[] recordBuffer)
    {
        // A READ on a file connector not open in the input or I-O mode is status 47 (ISO §9.1.13.7);
        // a closed file is, by definition, not open in those modes. Status 42 is CLOSE/UNLOCK-only.
        if (!IsOpen) return FileStatus.ReadNotOpenForInput;
        if (_openMode == FileOpenMode.Output || _openMode == FileOpenMode.Extend)
            return FileStatus.ReadNotOpenForInput;

        // An OPTIONAL file that was not present at OPEN INPUT is positioned at end-of-file: the first
        // (and every) READ raises the AT END condition with status 10 (ISO §9.1.13.2). No record is read.
        if (_optionalAbsentInput)
        {
            _prevOpWasSuccessfulRead = false;
            _lastReadUnsuccessful = true;
            return FileStatus.AtEnd;
        }

        // ISO §14.9.30 GR21: a sequential READ when the previous READ was unsuccessful (at-end/error) and
        // no reposition has occurred is itself unsuccessful with status 46 — no valid next record exists.
        if (_lastReadUnsuccessful)
            return FileStatus.NoValidNextRecord;

        string s = ReadNextCore(recordBuffer);
        // Track read-position state for §14.9.30 GR21 (46) and §14.9.35 GR5 (REWRITE 43).
        _prevOpWasSuccessfulRead = s == FileStatus.Success;
        if (s != FileStatus.Success)
            _lastReadUnsuccessful = true;
        return s;
    }

    private string ReadNextCore(byte[] recordBuffer)
    {
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
                // The line length IS the record length for a variable-length (line-sequential) record,
                // capped at the record area (a longer line is truncated into the buffer).
                LastRecordLength = Math.Min(lineBytes.Length, recordBuffer.Length);
                return FileStatus.Success;
            }

            if (_stream != null && IsRecordVarying)
                return ReadNextVarying(recordBuffer);

            if (_stream != null)
            {
                int bytesRead = _stream.Read(recordBuffer, 0, _recordLength);
                if (bytesRead == 0) return FileStatus.AtEnd;
                if (bytesRead < _recordLength)
                {
                    // Pad remaining with spaces
                    Array.Fill(recordBuffer, (byte)' ', bytesRead, _recordLength - bytesRead);
                }
                LastRecordLength = bytesRead;
                return FileStatus.Success;
            }

            return FileStatus.PermanentError;
        }
        catch (IOException)
        {
            return FileStatus.PermanentError;
        }
    }

    /// <summary>Read the next length-framed record from a record-sequential variable-length file:
    /// a 4-byte little-endian length prefix, then that many data bytes. The data is copied into the
    /// (largest-record-sized) buffer and the remainder space-padded; LastRecordLength is the data length
    /// so a RECORD VARYING DEPENDING ON item receives the true length. Records to be read into a smaller
    /// area are still consumed in full so the file position stays record-aligned.</summary>
    private string ReadNextVarying(byte[] recordBuffer)
    {
        long frameStart = _stream!.Position;
        byte[] lenBuf = new byte[4];
        int n = 0;
        while (n < 4) { int r = _stream.Read(lenBuf, n, 4 - n); if (r == 0) break; n += r; }
        if (n == 0) return FileStatus.AtEnd;
        if (n < 4) return FileStatus.PermanentError; // truncated frame
        int len = BinaryPrimitives.ReadInt32LittleEndian(lenBuf);

        Array.Fill(recordBuffer, (byte)' ');
        int toRead = Math.Min(len, recordBuffer.Length);
        int got = 0;
        while (got < toRead) { int r = _stream.Read(recordBuffer, got, toRead - got); if (r == 0) break; got += r; }
        // Consume any data bytes beyond the receiving area so the next read stays record-aligned.
        if (len > toRead) _stream.Seek(len - toRead, SeekOrigin.Current);

        LastRecordLength = len;
        _varyReadFrameStart = frameStart;
        _varyReadDataLen = len;
        return FileStatus.Success;
    }

    public string ReadPrevious(byte[] recordBuffer) =>
        FileStatus.PermanentError; // Not supported for sequential files

    public string ReadByKey(byte[] recordBuffer, byte[] keyValue) =>
        FileStatus.PermanentError; // Not supported for sequential files

    public string Write(byte[] recordData)
    {
        // A WRITE on a file connector not open in the correct mode is status 48 (ISO §9.1.13.7),
        // not 42 (which is CLOSE/UNLOCK-only). For a sequential-access file, WRITE is valid only
        // in the OUTPUT or EXTEND mode (item 8a) — I-O mode supports READ/REWRITE, not WRITE — so
        // any mode other than OUTPUT/EXTEND (Input, I-O, or closed) yields 48.
        if (!IsOpen) return FileStatus.WriteNotOpenForOutput;
        if (_openMode != FileOpenMode.Output && _openMode != FileOpenMode.Extend)
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

    public string WriteVariable(byte[] recordData)
    {
        // RECORD VARYING: recordData is exactly the bytes to write. Unlike Write (which TrimEnds for
        // line-sequential), the explicit length governs — emit the bytes verbatim so the on-disk line
        // length equals the record length and round-trips on read-back.
        if (!IsOpen) return FileStatus.WriteNotOpenForOutput;
        if (_openMode != FileOpenMode.Output && _openMode != FileOpenMode.Extend)
            return FileStatus.WriteNotOpenForOutput;
        try
        {
            if (_lineSequential && _writer != null)
            {
                _writer.WriteLine(Encoding.ASCII.GetString(recordData));
                return FileStatus.Success;
            }
            if (_stream != null)
            {
                // Record-sequential variable-length: frame the record with a 4-byte length prefix so
                // its length round-trips on read-back without relying on a delimiter (ISO §13.18.43).
                byte[] lenBuf = new byte[4];
                BinaryPrimitives.WriteInt32LittleEndian(lenBuf, recordData.Length);
                _stream.Write(lenBuf, 0, 4);
                _stream.Write(recordData, 0, recordData.Length);
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
        // For sequential files, rewrite replaces the last-read record. A REWRITE on a file connector
        // not open in the I-O mode is status 49 (ISO §9.1.13.7), not 42 (CLOSE/UNLOCK-only).
        if (!IsOpen || _openMode != FileOpenMode.InputOutput || _stream == null)
            return FileStatus.DeleteRewriteNotOpenForIO;

        // ISO §14.9.35 GR5: in sequential access the immediately previous I-O must have been a successful
        // READ; otherwise status 43 and the record is left unchanged. (A second REWRITE, or a REWRITE
        // after an at-end READ, fails here.)
        if (!_prevOpWasSuccessfulRead)
            return FileStatus.NoSuccessfulReadBeforeDeleteRewrite;
        // This REWRITE becomes the previous I-O, so a following REWRITE without an intervening READ → 43.
        _prevOpWasSuccessfulRead = false;

        try
        {
            if (IsRecordVarying)
            {
                // ISO §14.9.35 GR16: for a record-sequential file the rewritten record's length must
                // equal the replaced record's length; otherwise the REWRITE is unsuccessful (status 44)
                // and the record is left unchanged.
                if (recordData.Length != _varyReadDataLen)
                    return FileStatus.RecordBoundaryViolation;
                _stream.Seek(_varyReadFrameStart, SeekOrigin.Begin);
                byte[] lenBuf = new byte[4];
                BinaryPrimitives.WriteInt32LittleEndian(lenBuf, recordData.Length);
                _stream.Write(lenBuf, 0, 4);
                _stream.Write(recordData, 0, recordData.Length);
                // Position is now back at the end of this record (frameStart + 4 + len) for the next read.
                return FileStatus.Success;
            }

            // Fixed-length record-sequential: the record being replaced is _recordLength bytes. GR16 —
            // a length other than _recordLength is unsuccessful (44). (A fixed record-name is always
            // _recordLength, so this only guards a malformed caller.)
            if (recordData.Length != _recordLength)
                return FileStatus.RecordBoundaryViolation;
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

        if (lines < 0)
        {
            // ADVANCING PAGE: LINAGE-COUNTER is reset to one on the new page (ISO §13.18.34 GR7c1).
            LinageCounter = 1;
            return false;
        }

        // ADVANCING n (n>=0) or plain WRITE (n=1): the counter is incremented (GR7c2/c3).
        LinageCounter += lines;

        if (LinageCounter > LinageBody)
        {
            // Page overflow (§14.9.51 GR26a): the lines do not fit in the page body. The device is
            // repositioned to the first line of the succeeding page and the counter resets to 1
            // (GR7c4). An end-of-page (overflow) condition exists.
            LinageCounter = 1;
            EndOfPage = true;
        }
        else if (LinageFooting > 0 && LinageCounter >= LinageFooting)
        {
            // Footing-area end-of-page (§14.9.51 GR26b): a FOOTING is specified and this WRITE prints
            // within the footing area (counter at or beyond the footing start, still on the page).
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
