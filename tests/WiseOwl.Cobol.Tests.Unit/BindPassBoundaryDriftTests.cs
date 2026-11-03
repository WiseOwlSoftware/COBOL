// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Passes;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EACH ROW OF THE PER-UNIT RESOLVE PIPELINE IS ONE <see cref="IBindPass"/> CLASS, AND IT DECLARES THE FACT SETS IT
/// APPENDS TO (kb/Work PB2352, PB2353, PB2354; docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1)).
/// <para>Steps 2-4 of the <c>DataBinder</c> extract replaced the <c>BindPass</c> lambda rows (<c>d =&gt; d.ExpandTypes()</c>)
/// with one class per row, each carrying its name, its phase contract, its <see cref="PassViews"/> and the body of the rule
/// it owns. <c>BindPipelineTests</c> pins the phase order and the views against <see cref="FactSetContract"/>; this class
/// pins the boundary the extraction drew:</para>
/// <list type="bullet">
/// <item>the rows of <c>BindPipeline.Build</c> and the <see cref="IBindPass"/> classes of the assembly are the same set, each
/// class once, each in a file named for it (the drift that left a pass class unregistered, or a lambda row beside the
/// classes, is a red test);</item>
/// <item>a pass that owns its body leaves no method of its entry's name on <c>DataBinder</c> (a forwarder would be a second
/// way to run the pass); the row whose rule code belongs to a concern binder still to come
/// (<see cref="ConcernOwned"/>) is the named exception, and the list is closed: a row leaves it when its rule code leaves
/// <c>DataBinder</c> (a stale entry fails here), and while it stays an OPEN <c>kb/Work</c> note must name it, so the exception
/// always has an owner and an end condition (<c>work.py check</c> holds that half, since no compiler test reads
/// <c>kb/Work</c>: scripts/inert_lane.py, kb/Work PB4453);</item>
/// <item>a pass's own source never appends to a fact set its <see cref="IPassInfo.Views"/> does not declare, and while a
/// pass runs <see cref="AppendGate"/> refuses an append it did not declare, so the declaration cannot drift from what the
/// pass, or the binder code it calls, does.</item>
/// </list>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class BindPassBoundaryDriftTests
{
    /// <summary>The rows whose rule code still lives on <c>DataBinder</c>: the pass only schedules it.
    /// <c>DataBinder.ResolveFiles</c> (with <c>BindFileSection</c>) is the post-build half of the FILE concern: PB2356 moved the
    /// SELECT / I-O-CONTROL half into <c>FileControlBinder</c> and left this half for the next FILE step, which kb/Work
    /// PB2296 (Remaining, M7) owns and which ends this entry. <c>ResolveReports</c> left the list when <c>ReportSectionBinder</c> landed (PB2357), <c>CallBindExternalAndGlobal</c>
    /// when <c>LinkageBinder</c> did (PB2359), <c>OoRouteMethodRedefinesBackings</c> when <c>OoDataBinder</c> did (PB2360).</summary>
    private static readonly string[] ConcernOwned = ["ResolveFiles"];

    private static IReadOnlyList<IBindPass> Rows => BindPipeline.Build(program: null!);

    private static string PassSource(IBindPass row) =>
        File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "Passes", row.GetType().Name + ".cs"));

    [Fact]
    public void TheRowsOfBuild_AreExactlyTheIBindPassClasses_EachOnceInAFileNamedForIt()
    {
        var rowTypes = Rows.Select(r => r.GetType()).ToList();
        Assert.Equal(rowTypes.Count, rowTypes.Distinct().Count());
        var classes = typeof(IBindPass).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IBindPass).IsAssignableFrom(t))
            .Select(t => t.Name).Order().ToArray();
        Assert.Equal(classes, rowTypes.Select(t => t.Name).Order().ToArray());
        foreach (var row in Rows)
            Assert.True(File.Exists(TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "Passes", row.GetType().Name + ".cs")),
                $"{row.GetType().Name} is not in Binding/Passes/{row.GetType().Name}.cs (one class per file, named for it)");
    }

    [Fact]
    public void ThePassNameOfEveryRow_IsItsEntrysName()
    {
        // The Name is the identifier the DAG diagnostics and the watermark gate print, and the row number a reader finds in
        // the design (§8.3 (1)): a class `XPass` runs the row named X (UsageInheritancePass is its own name).
        foreach (var row in Rows)
        {
            string expected = row.GetType().Name;
            if (expected.EndsWith("Pass", StringComparison.Ordinal) && expected != row.Name) expected = expected[..^"Pass".Length];
            Assert.Equal(expected, row.Name);
        }
    }

    [Fact]
    public void ADataBinderMethodOfAPassesName_ExistsOnlyForTheConcernOwnedRows()
    {
        const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (var row in Rows)
        {
            bool onBinder = typeof(DataBinder).GetMethod(row.Name, all) is not null;
            bool owned = ConcernOwned.Contains(row.Name);
            Assert.True(onBinder == owned,
                owned
                    ? $"{row.Name} is listed as concern-owned but DataBinder no longer has it: its concern binder landed, so delete it from ConcernOwned and let the pass own the call"
                    : $"DataBinder still has a method {row.Name}, the entry of the pass that owns the rule: a second way to run it (move the body into {row.GetType().Name}, or list the row in ConcernOwned with the concern binder that will take it)");
        }
    }

    [Fact]
    public void APassesOwnSource_AppendsOnlyToTheFactSetsItDeclares()
    {
        var append = new Regex(@"\b(WholeGroupReferenced|ImageForcedItems|CharacterChannelItems|ActivationSites)\.Add\(");
        int found = 0;
        foreach (var row in Rows)
            foreach (Match m in append.Matches(PassSource(row)))
            {
                found++;
                var kind = Enum.Parse<FactSetKind>(m.Groups[1].Value);
                Assert.True(row.Views.AppendsTo.Contains(kind),
                    $"{row.GetType().Name} appends to {kind} but does not declare it in Views");
            }
        Assert.True(found >= 2, $"the scan found only {found} appends in the pass sources — the pattern no longer matches the code");
    }

    [Fact]
    public void AnAppend_ToAFactSetTheRunningPassDidNotDeclare_IsRefused()
    {
        var state = new DataBindState((_, _, _) => { });
        var none = new Pass("none", PassViews.None);
        var declares = new Pass("declares", PassViews.Appends(FactSetKind.ActivationSites));

        state.ActivationSites.Add(new object());   // no pass running: the entry walk and the procedure division append freely
        using (state.Gate.Running(declares)) state.ActivationSites.Add(new object());
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using (state.Gate.Running(none)) state.ActivationSites.Add(new object());
        });
        Assert.Contains("none", ex.Message);
        Assert.Contains("ActivationSites", ex.Message);
        var other = Assert.Throws<InvalidOperationException>(() =>
        {
            using (state.Gate.Running(declares)) state.CharacterChannelItems.Add(null!);
        });
        Assert.Contains("CharacterChannelItems", other.Message);
        state.CharacterChannelItems.Add(null!);   // the gate closed with the pass
    }

    private sealed record Pass(string Name, PassViews Views) : IPassInfo
    {
        public PassPhase Requires => PassPhase.None;
        public PassPhase Produces => PassPhase.None;
    }
}
