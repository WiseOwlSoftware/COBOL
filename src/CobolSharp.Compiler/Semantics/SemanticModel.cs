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
    /// Resolve a START / READ KEY operand to its key of reference: -1 for the prime record key, 0+ for an
    /// alternate record key index, or null if the operand is neither a key nor a leftmost generic-key prefix.
    /// A generic key (ISO §14.9.41) is a data item that begins at a key's leftmost byte and is no longer than
    /// that key — it names the leftmost portion of the key to position on (a partial-key START). Storage
    /// offsets are available because this runs after <see cref="StorageLayoutComputer"/>.
    /// </summary>
    public int? ResolveKeyOfReference(FileSymbol file, DataSymbol operand)
    {
        if (file.RecordKey != null &&
            string.Equals(operand.Name, file.RecordKey, StringComparison.OrdinalIgnoreCase))
            return -1;
        for (int i = 0; i < file.AlternateKeys.Count; i++)
            if (string.Equals(operand.Name, file.AlternateKeys[i].DataName, StringComparison.OrdinalIgnoreCase))
                return i;

        // Generic key: a leftmost prefix of one of the keys (same start offset, length not greater).
        var opLoc = GetStorageLocation(operand);
        if (opLoc == null) return null;
        if (IsLeftmostPrefix(opLoc.Value, file.RecordKey)) return -1;
        for (int i = 0; i < file.AlternateKeys.Count; i++)
            if (IsLeftmostPrefix(opLoc.Value, file.AlternateKeys[i].DataName)) return i;
        return null;
    }

    private bool IsLeftmostPrefix(CodeGen.StorageLocation opLoc, string? keyName)
    {
        if (keyName == null) return false;
        var keySym = ResolveData(keyName);
        var keyLoc = keySym != null ? GetStorageLocation(keySym) : null;
        return keyLoc != null && opLoc.Offset == keyLoc.Value.Offset && opLoc.Length <= keyLoc.Value.Length;
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
        if (file.IsRecordVarying) return true;
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
