// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;
using CatalogDescriptor = WiseOwl.Cobol.Editions.Diagnostics.DiagnosticDescriptor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// ⛔ EVERY EDITION FLAG CARRIES ITS FLAG, BY CONSTRUCTION (DESIGN-listing.md section 6.11; kb/Work PB5947). The listing's table of
/// flagged constructs by edition reads the flag each diagnostic carries (<see cref="EditionFlag"/>), never its code or message, so the
/// table is complete exactly when every diagnostic that is an edition flag carries one. Three facts hold that:
/// <list type="number">
/// <item>every construct registry row, at every edition and on both axes, reported through the one funnel
/// (<see cref="ConstructRegistry.Check"/> over <see cref="ConstructRegistry.VerdictAt"/>), carries the flag its row's data names:
/// obsolete as of its <c>ObsoleteIn</c>, removed in its <c>RemovedIn</c>, and none when not yet introduced; through the binder's sink
/// (<c>EditionContext</c>) the record keeps it;</item>
/// <item>every accepted, distinguishable nonstandard extension flagged under <c>--flag-extensions</c> carries the extension flag of its
/// row at the edition compiled to, and none is recorded without the option;</item>
/// <item>no source of the compiler but the funnels names a flag's code (COBOL0902, COBOL0903, COBOL2894, and the code each
/// registry row with a removal edition pins, read from the rows, by literal or by its constant or descriptor), so no stage can report
/// a flag the funnels did not make; the scan is shown able to fail on planted text. (Listing step pb5947's review lead R1-2, fixed by
/// kb/Work PB5951's lead sweep: the scan knew the three band codes only, and the MERGE-in-a-SORT-procedure removal, which its row
/// pins as COBOL1572, was reported by hand with no flag.)</item>
/// </list>
/// A row added to either register is checked with no change here. Expected values are the registers' own rows (the inputs).
/// </summary>
[Trait("Sweep", "tree")]
public sealed class EditionFlagDriftTests
{
    /// <summary>A sink that keeps what it is given.</summary>
    private sealed class Captured : IDiagnosticSink
    {
        public List<EditionDiagnostic> Reports { get; } = [];

        public void Report(in EditionDiagnostic diagnostic) => Reports.Add(diagnostic);
    }

    [Fact]
    public void EveryRegistryVerdict_CarriesTheFlagItsRowNames()
    {
        int flagged = 0;
        foreach (var row in ConstructRegistry.Entries)
            foreach (int year in EditionInfo.All.ToArray())
                foreach (bool permissive in new[] { false, true })
                {
                    var edition = EditionInfo.Of(year, permissive);
                    var sink = new Captured();
                    ConstructRegistry.Check(edition, sink, row.Id, "here");
                    var availability = row.StatusAt(year);
                    EditionFlag? expected = availability switch
                    {
                        ConstructAvailability.Obsolete => new(EditionFlagKind.Obsolete, row.Id, row.Display, row.ObsoleteIn!.Value),
                        ConstructAvailability.Removed => new(EditionFlagKind.Removed, row.Id, row.Display, row.RemovedIn!.Value),
                        _ => null,
                    };
                    if (availability == ConstructAvailability.Available)
                    {
                        Assert.Empty(sink.Reports);
                        continue;
                    }
                    var report = Assert.Single(sink.Reports);
                    Assert.True(expected == report.Flag, $"{row.Id} at {year}{(permissive ? " permissive" : "")}: flag {report.Flag}, expected {expected}");
                    if (expected is null) continue;
                    flagged++;
                    // The binder's sink records the same flag on the diagnostic it records.
                    var context = new EditionContext(year, permissive);
                    ConstructRegistry.Check(context.Edition, context, row.Id, "here");
                    Assert.Equal(expected, Assert.Single(context.Records).Flag);
                }
        Assert.True(flagged > 0, "no registry row is obsolete or removed at any edition: the test checked nothing");
    }

