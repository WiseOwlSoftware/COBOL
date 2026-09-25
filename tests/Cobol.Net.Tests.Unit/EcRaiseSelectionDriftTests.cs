// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Runtime.Exceptions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB1549 — <b>"fatal, checking enabled, not resumed ⇒ abnormal run-unit termination" is decided in ONE
/// place.</b> ISO §14.6.13.1.3 5) (a declarative that completes normally) and 7) (no handler) both end the run unit;
/// only RESUME continues. Every emitted raise site used to render the selector (<c>EcDispatchExpr</c>) and the
/// RESUME landing itself and was separately responsible for the fatal default after it; the SET … ADDRESS OF
/// PROGRAM / FUNCTION sites forgot it, so a failed SET CONTINUED the run unit while the CALL arm of the same
/// condition terminated it. The selection now renders only through <c>EcEmitter.EmitSelection</c>, and a
/// compile-time-named raise takes its fatality from Table 13 (<c>EcEmitter.EmitConditionRaise</c>). These pins keep
/// a new raise site from spelling the selector — and so the decision — again.
/// </summary>
public sealed class EcRaiseSelectionDriftTests
{
    private static string CodeGenDir =>
        Path.GetDirectoryName(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "EcEmitter.cs"))!;

    /// <summary>The selector is rendered by <c>EmitSelection</c> alone, plus the two NONFATAL-selector bridges the
    /// runtime's own raise sites enter through (§14.6.13.1.4 — a nonfatal condition has no fatal default to forget):
    /// <c>INonfatalSelector.NonfatalDispatch</c> (ProgramEmitter) and the class-unit <c>NonfatalSelectorFn</c>
    /// (OoEmitter). At the pre-PB1549 tree this listed PtrEmitter ×5, CallEmitter ×3, SequentialIoEmitter,
    /// ControlFlowEmitter, StatementEmitter and four sites inside EcEmitter (measured by grep).</summary>
    [Fact]
    public void RaiseSiteSelector_IsRenderedOnlyByEmitSelection()
    {
        var calls = Directory.EnumerateFiles(CodeGenDir, "*.cs", SearchOption.AllDirectories)
            .SelectMany(f => Regex.Matches(File.ReadAllText(f), @"\bEcDispatchExpr\(")
                .Select(_ => Path.GetFileName(f)))
            .GroupBy(n => n)
            .ToDictionary(g => g.Key, g => g.Count());
        var expected = new Dictionary<string, int>
        {
            ["EcEmitter.cs"] = 2,       // its declaration + the one use in EmitSelection
            ["ProgramEmitter.cs"] = 1,  // INonfatalSelector.NonfatalDispatch
            ["OoEmitter.cs"] = 1,       // the class unit's NonfatalSelectorFn
        };
        Assert.True(expected.OrderBy(p => p.Key).SequenceEqual(calls.OrderBy(p => p.Key)),
            "EcDispatchExpr is rendered outside EcEmitter.EmitSelection — route the raise site through "
            + "EmitSelection / EmitConditionRaise so the §14.6.13.1.3 fatal default cannot be forgotten: "
            + string.Join(", ", calls.Select(p => $"{p.Key}×{p.Value}")));
    }

    /// <summary>No emitter bakes a compile-time exception-name's fatality as a literal: a constant-name raise goes
    /// through <c>EmitConditionSet</c> / <c>EmitConditionRaise</c>, which read Table 13. (RAISE, whose fatality the
    /// binder already read from the same catalog, and dynamic names are not literal <c>"EC-…"</c> spellings.)</summary>
    [Fact]
    public void NoEmitter_BakesAConstantConditionsFatality()
    {
        var offenders = Directory.EnumerateFiles(CodeGenDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => Regex.IsMatch(File.ReadAllText(f), @"ExceptionState\.Set\(\\""EC-"))
            .Select(Path.GetFileName)
            .ToList();
        Assert.True(offenders.Count == 0,
            "an emitter sets a named exception condition with a hand-written fatal flag; use "
            + "EcEmitter.EmitConditionSet/EmitConditionRaise (Table 13 decides): " + string.Join(", ", offenders));
    }

    /// <summary>The conditions the SET … ADDRESS OF raise sites name are fatal in Table 13 — the premise the
    /// PB1549 fix rests on — and <see cref="EcInfo.IsFatal"/> is the one spelling of the test (Imp counts as
    /// fatal, the documented implementor choice).</summary>
    [Theory]
    [InlineData("EC-PROGRAM-NOT-FOUND", true)]
    [InlineData("EC-FUNCTION-NOT-FOUND", true)]
    [InlineData("EC-FUNCTION-PTR-INVALID", true)]
    [InlineData("EC-OVERFLOW-IMP", true)]
    [InlineData("EC-STORAGE-NOT-ALLOC", false)]
    [InlineData("EC-STORAGE-NOT-AVAIL", false)]
    [InlineData("EC-REPORT-NOT-TERMINATED", false)]
    public void Table13Fatality_DrivesTheDefault(string ecName, bool fatal)
    {
        Assert.True(ExceptionCatalog.TryGet(ecName, out var info));
        Assert.Equal(fatal, info.IsFatal);
    }
}
