// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// kb/Work PB1366, PB5599 — <see cref="CompilerDirectiveWords"/> is the ONE representation of the "Compiler-directive
/// words" clause of every edition that has one (§2002:8.12, §2014:8.12, §2023:8.12), and these tests are what keeps it equal to
/// each: a word the table prints and the code lacks is a compilation-variable-name this compiler would accept
/// (§2023:7.3.11.3 SR1, §2023:7.3.8.4.3 SR1), and a word the code has and the table lacks is legal source it would reject.
/// </summary>
[Trait("Sweep", "tree")]
public sealed partial class CompilerDirectiveWordsDriftTests
{
    [GeneratedRegex(@"^#+ 8\.12 Compiler-directive words", RegexOptions.Multiline)]
    private static partial Regex ClauseHeading();

    /// <summary>The §8.12 table of <paramref name="edition"/>, read from the specification itself: every cell of the pipe
    /// table between the clause heading and the "In addition to the above list" sentence.</summary>
    private static HashSet<string> SpecTable(int edition)
    {
        string path = TestRepo.Specs(edition switch
        {
            2002 => "ISO_COBOL_2002.md",
            2014 => "ISO_COBOL_2014.md",
            2023 => "ISO_COBOL_2023.md",
            _ => throw new ArgumentOutOfRangeException(nameof(edition), edition, "no §8.12 table held for this edition"),
        });
        Assert.True(File.Exists(path), $"the ISO transcription is missing: {path}");
        string text = File.ReadAllText(path);
        var head = ClauseHeading().Match(text);
        Assert.True(head.Success, $"clause 8.12 not found in the {edition} transcription");
        int end = text.IndexOf("In addition to the above list", head.Index, StringComparison.Ordinal);
        Assert.True(end > head.Index, "the 8.12 table's closing sentence was not found");
        var words = new HashSet<string>(StringComparer.Ordinal);
        foreach (string row in text[head.Index..end].Split('\n'))
        {
            string r = row.Trim().TrimEnd('\r');
            if (!r.StartsWith('|') || r.StartsWith("|---")) continue;
            foreach (string cell in r.Trim('|').Split('|'))
            {
                string w = cell.Trim().Replace("\\*", "*").Replace("&lt;", "<");
                if (w.Length > 0) words.Add(w);
            }
        }

        return words;
    }

    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void Table_IsEqualToTheSpecTable(int edition)
    {
        var spec = SpecTable(edition);
        var codeList = CompilerDirectiveWords.Of(edition);
        var code = codeList.ToHashSet(StringComparer.Ordinal);
        Assert.True(spec.Count > 60, $"the {edition} 8.12 table parse found only {spec.Count} words");
        Assert.True(spec.SetEquals(code),
            $"§{edition}:8.12 and CompilerDirectiveWords disagree — in the spec only: ["
            + string.Join(", ", spec.Except(code).Order(StringComparer.Ordinal)) + "]; in the code only: ["
            + string.Join(", ", code.Except(spec).Order(StringComparer.Ordinal)) + "]");
        Assert.Equal(codeList.Count, code.Count);   // no duplicate entry
        foreach (string w in spec) Assert.True(CompilerDirectiveWords.IsReserved(w, edition), w);
    }

    /// <summary>COBOL-85 has no compiler directives (§2002:7.2 is the first edition with the facility), so nothing is
    /// reserved in a directive below 2002 — not a table word, not an exception-name.</summary>
    [Theory]
    [InlineData("OFF")]
    [InlineData("EC-SIZE")]
    [InlineData("EC-IMP-SOMETHING")]
    public void Below2002_NothingIsReserved(string word) => Assert.False(CompilerDirectiveWords.IsReserved(word, 1985));

