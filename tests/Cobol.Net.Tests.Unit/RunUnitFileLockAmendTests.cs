// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A KEPT FILE LOCK IS REPLACED BY A RE-OPEN'S, NEVER GIVEN BACK FIRST (kb/Work PB3483). ISO §14.9.7.4 GR4: <i>"For
/// files specified in active APPLY COMMIT clauses that have been closed and not reopened prior to the COMMIT statement, the
/// file locks are released."</i>, and §14.9.27.4 GR25: <i>"If the execution of the OPEN statement is unsuccessful, the file is
/// not affected"</i>. The connector's own re-OPEN establishes a lock the kept one would refuse as another holder's, so the
/// kept lock used to be released first and taken again when the OPEN was unsuccessful — two instants in which another run
/// unit's OPEN against the kept column was admitted (§9.1.15, <i>"preventing other run units from opening that file with
/// incompatible sharing rules"</i>). <see cref="RunUnitFileLock.Amend"/> publishes the new column on the same hold before it
/// gives the old one back, and <see cref="RunUnitFileLock.Revert"/> publishes the old one before it gives the new one back.
/// <para>The witness is a host that, at the entry of EVERY establishment it is asked for, sends a racing run unit in with a
/// request Table 19 refuses against every holder (an OUTPUT row, here SHARING WITH ALL OTHER, which every host publishes;
/// a NO OTHER request publishes nothing on Windows, whose share mode carries it): the racer is admitted only at an instant
/// at which nobody holds the file. Each case runs on every host this machine has, Windows' included (kb/Work PB3482). The control case shows that the racer does get in once the lock is given back, so a green
/// run means the lock was never free, not that the racer cannot get in.</para>
/// </summary>
[Trait("Host", "ReFS")]
public sealed class RunUnitFileLockAmendTests
{
    public static TheoryData<string> Hosts()
    {
        var hosts = new TheoryData<string> { "sidecar" };
        if (OfdRegionLocks.Available) hosts.Add("ofd");
        if (OperatingSystem.IsWindows()) hosts.Add("handle");
        return hosts;
    }

    private static IColumnLockHost HostNamed(string name) => name switch
    {
        "ofd" => OfdColumnLockHost.Instance,
        "handle" => HandleColumnLockHost.Instance,
        _ => SidecarColumnLockHost.Instance,
    };

    /// <summary>The racer's request: refused against every column (Table 19's OUTPUT rows).</summary>
    private static readonly (FileSharing Sharing, FileOpenMode Mode) Racer = (FileSharing.AllOther, FileOpenMode.Output);

    private static (string Directory, string File) NewFile()
    {
        var dir = Directory.CreateTempSubdirectory("pb3483-").FullName;
        string file = Path.Combine(dir, "data.dat");
        File.WriteAllText(file, "SEED");
        return (dir, file);
    }

    private static void Clean(string directory)
    {
        try { Directory.Delete(directory, recursive: true); } catch (IOException) { }
    }

    /// <summary>What a request would be told while the lock is held, the request itself left nowhere.</summary>
    private static RunUnitFileLock.Outcome Ask(IColumnLockHost host, string file, FileSharing sharing, FileOpenMode mode)
    {
        var outcome = RunUnitFileLock.Take(host, file, sharing, mode, out var held);
        held?.Dispose();
        return outcome;
    }

    /// <summary>A host that sends a racing run unit in at the entry of every establishment.</summary>
    private sealed class RacingHost(IColumnLockHost inner, string file) : IColumnLockHost
    {
        public int Establishments { get; private set; }

        /// <summary>How many racers found nobody holding the file.</summary>
        public int Admitted { get; private set; }

        public void Reset() => (Establishments, Admitted) = (0, 0);

        public RunUnitFileLock.Outcome Establish(string hostPath, ExistingSharingColumn own, bool owesWrites,
            IReadOnlyList<ExistingSharingColumn> refused, IDisposable? supersedes, out IDisposable? hold)
        {
            Establishments++;
            if (Ask(inner, file, Racer.Sharing, Racer.Mode) == RunUnitFileLock.Outcome.Held) Admitted++;
            return inner.Establish(hostPath, own, owesWrites, refused, supersedes, out hold);
        }
    }

