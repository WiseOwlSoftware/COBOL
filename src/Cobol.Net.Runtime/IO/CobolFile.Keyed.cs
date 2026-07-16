// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// The KEYED (relative/indexed) half of the <see cref="CobolFile"/> facade (one facade, partial across the
/// organization slices — the singular-pattern rule; the sequential half owns the registry plumbing and routes
/// OPEN/CLOSE/Status here when the name is not a sequential connector). The generated code calls these entry
/// points in the SSOT status-first shape: every verb RETURNS the two-character I-O status (§9.1.13) and the
/// emitter branches on its first character.
/// </summary>
public static partial class CobolFile
{
    private static readonly Dictionary<string, RelativeConnector> RelativeFiles = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, IndexedConnector> IndexedFiles = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Register a SELECTed RELATIVE file (emitted at program start). <paramref name="relativeKeyDigits"/>
    /// is the RELATIVE KEY item's digit capacity (statuses '14'/'24', §9.1.13.4 / §14.9.51 GR29a; 0 = no clause).
    /// <paramref name="varyMin"/>/<paramref name="varyMax"/> are the RECORD IS VARYING bounds (ISO §13.18.43
    /// GR9/GR10; -1,-1 = fixed-length).</summary>
    public static void RegisterRelative(string cobolName, string assignTarget, int recordWidth, bool optional,
        int accessMode, int relativeKeyDigits, int varyMin = -1, int varyMax = -1)
    {
        if (cobolName.StartsWith("::EXT::", StringComparison.Ordinal) && RelativeFiles.ContainsKey(cobolName))
            return;   // the run-unit EXTERNAL connector already exists (§13.18.22.4 GR4a — mirror the sequential guard)
        RelativeFiles[cobolName] = new RelativeConnector(ResolveHostPath(assignTarget), recordWidth,
            (KeyedAccess)accessMode, relativeKeyDigits, varyMin, varyMax) { IsOptional = optional };
    }

    /// <summary>Register a SELECTed INDEXED file (emitted at program start) with its PRIME key's (offset, length)
    /// range of the record image (§12.4.5.12) and the RECORD IS VARYING bounds (-1,-1 = fixed-length).</summary>
    public static void RegisterIndexed(string cobolName, string assignTarget, int recordWidth, bool optional,
        int accessMode, int primeOffset, int primeLength, int varyMin = -1, int varyMax = -1)
    {
        if (cobolName.StartsWith("::EXT::", StringComparison.Ordinal) && IndexedFiles.ContainsKey(cobolName))
            return;   // the run-unit EXTERNAL connector already exists (§13.18.22.4 GR4a — mirror the sequential guard)
        IndexedFiles[cobolName] = new IndexedConnector(ResolveHostPath(assignTarget), recordWidth,
            (KeyedAccess)accessMode, primeOffset, primeLength, varyMin, varyMax) { IsOptional = optional };
    }

    /// <summary>Register one ALTERNATE RECORD KEY (§12.4.5.6), in declaration order.</summary>
    public static void AddAlternateKey(string name, int offset, int length, bool duplicates)
    {
        if (IndexedFiles.TryGetValue(name, out var f)) f.AddAlternateKey(offset, length, duplicates);
    }

    /// <summary>Stage the RELATIVE KEY item's value for the next keyed verb (the compiler reads the TYPED field).</summary>
    public static void SetRelativeKey(string name, long rrn)
    {
        if (RelativeFiles.TryGetValue(name, out var f)) f.SetPendingKey(rrn);
    }

    /// <summary>The RRN last made available/released — the §14.9.30 GR25 / §14.9.51 GR29a MOVE-back source.</summary>
    public static long RelativeSlot(string name) =>
        RelativeFiles.TryGetValue(name, out var f) ? f.LastSlot : 0;

    /// <summary>Keyed WRITE (§14.9.51) — returns the I-O status. <paramref name="length"/> is the varying-record
    /// length (ISO §13.18.43 GR13a), -1 = the record's own size.</summary>
    public static string WriteKeyed(string name, string image, int length = -1) =>
        RelativeFiles.TryGetValue(name, out var r) ? r.Write(image, length)
        : IndexedFiles.TryGetValue(name, out var ix) ? ix.Write(image, length)
        : FileStatusCode.PermanentError;

