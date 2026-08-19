// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CobolNet.Runtime.Collation;
using CobolNet.Runtime.Unicode;
using CobolNet.Runtime.Unicode.Segmentation;
using CobolNet.Tests.Shared;
using Xunit;
using Xunit.Abstractions;

namespace CobolNet.Tests.Unit.Unicode;

/// <summary>
/// The grapheme cluster segmentation (Runtime/Unicode/Segmentation/, kb/Work PB104): the derived property table
/// loads and matches its manifest and the pinned data; the UAX #29 rules hold on hand-picked cases (combining marks,
/// Hangul jamo, CR LF, emoji modifier and ZWJ sequences, regional-indicator pairs, Prepend, Indic conjuncts,
/// unpaired surrogates); EVERY line of the Unicode <c>GraphemeBreakTest.txt</c> of the same version passes; the
/// enumerator, Count, Split, Truncate and IsBoundary agree; and the interplay with normalization and collation is what
/// the design claims (clusters survive NFC/NFD, a cluster-safe truncation never changes how a text's kept prefix
/// collates, and keys are NOT built per cluster — a contraction may cross a cluster boundary).
/// </summary>
public sealed class GraphemeBreakerTests(ITestOutputHelper output)
{
    private static readonly string DataDir = Path.Combine(TestRepo.Root, "src", "Cobol.Net.Runtime", "Unicode", "Segmentation", "Data");
    private static readonly string UnicodeDir = Path.Combine(TestRepo.Root, "data", "unicode");
    private static readonly string TestFile = Path.Combine(TestRepo.Root, "tests", "Cobol.Net.Tests.Unit", "TestData", "segmentation", "GraphemeBreakTest.txt");

    private static string[] Clusters(string s) => GraphemeBreaker.Split(s);

    // ---- the table -------------------------------------------------------------------------------------------------

