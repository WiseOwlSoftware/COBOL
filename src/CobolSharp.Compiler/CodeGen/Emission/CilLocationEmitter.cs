// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Mono.Cecil;
using Mono.Cecil.Cil;
using CobolSharp.Compiler.IR;
using CobolSharp.Compiler.Semantics;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.CodeGen.Emission;

/// <summary>
/// Location/address emission: pushes (area, offset, length) triples onto the IL stack
/// for all IrLocation variants (static, element/subscript, ref-mod, cached, LINKAGE, EXTERNAL).
/// </summary>
internal sealed class CilLocationEmitter
{
    private readonly EmissionContext _ctx;

    internal CilLocationEmitter(EmissionContext ctx) => _ctx = ctx;

    /// <summary>
    /// Push (area, offset, length) onto the IL stack for any IrLocation.
    /// For static: pushes compile-time constants.
    /// For element ref: computes runtime offset via subscript decode.
    /// For ref mod: composes base location + runtime start:length.
    /// </summary>
    internal void EmitLocationArgs(ILProcessor il, IR.IrLocation loc)
    {
        switch (loc)
        {
            case IR.IrCachedLocation cached:
                EmitCachedLocationArgs(il, cached);
                break;

            case IR.IrStaticLocation s when s.Location.Area == StorageAreaKind.LinkageSection:
                // LINKAGE item: load from CobolDataPointer static field
                EmitLinkageLocationArgs(il, s);
                break;

            case IR.IrStaticLocation s
                when s.Location.Area == StorageAreaKind.WorkingStorage
                  && TryGetExternalField(s.Location.Offset, out var extField, out var adjOffset):
                // EXTERNAL item: load from shared ExternalStorage byte[]
                il.Append(il.Create(OpCodes.Ldsfld, extField!));
                il.Append(il.Create(OpCodes.Ldc_I4, adjOffset));
                il.Append(il.Create(OpCodes.Ldc_I4, s.Location.Length));
                break;

            case IR.IrStaticLocation s:
                EmitLoadBackingArray(il, s.Location.Area);
                il.Append(il.Create(OpCodes.Ldc_I4, s.Location.Offset));
                il.Append(il.Create(OpCodes.Ldc_I4, s.Location.Length));
                break;

            case IR.IrElementRef e:
                EmitElementAddress(il, e);
                il.Append(il.Create(OpCodes.Ldc_I4, e.ElementSize));
                break;

            case IR.IrRefModLocation r:
                EmitRefModAddress(il, r);
                break;

            default:
                throw new NotSupportedException($"Unknown IrLocation type: {loc.GetType().Name}");
        }
    }

    /// <summary>
    /// Push (area, offset, length, pic) onto the IL stack for any IrLocation.
    /// </summary>
    internal void EmitLocationArgsWithPic(ILProcessor il, IR.IrLocation loc)
    {
        EmitLocationArgs(il, loc);
        _ctx.Expression.EmitLoadPicDescriptor(il, loc.GetPic());
    }

