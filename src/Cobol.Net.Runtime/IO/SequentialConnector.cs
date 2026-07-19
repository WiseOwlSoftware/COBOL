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
/// read-position state machine live on the shared <see cref="FileConnector"/> base; the sequential-specific
/// stream/framing/LINAGE machinery is here.
/// </summary>
public sealed class SequentialConnector : FileConnector
{
    private readonly bool _lineSequential;

    private StreamReader? _reader;
    private StreamWriter? _writer;
    private bool _afterAdvancing;        // a WRITE … ADVANCING happened → a trailing newline is written at CLOSE

    // The byte offset of the most recently read record's fixed-width block (for the in-place record-sequential
    // REWRITE) and the LOGICAL read offset it derives from. The logical offset counts characters CONSUMED from
    // the reader (Latin1 — one byte per character; a record-sequential file is pure fixed-width blocks): the
    // StreamReader BUFFERS, so BaseStream.Position is the buffer-fill boundary, never the read position —
    // deriving the block start from it corrupted the rewrite target (IX106A REWRITE-TEST-GF-02).
    private long _lastReadBlockStart = -1;
    private long _readOffset;

    // ── Record-lock identity (ISO §9.1.16 on sequential organization) ────────────────────────────────────────
    // A sequential record's lock identity is its 1-based ORDINAL position in the physical file — two connectors
    // reading the same physical file agree on ordinals, and the successor relationship (§14.9.51 GR17) makes the
    // ordinal stable. Reads count from OPEN (INPUT/I-O always position at the start); writes count from a base
    // seeded by the registry for a SHARING-active connector (OUTPUT = 0; EXTEND = the pre-existing record count,
    // so appended ordinals continue the file's numbering). Unshared connectors never consult these.
    private long _readOrdinal;        // ordinal of the record most recently made available by Read
    private long _writeBase = -1;     // -1 = not seeded (unshared / not yet opened shared)
    private long _writesDone;         // successful record writes since OPEN

    /// <summary>The ordinal the NEXT sequential Read would deliver (the §14.9.30 GR9 pre-read conflict target —
    /// knowable BEFORE the read because sequential retrieval advances by exactly one record).</summary>
    internal long NextReadOrdinal => _readOrdinal + 1;

    /// <summary>True when a Read executed now would reach the physical-read stage (open INPUT/I-O, no '46'
    /// poison, a live stream) — the registry's lock pre-check runs only then, so a mode/position failure keeps
    /// its own status ('47'/'46'/'10') rather than a premature '51'.</summary>
    internal bool ReadEligible => IsOpen && Mode is FileOpenMode.Input or FileOpenMode.IO
        && !OptionalAbsent && !LastReadUnsuccessful && _reader is not null;

    /// <inheritdoc/>
    public override string LastReadRecordId =>
        _readOrdinal > 0 ? _readOrdinal.ToString(System.Globalization.CultureInfo.InvariantCulture) : "";

    /// <inheritdoc/>  (a sequential REWRITE replaces the record obtained by the last successful READ, §14.9.35)
    public override string MutationTargetRecordId(string recordImage) => LastReadRecordId;

    /// <inheritdoc/>
    public override string LastWrittenRecordId => _writeBase >= 0 && _writesDone > 0
        ? (_writeBase + _writesDone).ToString(System.Globalization.CultureInfo.InvariantCulture) : "";

    /// <summary>Seed the write-ordinal base for a SHARING-active connector (called by the registry after a
    /// successful shared OPEN): OUTPUT starts a fresh file at 0; EXTEND continues the existing numbering
    /// (§14.9.51 GR18 — appended records succeed the records present at OPEN), so the pre-existing record
    /// count is derived from the physical shape (frames / lines / fixed-width blocks).</summary>
    internal void SeedSharedWriteBase()
    {
        _writeBase = 0;
        _writesDone = 0;
        if (Mode != FileOpenMode.Extend || !File.Exists(HostPath)) return;
        if (IsVarying) _writeBase = RecordFraming.ReadStore(HostPath).Count;
        else if (_lineSequential) _writeBase = File.ReadLines(HostPath).Count();
        else _writeBase = RecordWidth > 0 ? new FileInfo(HostPath).Length / RecordWidth : 0;
    }

