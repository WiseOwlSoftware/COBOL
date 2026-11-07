// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE SPECIAL-NAMES BINDER (WHICH DECLARES INTO THE CONFIGURATION SLICE) AND THE FILE-CONTROL BINDER OWN WHAT THEY BIND, AND THE DATA BINDER KEEPS NO SECOND
/// DOOR TO IT (kb/Work PB2355, PB2356; docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1)).
/// <para><c>SpecialNamesBinder</c> (the SPECIAL-NAMES facts, ISO §2023:12.3.6-§2023:12.3.7) and <c>FileControlBinder</c> (the
/// FILE-CONTROL entries and the I-O-CONTROL paragraph, §2023:12.4.5-§2023:12.4.6) were partials of <c>DataBinder</c>. Each is now a
/// class of its own, reached through <c>DataBinder.SpecialNames</c> and <c>DataBinder.FileControl</c>, and the owner's rule
/// is that a move leaves no forwarder behind (CLAUDE.md rule 4). The C# types hold the boundary except for two things
/// they cannot say, which this test says:</para>
/// <list type="bullet">
/// <item>the data binder declares no member that has the NAME AND PARAMETER TYPES of one a binder declares, so no
/// forwarding property or method (<c>DataBinder.Alphabets =&gt; State.Configuration.Alphabets</c>) can reappear beside the
/// moved member;</item>
/// <item>each binder is made in one place, the data binder's own <c>DataBinder.Configuration.cs</c>, from the data binder
/// and its edition, so a unit has exactly one of each and a reader never holds a second copy of its state.</item>
/// </list>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ConfigurationBindersBoundaryDriftTests
{
    private const BindingFlags AllDeclared = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
        | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    private static IEnumerable<string> Signatures(Type type) =>
        type.GetMembers(AllDeclared)
            .Where(m => m is MethodInfo { IsSpecialName: false } or PropertyInfo or FieldInfo)
            .Where(m => m is not FieldInfo f || !f.Name.Contains('<'))
            .Select(m => m switch
            {
                MethodInfo mi => $"{mi.Name}({string.Join(",", mi.GetParameters().Select(p => p.ParameterType.FullName))})",
                PropertyInfo pi => pi.Name,
                _ => m.Name,
            })
            .Where(s => !s.StartsWith('_') && !s.StartsWith("get_", StringComparison.Ordinal))
            .Distinct();

    /// <summary>Each binder keeps its own private <c>Edition</c> (the data binder's, handed in at construction); the
    /// data binder's public <c>Edition</c> is its own member, not a forwarder.</summary>
    private static readonly HashSet<string> OwnNames = ["Edition"];

    [Theory]
    [InlineData(typeof(SpecialNamesBinder))]
    [InlineData(typeof(FileControlBinder))]
    public void TheDataBinder_DeclaresNoMemberOf_AMovedBinder(Type binder)
    {
        var moved = Signatures(binder).Where(s => !OwnNames.Contains(s)).ToHashSet();
        var data = Signatures(typeof(DataBinder)).ToHashSet();
        // the scan must see both sides, or an empty intersection proves nothing
        Assert.True(moved.Count >= 10, $"{binder.Name} reflects only {moved.Count} members — the scan lost them");
        Assert.True(data.Count >= 200, $"DataBinder reflects only {data.Count} members — the scan lost them");
        var shared = moved.Intersect(data).Order().ToList();
        Assert.True(shared.Count == 0,
            $"DataBinder still declares members {binder.Name} declares ({string.Join(", ", shared)}): a moved member has ONE home — "
            + "change its callers to ask the binder (Data.SpecialNames / Data.FileControl) and delete the old one (CLAUDE.md rule 4)");
    }

    /// <summary>The SPECIAL-NAMES binder DECLARES into the unit's <see cref="ConfigurationSlice"/> and holds none of its
    /// facts (kb/Work PB2296 M5 with PB2355): a member of the same name and parameter types on both would be a second
    /// owner of a configuration fact, and a reader could then ask either.</summary>
    [Fact]
    public void TheSpecialNamesBinder_DeclaresNoMemberOf_TheConfigurationSlice()
    {
        var binder = Signatures(typeof(SpecialNamesBinder)).ToHashSet();
        var slice = Signatures(typeof(ConfigurationSlice)).ToHashSet();
        Assert.True(slice.Count >= 20, $"ConfigurationSlice reflects only {slice.Count} members — the scan lost them");
        var shared = binder.Intersect(slice).Order().ToList();
        Assert.True(shared.Count == 0,
            $"SpecialNamesBinder declares members the ConfigurationSlice declares ({string.Join(", ", shared)}): the fact has ONE home, "
            + "the slice; the binder writes it through State.Configuration (CLAUDE.md rule 4)");
    }

    [Theory]
    [InlineData("SpecialNamesBinder", @"new\(this, Edition\)")]
    [InlineData("FileControlBinder", @"new\(this, Edition\)")]
    public void EachBinder_IsMadeOnlyInTheDataBindersConfigurationPartial(string binder, string made)
    {
        var offenders = new List<string>();
        int homes = 0;
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler"), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")) continue;
            string text = File.ReadAllText(file);
            string name = Path.GetFileName(file);
            if (Regex.IsMatch(text, $@"\bnew\s+{binder}\s*\("))
                offenders.Add($"{name}: new {binder}(…)");
            if (name == "DataBinder.Configuration.cs"
                && Regex.IsMatch(text, $@"public {binder} \w+ => field \?\?= {made};"))
                homes++;
        }
        Assert.True(homes == 1, $"DataBinder.Configuration.cs does not make {binder} once from the data binder and its edition");
        Assert.True(offenders.Count == 0, $"{binder} is made outside its one home: {string.Join(", ", offenders)}");
    }
}
