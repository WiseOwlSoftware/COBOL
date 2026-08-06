// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// Holds the TWO string-image channels for a <c>BoundOperand</c> in their intended relationship, leaf by leaf
/// (fix-queue PB49).
/// </summary>
/// <remarks>
/// <para>
/// ⛔ WHY THIS EXISTS. <c>OperandText.AsString</c> (the DISPLAY / MOVE / STRING channel) and
/// <c>IntrinsicRenderer.StrArgVisitor</c> (the intrinsic-argument channel) are two visitors over the same leaf
/// set. <c>StrArgVisitor</c> delegates only SOME leaves and answers the rest itself, so it inherited neither
/// <c>AsString</c>'s figurative arm nor its ALL-literal arm — and <c>FUNCTION LOWER-CASE(SPACE)</c>, legal
/// source, compiled clean and aborted at RUN TIME with "intrinsic string argument 'BoundFigurative'"
/// (fix-queue PB25). That was ONE RULE WRITTEN IN TWO PLACES with only one copy right. PB25 routed the two
/// offending leaves to <c>AsString</c>; the guard its own evidence file prescribed was never built, and this
/// is it.
/// </para>
/// <para>
/// ⚠ <b>THE TWO CHANNELS ARE NOT REQUIRED TO AGREE, AND A TEST ASSERTING THAT WOULD BE WRONG.</b> A numeric
/// literal is a legal DISPLAY operand and NOT a legal string-argument operand; the computed-operand arms differ
/// because the intrinsic channel re-enters <c>RenderString</c> for a nested string-class function where the
/// DISPLAY channel has a loud stage. So this encodes a PER-LEAF EXPECTATION with the intended answer stated,
/// exactly as PB49 required — never a blanket "these two functions are equal".
/// </para>
/// <para>
/// ⚙ <b>AND PB49's SECOND QUESTION HAS AN ANSWER RATHER THAN A GUARD.</b> It asked for the same check on the
/// NUMERIC channels. Measured: the whole compiler has four <c>IBoundOperandVisitor</c> implementations —
/// <c>StrArgVisitor</c> and <c>AsStringVisitor</c> (string), <c>IsStringVisitor</c> (a bool classifier), and
/// <c>NumericRenderer</c>. <b>NumericRenderer is the ONLY numeric one</b>; <c>OperandText.NumericIntrinsicText</c>
/// is a helper it is called from, not a parallel visitor. There is no numeric twin to drift from, so there is
/// nothing to guard — asserted below so that stops being true loudly rather than silently.
/// </para>
/// </remarks>
public sealed class OperandStringChannelDriftTests
{
    private static string IntrinsicRendererSource() =>
        File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Emit", "IntrinsicRenderer.cs"));

    private static string OperandTextSource() =>
        File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Emit", "OperandText.cs"));

    /// <summary>The body of <c>StrArgVisitor</c> — the intrinsic-argument string channel.</summary>
    private static string StrArgVisitorBody()
    {
        string src = IntrinsicRendererSource();
        int at = src.IndexOf("private sealed class StrArgVisitor", StringComparison.Ordinal);
        Assert.True(at > 0, "StrArgVisitor is gone from IntrinsicRenderer — the intrinsic-argument string "
            + "channel was restructured; re-point this guard at whatever replaced it.");
        return src[at..];
    }

    /// <summary>The leaf arms that MUST route to <c>OperandText.AsString</c> — the PB25 fix, and the exact
    /// thing that rots if someone re-implements a leaf locally "to avoid the dependency".</summary>
    [Theory]
    [InlineData("BoundFieldOperand", "a field's display image is width- and category-sensitive; the local copy "
        + "would have to re-derive DE-EDITING, the sign convention and the float check")]
    [InlineData("BoundFigurative", "§8.3.3.6.4 GR3's PCS-aware materialisation — the arm PB25 added, and the one "
        + "whose absence aborted FUNCTION LOWER-CASE(SPACE) at run time")]
    [InlineData("BoundAllLiteral", "§8.3.3.6.4 GR3c — ALL literal-1 is the literal ONCE in a length-unspecified "
        + "context, which a local copy has previously got wrong")]
    public void StrArgVisitor_DelegatesTheSharedLeaves_ToOperandTextAsString(string leaf, string why)
    {
        string body = StrArgVisitorBody();
        var m = Regex.Match(body, $@"public string Visit\({leaf} n\)\s*=>(?<rhs>[^;]*);");
        Assert.True(m.Success, $"StrArgVisitor no longer has a Visit({leaf}) arm — the generated visitor is "
            + "exhaustive, so this means the LEAF was renamed or removed; update this guard deliberately.");
        Assert.True(m.Groups["rhs"].Value.Contains("OperandText.AsString", StringComparison.Ordinal),
            $"StrArgVisitor.Visit({leaf}) no longer delegates to OperandText.AsString. {why}. This is the PB25 "
            + "shape: one rule written in two places, and the second copy silently wrong — it shipped as a "
            + "clean compile that aborted at RUN TIME on legal source.");
    }

