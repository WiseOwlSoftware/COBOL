// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVERY RESERVED WORD WRITTEN AS AN ENTRY'S NAME IS NAMED AS ONE (kb/Work PB382). ISO §2023:8.3.2.1 SR1 ("Reserved words shall not
/// be used as user-defined words or system-names") and §1985:IV.4.2.2.1.3 make <c>01 CLASS PIC X.</c> one fault, the word, and the
/// one answer is COBOL0901 naming it. The population is the generated reserved-word table, every word reserved at the edition, so
/// a word the table gains is asked the day it is added. The entry-name slot is optional (§1985:VI.5.3.2 Format 1, §2023:13.16.3
/// SR4), so a word that begins a clause the entry then PARSES whole as is that clause of an unnamed entry, not a name: the source
/// is accepted (<c>01 DISPLAY PIC X.</c>) or refused by the binder for what the clause says (<c>01 BINARY PIC X.</c>, COBOL0881),
/// and those are the conforming reading's answers. Every other word draws exactly one diagnostic, COBOL0901, naming it. The words
/// whose use in the slot is a different fault by design are <see cref="NotANameUse"/>, each with its reason; that list only shrinks.
/// </summary>
public sealed class ReservedEntryNameDriftTests
{
    /// <summary>The words whose use in the entry-name slot is NOT the §2023:8.9 violation, each with its reason. SECTION and
    /// DIVISION write a header, which the slot yields to as a structure fault (kb/Work PB4706). COPY and REPLACE are
    /// compiler-directing statements, read by text manipulation before any entry exists (§1985:XII.2, §2023:7.2).</summary>
    private static readonly Dictionary<string, string> NotANameUse = new(StringComparer.Ordinal)
    {
        ["SECTION"] = "a header word: the slot yields (kb/Work PB4706)",
        ["DIVISION"] = "a header word: the slot yields (kb/Work PB4706)",
        ["COPY"] = "a compiler-directing statement",
        ["REPLACE"] = "a compiler-directing statement",
    };

    public static TheoryData<int> Editions() => [85, 2002, 2014, 2023];

    [Theory]
    [MemberData(nameof(Editions))]
    public void EveryReservedWord_InTheEntryNameSlot_DrawsOneCobol0901NamingIt(int edition)
    {
        var words = ReservedWords.Entries.Select(e => e.Word)
            .Where(w => ReservedWordSet.Default.RejectsAt(w, edition) && !NotANameUse.ContainsKey(w))
            .ToList();
        Assert.True(words.Count > 300, $"the population is the reserved-word table; only {words.Count} words were read at {edition}");
        var wrong = new List<string>();
        int clauses = 0;
        foreach (string word in words)
        {
            var result = RecoverySources.Check(RecoverySources.Program("", workingStorage: $"01 {word} PIC X."), edition);
            bool named = result.Errors.Any(e => e.Contains("COBOL0901"));
            if (result.Success || result.Status == CompilerDriver.Outcome.BindError && !named) { clauses++; continue; }
            if (result.Errors.Count != 1 || !named || !result.Errors[0].Contains($"'{word}'"))
                wrong.Add($"{word}: {string.Join(" | ", result.Errors)}");
        }
        Assert.True(wrong.Count == 0, $"{wrong.Count} reserved word(s) at {edition} are not named as a reserved entry-name:\n"
            + string.Join("\n", wrong));
        // a probe that read most words as clauses would test no name slot
        Assert.True(clauses < words.Count / 4, $"{clauses} of {words.Count} words read as a clause of an unnamed entry");
    }

    [Fact]
    public void TheNamedExceptions_AreReservedWords()
    {
        foreach (string word in NotANameUse.Keys)
            Assert.True(ReservedWordSet.Default.RejectsAt(word, 2023), $"{word} is no reserved word at 2023: drop it from the exceptions");
    }
}
