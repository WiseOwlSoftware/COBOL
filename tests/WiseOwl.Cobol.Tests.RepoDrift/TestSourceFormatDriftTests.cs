// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.RepoDrift;

/// <summary>
/// ⛔ NO TEST LETS THE DETECTOR CHOOSE THE REFERENCE FORMAT OF THE PROGRAM IT COMPILES (kb/Work PB1757, R57, PB6033).
///
/// <para><b>What happened.</b> Every test harness compiled with <c>InitialReferenceFormat.Auto</c>, whose heuristic read
/// 2,905 of the 3,576 goldens of the old corpus as FREE form (text past column 72 vetoes fixed form), so 181 positive
/// goldens compiled only because of it and no golden it read as free exercised a fixed-form rule. The suite cases now state
/// their format in their headers; this test holds the rest: a source a test compiles is read in a STATED format
/// (<c>tests/_shared/TestSourceFormat.cs</c> gives the three), and <c>InitialReferenceFormat.Auto</c> appears in a test only
/// where the test is about the detector or the option that selects it (<see cref="Detector"/>).</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class TestSourceFormatDriftTests
{
    private static readonly TreeScope CSharpTests = new(Under: ["tests/"], Except: ["tests/_shared/TestSourceFormat.cs", "tests/WiseOwl.Cobol.Tests.RepoDrift/TestSourceFormatDriftTests.cs"], Kinds: [".cs"]);

    /// <summary>The files that test the detector or its option, each with its reason.</summary>
    private static readonly (string File, string Why)[] Detector =
    [
        ("tests/WiseOwl.Cobol.Tests.Unit/ExceptionPerformSourceFormatScopeTests.cs",
         "AutoDetectedFixedForm_IsRestoredAtEndPerform: the implicit POP restores the format the detector chose"),
        ("tests/WiseOwl.Cobol.Tests.Unit/CliParserTests.cs", "--source-format auto parses to InitialReferenceFormat.Auto"),
    ];

    /// <summary>The member access <c>InitialReferenceFormat.Auto</c>, qualified or not, outside a comment line.</summary>
    private static readonly Regex AutoUse = new(@"^(?!\s*///?)(?<line>.*\bInitialReferenceFormat\s*\.\s*Auto\b.*)$",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static IEnumerable<string> Uses(string path, string text) =>
        AutoUse.Matches(text).Select(m => $"{path}: {m.Groups["line"].Value.Trim()}");

    [Fact]
    public void NoTestCompilesInTheDetectedFormat()
    {
        var files = TrackedTree.Files(CSharpTests);
        Assert.True(files.Count > 500, $"the test tree listing found only {files.Count} C# files; it is broken");
        var found = files.Where(f => !Detector.Any(d => d.File == f))
            .SelectMany(f => Uses(f, File.ReadAllText(Path.Combine(TestRepo.Root, f))))
            .ToList();
        Assert.True(found.Count == 0, "a test compiles in the format the detector chooses; state it (TestSourceFormat.Inline for a "
            + "test's own free-form text, InitialReferenceFormat.Fixed for fixed-form text, a NIST program or a sample; "
            + "kb/Work PB1757):\n  " + string.Join("\n  ", found));
    }

    /// <summary>Every detector file still uses <c>Auto</c>, so a stale entry cannot hide a new offender.</summary>
    [Fact]
    public void EveryDetectorFile_StillTestsTheDetector()
    {
        var stale = Detector.Where(d => !File.Exists(Path.Combine(TestRepo.Root, d.File))
                                        || !Uses(d.File, File.ReadAllText(Path.Combine(TestRepo.Root, d.File))).Any())
            .Select(d => d.File).ToList();
        Assert.True(stale.Count == 0, "a detector entry names a file that no longer uses Auto; remove it:\n  " + string.Join("\n  ", stale));
    }

    /// <summary>The detector looks: each spelling of a use trips it, and a comment or another member trips nothing.</summary>
    [Fact]
    public void TheDetector_FlagsTheShapesItExistsFor()
    {
        const string offenders = """
            var a = new Options(src, dll, SourceFormat: InitialReferenceFormat.Auto);
            var b = new Options(src, dll, SourceFormat: WiseOwl.Cobol.Frontend.Preprocessor.InitialReferenceFormat.Auto);
            Parse(InitialReferenceFormat . Auto, text);
            """;
        Assert.Equal(3, Uses("fixture", offenders).Count());
        const string others = """
            /// <summary>Not <see cref="InitialReferenceFormat.Auto"/>.</summary>
            // InitialReferenceFormat.Auto is the detector
            var a = new Options(src, dll, SourceFormat: InitialReferenceFormat.Fixed);
            var b = new Options(src, dll, SourceFormat: TestSourceFormat.Inline);
            var c = InitialReferenceFormat.AutoDetectLater;
            """;
        Assert.Empty(Uses("fixture", others));
    }
}
