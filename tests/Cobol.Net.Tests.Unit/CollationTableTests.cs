// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Security.Cryptography;
using System.Text.Json;
using CobolNet.Runtime.Collation;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The derived collation TABLE (Runtime/Collation/CollationTable.cs; docs: src/Cobol.Net.Runtime/Collation/README.md):
/// the embedded root table loads, carries the versions its manifest records, covers ASCII and Latin-1 explicitly,
/// computes Hangul and UTS #10 Table 16 implicit weights, and knows its non-starters. The manifest \U000021C4 data drift test
/// keeps the committed table, the committed source data and the generator's record of them in agreement.
/// </summary>
public sealed class CollationTableTests
{
    private static readonly string DataDir = Path.Combine(TestRepo.Root, "src", "Cobol.Net.Runtime", "Collation", "Data");
    private static readonly string UnicodeDir = Path.Combine(TestRepo.Root, "data", "unicode");

    private static JsonDocument Manifest() =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(DataDir, "root-collation.manifest.json")));

    [Fact]
    public void Root_Loads_WithTheManifestsVersionAndCounts()
    {
        var t = CollationTable.Root;
        using var m = Manifest();
        Assert.Equal(m.RootElement.GetProperty("ucaVersion").GetString(), t.UcaVersion);
        Assert.Equal(m.RootElement.GetProperty("primaryShift").GetInt32(), t.PrimaryShift);
        var stats = m.RootElement.GetProperty("stats");
        Assert.Equal(stats.GetProperty("singleMappings").GetInt32(), t.MappingCount);
        Assert.Equal(stats.GetProperty("contractions").GetInt32(), t.ContractionCount);
        Assert.Equal("root", t.Name);
        Assert.Null(t.Tailoring);
    }

    /// <summary>The committed table IS the generator's output over the committed sources: the manifest's input hashes
    /// match data/unicode/*, and its output hash matches the embedded resource. Regenerate + recommit both together.</summary>
    [Fact]
    public void Manifest_InputsAndOutput_MatchTheCommittedFiles()
    {
        using var m = Manifest();
        foreach (var input in m.RootElement.GetProperty("inputs").EnumerateObject())
        {
            string path = Path.Combine(UnicodeDir, input.Name);
            Assert.True(File.Exists(path), $"manifest input {input.Name} missing from data/unicode/");
            Assert.Equal(input.Value.GetString(), Sha256(File.ReadAllBytes(path)));
        }
        byte[] embedded;
        using (var s = typeof(CollationTable).Assembly.GetManifestResourceStream(CollationTable.RootResourceName)!)
        using (var ms = new MemoryStream()) { s.CopyTo(ms); embedded = ms.ToArray(); }
        Assert.Equal(m.RootElement.GetProperty("outputSha256").GetString(), Sha256(embedded));
        Assert.Equal(embedded, File.ReadAllBytes(Path.Combine(DataDir, "root-collation.bin")));
        // And the blob decodes to the same table (the public Decode is what a regenerated file goes through).
        var decoded = CollationTable.Decode(embedded, "probe");
        Assert.Equal(CollationTable.Root.MappingCount, decoded.MappingCount);
        Assert.Equal(CollationTable.Root.UcaVersion, decoded.UcaVersion);
    }

    /// <summary>The generator's implicit-weight ranges are UTS #10 (rev. 53) Table 16, derived from the UCD — every
    /// row kind the table names is present, with the 17.0 siniform bases.</summary>
    [Fact]
    public void Manifest_ImplicitRanges_AreTable16()
    {
        using var m = Manifest();
        var rows = m.RootElement.GetProperty("implicitRanges").EnumerateArray()
            .Select(r => (First: r.GetProperty("first").GetString(), Base: r.GetProperty("base").GetString(), Kind: r.GetProperty("kind").GetString()))
            .ToList();
        Assert.Contains(rows, r => r.First == "U+4E00" && r.Base == "0xFB40" && r.Kind == "han-core");
        Assert.Contains(rows, r => r.First == "U+3400" && r.Base == "0xFB80" && r.Kind == "han-other");
        Assert.Contains(rows, r => r.First == "U+17000" && r.Base == "0xFB00" && r.Kind == "siniform");
        Assert.Contains(rows, r => r.First == "U+18800" && r.Base == "0xFB01" && r.Kind == "siniform");
        Assert.Contains(rows, r => r.First == "U+1B170" && r.Base == "0xFB02" && r.Kind == "siniform");
        Assert.Contains(rows, r => r.First == "U+18B00" && r.Base == "0xFB03" && r.Kind == "siniform");
    }

    /// <summary>ASCII + Latin-1 are fully and EXPLICITLY tabulated (no code point below U+0100 falls to an implicit weight).</summary>
    [Fact]
    public void AsciiAndLatin1_AreExplicit()
    {
        var t = CollationTable.Root;
        for (int cp = 0; cp < 0x100; cp++)
            Assert.True(t.HasExplicitMapping(cp), $"U+{cp:X4} has no explicit mapping");
    }

    [Fact]
    public void Lookup_KnownWeights_MatchTheSourceDataScaled()
    {
        var t = CollationTable.Root;
        int shift = t.PrimaryShift;
        // allkeys_CLDR.txt (UCA 17.0.0): 0061 ; [.23EC.0020.0002] LATIN SMALL LETTER A · 0041 ; [.23EC.0020.0008]
        var a = t.Lookup('a');
        var upperA = t.Lookup('A');
        Assert.Equal(0x23EC << shift, a.Primary);
        Assert.Equal(0x0020, a.Secondary);
        Assert.Equal(0x0002, a.Tertiary);
        Assert.False(a.IsVariable);
        Assert.Equal(a.Primary, upperA.Primary);
        Assert.Equal(0x0008, upperA.Tertiary);
        // 0020 ; [*0209.0020.0002] SPACE — variable
        var space = t.Lookup(' ');
        Assert.True(space.IsVariable);
        Assert.Equal(0x0209 << shift, space.Primary);
        // Controls are completely ignorable; digits precede letters; '0' < '1'.
        Assert.True(t.Lookup(0x0001).IsCompletelyIgnorable);
        Assert.True(t.Lookup('0').Primary < t.Lookup('a').Primary);
        Assert.True(t.Lookup('0').Primary < t.Lookup('1').Primary);
        Assert.True(t.Lookup('9').Primary < t.Lookup('a').Primary);
        Assert.True(t.Lookup(' ').Primary < t.Lookup('0').Primary);
        // The CLDR bounds: U+FFFE the lowest primary, U+FFFF the highest.
        Assert.Equal(0x0001 << shift, t.Lookup(0xFFFE).Primary);
        Assert.Equal(0xFFFE << shift, t.Lookup(0xFFFF).Primary);
    }

    [Fact]
    public void GetElements_Expansions_HaveTheSourceShape()
    {
        var t = CollationTable.Root;
        // 00E6 ; [.23EC.0020.0004][.0000.011F.0004][.2453.0020.0004] LATIN SMALL LETTER AE — a, ligature mark, e
        var ae = t.GetElements(0x00E6).Span;
        Assert.Equal(3, ae.Length);
        Assert.Equal(t.Lookup('a').Primary, ae[0].Primary);
        Assert.Equal(0, ae[1].Primary);
        Assert.Equal(0x011F, ae[1].Secondary);
        Assert.Equal(t.Lookup('e').Primary, ae[2].Primary);
        // 00DF ; [.2632.0020.0004][.0000.011F.0004][.2632.0020.0004] LATIN SMALL LETTER SHARP S — s, mark, s
        var sharpS = t.GetElements(0x00DF).Span;
        Assert.Equal(3, sharpS.Length);
        Assert.Equal(t.Lookup('s').Primary, sharpS[0].Primary);
        Assert.Equal(t.Lookup('s').Primary, sharpS[2].Primary);
        // 00C1 ; [.23EC.0020.0008][.0000.0024.0002] LATIN CAPITAL LETTER A WITH ACUTE — A, then the acute's secondary
        var aAcute = t.GetElements(0x00C1).Span;
        Assert.Equal(2, aAcute.Length);
        Assert.Equal(t.Lookup('a').Primary, aAcute[0].Primary);
        Assert.Equal(0x0024, aAcute[1].Secondary);
        // Lookup returns the FIRST element.
        Assert.Equal(aAcute[0], t.Lookup(0x00C1));
    }

    [Fact]
    public void GetElements_HangulSyllable_IsItsJamo()
    {
        var t = CollationTable.Root;
        // U+AC00 (가) = U+1100 + U+1161; U+AC01 (각) = U+1100 + U+1161 + U+11A8
        var ga = t.GetElements(0xAC00).ToArray();
        var expected = t.GetElements(0x1100).ToArray().Concat(t.GetElements(0x1161).ToArray()).ToArray();
        Assert.Equal(expected, ga);
        var gak = t.GetElements(0xAC01).ToArray();
        Assert.Equal(expected.Concat(t.GetElements(0x11A8).ToArray()).ToArray(), gak);
        Assert.False(t.HasExplicitMapping(0xAC00));
        Assert.Equal(t.Lookup(0x1100), t.Lookup(0xAC00));
    }

    /// <summary>UTS #10 Table 16: [.AAAA.0020.0002][.BBBB.0000.0000] with AAAA/BBBB by script family.</summary>
    [Fact]
    public void GetElements_ImplicitWeights_FollowTable16()
    {
        var t = CollationTable.Root;
        int shift = t.PrimaryShift;
        void AssertImplicit(int cp, int aaaa, int bbbb)
        {
            var e = t.GetElements(cp).Span;
            Assert.Equal(2, e.Length);
            Assert.Equal(new CollationElement(aaaa << shift, 0x20, 0x02), e[0]);
            Assert.Equal(new CollationElement(bbbb << shift, 0, 0), e[1]);
            Assert.False(t.HasExplicitMapping(cp));
        }
        AssertImplicit(0x4E00, 0xFB40, 0x4E00 | 0x8000);            // core Han
        AssertImplicit(0x9FFF, 0xFB41, 0x9FFF & 0x7FFF | 0x8000);   // core Han, CP >> 15 = 1
        AssertImplicit(0x3400, 0xFB80, 0x3400 | 0x8000);            // Extension A: other Han
        AssertImplicit(0x20000, 0xFB84, 0x8000);                    // Extension B: 0xFB80 + (0x20000 >> 15)
        AssertImplicit(0x17000, 0xFB00, 0x8000);                    // Tangut: (CP \U00002212 17000) | 8000
        AssertImplicit(0x18800, 0xFB01, 0x8000);                    // Tangut Components (17.0 base)
        AssertImplicit(0x1B170, 0xFB02, 0x8000);                    // Nushu
        AssertImplicit(0x18B00, 0xFB03, 0x8000);                    // Khitan Small Script
        AssertImplicit(0x0378, 0xFBC0, 0x0378 | 0x8000);            // unassigned
        AssertImplicit(0xE000, 0xFBC1, 0xE000 & 0x7FFF | 0x8000);   // private use ("any other"): 0xFBC0 + (0xE000 >> 15)
        AssertImplicit(0xD800, 0xFBC1, 0xD800 & 0x7FFF | 0x8000);   // an unpaired surrogate code unit still orders
        AssertImplicit(0x10FFFF, 0xFBC0 + (0x10FFFF >> 15), 0x10FFFF & 0x7FFF | 0x8000);
        // Everything implicit sorts above the highest explicit primary and below U+FFFF.
        Assert.True(t.Lookup(0x4E00).Primary > t.Lookup('z').Primary);
        Assert.True(t.Lookup(0x0378).Primary > t.Lookup(0x4E00).Primary);
        Assert.True(t.Lookup(0x10FFFF).Primary < t.Lookup(0xFFFF).Primary);
    }

    [Fact]
    public void NonStarters_AreTheCombiningMarks()
    {
        var t = CollationTable.Root;
        Assert.True(t.IsNonStarter(0x0301));           // COMBINING ACUTE ACCENT, ccc 230
        Assert.Equal(230, t.CombiningClass(0x0301));
        Assert.Equal(220, t.CombiningClass(0x0323));   // COMBINING DOT BELOW
        Assert.False(t.IsNonStarter('a'));
        Assert.False(t.IsNonStarter(0x00E9));          // precomposed é is a starter
        Assert.Equal(0, t.CombiningClass('a'));
        Assert.True(t.IsNonStarter(0x1D165));          // MUSICAL SYMBOL COMBINING STEM (supplementary, ccc 216)
    }

    private static string Sha256(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}
