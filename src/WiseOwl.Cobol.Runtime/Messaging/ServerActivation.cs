// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Sockets;
using WiseOwl.Cobol.Runtime.IO;
using WiseOwl.Cobol.Runtime.Mcs;

namespace WiseOwl.Cobol.Runtime.Messaging;

/// <summary>
/// ⛔ A REQUESTOR'S START OF A MESSAGE SERVER (docs/rearchitecture/DESIGN-message-control.md §5.4.5, §9 S5): the one executor of <see cref="Activate"/>. A SEND that finds no live
/// server of a name whose <c>mcs.json</c> definition has a <c>start</c> command (§0 Q3) comes here, and the steps are the design's:
/// <list type="number">
/// <item>Open <c>start/&lt;N&gt;.lock</c> and hold <see cref="HostRegionLocks.McsStartByte"/> exclusively, waiting, through <see cref="HostRegionLocks.Open"/>, so two requestors racing to
/// activate one name launch it once; never a <c>FileShare.None</c> open, which on Unix is a <c>flock</c> .NET drops when the file system refuses it. A host that cannot carry the
/// lock (<see cref="RegionLockResult.Unavailable"/>) is an <see cref="IOException"/>, EC-MCS-IMP (§0 Q8).</item>
/// <item>Holding the lock, enumerate the name's listeners again: a server another requestor started while this one waited for the lock is the answer, and nothing is launched.</item>
/// <item>Bind and listen at <c>n/&lt;r&gt;</c>, then launch <c>start</c> in the MCS directory with <c>WISEOWL_COBOL_MCS</c>, <c>_SERVER</c>, <c>_NOTIFY</c> and <c>_LOG</c>
/// (<c>servers/&lt;N&gt;.&lt;r&gt;.log</c>) set. The server publishes its listener, rings the pending bells and connects to the notify path (<see cref="MessagingPeer"/>'s start), and a
/// process that ends before that connection is a failed start: <see cref="ActivationEnded"/> unsuccessful, EC-MCS-IMP.</item>
/// <item>Release the lock, close and delete <c>n/&lt;r&gt;</c>; the protocol continues at §5.4.1 step 5.</item>
/// </list>
/// The server's standard output and error go to its log, which the process entry of the started run unit opens (<see cref="StandardStreams.EnsureUtf8"/>, <c>WISEOWL_COBOL_MCS_LOG</c>);
/// what the started command itself writes before that is drained into the same log while the start is awaited.
/// </summary>
internal sealed class ServerActivation(McsSocketDirectory sockets, string mcsDirectory, string runId, SocketTransport transport)
{
    /// <summary>The variable that names the log a started server's standard output and error go to (design 5.4.5).</summary>
    internal const string LogVariable = "WISEOWL_COBOL_MCS_LOG";

    /// <summary>How long a connection the listener already holds is awaited when the command has ended: the accept completes at once, so this bounds a defect and not a normal wait.</summary>
    private static readonly TimeSpan NotifyGrace = TimeSpan.FromSeconds(5);