    /// <summary>Keyed REWRITE (§14.9.35) — returns the I-O status. <paramref name="length"/> is the varying-record
    /// length (§13.18.43 GR13a; the keyed record size MAY differ from the replaced record's, §14.9.35 GR18).</summary>
    public static string RewriteKeyed(string name, string image, int length = -1) =>
        RelativeFiles.TryGetValue(name, out var r) ? r.Rewrite(image, length)
        : IndexedFiles.TryGetValue(name, out var ix) ? ix.Rewrite(image, length)
        : FileStatusCode.PermanentError;

    /// <summary>The length of the most recently read keyed record (ISO §13.18.43 GR15).</summary>
    private static int KeyedLastReadLength(string name) =>
        RelativeFiles.TryGetValue(name, out var r) ? r.LastReadLength
        : IndexedFiles.TryGetValue(name, out var ix) ? ix.LastReadLength
        : 0;

    /// <summary>DELETE RECORD (§14.9.10 F1); for indexed random/dynamic the prime key is sliced from
    /// <paramref name="keyedRecordImage"/> (GR3) — relative uses the staged relative key (GR4).</summary>
    public static string DeleteRecord(string name, string keyedRecordImage) =>
        RelativeFiles.TryGetValue(name, out var r) ? r.Delete()
        : IndexedFiles.TryGetValue(name, out var ix) ? ix.Delete(keyedRecordImage)
        : FileStatusCode.PermanentError;

    /// <summary>Sequential keyed READ [NEXT] (§14.9.30 F1) — returns the I-O status and the record image.</summary>
    public static string ReadKeyedNext(string name, out string image)
    {
        if (RelativeFiles.TryGetValue(name, out var r)) return r.ReadNext(out image);
        if (IndexedFiles.TryGetValue(name, out var ix)) return ix.ReadNext(out image);
        image = "";
        return FileStatusCode.PermanentError;
    }

    /// <summary>Sequential keyed READ PREVIOUS (§14.9.30 F1, COBOL-2002+; compiler edition-gated).</summary>
    public static string ReadKeyedPrevious(string name, out string image)
    {
        if (RelativeFiles.TryGetValue(name, out var r)) return r.ReadPrevious(out image);
        if (IndexedFiles.TryGetValue(name, out var ix)) return ix.ReadPrevious(out image);
        image = "";
        return FileStatusCode.PermanentError;
    }

    /// <summary>Random keyed READ (§14.9.30 F2): <paramref name="keyIndex"/> = −1 prime / i-th alternate (indexed,
    /// GR30–GR32; the key value is sliced from <paramref name="keyedRecordImage"/>); relative uses the staged
    /// relative key (GR29) and ignores both parameters.</summary>
    public static string ReadKeyed(string name, int keyIndex, string keyedRecordImage, out string image)
    {
        if (RelativeFiles.TryGetValue(name, out var r)) return r.ReadRandom(out image);
        if (IndexedFiles.TryGetValue(name, out var ix)) return ix.ReadRandom(keyIndex, keyedRecordImage, out image);
        image = "";
        return FileStatusCode.PermanentError;
    }

    /// <summary>START on a relative file (§14.9.41 GR8–GR12) — a numeric RRN comparison.</summary>
    public static string StartRelative(string name, string op, long rrn) =>
        RelativeFiles.TryGetValue(name, out var f) ? f.Start(op, rrn) : FileStatusCode.PermanentError;

    /// <summary>START on an indexed file (§14.9.41 GR13–GR17) — a leftmost-length partial-key comparison.</summary>
    public static string StartIndexed(string name, int keyIndex, string op, string operand, int compareLength) =>
        IndexedFiles.TryGetValue(name, out var f)
            ? f.Start(keyIndex, op, operand, compareLength) : FileStatusCode.PermanentError;

    /// <summary>START FIRST/LAST (COBOL-2002+; §14.9.41 GR11/GR12), either organization.</summary>
    public static string StartFirstLast(string name, bool last) =>
        RelativeFiles.TryGetValue(name, out var r) ? r.StartFirstLast(last)
        : IndexedFiles.TryGetValue(name, out var ix) ? ix.StartFirstLast(last)
        : FileStatusCode.PermanentError;

