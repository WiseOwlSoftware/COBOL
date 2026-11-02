// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE ANNEX B AND ANNEX C TABLES ARE THE STANDARD'S, LIST FOR LIST (kb/Work PB1402). The characters of a COBOL word
/// (Annex B.3's three position classes) and the case fold of a COBOL word (Annex C.2) are transcribed by
/// <c>scripts/spec/gen_annex_bc.py</c> into <c>CobolCharacterRepertoire.Table.cs</c> and <c>CobolNames.Table.cs</c>,
/// together with the Annex E lists that reverse them for COBOL 2002/2014 (E.2 items 4 and 14, E.3.3 items 4, 5 and 6).
/// This test re-reads every one of those ten lists from <c>specs/ISO_COBOL.md</c> and requires the tables to equal
/// them, so a transcription repair in the spec reaches the compiler or fails here; the three pair lists equal the
/// standard's LESS the pairs that cross between a basic and an extended letter, which the generator moves to
/// <c>CobolNames.NotApplied</c> (§8.1.3.2 4) b), kb/Work PB4420), and no extended letter folds to a basic one at any
/// edition. It also holds the two facts the
/// fold's implementation relies on: every Annex C mapping keeps a character's UTF-16 width (so a word and its fold
/// line up unit for unit), and every mapping Annex E.3.3 item 6 added at 2023 pairs a character the 2002/2014 Annex B
/// does not hold (so the ONE 2023 comparer is exact for those editions once the lexer has written E.2 item 14's two
/// letters in their spelling).
/// </summary>
[Trait("Sweep", "tree")]
public sealed class AnnexBCDriftTests
{
    private static readonly string[] Spec = File.ReadAllLines(TestRepo.Specs("ISO_COBOL.md"));

    private static readonly Regex Range = new(@"(?<![0-9A-Za-z])([0-9A-F]{4,5})(?:-([0-9A-F]{4,5}))?(?![0-9A-Za-z])");
    private static readonly Regex Pair = new(@"\(([0-9A-F]{4,5}),([0-9A-F]{4,5})\)");

    private static IEnumerable<string> Section(string opener, string closer)
    {
        int start = Array.FindIndex(Spec, l => l.StartsWith(opener, StringComparison.Ordinal));
        Assert.True(start >= 0, $"spec marker not found: {opener}");
        int end = Array.FindIndex(Spec, start + 1, l => l.StartsWith(closer, StringComparison.Ordinal));
        Assert.True(end > start, $"spec marker not found after '{opener}': {closer}");
        return Spec[(start + 1)..end];
    }

    private static SortedSet<int> CodePoints(IEnumerable<string> body)
    {
        var set = new SortedSet<int>();
        foreach (string line in body)
        {
            if (line.TrimStart().StartsWith("NOTE", StringComparison.Ordinal)) continue;
            foreach (Match m in Range.Matches(line))
            {
                int lo = Convert.ToInt32(m.Groups[1].Value, 16);
                int hi = m.Groups[2].Success ? Convert.ToInt32(m.Groups[2].Value, 16) : lo;
                for (int cp = lo; cp <= hi; cp++) set.Add(cp);
            }
        }
        return set;
    }

    private static SortedSet<int> CodePoints((int First, int Second)[] ranges)
    {
        var set = new SortedSet<int>();
        foreach (var (lo, hi) in ranges) for (int cp = lo; cp <= hi; cp++) set.Add(cp);
        return set;
    }

    private static List<(int, int)> Pairs(IEnumerable<string> body) =>
        [.. body.SelectMany(l => Pair.Matches(l)).Select(m => (Convert.ToInt32(m.Groups[1].Value, 16), Convert.ToInt32(m.Groups[2].Value, 16)))];

    private const string B1 = @"1\) The following characters are permitted in a user-defined word.";
    private const string B2 = @"2\) The following characters are permitted in a user-defined word except as the start character.";
    private const string B3 = @"3\) The following characters are permitted in a user-defined word except as the start or last character.";
    private const string E24 = @"4\) **Characters permitted in user-defined words.** The following character, represented";
    private const string E24Medial = "   The following character has been changed so it is not allowed for the start or last character";
    private const string E33i4 = @"4\) **Characters permitted in user-defined words.** The following characters have been changed";
    private const string E33i5 = @"5\) **Characters permitted in user-defined words.** The following characters have been added.";
    private const string E33i6 = @"6\) **General case mappings.** The following case mappings have been added.";

