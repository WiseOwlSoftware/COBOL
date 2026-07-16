// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// One shared, aliasable character-storage cell (the Tier-B string-canonical backing lifted onto the heap —
/// never a byte substrate): EXTERNAL records, ADDRESS-OF-taken items, and ALLOCATEd areas all live in one of
/// these; <see cref="Ref"/> is a FIELD so the generated <c>ref</c>-returning bridge property can alias it.
/// </summary>
public sealed class StorageCell
{
    /// <summary>The storage's character image (its full width; every view windows it).</summary>
    public string Ref = "";

    /// <summary>True for a cell obtained by ALLOCATE (ISO §14.9.3) — the only cells FREE releases (GR1a).</summary>
    public bool Allocated;

    /// <summary>True once FREE released the cell (§14.9.15 GR1a — "the contents become undefined"; this
    /// implementation makes any later dereference loud, EC-BOUND-PTR).</summary>
    public bool Freed;
}
