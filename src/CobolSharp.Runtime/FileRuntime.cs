// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace CobolSharp.Runtime;

/// <summary>
/// COBOL file I/O runtime. The compiler emits calls to these methods
/// for OPEN, CLOSE, READ, WRITE operations.
///
/// For now: sequential output to host files. NIST print files are
/// fixed-length ASCII records written line-by-line.
/// </summary>
public static class FileRuntime
{
    // Track open output files (StreamWriter for line-oriented output)
    private static readonly Dictionary<string, StreamWriter> _openFiles = new();

    // Track open input files (FileStream for binary/fixed-length reading)
    private static readonly Dictionary<string, FileStream> _inputFiles = new();

    // Track at-end state per file
    private static readonly Dictionary<string, bool> _atEnd = new();

    /// <summary>
    /// OPEN OUTPUT file-name.
    /// </summary>
    public static void OpenOutput(string fileName)
    {
        if (_openFiles.ContainsKey(fileName))
            return;

        // Map COBOL file name to host path
        string hostPath = MapToHostPath(fileName);
        var writer = new StreamWriter(hostPath, append: false, Encoding.ASCII);
        writer.AutoFlush = true;
        _openFiles[fileName] = writer;
    }

    /// <summary>
    /// CLOSE file-name.
    /// </summary>
    public static void CloseFile(string fileName)
    {
        if (_openFiles.TryGetValue(fileName, out var writer))
        {
            writer.WriteLine(); // Final newline after last AFTER ADVANCING record
            writer.Flush();
            writer.Close();
            _openFiles.Remove(fileName);
        }
        if (_inputFiles.TryGetValue(fileName, out var stream))
        {
            stream.Close();
            _inputFiles.Remove(fileName);
        }
        _atEnd.Remove(fileName);
    }

    /// <summary>
    /// WRITE record-name.
    /// Writes the record bytes as an ASCII line to the file.
    /// If the file isn't open, writes to console (DISPLAY device).
    /// </summary>
    public static void WriteRecord(string fileName, byte[] recordBytes, int offset, int length)
    {
        // Convert record bytes to ASCII text, trim trailing spaces
        string text = Encoding.ASCII.GetString(recordBytes, offset, length).TrimEnd();

        if (_openFiles.TryGetValue(fileName, out var writer))
        {
            writer.WriteLine(text);
        }
        else
        {
            // File not open — write to console (DISPLAY device)
            Console.WriteLine(text);
        }
    }

    /// <summary>
    /// WRITE record-name (simplified: just the text content).
    /// Used when record bytes aren't available yet.
    /// </summary>
    public static void WriteText(string fileName, string text)
    {
        WriteAfterAdvancing(fileName, text, 1);
    }

    /// <summary>
    /// WRITE AFTER ADVANCING n LINES: output n line-feeds then the record text.
    /// </summary>
    public static void WriteAfterAdvancing(string fileName, string text, int advanceLines)
    {
        if (_openFiles.TryGetValue(fileName, out var writer))
        {
            for (int i = 0; i < advanceLines; i++)
                writer.WriteLine();
            writer.Write(text);
        }
        else
        {
            for (int i = 0; i < advanceLines; i++)
                Console.WriteLine();
            Console.Write(text);
        }
    }

    /// <summary>
    /// OPEN INPUT file-name.
    /// </summary>
    public static void OpenInput(string fileName)
    {
        if (_inputFiles.ContainsKey(fileName))
            return;

        string hostPath = MapToHostPath(fileName);
        if (!File.Exists(hostPath))
            throw new FileNotFoundException($"OPEN INPUT: file not found: {hostPath}");

        var stream = new FileStream(hostPath, FileMode.Open, FileAccess.Read);
        _inputFiles[fileName] = stream;
        _atEnd[fileName] = false;
    }

    /// <summary>
    /// READ: read next fixed-length record into byte buffer.
    /// Returns true if a record was read, false if at end.
    /// </summary>
    public static bool ReadRecord(string fileName, byte[] buffer, int offset, int length)
    {
        if (!_inputFiles.TryGetValue(fileName, out var stream))
            return false;

        int totalRead = 0;
        while (totalRead < length)
        {
            int bytesRead = stream.Read(buffer, offset + totalRead, length - totalRead);
            if (bytesRead == 0)
            {
                _atEnd[fileName] = true;
                // Pad remainder with spaces if partial read
                for (int i = offset + totalRead; i < offset + length; i++)
                    buffer[i] = 0x20;
                return totalRead > 0; // true if we got any data
            }
            totalRead += bytesRead;
        }
        _atEnd[fileName] = false;
        return true;
    }

    /// <summary>
    /// Check if a file has reached end-of-file.
    /// </summary>
    public static bool IsAtEnd(string fileName)
    {
        return _atEnd.TryGetValue(fileName, out var atEnd) && atEnd;
    }

    /// <summary>
    /// Map COBOL file name to host file path.
    /// For NIST: PRINT-FILE → stdout (console).
    /// </summary>
    private static string MapToHostPath(string fileName)
    {
        // For now, use the COBOL file name as the host file name
        // NIST test programs use PRINT-FILE for output
        return fileName.ToLowerInvariant() + ".txt";
    }

    /// <summary>
    /// Flush and close all open files (called at program exit).
    /// </summary>
    public static void CloseAll()
    {
        foreach (var writer in _openFiles.Values)
        {
            writer.Flush();
            writer.Close();
        }
        _openFiles.Clear();

        foreach (var stream in _inputFiles.Values)
            stream.Close();
        _inputFiles.Clear();
        _atEnd.Clear();
    }
}
