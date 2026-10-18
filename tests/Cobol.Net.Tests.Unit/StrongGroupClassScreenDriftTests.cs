// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.IO;
using System.Linq;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A RULE WORDED "AN ALPHANUMERIC (OR NATIONAL) DATA ITEM" ASKS THE CATEGORY READER, NEVER THE CLASS LATTICE'S
/// ALPHANUMERIC ANSWER (kb/Work PB3355). <c>IntrinsicArgumentRules.ClassOfItem</c> answers ALPHANUMERIC for a
/// strongly-typed group because ISO §15.3 item 2 says a function ARGUMENT is treated so; §8.5.2.1 makes the class and
/// category of the group its type-name everywhere else. CALL's and CANCEL's identifier-1 (§14.9.4.3 SR1, §14.9.5.3
/// SR1) and the report CODE clause's identifier-1 (§13.18.12.3 SR2) asked the lattice and accepted the group, which
/// the call then read as a program-name; the EVALUATE range's same-class rule (§14.9.13.3 SR4) compared the two
/// alphanumeric answers and called a strongly-typed group the same class as any alphanumeric item.
/// <para><b>What is pinned.</b> (1) The three category screens read <c>ItemCategory</c>, and the range screen reads
/// the one same-class identity, <c>IntrinsicArgumentRules.SameClass</c>. (2) The only Binding sources that name
/// <c>CobolClass.Alphanumeric</c> or <c>CobolClass.National</c> are the ones below, each for a stated reason: a new
/// class-lattice screen that admits alphanumeric or national must either ask <c>ItemCategory</c> (a rule worded in
/// category), ask <c>SameClass</c> (a pair rule worded in class), or be a function-argument position §15.3 governs
/// and be listed here with that reason.</para>
/// </summary>
public sealed class StrongGroupClassScreenDriftTests
{
    /// <summary>Every Binding source that names the lattice's alphanumeric or national member, and why it may.</summary>
    private static readonly (string Path, string Reason)[] Allowed =
    [
        ("IntrinsicArgumentRules.cs", "the lattice itself, and the §15 function-argument screens §15.3 item 2 governs"),
        ("Procedure/Verbs/IntrinsicBinder.cs", "NATIONAL-OF / DISPLAY-OF arguments: function-argument positions (§15.3)"),
        ("Procedure/Verbs/ParameterConformance.cs", "the class a figurative constant or an ALL literal crosses BY VALUE as"),
        ("Procedure/Verbs/StringUnstringBinder.cs", "the all-or-nothing national rule; a strongly-typed group is screened beside it (Sr1Offence, PB244)"),
        ("Procedure/Verbs/InspectBinder.cs", "INSPECT SR4's governing national class; identifier-1 and the phrase operands are screened elementary (PB856)"),
        ("RelationComparability.cs", "the §8.8.4.2 class pair of a relation; a strongly-typed group meets the relation checkpoint's own rule (§8.8.4.2.3 SR1)"),
    ];

    private static string BindingRoot() => TestRepo.Src(["Cobol.Net.Compiler", "Binding"]);

    [Fact]
    public void OnlyTheListedBindingSources_NameTheLatticesAlphanumericOrNationalClass()
    {
        string root = BindingRoot();
        var offenders = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Select(f => (File: f, Rel: Path.GetRelativePath(root, f).Replace('\\', '/')))
            .Where(t => !Allowed.Any(a => a.Path == t.Rel))
            .SelectMany(t => File.ReadLines(t.File).Select((line, i) => (t.Rel, line, i)))
            .Where(t => !t.line.TrimStart().StartsWith("//", StringComparison.Ordinal)
                        && (t.line.Contains("CobolClass.Alphanumeric", StringComparison.Ordinal)
                            || t.line.Contains("CobolClass.National", StringComparison.Ordinal)))
            .Select(t => $"{t.Rel}:{t.i + 1}: {t.line.Trim()}")
            .ToList();
        Assert.True(offenders.Count == 0,
            "a rule worded 'alphanumeric/national data item' asks ItemCategory (§8.5.2.1: the category); a pair rule worded 'same class' "
            + "asks IntrinsicArgumentRules.SameClass; the lattice answers ALPHANUMERIC for a strongly-typed group (§15.3 item 2, a function "
            + "ARGUMENT). A function-argument position is added to Allowed with its reason (kb/Work PB3355):\n"
            + string.Join("\n", offenders));
    }

    [Fact]
    public void EveryAllowedSource_StillNamesTheClass()
    {
        string root = BindingRoot();
        foreach (var (path, reason) in Allowed)
        {
            string text = File.ReadAllText(Path.Combine(root, path));
            Assert.True(
                text.Contains("CobolClass.Alphanumeric", StringComparison.Ordinal)
                || text.Contains("CobolClass.National", StringComparison.Ordinal),
                $"{path} no longer names the class ({reason}): remove its entry");
        }
    }

    [Theory]
    [InlineData("Procedure/Verbs/CallBinder.cs", "ItemCategory.IsAlphanumericOrNational(target)")]
    [InlineData("Procedure/Verbs/CallBinder.cs", "NameTargetRefusal(place)")]
    [InlineData("Procedure/Verbs/CallBinder.cs", "NameTargetRefusal(p)")]
    [InlineData("DataBinder.Reports.cs", "!ItemCategory.IsAlphanumeric(item)")]
    [InlineData("Procedure/Verbs/EvaluateBinder.cs", "IntrinsicArgumentRules.SameClass(lo, lc, hi, hc)")]
    public void TheScreen_AsksItsOneReader(string path, string reader)
    {
        string text = File.ReadAllText(Path.Combine(BindingRoot(), path));
        Assert.True(text.Contains(reader, StringComparison.Ordinal),
            $"{path}: the screen no longer asks {reader} (kb/Work PB3355)");
    }
}
