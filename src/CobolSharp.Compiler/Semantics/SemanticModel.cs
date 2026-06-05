// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Runtime;
using CobolSharp.Compiler.CodeGen;
using CobolSharp.Compiler.Diagnostics;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// The SemanticModel is the single surface the binder consumes.
/// All real work stays in SemanticBuilder/ReferenceResolver/PicUsageResolver —
/// this just exposes results cleanly so the binder stays thin.
/// </summary>
public sealed class SemanticModel
{
    public ProgramSymbol Program { get; }
    public SymbolTable Symbols { get; }
    public DiagnosticBag Diagnostics { get; }

    /// <summary>
    /// Path of the source file being compiled. Threaded from <see cref="Compilation.Compile"/> so every
    /// post-parse diagnostic (semantic, binder, flow) reports the real file path instead of a placeholder.
    /// Defaults to the <see cref="Common.SourceLocation.None"/> sentinel until set.
    /// </summary>
    public string SourceName { get; set; } = "<source>";

    /// <summary>
    /// Program-level PIC formatting environment (CURRENCY SIGN, DECIMAL-POINT IS COMMA).
    /// Set from SPECIAL-NAMES during semantic analysis. Default: '$', period as decimal.
    /// </summary>
    public Runtime.PicEnvironment PicEnvironment { get; private set; } = Runtime.PicEnvironment.Default;

    public void SetPicEnvironment(char currencySign, char currencyOutputChar, bool decimalPointIsComma)
        => PicEnvironment = new Runtime.PicEnvironment(currencySign, currencyOutputChar, decimalPointIsComma);

    // ── Screen items from SCREEN SECTION ──

    private readonly List<Bound.BoundScreenItem> _screenItems = [];
    public IReadOnlyList<Bound.BoundScreenItem> ScreenItems => _screenItems;

    internal void RegisterScreenItems(IEnumerable<Bound.BoundScreenItem> items)
    {
        foreach (var item in items)
            _screenItems.Add(item);
    }

    // ── Implementor switches from SPECIAL-NAMES ──

    private readonly Dictionary<string, ImplementorSwitch> _implementorSwitches =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, ImplementorSwitch> ImplementorSwitches => _implementorSwitches;

    internal void RegisterImplementorSwitch(ImplementorSwitch sw)
        => _implementorSwitches[sw.Name] = sw;

    public ImplementorSwitch? ResolveImplementorSwitch(string name)
        => _implementorSwitches.TryGetValue(name, out var sw) ? sw : null;

    /// <summary>
    /// Resolve a condition name to its implementor switch and ON/OFF state.
    /// Returns null if the name is not a switch condition.
    /// </summary>
    public (ImplementorSwitch Switch, bool IsOn)? ResolveSwitchCondition(string name)
    {
        foreach (var sw in _implementorSwitches.Values)
        {
            if (sw.OnValueName != null &&
                string.Equals(sw.OnValueName, name, StringComparison.OrdinalIgnoreCase))
                return (sw, true);
            if (sw.OffValueName != null &&
                string.Equals(sw.OffValueName, name, StringComparison.OrdinalIgnoreCase))
                return (sw, false);
        }
        return null;
    }

    // ── User-defined CLASS conditions from SPECIAL-NAMES ──

    private readonly Dictionary<string, ClassDefinition> _classDefinitions =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, ClassDefinition> ClassDefinitions => _classDefinitions;

    internal void RegisterClassDefinition(ClassDefinition classDef)
        => _classDefinitions[classDef.Name] = classDef;

    public ClassDefinition? ResolveClassDefinition(string name)
        => _classDefinitions.TryGetValue(name, out var def) ? def : null;

    // ── SYMBOLIC CHARACTERS from SPECIAL-NAMES ──

    private readonly Dictionary<string, byte> _symbolicCharacters =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, byte> SymbolicCharacters => _symbolicCharacters;

    internal void RegisterSymbolicCharacter(string name, byte value)
        => _symbolicCharacters[name] = value;

    public byte? ResolveSymbolicCharacter(string name)
        => _symbolicCharacters.TryGetValue(name, out var val) ? val : null;

    // ── ALPHABET definitions from SPECIAL-NAMES ──

