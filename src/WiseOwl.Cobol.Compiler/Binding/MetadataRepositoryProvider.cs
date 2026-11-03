// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Binding.Repository;
using WiseOwl.Cobol.Compiler.Oo;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Repository;

namespace WiseOwl.Cobol.Binding;

/// <summary>The external repository one compilation reads (ISO §8.13; DESIGN-external-repository §8.6, §9.3): the
/// <c>--repository</c> directories in the order given — the output directory when none is given — then the
/// <c>--reference</c> assemblies in order. <paramref name="OutputPath"/> is the assembly this compilation produces, which is
/// never consulted for resolution (its previous build only by <paramref name="Check"/>'s comparison, §9.2).
/// <paramref name="DirectoriesNamed"/> is true when the directories were written on the command line, so one that does not
/// exist is an error (REPO-10) rather than an empty repository. <paramref name="Inputs"/> is the compilation's ambient-input
/// record: every existence probe of a named place goes through it, and the provider marks it when it consults the
/// repository.</summary>
internal sealed record RepositorySearchSpace(
    IReadOnlyList<string> Directories, bool DirectoriesNamed, IReadOnlyList<string> References, string? OutputPath, bool Check,
    IRepositoryInputs Inputs)
{
    /// <summary>No repository: an in-process bind with no output (a unit test's), which a specifier the group cannot answer
    /// leaves with nothing to search.</summary>
    public static RepositorySearchSpace None { get; } =
        new([], DirectoriesNamed: false, [], OutputPath: null, Check: false, NoCompilation.Instance);

    /// <summary><see cref="None"/>'s record: a bind with no compilation has no input record to mark, and names no place, so
    /// nothing probes one.</summary>
    private sealed class NoCompilation : IRepositoryInputs
    {
        public static NoCompilation Instance { get; } = new();

        public bool FileExists(string path) => throw new InvalidOperationException("the empty search space names no file");

        public bool DirectoryExists(string path) => throw new InvalidOperationException("the empty search space names no directory");

        public void RecordRepositoryRead() { }
    }
}

/// <summary>An entry of the external repository: the assembly at <paramref name="Path"/> (module version id
/// <paramref name="Mvid"/>) that publishes the unit <paramref name="Kind"/> <paramref name="ExternalizedName"/>
/// (DESIGN-external-repository §8.0). Its program or function is compiled elsewhere, so a CALL through it describes its
/// operands for the run-time check (§4.2).</summary>
internal sealed record RepositoryEntryOrigin(string Path, Guid Mvid, RepositoryUnitKind Kind, string ExternalizedName) : RepositoryOrigin
{
    public override bool IsDefinitionCompiledHere => false;

    public override string Describe(string kind) =>
        $"the {kind}{(Kind is RepositoryUnitKind.ProgramPrototype or RepositoryUnitKind.FunctionPrototype ? " prototype" : "")} "
        + $"\"{ExternalizedName}\" of the repository entry '{Path}'";
}

