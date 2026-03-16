// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Runtime;
using CobolSharp.Compiler.Diagnostics;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Resolves a data item's PIC string + USAGE clause into a concrete ITypeSymbol.
/// Called by the binder when creating DataSymbols.
/// </summary>
public static class PicUsageResolver
{
    public static ITypeSymbol ResolveForDataItem(
        string dataName,
        string? picString,
        UsageKind usage,
        DiagnosticBag diagnostics,
        int line)
    {
        PicLayout? layout = null;

        if (picString != null)
        {
            layout = ParsePic(picString, diagnostics, line);
        }

        var category = layout?.Category ?? CobolCategory.Unknown;
        bool isNumeric = category.IsNumericLike();
        bool isAlpha = category.IsAlphanumericLike();
        bool isBool = false;

        // Group items (no PIC) are alphanumeric by default
        if (picString == null && usage == UsageKind.Display)
        {
            isAlpha = true;
            category = CobolCategory.Alphanumeric;
        }

        var name = BuildTypeName(dataName, layout, usage);
        return new DataTypeSymbol(name, isNumeric, isAlpha, isBool, layout, usage);
    }

    private static string BuildTypeName(string dataName, PicLayout? pic, UsageKind usage)
    {
        if (pic == null)
            return $"{dataName}:{usage}";
        return $"{dataName}:PIC({pic.Category},{pic.Length})/{usage}";
    }

    /// <summary>
    /// Parse a PIC string into a PicLayout using the canonical Runtime.PicDescriptorFactory.
    /// Single pipeline: all PIC semantics are defined by PicDescriptorFactory.FromPicBody.
    /// PicLayout is a thin view for the compiler's type system.
    /// </summary>
    private static PicLayout ParsePic(string picString, DiagnosticBag diagnostics, int line)
    {
        var desc = Runtime.PicDescriptorFactory.FromPicBody(
            picString.Trim(),
            usage: UsageKind.Display,
            isSigned: false,               // S in the body will flip this
            signStorage: SignStorageKind.None,
            blankWhenZero: false);

        return new PicLayout(
            category: desc.Category,
            length: desc.StorageLength,
            integerDigits: desc.TotalDigits - desc.FractionDigits,
            fractionDigits: desc.FractionDigits,
            leadingPScaling: desc.LeadingScaleDigits,
            trailingPScaling: desc.TrailingScaleDigits,
            isSigned: desc.IsSigned,
            isEdited: desc.HasEditing);
    }
}

/// <summary>
/// Maps USAGE keyword text to UsageKind enum.
/// </summary>
public static class UsageMapper
{
    public static UsageKind FromUsageKeyword(string? keyword)
    {
        if (keyword == null)
            return UsageKind.Display;

        return keyword.ToUpperInvariant() switch
        {
            "DISPLAY" => UsageKind.Display,
            "COMP" or "COMPUTATIONAL" => UsageKind.Comp,
            "COMP-1" or "COMPUTATIONAL-1" => UsageKind.Comp1,
            "COMP-2" or "COMPUTATIONAL-2" => UsageKind.Comp2,
            "COMP-3" or "COMPUTATIONAL-3" => UsageKind.Comp3,
            "BINARY" => UsageKind.Binary,
            "PACKED-DECIMAL" => UsageKind.PackedDecimal,
            "INDEX" => UsageKind.Index,
            "POINTER" => UsageKind.Pointer,
            _ => UsageKind.Object
        };
    }
}
