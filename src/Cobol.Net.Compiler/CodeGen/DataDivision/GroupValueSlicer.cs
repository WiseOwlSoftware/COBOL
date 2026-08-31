// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

/// <summary>The group-VALUE positional distributor (P7 Step 9l; ISO §13.18.63): a GROUP-level VALUE
/// initializes the whole area as ONE alphanumeric value, sliced positionally over the (distributable)
/// subtree at compile time; otherwise the member-wise composed initializer. Wired by <see cref="DataEmitter"/>.</summary>
internal sealed class GroupValueSlicer(EmitContext ctx, PhysicalModel phys)
{
    public string ComposedInit(DataItem group)
    {
        // A GROUP-level VALUE initializes the whole AREA (ISO §13.18.63.4 GR5) — the ONE area rule lives in
        // AreaTextOf; here the area text distributes over the subordinate items POSITIONALLY at compile time
        // (NC104A `01 MOVE29A VALUE "$123.45". 02 MOVE30 PIC $999.99.`). Distribution requires every leaf
        // string-stored and no shared-storage member in the subtree; anything else keeps the member-wise
        // default (a SHARED-STORAGE subtree is not a loss — its Tier-B / EXTERNAL / BASED backing is seeded
        // by GroupImageCodec.ImageInitOf, which applies the SAME AreaTextOf rule to the same group).
        if (AreaTextOf(group, ctx) is { } text && DistributableSubtree(group))
            return SliceInit(group, text);
        var parts = phys.PhysicalChildrenOf(group).Select(f => $"{f.Name} = {f.Init}");
        return $"new {group.StructName} {{ {string.Join(", ", parts)} }}";
    }

    /// <summary>⛔ THE §13.18.63.4 GR5 GROUP-VALUE AREA RULE — the one place that says what a group-level VALUE
    /// puts in the group's storage, ridden by BOTH initializer lanes: this slicer (the typed-native record-struct
    /// fields) and <see cref="GroupImageCodec.ImageInitOf"/> (the character-image backings — Tier-B REDEFINES,
    /// EXTERNAL cells, BASED records, OO cells). Returns the area's CHARACTERS, space-padded / right-truncated
    /// to the group's image width per GR7 (§14.6.8 alphanumeric alignment — left-justified, space fill; no
    /// editing, and JUSTIFIED has no effect on initialization), or null when the entry carries no VALUE or an
    /// operand form that is not an area fill.
    ///
    /// <para>GR5 is "the group area is initialized without consideration for the individual elementary or group
    /// items contained within this group" — so the deposit is positional and the members are whatever their own
    /// descriptions make of the characters underneath them. That reading is exact because §13.18.63.3 SR14
    /// requires every data item subordinate to an alphanumeric group item carrying a VALUE to be usage DISPLAY
    /// (screened by <c>DataBinder.CheckGroupValueDeclarations</c>, COBOLNET1702): the character image IS the
    /// byte image for that whole population, so there is no character/byte width question to answer here.</para>
    ///
    /// <para><b>THREE operand forms, all of them area fills</b> (kb/Work PB184's sweep — the figurative arm was
    /// MISSING and it was a live silent wrong answer: `01 GZ VALUE ZEROS. 05 Z1 PIC X(2). 05 Z2 PIC X(2).`
    /// measured as four SPACES, where GR5 requires four '0' characters, because the group fell through to the
    /// member-wise default and each member took its own category default). §8.3.3.6.4 GR2 is the ONE rule for
    /// the repeat: it is the branch for the case "the length of the string is specified in the rules for the
    /// context", it names the VALUE clause explicitly, and it gives both the repeat-to-width and the
    /// truncate-from-the-right this method implements. (GR3 is the complementary branch — the length NOT
    /// specified — and its sub-rule a is scoped to a concatenation expression; neither governs here, because
    /// §13.18.63.4 GR5 makes the group area the receiving field and so specifies the length.)</para>
    /// <list type="number">
    ///   <item>a FIGURATIVE constant word, with or without ALL — its one character repeated to the group's width
    ///     (§8.3.3.6.4 GR2; the character itself is GR1's, with the per-format GR4–GR8 identities; SR13 admits
    ///     the operand as "a figurative constant that is permitted in a MOVE statement to a receiving item of
    ///     that category"). The fill character comes from the ONE figurative service, with the group's own
    ///     category so a national / bit group reads ITS anchor rather than the alphanumeric PCS.</item>
    ///   <item><c>ALL "literal"</c> — literal-1 repeated to the width (§8.3.3.6.4 GR2, the same rule).</item>
    ///   <item>a quoted literal — its characters, decoded.</item>
    /// </list></summary>
    internal static string? AreaTextOf(DataItem group, EmitContext ctx)
    {
        if (group.RawValue is not { } raw) return null;
        // ⛔ A BIT-PACKED group has no character area for GR5 to deposit into: its width is ceil(bits/8) laid
        // out by the §8.5.1.6.3 walk and the members do not tile it, so a positional CHARACTER slice would seed
        // the wrong bit run (it crashed the slicer on a multi-member bit group and stored one boolean position
        // on a single-member one — kb/Work PB207, which carries the boolean-position area rule). The predicate
        // is HasBitDescendant, the FACT that switches DataItem.ImageWidth to the bit walk — not GROUP-USAGE BIT,
        // which is only the commonest way to acquire it. ONE exclusion here, for BOTH lanes; the binder rejects
        // the shape first (DiagnosticCatalog.BitGroupLevelValue) and this keeps the emitter total regardless.
        if (group.HasBitDescendant) return null;
        int width = group.ImageWidth;
        if (width <= 0) return null;
        string? text =
            ValueInitializer.FigurativeKind(raw) is { } kind
                ? new string(FigurativeConstants.FillChar(kind, ctx.Data.Collating,
                        group.AsIfPic?.Category ?? PicCategory.Alphanumeric, ctx.Data.NationalCollating), width)
            : EmitText.AllLiteralText(raw) is { } all ? EmitText.RepeatToWidth(all, width)
            : CobolLiteral.IsStringLiteral(raw) ? CobolLiteral.Decode(raw)
            : null;
        if (text is null) return null;
        return text.Length >= width ? text[..width] : text.PadRight(width);
    }

