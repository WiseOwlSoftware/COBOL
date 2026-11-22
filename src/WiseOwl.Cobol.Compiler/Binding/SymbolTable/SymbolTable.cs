// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// THE ONE scope-aware name resolver (P6 Step 7 — collapses the <c>LookupData</c> / <c>LookupDataInScopeOf</c> /
/// <c>TryGetVisibleIndexField</c> / <c>IndexFieldFor</c> quadruple; the singular-pattern fix). Semantics are the
/// quadruple's, verbatim:
/// <list type="bullet">
/// <item>§2023:8.4.6.2.1 rule 3a / §2023:11.7.4 GR5 — a method-local name REPLACES (never unions with) the object/program
/// name: a lookup consults the scope's method overlay FIRST and falls through to the global maps only when the
/// overlay has NO entry for the name. ⛔ §2023:11.7.4 GR5 VERBATIM, re-derived on this tree (kb/Work PB467 doubted
/// it after probing for wording GR5 does not use): "If a given user-defined word is defined in the data division
/// of this method definition and in the data division of the containing object definition, the use of that word
/// in this method refers to the declaration in this method. The declaration in the containing object definition
/// is inaccessible to this method." The rule is real, it is this clause, and it says exactly what this table
/// implements.</item>
/// <item>§2023:8.4.6.2.3 — a method-local DATA-name shadows an object-level INDEX-name of the same spelling
/// (<c>IndexCandidates</c> returns null; without this every index-first consumer would silently
/// bind the subscript/SET target to the OBJECT's index cell — a torn read/write of the wrong storage).</item>
/// <item>§2023:11.7.4 GR5 index privacy — a method-local index-name has its OWN cell, never the shared global one.</item>
/// <item>§2023:8.4.6.2.1 GR3 for a CONTAINED program — the unit map holds its own names and every container's global
/// names, each at its nesting depth, and <see cref="NearestDeclaring{T}"/> narrows a qualified candidate set to the
/// nearest declaring source element (kb/Work PB1047 / PB1243).</item>
/// </list>
/// <para>Backed by the owning <see cref="DataBinder"/>'s live name maps (the P6 Step-7a wrapper stage — no data
/// moves; <c>SymbolTableBuilder</c>-owned storage is deferred to P7 per the phase doc). One table per binder:
/// COBOL name scopes are PER-UNIT (each program/class forest has its own namespace), so the table lives on
/// <see cref="DataBinder.Symbols"/> rather than the compilation record — recorded as a deviation from the
/// PHASE-06 doc's single <c>BoundCompilation.Symbols</c> sketch, which presumed a merged namespace that does
/// not exist.</para>
/// </summary>
public sealed partial class SymbolTable
{
    private readonly DataBinder _data;

    internal SymbolTable(DataBinder data)
    {
        _data = data;
        // The group's entries (DESIGN-listing.md section 6.7.1, "Step 6b, the entry ids"); a binder outside every compilation
        // group (a unit test's, the parameter-name screen of OoExpansion) is a group of its own.
        _entries = data.OoClasses?.Entries ?? new EntryStore();
        Declared = new Declarations(this);
    }

    /// <summary>The data division's declaration questions, answered as facts (<see cref="Declarations"/>).</summary>
    internal Declarations Declared { get; }

    /// <summary>Resolve a DATA-name in <paramref name="scope"/>: the method overlay first (§2023:8.4.6.2.1 rule 3a),
    /// else the unit's global multimap. False when the name is unknown in both. (← <c>LookupData</c> /
    /// <c>LookupDataInScopeOf</c> — the anchor-root-vs-active-method decision is now the caller's explicit
    /// <see cref="Scope"/>.) A method that declares the spelling in ANOTHER name class (a condition-name, an
    /// index-name, a CAPACITY register, a constant-name — <see cref="SymbolTable.MethodDeclaresWord"/>) makes the object's data-name of it
    /// inaccessible (§2023:11.7.4 GR5 "the use of that word in this method refers to the declaration in this method"), so
    /// the lookup is false rather than the object's item (kb/Work PB3165 review R2-1's sibling arm).</summary>
    private bool TryResolve(string name, Scope scope, out List<DataItem> items)
    {
        var key = Key(name);
        if (scope.Method is { } m)
        {
            if (_methodNames.TryGetValue(m, out var own) && own.DataNames.TryGetValue(key, out var mlist) && mlist.Count > 0)
            {
                items = WithLapsedTypeMembers(key, mlist, m);   // the method's own type declarations (kb/Work PB3192)
                return true;
            }
            if (MethodDeclaresWord(m, name)) { items = []; return false; }
        }
        if (_dataNames.TryGetValue(key, out var list) && list.Count > 0)
        {
            items = WithLapsedTypeMembers(key, list, null);
            return true;
        }
        items = [];
        return false;
    }

