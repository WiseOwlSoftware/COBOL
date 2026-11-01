// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.CodeGen;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE NEUTRAL TREE NAMES A METHOD LOCAL BY ITS IDENTITY, AND ONE BACKEND FUNCTION SPELLS IT (kb/Work PB4321;
/// DESIGN-backend-abstraction.md §1.2, §1.3 L8). A local's name is no ABI — no other module binds to it, and a CIL method
/// addresses its locals by slot — so the binder's loop counters, CORRESPONDING anchors, table(ALL) index vectors and report
/// VARYING counters are <see cref="StatementLocal"/>s (the declaring construct and an ordinal) and the Roslyn backend's
/// <see cref="LocalNames.Of"/> turns each into its C# identifier. Before PB4321 the binders formatted <c>__iniN</c>,
/// <c>__vlmN</c>, <c>__corrNs</c>, <c>__allN</c>, <c>__holdN</c> and <c>__kvN</c> themselves and froze them into
/// <c>PositionLocal.CsName</c>, <c>LocalVector.CsName</c>, <c>InitializeLoop.Var</c>, <c>TableElementMove.Var</c>,
/// <c>CorrespondingHoist.Local</c>, a CORRESPONDING anchor's <c>RootFieldSegment</c> and <c>BoundReportVaryingRef.CsName</c>.
/// These facts keep it from coming back:
/// <list type="bullet">
/// <item>no type of the neutral tree (the <c>WiseOwl.Cobol.Binding</c> namespaces) has a <c>string</c> property named for a
/// local (<c>CsName</c>, <c>Var</c>, <c>Local</c>, <c>…Local</c>, <c>…Var</c>);</item>
/// <item>no binder source spells a stem <see cref="LocalNames"/> gives a role — the spelling has one home;</item>
/// <item>every role has a spelling, and no two roles' locals of one ordinal share it.</item>
/// </list>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class MethodLocalIdentityDriftTests
{
    private static readonly Regex LocalShapedName = new(@"^(CsName|Var|Local|.+Local|.+Var)$", RegexOptions.Compiled);

    [Fact]
    public void NoNeutralTreeType_HoldsALocalsName()
    {
        var offenders = typeof(StatementLocal).Assembly.GetTypes()
            .Where(t => t.Namespace is { } ns && (ns == "WiseOwl.Cobol.Binding" || ns.StartsWith("WiseOwl.Cobol.Binding.", StringComparison.Ordinal)))
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(p => p.PropertyType == typeof(string) && LocalShapedName.IsMatch(p.Name))
                .Select(p => $"{t.FullName}.{p.Name}"))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();
        Assert.True(offenders.Count == 0,
            "A neutral-tree type names a method local by a C# spelling; carry its identity (a MethodLocal) and let the backend "
            + "name it (kb/Work PB4321). Offenders: " + string.Join(", ", offenders));
    }

    [Fact]
    public void NoBinderSource_SpellsALocalStem()
    {
        var stems = Enum.GetValues<StatementLocalRole>().Select(Stem).Distinct(StringComparer.Ordinal).ToList();
        var literal = new Regex(@"\$?@?""(?:[^""\\\r\n]|\\.)*""", RegexOptions.Compiled);
        var offenders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding"), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)) continue;
            string code = Regex.Replace(Regex.Replace(File.ReadAllText(file), @"/\*.*?\*/", " ", RegexOptions.Singleline), @"//[^\n]*", " ");
            foreach (Match lit in literal.Matches(code))
                foreach (string stem in stems)
                    if (Regex.IsMatch(lit.Value, $@"(?<![A-Za-z0-9_]){Regex.Escape(stem)}(?:\{{|[0-9]|""|$)"))
                        offenders.Add($"{Path.GetFileName(file)}: {lit.Value}");
        }
        Assert.True(offenders.Count == 0,
            "A binder spells a method local's C# name; the name is LocalNames.Of's alone (kb/Work PB4321). Offenders: "
            + string.Join(", ", offenders));
    }

    [Fact]
    public void EveryRole_IsSpelled_AndNoTwoRolesShareASpelling()
    {
        var names = Enum.GetValues<StatementLocalRole>().Select(r => LocalNames.Of(new StatementLocal(r, 7))).ToList();
        Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
        Assert.All(names, n => Assert.Matches(@"^__[a-z]+7[a-z]?$", n));
    }

    /// <summary>A role's stem: its spelling at ordinal 0, up to the ordinal.</summary>
    private static string Stem(StatementLocalRole role)
    {
        string name = LocalNames.Of(new StatementLocal(role, 0));
        return name[..name.IndexOf('0', StringComparison.Ordinal)];
    }
}
