// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVALUATE IS BOUND BY THREE TYPES, ONE CONCERN EACH (kb/Work PB2338; docs/rearchitecture/DESIGN-architecture-review.md
/// §8.3, census R0-0043): <c>EvaluateBinder</c> binds the STATEMENT (the WHEN clauses, their bodies, WHEN OTHER and the
/// edition's object-evaluation rule), <c>EvaluateSubjectBinder</c> the selection SUBJECTS (one slot each, evaluated once,
/// ISO §2023:14.9.13.4 GR3) and <c>WhenPhraseBinder</c> one WHEN phrase's selection OBJECTS (the Table 15 pairing of
/// §2023:14.9.13.3 SR10, through <c>EvaluateOperandCombinations</c>). <c>EvaluateOperands</c> holds the one bare-operand
/// classification and the one operand bind both sides share.
/// <para>The C# types say who may CALL whom; they cannot say that a second place does not grow back the same rule. So this
/// test says it, by a scan of the compiler's source:</para>
/// <list type="bullet">
/// <item>a subject slot is made only by <c>EvaluateSubjectBinder</c> (<c>new SubjectSlot(</c>): a second maker would be a
/// second evaluation of a subject GR3 evaluates once;</item>
/// <item>Table 15 is asked (<c>EvaluateOperandCombinations.IsPermitted</c>) only by <c>WhenPhraseBinder</c>, the one pair
/// classifier;</item>
/// <item><c>EvaluateBinder</c> reads neither side's operands (<c>.evaluateSubject()</c>, <c>.evaluateWhenItem()</c>), so the
/// statement binder cannot grow a subject or object arm again.</item>
/// </list>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class EvaluateBinderBoundaryDriftTests
{
    private static string Verbs(string file) =>
        File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "Procedure", "Verbs", file));

    private static IEnumerable<(string File, string Text)> CompilerSources() =>
        Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Select(f => (Path.GetRelativePath(TestRepo.Root, f).Replace('\\', '/'), File.ReadAllText(f)));

    [Theory]
    [InlineData(@"\bnew SubjectSlot\(", "/Binding/Procedure/Verbs/EvaluateSubjectBinder.cs")]
    [InlineData(@"\bEvaluateOperandCombinations\.IsPermitted\(", "/Binding/Procedure/Verbs/WhenPhraseBinder.cs")]
    public void TheRule_HasOneHome(string pattern, string home)
    {
        var rx = new Regex(pattern);
        var sites = CompilerSources().Where(s => rx.IsMatch(s.Text)).Select(s => s.File).ToList();
        Assert.True(sites.Count > 0, $"the scan found no `{pattern}` — the pattern no longer matches the code");
        Assert.True(sites.All(f => f.EndsWith(home, StringComparison.Ordinal)),
            $"`{pattern}` is written outside {home} (kb/Work PB2338): " + string.Join(", ", sites));
    }

    [Fact]
    public void TheStatementBinder_ReadsNeitherSidesOperands()
    {
        string statement = Verbs("EvaluateBinder.cs");
        Assert.Contains("_subjects.Bind(ev)", statement, StringComparison.Ordinal);
        Assert.Contains("_whens.Bind(phrase, slots, everyObjectFirst)", statement, StringComparison.Ordinal);
        foreach (string read in new[] { ".evaluateSubject()", ".evaluateWhenItem()" })
            Assert.False(statement.Contains(read, StringComparison.Ordinal),
                $"EvaluateBinder.cs reads {read}: a selection subject is EvaluateSubjectBinder's and a selection object "
                + "WhenPhraseBinder's (kb/Work PB2338)");
    }
}
