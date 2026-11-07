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

/// <summary>The external repository one compilation reads (ISO §2023:8.13; DESIGN-external-repository §8.6, §9.3): the
/// <c>--repository</c> directories in the order given — the output directory when none is given — then the
/// <c>--reference</c> assemblies in order. <paramref name="OutputPath"/> is the assembly this compilation produces, which is
/// never consulted for resolution (its previous build only by <paramref name="Check"/>'s comparison, §2023:9.2).
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

/// <summary>The kinds of unit one question of the external repository asks for (DESIGN-external-repository §5.1): a program (a
/// definition, else a prototype), a function (likewise), a class or an interface — an expansion is one of the last two, told by its
/// record — or the skeleton of a parameterized class or interface (§2023:10.5).</summary>
internal enum RepositoryFamily { Program, Function, Class, Interface, ParameterizedClass, ParameterizedInterface }

/// <summary>An entry of the external repository: the assembly at <paramref name="Path"/> (module version id
/// <paramref name="Mvid"/>) that publishes the unit <paramref name="Kind"/> <paramref name="ExternalizedName"/>
/// (DESIGN-external-repository §8.0), whose assembly simple name is <paramref name="AssemblyName"/>. Its program or function is compiled elsewhere, so a CALL through it describes its
/// operands for the run-time check (§2023:4.2).</summary>
internal sealed record RepositoryEntryOrigin(string Path, Guid Mvid, RepositoryUnitKind Kind, string ExternalizedName, string AssemblyName) : RepositoryOrigin
{
    public override bool IsDefinitionCompiledHere => false;

    public override string Describe(string kind) =>
        $"the {kind}{(Kind is RepositoryUnitKind.ProgramPrototype or RepositoryUnitKind.FunctionPrototype ? " prototype" : "")} "
        + $"\"{ExternalizedName}\" of the repository entry '{Path}'";
}

/// <summary>
/// ⛔ THE EXTERNAL REPOSITORY'S PROVIDER (ISO §2023:12.3.8.4 GR10 c) / GR11 c), §2023:8.13; DESIGN-external-repository §8.0, §8.2 step
/// 3, §2023:8.3, §2023:8.6): what the compiled assemblies of the search space publish, read through the runtime's
/// <see cref="RepositoryLocator"/> (which file publishes a unit) and the compiler's <see cref="RepositoryReader"/> (its record),
/// and bound into the throw-away forest by <see cref="RepositoryCodec.Decode"/>, which gives each decoded restricted pointer
/// the identity of its prototype's signature (§2023:6.4). Nothing is loaded: the reads are prefetched metadata (§2023:15.1).
/// <list type="bullet">
/// <item><b>Search order (§2023:8.6).</b> Every location in order; in a location the definition before the prototype of the name.
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
internal sealed class MetadataRepositoryProvider : IRepositoryProvider, IOoImporter
{
    private readonly RepositorySearchSpace _space;
    private OoClassTable? _table;
    private readonly EditionContext _edition;
    private readonly string? _output;
    private readonly List<Location> _locations = [];
    private bool _consulted;
    private readonly Dictionary<(string Path, Guid Mvid), Loaded?> _loaded = [];
    private readonly Dictionary<(RepositoryFamily Family, string Name), Hit?> _byName = [];
    private readonly Dictionary<(string Word, string? OtherThan), IReadOnlyList<Hit>> _byWord = [];
    private readonly Dictionary<(RepositoryFamily Family, string Name), IReadOnlyList<string>> _reasons = [];
    private readonly HashSet<(RepositoryFamily Family, string Name)> _refused = [];
    private readonly HashSet<string> _refusedWords = new(StringComparer.Ordinal);
    private readonly Dictionary<(string Path, RepositoryUnitKind Kind, string Name), Hit?> _answers = [];

    /// <param name="space">The search space.</param>
    /// <param name="edition">Where the findings are reported.</param>
    public MetadataRepositoryProvider(RepositorySearchSpace space, EditionContext edition)
    {
        _space = space;
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

        public IReadOnlyList<RepositoryEntry> FindAll(RepositoryUnitKind kind, string name, string? excluding) =>
            IsDirectory ? RepositoryLocator.FindAll(Path, kind, name, excluding) : RepositoryLocator.FindAllInFile(Path, kind, name);

        public IReadOnlyList<string> ReferencersOf(string assemblyName, string? excluding) =>
            IsDirectory ? RepositoryLocator.ReferencersOf(Path, assemblyName, excluding)
            : RepositoryLocator.ReferencesIn(Path, assemblyName) ? [Path] : [];

        public string Describe => IsDirectory ? $"the directory '{Path}'" : $"the assembly '{Path}'";
    }

