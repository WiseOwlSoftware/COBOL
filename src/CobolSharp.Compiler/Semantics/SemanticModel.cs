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

    // ── Data items in declaration order (all levels, preserves FILLERs) ──

    private IReadOnlyList<DataSymbol> _dataItemsInOrder = Array.Empty<DataSymbol>();
    public IReadOnlyList<DataSymbol> DataItemsInOrder => _dataItemsInOrder;

    public void SetDataItemsInOrder(IReadOnlyList<DataSymbol> items)
        => _dataItemsInOrder = items;

    // ── Data records (01/77-level items) ──

    private readonly List<DataSymbol> _dataRecords = new();
    public IReadOnlyList<DataSymbol> DataRecords => _dataRecords;

    // ── Procedure structure (declaration order) ──

    private readonly List<ParagraphSymbol> _paragraphsInOrder = new();
    public IReadOnlyList<ParagraphSymbol> ParagraphsInOrder => _paragraphsInOrder;

    private readonly List<SectionSymbol> _sectionsInOrder = new();
    public IReadOnlyList<SectionSymbol> SectionsInOrder => _sectionsInOrder;

    // ── PIC descriptors per data symbol ──

    private readonly Dictionary<DataSymbol, PicDescriptor> _picDescriptors = new();

    // ── Storage sizes (set by ComputeStorageLayout) ──

    public int WorkingStorageSize { get; set; }
    public int FileSectionSize { get; set; }

    // ── Storage locations per data symbol (set by ComputeStorageLayout) ──

    private readonly Dictionary<DataSymbol, CodeGen.StorageLocation> _storageLocations = new();

    // ── Initial VALUE clauses (typed) ──

    public sealed record InitialValue(object Value, Bound.CobolType Type);

    private readonly Dictionary<DataSymbol, InitialValue> _initialValues = new();

    public void RegisterInitialValue(DataSymbol symbol, object value, Bound.CobolType type)
        => _initialValues[symbol] = new InitialValue(value, type);

    public IReadOnlyDictionary<DataSymbol, InitialValue> InitialValues => _initialValues;

    // ── Parse node → symbol mapping (for binder lookups) ──

    private readonly Dictionary<object, Symbol> _nodeToSymbol = new();

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

    public void RegisterPicDescriptor(DataSymbol symbol, PicDescriptor pic)
        => _picDescriptors[symbol] = pic;

    public void RegisterStorageLocation(DataSymbol symbol, StorageLocation loc)
        => _storageLocations[symbol] = loc;

    public void RegisterNodeSymbol(object parseNode, Symbol symbol)
        => _nodeToSymbol[parseNode] = symbol;

    // ── Queries (consumed by binder) ──

    public PicDescriptor? GetPicDescriptor(DataSymbol symbol)
        => _picDescriptors.TryGetValue(symbol, out var pic) ? pic : null;

    public StorageLocation? GetStorageLocation(DataSymbol symbol)
        => _storageLocations.TryGetValue(symbol, out var loc) ? loc : null;

    public Symbol? GetSymbol(object parseNode)
        => _nodeToSymbol.TryGetValue(parseNode, out var s) ? s : null;

    /// <summary>Resolve a data item name.</summary>
    public DataSymbol? ResolveData(string name)
        => Symbols.Program.DataDivisionScope.Resolve<DataSymbol>(name);

    /// <summary>Resolve a paragraph name.</summary>
    public ParagraphSymbol? ResolveParagraph(string name)
        => Symbols.Program.ProcedureDivisionScope.Resolve<ParagraphSymbol>(name);

    /// <summary>Resolve a section name.</summary>
    public SectionSymbol? ResolveSection(string name)
        => Symbols.Program.ProcedureDivisionScope.Resolve<SectionSymbol>(name);

    /// <summary>Resolve a file name.</summary>
    public FileSymbol? ResolveFile(string name)
        => Symbols.Program.GlobalScope.Resolve<FileSymbol>(name);
}
