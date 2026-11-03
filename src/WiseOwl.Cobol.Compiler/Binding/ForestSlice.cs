// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Binding.Passes;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// The type forest of ONE bound unit and the name tables over it (ISO §8.4.6.2.1 3): the roots of its WORKING-STORAGE,
/// LOCAL-STORAGE and the rest of the forest, every referenceable data-name, condition-name, index-name and named CAPACITY
/// register, the type declarations, the screen-names and the inheritance tier of each GLOBAL name a container offered
/// (docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1); kb/Work PB2296, step 1 of the <see cref="DataBinder"/>
/// extract, M6). A value slice of <see cref="DataBindState"/>: its one writer is the data binder's declaration walk
/// (<c>DataBinder.RegisterName</c>, <c>RegisterCondition</c>, <c>BindEntries</c> and the post-build TYPE copies, which
/// register through the same funnel) and its inheritance of a container's GLOBAL subtree; everyone else reads the
/// read-only views below. <c>DataBindStateBoundaryDriftTests</c> pins the writers.
/// <para>A lookup that must see a METHOD's own names first asks the method's <see cref="OoMethodDataScope"/> before it asks
/// this slice (<see cref="Model.SymbolTable"/>); the mutators here take that scope, because a name joins the index of the
/// source element that owns its record (§11.7.4 GR5; kb/Work PB3192).</para>
/// </summary>
public sealed class ForestSlice(Action<PassPhase, string> require)
{
    private readonly List<DataItem> _roots = [];
    private readonly List<DataItem> _workingStorageRoots = [];
    private readonly List<DataItem> _localStorageRoots = [];
    private readonly Dictionary<string, List<DataItem>> _byName = new(CobolNames.Comparer);
    private readonly Dictionary<string, List<Condition88>> _conditions = new(CobolNames.Comparer);
    private readonly Dictionary<string, List<DataItem>> _capacityRegisters = new(CobolNames.Comparer);
    private readonly Dictionary<DataItem, int> _inheritedGlobalDepth = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<string, DataItem> _typeDecls = new(CobolNames.Comparer);
    private readonly HashSet<string> _screenNames = new(CobolNames.Comparer);

    /// <summary>The top-level (01/77) items, in source order: WORKING-STORAGE, the FILE records and the compiler
    /// temporaries share it (<see cref="WorkingStorageRoots"/> and <see cref="LocalStorageRoots"/> are the section subsets).</summary>
    public IReadOnlyList<DataItem> Roots => _roots;

    /// <summary>The unit's WORKING-STORAGE SECTION roots, in source order — the subset of <see cref="Roots"/> whose storage
    /// class is decided by §13.5.4 (static/initial data), captured at bind so the static-WS routing
    /// (<see cref="RouteStaticUnitStoragePass.Run"/>) and the emitter's <c>__ResetStatics</c> never guess from the mixed
    /// forest (FILE records and compiler temps share <see cref="Roots"/>). The ONE walk <c>__ResetStatics</c> emits from; each
    /// arm there is gated on the static-channel membership, so an automatic-storage root in this list contributes nothing.</summary>
    public IReadOnlyList<DataItem> WorkingStorageRoots => _workingStorageRoots;

    /// <summary>The unit's LOCAL-STORAGE SECTION roots, in source order (ISO §13.6 — automatic data, §13.6.4 GR1). Emitted as
    /// ordinary INSTANCE fields: for an INITIAL or RECURSIVE unit the fresh instance per activation IS the §14.6.2.3.2
    /// per-activation initial state; for a cached-singleton unit the emitted <c>Call</c> entry re-initializes them
    /// (ProgramEmitter) — automatic data is in initial state on EVERY activation regardless of the unit's attributes.</summary>
    public IReadOnlyList<DataItem> LocalStorageRoots => _localStorageRoots;

    /// <summary>
    /// Every named item of this source element and every GLOBAL one a container offered, keyed by COBOL name
    /// (case-insensitive) → the items with that name. COBOL permits duplicate data-names disambiguated only by qualification
    /// (OF/IN), so this is a MULTIMAP — a single-valued dictionary would silently drop all but the last (a latent
    /// wrong-item bug; DESIGN §3.5). Nearest declaring source element first (the inherited items join after the unit's own).
    /// </summary>
    public IReadOnlyDictionary<string, List<DataItem>> ByName => _byName;

