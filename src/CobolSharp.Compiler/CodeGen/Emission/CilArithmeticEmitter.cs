// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Mono.Cecil;
using Mono.Cecil.Cil;
using CobolSharp.Compiler.IR;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.CodeGen.Emission;

/// <summary>
/// Arithmetic emission: EmitBinary, EmitPicMultiply, EmitPicMultiplyLiteral,
/// EmitPicAdd, EmitPicAddLiteral, EmitPicSubtract, EmitPicSubtractLiteral,
/// EmitAddAccumulatedToTarget, EmitMoveAccumulatedToTarget,
/// EmitSubtractAccumulatedFromTarget, EmitPicDivide, EmitPicDivideLiteral,
/// EmitComputeStore, EmitCobolRemainder,
/// EnsureArithmeticStatusLocal, EmitInitArithmeticStatus, EmitLoadArithmeticStatusRef.
/// </summary>
internal sealed class CilArithmeticEmitter
{
    private readonly EmissionContext _ctx;

    internal CilArithmeticEmitter(EmissionContext ctx) => _ctx = ctx;

    internal void EmitBinary(ILProcessor il, IrBinary bin,
        Func<IrValue, VariableDefinition> getLocal)
    {
        var leftLocal = getLocal(bin.Left);
        var rightLocal = getLocal(bin.Right);

        il.Append(il.Create(OpCodes.Ldloc, leftLocal));
        il.Append(il.Create(OpCodes.Ldloc, rightLocal));

        bool needsNegate = bin.Op is IrBinaryOp.Ne or IrBinaryOp.Le or IrBinaryOp.Ge;

        var op = bin.Op switch
        {
            IrBinaryOp.Add => OpCodes.Add,
            IrBinaryOp.Sub => OpCodes.Sub,
            IrBinaryOp.Mul => OpCodes.Mul,
            IrBinaryOp.Div => OpCodes.Div,
            IrBinaryOp.Eq => OpCodes.Ceq,
            IrBinaryOp.Ne => OpCodes.Ceq,   // negate after
            IrBinaryOp.Lt => OpCodes.Clt,
            IrBinaryOp.Le => OpCodes.Cgt,   // negate after
            IrBinaryOp.Gt => OpCodes.Cgt,
            IrBinaryOp.Ge => OpCodes.Clt,   // negate after
            IrBinaryOp.And => OpCodes.And,
            IrBinaryOp.Or => OpCodes.Or,
            _ => throw new NotSupportedException($"Binary op {bin.Op}")
        };

        il.Append(il.Create(op));

        if (needsNegate)
        {
            // Logical negate: push 0, ceq (turns 1->0 and 0->1)
            il.Append(il.Create(OpCodes.Ldc_I4_0));
            il.Append(il.Create(OpCodes.Ceq));
        }

        if (bin.Result is { } res)
        {
            var local = getLocal(res);
            il.Append(il.Create(OpCodes.Stloc, local));
        }
    }