    /// <summary>⛔ ISO §2023:8.4.6.2.1 GR3 — THE NEAREST-DECLARING-ELEMENT RULE for data-names and condition-names
    /// (kb/Work PB1047 / PB1243), the data-name twin of the index-name table's depth tiers (<c>IndexNameTable.Candidates</c>). A
    /// contained program's name set holds its own names AND every container's global names (rule 1), so after the
    /// reference's qualifiers have been applied over that WHOLE set (rule 1: "the normal rules for qualification …
    /// are applied until one or more items is identified"), a plural survivor set is narrowed to the survivors
    /// declared in the NEAREST source element: "a) If the name is declared in source element B, the item in source
    /// element B is the referenced item. b) Otherwise … 1. The item in source element A if the name is declared in
    /// source element A. 2. The item in the containing source element …". Two survivors in that one element are still
    /// ambiguous — the count stays the caller's verdict. <paramref name="declarationOf"/> maps a candidate to the data
    /// item that places it (a condition-name is placed by its conditional variable). Returns
    /// <paramref name="candidates"/> itself when there is nothing to narrow — one survivor, or a unit that inherits
    /// no global data — so the common reference allocates nothing.</summary>
    public List<T> NearestDeclaring<T>(List<T> candidates, Func<T, DataItem> declarationOf)
    {
        if (candidates.Count < 2 || !_data.State.Forest.InheritsGlobalData) return candidates;
        int nearest = int.MaxValue;
        bool mixed = false;
        foreach (var c in candidates)
        {
            int depth = _data.State.Forest.DeclaringDepth(declarationOf(c));
            if (nearest != int.MaxValue && depth != nearest) mixed = true;
            if (depth < nearest) nearest = depth;
        }
        if (!mixed) return candidates;
        List<T> tier = [];
        foreach (var c in candidates)
            if (_data.State.Forest.DeclaringDepth(declarationOf(c)) == nearest) tier.Add(c);
        return tier;
    }

    /// <summary>⛔ ISO §2023:8.4.6.2.1 GR3 FOR A DATA-NAME REFERENCE — THE ONE NARROWING OF A QUALIFIED SET, in every lookup that has one
    /// (<see cref="TryResolveUnqualified"/>; <see cref="DataNameCandidates"/>, which applies the qualifier walk and passes the
    /// survivors): <see cref="NearestDeclaring{T}"/> keeps the survivors of the nearest declaring source element, and that tier
    /// stands only when no declaration of ANOTHER class of §1985:X.1.3.8.2 GR1's set, reached by the written
    /// <paramref name="qualifiers"/>, is nearer (<see cref="NearestInNameSet"/>, kb/Work PB5975): "If the name is declared in
    /// program B, the object in program B is the referenced object" (GR3a), whatever class B declares it as. A nearer report entry
    /// (kb/Work PB4196), condition-name (kb/Work PB4219), file-name or report-name (kb/Work PB5786, PB5973), index-name, cd-name,
    /// constant-name or CAPACITY register leaves NO survivor; the caller then meets an unidentified reference and its own verdict
    /// says what the name is declared as (<see cref="Declarations.OtherThanData"/>, or the report entry's refusal,
    /// §2023:13.8.6.2.3), instead of using a container's GLOBAL item the nearer declaration hides.</summary>
    private List<DataItem> NearestDeclaringData(List<DataItem> candidates, string name, IReadOnlyList<string> qualifiers)
    {
        var tier = NearestDeclaring(candidates, static i => i);
        if (tier.Count == 0) return tier;
        return DeclaredNearerInNameSet(name, qualifiers, _data.State.Forest.DeclaringDepth(tier[0]), NameSetClass.All & ~NameSetClass.DataItem)
            ? []
            : tier;
    }

    /// <summary>The condition-name twin of <see cref="NearestDeclaringData"/>: the nearest declaring source element's
    /// condition-names, or none when a declaration of another class of the spelling is declared nearer
    /// (<see cref="NearestInNameSet"/>, kb/Work PB4196, PB4219, PB4432, PB5975). The CAPACITY register is the one class not asked
    /// here: it has ONE lookup, which judges the written reference (<c>CapacityRegisterReferenceDriftTests</c>), and
    /// <c>ConditionBinder.ConditionOf</c> asks that one of a condition-name. The caller's own verdict then says what the name is
    /// declared as.</summary>
    private List<Condition88> NearestDeclaringConditions(List<Condition88> candidates, string name, IReadOnlyList<string> qualifiers)
    {
        var tier = NearestDeclaring(candidates, static c => c.Parent);
        if (tier.Count == 0) return tier;
        return DeclaredNearerInNameSet(name, qualifiers, _data.State.Forest.DeclaringDepth(tier[0].Parent),
                NameSetClass.All & ~(NameSetClass.ConditionName | NameSetClass.CapacityRegister))
            ? []
            : tier;
    }

    /// <summary>Is <see cref="NearestDeclaringConditions"/>' empty answer for <paramref name="candidates"/> (the
    /// level-88s the written <paramref name="qualifiers"/> reach) the work of an ordinary DATA ITEM of the spelling
    /// declared in a nearer source element (ISO §2023:8.4.6.2.1 GR3a)? A condition-only position then meets a reference that
    /// names a data item, which is neither a misqualified condition-name nor an undeclared one (kb/Work PB4219 review
    /// R1-1).</summary>
    private bool NearerDataItemHidesConditions(List<Condition88> candidates, string name, IReadOnlyList<string> qualifiers)
    {
        var tier = NearestDeclaring(candidates, static c => c.Parent);
        return tier.Count != 0
            && DeclaredNearerInNameSet(name, qualifiers, _data.State.Forest.DeclaringDepth(tier[0].Parent), NameSetClass.DataItem);
    }