    /// <summary>Whether the group's subtree can take the area text as a compile-time POSITIONAL slice per
    /// subordinate. Every admitted leaf is string-stored or a native zoned-DISPLAY numeric, so its slice IS its
    /// own image and the offsets are character offsets.
    ///
    /// <para>⚠ The usage arms below are NOT a Tier-C boundary and are no longer a known gap (they were read as
    /// one, and PB184 was registered against them). §13.18.63.3 SR14 makes a BINARY / PACKED / float / INDEX /
    /// NATIONAL-usage / BIT leaf under an alphanumeric group item carrying a VALUE non-conforming, and
    /// <c>DataBinder.CheckGroupValueDeclarations</c> now rejects it (COBOLNET1702) — so this predicate is
    /// answering about a population the binder has already screened, and it is kept as the defensive statement
    /// of what a positional character slice can mean. The <c>Class is null</c> arm is the live one: a
    /// shared-storage (REDEFINES-class) subtree is seeded through its BACKING by
    /// <see cref="GroupImageCodec.ImageInitOf"/> instead, from the same <see cref="AreaTextOf"/> rule.</para></summary>
    private static bool DistributableSubtree(DataItem item) =>
        item.Class is null && (item.IsGroup
            ? item.Children.All(DistributableSubtree)
            : item.StoreAsImage || item.Pic?.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited
                or PicCategory.National or PicCategory.Boolean   // string-stored — the slice is the chars (D-N4 identity)
              || item.Pic is { Category: PicCategory.Numeric, Usage: Usage.Display, IsFloat: false });

    /// <summary>Build the composed initializer of <paramref name="item"/> from its positional <paramref name="slice"/>
    /// of the group VALUE text — each subordinate (and each OCCURS occurrence) takes its own window.</summary>
    private static string SliceInit(DataItem item, string slice)
    {
        // A native (long-stored) numeric-DISPLAY leaf decodes its zoned slice to the unscaled value (sign-aware
        // overpunch/separate decode — the same ParseDisplay every image read uses). String-stored leaves
        // (alphanumeric / edited / StoreAsImage) keep the characters.
        if (!item.IsGroup && !item.StoreAsImage
            && item.Pic is { Category: PicCategory.Numeric, Usage: Usage.Display, IsFloat: false })
            return $"({item.ElementType}){RuntimeApi.NumParseDisplay(EmitText.CsLiteral(slice), item.ProfileName)}";
        if (!item.IsGroup) return EmitText.CsLiteral(slice);
        var parts = new List<string>();
        int off = 0;
        foreach (var c in item.Children)
        {
            int w = c.ImageWidth;
            if (c.Occurs is { } n)
            {
                var elems = new List<string>();
                for (int k = 0; k < n; k++) elems.Add(SliceInit(c, slice.Substring(off + k * w, w)));
                parts.Add($"{c.CsName} = new {c.ElementType}[] {{ {string.Join(", ", elems)} }}");
                off += w * n;
            }
            else
            {
                parts.Add($"{c.CsName} = {SliceInit(c, slice.Substring(off, w))}");
                off += w;
            }
        }
        return $"new {item.StructName} {{ {string.Join(", ", parts)} }}";
    }

}