    /// <summary>Starts the server <paramref name="name"/> (normalized) with <paramref name="definition"/>'s command, unless its listener is already published; blocks until the
    /// server has published it, the command's process has ended, or <paramref name="stop"/> is cancelled (the run unit was killed).</summary>
    internal ActivationEnded Run(string name, ServerDefinition definition, CancellationToken stop)
    {
        if (definition.Start is not { Count: > 0 } command) throw new IOException($"server '{name}' has no start command");
        string spelling = McsServerName.Spell(name);
        string lockPath = Path.Combine(sockets.Path, "start", spelling + ".lock");
        SocketTransport.CreateDirectory(Path.GetDirectoryName(lockPath)!);
        using var lockFile = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);
        using var region = HostRegionLocks.Open(lockFile.SafeFileHandle, lockPath, writable: true);
        var held = region.Hold(HostRegionLocks.McsStartByte, exclusive: true, wait: true);
        if (held != RegionLockResult.Ok)
            throw new IOException($"the activation lock '{lockPath}' of server '{name}' cannot be taken: {(held == RegionLockResult.Unavailable ? "this host cannot carry the lock" : "another requestor holds it")}");
        try
        {
            stop.ThrowIfCancellationRequested();
            if (transport.EnumerateServers(spelling).Count > 0) return new ActivationEnded(true, null);   // started by the requestor that held the lock before this one
            return Launch(sockets, mcsDirectory, runId, name, command, stop, out _);
        }
        finally
        {
            region.Release(HostRegionLocks.McsStartByte);
        }
    }

    /// <summary>⛔ THE ONE LAUNCH OF A MESSAGE SERVER (design 5.4.5, steps 3 and 4): binds and listens at <c>n/&lt;runId&gt;</c> under <paramref name="sockets"/>, runs
    /// <paramref name="command"/> in <paramref name="mcsDirectory"/> with <c>WISEOWL_COBOL_MCS</c>, <c>_SERVER</c> (<paramref name="name"/>), <c>_NOTIFY</c> and <c>_LOG</c> set, and blocks until the
    /// server connects to the notify socket (successful, <paramref name="started"/> the running process, which the caller owns) or the process ends first (unsuccessful, <paramref name="started"/> null),
    /// or <paramref name="stop"/> is cancelled. <paramref name="environment"/> is set in the process's environment first (a null value removes the variable), as <c>RunExit</c> sets a test's. The caller holds whatever excludes a second launch of the name: a requestor's <see cref="Run"/> holds the start lock, and a host that starts the
    /// servers of a test before the test (the conformance suite's <c>servers</c> field, DESIGN-message-control.md §0 Q9) starts each name once. A failure that is not the command's own end is an <see cref="IOException"/>.</summary>
    internal static ActivationEnded Launch(McsSocketDirectory sockets, string mcsDirectory, string runId, string name, IReadOnlyList<string> command, CancellationToken stop, out Process? started,
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        started = null;
        string spelling = McsServerName.Spell(name);
        string notifyPath = Path.Combine(sockets.Path, "n", runId);
        var endPoint = McsSocketDirectory.EndPointOf(notifyPath);
        SocketTransport.CreateDirectory(Path.GetDirectoryName(notifyPath)!);
        SocketTransport.DeleteQuietly(notifyPath);
        string logDirectory = Path.Combine(mcsDirectory, "servers");
        Directory.CreateDirectory(logDirectory);
        string logPath = Path.Combine(logDirectory, $"{spelling}.{runId}.log");
        using var listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        Process? process = null;
        try
        {
            try
            {
                listener.Bind(endPoint);
                listener.Listen(1);
            }
            catch (SocketException e)
            {
                throw new IOException($"cannot listen at the notify socket '{notifyPath}': {e.Message}", e);
            }
            var start = new ProcessStartInfo(command[0])
            {
                WorkingDirectory = mcsDirectory,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            for (int i = 1; i < command.Count; i++) start.ArgumentList.Add(command[i]);
            if (environment is not null)
                foreach (var (variable, value) in environment)
                    if (value is null) start.Environment.Remove(variable);
                    else start.Environment[variable] = value;
            start.Environment[McsDirectory.Variable] = mcsDirectory;
            start.Environment[MessagingSettings.ServerVariable] = name;
            start.Environment[MessagingSettings.NotifyVariable] = notifyPath;
            start.Environment[LogVariable] = logPath;
            try { process = Process.Start(start) ?? throw new IOException($"the start command '{command[0]}' of server '{name}' did not start"); }
            catch (Exception e) when (e is Win32Exception or InvalidOperationException)
            {
                throw new IOException($"cannot start server '{name}' with '{command[0]}': {e.Message}", e);
            }
            process.StandardInput.Close();
            Task drained = Task.WhenAll(Drain(process.StandardOutput.BaseStream, logPath), Drain(process.StandardError.BaseStream, logPath));
            var accepted = listener.AcceptAsync(stop).AsTask();
            var ended = process.WaitForExitAsync(CancellationToken.None);
            Task.WaitAny(accepted, ended);
            stop.ThrowIfCancellationRequested();
            // A server that publishes, connects and ends at once leaves its connection in the backlog: the notification counts, as the command's end is not what failed the start.
            if (!accepted.IsCompleted && listener.Poll(0, SelectMode.SelectRead)) accepted.Wait(NotifyGrace);
            if (accepted.IsFaulted) throw new IOException($"cannot accept the notification of server '{name}' at '{notifyPath}': {accepted.Exception!.GetBaseException().Message}", accepted.Exception);
            if (accepted.IsCompletedSuccessfully)
            {
                accepted.Result.Dispose();                           // the notification is the connection itself
                started = process;
                return new ActivationEnded(true, null);
            }
            ended.GetAwaiter().GetResult();
            drained.Wait(TimeSpan.FromSeconds(2));
            return new ActivationEnded(false, $"the start command of server '{name}' ended with exit code {process.ExitCode} before its server was published; its log is '{logPath}'");
        }
        catch (Exception e) when (e is OperationCanceledException or AggregateException { InnerException: OperationCanceledException })
        {
            throw new IOException($"starting server '{name}' was interrupted before its server was published");
        }
        finally
        {
            if (started is null && process is { HasExited: false }) TryKill(process);
            if (started is null) process?.Dispose();                 // a started server keeps its pipes: the log goes on being drained until they end
            listener.Close();
            SocketTransport.DeleteQuietly(notifyPath);
        }
    }

    /// <summary>Copies what the started command writes into the log until its pipe ends. The log is opened for append, so the started run unit's own writes to it interleave whole.</summary>
    private static async Task Drain(Stream pipe, string logPath)
    {
        try
        {
            await using var log = new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
            await pipe.CopyToAsync(log).ConfigureAwait(false);
        }
        catch (Exception e) when (e is IOException or ObjectDisposedException)
        {
            // the pipe or the log went away: the start does not depend on what the command printed
        }
    }

    private static void TryKill(Process process)
    {
        try { process.Kill(entireProcessTree: true); }
        catch (Exception e) when (e is InvalidOperationException or Win32Exception or NotSupportedException) { /* it ended first */ }
    }
}
