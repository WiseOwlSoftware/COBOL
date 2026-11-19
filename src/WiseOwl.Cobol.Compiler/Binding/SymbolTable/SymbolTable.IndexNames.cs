// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

public sealed partial class SymbolTable
{
    /// <summary>⛔ THE INDEX-NAME NAMESPACE OF ONE SCOPE (an element, or a method's §2023:11.7.4 GR5 tables) — every declaration
    /// VISIBLE there, each tagged with the nesting distance of the source element that declares it (0 = this one; n = the n-th
    /// containing program, whose GLOBAL tables §2023:8.4.6.2.3 makes visible: "the scope of an index-name is identical to that of
    /// the data-name that names the table"). Keyed by the declaration (the pair name and table, <see cref="IndexDeclaration"/>),
    /// never by spelling alone: two tables that each declare <c>INDEXED BY IX</c> are two declarations (kb/Work PB919).</summary>
    private sealed class IndexNameTable
    {
        private readonly Dictionary<CobolName, List<(IndexDeclaration Decl, int Depth)>> _byName = [];

        /// <summary>Records a declaration <paramref name="depth"/> containment levels out (0 for this scope's own).</summary>
        public void Add(CobolName name, IndexDeclaration decl, int depth)
        {
            if (!_byName.TryGetValue(name, out var list)) _byName[name] = list = [];
            list.Add((decl, depth));
        }

        /// <summary>True when some visible declaration carries the spelling: the reference IS an index-name reference (qualified
        /// correctly or not), never a data-name one.</summary>
        public bool Declares(CobolName name) => _byName.ContainsKey(name);

        /// <summary>The §2023:8.4.2.2 candidate set of a written index-name reference: the visible declarations of
        /// <paramref name="name"/> that <paramref name="qualifies"/> admits, then ISO §2023:8.4.6.2.1's rule for a name duplicated
        /// across nested source elements ("3) If more than one item is identified, no more than one of them may have a name local
        /// to source element B … a) If the name is declared in source element B, the item in source element B is the referenced
        /// item", else the nearest containing element that declares it, 3 b). So the survivors at the NEAREST depth are the set
        /// (two there are still ambiguous), and <paramref name="nearestDepth"/> is that depth (<see cref="int.MaxValue"/> when none
        /// survive), which <see cref="IndexCandidates"/> weighs against a same-spelled DATA-name's (kb/Work PB1047).</summary>
        public NameCandidates<IndexDeclaration> Candidates(CobolName name, Func<IndexDeclaration, bool> qualifies, out int nearestDepth)
        {
            nearestDepth = int.MaxValue;
            if (!_byName.TryGetValue(name, out var list)) return new([]);
            int nearest = int.MaxValue;
            foreach (var (d, depth) in list)
                if (depth < nearest && qualifies(d)) nearest = depth;
            List<IndexDeclaration> hits = [];
            foreach (var (d, depth) in list)
                if (depth == nearest && qualifies(d)) hits.Add(d);
            nearestDepth = nearest;
            return new(hits);
        }
    }

    /// <summary>This element's index-names: its own INDEXED BY declarations and the GLOBAL tables' declarations of every containing
    /// program (§2023:8.4.6.2.3), inherited beside their tables' data-names (<see cref="InheritGlobalItem"/>). A METHOD's own are its
    /// <see cref="MethodNames.IndexNames"/> (§2023:11.7.4 GR5 privacy: a method index has its own cell).</summary>
    private readonly IndexNameTable _indexNames = new();

    /// <summary>Declares one INDEXED BY index-name in the scope that owns its table: <paramref name="method"/>'s when the table is a
    /// method's, else this element's, whose own declarations are also the cells it emits (<see cref="ForestSlice.OwnIndexes"/>).</summary>
    internal void DeclareIndex(IndexDeclaration decl, OoMethodDataScope? method)
    {
        if (method is null)
        {
            _data.State.Forest.AddOwnIndex(decl);
            _indexNames.Add(Key(decl.Name), decl, 0);
        }
        else MethodNamesOf(method).IndexNames.Add(Key(decl.Name), decl, 0);
    }

