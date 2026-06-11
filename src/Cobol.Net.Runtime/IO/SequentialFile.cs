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

    // The byte offset of the most recently read record's fixed-width block (for the in-place record-sequential
    // REWRITE) and the LOGICAL read offset it derives from. The logical offset counts characters CONSUMED from
    // the reader (Latin1 — one byte per character; a record-sequential file is pure fixed-width blocks): the
    // StreamReader BUFFERS, so BaseStream.Position is the buffer-fill boundary, never the read position —
    // deriving the block start from it corrupted the rewrite target (IX106A REWRITE-TEST-GF-02).
    private long _lastReadBlockStart = -1;
    private long _readOffset;

    // RECORD IS VARYING bounds (ISO §13.18.43 GR9/GR10); (-1,-1) = fixed-length records. A varying file's
    // records are length-framed on disk (a 4-byte little-endian length prefix per record — the same framing the
    // keyed connectors' KeyedFrames use; the physical format is implementor-defined, §13.18.43 GR2, and only
    // self-consistency matters since producer and consumer run on this connector). WRITE/REWRITE outside the
    // bounds is the GR14 '44'; a record-sequential REWRITE must also match the replaced record's size
    // (§14.9.35 GR16).
    private readonly int _varyMin = -1, _varyMax = -1;
    private bool IsVarying => _varyMin >= 0;

    /// <summary>The length of the most recently read record (the frame length on a varying file, the record
    /// width on a fixed one) — the value a RECORD VARYING … DEPENDING item receives (ISO §13.18.43 GR15).</summary>
    public int LastReadLength { get; private set; }

    /// <summary>True for a SELECT OPTIONAL file (OPEN INPUT on a missing file is 05 + EOF, not 35).</summary>
    public bool IsOptional { get; set; }

    /// <summary>True between a successful OPEN and the matching CLOSE.</summary>
    public bool IsOpen => _reader is not null || _writer is not null || _optionalAbsentInput;

    /// <summary>The latest I-O status (ISO §9.1.13). "00" until the first operation.</summary>
    public string Status { get; private set; } = FileStatusCode.Success;

    /// <summary>The open-mode view for USE-declarative mode scoping (ISO §14.9.49.4 GR6b–e): <c>(int)</c> of the
    /// mode while open OR the ATTEMPTED mode of a failed OPEN ("in the process of being opened"); −1 after a
    /// successful CLOSE / before any OPEN (§9.1.4 — the file is then in no mode).</summary>
    public int OpenModeView => _modeKnown ? (int)_mode : -1;
    private bool _modeKnown;

    /// <summary>Set the I-O status directly (for facade-level conditions: a locked-file OPEN, a REEL/UNIT CLOSE).</summary>
    public void SetStatus(string status) => Status = status;

    public SequentialFile(string hostPath, int recordWidth, bool lineSequential, int varyMin = -1, int varyMax = -1)
    {
        _hostPath = hostPath;
        _recordWidth = recordWidth < 1 ? 1 : recordWidth;
        _lineSequential = lineSequential;
        _varyMin = varyMin;
        _varyMax = varyMax;
    }

    // ── OPEN / CLOSE ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>OPEN the file in <paramref name="mode"/> (ISO §14.9.25 / §9.1.13.4). Sets and returns the status.</summary>
    public string Open(FileOpenMode mode)
    {
        if (IsOpen) return Status = FileStatusCode.FileAlreadyOpen;
        _mode = mode;
        _modeKnown = true;   // a FAILED open still records the attempted mode (GR6b "being opened")
        _afterAdvancing = false;
        _optionalAbsentInput = false;
        _lastReadUnsuccessful = false;
        _prevOpWasSuccessfulRead = false;
        _lastReadBlockStart = -1;
        _readOffset = 0;
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
                    // I-O permits both READ and REWRITE on the one open connector (ISO §14.9.35 GR3 — REWRITE
                    // requires open mode I-O; its format-1 contract replaces the record retrieved by the last
                    // successful READ in place), so the underlying stream must open ReadWrite — Rewrite's
                    // seek-and-write path writes through the reader's BaseStream. An absent non-optional file
                    // is 35; an optional one is created (05).
                    if (!exists && !IsOptional) return Status = FileStatusCode.FileNotFound;
                    if (exists)
                        _reader = new StreamReader(new FileStream(_hostPath, FileMode.Open, FileAccess.ReadWrite),
                            Encoding.Latin1);
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
        _modeKnown = false;   // §9.1.4 — after a successful CLOSE the file is in no open mode
        return Status = FileStatusCode.Success;
    }

    // ── WRITE ────────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Plain <c>WRITE record</c> (ISO §14.9.46): line-sequential writes the trimmed image as a line;
    /// record-sequential writes a fixed-width block; a VARYING file writes a length-framed record of
    /// <paramref name="length"/> bytes (the DEPENDING item's content, §13.18.43 GR13a) or the image's own length
    /// (GR13b/c), failing with '44' outside the declared bounds (GR14). Valid only when open OUTPUT/EXTEND
    /// (else 48).</summary>
    public string Write(string image, int length = -1)
    {
        if (!IsOpen || _writer is null) return Status = FileStatusCode.WriteNotOpenForOutput;
        if (_mode is not (FileOpenMode.Output or FileOpenMode.Extend)) return Status = FileStatusCode.WriteNotOpenForOutput;
        // On a PRINT file (this connector has seen print-control advancing), an omitted ADVANCING phrase still
        // line-advances (ISO §14.9.46 GR — a raw fixed-width block would weld onto the previous line). In the
        // pending-advance stream model (each AFTER-write leads with its newline; CLOSE supplies the final one),
        // the write-then-advance shape reproduces the print stream the golden corpus encodes.
        if (_afterAdvancing && !_lineSequential) return WriteAdvancing(image, 1, before: true);
        if (IsVarying)
        {
            int len = length >= 0 ? length : image.Length;
            if (len < _varyMin || len > _varyMax)
                return Status = FileStatusCode.RecordSizeViolation;   // '44' §13.18.43 GR14a
            if (_lineSequential) _writer.WriteLine(Fit(image, len).TrimEnd());
            else { WriteFrameLength(len); _writer.Write(Fit(image, len)); }
        }
        else if (_lineSequential) _writer.WriteLine(image.TrimEnd());
        else _writer.Write(Fit(image, _recordWidth));
        _afterAdvancing = false;
        return Status = FileStatusCode.Success;
    }

    /// <summary>Write a record's 4-byte little-endian length prefix (the varying-file framing — chars 0–255 map
    /// 1:1 to bytes under the Latin1 writer).</summary>
    private void WriteFrameLength(int len)
    {
        _writer!.Write((char)(len & 0xFF));
        _writer.Write((char)((len >> 8) & 0xFF));
        _writer.Write((char)((len >> 16) & 0xFF));
        _writer.Write((char)((len >> 24) & 0xFF));
    }

    /// <summary>Print-control <c>WRITE record {BEFORE|AFTER} ADVANCING {n LINES | PAGE}</c> (ISO §14.9.46 GR): for
    /// AFTER, advance then write the trimmed image; for BEFORE, write then advance. <paramref name="lines"/> = -1
    /// means ADVANCING PAGE (a form feed). The leading/trailing newline structure matches the legacy print stream.</summary>
    public string WriteAdvancing(string image, int lines, bool before)
    {
        if (!IsOpen || _writer is null) return Status = FileStatusCode.WriteNotOpenForOutput;
        if (_mode is not (FileOpenMode.Output or FileOpenMode.Extend)) return Status = FileStatusCode.WriteNotOpenForOutput;
        _afterAdvancing = true;
        string text = PrintSafe(image.TrimEnd());
        if (before) { _writer.Write(text); Advance(lines); }
        else { Advance(lines); _writer.Write(text); }
        return Status = FileStatusCode.Success;
    }

    /// <summary>The PRINT-stream character mapping: a character above the 7-bit range writes as <c>?</c> — the
    /// implementor-defined runtime print encoding the NIST golden corpus encodes (the legacy print writer's
    /// ASCII fallback: HIGH-VALUE prints as <c>?</c>, NUL passes through — NC107A's figurative-constant
    /// information lines). Applies ONLY to print-control writes; a record (data-file) WRITE keeps its raw
    /// characters — a record image must round-trip through READ byte-exact.</summary>
    private static string PrintSafe(string s)
    {
        if (!s.Any(c => c > '\x7f')) return s;
        var a = s.ToCharArray();
        for (int i = 0; i < a.Length; i++) if (a[i] > '\x7f') a[i] = '?';
        return new string(a);
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
            LastReadLength = Math.Min(line.Length, _recordWidth);
            image = Fit(line, _recordWidth);
        }
        else if (IsVarying)
        {
            // Length-framed record: 4-byte LE prefix + payload (see the framing note at the field declarations).
            var pre = new char[4];
            if (FillChars(pre, 4) < 4) { _prevOpWasSuccessfulRead = false; _lastReadUnsuccessful = true; Status = FileStatusCode.AtEnd; return false; }
            int len = pre[0] | (pre[1] << 8) | (pre[2] << 16) | (pre[3] << 24);
            var buf = new char[len];
            int n = FillChars(buf, len);
            _lastReadBlockStart = _reader.BaseStream.CanSeek ? _readOffset + 4 : -1;
            _readOffset += 4 + n;
            LastReadLength = n;
            image = new string(buf, 0, n).PadRight(_recordWidth, ' ');
        }
        else
        {
            // The block start is the LOGICAL offset (characters consumed so far — 1:1 with bytes under Latin1),
            // never BaseStream.Position: the StreamReader buffers ahead, so the base position is the buffer-fill
            // boundary, not the read position.
            var buf = new char[_recordWidth];
            int n = FillChars(buf, _recordWidth);
            if (n == 0) { _prevOpWasSuccessfulRead = false; _lastReadUnsuccessful = true; Status = FileStatusCode.AtEnd; return false; }
            _lastReadBlockStart = _reader.BaseStream.CanSeek ? _readOffset : -1;
            _readOffset += n;
            LastReadLength = n;
            image = new string(buf, 0, n).PadRight(_recordWidth, ' ');
        }
        _prevOpWasSuccessfulRead = true;
        Status = FileStatusCode.Success;
        return true;
    }

    /// <summary>Fill exactly <paramref name="count"/> characters (or to end-of-stream): StreamReader.Read may
    /// return fewer than requested at an internal buffer boundary, which is not end-of-data.</summary>
    private int FillChars(char[] buf, int count)
    {
        int total = 0;
        while (total < count)
        {
            int n = _reader!.Read(buf, total, count - total);
            if (n == 0) break;
            total += n;
        }
        return total;
    }

    // ── REWRITE ──────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Sequential <c>REWRITE record</c> (ISO §14.9.35): replace the last-read record in place. Valid only
    /// when open I-O (else 49) and the immediately-previous op was a successful READ (else 43). On a varying file
    /// the rewritten record's length (<paramref name="length"/> = the DEPENDING item's content, GR13a, or the
    /// image's own length, GR13b/c) must lie within the declared bounds (GR20 → '44') AND — record sequential —
    /// equal the size of the record being replaced (GR16 → '44'; the in-place frame cannot change size).</summary>
    public string Rewrite(string image, int length = -1)
    {
        if (!IsOpen || _mode != FileOpenMode.IO) return Status = FileStatusCode.DeleteRewriteNotOpenForIO;
        if (!_prevOpWasSuccessfulRead) return Status = FileStatusCode.NoSuccessfulReadBeforeDeleteRewrite;
        _prevOpWasSuccessfulRead = false;
        int len = IsVarying ? (length >= 0 ? length : image.Length) : _recordWidth;
        if (IsVarying && (len < _varyMin || len > _varyMax))
            return Status = FileStatusCode.RecordSizeViolation;       // '44' §14.9.35 GR20
        if (IsVarying && len != LastReadLength)
            return Status = FileStatusCode.RecordSizeViolation;       // '44' §14.9.35 GR16 (record sequential)
        if (!_lineSequential && _lastReadBlockStart >= 0 && _reader is { BaseStream: { CanSeek: true, CanWrite: true } stream })
        {
            long resume = stream.Position;
            stream.Seek(_lastReadBlockStart, SeekOrigin.Begin);
            byte[] bytes = Encoding.Latin1.GetBytes(Fit(image, len));
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
