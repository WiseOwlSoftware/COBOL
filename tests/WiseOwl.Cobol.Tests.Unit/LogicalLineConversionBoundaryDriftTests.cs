// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE §2023:6.5 LOGICAL CONVERSION IS ONE PASS WITH ONE STATE OWNER (kb/Work PB2328; docs/rearchitecture/DESIGN-architecture-review.md
/// §8.3, census R0-0033). <c>LogicalLineConverter</c> is the entry: it reads the physical lines, splits them into reference-format
/// segments and hands each segment to the classifier of its format (<c>FixedFormConverter</c>, <c>FreeFormConverter</c>), and
/// each classifier feeds the one <c>LogicalLineBuilder</c>, which owns the resultant lines, the latest logical line and the
/// literal state, and is the only place the program text is scanned. The types cannot say that a second entry, a second
/// state owner or a second scan does not grow back beside them, so this test says it.
/// </summary>
public sealed class LogicalLineConversionBoundaryDriftTests
{
    private static readonly string Preprocessor = TestRepo.Src("WiseOwl.Cobol.Frontend", "Preprocessor");

    /// <summary>Every production source file of the front end, its comments removed, by its file name.</summary>
    private static IEnumerable<(string Name, string Code)> FrontendCode() =>
        Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Frontend"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(f => (Path.GetFileName(f), string.Join('\n', File.ReadLines(f).Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal)))));

    private static List<string> FilesMatching(string pattern) =>
        [.. FrontendCode().Where(s => Regex.IsMatch(s.Code, pattern)).Select(s => s.Name).Order(StringComparer.Ordinal)];

    [Fact]
    public void TheConverter_IsTheOnlyPlaceAClassifierIsMade()
    {
        Assert.Equal(["LogicalLineConverter.cs"], FilesMatching(@"new FixedFormConverter\("));
        Assert.Equal(["LogicalLineConverter.cs"], FilesMatching(@"new FreeFormConverter\("));
    }

    [Fact] // the classifiers each own one builder; the converter makes one only to decide a carried comment-entry candidate
    public void TheBuilder_IsMadeByTheClassifiersAndTheCandidateConversionOnly()
    {
        Assert.Equal(["FixedFormConverter.cs", "FreeFormConverter.cs", "LogicalLineConverter.cs"],
            FilesMatching(@"new(?: LogicalLineBuilder)?\(file, gates, (?:fixedForm: (?:true|false)|isFixed), kinds"));
    }

    [Fact]
    public void TheProgramTextScan_IsDeclaredInTheBuilderOnly()
    {
        Assert.Equal(["LogicalLineBuilder.cs"], FilesMatching(@"static ScanStop ScanProgramText\("));
        Assert.Equal(["LogicalLineBuilder.cs"], FilesMatching(@"record struct LiteralState\("));
    }

    [Fact] // the extract left no forwarder behind (CLAUDE.md rule 4)
    public void NoReferenceFormatProcessorRemains()
    {
        Assert.Empty(FilesMatching(@"\bReferenceFormatProcessor\b"));
        Assert.Empty(Directory.EnumerateFiles(Preprocessor, "ReferenceFormatProcessor*.cs"));
    }
}
