// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace CobolNet.Binding.Model;

/// <summary>The name-resolution SCOPE of one lookup (P6 Step 7): <see cref="Program"/> for program/object-level
/// code, or a METHOD scope carrying the method's own name overlay (ISO §11.7.4 GR5 — method-local names SHADOW
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
/// overlay has NO entry for the name. ⛔ §11.7.4 GR5 VERBATIM, re-derived on this tree (kb/Work PB467 doubted
/// it after probing for wording GR5 does not use): "If a given user-defined word is defined in the data division
/// of this method definition and in the data division of the containing object definition, the use of that word
/// in this method refers to the declaration in this method. The declaration in the containing object definition
/// is inaccessible to this method." The rule is real, it is this clause, and it says exactly what this table
/// implements.</item>
/// <item>§8.4.6.2.3 — a method-local DATA-name shadows an object-level INDEX-name of the same spelling
/// (<see cref="IndexCandidates"/> returns null; without this every index-first consumer would silently
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

    /// <summary>⛔ THE ONE INDEX-NAME RESOLUTION (kb/Work PB919) — the §8.4.2.2 candidate set of a WRITTEN
    /// index-name reference <c>name [OF|IN q] …</c> (§8.4.2.2.2 Format 3), or <see langword="null"/> when the
    /// spelling names no index-name visible in <paramref name="scope"/> (the reference is then a data-name one).
    /// Visibility is the data-name's (§8.4.6.2.3): a method-local DATA-name of the spelling shadows every
    /// index-name (§11.7.4 GR5 — the caller must not treat the reference as an index); else the method's own
    /// declarations (§11.7.4 GR5 privacy); else the unit's, where a nearer source element's declaration wins
    /// (§8.4.6.2.1 3), <see cref="IndexNameRegistry.Candidates"/>). The qualifiers are matched from the TABLE
    /// upward (§8.4.2.2.3 SR6). The COUNT is the caller's verdict — <see cref="DataBinder.UniqueOrReportAmbiguous{T}"/>
    /// — so two tables' <c>INDEXED BY IX</c> referenced as a bare <c>IX</c> is §8.4.2.2.3 SR1's ambiguity, never a
    /// silently shared cell.
    /// <para>A table's OWN declared index (SEARCH's first index-name, the SET/PERFORM of a known table) is read off
    /// <see cref="DataItem.Indexes"/> directly — it is a declaration, not a reference, so it needs no
    /// resolution.</para></summary>
    public NameCandidates<IndexDeclaration>? IndexCandidates(string name, IReadOnlyList<string> qualifiers, Scope scope)
    {
        if (scope.Method is { } m)
        {
            if (m.ByName.TryGetValue(name, out var mlist) && mlist.Count > 0) return null;   // the method data-name wins
            if (m.IndexNames.Declares(name))
                return m.IndexNames.Candidates(name, d => _data.IndexQualifierChainMatches(d, qualifiers));
        }
        return _data.IndexNames.Declares(name)
            ? _data.IndexNames.Candidates(name, d => _data.IndexQualifierChainMatches(d, qualifiers))
            : null;
    }
}
