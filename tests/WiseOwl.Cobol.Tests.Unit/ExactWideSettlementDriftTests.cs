// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ AN EXACT WIDE INTERMEDIATE (<c>NumX.Wide</c>, kb/Work PB1900) NEVER LEAVES <c>NumericRenderer</c> UNSETTLED, AND THE LIST OF
/// CONSUMERS THAT TAKE IT EXACT IS CLOSED.
/// <para>A nested native product past the Int128 carrier is carried as a <c>CobolWide</c> so a cancellation
/// (<c>A * B - C * D</c> over 21-digit operands) is exact. The form is NOT one of the <c>NumXCarrier</c>s the ~40 consumers
/// of a rendered <c>NumX</c> dispatch on (<c>x.Dec</c> / <c>x.Real</c> / <c>x.U</c>): a consumer handed one would read its
/// <c>CobolWide</c> text as an <c>Int128</c>. So every public entry of the renderer SETTLES it
/// (<c>NumericRenderer.Settle</c>: into the receiver's own scale and mode at the final transfer, else into the SDIDI), and a
/// consumer may take it exact only by passing <c>keepWide: true</c> — which it then owes a settlement of its own. This class
/// pins both halves: the entries all settle, and the opt-in sites are the ones that do (a several-receiver COMPUTE's one
/// initial evaluation, the DIVIDE ... REMAINDER back-multiply that the subtraction around it settles, kb/Work PB3538, a
/// relation, an intrinsic function's argument and the temporary DISPLAY shows, kb/Work PB3953, and a MOVE's store of a function
/// whose value is wide). Another site is a design decision (add its settlement here), never a silent edit.</para>
/// <para>⛔ AND THE FUNCTION FAMILY THAT TAKES IT IS CLOSED THE OTHER WAY ROUND (kb/Work PB3953): every arm of the exact
/// intrinsic switch whose intake is a fraction-bearing numeric argument has a wide body in <c>IntrinsicRenderer.RenderWide</c>, or is
/// named in <see cref="ImageTakers"/> with the reason its value cannot depend on digits past the SDIDI's 34. A new exact arm is then
/// a red here until it decides, never a function that quietly answers from a round-to-odd image.</para>
/// </summary>
public sealed class ExactWideSettlementDriftTests
{
    private static readonly string[] OptInSites =
        ["ArithmeticEmitter.cs", "ConditionRenderer.cs", "IntrinsicRenderer.cs", "FunctionDeviceText.cs", "MoveEmitter.cs", "OperandText.cs"];

    /// <summary>The exact-switch arms that take an argument expression past the <c>Int128</c> carrier as its SDIDI image, each for the
    /// reason no digit past the image's 34 can change its value.</summary>
    private static readonly Dictionary<string, string> ImageTakers = new(StringComparer.Ordinal)
    {
        // §15.17.3 r2: argument-2 is in standard numeric time form (§15.5.5, a magnitude below 86 401), so its image keeps 29
        // fraction digits, and the returned value is formed on the SDIDI (CombinedDatetimeDec) whatever carried the argument.
        ["CombinedDatetime"] = "argument-2 is bounded below 86401 and the value is formed on the SDIDI",
    };