/// <summary>
/// ⛔ THE EXTERNAL REPOSITORY'S PROVIDER (ISO §12.3.8.4 GR10 c) / GR11 c), §8.13; DESIGN-external-repository §8.0, §8.2 step
/// 3, §8.3, §8.6): what the compiled assemblies of the search space publish, read through the runtime's
/// <see cref="RepositoryLocator"/> (which file publishes a unit) and the compiler's <see cref="RepositoryReader"/> (its record),
/// and bound into the throw-away forest by <see cref="RepositoryCodec.Decode"/>, which admits the record's prototypes into the
/// group's <see cref="PrototypeSignatureClasses"/> (§6.4). Nothing is loaded: the reads are prefetched metadata (§15.1).
/// <list type="bullet">
/// <item><b>Search order (§8.6).</b> Every location in order; in a location the definition before the prototype of the name.
/// The FIRST entry wins. A later entry from another build (another module version id) whose signature differs is REPO-2;
/// the same build in two places is one entry.</item>
/// <item><b>Every reader outcome is reported (§14).</b> An unreadable file the search names for the unit is REPO-3, a malformed
/// or oversized record REPO-4, a description the codec cannot rebuild REPO-5, another schema, runtime major or call ABI REPO-7,
/// an entry built with <c>--repository-update off</c> REPO-8, an object reference to a class or interface identity no class of
/// the group has REPO-11, and a named location that does not exist REPO-10. A query that drew one of them answers nothing and
/// <see cref="Refused"/> says so, so the resolver does not add REPO-1. Each finding is reported once, at the specifier that
/// first asked (every answer is cached).</item>
/// <item><b>Zero cost when unused (§16).</b> Nothing is listed or opened until the first question; a group whose specifiers the
/// group answers asks none, except that a FUNCTION specifier the group answers still asks for a shadow (D-R3 step 3), which
/// opens only an entry of ANOTHER externalized name and reports nothing of it. The first question marks the compilation's
/// input record (<see cref="Frontend.CompilationInputs.ReadsRepository"/>).</item>
/// </list>
/// The repository has no source order, so <c>position</c> is ignored (§8.0).
/// </summary>
internal sealed class MetadataRepositoryProvider : IRepositoryProvider
{
    private readonly RepositorySearchSpace _space;
    private readonly PrototypeSignatureClasses _classes;
    private readonly OoClassTable? _ooClasses;
    private readonly EditionContext _edition;
    private readonly string? _output;
    private readonly List<Location> _locations = [];
    private bool _consulted;
    private readonly Dictionary<(string Path, Guid Mvid), Loaded?> _loaded = [];
    private readonly Dictionary<(bool Function, string Name), Hit?> _byName = [];
    private readonly Dictionary<(string Word, string? OtherThan), IReadOnlyList<Hit>> _byWord = [];
    private readonly Dictionary<(bool Function, string Name), IReadOnlyList<string>> _reasons = [];
    private readonly HashSet<(bool Function, string Name)> _refused = [];
    private readonly HashSet<string> _refusedWords = new(StringComparer.Ordinal);
    private readonly Dictionary<(string Path, RepositoryUnitKind Kind, string Name), Hit?> _answers = [];

    /// <param name="space">The search space.</param>
    /// <param name="classes">The group's signature classes, which every decoded record's prototypes join (§6.4).</param>
    /// <param name="ooClasses">The group's classes and interfaces, which answer a record's object-reference identities
    /// (REPO-11); null in a bind with no class table.</param>
    /// <param name="edition">Where the findings are reported.</param>
    public MetadataRepositoryProvider(RepositorySearchSpace space, PrototypeSignatureClasses classes, OoClassTable? ooClasses,
        EditionContext edition)
    {
        _space = space;
        _classes = classes;
        _ooClasses = ooClasses;
        _edition = edition;
        _output = space.OutputPath is { } output ? Path.GetFullPath(output) : null;
        // REPO-10 is a fault of the command line, stated before any source position: an existence probe per named place (no
        // listing, so the zero-cost rule holds), recorded as an input of the compilation. The output directory, which stands
        // in when no --repository is written, is not probed: a missing one is an empty repository (the locator lists nothing
        // there), nothing is read from it before a question, and a question marks the record (§16) — so whether it exists
        // decides nothing a compilation that asks no question produces, and the compilation itself creates it.
        foreach (var directory in space.Directories)
        {
            string full = Path.GetFullPath(directory);
            if (!space.DirectoriesNamed || space.Inputs.DirectoryExists(full)) _locations.Add(new Location(full, IsDirectory: true));
            else
                edition.Error(DiagnosticCatalog.RepositoryLocationUnavailable,
                    $"--repository '{directory}': no such directory, so the external repository cannot be read there (DESIGN-external-repository §9.3)");
        }
        foreach (var reference in space.References)
        {
            string full = Path.GetFullPath(reference);
            if (space.Inputs.FileExists(full)) _locations.Add(new Location(full, IsDirectory: false));
            else
                edition.Error(DiagnosticCatalog.RepositoryLocationUnavailable,
                    $"--reference '{reference}': no such file, so the external repository cannot be read there (DESIGN-external-repository §9.3)");
        }
    }

