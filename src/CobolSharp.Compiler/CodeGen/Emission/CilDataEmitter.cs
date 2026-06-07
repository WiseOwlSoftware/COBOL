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

    // ── Data-model migration S3 typed-field access helpers (docs/RECORD_STRUCT_STORAGE_DESIGN.md) ──
    // A typed field is either a flat static string field (S3a, InstanceName null) or a member of a static
    // record-struct instance (S3b). Store needs the instance address pushed BEFORE the value, so store is split
    // into a prefix (push the struct address, if any) and a suffix (stfld member / stsfld flat).

    private void EmitTypedStorePrefix(ILProcessor il, IrTypedFieldLocation t)
    {
        if (t.InstanceName is { } inst)
            il.Append(il.Create(OpCodes.Ldsflda, _ctx.TypedRecords[inst].Instance));
    }

    private void EmitTypedStoreSuffix(ILProcessor il, IrTypedFieldLocation t)
    {
        il.Append(t.InstanceName is { } inst
            ? il.Create(OpCodes.Stfld, _ctx.TypedRecords[inst].Members[t.FieldName])
            : il.Create(OpCodes.Stsfld, _ctx.TypedFields[t.FieldName]));
    }

    private void EmitTypedLoad(ILProcessor il, IrTypedFieldLocation t)
    {
        if (t.InstanceName is { } inst)
        {
            var rec = _ctx.TypedRecords[inst];
            il.Append(il.Create(OpCodes.Ldsflda, rec.Instance));
            il.Append(il.Create(OpCodes.Ldfld, rec.Members[t.FieldName]));
        }
        else
        {
            il.Append(il.Create(OpCodes.Ldsfld, _ctx.TypedFields[t.FieldName]));
        }
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
        if (ms.Target is IrTypedFieldLocation tfl && tfl.Pic.Category != CobolCategory.Numeric)
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
        // S3: MOVE SPACES/ZEROS to a typed field — store a width-long fill string (byte-identical: the byte path
        // fills the window with the same byte). Other figuratives (HIGH/LOW-VALUE/QUOTE/NULL) keep the byte path
        // for now — on a typed field they hit the loud EmitLocationArgs guard until their typed cell lands.
        if (mf.Destination is IrTypedFieldLocation tfl
            && mf.FigurativeKind is Runtime.FigurativeKind.Space or Runtime.FigurativeKind.Zero)
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
        if (mf.Source is IrTypedFieldLocation msrc && mf.Destination is IrTypedFieldLocation mdst)
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
                // S4: a decimal field on either end needs the dst-codec encode/decode round-trip (exact
                // scale/truncation), not the long fast path — not yet implemented, so fail loudly rather than
                // mis-emit a long where a decimal is expected.
                if (msrc.IsDecimalNumeric || mdst.IsDecimalNumeric)
                    throw new System.NotSupportedException(
                        "S4: typed field→field MOVE involving a decimal numeric field is not yet implemented " +
                        "(needs the dst-codec encode/decode round-trip) — RECORD_STRUCT_STORAGE_DESIGN.md.");
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
        if (mf.Destination is IrTypedFieldLocation tDst && tDst.Pic.Category != CobolCategory.Numeric)
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
        if (mf.Source is IrTypedFieldLocation tSrc && tSrc.Pic.Category != CobolCategory.Numeric)
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
        if (mv.Destination is IrTypedFieldLocation tnum && tnum.Pic.Category == CobolCategory.Numeric)
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
            if (field.Location is IrTypedFieldLocation tfl)
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
