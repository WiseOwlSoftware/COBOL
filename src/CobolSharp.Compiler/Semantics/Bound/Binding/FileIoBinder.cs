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

        // A WRITE … ADVANCING marks the file as a printed-page (report/listing) file — ADVANCING is
        // the spec's vertical page-positioning feature (ISO §14.9.51), meaningful only for a printer.
        // Such a file is line-rendered (each record is a page line), not binary record-sequential.
        if (fileSym != null && advCtx != null)
            fileSym.WrittenWithAdvancing = true;

        BoundExpression? from = null;
        if (ctx.writeFrom() is { } fromCtx)
            from = _ctx.Expression.BindDataReferenceWithSubscripts(fromCtx.dataReference());

        // INVALID KEY / NOT INVALID KEY
        var invalidKey = new List<BoundStatement>();
        var notInvalidKey = new List<BoundStatement>();
        if (ctx.writeInvalidKey() is { } wikCtx)
        {
            DialectStrictnessChecks.CheckInvalidKeyNoiseWord(_ctx, wikCtx);
            BindInvalidKeyBlocks(wikCtx.statementBlock(), wikCtx.NOT(), invalidKey, notInvalidKey);
        }

        // AT END-OF-PAGE / NOT AT END-OF-PAGE (EOP) phrases — for LINAGE files (ISO §14.9.51).
        var atEndOfPage = new List<BoundStatement>();
        var notAtEndOfPage = new List<BoundStatement>();
        if (ctx.writeAtEndOfPage() is { } eopCtx)
        {
            var eopBlocks = eopCtx.statementBlock();
            if (eopBlocks.Length >= 1)
                foreach (var stmt in eopBlocks[0].statement())
                {
                    var bound = _ctx.BindStatement(stmt);
                    if (bound != null) atEndOfPage.Add(bound);
                }
            if (eopBlocks.Length >= 2)
                foreach (var stmt in eopBlocks[1].statement())
                {
                    var bound = _ctx.BindStatement(stmt);
                    if (bound != null) notAtEndOfPage.Add(bound);
                }
        }

        return new BoundWriteStatement(fileSym, recordSym, from, advancingLines, isAfterAdvancing, invalidKey, notInvalidKey,
            advancingExpression: advancingExpression, atEndOfPage: atEndOfPage, notAtEndOfPage: notAtEndOfPage);
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
            foreach (var spec in clause.openFileSpec())
            {
                // The REVERSED / WITH NO REWIND tape phrase (if any) is ignored at runtime; only the file
                // matters. REVERSED is an obsolete element, flagged under cobol85 (CBL3607).
                DialectStrictnessChecks.CheckObsoleteOpenReversed(_ctx, spec);
                string name = spec.dataReference().cobolWord().GetText();
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

    // ── Report Writer: INITIATE / GENERATE / TERMINATE (ISO §14.9.21/19/62) ──

    internal BoundStatement BindInitiate(CobolParserCore.InitiateStatementContext ctx)
    {
        var reports = new List<ReportSymbol>();
        foreach (var rn in ctx.reportName())
            if (_ctx.Semantic.ResolveReport(rn.GetText()) is { } r) reports.Add(r);
        return new BoundInitiateStatement(reports);
    }

    internal BoundStatement BindTerminate(CobolParserCore.TerminateStatementContext ctx)
    {
        var reports = new List<ReportSymbol>();
        foreach (var rn in ctx.reportName())
            if (_ctx.Semantic.ResolveReport(rn.GetText()) is { } r) reports.Add(r);
        return new BoundTerminateStatement(reports);
    }

    internal BoundStatement BindGenerate(CobolParserCore.GenerateStatementContext ctx)
    {
        string name = ctx.reportName().GetText();
        // GENERATE report-group-name (detail reporting, §14.9.19) — the common case.
        if (_ctx.Semantic.ResolveReportGroup(name) is { } rg)
            return new BoundGenerateStatement(rg.Report, rg.Group, BuildReportLines(rg.Group));
        // GENERATE report-name (summary reporting) — recognized but not yet produced; bind an empty
        // generate so the program still compiles and runs (no detail line emitted).
        if (_ctx.Semantic.ResolveReport(name) is { } report)
            return new BoundGenerateStatement(report, null, []);
        return new BoundCompoundStatement([]);
    }

    /// <summary>Flatten a report group's entries into print lines: a LINE clause starts a new line (with its
    /// advance), and each COLUMN+SOURCE item places a field on the current line.</summary>
    private IReadOnlyList<BoundReportLine> BuildReportLines(ReportGroupSymbol group)
    {
        var lines = new List<BoundReportLine>();
        List<BoundReportField>? fields = null;
        int advance = 1;
        bool nextPage = false;

        void Flush()
        {
            if (fields != null)
                lines.Add(new BoundReportLine(advance, nextPage, fields));
            fields = null;
        }

        foreach (var g in group.SelfAndDescendants())
        {
            if (g.HasLine || g.LineNextPage)
            {
                Flush();
                fields = [];
                advance = g.LineValue > 0 ? g.LineValue : 1;   // relative PLUS n (absolute handled later)
                nextPage = g.LineNextPage;
            }
            if (g.HasColumn)
            {
                fields ??= [];
                BoundExpression? src = null;
                if (g.SourceName != null)
                    src = BindReportSource(g.SourceName);
                else if (g.ValueLiteral != null)
                    // A VALUE literal in a body group is a constant printable field (ISO §13.18.63); the raw
                    // captured text (quotes included) is unwrapped at lowering time (ExtractLiteral).
                    src = new BoundLiteralExpression(g.ValueLiteral, CobolCategory.Alphanumeric);
                // (SUM-counter / special-register body fields are presented by later Report Writer increments.)
                if (src != null)
                    fields.Add(new BoundReportField(g.ColumnValue, g.FieldWidth, src));
            }
        }
        Flush();
        return lines;
    }

    private BoundExpression? BindReportSource(string name)
    {
        var sym = _ctx.Semantic.ResolveData(name);
        if (sym == null) return null;
        var cat = sym.ResolvedType?.Category ?? CobolCategory.Alphanumeric;
        return new BoundIdentifierExpression(sym, cat);
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
            DialectStrictnessChecks.CheckInvalidKeyNoiseWord(_ctx, ikCtx);
            BindInvalidKeyBlocks(ikCtx.statementBlock(), ikCtx.NOT(), invalidKey, notInvalidKey);
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
            DialectStrictnessChecks.CheckInvalidKeyNoiseWord(_ctx, rikCtx);
            BindInvalidKeyBlocks(rikCtx.statementBlock(), rikCtx.NOT(), invalidKey, notInvalidKey);
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
            DialectStrictnessChecks.CheckInvalidKeyNoiseWord(_ctx, ikCtx);
            BindInvalidKeyBlocks(ikCtx.statementBlock(), ikCtx.NOT(), invalidKey, notInvalidKey);
        }

        return new BoundDeleteStatement(fileSym, invalidKey, notInvalidKey);
    }

    // DELETE FILE (COBOL-2023, ISO §14.9.10): delete the physical file. The optional ON EXCEPTION phrases are
    // accepted by the grammar but not yet honored (documented follow-up).
    internal BoundStatement? BindDeleteFile(CobolParserCore.DeleteFileStatementContext ctx)
    {
        var fileNameCtx = ctx.fileName();
        if (fileNameCtx == null) return null;
        var fileSym = _ctx.Semantic.ResolveFile(fileNameCtx.GetText());
        if (fileSym == null) return null;
        return new BoundDeleteFileStatement(fileSym);
    }

    // ── START ──

    internal BoundStatement? BindStart(CobolParserCore.StartStatementContext ctx)
    {
        var fileNameCtx = ctx.fileName();
        if (fileNameCtx == null) return null;

        var fileSym = _ctx.Semantic.ResolveFile(fileNameCtx.GetText());
        if (fileSym == null) return null;

        // KEY [IS] relational-operator data-name (optional). The left operand is the key of
        // reference (implicit); we carry the operator on a BoundBinaryExpression so the lowerer can
        // map it to a StartCondition. (§14.9.41 GR8: if the KEY phrase is omitted, EQUAL is assumed.)
        BoundExpression? keyCondition = null;
        if (ctx.startKeyPhrase() is { } keyCtx)
        {
            // Operator omitted (KEY IS data-name) → EQUAL is assumed.
            var op = keyCtx.comparisonOperator() is { } opCtx
                ? ConditionBinder.ParseComparisonOperator(opCtx)
                : BoundBinaryOperatorKind.Equal;
            var keyExpr = _ctx.Expression.BindDataReferenceWithSubscripts(keyCtx.dataReference());
            keyCondition = new BoundBinaryExpression(keyExpr, op, keyExpr, CobolCategory.Unknown);
        }

        var invalidKey = new List<BoundStatement>();
        var notInvalidKey = new List<BoundStatement>();
        if (ctx.startInvalidKeyPhrase() is { } ikCtx)
        {
            DialectStrictnessChecks.CheckInvalidKeyNoiseWord(_ctx, ikCtx);
            BindInvalidKeyBlocks(ikCtx.statementBlock(), ikCtx.NOT(), invalidKey, notInvalidKey);
        }

        return new BoundStartStatement(fileSym, keyCondition, invalidKey, notInvalidKey);
    }

    /// <summary>
    /// Bind the INVALID KEY / NOT INVALID KEY statement blocks shared by the WRITE/READ/REWRITE/DELETE/START
    /// phrases. The grammar is identical for all five: <c>INVALID KEY? block (NOT INVALID KEY? block)? | NOT
    /// INVALID KEY? block</c>. TWO blocks ⇒ [0] is the INVALID-KEY block and [1] the NOT-INVALID-KEY block.
    /// ONE block WITH a NOT token ⇒ the standalone <c>NOT INVALID KEY block</c> form, whose single block is
    /// the NOT-INVALID-KEY block — NOT the INVALID-KEY block. (A NOT in the first alternative always brings a
    /// second block, so "one block + a NOT token" uniquely identifies the NOT-only form.) ONE block, no NOT ⇒
    /// the <c>INVALID KEY block</c> form. ISO §9.1.14. Binding the NOT-only block as the INVALID-KEY block was
    /// the RL205A (8 of 9 FAIL*) and IX108A failure.
    /// </summary>
    private void BindInvalidKeyBlocks(
        CobolParserCore.StatementBlockContext[] blocks, Antlr4.Runtime.Tree.ITerminalNode? notToken,
        List<BoundStatement> invalidKey, List<BoundStatement> notInvalidKey)
    {
        if (blocks.Length == 0) return;
        var firstTarget = blocks.Length == 1 && notToken != null ? notInvalidKey : invalidKey;
        foreach (var stmt in blocks[0].statement())
            if (_ctx.BindStatement(stmt) is { } bound) firstTarget.Add(bound);
        if (blocks.Length >= 2)
            foreach (var stmt in blocks[1].statement())
                if (_ctx.BindStatement(stmt) is { } bound) notInvalidKey.Add(bound);
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

        var targetName = fileNameCtx.GetText();

        // Format 2 detection: if target is a data item (not a file), it's a table sort
        var fileSym = _ctx.Semantic.ResolveFile(targetName);
        if (fileSym == null)
        {
            // Try resolving as a data item for Format 2 (table sort)
            var dataSym = _ctx.Semantic.ResolveData(targetName);
            if (dataSym != null)
                return BindTableSort(ctx, dataSym);
            return null;
        }

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
            (inputProc, inputProcThru) = ResolveSortMergeProcedure(inputCtx.procedureName());

        // GIVING / OUTPUT PROCEDURE
        IReadOnlyList<FileSymbol>? givingFiles = null;
        ParagraphSymbol? outputProc = null, outputProcThru = null;
        if (ctx.sortGivingPhrase() is { } givingCtx)
        {
            givingFiles = ResolveFileList(givingCtx.dataReferenceList());
        }
        else if (ctx.sortOutputProcedurePhrase() is { } outputCtx)
            (outputProc, outputProcThru) = ResolveSortMergeProcedure(outputCtx.procedureName());

        DialectStrictnessChecks.CheckCollatingNoiseWord(_ctx, ctx.sortCollatingPhrase());
        return new BoundSortStatement(fileSym, keys, duplicates,
            usingFiles, givingFiles,
            inputProc, inputProcThru,
            outputProc, outputProcThru,
            ExtractCollatingName(ctx.sortCollatingPhrase()));
    }

    /// <summary>
    /// alphabet-name-1 from a SORT/MERGE COLLATING SEQUENCE phrase (the alphanumeric
    /// collating sequence); null when the phrase is absent. alphabet-name-2 (national)
    /// is not captured — national sort keys are out of scope.
    /// </summary>
    private static string? ExtractCollatingName(CobolParserCore.SortCollatingPhraseContext? phrase)
        => phrase?.cobolWord() is { Length: > 0 } words ? words[0].GetText() : null;

    /// <summary>
    /// Resolve a SORT/MERGE INPUT/OUTPUT PROCEDURE phrase to (first paragraph, thru paragraph). With an
    /// explicit THRU the two procedure-names bound the range directly. With a SINGLE procedure-name that
    /// names a SECTION, the procedure is the WHOLE section (ISO §14.9.45/§14.9.24) — so it resolves to the
    /// section's first AND last paragraph, exactly as a plain <c>PERFORM section</c> does. (Resolving only
    /// the first paragraph with no THRU would run just that one paragraph and silently skip the rest of the
    /// section — e.g. a SORT INPUT PROCEDURE whose RELEASE statements live past the first paragraph would
    /// release nothing.)
    /// </summary>
    private (ParagraphSymbol? proc, ParagraphSymbol? thru) ResolveSortMergeProcedure(
        CobolParserCore.ProcedureNameContext[] procNames)
    {
        if (procNames.Length == 0) return (null, null);
        var (n1, q1) = ProcedureNameResolver.ExtractProcedureNameWithQualifier(procNames[0]);
        if (procNames.Length >= 2)
        {
            var (n2, q2) = ProcedureNameResolver.ExtractProcedureNameWithQualifier(procNames[1]);
            return (_ctx.ProcedureName.ResolveProcedureName(n1, q1),
                    _ctx.ProcedureName.ResolveProcedureNameForThruEnd(n2, q2));
        }
        // Single procedure-name: a paragraph runs alone (thru == null); a section runs in full.
        return _ctx.ProcedureName.ResolveProcedureNameForPerform(n1, q1);
    }

    // ── TABLE SORT (Format 2) ──

    private BoundStatement? BindTableSort(CobolParserCore.SortStatementContext ctx, DataSymbol tableSym)
    {
        // Resolve sort keys from the ON ASCENDING/DESCENDING KEY phrases
        var keys = new List<BoundSortKey>();
        foreach (var phrase in ctx.sortKeyPhrase())
        {
            bool ascending = phrase.ASCENDING() != null;
            var dataRefList = phrase.dataReferenceList();
            if (dataRefList != null)
            {
                foreach (var dataRef in dataRefList.dataReference())
                {
                    var keySym = _ctx.Semantic.ResolveData(dataRef.GetText());
                    if (keySym != null)
                        keys.Add(new BoundSortKey(keySym, ascending));
                }
            }
            else
            {
                // Format 2 without explicit key names: use the table's inherent KEY
                if (tableSym.Occurs?.AscendingKeys is { Count: > 0 } ascKeys)
                {
                    foreach (var keyName in ascKeys)
                    {
                        var keySym = _ctx.Semantic.ResolveData(keyName);
                        if (keySym != null) keys.Add(new BoundSortKey(keySym, true));
                    }
                }
                if (tableSym.Occurs?.DescendingKeys is { Count: > 0 } descKeys)
                {
                    foreach (var keyName in descKeys)
                    {
                        var keySym = _ctx.Semantic.ResolveData(keyName);
                        if (keySym != null) keys.Add(new BoundSortKey(keySym, false));
                    }
                }
                // ISO §14.9.40.4 GR23: if data-name-1 is omitted and the table has no inherent KEY, the table
                // item (data-name-2) is itself the key.
                if (keys.Count == 0)
                    keys.Add(new BoundSortKey(tableSym, ascending));
            }
        }

        bool duplicates = ctx.sortDuplicatesPhrase() != null;
        DialectStrictnessChecks.CheckCollatingNoiseWord(_ctx, ctx.sortCollatingPhrase());
        return new BoundTableSortStatement(tableSym, keys, duplicates,
            ExtractCollatingName(ctx.sortCollatingPhrase()));
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
            (outputProc, outputProcThru) = ResolveSortMergeProcedure(outputCtx.procedureName());

        DialectStrictnessChecks.CheckCollatingNoiseWord(_ctx, ctx.sortCollatingPhrase());
        return new BoundMergeStatement(fileSym, keys, usingFiles, givingFiles,
            outputProc, outputProcThru,
            ExtractCollatingName(ctx.sortCollatingPhrase()));
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
                var keySym = ResolveKeyDataReference(dataRef);
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
                var keySym = ResolveKeyDataReference(dataRef);
                if (keySym != null)
                    keys.Add(new BoundSortKey(keySym, ascending));
            }
        }
        return keys;
    }

    /// <summary>Resolve a SORT/MERGE key data reference to its data item, honoring OF/IN qualifiers. A
    /// qualified key such as <c>A-KEY OF SORT-KEY</c> must NOT be resolved via <c>GetText()</c> (which
    /// concatenates to "A-KEYOFSORT-KEY" and resolves to nothing — leaving the SORT/MERGE with no keys, so
    /// records come back in input order). The base name + qualifier chain resolve to the correct field.</summary>
    private DataSymbol? ResolveKeyDataReference(CobolParserCore.DataReferenceContext dataRef)
    {
        string baseName = dataRef.cobolWord().GetText();
        var quals = new List<string>();
        foreach (var suffix in dataRef.dataReferenceSuffix())
            if (suffix.qualification()?.cobolWord()?.GetText() is { } q)
                quals.Add(q);
        return _ctx.Semantic.ResolveQualifiedData(baseName, quals);
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
