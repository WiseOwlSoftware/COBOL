// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ WINDOWS ARBITRATES A SECOND RUN UNIT BY TABLE 19 TOO, NOT BY ITS SHARE MODES ALONE (kb/Work PB3482). ISO §9.1.15 2):
/// <i>"The sharing with read only mode restricts concurrent access to a physical file through file connectors other than
/// this one, to input mode"</i> — and an INPUT connector can still owe writes, which a share mode cannot say, so a
/// <see cref="FileShare.Read"/> connector was refused ('61') by the HOST beside an INPUT opener that Table 19 prints
/// <i>Normal open</i> for. <see cref="HandleColumnLockHost"/> publishes each holder's Table 19 column as a byte range of the
/// physical file, so the table itself is the lock between run units on Windows as it is on every other host.
/// <para>The class holds the host to the table (<see cref="Table19.Conflicts"/>) crossed over every pair, exactly as
/// <see cref="SidecarColumnLockHostDriftTests"/> holds the lock-file host, with the one division of labour this host has:
/// <c>SHARING WITH NO OTHER</c> publishes no column (its own handle is <see cref="FileShare.None"/>, which is Table 19's
/// first column and refuses every other open), so a holder under it is invisible to the column and a request under it
/// is left to the share mode. The byte ranges are Windows locks, so the cases run on Windows and a host that is not
/// Windows is shown to answer <see cref="RunUnitFileLock.Outcome.Unavailable"/>.</para>
/// </summary>
[Trait("Host", "ReFS")]
public sealed class HandleColumnLockHostDriftTests
{
    private static readonly IColumnLockHost Host = HandleColumnLockHost.Instance;

    private static (string Directory, string File) NewFile()
    {
        var dir = System.IO.Directory.CreateTempSubdirectory("pb3482-").FullName;
        string file = Path.Combine(dir, "data.dat");
        System.IO.File.WriteAllText(file, "SEED");
        return (dir, file);
    }

    private static void Clean(string directory)
    {
        try { System.IO.Directory.Delete(directory, recursive: true); } catch (IOException) { }
    }

    public static TheoryData<FileSharing, FileOpenMode, FileSharing, FileOpenMode> EveryPair()
    {
        var data = new TheoryData<FileSharing, FileOpenMode, FileSharing, FileOpenMode>();
        foreach (var sa in Table19.StandardModes)
            foreach (var ma in Enum.GetValues<FileOpenMode>())
                foreach (var sb in Table19.StandardModes)
                    foreach (var mb in Enum.GetValues<FileOpenMode>())
                        data.Add(sa, ma, sb, mb);
        return data;
    }

    /// <summary>For EVERY sharing mode × open mode on the holder crossed with every one on the requester, the second holder
    /// is refused exactly where Table 19 prints <i>Unsuccessful open</i> — the requests that refuse their OWN column
    /// (READ ONLY beside a READ ONLY connector that writes, every OUTPUT) included, which a per-column lock could not tell
    /// from the asker itself. Under NO OTHER nothing is published or asked: the share mode says it.</summary>
    [Theory]
    [MemberData(nameof(EveryPair))]
    public void ASecondHolderIsArbitratedByTable19(FileSharing sa, FileOpenMode ma, FileSharing sb, FileOpenMode mb)
    {
        if (!OperatingSystem.IsWindows()) return;
        var (dir, file) = NewFile();
        try
        {
            var firstOutcome = RunUnitFileLock.Take(Host, file, sa, ma, out var first);
            Assert.Equal(sa == FileSharing.NoOther ? RunUnitFileLock.Outcome.Unavailable : RunUnitFileLock.Outcome.Held, firstOutcome);
            var outcome = RunUnitFileLock.Take(Host, file, sb, mb, out var second);

            bool refused = sa != FileSharing.NoOther && sb != FileSharing.NoOther && Table19.Conflicts((sb, mb), (sa, ma));
            var expected = sb == FileSharing.NoOther ? RunUnitFileLock.Outcome.Unavailable
                : refused ? RunUnitFileLock.Outcome.Refused
                : RunUnitFileLock.Outcome.Held;
            Assert.True(outcome == expected,
                $"Table 19 {(refused ? "refuses" : "permits")} {(sb, mb)} against {(sa, ma)}, but the second holder was {outcome} (expected {expected}).");
            Assert.Equal(outcome != RunUnitFileLock.Outcome.Held, second is null);

            second?.Dispose();
            first?.Dispose();
        }
        finally { Clean(dir); }
    }