    /// <summary>The racer is admitted once the lock is given back and refused while it is held — the control without which a
    /// green case below proves nothing.</summary>
    [Theory]
    [MemberData(nameof(Hosts))]
    public void The_racer_gets_in_exactly_when_the_lock_is_free(string hostName)
    {
        var (dir, file) = NewFile();
        try
        {
            var host = HostNamed(hostName);
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(host, file, FileSharing.AllOther, FileOpenMode.Input, out var kept));
            Assert.Equal(RunUnitFileLock.Outcome.Refused, Ask(host, file, Racer.Sharing, Racer.Mode));
            kept!.Dispose();   // what the re-OPEN used to do first
            Assert.Equal(RunUnitFileLock.Outcome.Held, Ask(host, file, Racer.Sharing, Racer.Mode));
        }
        finally { Clean(dir); }
    }

    /// <summary>A replacement that is admitted holds the NEW column and not the old one, with no instant in which nobody holds
    /// the file; <see cref="RunUnitFileLock.Revert"/> publishes the old column again, again with none. K is SHARING WITH ALL
    /// OTHER on INPUT, its re-OPEN SHARING WITH READ ONLY on INPUT; a third run unit's OPEN of SHARING WITH ALL OTHER on I-O
    /// is admitted beside the first and refused beside the second.</summary>
    [Theory]
    [MemberData(nameof(Hosts))]
    public void An_admitted_replacement_never_leaves_the_file_unlocked(string hostName)
    {
        var (dir, file) = NewFile();
        try
        {
            var racing = new RacingHost(HostNamed(hostName), file);
            (FileSharing, FileOpenMode) old = (FileSharing.AllOther, FileOpenMode.Input), replacement = (FileSharing.ReadOnly, FileOpenMode.Input),
                third = (FileSharing.AllOther, FileOpenMode.IO);
            Assert.False(Table19.Conflicts(third, old));
            Assert.True(Table19.Conflicts(third, replacement));
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(racing, file, old.Item1, old.Item2, out var kept));
            racing.Reset();

            Assert.Equal(RunUnitFileLock.Outcome.Held, kept!.Amend(replacement.Item1, replacement.Item2, owesWrites: false));
            Assert.Equal((1, 0), (racing.Establishments, racing.Admitted));
            Assert.Equal(RunUnitFileLock.Outcome.Refused, Ask(HostNamed(hostName), file, third.Item1, third.Item2));   // the new column stands

            Assert.True(kept.Revert());
            Assert.Equal((2, 0), (racing.Establishments, racing.Admitted));
            Assert.Equal(RunUnitFileLock.Outcome.Held, Ask(HostNamed(hostName), file, third.Item1, third.Item2));      // the old column is back
            Assert.Equal(RunUnitFileLock.Outcome.Refused, Ask(HostNamed(hostName), file, Racer.Sharing, Racer.Mode));

            kept.Dispose();
            Assert.Equal(RunUnitFileLock.Outcome.Held, Ask(HostNamed(hostName), file, Racer.Sharing, Racer.Mode));
        }
        finally { Clean(dir); }
    }

    /// <summary>A replacement another run unit's lock refuses leaves the kept lock EXACTLY as it was (§14.9.27.4 GR25): the
    /// request K's old column refuses and the other holder's does not is still refused afterwards, no racer got in, and the
    /// lock the refused request published is gone. K is SHARING WITH READ ONLY on INPUT, the other holder SHARING WITH ALL
    /// OTHER on INPUT (admitted beside K), the replacement SHARING WITH NO OTHER on I-O (refused by the other holder), and the
    /// request SHARING WITH ALL OTHER on I-O — a writer K restricts to input mode and the other holder admits.</summary>
    [Theory]
    [MemberData(nameof(Hosts))]
    public void A_refused_replacement_leaves_the_kept_lock_as_it_was(string hostName)
    {
        var (dir, file) = NewFile();
        try
        {
            var racing = new RacingHost(HostNamed(hostName), file);
            (FileSharing, FileOpenMode) old = (FileSharing.ReadOnly, FileOpenMode.Input), other = (FileSharing.AllOther, FileOpenMode.Input),
                replacement = (FileSharing.NoOther, FileOpenMode.IO), writer = (FileSharing.AllOther, FileOpenMode.IO);
            Assert.False(Table19.Conflicts(other, old));
            Assert.True(Table19.Conflicts(replacement, other));
            Assert.True(Table19.Conflicts(writer, old));
            Assert.False(Table19.Conflicts(writer, other));
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(racing, file, old.Item1, old.Item2, out var kept));
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(HostNamed(hostName), file, other.Item1, other.Item2, out var neighbour));
            racing.Reset();

            Assert.Equal(RunUnitFileLock.Outcome.Refused, kept!.Amend(replacement.Item1, replacement.Item2, owesWrites: false));
            Assert.Equal((1, 0), (racing.Establishments, racing.Admitted));
            neighbour!.Dispose();   // alone again: only K's column stands against the writer
            Assert.Equal(RunUnitFileLock.Outcome.Refused, Ask(HostNamed(hostName), file, writer.Item1, writer.Item2));
            Assert.True(kept.Revert());   // nothing was amended: a no-op that must not disturb the lock
            Assert.Equal(RunUnitFileLock.Outcome.Refused, Ask(HostNamed(hostName), file, writer.Item1, writer.Item2));

            kept.Dispose();
            Assert.Equal(RunUnitFileLock.Outcome.Held, Ask(HostNamed(hostName), file, writer.Item1, writer.Item2));
            Assert.Equal([file], Directory.EnumerateFileSystemEntries(dir));   // no lock directory, no holder file left behind
        }
        finally { Clean(dir); }
    }
}