    [Fact]
    public void EveryFlaggedExtension_CarriesTheExtensionFlagOfItsRow()
    {
        var flaggable = NonstandardExtensionRegister.Entries.Where(r => r is { Support: ExtensionSupport.Accepted, Distinguishable: true }).ToList();
        Assert.NotEmpty(flaggable);
        foreach (var row in flaggable)
            foreach (int year in EditionInfo.All.ToArray())
            {
                var context = new EditionContext(year) { FlagExtensions = true };
                context.Extension(row, "the use");
                var record = Assert.Single(context.Records);
                Assert.Equal(new EditionFlag(EditionFlagKind.Extension, row.Id, row.Display, year), record.Flag);
                var quiet = new EditionContext(year);
                quiet.Extension(row, "the use");
                Assert.Empty(quiet.Records);
            }
    }

    /// <summary>The funnels, each the one place its code is named: the codes' declarations and the descriptors, the registry's verdict
    /// and its rows, and the binder's extension seam.</summary>
    private static readonly IReadOnlyDictionary<string, string> Funnels = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["WiseOwl.Cobol.Editions/EditionCodes.cs"] = "declares COBOL0902 and COBOL0903",
        ["WiseOwl.Cobol.Editions/Diagnostics/DiagnosticCatalog.cs"] = "declares the descriptors of the three codes",
        ["WiseOwl.Cobol.Editions/ConstructRegistry.cs"] = "VerdictAt: the code and the flag of an obsolete or removed construct",
        ["WiseOwl.Cobol.Editions/ConstructRegistry.g.cs"] = "the registry's rows, each pinning its code (data VerdictAt reads)",
        ["WiseOwl.Cobol.Compiler/Binding/EditionContext.cs"] = "Extension: the code and the flag of a nonstandard extension",
    };

    [Fact]
    public void NoSourceButTheFunnels_NamesAFlagCode()
    {
        string root = TestRepo.At("src");
        var findings = new List<string>();
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            string rel = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (rel.Contains("/obj/", StringComparison.Ordinal) || rel.Contains("/bin/", StringComparison.Ordinal) || Funnels.ContainsKey(rel))
                continue;
            findings.AddRange(FlagCodeNames(rel, File.ReadAllText(file)));
        }
        Assert.True(findings.Count == 0, "a source outside the funnels names an edition flag's code, so it can report a flag that "
            + "carries no EditionFlag (DESIGN-listing.md section 6.11): report it through ConstructRegistry.VerdictAt or "
            + "EditionContext.Extension\n" + string.Join("\n", findings));
        foreach (string funnel in Funnels.Keys) Assert.True(File.Exists(Path.Combine(root, funnel)), $"the funnel {funnel} does not exist");
    }

    /// <summary>The scan finds each form a flag's code can be named in, and nothing in a comment or a documentation comment: the check
    /// can fail.</summary>
    [Fact]
    public void TheScan_FindsEveryFormOfAFlagCode()
    {
        const string planted = """
            /// <summary>Reports COBOL0903 (<see cref="EditionCodes.ObsoleteFlag"/>).</summary>
            class Planted
            {
                // COBOL0902 in a comment is no report.
                void M(DiagnosticBag bag, EditionContext edition)
                {
                    bag.ReportWarning(EditionCodes.ObsoleteFlag, "m", default, default);
                    bag.ReportError(WiseOwl.Cobol.Editions.EditionCodes.RemovedConstruct, "m", default, default);
                    bag.ReportError("COBOL0902", "m", default, default);
                    edition.Warning(DiagnosticCatalog.NonstandardExtensionUsed, "m");
                    edition.Warning(DiagnosticCatalog.EditionObsoleteFlag.Code, "m");
                    edition.Error(DiagnosticCatalog.EditionRemovedConstruct.Code, "m");
                    edition.Warning("COBOL2894", "m");
                    edition.Warning("COBOL0903", "m");
                    edition.Warning("COBOL0901", "m");
                    edition.Error(DiagnosticCatalog.MergeInSortMergeProc.Code, "m");
                    edition.Error("COBOL1572", "m");
                }
            }
            """;
        Assert.Equal(
        [
            "Planted.cs:7: EditionCodes.ObsoleteFlag",
            "Planted.cs:8: EditionCodes.RemovedConstruct",
            "Planted.cs:9: \"COBOL0902\"",
            "Planted.cs:10: NonstandardExtensionUsed",
            "Planted.cs:11: EditionObsoleteFlag",
            "Planted.cs:12: EditionRemovedConstruct",
            "Planted.cs:13: \"COBOL2894\"",
            "Planted.cs:14: \"COBOL0903\"",
            "Planted.cs:16: MergeInSortMergeProc",
            "Planted.cs:17: \"COBOL1572\"",
        ], FlagCodeNames("Planted.cs", planted));
    }

    /// <summary>The code the reserved-word list reports a removed user word under (§2023:8.9; <c>ReservedWordSet.UserWordVerdictAt</c>),
    /// which the registry's user-word rows pin too: its reports are the list's, for every reserved word, not one row's, and carry no
    /// flag yet (kb/Work PB5947's dated file <c>2026-11-19-reserved-word-removal-flag</c>), so the scan does not take it from the rows.</summary>
    private const string ReservedWordCode = EditionCodes.ReservedWord;

    /// <summary>The flags' codes: the three bands, and each code a registry row with a removal edition pins (read from the rows, so a
    /// row added with a code of its own is scanned with no change here).</summary>
    private static readonly HashSet<string> Codes = new(
        new[] { "COBOL0902", "COBOL0903", "COBOL2894" }.Concat(ConstructRegistry.Entries
            .Where(row => row.RemovedIn is not null && row.DiagnosticCode != ReservedWordCode).Select(row => row.DiagnosticCode)),
        StringComparer.Ordinal);

    /// <summary>The descriptors of those codes, read from <see cref="DiagnosticCatalog"/>.</summary>
    private static readonly HashSet<string> Descriptors = new(typeof(DiagnosticCatalog)
        .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
        .Where(f => f.FieldType == typeof(CatalogDescriptor)
            && Codes.Contains(((CatalogDescriptor)f.GetValue(null)!).Code))
        .Select(f => f.Name), StringComparer.Ordinal);

    /// <summary>The scan's sets hold what the registry and the catalog say: the pinned removal codes beside the bands (none of them the
    /// introduction band, which flags nothing), and the descriptors of each.</summary>
    [Fact]
    public void TheScannedCodes_AreTheBandsAndEveryRowsPinnedRemovalCode()
    {
        Assert.Contains("COBOL1572", Codes);   // merge-in-sort-merge-proc-removed-2023
        Assert.DoesNotContain(EditionCodes.Introduction, Codes);
        Assert.DoesNotContain(ReservedWordCode, Codes);
        Assert.Contains("MergeInSortMergeProc", Descriptors);
        Assert.Contains("EditionRemovedConstruct", Descriptors);
        Assert.Contains("EditionObsoleteFlag", Descriptors);
        Assert.Contains("NonstandardExtensionUsed", Descriptors);
    }
    private static readonly HashSet<string> CodeConstants = new(StringComparer.Ordinal) { "RemovedConstruct", "ObsoleteFlag" };

    /// <summary>Each place <paramref name="text"/> names a flag's code in code (never in a comment): a string literal of the code, a
    /// descriptor of it, or its <c>EditionCodes</c> constant; as <c>file:line: what</c>.</summary>
    private static IEnumerable<string> FlagCodeNames(string file, string text)
    {
        var tree = CSharpSyntaxTree.ParseText(text);
        foreach (var node in tree.GetRoot().DescendantNodes())
        {
            string? what = node switch
            {
                LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression)
                    && Codes.Contains(literal.Token.ValueText) => literal.ToString(),
                IdentifierNameSyntax name when Descriptors.Contains(name.Identifier.ValueText) => name.Identifier.ValueText,
                IdentifierNameSyntax name when CodeConstants.Contains(name.Identifier.ValueText)
                    && name.Parent is MemberAccessExpressionSyntax access && access.Name == name
                    && access.Expression.ToString().EndsWith("EditionCodes", StringComparison.Ordinal) => "EditionCodes." + name.Identifier.ValueText,
                _ => null,
            };
            if (what is not null) yield return $"{file}:{node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}: {what}";
        }
    }
}
