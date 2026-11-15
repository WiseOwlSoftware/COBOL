// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.IO;

namespace WiseOwl.Cobol.Binding;

/// <summary>One step of a record description entry's <see cref="RecordPlanModel"/>: a window of its byte image and what the item
/// there is — the compile-time twin of the runtime's <c>RecordStep</c>, from which the emitter renders it.</summary>
/// <param name="Kind">What the window is (<see cref="RecordStepKind"/>).</param>
/// <param name="Offset">The first byte of the window in the record's image.</param>
/// <param name="Length">The bytes of the window.</param>
/// <param name="Pic">The numeric item's picture and usage — the profile the runtime decodes it by — for the kinds that carry a value.</param>
/// <param name="Bits">For a bit run, the absolute start bit and the bit count of each item, two integers for each.</param>
internal sealed record PlanStep(RecordStepKind Kind, int Offset, int Length, PicInfo? Pic = null, int[]? Bits = null);

/// <summary>A record description entry as the steps of its byte image (<see cref="RecordPlans.Of"/>).</summary>
internal sealed record RecordPlanModel(int Width, IReadOnlyList<PlanStep> Steps);

/// <summary>
/// ⛔ THE ONE LAYOUT OF A RECORD DESCRIPTION ENTRY AS FORMATTABLE ITEMS — the elementary data description entries of the entry
/// (ISO §2023:13.18.24.4 GR3: "Elementary data description entries of the selected record description entry are used to determine
/// the data items to be formatted"; §2023:13.18.13.4 GR4: "each data item described with usage display in the selected record
/// description entry") as the contiguous, non-overlapping steps of its byte image that the runtime's
/// <see cref="RecordFormat"/> translates. Which kind of item selects which step is the FORMAT clause's (GR4 - GR6) and the
/// runtime's; this walk only says what each item IS, and where it is.
/// <para><b>Occurrences.</b> A table's every occurrence is a step of its own (GR3 reads the elementary entries; each occurrence is
/// a data item, §2023:8.5.1.8), at the first occurrence's offset plus the occurrence stride. A table whose size depends on a data item has
/// no fixed layout, so its record has no plan: <see cref="HasVariableLayout"/> is what the binder refuses.</para>
/// <para><b>Overlap.</b> Under FORMAT no item redefines another (SR2), so no two items overlap. A SELECT WHEN record under a CODE-SET
/// clause may redefine; its bytes are converted ONCE, as the item that comes first in memory order and then in the order written.</para>
/// </summary>
internal static class RecordPlans
{
    /// <summary>Whether the entry holds a table whose occurrences depend on a data item (OCCURS DEPENDING ON) or are dynamic — its
    /// image then has no fixed layout a plan can describe.</summary>
    public static bool HasVariableLayout(DataItem record) =>
        FileModel.IsVariableLengthRecord(record)
        || Descendants(record).Any(d => d.OccursSpec is { IsDynamic: true } or { DependingName: not null } || d.IsDynamicTable);

    private static IEnumerable<DataItem> Descendants(DataItem item)
    {
        yield return item;
        foreach (var child in item.Children)
            foreach (var d in Descendants(child)) yield return d;
    }

    /// <summary>The plan of one record description entry, whose image is <see cref="RecordLayout.PhysicalWidth"/> bytes.</summary>
    public static RecordPlanModel Of(DataItem record)
    {
        var windows = new List<(int StartBit, int Bits, DataItem Leaf)>();
        Collect(record, record, 0, windows);
        // Memory order, then the order written (a stable sort over the depth-first walk).
        var ordered = windows.Select((w, i) => (w, i)).OrderBy(p => p.w.StartBit).ThenBy(p => p.i).Select(p => p.w).ToList();

        int width = RecordLayout.PhysicalWidth(record);
        var steps = new List<PlanStep>();
        int at = 0;   // the first byte no step covers
        for (int i = 0; i < ordered.Count; i++)
        {
            var (startBit, bits, leaf) = ordered[i];
            int first = startBit / 8;
            if (first < at) continue;   // overlaps an item already placed: its bytes are converted once
            if (BitLayout.IsBitLeaf(leaf))
            {
                // A run: every following bit item that starts in a byte the run already reaches.
                var run = new List<int> { startBit, bits };
                int end = (startBit + bits - 1) / 8;
                while (i + 1 < ordered.Count && BitLayout.IsBitLeaf(ordered[i + 1].Leaf) && ordered[i + 1].StartBit / 8 <= end)
                {
                    i++;
                    run.Add(ordered[i].StartBit);
                    run.Add(ordered[i].Bits);
                    end = Math.Max(end, (ordered[i].StartBit + ordered[i].Bits - 1) / 8);
                }
                if (first > at) steps.Add(new PlanStep(RecordStepKind.Raw, at, first - at));
                steps.Add(new PlanStep(RecordStepKind.BitRun, first, end - first + 1, Bits: [.. run]));
                at = end + 1;
                continue;
            }
            int length = leaf.ByteWidth;
            if (length <= 0) continue;
            if (first > at) steps.Add(new PlanStep(RecordStepKind.Raw, at, first - at));
            steps.Add(new PlanStep(KindOf(leaf), first, length, leaf.Pic));
            at = first + length;
        }
        if (at < width) steps.Add(new PlanStep(RecordStepKind.Raw, at, width - at));
        return new RecordPlanModel(width, Merge(steps));
    }

