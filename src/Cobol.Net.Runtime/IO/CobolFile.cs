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
        KeyedInit();
    }

    /// <summary>Register a SELECTed sequential file (emitted at program start, one per SELECT). The host path is
    /// resolved from the ASSIGN target by <see cref="ResolveHostPath"/> so the same name round-trips OUTPUT→INPUT.</summary>
    public static void Register(string cobolName, string assignTarget, int recordWidth, bool lineSequential, bool optional)
    {
        Files[cobolName] = new SequentialFile(ResolveHostPath(assignTarget), recordWidth, lineSequential) { IsOptional = optional };
    }

    public static void OpenInput(string name) => Open(name, FileOpenMode.Input);
    public static void OpenOutput(string name) => Open(name, FileOpenMode.Output);
    public static void OpenExtend(string name) => Open(name, FileOpenMode.Extend);
    public static void OpenIO(string name) => Open(name, FileOpenMode.IO);

    private static void Open(string name, FileOpenMode mode)
    {
        if (Files.TryGetValue(name, out var f))
        {
            if (Locked.Contains(name)) { f.SetStatus(FileStatusCode.FileLocked); return; }
            f.Open(mode);
        }
        else KeyedOpen(name, mode);   // relative/indexed connectors (ISO §14.9.27 GR14/GR15/GR17)
    }

    /// <summary>CLOSE the file (emitted for each closed file-name).</summary>
    public static void Close(string name) { if (Files.TryGetValue(name, out var f)) f.Close(); else KeyedClose(name); }

    /// <summary>CLOSE … WITH LOCK — close, then prevent reopen (a subsequent OPEN is status 38).</summary>
    public static void CloseWithLock(string name) { Close(name); Locked.Add(name); }

    /// <summary>CLOSE … REEL/UNIT on a disk medium: a no-op that leaves the file open with status 07 (the file is not
    /// reel-structured). On a not-open file it is 42. Modeled minimally for the sequential slice.</summary>
    public static void CloseReelUnit(string name)
    {
        if (Files.TryGetValue(name, out var f)) f.SetStatus(f.IsOpen ? "07" : FileStatusCode.FileNotOpen);
    }

    /// <summary>Plain <c>WRITE record</c> — the record's character image.</summary>
    public static void Write(string name, string image) { if (Files.TryGetValue(name, out var f)) f.Write(image); }

    /// <summary><c>WRITE record {BEFORE|AFTER} ADVANCING {n LINES | PAGE}</c>; <paramref name="lines"/> = -1 is PAGE.</summary>
    public static void WriteAdvancing(string name, string image, int lines, bool before)
    { if (Files.TryGetValue(name, out var f)) f.WriteAdvancing(image, lines, before); }

    /// <summary>Sequential <c>READ … NEXT</c> — returns the record image and whether a record was obtained.</summary>
    public static bool Read(string name, out string image)
    {
        if (Files.TryGetValue(name, out var f)) return f.Read(out image);
        image = "";
        return false;
    }

    /// <summary>Sequential <c>REWRITE record</c> — replace the last-read record's image.</summary>
    public static void Rewrite(string name, string image) { if (Files.TryGetValue(name, out var f)) f.Rewrite(image); }

    /// <summary>The file's current FILE STATUS two-character code (ISO §9.1.13). "00" for an unknown name.</summary>
    public static string Status(string name) => Files.TryGetValue(name, out var f) ? f.Status : KeyedStatus(name);

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
