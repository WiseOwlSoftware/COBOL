// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Runtime.Collation;
using WiseOwl.Cobol.Runtime.Collation.Cldr;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVERY UNIT-TEST CLASS THAT READS THE SITE-DIRECTORY ENVIRONMENT VARIABLES NAMES <c>[Collection("process-globals")]</c>
/// (kb/Work PB3947). <c>COBOL_CLDR_DIR</c> and <c>COBOL_COLLATION_DIR</c> are process-wide and every locale resolution
/// reads them; <c>LocaleCollationFailureTests</c> sets them to a temporary site directory, so a class that resolves a
/// locale in parallel with it (<c>CldrLocaleLoaderTests</c> was one) can read the other's directory: a race by
/// construction that no local run reproduces. xUnit runs the classes of one collection one at a time, so the rule is
/// the collection, not a class's own care.
/// </summary>
/// <remarks>The readers are found by their entry points, not listed: any test source that names the two variables
/// (their constants, read from the runtime) or calls a member of the locale-resolution types. A new reader joins the
/// rule by being written; one that does not name the collection fails here.</remarks>
[Trait("Sweep", "tree")]
public sealed class ProcessGlobalCollectionDriftTests
{
    private static readonly string[] SiteVariables = [CldrLocaleLoader.EnvDirectory, TailoringRules.EnvDirectory];

    private static readonly Regex Reader = new(
        @"\b(CldrLocaleLoader|CldrTailoringBuilder|TailoringRules|LocaleManager|CollationEngine)\.\w+|\b(" + string.Join('|', SiteVariables) + @")\b",
        RegexOptions.Compiled);

    private static readonly Regex Collection = new(@"\[Collection\(""process-globals""\)\]", RegexOptions.Compiled);

    private static IEnumerable<string> UnitTestSources() =>
        Directory.EnumerateFiles(TestRepo.Tests("WiseOwl.Cobol.Tests.Unit"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(p => p is "obj" or "bin"))
            .Where(f => Path.GetFileName(f) != nameof(ProcessGlobalCollectionDriftTests) + ".cs");

    [Fact]
    public void TheScan_ReachesTheKnownReadersAndTheWriter()
    {
        string[] readers = [.. UnitTestSources().Where(f => Reader.IsMatch(File.ReadAllText(f))).Select(f => Path.GetFileName(f))];
        Assert.Contains("LocaleCollationFailureTests.cs", readers);   // the writer
        Assert.Contains("CldrLocaleLoaderTests.cs", readers);         // the reader this note found
        Assert.Contains("CldrCollationSettingsTests.cs", readers);
    }

    [Fact]
    public void EveryReaderOfTheSiteDirectoryVariables_NamesTheProcessGlobalsCollection()
    {
        string[] outside =
        [
            .. UnitTestSources()
                .Where(f => { string text = File.ReadAllText(f); return Reader.IsMatch(text) && !Collection.IsMatch(text); })
                .Select(f => Path.GetRelativePath(TestRepo.Root, f).Replace('\\', '/'))
                .Order(StringComparer.Ordinal),
        ];
        Assert.True(outside.Length == 0,
            "these test classes resolve locales (read COBOL_CLDR_DIR / COBOL_COLLATION_DIR) outside [Collection(\"process-globals\")] "
            + "and can race LocaleCollationFailureTests' temporary site directory (kb/Work PB3947): " + string.Join(", ", outside));
    }
}