    /// <summary>
    /// The leaves the two channels are meant to answer DIFFERENTLY, asserted so nobody "fixes" them into
    /// agreement. Making <c>BoundNumericLiteral</c> match the DISPLAY channel would silently admit a numeric
    /// literal into a string-argument position — the inverse of PB25 and just as quiet.
    /// </summary>
    [Fact]
    public void StrArgVisitor_KeepsTheDeliberatelyDifferentLeaves_Loud()
    {
        string body = StrArgVisitorBody();
        foreach ((string leaf, string why) in new[]
                 {
                     ("BoundNumericLiteral", "a numeric literal is a legal DISPLAY operand and NOT a legal "
                         + "string-argument operand — §15's string functions take alphanumeric/national/boolean"),
                     ("BoundBoolOperand", "a class-boolean operand does not cross the string-argument channel"),
                 })
        {
            var m = Regex.Match(body, $@"public string Visit\({leaf} n\)\s*=>(?<rhs>[^;]*);");
            Assert.True(m.Success, $"StrArgVisitor lost its Visit({leaf}) arm");
            Assert.True(m.Groups["rhs"].Value.Contains("Loud", StringComparison.Ordinal),
                $"StrArgVisitor.Visit({leaf}) is no longer a loud stage. {why}. If this became a delegation to "
                + "OperandText.AsString the channel would start ACCEPTING an operand §15.3 excludes, with no "
                + "diagnostic — silent over-acceptance, the inverse of the PB25 defect this file guards.");
        }
    }

    /// <summary>Both string channels cover the SAME leaf set. The generated visitor interface already forces
    /// this at compile time; asserting it here is what makes the per-leaf table above provably COMPLETE — a new
    /// <c>BoundOperand</c> leaf must be classified as shared-or-divergent, not quietly added to one side.</summary>
    [Fact]
    public void BothStringChannels_CoverTheSameLeafSet_AndTheTableAccountsForEveryOne()
    {
        static HashSet<string> Leaves(string body) =>
            [.. Regex.Matches(body, @"public string Visit\((?<t>Bound\w+) n\)").Select(m => m.Groups["t"].Value)];

        string opText = OperandTextSource();
        int at = opText.IndexOf("private sealed class AsStringVisitor", StringComparison.Ordinal);
        Assert.True(at > 0, "AsStringVisitor is gone from OperandText");

        var strArg = Leaves(StrArgVisitorBody());
        var display = Leaves(opText[at..]);
        Assert.True(strArg.SetEquals(display),
            $"the two string channels no longer cover the same leaves — only in StrArgVisitor: "
            + $"[{string.Join(", ", strArg.Except(display).Order())}]; only in AsStringVisitor: "
            + $"[{string.Join(", ", display.Except(strArg).Order())}]");

        // Every leaf is either DELEGATED (the shared rule) or DIVERGENT BY DESIGN. A leaf in neither bucket is
        // one nobody has decided about, which is how PB25 shipped.
        string[] delegated = ["BoundFieldOperand", "BoundFigurative", "BoundAllLiteral"];
        string[] divergent = ["BoundNumericLiteral", "BoundBoolOperand", "BoundComputedOperand",
                              "BoundStringLiteral", "BoundOperandError"];
        var unclassified = strArg.Except(delegated).Except(divergent).Order().ToList();
        Assert.True(unclassified.Count == 0,
            $"BoundOperand leaf/leaves [{string.Join(", ", unclassified)}] are handled by both string channels "
            + "but classified by neither bucket in this guard. Decide explicitly: does the intrinsic-argument "
            + "channel DELEGATE to OperandText.AsString (a shared rule), or answer differently on purpose? "
            + "Leaving it undecided is exactly how PB25's figurative arm went missing.");
    }

    /// <summary>PB49's second question, answered as a FACT: there is no second NUMERIC operand channel, so there
    /// is no numeric drift to guard. If a second one appears, this fails and the per-leaf treatment above has to
    /// be repeated for it.</summary>
    [Fact]
    public void ThereIsExactlyOneNumericOperandChannel()
    {
        var impls = new List<string>();
        foreach (string f in Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Compiler"), "*.cs",
                     SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(TestRepo.Src("Cobol.Net.Compiler"), f).Replace('\\', '/');
            if (rel.StartsWith("obj/", StringComparison.Ordinal) || rel.StartsWith("bin/", StringComparison.Ordinal))
                continue;
            foreach (Match m in Regex.Matches(File.ReadAllText(f),
                         @"class (?<c>\w+)[^\n]*IBoundOperandVisitor<(?<t>[^>]+)>"))
                if (m.Groups["t"].Value.Trim() is "NumX")
                    impls.Add($"{m.Groups["c"].Value} ({rel})");
        }
        Assert.True(impls.Count == 1,
            $"expected exactly ONE numeric BoundOperand visitor (NumericRenderer) and found {impls.Count}: "
            + $"[{string.Join(", ", impls)}]. PB49 asked whether the numeric channels could drift the way the "
            + "string ones did; the answer was 'there is only one'. A second means that answer has expired and "
            + "the per-leaf agreement table above must be repeated for the numeric pair.");
    }
}
