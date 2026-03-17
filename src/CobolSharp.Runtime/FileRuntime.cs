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
    // Track open text output files (StreamWriter for line-oriented output — PRINT-FILE)
    private static readonly Dictionary<string, StreamWriter> _openFiles = new();

    // Track open binary output files (FileStream for fixed-length records)
    private static readonly Dictionary<string, FileStream> _outputStreams = new();

    // Track open input files (FileStream for binary/fixed-length reading)
    private static readonly Dictionary<string, FileStream> _inputFiles = new();

    // Track at-end state per file
    private static readonly Dictionary<string, bool> _atEnd = new();

    /// <summary>
    /// OPEN OUTPUT file-name.
    /// </summary>
    // Map COBOL file name → ASSIGN target for host path resolution
    private static readonly Dictionary<string, string> _assignTargets = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Register a file's ASSIGN target (called by compiler-generated code at startup).
    /// </summary>
    public static void RegisterFile(string cobolName, string assignTarget)
    {
        _assignTargets[cobolName] = assignTarget;
    }

    public static void OpenOutput(string fileName)
    {
        if (_openFiles.ContainsKey(fileName) || _outputStreams.ContainsKey(fileName))
            return;

        string hostPath = ResolveHostPath(fileName);

        // Use text mode for PRINT-FILE (line-oriented NIST output),
        // binary mode for all other files (fixed-length records)
        if (fileName.Equals("PRINT-FILE", StringComparison.OrdinalIgnoreCase))
        {
            var writer = new StreamWriter(hostPath, append: false, Encoding.ASCII);
            writer.AutoFlush = true;
            _openFiles[fileName] = writer;
        }
        else
        {
            var stream = new FileStream(hostPath, FileMode.Create, FileAccess.Write);
            _outputStreams[fileName] = stream;
        }
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
        if (_outputStreams.TryGetValue(fileName, out var outStream))
        {
            outStream.Flush();
            outStream.Close();
            _outputStreams.Remove(fileName);
        }
        if (_inputFiles.TryGetValue(fileName, out var inStream))
        {
            inStream.Close();
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
        // Binary output stream (data files)
        if (_outputStreams.TryGetValue(fileName, out var stream))
        {
            stream.Write(recordBytes, offset, length);
            return;
        }

        // Text output via StreamWriter (line-oriented, for PRINT-FILE)
        // Uses WriteAfterAdvancing to match NIST expected format (newline before text)
        if (_openFiles.ContainsKey(fileName))
        {
            string text = Encoding.ASCII.GetString(recordBytes, offset, length).TrimEnd();
            WriteAfterAdvancing(fileName, text, 1);
            return;
        }

        // File not open — write to console
        string fallbackText = Encoding.ASCII.GetString(recordBytes, offset, length).TrimEnd();
        Console.WriteLine(fallbackText);
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

        string hostPath = ResolveHostPath(fileName);
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
    /// Resolve COBOL file name to host file path.
    /// Implementor-defined mapping (ISO §9.3.3):
    /// - If ASSIGN target was a string literal, use it as the base name.
    /// - Otherwise (identifier like NIST's XXXXX055), use the COBOL file name.
    /// The ".txt" extension is appended unless the name already has one.
    /// </summary>
    private static string ResolveHostPath(string cobolName)
    {
        if (_assignTargets.TryGetValue(cobolName, out var assignTarget))
        {
            string baseName = assignTarget;
            // If the target already has an extension or path separator, use as-is
            if (baseName.Contains('.') || baseName.Contains('/') || baseName.Contains('\\'))
                return baseName;
            return baseName.ToLowerInvariant() + ".txt";
        }
        return cobolName.ToLowerInvariant() + ".txt";
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

        foreach (var stream in _outputStreams.Values)
        {
            stream.Flush();
            stream.Close();
        }
        _outputStreams.Clear();

        foreach (var stream in _inputFiles.Values)
            stream.Close();
        _inputFiles.Clear();
        _atEnd.Clear();
        _assignTargets.Clear();
    }
}
