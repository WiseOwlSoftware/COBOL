// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY HOST OF THE REGION LOCKS KEEPS ONE CONTRACT, AND THE LOCK-FILE HOST IS HELD TO IT ON EVERY MACHINE (kb/Work
/// PB2936). ISO §9.1.16: <i>"While locked by a given file connector, a record is not accessible to another file
/// connector in the same or a different run unit"</i>, and §9.1.13.8 1) answers I-O status 51 for it. The record locks
/// and the store mutex of a shared RELATIVE or INDEXED file are bytes of the map in <see cref="HostRegionLocks"/>, held
/// through a <see cref="RegionLocks"/> endpoint; before PB2936 they had no expression on macOS, so two run units sharing
/// a keyed file under <c>SHARING WITH ALL OTHER</c> there both read '00' for a record one held and lost each other's
/// updates, silently.
/// <para>The contract: an exclusive hold refuses every other holder and a shared one refuses an exclusive one; the
/// asker's question "does anyone ELSE hold it" is answered for another endpoint's holds; a released or disposed hold is
/// free; a waiting hold gets the byte when it is given back; and two exclusive requests at the same instant never both
/// win. The lock-file host (<see cref="SidecarRegionLocks"/>) is plain managed code over <see cref="FileShare"/>, so
/// these tests run it on EVERY machine and need no Mac; the host of the machine the test runs on is held to the same
/// contract beside it. The end-to-end scenarios (two <see cref="FileRegistry"/> instances, '51', RETRY, lost updates)
/// are <see cref="CrossRunUnitKeyedStoreDriftTests"/> and <see cref="CrossRunUnitSequentialLockDriftTests"/>, which run
/// on both hosts too.</para>
/// </summary>
[Trait("Host", "ReFS")]
public sealed class RegionLocksDriftTests
{
    private static readonly Regex RegionFileName = new(@"^r4[0-9a-f]{15}-[SX]-[0-9a-f]{32}$", RegexOptions.Compiled);

    private static IRegionLockHost HostOf(RegionHostKind kind) =>
        kind == RegionHostKind.LockFiles ? SidecarRegionLocks.Host : HostRegionLocks.HostOfThisProcess;

    public static TheoryData<RegionHostKind> Hosts() => new() { RegionHostKind.OfThisProcess, RegionHostKind.LockFiles };

    /// <summary>A private directory holding one physical file, so the lock directory beside it is this test's alone.</summary>
    private sealed class Site : IDisposable
    {
        private readonly RegionHostKind _kind;
        private readonly List<IDisposable> _owned = [];

        internal Site(RegionHostKind kind)
        {
            _kind = kind;
            Directory = System.IO.Directory.CreateTempSubdirectory("pb2936-").FullName;
            File = Path.Combine(Directory, "data.dat");
            System.IO.File.WriteAllText(File, "SEED");
        }

        internal string Directory { get; }
        internal string File { get; }

        /// <summary>A second connector over the file: its own handle (open for write, as a store's is) and endpoint.</summary>
        internal RegionLocks Endpoint(bool writable = true)
        {
            var stream = new FileStream(File, FileMode.Open, writable ? FileAccess.ReadWrite : FileAccess.Read, FileShare.ReadWrite);
            var regions = HostOf(_kind).Open(stream.SafeFileHandle, File, writable);
            _owned.Add(regions);
            _owned.Add(stream);
            return regions;
        }

        public void Dispose()
        {
            for (int i = _owned.Count - 1; i >= 0; i--) _owned[i].Dispose();
            try { System.IO.Directory.Delete(Directory, recursive: true); } catch (IOException) { }
        }
    }

    private const long Byte1 = HostRegionLocks.StoreMutexByte;
    private const long Byte2 = HostRegionLocks.LockPresenceByte;

