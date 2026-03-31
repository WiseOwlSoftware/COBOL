// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using CobolSharp.Compiler.Generated;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.Semantics.Bound.Binding;

/// <summary>
/// File I/O binding: BindWrite, BindOpen, BindClose, BindRead, BindRewrite,
/// BindDelete, BindStart, BindReturn, BindSort, BindMerge, BindRelease,
/// BindSortKeys, BindMergeKeys, ResolveFileList, BindUse.
/// </summary>
internal sealed class FileIoBinder
{
    private readonly BindingContext _ctx;

    internal FileIoBinder(BindingContext ctx) => _ctx = ctx;

    // ── WRITE ──

    internal BoundWriteStatement? BindWrite(CobolParserCore.WriteStatementContext ctx)
    {
        var recordCtx = ctx.recordName();
        if (recordCtx == null) return null;

        string recordName = recordCtx.GetText();
        var recordSym = _ctx.Semantic.ResolveData(recordName);
        if (recordSym == null) return null;

        // Resolve file from record → FD relationship
        var fileSym = _ctx.Semantic.ResolveFileForRecord(recordSym);

        // Parse BEFORE/AFTER ADVANCING clause
        int? advancingLines = null;
        bool isAfterAdvancing = true;
        BoundExpression? advancingExpression = null;
        var advCtx = ctx.writeBeforeAfter();
        if (advCtx != null)
        {
            isAfterAdvancing = advCtx.GetChild(0).GetText().Equals("AFTER", StringComparison.OrdinalIgnoreCase);
            // Parse the advancing value — integer literal, PAGE, or identifier
            if (advCtx.PAGE() != null)
            {
                advancingLines = -1; // PAGE = form-feed (sentinel value)
            }
            else
            {
                var intLit = advCtx.integerLiteral();
                if (intLit != null)
                {
                    advancingLines = int.Parse(intLit.GetText());
                }
                else
                {
                    // Could be a data identifier referencing a data field
                    var idCtx = advCtx.dataReference();
                    if (idCtx != null)
                    {
                        // Data identifier — bind as expression, read at runtime
                        advancingExpression = _ctx.Expression.BindDataReferenceWithSubscripts(idCtx);
                        advancingLines = 0; // Sentinel: will be overridden at runtime
                    }
                    else
                    {
                        advancingLines = 1; // Default: 1 line
                    }
                }
            }
        }

        BoundExpression? from = null;
        if (ctx.writeFrom() is { } fromCtx)
            from = _ctx.Expression.BindDataReferenceWithSubscripts(fromCtx.dataReference());

        // INVALID KEY / NOT INVALID KEY
        var invalidKey = new List<BoundStatement>();
        var notInvalidKey = new List<BoundStatement>();
        if (ctx.writeInvalidKey() is { } wikCtx)
        {
            var impStmts = wikCtx.statementBlock();
            if (impStmts.Length >= 1)
                foreach (var stmt in impStmts[0].statement())
                {
                    var bound = _ctx.BindStatement(stmt);
                    if (bound != null) invalidKey.Add(bound);
                }
            if (impStmts.Length >= 2)
                foreach (var stmt in impStmts[1].statement())
                {
                    var bound = _ctx.BindStatement(stmt);
                    if (bound != null) notInvalidKey.Add(bound);
                }
        }

        return new BoundWriteStatement(fileSym, recordSym, from, advancingLines, isAfterAdvancing, invalidKey, notInvalidKey,
            advancingExpression: advancingExpression);
    }

    // ── OPEN ──

    internal BoundStatement BindOpen(CobolParserCore.OpenStatementContext ctx)
    {
        var results = new List<BoundStatement>();
        foreach (var clause in ctx.openClause())
        {
            var modeCtx = clause.openMode();
            var mode = modeCtx.GetText().ToUpperInvariant() switch
            {
                "INPUT" => OpenMode.Input,
                "OUTPUT" => OpenMode.Output,
                "I-O" => OpenMode.IO,
                "EXTEND" => OpenMode.Extend,
                _ => OpenMode.Output
            };

            var files = new List<FileSymbol>();
            foreach (var idCtx in clause.dataReference())
            {
                string name = idCtx.cobolWord().GetText();
                var fileSym = _ctx.Semantic.ResolveFile(name);
                if (fileSym != null)
                    files.Add(fileSym);
            }

            if (files.Count > 0)
                results.Add(new BoundOpenStatement(mode, files));
        }

        if (results.Count == 1) return results[0];
        return results.Count > 1 ? new BoundCompoundStatement(results)
            : new BoundOpenStatement(OpenMode.Output, Array.Empty<FileSymbol>());
    }

