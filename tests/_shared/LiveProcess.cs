// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using System.Text;

namespace CobolNet.Tests.Shared;

/// <summary>
/// A child process that STAYS ALIVE while the test acts on the rest of the machine — the second half of the
/// child-process instruments, beside <see cref="ProcessObserver"/> (which observes a process that RUNS TO
/// COMPLETION). A witness that needs two real run units (kb/Work PB2485: a file lock held by one process and tried
/// by another, a lock that dies with its holder, a descendant that inherits what its parent holds) cannot be built
/// from an observation of a finished run: the holder must still be holding when the second process asks.
/// <para>The protocol is the program's own stdin and stdout: the test <see cref="Send"/>s a line the program
/// <c>ACCEPT</c>s and <see cref="ReadLine"/>s a line the program <c>DISPLAY</c>ed, so every step is ordered by a
/// message and never by a delay. ⛔ A line that never comes is a NON-OBSERVATION, bounded by the one harness budget
/// (<see cref="ObservationBudget.UnitMs"/>) and raised as <see cref="HarnessNonObservationException"/> — never a
/// value to compare (plan §11 A12), and never a private literal timeout in a test.</para>
/// </summary>
internal sealed class LiveProcess : IDisposable
{
    private readonly Process _process;
    private readonly StringBuilder _stderr = new();
    private readonly string _command;

    private LiveProcess(Process process, string command)
    {
        _process = process;
        _command = command;
    }

    /// <summary>Start <paramref name="psi"/> with all three standard streams redirected and its stderr drained.</summary>
    public static LiveProcess Start(ProcessStartInfo psi)
    {
        psi.RedirectStandardInput = true;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;
        psi.StandardOutputEncoding = new UTF8Encoding(false);
        psi.StandardErrorEncoding = new UTF8Encoding(false);
        psi.StandardInputEncoding = new UTF8Encoding(false);
        var process = Process.Start(psi)
                      ?? throw new HarnessNonObservationException($"Process.Start returned null for '{psi.FileName} {psi.Arguments}'");
        var live = new LiveProcess(process, $"{psi.FileName} {psi.Arguments}");
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (live._stderr) live._stderr.AppendLine(e.Data); };
        process.BeginErrorReadLine();
        return live;
    }

    /// <summary>The program's next stdout line, or null when it closed stdout (it exited) first.</summary>
    public string? ReadLine()
    {
        var read = Task.Run(_process.StandardOutput.ReadLine);
        if (!read.Wait(ObservationBudget.UnitMs))
            throw new HarnessNonObservationException(
                $"⛔ HARNESS NON-OBSERVATION — no stdout line from '{_command}' within {ObservationBudget.UnitMs} ms "
                + "(COBOLNET_RUN_TIMEOUT_MS); this is NOT a value mismatch and NOT a compiler verdict.");
        return read.Result;
    }

    /// <summary>The program's next stdout line; a program that exited without printing it fails the test with its stderr.</summary>
    public string ExpectLine() =>
        ReadLine() ?? throw new Xunit.Sdk.XunitException(
            $"'{_command}' ended without the stdout line the test was waiting for; stderr:{Environment.NewLine}{Stderr}");

    /// <summary>Send one line to the program's stdin (what its <c>ACCEPT</c> reads).</summary>
    public void Send(string line)
    {
        _process.StandardInput.WriteLine(line);
        _process.StandardInput.Flush();
    }

    public bool HasExited => _process.HasExited;

    /// <summary>What the program wrote to stderr so far.</summary>
    public string Stderr { get { lock (_stderr) return _stderr.ToString(); } }

    /// <summary>Wait for the program to end by itself (its stdin closed so an <c>ACCEPT</c> cannot block it) and
    /// return its exit code. The unbounded wait is deliberate: a program that never ends is bounded by the gate's
    /// own limit, and a bounded wait here would turn a slow host into a value (plan §11 A12).</summary>
    public int WaitForExit()
    {
        try { _process.StandardInput.Close(); } catch (IOException) { /* it exited first */ }
        _process.WaitForExit();
        return _process.ExitCode;
    }

    /// <summary>End the program as a crash would: the process and its descendants are killed, with no CLOSE, no
    /// run-unit termination and no flush. Returns once the operating system has reaped it, so every handle and
    /// lock it held is released.</summary>
    public void Kill()
    {
        if (!_process.HasExited)
        {
            try { _process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { /* already gone */ }
        }
        _process.WaitForExit();
    }

    public void Dispose()
    {
        Kill();
        _process.Dispose();
    }
}