    /// <summary>One place of the search space: a directory (§7.4's named file, then its index) or one assembly file.</summary>
    private sealed record Location(string Path, bool IsDirectory)
    {
        public RepositoryFind Find(RepositoryUnitKind kind, string name, string? excluding) =>
            IsDirectory ? RepositoryLocator.Find(Path, kind, name, excluding) : RepositoryLocator.FindInFile(Path, kind, name);

        public RepositoryFind ByWord(string word, string? excluding) =>
            IsDirectory ? RepositoryLocator.FunctionByWord(Path, word, excluding) : RepositoryLocator.FunctionByWordInFile(Path, word);

        public string Describe => IsDirectory ? $"the directory '{Path}'" : $"the assembly '{Path}'";
    }

    /// <summary>An entry's records, by unit kind and folded externalized name.</summary>
    private sealed record Loaded(IReadOnlyDictionary<(RepositoryUnitKind Kind, string Name), RepositoryRecord> Records);

    /// <summary>A unit an entry publishes, decoded.</summary>
    private sealed record Hit(RepositoryRecord Record, DecodedRecord Decoded, RepositoryEntryOrigin Origin);

    /// <inheritdoc/>
    public ProgramAnswer? Program(string externalizedName, int position) =>
        ByName(function: false, externalizedName) is { } hit ? new ProgramAnswer(hit.Decoded.Signature, hit.Origin) : null;

    /// <inheritdoc/>
    public FunctionAnswer? Function(string externalizedName, int position) =>
        ByName(function: true, externalizedName) is { } hit ? FunctionAnswerOf(hit) : null;

    /// <inheritdoc/>
    public IEnumerable<FunctionAnswer> FunctionsByWord(string userFunctionName, int position, string? otherThan) =>
        ByWord(userFunctionName, otherThan).Select(FunctionAnswerOf);

    /// <summary>True when the questions for a specifier — <paramref name="externalizedName"/> by name (D-R3 step 1) and, for a
    /// function, <paramref name="word"/> by word (step 2) — drew a finding of their own (REPO-3, -4, -5, -7, -8, -10, -11), so
    /// the resolver reports no REPO-1 beside it.</summary>
    public bool Refused(bool function, string word, string externalizedName) =>
        _refused.Contains(Key(function, externalizedName)) || (function && _refusedWords.Contains(CobolNames.Fold(word)));

    /// <summary>What REPO-1 says was searched for <paramref name="externalizedName"/>: every location in order, why a file named
    /// after the unit was no answer, and the files the directory index skipped for their size (§15.2).</summary>
    public string Searched(bool function, string externalizedName)
    {
        if (_locations.Count == 0) return "no external repository is named (no --repository directory or --reference file, and no output directory)";
        var parts = new List<string> { "searched: " + string.Join(", ", _locations.Select(l => l.Describe)) };
        if (_reasons.TryGetValue(Key(function, externalizedName), out var reasons)) parts.AddRange(reasons);
        foreach (var location in _locations.Where(l => l.IsDirectory))
            if (RepositoryLocator.SkippedForSize(location.Path) is { Count: > 0 } skipped)
                parts.Add($"skipped for exceeding {RepositoryLocator.MaxFileBytes} bytes: {string.Join(", ", skipped)}");
        return string.Join("; ", parts);
    }

    /// <summary>Every entry the compilation opened that publishes a program or function — what §9.2's check compares the
    /// group's units against beside the previous build of the output (DESIGN-external-repository §9.2 (b): "the entries the
    /// resolver already opened"), with the record of each unit.</summary>
    public IEnumerable<(string Path, RepositoryRecord Record)> OpenedRecords() =>
        _loaded.Where(e => e.Value is not null).SelectMany(e => e.Value!.Records.Values.Select(r => (e.Key.Path, r)));

    private static (bool, string) Key(bool function, string name) => (function, CobolNames.Fold(name));

