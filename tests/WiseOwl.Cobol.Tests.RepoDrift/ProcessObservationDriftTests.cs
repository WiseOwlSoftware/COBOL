// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.RepoDrift;

/// <summary>
/// Keeps the child-process observer collapsed to ONE implementation across every test source (plan §11 A12;
/// DESIGN-test-build-ci.md §3.10): no test source runs a program under a private timeout, and every run of a compiled
/// program names its run directory. These sweeps read every C# source under <c>tests/</c>, so they run here (kb/Work
/// PB5230); the observer's own behaviour, proven rather than assumed, is Unit's <c>ProcessObservationDriftTests</c>.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ProcessObservationDriftTests
{
    /// <summary>The tell: a test source that both starts a process AND imposes its own bounded wait. Either
    /// half alone stays legal — a generator drift test that shells out to <c>pwsh</c> and waits is answering a
    /// different question than "what did the program under test print".</summary>
    private const string Launch = "Process.Start(";

    /// <summary>
    /// A BOUNDED wait is <c>WaitForExit(</c> with any argument; the argument-less <c>WaitForExit()</c> blocks
    /// until the child really exits and fabricates nothing, so it is not the defect.
    /// <para>⚠ The first draft of this test carried a hand-written list of numeric prefixes
    /// (<c>WaitForExit(1</c>, <c>WaitForExit(3</c>, …), which would have blessed <c>WaitForExit(45000)</c>
    /// silently — a hand-maintained list where a predicate belongs (CLAUDE.md rule 5). The regex IS the rule.</para>
    /// </summary>
    private static readonly System.Text.RegularExpressions.Regex BoundedWait =
        new(@"WaitForExit\s*\(\s*[^)\s]", System.Text.RegularExpressions.RegexOptions.Compiled);

    // The observer itself and this test (which must quote the needles). The two generator drift tests that were
    // exempt here no longer impose a bounded wait (kb/Work PB3022: no fixed time limit in a test), so they no longer
    // need to be; named individually so that a new exemption is a deliberate edit here, not a silent inheritance.
    private static readonly string[] Exempt =
    [
        "ProcessObservation.cs", "ProcessObservationDriftTests.cs",
    ];

    /// <summary>The test sources this sweep reads: the COMMITTED C# under <c>tests/</c> (kb/Work PB3814), never a walk
    /// of the directory from the file system, which enters every running test host's <c>bin/</c> and <c>obj/</c>.</summary>
    private static readonly TreeScope TestSourceScope = new(Under: ["tests/"], Kinds: [".cs"]);

    private static IEnumerable<string> TestSources() =>
        TrackedTree.Files(TestSourceScope).Select(rel => TestRepo.At(rel.Split('/')))
            .Where(f => !Exempt.Contains(Path.GetFileName(f)));

    [Fact]
    public void NoTestSource_RunsAProgramUnderItsOwnTimeout()
    {
        var offenders = new List<string>();
        foreach (string file in TestSources())
        {
            string body = File.ReadAllText(file);
            if (!body.Contains(Launch, StringComparison.Ordinal)) continue;
            var wait = BoundedWait.Match(body);
            if (wait.Success)
                offenders.Add($"{Path.GetRelativePath(TestRepo.Root, file)}  ({Launch} … {wait.Value}…)");
        }

        Assert.True(offenders.Count == 0,
            $"{offenders.Count} test source(s) run a child process under a private timeout instead of "
            + $"WiseOwl.Cobol.Tests.Shared.ProcessObserver:{Environment.NewLine}"
            + string.Join(Environment.NewLine, offenders.Select(o => "    " + o))
            + $"{Environment.NewLine}A private timeout returns PARTIAL OUTPUT on expiry, which the caller then "
            + "compares against a golden — a contention timeout reported as a value mismatch (plan §11 A12). "
            + "Use ProcessObserver.ObserveOrThrow — see tests/_shared/ProcessObservation.cs.");
    }

    /// <summary>
    /// ⛔ THE ARM THAT KEEPS THE FIX TOTAL (CLAUDE.md rule 4's two-arm dispatch). The retry restores a directory only
    /// for a caller that NAMES it, so a test source that runs a compiled program (<c>new ProcessStartInfo("dotnet", …)</c>)
    /// and observes it without <c>runDirectory:</c> would leave the IX216A failure open for that one call site. Every
    /// <c>ObserveOrThrow</c> call in a file that launches <c>dotnet</c> names the run directory.
    /// </summary>
    [Fact]
    public void EveryRunOfACompiledProgram_NamesItsRunDirectory()
    {
        var offenders = new List<string>();
        foreach (string file in TestSources())
        {
            string body = File.ReadAllText(file);
            if (!RunsDotnet.IsMatch(body)) continue;
            foreach (System.Text.RegularExpressions.Match call in ObserveCall.Matches(body))
                if (!call.Value.Contains("runDirectory:", StringComparison.Ordinal))
                    offenders.Add($"{Path.GetRelativePath(TestRepo.Root, file)}:  {call.Value}");
        }
        Assert.True(offenders.Count == 0,
            "a compiled program is observed without naming its run directory, so a retry after a contention timeout runs "
            + "over the killed attempt's files and measures another input (kb/Work PB3388, IX216A):\n"
            + string.Join("\n", offenders));

        // The patterns catch the shapes they forbid and pass the one they require.
        Assert.Matches(RunsDotnet, "var psi = new ProcessStartInfo(\"dotnet\", $\"\\\"{dll}\\\"\") { WorkingDirectory = dir };");
        Assert.Matches(ObserveCall, "ProcessObserver.ObserveOrThrow(psi, stdin);");
        Assert.DoesNotContain("runDirectory:", ObserveCall.Match("ProcessObserver.ObserveOrThrow(psi, stdin);").Value, StringComparison.Ordinal);
        Assert.Contains("runDirectory:", ObserveCall.Match("ProcessObserver.ObserveOrThrow(psi, stdin, runDirectory: dir);").Value, StringComparison.Ordinal);
    }

    private static readonly System.Text.RegularExpressions.Regex RunsDotnet =
        new(@"new ProcessStartInfo\(\s*""dotnet""", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static readonly System.Text.RegularExpressions.Regex ObserveCall =
        new(@"ObserveOrThrow\([^;]*\);", System.Text.RegularExpressions.RegexOptions.Compiled);
}
