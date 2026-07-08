// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolSharp.Compiler.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Pass 2: Walk the ANTLR parse tree and resolve all identifier references
/// against the symbol table built by SemanticBuilder.
/// </summary>
public sealed class ReferenceResolver : CobolParserCoreBaseVisitor<object?>
{
    private readonly SymbolTable _symbols;
    private readonly List<Diagnostic> _diagnostics;
    private readonly string _sourceName;
    private readonly HashSet<string> _knownNames;
    private bool _inProcedureDivision;

    // OO (ISO §11.7): per-method paragraph scopes. A method's paragraphs live ONLY in their method scope (not the
    // program-wide ProcedureDivisionScope), so PERFORM/GO TO targets must be resolved method-locally first.
    private readonly IReadOnlyList<(string Name, Scope Scope, IReadOnlyList<string> Using)> _classMethodScopes;
    private Scope? _currentMethodScope;

    public ReferenceResolver(SymbolTable symbols, List<Diagnostic> diagnostics, string sourceName,
        IEnumerable<string> knownNames,
        IReadOnlyList<(string Name, Scope Scope, IReadOnlyList<string> Using)>? classMethodScopes = null)
    {
        _symbols = symbols;
        _diagnostics = diagnostics;
        _sourceName = sourceName;
        _knownNames = new HashSet<string>(knownNames, StringComparer.OrdinalIgnoreCase);
        _classMethodScopes = classMethodScopes ?? [];
    }

    /// <summary>OO §11.7: resolve a PERFORM/GO TO target in the current METHOD scope first, then program-wide.</summary>
    private Symbol? ResolveProcedureTarget(string name)
        => _currentMethodScope?.Resolve(name) ?? _symbols.Program.ProcedureDivisionScope.Resolve(name);

    public override object? VisitMethodDefinition(CobolParserCore.MethodDefinitionContext ctx)
    {
        var nameCtxs = ctx.methodName();
        string methodName = nameCtxs is { Length: > 0 } ? nameCtxs[0].GetText() : "";
        Scope? scope = null;
        foreach (var m in _classMethodScopes)
            if (string.Equals(m.Name, methodName, StringComparison.OrdinalIgnoreCase)) { scope = m.Scope; break; }

        var prev = _currentMethodScope;
        _currentMethodScope = scope;
        var result = base.VisitMethodDefinition(ctx);
        _currentMethodScope = prev;
        return result;
    }

    // Do not resolve references inside a CONTAINED (nested) program — it is its own source element, resolved
    // by its own ReferenceResolver pass (Compilation builds each separately). Descending into it from the
    // containing program's walk would resolve the contained program's references in the wrong scope (IC235A:
    // contained USING params ELEM-NON-01/SUBSCRIPTED-DATA resolve to the container's WORKING-STORAGE → CBL3108).
    private Antlr4.Runtime.Tree.IParseTree? _walkRoot;

    public override object? Visit(Antlr4.Runtime.Tree.IParseTree tree)
    {
        _walkRoot ??= tree;
        return base.Visit(tree);
    }

    public override object? VisitNestedProgram(CobolParserCore.NestedProgramContext context)
        => ReferenceEquals(context, _walkRoot) ? base.VisitNestedProgram(context) : DefaultResult;

    private void Error(ParserRuleContext ctx, DiagnosticDescriptor descriptor, params object[] args)
    {
        string message = args.Length > 0
            ? string.Format(descriptor.MessageTemplate, args)
            : descriptor.MessageTemplate;
        _diagnostics.Add(new Diagnostic(
            descriptor.Code,
            descriptor.DefaultSeverity,
            message,
            new Common.SourceLocation(_sourceName, 0, ctx.Start.Line, ctx.Start.Column),
            new Common.TextSpan(ctx.Start.StartIndex, ctx.Stop?.StopIndex ?? ctx.Start.StopIndex)));
    }

    // ── Undefined data-name detection (ISO §8.4.2.1: uniqueness/validity of reference) ──
    // Without this, the binder demotes an unresolved name to an alphanumeric literal and lowering
    // silently drops it, so `MOVE 5 TO NONEXISTENT-ITEM.` compiled clean and exited 0 — the
    // assessment's #1 commercial gap. ONE centralized pass over operand-position data references
    // (not 66 scattered binder return-null sites) flags any whose BASE name resolves to no symbol of
    // any kind. Active in ALL dialects (DEVLOG 310): the staged rollout completed once the corpus
    // dry-run was clean (0/349, after the IC228A inherited-GLOBAL fix). `_knownNames` includes the
    // SPECIAL-NAMES names AND the IS GLOBAL names inherited from containing programs (both threaded in
    // via Compilation), neither of which lives in this program's own Scope. (DEVLOG 305/310)

    public override object? VisitProcedureDivision(CobolParserCore.ProcedureDivisionContext ctx)
    {
        _inProcedureDivision = true;
        try { return base.VisitProcedureDivision(ctx); }
        finally { _inProcedureDivision = false; }
    }

    public override object? VisitDataReference(CobolParserCore.DataReferenceContext ctx)
    {
        if (_inProcedureDivision)
            CheckUndefinedDataName(ctx);
        return base.VisitDataReference(ctx);
    }