    /// <summary>§9.1.15: <i>"The file lock is removed by an explicit or implicit CLOSE statement"</i>, so the refusal is not
    /// sticky: the holder's disposal gives its column back, and disposing twice is harmless.</summary>
    [Fact]
    public void TheCloseGivesTheColumnBack()
    {
        if (!OperatingSystem.IsWindows()) return;
        var (dir, file) = NewFile();
        try
        {
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.ReadOnly, FileOpenMode.IO, out var first));
            Assert.Equal(RunUnitFileLock.Outcome.Refused, RunUnitFileLock.Take(Host, file, FileSharing.AllOther, FileOpenMode.IO, out _));
            first!.Dispose();
            first.Dispose();   // idempotent
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.AllOther, FileOpenMode.IO, out var second));
            second!.Dispose();
        }
        finally { Clean(dir); }
    }

    /// <summary>The lock is re-establishable after it was given back (a CLOSE under commit control keeps it and the
    /// connector's re-OPEN gives it back and takes it again, <see cref="RunUnitFileLock.Retake"/>) — and a Retake that
    /// meets another holder in the instant the column was free reports <see cref="RunUnitFileLock.Outcome.Refused"/>.</summary>
    [Fact]
    public void ARetakeIsRefusedByAHolderThatCameInBetween()
    {
        if (!OperatingSystem.IsWindows()) return;
        var (dir, file) = NewFile();
        try
        {
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.ReadOnly, FileOpenMode.IO, out var first));
            first!.Dispose();
            Assert.Equal(RunUnitFileLock.Outcome.Held, first.Retake());
            Assert.Equal(RunUnitFileLock.Outcome.Held, first.Retake());   // already held: still held
            first.Dispose();
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.ReadOnly, FileOpenMode.IO, out var other));
            Assert.Equal(RunUnitFileLock.Outcome.Refused, first.Retake());
            other!.Dispose();
        }
        finally { Clean(dir); }
    }

    /// <summary>⛔ TWO RUN UNITS OPENING AT THE SAME INSTANT CANNOT BOTH WIN. The column is PUBLISHED before the others are
    /// tested, so whichever publishes second finds the first; with a test-then-publish order both would pass and both run
    /// units would write. Two <c>SHARING WITH READ ONLY</c> I-O takes, which refuse their own column, released by one
    /// barrier, many rounds: at most one is ever held (both may be refused — each saw the other — which is a refused
    /// OPEN, not a violated lock), and none is ever unavailable.</summary>
    [Fact]
    public void TwoSimultaneousTakesOfAColumnThatRefusesItself_NeverBothHold()
    {
        if (!OperatingSystem.IsWindows()) return;
        var (dir, file) = NewFile();
        try
        {
            for (int round = 0; round < 200; round++)
            {
                using var barrier = new Barrier(2);
                var outcomes = new RunUnitFileLock.Outcome[2];
                var holds = new RunUnitFileLock?[2];
                var threads = Enumerable.Range(0, 2).Select(i => new Thread(() =>
                {
                    barrier.SignalAndWait();
                    outcomes[i] = RunUnitFileLock.Take(Host, file, FileSharing.ReadOnly, FileOpenMode.IO, out holds[i]);
                })).ToArray();
                foreach (var t in threads) t.Start();
                foreach (var t in threads) t.Join();

                Assert.False(outcomes[0] == RunUnitFileLock.Outcome.Held && outcomes[1] == RunUnitFileLock.Outcome.Held,
                    $"Round {round}: both holders took SHARING WITH READ ONLY I-O on {file} — Table 19 refuses a READ ONLY "
                    + "request beside a READ ONLY connector that writes.");
                Assert.DoesNotContain(RunUnitFileLock.Outcome.Unavailable, outcomes);
                foreach (var hold in holds) hold?.Dispose();
            }
        }
        finally { Clean(dir); }
    }

    /// <summary>A column has room for every holder there is: forty INPUT holders of one column are all held (a holder is a
    /// byte of a 2^32-byte range, not a slot of a table), and a request that column refuses meets them all.</summary>
    [Fact]
    public void ManyHoldersOfOneColumnAreAllHeld()
    {
        if (!OperatingSystem.IsWindows()) return;
        var (dir, file) = NewFile();
        var holds = new List<RunUnitFileLock>();
        try
        {
            for (int i = 0; i < 40; i++)
            {
                Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.AllOther, FileOpenMode.Input, out var hold));
                holds.Add(hold!);
            }
            Assert.Equal(RunUnitFileLock.Outcome.Refused, RunUnitFileLock.Take(Host, file, FileSharing.AllOther, FileOpenMode.Output, out _));
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.ReadOnly, FileOpenMode.IO, out var normal));   // Table 19: Normal open beside ALL OTHER INPUT
            normal!.Dispose();
        }
        finally
        {
            foreach (var hold in holds) hold.Dispose();
            Clean(dir);
        }
    }

    /// <summary>A holder that excludes every reader — a <see cref="FileShare.None"/> handle, which is what SHARING WITH NO
    /// OTHER (or a foreign program) holds — refuses the take: Table 19's first column, which every request row refuses.
    /// The refusal leaves nothing published.</summary>
    [Fact]
    public void AHandleThatExcludesEveryReaderRefusesTheTake()
    {
        if (!OperatingSystem.IsWindows()) return;
        var (dir, file) = NewFile();
        try
        {
            using (new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                Assert.Equal(RunUnitFileLock.Outcome.Refused, RunUnitFileLock.Take(Host, file, FileSharing.AllOther, FileOpenMode.Input, out var held));
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.ReadOnly, FileOpenMode.IO, out var after));
            after!.Dispose();
        }
        finally { Clean(dir); }
    }

    /// <summary>A host that cannot hold the lock for a path — a file that is not there, a directory — holds nothing and
    /// refuses nothing: it is <see cref="RunUnitFileLock.Outcome.Unavailable"/>, never an exception out of an OPEN.</summary>
    [Fact]
    public void APathThatCannotCarryTheLockIsUnavailableNotAnException()
    {
        if (!OperatingSystem.IsWindows()) return;
        var (dir, file) = NewFile();
        try
        {
            Assert.Equal(RunUnitFileLock.Outcome.Unavailable, RunUnitFileLock.Take(Host, Path.Combine(dir, "absent.dat"), FileSharing.AllOther, FileOpenMode.Input, out var absent));
            Assert.Null(absent);
            Assert.Equal(RunUnitFileLock.Outcome.Unavailable, RunUnitFileLock.Take(Host, dir, FileSharing.AllOther, FileOpenMode.Input, out var directory));
            Assert.Null(directory);
            Assert.Equal(RunUnitFileLock.Outcome.Unavailable, RunUnitFileLock.Take(Host, "NUL", FileSharing.AllOther, FileOpenMode.Input, out var device));
            Assert.Null(device);
        }
        finally { Clean(dir); }
    }

    /// <summary>The ranges are Windows locks: forced onto a host that is not Windows the host holds nothing.</summary>
    [Fact]
    public void ANonWindowsHostIsUnavailable()
    {
        if (OperatingSystem.IsWindows()) return;
        var (dir, file) = NewFile();
        try
        {
            Assert.Equal(RunUnitFileLock.Outcome.Unavailable, RunUnitFileLock.Take(Host, file, FileSharing.AllOther, FileOpenMode.Input, out var held));
            Assert.Null(held);
        }
        finally { Clean(dir); }
    }

}
