// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Runtime.IO;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;
using static WiseOwl.Cobol.Tests.Shared.FileHarness;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE ENTRY MUTEX OF AN UNCOVERED SHARING WRITER IS PARKED BETWEEN STATEMENTS, NOT PUBLISHED AND RETIRED AROUND EACH ONE
/// (kb/Work PB4387; docs/rearchitecture/DESIGN-runtime-library.md 2.2.2.3, <c>SequentialConnector.UncoveredChangeGuard</c>). Before,
/// every WRITE and REWRITE of a connector no APPLY COMMIT names, under <c>SHARING WITH ALL OTHER</c>, created, locked, listed and
/// renamed a lock file and then renamed it away and deleted it with the lock directory, about ten metadata operations a record
/// (SQ214A: 3 s alone, 19 to 30 s in 24 copies on a loaded ReFS volume). The guarantee it gave (PB3340 R1-1: another writer's
/// transaction cannot begin inside one change) is kept: the byte is held from the lock test through the release of the record and
/// held by no one between statements. What is pinned here is a COUNT of publications and the lock directory's NAMES, never a time.
/// <para>The cases cover the sidecar endpoint itself (two writers interleaving, a holder that died, a parked file removed under
/// it) and the connector's statements (a WRITE and a REWRITE, and a WRITE the medium refuses, which gives the byte back).</para>
/// </summary>
[Trait("Host", "ReFS")]
public sealed class SequentialEntryMutexParkingTests : SequentialCommitControlHarness
{
    /// <summary>A parked entry mutex's file: <c>i</c>, the byte's 16 hex digits, <c>-</c> and the holder's id.</summary>
    private static readonly Regex Parked = new($"^i{HostRegionLocks.EntryMutexByte:x16}-[0-9a-f]{{32}}$", RegexOptions.Compiled);

    /// <summary>A held entry mutex's file: <c>r</c>, the byte, the mode and the id.</summary>
    private static readonly Regex Held = new($"^r{HostRegionLocks.EntryMutexByte:x16}-[SX]-[0-9a-f]{{32}}$", RegexOptions.Compiled);

    /// <summary>The entry mutex's files in the lock directory of <paramref name="f"/>.</summary>
    private static string[] MutexFiles(string f)
    {
        string lockDirectory = SequentialJournal.DirectoryOf(f);
        return Directory.Exists(lockDirectory)
            ? [.. Directory.GetFileSystemEntries(lockDirectory).Select(p => Path.GetFileName(p)!).Where(n => Parked.IsMatch(n) || Held.IsMatch(n))]
            : [];
    }

