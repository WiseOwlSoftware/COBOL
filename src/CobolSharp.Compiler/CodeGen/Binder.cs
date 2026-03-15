using CobolSharp.Runtime;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Generated;
using CobolSharp.Compiler.IR;
using CobolSharp.Compiler.Semantics;
using CobolSharp.Compiler.Semantics.Bound;

namespace CobolSharp.Compiler.CodeGen;

/// <summary>
/// The Binder lowers a BoundProgram (typed, symbol-resolved) into an IrModule.
/// It never touches the parse tree — all syntax is pre-resolved by BoundTreeBuilder.
/// </summary>
public sealed class Binder
{
    private readonly SemanticModel _semantic;
    private readonly RecordLayoutBuilder _layout;
    private readonly DiagnosticBag _diagnostics;
    private readonly IrValueFactory _valueFactory = new();
    private readonly Dictionary<string, IrMethod> _paragraphMethods =
        new(StringComparer.OrdinalIgnoreCase);

    public Binder(SemanticModel semantic, DiagnosticBag diagnostics)
    {
        _semantic = semantic;
        _layout = new RecordLayoutBuilder();
        _diagnostics = diagnostics;
    }

    /// <summary>
    /// Build BoundProgram from parse tree, then lower to IrModule.
    /// </summary>
    public IrModule Bind(CobolParserCore.CompilationUnitContext tree)
    {
        // Phase 1: Build bound tree from parse tree + symbols
        var builder = new BoundTreeBuilder(_semantic, _diagnostics);
        var boundProgram = builder.Build(tree);

        // Phase 2: Build record types
        var module = new IrModule(boundProgram.Program.Name);
        BuildRecordTypes(module);

        // Phase 3: Create paragraph method stubs
        foreach (var para in boundProgram.Paragraphs)
        {
            var method = new IrMethod($"Para_{para.Symbol.Name}", returnType: IrPrimitiveType.Void);
            method.Blocks.Add(new IrBasicBlock($"{para.Symbol.Name}_entry"));
            _paragraphMethods[para.Symbol.Name] = method;
            module.Methods.Add(method);
        }

        // Phase 4: Lower bound statements into IR
        foreach (var para in boundProgram.Paragraphs)
        {
            if (_paragraphMethods.TryGetValue(para.Symbol.Name, out var method))
            {
                var block = method.Blocks[0];
                foreach (var stmt in para.Statements)
                    LowerStatement(stmt, block);
            }
        }

        // Phase 5: Create entry point
        CreateEntryPoint(module, boundProgram);

        return module;
    }

    // ── Record layout ──

    private void BuildRecordTypes(IrModule module)
    {
        foreach (var record in _semantic.DataRecords)
        {
            var layout = _layout.Build(record);
            module.Types.Add(layout.RecordType);
        }
    }

    // ── Entry point ──

    private void CreateEntryPoint(IrModule module, BoundProgram boundProgram)
    {
        var main = new IrMethod("Main", returnType: IrPrimitiveType.Void);
        var block = new IrBasicBlock("main_entry");

        if (boundProgram.Paragraphs.Count > 0)
        {
            var firstPara = boundProgram.Paragraphs[0];
            if (_paragraphMethods.TryGetValue(firstPara.Symbol.Name, out var firstMethod))
                block.Instructions.Add(new IrPerform(firstMethod));
        }

        block.Instructions.Add(new IrReturn(null));
        main.Blocks.Add(block);
        module.Methods.Insert(0, main);
    }

    // ── Statement lowering ──

    private void LowerStatement(BoundStatement stmt, IrBasicBlock block)
    {
        switch (stmt)
        {
            case BoundDisplayStatement disp:
                LowerDisplay(disp, block);
                break;
            case BoundMoveStatement mv:
                LowerMove(mv, block);
                break;
            case BoundPerformStatement perf:
                LowerPerform(perf, block);
                break;
            case BoundWriteStatement wr:
                LowerWrite(wr, block);
                break;
            case BoundIfStatement iff:
                LowerIf(iff, block);
                break;
            case BoundGoToStatement gt:
                LowerGoTo(gt, block);
                break;
            case BoundStopStatement:
                block.Instructions.Add(new IrReturn(null));
                break;
            case BoundExitStatement:
                block.Instructions.Add(new IrReturn(null));
                break;
            case BoundOpenStatement:
            {
                // Emit FileRuntime.OpenOutput("PRINT-FILE")
                // TODO: resolve actual file name from bound statement
                var fnVal = _valueFactory.Next(IrPrimitiveType.String);
                block.Instructions.Add(new IrLoadConst(fnVal, "PRINT-FILE"));
                block.Instructions.Add(new IrRuntimeCall(
                    null, "CobolRuntime.OpenOutput", new[] { fnVal }));
                break;
            }
            case BoundCloseStatement:
            {
                var fnVal = _valueFactory.Next(IrPrimitiveType.String);
                block.Instructions.Add(new IrLoadConst(fnVal, "PRINT-FILE"));
                block.Instructions.Add(new IrRuntimeCall(
                    null, "CobolRuntime.CloseFile", new[] { fnVal }));
                break;
            }
            case BoundArithmeticStatement:
                // Placeholder — NOP for now
                break;
        }
    }

