// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace CobolNet.Runtime.IO;

/// <summary>
/// A typed-native sequential file connector (COBOLNET_DESIGN §8) — the control logic the legacy
/// <c>SequentialFileHandler</c>/<c>FileRuntime</c> proved over the 364-NIST corpus, re-substrated from a byte buffer
/// to the record's <b>character image</b> (a C# <see cref="string"/>). A COBOL record IS a typed value; the only edge
/// where it becomes characters is here, at the on-disk boundary. Two on-disk shapes: <b>line-sequential</b>
/// (newline-framed text — <c>WriteLine</c>/<c>ReadLine</c>) and <b>record-sequential</b> (fixed-width blocks, plus the
/// print-control <c>WRITE … ADVANCING</c> raw stream). The ISO §9.1.13 I-O status codes and the §14.9.30/§14.9.35
/// read-position state machine are ported verbatim. (Relative/indexed organizations are later slices.)
/// </summary>
public sealed class SequentialFile
{
    private readonly string _hostPath;
    private readonly int _recordWidth;
    private readonly bool _lineSequential;

    private StreamReader? _reader;
    private StreamWriter? _writer;
    private FileOpenMode _mode;
    private bool _afterAdvancing;        // a WRITE … ADVANCING happened → a trailing newline is written at CLOSE
    private bool _optionalAbsentInput;   // OPEN INPUT on an absent OPTIONAL file: positioned at EOF (status 05 then 10)

    // Read-position state (ISO §14.9.30 GR21 / §14.9.35 GR5): a sequential READ after an unsuccessful READ with no
    // reposition is itself unsuccessful (46); a sequential REWRITE requires the immediately-previous op be a
    // successful READ (else 43). Both reset at OPEN; any non-READ op clears _prevOpWasSuccessfulRead.
    private bool _lastReadUnsuccessful;
    private bool _prevOpWasSuccessfulRead;

    // The most recently read record image (for a sequential REWRITE position; the in-memory line model rewrites the
    // line at _rewriteIndex). For a record-sequential REWRITE the fixed-width block is replaced in place on disk.
    private long _lastReadBlockStart = -1;

    /// <summary>True for a SELECT OPTIONAL file (OPEN INPUT on a missing file is 05 + EOF, not 35).</summary>
    public bool IsOptional { get; set; }

    /// <summary>True between a successful OPEN and the matching CLOSE.</summary>
    public bool IsOpen => _reader is not null || _writer is not null || _optionalAbsentInput;

    /// <summary>The latest I-O status (ISO §9.1.13). "00" until the first operation.</summary>
    public string Status { get; private set; } = FileStatusCode.Success;

    /// <summary>Set the I-O status directly (for facade-level conditions: a locked-file OPEN, a REEL/UNIT CLOSE).</summary>
    public void SetStatus(string status) => Status = status;

    public SequentialFile(string hostPath, int recordWidth, bool lineSequential)
    {
        _hostPath = hostPath;
        _recordWidth = recordWidth < 1 ? 1 : recordWidth;
        _lineSequential = lineSequential;
    }