    [Fact]
    public void Table_MatchesTheManifest_AndThePinnedInputs()
    {
        using var m = JsonDocument.Parse(File.ReadAllText(Path.Combine(DataDir, "grapheme-break.manifest.json")));
        Assert.Equal(m.RootElement.GetProperty("unicodeVersion").GetString(), GraphemeBreaker.UnicodeVersion);
        Assert.Equal(m.RootElement.GetProperty("stats").GetProperty("ranges").GetInt32(), GraphemeBreaker.RangeCount);
        foreach (var input in m.RootElement.GetProperty("inputs").EnumerateObject())
        {
            string path = Path.Combine(UnicodeDir, input.Name);
            Assert.True(File.Exists(path), $"manifest input {input.Name} missing from data/unicode/");
            Assert.Equal(input.Value.GetString(), Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path))));
        }
        byte[] embedded;
        using (var s = typeof(GraphemeBreaker).Assembly.GetManifestResourceStream(GraphemeBreaker.ResourceName)!)
        using (var ms = new MemoryStream()) { s.CopyTo(ms); embedded = ms.ToArray(); }
        Assert.Equal(m.RootElement.GetProperty("outputSha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(embedded)));
        Assert.Equal(embedded, File.ReadAllBytes(Path.Combine(DataDir, "grapheme-break.bin")));
        var (version, ranges) = GraphemeBreaker.Inspect(embedded);
        Assert.Equal(GraphemeBreaker.UnicodeVersion, version);
        Assert.Equal(GraphemeBreaker.RangeCount, ranges);
        // The segmentation data and the collation data are the same Unicode version — one Unicode, one product.
        Assert.Equal(CollationTable.Root.UcaVersion, GraphemeBreaker.UnicodeVersion);
    }

    [Fact]
    public void Properties_AreTheUcdValues()
    {
        Assert.Equal(GraphemeBreakProperty.CR, GraphemeBreaker.GetBreakProperty('\r'));
        Assert.Equal(GraphemeBreakProperty.LF, GraphemeBreaker.GetBreakProperty('\n'));
        Assert.Equal(GraphemeBreakProperty.Control, GraphemeBreaker.GetBreakProperty('\t'));
        Assert.Equal(GraphemeBreakProperty.Other, GraphemeBreaker.GetBreakProperty('a'));
        Assert.Equal(GraphemeBreakProperty.Extend, GraphemeBreaker.GetBreakProperty(0x0301));      // COMBINING ACUTE ACCENT
        Assert.Equal(GraphemeBreakProperty.ZWJ, GraphemeBreaker.GetBreakProperty(0x200D));
        Assert.Equal(GraphemeBreakProperty.RegionalIndicator, GraphemeBreaker.GetBreakProperty(0x1F1E6));
        Assert.Equal(GraphemeBreakProperty.Prepend, GraphemeBreaker.GetBreakProperty(0x0600));     // ARABIC NUMBER SIGN
        Assert.Equal(GraphemeBreakProperty.SpacingMark, GraphemeBreaker.GetBreakProperty(0x0903)); // DEVANAGARI SIGN VISARGA
        Assert.Equal(GraphemeBreakProperty.L, GraphemeBreaker.GetBreakProperty(0x1100));
        Assert.Equal(GraphemeBreakProperty.V, GraphemeBreaker.GetBreakProperty(0x1161));
        Assert.Equal(GraphemeBreakProperty.T, GraphemeBreaker.GetBreakProperty(0x11A8));
        Assert.Equal(GraphemeBreakProperty.LV, GraphemeBreaker.GetBreakProperty(0xAC00));
        Assert.Equal(GraphemeBreakProperty.LVT, GraphemeBreaker.GetBreakProperty(0xAC01));
        Assert.True(GraphemeBreaker.IsExtendedPictographic(0x1F600));
        Assert.True(GraphemeBreaker.IsExtendedPictographic(0x00A9));                                // ©
        Assert.False(GraphemeBreaker.IsExtendedPictographic('a'));
        Assert.Equal(IndicConjunctBreak.Consonant, GraphemeBreaker.GetIndicConjunctBreak(0x0915)); // DEVANAGARI KA
        Assert.Equal(IndicConjunctBreak.Linker, GraphemeBreaker.GetIndicConjunctBreak(0x094D));    // VIRAMA
        Assert.Equal(IndicConjunctBreak.Extend, GraphemeBreaker.GetIndicConjunctBreak(0x200D));    // ZWJ
        Assert.Equal(IndicConjunctBreak.Extend, GraphemeBreaker.GetIndicConjunctBreak(0x093C));    // NUKTA
        Assert.Equal(IndicConjunctBreak.None, GraphemeBreaker.GetIndicConjunctBreak('a'));
        Assert.Equal(GraphemeBreakProperty.Other, GraphemeBreaker.GetBreakProperty(0x10FFFF));
        Assert.Equal(GraphemeBreakProperty.Other, GraphemeBreaker.GetBreakProperty(0xD800));       // an unpaired surrogate code unit
    }

    // ---- the rules ---------------------------------------------------------------------------------------------------

    [Fact]
    public void Rules_HandPickedCases()
    {
        Assert.Equal(["a", "b", "c"], Clusters("abc"));
        Assert.Equal(["e\U00000301"], Clusters("e\U00000301"));                                   // base + combining mark
        Assert.Equal(["e\U00000301\U00000302", "x"], Clusters("e\U00000301\U00000302x"));           // several marks
        Assert.Equal(["\r\n", "a"], Clusters("\r\na"));                                              // CR LF is one cluster (GB3)
        Assert.Equal(["\n", "\r"], Clusters("\n\r"));                                                // LF CR are two (GB4/GB5)
        Assert.Equal(["a", "\t", "b"], Clusters("a\tb"));                                            // a control breaks both sides
        Assert.Equal(["각"], Clusters("각"));                        // L V T jamo = one syllable (GB6–GB8)
        Assert.Equal(["각"], Clusters("각"));                                    // LV + T
        Assert.Equal(["각", "가"], Clusters("각가"));                                // LVT then LV: two syllables
        Assert.Equal(["\U0001F44D\U0001F3FD"], Clusters("\U0001F44D\U0001F3FD"));                    // thumbs up + skin tone (Extend)
        Assert.Equal(["\U0001F468‍\U0001F469‍\U0001F467"], Clusters("\U0001F468‍\U0001F469‍\U0001F467"));   // family ZWJ sequence (GB11)
        Assert.Equal(["\U0001F468‍", "a"], Clusters("\U0001F468‍a"));                       // ZWJ not followed by a pictograph: the ZWJ stays, then break
        Assert.Equal(["\U0001F1FA\U0001F1F8", "\U0001F1EC\U0001F1E7"], Clusters("\U0001F1FA\U0001F1F8\U0001F1EC\U0001F1E7"));   // two flags (GB12)
        Assert.Equal(["\U0001F1FA\U0001F1F8", "\U0001F1EC"], Clusters("\U0001F1FA\U0001F1F8\U0001F1EC"));                          // three RIs: pair + one
        Assert.Equal(["؀١"], Clusters("؀١"));                                    // Prepend + digit (GB9b)
        Assert.Equal(["क्त"], Clusters("क्त"));                        // Devanagari conjunct KA + VIRAMA + TA (GB9c)
        Assert.Equal(["क्‍त"], Clusters("क्‍त"));            // with a ZWJ inside
        Assert.Equal(["क़्त"], Clusters("क़्त"));            // with a nukta before the virama
        Assert.Equal(["क", "त"], Clusters("कत"));                                // no linker: two clusters
        Assert.Equal(["कः", "त"], Clusters("कःत"));                    // SpacingMark joins (GB9a)
        Assert.Equal(["\U0001F600", "a"], Clusters("\U0001F600a"));                                  // a supplementary character is one cluster
        Assert.Equal(["\uD800", "a"], Clusters("\uD800a"));                                          // an unpaired surrogate is its own cluster
        Assert.Equal(["a", "\uDC00\U00000301"], Clusters("a\uDC00\U00000301"));                      // …and takes a following mark like any base
        Assert.Empty(Clusters(""));
    }

    /// <summary>Every line of the Unicode GraphemeBreakTest.txt (the official test suite of UAX #29, same Unicode
    /// version as the table): ÷ marks a boundary, × a non-boundary; the segmentation must reproduce all of them.</summary>
    [Fact]
    public void GraphemeBreakTest_EveryLinePasses()
    {
        Assert.True(File.Exists(TestFile), $"missing {TestFile}");
        var lines = File.ReadAllLines(TestFile);
        string? version = lines.FirstOrDefault(l => l.StartsWith("# GraphemeBreakTest-", StringComparison.Ordinal));
        Assert.NotNull(version);
        Assert.Contains(GraphemeBreaker.UnicodeVersion, version);
        int cases = 0, failures = 0;
        var report = new StringBuilder();
        foreach (string raw in lines)
        {
            string line = raw;
            int hash = line.IndexOf('#');
            if (hash >= 0) line = line[..hash];
            line = line.Trim();
            if (line.Length == 0) continue;
            // "÷ 0020 × 0308 ÷ 0020 ÷": tokens alternate between markers and code points.
            var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var text = new StringBuilder();
            var expected = new List<int>();   // boundary indices in the built text
            for (int i = 0; i < tokens.Length; i++)
            {
                string t = tokens[i];
                if (t == "÷") expected.Add(text.Length);
                else if (t == "×") continue;
                else
                {
                    int cp = int.Parse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    if (cp is >= 0xD800 and <= 0xDFFF) text.Append((char)cp); else text.Append(char.ConvertFromUtf32(cp));
                }
            }
            string s = text.ToString();
            var actual = new List<int> { 0 };
            foreach (var c in GraphemeBreaker.Enumerate(s)) actual.Add(c.End);
            cases++;
            if (!actual.SequenceEqual(expected))
            {
                failures++;
                if (failures <= 20) report.AppendLine($"{raw.Trim()}\n   expected {string.Join(",", expected)} got {string.Join(",", actual)}");
            }
        }
        output.WriteLine($"GraphemeBreakTest.txt: {cases} cases, {failures} failures");
        Assert.True(cases > 700, $"only {cases} test cases read");
        Assert.True(failures == 0, $"{failures} of {cases} GraphemeBreakTest cases failed:\n{report}");
    }

    // ---- the API -------------------------------------------------------------------------------------------------

    [Fact]
    public void Enumerator_Count_Split_Truncate_IsBoundary_Agree()
    {
        string s = "e\U00000301\r\n\U0001F468‍\U0001F469각x";
        var clusters = GraphemeBreaker.Enumerate(s).ToArray();
        Assert.Equal(5, clusters.Length);
        Assert.Equal(5, GraphemeBreaker.Count(s));
        Assert.Equal(new GraphemeEnumerator(s).Count, clusters.Length);
        Assert.Equal(GraphemeBreaker.Split(s), clusters.Select(c => c.ToString()).ToArray());
        int pos = 0;
        foreach (var c in clusters)
        {
            Assert.Equal(pos, c.Start);
            Assert.Same(s, c.Source);
            Assert.True(GraphemeBreaker.IsBoundary(s, c.Start));
            for (int i = c.Start + 1; i < c.End; i++) Assert.False(GraphemeBreaker.IsBoundary(s, i), $"no boundary expected inside a cluster at {i}");
            pos = c.End;
        }
        Assert.Equal(s.Length, pos);
        Assert.True(GraphemeBreaker.IsBoundary(s, s.Length));
        Assert.Equal(new[] { 0x65, 0x301 }, clusters[0].CodePoints);
        Assert.Equal(2, clusters[0].CodePointCount);
        Assert.False(clusters[0].IsSingleCodePoint);
        Assert.Equal(0x1F468, clusters[2].FirstCodePoint);
        Assert.Equal(3, clusters[2].CodePointCount);
        Assert.True(clusters[4].IsSingleCodePoint);
        Assert.Equal("e\U00000301\r\n", GraphemeBreaker.Truncate(s, 2));
        Assert.Equal("", GraphemeBreaker.Truncate(s, 0));
        Assert.Same(s, GraphemeBreaker.Truncate(s, 99));
        // A generic IEnumerable<T> consumer sees the same clusters.
        IEnumerable<GraphemeCluster> generic = GraphemeBreaker.Enumerate(s);
        Assert.Equal(clusters.Select(c => c.ToString()), generic.Select(c => c.ToString()));
        Assert.Empty(GraphemeBreaker.Enumerate("").ToArray());
        Assert.Empty(GraphemeBreaker.Enumerate(null!).ToArray());
    }

    // ---- with normalization and collation ----------------------------------------------------------------------------

    /// <summary>Canonical normalization never moves a cluster boundary: NFC and NFD of a text have the same number of
    /// clusters, and cluster i of NFC(text) is canonically equivalent to cluster i of NFD(text).</summary>
    [Fact]
    public void Segmentation_IsStableUnderNormalization()
    {
        string[] corpus = ["café", "cafe\U00000301", "가각", "각", "Å\U0000030A", "e\U00000323\U00000302", "ệ", "\U0001F468‍\U0001F469", "क्त", "a\r\nb", "ǆ", "\uD800x"];
        foreach (string s in corpus)
        {
            string nfd = UnicodeNormalizer.Normalize(s, UnicodeNormalizationForm.NFD);
            string nfc = UnicodeNormalizer.Normalize(s, UnicodeNormalizationForm.NFC);
            var a = Clusters(nfd);
            var b = Clusters(nfc);
            Assert.Equal(b.Length, a.Length);
            for (int i = 0; i < a.Length; i++)
                Assert.Equal(0, CollationEngine.Compare(a[i], b[i]));   // canonically equivalent clusters collate equal
        }
    }

    /// <summary>A cluster-safe truncation keeps a PREFIX of the text in every sense the collation engine sees: the kept
    /// text is a prefix at every weight level (its key's levels are prefixes of the full key's levels), which a code-unit
    /// truncation inside a cluster is not (cutting "é" written as e + acute after the "e" drops the accent).</summary>
    [Fact]
    public void ClusterSafeTruncation_KeepsACollationPrefix()
    {
        string s = "cafe\U00000301s";
        string safe = GraphemeBreaker.Truncate(s, 4);       // "café" (4 clusters)
        string unsafe_ = s[..4];                            // "cafe" — cut inside the 4th cluster
        Assert.Equal("cafe\U00000301", safe);
        var full = CollationKey.Build(s);
        var keptSafe = CollationKey.Build(safe);
        var keptUnsafe = CollationKey.Build(unsafe_);
        Assert.True(full.Secondary.Take(keptSafe.Secondary.Count).SequenceEqual(keptSafe.Secondary));
        Assert.False(full.Secondary.Take(keptUnsafe.Secondary.Count + 1).SequenceEqual(keptUnsafe.Secondary.Append(full.Secondary[keptUnsafe.Secondary.Count])) && keptUnsafe.Secondary.Count == keptSafe.Secondary.Count);
        Assert.True(CollationEngine.Compare(safe, "cafe") > 0);   // the accent survived
        Assert.Equal(0, CollationEngine.Compare(unsafe_, "cafe")); // …and here it did not
    }

    /// <summary>Why the engine builds keys for WHOLE texts and not per cluster: a collation contraction may span a
    /// cluster boundary (Thai SARA E + KO KAI are two clusters but one contraction; Czech "ch" is two clusters and one
    /// letter), and a key is level-major (all primaries, then all secondaries), so concatenated per-cluster keys are
    /// not the text's key. Both facts, pinned.</summary>
    [Fact]
    public void KeysAreNotPerCluster_ContractionsCrossClusterBoundaries()
    {
        // Thai: two clusters, one contraction.
        string thai = "\U00000E40\U00000E01";
        Assert.Equal(2, GraphemeBreaker.Count(thai));
        var whole = CollationKey.Build(thai).Primary.ToArray();
        var perCluster = Clusters(thai).SelectMany(c => CollationKey.Build(c).Primary).ToArray();
        Assert.NotEqual(perCluster, whole);                 // the contraction reorders the two primaries
        // Czech: "ch" is two clusters and, under cs, one primary.
        var cs = CollationEngine.ForLocale("cs");
        Assert.Equal(2, GraphemeBreaker.Count("ch"));
        Assert.Single(cs.GetKey("ch").Primary);
        Assert.Equal(2, Clusters("ch").Sum(c => cs.GetKey(c).Primary.Count));
        // Level-major keys: the concatenation of per-cluster keys interleaves levels; the whole-text key does not.
        var wholeKey = CollationKey.Build("ab").ToByteArray();
        var concat = CollationKey.Build("a").ToByteArray().Concat(CollationKey.Build("b").ToByteArray()).ToArray();
        Assert.NotEqual(concat, wholeKey);
    }
}