    // A varying file's records are length-framed on disk (the ONE RecordFraming 4-byte little-endian length
    // prefix per record — the same framing the keyed connectors' store uses; the physical format is
    // implementor-defined, §13.18.43 GR2, and only self-consistency matters since producer and consumer run on
    // this connector). WRITE/REWRITE outside the VaryMin/VaryMax bounds is the GR14 '44'; a record-sequential
    // REWRITE must also match the replaced record's size (§14.9.35 GR16).

    // ── LINAGE logical-page state (ISO §13.18.34 / §14.9.51 GR26–28) ─────────────────────────────────────────
    // The LINAGE feature is COUNTER-ONLY on the physical stream: each logical page is contiguous to the next
    // with no additional spacing (§13.18.34 GR8 — no margin blank lines, nothing emitted at page wrap), so the
    // connector adds only the counter machine + the end-of-page flag over the unchanged pending-advance stream.
    // The evaluator closure is the ONE mechanism for both operand forms (GR6a literals are constant lambdas;
    // GR6b data-names defer the field reads to the evaluation points).
    private Func<(int Body, int Footing, int Top, int Bottom)>? _linageEval;
    private int _pageBody;      // page size — the writable page-body line count (GR2)
    private int _footing;       // footing start (GR3 — footing area = [footing, page size] inclusive); 0 = none
    private int _top, _bottom;  // top/bottom margins (GR4/GR5) — counted into the logical page (GR1), unprinted

    /// <summary>True when the FD carries a LINAGE clause (the registration installed an evaluator).</summary>
    public bool HasLinage => _linageEval is not null;

    /// <summary>The LINAGE-COUNTER register (ISO §8.4.3.14): the line number at which the device is positioned
    /// within the current page body (§13.18.34 GR7). Only this connector (the I-O control system) modifies it (GR7b).</summary>
    public long LinageCounter { get; private set; }

    /// <summary>The end-of-page condition of the most recent WRITE (ISO §14.9.51 GR26): page overflow (GR26a) or
    /// printing/spacing within the footing area (GR26b). Reset at the start of every counter-advancing write.</summary>
    public bool EndOfPage { get; private set; }

    /// <summary>Install the LINAGE evaluator (emitted right after registration for a LINAGE FD).</summary>
    public void SetLinage(Func<(int Body, int Footing, int Top, int Bottom)> eval) => _linageEval = eval;

    /// <summary>Evaluate the LINAGE operand values for the (next) logical page (ISO §13.18.34 GR6: at OPEN OUTPUT
    /// completion, during WRITE ADVANCING PAGE, and during a page-overflow WRITE — "the value applies to the next
    /// logical page"). GR6's value rules (page size &gt; 0; 0 &lt; footing ≤ page size — footing 0 here = the phrase
    /// is absent, GR1) violated ⇒ the EC-I-O-LINAGE exception condition (§13.18.34 GR6) — the EC subsystem is a
    /// later slice, so the seam fails LOUD (COBOLNET_DESIGN §1.4), never a silent bad page model.</summary>
    private void EvaluateLinage()
    {
        var (body, footing, top, bottom) = _linageEval!();
        if (body <= 0 || footing < 0 || footing > body)
            throw new InvalidOperationException(
                $"EC-I-O-LINAGE: LINAGE values page-size={body}, footing={footing} violate ISO §13.18.34 GR6 "
                + "(page size > 0; 0 < footing <= page size); the EC exception-condition machinery is a later slice");
        (_pageBody, _footing, _top, _bottom) = (body, footing, top, bottom);
    }

