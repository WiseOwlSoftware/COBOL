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
        // The §13.18.13.3 SR3 a)/b) CODE-SET record screen — §13.18.52.3 SR3's twin (kb/Work PB536). Placed
        // HERE, immediately after InheritSignClauses, and the placement IS the fix: SR3 asks whether a record
        // item is "described with the SIGN IS SEPARATE clause" and "described as usage display", and BOTH facts
        // can arrive by GROUP INHERITANCE — §13.18.52.4 GR1 for the sign, §13.18.60.4 GR1 for the usage — so the
        // screen must run no earlier than the last pass that can supply one. Run from BindFileSection, where it
        // used to live, it read the entry-bind default trailing over-punch and REFUSED a legal record whose
        // group carried SIGN IS SEPARATE. Nothing later is needed: it reads declared shape only, and ExpandTypes
        // (the first pass) has already composed the leaves of a record described with TYPE or SAME AS.
        new BindPass("CheckCodeSetRecordItems", PassPhase.SignResolved, PassPhase.SignResolved, d => d.CheckCodeSetRecordItems()),
        new BindPass("ResolveRedefines", PassPhase.SignResolved, PassPhase.SignResolved, d => d.ResolveRedefines()),
        new BindPass("ClassifyRedefinesClasses", PassPhase.SignResolved, PassPhase.RedefinesClassified, d => d.ClassifyRedefinesClasses()),
        new BindPass("CheckStrongTypeDeclarations", PassPhase.RedefinesClassified, PassPhase.StrongTypeChecked, d => d.CheckStrongTypeDeclarations()),
        new BindPass("OoRouteMethodRedefinesBackings", PassPhase.RedefinesClassified, PassPhase.StrongTypeChecked, d => d.OoRouteMethodRedefinesBackings()),
        // The §13.18.63.3 SR13/SR14 group-level VALUE subordinate screen (kb/Work PB184). Placed HERE because
        // SR14's "explicitly or IMPLICITLY described with usage DISPLAY" needs UsageInheritancePass to have
        // propagated a group-level USAGE down to the leaves (§13.18.60.4 GR1) — an inherited COMP must be
        // caught exactly as a written one is — and nothing later than that: it reads only declared shape.
        new BindPass("CheckGroupValueDeclarations", PassPhase.StrongTypeChecked, PassPhase.StrongTypeChecked, d => d.CheckGroupValueDeclarations()),
        new BindPass("OdoResolve", PassPhase.StrongTypeChecked, PassPhase.OccursResolved, d => d.OdoResolve()),
        new BindPass("DynamicResolve", PassPhase.OccursResolved, PassPhase.OccursResolved, d => d.DynamicResolve()),
        // The OCCURS KEY phrase (§13.18.38.3 SR3) resolved ONCE through the §8.4.2.2 resolver, qualifiers and all
        // (kb/Work PB1018) — after the forest and every TYPEDEF clone exist, before SEARCH ALL / the table SORT read it.
        new BindPass("OccursKeyResolve", PassPhase.OccursResolved, PassPhase.OccursResolved, d => d.OccursKeyResolve()),
        // The §13.18.63.3 SR18–SR23 Format-2 (table) VALUE geometry + the §13.18.63.4 GR12–GR16 odometer
        // resolution (kb/Work PB505; DataBinder.TableValue.cs). Placed HERE, and no earlier, because every one of
        // those rules is written against the entry's OCCURS ANCESTORS — DataItem.Parent is assigned only after
        // the entry binds — and because the dimension ceilings it measures subscripts against are the ones
        // OdoResolve / DynamicResolve have settled (§8.5.1.8's allocated maximum, the dynamic expected capacity).
        // Nothing later than that: it reads declared shape only, and the emitters read the plan it produces.
        new BindPass("ResolveTableValues", PassPhase.OccursResolved, PassPhase.OccursResolved, d => d.ResolveTableValues()),
        // The §13.18.63.3 SR7 VALUE-literal CLASS screen (kb/Work PB646). Placed HERE for two reasons, and it
        // needs both. SR7 reads §8.5.2.1 Table 2's CLASS of a numeric-edited item, which IS its usage
        // ("Numeric-edited (if usage is display)" / "(if usage is national)"), so UsageInheritancePass must have
        // settled §13.18.60.4 GR1 — an INHERITED usage has to give the same verdict as a written one, in both
        // directions. And the rule reaches formats 2 and 5 as well, whose per-occurrence literals only exist
        // once ResolveTableValues has built the plan. Nothing later is needed: it reads declared shape only.
        new BindPass("CheckValueLiteralClasses", PassPhase.OccursResolved, PassPhase.OccursResolved, d => d.CheckValueLiteralClasses()),
        new BindPass("ResolveFiles", PassPhase.OccursResolved, PassPhase.FilesResolved, d => d.ResolveFiles()),
        new BindPass("GateFileRecordByteSurface", PassPhase.FilesResolved, PassPhase.FilesResolved, d => d.GateFileRecordByteSurface()),
        new BindPass("ResolveReports", PassPhase.FilesResolved, PassPhase.FilesResolved, d => d.ResolveReports()),
        new BindPass("CallBindExternalAndGlobal", PassPhase.FilesResolved, PassPhase.FilesResolved, d => d.CallBindExternalAndGlobal(program)),
        new BindPass("PtrBindBasedAndAddressables", PassPhase.FilesResolved, PassPhase.FilesResolved, d => d.PtrBindBasedAndAddressables(program)),
        // A RECURSIVE unit's WS → the static-field channel (§13.5.4 GR1 / §14.6.2.3.3; no-op unless the binder's
        // UnitStaticWs). AFTER the pointer pass — the last tier-overwrite seam — so Tier-B backings, EXTERNAL
        // re-basing, and BASED/ADDRESS-OF forcing are settled facts when the roots route.
        new BindPass("RouteStaticUnitStorage", PassPhase.FilesResolved, PassPhase.FilesResolved, d => d.RouteStaticUnitStorage()),
        new BindPass("MarkFileRecordImageLeaves", PassPhase.FilesResolved, PassPhase.FilesResolved, d => d.MarkFileRecordImageLeaves()),
    };

    /// <summary>The whole-GROUP middle-end manifest (P6 Steps 3–4 — the formerly-hidden second pipeline, now
    /// declared): run by <c>BinderDriver.Bind</c> once per compilation, in this order, after every unit/class DATA
    /// division is bound. Each body needs every unit's BOUND tree or the settled whole-group facts. The TERMINAL
    /// pass is the <c>VersionConformancePass</c> — the SOLE edition gate (P3 pipeline design §2.4): a
    /// <c>CheckOnly</c> compile's verdict is complete once Bind returns, and the driver halts before emit on any
    /// diagnostics.</summary>
    public static IReadOnlyList<GroupBindPass> GroupTail() => new GroupBindPass[]
    {
        // Every unit's PROCEDURE DIVISION binds (user-function signature table first — §8.4.3.2.4 GR1 forward refs).
        new("ProcedureBinding", PassPhase.FilesResolved, PassPhase.ProcedureBound, BinderDriver.BindProcedures),
        // Whole-group operand collection from the BOUND trees + boundary formals (PHASE-05 Step 5; DEVLOG 752/753).
        new("UsageCollectionPass", PassPhase.ProcedureBound, PassPhase.UsageCollected, UsageCollectionPass.Run),
        // Settle every StoreAsImage flag (temp re-sync → image marking → OO harmonize), then classify StorageForm.
        new("StorageFormPass", PassPhase.UsageCollected, PassPhase.StorageComputed, StorageFormPass.Run),
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
