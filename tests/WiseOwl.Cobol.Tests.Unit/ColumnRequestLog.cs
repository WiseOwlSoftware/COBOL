// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime.IO;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The requests the connectors of the calling test make of the host's Table 19 column lock
/// (<see cref="IColumnLockHost.Establish"/>), in order — how a test COUNTS the attempts an OPEN makes
/// (<see cref="FileConnector.Open"/> makes at most two, kb/Work PB3498). Installed through
/// <see cref="RunUnitFileLock.HostObserver"/>, which wraps the host every lock established while it is installed goes
/// through, and which the lock keeps for the <see cref="RunUnitFileLock.Amend"/> and <see cref="RunUnitFileLock.Revert"/>
/// that follow. Dispose the result of <see cref="Install"/> to uninstall it.
/// </summary>
internal sealed class ColumnRequestLog
{
    /// <summary>One request: the column it published and whether it replaced a lock the connector already held (an
    /// <see cref="RunUnitFileLock.Amend"/> or a <see cref="RunUnitFileLock.Revert"/>, not a first
    /// <see cref="RunUnitFileLock.Take(string, FileSharing, FileOpenMode, out RunUnitFileLock?, bool)"/>).</summary>
    public readonly record struct Request(ExistingSharingColumn Own, bool Replaces, bool OwesWrites = false);

    private readonly List<Request> _requests = [];

    /// <summary>The requests made since the log was installed or last <see cref="Clear"/>ed.</summary>
    public IReadOnlyList<Request> Requests => _requests;

    /// <summary>Called with the zero-based index of a request (since the log was installed or last
    /// <see cref="Clear"/>ed) at its entry, before the host sees it: a test sends a racing run unit in between two
    /// attempts of an OPEN with it (the pattern of <c>RunUnitFileLockAmendTests.RacingHost</c>).</summary>
    public Action<int>? Before { get; set; }

    public void Clear() => _requests.Clear();

    /// <summary>Observe every lock established from now until the returned scope is disposed.</summary>
    public IDisposable Install()
    {
        RunUnitFileLock.HostObserver.Value = inner => new Host(inner, this);
        return new Scope();
    }

    private sealed class Scope : IDisposable
    {
        public void Dispose() => RunUnitFileLock.HostObserver.Value = null;
    }

    private sealed class Host(IColumnLockHost inner, ColumnRequestLog log) : IColumnLockHost
    {
        public RunUnitFileLock.Outcome Establish(string hostPath, ExistingSharingColumn own, bool owesWrites,
            IReadOnlyList<ExistingSharingColumn> refused, IDisposable? supersedes, out IDisposable? hold)
        {
            log.Before?.Invoke(log._requests.Count);
            log._requests.Add(new Request(own, supersedes is not null, owesWrites));
            return inner.Establish(hostPath, own, owesWrites, refused, supersedes, out hold);
        }
    }
}
