// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ AN ARTIFACT NEVER DECIDES A CI VERDICT (kb/Work PB3140; DESIGN-test-build-ci.md §1.4): no workflow under
/// <c>.github/workflows/</c> names <c>actions/upload-artifact</c> or <c>actions/download-artifact</c>; every upload
/// step uses <c>./.github/actions/upload-results</c> under <c>if: ${{ always() }}</c> and every download step uses
/// <c>./.github/actions/download-results</c>. The upload action tries at least twice, every attempt under
/// <c>always()</c> with <c>continue-on-error: true</c> and every retry with <c>overwrite: true</c>, and ends on a
/// warning; the download action tries at least twice, and only its last attempt can fail the step.
/// </summary>
/// <remarks>
/// Merge-group run 38502926616 (PR #39, 2026-10-14): a Windows conformance shard passed 5338 of 5338 tests, then its
/// bare <c>actions/upload-artifact</c> step (<c>if: always()</c>, nothing else) failed on the artifact service's
/// <c>ETIMEDOUT</c>; the job went red, <c>ci-gate</c> failed, and the merge queue removed a pull request whose every
/// test had passed. <c>always()</c> makes an upload run after a failure (a red job's results are what the artifacts
/// are for); it never made the upload's own failure harmless. The rule now lives in the two actions, and this test
/// keeps every workflow step on them, so the next upload added cannot bring the service back into the verdict.
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class CiArtifactDriftTests
{
    /// <summary>The one upload step every workflow uses.</summary>
    public const string UploadAction = "./.github/actions/upload-results";

    /// <summary>The one download step every workflow uses.</summary>
    public const string DownloadAction = "./.github/actions/download-results";

    private const string RawUpload = "actions/upload-artifact";
    private const string RawDownload = "actions/download-artifact";

    [Fact]
    public void EveryWorkflowArtifactStep_GoesThroughTheRetriedActions()
    {
        var offenders = new List<string>();
        int uploads = 0, downloads = 0;
        foreach (string workflow in Directory.EnumerateFiles(TestRepo.At(".github", "workflows"), "*.yml"))
        {
            var (u, d, found) = AuditWorkflow(Path.GetFileName(workflow), File.ReadAllLines(workflow));
            uploads += u;
            downloads += d;
            offenders.AddRange(found);
        }

        Assert.True(offenders.Count == 0,
            "workflow artifact steps outside the retried actions (kb/Work PB3140): an upload uses " + UploadAction
            + " under `if: ${{ always() }}`, a download uses " + DownloadAction + ":\n" + string.Join("\n", offenders));
        // The audit must have read something: build-and-test.yml uploads every test job's results and its population
        // guard downloads the shards'. A zero here is a broken parser, never a clean tree.
        Assert.True(uploads > 0 && downloads > 0,
            $"the audit found {uploads} upload and {downloads} download step(s) under .github/workflows: it reads nothing");
    }

    [Fact]
    public void TheUploadAction_RetriesAndNeverFailsTheJob()
    {
        var offenders = AuditUploadAction(File.ReadAllLines(TestRepo.At(".github", "actions", "upload-results", "action.yml")));
        Assert.True(offenders.Count == 0, "the upload action (kb/Work PB3140):\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void TheDownloadAction_RetriesAndFailsOnlyOnItsLastAttempt()
    {
        var offenders = AuditDownloadAction(File.ReadAllLines(TestRepo.At(".github", "actions", "download-results", "action.yml")));
        Assert.True(offenders.Count == 0, "the download action (kb/Work PB3140):\n" + string.Join("\n", offenders));
    }

    /// <summary>Each audit fires on every shape it forbids and passes the shape it requires.</summary>
    [Fact]
    public void TheAudits_FireOnEveryForbiddenShape()
    {
        const string Workflow = """
            jobs:
              a:
                steps:
                  - name: tests
                    run: dotnet test p.csproj
                  - name: Upload
                    if: ${{ always() }}
                    uses: ./.github/actions/upload-results
                    with:
                      name: r
                      path: TestResults/
                  - name: Download
                    uses: ./.github/actions/download-results
                    with:
                      pattern: r-*
                      path: shards
            """;
        Assert.Equal((1, 1, 0), Workflows(Workflow));
        Assert.Equal((1, 0, 1), Workflows(Workflow.Replace("uses: ./.github/actions/download-results", "uses: actions/download-artifact@v8", StringComparison.Ordinal)));
        Assert.Equal((0, 1, 1), Workflows(Workflow.Replace("uses: ./.github/actions/upload-results", "uses: actions/upload-artifact@v7", StringComparison.Ordinal)));
        Assert.Equal((1, 1, 1), Workflows(Workflow.Replace("        if: ${{ always() }}\n", "", StringComparison.Ordinal)));
        Assert.Equal((1, 1, 1), Workflows(Workflow.Replace("if: ${{ always() }}", "if: ${{ !cancelled() }}", StringComparison.Ordinal)));
        // A comment naming the raw action is prose, not a step.
        Assert.Equal((1, 1, 0), Workflows(Workflow.Replace("  - name: tests", "  # once a bare actions/upload-artifact step\n      - name: tests", StringComparison.Ordinal)));

        const string Upload = """
            runs:
              using: composite
              steps:
                - id: attempt1
                  if: ${{ always() }}
                  continue-on-error: true
                  uses: actions/upload-artifact@v7
                - if: ${{ always() && steps.attempt1.outcome == 'failure' }}
                  shell: bash
                  run: sleep 30
                - id: attempt2
                  if: ${{ always() && steps.attempt1.outcome == 'failure' }}
                  continue-on-error: true
                  uses: actions/upload-artifact@v7
                  with:
                    overwrite: true
                - if: ${{ always() && steps.attempt2.outcome == 'failure' }}
                  shell: bash
                  run: echo "::warning::not uploaded"
            """;
        Assert.Equal(0, Count(AuditUploadAction, Upload));
        Assert.Equal(1, Count(AuditUploadAction, Upload.Replace("    continue-on-error: true\n      uses: actions/upload-artifact@v7\n      with:", "    uses: actions/upload-artifact@v7\n      with:", StringComparison.Ordinal)));
        Assert.Equal(1, Count(AuditUploadAction, Upload.Replace("overwrite: true", "overwrite: false", StringComparison.Ordinal)));
        Assert.Equal(1, Count(AuditUploadAction, Upload.Replace("      if: ${{ always() }}\n", "", StringComparison.Ordinal)));
        Assert.Equal(1, Count(AuditUploadAction, Upload.Replace("::warning::", "", StringComparison.Ordinal)));
        // One attempt, and nothing after it: no retry, and no warning.
        Assert.Equal(2, Count(AuditUploadAction, Upload[..Upload.IndexOf("    - if: ${{ always() && steps.attempt1", StringComparison.Ordinal)]));

        const string Download = """
            runs:
              using: composite
              steps:
                - id: attempt1
                  continue-on-error: true
                  uses: actions/download-artifact@v8
                - if: ${{ !cancelled() && steps.attempt1.outcome == 'failure' }}
                  shell: bash
                  run: sleep 30
                - id: attempt2
                  if: ${{ !cancelled() && steps.attempt1.outcome == 'failure' }}
                  uses: actions/download-artifact@v8
            """;
        Assert.Equal(0, Count(AuditDownloadAction, Download));
        Assert.Equal(1, Count(AuditDownloadAction, Download.Replace("    continue-on-error: true\n", "", StringComparison.Ordinal)));
        Assert.Equal(1, Count(AuditDownloadAction, Download.Replace(
            "      if: ${{ !cancelled() && steps.attempt1.outcome == 'failure' }}\n      uses:",
            "      if: ${{ !cancelled() && steps.attempt1.outcome == 'failure' }}\n      continue-on-error: true\n      uses:", StringComparison.Ordinal)));
        // One attempt: no retry, and that last attempt tolerated.
        Assert.Equal(2, Count(AuditDownloadAction, Download[..Download.IndexOf("    - if:", StringComparison.Ordinal)]));

        static (int, int, int) Workflows(string yaml)
        {
            var (u, d, offenders) = AuditWorkflow("w.yml", Lines(yaml));
            return (u, d, offenders.Count);
        }

        static int Count(Func<string[], List<string>> audit, string yaml) => audit(Lines(yaml)).Count;

        static string[] Lines(string yaml) => yaml.ReplaceLineEndings("\n").Split('\n');
    }

    /// <summary>The steps of a YAML file as blocks of lines, comments dropped: a step starts at a <c>- </c> item whose
    /// indentation is that of the first item under the file's first <c>steps:</c> key, and every <c>steps:</c> key of a
    /// workflow sits at the same depth (six spaces for the item), so one indentation reads them all.</summary>
    private static List<List<string>> Steps(string[] lines)
    {
        var code = lines.Where(l => l.Trim().Length > 0 && !l.TrimStart().StartsWith('#')).ToList();
        int key = code.FindIndex(l => l.Trim() == "steps:");
        var steps = new List<List<string>>();
        if (key < 0 || key + 1 >= code.Count) return steps;
        int depth = Indent(code[key + 1]);
        List<string>? step = null;
        foreach (string line in code.Skip(key + 1))
        {
            if (Indent(line) == depth && line.TrimStart().StartsWith("- ", StringComparison.Ordinal)) steps.Add(step = []);
            else if (Indent(line) <= depth) step = null;   // a shallower key ends the step: the next job, its `steps:`
            step?.Add(line);
        }
        return steps;
    }

    private static int Indent(string line) => line.Length - line.TrimStart().Length;

    /// <summary>A step's value for <paramref name="key"/> (<c>key: value</c> on one line), or null.</summary>
    private static string? Value(List<string> step, string key)
    {
        foreach (string line in step)
        {
            string t = line.TrimStart();
            if (t.StartsWith("- ", StringComparison.Ordinal)) t = t[2..];
            if (t.StartsWith(key + ":", StringComparison.Ordinal)) return t[(key.Length + 1)..].Trim();
        }
        return null;
    }

    private static string Name(List<string> step) => step[0].Trim();

    private static (int Uploads, int Downloads, List<string> Offenders) AuditWorkflow(string file, string[] lines)
    {
        var offenders = new List<string>();
        foreach (string line in lines.Where(l => !l.TrimStart().StartsWith('#')))
        {
            if (line.Contains(RawUpload, StringComparison.Ordinal) || line.Contains(RawDownload, StringComparison.Ordinal))
                offenders.Add($"  {file}: `{line.Trim()}` names the raw artifact action");
        }

        int uploads = 0, downloads = 0;
        foreach (var step in Steps(lines))
        {
            string? uses = Value(step, "uses");
            if (uses == UploadAction)
            {
                uploads++;
                if (Value(step, "if") != "${{ always() }}")
                    offenders.Add($"  {file}: {Name(step)}: an upload runs `if: ${{{{ always() }}}}`, so a red job's results are uploaded too");
            }
            else if (uses == DownloadAction)
            {
                downloads++;
            }
        }
        return (uploads, downloads, offenders);
    }

    private static List<List<string>> Attempts(List<List<string>> steps, string action)
        => steps.Where(s => Value(s, "uses")?.StartsWith(action + "@", StringComparison.Ordinal) == true).ToList();

    private static List<string> AuditUploadAction(string[] lines)
    {
        var offenders = new List<string>();
        var steps = Steps(lines);
        var attempts = Attempts(steps, RawUpload);
        if (attempts.Count < 2) offenders.Add($"  {attempts.Count} upload attempt(s): a transient failure needs a retry");
        for (int i = 0; i < attempts.Count; i++)
        {
            var a = attempts[i];
            if (Value(a, "continue-on-error") != "true")
                offenders.Add($"  attempt {i + 1}: no `continue-on-error: true`, so its failure fails the job");
            if (Value(a, "if")?.StartsWith("${{ always()", StringComparison.Ordinal) != true)
                offenders.Add($"  attempt {i + 1}: its `if:` is not `always()`-based, so it is skipped in a red job");
            if (i > 0 && Value(a, "overwrite") != "true")
                offenders.Add($"  attempt {i + 1}: a retry needs `overwrite: true` (a failed attempt can leave the artifact half-made)");
        }
        int last = attempts.Count == 0 ? -1 : steps.IndexOf(attempts[^1]);
        if (last < 0 || !steps.Skip(last + 1).Any(s => s.Any(l => l.Contains("::warning::", StringComparison.Ordinal))))
            offenders.Add("  no `::warning::` step after the last attempt: a lost artifact would pass unannounced");
        return offenders;
    }

    private static List<string> AuditDownloadAction(string[] lines)
    {
        var offenders = new List<string>();
        var attempts = Attempts(Steps(lines), RawDownload);
        if (attempts.Count < 2) offenders.Add($"  {attempts.Count} download attempt(s): a transient failure needs a retry");
        for (int i = 0; i < attempts.Count; i++)
        {
            bool tolerated = Value(attempts[i], "continue-on-error") == "true";
            if (i < attempts.Count - 1 && !tolerated)
                offenders.Add($"  attempt {i + 1}: no `continue-on-error: true`, so a transient failure fails the job before its retry");
            if (i == attempts.Count - 1 && tolerated)
                offenders.Add($"  attempt {i + 1} (the last): `continue-on-error: true` hides a download the consuming job needs");
        }
        return offenders;
    }
}
