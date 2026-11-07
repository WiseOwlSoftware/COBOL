// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Binding.Passes;

using Core = CobolParserCore;

/// <summary>
/// The DECLARED, asserted order of the bind pipeline (rearchitecture PHASE 05 Step 3 / PHASE 06 Step 3). Replaces
/// the two formerly-hidden pipelines — <c>DataBinder.BindResolve</c>'s comment-ordered method calls and the
/// middle-end sequence the fused emitter ran — with explicit manifests whose phase contract
/// (<see cref="IPassInfo.Requires"/>/<see cref="IPassInfo.Produces"/>) is verified at startup by
/// <see cref="ValidateDag"/> over the FULL chain (<see cref="ValidateFullChainOnce"/>).
/// <para><b>Two sections, one chain:</b> <see cref="Build"/> is the per-UNIT resolve prefix (run by
/// <c>DataBinder.BindResolve</c> against each unit/class forest, up to <see cref="PassPhase.FilesResolved"/>);
/// <see cref="GroupTail"/> is the whole-GROUP middle-end (run by <c>BinderDriver.Bind</c> once per compilation —
/// procedure binding, usage collection, the storage-form computation; the passes that need every unit's BOUND
/// tree). The concatenation is validated as ONE monotone chain, so a group pass reading a resolve-produced fact
/// too early is a startup error exactly like a mis-ordered resolve pass.</para>
/// </summary>
internal static class BindPipeline
{
    /// <summary>Build the ordered pass list for one program unit's resolution. The two declaration-context binders
    /// close over <paramref name="program"/>; every other pass runs against the binder instance passed to
    /// <see cref="IBindPass.Run"/>. Built per <c>BindResolve</c> call because the two program-context passes need
    /// the parse tree — a trivial allocation; the SSOT for the pass order lives HERE.</summary>
    public static IReadOnlyList<IBindPass> Build(Core.ProgramUnitContext program) => new IBindPass[]
    {
        // ── The per-unit resolution passes, in the EXACT pre-P5 BindResolve order (DataBinder.cs). ──
        new SynthesizeImpliedPicturesPass(),
        new ExpandTypesPass(),
        new CheckConstantRecordSubtreesPass(),
        new UsageInheritancePass(),
        new CheckUsageDeclarationsPass(),
        new CheckAlignedClausesPass(),
        new CheckConditionNameAssociationsPass(),
        new CheckPictureRequiredPass(),
        new CheckClauseSubjectsPass(),
        new CheckSignClausesPass(),
        new InheritSignClausesPass(),
        new CheckCodeSetRecordItemsPass(),
        new ResolveRedefinesPass(),
        new ClassifyRedefinesClassesPass(),
        new CheckStrongTypeDeclarationsPass(),
        new OoRouteMethodRedefinesBackingsPass(),
        new CheckGroupValueDeclarationsPass(),
        new OdoResolvePass(),
        new DynamicResolvePass(),
        new OccursKeyResolvePass(),
        new ResolveTableValuesPass(),
        new CheckValueLiteralClassesPass(),
        new CheckCommunicationRecordsPass(),
        new ResolveFilesPass(),
        new GateFileRecordByteSurfacePass(),
        new ResolveReportsPass(),
        new CallBindExternalAndGlobalPass(program),
        new PtrBindBasedAndAddressablesPass(program),
        new RouteStaticUnitStoragePass(),
        new MarkFileRecordImageLeavesPass(),
    };

