// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using CobolNet.Binding.Passes;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// PHASE-05 Step 3: the declared bind pipeline (<see cref="BindPipeline"/>) + its startup DAG assert
/// (<see cref="BindPipeline.ValidateDag"/>). These pin the ordering CONTRACT — a monotone Requires/Produces chain —
/// proving it ACCEPTS the canonical order and REJECTS a reordering that reads a fact before its producing pass. The
/// real <c>BindResolve</c> pipeline running green across the whole conformance battery is the behavior-neutrality
/// proof; here we pin the guard itself.
/// </summary>
public sealed class BindPipelineTests
{
    /// <summary>The REAL production pipeline (<see cref="BindPipeline.Build"/>) is a valid monotone DAG. Build's two
    /// declaration-context passes capture the program, but <see cref="BindPipeline.ValidateDag"/> only reads the
    /// Name/Requires/Produces metadata (never runs a pass), so a null program is safe here.</summary>
    [Fact]
    public void RealPipeline_IsAMonotoneDag()
    {
        var pipeline = BindPipeline.Build(program: null!);
        BindPipeline.ValidateDag(pipeline);   // must not throw
        Assert.Equal(PassPhase.StorageComputed, pipeline[^1].Produces);   // the DAG runs all the way to the last phase
    }

    /// <summary>A hand-built canonical chain (each pass requires only what a prior pass produced) validates.</summary>
    [Fact]
    public void ValidateDag_CanonicalOrder_DoesNotThrow()
    {
        var passes = new IBindPass[]
        {
            new BindPass("a", PassPhase.None, PassPhase.TypesExpanded, _ => { }),
            new BindPass("b", PassPhase.TypesExpanded, PassPhase.UsageResolved, _ => { }),
            new BindPass("c", PassPhase.UsageResolved, PassPhase.UsageResolved, _ => { }),   // intermediate, no new milestone
            new BindPass("d", PassPhase.UsageResolved, PassPhase.StorageComputed, _ => { }),
        };
        BindPipeline.ValidateDag(passes);   // must not throw
    }

    /// <summary>A pass that requires a phase no preceding pass has produced is rejected — the reorder-before-prereq
    /// case (e.g. StorageFormPass placed before UsageCollectionPass produces its input).</summary>
    [Fact]
    public void ValidateDag_RequiresBeforeProduced_Throws()
    {
        var passes = new IBindPass[]
        {
            new BindPass("storage", PassPhase.UsageCollected, PassPhase.StorageComputed, _ => { }),
            new BindPass("usage", PassPhase.ProcedureBound, PassPhase.UsageCollected, _ => { }),
        };
        var ex = Assert.Throws<InvalidOperationException>(() => BindPipeline.ValidateDag(passes));
        Assert.Contains("storage", ex.Message);
    }

    /// <summary>A pass whose Produces regresses below the running high-water mark is rejected (monotonicity).</summary>
    [Fact]
    public void ValidateDag_ProducesRegression_Throws()
    {
        var passes = new IBindPass[]
        {
            new BindPass("a", PassPhase.None, PassPhase.FilesResolved, _ => { }),
            new BindPass("b", PassPhase.None, PassPhase.UsageResolved, _ => { }),   // regresses below FilesResolved
        };
        Assert.Throws<InvalidOperationException>(() => BindPipeline.ValidateDag(passes));
    }
}
