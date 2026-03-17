// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolSharp.Runtime.IO;

namespace CobolSharp.Runtime;

/// <summary>
/// COBOL file I/O runtime. Thin static facade over CobolFileManager.
/// The compiler emits calls to these static methods for OPEN, CLOSE, READ, WRITE operations.
/// Internally all operations delegate to production CobolFileManager + IFileHandler.
/// </summary>
public static class FileRuntime
{
    private static CobolFileManager? _manager;
    private static readonly Dictionary<string, string> _lastStatus = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> _afterAdvancingFiles = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initialize the file manager. Called once at program start.
    /// </summary>
    public static void Init()
    {
        _manager?.Dispose();
        _manager = new CobolFileManager();
        _lastStatus.Clear();
        _afterAdvancingFiles.Clear();
    }

    /// <summary>
    /// Register a file handler for a COBOL file name.
    /// Called by compiler-generated code at startup for each SELECT.
    /// </summary>
    public static void RegisterFileHandler(string cobolName, string externalPath, int recordLength, bool lineSequential)
    {
        RegisterFileHandlerWithOrg(cobolName, externalPath, recordLength, lineSequential, "SEQUENTIAL", 0, 0);
    }

    public static void RegisterFileHandlerWithOrg(string cobolName, string externalPath,
        int recordLength, bool lineSequential, string organization, int keyOffset, int keyLength)
    {
        IFileHandler handler = organization switch
        {
            "INDEXED" => new IndexedFileHandler(externalPath, recordLength, keyOffset, keyLength),
            "RELATIVE" => new RelativeFileHandler(externalPath, recordLength),
            _ => new SequentialFileHandler(externalPath, recordLength, lineSequential)
        };
        EnsureManager();
        _manager!.RegisterFile(cobolName, handler);
        _lastStatus[cobolName] = FileStatus.Success;
    }

    /// <summary>
    /// OPEN OUTPUT file-name.
    /// </summary>
    public static void OpenOutput(string fileName)
    {
        EnsureManager();
        string status = _manager!.Open(fileName, FileOpenMode.Output);
        _lastStatus[fileName] = status;
    }

    /// <summary>
    /// OPEN INPUT file-name.
    /// </summary>
    public static void OpenInput(string fileName)
    {
        EnsureManager();
        string status = _manager!.Open(fileName, FileOpenMode.Input);
        _lastStatus[fileName] = status;
    }

    /// <summary>
    /// OPEN I-O file-name.
    /// </summary>
    public static void OpenIO(string fileName)
    {
        EnsureManager();
        string status = _manager!.Open(fileName, FileOpenMode.InputOutput);
        _lastStatus[fileName] = status;
    }

    /// <summary>
    /// OPEN EXTEND file-name.
    /// </summary>
    public static void OpenExtend(string fileName)
    {
        EnsureManager();
        string status = _manager!.Open(fileName, FileOpenMode.Extend);
        _lastStatus[fileName] = status;
    }

    /// <summary>
    /// CLOSE file-name.
    /// </summary>
    public static void CloseFile(string fileName)
    {
        EnsureManager();
        // For AFTER ADVANCING files, write final newline before closing
        if (_afterAdvancingFiles.Remove(fileName))
        {
            var handler = _manager!.GetHandler(fileName) as SequentialFileHandler;
            handler?.WriteRawText("\r\n");
        }
        string status = _manager!.Close(fileName);
        _lastStatus[fileName] = status;
    }

    /// <summary>
    /// WRITE record-name: plain WRITE (data path).
    /// Delegates to handler.Write which does line-sequential formatting (TrimEnd + WriteLine).
    /// </summary>
    public static void WriteRecord(string fileName, byte[] recordBytes, int offset, int length)
    {
        EnsureManager();
        byte[] recordSlice = new byte[length];
        Array.Copy(recordBytes, offset, recordSlice, 0, length);
        string status = _manager!.Write(fileName, recordSlice);
        _lastStatus[fileName] = status;
    }

    /// <summary>
    /// WRITE AFTER ADVANCING n LINES: print-control semantics.
    /// Outputs n CR/LF sequences then the record text (no trailing newline).
    /// </summary>
    public static void WriteAfterAdvancing(string fileName, byte[] area, int offset, int size, int advanceLines)
    {
        string text = Encoding.ASCII.GetString(area, offset, size).TrimEnd();
        WriteAfterAdvancingText(fileName, text, advanceLines);
    }

