// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Binding.Passes;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// The data-binding state of ONE bound unit (docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1); kb/Work PB2296,
/// step 1 of the <see cref="DataBinder"/> extract): the facts the data pipeline produces, in the two shapes the code has.
/// <list type="bullet">
/// <item>A VALUE SLICE is a record of the facts ONE producer writes, frozen when its producer is done and read only after
/// it. Two slices have a second, LATE door that the PROCEDURE binders use for the compiler temporaries they mint
/// (<see cref="ForestSlice.AddTemporaryRoot"/>, <see cref="StorageTierSlice.ClaimTemporaryCell"/>); they are frozen when the
/// procedure binders are done, which is the group tail. The slices are the unit's checking flags (<see cref="UnitChecking"/>,
/// written by <see cref="CheckingFold"/>), what it inherits from its
/// containers (<see cref="InheritedGlobals"/>, <see cref="InheritStep"/>) and its configuration
/// (<see cref="ConfigurationSlice"/>, <see cref="ConfigurationStep"/>), and the facts the data binder's own phases declare:
/// the type forest and its name tables (<see cref="Forest"/>), the files (<see cref="Files"/>), the clause facts the entry
/// walk records for the screening passes (<see cref="Clauses"/>), the storage tier (<see cref="StorageTier"/>), the implicit
/// registers (<see cref="Implicit"/>) and the report scope (<see cref="Reports"/>), each with the one writer its summary names
/// and read-only views for everyone else.</item>
/// <item>A FACT SET (<see cref="FactSet{T}"/>) accumulates across phases and is append-only, with an ordered read: the
/// four below, each with its declared appending phases.</item>
/// </list>
/// The procedure-bind state is NOT here: it is <see cref="ProcedureBindState"/>, which no data phase reads and which never
/// joins the bound unit's result.
/// </summary>
public sealed class DataBindState
{
    /// <summary>The unit's configuration slice (<see cref="Binding.ConfigurationSlice"/>): SPECIAL-NAMES, the collating
    /// sequences, DEBUGGING MODE, the REPOSITORY specifiers and the OPTIONS model. Seeded before the unit's first data phase
    /// by <see cref="ConfigurationStep"/>, its one producer per unit, then overlaid by the unit's own CONFIGURATION SECTION
    /// and OPTIONS paragraph in <c>DataBinder.BindDeclarations</c>.</summary>
    public ConfigurationSlice Configuration { get; }

    /// <summary>What the unit inherits from the programs that contain it (<see cref="InheritedGlobals"/>), written by
    /// <see cref="InheritStep"/> before the unit's first data phase; empty for an outermost program and an OO element.</summary>
    public InheritedGlobals Inherited { get; } = new();

    /// <summary>The type forest and its name tables (<see cref="ForestSlice"/>): roots, data-names, condition-names,
    /// index-names, CAPACITY registers, type declarations, screen-names and the inheritance tier of a container's GLOBAL names.</summary>
    public ForestSlice Forest { get; }

    /// <summary>The files the unit declares (<see cref="FileSlice"/>); a container's GLOBAL files are <see cref="Inherited"/>'s.</summary>
    public FileSlice Files { get; } = new();

    /// <summary>What the entry walk recorded about the clauses each entry wrote (<see cref="ClauseFacts"/>).</summary>
    public ClauseFacts Clauses { get; } = new();

    /// <summary>The STATIC channels and the cell backings and bridges of the pointer pass (<see cref="StorageTierSlice"/>).</summary>
    public StorageTierSlice StorageTier { get; } = new();

    /// <summary>The DEBUG-ITEM family and EXCEPTION-OBJECT, which no entry declares (<see cref="ImplicitRegisters"/>).</summary>
    public ImplicitRegisters Implicit { get; } = new();

    /// <summary>The report description entries this unit declares and the GLOBAL ones it inherits, with the sum counters and
    /// the name lookups over them (<see cref="ReportScope"/>; kb/Work PB2357): written by <see cref="ReportSectionBinder"/>
    /// alone, read by every later phase.</summary>
    public ReportScope Reports { get; } = new();

