// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Runtime;

/// <summary>
/// Program-level PIC formatting/collation environment.
/// Represents SPECIAL-NAMES settings that affect PIC string interpretation
/// and numeric-edited field formatting.
///
/// Every PicDescriptor carries a reference to the environment it was parsed under,
/// making each descriptor self-contained for runtime formatting.
/// </summary>
public sealed record PicEnvironment(char CurrencySign = '$', bool DecimalPointIsComma = false)
{
    /// <summary>Default environment: $ currency, . decimal point.</summary>
    public static readonly PicEnvironment Default = new();
}
