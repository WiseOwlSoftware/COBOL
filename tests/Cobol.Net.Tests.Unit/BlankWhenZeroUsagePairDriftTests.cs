// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Editions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE USAGES BLANK WHEN ZERO NAMES ARE ONE SET, ASKED OF ONE PREDICATE, BY ITS SCREEN AND ITS PROMOTION ALIKE
/// (kb/Work PB508). ISO §13.18.8.3 SR2 ("The subject of the entry shall be implicitly or explicitly described as
/// usage display or usage national") is the clause's SCREEN, and §13.18.8.4 GR2 ("If the subject of the entry is
/// described by its picture character-string as category numeric, the BLANK WHEN ZERO clause defines the item as
/// numeric-edited") is its PROMOTION. The promotion once spelled the pair as the single <c>Usage.Display</c> the
/// screen had been widened beyond, so a national-usage numeric item passed the screen and was never defined
/// numeric-edited — no blanking, no arithmetic-resultant refusal — and nothing failed.
/// <para><b>What this pins.</b> The BEHAVIOUR is measured over every <see cref="Usage"/> member rather than over a list
/// of cases, so a usage added to <see cref="UsageFamilies.IsDisplayOrNational"/> is promoted without a second edit and
/// a usage added to the enum is measured the day it exists: a category-numeric picture with the clause is
/// numeric-edited exactly when the usage is in the pair. The SHAPE is that both readers ask the one predicate, and
/// neither re-spells <c>Usage.Display</c> on the clause's line. The goldens
/// <c>pb508_blank_when_zero_national</c> and <c>pb508_blank_when_zero_national_resultant</c> are the end-to-end
/// witnesses.</para>
/// </summary>
public sealed class BlankWhenZeroUsagePairDriftTests
{
    /// <summary>The file's code with its whole-line comments removed (a trailing comment and a string literal that
    /// holds a comment opener are left alone: PictureAnalyzer's PICTURE symbols include '/' and '*').</summary>
    private static string Code(params string[] segments) =>
        string.Join('\n', File.ReadAllLines(TestRepo.Src(["Cobol.Net.Compiler", .. segments]))
            .Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal)));

    [Fact]
    public void GR2_PromotesExactlyTheUsagesTheScreenAdmits_OverEveryUsage()
    {
        var measured = new HashSet<Usage>();
        foreach (var usage in Enum.GetValues<Usage>())
        {
            var ed = new EditionContext(2023);
            PicInfo pic;
            try
            {
                pic = PictureAnalyzer.Analyze("9(5)", usage, ed, "data item 'T'", blankWhenZero: true, explicitUsage: true);
            }
            catch (Exception ex) when (ex is not Xunit.Sdk.XunitException)
            {
                // A usage that takes no PICTURE (index, pointer, object reference …) is outside GR2's domain —
                // but it must be outside the pair too, or the pair names a usage no picture can carry.
                Assert.False(UsageFamilies.IsDisplayOrNational(usage),
                    $"USAGE {usage} is in the pair yet PIC 9(5) is refused: {ex.Message}");
                continue;
            }

            // A usage the PICTURE cannot carry is corrected to DISPLAY after its own diagnostic (SR5, SR12 …), so the
            // promotion would be measured against the wrong usage; GR2's domain is the legal combinations.
            if (pic.IsRecovery || ed.HasErrors) continue;
            measured.Add(usage);
            bool promoted = pic.Category == PicCategory.NumericEdited;
            bool admitted = UsageFamilies.IsDisplayOrNational(usage);
            Assert.True(promoted == admitted,
                $"USAGE {usage}: BLANK WHEN ZERO on PIC 9(5) {(promoted ? "defined" : "did not define")} the item numeric-edited, "
                + $"but §13.18.8.3 SR2 / §13.18.8.4 GR2 admit that usage {(admitted ? "" : "not ")}as display or national.");
        }

        // The measurement must reach both members of the pair and a usage outside it, or it proves nothing.
        Assert.Contains(Usage.Display, measured);
        Assert.Contains(Usage.National, measured);
        Assert.Contains(Usage.Packed, measured);
    }

    [Fact]
    public void PromotionAndScreen_AskTheOnePredicate_AndNeitherSpellsUsageDisplay()
    {
        string promotion = Code("Binding", "PictureAnalyzer.cs");
        string line = promotion.Split('\n').Single(l => l.Contains("blankWhenZero && digits > 0"));
        Assert.Contains("UsageFamilies.IsDisplayOrNational(usage)", line);
        Assert.DoesNotContain("Usage.Display", line);

        string screen = Code("Binding", "DataBinder.ClausePlacement.cs");
        Match violation = Regex.Match(screen, @"BlankWhenZeroViolation\(PicInfo pic\)\s*\{.*?\n    \}", RegexOptions.Singleline);
        Assert.True(violation.Success, "BlankWhenZeroViolation is the BLANK WHEN ZERO SR2 screen; it moved or was renamed.");
        Assert.Contains("UsageFamilies.IsDisplayOrNational(pic.Usage)", violation.Value);
        Assert.DoesNotContain("Usage.Display", violation.Value);
    }
}
