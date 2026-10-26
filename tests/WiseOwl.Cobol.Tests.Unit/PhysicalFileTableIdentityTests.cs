// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime.IO;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE RUN UNIT'S FILE AND RECORD LOCKS ARE OF A PHYSICAL FILE, NOT OF A PATH (kb/Work PB3576; §9.1.15, <i>"The successful
/// opening of a file establishes a file lock for the applicable sharing rules, thereby preventing other run units from opening
/// that file with incompatible sharing rules"</i>; §9.1.16, <i>"While locked by a given file connector, a record is not
/// accessible to another file connector"</i>). <see cref="PhysicalFileTable.For"/> finds a state by the host's identity of the file
/// (<see cref="HostFile.IdentityAt"/>), so these cases hand it identities of their own choosing and run on every host: the real
/// removal and replacement of a file under an open handle, which only Linux permits, is
/// <c>CommitRollbackRuntimeTests</c>'s.
/// </summary>
public sealed class PhysicalFileTableIdentityTests
{
    private static readonly PhysicalFileIdentity First = new(1, 100);
    private static readonly PhysicalFileIdentity Second = new(1, 200);

    /// <summary>One file under two names is one state, and one name for two files is two: the file made at a path after the first
    /// was renamed away meets none of its locks, and the first, reached under its new name, meets them all.</summary>
    [Fact]
    public void TheFileDecidesTheState_NeverItsName()
    {
        var table = new PhysicalFileTable();
        var original = table.For("/d/p.dat", First);
        Assert.Equal("00", table.LockRecord(original, "F", "7", holder: null));
        original.Open["F"] = (FileSharing.NoOther, FileOpenMode.IO);

        var replacement = table.For("/d/p.dat", Second);   // the first file was renamed away; another was made at the path
        Assert.NotSame(original, replacement);
        Assert.Empty(replacement.RecordLocks);
        Assert.Empty(replacement.Open);

        var renamed = table.For("/d/p.old", First);   // the first file, under its new name
        Assert.Same(original, renamed);
        Assert.Equal("51", table.LockRecord(renamed, "G", "7", holder: null));
        Assert.Equal("00", table.LockRecord(replacement, "G", "7", holder: null));   // record 7 of the OTHER file
        Assert.Same(replacement, table.For("/d/p.dat", Second));
    }

    /// <summary>The state of an absent path is the file's once it is made: the OPEN that creates the file registers its handle's
    /// identity (<see cref="PhysicalFileTable.Learn"/>), and every later question about the path or about the file's other name
    /// reaches it; a path that names nothing any more is a new, unknown state, not the vanished file's.</summary>
    [Fact]
    public void AnAbsentPathsStateBecomesTheFilesOnceItIsMade_AndAVanishedFileLeavesItsPath()
    {
        var table = new PhysicalFileTable();
        var absent = table.For("/d/q.dat", null);
        Assert.Same(absent, table.For("/d/q.dat", null));
        Assert.Null(absent.Identity);

        table.Learn(absent, First);   // OPEN OUTPUT made the file and its handle told which file it is
        Assert.Equal(First, absent.Identity);
        Assert.Same(absent, table.For("/d/q.dat", First));
        Assert.Same(absent, table.For("/d/q.link", First));

        var vanished = table.For("/d/q.dat", null);   // the file was removed: the path names nothing
        Assert.NotSame(absent, vanished);
        Assert.Null(vanished.Identity);
        Assert.Same(absent, table.For("/d/q.link", First));   // the removed file's state is still its connectors'
    }

