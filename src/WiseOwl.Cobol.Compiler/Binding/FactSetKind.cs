// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Passes;

namespace WiseOwl.Cobol.Binding;

/// <summary>The identity of each append-only fact set of <see cref="DataBindState"/> (docs/rearchitecture/DESIGN-architecture-review.md
/// §2023:8.3 (1), kb/Work PB2352): the name a pass uses to DECLARE that it appends to, reads early or reads whole one of them
/// (<see cref="PassViews"/>), which <c>BindPipeline.ValidateDag</c> checks against <see cref="FactSetContract"/> and
/// <see cref="AppendGate"/> checks against what the pass really does.</summary>
public enum FactSetKind
{
    /// <summary><see cref="DataBindState.WholeGroupReferenced"/>.</summary>
    WholeGroupReferenced,
    /// <summary><see cref="DataBindState.ImageForcedItems"/>.</summary>
    ImageForcedItems,
    /// <summary><see cref="DataBindState.CharacterChannelItems"/>.</summary>
    CharacterChannelItems,
    /// <summary><see cref="DataBindState.ActivationSites"/>.</summary>
    ActivationSites,
}

/// <summary>When each fact set may be appended to, and when it is complete: the contract a pass's declared
/// <see cref="PassViews"/> is checked against (§2023:8.3 (1): "a fact set is appended only in a phase that declares it, read
/// early only by a pass that says so, and read whole only after its last appender"). One switch per question, so a new
/// <see cref="FactSetKind"/> that has no row does not compile.</summary>
public static class FactSetContract
{
    /// <summary>The phases whose passes may append to <paramref name="kind"/>. A pass's phase is its
    /// <see cref="IPassInfo.Produces"/>.</summary>
    public static IReadOnlyList<PassPhase> AppendPhases(FactSetKind kind) => kind switch
    {
        // Group items used as a whole character image: the FILE record areas at FilesResolved
        // (MarkFileRecordImageLeaves), the boundary formals and the bound tree's whole-group operands at UsageCollected.
        FactSetKind.WholeGroupReferenced => [PassPhase.FilesResolved, PassPhase.UsageCollected],
        // Elementary items forced to their character image: the Tier-B REDEFINES leaves at RedefinesClassified
        // (ClassifyRedefinesClasses), FILE record leaves, report print faces and CALL cells at FilesResolved, the
        // figurative-fill and ref-mod-store receivers while the procedure division binds (ProcedureBound).
        FactSetKind.ImageForcedItems => [PassPhase.RedefinesClassified, PassPhase.FilesResolved, PassPhase.ProcedureBound],
        FactSetKind.CharacterChannelItems => [PassPhase.UsageCollected],
        FactSetKind.ActivationSites => [PassPhase.ProcedureBound],
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "a fact set kind has no contract row"),
    };

    /// <summary>The last phase that appends to <paramref name="kind"/>: a whole read (<see cref="IReads{T}"/>) is legal
    /// only in a later phase.</summary>
    public static PassPhase LastAppendPhase(FactSetKind kind) => AppendPhases(kind)[^1];
}