    /// <summary>
    /// WRITE AFTER ADVANCING with pre-extracted text.
    /// </summary>
    public static void WriteAfterAdvancingText(string fileName, string text, int advanceLines)
    {
        EnsureManager();
        var handler = _manager!.GetHandler(fileName) as SequentialFileHandler;
        if (handler != null && handler.IsOpen)
        {
            _afterAdvancingFiles.Add(fileName);
            for (int i = 0; i < advanceLines; i++)
                handler.WriteRawText("\r\n");
            handler.WriteRawText(text);
            _lastStatus[fileName] = FileStatus.Success;
        }
        else
        {
            // Fallback: console output
            for (int i = 0; i < advanceLines; i++)
                Console.WriteLine();
            Console.Write(text);
        }
    }

    /// <summary>
    /// READ: read next record from file into byte buffer.
    /// Returns true if a record was read, false if at end-of-file.
    /// </summary>
    public static bool ReadRecord(string fileName, byte[] buffer, int offset, int length)
    {
        EnsureManager();
        byte[] tempBuf = new byte[length];
        string status = _manager!.ReadNext(fileName, tempBuf);
        _lastStatus[fileName] = status;

        if (status == FileStatus.AtEnd)
            return false;

        Array.Copy(tempBuf, 0, buffer, offset, length);
        return status == FileStatus.Success;
    }

    /// <summary>
    /// Check if a file has reached end-of-file.
    /// </summary>
    public static bool IsAtEnd(string fileName)
    {
        return _lastStatus.TryGetValue(fileName, out var status) && status == FileStatus.AtEnd;
    }

    /// <summary>
    /// Get the last file status code for a COBOL file name.
    /// </summary>
    public static string GetLastStatus(string cobolName)
    {
        return _lastStatus.TryGetValue(cobolName, out var status) ? status : FileStatus.Success;
    }

    /// <summary>
    /// REWRITE: replace the last-read record.
    /// </summary>
    public static void Rewrite(string fileName, byte[] recordBytes, int offset, int length)
    {
        EnsureManager();
        byte[] recordSlice = new byte[length];
        Array.Copy(recordBytes, offset, recordSlice, 0, length);
        string status = _manager!.Rewrite(fileName, recordSlice);
        _lastStatus[fileName] = status;
    }

    /// <summary>
    /// DELETE: delete the current record from a relative/indexed file.
    /// </summary>
    public static void DeleteRecord(string fileName)
    {
        EnsureManager();
        string status = _manager!.Delete(fileName);
        _lastStatus[fileName] = status;
    }

    /// <summary>
    /// START: position an indexed file for subsequent READ NEXT.
    /// </summary>
    public static void StartFile(string fileName, byte[] keyArea, int keyOffset, int keyLength, int condition)
    {
        EnsureManager();
        byte[] keyValue = new byte[keyLength];
        Array.Copy(keyArea, keyOffset, keyValue, 0, keyLength);
        string status = _manager!.Start(fileName, keyValue, (IO.StartCondition)condition);
        _lastStatus[fileName] = status;
    }

    /// <summary>
    /// Check if the last file operation was NOT successful (status != "00").
    /// Returns true if an error occurred (invalid key, etc.).
    /// </summary>
    public static bool IsInvalidKey(string fileName)
    {
        if (_lastStatus.TryGetValue(fileName, out var status))
            return status != IO.FileStatus.Success;
        return false;
    }

    /// <summary>
    /// Resolve COBOL file name to host file path.
    /// Used during handler registration to compute the external file name.
    /// </summary>
    public static string ResolveHostPath(string assignTarget)
    {
        string baseName = assignTarget;
        if (baseName.Contains('.') || baseName.Contains('/') || baseName.Contains('\\'))
            return baseName;
        return baseName.ToLowerInvariant() + ".txt";
    }

    /// <summary>
    /// Flush and close all open files (called at program exit).
    /// </summary>
    public static void CloseAll()
    {
        _manager?.Dispose();
        _manager = null;
        _lastStatus.Clear();
        _afterAdvancingFiles.Clear();
    }

    private static void EnsureManager()
    {
        _manager ??= new CobolFileManager();
    }
}
