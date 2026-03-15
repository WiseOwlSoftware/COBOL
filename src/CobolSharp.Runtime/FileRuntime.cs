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
    // Track open files
    private static readonly Dictionary<string, StreamWriter> _openFiles = new();

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
    }
}