    /// <summary>A state learns its file once: a second identity does not rebind it, and an identity a state holds open stays with
    /// that state (a connector's handle keeps its file alive, so the number cannot have been given to another file).</summary>
    [Fact]
    public void AStateLearnsItsFileOnce_AndAnOpenStateKeepsItsClaim()
    {
        var table = new PhysicalFileTable();
        var a = table.For("/d/a.dat", First);
        a.Open["F"] = (FileSharing.NoOther, FileOpenMode.IO);
        table.Learn(a, Second);
        Assert.Equal(First, a.Identity);
        var b = table.For("/d/b.dat", null);
        table.Learn(b, First);   // a connector has the first file open: it belongs to a
        Assert.Null(b.Identity);
        table.Learn(b, null);    // a host that cannot tell
        Assert.Null(b.Identity);
        Assert.Same(a, table.For("/d/anything", First));
    }

    /// <summary>⛔ A FILE'S NUMBER CAN OUTLIVE THE FILE (review R1-1 of kb/Work PB3576; <see cref="PhysicalFileIdentity"/>): ext4
    /// gives the number of a removed file to the next one made, at once. A state with no connector open holds no handle on its file,
    /// so the file made at the path takes the number from it: every later question about the path reaches the state of the
    /// connector that made the file, never the removed file's, which keeps what its holders reach.</summary>
    [Fact]
    public void AFileMadeWithAFreedNumberTakesItFromTheStateOfTheFileThatWasRemoved()
    {
        var table = new PhysicalFileTable();
        var removed = table.For("/d/s.dat", null);
        table.Learn(removed, First);   // OPEN OUTPUT made the file; CLOSE; DELETE FILE removed it; its number was freed
        Assert.Equal("00", table.LockRecord(removed, "H", "3", holder: null));   // a lock a retired kept state still holds

        var made = table.For("/d/s.dat", null);   // OPEN OUTPUT of the absent path: a state of its own ...
        Assert.NotSame(removed, made);
        made.Open["F"] = (FileSharing.NoOther, FileOpenMode.Output);
        table.Learn(made, First);                // ... and the host gave the new file the removed file's number

        Assert.Equal(First, made.Identity);
        Assert.Null(removed.Identity);
        Assert.Same(made, table.For("/d/s.dat", First));     // another connector's OPEN of the path meets F's Table 19 entry
        Assert.Same(made, table.For("/d/s.link", First));
        Assert.Equal("51", table.LockRecord(removed, "G", "3", holder: null));   // the removed file's holders keep their locks
        Assert.Equal("00", table.LockRecord(made, "G", "3", holder: null));
    }

    /// <summary>Where the host cannot tell files apart (no identity anywhere) the path decides, as it always did.</summary>
    [Fact]
    public void WhereTheHostCannotTell_ThePathDecides()
    {
        var table = new PhysicalFileTable();
        var one = table.For("/d/x.dat", null);
        Assert.Same(one, table.For("/d/x.dat", null));
        Assert.NotSame(one, table.For("/d/y.dat", null));
    }

    /// <summary>§12.4.5.9.4 GR7's ceiling is the RUN UNIT's: the implementor-defined <i>"maximum number of record locks that may be
    /// held by a run unit; that maximum shall be at least 255"</i> counts the locks of every physical file the table holds, a file reached under
    /// another name and a file set aside by a replacement at its path included. 255 locks across two files at one path, then
    /// one more: '53'.</summary>
    [Fact]
    public void TheRunUnitsLockCeilingCountsEveryPhysicalFileTheTableHolds()
    {
        var table = new PhysicalFileTable();
        var old = table.For("/d/r.dat", First);
        var current = table.For("/d/r.dat", Second);   // First was replaced at the path; its connectors' locks stand
        for (int c = 1; c <= 16; c++)
            for (int r = 1; r <= 15; r++)
                Assert.Equal("00", table.LockRecord(old, $"C{c}", $"{c}-{r}", holder: null));
        for (int r = 1; r <= 15; r++)
            Assert.Equal("00", table.LockRecord(current, "C17", $"17-{r}", holder: null));   // 16 x 15 + 15 = 255
        Assert.Equal("53", table.LockRecord(current, "C18", "1", holder: null));
        Assert.Equal("53", table.PreflightNewLock(old, "C19"));
    }
}
