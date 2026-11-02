// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using System.Text;

namespace WiseOwl.Cobol.Tests.Shared;

/// <summary>
/// THE child-process observer for every test project — one rule, one place, and the rule is:
/// <b>a missing observation is not a negative observation.</b>
/// </summary>
/// <remarks>
/// <para>
/// ⛔ WHY THIS TYPE EXISTS (plan §11 A12; DESIGN-test-build-ci.md §3.10). Six separate copies of "start
/// <c>dotnet</c>, wait 30 seconds, return whatever came back" existed across the test tree
/// (<c>CutRunner.RunExit</c>, <c>AcceptDifferentialTests.AcceptRun</c>, <c>WiseOwlCobolTestBase.CompileAndRun</c>,
/// and three in the legacy <c>EndToEndTestBase</c>). Every one of them handled a timeout the same wrong way:
/// kill the process, return <i>partial or empty stdout</i>, and let the caller compare it against a golden.
/// A contention timeout therefore reported as a VALUE MISMATCH — indistinguishable from a semantic
/// regression. That is the mechanism behind the observation §11 A12 was opened for: two full Conformance runs
/// on an IDENTICAL tree returned 4159/4160 then 4160/4160, and the named test passed in isolation.
/// </para>
/// <para>
/// The fix is not a longer timeout — it is refusing to manufacture a value out of a run that never finished.
/// A process that timed out, was killed, or failed to launch produced NO OBSERVATION, and this type raises
/// <see cref="HarnessNonObservationException"/> rather than hand a caller something to compare. No assertion
/// anywhere can mistake that for a wrong answer, whatever it chose to assert on.
/// </para>
/// <para>
/// ⚖ ON THE RETRY. <see cref="ObserveOrThrow"/> re-attempts once before giving up, and that is NOT re-rolling
/// a failed assertion — it is re-attempting a MEASUREMENT THAT DID NOT COMPLETE. Nothing about the compiler
/// was learned on the first attempt, so there is no result being discarded. The retry is serialized behind
/// <see cref="RetryGate"/> so it runs with the least contention the run can offer, which is the whole point
/// given that contention is the named cause. Every retry is recorded (see <see cref="HarnessLog"/>) so the rate
/// is measurable rather than assumed — a silent retry would trade one blind spot for another.
/// </para>
/// <para>
/// ⚖ ON THE BUDGET (kb/Work PB3108). An attempt is bounded by the work it was started to do, never by one fixed number:
/// <see cref="ObservationBudget"/> gives one harness unit to a process plus one per serial round of the child processes
/// it runs, so a script that compiles a whole suite is not killed by a budget sized for one program. A timeout reports
/// the budget, the work it covered, the process's own processor time and the host's load across the attempt
/// (<see cref="HostLoad"/>), so a reader can tell a starved process from a hung one.
/// </para>
/// <para>
/// ⛔ A RETRY OF A PROGRAM THAT WRITES STARTS FROM THE FIRST ATTEMPT'S STATE, NOT FROM ITS WRECKAGE (kb/Work PB3388).
/// "Nothing was learned on the first attempt" holds only for a child that left nothing behind. A compiled COBOL
/// program killed at the budget has already created, appended to and rewritten files in its run directory, and a retry
/// over those files measures a different input: NIST IX216A's OPEN EXTEND of an absent OPTIONAL file answered 00 (file
/// present, created by the killed attempt) where ISO/IEC 1989:2023 §14.9.27.4 GR17 requires 05, one red in 17,812.
/// A caller that runs a program in a directory names it (<c>runDirectory</c>); the observer records that directory
/// before the first attempt and restores it before the retry (<see cref="RunDirectoryState"/>), or raises the
/// non-observation when it cannot.
/// </para>
/// <para>
/// ⛔ A KILLED PROCESS HAS NOT NECESSARILY EXITED (kb/Work PB4388). A process killed inside a blocking system call (a
/// <c>MoveFileEx</c> on a loaded ReFS volume, kb/Work PB4387) can outlive the wait after its kill, and until it exits it
/// holds the program's files. The wait's result is therefore read: <see cref="ProcessOutcome.SurvivedKill"/> says the
/// process was still alive, and the retry waits for it (<see cref="ProcessObserver.SurvivorWaitMs"/>) before it restores
/// the run directory, or raises the non-observation naming its id, so a transient hold ends in a retried observation
/// and a lasting one in a report that names its cause.
/// </para>
/// </remarks>
internal enum ProcessOutcome
{
    /// <summary>The process ran to completion and its exit code and streams are real.</summary>
    Completed,
    /// <summary>The process exceeded its wall-clock budget and was killed. Its output is TRUNCATED, not wrong.</summary>
    TimedOut,
    /// <summary>The process exceeded its budget, was killed, and was STILL ALIVE when the wait after the kill ended
    /// (<see cref="ProcessObserver.KillWaitMs"/>). Everything <see cref="TimedOut"/> says holds, and so does this: a
    /// process that has not exited still holds its files and its processor. <see cref="ProcessObservation.Survivor"/>
    /// names it (kb/Work PB4388).</summary>
    SurvivedKill,
    /// <summary>The process could not be started at all. Nothing about the program under test was observed.</summary>
    LaunchFailed,
}

