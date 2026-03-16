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
/// Factory for creating PicDescriptor from compiler DataSymbol.
/// </summary>
public static class PicDescriptorFactory
{
    public static PicDescriptor FromDataSymbol(DataSymbol symbol, int storageLength)
    {
        var pic = symbol.ResolvedType?.Pic;
        var category = symbol.ResolvedType?.Category ?? CobolCategory.Alphanumeric;

        // Determine editing kind from PIC analysis
        var editingKind = EditingKind.None;
        if (pic?.IsEdited ?? false)
            editingKind = EditingKind.ZeroSuppress; // Default; refined later

        // Parameter order matches the single PicDescriptor constructor
        return new PicDescriptor(
            totalDigits: (pic?.IntegerDigits ?? 0) + (pic?.FractionDigits ?? 0),
            fractionDigits: pic?.FractionDigits ?? 0,
            isSigned: pic?.IsSigned ?? false,
            isNumeric: category.IsNumericLike(),
            isAlphanumeric: category.IsAlphanumericLike(),
            hasEditing: pic?.IsEdited ?? false,
            storageLength: storageLength,
            usage: symbol.Usage,
            category: category,
            signStorage: DetermineSignStorage(pic?.IsSigned ?? false, symbol),
            editing: editingKind,
            blankWhenZero: false,
            leadingScaleDigits: pic?.LeadingPScaling ?? 0,
            trailingScaleDigits: pic?.TrailingPScaling ?? 0,
            editPattern: (pic?.IsEdited ?? false) ? ExpandEditPattern(symbol.PicString!) : null);
    }

    /// <summary>
    /// Expand PIC repeat notation: "9(5)" → "99999", "-9(9).9(9)" → "-999999999.999999999".
    /// The EditPattern must be the expanded form for FormatByEditPattern to walk character-by-character.
    /// </summary>
    private static string ExpandEditPattern(string pic)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < pic.Length; i++)
        {
            char c = pic[i];
            if (i + 1 < pic.Length && pic[i + 1] == '(')
            {
                // Parse repeat count: c(n) or c(nn)
                int start = i + 2;
                int end = start;
                while (end < pic.Length && char.IsDigit(pic[end])) end++;
                if (end < pic.Length && pic[end] == ')' && end > start)
                {
                    int count = int.Parse(pic.Substring(start, end - start));
                    for (int k = 0; k < count; k++)
                        sb.Append(c);
                    i = end; // skip past ')'
                }
                else
                {
                    sb.Append(c);
                }
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    private static SignStorageKind DetermineSignStorage(bool isSigned, DataSymbol symbol)
    {
        if (!isSigned) return SignStorageKind.None;

        // Explicit SIGN clause takes priority
        if (symbol.ExplicitSignStorage.HasValue)
            return symbol.ExplicitSignStorage.Value;

        // COBOL default: SIGN IS TRAILING OVERPUNCH (sign encoded in last digit)
        return SignStorageKind.TrailingOverpunch;
    }
}
