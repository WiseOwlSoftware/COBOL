// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Model;

/// <summary>⛔ THE COMPILE-TIME STORAGE EXTENT OF AN OPERAND — which storage area it lies in, where it starts and how
/// many bytes it spans — and the ONE answer to "do these two operands STATICALLY overlap?" (kb/Work PB1907;
/// docs/CONFORMANCE.md §3 D-OVL1).
/// <para>ISO §2023:14.6.10 1): "if the operands … are not described by the same data description entry … and the
/// storage areas overlap, the result of the statement is undefined" (Annex A.2 item 36). The standard leaves the
/// result open, so WiseOwl COBOL documents what it does, and a rule that depends on overlap needs the overlap
/// to be a FACT the binder can read, not a guess the emitter makes. This is that fact. It is known only where the
/// compiler can prove it: both operands live in ONE storage area (a REDEFINES class, §2023:13.18.44, or one record) and
/// every offset and length is a constant. A dynamic or variable-length item, a USAGE NATIONAL or BIT operand, or a
/// reference modification with a run-time bound answers <see langword="null"/>, and a null is "not provably overlapping" —
/// never "disjoint". An operand under OCCURS has no ONE extent (<see cref="OfItem"/> answers null); the storage of all its
/// occurrences is <see cref="OfEveryOccurrence"/>.</para>
/// <para>The geometry is <see cref="RecordLayout.OffsetInRecord"/>'s — the ONE offset walk the SORT/MERGE key
/// windows already ride — so the answer cannot disagree with the emitted record layout.</para></summary>
/// <param name="Area">The storage area's identity: the REDEFINES class's canonical item when the record belongs to
/// one (a secondary 01 that redefines the first shares its area), otherwise the 01 record itself.</param>
/// <param name="Start">The 0-based byte offset of the operand in the area.</param>
/// <param name="Length">The operand's extent in bytes.</param>
public readonly record struct StorageExtent(DataItem Area, int Start, int Length)
{
    /// <summary>True when the two extents lie in ONE area and share at least one byte.</summary>
    public bool Overlaps(StorageExtent other) =>
        ReferenceEquals(Area, other.Area) && Start < other.Start + other.Length && other.Start < Start + Length;

    /// <summary>The static extent of <paramref name="place"/>, or null when it is not provable (see the type's
    /// summary). A reference modification narrows its inner extent by its literal bounds (§2023:8.4.3.3.4 GR5);
    /// the image decorators keep their inner item's extent.</summary>
    public static StorageExtent? Of(Place place) => place switch
    {
        MemberPlace m => OfItem(m.MemberItem),
        RedefViewPlace { Coding: null } v => OfItem(v.ViewItem),
        NumericImagePlace n => Of(n.Inner),
        GroupImagePlace g => Of(g.Inner),
        RefModPlace r => OfRefMod(r),
        _ => null,
    };

    private static StorageExtent? OfRefMod(RefModPlace r)
    {
        // Positions are CHARACTER positions (§2023:8.4.3.3.4 GR5a); they are bytes only for a DISPLAY-usage operand,
        // which is the only kind whose extent this answers (USAGE NATIONAL / BIT operands answer null above).
        if (Of(r.Inner) is not { } inner || r.Start.Int32Literal is not { } start || start < 1) return null;
        if (r.StaticLength(inner.Length) is not { } len || len < 1 || start - 1 + len > inner.Length) return null;
        return new StorageExtent(inner.Area, inner.Start + start - 1, len);
    }

    /// <summary>The static extent of one data item (its whole occurrence), or null when not provable (see the type's
    /// summary). A level-66 alias is the storage of its data-name-2 (§2023:13.18.45.4 GR1) or, with THROUGH, the span its
    /// synthesized alphanumeric picture covers from data-name-2's first byte (GR2): <see cref="RecordLayout.OffsetInRecord"/>
    /// places it at data-name-2 and its width is the span's.</summary>
    public static StorageExtent? OfItem(DataItem item)
    {
        if (!IsProvable(item)) return null;
        DataItem area = AreaOf(item);
        return RecordLayout.OffsetInRecord(area, item) is { } off
            ? new StorageExtent(area, off, RecordLayout.PhysicalOccurrenceWidth(item))
            : null;
    }

    /// <summary>The storage of EVERY occurrence of <paramref name="item"/>: <see cref="OfItem"/> for an item no OCCURS clause
    /// governs, and for a table element (the item or an ancestor carries OCCURS) its occurrence with every subscript 1 and
    /// the stride and count of each OCCURS level, so a rule that asks whether ANY occurrence shares storage with another
    /// extent (a redefinition "of any part" of an item, §1985:XIII.3.20.3 SR5 c) has an exact answer. Null when not
    /// provable (see the type's summary).</summary>
    public static OccurrenceExtents? OfEveryOccurrence(DataItem item)
    {
        if (!item.IsTableElement) return OfItem(item) is { } single ? new OccurrenceExtents(single, []) : null;
        if (!IsProvable(item)) return null;
        // The occurrence strides, outermost first, and the outermost table's parent, which has ONE position.
        var levels = new List<OccurrenceLevel>();
        DataItem? outerParent = null;
        for (DataItem? x = item; x is not null; x = x.Parent)
        {
            if (x.Occurs is not { } count) continue;
            levels.Insert(0, new OccurrenceLevel(RecordLayout.PhysicalOccurrenceWidth(x), count));
            outerParent = x.Parent;
        }
        if (outerParent is null) return null;
        DataItem area = AreaOf(item);
        if (RecordLayout.OffsetInRecord(area, outerParent) is not { } parentOff
            || RecordLayout.OffsetInRootWithEveryIndexOne(outerParent) is not { } parentInRoot
            || RecordLayout.OffsetInRootWithEveryIndexOne(item) is not { } itemInRoot) return null;
        return new OccurrenceExtents(
            new StorageExtent(area, parentOff + itemInRoot - parentInRoot, RecordLayout.PhysicalOccurrenceWidth(item)), levels);
    }

    private static bool IsProvable(DataItem item) =>
        !(item.IsDynamicLength || item.IsAnyLength || VariableLengthCompatibility.IsVariableLength(item))
        && item.Pic is not { Usage: Usage.National or Usage.Bit } && !item.HasBitDescendant;

    private static DataItem AreaOf(DataItem item)
    {
        DataItem root = item;
        while (root.Parent is { } parent) root = parent;
        return root.Class?.Canonical ?? root;
    }
}

