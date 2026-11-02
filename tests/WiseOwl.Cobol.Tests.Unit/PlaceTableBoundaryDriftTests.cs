// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE CODE GENERATOR READS FINISHED PLACES AND BUILDS NONE BY ITEM (DESIGN-architecture-review §8.3 (4),
/// DESIGN-backend-abstraction §1.3 L6, kb/Work PB2375). Before PB2375 the emitters asked the reference resolver for a
/// <c>Place</c> at emit time (24 <c>ResolveItem</c> calls, so the backend input carried the binder). Now
/// <c>PlaceTablePass</c> builds every place the emitters need, once, in the bound tree (<c>BoundUnit.Places</c>), and
/// the emitters read them (<c>PlaceTable.Of</c> / <c>RecordArea</c>).
/// <para>The one remainder is the three windows of the Format-2 table SORT of a table in shared storage
/// (<c>SortEmitter</c>: two <c>PlaceTable.Window</c> calls and one <c>PlaceTable.CurrentOccurrenceCount</c>), whose
/// occurrence positions are the statement's run-time locals and so are not known when binding finishes (kb/Work PB599,
/// PB1175). This test pins that remainder at exactly those sites, so it can shrink (SortBinder building the windows
/// over its own <c>StatementLocal</c> positions) but never grow back.</para>
/// <para><b>Why a source scan.</b> The architecture-edge ratchet cannot see this: <c>CodeGen</c> to <c>Binding.Model</c>
/// is an allowed pair, and <c>PlaceBuilder</c>, <c>PlaceTable.Window</c> and an <c>IPlaceFacts</c> (the
/// <c>DataBinder</c> every emitter holds as <c>Ctx.Data</c>) are all public in that layer, so a later emitter that
/// builds a place by item compiles, passes every test that does not scan it, and silently restores emit-time place
/// building.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class PlaceTableBoundaryDriftTests
{
    private static readonly string CompilerRoot = TestRepo.Src("WiseOwl.Cobol.Compiler");
    private static readonly string CodeGenRoot = Path.Combine(CompilerRoot, "CodeGen");

    /// <summary>The names by which a place is built from an item: the builder, its fact interface, the resolver that used to
    /// own it, and the by-item entries.</summary>
    private static readonly Regex Builds = new(
        @"\b(PlaceBuilder|IPlaceFacts|ReferenceResolver|ResolveItem\w*|PlaceForItem|PlaceForRenames)\b",
        RegexOptions.CultureInvariant);

    private static readonly Regex WindowCall = new(@"\.Window\(", RegexOptions.CultureInvariant);
    private static readonly Regex CountCall = new(@"\.CurrentOccurrenceCount\(", RegexOptions.CultureInvariant);

    private static IEnumerable<(string File, int Line, string Text)> CodeLines(string root) =>
        Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.Combine("obj", ""), StringComparison.Ordinal)
                        && !f.Contains(Path.Combine("bin", ""), StringComparison.Ordinal))
            .SelectMany(f => File.ReadAllLines(f)
                .Select((text, i) => (File: f, Line: i + 1, Text: text.Trim()))
                .Where(l => !l.Text.StartsWith("//", StringComparison.Ordinal)));   // prose about a rule is not a site

    private static string Describe(IEnumerable<(string File, int Line, string Text)> sites) =>
        string.Join("\n", sites.Select(s => $"  {Path.GetRelativePath(CompilerRoot, s.File)}:{s.Line}: {s.Text}"));

    /// <summary>No file under <c>CodeGen</c> names the builder, its facts, the resolver or a by-item entry.</summary>
    [Fact]
    public void TheCodeGenerator_NamesNoPlaceBuilderAndNoByItemEntry()
    {
        var sites = CodeLines(CodeGenRoot).Where(l => Builds.IsMatch(l.Text)).ToList();
        Assert.True(sites.Count == 0,
            "the code generator builds a Place by item (or names the reference resolver). Every place an emitter needs is "
            + "built at bind time by PlaceTablePass and read from BoundUnit.Places / OoClassUnit.Places / "
            + "ReportControlModel.Place (DESIGN-backend-abstraction §1.3 L6, kb/Work PB2375). Found:\n" + Describe(sites));
    }

    /// <summary>The windowed answers are asked at the three sort sites and nowhere else; the other
    /// <c>.Window(</c> in <c>CodeGen</c> is a <c>GroupArea</c> slice of a VALUE initializer.</summary>
    [Fact]
    public void TheWindowedPlaceAnswers_AreAskedOnlyAtTheThreeSortSites()
    {
        var windows = CodeLines(CodeGenRoot).Where(l => WindowCall.IsMatch(l.Text)).ToList();
        var counts = CodeLines(CodeGenRoot).Where(l => CountCall.IsMatch(l.Text)).ToList();

        var sortWindows = windows.Where(l => Path.GetFileName(l.File) == "SortEmitter.cs").ToList();
        var sortCounts = counts.Where(l => Path.GetFileName(l.File) == "SortEmitter.cs").ToList();
        var elsewhere = windows.Where(l => Path.GetFileName(l.File) is not ("SortEmitter.cs" or "GroupValueSlicer.cs"))
            .Concat(counts.Where(l => Path.GetFileName(l.File) != "SortEmitter.cs")).ToList();
        var slices = windows.Where(l => Path.GetFileName(l.File) == "GroupValueSlicer.cs")
            .Where(l => !Regex.IsMatch(l.Text, @"\b(slice|area)\.Window\(")).ToList();

        Assert.True(elsewhere.Count == 0 && slices.Count == 0,
            "a PlaceTable window / occurrence count is asked outside the Format-2 shared-storage table SORT. Bind the place "
            + "(the SortBinder builds it over its StatementLocal positions, kb/Work PB4321 pattern) instead of building it at "
            + "emit time. Found:\n" + Describe(elsewhere.Concat(slices)));
        Assert.True(sortWindows.Count == 2 && sortCounts.Count == 1,
            "SortEmitter.cs asks exactly two PlaceTable.Window and one PlaceTable.CurrentOccurrenceCount (the three "
            + "documented remainders, DESIGN-backend-abstraction §1.3 L6). When one is bound, lower the count here; a new "
            + "one is refused. Found windows:\n" + Describe(sortWindows) + "\nFound counts:\n" + Describe(sortCounts));
    }

    /// <summary>The table is filled by exactly one pass: <c>PlaceTable.Build</c> is called only from
    /// <c>PlaceTablePass</c>.</summary>
    [Fact]
    public void ThePlaceTable_IsBuiltOnlyByThePlaceTablePass()
    {
        var sites = CodeLines(CompilerRoot)
            .Where(l => l.Text.Contains("PlaceTable.Build(", StringComparison.Ordinal))
            .ToList();
        Assert.True(sites.Count == 1 && Path.GetFileName(sites[0].File) == "PlaceTablePass.cs",
            "a PlaceTable is built only by PlaceTablePass (phase PlacesBuilt, kb/Work PB2375). Found:\n" + Describe(sites));
    }
}
