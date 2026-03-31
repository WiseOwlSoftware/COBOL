// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Mono.Cecil;
using Mono.Cecil.Cil;
using CobolSharp.Compiler.IR;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.CodeGen.Emission;

/// <summary>
/// Comparison emission: EmitClassCondition, EmitUserClassCondition,
/// EmitPicCompare, EmitPicCompareLiteral, EmitPicCompareAccumulator,
/// EmitDecimalCompare, EmitDecimalCompareLiteral, EmitCompareResultToBool,
/// EmitStringCompareLiteral, EmitStringCompare,
/// EmitStringCompareWithSequence, EmitStringCompareLiteralWithSequence.
/// </summary>
internal sealed class CilComparisonEmitter
{
    private readonly EmissionContext _ctx;

    internal CilComparisonEmitter(EmissionContext ctx) => _ctx = ctx;

    internal void EmitClassCondition(ILProcessor il, IrClassCondition inst,
        Func<IrValue, VariableDefinition> getLocal)
    {
        var kind = (IR.ClassConditionKind)inst.ClassKind;
        string methodName = kind switch
        {
            IR.ClassConditionKind.Numeric => "IsNumericClass",
            IR.ClassConditionKind.Alphabetic => "IsAlphabeticClass",
            IR.ClassConditionKind.AlphabeticLower => "IsAlphabeticLowerClass",
            IR.ClassConditionKind.AlphabeticUpper => "IsAlphabeticUpperClass",
            _ => throw new InvalidOperationException($"Unknown class condition: {kind}")
        };

        // IsNumericClass takes PicDescriptor; others don't
        System.Reflection.MethodInfo method;
        if (kind == IR.ClassConditionKind.Numeric)
        {
            _ctx.Location.EmitLocationArgsWithPic(il, inst.Subject);
            method = typeof(Runtime.PicRuntime).GetMethod(methodName,
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor) })!;
        }
        else
        {
            _ctx.Location.EmitLocationArgs(il, inst.Subject);
            method = typeof(Runtime.PicRuntime).GetMethod(methodName,
                new[] { typeof(byte[]), typeof(int), typeof(int) })!;
        }

        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(method)));

        if (inst.Result.HasValue)
        {
            var resLocal = getLocal(inst.Result.Value);
            il.Append(il.Create(OpCodes.Stloc, resLocal));
        }
    }

    internal void EmitUserClassCondition(ILProcessor il, IrUserClassCondition inst,
        Func<IrValue, VariableDefinition> getLocal)
    {
        _ctx.Location.EmitLocationArgs(il, inst.Subject);

        // Emit the valid bytes array as a static field or inline byte array
        _ctx.Expression.EmitByteArrayLiteral(il, inst.ValidBytes);

        var method = _ctx.Module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod("IsInUserClass",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(byte[]) })!);
        il.Append(il.Create(OpCodes.Call, method));

        if (inst.Result.HasValue)
        {
            var resLocal = getLocal(inst.Result.Value);
            il.Append(il.Create(OpCodes.Stloc, resLocal));
        }
    }

    internal void EmitPicCompare(ILProcessor il, IrPicCompare cmp,
        Func<IrValue, VariableDefinition> getLocal)
    {
        _ctx.Location.EmitLocationArgsWithPic(il, cmp.Left);
        _ctx.Location.EmitLocationArgsWithPic(il, cmp.Right);

        var method = _ctx.Module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod("CompareNumeric",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor) })!);
        il.Append(il.Create(OpCodes.Call, method));

        EmitCompareResultToBool(il, cmp.OperatorKind);

        if (cmp.Result.HasValue)
        {
            var resLocal = getLocal(cmp.Result.Value);
            il.Append(il.Create(OpCodes.Stloc, resLocal));
        }
    }

    internal void EmitPicCompareLiteral(ILProcessor il, IrPicCompareLiteral cmp,
        Func<IrValue, VariableDefinition> getLocal)
    {
        _ctx.Location.EmitLocationArgsWithPic(il, cmp.Left);
        _ctx.Expression.EmitLoadDecimal(il, cmp.Value);

        var method = _ctx.Module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod("CompareNumericToLiteral",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(decimal) })!);
        il.Append(il.Create(OpCodes.Call, method));

        EmitCompareResultToBool(il, cmp.OperatorKind);

        if (cmp.Result.HasValue)
        {
            var resLocal = getLocal(cmp.Result.Value);
            il.Append(il.Create(OpCodes.Stloc, resLocal));
        }
    }

    internal void EmitPicCompareAccumulator(ILProcessor il, IrPicCompareAccumulator cmp,
        Func<IrValue, VariableDefinition> getLocal)
    {
        _ctx.Location.EmitLocationArgsWithPic(il, cmp.Left);
        // Load the pre-evaluated accumulator (decimal)
        il.Append(il.Create(OpCodes.Ldloc, getLocal(cmp.Accumulator)));

        var method = _ctx.Module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod("CompareNumericToLiteral",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(decimal) })!);
        il.Append(il.Create(OpCodes.Call, method));

        EmitCompareResultToBool(il, cmp.OperatorKind);

        if (cmp.Result.HasValue)
        {
            var resLocal = getLocal(cmp.Result.Value);
            il.Append(il.Create(OpCodes.Stloc, resLocal));
        }
    }

    internal void EmitDecimalCompare(ILProcessor il, IrDecimalCompare cmp,
        Func<IrValue, VariableDefinition> getLocal)
    {
        // Load left accumulator, call CompareTo(right accumulator)
        il.Append(il.Create(OpCodes.Ldloca, getLocal(cmp.Left)));
        il.Append(il.Create(OpCodes.Ldloc, getLocal(cmp.Right)));
        var compareTo = _ctx.Module.ImportReference(
            typeof(decimal).GetMethod("CompareTo", new[] { typeof(decimal) })!);
        il.Append(il.Create(OpCodes.Call, compareTo));

        EmitCompareResultToBool(il, cmp.OperatorKind);

        if (cmp.Result.HasValue)
            il.Append(il.Create(OpCodes.Stloc, getLocal(cmp.Result.Value)));
    }

    internal void EmitDecimalCompareLiteral(ILProcessor il, IrDecimalCompareLiteral cmp,
        Func<IrValue, VariableDefinition> getLocal)
    {
        // Load accumulator, call CompareTo(literal)
        il.Append(il.Create(OpCodes.Ldloca, getLocal(cmp.Accumulator)));
        _ctx.Expression.EmitLoadDecimal(il, cmp.LiteralValue);
        var compareTo = _ctx.Module.ImportReference(
            typeof(decimal).GetMethod("CompareTo", new[] { typeof(decimal) })!);
        il.Append(il.Create(OpCodes.Call, compareTo));

        EmitCompareResultToBool(il, cmp.OperatorKind);

        if (cmp.Result.HasValue)
            il.Append(il.Create(OpCodes.Stloc, getLocal(cmp.Result.Value)));
    }

    /// <summary>
    /// Convert CompareNumeric result (-1/0/1) to bool based on operator kind.
    /// Uses enum values directly — never hardcode integer constants for enum members.
    /// </summary>
    internal void EmitCompareResultToBool(ILProcessor il, int operatorKind)
    {
        var op = (IR.IrCompareOp)operatorKind;
        switch (op)
        {
            case IR.IrCompareOp.Equal: // result == 0
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Ceq));
                break;
            case IR.IrCompareOp.NotEqual: // NOT (result == 0)
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Ceq));
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Ceq));
                break;
            case IR.IrCompareOp.Less: // result < 0
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Clt));
                break;
            case IR.IrCompareOp.LessOrEqual: // NOT (result > 0)
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Cgt));
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Ceq));
                break;
            case IR.IrCompareOp.Greater: // result > 0
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Cgt));
                break;
            case IR.IrCompareOp.GreaterOrEqual: // NOT (result < 0)
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Clt));
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Ceq));
                break;
            default: // Fallback: treat as equal
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Ceq));
                break;
        }
    }

    internal void EmitStringCompareLiteral(ILProcessor il, IrStringCompareLiteral cmp,
        Func<IrValue, VariableDefinition> getLocal)
    {
        _ctx.Location.EmitLocationArgs(il, cmp.Left);
        il.Append(il.Create(OpCodes.Ldstr, cmp.Value));

        var method = _ctx.Module.ImportReference(
            typeof(Runtime.StorageHelpers).GetMethod("CompareFieldToString",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(string) })!);
        il.Append(il.Create(OpCodes.Call, method));

        EmitCompareResultToBool(il, cmp.OperatorKind);

        if (cmp.Result.HasValue)
        {
            var resLocal = getLocal(cmp.Result.Value);
            il.Append(il.Create(OpCodes.Stloc, resLocal));
        }
    }

    internal void EmitStringCompare(ILProcessor il, IrStringCompare cmp,
        Func<IrValue, VariableDefinition> getLocal)
    {
        _ctx.Location.EmitLocationArgs(il, cmp.Left);
        _ctx.Location.EmitLocationArgs(il, cmp.Right);

        var method = _ctx.Module.ImportReference(
            typeof(Runtime.StorageHelpers).GetMethod("CompareFieldToField",
                new[] { typeof(byte[]), typeof(int), typeof(int),
                        typeof(byte[]), typeof(int), typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, method));

        EmitCompareResultToBool(il, cmp.OperatorKind);

        if (cmp.Result.HasValue)
        {
            var resLocal = getLocal(cmp.Result.Value);
            il.Append(il.Create(OpCodes.Stloc, resLocal));
        }
    }

    internal void EmitStringCompareWithSequence(ILProcessor il, IrStringCompareWithSequence cmp,
        Func<IrValue, VariableDefinition> getLocal)
    {
        _ctx.Location.EmitLocationArgs(il, cmp.Left);
        _ctx.Location.EmitLocationArgs(il, cmp.Right);
        _ctx.Expression.EmitByteArrayLiteral(il, cmp.CollatingSequence);

        var method = _ctx.Module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod("CompareAlphanumericWithSequence",
                new[] { typeof(byte[]), typeof(int), typeof(int),
                        typeof(byte[]), typeof(int), typeof(int), typeof(byte[]) })!);
        il.Append(il.Create(OpCodes.Call, method));

        EmitCompareResultToBool(il, cmp.OperatorKind);

        if (cmp.Result.HasValue)
        {
            var resLocal = getLocal(cmp.Result.Value);
            il.Append(il.Create(OpCodes.Stloc, resLocal));
        }
    }

    internal void EmitStringCompareLiteralWithSequence(ILProcessor il, IrStringCompareLiteralWithSequence cmp,
        Func<IrValue, VariableDefinition> getLocal)
    {
        // Convert the string literal to a byte array and compare using CompareAlphanumericWithSequence
        _ctx.Location.EmitLocationArgs(il, cmp.Left);

        // Create a temp byte array from the string literal
        var bytes = System.Text.Encoding.ASCII.GetBytes(cmp.Value);
        _ctx.Expression.EmitByteArrayLiteral(il, bytes);
        il.Append(il.Create(OpCodes.Ldc_I4_0)); // offset = 0
        il.Append(il.Create(OpCodes.Ldc_I4, bytes.Length)); // length

        _ctx.Expression.EmitByteArrayLiteral(il, cmp.CollatingSequence);

        var method = _ctx.Module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod("CompareAlphanumericWithSequence",
                new[] { typeof(byte[]), typeof(int), typeof(int),
                        typeof(byte[]), typeof(int), typeof(int), typeof(byte[]) })!);
        il.Append(il.Create(OpCodes.Call, method));

        EmitCompareResultToBool(il, cmp.OperatorKind);

        if (cmp.Result.HasValue)
        {
            var resLocal = getLocal(cmp.Result.Value);
            il.Append(il.Create(OpCodes.Stloc, resLocal));
        }
    }
}
