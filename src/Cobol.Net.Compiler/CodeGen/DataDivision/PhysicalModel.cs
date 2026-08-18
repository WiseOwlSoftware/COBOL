// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

/// <summary>The memoized physical-field model of the DATA DIVISION (P7 Step 9l — the 5th concern of the
/// former FieldEmitter, given its own home per the phase doc): which C# fields a group/root forest actually
/// emits (REDEFINES views collapse to their class backing) and each field's image-width contribution. The
/// memoization turns the otherwise-exponential nested-group emission into linear time (the ~49-level CCVS
/// nesting). Wired by <see cref="DataEmitter"/> (Values/Codec are the composed-initializer back-edges).</summary>
internal sealed class PhysicalModel(EmitContext ctx)
{
    /// <summary>Memoized physical-field list per group item — the cache that turns the otherwise-exponential
    /// nested-group emission (width and init recursively recomputing each other) into linear time. The root forest
    /// is cached separately in <see cref="_rootPhysCache"/>.</summary>
    private readonly Dictionary<DataItem, IReadOnlyList<Physical>> _physCache = [];
    private IReadOnlyList<Physical>? _rootPhysCache;

    /// <summary>The composed-initializer back-edges (set once by <see cref="DataEmitter"/> — the physical
    /// model, the VALUE initializers, and the image codec are mutually recursive by design: a field's Init is
    /// a VALUE initializer, a Tier-B backing's seed is an image init, and both walk the physical model).</summary>
    public ValueInitializer Values { get; set; } = null!;
    public GroupImageCodec Codec { get; set; } = null!;

    /// <summary>A field that physically appears in the emitted C# — an item's own field, OR a REDEFINES class's single
    /// string backing (which replaces ALL the class's members). A REDEFINES <i>view</i> yields no physical field
    /// (ISO §13.18.44; COBOLNET_DESIGN §4.1 — never two stored fields per storage area).</summary>
    /// <summary>One emitted struct field. <paramref name="Width"/> is the field's TOTAL contribution to its group's
    /// character image — element-image-width × <paramref name="Occurs"/> for a fixed-OCCURS table, else the item's own
    /// image width. <paramref name="Occurs"/> is the fixed occurrence count (0 = not a table), so the image facility
    /// knows to concat/distribute across the array's elements. <paramref name="NumLeaf"/> is the source leaf when the
    /// field stores a NATIVE fixed-point numeric (<c>long</c>/<c>Int128</c>; DISPLAY, BINARY, or PACKED usage) — the
    /// image facility then encodes/decodes it through <c>CobolNum.FormatDisplay</c>/<c>ParseDisplay</c> with the
    /// leaf's IMAGE profile (<see cref="ImageProfileOf"/>); null for every string-shaped field (alphanumeric, edited,
    /// <see cref="DataItem.StoreAsImage"/>, a Tier-B class backing) and for nested group structs.</summary>
    /// <param name="BitRun">⛔ Set ONLY on the FIRST field of a <c>USAGE BIT</c> run, and it is what lets a run
    /// span several FIELDS while occupying ONE image slice (design D19, fix-queue PB43). §8.5.1.6.3 puts two
    /// same-level bit items at successive BIT positions — they SHARE a byte — so their image cannot be composed
    /// field-by-field the way every other member is. The run's leaves are listed here, the first field carries the
    /// whole run's <see cref="Width"/> (<c>ceil(bits/8)</c>) and emits one <c>CobolBits.Pack</c> over the
    /// concatenated carriers, and the remaining fields carry <c>Width = 0</c> and contribute nothing — so the
    /// group's image width still falls out of a plain sum and no caller needs to know runs exist. Null on every
    /// non-bit field and on the continuation fields of a run.</param>
    internal readonly record struct Physical(string Name, string Type, int Width, bool IsGroupStruct, string Init, string Comment, int Occurs = 0, DataItem? NumLeaf = null, IReadOnlyList<DataItem>? BitRun = null);

    /// <summary>The memoized physical fields of a group's children (the root forest under the sentinel).</summary>
    public IReadOnlyList<Physical> PhysicalChildrenOf(DataItem owner)
    {
        if (_physCache.TryGetValue(owner, out var cached)) return cached;
        var list = BuildPhysicals(owner.Children).ToList();
        _physCache[owner] = list;
        return list;
    }

    /// <summary>The memoized physical fields of the top-level (01/77) forest.</summary>
    public IReadOnlyList<Physical> RootPhysicals() => _rootPhysCache ??= BuildPhysicals(ctx.Data.Roots).ToList();

