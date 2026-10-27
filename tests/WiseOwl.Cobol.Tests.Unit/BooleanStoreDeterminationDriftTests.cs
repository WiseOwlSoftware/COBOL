// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// kb/Work PB3697 — THE BOOLEAN-STORE DETERMINATION IS ONE TEXT, WORD FOR WORD, IN THREE PLACES. Which stores convert a boolean
/// item's characters (determination D-B5, kb/Work PB3666) and which content no store can reach (§14.6.13.2 1)) is stated in the
/// <c>RuntimeApi.StrStoreBoolean</c> summary, in the D19 paragraph of <c>docs/DATA_MODEL_DESIGN.md</c> and in determination D-B5 of
/// <c>docs/CONFORMANCE.md</c>. They had drifted into three different lists of bypasses, each wrong, so each carries the sentence
/// below verbatim (comment markers, backticks and line breaks aside): a fourth bypass is added to all three or to none. The
/// structure the text describes is pinned by <c>ReceivingStoreDriftTests.EveryStoreIntoABooleanItem_ConvertsItsCharacters</c>.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class BooleanStoreDeterminationDriftTests
{
    private const string Determination =
        "Every STORE into a boolean item converts its characters. That includes a GR4 group move (the decoded "
        + "characters of a national or display receiver; a USAGE BIT receiver's bits are all valid) and a READ or RETURN into an "
        + "elementary record area. A boolean item can still hold a character other than 0 or 1 only through another description of "
        + "the same storage, which no store into the item reaches: a group receiver (GR4 moves the group's bytes), a REDEFINES "
        + "view, or storage shared through a BY REFERENCE argument, EXTERNAL, BASED or ADDRESS OF. That content is undefined "
        + "(§14.6.13.2 1)), and the boolean operators read such a position as zero.";

    private static IReadOnlyDictionary<string, string> Places => new Dictionary<string, string>
    {
        ["RuntimeApi.cs (StrStoreBoolean)"] = TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "Roslyn", "RuntimeApi.cs"),
        ["DATA_MODEL_DESIGN.md (D19)"] = TestRepo.Docs("DATA_MODEL_DESIGN.md"),
        ["CONFORMANCE.md (D-B5)"] = TestRepo.Docs("CONFORMANCE.md"),
    };

    [Fact]
    public void TheDetermination_IsCarriedWordForWord_InAllThreePlaces()
    {
        foreach (var (place, path) in Places)
        {
            string text = Regex.Replace(Regex.Replace(File.ReadAllText(path), "///|</?para>|`", " "), @"\s+", " ");
            Assert.True(text.Contains(Determination, StringComparison.Ordinal),
                $"{place} no longer carries the boolean-store determination word for word (kb/Work PB3697).");
        }
    }

    [Fact]
    public void TheOldBypassWording_DoesNotComeBack()
    {
        // PB3666 named two bypasses and two exemptions in these places; the determination above replaced all of it.
        foreach (var (place, path) in Places)
            Assert.False(Regex.IsMatch(File.ReadAllText(path), "Two things bypass|Two exemptions|The exemptions:"),
                $"{place} spells the superseded 'two bypasses / two exemptions' wording (kb/Work PB3697).");
    }
}
