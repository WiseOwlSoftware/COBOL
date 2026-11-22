// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Generic;
using System.Linq;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Binding.Model;

/// <summary>The three measures the limit on one data item is decided over (<see cref="RecordLayout.ExceedsSizeLimit"/>): its image in
/// character positions, its storage extent in bytes with every overlay, and the elementary items it holds.</summary>
internal readonly record struct ItemMeasures(long Image, long Extent, long Elementary);

/// <summary>
/// The ONE physical offset/width authority over a record tree (rearchitecture PHASE 05; DESIGN-data-model §2.6).
/// PHASE-FREE since P5 Step 8: leaf widths come from the DECLARED shape (<c>DataItem.ElementaryImageWidth</c>, a
/// pure Pic fact proven equal to the <see cref="StorageForm"/> width by the Step-2 corpus identity #3), so every
/// member here is callable at bind time AND emit time. Consolidates the formerly-duplicated geometry
/// (prove-then-delete: the Step-4/8 corpus asserts + the Sort/Keyed goldens gate each fold before Step 9 deletes
/// the copies): <c>DataItem.ImageWidth</c>'s recursion, <c>OdoModel.PhysicalWidth</c> (the ODO/GR8 physical
/// extent), the Sort geometry (<c>SortPhysicalWidth</c>/<c>SortOffsetInRecord</c>/<c>SortPlainOffset</c>), and the
/// Keyed geometry (<c>KeyedAreaOffset</c>/<c>KeyedKeyIndex</c> + the emitter's <c>KeyedImageOffset</c> twin).
/// <para><b>The Step-8 basis unification (a REAL fix, gated by the failing-first <c>KeyedOffsetSpecTests</c>):</b>
/// the legacy Keyed copies advanced by the redefined item's OWN <see cref="ImageWidth"/>, so a key sitting AFTER a
/// REDEFINES whose redefiner is WIDER than its target got a byte offset that disagreed with the emitted record
/// codec's physical layout — the runtime extracted the WRONG bytes and a same-byte-position operand from a sibling
/// record description failed to match its key. <see cref="OffsetOf"/> sits on the codec-correct
/// <see cref="PhysicalWidth"/> basis (ISO §2023:12.4.5.12.4 GR4 — "the identical BYTE POSITIONS ... in any one record
/// description entry are implicitly referenced as keys for all other record description entries";
/// §2023:14.9.41.3 SR6b — leftmost character position correspondence).</para>
/// </summary>
internal static class RecordLayout
{
    /// <summary>⛔ THE ONE WIDTH RULE of an item's character image — <c>DataItem.ImageWidth</c> IS this (kb/Work
    /// PB1665: the rule was written twice, as a mirror the StorageFormPass identity audit compared, and both copies
    /// counted a national leaf ONCE inside an alphanumeric group). Per THIS item's occurrence (a parent multiplies
    /// by this item's own OCCURS count): a leaf's declared image width (digits + separate-sign, or PIC length,
    /// national in POSITIONS); a group's sum over its NON-redefining children of (child contribution × the child's
    /// own fixed-OCCURS count) — every OCCURS position is part of the group image (ISO §2023:14.9), and a REDEFINING
    /// child overlays its target and adds no storage (§2023:13.18.44).
    /// <para>⛔ THE UNIT OF A GROUP'S WIDTH IS ITS OWN CLASS'S. A <c>GROUP-USAGE NATIONAL</c> group is as-if
    /// <c>PICTURE N(m)</c> (§2023:13.18.29.4 GR2b), so its m is NATIONAL POSITIONS and every item beneath it is counted
    /// in them. Any other group is an alphanumeric group (§2023:8.5.2.1: "an alphanumeric group item has class and
    /// category alphanumeric"), moved "exactly as if it were an alphanumeric to alphanumeric elementary move"
    /// (§2023:14.9.25.4 GR4) — a string of STORAGE characters, in which a national position occupies
    /// <c>NationalImage.BytesPerNational</c> of them (§2023:13.18.60.4 GR8, pinned by D-N1). Counting the national leaf once
    /// there made a group MOVE copy fewer characters than the sending group's image holds, so the trailing
    /// members were lost (kb/Work PB1665: <c>MOVE G5 TO G6</c> left <c>B</c> as spaces). Every consumer of a group's
    /// character-image width — MOVE, the receiving store, the boundary windows, ref-mod positions, LENGTH folds,
    /// the Tier-B strides — reads this one answer, so none restates the unit.</para>
    /// <para>The answer is <see cref="ExactImageWidth"/> through <see cref="ItemSize"/>: see the size section below.</para></summary>
    public static int ImageWidth(DataItem item) => ItemSize(ExactImageWidth(item));

    // ── ⛔ THE ONE COMPUTATION OF AN ITEM'S SIZE (kb/Work PB5695 and its review R1-1) ─────────────────────────────────────
    //
    // Every size the compiler takes of a data item — its image, its bytes, its storage extent with every overlay, an OCCURS
    // product, a group sum, a REDEFINES comparison, an offset, a record length — is computed HERE, in 64 bits, saturating at
    // long.MaxValue, and an ITEM'S OWN size is narrowed to int by ONE function, ItemSize, which throws an internal compiler error
    // for a value no admitted item can have; a size at a hypothetical count stays long (or goes through SaturatedBound). The limit (MaxCharacterPositions / MaxStorageBytes, D-ITEM-SIZE) is decided by ONE function,
    // ExceedsSizeLimit, in CheckStorageLimitsPass, which runs before any pass that narrows a group's size and stops the
    // compile when it refuses an item (DataDivisionRefused). The int product used to wrap: 05 E PIC X(1000) OCCURS 2000000000
    // was 2,000,000,000,000 positions, read as -1,454,759,936, and an over-limit group with a REDEFINES over it crashed the
    // compiler with OverflowException out of a checked Sum. SizeArithmeticDriftTests holds the arithmetic to this file.

    /// <summary>⚠ THE IMPLEMENTOR-DEFINED MAXIMUM number of character positions in ONE DATA ITEM, elementary or group (docs/conformance
    /// determination D-ITEM-SIZE). No edition sets one: §2023:13.18.40.3 SR4 bounds only the WRITTEN character-string, SR14 bounds only
    /// a numeric or fixed-point numeric-edited item's DIGIT positions (1 through 31), an OCCURS integer bounds only its own count, and
    /// Annex A.1 carries no maximum-item-size item, so neither an alphanumeric, alphabetic, national or boolean string nor a group is
    /// bounded by the standard. 2^27 is the largest power of two whose UTF-16 image — two bytes per character position, for
    /// alphanumeric and national alike — stays inside .NET's single-object ceiling, and an item's image is one such string (an
    /// elementary item's value, a group's <c>AsImage</c>). Two screens ask it, each where its size is first known: the PICTURE
    /// expansion of an elementary item (COBOL2148, kb/Work PB531) and the size of a group (COBOL7641, <see cref="ExceedsSizeLimit"/>,
    /// kb/Work PB5695).</summary>
    internal const int MaxCharacterPositions = 1 << 27;

