// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler.Binding.Model;

namespace WiseOwl.Cobol.Compiler.Binding;

using Core = WiseOwl.Cobol.Frontend.Parsing.CobolParserCore;

/// <summary>The names the procedure division header and the LINKAGE SECTION's entry rules ask of an element's linkage records,
/// before the bind's name tables are complete: the header runs inside the data division's bind (<c>LinkageBinder.CallBindLinkage</c>,
/// <c>OoDataBinder.BindMethodData</c>), so each answer reads the records it is handed, by the table's one name equality
/// (docs/rearchitecture/DESIGN-listing.md section 6.7.1, "Equality of COBOL names is private too").</summary>
public sealed partial class SymbolTable
{
    /// <summary>The level-01 or level-77 LINKAGE SECTION record among <paramref name="linkageRoots"/> that a procedure division
    /// header's USING operand or RETURNING item <paramref name="written"/> names (§2023:14.2.2 SR1: each shall be such an entry);
    /// null when none does, which the header reports. The header is the reference itself, so its occurrence is recorded.</summary>
    internal DataItem? IdentifyHeaderOperand(IEnumerable<DataItem> linkageRoots, Core.DataReferenceContext written)
    {
        var key = Key(written.GetText());
        var item = linkageRoots.FirstOrDefault(r => r.CobolName is { } n && Key(n) == key);
        if (item is not null) RecordData(item, written, report: true);
        return item;
    }

    /// <summary>The names the ADDRESS OF operands <paramref name="targets"/> write (their data-names, qualifiers aside): the formals
    /// so named are AREA formals, never carrier-resident (<c>LinkageBinder.FormalIsCarrierResident</c>, kb/Work PB1019).</summary>
    internal static AddressedNames AddressedBy(IEnumerable<AddressTarget> targets) =>
        new([.. targets.Select(t => Key(t.Written.Name))]);

    /// <summary>The data-names an element's ADDRESS OF operands write (<see cref="AddressedBy"/>).</summary>
    internal sealed class AddressedNames(HashSet<CobolName> names)
    {
        /// <summary>None: a header with no formal to classify asks nothing (kb/Work PB4302).</summary>
        public static readonly AddressedNames None = new([]);

        /// <summary>Does an ADDRESS OF operand write the data-name of <paramref name="item"/>?</summary>
        public bool Names(DataItem item) => item.CobolName is { } n && names.Contains(Key(n));
    }

    /// <summary>The record among <paramref name="roots"/>, other than <paramref name="redefiner"/>, that its REDEFINES clause's
    /// data-name-2 names: an element's LINKAGE records, read while the header binds, before <c>LinkRedefinitionsPass</c> links
    /// the redefinition. The search is among the level-01 and level-77 roots alone because "the level-numbers of data-name-2 and the
    /// subject of the entry shall be identical" (§2023:13.18.44.3 SR2), and it compares the written word alone because
    /// "Data-name-2 shall not be qualified" (SR6). Null when the item redefines nothing among them.</summary>
    internal static DataItem? RedefinedAmong(DataItem redefiner, IEnumerable<DataItem> roots)
    {
        if (redefiner.RedefinesTargetName is not { } target) return null;
        var key = Key(target);
        return roots.FirstOrDefault(o => !ReferenceEquals(o, redefiner) && o.CobolName is { } n && Key(n) == key);
    }

    /// <summary>Does a record among <paramref name="roots"/>, other than <paramref name="item"/>, redefine it (its REDEFINES clause's
    /// data-name-2 names <paramref name="item"/>)? Read while the header binds, as <see cref="RedefinedAmong"/> is.</summary>
    internal static bool IsRedefinedAmong(DataItem item, IEnumerable<DataItem> roots) =>
        item.CobolName is { } name && roots.Any(r => !ReferenceEquals(r, item) && r.RedefinesTargetName is { } t && Key(t) == Key(name));

    /// <summary>Does the data-name of <paramref name="subject"/> require qualification for uniqueness among the object (or factory)
    /// data <paramref name="objectItems"/>? A PROPERTY clause may be specified only for an item whose name does not (§2023:13.18.42.3
    /// SR3): a declaration question of the class's own data.</summary>
    internal static bool NeedsQualificationAmong(DataItem subject, IEnumerable<DataItem> objectItems) =>
        subject.CobolName is { } name && objectItems.Count(i => i.CobolName is { } n && Key(n) == Key(name)) > 1;
}
