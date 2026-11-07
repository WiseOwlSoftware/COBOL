// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// <b>The &gt;&gt;COBOL-WORDS SPELLING sweep</b> (ISO §2023:7.3.10.4 GR2-GR4; kb/Work PB4572): the compiler source holds no
/// raw-word comparison of a word the directive can name and the lexer does not tokenize. The reach invariant itself
/// (two mechanisms, one partition) is <see cref="CobolWordsReachDriftTests"/>; this class is the tree-wide half of it.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class CobolWordsSpellingSweepDriftTests
{
    /// <summary>⛔ A WORD THE LEXER DOES NOT TOKENIZE IS ASKED THROUGH THE MAP, NEVER BY ITS SPELLING (ISO §2023:7.3.10.4 GR2-GR4;
    /// kb/Work PB4572). A spelling test of such a word (<c>CobolNames.Same(word, "COMMA")</c>) is inert to the directive
    /// in both directions: it refused <c>DECIMAL-POINT IS CMA</c> after <c>EQUATE "COMMA" WITH "CMA"</c>, and
    /// it took <c>EXO</c> for an undefined name after <c>EQUATE "EXCEPTION-OBJECT" WITH "EXO"</c>. The population is
    /// the words the directive can NAME (SR3: §2023:8.9 reserved at <see cref="CobolWordsMap.DirectiveEdition"/> and §2023:8.10
    /// context-sensitive) that the lexer spells in no rule, derived from the two tables and the lexer grammar, so the next
    /// such word is covered without an edit here. The form scanned is the raw-word comparison, <c>CobolNames.Same</c> /
    /// <c>CobolKeywords.Same</c> with the word as a literal; a comparison of a CANONICAL value (<c>sig.Name ==</c>, a
    /// phrase already through <see cref="CobolWordsMap.Resolve"/>) has another shape and is not this rule's business.
    /// Content-filter rule: at most a few offenders are named.</summary>
    [Fact]
    public void NoCompilerSourceComparesAnUnlexedDirectiveWord_ByItsSpelling()
    {
        var lexed = CobolWordsReachDriftTests.LexerRuleLiterals();
        var unlexed = DirectiveNamableWords().Where(w => !lexed.Contains(w)).ToHashSet(StringComparer.Ordinal);
        Assert.True(unlexed.Count > 20, $"only {unlexed.Count} directive-namable words with no lexer token — an input failed to load");
        Assert.Contains("COMMA", unlexed);
        Assert.Contains("EXCEPTION-OBJECT", unlexed);

        var compare = new Regex(@"\b(?:CobolNames|CobolKeywords)\.Same\(", RegexOptions.Compiled);
        var literal = new Regex("\"([A-Z][A-Z0-9_-]*)\"", RegexOptions.Compiled);
        int compared = 0;
        var offenders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler"), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")) continue;
            foreach (var (line, n) in File.ReadLines(file).Select((l, i) => (l, i + 1)))
            {
                if (line.TrimStart().StartsWith("//", StringComparison.Ordinal) || !compare.IsMatch(line)) continue;
                compared++;
                foreach (Match m in literal.Matches(line))
                    if (unlexed.Contains(m.Groups[1].Value))
                        offenders.Add($"{Path.GetFileName(file)}:{n} \"{m.Groups[1].Value}\"");
            }
        }
        Assert.True(compared > 20, $"only {compared} CobolNames/CobolKeywords.Same calls found — the scan broke");
        Assert.True(offenders.Count == 0,
            $"{offenders.Count} spelling test(s) of a word the lexer does not tokenize ignore a >>COBOL-WORDS synonym "
            + $"(ISO §2023:7.3.10.4 GR2): ask CobolWordsMap.Is instead (e.g. {string.Join(", ", offenders.Take(3))})");
    }

    /// <summary>The words an EQUATE, UNDEFINE or SUBSTITUTE can name (§2023:7.3.10.3 SR3): §2023:8.9 reserved at the directive's edition
    /// and §2023:8.10 context-sensitive. (Intrinsic function names are the third SR3 population; they reach the binder through
    /// the catalog's canonical name.)</summary>
    private static HashSet<string> DirectiveNamableWords()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var doc = JsonDocument.Parse(File.ReadAllText(TestRepo.VersionMatrix("reserved-words.json"))))
            foreach (var e in doc.RootElement.GetProperty("words").EnumerateArray())
                if (e.GetProperty("r2023").GetBoolean()) set.Add(e.GetProperty("word").GetString()!);
        using (var ctx = JsonDocument.Parse(File.ReadAllText(TestRepo.VersionMatrix("context-sensitive-words.json"))))
            foreach (var e in ctx.RootElement.GetProperty("words").EnumerateArray())
                set.Add(e.GetProperty("word").GetString()!);
        return set;
    }
}
