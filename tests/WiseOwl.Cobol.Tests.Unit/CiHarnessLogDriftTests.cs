// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A NON-OBSERVATION ON CI CAN BE READ AFTERWARDS (kb/Work PB3108; DESIGN-test-build-ci.md §3.10): every step of
/// <c>.github/workflows/build-and-test.yml</c> that runs <c>dotnet test</c> sets <c>WISEOWL_COBOL_HARNESS_LOG</c> to
/// <c>harness-observations.log</c> in its own <c>--results-directory</c>, and its job uploads that file (the directory,
/// or the file by name) with the TRX.
/// </summary>
/// <remarks>
/// The test harness records every retry and non-observation of a child process, with its budget, the work it covered
/// and the host's load (<c>HarnessLog.Path</c>). On a hosted runner that file was the temp directory's, gone
/// with the runner, so a CI retry or a red non-observation could not be read after the run: PB3108's open item. The
/// local gates write it beside each leg's log (<c>scripts/run_gate_legs.py</c>'s <c>leg_env</c>,
/// <c>scripts/linux-gate.sh</c>'s <c>test_leg</c>).
/// </remarks>
public sealed class CiHarnessLogDriftTests
{
    private const string LogName = "harness-observations.log";

    [Fact]
    public void EveryCiTestStep_WritesTheHarnessLogWhereItsJobUploadsIt()
    {
        string workflow = File.ReadAllText(TestRepo.At(".github", "workflows", "build-and-test.yml"));
        var (steps, offenders) = Audit(workflow);
        Assert.True(steps > 0, "no `dotnet test` step found in build-and-test.yml: the audit reads nothing");
        Assert.True(offenders.Count == 0,
            $"CI test steps whose harness observation log is not written into their results directory or not uploaded "
            + $"(kb/Work PB3108):\n{string.Join("\n", offenders)}");
    }

    /// <summary>The audit fires on each shape it forbids: a step with no log, a log outside the step's results
    /// directory, and a log its job does not upload; and passes the shape it requires.</summary>
    [Fact]
    public void TheAudit_FiresOnAMissingAMisplacedAndAnUnuploadedLog()
    {
        const string Good = """
            jobs:
              a:
                steps:
                  - name: tests
                    env:
                      WISEOWL_COBOL_HARNESS_LOG: ${{ github.workspace }}/TestResults/x/harness-observations.log
                    run: dotnet test p.csproj --results-directory TestResults/x
                  - name: Upload
                    uses: ./.github/actions/upload-results
                    with:
                      path: |
                        TestResults/x/*.trx
                        TestResults/x/harness-observations.log
            """;
        Assert.Equal((1, 0), Count(Good));
        Assert.Equal((1, 1), Count(Good.Replace("WISEOWL_COBOL_HARNESS_LOG: ${{ github.workspace }}/TestResults/x/harness-observations.log", "OTHER: x", StringComparison.Ordinal)));
        Assert.Equal((1, 1), Count(Good.Replace("/TestResults/x/harness-observations.log", "/TestResults/y/harness-observations.log", StringComparison.Ordinal)));
        Assert.Equal((1, 1), Count(Good.Replace("\n            TestResults/x/harness-observations.log", "", StringComparison.Ordinal)));
        Assert.Equal((1, 0), Count(Good.Replace("path: |\n            TestResults/x/*.trx\n            TestResults/x/harness-observations.log", "path: TestResults/x/", StringComparison.Ordinal)));

        static (int, int) Count(string yaml)
        {
            var (steps, offenders) = Audit(yaml);
            return (steps, offenders.Count);
        }
    }

    /// <summary>A path as the workflow spells it: a run of non-blank characters and <c>${{ … }}</c> expressions, which
    /// hold blanks (<c>TestResults/${{ runner.os }}-unit</c>).</summary>
    private const string WorkflowPath = @"((?:\$\{\{[^}]*\}\}|[^\s$])+)";
    private static readonly Regex ResultsDirectory = new(@"--results-directory\s+" + WorkflowPath, RegexOptions.NonBacktracking);
    private static readonly Regex HarnessLog = new(
        @"^\s*WISEOWL_COBOL_HARNESS_LOG:\s*\$\{\{\s*github\.workspace\s*\}\}/" + WorkflowPath + @"\s*$", RegexOptions.NonBacktracking);

    /// <summary>The upload paths of a job's upload steps (<see cref="CiArtifactDriftTests.UploadAction"/>): the
    /// <c>path:</c> value, or each line of a <c>path: |</c> block.</summary>
    private static IEnumerable<string> UploadPaths(CiJob job)
    {
        foreach (CiStep step in job.Steps.Where(s => s.Value("uses")?.Contains(CiArtifactDriftTests.UploadAction, StringComparison.Ordinal) == true))
        {
            List<string> lines = [.. step.CodeLines];
            int at = lines.FindIndex(l => l.TrimStart().StartsWith("path:", StringComparison.Ordinal));
            if (at < 0) continue;
            string value = lines[at].TrimStart()["path:".Length..].Trim();
            if (value != "|")
            {
                yield return value;
                continue;
            }
            int indent = CiWorkflow.Indent(lines[at]);
            foreach (string line in lines.Skip(at + 1).TakeWhile(l => CiWorkflow.Indent(l) > indent))
                yield return line.Trim();
        }
    }

    private static (int Steps, List<string> Offenders) Audit(string workflow)
    {
        int steps = 0;
        var offenders = new List<string>();
        foreach (CiJob job in CiWorkflow.Jobs(workflow))
        {
            var uploaded = UploadPaths(job).ToList();
            foreach (CiStep step in job.Steps.Where(s => s.CodeLines.Any(l => l.Contains("dotnet test ", StringComparison.Ordinal))))
            {
                steps++;
                string name = step.Lines[0].Trim();
                string? results = step.CodeLines.Select(l => ResultsDirectory.Match(l)).FirstOrDefault(m => m.Success)?.Groups[1].Value;
                string? log = step.CodeLines.Select(l => HarnessLog.Match(l)).FirstOrDefault(m => m.Success)?.Groups[1].Value;
                if (results is null) offenders.Add($"  {name}: names no --results-directory");
                else if (log is null) offenders.Add($"  {name}: sets no WISEOWL_COBOL_HARNESS_LOG");
                else if (log != $"{results}/{LogName}")
                    offenders.Add($"  {name}: WISEOWL_COBOL_HARNESS_LOG is {log}, not {results}/{LogName}");
                else if (!uploaded.Any(p => p == $"{results}/" || p == $"{results}/{LogName}"))
                    offenders.Add($"  {name}: its job uploads neither {results}/ nor {results}/{LogName}");
            }
        }
        return (steps, offenders);
    }
}
