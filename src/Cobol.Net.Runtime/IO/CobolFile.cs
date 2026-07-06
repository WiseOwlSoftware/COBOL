// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// The static file-I/O facade the COBOL.NET backend emits calls to (the typed-native analogue of the legacy
/// <c>FileRuntime</c>): one registry of named connectors keyed by COBOL file-name, plus the OPEN/CLOSE/WRITE/READ/
/// REWRITE verbs and the FILE STATUS / AT END accessors. The compiler registers every SELECTed file at program
/// start, then emits a verb call per file statement; the connector owns the ISO §9.1.13 status machine. Records cross
/// this boundary as their <b>character image</b> (a <see cref="string"/>) — the typed record ↔ image conversion is in
/// the generated code (a record struct's <c>AsImage</c>/<c>FromImage</c>), keeping the substrate typed.
/// </summary>
public static partial class CobolFile
{
    private static readonly Dictionary<string, SequentialFile> Files = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> Locked = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Reset the file registry (emitted once at program start).</summary>
    public static void Init()
    {
        foreach (var f in Files.Values) f.Close();
        Files.Clear();
        Locked.Clear();
        LocksInit();
        KeyedInit();
    }

    /// <summary>Register a SELECTed sequential file (emitted at program start, one per SELECT). The host path is
    /// resolved from the ASSIGN target by <see cref="ResolveHostPath"/> so the same name round-trips OUTPUT→INPUT.
    /// <paramref name="varyMin"/>/<paramref name="varyMax"/> are the RECORD IS VARYING bounds (ISO §13.18.43
    /// GR9/GR10) — the connector then length-frames its records; (-1,-1) = fixed-length. Re-registering an
    /// INTERNAL connector replaces it (a fresh program instance gets fresh connectors, ISO §14.6.2.3.2); an
    /// EXTERNAL file connector (the compiler's <c>"::EXT::"</c> key band) is ONE per run unit shared by every
    /// describing program (ISO §13.18.22.4 GR4a), so a later describer's activation keeps the existing live
    /// connector — its open mode and position persist across activations (IC227A: the main OPENs, the
    /// subprogram's first activation must not clobber that).</summary>
    public static void Register(string cobolName, string assignTarget, int recordWidth, bool lineSequential,
        bool optional, int varyMin = -1, int varyMax = -1)
    {
        if (cobolName.StartsWith("::EXT::", StringComparison.Ordinal) && Files.ContainsKey(cobolName))
            return;   // the run-unit EXTERNAL connector already exists (§13.18.22.4 GR4a)
        Files[cobolName] = new SequentialFile(ResolveHostPath(assignTarget), recordWidth, lineSequential, varyMin, varyMax)
            { IsOptional = optional };
    }

    public static void OpenInput(string name) => Open(name, FileOpenMode.Input);
    public static void OpenOutput(string name) => Open(name, FileOpenMode.Output);
    public static void OpenExtend(string name) => Open(name, FileOpenMode.Extend);
    public static void OpenIO(string name) => Open(name, FileOpenMode.IO);

    private static void Open(string name, FileOpenMode mode)
    {
        // A sharing-active connector (SHARING / LOCK MODE declared) routes through the physical-file registry
        // (Table-19 → 61); every other file keeps the legacy exclusive path byte-for-byte (ISO §14.9.27 GR23
        // implementor default — outside the sharing subsystem). Handles all three organizations via
        // ResolveConnector, so KeyedOpen is only reached on the legacy path.
        if (IsSharingActive(name)) { SharedOpenAttempt(name, mode, null); return; }
        if (Files.TryGetValue(name, out var f))
        {
            if (Locked.Contains(name)) { f.SetStatus(FileStatusCode.FileLocked); return; }
            f.Open(mode);
        }
        else KeyedOpen(name, mode);   // relative/indexed connectors (ISO §14.9.27 GR14/GR15/GR17)
    }

    /// <summary>CLOSE the file (emitted for each closed file-name). For a sharing-active connector this also
    /// deregisters it from the physical-file registry and releases its record locks (§9.1.16 :11754).</summary>
    public static void Close(string name)
    {
        SharedClose(name);   // no-op for a non-sharing-active connector
        if (Files.TryGetValue(name, out var f)) f.Close(); else KeyedClose(name);
    }

    /// <summary>CLOSE … WITH LOCK — close, then prevent reopen (a subsequent OPEN is status 38).</summary>
    public static void CloseWithLock(string name) { Close(name); Locked.Add(name); }

    /// <summary>CLOSE … REEL/UNIT on a disk medium: a no-op that leaves the file open with status 07 (the file is not
    /// reel-structured). On a not-open file it is 42. Modeled minimally for the sequential slice.</summary>
    public static void CloseReelUnit(string name)
    {
        if (Files.TryGetValue(name, out var f)) f.SetStatus(f.IsOpen ? "07" : FileStatusCode.FileNotOpen);
    }