    [Theory]
    [MemberData(nameof(Hosts))]
    public void AnExclusiveHoldRefusesEveryOtherHolder_UntilItIsGivenBack(RegionHostKind kind)
    {
        using var site = new Site(kind);
        var a = site.Endpoint();
        var b = site.Endpoint();
        Assert.Equal(RegionLockResult.Ok, a.Hold(Byte1, exclusive: true, wait: false));
        Assert.Equal(RegionLockResult.Held, b.Hold(Byte1, exclusive: true, wait: false));
        Assert.Equal(RegionLockResult.Held, b.Hold(Byte1, exclusive: false, wait: false));
        Assert.Equal(RegionLockResult.Held, b.HeldByAnother(Byte1));
        Assert.Equal(RegionLockResult.Ok, b.HeldByAnother(Byte2));   // another byte is free
        Assert.Equal(RegionLockResult.Ok, b.Hold(Byte2, exclusive: true, wait: false));

        a.Release(Byte1);
        a.Release(Byte1);   // idempotent
        Assert.Equal(RegionLockResult.Ok, b.HeldByAnother(Byte1));
        Assert.Equal(RegionLockResult.Ok, b.Hold(Byte1, exclusive: true, wait: false));
        Assert.Equal(RegionLockResult.Held, a.HeldByAnother(Byte1));
    }

    [Theory]
    [MemberData(nameof(Hosts))]
    public void SharedHoldsCoexist_AndRefuseAnExclusiveOne_InBothOrders(RegionHostKind kind)
    {
        using var site = new Site(kind);
        var a = site.Endpoint();
        var b = site.Endpoint();
        var c = site.Endpoint();
        Assert.Equal(RegionLockResult.Ok, a.Hold(Byte1, exclusive: false, wait: false));
        Assert.Equal(RegionLockResult.Ok, b.Hold(Byte1, exclusive: false, wait: false));
        Assert.Equal(RegionLockResult.Held, c.Hold(Byte1, exclusive: true, wait: false));
        Assert.Equal(RegionLockResult.Held, c.HeldByAnother(Byte1));   // a shared holder is a holder
        a.Release(Byte1);
        Assert.Equal(RegionLockResult.Held, c.Hold(Byte1, exclusive: true, wait: false));   // b still holds it
        b.Release(Byte1);
        Assert.Equal(RegionLockResult.Ok, c.Hold(Byte1, exclusive: true, wait: false));
        Assert.Equal(RegionLockResult.Held, a.Hold(Byte1, exclusive: false, wait: false));   // exclusive refuses shared
    }

    /// <summary>The CLOSE (or the loss of the handle) gives back everything the endpoint holds, and a successor is not
    /// refused for a lock the program had already given up.</summary>
    [Theory]
    [MemberData(nameof(Hosts))]
    public void DisposingAnEndpointGivesBackEverythingItHolds(RegionHostKind kind)
    {
        using var site = new Site(kind);
        var a = site.Endpoint();
        var b = site.Endpoint();
        Assert.Equal(RegionLockResult.Ok, a.Hold(Byte1, exclusive: true, wait: false));
        Assert.Equal(RegionLockResult.Ok, a.Hold(Byte2, exclusive: false, wait: false));
        a.Dispose();
        a.Dispose();   // idempotent
        // The handle-owned hosts give a lock back when the connector closes the handle, which the Site does last.
        if (kind == RegionHostKind.LockFiles)
        {
            Assert.Equal(RegionLockResult.Ok, b.HeldByAnother(Byte1));
            Assert.Equal(RegionLockResult.Ok, b.HeldByAnother(Byte2));
            Assert.Equal(RegionLockResult.Ok, b.Hold(Byte1, exclusive: true, wait: false));
        }
    }

    /// <summary>A waiting hold gets the byte when the holder gives it back (the store mutex, §14.7.9.3's statement
    /// boundary).</summary>
    [Theory]
    [MemberData(nameof(Hosts))]
    public void AWaitingHoldGetsTheByteWhenItIsGivenBack(RegionHostKind kind)
    {
        using var site = new Site(kind);
        var a = site.Endpoint();
        var b = site.Endpoint();
        Assert.Equal(RegionLockResult.Ok, a.Hold(Byte1, exclusive: true, wait: false));
        using var started = new ManualResetEventSlim();
        RegionLockResult waited = RegionLockResult.Unavailable;
        var waiter = new Thread(() =>
        {
            started.Set();
            waited = b.Hold(Byte1, exclusive: true, wait: true);
        });
        waiter.Start();
        started.Wait();
        a.Release(Byte1);
        waiter.Join();
        Assert.Equal(RegionLockResult.Ok, waited);
        Assert.Equal(RegionLockResult.Held, a.HeldByAnother(Byte1));
    }

