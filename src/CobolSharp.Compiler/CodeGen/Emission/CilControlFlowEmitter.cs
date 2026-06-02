// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Mono.Cecil;
using Mono.Cecil.Cil;
using CobolSharp.Compiler.IR;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.CodeGen.Emission;

/// <summary>
/// Control flow emission: EmitBranch, EmitReturn, EmitPerform, EmitPerformTimes,
/// EmitPerformInlineTimes, EmitPerformThru, EmitGoToDepending,
/// plus inline cases for IrJump, IrBranchIfFalse, IrReturnConst, IrReturnAlterable,
/// IrAlter, IrStopRun, IrExitProgram, IrGoBack, IrSetSwitch, IrTestSwitch.
/// </summary>
internal sealed class CilControlFlowEmitter
{
    private readonly EmissionContext _ctx;

    internal CilControlFlowEmitter(EmissionContext ctx) => _ctx = ctx;

    // ── Branch / Return ──

    internal void EmitBranch(ILProcessor il, IrBranch br,
        Func<IrValue, VariableDefinition> getLocal,
        Dictionary<IrBasicBlock, Instruction> blockLabels)
    {
        var condLocal = getLocal(br.Condition);
        il.Append(il.Create(OpCodes.Ldloc, condLocal));
        il.Append(il.Create(OpCodes.Brtrue, blockLabels[br.TrueTarget]));
        il.Append(il.Create(OpCodes.Br, blockLabels[br.FalseTarget]));
    }

    internal void EmitReturn(ILProcessor il, IrReturn ret,
        Func<IrValue, VariableDefinition> getLocal)
    {
        if (ret.Value is { } val)
        {
            var local = getLocal(val);
            il.Append(il.Create(OpCodes.Ldloc, local));
        }
        il.Append(il.Create(OpCodes.Ret));
    }

    // ── PERFORM ──

    internal void EmitPerform(ILProcessor il, IrPerform perf)
    {
        var target = _ctx.MethodMap[perf.Target];
        il.Append(il.Create(OpCodes.Call, target));
        // Paragraph methods return int (next PC); discard in PERFORM context
        if (target.ReturnType != _ctx.Module.TypeSystem.Void)
            il.Append(il.Create(OpCodes.Pop));
    }

    /// <summary>
    /// PERFORM N TIMES: evaluates count expression into a CIL local int,
    /// then loops calling the paragraph method(s) that many times.
    /// </summary>
    internal void EmitPerformTimes(ILProcessor il, IrPerformTimes pt, MethodDefinition md)
    {
        // Evaluate count expression -> decimal -> int -> store in local
        var counterLocal = new VariableDefinition(_ctx.Module.TypeSystem.Int32);
        md.Body.Variables.Add(counterLocal);

        _ctx.Expression.EmitIrExpression(il, pt.CountExpression);
        var toInt32 = _ctx.Module.ImportReference(
            typeof(Convert).GetMethod("ToInt32", new[] { typeof(decimal) })!);
        il.Append(il.Create(OpCodes.Call, toInt32));
        il.Append(il.Create(OpCodes.Stloc, counterLocal));

        // Loop: while counter > 0
        var loopStart = il.Create(OpCodes.Nop);
        var loopEnd = il.Create(OpCodes.Nop);

        il.Append(loopStart);

        // Check: counter > 0
        il.Append(il.Create(OpCodes.Ldloc, counterLocal));
        il.Append(il.Create(OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Ble, loopEnd));

        // Body: call paragraph(s)
        if (pt.StartIdx == pt.EndIdx)
        {
            var target = _ctx.MethodMap[pt.Target];
            il.Append(il.Create(OpCodes.Call, target));
            if (target.ReturnType != _ctx.Module.TypeSystem.Void)
                il.Append(il.Create(OpCodes.Pop));
        }
        else
        {
            // THRU: reuse EmitPerformThru with a synthetic IrPerformThru
            var syntheticThru = new IrPerformThru(pt.StartIdx, pt.EndIdx, pt.ThruMethods);
            EmitPerformThru(il, syntheticThru, md);
        }

        // Decrement counter
        il.Append(il.Create(OpCodes.Ldloc, counterLocal));
        il.Append(il.Create(OpCodes.Ldc_I4_1));
        il.Append(il.Create(OpCodes.Sub));
        il.Append(il.Create(OpCodes.Stloc, counterLocal));

        // Loop back
        il.Append(il.Create(OpCodes.Br, loopStart));

        il.Append(loopEnd);
    }

