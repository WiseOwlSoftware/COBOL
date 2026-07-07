// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Generated;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// The structured OCCURS description (ISO/IEC 1989:2023 §13.18.38) attached to a table's <see cref="DataItem"/>:
/// the Format-2 occurrence bounds (<c>OCCURS integer-1 TO integer-2 TIMES DEPENDING ON data-name-1</c>), the
/// DEPENDING ON data-name and its post-build-resolved item, and the ASCENDING/DESCENDING KEY data-names (Formats
/// 1 and 2 alike — §13.18.38 GR3, consumed by SEARCH ALL and the table SORT). The ARRAY itself is always
/// allocated at <see cref="Max"/> occurrences — §8.5.1.8: for an occurs-depending table "the physical capacity is
/// fixed at compile time; the logical capacity may vary" — so only the GR7 current-count machinery (group-operand
/// extents per GR8, the SEARCH bound per §14.9.37.4 GR4) consults this object; layout, width, and VALUE
/// initialization (§13.18.63 GR6 — as if the count were the maximum) ride on <see cref="DataItem.Occurs"/>.
/// </summary>
public sealed class OccursSpec
{
    /// <summary>integer-1 — the minimum occurrence count (<c>0 ≤ Min &lt; Max</c>, §13.18.38 SR16). Equals
    /// <see cref="Max"/> for a fixed (Format 1) table.</summary>
    public required int Min { get; init; }

    /// <summary>integer-2 — the maximum occurrence count, the allocated physical capacity (§8.5.1.8).</summary>
    public required int Max { get; init; }

    /// <summary>The <c>DEPENDING ON data-name-1</c> name as written, or <see langword="null"/> for a fixed
    /// (Format 1) table.</summary>
    public string? DependingName { get; init; }

    /// <summary>The resolved data-name-1 item — set by the post-build <c>DataBinder.OdoResolve</c> pass
    /// (data-name-1 may legally be declared anywhere outside the span the table starts, §13.18.38 SR20, so
    /// resolution must wait for the complete forest).</summary>
    public DataItem? Depending { get; set; }

    /// <summary>ASCENDING KEY data-names in declaration order (§13.18.38 GR3 — the significance order SEARCH ALL
    /// and the table SORT consult; captured for model completeness, the scan implementation is key-order-free).</summary>
    public List<string> AscendingKeyNames { get; } = [];

    /// <summary>DESCENDING KEY data-names in declaration order (§13.18.38 GR3).</summary>
    public List<string> DescendingKeyNames { get; } = [];

    // ── Format 4: DYNAMIC-capacity table (ISO §13.18.38 Format 4, COBOL-2014; data-model D9) ──────────────────

    /// <summary>True for a Format-4 DYNAMIC-capacity table (§8.5.1.9) — its capacity varies at run time. Mutually
    /// exclusive with the Format-2 DEPENDING form. A dynamic table has no fixed <see cref="DataItem.Occurs"/>.</summary>
    public bool IsDynamic { get; init; }

    /// <summary><c>CAPACITY IN data-name-3</c> — the current-capacity register name (§13.18.38 GR15), or null.</summary>
    public string? CapacityName { get; init; }

    /// <summary>The synthetic CAPACITY register item — a view over the table's Capacity, set by the post-build
    /// <c>DataBinder.DynamicResolve</c> pass (data-name-3 is implicitly defined at the OCCURS entry, SR30).</summary>
    public DataItem? CapacityRegister { get; set; }

    /// <summary><c>FROM integer-4</c> — the minimum / initial current capacity (§13.18.38 GR16); null ⇒ 0.</summary>
    public int? InitialCap { get; init; }

    /// <summary><c>TO integer-5</c> — the expected capacity (§13.18.38 GR17); null ⇒ unlimited.</summary>
    public int? ExpectedMax { get; init; }

    /// <summary>The INITIALIZED phrase — seed each new/intermediate occurrence per §8.5.1.9.5.</summary>
    public bool Initialized { get; init; }
}

