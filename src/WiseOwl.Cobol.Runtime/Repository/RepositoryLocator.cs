// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Concurrent;
using System.ComponentModel;

namespace WiseOwl.Cobol.Runtime.Repository;

/// <summary>One unit a <see cref="RepositoryLocator"/> found in a repository directory (DESIGN-external-repository §7.4):
/// the assembly that publishes it, the facts of that assembly's <see cref="CobolRepositoryAttribute"/> a caller needs before it
/// loads anything, and the unit's own row of the <c>Units</c> index.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class RepositoryEntry
{
    internal RepositoryEntry(string path, Guid mvid, int schemaVersion, string runtimeVersion, int callAbi, string? registrar,
        RepositoryUnitRow unit)
    {
        Path = path;
        Mvid = mvid;
        SchemaVersion = schemaVersion;
        RuntimeVersion = runtimeVersion;
        CallAbi = callAbi;
        Registrar = registrar;
        Unit = unit;
    }

    /// <summary>The full path of the assembly file.</summary>
    public string Path { get; }

    /// <summary>The assembly's module version id: two entries with one are the same build.</summary>
    public Guid Mvid { get; }

    /// <summary>The repository schema the assembly's record follows.</summary>
    public int SchemaVersion { get; }

    /// <summary>The runtime version the assembly was compiled against (<see cref="RuntimeAbi.Skew"/> judges it).</summary>
    public string RuntimeVersion { get; }

    /// <summary>The call ABI the assembly was compiled for (<see cref="RuntimeAbi.Skew"/> judges it).</summary>
    public int CallAbi { get; }

    /// <summary>The full CLR name of the assembly's registrar type, or null when its record names none.</summary>
    public string? Registrar { get; }

    /// <summary>The unit's row in the assembly's <c>Units</c> index.</summary>
    public RepositoryUnitRow Unit { get; }
}

/// <summary>The answer of a <see cref="RepositoryLocator"/> lookup: the entry, or none with the reason a file beside the
/// lookup's name was no answer. A reason is a plain miss's absence: a directory with no file that could carry the name has
/// none.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class RepositoryFind
{
    internal RepositoryFind(RepositoryEntry? entry, string? collision, string? reason)
    {
        Entry = entry;
        Collision = collision;
        Reason = reason;
    }

    /// <summary>The unit's entry, or null when no assembly of the directory publishes it.</summary>
    public RepositoryEntry? Entry { get; }

    /// <summary>The path of a SECOND, different assembly (another module version id) that publishes the same unit, or null.
    /// Two such assemblies are a collision (§7.4): <see cref="Entry"/> is the first, in ordinal path order, and neither is an
    /// answer a caller may use.</summary>
    public string? Collision { get; }

    /// <summary>Why a file the lookup read (the one named after the unit, which the lookup reads first) was no answer: it is
    /// not a managed assembly, carries no repository record or another schema's, is malformed or too large, or does not
    /// publish the unit. Null when the lookup met nothing worth saying. It is the caller's reason for a miss and means nothing
    /// when <see cref="Entry"/> is set.</summary>
    public string? Reason { get; }

    internal static RepositoryFind Miss(string? reason) => new(null, null, reason);
}

