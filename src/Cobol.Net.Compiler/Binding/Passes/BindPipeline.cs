// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Collections.Generic;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Passes;

using Core = CobolParserCore;

/// <summary>
/// The DECLARED, asserted order of the bind pipeline (rearchitecture PHASE 05; DESIGN-data-model §2.5). Replaces
/// <c>DataBinder.BindResolve</c>'s comment-ordered method calls with one explicit list whose phase contract
/// (<see cref="IBindPass.Requires"/>/<see cref="IBindPass.Produces"/>) is verified at startup by
/// <see cref="ValidateDag"/> — structurally killing the "implicit pass-ordering" smell.
/// <para><b>PHASE-05 Step 3 is a no-op wrapper:</b> the list preserves the EXACT existing <c>BindResolve</c> order
/// (zero reorder, zero behavior change). The three <c>ProcedureBound</c>-and-later entries are the middle-end passes
/// the emitter drives (they need the BOUND tree, so they are NOT run from the resolve loop) — present here only so
/// the DAG is validated all the way to <see cref="PassPhase.StorageComputed"/>. <c>BindResolve</c> runs the
/// resolve-phase prefix (<see cref="IBindPass.Produces"/> &lt;= <see cref="PassPhase.FilesResolved"/>); the tail's
/// <see cref="IBindPass.Run"/> throws (it is never a resolve-loop pass in this phase). The real Bind-phase extraction
/// that drives the tail FROM the pipeline is PHASE 06.</para>
/// </summary>
internal static class BindPipeline
{
    /// <summary>Build the ordered pass list for one program unit's resolution (+ the DAG's middle-end tail). The two
    /// declaration-context binders close over <paramref name="program"/>; every other pass runs against the binder
    /// instance passed to <see cref="IBindPass.Run"/>. Built per <c>BindResolve</c> call because the two
    /// program-context passes need the parse tree — a trivial allocation; the SSOT for the pass order lives HERE.</summary>
    public static IReadOnlyList<IBindPass> Build(Core.ProgramUnitContext program)
    {
        // The middle-end tail is driven from the emitter (CallEmitRunUnit) — it needs the bound tree — so its Run is
        // never invoked through the resolve loop. Throw loudly if it ever is (a filter regression).
        static void DrivenByEmitter(DataBinder _) => throw new InvalidOperationException(
            "This is a middle-end pass driven from the emitter (it needs the bound tree); it is present in the DAG "
            + "for validation only and must not be run from the resolve loop (PHASE-05 Step 3).");

        return new IBindPass[]
        {
            // ── The resolution passes, in the EXACT current BindResolve order (DataBinder.cs). ──
            new BindPass("ExpandTypes", PassPhase.None, PassPhase.TypesExpanded, d => d.ExpandTypes()),
            new BindPass("ResolveIndexItems", PassPhase.TypesExpanded, PassPhase.TypesExpanded, d => d.ResolveIndexItems()),
            new BindPass("InheritUsageClauses", PassPhase.TypesExpanded, PassPhase.UsageResolved, d => d.InheritUsageClauses()),
            new BindPass("InheritSignClauses", PassPhase.UsageResolved, PassPhase.SignResolved, d => d.InheritSignClauses()),
            new BindPass("ResolveRedefines", PassPhase.SignResolved, PassPhase.SignResolved, d => d.ResolveRedefines()),
            new BindPass("ClassifyRedefinesClasses", PassPhase.SignResolved, PassPhase.RedefinesClassified, d => d.ClassifyRedefinesClasses()),
            new BindPass("CheckStrongTypeDeclarations", PassPhase.RedefinesClassified, PassPhase.StrongTypeChecked, d => d.CheckStrongTypeDeclarations()),
            new BindPass("OoRouteMethodRedefinesBackings", PassPhase.RedefinesClassified, PassPhase.StrongTypeChecked, d => d.OoRouteMethodRedefinesBackings()),
            new BindPass("OdoResolve", PassPhase.StrongTypeChecked, PassPhase.OccursResolved, d => d.OdoResolve()),
            new BindPass("DynamicResolve", PassPhase.OccursResolved, PassPhase.OccursResolved, d => d.DynamicResolve()),
            new BindPass("ResolveFiles", PassPhase.OccursResolved, PassPhase.FilesResolved, d => d.ResolveFiles()),
            new BindPass("GateNationalRecords", PassPhase.FilesResolved, PassPhase.FilesResolved, d => d.GateNationalRecords()),
            new BindPass("ResolveReports", PassPhase.FilesResolved, PassPhase.FilesResolved, d => d.ResolveReports()),
            new BindPass("CallBindExternalAndGlobal", PassPhase.FilesResolved, PassPhase.FilesResolved, d => d.CallBindExternalAndGlobal(program)),
            new BindPass("PtrBindBasedAndAddressables", PassPhase.FilesResolved, PassPhase.FilesResolved, d => d.PtrBindBasedAndAddressables(program)),
            new BindPass("MarkFileRecordImageLeaves", PassPhase.FilesResolved, PassPhase.FilesResolved, d => d.MarkFileRecordImageLeaves()),

            // ── The middle-end tail: driven from the emitter (needs the bound tree). Present for DAG completeness. ──
            new BindPass("ProcedureBinding", PassPhase.FilesResolved, PassPhase.ProcedureBound, DrivenByEmitter),
            new BindPass("UsageCollectionPass", PassPhase.ProcedureBound, PassPhase.UsageCollected, DrivenByEmitter),
            new BindPass("StorageFormPass", PassPhase.UsageCollected, PassPhase.StorageComputed, DrivenByEmitter),
        };
    }

    /// <summary>The highest phase a pass may <see cref="IBindPass.Produces"/> and still be driven by the resolve loop.
    /// Passes above this (the middle-end tail) need the bound tree and are driven from the emitter.</summary>
    public const PassPhase LastResolvePhase = PassPhase.FilesResolved;

    /// <summary>Assert the pass list is a monotone phase chain: every pass's <see cref="IBindPass.Requires"/> is
    /// already produced by a preceding pass, and no pass's <see cref="IBindPass.Produces"/> regresses below the running
    /// high-water mark. Throws <see cref="InvalidOperationException"/> on the first violation. Called once at process
    /// start (the top of the first <c>BindResolve</c>) so a mis-ordering fails immediately, not silently.</summary>
    public static void ValidateDag(IReadOnlyList<IBindPass> passes)
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
