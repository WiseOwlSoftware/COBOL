// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ INITIALIZE'S REPLACING PHRASE AND ITS CATEGORY ROWS EACH HAVE ONE HOME (kb/Work PB2344;
/// docs/rearchitecture/DESIGN-architecture-review.md §8.3, census R0-0049). <c>ReplacingPhraseBinder</c> binds the REPLACING
/// phrase (ISO §2023:14.9.20.2) and asks its syntax rules (§2023:14.9.20.3 SR3, SR4's SET and MOVE halves, SR6), producing the
/// <c>InitializeReplacingItem</c> nodes the statement carries; <c>InitializeCategories</c> (Binding/Bound/BoundInitialize.cs)
/// holds the rows that give an item, or a reference-modified slice, its INITIALIZE category; <c>InitializeBinder</c> binds the
/// statement and expands its receivers. This test keeps each rule from growing back into the statement binder:
/// <list type="bullet">
/// <item>the REPLACING items are read (<c>.initializeReplacingItem()</c>) and their SR4 screens declared only in
/// <c>ReplacingPhraseBinder.cs</c>;</item>
/// <item>the item and slice category rows are declared only in <c>BoundInitialize.cs</c>.</item>
/// </list>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class InitializeBinderBoundaryDriftTests
{
    private static IEnumerable<(string File, string Text)> CompilerSources() =>
        Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Select(f => (Path.GetRelativePath(TestRepo.Root, f).Replace('\\', '/'), File.ReadAllText(f)));

    [Theory]
    [InlineData(@"\.initializeReplacingItem\(\)", "/Binding/Procedure/Verbs/ReplacingPhraseBinder.cs")]
    [InlineData(@"\bvoid CheckReplacingMoveValidity\(", "/Binding/Procedure/Verbs/ReplacingPhraseBinder.cs")]
    [InlineData(@"\bvoid CheckSetFormCategoryAgreement\(", "/Binding/Procedure/Verbs/ReplacingPhraseBinder.cs")]
    [InlineData(@"\bInitializeCategory\? InitializeItemCategory\(DataItem", "/Binding/Bound/BoundInitialize.cs")]
    [InlineData(@"\bInitializeCategory RefModInitializeCategory\(Place", "/Binding/Bound/BoundInitialize.cs")]
    public void TheRule_HasOneHome(string pattern, string home)
    {
        var rx = new Regex(pattern);
        var sites = CompilerSources().Where(s => rx.IsMatch(s.Text)).Select(s => s.File).ToList();
        Assert.True(sites.Count > 0, $"the scan found no `{pattern}` — the pattern no longer matches the code");
        Assert.True(sites.All(f => f.EndsWith(home, StringComparison.Ordinal)),
            $"`{pattern}` is written outside {home} (kb/Work PB2344): " + string.Join(", ", sites));
    }

    [Fact]
    public void TheStatementBinder_BindsTheReplacingPhraseThroughItsOneCollaborator()
    {
        string statement = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "Procedure", "Verbs", "InitializeBinder.cs"));
        Assert.Contains("private readonly ReplacingPhraseBinder _replacing = new(ctx, host);", statement, StringComparison.Ordinal);
        Assert.Contains("_replacing.Bind(replacing)", statement, StringComparison.Ordinal);
    }
}
