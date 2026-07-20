// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Passes;
using CobolNet.Common;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Cst;
using CobolNet.Frontend.Generated;

using CobolNet.Binding.Model;

using CobolNet.Compiler.Oo;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>The DATA DIVISION section a run of data-description entries belongs to — consumed by the
/// section-scoped placement rules (e.g. CONSTANT RECORD is WS/LS-only, ISO §13.18.15.3 SR1).</summary>
internal enum EntrySection
{
    WorkingStorage,
    LocalStorage,
    Linkage,
    File,
}

/// <summary>
/// Builds the bound DATA DIVISION model (a forest of <see cref="DataItem"/> trees, one per 01/77 item) from the
/// parse tree, and indexes every named item for reference resolution. Pure syntactic/semantic analysis — no byte
/// layout; the .NET type IS the storage. (Slice scope: WORKING-STORAGE groups + elementary items with fixed
/// OCCURS recorded; FILE/LINKAGE/LOCAL-STORAGE, level-66/88, and REDEFINES follow in later slices.)
/// </summary>
public sealed partial class DataBinder(EditionContext? edition = null)
{
    private int _fillerCounter;
    private int _uidCounter;

    /// <summary>The targeted-edition context (digit caps, bind-time rejection diagnostics). Defaults to the
    /// latest edition for direct test construction; <c>CompilerDriver</c> always supplies the CLI's
    /// <c>--std</c>.</summary>
    public EditionContext Edition { get; } = edition ?? new EditionContext(2023);

    /// <summary>The group's compile-time <c>&gt;&gt;REF-MOD-ZERO-LENGTH</c> resolution (ISO §7.3.23) — queried by
    /// <see cref="ReferenceResolver"/> when it builds a reference-modification Place, to set
    /// <c>RefModPlace.AllowZeroLength</c> from the directive fold at the ref-mod's source line (§8.4.3.3.4 item 5c).
    /// Defaults to <see cref="RefModZeroLengthState.Empty"/> (the OFF default) for direct test construction.</summary>
    public RefModZeroLengthState RefModZeroLength { get; init; } = RefModZeroLengthState.Empty;

    /// <summary>The top-level (01/77) items of WORKING-STORAGE, in source order. (READ-ONLY view — P6 Step 5:
    /// the emitter consumes the bound model without a write channel; the binder populates the private backing.)</summary>
    public IReadOnlyList<DataItem> Roots => _roots;
    private readonly List<DataItem> _roots = [];

    /// <summary>True when this binder binds a RECURSIVE-and-not-INITIAL PROGRAM unit or a FUNCTION unit
    /// (functions "are always recursive", ISO §8.6.6 :8821 / §9.4 :12529) that has NO contained programs — the
    /// unit whose WORKING-STORAGE emits STATIC (set once by <c>BinderDriver.BindUnitData</c>, init-only).
    /// <para><b>The §-derivation this flag realizes</b> (storage class × unit kind → copy semantics):
    /// §13.5.4 GR1 — WS of "a program that does not have the initial attribute, a function, a factory, or an
    /// object" is STATIC data; §14.6.2.3.3 — "Static and external data are the only data that are in the
    /// last-used state", so a recursive unit's WS is ONE copy shared across ALL concurrent and successive
    /// activations, placed in initial state only per the §14.6.2.3.2 triggers (first activation in the run
    /// unit; first activation after an activation of an INITIAL container; first activation after CANCEL).
    /// §13.6.4 GR1 — LOCAL-STORAGE is AUTOMATIC data → §14.6.2.3.2 "Automatic data and initial data is placed
    /// in the initial state every time the … program … is activated" (a separate copy per activation).
    /// LINKAGE formals are the activator's storage (§13.7.1), per-activation by construction; EXTERNAL data is
    /// run-unit storage on the ExternalStore (§8.6.7, last-used per §14.6.2.3.3), never per-class static.
    /// An INITIAL program's WS is INITIAL data (§13.5.4 GR2) → per-activation initial state, so the flag keys
    /// on Recursive AND NOT Initial (the two attributes are mutually exclusive anyway, §11.10.3 SR5–6).
    /// A NON-recursive NON-initial program's WS is equally static data, but such a program can never be
    /// concurrently active (§8.6.6 :8823) — the registry's cached singleton instance realizes its last-used
    /// state without statics, byte-identically to the pre-slice emission.</para></summary>
    public bool UnitStaticWs { get; init; }

    /// <summary>The unit's WORKING-STORAGE SECTION roots, in source order — the subset of <see cref="Roots"/>
    /// whose storage class is decided by §13.5.4 (static/initial data), captured at bind so the static-WS
    /// routing (<see cref="RouteStaticUnitStorage"/>) and the emitter's <c>__ResetStatics</c> never guess from
    /// the mixed forest (FILE records and compiler temps share <see cref="Roots"/>).</summary>
    public IReadOnlyList<DataItem> WorkingStorageRoots => _workingStorageRoots;
    private readonly List<DataItem> _workingStorageRoots = [];

    /// <summary>The unit's LOCAL-STORAGE SECTION roots, in source order (ISO §13.6 — automatic data,
    /// §13.6.4 GR1). Emitted as ordinary INSTANCE fields: for an INITIAL or RECURSIVE unit the fresh instance
    /// per activation IS the §14.6.2.3.2 per-activation initial state; for a cached-singleton unit the emitted
    /// <c>Call</c> entry re-initializes them (ProgramEmitter) — automatic data is in initial state on EVERY
    /// activation regardless of the unit's attributes.</summary>
    public IReadOnlyList<DataItem> LocalStorageRoots => _localStorageRoots;
    private readonly List<DataItem> _localStorageRoots = [];

