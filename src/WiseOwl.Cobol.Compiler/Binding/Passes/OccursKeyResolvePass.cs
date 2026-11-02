// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding.Passes;

/// <summary>
/// The OccursKeyResolve pass: row 20 of the per-unit resolve pipeline (OccursResolved to OccursResolved).
/// </summary>
/// <remarks>
/// <para><b>Placement in the pipeline.</b>
/// The OCCURS KEY phrase (§13.18.38.3 SR3) resolved ONCE through the §8.4.2.2 resolver, qualifiers and all (kb/Work PB1018) — after the forest and every TYPEDEF clone exist, before SEARCH ALL / the table SORT read it.
/// </para>
/// </remarks>
internal sealed class OccursKeyResolvePass : IBindPass
{
    /// <inheritdoc/>
    public string Name => "OccursKeyResolve";

    /// <inheritdoc/>
    public PassPhase Requires => PassPhase.OccursResolved;

    /// <inheritdoc/>
    public PassPhase Produces => PassPhase.OccursResolved;

    /// <inheritdoc/>
    public PassViews Views => PassViews.None;

    /// <summary>Post-build OCCURS KEY pass (kb/Work PB1018): resolve every data-name-2 of every table's KEY phrase
    /// ONCE, into <see cref="OccursSpec.ResolvedKeys"/>, which SEARCH ALL and the table SORT then read.
    /// <para>The candidates are the table's OWN subtree (ISO §13.18.38.3 SR3 — "The first specification of
    /// data-name-2 shall be the name of either the entry containing the OCCURS clause or an entry subordinate to
    /// the entry containing the OCCURS clause. Subsequent specification of data-name-2 shall be subordinate to the
    /// entry containing the OCCURS clause"), narrowed by the WRITTEN qualifiers through the ONE §8.4.2.2 matcher and
    /// counted by the ONE ambiguity verdict (§8.4.2.2.3 SR1). A key that identifies nothing there is SR3's error,
    /// COBOL2353 — it used to be reported by nobody: an unknown key compiled clean and the table SORT then
    /// quietly did nothing. The subtree walk, not the name index, because a TYPEDEF clone's members are off the
    /// index and each clone must bind its OWN key (§13.18.58.4 GR1), exactly as its DEPENDING ON does.</para></summary>
    public void Run(DataBinder binder)
    {
        foreach (var table in binder.ClauseResolutionForest())
        {
            if (table.OccursSpec is not { Keys.Count: > 0 } spec) continue;
            using var _ = binder.Edition.At(table);
            string subject = table.CobolName ?? table.ClrName;
            spec.ResolvedKeys.Clear();
            for (int i = 0; i < spec.Keys.Count; i++)
            {
                var key = spec.Keys[i];
                string written = DataBinder.WrittenQualified(key.Name, key.Qualifiers);
                // The phrase as the program wrote it: ASCENDING or DESCENDING names the KEY phrase of ISO
                // §13.18.38.2, so a diagnostic about the key never opens with an elided OCCURS (kb/Work PB4160).
                string keyPhrase = $"OCCURS {(key.Descending ? "DESCENDING" : "ASCENDING")} KEY IS";
                var within = binder.SubtreeCandidates(table, key.Name, key.Qualifiers);
                DataItem? item = binder.UniqueOrReportAmbiguous(within, $"{keyPhrase} data-name-2 of '{subject}'", written,
                    out bool ambiguous);
                if (item is null && !ambiguous)
                    binder.Edition.Error(DiagnosticCatalog.OccursKeyNotWithinTable,
                        $"{keyPhrase} '{written}' on '{subject}': no data item so named "
                        + (key.Qualifiers.Count > 0 ? "under the written qualifiers " : "")
                        + $"is '{subject}' itself or subordinate to it — \"The first specification of data-name-2 "
                        + "shall be the name of either the entry containing the OCCURS clause or an entry subordinate "
                        + "to the entry containing the OCCURS clause\" (ISO §13.18.38.3 SR3)");
                else if (i > 0 && ReferenceEquals(item, table))
                {
                    binder.Edition.Error(DiagnosticCatalog.OccursKeyNotWithinTable,
                        $"{keyPhrase} '{written}' on '{subject}': only the FIRST key data-name may name the "
                        + "table entry itself — \"Subsequent specification of data-name-2 shall be subordinate to the "
                        + "entry containing the OCCURS clause\" (ISO §13.18.38.3 SR3)");
                    item = null;
                }
                else if (item is not null && OccursKeyItemFault(table, item) is { } fault)
                {
                    binder.Edition.Error(DiagnosticCatalog.OccursKeyItemNotAdmitted,
                        $"{keyPhrase} '{written}' on '{subject}': {fault}");
                    item = null;
                }
                spec.ResolvedKeys.Add(item);
            }
        }
    }