    /// <summary>Adjacent raw steps are one raw window.</summary>
    private static List<PlanStep> Merge(List<PlanStep> steps)
    {
        var merged = new List<PlanStep>();
        foreach (var step in steps)
            if (step.Kind is RecordStepKind.Raw && merged.Count > 0 && merged[^1].Kind is RecordStepKind.Raw)
                merged[^1] = merged[^1] with { Length = merged[^1].Length + step.Length };
            else merged.Add(step);
        return merged;
    }

    /// <summary>The elementary items beneath <paramref name="item"/>, with the bit each starts at in the record — every occurrence of
    /// a table.</summary>
    private static void Collect(DataItem record, DataItem item, int bitDelta, List<(int, int, DataItem)> windows)
    {
        if (item.Occurs is { } occurs)
        {
            int stride = item.HasBitDescendant ? BitLayout.StrideBits(item) : RecordLayout.PhysicalOccurrenceWidth(item) * 8;
            for (int k = 0; k < occurs; k++) CollectOne(record, item, bitDelta + k * stride, windows);
        }
        else CollectOne(record, item, bitDelta, windows);
    }

    private static void CollectOne(DataItem record, DataItem item, int bitDelta, List<(int, int, DataItem)> windows)
    {
        if (item.IsElementary)
        {
            bool bit = BitLayout.IsBitLeaf(item);
            // A record holding usage bit items is laid out by the bit walk (§2023:8.5.1.6.3), which places every item of it, the characters after
            // a run of bits included; any other record by the byte walk (an overlay the bit walk cannot place falls back to it).
            int start = record.HasBitDescendant ? BitLayout.StartBitOf(record, item) : -1;
            if (start < 0 && bit) return;   // a bit item the layout walk cannot place (reported where the layout is)
            if (start < 0) start = (RecordLayout.OffsetInRootWithEveryIndexOne(item) ?? 0) * 8;
            // Both walks answer the FIRST occurrence's place, so a table's later occurrences are all in the delta.
            windows.Add((start + bitDelta, bit ? item.Pic!.Length : item.ByteWidth * 8, item));
            return;
        }
        foreach (var child in item.Children) Collect(record, child, bitDelta, windows);
    }

    /// <summary>What the runtime does with an elementary item: how it is formatted when a FORMAT kind selects it, and which
    /// alphabet converts it (§2023:13.18.24.4 GR4 - GR6; §2023:13.18.13.4 GR4).</summary>
    private static RecordStepKind KindOf(DataItem leaf)
    {
        if (leaf.Pic is not { } pic) return RecordStepKind.Raw;
        return pic.Category switch
        {
            // Class alphabetic and alphanumeric, numeric-edited (class alphanumeric): characters of usage display.
            PicCategory.Alphanumeric or PicCategory.NumericEdited => pic.Usage is Usage.Display ? RecordStepKind.Display : RecordStepKind.Raw,
            PicCategory.Boolean => pic.Usage is Usage.Display ? RecordStepKind.Display : RecordStepKind.Raw,
            PicCategory.Numeric => pic.Usage switch
            {
                Usage.Display => RecordStepKind.Zoned,
                Usage.National => RecordStepKind.NationalNumeric,
                Usage.Float or Usage.Double or Usage.FloatShort or Usage.FloatLong or Usage.FloatExtended
                    or Usage.FloatBinary32 or Usage.FloatBinary64 => RecordStepKind.Float,
                _ => pic.ByteForm is NumericByteForm.None ? RecordStepKind.Raw : RecordStepKind.Numeric,
            },
            // A national item crosses the medium as its byte pairs: no alphabet of this implementation converts them.
            _ => RecordStepKind.Raw,
        };
    }
}