    /// <summary>The physical fields a run of sibling items emits: skip REDEFINES views; substitute a Tier-B class's ONE
    /// string backing (emitted once, at the canonical) for the whole class; a Tier-A view forwards to its canonical's
    /// field. The class's numeric <c>NumProfile</c>s are still emitted elsewhere (EmitProfiles — D9).</summary>
    private IEnumerable<Physical> BuildPhysicals(IEnumerable<DataItem> items)
    {
        // D19/PB43 — the §8.5.1.6.3 bit RUNS in this sibling list, keyed by the leaf that starts each one. A run is
        // a maximal stretch of consecutive USAGE BIT leaves at the SAME level: exactly the items the standard puts
        // at successive bit positions, i.e. the ones that share bytes. Computed once here so the emit loop below
        // can stay a straight per-child walk.
        var siblings = items as IList<DataItem> ?? items.ToList();
        var runStart = new Dictionary<DataItem, List<DataItem>>(ReferenceEqualityComparer.Instance);
        var inRun = new HashSet<DataItem>(ReferenceEqualityComparer.Instance);
        for (int i = 0; i < siblings.Count; i++)
        {
            // A run member is a bit LEAF or a bit GROUP (§8.5.1.6.3 rule 1 names "an elementary bit data item or bit
            // group item of the same level" — D20/PB79); a bit group contributes its AsBits() carrier and its exact
            // extent. Only siblings WITHIN a group form runs — level-01 records never share a byte.
            static bool RunMember(DataItem x) =>
                BitLayout.IsBitLeaf(x) || (x.GroupUsage is GroupUsage.Bit && x.Parent is not null);
            if (!RunMember(siblings[i]) || siblings[i].RedefinesTargetName is not null
                || inRun.Contains(siblings[i])) continue;
            var run = new List<DataItem> { siblings[i] };
            for (int j = i + 1; j < siblings.Count && RunMember(siblings[j])
                                && siblings[j].RedefinesTargetName is null
                                && siblings[j].Level == siblings[i].Level; j++)
                run.Add(siblings[j]);
            foreach (var m in run) inRun.Add(m);
            runStart[siblings[i]] = run;
        }

        foreach (var c in siblings)
        {
            if (!(c.IsGroup || c.IsElementary)) continue;
            if (c.Class is { Tier: RedefinesTier.StringCanonical } cls)
            {
                // The whole redefines class is ONE string backing (the canonical's VALUE seeds it, SR9); every member
                // is a window over it. A non-canonical Tier-B member yields no field.
                if (c.IsCanonical)
                    yield return new Physical(cls.BackingCsName, "string", cls.Width, false,
                        RuntimeApi.StrStore(Codec.ImageInitOf(c), $"{cls.Width}"), $"REDEFINES backing for {c.CobolName}");
                continue;
            }
            if (c.Class is { Tier: RedefinesTier.Alias } && !c.IsCanonical)
                continue;   // a Tier-A view forwards to the canonical's stored field
            string comment = c.CobolName is { } n ? $"{n}{(c.Occurs is { } o ? $" OCCURS {o}" : "")}" : "FILLER";
            // For a group child, use its PHYSICAL image width (skips its own redefines views, counts a contained
            // backing once) — the raw DataItem.ImageWidth over-counts a group that contains a redefines class. A fixed
            // OCCURS table contributes its per-occurrence image width × the count to the group image (ISO §14.9).
            int elemWidth = c.IsGroup ? PhysicalImageWidth(c) : c.ImageWidth;
            int occurs = c.Occurs ?? 0;
            int width = occurs > 0 ? elemWidth * occurs : elemWidth;
            // A NATIVE fixed-point numeric field (the ElementType test excludes the string-stored shapes —
            // StoreAsImage leaves, edited items — and floats): its slice of the group image is the zoned digit
            // form, encoded/decoded by the image methods through CobolNum (COBOLNET_DESIGN §14.4). COMP-5 and
            // INDEX never qualify (excluded by the usage filter; see DataItem.IsImageCapable).
            DataItem? numLeaf = !c.IsGroup && c.ElementType is "long" or "Int128"
                && c.Pic is { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display or Usage.Binary or Usage.Packed }
                ? c : null;
            // D19/PB43 — a USAGE BIT leaf's image is the PACKED run it belongs to, not its own carrier. The run's
            // leader carries the whole run's byte width; a continuation carries 0, so the group's image width is
            // still a plain sum of Width and every downstream caller is unchanged.
            if (runStart.ContainsKey(c) || inRun.Contains(c))
            {
                var run = runStart.TryGetValue(c, out var r) ? r : null;
                int runBits = run?.Sum(m => BitLayout.RunBits(m)) ?? 0;
                // A bit GROUP member keeps its record-struct type (IsGroupStruct) — its own AsImage/FromImage still exist
                // for the standalone (record) case — but inside the run its slice is the run's (D20/PB79).
                yield return new Physical(c.CsName, c.FieldType, run is null ? 0 : BitLayout.Characters(runBits),
                    c.IsGroup, Values.FieldInit(c), comment, occurs, null, run);
                continue;
            }
            yield return new Physical(c.CsName, c.FieldType, width, c.IsGroup, Values.FieldInit(c), comment, occurs, numLeaf);
        }
    }

    /// <summary>The emitted character-image width of an item: a leaf's own width; a group's = the sum of its physical
    /// fields (a contained REDEFINES class contributes its single backing width once, its views nothing).</summary>
    private int PhysicalImageWidth(DataItem item) =>
        item.IsGroup ? PhysicalChildrenOf(item).Sum(f => f.Width) : item.ImageWidth;

}
