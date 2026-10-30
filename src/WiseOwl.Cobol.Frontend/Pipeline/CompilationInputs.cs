// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Frontend.Diagnostics;

namespace WiseOwl.Cobol.Frontend;

/// <summary>
/// ⛔ THE ONE GATEWAY for every AMBIENT input a compilation reads — the files it opens, the paths it probes, the
/// environment variables it consults and the wall clock (kb/Work PB985; <c>DESIGN-test-build-ci.md</c> §3.12). Each
/// read goes through a method here, which performs it AND records it, so after a compile the record names every
/// fact outside the source text and the options that the compiler's output depended on — the dependency list a
/// build tool needs (the <c>gcc -MD</c> depfile shape) and the key a compiled-program cache needs.
/// <para>A read that bypasses this class is an input no cache and no incremental build can see.
/// <c>CompilationInputsDriftTests</c> (Conformance) makes that a compile-time-of-the-tests failure: an ambient-read
/// API anywhere in <c>WiseOwl.Cobol.Compiler</c> / <c>WiseOwl.Cobol.Frontend</c> / <c>WiseOwl.Cobol.Editions</c> outside this
/// file is red unless its line says why it is not a compilation input (<c>// not a compilation input: …</c>).</para>
/// <para>One instance per compilation; not thread-safe (a compilation is single-threaded).</para>
/// </summary>
public sealed class CompilationInputs
{
    private readonly List<string> _filesRead = [];
    private readonly List<InputProbe> _probes = [];
    private readonly List<EnvironmentRead> _environment = [];

    /// <summary>Every file whose CONTENT the compilation read, as a full path, in read order (the source first,
    /// then each copybook as COPY incorporates it).</summary>
    public IReadOnlyList<string> FilesRead => _filesRead;

    /// <summary>Every existence probe and its answer — including the ones that found NOTHING. A copybook search
    /// that missed <c>dir1/X.cpy</c> and found <c>dir2/X.cpy</c> resolves differently the moment the first file
    /// appears, so an absent answer is as much an input as a present one.</summary>
    public IReadOnlyList<InputProbe> Probes => _probes;

    /// <summary>Every environment variable consulted and the value it had, <c>null</c> when it was unset — ISO
    /// §7.3.11.4 GR4: a <c>&gt;&gt;DEFINE … PARAMETER</c> value "is obtained from the operating environment".</summary>
    public IReadOnlyList<EnvironmentRead> EnvironmentReads => _environment;

    /// <summary>True once the compilation read the wall clock — the WHEN-COMPILED stamp (§15.99.3 r2) is baked
    /// into the program, so its output is a function of WHEN it was compiled, not only of its inputs.</summary>
    public bool ReadsCompilationTime { get; private set; }

    /// <summary>Read the text of a source or library text file, recording its full path. The bytes become characters in
    /// <see cref="SourceTextDecoder"/> and nowhere else (docs/CONFORMANCE.md §7, Annex A.1 items 25 and 26; kb/Work
    /// PB1397): a file read as ISO/IEC 8859-1 draws the warning COBOL5435 and a marked file that is not well-formed in its
    /// encoding draws the error COBOL5434, both into <paramref name="diagnostics"/> naming the file and the byte offset,
    /// at the start of that file. Returns null for the malformed file (its error is reported) and the text otherwise.</summary>
    public string? ReadSourceText(string path, DiagnosticBag? diagnostics)
    {
        var decoded = SourceTextDecoder.Decode(File.ReadAllBytes(path));
        _filesRead.Add(Path.GetFullPath(path));
        // The location names the file; the finding is once per file per bag, so a library text copied many times is one
        // finding and not one per COPY.
        var at = new SourceLocation(path, 0, 0, 0);
        switch (decoded.Reading)
        {
            case SourceTextReading.Malformed:
                ReportOnce(diagnostics, DiagnosticCatalog.SourceTextMalformedForItsMark.Code, DiagnosticSeverity.Error,
                    $"the file begins with a {decoded.EncodingName} byte-order mark but is not well-formed {decoded.EncodingName}: the "
                    + $"first byte it does not define is at offset {decoded.FaultOffset} — ISO §8.1.3.2 GR1 (Annex A.1 item 25): a file "
                    + "with a byte-order mark is read in exactly the encoding the mark names, and this implementation does not substitute a "
                    + "character for a byte sequence that encoding does not define",
                    at);
                break;
            case SourceTextReading.Latin1:
                ReportOnce(diagnostics, DiagnosticCatalog.SourceTextReadAsLatin1.Code, DiagnosticSeverity.Warning,
                    "the file has no byte-order mark and is not well-formed UTF-8 (the first byte that is not UTF-8 is at offset "
                    + $"{decoded.FaultOffset}), so it is read as ISO/IEC 8859-1: each byte is the character with that code point — ISO "
                    + "§8.1.3.2 GR1 (Annex A.1 item 25)",
                    at);
                break;
        }

        return decoded.Text;
    }