/// <summary>One completed observation of a child process — or an explicit statement that none was made.</summary>
/// <param name="Survivor">The killed process that had not exited when the wait after its kill ended; set only for
/// <see cref="ProcessOutcome.SurvivedKill"/> (kb/Work PB4388).</param>
internal readonly record struct ProcessObservation(
    ProcessOutcome Outcome, int ExitCode, string Stdout, string Stderr, string Detail, SurvivingProcess? Survivor = null)
{
    public bool Completed => Outcome == ProcessOutcome.Completed;
}

/// <summary>
/// A process the observer killed that was still running when it stopped waiting for it (kb/Work PB4388). A process
/// killed inside a blocking system call (a <c>MoveFileEx</c> on a loaded ReFS volume, kb/Work PB4387) can outlive the wait
/// that follows its kill, and until it exits it still holds the program's files, so a retry started over them is refused
/// for a reason that is not the program's. It is held by process id, not by <see cref="Process"/>: the observation
/// outlives the <c>Process</c> object that <see cref="ProcessObserver.Observe"/> disposes.
/// </summary>
internal sealed class SurvivingProcess(int id, Func<int, int, bool> waitForExit)
{
    /// <summary>The operating-system process id.</summary>
    public int Id { get; } = id;

    /// <summary>Wait up to <paramref name="milliseconds"/> for the process to exit; true when it has.</summary>
    public bool WaitForExit(int milliseconds) => waitForExit(Id, milliseconds);

    /// <summary>Wait for the process <paramref name="id"/> by id. A process that cannot be found has exited, and the id
    /// of a process that is still running is never reused.</summary>
    internal static bool WaitForExitById(int id, int milliseconds)
    {
        try
        {
            using var process = Process.GetProcessById(id);
            return process.WaitForExit(milliseconds);
        }
        catch (ArgumentException) { return true; }          // no process has this id: it has exited
    }
}

/// <summary>
/// Raised when a child process produced NO observation. Distinct from every assertion failure by TYPE and by
/// message, so a contention red can never be read as a semantic one — in a log, in a CI summary, or by a
/// reviewer skimming a verdict line.
/// </summary>
internal sealed class HarnessNonObservationException(string message) : Exception(message);

internal static class ProcessObserver
{
    /// <summary>Serializes RETRIES only. The first attempt of every test still runs fully parallel, so the
    /// suite pays nothing in the normal case; a retry gets the quietest machine the run can offer.</summary>
    private static readonly SemaphoreSlim RetryGate = new(1, 1);

    private static long _attempts, _retries, _nonObservations;