    /// <summary>
    /// Inline PERFORM N TIMES: evaluates count expression into a CIL local int,
    /// then loops over the body instructions that many times.
    /// The body instructions are emitted inline (no paragraph call).
    /// </summary>
    internal void EmitPerformInlineTimes(ILProcessor il, IrPerformInlineTimes pit,
        MethodDefinition md, Func<IrValue, VariableDefinition> getLocal,
        Dictionary<IrBasicBlock, Instruction> blockLabels)
    {
        // Create counter local
        var counterLocal = new VariableDefinition(_ctx.Module.TypeSystem.Int32);
        md.Body.Variables.Add(counterLocal);

        // Evaluate count expression -> decimal -> int -> store in counter
        _ctx.Expression.EmitIrExpression(il, pit.CountExpression);
        var toInt32 = _ctx.Module.ImportReference(
            typeof(Convert).GetMethod("ToInt32", new[] { typeof(decimal) })!);
        il.Append(il.Create(OpCodes.Call, toInt32));
        il.Append(il.Create(OpCodes.Stloc, counterLocal));

        // Loop: while counter > 0
        var loopStart = il.Create(OpCodes.Nop);
        var loopEnd = il.Create(OpCodes.Nop);

        il.Append(loopStart);

        // Check: counter > 0
        il.Append(il.Create(OpCodes.Ldloc, counterLocal));
        il.Append(il.Create(OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Ble, loopEnd));

        // Emit body instructions inline
        foreach (var bodyInst in pit.BodyInstructions)
            _ctx.EmitInstruction(il, bodyInst, getLocal, blockLabels);

        // Decrement counter
        il.Append(il.Create(OpCodes.Ldloc, counterLocal));
        il.Append(il.Create(OpCodes.Ldc_I4_1));
        il.Append(il.Create(OpCodes.Sub));
        il.Append(il.Create(OpCodes.Stloc, counterLocal));

        // Loop back
        il.Append(il.Create(OpCodes.Br, loopStart));

        il.Append(loopEnd);
    }

    /// <summary>
    /// PERFORM proc-1 THRU proc-2: delegate to the shared Dispatch helper with the range's TRUE
    /// endpoints — <c>Dispatch(startIndex, endIndex)</c>. Dispatch enters at startIndex, follows the
    /// program's control flow anywhere (GO TOs leaving/re-entering the range, and inverted ranges
    /// where proc-2 physically precedes proc-1), and returns when proc-2 (endIndex) falls off its end
    /// (ISO §14.9.30). The returned pc is discarded (PERFORM is a call, not a branch); a propagated
    /// STOP RUN (−1) is likewise discarded here, matching the prior physical-range behaviour.
    /// thru.Paragraphs is unused — the helper switches over the full paragraph table.
    /// </summary>
    internal void EmitPerformThru(ILProcessor il, IrPerformThru thru, MethodDefinition md)
    {
        il.Append(il.Create(OpCodes.Ldc_I4, thru.StartIndex));
        il.Append(il.Create(OpCodes.Ldc_I4, thru.EndIndex));
        il.Append(il.Create(OpCodes.Call, _ctx.DispatchMethod));
        il.Append(il.Create(OpCodes.Pop));
    }

    // ── GO TO DEPENDING ──

