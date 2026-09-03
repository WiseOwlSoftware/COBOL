// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>
/// The strong-typing model over the <see cref="DataItem"/> tree (ISO/IEC 1989:2023 §8.5.3 / §13.18.58 STRONG
/// TYPEDEF; data-model D17) — the §8.5.3.3 same-type test and the tree-walking predicates behind the MOVE /
/// relation / class-condition / REDEFINES / RENAMES gates. Extracted off <c>DataItem</c> (P5.11b,
/// DESIGN-data-model §2.4): strong typing is a USE-RESTRICTION overlay consulted at a handful of check sites,
/// not core record shape — <c>DataItem</c> keeps only the stored facts (<see cref="DataItem.StrongType"/>,
/// <see cref="DataItem.TypeName"/>) that <c>ExpandTypes</c> writes.
/// </summary>
public static class StrongTypeModel
{
    /// <summary>The outermost enclosing item (the item itself or an ancestor) whose data description is strongly
    /// typed — i.e. carries, or is subordinate to, a TYPE clause referencing a STRONG type declaration (ISO
    /// §8.5.3.1). Null when the item is not part of any strongly-typed subtree. Backs the §8.5.3.3
    /// use-restriction gates and the §8.5.3 same-type test.</summary>
    public static DataItem? StrongRoot(DataItem item)
    {
        DataItem? root = null;
        for (DataItem? cur = item; cur is not null; cur = cur.Parent)
            if (cur.StrongType) root = cur;
        return root;
    }

    /// <summary>True when the item is a strongly-typed GROUP — the operand form the MOVE / comparison /
    /// class-condition same-type gates restrict (ISO §8.5.3.3: only group items may be strongly typed;
    /// §14.9.25.3 SR2 / §8.8.4.2.3 SR1 / §8.8.4.4.3 SR1). An elementary leaf subordinate to a strong group is
    /// NOT strongly typed, so its individual MOVE / comparison is unrestricted (a strong record is still built
    /// up field by field).</summary>
    public static bool IsStrongGroup(DataItem item) => item.IsGroup && StrongRoot(item) is not null;

    /// <summary>True when the item is part of any strongly-typed subtree (a strong group OR a leaf subordinate
    /// to one) — backs the §13.18.57.3 SR3/SR4 "in whole or in part" declaration checks (a RENAMES / REDEFINES
    /// touching any part of a strong item is prohibited).</summary>
    public static bool IsStronglyTyped(DataItem item) => StrongRoot(item) is not null;

    /// <summary>The NEAREST enclosing item (the item itself or an ancestor) that directly carries a TYPE clause
    /// — the item whose <see cref="DataItem.TypeName"/> it acquired. This is the item's "type" for the §8.5.3
    /// same-type test: a nested <c>TYPE INNER-T</c> subgroup is anchored by INNER-T (itself), NOT by the
    /// outermost strong record. Null when the item is not part of any typed subtree.</summary>
    public static DataItem? TypeAnchor(DataItem item)
    {
        for (DataItem? cur = item; cur is not null; cur = cur.Parent)
            if (cur.TypeName is not null) return cur;
        return null;
    }

    /// <summary>Two operands are of the SAME type (ISO §8.5.3 / §8.5.3.3) when their NEAREST TYPE anchors
    /// reference equivalent type declarations — within one source element, identically-named ones
    /// (cross-program EXTERNAL equivalence is a follow-up) — and each operand occupies the identical relative
    /// position within that type (the §8.5.3 "same subordinate item in equivalent type declarations" rule; both
    /// are clones of one template, so corresponding items share a member-name path from the anchor down). Uses
    /// <see cref="TypeAnchor"/>, not <see cref="StrongRoot"/>, so a nested strong subgroup is matched by ITS
    /// OWN type, not the enclosing record's.</summary>
    public static bool SameStrongType(DataItem a, DataItem b)
    {
        if (TypeAnchor(a) is not { } ra || TypeAnchor(b) is not { } rb) return false;
        if (!string.Equals(ra.TypeName, rb.TypeName, StringComparison.OrdinalIgnoreCase)) return false;
        return RelativeMemberPath(a, ra).SequenceEqual(RelativeMemberPath(b, rb), StringComparer.Ordinal);
    }

