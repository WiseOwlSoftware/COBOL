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
    private static readonly Dictionary<string, int> _lastRecordLength = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> _afterAdvancingFiles = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> _lockedFiles = new(StringComparer.OrdinalIgnoreCase);
    // Open-mode scope of each currently-open file, for dispatching open-mode-scoped USE declaratives
    // (USE AFTER ERROR PROCEDURE ON INPUT/OUTPUT/I-O/EXTEND). Encoded as the UseScope.* values below;
    // set on the OPEN attempt (so a failed OPEN still routes to the mode's declarative), cleared on CLOSE.
    private static readonly Dictionary<string, int> _openModeScope = new(StringComparer.OrdinalIgnoreCase);

    // Files whose USE declarative is currently executing. ISO §14.9.49.4 GR2: a USE procedure must not be
    // re-invoked while it is still active (else EC-FLOW-USE). Bracketed by Enter/ExitUseDeclarative around the
    // declarative PERFORM so a declarative whose own body does I/O on its file (e.g. CLOSE) does not recurse
    // into itself — RL111A's D-CLOSE-FILES re-dispatched forever (a CLOSE on the already-closed file → status
    // 42 → re-fire the same declarative) and stack-overflowed.
    private static readonly HashSet<string> _activeUseDeclaratives = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Mark a file's USE declarative active so a re-entrant exception during its execution does not
    /// re-invoke it (ISO §14.9.49.4 GR2). Emitted by the lowerer immediately before the declarative PERFORM.</summary>
    public static void EnterUseDeclarative(string fileName) => _activeUseDeclaratives.Add(fileName);

    /// <summary>Clear a file's active-declarative flag when its USE procedure returns (emitted after the PERFORM).</summary>
    public static void ExitUseDeclarative(string fileName) => _activeUseDeclaratives.Remove(fileName);

    /// <summary>
    /// Initialize the file manager. Called once at program start.
    /// </summary>
    public static void Init()
    {
        _manager?.Dispose();
        _manager = new CobolFileManager();
        _lastStatus.Clear();
        _lastRecordLength.Clear();
        _afterAdvancingFiles.Clear();
        _lockedFiles.Clear();
        _openModeScope.Clear();
        _activeUseDeclaratives.Clear();
        ReportWriterRuntime.Reset();
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
        EnsureManager();

        // Register-if-absent. Every program (run-unit main AND every CALLed subprogram) registers its
        // own internal file connectors from its Entry method (ISO §14.6 — a called program's connectors
        // are established when it is activated). The runtime keys connectors by bare COBOL file name in
        // ONE shared manager, so when a subprogram SELECTs a name the caller already registered — the
        // NIST IC suite's shared PRINT-FILE: the main opens it, then CALLs a subprogram that also has
        // `SELECT PRINT-FILE` and WRITEs the same report — the existing OPEN connector must be kept, not
        // replaced with a fresh closed one (which would lose the caller's open file → the report would
        // come out empty: IC101A/IC103A/…). A genuinely-new name (IC115A's SQ-FS3, absent from the
        // manager) is registered normally. The per-program _filesRegistered flag (CilEmitter) already
        // prevents a program re-registering its OWN files across repeat CALLs; this guard additionally
        // prevents a subprogram from clobbering a DIFFERENT program's same-named open connector.
        if (_manager!.GetHandler(cobolName) != null)
            return;

        IFileHandler handler = organization switch
        {
            "INDEXED" => new IndexedFileHandler(externalPath, recordLength, keyOffset, keyLength),
            // For RELATIVE files keyLength carries the RELATIVE KEY data item's digit capacity
            // (relative files address records by slot number, not a record-embedded key range).
            "RELATIVE" => new RelativeFileHandler(externalPath, recordLength, keyLength),
            _ => new SequentialFileHandler(externalPath, recordLength, lineSequential)
        };
        _manager.RegisterFile(cobolName, handler);
        _lastStatus[cobolName] = FileStatus.Success;
    }

    /// <summary>
    /// Mark a file as OPTIONAL (SELECT OPTIONAL). When OPEN INPUT on a missing file,
    /// returns status "05" instead of "35".
    /// </summary>
    public static void SetFileOptional(string cobolName)
    {
        EnsureManager();
        var handler = _manager!.GetHandler(cobolName);
        if (handler is SequentialFileHandler seq) seq.IsOptional = true;
        else if (handler is IndexedFileHandler idx) idx.IsOptional = true;
        else if (handler is RelativeFileHandler rel) rel.IsOptional = true;
    }

    /// <summary>
    /// Set the access mode for a RELATIVE file: sequential=true (ACCESS SEQUENTIAL) appends on WRITE
    /// and uses the current record for REWRITE/DELETE; sequential=false (RANDOM/DYNAMIC) positions
    /// WRITE/REWRITE/DELETE by the RELATIVE KEY (the pending key set just before the operation).
    /// </summary>
    public static void SetRelativeAccess(string cobolName, bool sequential)
    {
        EnsureManager();
        if (_manager!.GetHandler(cobolName) is RelativeFileHandler rel)
            rel.SequentialAccess = sequential;
    }

    /// <summary>
    /// Set the access mode for an INDEXED file: sequential=true (ACCESS SEQUENTIAL) deletes/rewrites the
    /// current (last-read) record — requiring an immediately preceding successful READ (status 43 if not,
    /// ISO §9.1.13.6) — and may not change the primary key on REWRITE (21); sequential=false
    /// (RANDOM/DYNAMIC) deletes/rewrites the record identified by the primary key with no prior read.
    /// </summary>
    public static void SetIndexedAccess(string cobolName, bool sequential)
    {
        EnsureManager();
        if (_manager!.GetHandler(cobolName) is IndexedFileHandler ix)
            ix.SequentialAccess = sequential;
    }

    /// <summary>Mark an INDEXED file as having variable-length records (RECORD IS VARYING or multiple 01
    /// sizes) so each record is stored at its actual length and persisted length-framed. <paramref
    /// name="minSize"/>/<paramref name="maxSize"/> are the RECORD IS VARYING size bounds used for the
    /// ISO §9.1.13 status-44 WRITE boundary check (minSize 0 = no lower bound).</summary>
    public static void SetIndexedVarying(string cobolName, bool varying, int minSize, int maxSize)
    {
        EnsureManager();
        if (_manager!.GetHandler(cobolName) is IndexedFileHandler ix)
        {
            ix.IsRecordVarying = varying;
            ix.MinVaryingRecordSize = minSize;
            ix.MaxVaryingRecordSize = maxSize;
        }
    }

    /// <summary>Mark a RELATIVE file as having variable-length records (RECORD IS VARYING) so each slot
    /// stores and persists its own length. Must agree with the compiler's variable-write decision.
    /// <paramref name="minSize"/>/<paramref name="maxSize"/> are the RECORD IS VARYING size bounds for the
    /// ISO §9.1.13 status-44 WRITE boundary check (minSize 0 = no lower bound).</summary>
    public static void SetRelativeVarying(string cobolName, bool varying, int minSize, int maxSize)
    {
        EnsureManager();
        if (_manager!.GetHandler(cobolName) is RelativeFileHandler rel)
        {
            rel.IsRecordVarying = varying;
            rel.MinVaryingRecordSize = minSize;
            rel.MaxVaryingRecordSize = maxSize;
        }
    }

    /// <summary>Mark a record-sequential file as having variable-length records (RECORD IS VARYING or
    /// multiple 01 sizes) so each record is stored length-framed (4-byte length prefix + data) rather
    /// than as a fixed-size slot. Must agree with the compiler's variable-write decision. <paramref
    /// name="minSize"/>/<paramref name="maxSize"/> are the RECORD IS VARYING size bounds for the
    /// ISO §9.1.13 status-44 WRITE boundary check (minSize 0 = no lower bound).</summary>
    public static void SetSequentialVarying(string cobolName, bool varying, int minSize, int maxSize)
    {
        EnsureManager();
        if (_manager!.GetHandler(cobolName) is SequentialFileHandler seq)
        {
            seq.IsRecordVarying = varying;
            seq.MinVaryingRecordSize = minSize;
            seq.MaxVaryingRecordSize = maxSize;
        }
    }

    /// <summary>
    /// Set the RELATIVE KEY value (relative record number) for the next keyed WRITE/REWRITE/DELETE/READ
    /// on a RELATIVE file. The compiler decodes the RELATIVE KEY data item PIC-aware (DISPLAY/COMP/
    /// COMP-3) and passes the integer here, so binary (COMP) keys are handled correctly.
    /// </summary>
    public static void SetRelativeKey(string cobolName, int key)
    {
        EnsureManager();
        if (_manager!.GetHandler(cobolName) is RelativeFileHandler rel)
            rel.SetPendingKey(key);
    }

    /// <summary>
    /// Set the pending prime RECORD KEY for the next RANDOM/DYNAMIC INDEXED DELETE: the alphanumeric key
    /// bytes from the record-key data item identify the record to delete (ISO §14.9.10 GR). The key is the
    /// raw bytes at <paramref name="offset"/>..<paramref name="offset"/>+<paramref name="length"/> of the
    /// record area (a DELETE writes no record, so the handler cannot otherwise see the key).
    /// </summary>
    public static void SetIndexedKey(string cobolName, byte[] buffer, int offset, int length)
    {
        EnsureManager();
        if (_manager!.GetHandler(cobolName) is IndexedFileHandler ix)
        {
            var key = new byte[length];
            System.Array.Copy(buffer, offset, key, 0, length);
            ix.SetPendingKey(key);
        }
    }

    /// <summary>
    /// Relative record number ("slot") the most recent successful WRITE/READ on a RELATIVE file acted
    /// on, for the caller to MOVE into the RELATIVE KEY data item (ISO §14.9.51/§14.9.30). 0 if n/a.
    /// </summary>
    public static int GetRelativeSlot(string cobolName)
    {
        EnsureManager();
        return _manager!.GetHandler(cobolName) is RelativeFileHandler rel ? rel.CurrentSlot : 0;
    }

    /// <summary>
    /// Set LINAGE parameters for a sequential file.
    /// </summary>
    public static void SetFileLinage(string cobolName, int body, int footing, int top, int bottom)
    {
        EnsureManager();
        if (_manager!.GetHandler(cobolName) is SequentialFileHandler seq)
        {
            seq.LinageBody = body;
            seq.LinageFooting = footing;
            seq.LinageTop = top;
            seq.LinageBottom = bottom;
        }
    }

    /// <summary>Apply LINAGE page parameters evaluated at OPEN OUTPUT (ISO §13.18.34 GR6b for data-name
    /// phrases) and reset the LINAGE-COUNTER to one (GR7d). For an integer-only LINAGE clause the params
    /// were already set at registration, but re-applying here is harmless and keeps the OPEN-time reset
    /// uniform.</summary>
    public static void InitLinage(string cobolName, int body, int footing, int top, int bottom)
    {
        EnsureManager();
        if (_manager!.GetHandler(cobolName) is SequentialFileHandler seq)
        {
            seq.LinageBody = body;
            seq.LinageFooting = footing;
            seq.LinageTop = top;
            seq.LinageBottom = bottom;
            seq.LinageCounter = 1;
        }
    }

    /// <summary>Whether the most recent WRITE to a LINAGE file raised the end-of-page condition
    /// (ISO §14.9.51 GR26) — drives the AT END-OF-PAGE / NOT AT END-OF-PAGE phrase branch.</summary>
    public static bool WasEndOfPage(string cobolName)
    {
        EnsureManager();
        return _manager!.GetHandler(cobolName) is SequentialFileHandler { EndOfPage: true };
    }

    /// <summary>Read the LINAGE-COUNTER of a file (ISO §8.4.3.14): the current line within the page body.
    /// Returns the runtime counter as a decimal (the form numeric expressions expect). 0 if the file is
    /// unknown/not a sequential LINAGE file.</summary>
    public static decimal GetLinageCounter(string cobolName)
    {
        EnsureManager();
        return _manager!.GetHandler(cobolName) is SequentialFileHandler seq ? seq.LinageCounter : 0m;
    }

    /// <summary>
    /// Register an alternate key for an indexed file (after RegisterFileHandlerWithOrg).
    /// </summary>
    public static void RegisterAlternateKey(string cobolName, int keyOffset, int keyLength, bool allowDuplicates)
    {
        EnsureManager();
        var handler = _manager!.GetHandler(cobolName) as IndexedFileHandler;
        handler?.AddAlternateKey(keyOffset, keyLength, allowDuplicates);
    }

    /// <summary>
    /// OPEN OUTPUT file-name.
    /// </summary>
    public static void OpenOutput(string fileName)
    {
        if (CheckLocked(fileName)) return;
        EnsureManager();
        _openModeScope[fileName] = UseScopeOutput;
        string status = _manager!.Open(fileName, FileOpenMode.Output);
        _lastStatus[fileName] = status;
    }

    /// <summary>
    /// OPEN INPUT file-name.
    /// </summary>
    public static void OpenInput(string fileName)
    {
        if (CheckLocked(fileName)) return;
        EnsureManager();
        _openModeScope[fileName] = UseScopeInput;
        string status = _manager!.Open(fileName, FileOpenMode.Input);
        _lastStatus[fileName] = status;
    }

    /// <summary>
    /// OPEN I-O file-name.
    /// </summary>
    public static void OpenIO(string fileName)
    {
        if (CheckLocked(fileName)) return;
        EnsureManager();
        _openModeScope[fileName] = UseScopeIO;
        string status = _manager!.Open(fileName, FileOpenMode.InputOutput);
        _lastStatus[fileName] = status;
    }

    /// <summary>
    /// OPEN EXTEND file-name.
    /// </summary>
    public static void OpenExtend(string fileName)
    {
        if (CheckLocked(fileName)) return;
        EnsureManager();
        _openModeScope[fileName] = UseScopeExtend;
        string status = _manager!.Open(fileName, FileOpenMode.Extend);
        _lastStatus[fileName] = status;
    }

    /// <summary>
    /// Check if a file was closed WITH LOCK. If so, set status "38" (file previously closed with lock)
    /// and return true to prevent the open.
    /// </summary>
    private static bool CheckLocked(string fileName)
    {
        if (_lockedFiles.Contains(fileName))
        {
            // COBOL-85 doesn't define a specific status for this — use "38" (file locked)
            // or "41" (already open) doesn't fit. Use "38" as a non-standard but reasonable code.
            _lastStatus[fileName] = "38";
            return true;
        }
        return false;
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
    /// CLOSE file-name WITH LOCK — prevents reopening.
    /// </summary>
    public static void CloseFileWithLock(string fileName)
    {
        CloseFile(fileName);
        _lockedFiles.Add(fileName);
    }

    /// <summary>
    /// CLOSE file-name REEL / UNIT. On a non-reel (disk) medium this is a no-op that advances past the
    /// current volume: the file connector REMAINS OPEN and the I-O status is 07 (ISO §9.1.13.2 item 6),
    /// so subsequent WRITE/READ continue against the same file. On a not-open file it is 42, as for any
    /// CLOSE (ISO §9.1.13.7).
    /// </summary>
    public static void CloseReelUnit(string fileName)
    {
        EnsureManager();
        var handler = _manager!.GetHandler(fileName);
        _lastStatus[fileName] = handler is { IsOpen: true }
            ? FileStatus.CloseNonReelMedium
            : FileStatus.FileNotOpen;
    }

    /// <summary>
    /// WRITE record-name: plain WRITE (data path).
    /// Delegates to handler.Write which does line-sequential formatting (TrimEnd + WriteLine).
    /// </summary>
    public static void WriteRecord(string fileName, byte[] recordBytes, int offset, int length)
    {
        EnsureManager();
        RuntimeGuard.Buffer(recordBytes, offset, length, "WRITE", fileName);
        byte[] recordSlice = new byte[length];
        Array.Copy(recordBytes, offset, recordSlice, 0, length);
        string status = _manager!.Write(fileName, recordSlice);
        _lastStatus[fileName] = status;
        // A plain WRITE (no ADVANCING phrase) to a logical-page (LINAGE) file advances the
        // LINAGE-COUNTER by one (ISO §13.18.34 GR7c3).
        if (status == FileStatus.Success
            && _manager.GetHandler(fileName) is SequentialFileHandler { LinageBody: > 0 } seq)
            seq.AdvanceLinageCounter(1);
    }

    /// <summary>
    /// WRITE BEFORE/AFTER ADVANCING: print-control semantics.
    /// advanceLines = -1 means PAGE advancing (form-feed).
    /// AFTER: advance lines, then write record. BEFORE: write record, then advance lines.
    /// </summary>
    public static void WriteAdvancing(string fileName, byte[] area, int offset, int size,
        int advanceLines, bool isBefore)
    {
        string text = Encoding.ASCII.GetString(area, offset, size).TrimEnd();
        WriteAdvancingText(fileName, text, advanceLines, isBefore);
    }

    /// <summary>WRITE AFTER ADVANCING with pre-extracted text (legacy compatibility).</summary>
    public static void WriteAfterAdvancingText(string fileName, string text, int advanceLines)
        => WriteAdvancingText(fileName, text, advanceLines, isBefore: false);

    private static void WriteAdvancingText(string fileName, string text, int advanceLines, bool isBefore)
    {
        EnsureManager();
        var handler = _manager!.GetHandler(fileName) as SequentialFileHandler;
        if (handler != null && handler.IsOpen)
        {
            _afterAdvancingFiles.Add(fileName);
            if (isBefore) { handler.WriteRawText(text); EmitAdvance(handler, advanceLines); }
            else          { EmitAdvance(handler, advanceLines); handler.WriteRawText(text); }
            // Maintain LINAGE-COUNTER for a logical-page (LINAGE) file (ISO §13.18.34 GR7): the counter
            // advances by the ADVANCING value (PAGE = reset), tracking the current page-body line.
            if (handler.LinageBody > 0)
                handler.AdvanceLinageCounter(advanceLines);
            _lastStatus[fileName] = FileStatus.Success;
        }
        else
        {
            if (isBefore) { Console.Write(text); EmitAdvanceConsole(advanceLines); }
            else          { EmitAdvanceConsole(advanceLines); Console.Write(text); }
        }
    }

    private static void EmitAdvance(SequentialFileHandler handler, int advanceLines)
    {
        if (advanceLines == -1) handler.WriteRawText("\f");
        else for (int i = 0; i < advanceLines; i++) handler.WriteRawText("\r\n");
    }

    private static void EmitAdvanceConsole(int advanceLines)
    {
        if (advanceLines == -1) Console.Write('\f');
        else for (int i = 0; i < advanceLines; i++) Console.WriteLine();
    }

    /// <summary>
    /// READ PREVIOUS: read previous record from file into byte buffer.
    /// Returns true if a record was read, false if at beginning-of-file.
    /// </summary>
    public static bool ReadPreviousRecord(string fileName, byte[] buffer, int offset, int length)
    {
        EnsureManager();
        RuntimeGuard.Buffer(buffer, offset, length, "READ PREVIOUS", fileName);
        byte[] tempBuf = new byte[length];
        string status = _manager!.ReadPrevious(fileName, tempBuf);
        _lastStatus[fileName] = status;

        if (status == FileStatus.AtEnd)
            return false;

        Array.Copy(tempBuf, 0, buffer, offset, length);
        return status is FileStatus.Success or FileStatus.DuplicateAlternateKey;
    }

    /// <summary>
    /// READ by key: read a specific record from an indexed/relative file using the key value.
    /// Extracts key bytes and calls IFileHandler.ReadByKey.
    /// </summary>
    public static void ReadByKey(string fileName, byte[] recArea, int recOffset, int recSize,
        byte[] keyArea, int keyOffset, int keySize, int keyIndex)
    {
        EnsureManager();
        RuntimeGuard.Buffer(recArea, recOffset, recSize, "READ KEY", fileName);
        RuntimeGuard.Buffer(keyArea, keyOffset, keySize, "READ KEY", fileName);
        byte[] keyValue = new byte[keySize];
        Array.Copy(keyArea, keyOffset, keyValue, 0, keySize);
        byte[] tempBuf = new byte[recSize];
        string status = _manager!.ReadByKey(fileName, tempBuf, keyValue, keyIndex);
        _lastStatus[fileName] = status;
        // The record is made available on a successful read, including the duplicate-alternate-key case (02).
        if (status is FileStatus.Success or FileStatus.DuplicateAlternateKey)
            Array.Copy(tempBuf, 0, recArea, recOffset, recSize);
    }

    /// <summary>
    /// READ: read next record from file into byte buffer.
    /// Returns true if a record was read, false if at end-of-file.
    /// </summary>
    public static bool ReadRecord(string fileName, byte[] buffer, int offset, int length)
    {
        EnsureManager();
        RuntimeGuard.Buffer(buffer, offset, length, "READ", fileName);
        byte[] tempBuf = new byte[length];
        string status = _manager!.ReadNext(fileName, tempBuf);
        _lastStatus[fileName] = status;
        // No record is made available at end-of-file (10) or relative-key overflow (14, ISO §14.9.30).
        bool noRecord = status is FileStatus.AtEnd or FileStatus.RelativeKeyOverflow;
        // Record the actual record length for RECORD VARYING DEPENDING ON (0 when no record).
        _lastRecordLength[fileName] = noRecord
            ? 0 : (_manager.GetHandler(fileName)?.LastRecordLength ?? length);

        if (noRecord)
            return false;

        Array.Copy(tempBuf, 0, buffer, offset, length);
        return status is FileStatus.Success or FileStatus.DuplicateAlternateKey;
    }

    /// <summary>
    /// Variable-length WRITE (RECORD IS VARYING … DEPENDING ON): write exactly <paramref name="length"/>
    /// bytes of the record area without trailing-space trimming, so the on-disk length round-trips.
    /// </summary>
    public static void WriteRecordVariable(string fileName, byte[] recordBytes, int offset, int length)
    {
        EnsureManager();
        if (length < 0) length = 0;
        var handler = _manager!.GetHandler(fileName);
        // ISO §9.1.13, I-O status 44 (boundary violation, 4a): a variable-length WRITE whose record length
        // is larger than the largest — or smaller than the smallest — record permitted by the RECORD IS
        // VARYING clause is unsuccessful; the record is NOT transferred and the I-O status is set to "44".
        if (VaryingBoundsViolated(handler, length))
        {
            _lastStatus[fileName] = FileStatus.RecordBoundaryViolation;
            return;
        }
        RuntimeGuard.Buffer(recordBytes, offset, length, "WRITE", fileName);
        byte[] recordSlice = new byte[length];
        Array.Copy(recordBytes, offset, recordSlice, 0, length);
        _lastStatus[fileName] = handler?.WriteVariable(recordSlice) ?? FileStatus.FileNotOpen;
    }

    /// <summary>
    /// ISO §9.1.13, I-O status 44 (boundary violation, 4a): true when a variable-length record of
    /// <paramref name="length"/> bytes is larger than the largest or smaller than the smallest record
    /// permitted by the file's RECORD IS VARYING clause, so the WRITE/REWRITE must be rejected with no
    /// record transfer. Max/min of 0 mean "no bound on that side" (a fixed-length file — whose writes
    /// never reach this path — or a variable file with no explicit FROM minimum).
    /// </summary>
    private static bool VaryingBoundsViolated(IO.IFileHandler? handler, int length)
    {
        if (handler is null) return false;
        int max = handler.MaxVaryingRecordSize;
        int min = handler.MinVaryingRecordSize;
        return (max > 0 && length > max) || (min > 0 && length < min);
    }

    /// <summary>
    /// Length of the most recently read record, for the RECORD VARYING DEPENDING ON data item.
    /// </summary>
    public static int GetLastRecordLength(string cobolName)
        => _lastRecordLength.TryGetValue(cobolName, out var len) ? len : 0;

    /// <summary>
    /// The AT END condition (ISO §14.9.21): true only at end-of-file (status "10"). A non-EOF
    /// unsuccessful read (file not open, not found, permanent error) is NOT an AT END condition —
    /// it sets FILE STATUS and is handled by a USE procedure — so it must not drive the AT END
    /// imperative. Compiler-generated read loops that must terminate on any exhaustion use
    /// <see cref="IsReadExhausted"/> instead.
    /// </summary>
    public static bool IsAtEnd(string fileName)
    {
        // The at-end condition includes relative-key overflow ("14") as well as end-of-file ("10")
        // (ISO §14.7.4 / §9.1.13.5 — a sequential relative READ whose record number exceeds the
        // relative key data item's digit size raises the at-end condition).
        return _lastStatus.TryGetValue(fileName, out var status)
            && status is FileStatus.AtEnd or FileStatus.RelativeKeyOverflow;
    }

    /// <summary>
    /// True when no further record can be obtained: end-of-file OR a terminal status (file never
    /// opened — e.g. OPEN INPUT on a missing file — not found, or a permanent I/O error). Used by
    /// compiler-generated read loops (e.g. the SORT … USING input pass) so they terminate instead
    /// of spinning on a file that never delivers a record. This is loop termination, NOT the AT END
    /// condition (see <see cref="IsAtEnd"/>).
    /// </summary>
    public static bool IsReadExhausted(string fileName)
    {
        return _lastStatus.TryGetValue(fileName, out var status) && status is
            FileStatus.AtEnd or FileStatus.RelativeKeyOverflow or FileStatus.FileNotOpen
            or FileStatus.FileNotFound or FileStatus.PermanentError;
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
        // ISO §9.1.13, I-O status 44 (boundary violation, 4a): a REWRITE of a record larger than the
        // largest — or smaller than the smallest — record permitted by the RECORD IS VARYING clause is
        // unsuccessful; the record is left unchanged and the I-O status is set to "44". (For a record-
        // sequential file an in-bounds length that merely differs from the read record's length is also 44,
        // enforced separately by the handler's §14.9.35 GR16 check.)
        if (VaryingBoundsViolated(_manager!.GetHandler(fileName), length))
        {
            _lastStatus[fileName] = FileStatus.RecordBoundaryViolation;
            return;
        }
        RuntimeGuard.Buffer(recordBytes, offset, length, "REWRITE", fileName);
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
    /// DELETE FILE (COBOL-2023, ISO §14.9.10): delete the physical file associated with a file connector,
    /// referenced by its ASSIGN target. Sets the connector's I-O status: "00" when the file was deleted, "35"
    /// when it was not available (did not exist), "30" on any other host error. The file should not be open.
    /// </summary>
    public static void DeleteFile(string fileName, string assignTarget)
    {
        string status;
        try
        {
            string path = ResolveHostPath(assignTarget);
            if (System.IO.File.Exists(path)) { System.IO.File.Delete(path); status = "00"; }
            else status = "35"; // file not available (ISO §14.9.10 — referenced file is not available)
        }
        catch
        {
            status = "30"; // permanent error (e.g. the file is open / locked by the host)
        }
        _lastStatus[fileName] = status;
    }

    /// <summary>
    /// START: position an indexed file for subsequent READ NEXT.
    /// </summary>
    public static void StartFile(string fileName, byte[] keyArea, int keyOffset, int keyLength,
        int condition, int keyIndex)
    {
        EnsureManager();
        RuntimeGuard.Buffer(keyArea, keyOffset, keyLength, "START", fileName);
        byte[] keyValue = new byte[keyLength];
        Array.Copy(keyArea, keyOffset, keyValue, 0, keyLength);
        string status = _manager!.Start(fileName, keyValue, (IO.StartCondition)condition, keyIndex);
        _lastStatus[fileName] = status;
    }

    /// <summary>
    /// Check if the last file operation was NOT successful (status != "00").
    /// Returns true if an error occurred (invalid key, etc.).
    /// </summary>
    public static bool IsInvalidKey(string fileName)
    {
        if (_lastStatus.TryGetValue(fileName, out var status))
            return status != IO.FileStatus.Success && status != IO.FileStatus.DuplicateAlternateKey;
        return false;
    }

    // USE-declarative scope encoding (must match FileIoLowerer): -1 = file-name-scoped (applies to
    // the file regardless of mode); 0/1/2/3 = open-mode-scoped (INPUT/OUTPUT/I-O/EXTEND).
    private const int UseScopeFileName = -1;
    private const int UseScopeInput = 0;
    private const int UseScopeOutput = 1;
    private const int UseScopeIO = 2;
    private const int UseScopeExtend = 3;

    /// <summary>
    /// Decide whether a USE AFTER STANDARD ERROR/EXCEPTION declarative with the given scope should run
    /// after the last I/O on <paramref name="fileName"/> (ISO §14.9.49 / §9.1.13). It runs only when:
    /// (1) the last I-O status indicates an unsuccessful (exception) condition — i.e. it is NOT one of
    /// the successful codes 00/02/04/05/07 — and (2) the declarative's scope applies: a file-name-scoped
    /// declarative (scope -1) always applies to its file; an open-mode-scoped declarative applies only
    /// when the file was opened in that mode. The compiler emits this at I/O sites that have no explicit
    /// AT END / INVALID KEY phrase (which would otherwise handle the condition themselves).
    /// </summary>
    public static bool ShouldRunUseDeclarative(string fileName, int scope)
        => ShouldRunUseDeclarative(fileName, scope, excludeAtEnd: false, excludeInvalidKey: false);

    public static bool ShouldRunUseDeclarative(string fileName, int scope,
        bool excludeAtEnd, bool excludeInvalidKey)
    {
        // ISO §14.9.49.4 GR2: do not re-invoke a USE procedure already active for this file — a declarative
        // whose body does I/O on its own file (e.g. CLOSE) would otherwise recurse into itself (RL111A).
        if (_activeUseDeclaratives.Contains(fileName)) return false;
        if (!_lastStatus.TryGetValue(fileName, out var status)) return false;
        // Successful completion (status class 0: 00 successful, 02 dup-key-ok, 04 length, 05 optional-
        // absent-on-open, 07 no-reel) is not an exception — no declarative.
        if (status is FileStatus.Success or FileStatus.DuplicateAlternateKey
            or "04" or "05" or "07") return false;
        // A condition serviced by a handling phrase on the originating statement is not serviced by the
        // declarative (ISO §14.6.6): an AT END phrase handles the at-end condition (10); an INVALID KEY
        // phrase handles the invalid-key conditions (21/22/23/24). The declarative still fires for any
        // other exception (e.g. 30/35/47/48/49) that the phrase does not handle.
        if (excludeAtEnd && status == FileStatus.AtEnd) return false;
        if (excludeInvalidKey && status is "21" or "22" or "23" or "24") return false;
        if (scope == UseScopeFileName) return true;
        return _openModeScope.TryGetValue(fileName, out var m) && m == scope;
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
        _lastRecordLength.Clear();
        _afterAdvancingFiles.Clear();
        _lockedFiles.Clear();
        _openModeScope.Clear();
        _activeUseDeclaratives.Clear();
    }

    private static void EnsureManager()
    {
        _manager ??= new CobolFileManager();
    }
}