    /// <summary>
    /// Emit (area, offset, length) for a cached location. On first encounter with a
    /// given cache key, compute the inner location args, store into locals, and reload.
    /// On subsequent encounters, just load from the cached locals.
    /// </summary>
    internal void EmitCachedLocationArgs(ILProcessor il, IR.IrCachedLocation cached)
    {
        if (_ctx.CachedLocationLocals.TryGetValue(cached.CacheKey, out var locals))
        {
            // Already computed — reload from locals
            il.Append(il.Create(OpCodes.Ldloc, locals.area));
            il.Append(il.Create(OpCodes.Ldloc, locals.offset));
            il.Append(il.Create(OpCodes.Ldloc, locals.length));
            return;
        }

        // First encounter — compute inner, store into locals
        EmitLocationArgs(il, cached.Inner);

        var body = _ctx.CurrentMethodDef!.Body;
        var lengthLocal = new VariableDefinition(_ctx.Module.TypeSystem.Int32);
        body.Variables.Add(lengthLocal);
        var offsetLocal = new VariableDefinition(_ctx.Module.TypeSystem.Int32);
        body.Variables.Add(offsetLocal);
        var areaLocal = new VariableDefinition(_ctx.Module.ImportReference(typeof(byte[])));
        body.Variables.Add(areaLocal);

        // Stack is: area, offset, length — store in reverse order
        il.Append(il.Create(OpCodes.Stloc, lengthLocal));
        il.Append(il.Create(OpCodes.Stloc, offsetLocal));
        il.Append(il.Create(OpCodes.Stloc, areaLocal));

        _ctx.CachedLocationLocals[cached.CacheKey] = (areaLocal, offsetLocal, lengthLocal);

        // Reload onto stack
        il.Append(il.Create(OpCodes.Ldloc, areaLocal));
        il.Append(il.Create(OpCodes.Ldloc, offsetLocal));
        il.Append(il.Create(OpCodes.Ldloc, lengthLocal));
    }

    /// <summary>
    /// Push (area, effectiveOffset) for a multi-dimensional IrElementRef.
    /// Each subscript is an IrExpression evaluated via EmitIrExpression → decimal → int32.
    /// Handles identifiers (ARR(I)), arithmetic (ARR(I+1)), and any expression uniformly.
    /// </summary>
    internal void EmitElementAddress(ILProcessor il, IR.IrElementRef e)
    {
        // Push base area (EXTERNAL-aware)
        EmitLoadBackingArrayOrExternal(il, e.BaseLocation.Area, e.BaseLocation.Offset, out var elemAdjOffset);

        // Push base offset — accumulates displacement from each dimension
        il.Append(il.Create(OpCodes.Ldc_I4, elemAdjOffset));

        var toInt32 = _ctx.Module.ImportReference(
            typeof(Convert).GetMethod("ToInt32", new[] { typeof(decimal) })!);

        for (int dim = 0; dim < e.Subscripts.Count; dim++)
        {
            int multiplier = e.Multipliers[dim];

            // Evaluate subscript expression → decimal on stack
            _ctx.Expression.EmitIrExpression(il, e.Subscripts[dim]);

            // decimal → int32
            il.Append(il.Create(OpCodes.Call, toInt32));

            // (subscript - 1) * multiplier
            il.Append(il.Create(OpCodes.Ldc_I4_1));
            il.Append(il.Create(OpCodes.Sub));
            il.Append(il.Create(OpCodes.Ldc_I4, multiplier));
            il.Append(il.Create(OpCodes.Mul));

            // Add to running offset
            il.Append(il.Create(OpCodes.Add));
        }

        // Stack: [area, effectiveOffset]
    }