    /// <summary>An entry's records, by unit kind and folded externalized name.</summary>
    private sealed record Loaded(IReadOnlyDictionary<(RepositoryUnitKind Kind, string Name), RepositoryRecord> Records);

    /// <summary>A unit an entry publishes: its record, where it was found (<paramref name="Origin"/>, <paramref name="Registrar"/>
    /// the entry's registrar type, which names the module's namespace) and — for a program or a function, whose record is
    /// decoded as soon as it is found — the <paramref name="Decoded"/> signature. A class's or an interface's record is decoded when
    /// it is IMPORTED (<see cref="Class"/>), because decoding names other classes, which must be able to name this one.</summary>
    private sealed record Hit(RepositoryRecord Record, DecodedRecord? Decoded, RepositoryEntryOrigin Origin, string? Registrar)
    {
        /// <summary>The decoded signature of a program or a function.</summary>
        public CalleeSignature Signature => Decoded!.Signature;
    }

    /// <inheritdoc/>
    public ProgramAnswer? Program(string externalizedName, int position) =>
        ByName(RepositoryFamily.Program, externalizedName) is { } hit ? new ProgramAnswer(hit.Signature, hit.Origin) : null;

    /// <inheritdoc/>
    public FunctionAnswer? Function(string externalizedName, int position) =>
        ByName(RepositoryFamily.Function, externalizedName) is { } hit ? FunctionAnswerOf(hit) : null;

    /// <inheritdoc/>
    public IEnumerable<FunctionAnswer> FunctionsByWord(string userFunctionName, int position, string? otherThan) =>
        ByWord(userFunctionName, otherThan).Select(FunctionAnswerOf);

    /// <summary>True when the questions for a specifier — <paramref name="externalizedName"/> by name (D-R3 step 1) and, for a
    /// function, <paramref name="word"/> by word (step 2) — drew a finding of their own (REPO-3, -4, -5, -7, -8, -10, -11), so
    /// the resolver reports no REPO-1 beside it.</summary>
    public bool Refused(RepositoryFamily family, string word, string externalizedName) =>
        RefusedByName(family, externalizedName) || (family == RepositoryFamily.Function && _refusedWords.Contains(CobolNames.Fold(word)));

    /// <summary>True when the by-name question for <paramref name="externalizedName"/> drew a finding of its own.</summary>
    private bool RefusedByName(RepositoryFamily family, string externalizedName) => _refused.Contains(Key(family, externalizedName));

    /// <summary>What REPO-1 says was searched for <paramref name="externalizedName"/>: every location in order, why a file named
    /// after the unit was no answer, and the files the directory index skipped for their size (§2023:15.2).</summary>
    public string Searched(RepositoryFamily family, string externalizedName)
    {
        if (_locations.Count == 0) return "no external repository is named (no --repository directory or --reference file, and no output directory)";
        var parts = new List<string> { "searched: " + string.Join(", ", _locations.Select(l => l.Describe)) };
        if (_reasons.TryGetValue(Key(family, externalizedName), out var reasons)) parts.AddRange(reasons);
        foreach (var location in _locations.Where(l => l.IsDirectory))
            if (RepositoryLocator.SkippedForSize(location.Path) is { Count: > 0 } skipped)
                parts.Add($"skipped for exceeding {RepositoryLocator.MaxFileBytes} bytes: {string.Join(", ", skipped)}");
        return string.Join("; ", parts);
    }

    /// <summary>Every entry the compilation opened that publishes a program or function — what §2023:9.2's check compares the
    /// group's units against beside the previous build of the output (DESIGN-external-repository §9.2 (b): "the entries the
    /// resolver already opened"), with the record of each unit.</summary>
    public IEnumerable<(string Path, RepositoryRecord Record)> OpenedRecords() =>
        _loaded.Where(e => e.Value is not null).SelectMany(e => e.Value!.Records.Values.Select(r => (e.Key.Path, r)));

    private static (RepositoryFamily, string) Key(RepositoryFamily family, string name) => (family, CobolNames.Fold(name));

    private static FunctionAnswer FunctionAnswerOf(Hit hit) =>
        new(new UserFunctionSignature(hit.Record.Name, hit.Record.ExternalizedName, hit.Decoded!.Returning, hit.Decoded.Formals,
            DefinitionCompiledHere: false), hit.Origin);