    [Fact]
    public void An_uncovered_sharing_writer_publishes_its_entry_mutex_once_and_parks_it_between_writes()
    {
        string dir = ScratchDirectory.Create("sepk-");
        string f = Path.Combine(dir, "f.dat");
        try
        {
            File.WriteAllText(f, "C1", Encoding.Latin1);
            var u = new Unit(f, covered: false, new FaultHostMedium(), sharing: FileSharing.AllOther);
            Ok(u.Open(FileOpenMode.Extend));
            int publications = 0;
            SidecarLockFiles.RaceInjector.Value = _ => { publications++; return false; };
            string? parkedAs = null;
            try
            {
                for (int i = 0; i < 25; i++)
                {
                    Ok(u.Write("X1"));
                    string[] files = MutexFiles(f);
                    Assert.True(files.Length == 1 && Parked.IsMatch(files[0]), $"between statements the byte is parked, held by no one: [{string.Join(", ", files)}]");
                    parkedAs ??= files[0];
                    Assert.Equal(parkedAs, files[0]);   // the SAME file every time: never retired and published again
                }
            }
            finally { SidecarLockFiles.RaceInjector.Value = null; }
            Assert.Equal(1, publications);   // 25 WRITEs, one publication (before: 25)
            Ok(u.Close());
            Assert.Empty(MutexFiles(f));
            Assert.False(Directory.Exists(SequentialJournal.DirectoryOf(f)), "the CLOSE retires the parked file and the lock directory");
            Assert.Equal("C1" + string.Concat(Enumerable.Repeat("X1", 25)), File.ReadAllText(f, Encoding.Latin1));
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    [Fact]
    public void An_uncovered_sharing_rewrite_takes_the_same_parked_entry_mutex()
    {
        string dir = ScratchDirectory.Create("sepk-");
        string f = Path.Combine(dir, "f.dat");
        try
        {
            File.WriteAllText(f, "C1C2C3", Encoding.Latin1);
            var u = new Unit(f, covered: false, new FaultHostMedium(), sharing: FileSharing.AllOther);
            Ok(u.Open(FileOpenMode.IO));
            int publications = 0;
            SidecarLockFiles.RaceInjector.Value = _ => { publications++; return false; };
            try
            {
                foreach (string next in new[] { "D1", "D2", "D3" })
                {
                    Ok(u.Read(out _));
                    Ok(u.Rewrite(next));
                    Assert.Single(MutexFiles(f));
                }
            }
            finally { SidecarLockFiles.RaceInjector.Value = null; }
            Assert.Equal(1, publications);
            Ok(u.Close());
            Assert.False(Directory.Exists(SequentialJournal.DirectoryOf(f)));
            Assert.Equal("D1D2D3", File.ReadAllText(f, Encoding.Latin1));
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>Two writers interleaving: the byte excludes every other endpoint exactly while it is HELD, and a parked byte excludes
    /// no one (PB3340 R1-1's guarantee, which parking keeps).</summary>
    [Fact]
    public void A_parked_entry_mutex_excludes_no_one_and_a_held_one_excludes_everyone()
    {
        string dir = ScratchDirectory.Create("sepk-");
        string f = Path.Combine(dir, "f.dat");
        try
        {
            using var keeper = HostRegionLocks.KeptEntryMutex(f);
            using var other = HostRegionLocks.EntryMutex(f);
            Assert.Equal(RegionLockResult.Ok, keeper.Hold(HostRegionLocks.EntryMutexByte, exclusive: true, wait: true));
            Assert.Matches(Held, Assert.Single(MutexFiles(f)));
            Assert.Equal(RegionLockResult.Held, other.Hold(HostRegionLocks.EntryMutexByte, exclusive: true, wait: false));   // held: the other writer waits
            keeper.Release(HostRegionLocks.EntryMutexByte);
            Assert.Matches(Parked, Assert.Single(MutexFiles(f)));
            Assert.Equal(RegionLockResult.Ok, other.Hold(HostRegionLocks.EntryMutexByte, exclusive: true, wait: false));      // parked: nobody holds it
            Assert.Equal(RegionLockResult.Held, keeper.Hold(HostRegionLocks.EntryMutexByte, exclusive: true, wait: false));   // the other holds it now
            Assert.Matches(Parked, Assert.Single(MutexFiles(f), n => Parked.IsMatch(n)));   // the refused hold parked its holder again
            other.Release(HostRegionLocks.EntryMutexByte);
            Assert.Equal(RegionLockResult.Ok, keeper.Hold(HostRegionLocks.EntryMutexByte, exclusive: true, wait: false));
            Assert.Equal(RegionLockResult.Held, other.Hold(HostRegionLocks.EntryMutexByte, exclusive: true, wait: false));
            keeper.Release(HostRegionLocks.EntryMutexByte);
            keeper.Release(HostRegionLocks.EntryMutexByte);   // not held: nothing happens
            Assert.Matches(Parked, Assert.Single(MutexFiles(f)));
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    [Fact]
    public void Disposing_the_kept_entry_mutex_retires_its_parked_file_and_the_lock_directory()
    {
        string dir = ScratchDirectory.Create("sepk-");
        string f = Path.Combine(dir, "f.dat");
        try
        {
            var keeper = HostRegionLocks.KeptEntryMutex(f);
            Assert.Equal(RegionLockResult.Ok, keeper.Hold(HostRegionLocks.EntryMutexByte, exclusive: true, wait: true));
            keeper.Release(HostRegionLocks.EntryMutexByte);
            Assert.True(Directory.Exists(SequentialJournal.DirectoryOf(f)));
            keeper.Dispose();
            Assert.False(Directory.Exists(SequentialJournal.DirectoryOf(f)));
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>A writer killed while it kept the byte parked leaves its file with the lock gone (the operating environment drops it
    /// with the descriptor): the next publication by a parking endpoint sweeps it, as a tester sweeps a dead holder's file, so a
    /// killed run unit's litter never grows.</summary>
    [Fact]
    public void A_parked_file_of_a_dead_writer_is_swept_by_the_next_publication()
    {
        string dir = ScratchDirectory.Create("sepk-");
        string f = Path.Combine(dir, "f.dat");
        try
        {
            string lockDirectory = SidecarLockFiles.LockDirectoryOf(f);
            Directory.CreateDirectory(lockDirectory);
            string dead = Path.Combine(lockDirectory, $"i{HostRegionLocks.EntryMutexByte:x16}-{Guid.NewGuid():N}");
            File.WriteAllBytes(dead, []);   // no descriptor holds a lock on it: the lock a killed run unit held is gone
            using var keeper = HostRegionLocks.KeptEntryMutex(f);
            Assert.Equal(RegionLockResult.Ok, keeper.Hold(HostRegionLocks.EntryMutexByte, exclusive: true, wait: true));
            Assert.False(File.Exists(dead), "the dead writer's parked file is swept");
            Assert.Matches(Held, Assert.Single(MutexFiles(f)));
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>A parked file another process removed or renamed away (a sweeper, an operator) is not an error: the hold falls back
    /// to a publication and succeeds.</summary>
    [Fact]
    public void A_parked_file_removed_underneath_the_endpoint_is_published_again()
    {
        string dir = ScratchDirectory.Create("sepk-");
        string f = Path.Combine(dir, "f.dat");
        try
        {
            using var keeper = HostRegionLocks.KeptEntryMutex(f);
            Assert.Equal(RegionLockResult.Ok, keeper.Hold(HostRegionLocks.EntryMutexByte, exclusive: true, wait: true));
            keeper.Release(HostRegionLocks.EntryMutexByte);
            string lockDirectory = SequentialJournal.DirectoryOf(f);
            string parked = Path.Combine(lockDirectory, Assert.Single(MutexFiles(f)));
            File.Move(parked, Path.Combine(lockDirectory, "gone-" + Guid.NewGuid().ToString("N")));   // taken away under it
            Assert.Equal(RegionLockResult.Ok, keeper.Hold(HostRegionLocks.EntryMutexByte, exclusive: true, wait: true));
            Assert.Matches(Held, Assert.Single(MutexFiles(f)));
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>The exception path of the changed WRITE: the medium refuses the record's bytes ('30' or '34'), the statement ends,
    /// and the byte has been given back (parked): another writer is not locked out, and the run unit's end retires it with the lock directory.</summary>
    [Fact]
    public void A_write_the_medium_refuses_gives_the_entry_mutex_back()
    {
        string dir = ScratchDirectory.Create("sepk-");
        string f = Path.Combine(dir, "f.dat");
        try
        {
            File.WriteAllText(f, "C1", Encoding.Latin1);
            bool refuse = false;
            var medium = new FaultHostMedium
            {
                BeforeOperation = op =>
                {
                    if (refuse && op == "write f.dat") throw new IOException("the test medium refused the record's bytes");
                },
            };
            var u = new Unit(f, covered: false, medium, sharing: FileSharing.AllOther);
            Ok(u.Open(FileOpenMode.Extend));
            Ok(u.Write("X1"));
            refuse = true;
            Assert.NotEqual("00", u.Write("X2"));
            refuse = false;
            using (var other = HostRegionLocks.EntryMutex(f))
                Assert.Equal(RegionLockResult.Ok, other.Hold(HostRegionLocks.EntryMutexByte, exclusive: true, wait: false));
            u.Reg.CloseAll();   // the permanent error (9.1.13.1) ends the connector's statements; the run unit's end closes it
            Assert.False(Directory.Exists(SequentialJournal.DirectoryOf(f)));
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