    /// <summary>
    /// Push (area, substringOffset, substringLength) for a reference modification.
    /// Composes the base location (static or element) with runtime start:length.
    /// </summary>
    internal void EmitRefModAddress(ILProcessor il, IR.IrRefModLocation r)
    {
        var toInt32 = _ctx.Module.ImportReference(
            typeof(Convert).GetMethod("ToInt32", new[] { typeof(decimal) })!);

        // Evaluate start and length first (into locals), before pushing base
        // start (1-based)
        _ctx.Expression.EmitIrExpression(il, r.Start);
        il.Append(il.Create(OpCodes.Call, toInt32));
        var startLocal = new VariableDefinition(_ctx.Module.TypeSystem.Int32);
        _ctx.CurrentMethodDef!.Body.Variables.Add(startLocal);
        il.Append(il.Create(OpCodes.Stloc, startLocal));

        // length: expression or rest-of-field
        VariableDefinition lengthLocal;
        if (r.Length != null)
        {
            _ctx.Expression.EmitIrExpression(il, r.Length!);
            il.Append(il.Create(OpCodes.Call, toInt32));
            lengthLocal = new VariableDefinition(_ctx.Module.TypeSystem.Int32);
            _ctx.CurrentMethodDef!.Body.Variables.Add(lengthLocal);
            il.Append(il.Create(OpCodes.Stloc, lengthLocal));
        }
        else
        {
            // Rest-of-field: length = baseFieldLength - (start - 1)
            lengthLocal = new VariableDefinition(_ctx.Module.TypeSystem.Int32);
            _ctx.CurrentMethodDef!.Body.Variables.Add(lengthLocal);
            il.Append(il.Create(OpCodes.Ldc_I4, r.BaseFieldLength));
            il.Append(il.Create(OpCodes.Ldloc, startLocal));
            il.Append(il.Create(OpCodes.Sub));
            il.Append(il.Create(OpCodes.Ldc_I4_1));
            il.Append(il.Create(OpCodes.Add));
            il.Append(il.Create(OpCodes.Stloc, lengthLocal));
        }

        // Push base location (area, baseOffset)
        switch (r.Base)
        {
            case IR.IrStaticLocation s
                when s.Location.Area == StorageAreaKind.WorkingStorage
                  && TryGetExternalField(s.Location.Offset, out var rmExtField, out var rmAdjOffset):
                il.Append(il.Create(OpCodes.Ldsfld, rmExtField!));
                il.Append(il.Create(OpCodes.Ldc_I4, rmAdjOffset));
                break;

            case IR.IrStaticLocation s:
                EmitLoadBackingArray(il, s.Location.Area);
                il.Append(il.Create(OpCodes.Ldc_I4, s.Location.Offset));
                break;

            case IR.IrElementRef e:
                EmitElementAddress(il, e);
                break;

            default:
                throw new NotSupportedException($"Unsupported base location for ref mod: {r.Base.GetType().Name}");
        }

        // Stack: [area, baseOffset]

        // baseOffset + (start - 1)
        il.Append(il.Create(OpCodes.Ldloc, startLocal));
        il.Append(il.Create(OpCodes.Ldc_I4_1));
        il.Append(il.Create(OpCodes.Sub));
        il.Append(il.Create(OpCodes.Add));

        // Push length
        il.Append(il.Create(OpCodes.Ldloc, lengthLocal));

        // Stack: [area, substringOffset, substringLength]
    }

    internal void EmitLoadBackingArray(ILProcessor il, StorageAreaKind area)
    {
        // LINKAGE SECTION items are NOT backed by ProgramState — they're backed
        // by CobolDataPointer fields populated from CALL USING args.
        // This method only handles WorkingStorage, LocalStorage, and FileSection.
        // LINKAGE access is handled separately in EmitLocationArgs.
        il.Append(il.Create(OpCodes.Ldsfld, _ctx.ProgramStateField!));

        var propertyName = area switch
        {
            StorageAreaKind.WorkingStorage => "WorkingStorage",
            StorageAreaKind.LocalStorage   => "LocalStorage",
            StorageAreaKind.FileSection    => "FileSection",
            _ => throw new InvalidOperationException(
                $"EmitLoadBackingArray: unexpected StorageAreaKind '{area}'. " +
                "LinkageSection should be handled separately via CobolDataPointer.")
        };

        var getter = _ctx.Module.ImportReference(
            typeof(CobolSharp.Runtime.ProgramState).GetProperty(propertyName)!.GetGetMethod()!);
        il.Append(il.Create(OpCodes.Callvirt, getter));
    }

    /// <summary>
    /// Load the backing array for a storage location, accounting for EXTERNAL items.
    /// For EXTERNAL WorkingStorage items, loads the shared ExternalStorage byte[] field.
    /// Returns the adjusted offset (0-based within the external array, or unchanged for non-external).
    /// </summary>
    internal void EmitLoadBackingArrayOrExternal(ILProcessor il, StorageAreaKind area, int wsOffset, out int adjustedOffset)
    {
        if (area == StorageAreaKind.WorkingStorage && TryGetExternalField(wsOffset, out var extField, out adjustedOffset))
        {
            il.Append(il.Create(OpCodes.Ldsfld, extField!));
            return;
        }

        adjustedOffset = wsOffset;
        EmitLoadBackingArray(il, area);
    }