    /// <summary>
    /// Advance the LINAGE-COUNTER for one WRITE (ported VERBATIM from the legacy
    /// <c>SequentialFileHandler.AdvanceLinageCounter</c>, proven over the SQ goldens) plus the GR6b page-transition
    /// re-evaluation. <paramref name="lines"/> &lt; 0 = ADVANCING PAGE. Rules:
    /// <list type="bullet">
    /// <item>ADVANCING PAGE resets the counter to 1 (§13.18.34 GR7c1); no observable end-of-page (§14.9.51 SR18
    ///   bars PAGE+EOP in one statement, so the flag stays false).</item>
    /// <item>ADVANCING n adds n (GR7c2); a plain WRITE adds 1 (GR7c3 — the caller passes 1).</item>
    /// <item>Counter past the page body ⇒ page overflow (§14.9.51 GR26a): the device repositions to the FIRST
    ///   line of the next logical page, counter := 1 (GR7c4 — never a modulo carry), overflow end-of-page.</item>
    /// <item>Else, FOOTING specified and counter at/past the footing start ⇒ footing end-of-page (GR26b; the
    ///   footing area is inclusive of the page size, §13.18.34 GR3, so counter == body is FOOTING, and overflow
    ///   fires only when the positioning actually passes the body).</item>
    /// <item>GR6b2/3: at the two page transitions — AFTER the overflow decision was made against the OLD page
    ///   body — re-evaluate the operand values; they apply to the NEXT logical page (§13.18.34 GR6).</item>
    /// </list>
    /// The caller invokes this AFTER the physical write — the AT END-OF-PAGE branch then observes the
    /// post-advance counter (SQ201M's footing lines print the triggering write's line number).
    /// </summary>
    private void AdvanceLinageCounter(int lines)
    {
        EndOfPage = false;   // reset at the start of every counter-advancing write (the legacy entry reset)
        if (_pageBody <= 0) return;
        if (lines < 0)
        {
            // ADVANCING PAGE: the counter resets to one on the new page (§13.18.34 GR7c1).
            LinageCounter = 1;
            EvaluateLinage();   // GR6b2 — values for the NEXT logical page
            return;
        }
        // ADVANCING n (n >= 0) or plain WRITE (n = 1): the counter is incremented (GR7c2/c3).
        LinageCounter += lines;
        if (LinageCounter > _pageBody)
        {
            // Page overflow (§14.9.51 GR26a): the line does not fit in the page body — the device repositions
            // to the first writable line of the succeeding page and the counter resets to 1 (GR7c4).
            LinageCounter = 1;
            EndOfPage = true;
            EvaluateLinage();   // GR6b3 — after the overflow decision against the OLD body; next-page values
        }
        else if (_footing > 0 && LinageCounter >= _footing)
        {
            // Footing-area end-of-page (§14.9.51 GR26b): FOOTING is specified and this WRITE prints or spaces
            // within the footing area (counter at/past the footing start, still within the page body).
            EndOfPage = true;
        }
    }

    /// <summary>True between a successful OPEN and the matching CLOSE (an absent-OPTIONAL INPUT open counts —
    /// the connector is open at EOF with no physical stream).</summary>
    public override bool IsOpen => _reader is not null || _writer is not null || OptionalAbsent;

    public SequentialConnector(string hostPath, int recordWidth, bool lineSequential,
        int varyMin = -1, int varyMax = -1)
        : base(hostPath, recordWidth, varyMin, varyMax)
    {
        _lineSequential = lineSequential;
    }

