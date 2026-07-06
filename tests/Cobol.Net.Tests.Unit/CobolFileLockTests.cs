// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The COBOL-2002 file-sharing / record-locking registry (Phase 4d M2-FILE-1; ISO/IEC 1989:2023 §9.1.15/§9.1.16
/// / §12.4.5.9 / §14.9.27 / §14.9.47 / §14.7.9). End-to-end behavior (OPEN 61, WITH LOCK 51, RETRY, IGNORING
/// LOCK, UNLOCK, AUTOMATIC auto-lock) rides the <c>file_sharing</c> golden; these lock the
/// <c>CobolFile.Locks</c> primitives the emitter wires to: the record-lock grant/deny, the 53/54 ceilings, the
/// Table-19 open-conflict classifier, and the single-run-unit RETRY loop.
/// </summary>
public sealed class CobolFileLockTests
{
    private const int Random = 1;   // KeyedAccess.Random

    /// <summary>Register two RELATIVE connectors bound to ONE physical file (the two-SELECTs-one-file scenario
    /// §9.1.15 note :11729 makes 51/61 realizable in a single run unit), both MANUAL sharers.</summary>
    private static void TwoConnectorsOneFile(string a, string b, string host)
    {
        CobolFile.Init();
        CobolFile.RegisterRelative(a, host, 8, false, Random, 4);
        CobolFile.RegisterRelative(b, host, 8, false, Random, 4);
        CobolFile.RegisterSharing(a, FileSharing.AllOther, FileLockMode.Manual, false);
        CobolFile.RegisterSharing(b, FileSharing.AllOther, FileLockMode.Manual, false);
    }

    [Fact]
    public void RecordLock_Grants_ThenBlocksAnotherConnector_51()
    {
        TwoConnectorsOneFile("CA", "CB", "lk-grant.dat");
        Assert.Equal(FileStatusCode.Success, CobolFile.LockRecord("CA", "1"));
        Assert.True(CobolFile.IsLockedByOther("CB", "1"));
        Assert.Equal(FileStatusCode.RecordLocked, CobolFile.LockRecord("CB", "1"));   // 51 — another connector holds it
        Assert.False(CobolFile.IsLockedByOther("CA", "1"));                            // GR8: not "other" to its own owner
    }

    [Fact]
    public void SelfRelock_IsIdempotent_GR8()
    {
        TwoConnectorsOneFile("CA", "CB", "lk-self.dat");
        Assert.Equal(FileStatusCode.Success, CobolFile.LockRecord("CA", "7"));
        Assert.Equal(FileStatusCode.Success, CobolFile.LockRecord("CA", "7"));   // re-lock of a held record: idempotent
    }

    [Fact]
    public void Unlock_ReleasesEveryLock_ThenAnotherConnectorMayLock()
    {
        TwoConnectorsOneFile("CA", "CB", "lk-unlock.dat");
        CobolFile.LockRecord("CA", "1");
        CobolFile.LockRecord("CA", "2");
        Assert.True(CobolFile.IsLockedByOther("CB", "1"));
        CobolFile.ReleaseAllForConnector("CA");                                   // UNLOCK / CLOSE (§9.1.16 :11754)
        Assert.False(CobolFile.IsLockedByOther("CB", "1"));
        Assert.Equal(FileStatusCode.Success, CobolFile.LockRecord("CB", "1"));
    }

    [Fact]
    public void ReleaseSingle_ReleasesOnlyTheNamedRecord()
    {
        TwoConnectorsOneFile("CA", "CB", "lk-single.dat");
        CobolFile.LockRecord("CA", "1");
        CobolFile.LockRecord("CA", "2");
        CobolFile.ReleaseSingle("CA", "1");
        Assert.False(CobolFile.IsLockedByOther("CB", "1"));
        Assert.True(CobolFile.IsLockedByOther("CB", "2"));   // record 2 stays locked
    }

    [Fact]
    public void ConnectorLockLimit_16thRecord_Is54()
    {
        CobolFile.Init();
        CobolFile.RegisterRelative("CA", "lk-conn.dat", 8, false, Random, 4);
        CobolFile.RegisterSharing("CA", FileSharing.AllOther, FileLockMode.Manual, true);   // MULTIPLE — many locks
        for (int i = 1; i <= 15; i++)
            Assert.Equal(FileStatusCode.Success, CobolFile.LockRecord("CA", i.ToString()));   // GR7 impl max ≥15
        Assert.Equal(FileStatusCode.ConnectorLockLimit, CobolFile.LockRecord("CA", "16"));    // 54
    }