    /// <summary>The whole-GROUP middle-end manifest (P6 Steps 3–4 — the formerly-hidden second pipeline, now
    /// declared): run by <c>BinderDriver.Bind</c> once per compilation, in this order, after every unit/class DATA
    /// division is bound. Each body needs every unit's BOUND tree or the settled whole-group facts. The TERMINAL
    /// pass is the <c>VersionConformancePass</c> — the SOLE edition gate (P3 pipeline design §2.4): a
    /// <c>CheckOnly</c> compile's verdict is complete once Bind returns, and the driver halts before emit on any
    /// diagnostics.</summary>
    public static IReadOnlyList<GroupBindPass> GroupTail() => new GroupBindPass[]
    {
        // Every unit's PROCEDURE DIVISION binds (user-function signature table first — §2023:8.4.3.2.4 GR1 forward refs).
        new("ProcedureBinding", PassPhase.FilesResolved, PassPhase.ProcedureBound, BinderDriver.BindProcedures)
        {
            // MoveBinder's figurative-fill and ref-mod-store receivers; the resolver's NumericImagePlace wrap reads the
            // set as it stands; IntrinsicBinder and the resolver mark the function and property call sites.
            Views = PassViews.Of(appends: [FactSetKind.ImageForcedItems, FactSetKind.ActivationSites],
                readsEarly: [FactSetKind.ImageForcedItems], readsWhole: []),
        },
        // Whole-group operand collection from the BOUND trees + boundary formals (PHASE-05 Step 5; DEVLOG 752/753).
        new("UsageCollectionPass", PassPhase.ProcedureBound, PassPhase.UsageCollected, UsageCollectionPass.Run)
        {
            Views = PassViews.Appends(FactSetKind.WholeGroupReferenced, FactSetKind.CharacterChannelItems),
        },
        // Settle every StoreAsImage flag (temp re-sync → image marking → OO harmonize), then classify StorageForm.
        new("StorageFormPass", PassPhase.UsageCollected, PassPhase.StorageComputed, StorageFormPass.Run)
        {
            Views = PassViews.Of(appends: [], readsEarly: [],
                readsWhole: [FactSetKind.ImageForcedItems, FactSetKind.WholeGroupReferenced, FactSetKind.CharacterChannelItems]),
        },
        // Every Place the backend asks of an ITEM (not of a written reference) is built here, once, in the model layer
        // (kb/Work PB2375): the storage-form pass settled the early-image facts the builder reads.
        new("PlaceTablePass", PassPhase.StorageComputed, PassPhase.PlacesBuilt, PlaceTablePass.Run),
        // THE edition gate (P3's two-arm pass, now the NAMED terminal pass — P6 Step 4): the parse-tree arm fires
        // every syntactic introduction/removal/phrase gate on RECOGNITION; the bound-tree arm fires the
        // genuinely-semantic gates. Reports through the session's edition sink, so the Bind result carries every
        // edition diagnostic for BOTH a full compile and a CheckOnly verdict.
        new("VersionConformancePass", PassPhase.StorageComputed, PassPhase.EditionConformanceChecked,
            // Fully qualified: WiseOwl.Cobol.Binding.Validation (StatementValidation, P7 Step 10c) shadows the
            // relative `Validation.` lookup from inside WiseOwl.Cobol.Binding.*.
            ctx => WiseOwl.Cobol.Validation.VersionConformancePass.Run(ctx, ctx.Session.Edition.Edition, ctx.Session.Edition)),
    };

    /// <summary>Assert the FULL declared chain (the per-unit resolve prefix + the group tail) is a monotone phase
    /// chain, once per process — the pass-list shape is compile-time-fixed, so validating once suffices ("startup
    /// assert"; a benign race merely re-validates). Called from the first <c>BindResolve</c> AND the first
    /// <c>BinderDriver.Bind</c>, whichever runs first.</summary>
    public static void ValidateFullChainOnce()
    {
        if (_chainValidated) return;
        // Build's two declaration-context passes capture the program, but ValidateDag only reads the
        // Name/Requires/Produces metadata (never runs a pass), so a null program is safe here.
        ValidateDag([.. Build(program: null!), .. GroupTail()]);
        _chainValidated = true;
    }

    private static bool _chainValidated;

    /// <summary>Assert the pass list is a monotone phase chain: every pass's <see cref="IPassInfo.Requires"/> is
    /// already produced by a preceding pass, and no pass's <see cref="IPassInfo.Produces"/> regresses below the running
    /// high-water mark. Throws <see cref="InvalidOperationException"/> on the first violation.</summary>
    public static void ValidateDag(IReadOnlyList<IPassInfo> passes)
    {
        var produced = PassPhase.None;   // the running high-water mark over the passes seen so far
        foreach (var p in passes)
        {
            if (p.Requires > produced)
                throw new InvalidOperationException(
                    $"BindPipeline order violation: pass '{p.Name}' requires phase {p.Requires}, but the preceding "
                    + $"passes have only produced {produced}. A pass was reordered before its prerequisite.");
            if (p.Produces < produced)
                throw new InvalidOperationException(
                    $"BindPipeline monotonicity violation: pass '{p.Name}' produces phase {p.Produces}, regressing "
                    + $"below the already-produced {produced}.");
            if (p.Produces > produced) produced = p.Produces;
            ValidateViews(p);
        }
    }

    /// <summary>Assert one pass's declared <see cref="PassViews"/> against <see cref="FactSetContract"/> (kb/Work PB2352): a
    /// pass appends to a fact set only in a phase that set admits (the pass's phase is its <see cref="IPassInfo.Produces"/>),
    /// and reads it whole only in a phase after the last that appends. A mid-bind read (<see cref="PassViews.ReadsEarly"/>)
    /// is legal wherever the pass says it reads. The declaration is checked against what the pass does by
    /// <see cref="AppendGate"/>.</summary>
    private static void ValidateViews(IPassInfo p)
    {
        foreach (var kind in p.Views.AppendsTo)
            if (!FactSetContract.AppendPhases(kind).Contains(p.Produces))
                throw new InvalidOperationException(
                    $"BindPipeline views violation: pass '{p.Name}' (phase {p.Produces}) appends to {kind}, which admits "
                    + $"appends only in {string.Join(", ", FactSetContract.AppendPhases(kind))}.");
        foreach (var kind in p.Views.ReadsWhole)
            if (p.Produces <= FactSetContract.LastAppendPhase(kind))
                throw new InvalidOperationException(
                    $"BindPipeline views violation: pass '{p.Name}' (phase {p.Produces}) reads {kind} whole, but its last "
                    + $"appender runs in {FactSetContract.LastAppendPhase(kind)}: a whole read is legal only after it.");
    }
}
