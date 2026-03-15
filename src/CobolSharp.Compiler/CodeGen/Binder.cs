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
///
/// Paragraph methods return int (next PC):
///   fall-through → myIndex + 1
///   GO TO X      → indexOf(X)
///   STOP RUN     → -1
/// Main dispatches via: while (pc >= 0 && pc &lt; N) pc = paragraphs[pc]();
/// </summary>
public sealed class Binder
{
    private readonly SemanticModel _semantic;
    private readonly RecordLayoutBuilder _layout;
    private readonly DiagnosticBag _diagnostics;
    private readonly IrValueFactory _valueFactory = new();
    private readonly Dictionary<string, IrMethod> _paragraphMethods =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _paragraphIndices =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _paragraphsByIndex = new();

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

        // Phase 3: Create paragraph method stubs (return Int32 for PC)
        int paraIndex = 0;
        foreach (var para in boundProgram.Paragraphs)
        {
            var method = new IrMethod($"Para_{para.Symbol.Name}", returnType: IrPrimitiveType.Int32);
            method.Blocks.Add(new IrBasicBlock($"{para.Symbol.Name}_entry"));
            _paragraphMethods[para.Symbol.Name] = method;
            _paragraphIndices[para.Symbol.Name] = paraIndex;
            _paragraphsByIndex.Add(para.Symbol.Name);
            module.Methods.Add(method);
            paraIndex++;
        }

        // Phase 4: Lower bound statements into IR
        foreach (var para in boundProgram.Paragraphs)
        {
            if (_paragraphMethods.TryGetValue(para.Symbol.Name, out var method))
            {
                int myIndex = _paragraphIndices[para.Symbol.Name];
                var block = method.Blocks[0];
                foreach (var stmt in para.Statements)
                    block = LowerStatement(stmt, method, block);
                // Fall-through: return next paragraph index
                block.Instructions.Add(new IrReturnConst(myIndex + 1));
            }
        }

        // Phase 5: Create entry point (PC dispatch loop)
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

        // Collect paragraph methods in declaration order
        var orderedMethods = new List<IrMethod>();
        foreach (var para in boundProgram.Paragraphs)
        {
            if (_paragraphMethods.TryGetValue(para.Symbol.Name, out var m))
                orderedMethods.Add(m);
        }

        // Emit PC dispatch loop
        block.Instructions.Add(new IrParagraphDispatch(orderedMethods));

