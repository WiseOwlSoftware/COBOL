// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.IR;
using CobolSharp.Compiler.Semantics;
using CobolSharp.Compiler.Semantics.Bound;

namespace CobolSharp.Compiler.CodeGen.Lowering;

/// <summary>
/// Lowers COBOL file I/O statements to IR: OPEN, CLOSE, READ, WRITE,
/// REWRITE, DELETE, START, SORT, MERGE, RELEASE, RETURN.
/// Includes file status emission and USE AFTER declarative handling.
/// </summary>
internal sealed class FileIoLowerer
{
    private readonly LoweringContext _ctx;

    public FileIoLowerer(LoweringContext ctx) => _ctx = ctx;

    // ── WRITE ──

    public void LowerWrite(BoundWriteStatement wr, IrBasicBlock block)
    {
        string fileName = wr.File?.Name ?? "PRINT-FILE";

        // Try to get storage location for the record
        var recordLoc = _ctx.Location.ResolveLocation(wr.Record);
        if (recordLoc != null)
        {
            // WRITE FROM: MOVE source TO record before writing
            if (wr.From != null)
            {
                var fromLoc = _ctx.Location.ResolveExpressionLocation(wr.From);
                if (fromLoc != null)
                    block.Instructions.Add(new IrMoveFieldToField(
                        fromLoc, recordLoc,
                        fromLoc.GetPic(), recordLoc.GetPic()));
            }

            if (wr.AdvancingLines.HasValue)
            {
                IrLocation? advLoc = null;
                if (wr.AdvancingExpression != null)
                    advLoc = _ctx.Location.ResolveExpressionLocation(wr.AdvancingExpression);
                block.Instructions.Add(new IrWriteAdvancing(
                    fileName, recordLoc, wr.AdvancingLines.Value, !wr.IsAfterAdvancing,
                    advancingLocation: advLoc));
            }
            else
            {
                block.Instructions.Add(new IrWriteRecordFromStorage(fileName, recordLoc));
            }
        }
        else
        {
            // Fallback: write placeholder via WriteText
            var fileNameVal = _ctx.ValueFactory.Next(IrPrimitiveType.String);
            block.Instructions.Add(new IrLoadConst(fileNameVal, fileName));
            var textVal = _ctx.ValueFactory.Next(IrPrimitiveType.String);
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

    public IrBasicBlock LowerOpen(BoundOpenStatement open, IrMethod method, IrBasicBlock block)
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
            var fnVal = _ctx.ValueFactory.Next(IrPrimitiveType.String);
            block.Instructions.Add(new IrLoadConst(fnVal, cobolName));
            block.Instructions.Add(new IrRuntimeCall(null, runtimeMethod, new[] { fnVal }));

            // Update FILE STATUS if declared
            EmitFileStatus(file, block);

            // USE AFTER EXCEPTION check
            block = EmitUseDeclarative(file, method, block);
        }
        return block;
    }

    // ── CLOSE ──

    public IrBasicBlock LowerClose(BoundCloseStatement close, IrMethod method, IrBasicBlock block)
    {
        foreach (var phrase in close.FilePhrases)
        {
            string cobolName = phrase.File.Name;
            var fnVal = _ctx.ValueFactory.Next(IrPrimitiveType.String);
            block.Instructions.Add(new IrLoadConst(fnVal, cobolName));

            if (phrase.Option == CloseOption.Lock)
            {
                block.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.CloseFileWithLock", new[] { fnVal }));
            }
            else
            {
                // REEL, UNIT, NO REWIND are no-ops on disk — use standard close
                block.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.CloseFile", new[] { fnVal }));
            }

            EmitFileStatus(phrase.File, block);

            // USE AFTER EXCEPTION check
            block = EmitUseDeclarative(phrase.File, method, block);
        }
        return block;
    }

    // ── READ ──

