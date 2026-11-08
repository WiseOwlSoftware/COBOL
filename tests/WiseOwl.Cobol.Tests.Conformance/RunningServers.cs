// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using WiseOwl.Cobol.Runtime.Mcs;
using WiseOwl.Cobol.Runtime.Messaging;
using WiseOwl.Cobol.Tests.Shared;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ THE MESSAGE SERVERS OF A CONFORMANCE TEST, RUNNING (DESIGN-message-control.md §0 Q9, §9 S5; DESIGN-conformance-suite.md §3.1 <c>servers</c>): a WiseOwl test whose header
/// names servers has each one's compiled unit started as a concurrent run unit serving its name, in the test's own MCS directory, before the test runs, so a test file
/// can witness a SEND and a RECEIVE across two run units where a portable case file runs one. They are started by <see cref="ServerActivation.Launch"/>, the very mechanism
/// a requestor uses to start a server from <c>mcs.json</c> (design 5.4.5), so the harness holds no second way: it returns when each server has published its listener, and a server that
/// ends first, or does not publish within the harness's one budget (<see cref="ObservationBudget.UnitMs"/>), is a harness failure and no verdict. <see cref="Dispose"/> kills
/// every server and removes the socket directory the run left, as the test's own directory is removed.
/// </summary>
internal sealed class RunningServers : IDisposable
{
    private readonly List<Process> _processes = [];
    private readonly string _socketDirectory;

    private RunningServers(string socketDirectory) => _socketDirectory = socketDirectory;

    /// <summary>Starts each of <paramref name="servers"/> (its assembly <c>&lt;output&gt;.dll</c> already compiled in <paramref name="work"/>) in turn, the MCS directory <paramref name="work"/>; null when there are none.</summary>
    internal static RunningServers? Start(IReadOnlyList<SuiteServer> servers, string work)
    {
        if (servers.Count == 0) return null;                 // most tests start none, and none of them pays for a socket directory
        var mcs = McsDirectory.Resolve(work);
        var sockets = McsSocketDirectory.Resolve(mcs.Identity);
        var running = new RunningServers(sockets.Path);
        try
        {
            foreach (SuiteServer server in servers)
            {
                using var budget = new CancellationTokenSource(ObservationBudget.UnitMs);
                ActivationEnded started;
                Process? process;
                try
                {
                    started = ServerActivation.Launch(sockets, mcs.Path, McsSocketDirectory.NewRunId(), McsServerName.Normalize(server.Name),
                        ["dotnet", Path.Combine(work, server.Output + ".dll")], budget.Token, out process, CutRunner.PinnedEnvironment);
                }
                catch (IOException e) when (budget.IsCancellationRequested)
                {
                    throw new HarnessNonObservationException($"server {server.Name} did not publish its listener within {ObservationBudget.UnitMs} ms (WISEOWL_COBOL_RUN_TIMEOUT_MS): {e.Message}");
                }
                if (!started.Succeeded || process is null) throw new SuiteHarnessException($"server {server.Name} did not start: {started.Detail}");
                running._processes.Add(process);
            }
            return running;
        }
        catch
        {
            running.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        foreach (Process process in _processes)
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception) { /* it ended first */ }
            process.WaitForExit();
            process.Dispose();
        }
        _processes.Clear();
        ScratchDirectory.Remove(_socketDirectory);
    }
}