    /// <summary>Every directive the catalog recognizes heads its line with a compiler-directive word (§2023:7.3.3 SR6),
    /// and that word is reserved at every edition at which the directive exists — the windows of the construct row and of
    /// the table are one fact (PB5599: FLAG-85 and FLAG-NATIVE-ARITHMETIC end with 2014, PUSH and POP begin with 2023). A
    /// directive added to <c>tests/version-matrix/constructs/</c> without its word in the table is a red test, never a
    /// silently usable variable name.</summary>
    [Fact]
    public void EveryCatalogDirectiveWord_IsReservedWhereItsDirectiveExists()
    {
        var disagree = new List<string>();
        foreach (string w in CompilerDirectiveCatalog.Words)
        {
            var row = CompilerDirectiveCatalog.Find(w)!;
            foreach (int edition in new[] { 2002, 2014, 2023 })
            {
                bool exists = row.StatusAt(edition) != ConstructAvailability.NotYetIntroduced
                    && row.StatusAt(edition) != ConstructAvailability.Removed;
                if (exists != CompilerDirectiveWords.IsReserved(w, edition)) disagree.Add($"{w}@{edition}");
            }
        }

        Assert.True(disagree.Count == 0,
            $"directive words whose reservation disagrees with their directive's window: [{string.Join(", ", disagree)}]");
    }

    /// <summary>"All of the exception-names specified in 14.6.13.1 are reserved in the context of compiler
    /// directives": the level-1, level-2 and level-3 names of the catalog, and the open EC-USER / EC-IMP families.</summary>
    [Theory]
    [InlineData("EC-ALL")]
    [InlineData("EC-SIZE")]
    [InlineData("EC-SIZE-TRUNCATION")]
    [InlineData("EC-I-O-AT-END")]
    [InlineData("ec-user-anything")]
    [InlineData("EC-IMP-SOMETHING")]
    public void ExceptionNames_AreReserved(string name) => Assert.True(CompilerDirectiveWords.IsReserved(name, 2023));

    /// <summary>An exception-name an edition has not introduced is no exception-name of its §8.12 (the catalog's own
    /// <c>IntroducedIn</c>: EC-MCS is a 2023 level-2 name).</summary>
    [Fact]
    public void ExceptionName_IsReservedFromItsIntroducingEdition()
    {
        Assert.False(CompilerDirectiveWords.IsReserved("EC-MCS", 2014));
        Assert.True(CompilerDirectiveWords.IsReserved("EC-MCS", 2023));
    }

    [Theory]
    [InlineData("XMODE")]
    [InlineData("DEBUG-FLAG")]
    [InlineData("MYVAR")]
    [InlineData("EC-NOT-A-CONDITION")]
    public void OrdinaryNames_AreNot(string name) => Assert.False(CompilerDirectiveWords.IsReserved(name, 2023));

    [Theory]
    [InlineData("off")]
    [InlineData("Listing")]
    [InlineData("IMP")]
    public void Lookup_IsCaseInsensitive(string name) => Assert.True(CompilerDirectiveWords.IsReserved(name, 2002));

    /// <summary>PB5599: a word printed by one edition's table only is reserved at that edition only.</summary>
    [Theory]
    [InlineData("PUSH", 2002, false)]
    [InlineData("PUSH", 2014, false)]
    [InlineData("PUSH", 2023, true)]
    [InlineData("EQUATE", 2014, false)]
    [InlineData("EQUATE", 2023, true)]
    [InlineData("FLAG-85", 2002, true)]
    [InlineData("FLAG-85", 2014, true)]
    [InlineData("FLAG-85", 2023, false)]
    [InlineData("FLAG-NATIVE-ARITHMETIC", 2023, false)]
    [InlineData("FLAG-02", 2002, false)]
    [InlineData("FLAG-02", 2014, true)]
    [InlineData("MOVE-TO-SAME-NAME", 2002, false)]
    [InlineData("MOVE-TO-SAME-NAME", 2014, true)]
    [InlineData("<>", 2002, false)]
    [InlineData("<>", 2014, true)]
    public void EditionKeyedWords(string word, int edition, bool reserved) =>
        Assert.Equal(reserved, CompilerDirectiveWords.IsReserved(word, edition));
}