    // ── OPEN / CLOSE ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>OPEN the file in <paramref name="mode"/> (ISO §14.9.25 / §9.1.13.4). Sets and returns the status.</summary>
    public string Open(FileOpenMode mode)
    {
        if (IsOpen) return Status = FileStatusCode.FileAlreadyOpen;
        _mode = mode;
        _afterAdvancing = false;
        _optionalAbsentInput = false;
        _lastReadUnsuccessful = false;
        _prevOpWasSuccessfulRead = false;
        bool exists = File.Exists(_hostPath);
        try
        {
            switch (mode)
            {
                case FileOpenMode.Input:
                    if (!exists)
                    {
                        if (!IsOptional) return Status = FileStatusCode.FileNotFound;
                        _optionalAbsentInput = true;                 // positioned at EOF (ISO §9.1.13.2 item 5b)
                        return Status = FileStatusCode.OptionalFileNotFound;
                    }
                    _reader = new StreamReader(_hostPath, Encoding.Latin1);
                    break;

                case FileOpenMode.Output:
                    _writer = new StreamWriter(_hostPath, append: false, Encoding.Latin1) { NewLine = "\r\n" };
                    break;

                case FileOpenMode.Extend:
                    if (!exists && !IsOptional) return Status = FileStatusCode.FileNotFound;
                    _writer = new StreamWriter(_hostPath, append: true, Encoding.Latin1) { NewLine = "\r\n" };
                    if (!exists && IsOptional) return Status = FileStatusCode.OptionalFileNotFound;
                    break;

                case FileOpenMode.IO:
                    // I-O needs read+write; the typed line model loads the existing records, then writes them all
                    // back at CLOSE. An absent non-optional file is 35; an optional one is created (05).
                    if (!exists && !IsOptional) return Status = FileStatusCode.FileNotFound;
                    if (exists) _reader = new StreamReader(_hostPath, Encoding.Latin1);
                    else { _writer = new StreamWriter(_hostPath, append: false, Encoding.Latin1) { NewLine = "\r\n" }; }
                    if (!exists && IsOptional) return Status = FileStatusCode.OptionalFileNotFound;
                    break;
            }
            return Status = FileStatusCode.Success;
        }
        catch (UnauthorizedAccessException) { return Status = FileStatusCode.PermissionDenied; }
        catch (IOException) { return Status = FileStatusCode.PermanentError; }
    }

    /// <summary>CLOSE the file (ISO §14.9.7). A WRITE … ADVANCING stream is terminated with a trailing newline
    /// (matching the legacy print-control behavior). Returns the status.</summary>
    public string Close()
    {
        if (!IsOpen) return Status = FileStatusCode.FileNotOpen;
        if (_afterAdvancing) { _writer?.Write("\r\n"); _afterAdvancing = false; }
        _reader?.Dispose();
        _writer?.Dispose();
        _reader = null;
        _writer = null;
        _optionalAbsentInput = false;
        return Status = FileStatusCode.Success;
    }

    // ── WRITE ────────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Plain <c>WRITE record</c> (ISO §14.9.46): line-sequential writes the trimmed image as a line;
    /// record-sequential writes a fixed-width block. Valid only when open OUTPUT/EXTEND (else 48).</summary>
    public string Write(string image)
    {
        if (!IsOpen || _writer is null) return Status = FileStatusCode.WriteNotOpenForOutput;
        if (_mode is not (FileOpenMode.Output or FileOpenMode.Extend)) return Status = FileStatusCode.WriteNotOpenForOutput;
        if (_lineSequential) _writer.WriteLine(image.TrimEnd());
        else _writer.Write(Fit(image, _recordWidth));
        _afterAdvancing = false;
        return Status = FileStatusCode.Success;
    }

    /// <summary>Print-control <c>WRITE record {BEFORE|AFTER} ADVANCING {n LINES | PAGE}</c> (ISO §14.9.46 GR): for
    /// AFTER, advance then write the trimmed image; for BEFORE, write then advance. <paramref name="lines"/> = -1
    /// means ADVANCING PAGE (a form feed). The leading/trailing newline structure matches the legacy print stream.</summary>
    public string WriteAdvancing(string image, int lines, bool before)
    {
        if (!IsOpen || _writer is null) return Status = FileStatusCode.WriteNotOpenForOutput;
        if (_mode is not (FileOpenMode.Output or FileOpenMode.Extend)) return Status = FileStatusCode.WriteNotOpenForOutput;
        _afterAdvancing = true;
        string text = image.TrimEnd();
        if (before) { _writer.Write(text); Advance(lines); }
        else { Advance(lines); _writer.Write(text); }
        return Status = FileStatusCode.Success;
    }

