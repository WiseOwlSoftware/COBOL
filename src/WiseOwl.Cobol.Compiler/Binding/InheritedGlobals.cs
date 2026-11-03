// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// What a CONTAINED program inherits from the programs that contain it (ISO §13.18.27.4 GR1: "A constant-name, data-name,
/// file-name, report-name, or screen-name described using a GLOBAL clause is a global name"; GR2: a contained program "may
/// reference that name without describing it again"; §8.4.6.2): the value slice of <see cref="DataBindState"/> whose one
/// producer is <see cref="InheritStep"/> (docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1); kb/Work PB2296),
/// which writes it BEFORE the unit's first data phase from its containers, nearest container first. Empty for an outermost
/// program and for every OO source element.
/// <para>A lookup reads the unit's own declarations first and this slice after (<see cref="DataBindState.VisibleFiles"/>,
/// <see cref="DataBinder.TryFindTypeDecl"/>), which is the §8.4.6.2.1 3) a) rule — "if the name is declared in source
/// element B, the item in source element B is the referenced item" — made structural: an own declaration never has to
/// displace an inherited one.</para>
/// </summary>
public sealed class InheritedGlobals
{
    private readonly Dictionary<string, FileModel> _files = new(CobolNames.Comparer);
    private readonly Dictionary<FileModel, int> _fileDepths = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<DataItem, FileModel> _recordAreas = new(ReferenceEqualityComparer.Instance);
    private readonly List<FileModel> _containerLocalFiles = [];
    private readonly Dictionary<string, DataItem> _typeDecls = new(CobolNames.Comparer);
    private readonly Dictionary<string, InheritedConstant> _constants = new(CobolNames.Comparer);
    private readonly HashSet<string> _shadowedSymbolics = new(CobolNames.Comparer);

    /// <summary>One inherited GLOBAL constant: its folded definition and the containment distance of the container that
    /// declared it (1 = the direct container) — the tier <see cref="InheritStep.DropShadowed"/> weighs against a
    /// same-spelled name's (kb/Work PB1047).</summary>
    public readonly record struct InheritedConstant(ConstantDef Definition, int Depth);

    /// <summary>The GLOBAL constant-names of every containing program, nearest container first (ISO §13.18.27.4 GR1–GR2:
    /// "A constant-name, data-name, file-name, report-name, or screen-name described using a GLOBAL clause is a global
    /// name", and a contained program "may reference that name without describing it again"; kb/Work PB1009). A constant
    /// occupies no storage, so it needs no bridge: the container's folded definition is read here, after the unit's own
    /// constants (<c>ConstantEntryBinder.FindConstant</c>: a local constant entry of the spelling hides the inherited one, §8.4.6), and
    /// it leaves this table when a data-name declared NEARER hides it (<see cref="InheritStep.DropShadowed"/>; ISO
    /// §8.4.6.2.1 3) a)). Written before the unit's first data phase (definition precedes reference, the §13.10.4 GR1
    /// substitution model).</summary>
    public IReadOnlyDictionary<string, InheritedConstant> Constants => _constants;

    /// <summary>Does a nearer declaration hide the inherited symbolic-character <paramref name="word"/> (§8.4.6.1; kb/Work
    /// PB3154 review R1-1)? Such a one is HIDDEN, never removed from the configuration slice's table: a contained unit
    /// inherits the whole table from this one, and this unit's local data-name hides nothing from it.</summary>
    public bool HidesSymbolic(string word) => _shadowedSymbolics.Contains(word);

    /// <summary>The GLOBAL files of every containing program, by file-name, nearest container first (§13.18.27.4 GR1: the
    /// file-name of a GLOBAL FD is a global name, visible in every directly or indirectly contained program; §8.4.6.2.1 3)
    /// b): the nearest containing element's declaration is the referenced item). Read after the unit's own files
    /// (<see cref="DataBindState.VisibleFiles"/>): an FD of this unit finds the unit's own SELECT, never an inherited model,
    /// and the shared <see cref="FileModel"/> keys the child's verbs to the owner's connector, so the child never
    /// re-registers, re-qualifies or CANCEL-closes it. (An EXTERNAL non-GLOBAL FD is not here — §13.18.22.4 GR1 NOTE 1.)</summary>
    public IReadOnlyDictionary<string, FileModel> Files => _files;

    /// <summary>How many containment levels out <paramref name="file"/> is declared: n for a GLOBAL file of the n-th
    /// container (<see cref="Files"/>), 0 for any other — the file-name twin of <see cref="ForestSlice.DeclaringDepth"/>
    /// and <see cref="ReportScope.Depth"/>, so a name a container declares as a file and a nearer element declares
    /// as something else resolves to the nearer declaration (§8.4.6.2.1 3); kb/Work PB4219).</summary>
    public int FileDepth(FileModel file) => _fileDepths.GetValueOrDefault(file);

    /// <summary>The record areas of every containing program's files that are GLOBAL names (§13.18.27.4 GR1: the record of a
    /// GLOBAL FD, or a level-1 entry with its own GLOBAL clause), each mapped to the file that owns it — §12.4.6.3.3 SR11's
    /// "a file or record area … declared with the GLOBAL clause". Keyed by the record ITSELF, never by its name: the
    /// record is a data item this unit inherits with its depth, so the one data-name lookup decides which record a written
    /// operand names, and this map only says whether that record is a GLOBAL record area and of which file (kb/Work
    /// PB3557 review R2-1).</summary>
    public IReadOnlyDictionary<DataItem, FileModel> RecordAreas => _recordAreas;

