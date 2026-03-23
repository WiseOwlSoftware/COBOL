// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Runtime;
using Antlr4.Runtime;
using CobolSharp.Compiler.CodeGen;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Generated;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Pass 1: Walk the ANTLR parse tree and collect all symbol declarations.
/// Builds a proper parent/child DataSymbol tree using level numbers.
/// </summary>
public sealed class SemanticBuilder : CobolParserCoreBaseVisitor<object?>
{
    private readonly SymbolTable _symbols;
    private readonly DiagnosticBag _diagnostics = new();
    private int _fillerCounter;

    // Data items in declaration order (preserves all FILLERs)
    private readonly List<DataSymbol> _dataItemsInOrder = [];

    // Current section name (null if paragraphs are orphans)
    private string? _currentSectionName;

    // Section → paragraph membership (built during visit)
    private readonly Dictionary<string, List<string>> _sectionParagraphs =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Section-to-paragraph membership map built during parsing.</summary>
    public IReadOnlyDictionary<string, List<string>> SectionParagraphs => _sectionParagraphs;

    // Parent stack for level-number-based tree building
    private readonly Stack<DataSymbol> _dataStack = [];

    // Tracks which data division section we're currently visiting
    private StorageAreaKind _currentArea = StorageAreaKind.WorkingStorage;

    // Temporary: holds the REDEFINES target name during clause parsing
    private string? _deferredRedefinesName;
    private Runtime.SignStorageKind? _deferredSignStorage;
    private FigurativeKind? _deferredFigurativeInit;

    // Extension clauses captured during parsing (vendor extensions, unrecognized clauses)
    private readonly List<ExtensionClauseNode> _extensionClauses = [];
    public IReadOnlyList<ExtensionClauseNode> ExtensionClauses => _extensionClauses;

    private void CaptureGenericClause(CobolParserCore.GenericClauseContext? ctx, GenericClauseContext context)
    {
        if (ctx == null) return;
        _extensionClauses.Add(ExtensionClauseNode.FromParseTree(ctx, context, "<source>"));
    }

    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics.Diagnostics;
    public SymbolTable Symbols => _symbols;
    public IReadOnlyList<DataSymbol> DataItemsInOrder => _dataItemsInOrder;

    public SemanticBuilder(string programName, int line)
    {
        _symbols = new SymbolTable(programName, line);
    }

    /// <summary>
    /// Pass 2: Resolve all deferred REDEFINES references now that all data items
    /// are registered in the symbol table.
    /// </summary>
    public void ResolveRedefines()
    {
        foreach (var data in _dataItemsInOrder)
        {
            if (data.RedefinesName == null) continue;
            var target = _symbols.Program.DataDivisionScope.Resolve<DataSymbol>(data.RedefinesName);
            if (target != null)
                data.Redefines = target;
            // else: target not found — silently ignore for now (may be in COPY not yet expanded)
        }
    }

    /// <summary>
    /// Pass 2b: Resolve all deferred RENAMES FROM/THRU references now that all data items
    /// are registered in the symbol table.
    /// </summary>
    public void ResolveRenames()
    {
        foreach (var data in _dataItemsInOrder)
        {
            if (data.Renames == null) continue;
            var from = _symbols.Program.DataDivisionScope.Resolve<DataSymbol>(data.Renames.FromName);
            if (from != null)
            {
                if (from.LevelNumber is 66 or 88)
                    RenamesError(data, DiagnosticDescriptors.CBL0812, from.DisplayName);
                else
                    data.Renames.FromSymbol = from;
            }
            else
            {
                RenamesError(data, DiagnosticDescriptors.CBL0810, data.Renames.FromName, data.DisplayName);
            }

            if (data.Renames.ThruName != null)
            {
                var thru = _symbols.Program.DataDivisionScope.Resolve<DataSymbol>(data.Renames.ThruName);
                if (thru != null)
                {
                    if (thru.LevelNumber is 66 or 88)
                        RenamesError(data, DiagnosticDescriptors.CBL0812, thru.DisplayName);
                    else
                        data.Renames.ThruSymbol = thru;
                }
                else
                {
                    RenamesError(data, DiagnosticDescriptors.CBL0811, data.Renames.ThruName, data.DisplayName);
                }
            }
        }
    }

    private void RenamesError(DataSymbol data, DiagnosticDescriptor desc, params object[] args)
    {
        _diagnostics.Report(desc,
            new Common.SourceLocation("<source>", 0, data.Line, 0),
            new Common.TextSpan(0, 0), args);
    }