    /// <summary>
    /// Every named item, keyed by COBOL name (case-insensitive) → the list of items with that name. COBOL permits
    /// duplicate data-names disambiguated only by qualification (OF/IN), so this is a MULTIMAP — a single-valued
    /// dictionary would silently drop all but the last (a latent wrong-item bug; COBOLNET_DESIGN §3.5).
    /// </summary>
    public Dictionary<string, List<DataItem>> ByName { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>INDEXED BY index-names (case-insensitive) → the C# <c>long</c> field that holds the 1-based
    /// occurrence number (COBOLNET_DESIGN §3.5). A subscript may name an index, so the resolver consults this.
    /// (READ-ONLY view — P6 Step 5; the GLOBAL-inheritance preseed writes through
    /// <see cref="SeedInheritedGlobalIndex"/>.)</summary>
    public IReadOnlyDictionary<string, string> IndexFields => _indexFields;
    private readonly Dictionary<string, string> _indexFields = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Pre-seed one inherited GLOBAL-table index name BEFORE <see cref="Bind"/> (ISO §13.18.27 GR2 —
    /// a global index-name is SHARED storage reached through the ref-bridge, never re-declared locally): registers
    /// the CONTAINER's cell under the name and suppresses the field from this unit's emission. False when the
    /// name is already taken (a nearer declaration shadows). The ONE write channel BinderDriver uses (P6 Step 5).</summary>
    internal bool SeedInheritedGlobalIndex(string idxName, string field)
    {
        if (!_indexFields.TryAdd(idxName, field)) return false;
        _callSuppressedRootFields.Add(field);
        return true;
    }

    /// <summary>Level-88 condition-names (case-insensitive) → the conditions with that name (a list, since names
    /// may be duplicated under different parents and disambiguated by qualification).</summary>
    public Dictionary<string, List<Condition88>> Conditions { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>OCCURS DYNAMIC <c>CAPACITY IN data-name-3</c> register-names (case-insensitive) → the owning
    /// dynamic-table <see cref="DataItem"/> (ISO §13.18.38 GR15 / §8.5.1.9.1; data-model D9). The register is
    /// IMPLICITLY defined at the OCCURS entry (SR30) — it is NOT in <see cref="ByName"/>; the resolver consults
    /// this map to build a <see cref="CapacityRegisterPlace"/> (a view over the table's <c>Capacity</c>). Populated
    /// by the post-build <see cref="DynamicResolve"/> pass. (READ-ONLY view — P6 Step 5; the getter carries the
    /// P6 Step-6 watermark gate — a read before <see cref="Passes.PassPhase.OccursResolved"/> throws loud, the
    /// "read a null CapacityRegister" silent-miscompile class made structural.)</summary>
    public IReadOnlyDictionary<string, DataItem> CapacityRegisters
    {
        get { Require(PassPhase.OccursResolved, "CapacityRegisters"); return _capacityRegisters; }
    }
    private readonly Dictionary<string, DataItem> _capacityRegisters = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>TYPEDEF type declarations (case-insensitive) → the template root <see cref="DataItem"/> (ISO
    /// §13.18.58; data-model D17). The template is built by <see cref="BindEntries"/> but kept OFF <see cref="Roots"/>
    /// and <see cref="ByName"/> (it allocates no storage; its subordinate names are not globally referenceable,
    /// GR1/GR2). A <c>TYPE IS type-name</c> reference clones this subtree into the referencing entry in the post-build
    /// <see cref="ExpandTypes"/> pass — which runs before <see cref="BindResolve"/>, so every resolution pass sees the
    /// clone (the invariant the OO compiler-temp clone already relies on).</summary>
    public Dictionary<string, DataItem> TypeDecls { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Group items used as a WHOLE character-image operand (MOVE/DISPLAY/compare/ACCEPT/record I-O/whole-group
    /// CORRESPONDING pair/boundary formal) — collected AFTER binding by <see cref="Passes.UsageCollectionPass"/>'s
    /// typed walk of the bound tree (+ the structural FILE-record and boundary-formal sources), NOT by
    /// <see cref="ReferenceResolver"/> mid-resolve (which over-collected every RESOLVED group; PHASE-05 Step 5). The
    /// <c>StorageFormPass</c> consults this to decide which numeric-DISPLAY leaves must store their character
    /// image (ISO §14.9 MOVE GR4 — a whole-group move fills without conversion; see <see cref="DataItem.StoreAsImage"/>).
    /// </summary>
    public HashSet<DataItem> WholeGroupReferenced { get; } = [];

    /// <summary>The COLLECTED image-storage facts (PHASE-05 Step 7 — the flag-write sites become fact records):
    /// every elementary item some bind-time rule forces to store its CHARACTER IMAGE, recorded at the exact
    /// moment the legacy <c>StoreAsImage = true</c> writes fired — Tier-B/CALL-cell REDEFINES leaves (resolve),
    /// FILE-record leaves (resolve), report print faces (resolve), figurative-fill and ref-mod-store receivers
    /// (procedure bind). <see cref="Passes.StorageFormPass"/> unions this with the compiler-temp re-sync and the
    /// whole-group promotion to compute <see cref="DataItem.Storage"/> WITHOUT reading the mutable flag; the
    /// bind-time <c>NumericImagePlace</c> wrap decision reads it with the same mid-bind timing the flag had.</summary>
    public IReadOnlySet<DataItem> ImageForcedItems => _imageForcedItems;
    private readonly HashSet<DataItem> _imageForcedItems = [];

    /// <summary>Record one image-storage fact (the ONE write channel for <see cref="ImageForcedItems"/>).</summary>
    internal void MarkImageForced(DataItem item) => _imageForcedItems.Add(item);

    /// <summary>The BIND-TIME image-storage query (P5.7): true when <paramref name="item"/> is already known —
    /// at THIS point of binding — to store its character image. Reads the collected facts with EXACTLY the
    /// mid-bind timing the deleted mutable flag had (a fact recorded by an earlier statement's bind is visible
    /// to a later statement's; one recorded later is not — the pre-P5.7 order dependency, preserved verbatim
    /// and noted for the P7 lazy-place redesign). Post-bind consumers read <see cref="DataItem.StoreAsImage"/>
    /// (the Storage projection) instead.</summary>
    internal bool IsImageBackedEarly(DataItem item) => _imageForcedItems.Contains(item);

    /// <summary>The fully-parsed OPTIONS paragraph (ISO §11.9), program-level context for every later pass — the
    /// binder applies DEFAULT ROUNDED today (a bare ROUNDED phrase uses <see cref="OptionsModel.DefaultRounding"/>);
    /// the remaining clauses are captured for the features that will consume them. Defaults when no OPTIONS.</summary>
    public OptionsModel Options { get; private set; } = OptionsModel.Default;

    /// <summary>All SELECTed files (the SELECT clause joined with its FD records), in source order.
    /// (READ-ONLY view — P6 Step 5. The bind-phase file-connector qualification mutates the FileModel ELEMENTS,
    /// which a read-only list does not prevent — element immutability is a later data-model-track item.)</summary>
    public IReadOnlyList<FileModel> Files => _files;
    private readonly List<FileModel> _files = [];

    /// <summary>The files keyed by COBOL file-name (case-insensitive), for the binder to resolve OPEN/READ/CLOSE
    /// targets and to map a WRITE/REWRITE record-name back to its owning file.</summary>
    public Dictionary<string, FileModel> FilesByName { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>True when SOURCE-COMPUTER declares WITH DEBUGGING MODE (the X3.23-1985 compile-time debug
    /// switch) — consumed by the declaratives binder to decide the USE FOR DEBUGGING posture: with the switch the
    /// debugging section is compiled AND its procedure-trigger leg is modeled (fires; the DEBUG-ITEM register
    /// resolves), without it the section is comment-treated (VCR Table 7 rows 7.9/7.17).</summary>
    public bool DebuggingModeDeclared { get; private set; }

    /// <summary>The X3.23-1985 <c>DEBUG-ITEM</c> special register and its members (DEBUG-LINE / DEBUG-NAME /
    /// DEBUG-SUB-1/2/3 / DEBUG-CONTENTS), keyed by COBOL name (case-insensitive) → the synthesized alphanumeric
    /// <see cref="DataItem"/> VIEW + its read-only runtime read expression. The register is IMPLICITLY described
    /// (no DATA DIVISION entry, §ISO absent — 1985 debug module, VCR Table 7 row 7.17) — kept OFF <see cref="ByName"/>
    /// / <see cref="Roots"/>; the resolver consults this to build a <see cref="DebugRegisterPlace"/>. Populated by
    /// <see cref="ActivateDebugRegisters"/> when a procedure-subject debugging declarative is collected under
    /// WITH DEBUGGING MODE (empty otherwise — a non-debug program's resolution is unchanged).</summary>
    public IReadOnlyDictionary<string, (DataItem Item, DebugRegisterMember Member)> DebugRegisters => _debugRegisters;
    private readonly Dictionary<string, (DataItem, DebugRegisterMember)> _debugRegisters = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Register the X3.23-1985 DEBUG-ITEM special-register family (idempotent) — called by the procedure
    /// table builder when it collects a <c>USE FOR DEBUGGING</c> procedure-subject declarative under WITH DEBUGGING
    /// MODE, BEFORE statement binding resolves the DEBUG-* references inside the debugging section. Each member is a
    /// synthesized elementary VIEW of its fixed 1985 width over the program-instance <c>__dbgItem</c>
    /// (<see cref="CobolNet.Runtime.DebugItem"/>) — carried as a STRUCTURAL <see cref="DebugRegisterMember"/> selector
    /// (the C# read text lives in the renderer); the whole-group DEBUG-ITEM reads the concatenated image. DEBUG-SUB-n
    /// carries the S9(4) SIGN LEADING SEPARATE image WIDTH (5) but as an alphanumeric character-image view: the
    /// procedure-trigger leg only ever renders it SPACES, and the subscripted numeric-value population + the
    /// §14.9.25.4 GR6a "sign not moved" MOVE semantics ride the staged data-name leg (COBOLNET1571).</summary>
    internal void ActivateDebugRegisters()
    {
        if (_debugRegisters.Count > 0) return;
        void Reg(string name, int width, DebugRegisterMember member) =>
            _debugRegisters[name] = (new DataItem
            {
                Level = 49,
                CsName = "__dbg_" + name.Replace('-', '_'),
                CobolName = name,
                Pic = new PicInfo(PicCategory.Alphanumeric, Usage.Display, Length: width, Digits: 0, Scale: 0, Signed: false),
                Uid = _uidCounter++,
            }, member);
        Reg("DEBUG-ITEM", Runtime.DebugItem.GroupWidth, DebugRegisterMember.Item);
        Reg("DEBUG-LINE", Runtime.DebugItem.LineWidth, DebugRegisterMember.Line);
        Reg("DEBUG-NAME", Runtime.DebugItem.NameWidth, DebugRegisterMember.Name);
        Reg("DEBUG-SUB-1", Runtime.DebugItem.SubWidth, DebugRegisterMember.Sub1);
        Reg("DEBUG-SUB-2", Runtime.DebugItem.SubWidth, DebugRegisterMember.Sub2);
        Reg("DEBUG-SUB-3", Runtime.DebugItem.SubWidth, DebugRegisterMember.Sub3);
        Reg("DEBUG-CONTENTS", Runtime.DebugItem.ContentsWidth, DebugRegisterMember.Contents);
    }

    /// <summary>The compilation group's pass-1 class symbol table (OO deep-dive D1) — set by the run-unit
    /// emitter BEFORE <see cref="Bind"/> so a typed <c>USAGE OBJECT REFERENCE class-name</c> validates its
    /// declared class (§13.18.60.4) against classes defined anywhere in the group. Null only in unit-test
    /// direct construction, which then behaves as an empty group (every typed reference is unknown-class).</summary>
    public OoClassTable? OoClasses { get; set; }

    /// <summary>The unit's REPOSITORY PROPERTY specifier names (§12.3.8) — the §8.4.3.9.3 SR1 gate for
    /// object-property references (case-insensitive per §8.3.2).</summary>
    internal HashSet<string> OoRepositoryProperties { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The unit's REPOSITORY user-function specifiers (§12.3.8 — <c>FUNCTION function-prototype-name</c>
    /// WITHOUT the INTRINSIC phrase): the precondition for a user-function reference, and per §12.3.8.2 GR12
    /// (:14885) the declaration that makes the name refer to the USER-DEFINED function "and not to an intrinsic
    /// function of the same name" — so the binder's user-function dispatch precedes the intrinsic catalog.</summary>
    internal HashSet<string> UserFunctionNames { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The unit's REPOSITORY intrinsic-function specifiers by name (§12.3.8 —
    /// <c>FUNCTION intrinsic-function-name INTRINSIC</c>): the §8.4.3.2 SR2 precondition that lets the word
    /// FUNCTION be OMITTED when referencing that intrinsic (GR13). §8.3.2 case-insensitive.</summary>
    internal HashSet<string> RepositoryIntrinsics { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>True when the unit's REPOSITORY specifies <c>FUNCTION ALL INTRINSIC</c> (§12.3.8 GR14): the word
    /// FUNCTION may be omitted for EVERY §8.11 intrinsic-function-name in this scope (SR2/GR13). The SR13
    /// user-word prohibition (an intrinsic name shall not be a user-defined word here) is staged residue.</summary>
    internal bool RepositoryAllIntrinsic { get; set; }

    /// <summary>Bind a program unit's DATA DIVISION + the FILE-CONTROL paragraph: the OPTIONS paragraph, the SELECT
    /// clauses, the FILE SECTION records (which share storage with the WORKING-STORAGE roots — they emit as Program
    /// fields), and WORKING-STORAGE; then classify the shared-storage (REDEFINES) classes over the whole forest and
    /// resolve each file's FILE STATUS item.</summary>
    public void Bind(Core.ProgramUnitContext program)
    {
        BindDeclarations(program);
        BindResolve(program);
    }

    /// <summary>The declaration half of <see cref="Bind"/>: OPTIONS/SPECIAL-NAMES/FILE-CONTROL, the FILE /
    /// WORKING-STORAGE / LINKAGE sections, and the PD header formals — everything that ADDS items to the
    /// forest. Split from <see cref="BindResolve"/> so a CLASS unit can bind its METHODS' data sections
    /// (OO deep-dive D3/D6 — <c>DataBinder.Oo.cs</c>) into the same forest BEFORE the post-build passes run
    /// over it (a method item participates in USAGE/SIGN inheritance and object-reference resolution exactly
    /// like program data).</summary>
    internal void BindDeclarations(Core.ProgramUnitContext program)
    {
        Options = OptionsBinder.Bind(program, Edition);   // captured even when there is no WORKING-STORAGE

        // ARITHMETIC mode validity (§11.9.5 / §8.8.1): NATIVE, STANDARD-DECIMAL, and plain STANDARD are
        // implemented. STANDARD arithmetic (the 2002 mode; obsolete 2014, removed 2023 — Annex E.2 item 21)
        // performs operations in the standard intermediate data item, which for its reachable operands IS the
        // standard DECIMAL form, so STANDARD routes to the same CobolDec engine as STANDARD-DECIMAL
        // (NumericRenderer.StandardDecimal). Floating-point operands participate under BOTH modes through the
        // §8.8.1.5.1 implementor-defined float→SDIDI conversion (CobolDec.FromDouble — the shortest-round-trip
        // decimal identity of the IEEE value); the operations themselves are SDIDI (P10 Step 12).
        // STANDARD was DROPPED by ISO/IEC 1989:2023 (§8.8.1 names only NATIVE/STANDARD-BINARY/STANDARD-DECIMAL)
        // → the pass's arithmetic-standard-2002 dual-window row rejects it at --std 2023 (0807).
        // STANDARD-BINARY is spec-obsolete (§8.8.1.4.1 NOTE 1 — binary128 intermediates, no exact .NET carrier)
        // and documented-unsupported at EVERY edition that has it; its 2014 introduction edge is the pass's
        // arithmetic-standard-binary-2014 row.
        if (Options.Arithmetic == ArithmeticMode.StandardBinary)
            Edition.Error("COBOLNET0806", "ARITHMETIC IS STANDARD-BINARY is an obsolete feature (ISO §8.8.1.4.1 "
                + "NOTE 1 / Annex F) and is not supported; use NATIVE or STANDARD-DECIMAL");
        // arithmetic-standard-2002 (dual-window; dropped 2023): the pass owns the edition gate (Exec Step E).

        // The '85 debug facility's compile-time switch (X3.23-1985 SOURCE-COMPUTER … WITH DEBUGGING MODE; the
        // clause itself is 0902-gated ≥2002 by the version-conformance pass): its presence decides whether a USE FOR
        // DEBUGGING declarative section is COMPILED (switch present — the object-time switch is permanently off
        // here, so it never triggers) or treated as comment lines (switch absent — the '85 rule). Token-text
        // scan of the computerAttributes sink, the VisitComputerAttributes pattern (VCR Table 7 rows 7.9/7.17).
        DebuggingModeDeclared = EnvDivisions(program).Any(env => env.configurationSection()?.configurationParagraph()
            .Select(p => p.sourceComputerParagraph()?.computerAttributes())
            .Any(attrs => attrs is not null && Enumerable.Range(0, attrs.ChildCount)
                .Any(i => attrs.GetChild(i).GetText().Equals("DEBUGGING", StringComparison.OrdinalIgnoreCase)))
            ?? false);

        // REPOSITORY PROPERTY specifiers (§12.3.8 :14727-14729) — §8.4.3.9.3 SR1 makes a property-specifier
        // a PRECONDITION of every object-property reference in the unit; captured here, checked at the
        // property-reference desugar (0843). FUNCTION specifiers WITHOUT the INTRINSIC phrase declare
        // user-defined functions (§12.3.8.2 GR12 — the name then refers to the user function, never a
        // same-named intrinsic; the `FUNCTION ALL INTRINSIC` alternative carries no functionName and the
        // per-name INTRINSIC form is excluded by its phrase). CLASS/INTERFACE specifiers stay declarative
        // (names resolve through the group-wide pass-1 table).
        foreach (var re in EnvDivisions(program).SelectMany(env => env.configurationSection()?.configurationParagraph()
                     .Select(p => p.repositoryParagraph()).FirstOrDefault(r => r is not null)
                     ?.repositoryEntry() ?? []))
        {
            // REPOSITORY PROPERTY/INTERFACE/CLASS (§12.3.8, OO): the COBOL-2002 introduction gates are now
            // VersionConformancePass ParseArm.VisitRepositoryEntry (14g.5). The PROPERTY name still registers here for
            // reference resolution; INTERFACE/CLASS names are declarative-only in this loop (the pass-1 table resolves
            // them), so they no longer need a branch.
            if (re.PROPERTY() is not null && re.propertyName() is { } pn)
                OoRepositoryProperties.Add(pn.GetText());
            else if (re.FUNCTION() is not null && re.INTRINSIC() is null && re.functionName() is { } fn)
                UserFunctionNames.Add(fn.GetText());
            // FUNCTION … INTRINSIC (§12.3.8): `ALL` (GR14) or a named intrinsic — the §8.4.3.2 SR2 keyword-
            // omission enabler (M2-UDF-4). `FUNCTION ALL INTRINSIC` carries no functionName; `FUNCTION name
            // INTRINSIC` does.
            else if (re.FUNCTION() is not null && re.INTRINSIC() is not null)
            {
                if (re.ALL() is not null) RepositoryAllIntrinsic = true;
                else if (re.functionName() is { } inf) RepositoryIntrinsics.Add(inf.GetText());
            }
        }

        SwitchBindSpecialNames(program);           // SPECIAL-NAMES switch clauses → the external-switch registry (ISO §12.3.7)
        BindFileControl(program);                  // SELECT clauses → FileModels (before the FD records bind)
        BindFileSection(program, _rootNames);      // FD records → Roots + FileModel.Records + the shared-area REDEFINES
        BindReportSection(program);                // RD entries → ReportModels (ISO §13.14; DataBinder.Reports.cs)
        BindIoControl(program);                    // I-O-CONTROL: SAME RECORD AREA → cross-file shared record area (§12.4.6.4 GR2)

        // SCREEN SECTION (ISO §13.9) is an OPTIONAL facility (§4.2.7) COBOL.NET does not implement: it parses but is
        // not bound. §4.2.6 requires a compile-time WARNING naming the unsupported element (rather than the former
        // silent drop) — the COBOLNET1560 non-support band, catalogued in docs/CONFORMANCE.md §4. The program still
        // compiles; the screen behavior is simply absent.
        if (program.dataDivision()?.screenSection() is not null)
            Edition.Warning("COBOLNET1560", "the SCREEN SECTION (ISO §13.9) is an optional facility (§4.2.7) that is "
                + "not supported — it is accepted but produces no screen behavior (see docs/CONFORMANCE.md §4)");

        if (program.dataDivision()?.workingStorageSection() is { } ws)
            _workingStorageRoots.AddRange(BindEntries(ws.dataDescriptionEntry(), _rootNames));

        // LOCAL-STORAGE SECTION (ISO §13.6 — automatic data, legal in a program or function definition per
        // §13.6.3 SR1; the method leg binds in OoBindMethodData): the SAME BindEntries path as WS, so VALUE
        // clauses, CONSTANT entries (§13.10 — WS/LS-legal), REDEFINES classes, and INDEXED BY cells all bind
        // identically; only the ACTIVATION-STATE model differs (§13.6.4 GR1 automatic → §14.6.2.3.2 initial
        // state on every activation — realized at emit, see LocalStorageRoots).
        if (program.dataDivision()?.localStorageSection() is { } ls)
            _localStorageRoots.AddRange(BindEntries(ls.dataDescriptionEntry(), _rootNames, EntrySection.LocalStorage));

        // LINKAGE SECTION roots + the PROCEDURE DIVISION header's USING/RETURNING formals (ISO §13.7 / §14.2.2;
        // COBOLNET_INTERPROGRAM_DESIGN D1/D3 — bound into the same forest so every verb works unchanged).
        CallBindLinkage(program, _rootNames);
    }

    /// <summary>The C#-field-name scope at the class level, shared by FILE SECTION records, WORKING-STORAGE
    /// roots, LINKAGE roots — and, in a CLASS unit, every METHOD's data roots (an emitted field/local name is
    /// unique across the whole class, so sibling methods' same-named items can never cross-wire — the legacy
    /// trap-#6 guard at the NAME level).</summary>
    private readonly HashSet<string> _rootNames = new(StringComparer.Ordinal);

    /// <summary>Index-names contributed by TYPE-expanded clones (data-model D17 inc 4): a TYPEDEF whose OCCURS carries
    /// an INDEXED BY phrase can be referenced at most ONCE — a second reference clones the same global index-name and
    /// the two tables' indexes would collide on one C# field. A repeat here → COBOLNET1531 (staged loud). Cleared at
    /// the top of <see cref="ExpandTypes"/> (per program unit).</summary>
    private readonly HashSet<string> _typedIndexNames = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The resolution half of <see cref="Bind"/> — the post-build passes over the COMPLETE forest, driven by
    /// the DECLARED <see cref="BindPipeline"/> (rearchitecture PHASE 05 Step 3 / PHASE 06 Step 3; DESIGN-data-model
    /// §2.5). The order is IDENTICAL to the former comment-ordered call sequence — now EXPLICIT and asserted at
    /// startup by <see cref="BindPipeline.ValidateFullChainOnce"/> (each pass declares Requires/Produces phases),
    /// which structurally kills the implicit-pass-ordering smell with ZERO reorder / ZERO behavior change.
    /// <see cref="BindPipeline.Build"/> IS the per-unit resolve prefix; the whole-group middle-end tail (procedure
    /// binding → UsageCollection → StorageForm) is <see cref="BindPipeline.GroupTail"/>, run by
    /// <see cref="BinderDriver.Bind"/> once per compilation, and the two are validated as ONE chain.
    /// <para>Post-build the forest is complete: fix up USAGE INDEX entries (children weren't known at entry bind);
    /// apply group-level SIGN clauses (must precede the REDEFINES classification — a SEPARATE sign adds a character
    /// position to the item's image width, which feeds the class-max width); then resolve REDEFINES/RENAMES targets,
    /// group overlaid items into shared-storage classes and assign each a tier (ISO §13.18.44/§13.18.45;
    /// COBOLNET_DESIGN §4). This now covers the FILE SECTION records too (their multi-01 area sharing is a synthesized
    /// REDEFINES). Finally resolve each file's FILE STATUS data item.</para></summary>
    internal void BindResolve(Core.ProgramUnitContext program)
    {
        BindPipeline.ValidateFullChainOnce();
        foreach (var pass in BindPipeline.Build(program))
        {
            Require(pass.Requires, pass.Name);   // watermark gate: the declared prerequisite actually RAN (P6 Step 6)
            pass.Run(this);
            MarkProduced(pass.Produces);
        }
    }

    // ── The completion-phase WATERMARK gate (P6 Step 6). The construction-time DAG assert
    //    (BindPipeline.ValidateFullChainOnce) guards the declared LIST order; the watermark guards what actually
    //    RAN on THIS binder — "read a fact before its producing pass" becomes a loud, located error rather than
    //    a silent miscompile (a null Tier / missing CapacityRegister / unset StorageForm). ──

    /// <summary>The highest <see cref="PassPhase"/> whose producing pass has COMPLETED on this binder's forest.
    /// Advanced by <see cref="BindResolve"/> (the per-unit prefix) and by <c>BinderDriver.Bind</c> (the group
    /// tail, which marks every binder of the group after each group pass).</summary>
    internal PassPhase Watermark { get; private set; }

    /// <summary>Advance the watermark to <paramref name="produced"/> (never regresses).</summary>
    internal void MarkProduced(PassPhase produced)
    {
        if (produced > Watermark) Watermark = produced;
    }

    /// <summary>The ALWAYS-ON completion gate: assert <paramref name="required"/> has been produced on this
    /// binder before <paramref name="fact"/> is read/run — a mis-ordered pass in a production compiler is a
    /// silent miscompile, exactly when the loud throw matters most. Cost is a per-pass-entry integer compare
    /// (immaterial). Was <c>[Conditional("DEBUG")]</c> at first landing — CI's RELEASE test leg stripped the
    /// call sites and failed the throw-expecting WatermarkTests ("no exception was thrown"), proving the
    /// Debug-only design created a whole Release-divergence class for zero real saving (DEVLOG 774).</summary>
    internal void Require(PassPhase required, string fact)
    {
        if (Watermark < required)
            throw new InvalidOperationException(
                $"BindPipeline watermark violation: '{fact}' requires phase {required}, but this binder has only "
                + $"reached {Watermark} — a consumer ran before the producing pass (P6 Step 6 gate).");
    }

    /// <summary>The LAST resolve pass (<see cref="BindPipeline"/>): every FILE record area is filled WITHOUT conversion
    /// by READ/RETURN (ISO §9.1.2 — the record area is one character image), so its numeric-DISPLAY leaves store their
    /// images exactly like a whole-referenced group's — even when the PROCEDURE DIVISION never names the record as a
    /// whole (ST103A reads then tests only a child). <c>StorageFormPass</c> consumes <see cref="WholeGroupReferenced"/>
    /// after binding.</summary>
    internal void MarkFileRecordImageLeaves()
    {
        foreach (var f in Files)
            foreach (var rec in f.Records)
                if (rec.IsGroup)
                {
                    WholeGroupReferenced.Add(rec);
                    // Flag the leaves NOW (not at the group-tail whole-group pass): the bind-time
                    // NumericImagePlace wrap decision consults the early image facts (a ref-mod/RENAMES base
                    // under a record area must route through its character image at resolve time; ST102A's
                    // all-DISPLAY S-RECORD must not read as a Tier-C island at bind time).
                    MarkImageLeaves(rec);
                }

        void MarkImageLeaves(DataItem item)
        {
            foreach (var child in item.Children)
            {
                if (child.IsGroup) MarkImageLeaves(child);
                else if (child.Pic is { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display })
                    MarkImageForced(child);      // the collected image fact (same rule as the whole-group union, §14.9 MOVE GR4)
            }
        }
    }

    /// <summary>Route a RECURSIVE unit's WORKING-STORAGE onto the ONE static-field channel (the mechanism the
    /// method-WS D3 pattern established — <see cref="StaticRootFields"/> / <see cref="StaticIndexCells"/>,
    /// consumed by <c>RecordStructEmitter</c>): a no-op unless <see cref="UnitStaticWs"/>. ISO basis on the
    /// flag's doc — §13.5.4 GR1 (a non-initial program's / a function's WS is STATIC data) + §14.6.2.3.3 (static
    /// data is in LAST-USED state: one copy shared across all activations), while the per-activation instance
    /// keeps carrying the §13.6.4 GR1 automatic data and the formals. Runs AFTER the pointer pass (the last
    /// tier-overwrite seam) so Tier-B backings, EXTERNAL re-basing, and BASED/ADDRESS-OF forcing are settled:
    /// <list type="bullet">
    /// <item>a Tier-B StringCanonical WS root → its ONE string backing goes static (the OO
    ///   <see cref="OoRouteMethodRedefinesBackings"/> twin), plus the root's own name (harmless when the
    ///   physical is the backing — membership is tested per emitted field name);</item>
    /// <item>an EXTERNAL WS record stays OFF the channel — it is run-unit storage on the ExternalStore
    ///   (§8.6.7; §14.6.2.3.3 external data is ALWAYS last-used), reached through a ref-bridge, never a
    ///   per-class static;</item>
    /// <item>a WS table's INDEXED BY cell goes static with its table (the M2-OO-1h step-4 mirror — a
    ///   last-used table with per-activation indexes would silently lose SET positions);</item>
    /// <item>a BASED WS root / an ADDRESS-OF-taken WS record stages LOUD (0899) — their cell/bridge storage
    ///   is per-instance (<see cref="PtrAddressableBackings"/>/<see cref="PtrBasedBridges"/> emit instance
    ///   members), and §14.6.2.3.2 #5 makes a based item's address part of the unit's static state; a silent
    ///   instance-resident cell would re-initialize per activation (§1.4 — never silent-wrong).</item>
    /// </list></summary>
    internal void RouteStaticUnitStorage()
    {
        if (!UnitStaticWs) return;
        foreach (var root in _workingStorageRoots)
        {
            if (root.Class is { Tier: RedefinesTier.StringCanonical } cls
                && CallExternalBackings.Any(b => b.BackingCsName == cls.BackingCsName))
                continue;   // an EXTERNAL-backed class (canonical AND views) — run-unit cell storage (§14.6.2.3.3), not a class static
            if (root.IsBased || root.Class is { } c
                && (c.BasedPointerField is not null || PtrAddressableCellOf.ContainsKey(c)))
            {
                Edition.Error(DiagnosticCatalog.RecursiveWsPointerBacked,
                    $"'{root.CobolName ?? "FILLER"}': BASED data or an ADDRESS-OF-taken record in the "
                    + "WORKING-STORAGE of a RECURSIVE program or function is recognized but its static "
                    + "cell/bridge storage is not yet implemented (ISO §13.5.4 GR1 / §14.6.2.3.2 #5)");
                continue;
            }
            if (root.Class is { Tier: RedefinesTier.StringCanonical } c2 && ReferenceEquals(c2.Canonical, root))
                _staticRootFields.Add(c2.BackingCsName);   // Tier-B: the ONE string backing IS the storage
            _staticRootFields.Add(root.CsName);
            foreach (var idx in IndexNamesUnder(root))
                if (_indexFields.TryGetValue(idx, out var cell))
                    _staticIndexCells.Add(cell);
        }
    }

    /// <summary>Bind a run of data-description entries (a WORKING-STORAGE section or one FD's records) into the
    /// storage forest: a level-number stack attaches each entry under the nearest open item of a lower level; level-88
    /// becomes a condition-name and level-66 a RENAMES alias. Returns the new top-level (01/77) items, in order — the
    /// caller (an FD) needs them to model the shared record area. <paramref name="section"/> names the DATA DIVISION
    /// section the run belongs to — consumed by the section-scoped placement rules (CONSTANT RECORD §13.18.15.3 SR1).</summary>
    private List<DataItem> BindEntries(IEnumerable<Core.DataDescriptionEntryContext> entries, HashSet<string> rootNames,
        EntrySection section = EntrySection.WorkingStorage)
    {
        var newRoots = new List<DataItem>();
        var stack = new Stack<DataItem>();
        bool rootIsTemplate = false;   // true while the current level-1 subtree is a TYPEDEF template (D17)
        foreach (var entry in entries)
        {
            // A CONSTANT entry (ISO §13.10; the constantEntryBody alternative) is a COMPILE-TIME substitution,
            // not storage: fold it into the constant table and produce NO DataItem. Checked BEFORE the 66/88
            // early-outs so a mis-leveled constant entry gets its §13.10.2 level diagnostic, never a RENAMES/
            // condition-name misbind. (P10 Step 15; DataBinder.Constants.cs.)
            if (entry.dataDescriptionBody().constantEntryBody() is { } constBody)
            {
                BindConstantEntry(entry, constBody);
                continue;
            }
            int.TryParse(entry.levelNumber().GetText(), out int lvl);
            // A level-88 entry is a condition-name on the immediately superior item — not a node in the tree.
            if (lvl == 88)
            {
                // A TYPEDEF template's condition-names are NOT globally referenceable (§13.18.58.4 GR1) — bind them
                // onto the item (so CloneItem can carry them into each TYPE reference) but keep them off the global
                // by-name index; a non-template 88 registers globally as usual (D17 inc 3).
                if (stack.Count > 0)
                {
                    // §13.18.57.3 SR2 (review DEVLOG 664 fix #2): a TYPE-clause entry shall not be followed
                    // immediately by a level-88 entry.
                    if (stack.Peek().TypeRefName is not null)
                        Edition.Error("COBOLNET1537", $"condition-name '{entry.dataName()?.GetText() ?? "?"}': a data "
                            + "description entry that specifies a TYPE clause shall not be followed immediately by a "
                            + "level-88 entry (ISO §13.18.57.3 SR2)");
                    BindCondition(entry, stack.Peek(), registerGlobal: !rootIsTemplate);
                }
                continue;
            }
            // A level-66 RENAMES entry is a re-grouping alias on the owning record — not a node in the storage tree.
            if (lvl == 66)
            {
                // A RENAMES INSIDE a TYPEDEF template is part of the type (§13.18.58.4 GR1) but CloneItem does not
                // carry Renames66 into a TYPE reference — so it would be silently dropped. Staged loud (D17 inc 4).
                if (rootIsTemplate)
                    Edition.Error(DiagnosticCatalog.TypedefRenamesStaged, "a level-66 RENAMES inside a TYPEDEF "
                        + "(part of the type per ISO §13.18.58.4 GR1) is recognized but not yet cloned into TYPE "
                        + "references (data-model D17 residue)");
                BindRenames(entry);
                continue;
            }

            if (BindEntry(entry) is not { } item) continue;
            item.Uid = _uidCounter++;

            // Level 77 is an INDEPENDENT elementary item (ISO §13.18.38): always top-level, like 01, regardless of its
            // numeric value. Treat it as level 1 for the nesting pop so it attaches as a ROOT — never nested under an
            // open subordinate item just because 77 > that item's level (which would mis-qualify every later reference).
            int nestLevel = item.Level == 77 ? 1 : item.Level;
            while (stack.Count > 0 && stack.Peek().Level >= nestLevel)
                stack.Pop();

            if (stack.Count == 0)
            {
                // A TYPEDEF entry is a type DECLARATION (ISO §13.18.58; D17): a named level-01 template that
                // allocates NO storage — registered in TypeDecls, kept OFF Roots (and, below, off ByName).
                if (item.IsTypedef)
                {
                    rootIsTemplate = true;
                    RegisterTypeDecl(item);
                }
                else
                {
                    rootIsTemplate = false;
                    // A 01/77 emits as a Program-level static field — its C# name must be unique across every root
                    // (FILE SECTION records and WORKING-STORAGE alike), so record it in the shared scope.
                    item.CsName = Unique(item.CsName, rootNames);
                    rootNames.Add(item.CsName);
                    _roots.Add(item);
                    newRoots.Add(item);
                    _lastRoot = item;
                }
            }
            else
            {
                if (item.IsTypedef)   // §13.18.58 SR — a TYPEDEF is a level-01 entry only, never a subordinate.
                    Edition.Error("COBOLNET1529", $"TYPEDEF on '{item.CobolName ?? "FILLER"}': the TYPEDEF clause "
                        + "shall be specified only in a level-01 record-description entry (ISO §13.18.58)");
                var parent = stack.Peek();
                // §13.18.57.3 SR2 (review DEVLOG 664 fix #2): a TYPE-clause entry shall not be followed immediately by
                // a subordinate entry — the entry IS the whole type (§13.18.57.4 GR1). Without this the explicit
                // subordinate merges ahead of the cloned members (a silent-wrong record image for a group type; a
                // member-on-a-string CS1061 leak for an elementary type).
                if (parent.TypeRefName is not null)
                    Edition.Error("COBOLNET1537", $"'{item.CobolName ?? "FILLER"}': a data description entry that "
                        + "specifies a TYPE clause shall not be followed immediately by a subordinate entry "
                        + "(ISO §13.18.57.3 SR2)");
                // A member name need only be unique within its containing struct (the parent's children).
                item.CsName = Unique(item.CsName, parent.Children.Select(c => c.CsName));
                item.Parent = parent;
                parent.Children.Add(item);
            }
            // CONSTANT RECORD placement + subtree conflicts (P10 Step 15). §13.18.15.3 SR1: the clause may be
            // specified only in the local-storage or working-storage sections. §13.16.3 SR13: BLANK WHEN ZERO /
            // SYNCHRONIZED (and the level-checked BASED / ANY LENGTH / TYPEDEF) shall not appear in any entry
            // subordinate to a CONSTANT RECORD entry. (The same-entry SR13/SR3/SR6 conflicts are checked in
            // BindEntry, where the flags are local — the IsBased discipline.)
            if (item.IsConstantRecord && section is not (EntrySection.WorkingStorage or EntrySection.LocalStorage))
                Edition.Error(DiagnosticCatalog.ConstantRecordRule, $"'{item.CobolName ?? "FILLER"}': the "
                    + "CONSTANT RECORD clause may be specified only in the local-storage or working-storage "
                    + "sections (ISO §13.18.15.3 SR1)");
            else if (!item.IsConstantRecord && IsConstantRecordItem(item)
                && (item.BlankWhenZero || item.Synchronized || item.IsBased || item.IsAnyLength || item.IsTypedef))
                Edition.Error(DiagnosticCatalog.ConstantRecordRule, $"'{item.CobolName ?? "FILLER"}': the "
                    + "ANY LENGTH, BASED, BLANK WHEN ZERO, SYNCHRONIZED, and TYPEDEF clauses shall not be "
                    + "specified in any entry subordinate to a CONSTANT RECORD entry (ISO §13.16.3 SR13)");
            stack.Push(item);
            // A TYPEDEF template's items (root + subordinates) are NOT globally referenceable (ISO §13.18.58.4 GR1) —
            // keep them off ByName; the clones ExpandTypes produces ARE registered.
            if (!rootIsTemplate) RegisterName(item);
        }
        return newRoots;
    }

    /// <summary>Register a TYPEDEF type declaration (ISO §13.18.58; data-model D17): a named level-01 template.
    /// SR checks (→ COBOLNET1529): the entry shall be level-01 and named (not FILLER), and the type-name unique.</summary>
    private void RegisterTypeDecl(DataItem item)
    {
        if (item.Level != 1)
            Edition.Error("COBOLNET1529", $"TYPEDEF '{item.CobolName ?? item.CsName}': a type declaration shall be a "
                + "level-01 record-description entry (ISO §13.18.58 / §13.16)");
        if (item.CobolName is null)
        {
            Edition.Error("COBOLNET1529", "a TYPEDEF entry shall be named (not FILLER) — it defines a type-name "
                + "(ISO §13.18.58.4 GR2)");
            return;
        }
        if (item.RedefinesTargetName is not null)
            Edition.Error("COBOLNET1529", $"TYPEDEF '{item.CobolName}': the TYPEDEF and REDEFINES clauses are "
                + "mutually exclusive (ISO §13.16)");
        if (!TypeDecls.TryAdd(item.CobolName, item))
            Edition.Error("COBOLNET1529", $"duplicate type-name '{item.CobolName}' — a type-name shall be unique "
                + "(ISO §13.18.58)");
    }

    // ── FILE-CONTROL + FILE SECTION (ISO §12.4.5 / §13.18; COBOLNET_DESIGN §8) ─────────────────────────────────

    /// <summary>Every ENVIRONMENT DIVISION of the unit, OUTERMOST (class-level) FIRST. A real program unit
    /// carries at most ONE; an OO synthetic reparent unit (OoDriver's CallReparent pattern) carries
    /// [half's-own-env, class-env] nearer-first, so iterating REVERSED binds the class scope first and the
    /// nearer half's registrations override per-clause — the ISO §10.6 scoping (the class definition's
    /// environment division applies to the factory and object definitions). This is the DEVLOG-738 latent-bug
    /// fix: the former SINGULAR environmentDivision() read silently DROPPED the class-level env whenever the
    /// half carried its own (the shadow-0× case).</summary>
    internal static IReadOnlyList<Core.EnvironmentDivisionContext> EnvDivisions(Core.ProgramUnitContext program) =>
        [.. program.GetRuleContexts<Core.EnvironmentDivisionContext>().Reverse()];

    /// <summary>Bind the FILE-CONTROL paragraph's SELECT clauses into <see cref="FileModel"/>s (assign target,
    /// organization, access mode, OPTIONAL, FILE STATUS). The FD records attach in <see cref="BindFileSection"/>.</summary>
    private void BindFileControl(Core.ProgramUnitContext program)
    {
        foreach (var env in EnvDivisions(program)) BindFileControl(env);
    }

    private void BindFileControl(Core.EnvironmentDivisionContext env)
    {
        var fc = env.inputOutputSection()?.fileControlParagraph();
        if (fc is null) return;
        foreach (var grp in fc.fileControlClauseGroup())
        {
            if (grp.fileName()?.GetText() is not { } name) continue;
            var file = new FileModel { CobolName = name, AssignTarget = name, Optional = grp.OPTIONAL() is not null };
            foreach (var clauses in grp.fileControlClauses())
            {
                if (clauses.assignClause()?.assignTarget() is { } tgt)
                    file.AssignTarget = tgt.STRINGLIT() is { } s ? CobolLiteral.Decode(s.GetText()) : tgt.GetText();
                else if (clauses.organizationClause() is { } org) file.Organization = MapOrganization(org);
                else if (clauses.accessModeClause() is { } acc) file.AccessMode = MapAccessMode(acc);
                // The BASE word only: an OF/IN-qualified status name (`SQ-FS4-STATUS OF STATUS-GROUP`, SQ133A)
                // would otherwise glue its qualifier into the lookup key (the RENAMES capture pattern).
                else if (clauses.fileStatusClause()?.dataReference() is { } fs)
                    file.FileStatusName = fs.cobolWord()?.GetText() ?? fs.GetText();
                else if (clauses.recordKeyClause()?.dataReference() is { } rk)                                       // ISO §12.4.5.12
                    (file.RecordKeyName, file.RecordKeyQualifiers) = KeyReference(rk);
                else if (clauses.alternateKeyClause() is { } ak)                                                     // ISO §12.4.5.6
                {
                    var (an, aq) = KeyReference(ak.dataReference());
                    // §12.4.5.6.4 GR6 — the SUPPRESS WHEN key suppression value (decoded literal; null when absent).
                    string? suppress = ak.alternateKeySuppressWhen()?.literal() is { } sl ? CobolLiteral.Decode(sl.GetText()) : null;
                    file.AlternateKeyNames.Add((an, aq, ak.DUPLICATES() is not null, suppress));
                }
                else if (clauses.relativeKeyClause()?.dataReference() is { } rlk)
                    file.RelativeKeyName = KeyReference(rlk).Base;   // ISO §12.4.5.13 SR3 — outside the record
                else if (clauses.sharingClause() is { } sh)   // §12.4.5.15 — edition gate: VersionConformancePass ParseArm.VisitSharingClause (14g.4, recognition — fires on the clause's presence, drop-proof on a SELECT error)
                    file.Sharing = MapSharing(sh.sharingMode());
                else if (clauses.lockModeClause() is { } lm)   // §12.4.5.9 — edition gate: VersionConformancePass ParseArm.VisitLockModeClause (14g.4)
                    file.LockMode = MapLockMode(lm);
                else if (clauses.fileCollatingSequenceClause() is { } col)   // §12.4.5.7 — introduction-gated post-bind; resolved in ResolveFileCollating
                    CaptureFileCollating(file, col);
            }
            // §12.4.5.9 SR2: WITH LOCK ON MULTIPLE RECORDS shall not be specified for a sequentially-accessed
            // or sequential-organization file.
            if (file.LockMode is { Multiple: true }
                && (file.AccessMode is FileAccessMode.Sequential
                    || file.Organization is FileOrganization.Sequential or FileOrganization.LineSequential))
                Edition.Error("COBOLNET1512", $"file '{name}': LOCK MODE … WITH LOCK ON MULTIPLE RECORDS may "
                    + "not be specified for a sequential-access or sequential-organization file (ISO §12.4.5.9 SR2)");
            // §14.9.27 SR8: a file described SHARING WITH ALL OTHER (whether via the SELECT clause here or the
            // OPEN phrase, which BindOpen also checks) shall be described with a LOCK MODE clause.
            if (file.Sharing == SharingMode.AllOther && file.LockMode is null)
                Edition.Error("COBOLNET1512", $"file '{name}': SHARING WITH ALL OTHER requires the file to have a "
                    + "LOCK MODE clause (ISO §14.9.27 SR8)");
            _files.Add(file);
            FilesByName[name] = file;
        }
    }

    /// <summary>Capture one file-control COLLATING SEQUENCE clause as written (ISO §12.4.5.7) — resolved to per-key
    /// weight tables post-build in <see cref="ResolveFileCollating"/> (the keys are not yet bound here). Format 2 is
    /// OF-led; Format 1 is the FOR-split or the IS alphabet-name-1 [alphabet-name-2] form.</summary>
    private static void CaptureFileCollating(FileModel file, Core.FileCollatingSequenceClauseContext ctx)
    {
        if (ctx.OF() is not null)   // Format 2 (key-level): OF {key}… IS alphabet-name-3
        {
            var words = ctx.cobolWord();
            var keyNames = words.Take(words.Length - 1).Select(w => w.GetText()).ToList();
            file.KeyLevelCollating.Add((keyNames, words[^1].GetText()));
            return;
        }
        file.FileLevelCollatingCount++;   // §12.4.5.7.3 SR3 — at most one file-level clause
        string? alnum = null, nat = null;
        if (ctx.collatingForPhrase() is { Length: > 0 } fors)
            foreach (var f in fors)
            {
                if (f.NATIONAL() is not null) nat = f.cobolWord().GetText();
                else alnum = f.cobolWord().GetText();
            }
        else
        {
            var words = ctx.cobolWord();
            alnum = words.Length > 0 ? words[0].GetText() : null;
            nat = words.Length > 1 ? words[1].GetText() : null;
        }
        file.FileLevelCollating = (alnum, nat);
    }

    /// <summary>Resolve each INDEXED key's collating-weight table from the file's §12.4.5.7 COLLATING SEQUENCE
    /// clauses (post-build — the keys are bound by now). Per §12.4.5.7.4 the sequence for a key is, in order: (GR6)
    /// a Format-2 clause naming it; else (GR2/GR3) the Format-1 default for the key's class; else (GR4/GR5) native
    /// (null weights = ordinal). SR3 (single file-level clause), SR4/SR5 (Format-2 names shall be declared keys),
    /// and SR1/SR2/SR7 (alphabet class) are enforced. A NATIONAL alphabet on a key is recognized-but-not-yet-
    /// implemented (national-key collating is a documented P14 GAP — never silently applied).</summary>
    private void ResolveFileCollating(FileModel file)
    {
        if (file.FileLevelCollatingCount > 1)
            Edition.Error(DiagnosticCatalog.FileCollatingKey, $"file '{file.CobolName}': at most one file-level "
                + "COLLATING SEQUENCE clause may be specified in one file control entry (ISO §12.4.5.7.3 SR3)");
        if (file.FileLevelCollating is null && file.KeyLevelCollating.Count == 0) return;   // no clause → native

        if (file.Organization != FileOrganization.Indexed)
        {
            Edition.Error(DiagnosticCatalog.FileCollatingKey, $"file '{file.CobolName}': a file-control COLLATING "
                + "SEQUENCE clause applies only to an INDEXED file (ISO §12.4.5.7.1)");
            return;
        }

        // SR4/SR5: every Format-2 name shall be a declared RECORD KEY or ALTERNATE RECORD KEY of this file.
        var keyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (file.RecordKeyName is { } pk) keyNames.Add(pk);
        foreach (var (n, _, _, _) in file.AlternateKeyNames) keyNames.Add(n);
        var seenInClause = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (names, _) in file.KeyLevelCollating)
            foreach (var n in names)
            {
                if (!keyNames.Contains(n))
                    Edition.Error(DiagnosticCatalog.FileCollatingKey, $"file '{file.CobolName}': COLLATING SEQUENCE "
                        + $"OF '{n}' — '{n}' is not a RECORD KEY or ALTERNATE RECORD KEY of this file "
                        + "(ISO §12.4.5.7.3 SR4/SR5)");
                else if (!seenInClause.Add(n))   // SR8 — a key named in more than one Format-2 clause
                    Edition.Error(DiagnosticCatalog.FileCollatingKey, $"file '{file.CobolName}': key '{n}' is named "
                        + "in more than one COLLATING SEQUENCE clause (ISO §12.4.5.7.3 SR8)");
            }

        file.PrimeKeyWeights = ResolveKeyCollating(file, file.RecordKeyName);
        for (int i = 0; i < file.AlternateKeys.Count; i++)
            file.AlternateKeyWeights.Add(ResolveKeyCollating(file, AltName(file, i)));
    }

    /// <summary>The declared name of the i-th resolved alternate key (index-aligned when all names resolve — the
    /// normal case; a name that failed to resolve has already errored).</summary>
    private static string? AltName(FileModel file, int i) =>
        i < file.AlternateKeyNames.Count ? file.AlternateKeyNames[i].Name : null;

    /// <summary>Resolve one key's collating weights (§12.4.5.7.4): a Format-2 alphabet naming the key wins (GR6),
    /// else the file-level alphanumeric default (GR2), else native ordinal (null). An alphanumeric alphabet
    /// resolves to its weight table; a NATIONAL alphabet is the recognized-not-implemented P14 GAP; an undeclared
    /// name errors.</summary>
    private ushort[]? ResolveKeyCollating(FileModel file, string? keyName)
    {
        string? alphabet = null;
        if (keyName is not null)
            foreach (var (names, a) in file.KeyLevelCollating)
                if (names.Any(n => n.Equals(keyName, StringComparison.OrdinalIgnoreCase))) { alphabet = a; break; }
        alphabet ??= file.FileLevelCollating?.Alnum;   // GR2 file-level alphanumeric default
        if (alphabet is null) return null;             // GR4/GR5 — native ordinal

        if (Alphabets.TryGetValue(alphabet, out var table) && table is not null)
            return table.Positions;   // SR1 — alphanumeric collating
        if (NationalAlphabets.ContainsKey(alphabet))
        {
            Edition.Error(DiagnosticCatalog.FileCollatingNationalUnsupported, $"file '{file.CobolName}': COLLATING "
                + $"SEQUENCE '{alphabet}' names a NATIONAL alphabet — national-key collating for indexed files is "
                + "recognized but not yet implemented; the key orders natively (ISO §12.4.5.7).");
            return null;
        }
        Edition.Error(DiagnosticCatalog.FileCollatingAlphabet, $"file '{file.CobolName}': COLLATING SEQUENCE "
            + $"'{alphabet}' does not name an alphabet declared in SPECIAL-NAMES (ISO §12.4.5.7.3 SR1)");
        return null;
    }

    /// <summary>Map the SHARING clause / phrase mode (ISO §12.4.5.15).</summary>
    private static SharingMode MapSharing(Core.SharingModeContext m) =>
        m.READ() is not null ? SharingMode.ReadOnly
        : m.NO() is not null ? SharingMode.NoOther
        : SharingMode.AllOther;   // ALL OTHER

    /// <summary>Map the LOCK MODE clause (ISO §12.4.5.9): MANUAL / AUTOMATIC + single/MULTIPLE granularity.</summary>
    private static LockModeInfo MapLockMode(Core.LockModeClauseContext lm) =>
        new(lm.AUTOMATIC() is not null ? LockKind.Automatic : LockKind.Manual,
            lm.lockOnPhrase()?.MULTIPLE() is not null);

    /// <summary>A FILE-CONTROL key reference: the base word plus its IN/OF qualifier words in written order
    /// (innermost first, ISO §8.4.2.2). A raw <c>GetText()</c> would glue qualifiers into the lookup key
    /// (<c>IX-FD3-KEYINIX-FD3-RECKEY-AREA</c>) and the name could never resolve — the FILE STATUS / RENAMES
    /// capture pattern, applied to keys.</summary>
    private static (string Base, IReadOnlyList<string> Quals) KeyReference(Core.DataReferenceContext dref)
    {
        string baseWord = dref.cobolWord()?.GetText() ?? dref.GetText();
        var quals = new List<string>();
        foreach (var s in dref.dataReferenceSuffix())
            if (s.qualification()?.cobolWord() is { } q)
                quals.Add(q.GetText());
        return (baseWord, quals);
    }

    /// <summary>Bind the FILE SECTION's FD records into the storage forest (they emit as Program fields, like
    /// WORKING-STORAGE), attach them to their <see cref="FileModel"/>, and model the shared record area: multiple
    /// <c>01</c>s under one FD occupy ONE area (ISO §9.1.2), so each secondary record is synthesized as a REDEFINES of
    /// the first — the existing tier machinery then makes them alias one backing (the singular-pattern rule).</summary>
    private void BindFileSection(Core.ProgramUnitContext program, HashSet<string> rootNames)
    {
        var fs = program.dataDivision()?.fileSection();
        if (fs is null) return;
        foreach (var fd in fs.fileDescriptionEntry())
        {
            if (fd.fileName()?.GetText() is not { } name) continue;
            var records = BindEntries(fd.dataDescriptionEntry(), rootNames, EntrySection.File);
            if (!FilesByName.TryGetValue(name, out var file))
            {
                // An FD with no matching SELECT — keep a model so its records still resolve (it is never opened).
                file = new FileModel { CobolName = name };
                _files.Add(file);
                FilesByName[name] = file;
            }
            file.HasFd = true;
            file.Records.AddRange(records);
            for (int i = 1; i < records.Count; i++)
                records[i].RedefinesTarget ??= records[0];   // secondary record shares the first's storage area
            foreach (var clause in fd.fileDescriptionClauses()?.fileDescriptionClause() ?? [])
                if (clause.recordClause() is { } rc)
                    BindRecordClause(rc, file);   // RECORD VARYING / m TO n → FileModel.Varying (ISO §13.18.43)
                else if (clause.linageClause() is { } lc)
                    BindLinageClause(lc, file);   // LINAGE logical-page model → FileModel.Linage (ISO §13.18.34)
                else if (clause.reportClause() is { } rep)
                    // REPORT(S) clause (ISO §13.18.46): the FD hosts these reports — a report FILE (§9.1.22,
                    // legally record-less). Names resolve to ReportModels post-build (ResolveReports).
                    foreach (var rn in rep.reportName())
                        file.ReportNames.Add(rn.GetText());
                else if (clause.fileGlobalExternalClause() is { } ge)
                {
                    // FD IS EXTERNAL / IS GLOBAL (ISO §13.18.22 / §13.18.30): EXTERNAL ⇒ one run-unit file
                    // connector + external record data (GR4a/GR4b), externalized as the FD name (GR5); GLOBAL ⇒
                    // the file-name and record-names are global names, inherited by contained programs.
                    if (ge.EXTERNAL() is not null)
                    {
                        file.IsExternal = true;
                        file.ExternalName = name.ToUpperInvariant();
                    }
                    if (ge.GLOBAL() is not null) file.IsGlobal = true;
                }
        }

        // SD entries (ISO §13.4.6): a sort-merge file's records bind through the SAME entry path as FD records —
        // they emit as Program fields and multi-01 records share ONE area (synthesized REDEFINES, ISO §9.1.2).
        // The SD format admits only the record clause (§13.4.6); DATA RECORDS is an obsolete '85 element DELETED
        // by ISO/IEC 1989:2002 — accepted-inert at 85 (every NIST SD writes it), rejected ≥2002.
        foreach (var sd in fs.sortMergeDescriptionEntry())
        {
            if (sd.fileName()?.GetText() is not { } sdName) continue;
            var sdRecords = BindEntries(sd.dataDescriptionEntry(), rootNames, EntrySection.File);
            if (!FilesByName.TryGetValue(sdName, out var sdFile))
            {
                sdFile = new FileModel { CobolName = sdName };
                _files.Add(sdFile);
                FilesByName[sdName] = sdFile;
            }
            sdFile.HasFd = true;
            sdFile.IsSortMerge = true;   // referenced only by SORT/MERGE/RELEASE/RETURN (§13.4.6 SR3/SR4)
            sdFile.Records.AddRange(sdRecords);
            for (int i = 1; i < sdRecords.Count; i++)
                sdRecords[i].RedefinesTarget ??= sdRecords[0];
            foreach (var clause in sd.sortMergeDescriptionClauses()?.sortMergeDescriptionClause() ?? [])
            {
                if (clause.recordClause() is { } rc)
                    BindRecordClause(rc, sdFile);
                // (The SD DATA RECORDS 0873 gate MIGRATED to the version-conformance pass parse-arm (VisitDataRecordsClause) — one
                // enforcement site covering FD AND SD via the shared grammar rule; P2.6 / Table-7 row 7.1.)
            }
        }
    }

    /// <summary>Bind a RECORD clause's variable-length forms into <see cref="FileModel.Varying"/> (ISO §13.18.43:
    /// <c>RECORD IS VARYING [FROM m] [TO n] [DEPENDING ON d]</c> and <c>RECORD CONTAINS m TO n</c> describe
    /// variable-length records; the fixed Format-1 <c>RECORD CONTAINS n</c> leaves it null). Shared by the FD and
    /// SD loops — ONE binding for the clause. The DEPENDING name keeps only the base word (the FILE STATUS
    /// capture pattern) and resolves post-build in <see cref="ResolveFiles"/>.</summary>
    private static void BindRecordClause(Core.RecordClauseContext rc, FileModel file)
    {
        if (rc.VARYING() is null && rc.TO() is null)
        {
            // The fixed Format-1 RECORD CONTAINS n (ISO §13.18.43): captured for the report-file line width
            // (COBOLNET_REPORT_WRITER_DESIGN §4); a record-bearing FD's width still comes from its records.
            if (rc.integerLiteral() is { Length: > 0 } fixedLits && int.TryParse(fixedLits[0].GetText(), out int n0))
                file.RecordContains = n0;
            return;
        }
        var lits = rc.integerLiteral();
        int? lo = lits.Length > 0 ? int.Parse(lits[0].GetText()) : null;
        int? hi = lits.Length > 1 ? int.Parse(lits[1].GetText()) : null;
        if (rc.TO() is not null && lits.Length == 1) { hi = lo; lo = null; }
        string? dep = rc.dataReference() is { } d ? d.cobolWord()?.GetText() ?? d.GetText() : null;
        file.Varying = new VaryingRecordInfo(lo, hi, dep);
    }

    /// <summary>Bind a LINAGE clause into <see cref="FileModel.Linage"/> (ISO §13.18.34: <c>LINAGE IS
    /// {data-name-1 | integer-1} LINES [WITH FOOTING AT {data-name-2 | integer-2}] [LINES AT TOP
    /// {data-name-3 | integer-3}] [LINES AT BOTTOM {data-name-4 | integer-4}]</c>). Each operand is a fixed
    /// literal (GR6a) or a data-name (GR6b — read at the evaluation points); a data-name keeps only the base
    /// word (the FILE STATUS capture pattern) and resolves post-build in <see cref="ResolveFiles"/>. Absent
    /// FOOTING/TOP/BOTTOM phrases stay null (GR1 — margins zero; no footing ⇒ no end-of-page condition
    /// independent of page overflow).</summary>
    private static void BindLinageClause(Core.LinageClauseContext lc, FileModel file)
    {
        static LinageOperand Operand(Core.DataReferenceContext? d, Core.IntegerLiteralContext? i) =>
            i is not null ? new LinageOperand(int.Parse(i.GetText()), null)
            : new LinageOperand(null, d!.cobolWord()?.GetText() ?? d.GetText());
        file.Linage = new LinageInfo(
            Operand(lc.dataReference(), lc.integerLiteral()),
            lc.linageFootingPhrase() is { } f ? Operand(f.dataReference(), f.integerLiteral()) : null,
            lc.linageLinesAtTopPhrase() is { } t ? Operand(t.dataReference(), t.integerLiteral()) : null,
            lc.linageLinesAtBottomPhrase() is { } b ? Operand(b.dataReference(), b.integerLiteral()) : null);
    }

    /// <summary>Bind the I-O-CONTROL paragraph (ISO §12.4.6). A record-area SAME clause (Format 2) makes the listed
    /// files "share a memory area for processing the current logical record … equivalent to an implicit redefinition
    /// of the area with records aligned on the leftmost byte position" (§12.4.6.4 GR2) — modeled by chaining each
    /// listed file's FIRST record as a synthesized REDEFINES of the first LISTED file's first record, exactly the
    /// multi-01-under-one-FD mechanism (the singular-pattern rule): the tier machinery then aliases every record of
    /// every listed file over ONE backing, and READ/WRITE/RELEASE image distribution gives the
    /// record-of-the-most-recently-read-file semantics for free. A sort/merge file may appear in a record-area
    /// clause (SR6 — ST131A's <c>READ FILE3</c> then <c>RELEASE S3</c> with no FROM relies on it). The file-area
    /// (Format 1) and sort-merge-area (Format 3) formats are storage-economy permissions (GR1/GR4 — shared/reusable
    /// ALLOCATION plus open-mode constraints on the program, nothing a typed-native runtime must alias) — bound as
    /// conformant no-ops; MULTIPLE FILE TAPE is obsolete and parsed-and-ignored (grammar note), and so is the
    /// X3.23-1985 RERUN clause (a checkpoint HINT with no program-visible effect — a null rerun facility is
    /// conforming; deleted by ISO 2002, 0902-gated ≥2002 by the version-conformance pass, VCR Table 7 row 7.15) —
    /// both skip through the non-SAME `continue` below by design. The SR2–SR11 static
    /// legality checks (report/sort/file-area cross-membership consistency) are the diagnose-correctly track —
    /// staged with the version-conformance pass phase, not silently absent by oversight.</summary>
    private void BindIoControl(Core.ProgramUnitContext program)
    {
        foreach (var env in EnvDivisions(program)) BindIoControl(env);
    }

    private void BindIoControl(Core.EnvironmentDivisionContext env)
    {
        var io = env.inputOutputSection()?.ioControlParagraph();
        if (io is null) return;
        foreach (var clause in io.ioControlClause())
        {
            // Format 2 only — SAME RECORD AREA (the RECORD word distinguishes it; SORT/SORT-MERGE are Format 3).
            if (clause.sameClause() is not { } same || same.RECORD() is null) continue;
            DataItem? anchor = null;
            foreach (var fn in same.fileName())
            {
                if (!FilesByName.TryGetValue(fn.GetText(), out var f) || f.Records.Count == 0) continue;
                if (anchor is null) { anchor = f.Records[0]; continue; }
                if (!ReferenceEquals(f.Records[0], anchor))
                    f.Records[0].RedefinesTarget ??= anchor;   // leftmost-aligned over the one area (GR2)
            }
        }
    }

    /// <summary>Resolve each file's FILE STATUS data-name to its item (post-build, once the forest is indexed).</summary>
    internal void ResolveFiles()
    {
        foreach (var file in Files)
        {
            if (file.FileStatusName is { } sn && ByName.TryGetValue(sn, out var list) && list.Count > 0)
                file.FileStatusItem = list[0];
            // Keyed organizations: RECORD KEY / ALTERNATE RECORD KEY name items WITHIN the file's record
            // descriptions (ISO §12.4.5.12 SR2 / §12.4.5.6 SR2), possibly IN/OF-qualified (§8.4.2.2 — same-named
            // keys under different areas, IX215A); RELATIVE KEY is OUTSIDE the record (ISO §12.4.5.13 SR3) —
            // a plain name lookup.
            DataItem? InRecords(string keyName, IReadOnlyList<string> quals) =>
                file.Records.Select(r => FindQualified(r, keyName, quals)).FirstOrDefault(x => x is not null)
                ?? (quals.Count == 0 && ByName.TryGetValue(keyName, out var l) && l.Count > 0 ? l[0] : null);
            if (file.RecordKeyName is { } rk) file.RecordKeyItem = InRecords(rk, file.RecordKeyQualifiers);
            foreach (var (altName, altQuals, dups, suppress) in file.AlternateKeyNames)
                if (InRecords(altName, altQuals) is { } alt)
                    file.AlternateKeys.Add((alt, dups, suppress));
            ResolveFileCollating(file);   // §12.4.5.7 — per-key collating weights (needs the resolved keys)
            if (file.RelativeKeyName is { } rl && ByName.TryGetValue(rl, out var rlist) && rlist.Count > 0)
                file.RelativeKeyItem = rlist[0];
            // RECORD VARYING … DEPENDING ON names an integer item outside the record (ISO §13.18.43 SR — the
            // length register WRITE/REWRITE/RELEASE read per GR13a and READ/RETURN set per GR15).
            if (file.Varying?.DependingName is { } vn && ByName.TryGetValue(vn, out var vlist) && vlist.Count > 0)
                file.VaryingDependingItem = vlist[0];
            // LINAGE data-name operands (ISO §13.18.34 GR6b) name elementary unsigned integer items not subject
            // to OCCURS (SR1/SR2) — a plain name lookup, exactly the VaryingDependingItem pattern.
            if (file.Linage is { } lin)
                foreach (var op in lin.Operands)
                    if (op.DataName is { } ln && ByName.TryGetValue(ln, out var llist) && llist.Count > 0)
                        op.Item = llist[0];
        }
    }

    private static FileOrganization MapOrganization(Core.OrganizationClauseContext org)
    {
        var t = org.organizationType();
        if (t is null) return FileOrganization.Sequential;
        if (t.LINE() is not null) return FileOrganization.LineSequential;
        if (t.RELATIVE() is not null) return FileOrganization.Relative;
        if (t.INDEXED() is not null) return FileOrganization.Indexed;
        return FileOrganization.Sequential;
    }

    private static FileAccessMode MapAccessMode(Core.AccessModeClauseContext acc)
    {
        var m = acc.accessMode();
        if (m?.RANDOM() is not null) return FileAccessMode.Random;
        if (m?.DYNAMIC() is not null) return FileAccessMode.Dynamic;
        return FileAccessMode.Sequential;
    }

    /// <summary>VALUE-clause literal/category conformance for national and boolean receivers (ISO §13.18.63,
    /// the COBOLNET0898 band). SR5: a category-national item takes a national literal or a figurative constant
    /// (SPACE / QUOTE / HIGH-VALUE / LOW-VALUE / ZERO, §8.3.3.6 GR1/GR6/GR7). SR10: a category-boolean item
    /// takes a boolean literal or figurative ZERO (no boolean SPACE/QUOTE/HIGH/LOW exists — the §14.9.25.3 SR7
    /// posture). Both directions: an <c>N"…"</c>/<c>B"…"</c> literal seeds no OTHER category. Size: the decoded
    /// content shall not exceed the item's positions (SR5/SR10; alphanumeric receivers keep their historical
    /// truncating store — only the new categories get the strict check).</summary>
    /// <summary>True when <paramref name="raw"/> is a plain numeric literal (optional sign, digits, one decimal
    /// point) whose value is NOT zero — the VCR 86 gate subject (ISO §13.18.63 SR6 exempts the literal-zero forms
    /// at all editions, so <c>0</c>/<c>0.00</c> return false; a quoted/national/figurative VALUE is not numeric).</summary>
    private static bool IsNonZeroNumericLiteral(string raw)
    {
        string t = raw.Trim();
        if (t.StartsWith('+') || t.StartsWith('-')) t = t[1..];
        bool sawDigit = false, anyNonZero = false, sawDot = false;
        foreach (char c in t)
        {
            if (c == '.') { if (sawDot) return false; sawDot = true; continue; }
            if (!char.IsAsciiDigit(c)) return false;
            sawDigit = true;
            if (c != '0') anyNonZero = true;
        }
        return sawDigit && anyNonZero;
    }

    private void ValidateValueCategory(PicInfo pic, string raw, string where)
    {
        bool isNatLit = raw.Length >= 3 && raw[0] is 'N' or 'n' && raw[1] is '"' or '\'';
        bool isBoolLit = raw.Length >= 3 && raw[0] is 'B' or 'b' && raw[1] is '"' or '\'';
        bool isPlainString = raw.Length >= 1 && raw[0] is '"' or '\'';
        bool isNumeric = raw.Length >= 1 && (char.IsAsciiDigit(raw[0]) || raw[0] is '+' or '-' or '.');
        // The part after a leading ALL (GetText concatenates tokens, so `ALL SPACES` → "ALLSPACES",
        // `ALL "AB"` → 'ALL"AB"'). `ALL "literal"` is an alphanumeric ALL-literal (illegal for national/
        // boolean); `ALL SPACES` / `ALL ZEROS` is just the figurative WORD repeated (legal).
        string afterAll = raw.Length > 3 && raw.StartsWith("ALL", StringComparison.OrdinalIgnoreCase)
            ? raw[3..] : raw;
        bool isAllQuoted = !ReferenceEquals(afterAll, raw) && afterAll.Length >= 1 && afterAll[0] is '"' or '\'';
        string word = afterAll.ToUpperInvariant();
        bool isZeroWord = word is "ZERO" or "ZEROS" or "ZEROES";
        bool isNationalFigurative = isZeroWord
            || word is "SPACE" or "SPACES" or "QUOTE" or "QUOTES"
                or "HIGH-VALUE" or "HIGH-VALUES" or "LOW-VALUE" or "LOW-VALUES";
        switch (pic.Category)
        {
            // National: an N"…" literal or a figurative constant (§8.3.3.6 GR1/GR6/GR7 — SPACE/QUOTE/HIGH/
            // LOW/ZERO, incl. their ALL-prefixed forms). Plain strings, B"…", numeric, and ALL "literal" are
            // illegal.
            case PicCategory.National when isPlainString || isBoolLit || isNumeric || isAllQuoted
                    || !(isNatLit || isNationalFigurative):
                Edition.Error("COBOLNET0898", $"{where}: the VALUE of a national data item shall be a national "
                    + "literal (N\"…\") or a figurative constant (ISO §13.18.63 SR5)");
                break;
            case PicCategory.National when isNatLit && CobolLiteral.Decode(raw).Length > pic.Length:
                Edition.Error("COBOLNET0898", $"{where}: the VALUE national literal exceeds the item's "
                    + $"{pic.Length} national positions (ISO §13.18.63 SR5)");
                break;
            // Boolean: a B"…" literal or figurative ZERO (incl. ALL ZEROS) — no boolean SPACE/QUOTE/HIGH/LOW
            // exists (§14.9.25.3 SR7 posture).
            case PicCategory.Boolean when !isBoolLit && !isZeroWord:
                Edition.Error("COBOLNET0898", $"{where}: the VALUE of a boolean data item shall be a boolean "
                    + "literal (B\"…\") or the figurative constant ZERO (ISO §13.18.63 SR10)");
                break;
            case PicCategory.Boolean when isBoolLit && CobolLiteral.Decode(raw).Length > pic.Length:
                Edition.Error("COBOLNET0898", $"{where}: the VALUE boolean literal exceeds the item's "
                    + $"{pic.Length} boolean positions (ISO §13.18.63 SR10)");
                break;
            case not (PicCategory.National or PicCategory.Boolean) when isNatLit || isBoolLit:
                Edition.Error("COBOLNET0898", $"{where}: a {(isNatLit ? "national (N\"…\")" : "boolean (B\"…\")")} "
                    + "literal may seed only a data item of its own category (ISO §13.18.63 SR5/SR10)");
                break;
        }
    }

    // (The former private DecodeString twin is retired — all callers use CobolNet.Common.CobolLiteral.Decode,
    // the one ISO §8.3.1.2 literal codec, PHASE-05 Step 1.)

    /// <summary>The most-recently-opened 01/77 record, so a following level-66 RENAMES attaches to its owner.</summary>
    private DataItem? _lastRoot;

    /// <summary>Index a named item in the <see cref="ByName"/> multimap (COBOL allows duplicate names disambiguated
    /// only by qualification).</summary>
    private void RegisterName(DataItem item)
    {
        if (item.CobolName is not { } name) return;
        if (!ByName.TryGetValue(name, out var list)) ByName[name] = list = [];
        list.Add(item);
    }

    // ── TYPEDEF / the TYPE clause (ISO §13.18.58 / §13.18.57; data-model D17) ──────────────────────────────────

    /// <summary>Post-build TYPE-expansion pass: every <c>TYPE IS type-name</c> reference (a real item in the forest)
    /// gets the referenced type declaration's subtree cloned in. Runs at the top of <see cref="BindResolve"/> — AFTER
    /// all <see cref="BindEntries"/> (so forward references resolve; every TYPEDEF is in <see cref="TypeDecls"/>) and
    /// BEFORE the resolution passes (so the clone is a normal part of the forest they walk).</summary>
    internal void ExpandTypes()
    {
        _typedIndexNames.Clear();   // per-program: the ≥2×-INDEXED-type collision guard (D17 inc 4)
        foreach (var item in AllItems().Where(i => i.TypeRefName is not null).ToList())
            ExpandType(item, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        // SAME AS (ISO §13.18.49; P10 Step 16) — expanded AFTER every TYPE reference, so a data-name-1 that
        // was itself declared with a TYPE clause copies its fully-EXPANDED description (GR1 — "as though the
        // data description identified by data-name-1 had been coded in place"). Chains (a target with its own
        // pending SAME AS) recurse inside ExpandSameAs; cycles are the §13.18.49.3 SR3 rejection.
        foreach (var item in AllItems().Where(i => i.SameAsName is not null).ToList())
            ExpandSameAs(item, []);
    }

    /// <summary>Clone the referenced TYPEDEF template into <paramref name="item"/> (ISO §13.18.57.4 GR1/GR2): an
    /// ELEMENTARY type copies its PICTURE onto the item (a leaf); a GROUP type clones each subordinate under the item
    /// (a group). The subject's OWN OCCURS (an array-of-type, §13.16 SR14) and VALUE (GR3) are already on the item and
    /// preserved. <paramref name="expanding"/> is the set of type-names on the current expansion chain — a nested TYPE
    /// reference back to one is a recursive type declaration (§13.18.58.3 SR2 → COBOLNET1530).</summary>
    private void ExpandType(DataItem item, HashSet<string> expanding)
    {
        string typeName = item.TypeRefName!;
        item.TypeRefName = null;   // mark expanded (idempotent; also stops a cloned nested ref being re-processed)
        string subject = item.CobolName ?? item.CsName;
        if (!TypeDecls.TryGetValue(typeName, out var template))
        {
            Edition.Error("COBOLNET1530", $"TYPE '{typeName}' on '{subject}': the type-name is not defined by any "
                + "TYPEDEF entry (ISO §13.18.57 / §13.18.58)");
            return;
        }
        if (!expanding.Add(typeName))
        {
            Edition.Error("COBOLNET1530", $"TYPE '{typeName}' on '{subject}': a type declaration shall not directly "
                + "or indirectly reference itself (ISO §13.18.58.3 SR2)");
            return;
        }
        // Record the TYPE identity BEFORE cloning children (review DEVLOG 664 fix #5): a nested TYPE reference is
        // expanded inside CloneItem, and its SR6 strong-placement check walks THIS item's ancestor chain — so the
        // enclosing strong item's StrongType must already be set, else legal strong-in-strong nesting is falsely
        // rejected. TypeName also anchors the §8.5.3 same-type test (see DataItem.TypeAnchor).
        item.TypeName = typeName;
        item.StrongType = template.TypedefStrong;

        // An EXTERNAL type declaration's effect lands on its REFERENCES (ISO §13.18.22; P10 Step 16 — the
        // former COBOLNET1534 stage is lifted): GR2 — a data description containing an external type shall be
        // at level-number 1; GR3 — such a record is itself EXTERNAL (marked here; CallBindExternalAndGlobal
        // re-bases it onto the run-unit ExternalStore cell exactly like an explicitly-EXTERNAL record).
        if (template.IsExternalTypedef)
        {
            if (item.Level != 1)
                Edition.Error(DiagnosticCatalog.ExternalTypeRule, $"'{subject}' references EXTERNAL type "
                    + $"'{typeName}': a data description containing an external type declaration shall be at "
                    + "level-number 1 (ISO §13.18.22 GR2)");
            else
                item.ExternalFromType = true;   // §13.18.22 GR3
        }
        // §13.18.22 SR5: when a record description is an external item, an associated type declaration that is
        // strongly typed shall also be external.
        if (item.HasExternalClause && template.TypedefStrong && !template.IsExternalTypedef)
            Edition.Error(DiagnosticCatalog.ExternalTypeRule, $"'{subject}': an EXTERNAL record described with "
                + $"STRONG type '{typeName}' requires that type declaration to be external too "
                + "(ISO §13.18.22 SR5)");
        // VCR 16, the STRENGTH half (§13.16.3 SR13 ¶2; Annex E.2 item 10; the P13 review finding C9): "If the
        // CONSTANT RECORD clause is specified with the EXTERNAL clause, there shall also be a TYPE clause that
        // specifies a STRONGLY typed definition." The declaration-site check (BindEntry) can verify only TYPE
        // PRESENCE — strength is known HERE, where the template resolved. ≥2023 like its presence half (the
        // requirement is the 2023 flip; below 2023 a weak-TYPE external constant record was legal). Scoped to the
        // literal EXTERNAL-clause co-occurrence SR13 ¶2 names (an item external only via the TYPE's own EXTERNAL —
        // GR3 — has no EXTERNAL clause, and its external typedef is separately gated by VCR 63 when strong).
        if (item.IsConstantRecord && item.HasExternalClause && !template.TypedefStrong
            && Edition.DialectLevel >= 2023)
            Edition.Error(DiagnosticCatalog.ConstantRecordRule, $"'{subject}': a CONSTANT RECORD clause specified "
                + $"with the EXTERNAL clause requires a TYPE clause naming a STRONGLY typed definition — "
                + $"'{typeName}' is a weak (non-STRONG) typedef (ISO §13.16.3 SR13 ¶2; Annex E.2 item 10; ≥2023)");

        // §13.18.57.3 SR7 (review fix #1): a level-77 subject requires an ELEMENTARY type — a level-77 item is an
        // independent elementary item (§13.18.38). Version- AND strength-invariant (this applies to WEAK types too;
        // the old SR6 branch caught only the STRONG case).
        if (item.Level == 77 && template.IsGroup)
            Edition.Error("COBOLNET1536", $"'{subject}': a level-77 item referencing TYPE '{typeName}' requires an "
                + "elementary type — a level-77 item is an independent elementary item (ISO §13.18.57.3 SR7)");

        // §13.18.57.3 SR5 (review fix #3): no group SUPERORDINATE to a TYPE subject may carry a USAGE or SIGN clause —
        // it would silently override the type declaration's fixed representation.
        for (var p = item.Parent; p is not null; p = p.Parent)
            if (p.OwnUsage is not null || p.OwnSign is not null)
            {
                Edition.Error("COBOLNET1538", $"'{subject}': a group to which a TYPE reference is subordinate shall "
                    + "not carry a USAGE or SIGN clause (ISO §13.18.57.3 SR5)");
                break;
            }

        // §13.18.57.3 SR6: a STRONG type may be referenced only at level 1 or by an item subordinate to another
        // strongly-typed group — strong typing always covers a WHOLE record, never a lone field in an ordinary group.
        if (template.TypedefStrong)
        {
            bool underStrong = false;
            for (var p = item.Parent; p is not null; p = p.Parent)
                if (p.StrongType) { underStrong = true; break; }
            if (item.Level != 1 && !underStrong)
                Edition.Error("COBOLNET1532", $"'{subject}' references STRONG type '{typeName}': a strongly-typed "
                    + "item shall be specified only at level 1 or subordinate to a strongly-typed group "
                    + "(ISO §13.18.57.3 SR6)");
        }

        // Clone the template's structure in (children / the entry description / the type's root-level 88s)
        // AFTER the flags above. The entry-description copy (§13.18.58.4 GR3 — "all other data description
        // clauses ... are assumed by data defined using the type-name") shares CopyEntryDescription with the
        // SAME AS expansion; copySync: false — §13.18.57.4 GR1 EXCLUDES alignment (contrast §13.18.49 GR1,
        // which copies it). The subject's own VALUE wins (§13.18.57.4 GR3 — RawValue ??=).
        if (template.IsGroup)
            foreach (var child in template.Children)
                item.Children.Add(CloneItem(child, item, expanding));
        CopyEntryDescription(template, item, copySync: false);
        foreach (var c88 in template.Own88s) CloneConditionOnto(item, c88);   // the type's ROOT-level 88s (GR1; D17 inc 3)
        expanding.Remove(typeName);
    }

    /// <summary>Copy one entry's data description CLAUSES onto another (the shared GR-1 body of
    /// <see cref="ExpandType"/> — ISO §13.18.58.4 GR3 / §13.18.57.4 GR1 — and <see cref="ExpandSameAs"/> —
    /// §13.18.49 GR1): PICTURE / USAGE / SIGN / VALUE / JUSTIFIED / BLANK WHEN ZERO / the deferred
    /// NATIONAL-BIT mark / the carried TYPE identity. The receiver's OWN clause always wins (<c>??=</c> —
    /// §13.18.57.4 GR3 for VALUE; a SAME AS subject can own none of these, §13.16.3 SR12). NEVER copied:
    /// level-number, name, OCCURS (a SAME AS target owns none, SR5; a subject's own OCCURS is the
    /// array-of-description form, §13.16.3 SR12/SR14), REDEFINES / EXTERNAL / GLOBAL / CONSTANT RECORD
    /// (both GR-1 exclusion lists), BASED (§13.18.57.4 GR4 — the subject's own applies). SYNCHRONIZED
    /// (alignment) is copied ONLY for SAME AS (§13.18.49 GR1 has no alignment exclusion; §13.18.57.4 GR1
    /// excludes it).</summary>
    private static void CopyEntryDescription(DataItem from, DataItem to, bool copySync)
    {
        to.Pic ??= from.Pic;
        if (to.Pending is PicPending.None) to.Pending = from.Pending;
        to.RawValue ??= from.RawValue;
        to.Justified |= from.Justified;
        to.BlankWhenZero |= from.BlankWhenZero;
        if (copySync) to.Synchronized |= from.Synchronized;
        to.OwnUsage ??= from.OwnUsage;
        to.OwnSign ??= from.OwnSign;
        to.TypeName ??= from.TypeName;     // a data-name-1 declared with TYPE carries its type identity (§8.5.3)
        to.StrongType |= from.StrongType;
    }

    /// <summary>Deep-clone one template node under <paramref name="newParent"/> (generalizes the flat OO compiler-temp
    /// clone, <c>CreateCompilerTemp</c>): a FRESH <see cref="DataItem.Uid"/> (StructName/ProfileName ride on it, so a
    /// shared Uid would collide the clone's emitted type/profile with the template's), the immutable
    /// <see cref="DataItem.Pic"/> shared, the description fields copied, the <see cref="DataItem.CsName"/> re-uniquified
    /// in the NEW scope, and — unlike the template — REGISTERED (clones ARE referenceable). A nested TYPE reference in
    /// the template is expanded in place; so is a nested SAME AS (§13.18.49 — a subordinate of the copied
    /// description carrying SAME AS re-expands per clone, the TypeRefName pattern). <paramref name="levelDelta"/>
    /// renumbers the cloned subtree's level-numbers relative to the new subject (ISO §13.18.49.4 GR2b — SAME AS
    /// splices a level-1 description under an arbitrary-level subject, and the adjusted levels may exceed 49,
    /// GR2c; TYPE expansion passes 0 — its templates clone level-verbatim, byte-stable).</summary>
    private DataItem CloneItem(DataItem src, DataItem newParent, HashSet<string> expanding, int levelDelta = 0)
    {
        var clone = new DataItem
        {
            Level = src.Level + levelDelta,
            CobolName = src.CobolName,
            CsName = Unique(src.CsName, newParent.Children.Select(c => c.CsName)),
            Pic = src.Pic,
            Pending = src.Pending,   // the deferred NATIONAL/BIT adjudication travels with the clone (P5.11c)
            OwnSign = src.OwnSign,
            OwnUsage = src.OwnUsage,
            RawValue = src.RawValue,
            Occurs = src.Occurs,
            // Clone the OccursSpec — never SHARE it: its Depending / CapacityRegister are RESOLVED per-clone by the
            // post-build OdoResolve / DynamicResolve, so a shared object would let two clones of the same group type
            // collide on those fields (D17 risk #1). Copy the declared fields; leave the resolved ones null.
            OccursSpec = src.OccursSpec is { } os ? CloneOccursSpec(os) : null,
            RedefinesTargetName = src.RedefinesTargetName,
            Justified = src.Justified,
            BlankWhenZero = src.BlankWhenZero,
            TypeRefName = src.TypeRefName,
            SameAsName = src.SameAsName,   // a pending nested SAME AS re-expands per clone (below)
        };
        clone.Uid = _uidCounter++;
        clone.Parent = newParent;
        clone.SameAsQualifiers.AddRange(src.SameAsQualifiers);
        // An ALREADY-EXPANDED source subtree (a SAME AS target that was declared with TYPE) carries its type
        // identity on inner nodes — copy it so the clone stays anchored for the §8.5.3 same-type test. For the
        // TYPE-template flows these are always null/false (templates are never expanded in place; nested refs
        // expand per-clone), so this is a no-op there.
        clone.TypeName = src.TypeName;
        clone.StrongType = src.StrongType;
        foreach (var idx in src.IndexNames)
        {
            clone.IndexNames.Add(idx);
            // §13.18.38 (D17 inc 4, staged loud): a TYPE with an INDEXED BY table referenced ≥2× clones the same
            // global index-name onto two tables — they would share one C# index field and silently cross-drive.
            if (!_typedIndexNames.Add(idx))
                Edition.Error("COBOLNET1531", $"INDEXED BY '{idx}' comes from a type declaration referenced more "
                    + "than once — a type whose OCCURS has an INDEXED BY phrase may be referenced at most once, else "
                    + "the global index-name collides (ISO §13.18.38; data-model D17 residue)");
        }
        RegisterName(clone);
        foreach (var c88 in src.Own88s) CloneConditionOnto(clone, c88);   // §13.18.58.4 GR1 — the 88s are part of the type (D17 inc 3)
        foreach (var child in src.Children)
            clone.Children.Add(CloneItem(child, clone, expanding, levelDelta));
        if (clone.TypeRefName is not null) ExpandType(clone, expanding);   // a nested TYPE reference inside the template
        if (clone.SameAsName is not null) ExpandSameAs(clone, []);         // a nested SAME AS inside the copied description (§13.18.49)
        return clone;
    }

    /// <summary>Clone one level-88 condition-name from a TYPEDEF template item onto its <paramref name="target"/> clone
    /// (ISO §13.18.58.4 GR1 — the condition-names are part of the type): the clone's Parent is the target, its VALUE
    /// set is copied, and — unlike the template's copy — it IS registered in the global by-name index (a clone's
    /// condition-names ARE referenceable). D17 inc 3.</summary>
    private void CloneConditionOnto(DataItem target, Condition88 src)
    {
        var c = new Condition88 { Name = src.Name, Parent = target };
        c.Values.AddRange(src.Values);
        target.Own88s.Add(c);
        if (!Conditions.TryGetValue(c.Name, out var list)) Conditions[c.Name] = list = [];
        list.Add(c);
    }

    /// <summary>Copy an <see cref="OccursSpec"/>'s DECLARED fields for a TYPEDEF clone (D17), leaving the post-build
    /// RESOLVED fields (<c>Depending</c>/<c>CapacityRegister</c>) null so OdoResolve/DynamicResolve re-bind them in
    /// the clone's OWN scope — a shared spec would let sibling clones of the same type collide on those.</summary>
    private static OccursSpec CloneOccursSpec(OccursSpec s)
    {
        var c = new OccursSpec
        {
            Min = s.Min, Max = s.Max, DependingName = s.DependingName, IsDynamic = s.IsDynamic,
            CapacityName = s.CapacityName, InitialCap = s.InitialCap, ExpectedMax = s.ExpectedMax,
            Initialized = s.Initialized,
        };
        c.AscendingKeyNames.AddRange(s.AscendingKeyNames);
        c.DescendingKeyNames.AddRange(s.DescendingKeyNames);
        return c;
    }

    // ── SAME AS (ISO §13.18.49, COBOL-2002; P10 Step 16) ─────────────────────────────────────────────────────

    /// <summary>Expand one <c>SAME AS data-name-1</c> reference (ISO §13.18.49): resolve data-name-1 as an
    /// ordinary (optionally OF/IN-qualified) data-name, enforce the §13.18.49.3 syntax rules, then copy its
    /// data description onto <paramref name="item"/> — the subject "as though the data description identified
    /// by data-name-1 had been coded in place of the SAME AS clause" (GR1), via the ONE
    /// <see cref="CopyEntryDescription"/>/<see cref="CloneItem"/> machinery TYPE expansion uses
    /// (feedback_singular_pattern; SAME AS is structurally the TYPE expansion with a DATA-NAME source).
    /// Excluded from the copy per GR1: data-name-1's level-number, name, CONSTANT RECORD, EXTERNAL, GLOBAL,
    /// REDEFINES, and SELECT WHEN (not modeled). Subordinate levels renumber relative to the subject (GR2b/c —
    /// the <see cref="CloneItem"/> levelDelta). <paramref name="expanding"/> is the SAME-AS expansion chain —
    /// a target already on it is the SR3 cycle.</summary>
    private void ExpandSameAs(DataItem item, HashSet<DataItem> expanding)
    {
        if (item.SameAsName is null) return;   // already expanded through a chain hop (idempotent — the TypeRefName pattern)
        string targetName = item.SameAsName;
        item.SameAsName = null;
        string subject = item.CobolName ?? item.CsName;
        expanding.Add(item);

        // §13.18.49.3 SR2: the entry shall not be immediately followed by a subordinate data description entry
        // or level-88 entry — the copied description IS the subject's whole shape.
        if (item.Children.Count > 0 || item.Own88s.Count > 0)
        {
            Edition.Error(DiagnosticCatalog.SameAsEntryRule, $"'{subject}': a data description entry that "
                + "specifies the SAME AS clause shall not be immediately followed by a subordinate data "
                + "description entry or level 88 entry (ISO §13.18.49.3 SR2)");
            return;
        }
        // §13.18.49.3 SR9: no group containing the subject may carry a GROUP-USAGE, SIGN, or USAGE clause —
        // it would silently override the copied representation (the TYPE-clause §13.18.57.3 SR5 twin, 1538).
        // (GROUP-USAGE is not modeled — its national/bit group forms are the staged national legs.)
        for (var p = item.Parent; p is not null; p = p.Parent)
            if (p.OwnUsage is not null || p.OwnSign is not null)
            {
                Edition.Error(DiagnosticCatalog.SameAsEntryRule, $"'{subject}': a group item to which a SAME AS "
                    + "entry is subordinate shall not contain a GROUP-USAGE, SIGN, or USAGE clause "
                    + "(ISO §13.18.49.3 SR9)");
                break;   // report once; keep expanding under the already-failed compile
            }

        // Resolve data-name-1: an ordinary data-name reference (qualification narrows by ancestor names);
        // never subscripted — SR1 makes a table-subordinate target illegal anyway. TYPEDEF template members
        // are OFF ByName (§13.18.58.4 GR1), so a type declaration's insides are unreachable here by design.
        var candidates = ByName.TryGetValue(targetName, out var byName)
            ? byName.Where(c => SameAsQualifiersMatch(c, item.SameAsQualifiers)).ToList()
            : [];
        if (candidates.Count != 1)
        {
            Edition.Error(DiagnosticCatalog.SameAsReferencedEntry, $"'{subject}': SAME AS "
                + $"'{targetName}{(item.SameAsQualifiers.Count > 0 ? " OF " + string.Join(" OF ", item.SameAsQualifiers) : "")}' "
                + (candidates.Count == 0
                    ? "does not resolve to a data description entry (ISO §13.18.49.2 — data-name-1 shall "
                      + "reference a data item; §13.18.49.3 SR7)"
                    : "is ambiguous — the reference shall identify exactly one entry (ISO §8.4.3.2)"));
            return;
        }
        var target = candidates[0];

        // §13.18.49.3 SR3 — cycles: data-name-1 (or its description, through a chain) shall not reference the
        // subject or any group the subject is subordinate to. The chain leg is the expanding-set test; the
        // containment leg is the subject-ancestor walk. (SR4 — a TYPE clause in data-name-1's description
        // referencing the subject's record — is caught by the same walks over the EXPANDED target, plus
        // §13.18.57.3 SR1 on the TYPE side.)
        if (ReferenceEquals(target, item) || expanding.Contains(target))
        {
            Edition.Error(DiagnosticCatalog.SameAsCycle, $"'{subject}': SAME AS '{targetName}' directly or "
                + "indirectly references the subject of the entry (ISO §13.18.49.3 SR3)");
            return;
        }
        // A chained SAME AS target expands FIRST, so GR1 copies the COMPLETE description; a pending TYPE
        // reference likewise (defensive — the SAME AS loop runs after the TYPE loop, so Roots targets are
        // already expanded; ExpandType is idempotent via the TypeRefName-null mark).
        if (target.SameAsName is not null) ExpandSameAs(target, expanding);
        if (target.TypeRefName is not null) ExpandType(target, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        for (var p = item.Parent; p is not null; p = p.Parent)
            if (ReferenceEquals(p, target))
            {
                Edition.Error(DiagnosticCatalog.SameAsCycle, $"'{subject}': SAME AS '{targetName}' references a "
                    + "group item to which this entry is subordinate (ISO §13.18.49.3 SR3)");
                return;
            }

        // §13.18.49.3 SR7: data-name-1 shall reference an elementary item or a level-1 group item (of the
        // file / working-storage / local-storage / linkage section; a level-66 RENAMES alias is neither).
        if (target.Level == 66 || !(target.IsElementary || target.Level == 1))
        {
            Edition.Error(DiagnosticCatalog.SameAsReferencedEntry, $"'{subject}': SAME AS '{targetName}' shall "
                + "reference an elementary item or a level 1 group item (ISO §13.18.49.3 SR7)");
            return;
        }
        // §13.18.49.3 SR5: data-name-1's own entry shall not contain an OCCURS clause (subordinates may).
        if (target.Occurs is not null || target.OccursSpec is not null)
        {
            Edition.Error(DiagnosticCatalog.SameAsReferencedEntry, $"'{subject}': the description of SAME AS "
                + $"target '{targetName}' shall not contain an OCCURS clause — only items subordinate to it may "
                + "(ISO §13.18.49.3 SR5)");
            return;
        }
        // §13.18.49.3 SR1: data-name-1 shall not be SUBJECT TO any OCCURS clause (no table ancestor — the
        // reference is a bare data-name, never subscripted).
        for (var p = target.Parent; p is not null; p = p.Parent)
            if (p.IsTable)
            {
                Edition.Error(DiagnosticCatalog.SameAsReferencedEntry, $"'{subject}': SAME AS target "
                    + $"'{targetName}' shall not be subject to any OCCURS clause (ISO §13.18.49.3 SR1)");
                return;
            }
        // §13.18.49.3 SR10: data-name-1's description shall not contain a CONSTANT RECORD clause.
        if (target.IsConstantRecord)
        {
            Edition.Error(DiagnosticCatalog.SameAsReferencedEntry, $"'{subject}': the description of SAME AS "
                + $"target '{targetName}' shall not contain a CONSTANT RECORD clause (ISO §13.18.49.3 SR10)");
            return;
        }
        // §13.18.49.3 SR8: a level-77 subject requires an elementary data-name-1 (a level-77 item is an
        // independent ELEMENTARY item — the TYPE-clause SR7 twin, 1536).
        if (item.Level == 77 && target.IsGroup)
        {
            Edition.Error(DiagnosticCatalog.SameAsEntryRule, $"'{subject}': a level-77 SAME AS subject requires "
                + "an elementary data-name-1 (ISO §13.18.49.3 SR8)");
            return;
        }
        // §13.18.49.3 SR6: in the FILE SECTION, data-name-1's description (subordinates included) shall not
        // contain a USAGE OBJECT REFERENCE item.
        if (IsFileSectionItem(item) && HasObjectReferenceLeaf(target))
        {
            Edition.Error(DiagnosticCatalog.SameAsReferencedEntry, $"'{subject}': a SAME AS clause in the file "
                + $"section shall not reference '{targetName}', whose description contains a USAGE OBJECT "
                + "REFERENCE data item (ISO §13.18.49.3 SR6)");
            return;
        }

        // ── GR1/GR2: the copy. ──────────────────────────────────────────────────────────────────────────────
        // The entry description (PICTURE/USAGE/SIGN/VALUE/JUSTIFIED/BLANK WHEN ZERO/SYNCHRONIZED + the carried
        // TYPE identity; copySync: true — §13.18.49 GR1 does NOT exclude alignment, unlike §13.18.57.4 GR1).
        CopyEntryDescription(target, item, copySync: true);
        // GR3/GR5: a USAGE / SIGN clause of a group containing data-name-1 takes effect as though specified
        // for the SUBJECT (nearest enclosing clause, the §13.18.60 GR1 discipline; only an ELEMENTARY target
        // can have ancestors — SR7). The subject's chain cannot see data-name-1's ancestors, so the transform
        // the InheritUsage/InheritSign passes would have applied is applied here, on the copied Pic.
        if (item.OwnUsage is null)
            for (var p = target.Parent; p is not null; p = p.Parent)
                if (p.OwnUsage is { } au)
                {
                    item.OwnUsage = au;
                    if (au is Usage.Binary or Usage.Packed or Usage.Comp5
                        && item.Pic is { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display } upic)
                        item.Pic = upic with { Usage = au, SignKind = PicInfo.SignKindFor(au, upic.Signed, item.OwnSign) };
                    break;
                }
        if (item.OwnSign is null)
            for (var p = target.Parent; p is not null; p = p.Parent)
                if (p.OwnSign is { } asg)
                {
                    item.OwnSign = asg;
                    if (item.Pic is { Category: PicCategory.Numeric, Signed: true, Usage: Usage.Display } spic)
                        item.Pic = spic with { SignKind = PicInfo.SignKindFor(spic.Usage, signed: true, asg) };
                    break;
                }
        // GR2: a group data-name-1 → the subject becomes a group with the same subordinate names, descriptions,
        // and hierarchy; levels renumber relative to the subject (GR2b, may exceed 49 — GR2c). The ONE CloneItem.
        if (target.IsGroup)
        {
            int levelDelta = item.Level - target.Level;
            var typeExpanding = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var child in target.Children)
                item.Children.Add(CloneItem(child, item, typeExpanding, levelDelta));
        }
        foreach (var c88 in target.Own88s) CloneConditionOnto(item, c88);   // GR2a — data-name-1's own condition-names ride the copy

        // §13.18.22 GR3 carried through SAME AS: when data-name-1's external-ness came FROM ITS TYPE (not from
        // an explicit EXTERNAL clause — that one GR1 excludes), the copied description still names the external
        // type, so the subject is external under the same GR2 level-1 constraint.
        if (target.ExternalFromType)
        {
            if (item.Level != 1)
                Edition.Error(DiagnosticCatalog.ExternalTypeRule, $"'{subject}': a data description containing "
                    + "an external type declaration shall be at level-number 1 (ISO §13.18.22 GR2)");
            else
                item.ExternalFromType = true;
        }
        // A copied STRONG type identity re-checks placement (§13.18.57.3 SR6 — level 1 or subordinate to a
        // strongly-typed group; the ExpandType twin).
        if (item.StrongType && item.Level != 1)
        {
            bool underStrong = false;
            for (var p = item.Parent; p is not null; p = p.Parent)
                if (p.StrongType) { underStrong = true; break; }
            if (!underStrong)
                Edition.Error("COBOLNET1532", $"'{subject}' copies strongly-typed '{targetName}' (type "
                    + $"'{item.TypeName}'): a strongly-typed item shall be specified only at level 1 or "
                    + "subordinate to a strongly-typed group (ISO §13.18.57.3 SR6)");
        }
        expanding.Remove(item);
    }

    /// <summary>Whether a candidate matches a SAME AS reference's OF/IN qualifiers: each qualifier, in written
    /// order, names some (strictly enclosing) ancestor of the previous match (ISO §8.4.3.2 qualification).</summary>
    private static bool SameAsQualifiersMatch(DataItem candidate, List<string> qualifiers)
    {
        var p = candidate.Parent;
        foreach (string q in qualifiers)
        {
            while (p is not null && !string.Equals(p.CobolName, q, StringComparison.OrdinalIgnoreCase))
                p = p.Parent;
            if (p is null) return false;
            p = p.Parent;
        }
        return true;
    }

    /// <summary>Whether an item belongs to a FILE SECTION record (its root is some FD/SD's record) — the
    /// §13.18.49.3 SR6 placement test.</summary>
    private bool IsFileSectionItem(DataItem item)
    {
        var root = item;
        while (root.Parent is { } p) root = p;
        return Files.Any(f => f.Records.Contains(root));
    }

    /// <summary>True when the item's description (subordinates included) contains a USAGE OBJECT REFERENCE
    /// data item (ISO §13.18.49.3 SR6 / §13.18.57.3 SR8).</summary>
    private static bool HasObjectReferenceLeaf(DataItem item) =>
        item.Pic?.Category is PicCategory.ObjectReference || item.Children.Any(HasObjectReferenceLeaf);

    /// <summary>The STRONG type-declaration use restrictions that need the RESOLVED REDEFINES/RENAMES graph
    /// (ISO §13.18.57.3): <b>SR4</b> — a strongly-typed item shall not be redefined in whole or in part; <b>SR3</b> —
    /// nor renamed in whole or in part. Both → COBOLNET1532. (SR6, the level-1/strong-parent placement rule, is
    /// checked at clone time in <see cref="ExpandType"/>.) An INTERNAL redefine — a REDEFINES that is part of the same
    /// strong subtree, cloned in from the type template — is legitimate and NOT flagged (its subject and target share
    /// a strong root); only an EXTERNAL redefinition of a strong item is prohibited.</summary>
    internal void CheckStrongTypeDeclarations()
    {
        foreach (var item in AllItems())
            if (item.RedefinesTarget is { } tgt && StrongTypeModel.IsStronglyTyped(tgt)
                && !ReferenceEquals(StrongTypeModel.StrongRoot(item), StrongTypeModel.StrongRoot(tgt)))
                Edition.Error("COBOLNET1532", $"'{item.CobolName ?? item.CsName}' REDEFINES strongly-typed item "
                    + $"'{tgt.CobolName ?? tgt.CsName}': a strongly-typed item shall not be redefined in whole or in "
                    + "part (ISO §13.18.57.3 SR4)");

        foreach (var owner in Roots)
            foreach (var ren66 in owner.Renames66)
                if (ren66.Renames is { } ri
                    && ((ri.From is { } f && StrongTypeModel.IsStronglyTyped(f))
                        || (ri.Thru is { } t && StrongTypeModel.IsStronglyTyped(t))
                        || ri.SpanLeaves.Any(StrongTypeModel.IsStronglyTyped)))
                    Edition.Error("COBOLNET1532", $"RENAMES '{ren66.CobolName ?? ren66.CsName}' renames a "
                        + "strongly-typed item in whole or in part — prohibited (ISO §13.18.57.3 SR3)");
    }

    /// <summary>Bind a level-66 RENAMES entry (ISO §13.18.45): a re-grouping alias <c>RENAMES from [THRU thru]</c>
    /// over a contiguous sibling run of the owning record. It adds no storage (SR2/SR3) — it is attached to the
    /// owning record's <see cref="DataItem.Renames66"/> list (not <see cref="DataItem.Children"/>) and registered for
    /// reference resolution; the FROM/THRU operands are resolved by the post-build pass.</summary>
    private void BindRenames(Core.DataDescriptionEntryContext entry)
    {
        var rc = entry.dataDescriptionBody().renamesClause();
        if (rc is null || entry.dataName()?.GetText() is not { } name || _lastRoot is null) return;
        bool thru = rc.THRU() is not null || rc.THROUGH() is not null;
        var item = new DataItem
        {
            Level = 66,
            CobolName = name,
            CsName = DataItem.Sanitize(name),
            Renames = new RenamesInfo
            {
                // The BASE word only: an OF/IN-qualified operand (`SUB-GRP-1 OF GRP — NC252A RENAMES-TEST-2`)
                // is redundant inside the owning record, and GetText() would glue the suffix into the name.
                FromName = rc.dataReference(0).cobolWord()?.GetText() ?? rc.dataReference(0).GetText(),
                ThruName = thru && rc.dataReference().Length > 1
                    ? rc.dataReference(1).cobolWord()?.GetText() ?? rc.dataReference(1).GetText()
                    : null,
            },
        };
        item.Uid = _uidCounter++;
        item.Parent = _lastRoot;        // owning record — an alias sibling, NOT a storage child
        _lastRoot.Renames66.Add(item);
        RegisterName(item);
    }

    /// <summary>Bind a level-88 condition-name on its conditional variable <paramref name="parent"/>, capturing the
    /// VALUE set (singletons + THRU ranges) as raw operand text (decoded at emit time).</summary>
    private void BindCondition(Core.DataDescriptionEntryContext entry, DataItem parent, bool registerGlobal = true)
    {
        if (entry.dataName()?.GetText() is not { } name) return;
        var cond = new Condition88 { Name = name, Parent = parent };

        if (entry.dataDescriptionBody().dataDescriptionClauses() is { } clauses)
            foreach (var clause in clauses.dataDescriptionClause())
                if (clause.valueClause() is { } value)
                    foreach (var vi in value.valueItem())
                    {
                        // Numeric operands normalize to dot-decimal form (DECIMAL-POINT IS COMMA, ISO §12.3.7 GR14a).
                        if (vi.valueClauseRange() is { } range)
                        {
                            // §13.18.63 SR29: THROUGH shall not be specified for a boolean conditional
                            // variable (0898). A national THROUGH range is spec-legal but orders under a
                            // NATIONAL alphabet (SR31) — recognized, staged (0899).
                            if (parent.Pic is { Category: PicCategory.Boolean })
                                Edition.Error("COBOLNET0898", $"condition-name '{name}': THROUGH may not be "
                                    + "specified when the conditional variable is boolean (ISO §13.18.63 SR29)");
                            else if (parent.Pic is { Category: PicCategory.National })
                                Edition.Error(DiagnosticCatalog.NationalThroughRange, $"condition-name '{name}': a THROUGH range over "
                                    + "a national conditional variable (ordered by the national collating "
                                    + "sequence) is recognized but not yet implemented (Phase 4a residue) — "
                                    + "(ISO §13.18.63 SR31)");
                            // Fold ONCE per operand (a §8.8.3 concat folds here; RawValueOperandText) so the
                            // category check and the stored value see the same text without double diagnostics.
                            string rawLo = RawValueOperandText(range.valueClauseOperand(0));
                            string rawHi = RawValueOperandText(range.valueClauseOperand(1));
                            if (parent.Pic is { Category: not (PicCategory.Boolean or PicCategory.National) } rp)
                            {
                                // §13.18.63 SR4/SR5/SR24→SR10: the VALUE literals' category must match the
                                // conditional variable's — the SAME funnel the item-entry VALUE uses.
                                ValidateValueCategory(rp, rawLo, $"condition-name '{name}'");
                                ValidateValueCategory(rp, rawHi, $"condition-name '{name}'");
                            }
                            cond.Values.Add((rawLo, rawHi));
                        }
                        else
                            foreach (var op in vi.valueClauseOperand())
                            {
                                // §13.18.63 SR4/SR5/SR24→SR10 (both directions): an N"…"/B"…" literal seeds
                                // only its own category, and a national/boolean conditional variable takes only
                                // its own literal form — the ONE canonical checker (0898 band). Group parents
                                // (Pic null) are a separate leg.
                                // Fold ONCE (a §8.8.3 concat folds in RawValueOperandText) so the category
                                // check and the stored value share one text without double diagnostics.
                                string raw = RawValueOperandText(op);
                                if (parent.Pic is { } sp)
                                    ValidateValueCategory(sp, raw, $"condition-name '{name}'");
                                cond.Values.Add((raw, null));
                            }
                    }

        parent.Own88s.Add(cond);   // the item owns its 88s (source of truth; lets CloneItem carry a TYPEDEF's 88s)
        if (registerGlobal)
        {
            if (!Conditions.TryGetValue(name, out var list)) Conditions[name] = list = [];
            list.Add(cond);
        }
    }

    /// <summary>Make <paramref name="name"/> unique within a C# name scope, appending <c>_2</c>, <c>_3</c>, … on collision.</summary>
    private static string Unique(string name, IEnumerable<string> used)
    {
        var set = used as ICollection<string> ?? used.ToList();
        if (!set.Contains(name)) return name;
        for (int n = 2; ; n++)
        {
            string candidate = $"{name}_{n}";
            if (!set.Contains(candidate)) return candidate;
        }
    }

    /// <summary>Bind one data-description entry (skips level-66 RENAMES and level-88 condition names for now).</summary>
    private DataItem? BindEntry(Core.DataDescriptionEntryContext entry)
    {
        DataDescriptionCst e = entry;
        if (e.Level is not { } level) return null;
        if (level is 66 or 88) return null; // RENAMES / condition-names: later slice.

        string? cobolName = e.Name;
        bool isFiller = cobolName is null || cobolName.Equals("FILLER", StringComparison.OrdinalIgnoreCase);
        string csName = isFiller ? $"_filler{_fillerCounter++}" : DataItem.Sanitize(cobolName!);

        string? pictureText = null, usageText = null, rawValue = null, redefinesTargetName = null;
        string? objectClassName = null;   // USAGE OBJECT REFERENCE class-name (null = universal; §13.18.60.4)
        int? occurs = null;
        OccursSpec? occursSpec = null;
        var indexNames = new List<string>();
        SignSpec? ownSign = null;
        bool justified = false, blankWhenZero = false, synchronized = false;
        bool binaryUnsigned = false;   // USAGE BINARY-CHAR/... UNSIGNED (SIGNED is the default, ISO §13.18.60.4 GR12)
        bool noSign = false;           // USAGE PACKED-DECIMAL WITH NO SIGN (ISO §13.18.60.4 GR11 — no sign nibble; 2023)
        bool isBased = false;          // BASED (ISO §13.18.5 — a storage template; Phase-4b increment 2)
        bool isAnyLength = false;      // ANY LENGTH (ISO §13.18.2 — a runtime-length LINKAGE formal; PHASE-09 Step 11)
        bool isDynamicLength = false;  // DYNAMIC LENGTH (ISO §8.5.1.10 / §13.18.19 — a variable-length min-0 string; P12 wave 2)
        int dynLengthLimit = -1;       // the LIMIT phrase (§13.18.19.4 GR2); -1 = the implementor-defined maximum
        string? dynLengthStructureName = null;   // the optional dynamic-length-structure-name (§12.3.7 — not yet supported)
        bool hasExternal = false;      // observed for the BASED×EXTERNAL SR (the clause itself binds later)
        bool hasGlobal = false;        // GLOBAL (§13.18.27) — observed for the DYNAMIC LENGTH §13.16.3 SR18 co-clause check
        bool hasProperty = false;      // PROPERTY (§13.18.42, OO) — observed for the same SR18 check
        bool isTypedef = false, typedefStrong = false;   // TYPEDEF [STRONG] — a type declaration (ISO §13.18.58; D17)
        bool isConstantRecord = false; // CONSTANT RECORD (ISO §13.18.15 — a structured constant; P10 Step 15)
        string? typeRefName = null;    // TYPE IS type-name — the type this entry clones, expanded post-build (D17)
        string? sameAsName = null;     // SAME AS data-name-1 — the entry this one copies, expanded post-build (§13.18.49)
        IReadOnlyList<string> sameAsQuals = [];   // the SAME AS target's OF/IN qualifiers

        // The dataDescriptionClauses presence guard folds into e.Clauses (empty when the body has no clause list).
            foreach (var clause in e.Clauses)
            {
                if (clause.PictureText is { } picText)
                    pictureText = picText;
                else if (clause.Context.basedClause() is not null)
                    // BASED (§13.18.5) validated below (§13.16 SR16 placement; the 0881 declaration band). The
                    // COBOL-2002 introduction gate is VersionConformancePass ParseArm.VisitBasedClause (14g.2,
                    // recognition-based — IsBased is cleared for a LINKAGE item, so a bound-arm gate would drop it).
                    isBased = true;
                else if (clause.Context.anyLengthClause() is not null)
                    // ANY LENGTH (§13.18.2) — SR1 + the §13.16.3 SR17 clause-exclusion validated below; the
                    // COBOL-2002 introduction gate is VersionConformancePass ParseArm.VisitAnyLengthClause
                    // (recognition-based — IsAnyLength is cleared on every SR violation, so a bound-arm gate
                    // would drop the 0900 on exactly those paths; the BASED pattern).
                    isAnyLength = true;
                else if (clause.Context.dynamicLengthClause() is { } dl)
                {
                    // DYNAMIC LENGTH (§8.5.1.10 / §13.18.19) — SR1 + the §13.16.3 SR18 clause-exclusion + the
                    // structure-name non-support are validated below (the ANY LENGTH pattern). The COBOL-2014
                    // introduction gate is VersionConformancePass ParseArm.VisitDynamicLengthClause (recognition —
                    // IsDynamicLength is cleared on every SR violation, so a bound-arm gate would drop the 0900).
                    isDynamicLength = true;
                    dynLengthStructureName = dl.cobolWord()?.GetText();
                    if (dl.integerLiteral() is { } lim && int.TryParse(lim.GetText(), out int lv)) dynLengthLimit = lv;
                }
                else if (clause.Context.externalClause() is not null)
                    hasExternal = true;   // consumed by CallBindExternalAndGlobal; flagged here for the 0881 check
                else if (clause.Context.globalClause() is not null)
                    hasGlobal = true;   // §13.18.27; observed for the §13.16.3 SR18 DYNAMIC LENGTH co-clause check
                else if (clause.Context.propertyClause() is not null)
                    hasProperty = true;   // §13.18.42 (OO); observed for the SR18 DYNAMIC LENGTH co-clause check
                else if (clause.Context.typedefClause() is { } td)
                    // §13.18.58; D17. The COBOL-2002 introduction gate is VersionConformancePass ParseArm.VisitTypedefClause
                    // (14g.2, recognition-based — the typedef item is discarded from ConformanceForest when it fails to
                    // register (unnamed/duplicate) or binds into method scope, so a bound-arm gate would drop it; DEVLOG 734).
                    { isTypedef = true; typedefStrong = td.STRONG() is not null; }
                else if (clause.TypeRefName is { } trn)
                    // TYPE IS type-name — cloned in ExpandTypes (D17). The COBOL-2002 introduction gate is
                    // VersionConformancePass ParseArm.VisitTypeClause (14g.2, recognition-based — TypeRefName is
                    // nulled by ExpandTypes during bind, so a bound-arm gate would drop it).
                    typeRefName = trn;
                else if (clause.SameAsTargetName is { } san)
                    // SAME AS data-name-1 (ISO §13.18.49; P10 Step 16) — the subject copies data-name-1's
                    // description via the ONE ExpandTypes/CloneItem machinery (ExpandSameAs, GR1/GR2). The
                    // COBOL-2002 introduction gate is VersionConformancePass ParseArm.VisitSameAsClause
                    // (recognition-based — SameAsName is nulled by ExpandSameAs during bind, the TypeRefName
                    // pattern).
                    { sameAsName = san; sameAsQuals = clause.SameAsQualifiers; }
                else if (clause.Context.justifiedClause() is not null)
                    justified = true;   // JUSTIFIED [RIGHT] (ISO §13.18.34 — right-justify alphanumeric receives)
                else if (clause.Context.blankWhenZeroClause() is not null)
                    blankWhenZero = true;   // BLANK [WHEN] ZERO (ISO §13.18.8 — a zero value stores all spaces)
                else if (clause.Context.syncClause() is not null)
                    synchronized = true;   // SYNCHRONIZED/SYNC (ISO §13.18.55) — no-op here; gated on a GROUP <2023 (step 10)
                else if (clause.Context.constantRecordClause() is not null)
                    // CONSTANT RECORD (§13.18.15) — a structured constant; the §13.16.3 SR3/SR6/SR13 same-entry
                    // shape checks run below (the IsBased discipline). The COBOL-2002 introduction gate is
                    // VersionConformancePass ParseArm.VisitConstantRecordClause (recognition-based).
                    isConstantRecord = true;
                // PROPERTY clause (§13.18.42, COBOL-2002 OO): superset-parsed at every edition; its OO SEMANTICS bind
                // independently in DataBinder.Oo.OoBindPropertyClauses (which reads the propertyClause node directly),
                // and its COBOL-2002 introduction gate is VersionConformancePass ParseArm.VisitPropertyClause (14g.2) —
                // so the storage-clause loop needs no branch for it.
                else if (clause.Context.usageClause() is { } usage)
                {
                    usageText = UsageKeyword(usage);
                    // SIGNED (default) / UNSIGNED on a fixed-width binary usage (ISO §13.18.60.4 GR12) — the
                    // binarySign sibling is a direct child of usageClause in BOTH the full (USAGE IS
                    // BINARY-CHAR SIGNED) and the bare (BINARY-CHAR SIGNED) alternatives.
                    binaryUnsigned = usage.binarySign()?.UNSIGNED() is not null;
                    noSign = usage.noSignPhrase() is not null;   // §13.18.60.4 GR11 — validated against usage/picture below
                    var oru = usage.usageKeyword()?.objectReferenceUsage();
                    if (oru?.FACTORY() is not null)
                        // OBJECT REFERENCE FACTORY OF class (§13.18.60 :22681) — the factory-object
                        // reference item awaits the universal-reference wave (§16.2.2 FactoryObject).
                        Edition.Error(DiagnosticCatalog.OoFactoryObjectReference, "USAGE OBJECT REFERENCE FACTORY OF (a factory-object "
                            + "reference, ISO §13.18.60) is recognized but not yet implemented (the "
                            + "universal-reference wave)");
                    else
                        objectClassName = clause.ObjectClassName;
                }
                else if (clause.RedefinesTargetName is { } redefTarget)
                    // Capture the target name only; resolution waits until the forest is built (the target is a
                    // prior sibling, but a chain A REDEFINES B REDEFINES C resolves in the post-build pass).
                    redefinesTargetName = redefTarget;
                else if (clause.Context.valueClause() is { } value)
                    rawValue = ExtractValue(value);
                else if (clause.Context.signClause() is { } sign)
                    ownSign = new SignSpec(sign.LEADING() is not null, sign.SEPARATE() is not null);
                else if (clause.Context.occursClause() is { } occ)
                {
                    // Allocate at the table's MAXIMUM occurrence count — the last fixed bound (integer-2 for a
                    // Format-2 `n TO m` table, the sole bound for a fixed table) — per ISO §8.5.1.8 (physical
                    // capacity fixed at compile time). Each bound is an integer literal or an integer
                    // constant-name (§13.10.3 SR2) — OccursBoundValue resolves both (DataBinder.Constants.cs).
                    // The min/DEPENDING/KEY surface is captured in the OccursSpec.
                    string occWhere = $"data item '{cobolName ?? "FILLER"}'";
                    if (occ.occursBound() is { Length: > 0 } bnds && OccursBoundValue(bnds[^1], occWhere) is { } n)
                        occurs = n;
                    occursSpec = OdoBindOccursSpec(occ, occWhere, occurs);
                    if (occ.INDEXED() is not null)
                        foreach (var idxName in clause.IndexNames)
                            indexNames.Add(idxName);
                }
            }

        // Parse the usage keyword ONCE per entry — ParseUsage carries the W2 loud-guard gates (the 2002+
        // skeleton usages error, ISO §13.18.60), and a re-parse would duplicate their diagnostics.
        string entryWhere = $"data item '{cobolName ?? "FILLER"}'";
        Usage entryUsage = PictureAnalyzer.ParseUsage(usageText, Edition, entryWhere);

        // An integer constant-name may specify repetition in a PICTURE character-string (ISO §13.10.3 SR2 second
        // sentence → §13.18.40): expand `PIC X(K)` to `PIC X(5)` BEFORE Analyze reads the string. Guarded on the
        // unit actually defining constants, so a constant-free program's PICTURE pipeline is untouched.
        if (pictureText is not null && _constants.Count > 0)
            pictureText = ExpandPicConstants(pictureText, entryWhere);

        // A PICTURE-less USAGE INDEX entry is an ELEMENTARY index data item (ISO §13.18.60 — class index, no
        // PICTURE allowed), not a group: synthesize its profile so it emits as a long occurrence-number field.
        // A PICTURE-less SKELETON usage (BINARY-CHAR / POINTER / FLOAT-x / NATIONAL / BIT —
        // legally picture-less per §13.18.60) gets the RECOVERY shape: the compile has already failed, and a
        // Pic-null elementary item NREs the doomed emit on any MOVE receiver (the binary_usage crash,
        // DEVLOG 597) instead of surfacing the 0899/0900. Group headers shed it in ResolveIndexItems.
        // USAGE OBJECT REFERENCE (LIVE — the Phase-3 OO spine): a PICTURE-less elementary reference item
        // (§13.18.60.4; the IndexItem synthesis pattern). PICTURE is prohibited with it — reject loud, never
        // let Analyze classify an incoherent picture-with-reference shape (the W2 silent-misbind rule).
        if (entryUsage is Usage.ObjectReference && pictureText is not null)
        {
            Edition.Error("COBOLNET0812", $"{entryWhere}: PICTURE may not be specified with USAGE OBJECT "
                + "REFERENCE (ISO §13.18.60.4 — an object-reference item is picture-less)");
            pictureText = null;
        }
        // A TYPED reference (spine part 2 — LIVE): the declared class must resolve in the group's pass-1
        // class symbol table (OO deep-dive D1) — its emitted C# field type IS the class's emitted type
        // (PicInfo.ClrType), so an unresolved name would surface as a Roslyn CS0246 on user source (a
        // loud-failure violation). §13.18.60.4: class-name-1 shall reference a class.
        if (entryUsage is Usage.ObjectReference && objectClassName is not null
            && OoClasses?.Find(objectClassName) is null && OoClasses?.FindInterface(objectClassName) is null)
            Edition.Error("COBOLNET0813", $"{entryWhere}: USAGE OBJECT REFERENCE names the unknown class or "
                + $"interface '{objectClassName}' — the declared name of a typed object reference shall be a "
                + "class or interface of the compilation group (ISO §13.18.60.2/.4; separate compilation is "
                + "a later slice)");

        // PICTURE is prohibited on a fixed-width binary usage (ISO §13.16.3 SR8 — the item is picture-less; its
        // width and range are fixed by the usage, §13.18.60.4 GR12). Reject loud, never let Analyze classify an
        // incoherent picture-with-binary shape (the W2 silent-misbind rule; the OBJECT REFERENCE 0812 pattern).
        if (entryUsage is Usage.BinaryChar or Usage.BinaryShort or Usage.BinaryLong or Usage.BinaryDouble
            && pictureText is not null)
        {
            Edition.Error("COBOLNET0870", $"{entryWhere}: PICTURE may not be specified with a fixed-width binary "
                + "usage (BINARY-CHAR/-SHORT/-LONG/-DOUBLE) — the item is picture-less (ISO §13.16.3 SR8)");
            pictureText = null;
        }

        // PICTURE is prohibited with USAGE PROGRAM-POINTER / FUNCTION-POINTER (§13.16.3 SR8 — picture-less),
        // and a VALUE clause is prohibited (§13.18.63 SR9 — no literal denotes a program address). The
        // restricted TO-prototype form stages loud (§13.18.60 GR25 — signature matching needs the P13
        // prototype registry). The 0881 declaration band, mirroring the POINTER gates below.
        if (entryUsage is Usage.ProgramPointer or Usage.FunctionPointer)
        {
            if (pictureText is not null)
            {
                Edition.Error("COBOLNET0881", $"{entryWhere}: PICTURE may not be specified with USAGE "
                    + "PROGRAM-POINTER or FUNCTION-POINTER — the item is picture-less (ISO §13.16.3 SR8)");
                pictureText = null;
            }
            if (rawValue is not null)
            {
                Edition.Error("COBOLNET0881", $"{entryWhere}: the VALUE clause shall not be specified with a "
                    + "USAGE clause carrying the PROGRAM-POINTER or FUNCTION-POINTER phrase (ISO §13.18.63 SR9)");
                rawValue = null;
            }
            if (entryUsage is Usage.ProgramPointer
                && e.Clauses.Select(c => c.Context.usageClause()?.usageKeyword()?.programPointerUsage())
                    .FirstOrDefault(ppu => ppu is not null)?.TO() is not null)
                Edition.Error(DiagnosticCatalog.ProgramPointerRestricted,
                    $"{entryWhere}: USAGE PROGRAM-POINTER TO program-prototype-name (ISO §13.18.60 GR25)");
        }

        // PICTURE is prohibited with USAGE POINTER (§13.18.60.4 — a data-pointer is picture-less; before this
        // gate the entry silently misbound BY ITS PICTURE, the W2 hazard class). The 0881 declaration band.
        if (entryUsage is Usage.Pointer && pictureText is not null)
        {
            Edition.Error("COBOLNET0881", $"{entryWhere}: PICTURE may not be specified with USAGE POINTER — "
                + "a data-pointer item is picture-less (ISO §13.18.60.4)");
            pictureText = null;
        }

        // PICTURE is prohibited with a floating-point usage (COMP-1/COMP-2/FLOAT-SHORT/-LONG/-EXTENDED) — the item
        // is picture-less (§13.18.60.2). COBOLNET1521 (the 08xx declaration band is exhausted; this is a syntax-rule
        // violation, 15xx). Before this a float item synthesized pic=null and NRE'd the emit; a float WITH a picture
        // would misbind by that (illegal) picture. (D16.)
        if (entryUsage is Usage.Float or Usage.Double or Usage.FloatShort or Usage.FloatLong or Usage.FloatExtended
                or Usage.FloatBinary32 or Usage.FloatBinary64 or Usage.FloatBinary128
                or Usage.FloatDecimal16 or Usage.FloatDecimal34
            && pictureText is not null)
        {
            Edition.Error("COBOLNET1521", $"{entryWhere}: PICTURE may not be specified with a floating-point usage "
                + "(COMP-1/COMP-2/FLOAT-SHORT/-LONG/-EXTENDED/FLOAT-BINARY-*/FLOAT-DECIMAL-*) — a floating-point item "
                + "is picture-less (ISO §13.18.60.2)");
            pictureText = null;
        }

        var pic = pictureText is not null
            ? PictureAnalyzer.Analyze(pictureText, entryUsage, Edition, entryWhere, ownSign, CurrencyPicSymbol,
                blankWhenZero, explicitUsage: usageText is not null)
            : entryUsage is Usage.Index ? PicInfo.IndexItem
            : entryUsage is Usage.Pointer ? PicInfo.PointerItem
            : entryUsage is Usage.ProgramPointer ? PicInfo.ProgramPointerItem   // §13.18.60 GR24 (P10 Step 7)
            : entryUsage is Usage.ObjectReference ? PicInfo.ObjectReferenceItem(objectClassName)
            : entryUsage is Usage.BinaryChar or Usage.BinaryShort or Usage.BinaryLong or Usage.BinaryDouble
                ? PicInfo.BinaryItem(entryUsage, signed: !binaryUnsigned)
            // A PICTURE-less floating-point item (COMP-1/COMP-2/FLOAT-SHORT/-LONG/-EXTENDED + the 2014
            // FLOAT-BINARY-*/FLOAT-DECIMAL-* family, §13.18.60.2, D16) — its value is a native float/double, never
            // scaled-integer (before this the chain fell to null → NRE). The processor-dependent non-support forms
            // (binary128/decimal, rejected COBOLNET1564) still synthesize a Pic so the errored compile does not NRE.
            : entryUsage is Usage.Float or Usage.Double or Usage.FloatShort or Usage.FloatLong or Usage.FloatExtended
                or Usage.FloatBinary32 or Usage.FloatBinary64 or Usage.FloatBinary128
                or Usage.FloatDecimal16 or Usage.FloatDecimal34
                ? PicInfo.FloatItem(entryUsage)
            : null;   // incl. a PICTURE-less USAGE NATIONAL/BIT entry — Pending (below) carries its adjudication

        // A PICTURE-less USAGE NATIONAL/BIT entry is a GROUP header (legal — the usage sheds to subordinates,
        // §13.18.60.4 GR1) or an illegal picture-less elementary item (0881) — unknowable until the forest is
        // complete: ResolveIndexItems adjudicates via this EXPLICIT mark (P5.11c; formerly the reference-identity
        // sentinel PicInfos NationalUsagePending/BitUsagePending).
        var pending = pic is null && entryUsage is Usage.National ? PicPending.NationalUsage
            : pic is null && entryUsage is Usage.Bit ? PicPending.BitUsage
            : PicPending.None;

        // USAGE … WITH NO SIGN (ISO §13.18.60.4 GR11, 2023): applies ONLY to PACKED-DECIMAL (grammatically tolerated
        // after any usageKeyword — reject on a non-Packed usage, COBOLNET1565) and forbids an 'S' picture (SR31,
        // COBOLNET1566). When valid it drops the sign nibble via PicInfo.PackedNoSign (StorageWidth only — the value
        // path is identical to plain unsigned packed). The 2023 introduction gate is VersionConformancePass.
        if (noSign)
        {
            if (entryUsage is not Usage.Packed)
                Edition.Error("COBOLNET1565", $"{entryWhere}: the WITH NO SIGN phrase (ISO §13.18.60.4 GR11) applies "
                    + $"only to USAGE PACKED-DECIMAL, not USAGE {usageText}");
            else if (pic is { Signed: true })
                Edition.Error("COBOLNET1566", $"{entryWhere}: a PICTURE containing 'S' shall not be specified with "
                    + "PACKED-DECIMAL WITH NO SIGN (ISO §13.18.40.3 SR31 — an unsigned representation)");
            else if (pic is not null)
                pic = pic with { PackedNoSign = true };
        }

        // Edition gating (the four-compilers rule): a fixed-point picture's digit positions are capped at 18 by
        // COBOL-85 and 31 by 2002+ (ISO §8.3.1.2 / §13.18.40) — reject, never silently mis-store.
        if (pic is { Category: PicCategory.Numeric or PicCategory.NumericEdited, IsFloat: false, Digits: > 0 })
            Edition.CheckDigitCapacity(pic.Digits, $"data item '{cobolName ?? "FILLER"}' (PICTURE {pictureText})");

        // VALUE-clause literal/category conformance for the string-stored 2002 categories (ISO §13.18.63
        // SR5 national / SR10 boolean — the 0898 band, both directions).
        if (rawValue is { } rv && pic is not null) ValidateValueCategory(pic, rv, entryWhere);
        // VCR 86 (ISO §13.18.63 SR6; Annex E.3.3 item 43): a NON-ZERO numeric literal VALUE for a numeric-edited
        // item is a COBOL-2023 capability — below 2023 a numeric-edited VALUE required an alphanumeric edited-image
        // literal. SR6 exempts "the integer and decimal forms of the literal zero" (and the figurative ZERO — VCR
        // 35) at ALL editions, so only a non-zero numeric literal is gated. Scoped to the ITEM VALUE (not level-88).
        if (rawValue is { } nrv && pic is { Category: PicCategory.NumericEdited } && IsNonZeroNumericLiteral(nrv))
            ConstructRegistry.Check(Edition.Edition, Edition.Sink,
                Constructs.ValueNumericLiteralNumericEdited2023, entryWhere);
        // VCR 34 (ISO §13.18.63 SR4/SR5; Annex E.2 item 27): at >=2023 an alphanumeric edited-image literal VALUE on
        // a numeric-edited item is checked against the PICTURE size — a literal LONGER than the edited width is
        // rejected (below 2023 it was stored truncated — the "unclear value"). Under --permissive the check
        // downgrades to a warning (a removed-capability posture). The national-class-mismatch leg is already
        // COBOLNET0898 (ValidateValueCategory); only a plain alphanumeric literal (leading '"') reaches this size check.
        if (rawValue is { } arv && pic is { Category: PicCategory.NumericEdited } && Edition.DialectLevel >= 2023
            && arv.StartsWith('"') && CobolLiteral.Decode(arv).Length > pic.Length)
            Edition.Sink.Report(new EditionDiagnostic("COBOLNET1570",
                EditionSeverityPolicy.For(ConstructAvailability.Removed, Edition.Edition), "value-numeric-edited-oversize",
                $"{entryWhere}: the VALUE literal ({CobolLiteral.Decode(arv).Length} characters) exceeds the "
                + $"numeric-edited item's {pic.Length}-character edited size (ISO §13.18.63 SR4/SR5; COBOL-2023, "
                + "Annex E.2 item 27)", entryWhere, "ISO §13.18.63 SR4/SR5; Annex E.2 item 27"));
        // An EXTERNAL type declaration (ISO §13.18.22 SR1 — EXTERNAL is legal on a level-1 type declaration;
        // the level-1 shape is §13.18.58.3 SR3, already enforced by RegisterTypeDecl's 1529). The declaration
        // itself has no storage (§13.18.58.4 GR2); the effect lands on its REFERENCES — §13.18.22 GR2 (a data
        // description containing the type shall be level-1) and GR3 (those records are themselves EXTERNAL) are
        // enforced in ExpandType, and the record rides the ordinary run-unit ExternalStore re-basing
        // (CallBindExternalAndGlobal). The former COBOLNET1534 stage is LIFTED (P10 Step 16).
        // (Its old §13.18.57.4 GR5 citation was wrong — GR5 is a Format-2 REPORT-GROUP rule.)

        // SAME AS same-entry composition (ISO §13.16.3 SR12): the SAME AS clause shall not share a data
        // description entry with any clause except CONSTANT RECORD, entry-name, EXTERNAL, GLOBAL, level-number,
        // and OCCURS. A violation reports and clears the reference so the item binds as ordinary storage under
        // an already-failed compile (the IsBased discipline).
        if (sameAsName is not null
            && (pictureText is not null || usageText is not null || rawValue is not null || ownSign is not null
                || justified || blankWhenZero || synchronized || redefinesTargetName is not null || isBased
                || isAnyLength || typeRefName is not null || isTypedef))
        {
            Edition.Error(DiagnosticCatalog.SameAsEntryRule, $"{entryWhere}: the SAME AS clause shall not be "
                + "specified in the same data description entry with any clauses except CONSTANT RECORD, "
                + "entry-name, EXTERNAL, GLOBAL, level-number, and OCCURS (ISO §13.16.3 SR12)");
            sameAsName = null;
        }

        // CONSTANT RECORD same-entry shape checks (P10 Step 15; the IsBased discipline — a violation reports and
        // clears the flag so the item binds as ordinary storage under an already-failed compile). §13.16.3 SR6:
        // level-01 entries only; SR3: REDEFINES excluded; SR13: ANY LENGTH / BASED / BLANK WHEN ZERO /
        // SYNCHRONIZED / TYPEDEF excluded same-entry (the subordinate-entry half of SR13 checks in BindEntries,
        // where the parent chain exists); SR13 ¶2: with EXTERNAL, a strongly-typed TYPE clause is required.
        if (isConstantRecord)
        {
            string? crViolation =
                level != 1
                    ? "the CONSTANT RECORD clause may be specified only in a data description entry whose "
                      + "level-number is 1 (ISO §13.16.3 SR6)"
                : redefinesTargetName is not null
                    ? "REDEFINES shall not be specified in the same data description entry as the CONSTANT "
                      + "RECORD clause (ISO §13.16.3 SR3)"
                : isAnyLength || isBased || blankWhenZero || synchronized || isTypedef
                    ? "the ANY LENGTH, BASED, BLANK WHEN ZERO, SYNCHRONIZED, and TYPEDEF clauses shall not be "
                      + "specified in the same data description entry as the CONSTANT RECORD clause "
                      + "(ISO §13.16.3 SR13)"
                // VCR 16 (ISO §13.16.3 SR13 ¶2; Annex E.2 item 10): the "EXTERNAL CONSTANT RECORD requires a strong
                // TYPE" requirement is a COBOL-2023 addition — below 2023 the bare external constant record (no TYPE)
                // was the legacy accepted form (its content initializes per §13.18.15.4 GR1 and is not re-initialized
                // on run-unit re-entry, §11.9.10.4 GR7 / §14.6.2.3.3). Version-conditioned structural SR ⇒ read DialectLevel directly (the
                // CheckDigitCapacity/binder-reads-edition doctrine), NOT a ConstructRegistry introduction gate.
                : hasExternal && typeRefName is null && Edition.DialectLevel >= 2023
                    ? "a CONSTANT RECORD clause specified with the EXTERNAL clause requires a TYPE clause "
                      + "naming a strongly typed definition (ISO §13.16.3 SR13 ¶2; ≥2023)"
                : null;
            if (crViolation is not null)
            {
                Edition.Error(DiagnosticCatalog.ConstantRecordRule, $"{entryWhere}: {crViolation}");
                isConstantRecord = false;
            }
        }
        var item = new DataItem
        {
            Level = level,
            CobolName = isFiller ? null : cobolName,
            CsName = csName,
            Pic = pic,
            Pending = pending,
            OwnSign = ownSign,
            OwnUsage = usageText is not null ? entryUsage : null,
            RawValue = rawValue,
            Occurs = occurs,
            OccursSpec = occursSpec,
            RedefinesTargetName = redefinesTargetName,
            Justified = justified,
            BlankWhenZero = blankWhenZero,
            Synchronized = synchronized,
            IsTypedef = isTypedef,
            TypedefStrong = typedefStrong,
            IsExternalTypedef = isTypedef && hasExternal,   // §13.18.22 SR1 / §13.18.58.3 SR3 (P10 Step 16)
            HasExternalClause = hasExternal,                // backs the §13.18.22 SR5 strong-external pairing check
            IsConstantRecord = isConstantRecord,
            TypeRefName = typeRefName,
            SameAsName = sameAsName,
        };
        if (sameAsName is not null) item.SameAsQualifiers.AddRange(sameAsQuals);

        // BASED declaration validation (the 0881 declaration-entry band; Phase-4b increment 2): §13.16 SR16 —
        // a BASED entry is a level-01/77 record-description entry (WS/LS/LINKAGE; the file-subsystem sweep is
        // a named residue); §13.18.5 SRs — REDEFINES and BASED are mutually exclusive (:17215) and a VALUE
        // clause cannot seed storage the item does not own. Violations clear the flag so the item binds as
        // ordinary storage under an already-failed compile (never a half-based state).
        if (isBased)
        {
            if (level is not (1 or 77))
            {
                Edition.Error("COBOLNET0881", $"{entryWhere}: the BASED clause may be specified only in a "
                    + "level-01 or level-77 entry (ISO §13.16 SR16 / §13.18.5)");
                isBased = false;
            }
            else if (redefinesTargetName is not null)
            {
                Edition.Error("COBOLNET0881", $"{entryWhere}: BASED and REDEFINES may not be specified "
                    + "together (ISO §13.18.5 SR)");
                isBased = false;
            }
            else if (hasExternal)
            {
                // §13.16.3 SR5: "The EXTERNAL clause shall not be specified in the same data description
                // entry as the REDEFINES or BASED clause" — without this, BOTH mechanisms would emit a
                // bridge under the ONE BackingCsName (a CS0102 duplicate member, the review finding).
                Edition.Error("COBOLNET0881", $"{entryWhere}: BASED and EXTERNAL may not be specified "
                    + "together (ISO §13.16.3 SR5)");
                isBased = false;
            }
            // A VALUE clause on a BASED entry is LEGAL (its data seeds ALLOCATE … INITIALIZED per §14.9.3
            // GR7's TO-VALUE leg); without INITIALIZED the allocated content is undefined (GR8), so the
            // space-filled cell is conformant — the clause simply has no stored field to seed here.
        }
        item.IsBased = isBased;

        // ANY LENGTH declaration-shape validation (ISO §13.18.2; the COBOLNET1542 SR band — the 08xx declaration
        // band is exhausted, the 1521 precedent). Violations clear the flag so the item binds as ordinary storage
        // under an already-failed compile (the IsBased discipline). The SR2/SR3/SR4 PLACEMENT rules (linkage-only,
        // elementary, unit kind, formal/RETURNING reference) need unit-level facts and live in the post-bind
        // sweeps: AnyLengthValidateUnit (program/function/object paths) and OoBindMethodData (methods).
        if (isAnyLength)
        {
            // §13.18.2.3 SR1: a PICTURE clause shall be specified, and its character-string shall be ONE
            // instance of the picture symbol 'N', 'X', or '1' (a "(1)" repetition count writes the same one
            // instance). Checked on the WRITTEN character-string — category+length cannot distinguish 'A'.
            string norm = pictureText?.Trim().ToUpperInvariant() ?? "";
            if (norm is not ("X" or "N" or "1" or "X(1)" or "N(1)" or "1(1)"))
            {
                Edition.Error("COBOLNET1542", $"{entryWhere}: the ANY LENGTH clause requires a PICTURE whose "
                    + "character-string is exactly one instance of the picture symbol 'N', 'X', or '1' "
                    + $"(ISO §13.18.2.3 SR1{(pictureText is null ? "; no PICTURE clause is specified" : $"; PICTURE {pictureText}")})");
                isAnyLength = false;
            }
            // §13.16.3 SR17: with ANY LENGTH the only other clauses permitted are level-number, entry-name,
            // PICTURE, USAGE, and VALUE — reject every other decoded clause loud, never a half-shaped item.
            else if (occursSpec is not null || occurs is not null || redefinesTargetName is not null || isBased
                || hasExternal || justified || blankWhenZero || synchronized || ownSign is not null
                || isTypedef || typeRefName is not null)
            {
                Edition.Error("COBOLNET1542", $"{entryWhere}: with the ANY LENGTH clause the only other clauses "
                    + "permitted are level-number, entry-name, PICTURE, USAGE, and VALUE (ISO §13.16.3 SR17)");
                isAnyLength = false;
            }
            // §13.18.2.3 SR2 (the level half — checkable right here): an elementary LEVEL 1 entry only
            // (a 77 item is not a level-1 entry; the elementary/section/unit halves are the sweeps' job).
            else if (level is not 1)
            {
                Edition.Error("COBOLNET1542", $"{entryWhere}: the ANY LENGTH clause may be specified only in an "
                    + "elementary level 1 entry in the linkage section (ISO §13.18.2.3 SR2)");
                isAnyLength = false;
            }
        }
        item.IsAnyLength = isAnyLength;

        // DYNAMIC LENGTH declaration-shape validation (ISO §8.5.1.10 / §13.18.19; COBOL-2014). Violations clear the
        // flag so the item binds as ordinary storage under an already-failed compile (the ANY LENGTH / IsBased
        // discipline). The COBOL-2014 introduction gate is VersionConformancePass ParseArm.VisitDynamicLengthClause.
        if (isDynamicLength)
        {
            // §13.18.19.3 SR1: a PICTURE clause shall be specified and its character-string shall be exactly ONE
            // instance of the picture symbol 'N' or 'X'. A count of 1 in ANY spelling is still one instance —
            // `X`, `X(1)`, `X(01)`, `N(001)` all denote one position (the regex `^[XN](\(0*1\))?$` matches every
            // count-1 form and rejects `XX`, `X(2)`, `A`, `9`, editing symbols, …). Unlike ANY LENGTH (§13.18.2.3
            // SR1), the boolean symbol '1' is NOT permitted — a dynamic-length item is alphanumeric or national only
            // (§13.18.19.4 GR1).
            string norm = pictureText?.Trim().ToUpperInvariant() ?? "";
            if (!System.Text.RegularExpressions.Regex.IsMatch(norm, @"^[XN](\(0*1\))?$"))
            {
                Edition.Error("COBOLNET1561", $"{entryWhere}: the DYNAMIC LENGTH clause requires a PICTURE whose "
                    + "character-string is exactly one instance of the picture symbol 'N' or 'X' "
                    + $"(ISO §13.18.19.3 SR1{(pictureText is null ? "; no PICTURE clause is specified" : $"; PICTURE {pictureText}")})");
                isDynamicLength = false;
            }
            // §13.18.19.3 SR2/SR3: a dynamic-length-structure-name refers to a SPECIAL-NAMES DYNAMIC LENGTH
            // STRUCTURE (§12.3.7 — PREFIXED/DELIMITED/physical layout). COBOL.NET does not yet support the physical
            // structure declaration, so a naming reference is rejected LOUD rather than silently defaulting the
            // layout (a staged residue; the 2023 SET-length enhancement, VCR row 60, is separately P13).
            else if (dynLengthStructureName is not null)
            {
                Edition.Error("COBOLNET1562", $"{entryWhere}: a DYNAMIC LENGTH clause naming a "
                    + $"dynamic-length-structure-name ('{dynLengthStructureName}') is not yet supported "
                    + "(ISO §13.18.19.3 SR2 / §12.3.7 DYNAMIC LENGTH STRUCTURE)");
                isDynamicLength = false;
            }
            // §13.16.3 SR18: with DYNAMIC LENGTH the ONLY other clauses permitted are level-number, entry-name,
            // PICTURE, USAGE, and VALUE — reject every other decoded clause loud, never a half-shaped item. GLOBAL
            // (§13.18.27) and PROPERTY (§13.18.42) are decoded here for this check (GLOBAL otherwise binds post-build
            // in CallBindExternalAndGlobal, so it would escape the allowlist without an explicit flag).
            else if (occursSpec is not null || occurs is not null || redefinesTargetName is not null || isBased
                || hasExternal || hasGlobal || hasProperty || justified || blankWhenZero || synchronized || ownSign is not null
                || isTypedef || typeRefName is not null || sameAsName is not null || isConstantRecord || isAnyLength)
            {
                Edition.Error("COBOLNET1563", $"{entryWhere}: with the DYNAMIC LENGTH clause the only other clauses "
                    + "permitted are level-number, entry-name, PICTURE, USAGE, and VALUE (ISO §13.16.3 SR18)");
                isDynamicLength = false;
            }
        }
        item.IsDynamicLength = isDynamicLength;
        if (isDynamicLength) item.DynLengthLimit = dynLengthLimit;

        // Register each INDEXED BY index-name as a distinct C# long field (1-based occurrence number, §3.5).
        // A method's index-names (M2-OO-1h step 4) register into the METHOD's own scope with a FRESH cell — two
        // methods' IX, or a method IX shadowing an object IX, get distinct cells (§11.7.4 GR5); the program/object
        // path keeps the de-dup dict.
        foreach (var idxName in indexNames)
        {
            item.IndexNames.Add(idxName);
            if (_bindingMethodScope is { } ms)
                ms.IndexFields[idxName] = "_MIX_" + _ixSeq++;
            else if (!IndexFields.ContainsKey(idxName))
                _indexFields[idxName] = "_IX_" + _indexFields.Count;
        }
        return item;
    }

    /// <summary>Extract a usage clause's canonical keyword text by TOKEN inspection — never string-stripping.
    /// The full form (<c>USAGE [IS] usageKeyword [binarySign]</c>) carries the keyword in its
    /// <c>usageKeyword</c> child; a bare-keyword alternative (the USAGE word is optional, ISO §13.18.60 general
    /// format) carries the keyword TERMINAL as the clause's FIRST child with an optional <c>binarySign</c>
    /// sibling (SIGNED/UNSIGNED on the BINARY-CHAR/-SHORT/-LONG/-DOUBLE family). The historical
    /// <c>GetText()</c>-and-strip fallback GLUED the sign phrase into the keyword (bare <c>BINARY-CHAR
    /// SIGNED</c> → <c>"BINARY-CHARSIGNED"</c>, which then silently misbound to DISPLAY — the W2 loud-guard
    /// sweep), and even bare <c>DISPLAY</c> survived it only by accident. <c>USAGE OBJECT REFERENCE</c> (a rule,
    /// not one terminal) canonicalizes to <c>"OBJECT REFERENCE"</c> — its class-name operand is not part of the
    /// keyword.</summary>
    private static string UsageKeyword(Core.UsageClauseContext usage)
    {
        if (usage.usageKeyword() is { } kw)
            return kw.objectReferenceUsage() is not null ? "OBJECT REFERENCE"
                // The pointer-to-prototype usages are RULES with an optional TO tail (§13.18.60) — GetText()
                // would glue the prototype name into the keyword (the OBJECT REFERENCE precedent).
                : kw.programPointerUsage() is not null ? "PROGRAM-POINTER"
                : kw.functionPointerUsage() is not null ? "FUNCTION-POINTER"
                : kw.GetText();
        return usage.GetChild(0).GetText();
    }

    /// <summary>Extract the first VALUE operand's raw source text (literal or figurative constant). THRU ranges /
    /// 88-levels are later. The emitter (<c>FieldEmitter</c>) interprets the text — including figurative constants
    /// such as ZERO/SPACE — against the item's category and width. A numeric literal is normalized to the
    /// canonical dot-decimal form (DECIMAL-POINT IS COMMA, ISO §12.3.7 GR14a).</summary>
    private string? ExtractValue(Core.ValueClauseContext value)
    {
        var item = value.valueItem().FirstOrDefault();
        if (item is null) return null;
        // §8.8.3.3 GR3: a concatenation-expression VALUE operand folds to its equivalent single literal's
        // RAW text (GetText would glue the operand tokens — `"AB" & "CD"` → `"AB"&"CD"` — and the emitter's
        // decode would then mis-read the value). The fold happens HERE, once, so the whole raw-text VALUE
        // pipeline (ValidateValueCategory, FieldEmitter, ValueInitializer) sees an ordinary literal. A
        // constant-name operand substitutes its literal the same way (ISO §13.10.3 SR2 / §13.10.4 GR1 — "as if
        // [the] literal were written"; DataBinder.Constants.ConstantValueRawText).
        if (item.valueClauseOperand().FirstOrDefault() is { } op0
            && (op0.nonNumericLiteral()?.concatenationExpression() is not null
                || ConstantValueRawText(op0) is not null))
            return RawValueOperandText(op0);
        return item.GetText() is { } raw ? NormalizeIfNumericLiteral(raw) : null;
    }

    /// <summary>The RAW single-literal text of a VALUE operand — the data path's currency (decoded at emit
    /// time): a §8.8.3 concatenation expression folds to its equivalent literal's raw text (§8.8.3.3 GR3); a
    /// constant-name substitutes its literal's raw text (§13.10.3 SR2 / §13.10.4 GR1 — a VALUE operand is a
    /// literal position); any other operand keeps its source text, numeric literals normalized to dot-decimal
    /// (ISO §12.3.7 GR14a).</summary>
    private string RawValueOperandText(Core.ValueClauseOperandContext op) =>
        op.nonNumericLiteral()?.concatenationExpression() is { } ce
            ? ConcatFolder.Fold(ce, Edition, Collating, NationalCollating).RawText
            : ConstantValueRawText(op) is { } konst ? konst
            : NormalizeIfNumericLiteral(op.GetText());

    /// <summary>THE usage-inheritance pass (P5.11e, DESIGN-data-model §2.7 — the former
    /// <c>ResolveIndexItems</c> + <c>InheritUsageClauses</c> pipeline pair MERGED; both effects, same order): the
    /// two halves are one §13.18.60 GR1 job — resolve the PICTURE-less usage MARKERS once the forest is complete
    /// (index/object-reference shedding, the NATIONAL/BIT <see cref="DataItem.Pending"/> adjudication), then
    /// apply group-level USAGE clauses to subordinate elementary items.</summary>
    internal void UsageInheritancePass()
    {
        ResolveIndexItems();
        InheritUsageClauses();
    }

    /// <summary>Resolve PICTURE-less USAGE INDEX entries (ISO §13.18.60) once the forest is complete — entry bind
    /// synthesized an elementary index profile (<see cref="PicInfo.IndexItem"/>) before subordinates were known. An
    /// entry WITH subordinates is a GROUP whose USAGE INDEX merely inherits (GR1 — usage on a group applies to each
    /// elementary item under it): clear the synthesized profile; a PICTURE-less LEAF below it is an index data item
    /// even without its own USAGE clause. (A half of <see cref="UsageInheritancePass"/>, P5.11e.)</summary>
    private void ResolveIndexItems()
    {
        // Every elementary leaf under a group (for the NATIONAL/BIT group-usage conformance check below).
        static IEnumerable<DataItem> Leaves(DataItem g)
        {
            foreach (var c in g.Children)
                if (c.Children.Count > 0) foreach (var l in Leaves(c)) yield return l;
                else yield return c;
        }

        void Walk(DataItem item, bool inherited, PicInfo? inheritedObjRef)
        {
            bool isIndex = ReferenceEquals(item.Pic, PicInfo.IndexItem) || (inherited && item.Pic is null);
            // USAGE OBJECT REFERENCE inherits the same way (§13.18.60.4 GR1): a group header sheds its
            // synthesized reference profile; a PICTURE-less leaf below takes it (sharing the immutable
            // PicInfo — the declared class flows down with it).
            var objRef = item.Pic is { Category: PicCategory.ObjectReference } p ? p : inheritedObjRef;
            if (item.Children.Count > 0)
            {
                // SYNCHRONIZED on a GROUP item is a COBOL-2023 introduction (Annex E.3.2 item 6; before 2023 SYNC
                // is permitted only on elementary items). SYNC is a no-op in the typed-native model, so below 2023
                // it is REJECTED under strict but ACCEPTED-INERT under --permissive — the removed-severity seam
                // (Edition.Removed = error strict / warning permissive) gives exactly that, and keeps INV-1
                // continuity intact for any program carrying it (P3 step 10; owner-chosen disposition).
                if (item.Synchronized && Edition.DialectLevel < 2023)
                    Edition.Removed(EditionCodes.Introduction,
                        $"SYNCHRONIZED on a group item ('{item.CobolName ?? "FILLER"}') was introduced by ISO/IEC "
                        + "1989:2023 (Annex E.3.2 item 6; before 2023 SYNCHRONIZED is permitted only on elementary "
                        + $"items) - it requires --std 2023 or later (targeting COBOL-{Edition.DialectLevel})");
                if (ReferenceEquals(item.Pic, PicInfo.IndexItem)) item.Pic = null;   // a group, not an elementary index
                // USAGE NATIONAL / BIT on a GROUP header sheds per §13.18.60.4 GR1 — with the SR12/SR5
                // conformance check over the subordinate leaves (each leaf's own PICTURE has already
                // classified it): under NATIONAL a leaf must be national (fine), boolean/numeric (spec-legal
                // national FORMS — staged, the Analyze 0899 legs), never alphabetic/alphanumeric; under BIT
                // every leaf must be boolean (SR5). Adjudicated via the EXPLICIT Pending mark (P5.11c) — the
                // item's Pic was never a sentinel shape and is already null.
                if (item.Pending is PicPending.NationalUsage)
                {
                    item.Pending = PicPending.None;
                    foreach (var l in Leaves(item))
                        if (l.Pic is { Category: PicCategory.Boolean or PicCategory.Numeric or PicCategory.NumericEdited })
                            Edition.Error(DiagnosticCatalog.NationalData, "national-form data (a boolean or numeric item "
                                + $"under a group USAGE NATIONAL) is recognized but not yet implemented "
                                + $"(Phase 4a residue) — data item '{l.CobolName ?? "FILLER"}' "
                                + "(ISO §13.18.60.3 SR12 / §13.18.60.4 GR1)");
                        else if (l.Pic is not null and not { Category: PicCategory.National })
                            Edition.Error("COBOLNET0881", $"data item '{l.CobolName ?? "FILLER"}': USAGE "
                                + "NATIONAL inherited from its group admits boolean, national, "
                                + "national-edited, numeric, and numeric-edited pictures only "
                                + "(ISO §13.18.60.3 SR12 / §13.18.60.4 GR1; §13.18.40.3 SR30)");
                }
                if (item.Pending is PicPending.BitUsage)
                {
                    item.Pending = PicPending.None;
                    foreach (var l in Leaves(item))
                        if (l.Pic is not null and not { Category: PicCategory.Boolean })
                            Edition.Error("COBOLNET0881", $"data item '{l.CobolName ?? "FILLER"}': USAGE BIT "
                                + "inherited from its group requires a boolean PICTURE (symbol 1 only) "
                                + "(ISO §13.18.60.3 SR5 / §13.18.60.4 GR1)");
                }
                if (item.Pic is { Category: PicCategory.ObjectReference }) item.Pic = null;
                // A synthesized fixed-width binary profile on a GROUP header sheds the same way (the usage
                // merely inherits per §13.18.60.4 GR1). Group-level BINARY-* over PICTURE'd children is a spec
                // corner with no corpus surface (PICTURE is §13.16.3 SR8-illegal on the family) — left to a
                // later slice, mirroring the float-on-group deferral in InheritUsageClauses.
                if (item.Pic is { Category: PicCategory.Numeric, Usage: Usage.BinaryChar or Usage.BinaryShort
                        or Usage.BinaryLong or Usage.BinaryDouble }) item.Pic = null;
                foreach (var c in item.Children) Walk(c, isIndex, objRef);
            }
            else if (item.Pending is not PicPending.None)
            {
                // A PICTURE-less ELEMENTARY item may not carry USAGE NATIONAL/BIT — they are not among the
                // picture-less usages (§13.18.60.4; contrast INDEX/POINTER/OBJECT REFERENCE/BINARY-x). The
                // recovery shape keeps the doomed emit crash-free (the DEVLOG-597 pattern).
                Edition.Error("COBOLNET0881", $"data item '{item.CobolName ?? "FILLER"}': an elementary item "
                    + $"with USAGE {(item.Pending is PicPending.BitUsage ? "BIT" : "NATIONAL")} "
                    + "requires a PICTURE clause (ISO §13.18.60.4 — not a picture-less usage)");
                item.Pic = PicInfo.Recovery();
                item.Pending = PicPending.None;
            }
            else if (isIndex && item.Pic is null)
                item.Pic = PicInfo.IndexItem;
            else if (item.Pic is null && objRef is not null)
                item.Pic = objRef;
        }
        foreach (var root in Roots) Walk(root, false, null);
    }

    /// <summary>Apply group-level USAGE clauses to subordinate elementary items (ISO §13.18.60 GR1 — "the USAGE
    /// clause of a group item applies to each elementary item subordinate to it"; the nearest enclosing clause
    /// wins, an item's OWN clause outright). Scope: the binary/packed integer usages (NC107A's
    /// <c>01 U9 USAGE COMPUTATIONAL</c> with PICTURE-only children) — USAGE INDEX inheritance is
    /// <see cref="ResolveIndexItems"/>'s special case (PICTURE-less index items), and a float usage on a group
    /// with PICTUREd children has no NIST surface (left to the float slice). Runs BEFORE
    /// <see cref="InheritSignClauses"/> — a non-DISPLAY item takes the BinaryMinus sign form regardless of any
    /// inherited SIGN clause (§13.18.52 applies only to usage-display items). (A half of
    /// <see cref="UsageInheritancePass"/>, P5.11e.)</summary>
    private void InheritUsageClauses()
    {
        static void Walk(DataItem item, Usage? inherited)
        {
            Usage? effective = item.OwnUsage ?? inherited;
            if (item.OwnUsage is null
                && effective is Usage.Binary or Usage.Packed or Usage.Comp5
                && item.Pic is { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display } pic)
                item.Pic = pic with
                {
                    Usage = effective.Value,
                    SignKind = PicInfo.SignKindFor(effective.Value, pic.Signed, item.OwnSign),
                };
            foreach (var c in item.Children) Walk(c, effective);
        }
        foreach (var root in Roots) Walk(root, null);
    }

    /// <summary>Apply group-level SIGN clauses to subordinate signed numeric DISPLAY items (ISO §13.18.52 GR1–3):
    /// a SIGN on a group applies to every signed numeric item subordinate to it, the NEAREST enclosing clause takes
    /// precedence, and an item's OWN clause (already consumed by <see cref="PictureAnalyzer.Analyze"/> at entry bind) wins
    /// outright. Runs BEFORE the REDEFINES classification pass because a SEPARATE sign occupies its own character
    /// position (§13.18.52 GR6a) — it widens the item's image, which feeds the class-max width.</summary>
    internal void InheritSignClauses()
    {
        static void Walk(DataItem item, SignSpec? inherited)
        {
            SignSpec? effective = item.OwnSign ?? inherited;
            if (item.OwnSign is null && effective is not null
                && item.Pic is { Category: PicCategory.Numeric, Signed: true, Usage: Usage.Display } pic)
                item.Pic = pic with { SignKind = PicInfo.SignKindFor(pic.Usage, signed: true, effective) };
            foreach (var c in item.Children) Walk(c, effective);
        }
        foreach (var root in Roots) Walk(root, null);
    }

    // ── REDEFINES / RENAMES resolution + classification (post-build, ISO §13.18.44/45) ───────────────────────

    /// <summary>Resolve each item's REDEFINES target name to its <see cref="DataItem"/>, and each level-66 RENAMES
    /// FROM/THRU operand to its item. A REDEFINES target is an unqualified prior entry in the same scope (SR1/SR6); a
    /// RENAMES range names items within the owning record (SR3). Target resolution does not chase chains — the
    /// classification pass walks <see cref="DataItem.RedefinesTarget"/> transitively to the anchor (SR11).</summary>
    internal void ResolveRedefines()
    {
        static DataItem RootOf(DataItem d) { while (d.Parent is { } p) d = p; return d; }
        foreach (var item in AllItems())
            if (item.RedefinesTargetName is { } tname)
            {
                // A subordinate (02+) redefiner scopes to its own siblings (correct in every scope). A top-level
                // 01/77 method redefiner must scope to the OWNING METHOD's own roots (§13.18.44.3 SR — the target
                // is a prior sibling in the same data description; a method may NOT redefine object/program data,
                // §11.7.4), NEVER the cross-scope global `Roots` pool (M2-OO-1h step 3).
                IReadOnlyList<DataItem> scope;
                if (item.Parent is { } par)
                    scope = par.Children;
                else if (OoRootOwner.TryGetValue(RootOf(item), out var mm))
                    // A top-level method redefiner scopes to its OWN section only (§13.18.44.3 SR — the target is a
                    // preceding item in the SAME data description; cross-section WS↔LOCAL↔LINKAGE aliasing is illegal,
                    // and their storage classes differ [static WS vs per-activation LOCAL] — review B). RootOf(item)
                    // == item here (Parent is null).
                    scope = mm.Binding!.StaticRoots.Contains(item) ? mm.Binding!.StaticRoots
                          : mm.Binding!.LocalRoots.Contains(item) ? mm.Binding!.LocalRoots
                          : mm.Binding!.LinkageRoots;
                else
                    scope = Roots;
                item.RedefinesTarget = scope.FirstOrDefault(s =>
                    !ReferenceEquals(s, item) && string.Equals(s.CobolName, tname, StringComparison.OrdinalIgnoreCase));
                // A method 01 REDEFINES whose target isn't in the method's own roots is a scope error (never a
                // silent cross-scope bind to an object/program item) — §13.18.44.3 SR.
                if (item.RedefinesTarget is null && item.Parent is null && OoRootOwner.ContainsKey(RootOf(item)))
                    // 1577, renumbered from a bare "COBOLNET1518" that collided with the locale-module
                    // non-support meaning (review V11 — the code comes from the catalog descriptor, never a literal).
                    Edition.Error(DiagnosticCatalog.MethodRedefinesScope,
                        $"REDEFINES target '{tname}' of method data item "
                        + $"'{item.CobolName ?? "FILLER"}' is not a preceding item in the same method scope "
                        + "(ISO §13.18.44.3 — a method item may not redefine object or program data)");
                // §13.18.44.3 SR16: data-name-2 (the redefined item) shall not be described with the ANY
                // LENGTH clause — a runtime-length item has no fixed storage area a redefiner could overlay.
                if (item.RedefinesTarget is { IsAnyLength: true })
                    Edition.Error("COBOLNET1542", $"'{item.CobolName ?? "FILLER"}' REDEFINES "
                        + $"'{tname}': the redefined item is described with the ANY LENGTH clause "
                        + "(ISO §13.18.44.3 SR16 — data-name-2 shall not be ANY LENGTH)");
                // §13.18.44.3 SR8 (the P5.8 spec find): the SUBJECT's storage area shall not be larger than
                // data-name-2's, unless data-name-2 is a level-1 item (without the EXTERNAL clause — that
                // residue is unmodeled; a level-1 FILE SECTION entry cannot carry REDEFINES at all, SR3, so
                // the exception is a WORKING-STORAGE/LOCAL/LINKAGE 01). Previously accepted SILENTLY — the
                // classifier took the class-max width, giving the overlay byte-position semantics NO edition
                // defines. The extent is the full OCCURS allocation (the "storage area required").
                if (item.RedefinesTarget is { } tgt && tgt.Level != 1
                    && item.ImageWidth * (item.Occurs ?? 1) > tgt.ImageWidth * (tgt.Occurs ?? 1))
                    Edition.Error("COBOLNET1539", $"'{item.CobolName ?? "FILLER"}' REDEFINES "
                        + $"'{tgt.CobolName ?? "FILLER"}': the redefining storage area "
                        + $"({item.ImageWidth * (item.Occurs ?? 1)} characters) is larger than the redefined "
                        + $"({tgt.ImageWidth * (tgt.Occurs ?? 1)}) — permitted only when the redefined item is "
                        + "level 1 (ISO §13.18.44.3 SR8)");
            }

        foreach (var root in Roots)
            foreach (var ren in root.Renames66)
            {
                var info = ren.Renames!;
                info.From = FindDescendantOrSelf(root, info.FromName);
                info.Thru = info.ThruName is { } t ? FindDescendantOrSelf(root, t) : null;
                if (info.From is null || (info.ThruName is not null && info.Thru is null)) continue;
                // The no-THRU alias inherits the renamed item's description (§13.18.45 GR1) — the resolver
                // forwards to the FROM item's place; no span, no synthetic alphanumeric picture.
                if (info.Thru is null) { ren.Pic = info.From.Pic; continue; }

                // The alias spans the record's contiguous leaf run FROM..THRU (§13.18.45 GR1/GR2); the alias item
                // itself reads/writes as one elementary ALPHANUMERIC item of the span's width (its category per
                // GR — a re-grouping, always treated as an alphanumeric data item when referenced as a whole).
                var leaves = new List<DataItem>();
                void Walk(DataItem n) { if (n.IsElementary) leaves.Add(n); else foreach (var c in n.Children) Walk(c); }
                Walk(root);
                int start = leaves.FindIndex(l => ReferenceEquals(l, info.From) || IsUnder(l, info.From));
                DataItem last = info.Thru ?? info.From;
                int end = leaves.FindLastIndex(l => ReferenceEquals(l, last) || IsUnder(l, last));
                if (start < 0 || end < start) continue;
                info.SpanLeaves.AddRange(leaves[start..(end + 1)]);
                ren.Pic = new PicInfo(PicCategory.Alphanumeric, Usage.Display,
                    Length: info.SpanLeaves.Sum(l => l.ImageWidth * (l.Occurs ?? 1)), Digits: 0, Scale: 0, Signed: false);
            }

        static bool IsUnder(DataItem leaf, DataItem ancestor)
        {
            for (DataItem? n = leaf; n is not null; n = n.Parent)
                if (ReferenceEquals(n, ancestor)) return true;
            return false;
        }
    }

    /// <summary>Group every redefining entry with the non-redefining anchor it ultimately overlays (SR7/SR11) into a
    /// <see cref="RedefinesClass"/>, mark the anchor canonical and every other member a view, then assign the class a
    /// tier (D &gt; C &gt; B &gt; A) and its class-max width, and propagate view-suppression to each view's
    /// subordinates (SR9 — no VALUE on a subordinate of a redefiner). (COBOLNET_DESIGN §4.2.)</summary>
    internal void ClassifyRedefinesClasses()
    {
        var byAnchor = new Dictionary<DataItem, RedefinesClass>();
        foreach (var item in AllItems())
        {
            if (item.RedefinesTarget is null) continue;
            DataItem anchor = item;
            while (anchor.RedefinesTarget is { } t) anchor = t;     // chase the chain to the original (SR11)
            if (!byAnchor.TryGetValue(anchor, out var cls))
            {
                cls = new RedefinesClass { Canonical = anchor };
                cls.Members.Add(anchor);
                anchor.Class = cls;
                byAnchor[anchor] = cls;
            }
            cls.Members.Add(item);
            item.Class = cls;
            item.IsCanonical = false;
        }

        // ONE shared area per overlay nest (ISO §13.18.44 — a REDEFINES nested under another class's member
        // shares THAT storage): a class whose ANCHOR lies inside another class's subtree — its ancestor chain
        // crosses a redefining member or another class's anchor (NC252A's `RDEF8 REDEFINES RDF8` under the view
        // `REDEF11 REDEFINES REDEF10`) — DISSOLVES into the outer class. The outer subtree walk below assigns
        // every nested item its window over the one backing (an inner redefiner starts at its target's
        // already-assigned offset), so a dissolved class needs no members of its own; keeping it would let its
        // later walk re-claim the subtree and emit a backing inside a suppressed view struct (CS0103).
        foreach (var (anchor, cls) in byAnchor.ToList())
            for (var a = anchor.Parent; a is not null; a = a.Parent)
                if (a.RedefinesTargetName is not null || (byAnchor.ContainsKey(a) && !ReferenceEquals(byAnchor[a], cls)))
                {
                    byAnchor.Remove(anchor);
                    break;
                }

        foreach (var cls in byAnchor.Values)
        {
            var tier = ComputeTier(cls, out string? reject);
            // §13.18.44 SR5 (:21497): the redefined item shall not contain an OCCURS clause; and a dynamic-capacity
            // table is OUT-OF-LINE storage (a CobolDynTable, data-model D9) that can neither overlay nor be
            // overlaid. Reject a class whose canonical OR any redefining member is, or contains, a dynamic table.
            if (cls.Members.Any(ContainsDynamicTable))
            {
                Edition.Error("COBOLNET1525", $"REDEFINES involving the dynamic-capacity table in "
                    + $"'{cls.Canonical.CobolName ?? cls.Canonical.CsName}': a dynamic-capacity table is out-of-line "
                    + "storage and shall be neither the subject nor the object of a REDEFINES (ISO §13.18.44 SR5)");
                tier = RedefinesTier.Rejected;
                reject ??= "REDEFINES of/over a dynamic-capacity table (§13.18.44 SR5, D9)";
            }
            // The width is a member table's FULL extent (every occurrence). ONE verdict application (P5.11d).
            cls.Classify(tier, cls.Members.Max(m => m.ImageWidth * (m.Occurs ?? 1)), reject);
            // Each top-level member overlays the area from its start (a REDEFINES begins at the target's first
            // position, SR10); a subordinate accumulates its window offset within the member. Subordinates of any
            // member are themselves views (suppressed field, SR9).
            foreach (var member in cls.Members)
                AssignClassOffsets(member, 0, cls);
            // A Tier-B (string-canonical) numeric-DISPLAY view reads/writes its window through the character pipeline
            // (CobolNum.ParseDisplay / FormatDisplay) — the same image path used for whole-group numeric leaves.
            if (cls.Tier == RedefinesTier.StringCanonical)
                foreach (var leaf in cls.Members.SelectMany(LeavesOf))
                {
                    if (leaf.Pic is { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display })
                        MarkImageForced(leaf);   // the collected image fact (a Ptr-FORCED StringCanonical class deliberately never records — its display leaves stay TierBWindow)
                    else if (leaf.Pic is { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Binary or Usage.Packed } bp)
                    {
                        // A fixed-point BINARY/PACKED leaf of a Tier-B class is image-stored too: its window over
                        // the one string backing IS its zoned digit image (ISO §13.18.60 USAGE GR4 — implementor
                        // representation; COBOLNET_DESIGN §14.4). Its profile MUST be rewritten to describe that
                        // zoned storage: every accessor (EmitArithAssign stores, NumericRenderer reads, the
                        // window splice) threads `_P_`, and the leaf's declared BinaryMinus form is VARIABLE
                        // width (a leading '-' only when negative) — FormatDisplay(BinaryMinus) would write a
                        // Digits+1 image into the Digits-wide window and corrupt the value (and every following
                        // leaf). NOTE the observable consequence: DISPLAY of such a leaf shows the zoned
                        // overpunch image (like any signed zoned item), not the '-100' binary-minus form — the
                        // conformant face of the GR4 license. The DigitCount truncation discipline is unchanged
                        // (BINARY truncates by digit count; PACKED's 2n−1 over-capacity digits cannot survive an
                        // image round trip — standard stores never create them without implementor permission).
                        MarkImageForced(leaf);   // the collected image fact (the SignKind rewrite below is a Pic fact and stays)
                        leaf.Pic = bp with { SignKind = bp.ImageSignKind };
                    }
                }
        }
    }

    /// <summary>Assign each item in a redefines class its window offset within the class image and its class link; a
    /// top-level member starts at <paramref name="off"/> (0), a subordinate accumulates by preceding-sibling FULL
    /// extents (per-occurrence image width × OCCURS count — every occurrence is part of the layout). A subordinate
    /// that itself REDEFINES a prior sibling takes the TARGET's offset (redefinition begins at the redefined item's
    /// first position, ISO §13.18.44 GR1) and contributes NO width of its own. Every subordinate of a class member
    /// is itself a view (its stored field is suppressed — SR9).</summary>
    private static void AssignClassOffsets(DataItem item, int off, RedefinesClass cls)
    {
        item.ClassOffset = off;
        item.Class = cls;
        int childOff = off;
        foreach (var c in item.Children)
        {
            c.IsCanonical = false;
            // The inner-REDEFINES target is a PRIOR sibling, so its offset is already assigned this walk.
            int cOff = c.RedefinesTarget is { } target ? target.ClassOffset : childOff;
            AssignClassOffsets(c, cOff, cls);
            if (c.RedefinesTarget is null) childOff += c.ImageWidth * (c.Occurs ?? 1);
        }
    }

    /// <summary>Assign a redefines class its tier (COBOLNET_DESIGN §4.2 cascade D &gt; C &gt; B &gt; A). Tier C (the
    /// confined byte[] codec for a genuine mixed-USAGE pun) is not yet implemented, so a class that would be Tier C is
    /// loudly rejected in the interim — a conformant diagnostic on a legal-but-unimplemented construct.</summary>
    /// <summary>True if <paramref name="d"/> is, or has anywhere beneath it, an OCCURS DYNAMIC table (data-model
    /// D9) — the REDEFINES 1525 guard (a dynamic table is out-of-line and cannot participate in a shared area).</summary>
    private static bool ContainsDynamicTable(DataItem d) =>
        d.IsDynamicTable || d.Children.Any(ContainsDynamicTable);

    private static RedefinesTier ComputeTier(RedefinesClass cls, out string? reject)
    {
        reject = null;
        var leaves = cls.Members.SelectMany(LeavesOf).ToList();

        // Tier C → Rejected (interim): any leaf is float (COMP-1/2 — no fixed decimal-digit width), COMP-5 (its
        // BinaryCapacity discipline stores values EXCEEDING the PICTURE digit count — a Digits-wide character
        // window cannot carry them), or INDEX (no character image, §13.18.60). A DISPLAY + BINARY/PACKED mix is
        // Tier B: under the digit-image representation (ISO §13.18.60 USAGE GR4 — the representation, including
        // the sign, is implementor-defined; COBOLNET_DESIGN §4.2/§14.4) one string backing IS the shared area —
        // exactly §12.4.6.4.4 SAME RECORD AREA GR2's "equivalent to an implicit redefinition of the area, with
        // records aligned on the leftmost byte position". Its binary/packed leaves become zoned windows (the
        // StoreAsImage loop in ClassifyRedefinesClasses). (No pointer/object/strongly-typed items exist in the
        // bound model yet → no Tier-D check.)
        if (leaves.Any(l => l.Pic is { } p && (p.IsFloat
            || p.Usage is Usage.Comp5 or Usage.Index
                or Usage.BinaryChar or Usage.BinaryShort or Usage.BinaryLong or Usage.BinaryDouble)))
        {
            reject = $"float/COMP-5/BINARY-*/INDEX REDEFINES of '{cls.Canonical.CobolName}' (Tier-C byte path) not yet implemented";
            return RedefinesTier.Rejected;
        }

        // A NATIONAL leaf: §13.18.44 lays the shared area in BYTES, and the documented 2-byte national
        // character (D-N1/D-N2) has no char-window overlay over the single-byte members — recognized, staged
        // loud (Phase 4a residue: per-item byte offsets + UTF-16LE class images). BOOLEAN leaves fall through
        // legitimately (one '0'/'1' char = one byte, D-B1).
        if (leaves.Any(l => l.Pic is { Category: PicCategory.National }))
        {
            reject = $"REDEFINES over national data in '{cls.Canonical.CobolName}' (the 2-byte national "
                + "character has no single-byte char-window overlay) not yet implemented (Phase 4a residue)";
            return RedefinesTier.Rejected;
        }

        // Tier A — every member is an elementary item sharing the canonical's CLR storage type AND its image width:
        // one stored field, the rest pass-throughs (a numeric view reinterprets the shared value via its own scale).
        DataItem canon = cls.Canonical;
        bool allAlias = canon.IsElementary && cls.Members.All(m =>
            m.IsElementary && m.ElementType == canon.ElementType && m.ImageWidth == canon.ImageWidth);
        if (allAlias) return RedefinesTier.Alias;

        // Tier B — DISPLAY-homogeneous: one string canonical of class-max width, each view an (offset,width) accessor.
        return RedefinesTier.StringCanonical;
    }

    /// <summary>The D-N2 byte-surface gate for FILE records: the record codec reads/writes single-byte
    /// characters (Latin-1, <c>SequentialFile</c>), and a national leaf occupies TWO bytes per position under
    /// the documented D-N1 representation — a national leaf in an FD/SD record would silently halve its
    /// positions on disk. Recognized, staged loud (Phase 4a residue: the 2-byte national record layout).
    /// Boolean leaves flow — one '0'/'1' character IS one byte (D-B1).</summary>
    internal void GateNationalRecords()
    {
        foreach (var f in Files)
            foreach (var rec in f.Records)
                foreach (var leaf in LeavesOf(rec))
                    if (leaf.Pic is { Category: PicCategory.National })
                        Edition.Error(DiagnosticCatalog.NationalData, $"national data in a file record (data item "
                            + $"'{leaf.CobolName ?? "FILLER"}' of record '{rec.CobolName}') is recognized but "
                            + "not yet implemented — the record codec is single-byte and the national "
                            + "character is two bytes (Phase 4a residue; ISO §8.1.2 / §13.18.60.4 GR8)");
    }

    /// <summary>Every item in the WORKING-STORAGE forest, in declaration (pre-order DFS) order.</summary>
    private IEnumerable<DataItem> AllItems()
    {
        static IEnumerable<DataItem> Walk(DataItem d)
        {
            yield return d;
            foreach (var c in d.Children)
                foreach (var x in Walk(c)) yield return x;
        }
        return Roots.SelectMany(Walk);
    }

    /// <summary>
    /// Every SOURCE-DECLARED data item this unit binds, for the post-bind <c>VersionConformancePass</c> data-attribute
    /// gates (Step 14g): the WS + FILE-record + OO-method forest (<see cref="Roots"/>), the LINKAGE forest
    /// (<see cref="LinkageRoots"/>, kept OFF <c>Roots</c>), and the TYPEDEF templates (<see cref="TypeDecls"/>, also OFF
    /// <c>Roots</c>) — pre-order DFS, declaration order. It EXCLUDES the products of post-bind expansion, which the binder
    /// never re-analyzed and so never gated: a <c>TYPE IS type-name</c> clone subtree (its items carry a non-null
    /// <see cref="DataItem.TypeAnchor"/>; the once-per-source gate fired on the TEMPLATE in <c>TypeDecls</c>) and the
    /// OO/UDF compiler temps (recorded in <see cref="CompilerTempClones"/>; both share the source item's <c>PicInfo</c> by
    /// reference — so they must NOT be dedup'd by <c>PicInfo</c> identity: two distinct source pointer items share the ONE
    /// <c>PicInfo.PointerItem</c> singleton). The result is exactly the set of items the binder's per-entry
    /// <c>PictureAnalyzer.ParseUsage</c>/<c>Analyze</c> gates fired for, once each.
    /// </summary>
    public IEnumerable<DataItem> ConformanceForest()
    {
        var temps = CompilerTempClones.Count == 0 ? null
            : new HashSet<DataItem>(CompilerTempClones.Select(t => t.Temp));
        IEnumerable<DataItem> Walk(DataItem d)
        {
            // Prune a synthesized compiler temp's WHOLE subtree: a group temp (a UDF group-RETURNING result)
            // clones the callee's already-gated children — re-yielding them would double-fire the per-item
            // data-attribute gates in the CALLER's unit (the same exclusion the elementary temp always had).
            if (temps is not null && temps.Contains(d)) yield break;
            yield return d;
            foreach (var c in d.Children)
                foreach (var x in Walk(c)) yield return x;
        }
        foreach (var item in Roots.Concat(LinkageRoots).SelectMany(Walk)
                     .Concat(TypeDecls.Values.SelectMany(Walk)))
            if (StrongTypeModel.TypeAnchor(item) is null)
                yield return item;
    }

    /// <summary>The elementary leaves of an item (itself if elementary), in source order.</summary>
    private static IEnumerable<DataItem> LeavesOf(DataItem d)
    {
        if (d.IsElementary) { yield return d; yield break; }
        foreach (var c in d.Children)
            foreach (var l in LeavesOf(c)) yield return l;
    }

    /// <summary>Find an item by COBOL name within a record subtree (the item itself or any descendant).</summary>
    private static DataItem? FindDescendantOrSelf(DataItem root, string name)
    {
        if (string.Equals(root.CobolName, name, StringComparison.OrdinalIgnoreCase)) return root;
        foreach (var c in root.Children)
            if (FindDescendantOrSelf(c, name) is { } f) return f;
        return null;
    }

    /// <summary>Find a (possibly qualified) item within a record subtree: the base name matches the item and
    /// every IN/OF qualifier matches SOME ancestor, in written (innermost→outermost) order with skips allowed —
    /// ISO §8.4.2.2 qualification. Identically-named items under different areas are legal and disambiguated by
    /// their qualifiers (IX215A's three same-named keys).</summary>
    private static DataItem? FindQualified(DataItem root, string name, IReadOnlyList<string> quals)
    {
        if (string.Equals(root.CobolName, name, StringComparison.OrdinalIgnoreCase) && QualsMatch(root, quals))
            return root;
        foreach (var c in root.Children)
            if (FindQualified(c, name, quals) is { } f) return f;
        return null;

        static bool QualsMatch(DataItem item, IReadOnlyList<string> quals)
        {
            int qi = 0;
            for (DataItem? a = item.Parent; a is not null && qi < quals.Count; a = a.Parent)
                if (string.Equals(a.CobolName, quals[qi], StringComparison.OrdinalIgnoreCase)) qi++;
            return qi == quals.Count;
        }
    }
}