/// <summary>
/// The <see cref="Place"/> of a GROUP operand whose subtree contains an occurs-depending table (ISO/IEC 1989:2023
/// §13.18.38 Format 2). It decorates the plain member place — <see cref="Read"/>/<see cref="Write"/> are the
/// struct lvalue unchanged, so every consumer that does not know about ODO behaves exactly as before — and the
/// GR8 operand seams consult the decoration:
/// <list type="bullet">
///   <item><b>Sending</b> (the sending side of BOTH GR8 quadrants): "only that part of the table area that is
///     specified by the value of [data-name-1] at the start of the operation will be used" — <see
///     cref="SendingImage"/> is the group image truncated to the current extent. SR22 (the subject may be
///     followed within its record only by entries subordinate to it) guarantees the table is the TRAILING
///     storage, so the current extent is a character PREFIX of the maximum image; a zero count with no preceding
///     fixed part is the zero-length item of §8.5.4 item 1.</item>
///   <item><b>Receiving, data-name-1 outside the group</b> (GR8a): the same current extent — character positions
///     past it are NOT modified; <see cref="ReceiveInto"/> splices the stored prefix over the live image.</item>
///   <item><b>Receiving, data-name-1 inside the group</b> (GR8b): "the maximum length of the group will be used"
///     — <see cref="DependingInside"/> lets each receiver keep the plain full-width <c>FromImage</c> store.</item>
/// </list>
/// The legacy engine proved exactly this direction split over the NIST-85 corpus (NC247A; its
/// <c>LocationResolver.ResolveWholeItem(receiving)</c>); the greenfield twin computes the CHARACTER extent at the
/// operand site — no runtime table state (COBOLNET_DESIGN §3.6 / §14.4 — ONE image facility, the GR8 slice is a
/// view over it). The legacy's LINKAGE max-length shortcut is deliberately NOT ported: GR8 applies in any section.
/// </summary>
public sealed record OdoGroupPlace(
    MemberPlace Inner, Place Depending, int FixedChars, int ElemChars, int MaxOccurs, bool DependingInside) : Place
{
    /// <inheritdoc/>
    public override PicInfo? Pic => Inner.Pic;

    /// <inheritdoc/>
    public override DataItem Item => Inner.Item;

    /// <inheritdoc/>
    public override string Read() => Inner.Read();

    /// <inheritdoc/>
    public override string Write(string rhs) => Inner.Write(rhs);

    /// <summary>C# <c>int</c> expression: the operand's CURRENT character extent — the fixed prefix plus
    /// data-name-1's value × the element width. The count is read at the operation site (GR8 — "at the start of
    /// the operation") through <c>CobolTable.Occ</c> (storage-form-agnostic: native <c>long</c> or a
    /// whole-group-aliased character image) and clamped benignly to [0, max]: a count outside
    /// integer-1..integer-2 at reference time makes the excess content undefined (GR7) — EC-BOUND-ODO is the
    /// 2002+ checked mode, the later EC slice (SSOT §11); COBOL-85 has no exception conditions.</summary>
    public string LengthExpr =>
        $"CobolTable.OdoExtent(CobolTable.Occ({Depending.Read()}), {MaxOccurs}, {FixedChars}, {ElemChars})";

    /// <summary>The group's SENDING character image (ISO §13.18.38 GR8 — both quadrants send the current-count
    /// part): the maximum image truncated to <see cref="LengthExpr"/> characters (a prefix, by SR22).</summary>
    public string SendingImage() => $"{Inner.Read()}.AsImage().Substring(0, {LengthExpr})";

    /// <summary>A complete receiving C# statement for the GR8a (depending-outside) quadrant: store
    /// <paramref name="imageExpr"/> over the CURRENT extent only — splice it into the live image, leaving every
    /// character position past the count unmodified (GR8a), then distribute back through the group's generated
    /// <c>FromImage</c> (the §14.4 single image facility).</summary>
    public string ReceiveInto(string imageExpr) =>
        $"{Inner.Read()}.FromImage(CobolString.SpliceInto({Inner.Read()}.AsImage(), 1, {LengthExpr}, {imageExpr}));";
}

