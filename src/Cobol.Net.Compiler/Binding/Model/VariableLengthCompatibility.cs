// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;

namespace CobolNet.Binding.Model;

/// <summary>
/// ⛔ THE ONE ISO §8.5.1.12 VARIABLE-LENGTH-GROUP COMPATIBILITY RELATION (kb/Work PB204).
/// <para>§8.5.1.12.1: "a variable-length group is not equivalent to an alphanumeric data item and may not
/// undergo a comparison or a move operation, in either direction, explicitly or otherwise, unless the other
/// operand is a compatible group. Groups are compatible if all variable-length data items correspond and match
/// as specified below." Three named consumers import it verbatim — §14.8.2.2 ("If either the formal parameter or
/// the argument is a variable length group, the formal parameter and the argument shall be compatible, as
/// described in 8.5.1.12"), §14.8.3.2 (the same sentence over a RETURNING pair), and §14.9.4.3 SR25, which makes
/// both apply to a Format-2 CALL. Format 1 needs none of this: SR12 forbids the crossing outright
/// ("Identifier-2 shall not reference a variable-length group"), which is why the relation belongs at the
/// Format-2 / INVOKE boundary and nowhere else.</para>
/// <para>THE THREE RULES, implemented as ONE positional walk rather than three passes, because they are three
/// statements about the SAME left-to-right byte accounting:</para>
/// <list type="number">
/// <item>§8.5.1.12.1 rule 1 + §8.5.1.12.2 sentence 2 — for each dynamic-capacity table in either group there is
/// a corresponding table in the other: "at least one of them is a dynamic-capacity table and they occupy the
/// same relative byte positions within their groups".</item>
/// <item>§8.5.1.12.1 rule 2 + §8.5.1.12.3 sentence 2 — corresponding tables MATCH "when the byte length of
/// their elements is equal and their elements are compatible" (the element compatibility recurses HERE).</item>
/// <item>§8.5.1.12.1 rule 3 + §8.5.1.12.2 sentence 1 — for each dynamic-length elementary item in either group
/// there is a corresponding one in the other, correspondence being "start at the same relative byte positions
/// within their groups"; §8.5.1.12.3 sentence 1 then makes any two of them match "regardless of their
/// definitions".</item>
/// </list>
/// <para>THE BYTE ACCOUNTING IS NOT RE-DERIVED HERE — it is <see cref="DataItem.ByteWidth"/>, which already
/// satisfies §8.5.1.12.3's two collapse conventions BY CONSTRUCTION: a dynamic-length elementary item's
/// <c>ElementaryImageWidth</c> is 0 ("all dynamic-length elementary items are considered to be of zero length"),
/// and a dynamic-capacity table carries no <c>Occurs</c>, so a parent sums <c>ByteWidth * (Occurs ?? 1)</c> =
/// ONE element ("Dynamic-capacity tables that match each other are each considered to be the length of a single
/// element of that table"). The one convention the walk must apply itself is §8.5.1.12.3 sentence 3 — a
/// dynamic-capacity table corresponding to a table that is NOT one "is considered to be the same length as the
/// corresponding table" — which is a fact about a PAIR and therefore cannot live on either item.</para>
/// <para>NESTING IS TRANSPARENT: the relation is stated over relative byte positions within the group, never
/// over its declaration tree, so <see cref="Atoms"/> FLATTENS scalar subordinate groups. A subordinate entry
/// carrying REDEFINES is dropped with its whole subtree — §8.5.1.12.1: "In determining compatibility, any
/// subordinate data items that specify the REDEFINES clause, and all data items subordinate to those data
/// items, are ignored."</para>
/// </summary>
internal static class VariableLengthCompatibility
{
    /// <summary>True for a VARIABLE-LENGTH GROUP (§8.5.1.12.1 sentence 1: "a group item whose data description
    /// has at least one dynamic-length elementary item or dynamic-capacity table as a subordinate item"). The
    /// ONE spelling — <see cref="DataItem.IsImageCapable"/>'s dynamic axis answers the same question about a
    /// single item, and <c>ReferenceResolver.HasVariableLengthSubordinate</c> is the walk both share.</summary>
    public static bool IsVariableLength(DataItem item) =>
        item.IsGroup && ReferenceResolver.HasVariableLengthSubordinate(item);