    // ── CLOSE ──

    internal BoundStatement BindClose(CobolParserCore.CloseStatementContext ctx)
    {
        var phrases = new List<BoundCloseFilePhrase>();
        foreach (var phraseCtx in ctx.closeFilePhrase())
        {
            var fn = phraseCtx.fileName();
            if (fn == null) continue;
            string name = fn.GetText();
            var fileSym = _ctx.Semantic.ResolveFile(name);
            if (fileSym == null) continue;

            var option = CloseOption.None;
            var optCtx = phraseCtx.closeOption();
            if (optCtx != null)
            {
                if (optCtx.LOCK() != null) option = CloseOption.Lock;
                else if (optCtx.NO() != null) option = CloseOption.NoRewind;
                else if (optCtx.REEL() != null) option = CloseOption.Reel;
                else if (optCtx.UNIT() != null) option = CloseOption.Unit;
            }
            phrases.Add(new BoundCloseFilePhrase(fileSym, option));
        }
        return new BoundCloseStatement(phrases);
    }

    // ── READ ──

    internal BoundStatement? BindRead(CobolParserCore.ReadStatementContext ctx)
    {
        var fileNameCtx = ctx.fileName();
        if (fileNameCtx == null) return null;

        string name = fileNameCtx.GetText();
        var fileSym = _ctx.Semantic.ResolveFile(name);
        if (fileSym == null) return null;

        // NEXT/PREVIOUS direction
        var direction = ReadDirection.None;
        var dirCtx = ctx.readDirection();
        if (dirCtx != null)
        {
            if (dirCtx.PREVIOUS() != null)
                direction = ReadDirection.Previous;
            else
                direction = ReadDirection.Next;
        }

        // KEY IS data-name
        string? keyDataName = null;
        if (ctx.readKey() is { } keyCtx)
            keyDataName = keyCtx.dataReference().cobolWord().GetText();

        // INTO clause
        BoundIdentifierExpression? intoId = null;
        var intoCtx = ctx.readInto();
        if (intoCtx != null)
        {
            var intoExpr = _ctx.Expression.BindDataReferenceWithSubscripts(intoCtx.dataReference());
            intoId = intoExpr as BoundIdentifierExpression;
        }

        // AT END / NOT AT END
        var atEnd = new List<BoundStatement>();
        var notAtEnd = new List<BoundStatement>();
        var atEndCtx = ctx.readAtEnd();
        if (atEndCtx != null)
        {
            var impStmts = atEndCtx.statementBlock();
            if (impStmts.Length >= 1)
            {
                foreach (var stmt in impStmts[0].statement())
                {
                    var bound = _ctx.BindStatement(stmt);
                    if (bound != null) atEnd.Add(bound);
                }
            }
            if (impStmts.Length >= 2)
            {
                foreach (var stmt in impStmts[1].statement())
                {
                    var bound = _ctx.BindStatement(stmt);
                    if (bound != null) notAtEnd.Add(bound);
                }
            }
        }

        // INVALID KEY / NOT INVALID KEY (separate from AT END for keyed/random reads)
        var invalidKey = new List<BoundStatement>();
        var notInvalidKey = new List<BoundStatement>();
        if (ctx.readInvalidKey() is { } ikCtx)
        {
            var impStmts = ikCtx.statementBlock();
            if (impStmts.Length >= 1)
                foreach (var stmt in impStmts[0].statement())
                {
                    var bound = _ctx.BindStatement(stmt);
                    if (bound != null) invalidKey.Add(bound);
                }
            if (impStmts.Length >= 2)
                foreach (var stmt in impStmts[1].statement())
                {
                    var bound = _ctx.BindStatement(stmt);
                    if (bound != null) notInvalidKey.Add(bound);
                }
        }

        return new BoundReadStatement(fileSym, intoId, direction, keyDataName, atEnd, notAtEnd, invalidKey, notInvalidKey);
    }

    // ── REWRITE ──

