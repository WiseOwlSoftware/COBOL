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
/// <item>no row's name is a method of <c>DataBinder</c>: a pass owns its body, or its concern binder does (<c>ResolveFilesPass</c>
/// schedules <c>FileControlBinder.ResolveFiles</c>, the last row whose rule code stayed on the binder until kb/Work PB2296 M7),
/// and a method of the row's name left on the binder would be a second way to run the rule;</item>
/// <item>a pass's own source never appends to a fact set its <see cref="IPassInfo.Views"/> does not declare, and while a
/// pass runs <see cref="AppendGate"/> refuses an append it did not declare, so the declaration cannot drift from what the
/// pass, or the binder code it calls, does.</item>
/// </list>
/// </summary>
public sealed class BindPassBoundaryDriftTests
{
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
    public void NoRowsName_IsAMethodOfDataBinder()
    {
        const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (var row in Rows)
            Assert.True(typeof(DataBinder).GetMethod(row.Name, all) is null,
                $"DataBinder still has a method {row.Name}, the entry of the pass that owns the rule: a second way to run it " +
                $"(move the body into {row.GetType().Name}, or into the concern binder the pass schedules, and delete the method)");
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
