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
    None = 0,
    Zero,
    Space,
    HighValue,
    LowValue,
    Quote,
    Null
}