    internal BoundStatement? BindRewrite(CobolParserCore.RewriteStatementContext ctx)
    {
        var recordCtx = ctx.recordName();
        if (recordCtx == null) return null;

        string recordName = recordCtx.GetText();
        var recordSym = _ctx.Semantic.ResolveData(recordName);
        if (recordSym == null) return null;

        var fileSym = _ctx.Semantic.ResolveFileForRecord(recordSym);
        if (fileSym == null) return null;

        // FROM clause
        BoundExpression? from = null;
        var fromCtx = ctx.rewriteFrom()?.dataReference();
        if (fromCtx != null)
            from = _ctx.Expression.BindDataReferenceWithSubscripts(fromCtx);

        // INVALID KEY / NOT INVALID KEY
        var invalidKey = new List<BoundStatement>();
        var notInvalidKey = new List<BoundStatement>();
        if (ctx.rewriteInvalidKeyPhrase() is { } rikCtx)
        {
            var impStmts = rikCtx.statementBlock();
            if (impStmts.Length >= 1)
                foreach (var stmt in impStmts[0].statement())
                {
                    var bound = _ctx.BindStatement(stmt);
                    if (bound != null) invalidKey.Add(bound);
                }
            if (impStmts.Length >= 2)
                foreach (var stmt in impStmts[1].statement())
                {
                    var bound = _ctx.BindStatement(stmt);
                    if (bound != null) notInvalidKey.Add(bound);
                }
        }

        return new BoundRewriteStatement(fileSym, recordSym, from, invalidKey, notInvalidKey);
    }

    // ── DELETE ──

    internal BoundStatement? BindDelete(CobolParserCore.DeleteStatementContext ctx)
    {
        var fileNameCtx = ctx.fileName();
        if (fileNameCtx == null) return null;

        var fileSym = _ctx.Semantic.ResolveFile(fileNameCtx.GetText());
        if (fileSym == null) return null;

        var invalidKey = new List<BoundStatement>();
        var notInvalidKey = new List<BoundStatement>();
        if (ctx.deleteInvalidKeyPhrase() is { } ikCtx)
        {
            var impStmts = ikCtx.statementBlock();
            if (impStmts.Length >= 1)
                foreach (var stmt in impStmts[0].statement())
                {
                    var bound = _ctx.BindStatement(stmt);
                    if (bound != null) invalidKey.Add(bound);
                }
            if (impStmts.Length >= 2)
                foreach (var stmt in impStmts[1].statement())
                {
                    var bound = _ctx.BindStatement(stmt);
                    if (bound != null) notInvalidKey.Add(bound);
                }
        }

        return new BoundDeleteStatement(fileSym, invalidKey, notInvalidKey);
    }

    // ── START ──

    internal BoundStatement? BindStart(CobolParserCore.StartStatementContext ctx)
    {
        var fileNameCtx = ctx.fileName();
        if (fileNameCtx == null) return null;

        var fileSym = _ctx.Semantic.ResolveFile(fileNameCtx.GetText());
        if (fileSym == null) return null;

        // KEY IS relationalExpression (optional)
        BoundExpression? keyCondition = null;
        if (ctx.startKeyPhrase() is { } keyCtx)
            keyCondition = _ctx.Condition.BindComparison(keyCtx.comparisonExpression());

        var invalidKey = new List<BoundStatement>();
        var notInvalidKey = new List<BoundStatement>();
        if (ctx.startInvalidKeyPhrase() is { } ikCtx)
        {
            var impStmts = ikCtx.statementBlock();
            if (impStmts.Length >= 1)
                foreach (var stmt in impStmts[0].statement())
                {
                    var bound = _ctx.BindStatement(stmt);
                    if (bound != null) invalidKey.Add(bound);
                }
            if (impStmts.Length >= 2)
                foreach (var stmt in impStmts[1].statement())
                {
                    var bound = _ctx.BindStatement(stmt);
                    if (bound != null) notInvalidKey.Add(bound);
                }
        }

        return new BoundStartStatement(fileSym, keyCondition, invalidKey, notInvalidKey);
    }

    // ── RETURN ──

    internal BoundStatement? BindReturn(CobolParserCore.ReturnStatementContext ctx)
    {
        var fileNameCtx = ctx.fileName();
        if (fileNameCtx == null) return null;

        var fileSym = _ctx.Semantic.ResolveFile(fileNameCtx.GetText());
        if (fileSym == null) return null;

        // INTO clause
        BoundIdentifierExpression? intoId = null;
        var intoCtx = ctx.dataReference();
        if (intoCtx != null)
        {
            var intoExpr = _ctx.Expression.BindDataReferenceWithSubscripts(intoCtx);
            intoId = intoExpr as BoundIdentifierExpression;
        }

        // AT END / NOT AT END
        var atEnd = new List<BoundStatement>();
        var notAtEnd = new List<BoundStatement>();
        if (ctx.returnAtEndPhrase() is { } atEndCtx)
        {
            var impStmts = atEndCtx.statementBlock();
            if (impStmts.Length >= 1)
                foreach (var stmt in impStmts[0].statement())
                {
                    var bound = _ctx.BindStatement(stmt);
                    if (bound != null) atEnd.Add(bound);
                }
            if (impStmts.Length >= 2)
                foreach (var stmt in impStmts[1].statement())
                {
                    var bound = _ctx.BindStatement(stmt);
                    if (bound != null) notAtEnd.Add(bound);
                }
        }

        return new BoundReturnStatement(fileSym, intoId, atEnd, notAtEnd);
    }