    /// <summary>The most bytes one data item's storage occupies, every item that redefines within it included: as many as it may
    /// hold character positions, so that every BIT offset in it (eight to a byte, §2023:8.1.2 as WiseOwl COBOL pins it) is an
    /// <see cref="int"/>. Part of the same limit (D-ITEM-SIZE); a national item of more than 2^26 positions (two bytes each, D-N1)
    /// meets it before its image does.</summary>
    internal const long MaxStorageBytes = MaxCharacterPositions;

    /// <summary>The largest measure an item within the limit has, so the largest value <see cref="ItemSize"/> accepts: its storage in
    /// bits, eight times <see cref="MaxStorageBytes"/> — above every other measure (its image at most
    /// <see cref="MaxCharacterPositions"/>, its bytes and codec span at most <see cref="MaxStorageBytes"/>, an offset less, the
    /// characters of standard data format at most 2.25 per byte, its boolean positions at most its bits).</summary>
    internal const long MaxMeasure = 8L * MaxStorageBytes;

    /// <summary>⛔ THE ONE NARROWING OF AN ITEM'S OWN SIZE to <see cref="int"/>: a measure of a data item the limit has proved in range
    /// (<see cref="ExceedsSizeLimit"/>) — its image, bytes, storage, an offset or occurrence inside it, a count of its real
    /// occurrences — which is at most <see cref="MaxMeasure"/>; any other value means a pass measured an item the limit refused, or
    /// one it has not screened yet, and that is an internal compiler error, never a wrapped or clamped size.
    /// <para>⛔ A computation over a HYPOTHETICAL count — a search for the largest count a record length admits, a maximum, a bound — is
    /// NOT an item's size and never asks this: it stays in 64 bits (<see cref="BitLayout.TableBitsAtCount"/>, <see cref="Product"/>,
    /// <see cref="Plus"/>) and, if the run time needs an int, goes through <see cref="SaturatedBound"/> (kb/Work PB5695 review R2-2:
    /// <c>SequentialIoEmitter.OdoRecordExceedsMaximum</c>'s search reached it and a legal 2023 program crashed the compiler).</para></summary>
    public static int ItemSize(long measure) =>
        measure is >= 0 and <= MaxMeasure ? (int)measure
        : throw new InvalidOperationException(
            $"internal compiler error: a data item size of {measure} reached the narrowing to int; only an item within the "
            + $"{MaxCharacterPositions:N0}-position limit may be narrowed (RecordLayout.ItemSize, kb/Work PB5695)");

    /// <summary>⛔ THE ONE CONVERSION OF A RUN-TIME BOUND that is not a data item's size — a dynamic-length item's maximum (Annex A.1 item
    /// 62 allows 1,073,741,791 characters) summed into a record's maximum length, a dynamic-capacity table's expected capacity — to
    /// the int the run time takes: saturated at <see cref="int.MaxValue"/>, the documented ceiling of those bounds.</summary>
    public static int SaturatedBound(long bound) => (int)Math.Clamp(bound, int.MinValue, int.MaxValue);

    /// <summary>The occurrences of one entry: its OCCURS count (an OCCURS DEPENDING table's maximum, §2023:8.5.1.8), one for an entry
    /// with no OCCURS clause and for a dynamic-capacity table (its occurrences are not part of the fixed layout).</summary>
    public static int Occurrences(DataItem item) => item.Occurs ?? 1;

    /// <summary>The OCCURS count an entry declares, zero for an entry with no OCCURS clause (the count a field record carries
    /// when it must tell a table from a single item).</summary>
    public static int DeclaredOccurrences(DataItem item) => item.Occurs ?? 0;

    /// <summary>⛔ AN OFFSET ADVANCED PAST A FIELD: <paramref name="offset"/> plus <paramref name="by"/>, in 64 bits and narrowed as the
    /// position inside an item within the limit that it is (the walks that lay a group's fields out one after another).</summary>
    public static int Advance(long offset, long by) => ItemSize(Plus(offset, by));

    /// <summary>⛔ THE OFFSET OF AN OCCURRENCE: <paramref name="origin"/> plus <paramref name="zeroBasedIndex"/> strides of
    /// <paramref name="stride"/>, in 64 bits and narrowed as the item position it is (the occurrence is one of the table's own, so
    /// the offset lies inside an item within the limit).</summary>
    public static int OccurrenceAt(long origin, long zeroBasedIndex, long stride) => ItemSize(Plus(origin, Product(zeroBasedIndex, stride)));

    /// <summary>⛔ THE OCCURS PRODUCT: <paramref name="perOccurrence"/> for each of <paramref name="item"/>'s
    /// <see cref="Occurrences"/>, in 64 bits.</summary>
    public static long Times(long perOccurrence, DataItem item) => Product(perOccurrence, Occurrences(item));

    /// <summary>A product of two sizes or counts, saturating at <see cref="long.MaxValue"/>.</summary>
    public static long Product(long a, long b) => TableValuePlan.SaturatingMultiply(a, b);

    /// <summary>A sum of two sizes, saturating at <see cref="long.MaxValue"/>.</summary>
    public static long Plus(long a, long b) => TableValuePlan.SaturatingAdd(a, b);

    /// <summary>⛔ THE GROUP SUM: <paramref name="perOccurrence"/> of each of <paramref name="group"/>'s storage children (not one that
    /// REDEFINES, which overlays its target and adds no storage, §2023:13.18.44) times its occurrences.</summary>
    public static long OverStorageChildren(DataItem group, Func<DataItem, long> perOccurrence)
    {
        long sum = 0;
        foreach (var c in group.Children)
            if (c.RedefinesTargetName is null) sum = Plus(sum, Times(perOccurrence(c), c));
        return sum;
    }

    /// <summary>⛔ THE ONE DECIDER of "this data item is larger than this implementation's limit" (kb/Work PB5695; D-ITEM-SIZE): its
    /// <see cref="ItemMeasures"/>. An item is within the limit when its image is at most <see cref="MaxCharacterPositions"/>
    /// characters, its storage extent at most <see cref="MaxStorageBytes"/> bytes and it holds at most
    /// <see cref="MaxCharacterPositions"/> elementary items (a table of dynamic-length items occupies no fixed positions, but each
    /// of its occurrences is a string the program allocates).</summary>
    public static bool ExceedsSizeLimit(DataItem item, out ItemMeasures measures)
    {
        measures = new ItemMeasures(ExactImageWidth(item), ExactExtent(item), ElementaryOccurrences(item));
        return measures.Image > MaxCharacterPositions || measures.Extent > MaxStorageBytes || measures.Elementary > MaxCharacterPositions;
    }

    /// <summary><see cref="ImageWidth"/>'s rule, in 64 bits.</summary>
    public static long ExactImageWidth(DataItem item) =>
        item.IsElementary ? item.ElementaryImageWidth
        // D19/PB43 — a bit-bearing subtree is laid out by the ONE §2023:8.5.1.6.3 walk, never summed.
        : item.HasBitDescendant ? BitLayout.Characters(BitLayout.ExactExtentBits(item))
        : OverStorageChildren(item, c => ImageContribution(item, c));

