// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Binding;

/// <summary>⛔ THE NAME-UNIQUENESS RULES OF A DECLARATION, asked once the unit's own data division has bound and every name
/// of the unit is declared (kb/Work PB5073). A name resolver asks uniqueness of a REFERENCE (§2023:8.4.2.2.3 SR1,
/// COBOL1639); these rules are about the DECLARATIONS, so an unreferenced duplicate is refused too.</summary>
public sealed partial class DataBinder
{
    /// <summary>⛔ A GLOBAL data-name names no other data item of its source element, whatever the dialect. §2023:8.4.2.1: "The
    /// same name shall not be used both as the name of an item possessing the global attribute and as the name of any other data
    /// item described in the source element that describes that global data item." (the 1985 and 2002 texts carry the same
    /// sentence, §1985:IV.4.3.8.1). It states the rule on two items both written GLOBAL too, which §1985:X.4.6.3 SR2 repeats:
    /// "In the same Data Division, the data description entries for any two data items for which the same data-name is
    /// specified must not include the GLOBAL clause."
    /// <para>The GLOBAL item is the entry that CARRIES the clause — a record the clause (or its file description's) makes
    /// global. The data-names subordinate to it possess the attribute too (§2023:13.18.27.4 GR1), but §2023:8.4.6.2.1 3) b) 1.
    /// leaves two such subordinates of one element a name to be qualified, never a declaration to refuse
    /// (conformance:pb1047-two-globals-one-element-ambiguous), so the screen names the entries that write the clause. A
    /// description a TYPE or SAME AS clause copied in IS a data item of the source element: "as though the type description
    /// had been coded in place" (§2023:13.18.57.4 GR1; SAME AS, §2023:13.18.49.4 GR1), so the screen runs on the COMPOSED
    /// forest (<see cref="Passes.CheckGlobalNameUniquenessPass"/>, after ExpandTypes) and counts the copies. A GLOBAL name a
    /// CONTAINER declared and this unit inherited (<see cref="ForestSlice.DeclaringDepth"/> above 0) is another source
    /// element's and is left out.</para>
    /// <para>The pair is reported once, at the LATER entry of the two, where the user can edit it.</para></summary>
    internal void ScreenGlobalNameUniqueness()
    {
        foreach (var (name, items) in State.Forest.ByName)
        {
            if (items.Count < 2) continue;
            DataItem? firstGlobal = null;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (State.Forest.DeclaringDepth(item) != 0) continue;
                bool global = item.Parent is null && IsGlobalRecord(item);
                bool another = i > 0 && items.Take(i).Any(p => State.Forest.DeclaringDepth(p) == 0);
                if (another && (global || firstGlobal is not null))
                {
                    using var _ = Edition.At(item.DeclaredAt);
                    Edition.Error(DiagnosticCatalog.GlobalDataNameNotUnique, $"data-name '{name}' names {(global ? "this GLOBAL item" : "this item")} "
                        + $"and also {(global && firstGlobal is null ? "another data item" : "a GLOBAL item")} described in the same source element; "
                        + (Edition.DialectLevel < 2002
                            ? "the same data-name must not name an item possessing the global attribute and any other data item (ISO §1985:IV.4.3.8.1), "
                                + "and no two data items of a Data Division with the same data-name may include the GLOBAL clause (ISO §1985:X.4.6.3 SR2)"
                            : "the same name shall not be used both as the name of an item possessing the global attribute and as the name of any other data item (ISO §2023:8.4.2.1)"));
                }
                if (global) firstGlobal ??= item;
            }
        }
    }

    /// <summary>⛔ §1985:VI.5.8.3 SR14, "Index-name-1 must be a unique word within the program", asked of the DECLARATION, in
    /// the dialect that states it. COBOL-2002 and later let an index-name be qualified "in the same manner as data and
    /// condition-names" (ISO/IEC 1989:2002 Annex F item 108), so two tables may each declare one spelling and a
    /// REFERENCE that fails to tell them apart is the §2023:8.4.2.2.3 SR1 ambiguity (COBOL1639, the index-name resolver).
    /// The second and later declaration of a spelling is reported, one entry writing it twice included.</summary>
    private void ScreenIndexNameUniqueness()
    {
        if (Edition.DialectLevel >= 2002) return;
        var seen = new HashSet<string>(CobolNames.Comparer);
        foreach (var decl in State.Forest.IndexNames.Own)
        {
            if (seen.Add(decl.Name)) continue;
            using var _ = Edition.At(decl.Table.DeclaredAt);
            Edition.Error(DiagnosticCatalog.IndexNameNotUniqueInProgram, $"index-name '{decl.Name}' of the OCCURS clause of "
                + $"'{decl.Table.CobolName ?? "FILLER"}' is already declared by an INDEXED BY phrase of this program; "
                + "index-name-1 must be a unique word within the program (ISO §1985:VI.5.8.3 SR14)");
        }
    }
}