    private readonly Dictionary<string, AlphabetDefinition> _alphabetDefinitions =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, AlphabetDefinition> AlphabetDefinitions => _alphabetDefinitions;

    internal void RegisterAlphabetDefinition(AlphabetDefinition def)
        => _alphabetDefinitions[def.Name] = def;

    public AlphabetDefinition? ResolveAlphabetDefinition(string name)
        => _alphabetDefinitions.TryGetValue(name, out var def) ? def : null;

    // ── PROGRAM COLLATING SEQUENCE ──

    /// <summary>
    /// The collating sequence byte[] (256 entries mapping character ordinal → sort weight).
    /// Null when default (native) collating sequence is used.
    /// </summary>
    public byte[]? ProgramCollatingSequence { get; private set; }

    internal void SetProgramCollatingSequence(byte[] sequence)
        => ProgramCollatingSequence = sequence;

    // ── Extension clauses (vendor extensions, unrecognized clauses) ──

    private readonly List<ExtensionClauseNode> _extensionClauses = new();

    /// <summary>All extension/vendor clauses captured during parsing, classified by context.</summary>
    public IReadOnlyList<ExtensionClauseNode> ExtensionClauses => _extensionClauses;

    internal void AddExtensionClause(ExtensionClauseNode clause) => _extensionClauses.Add(clause);

    /// <summary>Get extension clauses for a specific context.</summary>
    public IEnumerable<ExtensionClauseNode> GetExtensionClauses(GenericClauseContext context)
        => _extensionClauses.Where(c => c.Context == context);

    /// <summary>Get extension clauses of a specific typed subclass.</summary>
    public IEnumerable<T> GetExtensionClauses<T>() where T : ExtensionClauseNode
        => _extensionClauses.OfType<T>();


    // ── Data items in declaration order (all levels, preserves FILLERs) ──

    private IReadOnlyList<DataSymbol> _dataItemsInOrder = [];
    public IReadOnlyList<DataSymbol> DataItemsInOrder => _dataItemsInOrder;

    public void SetDataItemsInOrder(IReadOnlyList<DataSymbol> items)
        => _dataItemsInOrder = items;

    // ── Data records (01/77-level items) ──

    private readonly List<DataSymbol> _dataRecords = [];
    public IReadOnlyList<DataSymbol> DataRecords => _dataRecords;

    // ── Procedure structure (declaration order) ──

    private readonly List<ParagraphSymbol> _paragraphsInOrder = [];
    public IReadOnlyList<ParagraphSymbol> ParagraphsInOrder => _paragraphsInOrder;

    private readonly List<SectionSymbol> _sectionsInOrder = [];
    public IReadOnlyList<SectionSymbol> SectionsInOrder => _sectionsInOrder;

    // Section → ordered list of paragraph names within that section
    private readonly Dictionary<string, List<string>> _sectionParagraphs =
        new(StringComparer.OrdinalIgnoreCase);
    // Paragraph → section it belongs to (null if orphan)
    private readonly Dictionary<string, string> _paragraphSection =
        new(StringComparer.OrdinalIgnoreCase);

    // ── USE declarative associations (file-name → section name) ──

    private readonly Dictionary<string, string> _useDeclaratives =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Map from file name to the declarative section that handles its I/O errors
    /// (USE AFTER STANDARD ERROR PROCEDURE ON file-name…).</summary>
    public IReadOnlyDictionary<string, string> UseDeclaratives => _useDeclaratives;

    /// <summary>Register a file-name-scoped USE AFTER ERROR declarative.</summary>
    public void RegisterUseDeclarative(string fileName, string sectionName)
        => _useDeclaratives[fileName] = sectionName;

    // Open-mode-scoped USE declaratives (USE AFTER ERROR PROCEDURE ON INPUT/OUTPUT/I-O/EXTEND):
    // they apply to every file currently open in that mode, so they are keyed by mode, not file name.
    private readonly Dictionary<Bound.OpenMode, string> _useDeclarativesByMode = new();

    /// <summary>Map from open mode to the declarative section that handles I/O errors for files
    /// opened in that mode.</summary>
    public IReadOnlyDictionary<Bound.OpenMode, string> UseDeclarativesByMode => _useDeclarativesByMode;

