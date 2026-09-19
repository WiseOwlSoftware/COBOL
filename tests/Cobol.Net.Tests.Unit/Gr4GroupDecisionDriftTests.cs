// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.IO;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ <b>ISO §14.9.25.4 GR4 IS ONE RULE OVER BOTH OPERANDS, AND ONE CODEC IN BOTH DIRECTIONS</b> (kb/Work PB430).
/// The two defects this pins had the same shape — a question answered in two places, and the two answers drifted:
/// the elementary-vs-group decision was asked structurally on the receiving side and through
/// <c>IsGroupPlace</c> on the sending side (so a level-66 THROUGH alias was a group SENDER and an elementary
/// RECEIVER), and the sender's image was taken from the representation codec in the group-sender arm and from
/// the operand TEXT in the group-receiver arm (so a COMP-3 item deposited BCD bytes one way and DISPLAY digits
/// the other). Neither is a shape a behavioural test can hold on its own: the wrong answer is silent, legal
/// source produces it, and the next arm added to either switch would inherit it. These assert the SHAPE
/// (feedback_two_arm_dispatch; CLAUDE.md rule 5 — pair the structure with a drift test).
/// </summary>
public sealed class Gr4GroupDecisionDriftTests
{
    private static string Classifier() => StripComments(File.ReadAllText(
        TestRepo.Src("Cobol.Net.Compiler", "Binding", "Bound", "MoveClassifier.cs")));

    private static string MoveEmitter() => StripComments(File.ReadAllText(
        TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs", "MoveEmitter.cs")));

    /// <summary>The RECEIVING half of GR4's test goes through the SAME predicate the sending half does. A second
    /// spelling of the structural test is the defect: §13.18.45.4 GR2 and §13.18.29.4 GR1b/GR2b both make the
    /// answer non-structural, and a re-spelling sees neither.</summary>
    [Fact]
    public void MoveClassifier_ReceivingHalfOfGr4_AsksIsGroupPlace_NotAReSpelling()
    {
        string src = Classifier();
        var kind = Regex.Match(src, @"public\s+static\s+MoveKind\s+Kind\s*\([^)]*\)(?<b>.*?)\n    \}",
            RegexOptions.Singleline);
        Assert.True(kind.Success, "MoveClassifier.Kind is no longer recognizable");
        Assert.Contains("IsGroupPlace(target)", kind.Groups["b"].Value);
        Assert.DoesNotMatch(@"target\s*\.\s*Item\s*\.\s*IsGroup\b", kind.Groups["b"].Value);
    }

    /// <summary>THE ONE PLACE the §14.9.25.4 GR4 group designation is decided — so a shape the standard
    /// DESIGNATES a group item (a RENAMES … THROUGH alias, §13.18.45.4 GR2) is one everywhere or nowhere.</summary>
    [Fact]
    public void MoveClassifier_IsGroupPlace_IsTheOnlySiteThatNamesRenamesPlace()
    {
        string src = Classifier();
        int mentions = Regex.Matches(src, @"\bRenamesPlace\b").Count;
        Assert.True(mentions == 1,
            $"MoveClassifier names RenamesPlace {mentions} time(s); IsGroupPlace is the ONE site that decides "
            + "the §14.9.25.4 GR4 group designation, and a second test of the place kind is how the sending and "
            + "receiving halves came to disagree (kb/Work PB430)");
    }

    /// <summary>BOTH arms of the group move read the sender through the ONE representation codec. Reaching for
    /// <c>OperandText.AsString</c> in either of them re-introduces the conversion GR4 forbids for whichever arm
    /// made the slip — and it is silent, because for a DISPLAY sender the two readers agree.</summary>
    [Theory]
    [InlineData("EmitGroupMove")]
    [InlineData("EmitGroupToElementaryMove")]
    public void MoveEmitter_GroupArms_ReadTheSenderThroughTheOneRepresentationCodec(string method)
    {
        string src = MoveEmitter();
        var body = Regex.Match(src, @"private\s+(?:void|bool|string)\s+" + method + @"\s*\([^)]*\)(?<b>.*?)\n    \}",
            RegexOptions.Singleline);
        Assert.True(body.Success, $"MoveEmitter.{method} is no longer recognizable");
        Assert.Contains("OperandText.NonElementaryMoveSender", body.Groups["b"].Value);
        Assert.DoesNotContain("OperandText.AsString", body.Groups["b"].Value);
    }

    /// <summary>Comment-stripped source, so a mechanism DISCUSSED in prose (these files discuss this one at
    /// length) is never mistaken for one performed in code.</summary>
    private static string StripComments(string src) =>
        Regex.Replace(Regex.Replace(src, @"/\*.*?\*/", "", RegexOptions.Singleline), @"//[^\n]*", "");
}
