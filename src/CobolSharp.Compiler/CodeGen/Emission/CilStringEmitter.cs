// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Mono.Cecil;
using Mono.Cecil.Cil;
using CobolSharp.Compiler.IR;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.CodeGen.Emission;

/// <summary>
/// String operation emission: EmitStringStatement, EmitUnstringStatement,
/// EmitInspectTally, EmitInspectReplace, EmitInspectConvert,
/// EmitIrInspectPatternValue, EmitIrInspectPatternValueAsOptionalString.
/// </summary>
internal sealed class CilStringEmitter
{
    private readonly EmissionContext _ctx;

    internal CilStringEmitter(EmissionContext ctx) => _ctx = ctx;

    internal void EmitStringStatement(ILProcessor il, IR.IrStringStatement strStmt,
        Func<IR.IrValue, VariableDefinition> getLocal)
    {
        // Create a shared pointer local for the entire STRING statement
        var ptrLocal = new VariableDefinition(_ctx.Module.TypeSystem.Int32);
        _ctx.CurrentMethodDef!.Body.Variables.Add(ptrLocal);

        // Initialize pointer: from user POINTER variable or 1
        if (strStmt.PointerLocation != null)
        {
            _ctx.Location.EmitLocationArgsWithPic(il, strStmt.PointerLocation);
            il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
                typeof(Runtime.PicRuntime).GetMethod("DecodeNumeric",
                    new[] { typeof(byte[]), typeof(int), typeof(int),
                            typeof(Runtime.PicDescriptor) })!)));
            il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
                typeof(Convert).GetMethod("ToInt32", new[] { typeof(decimal) })!)));
        }
        else
        {
            il.Append(il.Create(OpCodes.Ldc_I4_1));
        }
        il.Append(il.Create(OpCodes.Stloc, ptrLocal));

        // Initialize overflow result to false
        var overflowLocal = getLocal(strStmt.Result!.Value);
        il.Append(il.Create(OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Stloc, overflowLocal));

        // Emit each sending
        foreach (var sending in strStmt.Sendings)
        {
            // Push dest args
            _ctx.Location.EmitLocationArgs(il, strStmt.Destination);

            if (sending.LiteralValue != null)
            {
                // Literal sending: StringConcatLiteral(dest area/off/len, value, delim, bySize, ref ptr)
                il.Append(il.Create(OpCodes.Ldstr, sending.LiteralValue));
            }
            else
            {
                // Field sending: StringConcat(dest area/off/len, src area/off/len, delim, bySize, ref ptr)
                _ctx.Location.EmitLocationArgs(il, sending.SourceLocation!);
            }

            // Delimiter: field-based or literal string
            bool hasFieldDelim = sending.DelimiterLocation != null;
            if (hasFieldDelim)
                _ctx.Location.EmitLocationArgs(il, sending.DelimiterLocation!);
            else if (sending.Delimiter != null)
                il.Append(il.Create(OpCodes.Ldstr, sending.Delimiter));
            else
                il.Append(il.Create(OpCodes.Ldnull));

            // DelimitedBySize
            il.Append(il.Create(sending.DelimitedBySize ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));

            // Pass pointer by ref
            il.Append(il.Create(OpCodes.Ldloca, ptrLocal));

            // Call appropriate runtime method
            if (sending.LiteralValue != null && hasFieldDelim)
            {
                il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
                    typeof(Runtime.StorageHelpers).GetMethod("StringConcatLiteralFieldDelim",
                        new[] { typeof(byte[]), typeof(int), typeof(int), typeof(string),
                                typeof(byte[]), typeof(int), typeof(int),
                                typeof(bool), typeof(int).MakeByRefType() })!)));
            }
            else if (sending.LiteralValue != null)
            {
                il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
                    typeof(Runtime.StorageHelpers).GetMethod("StringConcatLiteral",
                        new[] { typeof(byte[]), typeof(int), typeof(int), typeof(string),
                                typeof(string), typeof(bool), typeof(int).MakeByRefType() })!)));
            }
            else if (hasFieldDelim)
            {
                il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
                    typeof(Runtime.StorageHelpers).GetMethod("StringConcatFieldDelim",
                        new[] { typeof(byte[]), typeof(int), typeof(int),
                                typeof(byte[]), typeof(int), typeof(int),
                                typeof(byte[]), typeof(int), typeof(int),
                                typeof(bool), typeof(int).MakeByRefType() })!)));
            }
            else
            {
                il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
                    typeof(Runtime.StorageHelpers).GetMethod("StringConcat",
                        new[] { typeof(byte[]), typeof(int), typeof(int),
                                typeof(byte[]), typeof(int), typeof(int),
                                typeof(string), typeof(bool), typeof(int).MakeByRefType() })!)));
            }

            // OR overflow: overflowLocal |= result
            il.Append(il.Create(OpCodes.Ldloc, overflowLocal));
            il.Append(il.Create(OpCodes.Or));
            il.Append(il.Create(OpCodes.Stloc, overflowLocal));
        }

        // Write pointer back to POINTER variable (if present)
        if (strStmt.PointerLocation != null)
        {
            _ctx.Location.EmitLocationArgsWithPic(il, strStmt.PointerLocation);
            il.Append(il.Create(OpCodes.Ldloc, ptrLocal));
            il.Append(il.Create(OpCodes.Newobj,
                _ctx.Module.ImportReference(typeof(decimal).GetConstructor(new[] { typeof(int) })!)));
            il.Append(il.Create(OpCodes.Ldc_I4_0));
            il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
                typeof(Runtime.PicRuntime).GetMethod("MoveNumericLiteral",
                    new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                            typeof(decimal), typeof(int) })!)));
        }
    }

    internal void EmitUnstringStatement(ILProcessor il, IR.IrUnstringStatement unstrStmt,
        Func<IR.IrValue, VariableDefinition> getLocal)
    {
        // Create shared pointer local for the entire UNSTRING statement
        var ptrLocal = new VariableDefinition(_ctx.Module.TypeSystem.Int32);
        _ctx.CurrentMethodDef!.Body.Variables.Add(ptrLocal);

        // Initialize pointer: from user POINTER variable or 1
        if (unstrStmt.PointerLocation != null)
        {
            _ctx.Location.EmitLocationArgsWithPic(il, unstrStmt.PointerLocation);
            il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
                typeof(Runtime.PicRuntime).GetMethod("DecodeNumeric",
                    new[] { typeof(byte[]), typeof(int), typeof(int),
                            typeof(Runtime.PicDescriptor) })!)));
            il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
                typeof(Convert).GetMethod("ToInt32", new[] { typeof(decimal) })!)));
        }
        else
        {
            il.Append(il.Create(OpCodes.Ldc_I4_1));
        }
        il.Append(il.Create(OpCodes.Stloc, ptrLocal));

        // Create shared overflow local
        var overflowLocal = new VariableDefinition(_ctx.Module.TypeSystem.Boolean);
        _ctx.CurrentMethodDef.Body.Variables.Add(overflowLocal);
        il.Append(il.Create(OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Stloc, overflowLocal));

        // Tally counter local — initialize from existing TALLYING field value (not zero)
        // Per ISO §14.9.44: "the value of identifier-7 is incremented by 1"
        var tallyLocal = new VariableDefinition(_ctx.Module.TypeSystem.Int32);
        _ctx.CurrentMethodDef.Body.Variables.Add(tallyLocal);
        if (unstrStmt.TallyingLocation != null)
        {
            _ctx.Location.EmitLocationArgsWithPic(il, unstrStmt.TallyingLocation);
            il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
                typeof(Runtime.PicRuntime).GetMethod("DecodeNumeric",
                    new[] { typeof(byte[]), typeof(int), typeof(int),
                            typeof(Runtime.PicDescriptor) })!)));
            il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
                typeof(Convert).GetMethod("ToInt32", new[] { typeof(decimal) })!)));
        }
        else
        {
            il.Append(il.Create(OpCodes.Ldc_I4_0));
        }
        il.Append(il.Create(OpCodes.Stloc, tallyLocal));

        // Resolve the UnstringExtract method reference
        var extractMethod = _ctx.Module.ImportReference(
            typeof(Runtime.StorageHelpers).GetMethod("UnstringExtract",
                new[] { typeof(byte[]), typeof(int), typeof(int),
                        typeof(byte[]), typeof(int), typeof(int),
                        typeof(string), typeof(bool),
                        typeof(byte[]), typeof(int), typeof(int),
                        typeof(int).MakeByRefType(), typeof(bool).MakeByRefType() })!);

        // Count local for COUNT IN write-back
        var countLocal = new VariableDefinition(_ctx.Module.TypeSystem.Int32);
        _ctx.CurrentMethodDef.Body.Variables.Add(countLocal);

        // Process each INTO
        foreach (var into in unstrStmt.Intos)
        {
            // Push source args (area, offset, length)
            _ctx.Location.EmitLocationArgs(il, unstrStmt.Source);

            // Push dest args (area, offset, length)
            _ctx.Location.EmitLocationArgs(il, into.Target);

            // Push delimiter (string? or null)
            if (unstrStmt.LiteralDelimiter != null)
                il.Append(il.Create(OpCodes.Ldstr, unstrStmt.LiteralDelimiter));
            else if (unstrStmt.DelimiterLocation != null)
            {
                // Field-based delimiter: read field as string at runtime
                _ctx.Location.EmitLocationArgs(il, unstrStmt.DelimiterLocation);
                il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
                    typeof(Runtime.StorageHelpers).GetMethod("ReadFieldAsString",
                        new[] { typeof(byte[]), typeof(int), typeof(int) })!)));
            }
            else
                il.Append(il.Create(OpCodes.Ldnull));

            // Push delimitedByAll flag
            il.Append(il.Create(unstrStmt.DelimitedByAll ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));

            // Push DELIMITER IN args (area, offset, length) or nulls
            if (into.DelimiterIn != null)
            {
                _ctx.Location.EmitLocationArgs(il, into.DelimiterIn);
            }
            else
            {
                il.Append(il.Create(OpCodes.Ldnull));
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Ldc_I4_0));
            }

            // Pass pointer by ref
            il.Append(il.Create(OpCodes.Ldloca, ptrLocal));

            // Pass overflow by ref
            il.Append(il.Create(OpCodes.Ldloca, overflowLocal));

            // Call UnstringExtract — returns int (count of extracted chars)
            il.Append(il.Create(OpCodes.Call, extractMethod));

            // Store returned count
            il.Append(il.Create(OpCodes.Stloc, countLocal));

            // Handle COUNT IN: write the count to the COUNT IN field
            if (into.CountIn != null)
            {
                _ctx.Location.EmitLocationArgsWithPic(il, into.CountIn);
                il.Append(il.Create(OpCodes.Ldloc, countLocal));
                il.Append(il.Create(OpCodes.Newobj,
                    _ctx.Module.ImportReference(typeof(decimal).GetConstructor(new[] { typeof(int) })!)));
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
                    typeof(Runtime.PicRuntime).GetMethod("MoveNumericLiteral",
                        new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                                typeof(decimal), typeof(int) })!)));
            }

            // Increment tally counter — only if overflow hasn't occurred
            // (per spec, tally counts INTO targets that received data)
            var skipTally = il.Create(OpCodes.Nop);
            il.Append(il.Create(OpCodes.Ldloc, overflowLocal));
            il.Append(il.Create(OpCodes.Brtrue, skipTally));
            il.Append(il.Create(OpCodes.Ldloc, tallyLocal));
            il.Append(il.Create(OpCodes.Ldc_I4_1));
            il.Append(il.Create(OpCodes.Add));
            il.Append(il.Create(OpCodes.Stloc, tallyLocal));
            il.Append(skipTally);
        }

        // Post-loop overflow check: if pointer <= srcLength (unexamined chars remain),
        // set overflow. Per ISO §14.9.44: overflow occurs when "all receiving areas
        // have been acted upon" but source is not exhausted.
        // Logic: overflow = existingOverflow OR (pointer <= srcLength)
        {
            var srcLen = unstrStmt.Source.GetPic().StorageLength;
            il.Append(il.Create(OpCodes.Ldloc, overflowLocal));    // existing overflow
            il.Append(il.Create(OpCodes.Ldloc, ptrLocal));         // pointer (1-based)
            il.Append(il.Create(OpCodes.Ldc_I4, srcLen));           // source length
            il.Append(il.Create(OpCodes.Cgt));                      // pointer > srcLength?
            il.Append(il.Create(OpCodes.Ldc_I4_0));
            il.Append(il.Create(OpCodes.Ceq));                      // NOT(pointer > srcLen) = unexamined chars remain
            il.Append(il.Create(OpCodes.Or));                       // overflow OR unexamined
            il.Append(il.Create(OpCodes.Stloc, overflowLocal));
        }

        // Write pointer back to POINTER variable (if present)
        if (unstrStmt.PointerLocation != null)
        {
            _ctx.Location.EmitLocationArgsWithPic(il, unstrStmt.PointerLocation);
            il.Append(il.Create(OpCodes.Ldloc, ptrLocal));
            il.Append(il.Create(OpCodes.Newobj,
                _ctx.Module.ImportReference(typeof(decimal).GetConstructor(new[] { typeof(int) })!)));
            il.Append(il.Create(OpCodes.Ldc_I4_0));
            il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
                typeof(Runtime.PicRuntime).GetMethod("MoveNumericLiteral",
                    new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                            typeof(decimal), typeof(int) })!)));
        }

        // Write tally count to TALLYING variable (if present)
        if (unstrStmt.TallyingLocation != null)
        {
            _ctx.Location.EmitLocationArgsWithPic(il, unstrStmt.TallyingLocation);
            il.Append(il.Create(OpCodes.Ldloc, tallyLocal));
            il.Append(il.Create(OpCodes.Newobj,
                _ctx.Module.ImportReference(typeof(decimal).GetConstructor(new[] { typeof(int) })!)));
            il.Append(il.Create(OpCodes.Ldc_I4_0));
            il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
                typeof(Runtime.PicRuntime).GetMethod("MoveNumericLiteral",
                    new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                            typeof(decimal), typeof(int) })!)));
        }

        // Store overflow result for branching
        var resultLocal = getLocal(unstrStmt.Result!.Value);
        il.Append(il.Create(OpCodes.Ldloc, overflowLocal));
        il.Append(il.Create(OpCodes.Stloc, resultLocal));
    }

    internal void EmitInspectTally(ILProcessor il, IrInspectTally it)
    {
        // Target area/offset/length
        _ctx.Location.EmitLocationArgs(il, it.Target);

        string methodName;

        if (it.Kind == IR.InspectTallyKind.Characters)
        {
            methodName = "TallyCharactersAndStore";
        }
        else
        {
            methodName = it.Kind == IR.InspectTallyKind.Leading
                ? "TallyLeadingAndStore" : "TallyAllAndStore";
            EmitIrInspectPatternValue(il, it.Pattern);
        }

        // Counter area/offset/length/pic
        _ctx.Location.EmitLocationArgsWithPic(il, it.Counter);

        // Region args
        EmitIrInspectPatternValueAsOptionalString(il, it.BeforePattern);
        il.Append(il.Create(it.BeforeInitial ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
        EmitIrInspectPatternValueAsOptionalString(il, it.AfterPattern);
        il.Append(il.Create(it.AfterInitial ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));

        System.Type[] paramTypes;
        if (it.Kind == IR.InspectTallyKind.Characters)
        {
            paramTypes = new[] { typeof(byte[]), typeof(int), typeof(int),
                typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                typeof(string), typeof(bool), typeof(string), typeof(bool) };
        }
        else
        {
            paramTypes = new[] { typeof(byte[]), typeof(int), typeof(int), typeof(string),
                typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                typeof(string), typeof(bool), typeof(string), typeof(bool) };
        }

        var method = _ctx.Module.ImportReference(
            typeof(Runtime.InspectRuntime).GetMethod(methodName, paramTypes)!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    internal void EmitInspectReplace(ILProcessor il, IrInspectReplace ir)
    {
        _ctx.Location.EmitLocationArgs(il, ir.Target);

        if (ir.Kind == IR.InspectReplaceKind.Characters)
        {
            // REPLACING CHARACTERS BY x — no pattern, just replacement
            EmitIrInspectPatternValue(il, ir.Replacement);
            EmitIrInspectPatternValueAsOptionalString(il, ir.BeforePattern);
            il.Append(il.Create(ir.BeforeInitial ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
            EmitIrInspectPatternValueAsOptionalString(il, ir.AfterPattern);
            il.Append(il.Create(ir.AfterInitial ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));

            var charsMethod = _ctx.Module.ImportReference(
                typeof(Runtime.InspectRuntime).GetMethod("ReplaceCharacters",
                    new[] { typeof(byte[]), typeof(int), typeof(int),
                        typeof(string),
                        typeof(string), typeof(bool), typeof(string), typeof(bool) })!);
            il.Append(il.Create(OpCodes.Call, charsMethod));
        }
        else
        {
            string methodName = ir.Kind switch
            {
                IR.InspectReplaceKind.First => "ReplaceFirst",
                IR.InspectReplaceKind.Leading => "ReplaceLeading",
                _ => "ReplaceAll"
            };

            EmitIrInspectPatternValue(il, ir.Pattern);
            EmitIrInspectPatternValue(il, ir.Replacement);
            EmitIrInspectPatternValueAsOptionalString(il, ir.BeforePattern);
            il.Append(il.Create(ir.BeforeInitial ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
            EmitIrInspectPatternValueAsOptionalString(il, ir.AfterPattern);
            il.Append(il.Create(ir.AfterInitial ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));

            var method = _ctx.Module.ImportReference(
                typeof(Runtime.InspectRuntime).GetMethod(methodName,
                    new[] { typeof(byte[]), typeof(int), typeof(int),
                        typeof(string), typeof(string),
                        typeof(string), typeof(bool), typeof(string), typeof(bool) })!);
            il.Append(il.Create(OpCodes.Call, method));
        }
    }

    internal void EmitInspectConvert(ILProcessor il, IrInspectConvert ic)
    {
        _ctx.Location.EmitLocationArgs(il, ic.Target);
        EmitIrInspectPatternValue(il, ic.FromSet);
        EmitIrInspectPatternValue(il, ic.ToSet);
        EmitIrInspectPatternValueAsOptionalString(il, ic.BeforePattern);
        il.Append(il.Create(ic.BeforeInitial ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
        EmitIrInspectPatternValueAsOptionalString(il, ic.AfterPattern);
        il.Append(il.Create(ic.AfterInitial ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));

        var method = _ctx.Module.ImportReference(
            typeof(Runtime.InspectRuntime).GetMethod("Convert",
                new[] { typeof(byte[]), typeof(int), typeof(int),
                    typeof(string), typeof(string),
                    typeof(string), typeof(bool), typeof(string), typeof(bool) })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    internal void EmitIrInspectPatternValue(ILProcessor il, IR.IrInspectPatternValue? pv)
    {
        if (pv == null || pv.IsLiteral)
        {
            il.Append(il.Create(OpCodes.Ldstr, pv?.Literal ?? ""));
        }
        else if (pv.IsLocation)
        {
            _ctx.Location.EmitLocationArgs(il, pv.Location!);
            var readMethod = _ctx.Module.ImportReference(
                typeof(Runtime.StorageHelpers).GetMethod("ReadFieldAsRawString",
                    new[] { typeof(byte[]), typeof(int), typeof(int) })!);
            il.Append(il.Create(OpCodes.Call, readMethod));
        }
    }

    /// <summary>
    /// Emit an InspectPatternValue as a nullable string for BEFORE/AFTER/CONVERTING args.
    /// Literals use Ldstr (compile-time). Data refs use ReadFieldAsRawString (runtime).
    /// Null patterns emit Ldnull.
    /// </summary>
    internal void EmitIrInspectPatternValueAsOptionalString(ILProcessor il,
        IR.IrInspectPatternValue? pv)
    {
        if (pv == null)
            il.Append(il.Create(OpCodes.Ldnull));
        else
            EmitIrInspectPatternValue(il, pv);
    }
}
