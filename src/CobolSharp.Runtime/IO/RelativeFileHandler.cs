// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
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

    /// <summary>Relative records are fixed-length; the record length is constant.</summary>
    public int LastRecordLength => _recordLength;

    /// <summary>Relative records are fixed-length — variable write is an ordinary write.</summary>
    public string WriteVariable(byte[] recordData) => Write(recordData);

    /// <summary>When true (SELECT OPTIONAL), OPEN INPUT on a missing file returns "05" instead of "35".</summary>
    public bool IsOptional { get; set; }

    /// <summary>The RELATIVE KEY data-name identifier, if specified in FILE-CONTROL.</summary>
    public string? RelativeKeyName { get; set; }

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
            return IsOptional ? FileStatus.OptionalFileNotFound : FileStatus.FileNotFound;
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
        // READ on a file connector not open in input/I-O mode is status 47 (ISO §9.1.13.7), not 42.
        if (!IsOpen) return FileStatus.ReadNotOpenForInput;
        if (_openMode == FileOpenMode.Output || _openMode == FileOpenMode.Extend)
            return FileStatus.ReadNotOpenForInput;
        _currentRecord++;
        return ReadRecord(_currentRecord, recordBuffer);
    }

    public string ReadPrevious(byte[] recordBuffer)
    {
        if (!IsOpen) return FileStatus.ReadNotOpenForInput;
        if (_openMode == FileOpenMode.Output || _openMode == FileOpenMode.Extend)
            return FileStatus.ReadNotOpenForInput;
        if (_currentRecord <= 1) return FileStatus.AtEnd;
        _currentRecord--;
        return ReadRecord(_currentRecord, recordBuffer);
    }

    public string ReadByKey(byte[] recordBuffer, byte[] keyValue)
    {
        if (!IsOpen) return FileStatus.ReadNotOpenForInput;
        if (_openMode == FileOpenMode.Output || _openMode == FileOpenMode.Extend)
            return FileStatus.ReadNotOpenForInput;
        int recordNum = int.Parse(System.Text.Encoding.ASCII.GetString(keyValue).Trim());
        _currentRecord = recordNum;
        return ReadRecord(recordNum, recordBuffer);
    }

    public string Write(byte[] recordData)
    {
        // WRITE on a file connector not open in the correct mode is status 48 (ISO §9.1.13.7), not 42.
        if (!IsOpen) return FileStatus.WriteNotOpenForOutput;
        if (_openMode == FileOpenMode.Input)
            return FileStatus.WriteNotOpenForOutput;
        try
        {
            _stream!.Write(ToSlot(recordData), 0, _recordLength);
            return FileStatus.Success;
        }
        catch (IOException) { return FileStatus.PermanentError; }
    }

    public string Rewrite(byte[] recordData)
    {
        // REWRITE on a file connector not open in I-O mode is status 49 (ISO §9.1.13.7), not 42.
        // With no successfully-read current record, it is status 43 (ISO §9.1.13.6).
        if (!IsOpen || _openMode != FileOpenMode.InputOutput)
            return FileStatus.DeleteRewriteNotOpenForIO;
        if (_currentRecord == 0)
            return FileStatus.NoSuccessfulReadBeforeDeleteRewrite;
        try
        {
            _stream!.Seek((long)(_currentRecord - 1) * _recordLength, SeekOrigin.Begin);
            _stream.Write(ToSlot(recordData), 0, _recordLength);
            return FileStatus.Success;
        }
        catch (IOException) { return FileStatus.PermanentError; }
    }

    /// <summary>
    /// Normalize a record to the file's fixed slot size: a relative record always occupies
    /// <see cref="_recordLength"/> bytes regardless of the supplied buffer's length (a shorter
    /// record — e.g. one alternative of a RECORD VARYING file — is space-padded; a longer one is
    /// truncated), so a slot write never reads past the source buffer.
    /// </summary>
    private byte[] ToSlot(byte[] recordData)
    {
        if (recordData.Length == _recordLength) return recordData;
        byte[] slot = new byte[_recordLength];
        Array.Fill(slot, (byte)' ');
        Array.Copy(recordData, 0, slot, 0, Math.Min(recordData.Length, _recordLength));
        return slot;
    }

    public string Delete()
    {
        // Mark record as deleted by filling with high-values.
        // DELETE on a file connector not open in I-O mode is status 49 (ISO §9.1.13.7), not 42.
        // With no successfully-read current record, it is status 43 (ISO §9.1.13.6).
        if (!IsOpen || _openMode != FileOpenMode.InputOutput)
            return FileStatus.DeleteRewriteNotOpenForIO;
        if (_currentRecord == 0)
            return FileStatus.NoSuccessfulReadBeforeDeleteRewrite;
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
            // The caller's record buffer may differ from the fixed slot size (e.g. a VARYING file's
            // alternative record). Read at most the buffer's length, then space-fill any remainder.
            int toRead = Math.Min(_recordLength, buffer.Length);
            int read = _stream.Read(buffer, 0, toRead);
            if (read < buffer.Length)
                Array.Fill(buffer, (byte)' ', read, buffer.Length - read);
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