    /// <summary><c>DataItem.ByteWidth</c>'s rule, in 64 bits: the item's size in BYTES, the FUNCTION BYTE-LENGTH (§2023:15.14)
    /// authority — a leaf's pinned per-usage width (<c>DataItem.ElementaryByteWidth</c>), a no-THROUGH level-66 alias of a group the
    /// group it renames (§2023:13.18.45.4 GR1), a bit-bearing group its §2023:8.5.1.6.3 characters, any other group the sum of its
    /// storage children's bytes times their occurrences.</summary>
    public static long ExactByteWidth(DataItem item) =>
        item.IsElementary ? item.ElementaryByteWidth
        : item.Renames is { IsAlias: true, From: { } renamed } ? ExactByteWidth(renamed)
        : item.HasBitDescendant ? BitLayout.Characters(BitLayout.ExactExtentBits(item))
        : OverStorageChildren(item, ExactByteWidth);

    /// <summary>The bytes every occurrence of <paramref name="item"/> occupies — its <c>ByteWidth</c> times its occurrences: the
    /// size a REDEFINES class member is compared and allocated by (§2023:13.18.44.4 GR1, in bytes, kb/Work PB231).</summary>
    public static int OccupiedBytes(DataItem item) => ItemSize(Times(ExactByteWidth(item), item));

    /// <summary><see cref="OccupiedBytes"/> in bits, the unit a REDEFINES overlay of a bit item is measured in (§2023:13.18.44.3 SR8).</summary>
    public static long OccupiedBits(DataItem item) => Product(Times(ExactByteWidth(item), item), BitLayout.BitsPerCharacter);

    /// <summary>The bytes <paramref name="item"/>'s storage spans with every item that redefines within it: a leaf its bytes, a
    /// group the sum over its storage children of the larger of the child's own occupied storage and that of each sibling that
    /// redefines it (§2023:13.18.44.4 GR1: the area is the larger of the two). The codec span (<see cref="PhysicalWidth"/>) never
    /// exceeds it, so an item within <see cref="MaxStorageBytes"/> lays out without a wrap.</summary>
    public static long ExactExtent(DataItem item)
    {
        if (!item.IsGroup) return ExactByteWidth(item);
        if (item.HasBitDescendant) return BitLayout.Characters(BitLayout.ExactExtentBits(item));
        long sum = 0;
        var children = item.Children;
        for (int i = 0; i < children.Count; i++)
        {
            var c = children[i];
            if (c.RedefinesTargetName is not null) continue;
            long slot = Times(ExactExtent(c), c);
            // The entries that redefine it follow it "without intervening entries that define new storage areas" (§2023:13.18.44.3
            // SR10), each overlaying its area.
            for (int j = i + 1; j < children.Count && children[j].RedefinesTargetName is not null; j++)
                slot = Math.Max(slot, Times(ExactExtent(children[j]), children[j]));
            sum = Plus(sum, slot);
        }
        return sum;
    }

    /// <summary>The elementary items (counting each occurrence) that <paramref name="item"/>'s storage holds.</summary>
    public static long ElementaryOccurrences(DataItem item) =>
        item.IsElementary ? 1 : OverStorageChildren(item, ElementaryOccurrences);
    /// <summary>The character positions EVERY occurrence of <paramref name="item"/> occupies — <see cref="ImageWidth"/> (one occurrence)
    /// times the occurrences of each table level on its path: the most a reference that names the item without subscripts (a whole
    /// table, <c>PlaceBuilder.WholeTable</c>) can hold, which is what X3.23-1985 USE FOR DEBUGGING general rule 15 sizes DEBUG-CONTENTS
    /// by ("large enough to contain the data required by the following general rules"; rule 24 c, kb/Work PB4855). A fixed or OCCURS
    /// DEPENDING level counts its maximum (§2023:8.5.1.8). A dynamic-capacity level has no maximum (§2023:8.5.1.9.1 GR2), so it counts
    /// its expected capacity (TO integer-5), else its initial capacity (FROM integer-4), at least one; an image past the register's
    /// width is truncated by the rule 11 move (docs/CONFORMANCE.md D-DEBUG-WHOLE-TABLE (4)). Equal to <see cref="ImageWidth"/> for an
    /// item in no table.</summary>
    public static long AllOccurrencesImageWidth(DataItem item)
    {
        long width = ExactImageWidth(item);
        foreach (var level in item.SubscriptLevels())
            width = Product(width, level.Occurs ?? (level.OccursSpec is { IsDynamic: true } d ? Math.Max(1, d.ExpectedMax ?? d.InitialCap ?? 1) : 1));
        return width;
    }

    /// <summary>ONE occurrence of <paramref name="child"/> in the character positions of its enclosing
    /// <paramref name="group"/> — see <see cref="ImageWidth"/>: its own width, doubled to storage characters when
    /// the enclosing group is alphanumeric and the child is of class national.</summary>
    private static long ImageContribution(DataItem group, DataItem child)
    {
        long w = ExactImageWidth(child);
        return !InNationalGroup(group) && IsNationalClass(child) ? Product(w, PicInfo.BytesPerPositionOf(Usage.National)) : w;
    }

    /// <summary>A national item is an elementary item of usage national or a national group
    /// (§2023:13.18.29.4 GR2b treats the group as an elementary national item).</summary>
    private static bool IsNationalClass(DataItem item) =>
        item.IsElementary ? item.Pic?.Usage is Usage.National : InNationalGroup(item);

    /// <summary>The item is a GROUP-USAGE NATIONAL group or beneath one. Asked of the ancestor chain rather than of
    /// the group's own <see cref="DataItem.GroupUsage"/> alone, because usage is propagated to subordinate groups
    /// by a later bind walk (§2023:13.16.4 GR2's implied clause) and a width is a pure function of the declared shape,
    /// callable before it runs.</summary>
    private static bool InNationalGroup(DataItem item)
    {
        for (var p = item; p is not null; p = p.Parent)
            if (p.IsGroup && p.GroupUsage is GroupUsage.National) return true;
        return false;
    }

    /// <summary>The tier-aware PHYSICAL image width of an item — the extent the emitted <c>AsImage()</c>/<c>FromImage()</c>
    /// codec spans (DESIGN §4.2): a REDEFINING child overlays its target and contributes nothing; a Tier-B
    /// <see cref="RedefinesTier.StringCanonical"/> class contributes its ONE backing (class-max
    /// <see cref="RedefinesClass.Width"/>) once at the canonical member. For the class-free common subtree this
    /// equals <see cref="ImageWidth"/>. Mirrors <c>OdoModel.PhysicalWidth</c> / <c>SortPhysicalWidth</c> (the ODO
    /// §2023:13.18.38.4 GR8 extent) — the raw <see cref="ImageWidth"/> would over- or under-count a group containing a
    /// redefines class whose redefiner is wider than the redefined item.</summary>
    public static int PhysicalWidth(DataItem item) => ItemSize(ExactPhysicalWidth(item));

