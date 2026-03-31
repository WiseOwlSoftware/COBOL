// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Mono.Cecil;
using Mono.Cecil.Cil;
using CobolSharp.Compiler.IR;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.CodeGen.Emission;

/// <summary>
/// Data movement emission: EmitLoadConst, EmitLoadField, EmitStoreField, EmitMove,
/// EmitMoveStringToField, EmitMoveWithStandardSignature, EmitMoveFigurative,
/// EmitMoveAllLiteral, EmitMoveFieldToField, EmitPicMoveLiteralNumeric,
/// EmitPicDisplay, EmitDisplayOperand, EmitAccept, EmitLocationLength,
/// EmitDefaultPicDescriptor, GetCobolDataPointerCtor, EmitOptionalString.
/// </summary>
internal sealed class CilDataEmitter
{
    private readonly EmissionContext _ctx;

    internal CilDataEmitter(EmissionContext ctx) => _ctx = ctx;

    internal void EmitLoadConst(ILProcessor il, IrLoadConst lc,
        Func<IrValue, VariableDefinition> getLocal)
    {
        // Push constant onto stack — no stloc.
        // Consumer (next instruction) reads from stack directly.
        switch (lc.Value)
        {
            case int i:
                il.Append(il.Create(OpCodes.Ldc_I4, i));
                break;
            case long l:
                il.Append(il.Create(OpCodes.Ldc_I8, l));
                break;
            case string s:
                il.Append(il.Create(OpCodes.Ldstr, s));
                break;
            case bool b:
                il.Append(il.Create(b ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
                break;
            default:
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                break;
        }
        // No stloc — value stays on stack for the next instruction to consume
    }

    internal void EmitLoadField(ILProcessor il, IrLoadField lf,
        Func<IrValue, VariableDefinition> getLocal)
    {
        var fieldRef = _ctx.FieldMap[lf.Field];
        il.Append(il.Create(OpCodes.Ldsfld, fieldRef));

        if (lf.Result is { } res)
        {
            var local = getLocal(res);
            il.Append(il.Create(OpCodes.Stloc, local));
        }
    }

    internal void EmitStoreField(ILProcessor il, IrStoreField sf,
        Func<IrValue, VariableDefinition> getLocal)
    {
        var fieldRef = _ctx.FieldMap[sf.Field];
        var valueLocal = getLocal(sf.Value);

        il.Append(il.Create(OpCodes.Ldloc, valueLocal));
        il.Append(il.Create(OpCodes.Stsfld, fieldRef));
    }

    internal void EmitMove(ILProcessor il, IrMove mv,
        Func<IrValue, VariableDefinition> getLocal)
    {
        var srcLocal = getLocal(mv.Source);
        var dstLocal = getLocal(mv.Target);

        il.Append(il.Create(OpCodes.Ldloc, srcLocal));
        il.Append(il.Create(OpCodes.Stloc, dstLocal));
    }

    /// <summary>
    /// MOVE "literal" TO field:
    /// IL: ldsfld State → ldfld WorkingStorage → ldc.i4 offset → ldc.i4 size → ldstr value → call MoveStringToField
    /// </summary>
    internal void EmitMoveStringToField(ILProcessor il, IrMoveStringToField ms,
        Func<IrValue, VariableDefinition> getLocal)
    {
        var pic = ms.Target.GetPic();

        // Numeric targets: right-justified numeric MOVE (rightmost digits taken)
        if (pic.Category == CobolCategory.Numeric)
        {
            _ctx.Location.EmitLocationArgsWithPic(il, ms.Target);
            il.Append(il.Create(OpCodes.Ldstr, ms.Value));

            var method = _ctx.Module.ImportReference(
                typeof(PicRuntime).GetMethod(
                    "MoveStringLiteralToNumeric",
                    new[] { typeof(byte[]), typeof(int), typeof(int),
                            typeof(PicDescriptor), typeof(string) })!);
            il.Append(il.Create(OpCodes.Call, method));
        }
        // Alphanumeric-edited targets: apply edit pattern (B→space, 0→zero, etc.)
        else if (pic.Category == CobolCategory.AlphanumericEdited && pic.EditPattern != null)
        {
            _ctx.Location.EmitLocationArgs(il, ms.Target);
            il.Append(il.Create(OpCodes.Ldstr, ms.Value));
            il.Append(il.Create(OpCodes.Ldstr, pic.EditPattern));

            var method = _ctx.Module.ImportReference(
                typeof(StorageHelpers).GetMethod(
                    "MoveStringToEditedField",
                    new[] { typeof(byte[]), typeof(int), typeof(int),
                            typeof(string), typeof(string) })!);
            il.Append(il.Create(OpCodes.Call, method));
        }
        else if (pic.IsJustifiedRight)
        {
            // JUSTIFIED RIGHT alphanumeric: right-justified, left-padded/left-truncated
            _ctx.Location.EmitLocationArgs(il, ms.Target);
            il.Append(il.Create(OpCodes.Ldstr, ms.Value));

            var method = _ctx.Module.ImportReference(
                typeof(StorageHelpers).GetMethod(
                    "MoveStringToJustifiedField",
                    new[] { typeof(byte[]), typeof(int), typeof(int), typeof(string) })!);
            il.Append(il.Create(OpCodes.Call, method));
        }
        else
        {
            // Plain alphanumeric: left-justified, space-padded
            _ctx.Location.EmitLocationArgs(il, ms.Target);
            il.Append(il.Create(OpCodes.Ldstr, ms.Value));

            var method = _ctx.Module.ImportReference(
                typeof(StorageHelpers).GetMethod(
                    "MoveStringToField",
                    new[] { typeof(byte[]), typeof(int), typeof(int), typeof(string) })!);
            il.Append(il.Create(OpCodes.Call, method));
        }
    }

    /// <summary>
    /// Emit a MOVE call with the standard (src, dst, rounding) signature used by most PicRuntime MOVE methods.
    /// </summary>
    internal void EmitMoveWithStandardSignature(
        ILProcessor il, IrLocation source, IrLocation destination, int rounding, string methodName)
    {
        _ctx.Location.EmitLocationArgsWithPic(il, source);
        _ctx.Location.EmitLocationArgsWithPic(il, destination);

        il.Append(il.Create(OpCodes.Ldc_I4, rounding));

        var method = _ctx.Module.ImportReference(
            typeof(PicRuntime).GetMethod(methodName,
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor),
                        typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor),
                        typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    internal void EmitMoveFigurative(ILProcessor il, IrMoveFigurative mf)
    {
        _ctx.Location.EmitLocationArgsWithPic(il, mf.Destination);
        il.Append(il.Create(OpCodes.Ldc_I4, (int)mf.FigurativeKind));

        var method = _ctx.Module.ImportReference(
            typeof(PicRuntime).GetMethod(
                "MoveFigurativeToField",
                new[] { typeof(byte[]), typeof(int), typeof(int),
                        typeof(PicDescriptor), typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    /// <summary>
    /// MOVE ALL "pattern" TO field: calls PicRuntime.MoveAllLiteralToField.
    /// </summary>
    internal void EmitMoveAllLiteral(ILProcessor il, IrMoveAllLiteral mal)
    {
        _ctx.Location.EmitLocationArgs(il, mal.Destination);

        // Emit pattern as byte[]: new byte[] { b0, b1, ... }
        var patternBytes = System.Text.Encoding.ASCII.GetBytes(mal.Pattern);
        il.Append(il.Create(OpCodes.Ldc_I4, patternBytes.Length));
        il.Append(il.Create(OpCodes.Newarr, _ctx.Module.TypeSystem.Byte));
        for (int i = 0; i < patternBytes.Length; i++)
        {
            il.Append(il.Create(OpCodes.Dup));
            il.Append(il.Create(OpCodes.Ldc_I4, i));
            il.Append(il.Create(OpCodes.Ldc_I4, (int)patternBytes[i]));
            il.Append(il.Create(OpCodes.Stelem_I1));
        }

        var method = _ctx.Module.ImportReference(
            typeof(PicRuntime).GetMethod(
                "MoveAllLiteralToField",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(byte[]) })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    /// <summary>
    /// MOVE field TO field: routes numeric→numeric through PicRuntime.MoveNumeric,
    /// alpha→alpha through StorageHelpers.MoveFieldToField.
    /// </summary>
    internal void EmitMoveFieldToField(ILProcessor il, IrMoveFieldToField mf)
    {
        var srcPic = mf.SourcePic;
        var dstPic = mf.DestinationPic;
        var srcCat = srcPic.Category;
        var dstCat = dstPic.Category;
        int rounding = mf.IsRounded ? 1 : 0;

        // Group items are always alphanumeric for MOVE: raw byte copy, no formatting/editing.
        if (srcPic.IsGroup || dstPic.IsGroup)
        {
            EmitMoveWithStandardSignature(il, mf.Source, mf.Destination, rounding, "MoveAlphanumericToAlphanumeric");
            return;
        }

        // Destination AlphanumericEdited: must be checked before generic IsNumericLike() rules.
        if (dstCat == CobolCategory.AlphanumericEdited)
        {
            if (srcCat == CobolCategory.Numeric)
                EmitMoveWithStandardSignature(il, mf.Source, mf.Destination, rounding, "MoveNumericToAlphanumericEdited");
            else
                EmitMoveWithStandardSignature(il, mf.Source, mf.Destination, rounding, "MoveAlphanumericToAlphanumericEdited");
            return;
        }
        // NumericEdited source: specific handling before generic IsNumericLike() rules.
        else if (srcCat == CobolCategory.NumericEdited && dstCat == CobolCategory.NumericEdited)
        {
            EmitMoveWithStandardSignature(il, mf.Source, mf.Destination, rounding, "MoveNumericEditedToNumericEdited");
        }
        else if (srcCat == CobolCategory.NumericEdited && dstCat == CobolCategory.Numeric)
        {
            EmitMoveWithStandardSignature(il, mf.Source, mf.Destination, rounding, "MoveNumericEditedToNumeric");
        }
        else if (srcCat == CobolCategory.NumericEdited && dstCat.IsAlphanumericLike())
        {
            // NumericEdited → Alphanumeric: COBOL treats source as alphanumeric (raw byte copy)
            _ctx.Location.EmitLocationArgs(il, mf.Destination);
            _ctx.Location.EmitLocationArgs(il, mf.Source);
            var method = _ctx.Module.ImportReference(
                typeof(StorageHelpers).GetMethod(
                    "MoveFieldToField",
                    new[] { typeof(byte[]), typeof(int), typeof(int),
                            typeof(byte[]), typeof(int), typeof(int) })!);
            il.Append(il.Create(OpCodes.Call, method));
            return;
        }
        // Generic numeric source rules.
        else if (srcCat.IsNumericLike() && dstCat == CobolCategory.NumericEdited)
        {
            EmitMoveWithStandardSignature(il, mf.Source, mf.Destination, rounding, "MoveNumericToNumericEdited");
        }
        else if (srcCat.IsNumericLike() && dstCat.IsNumericLike())
        {
            EmitMoveWithStandardSignature(il, mf.Source, mf.Destination, rounding, "MoveNumericToNumeric");
        }
        else if (srcCat.IsNumericLike() && dstCat.IsAlphanumericLike())
        {
            EmitMoveWithStandardSignature(il, mf.Source, mf.Destination, rounding, "MoveNumericToAlphanumeric");
        }
        // Alphanumeric source rules.
        else if (srcCat.IsAlphanumericLike() && dstCat == CobolCategory.Numeric)
        {
            EmitMoveWithStandardSignature(il, mf.Source, mf.Destination, rounding, "MoveAlphanumericToNumeric");
        }
        else if (srcCat.IsAlphanumericLike() && dstCat == CobolCategory.NumericEdited)
        {
            EmitMoveWithStandardSignature(il, mf.Source, mf.Destination, rounding, "MoveAlphanumericToNumericEdited");
        }
        else
        {
            // Alphanumeric MOVE: left-justified, space-padded (handles JUSTIFIED RIGHT)
            EmitMoveWithStandardSignature(il, mf.Source, mf.Destination, rounding, "MoveAlphanumericToAlphanumeric");
        }
    }

    internal void EmitPicMoveLiteralNumeric(ILProcessor il, IrPicMoveLiteralNumeric mv)
    {
        _ctx.Location.EmitLocationArgsWithPic(il, mv.Destination);
        _ctx.Expression.EmitLoadDecimal(il, mv.Value);
        il.Append(il.Create(OpCodes.Ldc_I4, mv.Rounding));

        var method = _ctx.Module.ImportReference(
            typeof(PicRuntime).GetMethod("MoveNumericLiteral",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor),
                        typeof(decimal), typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    internal void EmitPicDisplay(ILProcessor il, IrPicDisplay disp)
    {
        // Strategy: push each operand as a string, then concat and call Console.WriteLine.
        // For a single operand, just push it directly. For multiple, use String.Concat.
        if (disp.Operands.Count == 0)
        {
            // DISPLAY with no operands: just output empty line
            il.Append(il.Create(OpCodes.Ldstr, ""));
        }
        else if (disp.Operands.Count == 1)
        {
            EmitDisplayOperand(il, disp.Operands[0]);
        }
        else
        {
            // Create a string array, populate it, then call String.Concat(string[])
            il.Append(il.Create(OpCodes.Ldc_I4, disp.Operands.Count));
            il.Append(il.Create(OpCodes.Newarr, _ctx.Module.ImportReference(typeof(string))));

            for (int i = 0; i < disp.Operands.Count; i++)
            {
                il.Append(il.Create(OpCodes.Dup)); // keep array ref
                il.Append(il.Create(OpCodes.Ldc_I4, i));
                EmitDisplayOperand(il, disp.Operands[i]);
                il.Append(il.Create(OpCodes.Stelem_Ref));
            }

            var concat = _ctx.Module.ImportReference(
                typeof(string).GetMethod("Concat", new[] { typeof(string[]) })!);
            il.Append(il.Create(OpCodes.Call, concat));
        }

        var consoleWriteLine = _ctx.Module.ImportReference(
            typeof(Console).GetMethod("WriteLine", new[] { typeof(string) })!);
        il.Append(il.Create(OpCodes.Call, consoleWriteLine));
    }

    internal void EmitDisplayOperand(ILProcessor il, DisplayOperand operand)
    {
        if (operand is DisplayLiteralOperand lit)
        {
            il.Append(il.Create(OpCodes.Ldstr, lit.Value));
        }
        else if (operand is DisplayFieldOperand field)
        {
            // Call PicRuntime.GetDisplayString(byte[] area, int offset, int length, PicDescriptor pic)
            _ctx.Location.EmitLocationArgsWithPic(il, field.Location);

            var method = _ctx.Module.ImportReference(
                typeof(PicRuntime).GetMethod("GetDisplayString",
                    new[] { typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor) })!);
            il.Append(il.Create(OpCodes.Call, method));
        }
    }

    internal void EmitAccept(ILProcessor il, IrAccept acc)
    {
        _ctx.Location.EmitLocationArgs(il, acc.Target);
        il.Append(il.Create(OpCodes.Ldc_I4, (int)acc.Source));

        var method = _ctx.Module.ImportReference(
            typeof(AcceptRuntime).GetMethod("Accept",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(AcceptSourceKind) })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    internal void EmitLocationLength(ILProcessor il, IrLocation loc)
    {
        if (loc is IrStaticLocation sl)
            il.Append(il.Create(OpCodes.Ldc_I4, sl.Location.Length));
        else
            il.Append(il.Create(OpCodes.Ldc_I4_0)); // fallback
    }

    internal void EmitDefaultPicDescriptor(ILProcessor il)
    {
        // Push a default PicDescriptor (alphanumeric, for parameter passing)
        // This is a simplified version — the actual PicDescriptor comes from
        // the caller's StorageLocation.Pic in a full implementation
        var defaultPicCtor = _ctx.Module.ImportReference(
            typeof(PicDescriptor).GetConstructors()
                .First(c => c.GetParameters().Length > 10));
        // Push all constructor args for a basic alphanumeric descriptor
        il.Append(il.Create(OpCodes.Ldc_I4_0)); // totalDigits
        il.Append(il.Create(OpCodes.Ldc_I4_0)); // fractionDigits
        il.Append(il.Create(OpCodes.Ldc_I4_0)); // isSigned
        il.Append(il.Create(OpCodes.Ldc_I4_0)); // isNumeric
        il.Append(il.Create(OpCodes.Ldc_I4_1)); // isAlphanumeric
        il.Append(il.Create(OpCodes.Ldc_I4_0)); // hasEditing
        il.Append(il.Create(OpCodes.Ldc_I4_0)); // storageLength (will be set from Length)
        il.Append(il.Create(OpCodes.Ldc_I4_0)); // usage = Display
        il.Append(il.Create(OpCodes.Ldc_I4_1)); // category = Alphanumeric
        il.Append(il.Create(OpCodes.Ldc_I4_0)); // signStorage
        il.Append(il.Create(OpCodes.Ldc_I4_0)); // editing
        il.Append(il.Create(OpCodes.Ldc_I4_0)); // blankWhenZero
        il.Append(il.Create(OpCodes.Ldc_I4_0)); // leadingScaleDigits
        il.Append(il.Create(OpCodes.Ldc_I4_0)); // trailingScaleDigits
        il.Append(il.Create(OpCodes.Ldnull));    // editPattern
        il.Append(il.Create(OpCodes.Ldc_I4_0)); // isGroup
        il.Append(il.Create(OpCodes.Ldc_I4, 36)); // currencySign '$'
        il.Append(il.Create(OpCodes.Ldc_I4_0)); // decimalPointIsComma
        var picEnvCtor = _ctx.Module.ImportReference(
            typeof(PicEnvironment).GetConstructor(new[] { typeof(char), typeof(bool) })!);
        il.Append(il.Create(OpCodes.Newobj, picEnvCtor));
        il.Append(il.Create(OpCodes.Newobj, defaultPicCtor));
    }

    internal MethodReference GetCobolDataPointerCtor()
    {
        _ctx.CobolDataPointerCtor ??= _ctx.Module.ImportReference(
            typeof(CobolDataPointer).GetConstructor(
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor) })!);
        return _ctx.CobolDataPointerCtor;
    }

    internal void EmitOptionalString(ILProcessor il, string? value)
    {
        if (value != null)
            il.Append(il.Create(OpCodes.Ldstr, value));
        else
            il.Append(il.Create(OpCodes.Ldnull));
    }
}