    /// <summary>Attempt one observation within <paramref name="budget"/> (<see cref="ObservationBudget"/>: the work
    /// the process was started to do). Never throws for a process-level failure — it REPORTS it, and a timeout's
    /// report says how much processor time the process used and how loaded the host was across the attempt, so the
    /// next reader can tell load from a hang (kb/Work PB3108).</summary>
    public static ProcessObservation Observe(ProcessStartInfo psi, string? stdinText, ObservationBudget budget)
    {
        Interlocked.Increment(ref _attempts);
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.RedirectStandardInput = true;
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;
        // The standard display device writes UTF-8 (CONFORMANCE.md item 59; the PB59 repertoire-identity
        // landing) — decode the capture the same way, or a wide character in a golden reads back as OEM
        // mojibake (Ω arrived as '╬⌐' before this line existed).
        psi.StandardOutputEncoding = new UTF8Encoding(false);
        psi.StandardErrorEncoding = new UTF8Encoding(false);
        // The ACCEPT device decodes a redirected standard input as UTF-8 (CONFORMANCE.md item 1; kb/Work PB3193), so
        // the harness encodes what it pipes the same way; left unset, .NET on Windows encodes it in the console's input
        // code page. BOM-less, or the first record of every run would begin with U+FEFF.
        psi.StandardInputEncoding = new UTF8Encoding(false);

        HostCpuTimes? hostAtStart = HostLoad.Sample();
        Process? proc;
        try
        {
            proc = Process.Start(psi);
        }
        catch (Exception e)                                     // Win32Exception, IOException, …
        {
            return new ProcessObservation(ProcessOutcome.LaunchFailed, -1, "", "",
                $"could not start '{CommandLine(psi)}': {e.GetType().Name}: {e.Message}");
        }
        if (proc is null)
            return new ProcessObservation(ProcessOutcome.LaunchFailed, -1, "", "",
                $"Process.Start returned null for '{CommandLine(psi)}'");

        using (proc)
        {
            // Read both streams asynchronously BEFORE waiting: a child that fills a redirected pipe blocks
            // forever otherwise, which would present as a timeout that is really our own deadlock.
            var outTask = proc.StandardOutput.ReadToEndAsync();
            var errTask = proc.StandardError.ReadToEndAsync();
            try
            {
                if (stdinText is not null) proc.StandardInput.Write(stdinText);
            }
            catch (IOException) { /* the child closed stdin first — legitimate, and not our observation */ }
            try { proc.StandardInput.Close(); } catch (IOException) { /* same */ }

            if (!proc.WaitForExit(budget.Ms))
            {
                // Read before the kill: what the process itself had used, and what the host was doing meanwhile.
                string used = OwnProcessorTime(proc);
                string host = HostLoad.Describe(hostAtStart, HostLoad.Sample());
                try { proc.Kill(entireProcessTree: true); } catch { /* already gone */ }
                // ⛔ The partial stdout is DELIBERATELY NOT RETURNED as output. Handing a caller a truncated
                // report is what turned a contention timeout into a golden mismatch in the first place.
                return ReportKilled(proc.Id, budget.Ms, $"{used}; {host}", proc.WaitForExit, SurvivingProcess.WaitForExitById);
            }
            return new ProcessObservation(ProcessOutcome.Completed, proc.ExitCode,
                outTask.Result, errTask.Result, "");
        }
    }

    /// <summary>How long <see cref="Observe"/> waits for a process to exit after it kills it; it stops waiting then, and
    /// <see cref="ReportKilled"/> says whether the process had exited.</summary>
    internal const int KillWaitMs = 5_000;

    /// <summary>How long <see cref="ObserveWithRetry"/> waits for a process that survived its kill to exit before it
    /// refuses the retry, naming the process (kb/Work PB4388).</summary>
    internal const int SurvivorWaitMs = 60_000;

    /// <summary>The observation of a process that exceeded its budget and has just been killed. A kill is a request, and
    /// the process is dead only when it has exited: <paramref name="waitForExit"/> waits <see cref="KillWaitMs"/> for that,
    /// and its result decides what the observation may say (kb/Work PB4388). A process that exited is
    /// <see cref="ProcessOutcome.TimedOut"/>; one that did not is <see cref="ProcessOutcome.SurvivedKill"/>, which says so
    /// and carries the process, so the retry can wait for it before it touches the files it holds.</summary>
    /// <param name="id">The killed process's id.</param>
    /// <param name="budgetMs">The budget it exceeded.</param>
    /// <param name="measured">What was measured before the kill: its processor time and the host's load.</param>
    /// <param name="waitForExit">Waits up to the given milliseconds for the process to exit; true when it has.</param>
    /// <param name="waitForExitById">Waits for a process by id once its <c>Process</c> object is gone: the survivor's wait.</param>
    internal static ProcessObservation ReportKilled(int id, int budgetMs, string measured,
        Func<int, bool> waitForExit, Func<int, int, bool> waitForExitById)
    {
        if (waitForExit(KillWaitMs))
            return new ProcessObservation(ProcessOutcome.TimedOut, -1, "", "",
                $"process did not exit within {budgetMs} ms and was killed; {measured}");
        return new ProcessObservation(ProcessOutcome.SurvivedKill, -1, "", "",
            $"process {id} did not exit within {budgetMs} ms and was STILL ALIVE {KillWaitMs} ms after it was killed; {measured}",
            new SurvivingProcess(id, waitForExitById));
    }