    /// <summary>
    /// Pass 3: Propagate group-level SIGN clauses to elementary children.
    /// Per ISO, a SIGN clause on a group item applies to all subordinate
    /// elementary numeric DISPLAY items that don't have their own SIGN clause.
    /// </summary>
    public void PropagateGroupSignClauses()
    {
        foreach (var root in _dataItemsInOrder)
        {
            if (root.LevelNumber == 1 || root.LevelNumber == 77)
                PropagateSignRecursive(root, inherited: null);
        }
    }

    private void PropagateSignRecursive(DataSymbol sym, Runtime.SignStorageKind? inherited)
    {
        var effective = sym.ExplicitSignStorage ?? inherited;

        if (sym.IsElementary &&
            sym.ResolvedType?.Pic is PicLayout pic &&
            pic.IsSigned &&
            sym.Usage == Runtime.UsageKind.Display &&
            sym.ExplicitSignStorage == null)
        {
            sym.ExplicitSignStorage = effective;
        }

        foreach (var child in sym.Children)
            PropagateSignRecursive(child, effective);
    }

    private void Error(ParserRuleContext ctx, string message)
    {
        _diagnostics.ReportError("SEM", message,
            new Common.SourceLocation("<source>", 0, ctx.Start.Line, ctx.Start.Column),
            new Common.TextSpan(ctx.Start.StartIndex, ctx.Stop?.StopIndex ?? ctx.Start.StopIndex));
    }

    // ── SPECIAL-NAMES ──

    private char _currencySign = '$';
    private bool _decimalPointIsComma = false;
    private readonly List<ImplementorSwitch> _implementorSwitches = [];

    public char CurrencySign => _currencySign;
    public bool DecimalPointIsComma => _decimalPointIsComma;
    public IReadOnlyList<ImplementorSwitch> ImplementorSwitches => _implementorSwitches;

    public override object? VisitSpecialNamesParagraph(CobolParserCore.SpecialNamesParagraphContext ctx)
    {
        foreach (var entry in ctx.specialNameEntry())
        {
            // CURRENCY SIGN IS "W"
            if (entry.currencySignClause() is { } currClause)
            {
                var lit = currClause.literal();
                var nonNum = lit?.nonNumericLiteral();
                if (nonNum?.STRINGLIT() is { } slit)
                {
                    var text = slit.GetText();
                    if (text.Length >= 3)
                        _currencySign = text[1];
                }
            }

            // DECIMAL-POINT IS COMMA
            if (entry.decimalPointClause() is { } dpClause)
            {
                var word = dpClause.IDENTIFIER()?.GetText();
                if (string.Equals(word, "COMMA", StringComparison.OrdinalIgnoreCase))
                    _decimalPointIsComma = true;
            }

            // Implementor switch: IDENTIFIER IS IDENTIFIER (ON name)? (OFF IS? name)?
            if (entry.implementorSwitchEntry() is { } swClause)
            {
                var ids = swClause.IDENTIFIER();
                if (ids.Length >= 2)
                {
                    string implName = ids[0].GetText();
                    string mnemonicName = ids[1].GetText();
                    string? onName = null;
                    string? offName = null;

                    if (swClause.ON() != null && ids.Length >= 3)
                        onName = ids[2].GetText();

                    if (swClause.OFF() != null)
                    {
                        int idx = swClause.ON() != null ? 3 : 2;
                        if (ids.Length > idx)
                            offName = ids[idx].GetText();
                    }

                    _implementorSwitches.Add(
                        new ImplementorSwitch(mnemonicName, implName, onName, offName));
                }
            }

            // Capture generic/vendor extensions
            if (entry.genericClause() is { } genCtx)
                CaptureGenericClause(genCtx, GenericClauseContext.SpecialNames);
        }

        return base.VisitSpecialNamesParagraph(ctx);
    }

    // ── Data Division ──

    public override object? VisitWorkingStorageSection(CobolParserCore.WorkingStorageSectionContext ctx)
    {
        _dataStack.Clear();
        _currentArea = StorageAreaKind.WorkingStorage;
        return base.VisitWorkingStorageSection(ctx);
    }

    public override object? VisitLocalStorageSection(CobolParserCore.LocalStorageSectionContext ctx)
    {
        _dataStack.Clear();
        _currentArea = StorageAreaKind.LocalStorage;
        return base.VisitLocalStorageSection(ctx);
    }