    /// <summary>The kind of one positional atom of a group's byte layout.</summary>
    private enum AtomKind
    {
        /// <summary>An elementary item that is neither a table nor dynamic-length — pure bytes.</summary>
        Fixed,
        /// <summary>A dynamic-length elementary item (§8.5.1.10) — zero bytes for position purposes.</summary>
        DynamicLength,
        /// <summary>A table (fixed OCCURS, OCCURS DEPENDING, or OCCURS DYNAMIC).</summary>
        Table,
    }

    /// <param name="Kind">Which of the three §8.5.1.12 roles this atom plays.</param>
    /// <param name="Item">The declaring item (its name goes into the diagnostic; a table's element description
    /// is this item, so element compatibility recurses through it).</param>
    /// <param name="Width">The atom's contribution to the relative byte position, under the §8.5.1.12.3
    /// conventions.</param>
    /// <param name="DynamicCapacity">True for an OCCURS DYNAMIC table (§8.5.1.9).</param>
    /// <param name="ElementBytes">One occurrence's byte length — the quantity §8.5.1.12.3 sentence 2 compares.</param>
    /// <param name="ImageChars">The atom's contribution in CHARACTER POSITIONS. Not used by the relation, which
    /// §8.5.1.12 states in bytes — only by <see cref="Signature"/>, whose consumer (the universal-dispatch
    /// descriptor) has to know the CARRIER layout too, and a NATIONAL member is 1 character per 2 bytes.</param>
    private readonly record struct Atom(
        AtomKind Kind, DataItem Item, int Width, bool DynamicCapacity, int ElementBytes, int ImageChars);

    /// <summary>The group's byte layout as a FLAT left-to-right atom sequence: REDEFINES subtrees dropped
    /// (§8.5.1.12.1), scalar subordinate groups flattened (relative byte position is nesting-blind), a table
    /// kept WHOLE (its element description is the recursion subject of §8.5.1.12.3 sentence 2, not something to
    /// flatten through).</summary>
    private static void Atoms(DataItem g, List<Atom> into)
    {
        foreach (var c in g.Children)
        {
            if (c.RedefinesTargetName is not null || !(c.IsGroup || c.IsElementary)) continue;
            if (c.IsDynamicTable)
                into.Add(new Atom(AtomKind.Table, c, c.ByteWidth, DynamicCapacity: true, c.ByteWidth, c.ImageWidth));
            else if (c.Occurs is { } n)
                // A fixed-OCCURS or OCCURS DEPENDING table takes its MAXIMUM length — §14.8.2.2's own sentence
                // for an occurs-depending group passed by reference, and §8.5.1.12.3 sentence 3's "its fixed
                // number of occurrences or the value of the DEPENDING operand, as applicable" resolved
                // statically (the DEPENDING operand's run-time value is not a compile-time quantity).
                into.Add(new Atom(AtomKind.Table, c, c.ByteWidth * n, DynamicCapacity: false, c.ByteWidth,
                    c.ImageWidth * n));
            else if (c.IsDynamicLength)
                into.Add(new Atom(AtomKind.DynamicLength, c, 0, DynamicCapacity: false, 0, 0));
            else if (c.IsGroup)
                Atoms(c, into);
            else
                into.Add(new Atom(AtomKind.Fixed, c, c.ByteWidth, DynamicCapacity: false, c.ByteWidth,
                    c.ImageWidth));
        }
    }

    private static List<Atom> AtomsOf(DataItem g)
    {
        var list = new List<Atom>();
        Atoms(g, list);
        return list;
    }