    /// <summary>
    /// Observe, re-attempting once (serialized) if the first attempt observed nothing, and raising
    /// <see cref="HarnessNonObservationException"/> if the second attempt observes nothing either. Callers get
    /// a real exit code and real streams, or an exception — never a fabricated value.
    /// </summary>
    /// <param name="budget">The work the process was started to do (<see cref="ObservationBudget"/>); null is
    /// <see cref="ObservationBudget.OneProcess"/>, the budget of one program or one script. A process that runs child
    /// processes of its own states them (<see cref="ObservationBudget.ForChildren"/>), kb/Work PB3108.</param>
    /// <param name="runDirectory">The directory the child reads and writes its files in, when it is a program under
    /// test (a compiled COBOL program run in its own scratch directory). Its state is captured before the first
    /// attempt and restored before a retry, so the retry measures the same input the first attempt had
    /// (<see cref="RunDirectoryState"/>, kb/Work PB3388). Leave it null for a tool run in a directory that is not
    /// the child's to own (git or python in the repository).</param>
    public static ProcessObservation ObserveOrThrow(ProcessStartInfo psi, string? stdinText = null,
        ObservationBudget? budget = null, string? runDirectory = null)
    {
        ObservationBudget within = budget ?? ObservationBudget.OneProcess;
        return ObserveWithRetry(CommandLine(psi), within, runDirectory, () => Observe(psi, stdinText, within));
    }

    /// <summary>The command line as the log and the non-observation name it: <see cref="ProcessStartInfo.Arguments"/>
    /// or, for a caller that built <see cref="ProcessStartInfo.ArgumentList"/>, that list (each argument quoted when it
    /// holds a space). Reading <c>Arguments</c> alone logged a bare <c>python</c> for every script PythonInstrument
    /// runs, so a non-observation of run.py did not say it was run.py (kb/Work PB3108).</summary>
    internal static string CommandLine(ProcessStartInfo psi)
    {
        IEnumerable<string> args = psi.ArgumentList.Count > 0
            ? psi.ArgumentList.Select(a => a.Contains(' ', StringComparison.Ordinal) ? $"\"{a}\"" : a)
            : [psi.Arguments];
        return string.Join(" ", args.Prepend(psi.FileName).Where(s => s.Length > 0));
    }

    /// <summary>The processor time the process itself has used (its children are not counted), for a timeout's report:
    /// near the budget means it was spinning, near zero that it was blocked or starved.</summary>
    private static string OwnProcessorTime(Process proc)
    {
        try
        {
            return string.Create(System.Globalization.CultureInfo.InvariantCulture,
                $"it had used {proc.TotalProcessorTime.TotalSeconds:0.0} s of processor time itself (its children not counted)");
        }
        catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
        {
            return $"its processor time is not readable ({e.GetType().Name})";
        }
    }

    /// <summary>
    /// The retry policy, separated from the process launch so that it is testable WITHOUT a child process and without a
    /// time limit: a test supplies <paramref name="attempt"/> as an in-process stand-in that writes into the run
    /// directory and reports <see cref="ProcessOutcome.TimedOut"/>, exactly what a killed attempt leaves behind, so no
    /// wall clock decides which attempt the test observes (kb/Work PB3022, PB1590).
    /// </summary>
    /// <param name="command">The command line, for the log and the non-observation message.</param>
    /// <param name="budget">The per-attempt budget and the work it covers, reported in the message; the attempt
    /// enforces it.</param>
    /// <param name="runDirectory">See <see cref="ObserveOrThrow"/>.</param>
    /// <param name="attempt">One observation of the child.</param>
    internal static ProcessObservation ObserveWithRetry(string command, ObservationBudget budget, string? runDirectory,
        Func<ProcessObservation> attempt)
    {
        var before = runDirectory is null ? null : RunDirectoryState.Capture(runDirectory);
        var first = attempt();
        if (first.Completed) return first;

        Interlocked.Increment(ref _retries);
        HarnessLog.Record($"RETRY  {command} :: {first.Outcome} :: {first.Detail}");
        // Before the gate: this waits for a process that has not exited, not for contention, and holds nobody else's retry.
        if (first.Survivor is not null) AwaitSurvivor(command, first);
        RetryGate.Wait();
        ProcessObservation second;
        try
        {
            if (before is not null) RestoreForRetry(command, before, first);
            second = attempt();
        }
        finally { RetryGate.Release(); }
        if (second.Completed)
        {
            HarnessLog.Record($"RECOVERED  {command} :: exit {second.ExitCode}");
            return second;
        }

        Interlocked.Increment(ref _nonObservations);
        HarnessLog.Record($"NON-OBSERVATION  {command} :: {second.Outcome} :: {second.Detail}");
        throw new HarnessNonObservationException(
            "⛔ HARNESS NON-OBSERVATION — this is NOT a value mismatch and NOT a compiler verdict.\n" +
            $"  command : {command}\n" +
            $"  attempt1: {first.Outcome} — {first.Detail}\n" +
            $"  attempt2: {second.Outcome} — {second.Detail}\n" +
            $"  budget  : {budget}, per attempt\n" +
            $"  run so far: {Interlocked.Read(ref _attempts)} attempts, {Interlocked.Read(ref _retries)} retried, " +
            $"{Interlocked.Read(ref _nonObservations)} unobserved\n" +
            "  The process never finished, so NOTHING was learned about the program under test. Do not read\n" +
            "  this as a failing assertion: no output was compared. See plan §11 A12 and the observation log\n" +
            $"  at {HarnessLog.Path}.");
    }