    public IrBasicBlock LowerRead(BoundReadStatement read, IrMethod method, IrBasicBlock block)
    {
        string cobolName = read.File.Name;

        // Read into the FD record buffer
        var recordSym = read.File.Record;
        if (recordSym != null)
        {
            var recordLoc = _ctx.Location.ResolveLocation(recordSym);
            if (recordLoc != null)
            {
                if (read.IsPrevious)
                {
                    // READ PREVIOUS — reverse sequential access
                    block.Instructions.Add(new IrReadPreviousToStorage(cobolName, recordLoc));
                }
                else
                {
                    // Keyed read: RANDOM/DYNAMIC access without NEXT → ReadByKey
                    // For INDEXED files, use RECORD KEY; for RELATIVE files, use RELATIVE KEY
                    string? keyName = read.File.RecordKey ?? read.File.RelativeKey;
                    bool isKeyedRead = !read.IsNext &&
                        read.File.AccessMode is "RANDOM" or "DYNAMIC" &&
                        keyName != null;

                    if (isKeyedRead)
                    {
                        var keySym = _ctx.Semantic.ResolveData(keyName!);
                        var keyLoc = keySym != null ? _ctx.Location.ResolveLocation(keySym) : null;
                        if (keyLoc != null)
                            block.Instructions.Add(new IrReadByKey(cobolName, recordLoc, keyLoc));
                        else
                            block.Instructions.Add(new IrReadRecordToStorage(cobolName, recordLoc));
                    }
                    else
                    {
                        block.Instructions.Add(new IrReadRecordToStorage(cobolName, recordLoc));
                    }
                }
            }
        }

        // Update FILE STATUS
        EmitFileStatus(read.File, block);

        // If INTO specified, MOVE FD record to INTO target
        if (read.Into != null && recordSym != null)
        {
            var srcLoc = _ctx.Location.ResolveLocation(recordSym);
            var dstLoc = _ctx.Location.ResolveLocation(read.Into);
            if (srcLoc != null && dstLoc != null)
            {
                block.Instructions.Add(new IrMoveFieldToField(
                    srcLoc, dstLoc,
                    srcLoc.GetPic(), dstLoc.GetPic()));
            }
        }

        // AT END / NOT AT END branching
        if (read.AtEnd.Count > 0 || read.NotAtEnd.Count > 0)
        {
            var atEndResult = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
            block.Instructions.Add(new IrCheckFileAtEnd(cobolName, atEndResult));
            return _ctx.Condition.LowerConditionalBranch(read.AtEnd, read.NotAtEnd, atEndResult, method, block, "read");
        }

        // INVALID KEY branching
        if (read.InvalidKey.Count > 0 || read.NotInvalidKey.Count > 0)
        {
            var invalidResult = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
            block.Instructions.Add(new IrCheckFileInvalidKey(cobolName, invalidResult));
            return _ctx.Condition.LowerConditionalBranch(read.InvalidKey, read.NotInvalidKey, invalidResult, method, block, "read");
        }

        // USE AFTER EXCEPTION: fire declarative handler if no explicit AT END/INVALID KEY
        block = EmitUseDeclarative(read.File, method, block);

        return block;
    }

    /// <summary>
    /// Emit FILE STATUS update: after each file operation, store the 2-char
    /// status code into the FILE STATUS variable if one was declared.
    /// </summary>
    private void EmitFileStatus(FileSymbol file, IrBasicBlock block)
    {
        if (file.FileStatus == null) return;

        var statusSym = _ctx.Semantic.ResolveData(file.FileStatus);
        if (statusSym == null) return;

        var statusLoc = _ctx.Location.ResolveLocation(statusSym);
        if (statusLoc == null) return;

        block.Instructions.Add(new IrStoreFileStatus(file.Name, statusLoc));
    }

