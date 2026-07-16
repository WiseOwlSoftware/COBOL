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

    // ── P10 Step 8: the governed mutating verbs (§14.9.35 GR11/GR12 · §14.9.10 GR6/GR7 · §14.9.51 GR10/GR11)
    //    and the sequential-organization governed READ (§9.1.16 ordinal identity; §14.9.30 GR9/GR22) ──────────

    /// <summary>Two open RELATIVE sharers on one seeded physical file (record 1 = AAAAA).</summary>
    private static void TwoOpenSharers(string a, string b, string host)
    {
        TwoConnectorsOneFile(a, b, host);
        CobolFile.OpenOutput(a);
        CobolFile.SetRelativeKey(a, 1);
        Assert.Equal(FileStatusCode.Success, CobolFile.WriteKeyed(a, "AAAAA"));
        CobolFile.Close(a);
        CobolFile.OpenIO(a);
        CobolFile.OpenIO(b);
        Assert.Equal(FileStatusCode.Success, CobolFile.Status(a));
        Assert.Equal(FileStatusCode.Success, CobolFile.Status(b));
    }

    [Fact]
    public void RewriteAndDelete_HonorAnotherConnectorsLock_51_ThenUnlockFrees()
    {
        TwoOpenSharers("MA", "MB", "lk-mut.dat");
        CobolFile.SetRelativeKey("MA", 1);
        string st = CobolFile.ReadKeyed("MA", -1, "", out _);
        Assert.Equal(FileStatusCode.Success,
            CobolFile.ReadLockGovern("MA", st, FileRecordLock.WithLock, FileRetryKind.None, 0));   // MA locks record 1
        CobolFile.SetRelativeKey("MB", 1);
        // §14.9.35 GR11 — the record identified for rewriting is locked by another connector → 51 (not rewritten).
        Assert.Equal(FileStatusCode.RecordLocked,
            CobolFile.RewriteShared("MB", "BBBBB", -1, FileRecordLock.None, FileRetryKind.None, 0));
        // §14.9.10 GR6 — same for deletion; a bounded RETRY exhausts to 51, FOREVER deadlock-bails to 52 (§14.7.9).
        Assert.Equal(FileStatusCode.RecordLocked, CobolFile.DeleteShared("MB", "", FileRetryKind.Times, 2));
        Assert.Equal(FileStatusCode.Deadlock,
            CobolFile.RewriteShared("MB", "BBBBB", -1, FileRecordLock.None, FileRetryKind.Forever, 0));
        CobolFile.Unlock("MA", records: false);   // §14.9.47 GR1
        Assert.Equal(FileStatusCode.Success,
            CobolFile.RewriteShared("MB", "BBBBB", -1, FileRecordLock.None, FileRetryKind.None, 0));
    }

    [Fact]
    public void WriteShared_WithLock_LocksTheWrittenRecord_AndSingleLockingReleasesItOnTheNextWrite()
    {
        TwoOpenSharers("WA", "WB", "lk-wrlock.dat");
        CobolFile.SetRelativeKey("WA", 2);
        // §14.9.51 GR11 — WITH LOCK locks the record written.
        Assert.Equal(FileStatusCode.Success,
            CobolFile.WriteShared("WA", "BBBBB", -1, FileRecordLock.WithLock, FileRetryKind.None, 0));
        Assert.True(CobolFile.IsLockedByOther("WB", "2"));
        // §14.9.51 GR10 / §12.4.5.9 GR6 — single locking: the next WRITE releases the prior lock.
        CobolFile.SetRelativeKey("WA", 3);
        Assert.Equal(FileStatusCode.Success,
            CobolFile.WriteShared("WA", "CCCCC", -1, FileRecordLock.None, FileRetryKind.None, 0));
        Assert.False(CobolFile.IsLockedByOther("WB", "2"));
    }

    [Fact]
    public void ReadShared_SequentialOrdinals_Conflict51_AndAdvancingOnLockSkips()
    {
        CobolFile.Init();
        const string host = "lk-seq.dat";
        CobolFile.Register("SA", host, 5, false, false);
        CobolFile.Register("SB", host, 5, false, false);
        CobolFile.RegisterSharing("SA", FileSharing.AllOther, FileLockMode.Manual, false);
        CobolFile.RegisterSharing("SB", FileSharing.AllOther, FileLockMode.Manual, false);
        CobolFile.OpenOutput("SA");
        CobolFile.Write("SA", "AAAAA");
        CobolFile.Write("SA", "BBBBB");
        CobolFile.Write("SA", "CCCCC");
        CobolFile.Close("SA");
        CobolFile.OpenInput("SA");
        CobolFile.OpenInput("SB");
        // SA reads ordinal 1 WITH LOCK (§9.1.16 — the sequential lock identity is the ordinal position).
        Assert.True(CobolFile.ReadShared("SA", FileRecordLock.WithLock, false, FileRetryKind.None, 0, out string img));
        Assert.Equal("AAAAA", img);
        // SB's READ of ordinal 1 conflicts BEFORE the physical read (§14.9.30 GR9; FPI unchanged, GR10a) → 51.
        Assert.False(CobolFile.ReadShared("SB", FileRecordLock.None, false, FileRetryKind.None, 0, out _));
        Assert.Equal(FileStatusCode.RecordLocked, CobolFile.Status("SB"));
        // ADVANCING ON LOCK (§14.9.30 GR22) skip-scans the locked record — SB gets ordinal 2, no conflict raised.
        Assert.True(CobolFile.ReadShared("SB", FileRecordLock.None, true, FileRetryKind.None, 0, out img));
        Assert.Equal("BBBBB", img);
        // IGNORING LOCK would have delivered the locked record itself (GR12) — proven by the file_sharing_seq golden.
        CobolFile.Close("SA");
        CobolFile.Close("SB");
    }

    [Fact]
    public void DeleteFile_OpenByAnotherConnector_62_ThenCloseFrees()
    {
        CobolFile.Init();
        const string host = "lk-dfs.dat";
        CobolFile.Register("DA", host, 5, false, false);
        CobolFile.Register("DB", host, 5, false, false);
        CobolFile.OpenOutput("DA");
        CobolFile.Write("DA", "HELLO");
        // §9.1.13.9 item 2 / §14.9.10 GR15 — the physical file is open by ANOTHER connector → 62 (not deleted);
        // RETRY n TIMES exhausts to 62; FOREVER deadlock-bails to 52 (§14.7.9 — no external closer in one run unit).
        Assert.Equal(FileStatusCode.DeleteFileSharing, CobolFile.DeleteFile("DB"));
        Assert.Equal(FileStatusCode.DeleteFileSharing, CobolFile.DeleteFile("DB", FileRetryKind.Times, 2));
        Assert.Equal(FileStatusCode.Deadlock, CobolFile.DeleteFile("DB", FileRetryKind.Forever, 0));
        CobolFile.Close("DA");
        Assert.Equal(FileStatusCode.Success, CobolFile.DeleteFile("DB"));            // GR20 — deleted
        Assert.Equal(FileStatusCode.OptionalFileNotFound, CobolFile.DeleteFile("DB"));   // GR14 — absent = successful '05'
    }
}
