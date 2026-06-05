// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Mono.Cecil;
using Mono.Cecil.Cil;
using CobolSharp.Compiler.IR;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.CodeGen.Emission;

/// <summary>
/// File I/O emission: EmitWriteRecordFromStorage, EmitRewriteRecordFromStorage,
/// EmitWriteAdvancing, EmitReadRecordToStorage, EmitReadPreviousToStorage,
/// EmitReadByKey, EmitStoreFileStatus, EmitCheckFileAtEnd,
/// EmitDeleteRecord, EmitStartFile, EmitCheckFileInvalidKey,
/// EmitSortInit, EmitSortRelease, EmitSortSort, EmitSortReturn,
/// EmitSortClose, EmitSortMerge.
/// </summary>
internal sealed class CilFileIoEmitter
{
    private readonly EmissionContext _ctx;

    internal CilFileIoEmitter(EmissionContext ctx) => _ctx = ctx;

    internal void EmitWriteRecordFromStorage(ILProcessor il, IrWriteRecordFromStorage wr)
    {
        // fileName
        il.Append(il.Create(OpCodes.Ldstr, wr.FileName));

        // Load area, offset, size
        _ctx.Location.EmitLocationArgs(il, wr.Record);

        // Call ProgramState.WriteRecordToFile(string, byte[], int, int)
        var method = _ctx.Module.ImportReference(
            typeof(StorageHelpers).GetMethod(
                "WriteRecordToFile",
                new[] { typeof(string), typeof(byte[]), typeof(int), typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    /// <summary>
    /// Report Writer SOURCE placement (ISO §14.9.19): push reportName, COLUMN, field width, then the source
    /// storage location (area/offset/size), and call ReportWriterRuntime.PlaceField to copy the bytes into
    /// the active report line buffer.
    /// </summary>
    internal void EmitReportPlaceField(ILProcessor il, IrReportPlaceField rpf)
    {
        il.Append(il.Create(OpCodes.Ldstr, rpf.ReportName));
        il.Append(il.Create(OpCodes.Ldc_I4, rpf.Column));
        il.Append(il.Create(OpCodes.Ldc_I4, rpf.FieldWidth));
        // Load source area, offset, size (byte[], int, int).
        _ctx.Location.EmitLocationArgs(il, rpf.Source);
        var method = _ctx.Module.ImportReference(
            typeof(CobolSharp.Runtime.ReportWriterRuntime).GetMethod(
                "PlaceField",
                new[] { typeof(string), typeof(int), typeof(int), typeof(byte[]), typeof(int), typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    /// <summary>Report Writer VALUE-literal placement: push reportName, COLUMN, field width, and the literal
    /// text; call ReportWriterRuntime.PlaceLiteralField to copy it into the active line buffer.</summary>
    internal void EmitReportPlaceLiteral(ILProcessor il, IrReportPlaceLiteral rpl)
    {
        il.Append(il.Create(OpCodes.Ldstr, rpl.ReportName));
        il.Append(il.Create(OpCodes.Ldc_I4, rpl.Column));
        il.Append(il.Create(OpCodes.Ldc_I4, rpl.FieldWidth));
        il.Append(il.Create(OpCodes.Ldstr, rpl.Text));
        var method = _ctx.Module.ImportReference(
            typeof(CobolSharp.Runtime.ReportWriterRuntime).GetMethod(
                "PlaceLiteralField",
                new[] { typeof(string), typeof(int), typeof(int), typeof(string) })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    /// <summary>Register a data-SOURCE field of an auto-presented group: push reportName, slot, COLUMN, width,
    /// then the source storage location (area/offset/size); call ReportWriterRuntime.RegisterAutoDataField so
    /// the runtime reads the live bytes when it presents the group.</summary>
    internal void EmitReportRegisterDataField(ILProcessor il, IrReportRegisterDataField inst)
    {
        il.Append(il.Create(OpCodes.Ldstr, inst.ReportName));
        il.Append(il.Create(OpCodes.Ldc_I4, inst.Slot));
        il.Append(il.Create(OpCodes.Ldc_I4, inst.Column));
        il.Append(il.Create(OpCodes.Ldc_I4, inst.FieldWidth));
        _ctx.Location.EmitLocationArgs(il, inst.Source); // byte[] area, int offset, int size
        var method = _ctx.Module.ImportReference(
            typeof(CobolSharp.Runtime.ReportWriterRuntime).GetMethod(
                "RegisterAutoDataField",
                new[] { typeof(string), typeof(int), typeof(int), typeof(int), typeof(byte[]), typeof(int), typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    /// <summary>
    /// Variable-length WRITE (RECORD IS VARYING … DEPENDING ON): write the record area for the byte
    /// count read at runtime from the DEPENDING data item, without trailing-space trimming.
    /// Calls StorageHelpers.WriteRecordVariableToFile(string, byte[], int, int).
    /// </summary>
    internal void EmitWriteRecordVariable(ILProcessor il, IrWriteRecordVariable wr)
    {
        // fileName
        il.Append(il.Create(OpCodes.Ldstr, wr.FileName));

        // Record area + offset + the record's declared size.
        _ctx.Location.EmitLocationArgs(il, wr.Record); // byte[], offset, size

        if (wr.LengthLocation != null)
        {
            // DEPENDING ON present: the depending data item supplies the length at runtime.
            il.Append(il.Create(OpCodes.Pop));             // discard declared size
            _ctx.Location.EmitLocationArgs(il, wr.LengthLocation); // byte[], offset, size
            var readInt = _ctx.Module.ImportReference(
                typeof(StorageHelpers).GetMethod("ReadFieldAsInt",
                    new[] { typeof(byte[]), typeof(int), typeof(int) })!);
            il.Append(il.Create(OpCodes.Call, readInt));   // -> int length
        }
        // else: VARYING without DEPENDING — keep the record's declared size as the length.

        var method = _ctx.Module.ImportReference(
            typeof(StorageHelpers).GetMethod(
                "WriteRecordVariableToFile",
                new[] { typeof(string), typeof(byte[]), typeof(int), typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    /// <summary>
    /// Store the actual record length into the RECORD VARYING DEPENDING ON data item after a READ:
    /// MoveIntToField(area, offset, size, FileRuntime.GetLastRecordLength(name)).
    /// </summary>
    internal void EmitStoreRecordLength(ILProcessor il, IrStoreRecordLength srl)
    {
        // Push area, offset, size for the depending field.
        _ctx.Location.EmitLocationArgs(il, srl.LengthVariable);

        // FileRuntime.GetLastRecordLength(name) -> int
        il.Append(il.Create(OpCodes.Ldstr, srl.CobolFileName));
        var getLen = _ctx.Module.ImportReference(
            typeof(FileRuntime).GetMethod("GetLastRecordLength", new[] { typeof(string) })!);
        il.Append(il.Create(OpCodes.Call, getLen));

        // StorageHelpers.MoveIntToField(area, offset, size, value)
        var moveInt = _ctx.Module.ImportReference(
            typeof(StorageHelpers).GetMethod(
                "MoveIntToField",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, moveInt));
    }

    /// <summary>
    /// Before a random/dynamic relative WRITE/REWRITE/DELETE: FileRuntime.SetRelativeKey(name, area,
    /// offset, size) reads the RELATIVE KEY data item and records the target slot.
    /// </summary>
    internal void EmitSetRelativeKey(ILProcessor il, IrSetRelativeKey srk)
    {
        il.Append(il.Create(OpCodes.Ldstr, srk.CobolFileName));
        // Decode the RELATIVE KEY data item to its integer value PIC-aware (DISPLAY / COMP / COMP-3),
        // NOT by parsing its raw bytes as ASCII — relative keys are routinely USAGE COMP, whose bytes
        // are binary. This is the same decode the subscript/EVALUATE paths use.
        _ctx.Location.EmitLocationArgsWithPic(il, srk.KeyVariable); // byte[], offset, length, PicDescriptor
        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
            typeof(PicRuntime).GetMethod("DecodeNumeric",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor) })!)));
        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
            typeof(System.Convert).GetMethod("ToInt32", new[] { typeof(decimal) })!)));
        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
            typeof(FileRuntime).GetMethod("SetRelativeKey",
                new[] { typeof(string), typeof(int) })!)));
    }

    /// <summary>
    /// Before a RANDOM/DYNAMIC INDEXED DELETE: pass the prime RECORD KEY bytes (from the record-key data
    /// item) to FileRuntime.SetIndexedKey(string, byte[], int, int) so the handler deletes the identified
    /// record. The key is alphanumeric (the record/alternate key bytes), so it is passed raw — unlike a
    /// RELATIVE key, which is PIC-decoded to an integer.
    /// </summary>
    internal void EmitSetIndexedKey(ILProcessor il, IrSetIndexedKey sik)
    {
        il.Append(il.Create(OpCodes.Ldstr, sik.CobolFileName));
        _ctx.Location.EmitLocationArgs(il, sik.KeyVariable); // byte[], offset, length
        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
            typeof(FileRuntime).GetMethod("SetIndexedKey",
                new[] { typeof(string), typeof(byte[]), typeof(int), typeof(int) })!)));
    }

    /// <summary>
    /// After a sequential relative WRITE or a relative READ: store the acted-on slot
    /// (FileRuntime.GetRelativeSlot) into the RELATIVE KEY data item via MoveIntToField.
    /// </summary>
    internal void EmitStoreRelativeKey(ILProcessor il, IrStoreRelativeKey srk)
    {
        // Encode the relative record number into the RELATIVE KEY data item PIC-aware (DISPLAY / COMP /
        // COMP-3), the inverse of the decode used to read it — so a USAGE COMP key round-trips.
        _ctx.Location.EmitLocationArgsWithPic(il, srk.KeyVariable); // byte[], offset, length, PicDescriptor
        il.Append(il.Create(OpCodes.Ldstr, srk.CobolFileName));
        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
            typeof(FileRuntime).GetMethod("GetRelativeSlot", new[] { typeof(string) })!)));
        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
            typeof(System.Convert).GetMethod("ToDecimal", new[] { typeof(int) })!)));
        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
            typeof(PicRuntime).GetMethod("EncodeNumeric",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor), typeof(decimal) })!)));
    }

    /// <summary>
    /// WRITE AFTER ADVANCING: calls FileRuntime.WriteAfterAdvancing(string, byte[], int, int, int).
    /// </summary>
    /// <summary>
    /// REWRITE record: calls FileRuntime.Rewrite(string, byte[], int, int).
    /// </summary>
    internal void EmitRewriteRecordFromStorage(ILProcessor il, IrRewriteRecordFromStorage rw)
    {
        il.Append(il.Create(OpCodes.Ldstr, rw.FileName));
        _ctx.Location.EmitLocationArgs(il, rw.Record); // byte[], offset, declared size

        if (rw.LengthLocation != null)
        {
            // RECORD VARYING DEPENDING ON: the depending item supplies the rewrite length at runtime
            // (so §14.9.35 GR16 sees the true length, not the record-name's declared size).
            il.Append(il.Create(OpCodes.Pop)); // discard declared size
            _ctx.Location.EmitLocationArgs(il, rw.LengthLocation);
            il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
                typeof(StorageHelpers).GetMethod("ReadFieldAsInt",
                    new[] { typeof(byte[]), typeof(int), typeof(int) })!)));
        }

        var method = _ctx.Module.ImportReference(
            typeof(FileRuntime).GetMethod(
                "Rewrite",
                new[] { typeof(string), typeof(byte[]), typeof(int), typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    internal void EmitWriteAdvancing(ILProcessor il, IrWriteAdvancing waa)
    {
        // fileName
        il.Append(il.Create(OpCodes.Ldstr, waa.FileName));
        // Load area, offset, size
        _ctx.Location.EmitLocationArgs(il, waa.Record);
        // advanceLines: from data field or compile-time constant
        if (waa.AdvancingLocation != null)
        {
            // Read advancing count from data field at runtime
            _ctx.Location.EmitLocationArgs(il, waa.AdvancingLocation);
            var readInt = _ctx.Module.ImportReference(
                typeof(StorageHelpers).GetMethod("ReadFieldAsInt",
                    new[] { typeof(byte[]), typeof(int), typeof(int) })!);
            il.Append(il.Create(OpCodes.Call, readInt));
        }
        else
        {
            il.Append(il.Create(OpCodes.Ldc_I4, waa.AdvanceLines));
        }
        // isBefore
        il.Append(il.Create(waa.IsBefore ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));

        var method = _ctx.Module.ImportReference(
            typeof(FileRuntime).GetMethod(
                "WriteAdvancing",
                new[] { typeof(string), typeof(byte[]), typeof(int), typeof(int), typeof(int), typeof(bool) })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    internal void EmitReadRecordToStorage(ILProcessor il, IrReadRecordToStorage rd)
    {
        // StorageHelpers.ReadRecordFromFile(string fileName, byte[] area, int offset, int size)
        il.Append(il.Create(OpCodes.Ldstr, rd.FileName));
        _ctx.Location.EmitLocationArgs(il, rd.Record);

        var method = _ctx.Module.ImportReference(
            typeof(StorageHelpers).GetMethod(
                "ReadRecordFromFile",
                new[] { typeof(string), typeof(byte[]), typeof(int), typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, method));
        il.Append(il.Create(OpCodes.Pop)); // Discard bool return (AT END checked separately)
    }

    internal void EmitReadPreviousToStorage(ILProcessor il, IrReadPreviousToStorage rdp)
    {
        // StorageHelpers.ReadPreviousRecordFromFile(string fileName, byte[] area, int offset, int size)
        il.Append(il.Create(OpCodes.Ldstr, rdp.FileName));
        _ctx.Location.EmitLocationArgs(il, rdp.Record);

        var method = _ctx.Module.ImportReference(
            typeof(StorageHelpers).GetMethod(
                "ReadPreviousRecordFromFile",
                new[] { typeof(string), typeof(byte[]), typeof(int), typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, method));
        il.Append(il.Create(OpCodes.Pop)); // Discard bool return (AT END checked separately)
    }

    internal void EmitReadByKey(ILProcessor il, IrReadByKey rbk)
    {
        // FileRuntime.ReadByKey(string fileName, byte[] recArea, int recOff, int recSize,
        //                       byte[] keyArea, int keyOff, int keySize)
        il.Append(il.Create(OpCodes.Ldstr, rbk.FileName));
        _ctx.Location.EmitLocationArgs(il, rbk.Record);
        _ctx.Location.EmitLocationArgs(il, rbk.Key);
        il.Append(il.Create(OpCodes.Ldc_I4, rbk.KeyIndex));

        var method = _ctx.Module.ImportReference(
            typeof(FileRuntime).GetMethod(
                "ReadByKey",
                new[] { typeof(string), typeof(byte[]), typeof(int), typeof(int),
                        typeof(byte[]), typeof(int), typeof(int), typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    /// <summary>
    /// Store FILE STATUS: call FileRuntime.GetLastStatus(cobolName) -> MoveStringToField.
    /// </summary>
    internal void EmitStoreFileStatus(ILProcessor il, IrStoreFileStatus sfs)
    {
        // Push args for MoveStringToField(byte[] area, int offset, int length, string value)
        _ctx.Location.EmitLocationArgs(il, sfs.StatusVariable);

        // Call FileRuntime.GetLastStatus(cobolName) to get the status string
        il.Append(il.Create(OpCodes.Ldstr, sfs.CobolFileName));
        var getStatus = _ctx.Module.ImportReference(
            typeof(FileRuntime).GetMethod(
                "GetLastStatus", new[] { typeof(string) })!);
        il.Append(il.Create(OpCodes.Call, getStatus));

        // Call StorageHelpers.MoveStringToField(area, offset, length, value)
        var moveString = _ctx.Module.ImportReference(
            typeof(StorageHelpers).GetMethod(
                "MoveStringToField",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(string) })!);
        il.Append(il.Create(OpCodes.Call, moveString));
    }

    internal void EmitCheckFileAtEnd(
        ILProcessor il,
        IrCheckFileAtEnd chk,
        Func<IrValue, VariableDefinition> getLocal)
    {
        // FileRuntime.IsAtEnd (AT END condition, status "10") or IsReadExhausted (EOF or any
        // terminal unreadable status — for compiler-generated loop termination).
        il.Append(il.Create(OpCodes.Ldstr, chk.FileName));
        var method = _ctx.Module.ImportReference(
            typeof(FileRuntime).GetMethod(
                chk.TreatErrorsAsEnd ? "IsReadExhausted" : "IsAtEnd",
                new[] { typeof(string) })!);
        il.Append(il.Create(OpCodes.Call, method));
        if (chk.Result.HasValue)
            il.Append(il.Create(OpCodes.Stloc, getLocal(chk.Result.Value)));
        else
            il.Append(il.Create(OpCodes.Pop));
    }

    internal void EmitCheckEndOfPage(
        ILProcessor il,
        IrCheckEndOfPage chk,
        Func<IrValue, VariableDefinition> getLocal)
    {
        il.Append(il.Create(OpCodes.Ldstr, chk.FileName));
        var method = _ctx.Module.ImportReference(
            typeof(FileRuntime).GetMethod("WasEndOfPage", new[] { typeof(string) })!);
        il.Append(il.Create(OpCodes.Call, method));
        if (chk.Result.HasValue)
            il.Append(il.Create(OpCodes.Stloc, getLocal(chk.Result.Value)));
        else
            il.Append(il.Create(OpCodes.Pop));
    }

    internal void EmitInitLinage(ILProcessor il, IrInitLinage init)
    {
        il.Append(il.Create(OpCodes.Ldstr, init.FileName));
        EmitLinagePhrase(il, init.BodyLoc, init.BodyConst);
        EmitLinagePhrase(il, init.FootingLoc, init.FootingConst);
        EmitLinagePhrase(il, init.TopLoc, init.TopConst);
        EmitLinagePhrase(il, init.BottomLoc, init.BottomConst);
        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
            typeof(FileRuntime).GetMethod("InitLinage",
                new[] { typeof(string), typeof(int), typeof(int), typeof(int), typeof(int) })!)));
    }

    /// <summary>Push one LINAGE phrase value as int32: a data-name is decoded from its storage
    /// (DecodeNumeric → ToInt32); an integer-literal phrase pushes its constant directly.</summary>
    private void EmitLinagePhrase(ILProcessor il, IR.IrLocation? loc, int constVal)
    {
        if (loc == null)
        {
            il.Append(il.Create(OpCodes.Ldc_I4, constVal));
            return;
        }
        _ctx.Location.EmitLocationArgsWithPic(il, loc);
        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
            typeof(PicRuntime).GetMethod("DecodeNumeric",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor) })!)));
        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
            typeof(System.Convert).GetMethod("ToInt32", new[] { typeof(decimal) })!)));
    }

    // ── DELETE / START / INVALID KEY ──

    internal void EmitDeleteRecord(ILProcessor il, IrDeleteRecord del)
    {
        il.Append(il.Create(OpCodes.Ldstr, del.FileName));
        var method = _ctx.Module.ImportReference(
            typeof(FileRuntime).GetMethod("DeleteRecord",
                new[] { typeof(string) })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    internal void EmitStartFile(ILProcessor il, IrStartFile sf)
    {
        il.Append(il.Create(OpCodes.Ldstr, sf.FileName));
        _ctx.Location.EmitLocationArgs(il, sf.KeyLocation);
        il.Append(il.Create(OpCodes.Ldc_I4, sf.Condition));
        il.Append(il.Create(OpCodes.Ldc_I4, sf.KeyIndex));
        var method = _ctx.Module.ImportReference(
            typeof(FileRuntime).GetMethod("StartFile",
                new[] { typeof(string), typeof(byte[]), typeof(int), typeof(int), typeof(int), typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    internal void EmitCheckFileInvalidKey(ILProcessor il, IrCheckFileInvalidKey cik,
        Func<IrValue, VariableDefinition> getLocal)
    {
        il.Append(il.Create(OpCodes.Ldstr, cik.FileName));
        var method = _ctx.Module.ImportReference(
            typeof(FileRuntime).GetMethod("IsInvalidKey",
                new[] { typeof(string) })!);
        il.Append(il.Create(OpCodes.Call, method));
        if (cik.Result.HasValue)
            il.Append(il.Create(OpCodes.Stloc, getLocal(cik.Result.Value)));
    }

    /// <summary>FileRuntime.ShouldRunUseDeclarative(fileName, scope, excludeAtEnd, excludeInvalidKey) -> bool.</summary>
    internal void EmitCheckUseDeclarative(ILProcessor il, IrCheckUseDeclarative cud,
        Func<IrValue, VariableDefinition> getLocal)
    {
        il.Append(il.Create(OpCodes.Ldstr, cud.FileName));
        il.Append(il.Create(OpCodes.Ldc_I4, cud.Scope));
        il.Append(il.Create(cud.ExcludeAtEnd ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
        il.Append(il.Create(cud.ExcludeInvalidKey ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
        var method = _ctx.Module.ImportReference(
            typeof(FileRuntime).GetMethod("ShouldRunUseDeclarative",
                new[] { typeof(string), typeof(int), typeof(bool), typeof(bool) })!);
        il.Append(il.Create(OpCodes.Call, method));
        if (cud.Result.HasValue)
            il.Append(il.Create(OpCodes.Stloc, getLocal(cud.Result.Value)));
    }

    // ── SORT / MERGE ──

    internal void EmitSortInit(ILProcessor il, IrSortInit inst)
    {
        il.Append(il.Create(OpCodes.Ldstr, inst.FileName));
        il.Append(il.Create(OpCodes.Ldc_I4, inst.RecordLength));
        var m = _ctx.Module.ImportReference(
            typeof(SortRuntime).GetMethod("InitSortFile",
                new[] { typeof(string), typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, m));
    }

    internal void EmitSortRelease(ILProcessor il, IrSortRelease inst)
    {
        il.Append(il.Create(OpCodes.Ldstr, inst.FileName));
        _ctx.Location.EmitLocationArgs(il, inst.Record);
        var m = _ctx.Module.ImportReference(
            typeof(SortRuntime).GetMethod("ReleaseRecord",
                new[] { typeof(string), typeof(byte[]), typeof(int), typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, m));
    }

    /// <summary>SortRuntime.RewindReturn(fileName) — reset the return cursor before a GIVING file's write loop.</summary>
    internal void EmitSortRewind(ILProcessor il, IrSortRewind inst)
    {
        il.Append(il.Create(OpCodes.Ldstr, inst.FileName));
        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
            typeof(SortRuntime).GetMethod("RewindReturn", new[] { typeof(string) })!)));
    }

    internal void EmitSortSort(ILProcessor il, IrSortSort inst)
    {
        il.Append(il.Create(OpCodes.Ldstr, inst.FileName));
        il.Append(il.Create(OpCodes.Ldstr, inst.KeysSpec));
        EmitCollatingArg(il, inst.CollatingSequence);
        var m = _ctx.Module.ImportReference(
            typeof(SortRuntime).GetMethod("SortRecords",
                new[] { typeof(string), typeof(string), typeof(byte[]) })!);
        il.Append(il.Create(OpCodes.Call, m));
    }

    internal void EmitSortReturn(ILProcessor il, IrSortReturn inst,
        Func<IrValue, VariableDefinition> getLocal)
    {
        il.Append(il.Create(OpCodes.Ldstr, inst.FileName));
        _ctx.Location.EmitLocationArgs(il, inst.Record);
        var m = _ctx.Module.ImportReference(
            typeof(SortRuntime).GetMethod("ReturnRecord",
                new[] { typeof(string), typeof(byte[]), typeof(int), typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, m));
        // Store bool result
        var local = getLocal(inst.Result!.Value);
        il.Append(il.Create(OpCodes.Stloc, local));
    }

    /// <summary>Variable-length SORT … USING release: SortRuntime.ReleaseRecord(sortName, area, offset,
    /// GetLastRecordLength(inputFile)) — releases exactly the bytes just read, not the record's declared max.</summary>
    internal void EmitSortReleaseVariable(ILProcessor il, IrSortReleaseVariable inst)
    {
        il.Append(il.Create(OpCodes.Ldstr, inst.FileName));      // sortName
        _ctx.Location.EmitLocationArgs(il, inst.Record);         // area, offset, declaredSize
        il.Append(il.Create(OpCodes.Pop));                       // drop declaredSize
        il.Append(il.Create(OpCodes.Ldstr, inst.LengthFileName)); // input file name
        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
            typeof(FileRuntime).GetMethod("GetLastRecordLength", new[] { typeof(string) })!))); // -> actual length
        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
            typeof(SortRuntime).GetMethod("ReleaseRecord",
                new[] { typeof(string), typeof(byte[]), typeof(int), typeof(int) })!)));
    }

    /// <summary>Variable-length SORT … GIVING write: StorageHelpers.WriteRecordVariableToFile(outputFile,
    /// area, offset, GetLastReturnedLength(sortFile)) — re-emits the returned record at its own length.</summary>
    internal void EmitSortGivingWriteVariable(ILProcessor il, IrSortGivingWriteVariable inst)
    {
        il.Append(il.Create(OpCodes.Ldstr, inst.FileName));      // output file name
        _ctx.Location.EmitLocationArgs(il, inst.Record);         // area, offset, declaredSize
        il.Append(il.Create(OpCodes.Pop));                       // drop declaredSize
        il.Append(il.Create(OpCodes.Ldstr, inst.SortFileName));  // sort file name
        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
            typeof(SortRuntime).GetMethod("GetLastReturnedLength", new[] { typeof(string) })!))); // -> length
        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
            typeof(StorageHelpers).GetMethod("WriteRecordVariableToFile",
                new[] { typeof(string), typeof(byte[]), typeof(int), typeof(int) })!)));
    }

    /// <summary>Explicit RELEASE of a variable-length record: SortRuntime.ReleaseRecord(sortName, area, offset,
    /// ReadFieldAsInt(depending)) — releases exactly the bytes the program's DEPENDING ON item indicates.</summary>
    internal void EmitSortReleaseFromDepending(ILProcessor il, IrSortReleaseFromDepending inst)
    {
        il.Append(il.Create(OpCodes.Ldstr, inst.FileName));      // sortName
        _ctx.Location.EmitLocationArgs(il, inst.Record);         // area, offset, declaredSize
        il.Append(il.Create(OpCodes.Pop));                       // drop declaredSize
        _ctx.Location.EmitLocationArgs(il, inst.LengthLocation); // depending: area, offset, size
        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
            typeof(StorageHelpers).GetMethod("ReadFieldAsInt",
                new[] { typeof(byte[]), typeof(int), typeof(int) })!)));   // -> int length
        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
            typeof(SortRuntime).GetMethod("ReleaseRecord",
                new[] { typeof(string), typeof(byte[]), typeof(int), typeof(int) })!)));
    }

    /// <summary>After an explicit RETURN of a variable-length record, store SortRuntime.GetLastReturnedLength
    /// into the SD's DEPENDING ON item: MoveIntToField(area, offset, size, GetLastReturnedLength(sortFile)).</summary>
    internal void EmitSortReturnStoreLength(ILProcessor il, IrSortReturnStoreLength inst)
    {
        _ctx.Location.EmitLocationArgs(il, inst.LengthVariable); // area, offset, size
        il.Append(il.Create(OpCodes.Ldstr, inst.FileName));
        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
            typeof(SortRuntime).GetMethod("GetLastReturnedLength", new[] { typeof(string) })!))); // -> int
        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
            typeof(StorageHelpers).GetMethod("MoveIntToField",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(int) })!)));
    }

    internal void EmitTableSort(ILProcessor il, IrTableSort inst)
    {
        // SortRuntime.SortTable(byte[] storageArea, int tableOffset, int entrySize, int entryCount, string keysSpec)
        _ctx.Location.EmitLocationArgs(il, inst.TableLocation); // pushes byte[], offset, length
        il.Append(il.Create(OpCodes.Pop)); // pop the length — we don't need it
        il.Append(il.Create(OpCodes.Ldc_I4, inst.EntrySize));
        il.Append(il.Create(OpCodes.Ldc_I4, inst.EntryCount));
        il.Append(il.Create(OpCodes.Ldstr, inst.KeysSpec));
        EmitCollatingArg(il, inst.CollatingSequence);
        var m = _ctx.Module.ImportReference(
            typeof(SortRuntime).GetMethod("SortTable",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(int), typeof(string), typeof(byte[]) })!);
        il.Append(il.Create(OpCodes.Call, m));
    }

    internal void EmitSortClose(ILProcessor il, IrSortClose inst)
    {
        il.Append(il.Create(OpCodes.Ldstr, inst.FileName));
        var m = _ctx.Module.ImportReference(
            typeof(SortRuntime).GetMethod("CloseSortFile",
                new[] { typeof(string) })!);
        il.Append(il.Create(OpCodes.Call, m));
    }

    internal void EmitSortMerge(ILProcessor il, IrSortMerge inst)
    {
        il.Append(il.Create(OpCodes.Ldstr, inst.MergeFileName));
        il.Append(il.Create(OpCodes.Ldstr, inst.InputFileNames));
        il.Append(il.Create(OpCodes.Ldstr, inst.KeysSpec));
        EmitCollatingArg(il, inst.CollatingSequence);
        var m = _ctx.Module.ImportReference(
            typeof(SortRuntime).GetMethod("MergeRecords",
                new[] { typeof(string), typeof(string), typeof(string), typeof(byte[]) })!);
        il.Append(il.Create(OpCodes.Call, m));
    }

    /// <summary>
    /// Push the optional collating-sequence argument: the baked 256-byte table, or null
    /// (native byte order). Mirrors how comparison emission bakes the collating table inline.
    /// </summary>
    private void EmitCollatingArg(ILProcessor il, byte[]? collatingSequence)
    {
        if (collatingSequence == null)
            il.Append(il.Create(OpCodes.Ldnull));
        else
            _ctx.Expression.EmitByteArrayLiteral(il, collatingSequence);
    }
}