    /// <summary>
    /// Emit USE AFTER EXCEPTION declarative check: if a USE declarative is registered
    /// for this file and the last I/O status indicates an error, PERFORM the declarative section.
    /// Returns the (possibly new) current block after branching.
    /// </summary>
    private IrBasicBlock EmitUseDeclarative(FileSymbol file, IrMethod method, IrBasicBlock block)
    {
        if (!_ctx.Semantic.UseDeclaratives.TryGetValue(file.Name, out var sectionName))
            return block;

        // Find the first paragraph of the declarative section
        var sectionParas = _ctx.Semantic.GetSectionParagraphs(sectionName);
        if (sectionParas == null || sectionParas.Count == 0)
            return block;

        string firstPara = sectionParas[0];
        if (!_ctx.ParagraphMethods.TryGetValue(firstPara, out var paraMethod))
            return block;

        // Check if file status != "00" (error occurred)
        var errorVal = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
        block.Instructions.Add(new IrCheckFileInvalidKey(file.Name, errorVal));

        var useBlock = method.CreateBlock("use.handler");
        var afterBlock = method.CreateBlock("use.after");
        block.Instructions.Add(new IrBranch(errorVal, useBlock, afterBlock));

        // USE handler: PERFORM the declarative section
        method.Blocks.Add(useBlock);
        if (sectionParas.Count == 1)
        {
            useBlock.Instructions.Add(new IrPerform(paraMethod));
        }
        else
        {
            // PERFORM THRU all paragraphs in the section
            int startIdx = _ctx.ParagraphIndices.GetValueOrDefault(sectionParas[0], -1);
            int endIdx = _ctx.ParagraphIndices.GetValueOrDefault(sectionParas[^1], -1);
            if (startIdx >= 0 && endIdx >= 0)
            {
                var methods = new List<IrMethod>();
                for (int i = startIdx; i <= endIdx; i++)
                {
                    var pName = _ctx.ParagraphsByIndex[i];
                    if (_ctx.ParagraphMethods.TryGetValue(pName, out var pm))
                        methods.Add(pm);
                    else
                    {
                        _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0501, SourceLocation.None, TextSpan.Empty, pName);
                        continue;
                    }
                }
                useBlock.Instructions.Add(new IrPerformThru(startIdx, endIdx, methods));
            }
        }
        useBlock.Instructions.Add(new IrJump(afterBlock));