    /// <summary>The unit kinds a question of <paramref name="family"/> is answered by, in the order a location is searched: a
    /// definition before the prototype of its name.</summary>
    private static RepositoryUnitKind[] KindsOf(RepositoryFamily family) => family switch
    {
        RepositoryFamily.Function => [RepositoryUnitKind.Function, RepositoryUnitKind.FunctionPrototype],
        RepositoryFamily.Class => [RepositoryUnitKind.Class, RepositoryUnitKind.Expansion],
        RepositoryFamily.Interface => [RepositoryUnitKind.Interface, RepositoryUnitKind.Expansion],
        RepositoryFamily.ParameterizedClass => [RepositoryUnitKind.ParameterizedClass],
        RepositoryFamily.ParameterizedInterface => [RepositoryUnitKind.ParameterizedInterface],
        _ => [RepositoryUnitKind.Program, RepositoryUnitKind.ProgramPrototype],
    };

    private static string KindWord(RepositoryFamily family) => family switch
    {
        RepositoryFamily.Function => "function", RepositoryFamily.Class => "class", RepositoryFamily.Interface => "interface",
        RepositoryFamily.ParameterizedClass => "parameterized class", RepositoryFamily.ParameterizedInterface => "parameterized interface", _ => "program",
    };

    private void Consult()
    {
        if (_consulted) return;
        _consulted = true;
        _space.Inputs.RecordRepositoryRead();
    }