    [Fact]
    public void RunUnitLockLimit_256thRunUnitLock_Is53()
    {
        // 17 connectors × 15 locks (each at its own connector ceiling, each on its OWN physical file so no 51/54
        // cross-talk) = 255 run-unit locks; an 18th connector's FIRST lock exceeds the run-unit max → 53.
        CobolFile.Init();
        for (int c = 0; c < 17; c++)
        {
            string name = "C" + c;
            CobolFile.RegisterRelative(name, $"lk-run-{c}.dat", 8, false, Random, 4);
            CobolFile.RegisterSharing(name, FileSharing.AllOther, FileLockMode.Manual, true);
            for (int i = 1; i <= 15; i++)
                Assert.Equal(FileStatusCode.Success, CobolFile.LockRecord(name, i.ToString()));
        }
        CobolFile.RegisterRelative("COVER", "lk-run-cover.dat", 8, false, Random, 4);
        CobolFile.RegisterSharing("COVER", FileSharing.AllOther, FileLockMode.Manual, true);
        Assert.Equal(FileStatusCode.RunUnitLockLimit, CobolFile.LockRecord("COVER", "1"));   // 53
    }

    [Theory]
    // Table-19 open-conflict matrix (§9.1.13.9 sub-cases a–e): existing (sharing,mode) vs incoming (sharing,mode).
    [InlineData(FileSharing.NoOther, FileOpenMode.Input, FileSharing.AllOther, FileOpenMode.Input, true)]   // (a) existing exclusive
    [InlineData(FileSharing.AllOther, FileOpenMode.IO, FileSharing.NoOther, FileOpenMode.Input, true)]      // (b) incoming exclusive
    [InlineData(FileSharing.ReadOnly, FileOpenMode.Input, FileSharing.AllOther, FileOpenMode.IO, true)]     // (c) READ-ONLY sharer, non-INPUT opener
    [InlineData(FileSharing.AllOther, FileOpenMode.IO, FileSharing.ReadOnly, FileOpenMode.Input, true)]     // (d) incoming READ-ONLY, non-INPUT existing
    [InlineData(FileSharing.AllOther, FileOpenMode.IO, FileSharing.AllOther, FileOpenMode.IO, false)]       // (e) ALL OTHER both — no conflict
    [InlineData(FileSharing.ReadOnly, FileOpenMode.Input, FileSharing.ReadOnly, FileOpenMode.Input, false)] // two READ-ONLY INPUT sharers — no conflict
    public void Conflicts_ClassifiesTable19(FileSharing exShare, FileOpenMode exMode,
        FileSharing inShare, FileOpenMode inMode, bool conflict) =>
        Assert.Equal(conflict, CobolFile.Conflicts((exShare, exMode), (inShare, inMode)));

    [Fact]
    public void RetryLoop_TimesExhausts_ForeverDeadlockBails_SuccessShortCircuits()
    {
        // A conflict that never clears: n TIMES exhausts to the conflict status (51), FOREVER bails to 52.
        Assert.Equal(FileStatusCode.RecordLocked,
            CobolFile.RetryLoop(() => FileStatusCode.RecordLocked, FileRetryKind.Times, 3));
        Assert.Equal(FileStatusCode.Deadlock,
            CobolFile.RetryLoop(() => FileStatusCode.RecordLocked, FileRetryKind.Forever, 0));
        // A conflict that clears on the 2nd attempt: TIMES stops as soon as it succeeds.
        int calls = 0;
        Assert.Equal(FileStatusCode.Success, CobolFile.RetryLoop(
            () => { calls++; return calls >= 2 ? FileStatusCode.Success : FileStatusCode.RecordLocked; },
            FileRetryKind.Times, 5));
        Assert.Equal(2, calls);
    }

    [Fact]
    public void Unlock_OfANotOpenFile_Is42()
    {
        CobolFile.Init();
        CobolFile.RegisterRelative("CX", "lk-notopen.dat", 8, false, Random, 4);
        CobolFile.RegisterSharing("CX", FileSharing.AllOther, FileLockMode.Manual, false);
        CobolFile.Unlock("CX", records: false);
        Assert.Equal(FileStatusCode.FileNotOpen, CobolFile.Status("CX"));   // 42 — §9.1.13 (:11579)
    }
}
