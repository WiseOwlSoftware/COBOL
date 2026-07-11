// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using CobolNet.Binding.Passes;
using Xunit;

namespace CobolNet.Tests.Unit;

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
            new BindPass("a", PassPhase.None, PassPhase.TypesExpanded, _ => { }),
            new BindPass("b", PassPhase.TypesExpanded, PassPhase.UsageResolved, _ => { }),
            new BindPass("c", PassPhase.UsageResolved, PassPhase.UsageResolved, _ => { }),   // intermediate, no new milestone
            new BindPass("d", PassPhase.UsageResolved, PassPhase.StorageComputed, _ => { }),
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
            new BindPass("a", PassPhase.None, PassPhase.FilesResolved, _ => { }),
            new BindPass("b", PassPhase.None, PassPhase.UsageResolved, _ => { }),   // regresses below FilesResolved
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
}