    /// <summary><see cref="PhysicalWidth"/>'s walk, in 64 bits.</summary>
    private static long ExactPhysicalWidth(DataItem item)
    {
        // ⛔ THE CODEC BASIS IS BYTES, NOT CARRIER POSITIONS (kb/Work PB327). This walk is the record/frame
        // geometry — §2023:12.4.5.12.4 GR4 states key correspondence over "the identical BYTE POSITIONS", §2023:14.9.30.4
        // GR14/GR15 state the short/long-record rules over "the number of BYTES in the record", and
        // §2023:14.9.3.4 GR3 allocates "the number of BYTES required to hold an item". ByteWidth IS ImageWidth for
        // every leaf kind but NATIONAL, whose §2023:13.18.60.4 GR8 size this implementation pins at two bytes per
        // position (D-N1) — so this read is byte-identical everywhere else and is the whole of the geometry
        // change that admits a national leaf to a file record. <see cref="ImageWidth"/> stays the CARRIER
        // authority (StorageFormPass's form widths, the §2023:13.18.29.4 GR2b as-if PICTURE N(m) length).
        if (!item.IsGroup) return ExactByteWidth(item);
        // D19/PB43 — a bit-bearing subtree's extent comes from the §2023:8.5.1.6.3 walk. A REDEFINES class inside such
        // a group is not reachable here: D-N2's byte≠char containment already refuses a sub-byte overlay, and
        // PB43 records the sub-byte redefiner as explicitly OUT of scope and loud rather than rounded.
        if (item.HasBitDescendant) return BitLayout.Characters(BitLayout.ExactExtentBits(item));
        long w = 0;
        foreach (var c in item.Children) w = Plus(w, ExactPhysicalAdvance(c));
        return w;
    }

    /// <summary>⛔ THE ONE PER-CHILD PHYSICAL ADVANCE (kb/Work PB3256): the bytes <paramref name="child"/> adds to its parent's
    /// running position on the codec basis — what <see cref="PhysicalWidth"/> sums and what every offset walk here
    /// (<see cref="OffsetInRootWithEveryIndexOne"/>, <see cref="ContiguousPlaceOf"/>) advances by, so a record's width and the
    /// offsets inside it cannot disagree. It was written three times and the copies had drifted: the width had no redefiner
    /// arm, so a redefining child outside a class (any redefiner asked before <c>ClassifyRedefinesClassesPass</c> runs, which
    /// this phase-free type admits) added its width to the record while adding nothing to the offsets after it.
    /// <list type="bullet">
    ///   <item>A top-level member of a Tier-B (<see cref="RedefinesTier.StringCanonical"/>) class: the CANONICAL member —
    ///     which may itself be a REDEFINER (the classifier picks the class's storage owner, not necessarily the target) —
    ///     contributes the ONE backing at class-max width; every other member nothing. The substitution applies ONLY where
    ///     the child is a class MEMBER (the overlay root): a SUBORDINATE of a member carries an inherited <c>.Class</c> link
    ///     but occupies its own positions inside the member's window (the P5.8 area-class find: skipping subordinates of a
    ///     multi-record FD's synthesized AREA class collapsed the record width to 0).</item>
    ///   <item>A non-canonical member of a Tier-A (<see cref="RedefinesTier.Alias"/>) class: a forwarded view, nothing.</item>
    ///   <item>Any other REDEFINING child: it overlays its target (§2023:13.18.44.4 GR1, "Storage association for the subject
    ///     of the entry starts at the first bit of the data item referenced by data-name-2"), nothing.</item>
    ///   <item>Everything else: one occurrence's <see cref="PhysicalOccurrenceWidth"/> times its OCCURS count.</item>
    /// </list></summary>
    private static long ExactPhysicalAdvance(DataItem child)
    {
        if (child.Class is { Tier: RedefinesTier.StringCanonical } cls && cls.Members.Contains(child))
            return child.IsCanonical ? cls.Width : 0;
        if (child.Class is { Tier: RedefinesTier.Alias } alias && alias.Members.Contains(child) && !child.IsCanonical)
            return 0;
        if (child.RedefinesTargetName is not null) return 0;
        return Times(ExactPhysicalOccurrenceWidth(child), child);
    }

    /// <summary>ONE OCCURRENCE of <paramref name="item"/> on the PHYSICAL (codec, byte) basis — the per-child
    /// contribution <see cref="PhysicalWidth"/> multiplies by the child's OCCURS count, written once because three
    /// walks need it: <see cref="PhysicalWidth"/>'s sum, <see cref="OffsetOf"/>'s advance, and the §2023:13.18.38.4 GR8
    /// occurs-depending extent (<c>OdoModel.WrapGroup</c>), whose per-occurrence STRIDE has to be in the same unit
    /// as the group image it slices. ⛔ That third reader used to take <c>table.ImageWidth</c> — CHARACTER
    /// positions, which a national leaf counts once where this basis counts it twice (§2023:13.18.60.4 GR8; D-N1) —
    /// against a <see cref="PhysicalWidth"/> total in BYTES, so the fixed prefix came out as the difference and a
    /// zero-occurrence national table sent half its maximum image (kb/Work PB943).</summary>
    internal static int PhysicalOccurrenceWidth(DataItem item) => ItemSize(ExactPhysicalOccurrenceWidth(item));

    private static long ExactPhysicalOccurrenceWidth(DataItem item) =>
        item.IsGroup ? ExactPhysicalWidth(item) : ExactByteWidth(item);

    /// <summary>The item's character offset within its record AREA on the PHYSICAL (codec) basis: the offset inside
    /// its own 01 root, which IS the area offset because every secondary 01 under an FD is a synthesized REDEFINES
    /// of the first, starting at position 0 (ISO §2023:13.18.44.4 GR1). A REDEFINING child takes its TARGET's offset and
    /// contributes no width; the running position advances by each preceding sibling's PHYSICAL contribution — the
    /// class-max backing width at a Tier-B canonical (matching the emitted record layout), the plain image extent
    /// elsewhere. Null when the item or any ancestor carries OCCURS (no single fixed position — §2023:12.4.5.12.3 SR1 /
    /// §2023:14.9.40.3 SR6b ban OCCURS subjects; mirrors the Sort walk's bail).</summary>
    public static int? OffsetOf(DataItem item) => item.Occurs is null ? FirstOccurrenceOffsetOf(item) : null;