    /// <summary>The candidates an UNQUALIFIED data-name reference can name: <see cref="TryResolve"/>'s set narrowed
    /// by <see cref="NearestDeclaringData"/> (ISO §2023:8.4.6.2.1 GR3) — with no qualifier to apply, the nearest declaring
    /// source element's items are the whole set, and a report entry of a nearer one hides them all). False when the
    /// word names no data item here, or only items a nearer report entry hides. ⛔ A caller that picks among the
    /// candidates BY KIND (the FUNCTION-POINTER of §2023:8.4.3.2.3 SR4, a dynamic-capacity table) filters THIS set, never the
    /// raw one: filtering first skips a nearer declaration of another kind — or counts a container's hidden global of
    /// the same kind as a rival — and so resolves the name to an item §2023:8.4.6.2.1 GR3a) makes inaccessible (wave 69 Z,
    /// found by the train-68b review on <c>IntrinsicBinder.FunctionPointerNamed</c>).</summary>
    private bool TryResolveUnqualified(string name, Scope scope, out List<DataItem> items)
    {
        if (!TryResolve(name, scope, out items)) return false;
        items = NearestDeclaringData(items, name, []);
        return items.Count != 0;
    }

    /// <summary>Resolve a level-88 CONDITION-name in <paramref name="scope"/>: the method overlay first, else the
    /// unit's global multimap (the same §2023:8.4.6.2.1 rule-3a precedence the data-name lookup applies). A method-local
    /// name of the spelling in any other class hides the object's condition-name (§2023:11.7.4 GR5, as in
    /// <see cref="TryResolve"/>).</summary>
    private bool TryResolveCondition(string name, Scope scope, out List<Condition88> conds)
    {
        var key = Key(name);
        if (scope.Method is { } m)
        {
            if (_methodNames.TryGetValue(m, out var own) && own.ConditionNames.TryGetValue(key, out var mlist) && mlist.Count > 0)
            {
                conds = WithLapsedTypeConditions(key, mlist, m);   // the method's own type declarations (kb/Work PB3192)
                return true;
            }
            if (MethodDeclaresWord(m, name)) { conds = []; return false; }
        }
        if (_conditionNames.TryGetValue(key, out var list) && list.Count > 0)
        {
            conds = WithLapsedTypeConditions(key, list, null);
            return true;
        }
        conds = [];
        return false;
    }



    /// <summary>⛔ ISO §2023:8.4.6.2.1 GR3 FOR A NAME CLASS THAT THE ORDINARY LOOKUPS DO NOT RANK — an index-name
    /// (<c>IndexCandidates</c>) or a named CAPACITY register (<c>ReferenceResolver.CapacityRegisterFor</c>), resolved before the
    /// data-name lookup — against EVERY other class of §1985:X.1.3.8.2's set (<see cref="NearestInNameSet"/>): is the spelling,
    /// reached by the written <paramref name="qualifiers"/>, declared in a class other than <paramref name="own"/> in a source element
    /// NEARER than <paramref name="depth"/>? "If the name is declared in source element B, the item in source element B is the
    /// referenced item", whichever class B declares it as, so a contained program's own data item, level-88, RD or SELECT hides its
    /// container's GLOBAL index-name exactly as its own data item does (kb/Work PB1047, PB4432, PB5973, PB5975). At EQUAL depth the
    /// asking class keeps the reference.</summary>
    internal bool NearerNameOfAnotherClassHides(string name, IReadOnlyList<string> qualifiers, int depth, NameSetClass own) =>
        DeclaredNearerInNameSet(name, qualifiers, depth, NameSetClass.All & ~own);

    /// <summary>Makes ONE container GLOBAL item (no recursion: <c>DataBinder.InheritGlobalSubtree</c> walks the subtree) visible here,
    /// <paramref name="depth"/> containment levels out (ISO §2023:13.18.27.4 GR2, "may reference that name without describing it
    /// again"): its declaring depth (<see cref="ForestSlice.Inherit"/>), its data-name and the condition-names associated with it, its index-names and
    /// its CAPACITY register. A register the container refused (SR30's COBOL1523) failed that compile and has none to inherit.</summary>
    internal void InheritGlobalItem(DataItem item, int depth)
    {
        _data.State.Forest.Inherit(item, depth);
        if (item.CobolName is { } name) DeclareDataName(item, name, null);
        foreach (var cond in item.Own88s) DeclareConditionName(cond, null);
        foreach (var index in item.Indexes) _indexNames.Add(Key(index.Name), index, depth);
        if (item.OccursSpec is { CapacityName: { } cap, CapacityRegister: not null }) DeclareCapacityRegister(null, cap, item);
    }
}
