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

    /// <summary>A kept lock's re-OPEN under the SAME column (<see cref="RunUnitFileLock.Amend"/>, kb/Work PB3483) keeps its
    /// own byte and is not refused by it — the test leaves the holder's own byte out — while another holder of a column its
    /// row refuses still refuses it, and the refusal leaves the kept column standing. SHARING WITH READ ONLY on I-O refuses
    /// its own column (READ ONLY beside a READ ONLY connector that writes).</summary>
    [Fact]
    public void ASupersedingRequestForTheSameColumnIsArbitratedAroundItsOwnByte()
    {
        if (!OperatingSystem.IsWindows()) return;
        var (dir, file) = NewFile();
        try
        {
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.ReadOnly, FileOpenMode.IO, out var kept));
            Assert.Equal(RunUnitFileLock.Outcome.Held, kept!.Amend(FileSharing.ReadOnly, FileOpenMode.IO, owesWrites: false));
            Assert.Equal(RunUnitFileLock.Outcome.Refused, RunUnitFileLock.Take(Host, file, FileSharing.ReadOnly, FileOpenMode.IO, out _));
            Assert.True(kept.Revert());

            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.AllOther, FileOpenMode.Input, out var other));
            Assert.Equal(RunUnitFileLock.Outcome.Refused, kept.Amend(FileSharing.AllOther, FileOpenMode.Output, owesWrites: false));   // OUTPUT refuses every column
            other!.Dispose();
            Assert.Equal(RunUnitFileLock.Outcome.Refused, RunUnitFileLock.Take(Host, file, FileSharing.ReadOnly, FileOpenMode.IO, out _));   // still kept
            kept.Dispose();
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.ReadOnly, FileOpenMode.IO, out var after));
            after!.Dispose();
        }
        finally { Clean(dir); }
    }

    /// <summary>⛔ A KEPT LOCK'S RE-OPEN UNDER SHARING WITH NO OTHER GIVES ITS HOLDER HANDLE WAY (the class remarks of
    /// <see cref="HandleColumnLockHost"/>): the connector's own <see cref="FileShare.None"/> handle, which the holder's read
    /// handle would refuse, then opens; but only after the NO OTHER row's test passed — another holder of any column refuses
    /// the request and leaves the kept column exactly as it was (§14.9.27.4 GR25) — and <see cref="RunUnitFileLock.Revert"/>
    /// publishes the kept column again once the connector's handle is gone, so a request it refuses is refused again.</summary>
    [Fact]
    public void ANoOtherReplacementGivesWayToTheShareModeOnlyWhenTheTableAdmitsIt()
    {
        if (!OperatingSystem.IsWindows()) return;
        var (dir, file) = NewFile();
        try
        {
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.ReadOnly, FileOpenMode.Input, out var kept));
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.AllOther, FileOpenMode.Input, out var other));
            Assert.Equal(RunUnitFileLock.Outcome.Refused, kept!.Amend(FileSharing.NoOther, FileOpenMode.IO, owesWrites: false));
            Assert.True(kept.HoldsAHandle);   // the holder is still there
            other!.Dispose();
            Assert.Equal(RunUnitFileLock.Outcome.Refused, RunUnitFileLock.Take(Host, file, FileSharing.AllOther, FileOpenMode.IO, out _));   // the kept column refuses a writer

            Assert.Equal(RunUnitFileLock.Outcome.Held, kept.Amend(FileSharing.NoOther, FileOpenMode.IO, owesWrites: false));
            Assert.False(kept.HoldsAHandle);
            using (new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }   // the share mode's own handle opens

            Assert.True(kept.Revert());
            Assert.True(kept.HoldsAHandle);
            Assert.Equal(RunUnitFileLock.Outcome.Refused, RunUnitFileLock.Take(Host, file, FileSharing.AllOther, FileOpenMode.IO, out _));
            kept.Dispose();
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.AllOther, FileOpenMode.IO, out var after));
            after!.Dispose();
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

    /// <summary>The question a share mode needs answered to yield (<see cref="RunUnitFileLock.AnotherOwesWrites"/>, kb/Work
    /// PB3482, PB3499): does ANOTHER holder owe writes — is it an INPUT opener whose handle stands open for writing. A reader
    /// that merely holds an INPUT column does not, however many hold one (the proxy this replaced asked "does another INPUT
    /// opener hold a column", and an unrelated reader answered yes). A holder never counts itself (the owed-writes range is
    /// asked around its own byte), a lock given back is not held and asks nothing, and the state follows a superseding request:
    /// the byte is carried when the new request owes writes too, published when only it does, given back when only the
    /// superseded one did, and <see cref="RunUnitFileLock.Revert"/> publishes the superseded state again.</summary>
    [Fact]
    public void AHolderThatOwesWritesIsSeenByAnotherHolderAndOnlyByAnother()
    {
        if (!OperatingSystem.IsWindows()) return;
        var (dir, file) = NewFile();
        try
        {
            // INPUT readers owe nothing, however many hold an INPUT column (Table 19: ALL OTHER input beside ALL OTHER input is a normal open).
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.AllOther, FileOpenMode.Input, out var a));
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.AllOther, FileOpenMode.Input, out var b));
            Assert.False(a!.AnotherOwesWrites());
            Assert.False(b!.AnotherOwesWrites());

            // A kept lock's INPUT re-OPEN that owes its transaction's writes: seen by both readers, not by itself.
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.AllOther, FileOpenMode.IO, out var k));
            Assert.False(a.AnotherOwesWrites());                  // the I-O opener the CLOSE kept owes none yet
            Assert.Equal(RunUnitFileLock.Outcome.Held, k!.Amend(FileSharing.AllOther, FileOpenMode.Input, owesWrites: true));
            Assert.True(a.AnotherOwesWrites());
            Assert.True(b.AnotherOwesWrites());
            Assert.False(k.AnotherOwesWrites());                  // itself is no other

            // A second owing holder: each sees the other; one that stops owing (its transaction ended, a replacement under
            // another mode) is no longer seen, and a Revert publishes what it replaced.
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.AllOther, FileOpenMode.IO, out var k2));
            Assert.Equal(RunUnitFileLock.Outcome.Held, k2!.Amend(FileSharing.AllOther, FileOpenMode.Input, owesWrites: true));
            Assert.True(k.AnotherOwesWrites());
            Assert.True(k2.AnotherOwesWrites());
            Assert.Equal(RunUnitFileLock.Outcome.Held, k2.Amend(FileSharing.AllOther, FileOpenMode.Input, owesWrites: false));
            Assert.False(k.AnotherOwesWrites());                  // k2 stopped owing
            Assert.True(k2.AnotherOwesWrites());                  // and k still does
            Assert.True(k2.Revert());
            Assert.True(k.AnotherOwesWrites());                   // the superseded state, owing, is published again
            Assert.True(a.AnotherOwesWrites());

            // A superseding request that is refused publishes nothing: the owed-writes byte it added goes back with it.
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.AllOther, FileOpenMode.IO, out var k3));
            Assert.Equal(RunUnitFileLock.Outcome.Refused, k3!.Amend(FileSharing.AllOther, FileOpenMode.Output, owesWrites: true));   // OUTPUT refuses every column
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.AllOther, FileOpenMode.IO, out var probe));
            k.Dispose();
            k2.Dispose();
            Assert.False(probe!.AnotherOwesWrites());             // nobody owes writes now: not k3's refused request
            Assert.False(a.AnotherOwesWrites());
            Assert.False(k.AnotherOwesWrites());                  // a lock given back asks nothing
            k3.Dispose();
            probe.Dispose();
            b.Dispose();
            a.Dispose();
            Assert.False(a.AnotherOwesWrites());

            // A column taken afresh carries it too (a connector whose kept handle published no column: the other arm of
            // FileConnector.TakeRunUnitFileLock), seen by a reader beside it and not by itself.
            Assert.Equal(RunUnitFileLock.Outcome.Held,
                RunUnitFileLock.Take(Host, file, FileSharing.ReadOnly, FileOpenMode.Input, out var fresh, owesWrites: true));
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.ReadOnly, FileOpenMode.Input, out var reader));
            Assert.False(fresh!.AnotherOwesWrites());
            Assert.True(reader!.AnotherOwesWrites());
            fresh.Dispose();
            Assert.False(reader.AnotherOwesWrites());
            reader.Dispose();
        }
        finally { Clean(dir); }
    }

    /// <summary>The owed-writes byte is one fact about the holder and changes no answer of the table: for every request, a
    /// holder that owes writes (an INPUT re-OPEN under ALL OTHER) refuses it exactly as the same holder that owes none does
    /// (§14.9.27.4 GR4, Table 19 is the arbiter between connectors).</summary>
    [Theory]
    [MemberData(nameof(EveryRequest))]
    public void OwingWritesChangesNoAnswerOfTheTable(FileSharing sharing, FileOpenMode mode)
    {
        if (!OperatingSystem.IsWindows()) return;
        var (dir, file) = NewFile();
        try
        {
            RunUnitFileLock.Outcome Answer(bool owesWrites)
            {
                Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.AllOther, FileOpenMode.IO, out var kept));
                Assert.Equal(RunUnitFileLock.Outcome.Held, kept!.Amend(FileSharing.AllOther, FileOpenMode.Input, owesWrites));
                var outcome = RunUnitFileLock.Take(Host, file, sharing, mode, out var requested);
                requested?.Dispose();
                kept.Dispose();
                return outcome;
            }
            Assert.Equal(Answer(owesWrites: false), Answer(owesWrites: true));
        }
        finally { Clean(dir); }
    }

    public static TheoryData<FileSharing, FileOpenMode> EveryRequest()
    {
        var data = new TheoryData<FileSharing, FileOpenMode>();
        foreach (var sharing in Table19.StandardModes)
            foreach (var mode in Enum.GetValues<FileOpenMode>())
                data.Add(sharing, mode);
        return data;
    }

    /// <summary>The owed-writes range lies right past the last column's range, so no column's byte is ever one of its bytes and
    /// no owed-writes byte one of a column's (a WIRE VALUE between Windows run units, <see cref="HostRegionLocks.OwedWritesRangeStart"/>).</summary>
    [Fact]
    public void TheOwedWritesRangeLiesPastEveryColumnsRange()
    {
        long last = Enum.GetValues<ExistingSharingColumn>().Max(column => HostRegionLocks.ColumnRangeStart(column));
        Assert.Equal(HostRegionLocks.OwedWritesRangeStart, last + HostRegionLocks.ColumnRangeWidth);
        foreach (var column in Enum.GetValues<ExistingSharingColumn>())
            Assert.True(HostRegionLocks.ColumnRangeStart(column) + HostRegionLocks.ColumnRangeWidth <= HostRegionLocks.OwedWritesRangeStart);
    }

    /// <summary>⛔ CONTENTION BETWEEN PUBLISHERS AND TESTERS NEVER MAKES THE HOST UNAVAILABLE. A tester's shared request over a
    /// column's range conflicts, for the instant it lasts, with a publisher's exclusive attempt at a byte of it, and the
    /// publisher chooses another byte; six threads taking and giving back the three columns that test each other's ranges,
    /// many rounds, are only ever held or refused — a lock this host "cannot hold" would be an OPEN granted without it.</summary>
    [Fact]
    public void ContendedPublishersAndTesters_AreOnlyEverHeldOrRefused()
    {
        if (!OperatingSystem.IsWindows()) return;
        var (dir, file) = NewFile();
        try
        {
            (FileSharing Sharing, FileOpenMode Mode)[] requests =
            [
                (FileSharing.AllOther, FileOpenMode.IO), (FileSharing.ReadOnly, FileOpenMode.Input), (FileSharing.AllOther, FileOpenMode.Input),
            ];
            int unavailable = 0;
            var threads = Enumerable.Range(0, 6).Select(i => new Thread(() =>
            {
                for (int round = 0; round < 300; round++)
                {
                    var (sharing, mode) = requests[(i + round) % requests.Length];
                    var outcome = RunUnitFileLock.Take(Host, file, sharing, mode, out var hold);
                    if (outcome == RunUnitFileLock.Outcome.Unavailable) Interlocked.Increment(ref unavailable);
                    hold?.Dispose();
                }
            })).ToArray();
            foreach (var t in threads) t.Start();
            foreach (var t in threads) t.Join();
            Assert.Equal(0, unavailable);
        }
        finally { Clean(dir); }
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

    /// <summary>⛔ A PUBLICATION EVERY ONE OF ITS TRIES COLLIDES IN IS A LOCK THIS HOST CANNOT HOLD, NOT A REFUSAL (kb/Work
    /// PB3498, the sibling of <c>FileConnector.Open</c>'s two attempts): the publication chooses another byte of its column's
    /// range after each collision with a holder or a tester, a bounded number of times, and a range an outside program
    /// holds exclusively collides every one. The outcome is <see cref="RunUnitFileLock.Outcome.Unavailable"/> (the OPEN goes on
    /// without a column; the share modes it falls back on are the host's own), never <see cref="RunUnitFileLock.Outcome.Refused"/>
    /// ('61' is another connector's, §9.1.13.9 1)) and never an exception; a first request leaves nothing published (no handle
    /// stays on the file: a <see cref="FileShare.None"/> open is admitted), and a superseding request leaves the superseded
    /// column standing exactly as it was (§14.9.27.4 GR25, <i>"If the execution of the OPEN statement is unsuccessful, the
    /// file is not affected"</i>), refusing what it refused, and the outside program's range given back, the same requests
    /// are held.</summary>
    [Fact]
    public void APublicationThatEveryTryCollidesInIsUnavailableAndLeavesWhatWasHeldStanding()
    {
        if (!OperatingSystem.IsWindows()) return;
        var (dir, file) = NewFile();
        try
        {
            var (sharing, mode) = (FileSharing.AllOther, FileOpenMode.IO);
            long start = HostRegionLocks.ColumnRangeStart(Table19.Column(sharing, mode));
            // A column held already (before the outsider takes the range, which a holder of another column would meet as a holder).
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.ReadOnly, FileOpenMode.Input, out var kept));
            using (var outsider = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                Assert.Equal(RegionLockResult.Ok, WindowsByteRangeLocks.Lock(outsider.SafeFileHandle, start, HostRegionLocks.ColumnRangeWidth, exclusive: true, wait: false));

                Assert.Equal(RunUnitFileLock.Outcome.Unavailable, RunUnitFileLock.Take(Host, file, sharing, mode, out var none));
                Assert.Null(none);

                // The held column replaced by the one whose range the outsider holds: the request is unavailable and the kept
                // column stands (it refuses a READ ONLY I-O request, whose own range the outsider does not hold).
                Assert.Equal(RunUnitFileLock.Outcome.Unavailable, kept!.Amend(sharing, mode, owesWrites: false));
                Assert.Equal(RunUnitFileLock.Outcome.Refused, RunUnitFileLock.Take(Host, file, FileSharing.ReadOnly, FileOpenMode.IO, out _));
                Assert.True(kept.Revert());   // nothing was amended: the restore is a no-op
                Assert.Equal(RunUnitFileLock.Outcome.Refused, RunUnitFileLock.Take(Host, file, FileSharing.ReadOnly, FileOpenMode.IO, out _));
                kept.Dispose();
            }
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, sharing, mode, out var after));
            after!.Dispose();
            using (new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }   // no handle of a failed try stays
        }
        finally { Clean(dir); }
    }

    /// <summary>⛔ AN OWED-WRITES BYTE THAT CANNOT BE PUBLISHED FAILS THE WHOLE REQUEST AND TAKES BACK THE COLUMN BYTE IT ALREADY
    /// ADDED (kb/Work PB3499, review round 1, MANDATORY-PRACTICES I11 exception path): the request publishes its column first
    /// and the owed-writes byte second, so an outside program that holds the whole owed-writes range exclusively collides every
    /// try of the second publication. The outcome is <see cref="RunUnitFileLock.Outcome.Unavailable"/> (a lock this host cannot
    /// hold, never '61': §9.1.13.9 1) names another file connector), and it leaves nothing behind (§14.9.27.4 GR25, <i>"If the
    /// execution of the OPEN statement is unsuccessful, the file is not affected"</i>): a first request leaves no stray byte in
    /// its column's range and no handle on the file, and a superseding request leaves the superseded column standing, the
    /// column it asked for unpublished.</summary>
    [Fact]
    public void AnOwedWritesByteThatCannotBePublishedFailsTheRequestAndGivesBackTheColumnByteAlreadyAdded()
    {
        if (!OperatingSystem.IsWindows()) return;
        var (dir, file) = NewFile();
        try
        {
            var (sharing, mode) = (FileSharing.AllOther, FileOpenMode.Input);
            long column = HostRegionLocks.ColumnRangeStart(Table19.Column(sharing, mode));
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, FileSharing.ReadOnly, FileOpenMode.Input, out var kept));
            using (var outsider = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (var probe = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                Assert.Equal(RegionLockResult.Ok, WindowsByteRangeLocks.Lock(outsider.SafeFileHandle, HostRegionLocks.OwedWritesRangeStart,
                    HostRegionLocks.ColumnRangeWidth, exclusive: true, wait: false));

                // A first request: unavailable, no hold, and the column range it published a byte in is free again.
                Assert.Equal(RunUnitFileLock.Outcome.Unavailable, RunUnitFileLock.Take(Host, file, sharing, mode, out var none, owesWrites: true));
                Assert.Null(none);
                Assert.Equal(RegionLockResult.Ok, WindowsByteRangeLocks.Lock(probe.SafeFileHandle, column, HostRegionLocks.ColumnRangeWidth, exclusive: true, wait: false));
                WindowsByteRangeLocks.Unlock(probe.SafeFileHandle, column, HostRegionLocks.ColumnRangeWidth);

                // A superseding request: unavailable, the new column's range free again, the kept column standing (it still
                // refuses a READ ONLY I-O request) and Revert a no-op.
                Assert.Equal(RunUnitFileLock.Outcome.Unavailable, kept!.Amend(sharing, mode, owesWrites: true));
                Assert.Equal(RegionLockResult.Ok, WindowsByteRangeLocks.Lock(probe.SafeFileHandle, column, HostRegionLocks.ColumnRangeWidth, exclusive: true, wait: false));
                WindowsByteRangeLocks.Unlock(probe.SafeFileHandle, column, HostRegionLocks.ColumnRangeWidth);
                Assert.Equal(RunUnitFileLock.Outcome.Refused, RunUnitFileLock.Take(Host, file, FileSharing.ReadOnly, FileOpenMode.IO, out _));
                Assert.True(kept.Revert());
                kept.Dispose();
            }
            Assert.Equal(RunUnitFileLock.Outcome.Held, RunUnitFileLock.Take(Host, file, sharing, mode, out var after, owesWrites: true));
            after!.Dispose();
            using (new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }   // no handle of a failed try stays
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