    /// <summary>DELETE FILE (§14.9.10 Format 2, COBOL-2023): an OPEN connector → '41' (GR13); an ABSENT physical
    /// file is a SUCCESSFUL completion, status '05' (GR14 — the legacy's '35' was a deviation; the spec wins);
    /// insufficient authority → '37' (GR16). Fixed-file-attribute matching (GR18, '39') awaits that model.</summary>
    public static string DeleteFile(string name)
    {
        string host;
        bool open;
        Action<string> setStatus;
        // The SEQUENTIAL-organization connector (§14.9.10 Format 2 applies to every organization — the keyed
        // half owns the runtime dispatch, so it checks the sequential Files registry first): same GR13/GR14/GR16
        // outcomes on the host path.
        if (Files.TryGetValue(name, out var sq)) { host = sq.HostPath; open = sq.IsOpen; setStatus = sq.SetStatus; }
        else if (RelativeFiles.TryGetValue(name, out var r)) { host = r.HostPath; open = r.IsOpen; setStatus = r.SetStatus; }
        else if (IndexedFiles.TryGetValue(name, out var ix)) { host = ix.HostPath; open = ix.IsOpen; setStatus = ix.SetStatus; }
        else return FileStatusCode.PermanentError;
        string status;
        if (open) status = FileStatusCode.FileAlreadyOpen;                 // '41' GR13
        else
            try
            {
                if (!File.Exists(host)) status = FileStatusCode.OptionalFileNotFound;   // '05' GR14 — successful
                else { File.Delete(host); status = FileStatusCode.Success; }
            }
            catch (UnauthorizedAccessException) { status = FileStatusCode.PermissionDenied; }   // '37' GR16
            catch (IOException) { status = FileStatusCode.PermanentError; }
        setStatus(status);
        return status;
    }

    // ── Routing hooks the sequential half calls (registry init/open/close/status/close-all) ─────────────────

    private static void KeyedInit()
    {
        RelativeFiles.Clear();
        IndexedFiles.Clear();
    }

    private static void KeyedOpen(string name, FileOpenMode mode)
    {
        if (RelativeFiles.TryGetValue(name, out var r))
        {
            if (Locked.Contains(name)) r.SetStatus(FileStatusCode.FileLocked); else r.Open(mode);
        }
        else if (IndexedFiles.TryGetValue(name, out var ix))
        {
            if (Locked.Contains(name)) ix.SetStatus(FileStatusCode.FileLocked); else ix.Open(mode);
        }
    }

    private static void KeyedClose(string name)
    {
        if (RelativeFiles.TryGetValue(name, out var r)) r.Close();
        else if (IndexedFiles.TryGetValue(name, out var ix)) ix.Close();
    }

    /// <summary>Remove a keyed (relative/indexed) connector from its registry — the keyed half of
    /// <see cref="CloseAndDrop"/> (M2-OO-1i §9.1.4: a per-object connector is dropped, not just closed, when the
    /// owning object is deleted). No-op for a name that is not a keyed connector.</summary>
    private static void KeyedDrop(string name)
    {
        RelativeFiles.Remove(name);
        IndexedFiles.Remove(name);
    }

    private static int KeyedOpenModeOf(string name) =>
        RelativeFiles.TryGetValue(name, out var r) ? r.OpenModeView
        : IndexedFiles.TryGetValue(name, out var ix) ? ix.OpenModeView
        : -1;

    private static string KeyedStatus(string name) =>
        RelativeFiles.TryGetValue(name, out var r) ? r.Status
        : IndexedFiles.TryGetValue(name, out var ix) ? ix.Status
        : FileStatusCode.Success;

    /// <summary>Close (and so PERSIST) every open keyed connector at run-unit termination (ISO §14.6 — the
    /// implicit CLOSE; keyed chains depend on the store flushing at STOP RUN, e.g. NIST RL208A).</summary>
    private static void KeyedCloseAll()
    {
        foreach (var r in RelativeFiles.Values) if (r.IsOpen) r.Close();
        foreach (var ix in IndexedFiles.Values) if (ix.IsOpen) ix.Close();
    }
}
