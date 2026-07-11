// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace CobolNet.Binding.Model;

/// <summary>The name-resolution SCOPE of one lookup (P6 Step 7): <see cref="Program"/> for program/object-level
/// code, or a METHOD scope carrying the method's own name overlay (ISO §11.7 GR5 — method-local names SHADOW
/// object/program names and are invisible to sibling methods). The scope is an EXPLICIT parameter of every
/// <see cref="SymbolTable"/> lookup — the "which overload" decision the old <c>LookupData</c>/
/// <c>LookupDataInScopeOf</c> pair encoded in the METHOD NAME is now data.</summary>
public readonly record struct Scope(OoMethodDataScope? Method)
{
    /// <summary>The program/object-level scope (no method overlay).</summary>
    public static Scope Program => new((OoMethodDataScope?)null);
}

/// <summary>
/// THE ONE scope-aware name resolver (P6 Step 7 — collapses the <c>LookupData</c> / <c>LookupDataInScopeOf</c> /
/// <c>TryGetVisibleIndexField</c> / <c>IndexFieldFor</c> quadruple; the singular-pattern fix). Semantics are the
/// quadruple's, verbatim:
/// <list type="bullet">
/// <item>§8.4.6.2.1 rule 3a / §11.7.4 GR5 — a method-local name REPLACES (never unions with) the object/program
/// name: a lookup consults the scope's method overlay FIRST and falls through to the global maps only when the
/// overlay has NO entry for the name.</item>
/// <item>§8.4.6.2.3 — a method-local DATA-name shadows an object-level INDEX-name of the same spelling
/// (<see cref="TryResolveIndex"/> returns false; without this every IndexFields-first consumer would silently
/// bind the subscript/SET target to the OBJECT's index cell — a torn read/write of the wrong storage).</item>
/// <item>§11.7.4 GR5 index privacy — a method-local index-name has its OWN cell, never the shared global one.</item>
/// </list>
/// <para>Backed by the owning <see cref="DataBinder"/>'s live name maps (the P6 Step-7a wrapper stage — no data
/// moves; <c>SymbolTableBuilder</c>-owned storage is deferred to P7 per the phase doc). One table per binder:
/// COBOL name scopes are PER-UNIT (each program/class forest has its own namespace), so the table lives on
/// <see cref="DataBinder.Symbols"/> rather than the compilation record — recorded as a deviation from the
/// PHASE-06 doc's single <c>BoundCompilation.Symbols</c> sketch, which presumed a merged namespace that does
/// not exist.</para>
/// </summary>
public sealed class SymbolTable
{
    private readonly DataBinder _data;

    internal SymbolTable(DataBinder data) => _data = data;

    /// <summary>Resolve a DATA-name in <paramref name="scope"/>: the method overlay first (§8.4.6.2.1 rule 3a),
    /// else the unit's global multimap. False when the name is unknown in both. (← <c>LookupData</c> /
    /// <c>LookupDataInScopeOf</c> — the anchor-root-vs-active-method decision is now the caller's explicit
    /// <see cref="Scope"/>.)</summary>
    public bool TryResolve(string name, Scope scope, out List<DataItem> items)
    {
        if (scope.Method is { } m && m.ByName.TryGetValue(name, out var mlist) && mlist.Count > 0)
        {
            items = mlist;
            return true;
        }
        if (_data.ByName.TryGetValue(name, out var list) && list.Count > 0)
        {
            items = list;
            return true;
        }
        items = [];
        return false;
    }

    /// <summary>Resolve a level-88 CONDITION-name in <paramref name="scope"/>: the method overlay first, else the
    /// unit's global multimap (the same §8.4.6.2.1 rule-3a precedence the data-name lookup applies).</summary>
    public bool TryResolveCondition(string name, Scope scope, out List<Condition88> conds)
    {
        if (scope.Method is { } m && m.Conditions.TryGetValue(name, out var mlist) && mlist.Count > 0)
        {
            conds = mlist;
            return true;
        }
        if (_data.Conditions.TryGetValue(name, out var list) && list.Count > 0)
        {
            conds = list;
            return true;
        }
        conds = [];
        return false;
    }

    /// <summary>VISIBILITY-checked INDEX-name resolution (← <c>TryGetVisibleIndexField</c>): false when a
    /// method-local DATA-name shadows the spelling (§8.4.6.2.1 rule 3a — the data-name wins and the caller must
    /// not treat the reference as an index); else the method's own cell (§11.7.4 GR5 privacy), else the unit's
    /// global cell.</summary>
    public bool TryResolveIndex(string name, Scope scope, out string field)
    {
        field = "";
        if (scope.Method is { } m && m.ByName.TryGetValue(name, out var mlist) && mlist.Count > 0)
            return false;   // the method-local data-name wins (§8.4.6.2.1 rule 3a)
        if (scope.Method is { } ms && ms.IndexFields.TryGetValue(name, out field!)) return true;
        return _data.IndexFields.TryGetValue(name, out field!);
    }

    /// <summary>The resolved-cell accessor for a KNOWN index-name (← <c>IndexFieldFor</c>): the method's own cell
    /// first (§11.7.4 GR5), else the global cell — throwing on a miss, exactly like the dictionary indexer it
    /// replaces. DELIBERATELY without the data-name-shadow check: callers pass a TABLE'S DECLARED index-name
    /// (never a user-written reference), where shadowing does not apply — folding this shape into
    /// <see cref="TryResolveIndex"/> would CHANGE behavior when a method data-name happens to share an object
    /// table's index spelling (the reason the quadruple had two index members; recorded in PHASE-06 §STATUS).</summary>
    public string IndexCellOf(string name, Scope scope) =>
        scope.Method is { } m && m.IndexFields.TryGetValue(name, out var cell) ? cell : _data.IndexFields[name];
}
