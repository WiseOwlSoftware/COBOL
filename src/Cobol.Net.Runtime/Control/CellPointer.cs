// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// A data-pointer VALUE: a byte(character)-granular window position inside one <see cref="StorageCell"/>
/// (ISO §8.5.2.6 — a data pointer identifies a storage address; §14.9.39 Format 10 moves it by bytes).
/// Structural equality via <see cref="ManagedPointer.SameTarget"/>: same cell, same offset.
/// </summary>
public sealed class CellPointer(StorageCell cell, long offset) : ManagedPointer
{
    /// <summary>The addressed storage cell.</summary>
    public StorageCell Cell { get; } = cell;

    /// <summary>The character-position offset into <see cref="Cell"/> (0-based; byte = character in the
    /// alphanumeric/zoned character model).</summary>
    public long Offset { get; } = offset;
}
