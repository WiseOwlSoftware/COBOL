// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The static facade over the run unit's <see cref="ExternalTable"/> (the emitted surface — generated programs
/// call <c>ExternalStore.Cell(name, image)</c>; kept name-stable pre-G8). Forwards to
/// <c>RunUnit.Current.External</c>.
/// </summary>
public static class ExternalStore
{
    /// <inheritdoc cref="ExternalTable.Cell"/>
    public static StorageCell Cell(string name, string initialImage) => RunUnit.Current.External.Cell(name, initialImage);

    /// <inheritdoc cref="ExternalTable.Reset"/>
    public static void Reset() => RunUnit.Current.External.Reset();
}
