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
        var category = symbol.ResolvedType?.Category ?? CobolCategory.Alphanumeric;
        bool isSigned = pic?.IsSigned ?? false;
        var signStorage = DetermineSignStorage(isSigned, symbol);

        // For any DISPLAY numeric field with a PIC string, use the canonical runtime factory.
        // This ensures PicDescriptor semantics (digits, fractions, sign, editing, pattern)
        // are identical between compiler and runtime — no divergence between PicLayout and factory.
        if (symbol.PicString != null && category.IsNumericLike() &&
            symbol.Usage == UsageKind.Display)
        {
            var desc = Runtime.PicDescriptorFactory.FromPicBody(
                symbol.PicString,
                usage: symbol.Usage,
                isSigned: isSigned,
                signStorage: signStorage,
                blankWhenZero: false);

            // Override storageLength from layout (compiler knows the actual allocation)
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
                blankWhenZero: false,
                leadingScaleDigits: desc.LeadingScaleDigits,
                trailingScaleDigits: desc.TrailingScaleDigits,
                editPattern: desc.EditPattern);
        }

        // Non-DISPLAY or non-numeric: use compiler's PicLayout directly
        var editingKind = EditingKind.None;
        if (pic?.IsEdited ?? false)
            editingKind = EditingKind.ZeroSuppress;

        return new PicDescriptor(
            totalDigits: (pic?.IntegerDigits ?? 0) + (pic?.FractionDigits ?? 0),
            fractionDigits: pic?.FractionDigits ?? 0,
            isSigned: isSigned,
            isNumeric: category.IsNumericLike(),
            isAlphanumeric: category.IsAlphanumericLike(),
            hasEditing: pic?.IsEdited ?? false,
            storageLength: storageLength,
            usage: symbol.Usage,
            category: category,
            signStorage: signStorage,
            editing: editingKind,
            blankWhenZero: false,
            leadingScaleDigits: pic?.LeadingPScaling ?? 0,
            trailingScaleDigits: pic?.TrailingPScaling ?? 0,
            editPattern: null);
    }

    private static SignStorageKind DetermineSignStorage(bool isSigned, DataSymbol symbol)
    {
        if (!isSigned) return SignStorageKind.None;

        if (symbol.ExplicitSignStorage.HasValue)
            return symbol.ExplicitSignStorage.Value;

        return SignStorageKind.TrailingOverpunch;
    }
}
