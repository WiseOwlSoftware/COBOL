// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Collections.Generic;
using System.Linq;
using CobolNet.Frontend.Generated;

using CobolNet.Binding.Model;

namespace CobolNet.Binding.Passes;

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
        new BindPass("ExpandTypes", PassPhase.None, PassPhase.TypesExpanded, d => d.ExpandTypes()),
        new BindPass("UsageInheritancePass", PassPhase.TypesExpanded, PassPhase.UsageResolved, d => d.UsageInheritancePass()),
        // The §13.18.60.3 USAGE declaration-PLACEMENT screen — SR14/SR15/SR4 (kb/Work PB183). Placed HERE, and
        // not one pass earlier or later, for two reasons. It needs UsageInheritancePass to have settled
        // §13.18.60.4 GR1 group-usage inheritance, so an INHERITED pointer usage is screened exactly as a
        // written one. And it must precede ClassifyRedefinesClasses, whose Tier-D backstop rejects a nested
        // pointer/object leaf under REDEFINES with a message that names THIS rule as the missing screen: the
        // declaration's own defect should be reported before the downstream machinery's defense against it.
        // A plain syntax rule, so NOT the terminal VersionConformancePass — that pass is the edition gate.
        new BindPass("CheckUsageDeclarations", PassPhase.UsageResolved, PassPhase.UsageResolved, d => d.CheckUsageDeclarations()),
        new BindPass("InheritSignClauses", PassPhase.UsageResolved, PassPhase.SignResolved, d => d.InheritSignClauses()),
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
            // Fully qualified: CobolNet.Binding.Validation (StatementValidation, P7 Step 10c) shadows the
            // relative `Validation.` lookup from inside CobolNet.Binding.*.
            ctx => CobolNet.Validation.VersionConformancePass.Run(ctx, ctx.Session.Edition.Edition, ctx.Session.Edition)),
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
        }
    }
}
