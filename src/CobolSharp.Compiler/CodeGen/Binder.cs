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

    /// <summary>
    /// The block representing the start of the next sentence.
    /// Set during sentence lowering; NEXT SENTENCE emits a jump to this block.
    /// </summary>
    private IrBasicBlock? _currentSentenceEnd;

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

        // Phase 4: Lower bound statements into IR, preserving sentence structure
        foreach (var para in boundProgram.Paragraphs)
        {
            if (_paragraphMethods.TryGetValue(para.Symbol.Name, out var method))
            {
                int myIndex = _paragraphIndices[para.Symbol.Name];
                var block = method.Blocks[0];

                for (int si = 0; si < para.Sentences.Count; si++)
                {
                    var sentence = para.Sentences[si];

                    // Create the block that represents the start of the next sentence.
                    // NEXT SENTENCE jumps here; normal flow falls through.
                    var sentenceEnd = new IrBasicBlock($"{para.Symbol.Name}_sent{si}_end");
                    _currentSentenceEnd = sentenceEnd;

                    foreach (var stmt in sentence.Statements)
                        block = LowerStatement(stmt, method, block);

                    // Fall through into the sentence-end block
                    block.Instructions.Add(new IrJump(sentenceEnd));
                    method.Blocks.Add(sentenceEnd);
                    block = sentenceEnd;
                }

                _currentSentenceEnd = null;

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

        // Initialize file manager
        block.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.Init", Array.Empty<IrValue>()));

        // Register file handlers at startup for each SELECT
        foreach (var fileSym in _semantic.Symbols.Program.GlobalScope.GetAllSymbols<FileSymbol>())
        {
            // Resolve external path: literal ASSIGN target uses the target value,
            // identifier ASSIGN target (like NIST's XXXXX055) uses the COBOL file name
            string externalName = (fileSym.AssignIsLiteral && fileSym.AssignTarget != null)
                ? fileSym.AssignTarget
                : fileSym.Name;
            string externalPath = FileRuntime.ResolveHostPath(externalName);

            int recordLength = fileSym.RecordLength;
            if (recordLength == 0) recordLength = 132; // Default for print files

            // Line-sequential for SEQUENTIAL org (default) and LINE SEQUENTIAL
            bool lineSequential = fileSym.Organization == null
                || fileSym.Organization == "SEQUENTIAL"
                || fileSym.Organization == "LINE SEQUENTIAL";

            var nameVal = _valueFactory.Next(IrPrimitiveType.String);
            var pathVal = _valueFactory.Next(IrPrimitiveType.String);
            var recLenVal = _valueFactory.Next(IrPrimitiveType.Int32);
            var lineSeqVal = _valueFactory.Next(IrPrimitiveType.Bool);
            block.Instructions.Add(new IrLoadConst(nameVal, fileSym.Name));
            block.Instructions.Add(new IrLoadConst(pathVal, externalPath));
            block.Instructions.Add(new IrLoadConst(recLenVal, recordLength));
            block.Instructions.Add(new IrLoadConst(lineSeqVal, lineSequential));
            block.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.RegisterFileHandler",
                new[] { nameVal, pathVal, recLenVal, lineSeqVal }));
        }

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
                return LowerPerform(perf, method, block);
            case BoundEvaluateStatement eval:
                return LowerEvaluate(eval, method, block);
            case BoundWriteStatement wr:
                LowerWrite(wr, block);
                break;
            case BoundIfStatement iff:
                return LowerIf(iff, method, block);
            case BoundMultiplyStatement mult:
                return LowerMultiply(mult, method, block);
            case BoundAddStatement add:
                return LowerAdd(add, method, block);
            case BoundGoToStatement gt:
                LowerGoTo(gt, block);
                break;
            case BoundStopStatement:
                block.Instructions.Add(new IrReturnConst(-1));
                break;
            case BoundExitStatement:
                // EXIT is a no-op; fall-through return handles it
                break;
            case BoundNextSentenceStatement:
                return LowerNextSentence(method, block);
            case BoundOpenStatement open:
                LowerOpen(open, block);
                break;
            case BoundCloseStatement close:
                LowerClose(close, block);
                break;
            case BoundReadStatement read:
                return LowerRead(read, method, block);
            case BoundRewriteStatement rw:
                LowerRewrite(rw, block);
                break;
            case BoundInitializeStatement init:
                LowerInitialize(init, block);
                break;
            case BoundAcceptStatement acc:
                LowerAccept(acc, block);
                break;
            case BoundInspectStatement insp:
                LowerInspect(insp, block);
                break;
            case BoundSetConditionStatement setCond:
                LowerSetCondition(setCond, block);
                break;
            case BoundSetIndexStatement setIdx:
                LowerSetIndex(setIdx, block);
                break;
            case BoundSubtractStatement sub:
                return LowerSubtract(sub, method, block);
            case BoundDivideStatement div:
                return LowerDivide(div, method, block);
            case BoundComputeStatement comp:
                return LowerCompute(comp, method, block);
        }
        return block;
    }

    // ── DISPLAY ──

    private void LowerDisplay(BoundDisplayStatement disp, IrBasicBlock block)
    {
        var operands = new List<IR.DisplayOperand>();
        foreach (var op in disp.Operands)
        {
            if (op is BoundFigurativeExpression fig)
            {
                // In DISPLAY context, figuratives display a single character
                string figStr = ((Runtime.FigurativeKind)fig.FigurativeKind) switch
                {
                    Runtime.FigurativeKind.Space => " ",
                    Runtime.FigurativeKind.Zero => "0",
                    Runtime.FigurativeKind.HighValue => "\xFF",
                    Runtime.FigurativeKind.LowValue => "\x00",
                    Runtime.FigurativeKind.Quote => "\"",
                    _ => fig.AllLiteral ?? " "
                };
                operands.Add(new IR.DisplayLiteralOperand(figStr));
            }
            else if (op is BoundLiteralExpression lit && lit.Value is string s)
            {
                operands.Add(new IR.DisplayLiteralOperand(s));
            }
            else if (op is BoundLiteralExpression numLit && numLit.Value is decimal d)
            {
                operands.Add(new IR.DisplayLiteralOperand(
                    d.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }
            else if (op is BoundIdentifierExpression id)
            {
                var loc = _semantic.GetStorageLocation(id.Symbol);
                if (loc.HasValue)
                    operands.Add(new IR.DisplayFieldOperand(loc.Value));
                else
                    operands.Add(new IR.DisplayLiteralOperand($"[{id.Symbol.Name}]"));
            }
            else
            {
                operands.Add(new IR.DisplayLiteralOperand(op.ToString() ?? ""));
            }
        }

        block.Instructions.Add(new IR.IrPicDisplay(operands));
    }

    // ── MOVE ──

    private void LowerMove(BoundMoveStatement mv, IrBasicBlock block)
    {
        // Handle MOVE figurative-constant TO identifier
        if (mv.Source is BoundFigurativeExpression fig)
        {
            foreach (var t in mv.Targets)
            {
                if (t is not BoundIdentifierExpression id) continue;
                var loc = _semantic.GetStorageLocation(id.Symbol);
                if (!loc.HasValue) continue;

                if (fig.AllLiteral != null)
                {
                    // ALL "pattern" — repeat pattern to fill field
                    block.Instructions.Add(new IrMoveAllLiteral(loc.Value, fig.AllLiteral));
                }
                else
                {
                    // Named figurative (SPACE, ZERO, HIGH-VALUE, etc.)
                    block.Instructions.Add(new IrMoveFigurative(loc.Value, fig.FigurativeKind));
                }
            }
            return;
        }

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

    private IrBasicBlock LowerPerform(BoundPerformStatement perf, IrMethod method, IrBasicBlock block)
    {
        // PERFORM VARYING: recursive loop structure with optional AFTER nesting
        if (perf.Varying != null)
        {
            return LowerPerformVarying(perf.Varying, perf, method, block);
        }

        // PERFORM UNTIL (no VARYING): simple condition loop
        if (perf.UntilCondition != null)
        {
            var loopStart = method.CreateBlock("perf.until.start");
            var loopBody = method.CreateBlock("perf.until.body");
            var loopEnd = method.CreateBlock("perf.until.end");

            block.Instructions.Add(new IrJump(loopStart));

            method.Blocks.Add(loopStart);
            var condVal = _valueFactory.Next(IrPrimitiveType.Bool);
            LowerCondition(perf.UntilCondition, condVal, loopStart);
            loopStart.Instructions.Add(new IrBranchIfFalse(condVal, loopBody));
            loopStart.Instructions.Add(new IrJump(loopEnd));

            method.Blocks.Add(loopBody);
            var bodyCurrent = LowerPerformBody(perf, method, loopBody);
            bodyCurrent.Instructions.Add(new IrJump(loopStart));

            method.Blocks.Add(loopEnd);
            return loopEnd;
        }

        // Simple PERFORM (no UNTIL/VARYING): just call paragraphs
        LowerPerformSimple(perf, block);
        return block;
    }

    /// <summary>
    /// Recursive VARYING lowering. For each level:
    ///   1. Initialize index from FROM value
    ///   2. Loop: test UNTIL; if true exit; execute body (or inner VARYING); increment; repeat
    /// AFTER clauses become nested inner levels via v.Next.
    /// </summary>
    private IrBasicBlock LowerPerformVarying(BoundPerformVarying v, BoundPerformStatement perf,
        IrMethod method, IrBasicBlock block)
    {
        var indexLoc = _semantic.GetStorageLocation(v.Index);
        if (!indexLoc.HasValue) return block;

        // 1. Initialize: MOVE initial TO index
        EmitVaryingMove(v.Initial, indexLoc.Value, block);

        // 2. Loop structure
        var loopStart = method.CreateBlock("vary.start");
        var loopBody = method.CreateBlock("vary.body");
        var loopEnd = method.CreateBlock("vary.end");

        block.Instructions.Add(new IrJump(loopStart));

        // Test UNTIL (top-tested)
        method.Blocks.Add(loopStart);
        var condVal = _valueFactory.Next(IrPrimitiveType.Bool);
        LowerCondition(v.UntilCondition, condVal, loopStart);
        loopStart.Instructions.Add(new IrBranchIfFalse(condVal, loopBody));
        loopStart.Instructions.Add(new IrJump(loopEnd));

        // Body
        method.Blocks.Add(loopBody);
        IrBasicBlock bodyCurrent;

        if (v.Next != null)
        {
            // Nested AFTER: recurse into inner level
            bodyCurrent = LowerPerformVarying(v.Next, perf, method, loopBody);
        }
        else
        {
            // Innermost level: execute the actual body
            bodyCurrent = LowerPerformBody(perf, method, loopBody);
        }

        // Increment: ADD step TO index
        EmitVaryingAdd(v.Step, indexLoc.Value, bodyCurrent);

        bodyCurrent.Instructions.Add(new IrJump(loopStart));

        method.Blocks.Add(loopEnd);
        return loopEnd;
    }

    private void EmitVaryingMove(BoundExpression source, StorageLocation dest, IrBasicBlock block)
    {
        if (source is BoundLiteralExpression lit && lit.Value is decimal d)
        {
            block.Instructions.Add(new IrPicMoveLiteralNumeric(dest, d, 0));
        }
        else if (source is BoundIdentifierExpression id)
        {
            var srcLoc = _semantic.GetStorageLocation(id.Symbol);
            if (srcLoc.HasValue)
                block.Instructions.Add(new IrPicMove(srcLoc.Value, dest, 0));
        }
    }

    private void EmitVaryingAdd(BoundExpression step, StorageLocation indexLoc, IrBasicBlock block)
    {
        block.Instructions.Add(new IrInitArithmeticStatus());
        if (step is BoundLiteralExpression litStep && litStep.Value is decimal dStep)
        {
            block.Instructions.Add(new IrPicAddLiteral(indexLoc, dStep, 0));
        }
        else if (step is BoundIdentifierExpression idStep)
        {
            var stepLoc = _semantic.GetStorageLocation(idStep.Symbol);
            if (stepLoc.HasValue)
                block.Instructions.Add(new IrPicAdd(stepLoc.Value, indexLoc, 0));
        }
    }

    private IrBasicBlock LowerPerformBody(BoundPerformStatement perf, IrMethod method, IrBasicBlock block)
    {
        // Inline statements
        if (perf.InlineStatements is { Count: > 0 })
        {
            var current = block;
            foreach (var stmt in perf.InlineStatements)
                current = LowerStatement(stmt, method, current);
            return current;
        }

        // Target paragraph(s)
        if (perf.Target != null)
        {
            LowerPerformSimple(perf, block);
        }
        return block;
    }

    private void LowerPerformSimple(BoundPerformStatement perf, IrBasicBlock block)
    {
        if (perf.Target == null) return;
        if (!_paragraphIndices.TryGetValue(perf.Target.Name, out int startIdx))
            return;

        int endIdx = startIdx;
        if (perf.ThruTarget != null &&
            _paragraphIndices.TryGetValue(perf.ThruTarget.Name, out int thruIdx))
            endIdx = thruIdx;

        int times = perf.Times > 0 ? perf.Times : 1;

        if (startIdx == endIdx)
        {
            var paraName = _paragraphsByIndex[startIdx];
            if (_paragraphMethods.TryGetValue(paraName, out var paraMethod))
            {
                for (int t = 0; t < times; t++)
                    block.Instructions.Add(new IrPerform(paraMethod));
            }
        }
        else
        {
            var methods = new List<IrMethod>();
            for (int i = startIdx; i <= endIdx; i++)
            {
                var paraName = _paragraphsByIndex[i];
                if (_paragraphMethods.TryGetValue(paraName, out var paraMethod))
                    methods.Add(paraMethod);
                else
                    methods.Add(null!);
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
            if (wr.AdvancingLines.HasValue)
            {
                // WRITE AFTER/BEFORE ADVANCING: print-control path
                block.Instructions.Add(new IrWriteAfterAdvancing(
                    fileName, recordLoc.Value, wr.AdvancingLines.Value));
            }
            else
            {
                // Plain WRITE: data record path via handler.Write
                block.Instructions.Add(new IrWriteRecordFromStorage(fileName, recordLoc.Value));
            }
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

        // Update FILE STATUS if declared
        if (wr.File != null)
            EmitFileStatus(wr.File, block);
    }

    // ── OPEN ──

    private void LowerOpen(BoundOpenStatement open, IrBasicBlock block)
    {
        string runtimeMethod = open.Mode switch
        {
            OpenMode.Input => "FileRuntime.OpenInput",
            OpenMode.Output => "FileRuntime.OpenOutput",
            OpenMode.IO => "FileRuntime.OpenIO",
            OpenMode.Extend => "FileRuntime.OpenExtend",
            _ => "FileRuntime.OpenOutput"
        };

        foreach (var file in open.Files)
        {
            string cobolName = file.Name;
            var fnVal = _valueFactory.Next(IrPrimitiveType.String);
            block.Instructions.Add(new IrLoadConst(fnVal, cobolName));
            block.Instructions.Add(new IrRuntimeCall(null, runtimeMethod, new[] { fnVal }));

            // Update FILE STATUS if declared
            EmitFileStatus(file, block);
        }
    }

    // ── CLOSE ──

    private void LowerClose(BoundCloseStatement close, IrBasicBlock block)
    {
        foreach (var file in close.Files)
        {
            string cobolName = file.Name;
            var fnVal = _valueFactory.Next(IrPrimitiveType.String);
            block.Instructions.Add(new IrLoadConst(fnVal, cobolName));
            block.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.CloseFile", new[] { fnVal }));

            EmitFileStatus(file, block);
        }
    }

    // ── READ ──

    private IrBasicBlock LowerRead(BoundReadStatement read, IrMethod method, IrBasicBlock block)
    {
        string cobolName = read.File.Name;

        // Read into the FD record buffer
        var recordSym = read.File.Record;
        if (recordSym != null)
        {
            var recordLoc = _semantic.GetStorageLocation(recordSym);
            if (recordLoc.HasValue)
            {
                block.Instructions.Add(new IrReadRecordToStorage(cobolName, recordLoc.Value));
            }
        }

        // Update FILE STATUS
        EmitFileStatus(read.File, block);

        // If INTO specified, MOVE FD record to INTO target
        if (read.Into != null && recordSym != null)
        {
            var srcLoc = _semantic.GetStorageLocation(recordSym);
            var dstLoc = _semantic.GetStorageLocation(read.Into);
            if (srcLoc.HasValue && dstLoc.HasValue)
            {
                block.Instructions.Add(new IrPicMove(srcLoc.Value, dstLoc.Value));
            }
        }

        // AT END / NOT AT END branching
        if (read.AtEnd.Count > 0 || read.NotAtEnd.Count > 0)
        {
            var atEndResult = _valueFactory.Next(IrPrimitiveType.Bool);
            block.Instructions.Add(new IrCheckFileAtEnd(cobolName, atEndResult));

            var atEndBlock = method.CreateBlock("read_at_end");
            var notAtEndBlock = method.CreateBlock("read_not_at_end");
            var afterBlock = method.CreateBlock("read_after");

            block.Instructions.Add(new IrBranchIfFalse(atEndResult, notAtEndBlock));
            block.Instructions.Add(new IrJump(atEndBlock));

            // AT END
            method.Blocks.Add(atEndBlock);
            var current = atEndBlock;
            foreach (var stmt in read.AtEnd)
                current = LowerStatement(stmt, method, current);
            current.Instructions.Add(new IrJump(afterBlock));

            // NOT AT END
            method.Blocks.Add(notAtEndBlock);
            current = notAtEndBlock;
            foreach (var stmt in read.NotAtEnd)
                current = LowerStatement(stmt, method, current);
            current.Instructions.Add(new IrJump(afterBlock));

            method.Blocks.Add(afterBlock);
            return afterBlock;
        }

        return block;
    }

    /// <summary>
    /// Emit FILE STATUS update: after each file operation, store the 2-char
    /// status code into the FILE STATUS variable if one was declared.
    /// </summary>
    private void EmitFileStatus(FileSymbol file, IrBasicBlock block)
    {
        if (file.FileStatus == null) return;

        var statusSym = _semantic.ResolveData(file.FileStatus);
        if (statusSym == null) return;

        var statusLoc = _semantic.GetStorageLocation(statusSym);
        if (!statusLoc.HasValue) return;

        block.Instructions.Add(new IrStoreFileStatus(file.Name, statusLoc.Value));
    }

    // ── REWRITE ──

    private void LowerRewrite(BoundRewriteStatement rw, IrBasicBlock block)
    {
        string cobolName = rw.File.Name;
        var recordLoc = _semantic.GetStorageLocation(rw.Record);
        if (recordLoc.HasValue)
        {
            block.Instructions.Add(new IrRewriteRecordFromStorage(cobolName, recordLoc.Value));
        }
        EmitFileStatus(rw.File, block);
    }

    // ── INITIALIZE ──

    private void LowerInitialize(BoundInitializeStatement stmt, IrBasicBlock block)
    {
        foreach (var target in stmt.Targets)
            InitializeDataItem(target, stmt, block);
    }

    private void InitializeDataItem(DataSymbol item, BoundInitializeStatement stmt, IrBasicBlock block)
    {
        if (item.IsGroup)
        {
            // Recurse into children (skipping REDEFINES items — they share storage)
            foreach (var child in item.Children)
            {
                if (child.Redefines != null) continue;
                InitializeDataItem(child, stmt, block);
            }
            return;
        }

        // Elementary item: check for category replacement, then default
        var loc = _semantic.GetStorageLocation(item);
        if (!loc.HasValue) return;

        var category = ClassifyInitializeCategory(loc.Value.Pic.Category);

        // Check for matching category replacement
        foreach (var repl in stmt.CategoryReplacements)
        {
            if (repl.Category == category)
            {
                EmitInitializeAssignment(loc.Value, repl.Value, block);
                return;
            }
        }

        // Default: numeric → zero, everything else → spaces
        if (category == InitializeCategory.Numeric || category == InitializeCategory.NumericEdited)
        {
            block.Instructions.Add(new IrPicMoveLiteralNumeric(loc.Value, 0m));
        }
        else
        {
            block.Instructions.Add(new IrMoveFigurative(loc.Value, (int)FigurativeKind.Space));
        }
    }

    private InitializeCategory ClassifyInitializeCategory(CobolCategory cat)
    {
        return cat switch
        {
            CobolCategory.Numeric => InitializeCategory.Numeric,
            CobolCategory.NumericEdited => InitializeCategory.NumericEdited,
            CobolCategory.AlphanumericEdited => InitializeCategory.AlphanumericEdited,
            _ => InitializeCategory.Alphanumeric
        };
    }

    private void EmitInitializeAssignment(StorageLocation dest, BoundExpression value, IrBasicBlock block)
    {
        if (value is BoundLiteralExpression lit)
        {
            if (lit.Value is decimal d)
            {
                if (dest.Pic.Category.IsNumericLike())
                    block.Instructions.Add(new IrPicMoveLiteralNumeric(dest, d));
                else
                    block.Instructions.Add(new IrMoveStringToField(dest,
                        d.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }
            else if (lit.Value is string s)
            {
                block.Instructions.Add(new IrMoveStringToField(dest, s));
            }
        }
        else if (value is BoundIdentifierExpression id)
        {
            var srcLoc = _semantic.GetStorageLocation(id.Symbol);
            if (srcLoc.HasValue)
                block.Instructions.Add(new IrPicMove(srcLoc.Value, dest));
        }
    }

    // ── ACCEPT ──

    private void LowerAccept(BoundAcceptStatement stmt, IrBasicBlock block)
    {
        var loc = _semantic.GetStorageLocation(stmt.Target);
        if (!loc.HasValue) return;
        block.Instructions.Add(new IrAccept(loc.Value, stmt.Source));
    }

    // ── INSPECT ──

    private void LowerInspect(BoundInspectStatement stmt, IrBasicBlock block)
    {
        var targetLoc = _semantic.GetStorageLocation(stmt.Target);
        if (!targetLoc.HasValue) return;

        foreach (var t in stmt.Tallying)
        {
            var counterLoc = _semantic.GetStorageLocation(t.Counter);
            if (!counterLoc.HasValue) continue;

            block.Instructions.Add(new IrInspectTally(
                targetLoc.Value, counterLoc.Value, t.Kind, t.Pattern,
                t.Region.BeforePattern, t.Region.BeforeInitial,
                t.Region.AfterPattern, t.Region.AfterInitial));
        }

        foreach (var r in stmt.Replacing)
        {
            block.Instructions.Add(new IrInspectReplace(
                targetLoc.Value, r.Kind, r.Pattern, r.Replacement,
                r.Region.BeforePattern, r.Region.BeforeInitial,
                r.Region.AfterPattern, r.Region.AfterInitial));
        }

        if (stmt.Converting != null)
        {
            block.Instructions.Add(new IrInspectConvert(
                targetLoc.Value, stmt.Converting.FromSet, stmt.Converting.ToSet,
                stmt.Converting.Region.BeforePattern, stmt.Converting.Region.BeforeInitial,
                stmt.Converting.Region.AfterPattern, stmt.Converting.Region.AfterInitial));
        }
    }

    // ── SET ──

    private void LowerSetCondition(BoundSetConditionStatement stmt, IrBasicBlock block)
    {
        var parentSym = stmt.Condition.ParentDataItem;
        var parentLoc = _semantic.GetStorageLocation(parentSym);
        if (!parentLoc.HasValue) return;

        if (stmt.SetToTrue)
        {
            // SET condition TO TRUE: move the first defining value into the parent
            var ranges = stmt.Condition.ValueRanges;
            if (ranges.Count == 0) return;

            var firstVal = ranges[0].From;
            if (firstVal is decimal d)
                block.Instructions.Add(new IrPicMoveLiteralNumeric(parentLoc.Value, d));
            else if (firstVal is string s)
                block.Instructions.Add(new IrMoveStringToField(parentLoc.Value, s));
        }
        else
        {
            // SET condition TO FALSE: move a value that doesn't match any true value
            var parentCat = parentLoc.Value.Pic.Category;
            if (parentCat.IsNumericLike())
            {
                // Try 0 first; if 0 is a true value, try others
                var trueVals = stmt.Condition.ValueRanges.Select(r => r.From).ToList();
                decimal falseVal = 0m;
                foreach (var candidate in new[] { 0m, 1m, -1m, 99m })
                {
                    if (!trueVals.Any(v => v is decimal d && d == candidate))
                    {
                        falseVal = candidate;
                        break;
                    }
                }
                block.Instructions.Add(new IrPicMoveLiteralNumeric(parentLoc.Value, falseVal));
            }
            else
            {
                // Alphanumeric: try spaces first
                block.Instructions.Add(new IrMoveFigurative(parentLoc.Value, (int)FigurativeKind.Space));
            }
        }
    }

    private void LowerSetIndex(BoundSetIndexStatement stmt, IrBasicBlock block)
    {
        var targetLoc = _semantic.GetStorageLocation(stmt.Target);
        if (!targetLoc.HasValue) return;

        switch (stmt.Operation)
        {
            case SetOperation.Assign:
                // SET identifier TO value — reuse MOVE machinery
                if (stmt.Value is BoundLiteralExpression lit)
                {
                    if (lit.Value is decimal d)
                    {
                        if (targetLoc.Value.Pic.Category.IsNumericLike())
                            block.Instructions.Add(new IrPicMoveLiteralNumeric(targetLoc.Value, d));
                        else
                            block.Instructions.Add(new IrMoveStringToField(targetLoc.Value,
                                d.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                    }
                    else if (lit.Value is string s)
                    {
                        block.Instructions.Add(new IrMoveStringToField(targetLoc.Value, s));
                    }
                }
                else if (stmt.Value is BoundIdentifierExpression id)
                {
                    var srcLoc = _semantic.GetStorageLocation(id.Symbol);
                    if (srcLoc.HasValue)
                        block.Instructions.Add(new IrPicMove(srcLoc.Value, targetLoc.Value));
                }
                break;

            case SetOperation.UpBy:
                // SET identifier UP BY n → ADD n TO identifier
                if (stmt.Value is BoundLiteralExpression upLit && upLit.Value is decimal upVal)
                    block.Instructions.Add(new IrPicAddLiteral(targetLoc.Value, upVal));
                break;

            case SetOperation.DownBy:
                // SET identifier DOWN BY n → SUBTRACT n FROM identifier
                if (stmt.Value is BoundLiteralExpression downLit && downLit.Value is decimal downVal)
                    block.Instructions.Add(new IrPicSubtractLiteral(targetLoc.Value, downVal));
                break;
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

        return LowerSizeError(mult.SizeError, method, block);
    }

    // ── SUBTRACT ──

    private IrBasicBlock LowerSubtract(BoundSubtractStatement sub, IrMethod method, IrBasicBlock block)
    {
        // One ArithmeticStatus per statement — init once, sticky across all targets
        block.Instructions.Add(new IrInitArithmeticStatus());

        // COBOL spec: "All operands preceding FROM are added together, and this sum
        // is subtracted from each identifier following FROM."
        // Step 1: Accumulate all operands into a decimal sum
        var accum = _valueFactory.Next(IrPrimitiveType.Decimal);
        block.Instructions.Add(new IrInitAccumulator(accum));

        foreach (var operand in sub.Operands)
        {
            if (operand is BoundLiteralExpression lit && lit.Value is decimal d)
            {
                block.Instructions.Add(new IrAccumulateLiteral(accum, d));
            }
            else if (operand is BoundIdentifierExpression opId)
            {
                var opLoc = _semantic.GetStorageLocation(opId.Symbol);
                if (opLoc.HasValue)
                {
                    block.Instructions.Add(new IrAccumulateField(accum, opLoc.Value));
                }
            }
        }

        // Step 2: For each target, apply the subtraction
        foreach (var target in sub.Targets)
        {
            var destLoc = _semantic.GetStorageLocation(target.Symbol);
            if (!destLoc.HasValue) continue;

            int roundingMode = target.IsRounded ? 1 : 0;
            if (sub.IsGiving && sub.GivingMinuend != null)
            {
                // GIVING: target = minuend - accumulated
                // Use IrComputeStore with synthetic expression: minuend - accumulated_as_literal
                // Since accumulated is a runtime value, we build: minuend - (sum of operands)
                // Reconstruct the subtraction expression from the bound operands
                BoundExpression sumExpr = sub.Operands[0];
                for (int i = 1; i < sub.Operands.Count; i++)
                {
                    sumExpr = new BoundBinaryExpression(
                        sumExpr, BoundBinaryOperatorKind.Add, sub.Operands[i],
                        CobolCategory.Numeric);
                }
                var subExpr = new BoundBinaryExpression(
                    sub.GivingMinuend, BoundBinaryOperatorKind.Subtract, sumExpr,
                    CobolCategory.Numeric);
                block.Instructions.Add(new IrComputeStore(subExpr, destLoc.Value, roundingMode));
            }
            else
            {
                // FROM: target = target - accumulated
                block.Instructions.Add(new IrSubtractAccumulatedFromTarget(accum, destLoc.Value, roundingMode));
            }
        }

        return LowerSizeError(sub.SizeError, method, block);
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
                // Use COMPUTE expression path — handles all operand combinations
                var divExpr = new BoundBinaryExpression(
                    div.Dividend, BoundBinaryOperatorKind.Divide, div.Divisor,
                    CobolCategory.Numeric);
                block.Instructions.Add(new IrComputeStore(divExpr, destLoc.Value, roundingMode));
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

        // REMAINDER: dividend MOD divisor
        if (div.RemainderTarget != null && div.Dividend != null)
        {
            var remLoc = _semantic.GetStorageLocation(div.RemainderTarget);
            if (remLoc.HasValue)
            {
                var remExpr = new BoundBinaryExpression(
                    div.Dividend, BoundBinaryOperatorKind.Remainder, div.Divisor,
                    CobolCategory.Numeric);
                block.Instructions.Add(new IrComputeStore(remExpr, remLoc.Value, 0));
            }
        }

        return LowerSizeError(div.SizeError, method, block);
    }

    // ── COMPUTE ──

    private IrBasicBlock LowerCompute(BoundComputeStatement comp, IrMethod method, IrBasicBlock block)
    {
        block.Instructions.Add(new IrInitArithmeticStatus());

        // Emit expression evaluation + store for each target
        foreach (var target in comp.Targets)
        {
            var destLoc = _semantic.GetStorageLocation(target.Symbol);
            if (!destLoc.HasValue) continue;

            int roundingMode = target.IsRounded ? 1 : 0;
            block.Instructions.Add(new IrComputeStore(
                comp.Expression, destLoc.Value, roundingMode));
        }

        return LowerSizeError(comp.SizeError, method, block);
    }

    // ── ADD ──

    private IrBasicBlock LowerAdd(BoundAddStatement add, IrMethod method, IrBasicBlock block)
    {
        block.Instructions.Add(new IrInitArithmeticStatus());

        // COBOL spec: "All operands preceding TO are added together, and this sum
        // is added to each identifier following TO."
        // Step 1: Accumulate all operands into a decimal sum
        var accum = _valueFactory.Next(IrPrimitiveType.Decimal);
        block.Instructions.Add(new IrInitAccumulator(accum));

        foreach (var operand in add.Operands)
        {
            if (operand is BoundLiteralExpression lit && lit.Value is decimal d)
            {
                block.Instructions.Add(new IrAccumulateLiteral(accum, d));
            }
            else if (operand is BoundIdentifierExpression srcId)
            {
                var srcLoc = _semantic.GetStorageLocation(srcId.Symbol);
                if (srcLoc.HasValue)
                {
                    block.Instructions.Add(new IrAccumulateField(accum, srcLoc.Value));
                }
            }
        }

        // Step 2: For each target, apply the accumulated sum
        foreach (var target in add.Targets)
        {
            var destLoc = _semantic.GetStorageLocation(target.Symbol);
            if (!destLoc.HasValue) continue;

            int roundingMode = target.IsRounded ? 1 : 0;
            if (add.IsGiving)
            {
                // GIVING: target = accumulated sum (store directly, don't add to current value)
                block.Instructions.Add(new IrMoveAccumulatedToTarget(accum, destLoc.Value, roundingMode));
            }
            else
            {
                // TO: target = target + accumulated sum
                block.Instructions.Add(new IrAddAccumulatedToTarget(accum, destLoc.Value, roundingMode));
            }
        }

        return LowerSizeError(add.SizeError, method, block);
    }

    // ── Shared ON SIZE ERROR lowering ──

    private IrBasicBlock LowerSizeError(BoundSizeErrorClause? clause, IrMethod method, IrBasicBlock block)
    {
        if (clause == null || !clause.HasClauses)
            return block;

        var sizeErrorBlock = method.CreateBlock("size.error");
        var notSizeErrorBlock = method.CreateBlock("not.size.error");
        var doneBlock = method.CreateBlock("size.done");

        var condVal = _valueFactory.Next(IrPrimitiveType.Bool);
        block.Instructions.Add(new IrLoadSizeError(condVal));
        block.Instructions.Add(new IrBranchIfFalse(condVal, notSizeErrorBlock));

        method.Blocks.Add(sizeErrorBlock);
        var seCurrent = sizeErrorBlock;
        foreach (var stmt in clause.OnSizeError)
            seCurrent = LowerStatement(stmt, method, seCurrent);
        seCurrent.Instructions.Add(new IrJump(doneBlock));

        method.Blocks.Add(notSizeErrorBlock);
        var nseCurrent = notSizeErrorBlock;
        foreach (var stmt in clause.NotOnSizeError)
            nseCurrent = LowerStatement(stmt, method, nseCurrent);
        nseCurrent.Instructions.Add(new IrJump(doneBlock));

        method.Blocks.Add(doneBlock);
        return doneBlock;
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

    // ── EVALUATE ──

    private IrBasicBlock LowerEvaluate(BoundEvaluateStatement eval, IrMethod method, IrBasicBlock block)
    {
        var doneBlock = method.CreateBlock("eval.done");

        for (int i = 0; i < eval.Whens.Count; i++)
        {
            var when = eval.Whens[i];
            var whenBody = method.CreateBlock($"eval.when.{i}");
            var nextWhen = method.CreateBlock($"eval.next.{i}");

            // Build match condition: AND across subjects, OR within each subject's values/ranges
            LowerEvaluateWhenMatch(eval, when, whenBody, nextWhen, method, ref block);

            // WHEN body
            method.Blocks.Add(whenBody);
            var bodyCurrent = whenBody;
            foreach (var stmt in when.Statements)
                bodyCurrent = LowerStatement(stmt, method, bodyCurrent);
            bodyCurrent.Instructions.Add(new IrJump(doneBlock));

            // Next WHEN
            method.Blocks.Add(nextWhen);
            block = nextWhen;
        }

        // WHEN OTHER
        if (eval.WhenOther is { Count: > 0 })
        {
            var otherCurrent = block;
            foreach (var stmt in eval.WhenOther)
                otherCurrent = LowerStatement(stmt, method, otherCurrent);
            otherCurrent.Instructions.Add(new IrJump(doneBlock));
        }
        else
        {
            block.Instructions.Add(new IrJump(doneBlock));
        }

        method.Blocks.Add(doneBlock);
        return doneBlock;
    }

    /// <summary>
    /// For a single WHEN clause, emit the match logic.
    /// Multi-subject: AND across subjects. Within each subject: OR over values/ranges.
    /// EVALUATE TRUE: each SubjectCondition is a BoundEvaluateConditionWhen (standalone condition).
    /// </summary>
    private void LowerEvaluateWhenMatch(
        BoundEvaluateStatement eval,
        BoundEvaluateWhen when,
        IrBasicBlock whenBody,
        IrBasicBlock nextWhen,
        IrMethod method,
        ref IrBasicBlock block)
    {
        int subjectCount = when.SubjectConditions.Count;

        if (subjectCount == 0)
        {
            block.Instructions.Add(new IrJump(nextWhen));
            return;
        }

        // For single subject (common case), simplify: no AND needed
        if (subjectCount == 1)
        {
            LowerEvaluateSubjectMatch(eval, 0, when.SubjectConditions[0],
                whenBody, nextWhen, method, ref block);
            return;
        }

        // Multi-subject: all subjects must match (AND).
        // Strategy: for each subject, if it fails → jump to nextWhen.
        // If all pass, fall through to whenBody.
        for (int k = 0; k < subjectCount; k++)
        {
            var cond = when.SubjectConditions[k];
            if (cond is BoundEvaluateValueCondition vc && vc.IsAny)
                continue; // ANY always matches — skip

            if (k < subjectCount - 1)
            {
                // Not the last subject: if this fails, go to nextWhen
                var nextSubject = method.CreateBlock($"eval.subj.{k + 1}");
                LowerEvaluateSubjectMatch(eval, k, cond, nextSubject, nextWhen, method, ref block);
                method.Blocks.Add(nextSubject);
                block = nextSubject;
            }
            else
            {
                // Last subject: if this passes, go to whenBody; if fails, nextWhen
                LowerEvaluateSubjectMatch(eval, k, cond, whenBody, nextWhen, method, ref block);
            }
        }
    }

    /// <summary>
    /// Match a single subject against its condition.
    /// On match: jump to successBlock. On fail: jump to failBlock.
    /// </summary>
    private void LowerEvaluateSubjectMatch(
        BoundEvaluateStatement eval,
        int subjectIndex,
        BoundEvaluateCondition cond,
        IrBasicBlock successBlock,
        IrBasicBlock failBlock,
        IrMethod method,
        ref IrBasicBlock block)
    {
        if (cond is BoundEvaluateConditionWhen condWhen)
        {
            // EVALUATE TRUE: condition is a standalone boolean
            var condVal = _valueFactory.Next(IrPrimitiveType.Bool);
            LowerCondition(condWhen.Condition, condVal, block);
            block.Instructions.Add(new IrBranchIfFalse(condVal, failBlock));
            block.Instructions.Add(new IrJump(successBlock));
            return;
        }

        if (cond is BoundEvaluateValueCondition vc)
        {
            if (vc.IsAny)
            {
                block.Instructions.Add(new IrJump(successBlock));
                return;
            }

            var subject = eval.Subjects[subjectIndex];

            // OR over values: if any value matches, success
            foreach (var value in vc.Values)
            {
                var condVal = _valueFactory.Next(IrPrimitiveType.Bool);
                LowerEvaluateComparison(subject, value, condVal, block);
                // If true → success
                var tryNext = method.CreateBlock("eval.val.next");
                block.Instructions.Add(new IrBranchIfFalse(condVal, tryNext));
                block.Instructions.Add(new IrJump(successBlock));
                method.Blocks.Add(tryNext);
                block = tryNext;
            }

            // OR over ranges: if any range matches, success
            foreach (var range in vc.Ranges)
            {
                // subject >= from AND subject <= to
                var geVal = _valueFactory.Next(IrPrimitiveType.Bool);
                var leVal = _valueFactory.Next(IrPrimitiveType.Bool);
                LowerEvaluateComparison(subject, range.From, geVal, block,
                    BoundBinaryOperatorKind.GreaterOrEqual);
                LowerEvaluateComparison(subject, range.To, leVal, block,
                    BoundBinaryOperatorKind.LessOrEqual);
                // Both must be true → AND
                var tryNextRange = method.CreateBlock("eval.range.next");
                block.Instructions.Add(new IrBranchIfFalse(geVal, tryNextRange));
                block.Instructions.Add(new IrBranchIfFalse(leVal, tryNextRange));
                block.Instructions.Add(new IrJump(successBlock));
                method.Blocks.Add(tryNextRange);
                block = tryNextRange;
            }

            // None matched → fail
            block.Instructions.Add(new IrJump(failBlock));
            return;
        }

        // Unknown condition type — skip to fail
        block.Instructions.Add(new IrJump(failBlock));
    }

    private void LowerEvaluateComparison(BoundExpression subject, BoundExpression obj,
        IrValue result, IrBasicBlock block,
        BoundBinaryOperatorKind op = BoundBinaryOperatorKind.Equal)
    {
        var cmpExpr = new BoundBinaryExpression(subject, op, obj, CobolCategory.Unknown);
        LowerCondition(cmpExpr, result, block);
    }

    private void LowerCondition(BoundExpression cond, IrValue result, IrBasicBlock block)
    {
        // Class condition: IS NUMERIC, IS ALPHABETIC, etc.
        if (cond is BoundClassConditionExpression cc)
        {
            LowerClassCondition(cc, result, block);
            return;
        }

        // Level-88 condition name: expand to parent == val1 OR parent == val2 ...
        if (cond is BoundConditionNameExpression cn)
        {
            LowerConditionName(cn, result, block);
            return;
        }

        if (cond is BoundBinaryExpression binCond)
        {
            switch (binCond.OperatorKind)
            {
                case BoundBinaryOperatorKind.Or:
                {
                    var leftVal = _valueFactory.Next(IrPrimitiveType.Bool);
                    var rightVal = _valueFactory.Next(IrPrimitiveType.Bool);
                    LowerCondition(binCond.Left, leftVal, block);
                    LowerCondition(binCond.Right, rightVal, block);
                    block.Instructions.Add(new IrBinaryLogical(result, leftVal, rightVal, IrLogicalOp.Or));
                    return;
                }

                case BoundBinaryOperatorKind.And:
                {
                    var leftVal = _valueFactory.Next(IrPrimitiveType.Bool);
                    var rightVal = _valueFactory.Next(IrPrimitiveType.Bool);
                    LowerCondition(binCond.Left, leftVal, block);
                    LowerCondition(binCond.Right, rightVal, block);
                    block.Instructions.Add(new IrBinaryLogical(result, leftVal, rightVal, IrLogicalOp.And));
                    return;
                }

                case BoundBinaryOperatorKind.Not:
                {
                    var innerVal = _valueFactory.Next(IrPrimitiveType.Bool);
                    LowerCondition(binCond.Left, innerVal, block);
                    block.Instructions.Add(new IrBinaryLogical(result, innerVal, innerVal, IrLogicalOp.Not));
                    return;
                }
            }

            var leftSym = (binCond.Left as BoundIdentifierExpression)?.Symbol;
            var rightSym = (binCond.Right as BoundIdentifierExpression)?.Symbol;

            if (leftSym != null)
            {
                var leftLoc = _semantic.GetStorageLocation(leftSym);
                if (leftLoc.HasValue)
                {
                    var leftCat = leftLoc.Value.Pic.Category;

                    // identifier vs figurative constant (alphanumeric comparison)
                    if (binCond.Right is BoundFigurativeExpression figRight)
                    {
                        // Convert figurative to single-char string for comparison;
                        // IrStringCompareLiteral pads with spaces, so SPACE works.
                        // For ZERO on numeric, use numeric comparison.
                        var fk = (Runtime.FigurativeKind)figRight.FigurativeKind;
                        if (leftCat.IsNumericLike() && fk == Runtime.FigurativeKind.Zero)
                        {
                            block.Instructions.Add(new IrPicCompareLiteral(
                                leftLoc.Value, 0m, result,
                                (int)binCond.OperatorKind));
                        }
                        else
                        {
                            string figStr = fk switch
                            {
                                Runtime.FigurativeKind.Space => " ",
                                Runtime.FigurativeKind.Zero => "0",
                                Runtime.FigurativeKind.HighValue => "\xFF",
                                Runtime.FigurativeKind.LowValue => "\x00",
                                Runtime.FigurativeKind.Quote => "\"",
                                _ => figRight.AllLiteral ?? " "
                            };
                            block.Instructions.Add(new IrStringCompareLiteral(
                                leftLoc.Value, figStr, result,
                                (int)binCond.OperatorKind));
                        }
                        return;
                    }

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
                    // identifier vs negative numeric literal: -(literal) encoded as (0 - literal)
                    else if (binCond.Right is BoundBinaryExpression negExpr
                             && negExpr.OperatorKind == BoundBinaryOperatorKind.Subtract
                             && negExpr.Left is BoundLiteralExpression zeroLit
                             && zeroLit.Value is decimal zd && zd == 0m
                             && negExpr.Right is BoundLiteralExpression innerLit
                             && innerLit.Value is decimal innerD)
                    {
                        block.Instructions.Add(new IrPicCompareLiteral(
                            leftLoc.Value, -innerD, result,
                            (int)binCond.OperatorKind));
                        return;
                    }
                }
            }

            // Handle literal on left side (e.g., from EVALUATE comparison: literal == identifier)
            if (binCond.Left is BoundLiteralExpression litLeft && rightSym != null)
            {
                var rightLoc = _semantic.GetStorageLocation(rightSym);
                if (rightLoc.HasValue)
                {
                    if (litLeft.Value is decimal dLeft)
                    {
                        // Flip: compare right field against left literal
                        // Flip the operator for non-symmetric comparisons
                        var flippedOp = FlipComparisonOp(binCond.OperatorKind);
                        block.Instructions.Add(new IrPicCompareLiteral(
                            rightLoc.Value, dLeft, result,
                            (int)flippedOp));
                        return;
                    }
                    else if (litLeft.Value is string sLeft)
                    {
                        var flippedOp = FlipComparisonOp(binCond.OperatorKind);
                        block.Instructions.Add(new IrStringCompareLiteral(
                            rightLoc.Value, sLeft, result,
                            (int)flippedOp));
                        return;
                    }
                }
            }
        }

        // Fatal: unrecognized condition shape. Never silently return true —
        // that masks bugs (e.g., NC106A SUB-TEST-F1-7 passed for months with wrong arithmetic).
        // TODO: replace with IrExpressionCompare for production-grade general comparison.
        throw new InvalidOperationException(
            $"Unsupported condition shape in LowerCondition: {cond.GetType().Name} " +
            $"(operator: {(cond is BoundBinaryExpression bc ? bc.OperatorKind.ToString() : "N/A")})");
    }

    private static BoundBinaryOperatorKind FlipComparisonOp(BoundBinaryOperatorKind op)
    {
        return op switch
        {
            BoundBinaryOperatorKind.Less => BoundBinaryOperatorKind.Greater,
            BoundBinaryOperatorKind.LessOrEqual => BoundBinaryOperatorKind.GreaterOrEqual,
            BoundBinaryOperatorKind.Greater => BoundBinaryOperatorKind.Less,
            BoundBinaryOperatorKind.GreaterOrEqual => BoundBinaryOperatorKind.LessOrEqual,
            _ => op // Equal, NotEqual are symmetric
        };
    }

    // ── Class condition (IS NUMERIC, IS ALPHABETIC, etc.) ──

    private void LowerClassCondition(BoundClassConditionExpression cc, IrValue result, IrBasicBlock block)
    {
        if (cc.Subject is not BoundIdentifierExpression id)
            throw new InvalidOperationException("Class condition subject must be a data item");

        var loc = _semantic.GetStorageLocation(id.Symbol);
        if (!loc.HasValue)
            throw new InvalidOperationException($"Cannot resolve storage for {id.Symbol.Name}");

        var tmp = _valueFactory.Next(IrPrimitiveType.Bool);
        block.Instructions.Add(new IrClassCondition(loc.Value, (int)cc.ClassKind, tmp));

        if (cc.IsNegated)
        {
            block.Instructions.Add(new IrBinaryLogical(result, tmp, tmp, IrLogicalOp.Not));
        }
        else
        {
            // Copy tmp to result
            block.Instructions.Add(new IrBinaryLogical(result, tmp, tmp, IrLogicalOp.Or));
        }
    }

    // ── Level-88 condition name ──

    private void LowerConditionName(BoundConditionNameExpression cn, IrValue result, IrBasicBlock block)
    {
        var parentSym = cn.Condition.ParentDataItem;
        var parentLoc = _semantic.GetStorageLocation(parentSym);
        if (!parentLoc.HasValue)
        {
            block.Instructions.Add(new IrSetBool(result, false));
            return;
        }

        var ranges = cn.Condition.ValueRanges;
        if (ranges.Count == 0)
        {
            block.Instructions.Add(new IrSetBool(result, false));
            return;
        }

        var parentCat = parentLoc.Value.Pic.Category;

        // Build a list of individual match results
        var matchResults = new List<IrValue>();

        foreach (var (from, to) in ranges)
        {
            var matchVal = _valueFactory.Next(IR.IrPrimitiveType.Bool);

            if (to == null)
            {
                // Single value comparison: parent == from
                if (from is decimal d)
                {
                    block.Instructions.Add(new IR.IrPicCompareLiteral(
                        parentLoc.Value, d, matchVal,
                        (int)BoundBinaryOperatorKind.Equal));
                }
                else if (from is string s)
                {
                    if (parentCat.IsNumericLike() && decimal.TryParse(s,
                        System.Globalization.CultureInfo.InvariantCulture, out var numVal))
                    {
                        block.Instructions.Add(new IR.IrPicCompareLiteral(
                            parentLoc.Value, numVal, matchVal,
                            (int)BoundBinaryOperatorKind.Equal));
                    }
                    else
                    {
                        block.Instructions.Add(new IR.IrStringCompareLiteral(
                            parentLoc.Value, s, matchVal,
                            (int)BoundBinaryOperatorKind.Equal));
                    }
                }
            }
            else
            {
                // Range: parent >= from AND parent <= to
                var geVal = _valueFactory.Next(IR.IrPrimitiveType.Bool);
                var leVal = _valueFactory.Next(IR.IrPrimitiveType.Bool);

                if (from is decimal dFrom && to is decimal dTo)
                {
                    block.Instructions.Add(new IR.IrPicCompareLiteral(
                        parentLoc.Value, dFrom, geVal,
                        (int)BoundBinaryOperatorKind.GreaterOrEqual));
                    block.Instructions.Add(new IR.IrPicCompareLiteral(
                        parentLoc.Value, dTo, leVal,
                        (int)BoundBinaryOperatorKind.LessOrEqual));
                }
                else if (from is string sFrom && to is string sTo)
                {
                    block.Instructions.Add(new IR.IrStringCompareLiteral(
                        parentLoc.Value, sFrom, geVal,
                        (int)BoundBinaryOperatorKind.GreaterOrEqual));
                    block.Instructions.Add(new IR.IrStringCompareLiteral(
                        parentLoc.Value, sTo, leVal,
                        (int)BoundBinaryOperatorKind.LessOrEqual));
                }

                block.Instructions.Add(new IR.IrBinaryLogical(matchVal, geVal, leVal, IR.IrLogicalOp.And));
            }

            matchResults.Add(matchVal);
        }

        // OR all match results together
        if (matchResults.Count == 1)
        {
            // Single value — copy directly
            block.Instructions.Add(new IR.IrBinaryLogical(result, matchResults[0], matchResults[0], IR.IrLogicalOp.Or));
        }
        else
        {
            // Multiple values — chain ORs
            var accumulated = matchResults[0];
            for (int i = 1; i < matchResults.Count; i++)
            {
                var orResult = _valueFactory.Next(IR.IrPrimitiveType.Bool);
                block.Instructions.Add(new IR.IrBinaryLogical(orResult, accumulated, matchResults[i], IR.IrLogicalOp.Or));
                accumulated = orResult;
            }
            // Final result
            block.Instructions.Add(new IR.IrBinaryLogical(result, accumulated, accumulated, IR.IrLogicalOp.Or));
        }

        // Handle NOT condition-name
        if (cn.IsNegated)
        {
            var notResult = _valueFactory.Next(IR.IrPrimitiveType.Bool);
            block.Instructions.Add(new IR.IrBinaryLogical(notResult, result, result, IR.IrLogicalOp.Not));
            // Copy negated result back
            block.Instructions.Add(new IR.IrBinaryLogical(result, notResult, notResult, IR.IrLogicalOp.Or));
        }
    }

    // ── GO TO ──

    private void LowerGoTo(BoundGoToStatement gt, IrBasicBlock block)
    {
        if (_paragraphIndices.TryGetValue(gt.Target.Name, out int targetIndex))
        {
            block.Instructions.Add(new IrReturnConst(targetIndex));
        }
    }

    // ── NEXT SENTENCE ──

    private IrBasicBlock LowerNextSentence(IrMethod method, IrBasicBlock block)
    {
        if (_currentSentenceEnd is null)
            throw new InvalidOperationException("NEXT SENTENCE used outside a sentence context.");

        // Jump to the end of the current sentence
        block.Instructions.Add(new IrJump(_currentSentenceEnd));

        // Any statements after NEXT SENTENCE in this sentence are unreachable.
        // Create a dead block so subsequent lowering has somewhere to emit into.
        var dead = new IrBasicBlock("dead_after_next_sentence");
        method.Blocks.Add(dead);
        return dead;
    }
}