    private void Advance(int lines)
    {
        if (lines < 0) { _writer!.Write('\f'); return; }   // ADVANCING PAGE
        for (int i = 0; i < lines; i++) _writer!.Write("\r\n");
    }

    // ── READ ─────────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Sequential <c>READ … NEXT</c> (ISO §14.9.30). Returns the record image (padded to the record width)
    /// in <paramref name="image"/> and <see langword="true"/> on success; <see langword="false"/> at end-of-file
    /// (AT END, status 10) or any unsuccessful read. Sets the status.</summary>
    public bool Read(out string image)
    {
        image = new string(' ', _recordWidth);
        if (!IsOpen) { Status = FileStatusCode.ReadNotOpenForInput; return false; }
        if (_mode is FileOpenMode.Output or FileOpenMode.Extend) { Status = FileStatusCode.ReadNotOpenForInput; return false; }
        if (_optionalAbsentInput) { _prevOpWasSuccessfulRead = false; _lastReadUnsuccessful = true; Status = FileStatusCode.AtEnd; return false; }
        if (_lastReadUnsuccessful) { Status = FileStatusCode.NoValidNextRecord; return false; }
        if (_reader is null) { Status = FileStatusCode.ReadNotOpenForInput; return false; }

        if (_lineSequential)
        {
            string? line = _reader.ReadLine();
            if (line is null) { _prevOpWasSuccessfulRead = false; _lastReadUnsuccessful = true; Status = FileStatusCode.AtEnd; return false; }
            image = Fit(line, _recordWidth);
        }
        else
        {
            _lastReadBlockStart = _reader.BaseStream.CanSeek ? _reader.BaseStream.Position : -1;
            var buf = new char[_recordWidth];
            int n = _reader.Read(buf, 0, _recordWidth);
            if (n == 0) { _prevOpWasSuccessfulRead = false; _lastReadUnsuccessful = true; Status = FileStatusCode.AtEnd; return false; }
            image = new string(buf, 0, n).PadRight(_recordWidth, ' ');
        }
        _prevOpWasSuccessfulRead = true;
        Status = FileStatusCode.Success;
        return true;
    }

    // ── REWRITE ──────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Sequential <c>REWRITE record</c> (ISO §14.9.35): replace the last-read record. Valid only when open
    /// I-O (else 49) and the immediately-previous op was a successful READ (else 43). For the line/record models the
    /// rewrite is applied to the open stream in place where supported.</summary>
    public string Rewrite(string image)
    {
        if (!IsOpen || _mode != FileOpenMode.IO) return Status = FileStatusCode.DeleteRewriteNotOpenForIO;
        if (!_prevOpWasSuccessfulRead) return Status = FileStatusCode.NoSuccessfulReadBeforeDeleteRewrite;
        _prevOpWasSuccessfulRead = false;
        if (!_lineSequential && _lastReadBlockStart >= 0 && _reader is { BaseStream: { CanSeek: true, CanWrite: true } stream })
        {
            long resume = stream.Position;
            stream.Seek(_lastReadBlockStart, SeekOrigin.Begin);
            byte[] bytes = Encoding.Latin1.GetBytes(Fit(image, _recordWidth));
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();
            stream.Seek(resume, SeekOrigin.Begin);
            return Status = FileStatusCode.Success;
        }
        // Line-sequential / non-seekable REWRITE in place is not modeled in this slice → loud at the call site is
        // avoided; report a permanent error so the program's FILE STATUS / declarative path observes it.
        return Status = FileStatusCode.PermanentError;
    }

    /// <summary>The AT END condition (ISO §14.9.30): true only at end-of-file (status 10).</summary>
    public bool AtEnd => Status == FileStatusCode.AtEnd;

    /// <summary>Pad (right) or truncate <paramref name="s"/> to exactly <paramref name="width"/> characters.</summary>
    private static string Fit(string s, int width) => s.Length == width ? s : s.Length > width ? s[..width] : s.PadRight(width, ' ');
}
