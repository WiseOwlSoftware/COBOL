// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Mono.Cecil;
using Mono.Cecil.Cil;
using CobolSharp.Compiler.IR;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.CodeGen.Emission;

/// <summary>
/// Expression emission: evaluates IrExpression trees leaving decimal on the IL stack,
/// emits intrinsic function calls, and provides literal emission helpers
/// (decimal, PicDescriptor, byte[]).
/// </summary>
internal sealed class CilExpressionEmitter
{
    private readonly EmissionContext _ctx;

    internal CilExpressionEmitter(EmissionContext ctx) => _ctx = ctx;

    /// <summary>
    /// Emit an IR-native expression tree, leaving a decimal on the IL stack.
    /// All locations are pre-resolved by the Binder during lowering — no fallback needed.
    /// </summary>
    internal void EmitIrExpression(ILProcessor il, IR.IrExpression expr)
    {
        switch (expr)
        {
            case IR.IrLiteral lit:
                EmitLoadDecimal(il, lit.Value);
                break;

            case IR.IrLoadNumeric load:
                _ctx.Location.EmitLocationArgsWithPic(il, load.Source);
                il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
                    typeof(Runtime.PicRuntime).GetMethod("DecodeNumeric",
                        new[] { typeof(byte[]), typeof(int), typeof(int),
                                typeof(Runtime.PicDescriptor) })!)));
                break;

            case IR.IrBinaryExpr bin:
                EmitIrExpression(il, bin.Left);
                EmitIrExpression(il, bin.Right);
                switch (bin.Op)
                {
                    case IR.IrArithmeticOp.Add:
                        il.Append(il.Create(OpCodes.Call,
                            _ctx.Module.ImportReference(typeof(decimal).GetMethod("op_Addition",
                                new[] { typeof(decimal), typeof(decimal) })!)));
                        break;
                    case IR.IrArithmeticOp.Subtract:
                        il.Append(il.Create(OpCodes.Call,
                            _ctx.Module.ImportReference(typeof(decimal).GetMethod("op_Subtraction",
                                new[] { typeof(decimal), typeof(decimal) })!)));
                        break;
                    case IR.IrArithmeticOp.Multiply:
                        il.Append(il.Create(OpCodes.Call,
                            _ctx.Module.ImportReference(typeof(decimal).GetMethod("op_Multiply",
                                new[] { typeof(decimal), typeof(decimal) })!)));
                        break;
                    case IR.IrArithmeticOp.Divide:
                        EmitLoadArithmeticStatusRef(il);
                        il.Append(il.Create(OpCodes.Call,
                            _ctx.Module.ImportReference(typeof(Runtime.PicRuntime).GetMethod("SafeDivide",
                                new[] { typeof(decimal), typeof(decimal),
                                        typeof(Runtime.ArithmeticStatus).MakeByRefType() })!)));
                        break;
                    case IR.IrArithmeticOp.Remainder:
                        EmitLoadArithmeticStatusRef(il);
                        il.Append(il.Create(OpCodes.Call,
                            _ctx.Module.ImportReference(typeof(Runtime.PicRuntime).GetMethod("SafeRemainder",
                                new[] { typeof(decimal), typeof(decimal),
                                        typeof(Runtime.ArithmeticStatus).MakeByRefType() })!)));
                        break;
                    case IR.IrArithmeticOp.Power:
                    {
                        var tempRight = new VariableDefinition(_ctx.Module.ImportReference(typeof(decimal)));
                        il.Body.Variables.Add(tempRight);
                        il.Append(il.Create(OpCodes.Stloc, tempRight));
                        il.Append(il.Create(OpCodes.Call,
                            _ctx.Module.ImportReference(typeof(decimal).GetMethod("ToDouble",
                                new[] { typeof(decimal) })!)));
                        il.Append(il.Create(OpCodes.Ldloc, tempRight));
                        il.Append(il.Create(OpCodes.Call,
                            _ctx.Module.ImportReference(typeof(decimal).GetMethod("ToDouble",
                                new[] { typeof(decimal) })!)));
                        il.Append(il.Create(OpCodes.Call,
                            _ctx.Module.ImportReference(typeof(Math).GetMethod("Pow",
                                new[] { typeof(double), typeof(double) })!)));
                        il.Append(il.Create(OpCodes.Newobj,
                            _ctx.Module.ImportReference(typeof(decimal).GetConstructor(
                                new[] { typeof(double) })!)));
                        break;
                    }
                }
                break;

            case IR.IrUnaryExpr unary when unary.Op == IR.IrUnaryOp.Negate:
                EmitIrExpression(il, unary.Operand);
                il.Append(il.Create(OpCodes.Call,
                    _ctx.Module.ImportReference(typeof(decimal).GetMethod("op_UnaryNegation",
                        new[] { typeof(decimal) })!)));
                break;

            case IR.IrIntrinsicCall call:
                EmitIrIntrinsicCall(il, call);
                il.Append(il.Create(OpCodes.Unbox_Any,
                    _ctx.Module.ImportReference(typeof(decimal))));
                break;

            default:
                EmitLoadDecimal(il, 0m);
                break;
        }
    }

    /// <summary>
    /// Emit an IR-native intrinsic function call, leaving an object on the IL stack.
    /// </summary>
    internal void EmitIrIntrinsicCall(ILProcessor il, IR.IrIntrinsicCall call)
    {
        // Push function name first (matches Call(string, object[]) signature)
        il.Append(il.Create(OpCodes.Ldstr, call.FunctionName.ToUpperInvariant()));

        // Build object[] args array
        il.Append(il.Create(OpCodes.Ldc_I4, call.Arguments.Count));
        il.Append(il.Create(OpCodes.Newarr, _ctx.Module.ImportReference(typeof(object))));

        for (int i = 0; i < call.Arguments.Count; i++)
        {
            il.Append(il.Create(OpCodes.Dup));
            il.Append(il.Create(OpCodes.Ldc_I4, i));

            switch (call.Arguments[i])
            {
                case IR.IrLiteralStringArg strArg:
                    il.Append(il.Create(OpCodes.Ldstr, strArg.Value));
                    break;

                case IR.IrAlphanumericArg alphaArg:
                    _ctx.Location.EmitLocationArgs(il, alphaArg.Source);
                    il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
                        typeof(Runtime.StorageHelpers).GetMethod("ReadFieldAsString",
                            new[] { typeof(byte[]), typeof(int), typeof(int) })!)));
                    break;

                case IR.IrNumericArg numArg:
                    EmitIrExpression(il, numArg.Expression);
                    il.Append(il.Create(OpCodes.Box, _ctx.Module.ImportReference(typeof(decimal))));
                    break;
            }

            il.Append(il.Create(OpCodes.Stelem_Ref));
        }

        // Call IntrinsicFunctions.Call(string, object[])
        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
            typeof(Runtime.Intrinsics.IntrinsicFunctions).GetMethod("Call",
                new[] { typeof(string), typeof(object[]) })!)));
    }

    /// <summary>
    /// Emit CIL for IrFunctionCall: call IntrinsicFunctions.Call() and store result into destination.
    /// Dispatches to numeric (MoveAccumulatedToField) or string (MoveAlphanumericToField) path
    /// based on the function's result category.
    /// </summary>
    internal void EmitFunctionCall(ILProcessor il, IR.IrFunctionCall funcCall)
    {
        // Determine if this function returns a string result
        bool isStringFunction = funcCall.FunctionName.ToUpperInvariant() switch
        {
            "LOWER-CASE" or "UPPER-CASE" or "REVERSE" or "TRIM" or "CONCATENATE"
                or "SUBSTITUTE" or "CHAR" or "CURRENT-DATE" or "WHEN-COMPILED" => true,
            _ => false
        };

        var irCall = new IR.IrIntrinsicCall(funcCall.FunctionName, funcCall.Arguments);
        if (isStringFunction)
        {
            // String-returning function: push dest args first, then call function, store to field.
            // Stack order: area, offset, length, stringValue → MoveStringToField
            _ctx.Location.EmitLocationArgs(il, funcCall.Destination);
            EmitIrIntrinsicCall(il, irCall);
            // Result is object on stack; cast to string
            il.Append(il.Create(OpCodes.Castclass,
                _ctx.Module.ImportReference(typeof(string))));
            il.Append(il.Create(OpCodes.Call,
                _ctx.Module.ImportReference(
                    typeof(Runtime.StorageHelpers).GetMethod("MoveStringToField",
                        new[] { typeof(byte[]), typeof(int), typeof(int),
                                typeof(string) })!)));
        }
        else
        {
            // Numeric-returning function: call, unbox to decimal, store via MoveAccumulatedToField
            _ctx.Location.EmitLocationArgsWithPic(il, funcCall.Destination);
            EmitIrIntrinsicCall(il, irCall);
            il.Append(il.Create(OpCodes.Unbox_Any,
                _ctx.Module.ImportReference(typeof(decimal))));
            il.Append(il.Create(OpCodes.Ldc_I4_0)); // no rounding
            EmitLoadArithmeticStatusRef(il);
            il.Append(il.Create(OpCodes.Call,
                _ctx.Module.ImportReference(
                    typeof(Runtime.PicRuntime).GetMethod("MoveAccumulatedToField",
                        new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                                typeof(decimal), typeof(int),
                                typeof(Runtime.ArithmeticStatus).MakeByRefType() })!)));
        }
    }

    /// <summary>
    /// Emit a decimal literal with exact precision (no double round-trip).
    /// Uses decimal(lo, mid, hi, isNegative, scale) constructor.
    /// </summary>
    internal void EmitLoadDecimal(ILProcessor il, decimal value)
    {
        var bits = decimal.GetBits(value);
        il.Append(il.Create(OpCodes.Ldc_I4, bits[0]));  // lo
        il.Append(il.Create(OpCodes.Ldc_I4, bits[1]));  // mid
        il.Append(il.Create(OpCodes.Ldc_I4, bits[2]));  // hi
        il.Append(il.Create(value < 0 ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));  // isNegative
        // Scale is in bits 16-23 of bits[3]
        byte scale = (byte)((bits[3] >> 16) & 0xFF);
        il.Append(il.Create(OpCodes.Ldc_I4, (int)scale));  // scale

        var decCtor = _ctx.Module.ImportReference(
            typeof(decimal).GetConstructor(new[] {
                typeof(int), typeof(int), typeof(int), typeof(bool), typeof(byte) })!);
        il.Append(il.Create(OpCodes.Newobj, decCtor));
    }

    /// <summary>
    /// Construct a PicDescriptor on the CIL stack.
    /// </summary>
    internal void EmitLoadPicDescriptor(ILProcessor il, Runtime.PicDescriptor pic,
        bool suppressBlankWhenZero = false)
    {
        // Must match the CIL-emitted constructor parameter order in PicDescriptor
        il.Append(il.Create(OpCodes.Ldc_I4, pic.TotalDigits));
        il.Append(il.Create(OpCodes.Ldc_I4, pic.FractionDigits));
        il.Append(il.Create(pic.IsSigned ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
        il.Append(il.Create(pic.IsNumeric ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
        il.Append(il.Create(pic.IsAlphanumeric ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
        il.Append(il.Create(pic.HasEditing ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Ldc_I4, pic.StorageLength));
        il.Append(il.Create(OpCodes.Ldc_I4, (int)pic.Usage));
        il.Append(il.Create(OpCodes.Ldc_I4, (int)pic.Category));
        il.Append(il.Create(OpCodes.Ldc_I4, (int)pic.SignStorage));
        il.Append(il.Create(OpCodes.Ldc_I4, (int)pic.Editing));
        bool emitBlank = pic.BlankWhenZero && !suppressBlankWhenZero;
        il.Append(il.Create(emitBlank ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Ldc_I4, pic.LeadingScaleDigits));
        il.Append(il.Create(OpCodes.Ldc_I4, pic.TrailingScaleDigits));

        if (pic.EditPattern != null)
            il.Append(il.Create(OpCodes.Ldstr, pic.EditPattern));
        else
            il.Append(il.Create(OpCodes.Ldnull));

        il.Append(il.Create(pic.IsJustifiedRight ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));

        // Emit PicEnvironment: new PicEnvironment(currencySign, decimalPointIsComma)
        var env = pic.Environment;
        il.Append(il.Create(OpCodes.Ldc_I4, (int)env.CurrencySign));
        il.Append(il.Create(env.DecimalPointIsComma ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
        var envCtor = _ctx.Module.ImportReference(
            typeof(Runtime.PicEnvironment).GetConstructor(
                new[] { typeof(char), typeof(bool) })!);
        il.Append(il.Create(OpCodes.Newobj, envCtor));

        var ctor = _ctx.Module.ImportReference(
            typeof(Runtime.PicDescriptor).GetConstructor(
                new[] { typeof(int), typeof(int), typeof(bool), typeof(bool),
                        typeof(bool), typeof(bool), typeof(int), typeof(Runtime.UsageKind),
                        typeof(Runtime.CobolCategory),
                        typeof(Runtime.SignStorageKind), typeof(Runtime.EditingKind),
                        typeof(bool), typeof(int), typeof(int), typeof(string),
                        typeof(bool), typeof(Runtime.PicEnvironment) })!);
        il.Append(il.Create(OpCodes.Newobj, ctor));
    }

    /// <summary>
    /// Emit a byte[] literal onto the CIL stack. Creates a new array and fills it.
    /// </summary>
    internal void EmitByteArrayLiteral(ILProcessor il, byte[] data)
    {
        il.Append(il.Create(OpCodes.Ldc_I4, data.Length));
        il.Append(il.Create(OpCodes.Newarr, _ctx.Module.ImportReference(typeof(byte))));
        for (int i = 0; i < data.Length; i++)
        {
            il.Append(il.Create(OpCodes.Dup));
            il.Append(il.Create(OpCodes.Ldc_I4, i));
            il.Append(il.Create(OpCodes.Ldc_I4, (int)data[i]));
            il.Append(il.Create(OpCodes.Stelem_I1));
        }
    }

    // ── Private helper: forward to CilEmitter's arithmetic status (stays there until Stage 3) ──

    private void EmitLoadArithmeticStatusRef(ILProcessor il)
    {
        // ArithmeticStatus management stays in CilEmitter until Stage 3.
        // Access via _ctx fields directly (same logic as CilEmitter.EmitLoadArithmeticStatusRef).
        if (_ctx.ArithmeticStatusLocal == null)
        {
            _ctx.ArithmeticStatusLocal = new VariableDefinition(
                _ctx.Module.ImportReference(typeof(ArithmeticStatus)));
            _ctx.CurrentMethodDef!.Body.Variables.Add(_ctx.ArithmeticStatusLocal);
        }
        il.Append(il.Create(OpCodes.Ldloca, _ctx.ArithmeticStatusLocal));
    }
}
