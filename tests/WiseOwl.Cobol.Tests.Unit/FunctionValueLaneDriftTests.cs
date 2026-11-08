// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Procedure;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE ONE LIST THE BIND-TIME LANE PREDICTION RESTATES IS THE RENDERER'S OWN (kb/Work PB3268, review R1-1).
/// <c>FunctionValueLane.ReturnsBinary64</c> answers, before the code generator has rendered anything, whether a
/// numeric function's value arrives as a binary64 — the fact that decides whether the §2023:14.9.25.4 GR1 intermediate
/// result item is a COMP-2 or a fixed-point item. It restates <c>IntrinsicRenderer.RenderNum</c>'s dispatch, and the
/// ONE piece of it that is a list rather than a rule is the set of functions that have an SDIDI body under NATIVE
/// arithmetic (<c>RenderDec</c>'s switch): given an SDIDI-carried argument (a floating-point literal), those functions
/// compute on the SDIDI and leave the float lane. A body added to the renderer's switch without its name in
/// <see cref="FunctionValueLane.NativeSdidiBodies"/> would send that function's float-literal call to a COMP-2
/// intermediate that rounds its SDIDI value, which is the defect this file holds shut.
/// </summary>
/// <remarks>The matrix that holds the whole prediction against the renderer's actual answer is the
/// <c>pb3268_function_value_lane_matrix</c> golden (a multi-receiver MOVE stores what the single-receiver MOVE stores,
/// over function x argument shape x receiver set); this test holds the list that golden cannot enumerate by itself.</remarks>
[Trait("Sweep", "tree")]
public sealed class FunctionValueLaneDriftTests
{
    /// <summary>A <c>"Name" =></c> or <c>"Name" when … =></c> arm of the <c>RenderDec</c> switch, and the text of its
    /// value expression that follows on the same line (empty when the expression starts on the next one).</summary>
    private static readonly Regex Arm = new(
        "^\\s*\"(?<name>\\w+)\"(?<guard>\\s+when\\s+[^=]+?)?\\s*=>\\s*(?<body>.*)$", RegexOptions.Compiled);

    /// <summary>How a <c>RenderDec</c> arm builds its value: the switch's local <c>Dec(...)</c> (an SDIDI value), its local
    /// <c>Exact(...)</c> (a run-time-scaled <c>NumX.Exact</c> its consumer lowers to the SDIDI), or <c>new NumX(...)</c> (an exact
    /// scale-0 value). A name with several arms of different kinds resolves to the later kind in declaration order.</summary>
    private enum ArmKind { Sdidi, Whole, Scalar }