/// <summary>Pure model helpers for the OCCURS DEPENDING ON subsystem (ISO/IEC 1989:2023 §13.18.38).</summary>
public static class OdoModel
{
    /// <summary>The occurs-depending table among <paramref name="group"/>'s STRICT descendants, or
    /// <see langword="null"/>. At most one exists in a legal program: §13.18.38 SR22 makes it the unique trailing
    /// variable part of its record, and SR1(b)/SR10 forbid nesting it under another OCCURS — both validated by
    /// <c>DataBinder.OdoResolve</c>, so a first-match scan is exact.</summary>
    public static DataItem? TableUnder(DataItem group)
    {
        foreach (var c in group.Children)
        {
            if (c.OccursSpec is { DependingName: not null }) return c;
            if (TableUnder(c) is { } nested) return nested;
        }
        return null;
    }

    /// <summary>True when <paramref name="item"/> is <paramref name="ancestor"/> itself or lies within its
    /// subtree (the GR8a/GR8b "data item that describes the group" containment test).</summary>
    public static bool IsWithin(DataItem item, DataItem ancestor)
    {
        for (DataItem? n = item; n is not null; n = n.Parent)
            if (ReferenceEquals(n, ancestor)) return true;
        return false;
    }

    /// <summary>The EMITTED character-image width of an item — mirrors <c>FieldEmitter</c>'s physical layout (a
    /// Tier-B REDEFINES class contributes its single string backing once, its views nothing; a Tier-A view
    /// forwards), so the GR8 extent arithmetic aligns with the generated <c>AsImage</c> exactly. For the common
    /// class-free subtree this equals <see cref="DataItem.ImageWidth"/>.</summary>
    public static int PhysicalWidth(DataItem item)
    {
        if (!item.IsGroup) return item.ImageWidth;
        int w = 0;
        foreach (var c in item.Children)
        {
            if (c.Class is { Tier: RedefinesTier.StringCanonical } cls)
            {
                if (c.IsCanonical) w += cls.Width;   // the ONE backing, counted once at the canonical
                continue;
            }
            if (c.Class is { Tier: RedefinesTier.Alias } && !c.IsCanonical) continue;   // a forwarded view
            w += (c.IsGroup ? PhysicalWidth(c) : c.ImageWidth) * (c.Occurs ?? 1);
        }
        return w;
    }

    /// <summary>Wrap a resolved GROUP place whose subtree contains the occurs-depending <paramref name="table"/>
    /// (ISO §13.18.38 GR8). The fixed prefix is everything before the table in the group's emitted image — exact
    /// because the table is the record's trailing storage (SR22, enforced at bind).</summary>
    public static OdoGroupPlace WrapGroup(MemberPlace inner, Place depending, DataItem group, DataItem table)
    {
        int elem = table.ImageWidth;                          // per-occurrence character width
        int max = table.Occurs ?? 1;                          // the allocated capacity = integer-2 (§8.5.1.8)
        int fixedChars = PhysicalWidth(group) - elem * max;   // SR22 — the variable tail is trailing
        return new OdoGroupPlace(inner, depending, fixedChars, elem, max, IsWithin(depending.Item, group));
    }

    /// <summary>The SEARCH / SEARCH ALL AT-END bound for a table (ISO §14.9.37.4 GR4/GR9 → §13.18.38 GR7): a C#
    /// <c>long</c> expression reading the CURRENT depending count for an occurs-depending table (storage-form
    /// agnostic via <c>CobolTable.Occ</c>), or <see langword="null"/> for a fixed table — the caller then uses the
    /// compile-time maximum. data-name-1 is resolved post-build (it may be declared anywhere, SR20).</summary>
    public static string? SearchBound(DataItem table, ReferenceResolver refs) =>
        table.OccursSpec is { Depending: { } dep } && refs.ResolveItem(dep) is { } dp
            ? $"CobolTable.Occ({dp.Read()})" : null;
}