    /// <summary>REPORT SECTION sum counter names → their SUM entries (<see cref="ReportScope.SumCounters"/>), behind the P6
    /// Step-6 watermark gate: the counters exist once <see cref="PassPhase.FilesResolved"/> has run.</summary>
    public IReadOnlyDictionary<string, List<(ReportModel Report, ReportSumFamily Family)>> SumCounters
    {
        get { Require(PassPhase.FilesResolved, "SumCounters"); return Reports.SumCounters; }
    }

    /// <summary>⛔ THE ONE LOOKUP OF A FILE-NAME VISIBLE IN THIS SOURCE ELEMENT, at any point of the bind: its own file
    /// (<see cref="FileSlice.FilesByName"/>), which hides a container's of the same spelling (§8.4.6.2.1 3) a)), else the
    /// nearest container's GLOBAL file (§13.18.27.4 GR1; <see cref="InheritedGlobals.Files"/>, known before the unit binds). A
    /// container's GLOBAL record qualified by its file (§8.4.2.2.2 Format 1's file-report-qualifier) resolves through it
    /// too (kb/Work PB3557 review R2-1).</summary>
    public FileNameScope VisibleFiles => new(Files.FilesByName, Inherited.Files);

    /// <summary>The highest <see cref="PassPhase"/> whose producing pass has COMPLETED on this unit's forest (the P6 Step-6
    /// completion gate). Advanced by <c>DataBinder.BindResolve</c> (the per-unit prefix) and by <c>BinderDriver.Bind</c> (the
    /// group tail, which marks every binder of the group after each group pass); never regresses.</summary>
    internal PassPhase Watermark { get; private set; }

    /// <summary>Advance the watermark to <paramref name="produced"/> (never regresses).</summary>
    internal void MarkProduced(PassPhase produced)
    {
        if (produced > Watermark) Watermark = produced;
    }

    /// <summary>The ALWAYS-ON completion gate: assert <paramref name="required"/> has been produced on this unit before
    /// <paramref name="fact"/> is read/run — a mis-ordered pass in a production compiler is a silent miscompile, exactly when
    /// the loud throw matters most. Cost is a per-pass-entry integer compare (immaterial). Was
    /// <c>[Conditional("DEBUG")]</c> at first landing — CI's RELEASE test leg stripped the call sites and failed the
    /// throw-expecting WatermarkTests ("no exception was thrown"), proving the Debug-only design created a whole
    /// Release-divergence class for zero real saving (DEVLOG 774).</summary>
    internal void Require(PassPhase required, string fact)
    {
        if (Watermark < required)
            throw new InvalidOperationException(
                $"BindPipeline watermark violation: '{fact}' requires phase {required}, but this binder has only "
                + $"reached {Watermark} — a consumer ran before the producing pass (P6 Step 6 gate).");
    }

    /// <summary>The check that a per-unit resolve pass appends only to the fact sets it declared
    /// (<see cref="Passes.PassViews"/>); opened by <c>DataBinder.BindResolve</c> around each pass.</summary>
    internal AppendGate Gate { get; } = new();

    /// <summary>The unit's checking slice (<see cref="Binding.UnitChecking"/>): <see cref="UnitChecking.None"/> until
    /// <see cref="CheckingFold"/> writes it, once, after the unit's data phases; read by the reference resolver while the
    /// procedure division binds and by code generation.</summary>
    public UnitChecking UnitChecking
    {
        get => field ?? UnitChecking.None;
        internal set => field = field is null ? value
            : throw new InvalidOperationException("DataBindState.UnitChecking is written once, by CheckingFold (kb/Work PB2296)");
    }

    /// <summary>Group items used as a WHOLE character-image operand (MOVE / DISPLAY / compare / ACCEPT / record I-O /
    /// whole-group CORRESPONDING pair / boundary formal). Appended at FilesResolved by
    /// <c>MarkFileRecordImageLeavesPass</c> (the FILE record areas) and at UsageCollected by
    /// <see cref="Passes.UsageCollectionPass"/> (the boundary formals and the whole-group operands of the bound tree);
    /// read whole by <c>StorageFormPass</c>, which promotes the leaves of each group to their character image (ISO
    /// §14.9.25.4 GR4: a whole-group move fills without conversion; see <see cref="DataItem.StoreAsImage"/>).</summary>
    public FactSet<DataItem> WholeGroupReferenced { get; }

