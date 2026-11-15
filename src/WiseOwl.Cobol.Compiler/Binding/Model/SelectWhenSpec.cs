// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;

namespace WiseOwl.Cobol.Binding.Model;

/// <summary>The SELECT WHEN clause of one record description entry, as written (ISO §2023:13.18.51.2:
/// <c>SELECT WHEN { condition-name-1 | OTHER }</c>; ISO/IEC 1989:2002 §13.16.49.1): the condition under which the entry is
/// selected for a record during input and output operations (§2023:13.18.51.4 GR3).
/// <para>The condition-name is a name; it is resolved to its <see cref="Condition88"/> by <c>ResolveRecordFormatPass</c>, after every
/// level-88 entry exists, and screened there against SR2 - SR6.</para></summary>
public sealed class SelectWhenSpec
{
    /// <summary>condition-name-1 as written, or null for the OTHER phrase (always true, GR3).</summary>
    public string? ConditionName { get; init; }

    /// <summary>The OTHER phrase.</summary>
    public bool IsOther => ConditionName is null;

    /// <summary>Where the clause is written, for the screens that refuse it.</summary>
    public DiagnosticCursor At { get; init; }

    /// <summary>The condition-name's level-88 entry — null for OTHER, and for a name that did not resolve (already reported).</summary>
    public Condition88? Condition { get; set; }
}
