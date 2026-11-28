// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler.Binding.Passes;

namespace WiseOwl.Cobol.Compiler.Binding.Model;

/// <summary>
/// The type forest of ONE bound unit (ISO §2023:8.4.6.2.1 GR3): the roots of its WORKING-STORAGE, LOCAL-STORAGE and the rest of the
/// forest, the element's own index declarations and type declarations in source order, and the inheritance tier of each GLOBAL item a
/// container offered. It holds no name table: every name of the element (its data-names, condition-names, index-names, type-names,
/// CAPACITY registers and screen-names) is keyed by <see cref="SymbolTable"/>, the one store of names (docs/rearchitecture/DESIGN-listing.md
/// section 6.7.1)
/// (docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1); kb/Work PB2296, step 1 of the <see cref="DataBinder"/>
/// extract, M6). A value slice of <see cref="DataBindState"/>: its one writer is the data binder's declaration walk
/// (<c>BindEntries</c>, the roots and the declarations) and its inheritance of a container's GLOBAL subtree, plus the one late door
/// <see cref="AddTemporaryRoot"/> that the procedure binders' compiler temporaries use; everyone else reads the
/// read-only views below. <c>DataBindStateBoundaryDriftTests</c> pins the writers.
/// <para>A model-layer value: it names no binder type, so <see cref="DataBindResult"/> hands it to a backend as it is (kb/Work
/// PB2361). It has no gated read: the CAPACITY registers' <see cref="PassPhase.OccursResolved"/> watermark gate is the symbol
/// table's (<c>SymbolTable.CapacityRegisterNamed</c>).</para>
/// </summary>
public sealed class ForestSlice
{
    private readonly List<DataItem> _roots = [];
    private readonly List<DataItem> _workingStorageRoots = [];
    private readonly List<DataItem> _localStorageRoots = [];
    private readonly Dictionary<DataItem, int> _inheritedGlobalDepth = new(ReferenceEqualityComparer.Instance);
    private readonly List<DataItem> _typeDeclarations = [];

    /// <summary>The top-level (01/77) items, in source order: WORKING-STORAGE, the FILE records and the compiler
    /// temporaries share it (<see cref="WorkingStorageRoots"/> and <see cref="LocalStorageRoots"/> are the section subsets).</summary>
    public IReadOnlyList<DataItem> Roots => _roots;

    /// <summary>The unit's WORKING-STORAGE SECTION roots, in source order — the subset of <see cref="Roots"/> whose storage
    /// class is decided by §2023:13.5.4 (static/initial data), captured at bind so the static-WS routing
    /// (<see cref="RouteStaticUnitStoragePass.Run"/>) and the emitter's <c>__ResetStatics</c> never guess from the mixed
    /// forest (FILE records and compiler temps share <see cref="Roots"/>). The ONE walk <c>__ResetStatics</c> emits from; each
    /// arm there is gated on the static-channel membership, so an automatic-storage root in this list contributes nothing.</summary>
    public IReadOnlyList<DataItem> WorkingStorageRoots => _workingStorageRoots;

    /// <summary>The unit's LOCAL-STORAGE SECTION roots, in source order (ISO §2023:13.6 — automatic data, §2023:13.6.4 GR1). Emitted as
    /// ordinary INSTANCE fields: for an INITIAL or RECURSIVE unit the fresh instance per activation IS the §2023:14.6.2.3.2
    /// per-activation initial state; for a cached-singleton unit the emitted <c>Call</c> entry re-initializes them
    /// (ProgramEmitter) — automatic data is in initial state on EVERY activation regardless of the unit's attributes.</summary>
    public IReadOnlyList<DataItem> LocalStorageRoots => _localStorageRoots;

    /// <summary>THE INDEX-NAME DECLARATIONS THIS SOURCE ELEMENT MAKES, in declaration order (kb/Work PB919): every INDEXED BY of
    /// its own data division, each with its own <c>long</c> cell (1-based occurrence numbers, DESIGN §3.5), the cells it emits.
    /// The index-names, its own and the GLOBAL tables' of every containing program (§2023:8.4.6.2.3), are the symbol table's
    /// (<c>SymbolTable.DeclareIndex</c>, which adds each own declaration here); a reference resolves through it, counted, never
    /// keyed by spelling alone.</summary>
    public IReadOnlyList<IndexDeclaration> OwnIndexes => _ownIndexes;
    private readonly List<IndexDeclaration> _ownIndexes = [];


