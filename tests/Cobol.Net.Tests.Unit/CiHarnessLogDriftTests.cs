// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A NON-OBSERVATION ON CI CAN BE READ AFTERWARDS (kb/Work PB3108; DESIGN-test-build-ci.md §3.10): every step of
/// <c>.github/workflows/build-and-test.yml</c> that runs <c>dotnet test</c> sets <c>COBOLNET_HARNESS_LOG</c> to
/// <c>harness-observations.log</c> in its own <c>--results-directory</c>, and its job uploads that file (the directory,
/// or the file by name) with the TRX.
/// </summary>
/// <remarks>
/// The test harness records every retry and non-observation of a child process, with its budget, the work it covered
/// and the host's load (<c>ProcessObserver.LogPath</c>). On a hosted runner that file was the temp directory's, gone
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
        string[] workflow = File.ReadAllLines(TestRepo.At(".github", "workflows", "build-and-test.yml"));
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
                      COBOLNET_HARNESS_LOG: ${{ github.workspace }}/TestResults/x/harness-observations.log
                    run: dotnet test p.csproj --results-directory TestResults/x
                  - name: Upload
                    uses: actions/upload-artifact@v7
                    with:
                      path: |
                        TestResults/x/*.trx
                        TestResults/x/harness-observations.log
            """;
        Assert.Equal((1, 0), Count(Good));
        Assert.Equal((1, 1), Count(Good.Replace("COBOLNET_HARNESS_LOG: ${{ github.workspace }}/TestResults/x/harness-observations.log", "OTHER: x", StringComparison.Ordinal)));
        Assert.Equal((1, 1), Count(Good.Replace("/TestResults/x/harness-observations.log", "/TestResults/y/harness-observations.log", StringComparison.Ordinal)));
        Assert.Equal((1, 1), Count(Good.Replace("\n            TestResults/x/harness-observations.log", "", StringComparison.Ordinal)));
        Assert.Equal((1, 0), Count(Good.Replace("path: |\n            TestResults/x/*.trx\n            TestResults/x/harness-observations.log", "path: TestResults/x/", StringComparison.Ordinal)));

        static (int, int) Count(string yaml)
        {
            var (steps, offenders) = Audit(yaml.ReplaceLineEndings("\n").Split('\n'));
            return (steps, offenders.Count);
        }
    }

    private static readonly Regex JobHeader = new(@"^  ([A-Za-z0-9_-]+):\s*(#.*)?$", RegexOptions.NonBacktracking);
    /// <summary>A path as the workflow spells it: a run of non-blank characters and <c>${{ … }}</c> expressions, which
    /// hold blanks (<c>TestResults/${{ runner.os }}-unit</c>).</summary>
    private const string WorkflowPath = @"((?:\$\{\{[^}]*\}\}|[^\s$])+)";
    private static readonly Regex ResultsDirectory = new(@"--results-directory\s+" + WorkflowPath, RegexOptions.NonBacktracking);
    private static readonly Regex HarnessLog = new(
        @"^\s*COBOLNET_HARNESS_LOG:\s*\$\{\{\s*github\.workspace\s*\}\}/" + WorkflowPath + @"\s*$", RegexOptions.NonBacktracking);

    /// <summary>Each job's steps (a step starts at <c>      - </c>), as blocks of lines, comments dropped.</summary>
    private static IEnumerable<List<List<string>>> Jobs(string[] workflow)
    {
        int start = Array.FindIndex(workflow, l => l.TrimEnd() == "jobs:");
        if (start < 0) yield break;
        List<List<string>>? job = null;
        foreach (string line in workflow.Skip(start + 1))
        {
            if (JobHeader.IsMatch(line))
            {
                if (job is not null) yield return job;
                job = [];
                continue;
            }
            if (job is null || line.TrimStart().StartsWith('#')) continue;
            if (line.StartsWith("      - ", StringComparison.Ordinal)) job.Add([]);
            if (job.Count > 0) job[^1].Add(line);
        }
        if (job is not null) yield return job;
    }

    /// <summary>The upload paths of a job's upload-artifact steps: the <c>path:</c> value, or each line of a
    /// <c>path: |</c> block.</summary>
    private static IEnumerable<string> UploadPaths(List<List<string>> job)
    {
        foreach (var step in job.Where(s => s.Any(l => l.Contains("actions/upload-artifact", StringComparison.Ordinal))))
        {
            int at = step.FindIndex(l => l.TrimStart().StartsWith("path:", StringComparison.Ordinal));
            if (at < 0) continue;
            string value = step[at].TrimStart()["path:".Length..].Trim();
            if (value != "|")
            {
                yield return value;
                continue;
            }
            int indent = step[at].Length - step[at].TrimStart().Length;
            foreach (string line in step.Skip(at + 1).TakeWhile(l => l.Length - l.TrimStart().Length > indent))
                yield return line.Trim();
        }
    }

    private static (int Steps, List<string> Offenders) Audit(string[] workflow)
    {
        int steps = 0;
        var offenders = new List<string>();
        foreach (var job in Jobs(workflow))
        {
            var uploaded = UploadPaths(job).ToList();
            foreach (var step in job.Where(s => s.Any(l => l.Contains("dotnet test ", StringComparison.Ordinal))))
            {
                steps++;
                string name = step[0].Trim();
                string? results = step.Select(l => ResultsDirectory.Match(l)).FirstOrDefault(m => m.Success)?.Groups[1].Value;
                string? log = step.Select(l => HarnessLog.Match(l)).FirstOrDefault(m => m.Success)?.Groups[1].Value;
                if (results is null) offenders.Add($"  {name}: names no --results-directory");
                else if (log is null) offenders.Add($"  {name}: sets no COBOLNET_HARNESS_LOG");
                else if (log != $"{results}/{LogName}")
                    offenders.Add($"  {name}: COBOLNET_HARNESS_LOG is {log}, not {results}/{LogName}");
                else if (!uploaded.Any(p => p == $"{results}/" || p == $"{results}/{LogName}"))
                    offenders.Add($"  {name}: its job uploads neither {results}/ nor {results}/{LogName}");
            }
        }
        return (steps, offenders);
    }
}