        method.Blocks.Add(afterBlock);
        return afterBlock;
    }

    // ── REWRITE ──

    public void LowerRewrite(BoundRewriteStatement rw, IrBasicBlock block)
    {
        string cobolName = rw.File.Name;
        var recordLoc = _ctx.Location.ResolveLocation(rw.Record);
        if (recordLoc != null)
        {
            // REWRITE FROM: MOVE source TO record before rewriting
            if (rw.From != null)
            {
                var fromLoc = _ctx.Location.ResolveExpressionLocation(rw.From);
                if (fromLoc != null)
                    block.Instructions.Add(new IrMoveFieldToField(
                        fromLoc, recordLoc,
                        fromLoc.GetPic(), recordLoc.GetPic()));
            }

            block.Instructions.Add(new IrRewriteRecordFromStorage(cobolName, recordLoc));
        }
        EmitFileStatus(rw.File, block);
    }

    // ── DELETE ──

    public IrBasicBlock LowerDelete(BoundDeleteStatement del, IrMethod method, IrBasicBlock block)
    {
        string cobolName = del.File.Name;
        block.Instructions.Add(new IrDeleteRecord(cobolName));
        EmitFileStatus(del.File, block);

        // INVALID KEY / NOT INVALID KEY branching
        if (del.InvalidKey.Count > 0 || del.NotInvalidKey.Count > 0)
        {
            var invalidResult = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
            block.Instructions.Add(new IrCheckFileInvalidKey(cobolName, invalidResult));
            return _ctx.Condition.LowerConditionalBranch(del.InvalidKey, del.NotInvalidKey, invalidResult, method, block, "delete");
        }

        return block;
    }

    // ── START ──

    public IrBasicBlock LowerStart(BoundStartStatement start, IrMethod method, IrBasicBlock block)
    {
        string cobolName = start.File.Name;

        // Resolve key from the file's RECORD KEY
        var recordKey = start.File.RecordKey;
        if (recordKey != null)
        {
            var keySym = _ctx.Semantic.ResolveData(recordKey);
            if (keySym != null)
            {
                var keyLoc = _ctx.Location.ResolveLocation(keySym);
                if (keyLoc != null)
                {
                    // Extract key condition from bound tree (default: Equal)
                    int condition = 0; // StartCondition.Equal
                    if (start.KeyCondition is BoundBinaryExpression keyExpr)
                    {
                        condition = keyExpr.OperatorKind switch
                        {
                            BoundBinaryOperatorKind.Equal => 0,
                            BoundBinaryOperatorKind.Greater => 1,
                            BoundBinaryOperatorKind.GreaterOrEqual => 2,
                            BoundBinaryOperatorKind.Less => 3,
                            BoundBinaryOperatorKind.LessOrEqual => 4,
                            _ => 0
                        };
                    }
                    block.Instructions.Add(new IrStartFile(cobolName, keyLoc, condition));
                }
            }
        }

        EmitFileStatus(start.File, block);

        // INVALID KEY / NOT INVALID KEY branching
        if (start.InvalidKey.Count > 0 || start.NotInvalidKey.Count > 0)
        {
            var invalidResult = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
            block.Instructions.Add(new IrCheckFileInvalidKey(cobolName, invalidResult));
            return _ctx.Condition.LowerConditionalBranch(start.InvalidKey, start.NotInvalidKey, invalidResult, method, block, "start");
        }

        return block;
    }

    // ── RETURN (sort/merge) ──

    public IrBasicBlock LowerReturn(BoundReturnStatement ret, IrMethod method, IrBasicBlock block)
    {
        string cobolName = ret.File.Name;
        var recordSym = ret.File.Record;
        if (recordSym == null) return block;

        var recordLoc = _ctx.Location.ResolveLocation(recordSym);
        if (recordLoc == null) return block;

        // SortRuntime.ReturnRecord → bool (true = record available, false = at end)
        var resultVal = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
        block.Instructions.Add(new IrSortReturn(cobolName, recordLoc, resultVal));

        if (ret.AtEnd.Count > 0 || ret.NotAtEnd.Count > 0)
        {
            // resultVal = true means NOT at end; build branching manually
            // True branch = NOT AT END (record available), False branch = AT END
            var notAtEndBlock = method.CreateBlock("return.not_at_end");
            var atEndBlock = method.CreateBlock("return.at_end");
            var afterBlock = method.CreateBlock("return.after");

            // Branch: if resultVal is false → atEndBlock, else fall through to notAtEndBlock
            block.Instructions.Add(new IrBranchIfFalse(resultVal, atEndBlock));
            block.Instructions.Add(new IrJump(notAtEndBlock));

            // NOT AT END: move INTO, then user statements
            method.Blocks.Add(notAtEndBlock);
            if (ret.Into != null)
            {
                var srcLoc = _ctx.Location.ResolveLocation(recordSym);
                var dstLoc = _ctx.Location.ResolveLocation(ret.Into);
                if (srcLoc != null && dstLoc != null)
                    notAtEndBlock.Instructions.Add(new IrMoveFieldToField(srcLoc, dstLoc,
                        srcLoc.GetPic(), dstLoc.GetPic()));
            }
            var current = notAtEndBlock;
            foreach (var stmt in ret.NotAtEnd)
                current = _ctx.LowerStatement(stmt, method, current);
            current.Instructions.Add(new IrJump(afterBlock));

            // AT END: user statements
            method.Blocks.Add(atEndBlock);
            current = atEndBlock;
            foreach (var stmt in ret.AtEnd)
                current = _ctx.LowerStatement(stmt, method, current);
            current.Instructions.Add(new IrJump(afterBlock));

            method.Blocks.Add(afterBlock);
            return afterBlock;
        }
        else
        {
            // No AT END phrase — just do INTO move unconditionally
            if (ret.Into != null)
            {
                var srcLoc = _ctx.Location.ResolveLocation(recordSym);
                var dstLoc = _ctx.Location.ResolveLocation(ret.Into);
                if (srcLoc != null && dstLoc != null)
                    block.Instructions.Add(new IrMoveFieldToField(srcLoc, dstLoc,
                        srcLoc.GetPic(), dstLoc.GetPic()));
            }
        }

        return block;
    }

    // ── SORT ──

    public IrBasicBlock LowerSort(BoundSortStatement sort, IrMethod method, IrBasicBlock block)
    {
        string sortFileName = sort.SortFile.Name;
        var sdRecord = sort.SortFile.Record;
        if (sdRecord == null) return block;

        var sdLoc = _ctx.Location.ResolveLocation(sdRecord);
        if (sdLoc == null) return block;

        int recordLength = sort.SortFile.RecordLength;
        if (recordLength == 0)
        {
            var recLoc = _ctx.Semantic.GetStorageLocation(sdRecord);
            if (recLoc.HasValue) recordLength = recLoc.Value.Length;
        }

        // Phase 0: Initialize the sort file
        block.Instructions.Add(new IrSortInit(sortFileName, recordLength));

        // Phase 1: Input — collect records
        if (sort.UsingFiles != null)
        {
            foreach (var inputFile in sort.UsingFiles)
                EmitSortUsingFile(inputFile, sort.SortFile, sdRecord, sdLoc, recordLength, method, ref block);
        }
        else if (sort.InputProcedure != null)
        {
            var performStmt = new BoundPerformStatement(sort.InputProcedure, sort.InputProcedureThru);
            block = _ctx.LowerStatement(performStmt, method, block);
        }

        // Phase 2: Sort the records
        block.Instructions.Add(new IrSortSort(sortFileName, BuildKeysSpec(sort.SortFile, sort.Keys)));

        // Phase 3: Output — return sorted records
        if (sort.GivingFiles != null)
        {
            foreach (var outputFile in sort.GivingFiles)
                EmitSortGivingFile(outputFile, sort.SortFile, sdRecord, sdLoc, recordLength, method, ref block);
        }
        else if (sort.OutputProcedure != null)
        {
            var performStmt = new BoundPerformStatement(sort.OutputProcedure, sort.OutputProcedureThru);
            block = _ctx.LowerStatement(performStmt, method, block);
        }

        // Clean up
        block.Instructions.Add(new IrSortClose(sortFileName));

        return block;
    }

    // ── TABLE SORT (Format 2) ──

    public IrBasicBlock LowerTableSort(BoundTableSortStatement tableSort, IrMethod method, IrBasicBlock block)
    {
        var tableSym = tableSort.TableItem;

        // Resolve storage location of the table item
        var tableLoc = _ctx.Location.ResolveLocation(tableSym);
        if (tableLoc == null) return block;

        // Get OCCURS info for entry count and entry size
        var occurs = tableSym.Occurs;
        if (occurs == null) return block;

        int entryCount = occurs.MaxOccurs;
        int entrySize = tableSym.ElementSize;
        if (entrySize <= 0) return block;

        // Build keys spec string: "relOffset,length,asc;..."
        var keysSpec = BuildTableKeysSpec(tableSort.Keys, tableSym);

        block.Instructions.Add(new IrTableSort(tableLoc, entrySize, entryCount, keysSpec));

        return block;
    }

    private string BuildTableKeysSpec(IReadOnlyList<BoundSortKey> keys, DataSymbol tableItem)
    {
        var specs = new List<string>();
        foreach (var key in keys)
        {
            var keySym = key.Key;
            // Key offset relative to the table entry start
            var keyLoc = _ctx.Semantic.GetStorageLocation(keySym);
            var tableLoc = _ctx.Semantic.GetStorageLocation(tableItem);
            if (!keyLoc.HasValue || !tableLoc.HasValue) continue;

            int relativeOffset = keyLoc.Value.Offset - tableLoc.Value.Offset;
            int length = keyLoc.Value.Length;
            bool asc = key.IsAscending;

            // Check if numeric
            specs.Add($"{relativeOffset},{length},{(asc ? "1" : "0")}");
        }
        return string.Join(";", specs);
    }

    private void EmitSortUsingFile(FileSymbol inputFile, FileSymbol sortFile,
        DataSymbol sdRecord, IrLocation sdLoc, int recordLength,
        IrMethod method, ref IrBasicBlock block)
    {
        string inputName = inputFile.Name;
        string sortName = sortFile.Name;

        var inputRecord = inputFile.Record;
        if (inputRecord == null) return;
        var inputLoc = _ctx.Location.ResolveLocation(inputRecord);
        if (inputLoc == null) return;

        // OPEN INPUT input-file
        var openNameVal = _ctx.ValueFactory.Next(IrPrimitiveType.String);
        block.Instructions.Add(new IrLoadConst(openNameVal, inputName));
        block.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.OpenInput", new[] { openNameVal }));

        // Loop: READ → RELEASE → repeat until AT END
        var loopHead = method.CreateBlock("sort_using_read");
        var loopBody = method.CreateBlock("sort_using_release");
        var loopExit = method.CreateBlock("sort_using_done");

        block.Instructions.Add(new IrJump(loopHead));
        method.Blocks.Add(loopHead);

        // Read record from input file
        loopHead.Instructions.Add(new IrReadRecordToStorage(inputName, inputLoc));
        var atEndVal = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
        loopHead.Instructions.Add(new IrCheckFileAtEnd(inputName, atEndVal));
        loopHead.Instructions.Add(new IrBranch(atEndVal, loopExit, loopBody));

        method.Blocks.Add(loopBody);
        // Copy input record → SD record, then release to sort
        var inputPic = inputLoc.GetPic();
        var sdPic = sdLoc.GetPic();
        loopBody.Instructions.Add(new IrMoveFieldToField(inputLoc, sdLoc, inputPic, sdPic));
        loopBody.Instructions.Add(new IrSortRelease(sortName, sdLoc));
        loopBody.Instructions.Add(new IrJump(loopHead));

        method.Blocks.Add(loopExit);
        // Close input file
        var closeNameVal = _ctx.ValueFactory.Next(IrPrimitiveType.String);
        loopExit.Instructions.Add(new IrLoadConst(closeNameVal, inputName));
        loopExit.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.CloseFile", new[] { closeNameVal }));

        block = loopExit;
    }

    private void EmitSortGivingFile(FileSymbol outputFile, FileSymbol sortFile,
        DataSymbol sdRecord, IrLocation sdLoc, int recordLength,
        IrMethod method, ref IrBasicBlock block)
    {
        string outputName = outputFile.Name;
        string sortName = sortFile.Name;

        var outputRecord = outputFile.Record;
        if (outputRecord == null) return;
        var outputLoc = _ctx.Location.ResolveLocation(outputRecord);
        if (outputLoc == null) return;

        // OPEN OUTPUT output-file
        var openNameVal = _ctx.ValueFactory.Next(IrPrimitiveType.String);
        block.Instructions.Add(new IrLoadConst(openNameVal, outputName));
        block.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.OpenOutput", new[] { openNameVal }));

        // Loop: RETURN → WRITE → repeat until at end
        var loopHead = method.CreateBlock("sort_giving_return");
        var loopBody = method.CreateBlock("sort_giving_write");
        var loopExit = method.CreateBlock("sort_giving_done");

        block.Instructions.Add(new IrJump(loopHead));
        method.Blocks.Add(loopHead);

        // Return next sorted record into SD record
        var retResult = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
        loopHead.Instructions.Add(new IrSortReturn(sortName, sdLoc, retResult));
        loopHead.Instructions.Add(new IrBranch(retResult, loopBody, loopExit));

        method.Blocks.Add(loopBody);
        // Copy SD record → output record, then write
        var sdPic = sdLoc.GetPic();
        var outPic = outputLoc.GetPic();
        loopBody.Instructions.Add(new IrMoveFieldToField(sdLoc, outputLoc, sdPic, outPic));
        loopBody.Instructions.Add(new IrWriteRecordFromStorage(outputName, outputLoc));
        loopBody.Instructions.Add(new IrJump(loopHead));

        method.Blocks.Add(loopExit);
        // Close output file
        var closeNameVal = _ctx.ValueFactory.Next(IrPrimitiveType.String);
        loopExit.Instructions.Add(new IrLoadConst(closeNameVal, outputName));
        loopExit.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.CloseFile", new[] { closeNameVal }));

        block = loopExit;
    }

    private string BuildKeysSpec(FileSymbol sortFile, IReadOnlyList<BoundSortKey> keys)
    {
        var sdRecord = sortFile.Record;
        var sdRecLoc = sdRecord != null ? _ctx.Semantic.GetStorageLocation(sdRecord) : null;
        int sdBaseOffset = sdRecLoc?.Offset ?? 0;

        return string.Join(";", keys.Select(k =>
        {
            var keyLoc = _ctx.Semantic.GetStorageLocation(k.Key);
            int keyOff = keyLoc.HasValue ? keyLoc.Value.Offset - sdBaseOffset : 0;
            int keyLen = keyLoc.HasValue ? keyLoc.Value.Length : 0;
            var pic = _ctx.Semantic.GetPicDescriptor(k.Key);
            // Extended format: offset,length,asc,usage,isSigned,signStorage,fractionDigits,totalDigits,leadingScale,trailingScale
            int usage = pic != null ? (int)pic.Usage : 0;
            int isSigned = pic is { IsSigned: true } ? 1 : 0;
            int signStorage = pic != null ? (int)pic.SignStorage : 0;
            int fractionDigits = pic?.FractionDigits ?? 0;
            int totalDigits = pic?.TotalDigits ?? 0;
            int leadingScale = pic?.LeadingScaleDigits ?? 0;
            int trailingScale = pic?.TrailingScaleDigits ?? 0;
            bool isNumeric = pic is { IsNumeric: true };
            return $"{keyOff},{keyLen},{(k.IsAscending ? "1" : "0")},{(isNumeric ? "1" : "0")},{usage},{isSigned},{signStorage},{fractionDigits},{totalDigits},{leadingScale},{trailingScale}";
        }));
    }

    // ── MERGE ──

    public IrBasicBlock LowerMerge(BoundMergeStatement merge, IrMethod method, IrBasicBlock block)
    {
        string mergeFileName = merge.MergeFile.Name;
        var sdRecord = merge.MergeFile.Record;
        if (sdRecord == null) return block;

        var sdLoc = _ctx.Location.ResolveLocation(sdRecord);
        if (sdLoc == null) return block;

        int recordLength = merge.MergeFile.RecordLength;
        if (recordLength == 0)
        {
            var recLoc = _ctx.Semantic.GetStorageLocation(sdRecord);
            if (recLoc.HasValue) recordLength = recLoc.Value.Length;
        }

        // Initialize
        block.Instructions.Add(new IrSortInit(mergeFileName, recordLength));

        // Merge: read from all USING files, sort by keys
        var inputNames = string.Join(";", merge.UsingFiles.Select(f => f.Name));
        var keysSpec = BuildKeysSpec(merge.MergeFile, merge.Keys);
        block.Instructions.Add(new IrSortMerge(mergeFileName, inputNames, keysSpec));

        // Output phase
        if (merge.GivingFiles != null)
        {
            foreach (var outputFile in merge.GivingFiles)
                EmitSortGivingFile(outputFile, merge.MergeFile, sdRecord, sdLoc, recordLength, method, ref block);
        }
        else if (merge.OutputProcedure != null)
        {
            var performStmt = new BoundPerformStatement(merge.OutputProcedure, merge.OutputProcedureThru);
            block = _ctx.LowerStatement(performStmt, method, block);
        }

        // Clean up
        block.Instructions.Add(new IrSortClose(mergeFileName));

        return block;
    }

    // ── RELEASE ──

    public IrBasicBlock LowerRelease(BoundReleaseStatement release, IrMethod method, IrBasicBlock block)
    {
        string sortFileName = release.SortFile.Name;
        var recordLoc = _ctx.Location.ResolveLocation(release.Record);
        if (recordLoc == null) return block;

        // If FROM is specified, MOVE source → record first
        if (release.From != null)
        {
            var fromLoc = _ctx.Location.ResolveExpressionLocation(release.From);
            if (fromLoc != null)
                block.Instructions.Add(new IrMoveFieldToField(fromLoc, recordLoc,
                    fromLoc.GetPic(), recordLoc.GetPic()));
        }

        // Release the record to the sort file
        block.Instructions.Add(new IrSortRelease(sortFileName, recordLoc));

        return block;
    }
}