    public override object? VisitLinkageSection(CobolParserCore.LinkageSectionContext ctx)
    {
        _dataStack.Clear();
        _currentArea = StorageAreaKind.LinkageSection;
        return base.VisitLinkageSection(ctx);
    }

    // ═══════════════════════════════════
    // FILE-CONTROL — extract SELECT/ASSIGN/clauses into FileSymbol
    // ═══════════════════════════════════

    public override object? VisitFileControlClauseGroup(CobolParserCore.FileControlClauseGroupContext ctx)
    {
        var fileNameCtx = ctx.fileName();
        if (fileNameCtx == null) return base.VisitFileControlClauseGroup(ctx);

        string name = fileNameCtx.GetText();
        var fileSym = new FileSymbol(name, fileNameCtx.Start.Line);

        // ASSIGN TO
        var assignCtx = ctx.assignTarget();
        if (assignCtx != null)
        {
            string assignText = assignCtx.GetText();
            // String literal → explicit host path; identifier → implementor-defined
            if (assignText.Length >= 2 &&
                (assignText[0] == '"' || assignText[0] == '\''))
            {
                fileSym.AssignTarget = assignText[1..^1];
                fileSym.AssignIsLiteral = true;
            }
            else
            {
                fileSym.AssignTarget = assignText;
                fileSym.AssignIsLiteral = false;
            }
        }

        // Clauses
        foreach (var clause in ctx.fileControlClauses())
        {
            if (clause.organizationClause() is { } orgClause)
            {
                var orgType = orgClause.organizationType();
                // LINE SEQUENTIAL is two tokens; GetText() gives "LINESEQUENTIAL"
                if (orgType.LINE() != null)
                    fileSym.Organization = "LINE SEQUENTIAL";
                else
                    fileSym.Organization = orgType.GetText().ToUpperInvariant();
            }
            if (clause.accessModeClause() is { } accessClause)
                fileSym.AccessMode = accessClause.accessMode().GetText().ToUpperInvariant();
            if (clause.recordKeyClause() is { } keyClause)
                fileSym.RecordKey = keyClause.dataReference().GetText();
            if (clause.fileStatusClause() is { } statusClause)
                fileSym.FileStatus = statusClause.dataReference().GetText();
        }

        _symbols.Program.GlobalScope.TryDeclare(fileSym, out _);
        return base.VisitFileControlClauseGroup(ctx);
    }

    // ═══════════════════════════════════
    // FILE SECTION / FD
    // ═══════════════════════════════════

    public override object? VisitFileSection(CobolParserCore.FileSectionContext ctx)
    {
        _dataStack.Clear();
        _currentArea = StorageAreaKind.FileSection;
        return base.VisitFileSection(ctx);
    }

    /// <summary>Current FD file symbol — set during FD visiting so 01-level records can be linked.</summary>
    private FileSymbol? _currentFdFile;

    public override object? VisitFileDescriptionEntry(CobolParserCore.FileDescriptionEntryContext ctx)
    {
        var nameCtx = ctx.fileName();
        if (nameCtx != null)
        {
            string name = nameCtx.GetText();
            // Look up existing FileSymbol created by FILE-CONTROL visitor.
            // If not found (no SELECT), create one as a fallback with a warning.
            var fileSym = _symbols.Program.GlobalScope.Resolve<FileSymbol>(name);
            if (fileSym == null)
            {
                fileSym = new FileSymbol(name, nameCtx.Start.Line);
                _symbols.Program.GlobalScope.TryDeclare(fileSym, out _);
                _diagnostics.Report(DiagnosticDescriptors.CBL0601,
                    new Common.SourceLocation("<source>", 0, nameCtx.Start.Line, nameCtx.Start.Column),
                    new Common.TextSpan(nameCtx.Start.StartIndex, nameCtx.Stop?.StopIndex ?? nameCtx.Start.StopIndex),
                    name);
            }
            _currentFdFile = fileSym;
        }
        _dataStack.Clear();
        var result = base.VisitFileDescriptionEntry(ctx);
        _currentFdFile = null;
        return result;
    }