    /// <summary>Register an open-mode-scoped USE AFTER ERROR declarative.</summary>
    public void RegisterUseDeclarativeForMode(Bound.OpenMode mode, string sectionName)
        => _useDeclarativesByMode[mode] = sectionName;

    // ── GLOBAL USE declaratives (USE GLOBAL AFTER ERROR …) ──
    // A USE GLOBAL declarative in THIS program also handles I/O exceptions arising in CONTAINED programs
    // that have no applicable USE declarative of their own (ISO §14.9.49.4 GR4 / §8.4.6.2.2). These are
    // recorded so the container can emit a registered cross-program handler and a contained program can
    // dispatch it. Scope: -1 = file-name-scoped (FileName set); 0/1/2/3 = INPUT/OUTPUT/I-O/EXTEND.

    /// <summary>A GLOBAL USE AFTER ERROR declarative: its scope, optional file name, and the declarative
    /// section that handles the condition.</summary>
    public sealed record GlobalUseDeclarative(int Scope, string? FileName, string SectionName);

    private readonly List<GlobalUseDeclarative> _globalUseDeclaratives = [];

    /// <summary>GLOBAL USE AFTER ERROR declaratives declared in this program (in declaration order).</summary>
    public IReadOnlyList<GlobalUseDeclarative> GlobalUseDeclaratives => _globalUseDeclaratives;

    /// <summary>Register a GLOBAL USE AFTER ERROR declarative (file-name-scoped: scope -1, FileName set;
    /// open-mode-scoped: scope 0/1/2/3, FileName null).</summary>
    public void RegisterGlobalUseDeclarative(int scope, string? fileName, string sectionName)
        => _globalUseDeclaratives.Add(new GlobalUseDeclarative(scope, fileName, sectionName));

    // ── Storage sizes (set by ComputeStorageLayout) ──

    public int WorkingStorageSize { get; set; }
    public int FileSectionSize { get; set; }
    public int LinkageSectionSize { get; set; }
    public int LocalStorageSize { get; set; }

    // ── PROCEDURE DIVISION USING/RETURNING parameters ──

    private List<DataSymbol> _procedureUsingParameters = [];
    private DataSymbol? _procedureReturningItem;

    /// <summary>Ordered list of LINKAGE SECTION items from PROCEDURE DIVISION USING.</summary>
    public IReadOnlyList<DataSymbol> ProcedureUsingParameters => _procedureUsingParameters;

    /// <summary>The LINKAGE SECTION item from PROCEDURE DIVISION RETURNING (COBOL-2002+).</summary>
    public DataSymbol? ProcedureReturningItem => _procedureReturningItem;

    public void SetProcedureUsingParameters(IReadOnlyList<DataSymbol> parameters)
        => _procedureUsingParameters = [..parameters];

    public void SetProcedureReturningItem(DataSymbol? item)
        => _procedureReturningItem = item;

