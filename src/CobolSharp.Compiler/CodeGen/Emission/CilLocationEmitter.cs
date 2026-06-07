// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Linq;
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
            // Data-model migration S3: a typed-native field has no byte window. The S3 typed cells
            // (MOVE-literal, DISPLAY) handle IrTypedFieldLocation directly; any other op reaching the byte
            // path for a typed field is unsupported until that op's typed cell (or the materialize fallback)
            // lands — fail loudly (no silent miscompile) rather than push a bogus (area,offset,length).
            case IR.IrTypedLocation t:
                throw new NotSupportedException(
                    $"Typed-native location ({t.GetType().Name}) reached a byte-window operation; the data-model " +
                    "migration (RECORD_STRUCT_STORAGE_DESIGN.md) routes typed reads through the materialize fallback " +
                    "(EmitLocationArgsMaterializingTyped) and typed writes through the typed cells. Add the missing " +
                    "typed cell (or materialize fallback) for this op before using it here.");

            case IR.IrCachedLocation cached:
                EmitCachedLocationArgs(il, cached);
                break;

            case IR.IrStaticLocation s when s.Location.OwnerProgramId != null:
                // GLOBAL item inherited from a containing program: load that program's State, so the
                // storage is shared between the declaring and the contained program (ISO §8.4.5).
                EmitForeignGlobalLocationArgs(il, s.Location);
                break;

            case IR.IrStaticLocation s when s.Location.Area == StorageAreaKind.LinkageSection:
                // LINKAGE item: load from ManagedPointer static field
                EmitLinkageLocationArgs(il, s);
                break;

            case IR.IrStaticLocation s
                when TryGetExternalField(s.Location.Area, s.Location.Offset, out var extField, out var adjOffset):
                // EXTERNAL item: load from shared ExternalStorage byte[]. Fires for an EXTERNAL WS record
                // (Area=WorkingStorage, IC226A) or an FD ... IS EXTERNAL record area (Area=FileSection,
                // IC227A); TryGetExternalField matches the range registered for this reference's own area.
                il.Append(il.Create(OpCodes.Ldsfld, extField!));
                il.Append(il.Create(OpCodes.Ldc_I4, adjOffset));
                il.Append(il.Create(OpCodes.Ldc_I4, s.Location.Length));
                break;

            case IR.IrStaticLocation s:
                EmitLoadBackingArray(il, s.Location.Area);
                il.Append(il.Create(OpCodes.Ldc_I4, s.Location.Offset));
                il.Append(il.Create(OpCodes.Ldc_I4, s.Location.Length));
                break;

            case IR.IrOdoGroupLocation o:
                EmitOdoGroupLocationArgs(il, o);
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
    /// Like <see cref="EmitLocationArgs"/>, but a typed-native field (S3) used as a READ-ONLY operand — a
    /// <b>sender</b>, e.g. a comparison operand — is materialized to a scratch byte window
    /// (<c>CobolString.ToWindow</c>, Latin-1) so the byte engine can read it (the §2.5 materialize floor;
    /// Latin-1 round-trips losslessly, so it is byte-identical). <b>SENDER-ONLY:</b> the called byte op must not
    /// write the window — there is no write-back — so this is used only for read operands, never receivers.
    /// </summary>
    internal void EmitLocationArgsMaterializingTyped(ILProcessor il, IR.IrLocation loc)
    {
        if (loc is IR.IrTypedLocation t)
        {
            if (t.Pic.Category == Runtime.CobolCategory.Numeric)
            {
                // S4 numeric sender-materialize: a typed NUMERIC value (`long`/`decimal`). Encode it into a scratch
                // byte window via the SAME codec the byte field uses (PicRuntime.EncodeNumeric) so the byte op reads
                // identical bytes — byte-identical, because encode∘decode is a round-trip for an in-range value. The
                // shared helper leaves (scratch, 0, width) on the stack; the WithPic wrapper appends the pic.
                EmitMaterializeNumericToScratch(il, t);
                return;
            }

            // character: load the typed string, then CobolString.ToWindow(string, width) -> byte[width];
            // then push (array, 0, width). (Works for a flat field, a record-struct member, or an array element —
            // _ctx.Data.EmitTypedLoad dispatches on the concrete typed-location shape.)
            _ctx.Data.EmitTypedLoad(il, t);
            il.Append(il.Create(OpCodes.Ldc_I4, t.Width));
            il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
                typeof(CobolSharp.Runtime.Text.CobolString).GetMethod(
                    "ToWindow", new[] { typeof(string), typeof(int) })!)));
            il.Append(il.Create(OpCodes.Ldc_I4_0));
            il.Append(il.Create(OpCodes.Ldc_I4, t.Width));
            return;
        }
        EmitLocationArgs(il, loc);
    }

    /// <summary>
    /// Push (area, offset, length, pic) onto the IL stack for any IrLocation.
    /// </summary>
    internal void EmitLocationArgsWithPic(ILProcessor il, IR.IrLocation loc)
    {
        EmitLocationArgs(il, loc);
        _ctx.Expression.EmitLoadPicDescriptor(il, loc.GetPic());
    }

    /// <summary>Like <see cref="EmitLocationArgsWithPic"/> but materializes a typed-native field
    /// (<see cref="EmitLocationArgsMaterializingTyped"/>) — SENDER-ONLY — then pushes its PicDescriptor. Used by
    /// read-only PIC-taking ops on a typed field (e.g. the <c>IS NUMERIC</c> class condition).</summary>
    internal void EmitLocationArgsWithPicMaterializingTyped(ILProcessor il, IR.IrLocation loc)
    {
        EmitLocationArgsMaterializingTyped(il, loc);
        _ctx.Expression.EmitLoadPicDescriptor(il, loc.GetPic());
    }

    /// <summary>
    /// Allocates a scratch <c>byte[width]</c>, encodes the typed numeric field's current <c>long</c> value into it
    /// via <c>PicRuntime.EncodeNumeric</c> (the SAME codec the byte field uses), and leaves <c>(scratch, 0, width)</c>
    /// on the stack. Returns the scratch local. Shared by the numeric sender-materialize (which then needs only the
    /// pic appended) and the numeric-receiver prologue (which keeps the local for the write-back epilogue).
    /// </summary>
    private VariableDefinition EmitMaterializeNumericToScratch(ILProcessor il, IR.IrTypedLocation t)
    {
        var scratch = new VariableDefinition(_ctx.Module.ImportReference(typeof(byte[])));
        _ctx.CurrentMethodDef!.Body.Variables.Add(scratch);
        il.Append(il.Create(OpCodes.Ldc_I4, t.Width));
        il.Append(il.Create(OpCodes.Newarr, _ctx.Module.TypeSystem.Byte));
        il.Append(il.Create(OpCodes.Stloc, scratch));
        // EncodeNumeric(scratch, 0, width, pic, currentValue-as-decimal)
        il.Append(il.Create(OpCodes.Ldloc, scratch));
        il.Append(il.Create(OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Ldc_I4, t.Width));
        _ctx.Expression.EmitLoadPicDescriptor(il, t.Pic);
        _ctx.Data.EmitTypedLoad(il, t);
        // A `long` field needs widening to `decimal`; a `decimal` field is already the right type.
        if (!t.IsDecimalNumeric)
            il.Append(il.Create(OpCodes.Newobj, _ctx.Module.ImportReference(
                typeof(decimal).GetConstructor(new[] { typeof(long) })!)));
        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod("EncodeNumeric",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor), typeof(decimal) })!)));
        // leave (scratch, 0, width) on the stack for the byte op
        il.Append(il.Create(OpCodes.Ldloc, scratch));
        il.Append(il.Create(OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Ldc_I4, t.Width));
        return scratch;
    }

    /// <summary>
    /// S4 typed-NUMERIC receiver — PROLOGUE. Materializes the field's current <c>long</c> into a scratch window and
    /// pushes <c>(scratch, 0, width, pic)</c> so the byte arithmetic op can read-modify-write it in place; returns the
    /// scratch local for the matching <see cref="EmitTypedNumericReceiverEpilogue"/>. The current value is
    /// materialized even for write-only (GIVING) receivers — harmless, the op overwrites it — so the prologue is
    /// uniform across read-modify-write (<c>ADD…TO</c>) and write-only (<c>…GIVING</c>) receivers.
    /// </summary>
    internal VariableDefinition EmitTypedNumericReceiverPrologue(ILProcessor il, IR.IrTypedLocation t)
    {
        var scratch = EmitMaterializeNumericToScratch(il, t);
        _ctx.Expression.EmitLoadPicDescriptor(il, t.Pic);   // the receiver args take a pic too
        return scratch;
    }

    /// <summary>
    /// S4 typed-NUMERIC receiver — EPILOGUE. After the byte arithmetic op has written its result into the scratch
    /// window, decode it (<c>PicRuntime.DecodeNumeric</c>) and store back into the field — the write-back. The
    /// decoded value is the field's truncated/scaled image, exactly what the byte path holds, so DISPLAY and
    /// downstream arithmetic stay byte-identical. A <c>long</c> field takes the explicit decimal→long narrowing; a
    /// <c>decimal</c> field stores the decoded value directly.
    /// </summary>
    internal void EmitTypedNumericReceiverEpilogue(ILProcessor il, IR.IrTypedLocation t, VariableDefinition scratch)
    {
        // store-target prefix: container addressing pushed BEFORE the value (struct instance addr, or array+index).
        _ctx.Data.EmitTypedStorePrefix(il, t);
        // DecodeNumeric(scratch, 0, width, pic) -> decimal
        il.Append(il.Create(OpCodes.Ldloc, scratch));
        il.Append(il.Create(OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Ldc_I4, t.Width));
        _ctx.Expression.EmitLoadPicDescriptor(il, t.Pic);
        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod("DecodeNumeric",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor) })!)));
        // a `long` receiver narrows decimal→long; a `decimal` receiver stores the decoded value as-is.
        if (!t.IsDecimalNumeric)
            il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
                typeof(decimal).GetMethods().Single(m =>
                    m.Name == "op_Explicit" && m.ReturnType == typeof(long)
                    && m.GetParameters() is { Length: 1 } p && p[0].ParameterType == typeof(decimal)))));
        // store op: stfld member / stsfld flat / stelem element.
        _ctx.Data.EmitTypedStoreSuffix(il, t);
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
        // Push base (array, baseOffset). A LINKAGE base resolves through the ManagedPointer
        // (runtime offset); WorkingStorage/EXTERNAL/etc. use a compile-time offset.
        if (e.BaseLocation.Area == StorageAreaKind.LinkageSection)
        {
            EmitLinkageBufferAndOffset(il, e.BaseLocation.Offset);
        }
        else
        {
            EmitLoadBackingArrayOrExternal(il, e.BaseLocation.Area, e.BaseLocation.Offset, out var elemAdjOffset);
            // Push base offset — accumulates displacement from each dimension
            il.Append(il.Create(OpCodes.Ldc_I4, elemAdjOffset));
        }

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
    /// Push (area, offset, runtimeLength) for an ODO-variable-length group/table.
    /// runtimeLength = (maxLength - maxOccurs*elementSize) + dependingOnValue*elementSize,
    /// i.e. the compile-time layout size with the inactive trailing occurrences removed.
    /// </summary>
    internal void EmitOdoGroupLocationArgs(ILProcessor il, IR.IrOdoGroupLocation o)
    {
        if (o.Base.Area == StorageAreaKind.LinkageSection)
        {
            EmitLinkageBufferAndOffset(il, o.Base.Offset);
        }
        else
        {
            EmitLoadBackingArrayOrExternal(il, o.Base.Area, o.Base.Offset, out var adjOffset);
            il.Append(il.Create(OpCodes.Ldc_I4, adjOffset));
        }

        // Fixed (non-ODO) part of the length, known at compile time.
        int fixedPart = o.Base.Length - o.MaxOccurs * o.ElementSize;
        il.Append(il.Create(OpCodes.Ldc_I4, fixedPart));

        // + dependingOnValue * elementSize, read at runtime.
        EmitLocationArgsWithPic(il, o.DependingOnLocation);
        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
            typeof(PicRuntime).GetMethod("DecodeNumeric",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor) })!)));
        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
            typeof(Convert).GetMethod("ToInt32", new[] { typeof(decimal) })!)));
        il.Append(il.Create(OpCodes.Ldc_I4, o.ElementSize));
        il.Append(il.Create(OpCodes.Mul));
        il.Append(il.Create(OpCodes.Add));

        // Stack: [area, offset, runtimeLength]
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
            case IR.IrStaticLocation s when s.Location.Area == StorageAreaKind.LinkageSection:
                // Reference-modified USING parameter: base via the ManagedPointer.
                EmitLinkageBufferAndOffset(il, s.Location.Offset);
                break;

            case IR.IrStaticLocation s
                when TryGetExternalField(s.Location.Area, s.Location.Offset, out var rmExtField, out var rmAdjOffset):
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

    /// <summary>
    /// Emit (array, offset, length) for a GLOBAL item that a contained program inherits from a
    /// containing program. The bytes live in the containing program's ProgramState, so we load that
    /// program type's static <c>State</c> field (all program types share one module and the
    /// container is emitted first) and the appropriate backing array, then the owner-relative offset.
    /// </summary>
    private void EmitForeignGlobalLocationArgs(ILProcessor il, StorageLocation loc)
    {
        var stateField = ResolveForeignStateField(loc.OwnerProgramId!)
            ?? throw new InvalidOperationException(
                $"Containing program '{loc.OwnerProgramId}' not found for a GLOBAL reference.");

        il.Append(il.Create(OpCodes.Ldsfld, stateField));
        string propertyName = loc.Area switch
        {
            StorageAreaKind.WorkingStorage => "WorkingStorage",
            StorageAreaKind.FileSection    => "FileSection",
            _ => "WorkingStorage"
        };
        var getter = _ctx.Module.ImportReference(
            typeof(CobolSharp.Runtime.ProgramState).GetProperty(propertyName)!.GetGetMethod()!);
        il.Append(il.Create(OpCodes.Callvirt, getter));
        il.Append(il.Create(OpCodes.Ldc_I4, loc.Offset));
        il.Append(il.Create(OpCodes.Ldc_I4, loc.Length));
    }

    /// <summary>Find the static <c>State</c> field of another program type in the shared module.</summary>
    private FieldReference? ResolveForeignStateField(string ownerProgramId)
    {
        foreach (var type in _ctx.Module.Types)
        {
            if (!string.Equals(type.Name, ownerProgramId, StringComparison.OrdinalIgnoreCase))
                continue;
            foreach (var field in type.Fields)
                if (field.Name == "State")
                    return field;
        }
        return null;
    }

    internal void EmitLoadBackingArray(ILProcessor il, StorageAreaKind area)
    {
        // LINKAGE SECTION items are NOT backed by ProgramState — they're backed
        // by ManagedPointer fields populated from CALL USING args.
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
                "LinkageSection should be handled separately via ManagedPointer.")
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
        if (TryGetExternalField(area, wsOffset, out var extField, out adjustedOffset))
        {
            il.Append(il.Create(OpCodes.Ldsfld, extField!));
            return;
        }

        adjustedOffset = wsOffset;
        EmitLoadBackingArray(il, area);
    }

    /// <summary>
    /// Emit (area, offset, length) for a LINKAGE SECTION item.
    /// Loads from the ManagedPointer field, adding the relative offset.
    /// </summary>
    internal void EmitLinkageLocationArgs(ILProcessor il, IR.IrStaticLocation s)
    {
        // Push (Buffer, pointer.Offset + relativeOffset) from the matching ManagedPointer,
        // then the item length.
        EmitLinkageBufferAndOffset(il, s.Location.Offset);
        il.Append(il.Create(OpCodes.Ldc_I4, s.Location.Length));
    }

    /// <summary>
    /// Find the ManagedPointer field for the USING parameter whose storage range contains
    /// <paramref name="relOffset"/> (a LINKAGE-section offset), or null if it is unmapped.
    /// <paramref name="paramBaseOffset"/> receives the parameter's own base LINKAGE offset, so the
    /// caller can compute the displacement WITHIN the parameter (relOffset - paramBaseOffset).
    /// </summary>
    private FieldDefinition? FindLinkageField(int relOffset, out int paramBaseOffset)
    {
        paramBaseOffset = 0;
        if (_ctx.SemanticModel == null) return null;
        foreach (var param in _ctx.SemanticModel.ProcedureUsingParameters)
        {
            if (!_ctx.LinkageFields.TryGetValue(param.Name, out var f)) continue;
            var paramLoc = _ctx.SemanticModel.GetStorageLocation(param);
            if (paramLoc.HasValue &&
                relOffset >= paramLoc.Value.Offset &&
                relOffset < paramLoc.Value.Offset + paramLoc.Value.Length)
            {
                paramBaseOffset = paramLoc.Value.Offset;
                return f;
            }
        }
        // The PROCEDURE DIVISION RETURNING item is the trailing linkage parameter (the caller passes it as the
        // last BY-REFERENCE argument; Binder adds its _linkage_<name> field). Resolve a reference into it through
        // that field too — otherwise it falls through to the null-buffer branch and the callee writes to a
        // 0-length area (RT0001 bufferLength=0).
        if (_ctx.SemanticModel.ProcedureReturningItem is { } ret
            && _ctx.LinkageFields.TryGetValue(ret.Name, out var rf))
        {
            var retLoc = _ctx.SemanticModel.GetStorageLocation(ret);
            if (retLoc.HasValue &&
                relOffset >= retLoc.Value.Offset &&
                relOffset < retLoc.Value.Offset + retLoc.Value.Length)
            {
                paramBaseOffset = retLoc.Value.Offset;
                return rf;
            }
        }
        return null;
    }

    /// <summary>
    /// Push [ManagedPointer.Buffer, pointer.Offset + (relOffset - paramBase)] for a LINKAGE
    /// location. The pointer addresses the caller's argument storage, so the displacement must be
    /// taken WITHIN the matched USING parameter (relOffset - parameter's base LINKAGE offset), NOT
    /// the absolute LINKAGE offset — otherwise the 2nd and later parameters (whose base offset is
    /// non-zero) read/write the wrong bytes of the caller's data. (The 1st parameter has base 0,
    /// so single-parameter CALLs were unaffected and masked this bug.) This is the (array,
    /// base-offset) pair the element-address and ref-mod composition expect; the LINKAGE base
    /// offset is a runtime value, so a subscripted or ref-modified USING parameter routes here
    /// instead of EmitLoadBackingArray. Falls back to [null, 0] for an unbound LINKAGE item.
    /// </summary>
    internal void EmitLinkageBufferAndOffset(ILProcessor il, int relOffset)
    {
        var field = FindLinkageField(relOffset, out int paramBase);
        if (field != null)
        {
            il.Append(il.Create(OpCodes.Ldsflda, field));
            il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
                typeof(ManagedPointer).GetProperty("Buffer")!.GetGetMethod()!)));
            il.Append(il.Create(OpCodes.Ldsflda, field));
            il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
                typeof(ManagedPointer).GetProperty("Offset")!.GetGetMethod()!)));
            il.Append(il.Create(OpCodes.Ldc_I4, relOffset - paramBase));
            il.Append(il.Create(OpCodes.Add));
        }
        else
        {
            il.Append(il.Create(OpCodes.Ldnull));
            il.Append(il.Create(OpCodes.Ldc_I4, relOffset - paramBase));
        }
    }

    /// <summary>
    /// Try to find the EXTERNAL byte[] field for an offset in the given storage area.
    /// Returns true if the offset falls within an EXTERNAL record's range REGISTERED FOR THAT AREA.
    /// The area match is required: WorkingStorage and FileSection have independent offset namespaces, so a
    /// FileSection EXTERNAL range at offset N must not redirect a WorkingStorage reference at N (which would
    /// corrupt IC226A's EXTERNAL WS), and vice-versa.
    /// adjustedOffset is the offset relative to the EXTERNAL array (always starts at 0).
    /// </summary>
    internal bool TryGetExternalField(StorageAreaKind area, int offset, out FieldDefinition? extField, out int adjustedOffset)
    {
        foreach (var (rangeArea, rangeOffset, rangeLength, field) in _ctx.ExternalRanges)
        {
            if (rangeArea == area && offset >= rangeOffset && offset < rangeOffset + rangeLength)
            {
                extField = field;
                adjustedOffset = offset - rangeOffset;
                return true;
            }
        }
        extField = null;
        adjustedOffset = 0;
        return false;
    }
}
