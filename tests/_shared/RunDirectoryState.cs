// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Tests.Shared;

/// <summary>
/// The state of the directory a program under test runs in, taken BEFORE an attempt so that a RETRY of the same
/// measurement starts from the same state the first attempt did (kb/Work PB3388).
/// </summary>
/// <remarks>
/// <para>
/// ⛔ WHY THIS TYPE EXISTS. <see cref="ProcessObserver.ObserveOrThrow"/> re-attempts a child that did not complete
/// (a contention timeout), on the stated ground that "nothing about the compiler was learned on the first attempt,
/// so there is no result being discarded". That ground is false for any program that WRITES: the killed first
/// attempt had already created, appended to or rewritten files in its run directory, and the retry ran in that
/// directory. The NIST program IX216A opens an absent OPTIONAL indexed file with OPEN EXTEND and expects I-O status
/// 05 (ISO/IEC 1989:2023 §14.9.27.4 GR17, "the file is created" because it is not present); the first attempt, killed
/// by the 120 s budget, had created the file, so the retry found it PRESENT and answered 00. One test of 17,812 went
/// red on the Linux gate with <c>OPEN EXTEND: EXP: 05 FAIL*</c>, a measurement taken from the wrong starting state and
/// reported as a value (the A12 failure class this observer exists to prevent, plan §11 A12).
/// </para>
/// <para>
/// THE RULE: a retry is a measurement of the SAME input, and the input of a program that reads and writes files
/// includes the directory it runs in. <see cref="Capture"/> records it, <see cref="Restore"/> puts it back.
/// </para>
/// <para>
/// ⚖ WHAT IS KEPT, AND WHY IT IS BOUNDED. Files up to <see cref="MaxCopiedFileBytes"/> are kept by content, because
/// a program rewrites a data file in place at the same length within one timestamp tick, which no metadata can see.
/// A larger file (the compiled program's runtime assembly is 3.7 MB, copied beside every program and never written
/// by one) is kept by length and last-write time only: if a failed attempt changed it, the state cannot be restored
/// and <see cref="Restore"/> SAYS SO (the observer then raises the non-observation) rather than run the retry on a
/// state that is not the first attempt's. An unrestorable retry is a non-observation, never a fabricated value.
/// Copying every run directory whole would cost the 17,000-run suite gigabytes of memory traffic for a case that
/// occurs on a loaded host only.
/// </para>
/// </remarks>
internal sealed class RunDirectoryState
{
    /// <summary>The largest file kept by content. A data file a COBOL test program writes is far smaller; the
    /// binaries the compiler copies beside it are larger and are the instrument, not the state under test.</summary>
    internal const int MaxCopiedFileBytes = 1 << 20;

    private readonly string _root;

    /// <summary>Every directory under the root, relative, as it was.</summary>
    private readonly HashSet<string> _directories;

    /// <summary>Every file under the root as it was: relative path → what is needed to recognize it again.</summary>
    private readonly Dictionary<string, FileState> _files;

    private readonly record struct FileState(long Length, DateTime LastWriteUtc, byte[]? Content);

    private RunDirectoryState(string root, HashSet<string> directories, Dictionary<string, FileState> files)
    {
        _root = root;
        _directories = directories;
        _files = files;
    }

    private static readonly EnumerationOptions Everything = new()
    {
        RecurseSubdirectories = true,
        AttributesToSkip = 0,                       // hidden and system files are state too
        ReturnSpecialDirectories = false,
    };

    /// <summary>Record <paramref name="directory"/> as it is now.</summary>
    internal static RunDirectoryState Capture(string directory)
    {
        string root = Path.GetFullPath(directory);
        var dirs = new HashSet<string>(StringComparer.Ordinal);
        var files = new Dictionary<string, FileState>(StringComparer.Ordinal);
        // A run directory that does not exist yet has nothing to record (the child cannot start in it either).
        if (!Directory.Exists(root)) return new RunDirectoryState(root, dirs, files);
        foreach (string path in Directory.EnumerateFileSystemEntries(root, "*", Everything))
        {
            string relative = Path.GetRelativePath(root, path);
            if (Directory.Exists(path)) { dirs.Add(relative); continue; }
            var info = new FileInfo(path);
            files[relative] = new FileState(info.Length, info.LastWriteTimeUtc, ContentOf(info));
        }
        return new RunDirectoryState(root, dirs, files);
    }

    /// <summary>The content to keep of <paramref name="file"/>, or null when it is over the bound or another process has
    /// it open (a scanner on the host): such a file is then kept by length and write time, which only costs the retry
    /// its restoration if an attempt changed it. Capture runs before EVERY observation, so it must never fail one.</summary>
    private static byte[]? ContentOf(FileInfo file)
    {
        if (file.Length > MaxCopiedFileBytes) return null;
        try { return File.ReadAllBytes(file.FullName); }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
    }

    /// <summary>Put the directory back as <see cref="Capture"/> recorded it: delete what an attempt created, rewrite
    /// what it changed or removed. Returns the relative paths it could NOT restore (a large file an attempt changed
    /// or removed); an empty list is a complete restoration.</summary>
    internal IReadOnlyList<string> Restore()
    {
        var unrestorable = new List<string>();
        if (!Directory.Exists(_root)) return unrestorable;

        // What the attempt created: files first, then directories deepest-first so a parent is empty when its turn comes.
        var currentFiles = new List<string>();
        var currentDirectories = new List<string>();
        foreach (string path in Directory.EnumerateFileSystemEntries(_root, "*", Everything))
            (Directory.Exists(path) ? currentDirectories : currentFiles).Add(Path.GetRelativePath(_root, path));
        foreach (string relative in currentFiles.Where(f => !_files.ContainsKey(f)))
            DeleteFile(Path.Combine(_root, relative));
        foreach (string relative in currentDirectories.Where(d => !_directories.Contains(d)).OrderByDescending(d => d.Length))
            if (Directory.Exists(Path.Combine(_root, relative)))
                Directory.Delete(Path.Combine(_root, relative), recursive: true);

        // What the attempt changed or removed.
        foreach (var (relative, was) in _files)
        {
            string path = Path.Combine(_root, relative);
            if (was.Content is null)
            {
                var now = new FileInfo(path);
                if (!now.Exists || now.Length != was.Length || now.LastWriteTimeUtc != was.LastWriteUtc)
                    unrestorable.Add(relative);
                continue;
            }
            if (File.Exists(path) && File.ReadAllBytes(path).AsSpan().SequenceEqual(was.Content)) continue;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            DeleteFile(path);
            File.WriteAllBytes(path, was.Content);
            File.SetLastWriteTimeUtc(path, was.LastWriteUtc);
        }
        return unrestorable;
    }

    /// <summary>Delete a file the attempt left, including one it made read-only.</summary>
    private static void DeleteFile(string path)
    {
        if (!File.Exists(path)) return;
        File.SetAttributes(path, FileAttributes.Normal);
        File.Delete(path);
    }
}