        main.Blocks.Add(block);
        module.Methods.Insert(0, main);
    }

    // ── Statement lowering ──

    private IrBasicBlock LowerStatement(BoundStatement stmt, IrMethod method, IrBasicBlock block)
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
                return LowerIf(iff, method, block);
            case BoundMultiplyStatement mult:
                return LowerMultiply(mult, method, block);
            case BoundAddStatement add:
                LowerAdd(add, block);
                break;
            case BoundGoToStatement gt:
                LowerGoTo(gt, block);
                break;
            case BoundStopStatement:
                block.Instructions.Add(new IrReturnConst(-1));
                break;
            case BoundExitStatement:
                // EXIT is a no-op; fall-through return handles it
                break;
            case BoundOpenStatement:
            {
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
            case BoundSubtractStatement sub:
                return LowerSubtract(sub, method, block);
            case BoundDivideStatement div:
                return LowerDivide(div, method, block);
            case BoundArithmeticStatement:
                break;
        }
        return block;
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

                var destCat = loc.Value.Pic.Category;
                if (lit.Value is string s)
                {
                    // Alphanumeric literal → MoveStringToField
                    block.Instructions.Add(new IrMoveStringToField(loc.Value, s));
                }
                else if (lit.Value is decimal d)
                {
                    if (destCat.IsNumericLike())
                    {
                        // Numeric literal → numeric destination
                        block.Instructions.Add(new IrPicMoveLiteralNumeric(
                            loc.Value, d, mv.IsRounded ? 1 : 0));
                    }
                    else
                    {
                        // Numeric literal → alphanumeric destination: format as string
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
        if (!_paragraphIndices.TryGetValue(perf.Target.Name, out int startIdx))
            return;

        int endIdx = startIdx;
        if (perf.ThruTarget != null &&
            _paragraphIndices.TryGetValue(perf.ThruTarget.Name, out int thruIdx))
            endIdx = thruIdx;

        int times = perf.Times > 0 ? perf.Times : 1;

        if (startIdx == endIdx)
        {
            // Simple PERFORM (no THRU): call once, ignore return value
            var paraName = _paragraphsByIndex[startIdx];
            if (_paragraphMethods.TryGetValue(paraName, out var method))
            {
                for (int t = 0; t < times; t++)
                    block.Instructions.Add(new IrPerform(method));
            }
        }
        else
        {
            // PERFORM THRU: dynamic dispatch that respects GO TO returns
            var methods = new List<IrMethod>();
            for (int i = startIdx; i <= endIdx; i++)
            {
                var paraName = _paragraphsByIndex[i];
                if (_paragraphMethods.TryGetValue(paraName, out var method))
                    methods.Add(method);
                else
                    methods.Add(null!); // placeholder for unresolved paragraphs
            }

            for (int t = 0; t < times; t++)
                block.Instructions.Add(new IrPerformThru(startIdx, endIdx, methods));
        }
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

    private IrBasicBlock LowerMultiply(BoundMultiplyStatement mult, IrMethod method, IrBasicBlock block)
    {
        // One ArithmeticStatus per statement — init once, sticky across all targets
        block.Instructions.Add(new IrInitArithmeticStatus());

        // Emit the arithmetic operations
        foreach (var target in mult.Targets)
        {
            var destLoc = _semantic.GetStorageLocation(target.Symbol);
            if (!destLoc.HasValue) continue;

            int roundingMode = target.IsRounded ? 1 : 0;

            if (mult.Operand is BoundLiteralExpression lit && lit.Value is decimal d)
            {
                block.Instructions.Add(new IrPicMultiplyLiteral(
                    d, destLoc.Value, destLoc.Value, roundingMode));
            }
            else if (mult.Operand is BoundIdentifierExpression opId)
            {
                var opLoc = _semantic.GetStorageLocation(opId.Symbol);
                if (opLoc.HasValue)
                {
                    block.Instructions.Add(new IrPicMultiply(
                        opLoc.Value, destLoc.Value, destLoc.Value, roundingMode));
                }
            }
        }

        // ON SIZE ERROR / NOT ON SIZE ERROR conditional execution
        if (mult.OnSizeError.Count > 0 || mult.NotOnSizeError.Count > 0)
        {
            var sizeErrorBlock = method.CreateBlock("size.error");
            var notSizeErrorBlock = method.CreateBlock("not.size.error");
            var doneBlock = method.CreateBlock("size.done");

            // Load real ArithmeticStatus.SizeError from the method's status local
            var condVal = _valueFactory.Next(IrPrimitiveType.Bool);
            block.Instructions.Add(new IrLoadSizeError(condVal));
            block.Instructions.Add(new IrBranchIfFalse(condVal, notSizeErrorBlock));

            // ON SIZE ERROR block
            method.Blocks.Add(sizeErrorBlock);
            var seCurrent = sizeErrorBlock;
            foreach (var stmt in mult.OnSizeError)
                seCurrent = LowerStatement(stmt, method, seCurrent);
            seCurrent.Instructions.Add(new IrJump(doneBlock));

            // NOT ON SIZE ERROR block
            method.Blocks.Add(notSizeErrorBlock);
            var nseCurrent = notSizeErrorBlock;
            foreach (var stmt in mult.NotOnSizeError)
                nseCurrent = LowerStatement(stmt, method, nseCurrent);
            nseCurrent.Instructions.Add(new IrJump(doneBlock));

            method.Blocks.Add(doneBlock);
            return doneBlock;
        }

        return block;
    }

    // ── SUBTRACT ──

    private IrBasicBlock LowerSubtract(BoundSubtractStatement sub, IrMethod method, IrBasicBlock block)
    {
        // One ArithmeticStatus per statement — init once, sticky across all targets
        block.Instructions.Add(new IrInitArithmeticStatus());

        // SUBTRACT A B C FROM T1 T2 → for each target, subtract each operand in sequence
        // T1 = T1 - A - B - C, T2 = T2 - A - B - C
        foreach (var target in sub.Targets)
        {
            var destLoc = _semantic.GetStorageLocation(target.Symbol);
            if (!destLoc.HasValue) continue;

            int roundingMode = target.IsRounded ? 1 : 0;

            foreach (var operand in sub.Operands)
            {
                if (operand is BoundLiteralExpression lit && lit.Value is decimal d)
                {
                    block.Instructions.Add(new IrPicSubtractLiteral(
                        destLoc.Value, d, roundingMode));
                }
                else if (operand is BoundIdentifierExpression opId)
                {
                    var opLoc = _semantic.GetStorageLocation(opId.Symbol);
                    if (opLoc.HasValue)
                    {
                        block.Instructions.Add(new IrPicSubtract(
                            opLoc.Value, destLoc.Value, roundingMode));
                    }
                }
            }
        }

        // ON SIZE ERROR / NOT ON SIZE ERROR
        if (sub.OnSizeError.Count > 0 || sub.NotOnSizeError.Count > 0)
        {
            var sizeErrorBlock = method.CreateBlock("size.error");
            var notSizeErrorBlock = method.CreateBlock("not.size.error");
            var doneBlock = method.CreateBlock("size.done");

            var condVal = _valueFactory.Next(IrPrimitiveType.Bool);
            block.Instructions.Add(new IrLoadSizeError(condVal));
            block.Instructions.Add(new IrBranchIfFalse(condVal, notSizeErrorBlock));

            method.Blocks.Add(sizeErrorBlock);
            var seCurrent = sizeErrorBlock;
            foreach (var stmt in sub.OnSizeError)
                seCurrent = LowerStatement(stmt, method, seCurrent);
            seCurrent.Instructions.Add(new IrJump(doneBlock));

            method.Blocks.Add(notSizeErrorBlock);
            var nseCurrent = notSizeErrorBlock;
            foreach (var stmt in sub.NotOnSizeError)
                nseCurrent = LowerStatement(stmt, method, nseCurrent);
            nseCurrent.Instructions.Add(new IrJump(doneBlock));

            method.Blocks.Add(doneBlock);
            return doneBlock;
        }

        return block;
    }

    // ── DIVIDE ──

    private IrBasicBlock LowerDivide(BoundDivideStatement div, IrMethod method, IrBasicBlock block)
    {
        block.Instructions.Add(new IrInitArithmeticStatus());

        foreach (var target in div.Targets)
        {
            var destLoc = _semantic.GetStorageLocation(target.Symbol);
            if (!destLoc.HasValue) continue;

            int roundingMode = target.IsRounded ? 1 : 0;

            if (div.Dividend != null)
            {
                // GIVING form: dest = dividend / divisor
                // For now, handle literal divisor or identifier divisor
                if (div.Divisor is BoundLiteralExpression litDiv && litDiv.Value is decimal d)
                {
                    // DivideLiteral: literal is the divisor, other is the dividend
                    if (div.Dividend is BoundIdentifierExpression dividendId)
                    {
                        var dividendLoc = _semantic.GetStorageLocation(dividendId.Symbol);
                        if (dividendLoc.HasValue)
                        {
                            block.Instructions.Add(new IrPicDivideLiteral(
                                d, dividendLoc.Value, destLoc.Value, roundingMode));
                        }
                    }
                }
                else if (div.Divisor is BoundIdentifierExpression divisorId)
                {
                    var divisorLoc = _semantic.GetStorageLocation(divisorId.Symbol);
                    if (divisorLoc.HasValue)
                    {
                        if (div.Dividend is BoundIdentifierExpression dividendId)
                        {
                            var dividendLoc = _semantic.GetStorageLocation(dividendId.Symbol);
                            if (dividendLoc.HasValue)
                            {
                                block.Instructions.Add(new IrPicDivide(
                                    dividendLoc.Value, divisorLoc.Value, destLoc.Value, roundingMode));
                            }
                        }
                        else if (div.Dividend is BoundLiteralExpression litDividend && litDividend.Value is decimal dv)
                        {
                            // dividend is literal, divisor is field — use DivideLiteral with swapped semantics
                            // Actually we need DivideNumeric with the field as right operand
                            // For now emit DivideLiteral where literal=dv, other=divisor, dest=target
                            block.Instructions.Add(new IrPicDivideLiteral(
                                dv, divisorLoc.Value, destLoc.Value, roundingMode));
                        }
                    }
                }
            }
            else
            {
                // INTO form without GIVING: dest = dest / divisor
                if (div.Divisor is BoundLiteralExpression litDiv && litDiv.Value is decimal d)
                {
                    block.Instructions.Add(new IrPicDivideLiteral(
                        d, destLoc.Value, destLoc.Value, roundingMode));
                }
                else if (div.Divisor is BoundIdentifierExpression divisorId)
                {
                    var divisorLoc = _semantic.GetStorageLocation(divisorId.Symbol);
                    if (divisorLoc.HasValue)
                    {
                        block.Instructions.Add(new IrPicDivide(
                            destLoc.Value, divisorLoc.Value, destLoc.Value, roundingMode));
                    }
                }
            }
        }

        // ON SIZE ERROR / NOT ON SIZE ERROR
        if (div.OnSizeError.Count > 0 || div.NotOnSizeError.Count > 0)
        {
            var sizeErrorBlock = method.CreateBlock("size.error");
            var notSizeErrorBlock = method.CreateBlock("not.size.error");
            var doneBlock = method.CreateBlock("size.done");

            var condVal = _valueFactory.Next(IrPrimitiveType.Bool);
            block.Instructions.Add(new IrLoadSizeError(condVal));
            block.Instructions.Add(new IrBranchIfFalse(condVal, notSizeErrorBlock));

            method.Blocks.Add(sizeErrorBlock);
            var seCurrent = sizeErrorBlock;
            foreach (var stmt in div.OnSizeError)
                seCurrent = LowerStatement(stmt, method, seCurrent);
            seCurrent.Instructions.Add(new IrJump(doneBlock));

            method.Blocks.Add(notSizeErrorBlock);
            var nseCurrent = notSizeErrorBlock;
            foreach (var stmt in div.NotOnSizeError)
                nseCurrent = LowerStatement(stmt, method, nseCurrent);
            nseCurrent.Instructions.Add(new IrJump(doneBlock));

            method.Blocks.Add(doneBlock);
            return doneBlock;
        }

        return block;
    }

    // ── ADD ──

    private void LowerAdd(BoundAddStatement add, IrBasicBlock block)
    {
        block.Instructions.Add(new IrInitArithmeticStatus());

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

    private IrBasicBlock LowerIf(BoundIfStatement iff, IrMethod method, IrBasicBlock current)
    {
        var condVal = _valueFactory.Next(IrPrimitiveType.Bool);
        LowerCondition(iff.Condition, condVal, current);

        var thenBlock = method.CreateBlock("if.then");
        var elseBlock = iff.ElseStatements is { Count: > 0 }
            ? method.CreateBlock("if.else")
            : null;
        var joinBlock = method.CreateBlock("if.join");

        // Branch: if condition false → else (or join if no else)
        current.Instructions.Add(new IrBranchIfFalse(condVal,
            elseBlock ?? joinBlock));

        // THEN block
        method.Blocks.Add(thenBlock);
        var thenCurrent = thenBlock;
        foreach (var stmt in iff.ThenStatements)
            thenCurrent = LowerStatement(stmt, method, thenCurrent);
        thenCurrent.Instructions.Add(new IrJump(joinBlock));

        // ELSE block (optional)
        if (elseBlock != null)
        {
            method.Blocks.Add(elseBlock);
            var elseCurrent = elseBlock;
            foreach (var stmt in iff.ElseStatements!)
                elseCurrent = LowerStatement(stmt, method, elseCurrent);
            elseCurrent.Instructions.Add(new IrJump(joinBlock));
        }

        // Subsequent statements go to join block
        method.Blocks.Add(joinBlock);
        return joinBlock;
    }

    private void LowerCondition(BoundExpression cond, IrValue result, IrBasicBlock block)
    {
        if (cond is BoundBinaryExpression binCond)
        {
            var leftSym = (binCond.Left as BoundIdentifierExpression)?.Symbol;
            var rightSym = (binCond.Right as BoundIdentifierExpression)?.Symbol;

            if (leftSym != null)
            {
                var leftLoc = _semantic.GetStorageLocation(leftSym);
                if (leftLoc.HasValue)
                {
                    var leftCat = leftLoc.Value.Pic.Category;

                    // identifier vs string literal (alphanumeric comparison)
                    if (binCond.Right is BoundLiteralExpression litStr &&
                        litStr.Value is string s && leftCat.IsAlphanumericLike())
                    {
                        block.Instructions.Add(new IrStringCompareLiteral(
                            leftLoc.Value, s, result,
                            (int)binCond.OperatorKind));
                        return;
                    }

                    // identifier vs identifier
                    if (rightSym != null)
                    {
                        var rightLoc = _semantic.GetStorageLocation(rightSym);
                        if (rightLoc.HasValue)
                        {
                            block.Instructions.Add(new IrPicCompare(
                                leftLoc.Value, rightLoc.Value, result,
                                (int)binCond.OperatorKind));
                            return;
                        }
                    }
                    // identifier vs numeric literal
                    else if (binCond.Right is BoundLiteralExpression litRight
                             && litRight.Value is decimal d)
                    {
                        block.Instructions.Add(new IrPicCompareLiteral(
                            leftLoc.Value, d, result,
                            (int)binCond.OperatorKind));
                        return;
                    }
                }
            }
        }

        // Fallback: always true
        block.Instructions.Add(new IrSetBool(result, true));
    }

    // ── GO TO ──

    private void LowerGoTo(BoundGoToStatement gt, IrBasicBlock block)
    {
        if (_paragraphIndices.TryGetValue(gt.Target.Name, out int targetIndex))
        {
            block.Instructions.Add(new IrReturnConst(targetIndex));
        }
    }
}