    /// <summary>⛔ THE ONE INDEX-NAME RESOLUTION (kb/Work PB919): the §2023:8.4.2.2 candidate set of a WRITTEN index-name reference
    /// <c>name [OF|IN q] …</c> (§2023:8.4.2.2.2 Format 3), or <see langword="null"/> when the spelling names no index-name visible
    /// in <paramref name="scope"/> (the reference is then a data-name one). Visibility is the data-name's (§2023:8.4.6.2.3): a
    /// method-local DATA-name, condition-name, CAPACITY register or constant-name of the spelling shadows every index-name
    /// (§2023:11.7.4 GR5: the caller must not treat the reference as an index); else the method's own declarations (§2023:11.7.4 GR5
    /// privacy); else the unit's, where a nearer source element's declaration wins (§2023:8.4.6.2.1 3)). The qualifiers are matched
    /// from the TABLE upward (§2023:8.4.2.2.3 SR6). The COUNT is the caller's verdict (<see cref="DataBinder.UniqueOrReportAmbiguous{T}"/>),
    /// so two tables' <c>INDEXED BY IX</c> referenced as a bare <c>IX</c> is §2023:8.4.2.2.3 SR1's ambiguity, never a silently shared
    /// cell. A table's OWN declared index (SEARCH's first index-name, the SET/PERFORM of a known table) is read off
    /// <see cref="DataItem.Indexes"/> directly: it is a declaration, not a reference.</summary>
    private NameCandidates<IndexDeclaration>? IndexCandidates(string name, IReadOnlyList<string> qualifiers, Scope scope)
    {
        var key = Key(name);
        if (scope.Method is { } m)
        {
            _methodNames.TryGetValue(m, out var own);
            if (own is not null && own.DataNames.TryGetValue(key, out var mlist) && mlist.Count > 0) return null;   // the method data-name wins
            if (own is not null && own.IndexNames.Declares(key))
                return WithLapsedTypeIndexes(key,
                    own.IndexNames.Candidates(key, d => _data.IndexQualifierChainMatches(d, qualifiers), out _),
                    d => _data.IndexQualifierChainMatches(d, qualifiers), m);   // the method's own type declarations (kb/Work PB3192)
            // A method condition-name, CAPACITY register or constant-name of the spelling hides the object's
            // index-name too (GR5 covers every name class; kb/Work PB3165 review R2-1, PB3153 review round 3).
            if (MethodDeclaresWord(m, name)) return null;
        }
        if (!_indexNames.Declares(key)) return null;
        var indexes = _indexNames.Candidates(key, d => _data.IndexQualifierChainMatches(d, qualifiers), out int indexDepth);
        // §2023:8.4.6.2.1 3) across the name classes (kb/Work PB1047): a DATA-name or a CONDITION-name of the spelling that the
        // written qualifiers reach, declared in a NEARER source element than every such index-name, is the referenced
        // item — a contained program's `01 IX` or its `88 IX` hides its container's GLOBAL table's `INDEXED BY IX`,
        // exactly as its `01 X` hides the container's global `X` (kb/Work PB4432). At EQUAL depth the index-name keeps
        // the reference, as before.
        if (indexes.Count > 0 && NearerDataOrConditionNameHides(name, qualifiers, indexDepth)) return null;
        return WithLapsedTypeIndexes(key, indexes, d => _data.IndexQualifierChainMatches(d, qualifiers), null);
    }

    /// <summary>Whether an index-name of <paramref name="name"/> visible to this element is declared in a source element nearer than
    /// <paramref name="depth"/> (<see cref="Declarations.DeclaredNearerThan"/>).</summary>
    private bool IndexNameDeclaredNearerThan(string name, int depth)
    {
        var key = Key(name);
        if (!_indexNames.Declares(key)) return false;
        _indexNames.Candidates(key, static _ => true, out int indexDepth);   // int.MaxValue when none survives
        return indexDepth < depth;
    }
}
