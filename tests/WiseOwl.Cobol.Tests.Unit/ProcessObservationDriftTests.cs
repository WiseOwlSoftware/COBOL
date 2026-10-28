// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// Keeps the child-process observer collapsed to ONE implementation, and proves it can actually tell a
/// non-observation from a wrong answer.
/// </summary>
/// <remarks>
/// <para>
/// ⛔ THE DEFECT THIS GUARDS (plan §11 A12; DESIGN-test-build-ci.md §3.10). Six copies of "start
/// <c>dotnet</c>, wait N seconds, return whatever came back" existed across five test projects, and every one
/// of them, on a timeout, KILLED THE PROCESS AND RETURNED ITS PARTIAL OR EMPTY STDOUT. The caller then compared
/// that against a golden, so a contention timeout was indistinguishable from a semantic regression — which is
/// the mechanism behind two full Conformance runs on an identical tree returning 4159/4160 then 4160/4160.
/// </para>
/// <para>
/// The extraction alone is not the fix, exactly as <c>TestRepoDriftTests</c> records for the repo-root walkers:
/// writing a fifteen-line process launcher is cheaper in the moment than finding the one that already exists,
/// so the copies grow back one project at a time. This test is the half that makes it stay fixed
/// (<c>feedback_one_rule_one_place</c>, <c>feedback_scan_all_similar</c>).
/// </para>
/// </remarks>
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

    // ── The observer's own behaviour, proven rather than assumed ───────────────────────────────────────────

    /// <summary>The ordinary case still works: a process that finishes yields its real exit code and streams.</summary>
    [Fact]
    public void ACompletedProcess_IsObservedNormally()
    {
        var obs = ProcessObserver.ObserveOrThrow(ExitsWith(7));
        Assert.Equal(ProcessOutcome.Completed, obs.Outcome);
        Assert.Equal(7, obs.ExitCode);
    }

    /// <summary>stdout is captured, not merely awaited.</summary>
    [Fact]
    public void ACompletedProcess_YieldsItsStdout()
    {
        var obs = ProcessObserver.ObserveOrThrow(Echoes("HELLO-FROM-CHILD"));
        Assert.Contains("HELLO-FROM-CHILD", obs.Stdout, StringComparison.Ordinal);
    }

    /// <summary>
    /// ⛔ THE ONE THAT MATTERS. A process that never finishes must raise — never hand back an empty string that
    /// an <c>Assert.Equal(expected, stdout)</c> would report as a wrong answer.
    /// <c>feedback_green_gates_arent_evidence</c>: this is the guard failing once, on purpose, forever.
    /// </summary>
    [Fact]
    public void AProcessThatNeverFinishes_RaisesInsteadOfReturningEmptyOutput()
    {
        // A short budget so the test costs ~2×1s rather than 2×120s; the retry path is exercised either way.
        var ex = Assert.Throws<HarnessNonObservationException>(
            () => ProcessObserver.ObserveOrThrow(RunsForever(), budget: OneSecond));
        Assert.Contains("HARNESS NON-OBSERVATION", ex.Message, StringComparison.Ordinal);
        Assert.Contains("NOT a value mismatch", ex.Message, StringComparison.Ordinal);
        // Both attempts must be reported — a single-attempt message would mean the retry never ran.
        Assert.Contains("attempt1", ex.Message, StringComparison.Ordinal);
        Assert.Contains("attempt2", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>A process that cannot be started produced no observation either, and says which.</summary>
    [Fact]
    public void AProcessThatCannotStart_RaisesAsALaunchFailure()
    {
        var psi = new ProcessStartInfo(ScratchDirectory.Create("wiseowl-cobol-no-such-exe-"));
        var ex = Assert.Throws<HarnessNonObservationException>(() => ProcessObserver.ObserveOrThrow(psi, budget: OneSecond));
        Assert.Contains("LaunchFailed", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>The non-fatal probe reports rather than throws, so a caller that wants to inspect can.</summary>
    [Fact]
    public void Observe_ReportsATimeoutWithoutThrowing()
    {
        var obs = ProcessObserver.Observe(RunsForever(), null, OneSecond);
        Assert.Equal(ProcessOutcome.TimedOut, obs.Outcome);
        // ⛔ And it is EMPTY on purpose: the partial stdout of a killed run is exactly what must never reach a
        // comparison. If this ever starts returning content, the A12 defect is back.
        Assert.Equal("", obs.Stdout);
    }

    // ── The retry of a program that WRITES (kb/Work PB3388) ────────────────────────────────────────────────
    //
    // The retry policy is exercised through ProcessObserver.ObserveWithRetry with an IN-PROCESS attempt, not a child
    // process under a short budget: a real child killed at a literal 1 s budget makes WHICH attempt a test observes a
    // function of host load (kb/Work PB1590, PB3022). The first attempt here does what a killed COBOL program leaves
    // behind (writes into the run directory, then reports TimedOut); the second reports the run directory it found.
    // That is NIST IX216A's failure in miniature: its OPEN EXTEND of an absent OPTIONAL file answered 00 on the retry,
    // because the killed attempt had created the file (ISO/IEC 1989:2023 §14.9.27.4 GR17: an absent OPTIONAL file is
    // created and the status is 05). The process-level wiring (Observe over a real child) is covered by the
    // never-finishes tests above; the three call sites that run a program are pinned by the drift test below.

    /// <summary>What the attempts of one probe saw: the retry's view of the run directory.</summary>
    private sealed class WritingAttempts(string run, bool touchBig)
    {
        public int Calls;
        public bool CreatedFileSeenByRetry;
        public string? DataSeenByRetry;

        public ProcessObservation Attempt()
        {
            if (++Calls == 1)
            {
                // The killed first attempt's leftovers: a created file, a file rewritten in place, and (optionally) a
                // change to a file over the copy bound.
                File.WriteAllText(Path.Combine(run, "created.txt"), "x");
                File.WriteAllText(Path.Combine(run, "data.txt"), "after");
                if (touchBig) File.AppendAllText(Path.Combine(run, "big.bin"), "x");
                return new ProcessObservation(ProcessOutcome.TimedOut, -1, "", "", "killed at the budget");
            }
            CreatedFileSeenByRetry = File.Exists(Path.Combine(run, "created.txt"));
            DataSeenByRetry = File.ReadAllText(Path.Combine(run, "data.txt"));
            return new ProcessObservation(ProcessOutcome.Completed, CreatedFileSeenByRetry ? 9 : 0, "", "", "");
        }
    }

    /// <summary>A run directory holding what a chain predecessor leaves a program: a data file the killed attempt
    /// rewrites, a subdirectory, and a file over the copy bound (the runtime assembly's stand-in).</summary>
    private static (string outer, string run) WritingProbeDirectories()
    {
        string outer = ScratchDirectory.Create("WiseOwlCobol_RetryProbe_");
        string run = Path.Combine(outer, "run");
        Directory.CreateDirectory(Path.Combine(run, "sub"));
        File.WriteAllText(Path.Combine(run, "data.txt"), "before");
        File.WriteAllText(Path.Combine(run, "sub", "kept.txt"), "kept");
        File.WriteAllBytes(Path.Combine(run, "big.bin"), new byte[RunDirectoryState.MaxCopiedFileBytes + 1]);
        return (outer, run);
    }

    /// <summary>
    /// ⛔ THE ONE THAT PINS PB3388. The killed first attempt created <c>created.txt</c> and rewrote <c>data.txt</c>; the
    /// retry must find neither and the directory must end as it began. A file over the copy bound that no attempt
    /// touched does not refuse the retry.
    /// </summary>
    [Fact]
    public void ARetryOfAProgramThatWrites_StartsFromTheStateTheFirstAttemptStartedFrom()
    {
        var (outer, run) = WritingProbeDirectories();
        try
        {
            var attempts = new WritingAttempts(run, touchBig: false);
            var obs = ProcessObserver.ObserveWithRetry("probe", OneSecond, run, attempts.Attempt);

            Assert.Equal(2, attempts.Calls);
            Assert.Equal(0, obs.ExitCode);   // 9 = the retry found what the killed attempt created
            Assert.False(attempts.CreatedFileSeenByRetry, "a file the killed attempt created is deleted before the retry");
            Assert.Equal("before", attempts.DataSeenByRetry);   // rewritten in place by the killed attempt, restored
            Assert.False(File.Exists(Path.Combine(run, "created.txt")));
            Assert.Equal("kept", File.ReadAllText(Path.Combine(run, "sub", "kept.txt")));
            Assert.Equal(RunDirectoryState.MaxCopiedFileBytes + 1, new FileInfo(Path.Combine(run, "big.bin")).Length);
        }
        finally { ScratchDirectory.Remove(outer); }
    }

    /// <summary>
    /// The exception path of the restoration. A file over the copy bound that the killed attempt CHANGED cannot be put
    /// back, and the retry is then not made: a non-observation that names the file, never a value measured over a
    /// state that is not the first attempt's.
    /// </summary>
    [Fact]
    public void ARetryWhoseRunDirectoryCannotBeRestored_RaisesInsteadOfMeasuringTheWrongState()
    {
        var (outer, run) = WritingProbeDirectories();
        try
        {
            var attempts = new WritingAttempts(run, touchBig: true);
            var ex = Assert.Throws<HarnessNonObservationException>(
                () => ProcessObserver.ObserveWithRetry("probe", OneSecond, run, attempts.Attempt));

            Assert.Equal(1, attempts.Calls);   // the retry is not made
            Assert.Contains("HARNESS NON-OBSERVATION", ex.Message, StringComparison.Ordinal);
            Assert.Contains("cannot be restored", ex.Message, StringComparison.Ordinal);
            Assert.Contains("big.bin", ex.Message, StringComparison.Ordinal);
            Assert.Contains("attempt2: not run", ex.Message, StringComparison.Ordinal);
        }
        finally { ScratchDirectory.Remove(outer); }
    }

    /// <summary>A caller that names no run directory (a tool run in the repository, which is not the child's to
    /// own) is NOT restored: the retry sees what the killed attempt left. The two behaviours are the caller's choice,
    /// stated at the call, so the test pins that the opt-in is what restores.</summary>
    [Fact]
    public void ARetryWithoutARunDirectory_LeavesTheDirectoryAlone()
    {
        var (outer, run) = WritingProbeDirectories();
        try
        {
            var attempts = new WritingAttempts(run, touchBig: false);
            var obs = ProcessObserver.ObserveWithRetry("probe", OneSecond, runDirectory: null, attempts.Attempt);

            Assert.Equal(9, obs.ExitCode);
            Assert.True(File.Exists(Path.Combine(run, "created.txt")));
        }
        finally { ScratchDirectory.Remove(outer); }
    }

    /// <summary>The abnormal arm of the retry itself: a retry that also observes nothing raises, and the directory it
    /// ran over is the restored one, not the first attempt's leftovers.</summary>
    [Fact]
    public void ARetryThatAlsoTimesOut_RaisesAfterRestoringTheDirectory()
    {
        var (outer, run) = WritingProbeDirectories();
        try
        {
            int calls = 0;
            string? createdSeenByRetry = null;
            var ex = Assert.Throws<HarnessNonObservationException>(() => ProcessObserver.ObserveWithRetry("probe", OneSecond, run, () =>
            {
                if (++calls == 1) File.WriteAllText(Path.Combine(run, "created.txt"), "x");
                else createdSeenByRetry = File.Exists(Path.Combine(run, "created.txt")).ToString();
                return new ProcessObservation(ProcessOutcome.TimedOut, -1, "", "", "killed at the budget");
            }));

            Assert.Equal(2, calls);
            Assert.Equal("False", createdSeenByRetry);
            Assert.Contains("attempt2", ex.Message, StringComparison.Ordinal);
        }
        finally { ScratchDirectory.Remove(outer); }
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

    // ── The budget rule (kb/Work PB3108) ─────────────────────────────────────────────────────────────────────
    //
    // The budget scales with the work a process was started to do: one unit for the process, one per serial round of
    // its children. The two observation tests below put ONE slow child (it sleeps about 3 s) under a 1 s unit: as one
    // process it is killed (it cannot exit in 1 s, so that direction holds on any host), and as a process stated to run
    // children it gets at least the harness unit, so that direction is exactly as robust as every observation the
    // harness makes. Neither direction is a time expectation of this test.

    /// <summary>The unit these tests choose: short, so a child that never finishes costs about 2×1 s.</summary>
    private static readonly ObservationBudget OneSecond = ObservationBudget.For(1_000, children: 0, parallel: 1, "a probe");

    /// <summary>The rule as a table: units = 1 + ceil(children / parallel), and the budget is units × unit. The
    /// 78-process row is the conformance suite's run.py at 8 jobs (39 tests, two processes each, 2026-10-22).</summary>
    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(1, 1, 2)]
    [InlineData(1, 8, 2)]
    [InlineData(8, 8, 2)]
    [InlineData(9, 8, 3)]
    [InlineData(24, 8, 4)]
    [InlineData(78, 8, 11)]
    [InlineData(78, 1, 79)]
    [InlineData(2_000, 8, 251)]
    public void TheBudget_ScalesWithTheWork(int children, int parallel, long units)
    {
        var budget = ObservationBudget.For(1_000, children, parallel, "a table row");
        Assert.Equal(units, budget.Units);
        Assert.Equal(units * 1_000, budget.Ms);
        Assert.Contains($"{units} unit(s) of 1000 ms", budget.ToString(), StringComparison.Ordinal);
        Assert.Contains("a table row", budget.ToString(), StringComparison.Ordinal);
    }

    /// <summary>The edges of the rule: one process is one harness unit; a budget past an int clamps (WaitForExit takes
    /// an int) rather than wrapping to a negative wait; and a parallelism below one, a negative child count, a unit
    /// below one or a budget that names no work is refused.</summary>
    [Fact]
    public void TheBudget_IsOneUnitForOneProcess_ClampsAtItsLimit_AndRefusesNonsense()
    {
        Assert.Equal(ObservationBudget.UnitMs, ObservationBudget.OneProcess.Ms);
        Assert.Equal(1, ObservationBudget.OneProcess.Units);
        Assert.Equal(int.MaxValue, ObservationBudget.For(int.MaxValue, children: int.MaxValue, parallel: 1, "huge").Ms);
        Assert.Throws<ArgumentOutOfRangeException>(() => ObservationBudget.For(1_000, children: 1, parallel: 0, "x"));
        Assert.Throws<ArgumentOutOfRangeException>(() => ObservationBudget.For(1_000, children: -1, parallel: 1, "x"));
        Assert.Throws<ArgumentOutOfRangeException>(() => ObservationBudget.For(0, children: 0, parallel: 1, "x"));
        Assert.Throws<ArgumentException>(() => ObservationBudget.For(1_000, children: 0, parallel: 1, " "));
    }

    /// <summary>A slow process stated to run children is observed: its budget is at least the harness unit.</summary>
    [Fact]
    public void ASlowProcess_UnderTheBudgetOfItsWork_IsObserved()
    {
        var work = ObservationBudget.For(1_000, children: Math.Max(1, ObservationBudget.UnitMs / 1_000), parallel: 1,
            "a stand-in for a script that runs one child after another");
        Assert.True(work.Ms > ObservationBudget.UnitMs);
        var obs = ProcessObserver.ObserveOrThrow(SleepsAFewSeconds(), budget: work);
        Assert.Equal(ProcessOutcome.Completed, obs.Outcome);
        Assert.Equal(0, obs.ExitCode);
    }

    /// <summary>
    /// ⛔ The same slow process under ONE unit is a non-observation, and its report carries what PB3108 asked for: the
    /// budget, the work it covered, the process's own processor time and the host's load across each attempt.
    /// </summary>
    [Fact]
    public void ASlowProcess_UnderOneUnit_IsANonObservationThatNamesItsWorkAndTheHostLoad()
    {
        var ex = Assert.Throws<HarnessNonObservationException>(
            () => ProcessObserver.ObserveOrThrow(SleepsAFewSeconds(), budget: OneSecond));
        Assert.Contains("budget  : 1000 ms for a probe (1 unit(s) of 1000 ms", ex.Message, StringComparison.Ordinal);
        Assert.Contains("of processor time itself", ex.Message, StringComparison.Ordinal);
        Assert.Contains($"host: {Environment.ProcessorCount} logical processor(s)", ex.Message, StringComparison.Ordinal);
        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
            Assert.Contains("% busy across the attempt", ex.Message, StringComparison.Ordinal);
        if (OperatingSystem.IsLinux())
            Assert.Contains("load average", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>The load line's arithmetic, and its honest arm: the busy share is 1 - idle/total across the two
    /// samples, and a host with no reading says so instead of reporting a number.</summary>
    [Fact]
    public void TheHostLoadLine_IsTheBusyShareAcrossTheAttempt_OrSaysItHasNone()
    {
        Assert.Contains("75 % busy across the attempt",
            HostLoad.Describe(new HostCpuTimes(Idle: 100, Total: 1_000), new HostCpuTimes(Idle: 125, Total: 1_100)),
            StringComparison.Ordinal);
        Assert.Contains("processor use not readable on this host", HostLoad.Describe(null, null), StringComparison.Ordinal);
        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
            Assert.NotNull(HostLoad.Sample());
    }

    /// <summary>A non-observation names the command a caller built with <c>ArgumentList</c>: before PB3108 the log read a
    /// bare <c>python</c> for every script, run.py included.</summary>
    [Fact]
    public void TheCommandLine_IncludesAnArgumentList()
    {
        var psi = new ProcessStartInfo("python");
        foreach (string a in new[] { "run.py", "--out", "a b" }) psi.ArgumentList.Add(a);
        Assert.Equal("python run.py --out \"a b\"", ProcessObserver.CommandLine(psi));
        Assert.Equal("cmd.exe /c exit", ProcessObserver.CommandLine(new ProcessStartInfo("cmd.exe", "/c exit")));
    }

    private static readonly System.Text.RegularExpressions.Regex RunsDotnet =
        new(@"new ProcessStartInfo\(\s*""dotnet""", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static readonly System.Text.RegularExpressions.Regex ObserveCall =
        new(@"ObserveOrThrow\([^;]*\);", System.Text.RegularExpressions.RegexOptions.Compiled);

    // ⛔ THE SHELL COMMANDS ARE WRITTEN PER-OS, NOT TRANSLATED. The first version of this helper took one
    // cmd.exe string and string-replaced `>nul` for `>/dev/null` on POSIX — which left `exit /b 7` intact, and
    // `/bin/sh` does not accept it. The Windows leg was green, the Linux CI leg was red, and the defect was a
    // TRANSLATION that only covered the token I happened to think of. A cross-platform helper validated on one
    // platform is not cross-platform (`feedback_wsl_linux_repro`: build on Windows, RUN under WSL).
    private static ProcessStartInfo Shell(string windows, string posix) =>
        OperatingSystem.IsWindows()
            ? new ProcessStartInfo("cmd.exe", $"/c {windows}")
            : new ProcessStartInfo("/bin/sh", $"-c \"{posix}\"");

    /// <summary>A process that terminates immediately with a chosen exit code.</summary>
    private static ProcessStartInfo ExitsWith(int code) => Shell($"exit /b {code}", $"exit {code}");

    /// <summary>A process that writes one marker line to stdout and exits.</summary>
    private static ProcessStartInfo Echoes(string marker) => Shell($"echo {marker}", $"echo {marker}");

    /// <summary>A process that sleeps about 3 s and exits 0: longer than one 1 s unit on any host.</summary>
    private static ProcessStartInfo SleepsAFewSeconds() => Shell("ping -n 4 127.0.0.1 >nul", "sleep 3");

    /// <summary>A process that does not finish within any budget this test would set.</summary>
    private static ProcessStartInfo RunsForever() =>
        Shell("ping -n 30 127.0.0.1 >nul", "sleep 30");
}
