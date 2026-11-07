// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ ISO §2023:13.18.60.3 SR14 AS A MATRIX, over every phrase it names and both of the spellings that put the phrase
/// below level 1 (kb/Work PB819): "A USAGE clause with the MESSAGE-TAG, OBJECT REFERENCE, POINTER, FUNCTION-POINTER,
/// or PROGRAM-POINTER phrase may be specified only for an elementary data item at level 1 or an elementary data item
/// subordinate to a type declaration that includes the STRONG phrase."
/// <code>
///     01 G.  05 A USAGE u.            the ELEMENTARY item below level 1     (arm B)
///     01 G USAGE u.  05 A PIC X.      the GROUP spelling, §2023:13.18.60.4 GR1    (arm A)
/// </code>
/// <para>Both are violations of the one rule, so both draw COBOL1724 — for EVERY phrase, whether the compiler
/// models the usage or refuses its keyword (every SR14 phrase is a modelled usage since kb/Work PB4740). The matrix is read off
/// <see cref="ItemCategory.Sr14PhraseOf(Usage?)"/>, so a usage added to SR14's population is a row here without anyone
/// remembering it; one hand-written <c>if</c> per phrase is how the elementary arm went blind to the staged
/// FUNCTION-POINTER and the declined MESSAGE-TAG while the group arm saw them.</para>
/// </summary>
public sealed class UsageDeclarationPlacementMatrixTests
{
    /// <summary>One row per usage SR14 names, spelled as a programmer writes it.</summary>
    public static TheoryData<Usage> Sr14Usages()
    {
        var d = new TheoryData<Usage>();
        foreach (Usage u in Enum.GetValues<Usage>())
            if (ItemCategory.Sr14PhraseOf(u) is not null) d.Add(u);
        return d;
    }

    private static string Program(string id, string ws) =>
        $"""
         IDENTIFICATION DIVISION.
         PROGRAM-ID. {id}.
         DATA DIVISION.
         WORKING-STORAGE SECTION.
         {ws}
         PROCEDURE DIVISION.
             STOP RUN.
         """;

    private static bool Reports1724(string source)
    {
        var (ok, _, detail) = new WiseOwlCobolCompiler(2023).CompileAndRun(source);
        return !ok && Regex.IsMatch(detail, @"COBOL1724");
    }

    [Theory]
    [MemberData(nameof(Sr14Usages))]
    public void EveryPhrase_IsRefusedBelowLevel1_OnBothSpellings(Usage usage)
    {
        string word = UsageFamilies.UsageWord(usage);
        string tag = Regex.Replace(word, "[^A-Z0-9]", "");
        string elementary = Program($"P819E{tag}", $"01 G.\n    05 A USAGE {word}.");
        string group = Program($"P819G{tag}", $"01 G USAGE {word}.\n    05 A PIC X.");
        Assert.True(Reports1724(elementary),
            $"`05 A USAGE {word}.` inside an ordinary group violates §2023:13.18.60.3 SR14 and must draw COBOL1724");
        Assert.True(Reports1724(group),
            $"`01 G USAGE {word}. 05 A PIC X.` violates §2023:13.18.60.3 SR14 (via §2023:13.18.60.4 GR1) and must draw COBOL1724");
    }

    private static int Count1724(string source)
    {
        var (_, _, detail) = new WiseOwlCobolCompiler(2023).CompileAndRun(source);
        return detail.Split('\n').Count(l => l.Contains("COBOL1724"));
    }

    /// <summary>⛔ ONE WRITTEN SR14 VIOLATION DRAWS ONE COBOL1724, THROUGH EVERY COPY OF ITS GROUP (kb/Work PB4705).
    /// §2023:13.18.49.4 GR1 reproduces a level-1 group's members under each SAME AS subject (§2023:13.18.49.3 SR7 admits only an
    /// elementary item or a level 1 group), and a copied member carries the position of the member it copies, so a screen
    /// that asked the clones drew a second COBOL1724 per copy at that one position. The member is judged where it is
    /// written (in the source group, and for the TYPE spelling in `05 PZ TYPE PT`, §2023:13.18.57.4 GR1); the copies add none.
    /// The expected count is the count WITHOUT the copies, so a copy of any usage in SR14's population is a row here.</summary>
    [Theory]
    [MemberData(nameof(Sr14Usages))]
    public void ASameAsCopyOfAViolatingGroup_AddsNoSecondReport(Usage usage)
    {
        string word = UsageFamilies.UsageWord(usage);
        string tag = Regex.Replace(word, "[^A-Z0-9]", "");
        string bare = Program($"P4705B{tag}", $"01 PG.\n    05 PX PIC X.\n    05 PP USAGE {word}.");
        string copied = Program($"P4705C{tag}",
            $"01 PG.\n    05 PX PIC X.\n    05 PP USAGE {word}.\n01 PC SAME AS PG.\n01 G.\n    05 Q SAME AS PG.");
        Assert.Equal(1, Count1724(bare));
        Assert.Equal(1, Count1724(copied));

        string typeBare = Program($"P4705D{tag}", $"01 PT TYPEDEF USAGE {word}.\n01 PG.\n    05 PX PIC X.\n    05 PZ TYPE PT.");
        string typeCopied = Program($"P4705E{tag}",
            $"01 PT TYPEDEF USAGE {word}.\n01 PG.\n    05 PX PIC X.\n    05 PZ TYPE PT.\n01 PC SAME AS PG.\n01 G.\n    05 Q SAME AS PG.");
        Assert.Equal(1, Count1724(typeBare));
        Assert.Equal(1, Count1724(typeCopied));
    }

    /// <summary>The copy of a LEGAL group stays legal: a strong type declaration is where SR14 admits the pointer at level
    /// 05, and a SAME AS copy of the strongly-typed item carries that TYPE identity (§2023:13.18.49.4 GR1), so skipping the copied
    /// members costs the legal shape nothing.</summary>
    [Fact]
    public void ASameAsCopyOfAStronglyTypedGroup_StaysLegal()
    {
        string source = Program("P4705F",
            "01 ST TYPEDEF STRONG.\n    05 PP USAGE POINTER.\n    05 PX PIC X.\n01 SV TYPE ST.\n01 Q SAME AS SV.");
        var (ok, _, detail) = new WiseOwlCobolCompiler(2023).CompileAndRun(source);
        Assert.True(ok, detail);
    }
}