    // ── DISPLAY ──

    private void LowerDisplay(BoundDisplayStatement disp, IrBasicBlock block)
    {
        // Concatenate operand texts for now
        var parts = new List<string>();
        foreach (var op in disp.Operands)
        {
            if (op is BoundLiteralExpression lit && lit.Value is string s)
                parts.Add(s);
            else if (op is BoundIdentifierExpression id)
                parts.Add($"[{id.Symbol.Name}]"); // placeholder — later load from storage
            else
                parts.Add(op.ToString() ?? "");
        }

        string text = string.Join(" ", parts);
        var constVal = _valueFactory.Next(IrPrimitiveType.String);
        block.Instructions.Add(new IrLoadConst(constVal, text));
        block.Instructions.Add(new IrRuntimeCall(
            null, "CobolRuntime.Display", new[] { constVal }));
    }

    // ── MOVE ──

    private void LowerMove(BoundMoveStatement mv, IrBasicBlock block)
    {
        // Handle MOVE "literal" TO identifier (string literal)
        if (mv.Source is BoundLiteralExpression lit && lit.Value is string s)
        {
            foreach (var t in mv.Targets)
            {
                if (t is BoundIdentifierExpression id)
                {
                    var loc = _semantic.GetStorageLocation(id.Symbol);
                    if (loc.HasValue)
                    {
                        block.Instructions.Add(new IrMoveStringToField(loc.Value, s));
                        continue;
                    }
                }
            }
            return;
        }

        // Handle MOVE numeric-literal TO identifier
        if (mv.Source is BoundLiteralExpression numLit && numLit.Value is decimal d)
        {
            string numStr = d.ToString(System.Globalization.CultureInfo.InvariantCulture);
            foreach (var t in mv.Targets)
            {
                if (t is BoundIdentifierExpression id)
                {
                    var loc = _semantic.GetStorageLocation(id.Symbol);
                    if (loc.HasValue)
                    {
                        block.Instructions.Add(new IrMoveStringToField(loc.Value, numStr));
                    }
                }
            }
            return;
        }

        // Handle MOVE identifier TO identifier (field-to-field)
        if (mv.Source is BoundIdentifierExpression srcId)
        {
            var srcLoc = _semantic.GetStorageLocation(srcId.Symbol);
            if (srcLoc.HasValue)
            {
                foreach (var t in mv.Targets)
                {
                    if (t is BoundIdentifierExpression destId)
                    {
                        var destLoc = _semantic.GetStorageLocation(destId.Symbol);
                        if (destLoc.HasValue)
                        {
                            block.Instructions.Add(new IrPicMove(
                                srcLoc.Value, destLoc.Value,
                                mv.IsRounded ? 1 : 0));
                        }
                    }
                }
                return;
            }
        }

        // Fallback for unresolved: NOP
    }

    // ── PERFORM ──

    private void LowerPerform(BoundPerformStatement perf, IrBasicBlock block)
    {
        if (!_paragraphMethods.TryGetValue(perf.Target.Name, out var method))
            return;

        int times = perf.Times > 0 ? perf.Times : 1;
        for (int i = 0; i < times; i++)
            block.Instructions.Add(new IrPerform(method));
    }

    // ── WRITE ──

    private void LowerWrite(BoundWriteStatement wr, IrBasicBlock block)
    {
        string fileName = wr.File?.Name ?? "PRINT-FILE";

        // Try to get storage location for the record
        var recordLoc = _semantic.GetStorageLocation(wr.Record);
        if (recordLoc.HasValue)
        {
            // Real storage: emit IrWriteRecordFromStorage
            block.Instructions.Add(new IrWriteRecordFromStorage(fileName, recordLoc.Value));
        }
        else
        {
            // Fallback: write placeholder via WriteText
            var fileNameVal = _valueFactory.Next(IrPrimitiveType.String);
            block.Instructions.Add(new IrLoadConst(fileNameVal, fileName));
            var textVal = _valueFactory.Next(IrPrimitiveType.String);
            block.Instructions.Add(new IrLoadConst(textVal, $"[RECORD: {wr.Record.Name}]"));
            block.Instructions.Add(new IrRuntimeCall(
                null, "CobolRuntime.WriteText",
                new[] { fileNameVal, textVal }));
        }
    }

    // ── IF ──

    private void LowerIf(BoundIfStatement iff, IrBasicBlock block)
    {
        // Simplified: emit then-statements sequentially (always-true for now)
        // TODO: condition → IrBinary → IrBranch with then/else blocks
        foreach (var stmt in iff.ThenStatements)
            LowerStatement(stmt, block);
    }

    // ── GO TO ──

    private void LowerGoTo(BoundGoToStatement gt, IrBasicBlock block)
    {
        if (_paragraphMethods.TryGetValue(gt.Target.Name, out var method))
        {
            block.Instructions.Add(new IrPerform(method));
            block.Instructions.Add(new IrReturn(null));
        }
    }
}
