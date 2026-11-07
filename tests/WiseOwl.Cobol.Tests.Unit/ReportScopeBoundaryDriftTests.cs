// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE REPORT SECTION HAS ONE WRITER AND ONE HOME (kb/Work PB2357;
/// docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1), step 7 of the <c>DataBinder</c> extract).
/// <para><c>ReportSectionBinder</c> binds and resolves the REPORT SECTION and is the only writer of <c>ReportScope</c>,
/// the facts every later phase reads (the reports a unit declares and inherits, their depths, the sum counters, the
/// refused PRESENT WHEN conditions). The C# types hold the read side (<c>ReportScope</c>'s mutators are
/// <c>internal</c>); what they cannot say is WHICH file of the assembly calls them, and that the report concern does not
/// grow back into a <c>DataBinder</c> partial. So this test says both, by a scan of the compiler's source:</para>
/// <list type="bullet">
/// <item>a <c>ReportScope</c> is mutated (<c>Declare</c>, <c>Inherit</c>, <c>AddSumCounter</c>, <c>RefusePresentWhen</c>)
/// only in <c>ReportSectionBinder.cs</c>, so a second producer of a unit's reports, sum counters or refusals cannot
/// appear;</item>
/// <item>no <c>DataBinder.Reports*.cs</c> partial exists: the report-section binding is <c>ReportSectionBinder</c>'s, and a
/// method of it re-added to <c>DataBinder</c> would be the second home of the rule;</item>
/// <item>the sum-counter map is read only through <c>DataBinder.SumCounters</c>, whose watermark gate makes an early reader
/// fail by name, except by the one <c>DataBinder</c> name-scope test that owns the map's other read.</item>
/// </list>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ReportScopeBoundaryDriftTests
{
    // The mutators are internal, so any file of the assembly can call them through any receiver spelling. The two whose
    // names no other type of the compiler uses are matched on ANY receiver. Declare and Inherit are also the verbs of the
    // name scopes (IndexNames, Paras, Sections), so they are matched when the receiver's last word says it is a scope or
    // the reports (_scope, scope, data.Reports, reports, reportScope); a receiver spelled otherwise is the limit of a text scan.
    private static readonly Regex Mutation = new(
        @"\.(?:AddSumCounter|RefusePresentWhen)\(|\b\w*(?:[Ss]cope|[Rr]eports)\.(?:Declare|Inherit)\(", RegexOptions.Compiled);

    private static IEnumerable<(string File, string Text)> CompilerSources() =>
        Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Select(f => (Path.GetRelativePath(TestRepo.Root, f).Replace('\\', '/'), File.ReadAllText(f)));

    [Fact]
    public void TheReportScope_IsMutatedOnlyByTheReportSectionBinder()
    {
        var offenders = new List<string>();
        int found = 0;
        foreach (var (file, text) in CompilerSources())
        {
            if (file.EndsWith("/Binding/ReportScope.cs", StringComparison.Ordinal)) continue;
            foreach (Match m in Mutation.Matches(text))
            {
                found++;
                if (!file.EndsWith("/Binding/ReportSectionBinder.cs", StringComparison.Ordinal))
                    offenders.Add($"{file}: {m.Value}");
            }
        }
        Assert.True(found >= 4, $"the scan found only {found} report-scope mutations — the pattern no longer matches the code");
        Assert.True(offenders.Count == 0,
            "A ReportScope is written outside ReportSectionBinder — a unit's reports, sum counters and refused PRESENT WHEN "
            + "conditions have ONE producer (ISO §2023:13.18.27.4 GR1–GR2; §2023:13.15.3 SR16):\n  " + string.Join("\n  ", offenders));
    }

    [Fact]
    public void TheSumCounterMap_IsReadOutsideTheDataBinderOnlyThroughItsGatedView()
    {
        // DataBindState.SumCounters carries the P6 Step-6 watermark (Require(FilesResolved)); ReportScope.SumCounters is the
        // ungated map behind it, internal so no other assembly reaches it, and read in the two files that own the gate
        // (DataBindState) and the name-scope test (DataBinder.NonDataNames.cs, DeclaredNearerThan) only - any other
        // reader asks data.State.SumCounters.
        var offenders = new List<string>();
        int found = 0;
        foreach (var (file, text) in CompilerSources())
            foreach (Match m in Regex.Matches(text, @"\bReports\.SumCounters\b"))
            {
                found++;
                if (!file.EndsWith("/Binding/DataBindState.cs", StringComparison.Ordinal)
                    && !file.EndsWith("/Binding/DataBinder.NonDataNames.cs", StringComparison.Ordinal))
                    offenders.Add($"{file}: {m.Value}");
            }
        Assert.True(found >= 2, $"the scan found only {found} reads of Reports.SumCounters - the pattern no longer matches the code");
        Assert.True(offenders.Count == 0,
            "The ungated sum-counter map is read outside DataBindState - read DataBindState.SumCounters, which carries the "
            + "watermark gate:\n  " + string.Join("\n  ", offenders));
    }

    [Fact]
    public void TheMutationScan_SeesAWriteOutsideTheBinder()
    {
        // The scan's own population: a write planted in another file's text is matched, so a rename of the mutators
        // fails the scan loudly instead of emptying it.
        Assert.Matches(Mutation, "        data.Reports.AddSumCounter(name, report, family);");
        Assert.Matches(Mutation, "        sums.AddSumCounter(name, report, family);");
        Assert.Matches(Mutation, "        reports.Inherit(report, depth);");
        Assert.Matches(Mutation, "        var scope = data.Reports; scope.Declare(model);");
        Assert.DoesNotMatch(Mutation, "        IndexNames.Inherit(idx, depth);");
        Assert.DoesNotMatch(Mutation, "            ms.Paras.Declare(name, _paras.Count);");
        Assert.DoesNotMatch(Mutation, "        data.Reports.Declared.Count > 0");
    }

    [Fact]
    public void TheReportSectionBinding_HasNoDataBinderPartial()
    {
        string binding = Path.Combine(TestRepo.Src("WiseOwl.Cobol.Compiler"), "Binding");
        Assert.Empty(Directory.GetFiles(binding, "DataBinder.Reports*.cs"));
        foreach (string home in new[] { "ReportSectionBinder.cs", "ReportScope.cs", "ReportModel.cs" })
            Assert.True(File.Exists(Path.Combine(binding, home)), $"{home} is the report section's home and is missing");
    }
}