    private static FunctionAnswer FunctionAnswerOf(Hit hit) =>
        new(new UserFunctionSignature(hit.Record.Name, hit.Record.ExternalizedName, hit.Decoded.Returning, hit.Decoded.Formals,
            DefinitionCompiledHere: false), hit.Origin);

    private static RepositoryUnitKind[] KindsOf(bool function) => function
        ? [RepositoryUnitKind.Function, RepositoryUnitKind.FunctionPrototype]
        : [RepositoryUnitKind.Program, RepositoryUnitKind.ProgramPrototype];

    private void Consult()
    {
        if (_consulted) return;
        _consulted = true;
        _space.Inputs.RecordRepositoryRead();
    }

    /// <summary>GR10 c) / GR11 c): the first entry of the search space publishing the name (§8.6), decoded; REPO-2 for a later
    /// different entry whose signature differs.</summary>
    private Hit? ByName(bool function, string externalizedName)
    {
        var key = Key(function, externalizedName);
        if (_byName.TryGetValue(key, out var cached)) return cached;
        Consult();
        var entries = new List<RepositoryEntry>();
        var reasons = new List<string>();
        RepositoryFind? typedMiss = null;
        foreach (var location in _locations)
            foreach (var kind in KindsOf(function))
            {
                var find = location.Find(kind, externalizedName, _output);
                if (find.Entry is { } entry)
                {
                    Add(entries, entry);
                    if (find.Collision is { } collision && RepositoryLocator.FindInFile(collision, kind, externalizedName).Entry is { } other)
                        Add(entries, other);
                }
                else if (find.Reason is { } reason)
                {
                    reasons.Add(reason);
                    if (find.Failure is not (RepositoryReadFailure.None or RepositoryReadFailure.NoRepositoryRecord or RepositoryReadFailure.TooLarge))
                        typedMiss ??= find;
                }
            }
        Hit? hit = null;
        if (entries.Count > 0)
        {
            hit = Answer(entries[0], report: true);
            if (hit is null) _refused.Add(key);
            else
                foreach (var later in entries.Skip(1))
                    if (Answer(later, report: false) is { } shadowed
                        && PrototypeSignatures.FirstDifference(hit.Decoded.Signature, shadowed.Decoded.Signature) is { } difference)
                        _edition.Warning(DiagnosticCatalog.RepositoryEntryShadowedDiffers,
                            $"{hit.Origin.Describe(function ? "function" : "program")} is used; {shadowed.Origin.Describe(function ? "function" : "program")} "
                            + $"comes later in the search order and differs in {difference} (ISO §8.13; DESIGN-external-repository §8.6)");
        }
        else if (typedMiss is not null)
        {
            ReportFileFault(typedMiss.Failure, typedMiss.Reason!);
            _refused.Add(key);
        }
        _reasons[key] = reasons;
        _byName[key] = hit;
        return hit;

        static void Add(List<RepositoryEntry> entries, RepositoryEntry entry)
        {
            if (!entries.Exists(e => e.Mvid == entry.Mvid)) entries.Add(entry);   // one build in two places is one entry (§8.6)
        }
    }

