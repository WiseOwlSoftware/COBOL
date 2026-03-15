using Antlr4.Runtime;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Generated;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Pass 1: Walk the ANTLR parse tree and collect all symbol declarations.
/// Creates ProgramSymbol, DataSymbol, FileSymbol, SectionSymbol, ParagraphSymbol,
/// and ConditionSymbol entries in the symbol table.
/// </summary>
public sealed class SemanticBuilder : CobolParserCoreBaseVisitor<object?>
{
    private readonly SymbolTable _symbols;
    private readonly List<Diagnostic> _diagnostics = new();

    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;
    public SymbolTable Symbols => _symbols;

    public SemanticBuilder(string programName, int line)
    {
        _symbols = new SymbolTable(programName, line);
    }

    private void Error(ParserRuleContext ctx, string message)
    {
        _diagnostics.Add(new Diagnostic(
            "SEM",
            DiagnosticSeverity.Error,
            message,
            new Common.SourceLocation("<source>", 0, ctx.Start.Line, ctx.Start.Column),
            new Common.TextSpan(ctx.Start.StartIndex, ctx.Stop?.StopIndex ?? ctx.Start.StopIndex)));
    }

    // ── Data Division ──

    public override object? VisitWorkingStorageSection(CobolParserCore.WorkingStorageSectionContext ctx)
    {
        using var _ = _symbols.PushScope(
            new Scope(ScopeKind.WorkingStorage, _symbols.Program.DataDivisionScope));
        return base.VisitWorkingStorageSection(ctx);
    }

    public override object? VisitLocalStorageSection(CobolParserCore.LocalStorageSectionContext ctx)
    {
        using var _ = _symbols.PushScope(
            new Scope(ScopeKind.LocalStorage, _symbols.Program.DataDivisionScope));
        return base.VisitLocalStorageSection(ctx);
    }

    public override object? VisitLinkageSection(CobolParserCore.LinkageSectionContext ctx)
    {
        using var _ = _symbols.PushScope(
            new Scope(ScopeKind.Linkage, _symbols.Program.DataDivisionScope));
        return base.VisitLinkageSection(ctx);
    }

    public override object? VisitFileSection(CobolParserCore.FileSectionContext ctx)
    {
        using var _ = _symbols.PushScope(
            new Scope(ScopeKind.FileSection, _symbols.Program.DataDivisionScope));
        return base.VisitFileSection(ctx);
    }

    public override object? VisitFileDescriptionEntry(CobolParserCore.FileDescriptionEntryContext ctx)
    {
        var nameCtx = ctx.fileName();
        if (nameCtx != null)
        {
            var fileSym = new FileSymbol(nameCtx.GetText(), nameCtx.Start.Line);
            if (!_symbols.Program.GlobalScope.TryDeclare(fileSym, out _))
                Error(ctx, $"Duplicate file name '{fileSym.Name}'.");
        }
        return base.VisitFileDescriptionEntry(ctx);
    }

    public override object? VisitDataDescriptionEntry(CobolParserCore.DataDescriptionEntryContext ctx)
    {
        var levelCtx = ctx.levelNumber();
        var nameCtx = ctx.dataName();
        if (levelCtx == null) return base.VisitDataDescriptionEntry(ctx);

        int level = int.Parse(levelCtx.GetText());
        string name = nameCtx?.GetText() ?? "FILLER";
        int line = levelCtx.Start.Line;

        // Extract PIC and USAGE from dataDescriptionBody
        string? picString = null;
        var usage = UsageKind.Display;
        string? typeName = null;
        DataSymbol? redefines = null;

        var body = ctx.dataDescriptionBody();
        if (body?.dataDescriptionClauses() != null)
        {
            foreach (var clause in body.dataDescriptionClauses().dataDescriptionClause())
            {
                var picClause = clause.pictureClause();
                if (picClause != null)
                {
                    var picStr = picClause.PIC_STRING();
                    picString = picStr?.GetText()?.Trim();
                }

                var usageClause = clause.usageClause();
                if (usageClause != null)
                {
                    usage = ExtractUsage(usageClause);
                }

                var redefinesClause = clause.redefinesClause();
                if (redefinesClause != null)
                {
                    string redefinesName = redefinesClause.identifier()?.GetText() ?? "";
                    redefines = _symbols.Resolve<DataSymbol>(redefinesName);
                }

                var typeClause = clause.typeClause();
                if (typeClause != null)
                {
                    typeName = typeClause.IDENTIFIER()?.GetText();
                }
            }

            // Handle 88-level condition entries
            var condEntry = body.conditionEntry88();
            if (condEntry != null && level == 88)
            {
                // 88-levels are handled below
            }
        }

        if (level == 88)
        {
            // Condition name — parent is the previously declared data item
            // For now, declare in data division scope
            var condSym = new ConditionSymbol(name, null!, line);
            _symbols.Program.DataDivisionScope.TryDeclare(condSym, out _);
            return null;
        }

        var data = new DataSymbol(name, level, picString, usage, typeName, redefines, line);

        // Resolve PIC/USAGE → ITypeSymbol
        var diagBag = new DiagnosticBag();
        data.ResolvedType = PicUsageResolver.ResolveForDataItem(
            name, picString, usage, diagBag, line);
        foreach (var d in diagBag.Diagnostics)
            _diagnostics.Add(d);

        // Declare in data division scope
        // COBOL allows duplicate names (resolved by IN/OF qualification)
        _symbols.Program.DataDivisionScope.TryDeclare(data, out _);

        return null;
    }

    private static UsageKind ExtractUsage(CobolParserCore.UsageClauseContext usageClause)
    {
        var usageKw = usageClause.usageKeyword();
        return UsageMapper.FromUsageKeyword(usageKw?.GetText());
    }

    // ── Procedure Division ──

    public override object? VisitProcedureDivision(CobolParserCore.ProcedureDivisionContext ctx)
    {
        using var _ = _symbols.PushScope(_symbols.Program.ProcedureDivisionScope);
        return base.VisitProcedureDivision(ctx);
    }

    public override object? VisitSectionDeclaration(CobolParserCore.SectionDeclarationContext ctx)
    {
        var nameCtx = ctx.sectionName();
        if (nameCtx == null) return base.VisitSectionDeclaration(ctx);

        string name = nameCtx.GetText();
        var section = new SectionSymbol(name,
            _symbols.Program.ProcedureDivisionScope, ctx.Start.Line);

        if (!_symbols.Program.ProcedureDivisionScope.TryDeclare(section, out var existingSec))
            Error(ctx, $"Duplicate section '{name}'.");

        using var scopeGuard = _symbols.PushScope(section.Scope);
        foreach (var para in ctx.paragraphDeclaration())
            VisitParagraphDeclaration(para);

        return null;
    }

    public override object? VisitParagraphDeclaration(CobolParserCore.ParagraphDeclarationContext ctx)
    {
        var nameCtx = ctx.paragraphName();
        if (nameCtx == null) return base.VisitParagraphDeclaration(ctx);

        string name = nameCtx.GetText();
        var paragraph = new ParagraphSymbol(name, _symbols.CurrentScope, ctx.Start.Line);

        _symbols.CurrentScope.TryDeclare(paragraph, out var existingLocal);
        // Also register in procedure division scope for global PERFORM/GO TO resolution
        _symbols.Program.ProcedureDivisionScope.TryDeclare(paragraph, out var existingGlobal);

        using var paraScope = _symbols.PushScope(paragraph.Scope);
        foreach (var stmt in ctx.statement())
            Visit(stmt);

        return null;
    }
}
