// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Frontend.Generated;

using CobolNet.Binding.Model;

namespace CobolNet.Binding;

using Core = CobolParserCore;

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
    private OccursSpec? OdoBindOccursSpec(Core.OccursClauseContext occ, string where, int? maxBound)
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
            // COBOL-2014 introduction gate: VersionConformancePass ParseArm.VisitOccursClause (rearch 14g.3,
            // recognition — once per source occursClause; a per-DataItem bound-arm walk would over-count TYPE clones).
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

        // Each fixed bound is an integer literal or an integer constant-name (§13.10.3 SR2); the caller already
        // resolved the LAST bound (the maximum) via OccursBoundValue — <paramref name="maxBound"/> — so an
        // unresolvable bound reports exactly once. Only integer-1 of a Format-2 pair resolves here.
        var bounds = occ.occursBound();
        int max = maxBound ?? 0;
        // Format 2 is `OCCURS integer-1 TO integer-2 … DEPENDING …` (§13.18.38 general formats); `OCCURS n
        // DEPENDING` without TO (a widespread dialect shorthand the grammar tolerates) takes minimum 1. Min
        // feeds only the SR16 check and the later EC-BOUND-ODO bounds — allocation is ALWAYS Max (§8.5.1.8).
        int min = !depending ? max
            : bounds.Length > 1 ? OccursBoundValue(bounds[0], where) ?? 1
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
    internal void OdoResolve()
    {
        static DataItem RootOf(DataItem d)
        {
            while (d.Parent is { } p) d = p;
            return d;
        }

        // The first item named `name` within `root`'s subtree (the same record) — used to prefer a same-record
        // counter over a globally-first same-named item (review DEVLOG 664 fix #4).
        static DataItem? FindInSubtree(DataItem root, string name)
        {
            if (string.Equals(root.CobolName, name, StringComparison.OrdinalIgnoreCase)) return root;
            foreach (var c in root.Children)
                if (FindInSubtree(c, name) is { } hit) return hit;
            return null;
        }

        foreach (var item in AllItems())
        {
            if (item.OccursSpec is not { DependingName: { } depName } spec) continue;
            string subject = item.CobolName ?? item.CsName;

            // SR16: 0 ≤ integer-1 < integer-2.
            if (spec.Min < 0 || spec.Min >= spec.Max)
                Edition.Error("COBOLNET0850", $"OCCURS {spec.Min} TO {spec.Max} on '{subject}': integer-1 shall "
                    + "be greater than or equal to zero and less than integer-2 (ISO §13.18.38 SR16)");

            // data-name-1 resolution. PREFER a counter in the item's OWN record (same subtree) — critical for a
            // TYPEDEF clone, whose internal DEPENDING must bind to the clone's OWN sibling, not a globally-first
            // same-named item in a different record (review DEVLOG 664 fix #4; §13.18.57.4 GR1 — the type is coded in
            // place — + §13.18.38 SR20, data-name-1 lies within the same record). Otherwise fall back to the
            // scope-aware lookup (M2-OO-1h): a method table's data-name-1 resolves in the owning method's scope first
            // (§11.7.4 GR5), then a visible object/program item.
            DataItem? dep = FindInSubtree(RootOf(item), depName);
            if (dep is null)
            {
                if (!Symbols.TryResolve(depName, ScopeOf(RootOf(item)), out var cands))
                {
                    Edition.Error("COBOLNET0851", $"OCCURS … DEPENDING ON '{depName}' on '{subject}': data-name-1 "
                        + "is not defined (ISO §13.18.38 Format 2)");
                    continue;
                }
                dep = cands[0];
            }
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
    /// implicit-definition rule → COBOLNET1523. Placement/declaration guards: SR28 FROM ≤ TO (1522); the FILE
    /// SECTION prohibition §8.5.1.9.1 (1526); the VALUE-derived-capacity §13.18.63 GR16 staging (1528).
    /// </summary>
    internal void DynamicResolve()
    {
        // §8.5.1.9.1 item 3 (:8195) — the roots of every FILE SECTION record, so a dynamic table in one is rejected.
        var fileRecordRoots = new HashSet<DataItem>(Files.SelectMany(f => f.Records));
        static bool SubtreeHasValue(DataItem d) => d.RawValue is not null || d.Children.Any(SubtreeHasValue);

        foreach (var item in AllItems())
        {
            if (item.OccursSpec is not { IsDynamic: true } spec) continue;
            string subject = item.CobolName ?? item.CsName;

            // §8.5.1.9.1 item 3 (:8195) — a dynamic-capacity table "may be defined in any place, OTHER THAN the file
            // section" (its out-of-line CobolDynTable has no place in a record image). Reject a dynamic table whose
            // storage root is an FD/SD record.
            DataItem root = item; while (root.Parent is { } p) root = p;
            if (fileRecordRoots.Contains(root))
                Edition.Error("COBOLNET1526", $"OCCURS DYNAMIC on '{subject}': a dynamic-capacity table shall not be "
                    + "defined in the FILE SECTION (ISO §8.5.1.9.1)");

            // SR28 (:19987): integer-4 (FROM) shall be nonnegative and integer-5 (TO), if both present, shall be
            // GREATER THAN integer-4. (FROM<0 cannot be written — the grammar takes an unsigned integerLiteral.)
            if (spec.InitialCap is { } from && spec.ExpectedMax is { } to && to <= from)
                Edition.Error("COBOLNET1522", $"OCCURS DYNAMIC FROM {from} TO {to} on '{subject}': the expected "
                    + $"capacity (TO integer-5) shall be greater than the minimum capacity (FROM integer-4) "
                    + "(ISO §13.18.38 SR28)");

            // §13.18.63 GR16 (:23440) — a VALUE clause in the DYNAMIC entry OR any entry SUPERORDINATE to it derives
            // the initial capacity (GR16b: no-TO-in-VALUE → the OCCURS expected capacity/TO). That derivation is
            // staged (data-model D9) → LOUD, never silently mis-sized: (a) an elementary dynamic entry's OWN VALUE;
            // (b) a GROUP dynamic table whose subtree carries a VALUE AND whose OCCURS has a TO (an expected capacity
            // for GR16b to use). A subordinate VALUE with NO TO has no expected capacity for GR16b, so the
            // §14.6.2.3.2 item-6 default (capacity = FROM, elements seeded) applies and IS supported (the
            // dyn_initialize / dyn_initialized goldens).
            if ((item.IsElementary && item.RawValue is not null)
                || (item.IsGroup && spec.ExpectedMax is not null && item.Children.Any(SubtreeHasValue)))
                Edition.Error("COBOLNET1528", $"OCCURS DYNAMIC on '{subject}' with a VALUE clause: a VALUE in the "
                    + "dynamic entry or a superordinate entry derives the initial capacity (ISO §13.18.63 GR16) — "
                    + "that derivation is not yet implemented");

            // The register: an unsigned-integer VIEW over the table's Capacity (SR31) — a native-binary PicInfo so
            // the numeric pipeline reads {tablePath}.Capacity (a long) as a scale-0 integer; no stored field. The
            // implementor digit count is 10 (unsigned BinaryLong): the CobolDynTable implementor maximum,
            // 0x3FFF_FFFF ≈ 1.07e9, fits in 10 digits (§8.5.1.9.1 — "a number of digits sufficient to hold the
            // maximum"). Kept off ByName/Roots — reachable ONLY through CapacityRegisters (the resolver hook).
            // ⛔ MINTED FOR EVERY DYNAMIC TABLE, NAMED ONLY WHEN `CAPACITY IN data-name-3` IS WRITTEN (kb/Work
            // PB61): the view over the table's current capacity is what FUNCTION LENGTH / BYTE-LENGTH read for a
            // variable-length group (§15.50.4 r7c / §15.14.4 r6c — "based on their current capacity"), whether or
            // not the program gave the register a name. An unnamed register has no CobolName and no
            // CapacityRegisters entry, so no COBOL reference can reach it.
            var reg = new DataItem
            {
                Level = 49,
                CsName = "__cap_" + item.CsName,
                CobolName = spec.CapacityName,
                Pic = PicInfo.BinaryItem(Usage.BinaryLong, signed: false),
                Uid = _uidCounter++,
            };
            spec.CapacityRegister = reg;

            if (spec.CapacityName is not { } capName) continue;

            // SR30 — data-name-3 is implicitly defined at the OCCURS entry, so it must not also be an explicit
            // data-name (a duplicate definition) nor the CAPACITY register of another dynamic table.
            if (ByName.ContainsKey(capName) || _capacityRegisters.ContainsKey(capName))
            {
                Edition.Error("COBOLNET1523", $"CAPACITY IN '{capName}' on '{subject}': data-name-3 is implicitly "
                    + "defined by the OCCURS DYNAMIC entry and shall not duplicate another data-name or CAPACITY "
                    + "register (ISO §13.18.38 SR30)");
                continue;
            }
            _capacityRegisters[capName] = item;
        }
    }
}