/// <summary>
/// The OCCURS DEPENDING ON half of the data binder (ISO/IEC 1989:2023 §13.18.38): Format-2 clause capture
/// (<see cref="OdoBindOccursSpec"/>) and the post-build resolution + structural-validation pass
/// (<see cref="OdoResolve"/>). Partial-class extension over <c>DataBinder</c> — the entry binder stores the array
/// capacity (the MAXIMUM, §8.5.1.8) in <see cref="DataItem.Occurs"/> and the bounds/DEPENDING/KEY surface here.
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>Capture an OCCURS clause's structured description: the Format-2 <c>integer-1 TO integer-2</c>
    /// bounds and DEPENDING ON name, plus the ASCENDING/DESCENDING KEY data-names (Formats 1 and 2, §13.18.38
    /// GR3). Returns <see langword="null"/> for a plain keyless fixed table — <see cref="DataItem.Occurs"/>
    /// alone carries those (the dominant case stays allocation-free).</summary>
    private OccursSpec? OdoBindOccursSpec(Core.OccursClauseContext occ)
    {
        bool depending = occ.DEPENDING() is not null;
        var asc = new List<string>();
        var desc = new List<string>();
        foreach (var kc in occ.occursKeyClause())
            foreach (var k in kc.dataReference())
                (kc.DESCENDING() is not null ? desc : asc).Add(k.GetText());

        // Format 4 — a DYNAMIC-capacity table (§13.18.38 Format 4, D9): capture CAPACITY IN / FROM / TO / INITIALIZED
        // (phrases order-independent). ALWAYS returns a spec (a keyless dynamic table still needs IsDynamic recorded,
        // unlike a keyless fixed table where DataItem.Occurs alone suffices). DataItem.Occurs stays null — a dynamic
        // table has no fixed physical capacity; its storage is the out-of-line CobolDynTable.
        if (occ.DYNAMIC() is not null)
        {
            string? capName = null; int? fromCap = null; int? toCap = null; bool initialized = false;
            foreach (var ph in occ.occursDynamicPhrase())
            {
                if (ph.CAPACITY() is not null) capName = ph.dataReference()?.GetText();
                else if (ph.INITIALIZED() is not null) initialized = true;
                else if (ph.FROM() is not null && int.TryParse(ph.integerLiteral()?.GetText(), out int fv)) fromCap = fv;
                else if (ph.TO() is not null && int.TryParse(ph.integerLiteral()?.GetText(), out int tv)) toCap = tv;
            }
            var dyn = new OccursSpec
            {
                Min = fromCap ?? 0, Max = 0, IsDynamic = true,
                CapacityName = capName, InitialCap = fromCap, ExpectedMax = toCap, Initialized = initialized,
            };
            dyn.AscendingKeyNames.AddRange(asc);
            dyn.DescendingKeyNames.AddRange(desc);
            return dyn;
        }
        if (!depending && asc.Count == 0 && desc.Count == 0) return null;

        var lits = occ.integerLiteral();
        int max = lits.Length > 0 && int.TryParse(lits[^1].GetText(), out int m) ? m : 0;
        // Format 2 is `OCCURS integer-1 TO integer-2 … DEPENDING …` (§13.18.38 general formats); `OCCURS n
        // DEPENDING` without TO (a widespread dialect shorthand the grammar tolerates) takes minimum 1. Min
        // feeds only the SR16 check and the later EC-BOUND-ODO bounds — allocation is ALWAYS Max (§8.5.1.8).
        int min = !depending ? max
            : lits.Length > 1 && int.TryParse(lits[0].GetText(), out int mn) ? mn
            : 1;
        var spec = new OccursSpec
        {
            Min = min,
            Max = max,
            DependingName = depending ? occ.dataReference()?.GetText() : null,
        };
        spec.AscendingKeyNames.AddRange(asc);
        spec.DescendingKeyNames.AddRange(desc);
        return spec;
    }

    /// <summary>
    /// Post-build OCCURS DEPENDING ON pass (runs once the whole forest and the redefines classes exist): resolve
    /// each Format-2 table's data-name-1 and enforce the structural syntax rules the GR8 character-prefix model
    /// relies on, each with its ISO citation — SR16 bounds, SR17 integer data-name-1, SR2 unsubscripted
    /// data-name-1, SR1(b)/SR10 no "complex ODO" (an occurs-depending table never nests under another OCCURS, at
    /// EVERY edition — the legacy comment claiming 2002+ legality was wrong), §13.18.44 SR no ODO inside an
    /// explicit REDEFINES, SR22 trailing table, SR20 data-name-1 placement. Violations are bind-time rejections
    /// (<c>Edition.Error</c> fails the compile) — never a silently mis-sized table (SSOT §1.4).
    /// </summary>
    private void OdoResolve()
    {
        static DataItem RootOf(DataItem d)
        {
            while (d.Parent is { } p) d = p;
            return d;
        }

        foreach (var item in AllItems())
        {
            if (item.OccursSpec is not { DependingName: { } depName } spec) continue;
            string subject = item.CobolName ?? item.CsName;

            // SR16: 0 ≤ integer-1 < integer-2.
            if (spec.Min < 0 || spec.Min >= spec.Max)
                Edition.Error("COBOLNET0850", $"OCCURS {spec.Min} TO {spec.Max} on '{subject}': integer-1 shall "
                    + "be greater than or equal to zero and less than integer-2 (ISO §13.18.38 SR16)");

            // data-name-1 resolution (first declaration wins; OF/IN-qualified data-name-1 is a later refinement).
            // Scope-aware (M2-OO-1h): a method table's data-name-1 resolves in the owning method's scope first
            // (§11.7.4 GR5), then a visible object/program item — never a same-named item in the wrong scope.
            var cands = LookupDataInScopeOf(RootOf(item), depName);
            if (cands is null || cands.Count == 0)
            {
                Edition.Error("COBOLNET0851", $"OCCURS … DEPENDING ON '{depName}' on '{subject}': data-name-1 "
                    + "is not defined (ISO §13.18.38 Format 2)");
                continue;
            }
            DataItem dep = cands[0];
            spec.Depending = dep;

            // SR17: data-name-1 shall describe an integer (an index item is NOT an integer data item).
            if (dep.Pic is not { Category: PicCategory.Numeric, IsFloat: false, Scale: 0 })
                Edition.Error("COBOLNET0852", $"OCCURS … DEPENDING ON '{depName}' on '{subject}': data-name-1 "
                    + "shall describe an integer (ISO §13.18.38 SR17)");

            // SR2: data-name-1 shall not be subscripted (it cannot lie within any table).
            for (DataItem? a = dep.Parent; a is not null; a = a.Parent)
                if (a.Occurs is not null)
                {
                    Edition.Error("COBOLNET0853", $"OCCURS … DEPENDING ON '{depName}' on '{subject}': "
                        + "data-name-1 shall not be subscripted (ISO §13.18.38 SR2)");
                    break;
                }

            // SR1(b)/SR10: "complex ODO" is illegal at every edition — tables may be nested only when the
            // DEPENDING phrase is absent, and no OCCURS subject may have an occurs-depending table beneath it.
            for (DataItem? a = item.Parent; a is not null; a = a.Parent)
                if (a.Occurs is not null)
                {
                    Edition.Error("COBOLNET0854", $"occurs-depending table '{subject}' is subordinate to the "
                        + $"OCCURS item '{a.CobolName}': tables may be nested only when the DEPENDING phrase is "
                        + "absent (ISO §13.18.38 SR1(b)/SR10)");
                    break;
                }

            // §13.18.44 SR: neither the redefined item nor a redefinition may include an occurs-depending table.
            // (The FD multi-record shared AREA is §9.1.2 record sharing — synthesized with no written REDEFINES
            // clause — and is exempt: only an explicitly-written REDEFINES anywhere in the class trips this.)
            for (DataItem? a = item; a is not null; a = a.Parent)
                if (a.RedefinesTargetName is not null
                    || (a.Class is { } cls && cls.Members.Any(mm => mm.RedefinesTargetName is not null)))
                {
                    Edition.Error("COBOLNET0855", $"occurs-depending table '{subject}' lies within a REDEFINES "
                        + "area: neither the original nor a redefinition may include an OCCURS DEPENDING ON "
                        + "table (ISO §13.18.44 SR)");
                    break;
                }

            // SR22: within its record the subject may be followed only by entries subordinate to it — the
            // variable tail is the record's TRAILING storage (the GR8 character-prefix model relies on this).
            // A later sibling that itself REDEFINES an earlier one adds no storage and does not violate SR22.
            for (DataItem? n = item; n is { Parent: { } parent }; n = parent)
            {
                int idx = parent.Children.IndexOf(n);
                if (idx >= 0 && parent.Children.Skip(idx + 1).Any(s => s.RedefinesTargetName is null))
                {
                    Edition.Error("COBOLNET0856", $"occurs-depending table '{subject}' is followed by a "
                        + "non-subordinate entry in its record: the subject of an OCCURS DEPENDING ON entry may "
                        + "be followed, within that record, only by data items subordinate to it "
                        + "(ISO §13.18.38 SR22)");
                    break;
                }
            }

            // SR20: data-name-1 shall not occupy a character position within the range delineated by the
            // table's first character position and the record's last — within the SAME record it must lie
            // strictly BEFORE the table (record leaf order IS character order for the canonical storage).
            if (ReferenceEquals(RootOf(dep), RootOf(item)))
            {
                var leaves = LeavesOf(RootOf(item)).ToList();
                int tableStart = leaves.FindIndex(l => OdoModel.IsWithin(l, item));
                int depIdx = leaves.FindIndex(l => ReferenceEquals(l, dep));
                if (tableStart >= 0 && depIdx >= tableStart)
                    Edition.Error("COBOLNET0857", $"OCCURS … DEPENDING ON '{depName}' on '{subject}': "
                        + "data-name-1 shall not occupy a character position within the range from the table's "
                        + "first character position to the last character position of the record "
                        + "(ISO §13.18.38 SR20)");
            }
        }
    }

    /// <summary>
    /// Post-build OCCURS DYNAMIC pass (ISO §13.18.38 Format 4 / §8.5.1.9; data-model D9): for each dynamic-capacity
    /// table carrying a <c>CAPACITY IN data-name-3</c> phrase, synthesize the IMPLICITLY-defined CAPACITY register
    /// (SR30) — a VIEW over the table's current capacity, an unsigned integer (SR31) — and index it by name so the
    /// <see cref="ReferenceResolver"/> can build a <see cref="CapacityRegisterPlace"/>. The register is NOT a stored
    /// field (no <c>FieldEmitter</c> entry): its value IS the runtime <c>CobolDynTable&lt;T&gt;.Capacity</c>. A
    /// register-name that duplicates an explicit data-name (or another table's register) violates the
    /// implicit-definition rule → COBOLNET1523. (The declaration/placement rules — SR28 FILE SECTION, FROM ≤ TO,
    /// no nesting under another OCCURS — are the inc-5 staged-loud 1522 sweep.)
    /// </summary>
    private void DynamicResolve()
    {
        foreach (var item in AllItems())
        {
            if (item.OccursSpec is not { IsDynamic: true, CapacityName: { } capName } spec) continue;
            string subject = item.CobolName ?? item.CsName;

            // SR30 — data-name-3 is implicitly defined at the OCCURS entry, so it must not also be an explicit
            // data-name (a duplicate definition) nor the CAPACITY register of another dynamic table.
            if (ByName.ContainsKey(capName) || CapacityRegisters.ContainsKey(capName))
            {
                Edition.Error("COBOLNET1523", $"CAPACITY IN '{capName}' on '{subject}': data-name-3 is implicitly "
                    + "defined by the OCCURS DYNAMIC entry and shall not duplicate another data-name or CAPACITY "
                    + "register (ISO §13.18.38 SR30)");
                continue;
            }

            // The register: an unsigned-integer VIEW over the table's Capacity (SR31) — a native-binary PicInfo so
            // the numeric pipeline reads {tablePath}.Capacity (a long) as a scale-0 integer; no stored field. The
            // implementor digit count is 10 (unsigned BinaryLong): the CobolDynTable implementor maximum,
            // 0x3FFF_FFFF ≈ 1.07e9, fits in 10 digits (§8.5.1.9.1 — "a number of digits sufficient to hold the
            // maximum"). Kept off ByName/Roots — reachable ONLY through CapacityRegisters (the resolver hook).
            var reg = new DataItem
            {
                Level = 49,
                CsName = "__cap_" + item.CsName,
                CobolName = capName,
                Pic = PicInfo.BinaryItem(Usage.BinaryLong, signed: false),
                Uid = _uidCounter++,
            };
            spec.CapacityRegister = reg;
            CapacityRegisters[capName] = item;
        }
    }
}
