// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Single source of truth for COBOL elementary-item byte sizes.
/// Called by both <see cref="StorageLayoutComputer"/> (semantic pass) and
/// <see cref="CobolSharp.Compiler.CodeGen.RecordLayoutBuilder"/> (IR pass)
/// to ensure consistent sizing across the pipeline.
/// </summary>
public static class FieldSizeCalculator
{
    /// <summary>
    /// Computes the byte size of one element of <paramref name="data"/> before
    /// OCCURS multiplication. Dispatches on USAGE to select the appropriate
    /// size formula (DISPLAY, COMP, COMP-3, etc.).
    /// </summary>
    public static int ComputeElementSize(DataSymbol data)
    {
        // USAGE INDEX elementary items have no PIC — store occurrence number as 4-byte binary
        // (same layout as INDEXED BY items which are S9(9) COMP = 4 bytes)
        if (data.Usage == UsageKind.Index)
            return 4;

        var pic = data.ResolvedType?.Pic;
        if (pic == null || pic.Length <= 0) return 1;

        int totalDigits = pic.IntegerDigits + pic.FractionDigits;

        return data.Usage switch
        {
            UsageKind.Comp or UsageKind.Binary or UsageKind.Comp5 => ComputeBinarySize(totalDigits),
            UsageKind.Comp3 or UsageKind.PackedDecimal => (totalDigits + 2) / 2,
            UsageKind.Comp1 => 4,
            UsageKind.Comp2 => 8,
            _ => ComputeDisplaySize(data, pic)
        };
    }

    /// <summary>
    /// Compute binary storage size from digit count: 1-4 digits -> 2 bytes, 5-9 -> 4, 10-18 -> 8.
    /// </summary>
    public static int ComputeBinarySize(int digits) => digits switch
    {
        <= 4 => 2,
        <= 9 => 4,
        _ => 8
    };

    /// <summary>
    /// DISPLAY items: PIC length in bytes, plus one byte if SIGN IS SEPARATE.
    /// </summary>
    private static int ComputeDisplaySize(DataSymbol data, PicLayout pic)
    {
        bool separateSign = data.ExplicitSignStorage is SignStorageKind.LeadingSeparate
            or SignStorageKind.TrailingSeparate;
        int signBytes = separateSign ? 1 : 0;
        return pic.Length + signBytes;
    }
}