    /// <summary>THIS source element's own TYPEDEF type declarations, in source order (ISO §2023:13.18.58; data-model D17): each the
    /// template root <see cref="DataItem"/>. A METHOD's declarations are its own source element's
    /// (<see cref="OoMethodDataScope.TypeDeclarations"/>, kb/Work PB3192), and <c>DataBinder.AllTypeDecls</c> walks both. Their
    /// type-names are the symbol table's (<c>SymbolTable.DeclareType</c>, which adds each here; docs/rearchitecture/DESIGN-listing.md
    /// section 6.7.1). The template is built by <c>BindEntries</c> but kept OFF <see cref="Roots"/> and the symbol table's data-names (it
    /// allocates no storage; its subordinate names are not globally referenceable, GR1/GR2). A <c>TYPE IS type-name</c> reference
    /// clones this subtree into the referencing entry in the post-build <see cref="ExpandTypesPass.Run"/> pass, which runs before
    /// every resolution pass, so each sees the clone.</summary>
    public IReadOnlyList<DataItem> TypeDeclarations => _typeDeclarations;

    // ── The inheritance tier ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>How many containment levels out <paramref name="item"/> is declared: 0 for an item this source element
    /// declares (or any item of a unit that inherits nothing), n for a GLOBAL item of its n-th container. ⛔ THE NESTING
    /// DISTANCE OF EVERY INHERITED GLOBAL DECLARATION (kb/Work PB1047 / PB1243) — the tier ISO §2023:8.4.6.2.1 GR3 selects on. Keyed
    /// by identity, because the SAME container item is inherited into every contained program at a different distance.</summary>
    public int DeclaringDepth(DataItem item) =>
        _inheritedGlobalDepth.Count != 0 && _inheritedGlobalDepth.TryGetValue(item, out int depth) ? depth : 0;

    /// <summary>True when this unit inherits any container's global data — the only case in which a candidate set can span
    /// tiers, so the nearest-tier filter is free for every other unit.</summary>
    public bool InheritsGlobalData => _inheritedGlobalDepth.Count != 0;



    // ── The writers (internal: the data binder's declaration walk, its passes and its inheritance of a container's GLOBALs) ──

    /// <summary>Append a top-level item to the forest, in source order.</summary>
    internal void AddRoot(DataItem root) => _roots.Add(root);

    /// <summary>⛔ THE LATE DOOR: append the root of a compiler temporary (<see cref="DataItem.IsCompilerTemp"/>) a PROCEDURE
    /// binder minted (<c>CompilerTemps.Create</c>: a subscript, sending-value or function-result temporary, an ACCEPT or DISPLAY
    /// operand), after the declaration walk and every data pass. The temporary joins <see cref="Roots"/> and no name table (its
    /// name is never referenceable), and the slice is complete only when the procedure binders are done (the group tail, when
    /// <c>DataBindResult</c> is built). <c>CompilerTemps</c> is its one caller.</summary>
    internal void AddTemporaryRoot(DataItem temp) => _roots.Add(temp);

    /// <summary>Record the roots a WORKING-STORAGE SECTION bound.</summary>
    internal void AddWorkingStorageRoots(IEnumerable<DataItem> roots) => _workingStorageRoots.AddRange(roots);

    /// <summary>Record the roots a LOCAL-STORAGE SECTION bound.</summary>
    internal void AddLocalStorageRoots(IEnumerable<DataItem> roots) => _localStorageRoots.AddRange(roots);

    /// <summary>Append one INDEXED BY declaration of this element's own data division, in declaration order:
    /// <c>SymbolTable.DeclareIndex</c>'s, once it has keyed the index-name.</summary>
    internal void AddOwnIndex(IndexDeclaration decl) => _ownIndexes.Add(decl);

    /// <summary>Append a TYPEDEF template of this element, in source order: <c>SymbolTable.DeclareType</c>'s, once it has keyed the
    /// type-name.</summary>
    internal void AddTypeDeclaration(DataItem template) => _typeDeclarations.Add(template);

    /// <summary>Record that ONE container GLOBAL item (no recursion: the caller walks the subtree) is inherited here,
    /// <paramref name="depth"/> containment levels out (ISO §2023:13.18.27.4 GR2: "may reference that name without describing it
    /// again"): <c>SymbolTable.InheritGlobalItem</c>'s, which keys its names.</summary>
    internal void Inherit(DataItem item, int depth) => _inheritedGlobalDepth[item] = depth;

}