    /// <summary>Plain <c>WRITE record</c> — the record's character image; <paramref name="length"/> is the
    /// varying-record length (ISO §13.18.43 GR13a — the DEPENDING item's content), -1 = the record's own size.</summary>
    public static void Write(string name, string image, int length = -1)
    { if (Files.TryGetValue(name, out var f)) f.Write(image, length); }

    /// <summary><c>WRITE record {BEFORE|AFTER} ADVANCING {n LINES | PAGE}</c>; <paramref name="lines"/> = -1 is PAGE.</summary>
    public static void WriteAdvancing(string name, string image, int lines, bool before)
    { if (Files.TryGetValue(name, out var f)) f.WriteAdvancing(image, lines, before); }

    /// <summary>Install a LINAGE file's logical-page evaluator (ISO §13.18.34 GR6; emitted right after
    /// <see cref="Register"/> for an FD with a LINAGE clause). The closure defers the operand reads, so the
    /// literal (GR6a) and data-name (GR6b) forms share ONE mechanism; the connector invokes it at OPEN OUTPUT,
    /// WRITE ADVANCING PAGE, and page overflow (GR6b1–3).</summary>
    public static void SetLinage(string name, Func<(int Body, int Footing, int Top, int Bottom)> eval)
    { if (Files.TryGetValue(name, out var f)) f.SetLinage(eval); }

    /// <summary>The file's LINAGE-COUNTER register (ISO §8.4.3.14): the line at which the device is positioned
    /// within the current page body (§13.18.34 GR7). 0 for an unknown name / before the first OPEN OUTPUT.</summary>
    public static long LinageCounter(string name) => Files.TryGetValue(name, out var f) ? f.LinageCounter : 0;

    /// <summary>The end-of-page condition of the file's most recent WRITE (ISO §14.9.51 GR26a/b), driving the
    /// END-OF-PAGE / NOT END-OF-PAGE branch (GR27b/GR28).</summary>
    public static bool EndOfPage(string name) => Files.TryGetValue(name, out var f) && f.EndOfPage;

    /// <summary>Sequential <c>READ … NEXT</c> — returns the record image and whether a record was obtained.</summary>
    public static bool Read(string name, out string image)
    {
        if (Files.TryGetValue(name, out var f)) return f.Read(out image);
        image = "";
        return false;
    }

    /// <summary>Sequential <c>REWRITE record</c> — replace the last-read record's image; <paramref name="length"/>
    /// is the varying-record length (§13.18.43 GR13a), -1 = the record's own size.</summary>
    public static void Rewrite(string name, string image, int length = -1)
    { if (Files.TryGetValue(name, out var f)) f.Rewrite(image, length); }

    /// <summary>The length of the most recently read record (ISO §13.18.43 GR15 — the value the RECORD VARYING
    /// DEPENDING item receives after a successful READ).</summary>
    public static int LastReadLength(string name) =>
        Files.TryGetValue(name, out var f) ? f.LastReadLength : KeyedLastReadLength(name);

    /// <summary>The file's current FILE STATUS two-character code (ISO §9.1.13). "00" for an unknown name.</summary>
    public static string Status(string name) => Files.TryGetValue(name, out var f) ? f.Status : KeyedStatus(name);

    /// <summary>The open-mode view for USE-declarative mode scoping (ISO 14.9.49.4 GR6b-e): (int)FileOpenMode
    /// while open or in-the-process-of-being-opened; -1 otherwise (incl. an unknown name).</summary>
    public static int OpenModeOf(string name) =>
        Files.TryGetValue(name, out var f) ? f.OpenModeView : KeyedOpenModeOf(name);

    /// <summary>The AT END condition for a file (status 10), driving the AT END / NOT AT END branch.</summary>
    public static bool AtEnd(string name) => Files.TryGetValue(name, out var f) && f.AtEnd;

    /// <summary>True when the last operation was unsuccessful (status not 00 — for the implicit INVALID/error branch).</summary>
    public static bool Failed(string name) => Files.TryGetValue(name, out var f) && f.Status != FileStatusCode.Success;

    /// <summary>Close every open file (emitted at run-unit termination, ISO §14.6 — flushes print streams).</summary>
    public static void CloseAll() { foreach (var f in Files.Values) f.Close(); KeyedCloseAll(); }

    /// <summary>Resolve an ASSIGN target to a host file path: a target that already looks like a path (has a
    /// directory separator or an extension) is used verbatim; otherwise it becomes <c>&lt;lowercased&gt;.txt</c> in the
    /// current directory — the convention the legacy oracle uses, so the differential corpus finds the same file.</summary>
    public static string ResolveHostPath(string assignTarget)
    {
        if (assignTarget.Contains('.') || assignTarget.Contains('/') || assignTarget.Contains('\\')) return assignTarget;
        return assignTarget.ToLowerInvariant() + ".txt";
    }
}