    /// <summary>The offset of <paramref name="item"/>'s FIRST occurrence within its record area — <see cref="OffsetOf"/>
    /// for an item that may itself carry the OCCURS clause, whose first byte position §2023:13.18.38.3 SR20 measures from
    /// ("the first byte position defined by the data description entry containing the OCCURS clause"; kb/Work
    /// PB1261). Null when an ANCESTOR carries OCCURS.</summary>
    public static int? FirstOccurrenceOffsetOf(DataItem item)
    {
        // ⛔ A LEVEL-66 ALIAS HAS NO PLACE IN THE CHILD WALK BELOW — it rides its record's Renames66 list, not Children
        // (kb/Work PB2502) — but it OCCUPIES storage: without THROUGH it is data-name-2's own storage area
        // (§2023:13.18.45.4 GR1), and with THROUGH it is the area that begins where data-name-2 begins (GR2; SR11 puts
        // data-name-3 at or after it). So its offset is data-name-2's, which this walk answers. A RECORD KEY naming
        // an alias used to find no offset and abort the run unit at registration.
        if (item.Renames is { From: { } renamed }) return FirstOccurrenceOffsetOf(renamed);
        for (var p = item.Parent; p is not null; p = p.Parent)
            if (p.Occurs is not null) return null;   // subject to OCCURS ⇒ no single fixed position
        return OffsetInRootWithEveryIndexOne(item);
    }

    /// <summary>The offset in its 01 root of <paramref name="item"/>'s occurrence whose every subscript is 1 — the
    /// <see cref="FirstOccurrenceOffsetOf"/> walk without its OCCURS-ancestor bail. A table element has no ONE position,
    /// so a caller asks this only to build the positions of ALL its occurrences
    /// (<see cref="StorageExtent.OfEveryOccurrence"/>), each the walk's answer plus the occurrence strides.</summary>
    internal static int? OffsetInRootWithEveryIndexOne(DataItem item)
    {
        if (item.Renames is { From: { } renamed }) return OffsetInRootWithEveryIndexOne(renamed);
        DataItem root = item;
        while (root.Parent is { } parent) root = parent;

        long? found = null;
        var offsets = new Dictionary<DataItem, long>(ReferenceEqualityComparer.Instance);
        Walk(root, 0);
        return found is { } at ? ItemSize(at) : null;

        void Walk(DataItem node, long off)
        {
            if (found is not null) return;
            offsets[node] = off;
            if (ReferenceEquals(node, item)) { found = off; return; }
            long running = off;
            foreach (var c in node.Children)
            {
                long cOff = c.RedefinesTarget is { } t && offsets.TryGetValue(t, out long tOff) ? tOff : running;
                Walk(c, cOff);
                if (found is not null) return;
                running = Plus(running, ExactPhysicalAdvance(c));
            }
        }
    }

    /// <summary>The PHYSICAL width of a RECORD as its frame/area extent: a record that is itself a member of a
    /// REDEFINES class (a secondary 01 of a multi-record SD/FD — the synthesized area class) spans the ONE shared
    /// backing (class-max width, §2023:9.1.2 records leftmost-aligned in one area); otherwise its own
    /// <see cref="PhysicalWidth"/>. (← <c>SortPhysicalWidth</c>'s item-level rule.)</summary>
    public static int AreaWidth(DataItem record) =>
        record.Class is { Tier: RedefinesTier.StringCanonical } cls && record.IsCanonical
            ? cls.Width
            : PhysicalWidth(record);

    /// <summary>The character offset of <paramref name="target"/> within <paramref name="root"/>'s PHYSICAL
    /// record image — the compile-time key window (ISO §2023:14.9.40.3 SR6e: the same byte positions are the key in
    /// every record). An item inside a REDEFINES class sits at the class anchor's offset plus its
    /// <see cref="DataItem.ClassOffset"/> (a redefinition begins at the redefined item's first position,
    /// §2023:13.18.44.4 GR1; covers keys under a redefining group and keys in a secondary 01 of a multi-01 SD/FD, whose
    /// synthesized class anchors at the first record). Null when the target does not live in
    /// <paramref name="root"/>'s area, or sits under an OCCURS (not a legal key, §2023:14.9.40.3 SR6b/SR6f).
    /// (← <c>SortOffsetInRecord</c>/<c>SortPlainOffset</c>, on the ONE <see cref="OffsetOf"/> walk.)</summary>
    public static int? OffsetInRecord(DataItem root, DataItem target) => OffsetInRecordOfFirstOccurrence(root, target, tableElementAllowed: false);

    /// <summary>The walk of <see cref="OffsetInRecord(DataItem, DataItem)"/>, optionally for a table element's FIRST occurrence — the
    /// start every occurrence's offset adds its <see cref="OccurrenceDisplacement"/> to (kb/Work PB5602). The REDEFINES-class branch
    /// is the same walk: a class member's <see cref="DataItem.ClassOffset"/> counts a table element from its first occurrence too.</summary>
    private static int? OffsetInRecordOfFirstOccurrence(DataItem root, DataItem target, bool tableElementAllowed)
    {
        // ⛔ THE OCCURS BAIL IS ASKED OF THE DATA DESCRIPTION, AHEAD OF BOTH BRANCHES (kb/Work PB1173). It lived in
        // the plain walk (OffsetOf) only, so the class branch below — taken by EVERY 01 of a multi-record SD/FD,
        // which is a member of the synthesized REDEFINES class (PB836) — returned the FIRST occurrence's position
        // for an item under OCCURS and a key under a table was accepted with its occurrence discarded. An item
        // subject to OCCURS has no one position in the record whatever its storage form.
        if (target.IsTableElement && !tableElementAllowed) return null;   // §2023:8.4.2.3.3 SR2's own-or-ancestor question, asked of DataItem (PB877)
        // A level-66 alias is positioned where its data-name-2 is (§2023:13.18.45.4 GR1/GR2; see FirstOccurrenceOffsetOf),
        // and data-name-2 may itself be a member of a REDEFINES class, which the branch below places.
        if (target.Renames is { From: { } renamed }) return OffsetInRecordOfFirstOccurrence(root, renamed, tableElementAllowed);
        if (target.Class is { } cls)
        {
            if (ReferenceEquals(cls.Canonical, root)) return target.ClassOffset;
            if (ReferenceEquals(root.Class, cls)) return target.ClassOffset - root.ClassOffset;
            // The class anchors at its CANONICAL's plain position (the canonical itself carries the class link,
            // so it must resolve through the PLAIN walk — recursing the class branch would never terminate).
            return Plain(root, cls.Canonical) is { } a ? a + target.ClassOffset : null;
        }
        return Plain(root, target);

        static int? Plain(DataItem root, DataItem target)
        {
            for (DataItem? n = target; n is not null; n = n.Parent)
                if (ReferenceEquals(n, root))
                    // under root (an 01 ⇒ area offset 0): the area offset IS the window
                    return target.IsTableElement ? OffsetInRootWithEveryIndexOne(target) : OffsetOf(target);
            return null;
        }
    }

    // ── A key's window in a VARIABLE-LENGTH record (kb/Work PB1025; docs/CONFORMANCE.md §3 D-KWV) ────────────────

    /// <summary>A key's byte window in its record (<see cref="KeyWindowOf"/>): <paramref name="Offset"/> — the
    /// record offset, or for a key a variable-length member precedes (<paramref name="FollowsVariable"/>) its
    /// FIXED-run offset; <paramref name="Bytes"/> — its byte extent; <paramref name="MaxEnd"/> — the furthest byte
    /// past its end in any record of the type, the quantity the minimum-record-size rules bound.</summary>
    public readonly record struct KeyWindow(int Offset, int Bytes, bool FollowsVariable, long MaxEnd);