    public override object? VisitDataDescriptionEntry(CobolParserCore.DataDescriptionEntryContext ctx)
    {
        var levelCtx = ctx.levelNumber();
        var nameCtx = ctx.dataName();
        if (levelCtx == null) return base.VisitDataDescriptionEntry(ctx);

        int level = int.Parse(levelCtx.GetText());
        string displayName = nameCtx?.GetText() ?? "FILLER";
        int line = levelCtx.Start.Line;

        // Make FILLER unique for symbol table
        bool isFiller = string.Equals(displayName, "FILLER", StringComparison.OrdinalIgnoreCase);
        string internalName = isFiller ? $"FILLER${_fillerCounter++}" : displayName;

        // Extract PIC, USAGE, VALUE from clauses
        string? picString = null;
        var usage = UsageKind.Display;
        bool hasExplicitUsage = false;
        string? typeName = null;
        string? initialValue = null;
        var body = ctx.dataDescriptionBody();
        if (body?.dataDescriptionClauses() != null)
        {
            foreach (var clause in body.dataDescriptionClauses().dataDescriptionClause())
            {
                var picClause = clause.pictureClause();
                if (picClause != null)
                    picString = picClause.PIC_STRING()?.GetText()?.Trim();

                var usageClause = clause.usageClause();
                if (usageClause != null)
                {
                    // Full form: USAGE IS? usageKeyword → use usageKeyword text
                    // Bare form: COMP, BINARY, etc. → use the clause's text directly
                    var kwText = usageClause.usageKeyword()?.GetText()
                        ?? usageClause.GetText();
                    usage = UsageMapper.FromUsageKeyword(kwText);
                    hasExplicitUsage = true;
                }

                var redefinesClause = clause.redefinesClause();
                if (redefinesClause != null)
                {
                    // Store the unresolved name; actual resolution happens in pass 2
                    // after all data items are registered in the symbol table
                    _deferredRedefinesName = redefinesClause.dataReference()?.GetText() ?? "";
                }

                var typeClause = clause.typeClause();
                if (typeClause != null)
                    typeName = typeClause.IDENTIFIER()?.GetText();

                var signCl = clause.signClause();
                if (signCl != null)
                {
                    bool isLeading = signCl.LEADING() != null;
                    bool isSeparate = signCl.SEPARATE() != null;
                    _deferredSignStorage = (isLeading, isSeparate) switch
                    {
                        (true, true) => Runtime.SignStorageKind.LeadingSeparate,
                        (true, false) => Runtime.SignStorageKind.LeadingOverpunch,
                        (false, true) => Runtime.SignStorageKind.TrailingSeparate,
                        (false, false) => Runtime.SignStorageKind.TrailingOverpunch,
                    };
                }

                // VALUE clause — extract the first literal from the first valueItem
                var valClause = clause.valueClause();
                if (valClause != null)
                {
                    var items = valClause.valueItem();
                    var voCtx = items.Length > 0
                        ? (items[0].valueRange()?.valueOperand(0) ?? items[0].valueOperand(0))
                        : null;
                    if (voCtx != null)
                    {
                        // valueOperand: arithmeticExpression | nonNumericLiteral
                        // For numeric VALUE, find numericLiteral and detect unary minus
                        var arithCtx = voCtx.arithmeticExpression();
                        var (numLit, isNegated) = FindNumericLiteralInArith(arithCtx);
                        if (numLit != null)
                        {
                            var text = NormalizeNumericLiteralText(numLit);
                            initialValue = isNegated ? "-" + text : text;
                        }
                        else
                        {
                            var nonNum = voCtx.nonNumericLiteral();
                            if (nonNum?.STRINGLIT() is { } slit)
                            {
                                var text = slit.GetText();
                                if (text.Length >= 2)
                                {
                                    char q = text[0];
                                    initialValue = text[1..^1].Replace(new string(q, 2), new string(q, 1));
                                }
                            }
                            else if (nonNum?.figurativeConstant() is { } fig)
                            {
                                string figText = fig.GetText().ToUpperInvariant();
                                // For ZERO, keep as "0" so numeric VALUE parsing still works
                                initialValue = figText switch
                                {
                                    "SPACE" or "SPACES" => " ",
                                    "ZERO" or "ZEROS" or "ZEROES" => "0",
                                    "HIGH-VALUE" or "HIGH-VALUES" => null,
                                    "LOW-VALUE" or "LOW-VALUES" => null,
                                    "QUOTE" or "QUOTES" => null,
                                    _ => figText
                                };
                                // Store figurative kind for field-filling initialization
                                _deferredFigurativeInit = figText switch
                                {
                                    "SPACE" or "SPACES" => FigurativeKind.Space,
                                    "ZERO" or "ZEROS" or "ZEROES" => FigurativeKind.Zero,
                                    "HIGH-VALUE" or "HIGH-VALUES" => FigurativeKind.HighValue,
                                    "LOW-VALUE" or "LOW-VALUES" => FigurativeKind.LowValue,
                                    "QUOTE" or "QUOTES" => FigurativeKind.Quote,
                                    _ => null
                                };
                            }
                        }
                    }
                }
            }
        }

        // Level-88 condition names: attach to parent data item, extract VALUE clauses
        if (level == 88)
        {
            // Parent is the most recent data item on the stack
            DataSymbol? parent = _dataStack.Count > 0 ? _dataStack.Peek() : null;
            if (parent == null)
                throw new InvalidOperationException(
                    $"Level-88 condition name '{displayName}' has no parent data item (line {line})");

            var condSym = new ConditionSymbol(displayName, parent, line);

            // Extract values from VALUE clause — now unified with THRU support via valueItem
            var condBody = ctx.dataDescriptionBody();
            if (condBody?.dataDescriptionClauses() != null)
            {
                foreach (var clause in condBody.dataDescriptionClauses().dataDescriptionClause())
                {
                    var valClause = clause.valueClause();
                    if (valClause != null)
                    {
                        foreach (var item in valClause.valueItem())
                        {
                            var rangeCtx = item.valueRange();
                            if (rangeCtx != null)
                            {
                                var fromVal = ConditionValue.FromObject(ParseConditionValueOperand(rangeCtx.valueOperand(0)));
                                var toVal = ConditionValue.FromObject(ParseConditionValueOperand(rangeCtx.valueOperand(1)));
                                condSym.AddRange(fromVal, toVal);
                            }
                            else
                            {
                                foreach (var vo in item.valueOperand())
                                {
                                    var fromVal = ConditionValue.FromObject(ParseConditionValueOperand(vo));
                                    condSym.AddRange(fromVal, null);
                                }
                            }
                        }
                    }
                }
            }

            _symbols.Program.DataDivisionScope.TryDeclare(condSym, out _);
            return null;
        }

        // Extract OCCURS clause, BLANK WHEN ZERO, JUSTIFIED from clauses
        OccursInfo? occursInfo = null;
        bool blankWhenZero = false;
        bool justifiedRight = false;
        if (body?.dataDescriptionClauses() != null)
        {
            foreach (var clause in body.dataDescriptionClauses().dataDescriptionClause())
            {
                var occClause = clause.occursClause();
                if (occClause != null)
                {
                    var intLits = occClause.integerLiteral();
                    int maxOccurs = 1;
                    int minOccurs = 0;
                    string? dependingOn = null;

                    if (intLits.Length > 0 && int.TryParse(intLits[0].GetText(), out int oc))
                        maxOccurs = oc;

                    // OCCURS m TO n DEPENDING ON: first int = min, second int = max
                    if (intLits.Length > 1 && int.TryParse(intLits[1].GetText(), out int oc2))
                    {
                        minOccurs = maxOccurs;
                        maxOccurs = oc2;
                    }
                    else
                    {
                        minOccurs = maxOccurs;
                    }

                    // DEPENDING ON data-name
                    var depRef = occClause.dataReference();
                    if (depRef != null)
                        dependingOn = depRef.GetText();

                    // ASCENDING/DESCENDING KEY data-names
                    var ascKeys = new List<string>();
                    var descKeys = new List<string>();
                    foreach (var keyClause in occClause.occursKeyClause())
                    {
                        bool isAscending = keyClause.ASCENDING() != null;
                        foreach (var keyRef in keyClause.dataReference())
                        {
                            string keyName = keyRef.GetText();
                            if (isAscending)
                                ascKeys.Add(keyName);
                            else
                                descKeys.Add(keyName);
                        }
                    }

                    // INDEXED BY names (captured here; index DataSymbols created below)
                    var indexNames = new List<string>();
                    if (occClause.dataReferenceList() is { } indexList)
                    {
                        foreach (var idCtx in indexList.dataReference())
                            indexNames.Add(idCtx.GetText());
                    }

                    occursInfo = new OccursInfo(
                        minOccurs, maxOccurs, dependingOn,
                        ascKeys.Count > 0 ? ascKeys : null,
                        descKeys.Count > 0 ? descKeys : null,
                        indexNames.Count > 0 ? indexNames : null);
                }

                if (clause.blankWhenZeroClause() != null)
                    blankWhenZero = true;

                if (clause.justifiedClause() != null)
                    justifiedRight = true;
            }
        }

        // Level-77 USAGE INDEX with no PIC: normalize to S9(9) COMP here (always elementary).
        // Sub-level USAGE INDEX items are handled at layout time by FieldSizeCalculator +
        // CompilerPicDescriptorFactory, since we don't know here whether they'll be groups.
        if (usage == Runtime.UsageKind.Index && picString == null && level == 77)
        {
            picString = "S9(9)";
            usage = Runtime.UsageKind.Comp;
        }

        // Create DataSymbol (REDEFINES resolved in pass 2 after all items registered)
        var data = new DataSymbol(internalName, displayName, level, picString, usage, typeName, redefines: null, line);
        data.HasExplicitUsage = hasExplicitUsage;
        data.Occurs = occursInfo;
        data.IsJustifiedRight = justifiedRight;
        data.RedefinesName = _deferredRedefinesName;
        _deferredRedefinesName = null;
        data.ExplicitSignStorage = _deferredSignStorage;
        _deferredSignStorage = null;
        data.FigurativeInit = _deferredFigurativeInit;
        _deferredFigurativeInit = null;

        // Resolve PIC/USAGE → ITypeSymbol
        var diagBag = new DiagnosticBag();
        var picEnv = new Runtime.PicEnvironment(_currencySign, _decimalPointIsComma);
        data.ResolvedType = PicUsageResolver.ResolveForDataItem(
            displayName, picString, usage, diagBag, line, blankWhenZero, picEnv);
        foreach (var d in diagBag.Diagnostics)
            _diagnostics.Add(d);

        data.InitialValue = initialValue;
        data.Area = _currentArea;

        // Build parent/child tree using level numbers
        if (level == 1 || level == 77)
        {
            // Root-level item
            _dataStack.Clear();
            _dataStack.Push(data);

            // Link 01-level record to FD file symbol (first 01 under an FD)
            if (_currentFdFile != null && _currentFdFile.Record == null && level == 1)
            {
                _currentFdFile.Record = data;
            }
        }
        else if (level == 66)
        {
            // RENAMES — extract FROM/THRU targets from renamesClause
            _dataStack.Clear();
            var renamesCtx = body?.renamesClause();
            if (renamesCtx != null)
            {
                var refs = renamesCtx.dataReference();
                string fromName = refs.Length > 0 ? refs[0].IDENTIFIER()?.GetText() ?? "" : "";
                string? thruName = refs.Length > 1 ? refs[1].IDENTIFIER()?.GetText() : null;
                data.Renames = new RenamesInfo(fromName, thruName);
            }
        }
        else
        {
            // Subordinate: pop stack until we find a parent with lower level
            while (_dataStack.Count > 0 && _dataStack.Peek().LevelNumber >= level)
                _dataStack.Pop();

            if (_dataStack.Count > 0)
                _dataStack.Peek().AddChild(data);

            _dataStack.Push(data);
        }

        // Add to declaration-order list (preserves ALL items including FILLERs)
        _dataItemsInOrder.Add(data);

        // Declare in scope (for name resolution)
        _symbols.Program.DataDivisionScope.TryDeclare(data, out _);

        // Declare INDEXED BY index-names from OCCURS clause.
        // INDEX items are implicit level-77 items stored as PIC S9(9) COMP
        // holding 1-based element numbers.
        if (body?.dataDescriptionClauses() != null)
        {
            foreach (var clause in body.dataDescriptionClauses().dataDescriptionClause())
            {
                var occClause = clause.occursClause();
                if (occClause?.dataReferenceList() is { } indexList)
                {
                    foreach (var idCtx in indexList.dataReference())
                    {
                        string indexName = idCtx.IDENTIFIER().GetText();
                        var indexSym = new DataSymbol(indexName, indexName, 77,
                            "S9(9)", Runtime.UsageKind.Comp, null, null, ctx.Start.Line);
                        indexSym.HasExplicitUsage = true;
                        indexSym.Area = _currentArea;
                        // Resolve PIC for the INDEX item so it gets proper storage layout
                        var idxDiagBag = new DiagnosticBag();
                        var idxPicEnv = new Runtime.PicEnvironment(_currencySign, _decimalPointIsComma);
                        indexSym.ResolvedType = PicUsageResolver.ResolveForDataItem(
                            indexName, "S9(9)", Runtime.UsageKind.Comp, idxDiagBag, ctx.Start.Line, false, idxPicEnv);
                        _dataItemsInOrder.Add(indexSym);
                        _symbols.Program.DataDivisionScope.TryDeclare(indexSym, out _);
                    }
                }
            }
        }

        return null;
    }

