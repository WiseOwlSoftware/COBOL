// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Runtime;

/// <summary>
/// Identifies COBOL figurative constants (ZERO, SPACE, HIGH-VALUE, etc.).
/// Used by MoveFigurativeToField to fill an entire destination field
/// with the appropriate byte value.
/// </summary>
public enum FigurativeKind
{
    /// <summary>Not a figurative constant.</summary>
    None = 0,
    /// <summary>ZERO / ZEROS / ZEROES — fills with '0' (0x30) for alphanumeric, numeric zero for numeric.</summary>
    Zero,
    /// <summary>SPACE / SPACES — fills with ' ' (0x20).</summary>
    Space,
    /// <summary>HIGH-VALUE / HIGH-VALUES — fills with 0xFF (highest value in collating sequence).</summary>
    HighValue,
    /// <summary>LOW-VALUE / LOW-VALUES — fills with 0x00 (lowest value in collating sequence).</summary>
    LowValue,
    /// <summary>QUOTE / QUOTES — fills with '"' (0x22).</summary>
    Quote,
    /// <summary>NULL / NULLS — fills with 0x00 (address context, semantically distinct from LOW-VALUE).</summary>
    Null
}
