// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>⛔ THE NATIVE COLLATING SEQUENCE'S EXTREMES — the ONE answer to "which character is HIGH-VALUE / LOW-VALUE
/// in the native sequence", read by every site that answers it (the compiler's figurative fill, the SPECIAL-NAMES
/// HIGH-VALUE, the collating-table extremes, the runtime collation defaults, the no-PCS initial fill, the storage
/// byte law below). ISO/IEC 1989:2023 §2023:8.3.3.6.4 GR6: <i>"the high-value format represents the character, or
/// multiple-character combination, that has the highest ordinal position in the runtime collating sequence"</i>;
/// GR7 the same for LOW-VALUE at the lowest. The native alphanumeric and national character sets are the 65,536
/// UTF-16 code units in code-unit order (implementor items 31/188, D-N1/D-N3), so both classes answer
/// <b>U+FFFF / U+0000</b> — owner decision kb/Work R52 (PB1093 option A). Before it the alphanumeric arm answered
/// U+00FF, which left U+0100..U+FFFF ordered ABOVE "the highest character".</summary>
public static class NativeCollatingSequence
{
    /// <summary>HIGH-VALUE of the native alphanumeric AND national sequences — U+FFFF, the highest code unit.</summary>
    public const char HighValue = '￿';

    /// <summary>LOW-VALUE of the native alphanumeric AND national sequences — U+0000, the lowest code unit.</summary>
    public const char LowValue = '\0';
}