    // ── Procedure Division ──

    public override object? VisitProcedureDivision(CobolParserCore.ProcedureDivisionContext ctx)
    {
        using var _ = _symbols.PushScope(_symbols.Program.ProcedureDivisionScope);
        return base.VisitProcedureDivision(ctx);
    }

    public override object? VisitSectionDefinition(CobolParserCore.SectionDefinitionContext ctx)
    {
        var nameCtx = ctx.sectionName();
        if (nameCtx == null) return base.VisitSectionDefinition(ctx);

        string name = nameCtx.GetText();
        var section = new SectionSymbol(name,
            _symbols.Program.ProcedureDivisionScope, ctx.Start.Line);

        _symbols.Program.ProcedureDivisionScope.TryDeclare(section, out _);

        _currentSectionName = name;
        using var scopeGuard = _symbols.PushScope(section.Scope);
        foreach (var para in ctx.paragraphDefinition())
            VisitParagraphDefinition(para);
        _currentSectionName = null;

        return null;
    }

    public override object? VisitParagraphDefinition(CobolParserCore.ParagraphDefinitionContext ctx)
    {
        var nameCtx = ctx.paragraphName();
        if (nameCtx == null) return base.VisitParagraphDefinition(ctx);

        string name = nameCtx.GetText();
        var paragraph = new ParagraphSymbol(name, _symbols.CurrentScope, ctx.Start.Line);

        _symbols.CurrentScope.TryDeclare(paragraph, out _);
        // Also declare in ProcedureDivisionScope for global resolution,
        // but only if CurrentScope is a section scope (not already ProcedureDivisionScope)
        if (_symbols.CurrentScope != _symbols.Program.ProcedureDivisionScope)
            _symbols.Program.ProcedureDivisionScope.TryDeclare(paragraph, out _);

        // Track section membership
        if (_currentSectionName != null)
        {
            if (!_sectionParagraphs.TryGetValue(_currentSectionName, out var list))
            {
                list = new List<string>();
                _sectionParagraphs[_currentSectionName] = list;
            }
            list.Add(name);
        }

        using var paraScope = _symbols.PushScope(paragraph.Scope);
        foreach (var sentence in ctx.sentence())
            foreach (var stmt in sentence.statement())
                Visit(stmt);

        return null;
    }