    /// <summary>⛔ THE ONE ANSWER TO "WHERE IS THIS KEY IN THE RECORD, AND HOW FAR CAN IT REACH?" — for a SORT/MERGE
    /// key (§2023:14.9.40.3 SR6g / §2023:14.9.24.3 SR4g) and an indexed RECORD KEY / ALTERNATE RECORD KEY (§2023:12.4.5.12.3
    /// SR4 / §2023:12.4.5.6.3 SR5), whose rules are the same sentence: in a file of variable-length records every key
    /// "shall be contained within the first n bytes of the record, where n equals the minimum record size".
    /// <para>A variable-length record is sent and held as its CONTIGUOUS image (§2023:8.5.1.11.2 — "behaves in all
    /// respects as though it were in fact contiguous with its neighbors"; determination D-FRA), so a key that a
    /// dynamic-length item PRECEDES has no one position (a dynamic-capacity table cannot be in a record —
    /// §2023:8.5.1.9.1 SR3, COBOL1526; <see cref="FileModel.MaxDynamicExtent"/>): it starts at its offset in the
    /// record's FIXED run plus whatever those members hold in THIS record. Such a key is
    /// <see cref="KeyWindow.FollowsVariable"/>, its <see cref="KeyWindow.Offset"/> is the FIXED-run offset the
    /// record type's runtime layout (<c>CobolContiguousLayout.Position</c>) starts from, and its
    /// <see cref="KeyWindow.MaxEnd"/> charges every preceding member its MAXIMUM extent (§2023:13.18.43.4 GR8b) — the
    /// furthest byte the key can occupy in any record, which is what "contained within the first n bytes" has to
    /// hold for. Every other key keeps <see cref="OffsetInRecord"/>'s fixed window, unchanged.</para>
    /// <para>Null when the key does not lie in <paramref name="root"/>'s area, or is subject to OCCURS.</para></summary>
    public static KeyWindow? KeyWindowOf(DataItem root, DataItem key, int bytes, IReadOnlyList<int> occurrence)
    {
        if (OffsetInRecord(root, key, occurrence) is not { } off) return null;
        if (FileModel.IsVariableLengthRecord(root) && ContiguousPlaceOf(root, key) is { Preceding: > 0 } p)
        {
            int shift = OccurrenceDisplacement(key, occurrence);
            return new KeyWindow(p.FixedOffset + shift, bytes, true, p.FixedOffset + shift + p.PrecedingMaxBytes + bytes);
        }
        return new KeyWindow(off, bytes, false, off + bytes);
    }

    /// <summary>⛔ THE BYTES FROM A TABLE ELEMENT'S FIRST OCCURRENCE TO THE OCCURRENCE THE INTEGER-LITERAL SUBSCRIPTS SELECT
    /// (kb/Work PB5602): the sum, over the OCCURS levels of <paramref name="item"/> outermost first, of the occurrence number
    /// less one times that level's physical occurrence width — the stride <see cref="StorageExtent.OfEveryOccurrence"/> records
    /// per level. Zero for no subscripts. The caller has screened the subscripts (<c>DataBinder.ScreenOperandOccurrence</c>).</summary>
    public static int OccurrenceDisplacement(DataItem item, IReadOnlyList<int> occurrence)
    {
        if (occurrence.Count == 0) return 0;
        var levels = item.SubscriptLevels();
        long bytes = 0;
        for (int i = 0; i < levels.Count && i < occurrence.Count; i++)
            bytes = Plus(bytes, Product(occurrence[i] - 1, ExactPhysicalOccurrenceWidth(levels[i])));
        return ItemSize(bytes);
    }

    /// <summary>The offset of <paramref name="item"/> in its 01 record when it is written with the integer-literal
    /// <paramref name="occurrence"/> subscripts (COBOL-2002, §2002:5.1.3; kb/Work PB5602): <see cref="OffsetOf"/> for no subscripts,
    /// else the offset of the item's first occurrence (the same walk as <see cref="OffsetInRecord(DataItem, DataItem)"/>, REDEFINES
    /// classes included) plus <see cref="OccurrenceDisplacement"/>. Null for an item that is not a table element.</summary>
    public static int? OffsetOf(DataItem item, IReadOnlyList<int> occurrence)
    {
        if (occurrence.Count == 0) return OffsetOf(item);
        if (!item.IsTableElement) return null;
        var root = item;
        while (root.Parent is { } parent) root = parent;
        return OffsetInRecordOfFirstOccurrence(root, item, tableElementAllowed: true) is { } first
            ? first + OccurrenceDisplacement(item, occurrence) : null;
    }

    /// <summary><see cref="OffsetInRecord(DataItem, DataItem)"/> for the occurrence the subscripts select (kb/Work PB5602): the
    /// plain walk's first occurrence plus <see cref="OccurrenceDisplacement"/> when the item lies in <paramref name="root"/>.</summary>
    public static int? OffsetInRecord(DataItem root, DataItem target, IReadOnlyList<int> occurrence)
    {
        if (occurrence.Count == 0) return OffsetInRecord(root, target);
        return target.IsTableElement && OffsetInRecordOfFirstOccurrence(root, target, tableElementAllowed: true) is { } first
            ? first + OccurrenceDisplacement(target, occurrence) : null;
    }

    /// <summary><see cref="KeyWindowOf"/> for a key of <paramref name="file"/>'s own file control entry — the form
    /// every rule that reads a RECORD KEY or ALTERNATE RECORD KEY asks (§2023:12.4.5.12.3 SR4, §2023:12.4.5.6.3 SR4 and SR5):
    /// the key's window in the record that holds it, null when <paramref name="key"/> is null, is not in a record of
    /// the file (its SR2 reports that), or is subject to OCCURS (its SR1 reports that).</summary>
    public static KeyWindow? KeyWindowInFile(FileModel file, DataItem? key, IReadOnlyList<int> occurrence)
    {
        if (key is null || !IsInRecordOfFile(file, key)) return null;
        var root = key;
        while (root.Parent is { } p) root = p;
        return KeyWindowOf(root, key, key.ByteWidth, occurrence);
    }