    /// <summary>GR10 c) / GR11 c): the first entry of the search space publishing the name (§2023:8.6), decoded; REPO-2 for a later
    /// different entry whose signature differs.</summary>
    private Hit? ByName(RepositoryFamily family, string externalizedName)
    {
        var key = Key(family, externalizedName);
        if (_byName.TryGetValue(key, out var cached)) return cached;
        Consult();
        var entries = new List<RepositoryEntry>();
        var reasons = new List<string>();
        RepositoryFind? typedMiss = null;
        foreach (var location in _locations)
            foreach (var kind in KindsOf(family))
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
            // An expansion is a class or an interface by what its record holds: a factory type makes it a class (§2023:6.1 ClassRecord).
            if (hit?.Record is { Kind: RepositoryUnitKind.Expansion, Class: { } expanded }
                && (expanded.FactoryType is null) != (family == RepositoryFamily.Interface))
                return _byName[key] = null;
            if (hit is null) _refused.Add(key);
            else
                foreach (var later in entries.Skip(1))
                    if (Answer(later, report: false) is { } shadowed && FirstDifference(hit, shadowed) is { } difference)
                        _edition.Warning(DiagnosticCatalog.RepositoryEntryShadowedDiffers,
                            $"{hit.Origin.Describe(KindWord(family))} is used; {shadowed.Origin.Describe(KindWord(family))} "
                            + $"comes later in the search order and differs in {difference} (ISO §2023:8.13; DESIGN-external-repository §8.6)");
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
            if (!entries.Exists(e => e.Mvid == entry.Mvid)) entries.Add(entry);   // one build in two places is one entry (§2023:8.6)
        }
    }

    /// <summary>D-R3's repository arms (§2023:8.3): every function — definition or prototype — whose recorded WORD is
    /// <paramref name="word"/>, in search order, decoded.
    /// <list type="bullet">
    /// <item><paramref name="otherThan"/> null — step 2. A word is how a later-compiled unit names a function (§2023:8.4.6.7), so
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
                        $"the repository entry '{entry.Path}' lists {unit.Kind} \"{unit.ExternalizedName}\" in its index and carries no record of it (ISO §2023:8.13)");
            }
            else
            {
                var origin = new RepositoryEntryOrigin(entry.Path, entry.Mvid, unit.Kind, record.ExternalizedName, entry.AssemblyName);
                if (unit.Kind is RepositoryUnitKind.Class or RepositoryUnitKind.Interface or RepositoryUnitKind.Expansion
                    or RepositoryUnitKind.ParameterizedClass or RepositoryUnitKind.ParameterizedInterface)
                    hit = new Hit(record, null, origin, entry.Registrar);   // decoded when imported (Class), once it can be named; a skeleton is source
                else if (Decode(entry.Path, record, report) is { } decoded)
                    hit = new Hit(record, decoded, origin, entry.Registrar);
            }
        }
        // A refused answer is remembered only when it was reported, so a quiet look (a later entry's REPO-2 compare) never
        // hides the finding from the question that names that entry first.
        if (hit is not null || report) _answers[key] = hit;
        return hit;
    }

    /// <summary>The entry's records, or null (the finding reported when <paramref name="report"/>): another runtime major or
    /// call ABI, another schema, no signatures, an unreadable or malformed record (§14, §2023:15.4).</summary>
    private Loaded? Load(RepositoryEntry entry, bool report)
    {
        var key = (entry.Path, entry.Mvid);
        if (_loaded.TryGetValue(key, out var known)) return known;
        Loaded? loaded = null;
        if (RuntimeAbi.Skew(entry.RuntimeVersion, entry.CallAbi) is { } skew)
        {
            if (report)
                _edition.Error(DiagnosticCatalog.RepositoryEntryVersionSkew,
                    $"the repository entry '{entry.Path}' cannot be used: {skew}; recompile it (ISO §2023:8.13; Annex A.1 item 67)");
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
                                    + "--repository-update on (ISO §2023:8.13)");
                                break;
                            case RepositoryReadFailure.OtherSchemaVersion:
                                _edition.Error(DiagnosticCatalog.RepositoryEntryVersionSkew,
                                    $"the repository entry '{entry.Path}' cannot be read: {outcome.Detail}; recompile it (ISO §2023:8.13; Annex A.1 item 67)");
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
                _edition.Error(DiagnosticCatalog.RepositoryEntryUnreadable, $"{reason}: it cannot be read as a repository entry (ISO §2023:8.13)");
                break;
            case RepositoryReadFailure.OtherSchemaVersion:
                _edition.Error(DiagnosticCatalog.RepositoryEntryVersionSkew, $"{reason} (ISO §2023:8.13; Annex A.1 item 67)");
                break;
            default:
                _edition.Error(DiagnosticCatalog.RepositoryEntryMalformed, $"{reason} (ISO §2023:8.13)");
                break;
        }
    }

    /// <inheritdoc/>
    bool IOoImporter.Refused(string identity, bool isInterface) =>
        RefusedByName(isInterface ? RepositoryFamily.Interface : RepositoryFamily.Class, identity)
        || RefusedByName(isInterface ? RepositoryFamily.ParameterizedInterface : RepositoryFamily.ParameterizedClass, identity);

    /// <inheritdoc/>
    string IOoImporter.Searched(string identity, bool isInterface) =>
        Searched(isInterface ? RepositoryFamily.Interface : RepositoryFamily.Class, identity);

    private static string? FirstDifference(Hit chosen, Hit shadowed) =>
        chosen.Decoded is not null && shadowed.Decoded is not null
            ? PrototypeSignatures.FirstDifference(chosen.Signature, shadowed.Signature)
            : chosen.Record != shadowed.Record ? "its record" : null;

    /// <summary>Tell the provider the class table the identities of the records it decodes resolve in (§2023:8.7): the group's, the
    /// one the class importer is asked through. Idempotent, and set before any record that names an object reference is decoded.</summary>
    public void UseClassTable(OoClassTable table) => _table ??= table;

    private OoClassTable Table => _table ?? throw new InvalidOperationException(
        "the repository provider decoded a record before it was given the group's class table (MetadataRepositoryProvider.UseClassTable)");

    /// <summary>The record bound into its throw-away forest (§2023:6.3), every class and interface identity it names decoded to the
    /// importing group's name for it (<see cref="RepositoryCodec.Names"/>, which imports a class the repository publishes), or null
    /// with REPO-11 (an identity nothing answers) or REPO-5 (a description the codec cannot rebuild) reported when
    /// <paramref name="report"/>.</summary>
    private DecodedRecord? Decode(string path, RepositoryRecord record, bool report)
    {
        var unanswered = new List<(bool Interface, string Identity)>();
        var names = RepositoryCodec.Names(record, Table, unanswered, import: true);
        if (unanswered.Count > 0)
        {
            if (report)
                foreach (var (isInterface, identity) in unanswered)
                    Unanswered($"the repository entry '{path}' describes {record.ExternalizedName}'s object reference to", isInterface, identity);
            return null;
        }
        try { return RepositoryCodec.Decode(record, names); }
        catch (RepositoryRecordException e)
        {
            if (report)
                _edition.Error(DiagnosticCatalog.RepositoryDescriptionUnbuildable,
                    $"the repository entry '{path}': {e.Message} (ISO §2023:8.13; DESIGN-external-repository §6.3)");
            return null;
        }
    }

    /// <summary>REPO-11: <paramref name="what"/> names a class or interface identity that no class or interface of the
    /// compilation group is and no entry of the external repository publishes (ISO §2023:12.3.8.4 GR6).</summary>
    private void Unanswered(string what, bool isInterface, string identity) =>
        _edition.Error(DiagnosticCatalog.RepositoryIdentityUnanswered,
            $"{what} the {(isInterface ? "interface" : "class")} \"{identity}\", which no {(isInterface ? "interface" : "class")} of this "
            + "compilation group defines and no entry of the external repository publishes (ISO §2023:12.3.8.4 GR6; DESIGN-external-repository §14)");

    /// <summary>⛔ THE CLASS IMPORT (ISO §2023:12.3.8.3 SR6 b), §2023:12.3.8.4 GR6; DESIGN-external-repository §8.4 step 3, §2023:10.1): the class
    /// the first entry of the search space publishes under <paramref name="identity"/> (§2023:8.6), as a symbol of
    /// <see cref="OoSymbolOrigin.Imported"/> — <see cref="OoClassSymbol.ClrName"/> the TypeDef's full name under <c>global::</c> in the
    /// module's namespace (§2023:4.5), the superclass resolved by identity through the table (transitively; the standard class BASE
    /// is the table's; one nothing answers is REPO-11), one <see cref="OoMethodSymbol"/> per recorded method over the decoded
    /// <see cref="DataItem"/>s, <see cref="OoMethodSymbol.OverrideOf"/> re-linked. Downstream — INVOKE binding,
    /// <c>ActivationDescriptions.Of</c>, the universal <c>__CobolInvoke</c>, typed object references, override checks — reads the
    /// symbol as a source one. Null when nothing answers, or the entry cannot (REPO-3/-4/-5/-7/-8, REPO-6: its module's namespace
    /// is another imported module's or this compilation's own).</summary>
    public ImportedClass? Class(string identity, OoClassTable table)
    {
        if (Locate(RepositoryFamily.Class, identity, table) is not { } located) return null;
        var (hit, module) = located;
        var facts = hit.Record.Class!;
        var symbol = new OoClassSymbol(OoClassTable.ImportedKey(identity), NamingConvention.ImportedTypeName(module, facts.ClrName), OoSymbolOrigin.Imported)
        {
            Imported = new ImportedFrom(hit.Origin.Path, hit.Origin.Mvid, module.ClrName, hit.Origin.AssemblyName),
            ExternalizedName = hit.Record.ExternalizedName,
            IsFinal = facts.Final,
            Record = hit.Record,
        };
        return new ImportedClass(symbol, () => Complete(symbol, hit, Table));
    }

    /// <summary>An expansion some assembly of the search space publishes (DESIGN-external-repository §10.5): its record (the class facts and
    /// the <see cref="ExpansionIdentity"/> with its key) and the assembly that holds it, with the namespace its types live in.</summary>
    internal sealed record PublishedExpansion(RepositoryRecord Record, ImportedFrom From)
    {
        /// <summary>The expansion's identity (kind <see cref="RepositoryUnitKind.Expansion"/> records always have one).</summary>
        public ExpansionIdentity Identity => Record.Expansion!;
    }

    /// <summary>EVERY expansion of the search space published under the externalized name <paramref name="externalizedName"/>, in search
    /// order, one build in two places once (§2023:8.6): the fresh copy, the stale copies and a same-name expansion of another definition or
    /// other actuals alike, because telling them apart is the caller's (the key, then the identities). An entry that cannot be read is
    /// skipped quietly: what an expansion whose assembly is unreadable would have said, the expansion made here replaces (§2023:10.5).</summary>
    internal IReadOnlyList<PublishedExpansion> ExpansionsNamed(string externalizedName)
    {
        Consult();
        var found = new List<PublishedExpansion>();
        foreach (var location in _locations)
            foreach (var entry in location.FindAll(RepositoryUnitKind.Expansion, externalizedName, _output))
                if (!found.Exists(e => e.From.Mvid == entry.Mvid) && Answer(entry, report: false) is { Record.Expansion: not null } hit
                    && ModuleOf(hit.Registrar) is { } module)
                    found.Add(new PublishedExpansion(hit.Record, new ImportedFrom(entry.Path, entry.Mvid, module.ClrName, entry.AssemblyName)));
        return found;
    }

    /// <summary>What a file an expansion's own assembly would be written to holds (§2023:10.5).</summary>
    internal enum TargetFile
    {
        /// <summary>Nothing: the file does not exist.</summary>
        Absent,
        /// <summary>An expansion of that name this compiler can read (stale or fresh): the compilation that finds it fresh imports it, and
        /// one that finds it stale replaces it under REPO-9.</summary>
        Expansion,
        /// <summary>An assembly of this family of another repository schema, runtime major or call ABI: replaced under REPO-12.</summary>
        Skewed,
        /// <summary>Anything else — another unit, or no WiseOwl COBOL module at all: not ours to overwrite.</summary>
        Foreign,
    }

    /// <summary>The directory the compilation's output lives in, where an expansion's own assembly is written (§2023:10.5).</summary>
    internal string OutputDirectory => Path.GetDirectoryName(_output ?? Path.GetFullPath("x.dll"))!;

    /// <summary>What <paramref name="path"/>, the file an expansion externalized <paramref name="externalizedName"/> would be written to, holds,
    /// with the reason in words for <see cref="TargetFile.Skewed"/> and <see cref="TargetFile.Foreign"/>.</summary>
    internal (TargetFile State, string? Why) StateOfExpansionFile(string path, string externalizedName)
    {
        _space.Inputs.RecordRepositoryRead();
        if (!_space.Inputs.FileExists(path)) return (TargetFile.Absent, null);
        var find = RepositoryLocator.FindInFile(path, RepositoryUnitKind.Expansion, externalizedName);
        if (find.Entry is { } entry)
            return RuntimeAbi.Skew(entry.RuntimeVersion, entry.CallAbi) is { } skew ? (TargetFile.Skewed, skew) : (TargetFile.Expansion, null);
        return find.Failure is RepositoryReadFailure.OtherSchemaVersion ? (TargetFile.Skewed, find.Reason)
            : (TargetFile.Foreign, find.Reason is { } reason ? $"is no expansion of that name ({reason})" : "is no expansion of that name");
    }

    /// <summary>The assemblies of the search space that REFERENCE the assembly named <paramref name="assemblyName"/>, read from each
    /// file's <c>AssemblyRef</c> table (nothing is loaded): the groups compiled against it (§2023:10.5's stale-expansion scan), each once.</summary>
    internal IReadOnlyList<string> ReferencersOf(string assemblyName)
    {
        Consult();
        return [.. _locations.SelectMany(l => l.ReferencersOf(assemblyName, _output)).Distinct(StringComparer.OrdinalIgnoreCase)];   // not a COBOL word: a file path
    }

    /// <summary>The simple name of the assembly this compilation produces (its output file's name, which is what the backend names the
    /// assembly, <c>ICodeGenBackend</c>), or null for a compilation with no output path.</summary>
    internal string? OutputAssemblyName => _output is null ? null : Path.GetFileNameWithoutExtension(_output);

    /// <summary>The namespace the types of the module whose registrar type is <paramref name="registrar"/> live in (§2023:4.5), or null.</summary>
    private static ModuleNamespace? ModuleOf(string? registrar)
    {
        string suffix = "." + ModuleNamespace.RegistrarClass;
        return registrar is not null && registrar.EndsWith(suffix, StringComparison.Ordinal) ? new ModuleNamespace(registrar[..^suffix.Length]) : null;
    }

    /// <inheritdoc/>
    public ImportedSkeleton? Skeleton(string identity, bool isInterface)
    {
        if (ByName(isInterface ? RepositoryFamily.ParameterizedInterface : RepositoryFamily.ParameterizedClass, identity) is not { } hit) return null;
        return new ImportedSkeleton(hit.Record.Skeleton!, new ImportedFrom(hit.Origin.Path, hit.Origin.Mvid, "", hit.Origin.AssemblyName));
    }

    /// <summary>⛔ THE INTERFACE IMPORT (ISO §2023:12.3.8.3 SR9, §2023:12.3.8.4 GR9; DESIGN-external-repository §10.6): the twin of
    /// <see cref="Class"/> — <c>[CobolUnit(Kind = Interface)]</c> on the public CLR interface, its INHERITS list resolved by identity
    /// (transitively), one prototype per recorded method over the decoded forest.</summary>
    public ImportedInterface? Interface(string identity, OoClassTable table)
    {
        if (Locate(RepositoryFamily.Interface, identity, table) is not { } located) return null;
        var (hit, module) = located;
        var symbol = new OoInterfaceSymbol(OoClassTable.ImportedInterfaceKey(identity),
            NamingConvention.ImportedTypeName(module, hit.Record.Class!.ClrName), OoSymbolOrigin.Imported)
        {
            Imported = new ImportedFrom(hit.Origin.Path, hit.Origin.Mvid, module.ClrName, hit.Origin.AssemblyName),
            ExternalizedName = hit.Record.ExternalizedName,
            Record = hit.Record,
        };
        return new ImportedInterface(symbol, () => Complete(symbol, hit, Table));
    }

    /// <summary>The first entry of the search space publishing the class or interface <paramref name="identity"/> (§2023:8.6) and the
    /// namespace its types live in (the registrar type's, §2023:4.5): null — with the finding reported and the question remembered as
    /// refused — when nothing answers, the entry names no registrar (REPO-4), or its namespace is another needed module's or this
    /// compilation's own (REPO-6).</summary>
    private (Hit Hit, ModuleNamespace Module)? Locate(RepositoryFamily family, string identity, OoClassTable table)
    {
        UseClassTable(table);
        if (ByName(family, identity) is not { } hit) return null;
        var key = Key(family, identity);
        string kind = KindWord(family);
        string registrar = hit.Registrar ?? "";
        string suffix = "." + ModuleNamespace.RegistrarClass;
        if (!registrar.EndsWith(suffix, StringComparison.Ordinal))
        {
            _edition.Error(DiagnosticCatalog.RepositoryEntryMalformed,
                $"the repository entry '{hit.Origin.Path}' publishes the {kind} \"{hit.Record.ExternalizedName}\" and names no registrar type, "
                + "so the namespace its types live in is unknown (ISO §2023:8.13; DESIGN-external-repository §4.5)");
            _refused.Add(key);
            return null;
        }
        var module = new ModuleNamespace(registrar[..^suffix.Length]);
        if (NamespaceCollision(hit, module) is { } collision)
        {
            _edition.Error(DiagnosticCatalog.RepositoryAssemblyNamespaceCollision,
                $"the repository entry '{hit.Origin.Path}' publishes the {kind} \"{hit.Record.ExternalizedName}\" in the namespace "
                + $"{module.ClrName}, which {collision}; one assembly simple name names one assembly per load context (ISO §2023:8.13; "
                + "DESIGN-external-repository §8.6)");
            _refused.Add(key);
            return null;
        }
        return (hit, module);
    }

    /// <summary>What the other module or this compilation's own output already holds that <paramref name="module"/> collides with
    /// (REPO-6), or null.</summary>
    private string? NamespaceCollision(Hit hit, ModuleNamespace module)
    {
        foreach (var from in Table.ImportedClasses.Select(c => c.Imported!).Concat(Table.ImportedInterfaces.Select(i => i.Imported!)))
            if (from.ClrNamespace == module.ClrName && from.Mvid != hit.Origin.Mvid)
                return $"the entry '{from.AssemblyFile}' (another build) already holds";
        if (OutputAssemblyName is { } output && CsNames.ModuleNamespaceOf(output).ClrName == module.ClrName)
            return "this compilation's own output assembly holds";
        return null;
    }

    /// <summary>The second half of the import: the superclass and the implemented interfaces by identity, the methods over the
    /// decoded forest, the overrides.</summary>
    private void Complete(OoClassSymbol symbol, Hit hit, OoClassTable table)
    {
        var facts = hit.Record.Class!;
        string what = $"the repository entry '{hit.Origin.Path}', class \"{hit.Record.ExternalizedName}\", names";
        if (facts.BaseIdentity is { } baseIdentity)
        {
            if (table.ImportClass(baseIdentity) is { } baseSymbol) symbol.Base = baseSymbol;
            else Unanswered($"{what} its superclass", isInterface: false, baseIdentity);
        }
        Implemented(symbol.Implements, facts.Implements);
        Implemented(symbol.FactoryImplements, facts.FactoryImplements);
        if (Decode(hit.Origin.Path, hit.Record, report: true) is not { } decoded) return;
        foreach (var dm in decoded.Methods)
        {
            var m = MethodOf(symbol, dm, what);
            if (!(dm.Record.IsFactory ? symbol.TryAddFactoryMethod(m) : symbol.TryAddMethod(m)))
                _edition.Error(DiagnosticCatalog.RepositoryEntryMalformed,
                    $"{hit.Origin.Path}: the class \"{hit.Record.ExternalizedName}\" records two {(dm.Record.IsFactory ? "factory " : "")}methods "
                    + $"externalized \"{m.ExternalizedName}\" (ISO §2023:8.13)");
            else if (dm.Record.Override)
                m.OverrideOf = dm.Record.IsFactory ? symbol.Base?.FindFactoryMethod(m.ExternalizedName) : symbol.Base?.FindMethod(m.ExternalizedName);
        }

        void Implemented(List<OoInterfaceSymbol> into, RecordList<string> identities)
        {
            foreach (var identity in identities)
                if (table.ImportInterface(identity) is { } iface) into.Add(iface);
                else Unanswered($"{what} an implemented interface", isInterface: true, identity);
        }
    }

    /// <summary>The second half of an interface import: the interfaces it INHERITS by identity (a record's <c>Implements</c> is an
    /// interface's INHERITS list, ISO §2023:11.6.2) and its method prototypes over the decoded forest.</summary>
    private void Complete(OoInterfaceSymbol symbol, Hit hit, OoClassTable table)
    {
        var facts = hit.Record.Class!;
        string what = $"the repository entry '{hit.Origin.Path}', interface \"{hit.Record.ExternalizedName}\", names";
        foreach (var identity in facts.Implements)
            if (table.ImportInterface(identity) is { } inherited) symbol.Inherits.Add(inherited);
            else Unanswered($"{what} an inherited interface", isInterface: true, identity);
        if (Decode(hit.Origin.Path, hit.Record, report: true) is not { } decoded) return;
        foreach (var dm in decoded.Methods)
        {
            var proto = MethodOf(owner: null, dm, what);
            if (!symbol.TryAddPrototype(proto))
                _edition.Error(DiagnosticCatalog.RepositoryEntryMalformed,
                    $"{hit.Origin.Path}: the interface \"{hit.Record.ExternalizedName}\" records two methods externalized "
                    + $"\"{proto.ExternalizedName}\" (ISO §2023:8.13)");
        }
    }

    /// <summary>One recorded method as a symbol of the importing group (§2023:10.1): its signature is the decoded forest's, exactly the
    /// shape <c>OoStandardClasses</c> gives the standard class's methods, so every consumer that reads a method's signature —
    /// override conformance, the activation descriptions, the universal dispatch — reads it as it reads a method written in COBOL.
    /// An accessor is rostered under the pinned name its record carries (§2023:11.7.4 GR1 a)); its externalized name is its property's.</summary>
    private OoMethodSymbol MethodOf(OoClassSymbol? owner, DecodedMethod dm, string what)
    {
        var r = dm.Record;
        char accessor = r.Accessor switch { RepositoryAccessor.Get => 'G', RepositoryAccessor.Set => 'S', _ => '\0' };
        var m = new OoMethodSymbol(r.Name, hasUsing: dm.Formals.Count > 0, hasReturning: dm.Returning is not null, OoSymbolOrigin.Imported)
        {
            ClrName = r.ClrName, Owner = owner!, IsFactory = r.IsFactory, HasOverride = r.Override, IsFinal = r.Final,
            Accessor = accessor, PropertyName = r.PropertyName,
            ExternalizedName = accessor == '\0' ? r.ExternalizedName : r.Name,
        };
        var binding = new OoMethodBinding { Returning = dm.Returning };
        foreach (var f in dm.Formals)
            binding.Formals.Add(new OoFormal(f.Item, f.Position, binding.FormalName(f.Item, f.Position), f.Optional, f.ByValue, f.CarrierResident));
        m.Binding = binding;
        Passes.StorageFormPass.ClassifyImportedMethod(m);
        foreach (var raised in r.Raising)
        {
            switch (raised.Kind)
            {
                case RepositoryRaisingKind.ExceptionName:
                    m.Raising.Add(new RaisingTarget(RaisingTargetKind.ExceptionName, raised.Identity, Factory: false));
                    break;
                case RepositoryRaisingKind.ObjectClass:
                    if (Table.ImportClass(raised.Identity) is { } cls) m.Raising.Add(new RaisingTarget(RaisingTargetKind.ObjectClass, cls.Name, raised.Factory));
                    else Unanswered($"{what} RAISING", isInterface: false, raised.Identity);
                    break;
                default:
                    if (Table.ImportInterface(raised.Identity) is { } iface) m.Raising.Add(new RaisingTarget(RaisingTargetKind.Interface, iface.Name, Factory: false));
                    else Unanswered($"{what} RAISING", isInterface: true, raised.Identity);
                    break;
            }
        }
        return m;
    }
}
