// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;

using CobolNet.Binding.Model;

namespace CobolNet.Binding.Passes;

/// <summary>
/// The ordered MILESTONE phases of the bind pipeline (rearchitecture PHASE 05; DESIGN-data-model §2.5). A pass
/// declares the phase it <see cref="IPassInfo.Requires"/> (the high-water mark that must already be reached) and the
/// phase it <see cref="IPassInfo.Produces"/>; <c>BindPipeline.ValidateDag</c> asserts, at startup, that the declared
/// order is a monotone chain — no pass reads a fact before its producing pass. This structurally kills the "implicit
/// pass-ordering" smell (passes were ordered only by call sequence + comments).
/// <para>The enum's ORDINAL order is the phase order — <c>Requires &lt;= produced-so-far</c> and
/// <c>Produces</c> non-regressing are ordinal comparisons. Several passes may share a <c>Produces</c> phase (an
/// intermediate pass that advances no new milestone declares <c>Produces</c> equal to the running high-water mark).
/// The phases up to <see cref="FilesResolved"/> are per-UNIT resolve passes (<c>DataBinder.BindResolve</c> over
/// <c>BindPipeline.Build</c>); <see cref="ProcedureBound"/> and later are GROUP passes over the whole compilation,
/// driven by <c>BinderDriver.Bind</c> through <c>BindPipeline.GroupTail</c> (P6 Step 3 — they need every unit's
/// BOUND tree). ONE <c>ValidateFullChainOnce</c> covers both sections as a single declared chain.</para>
/// </summary>
public enum PassPhase
{
    /// <summary>Nothing produced yet — the pipeline entry point (the first pass requires this).</summary>
    None,
    /// <summary>TYPEDEF/<c>TYPE IS</c> clones expanded into the forest (<c>ExpandTypes</c>).</summary>
    TypesExpanded,
    /// <summary>USAGE markers + INDEX items resolved/inherited (<c>UsageInheritancePass</c> — the merged
    /// former <c>ResolveIndexItems</c> + <c>InheritUsageClauses</c> pair, P5.11e).</summary>
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
    /// <summary>Every unit's PROCEDURE DIVISION is bound to a <c>BoundProgram</c> (the <c>ProcedureBinding</c> group
    /// pass: the user-function signature table, then <c>StatementBinder.Bind</c> per unit).</summary>
    ProcedureBound,
    /// <summary>Whole-group operand references collected from the bound tree (<c>UsageCollectionPass</c>; PHASE-05
    /// Step 5). A group pass.</summary>
    UsageCollected,
    /// <summary>The canonical <c>StorageForm</c> computed once for every item, with every <c>StoreAsImage</c> flag
    /// settled first (<c>StorageFormPass.Run</c>: compiler-temp re-sync → whole-group image marking → OO override
    /// harmonize → classify; PHASE-06 Step 3). A group pass.</summary>
    StorageComputed,
    /// <summary>Every version-gated construct checked against the targeted edition (the
    /// <c>VersionConformancePass</c> — the manifest's NAMED TERMINAL pass and the SOLE
    /// <c>ConstructRegistry.Check</c> caller; PHASE-06 Step 4 / the P3 pipeline design). The driver's
    /// <c>CheckOnly</c> verdict is settled once this phase is reached — emit never runs on an errored tree.</summary>
    EditionConformanceChecked,
}

/// <summary>The declared metadata of one pipeline pass — the name + <see cref="Requires"/>/<see cref="Produces"/>
/// phase contract <c>BindPipeline.ValidateDag</c> checks, shared by the per-unit resolve passes
/// (<see cref="IBindPass"/>) and the whole-group middle-end passes (<see cref="GroupBindPass"/>) so ONE validation
/// covers the FULL chain (P6 Step 3).</summary>
public interface IPassInfo
{
    /// <summary>The pass's stable identifier (used in the DAG-violation diagnostic).</summary>
    string Name { get; }

    /// <summary>The phase that must already be reached before this pass may run.</summary>
    PassPhase Requires { get; }

    /// <summary>The phase reached after this pass completes (&gt;= the running high-water mark).</summary>
    PassPhase Produces { get; }
}

/// <summary>
/// One declared PER-UNIT resolve pass in the bind pipeline: the <see cref="IPassInfo"/> contract plus the work it
/// runs over a single <see cref="DataBinder"/>'s forest (PHASE-05 Step 3; the manifest is <c>BindPipeline.Build</c>).
/// </summary>
public interface IBindPass : IPassInfo
{
    /// <summary>Run the pass over the binder's forest.</summary>
    void Run(DataBinder data);
}

/// <summary>The canonical <see cref="IBindPass"/> implementation: a name + phase contract + a delegate body.</summary>
internal sealed record BindPass(string Name, PassPhase Requires, PassPhase Produces, Action<DataBinder> Body)
    : IBindPass
{
    public void Run(DataBinder data) => Body(data);
}
