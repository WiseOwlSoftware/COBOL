using Antlr4.Runtime;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Generated;
using CobolSharp.Compiler.IR;
using CobolSharp.Compiler.Semantics;

namespace CobolSharp.Compiler.CodeGen;

/// <summary>
/// The Binder walks the ANTLR parse tree with resolved symbols and types,
/// and produces an IrModule that the CIL emitter consumes.
///
/// It's deliberately thin — all PIC, type, and flow analysis is pre-computed
/// in the SemanticModel. The binder just maps to IR.
/// </summary>
public sealed class Binder
{
    private readonly SemanticModel _semantic;
    private readonly RecordLayoutBuilder _layout;
    private readonly DiagnosticBag _diagnostics;
    private readonly IrValueFactory _valueFactory = new();
    private readonly Dictionary<string, IrMethod> _paragraphMethods = new(StringComparer.OrdinalIgnoreCase);

    public Binder(SemanticModel semantic, DiagnosticBag diagnostics)
    {
        _semantic = semantic;
        _layout = new RecordLayoutBuilder();
        _diagnostics = diagnostics;
    }

    /// <summary>
    /// Bind the parse tree into a complete IrModule.
    /// </summary>
    public IrModule Bind(CobolParserCore.CompilationUnitContext tree)
    {
        string programName = _semantic.Program.Name;
        var module = new IrModule(programName);

        // 1. Build record types from data symbols
        BuildRecordTypes(module);

        // 2. Create method stubs for all paragraphs (so PERFORM can reference them)
        CreateParagraphMethods(module);

        // 3. Walk procedure division and lower statements into IR
        LowerProcedureDivision(tree, module);

        // 4. Create entry point method
        CreateEntryPoint(module);

        return module;
    }

    // ── Record layout ──

    private void BuildRecordTypes(IrModule module)
    {
        foreach (var record in _semantic.DataRecords)
        {
            var layout = _layout.Build(record);
            module.Types.Add(layout.RecordType);

            // Register storage locations for each field
            foreach (var field in layout.RecordType.Fields)
            {
                var dataSym = FindDataChild(record, field.Name);
                if (dataSym != null)
                {
                    var pic = PicDescriptor.FromDataSymbol(dataSym, field.Size);
                    _semantic.RegisterStorageLocation(dataSym, new StorageLocation(field, pic));
                }
            }
        }
    }

    private static DataSymbol? FindDataChild(DataSymbol parent, string name)
    {
        if (string.Equals(parent.Name, name, StringComparison.OrdinalIgnoreCase))
            return parent;
        foreach (var child in parent.Children)
        {
            var found = FindDataChild(child, name);
            if (found != null) return found;
        }
        return null;
    }

    // ── Paragraph methods ──

    private void CreateParagraphMethods(IrModule module)
    {
        foreach (var para in _semantic.ParagraphsInOrder)
        {
            var method = new IrMethod($"Para_{para.Name}", returnType: IrPrimitiveType.Void);
            method.Blocks.Add(new IrBasicBlock($"{para.Name}_entry"));
            _paragraphMethods[para.Name] = method;
            module.Methods.Add(method);
        }
    }

    private void CreateEntryPoint(IrModule module)
    {
        var main = new IrMethod("Main", returnType: IrPrimitiveType.Void);
        var block = new IrBasicBlock("main_entry");

        // Call each paragraph in order (simplified — real COBOL has PERFORM/GO TO flow)
        if (_semantic.ParagraphsInOrder.Count > 0)
        {
            var firstPara = _semantic.ParagraphsInOrder[0];
            if (_paragraphMethods.TryGetValue(firstPara.Name, out var firstMethod))
            {
                block.Instructions.Add(new IrPerform(firstMethod));
            }
        }

        block.Instructions.Add(new IrReturn(null));
        main.Blocks.Add(block);
        module.Methods.Insert(0, main); // Main is first method
    }

    // ── Procedure division lowering ──

    private void LowerProcedureDivision(CobolParserCore.CompilationUnitContext tree, IrModule module)
    {
        // Walk the parse tree to find procedure division statements
        var visitor = new ProcedureLoweringVisitor(this);
        visitor.Visit(tree);
    }

    /// <summary>
    /// Visitor that walks the parse tree's procedure division and lowers
    /// statements into IR instructions in the appropriate paragraph method.
    /// </summary>
    private sealed class ProcedureLoweringVisitor : CobolParserCoreBaseVisitor<object?>
    {
        private readonly Binder _binder;
        private IrBasicBlock? _currentBlock;

        public ProcedureLoweringVisitor(Binder binder)
        {
            _binder = binder;
        }

        public override object? VisitParagraphDeclaration(CobolParserCore.ParagraphDeclarationContext ctx)
        {
            var nameCtx = ctx.paragraphName();
            if (nameCtx == null) return base.VisitParagraphDeclaration(ctx);

            string name = nameCtx.GetText();
            if (_binder._paragraphMethods.TryGetValue(name, out var method))
            {
                _currentBlock = method.Blocks[0]; // entry block
                foreach (var stmt in ctx.statement())
                    LowerStatement(stmt);
            }

            return null; // don't recurse — we handled statements manually
        }

        private void LowerStatement(CobolParserCore.StatementContext ctx)
        {
            if (_currentBlock == null) return;

            if (ctx.moveStatement() != null)
                LowerMove(ctx.moveStatement());
            else if (ctx.displayStatement() != null)
                LowerDisplay(ctx.displayStatement());
            else if (ctx.performStatement() != null)
                LowerPerform(ctx.performStatement());
            else if (ctx.goToStatement() != null)
                LowerGoTo(ctx.goToStatement());
            else if (ctx.stopStatement() != null)
                LowerStop(ctx.stopStatement());
            else if (ctx.addStatement() != null)
                LowerAdd(ctx.addStatement());
            else if (ctx.ifStatement() != null)
                LowerIf(ctx.ifStatement());
            else if (ctx.exitStatement() != null)
                LowerExit(ctx.exitStatement());
            else if (ctx.openStatement() != null)
                LowerOpen(ctx.openStatement());
            else if (ctx.closeStatement() != null)
                LowerClose(ctx.closeStatement());
            // ... other statements emit IrRuntimeCall or are skipped for now
        }

