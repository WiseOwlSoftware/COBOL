// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Binding.Procedure;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// ⛔ THE INHERITANCE MERGE of a contained program (docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1); kb/Work
/// PB2296): ISO §13.18.27.4 GR1 ("A constant-name, data-name, file-name, report-name, or screen-name described using a
/// GLOBAL clause is a global name") and GR2 (a program contained directly or indirectly within the program that describes
/// a global name "may reference that name without describing it again"), with §8.4.6.2.1 3)'s nearest-declaration rule.
/// The containers are read nearest first, and a parent binds before its children, so each container is complete when its
/// child reads it. Run by <c>BinderDriver.BindUnitData</c> for every program unit, in two halves:
/// <list type="bullet">
/// <item><see cref="Seed"/>, BEFORE the unit's first data phase, because the unit's own data division reads it: the
/// containers' GLOBAL constant-names (compile-time substitutions), type declarations (<c>ExpandTypes</c>), file-names,
/// record areas, reports and data roots, and the C# member names their GLOBAL roots occupy.</item>
/// <item><see cref="Merge"/>, AFTER the unit's data phases and before its procedure division binds: the formals'
/// omitted-argument guards, the GLOBAL bridges (the C# half of every inherited root) and the drop of an inherited
/// constant a nearer data-name hides.</item>
/// </list>
/// Its slice is <see cref="InheritedGlobals"/>; a lookup reads the unit's own declarations first and that slice after.
/// </summary>
internal static class InheritStep
{
    /// <summary>Write the seed half of <paramref name="unit"/>'s inheritance into <paramref name="data"/>, its binder,
    /// before <c>DataBinder.Bind</c>. An outermost unit has no container and is left as it was.</summary>
    public static void Seed(BoundUnit unit, DataBinder data, ElementBinders binders)
    {
        var inherited = data.State.Inherited;
        // The containers' GLOBAL reports (§13.18.27.3 SR1 e) / §13.18.27.4 GR1–GR2; kb/Work PB369) and GLOBAL data roots,
        // OFFERED here and made visible by the unit's own data division at the end of its declaration half
        // (ReportSectionBinder.InheritContainerReports / InheritContainerGlobalData), so the unit's own clauses that name one
        // (FILE STATUS, RECORD / RELATIVE KEY, OCCURS … DEPENDING ON, ASSIGN … USING) see it (kb/Work PB3766).
        inherited.OfferContainerGlobals(ContainerGlobalReportsOf(unit, binders), [.. ContainerGlobalRootsOf(unit, binders).Select(r => (r.Root, r.Depth))]);
        int depth = 0;   // the containment distance, 1 for the parent
        for (var anc = unit.Parent; anc is not null; anc = anc.Parent)
        {
            // The container's GLOBAL constant-names (kb/Work PB1009) — compile-time substitutions, so they join BEFORE
            // this unit binds, and a local data-name shadows them after (Merge). EVERY container, nearest first (the
            // nearest global declaration wins): an intermediate program's own NON-global declaration of the name hides
            // nothing from the programs it contains.
            inherited.AddContainerConstants(binders.Of(anc).Data.Constants.GlobalConstantsToInherit(), ++depth);
            // …its GLOBAL type declarations (§13.18.58.4 GR3; §8.4.6.2.2; kb/Work PB1303): ExpandTypes runs inside the
            // unit's first data phase, so a TYPE clause there already sees the container's type-name.
            inherited.AddContainerTypeDecls(binders.Of(anc).Data.State.Forest.TypeDecls);
            // …and its files' names (§13.18.27.4 GR1): the data division's own resolutions ask whether a name that is no
            // data item IS a visible file-name (kb/Work PB3557).
            inherited.AddContainerFiles(binders.Of(anc).Data.State.Files.Declared, binders.Of(anc).Data.Linkage.GlobalRoots, depth);
        }
        // The member names every container's GLOBAL root will occupy in this unit's class (kb/Work PB1047): the bridges are
        // emitted for every global root (Merge), so a LOCAL root spelled like one (the §8.4.6.2.1 3) a) shadowing case)
        // must take another C# name — its stem too, or a member DERIVED from the stem (a REDEFINES backing) would re-spell
        // the bridge (LinkageBinder.InheritedMemberNamesOf).
        foreach (var (anc, g, _) in ContainerBridgedRootsOf(unit, binders))
            data.ReserveInheritedMemberNames(binders.Of(anc).Data.Linkage.InheritedMemberNamesOf(g));
    }