    private static string Renderer() =>
        File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "Emit", "NumericRenderer.cs"));

    private static string BodyOf(string src, string signaturePrefix)
    {
        int at = src.IndexOf(signaturePrefix, StringComparison.Ordinal);
        Assert.True(at >= 0, $"NumericRenderer no longer declares '{signaturePrefix}'");
        int open = src.IndexOf('{', at);
        int depth = 0, i = open;
        for (; i < src.Length; i++)
        {
            if (src[i] == '{') depth++;
            else if (src[i] == '}' && --depth == 0) break;
        }
        return src[open..(i + 1)];
    }

    /// <summary>Every public entry that hands a rendered value to a caller settles it unless the caller asked to keep it.</summary>
    [Fact]
    public void EveryPublicRenderEntry_SettlesTheWideForm()
    {
        string src = Renderer();
        foreach (string entry in new[] { "public NumX Render(BoundExpr e", "public NumX AsNum(BoundOperand op" })
            Assert.Contains("keepWide ? x : Settle(x, rcv, outermost)", BodyOf(src, entry));
        Assert.Contains("return Settle(acc, rcv, outermost: false);", BodyOf(src, "public NumX Fold("));
        Assert.Contains("return keepWide ? x : Settle(x, rcv, outermost);", BodyOf(src, "public NumX Combine("));
    }

    /// <summary>The consumers that opt in to the exact form are exactly the two that settle it themselves.</summary>
    [Fact]
    public void OnlyTheRegisteredSettlingSites_KeepTheWideForm()
    {
        var offenders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen"), "*.cs", SearchOption.AllDirectories))
        {
            string name = Path.GetFileName(file);
            if (name == "NumericRenderer.cs") continue;                       // the declaration itself
            string text = File.ReadAllText(file);
            if (Regex.IsMatch(text, @"keepWide\s*:\s*true") && !OptInSites.Contains(name)) offenders.Add(name);
        }
        Assert.True(offenders.Count == 0,
            "These files ask for an UNSETTLED exact wide intermediate (keepWide: true) without being a registered settling site "
            + "(ExactWideSettlementDriftTests.OptInSites): " + string.Join(", ", offenders));
    }

    /// <summary>The opt-in sites really do settle: the guard above would pass vacuously if they stopped.</summary>
    [Fact]
    public void TheOptInSites_SettleWhatTheyKeep()
    {
        string arithmetic = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "Verbs", "ArithmeticEmitter.cs"));
        Assert.Contains("keepWide: true", arithmetic);
        Assert.Contains("num.Settle(v, RcvFor(r, ise), outermost: true)", arithmetic);
        // The REMAINDER back-multiply is kept exact only as the operand of the Combine that settles the remainder.
        Assert.Contains("num.Combine(dividend, \"-\",", arithmetic);
        Assert.Contains("num.Combine(subsidiary, \"*\", divisor, rcv, keepWide: true), rcv)", arithmetic);
        string condition = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "Emit", "ConditionRenderer.cs"));
        Assert.Contains("keepWide: true", condition);
        Assert.Contains("NumericRenderer.CompareWide(l, rr)", condition);
        Assert.Contains("NumericRenderer.LowerWide(l)", condition);
        // An intrinsic argument (kb/Work PB3953): the one keeping intake, lowered for every consumer but the wide bodies, whose
        // results are NumX.Wide values the renderer settles at its public entry.
        string intrinsic = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "Emit", "IntrinsicRenderer.cs"));
        Assert.Single(Regex.Matches(intrinsic, @"keepWide\s*:\s*true"));
        Assert.Contains("NumX x = num.AsNum(ic.Args[i], num.Receiver, keepWide: true);", BodyOf(intrinsic, "private NumX RawWideArg("));
        Assert.Contains("private NumX RawArg(BoundIntrinsicCall ic, int i) => NumericRenderer.LowerWide(RawWideArg(ic, i));", intrinsic);
        // The temporary DISPLAY shows: every kept value settles into a wide text or a wide field.
        string device = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "Emit", "FunctionDeviceText.cs"));
        Assert.Contains("if (x.Wide) return RuntimeApi.FunctionDeviceTextWide(x.Expr, x.Scale, Comma);", device);
        Assert.Contains(": x.Wide ? RuntimeApi.FunctionFieldOfWide(x.Expr, x.Scale)", device);
        Assert.Contains("? RuntimeApi.FunctionFieldOfWide(value.Expr, value.Scale)", device);
        // A MOVE's store: transferred at the receiver's (or the mask's) scale, or lowered for a floating-point mask.
        string move = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "Verbs", "MoveEmitter.cs"));
        Assert.Equal(2, Regex.Matches(move, @"keepWide\s*:\s*true").Count);
        Assert.Contains("n = NumericRenderer.TransferWide(n, recvScaleM, rounding, checkedTransfer: false);", move);
        Assert.Contains("e = NumericRenderer.TransferWide(e, ems, rounding, checkedTransfer: false);", move);
        Assert.Contains("NumericRenderer.LowerWide(e)", move);
        // The string-image channel of a numeric function's value: the kept value settles into the text of its every digit.
        string text = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "Emit", "OperandText.cs"));
        Assert.Single(Regex.Matches(text, @"keepWide\s*:\s*true"));
        Assert.Contains(": x.Wide ? RuntimeApi.WideFunctionText(x.Expr, x.Scale, deSign, comma)", text);
    }

    /// <summary>The exact-switch arms of <c>IntrinsicRenderer.RenderNum</c> (the first <c>switch (sig.RuntimeMethod)</c>), each label
    /// with the text of its arm, read up to the switch's <c>default:</c>.</summary>
    private static List<(string Label, string Body)> ExactArms(string src)
    {
        int from = src.IndexOf("switch (sig.RuntimeMethod)", StringComparison.Ordinal);
        Assert.True(from > 0, "IntrinsicRenderer.RenderNum no longer switches on sig.RuntimeMethod — this guard is blind");
        int end = src.IndexOf("default:", from, StringComparison.Ordinal);
        string body = Regex.Replace(src[from..end], @"//[^\n]*", "");
        var arms = new List<(string, string)>();
        var labels = Regex.Matches(body, @"case\s+(?<l>""[^:]*?""(?:\s+or\s+""[^""]+"")*)\s*(?:when[^:]*)?:");
        for (int i = 0; i < labels.Count; i++)
        {
            int start = labels[i].Index + labels[i].Length;
            int stop = i + 1 < labels.Count ? labels[i + 1].Index : body.Length;
            foreach (Match name in Regex.Matches(labels[i].Groups["l"].Value, @"""(?<n>\w+)"""))
                arms.Add((name.Groups["n"].Value, body[start..stop]));
        }
        return arms;
    }

    /// <summary>⛔ EVERY EXACT-FAMILY ARM THAT TAKES A FRACTION-BEARING NUMERIC ARGUMENT HAS A WIDE BODY, OR SAYS WHY ITS IMAGE IS ITS
    /// VALUE (kb/Work PB3953). A stacked label pair shares the body after it (an empty arm is judged by the next one's text).</summary>
    [Fact]
    public void EveryExactArmTakingANumericArgument_HasAWideBody()
    {
        string src = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "Emit", "IntrinsicRenderer.cs"));
        var arms = ExactArms(src);
        var fractionBearing = new Regex(@"\b(?:Arg|RawArg)\(ic,\s*\d+\)|\bAlignedArgs\(|\bRawArgPairs\(");
        var numeric = new List<string>();
        for (int i = 0; i < arms.Count; i++)
        {
            string body = arms[i].Body;
            for (int j = i + 1; body.Trim().Length == 0 && j < arms.Count; j++) body = arms[j].Body;
            if (fractionBearing.IsMatch(body)) numeric.Add(arms[i].Label);
        }
        Assert.True(numeric.Count >= 15, "only " + numeric.Count + " numeric exact arms found (" + string.Join(", ", numeric) + ") — the scan is blind");
        Assert.Contains("AbsScaled", numeric);

        string wide = BodyOf(src, "private NumX? RenderWide(");
        var wideLabels = Regex.Matches(wide, @"""(?<n>\w+)""\s*=>").Select(m => m.Groups["n"].Value).ToHashSet(StringComparer.Ordinal);
        var missing = numeric.Distinct().Where(n => !wideLabels.Contains(n) && !ImageTakers.ContainsKey(n)).ToList();
        Assert.True(missing.Count == 0,
            "these exact-switch arms take a numeric argument but have no wide body in IntrinsicRenderer.RenderWide, so an argument "
            + "expression past the Int128 carrier reaches them as its 34-digit round-to-odd image (kb/Work PB3953: ABS (BIG * BIG * K3) "
            + "showed …100000 for …000999). Give each a …Wide body, or name it in ImageTakers with the reason its value cannot depend on "
            + "the lost digits: " + string.Join(", ", missing));
        var stale = ImageTakers.Keys.Where(n => !numeric.Contains(n) || wideLabels.Contains(n)).ToList();
        Assert.True(stale.Count == 0, "ImageTakers names arms that no longer take the image: " + string.Join(", ", stale));
    }

    /// <summary>The wide form is not a carrier: no consumer may dispatch on it, so <c>NumX.Carrier</c> refuses it.</summary>
    [Fact]
    public void TheWideForm_IsNotACarrier()
    {
        string core = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "Emit", "EmitCore.cs"));
        Assert.Matches(@"Carrier\s*=>\s*Wide\s*\?\s*throw", core);
        // The enum is internal to the compiler, so its members are read from source.
        Match members = Regex.Match(core, @"enum\s+NumXCarrier\s*\{(?<m>[^}]*)\}");
        Assert.True(members.Success, "NumXCarrier is no longer declared in EmitCore.cs");
        Assert.DoesNotContain("Wide", members.Groups["m"].Value.Replace("UnsignedWide", ""));
    }
}
