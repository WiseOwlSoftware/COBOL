// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// The representation a data item gets in the .NET-native data model
/// (<c>docs/DATA_MODEL_ARCHITECTURE.md</c> §3).
/// </summary>
internal enum RepresentationKind
{
    /// <summary>Typed-native: the item maps to a native .NET value/field (the default).</summary>
    Typed,
    /// <summary>A byte-island: the item keeps a byte image because the COBOL semantics observe its bytes
    /// (REDEFINES/RENAMES type-puns, file records, pointers, CALL-aliased / LINKAGE storage, etc.).</summary>
    ByteIsland,
}

/// <summary>The per-item representation map produced by <see cref="RecordClassificationPass"/>.</summary>
internal sealed class RecordClassification(IReadOnlyDictionary<DataSymbol, RepresentationKind> representations)
{
    private readonly IReadOnlyDictionary<DataSymbol, RepresentationKind> _rep = representations;

    /// <summary>The representation assigned to <paramref name="item"/> (defaults to byte-island if unseen — the
    /// conservative "any doubt → byte" default).</summary>
    public RepresentationKind Get(DataSymbol item) =>
        _rep.TryGetValue(item, out var r) ? r : RepresentationKind.ByteIsland;

    public bool IsByteIsland(DataSymbol item) => Get(item) == RepresentationKind.ByteIsland;
    public bool IsTyped(DataSymbol item) => Get(item) == RepresentationKind.Typed;

    /// <summary>Count of items classified as a byte-island (for assertions / diagnostics).</summary>
    public int ByteIslandCount => _rep.Values.Count(r => r == RepresentationKind.ByteIsland);
}

/// <summary>
/// Assigns each data item a <see cref="RepresentationKind"/> (ADR §3). The default is
/// <see cref="RepresentationKind.Typed"/>; an item (transitively, with its REDEFINES class and subordinates)
/// is demoted to a <see cref="RepresentationKind.ByteIsland"/> when the COBOL semantics genuinely observe its
/// bytes. The pass is conservative and monotone: representations only move typed → byte, never back, so the
/// propagation fixpoint terminates (lattice height 1).
///
/// <para><b>Scope — Phase A (data-division triggers) + structural fixpoint.</b> This implements the triggers
/// observable from the data division alone, plus REDEFINES-class closure and downward (island-membership)
/// transitivity (ADR §3): REDEFINES (1), RENAMES/66 (2), FD/SD file records (5), IS EXTERNAL / IS GLOBAL (8),
/// LINKAGE-SECTION items (12), and edited items (13). The <b>procedure-division</b> triggers — reference
/// modification of a numeric-DISPLAY item (3), group MOVE/COMPARE/class-condition (4), CALL … USING BY
/// REFERENCE arguments (11), the ODO-whole-group operand (15), and write-pattern items (14) — require a
/// bound-tree scan (Phase B) and the cross-edge fixpoint (Phase C), which are a separate slice. Per ADR §3's
/// soundness requirement the classifier must be <i>complete</i> before any Stage-3 typed flip; until Phase B/C
/// land this pass is built and unit-tested but <b>not</b> consumed by codegen (Stage 2: everything stays
/// byte-backed regardless), so its current incompleteness changes no behavior.</para>
/// </summary>
internal sealed class RecordClassificationPass
{
    /// <summary>
    /// Classifies <paramref name="items"/> (every data item in the program, in declaration order).
    /// <paramref name="categoryOf"/> yields an item's resolved <see cref="CobolCategory"/> (used for the
    /// edited-item trigger) — in the pipeline it is <c>s =&gt; model.GetStorageLocation(s)?.Pic.Category</c>.
    /// </summary>
    public RecordClassification Classify(IReadOnlyList<DataSymbol> items, Func<DataSymbol, CobolCategory> categoryOf)
    {
        var rep = new Dictionary<DataSymbol, RepresentationKind>(ReferenceEqualityComparer.Instance);
        foreach (DataSymbol it in items)
            rep[it] = RepresentationKind.Typed;

        int order(DataSymbol s) // declaration index (for the RENAMES span)
        {
            for (int i = 0; i < items.Count; i++)
                if (ReferenceEquals(items[i], s)) return i;
            return -1;
        }

        bool Mark(DataSymbol? s)
        {
            if (s is null || !rep.TryGetValue(s, out var cur) || cur == RepresentationKind.ByteIsland)
                return false;
            rep[s] = RepresentationKind.ByteIsland;
            return true;
        }

        // ── initial data-division triggers ──
        foreach (DataSymbol it in items)
        {
            // (5) FD/SD file record storage — the disk image is bytes (ISO §13.18.42).
            // (12) LINKAGE items — caller-owned storage; layout cannot be renegotiated (ADR §3.12).
            if (it.Area is StorageAreaKind.FileSection or StorageAreaKind.LinkageSection)
                Mark(it);

            // (8) IS EXTERNAL / IS GLOBAL — one canonical cross-program representation (ISO §13.18.22/.27).
            if (it.IsExternal || it.IsGlobal)
                Mark(it);

            // (1) REDEFINES — the redefiner and its target are the same storage; whole class is one island.
            if (it.Redefines is not null)
            {
                Mark(it);
                Mark(it.Redefines);
            }

            // (2) RENAMES (level 66) — an alphanumeric view over a raw slice (ISO §13.18.43); byte the renaming
            // item and the items it spans, so a later typed flip cannot make the view read re-encoded bytes.
            if (it.Renames is not null)
            {
                Mark(it);
                MarkRenamesSpan(it, items, order, Mark);
            }

            // (13) edited items carry a stored character image / edit pattern (ISO §14.9.25) — keep byte-backed.
            CobolCategory cat = categoryOf(it);
            if (cat is CobolCategory.NumericEdited or CobolCategory.AlphanumericEdited or CobolCategory.NationalEdited)
                Mark(it);
        }

        // ── propagate to a fixpoint: REDEFINES-class closure (both directions) + downward island-membership ──
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (DataSymbol it in items)
            {
                if (rep[it] != RepresentationKind.ByteIsland)
                {
                    // a typed item whose REDEFINES target is already an island must itself become one
                    if (it.Redefines is not null && rep.TryGetValue(it.Redefines, out var t) && t == RepresentationKind.ByteIsland)
                        changed |= Mark(it);
                    continue;
                }

                // it is a byte island → its REDEFINES target and all its subordinates are byte windows too
                if (it.Redefines is not null)
                    changed |= Mark(it.Redefines);
                foreach (DataSymbol child in it.Children)
                    changed |= Mark(child);
            }
        }

        return new RecordClassification(rep);
    }

    /// <summary>Marks the declaration-order range a RENAMES item spans (FROM … THRU), inclusive.</summary>
    private static void MarkRenamesSpan(DataSymbol renamingItem, IReadOnlyList<DataSymbol> items,
        Func<DataSymbol, int> order, Func<DataSymbol?, bool> mark)
    {
        RenamesInfo info = renamingItem.Renames!;
        if (info.FromSymbol is null)
            return;
        mark(info.FromSymbol);
        if (info.ThruSymbol is null)
            return;
        mark(info.ThruSymbol);

        int from = order(info.FromSymbol), thru = order(info.ThruSymbol);
        if (from < 0 || thru < 0)
            return;
        if (from > thru)
            (from, thru) = (thru, from);
        for (int i = from; i <= thru; i++)
            mark(items[i]);
    }
}