    private static void ReportOnce(DiagnosticBag? diagnostics, string code, DiagnosticSeverity severity, string message,
        SourceLocation at)
    {
        if (diagnostics is not null && !diagnostics.Diagnostics.Any(d => d.Code == code && d.Location.FileName == at.FileName))
            diagnostics.Report(code, severity, message, at, default);
    }

    /// <summary>Probe whether a FILE exists, recording the answer.</summary>
    public bool FileExists(string path)
    {
        bool exists = File.Exists(path);
        _probes.Add(new InputProbe(Path.GetFullPath(path), IsDirectory: false, exists));
        return exists;
    }

    /// <summary>Probe whether a FILE exists and, when it does, return its path with the FINAL component spelled as the
    /// directory entry spells it (kb/Work PB1680): on a case-insensitive file system <c>gr9dbook.CPY</c> finds
    /// <c>gr9dbook.cpy</c>, and a diagnostic, a line map or a dependency record must name the file that was read, not
    /// the spelling that was probed. Where several entries differ only in case (a case-sensitive file system) the one
    /// that matches exactly is the file. Null when no such file exists; the probe is recorded as
    /// <see cref="FileExists"/> records it.</summary>
    public string? LocateFile(string path)
    {
        if (!FileExists(path)) return null;
        string name = Path.GetFileName(path);
        // A wildcard in the name would be a pattern, not a name; no file system that matches case-insensitively allows one.
        if (name.AsSpan().IndexOfAny('*', '?') >= 0) return path;
        string? directory = Path.GetDirectoryName(path);
        string? spelledAs = null;
        try
        {
            var options = new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive, MatchType = MatchType.Simple };
            foreach (var entry in Directory.EnumerateFiles(string.IsNullOrEmpty(directory) ? "." : directory, name, options))
            {
                string entryName = Path.GetFileName(entry);
                if (string.Equals(entryName, name, StringComparison.Ordinal)) return path;   // the exact spelling is an entry
                spelledAs ??= entryName;
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return path;   // the directory cannot be listed: the probe spelling stands (File.Exists already said it is there)
        }

        return spelledAs is null ? path : Path.Combine(directory ?? "", spelledAs);
    }

    /// <summary>Probe whether a DIRECTORY exists, recording the answer.</summary>
    public bool DirectoryExists(string path)
    {
        bool exists = Directory.Exists(path);
        _probes.Add(new InputProbe(Path.GetFullPath(path), IsDirectory: true, exists));
        return exists;
    }

    /// <summary>The compiler process's working directory, read once per compilation (null until read) — the first
    /// place of the default COBOL library (DOC-A.1-40, kb/Work PB1355), so the output depends on it. Each probe made
    /// there is recorded by <see cref="FileExists"/> / <see cref="DirectoryExists"/> as a full path.</summary>
    public string? WorkingDirectory { get; private set; }

    /// <summary>Read the working directory (<see cref="WorkingDirectory"/>), recording it.</summary>
    public string GetWorkingDirectory() => WorkingDirectory ??= Directory.GetCurrentDirectory();

    /// <summary>Read an environment variable, recording its name and value (null = unset).</summary>
    public string? GetEnvironmentVariable(string name)
    {
        string? value = Environment.GetEnvironmentVariable(name);
        _environment.Add(new EnvironmentRead(name, value));
        return value;
    }

    /// <summary>Read the compilation time through <paramref name="clock"/> (the injectable compile-clock seam),
    /// recording that the output now depends on it.</summary>
    public DateTimeOffset ReadCompilationTime(Func<DateTimeOffset> clock)
    {
        ReadsCompilationTime = true;
        return clock();
    }
}

/// <summary>One existence probe of a compilation (<see cref="CompilationInputs.Probes"/>).</summary>
/// <param name="Path">The full path probed.</param>
/// <param name="IsDirectory">A directory probe (a COPY library, the NIST copy library) rather than a file probe.</param>
/// <param name="Exists">The answer the compilation acted on.</param>
public readonly record struct InputProbe(string Path, bool IsDirectory, bool Exists);

/// <summary>One environment-variable read of a compilation (<see cref="CompilationInputs.EnvironmentReads"/>).</summary>
/// <param name="Name">The variable's name.</param>
/// <param name="Value">Its value at the read; <c>null</c> when unset.</param>
public readonly record struct EnvironmentRead(string Name, string? Value);
