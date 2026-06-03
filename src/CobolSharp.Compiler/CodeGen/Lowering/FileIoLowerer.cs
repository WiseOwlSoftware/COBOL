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

    public IrBasicBlock LowerWrite(BoundWriteStatement wr, IrMethod method, IrBasicBlock block)
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

            // RELATIVE random/dynamic WRITE positions by the RELATIVE KEY (ISO §14.9.51): convey the
            // program's key value to the runtime before the write.
            if (IsRelativeRandom(wr.File) && ResolveRelativeKeyLocation(wr.File) is { } wKey)
                block.Instructions.Add(new IrSetRelativeKey(fileName, wKey));

            if (wr.AdvancingLines.HasValue)
            {
                IrLocation? advLoc = null;
                if (wr.AdvancingExpression != null)
                    advLoc = _ctx.Location.ResolveExpressionLocation(wr.AdvancingExpression);
                block.Instructions.Add(new IrWriteAdvancing(
                    fileName, recordLoc, wr.AdvancingLines.Value, !wr.IsAfterAdvancing,
                    advancingLocation: advLoc));
            }
            else if (IsVaryingRecord(wr.File))
            {
                // RECORD IS VARYING: write without trailing-space trimming. With DEPENDING ON the length
                // is the depending item's runtime value; otherwise it is the written record's own declared
                // size (ISO §13.18.43 / §14.9.51). A lengthLoc of null selects the latter. For a RELATIVE
                // file the variable write stores the record at its actual length in its slot (the slot
                // carries a per-record length); the RELATIVE KEY handling above/below still applies.
                var lengthLoc = ResolveRecordLengthLocation(wr.File);
                block.Instructions.Add(new IrWriteRecordVariable(fileName, recordLoc, lengthLoc));
            }
            else
            {
                block.Instructions.Add(new IrWriteRecordFromStorage(fileName, recordLoc));
            }

            // RELATIVE sequential WRITE: the system assigns the next relative record number and MOVEs
            // it into the RELATIVE KEY data item (ISO §14.9.51 GR, sequential access).
            if (IsRelative(wr.File) && !IsRelativeRandom(wr.File)
                && ResolveRelativeKeyLocation(wr.File) is { } wStoreKey)
                block.Instructions.Add(new IrStoreRelativeKey(fileName, wStoreKey));
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

        // USE AFTER EXCEPTION: a WRITE exception (e.g. 48 not-open, or 21/22/24 on an indexed/relative file
        // with no INVALID KEY phrase) fires the applicable USE declarative (ISO §14.6.6). An INVALID KEY
        // phrase, when present, services the invalid-key conditions itself, so those are excluded.
        if (wr.File != null)
            block = EmitUseDeclarative(wr.File, method, block,
                excludeInvalidKey: wr.InvalidKey.Count > 0 || wr.NotInvalidKey.Count > 0);

        // AT END-OF-PAGE / NOT AT END-OF-PAGE (LINAGE files, ISO §14.9.51 GR26-28): after the WRITE,
        // branch on whether the end-of-page condition was raised.
        if (wr.AtEndOfPage.Count > 0 || wr.NotAtEndOfPage.Count > 0)
        {
            var eopResult = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
            block.Instructions.Add(new IrCheckEndOfPage(fileName, eopResult));
            return _ctx.Condition.LowerConditionalBranch(
                wr.AtEndOfPage, wr.NotAtEndOfPage, eopResult, method, block, "write");
        }
        return block;
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

            // LINAGE with data-name phrases (ISO §13.18.34 GR6b): the page parameters are the runtime
            // values of those data items, read at completion of OPEN OUTPUT; the counter is then reset to
            // one (GR7d). Integer-only LINAGE was applied at registration, so only the data-name form
            // needs this OPEN-time evaluation.
            if (open.Mode == OpenMode.Output && file.HasLinageDataNames)
                block.Instructions.Add(BuildInitLinage(file));

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
            else if (phrase.Option is CloseOption.Reel or CloseOption.Unit)
            {
                // CLOSE … REEL/UNIT on a disk medium: file stays OPEN, I-O status 07 (ISO §9.1.13.2 item 6).
                block.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.CloseReelUnit", new[] { fnVal }));
            }
            else
            {
                // Plain CLOSE (and NO REWIND, which on disk closes normally) — standard close.
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
            // For RECORD VARYING, read into the LARGEST record area under the FD so a maximum-length
            // record is not truncated to the first record's size (and the recovered length is right).
            var recordLoc = ResolveReadRecordLocation(read.File, recordSym);
            if (recordLoc != null)
            {
                if (read.IsPrevious)
                {
                    // READ PREVIOUS — reverse sequential access
                    block.Instructions.Add(new IrReadPreviousToStorage(cobolName, recordLoc));
                }
                else
                {
                    // Keyed read: RANDOM/DYNAMIC access without NEXT → ReadByKey. The KEY IS operand names
                    // the key of reference — the prime record key OR an alternate record key (ISO §14.9.30);
                    // with no KEY phrase the prime key is implied. For RELATIVE files the RELATIVE KEY is used.
                    string? keyName = read.KeyDataName ?? read.File.RecordKey ?? read.File.RelativeKey;
                    bool isKeyedRead = !read.IsNext &&
                        read.File.AccessMode is "RANDOM" or "DYNAMIC" &&
                        keyName != null;

                    if (isKeyedRead)
                    {
                        var keySym = _ctx.Semantic.ResolveData(keyName!);
                        var keyLoc = keySym != null ? _ctx.Location.ResolveLocation(keySym) : null;
                        if (keyLoc != null)
                        {
                            // RELATIVE random/dynamic READ positions by the RELATIVE KEY, decoded
                            // PIC-aware to the relative record number (ISO §14.9.30). INDEXED keyed
                            // reads keep using the key bytes (the alphanumeric record/alternate key).
                            if (IsRelative(read.File))
                                block.Instructions.Add(new IrSetRelativeKey(cobolName, keyLoc));
                            int keyIndex = ResolveStartKeyIndex(read.File, keyName!);
                            block.Instructions.Add(new IrReadByKey(cobolName, recordLoc, keyLoc, keyIndex));
                        }
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

        // RECORD IS VARYING … DEPENDING ON: store the actual record length into the depending item.
        var readLengthLoc = ResolveRecordLengthLocation(read.File);
        if (readLengthLoc != null)
            block.Instructions.Add(new IrStoreRecordLength(read.File.Name, readLengthLoc));

        // RELATIVE sequential READ (NEXT/PREVIOUS) moves the made-available record's relative number
        // into the RELATIVE KEY data item (ISO §14.9.30 GR 25). A keyed (random) read leaves the
        // program-supplied key as-is, so it is excluded.
        bool relKeyedRead = !read.IsPrevious && !read.IsNext
            && read.File.AccessMode is "RANDOM" or "DYNAMIC";
        if (IsRelative(read.File) && !relKeyedRead
            && ResolveRelativeKeyLocation(read.File) is { } rdKey)
            block.Instructions.Add(new IrStoreRelativeKey(read.File.Name, rdKey));

        // If INTO specified, MOVE FD record to INTO target. The INTO target is a RECEIVING operand:
        // when it is a group whose OCCURS DEPENDING ON object is inside the group, the MAXIMUM length
        // is used (ISO §13.18.38 OCCURS GR — receiving operand), so all occurrences are moved.
        if (read.Into != null && recordSym != null)
        {
            var srcLoc = _ctx.Location.ResolveLocation(recordSym);
            var dstLoc = _ctx.Location.ResolveLocation(read.Into, receiving: true);
            if (srcLoc != null && dstLoc != null)
            {
                block.Instructions.Add(new IrMoveFieldToField(
                    srcLoc, dstLoc,
                    srcLoc.GetPic(), dstLoc.GetPic()));
            }
        }

        bool hasAtEnd = read.AtEnd.Count > 0 || read.NotAtEnd.Count > 0;
        bool hasInvalidKey = read.InvalidKey.Count > 0 || read.NotInvalidKey.Count > 0;

        // USE AFTER EXCEPTION: a READ exception that the statement's own phrase does not service fires the
        // applicable USE declarative (ISO §14.6.6). An AT END phrase services the at-end condition (10) and
        // an INVALID KEY phrase the invalid-key conditions — those are excluded here so the declarative does
        // not double-handle them — but a not-open / other exception (47, …) still fires the declarative even
        // when a phrase is present. With no phrase, every exception (including at-end) fires the declarative.
        block = EmitUseDeclarative(read.File, method, block,
            excludeAtEnd: hasAtEnd, excludeInvalidKey: hasInvalidKey);

        // AT END / NOT AT END branching
        if (hasAtEnd)
        {
            var atEndResult = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
            block.Instructions.Add(new IrCheckFileAtEnd(cobolName, atEndResult));
            return _ctx.Condition.LowerConditionalBranch(read.AtEnd, read.NotAtEnd, atEndResult, method, block, "read");
        }

        // INVALID KEY branching
        if (hasInvalidKey)
        {
            var invalidResult = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
            block.Instructions.Add(new IrCheckFileInvalidKey(cobolName, invalidResult));
            return _ctx.Condition.LowerConditionalBranch(read.InvalidKey, read.NotInvalidKey, invalidResult, method, block, "read");
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

        var statusSym = _ctx.Semantic.ResolveData(file.FileStatus);
        if (statusSym == null) return;

        var statusLoc = _ctx.Location.ResolveLocation(statusSym);
        if (statusLoc == null) return;

        block.Instructions.Add(new IrStoreFileStatus(file.Name, statusLoc));
    }

    /// <summary>
    /// Resolve the storage location of a file's RECORD IS VARYING … DEPENDING ON data item, or null
    /// if the file has no such clause (fixed-length record). Used to set the length after READ and to
    /// supply the byte count for a variable-length WRITE.
    /// </summary>
    private IrLocation? ResolveRecordLengthLocation(FileSymbol? file)
    {
        if (!IsVaryingRecord(file) || file!.RecordVaryingDependingOn == null) return null;
        var sym = _ctx.Semantic.ResolveData(file.RecordVaryingDependingOn);
        return sym != null ? _ctx.Location.ResolveLocation(sym) : null;
    }

    /// <summary>Build the OPEN-OUTPUT LINAGE initialization for a file with data-name phrases: each phrase
    /// is the location of its data-name (decoded at runtime) or the captured integer-literal value.</summary>
    private IrInitLinage BuildInitLinage(FileSymbol file)
    {
        IrLocation? Loc(string? name)
            => name != null && _ctx.Semantic.ResolveData(name) is { } s ? _ctx.Location.ResolveLocation(s) : null;
        return new IrInitLinage(file.Name,
            Loc(file.LinageBodyName), file.LinageBody,
            Loc(file.LinageFootingName), file.LinageFooting,
            Loc(file.LinageTopName), file.LinageTop,
            Loc(file.LinageBottomName), file.LinageBottom);
    }

    private static bool IsRelative(FileSymbol? f) => f?.Organization == "RELATIVE";

    /// <summary>RELATIVE file in random or dynamic access — WRITE/REWRITE/DELETE position by the
    /// RELATIVE KEY; sequential access appends / uses the current record (ISO §14.9.x).</summary>
    private static bool IsRelativeRandom(FileSymbol? f)
        => IsRelative(f) && f!.AccessMode is "RANDOM" or "DYNAMIC";

    /// <summary>Storage location of a file's RELATIVE KEY data item, or null.</summary>
    private IrLocation? ResolveRelativeKeyLocation(FileSymbol? f)
    {
        if (f?.RelativeKey == null) return null;
        var sym = _ctx.Semantic.ResolveData(f.RelativeKey);
        return sym != null ? _ctx.Location.ResolveLocation(sym) : null;
    }

    /// <summary>
    /// True for a variable-length-record file that the variable-record machinery (no-trim WRITE,
    /// read-into-largest, length store) applies to: SEQUENTIAL (explicit RECORD VARYING or multiple 01
    /// sizes) or RELATIVE (explicit RECORD VARYING — relative slots now carry a per-record length, see
    /// RelativeFileHandler). INDEXED is excluded. The RELATIVE case must match the runtime flag set in
    /// Binder (FileRuntime.SetRelativeVarying), so it keys on the explicit clause only. The SEQUENTIAL
    /// case delegates to SemanticModel.IsVariableLengthSequential — the single source of truth shared
    /// with Binder's handler registration (FileRuntime.SetSequentialVarying), so they cannot disagree.
    /// </summary>
    private bool IsVaryingRecord(FileSymbol? file)
    {
        if (file is null) return false;
        if (IsRelative(file)) return file.IsRecordVarying;
        return _ctx.Semantic.IsVariableLengthSequential(file);
    }

    /// <summary>
    /// Location to READ a record into. For a fixed-length file this is the FD's record. For a
    /// RECORD VARYING file it is the LARGEST 01 record under the FD (its storage spans the whole
    /// record area), so a maximum-length record is read in full and its length recovered correctly.
    /// The location is resolved as a RECEIVING operand so that an 01 containing an OCCURS DEPENDING
    /// table (a format-3 variable record, ISO §13.18.43) uses its MAXIMUM length for the read buffer —
    /// otherwise the buffer would be sized by the depending item's (pre-read) value and truncate the
    /// table's bytes.
    /// </summary>
    private IrLocation? ResolveReadRecordLocation(FileSymbol file, DataSymbol recordSym)
    {
        if (IsVaryingRecord(file))
        {
            DataSymbol largest = recordSym;
            int largestLen = _ctx.Semantic.GetStorageLocation(recordSym)?.Length ?? 0;
            foreach (var d in _ctx.Semantic.DataItemsInOrder)
            {
                if (d.LevelNumber != 1 || !ReferenceEquals(d.OwningFile, file)) continue;
                int len = _ctx.Semantic.GetStorageLocation(d)?.Length ?? 0;
                if (len > largestLen) { largest = d; largestLen = len; }
            }
            return _ctx.Location.ResolveLocation(largest, receiving: true);
        }
        return _ctx.Location.ResolveLocation(recordSym);
    }

    /// <summary>
    /// Emit USE AFTER STANDARD ERROR/EXCEPTION declarative dispatch (ISO §14.9.49) at an I/O site that
    /// has no explicit AT END / INVALID KEY phrase. If the last I/O on the file raised an exception
    /// condition, PERFORM the applicable declarative section. A file-name-scoped declarative for this
    /// file takes precedence; otherwise each open-mode-scoped declarative (USE … ON INPUT/OUTPUT/I-O/
    /// EXTEND) is dispatched at runtime by the file's actual open mode (FileRuntime.ShouldRunUseDeclarative).
    /// Returns the (possibly new) current block after the branches.
    /// </summary>
    private IrBasicBlock EmitUseDeclarative(FileSymbol file, IrMethod method, IrBasicBlock block,
        bool excludeAtEnd = false, bool excludeInvalidKey = false)
    {
        // Build the candidate (scope, section) list. UseScope: -1 file-name; 0/1/2/3 INPUT/OUTPUT/I-O/EXTEND.
        var candidates = new List<(int scope, string section)>();
        if (_ctx.Semantic.UseDeclaratives.TryGetValue(file.Name, out var fileSection))
            candidates.Add((-1, fileSection)); // file-name-specific takes precedence; no mode fallbacks
        else
            foreach (var kv in _ctx.Semantic.UseDeclarativesByMode)
                candidates.Add((UseModeScope(kv.Key), kv.Value));

        foreach (var (scope, section) in candidates)
        {
            var sectionParas = _ctx.Semantic.GetSectionParagraphs(section);
            if (sectionParas == null || sectionParas.Count == 0) continue;
            if (!_ctx.ParagraphMethods.ContainsKey(sectionParas[0])) continue;

            var cond = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
            block.Instructions.Add(new IrCheckUseDeclarative(file.Name, scope, cond, excludeAtEnd, excludeInvalidKey));

            var useBlock = method.CreateBlock("use.handler");
            var afterBlock = method.CreateBlock("use.after");
            block.Instructions.Add(new IrBranch(cond, useBlock, afterBlock));

            method.Blocks.Add(useBlock);
            EmitPerformDeclarativeSection(sectionParas, useBlock);
            useBlock.Instructions.Add(new IrJump(afterBlock));

            method.Blocks.Add(afterBlock);
            block = afterBlock; // chain: the next candidate's check follows this branch's join
        }
        return block;
    }

    /// <summary>Map a USE-declarative open-mode scope to the runtime UseScope encoding used by
    /// FileRuntime.ShouldRunUseDeclarative.</summary>
    private static int UseModeScope(OpenMode mode) => mode switch
    {
        OpenMode.Input => 0,
        OpenMode.Output => 1,
        OpenMode.IO => 2,
        OpenMode.Extend => 3,
        _ => -1,
    };

    /// <summary>PERFORM a declarative SECTION: one paragraph as a plain PERFORM, multiple as PERFORM THRU
    /// (first THRU last paragraph of the section), so control returns after the section's last paragraph.</summary>
    private void EmitPerformDeclarativeSection(IReadOnlyList<string> sectionParas, IrBasicBlock useBlock)
    {
        if (sectionParas.Count == 1)
        {
            if (_ctx.ParagraphMethods.TryGetValue(sectionParas[0], out var pm))
                useBlock.Instructions.Add(new IrPerform(pm));
            return;
        }
        int startIdx = _ctx.ParagraphIndices.GetValueOrDefault(sectionParas[0], -1);
        int endIdx = _ctx.ParagraphIndices.GetValueOrDefault(sectionParas[^1], -1);
        if (startIdx < 0 || endIdx < 0) return;
        var methods = new List<IrMethod>();
        for (int i = startIdx; i <= endIdx; i++)
        {
            var pName = _ctx.ParagraphsByIndex[i];
            if (_ctx.ParagraphMethods.TryGetValue(pName, out var pm))
                methods.Add(pm);
            else
                _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0501, SourceLocation.None, TextSpan.Empty, pName);
        }
        useBlock.Instructions.Add(new IrPerformThru(startIdx, endIdx, methods));
    }

    // ── REWRITE ──

    public IrBasicBlock LowerRewrite(BoundRewriteStatement rw, IrMethod method, IrBasicBlock block)
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

            // RELATIVE random/dynamic REWRITE replaces the record at the RELATIVE KEY (ISO §14.9.35).
            if (IsRelativeRandom(rw.File) && ResolveRelativeKeyLocation(rw.File) is { } rwKey)
                block.Instructions.Add(new IrSetRelativeKey(cobolName, rwKey));

            // RECORD VARYING on a record-SEQUENTIAL file: the rewrite length is the DEPENDING ON item's
            // runtime value (not the record-name's declared size), so a different-size REWRITE is detected
            // per §14.9.35 GR16 (status 44). RELATIVE files are excluded — their handler carries a per-slot
            // length and §14.9.35 GR18 permits a relative REWRITE to differ in length.
            var rwLengthLoc = !IsRelative(rw.File) && IsVaryingRecord(rw.File)
                ? ResolveRecordLengthLocation(rw.File) : null;
            block.Instructions.Add(new IrRewriteRecordFromStorage(cobolName, recordLoc, rwLengthLoc));
        }
        EmitFileStatus(rw.File, block);

        // USE AFTER EXCEPTION/ERROR: a REWRITE exception the statement's INVALID KEY phrase does not
        // service (e.g. 44 different-length, 49 not-open, 43 no-prior-read) invokes the applicable
        // declarative (ISO §14.9.49 / §14.6.6). The invalid-key conditions are excluded when an
        // INVALID KEY phrase is present, since that phrase services them.
        if (rw.File != null)
            block = EmitUseDeclarative(rw.File, method, block,
                excludeInvalidKey: rw.InvalidKey.Count > 0 || rw.NotInvalidKey.Count > 0);

        // INVALID KEY / NOT INVALID KEY branching (ISO §14.9.35 — the invalid key condition exists
        // when the relative key names no existing record). Without this the imperative-statements
        // (e.g. NOT INVALID KEY GO TO …) never execute and control falls through past the REWRITE.
        if (rw.InvalidKey.Count > 0 || rw.NotInvalidKey.Count > 0)
        {
            var invalidResult = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
            block.Instructions.Add(new IrCheckFileInvalidKey(cobolName, invalidResult));
            return _ctx.Condition.LowerConditionalBranch(rw.InvalidKey, rw.NotInvalidKey, invalidResult, method, block, "rewrite");
        }

        return block;
    }

    // ── DELETE ──

    public IrBasicBlock LowerDelete(BoundDeleteStatement del, IrMethod method, IrBasicBlock block)
    {
        string cobolName = del.File.Name;
        // RELATIVE random/dynamic DELETE removes the record at the RELATIVE KEY (ISO §14.9.12 GR 4).
        if (IsRelativeRandom(del.File) && ResolveRelativeKeyLocation(del.File) is { } delKey)
            block.Instructions.Add(new IrSetRelativeKey(cobolName, delKey));
        block.Instructions.Add(new IrDeleteRecord(cobolName));
        EmitFileStatus(del.File, block);

        // USE AFTER EXCEPTION: a DELETE exception that the statement's INVALID KEY phrase does not service
        // (e.g. 49 not-open, or 43 no-prior-read in sequential access) fires the USE declarative (ISO
        // §14.6.6). When an INVALID KEY phrase is present it services the invalid-key conditions itself.
        block = EmitUseDeclarative(del.File, method, block,
            excludeInvalidKey: del.InvalidKey.Count > 0 || del.NotInvalidKey.Count > 0);

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

        // The KEY operand names the key of reference — the prime record key, an alternate record key, or a
        // generic-key prefix of one (ISO §14.9.41). The search value is that data item's current value,
        // truncated by the runtime to the operand's length (so a shorter generic operand positions on the
        // matching leftmost portion). Its index (-1 prime, 0+ alternate) is conveyed so a subsequent READ
        // NEXT sequences on the same key. With no KEY phrase the prime record key is implied.
        int condition = 0; // StartCondition.Equal (default)
        var operandSym = start.KeyCondition switch
        {
            BoundBinaryExpression be when be.Left is BoundIdentifierExpression bid => bid.Symbol,
            BoundIdentifierExpression id => id.Symbol,
            _ => null
        };
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

        // No KEY phrase → the prime record key.
        operandSym ??= start.File.RecordKey != null ? _ctx.Semantic.ResolveData(start.File.RecordKey) : null;
        if (operandSym != null)
        {
            int keyIndex = _ctx.Semantic.ResolveKeyOfReference(start.File, operandSym) ?? -1;
            var keyLoc = _ctx.Location.ResolveLocation(operandSym);
            if (keyLoc != null)
                block.Instructions.Add(new IrStartFile(cobolName, keyLoc, condition, keyIndex));
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

    /// <summary>The key-of-reference index for a START / READ KEY operand: -1 for the prime record key, or
    /// the 0-based alternate-key index (ISO §14.9.41). Defaults to -1 (prime) when the name matches neither.</summary>
    private static int ResolveStartKeyIndex(FileSymbol file, string keyName)
    {
        if (file.RecordKey != null && string.Equals(keyName, file.RecordKey, System.StringComparison.OrdinalIgnoreCase))
            return -1;
        for (int i = 0; i < file.AlternateKeys.Count; i++)
            if (string.Equals(keyName, file.AlternateKeys[i].DataName, System.StringComparison.OrdinalIgnoreCase))
                return i;
        return -1;
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
        block.Instructions.Add(new IrSortSort(sortFileName, BuildKeysSpec(sort.SortFile, sort.Keys),
            ResolveCollating(sort.CollatingAlphabetName)));

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

        block.Instructions.Add(new IrTableSort(tableLoc, entrySize, entryCount, keysSpec,
            ResolveCollating(tableSort.CollatingAlphabetName)));

        return block;
    }

    /// <summary>
    /// Resolve a SORT/MERGE collating sequence to a 256-byte code→weight table.
    /// Precedence (ISO/IEC 1989:2023 14.9.40 / 14.9.22): the statement's COLLATING SEQUENCE
    /// phrase if present; otherwise the program collating sequence; otherwise null (native).
    /// </summary>
    private byte[]? ResolveCollating(string? alphabetName)
    {
        if (alphabetName != null &&
            _ctx.Semantic.ResolveAlphabetDefinition(alphabetName) is { } def)
            return def.CollatingSequence;
        return _ctx.Semantic.ProgramCollatingSequence;
    }

    private string BuildTableKeysSpec(IReadOnlyList<BoundSortKey> keys, DataSymbol tableItem)
    {
        var tableLoc = _ctx.Semantic.GetStorageLocation(tableItem);
        if (!tableLoc.HasValue) return "";
        int tableBaseOffset = tableLoc.Value.Offset;

        var specs = new List<string>();
        foreach (var key in keys)
        {
            // Offset is relative to the table entry start; skip keys with no storage location.
            if (!_ctx.Semantic.GetStorageLocation(key.Key).HasValue) continue;
            specs.Add(BuildKeySpecField(key, tableBaseOffset));
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
        // Loop termination: stop on EOF OR any terminal unreadable status (a missing input file
        // must not spin), not the AT END condition.
        loopHead.Instructions.Add(new IrCheckFileAtEnd(inputName, atEndVal, treatErrorsAsEnd: true));
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
        return string.Join(";", keys.Select(k => BuildKeySpecField(k, sdBaseOffset)));
    }

    /// <summary>
    /// Encode one sort/merge key as
    /// "offset,length,asc,isNumeric,usage,isSigned,signStorage,fractionDigits,totalDigits,leadingScale,trailingScale"
    /// with offset relative to <paramref name="baseOffset"/>. The PIC is read from the key's
    /// resolved IrLocation (the live path used throughout lowering) — NOT the unused SemanticModel
    /// pic registry, which is never populated — so numeric keys are flagged and the runtime compares
    /// them by value, never applying a collating sequence (ISO/IEC 1989:2023 14.9.40 / 14.9.22).
    /// </summary>
    private string BuildKeySpecField(BoundSortKey key, int baseOffset)
    {
        var keyLoc = _ctx.Semantic.GetStorageLocation(key.Key);
        int keyOff = keyLoc.HasValue ? keyLoc.Value.Offset - baseOffset : 0;
        int keyLen = keyLoc.HasValue ? keyLoc.Value.Length : 0;
        var pic = _ctx.Location.ResolveLocation(key.Key)?.GetPic();
        int usage = pic != null ? (int)pic.Usage : 0;
        int isSigned = pic is { IsSigned: true } ? 1 : 0;
        int signStorage = pic != null ? (int)pic.SignStorage : 0;
        int fractionDigits = pic?.FractionDigits ?? 0;
        int totalDigits = pic?.TotalDigits ?? 0;
        int leadingScale = pic?.LeadingScaleDigits ?? 0;
        int trailingScale = pic?.TrailingScaleDigits ?? 0;
        bool isNumeric = pic is { IsNumeric: true };
        return $"{keyOff},{keyLen},{(key.IsAscending ? "1" : "0")},{(isNumeric ? "1" : "0")},{usage},{isSigned},{signStorage},{fractionDigits},{totalDigits},{leadingScale},{trailingScale}";
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
        block.Instructions.Add(new IrSortMerge(mergeFileName, inputNames, keysSpec,
            ResolveCollating(merge.CollatingAlphabetName)));

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