    // ── SORT ──

    internal BoundStatement? BindSort(CobolParserCore.SortStatementContext ctx)
    {
        var fileNameCtx = ctx.sortFileName()?.dataReference();
        if (fileNameCtx == null) return null;

        var fileSym = _ctx.Semantic.ResolveFile(fileNameCtx.GetText());
        if (fileSym == null) return null;

        // Parse sort keys
        var keys = BindSortKeys(ctx.sortKeyPhrase(), fileSym);

        bool duplicates = ctx.sortDuplicatesPhrase() != null;

        // USING / INPUT PROCEDURE
        IReadOnlyList<FileSymbol>? usingFiles = null;
        ParagraphSymbol? inputProc = null, inputProcThru = null;
        if (ctx.sortUsingPhrase() is { } usingCtx)
        {
            usingFiles = ResolveFileList(usingCtx.dataReferenceList());
        }
        else if (ctx.sortInputProcedurePhrase() is { } inputCtx)
        {
            var procNames = inputCtx.procedureName();
            if (procNames.Length >= 1)
                inputProc = _ctx.ProcedureName.ResolveProcedureName(
                    ProcedureNameResolver.ExtractProcedureNameText(procNames[0]));
            if (procNames.Length >= 2)
                inputProcThru = _ctx.ProcedureName.ResolveProcedureNameForThruEnd(
                    ProcedureNameResolver.ExtractProcedureNameText(procNames[1]));
        }

        // GIVING / OUTPUT PROCEDURE
        IReadOnlyList<FileSymbol>? givingFiles = null;
        ParagraphSymbol? outputProc = null, outputProcThru = null;
        if (ctx.sortGivingPhrase() is { } givingCtx)
        {
            givingFiles = ResolveFileList(givingCtx.dataReferenceList());
        }
        else if (ctx.sortOutputProcedurePhrase() is { } outputCtx)
        {
            var procNames = outputCtx.procedureName();
            if (procNames.Length >= 1)
                outputProc = _ctx.ProcedureName.ResolveProcedureName(
                    ProcedureNameResolver.ExtractProcedureNameText(procNames[0]));
            if (procNames.Length >= 2)
                outputProcThru = _ctx.ProcedureName.ResolveProcedureNameForThruEnd(
                    ProcedureNameResolver.ExtractProcedureNameText(procNames[1]));
        }

        return new BoundSortStatement(fileSym, keys, duplicates,
            usingFiles, givingFiles,
            inputProc, inputProcThru,
            outputProc, outputProcThru);
    }

    // ── MERGE ──

    internal BoundStatement? BindMerge(CobolParserCore.MergeStatementContext ctx)
    {
        var fileNameCtx = ctx.mergeFileName()?.dataReference();
        if (fileNameCtx == null) return null;

        var fileSym = _ctx.Semantic.ResolveFile(fileNameCtx.GetText());
        if (fileSym == null) return null;

        var keys = BindMergeKeys(ctx.mergeKeyPhrase(), fileSym);

        // USING (required for MERGE)
        var usingFiles = ResolveFileList(ctx.mergeUsingPhrase().dataReferenceList());

        // GIVING / OUTPUT PROCEDURE
        IReadOnlyList<FileSymbol>? givingFiles = null;
        ParagraphSymbol? outputProc = null, outputProcThru = null;
        if (ctx.mergeGivingPhrase() is { } givingCtx)
        {
            givingFiles = ResolveFileList(givingCtx.dataReferenceList());
        }
        else if (ctx.mergeOutputProcedurePhrase() is { } outputCtx)
        {
            var procNames = outputCtx.procedureName();
            if (procNames.Length >= 1)
                outputProc = _ctx.ProcedureName.ResolveProcedureName(
                    ProcedureNameResolver.ExtractProcedureNameText(procNames[0]));
            if (procNames.Length >= 2)
                outputProcThru = _ctx.ProcedureName.ResolveProcedureNameForThruEnd(
                    ProcedureNameResolver.ExtractProcedureNameText(procNames[1]));
        }

        return new BoundMergeStatement(fileSym, keys, usingFiles, givingFiles,
            outputProc, outputProcThru);
    }