    /// <summary>Every elementary item some bind-time rule forces to store its CHARACTER IMAGE: Tier-B REDEFINES leaves
    /// (appended at RedefinesClassified, <c>ClassifyRedefinesClassesPass</c>), CALL-cell REDEFINES leaves, FILE record leaves
    /// and report print faces (appended at FilesResolved; <see cref="FactSetContract"/> holds the phases), figurative-fill and
    /// ref-mod-store receivers (appended at ProcedureBound, <c>MoveBinder</c>). Read EARLY by
    /// <see cref="ReferenceResolver"/>'s <c>NumericImagePlace</c> wrap decision (<see cref="IReadsEarly{T}"/>: a fact an
    /// earlier statement's bind recorded is visible to a later statement's), and read whole by <c>StorageFormPass</c>,
    /// which unions it with the compiler-temp re-sync and the whole-group promotion to compute
    /// <see cref="DataItem.Storage"/>.</summary>
    public FactSet<DataItem> ImageForcedItems { get; }

    /// <summary>ELEMENTARY items a CHARACTER CHANNEL can write (kb/Work PB992), appended at UsageCollected by
    /// <see cref="Passes.UsageCollectionPass"/> beside <see cref="WholeGroupReferenced"/> and promoted by
    /// <c>StorageFormPass</c> exactly as a whole-referenced group's leaves are. A channel that delivers CHARACTERS rather
    /// than a value — §14.9.25.4 GR4's group move into an elementary receiver, and §14.2.3 GR8's shared storage between a
    /// BY REFERENCE argument and its formal parameter, where §14.8.2.3.2 rule 1 admits a formal of any description "of
    /// the same length", and the CALL RETURNING receiver — can deposit content that is not a numeric value, and a native
    /// carrier holds only a value.</summary>
    public FactSet<DataItem> CharacterChannelItems { get; }

    /// <summary>The parse nodes (and subscript tokens) the binder RESOLVED to a function activation or an object-property
    /// accessor invocation although the text carries no syntactic marker of one: a function-identifier written without
    /// the word FUNCTION (§8.4.3.2.3 SR2 — <c>UPPER-CASE (X)</c> under <c>REPOSITORY. FUNCTION ALL INTRINSIC</c>) and a
    /// property reference (§8.4.3.9 — <c>prop OF obj</c>, whose get/set accessor is a METHOD invocation). Appended at
    /// ProcedureBound, where the binder makes the decision (<c>IntrinsicBinder.KeywordOmittedFunction</c>,
    /// <c>SubscriptBinder.IsFunctionBearing</c>, <c>ObjectPropertyResolver.ResolveObjectProperty</c>), and read whole by
    /// <see cref="Validation.FlagConformancePass"/> for ISO §7.3.14.4 GR4 b ("the source element calls any function, or
    /// invokes any method") instead of re-deriving the rule from syntax. Identity-keyed: a node is a call site or it is
    /// not.</summary>
    public FactSet<object> ActivationSites { get; }

    /// <summary>An empty state: every fact set empty, each checking its appends through <see cref="Gate"/>.</summary>
    public DataBindState(Action<Antlr4.Runtime.RuleContext?, string, UserWordKind> recordConfigurationNameUse)
    {
        Configuration = new(recordConfigurationNameUse);
        Forest = new(Require);
        WholeGroupReferenced = new(FactSetKind.WholeGroupReferenced, Gate);
        ImageForcedItems = new(FactSetKind.ImageForcedItems, Gate);
        CharacterChannelItems = new(FactSetKind.CharacterChannelItems, Gate);
        ActivationSites = new(FactSetKind.ActivationSites, Gate, ReferenceEqualityComparer.Instance);
    }
}
