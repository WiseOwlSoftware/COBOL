// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Runtime;
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.CodeGen.Lowering;
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
    private readonly CompilationOptions _options;

    // ── M002: Lowering context and lowerer instances ──
    // Created in constructor; methods will move to these classes in Stages 2-4.
    internal readonly LoweringContext _ctx;

    public Binder(SemanticModel semantic, DiagnosticBag diagnostics, CompilationOptions? options = null)
    {
        _semantic = semantic;
        _layout = new RecordLayoutBuilder();
        _diagnostics = diagnostics;
        _options = options ?? new CompilationOptions();

        // M002: Build lowering context with shared state
        _ctx = new LoweringContext(semantic, diagnostics, _options, _valueFactory);

        // M002: Create lowerer instances (empty shells — methods move in Stages 2-4)
        _ctx.Location = new LocationResolver(_ctx);
        _ctx.Expression = new ExpressionLowerer(_ctx);
        _ctx.Condition = new ConditionLowerer(_ctx);
        _ctx.ControlFlow = new ControlFlowLowerer(_ctx);
        _ctx.Arithmetic = new ArithmeticLowerer(_ctx);
        _ctx.DataMovement = new DataMovementLowerer(_ctx);
        _ctx.FileIo = new FileIoLowerer(_ctx);
        _ctx.String = new StringLowerer(_ctx);
        _ctx.LowerStatement = LowerStatement;
    }

    /// <summary>
    /// Build BoundProgram from parse tree, then lower to IrModule.
    /// </summary>
    public IrModule Bind(Antlr4.Runtime.ParserRuleContext tree)
    {
        // Phase 1: Build bound tree + validate
        var builder = new BoundTreeBuilder(_semantic, _diagnostics, _options);
        var boundProgram = builder.Build(tree);
        Semantics.ProcedureGraph.Analyze(boundProgram, _semantic, _diagnostics);
        Semantics.Bound.BoundTreeValidator.Validate(boundProgram, _diagnostics, _semantic);
        Semantics.FileStateValidator.Validate(boundProgram, _diagnostics);

        // Phase 2: Build record types
        var module = new IrModule(boundProgram.Program.Name);
        BuildRecordTypes(module);

        // Phase 3: Create paragraph method stubs
        CreateParagraphStubs(module, boundProgram);

        // Phase 3.5: Pre-scan for ALTER targets
        ScanAlterTargets(boundProgram);

        // Phase 4: Lower all paragraph bodies
        LowerAllParagraphs(boundProgram);

        // Phase 5: Populate module metadata + create entry point
        PopulateModuleMetadata(module, boundProgram);
        CreateEntryPoint(module, boundProgram);

        return module;
    }

    private void CreateParagraphStubs(IrModule module, BoundProgram boundProgram)
    {
        int paraIndex = 0;
        foreach (var para in boundProgram.Paragraphs)
        {
            var method = new IrMethod($"Para_{para.Symbol.Name}", returnType: IrPrimitiveType.Int32);
            method.Blocks.Add(new IrBasicBlock($"{para.Symbol.Name}_entry"));
            _ctx.ParagraphMethods[para.Symbol.Name] = method;
            _ctx.ParagraphIndices[para.Symbol.Name] = paraIndex;
            _ctx.ParagraphsByIndex.Add(para.Symbol.Name);
            _ctx.ParagraphSymbolMethods[para.Symbol] = method;
            _ctx.ParagraphSymbolIndices[para.Symbol] = paraIndex;
            module.Methods.Add(method);
            paraIndex++;
        }
    }

    private void ScanAlterTargets(BoundProgram boundProgram)
    {
        foreach (var para in boundProgram.Paragraphs)
            foreach (var sentence in para.Sentences)
                foreach (var stmt in sentence.Statements)
                    if (stmt is BoundAlterStatement alter)
                        foreach (var entry in alter.Entries)
                        {
                            string name = entry.TargetParagraph.Name;
                            if (!_ctx.AlterSlots.ContainsKey(name))
                            {
                                _ctx.AlterSlots[name] = _ctx.AlterSlots.Count;
                                _ctx.AlterDefaults.Add(-1);
                            }
                        }
    }

    private void LowerAllParagraphs(BoundProgram boundProgram)
    {
        foreach (var para in boundProgram.Paragraphs)
        {
            // Use symbol-based lookup to correctly handle duplicate paragraph names
            // in different sections. Fall back to name-based for compatibility.
            if (!_ctx.ParagraphSymbolMethods.TryGetValue(para.Symbol, out var method)
                && !_ctx.ParagraphMethods.TryGetValue(para.Symbol.Name, out method))
                continue;

            if (!_ctx.ParagraphSymbolIndices.TryGetValue(para.Symbol, out int myIndex))
                myIndex = _ctx.ParagraphIndices.GetValueOrDefault(para.Symbol.Name);
            var block = method.Blocks[0];
            _ctx.CurrentParagraphName = para.Symbol.Name;

            var paraEnd = method.CreateBlock($"{para.Symbol.Name}_exit");
            _ctx.ParagraphEndBlock = paraEnd;

            // EXIT SECTION target
            var sectionName = _semantic.GetParagraphSection(para.Symbol.Name);
            _ctx.SectionExitReturnIndex = null;
            if (sectionName != null)
            {
                var sectionParas = _semantic.GetSectionParagraphs(sectionName);
                if (sectionParas is { Count: > 0 }
                    && _ctx.ParagraphIndices.TryGetValue(sectionParas[^1], out var lastIdx))
                    _ctx.SectionExitReturnIndex = lastIdx + 1;
            }

            for (int si = 0; si < para.Sentences.Count; si++)
            {
                var sentenceEnd = new IrBasicBlock($"{para.Symbol.Name}_sent{si}_end");
                _ctx.CurrentSentenceEnd = sentenceEnd;

                foreach (var stmt in para.Sentences[si].Statements)
                    block = LowerStatement(stmt, method, block);

                block.Instructions.Add(new IrJump(sentenceEnd));
                method.Blocks.Add(sentenceEnd);
                block = sentenceEnd;
            }

            _ctx.CurrentSentenceEnd = null;
            _ctx.ParagraphEndBlock = null;
            _ctx.SectionExitReturnIndex = null;
            _ctx.CurrentParagraphName = null;

            block.Instructions.Add(new IrJump(paraEnd));
            method.Blocks.Add(paraEnd);
            paraEnd.Instructions.Add(new IrReturnConst(myIndex + 1));
        }
    }

    private void PopulateModuleMetadata(IrModule module, BoundProgram boundProgram)
    {
        module.AlterDefaults.AddRange(_ctx.AlterDefaults);
        module.IsInitial = _semantic.Program.IsInitial;

        foreach (var param in _semantic.ProcedureUsingParameters)
            module.UsingParameterNames.Add(param.Name);

        foreach (var para in boundProgram.Paragraphs)
            foreach (var sentence in para.Sentences)
                foreach (var stmt in sentence.Statements)
                    if (stmt is BoundEntryStatement entry)
                        module.EntryPoints.Add((entry.EntryName, entry.UsingParameters));
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

        // Register file handlers at startup for each SELECT (skip SD sort-merge files)
        foreach (var fileSym in _semantic.Symbols.Program.GlobalScope.GetAllSymbols<FileSymbol>())
        {
            // Sort-merge files (SD) don't have physical handlers
            if (fileSym.IsSortMerge) continue;

            // Resolve external path: a literal ASSIGN target ("TFIL1", "TF002") is an explicit,
            // possibly-shared physical name (e.g. NIST producer/consumer files) and is used as-is.
            // A non-literal target (an implementor-name / identifier, like NIST's bare XXXXX014)
            // names a file PRIVATE to this program; qualify it with the program-id so two programs
            // that happen to use the same SELECT name (e.g. SQ130A and SQ156A both SELECT SQ-FS1)
            // do not collide on one host file — which would let one program's leftover file defeat
            // another's "file is absent" semantics.
            string externalName = (fileSym.AssignIsLiteral && fileSym.AssignTarget != null)
                ? fileSym.AssignTarget
                : $"{boundProgram.Program.Name}-{fileSym.Name}";
            string externalPath = FileRuntime.ResolveHostPath(externalName);

            int recordLength = fileSym.RecordLength;
            if (recordLength == 0 && fileSym.Record != null)
            {
                if (_semantic.IsVariableLengthRecord(fileSym))
                {
                    // Variable-length records (RECORD VARYING, Format-2 RECORD CONTAINS m TO n, or multiple
                    // differently-sized 01s): the handler's slot/buffer must hold the MAXIMUM record, not the
                    // first 01 (which may be the minimum — RL-FR6's 56-byte 6A precedes its 102-byte 6B), or
                    // LONG records get truncated to the min (ISO §13.18.43). Per-slot/per-record length
                    // framing then recovers each record's actual length on READ.
                    recordLength = _semantic.MaxRecordLength(fileSym);
                }
                else
                {
                    var recLoc = _semantic.GetStorageLocation(fileSym.Record);
                    if (recLoc.HasValue)
                        recordLength = recLoc.Value.Length;
                }
            }
            if (recordLength == 0) recordLength = 132; // Default for print files

            // Per ISO §9.1.2 / §12.4.5.2: ORGANIZATION SEQUENTIAL (and the unspecified default) is RECORD
            // sequential — fixed-length records stored contiguously, no line delimiters — so REWRITE can
            // replace a record in place and READ/REWRITE are length-consistent (a line-sequential file
            // cannot, because trimming + newline framing makes records variable on disk).
            //
            // A line-RENDERED host representation (trimmed text, one line per record) is used for:
            //   • ORGANIZATION LINE SEQUENTIAL (the explicit text organization), and
            //   • printer/report files — those written with WRITE … ADVANCING (§14.9.51 vertical page
            //     positioning) or declared with a LINAGE clause (§13.18.30 logical page). These are the
            //     spec's printer-file features; real implementations key the same decision off the
            //     ASSIGN device (IBM SYSOUT, MF PRINTER), which the NIST suite encodes as XXXXX055. A
            //     printer file's records are page lines, never read back as binary records.
            // Everything else is record-sequential binary.
            bool lineSequential = fileSym.Organization == "LINE SEQUENTIAL"
                || fileSym.WrittenWithAdvancing
                || fileSym.LinageBody > 0;

            string org = fileSym.Organization ?? "SEQUENTIAL";
            int keyOffset = 0, keyLength = 0;

            // For INDEXED files, resolve RECORD KEY to get offset/length
            if (org == "INDEXED" && fileSym.RecordKey != null)
            {
                var keySym = _semantic.ResolveKeyData(fileSym, -1);
                if (keySym != null)
                {
                    var keyLoc = _semantic.GetStorageLocation(keySym);
                    if (keyLoc.HasValue)
                    {
                        // Key offset is relative to the record's start
                        var recordSym = fileSym.Record;
                        if (recordSym != null)
                        {
                            var recordLoc = _semantic.GetStorageLocation(recordSym);
                            if (recordLoc.HasValue)
                                keyOffset = keyLoc.Value.Offset - recordLoc.Value.Offset;
                        }
                        keyLength = keyLoc.Value.Length;
                    }
                }
            }
            // For RELATIVE files, carry the RELATIVE KEY data item's digit capacity in keyLength so
            // the runtime can raise boundary/overflow status when a relative record number exceeds it
            // (ISO §9.1.13.4). Relative files have no record-embedded key offset/length.
            else if (org == "RELATIVE" && fileSym.RelativeKey != null)
            {
                var keySym = _semantic.ResolveData(fileSym.RelativeKey);
                if (keySym != null)
                {
                    var keyLoc = _semantic.GetStorageLocation(keySym);
                    if (keyLoc.HasValue)
                        keyLength = keyLoc.Value.Pic.TotalDigits;
                }
            }

            var nameVal = _valueFactory.Next(IrPrimitiveType.String);
            var pathVal = _valueFactory.Next(IrPrimitiveType.String);
            var recLenVal = _valueFactory.Next(IrPrimitiveType.Int32);
            var lineSeqVal = _valueFactory.Next(IrPrimitiveType.Bool);
            var orgVal = _valueFactory.Next(IrPrimitiveType.String);
            var keyOffVal = _valueFactory.Next(IrPrimitiveType.Int32);
            var keyLenVal = _valueFactory.Next(IrPrimitiveType.Int32);
            block.Instructions.Add(new IrLoadConst(nameVal, fileSym.Name));
            block.Instructions.Add(new IrLoadConst(pathVal, externalPath));
            block.Instructions.Add(new IrLoadConst(recLenVal, recordLength));
            block.Instructions.Add(new IrLoadConst(lineSeqVal, lineSequential));
            block.Instructions.Add(new IrLoadConst(orgVal, org));
            block.Instructions.Add(new IrLoadConst(keyOffVal, keyOffset));
            block.Instructions.Add(new IrLoadConst(keyLenVal, keyLength));
            block.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.RegisterFileHandlerWithOrg",
                new[] { nameVal, pathVal, recLenVal, lineSeqVal, orgVal, keyOffVal, keyLenVal }));

            // A record-sequential file with variable-length records (RECORD IS VARYING or multiple 01
            // sizes) stores each record length-framed so lengths round-trip without line delimiters
            // (ISO §13.18.43 — the length-determination method is implementor-defined). Line-sequential
            // files frame by newline instead, so the flag only applies to record-sequential.
            if (!lineSequential && org == "SEQUENTIAL" && _semantic.IsVariableLengthSequential(fileSym))
            {
                var seqVarNameVal = _valueFactory.Next(IrPrimitiveType.String);
                var seqVarVal = _valueFactory.Next(IrPrimitiveType.Bool);
                // Convey the RECORD IS VARYING size bounds (min..max) so the runtime can enforce the
                // ISO §9.1.13 status-44 boundary check on a variable WRITE (a record longer than the
                // largest or shorter than the smallest permitted). min = 0 ⇒ no lower bound.
                var seqVarMinVal = _valueFactory.Next(IrPrimitiveType.Int32);
                var seqVarMaxVal = _valueFactory.Next(IrPrimitiveType.Int32);
                block.Instructions.Add(new IrLoadConst(seqVarNameVal, fileSym.Name));
                block.Instructions.Add(new IrLoadConst(seqVarVal, true));
                block.Instructions.Add(new IrLoadConst(seqVarMinVal, fileSym.RecordVaryingMin));
                block.Instructions.Add(new IrLoadConst(seqVarMaxVal, _semantic.MaxRecordLength(fileSym)));
                block.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.SetSequentialVarying",
                    new[] { seqVarNameVal, seqVarVal, seqVarMinVal, seqVarMaxVal }));
            }

            // INDEXED access mode: SEQUENTIAL (or unspecified — the indexed default) deletes/rewrites the
            // current (last-read) record and requires a preceding successful READ (43 if not); RANDOM/DYNAMIC
            // delete/rewrite the record identified by the primary key with no prior read (ISO §9.1.13.6).
            if (org == "INDEXED")
            {
                bool ixSequential = fileSym.AccessMode is null or "SEQUENTIAL";
                var ixNameVal = _valueFactory.Next(IrPrimitiveType.String);
                var ixSeqVal = _valueFactory.Next(IrPrimitiveType.Bool);
                block.Instructions.Add(new IrLoadConst(ixNameVal, fileSym.Name));
                block.Instructions.Add(new IrLoadConst(ixSeqVal, ixSequential));
                block.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.SetIndexedAccess",
                    new[] { ixNameVal, ixSeqVal }));

                // Variable-length records (RECORD IS VARYING or multiple 01 sizes): each record carries its
                // own length, stored length-framed so a SHORT vs LONG record round-trips with the right
                // length (ISO §13.18.43). Must agree with FileIoLowerer.IsVaryingRecord (INDEXED branch).
                if (fileSym.IsRecordVarying || _semantic.HasMultipleRecordSizes(fileSym))
                {
                    var ixvNameVal = _valueFactory.Next(IrPrimitiveType.String);
                    var ixvVal = _valueFactory.Next(IrPrimitiveType.Bool);
                    // RECORD IS VARYING size bounds (min..max) for the ISO §9.1.13 status-44 WRITE
                    // boundary check; min = 0 ⇒ no lower bound (e.g. multiple-01 sizes, no FROM).
                    var ixvMinVal = _valueFactory.Next(IrPrimitiveType.Int32);
                    var ixvMaxVal = _valueFactory.Next(IrPrimitiveType.Int32);
                    block.Instructions.Add(new IrLoadConst(ixvNameVal, fileSym.Name));
                    block.Instructions.Add(new IrLoadConst(ixvVal, true));
                    block.Instructions.Add(new IrLoadConst(ixvMinVal, fileSym.RecordVaryingMin));
                    block.Instructions.Add(new IrLoadConst(ixvMaxVal, _semantic.MaxRecordLength(fileSym)));
                    block.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.SetIndexedVarying",
                        new[] { ixvNameVal, ixvVal, ixvMinVal, ixvMaxVal }));
                }
            }

            // Register alternate keys for INDEXED files
            if (org == "INDEXED")
            {
                for (int ak = 0; ak < fileSym.AlternateKeys.Count; ak++)
                {
                    var altKey = fileSym.AlternateKeys[ak];
                    var altKeySym = _semantic.ResolveKeyData(fileSym, ak);
                    if (altKeySym == null) continue;
                    var altKeyLoc = _semantic.GetStorageLocation(altKeySym);
                    if (!altKeyLoc.HasValue) continue;

                    int altKeyOffset = altKeyLoc.Value.Offset;
                    var recordSym2 = fileSym.Record;
                    if (recordSym2 != null)
                    {
                        var recordLoc2 = _semantic.GetStorageLocation(recordSym2);
                        if (recordLoc2.HasValue)
                            altKeyOffset = altKeyLoc.Value.Offset - recordLoc2.Value.Offset;
                    }

                    var altNameVal = _valueFactory.Next(IrPrimitiveType.String);
                    var altOffVal = _valueFactory.Next(IrPrimitiveType.Int32);
                    var altLenVal = _valueFactory.Next(IrPrimitiveType.Int32);
                    var altDupVal = _valueFactory.Next(IrPrimitiveType.Bool);
                    block.Instructions.Add(new IrLoadConst(altNameVal, fileSym.Name));
                    block.Instructions.Add(new IrLoadConst(altOffVal, altKeyOffset));
                    block.Instructions.Add(new IrLoadConst(altLenVal, altKeyLoc.Value.Length));
                    block.Instructions.Add(new IrLoadConst(altDupVal, altKey.AllowDuplicates));
                    block.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.RegisterAlternateKey",
                        new[] { altNameVal, altOffVal, altLenVal, altDupVal }));
                }
            }

            // SELECT OPTIONAL
            if (fileSym.IsOptional)
            {
                var optNameVal = _valueFactory.Next(IrPrimitiveType.String);
                block.Instructions.Add(new IrLoadConst(optNameVal, fileSym.Name));
                block.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.SetFileOptional",
                    new[] { optNameVal }));
            }

            // LINAGE clause
            if (fileSym.LinageBody > 0)
            {
                var linNameVal = _valueFactory.Next(IrPrimitiveType.String);
                var linBodyVal = _valueFactory.Next(IrPrimitiveType.Int32);
                var linFootVal = _valueFactory.Next(IrPrimitiveType.Int32);
                var linTopVal = _valueFactory.Next(IrPrimitiveType.Int32);
                var linBotVal = _valueFactory.Next(IrPrimitiveType.Int32);
                block.Instructions.Add(new IrLoadConst(linNameVal, fileSym.Name));
                block.Instructions.Add(new IrLoadConst(linBodyVal, fileSym.LinageBody));
                block.Instructions.Add(new IrLoadConst(linFootVal, fileSym.LinageFooting));
                block.Instructions.Add(new IrLoadConst(linTopVal, fileSym.LinageTop));
                block.Instructions.Add(new IrLoadConst(linBotVal, fileSym.LinageBottom));
                block.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.SetFileLinage",
                    new[] { linNameVal, linBodyVal, linFootVal, linTopVal, linBotVal }));
            }

            // RELATIVE access mode: RANDOM/DYNAMIC position WRITE/REWRITE/DELETE by the RELATIVE KEY;
            // SEQUENTIAL (or unspecified) appends on WRITE and uses the current record.
            if (org == "RELATIVE")
            {
                bool sequential = fileSym.AccessMode is null or "SEQUENTIAL";
                var relNameVal = _valueFactory.Next(IrPrimitiveType.String);
                var relSeqVal = _valueFactory.Next(IrPrimitiveType.Bool);
                block.Instructions.Add(new IrLoadConst(relNameVal, fileSym.Name));
                block.Instructions.Add(new IrLoadConst(relSeqVal, sequential));
                block.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.SetRelativeAccess",
                    new[] { relNameVal, relSeqVal }));

                // RECORD IS VARYING on a relative file: each slot stores its own length (persisted as a
                // length prefix). Must agree with FileIoLowerer.IsVaryingRecord (relative → explicit clause).
                if (fileSym.IsRecordVarying)
                {
                    var relVarNameVal = _valueFactory.Next(IrPrimitiveType.String);
                    var relVarVal = _valueFactory.Next(IrPrimitiveType.Bool);
                    // RECORD IS VARYING size bounds (min..max) for the ISO §9.1.13 status-44 WRITE
                    // boundary check; min = 0 ⇒ no lower bound.
                    var relVarMinVal = _valueFactory.Next(IrPrimitiveType.Int32);
                    var relVarMaxVal = _valueFactory.Next(IrPrimitiveType.Int32);
                    block.Instructions.Add(new IrLoadConst(relVarNameVal, fileSym.Name));
                    block.Instructions.Add(new IrLoadConst(relVarVal, true));
                    block.Instructions.Add(new IrLoadConst(relVarMinVal, fileSym.RecordVaryingMin));
                    block.Instructions.Add(new IrLoadConst(relVarMaxVal, _semantic.MaxRecordLength(fileSym)));
                    block.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.SetRelativeVarying",
                        new[] { relVarNameVal, relVarVal, relVarMinVal, relVarMaxVal }));
                }
            }
        }

        // Collect paragraph methods for the Entry dispatch in paragraph-index order — INCLUDING
        // declaratives — so ParagraphDispatchOrder[pc] resolves the paragraph whose index is pc.
        // Every pc value (fall-through myIndex+1, GO TO, PERFORM THRU, GO TO DEPENDING) is in this
        // declarative-inclusive index space; excluding declaratives here previously left the switch
        // off by the number of leading DECLARATIVES paragraphs, so any program with declaratives
        // dispatched to the wrong paragraph and looped forever (DEVLOG: SQ105A hang).
        //
        // Declaratives are only ENTERED via PERFORM from the USE handler (EmitPerformDeclarativeSection,
        // which emits IrPerform / IrPerformThru — direct calls, not this switch), so the main loop
        // never lands on a declarative index: it starts at EntryParagraphIndex (the first non-
        // declarative paragraph) and main-flow control never targets a declarative.
        // Symbol-based lookup so each list position holds THIS paragraph's own method — duplicate
        // paragraph names in different sections each get their own entry (a name-based lookup would
        // put the last-defined duplicate's method at every same-name position). This must agree with
        // GO TO / GO TO DEPENDING / fall-through, which all resolve the bound ParagraphSymbol's index
        // (ControlFlowLowerer.TryResolveParagraphIndex, ParagraphSymbolIndices). Name fallback for
        // safety; a paragraph always resolves, so every paragraph is added and list position == index.
        int firstNonDeclarative = -1;
        int idx = 0;
        foreach (var para in boundProgram.Paragraphs)
        {
            if (firstNonDeclarative < 0 && !para.IsDeclarative)
                firstNonDeclarative = idx;
            if (_ctx.ParagraphSymbolMethods.TryGetValue(para.Symbol, out var m)
                || _ctx.ParagraphMethods.TryGetValue(para.Symbol.Name, out m))
                module.ParagraphDispatchOrder.Add(m);
            idx++;
        }
        module.EntryParagraphIndex = firstNonDeclarative < 0 ? 0 : firstNonDeclarative;

        // Main calls Entry(Array.Empty<CobolDataPointer>()) — dispatch loop is in Entry
        block.Instructions.Add(new IrRuntimeCall(null, "Self.Entry", Array.Empty<IrValue>()));

        main.Blocks.Add(block);
        module.Methods.Insert(0, main);
    }

    // ── Statement dispatch — routes directly to lowerers via _ctx ──

    private IrBasicBlock LowerStatement(BoundStatement stmt, IrMethod method, IrBasicBlock block)
    {
        switch (stmt)
        {
            case BoundCompoundStatement compound:
                foreach (var s in compound.Statements)
                    block = LowerStatement(s, method, block);
                return block;

            // ── Inline (stays in Binder) ──
            case BoundDisplayStatement disp:
                LowerDisplay(disp, block);
                break;
            case BoundAcceptStatement acc:
                LowerAccept(acc, block);
                break;
            case BoundCallStatement call:
                return LowerCall(call, method, block);
            case BoundCancelStatement cancel:
                foreach (var target in cancel.Targets)
                {
                    IrLocation? targetLoc = null;
                    if (target.IsDynamic)
                    {
                        // CANCEL identifier: read the program-name from the data item at runtime.
                        var targetSym = _semantic.ResolveData(target.Name);
                        if (targetSym != null)
                            targetLoc = _ctx.Location.ResolveLocation(targetSym);
                    }
                    block.Instructions.Add(new IrCancelProgram(target.Name, target.IsDynamic, targetLoc));
                }
                break;
            case BoundStopStatement:
                block.Instructions.Add(new IrStopRun());
                break;
            case BoundExitProgramStatement:
                block.Instructions.Add(new IrExitProgram());
                break;
            case BoundGoBackStatement:
                block.Instructions.Add(new IrGoBack());
                break;
            case BoundEntryStatement:
            case BoundExitStatement:
            case BoundUseStatement:
                break;
            case BoundSetSwitchStatement setSwitch:
                foreach (var (implName, setToOn) in setSwitch.Switches)
                    block.Instructions.Add(new IrSetSwitch(implName, setToOn));
                break;

            // ── Data movement → _ctx.DataMovement ──
            case BoundMoveStatement mv:
                _ctx.DataMovement.LowerMove(mv, block);
                break;
            case BoundCorrespondingStatement corr:
                return _ctx.DataMovement.LowerCorresponding(corr, method, block);
            case BoundInitializeStatement init:
                _ctx.DataMovement.LowerInitialize(init, block);
                break;
            case BoundSetConditionStatement setCond:
                _ctx.DataMovement.LowerSetCondition(setCond, block);
                break;
            case BoundSetIndexStatement setIdx:
                _ctx.DataMovement.LowerSetIndex(setIdx, block);
                break;

            // ── Arithmetic → _ctx.Arithmetic ──
            case BoundArithmeticStatement arith:
                return _ctx.Arithmetic.LowerArithmetic(arith, method, block);

            // ── Control flow → _ctx.ControlFlow ──
            case BoundPerformStatement perf:
                return _ctx.ControlFlow.LowerPerform(perf, method, block);
            case BoundIfStatement iff:
                return _ctx.ControlFlow.LowerIf(iff, method, block);
            case BoundEvaluateStatement eval:
                return _ctx.ControlFlow.LowerEvaluate(eval, method, block);
            case BoundGoToStatement gt:
                _ctx.ControlFlow.LowerGoTo(gt, block);
                break;
            case BoundAlterStatement alter:
                _ctx.ControlFlow.LowerAlter(alter, block);
                break;
            case BoundExitPerformStatement exitPerf:
                return _ctx.ControlFlow.LowerExitPerform(exitPerf, method, block);
            case BoundExitParagraphStatement:
                return _ctx.ControlFlow.LowerExitParagraph(method, block);
            case BoundExitSectionStatement:
                return _ctx.ControlFlow.LowerExitSection(method, block);
            case BoundNextSentenceStatement:
                return _ctx.ControlFlow.LowerNextSentence(method, block);
            case BoundSearchStatement search:
                return _ctx.ControlFlow.LowerSearch(search, method, block);
            case BoundSearchAllStatement searchAll:
                return _ctx.ControlFlow.LowerSearchAll(searchAll, method, block);

            // ── File I/O → _ctx.FileIo ──
            case BoundWriteStatement wr:
                return _ctx.FileIo.LowerWrite(wr, method, block);
            case BoundOpenStatement open:
                return _ctx.FileIo.LowerOpen(open, method, block);
            case BoundCloseStatement close:
                return _ctx.FileIo.LowerClose(close, method, block);
            case BoundReadStatement read:
                return _ctx.FileIo.LowerRead(read, method, block);
            case BoundRewriteStatement rw:
                return _ctx.FileIo.LowerRewrite(rw, method, block);
            case BoundDeleteStatement del:
                return _ctx.FileIo.LowerDelete(del, method, block);
            case BoundStartStatement start:
                return _ctx.FileIo.LowerStart(start, method, block);
            case BoundReturnStatement ret:
                return _ctx.FileIo.LowerReturn(ret, method, block);
            case BoundSortStatement sort:
                return _ctx.FileIo.LowerSort(sort, method, block);
            case BoundTableSortStatement tableSort:
                return _ctx.FileIo.LowerTableSort(tableSort, method, block);
            case BoundMergeStatement merge:
                return _ctx.FileIo.LowerMerge(merge, method, block);
            case BoundReleaseStatement release:
                return _ctx.FileIo.LowerRelease(release, method, block);

            // ── String operations → _ctx.String ──
            case BoundInspectStatement insp:
                _ctx.String.LowerInspect(insp, block);
                break;
            case BoundStringStatement str:
                return _ctx.String.LowerString(str, method, block);
            case BoundUnstringStatement unstr:
                return _ctx.String.LowerUnstring(unstr, method, block);
        }
        return block;
    }

    // ── DISPLAY (inline — too simple to extract) ──

    private void LowerDisplay(BoundDisplayStatement disp, IrBasicBlock block)
    {
        var operands = new List<IR.DisplayOperand>();
        foreach (var op in disp.Operands)
        {
            if (op is BoundFigurativeExpression fig)
            {
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
            else if (op is BoundIdentifierExpression or BoundReferenceModificationExpression)
            {
                var loc = _ctx.Location.ResolveExpressionLocation(op);
                if (loc != null)
                    operands.Add(new IR.DisplayFieldOperand(loc));
                else if (op is BoundIdentifierExpression failedId)
                    operands.Add(new IR.DisplayLiteralOperand($"[{failedId.Symbol.Name}]"));
            }
            else
            {
                operands.Add(new IR.DisplayLiteralOperand(op.ToString() ?? ""));
            }
        }

        block.Instructions.Add(new IR.IrPicDisplay(operands));
    }

    // ── ACCEPT (inline — 4 lines) ──

    private void LowerAccept(BoundAcceptStatement stmt, IrBasicBlock block)
    {
        var loc = _ctx.Location.ResolveLocation(stmt.Target);
        if (loc == null) return;
        block.Instructions.Add(new IrAccept(loc, stmt.Source));
    }

    // ── CALL (inline — cross-cutting, uses location + condition) ──

    private IrBasicBlock LowerCall(BoundCallStatement call, IrMethod method, IrBasicBlock block)
    {
        var args = new List<IrCallArgument>();
        foreach (var arg in call.Arguments)
        {
            var loc = _ctx.Location.ResolveExpressionLocation(arg.Expression);
            if (loc != null)
            {
                int mode = arg.Mode switch
                {
                    ParameterMode.ByReference => 0,
                    ParameterMode.ByContent => 1,
                    ParameterMode.ByValue => 2,
                    _ => 0
                };
                args.Add(new IrCallArgument(mode, loc));
            }
        }

        IrLocation? returningLoc = null;
        if (call.ReturningTarget != null)
            returningLoc = _ctx.Location.ResolveLocation(call.ReturningTarget);

        IrLocation? targetLoc = null;
        if (call.IsDynamic)
        {
            var targetSym = _semantic.ResolveData(call.TargetName);
            if (targetSym != null)
                targetLoc = _ctx.Location.ResolveLocation(targetSym);
        }

        block.Instructions.Add(new IrCallProgram(
            call.TargetName, call.IsDynamic, args, returningLoc, targetLoc));

        if (call.OnException.Count > 0 || call.NotOnException.Count > 0)
        {
            var callResult = _valueFactory.Next(IrPrimitiveType.Bool);
            block.Instructions.Add(new IrCheckCallException(call.TargetName, callResult));
            return _ctx.Condition.LowerConditionalBranch(
                call.OnException, call.NotOnException, callResult, method, block, "call");
        }

        return block;
    }
}