    /// <summary>Level-88 condition-names (case-insensitive) → the conditions with that name (a list, since names may be
    /// duplicated under different parents and disambiguated by qualification).</summary>
    public IReadOnlyDictionary<string, List<Condition88>> Conditions => _conditions;

    /// <summary>THE UNIT'S INDEX-NAME NAMESPACE (kb/Work PB919): every INDEXED BY declaration of this unit's own data division
    /// (<see cref="IndexNameRegistry.Own"/> — the <c>long</c> cells it emits, 1-based occurrence numbers, DESIGN §3.5), plus
    /// the GLOBAL tables' declarations of every containing program (§8.4.6.2.3), registered beside their tables' data-names
    /// by <see cref="Inherit"/>. A reference resolves through <see cref="Model.SymbolTable.IndexCandidates"/> — counted,
    /// never keyed by spelling alone. Declared into only by <c>DataBinder.RegisterIndexes</c> and <see cref="Inherit"/>.</summary>
    public IndexNameRegistry IndexNames { get; } = new();

    /// <summary>OCCURS DYNAMIC <c>CAPACITY IN data-name-3</c> register-names (case-insensitive) → the owning dynamic-table
    /// <see cref="DataItem"/> (ISO §13.18.38 GR15 / §8.5.1.9.1; data-model D9). The register is IMPLICITLY defined at the OCCURS
    /// entry (SR30) — it is NOT in <see cref="ByName"/>; the resolver consults this map to build a
    /// <see cref="CapacityRegisterPlace"/> (a view over the table's <c>Capacity</c>). Populated by the post-build
    /// <see cref="DynamicResolvePass.Run"/> pass; the getter carries the P6 Step-6 watermark gate — a read before
    /// <see cref="PassPhase.OccursResolved"/> throws loud, the "read a null CapacityRegister" silent-miscompile class made
    /// structural.
    /// <para>⛔ A LIST PER NAME, NEAREST DECLARING SOURCE ELEMENT FIRST (kb/Work PB1674). The register of a dynamic table in a
    /// GLOBAL record is subordinate to a global name — SR30 places it "at the same level as the entry containing the OCCURS
    /// clause" — so it is a global name too (§8.4.6.2.2: "All data-names and screen-names subordinate to a global name are
    /// global names"), and a contained program inherits it with the rest of the record (<see cref="Inherit"/>). This
    /// element's own registers come first (one written definition per spelling: SR30's "shall not be defined elsewhere in the
    /// source element", reproduced once per TYPE / SAME AS copy of its entry, kb/Work PB2988), then each container's, so
    /// <c>ReferenceResolver.CapacityRegisterFor</c> can apply §8.4.6.2.1 3)'s nearest-element rule. The one reader of a name
    /// in this map is <see cref="Model.SymbolTable.CapacityCandidates"/>. The OWN registers alone are the tables whose
    /// <see cref="DeclaringDepth"/> is 0.</para></summary>
    public IReadOnlyDictionary<string, List<DataItem>> CapacityRegisters
    {
        get { require(PassPhase.OccursResolved, "CapacityRegisters"); return _capacityRegisters; }
    }

    /// <summary>The capacity registers recorded SO FAR, with no watermark gate: for the phases that produce and screen them
    /// (<see cref="DynamicResolvePass"/>, which writes them at <see cref="PassPhase.OccursResolved"/>, and the declaration
    /// funnel's "is this word declared nearer" question, which runs while they are still being minted).</summary>
    internal IReadOnlyDictionary<string, List<DataItem>> CapacityRegistersSoFar => _capacityRegisters;

    /// <summary>TYPEDEF type declarations (case-insensitive) → the template root <see cref="DataItem"/> (ISO §13.18.58;
    /// data-model D17) — THIS source element's own: a METHOD's declarations are its own source element's, in
    /// <see cref="OoMethodDataScope.TypeDecls"/> (kb/Work PB3192), and <c>DataBinder.AllTypeDecls</c> walks both. The template
    /// is built by <c>BindEntries</c> but kept OFF <see cref="Roots"/> and <see cref="ByName"/> (it allocates no storage; its
    /// subordinate names are not globally referenceable, GR1/GR2). A <c>TYPE IS type-name</c> reference clones this subtree
    /// into the referencing entry in the post-build <see cref="ExpandTypesPass.Run"/> pass — which runs before every resolution
    /// pass, so each sees the clone.</summary>
    public IReadOnlyDictionary<string, DataItem> TypeDecls => _typeDecls;