        private void LowerMove(CobolParserCore.MoveStatementContext ctx)
        {
            if (_currentBlock == null) return;

            // For now, emit as a runtime call placeholder
            // Full implementation: resolve source/dest symbols → IrPicMove
            var v = _binder._valueFactory.Next(IrPrimitiveType.Void);
            _currentBlock.Instructions.Add(new IrRuntimeCall(
                null, "CobolRuntime.Move",
                Array.Empty<IrValue>()));
        }

        private void LowerDisplay(CobolParserCore.DisplayStatementContext ctx)
        {
            if (_currentBlock == null) return;

            var operands = new List<string>();

            // Walk typed child contexts (identifier | literal), skip DISPLAY and DOT terminals
            foreach (var child in ctx.children)
            {
                if (child is Antlr4.Runtime.Tree.ITerminalNode t)
                {
                    var kind = t.Symbol.Type;
                    if (kind == CobolLexer.DISPLAY || kind == CobolLexer.DOT)
                        continue;
                }

                if (child is CobolParserCore.IdentifierContext idCtx)
                {
                    // For now just use raw text; later bind to DataSymbol
                    operands.Add(idCtx.GetText());
                    continue;
                }

                if (child is CobolParserCore.LiteralContext litCtx)
                {
                    var s = ExtractLiteralText(litCtx);
                    if (s != null)
                        operands.Add(s);
                    continue;
                }
            }

            var displayText = string.Join(" ", operands);

            var constVal = _binder._valueFactory.Next(IrPrimitiveType.String);
            _currentBlock.Instructions.Add(new IrLoadConst(constVal, displayText));
            _currentBlock.Instructions.Add(new IrRuntimeCall(
                null, "CobolRuntime.Display",
                new[] { constVal }));
        }

        /// <summary>
        /// Extract text from a literal parse context.
        /// Strips quotes from STRINGLIT. For numeric/figurative, returns raw text.
        /// TODO: add special handling for figurativeConstant (ZERO, SPACE, ALL "X", etc.)
        /// </summary>
        private static string? ExtractLiteralText(CobolParserCore.LiteralContext lit)
        {
            // STRINGLIT
            var s = lit.STRINGLIT();
            if (s != null)
            {
                var text = s.GetText();
                if (text.Length >= 2 &&
                    ((text[0] == '"' && text[^1] == '"') ||
                     (text[0] == '\'' && text[^1] == '\'')))
                {
                    return text[1..^1];
                }
                return text;
            }

            // signedNumericLiteral, HEXLIT, figurativeConstant → raw text for now
            return lit.GetText();
        }

        private void LowerPerform(CobolParserCore.PerformStatementContext ctx)
        {
            if (_currentBlock == null) return;

            var target = ctx.performTarget();
            if (target != null)
            {
                var procNames = target.procedureName();
                if (procNames.Length > 0)
                {
                    string name = procNames[0].GetText();
                    if (_binder._paragraphMethods.TryGetValue(name, out var method))
                    {
                        _currentBlock.Instructions.Add(new IrPerform(method));
                    }
                }
            }
        }

        private void LowerGoTo(CobolParserCore.GoToStatementContext ctx)
        {
            if (_currentBlock == null) return;

            // GO TO is lowered as a jump to the target paragraph's method
            var idCtx = ctx.identifier();
            if (idCtx != null)
            {
                string name = idCtx.GetText();
                if (_binder._paragraphMethods.TryGetValue(name, out var method))
                {
                    _currentBlock.Instructions.Add(new IrPerform(method));
                    _currentBlock.Instructions.Add(new IrReturn(null));
                }
            }
        }

        private void LowerStop(CobolParserCore.StopStatementContext ctx)
        {
            if (_currentBlock == null) return;
            _currentBlock.Instructions.Add(new IrReturn(null));
        }

        private void LowerAdd(CobolParserCore.AddStatementContext ctx)
        {
            if (_currentBlock == null) return;

            _currentBlock.Instructions.Add(new IrRuntimeCall(
                null, "CobolRuntime.Add",
                Array.Empty<IrValue>()));
        }

        private void LowerIf(CobolParserCore.IfStatementContext ctx)
        {
            if (_currentBlock == null) return;

            // Simplified: emit all then/else statements sequentially for now
            // Full implementation: condition → IrBinary → IrBranch
            foreach (var impStmt in ctx.imperativeStatement())
            {
                foreach (var stmt in impStmt.statement())
                    LowerStatement(stmt);
            }
        }

        private void LowerExit(CobolParserCore.ExitStatementContext ctx)
        {
            if (_currentBlock == null) return;
            _currentBlock.Instructions.Add(new IrReturn(null));
        }

        private void LowerOpen(CobolParserCore.OpenStatementContext ctx)
        {
            if (_currentBlock == null) return;

            _currentBlock.Instructions.Add(new IrRuntimeCall(
                null, "CobolRuntime.Open",
                Array.Empty<IrValue>()));
        }

        private void LowerClose(CobolParserCore.CloseStatementContext ctx)
        {
            if (_currentBlock == null) return;

            _currentBlock.Instructions.Add(new IrRuntimeCall(
                null, "CobolRuntime.Close",
                Array.Empty<IrValue>()));
        }
    }
}