    /// <summary>Write the merge half of <paramref name="unit"/>'s inheritance, after its data phases: its binder is
    /// <c>unit.Data</c>, <paramref name="kind"/> its source-element kind and <paramref name="turn"/> the group's
    /// TurnState.</summary>
    public static void Merge(BoundUnit unit, SourceElementKind kind, TurnState turn, ElementBinders binders)
    {
        var data = binders.Of(unit).Data;
        // kb/Work PB971 — the formals' *-ARG-OMITTED guards, set BEFORE any contained program binds (a parent binds before
        // its children), so the GLOBAL bridge below can carry a guarded root's presence member.
        EcBinder.MarkFormals(data.Linkage.Formals, kind, turn);

        var bridgedMembers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (anc, g, depth) in ContainerBridgedRootsOf(unit, binders))
        {
            // The root's NAMES are inherited already, at the end of the unit's declaration half
            // (DataBinder.InheritContainerGlobalData, offered by Seed); what is left here is the C# half. Every container
            // member a reference to the root can render, from the ONE list the container's binder owns (kb/Work PB1009 —
            // the per-residence arms used to be spelled here, and the carrier-resident formal and the BASED item's address
            // pointer were missing from them). A local root never collides with a bridge's member name:
            // ReserveInheritedMemberNames ran in Seed. A SET keyed on the emitted member (kb/Work PB1523): two global roots
            // can name ONE member — the records of a GLOBAL FD share a Tier-B class's backing, a GLOBAL redefiner and its
            // anchor share one field — and a member declared twice is CS0102. The member names are unique along the
            // container chain (ReserveInheritedMemberNames), so equal names are the same member.
            var container = binders.Of(anc).Data;
            foreach (var bridge in container.Linkage.GlobalBridgesOf(g, member => container.State.StorageTier.AnchorOf(anc.ClassName, member, depth)))   // consumed here, within this iteration
                if (bridgedMembers.Add(bridge.MemberClrName)) unit.Bridges.Add(bridge);
        }