    /// <summary>A CANONICAL rendering of the group's §8.5.1.12 atom layout: consecutive fixed material
    /// collapsed to one <c>bytes/chars</c> run, a dynamic-length item as <c>D</c>, a dynamic-capacity table as
    /// <c>T</c> plus its element's byte and character widths. Used by the OO UNIVERSAL-dispatch conformance
    /// DESCRIPTOR (§14.9.23.4 GR7c), which must decide conformance at RUN time from two independently compiled
    /// descriptions and therefore cannot run the relation itself.
    /// <para>⚠ EQUAL SIGNATURES IMPLY COMPATIBILITY, NOT THE CONVERSE — a DOCUMENTED STRICTNESS DELTA of the
    /// same family as the descriptor's existing one (a fixed group's <c>S:{width}</c> cannot express §14.8.2.2
    /// rule 1's prefix latitude either). Two shapes the relation accepts but the signature separates: a
    /// dynamic-capacity table corresponding to a FIXED table (§8.5.1.12.3 sentence 3), and fixed runs of
    /// unequal total length after the last variable-length component. Both raise EC-OO-UNIVERSAL through the
    /// universal path while the TYPED path — which runs <see cref="Mismatch"/> itself at bind — accepts
    /// them.</para></summary>
    public static string Signature(DataItem g)
    {
        var outp = new List<string>();
        long bytes = 0; long chars = 0;
        void Flush()
        {
            if (bytes == 0 && chars == 0) return;
            outp.Add($"{bytes}/{chars}");
            bytes = 0; chars = 0;
        }
        foreach (var a in AtomsOf(g))
        {
            if (a.Kind is AtomKind.DynamicLength) { Flush(); outp.Add("D"); }
            else if (a.Kind is AtomKind.Table && a.DynamicCapacity)
            {
                Flush();
                outp.Add($"T{a.ElementBytes}/{a.ImageChars}");
            }
            else { bytes += a.Width; chars += a.ImageChars; }
        }
        Flush();
        return string.Join(",", outp);
    }

    /// <summary>Null when <paramref name="one"/> and <paramref name="other"/> are COMPATIBLE per §8.5.1.12,
    /// else the reason, worded for a diagnostic. Two FIXED-length groups are compatible outright (§8.5.1.12.1:
    /// "Two fixed-length groups are always compatible, unless they are strongly typed and have different type
    /// definitions" — the strong-typing half is §14.8.2.2's own separate sentence and is checked by the caller,
    /// so this returns null for that pair rather than restating a rule that lives elsewhere).</summary>
    public static string? Mismatch(DataItem one, DataItem other)
    {
        if (!IsVariableLength(one) && !IsVariableLength(other)) return null;
        // §8.5.1.12.1: compatibility is a relation between GROUPS ("unless the other operand is a compatible
        // group"). An elementary counterpart — which §14.8.2.2 rule 1 would otherwise admit for a fixed-length
        // group — has no atom sequence to correspond with.
        if (!one.IsGroup || !other.IsGroup)
            return $"'{(one.IsGroup ? other : one).CobolName}' is not a group: a variable-length group is "
                + "compatible only with a group (ISO §8.5.1.12.1)";
        return Walk(AtomsOf(one), AtomsOf(other), one, other);
    }

    private static string? Walk(List<Atom> a, List<Atom> b, DataItem ga, DataItem gb)
    {
        int ia = 0, ib = 0;
        long pa = 0, pb = 0;
        while (ia < a.Count || ib < b.Count)
        {
            if (ia >= a.Count) return Tail(b, ib, gb, ga);
            if (ib >= b.Count) return Tail(a, ia, ga, gb);
            // The two sides are walked to the SAME relative byte position before any correspondence is decided:
            // §8.5.1.12.2 states BOTH correspondences as "the same relative byte positions within their groups".
            if (pa < pb) { if (Skip(a, ref ia, ref pa, ga) is { } e) return e; continue; }
            if (pb < pa) { if (Skip(b, ref ib, ref pb, gb) is { } e) return e; continue; }

            var x = a[ia];
            var y = b[ib];
            // Rule 3 (§8.5.1.12.2 sentence 1 + §8.5.1.12.3 sentence 1): dynamic-length items correspond by
            // position and then ALWAYS match, "regardless of their definitions".
            if (x.Kind is AtomKind.DynamicLength || y.Kind is AtomKind.DynamicLength)
            {
                if (x.Kind != y.Kind)
                {
                    var lone = x.Kind is AtomKind.DynamicLength ? x : y;
                    var host = x.Kind is AtomKind.DynamicLength ? ga : gb;
                    return $"the dynamic-length item '{lone.Item.CobolName}' of '{host.CobolName}' starts at "
                        + $"relative byte position {pa} and the other group has no dynamic-length item there "
                        + "(ISO §8.5.1.12.1 rule 3 / §8.5.1.12.2)";
                }
                ia++; ib++;
                continue;
            }
            // Rules 1 and 2 (§8.5.1.12.2 sentence 2 + §8.5.1.12.3 sentences 2-4).
            if ((x.Kind is AtomKind.Table && x.DynamicCapacity) || (y.Kind is AtomKind.Table && y.DynamicCapacity))
            {
                if (x.Kind is not AtomKind.Table || y.Kind is not AtomKind.Table)
                {
                    var lone = x.DynamicCapacity ? x : y;
                    var host = x.DynamicCapacity ? ga : gb;
                    return $"the dynamic-capacity table '{lone.Item.CobolName}' of '{host.CobolName}' occupies "
                        + $"relative byte position {pa} and the other group has no table there "
                        + "(ISO §8.5.1.12.1 rule 1 / §8.5.1.12.2)";
                }
                if (x.ElementBytes != y.ElementBytes)
                    return $"corresponding tables '{x.Item.CobolName}' and '{y.Item.CobolName}' do not match: "
                        + $"their elements are {x.ElementBytes} and {y.ElementBytes} bytes "
                        + "(ISO §8.5.1.12.3 — the byte length of their elements shall be equal)";
                if (Elements(x.Item, y.Item) is { } inner)
                    return $"corresponding tables '{x.Item.CobolName}' and '{y.Item.CobolName}' do not match: "
                        + $"their elements are not compatible — {inner} (ISO §8.5.1.12.3)";
                // §8.5.1.12.3 sentence 3: when only ONE of the pair is a dynamic-capacity table, the dynamic one
                // "is considered to be the same length as the corresponding table"; when BOTH are, sentence 4
                // makes each one element long — which is the Width each atom already carries.
                long len = x.DynamicCapacity && y.DynamicCapacity ? x.ElementBytes
                    : x.DynamicCapacity ? y.Width : x.Width;
                pa += len; pb += len; ia++; ib++;
                continue;
            }
            // Plain bytes on both sides: consume one atom and let the position compare above re-align. Widths
            // may differ freely — the standard constrains the POSITIONS of the variable-length items, not the
            // shape of the fixed material between them.
            pa += x.Width; ia++;
        }
        return null;
    }