    /// <summary>The walk behind <see cref="KeyWindowOf"/>: <paramref name="key"/>'s offset in
    /// <paramref name="root"/>'s FIXED run (every variable-length member contributing ZERO — the §2023:8.5.1.12.3
    /// accounting the emitted layout's <c>FixedAt</c> uses, flattened through nested variable-length groups the
    /// same way), how many variable-length members precede it, and their summed MAXIMUM byte extents. The fixed
    /// members advance by <see cref="ExactPhysicalAdvance"/>, as <see cref="OffsetOf"/> does (the codec basis), so the offset is the one the
    /// layout's take step starts from.</summary>
    private static (int FixedOffset, int Preceding, long PrecedingMaxBytes)? ContiguousPlaceOf(DataItem root, DataItem key)
    {
        // A level-66 alias is no child of the walk; its storage begins where data-name-2's does (§2023:13.18.45.4).
        if (key.Renames is { From: { } renamed }) key = renamed;
        int preceding = 0;
        long maxBytes = 0;
        long? found = null;
        var offsets = new Dictionary<DataItem, long>(ReferenceEqualityComparer.Instance);
        Walk(root, 0);
        return found is { } f ? (ItemSize(f), preceding, maxBytes) : null;

        // Returns the FIXED-run offset just past node.
        long Walk(DataItem node, long start)
        {
            offsets[node] = start;
            if (ReferenceEquals(node, key)) { found = start; return start; }
            long running = start;
            foreach (var c in node.Children)
            {
                if (found is not null) return running;
                long cStart = c.RedefinesTarget is { } t && offsets.TryGetValue(t, out long tOff) ? tOff : running;
                if (c.IsDynamicLength)
                {
                    if (ReferenceEquals(c, key)) { found = cStart; return running; }
                    if (c.RedefinesTargetName is null) { preceding++; maxBytes += FileModel.MaxDynamicExtent(c); }
                    continue;   // §2023:8.5.1.12.3 — contributes nothing to the fixed run
                }
                if (c.IsGroup && DataItem.HasVariableLengthSubordinate(c) && c.RedefinesTargetName is null)
                {
                    running = Walk(c, cStart);   // a nested variable-length group flattens in place
                    continue;
                }
                Walk(c, cStart);
                if (found is not null) return running;
                running = Plus(running, ExactPhysicalAdvance(c));
            }
            return running;
        }
    }

    // ── Key-of-reference operand screens (ISO §2023:14.9.41.3 SR6 · §2023:14.9.30.3 SR11 · §2023:12.4.5.12.4 GR4) ─────────────
    //
    // ⛔ TWO RULES, TWO ENTRY POINTS (kb/Work PB354). One method used to answer both, with the SR6 b) test —
    // "leftmost position coincides AND no longer than the key" — standing in for BOTH the "this IS a record key"
    // question READ asks and the "this is a legal GENERIC key" question START asks. That conflation is what let
    // §2023:14.9.41.3 SR6b) 2.'s class/category/usage condition sit unimplemented behind a doc comment (it has no
    // meaning for READ, so it could not be added to the shared body), and it made READ's §2023:14.9.30.3 SR11 — which
    // carries NO generic-key arm at all — accept a short item where the rule names the key itself. The two rules
    // now have one method each, and each names the clause it enforces.

    /// <summary>ISO §2023:14.9.41.3 SR6a / §2023:14.9.30.3 SR11 — the operand IS a prime or alternate record key of
    /// <paramref name="file"/> (−1 = prime, i = the i-th alternate, null = it is not a key of this file): the
    /// declared key item itself, or the item occupying the IDENTICAL byte positions in another record
    /// description entry of the same file. §2023:12.4.5.12.4 GR4 — <i>"the identical byte positions … in any one
    /// record description entry are implicitly referenced as keys for all other record description entries"</i>
    /// — is what makes the second form a key, and it is why the positional arm demands the SAME byte width
    /// rather than a smaller one: GR4 promotes the key's whole span, never a prefix of it. A prefix is SR6 b)'s
    /// generic key, which START admits and READ does not (see <see cref="GenericKeyIndex"/>).</summary>
    public static int? KeyIndexOfKeyItem(FileModel file, DataItem operand, IReadOnlyList<int> occurrence)
    {
        if (file.PrimeKey is { } prime && prime.Names(operand, occurrence)) return -1;
        for (int i = 0; i < file.AlternateKeys.Count; i++)
            if (file.AlternateKeys[i].Key.Names(operand, occurrence)) return i;
        // §2023:12.4.5.12.4 GR4's implicit keys — the same byte window in ANOTHER record description of the file
        // (a same-position item in a secondary 01 — NOT a REDEFINES inside the key's own record, kb/Work PB5877). The operand shall live in a
        // record description entry OF THIS FILE: GR4 speaks of the file's own record description entries, and
        // without that test offset 0 in ANY 01 anywhere — WORKING-STORAGE included — collided with offset 0 in
        // the record (kb/Work PB354 part 1).
        if (!IsInRecordOfFile(file, operand) || OffsetOf(operand, occurrence) is not { } off) return null;
        // The implicit key is "specified as a record key" only if it can be one: the item shares the key's class,
        // category and usage (§1985:IX.2.7.3 SR2 makes every key alphanumeric; §2023:14.9.41.3 SR6b), so a numeric
        // item of the same width at the key's positions is no key (kb/Work PB5794).
        // ⛔ GR4 reaches "all OTHER record description entries" only (§1985:IX.2.7.4 GR3, §2023:12.4.5.12.4 GR4): an item
        // in the key's OWN record description entry that is not the key itself (a REDEFINES of it) is not specified as a
        // record key, so it is no implicit key (kb/Work PB5877). Names() above already answered the key itself.
        DataItem operandRecord = RootOf(operand);
        return KeyIndexAtOffset(file, off,
            key => !ReferenceEquals(RootOf(key), operandRecord) && key.ByteWidth == operand.ByteWidth && SameClassCategoryUsage(operand, key));
    }

    private static DataItem RootOf(DataItem item)
    {
        while (item.Parent is { } p) item = p;
        return item;
    }

    /// <summary>ISO §2023:14.9.41.3 SR6b — START's GENERIC key: a data item that is not itself a record key but
    /// <b>b) 1.</b> whose leftmost character position <i>within a record of the file</i> corresponds to a record
    /// key's leftmost character position, <b>b) 2.</b> which has the same class, category and usage as that
    /// record key, and <b>b) 3.</b> whose length is not greater than that key's. All three conditions are
    /// conjunctive; b) 2. used to be omitted by an explicit doc-comment note, which let a
    /// <c>PIC 9(4) COMP-3</c> item stand in for an <c>X(6)</c> key and be compared on an incommensurable basis
    /// (kb/Work PB354 part 4; b) 2. and b) 3. are addressed by their printed path on <c>SR-14.9.41.3-6</c> since the
    /// PB698 transcription repair returned them to their printed level).
    /// <para>b) 1. also requires the key to be <i>"defined without the SOURCE phrase"</i>: a SOURCE-form key
    /// (kb/Work PB1099) has no data item of its own to coincide with, and <see cref="KeyIndexAtOffset"/> passes
    /// over it — the same exclusion START's §2023:14.9.41.3 SR6a and READ's §2023:14.9.30.3 SR11 get, since a SOURCE-form
    /// key is named only by its record-key-name (§2023:14.9.41.3 SR7).</para></summary>
    public static int? GenericKeyIndex(FileModel file, DataItem operand, IReadOnlyList<int> occurrence)
    {
        if (!IsInRecordOfFile(file, operand) || OffsetOf(operand, occurrence) is not { } off) return null;   // b) 1.
        return KeyIndexAtOffset(file, off,
            key => SameClassCategoryUsage(operand, key)                                        // b) 2.
                && operand.ByteWidth <= key.ByteWidth);                                        // b) 3.
    }

