// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Passes;
using CobolNet.Common;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Cst;
using CobolNet.Frontend.Generated;

using CobolNet.Binding.Model;
using CobolEdit = CobolNet.Runtime.CobolEdit;

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

    /// <summary>The group's <c>&gt;&gt;COBOL-WORDS</c> override (ISO §7.3.10) — the intrinsic binder resolves a
    /// function-name synonym / removal (EQUATE/UNDEFINE/SUBSTITUTE of an intrinsic-function-name) through it.
    /// Defaults to <see cref="Editions.CobolWordsMap.Empty"/> (no directive) for direct test construction.</summary>
    public Editions.CobolWordsMap CobolWords { get; init; } = Editions.CobolWordsMap.Empty;

    /// <summary>The group's <c>&gt;&gt;LEAP-SECOND</c> state (ISO §7.3.17; kb/Work PB65) — the intrinsic renderer
    /// passes it to every §15.3 date/time runtime function that reads a seconds subfield or a standard numeric
    /// time form (SECONDS-FROM-FORMATTED-TIME, TEST-FORMATTED-DATETIME, INTEGER-OF-FORMATTED-DATE, FORMATTED-TIME,
    /// FORMATTED-DATETIME, COMBINED-DATETIME).</summary>
    public bool LeapSecond { get; init; }

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

    /// <summary>True when this unit's internal FILE CONNECTORS are unit-scoped — ONE registration per run
    /// unit, LAST-USED across activations: a RECURSIVE-and-not-INITIAL unit, every FUNCTION included
    /// (§8.6.6). ISO §14.6.2.3.2 action 3 sets internal connectors "to not be in any open mode" only when
    /// data is placed in the INITIAL state, which for a non-INITIAL unit's static data is cases 1–3 (run-unit
    /// start / an INITIAL container's activation / after CANCEL); §14.6.2.3.3 keeps them last-used otherwise.
    /// UNLIKE <see cref="UnitStaticWs"/> this carries NO childless conjunct: the static registration flag is
    /// referenced only from the unit's OWN class (never through an <c>__outer</c> bridge), so contained
    /// programs do not constrain it. kb/Work PB168 — the per-INSTANCE flag re-ran registration on every
    /// fresh RECURSIVE activation and <c>FileRegistry.Register</c> silently REPLACED the live connector
    /// (the depth-2 WRITE answered '42'; the displaced writer's buffer was lost, its handle leaked).</summary>
    public bool UnitStaticFiles { get; init; }

    /// <summary>⛔ THE ONE condition for both EMITTING <c>__ResetStatics</c> (RecordStructEmitter) and
    /// REGISTERING it as the unit's initial-state hook (ProgramEmitter → ProgramTable): static WS storage
    /// exists, or the unit-scoped file-registration flag does — the flag must return to false on the
    /// §14.6.2.3.2 initial-state cases so a run-unit re-run and a post-CANCEL activation re-register their
    /// connectors. The two emitter sites previously mirrored this predicate BY HAND with a comment warning
    /// that divergence is a CS0103 in generated code; reading it here retires the mirroring (kb/Work PB168;
    /// the one-rule-one-place discipline).</summary>
    public bool EmitsStaticReset =>
        (UnitStaticWs && (StaticRootFields.Count > 0 || StaticBasedBridgeAddrs.Count > 0))
        || (UnitStaticFiles && Files.Count > 0);

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
    /// image (ISO §14.9.25.4 MOVE GR4 — a whole-group move fills without conversion; see <see cref="DataItem.StoreAsImage"/>).
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
    public OptionsModel Options { get; internal set; } = OptionsModel.Default;

    /// <summary>Seed the §11.9.4 GR1 baseline for a CLASS-CHANNEL bind (kb/Work PB135): OoDriver computes the
    /// class/object/factory-level model from the skeleton paragraphs and hands it here before BindDeclarations,
    /// whose own Bind over the synthetic unit (no identification body) then folds it as the baseline.</summary>
    internal void CallInheritOptions(OptionsModel m) => _inheritedOptions = m;

    /// <summary>All SELECTed files (the SELECT clause joined with its FD records), in source order.
    /// (READ-ONLY view — P6 Step 5. The bind-phase file-connector qualification mutates the FileModel ELEMENTS,
    /// which a read-only list does not prevent — element immutability is a later data-model-track item.)</summary>
    public IReadOnlyList<FileModel> Files => _files;
    private readonly List<FileModel> _files = [];

    /// <summary>The files keyed by COBOL file-name (case-insensitive), for the binder to resolve OPEN/READ/CLOSE
    /// targets and to map a WRITE/REWRITE record-name back to its owning file.</summary>
    public Dictionary<string, FileModel> FilesByName { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Names DECLARED in the SCREEN SECTION, which is REFUSED as the declined Annex A.4.2 module
    /// (COBOLNET1560; <see cref="ScreenFacility"/>). Two consumers, both still needed after the refusal:
    /// <list type="number">
    /// <item>kb/Work R32 — a reference to such a name is exempt from the §8.4.2.1 UNDEFINED diagnostic
    /// (COBOLNET1639). The compile already fails on the section; adding "SG is not defined" on top would send
    /// the user hunting a declaration that is right there. "Not defined" and "declared in a refused section"
    /// are different verdicts, and only the second is true.</item>
    /// <item>It is how <c>ACCEPT screen-name-1</c> / <c>DISPLAY screen-name-1</c> are told apart from their
    /// token-identical DEVICE formats (AcceptDisplayBinder.ScreenFormatOf) — the operand's DECLARATION is the
    /// only thing that distinguishes them.</item></list>
    /// ⚠ The posture used to be compile-ACCEPT with a staged runtime loud; kb/Work PB260 measured what that
    /// actually did (a screen record printed to standard output) and made it a refusal.</summary>
    public HashSet<string> ScreenNames { get; } = new(StringComparer.OrdinalIgnoreCase);

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
        // §11.9.4 GR1: a contained program's OPTIONS start from its container's model and override clause by
        // clause (InheritConfiguration set the baseline); an outermost unit starts from the all-defaults model.
        Options = OptionsBinder.Bind(program, Edition, _inheritedOptions);   // captured even when there is no WORKING-STORAGE

        // §12.3.3 SR1: "The configuration section shall not be specified in a program that is contained within
        // another program" — the container's applies (§12.3.4 GR1, InheritConfiguration). Diagnosed, then the
        // section is still walked below so a second diagnostic never masks this one.
        if (UnitIsContained && EnvDivisions(program).Any(env => env.configurationSection() is not null))
            Edition.Error(DiagnosticCatalog.ConfigurationSectionInContainedProgram,
                "a CONFIGURATION SECTION is specified in a program contained within another program; the "
                + "containing program's configuration section applies to it (ISO §12.3.3 SR1 / §12.3.4 GR1)");

        // ARITHMETIC mode validity (§11.9.5 / §8.8.1): NATIVE, STANDARD-DECIMAL, and plain STANDARD are
        // implemented. STANDARD arithmetic (the 2002 mode; obsolete 2014, removed 2023 — Annex E.2 item 21)
        // performs operations in the standard intermediate data item, which for its reachable operands IS the
        // standard DECIMAL form, so STANDARD routes to the same CobolDec engine as STANDARD-DECIMAL
        // (NumericRenderer.StandardDecimal). Floating-point operands participate under BOTH modes through the
        // §8.8.1.5.1 implementor-defined float→SDIDI conversion (CobolDec.FromDouble — the shortest-round-trip
        // decimal identity of the IEEE value); the operations themselves are SDIDI (P10 Step 12).
        // STANDARD was DROPPED by ISO/IEC 1989:2023 (§8.8.1 names only NATIVE/STANDARD-BINARY/STANDARD-DECIMAL)
        // → the pass's arithmetic-standard-2002 dual-window row rejects it at --std 2023 (0807).
        // STANDARD-BINARY is DECLINED — but the screen is NOT here any more: it moved to OptionsBinder's
        // ArithmeticOf, the single point every unit kind's OPTIONS clause is read through. This binder does not
        // see a METHOD's or an INTERFACE's options paragraph, so the twin that stood here left both of those
        // arms silent (kb/Work PB197, measured). Its 2014 introduction edge remains the pass's
        // arithmetic-standard-binary-2014 row.
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

        // SCREEN SECTION (ISO §13.9) is an OPTIONAL element of Annex A.4.2, the module docs/CONFORMANCE.md §5
        // records as Not claimed. A.4.1 admits an optional element's syntax ONLY when support is claimed, so the
        // section, every entry in it and every clause inside those entries are refused BY NAME (COBOLNET1560) —
        // ScreenFacility is the one funnel and names the construct the user actually wrote. The declared
        // screen-names are recorded there as well, so a later reference reports "declared in a refused section"
        // rather than the §8.4.2.1 UNDEFINED verdict (kb/Work R32; the differential's syn_screen:221 flip).
        // ⚠ This was a WARNING until kb/Work PB260: the program compiled, the screen behavior was simply absent,
        // and a DISPLAY of the screen record printed its characters — a declined facility that produced output.
        if (program.dataDivision()?.screenSection() is { } scr)
            ScreenFacility.ReportSection(Edition, scr, ScreenNames);

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
                    MarkImageForced(child);      // the collected image fact (same rule as the whole-group union, §14.9.25.4 MOVE GR4)
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
            RouteStaticRoot(root, "WORKING-STORAGE");
        // THE FILE SECTION ROUTES WITH IT (kb/Work PB168 — the review fleet's §8.6.4 finding): "Data items
        // and file connectors defined in the working-storage or file section of a source element that is not
        // an initial program are static items … For static items that are not object data, there is one copy
        // in a run unit." The connector half is UnitStaticFiles/the static registration guard; the RECORD
        // AREA half is this — without it, the shared connector reads into a per-activation area and the
        // resumed outer activation sees stale record content (a silent wrong answer where the standard has
        // ONE area). EXTERNAL files stay off the channel (run-unit ExternalStore, §8.6.7); a REPORT FD has
        // no record description to route (§13.18.46). ⚠ __ResetStatics deliberately does NOT re-seed these:
        // §14.6.2.3.2 action 2 names only the working-storage and local-storage sections.
        foreach (var file in Files)
            if (!file.IsExternal)
                foreach (var record in file.Records)
                    RouteStaticRoot(record, "FILE SECTION");
    }

    /// <summary>Route ONE root of a <see cref="UnitStaticWs"/> unit onto the static channel — shared by the
    /// WORKING-STORAGE walk and the kb/Work PB168 FILE SECTION walk (<paramref name="section"/> names the
    /// section in the staged-loud message).</summary>
    private void RouteStaticRoot(DataItem root, string section)
    {
        if (root.Class is { Tier: RedefinesTier.StringCanonical } cls
            && CallExternalBackings.Any(b => b.BackingCsName == cls.BackingCsName))
            return;   // an EXTERNAL-backed class (canonical AND views) — run-unit cell storage (§14.6.2.3.3), not a class static
        if (root.IsBased)
        {
            // kb/Work PB154 — §14.6.2.3.2 action 5: a BASED root in a RECURSIVE unit's static WS routes
            // its `__addr_X` bridge as a STATIC field, reset to NULL by __ResetStatics (the old code
            // REJECTED this legal source). The DATA lives in the allocated cell, so no static root field
            // joins the value-reset set. A based root whose class the substrate screen rejected
            // (COBOLNET1695) was already diagnosed — skip quietly.
            if (root.Class?.BasedPointerField is { } bp && !PtrAddressableCellOf.ContainsKey(root.Class))
                _staticBasedBridgeAddrs.Add(bp);
            // An INDEXED BY cell under a BASED table is an ordinary emitted field (allocated in
            // _indexFields regardless of BASED-ness) and §13.5.4 GR1 makes it static like the rest of
            // the unit's WS — the bridge routes the ADDRESS, the index cells still route themselves.
            foreach (var idx in IndexNamesUnder(root))
                if (_indexFields.TryGetValue(idx, out var basedCell))
                    _staticIndexCells.Add(basedCell);
            return;
        }
        if (root.Class is { } c && (c.BasedPointerField is not null || PtrAddressableCellOf.ContainsKey(c)))
        {
            Edition.Error(DiagnosticCatalog.RecursiveWsPointerBacked,
                $"'{root.CobolName ?? "FILLER"}': an ADDRESS-OF-taken record in the {section} of "
                + "a RECURSIVE program or function is recognized but its static cell storage is not yet "
                + "implemented (ISO §13.5.4 GR1 / §14.6.2.3.2 #5; the BASED half landed with kb/Work PB154)");
            return;
        }
        if (root.Class is { Tier: RedefinesTier.StringCanonical } c2 && ReferenceEquals(c2.Canonical, root))
            _staticRootFields.Add(c2.BackingCsName);   // Tier-B: the ONE string backing IS the storage
        _staticRootFields.Add(root.CsName);
        foreach (var idx in IndexNamesUnder(root))
            if (_indexFields.TryGetValue(idx, out var cell))
                _staticIndexCells.Add(cell);
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
            using var _ = Edition.At(entry);   // the entry cursor (kb/Work PB82): every diagnostic below names this entry
            // A CONSTANT entry (ISO §13.10; the constantEntryBody alternative) is a COMPILE-TIME substitution,
            // not storage: fold it into the constant table and produce NO DataItem. Checked BEFORE the 66/88
            // early-outs so a mis-leveled constant entry gets its §13.10.2 level diagnostic, never a RENAMES/
            // condition-name misbind. (P10 Step 15; DataBinder.Constants.cs.)
            if (entry.dataDescriptionBody().constantEntryBody() is { } constBody)
            {
                BindConstantEntry(entry, constBody);
                continue;
            }
            // The level-number arrives PRE-SCREENED: LevelNumberPass ran over the whole parse tree before any
            // binding and rejected every value outside the set §13.18.33.3 permits for this entry's section, so
            // `lvl` here is 1–49, 66, 77 or 88 and the TryParse cannot fail in a compile that reaches emit. Do
            // NOT re-decide the set here — there are four grammar arms that spell a level-number and only one of
            // them is this one (kb/Work PB485; LevelNumberArmDriftTests holds the list honest).
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
                    Edition.Error(DiagnosticCatalog.TypeDeclarationShape, $"TYPEDEF on '{item.CobolName ?? "FILLER"}': the TYPEDEF clause "
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
            // §13.18.40.3 SR32 (subordinate half — the parent chain exists only here): a format 2 PICTURE clause
            // shall not be specified in any data item subordinate to a CONSTANT RECORD item.
            if (!item.IsConstantRecord && IsConstantRecordItem(item) && item.Pic is { LocaleEdit: not null })
                Edition.Error(DiagnosticCatalog.PictureLocaleFormat2Violation, $"'{item.CobolName ?? "FILLER"}': a "
                    + "format 2 PICTURE clause shall not be specified in any data item subordinate to a data item "
                    + "described with the CONSTANT RECORD clause (ISO §13.18.40.3 SR32)");
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
            Edition.Error(DiagnosticCatalog.TypeDeclarationShape, $"TYPEDEF '{item.CobolName ?? item.CsName}': a type declaration shall be a "
                + "level-01 record-description entry (ISO §13.18.58 / §13.16)");
        if (item.CobolName is null)
        {
            Edition.Error(DiagnosticCatalog.TypeDeclarationShape, "a TYPEDEF entry shall be named (not FILLER) — it defines a type-name "
                + "(ISO §13.18.58.4 GR2)");
            return;
        }
        if (item.RedefinesTargetName is not null)
            Edition.Error(DiagnosticCatalog.TypeDeclarationShape, $"TYPEDEF '{item.CobolName}': the TYPEDEF and REDEFINES clauses are "
                + "mutually exclusive (ISO §13.16)");
        // ⛔ §8.5.3.1: "Elementary type definitions shall not be specified with the STRONG phrase." — with
        // §8.5.3.3, "The only kind of items that may be strongly typed are group items." Unenforced until
        // kb/Work PB153: `01 T TYPEDEF STRONG PIC 9(3).` bound clean, and the illegal shape had already reached
        // the corpus — tests/version-matrix/constructs.json's usage-pointer-to-type-2014 row carried exactly it
        // as the construct's canonical witness. A type declaration nothing ever compiled is a declaration nothing
        // ever contradicted.
        // ⛔ THE TEST IS `IsElementary`, NOT `!IsGroup`, AND THAT IS NOT A STYLE CHOICE. RegisterTypeDecl runs
        // while the forest is still being BUILT — the level-01 is registered before its subordinate entries are
        // attached — so `IsGroup` (Pic is null AND Children.Count > 0) is FALSE for every group typedef at this
        // moment and the check would reject all of them. `Pic`, by contrast, is already assigned by BindEntry
        // before the item is pushed, so `IsElementary` is exactly the settled fact this rule needs.
        if (item.TypedefStrong && item.IsElementary)
            Edition.Error(DiagnosticCatalog.TypeDeclarationShape, $"TYPEDEF '{item.CobolName}': an ELEMENTARY type definition shall not be "
                + "specified with the STRONG phrase (ISO §8.5.3.1) — §8.5.3.3 makes group items the only kind of "
                + "item that may be strongly typed");
        if (!TypeDecls.TryAdd(item.CobolName, item))
            Edition.Error(DiagnosticCatalog.TypeDeclarationShape, $"duplicate type-name '{item.CobolName}' — a type-name shall be unique "
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
            using var _ = Edition.At(grp);
            if (grp.fileName()?.GetText() is not { } name) continue;
            var file = new FileModel { CobolName = name, SelectName = name, AssignTarget = name, Optional = grp.OPTIONAL() is not null };
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
            ScreenRepositoryIntrinsicName(name, "file-name");   // §8.3.2.1 rule 5 (kb/Work PB65)
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

        file.PrimeKeyCollation = ResolveKeyCollating(file, file.RecordKeyName);
        for (int i = 0; i < file.AlternateKeys.Count; i++)
            file.AlternateKeyCollations.Add(ResolveKeyCollating(file, AltName(file, i)));
    }

    /// <summary>The declared name of the i-th resolved alternate key (index-aligned when all names resolve — the
    /// normal case; a name that failed to resolve has already errored).</summary>
    private static string? AltName(FileModel file, int i) =>
        i < file.AlternateKeyNames.Count ? file.AlternateKeyNames[i].Name : null;

    /// <summary>Resolve one key's collating sequence (§12.4.5.7.4): a Format-2 alphabet naming the key wins (GR6),
    /// else the file-level alphanumeric default (GR2), else native ordinal (null). An alphanumeric alphabet
    /// resolves to its <see cref="AlphabetDef"/> — a literal-phrase table or, per owner decision Q3 (determination
    /// L8), a LOCALE sequence (the key locale is captured when the connector is registered; a file written under one
    /// locale and read under another is not guaranteed to be in key order — documented); a NATIONAL alphabet is the
    /// recognized-not-implemented P14 GAP; an undeclared name errors.</summary>
    private AlphabetDef? ResolveKeyCollating(FileModel file, string? keyName)
    {
        string? alphabet = null;
        if (keyName is not null)
            foreach (var (names, a) in file.KeyLevelCollating)
                if (names.Any(n => n.Equals(keyName, StringComparison.OrdinalIgnoreCase))) { alphabet = a; break; }
        alphabet ??= file.FileLevelCollating?.Alnum;   // GR2 file-level alphanumeric default
        if (alphabet is null) return null;             // GR4/GR5 — native ordinal

        if (Alphabets.TryGetValue(alphabet, out var def))
            return def.IsIdentity ? null : def;   // SR1 — alphanumeric collating (an identity alphabet ⇒ native)
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
    /// capture pattern, applied to keys.
    /// <para>⛔ IT KEEPS THE QUALIFICATION AND DROPS EVERYTHING ELSE — a subscript or a reference modification
    /// written on the operand is DISCARDED here, silently (kb/Work PB205). That is admissible only where the
    /// clause's own rules make those spellings illegal, and §8.4.3.3.3's NOTE is what makes them so for the
    /// data-name clauses: "Because the references to data items are restricted to identifiers, where data-name-n
    /// is used in a general format or syntax rule, then reference-modification is not permitted." A clause that
    /// EXPRESSLY permits the ref-mod — §13.18.16.3 SR4's CONTROL operand and its §13.18.57.3 SR10 / §13.18.54.3
    /// SR8 twins — must NOT use this helper alone: it binds as if the operand were unmodified, which is a silent
    /// wrong answer. Those three go through <c>DataBinder.Reports.ControlOperandRef</c>, which keeps the whole
    /// written reference. Where the ref-mod is illegal, dropping it still ACCEPTS illegal source instead of
    /// rejecting it (measured on a reference-modified RECORD KEY) — a screen those clauses still owe.</para></summary>
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
            using var _ = Edition.At(fd);
            if (fd.fileName()?.GetText() is not { } name) continue;
            var records = BindEntries(fd.dataDescriptionEntry(), rootNames, EntrySection.File);
            if (!FilesByName.TryGetValue(name, out var file))
            {
                // An FD with no matching SELECT — keep a model so its records still resolve (it is never opened).
                file = new FileModel { CobolName = name, SelectName = name };
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
                else if (clause.codeSetClause() is { } cs)
                    BindCodeSetClause(cs, file, records);   // ISO §13.18.13 (kb/Work PB110)
                else if (clause.formatClause() is { } fmt)
                    // FORMAT clause (ISO §13.18.24) — Annex A.4.8 item 1), an OPTIONAL element this
                    // implementation does not claim (docs/CONFORMANCE.md §5). Recognized so it can be REFUSED
                    // BY NAME rather than parse-erroring or, worse, binding inert: §13.18.24.4 GR1 makes the
                    // clause change the on-medium representation, so an inert compile writes the wrong bytes.
                    // Every occurrence reports (this loop runs per clause per FD).
                    Edition.Declined(DiagnosticCatalog.FormatSelectWhenUnclaimed,
                        $"the FORMAT clause on file description entry '{name}' ({Spelled(fmt)})");
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
            using var _ = Edition.At(sd);
            if (sd.fileName()?.GetText() is not { } sdName) continue;
            var sdRecords = BindEntries(sd.dataDescriptionEntry(), rootNames, EntrySection.File);
            if (!FilesByName.TryGetValue(sdName, out var sdFile))
            {
                sdFile = new FileModel { CobolName = sdName, SelectName = sdName };
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
            using var _ = Edition.At(clause);
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
    /// <summary>True when <paramref name="raw"/> is a numeric literal (optional sign, digits, one decimal point —
    /// the floating-point form's significand likewise, ISO §8.3.3.3.3: the exponent cannot make a zero significand
    /// nonzero) whose value is NOT zero — the VCR 86 gate subject (ISO §13.18.63 SR6 exempts the literal-zero forms
    /// at all editions, so <c>0</c>/<c>0.00</c> return false; a quoted/national/figurative VALUE is not numeric).</summary>
    private static bool IsNonZeroNumericLiteral(string raw)
    {
        string t = raw.Trim();
        if (t.IndexOfAny(['E', 'e']) is > 0 and var ePos && NumericLiteral.IsFloatingPointForm(t)) t = t[..ePos];
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

    /// <summary>The numeric-literal FORM test (§8.3.3.3.2 fixed-point: an optional sign, digits, at most one decimal
    /// point, at least one digit) — the shape a permissive digits-only alphanumeric VALUE must have to be stored as a
    /// number (kb/Work PB94).</summary>
    private static bool IsNumericLiteralForm(string text)
    {
        string t = text.Trim();
        if (t.StartsWith('+') || t.StartsWith('-')) t = t[1..];
        bool digit = false, dot = false;
        foreach (char c in t)
        {
            if (c == '.') { if (dot) return false; dot = true; continue; }
            if (!char.IsAsciiDigit(c)) return false;
            digit = true;
        }
        return digit;
    }

    /// <summary>The size §13.18.63.3 SR4/SR5/SR10 measure a VALUE literal against for an ELEMENTARY subject —
    /// "the size indicated by an explicit PICTURE clause" — or <see langword="null"/> when the entry indicates
    /// none. ⛔ The null arm is not a defensive nicety: for a DYNAMIC LENGTH item, §13.18.19.3 SR1 — "The
    /// character-string specified in that PICTURE clause shall be one instance of the picture symbol 'N', or
    /// 'X'" — and §13.18.19.4 GR1 — "The picture symbol determines the class." That one symbol indicates a CLASS,
    /// never a size: its maximum is the LIMIT phrase's, or implementor-defined (GR2), never the picture's
    /// one position. ANY LENGTH (§13.18.2) is the same shape. The GROUP subject's size is its own
    /// (<c>DataBinder.GroupValue</c>'s <c>GroupSubjectPic</c>), and a group whose subordinate is dynamic-length is
    /// a variable-length group, which §13.18.63.3 SR1 already bars from being a VALUE subject at all.</summary>
    private static int? ValueSizePositions(PicInfo pic, bool isDynamicLength, bool isAnyLength) =>
        isDynamicLength || isAnyLength ? null : pic.Length;

    /// <summary>⛔ THE ONE VALUE-CLAUSE LITERAL SCREEN — every literal of every VALUE format passes through here,
    /// and the raw text it returns is what gets STORED (unchanged, or the --permissive rewrite of a class-mismatched
    /// literal on a numeric subject).
    /// <para>ISO §13.18.63.3 SR2 ("If the category of the subject of the entry is numeric, all literals in the VALUE
    /// clause shall be numeric and shall be permissible values within the range indicated by the PICTURE clause or
    /// the USAGE clause") is an <b>ALL FORMATS</b> rule, and SR16 carries SRs 10–15 into FORMAT 2 as well
    /// ("Syntax rules 10, 11,12,13,14,and 15 above apply") — so the format-2 (table) literals are governed exactly
    /// as the format-1 one is. They were not: the four checks below sat INLINE at the format-1 call site, and
    /// <see cref="BuildTableValueSpecs"/>' per-occurrence literals reached the emitter unscreened, so
    /// <c>05 B PIC 9(4) COMP OCCURS 2 VALUE "0012" FROM (1) TO (2)</c> compiled clean at strict 2023 where its
    /// format-1 twin is COBOLNET1657. The <c>feedback_two_arm_dispatch</c> shape on the VALUE clause's own formats;
    /// extracting the screen is the fix, so a rule added here reaches every format BY CONSTRUCTION (kb/Work PB208).</para></summary>
    private string ScreenValueLiteral(PicInfo pic, string raw, string where, int? sizePositions)
    {
        // VALUE-clause literal/category conformance for the string-stored 2002 categories (ISO §13.18.63
        // SR5 national / SR10 boolean — the 0898 band, both directions) and SR2's CLASS half.
        raw = ValidateValueCategory(pic, raw, where, sizePositions);
        // VALUE-clause range/sign conformance for a fixed-point NUMERIC subject (ISO §13.18.63.3 SR2/SR3): a numeric
        // VALUE literal must be a permissible value in the PICTURE range, representable WITHOUT truncation of a
        // leading/trailing nonzero digit (SR2), and a negative literal requires a signed subject (SR3). The
        // initializer path (ValueInitializer → EmitText.UnscaledAtScale) does NO high-order modulo and NO
        // unsigned-magnitude, so absent this bind-time gate an out-of-range / wrong-sign VALUE silently seeds an
        // out-of-range native field (COBOLNET1625). A NUMERIC-EDITED subject's numeric literal rides the same check
        // (SR6 — converted per the MOVE rules "such that no truncation of digits or sign is required"; kb/Work PB97).
        if (pic is { Category: PicCategory.Numeric, IsFloat: false } or { Category: PicCategory.NumericEdited })
            ValidateNumericValue(pic, raw, where);
        // (§13.18.63.3 SR6's literal-FORM rule for numeric-edited subjects — both forms, both directions — is
        // ValidateValueCategory's, above, the item-VALUE + level-88 funnel; D21/PB66, kb/Work PB97.)
        // VCR 86 (ISO §13.18.63 SR6; Annex E.3.3 item 43): a NON-ZERO numeric literal VALUE for a numeric-edited
        // item is a COBOL-2023 capability — below 2023 a numeric-edited VALUE required an alphanumeric edited-image
        // literal. SR6 exempts "the integer and decimal forms of the literal zero" (and the figurative ZERO — VCR
        // 35) at ALL editions, so only a non-zero numeric literal is gated. Scoped to the ITEM VALUE (not level-88).
        // SR6 names "formats 1, 2, and 4", so the format-2 occurrence literals are gated by the same call.
        if (pic is { Category: PicCategory.NumericEdited } && IsNonZeroNumericLiteral(raw))
            ConstructRegistry.Check(Edition.Edition, Edition.Sink,
                Constructs.ValueNumericLiteralNumericEdited2023, where);
        // VCR 34 (ISO §13.18.63 SR4/SR5; Annex E.2 item 27): at >=2023 an alphanumeric edited-image literal VALUE on
        // a numeric-edited item is checked against the PICTURE size — a literal LONGER than the edited width is
        // rejected (below 2023 it was stored truncated — the "unclear value"). Under --permissive the check
        // downgrades to a warning (a removed-capability posture). The national-class-mismatch leg is already
        // COBOLNET0898 (ValidateValueCategory); only a plain alphanumeric literal (leading '"') reaches this size check.
        if (pic is { Category: PicCategory.NumericEdited } && Edition.DialectLevel >= 2023
            && raw.StartsWith('"') && CobolLiteral.Decode(raw).Length > pic.Length)
            Edition.Sink.Report(new EditionDiagnostic("COBOLNET1570",
                EditionSeverityPolicy.For(ConstructAvailability.Removed, Edition.Edition), "value-numeric-edited-oversize",
                $"{where}: the VALUE literal ({CobolLiteral.Decode(raw).Length} characters) exceeds the "
                + $"numeric-edited item's {pic.Length}-character edited size (ISO §13.18.63 SR4/SR5; COBOL-2023, "
                + "Annex E.2 item 27)", where, "ISO §13.18.63 SR4/SR5; Annex E.2 item 27"));
        return raw;
    }

    /// <summary>⛔ THE §13.18.63.3 VALUE-OPERAND SCREEN — the ONE place that decides whether a VALUE clause's
    /// literal may seed its subject, for EVERY subject the rules name: an elementary item, a condition-name's
    /// conditional variable, and (kb/Work PB206) a GROUP carrying a group-level VALUE. Returns the raw VALUE text
    /// to STORE — the input unchanged, or (kb/Work PB94, --permissive only) the representable numeric rewrite of a
    /// class-mismatched literal on a numeric subject.
    ///
    /// <para><b>Why a group subject enters HERE rather than a second screen.</b> SR4, SR5 and SR10 each say "the
    /// item" / "the subject of the entry", never "the elementary item": §8.5.2.1 gives a group a class and a
    /// category of its own ("an alphanumeric group item has class and category alphanumeric"; "a bit group item
    /// has class and category boolean"; "a national group item has class and category national"), and
    /// §13.18.29.4 GR1b/GR2b give a bit / national group an as-if PICTURE. So the group is a subject with a
    /// category and a size exactly as an elementary item is, and SR13 sentence 1 — "If the VALUE clause is
    /// specified at the group level, literal-1 shall be of the same category as the group item or shall be a
    /// figurative constant that is permitted in a MOVE statement to a receiving item of that category" — is
    /// those same three rules restated for it. Writing it twice is how the two answers drift.</para>
    ///
    /// <para>⚠ <b>Reached through <see cref="ScreenValueLiteral"/></b> for the item VALUE of EVERY
    /// format (kb/Work PB208), and directly by exactly two other kinds of site: the level-88 arms, and
    /// the group-level VALUE (<c>DataBinder.GroupValue</c>). A NEW direct caller inherits only ONE of SR2's
    /// two halves — the CLASS half here, never the RANGE half <see cref="ValidateNumericValue"/> carries —
    /// so route it through the funnel unless its subject cannot be of category numeric at all, which is the
    /// group arm's standing reason (§8.5.2.1 gives a group item category alphanumeric, national or
    /// boolean, never numeric).</para></summary>
    /// <param name="sizePositions">The number of positions §13.18.63.3 SR4/SR5/SR10 measure the literal against —
    /// "the size indicated by an explicit PICTURE clause" for an elementary subject, "the size of the group item"
    /// for a group one — or <see langword="null"/> when the entry indicates NO size. ⛔ The null case is REAL and
    /// was a measured rejection of legal source: for a DYNAMIC LENGTH (or ANY LENGTH) item, §13.18.19.3 SR1 —
    /// "The character-string specified in that PICTURE clause shall be one instance of the picture symbol 'N', or
    /// 'X'" — and §13.18.19.4 GR1 — "The picture symbol determines the class." It indicates no size at all, the
    /// maximum being the LIMIT phrase's or implementor-defined
    /// (GR2). `01 UN PIC N DYNAMIC LENGTH VALUE N"SEED".` was rejected as exceeding "the item's 1 national
    /// positions" while its alphanumeric twin `01 UD PIC X DYNAMIC LENGTH VALUE "SEED".` was accepted and ran —
    /// the [[two_arm_dispatch]] shape, one arm sized and one not.</param>
    /// <param name="groupSubject">True when the subject is a GROUP carrying a group-level VALUE (§13.18.63.3 SR13
    /// sentence 1). It withholds ONE thing: the SR4-sentence-1 vendor leniency that stores a numeric literal's
    /// digits on an alphanumeric subject. That leniency is a statement about a store the compiler can perform —
    /// for a group the store is §13.18.63.4 GR5's area deposit, which is defined over the operand's CHARACTERS,
    /// and a numeric literal has none (measured: `01 GN VALUE 1234. 05 N1 PIC X(2). 05 N2 PIC X(2).` seeded
    /// SPACES). A warning plus the wrong area is worse than the rejection SR13 asks for, so the group arm is an
    /// error on both dialect axes.</param>
    private string ValidateValueCategory(PicInfo pic, string raw, string where, int? sizePositions,
        bool groupSubject = false)    {
        // ⛔ THE LITERAL'S CLASS COMES FROM THE ONE CLASSIFIER (CobolLiteral.ClassOf — kb/Work PB71): the former
        // raw-text tests (`raw[1] is '"'`) refused the Format-2 hexadecimal spellings NX"…" / BX"…" (§8.3.3.5.2 /
        // §8.3.3.4.2 — the SAME class as N"…" / B"…") and every `ALL literal` figurative, whose class is
        // literal-1's (§8.3.3.6.3 SR2 / §14.9.25.4 GR7 Table 17). `ALL "AB"` (an alphanumeric literal-1) stays
        // illegal for a national or boolean item; `ALL SPACES` / `ALL ZEROS` is the figurative WORD (legal).
        LiteralClass? lit = CobolLiteral.ClassOf(raw);
        string? allRaw = CobolLiteral.AllLiteralRaw(raw);
        LiteralClass? allLit = allRaw is null ? null : CobolLiteral.ClassOf(allRaw);
        bool isNatLit = lit is LiteralClass.National || allLit is LiteralClass.National;
        bool isBoolLit = lit is LiteralClass.Boolean || allLit is LiteralClass.Boolean;
        bool isPlainString = lit is LiteralClass.Alphanumeric;
        bool isNumeric = raw.Length >= 1 && (char.IsAsciiDigit(raw[0]) || raw[0] is '+' or '-' or '.');
        bool isFloatLit = isNumeric && CobolNet.Common.NumericLiteral.IsFloatingPointForm(raw);
        // The part after a leading ALL (GetText concatenates tokens, so `ALL SPACES` → "ALLSPACES").
        string afterAll = raw.Length > 3 && raw.StartsWith("ALL", StringComparison.OrdinalIgnoreCase)
            ? raw[3..] : raw;
        bool isAllQuoted = allLit is LiteralClass.Alphanumeric;
        string word = afterAll.ToUpperInvariant();
        bool isZeroWord = word is "ZERO" or "ZEROS" or "ZEROES";
        bool isNationalFigurative = isZeroWord
            || word is "SPACE" or "SPACES" or "QUOTE" or "QUOTES"
                or "HIGH-VALUE" or "HIGH-VALUES" or "LOW-VALUE" or "LOW-VALUES";
        switch (pic.Category)
        {
            // ── The numeric literal's FORM vs the subject's (kb/Work PB97; the floating-point form is ISO §8.3.3.3.3):
            //    a FIXED-POINT numeric subject takes a floating-point literal at its EXACT value (§8.3.3.3.3 GR5 —
            //    significand × 10^exponent; SR2's "numeric ... representable exactly", checked on the expanded text
            //    downstream by ValidateNumericValue) — the raw text is REWRITTEN to that fixed-point value here, once,
            //    so no later reader (the carrier initializer, a level-88 membership test) meets an E-form it cannot
            //    scale (before this the emitter wrote `15EL` and Roslyn failed with CS0595). A FLOAT-usage subject
            //    keeps the floating form (its initializer is a binary64 literal).
            case PicCategory.Numeric when isFloatLit && !pic.IsFloat:
                return CobolNet.Common.NumericLiteral.ExpandFloatingPoint(raw);
            //    A numeric literal (either form) seeding a FLOAT-SHORT / FLOAT-LONG / FLOAT-BINARY-32/64 subject shall be
            //    "a permissible value within the range indicated by ... the USAGE clause" (§13.18.63.3 SR2) — the
            //    receiver's binary form (kb/Work PB99: a value beyond it became an overflowing C# double literal, CS0594).
            case PicCategory.Numeric when isNumeric && pic.IsFloat && !CobolNet.Common.NumericLiteral.FitsBinaryFloat(raw, pic.IsSingle):
                Edition.Error(DiagnosticCatalog.FloatingLiteral, $"{where}: the VALUE literal {raw} lies outside the range of the "
                    + $"item's {(pic.IsSingle ? "binary32 (FLOAT-SHORT / FLOAT-BINARY-32)" : "binary64 (FLOAT-LONG / FLOAT-BINARY-64)")} "
                    + "usage — a numeric VALUE shall be a permissible value within the range the USAGE indicates (ISO §13.18.63.3 SR2; "
                    + "the exponent range is implementor-defined, §8.3.3.3.3 r3)");
                return raw;
            //    §13.18.63.3 SR6 — a numeric-edited subject's numeric literal shall be of ITS form: "literals for
            //    fixed-point formats shall be specified as fixed-point, while literals for floating-point formats
            //    shall be specified as floating-point, though the figurative constant ZERO or ZEROES and the integer
            //    and decimal forms of the literal zero may also be specified for either format" (D21/PB66). The rule
            //    reaches formats 1, 2 and 4 of the VALUE clause — this funnel serves the item VALUE and the level-88 set.
            case PicCategory.NumericEdited when isFloatLit && !pic.IsFloatEdited:
                Edition.Error(DiagnosticCatalog.ValueEditedLiteralForm, $"{where}: the VALUE literal {raw} is a floating-point "
                    + "numeric literal but the item is a fixed-point numeric-edited item — literals for fixed-point formats "
                    + "shall be specified as fixed-point (ISO §13.18.63.3 SR6)");
                return CobolNet.Common.NumericLiteral.ExpandFloatingPoint(raw);
            case PicCategory.NumericEdited when pic.IsFloatEdited && isNumeric && !isFloatLit && IsNonZeroNumericLiteral(raw):
                Edition.Error(DiagnosticCatalog.ValueEditedLiteralForm, $"{where}: the VALUE literal {raw} is a fixed-point "
                    + "numeric literal but the item is a floating-point numeric-edited item — its VALUE shall be a "
                    + "floating-point literal, ZERO, or the literal zero (ISO §13.18.63.3 SR6)");
                return raw;
            // National: an N"…" literal or a figurative constant (§8.3.3.6 GR1/GR6/GR7 — SPACE/QUOTE/HIGH/
            // LOW/ZERO, incl. their ALL-prefixed forms). Plain strings, B"…", numeric, and ALL "literal" are
            // illegal.
            case PicCategory.National when isPlainString || isBoolLit || isNumeric || isAllQuoted
                    || !(isNatLit || isNationalFigurative):
                Edition.Error("COBOLNET0898", $"{where}: the VALUE of a national data item shall be a national "
                    + "literal (N\"…\") or a figurative constant (ISO §13.18.63 SR5)");
                break;
            // SR5 sentences 2 and 3 — ONE sentence pair, ONE arm: "National literals in the VALUE clause of an
            // elementary item shall not exceed the size indicated by an explicit PICTURE clause. National
            // literals in the VALUE clause of a national group item shall not exceed the size of the group item."
            // The limit is the SUBJECT's (sizePositions), never pic.Length: a DYNAMIC LENGTH item's picture
            // indicates no size (see the parameter's remarks — this was a live rejection of legal source).
            case PicCategory.National when lit is LiteralClass.National
                    && sizePositions is { } natLimit && CobolLiteral.Decode(raw).Length > natLimit:
                Edition.Error("COBOLNET0898", $"{where}: the VALUE national literal exceeds the item's "
                    + $"{sizePositions} national positions (ISO §13.18.63.3 SR5)");
                break;
            // Boolean: a B"…" literal or figurative ZERO (incl. ALL ZEROS) — no boolean SPACE/QUOTE/HIGH/LOW
            // exists (§14.9.25.3 SR7 posture).
            case PicCategory.Boolean when !isBoolLit && !isZeroWord:
                Edition.Error("COBOLNET0898", $"{where}: the VALUE of a boolean data item shall be a boolean "
                    + "literal (B\"…\") or the figurative constant ZERO (ISO §13.18.63 SR10)");
                break;
            // SR10 sentences 2 and 3 — the same sentence pair as SR5's, over boolean positions: an elementary
            // item's explicit PICTURE, or "the size of the group item" for a bit group item (whose positions are
            // §8.5.1.6.3's bit extent, DataItem.AsIfPic).
            case PicCategory.Boolean when lit is LiteralClass.Boolean
                    && sizePositions is { } boolLimit && CobolLiteral.Decode(raw).Length > boolLimit:
                Edition.Error("COBOLNET0898", $"{where}: the VALUE boolean literal exceeds the item's "
                    + $"{sizePositions} boolean positions (ISO §13.18.63.3 SR10)");
                break;
            case not (PicCategory.National or PicCategory.Boolean) when isNatLit || isBoolLit:
                Edition.Error("COBOLNET0898", $"{where}: a {(isNatLit ? "national (N\"…\")" : "boolean (B\"…\")")} "
                    + "literal may seed only a data item of its own category (ISO §13.18.63 SR5/SR10)");
                break;
            // ── kb/Work PB94 — §13.18.63.3 SR2: a NUMERIC subject takes numeric literals (and figurative ZERO) only.
            //    A digits-only alphanumeric literal (or a digits-only ALL "literal" repeated to the digit width) is
            //    the representable vendor leniency: error strict, warning + the number under --permissive; a
            //    character figurative (SPACE / QUOTE / HIGH-VALUE / LOW-VALUE) is stored as ZERO under --permissive
            //    (a native numeric holds no character fill — said in the warning); anything else is an error on
            //    both axes (the former path rendered it as a numeric initializer and the C# backend crashed).
            case PicCategory.Numeric when isPlainString || isAllQuoted:
            {
                string content = CobolLiteral.Decode(isAllQuoted ? allRaw! : raw);
                if (isAllQuoted && content.Length > 0 && content.All(char.IsAsciiDigit))
                {
                    // ALL "digits" repeats to the receiver's digit positions (§8.3.3.6.4 GR2 — MoveEmitter.AllDigitFill's rule)
                    int w = Math.Max(pic.Digits, 1);
                    content = string.Concat(Enumerable.Repeat(content, w / content.Length + 1))[..w];
                }
                if (IsNumericLiteralForm(content))
                {
                    Edition.Removed(DiagnosticCatalog.ValueLiteralClass.Code, $"{where}: the VALUE literal "
                        + $"{raw} is alphanumeric but the item is numeric — all literals in the VALUE clause of a "
                        + "numeric item shall be numeric (ISO §13.18.63.3 SR2); under --permissive it is stored as "
                        + $"the number {content.Trim()}");
                    return Edition.Permissive ? content.Trim() : raw;
                }
                Edition.Error(DiagnosticCatalog.ValueLiteralClass, $"{where}: the VALUE literal {raw} is not a "
                    + "numeric literal and the item is numeric (ISO §13.18.63.3 SR2)");
                return raw;
            }
            case PicCategory.Numeric when isNationalFigurative && !isZeroWord:
                Edition.Removed(DiagnosticCatalog.ValueLiteralClass.Code, $"{where}: the figurative constant "
                    + $"{raw} is not a numeric literal — the VALUE of a numeric item shall be numeric or ZERO (ISO "
                    + "§13.18.63.3 SR2); under --permissive the item is initialized to ZERO (a native numeric item "
                    + "holds no character fill)");
                return Edition.Permissive ? "0" : raw;
            // ── SR4 sentence 1: an alphabetic / alphanumeric / alphanumeric-edited subject takes alphanumeric
            //    literals only. On an ELEMENTARY subject a numeric literal is the vendor leniency (the digits,
            //    left-justified, as MOVE would store them): error strict, warning + that store under --permissive.
            //    On a GROUP subject there is no such store — §13.18.63.4 GR5 deposits the operand's CHARACTERS
            //    over the group area and a numeric literal has none, so SR13 sentence 1's "shall be of the same
            //    category as the group item" is an error on both axes (kb/Work PB206; measured on 1d949007:
            //    `01 GN VALUE 1234. 05 N1 PIC X(2). 05 N2 PIC X(2).` compiled clean and seeded SPACES).
            case PicCategory.Alphanumeric when isNumeric && IsNumericLiteralForm(raw) && groupSubject:
                Edition.Error(DiagnosticCatalog.ValueLiteralClass, $"{where}: the VALUE literal {raw} is numeric "
                    + "but the subject is an alphanumeric group item — a group-level VALUE literal shall be of the "
                    + "same category as the group item, or a figurative constant permitted in a MOVE statement to "
                    + "a receiving item of that category (ISO §13.18.63.3 SR13; §8.5.2.1 — an alphanumeric group "
                    + "item has class and category alphanumeric)");
                break;
            case PicCategory.Alphanumeric when isNumeric && IsNumericLiteralForm(raw):
                Edition.Removed(DiagnosticCatalog.ValueLiteralClass.Code, $"{where}: the VALUE literal {raw} is "
                    + "numeric but the item is alphabetic / alphanumeric / alphanumeric-edited — its VALUE literals "
                    + "shall be alphanumeric (ISO §13.18.63.3 SR4); under --permissive the literal's characters are "
                    + "stored as MOVE would store them");
                break;
            // ── SR4 sentences 2 and 3 — THE THIRD ARM of the size rule, which had no implementation at all while
            //    its national (SR5) and boolean (SR10) twins had theirs above: "Alphanumeric literals in the VALUE
            //    clause of an elementary item shall not exceed the size indicated by an explicit PICTURE clause.
            //    Alphanumeric literals in the VALUE clause of an alphanumeric group item shall not exceed the size
            //    of the group item." Measured on 1d949007, both silently TRUNCATED: `01 E1 PIC X(2) VALUE "ABCD".`
            //    displayed `AB`, and `01 GZ VALUE "ABCDEF". 05 O1 PIC X(2). 05 O2 PIC X(2).` displayed `ABCD`
            //    (kb/Work PB206). Scoped to a plain alphanumeric literal: a FIGURATIVE constant is repeated and
            //    truncated to the subject's size BY RULE (§8.3.3.6.4 GR2, which names the VALUE clause), so
            //    `01 GH VALUE ALL "XYZ".` over four positions is conforming source seeding `XYZX`.
            case PicCategory.Alphanumeric when lit is LiteralClass.Alphanumeric
                    && sizePositions is { } alnumLimit && CobolLiteral.Decode(raw).Length > alnumLimit:
                Edition.Error(DiagnosticCatalog.ValueLiteralOversize, $"{where}: the VALUE alphanumeric literal "
                    + $"({CobolLiteral.Decode(raw).Length} characters) exceeds the "
                    + $"{(groupSubject ? $"group item's {sizePositions} character positions"
                                       : $"item's {sizePositions} character positions indicated by its PICTURE clause")}"
                    + " (ISO §13.18.63.3 SR4)");
                break;
        }
        return raw;
    }

    /// <summary>ISO §13.18.63.3 SR2/SR3: a numeric VALUE literal on a fixed-point numeric subject shall be a
    /// permissible value within the range the PICTURE indicates, representable EXACTLY in the subject "without
    /// truncation of leading or trailing nonzero digits" (SR2), and a signed numeric literal requires a signed
    /// subject (SR3). Both are syntax rules ('shall'), so the only conforming response to a violation is a
    /// compile-time diagnostic (COBOLNET1625) — never the legacy lenient truncating/sign-dropping store, which the
    /// typed-native initializer would otherwise perform (ValueInitializer emits the literal verbatim through
    /// <c>EmitText.UnscaledAtScale</c>, which does NO high-order modulo and NO unsigned magnitude).
    /// <para>The NUMERIC-EDITED subject rides the same check (kb/Work PB97): SR6 converts its numeric literal "according
    /// to the rules for the MOVE statement, such that no truncation of digits or sign is required" and SR3 admits a
    /// signed literal only for "a numeric-edited data item with a representation of a sign". A fixed-point edited
    /// picture's stored span is its DIGIT POSITIONS (9 / Z / * / the floating-insertion digit positions) at the
    /// MASK's scale (the '.' or V, P-scaled — <c>CobolEdit.MaskScale</c>, the same scale every edited store aligns
    /// to); a FLOATING-POINT numeric-edited picture (D21/PB66) holds a literal iff its significand's nonzero digit
    /// run fits the mask's significand digits and its normalized exponent lies within the exponent field's range
    /// (<c>CobolEdit.FloatMask</c>, the ONE parser of the form).</para></summary>
    /// <remarks>
    /// The subject's stored ('9') digit positions are modeled by their power-of-ten exponents. Uniformly across an
    /// implied point (V), a leading-P, and a trailing-P scaled picture — given <see cref="PicInfo.Digits"/> stored
    /// digits and the signed <see cref="PicInfo.Scale"/> (trailing P ⇒ negative, leading P ⇒ &gt; Digits; ISO
    /// §13.18.40.4) — the lowest stored exponent is <c>-Scale</c> and the highest is <c>Digits - Scale - 1</c>. The
    /// literal is representable exactly iff every NONZERO digit of the literal lands within <c>[low, high]</c>; a
    /// nonzero digit above the high exponent is a leading-nonzero truncation, one below the low exponent a
    /// trailing-nonzero truncation. Zero (and a literal whose only nonzero digits are outside the tested span) is
    /// handled naturally: a literal of value zero is always representable and skipped. A picture-less numeric item
    /// (USAGE INDEX, Digits 0) has no digit positions to test and is skipped; SR3's negative-into-unsigned test is
    /// independent of the digit range. A COMP-5 / fixed-width binary item's Digits equals the decimal width of its
    /// range's maximum magnitude, so the digit test never false-rejects a capacity-valid value (its full
    /// binary-capacity range is a separate discipline, not SR2's PICTURE range).
    /// </remarks>
    private void ValidateNumericValue(PicInfo pic, string raw, string where)
    {
        bool edited = pic.Category is PicCategory.NumericEdited;
        string mask = pic.EditMask ?? "";
        if (edited ? pic.DigitPositions <= 0 && !pic.IsFloatEdited : pic.Digits <= 0) return;   // picture-less numeric (INDEX) — no digit positions
        string t = raw.Trim();
        if (t.Length == 0) return;
        bool neg = t[0] == '-';
        if (neg || t[0] == '+') t = t[1..];
        // A plain numeric literal is a digit run with at most one decimal point (the floating-point form of
        // §8.3.3.3.3 — significand E exponent — reaches only a floating-point numeric-edited subject here: a
        // fixed-point subject's ValidateValueCategory expanded it to its exact value first); figurative words, quoted
        // alphanumeric, and national/boolean literals are validated on their own paths and are out of scope here.
        int ePos = t.IndexOfAny(['E', 'e']);
        int exp10 = 0;
        if (ePos >= 0)
        {
            if (!NumericLiteral.IsFloatingPointForm(t)) return;
            exp10 = int.Parse(t[(ePos + 1)..], System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture);
            t = t[..ePos];
        }
        int dot = t.IndexOf('.');
        if (dot != t.LastIndexOf('.')) return;
        string digits = dot < 0 ? t : t.Remove(dot, 1);
        if (digits.Length == 0 || !digits.All(char.IsAsciiDigit)) return;

        // SR3 (ISO §13.18.63.3, ISO_COBOL.md:22914): a signed numeric literal requires a signed numeric subject or "a
        // numeric-edited data item with a representation of a sign" (an editing sign symbol +, -, CR or DB — the
        // floating-point form's significand sign included). Scoped to a NEGATIVE literal (the sign-losing case) — a
        // leading '+' into an unsigned item is the common harmless idiom (+5 == 5, no data loss) and is not rejected.
        // (a floating-point edited mask's exponent '+' is NOT a representation of the value's sign — only the
        // significand's own sign symbol is, CobolEdit.FloatMask.SigSign.)
        bool signBearing = pic.IsFloatEdited ? CobolEdit.FloatMask.Parse(mask, DecimalPointIsComma).SigSign != '\0'
            // A format-2 (LOCALE) item has no mask: its sign representation is the picture's '+' (§13.18.40.4
            // GR18 — Signed carries it; PB64 T6). Reading the empty mask here falsely rejected a legal negative
            // VALUE on a '+'-bearing format-2 picture.
            : pic.LocaleEdit is not null ? pic.Signed
            : edited
            ? mask.Any(c => c is '+' or '-') || mask.Contains("CR", StringComparison.OrdinalIgnoreCase) || mask.Contains("DB", StringComparison.OrdinalIgnoreCase)
            : pic.Signed;
        if (neg && !signBearing)
        {
            Edition.Error("COBOLNET1625", $"{where}: a signed (negative) numeric literal in the VALUE clause "
                + "requires a signed numeric or sign-bearing numeric-edited subject (ISO §13.18.63.3 SR3)");
            return;
        }

        int litScale = dot < 0 ? 0 : t.Length - dot - 1;    // the literal's fractional-digit count
        int firstNonZero = -1, lastNonZero = -1;
        for (int i = 0; i < digits.Length; i++)
            if (digits[i] != '0') { if (firstNonZero < 0) firstNonZero = i; lastNonZero = i; }
        if (firstNonZero < 0) return;                       // the literal is zero — always representable (SR2 / SR6)
        int last = digits.Length - 1;
        int highLitExp = exp10 - litScale + (last - firstNonZero); // exponent of the most-significant nonzero digit
        int lowLitExp = exp10 - litScale + (last - lastNonZero);   // exponent of the least-significant nonzero digit

        // A FLOATING-POINT numeric-edited subject (SR6, D21/PB66): the nonzero digit run is the significand the mask
        // must hold whole (no truncation of digits), and the value normalizes to a leading-nonzero significand
        // (§14.6.8.4 GR1) whose exponent shall fit the exponent field: value = 0.d1d2… × 10^(highLitExp + 1) →
        // E = highLitExp + 1 − (integer significand digits).
        if (pic.IsFloatEdited)
        {
            var fm = CobolEdit.FloatMask.Parse(mask, DecimalPointIsComma);
            int run = lastNonZero - firstNonZero + 1;
            int normExp = highLitExp + 1 - (fm.SigDigits - fm.SigScale);
            if (run > fm.SigDigits || normExp > fm.MaxExp || normExp < -fm.MaxExp)
                Edition.Error("COBOLNET1625", $"{where}: the numeric literal {raw.Trim()} in the VALUE clause is not "
                    + $"representable in the floating-point numeric-edited PICTURE {mask} without truncation — the "
                    + $"significand holds {fm.SigDigits} digit(s) (the literal has {run} significant digits) and the "
                    + $"exponent ranges over -{fm.MaxExp}..+{fm.MaxExp} (the normalized exponent is {normExp:+0;-0}) (ISO §13.18.63.3 SR6)");
            return;
        }

        // SR2 (ISO §13.18.63.3, ISO_COBOL.md:22906): the literal shall be a permissible value in the PICTURE range,
        // representable without truncation of a leading or trailing nonzero digit. Compare the literal's nonzero
        // digit-exponent span against the subject's stored-digit exponent span [-Scale, Digits-Scale-1] —
        // StoredShapeOf, the ONE fixed-point (digits, scale) geometry (kb/Work PB155 lifted it so the §14.7.7
        // composite aligns on the same shape).
        var (storedDigits, storedScale) = StoredShapeOf(pic);
        int lowStoredExp = -storedScale;
        int highStoredExp = storedDigits - storedScale - 1;
        if (highLitExp > highStoredExp || lowLitExp < lowStoredExp)
            Edition.Error("COBOLNET1625", edited
                ? $"{where}: the numeric literal {raw.Trim()} in the VALUE clause is not representable in the numeric-edited "
                    + $"PICTURE {mask} without truncation of leading or trailing nonzero digits (ISO §13.18.63.3 SR6)"
                : $"{where}: the numeric literal {raw.Trim()} in the VALUE clause is not a "
                    + "permissible value in the range the PICTURE indicates — it is not representable without truncation "
                    + "of leading or trailing nonzero digits (ISO §13.18.63.3 SR2)");
    }

    /// <summary>The stored digit-position span and P-signed scale of a FIXED-POINT item — plain numeric
    /// (Digits/Scale), numeric-edited (the MASK's geometry: DigitPositions less its P positions, at
    /// <c>CobolEdit.MaskScale</c> — the '.' or V, P-signed; SR6 "no truncation of digits or sign" spans the
    /// digit positions, P excluded because P scales and stores nothing), or locale-edited format 2
    /// (DigitPositions at the analyzer's Scale — there is no mask for MaskScale to read, and 'P' does not
    /// exist in format 2). The ONE (digits, scale) shape the §13.18.63.3 SR2/SR6 VALUE-fit check and the
    /// §14.7.7 r2 composite of operands both align on (kb/Work PB155). Callers exclude floating-point forms
    /// first (<see cref="PicInfo.IsFloat"/> / <see cref="PicInfo.IsFloatEdited"/> — no fixed decimal
    /// alignment exists for them).</summary>
    internal (int Digits, int Scale) StoredShapeOf(PicInfo pic)
    {
        bool edited = pic.Category is PicCategory.NumericEdited;
        string mask = pic.EditMask ?? "";
        return (edited ? pic.DigitPositions - mask.Count(c => c == 'P') : pic.Digits,
                pic.LocaleEdit is not null ? pic.Scale
                    : edited ? CobolEdit.MaskScale(mask, '$', DecimalPointIsComma) : pic.Scale);
    }

    // (The former private DecodeString twin is retired — all callers use CobolNet.Common.CobolLiteral.Decode,
    // the one ISO §8.3.3.1/§8.3.3.2 literal codec, PHASE-05 Step 1.)

    /// <summary>The most-recently-opened 01/77 record, so a following level-66 RENAMES attaches to its owner.</summary>
    private DataItem? _lastRoot;

    /// <summary>Index a named item in the <see cref="ByName"/> multimap (COBOL allows duplicate names disambiguated
    /// only by qualification).</summary>
    private void RegisterName(DataItem item)
    {
        if (item.CobolName is not { } name) return;
        ScreenRepositoryIntrinsicName(name, "data-name");
        if (!ByName.TryGetValue(name, out var list)) ByName[name] = list = [];
        list.Add(item);
    }

    private readonly HashSet<string> _repositoryNameReported = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>ISO §8.3.2.1 rule 5 — THE ONE screen for a user-defined word that spells an intrinsic-function-name
    /// "identified in a function-specifier in the REPOSITORY paragraph" (<c>FUNCTION name INTRINSIC</c>, or every
    /// catalogued name under <c>FUNCTION ALL INTRINSIC</c>): asked by every declaration funnel — a data-name
    /// (<see cref="RegisterName"/>), a condition-name, an index-name, a file-name, a paragraph or section name
    /// (<c>ProcedureTableBuilder</c>). Reported once per name (a TYPE expansion re-registers a clone). Returns true
    /// when the name is reserved. kb/Work PB65 (FMT-15.43.2 / FMT-15.58.2): the REPOSITORY sets were filled and
    /// consulted by NOTHING at declaration time, so a table named HIGHEST-ALGEBRAIC compiled and the keyword-omitted
    /// reference `HIGHEST-ALGEBRAIC(A1)` silently read the table — the standard's prohibition is exactly what makes
    /// §8.4.3.2.3 SR2's FUNCTION-less reference unambiguous, and the binder no longer substitutes a hand-written
    /// "the data item wins" precedence for it.</summary>
    internal bool ScreenRepositoryIntrinsicName(string name, string what)
    {
        bool reserved = RepositoryAllIntrinsic ? IntrinsicCatalog.TryGet(name, out _) : RepositoryIntrinsics.Contains(name);
        if (!reserved) return false;
        if (_repositoryNameReported.Add(name))
            Edition.Error(DiagnosticCatalog.RepositoryIntrinsicNameAsUserWord,
                $"{what} '{name}': the intrinsic-function-name is identified in a function-specifier of the REPOSITORY "
                + $"paragraph (FUNCTION {(RepositoryAllIntrinsic ? "ALL" : name)} INTRINSIC), so it shall not be used "
                + "as a user-defined word in this source unit (ISO §8.3.2.1 rule 5)");
        return true;
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
        using var _ = Edition.At(item);
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
        // The provenance of the VALUE travels with it (DataItem.ValueIsCopied): the §13.18.63.3 SR13/SR14
        // screen's subject is the entry that WROTE the VALUE clause, so a copied one must not re-report the
        // template's / target's already-screened entry once per reference site.
        if (to.RawValue is null && from.RawValue is not null) to.ValueIsCopied = true;
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
            DeclaredAt = src.DeclaredAt,
            CobolName = src.CobolName,
            CsName = Unique(src.CsName, newParent.Children.Select(c => c.CsName)),
            Pic = src.Pic,
            Pending = src.Pending,   // the deferred NATIONAL/BIT adjudication travels with the clone (P5.11c)
            OwnSign = src.OwnSign,
            OwnUsage = src.OwnUsage,
            RawValue = src.RawValue,
            ValueIsCopied = src.RawValue is not null,   // a clone never WROTE its VALUE (see DataItem.ValueIsCopied)
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
    /// (feedback_one_mechanism_per_job; SAME AS is structurally the TYPE expansion with a DATA-NAME source).
    /// Excluded from the copy per GR1: data-name-1's level-number, name, CONSTANT RECORD, EXTERNAL, GLOBAL,
    /// REDEFINES, and SELECT WHEN (not modeled). Subordinate levels renumber relative to the subject (GR2b/c —
    /// the <see cref="CloneItem"/> levelDelta). <paramref name="expanding"/> is the SAME-AS expansion chain —
    /// a target already on it is the SR3 cycle.</summary>
    private void ExpandSameAs(DataItem item, HashSet<DataItem> expanding)
    {
        using var _ = Edition.At(item);
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
    /// a strong root); only an EXTERNAL redefinition of a strong item is prohibited.
    /// <para><b>The carve-out survives §13.18.57.3 SR4's letter — kb/Work PB183's companion derivation, ANSWERED.</b>
    /// SR4 reads "If type-name-1 is described with the STRONG phrase, the subject of the entry shall not be implicitly
    /// or explicitly redefined in whole or in part", and the question was whether that overturns the internal carve-out.
    /// It does not, because SR4's SUBJECT is the entry CARRYING the TYPE clause, and §13.18.57.3 SR2 — "A data
    /// description entry in which a TYPE clause is specified shall not be followed immediately by a subordinate data
    /// description entry or a level 88 entry" — makes a subordinate REDEFINES under a TYPE entry syntactically
    /// unwritable. The only REDEFINES that can reach a strong subtree from outside is <c>01 X REDEFINES S.</c>, which
    /// the loop below already rejects. A REDEFINES written INSIDE the typedef TEMPLATE has no TYPE-clause subject at
    /// all, so SR4 never reaches it. PB179's cross-root narrowing was correct and needs no change; a positive golden
    /// (<c>pb183_redefines_in_strong_typedef_ok</c>) guards it against a later "tightening".</para></summary>
    internal void CheckStrongTypeDeclarations()
    {
        foreach (var item in AllItems())
            if (item.RedefinesTarget is { } tgt && StrongTypeModel.IsStronglyTyped(tgt)
                && !ReferenceEquals(StrongTypeModel.StrongRoot(item), StrongTypeModel.StrongRoot(tgt)))
            {
                using var _ = Edition.At(item);
                Edition.Error("COBOLNET1532", $"'{item.CobolName ?? item.CsName}' REDEFINES strongly-typed item "
                    + $"'{tgt.CobolName ?? tgt.CsName}': a strongly-typed item shall not be redefined in whole or in "
                    + "part (ISO §13.18.57.3 SR4)");
            }

        foreach (var owner in Roots)
            foreach (var ren66 in owner.Renames66)
                if (ren66.Renames is { } ri
                    && ((ri.From is { } f && StrongTypeModel.IsStronglyTyped(f))
                        || (ri.Thru is { } t && StrongTypeModel.IsStronglyTyped(t))
                        || ri.SpanLeaves.Any(StrongTypeModel.IsStronglyTyped)))
                {
                    using var _ = Edition.At(ren66);
                    Edition.Error("COBOLNET1532", $"RENAMES '{ren66.CobolName ?? ren66.CsName}' renames a "
                        + "strongly-typed item in whole or in part — prohibited (ISO §13.18.57.3 SR3)");
                }
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
            DeclaredAt = Edition.Cursor,
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
                                int? rangeSize = ValueSizePositions(rp, parent.IsDynamicLength, parent.IsAnyLength);
                                rawLo = ValidateValueCategory(rp, rawLo, $"condition-name '{name}'", rangeSize);
                                rawHi = ValidateValueCategory(rp, rawHi, $"condition-name '{name}'", rangeSize);
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
                                    raw = ValidateValueCategory(sp, raw, $"condition-name '{name}'",
                                        ValueSizePositions(sp, parent.IsDynamicLength, parent.IsAnyLength));
                                cond.Values.Add((raw, null));
                            }
                    }

        parent.Own88s.Add(cond);   // the item owns its 88s (source of truth; lets CloneItem carry a TYPEDEF's 88s)
        if (registerGlobal)
        {
            ScreenRepositoryIntrinsicName(name, "condition-name");   // §8.3.2.1 rule 5 (kb/Work PB65)
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
    /// <summary>A clause's source spelling with its words SEPARATED. <c>ctx.GetText()</c> concatenates the
    /// tokens ("FORMATCHARACTERDATA"), which is unreadable in a diagnostic whose whole job is to quote back
    /// what the user wrote — one join, at the two declined-clause sites that quote a multi-word clause.</summary>
    private static string Spelled(Antlr4.Runtime.Tree.IParseTree node) =>
        string.Join(' ', Enumerable.Range(0, node.ChildCount).Select(i => node.GetChild(i).GetText()));

    private DataItem? BindEntry(Core.DataDescriptionEntryContext entry)
    {
        DataDescriptionCst e = entry;
        if (e.Level is not { } level) return null;
        if (level is 66 or 88) return null; // RENAMES / condition-names: later slice.

        string? cobolName = e.Name;
        bool isFiller = cobolName is null || cobolName.Equals("FILLER", StringComparison.OrdinalIgnoreCase);
        string csName = isFiller ? $"_filler{_fillerCounter++}" : DataItem.Sanitize(cobolName!);

        string? pictureText = null, usageText = null, rawValue = null, redefinesTargetName = null;
        var groupUsage = GroupUsage.None;   // GROUP-USAGE (§13.18.29; D20/PB79)
        List<EditingPhraseSpec>? editingSpecs = null;   // PICTURE EDITING phrases (§13.18.40.2), threaded to PictureAnalyzer
        LocaleEditSpec? pictureLocale = null;           // PICTURE format 2 — the LOCALE phrase (§13.18.40.2; PB64 T6)
        List<TableValueSpec>? tableValues = null;       // Format 2 (table) VALUE phrases (§13.18.63.2)
        bool gluedMultiLiteral = false;                 // a Format-1 VALUE with >1 operand (no FROM) — the glued-list reject
        string? objectClassName = null;   // USAGE OBJECT REFERENCE class-name (null = universal; §13.18.60.4)
        int? occurs = null;
        OccursSpec? occursSpec = null;
        var indexNames = new List<string>();
        SignSpec? ownSign = null;
        bool justified = false, blankWhenZero = false, synchronized = false;
        bool binaryUnsigned = false;   // USAGE BINARY-CHAR/... UNSIGNED (SIGNED is the default, ISO §13.18.60.4 GR12)
        bool noSign = false;           // USAGE PACKED-DECIMAL WITH NO SIGN (ISO §13.18.60.4 GR11 — no sign nibble; 2023)
        // The entry's USAGE clause node, kept for the float FORMAT phrases (ISO §13.18.60.2 general format, 2014):
        // their applicability depends on the RESOLVED usage, so the whole phrase adjudication runs in one block
        // after entryUsage exists. kb/Work PB174.
        Core.UsageClauseContext? usageCtx = null;
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
                {
                    pictureText = picText;
                    editingSpecs = BuildEditingSpecs(clause.Context.pictureClause());   // PICTURE EDITING (§13.18.40.2)
                    // PICTURE Format 2 (locale) — `LOCALE [IS locale-name-1] SIZE IS integer-1` (§13.18.40.2; LIVE
                    // since kb/Work PB64 T6): capture the locale (SR37 through the ONE undeclared-locale-name path,
                    // COBOLNET1664; absent ⇒ the current locale at each edit, §13.18.40.5 r11) and SIZE integer-1
                    // (§13.18.40.4 GR17 — the item's length); the analysis is AnalyzeLocaleEdited's (SR33–SR36 +
                    // Table 11 → COBOLNET1673).
                    if (clause.Context.pictureClause()?.pictureLocalePhrase() is { } lp)
                    {
                        var lw = lp.cobolWord();   // [0] = the word LOCALE itself; [1] = locale-name-1 when written
                        var locale = LocaleRef.Current;
                        if (lw.Length > 1)
                        {
                            var sym = ResolveLocaleName(lw[1].GetText(),
                                $"data item '{cobolName ?? "FILLER"}' PICTURE … LOCALE {lw[1].GetText()}",
                                "ISO §13.18.40.3 SR37 — locale-name-1 shall be specified in the LOCALE clause in the SPECIAL-NAMES paragraph");
                            if (sym is not null) locale = new LocaleRef(sym);
                        }
                        int size = int.TryParse(lp.integerLiteral().GetText(), out int sz) && sz > 0 ? sz : 0;
                        if (size == 0)
                            Edition.Error(DiagnosticCatalog.PictureLocaleFormat2Violation,
                                $"data item '{cobolName ?? "FILLER"}': SIZE IS {lp.integerLiteral().GetText()} — "
                                + "integer-1 gives the item's character positions and shall be a nonzero unsigned "
                                + "integer (ISO §13.18.40.2 / §13.18.40.4 GR17)");
                        pictureLocale = new LocaleEditSpec(locale, Math.Max(1, size), "");
                    }
                }
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
                else if (clause.Context.selectWhenClause() is { } sw)
                    // SELECT WHEN clause (ISO §13.18.51) — Annex A.4.8 item 2), DECLINED (CONFORMANCE.md §5).
                    // Reported HERE, in BindEntry, because BindEntry is the ONE path every data description
                    // entry of every section reaches (BindEntries feeds it for file / working-storage /
                    // local-storage / linkage alike), so the refusal cannot be dodged by moving the record —
                    // and §13.18.51.3 SR1 admits the clause in all four of those sections. Never bound: an
                    // inert SELECT WHEN would silently select the wrong record description entry
                    // (§13.18.51.4 GR1/GR2).
                    Edition.Declined(DiagnosticCatalog.FormatSelectWhenUnclaimed,
                        $"the SELECT WHEN clause on '{cobolName ?? "FILLER"}' ({Spelled(sw)})");
                else if (clause.Context.groupUsageClause() is { } gu)
                    // GROUP-USAGE (ISO §13.18.29; D20/PB79). The COBOL-2002 introduction gate is VersionConformancePass
                    // ParseArm.VisitGroupUsageClause; SR1 (a group, not strongly typed, not variable-length) and the
                    // subordinate conformance (SR2/SR3) are adjudicated once the forest is complete (ResolveIndexItems);
                    // the no-explicit-USAGE half of SR2/SR3 is checked below, where both clauses are known.
                    groupUsage = gu.BIT() is not null ? GroupUsage.Bit : GroupUsage.National;
                else if (clause.Context.usageClause() is { } usage)
                {
                    usageText = UsageKeyword(usage);
                    // SIGNED (default) / UNSIGNED on a fixed-width binary usage (ISO §13.18.60.4 GR12) — the
                    // binarySign sibling is a direct child of usageClause in BOTH the full (USAGE IS
                    // BINARY-CHAR SIGNED) and the bare (BINARY-CHAR SIGNED) alternatives.
                    binaryUnsigned = usage.binarySign()?.UNSIGNED() is not null;
                    noSign = usage.noSignPhrase() is not null;   // §13.18.60.4 GR11 — validated against usage/picture below
                    // The item's OWN float format phrases (ISO §13.18.60.2, 2014) are adjudicated in ONE block
                    // below, where entryUsage is resolved (their applicability is per-usage) — the clause node is
                    // captured here rather than four derived locals, so §13.18.60.2's phrase rules stay in one
                    // place. kb/Work PB174.
                    usageCtx = usage;
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
                {
                    if (value.valueClauseTablePhrase() is { Length: > 0 } tphrases)
                        tableValues = BuildTableValueSpecs(tphrases);   // Format 2 (table) — §13.18.63.2
                    else
                    {
                        rawValue = ExtractValue(value);
                        // Format 1 takes EXACTLY ONE literal (§13.18.63.2); a bare multi-literal list (no FROM) is
                        // Format 2 or (report section) Format 4, never a Format-1 data item — ExtractValue GLUES it
                        // today (GetText over the collapsed valueItem). Flag it for a loud reject once entryWhere exists.
                        var vi0 = value.valueItem().FirstOrDefault();
                        if (vi0?.valueClauseRange() is null && (vi0?.valueClauseOperand().Length ?? 0) > 1)
                            gluedMultiLiteral = true;
                    }
                }
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

        // THE GLUED-MULTI-LITERAL REJECT (ISO §13.18.63.2): a Format-1 (data-item) VALUE takes exactly one literal;
        // a list needs Format 2's FROM (subscript) phrase. The greedy operand loop silently glued a bare list into
        // one corrupt value for the life of the tree — reject it loud, binding nothing.
        if (gluedMultiLiteral)
        {
            Edition.Error("COBOLNET1585", $"{entryWhere}: a data-item VALUE clause (Format 1) takes exactly one "
                + "literal; a list of literals requires the Format 2 table form's FROM (subscript) phrase "
                + "(ISO §13.18.63.2)");
            rawValue = null;
        }

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
                Edition.Error(DiagnosticCatalog.UsageClauseCompatibility, $"{entryWhere}: PICTURE may not be specified with USAGE "
                    + "PROGRAM-POINTER or FUNCTION-POINTER — the item is picture-less (ISO §13.16.3 SR8)");
                pictureText = null;
            }
            if (rawValue is not null)
            {
                Edition.Error(DiagnosticCatalog.UsageClauseCompatibility, $"{entryWhere}: the VALUE clause shall not be specified with a "
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
            Edition.Error(DiagnosticCatalog.UsageClauseCompatibility, $"{entryWhere}: PICTURE may not be specified with USAGE POINTER — "
                + "a data-pointer item is picture-less (ISO §13.18.60.4)");
            pictureText = null;
        }

        // USAGE POINTER TO type-name-1 — the RESTRICTED data-pointer (§13.18.60.2 general format; §13.18.60.4
        // GR23). §13.18.60.3 SR18 constrains the DECLARATION SHAPE, and it is the rule that makes the obvious
        // spelling illegal: "If type-name-1 is specified, the TYPEDEF clause shall be specified for the subject
        // of the entry." So a restricted data-pointer is declared as a TYPE DECLARATION and then referenced by a
        // TYPE clause — `01 P USAGE POINTER TO T.` is itself nonconforming. The 0881 declaration band, beside the
        // PROGRAM-POINTER declaration gates above. kb/Work PB153.
        var dpu = usageCtx?.usageKeyword()?.dataPointerUsage();
        string? restrictedTypeName = dpu?.TO() is not null ? dpu.cobolWord()?.GetText() : null;
        if (restrictedTypeName is not null && !isTypedef)
        {
            Edition.Error(DiagnosticCatalog.UsageClauseCompatibility, $"{entryWhere}: USAGE POINTER TO {restrictedTypeName} declares a "
                + "RESTRICTED data-pointer, and the TYPEDEF clause shall be specified for the subject of such an "
                + "entry (ISO §13.18.60.3 SR18) — declare the restricted pointer as a type declaration and "
                + "reference it with a TYPE clause");
            restrictedTypeName = null;
        }

        // ⛔ THE USAGE CLAUSE'S OWN FLOAT FORMAT PHRASES (ISO §13.18.60.2 general format, a COBOL-2014 addition;
        // kb/Work PB174). ONE block owns all three of the general format's rules over them, because the grammar
        // parses `floatFormatPhrase*` after ANY usageKeyword (the established binarySign/noSignPhrase superset
        // posture) and the narrowing is the binder's:
        //   • REPEAT — §5.2.6.4, "any single alternative may be specified only once" (COBOLNET1718). The
        //     FLOAT-DECIMAL-16/-34 tail is a BRACKETED CHOICE-INDICATOR group over { encoding-phrase,
        //     endianness-phrase }: zero or more, each at most once, in ANY order. The FLOAT-BINARY-* tail is a
        //     plain single bracket, so at most one endianness-phrase there too.
        //   • ENDIANNESS APPLICABILITY — the figure prints the endianness-phrase only on the five STANDARD float
        //     usages (COBOLNET1716); §13.18.60.4 GR19c/d scope the OPTIONS-implied phrase the same way, and
        //     GR13/GR21 leave the implementor-defined float usages (COMP-1/COMP-2/FLOAT-SHORT/-LONG/-EXTENDED) to
        //     our documented big-endian pin (Annex A.1 item 48).
        //   • ENCODING APPLICABILITY — the figure prints the encoding-phrase only on FLOAT-DECIMAL-16/-34
        //     (COBOLNET1717); GR20a says the same in prose ("any standard decimal floating-point usage").
        // The RESULT — the item's own endianness — then feeds PicInfo.FloatItem below as the phrase that WINS over
        // the OPTIONS-supplied implied one, which is not a preference but §11.9.8.3 SR2/SR3's own wording: the
        // clause is implied "for … any data item described with a standard binary floating-point usage IN WHICH AN
        // ENDIANNESS-PHRASE IS NOT SPECIFIED". §11.9.9.3 SR1-SR6 say it for the decimal usages.
        // A phrase on FLOAT-BINARY-128 / FLOAT-DECIMAL-16 / -34 PARSES and is applicable — those usages are
        // documented non-support (COBOLNET1564, Annex A.3 items 17/19), so the reject a user sees names the
        // non-support, never a raw parse error at the wrong stage.
        FloatEndianness itemEndianness = FloatEndianness.Unspecified;
        FloatEncoding itemEncoding = FloatEncoding.Unspecified;
        if (usageCtx is not null)
        {
            foreach (var phrase in usageCtx.floatFormatPhrase())
            {
                if (phrase.endiannessPhrase() is { } ep)
                {
                    if (itemEndianness is not FloatEndianness.Unspecified)
                        Edition.Error(DiagnosticCatalog.UsageFloatFormatPhraseRepeated, $"{entryWhere}: the "
                            + "endianness-phrase is specified more than once on this USAGE clause "
                            + "(ISO §5.2.6.4 — any single alternative may be specified only once)");
                    itemEndianness = FloatFormatPhrase.Endianness(ep);
                }
                else if (phrase.encodingPhrase() is { } enc)
                {
                    if (itemEncoding is not FloatEncoding.Unspecified)
                        Edition.Error(DiagnosticCatalog.UsageFloatFormatPhraseRepeated, $"{entryWhere}: the "
                            + "encoding-phrase is specified more than once on this USAGE clause "
                            + "(ISO §5.2.6.4 — any single alternative may be specified only once)");
                    itemEncoding = FloatFormatPhrase.Encoding(enc);
                }
            }
            if (itemEndianness is not FloatEndianness.Unspecified && !UsageFamilies.IsStandardFloat(entryUsage))
            {
                Edition.Error(DiagnosticCatalog.UsageEndiannessPhraseScope, $"{entryWhere}: an endianness-phrase "
                    + "(HIGH-ORDER-LEFT / HIGH-ORDER-RIGHT) is written only with a standard floating-point usage — "
                    + "FLOAT-BINARY-32/-64/-128 or FLOAT-DECIMAL-16/-34 — not USAGE "
                    + $"{usageText ?? "DISPLAY"} (ISO §13.18.60.2 general format; §13.18.60.4 GR19c/d)");
                itemEndianness = FloatEndianness.Unspecified;
            }
            if (itemEncoding is not FloatEncoding.Unspecified
                && !UsageFamilies.IsStandardDecimalFloat(entryUsage))
            {
                Edition.Error(DiagnosticCatalog.UsageEncodingPhraseScope, $"{entryWhere}: an encoding-phrase "
                    + "(BINARY-ENCODING / DECIMAL-ENCODING) is written only with a standard DECIMAL "
                    + "floating-point usage — FLOAT-DECIMAL-16 or FLOAT-DECIMAL-34 — not USAGE "
                    + $"{usageText ?? "DISPLAY"} (ISO §13.18.60.2 general format; §13.18.60.4 GR20a)");
                itemEncoding = FloatEncoding.Unspecified;
            }
            // A SURVIVING encoding-phrase has no consumer, and that is a MEASURED state, not an oversight: the
            // only usages it is applicable to — FLOAT-DECIMAL-16/-34 — are documented non-support (COBOLNET1564,
            // Annex A.3 item 19: .NET has no IEEE decimal64/128 type and GR17/GR18 pin the formats). The phrase
            // is screened here so the reject a user sees names that non-support rather than a parse error, and the
            // value is deliberately NOT carried onto PicInfo: a field nothing reads is a lookup nothing has ever
            // contradicted. It threads through when the usages themselves land.
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
            ? PictureAnalyzer.Analyze(pictureText, entryUsage, Edition, entryWhere, ownSign, currencies: CurrencySigns,
                blankWhenZero: blankWhenZero, explicitUsage: usageText is not null, editing: editingSpecs,
                localeFormat2: pictureLocale)
            : entryUsage is Usage.Index ? PicInfo.IndexItem
            : entryUsage is Usage.Pointer ? PicInfo.PointerItem(restrictedTypeName)
            : entryUsage is Usage.ProgramPointer ? PicInfo.ProgramPointerItem   // §13.18.60 GR24 (P10 Step 7)
            : entryUsage is Usage.ObjectReference ? PicInfo.ObjectReferenceItem(objectClassName)
            : entryUsage is Usage.BinaryChar or Usage.BinaryShort or Usage.BinaryLong or Usage.BinaryDouble
                ? PicInfo.BinaryItem(entryUsage, signed: !binaryUnsigned)
            // A PICTURE-less floating-point item (COMP-1/COMP-2/FLOAT-SHORT/-LONG/-EXTENDED + the 2014
            // FLOAT-BINARY-*/FLOAT-DECIMAL-* family, §13.18.60.2, D16) — its value is a native float/double, never
            // scaled-integer (before this the chain fell to null → NRE). The processor-dependent non-support forms
            // (binary128/decimal, rejected COBOLNET1564) still synthesize a Pic so the errored compile does not NRE.
            // The item's effective endianness rides in, resolved by ONE null-coalesce over the two-level
            // derivation §11.9.8.3 SR2/SR3 legislates: the ITEM's own endianness-phrase (§13.18.60.2, captured
            // above) where it stated one, else the unit's OPTIONS FLOAT-BINARY model (§11.9.8 — Options is bound
            // at the top of BindDeclarations, before any entry), whose clause is implied only "for … any data
            // item … IN WHICH AN ENDIANNESS-PHRASE IS NOT SPECIFIED". FloatItem then applies GR19a/b ONCE, for
            // the standard binary float usages only (kb/Work PB164 wave 2 + PB174).
            : entryUsage is Usage.Float or Usage.Double or Usage.FloatShort or Usage.FloatLong or Usage.FloatExtended
                or Usage.FloatBinary32 or Usage.FloatBinary64 or Usage.FloatBinary128
                or Usage.FloatDecimal16 or Usage.FloatDecimal34
                ? PicInfo.FloatItem(entryUsage, itemEndianness is FloatEndianness.Unspecified
                    ? Options.FloatBinaryEndianness : itemEndianness)
            : null;   // incl. a PICTURE-less USAGE NATIONAL/BIT entry — Pending (below) carries its adjudication

        // A PICTURE-less USAGE NATIONAL/BIT entry is a GROUP header (legal — the usage sheds to subordinates,
        // §13.18.60.4 GR1) or an illegal picture-less elementary item (0881) — unknowable until the forest is
        // complete: ResolveIndexItems adjudicates via this EXPLICIT mark (P5.11c; formerly the reference-identity
        // sentinel PicInfos NationalUsagePending/BitUsagePending).
        var pending = pic is null && entryUsage is Usage.National ? PicPending.NationalUsage
            : pic is null && entryUsage is Usage.Bit ? PicPending.BitUsage
            : PicPending.None;
        // GROUP-USAGE (§13.18.29.3 SR2/SR3; D20/PB79): "USAGE BIT / NATIONAL is implied for the subject of the entry.
        // A USAGE clause shall not be explicitly specified for the subject" — the implied usage rides the SAME
        // Pending adjudication a group-level USAGE BIT / NATIONAL clause rides (§13.18.60.4 GR1: the usage sheds to
        // every subordinate leaf, checked and applied in ResolveIndexItems), so the two spellings share one walk.
        if (groupUsage is not GroupUsage.None)
        {
            if (usageText is not null)
                Edition.Error(DiagnosticCatalog.GroupUsageRule, $"{entryWhere}: a USAGE clause shall not be explicitly "
                    + $"specified for the subject of a GROUP-USAGE {(groupUsage is GroupUsage.Bit ? "BIT" : "NATIONAL")} "
                    + "entry — the usage is implied by the GROUP-USAGE clause (ISO §13.18.29.3 "
                    + $"{(groupUsage is GroupUsage.Bit ? "SR2" : "SR3")})");
            else if (pic is not null)
                Edition.Error(DiagnosticCatalog.GroupUsageRule, $"{entryWhere}: the GROUP-USAGE clause may be specified "
                    + "only if the subject of the entry is a group item — this entry has a PICTURE clause "
                    + "(ISO §13.18.29.3 SR1)");
            else
                pending = groupUsage is GroupUsage.Bit ? PicPending.BitUsage : PicPending.NationalUsage;
        }

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
        // COBOL-85 and 31 by 2002+ (ISO §8.3.3.3.2 / §13.18.40) — reject, never silently mis-store.
        // §13.18.40.3 SR14: the 1–31 (18 pre-2002) cap is measured against DIGIT POSITIONS, not just the '9' count —
        // a numeric-edited Z(11)9(8) is 19 positions and Z(35) is 35 (Digits=0), both of which the old '9'-only
        // Digits check let slip past. DigitPositions == Digits for pure-numeric-without-P, so no regression. (CA33.)
        // §13.18.40.3 SR14 reaches category numeric and FIXED-POINT numeric-edited items; the floating-point form's
        // capacity is SR15's 1..36 significand digits, checked by the analyzer (kb/Work PB66 — DigitPositions is 0 there).
        if (pic is { Category: PicCategory.Numeric or PicCategory.NumericEdited, IsFloat: false, IsFloatEdited: false } && pic.DigitPositions > 0)
            Edition.CheckDigitCapacity(pic.DigitPositions, $"data item '{cobolName ?? "FILLER"}' (PICTURE {pictureText})");
        // §13.18.52.3 SR1: a SIGN clause needs a picture with the symbol S — a floating-point edited picture cannot
        // carry one (its significand's sign is a fixed-insertion editing symbol, Table 8), so the clause is illegal here.
        if (pic is { IsFloatEdited: true } && ownSign is not null)
            Edition.Error(DiagnosticCatalog.PictureFloatEdited, $"{entryWhere}: the SIGN clause may be specified only for a "
                + "numeric entry whose picture contains the symbol S — a floating-point numeric-edited picture has none "
                + "(ISO §13.18.52.3 SR1; §13.18.40.6 Table 10 row E)");
        // §13.16.3 SR19 — the SIGN clause shall not be specified with a format-2 (LOCALE) PICTURE: the sign
        // representation is the LOCALE's (§13.18.40.5 r13). The SCREEN description twin (§13.17.3 SR9) rides the
        // screen arm; a REPORT GROUP entry carries NO such rule (§13.15.3) and the pair is legal there (PB113).
        if (pic is { LocaleEdit: not null } && ownSign is not null)
            Edition.Error(DiagnosticCatalog.SignClauseWithLocalePicture, $"{entryWhere}: the SIGN clause shall not be "
                + "specified when the LOCALE phrase of the PICTURE clause is specified (ISO §13.16.3 SR19)");

        // THE VALUE-CLAUSE LITERAL SCREEN — one funnel, every format (see ScreenValueLiteral). The format-2
        // (table) literals ride the SAME call from ValidateTableValues, per occurrence (kb/Work PB208), and the
        // SIZE the SR4/SR5/SR10 sentences measure against travels with the literal (kb/Work PB206) — null for a
        // DYNAMIC LENGTH / ANY LENGTH subject, whose PICTURE indicates a class and no size.
        if (rawValue is { } rv && pic is not null)
            rawValue = ScreenValueLiteral(pic, rv, entryWhere, ValueSizePositions(pic, isDynamicLength, isAnyLength));
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
            // §13.18.40.3 SR32 (same-entry half): a format 2 PICTURE clause shall not be specified in a data item
            // described with the CONSTANT RECORD clause. Its OWN rule and code (the rule lives in §13.18.40.3,
            // not §13.16.3, so not ConstantRecordRule); the subordinate half checks in BindEntries, where the
            // parent chain exists.
            if (isConstantRecord && pic is { LocaleEdit: not null })
                Edition.Error(DiagnosticCatalog.PictureLocaleFormat2Violation, $"{entryWhere}: a format 2 PICTURE "
                    + "clause shall not be specified in a data item described with the CONSTANT RECORD clause "
                    + "(ISO §13.18.40.3 SR32)");
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
            TableValues = tableValues,
            Occurs = occurs,
            OccursSpec = occursSpec,
            RedefinesTargetName = redefinesTargetName,
            GroupUsage = pic is null ? groupUsage : GroupUsage.None,   // §13.18.29 (D20/PB79); never on an elementary item
            DeclaredAt = Edition.Cursor,   // the entry cursor (kb/Work PB82) — where post-build passes report
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
        ValidateTableValues(item, entryWhere);   // Format 2 (table) VALUE SR18–SR22 (§13.18.63.3)

        // BASED declaration validation (the 0881 declaration-entry band; Phase-4b increment 2): §13.16 SR16 —
        // a BASED entry is a level-01/77 record-description entry (WS/LS/LINKAGE; the file-subsystem sweep is
        // a named residue); §13.18.5 SRs — REDEFINES and BASED are mutually exclusive (:17215) and a VALUE
        // clause cannot seed storage the item does not own. Violations clear the flag so the item binds as
        // ordinary storage under an already-failed compile (never a half-based state).
        if (isBased)
        {
            if (level is not (1 or 77))
            {
                Edition.Error(DiagnosticCatalog.UsageClauseCompatibility, $"{entryWhere}: the BASED clause may be specified only in a "
                    + "level-01 or level-77 entry (ISO §13.16 SR16 / §13.18.5)");
                isBased = false;
            }
            else if (redefinesTargetName is not null)
            {
                Edition.Error(DiagnosticCatalog.UsageClauseCompatibility, $"{entryWhere}: BASED and REDEFINES may not be specified "
                    + "together (ISO §13.18.5 SR)");
                isBased = false;
            }
            else if (hasExternal)
            {
                // §13.16.3 SR5: "The EXTERNAL clause shall not be specified in the same data description
                // entry as the REDEFINES or BASED clause" — without this, BOTH mechanisms would emit a
                // bridge under the ONE BackingCsName (a CS0102 duplicate member, the review finding).
                Edition.Error(DiagnosticCatalog.UsageClauseCompatibility, $"{entryWhere}: BASED and EXTERNAL may not be specified "
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
            ScreenRepositoryIntrinsicName(idxName, "index-name");   // §8.3.2.1 rule 5 (kb/Work PB65)
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
                // USAGE POINTER [TO type-name] (§13.18.60.2) is a RULE for the same reason: the restricted form's
                // type-name would otherwise glue into the keyword ("POINTERT") and reach ParseUsage's
                // internal-error arm. kb/Work PB153.
                : kw.dataPointerUsage() is not null ? "POINTER"
                : kw.GetText();
        // Unreachable since `[USAGE IS] usageKeyword` became ONE alternative (kb/Work PB95): every spelling, bare
        // or prefixed, carries the usageKeyword node.
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
                || op0.nonNumericLiteral()?.figurativeConstant()?.allLiteral() is { } al0 && al0.allLiteralOperand().Length > 1   // ALL over a concatenated literal-1 (PB71)
                || op0.nonNumericLiteral()?.figurativeConstant()?.cobolWord() is not null   // ALL symbolic-character-1 (PB110)
                || SymbolicValueRawText(op0) is not null
                || ConstantValueRawText(op0) is not null))
            return RawValueOperandText(op0);
        return item.GetText() is { } raw ? NormalizeIfNumericLiteral(raw) : null;
    }

    /// <summary>Bind an FD CODE-SET clause (ISO §13.18.13; kb/Work PB110 — it was parsed as the '85 one-name form
    /// and read by nothing): both formats — <c>IS alphabet-name-1 [alphabet-name-2]</c> and the FOR ALPHANUMERIC /
    /// FOR NATIONAL phrases (one or both, any order, each at most once — §5.2.6.4). SR1/SR2: each name shall
    /// reference an alphabet defining a coded character set of its class (a LOCALE alphabet is COBOLNET1669 through
    /// the ONE resolver; a class mismatch or an undeclared name is COBOLNET1672). SR3: with record description
    /// entries and no SELECT WHEN, one class only, every elementary item of that usage, signed items SIGN SEPARATE.
    /// GR2/GR6: the on-medium coded character set — the sets whose implementor correspondence is the IDENTITY
    /// (NATIVE; STANDARD-1/2 on the ASCII-coincident native set — GR7 c; UTF-16 on the D-N1 substrate) convert as
    /// the identity and are CLAIMED; a set whose on-medium representation would differ (a literal-phrase alphabet's
    /// remapped ordinals; UTF-8 / UCS-4 as variable-width medium encodings) is the DOCUMENTED A.3 item 27
    /// non-support (COBOLNET1672 — "dependent upon a device capable of supporting the specified code";
    /// CONFORMANCE.md §2 row 27), never a silent identity.</summary>
    private void BindCodeSetClause(Core.CodeSetClauseContext cs, FileModel file, IReadOnlyList<DataItem> records)
    {
        using var _ = Edition.At(cs);
        string? alnumName = null, natName = null;
        if (cs.codeSetForPhrase() is { Length: > 0 } fors)
        {
            foreach (var f in fors)
            {
                bool nat = f.NATIONAL() is not null;
                ref string? slot = ref nat ? ref natName : ref alnumName;
                if (slot is not null)
                    Edition.Error(DiagnosticCatalog.CodeSetClauseViolation, $"CODE-SET FOR {(nat ? "NATIONAL" : "ALPHANUMERIC")} "
                        + "is specified more than once — each alternative of the clause's brace shall be specified at most "
                        + "once (ISO §13.18.13.2 / §5.2.6.4)");
                slot = f.cobolWord().GetText();
            }
        }
        else
        {
            alnumName = cs.cobolWord(0).GetText();
            natName = cs.cobolWord().Length > 1 ? cs.cobolWord(1).GetText() : null;
        }
        var alnumSet = alnumName is null ? null : CodedCharacterSetOf(alnumName, $"CODE-SET … {alnumName}",
            "ISO §13.18.13.3 SR1 — alphabet-name-1 shall reference an alphabet that defines an alphanumeric coded character set");
        var natSet = natName is null ? null : CodedCharacterSetOf(natName, $"CODE-SET … {natName}",
            "ISO §13.18.13.3 SR2 — alphabet-name-2 shall reference an alphabet that defines a national coded character set");
        if (alnumName is not null && alnumSet is null || natName is not null && natSet is null) return;
        if (alnumSet is { National: true })
        {
            Edition.Error(DiagnosticCatalog.CodeSetClauseViolation, $"CODE-SET … {alnumName}: alphabet-name-1 shall "
                + "reference an alphabet that defines an ALPHANUMERIC coded character set — this alphabet is defined "
                + "FOR NATIONAL (ISO §13.18.13.3 SR1)");
            return;
        }
        if (natSet is { National: false })
        {
            Edition.Error(DiagnosticCatalog.CodeSetClauseViolation, $"CODE-SET … {natName}: alphabet-name-2 shall "
                + "reference an alphabet that defines a NATIONAL coded character set — this alphabet is alphanumeric "
                + "(ISO §13.18.13.3 SR2)");
            return;
        }
        // SR3 — record description entries and no SELECT WHEN (none exists in this grammar): one class only.
        if (records.Count > 0 && alnumName is not null && natName is not null)
            Edition.Error(DiagnosticCatalog.CodeSetClauseViolation, "CODE-SET: if any record description entries are "
                + "associated with the file and no SELECT WHEN clauses are specified, either alphabet-name-1 or "
                + "alphabet-name-2 may be specified, but not both (ISO §13.18.13.3 SR3)");
        // GR2/GR6 — which conversion the set asks for. The identity-correspondence sets are CLAIMED (the conversion
        // IS the identity, byte-for-byte); a set that names a genuinely DIFFERENT on-medium code is the documented
        // A.3 item 27 non-support — refused loudly, never a silent identity (that would be a wrong answer for an
        // EBCDIC-shaped alphabet).
        foreach (var (set, name) in new[] { (alnumSet, alnumName), (natSet, natName) })
            if (set is not null && (set.Table is not null || set.NatTable is not null || set.Phrase is "UTF-8" or "UCS-4"))
                Edition.Error(DiagnosticCatalog.CodeSetClauseViolation, $"CODE-SET … {name}: the {set.Phrase} coded "
                    + "character set's on-medium representation differs from the native encoding, and this processor "
                    + "does not provide alternate device code sets (Annex A §A.3 item 27 — documented non-support, "
                    + "CONFORMANCE.md §2 row 27); NATIVE, STANDARD-1, STANDARD-2 and UTF-16 convert as the identity");
        // SR3 a/b — the selected class's usage over every elementary record item; signed numeric SIGN SEPARATE.
        if (records.Count > 0 && (alnumName is not null || natName is not null))
        {
            bool wantNational = natName is not null;
            void CheckItem(DataItem it)
            {
                foreach (var child in it.Children) CheckItem(child);
                if (it.Pic is not { } pic || it.Children.Count > 0) return;   // elementary items only (SR3 a/b)
                bool right = !wantNational && pic.Usage is Usage.Display || wantNational && pic.Usage is Usage.National;
                if (!right)
                    Edition.Error(DiagnosticCatalog.CodeSetClauseViolation, $"CODE-SET: the record item "
                        + $"'{it.CobolName}' is not usage {(wantNational ? "NATIONAL" : "DISPLAY")} — all elementary "
                        + $"data items of all record description entries shall be described as usage "
                        + $"{(wantNational ? "national" : "display")} (ISO §13.18.13.3 SR3 {(wantNational ? "b" : "a")})");
                else if (pic is { Signed: true } sp && !sp.SignKind.Contains("Separate"))
                    Edition.Error(DiagnosticCatalog.CodeSetClauseViolation, $"CODE-SET: the signed numeric record "
                        + $"item '{it.CobolName}' shall be described with the SIGN IS SEPARATE clause "
                        + $"(ISO §13.18.13.3 SR3 {(wantNational ? "b" : "a")})");
            }
            foreach (var rec in records) CheckItem(rec);
        }
    }

    /// <summary>The RAW single-literal text of a VALUE operand — the data path's currency (decoded at emit
    /// time): a §8.8.3 concatenation expression folds to its equivalent literal's raw text (§8.8.3.3 GR3); a
    /// constant-name substitutes its literal's raw text (§13.10.3 SR2 / §13.10.4 GR1 — a VALUE operand is a
    /// literal position); any other operand keeps its source text, numeric literals normalized to dot-decimal
    /// (ISO §12.3.7 GR14a).</summary>
    private string RawValueOperandText(Core.ValueClauseOperandContext op) =>
        op.nonNumericLiteral()?.concatenationExpression() is { } ce
            ? ConcatFolder.Fold(ce, Edition, Collating, NationalCollating).RawText
            // ALL over a concatenated literal-1 (§8.3.3.6.3 SR2 — kb/Work PB71): `ALL` + the folded literal re-quoted,
            // so the raw-text ALL reader (CobolLiteral.AllLiteralRaw) sees ONE literal of the right class.
            : op.nonNumericLiteral()?.figurativeConstant()?.allLiteral() is { } al && al.allLiteralOperand().Length > 1
            ? "ALL" + ConcatFolder.FoldAll(al).RawText
            // [ALL] symbolic-character-1 (§8.3.3.6.2 Format 7; §12.3.7.4 GR11 — kb/Work PB110): the figurative's ONE
            // character as an ALL literal of its class — GR2's fill in a VALUE association, exactly like ALL "c".
            : op.nonNumericLiteral()?.figurativeConstant()?.cobolWord() is { } symAll && SymbolicRaw(symAll.GetText()) is { } rawAll
            ? rawAll
            : SymbolicValueRawText(op) is { } rawBare ? rawBare
            : ConstantValueRawText(op) is { } konst ? konst
            : NormalizeIfNumericLiteral(op.GetText());

    /// <summary>The raw <c>ALL"c"</c> text a symbolic-character VALUE operand substitutes (bare-word form), or
    /// null when the operand names no symbolic character (kb/Work PB110; the ConstantValueRawText shape).</summary>
    private string? SymbolicValueRawText(Core.ValueClauseOperandContext op)
    {
        Antlr4.Runtime.Tree.IParseTree? n = op.unaryExpression();
        while (n is not null and not Core.DataReferenceContext)
            n = n.ChildCount == 1 ? n.GetChild(0) : null;
        return n is Core.DataReferenceContext dref && SymbolicOf(dref) is { } sym ? SymbolicRaw(dref.GetText()) : null;
    }

    /// <summary>The raw ALL-literal text of the symbolic character named <paramref name="word"/>, or null: the
    /// class-prefixed re-quoted one-character literal (embedded delimiters doubled per §8.3.3.2.3 r3).</summary>
    internal string? SymbolicRaw(string word) =>
        SymbolicOf(word) is not { } sym ? null
        : "ALL" + (sym.National ? "N" : "") + "\"" + sym.Value.Replace("\"", "\"\"") + "\"";

    /// <summary>Build the <see cref="EditingPhraseSpec"/> list for a PICTURE clause's EDITING phrases
    /// (ISO §13.18.40.2 Format 1) — DECODED character-1 + literal text, handed to <see cref="PictureAnalyzer"/>
    /// for SR8–SR12 validation and render-rule construction. Null when the clause carries no EDITING phrase.</summary>
    private static List<EditingPhraseSpec>? BuildEditingSpecs(Core.PictureClauseContext? pic)
    {
        var phrases = pic?.editingPhrase();
        if (phrases is null || phrases.Length == 0) return null;
        var list = new List<EditingPhraseSpec>(phrases.Length);
        foreach (var ph in phrases)
        {
            var lits = ph.literal();
            string char1 = DecodeEditLiteral(lits.Length > 0 ? lits[0] : null) ?? "";
            if (ph.editingForPhrase() is { } forp)
            {
                // FOR (extended sign control): map the literals to NEGATIVE / POSITIVE by keyword position (either
                // order is legal — §13.18.40.2 choice indicators). The keyword that appears first owns literal[0].
                var flits = forp.literal();
                var neg = forp.NEGATIVE();
                var pos = forp.POSITIVE();
                bool negFirst = neg is not null && (pos is null || neg.Symbol.TokenIndex < pos.Symbol.TokenIndex);
                string? negLit, posLit;
                if (negFirst)
                {
                    negLit = DecodeEditLiteral(flits.Length > 0 ? flits[0] : null);
                    posLit = pos is not null && flits.Length > 1 ? DecodeEditLiteral(flits[1]) : null;
                }
                else
                {
                    posLit = DecodeEditLiteral(flits.Length > 0 ? flits[0] : null);
                    negLit = neg is not null && flits.Length > 1 ? DecodeEditLiteral(flits[1]) : null;
                }
                list.Add(new EditingPhraseSpec(char1, Simple: null, Neg: negLit, Pos: posLit, IsForForm: true));
            }
            else
            {
                // IS (simple insertion): literal(1) is literal-1 (literal(0) is character-1).
                list.Add(new EditingPhraseSpec(char1,
                    Simple: DecodeEditLiteral(lits.Length > 1 ? lits[1] : null), Neg: null, Pos: null, IsForForm: false));
            }
        }
        return list;
    }

    /// <summary>Decode a PICTURE EDITING literal (character-1 or an insertion literal). A quoted alphanumeric /
    /// national / hex literal decodes to its content; any other shape (numeric, figurative, concatenation) is
    /// returned raw so <see cref="PictureAnalyzer"/>'s SR8/SR9 checks reject it with a named diagnostic.</summary>
    private static string? DecodeEditLiteral(Core.LiteralContext? lit)
    {
        if (lit is null) return null;
        var nn = lit.nonNumericLiteral();
        return nn?.STRINGLIT() is not null || nn?.NATLIT() is not null || nn?.HEXLIT() is not null
            ? CobolLiteral.Decode(lit.GetText())
            : lit.GetText();
    }

    /// <summary>Build the <see cref="TableValueSpec"/> list for a Format 2 (table) VALUE clause (ISO §13.18.63.2):
    /// each phrase's literal list (raw operand text, concat/constant folded — the Format-1 currency) plus its FROM
    /// (subscript-1 …) and optional TO (subscript-2 …) integer subscripts. The subscripts are split by the TO
    /// token's position (FROM's precede it, TO's follow).</summary>
    private List<TableValueSpec> BuildTableValueSpecs(Core.ValueClauseTablePhraseContext[] phrases)
    {
        var list = new List<TableValueSpec>(phrases.Length);
        for (int i = 0; i < phrases.Length; i++)
        {
            var ph = phrases[i];
            var literals = ph.valueClauseOperand().Select(RawValueOperandText).ToList();
            int toIdx = ph.TO()?.Symbol.TokenIndex ?? int.MaxValue;
            var from = new List<int>();
            List<int>? to = ph.TO() is not null ? [] : null;
            foreach (var il in ph.integerLiteral())
            {
                int v = int.TryParse(il.GetText(), out int n) ? n : 0;
                if (il.Start.TokenIndex < toIdx) from.Add(v); else to!.Add(v);
            }
            list.Add(new TableValueSpec(literals, from, to, i));
        }
        return list;
    }

    /// <summary>Validate a Format 2 (table) VALUE (ISO §13.18.63.3 SR18–SR22, plus the ALL-FORMATS literal screen
    /// SR16 carries in). LANDABLE scope: a SINGLE-dimension table VALUE on the SAME entry that carries the OCCURS
    /// clause (fixed or dynamic). A multi-dimension odometer or a table VALUE on an item SUBORDINATE to the OCCURS
    /// is recognized but its per-occurrence path threading is not yet implemented — staged loud (COBOLNET0899, P14
    /// GAP), TableValues cleared so the emitter skips it.
    /// <para>⚠ Runs at ENTRY BIND, so <see cref="DataItem.Children"/> is still empty and
    /// <see cref="DataItem.IsGroup"/> is FALSE for every subject — the elementary-vs-group discrimination cannot be
    /// made here (measured: adding the conjunct changed nothing). A GROUP entry's format-2 VALUE is a group-level
    /// VALUE, and §13.18.63.3 SR16 carries SR13/SR14 onto it; its subject screen is
    /// <c>CheckGroupValueDeclarations</c>, which already reads BOTH VALUE carriers, and its area deposit is
    /// <c>GroupValueSlicer.AreaTextOf</c> (§13.18.63.4 GR5). Both emit lanes guard the shape with the SAME
    /// <c>!IsGroup</c> predicate at a point where the forest exists (<see cref="ValueInitializer.FieldInit"/>,
    /// <see cref="GroupImageCodec.ImageInitOf"/>).</para></summary>
    private void ValidateTableValues(DataItem item, string where)
    {
        if (item.TableValues is not { Count: > 0 } specs) return;

        bool sameItemTable = item.Occurs is not null || item.IsDynamicTable;
        bool singleDim = specs.All(s => s.From.Count == 1 && (s.To is null || s.To.Count == 1));
        if (!sameItemTable || !singleDim)
        {
            Edition.Error(DiagnosticCatalog.ConstructStagedNotImplemented, $"{where}: a Format 2 (table) VALUE clause "
                + "is recognized but currently supported only on a single-dimension table's own OCCURS entry — a "
                + "multi-dimension or subordinate-item table VALUE is not yet implemented (ISO §13.18.63.2; P14 GAP)");
            item.TableValues = null;
            return;
        }

        // The physical maximum: fixed = Occurs; dynamic = the OCCURS TO expected capacity (null ⇒ unbounded).
        int? max = item.Occurs ?? item.OccursSpec?.ExpectedMax;
        bool dynamicNoTo = item.IsDynamicTable && item.OccursSpec?.ExpectedMax is null;
        foreach (var s in specs)
        {
            int from = s.From[0];
            if (from < 1 || (max is { } mx && from > mx))
                Edition.Error("COBOLNET1586", $"{where}: a Format 2 VALUE FROM subscript ({from}) is out of range "
                    + $"1..{(max?.ToString() ?? "the expected capacity")} (ISO §13.18.63.3 SR20)");
            if (s.To is { } toList)
            {
                int t = toList[0];
                if (t < 1 || (max is { } mx2 && t > mx2))
                    Edition.Error("COBOLNET1587", $"{where}: a Format 2 VALUE TO subscript ({t}) is out of range "
                        + $"1..{(max?.ToString() ?? "the expected capacity")} (ISO §13.18.63.3 SR21)");
                else if (t < from)
                    Edition.Error("COBOLNET1587", $"{where}: a Format 2 VALUE TO subscript ({t}) is less than its "
                        + $"FROM subscript ({from}) — subscript-2 shall be the same or a successive occurrence "
                        + "(ISO §13.18.63.3 SR21)");
            }
            else if (dynamicNoTo)
                Edition.Error("COBOLNET1588", $"{where}: a Format 2 VALUE with no TO phrase is not permitted on an "
                    + "OCCURS DYNAMIC table declared without an OCCURS TO (expected) capacity (ISO §13.18.63.3 SR22)");
        }

        // ⛔ THE ALL-FORMATS LITERAL SCREEN, ONE OCCURRENCE-LITERAL AT A TIME (kb/Work PB208). §13.18.63.3 SR2 —
        // "If the category of the subject of the entry is numeric, all literals in the VALUE clause shall be
        // numeric and shall be permissible values within the range indicated by the PICTURE clause or the USAGE
        // clause" — is an ALL FORMATS rule, and SR16 carries SRs 10–15 in as well, so a format-2 literal is
        // governed exactly as the format-1 one. It was not: BuildTableValueSpecs' literals went straight to the
        // emitter, so `05 B PIC 9(4) COMP OCCURS 2 VALUE "0012" FROM (1) TO (2).` compiled clean at strict 2023
        // where its format-1 twin is COBOLNET1657. Routing through the SAME funnel the format-1 RawValue takes
        // (ScreenValueLiteral) makes the two formats answer alike BY CONSTRUCTION, and the text it returns is
        // what gets stored — including the --permissive numeric rewrite of a class-mismatched literal, so the
        // emitter's per-occurrence rawOverride carries a screened literal exactly as item.RawValue does.
        if (item.Pic is not { } subject) return;
        // The size §13.18.63.3 SR4/SR5/SR10 measure each occurrence-literal against is the ELEMENT's
        // (kb/Work PB206) — computed once, exactly as the format-1 call site computes it, so the two formats
        // cannot disagree about a size any more than they can about a category.
        int? elementSize = ValueSizePositions(subject, item.IsDynamicLength, item.IsAnyLength);
        var screened = new List<TableValueSpec>(specs.Count);
        bool rewritten = false;
        foreach (var s in specs)
        {
            var lits = new List<string>(s.Literals.Count);
            foreach (string lit in s.Literals)
            {
                string kept = ScreenValueLiteral(subject, lit, $"{where}, Format 2 VALUE FROM ({s.From[0]})",
                    elementSize);
                rewritten |= !string.Equals(kept, lit, StringComparison.Ordinal);
                lits.Add(kept);
            }
            screened.Add(s with { Literals = lits });
        }
        if (rewritten) item.TableValues = screened;
    }

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
            using var _ = Edition.At(item);
            bool isIndex = ReferenceEquals(item.Pic, PicInfo.IndexItem) || (inherited && item.Pic is null);
            // USAGE OBJECT REFERENCE inherits the same way (§13.18.60.4 GR1): a group header sheds its
            // synthesized reference profile; a PICTURE-less leaf below takes it (sharing the immutable
            // PicInfo — the declared class flows down with it).
            var objRef = item.Pic is { Category: PicCategory.ObjectReference } p ? p : inheritedObjRef;
            if (item.Children.Count > 0)
            {
                // GROUP-USAGE (ISO §13.18.29; D20/PB79) — SR1's forest-dependent halves, and SR2/SR3's "all subordinate
                // group items shall be explicitly or implicitly described with GROUP-USAGE BIT / NATIONAL": a
                // subordinate group inherits, one that declares the OTHER usage is a violation.
                if (item.GroupUsage is not GroupUsage.None)
                {
                    string gu = item.GroupUsage is GroupUsage.Bit ? "BIT" : "NATIONAL";
                    if (StrongTypeModel.IsStrongGroup(item))
                        Edition.Error(DiagnosticCatalog.GroupUsageRule, $"data item '{item.CobolName ?? "FILLER"}': the "
                            + "GROUP-USAGE clause may be specified only if the subject of the entry is not strongly typed "
                            + "(ISO §13.18.29.3 SR1)");
                    if (ReferenceResolver.HasVariableLengthSubordinate(item))
                        Edition.Error(DiagnosticCatalog.GroupUsageRule, $"data item '{item.CobolName ?? "FILLER"}': the "
                            + "GROUP-USAGE clause may be specified only if the subject of the entry is not a "
                            + "variable-length group (ISO §13.18.29.3 SR1; §8.5.1.12)");
                    foreach (var c in item.Children)
                    {
                        if (c.Children.Count == 0) continue;
                        if (c.GroupUsage is GroupUsage.None) c.GroupUsage = item.GroupUsage;   // implied (SR2/SR3)
                        else if (c.GroupUsage != item.GroupUsage)
                        {
                            using var __c = Edition.At(c);
                            Edition.Error(DiagnosticCatalog.GroupUsageRule, $"data item '{c.CobolName ?? "FILLER"}': a group "
                                + $"subordinate to a GROUP-USAGE {gu} group shall itself be GROUP-USAGE {gu}, explicitly or "
                                + $"implicitly — not GROUP-USAGE {(c.GroupUsage is GroupUsage.Bit ? "BIT" : "NATIONAL")} "
                                + $"(ISO §13.18.29.3 {(item.GroupUsage is GroupUsage.Bit ? "SR2" : "SR3")})");
                        }
                    }
                }
                // SYNCHRONIZED on a GROUP item is a COBOL-2023 introduction (ISO §E.3.2 item 6 — "This clause may
                // now be specified for a group level data item"; §13.18.55 is the clause itself). It routes
                // through the CANONICAL funnel like every other introduction, so it is a hard error on BOTH
                // axes: §4.2.2's warning mechanism reports violations of the standard, and --permissive is the
                // migration mode for constructs an edition REMOVED — it has no meaning for one the targeted
                // edition has not yet acquired, which no pre-existing program can legally contain (CA14,
                // owner-approved option (a); this was the sole site routing an introduction through the
                // removed-severity seam, contradicting the compiler's own single-policy contract).
                if (item.Synchronized && Edition.DialectLevel < 2023)
                    ConstructRegistry.Check(Edition.Edition, Edition.Sink, Constructs.SyncOnGroup2023,
                        $"data item '{item.CobolName ?? "FILLER"}'");
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
                    {
                        using var __l = Edition.At(l);   // the LEAF's own entry position (kb/Work PB82)
                        if (l.Pic is { Category: PicCategory.Boolean or PicCategory.Numeric or PicCategory.NumericEdited })
                            Edition.Error(DiagnosticCatalog.NationalData, "national-form data (a boolean or numeric item "
                                + $"under a group USAGE NATIONAL) is recognized but not yet implemented "
                                + $"(Phase 4a residue) — data item '{l.CobolName ?? "FILLER"}' "
                                + "(ISO §13.18.60.3 SR12 / §13.18.60.4 GR1)");
                        else if (l.Pic is not null and not { Category: PicCategory.National })
                            Edition.Error(DiagnosticCatalog.UsageClauseCompatibility, $"data item '{l.CobolName ?? "FILLER"}': USAGE "
                                + "NATIONAL inherited from its group admits boolean, national, "
                                + "national-edited, numeric, and numeric-edited pictures only "
                                + "(ISO §13.18.60.3 SR12 / §13.18.60.4 GR1; §13.18.40.3 SR30)");
                    }
                }
                if (item.Pending is PicPending.BitUsage)
                {
                    item.Pending = PicPending.None;
                    foreach (var l in Leaves(item))
                    {
                        using var __l = Edition.At(l);   // the LEAF's own entry position (kb/Work PB82)
                        if (l.Pic is not null and not { Category: PicCategory.Boolean })
                            Edition.Error(DiagnosticCatalog.UsageClauseCompatibility, $"data item '{l.CobolName ?? "FILLER"}': USAGE BIT "
                                + "inherited from its group requires a boolean PICTURE (symbol 1 only) "
                                + "(ISO §13.18.60.3 SR5 / §13.18.60.4 GR1)");
                        // §13.18.60.4 GR1 — the group's USAGE BIT (declared, or implied by GROUP-USAGE BIT — §13.18.29.3
                        // SR2 "USAGE BIT may be implicitly specified") APPLIES to each subordinate boolean leaf that
                        // has no usage of its own: it becomes bit-form (bits, §8.5.1.6.3 alignment — D19), not the
                        // display-form SR13b default. Before D20 the check above validated the leaves and left them
                        // display-form, so `01 G USAGE BIT. 05 A PIC 1(5). 05 B PIC 1(3).` occupied 8 bytes (D20/PB79).
                        else if (l.OwnUsage is null && l.Pic is { Category: PicCategory.Boolean, Usage: Usage.Display } bp)
                            l.Pic = bp with { Usage = Usage.Bit };
                    }
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
            else if (item.GroupUsage is not GroupUsage.None)
            {
                // A GROUP-USAGE entry with no subordinates is not a group at all (§13.18.29.3 SR1) — the implied
                // usage's own Pending mark is consumed here so the picture-less-usage arm below does not double-report.
                Edition.Error(DiagnosticCatalog.GroupUsageRule, $"data item '{item.CobolName ?? "FILLER"}': the GROUP-USAGE "
                    + "clause may be specified only if the subject of the entry is a group item — this entry has no "
                    + "subordinate entries (ISO §13.18.29.3 SR1)");
                item.GroupUsage = GroupUsage.None;
                item.Pending = PicPending.None;
                item.Pic = PicInfo.Recovery();
            }
            else if (item.Pending is not PicPending.None)
            {
                // A PICTURE-less ELEMENTARY item may not carry USAGE NATIONAL/BIT — they are not among the
                // picture-less usages (§13.18.60.4; contrast INDEX/POINTER/OBJECT REFERENCE/BINARY-x). The
                // recovery shape keeps the doomed emit crash-free (the DEVLOG-597 pattern).
                Edition.Error(DiagnosticCatalog.UsageClauseCompatibility, $"data item '{item.CobolName ?? "FILLER"}': an elementary item "
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
                using var _ = Edition.At(item);
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
                // §13.18.44.3 SR4/SR7 + NOTE 1: data-name-2 is a PRECEDING entry at the same level, and when the name is
                // not unique "no ambiguity of reference exists because of the required placement" — the NEAREST preceding
                // same-named sibling. The former whole-scope FirstOrDefault admitted a LATER sibling (illegal source, kb/Work
                // PB93) and picked the FIRST of duplicates.
                item.RedefinesTarget = scope.TakeWhile(s => !ReferenceEquals(s, item))
                    .LastOrDefault(s => string.Equals(s.CobolName, tname, StringComparison.OrdinalIgnoreCase));
                // A method 01 REDEFINES whose target isn't in the method's own roots is a scope error (never a
                // silent cross-scope bind to an object/program item) — §13.18.44.3 SR.
                if (item.RedefinesTarget is null && item.Parent is null && OoRootOwner.ContainsKey(RootOf(item)))
                    // 1577, renumbered from a bare "COBOLNET1518" that collided with the locale-module
                    // non-support meaning (review V11 — the code comes from the catalog descriptor, never a literal).
                    Edition.Error(DiagnosticCatalog.MethodRedefinesScope,
                        $"REDEFINES target '{tname}' of method data item "
                        + $"'{item.CobolName ?? "FILLER"}' is not a preceding item in the same method scope "
                        + "(ISO §13.18.44.3 — a method item may not redefine object or program data)");
                else if (item.RedefinesTarget is null)
                    // kb/Work PB93: the PROGRAM-scope miss used to be silent — the item kept RedefinesTargetName (so
                    // BitLayout / ImageWidth / ByteWidth skipped it as an overlay) against a null RedefinesTarget (so
                    // the emitter gave it its own field): a storage shape no edition defines. ONE diagnostic; the
                    // item then binds as an ORDINARY entry (the name cleared below) so no consumer sees the half-state.
                    Edition.Error(DiagnosticCatalog.RedefinesTargetUnresolved,
                        $"'{item.CobolName ?? "FILLER"}' REDEFINES '{tname}': data-name-2 does not name a preceding "
                        + "entry in the same scope (ISO §13.18.44.3 SR4/SR7/SR10; §8.4.2.1)");
                if (item.RedefinesTarget is null)
                    item.RedefinesTargetName = null;   // no half-state: an unresolved redefiner is an ordinary entry
                // §13.18.44.3 SR7 (kb/Work PB93 sweep): data-name-2 shall be the entry that ORIGINALLY defined the area
                // — a redefiner naming a redefiner is a chain ISO forbids and the field's vendors accept: error strict,
                // warning under --permissive with the chain semantics (ClassifyRedefinesClasses chases the anchor).
                if (item.RedefinesTarget is { RedefinesTargetName: not null } viaRedefiner)
                    Edition.Removed(DiagnosticCatalog.RedefinesOfRedefinition.Code,
                        $"'{item.CobolName ?? "FILLER"}' REDEFINES '{viaRedefiner.CobolName ?? "FILLER"}', which is itself "
                        + "a redefinition — data-name-2 shall be the entry that originally defined the storage area "
                        + "(ISO §13.18.44.3 SR7)");
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
                if (info.From is null || (info.ThruName is not null && info.Thru is null))
                {
                    // kb/Work PB93: an operand naming nothing in the record was skipped silently — the alias then had
                    // no picture and no span, and every reference to it failed downstream with an unrelated message.
                    using var __r = Edition.At(ren);
                    string missing = info.From is null ? info.FromName : info.ThruName!;
                    Edition.Error(DiagnosticCatalog.RenamesOperandUnresolved,
                        $"'{ren.CobolName ?? "FILLER"}' RENAMES {info.FromName}{(info.ThruName is { } tn ? " THRU " + tn : "")}: "
                        + $"'{missing}' does not name an item of the record (ISO §13.18.45.3 SR4; §8.4.2.1)");
                    continue;
                }
                // The no-THRU alias inherits the renamed item's description (§13.18.45 GR1) — the resolver
                // forwards to the FROM item's place; no span, no synthetic alphanumeric picture.
                if (info.Thru is null) { ren.Pic = info.From.Pic; continue; }

                // The alias is the record's STORAGE WINDOW from data-name-2's first character to data-name-3's last
                // (§13.18.45.4 GR1/GR2 — a re-grouping of the record's characters; the alias reads/writes as one
                // elementary ALPHANUMERIC item of the window's width). kb/Work PB96: the former leaf-run walk listed
                // every leaf between FROM and THRU — a REDEFINES view included, as if it occupied its own characters —
                // so `RENAMES A THRU C` over A / B REDEFINES A / C was 6 characters, not 4. Offsets now come from the
                // ONE recursive storage function (a redefiner sits at its target's offset, §13.18.44), the parts are
                // the record's NON-redefining leaves that intersect the window, and a boundary inside a leaf (a FROM /
                // THRU that partially redefines it) is a partial part.
                int Width(DataItem d) => d.ImageWidth * (d.Occurs ?? 1);
                int Offset(DataItem d)
                {
                    if (d.RedefinesTarget is { } tgt) return Offset(tgt);          // an overlay starts where its target does
                    if (d.Parent is not { } par) return 0;                           // the record itself
                    int off = Offset(par);
                    foreach (var sib in par.Children)
                    {
                        if (ReferenceEquals(sib, d)) break;
                        if (sib.RedefinesTargetName is null) off += Width(sib);       // a redefining sibling adds no storage
                    }
                    return off;
                }
                int startOff = Offset(info.From);
                int endOff = Offset(info.Thru) + Width(info.Thru) - 1;
                if (endOff < startOff)
                {
                    using var __o = Edition.At(ren);
                    Edition.Error(DiagnosticCatalog.RenamesOperandUnresolved,
                        $"'{ren.CobolName ?? "FILLER"}' RENAMES {info.FromName} THRU {info.ThruName}: data-name-3 ends before "
                        + "data-name-2 begins — the THRU item shall follow the FROM item in the record (ISO §13.18.45.4 GR2)");
                    continue;
                }
                // Every leaf of the record with its storage extent — REDEFINES views INCLUDED: a view reads the
                // storage it overlays exactly as the redefined entry does (NC252A's `RDF8-5 THRU RDF8-6` lies inside a
                // double redefinition of an OCCURS 36 table, and only those two views tile that window). Views are
                // never counted twice because the tiling below advances by what it covered.
                var leaves = new List<(DataItem Leaf, int Off, int W, int Occ)>();
                void Walk(DataItem n)
                {
                    if (n.IsElementary) { if (Width(n) > 0) leaves.Add((n, Offset(n), n.ImageWidth, n.Occurs ?? 1)); return; }
                    foreach (var c in n.Children) Walk(c);
                }
                Walk(root);
                // GREEDY TILING of [startOff, endOff]: at each position take the LONGEST whole leaf (or whole table
                // cell) that starts exactly there and fits inside the window; else the most specific (narrowest) leaf
                // cell containing the position, as a partial slice up to the window's end or the cell's end.
                int pos = startOff;
                bool stuck = false;
                while (pos <= endOff)
                {
                    (DataItem Leaf, int? Occ, int Start, int Len, int Cover)? best = null;
                    foreach (var (leaf, off, w, occ) in leaves)
                    {
                        int total = w * occ;
                        if (pos < off || pos > off + total - 1) continue;          // the leaf does not contain pos
                        // the whole leaf (every occurrence) starting exactly here and fitting the window
                        if (off == pos && off + total - 1 <= endOff)
                            Consider((leaf, null, 1, total, total));
                        int k = (pos - off) / w + 1;                                // the occurrence containing pos
                        int cellLo = off + (k - 1) * w, cellHi = cellLo + w - 1;
                        if (cellLo == pos && cellHi <= endOff)
                            Consider((leaf, occ == 1 ? null : k, 1, w, w));       // a whole cell (or the whole 1-occurrence leaf)
                        else
                        {
                            int to = Math.Min(endOff, cellHi);                     // a partial slice of the containing cell
                            Consider((leaf, k, pos - cellLo + 1, to - pos + 1, to - pos + 1));
                        }
                    }
                    void Consider((DataItem Leaf, int? Occ, int Start, int Len, int Cover) c)
                    {
                        // prefer: whole (non-partial) over partial; then the longest cover; then the narrowest leaf
                        bool cPartial = c.Occ is not null && (c.Start != 1 || c.Len != c.Leaf.ImageWidth);
                        bool bPartial = best is { } b0 && b0.Occ is not null && (b0.Start != 1 || b0.Len != b0.Leaf.ImageWidth);
                        if (best is null
                            || (bPartial && !cPartial)
                            || (bPartial == cPartial && c.Cover > best.Value.Cover)
                            || (bPartial == cPartial && c.Cover == best.Value.Cover && c.Leaf.ImageWidth < best.Value.Leaf.ImageWidth))
                            best = c;
                    }
                    if (best is not { } chosen || chosen.Cover <= 0) { stuck = true; break; }
                    info.Span.Add(new RenamesSpanPart(chosen.Leaf, chosen.Occ, chosen.Start, chosen.Len));
                    pos += chosen.Cover;
                }
                if (stuck || info.Span.Count == 0)
                {
                    using var __u = Edition.At(ren);
                    Edition.Error(DiagnosticCatalog.RenamesOperandUnresolved,
                        $"'{ren.CobolName ?? "FILLER"}' RENAMES {info.FromName} THRU {info.ThruName}: the record's leaves do "
                        + "not tile the alias's storage window (kb/Work PB96)");
                    info.Span.Clear();
                    continue;
                }
                ren.Pic = new PicInfo(PicCategory.Alphanumeric, Usage.Display,
                    Length: endOff - startOff + 1, Digits: 0, Scale: 0, Signed: false);
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
            using var _ = Edition.At(item);
            // §13.18.44.3 SR12/SR14 (kb/Work PB179; the skeptic round moved this screen HERE, per WRITTEN
            // ENTRY and BEFORE dissolution — the dissolve loop below removes nested classes, so a per-class
            // screen let an inner entry's violation escape the diagnostic and fall to the outer class's
            // staged-loud arm: same verdict, wrong posture): SR12 bars the SUBJECT being of class
            // object/message-tag/pointer or a strongly-typed group; SR14 bars data-name-2 (the DIRECT
            // target as written) likewise, plus "subordinate to a strongly-typed group item". These are the
            // rules' LETTER — the drafting contrast with SR9's "nor any entry subordinate to it" shows they
            // name the entry-level items only; a NESTED pointer leaf is §13.18.60.3 SR14's territory — now
            // screened at its own declaration by CheckUsageDeclarations (kb/Work PB183, COBOLNET1724), with
            // ComputeTier's backstop arm kept as the recovery-path guard behind it.
            if (Sr12Sr14Violation(item, item.RedefinesTarget) is { } srv)
                Edition.Error(DiagnosticCatalog.RedefinesPointerObject, srv);
            // §13.18.44.3 SR17, the SAME per-written-entry posture and for the same reason (kb/Work PB177 arm C):
            // "Neither data-name-2 nor the subject of the entry shall be a variable-length group or a
            // dynamic-length elementary item." A SYMMETRIC rule, so BOTH sides are tested — the two-arm
            // discipline applied to the screen itself. It must run HERE, before the dissolve loop, or a nested
            // entry's violation escapes into the outer class's staged-loud arm (the PB179 skeptic round's
            // finding, which is why SR12/SR14 sit here).
            // ⛔ AND IT CLOSES A SILENT MIS-MODEL, not just an under-rejection: StorageFormPass.Classify returns
            // DynamicString for an IsDynamicLength item BEFORE reaching its Tier-B view arm, so such a view got
            // its OWN disjoint native string — two storages for one shared area (§13.18.44.4 GR1 says one), with
            // no diagnostic. Rejecting the entry makes that path unreachable.
            foreach (var (side, sideItem) in new[]
                     { ("the subject of the REDEFINES entry", item), ("data-name-2 of a REDEFINES entry", item.RedefinesTarget) })
                if (Sr17Shape(sideItem) is { } shape)
                    Edition.Error(DiagnosticCatalog.RedefinesVariableLength,
                        $"'{sideItem.CobolName ?? sideItem.CsName}' is {side} but is {shape}: neither "
                        + "data-name-2 nor the subject of the entry shall be a variable-length group or a "
                        + "dynamic-length elementary item (ISO §13.18.44.3 SR17)");
            // §13.18.44.3 SR5 SENTENCE 1 — the OBJECT side, per written entry like its two neighbours above.
            // "The data description entry for data-name-2 shall not contain an OCCURS clause." ANY format: the
            // fixed OCCURS of Format 1, Format 2's occurs-depending table, and Format 4's dynamic-capacity table
            // are all THE OCCURS CLAUSE (§13.18.38), so the predicate is `IsTable` — the union. ⛔ NEITHER half
            // alone works, and each is a live trap this repo has already sprung once: `Occurs is not null` is
            // the FIXED physical capacity and is NULL for a Format-4 table (the CONTROL SR3 arm's defect), while
            // `OccursSpec is not null` is NULL for a plain keyless fixed table, which OdoBindOccursSpec
            // deliberately leaves allocation-free (measured: with that spelling `05 T PIC X(3) OCCURS 4. 05 R
            // REDEFINES T PIC X(12).` still compiled clean — the very shape this screen exists for).
            // ⛔ SENTENCE 2 IS THE LIMIT OF THIS ARM: "However, data-name-2 may be subordinate to an item whose
            // data description entry contains an OCCURS clause." So the test is on data-name-2's OWN entry —
            // an ancestor walk here would reject legal source.
            // ⛔ AND THE DEPENDING SHAPE IS DELIBERATELY EXCLUDED, so one entry never draws two diagnostics from
            // one rule: SENTENCE 4 ("Neither the original definition nor the redefinition shall include an
            // occurs-depending table") is COBOLNET0855's, whose population is WIDER (an ODO table anywhere in
            // either definition, not merely on data-name-2's own entry) and which already rejects every entry
            // this arm would also match. One rule, four sentences, two disjoint screens.
            if (item.RedefinesTarget.IsTable && item.RedefinesTarget.OccursSpec?.DependingName is null)
                Edition.Error(DiagnosticCatalog.RedefinesTargetOccurs,
                    $"'{item.CobolName ?? item.CsName}' REDEFINES '{item.RedefinesTarget.CobolName ?? item.RedefinesTarget.CsName}', "
                    + "whose data description entry contains an OCCURS clause: the data description entry for "
                    + "data-name-2 shall not contain an OCCURS clause (ISO §13.18.44.3 SR5) — data-name-2 may be "
                    + "SUBORDINATE to an item that has one, but shall not carry one itself");
            DataItem anchor = item;
            // Chase data-name-2 to the non-redefining anchor. A CHAIN (X REDEFINES Y, Y REDEFINES Z) is
            // SR7-ILLEGAL (the skeptic round corrected this comment's SR11 miscitation — SR11 is the
            // nested-under-a-redefinition PERMISSION) and diagnosed at resolve; it reaches here only under
            // --permissive, where the anchor walk still yields one shared area.
            while (anchor.RedefinesTarget is { } t) anchor = t;
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
            // ⛔ CITATION REPAIRED **TWO-SIDEDLY**, AND THE SCREEN NARROWED TO THE SUBJECT SIDE (kb/Work PB177
            // arm C, then its follow-up). Three moves, and the middle one was itself wrong on one side:
            //  1. The arm used to cite "§13.18.44 SR5" for BOTH sides.
            //  2. The repair read SR5's FOURTH sentence ("Neither the original definition nor the redefinition
            //     shall include an occurs-depending table" — COBOLNET0855's rule) and concluded the whole rule
            //     was about something else, so it wrote "which NO syntax rule literally names". ⛔ THAT IS TRUE
            //     ONLY OF THE SUBJECT SIDE. SR5's FIRST sentence is "The data description entry for data-name-2
            //     shall not contain an OCCURS clause", and OCCURS DYNAMIC is Format 4 OF the OCCURS clause
            //     (§13.18.38) — so the OBJECT side is named outright, and is now COBOLNET1701 per written entry.
            //     Reading one sentence of a four-sentence rule and generalising is the same inheritance failure
            //     CLAUDE.md rule 1 names, one level down: the clause NUMBER was right and the SENTENCE was not.
            //  3. The dynamic-capacity SUBORDINATE case is §13.18.44.3 SR17's (a group with such a table
            //     subordinate IS a "variable-length group", §8.5.1.12.1) and is screened per WRITTEN ENTRY above
            //     as COBOLNET1698.
            // WHAT REMAINS HERE IS THE SUBJECT THAT IS ITSELF A DYNAMIC-CAPACITY TABLE, and for THAT side the
            // "no syntax rule names it" reading holds: §13.18.38.3 carries no REDEFINES restriction, and SR17
            // does not reach an ELEMENTARY dynamic table (§8.5.1.12.1's "variable-length group" is defined over
            // items SUBORDINATE to the group). So cite what actually decides it — §13.18.44.4 GR1 ("Storage
            // association for the subject of the entry starts at the first bit of the data item referenced by
            // data-name-2 and continues over an area sufficient to contain the number of bits required by the
            // data item referenced by the subject of the entry") against §8.5.1.9.1's dynamic-capacity model,
            // whose "physical and logical capacities may vary during execution": there is no fixed area to
            // associate. A COBOL.NET storage-model rejection, honestly labelled, not a borrowed rule number.
            // (The message names the OFFENDING MEMBER, not the class canonical — it used to say "the
            // dynamic-capacity table in '<canonical>'" even when the canonical was the ordinary fixed item and
            // the dynamic table was the REDEFINING entry.)
            if (cls.Members.FirstOrDefault(m => !ReferenceEquals(m, cls.Canonical) && m.IsDynamicTable) is { } dynSubject)
            {
                Edition.Error("COBOLNET1525", $"REDEFINES entry '{dynSubject.CobolName ?? dynSubject.CsName}' is "
                    + "itself a dynamic-capacity table: its capacity \"may vary during execution\" (ISO "
                    + "§8.5.1.9.1), so it has no fixed area for the storage association §13.18.44.4 GR1 requires; "
                    + "it shall not be the subject of a REDEFINES");
                tier = RedefinesTier.Rejected;
                reject ??= "REDEFINES subject is a dynamic-capacity table (§13.18.44.4 GR1 / §8.5.1.9.1, D9)";
            }
            // The §13.18.44.3 SR5 sentence-1 TIER verdict, same posture as SR12/SR14's and SR17's below (the
            // DIAGNOSTIC fired per written entry, before dissolution — no second diagnostic here). The tier
            // matters on its own: without it the class model is built as though the OCCURS-bearing object were
            // a legal single area, and the offsets/width walk below lays a view over one occurrence's worth of
            // storage. The predicate is the RULE's (`OccursSpec is not null`, every format) even though the
            // depending shape's DIAGNOSTIC belongs to COBOLNET0855 — a tier is not a message.
            foreach (var m in cls.Members)
                if (m.RedefinesTarget is { IsTable: true } occTarget)
                {
                    tier = RedefinesTier.Rejected;
                    reject = $"REDEFINES over '{occTarget.CobolName ?? occTarget.CsName}', whose data "
                        + "description entry contains an OCCURS clause (ISO §13.18.44.3 SR5)";
                }
            // The SR12/SR14 TIER verdict for a retained class (the DIAGNOSTIC fired per written entry in the
            // grouping loop above, before dissolution — no second diagnostic here): the class rejects with
            // the spec-required reason, OVERRIDING ComputeTier's staged-loud one (its backstop arm also
            // matches an entry-level pointer).
            foreach (var m in cls.Members)
                if (!ReferenceEquals(m, cls.Canonical) && m.RedefinesTarget is { } srTarget
                    && Sr12Sr14Violation(m, srTarget) is { } srReason)
                {
                    tier = RedefinesTier.Rejected;
                    reject = srReason;
                }
            // The §13.18.44.3 SR17 TIER verdict, same posture as SR12/SR14's above (the DIAGNOSTIC fired per
            // written entry, before dissolution — no second diagnostic here).
            // ⛔ THE TIER IS BELT-AND-BRACES, AND THE COMMENT HERE USED TO OVERCLAIM IT. It said "Rejecting the
            // CLASS makes that path structurally unreachable rather than merely unreached because a diagnostic
            // happened to stop the compile." MEASURED FALSE (RedefinesClassificationTests, once its assertion
            // was made non-inert): StorageFormPass.Classify orders its arms (1) IsDynamicTable, (1b)
            // IsDynamicLength → DynamicString, and only THEN (2) the REDEFINES-view arm — so arm 1b returns
            // before the tier is ever read, and a Rejected class's dynamic-length view still classifies as
            // DynamicString. A class DISSOLVED by the nested-anchor loop below bypasses the tier loop entirely,
            // which is the same hole from the other side. WHAT ACTUALLY PREVENTS THE DISJOINT STORAGE REACHING
            // A USER'S PROGRAM IS THE COBOLNET1698 DIAGNOSTIC, which is fatal. The tier verdict is a second,
            // independent verdict at the modelling layer and is worth keeping as one — every consumer that asks
            // the CLASS gets the right answer — but "structurally unreachable" was a claim about a path this
            // code does not close.
            foreach (var m in cls.Members)
                if (Sr17Shape(m) is { } vlShape)
                {
                    tier = RedefinesTier.Rejected;
                    reject = $"REDEFINES entry side '{m.CobolName ?? m.CsName}' is {vlShape} "
                        + "(ISO §13.18.44.3 SR17)";
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
                    else if (leaf.Pic is { HasImageByteForm: true, Usage: not Usage.Display })   // the ONE image predicate (kb/Work PB164 — COMP-5/BINARY-* windows included)
                        // A fixed-point BINARY/PACKED leaf of a Tier-B class is image-stored too: its window over
                        // the one string backing IS its BYTES — radix-2 two's complement or BCD, of exactly
                        // PicInfo.StorageWidth (ISO §13.18.60.4 GR4/GR11 implementor representation;
                        // COBOLNET_DESIGN §14.4, V59). Every accessor threads the leaf's OWN `_P_` profile
                        // through CobolNum.FormatImage/ParseImage, so no profile rewrite is needed or wanted:
                        // the sign lives in the bytes (two's complement / the sign nibble), and SignKind — a
                        // DISPLAY concern — stays the declared BinaryMinus, which is what DISPLAY of the leaf
                        // must still render. The former rewrite to a trailing-overpunch ZONED image existed only
                        // because the window used to be Digits characters wide.
                        MarkImageForced(leaf);
                }
        }
    }

    /// <summary>Assign each item in a redefines class its window offset within the class image and its class link; a
    /// top-level member starts at <paramref name="off"/> (0), a subordinate accumulates by preceding-sibling FULL
    /// extents (per-occurrence image width × OCCURS count — every occurrence is part of the layout). A subordinate
    /// that itself REDEFINES a prior sibling takes the TARGET's offset (redefinition begins at the redefined item's
    /// first position, ISO §13.18.44 GR1) and contributes NO width of its own. Every subordinate of a class member
    /// is itself a view (its stored field is suppressed — SR9).</summary>
    private static void AssignClassOffsets(DataItem item, int off, RedefinesClass cls) =>
        AssignClassOffsets(item, off * BitLayout.BitsPerCharacter, cls, item.HasBitDescendant);

    /// <summary>The walk proper, carrying the offset in BITS — the unit §13.18.44.4 GR1 states the storage
    /// association in ("starts at the first BIT … an area sufficient to contain the number of BITS required").
    /// <para>⛔ <paramref name="bitLaid"/> selects between TWO SPELLINGS OF ONE LAW, not two laws (kb/Work
    /// PB203, following the D19/PB43 discipline <see cref="DataItem.ImageWidth"/> already uses). When the
    /// member's subtree holds no <c>USAGE BIT</c> leaf, the §8.5.1.6.3 cursor and the plain character sum agree
    /// BY CONSTRUCTION — every item is byte-aligned and every advance is <c>ImageWidth × OCCURS</c> bytes — so
    /// the sum is taken and the result is byte-identical for the whole bit-free corpus. When it does hold one,
    /// the sum is WRONG in two ways at once and both are silent: two same-level bit members SHARE a byte
    /// (§8.5.1.6.3 rule 1) so the sum hands the second its own byte, and every item after such a run is
    /// displaced by the over-count. Measured before the fix on `01 A PIC X(1). 01 V REDEFINES A.
    /// 05 F1 PIC 1(4) USAGE BIT. 05 F2 PIC 1(4) USAGE BIT.`: F1 read byte 0 as a character and F2 read PAST the
    /// one-byte backing.</para></summary>
    private static void AssignClassOffsets(DataItem item, int bitOff, RedefinesClass cls, bool bitLaid)
    {
        item.ClassBitOffset = bitOff;
        item.ClassOffset = bitOff / BitLayout.BitsPerCharacter;
        item.Class = cls;
        int childBit = bitOff;
        DataItem? prev = null;
        foreach (var c in item.Children)
        {
            c.IsCanonical = false;
            // The inner-REDEFINES target is a PRIOR sibling, so its offset is already assigned this walk.
            int cBit;
            if (c.RedefinesTarget is { } target) cBit = target.ClassBitOffset;
            else
            {
                // §8.5.1.6.3 rules 1 and 2, the SAME pair BitLayout.ExtentBits walks: a bit item immediately
                // following a bit item OF THE SAME LEVEL takes the next bit position — the only case that shares
                // a byte; every other item starts at the first bit position of the first available byte.
                if (bitLaid && !(BitLayout.IsBitItem(c) && prev is not null && BitLayout.IsBitItem(prev)
                                 && prev.Level == c.Level))
                    childBit = (childBit + BitLayout.BitsPerCharacter - 1)
                               / BitLayout.BitsPerCharacter * BitLayout.BitsPerCharacter;
                cBit = childBit;
            }
            AssignClassOffsets(c, cBit, cls, bitLaid);
            if (c.RedefinesTarget is null)
            {
                childBit += (bitLaid ? BitLayout.WidthBits(c) : c.ImageWidth * BitLayout.BitsPerCharacter)
                            * (c.Occurs ?? 1);
                prev = c;
            }
        }
    }

    /// <summary>Assign a redefines class its tier (COBOLNET_DESIGN §4.2; the Step D arm-1 dissolution made the
    /// cascade D &gt; B &gt; A — Tier C dissolved into B: every numeric leaf kind's byte form is pinned, so a
    /// mixed-USAGE pun is an ordinary byte-window class. D rejects the nested pointer/object backstop and the
    /// NATIONAL residue.</summary>
    // (ContainsDynamicTable was deleted with kb/Work PB177 arm C: its SUBORDINATE half is §13.18.44.3 SR17's,
    // now screened per written entry through ReferenceResolver.HasVariableLengthSubordinate — §8.5.1.12.1's own
    // definition of "variable-length group" — and its SELF half is the narrowed `m.IsDynamicTable` test at the
    // COBOLNET1525 arm. Two spellings of one walk collapsed into the predicate the standard actually defines.)

    /// <summary>The §13.18.44.3 SR12/SR14 class test (kb/Work PB179): object/pointer classes occupy no
    /// character positions and can neither overlay nor be overlaid. Message-tag has no bound model yet;
    /// a USAGE FUNCTION-POINTER entry's semantics are staged at declaration (the P13 band — its Pic stays
    /// null), so it never reaches the class machinery live.
    /// <para>⛔ THE ONE CLASS PREDICATE FOR BOTH SCREENS. §13.18.60.3 SR14's declaration-placement screen
    /// (<c>DataBinder.UsageDeclaration.cs</c>, kb/Work PB183) resolves its class question through this same
    /// method — see <c>Sr14PlacementClass</c>. The two rules spell the population differently (SR12/SR14 name
    /// the CLASSES "class object, message-tag, or pointer"; §13.18.60.3 SR14 names the five USAGE PHRASES that
    /// produce exactly those classes) and a second hand-written list would drift the moment MESSAGE-TAG or
    /// FUNCTION-POINTER gains a model. A unit pin asserts the identity. ⛔ CLASS INDEX IS NOT HERE and must
    /// never be added: §13.18.60.3 SR4 is the rule whose list includes INDEX, and it has its own predicate.
    /// </para></summary>
    private static bool PointerObjectClass(DataItem d) =>
        d.Pic?.Category is PicCategory.Pointer or PicCategory.ProgramPointer or PicCategory.ObjectReference;

    /// <summary>⛔ THE ONE BYTE-WINDOW CARRIAGE GATE — the single answer to "may this LEAF ride a shared
    /// byte-window storage area?", returning null when it may and the residue clause naming why not when it
    /// may not. Both surfaces that ask the question ask it here: <see cref="ComputeTier"/> (a REDEFINES class
    /// over one string canonical, §13.18.44.4 GR1 — "Storage association for the subject of the entry starts
    /// at the first bit of the data item referenced by data-name-2 and continues over an area sufficient to
    /// contain the number of bits required") and <c>ForceStringCanonical</c> (the EXTERNAL run-unit cell, the
    /// ADDRESS-OF-taken per-instance cell, and the BASED pointer-deref bridge whose area ALLOCATE obtains,
    /// §14.9.3.4 GR3 — "the amount of storage to be allocated is the number of bytes required to hold an item
    /// as described by data-name-1").
    /// <para>⛔ IT IS ONE PREDICATE BECAUSE IT WAS TWO, AND THEY DRIFTED (kb/Work PB231). ComputeTier was a
    /// DENY-list and the cell forcer's local <c>CellCapable</c> an ALLOW-list, and PB203 opened only the
    /// REDEFINES one to USAGE BIT leaves — the ninth instance of this repo's two-arm dispatch shape. The
    /// consequence was rejects-legal-source: `01 R BASED. 05 B PIC 1(8) USAGE BIT.` drew COBOLNET1695 at bind
    /// while the byte-identical REDEFINES spelling compiled and ran, even though §13.18.5.3 SR1/SR2 restrict a
    /// BASED subject to exactly "shall not be of class object" and "shall not be a dynamic-length elementary
    /// item or a variable-length group" and §14.9.3.3 SR1 asks only for the BASED clause — no rule anywhere
    /// conditions BASED, EXTERNAL or ADDRESS OF on a subordinate's USAGE. Two smaller divergences went with it
    /// and are closed by the same collapse: a Pic-less leaf and a NATIONAL-usage numeric were denied by the
    /// cell and admitted by REDEFINES.</para>
    /// <para>The ALLOW-list direction is deliberate and must stay: a category added to the model with no
    /// pinned byte image has to be DENIED by default, never silently laid out as a zero-width alias.
    /// <c>ByteWindowResidueDriftTests</c> pins that both callers route through here.</para>
    /// <para>⚠ THE THIRD CALLER-TO-BE is <see cref="GateNationalRecords"/> — the FD/SD record gate, which asks
    /// the same question of a file record area and today screens the NATIONAL half only, with its own message
    /// and its own diagnostic. It is deliberately NOT routed here yet: folding it in would also start refusing
    /// a pointer-class leaf in a file record, a tightening kb/Work PB231 did not measure. The landing that
    /// discharges RESIDUE-11 should fold all three, and the note carries that instruction.</para></summary>
    internal const string NationalResidue =
        "a national leaf (the D-N1 2-byte-per-position byte-window layout is an undischarged documentation "
        + "residue — RESIDUE-11; ISO §13.18.60.4 GR8 gives the implementor the size, and this "
        + "implementation's national character is TWO bytes over a byte-addressed area)";

    internal static string? ByteWindowResidueOf(DataItem leaf)
    {
        // A pointer/object-class leaf is not a byte sequence at all — the managed pointer/reference cell has
        // no byte-window overlay. §13.18.60.3 SR14 already makes the nested DECLARATION nonconforming (a USAGE
        // clause with the MESSAGE-TAG, OBJECT REFERENCE, POINTER, FUNCTION-POINTER or PROGRAM-POINTER phrase
        // "may be specified only for an elementary data item at level 1 or an elementary data item subordinate
        // to a type declaration that includes the STRONG phrase" — CheckUsageDeclarations / COBOLNET1724,
        // kb/Work PB183), so on conforming source this arm is reached only through the level-1 and STRONG
        // shapes it does NOT bar; those are the pointer half of kb/Work PB231, which needs a parallel
        // object-slot in the storage area before §14.9.3.4 GR9's null-seeding has anywhere to write.
        if (PointerObjectClass(leaf))
            return "a pointer/object-class leaf (a managed pointer/reference cell has no byte-window overlay "
                + "— ISO §13.18.60.3 SR14 / kb/Work PB183, PB231)";
        // A USAGE FUNCTION-POINTER entry is staged at declaration (the P13 band) and never gains a PicInfo;
        // an elementary item with no bound representation has no image to window.
        if (leaf.Pic is not { } p)
            return $"a leaf with no bound representation ('{leaf.CobolName ?? "FILLER"}' — the staged "
                + "USAGE band; ISO §13.18.60)";
        return p.Category switch
        {
            // One byte per character position (the documented item-209 serialization); an edited image is
            // its own characters.
            PicCategory.Alphanumeric or PicCategory.NumericEdited => null,
            // ⛔ BOOLEAN RIDES, IN BOTH ITS REPRESENTATIONS — the PB231 arm. A DISPLAY boolean is one
            // '0'/'1' character per position and byte==char holds (D-B1). A USAGE BIT run is a SUB-BYTE
            // packing, and that packing is already laid out and windowed: §13.18.60.4 GR5 ("The USAGE BIT
            // clause specifies that bits shall be used to represent a boolean data item"), §8.5.1.6.3's
            // cursor ("an elementary bit data item immediately following an elementary bit data item or bit
            // group item of the same level" takes the next bit position, everything else the first bit of
            // the first available byte) and §13.18.29.4 GR1c ("Data items contained within a bit group are
            // allocated in storage in accordance with the rules specified in 8.5.1.6.3") are walked by
            // AssignClassOffsets' bit-laid arm — the SAME walk this predicate's two callers share — and the
            // resulting window is read/written by CobolBits.ReadWindow/WriteWindow over the class backing
            // (D19/PB43 for the layout, PB203 for the window). DataItem.ImageWidth is already the
            // ceil(bits/8) byte extent, so the area a cell allocates for such a record is GR3's byte
            // quantity by construction.
            PicCategory.Boolean => null,
            // Every NUMERIC usage carries a pinned byte form since the Step D dissolution (kb/Work PB164) —
            // THE ONE image predicate, never a hand-rolled usage union.
            PicCategory.Numeric when p.HasImageByteForm => null,
            // NATIONAL, by CATEGORY (PIC N / USAGE NATIONAL) or by a national-form NUMERIC (PIC 9 USAGE
            // NATIONAL, §13.18.60.3 SR12 — staged loud earlier, screened here too so the two surfaces cannot
            // disagree about it). ⛔ The usage is tested EXPLICITLY rather than inferred from "numeric with no
            // byte form": that inference happens to hold today only because FLOAT-BINARY-128 and the decimal
            // floats are rejected at ParseUsage, and a leaf mislabelled "national" in a residue message is
            // exactly the kind of inherited half-truth CLAUDE.md rule 1 is about.
            PicCategory.National => NationalResidue,
            PicCategory.Numeric when p.Usage is Usage.National => NationalResidue,
            _ => $"a leaf of category {p.Category} and usage {p.Usage} with no pinned byte image "
                + "(ISO §13.18.60.4 — no representation this model can lay out in a byte window)",
        };
    }

    /// <summary>ONE §13.18.44.3 SR12/SR14 verdict per WRITTEN entry — <paramref name="subject"/> is the item
    /// carrying the REDEFINES clause, <paramref name="target"/> its DIRECT data-name-2 (never the chained
    /// anchor). Returns the violation text (the diagnostic message AND the class RejectReason), or null.
    /// Each arm's message names the arm that actually fired (the skeptic round: a subordinate-to-strong
    /// violation was described as "is a strongly-typed group" — false of the item named).
    /// <para>⚠ The SR14 "subordinate to a strongly-typed group item" arm fires only when the subject sits
    /// OUTSIDE the target's strong subtree: a REDEFINES written INSIDE a STRONG typedef template (subject and
    /// target under one strong root) is the recorded deliberate carve-out `CheckStrongTypeDeclarations`
    /// documents ("An INTERNAL redefine … is legitimate and NOT flagged") — this screen must not silently
    /// overturn that determination; whether the §13.18.57.3 SR4 letter overturns IT is [[PB183]]'s companion
    /// derivation.</para></summary>
    /// <summary>The §8.5.1.12.1 shapes ISO §13.18.44.3 SR17 bars from EITHER side of a REDEFINES entry: a
    /// DYNAMIC-LENGTH elementary item, or a VARIABLE-LENGTH GROUP — "a group item whose data description has at
    /// least one dynamic-length elementary item or dynamic-capacity table as a subordinate item". Returns the
    /// clause of the message naming which shape was found, or null when the item is fine.
    /// <para>The dynamic-capacity half overlaps the COBOLNET1525 arm deliberately: 1525 is
    /// the narrower per-CLASS backstop for an item that IS itself a dynamic-capacity table (which SR17 does not
    /// literally name — it names variable-length GROUPS and dynamic-length ELEMENTARY items), and it now fires
    /// only where SR17 does not. Neither code is reallocated.</para></summary>
    private static string? Sr17Shape(DataItem item) =>
        item.IsDynamicLength ? "a dynamic-length elementary item (ISO §8.5.1.10)"
        : item.IsGroup && ReferenceResolver.HasVariableLengthSubordinate(item)
            ? "a variable-length group (ISO §8.5.1.12.1 — a dynamic-length elementary item or a "
              + "dynamic-capacity table is subordinate to it)"
        : null;

    private static string? Sr12Sr14Violation(DataItem subject, DataItem target)
    {
        if (PointerObjectClass(subject))
            return $"'{subject.CobolName ?? subject.CsName}' is the subject of a REDEFINES entry but is of "
                + "class object/pointer (ISO §13.18.44.3 SR12)";
        if (StrongTypeModel.IsStrongGroup(subject))
            return $"'{subject.CobolName ?? subject.CsName}' is the subject of a REDEFINES entry but is a "
                + "strongly-typed group item (ISO §13.18.44.3 SR12)";
        if (PointerObjectClass(target))
            return $"'{target.CobolName ?? target.CsName}' is redefined (data-name-2) but is of class "
                + "object/pointer (ISO §13.18.44.3 SR14)";
        if (StrongTypeModel.IsStrongGroup(target))
            return $"'{target.CobolName ?? target.CsName}' is redefined (data-name-2) but is a strongly-typed "
                + "group item (ISO §13.18.44.3 SR14)";
        if (StrongTypeModel.StrongRoot(target) is { } tRoot
            && !ReferenceEquals(StrongTypeModel.StrongRoot(subject), tRoot))
            return $"'{target.CobolName ?? target.CsName}' is redefined (data-name-2) but is subordinate to "
                + "the strongly-typed group item "
                + $"'{tRoot.CobolName ?? tRoot.CsName}' (ISO §13.18.44.3 SR14)";
        return null;
    }

    private static RedefinesTier ComputeTier(RedefinesClass cls, out string? reject)
    {
        reject = null;
        var leaves = cls.Members.SelectMany(LeavesOf).ToList();

        // ⛔ THE TIER-C ARM IS GONE (the Step D arm-1 dissolution — DESIGN-data-model §2.3; kb/Work PB164's
        // LAST codegen half): every NUMERIC leaf kind carries a pinned byte form (NumericByteForm — waves
        // 1–2 + R40), the Tier-B geometry has been byte-form since V59 (§13.18.44.4 GR1 associates storage
        // at the bit over "the number of bits required"; GR2 grants every entry's name reference to that
        // storage), and the StoreAsImage loop's HasImageByteForm mark goes LIVE for float/COMP-5/BINARY-*/
        // INDEX leaves — each lane it feeds carries its own byte-level pin (the windowed-float read/store
        // arms, the unsigned ParseImage twins, the float seeds, the StorageFormPass canonical promotion).
        // A mixed class is simply Tier B: one string backing IS the shared storage area, every member an
        // (offset, StorageWidth) byte window.
        // Tier D backstop — a pointer/object-class LEAF nested inside a member (kb/Work PB179): NOT
        // §13.18.44.3 SR12/SR14's letter (those bar the ENTRY-level items — screened per written entry in
        // ClassifyRedefinesClasses). The skeptic round derived that §13.18.60.3 SR14 makes the nested
        // declaration itself NONCONFORMING (a pointer/object USAGE is legal only at level 1 or under a
        // STRONG type — and the strong case trips the entry-level screen via its enclosing group).
        // ⛔ THAT SCREEN NOW EXISTS — CheckUsageDeclarations (DataBinder.UsageDeclaration.cs, COBOLNET1724,
        // kb/Work PB183), which runs EARLIER in the pipeline and rejects the declaration itself, so on
        // conforming source this arm is unreachable-by-construction exactly as predicted. It is KEPT, not
        // deleted: it remains the loud guard for the recovery paths and for any future mode that admits a
        // nonconforming declaration, and without it such a leaf would reach the byte-window machinery as a
        // silent zero-width Tier-B alias. Never that.
        //
        // ⛔ Tier D IS NOW ONE CALL — ByteWindowResidueOf, the SHARED gate this screen and the cell forcer
        // (ForceStringCanonical) both route through. It used to be two hand-written lists, one per surface,
        // and they drifted: PB203 admitted USAGE BIT leaves HERE and nowhere else, so the byte-identical
        // BASED / EXTERNAL / ADDRESS-OF spelling of a conforming record still drew a bind-time reject
        // (kb/Work PB231). The residue clauses — the pointer/object backstop and the RESIDUE-11 national
        // layout — now live once, so the landing that discharges either one opens BOTH surfaces in a single
        // edit. ByteWindowResidueDriftTests pins the routing.
        if (leaves.Select(ByteWindowResidueOf).FirstOrDefault(r => r is not null) is { } residue)
        {
            reject = $"{residue} under REDEFINES of '{cls.Canonical.CobolName}' — not yet implemented";
            return RedefinesTier.Rejected;
        }

        // Tier A — every member is an elementary item sharing the canonical's CLR storage type AND its image width:
        // one stored field, the rest pass-throughs (a numeric view reinterprets the shared value via its own scale).
        // ⛔ AND THE SAME STORAGE UNIT (kb/Work PB203). CLR type + image width is not enough to prove ONE FIELD
        // SERVES BOTH: a `USAGE BIT` leaf's stored carrier is its PICTURE 1(n) boolean positions while its
        // ImageWidth is the ceil(n/8) BYTES it occupies (§13.18.60.4 GR5 — "bits shall be used"; D19/PB43), so
        // `01 A PIC X(1). 01 B REDEFINES A PIC 1(8) USAGE BIT.` matched on both counts (string, width 1) and B
        // aliased A's one-CHARACTER field — measured: `DISPLAY B` printed `A` where the eight boolean positions
        // of 0x41 are `01000001`. Two members share one field only when they agree on the UNIT (both bit or
        // neither) and, being bit, on the boolean-position COUNT — `PIC 1(5)` and `PIC 1(8)` share a byte
        // ceiling and NOT a carrier. A mismatched pair drops to Tier B, whose windows carry the unit explicitly.
        DataItem canon = cls.Canonical;
        bool allAlias = canon.IsElementary && cls.Members.All(m =>
            m.IsElementary && m.ElementType == canon.ElementType && m.ImageWidth == canon.ImageWidth
            && BitLayout.IsBitLeaf(m) == BitLayout.IsBitLeaf(canon)
            && (!BitLayout.IsBitLeaf(m) || m.Pic!.Length == canon.Pic!.Length));
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
    /// reference — so they must NOT be dedup'd by <c>PicInfo</c> identity: a clone and its source share ONE <c>PicInfo</c>
    /// instance. (<c>PicInfo.PointerItem</c> stopped being a singleton at kb/Work PB153 — it is a factory now, since a
    /// restricted data-pointer carries its <c>TO type-name</c> — so the dedup hazard no longer needs that example to be
    /// real, but the clone-sharing one still is.) The result is exactly the set of items the binder's per-entry
    /// <c>PictureAnalyzer.ParseUsage</c>/<c>Analyze</c> gates fired for, once each.
    ///
    /// <para>⛔ This is the WRITTEN-ENTRY forest and its exclusion of the clones is part of its contract — do not
    /// widen it. A pass whose subject is a property of the COMPOSED entry rather than of the written clause list
    /// takes <see cref="CompositionForest"/> instead; that distinction is why the §13.18.63.3 SR13/SR14 screen
    /// missed the whole TYPE population on its first landing.</para>
    /// </summary>
    public IEnumerable<DataItem> ConformanceForest() =>
        DeclaredForest().Where(item => StrongTypeModel.TypeAnchor(item) is null);

    /// <summary>
    /// Every data item this unit binds AS COMPOSED — the same forest as <see cref="ConformanceForest"/> plus the
    /// TYPE / SAME AS clone subtrees it excludes, pre-order DFS in declaration order, still without the OO/UDF
    /// compiler temps.
    ///
    /// <para>⛔ TWO FORESTS, TWO KINDS OF SUBJECT — pick by what the rule is ABOUT, not by convenience.
    /// <see cref="ConformanceForest"/> answers "which entries did the programmer WRITE, as written": it is the
    /// once-per-source-entry set the per-entry data-attribute gates fire over, and excluding the clones is right
    /// there because the TEMPLATE carried the same clauses and was gated once. A rule whose subject is a property
    /// of the COMPOSED entry has no such counterpart on the template: `01 R TYPE T VALUE "ABCD".` composes a
    /// §13.18.63.3 SR14 violation out of a VALUE clause the reference site wrote and a usage the template wrote,
    /// and NEITHER entry carries it alone. Measured on the first landing of the SR13/SR14 screen: that program
    /// compiled clean while its byte-identical inline spelling was rejected — the whole TYPE population escaped.
    /// Use this forest for such a rule, and keep the item's OWN provenance (see <see cref="DataItem.ValueIsCopied"/>)
    /// to avoid re-reporting a clause the template already answered for.</para>
    /// </summary>
    public IEnumerable<DataItem> CompositionForest() => DeclaredForest();

    /// <summary>The shared spine of the two forests: <see cref="Roots"/> + <see cref="LinkageRoots"/> +
    /// <see cref="TypeDecls"/> templates, pre-order DFS in declaration order, pruning the OO/UDF compiler temps
    /// recorded in <see cref="CompilerTempClones"/>.</summary>
    private IEnumerable<DataItem> DeclaredForest()
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
        return Roots.Concat(LinkageRoots).SelectMany(Walk).Concat(TypeDecls.Values.SelectMany(Walk));
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