    /// <summary>⛔ TWO RUN UNITS ASKING FOR ONE BYTE AT THE SAME INSTANT CANNOT BOTH HOLD IT: at most one is
    /// <see cref="RegionLockResult.Ok"/> (both may be refused, each having seen the other — a refused statement, not a
    /// violated lock).</summary>
    [Theory]
    [MemberData(nameof(Hosts))]
    public void TwoSimultaneousExclusiveHolds_NeverBothHold(RegionHostKind kind)
    {
        using var site = new Site(kind);
        var endpoints = new[] { site.Endpoint(), site.Endpoint() };
        for (int round = 0; round < 100; round++)
        {
            using var barrier = new Barrier(2);
            var results = new RegionLockResult[2];
            var threads = Enumerable.Range(0, 2).Select(i => new Thread(() =>
            {
                barrier.SignalAndWait();
                results[i] = endpoints[i].Hold(Byte1, exclusive: true, wait: false);
            })).ToArray();
            foreach (var t in threads) t.Start();
            foreach (var t in threads) t.Join();
            Assert.False(results[0] == RegionLockResult.Ok && results[1] == RegionLockResult.Ok,
                $"Round {round}: both endpoints hold byte {Byte1:x} exclusively — §9.1.16 'not accessible to another file connector'.");
            Assert.DoesNotContain(RegionLockResult.Unavailable, results);
            foreach (var endpoint in endpoints) endpoint.Release(Byte1);
        }
    }

    /// <summary>Two waiting requests that refused each other at the same instant are not a deadlock: jittered retries let
    /// one of them through, then the other.</summary>
    [Theory]
    [MemberData(nameof(Hosts))]
    public void TwoWaitingHoldsBothGetTheByteInTurn(RegionHostKind kind)
    {
        using var site = new Site(kind);
        var endpoints = new[] { site.Endpoint(), site.Endpoint() };
        for (int round = 0; round < 25; round++)
        {
            using var barrier = new Barrier(2);
            var results = new RegionLockResult[2];
            var threads = Enumerable.Range(0, 2).Select(i => new Thread(() =>
            {
                barrier.SignalAndWait();
                results[i] = endpoints[i].Hold(Byte1, exclusive: true, wait: true);
                if (results[i] == RegionLockResult.Ok) endpoints[i].Release(Byte1);
            })).ToArray();
            foreach (var t in threads) t.Start();
            foreach (var t in threads) t.Join();
            Assert.Equal([RegionLockResult.Ok, RegionLockResult.Ok], results);
        }
    }

    // ── The lock-file host's own facts ──────────────────────────────────────────────────────────────────────

    /// <summary>What a Unix lock-directory creation does with <c>EEXIST</c> (kb/Work PB3313): the closing-sibling race has
    /// TWO arms, a sibling's CLOSE removed the directory .NET's <c>mkdir</c> met (nothing at the name) and a sibling's next
    /// publication made it again after that CLOSE (a directory at the name), and both are answered as success, as .NET
    /// answers them on Windows; only a FILE at the name keeps the failure, and so does any other errno. The race itself
    /// lies between two calls inside .NET that no test can hold apart, so the decision is asked directly, of the four
    /// states the name can be in when .NET throws (branch review R3-1: the directory-again arm used to throw, and
    /// <see cref="TwoWaitingHoldsBothGetTheByteInTurn"/> cannot reach it).</summary>
    [Theory]
    [InlineData("nothing", 17, true)]
    [InlineData("directory", 17, true)]
    [InlineData("file", 17, false)]
    [InlineData("nothing", 13, false)]
    public void ALockDirectoryEexist_IsTheClosingSiblingRace_UnlessAFileHoldsTheName(string atTheName, int errno, bool race)
    {
        string parent = Directory.CreateTempSubdirectory("pb3313-").FullName;
        try
        {
            string name = Path.Combine(parent, "lockdir");
            if (atTheName == "directory") Directory.CreateDirectory(name);
            if (atTheName == "file") File.WriteAllText(name, "");
            Assert.Equal(race, HostFile.IsClosingSiblingRace(new IOException("mkdir", errno), name));
        }
        finally { Directory.Delete(parent, recursive: true); }
    }

