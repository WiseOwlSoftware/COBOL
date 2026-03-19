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

    public void SetPicEnvironment(char currencySign, bool decimalPointIsComma)
        => PicEnvironment = new Runtime.PicEnvironment(currencySign, decimalPointIsComma);

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

    // ── PIC descriptors per data symbol ──

    private readonly Dictionary<DataSymbol, PicDescriptor> _picDescriptors = [];

    // ── Storage sizes (set by ComputeStorageLayout) ──

    public int WorkingStorageSize { get; set; }
    public int FileSectionSize { get; set; }

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

    /// <summary>Resolve a level-88 condition name.</summary>
    public ConditionSymbol? ResolveConditionName(string name)
        => Symbols.Program.DataDivisionScope.Resolve<ConditionSymbol>(name);

    /// <summary>Find the FileSymbol whose FD record matches the given DataSymbol.</summary>
    public FileSymbol? ResolveFileForRecord(DataSymbol record)
    {
        foreach (var sym in Symbols.Program.GlobalScope.GetAllSymbols<FileSymbol>())
        {
            if (sym.Record == record)
                return sym;
        }
        return null;
    }
}