    /// <summary>True when the item is <i>subject to an OCCURS clause</i> — it or any ancestor carries one. THE
    /// ONE predicate for that phrase (ISO §2023:14.9.41.3 SR4 <i>"Data-name-1 or record-key-name-1 shall not be
    /// subject to any OCCURS clauses"</i>; §2023:12.4.5.13.3 SR1, the RELATIVE KEY twin). Kept apart from
    /// <see cref="OffsetOf"/>'s own OCCURS bail, which answers a different question ("this item has no single
    /// fixed position") — folding the two is what made an SR4 violation report itself as an SR6 failure under a
    /// sentence that was FALSE of the operand (kb/Work PB354 part 2).</summary>
    public static bool IsSubjectToOccurs(DataItem item) => OccursSubjectOf(item) is not null;

    /// <summary>The entry whose OCCURS clause <paramref name="item"/> is subject to — the item itself or its nearest
    /// ancestor that carries one — or null. ANY format of the clause counts (§2023:13.18.38: fixed, occurs-depending and
    /// Format-4 dynamic-capacity alike), so the test is <see cref="DataItem.IsTable"/>, never <c>Occurs</c> alone:
    /// <c>Occurs</c> is null for a dynamic-capacity table, and asking it let an item under OCCURS DYNAMIC pass every
    /// "subject to an OCCURS clause" rule (kb/Work PB1273's sweep; the CONTROL SR3 arm fell into the same trap).</summary>
    public static DataItem? OccursSubjectOf(DataItem item)
    {
        for (var p = item; p is not null; p = p.Parent)
            if (p.IsTable) return p;
        return null;
    }

    /// <summary>The item's 01 record description entry is one of <paramref name="file"/>'s records — ISO
    /// §2023:14.9.41.3 SR6b 1.'s <i>"within a record of the file"</i>, and the same predicate the file control
    /// entry's own key rules are stated on: §2023:12.4.5.12.3 SR2 and §2023:12.4.5.6.3 SR2 REQUIRE it of a record key,
    /// §2023:12.4.5.13.3 SR3 FORBIDS it of the relative key. ONE predicate for all four (<c>FileControlKeyRules</c>).
    /// <para>A null item is NOT in a record of the file — a data-name-1 that referenced nothing describable is a
    /// violation of the two SR2s, and answering "false" is what lets one row state that rule.</para></summary>
    public static bool IsInRecordOfFile(FileModel file, DataItem? item)
    {
        return item is not null && ReferenceEquals(RootOf(item).RecordOfFile, file);
    }

    /// <summary>The index of the record key whose leftmost byte position is <paramref name="off"/> and which
    /// satisfies the caller's rule (−1 = prime, i = the i-th alternate, null = none). Positions are the PHYSICAL
    /// codec layout via <see cref="OffsetOf"/> — the basis §2023:12.4.5.12.4 GR4 states the correspondence on.</summary>
    private static int? KeyIndexAtOffset(FileModel file, int off, Func<DataItem, bool> admits)
    {
        // Only data-name-1 keys: a SOURCE-form key (Item null) is no data item, so no operand's byte positions are
        // it (§2023:14.9.41.3 SR6b.1 "defined without the SOURCE phrase"; kb/Work PB1099).
        if (file.PrimeKey is { Item: { } prime } primeKey && OffsetOf(prime, primeKey.Subscripts) == off && admits(prime)) return -1;
        for (int i = 0; i < file.AlternateKeys.Count; i++)
            if (file.AlternateKeys[i].Key is { Item: { } alt } altKey && OffsetOf(alt, altKey.Subscripts) == off && admits(alt)) return i;
        return null;
    }

    /// <summary>ISO §2023:14.9.41.3 SR6b.2 — <i>"It has the same class, category, and usage as that record key."</i>
    /// Class comes from the ONE §2023:8.5.2.1 Table-2 classifier (<see cref="IntrinsicArgumentRules.ClassOfItem"/>),
    /// never a second copy of that table, and category from the ONE spelling of a Table 2 category
    /// (<see cref="ItemCategory.CategoryName(PicInfo)"/>), which tells alphanumeric-edited and national-edited from
    /// the plain categories the model folds them into (kb/Work PB850: compared as a bare <see cref="PicCategory"/>,
    /// an alphanumeric-edited operand passed as the same category as an alphanumeric key).</summary>
    private static bool SameClassCategoryUsage(DataItem operand, DataItem key) =>
        IntrinsicArgumentRules.ClassOfItem(operand) == IntrinsicArgumentRules.ClassOfItem(key)
        && CategoryOfItem(operand) == CategoryOfItem(key)
        && UsageOfItem(operand) == UsageOfItem(key);

    /// <summary>The item's §2023:8.5.2.1 Table 2 category name: its PICTURE's (or a bit/national group's as-if
    /// PICTURE's), else — for an ordinary group, which has no PICTURE at all — category alphanumeric (§2023:13.18.29.4
    /// GR3, "an alphanumeric group item"). A group key with an elementary operand at its leftmost position is
    /// exactly the generic-key shape SR6 b) is written for, so the group arm may not answer "no category".
    /// <para>⚠ NOT <see cref="Place.CategoryOf"/>, which looks similar and answers a DIFFERENT question: that is
    /// §2023:8.4.3.3.4 GR6's rule for the category a REFERENCE-MODIFIED view takes (numeric becomes alphanumeric, and
    /// so on). SR6 b) 2. compares the items' OWN categories, so folding the two would import a rule about
    /// reference modification into a rule about record keys.</para></summary>
    private static string CategoryOfItem(DataItem item) =>
        item.OperandPic is { } pic ? ItemCategory.CategoryName(pic) : ActivationCategory.Alphanumeric;

    /// <summary>The item's USAGE (ISO §2023:13.18.60), through THE ONE §2023:8.5.2.1 usage reader
    /// (<see cref="ItemCategory.UsageOf"/>): the PICTURE's resolved usage for an elementary item, the as-if
    /// PICTURE's for a bit / national group, and DISPLAY for an alphanumeric group — "An alphanumeric group item
    /// is treated as though it had a usage of display" (kb/Work PB411's sibling sweep; the former
    /// <c>?? item.OwnUsage</c> gave <c>01 G USAGE COMP.</c> the answer BINARY, which §2023:13.18.60.4 GR1 settles
    /// outright — "Unless the GROUP-USAGE clause is also specified or implied, the USAGE clause applies only to
    /// each elementary item in the group and not to the group itself"). The tail
    /// survives for the group kinds §2023:3.11 excludes from "alphanumeric group item", where the standard states no
    /// usage and SR6 b) 2. still has to compare something: the entry's own keyword, else DISPLAY — GR2's
    /// standard data format, the usage an entry with no clause has.</summary>
    private static Usage UsageOfItem(DataItem item) =>
        ItemCategory.UsageOf(item) ?? item.OwnUsage ?? Usage.Display;
}