/// <summary>One OCCURS level of a table element's position: the bytes one occurrence of the table entry spans
/// (<paramref name="Stride"/>) and how many occurrences it has (<paramref name="Count"/>, the maximum of a variable table).</summary>
public readonly record struct OccurrenceLevel(int Stride, int Count);

/// <summary>The storage of every occurrence of a data item (<see cref="StorageExtent.OfEveryOccurrence"/>): the occurrence
/// whose every subscript is 1 (<paramref name="First"/>), and each OCCURS level on its path outermost first; occurrence
/// (i1, …, ik) starts at <c>First.Start + Σ (ij − 1) × Stride_j</c> and is <c>First.Length</c> bytes long.</summary>
public sealed record OccurrenceExtents(StorageExtent First, IReadOnlyList<OccurrenceLevel> Levels)
{
    /// <summary>Does any occurrence share at least one byte with <paramref name="other"/>? Exact: occurrences whose
    /// enclosing span lies wholly before or after <paramref name="other"/> are skipped a level at a time, so the walk visits
    /// only the occurrences near it.</summary>
    public bool Overlaps(StorageExtent other)
    {
        if (!ReferenceEquals(First.Area, other.Area)) return false;
        // reach[k]: how far past an occurrence's start of levels k.. the item's last byte can lie.
        var reach = new long[Levels.Count + 1];
        reach[Levels.Count] = First.Length;
        for (int k = Levels.Count - 1; k >= 0; k--)
            reach[k] = reach[k + 1] + (long)(Levels[k].Count - 1) * Levels[k].Stride;
        long otherEnd = (long)other.Start + other.Length;
        return Any(0, First.Start);

        bool Any(int level, long start)
        {
            if (start + reach[level] <= other.Start || start >= otherEnd) return false;
            if (level == Levels.Count) return true;   // one occurrence, and it overlaps
            var (stride, count) = Levels[level];
            for (int i = 0; i < count; i++)
            {
                long s = start + (long)i * stride;
                if (s >= otherEnd) break;
                if (Any(level + 1, s)) return true;
            }
            return false;
        }
    }
}
