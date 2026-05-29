// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Linq;
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

        // Resolve the UnstringExtract method reference (with PicDescriptor for MOVE semantics)
        var extractMethod = _ctx.Module.ImportReference(
            typeof(Runtime.StorageHelpers).GetMethod("UnstringExtract",
                new[] { typeof(byte[]), typeof(int), typeof(int),
                        typeof(byte[]), typeof(int), typeof(int),
                        typeof(Runtime.PicDescriptor),
                        typeof(string[]), typeof(bool[]),
                        typeof(byte[]), typeof(int), typeof(int),
                        typeof(int).MakeByRefType(), typeof(bool).MakeByRefType() })!);

        // Count local for COUNT IN write-back
        var countLocal = new VariableDefinition(_ctx.Module.TypeSystem.Int32);
        _ctx.CurrentMethodDef.Body.Variables.Add(countLocal);

        // Pre-loop overflow check: per ISO §14.9.44 condition (a),
        // overflow occurs if pointer < 1 or pointer > source size at START.
        {
            var srcLen = unstrStmt.Source.GetPic().StorageLength;
            var setOverflow = il.Create(OpCodes.Ldc_I4_1);
            var endCheck = il.Create(OpCodes.Nop);

            // if (ptr < 1) goto setOverflow
            il.Append(il.Create(OpCodes.Ldloc, ptrLocal));
            il.Append(il.Create(OpCodes.Ldc_I4_1));
            il.Append(il.Create(OpCodes.Blt, setOverflow));
            // if (ptr > srcLen) goto setOverflow
            il.Append(il.Create(OpCodes.Ldloc, ptrLocal));
            il.Append(il.Create(OpCodes.Ldc_I4, srcLen));
            il.Append(il.Create(OpCodes.Bgt, setOverflow));
            // else goto endCheck (no overflow)
            il.Append(il.Create(OpCodes.Br, endCheck));
            // setOverflow: overflowLocal = true
            il.Append(setOverflow); // pushes 1
            il.Append(il.Create(OpCodes.Stloc, overflowLocal));
            // endCheck:
            il.Append(endCheck);
        }

        // Build delimiter arrays (string[]? and bool[]?) — shared across all INTO iterations.
        // For field-based delimiters, we must read the field value at runtime each time
        // (since the field could change), but for literal-only delimiters we build once.
        var hasFieldDelimiters = unstrStmt.Delimiters.Any(d => d.Location != null);
        var delimArrLocal = new VariableDefinition(_ctx.Module.ImportReference(typeof(string[])));
        var delimFlagsLocal = new VariableDefinition(_ctx.Module.ImportReference(typeof(bool[])));
        _ctx.CurrentMethodDef.Body.Variables.Add(delimArrLocal);
        _ctx.CurrentMethodDef.Body.Variables.Add(delimFlagsLocal);

        if (unstrStmt.Delimiters.Count > 0)
        {
            // Create string[] for delimiters
            il.Append(il.Create(OpCodes.Ldc_I4, unstrStmt.Delimiters.Count));
            il.Append(il.Create(OpCodes.Newarr, _ctx.Module.TypeSystem.String));
            for (int di = 0; di < unstrStmt.Delimiters.Count; di++)
            {
                var d = unstrStmt.Delimiters[di];
                il.Append(il.Create(OpCodes.Dup));
                il.Append(il.Create(OpCodes.Ldc_I4, di));
                if (d.LiteralValue != null)
                    il.Append(il.Create(OpCodes.Ldstr, d.LiteralValue));
                else if (d.Location != null)
                {
                    _ctx.Location.EmitLocationArgs(il, d.Location);
                    il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
                        typeof(Runtime.StorageHelpers).GetMethod("ReadFieldAsString",
                            new[] { typeof(byte[]), typeof(int), typeof(int) })!)));
                }
                else
                    il.Append(il.Create(OpCodes.Ldstr, ""));
                il.Append(il.Create(OpCodes.Stelem_Ref));
            }
            il.Append(il.Create(OpCodes.Stloc, delimArrLocal));

            // Create bool[] for ALL flags
            il.Append(il.Create(OpCodes.Ldc_I4, unstrStmt.Delimiters.Count));
            il.Append(il.Create(OpCodes.Newarr, _ctx.Module.TypeSystem.Boolean));
            for (int di = 0; di < unstrStmt.Delimiters.Count; di++)
            {
                if (unstrStmt.Delimiters[di].IsAll)
                {
                    il.Append(il.Create(OpCodes.Dup));
                    il.Append(il.Create(OpCodes.Ldc_I4, di));
                    il.Append(il.Create(OpCodes.Ldc_I4_1));
                    il.Append(il.Create(OpCodes.Stelem_I1));
                }
            }
            il.Append(il.Create(OpCodes.Stloc, delimFlagsLocal));
        }
        else
        {
            il.Append(il.Create(OpCodes.Ldnull));
            il.Append(il.Create(OpCodes.Stloc, delimArrLocal));
            il.Append(il.Create(OpCodes.Ldnull));
            il.Append(il.Create(OpCodes.Stloc, delimFlagsLocal));
        }

        // Process each INTO
        foreach (var into in unstrStmt.Intos)
        {
            // Push source args (area, offset, length)
            _ctx.Location.EmitLocationArgs(il, unstrStmt.Source);

            // Push dest args (area, offset, length, PicDescriptor)
            _ctx.Location.EmitLocationArgsWithPic(il, into.Target);

            // Push delimiter arrays
            il.Append(il.Create(OpCodes.Ldloc, delimArrLocal));
            il.Append(il.Create(OpCodes.Ldloc, delimFlagsLocal));

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

            // Increment tally counter — only if INTO target was acted upon.
            // UnstringExtract returns -1 when source is exhausted (not acted upon).
            // Per spec, tally counts only INTO targets that received data.
            var skipTally = il.Create(OpCodes.Nop);
            il.Append(il.Create(OpCodes.Ldloc, countLocal));
            il.Append(il.Create(OpCodes.Ldc_I4_0));
            il.Append(il.Create(OpCodes.Blt, skipTally));
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

    internal void EmitInspectTallying(ILProcessor il, IrInspectTallying inspect)
    {
        int n = inspect.Ops.Count;

        // Marshal the operands into parallel arrays for a single comparison cycle.
        var kinds = EmitInspectIntArray(il, n, i => (int)inspect.Ops[i].Kind);
        var patterns = EmitInspectStringArray(il, n, i => inspect.Ops[i].Pattern);
        var befores = EmitInspectStringArray(il, n, i => inspect.Ops[i].BeforePattern);
        var afters = EmitInspectStringArray(il, n, i => inspect.Ops[i].AfterPattern);

        // counts = InspectRuntime.TallyingPass(area, offset, length, targetPic, kinds, patterns, befores, afters)
        var counts = new VariableDefinition(_ctx.Module.ImportReference(typeof(int[])));
        _ctx.CurrentMethodDef!.Body.Variables.Add(counts);
        _ctx.Location.EmitLocationArgsWithPic(il, inspect.Target);   // area, offset, length, pic (pic drives GR 4d de-signing)
        il.Append(il.Create(OpCodes.Ldloc, kinds));
        il.Append(il.Create(OpCodes.Ldloc, patterns));
        il.Append(il.Create(OpCodes.Ldloc, befores));
        il.Append(il.Create(OpCodes.Ldloc, afters));
        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
            typeof(Runtime.InspectRuntime).GetMethod("TallyingPass",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(int[]), typeof(string[]), typeof(string[]), typeof(string[]) })!)));
        il.Append(il.Create(OpCodes.Stloc, counts));

        // Add each operand's count into its counter field.
        var addMethod = _ctx.Module.ImportReference(
            typeof(Runtime.InspectRuntime).GetMethod("AddCountToField",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor), typeof(int) })!);
        for (int i = 0; i < n; i++)
        {
            _ctx.Location.EmitLocationArgsWithPic(il, inspect.Ops[i].Counter);
            il.Append(il.Create(OpCodes.Ldloc, counts));
            il.Append(il.Create(OpCodes.Ldc_I4, i));
            il.Append(il.Create(OpCodes.Ldelem_I4));
            il.Append(il.Create(OpCodes.Call, addMethod));
        }
    }

    internal void EmitInspectReplacing(ILProcessor il, IrInspectReplacing inspect)
    {
        int n = inspect.Ops.Count;

        var kinds = EmitInspectIntArray(il, n, i => (int)inspect.Ops[i].Kind);
        var patterns = EmitInspectStringArray(il, n, i => inspect.Ops[i].Pattern);
        var replacements = EmitInspectStringArray(il, n, i => inspect.Ops[i].Replacement);
        var befores = EmitInspectStringArray(il, n, i => inspect.Ops[i].BeforePattern);
        var afters = EmitInspectStringArray(il, n, i => inspect.Ops[i].AfterPattern);

        // InspectRuntime.ReplacingPass(area, offset, length, kinds, patterns, replacements, befores, afters)
        _ctx.Location.EmitLocationArgs(il, inspect.Target);
        il.Append(il.Create(OpCodes.Ldloc, kinds));
        il.Append(il.Create(OpCodes.Ldloc, patterns));
        il.Append(il.Create(OpCodes.Ldloc, replacements));
        il.Append(il.Create(OpCodes.Ldloc, befores));
        il.Append(il.Create(OpCodes.Ldloc, afters));
        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
            typeof(Runtime.InspectRuntime).GetMethod("ReplacingPass",
                new[] { typeof(byte[]), typeof(int), typeof(int),
                        typeof(int[]), typeof(string[]), typeof(string[]), typeof(string[]), typeof(string[]) })!)));
    }

    /// <summary>Build an int[] local from a per-index selector (kinds for INSPECT operands).</summary>
    private VariableDefinition EmitInspectIntArray(ILProcessor il, int count, System.Func<int, int> selector)
    {
        var local = new VariableDefinition(_ctx.Module.ImportReference(typeof(int[])));
        _ctx.CurrentMethodDef!.Body.Variables.Add(local);
        il.Append(il.Create(OpCodes.Ldc_I4, count));
        il.Append(il.Create(OpCodes.Newarr, _ctx.Module.TypeSystem.Int32));
        for (int i = 0; i < count; i++)
        {
            il.Append(il.Create(OpCodes.Dup));
            il.Append(il.Create(OpCodes.Ldc_I4, i));
            il.Append(il.Create(OpCodes.Ldc_I4, selector(i)));
            il.Append(il.Create(OpCodes.Stelem_I4));
        }
        il.Append(il.Create(OpCodes.Stloc, local));
        return local;
    }

    /// <summary>
    /// Build a string[] local from a per-index pattern selector. Literal patterns are baked
    /// at compile time; data-ref patterns are read from their field at runtime; null patterns
    /// (e.g. CHARACTERS, or absent BEFORE/AFTER) become null array elements.
    /// </summary>
    private VariableDefinition EmitInspectStringArray(
        ILProcessor il, int count, System.Func<int, IR.IrInspectPatternValue?> selector)
    {
        var local = new VariableDefinition(_ctx.Module.ImportReference(typeof(string[])));
        _ctx.CurrentMethodDef!.Body.Variables.Add(local);
        il.Append(il.Create(OpCodes.Ldc_I4, count));
        il.Append(il.Create(OpCodes.Newarr, _ctx.Module.TypeSystem.String));
        for (int i = 0; i < count; i++)
        {
            il.Append(il.Create(OpCodes.Dup));
            il.Append(il.Create(OpCodes.Ldc_I4, i));
            EmitIrInspectPatternValueAsOptionalString(il, selector(i));
            il.Append(il.Create(OpCodes.Stelem_Ref));
        }
        il.Append(il.Create(OpCodes.Stloc, local));
        return local;
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
