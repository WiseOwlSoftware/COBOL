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
    /// <para>The items that POSSESS the global attribute are the entries the clause (or their file description's) makes global,
    /// the records: §2023:13.18.27.4 GR3 attaches the attribute to "the subject of that REDEFINES clause", and
    /// §2023:8.4.6.2.3 says "If a data item possessing the global attribute includes a table described with an index-name",
    /// so it is the group that possesses it (§1985:X.1.3.8.2 GR1 likewise applies "the normal rules for qualification" to
    /// find a global name). The
    /// data-names subordinate to it are global NAMES ("All data-names subordinate to a global name are global names",
    /// §2023:13.18.27.4 GR1), a matter of SCOPE, and they are told apart by qualification like any other data-names
    /// (§2023:8.4.2.2.1; §2023:8.4.6.2.1 GR3b.1) picks the nearest declaring element for a name duplicated ACROSS nested
    /// source elements): a second subordinate of the same name, in the same record or another, is no violation of the
    /// sentence, and an unqualified reference to the pair is the reference-time ambiguity COBOL1639
    /// (the ruling, kb/Work PB5629, which reversed PB5150's reading of the same sentence). A description a TYPE or SAME AS clause copied in IS a data
    /// item of the source element: "as though the type description had been coded in place" (§2023:13.18.57.4 GR1; SAME AS,
    /// §2023:13.18.49.4 GR1), so the screen runs on the COMPOSED forest (<see cref="Passes.CheckGlobalNameUniquenessPass"/>,
    /// after ExpandTypes) and counts the copies. A GLOBAL name a CONTAINER declared and this unit inherited
    /// (<see cref="ForestSlice.DeclaringDepth"/> above 0) is another source element's and is left out.</para>
    /// <para>The pair is reported once, at the LATER entry of the two that the programmer wrote: a copy is reported at the entry
    /// whose TYPE or SAME AS clause brought it in (<see cref="DataItem.ReferenceSite"/>), never at the declaration it was
    /// copied from, which is legal on its own.</para></summary>
    internal void ScreenGlobalNameUniqueness()
    {
        foreach (var (name, items) in Symbols.DataNameDeclarations())
        {
            if (items.Count < 2) continue;
            DataItem? firstGlobal = null;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (State.Forest.DeclaringDepth(item) != 0) continue;
                bool global = ReferenceEquals(item, item.Root) && IsGlobalRecord(item);
                bool another = i > 0 && items.Take(i).Any(p => State.Forest.DeclaringDepth(p) == 0);
                if (another && (global || firstGlobal is not null))
                {
                    using var _ = Edition.At(item.ReferenceSite);
                    Edition.Error(DiagnosticCatalog.GlobalDataNameNotUnique, $"data-name '{name}' names {(global ? "this GLOBAL item" : "this item")} "
                        + $"and also {(global && firstGlobal is null ? "another data item" : "a GLOBAL item")} described in the same source element{ViaReference(item)}; "
                        + (Edition.Edition.Cite(
                            "the same data-name must not name an item possessing the global attribute and any other data item (§1985:IV.4.3.8.1), "
                                + "and no two data items of a Data Division with the same data-name may include the GLOBAL clause (§1985:X.4.6.3 SR2)",
                            "the same name shall not be used both as the name of an item possessing the global attribute and as the name of any other data item (ISO §2023:8.4.2.1)")));
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
        foreach (var decl in SymbolTable.RepeatedIndexNames(State.Forest.OwnIndexes))
        {
            using var _ = Edition.At(decl.Table.DeclaredAt);
            Edition.Error(DiagnosticCatalog.IndexNameNotUniqueInProgram, $"index-name '{decl.Name}' of the OCCURS clause of "
                + $"'{decl.Table.CobolName ?? "FILLER"}' is already declared by an INDEXED BY phrase of this program; "
                + "index-name-1 must be a unique word within the program (§1985:VI.5.8.3 SR14)");
        }
    }
}