    /// <summary>Names of the COBOL-2002 user-defined functions (FUNCTION-ID units) in this compilation group.
    /// A `FUNCTION user-name(args)` reference to one of these is a user-function call (lowered to a CALL …
    /// RETURNING), not an intrinsic. Shared across all units of the group (a caller may precede the function).</summary>
    public IReadOnlyCollection<string> UserFunctionNames { get; set; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    // ── Storage locations per data symbol (set by ComputeStorageLayout) ──

    private readonly Dictionary<DataSymbol, CodeGen.StorageLocation> _storageLocations = [];

    // ── Initial VALUE clauses (typed) ──

    public sealed record InitialValue(object Value, Runtime.CobolCategory Category);

    private readonly Dictionary<DataSymbol, InitialValue> _initialValues = [];

    public void RegisterInitialValue(DataSymbol symbol, object value, Runtime.CobolCategory category)
        => _initialValues[symbol] = new InitialValue(value, category);

    public IReadOnlyDictionary<DataSymbol, InitialValue> InitialValues => _initialValues;

    // ── Figurative initial values (field-filling VALUE SPACE, HIGH-VALUE, etc.) ──

    private readonly Dictionary<DataSymbol, FigurativeKind> _figurativeInitValues = [];

    public void RegisterFigurativeInit(DataSymbol symbol, FigurativeKind figurativeKind)
        => _figurativeInitValues[symbol] = figurativeKind;

    public IReadOnlyDictionary<DataSymbol, FigurativeKind> FigurativeInitValues => _figurativeInitValues;

    // ── Parse node → symbol mapping (for binder lookups) ──

    private readonly Dictionary<object, Symbol> _nodeToSymbol = [];

    public SemanticModel(ProgramSymbol program, SymbolTable symbols, DiagnosticBag diagnostics)
    {
        Program = program;
        Symbols = symbols;
        Diagnostics = diagnostics;
    }

    // ── Registration (called by semantic passes) ──

    public void AddDataRecord(DataSymbol record) => _dataRecords.Add(record);
    public void AddParagraph(ParagraphSymbol paragraph) => _paragraphsInOrder.Add(paragraph);
    public void AddSection(SectionSymbol section) => _sectionsInOrder.Add(section);

    /// <summary>Register a paragraph as belonging to a section.</summary>
    public void RegisterSectionParagraph(string sectionName, string paragraphName)
    {
        if (!_sectionParagraphs.TryGetValue(sectionName, out var list))
        {
            list = new List<string>();
            _sectionParagraphs[sectionName] = list;
        }
        list.Add(paragraphName);
        _paragraphSection[paragraphName] = sectionName;
    }

    /// <summary>Get the ordered paragraph names within a section.</summary>
    public IReadOnlyList<string>? GetSectionParagraphs(string sectionName)
        => _sectionParagraphs.TryGetValue(sectionName, out var list) ? list : null;

    /// <summary>Get the section a paragraph belongs to (null if orphan).</summary>
    public string? GetParagraphSection(string paragraphName)
        => _paragraphSection.TryGetValue(paragraphName, out var sec) ? sec : null;

    public void RegisterStorageLocation(DataSymbol symbol, StorageLocation loc)
        => _storageLocations[symbol] = loc;

    /// <summary>
    /// Make an item declared IS GLOBAL in a containing program resolvable by name in this program
    /// (ISO §8.4.5), mapping it to the owning program's storage via <paramref name="ownerLocation"/>
    /// (whose <see cref="CodeGen.StorageLocation.OwnerProgramId"/> is set). Returns false if the name
    /// is already declared locally — a local declaration shadows the inherited global.
    /// </summary>
    public bool TryInheritGlobal(DataSymbol symbol, StorageLocation ownerLocation)
    {
        if (!Symbols.Program.DataDivisionScope.TryDeclare(symbol, out _))
            return false;
        RegisterStorageLocation(symbol, ownerLocation);
        return true;
    }

    /// <summary>
    /// Make a file-name declared FD ... IS GLOBAL in a containing program resolvable by name in this
    /// program (ISO §8.4.6.2.2), so OPEN/READ/CLOSE here bind to the containing program's file connector.
    /// The same <see cref="FileSymbol"/> instance is shared (its <see cref="FileSymbol.Record"/> is the
    /// containing program's record); the runtime FileRuntime state is name-keyed so the open file is
    /// already shared. Returns false if a file of the same name is already declared locally (which shadows
    /// the inherited global).
    /// </summary>
    public bool TryInheritGlobalFile(FileSymbol file)
        => Symbols.Program.GlobalScope.TryDeclare(file, out _);

    public void RegisterNodeSymbol(object parseNode, Symbol symbol)
        => _nodeToSymbol[parseNode] = symbol;

    // ── Queries (consumed by binder) ──

    public StorageLocation? GetStorageLocation(DataSymbol symbol)
        => _storageLocations.TryGetValue(symbol, out var loc) ? loc : null;

    public Symbol? GetSymbol(object parseNode)
        => _nodeToSymbol.TryGetValue(parseNode, out var s) ? s : null;

    /// <summary>Resolve a data item name.</summary>
    public DataSymbol? ResolveData(string name)
        => Symbols.Program.DataDivisionScope.Resolve<DataSymbol>(name);

    /// <summary>
    /// Resolve a data-name with OF/IN qualifiers (innermost-first, as stored in <see
    /// cref="FileSymbol.RecordKeyQualifiers"/>). With no qualifiers this is just <see cref="ResolveData"/>;
    /// otherwise the outermost qualifier is resolved first and the chain is walked inward, so a base name
    /// shared by several items (e.g. three <c>IX-FD3-KEY</c> keys) resolves to the one under the named group.
    /// </summary>
    public DataSymbol? ResolveQualifiedData(string name, IReadOnlyList<string> qualifiers)
    {
        if (qualifiers.Count == 0) return ResolveData(name);
        var context = ResolveData(qualifiers[^1]);
        if (context == null) return null;
        for (int i = qualifiers.Count - 2; i >= 0; i--)
        {
            context = FindChildData(context, qualifiers[i]);
            if (context == null) return null;
        }
        return FindChildData(context, name);
    }

    /// <summary>Find a descendant data item by name within a group (depth-first).</summary>
    private static DataSymbol? FindChildData(DataSymbol parent, string name)
    {
        foreach (var child in parent.Children)
        {
            if (string.Equals(child.Name, name, StringComparison.OrdinalIgnoreCase))
                return child;
            if (FindChildData(child, name) is { } deep) return deep;
        }
        return null;
    }

    /// <summary>Resolve a file's record key to its data item: <paramref name="keyIndex"/> -1 = the prime
    /// RECORD KEY, 0+ = the matching ALTERNATE RECORD KEY. Honors each key's OF/IN qualifiers so keys that
    /// share a base name resolve to distinct record positions. Null if the file declares no such key.</summary>
    public DataSymbol? ResolveKeyData(FileSymbol file, int keyIndex)
    {
        if (keyIndex < 0)
            return file.RecordKey != null ? ResolveQualifiedData(file.RecordKey, file.RecordKeyQualifiers) : null;
        if (keyIndex >= file.AlternateKeys.Count) return null;
        var ak = file.AlternateKeys[keyIndex];
        return ResolveQualifiedData(ak.DataName, ak.Qualifiers);
    }

    /// <summary>
    /// Resolve a START / READ KEY operand to its key of reference: -1 for the prime record key, 0+ for an
    /// alternate record key index, or null if the operand is neither a key nor a leftmost generic-key prefix.
    /// Per ISO §14.9.41 the operand identifies a key by POSITION — it must begin at the key's leftmost byte
    /// and be no longer than the key (so a shorter generic operand positions on the matching leftmost
    /// portion, a longer or differently-placed operand is invalid). Comparing storage locations (not names)
    /// is what lets identically-named qualified keys, a REDEFINES of a key, and a leftmost subfield all
    /// resolve correctly. Storage offsets are available because this runs after <see
    /// cref="StorageLayoutComputer"/>; a name-equality fallback covers the (post-layout-shouldn't-happen)
    /// case where a location is unavailable.
    /// </summary>
    public int? ResolveKeyOfReference(FileSymbol file, DataSymbol operand)
    {
        var opLoc = GetStorageLocation(operand);
        if (opLoc == null)
        {
            // Locations unavailable — fall back to base-name equality (only correct for unique key names).
            if (file.RecordKey != null && string.Equals(operand.Name, file.RecordKey, StringComparison.OrdinalIgnoreCase))
                return -1;
            for (int i = 0; i < file.AlternateKeys.Count; i++)
                if (string.Equals(operand.Name, file.AlternateKeys[i].DataName, StringComparison.OrdinalIgnoreCase))
                    return i;
            return null;
        }
        if (IsKeyPrefix(opLoc.Value, ResolveKeyData(file, -1))) return -1;
        for (int i = 0; i < file.AlternateKeys.Count; i++)
            if (IsKeyPrefix(opLoc.Value, ResolveKeyData(file, i))) return i;
        return null;
    }

    /// <summary>True when <paramref name="opLoc"/> begins at the key's leftmost byte (same storage area and
    /// offset) and is no longer than the key — the ISO §14.9.41 key-of-reference / generic-key test.</summary>
    private bool IsKeyPrefix(CodeGen.StorageLocation opLoc, DataSymbol? keySym)
    {
        var keyLoc = keySym != null ? GetStorageLocation(keySym) : null;
        return keyLoc != null
            && opLoc.Area == keyLoc.Value.Area
            && opLoc.Offset == keyLoc.Value.Offset
            && opLoc.Length <= keyLoc.Value.Length;
    }

    /// <summary>Resolve a paragraph name.</summary>
    public ParagraphSymbol? ResolveParagraph(string name)
        => Symbols.Program.ProcedureDivisionScope.Resolve<ParagraphSymbol>(name);

    /// <summary>Resolve a section name.</summary>
    public SectionSymbol? ResolveSection(string name)
        => Symbols.Program.ProcedureDivisionScope.Resolve<SectionSymbol>(name);

    /// <summary>Resolve a file name.</summary>
    public FileSymbol? ResolveFile(string name)
        => Symbols.Program.GlobalScope.Resolve<FileSymbol>(name);

    /// <summary>The FD with a LINAGE clause, for an unqualified LINAGE-COUNTER reference (ISO §8.4.3.14
    /// SR3 / §13.18.34 — unqualified is valid only when a single LINAGE file exists). Returns the first
    /// LINAGE file; null if none.</summary>
    public FileSymbol? FindLinageFile()
    {
        foreach (var sym in Symbols.Program.GlobalScope.GetAllSymbols<FileSymbol>())
            if (sym.HasLinage) return sym;
        return null;
    }

    /// <summary>Resolve an RD report-name from the REPORT SECTION.</summary>
    public ReportSymbol? ResolveReport(string name)
        => Symbols.Program.GlobalScope.Resolve<ReportSymbol>(name);

    /// <summary>The first report, for an unqualified LINE-COUNTER/PAGE-COUNTER reference (valid only when a
    /// single report exists, ISO §8.4.3.15). Returns null if none.</summary>
    public ReportSymbol? FindFirstReport()
    {
        foreach (var sym in Symbols.Program.GlobalScope.GetAllSymbols<ReportSymbol>())
            return sym;
        return null;
    }

    /// <summary>The report whose RD names a report group entry with this name (to resolve a GENERATE
    /// report-group-name to its owning report).</summary>
    public (ReportSymbol Report, ReportGroupSymbol Group)? ResolveReportGroup(string name)
    {
        foreach (var report in Symbols.Program.GlobalScope.GetAllSymbols<ReportSymbol>())
            if (report.FindGroup(name) is { } g)
                return (report, g);
        return null;
    }

    /// <summary>Resolve a level-88 condition name.</summary>
    public ConditionSymbol? ResolveConditionName(string name)
        => Symbols.Program.DataDivisionScope.Resolve<ConditionSymbol>(name);

    /// <summary>Resolve a qualified level-88 condition name using qualification chain.</summary>
    public ConditionSymbol? ResolveQualifiedConditionName(string name, IReadOnlyList<string> qualifiers)
    {
        // Collect all condition symbols with this name (scope entry + rejections)
        var candidates = new List<ConditionSymbol>();
        var scope = Symbols.Program.DataDivisionScope;

        if (scope.Resolve<ConditionSymbol>(name) is { } primary)
            candidates.Add(primary);

        foreach (var (rejected, _) in scope.Rejections)
        {
            if (rejected is ConditionSymbol cs
                && string.Equals(cs.Name, name, StringComparison.OrdinalIgnoreCase))
                candidates.Add(cs);
        }

        if (candidates.Count == 0)
            return null;
        if (candidates.Count == 1)
            return candidates[0];

        // Multiple candidates — use qualifiers to disambiguate
        foreach (var cond in candidates)
        {
            if (MatchesQualification(cond.ParentDataItem, qualifiers))
                return cond;
        }
        return candidates[0]; // fallback to first if none match
    }

    private static bool MatchesQualification(DataSymbol? sym, IReadOnlyList<string> qualifiers)
    {
        // Walk up the parent chain checking each qualifier matches
        var current = sym;
        foreach (var qual in qualifiers)
        {
            current = FindAncestorByName(current, qual);
            if (current == null)
                return false;
        }
        return true;
    }

    private static DataSymbol? FindAncestorByName(DataSymbol? sym, string name)
    {
        // Include the symbol itself — for condition names, the first qualifier
        // typically names the parent data item directly.
        var current = sym;
        while (current != null)
        {
            if (string.Equals(current.DisplayName, name, StringComparison.OrdinalIgnoreCase))
                return current;
            current = current.Parent;
        }
        return null;
    }

    /// <summary>Find the FileSymbol whose FD record matches the given DataSymbol.</summary>
    public FileSymbol? ResolveFileForRecord(DataSymbol record)
    {
        // Every FILE SECTION 01 record carries its owning FD (not just the FD's first record), so a
        // WRITE/REWRITE of a secondary record (e.g. the long alternative of a RECORD VARYING file)
        // resolves to its file rather than falling back to a no-op.
        if (record.OwningFile != null)
            return record.OwningFile;
        foreach (var sym in Symbols.Program.GlobalScope.GetAllSymbols<FileSymbol>())
        {
            if (sym.Record == record)
                return sym;
        }
        return null;
    }

    /// <summary>
    /// True when a SEQUENTIAL file (record sequential or line sequential) has variable-length records —
    /// either an explicit RECORD IS VARYING clause, or two or more 01 record descriptions of differing
    /// storage sizes (implicitly variable, ISO §13.18.43). Record-sequential variable-length records are
    /// stored length-framed; fixed-length records are stored contiguous fixed-size. The single source of
    /// truth for this decision, shared by Binder (handler registration) and FileIoLowerer (WRITE/READ
    /// lowering) so they cannot disagree.
    /// </summary>
    public bool IsVariableLengthSequential(FileSymbol file)
    {
        if (file.Organization is not (null or "SEQUENTIAL" or "LINE SEQUENTIAL")) return false;
        return IsVariableLengthRecord(file);
    }

    /// <summary>True if the file has variable-length records by EITHER an explicit RECORD IS VARYING /
    /// Format-2 RECORD CONTAINS m TO n clause (<see cref="FileSymbol.IsRecordVarying"/>) OR multiple
    /// differently-sized 01 record descriptions. ORGANIZATION-INDEPENDENT — unlike
    /// <see cref="IsVariableLengthSequential"/>, which additionally requires a sequential organization
    /// (that gate is specific to the length-framed *sequential* on-disk storage decision; the
    /// RELATIVE/INDEXED handlers frame per-slot and so need the org-agnostic test).</summary>
    public bool IsVariableLengthRecord(FileSymbol file)
        => file.IsRecordVarying || HasMultipleRecordSizes(file);

    /// <summary>The MAXIMUM record length in bytes for a file: the larger of
    /// <see cref="FileSymbol.RecordVaryingMax"/> and every level-1 record declared under the FD. The
    /// record area and every runtime file handler's slot/buffer size MUST use this maximum (ISO §13.18.43:
    /// for Format-2 RECORD CONTAINS integer-2 TO integer-3, integer-3 is the maximum number of bytes in any
    /// record), NEVER the first/minimum 01 — otherwise LONG records are truncated to the first record's
    /// (possibly minimum) size (the RL106A bug: RL-FR6's 56-byte 6A precedes its 102-byte 6B).</summary>
    public int MaxRecordLength(FileSymbol file)
    {
        int max = file.RecordVaryingMax;
        foreach (var d in _dataItemsInOrder)
        {
            if (d.LevelNumber != 1 || !ReferenceEquals(d.OwningFile, file)) continue;
            int len = GetStorageLocation(d)?.Length ?? 0;
            if (len > max) max = len;
        }
        return max;
    }

    /// <summary>True if the FD declares record descriptions of differing sizes (multiple 01 records under one
    /// FD with different lengths) — an implicit variable-length record (ISO §13.18.43). Org-independent.</summary>
    public bool HasMultipleRecordSizes(FileSymbol file)
    {
        int firstLen = -1;
        foreach (var d in _dataItemsInOrder)
        {
            if (d.LevelNumber != 1 || !ReferenceEquals(d.OwningFile, file)) continue;
            int len = GetStorageLocation(d)?.Length ?? 0;
            if (firstLen < 0) firstLen = len;
            else if (len != firstLen) return true;
        }
        return false;
    }
}