    /// <summary>⛔ A PUBLICATION THAT LOSES THE SAME RACE AGAIN WAITS FOR IT TO END INSTEAD OF RETRYING AT ONCE (kb/Work
    /// PB3314). On Linux a lock directory in the middle of its <c>rmdir</c> answers <c>stat</c> and an empty listing while
    /// every creation inside it is <c>ENOENT</c>, for as long as a preempted remover is off the CPU (measured up to 17 ms
    /// with 8 processes beside 24 CPU burners); eight immediate tries all fell inside that window and a waiting hold
    /// answered <see cref="RegionLockResult.Unavailable"/> once in about 500,000 rounds. The schedule is asked directly,
    /// because the window itself cannot be held open on demand: the first retry is immediate, no pause exceeds the
    /// 128 ms that a statement can bear, the pauses never shrink, and their ceilings sum to at least 25 times the
    /// longest window measured.</summary>
    [Fact]
    public void ARepeatedLostPublicationRaceIsPacedByTime_NotRetriedAtOnce()
    {
        Assert.Equal(0, SidecarLockFiles.RetryPauseCeiling(1));
        int total = 0, previous = 0;
        for (int retry = 1; retry < SidecarLockFiles.PublishAttempts; retry++)
        {
            int ceiling = SidecarLockFiles.RetryPauseCeiling(retry);
            Assert.InRange(ceiling, previous, 128);
            if (retry > 1) Assert.True(ceiling > 0, $"Retry {retry} would be immediate again.");
            total += ceiling;
            previous = ceiling;
        }
        Assert.True(total >= 25 * 17, $"The schedule sums to {total} ms, inside 25 times the longest removal window measured (17 ms).");
    }

