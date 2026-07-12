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
    internal readonly record struct Physical(string Name, string Type, int Width, bool IsGroupStruct, string Init, string Comment, int Occurs = 0, DataItem? NumLeaf = null);

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
        foreach (var c in items)
        {
            if (!(c.IsGroup || c.IsElementary)) continue;
            if (c.Class is { Tier: RedefinesTier.StringCanonical } cls)
            {
                // The whole redefines class is ONE string backing (the canonical's VALUE seeds it, SR9); every member
                // is a window over it. A non-canonical Tier-B member yields no field.
                if (c.IsCanonical)
                    yield return new Physical(cls.BackingCsName, "string", cls.Width, false,
                        $"CobolString.Store({Codec.ImageInitOf(c)}, {cls.Width})", $"REDEFINES backing for {c.CobolName}");
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
            yield return new Physical(c.CsName, c.FieldType, width, c.IsGroup, Values.FieldInit(c), comment, occurs, numLeaf);
        }
    }

    /// <summary>The emitted character-image width of an item: a leaf's own width; a group's = the sum of its physical
    /// fields (a contained REDEFINES class contributes its single backing width once, its views nothing).</summary>
    private int PhysicalImageWidth(DataItem item) =>
        item.IsGroup ? PhysicalChildrenOf(item).Sum(f => f.Width) : item.ImageWidth;

}