    /// <summary>Names DECLARED in the SCREEN SECTION, which is REFUSED as the declined Annex A.4.2 module
    /// (COBOL1560; <see cref="ScreenFacility"/>). Two consumers, both still needed after the refusal:
    /// <list type="number">
    /// <item>kb/Work R32 — a reference to such a name is exempt from the §8.4.2.1 UNDEFINED diagnostic
    /// (COBOL1639). The compile already fails on the section; adding "SG is not defined" on top would send
    /// the user hunting a declaration that is right there. "Not defined" and "declared in a refused section"
    /// are different verdicts, and only the second is true.</item>
    /// <item>It is how <c>ACCEPT screen-name-1</c> / <c>DISPLAY screen-name-1</c> are told apart from their
    /// token-identical DEVICE formats (AcceptDisplayBinder.ScreenFormatOf) — the operand's DECLARATION is the
    /// only thing that distinguishes them.</item></list>
    /// ⚠ The posture used to be compile-ACCEPT with a staged runtime loud; kb/Work PB260 measured what that
    /// actually did (a screen record printed to standard output) and made it a refusal.</summary>
    public IReadOnlySet<string> ScreenNames => _screenNames;

    // ── The inheritance tier ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>How many containment levels out <paramref name="item"/> is declared: 0 for an item this source element
    /// declares (or any item of a unit that inherits nothing), n for a GLOBAL item of its n-th container. ⛔ THE NESTING
    /// DISTANCE OF EVERY INHERITED GLOBAL DECLARATION (kb/Work PB1047 / PB1243) — the tier ISO §8.4.6.2.1 3) selects on. Keyed
    /// by identity, because the SAME container item is inherited into every contained program at a different distance.</summary>
    public int DeclaringDepth(DataItem item) =>
        _inheritedGlobalDepth.Count != 0 && _inheritedGlobalDepth.TryGetValue(item, out int depth) ? depth : 0;

    /// <summary>True when this unit inherits any container's global data — the only case in which a candidate set can span
    /// tiers, so the nearest-tier filter is free for every other unit.</summary>
    public bool InheritsGlobalData => _inheritedGlobalDepth.Count != 0;

    /// <summary>⛔ THE ONE QUESTION "IS THIS WORD A DATA-NAME THIS SOURCE ELEMENT ITSELF DEFINES?" — the test behind every rule
    /// that reads "shall not be defined elsewhere in the source element" (§13.18.38.3 SR30 for a CAPACITY register,
    /// §13.18.64.3 SR2 for a report VARYING counter) and behind the report section's "also declared in ordinary storage"
    /// screens (§8.4.2.1). A container's GLOBAL data-name is inherited into <see cref="ByName"/> before the resolve passes run
    /// (<see cref="Inherit"/>), but it is defined in ANOTHER source element: this element's own declaration of the spelling
    /// hides it (§8.4.6.2.1 3), so it can neither duplicate nor be duplicated by it. Asking <c>ByName.ContainsKey</c> instead
    /// refuses a legal program (kb/Work PB3766, review finding R2-1).</summary>
    public bool DeclaresOwnDataName(string name) =>
        _byName.TryGetValue(name, out var items) && items.Any(i => DeclaringDepth(i) == 0);

    /// <summary>The type declarations of the source element <paramref name="scope"/> names: a METHOD's own table when the scope
    /// is a method's (§8.4.6.2.2; kb/Work PB3192), else this unit's.</summary>
    public IReadOnlyDictionary<string, DataItem> TypeDeclsOf(Scope scope) => scope.Method?.TypeDecls ?? _typeDecls;

    // ── The writers (internal: the data binder's declaration walk, its passes and its inheritance of a container's GLOBALs) ──

    /// <summary>Append a top-level item to the forest, in source order.</summary>
    internal void AddRoot(DataItem root) => _roots.Add(root);

    /// <summary>Record the roots a WORKING-STORAGE SECTION bound.</summary>
    internal void AddWorkingStorageRoots(IEnumerable<DataItem> roots) => _workingStorageRoots.AddRange(roots);

    /// <summary>Record the roots a LOCAL-STORAGE SECTION bound.</summary>
    internal void AddLocalStorageRoots(IEnumerable<DataItem> roots) => _localStorageRoots.AddRange(roots);