    // ── RESTRICTED DATA-POINTERS (ISO Annex D.9.2.2; §13.18.60.4 GR23, §8.4.3.11.4 GR2) ─────────────────────
    // Annex D.9.2.2 names the model's TWO sources outright: a restricted data-pointer arises "1) by specifying a
    // data description entry that contains a usage clause of the form USAGE POINTER TO type-name-1, or 2) by
    // specifying a data-address-identifier (ADDRESS OF identifier-1, where identifier-1 is a strongly typed group
    // item or another restricted data-pointer)". The SECOND source needs no grammar at all and was violable the
    // day strong TYPEDEF landed — the whole strong-type use-restriction network was built out across MOVE / CALL /
    // ACCEPT / STRING / REDEFINES / RENAMES / intrinsics and the POINTER subsystem was never wired in (kb/Work
    // PB153). Both sources are reduced HERE to one thing — a type NAME — so the consumption screens
    // (§14.9.3.3 SR4/SR5, §14.9.39.3 SR19/SR20, §14.8.2.3.2) all ask the same question of the same model.

    /// <summary>The type a DATA-POINTER ITEM's value is restricted to — Annex D.9.2.2 source 1, the declared
    /// <c>USAGE POINTER TO type-name-1</c> (§13.18.60.4 GR23). Null for an ordinary data-pointer, and for any
    /// item that is not a data-pointer at all.</summary>
    public static string? PointerRestriction(DataItem item) =>
        item.Pic is { Category: PicCategory.Pointer } pic ? pic.RestrictedTypeName : null;

    /// <summary>The type an <c>ADDRESS OF identifier-1</c> VALUE is restricted to — Annex D.9.2.2 source 2, whose
    /// normative statement is §8.4.3.11.4 GR2: "If identifier-1 is a strongly-typed group item or a restricted
    /// data-pointer, the data-address-identifier is a restricted data-pointer that is restricted to the type of
    /// identifier-1." Null when the operand is neither, i.e. the address is unrestricted.
    /// <para>⛔ The strong-group arm deliberately uses <see cref="IsStrongGroup"/>, so a LEAF subordinate to a
    /// strong group yields null: <c>SET P TO ADDRESS OF &lt;leaf of a strong record&gt;</c> is UNRESTRICTED and
    /// must stay legal. That asymmetry is the model's own (a strong record is still built up field by field), and
    /// an over-eager screen here would break it.</para></summary>
    public static string? AddressOfRestriction(DataItem operand) =>
        IsStrongGroup(operand) ? TypeAnchor(operand)?.TypeName : PointerRestriction(operand);

    /// <summary>"Restricted to the same type" (§14.9.39.3 SR19, §14.8.2.3.2, §14.9.3.3 SR4/SR5). A restriction is
    /// to a TYPE, not to a member position, so this is type-NAME equivalence and NOT
    /// <see cref="SameStrongType"/>, whose relative-member-path refinement answers the different question of
    /// whether two OPERANDS occupy corresponding positions in equivalent type declarations.
    /// <para>Equivalence is within one source element, identically-named — the same scope
    /// <see cref="SameStrongType"/> documents, with cross-program EXTERNAL equivalence deferred. §14.8.2.3.2's
    /// CALL rule is inherently cross-program, so its screen is scoped to within-element operands and must not
    /// over-reject on the deferred axis.</para></summary>
    public static bool SameRestriction(string? a, string? b) =>
        a is not null && b is not null && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    /// <summary>The type a strongly-typed group item IS (§14.9.3.3 SR5's "the type of data-name-1"), or null when
    /// the item is not a strongly-typed group.</summary>
    public static string? StrongGroupType(DataItem item) =>
        IsStrongGroup(item) ? TypeAnchor(item)?.TypeName : null;

    /// <summary>The member-name path from <paramref name="root"/> (exclusive) down to <paramref name="item"/>
    /// (inclusive), root-first — the operand's relative position within its strong type.</summary>
    private static List<string> RelativeMemberPath(DataItem item, DataItem root)
    {
        var path = new List<string>();
        for (DataItem? cur = item; cur is not null && !ReferenceEquals(cur, root); cur = cur.Parent)
            path.Add(cur.CsName);
        path.Reverse();
        return path;
    }
}
