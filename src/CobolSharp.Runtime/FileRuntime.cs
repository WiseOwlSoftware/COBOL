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

    // Track open input files (StreamReader for line-sequential reading)
    private static readonly Dictionary<string, StreamReader> _inputFiles = new();

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
        if (_openFiles.ContainsKey(fileName))
            return;

        string hostPath = ResolveHostPath(fileName);
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
        if (_inputFiles.TryGetValue(fileName, out var reader))
        {
            reader.Close();
            _inputFiles.Remove(fileName);
        }
        _atEnd.Remove(fileName);
    }

    /// <summary>
    /// WRITE record-name.
    /// Writes the record bytes as an ASCII line to the file.
    /// If the file isn't open, writes to console (DISPLAY device).
    /// </summary>
    /// <summary>
    /// WRITE record-name: write record as a line-sequential record.
    /// Uses AFTER ADVANCING 1 LINE semantics (newline before text) to match
    /// NIST expected output format. This will be split into separate WRITE
    /// and WRITE AFTER ADVANCING paths when we implement proper file mode handling.
    /// </summary>
    public static void WriteRecord(string fileName, byte[] recordBytes, int offset, int length)
    {
        string text = Encoding.ASCII.GetString(recordBytes, offset, length).TrimEnd();

        if (_openFiles.TryGetValue(fileName, out _))
        {
            WriteAfterAdvancing(fileName, text, 1);
        }
        else
        {
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

        string hostPath = ResolveHostPath(fileName);
        if (!File.Exists(hostPath))
            throw new FileNotFoundException($"OPEN INPUT: file not found: {hostPath}");

        var reader = new StreamReader(hostPath, Encoding.ASCII);
        _inputFiles[fileName] = reader;
        _atEnd[fileName] = false;
    }

    /// <summary>
    /// READ: read next record (line-sequential) into byte buffer.
    /// Reads one line, pads/truncates to fit the record length.
    /// Returns true if a record was read, false if at end-of-file.
    /// </summary>
    public static bool ReadRecord(string fileName, byte[] buffer, int offset, int length)
    {
        if (!_inputFiles.TryGetValue(fileName, out var reader))
            return false;

        string? line = reader.ReadLine();
        if (line == null)
        {
            _atEnd[fileName] = true;
            return false;
        }

        // Convert line to ASCII bytes, pad with spaces to record length
        byte[] lineBytes = Encoding.ASCII.GetBytes(line);
        int copyLen = Math.Min(lineBytes.Length, length);
        Array.Copy(lineBytes, 0, buffer, offset, copyLen);
        // Pad remainder with spaces
        for (int i = offset + copyLen; i < offset + length; i++)
            buffer[i] = 0x20;

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

        foreach (var reader in _inputFiles.Values)
            reader.Close();
        _inputFiles.Clear();
        _atEnd.Clear();
        _assignTargets.Clear();
    }
}