    internal void EmitPicMultiply(ILProcessor il, IrPicMultiply mul)
    {
        _ctx.Location.EmitLocationArgsWithPic(il, mul.Destination);
        _ctx.Location.EmitLocationArgsWithPic(il, mul.Left);
        _ctx.Location.EmitLocationArgsWithPic(il, mul.Right);

        il.Append(il.Create(OpCodes.Ldc_I4, mul.Rounding));
        EmitLoadArithmeticStatusRef(il, _ctx.CurrentMethodDef!);

        var method = _ctx.Module.ImportReference(
            typeof(PicRuntime).GetMethod("MultiplyNumeric",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor),
                        typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor),
                        typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor),
                        typeof(int), typeof(ArithmeticStatus).MakeByRefType() })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    internal void EmitPicMultiplyLiteral(ILProcessor il, IrPicMultiplyLiteral mul)
    {
        _ctx.Location.EmitLocationArgsWithPic(il, mul.Destination);
        _ctx.Expression.EmitLoadDecimal(il, mul.Value);
        _ctx.Location.EmitLocationArgsWithPic(il, mul.Other);
        il.Append(il.Create(OpCodes.Ldc_I4, mul.Rounding));
        EmitLoadArithmeticStatusRef(il, _ctx.CurrentMethodDef!);

        var method = _ctx.Module.ImportReference(
            typeof(PicRuntime).GetMethod("MultiplyNumericLiteral",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor),
                        typeof(decimal),
                        typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor),
                        typeof(int), typeof(ArithmeticStatus).MakeByRefType() })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    internal void EmitPicAdd(ILProcessor il, IrPicAdd add)
    {
        _ctx.Location.EmitLocationArgsWithPic(il, add.Destination);
        _ctx.Location.EmitLocationArgsWithPic(il, add.Source);
        il.Append(il.Create(OpCodes.Ldc_I4, add.Rounding));
        EmitLoadArithmeticStatusRef(il, _ctx.CurrentMethodDef!);

        var method = _ctx.Module.ImportReference(
            typeof(PicRuntime).GetMethod("AddNumeric",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor),
                        typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor),
                        typeof(int), typeof(ArithmeticStatus).MakeByRefType() })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    internal void EmitPicAddLiteral(ILProcessor il, IrPicAddLiteral add)
    {
        _ctx.Location.EmitLocationArgsWithPic(il, add.Destination);
        _ctx.Expression.EmitLoadDecimal(il, add.Value);
        il.Append(il.Create(OpCodes.Ldc_I4, add.Rounding));
        EmitLoadArithmeticStatusRef(il, _ctx.CurrentMethodDef!);

        var method = _ctx.Module.ImportReference(
            typeof(PicRuntime).GetMethod("AddNumericLiteral",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor),
                        typeof(decimal), typeof(int), typeof(ArithmeticStatus).MakeByRefType() })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    internal void EmitPicSubtract(ILProcessor il, IrPicSubtract sub)
    {
        _ctx.Location.EmitLocationArgsWithPic(il, sub.Destination);
        _ctx.Location.EmitLocationArgsWithPic(il, sub.Source);
        il.Append(il.Create(OpCodes.Ldc_I4, sub.Rounding));
        EmitLoadArithmeticStatusRef(il, _ctx.CurrentMethodDef!);

        var method = _ctx.Module.ImportReference(
            typeof(PicRuntime).GetMethod("SubtractNumeric",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor),
                        typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor),
                        typeof(int), typeof(ArithmeticStatus).MakeByRefType() })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    internal void EmitPicSubtractLiteral(ILProcessor il, IrPicSubtractLiteral sub)
    {
        _ctx.Location.EmitLocationArgsWithPic(il, sub.Destination);
        _ctx.Expression.EmitLoadDecimal(il, sub.Value);
        il.Append(il.Create(OpCodes.Ldc_I4, sub.Rounding));
        EmitLoadArithmeticStatusRef(il, _ctx.CurrentMethodDef!);

        var method = _ctx.Module.ImportReference(
            typeof(PicRuntime).GetMethod("SubtractNumericLiteral",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor),
                        typeof(decimal), typeof(int), typeof(ArithmeticStatus).MakeByRefType() })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    internal void EmitAddAccumulatedToTarget(ILProcessor il, IrAddAccumulatedToTarget inst,
        Func<IrValue, VariableDefinition> getLocal)
    {
        _ctx.Location.EmitLocationArgsWithPic(il, inst.Destination);
        var accLocal = getLocal(inst.Accumulator);
        il.Append(il.Create(OpCodes.Ldloc, accLocal));
        il.Append(il.Create(OpCodes.Ldc_I4, inst.Rounding));
        EmitLoadArithmeticStatusRef(il, _ctx.CurrentMethodDef!);

        var method = _ctx.Module.ImportReference(
            typeof(PicRuntime).GetMethod("AddAccumulatedToField",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor),
                        typeof(decimal), typeof(int), typeof(ArithmeticStatus).MakeByRefType() })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    internal void EmitMoveAccumulatedToTarget(ILProcessor il, IrMoveAccumulatedToTarget inst,
        Func<IrValue, VariableDefinition> getLocal)
    {
        _ctx.Location.EmitLocationArgsWithPic(il, inst.Destination);
        var accLocal = getLocal(inst.Accumulator);
        il.Append(il.Create(OpCodes.Ldloc, accLocal));
        il.Append(il.Create(OpCodes.Ldc_I4, inst.Rounding));
        EmitLoadArithmeticStatusRef(il, _ctx.CurrentMethodDef!);

        var method = _ctx.Module.ImportReference(
            typeof(PicRuntime).GetMethod("MoveAccumulatedToField",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor),
                        typeof(decimal), typeof(int), typeof(ArithmeticStatus).MakeByRefType() })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    internal void EmitSubtractAccumulatedFromTarget(ILProcessor il, IrSubtractAccumulatedFromTarget inst,
        Func<IrValue, VariableDefinition> getLocal)
    {
        _ctx.Location.EmitLocationArgsWithPic(il, inst.Destination);
        var accLocal = getLocal(inst.Accumulator);
        il.Append(il.Create(OpCodes.Ldloc, accLocal));
        il.Append(il.Create(OpCodes.Ldc_I4, inst.Rounding));
        EmitLoadArithmeticStatusRef(il, _ctx.CurrentMethodDef!);

        var method = _ctx.Module.ImportReference(
            typeof(PicRuntime).GetMethod("SubtractAccumulatedFromField",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor),
                        typeof(decimal), typeof(int), typeof(ArithmeticStatus).MakeByRefType() })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    internal void EmitPicDivide(ILProcessor il, IrPicDivide div)
    {
        _ctx.Location.EmitLocationArgsWithPic(il, div.Destination);
        _ctx.Location.EmitLocationArgsWithPic(il, div.Left);
        _ctx.Location.EmitLocationArgsWithPic(il, div.Right);

        il.Append(il.Create(OpCodes.Ldc_I4, div.Rounding));
        EmitLoadArithmeticStatusRef(il, _ctx.CurrentMethodDef!);

        var method = _ctx.Module.ImportReference(
            typeof(PicRuntime).GetMethod("DivideNumeric",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor),
                        typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor),
                        typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor),
                        typeof(int), typeof(ArithmeticStatus).MakeByRefType() })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    internal void EmitPicDivideLiteral(ILProcessor il, IrPicDivideLiteral div)
    {
        _ctx.Location.EmitLocationArgsWithPic(il, div.Destination);
        _ctx.Expression.EmitLoadDecimal(il, div.Value);
        _ctx.Location.EmitLocationArgsWithPic(il, div.Other);
        il.Append(il.Create(OpCodes.Ldc_I4, div.Rounding));
        EmitLoadArithmeticStatusRef(il, _ctx.CurrentMethodDef!);

        var method = _ctx.Module.ImportReference(
            typeof(PicRuntime).GetMethod("DivideNumericLiteral",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor),
                        typeof(decimal),
                        typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor),
                        typeof(int), typeof(ArithmeticStatus).MakeByRefType() })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    /// <summary>
    /// COMPUTE/GIVING store: evaluate expression tree to decimal, then store to target.
    /// Routes through MoveAccumulatedToField — the unified "store decimal with overflow
    /// detection" path shared by all arithmetic operations.
    /// </summary>
    internal void EmitComputeStore(ILProcessor il, IrComputeStore cs)
    {
        _ctx.Location.EmitLocationArgsWithPic(il, cs.Destination);
        _ctx.Expression.EmitIrExpression(il, cs.Expression);
        il.Append(il.Create(OpCodes.Ldc_I4, cs.Rounding));
        EmitLoadArithmeticStatusRef(il, _ctx.CurrentMethodDef!);

        var method = _ctx.Module.ImportReference(
            typeof(PicRuntime).GetMethod("MoveAccumulatedToField",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor),
                        typeof(decimal), typeof(int),
                        typeof(ArithmeticStatus).MakeByRefType() })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    internal void EmitCobolRemainder(ILProcessor il, IrCobolRemainder rem,
        Func<IrValue, VariableDefinition> getLocal)
    {
        // Push: dividend(decimal), divisor(decimal), rawQuotient(decimal),
        //        givingFractionDigits(int), dest(area,off,len,pic), ref status
        _ctx.Expression.EmitIrExpression(il, rem.Dividend);
        _ctx.Expression.EmitIrExpression(il, rem.Divisor);
        il.Append(il.Create(OpCodes.Ldloc, getLocal(rem.QuotientAccumulator)));
        il.Append(il.Create(OpCodes.Ldc_I4, rem.GivingFractionDigits));
        _ctx.Location.EmitLocationArgsWithPic(il, rem.Destination);
        EmitLoadArithmeticStatusRef(il, _ctx.CurrentMethodDef!);

        var method = _ctx.Module.ImportReference(
            typeof(PicRuntime).GetMethod("ComputeCobolRemainder",
                new[] {
                    typeof(decimal), typeof(decimal), typeof(decimal),
                    typeof(int),
                    typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor),
                    typeof(ArithmeticStatus).MakeByRefType()
                })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    /// <summary>
    /// Lazily allocate one ArithmeticStatus local per method.
    /// </summary>
    internal VariableDefinition EnsureArithmeticStatusLocal(MethodDefinition md)
    {
        if (_ctx.ArithmeticStatusLocal == null)
        {
            _ctx.ArithmeticStatusLocal = new VariableDefinition(
                _ctx.Module.ImportReference(typeof(ArithmeticStatus)));
            md.Body.Variables.Add(_ctx.ArithmeticStatusLocal);
        }
        return _ctx.ArithmeticStatusLocal;
    }

    /// <summary>
    /// Zero-initialize the ArithmeticStatus local before an arithmetic call.
    /// </summary>
    internal void EmitInitArithmeticStatus(ILProcessor il, MethodDefinition md)
    {
        var statusLocal = EnsureArithmeticStatusLocal(md);
        il.Append(il.Create(OpCodes.Ldloca, statusLocal));
        il.Append(il.Create(OpCodes.Initobj,
            _ctx.Module.ImportReference(typeof(ArithmeticStatus))));
    }

    /// <summary>
    /// Push address of ArithmeticStatus local onto stack (for ref parameter).
    /// </summary>
    internal void EmitLoadArithmeticStatusRef(ILProcessor il, MethodDefinition md)
    {
        var statusLocal = EnsureArithmeticStatusLocal(md);
        il.Append(il.Create(OpCodes.Ldloca, statusLocal));
    }
}
