// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE REPORT SECTION HAS ONE WRITER AND ONE HOME (kb/Work PB2357; docs/rearchitecture/DESIGN-architecture-review.md
/// §8.3 (1), step 7 of the <c>DataBinder</c> extract).
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
/// method of it re-added to <c>DataBinder</c> would be the second home of the rule.</item>
/// </list>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ReportScopeBoundaryDriftTests
{
    private static readonly Regex Mutation =
        new(@"\b(?:_scope|Reports)\.(?:Declare|Inherit|AddSumCounter|RefusePresentWhen)\(", RegexOptions.Compiled);

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
            + "conditions have ONE producer (ISO §13.18.27.4 GR1–GR2; §13.15.3 SR16):\n  " + string.Join("\n  ", offenders));
    }

    [Fact]
    public void TheMutationScan_SeesAWriteOutsideTheBinder()
    {
        // The scan's own population: a write planted in another file's text is matched, so a rename of the mutators
        // fails the scan loudly instead of emptying it.
        Assert.Matches(Mutation, "        data.Reports.AddSumCounter(name, report, family);");
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
