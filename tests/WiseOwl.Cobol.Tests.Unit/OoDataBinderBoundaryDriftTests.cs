// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE OO HALF OF THE DATA BINDER IS ONE TYPE, AND THE COMPILER TEMPORARIES ARE MADE IN ONE PLACE (kb/Work PB2360;
/// docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1), step 10 of the <c>DataBinder</c> extract).
/// <para><c>DataBinder.Oo.cs</c> held a class unit's method data, its PROPERTY accessors, the <c>USAGE OBJECT REFERENCE</c>
/// adjudication, the per-method name scopes, the compiler-temporary constructors and the receiver claims in one partial of
/// the god class. They now live in <see cref="OoDataBinder"/> (reached as <c>DataBinder.Oo</c>), <c>CompilerTemps</c>
/// (<c>DataBinder.Temps</c>) and <c>ProcedureBindState</c>. What the types cannot say, this test does:</para>
/// <list type="bullet">
/// <item>no <c>DataBinder.Oo*.cs</c> partial comes back, and <see cref="DataBinder"/> declares no <c>Oo…</c> member beyond
/// the OO configuration it is made with and the one door, <c>Oo</c> — a new OO fact goes on <see cref="OoDataBinder"/>;</item>
/// <item>the OO binder is made in exactly one place, by <see cref="DataBinder"/>, from the role the driver gave it;</item>
/// <item>a compiler temporary (<c>IsCompilerTemp = true</c>) is built only by <c>CompilerTemps.Create</c>, so §8.4.3.2.4
/// GR1's "the description, class, and category" copy has one constructor.</item>
/// </list>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class OoDataBinderBoundaryDriftTests
{
    private static IEnumerable<(string File, string Text)> CompilerSources() =>
        Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Select(f => (Path.GetRelativePath(TestRepo.Root, f).Replace('\\', '/'), File.ReadAllText(f)));

    /// <summary>The <c>Oo…</c> members <see cref="DataBinder"/> keeps: the class table the group shares, the unit's REPOSITORY
    /// PROPERTY specifiers (configuration of the configuration section, not OO data), the role the driver makes it with, and
    /// the door to the OO half.</summary>
    private static readonly string[] AllowedOoMembers = ["Oo", "OoClasses", "OoRepositoryProperties", "OoRole"];

    [Fact]
    public void TheDataBinder_HasNoOoPartial_AndNoOoMemberBeyondItsDoor()
    {
        var partials = Directory.EnumerateFiles(Path.Combine(TestRepo.Src("WiseOwl.Cobol.Compiler"), "Binding"), "DataBinder.Oo*.cs");
        Assert.Empty(partials);

        const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.DeclaredOnly;
        var members = typeof(DataBinder).GetMembers(all)
            .Where(m => m.Name.StartsWith("Oo", StringComparison.Ordinal) && m is not Type)
            .Select(m => m.Name.StartsWith("get_", StringComparison.Ordinal) ? m.Name[4..] : m.Name)
            .Distinct().Order(StringComparer.Ordinal).ToList();
        Assert.Contains("Oo", members);   // the scan sees the door, so an empty verdict cannot be a scan that saw nothing
        var extra = members.Except(AllowedOoMembers).ToList();
        Assert.True(extra.Count == 0,
            "DataBinder declares OO members beyond its door: " + string.Join(", ", extra)
            + ". A class unit's data, its method roster and its per-method scopes are OoDataBinder's (DataBinder.Oo; kb/Work PB2360).");
    }

    [Fact]
    public void TheOoDataBinder_IsMadeOnlyByTheDataBinder()
    {
        var makers = new List<string>();
        foreach (var (file, text) in CompilerSources())
            if (Regex.IsMatch(text, @"new\s+OoDataBinder\s*\("))
                makers.Add(file);
        Assert.Equal(["src/WiseOwl.Cobol.Compiler/Binding/DataBinder.cs"], makers);
    }

    [Fact]
    public void ACompilerTemporary_IsBuiltOnlyByTheOneCreate()
    {
        var builders = new List<string>();
        foreach (var (file, text) in CompilerSources())
            if (Regex.IsMatch(text, @"IsCompilerTemp\s*=\s*true"))
                builders.Add(file);
        Assert.Equal(["src/WiseOwl.Cobol.Compiler/Binding/CompilerTemps.cs"], builders);
    }
}
