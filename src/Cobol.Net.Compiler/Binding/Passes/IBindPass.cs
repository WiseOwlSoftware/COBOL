// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;

namespace CobolNet.Binding.Passes;

/// <summary>
/// The ordered MILESTONE phases of the bind pipeline (rearchitecture PHASE 05; DESIGN-data-model §2.5). A pass
/// declares the phase it <see cref="IBindPass.Requires"/> (the high-water mark that must already be reached) and the
/// phase it <see cref="IBindPass.Produces"/>; <c>BindPipeline.ValidateDag</c> asserts, at startup, that the declared
/// order is a monotone chain — no pass reads a fact before its producing pass. This structurally kills the "implicit
/// pass-ordering" smell (passes were ordered only by call sequence + comments).
/// <para>The enum's ORDINAL order is the phase order — <c>Requires &lt;= produced-so-far</c> and
/// <c>Produces</c> non-regressing are ordinal comparisons. Several passes may share a <c>Produces</c> phase (an
/// intermediate pass that advances no new milestone declares <c>Produces</c> equal to the running high-water mark).
/// <c>ProcedureBound</c>/<c>UsageCollected</c>/<c>StorageComputed</c> are reached by the middle-end passes that run
/// from the emitter (they need the BOUND tree), present in the DAG for completeness (§2.5 steps 8–10).</para>
/// </summary>
public enum PassPhase
{
    /// <summary>Nothing produced yet — the pipeline entry point (the first pass requires this).</summary>
    None,
    /// <summary>TYPEDEF/<c>TYPE IS</c> clones expanded into the forest (<c>ExpandTypes</c>).</summary>
    TypesExpanded,
    /// <summary>USAGE markers + INDEX items resolved/inherited (<c>InheritUsageClauses</c>).</summary>
    UsageResolved,
    /// <summary>Group-level SIGN clauses inherited (<c>InheritSignClauses</c>).</summary>
    SignResolved,
    /// <summary>REDEFINES/RENAMES targets resolved + overlaid items grouped into shared-storage classes with a tier
    /// (<c>ClassifyRedefinesClasses</c>).</summary>
    RedefinesClassified,
    /// <summary>Strong-type declaration rules checked (<c>CheckStrongTypeDeclarations</c>).</summary>
    StrongTypeChecked,
    /// <summary>OCCURS DEPENDING ON + OCCURS DYNAMIC resolved (<c>OdoResolve</c>/<c>DynamicResolve</c>).</summary>
    OccursResolved,
    /// <summary>FILE STATUS items, national record gates, reports, EXTERNAL/GLOBAL + BASED/addressable cells, and the
    /// FILE whole-group image leaves resolved (<c>ResolveFiles</c> … <c>MarkFileRecordImageLeaves</c>).</summary>
    FilesResolved,
    /// <summary>The PROCEDURE DIVISION is bound to a <c>BoundProgram</c> (<c>StatementBinder.Bind</c>, driven from the
    /// emitter). Not a <c>DataBinder</c> pass — present for DAG completeness.</summary>
    ProcedureBound,
    /// <summary>Whole-group operand references collected from the bound tree (<c>UsageCollectionPass</c>; PHASE-05
    /// Step 5). Driven from the emitter.</summary>
    UsageCollected,
    /// <summary>The canonical <c>StorageForm</c> computed once for every item (<c>StorageFormPass</c>; PHASE-05
    /// Step 2/10). Driven from the emitter.</summary>
    StorageComputed,
}

/// <summary>
/// One declared pass in the bind pipeline: a name, its <see cref="Requires"/>/<see cref="Produces"/> phase contract
/// (validated by <c>BindPipeline.ValidateDag</c>), and the work it runs over a <see cref="DataBinder"/>.
/// (PHASE-05 Step 3; the pass framework is a no-op wrapper over the existing <c>BindResolve</c> passes in this phase —
/// zero reorder, zero behavior change; the real Bind-phase extraction + <c>BindModel</c> result object is PHASE 06.)
/// </summary>
public interface IBindPass
{
    /// <summary>The pass's stable identifier (used in the DAG-violation diagnostic).</summary>
    string Name { get; }

    /// <summary>The phase that must already be reached before this pass may run.</summary>
    PassPhase Requires { get; }

    /// <summary>The phase reached after this pass completes (&gt;= the running high-water mark).</summary>
    PassPhase Produces { get; }

    /// <summary>Run the pass over the binder's forest.</summary>
    void Run(DataBinder data);
}

/// <summary>The canonical <see cref="IBindPass"/> implementation: a name + phase contract + a delegate body.</summary>
internal sealed record BindPass(string Name, PassPhase Requires, PassPhase Produces, Action<DataBinder> Body)
    : IBindPass
{
    public void Run(DataBinder data) => Body(data);
}