    /// <summary>
    /// Emit (area, offset, length) for a LINKAGE SECTION item.
    /// Loads from the CobolDataPointer field, adding the relative offset.
    /// </summary>
    internal void EmitLinkageLocationArgs(ILProcessor il, IR.IrStaticLocation s)
    {
        // Find which LINKAGE parameter field this item belongs to.
        // Try to match the item name directly first, then search for a parent match.
        FieldDefinition? field = null;
        string? matchedName = null;

        // Try exact match (for 01-level LINKAGE items)
        if (_ctx.SemanticModel != null)
        {
            foreach (var param in _ctx.SemanticModel.ProcedureUsingParameters)
            {
                if (_ctx.LinkageFields.TryGetValue(param.Name, out var f))
                {
                    // Check if this storage location falls within this parameter's range
                    var paramLoc = _ctx.SemanticModel.GetStorageLocation(param);
                    if (paramLoc.HasValue &&
                        s.Location.Offset >= paramLoc.Value.Offset &&
                        s.Location.Offset < paramLoc.Value.Offset + paramLoc.Value.Length)
                    {
                        field = f;
                        matchedName = param.Name;
                        break;
                    }
                }
            }
        }

        if (field != null)
        {
            // Load CobolDataPointer.Buffer
            il.Append(il.Create(OpCodes.Ldsflda, field));
            var bufferGetter = _ctx.Module.ImportReference(
                typeof(CobolDataPointer).GetProperty("Buffer")!.GetGetMethod()!);
            il.Append(il.Create(OpCodes.Call, bufferGetter));

            // Offset = CobolDataPointer.Offset + relative offset within the parameter
            il.Append(il.Create(OpCodes.Ldsflda, field));
            var offsetGetter = _ctx.Module.ImportReference(
                typeof(CobolDataPointer).GetProperty("Offset")!.GetGetMethod()!);
            il.Append(il.Create(OpCodes.Call, offsetGetter));
            il.Append(il.Create(OpCodes.Ldc_I4, s.Location.Offset)); // relative offset
            il.Append(il.Create(OpCodes.Add));

            // Length
            il.Append(il.Create(OpCodes.Ldc_I4, s.Location.Length));
        }
        else
        {
            // Fallback: LINKAGE item not mapped to a USING parameter
            // (may happen for LINKAGE items not in the USING clause)
            // Push nulls that will likely cause a runtime NullReferenceException
            // if actually accessed — this is correct behavior for unmapped LINKAGE
            il.Append(il.Create(OpCodes.Ldnull));
            il.Append(il.Create(OpCodes.Ldc_I4, s.Location.Offset));
            il.Append(il.Create(OpCodes.Ldc_I4, s.Location.Length));
        }
    }

    /// <summary>
    /// Try to find the EXTERNAL byte[] field for a WorkingStorage offset.
    /// Returns true if the offset falls within an EXTERNAL record's range.
    /// adjustedOffset is the offset relative to the EXTERNAL array (always starts at 0).
    /// </summary>
    internal bool TryGetExternalField(int wsOffset, out FieldDefinition? extField, out int adjustedOffset)
    {
        foreach (var (rangeOffset, rangeLength, field) in _ctx.ExternalRanges)
        {
            if (wsOffset >= rangeOffset && wsOffset < rangeOffset + rangeLength)
            {
                extField = field;
                adjustedOffset = wsOffset - rangeOffset;
                return true;
            }
        }
        extField = null;
        adjustedOffset = 0;
        return false;
    }
}