    /// <summary>⛔ WHAT a resolved KEY data-name-2 may BE (kb/Work PB1263) — §13.18.38.3's four constraints on the item,
    /// asked once the name has resolved within the table (SR3, above). Null when the key is admitted; otherwise the
    /// broken rule, quoted. Each one exists because a key must have exactly ONE ordered value per table element for
    /// SEARCH ALL and the table SORT to mean anything:
    /// <list type="bullet">
    /// <item>SR4 — "If data-name-2 is subordinate to an alphanumeric group item, bit group item, national group item,
    /// or strongly-typed group item that is subordinate to the entry containing the OCCURS clause, that group item
    /// shall not contain an OCCURS clause": no table may sit BETWEEN the table and its key. The rule's own list of
    /// group kinds is asked (<see cref="ItemCategory.GroupKindsOf"/>), so a group that is ONLY a variable-length
    /// group is not named by it.</item>
    /// <item>SR6 — "The data item identified by data-name-2 shall not contain an OCCURS clause except when
    /// data-name-2 is the subject of the entry".</item>
    /// <item>SR8 — "The KEY phrase shall not be specified for a data item of class boolean, message-tag, object, or
    /// pointer" (<see cref="ItemCategory.IsBooleanMessageTagObjectOrPointer"/>).</item>
    /// <item>SR9 — "Data-name-2 shall not reference a variable-length group", §8.5.1.12.1's "group item whose data
    /// description has at least one dynamic-length elementary item or dynamic-capacity table as a subordinate
    /// item".</item>
    /// </list></summary>
    private static string? OccursKeyItemFault(DataItem table, DataItem key)
    {
        const GroupKinds Sr4Kinds = GroupKinds.Alphanumeric | GroupKinds.Bit | GroupKinds.National | GroupKinds.StronglyTyped;
        // SR4 asks about a group BETWEEN the table entry and its key. When the key IS the table (SR6 admits that), no group
        // lies between them: key.Parent is already above the table, so the walk below would never meet it and would
        // charge the key with any OCCURS group that merely ENCLOSES the table (a table inside OUTER OCCURS 2).
        for (DataItem? g = ReferenceEquals(key, table) ? null : key.Parent; g is not null && !ReferenceEquals(g, table); g = g.Parent)
            if (g.IsTable && (ItemCategory.GroupKindsOf(g) & Sr4Kinds) != 0)
                return $"the key is subordinate to '{g.CobolName ?? g.ClrName}', a group within the table that contains an "
                    + "OCCURS clause — \"If data-name-2 is subordinate to an alphanumeric group item, bit group item, "
                    + "national group item, or strongly-typed group item that is subordinate to the entry containing the "
                    + "OCCURS clause, that group item shall not contain an OCCURS clause\" (ISO §13.18.38.3 SR4)";
        if (!ReferenceEquals(key, table) && key.IsTable)
            return "the key's own entry contains an OCCURS clause — \"The data item identified by data-name-2 shall not "
                + "contain an OCCURS clause except when data-name-2 is the subject of the entry\" (ISO §13.18.38.3 SR6)";
        if (ItemCategory.IsBooleanMessageTagObjectOrPointer(key))
            return "the key is a data item of class boolean, message-tag, object or pointer — \"The KEY phrase shall not "
                + "be specified for a data item of class boolean, message-tag, object, or pointer\" (ISO §13.18.38.3 SR8)";
        if (ItemCategory.GroupKindsOf(key).HasFlag(GroupKinds.VariableLength))
            return "the key is a variable-length group (ISO §8.5.1.12.1) — \"Data-name-2 shall not reference a "
                + "variable-length group\" (ISO §13.18.38.3 SR9)";
        return null;
    }
}