    /// <summary>Wait for the process the first attempt could not kill to exit, so the retry starts once it has released
    /// its files, or raise the non-observation naming it (kb/Work PB4388). A process still alive after
    /// <see cref="SurvivorWaitMs"/> is not the program's to wait for any longer, and a retry over its held files would be
    /// refused for that reason alone, reported as an unrestorable run directory.</summary>
    private static void AwaitSurvivor(string command, ProcessObservation first)
    {
        SurvivingProcess survivor = first.Survivor!;
        if (survivor.WaitForExit(SurvivorWaitMs))
        {
            HarnessLog.Record($"SURVIVOR-EXITED  {command} :: process {survivor.Id} exited while the retry waited for it");
            return;
        }
        Interlocked.Increment(ref _nonObservations);
        HarnessLog.Record($"NON-OBSERVATION  {command} :: process {survivor.Id} still alive {SurvivorWaitMs} ms into the wait for it");
        throw new HarnessNonObservationException(
            "⛔ HARNESS NON-OBSERVATION — this is NOT a value mismatch and NOT a compiler verdict.\n" +
            $"  command : {command}\n" +
            $"  attempt1: {first.Outcome} — {first.Detail}\n" +
            $"  attempt2: not run — process {survivor.Id}, killed after attempt 1, is still alive {SurvivorWaitMs} ms into the\n" +
            "            retry's wait for it, and holds the files and the processor attempt 1 gave it; a retry started\n" +
            "            beside it would measure a different input (kb/Work PB4388).\n" +
            $"  See the observation log at {HarnessLog.Path}.");
    }

    /// <summary>Put the run directory back as it was before the first attempt, or raise the non-observation: a retry
    /// over a state that is not the first attempt's would measure a different input and report it as a value.</summary>
    private static void RestoreForRetry(string command, RunDirectoryState before, ProcessObservation first)
    {
        IReadOnlyList<string> unrestorable;
        try { unrestorable = before.Restore(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            unrestorable = [$"(the directory could not be reset: {e.GetType().Name}: {e.Message})"];
        }
        if (unrestorable.Count == 0) return;
        Interlocked.Increment(ref _nonObservations);
        HarnessLog.Record($"NON-OBSERVATION  {command} :: run directory cannot be restored :: {string.Join(", ", unrestorable)}");
        throw new HarnessNonObservationException(
            "⛔ HARNESS NON-OBSERVATION — this is NOT a value mismatch and NOT a compiler verdict.\n" +
            $"  command : {command}\n" +
            $"  attempt1: {first.Outcome} — {first.Detail}\n" +
            "  attempt2: not run — the run directory cannot be restored to the state attempt 1 started from, and a\n" +
            "            retry over the first attempt's leftovers measures a different input (kb/Work PB3388).\n" +
            $"  cannot be restored: {string.Join(", ", unrestorable)}\n" +
            $"  (files over {RunDirectoryState.MaxCopiedFileBytes} bytes are not copied; see {nameof(RunDirectoryState)}).");
    }
}