/// <summary>
/// ⛔ THE ONE LOCATOR OF A COMPILED UNIT IN A REPOSITORY DIRECTORY (DESIGN-external-repository §7.4; kb/Work PB2101), over
/// <see cref="RepositoryIndex"/>: the run-time probe's way to a sibling module (<c>AppContext.BaseDirectory</c>, before it loads
/// anything) and, from slice 7, the compiler's to a <c>--repository</c> directory.
/// <list type="number">
///   <item><b>The named file.</b> <c>&lt;externalized name&gt;.dll</c> in the directory is read first; when its
///     <c>Units</c> index lists the unit it is the answer and nothing else is read, so <c>PAY.dll</c> beside
///     <c>PAY - Copy.dll</c> keeps working and a program whose name is its file's name costs one file read.</item>
///   <item><b>The directory index</b>, only on a miss: every <c>*.dll</c> of the directory (files only, no recursion, no
///     reparse point, sorted ordinally), each read once per process and remembered by its path, length and last write time as
///     the rows of its index, so a program whose assembly is not named after it is found too. A file that is not a managed
///     assembly, carries no repository record or another schema's, or is over <see cref="MaxFileBytes"/> is no entry and is
///     skipped.</item>
/// </list>
/// Two different assemblies (different module version ids) publishing one unit are a collision; identical copies are one
/// entry. Reading throws nothing: an unreadable file is a reason or a skip (§15.2, §15.3).
/// <para>It is public for the same reason <see cref="RepositoryIndex"/> is (the compiler cannot see the runtime's internals) and
/// is the compiler's and the probe's ABI, not an API for a hand-written caller.</para>
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class RepositoryLocator
{
    /// <summary>The largest file the locator reads (§15.2): 256 MiB. A larger one is skipped.</summary>
    public const long MaxFileBytes = 256L * 1024 * 1024;

    /// <summary>What reading one file of a directory established, by the file's path, length and last write time (a different
    /// length or time is another build of the file): the file is read once per process, not once per lookup (§7.4, §16).</summary>
    private sealed record FileFacts(string Path, long Length, DateTime LastWriteUtc, string? Problem, Guid Mvid,
        int SchemaVersion, string RuntimeVersion, int CallAbi, string? Registrar, IReadOnlyList<RepositoryUnitRow> Units)
    {
        /// <summary>Does the file publish units this runtime reads (a managed assembly of the current schema)?</summary>
        public bool IsEntry => Problem is null && SchemaVersion == CobolRepositoryAttribute.CurrentSchemaVersion;
    }

    private static readonly ConcurrentDictionary<string, FileFacts> Cache = new(StringComparer.Ordinal);

    /// <summary>Locate the unit of <paramref name="kind"/> whose externalized name is <paramref name="externalizedName"/>
    /// (compared by the Annex C fold, <see cref="ExternalizedNames.Same"/>) in <paramref name="directory"/> (§7.4).</summary>
    /// <param name="directory">The repository directory.</param>
    /// <param name="kind">What the unit is.</param>
    /// <param name="externalizedName">The unit's externalized name.</param>
    public static RepositoryFind Find(string directory, RepositoryUnitKind kind, string externalizedName)
    {
        string name = ExternalizedNames.Form(externalizedName);
        if (name.Length == 0) return RepositoryFind.Miss(null);
        string? reason = null;
        if (FactsOf(NamedFile(directory, name)) is { } named)
        {
            if (named.Problem is { } problem) reason = problem;
            else if (!named.IsEntry)
                reason = $"'{named.Path}': schema v{named.SchemaVersion}, this runtime reads v{CobolRepositoryAttribute.CurrentSchemaVersion}; recompile it";
            else if (RowOf(named, kind, name) is { } row)
                return new RepositoryFind(EntryOf(named, row), null, null);
            else
                reason = $"'{named.Path}' does not carry the {KindWord(kind)} {name}";
        }
        // The named file did not answer: the directory index, the one read of every file of the directory.
        return Answer(directory, f => RowOf(f, kind, name), reason);
    }

    /// <summary>Locate the user-defined function whose user-defined WORD is <paramref name="word"/> (§8.3, the D-R3 word
    /// step: a function is reached by the word its FUNCTION-ID declares as well as by its externalized name) in the directory
    /// index. The word rows of the <c>Units</c> index answer it; there is no named file to try, because a file is named for an
    /// externalized name.</summary>
    /// <param name="directory">The repository directory.</param>
    /// <param name="word">The function's word as declared (compared by the Annex C fold).</param>
    public static RepositoryFind FunctionByWord(string directory, string word)
    {
        string wanted = ExternalizedNames.Form(word);
        if (wanted.Length == 0) return RepositoryFind.Miss(null);
        return Answer(directory, f => WordRowOf(f, wanted), null);
    }

    /// <summary>The <c>*.dll</c> files of <paramref name="directory"/> the directory index skips for exceeding
    /// <see cref="MaxFileBytes"/> (§15.2: the compiler's REPO-1 lists them), in ordinal path order.</summary>
    /// <param name="directory">The repository directory.</param>
    public static IReadOnlyList<string> SkippedForSize(string directory) =>
        Listing(directory).Where(path => FactsOf(path) is { Length: > MaxFileBytes }).ToList();

    private static RepositoryFind Answer(string directory, Func<FileFacts, RepositoryUnitRow?> carries, string? reason)
    {
        RepositoryEntry? first = null;
        string? collision = null;
        foreach (string path in Listing(directory))
        {
            if (FactsOf(path) is not { IsEntry: true } facts || carries(facts) is not { } row) continue;
            var entry = EntryOf(facts, row);
            if (first is null) first = entry;
            else if (first.Mvid != entry.Mvid) { collision = entry.Path; break; }   // the same build twice is one entry
        }
        return first is null ? RepositoryFind.Miss(reason) : new RepositoryFind(first, collision, null);
    }

    private static RepositoryUnitRow? RowOf(FileFacts file, RepositoryUnitKind kind, string name)
    {
        foreach (RepositoryUnitRow unit in file.Units)
            if (unit.Kind == kind && ExternalizedNames.Same(unit.ExternalizedName, name)) return unit;
        return null;
    }

    private static RepositoryUnitRow? WordRowOf(FileFacts file, string word)
    {
        foreach (RepositoryUnitRow unit in file.Units)
            if (unit.Kind == RepositoryUnitKind.Function && ExternalizedNames.Same(unit.Word, word)) return unit;
        return null;
    }

    private static RepositoryEntry EntryOf(FileFacts file, RepositoryUnitRow row) =>
        new(file.Path, file.Mvid, file.SchemaVersion, file.RuntimeVersion, file.CallAbi, file.Registrar, row);

    private static string KindWord(RepositoryUnitKind kind) => kind switch
    {
        RepositoryUnitKind.Function => "function",
        RepositoryUnitKind.Program => "program",
        _ => kind.ToString(),
    };

    /// <summary>The path of the file <paramref name="name"/> names, or null when the name cannot be a file's (an AS literal
    /// holding a separator or a character no file name has: the directory index finds such a unit by its row).</summary>
    private static string? NamedFile(string directory, string name) =>
        name.AsSpan().IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0 ? null
        : System.IO.Path.Combine(directory, name + ".dll");

    /// <summary>The <c>*.dll</c> FILES directly in <paramref name="directory"/> (no recursion, no reparse point), sorted
    /// ordinally on the full path; none for a directory that is missing or unreadable.</summary>
    private static List<string> Listing(string directory)
    {
        var paths = new List<string>();
        try
        {
            foreach (var info in new DirectoryInfo(directory).EnumerateFiles("*.dll"))
                // "*.dll" also matches a longer extension on some file systems (8.3 short names): the extension is exact.
                if ((info.Attributes & FileAttributes.ReparsePoint) == 0
                    && string.Equals(info.Extension, ".dll", StringComparison.OrdinalIgnoreCase))   // not a COBOL word: a file extension
                    paths.Add(info.FullName);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException) { }
        paths.Sort(StringComparer.Ordinal);
        return paths;
    }

    /// <summary>What <paramref name="path"/> is, or null when there is no such file or it is not a plain file (a reparse point
    /// is no repository entry, §15.2).</summary>
    private static FileFacts? FactsOf(string? path)
    {
        if (path is null) return null;
        try
        {
            var info = new FileInfo(path);
            long length = info.Length;                                  // throws for a missing file
            if ((info.Attributes & FileAttributes.ReparsePoint) != 0) return null;
            DateTime written = info.LastWriteTimeUtc;
            if (Cache.TryGetValue(info.FullName, out var known) && known.Length == length && known.LastWriteUtc == written)
                return known;
            FileFacts facts = Read(info.FullName, length, written);
            Cache[info.FullName] = facts;
            return facts;
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException) { return null; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return new FileFacts(path, 0, default, $"'{path}': {e.Message}", default, 0, "", 0, null, []);
        }
    }

    private static FileFacts Read(string path, long length, DateTime written)
    {
        FileFacts Unreadable(string problem) => new(path, length, written, problem, default, 0, "", 0, null, []);
        if (length > MaxFileBytes)
            return Unreadable($"'{path}' is larger than {MaxFileBytes} bytes and was skipped");
        RepositoryIndexOutcome outcome;
        try { outcome = RepositoryIndex.Open(path); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return Unreadable($"'{path}': {e.Message}");
        }
        if (outcome.Index is not { } index)
            return Unreadable(outcome.Failure switch
            {
                RepositoryReadFailure.NotManagedAssembly => $"'{path}' is not a managed assembly",
                RepositoryReadFailure.NoRepositoryRecord => $"'{path}' is not a WiseOwl COBOL module: it carries no CobolRepository record",
                RepositoryReadFailure.TooManyUnits => $"'{path}': the repository index is too large: {outcome.Detail}",
                _ => $"'{path}': the repository record is malformed: {outcome.Detail}",
            });
        using (index)
            return new FileFacts(path, length, written, null, index.Mvid, index.SchemaVersion, index.RuntimeVersion, index.CallAbi,
                index.Registrar, index.Units);
    }
}