    /// <summary>Index a named item in the by-name multimap of the source element that owns its record: <paramref name="method"/>'s
    /// own when its record is a method's, else this unit's — COBOL allows duplicate names disambiguated only by qualification.
    /// The one insert of a bound entry and of a TYPE copy alike (kb/Work PB3192).</summary>
    internal void DeclareName(DataItem item, string name, OoMethodDataScope? method)
    {
        var byName = method?.ByName ?? _byName;
        if (!byName.TryGetValue(name, out var list)) byName[name] = list = [];
        list.Add(item);
    }

    /// <summary>Index a level-88 condition-name in the condition multimap of the source element that owns its conditional
    /// variable's record (<paramref name="method"/>'s when it is a method's; kb/Work PB3192).</summary>
    internal void DeclareCondition(Condition88 cond, OoMethodDataScope? method)
    {
        var conditions = method?.Conditions ?? _conditions;
        if (!conditions.TryGetValue(cond.Name, out var list)) conditions[cond.Name] = list = [];
        list.Add(cond);
    }

    /// <summary>Declare one INDEXED BY index-name of this unit's own data division, or of the method that owns its table.</summary>
    internal void DeclareIndex(IndexDeclaration decl, OoMethodDataScope? method) =>
        (method?.IndexNames ?? IndexNames).Declare(decl);

    /// <summary>Declare a TYPEDEF template in the declaring source element's table (a method's type-name is local to the
    /// method, §8.4.6.2.2; kb/Work PB3192); false when the spelling is already declared there.</summary>
    internal bool TryDeclareType(DataItem template, string name, Scope scope) =>
        (scope.Method?.TypeDecls ?? _typeDecls).TryAdd(name, template);

    /// <summary>Record the screen-names the refused SCREEN SECTION declared.</summary>
    internal void AddScreenNames(IEnumerable<string> names)
    {
        foreach (var n in names) _screenNames.Add(n);
    }

    /// <summary>Make ONE container GLOBAL item (no recursion: the caller walks the subtree) visible here,
    /// <paramref name="depth"/> containment levels out (ISO §13.18.27.4 GR2 — "may reference that name without describing it
    /// again"): its data-name, the condition-names associated with it, its index-names and its CAPACITY register.</summary>
    internal void Inherit(DataItem item, int depth)
    {
        _inheritedGlobalDepth[item] = depth;
        if (item.CobolName is { } name)
        {
            if (!_byName.TryGetValue(name, out var list)) _byName[name] = list = [];
            list.Add(item);
        }
        foreach (var cond in item.Own88s)
        {
            if (!_conditions.TryGetValue(cond.Name, out var conds)) _conditions[cond.Name] = conds = [];
            conds.Add(cond);
        }
        foreach (var idx in item.Indexes) IndexNames.Inherit(idx, depth);
        // Placed by depth (AddCapacityRegister): this element's own registers, minted later by DynamicResolve, sort
        // in before it, a nearer container's before a farther one's — the order
        // ReferenceResolver.CapacityRegisterFor reads. A register the container refused (SR30's COBOL1523)
        // failed that compile and has no CapacityRegister view to inherit.
        if (item.OccursSpec is { CapacityName: { } cap, CapacityRegister: not null }) AddCapacityRegister(null, cap, item);
    }

    /// <summary>The ONE insert into <see cref="CapacityRegisters"/>: this element's own register (from
    /// <see cref="DynamicResolvePass.Run"/>) and each container's GLOBAL one (from <see cref="Inherit"/>, nearest container
    /// first), into <paramref name="method"/>'s own register index when the table is a method's. ⛔ THE LIST IS KEPT IN
    /// NEAREST-DECLARING-ELEMENT ORDER BY THE INSERT, NOT BY THE ORDER OF THE CALLS (kb/Work PB1674; PB3766 review R2-1): the
    /// containers' names are inherited before this element's registers are minted, so a register goes after the last one at
    /// its own depth or nearer — the order <c>ReferenceResolver.CapacityRegisterFor</c> reads (<c>tables[0]</c> is the
    /// nearest).</summary>
    internal void AddCapacityRegister(OoMethodDataScope? method, string name, DataItem table)
    {
        var registers = method?.CapacityRegisters ?? _capacityRegisters;
        if (!registers.TryGetValue(name, out var tables)) registers[name] = tables = [];
        int depth = DeclaringDepth(table);
        int at = tables.Count;
        while (at > 0 && DeclaringDepth(tables[at - 1]) > depth) at--;
        tables.Insert(at, table);
    }
}
