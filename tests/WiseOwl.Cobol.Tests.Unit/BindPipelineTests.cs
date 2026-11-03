// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Passes;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// PHASE-05 Step 3 / PHASE-06 Step 3: the declared bind pipeline (<see cref="BindPipeline"/>) + its startup DAG
/// assert (<see cref="BindPipeline.ValidateDag"/>). These pin the ordering CONTRACT — a monotone Requires/Produces
/// chain over the per-unit resolve prefix (<see cref="BindPipeline.Build"/>) AND the whole-group middle-end tail
/// (<see cref="BindPipeline.GroupTail"/>) — proving it ACCEPTS the canonical order and REJECTS a reordering that
/// reads a fact before its producing pass. The real pipeline running green across the whole conformance battery is
/// the behavior-neutrality proof; here we pin the guard itself.
/// </summary>
public sealed class BindPipelineTests
{
    /// <summary>The REAL production chain — the per-unit resolve prefix concatenated with the group tail — is a
    /// valid monotone DAG that runs all the way to <see cref="PassPhase.StorageComputed"/>. Build's two
    /// declaration-context passes capture the program, but <see cref="BindPipeline.ValidateDag"/> only reads the
    /// Name/Requires/Produces metadata (never runs a pass), so a null program is safe here.</summary>
    [Fact]
    public void RealPipeline_FullChain_IsAMonotoneDag()
    {
        IPassInfo[] chain = [.. BindPipeline.Build(program: null!), .. BindPipeline.GroupTail()];
        BindPipeline.ValidateDag(chain);   // must not throw
        // The DAG runs all the way to the terminal phase, and the terminal pass IS the edition gate (P6 Step 4 /
        // exit criterion #6 — the VersionConformancePass is a NAMED pass, the manifest's last entry).
        Assert.Equal(PassPhase.EditionConformanceChecked, chain[^1].Produces);
        Assert.Equal("VersionConformancePass", chain[^1].Name);
    }

    /// <summary>A hand-built canonical chain (each pass requires only what a prior pass produced) validates.</summary>
    [Fact]
    public void ValidateDag_CanonicalOrder_DoesNotThrow()
    {
        var passes = new IPassInfo[]
        {
            new TestPass("a", PassPhase.None, PassPhase.TypesExpanded),
            new TestPass("b", PassPhase.TypesExpanded, PassPhase.UsageResolved),
            new TestPass("c", PassPhase.UsageResolved, PassPhase.UsageResolved),   // intermediate, no new milestone
            new TestPass("d", PassPhase.UsageResolved, PassPhase.StorageComputed),
        };
        BindPipeline.ValidateDag(passes);   // must not throw
    }

    /// <summary>A pass that requires a phase no preceding pass has produced is rejected — the reorder-before-prereq
    /// case (e.g. StorageFormPass placed before UsageCollectionPass produces its input). Mixed per-unit + group
    /// entries, exactly like the production chain.</summary>
    [Fact]
    public void ValidateDag_RequiresBeforeProduced_Throws()
    {
        var passes = new IPassInfo[]
        {
            new GroupBindPass("storage", PassPhase.UsageCollected, PassPhase.StorageComputed, _ => { }),
            new GroupBindPass("usage", PassPhase.ProcedureBound, PassPhase.UsageCollected, _ => { }),
        };
        var ex = Assert.Throws<InvalidOperationException>(() => BindPipeline.ValidateDag(passes));
        Assert.Contains("storage", ex.Message);
    }

    /// <summary>A pass whose Produces regresses below the running high-water mark is rejected (monotonicity).</summary>
    [Fact]
    public void ValidateDag_ProducesRegression_Throws()
    {
        var passes = new IPassInfo[]
        {
            new TestPass("a", PassPhase.None, PassPhase.FilesResolved),
            new TestPass("b", PassPhase.None, PassPhase.UsageResolved),   // regresses below FilesResolved
        };
        Assert.Throws<InvalidOperationException>(() => BindPipeline.ValidateDag(passes));
    }

    /// <summary>A group pass moved AHEAD of the resolve prefix it depends on is rejected — the cross-section
    /// ordering the ONE-chain validation exists to guard (P6 Step 3).</summary>
    [Fact]
    public void ValidateDag_GroupTailBeforeResolvePrefix_Throws()
    {
        IPassInfo[] chain = [.. BindPipeline.GroupTail(), .. BindPipeline.Build(program: null!)];
        Assert.Throws<InvalidOperationException>(() => BindPipeline.ValidateDag(chain));
    }

    /// <summary>A pass appending to a fact set in a phase the set does not admit is rejected: the Tier-B REDEFINES leaves
    /// are image-forced at RedefinesClassified or later, never while the types are still being expanded
    /// (<see cref="FactSetContract"/>; kb/Work PB2352).</summary>
    [Fact]
    public void ValidateDag_AppendInAPhaseTheSetDoesNotAdmit_Throws()
    {
        var passes = new IPassInfo[]
        {
            new TestPass("early", PassPhase.None, PassPhase.TypesExpanded, PassViews.Appends(FactSetKind.ImageForcedItems)),
        };
        var ex = Assert.Throws<InvalidOperationException>(() => BindPipeline.ValidateDag(passes));
        Assert.Contains("early", ex.Message);
        Assert.Contains("ImageForcedItems", ex.Message);
    }

    /// <summary>A pass reading a fact set whole in or before the phase of its last appender is rejected (§8.3 (1): a whole
    /// read is legal only after the last phase that appends).</summary>
    [Fact]
    public void ValidateDag_WholeReadNotAfterTheLastAppender_Throws()
    {
        var passes = new IPassInfo[]
        {
            new TestPass("collect", PassPhase.None, PassPhase.UsageCollected, PassViews.Appends(FactSetKind.CharacterChannelItems)),
            new TestPass("read", PassPhase.UsageCollected, PassPhase.UsageCollected,
                PassViews.Of([], [], [FactSetKind.CharacterChannelItems])),
        };
        var ex = Assert.Throws<InvalidOperationException>(() => BindPipeline.ValidateDag(passes));
        Assert.Contains("read", ex.Message);
    }

    /// <summary>The same reader one phase later validates, and so does an append in an admitted phase.</summary>
    [Fact]
    public void ValidateDag_DeclaredViewsInTheirPhases_DoNotThrow()
    {
        var passes = new IPassInfo[]
        {
            new TestPass("collect", PassPhase.None, PassPhase.UsageCollected, PassViews.Appends(FactSetKind.CharacterChannelItems)),
            new TestPass("read", PassPhase.UsageCollected, PassPhase.StorageComputed,
                PassViews.Of([], [FactSetKind.CharacterChannelItems], [FactSetKind.CharacterChannelItems])),
        };
        BindPipeline.ValidateDag(passes);   // must not throw
    }

    private sealed record TestPass(string Name, PassPhase Requires, PassPhase Produces, PassViews Views = default) : IPassInfo;
}
