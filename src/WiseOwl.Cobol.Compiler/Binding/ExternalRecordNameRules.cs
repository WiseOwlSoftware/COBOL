// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// ⛔ THE EXTERNAL HALF OF THE DATA-NAME UNIQUENESS RULE, ASKED OF THE COMPILATION GROUP (kb/Work PB5427). §2023:8.4.2.1: "The
/// same name shall not be used both as the name of an external record and as the name of any other external data item
/// described in the run unit." The 1985 text bounds the reach to the programs a program is nested with: "the same data-name must
/// not be used as the name of an external record and as the name of any other external data item described in any program
/// contained within or containing the program which describes that external data record" (§1985:IV.4.3.8.1 SR4). Two
/// descriptions of ONE external record share a name (that sharing is what the EXTERNAL clause is for), so the pair this
/// refuses is an external RECORD's name reused for an item SUBORDINATE to an external record: the reuse binds the item to its
/// own area while the name of the shared record reaches another.
/// <para>The GLOBAL half is <see cref="DataBinder.ScreenGlobalNameUniqueness"/> (COBOL6862), asked of one source element; this
/// half crosses source elements, so it can only be asked once every element's data has bound, beside the other run-unit
/// rules (<see cref="ExternalFileConnectorRules"/>).</para>
/// <para>⛔ THE REACH IS THE EDITION'S, NOT A CHOICE. From COBOL-2002 the standard says "the run unit", and a compilation group
/// is the part of it a compiler sees, so every source element of the group is compared with every other (and a class's
/// object and factory halves are source elements). At COBOL-85 only a program and the programs it contains or is contained
/// by are compared. Separately compiled programs are the run time's: <c>ExternalTable</c>.</para>
/// </summary>
internal static class ExternalRecordNameRules
{
    /// <summary>One source element's data: the program it is when it is one (the containment relation is the programs'),
    /// null for a class's object or factory half, which has no container.</summary>
    internal readonly record struct Element(BoundUnit? Unit, DataBindResult Data);

    public static void Check(IReadOnlyList<Element> elements, EditionContext edition)
    {
        // The external records (root entries) of every element, by name. Most groups declare none, and then there is nothing to compare.
        Dictionary<string, List<Element>>? records = null;
        foreach (var e in elements)
            foreach (var root in e.Data.Forest.Roots)
                if (root.CobolName is { } n && e.Data.Forest.DeclaringDepth(root) == 0 && DataBinder.IsInExternalRecord(root, e.Data.Files))
                {
                    records ??= new Dictionary<string, List<Element>>(CobolNames.Comparer);
                    if (!records.TryGetValue(n, out var list)) records[n] = list = [];
                    list.Add(e);
                }
        if (records is null) return;

        bool runUnit = edition.DialectLevel >= 2002;
        foreach (var e in elements)
            foreach (var (name, items) in e.Data.Forest.ByName)
            {
                if (!records.TryGetValue(name, out var owners)) continue;
                foreach (var item in items)
                {
                    // The item must be a SUBORDINATE external item this element describes (an inherited GLOBAL is its container's own).
                    if (item.Parent is null || e.Data.Forest.DeclaringDepth(item) != 0 || !DataBinder.IsInExternalRecord(item, e.Data.Files)) continue;
                    int at = owners.FindIndex(o => runUnit || Nested(e.Unit, o.Unit));
                    if (at < 0) continue;
                    var owner = owners[at];
                    using var _ = edition.At(item.DeclaredAt);
                    edition.Error(DiagnosticCatalog.ExternalRecordNameNotUnique,
                        $"data-name '{name}' names an item subordinate to an external record and is also the name of an external record"
                        + (owner.Unit is { } u ? $" described in program '{u.Name}'" : " described in a class")
                        + (runUnit
                            ? "; the same name shall not be used both as the name of an external record and as the name of any other external data item described in the run unit (ISO §2023:8.4.2.1)"
                            : "; the same data-name must not be used as the name of an external record and as the name of any other external data item described in any program contained within or containing the program which describes that external data record (ISO §1985:IV.4.3.8.1 SR4)"));
                }
            }
    }

    /// <summary>Whether two programs are one program or one contains the other, directly or indirectly (§1985:IV.4.3.8.1 SR4's
    /// "contained within or containing"). A class half has no container, so it is nested with nothing but its own program-less self.</summary>
    private static bool Nested(BoundUnit? a, BoundUnit? b)
    {
        if (a is null || b is null) return false;
        return a == b || Contains(a, b) || Contains(b, a);
        static bool Contains(BoundUnit outer, BoundUnit inner)
        {
            for (var p = inner.Parent; p is not null; p = p.Parent)
                if (p == outer) return true;
            return false;
        }
    }
}
