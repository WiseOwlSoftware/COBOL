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
        int line,
        bool blankWhenZero = false,
        PicEnvironment? environment = null)
    {
        PicLayout? layout = null;

        if (picString != null)
        {
            layout = ParsePic(picString, diagnostics, line, blankWhenZero, environment);
        }

        var category = layout?.Category ?? CobolCategory.Unknown;
        bool isNumeric = category.IsNumericLike();
        bool isAlpha = category.IsAlphanumericLike();
        bool isBool = false;

        // COMP-1 (single float) and COMP-2 (double float) have no PIC clause but are numeric
        if (picString == null && usage is UsageKind.Comp1 or UsageKind.Comp2)
        {
            isNumeric = true;
            category = CobolCategory.Numeric;
        }
        // Group items (no PIC) are alphanumeric by default
        else if (picString == null && usage == UsageKind.Display)
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
    private static PicLayout ParsePic(string picString, DiagnosticBag diagnostics, int line,
        bool blankWhenZero = false, PicEnvironment? environment = null)
    {
        // Per ISO §13.18.52, signed DISPLAY numerics default to trailing overpunch
        // when no explicit SIGN clause is present. Detect 'S' in the PIC body upfront.
        bool bodySigned = picString.IndexOf('S', StringComparison.OrdinalIgnoreCase) >= 0;
        var signStorage = bodySigned ? SignStorageKind.TrailingOverpunch : SignStorageKind.None;

        var desc = Runtime.PicDescriptorFactory.FromPicBody(
            picString.Trim(),
            usage: UsageKind.Display,
            isSigned: false,               // S in the body will flip this
            signStorage: signStorage,
            blankWhenZero: blankWhenZero,
            environment: environment);

        return new PicLayout(
            Category: desc.Category,
            Length: desc.StorageLength,
            IntegerDigits: desc.TotalDigits - desc.FractionDigits,
            FractionDigits: desc.FractionDigits,
            LeadingPScaling: desc.LeadingScaleDigits,
            TrailingPScaling: desc.TrailingScaleDigits,
            IsSigned: desc.IsSigned,
            IsEdited: desc.HasEditing,
            BlankWhenZero: desc.BlankWhenZero);
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
            "COMP-5" or "COMPUTATIONAL-5" => UsageKind.Comp5,
            "BINARY" => UsageKind.Binary,
            "PACKED-DECIMAL" => UsageKind.PackedDecimal,
            "INDEX" => UsageKind.Index,
            "POINTER" => UsageKind.Pointer,
            _ => UsageKind.Object
        };
    }
}