    /// <summary>The native arms of <c>RenderDec</c>'s switch (the standard-decimal-only arms excluded), each with how its value is built.</summary>
    private static SortedDictionary<string, ArmKind> NativeRenderDecArms()
    {
        string source = File.ReadAllText(Path.Combine(
            TestRepo.Src("WiseOwl.Cobol.Compiler"), "CodeGen", "Emit", "IntrinsicRenderer.cs"));
        int start = source.IndexOf("private NumX? RenderDec(BoundIntrinsicCall ic)", StringComparison.Ordinal);
        int end = source.IndexOf("static NumX Dec(string expr)", start, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start, "RenderDec's body could not be located in IntrinsicRenderer.cs");

        var native = new SortedDictionary<string, ArmKind>(StringComparer.Ordinal);
        string[] lines = source[start..end].Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var m = Arm.Match(lines[i].TrimEnd('\r'));
            if (!m.Success) continue;
            // `"Sqrt" when num.StandardDecimal` / `"Factorial" when num.StandardDecimal` are standard-mode arms: a NATIVE
            // call to either keeps the float / exact lane (IntrinsicRenderer: "an unguarded arm would flip native float
            // SQRT onto the Dec lane"). A `when !num.StandardDecimal` arm is the NATIVE one and stays (kb/Work PB4730: the native
            // selections return their exact value, not an SDIDI).
            string guard = m.Groups["guard"].Value;
            if (guard.Contains("num.StandardDecimal", StringComparison.Ordinal) && !guard.Contains("!num.StandardDecimal", StringComparison.Ordinal)) continue;
            string body = m.Groups["body"].Value.Trim();
            if (body.Length == 0 && i + 1 < lines.Length) body = lines[i + 1].Trim();
            ArmKind kind = body.StartsWith("Dec(", StringComparison.Ordinal) ? ArmKind.Sdidi
                : body.StartsWith("Exact(", StringComparison.Ordinal) ? ArmKind.Whole
                : ArmKind.Scalar;
            // A name with several arms (NUMVAL-C's LOCALE arm and its plain one) is SDIDI only when every arm is.
            native[m.Groups["name"].Value] = native.TryGetValue(m.Groups["name"].Value, out ArmKind prior)
                ? (ArmKind)Math.Max((int)prior, (int)kind) : kind;
        }
        Assert.True(native.Count > 10, "the RenderDec arm scan found suspiciously few arms: " + string.Join(", ", native.Keys));
        return native;
    }

    /// <summary>⛔ THE PREDICTION AND THE RENDERER SHARE THEIR RULES THROUGH compiler.bound, NEVER THROUGH EACH OTHER
    /// (kb/Work PB3268, review R1-2; DESIGN-architecture-review.md §8.1: compiler.codegen reaches compiler.bound and
    /// compiler.model, compiler.binding never reaches compiler.codegen, and no tolerated row covers either direction
    /// between <c>WiseOwl.Cobol.Binding.Procedure</c> and <c>WiseOwl.Cobol.CodeGen.Emit</c>). The lane once imported
    /// <c>IntrinsicRenderer.CrossAlignedNativeArms</c> and <c>EmitText.UnscaledLit</c> while the renderer imported the
    /// lane's <c>ReturnsExactParse</c> and <c>AlignmentMayLeaveCarrier</c>: a namespace cycle between the binder and
    /// the code generator. The shared rules now live in <see cref="IntrinsicCarrierRules"/> and
    /// <c>NumericLiteral.TryUnscaledParts</c>; this holds every file of both namespaces to it.</summary>
    [Fact]
    public void BindingProcedure_AndCodeGenEmit_DoNotReachEachOther()
    {
        string compiler = TestRepo.Src("WiseOwl.Cobol.Compiler");
        string obj = $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}";
        var offenders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(compiler, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains(obj, StringComparison.Ordinal)) continue;
            string text = File.ReadAllText(file);
            string? forbidden =
                text.Contains("namespace WiseOwl.Cobol.Binding.Procedure;", StringComparison.Ordinal) ? "WiseOwl.Cobol.CodeGen.Emit"
                : text.Contains("namespace WiseOwl.Cobol.CodeGen.Emit;", StringComparison.Ordinal) ? "WiseOwl.Cobol.Binding.Procedure"
                : null;
            if (forbidden is null) continue;
            // A comment may name the other layer's member (the cref-free prose that says which rule it restates).
            bool named = text.Split('\n').Any(l =>
                !l.TrimStart().StartsWith("//", StringComparison.Ordinal) && l.Contains(forbidden, StringComparison.Ordinal));
            if (named) offenders.Add($"{Path.GetRelativePath(compiler, file)} names {forbidden}");
        }
        Assert.True(offenders.Count == 0, "a cross-layer edge DESIGN-architecture-review §8.1 does not allow: " + string.Join("; ", offenders));
        // The shared rules depend on nothing above compiler.bound.
        string rules = File.ReadAllText(Path.Combine(compiler, "Binding", "Bound", "IntrinsicCarrierRules.cs"));
        Assert.DoesNotContain("using WiseOwl.Cobol.CodeGen", rules, StringComparison.Ordinal);
        Assert.DoesNotContain("using WiseOwl.Cobol.Binding.Procedure", rules, StringComparison.Ordinal);
        Assert.DoesNotContain("using WiseOwl.Cobol.Binding;", rules, StringComparison.Ordinal);
    }

    /// <summary>The other function sets the prediction shares with the renderer
    /// (<see cref="IntrinsicCarrierRules.CrossAligningFunctions"/>, <see cref="IntrinsicCarrierRules.ReturnsExactParse"/>,
    /// <see cref="IntrinsicCarrierRules.SdidiQuotientFunctions"/>) route a call
    /// to its SDIDI body, so each member's runtime body needs one in <see cref="FunctionValueLane.NativeSdidiBodies"/> - else
    /// the prediction would name a lane the renderer cannot take (kb/Work PB3268, review R2-1). The sets name functions and
    /// the bodies are runtime methods, so each member meets its body through its catalog row (kb/Work PB4370).</summary>
    [Fact]
    public void TheSdidiRoutedSets_AreSubsetsOfNativeSdidiBodies()
    {
        var crossAligned = IntrinsicCarrierRules.CrossAligningFunctions;
        Assert.True(crossAligned.Count > 0);
        foreach (string fn in crossAligned)
            Assert.True(FunctionValueLane.NativeSdidiBodies.Contains(IntrinsicCarrierRuleKeyDriftTests.RuntimeMethodOf(fn)),
                $"CrossAligningFunctions names FUNCTION {fn}, which has no native SDIDI body in NativeSdidiBodies");
        foreach (string fn in new[] { "NUMVAL", "NUMVAL-C", "NUMVAL-F" })
        {
            Assert.True(IntrinsicCarrierRules.ReturnsExactParse(fn), $"FUNCTION {fn} returns the exact parse of its argument (§2023:15.67.4 r1 / §2023:15.68.4 r1 by definition, §2023:15.69.4 r2/r3 by the PB3839 determination)");
            Assert.True(FunctionValueLane.NativeSdidiBodies.Contains(IntrinsicCarrierRuleKeyDriftTests.RuntimeMethodOf(fn)),
                $"FUNCTION {fn} has no native SDIDI body in NativeSdidiBodies");
        }
        Assert.True(IntrinsicCarrierRules.SdidiQuotientFunctions.Count > 0);
        foreach (string fn in IntrinsicCarrierRules.SdidiQuotientFunctions)
            Assert.True(FunctionValueLane.NativeSdidiBodies.Contains(IntrinsicCarrierRuleKeyDriftTests.RuntimeMethodOf(fn)),
                $"SdidiQuotientFunctions names FUNCTION {fn}, which has no native SDIDI body in NativeSdidiBodies (kb/Work PB3860)");
    }

    [Fact]
    public void NativeSdidiBodies_AreTheArmsOfRenderDec_ThatAreNotStandardDecimalOnly()
    {
        var native = new SortedSet<string>(NativeRenderDecArms().Keys, StringComparer.Ordinal);
        var restated = new SortedSet<string>(FunctionValueLane.NativeSdidiBodies, StringComparer.Ordinal);
        Assert.True(native.SetEquals(restated),
            "FunctionValueLane.NativeSdidiBodies must be exactly RenderDec's native arms (kb/Work PB3268).\n"
            + "  in RenderDec only: [" + string.Join(", ", native.Except(restated)) + "]\n"
            + "  in NativeSdidiBodies only: [" + string.Join(", ", restated.Except(native)) + "]");
    }

    /// <summary>The native SDIDI bodies whose value the renderer returns as an EXACT scale-0 value (SIGN, ORD-MAX, ORD-MIN:
    /// built by <c>new NumX(...)</c>) are exactly <see cref="FunctionValueLane.NativeSdidiBodiesWithExactResult"/>, and those it
    /// returns as a run-time-scaled <c>Exact(...)</c> value its consumer lowers to the SDIDI (SUM, RANGE, MEAN, MIDRANGE, MAX, MIN,
    /// MEDIAN) are exactly <see cref="FunctionValueLane.NativeSdidiBodiesWithWholeResult"/> — so a nested call the lane prediction
    /// routes onto an SDIDI body is handed to its caller on the carrier the renderer's arm really returns (kb/Work PB3268, review R3-1:
    /// an INTEGER function is no longer exempted wholesale; kb/Work PB4980: MAX / MIN / MEDIAN were predicted as scale-0 values, and the
    /// function above them took the float lane in a multi-receiver MOVE where the single-receiver MOVE computed it on the SDIDI).</summary>
    [Fact]
    public void NativeSdidiBodiesWithExactResult_AreTheRenderDecArmsNotBuiltByDec()
    {
        var arms = NativeRenderDecArms();
        foreach ((ArmKind kind, string name, IReadOnlySet<string> restated) in new[]
        {
            (ArmKind.Scalar, "NativeSdidiBodiesWithExactResult", (IReadOnlySet<string>)FunctionValueLane.NativeSdidiBodiesWithExactResult),
            (ArmKind.Whole, "NativeSdidiBodiesWithWholeResult", FunctionValueLane.NativeSdidiBodiesWithWholeResult),
        })
        {
            var built = new SortedSet<string>(arms.Where(a => a.Value == kind).Select(a => a.Key), StringComparer.Ordinal);
            Assert.NotEmpty(built);
            Assert.True(built.SetEquals(restated),
                $"FunctionValueLane.{name} must be exactly RenderDec's native arms built as {kind}.\n"
                + "  in RenderDec only: [" + string.Join(", ", built.Except(restated)) + "]\n"
                + $"  in {name} only: [" + string.Join(", ", restated.Except(built)) + "]");
        }
    }

    /// <summary>⛔ A FUNCTION'S BINARY64 VALUE IS AN APPROXIMATION OR A FLOAT ITEM'S CONTENT BY ONE PREDICATE, ASKED BY THE BINDER AND BY
    /// THE RENDERER (kb/Work PB4622): the floating-math family and a function of approximations only return an approximation, which
    /// lands by its shortest round-trip decimal; ABS, MAX, FRACTION-PART, SUM ... over a float ITEM return a value derived from the
    /// item's exact binary64 (§2023:15.59.4 1) "the content of the argument"), which lands as the item does.</summary>
    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, true)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    public void Binary64ResultIsApproximation_IsTheFloatFamilyOrAFunctionOfApproximationsOnly(
        bool floatFamily, bool anyFloatItemArgument, bool approximation) =>
        Assert.Equal(approximation, IntrinsicCarrierRules.Binary64ResultIsApproximation(floatFamily, anyFloatItemArgument));

    [Fact]
    public void BinderAndRenderer_AskTheSameApproximationPredicate()
    {
        string lane = File.ReadAllText(Path.Combine(TestRepo.Src("WiseOwl.Cobol.Compiler"), "Binding", "Procedure", "FunctionValueLane.cs"));
        string renderer = File.ReadAllText(Path.Combine(TestRepo.Src("WiseOwl.Cobol.Compiler"), "CodeGen", "Emit", "IntrinsicRenderer.cs"));
        Assert.Contains("IntrinsicCarrierRules.Binary64ResultIsApproximation(", lane, StringComparison.Ordinal);
        Assert.Contains("IntrinsicCarrierRules.Binary64ResultIsApproximation(", renderer, StringComparison.Ordinal);
        // No other site decides it: the renderer flags a binary64 Approximate only from the predicate's answer.
        Assert.DoesNotContain("Real: true, Approximate: true);", renderer, StringComparison.Ordinal);
    }
}