    internal void EmitGoToDepending(ILProcessor il, IrGoToDepending gtd,
        Func<IrValue, VariableDefinition> getLocal)
    {
        // Decode selector to int: PicRuntime.DecodeNumeric(area, offset, length, pic) -> decimal
        _ctx.Location.EmitLocationArgsWithPic(il, gtd.Selector);

        var decodeMethod = _ctx.Module.ImportReference(
            typeof(PicRuntime).GetMethod("DecodeNumeric",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor) })!);
        il.Append(il.Create(OpCodes.Call, decodeMethod));

        // Convert decimal -> int32
        var toInt = _ctx.Module.ImportReference(
            typeof(Convert).GetMethod("ToInt32", new[] { typeof(decimal) })!);
        il.Append(il.Create(OpCodes.Call, toInt));

        // Store in a local
        var selectorLocal = new VariableDefinition(_ctx.Module.TypeSystem.Int32);
        _ctx.CurrentMethodDef!.Body.Variables.Add(selectorLocal);
        il.Append(il.Create(OpCodes.Stloc, selectorLocal));

        // Emit cascaded: if (selector == 1) return target[0]; if (selector == 2) return target[1]; ...
        for (int i = 0; i < gtd.TargetParagraphIndices.Count; i++)
        {
            int value = i + 1; // 1-based
            int targetPc = gtd.TargetParagraphIndices[i];

            var nextCheck = il.Create(OpCodes.Nop);

            il.Append(il.Create(OpCodes.Ldloc, selectorLocal));
            il.Append(il.Create(OpCodes.Ldc_I4, value));
            il.Append(il.Create(OpCodes.Bne_Un, nextCheck));

            // Match: return the target PC
            il.Append(il.Create(OpCodes.Ldc_I4, targetPc));
            il.Append(il.Create(OpCodes.Ret));

            il.Append(nextCheck);
        }

        // No match: fall through (don't return, let execution continue)
    }

    // ── Inline instruction cases ──

    internal void EmitJump(ILProcessor il, IrJump j,
        Dictionary<IrBasicBlock, Instruction> blockLabels)
    {
        il.Append(il.Create(OpCodes.Br, blockLabels[j.Target]));
    }

    internal void EmitBranchIfFalse(ILProcessor il, IrBranchIfFalse bif,
        Func<IrValue, VariableDefinition> getLocal,
        Dictionary<IrBasicBlock, Instruction> blockLabels)
    {
        var condLocal = getLocal(bif.Condition);
        il.Append(il.Create(OpCodes.Ldloc, condLocal));
        il.Append(il.Create(OpCodes.Brfalse, blockLabels[bif.Target]));
    }

    internal void EmitReturnConst(ILProcessor il, IrReturnConst rc)
    {
        il.Append(il.Create(OpCodes.Ldc_I4, rc.Value));
        il.Append(il.Create(OpCodes.Ret));
    }

    internal void EmitReturnAlterable(ILProcessor il, IrReturnAlterable ra)
    {
        // return _alterTable[slot]
        il.Append(il.Create(OpCodes.Ldsfld, _ctx.AlterTableField!));
        il.Append(il.Create(OpCodes.Ldc_I4, ra.AlterSlot));
        il.Append(il.Create(OpCodes.Ldelem_I4));
        il.Append(il.Create(OpCodes.Ret));
    }

    internal void EmitAlter(ILProcessor il, IrAlter alt)
    {
        // _alterTable[slot] = newTargetIndex
        il.Append(il.Create(OpCodes.Ldsfld, _ctx.AlterTableField!));
        il.Append(il.Create(OpCodes.Ldc_I4, alt.AlterSlot));
        il.Append(il.Create(OpCodes.Ldc_I4, alt.NewTargetIndex));
        il.Append(il.Create(OpCodes.Stelem_I4));
    }

    internal void EmitStopRun(ILProcessor il)
    {
        // STOP RUN: exit the paragraph dispatch loop by returning -1.
        il.Append(il.Create(OpCodes.Ldc_I4_M1));
        il.Append(il.Create(OpCodes.Ret));
    }

    internal void EmitExitProgram(ILProcessor il)
    {
        // EXIT PROGRAM: return -1 from paragraph method (exit dispatch loop)
        il.Append(il.Create(OpCodes.Ldc_I4_M1));
        il.Append(il.Create(OpCodes.Ret));
    }

    internal void EmitGoBack(ILProcessor il)
    {
        // GOBACK: same as EXIT PROGRAM (return from dispatch loop)
        il.Append(il.Create(OpCodes.Ldc_I4_M1));
        il.Append(il.Create(OpCodes.Ret));
    }

    internal void EmitSetSwitch(ILProcessor il, IrSetSwitch ss)
    {
        // Call SwitchRuntime.SetSwitchState(implementorName, isOn)
        il.Append(il.Create(OpCodes.Ldstr, ss.ImplementorName));
        il.Append(il.Create(ss.SetToOn ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
        var setSwitchMethod = _ctx.Module.ImportReference(
            typeof(SwitchRuntime).GetMethod("SetSwitchState")!);
        il.Append(il.Create(OpCodes.Call, setSwitchMethod));
    }

    internal void EmitTestSwitch(ILProcessor il, IrTestSwitch ts,
        Func<IrValue, VariableDefinition> getLocal)
    {
        // Call SwitchRuntime.GetSwitchState(implementorName)
        il.Append(il.Create(OpCodes.Ldstr, ts.ImplementorName));
        var getSwitchMethod = _ctx.Module.ImportReference(
            typeof(SwitchRuntime).GetMethod("GetSwitchState")!);
        il.Append(il.Create(OpCodes.Call, getSwitchMethod));
        // If testing OFF state, negate the result
        if (!ts.TestOnState)
        {
            il.Append(il.Create(OpCodes.Ldc_I4_0));
            il.Append(il.Create(OpCodes.Ceq));
        }
        if (ts.Result.HasValue)
        {
            var local = getLocal(ts.Result.Value);
            il.Append(il.Create(OpCodes.Stloc, local));
        }
    }
}
