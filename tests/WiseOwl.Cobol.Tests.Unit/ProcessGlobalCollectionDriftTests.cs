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

    /// <summary>The types that resolve a locale through the site directories (they read the two variables, directly or
    /// through <c>CollationEngine.ForLocale</c>). A test source calling a member of one is a reader.</summary>
    private static readonly string[] ResolverTypes =
    [
        "CldrLocaleLoader", "CldrTailoringBuilder", "TailoringRules", "LocaleManager", "CollationEngine",
        "LocaleCollation", "LocaleConfig", "CollationRuntime", "LocaleState", "LocaleInfo",
    ];

    /// <summary>The source types that name a resolver but resolve nothing themselves (tables, keys, facts, the config
    /// listing, the binder). <see cref="EveryRuntimeTypeNamingAResolver_IsClassified"/> requires every other one to be a resolver.</summary>
    private static readonly string[] NonResolvingTypes =
    [
        "CldrLocaleData", "CobolIntrinsics", "CodePointKey", "CollationElement", "CollationTable", "Collator",
        "LocaleCollationException", "LocaleFacts", "RuntimeConfig", "SpecialNamesBinder", "UnicodeNormalizer",
    ];

    private static readonly Regex Reader = new(
        @"\b(" + string.Join('|', ResolverTypes) + @")\.\w+|\bnew (LocaleCollation|LocaleConfig)\b|\b(" + string.Join('|', SiteVariables) + @")\b",
        RegexOptions.Compiled);

    private static readonly Regex NamesResolver = new(@"\b(" + string.Join('|', ResolverTypes[..5]) + @")\b", RegexOptions.Compiled);

    private static readonly TreeScope ProductSources = new(Under: ["src/"], Kinds: [".cs"]);

    /// <summary>A source file naming one of the five root resolvers declares a type that either resolves locales (and is
    /// then a resolver) or does not; a new such type must be classified, so the next reader cannot slip past the scan.</summary>
    [Fact]
    public void EveryRuntimeTypeNamingAResolver_IsClassified()
    {
        string[] known = [.. ResolverTypes, .. NonResolvingTypes];
        string[] unclassified =
        [
            .. TrackedTree.Files(ProductSources)
                .Where(rel => NamesResolver.IsMatch(File.ReadAllText(TestRepo.Within("src/", rel))))
                .Select(rel => Path.GetFileName(rel).Split('.')[0])
                .Where(stem => !known.Contains(stem))
                .Distinct().Order(StringComparer.Ordinal),
        ];
        Assert.True(unclassified.Length == 0,
            "these source types name a locale resolver but are in neither ResolverTypes nor NonResolvingTypes: " + string.Join(", ", unclassified));
    }

    private static readonly Regex Collection = new(@"\[Collection\(""process-globals""\)\]", RegexOptions.Compiled);

    /// <summary>The Unit assembly's committed sources; this file names the entry points it scans for, so it sets itself aside.</summary>
    private static readonly TreeScope UnitSources = new(Under: ["tests/WiseOwl.Cobol.Tests.Unit/"],
        Except: ["tests/WiseOwl.Cobol.Tests.Unit/ProcessGlobalCollectionDriftTests.cs"], Kinds: [".cs"]);

    private static IEnumerable<string> UnitTestSources() => TrackedTree.Files(UnitSources).Select(rel => TestRepo.Within("tests/WiseOwl.Cobol.Tests.Unit/", rel));

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