    /// <summary>D-R3's repository arms (§8.3): every function — definition or prototype — whose recorded WORD is
    /// <paramref name="word"/>, in search order, decoded.
    /// <list type="bullet">
    /// <item><paramref name="otherThan"/> null — step 2. A word is how a later-compiled unit names a function (§8.4.6.7), so
    /// the entry is the specifier's answer and one that cannot be read here is reported as the by-name search reports it.</item>
    /// <item><paramref name="otherThan"/> set — step 3's shadow look, after GR11 selected the function externalized
    /// <paramref name="otherThan"/>. An entry externalized <paramref name="otherThan"/> is that function again (or a stale copy of
    /// it) and is skipped BEFORE it is opened; the rest are decoded quietly, because GR11 b) ignores what the repository holds
    /// for a function the group answered, and an unreadable entry that merely might shadow it is no fault of this compilation.</item>
    /// </list></summary>
    private IReadOnlyList<Hit> ByWord(string word, string? otherThan)
    {
        var key = (CobolNames.Fold(word), otherThan);
        if (_byWord.TryGetValue(key, out var cached)) return cached;
        if (_locations.Count == 0) return _byWord[key] = [];
        Consult();
        bool report = otherThan is null;
        var hits = new List<Hit>();
        var seen = new HashSet<Guid>();
        bool asked = false;   // the first entry in search order is the specifier's answer, as in ByName: only its faults are the specifier's
        foreach (var location in _locations)
        {
            var find = location.ByWord(word, _output);
            foreach (var entry in new[] { find.Entry, find.Collision is { } c ? RepositoryLocator.FunctionByWordInFile(c, word).Entry : null })
            {
                if (entry is null || (!report && ExternalizedNames.Same(entry.Unit.ExternalizedName, otherThan!)) || !seen.Add(entry.Mvid)) continue;
                bool answer = report && !asked;
                asked = true;
                if (Answer(entry, answer) is { } hit) hits.Add(hit);
                else if (answer) _refusedWords.Add(CobolNames.Fold(word));
            }
        }
        return _byWord[key] = hits;
    }

    /// <summary>The unit <paramref name="entry"/> publishes, decoded, or null (with the finding reported when
    /// <paramref name="report"/>) when the entry cannot answer.</summary>
    private Hit? Answer(RepositoryEntry entry, bool report)
    {
        var unit = entry.Unit;
        var key = (entry.Path, unit.Kind, CobolNames.Fold(unit.ExternalizedName));
        if (_answers.TryGetValue(key, out var known)) return known;
        Hit? hit = null;
        if (Load(entry, report) is { } loaded)
        {
            if (!loaded.Records.TryGetValue((unit.Kind, CobolNames.Fold(unit.ExternalizedName)), out var record))
            {
                if (report)
                    _edition.Error(DiagnosticCatalog.RepositoryEntryMalformed,
                        $"the repository entry '{entry.Path}' lists {unit.Kind} \"{unit.ExternalizedName}\" in its index and carries no record of it (ISO §8.13)");
            }
            else if (Words(entry, record, report) is { } words)
            {
                try
                {
                    hit = new Hit(record, RepositoryCodec.Decode(record, _classes, words),
                        new RepositoryEntryOrigin(entry.Path, entry.Mvid, unit.Kind, record.ExternalizedName));
                }
                catch (RepositoryRecordException e)
                {
                    if (report)
                        _edition.Error(DiagnosticCatalog.RepositoryDescriptionUnbuildable,
                            $"the repository entry '{entry.Path}': {e.Message} (ISO §8.13; DESIGN-external-repository §6.3)");
                }
            }
        }
        // A refused answer is remembered only when it was reported, so a quiet look (a later entry's REPO-2 compare) never
        // hides the finding from the question that names that entry first.
        if (hit is not null || report) _answers[key] = hit;
        return hit;
    }