    public static TheoryData<string, string, string> RangeLists => new()
    {
        { nameof(CobolCharacterRepertoire.B3Anywhere), B1, B2 },
        { nameof(CobolCharacterRepertoire.B3NotFirst), B2, B3 },
        { nameof(CobolCharacterRepertoire.B3Medial), B3, "<a id=\"section-annex-c\"></a>" },
        { nameof(CobolCharacterRepertoire.E2Item4Deleted), E24, E24Medial },
        { nameof(CobolCharacterRepertoire.E2Item4MadeMedial), E24Medial, "   Justification:" },
        { nameof(CobolCharacterRepertoire.E33Item4MadeFirst), E33i4, E33i5 },
        { nameof(CobolCharacterRepertoire.E33Item5Added), E33i5, E33i6 },
    };

    [Theory]
    [MemberData(nameof(RangeLists))]
    public void AnnexBList_EqualsTheSpec(string field, string opener, string closer)
    {
        var table = (( int First, int Second)[])typeof(CobolCharacterRepertoire)
            .GetField(field, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.GetValue(null)!;
        Assert.Equal(CodePoints(Section(opener, closer)), CodePoints(table));
    }

    /// <summary>Whether an (uppercase, lowercase) pair maps between the BASIC letters (A-Z, a-z) and the EXTENDED ones,
    /// derived here from the pair alone — the same reading as the generator's (§8.1.3.2 4) b): the corresponding lowercase
    /// extended letter), written independently so the two cannot drift together.</summary>
    private static bool Crosses((int Upper, int Lower) pair) => IsBasicLetter(pair.Upper) != IsBasicLetter(pair.Lower);

    private static bool IsBasicLetter(int codePoint) => codePoint is >= 'A' and <= 'Z' or >= 'a' and <= 'z';

    /// <summary>Each Annex C list equals the standard's, less the pairs that cross between the basic and the extended
    /// letters (the fold applies those to no word, kb/Work PB4420), and those pairs are exactly
    /// <see cref="CobolNames.NotApplied"/>, in the lists' order.</summary>
    [Fact]
    public void AnnexCLists_EqualTheSpec()
    {
        (string Name, List<(int, int)> Printed, (int First, int Second)[] Applied)[] lists =
        [
            (nameof(CobolNames.C2Mappings), Pairs(Section("### C.2 General case mappings", "<a id=\"section-annex-d\"></a>")), CobolNames.C2Mappings),
            (nameof(CobolNames.E2Item14Deleted), Pairs(Section(@"14\) **General case mappings.** The following case mappings have been deleted.", "Justification:")), CobolNames.E2Item14Deleted),
            (nameof(CobolNames.E33Item6Added), Pairs(Section(E33i6, @"7\) **Clarification** of exception handling procedures.")), CobolNames.E33Item6Added),
        ];
        var crossing = new List<(int, int)>();
        foreach (var (name, printed, applied) in lists)
        {
            Assert.True(printed.Count > 0, $"{name}: the spec marker found no pairs");
            Assert.Equal(printed.Where(p => !Crosses(p)).ToList(), applied.Select(p => (p.First, p.Second)).ToList());
            crossing.AddRange(printed.Where(Crosses));
        }
        Assert.Equal(crossing, CobolNames.NotApplied.Select(p => (p.First, p.Second)).ToList());
    }

    /// <summary>⛔ THE DETERMINATION (kb/Work PB4420, owner decision 2026-11-01, GnuCOBOL's reading of §8.1.3.2 4) b)): the
    /// fold applies no pair between a basic letter and an extended one. Today that leaves out exactly the dotted capital I
    /// (0130,0069), the Kelvin sign (212A,006B) and the dotless i of the 2002/2014 table (0131,0069); a table that gains a
    /// pair of the kind changes this list, and the change is seen here.</summary>
    [Fact]
    public void NotApplied_IsExactlyTheCrossingPairs_OfTheMeasuredTable()
    {
        Assert.Equal([(0x0130, 0x0069), (0x212A, 0x006B), (0x0131, 0x0069)],
            CobolNames.NotApplied.Select(p => (p.First, p.Second)).ToArray());
    }

    /// <summary>⛔ NO EXTENDED LETTER FOLDS TO A BASIC ONE, AT ANY EDITION — the proof that the fold of a user-defined word
    /// never makes a spelling with an extended letter equal a keyword (a reserved word, context-sensitive word, intrinsic
    /// function name, system-name, implementor-name or directive word, every one written in basic characters,
    /// <see cref="EveryKeywordPopulation_IsBasicCharactersOnly"/>): a folded word equals a keyword only if each of its
    /// characters folds to the keyword's, and no character at or above U+0080 folds below it. It is what lets the compiler
    /// sites that ask <see cref="CobolNames.Same(string?, string?)"/> or <see cref="CobolNames.UpperFold"/> about a keyword,
    /// and the fold-keyed word tables, answer exactly as the basic-letter comparison does.</summary>
    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void NoCharacterAboveAscii_FoldsToAscii(int edition)
    {
        var folded = new List<string>();
        for (int cp = 0x80; cp <= 0x10FFFF; cp++)
            if (CobolNames.Fold(cp, edition) < 0x80) folded.Add($"U+{cp:X4}");
        Assert.True(folded.Count == 0, "folds to a basic character: " + string.Join(", ", folded));
        for (int cp = 0; cp < 0x80; cp++)
            Assert.True(CobolNames.Fold(cp, edition) < 0x80, $"U+{cp:X4} folds to a non-basic character");
    }

    /// <summary>Every keyword population the compiler holds is written in basic characters alone (ASCII letters, digits,
    /// hyphen, underscore), which with <see cref="NoCharacterAboveAscii_FoldsToAscii"/> proves the Annex C fold and the
    /// basic-letter keyword comparison give one answer for every keyword.</summary>
    [Fact]
    public void EveryKeywordPopulation_IsBasicCharactersOnly()
    {
        var intrinsics = ((System.Collections.IDictionary)typeof(IntrinsicCatalog)
            .GetField("Table", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.GetValue(null)!)
            .Keys.Cast<string>();
        var populations = new (string Name, IEnumerable<string> Words)[]
        {
            ("reserved words (§8.9)", ReservedWords.Entries.Select(e => e.Word)),
            ("context-sensitive words (§8.10)", ContextSensitiveWords.Entries.Select(e => e.Word)),
            ("intrinsic function names (§8.11)", intrinsics),
            ("implementor names", ImplementorNames.All.Keys),
            ("compiler-directive words (§8.12)", CompilerDirectiveCatalog.Words),
        };
        foreach (var (name, words) in populations)
        {
            var list = words.ToList();
            Assert.NotEmpty(list);
            var bad = list.Where(w => w.Any(c => !(c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_'))).ToList();
            Assert.True(bad.Count == 0, $"{name} holds a word with a non-basic character: " + string.Join(", ", bad));
        }
    }

    /// <summary>CobolNames compares a word and its fold unit for unit; that is exact only while no mapping changes a
    /// character's UTF-16 width.</summary>
    [Fact]
    public void EveryAnnexCMapping_KeepsTheCharacterWidth()
    {
        foreach (var (upper, lower) in CobolNames.C2Mappings.Concat(CobolNames.E2Item14Deleted).Concat(CobolNames.NotApplied))
            Assert.True(upper > 0xFFFF == lower > 0xFFFF, $"({upper:X4},{lower:X4}) changes width");
    }

    /// <summary>The ONE (2023) comparer serves COBOL 2002/2014 only because no mapping 2023 added can join two words
    /// those editions accept: one side of each is outside their Annex B, so the word is refused before any fold.</summary>
    [Fact]
    public void EveryMappingAddedIn2023_PairsACharacterOutsideThe2014Repertoire()
    {
        foreach (var (upper, lower) in CobolNames.E33Item6Added)
            Assert.True(CobolCharacterRepertoire.PositionOf(upper, 2014) == WordCharacterPosition.NotPermitted
                        || CobolCharacterRepertoire.PositionOf(lower, 2014) == WordCharacterPosition.NotPermitted,
                $"({upper:X4},{lower:X4}) joins two COBOL-2014 characters");
    }
}
