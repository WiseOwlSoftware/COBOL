// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A CLAUSE ARM THAT NAMES A REPORT ENTRY TAKES THE EDITION'S OWN VERDICT (kb/Work PB5361). The refusal
/// <c>ReportSectionBinder.ReportEntryReferenceRefused</c> is §2023:13.8.6.2.3's section rule, which COBOL-85 does not have
/// (§1985:XIII.3.9.3 SR10d lets a sum counter be referenced wherever it is written), so an arm that asked it directly refused a
/// COBOL-85 program under a rule of another edition (LINAGE, RECORD DEPENDING ON, RECORD KEY and ALTERNATE RECORD KEY did, until
/// this note). The one ask that carries the 85 edge is <c>ReportSectionBinder.ReportItemOperandRefused</c>; the raw refusal
/// has exactly the callers below, each with the reason it needs no edge of its own, so a NEW clause arm that calls it
/// fails here instead of shipping the 2023 citation at <c>--std 85</c>.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ReportItemOperandDriftTests
{
    /// <summary>File → why a direct call of the raw refusal is right there. A file is listed only with its reason.</summary>
    private static readonly Dictionary<string, string> RawCallers = new(StringComparer.Ordinal)
    {
        ["Binding/ReportSectionBinder.cs"] = "the refusal's own home and its one wrapper, ReportItemOperandRefused",
        ["Binding/NameResolution.cs"] = "a procedure division reference: a sum counter resolves there before name lookup fails (§2023:13.8.6.2.3 1)",
        ["Binding/Procedure/Verbs/ConditionBinder.cs"] = "a procedure division condition operand, the same reading",
        ["Binding/Passes/OdoResolvePass.cs"] = "OCCURS DEPENDING ON asks SumCounterOperand85 itself, on the line before, for its clone-by-clone tiers",
    };

    private static readonly Regex Call = new(@"\bReportEntryReferenceRefused\(", RegexOptions.Compiled);

    private static IEnumerable<string> CompilerSources() =>
        Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

    private static string Relative(string path) =>
        Path.GetRelativePath(TestRepo.Src("WiseOwl.Cobol.Compiler"), path).Replace('\\', '/');

    private static IEnumerable<string> CodeLinesWithCall(string path) =>
        File.ReadLines(path).Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal) && Call.IsMatch(l));

    /// <summary>Only the listed files call the raw refusal; a clause arm anywhere else must call
    /// <c>ReportItemOperandRefused</c> (and the scan is proven able to find a call: the listed files do call it).</summary>
    [Fact]
    public void TheRawRefusal_IsCalledOnlyWhereItNeedsNoEightyFiveEdge()
    {
        var callers = CompilerSources().Where(f => CodeLinesWithCall(f).Any()).Select(Relative).ToHashSet(StringComparer.Ordinal);
        Assert.NotEmpty(callers);
        Assert.Empty(callers.Except(RawCallers.Keys));
        Assert.Empty(RawCallers.Keys.Except(callers));
    }

    /// <summary>The OCCURS DEPENDING ON arm, which keeps its own ask, does take the 85 reading first.</summary>
    [Fact]
    public void TheOdoArm_AsksSumCounterOperand85BeforeTheRawRefusal()
    {
        string text = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "Passes", "OdoResolvePass.cs"));
        int edge = text.IndexOf("SumCounterOperand85(", StringComparison.Ordinal);
        int raw = text.IndexOf("ReportEntryReferenceRefused(", StringComparison.Ordinal);
        Assert.True(edge > 0 && raw > edge, "OdoResolvePass must read the COBOL-85 sum counter before it refuses a report entry.");
    }
}