    /// <summary>The entry's records, or null (the finding reported when <paramref name="report"/>): another runtime major or
    /// call ABI, another schema, no signatures, an unreadable or malformed record (§14, §15.4).</summary>
    private Loaded? Load(RepositoryEntry entry, bool report)
    {
        var key = (entry.Path, entry.Mvid);
        if (_loaded.TryGetValue(key, out var known)) return known;
        Loaded? loaded = null;
        if (RuntimeAbi.Skew(entry.RuntimeVersion, entry.CallAbi) is { } skew)
        {
            if (report)
                _edition.Error(DiagnosticCatalog.RepositoryEntryVersionSkew,
                    $"the repository entry '{entry.Path}' cannot be used: {skew}; recompile it (ISO §8.13; Annex A.1 item 67)");
        }
        else
        {
            RepositoryIndexOutcome opened;
            try { opened = RepositoryIndex.Open(entry.Path); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                opened = new RepositoryIndexOutcome(null, RepositoryReadFailure.Unreadable, $"'{entry.Path}': {e.Message}");
            }
            if (opened.Index is not { } index)
            {
                if (report) ReportFileFault(opened.Failure, opened.Detail ?? $"'{entry.Path}'");
            }
            else
                using (index)
                {
                    var outcome = RepositoryReader.Read(index);
                    if (outcome.Failure is RepositoryReadFailure.None)
                        loaded = new Loaded(outcome.Records.ToDictionary(r => (r.Kind, CobolNames.Fold(r.ExternalizedName))));
                    else if (report)
                        switch (outcome.Failure)
                        {
                            case RepositoryReadFailure.NoSignatures:
                                _edition.Error(DiagnosticCatalog.RepositoryEntryWithoutSignatures,
                                    $"the repository entry '{entry.Path}' was compiled with --repository-update off and carries no "
                                    + $"signature for {entry.Unit.Kind} \"{entry.Unit.ExternalizedName}\"; recompile it with "
                                    + "--repository-update on (ISO §8.13)");
                                break;
                            case RepositoryReadFailure.OtherSchemaVersion:
                                _edition.Error(DiagnosticCatalog.RepositoryEntryVersionSkew,
                                    $"the repository entry '{entry.Path}' cannot be read: {outcome.Detail}; recompile it (ISO §8.13; Annex A.1 item 67)");
                                break;
                            default:
                                ReportFileFault(outcome.Failure, $"'{entry.Path}': {outcome.Detail}");
                                break;
                        }
                }
        }
        if (loaded is not null || report) _loaded[key] = loaded;
        return loaded;
    }

    /// <summary>A file the search read that cannot answer, by its typed outcome: REPO-3 for a file that is no readable managed
    /// assembly, REPO-7 for another schema, REPO-4 otherwise (a malformed or oversized record or index).</summary>
    private void ReportFileFault(RepositoryReadFailure failure, string reason)
    {
        switch (failure)
        {
            case RepositoryReadFailure.NotManagedAssembly or RepositoryReadFailure.Unreadable:
                _edition.Error(DiagnosticCatalog.RepositoryEntryUnreadable, $"{reason}: it cannot be read as a repository entry (ISO §8.13)");
                break;
            case RepositoryReadFailure.OtherSchemaVersion:
                _edition.Error(DiagnosticCatalog.RepositoryEntryVersionSkew, $"{reason} (ISO §8.13; Annex A.1 item 67)");
                break;
            default:
                _edition.Error(DiagnosticCatalog.RepositoryEntryMalformed, $"{reason} (ISO §8.13)");
                break;
        }
    }

    /// <summary>⛔ The words the importing group gives the class and interface identities <paramref name="record"/> names in its
    /// object-reference descriptions (§6.4, §8.7): each identity's class or interface of this group, by externalized name —
    /// the record keeps the PUBLISHING element's words, which mean nothing here. Null (REPO-11 reported when
    /// <paramref name="report"/>) when an identity is no class or interface of the group.</summary>
    private IReadOnlyDictionary<(bool Interface, string Identity), string>? Words(RepositoryEntry entry, RepositoryRecord record, bool report)
    {
        var words = new Dictionary<(bool Interface, string Identity), string>();
        foreach (var pic in RepositoryCodec.ObjectReferences(record))
        {
            bool isInterface = pic.ObjectRefKind is ObjectReferenceKind.Interface;
            string identity = pic.ObjectRefIdentity!;
            if (words.ContainsKey((isInterface, identity))) continue;
            string? word = isInterface
                ? _ooClasses?.FindInterfaceByExternalizedName(identity)?.Name
                : _ooClasses?.FindByExternalizedName(identity)?.Name;
            if (word is null)
            {
                if (report)
                    _edition.Error(DiagnosticCatalog.RepositoryIdentityUnanswered,
                        $"the repository entry '{entry.Path}' describes {record.ExternalizedName}'s object reference to the "
                        + $"{(isInterface ? "interface" : "class")} \"{identity}\", which no {(isInterface ? "interface" : "class")} of this "
                        + "compilation group is (ISO §12.3.8.4 GR6)");
                return null;
            }
            words[(isInterface, identity)] = word;
        }
        return words;
    }
}