    // ── OPEN / CLOSE ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The sequential OPEN body (ISO §14.9.25 / §9.1.13.4) — the shared preamble/guards live on
    /// <see cref="FileConnector.Open"/>.</summary>
    protected override string OpenCore(FileOpenMode mode)
    {
        _afterAdvancing = false;
        _lastReadBlockStart = -1;
        _readOffset = 0;
        _readOrdinal = 0;
        _writeBase = -1;   // unshared default; the registry seeds a sharing-active connector (§9.1.16)
        _writesDone = 0;
        bool exists = File.Exists(HostPath);
        {
            switch (mode)
            {
                case FileOpenMode.Input:
                    if (!exists)
                    {
                        if (!IsOptional) return FileStatusCode.FileNotFound;
                        OptionalAbsent = true;                 // positioned at EOF (ISO §9.1.13.2 item 5b)
                        return FileStatusCode.OptionalFileNotFound;
                    }
                    // A SHARING-active connector's stream permits other connectors' handles (§9.1.15 — the
                    // Table-19 registry is the sharing arbiter, not the OS handle); unshared keeps the default.
                    _reader = SharedStreams
                        ? new StreamReader(new FileStream(HostPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
                            Encoding.Latin1)
                        : new StreamReader(HostPath, Encoding.Latin1);
                    break;

                case FileOpenMode.Output:
                    _writer = SharedStreams
                        ? new StreamWriter(new FileStream(HostPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite),
                            Encoding.Latin1) { NewLine = "\r\n" }
                        : new StreamWriter(HostPath, append: false, Encoding.Latin1) { NewLine = "\r\n" };
                    break;

                case FileOpenMode.Extend:
                    if (!exists && !IsOptional) return FileStatusCode.FileNotFound;
                    _writer = SharedStreams
                        ? new StreamWriter(new FileStream(HostPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite),
                            Encoding.Latin1) { NewLine = "\r\n" }
                        : new StreamWriter(HostPath, append: true, Encoding.Latin1) { NewLine = "\r\n" };
                    if (!exists && IsOptional) return FileStatusCode.OptionalFileNotFound;
                    break;

                case FileOpenMode.IO:
                    // I-O permits both READ and REWRITE on the one open connector (ISO §14.9.35 GR3 — REWRITE
                    // requires open mode I-O; its format-1 contract replaces the record retrieved by the last
                    // successful READ in place), so the underlying stream must open ReadWrite — Rewrite's
                    // seek-and-write path writes through the reader's BaseStream. An absent non-optional file
                    // is 35; an optional one is created (05).
                    if (!exists && !IsOptional) return FileStatusCode.FileNotFound;
                    if (exists)
                        _reader = new StreamReader(new FileStream(HostPath, FileMode.Open, FileAccess.ReadWrite,
                            SharedStreams ? FileShare.ReadWrite : FileShare.Read), Encoding.Latin1);
                    else { _writer = new StreamWriter(HostPath, append: false, Encoding.Latin1) { NewLine = "\r\n" }; }
                    if (!exists && IsOptional) return FileStatusCode.OptionalFileNotFound;
                    break;
            }
            if (mode == FileOpenMode.Output && _linageEval is not null)
            {
                EvaluateLinage();    // ISO §13.18.34 GR6b1 — values read at the completion of an OPEN OUTPUT
                LinageCounter = 1;   // GR7d — the counter is set to one at OPEN OUTPUT
                EndOfPage = false;
            }
            return FileStatusCode.Success;
        }
    }

    /// <summary>The sequential CLOSE body (ISO §14.9.7). A WRITE … ADVANCING stream is terminated with a
    /// trailing newline (matching the legacy print-control behavior) — the not-open guard lives on
    /// <see cref="FileConnector.Close"/>.</summary>
    protected override string CloseCore()
    {
        if (_afterAdvancing) { _writer?.Write("\r\n"); _afterAdvancing = false; }
        _reader?.Dispose();
        _writer?.Dispose();
        _reader = null;
        _writer = null;
        OptionalAbsent = false;
        ModeKnown = false;   // §9.1.4 — after a successful CLOSE the file is in no open mode
        return FileStatusCode.Success;
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
        if (Mode is not (FileOpenMode.Output or FileOpenMode.Extend)) return Status = FileStatusCode.WriteNotOpenForOutput;
        // On a PRINT file (this connector has seen print-control advancing) — or a LINAGE file, which is
        // line-oriented from its FIRST write (the FILES deep-dive rule; its plain WRITE acts as AFTER
        // ADVANCING 1, ISO §14.9.51 GR25, and the counter advances by one, §13.18.34 GR7c3) — an omitted
        // ADVANCING phrase still line-advances (a raw fixed-width block would weld onto the previous line). In
        // the pending-advance stream model (each AFTER-write leads with its newline; CLOSE supplies the final
        // one), the write-then-advance shape reproduces the print stream the golden corpus encodes.
        if ((_afterAdvancing || HasLinage) && !_lineSequential) return WriteAdvancing(image, 1, before: true);
        if (IsVarying)
        {
            int len = length >= 0 ? length : image.Length;
            if (len < VaryMin || len > VaryMax)
                return Status = FileStatusCode.RecordSizeViolation;   // '44' §13.18.43 GR14a
            if (_lineSequential) _writer.WriteLine(Fit(image, len).TrimEnd());
            else { RecordFraming.WritePrefix(_writer, len); _writer.Write(Fit(image, len)); }
        }
        else if (_lineSequential) _writer.WriteLine(image.TrimEnd());
        else _writer.Write(Fit(image, RecordWidth));
        _writesDone++;   // the record just released is ordinal base+N (§9.1.16 lock identity; §14.9.51 GR11)
        _afterAdvancing = false;
        // GR7c3 (§13.18.34): a plain WRITE to a LINAGE file advances the counter by one. Only the
        // line-sequential/varying shapes reach here (the record-sequential LINAGE write rerouted above).
        if (_linageEval is not null) AdvanceLinageCounter(1);
        return Status = FileStatusCode.Success;
    }

    /// <summary>Print-control <c>WRITE record {BEFORE|AFTER} ADVANCING {n LINES | PAGE}</c> (ISO §14.9.46 GR): for
    /// AFTER, advance then write the trimmed image; for BEFORE, write then advance. <paramref name="lines"/> = -1
    /// means ADVANCING PAGE (a form feed). The leading/trailing newline structure matches the legacy print stream.</summary>
    public string WriteAdvancing(string image, int lines, bool before)
    {
        if (!IsOpen || _writer is null) return Status = FileStatusCode.WriteNotOpenForOutput;
        if (Mode is not (FileOpenMode.Output or FileOpenMode.Extend)) return Status = FileStatusCode.WriteNotOpenForOutput;
        _afterAdvancing = true;
        string text = PrintSafe(image.TrimEnd());
        if (before) { _writer.Write(text); Advance(lines); }
        else { Advance(lines); _writer.Write(text); }
        // The LINAGE counter advances as part of the write, AFTER the physical presentation (the legacy
        // ordering): an AT END-OF-PAGE branch then reads the POST-advance counter of the triggering write
        // (§13.18.34 GR7c; SQ201M's footing lines print line 45).
        if (_linageEval is not null) AdvanceLinageCounter(lines);
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

    /// <summary>Print-control <c>WRITE record BEFORE ADVANCING n AFTER ADVANCING m</c> (ISO §14.9.51 GR25e/GR25f,
    /// COBOL-2023): present the trimmed image at the CURRENT line, then advance by the BEFORE amount and by the AFTER
    /// amount — both after presentation (SR17 forbids PAGE, so neither is a form feed). LINAGE-COUNTER increments by
    /// n+m.</summary>
    public string WriteBeforeAndAfter(string image, int beforeLines, int afterLines)
    {
        if (!IsOpen || _writer is null) return Status = FileStatusCode.WriteNotOpenForOutput;
        if (Mode is not (FileOpenMode.Output or FileOpenMode.Extend)) return Status = FileStatusCode.WriteNotOpenForOutput;
        _afterAdvancing = true;
        _writer.Write(PrintSafe(image.TrimEnd()));
        // Two DISTINCT advancing operations (GR25e then GR25f): advance and count each SEPARATELY so a page-boundary
        // crossing WITHIN the BEFORE advance is handled by its own §14.9.51 GR26/GR7c overflow logic before the
        // AFTER advance runs (a single combined increment would mis-handle a boundary between the two).
        Advance(beforeLines);
        if (_linageEval is not null) AdvanceLinageCounter(beforeLines);
        Advance(afterLines);
        if (_linageEval is not null) AdvanceLinageCounter(afterLines);
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
        image = new string(' ', RecordWidth);
        if (!IsOpen) { Status = FileStatusCode.ReadNotOpenForInput; return false; }
        if (Mode is FileOpenMode.Output or FileOpenMode.Extend) { Status = FileStatusCode.ReadNotOpenForInput; return false; }
        if (OptionalAbsent) { PrevOpWasSuccessfulRead = false; LastReadUnsuccessful = true; Status = FileStatusCode.AtEnd; return false; }
        if (LastReadUnsuccessful) { Status = FileStatusCode.NoValidNextRecord; return false; }
        if (_reader is null) { Status = FileStatusCode.ReadNotOpenForInput; return false; }

        // §14.9.35 GR14 / §9.1.13.2 item 3: a RECORD-sequential physical record whose length is outside the file's
        // min/max record size is a SUCCESSFUL read with status '04' (the record is still delivered). Line-sequential
        // is excluded — its short/long conditions are '06'/'09', never '04'.
        bool shortLong = false;
        if (_lineSequential)
        {
            string? line = _reader.ReadLine();
            if (line is null) { PrevOpWasSuccessfulRead = false; LastReadUnsuccessful = true; Status = FileStatusCode.AtEnd; return false; }
            LastReadLength = Math.Min(line.Length, RecordWidth);
            image = Fit(line, RecordWidth);
        }
        else if (IsVarying)
        {
            // Length-framed record: 4-byte LE prefix + payload (see the framing note at the field declarations).
            var pre = new char[4];
            if (FillChars(pre, 4) < 4) { PrevOpWasSuccessfulRead = false; LastReadUnsuccessful = true; Status = FileStatusCode.AtEnd; return false; }
            int len = RecordFraming.PrefixLength(pre);
            var buf = new char[len];
            int n = FillChars(buf, len);
            _lastReadBlockStart = _reader.BaseStream.CanSeek ? _readOffset + 4 : -1;
            _readOffset += 4 + n;
            LastReadLength = n;
            image = new string(buf, 0, n).PadRight(RecordWidth, ' ');
            if (n < VaryMin || n > VaryMax) shortLong = true;   // outside the varying record min/max (§14.9.35 GR14)
        }
        else
        {
            // The block start is the LOGICAL offset (characters consumed so far — 1:1 with bytes under Latin1),
            // never BaseStream.Position: the StreamReader buffers ahead, so the base position is the buffer-fill
            // boundary, not the read position.
            var buf = new char[RecordWidth];
            int n = FillChars(buf, RecordWidth);
            if (n == 0) { PrevOpWasSuccessfulRead = false; LastReadUnsuccessful = true; Status = FileStatusCode.AtEnd; return false; }
            _lastReadBlockStart = _reader.BaseStream.CanSeek ? _readOffset : -1;
            _readOffset += n;
            LastReadLength = n;
            image = new string(buf, 0, n).PadRight(RecordWidth, ' ');
            // Fixed-length record sequential: min == max == RecordWidth, so a partial (short) final record is '04'.
            // A longer-than-max record cannot occur (the buffer is read in RecordWidth chunks). n == 0 is EOF above.
            if (n < RecordWidth) shortLong = true;
        }
        PrevOpWasSuccessfulRead = true;
        _readOrdinal++;   // the record just made available is ordinal N+1 (§9.1.16 lock identity)
        Status = shortLong ? FileStatusCode.RecordLengthShortLong : FileStatusCode.Success;
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
        if (!IsOpen || Mode != FileOpenMode.IO) return Status = FileStatusCode.DeleteRewriteNotOpenForIO;
        if (!PrevOpWasSuccessfulRead) return Status = FileStatusCode.NoSuccessfulReadBeforeDeleteRewrite;
        PrevOpWasSuccessfulRead = false;
        int len = IsVarying ? (length >= 0 ? length : image.Length) : RecordWidth;
        if (IsVarying && (len < VaryMin || len > VaryMax))
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
}
