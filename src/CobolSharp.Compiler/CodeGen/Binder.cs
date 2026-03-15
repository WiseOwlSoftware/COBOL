// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
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
            case BoundMultiplyStatement mult:
                LowerMultiply(mult, block);
                break;
            case BoundAddStatement add:
                LowerAdd(add, block);
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
        // Handle MOVE literal TO identifier
        if (mv.Source is BoundLiteralExpression lit)
        {
            foreach (var t in mv.Targets)
            {
                if (t is not BoundIdentifierExpression id) continue;
                var loc = _semantic.GetStorageLocation(id.Symbol);
                if (!loc.HasValue) continue;

                if (lit.Value is string s)
                {
                    // String literal → MoveStringToField
                    block.Instructions.Add(new IrMoveStringToField(loc.Value, s));
                }
                else if (lit.Value is decimal d)
                {
                    if (loc.Value.Pic.IsNumeric)
                    {
                        // Numeric literal → PicRuntime.MoveNumericLiteral
                        block.Instructions.Add(new IrPicMoveLiteralNumeric(
                            loc.Value, d, mv.IsRounded ? 1 : 0));
                    }
                    else
                    {
                        // Numeric to alpha field: format as string
                        string numStr = d.ToString(System.Globalization.CultureInfo.InvariantCulture);
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

    // ── MULTIPLY ──

    private void LowerMultiply(BoundMultiplyStatement mult, IrBasicBlock block)
    {
        // Destination: GIVING target or in-place (BY operand)
        DataSymbol? destSym = mult.GivingTarget
            ?? (mult.Right as BoundIdentifierExpression)?.Symbol;
        if (destSym == null) return;

        var destLoc = _semantic.GetStorageLocation(destSym);
        if (!destLoc.HasValue) return;

        // identifier × identifier
        if (mult.Left is BoundIdentifierExpression leftId &&
            mult.Right is BoundIdentifierExpression rightId)
        {
            var leftLoc = _semantic.GetStorageLocation(leftId.Symbol);
            var rightLoc = _semantic.GetStorageLocation(rightId.Symbol);
            if (leftLoc.HasValue && rightLoc.HasValue)
            {
                block.Instructions.Add(new IrPicMultiply(
                    leftLoc.Value, rightLoc.Value, destLoc.Value));
            }
            return;
        }

        // literal × identifier
        if (mult.Left is BoundLiteralExpression litLeft && litLeft.Value is decimal dLeft &&
            mult.Right is BoundIdentifierExpression rId)
        {
            var rightLoc = _semantic.GetStorageLocation(rId.Symbol);
            if (rightLoc.HasValue)
            {
                block.Instructions.Add(new IrPicMultiplyLiteral(
                    dLeft, rightLoc.Value, destLoc.Value));
            }
            return;
        }

        // identifier × literal (swap)
        if (mult.Right is BoundLiteralExpression litRight && litRight.Value is decimal dRight &&
            mult.Left is BoundIdentifierExpression lId)
        {
            var leftLoc = _semantic.GetStorageLocation(lId.Symbol);
            if (leftLoc.HasValue)
            {
                block.Instructions.Add(new IrPicMultiplyLiteral(
                    dRight, leftLoc.Value, destLoc.Value));
            }
        }
    }

    // ── ADD ──

    private void LowerAdd(BoundAddStatement add, IrBasicBlock block)
    {
        if (add.Target is not BoundIdentifierExpression destId) return;
        var destLoc = _semantic.GetStorageLocation(destId.Symbol);
        if (!destLoc.HasValue) return;

        // ADD literal TO identifier
        if (add.Operand is BoundLiteralExpression lit && lit.Value is decimal d)
        {
            block.Instructions.Add(new IrPicAddLiteral(
                destLoc.Value, d, add.IsRounded ? 1 : 0));
            return;
        }

        // ADD identifier TO identifier
        if (add.Operand is BoundIdentifierExpression srcId)
        {
            var srcLoc = _semantic.GetStorageLocation(srcId.Symbol);
            if (srcLoc.HasValue)
            {
                block.Instructions.Add(new IrPicAdd(
                    srcLoc.Value, destLoc.Value, add.IsRounded ? 1 : 0));
            }
        }
    }

    // ── IF ──

    private void LowerIf(BoundIfStatement iff, IrBasicBlock block)
    {
        // Check if condition is a real comparison
        if (iff.Condition is BoundBinaryExpression binCond)
        {
            var leftSym = (binCond.Left as BoundIdentifierExpression)?.Symbol;
            var rightSym = (binCond.Right as BoundIdentifierExpression)?.Symbol;

            // Compare two identifiers (numeric comparison)
            if (leftSym != null)
            {
                var leftLoc = _semantic.GetStorageLocation(leftSym);

                // Right side can be identifier or literal
                StorageLocation? rightLoc = null;
                if (rightSym != null)
                    rightLoc = _semantic.GetStorageLocation(rightSym);

                if (leftLoc.HasValue)
                {
                    // Emit compare + branch
                    var condVal = _valueFactory.Next(IrPrimitiveType.Bool);

                    if (rightLoc.HasValue)
                    {
                        block.Instructions.Add(new IrPicCompare(
                            leftLoc.Value, rightLoc.Value, condVal,
                            (int)binCond.OperatorKind));
                    }
                    else if (binCond.Right is BoundLiteralExpression litRight)
                    {
                        // Compare identifier to literal: write literal to temp, compare
                        // For now: fall through to always-then
                    }

                    // Simple branch: if condition true → then, else → else
                    // For now, use condVal to gate then/else
                    // TODO: proper basic block branching
                    // Emit then statements when condition is true
                    foreach (var stmt in iff.ThenStatements)
                        LowerStatement(stmt, block);

                    // Emit else if present
                    if (iff.ElseStatements != null)
                    {
                        foreach (var stmt in iff.ElseStatements)
                            LowerStatement(stmt, block);
                    }
                    return;
                }
            }
        }

        // Fallback: emit then-statements (always-true)
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