    /// <summary>
    /// Parse a literal context into a typed value for level-88 condition entries.
    /// Returns decimal for numeric literals, string for string literals.
    /// </summary>
    private static object ParseConditionLiteralValue(Generated.CobolParserCore.LiteralContext lit)
    {
        var numLit = lit.numericLiteral();
        if (numLit != null)
        {
            var text = NormalizeNumericLiteralText(numLit);
            if (decimal.TryParse(text,
                System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowDecimalPoint,
                System.Globalization.CultureInfo.InvariantCulture, out var d))
                return d;
            return text;
        }
        var nonNum = lit.nonNumericLiteral();
        if (nonNum?.STRINGLIT() is { } slit)
        {
            var text = slit.GetText();
            if (text.Length >= 2)
            {
                char q = text[0];
                return text[1..^1].Replace(new string(q, 2), new string(q, 1));
            }
            return text;
        }
        return lit.GetText();
    }

    /// <summary>
    /// Parse a valueOperand context into a typed value for level-88 condition entries.
    /// Navigates through the arithmeticExpression chain to reach numericLiteral for numeric values.
    /// </summary>
    private static object ParseConditionValueOperand(Generated.CobolParserCore.ValueOperandContext vo)
    {
        var nonNum = vo.nonNumericLiteral();
        if (nonNum?.STRINGLIT() is { } slit)
        {
            var text = slit.GetText();
            if (text.Length >= 2)
            {
                char q = text[0];
                return text[1..^1].Replace(new string(q, 2), new string(q, 1));
            }
            return text;
        }
        if (nonNum?.figurativeConstant() != null)
            return nonNum.GetText();