    /// <summary>Every file of every containing program that is NOT a global name here — its FD has no GLOBAL clause
    /// (§13.18.27.3 SR1 d)), so its file-name is not in <see cref="Files"/>. It is still reachable through a record: a
    /// file-section level-1 entry may carry its own GLOBAL clause (SR1 b)), and the record-name is then visible here.
    /// Read by <c>StatementValidation.ResolveRecordName</c>, to refuse WRITE / REWRITE of such a record by §14.9.51.3 SR21
    /// / §14.9.35.3 SR3 (kb/Work PB1193), and by the APPLY COMMIT screen (§12.4.6.3.3 SR1).</summary>
    public IReadOnlyList<FileModel> ContainerLocalFiles => _containerLocalFiles;

    /// <summary>The GLOBAL type declarations of every containing program (§13.18.58.4 GR3; §8.4.6.2.2 — a type-name
    /// described with a GLOBAL clause is a global name; kb/Work PB1303), the nearest container's declaration of a name
    /// kept. Kept APART from <see cref="ForestSlice.TypeDecls"/>, the source element's OWN declarations: an own declaration
    /// must neither collide with an inherited one (§8.4.6.2.1 3 a) nor be walked, emitted or re-validated as this
    /// unit's. An inherited template is already fully expanded (its container's bind finished first), and cloning it
    /// writes only to the referencing item.</summary>
    public IReadOnlyDictionary<string, DataItem> TypeDecls => _typeDecls;

    /// <summary>The containers' GLOBAL reports, nearest container first, each with how many containment levels out it
    /// lives (the <c>__outer</c> chain length a reference to its storage renders through). Like a GLOBAL FD, the report is
    /// SHARED, never re-declared: the child's verbs hold the container's <see cref="ReportModel"/>. Offered here and made
    /// visible by the unit's own data division at the end of its declaration half (<c>ReportSectionBinder.InheritContainerReports</c>),
    /// so the unit's own clauses that name a report item (FILE STATUS, ODO, ASSIGN USING) see them (kb/Work
    /// PB3766).</summary>
    public IReadOnlyList<(ReportModel Report, int Depth)> Reports { get; private set; } = [];

    /// <summary>The containers' named GLOBAL data roots, nearest container first, each with its containment distance —
    /// offered the same way and inherited at the same point (<c>DataBinder.InheritContainerGlobalData</c>): FILE STATUS,
    /// RECORD / RELATIVE KEY, OCCURS … DEPENDING ON and ASSIGN … USING of the unit's own data division resolve inside its
    /// data phases, so a container's GLOBAL item they name must already be visible (kb/Work PB3766, review finding
    /// R1-1).</summary>
    public IReadOnlyList<(DataItem Root, int Depth)> DataRoots { get; private set; } = [];

    /// <summary>Record one containing program's files, <paramref name="depth"/> containment levels out: a GLOBAL one is a
    /// visible file-name (the nearest container's declaration kept), any other is known for the one question
    /// <see cref="ContainerLocalFiles"/> answers; a record that is a GLOBAL name is recorded with its file.
    /// <paramref name="globalRoots"/> is the container's GLOBAL data roots. <see cref="InheritStep"/> only.</summary>
    internal void AddContainerFiles(IEnumerable<FileModel> files, IReadOnlyCollection<DataItem> globalRoots, int depth)
    {
        foreach (var f in files)
        {
            if (!f.IsGlobal)
                _containerLocalFiles.Add(f);
            else if (_files.TryAdd(f.CobolName, f))
                _fileDepths[f] = depth;
            foreach (var record in f.Records)
                if (globalRoots.Contains(record))
                    _recordAreas.TryAdd(record, f);
        }
    }

    /// <summary>Record one containing program's GLOBAL type declarations, the nearer container's kept.
    /// <see cref="InheritStep"/> only.</summary>
    internal void AddContainerTypeDecls(IEnumerable<KeyValuePair<string, DataItem>> typeDecls)
    {
        foreach (var (name, decl) in typeDecls)
            if (decl.HasGlobalClause)
                _typeDecls.TryAdd(name, decl);
    }

    /// <summary>Record one containing program's GLOBAL constants, <paramref name="depth"/> containment levels out, the
    /// nearer container's kept. <paramref name="visible"/> is what the container's binder offers
    /// (<c>DataBinder.GlobalConstantsToInherit</c>). <see cref="InheritStep"/> only.</summary>
    internal void AddContainerConstants(IEnumerable<KeyValuePair<string, ConstantDef>> visible, int depth)
    {
        foreach (var (name, def) in visible)
            if (def.IsGlobal)
                _constants.TryAdd(name, new(def, depth));
    }

    /// <summary>Drop the inherited constants, and hide the inherited symbolic-characters (<paramref name="symbolicDepths"/>:
    /// each with its containment distance), that a declaration in a source element NEARER than their containers' hides
    /// (<paramref name="declaredNearerThan"/>: the name and the container's distance). <see cref="InheritStep"/> only.</summary>
    internal void DropShadowed(Func<string, int, bool> declaredNearerThan, IEnumerable<KeyValuePair<string, int>> symbolicDepths)
    {
        foreach (var (name, constant) in _constants.ToList())
            if (declaredNearerThan(name, constant.Depth))
                _constants.Remove(name);
        foreach (var (name, depth) in symbolicDepths)
            if (declaredNearerThan(name, depth))
                _shadowedSymbolics.Add(name);
    }

    /// <summary>Offer the containers' GLOBAL reports and data roots, nearest container first. <see cref="InheritStep"/>
    /// only.</summary>
    internal void OfferContainerGlobals(IReadOnlyList<(ReportModel Report, int Depth)> reports, IReadOnlyList<(DataItem Root, int Depth)> roots)
    {
        Reports = reports;
        DataRoots = roots;
    }
}
