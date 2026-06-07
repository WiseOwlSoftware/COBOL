// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Linq;
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
/// EmitDefaultPicDescriptor, GetManagedPointerCtor, EmitOptionalString.
/// </summary>
internal sealed class CilDataEmitter
{
    private readonly EmissionContext _ctx;

    internal CilDataEmitter(EmissionContext ctx) => _ctx = ctx;

    // ── Data-model migration typed-location access primitives (docs/RECORD_STRUCT_STORAGE_DESIGN.md §9.2) ──
    // The THREE value-access primitives every typed cell is built on. They are the ONLY places that know a typed
    // location's shape — a flat static field (S3a), a record-struct member (S3b, InstanceName set), or an OCCURS
    // array element (S4, IrTypedElementLocation). Generalizing only these three lets every other numeric/char cell
    // work on an element unchanged. Store is split: a prefix (push the container addressing — struct address, or
    // array+index — BEFORE the value) and a suffix (the store op — stfld / stsfld / stelem).

    /// <summary>Pushes the element-address prefix for an OCCURS element: <c>ldsfld array; &lt;0-based index i4&gt;</c>
    /// (the COBOL 1-based subscript expression, decimal→int via Convert.ToInt32, minus one). Shared by load + store.</summary>
    private void EmitTypedElementAddress(ILProcessor il, IrTypedElementLocation e)
    {
        il.Append(il.Create(OpCodes.Ldsfld, _ctx.TypedArrays[e.ArrayFieldName]));
        _ctx.Expression.EmitIrExpression(il, e.Index);   // decimal (1-based subscript)
        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
            typeof(System.Convert).GetMethod("ToInt32", new[] { typeof(decimal) })!)));
        il.Append(il.Create(OpCodes.Ldc_I4_1));
        il.Append(il.Create(OpCodes.Sub));               // 1-based → 0-based
    }

    /// <summary>The element store opcode for an array element by representation: <c>stelem.ref</c> (string) /
    /// <c>stelem.i8</c> (long) / <c>stelem &lt;decimal&gt;</c>.</summary>
    private void EmitArrayStoreElement(ILProcessor il, IrTypedLocation t) =>
        il.Append(t.IsDecimalNumeric
            ? il.Create(OpCodes.Stelem_Any, _ctx.Module.ImportReference(typeof(decimal)))
            : t.Pic.Category == CobolCategory.Numeric
                ? il.Create(OpCodes.Stelem_I8)
                : il.Create(OpCodes.Stelem_Ref));

    /// <summary>The element load opcode for an array element by representation (mirror of <see cref="EmitArrayStoreElement"/>).</summary>
    private void EmitArrayLoadElement(ILProcessor il, IrTypedLocation t) =>
        il.Append(t.IsDecimalNumeric
            ? il.Create(OpCodes.Ldelem_Any, _ctx.Module.ImportReference(typeof(decimal)))
            : t.Pic.Category == CobolCategory.Numeric
                ? il.Create(OpCodes.Ldelem_I8)
                : il.Create(OpCodes.Ldelem_Ref));

    internal void EmitTypedStorePrefix(ILProcessor il, IrTypedLocation t)
    {
        switch (t)
        {
            case IrTypedFieldLocation { InstanceName: { } } f:
                EmitInstanceAddressChain(il, f);   // ldsflda instance; ldflda nested… (leaves leaf's parent addr)
                break;
            case IrTypedFieldLocation:
                break;   // flat static field: no prefix
            case IrTypedElementLocation e:
                EmitTypedElementAddress(il, e);
                break;
        }
    }

    internal void EmitTypedStoreSuffix(ILProcessor il, IrTypedLocation t)
    {
        switch (t)
        {
            case IrTypedFieldLocation { InstanceName: { } } f:
                il.Append(il.Create(OpCodes.Stfld, ResolveLeafField(f)));
                break;
            case IrTypedFieldLocation f:
                il.Append(il.Create(OpCodes.Stsfld, _ctx.TypedFields[f.FieldName]));
                break;
            case IrTypedElementLocation e:
                EmitArrayStoreElement(il, e);
                break;
        }
    }

    internal void EmitTypedLoad(ILProcessor il, IrTypedLocation t)
    {
        switch (t)
        {
            case IrTypedFieldLocation { InstanceName: { } } f:
                EmitInstanceAddressChain(il, f);
                il.Append(il.Create(OpCodes.Ldfld, ResolveLeafField(f)));
                break;
            case IrTypedFieldLocation f:
                il.Append(il.Create(OpCodes.Ldsfld, _ctx.TypedFields[f.FieldName]));
                break;
            case IrTypedElementLocation e:
                EmitTypedElementAddress(il, e);
                EmitArrayLoadElement(il, e);
                break;
        }
    }

    // ── S3b/S5 record-struct member addressing — resolve flat OR nested members by walking the struct FieldTypes ──

    /// <summary>Emits the address chain for a record-struct member: <c>ldsflda &lt;static instance&gt;</c> then
    /// <c>ldflda</c> for each intermediate nested struct in <see cref="IrTypedFieldLocation.MemberPath"/>, leaving
    /// the leaf's PARENT struct address on the stack (for the flat case, just the instance address).</summary>
    private void EmitInstanceAddressChain(ILProcessor il, IrTypedFieldLocation f)
    {
        var instField = _ctx.TypedRecords[f.InstanceName!];
        il.Append(il.Create(OpCodes.Ldsflda, instField));
        var cur = instField.FieldType.Resolve();
        foreach (var m in f.MemberPath)
        {
            var nestedField = cur.Fields.First(x => x.Name == m);
            il.Append(il.Create(OpCodes.Ldflda, nestedField));
            cur = nestedField.FieldType.Resolve();
        }
    }

    /// <summary>Resolves the leaf member <see cref="FieldDefinition"/> by walking the struct's <c>FieldType.Fields</c>
    /// along <see cref="IrTypedFieldLocation.MemberPath"/> (no IL emitted) — the field used by load/store.</summary>
    private FieldDefinition ResolveLeafField(IrTypedFieldLocation f)
    {
        var cur = _ctx.TypedRecords[f.InstanceName!].FieldType.Resolve();
        foreach (var m in f.MemberPath)
            cur = cur.Fields.First(x => x.Name == m).FieldType.Resolve();
        return cur.Fields.First(x => x.Name == f.FieldName);
    }

    /// <summary>
    /// S4: a typed numeric field→field MOVE where at least one side is a <c>decimal</c>. Routes through the
    /// DESTINATION's byte codec — encode the source value into a dst-shaped scratch window (applies the dst's
    /// sign/scale/truncation exactly as the byte <c>MoveNumeric</c> would), decode it back, and store — so it is
    /// byte-identical for every long/decimal combination. (The both-<c>long</c> case keeps its faster mod path.)
    /// </summary>
    private void EmitNumericFieldToFieldViaCodec(ILProcessor il, IrTypedLocation src, IrTypedLocation dst)
    {
        var scratch = new VariableDefinition(_ctx.Module.ImportReference(typeof(byte[])));
        _ctx.CurrentMethodDef!.Body.Variables.Add(scratch);
        il.Append(il.Create(OpCodes.Ldc_I4, dst.Width));
        il.Append(il.Create(OpCodes.Newarr, _ctx.Module.TypeSystem.Byte));
        il.Append(il.Create(OpCodes.Stloc, scratch));
        // EncodeNumeric(scratch, 0, dstWidth, dstPic, srcValue-as-decimal)
        il.Append(il.Create(OpCodes.Ldloc, scratch));
        il.Append(il.Create(OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Ldc_I4, dst.Width));
        _ctx.Expression.EmitLoadPicDescriptor(il, dst.Pic);
        EmitTypedLoad(il, src);
        if (!src.IsDecimalNumeric)   // a long source widens to decimal; a decimal source is already right
            il.Append(il.Create(OpCodes.Newobj, _ctx.Module.ImportReference(
                typeof(decimal).GetConstructor(new[] { typeof(long) })!)));
        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
            typeof(PicRuntime).GetMethod("EncodeNumeric",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor), typeof(decimal) })!)));
        // dst = DecodeNumeric(scratch, 0, dstWidth, dstPic) — stored as decimal, or narrowed to long.
        EmitTypedStorePrefix(il, dst);
        il.Append(il.Create(OpCodes.Ldloc, scratch));
        il.Append(il.Create(OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Ldc_I4, dst.Width));
        _ctx.Expression.EmitLoadPicDescriptor(il, dst.Pic);
        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
            typeof(PicRuntime).GetMethod("DecodeNumeric",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor) })!)));
        if (!dst.IsDecimalNumeric)
            il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
                typeof(decimal).GetMethods().Single(m =>
                    m.Name == "op_Explicit" && m.ReturnType == typeof(long)
                    && m.GetParameters() is { Length: 1 } p && p[0].ParameterType == typeof(decimal)))));
        EmitTypedStoreSuffix(il, dst);
    }

    /// <summary>Emits <c>ldc width; ldc.i4.0; call CobolString.Store(string,int,bool)</c> (the receiving value
    /// already-on-stack: width/justify/space-fill per ISO §14.9.25).</summary>
    private void EmitCobolStringStore(ILProcessor il, int width)
    {
        il.Append(il.Create(OpCodes.Ldc_I4, width));
        il.Append(il.Create(OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
            typeof(CobolSharp.Runtime.Text.CobolString).GetMethod(
                "Store", new[] { typeof(string), typeof(int), typeof(bool) })!)));
    }

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
        // Data-model migration S3: MOVE "literal" TO a typed-native string field — store the receiving value
        // (CobolString.Store: width/justify/space-fill, ISO §14.9.25) directly to the .NET field. No byte window.
        // (A string literal to a typed NUMERIC field is a different conversion — falls through to the loud guard.)
        if (ms.Target is IrTypedLocation tfl && tfl.Pic.Category != CobolCategory.Numeric)
        {
            EmitTypedStorePrefix(il, tfl);
            il.Append(il.Create(OpCodes.Ldstr, ms.Value));
            EmitCobolStringStore(il, tfl.Width);   // justifiedRight: false (S3 widening adds JUSTIFIED)
            EmitTypedStoreSuffix(il, tfl);
            return;
        }

        var pic = ms.Target.GetPic();

        // National targets: encode the literal's characters as UTF-16 and store them left-justified,
        // U+0020-padded / right-truncated (ISO §14.6.8.5). Covers N"…" and the ASCII-subset
        // alphanumeric→national correspondence for a "…" literal moved to a national receiver.
        if (pic.Category.IsNationalLike())
        {
            _ctx.Location.EmitLocationArgsWithPic(il, ms.Target);
            il.Append(il.Create(OpCodes.Ldstr, ms.Value));

            var method = _ctx.Module.ImportReference(
                typeof(PicRuntime).GetMethod(
                    "MoveStringLiteralToNational",
                    new[] { typeof(byte[]), typeof(int), typeof(int),
                            typeof(PicDescriptor), typeof(string) })!);
            il.Append(il.Create(OpCodes.Call, method));
        }
        // Boolean targets: store the literal's '0'/'1' bytes, zero-filled / right-truncated.
        else if (pic.Category.IsBooleanLike())
        {
            _ctx.Location.EmitLocationArgsWithPic(il, ms.Target);
            il.Append(il.Create(OpCodes.Ldstr, ms.Value));

            var method = _ctx.Module.ImportReference(
                typeof(PicRuntime).GetMethod(
                    "MoveStringLiteralToBoolean",
                    new[] { typeof(byte[]), typeof(int), typeof(int),
                            typeof(PicDescriptor), typeof(string) })!);
            il.Append(il.Create(OpCodes.Call, method));
        }
        // Numeric targets: right-justified numeric MOVE (rightmost digits taken)
        else if (pic.Category == CobolCategory.Numeric)
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
        // S3/S4: MOVE a figurative constant to a typed field.
        if (mf.Destination is IrTypedLocation tfl)
        {
            // S4 numeric: MOVE ZEROS → 0 (byte-identical: the byte path zero-fills the digit image, which DISPLAYs
            // identically to a 0-valued long/decimal). A figurative fill STRING must never be stored into a numeric
            // long/decimal field (that mis-emits invalid IL).
            if (tfl.Pic.Category == CobolCategory.Numeric)
            {
                if (mf.FigurativeKind == Runtime.FigurativeKind.Zero)
                {
                    EmitTypedStorePrefix(il, tfl);
                    if (tfl.IsDecimalNumeric)
                        _ctx.Expression.EmitLoadDecimal(il, 0m);
                    else
                        il.Append(il.Create(OpCodes.Ldc_I8, 0L));
                    EmitTypedStoreSuffix(il, tfl);
                    return;
                }
                // SPACE/HIGH-VALUE/LOW-VALUE/QUOTE/NULL into a numeric field is a byte-pattern fill with no native
                // long/decimal equivalent — fail loudly rather than mis-emit (deferred to a materialize cell).
                throw new System.NotSupportedException(
                    $"S4: MOVE {mf.FigurativeKind} to a typed numeric location ({tfl.GetType().Name}) is not " +
                    "supported (only ZEROS has a native equivalent).");
            }
            // S3 character: MOVE SPACES/ZEROS → a width-long fill string (byte-identical: the byte path fills the
            // window with the same byte). Other figuratives (HIGH/LOW-VALUE/QUOTE/NULL) fall through to the byte
            // loud guard until their typed cell lands.
            if (mf.FigurativeKind is Runtime.FigurativeKind.Space or Runtime.FigurativeKind.Zero)
            {
                char fill = mf.FigurativeKind == Runtime.FigurativeKind.Space ? ' ' : '0';
                EmitTypedStorePrefix(il, tfl);
                il.Append(il.Create(OpCodes.Ldc_I4, fill));
                il.Append(il.Create(OpCodes.Ldc_I4, tfl.Width));
                il.Append(il.Create(OpCodes.Newobj, _ctx.Module.ImportReference(
                    typeof(string).GetConstructor(new[] { typeof(char), typeof(int) })!)));
                EmitTypedStoreSuffix(il, tfl);
                return;
            }
        }

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
        // Data-model migration S3: typed↔typed alphanumeric MOVE (both operands flipped to native string
        // fields) — re-store the source value into the destination at its width (CobolString.Store: ISO §14.9.25
        // space-pad/truncate; a ref copy when widths match). A mixed typed/byte pair instead hits the loud
        // EmitLocationArgs guard until the materialize fallback (§2.5) lands.
        if (mf.Source is IrTypedLocation msrc && mf.Destination is IrTypedLocation mdst)
        {
            bool srcNum = msrc.Pic.Category == CobolCategory.Numeric;
            bool dstNum = mdst.Pic.Category == CobolCategory.Numeric;
            if (!srcNum && !dstNum)
            {
                // both typed strings: re-store the source value at the destination width (CobolString.Store:
                // ISO §14.9.25 space-pad/truncate; a ref copy when widths match).
                EmitTypedStorePrefix(il, mdst);
                EmitTypedLoad(il, msrc);
                EmitCobolStringStore(il, mdst.Width);
                EmitTypedStoreSuffix(il, mdst);
                return;
            }
            if (srcNum && dstNum)
            {
                // S4: a decimal field on either end routes through the destination byte codec (encode the source
                // value into a dst-shaped scratch window, decode back, store) so the dst's sign/scale/truncation is
                // applied exactly as the byte MoveNumeric would — byte-identical for every long/decimal combination.
                if (msrc.IsDecimalNumeric || mdst.IsDecimalNumeric)
                {
                    EmitNumericFieldToFieldViaCodec(il, msrc, mdst);
                    return;
                }
                // both typed unsigned-integer `long`s — dst = src truncated to the dst's digit count
                // (src mod 10^n), byte-identical to a numeric→numeric byte MOVE (high-order truncation). Digit
                // count comes from the PIC (TotalDigits), not the byte Width — they differ for COMP/BINARY.
                long mod = 1;
                for (int i = 0; i < mdst.Pic.TotalDigits; i++) mod *= 10;
                EmitTypedStorePrefix(il, mdst);
                EmitTypedLoad(il, msrc);                       // long (≥ 0)
                il.Append(il.Create(OpCodes.Ldc_I8, mod));
                il.Append(il.Create(OpCodes.Rem));             // low `n` digits
                EmitTypedStoreSuffix(il, mdst);
                return;
            }
            // mixed string↔numeric typed move (rare): fall through to the byte dispatch / loud guard.
        }

        // S3: the typed↔byte materialize boundary (§2.5) for field→field MOVE of a typed STRING field. The byte
        // engine is the safety floor — Latin-1 round-trips byte↔char losslessly, so these are byte-identical.
        // (A typed NUMERIC field ↔ byte falls through to the loud guard until its materialize cell lands.)
        if (mf.Destination is IrTypedLocation tDst && tDst.Pic.Category != CobolCategory.Numeric)
        {
            // byte source → typed string dest: read the source window as a Latin-1 string, then Store at width.
            EmitTypedStorePrefix(il, tDst);
            _ctx.Location.EmitLocationArgs(il, mf.Source);   // pushes (area, offset, length)
            il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
                typeof(CobolSharp.Runtime.Text.CobolString).GetMethod(
                    "FromWindow", new[] { typeof(byte[]), typeof(int), typeof(int) })!)));
            EmitCobolStringStore(il, tDst.Width);
            EmitTypedStoreSuffix(il, tDst);
            return;
        }
        if (mf.Source is IrTypedLocation tSrc && tSrc.Pic.Category != CobolCategory.Numeric)
        {
            // typed string source → byte dest: lay the source string into the destination window (StorageHelpers
            // .MoveStringToField — left-justified, space-padded / right-truncated, the same as the byte path).
            _ctx.Location.EmitLocationArgs(il, mf.Destination);   // pushes (area, offset, length)
            EmitTypedLoad(il, tSrc);
            il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
                typeof(StorageHelpers).GetMethod(
                    "MoveStringToField", new[] { typeof(byte[]), typeof(int), typeof(int), typeof(string) })!)));
            return;
        }

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
        // National receiver (ISO §14.6.8.5 / Table 16): copy a national source, widen a numeric source to
        // UTF-16 digits, or widen an alphanumeric/edited source byte-by-byte to UTF-16 — all char-aware so
        // padding/truncation operate on whole national character positions, never single bytes.
        else if (dstCat.IsNationalLike())
        {
            string nat = srcCat.IsNationalLike() ? "MoveNationalToNational"
                       : srcCat == CobolCategory.Numeric ? "MoveNumericToNational"
                       : "MoveAlphanumericToNational";   // Alphanumeric / AlphanumericEdited / NumericEdited bytes
            EmitMoveWithStandardSignature(il, mf.Source, mf.Destination, rounding, nat);
        }
        // National source into an alphanumeric receiver (ISO Table 16): narrow UTF-16 to one byte per
        // character (Latin-1 subset; non-Latin-1 → '?'). National → numeric is not a legal MOVE.
        else if (srcCat.IsNationalLike())
        {
            string nat = dstCat == CobolCategory.AlphanumericEdited
                ? "MoveNationalToAlphanumericEdited" : "MoveNationalToAlphanumeric";
            EmitMoveWithStandardSignature(il, mf.Source, mf.Destination, rounding, nat);
        }
        // Boolean receiver (only boolean←boolean is a legal MOVE, ISO §14.6.8.6): byte-wise with '0' fill.
        else if (dstCat.IsBooleanLike())
        {
            EmitMoveWithStandardSignature(il, mf.Source, mf.Destination, rounding, "MoveBooleanToBoolean");
        }
        // Pointer receiver (SET p TO q lowered as MOVE): an 8-byte opaque-handle copy. Both operands are
        // 8 bytes, so the alphanumeric byte-copy moves the whole handle (no padding/truncation).
        else if (dstCat.IsPointerLike())
        {
            EmitMoveWithStandardSignature(il, mf.Source, mf.Destination, rounding, "MoveAlphanumericToAlphanumeric");
        }
        else
        {
            // Alphanumeric MOVE: left-justified, space-padded (handles JUSTIFIED RIGHT)
            EmitMoveWithStandardSignature(il, mf.Source, mf.Destination, rounding, "MoveAlphanumericToAlphanumeric");
        }
    }

    internal void EmitPicMoveLiteralNumeric(ILProcessor il, IrPicMoveLiteralNumeric mv)
    {
        // S4: MOVE numeric-literal → a typed (unsigned-integer) `long` field. Truncate the literal to the field's
        // digit count at compile time (|value| mod 10^n — byte-identical to the byte path's EncodeNumeric) and
        // store the long. MOVE never carries ROUNDED, so mv.Rounding is 0 here.
        if (mv.Destination is IrTypedLocation tnum && tnum.Pic.Category == CobolCategory.Numeric)
        {
            EmitTypedStorePrefix(il, tnum);
            if (tnum.IsDecimalNumeric)
            {
                // S4 decimal: round-trip the literal through the destination byte codec at COMPILE time
                // (Encode→Decode) to get the exact stored value — sign + implied-decimal scale + truncation all
                // applied identically to the byte path, so it is byte-identical by construction.
                var scratch = new byte[tnum.Width];
                Runtime.PicRuntime.EncodeNumeric(scratch, 0, tnum.Width, tnum.Pic, mv.Value);
                decimal storedDec = Runtime.PicRuntime.DecodeNumeric(scratch, 0, tnum.Width, tnum.Pic);
                _ctx.Expression.EmitLoadDecimal(il, storedDec);
            }
            else
            {
                decimal mod = 1m;
                for (int i = 0; i < tnum.Pic.TotalDigits; i++) mod *= 10m;   // digit count, not byte Width (COMP differs)
                long stored = (long)(System.Math.Truncate(System.Math.Abs(mv.Value)) % mod);
                il.Append(il.Create(OpCodes.Ldc_I8, stored));
            }
            EmitTypedStoreSuffix(il, tnum);
            return;
        }

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

        // DISPLAY … WITH NO ADVANCING (§14.9.11) suppresses the line terminator → Console.Write.
        var consoleMethod = _ctx.Module.ImportReference(
            typeof(Console).GetMethod(disp.NoAdvancing ? "Write" : "WriteLine", new[] { typeof(string) })!);
        il.Append(il.Create(OpCodes.Call, consoleMethod));
    }

    internal void EmitDisplayOperand(ILProcessor il, DisplayOperand operand)
    {
        if (operand is DisplayLiteralOperand lit)
        {
            il.Append(il.Create(OpCodes.Ldstr, lit.Value));
        }
        else if (operand is DisplayFieldOperand field)
        {
            // Data-model migration S3: DISPLAY of a typed-native string field — push the .NET string directly
            // (it IS the field's character image, space-padded to width); no GetDisplayString byte decode.
            if (field.Location is IrTypedLocation tfl)
            {
                // S4: a typed numeric (`long`) field → format its digit image (CobolNum.FormatUnsignedDisplay),
                // byte-identical to the byte path's stored DISPLAY bytes for an unsigned PIC 9(n).
                if (tfl.Pic.Category == CobolCategory.Numeric)
                {
                    // S4 decimal (signed/scaled): materialize to the field's byte image and format via the EXACT
                    // byte-path formatter (GetDisplayString) — byte-identical, and it handles sign overpunch /
                    // implied decimal scaling that the unsigned-integer fast path (FormatUnsignedDisplay) does not.
                    if (tfl.IsDecimalNumeric)
                    {
                        _ctx.Location.EmitLocationArgsWithPicMaterializingTyped(il, tfl);
                        il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
                            typeof(PicRuntime).GetMethod("GetDisplayString",
                                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor) })!)));
                        return;
                    }
                    EmitTypedLoad(il, tfl);                       // long value
                    il.Append(il.Create(OpCodes.Ldc_I4, tfl.Pic.TotalDigits));   // digit count, not byte Width
                    il.Append(il.Create(OpCodes.Call, _ctx.Module.ImportReference(
                        typeof(CobolSharp.Runtime.Numeric.CobolNum).GetMethod(
                            "FormatUnsignedDisplay", new[] { typeof(long), typeof(int) })!)));
                    return;
                }
                // S3: a typed string field. Match the byte path exactly (GetDisplayString, alphanumeric arm):
                // trailing spaces are trimmed (.TrimEnd()) so the typed flip is byte-identical to the byte DISPLAY.
                EmitTypedLoad(il, tfl);
                var trimEnd = _ctx.Module.ImportReference(
                    typeof(string).GetMethod("TrimEnd", System.Type.EmptyTypes)!);
                il.Append(il.Create(OpCodes.Callvirt, trimEnd));
                return;
            }

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

    internal MethodReference GetManagedPointerCtor()
    {
        _ctx.ManagedPointerCtor ??= _ctx.Module.ImportReference(
            typeof(ManagedPointer).GetConstructor(
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor) })!);
        return _ctx.ManagedPointerCtor;
    }

    internal void EmitOptionalString(ILProcessor il, string? value)
    {
        if (value != null)
            il.Append(il.Create(OpCodes.Ldstr, value));
        else
            il.Append(il.Create(OpCodes.Ldnull));
    }
}