        // Navigate arithmeticExpression chain to reach numericLiteral (handles unary minus)
        var (numLit, isNegated) = FindNumericLiteralInArith(vo.arithmeticExpression());
        if (numLit != null)
        {
            var text = NormalizeNumericLiteralText(numLit);
            if (isNegated) text = "-" + text;
            if (decimal.TryParse(text,
                System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowDecimalPoint,
                System.Globalization.CultureInfo.InvariantCulture, out var d))
                return d;
            return text;
        }
        return vo.GetText();
    }

    /// <summary>
    /// Navigate the arithmeticExpression chain to find a numericLiteral in a VALUE clause context.
    /// Returns the numericLiteral and whether a unary MINUS wraps it (for signed VALUE like -42).
    /// </summary>
    private static (Generated.CobolParserCore.NumericLiteralContext? numLit, bool isNegated)
        FindNumericLiteralInArith(Generated.CobolParserCore.ArithmeticExpressionContext? arith)
    {
        if (arith == null) return (null, false);
        var unary = arith.additiveExpression()?.multiplicativeExpression(0)
            ?.powerExpression(0)?.unaryExpression(0);
        if (unary == null) return (null, false);

        // Check for unary sign: addOp unaryExpression (PLUS or MINUS)
        bool negated = false;
        var addOp = unary.addOp();
        if (addOp != null)
        {
            if (addOp.MINUS() != null)
                negated = true;
            // Both +N and -N have a nested unaryExpression containing the literal
            unary = unary.unaryExpression();
        }
        var numLit = unary?.primaryExpression()?.numericLiteral();
        return (numLit, negated);
    }