    /// <summary>⛔ PUBLICATION SURVIVES A SIBLING'S CLOSE THAT IS CAUGHT HALF WAY THROUGH REMOVING THE LOCK DIRECTORY
    /// (kb/Work PB3314, PB3313): a sibling's CLOSE (<see cref="SidecarLockFiles.RetireDirectory"/>) removes the empty
    /// directory and its next publication makes it again, so a publication meets a directory that is gone, about to be, or
    /// MID-<c>rmdir</c>: on Linux, for as long as the remover is off the CPU, the name still answers <c>stat</c> while
    /// every creation inside it is <c>ENOENT</c>. Each publication must still end published: <c>null</c> is a host that
    /// cannot hold the lock, and the waiting hold that reads it answers <see cref="RegionLockResult.Unavailable"/> with
    /// the store mutex untaken. The window cannot be held open on demand, so this is the contract under the load that opens
    /// it (spinning threads beside the sibling leave a remover off the CPU mid-call), a repetition witness that holds
    /// the host to publishing; <see cref="ARepeatedLostPublicationRaceIsPacedByTime_NotRetriedAtOnce"/> pins the fix
    /// itself, because a window of ONE publication in 500,000 is not one a test can wait for.</summary>
    [Fact]
    public void EveryPublicationSucceeds_BesideASiblingThatClosesAndReopensTheLockDirectory_OnAnOversubscribedHost()
    {
        using var site = new Site(RegionHostKind.LockFiles);
        string lockDir = SidecarLockFiles.LockDirectoryOf(site.File);
        using var stop = new CancellationTokenSource();
        var threads = new List<Thread>();
        threads.Add(new Thread(() =>
        {
            while (!stop.IsCancellationRequested)
            {
                try { HostFile.CreateLockDirectory(lockDir); SidecarLockFiles.RetireDirectory(lockDir); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                Thread.Sleep(1);   // a CLOSE and the next OPEN, not a spin: the spinners below are what preempt it
            }
        }));
        for (int i = 0; i < Environment.ProcessorCount * 2; i++)
            threads.Add(new Thread(() => { while (!stop.IsCancellationRequested) Thread.SpinWait(100); }) { Priority = ThreadPriority.BelowNormal });
        foreach (var t in threads) t.Start();
        try
        {
            for (int i = 0; i < 300; i++)
            {
                using var holder = SidecarLockFiles.Publish(lockDir, id => $"r{Byte1:x16}-X-{id:N}");
                Assert.True(holder is not null, $"Publication {i} gave up: the host was read as unable to hold a lock.");
            }
        }
        finally
        {
            stop.Cancel();
            foreach (var t in threads) t.Join();
        }
    }

    /// <summary>⛔ A TESTER THAT REAPS A PENDING FILE IS A RACE LOST, NOT A REFUSAL (SidecarLockFiles remarks): a tester that
    /// opens a pending file freely in the instant between its creation and its lock reads it as a dead holder's and
    /// deletes it, and the publisher publishes again. Here a tester sweeps the directory's pending files without pause
    /// while publications run; each must still end published, and the published file must be Live.</summary>
    [Fact]
    public void EveryPublicationSucceeds_BesideATesterThatReapsPendingFilesWithoutPause()
    {
        using var site = new Site(RegionHostKind.LockFiles);
        string lockDir = SidecarLockFiles.LockDirectoryOf(site.File);
        HostFile.CreateLockDirectory(lockDir);
        using var keep = SidecarLockFiles.Publish(lockDir, id => $"r{Byte2:x16}-S-{id:N}");   // keeps the directory standing
        Assert.NotNull(keep);
        using var stop = new CancellationTokenSource();
        var tester = new Thread(() =>
        {
            while (!stop.IsCancellationRequested)
            {
                try
                {
                    foreach (string path in Directory.EnumerateFileSystemEntries(lockDir, "pending-*"))
                        SidecarLockFiles.Observe(path);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        });
        tester.Start();
        try
        {
            for (int i = 0; i < 400; i++)
            {
                using var holder = SidecarLockFiles.Publish(lockDir, id => $"r{Byte1:x16}-X-{id:N}");
                Assert.True(holder is not null, $"Publication {i} gave up beside a reaping tester.");
                Assert.Equal(HostFile.LockFileState.Live, HostFile.ProbeLockFile(Path.Combine(lockDir, holder!.Name)));
            }
        }
        finally
        {
            stop.Cancel();
            tester.Join();
        }
    }

    /// <summary>A NAME THE HOST WILL NOT LET BE A LOCK DIRECTORY — a regular file at it — is the host refusing the lock,
    /// not a race to retry: the hold answers <see cref="RegionLockResult.Unavailable"/> at once, for the waiting and
    /// the non-waiting alike, never an exception out of a statement and never a wait through the retry schedule
    /// (<see cref="SidecarLockFiles.RetryPauseCeiling"/>).</summary>
    [Fact]
    public void AFileAtTheLockDirectoryName_IsUnavailableAtOnce_NotARetriedRace()
    {
        using var site = new Site(RegionHostKind.LockFiles);
        System.IO.File.WriteAllText(SidecarLockFiles.LockDirectoryOf(site.File), "not a directory");
        var a = site.Endpoint();
        string lockDir = SidecarLockFiles.LockDirectoryOf(site.File);
        // The refusal is the host's, raised where the directory is asked for, and never a lost race that publishes again.
        Assert.Throws<IOException>(() => HostFile.CreateLockDirectory(lockDir));
        Assert.Throws<IOException>(() => SidecarLockFiles.Publish(lockDir, id => $"r{Byte1:x16}-X-{id:N}"));
        Assert.Equal(RegionLockResult.Unavailable, a.Hold(Byte1, exclusive: true, wait: false));
        Assert.Equal(RegionLockResult.Unavailable, a.Hold(Byte1, exclusive: true, wait: true));
    }

    /// <summary>The names are a contract between run units (two runtime versions must find each other): a held byte is a
    /// file of the physical file's lock directory named <c>r</c>, the byte as 16 hex digits, the mode, and a GUID; the
    /// directory goes with its last file.</summary>
    [Fact]
    public void TheLockFileIsNamedByTheWireContract_AndTheDirectoryGoesWithItsLastFile()
    {
        using var site = new Site(RegionHostKind.LockFiles);
        var a = site.Endpoint();
        Assert.Equal(RegionLockResult.Ok, a.Hold(Byte1, exclusive: true, wait: false));
        Assert.Equal(RegionLockResult.Ok, a.Hold(Byte2, exclusive: false, wait: false));
        string lockDir = Assert.Single(Directory.EnumerateDirectories(site.Directory));
        Assert.Equal(SidecarLockFiles.LockDirectoryOf(site.File), lockDir);
        var names = Directory.EnumerateFiles(lockDir).Select(Path.GetFileName).Order().ToArray();
        Assert.Equal(2, names.Length);
        Assert.All(names, n => Assert.Matches(RegionFileName, n!));
        Assert.Equal($"r{Byte1:x16}-X-", names[0]![..20]);
        Assert.Equal($"r{Byte2:x16}-S-", names[1]![..20]);
        a.Release(Byte1);
        Assert.Single(Directory.EnumerateFiles(lockDir));
        a.Release(Byte2);
        Assert.Empty(Directory.EnumerateDirectories(site.Directory));
    }

    /// <summary>The column host's files and the region host's files share one lock directory and never read as each
    /// other's: a column file is not a region holder, and a region file is not a column holder.</summary>
    [Fact]
    public void ColumnFilesAndRegionFilesShareADirectoryWithoutConfusion()
    {
        using var site = new Site(RegionHostKind.LockFiles);
        var a = site.Endpoint();
        Assert.Equal(RunUnitFileLock.Outcome.Held,
            RunUnitFileLock.Take(SidecarColumnLockHost.Instance, site.File, FileSharing.NoOther, FileOpenMode.IO, out var column));
        Assert.Equal(RegionLockResult.Ok, a.Hold(Byte1, exclusive: true, wait: false));
        // The region file does not refuse a file lock (its name is no column), and the column file no region hold.
        Assert.Equal(RunUnitFileLock.Outcome.Refused,
            RunUnitFileLock.Take(SidecarColumnLockHost.Instance, site.File, FileSharing.AllOther, FileOpenMode.Input, out _));
        Assert.Equal(RegionLockResult.Ok, site.Endpoint().HeldByAnother(Byte2));
        a.Release(Byte1);
        column!.Dispose();
        Assert.Empty(Directory.EnumerateDirectories(site.Directory));
    }

    /// <summary>A killed run unit holds nothing — the kernel dropped its lock with its descriptor — but its file is left
    /// behind: it refuses nobody and the next holder sweeps it.</summary>
    [Fact]
    public void AKilledHoldersLeftoverRefusesNobody_AndIsSwept()
    {
        using var site = new Site(RegionHostKind.LockFiles);
        string lockDir = SidecarLockFiles.LockDirectoryOf(site.File);
        Directory.CreateDirectory(lockDir);
        string stale = Path.Combine(lockDir, $"r{Byte1:x16}-X-{Guid.NewGuid():N}");
        System.IO.File.WriteAllText(stale, "");
        var a = site.Endpoint();
        Assert.Equal(RegionLockResult.Ok, a.HeldByAnother(Byte1));
        Assert.False(System.IO.File.Exists(stale));
        System.IO.File.WriteAllText(stale, "");
        Assert.Equal(RegionLockResult.Ok, a.Hold(Byte1, exclusive: true, wait: false));
        Assert.False(System.IO.File.Exists(stale));
    }

    /// <summary>⛔ A HOLDER THIS PROCESS CANNOT OBSERVE IS A HOLDER (kb/Work PB2917, PB2936): a non-waiting hold and the
    /// asker's question both answer <see cref="RegionLockResult.Held"/> and nothing is swept, and a WAITING hold, which
    /// cannot wait for a release it cannot see, is <see cref="RegionLockResult.Unavailable"/> rather than a hang. The
    /// entry is a directory named like a holder file, which no host lets a file open read.</summary>
    [Fact]
    public void AHolderThatCannotBeObserved_IsHeldForTheNonWaiting_AndUnavailableForTheWaiting()
    {
        using var site = new Site(RegionHostKind.LockFiles);
        string lockDir = SidecarLockFiles.LockDirectoryOf(site.File);
        string unreadable = Path.Combine(lockDir, $"r{Byte1:x16}-X-{Guid.NewGuid():N}");
        Directory.CreateDirectory(unreadable);
        var a = site.Endpoint();
        Assert.Equal(RegionLockResult.Held, a.HeldByAnother(Byte1));
        Assert.Equal(RegionLockResult.Held, a.Hold(Byte1, exclusive: true, wait: false));
        Assert.Equal(RegionLockResult.Unavailable, a.Hold(Byte1, exclusive: true, wait: true));
        Assert.True(Directory.Exists(unreadable), "An entry the process cannot observe was swept.");
        Assert.Equal(RegionLockResult.Ok, a.Hold(Byte2, exclusive: true, wait: false));   // another byte is not affected
    }

    /// <summary>⛔ A LOCK DIRECTORY THIS USER MAY ENTER BUT NOT LIST HIDES EVERY HOLDER (the train 1056b review): the
    /// listing's "access denied" was read as a lock the host cannot hold, so a READ WITH LOCK got the record another run
    /// unit may hold, where §9.1.13.8 1) answers '51'. It is unobservable as a whole: held for the non-waiting and the
    /// asker, unavailable for the waiting. Another endpoint really holds the byte, and only then is the directory's mode
    /// narrowed to write and search, so the holder is hidden and its file keeps the directory alive across the asker's
    /// own publications. Unix modes only; the test witnesses nothing as root.</summary>
    [Fact]
    public void ALockDirectoryThatCannotBeListed_IsHeldForTheNonWaiting_AndUnavailableForTheWaiting()
    {
        if (OperatingSystem.IsWindows()) return;
        using var site = new Site(RegionHostKind.LockFiles);
        string lockDir = SidecarLockFiles.LockDirectoryOf(site.File);
        var a = site.Endpoint();
        var b = site.Endpoint();
        Assert.Equal(RegionLockResult.Ok, b.Hold(Byte1, exclusive: true, wait: false));   // the holder a cannot see
        System.IO.File.SetUnixFileMode(lockDir, UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        try
        {
            try { _ = Directory.EnumerateFileSystemEntries(lockDir).ToList(); return; }   // root lists it: nothing to witness
            catch (UnauthorizedAccessException) { }

            Assert.Equal(RegionLockResult.Held, a.HeldByAnother(Byte1));
            Assert.Equal(RegionLockResult.Held, a.Hold(Byte1, exclusive: true, wait: false));
            Assert.Equal(RegionLockResult.Unavailable, a.Hold(Byte1, exclusive: true, wait: true));
        }
        finally
        {
            if (Directory.Exists(lockDir))
                System.IO.File.SetUnixFileMode(lockDir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    /// <summary>⛔ THE LOCK DIRECTORY NEVER PASSES THROUGH DELETE-PENDING UNDER ITS OWN NAME (kb/Work PB3109). Windows'
    /// RemoveDirectory sets the delete disposition and closes its handle, and for as long as ANY other handle on the
    /// directory is open (an indexer's, a scanner's, a deleter frozen between its two steps) the directory stays listed
    /// but every open inside it and every listing of it is "access denied", the very answer an unobservable holder gives.
    /// The last holder's CLOSE therefore renames the empty directory away before deleting it
    /// (<see cref="SidecarLockFiles.RetireDirectory"/>), as <see cref="SidecarLockFiles.Retire"/> does a lock file, so
    /// no name a publisher or tester enters is ever deleted in place and the next hold finds it free: a waiting hold
    /// answered <see cref="RegionLockResult.Unavailable"/> instead of waiting (measured 11 of 3,000 two-endpoint rounds
    /// under CPU load, each at <c>HostFile.OpenLockHolder</c>), and the store mutex it stood for was not taken (the
    /// PB2936 lost-update shape). The race cannot be made to happen on demand, so the witness reads the ORDER the host
    /// reports (<see cref="DirectoryNameWatch"/>): the lock directory's name went by a rename, never by a deletion.</summary>
    [Fact]
    public void TheLastHoldersCloseRenamesTheLockDirectoryAwayRatherThanDeletingItInPlace()
    {
        using var site = new Site(RegionHostKind.LockFiles);
        var a = site.Endpoint();
        var b = site.Endpoint();
        Assert.Equal(RegionLockResult.Ok, b.Hold(Byte2, exclusive: false, wait: false));
        string lockDir = SidecarLockFiles.LockDirectoryOf(site.File);
        using var watch = new DirectoryNameWatch(site.Directory);
        b.Release(Byte2);               // the last holder's CLOSE
        watch.AssertRenamedAwayRatherThanDeletedInPlace(Path.GetFileName(lockDir));
        Assert.Equal(RegionLockResult.Ok, a.HeldByAnother(Byte1));
        Assert.Equal(RegionLockResult.Ok, a.Hold(Byte1, exclusive: true, wait: true));
        Assert.Equal(RegionLockResult.Ok, b.Hold(Byte2, exclusive: true, wait: true));
        Assert.Equal(RegionLockResult.Held, a.Hold(Byte2, exclusive: true, wait: false));
    }

    /// <summary>A host that cannot put a lock beside the file — here a directory that is really a file — holds nothing
    /// and refuses nothing: <see cref="RegionLockResult.Unavailable"/>, never an exception out of a statement.</summary>
    [Fact]
    public void ADirectoryThatCannotHoldTheLockIsUnavailableNotAnException()
    {
        using var site = new Site(RegionHostKind.LockFiles);
        string inner = Path.Combine(site.File, "inner.dat");   // beside a path whose "directory" is a regular file
        using var stream = new FileStream(site.File, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var a = SidecarRegionLocks.Host.Open(stream.SafeFileHandle, inner, writable: true);
        Assert.Equal(RegionLockResult.Unavailable, a.Hold(Byte1, exclusive: true, wait: false));
        Assert.Equal(RegionLockResult.Unavailable, a.Hold(Byte1, exclusive: true, wait: true));
        // Nobody could have published a lock where no lock directory can exist, so nobody else holds the byte.
        Assert.Equal(RegionLockResult.Ok, a.HeldByAnother(Byte1));
    }

    /// <summary>The host of a process is chosen once, from facts of the process: the per-handle expression where it
    /// exists (Windows; Linux with open-file-description locks) and the lock files everywhere else.</summary>
    [Fact]
    public void TheHostOfTheProcessIsChosenFromTheProcessShape()
    {
        object expected = OperatingSystem.IsWindows() || OfdRegionLocks.Available
            ? HandleRegionLocks.Host : SidecarRegionLocks.Host;
        Assert.Same(expected, HostRegionLocks.HostOfThisProcess);
    }

    /// <summary>The witness seam scopes to the flow that asked for it and is given back.</summary>
    [Fact]
    public void ForcingAHostIsScopedToTheCallersFlow()
    {
        using var site = new Site(RegionHostKind.OfThisProcess);
        using var stream = new FileStream(site.File, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        using (HostRegionLocks.Forcing(SidecarRegionLocks.Host))
        {
            using var forced = HostRegionLocks.Open(stream.SafeFileHandle, site.File, writable: true);
            Assert.IsType<SidecarRegionLocks>(forced);
        }
        using var back = HostRegionLocks.Open(stream.SafeFileHandle, site.File, writable: true);
        Assert.Equal(ReferenceEquals(HostRegionLocks.HostOfThisProcess, HandleRegionLocks.Host), back is HandleRegionLocks);
    }
}