    private void CheckUndefinedDataName(CobolParserCore.DataReferenceContext ctx)
    {
        if (ctx.LINAGE_COUNTER() != null) return;          // special register (ISO §8.4.3.14)
        var baseWord = ctx.cobolWord();                     // base name; OF/IN qualifiers are nested qualification nodes
        if (baseWord == null) return;
        string name = baseWord.GetText();
        if (IsDefinedName(name)) return;
        Error(baseWord, DiagnosticDescriptors.CBL3128, name);
    }

    // A name is "defined" if it resolves to any symbol kind in any scope (data item, level-88
    // condition-name, index-name, file connector, paragraph/section) or is a known SPECIAL-NAMES name
    // (mnemonic, switch ON/OFF condition, symbolic character, class, alphabet) — none of which live in a
    // Scope. Untyped Scope.Resolve returns any kind, so a real declaration is never false-flagged.
    private bool IsDefinedName(string name) =>
        _symbols.Program.DataDivisionScope.Resolve(name) != null
        || _symbols.Program.GlobalScope.Resolve(name) != null
        || _symbols.Program.ProcedureDivisionScope.Resolve(name) != null
        || _knownNames.Contains(name)
        || SpecialRegisters.Contains(name);

    // COBOL special registers that lex as ordinary identifiers (LINAGE-COUNTER / LINE-COUNTER /
    // PAGE-COUNTER are distinct tokens handled elsewhere). These are RECOGNIZED COBOL names, not
    // "undefined", so the undefined-name check must not flag them — even when a given register is not yet
    // implemented (visible/known is a separate concern from supported). Covers the COBOL-85 debugging
    // module registers (ISO §14.x DEBUG-ITEM) and the universal vendor SORT/RETURN-CODE registers, which
    // the permissive Default dialect accepts. (DEVLOG 310)
    private static readonly HashSet<string> SpecialRegisters = new(StringComparer.OrdinalIgnoreCase)
    {
        "RETURN-CODE", "SORT-RETURN", "SORT-CONTROL", "SORT-CORE-SIZE", "SORT-FILE-SIZE", "SORT-MODE-SIZE",
        "TALLY", "DEBUG-ITEM", "DEBUG-LINE", "DEBUG-NAME", "DEBUG-CONTENTS",
        "DEBUG-SUB-1", "DEBUG-SUB-2", "DEBUG-SUB-3",
    };

    private static string ExtractProcedureName(CobolParserCore.ProcedureNameContext ctx)
    {
        var words = ctx.cobolWord();
        if (words.Length > 0) return words[0].GetText();
        var ints = ctx.INTEGERLIT();
        if (ints.Length > 0) return ints[0].GetText();
        return ctx.GetText();
    }

    // ── Resolve PERFORM targets ──

    public override object? VisitPerformStatement(CobolParserCore.PerformStatementContext ctx)
    {
        var procNames = ctx.procedureName();
        if (procNames.Length > 0)
        {
            foreach (var procName in procNames)
            {
                string name = ExtractProcedureName(procName);
                var sym = ResolveProcedureTarget(name);

                if (sym is not (ParagraphSymbol or SectionSymbol))
                    Error(procName, DiagnosticDescriptors.CBL3120, "PERFORM", name);
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
            string name = ExtractProcedureName(pn);
            var sym = ResolveProcedureTarget(name);

            if (sym is not (ParagraphSymbol or SectionSymbol))
                Error(pn, DiagnosticDescriptors.CBL3120, "GO TO", name);
        }

        return base.VisitGoToStatement(ctx);
    }

    // ── Resolve file names in I/O statements ──

    // A file reference is valid if a FileSymbol of that name is declared in this program OR the name is an
    // FD ... IS GLOBAL file inherited from a containing program (ISO §8.4.6.2.2) — such inherited global
    // file-names are threaded in via _knownNames, since the inherited FileSymbol is not added to this
    // program's GlobalScope until after this resolution pass (see Compilation.InheritGlobalItems).
    private bool IsDeclaredFile(string name) =>
        _symbols.Program.GlobalScope.Resolve<FileSymbol>(name) != null || _knownNames.Contains(name);

    public override object? VisitReadStatement(CobolParserCore.ReadStatementContext ctx)
    {
        var fileCtx = ctx.fileName();
        if (fileCtx != null)
        {
            string name = fileCtx.GetText();
            if (!IsDeclaredFile(name))
                Error(fileCtx, DiagnosticDescriptors.CBL3121, "READ", name);
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
            foreach (var spec in clause.openFileSpec())
            {
                var id = spec.dataReference();
                string name = id.GetText();
                if (!IsDeclaredFile(name))
                    Error(id, DiagnosticDescriptors.CBL3121, "OPEN", name);
            }
        }
        return base.VisitOpenStatement(ctx);
    }

    public override object? VisitCloseStatement(CobolParserCore.CloseStatementContext ctx)
    {
        foreach (var phrase in ctx.closeFilePhrase())
        {
            var fn = phrase.fileName();
            if (fn != null)
            {
                string name = fn.GetText();
                if (!IsDeclaredFile(name))
                    Error(fn, DiagnosticDescriptors.CBL3121, "CLOSE", name);
            }
        }
        return base.VisitCloseStatement(ctx);
    }
}