    // ── RELEASE ──

    internal BoundStatement? BindRelease(CobolParserCore.ReleaseStatementContext ctx)
    {
        // record-name-1 is the first dataReference — must be a record in an SD
        var recordRef = ctx.dataReference();
        if (recordRef == null) return null;

        string recordName = recordRef.GetText();
        var recordSym = _ctx.Semantic.ResolveData(recordName);
        if (recordSym == null) return null;

        // Find the SD file for this record
        var fileSym = _ctx.Semantic.ResolveFileForRecord(recordSym);
        if (fileSym == null) return null;

        // FROM clause
        BoundExpression? fromExpr = null;
        var fromCtx = ctx.releaseFrom()?.dataReference();
        if (fromCtx != null)
        {
            fromExpr = _ctx.Expression.BindDataReferenceWithSubscripts(fromCtx);
        }

        return new BoundReleaseStatement(fileSym, recordSym, fromExpr);
    }

    // ── Sort/merge key binding helpers ──

    internal List<BoundSortKey> BindSortKeys(
        CobolParserCore.SortKeyPhraseContext[] keyPhrases, FileSymbol file)
    {
        var keys = new List<BoundSortKey>();
        foreach (var phrase in keyPhrases)
        {
            bool ascending = phrase.ASCENDING() != null;
            var dataRefList = phrase.dataReferenceList();
            if (dataRefList == null) continue; // Format 2: KEY without data-names uses table's inherent KEY
            foreach (var dataRef in dataRefList.dataReference())
            {
                var keySym = _ctx.Semantic.ResolveData(dataRef.GetText());
                if (keySym != null)
                    keys.Add(new BoundSortKey(keySym, ascending));
            }
        }
        return keys;
    }

    internal List<BoundSortKey> BindMergeKeys(
        CobolParserCore.MergeKeyPhraseContext[] keyPhrases, FileSymbol file)
    {
        var keys = new List<BoundSortKey>();
        foreach (var phrase in keyPhrases)
        {
            bool ascending = phrase.ASCENDING() != null;
            foreach (var dataRef in phrase.dataReferenceList().dataReference())
            {
                var keySym = _ctx.Semantic.ResolveData(dataRef.GetText());
                if (keySym != null)
                    keys.Add(new BoundSortKey(keySym, ascending));
            }
        }
        return keys;
    }

    internal List<FileSymbol> ResolveFileList(CobolParserCore.DataReferenceListContext listCtx)
    {
        var files = new List<FileSymbol>();
        foreach (var dataRef in listCtx.dataReference())
        {
            var fileSym = _ctx.Semantic.ResolveFile(dataRef.GetText());
            if (fileSym != null)
                files.Add(fileSym);
        }
        return files;
    }

    // ── USE (declaratives) ──

    internal BoundUseStatement BindUse(CobolParserCore.UseStatementContext ctx)
    {
        bool isGlobal = ctx.GLOBAL() != null;

        // USE [GLOBAL] BEFORE REPORTING report-name
        if (ctx.BEFORE() != null && ctx.REPORTING() != null)
        {
            string reportName = ctx.procedureName() != null
                ? ProcedureNameResolver.ExtractProcedureNameText(ctx.procedureName())
                : "";
            return new BoundUseStatement(isBeforeReporting: true, isGlobal, [], reportName);
        }

        // USE [GLOBAL] AFTER STANDARD {EXCEPTION|ERROR} PROCEDURE ON {file-name+ | INPUT | OUTPUT | I-O | EXTEND}
        var target = ctx.useOnTarget();

        if (target.INPUT() != null)
            return new BoundUseStatement(false, isGlobal, [], reportName: null, targetMode: OpenMode.Input);
        if (target.OUTPUT() != null)
            return new BoundUseStatement(false, isGlobal, [], reportName: null, targetMode: OpenMode.Output);
        if (target.I_O() != null)
            return new BoundUseStatement(false, isGlobal, [], reportName: null, targetMode: OpenMode.IO);
        if (target.EXTEND() != null)
            return new BoundUseStatement(false, isGlobal, [], reportName: null, targetMode: OpenMode.Extend);

        // file-name+
        var fileNames = new List<string>();
        foreach (var fn in target.fileName())
        {
            fileNames.Add(fn.GetText());
        }
        return new BoundUseStatement(false, isGlobal, fileNames, reportName: null);
    }
}
