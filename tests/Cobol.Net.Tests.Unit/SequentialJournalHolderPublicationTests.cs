// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A TRANSACTION'S HOLDER THAT CANNOT BE PUBLISHED IS REFUSED FOR THE CAUSE IT HAD (kb/Work PB3675; docs/rearchitecture/DESIGN-runtime-library.md
/// 2.2.2.5 R1 and 2.2.2.12): <see cref="IHostMedium.PublishHolder"/> answers the publication's <see cref="RegionLockResult"/>
/// (<c>Unavailable</c>: the host takes no lock here; <c>Held</c>: every try lost its race, which says nothing about the host),
/// and <see cref="SequentialJournal.Begin"/> refuses with a message that names the cause that happened. The statement answers
/// '30' for either (§9.1.13.6 item 1): the transaction cannot begin, and §14.9.27.4 GR25's <i>"If the execution of the OPEN statement
/// is unsuccessful, the file is not affected"</i> holds with no file, record or holder left behind (I3 of the journal's rows).
/// A publication that loses some tries and wins one begins the transaction, as a publisher that loses a race publishes again.
/// </summary>
public sealed class SequentialJournalHolderPublicationTests
{
    /// <summary>Run <paramref name="body"/> with the publication's tries lost as <paramref name="lost"/> says (attempt N, 0 for
    /// the first), its pauses observed and not slept (<see cref="SidecarLockFiles.PauseObserver"/>).</summary>
    private static void WithLostTries(Func<int, bool> lost, Action body)
    {
        SidecarLockFiles.RaceInjector.Value = lost;
        SidecarLockFiles.PauseObserver.Value = _ => { };
        try { body(); }
        finally
        {
            SidecarLockFiles.RaceInjector.Value = null;
            SidecarLockFiles.PauseObserver.Value = null;
        }
    }

    private static void AssertNoLitter(string f)
    {
        string lockDirectory = SequentialJournal.DirectoryOf(f);
        string[] left = Directory.Exists(lockDirectory) ? [.. Directory.GetFileSystemEntries(lockDirectory).Select(Path.GetFileName)!] : [];
        Assert.True(left.Length == 0, $"left behind in the lock directory: {string.Join(", ", left)}");
    }

    [Fact]
    public void A_publication_whose_every_try_lost_its_race_is_refused_as_a_lost_race_and_not_as_a_host_that_holds_nothing()
    {
        string dir = ScratchDirectory.Create("sjh-");
        string f = Path.Combine(dir, "f.dat");
        try
        {
            File.WriteAllText(f, "C1C2");
            var ex = Assert.Throws<IOException>(() =>
                WithLostTries(_ => true, () => SequentialJournal.Begin(HostMedium.Production, f, SequentialJournal.StartState.Present)));
            Assert.Contains("lost its race", ex.Message);
            Assert.Contains("removed the lock directory, or reaped or held the pending holder file", ex.Message);   // the three ways SidecarLockFiles.Publish loses a try (kb/Work PB3749)
            Assert.DoesNotContain("cannot hold", ex.Message);
            Assert.Equal("C1C2", File.ReadAllText(f));
            AssertNoLitter(f);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    [Fact]
    public void A_host_that_holds_no_lock_beside_the_file_is_refused_as_a_host_that_cannot_hold_the_holder()
    {
        string dir = ScratchDirectory.Create("sjh-");
        string f = Path.Combine(dir, "gone", "f.dat");   // the physical file's own directory is not there: nothing beside it can be held
        try
        {
            var ex = Assert.Throws<IOException>(() => SequentialJournal.Begin(HostMedium.Production, f, SequentialJournal.StartState.Absent));
            Assert.Contains("cannot hold", ex.Message);
            Assert.DoesNotContain("lost its race", ex.Message);
            Assert.False(Directory.Exists(Path.GetDirectoryName(f)), "the refusal made no directory");
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    [Fact]
    public void A_publication_that_loses_some_tries_and_wins_one_begins_the_transaction()
    {
        string dir = ScratchDirectory.Create("sjh-");
        string f = Path.Combine(dir, "f.dat");
        try
        {
            File.WriteAllText(f, "C1C2");
            var tries = new List<int>();
            SequentialJournal? journal = null;
            WithLostTries(attempt => { tries.Add(attempt); return attempt < 3; },
                () => journal = SequentialJournal.Begin(HostMedium.Production, f, SequentialJournal.StartState.Present));
            Assert.NotNull(journal);
            Assert.Equal([0, 1, 2, 3], tries);   // three tries lost, the fourth published
            journal!.Withdraw();
            Assert.Equal("C1C2", File.ReadAllText(f));
            AssertNoLitter(f);
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
