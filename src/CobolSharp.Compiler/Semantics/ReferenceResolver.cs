// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Generated;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Pass 2: Walk the ANTLR parse tree and resolve all identifier references
/// against the symbol table built by SemanticBuilder.
/// </summary>
public sealed class ReferenceResolver : CobolParserCoreBaseVisitor<object?>
{
    private readonly SymbolTable _symbols;
    private readonly List<Diagnostic> _diagnostics;

    public ReferenceResolver(SymbolTable symbols, List<Diagnostic> diagnostics)
    {
        _symbols = symbols;
        _diagnostics = diagnostics;
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

    // ── Resolve PERFORM targets ──

    public override object? VisitPerformStatement(CobolParserCore.PerformStatementContext ctx)
    {
        var procNames = ctx.procedureName();
        if (procNames.Length > 0)
        {
            foreach (var procName in procNames)
            {
                string name = procName.GetText();
                var sym = _symbols.Program.ProcedureDivisionScope.Resolve(name);

                if (sym is not (ParagraphSymbol or SectionSymbol))
                    Error(procName, $"PERFORM target '{name}' is not a paragraph or section.");
            }
        }

        return base.VisitPerformStatement(ctx);
    }

    // ── Resolve GO TO targets ──

    public override object? VisitGoToStatement(CobolParserCore.GoToStatementContext ctx)
    {
        var procNames = ctx.procedureName();

        foreach (var pn in procNames)
        {
            string name = pn.GetText();
            var sym = _symbols.Program.ProcedureDivisionScope.Resolve(name);

            if (sym is not (ParagraphSymbol or SectionSymbol))
                Error(pn, $"GO TO target '{name}' is not a paragraph or section.");
        }

        return base.VisitGoToStatement(ctx);
    }

    // ── Resolve file names in I/O statements ──

    public override object? VisitReadStatement(CobolParserCore.ReadStatementContext ctx)
    {
        var fileCtx = ctx.fileName();
        if (fileCtx != null)
        {
            string name = fileCtx.GetText();
            if (_symbols.Program.GlobalScope.Resolve<FileSymbol>(name) is null)
                Error(fileCtx, $"READ target '{name}' is not a declared file.");
        }
        return base.VisitReadStatement(ctx);
    }

    public override object? VisitWriteStatement(CobolParserCore.WriteStatementContext ctx)
    {
        // WRITE uses record name, not file name — skip file validation for now
        return base.VisitWriteStatement(ctx);
    }

    public override object? VisitOpenStatement(CobolParserCore.OpenStatementContext ctx)
    {
        foreach (var clause in ctx.openClause())
        {
            foreach (var id in clause.dataReference())
            {
                string name = id.GetText();
                if (_symbols.Program.GlobalScope.Resolve<FileSymbol>(name) is null)
                    Error(id, $"OPEN target '{name}' is not a declared file.");
            }
        }
        return base.VisitOpenStatement(ctx);
    }

    public override object? VisitCloseStatement(CobolParserCore.CloseStatementContext ctx)
    {
        var idList = ctx.dataReferenceList();
        if (idList != null)
        {
            foreach (var id in idList.dataReference())
            {
                string name = id.GetText();
                if (_symbols.Program.GlobalScope.Resolve<FileSymbol>(name) is null)
                    Error(id, $"CLOSE target '{name}' is not a declared file.");
            }
        }
        return base.VisitCloseStatement(ctx);
    }
}