    /// <summary>
    /// Normalize a numericLiteral parse tree into a string suitable for decimal.Parse
    /// with InvariantCulture. Replaces comma decimal points with dots.
    /// </summary>
    internal static string NormalizeNumericLiteralText(Generated.CobolParserCore.NumericLiteralContext numLit)
    {
        var signed = numLit.signedNumericLiteral();
        var core = signed.numericLiteralCore();
        var sign = signed.MINUS() != null ? "-" : "";

        // DECIMALLIT from the lexer (dot-based: 123.45 or .45)
        if (core.DECIMALLIT() != null)
            return sign + core.DECIMALLIT().GetText();

        // COMMA-based decimals (for DECIMAL-POINT IS COMMA)
        var integers = core.INTEGERLIT();
        if (core.COMMA() != null && integers.Length == 2)
            return sign + integers[0].GetText() + "." + integers[1].GetText();
        if (core.COMMA() != null && integers.Length == 1)
            return sign + "." + integers[0].GetText();

        // Plain integer
        return sign + integers[0].GetText();
    }

    // ═══════════════════════════════════
    // Generic clause capture (vendor extensions)
    // ═══════════════════════════════════

    public override object? VisitGenericIdentificationParagraph(
        CobolParserCore.GenericIdentificationParagraphContext ctx)
    {
        CaptureGenericClause(ctx.genericClause(), GenericClauseContext.IdentificationParagraph);
        return base.VisitGenericIdentificationParagraph(ctx);
    }

    public override object? VisitVendorConfigurationParagraph(
        CobolParserCore.VendorConfigurationParagraphContext ctx)
    {
        CaptureGenericClause(ctx.genericClause(), GenericClauseContext.ConfigurationVendor);
        return base.VisitVendorConfigurationParagraph(ctx);
    }

    public override object? VisitGenericFileDescriptionClause(
        CobolParserCore.GenericFileDescriptionClauseContext ctx)
    {
        CaptureGenericClause(ctx.genericClause(), GenericClauseContext.FileDescription);
        return base.VisitGenericFileDescriptionClause(ctx);
    }

    public override object? VisitGenericDataClause(
        CobolParserCore.GenericDataClauseContext ctx)
    {
        CaptureGenericClause(ctx.genericClause(), GenericClauseContext.DataDescription);
        return base.VisitGenericDataClause(ctx);
    }

    public override object? VisitGenericReportGroupClause(
        CobolParserCore.GenericReportGroupClauseContext ctx)
    {
        CaptureGenericClause(ctx.genericClause(), GenericClauseContext.ReportGroup);
        return base.VisitGenericReportGroupClause(ctx);
    }

    public override object? VisitVendorFileControlClause(
        CobolParserCore.VendorFileControlClauseContext ctx)
    {
        CaptureGenericClause(ctx.genericClause(), GenericClauseContext.FileControl);
        return base.VisitVendorFileControlClause(ctx);
    }

    public override object? VisitIoControlEntry(
        CobolParserCore.IoControlEntryContext ctx)
    {
        CaptureGenericClause(ctx.genericClause(), GenericClauseContext.IOControl);
        return base.VisitIoControlEntry(ctx);
    }
}
