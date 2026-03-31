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
    /// WRITE AFTER ADVANCING: calls FileRuntime.WriteAfterAdvancing(string, byte[], int, int, int).
    /// </summary>
    /// <summary>
    /// REWRITE record: calls FileRuntime.Rewrite(string, byte[], int, int).
    /// </summary>
    internal void EmitRewriteRecordFromStorage(ILProcessor il, IrRewriteRecordFromStorage rw)
    {
        il.Append(il.Create(OpCodes.Ldstr, rw.FileName));
        _ctx.Location.EmitLocationArgs(il, rw.Record);

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

        var method = _ctx.Module.ImportReference(
            typeof(FileRuntime).GetMethod(
                "ReadByKey",
                new[] { typeof(string), typeof(byte[]), typeof(int), typeof(int),
                        typeof(byte[]), typeof(int), typeof(int) })!);
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
        // FileRuntime.IsAtEnd(string fileName)
        il.Append(il.Create(OpCodes.Ldstr, chk.FileName));
        var method = _ctx.Module.ImportReference(
            typeof(FileRuntime).GetMethod(
                "IsAtEnd",
                new[] { typeof(string) })!);
        il.Append(il.Create(OpCodes.Call, method));
        if (chk.Result.HasValue)
            il.Append(il.Create(OpCodes.Stloc, getLocal(chk.Result.Value)));
        else
            il.Append(il.Create(OpCodes.Pop));
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
        var method = _ctx.Module.ImportReference(
            typeof(FileRuntime).GetMethod("StartFile",
                new[] { typeof(string), typeof(byte[]), typeof(int), typeof(int), typeof(int) })!);
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

    internal void EmitSortSort(ILProcessor il, IrSortSort inst)
    {
        il.Append(il.Create(OpCodes.Ldstr, inst.FileName));
        il.Append(il.Create(OpCodes.Ldstr, inst.KeysSpec));
        var m = _ctx.Module.ImportReference(
            typeof(SortRuntime).GetMethod("SortRecords",
                new[] { typeof(string), typeof(string) })!);
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
        var m = _ctx.Module.ImportReference(
            typeof(SortRuntime).GetMethod("MergeRecords",
                new[] { typeof(string), typeof(string), typeof(string) })!);
        il.Append(il.Create(OpCodes.Call, m));
    }
}
