// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The run-unit EXTERNAL data store (ISO §8.6.7 / §13.18.22): ONE storage copy per external name for the whole
/// run unit, represented as the record's character image (the same Tier-B string-canonical shape the data model
/// uses for shared storage — never a persisted byte substrate). Every program describing the same external name
/// windows the same cell; CANCEL does NOT reset it (§14.9.5 GR8). The §13.18.22 GR6 conformance checks
/// (same byte count / same VALUE across describers) belong to the §14.8.4 EC machinery — not enforced here yet.
/// </summary>
public static class ExternalStore
{
    private static readonly Dictionary<string, StorageCell> Cells = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The run-unit cell for <paramref name="name"/>, created with <paramref name="initialImage"/> on
    /// first reference (ISO §14.6.2.3.2 — external data takes its initial state once per run unit). The cell
    /// is the ONE shared-storage shape (<see cref="StorageCell"/> — increment-2 unification), so ADDRESS OF an
    /// EXTERNAL item needs no special case.</summary>
    public static StorageCell Cell(string name, string initialImage)
    {
        if (!Cells.TryGetValue(name, out var h)) Cells[name] = h = new StorageCell { Ref = initialImage };
        return h;
    }

    /// <summary>Drop every cell (run-unit start hygiene; called from <see cref="ProgramRegistry.Reset"/>).</summary>
    public static void Reset() => Cells.Clear();
}
