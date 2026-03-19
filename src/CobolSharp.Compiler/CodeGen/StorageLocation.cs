// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Semantics;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.CodeGen;

public enum StorageAreaKind
{
    WorkingStorage,
    FileSection,
}

/// <summary>
/// Maps a DataSymbol to its backing byte slice in ProgramState.
/// Uses the shared PicDescriptor from CobolSharp.Runtime.
/// </summary>
public readonly struct StorageLocation
{
    public StorageAreaKind Area { get; }
    public int Offset { get; }
    public int Length { get; }
    public PicDescriptor Pic { get; }

    public StorageLocation(StorageAreaKind area, int offset, int length, PicDescriptor pic)
    {
        Area = area;
        Offset = offset;
        Length = length;
        Pic = pic;
    }
}

/// <summary>
/// Compiler-side bridge: creates PicDescriptor from DataSymbol using the canonical
/// Runtime.PicDescriptorFactory for PIC parsing, plus compiler-specific context
/// (ExplicitSignStorage, Usage from data description).
/// </summary>
public static class CompilerPicDescriptorFactory
{
    public static PicDescriptor FromDataSymbol(DataSymbol symbol, int storageLength)
    {
        var pic = symbol.ResolvedType?.Pic;
        bool isSigned = pic?.IsSigned ?? false;
        var signStorage = DetermineSignStorage(isSigned, symbol);

        // Single pipeline: all PIC semantics come from the canonical runtime factory.
        // The compiler only overlays storage length (from layout) and sign storage
        // (from explicit SIGN clause in data description).
        if (symbol.PicString != null)
        {
            bool blankWhenZero = pic?.BlankWhenZero ?? false;
            var desc = Runtime.PicDescriptorFactory.FromPicBody(
                symbol.PicString,
                usage: symbol.Usage,
                isSigned: isSigned,
                signStorage: signStorage,
                blankWhenZero: blankWhenZero);

            return new PicDescriptor(
                totalDigits: desc.TotalDigits,
                fractionDigits: desc.FractionDigits,
                isSigned: desc.IsSigned,
                isNumeric: desc.IsNumeric,
                isAlphanumeric: desc.IsAlphanumeric,
                hasEditing: desc.HasEditing,
                storageLength: storageLength,
                usage: symbol.Usage,
                category: desc.Category,
                signStorage: signStorage,
                editing: desc.Editing,
                blankWhenZero: desc.BlankWhenZero,
                leadingScaleDigits: desc.LeadingScaleDigits,
                trailingScaleDigits: desc.TrailingScaleDigits,
                editPattern: desc.EditPattern,
                isJustifiedRight: symbol.IsJustifiedRight);
        }

        // Group items (no PIC): alphanumeric DISPLAY
        var category = symbol.ResolvedType?.Category ?? CobolCategory.Alphanumeric;
        return new PicDescriptor(
            totalDigits: 0,
            fractionDigits: 0,
            isSigned: false,
            isNumeric: false,
            isAlphanumeric: true,
            hasEditing: false,
            storageLength: storageLength,
            usage: symbol.Usage,
            category: category,
            signStorage: SignStorageKind.None,
            editing: EditingKind.None,
            blankWhenZero: false,
            leadingScaleDigits: 0,
            trailingScaleDigits: 0,
            editPattern: null) { IsGroup = true };
    }

    private static SignStorageKind DetermineSignStorage(bool isSigned, DataSymbol symbol)
    {
        if (!isSigned) return SignStorageKind.None;

        if (symbol.ExplicitSignStorage.HasValue)
            return symbol.ExplicitSignStorage.Value;

        return SignStorageKind.TrailingOverpunch;
    }
}