    /// <summary>Walk the lagging side forward over material the other side has already passed. A dynamic item
    /// found here sits at a byte position the other group has no counterpart at, so it fails rules 1/3.</summary>
    private static string? Skip(List<Atom> list, ref int i, ref long p, DataItem host)
    {
        var at = list[i];
        if (at.Kind is AtomKind.DynamicLength)
            return $"the dynamic-length item '{at.Item.CobolName}' of '{host.CobolName}' starts at relative "
                + $"byte position {p} and the other group has no dynamic-length item there "
                + "(ISO §8.5.1.12.1 rule 3 / §8.5.1.12.2)";
        if (at.DynamicCapacity)
            return $"the dynamic-capacity table '{at.Item.CobolName}' of '{host.CobolName}' occupies relative "
                + $"byte position {p} and the other group has no corresponding table there "
                + "(ISO §8.5.1.12.1 rule 1 / §8.5.1.12.2)";
        p += at.Width;
        i++;
        return null;
    }

    /// <summary>The atoms of the LONGER group beyond the shorter group's last byte. §8.5.1.12.2's last sentence
    /// grants exactly one latitude here — "where the relative byte position of a dynamic capacity table in the
    /// longer group is beyond the last character of the shorter group, the dynamic capacity table is treated as
    /// if it corresponds to a space-filled fixed-length table" — and grants it to TABLES ONLY. A trailing
    /// dynamic-LENGTH item still needs a real counterpart (rule 3), so it fails.</summary>
    private static string? Tail(List<Atom> list, int i, DataItem host, DataItem other)
    {
        for (; i < list.Count; i++)
            if (list[i].Kind is AtomKind.DynamicLength)
                return $"the dynamic-length item '{list[i].Item.CobolName}' of '{host.CobolName}' lies beyond "
                    + $"the last byte of '{other.CobolName}', which therefore has no corresponding "
                    + "dynamic-length item (ISO §8.5.1.12.1 rule 3 / §8.5.1.12.2)";
        return null;
    }

    /// <summary>§8.5.1.12.3 sentence 2's second conjunct — "their elements are compatible". A group element
    /// recurses into the SAME relation (a table of variable-length groups is exactly the shape rule 2 exists
    /// for); an elementary element has no atoms of its own and its byte length was compared by the caller.</summary>
    private static string? Elements(DataItem x, DataItem y) =>
        x.IsGroup && y.IsGroup ? Walk(AtomsOf(x), AtomsOf(y), x, y)
        : x.IsGroup != y.IsGroup
            ? $"'{(x.IsGroup ? y : x).CobolName}' is elementary and '{(x.IsGroup ? x : y).CobolName}' is a group"
            : null;
}
