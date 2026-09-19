// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.IO;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ <b>THE §14.9.25.4 GR2/GR3 SUBSTITUTION IS ASKED ONCE, AT ONE SEAM, AND THE EMITTER MAY NOT GO ROUND IT</b>
/// (kb/Work PB425). The rule rewrites the SENDING operand per RECEIVING operand — a zero-length literal-1 "is
/// treated as if it were the figurative constant SPACE" (GR3: ZERO for a boolean one) unless the receiver is a
/// dynamic-length elementary item — so one statement can legitimately store two different senders, and the
/// answer travels on <c>BoundMove.Stores</c>, one <c>MoveStore(Sender, Kind)</c> per target.
///
/// <para><b>What kept it broken for so long is exactly what this measures.</b> Before PB425 the substitution
/// existed nowhere and it COINCIDED with the required answer at every receiver whose fill is a space, so the
/// corpus never contradicted it. The shape that makes the NEXT case automatic is the per-target store; the way
/// to lose it again is for a renderer to reach past <c>Stores</c> for <c>m.Source</c>, which is scalar and is
/// deliberately the WRITTEN operand the syntax screens read. That is a one-word slip in a five-arm switch, and
/// it would silently restore the defect for whichever arm made it (feedback_two_arm_dispatch).</para>
///
/// <para><b>The complement is asserted too</b>, because the rule has two halves and a test that watched only one
/// would pass while the other rotted: the SYNTAX screens must keep reading <c>m.Source</c>. §14.9.25.3 SR5 names
/// a closed list of figurative constants WRITTEN in the source, so a gate that saw the substituted sender would
/// reject <c>MOVE "" TO PIC 9(3)</c> — legal at every edition — as the 2023 removal.</para>
/// </summary>
public sealed class ZeroLengthSubstitutionDriftTests
{
    private static string MoveEmitter() => StripComments(File.ReadAllText(
        TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs", "MoveEmitter.cs")));

    /// <summary>The MOVE renderer reads the per-target SENDER, never the statement's scalar written source.
    /// <c>m.Source</c> is the operand the programmer wrote; <c>m.Stores[i].Sender</c> is the operand
    /// §14.9.25.4 GR2/GR3 leave for THAT receiver, and only the second is what gets stored.</summary>
    [Fact]
    public void MoveEmitter_NeverRendersTheStatementsScalarSource()
    {
        string src = MoveEmitter();
        Assert.False(Regex.IsMatch(src, @"\bm\s*\.\s*Source\b"),
            "MoveEmitter reads BoundMove.Source. The sending operand of a MOVE is PER RECEIVING OPERAND — "
            + "ISO §14.9.25.4 GR2/GR3 substitute the figurative constant SPACE / ZERO for a zero-length "
            + "literal-1 at every receiver EXCEPT a dynamic-length elementary one, so `MOVE \"\" TO D-DYN, "
            + "N-NUM` stores two different senders from one statement. Render `m.Stores[i].Sender` "
            + "(kb/Work PB425); `m.Source` is the WRITTEN operand, which only the syntax screens may read.");
    }

    /// <summary>The store's dispatch KIND is the one the classifier computed over the SUBSTITUTED sender (so a
    /// zero-length literal into a numeric receiver dispatches to the figurative fill §14.9.25.4 GR2 makes it),
    /// and its ORIGIN says which rule put that sender there — a written figurative is §14.9.25.3 SR5's removed
    /// construct, a substituted one is legal source. All three come off the SAME store in one destructuring:
    /// recovering any of them some other way means reading `m.Source`, which the test above forbids.</summary>
    [Fact]
    public void MoveEmitter_TakesSenderKindAndOriginFromTheSameStore()
    {
        Assert.Matches(@"var\s*\(\s*source\s*,\s*kind\s*,\s*origin\s*\)\s*=\s*m\.Stores\[\s*i\s*\]\s*;",
            MoveEmitter());
    }

    /// <summary>⛔ THE COMPLEMENT: the §14.9.25.3 SR5 edition gates re-derive from the WRITTEN sending operand.
    /// SR5's prohibition is a SYNTAX rule over "an alphanumeric figurative constant (SPACE, QUOTE, HIGH-VALUE,
    /// LOW-VALUE, ALL "literal", or ALL symbolic-character)" — things spelled in the source — while GR2/GR3
    /// substitute a value. A gate reading the substituted sender would turn a wrong answer into a false
    /// rejection, which is the one outcome CLAUDE.md rule 4 forbids outright.</summary>
    [Fact]
    public void Sr5GateMove_ReadsTheWrittenSource_NotTheSubstitutedSender()
    {
        string src = StripComments(File.ReadAllText(
            TestRepo.Src("Cobol.Net.Compiler", "Validation", "VersionConformancePass.cs")));
        var body = Regex.Match(src, @"private\s+void\s+GateMove\s*\(\s*BoundMove\s+m\s*\)(?<b>.*?)\n    \}",
            RegexOptions.Singleline);
        Assert.True(body.Success, "GateMove(BoundMove m) is no longer recognizable in VersionConformancePass");
        Assert.Contains("m.Source", body.Groups["b"].Value);
        Assert.DoesNotContain("Stores", body.Groups["b"].Value);
    }

    /// <summary>Comment-stripped source, so a mechanism DISCUSSED in prose (these files discuss this one at
    /// length) is never mistaken for one performed in code.</summary>
    private static string StripComments(string src) =>
        Regex.Replace(Regex.Replace(src, @"/\*.*?\*/", "", RegexOptions.Singleline), @"//[^\n]*", "");
}
