// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A DIVIDE QUOTIENT IS FORMED BY ONE METHOD, AND THE SUBSIDIARY QUOTIENT HAS ONE ARM PER FORM OF IDENTIFIER-3
/// (kb/Work PB2923). ISO §14.9.12.4 5) b) and 6) b) both say "The quotient is then formed as specified in General rule 1
/// and stored in identifier-3 as specified in 14.7.7": <c>DIVIDE ... GIVING</c> and <c>DIVIDE ... GIVING ... REMAINDER</c>
/// store the same quotient, and 6) c) only derives the subsidiary quotient FROM it. The REMAINDER arm used to form the
/// quotient a second way, truncated at the receiver's fixed scale, so a floating-point identifier-3 (which has none) held a
/// different value with the phrase than without it (<c>DIVIDE 1 BY 3000 GIVING FE REMAINDER R</c> stored +3.30E-04 where the
/// statement without REMAINDER stored +3.33E-04).
/// <para>This reads <c>ArithmeticEmitter</c>: the division is rendered through <c>FinalQuotient</c> alone, both statements
/// call it, and every <c>SubsidiaryForm</c> member has its arm in the carried subsidiary-quotient switch - so a new form of
/// identifier-3 is a red test until its shape is written down, never a silent fall into the fixed-point arm.</para>
/// </summary>
public sealed class DivideQuotientOneFormerDriftTests
{
    private static string[] Code() =>
        File.ReadAllLines(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "Verbs", "ArithmeticEmitter.cs"))
            .Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal)).ToArray();

    private static string Between(string text, string from, string to)
    {
        int a = text.IndexOf(from, StringComparison.Ordinal);
        Assert.True(a >= 0, $"ArithmeticEmitter no longer contains '{from}' - re-point this guard");
        int b = text.IndexOf(to, a, StringComparison.Ordinal);
        Assert.True(b > a, $"ArithmeticEmitter no longer has '{to}' after '{from}' - re-point this guard");
        return text[a..b];
    }

    [Fact]
    public void TheDivisionIsRenderedThroughFinalQuotientAlone()
    {
        var code = Code();
        var divisions = code.Select((line, i) => (line, i))
            .Where(x => Regex.IsMatch(x.line, "num\\.Combine\\([^;]*\"/\"")).ToList();
        Assert.True(divisions.Count == 1,
            "ArithmeticEmitter renders a division through num.Combine(..., \"/\", ...) in " + divisions.Count + " places; the "
            + "quotient of every DIVIDE format is formed by FinalQuotient alone (ISO 14.9.12.4 5) b), 6) b), kb/Work PB2923)");
        int definition = Array.FindIndex(code, l => l.Contains("private NumX FinalQuotient(", StringComparison.Ordinal));
        Assert.True(definition >= 0 && divisions[0].i == definition + 1,
            "the one division must be the body of FinalQuotient");
    }

    [Fact]
    public void BothDivideStatementsFormTheirQuotientByIt()
    {
        string text = string.Join(Environment.NewLine, Code());
        Assert.Contains("FinalQuotient(", Between(text, "public void EmitDivide(", "private NumX FinalQuotient("), StringComparison.Ordinal);
        Assert.Contains("FinalQuotient(", Between(text, ") CarriedSubsidiaryQuotient(", "private NumX FixedPointSubsidiary("), StringComparison.Ordinal);
    }

    [Fact]
    public void EverySubsidiaryFormHasItsOwnArm()
    {
        string text = string.Join(Environment.NewLine, Code());
        string enumBody = Regex.Match(text, "private enum SubsidiaryForm\\s*\\{(?<b>[^}]*)\\}").Groups["b"].Value;
        var forms = Regex.Matches(enumBody, "\\b(?<n>[A-Z]\\w+),").Select(m => m.Groups["n"].Value).ToList();
        Assert.True(forms.Count >= 3, "SubsidiaryForm shrank - re-point this guard");
        string arms = Between(text, "NumX subsidiary = shape.Form switch", "};");
        foreach (string form in forms.Where(f => f != "FixedPoint"))
            Assert.True(arms.Contains($"SubsidiaryForm.{form} =>", StringComparison.Ordinal),
                $"the subsidiary-quotient switch has no arm for SubsidiaryForm.{form}: a form of identifier-3 whose shape is not " +
                "written down falls into the fixed-point arm and truncates at a scale it does not have (ISO 14.9.12.4 6) c), kb/Work PB2923)");
    }
}