        // Every tier is in place now — this unit's own names and every container's globals — so an inherited constant a
        // NEARER data-name hides can leave (§8.4.6.2.1 3); kb/Work PB1009 / PB1047).
        DropShadowed(data);
    }

    /// <summary>Once <paramref name="data"/>'s DATA DIVISION is bound AND every container's global data is inherited, an
    /// inherited GLOBAL constant, and an inherited symbolic-character (§12.3.7.4 GR11 a), is SHADOWED by a data-name,
    /// condition-name or index-name of the same spelling declared in a NEARER source element — this unit's own, or a nearer
    /// container's global one — by ISO §8.4.6.2.1 3) ("a) If the name is declared in source element B, the item in source
    /// element B is the referenced item. b) … 1. The item in source element A if the name is declared in source element
    /// A"), so a shadowed constant leaves <see cref="InheritedGlobals.Constants"/> before any procedure reference can
    /// substitute it (kb/Work PB1009; the nearer-container half kb/Work PB1047 — an outer program's constant used to beat
    /// the middle program's global data-name) and a shadowed symbolic-character is hidden (kb/Work PB3154 review R1-1:
    /// <c>MOVE S1 TO Y</c> moved the container's figurative instead of the local <c>01 S1</c>, with no diagnostic). Run by
    /// <see cref="Merge"/> for a program unit and by <c>OoDriver</c> for an OBJECT or FACTORY half, whose class definition
    /// hands it symbolic-characters (§8.4.6.1) and no constants.
    /// <para>Every tier a name can be declared at is asked by <c>DataBinder.DeclaredNearerThan</c>; a METHOD's own names
    /// are the one tier nearer still, asked at reference time (<c>OoDataBinder.ActiveMethodDeclares</c>).</para></summary>
    internal static void DropShadowed(DataBinder data) =>
        data.State.Inherited.DropShadowed(data.DeclaredNearerThan, data.State.Configuration.SymbolicCharacters.InheritedDepths);

    /// <summary>⛔ THE ONE LIST OF A UNIT'S INHERITED GLOBAL DATA ROOTS: every named GLOBAL root of every container of
    /// <paramref name="unit"/>, nearest container first, each with its container and how many containment levels out it
    /// lives (the nesting distance ISO §8.4.6.2.1 3) selects on). EVERY global root joins, shadowed spelling or not (kb/Work
    /// PB1047 / PB1243): §8.4.6.2.1 1) puts "all global names that are defined in source element A and in any source
    /// elements that directly or indirectly contain source element A" in the set, and §8.4.6.2.2 makes every data-name
    /// subordinate to a global name a global name in its own right — so a local 01 G hides the container's G, never its
    /// subordinate Y. WHICH candidate a reference names is §8.4.6.2.1 3)'s nearest-declaring-element rule, applied after
    /// qualification (SymbolTable.NearestDeclaring); the depth is what it reads. The names are inherited from it by
    /// <c>DataBinder.InheritContainerGlobalData</c>, the C# bridges are built from it by <see cref="Merge"/>.</summary>
    private static List<(BoundUnit Container, DataItem Root, int Depth)> ContainerGlobalRootsOf(BoundUnit unit, ElementBinders binders)
    {
        var roots = new List<(BoundUnit Container, DataItem Root, int Depth)>();
        int depth = 0;
        for (var anc = unit.Parent; anc is not null; anc = anc.Parent)
        {
            depth++;
            foreach (var g in binders.Of(anc).Data.Linkage.GlobalRoots)
                if (g.CobolName is not null)
                    roots.Add((anc, g, depth));
        }
        return roots;
    }

    /// <summary>⛔ THE ONE LIST OF THE CONTAINER ROOTS A UNIT'S CLASS BRIDGES (the C# half, <see cref="Merge"/>) AND WHOSE
    /// MEMBER NAMES IT RESERVES (<see cref="Seed"/>): the inherited GLOBAL roots (<see cref="ContainerGlobalRootsOf"/>) and,
    /// after them, the roots a container's GLOBAL files' own clauses name (<see cref="DataBinder.GlobalFileOperandRoots"/>,
    /// kb/Work PB4454). The second kind is reached by the unit's I-O statements on the file, never by a name it writes
    /// (ISO §13.18.27.4 GR2), so it is bridged and placed and is not offered as a name.</summary>
    private static List<(BoundUnit Container, DataItem Root, int Depth)> ContainerBridgedRootsOf(BoundUnit unit, ElementBinders binders)
    {
        var roots = ContainerGlobalRootsOf(unit, binders);
        int depth = 0;
        for (var anc = unit.Parent; anc is not null; anc = anc.Parent)
        {
            depth++;
            // GlobalFileOperandRoots excludes every global record, so no root is listed twice.
            roots.AddRange(binders.Of(anc).Data.GlobalFileOperandRoots().Select(root => (anc, root, depth)));
        }
        return roots;
    }

    /// <summary>The GLOBAL report description entries of every container of <paramref name="unit"/>, nearest container
    /// first, each with how many containment levels out it lives (the <c>__outer</c> chain length a reference to its
    /// storage renders through, <see cref="ReportScope.Depth"/>). Like a GLOBAL FD, the report is SHARED, never
    /// re-declared: the child's verbs hold the container's <see cref="ReportModel"/> (ISO §13.18.27.4 GR1–GR2).</summary>
    private static List<(ReportModel Report, int Depth)> ContainerGlobalReportsOf(BoundUnit unit, ElementBinders binders)
    {
        var reports = new List<(ReportModel Report, int Depth)>();
        int depth = 0;
        for (var anc = unit.Parent; anc is not null; anc = anc.Parent)
        {
            depth++;
            foreach (var r in binders.Of(anc).Data.State.Reports.Declared)
                if (r.IsGlobal)
                    reports.Add((r, depth));
        }
        return reports;
    }
}
