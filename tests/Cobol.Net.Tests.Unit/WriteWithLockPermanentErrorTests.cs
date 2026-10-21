// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB1880 item 4 — a WRITE ... WITH LOCK under a permanent error in effect. ISO §9.1.13.1: the condition
/// "remains in effect for all subsequent input-output operations on the file unless an implementor-defined technique
/// is invoked" (DOC-A.1-105: CLOSE), and the record lock a WRITE requests is set only "when the execution of the
/// WRITE statement is successful" (§14.9.51.4 GR11). So a WRITE WITH LOCK the condition refuses must answer the
/// status that began it, store nothing and lock nothing, and the answer must come BEFORE the §12.4.5.9.4 GR7 lock
/// ceiling is judged: a connector that holds its maximum number of locks answers '54' to a healthy WRITE WITH LOCK,
/// and '34' while the permanent error is in effect (<c>FileRegistry.WriteSharedInStatement</c> pre-flights nothing:
/// the organization replays the condition first, then asks the commit gate).
/// <para>The condition is started through <see cref="CobolFile.ReadExceedsRecordMaximum"/> — the entry the generated
/// code uses for the §9.1.13.6 item 4 b) '34' of a READ — so every organization (sequential, relative, indexed) gets
/// a real producer through the facade, and the replay is asked of each organization's own WRITE precondition. The
/// COBOL-level witness is <c>pb1880_write_with_lock_under_permanent_error</c>.</para>
/// </summary>
public sealed class WriteWithLockPermanentErrorTests
{
    private const int Random = 1;    // KeyedAccess.Random
    private const int Dynamic = 2;   // KeyedAccess.Dynamic
    private const string Stuck = FileStatusCode.PermanentBoundary;   // '34'

    /// <summary>Hold the connector's maximum number of record locks (§12.4.5.9.4 GR7; fifteen in this implementation).</summary>
    private static void HoldTheMaximumNumberOfLocks(string name)
    {
        for (int i = 1; i <= 15; i++)
            Assert.Equal(FileStatusCode.Success, CobolFile.LockRecord(name, i.ToString()));
    }

    public static TheoryData<FileRetryKind, long> EveryRetryForm() => new()
    {
        { FileRetryKind.None, 0 }, { FileRetryKind.Times, 3 }, { FileRetryKind.Seconds, 30 }, { FileRetryKind.Forever, 0 },
    };

    /// <summary>One scenario for every organization: at the lock ceiling a healthy WRITE WITH LOCK is '54' (the control: the
    /// ceiling IS what would answer), after the permanent error every form of WRITE answers '34' under every RETRY form,
    /// and a CLOSE ends it.</summary>
    private static void AssertCeilingOutrankedByThePermanentError(string name, string host, Action register, string image,
        FileRetryKind kind, long amount, Action beforeWrite, string absent, Func<string> readBackStatus)
    {
        CobolFile.Init();
        register();
        CobolFile.RegisterSharing(name, FileSharing.AllOther, FileLockMode.Manual, true);   // MULTIPLE
        CobolFile.OpenOutput(name, host, assignDynamic: false, page: null);
        HoldTheMaximumNumberOfLocks(name);
        beforeWrite();
        Assert.Equal(FileStatusCode.ConnectorLockLimit,
            CobolFile.WriteShared(name, image, -1, FileRecordLock.WithLock, kind, amount, page: null));   // the control

        Assert.Equal(Stuck, CobolFile.ReadExceedsRecordMaximum(name));   // the producer: §9.1.13.6 item 4 b)
        beforeWrite();
        Assert.Equal(Stuck, CobolFile.WriteShared(name, image, -1, FileRecordLock.WithLock, kind, amount, page: null));
        Assert.Equal(Stuck, CobolFile.Status(name));
        beforeWrite();
        Assert.Equal(Stuck, CobolFile.WriteShared(name, image, -1, FileRecordLock.None, kind, amount, page: null));
        beforeWrite();
        Assert.Equal(Stuck, CobolFile.WriteShared(name, image, -1, FileRecordLock.WithNoLock, kind, amount, page: null));

        CobolFile.Close(name);   // the technique; it also gives back the fifteen locks (§9.1.16)
        Assert.Equal(FileStatusCode.Success, CobolFile.Status(name));
        Assert.Equal(absent, readBackStatus());   // nothing was stored while the condition held: the organization's own "absent" answer
    }

    [Theory]
    [MemberData(nameof(EveryRetryForm))]
    public void Sequential_WriteWithLock_AtTheCeiling_AnswersThePermanentError_StoresNothing(FileRetryKind kind, long amount)
    {
        string host = FileHarness.Tmp("seq");
        try
        {
            AssertCeilingOutrankedByThePermanentError("PS", host, () => CobolFile.Register("PS", host, 5, false, false),
                "AAAAA", kind, amount, () => { }, FileStatusCode.AtEnd,
                readBackStatus: () =>
                {
                    CobolFile.OpenInput("PS", host, assignDynamic: false, page: null);
                    string status = CobolFile.ReadShared("PS", false, FileRecordLock.None, false, false, FileRetryKind.None, 0, out _);
                    CobolFile.Close("PS");
                    return status;
                });
        }
        finally { FileHarness.TryDelete(host); }
    }

    [Theory]
    [MemberData(nameof(EveryRetryForm))]
    public void Relative_WriteWithLock_AtTheCeiling_AnswersThePermanentError_StoresNothing(FileRetryKind kind, long amount)
    {
        string host = FileHarness.Tmp("rel");
        try
        {
            AssertCeilingOutrankedByThePermanentError("PR", host, () => CobolFile.RegisterRelative("PR", host, 5, false, Random, 4),
                "AAAAA", kind, amount, () => CobolFile.SetRelativeKey("PR", 16), FileStatusCode.RecordNotFound,
                readBackStatus: () =>
                {
                    CobolFile.OpenIO("PR", host, assignDynamic: false, page: null);
                    CobolFile.SetRelativeKey("PR", 16);
                    string status = CobolFile.ReadKeyedShared("PR", -1, "", FileRecordLock.None, false, FileRetryKind.None, 0, out _);
                    CobolFile.Close("PR");
                    return status;
                });
        }
        finally { FileHarness.TryDelete(host); }
    }

    [Theory]
    [MemberData(nameof(EveryRetryForm))]
    public void Indexed_WriteWithLock_AtTheCeiling_AnswersThePermanentError_StoresNothing(FileRetryKind kind, long amount)
    {
        string host = FileHarness.Tmp("idx");
        try
        {
            AssertCeilingOutrankedByThePermanentError("PX", host,
                () => CobolFile.RegisterIndexed("PX", host, 8, false, Dynamic, [new(0, 4)]),
                "AAAA0016", kind, amount, () => { }, FileStatusCode.RecordNotFound,
                readBackStatus: () =>
                {
                    CobolFile.OpenIO("PX", host, assignDynamic: false, page: null);
                    string status = CobolFile.ReadKeyedShared("PX", -1, "AAAA0016", FileRecordLock.None, false, FileRetryKind.None, 0, out _);
                    CobolFile.Close("PX");
                    return status;
                });
        }
        finally { FileHarness.TryDelete(host); }
    }

    /// <summary>The lock itself: a relative WRITE WITH LOCK the permanent error refuses takes NO record lock (§14.9.51.4
    /// GR11 — only a successful WRITE sets it), and the control — the same WRITE after the CLOSE, which succeeds — does:
    /// another connector's lock request for that record answers '51'. The first assertion would also hold if the
    /// identity "2" were not the lock the WRITE takes, so the control pins that it is.</summary>
    [Fact]
    public void Relative_RefusedWriteWithLock_SetsNoRecordLock_TheControlWriteDoes()
    {
        string host = FileHarness.Tmp("rellock");
        try
        {
            CobolFile.Init();
            foreach (string n in new[] { "RA", "RB" })
            {
                CobolFile.RegisterRelative(n, host, 5, false, Random, 4);
                CobolFile.RegisterSharing(n, FileSharing.AllOther, FileLockMode.Manual, true);
            }
            CobolFile.OpenOutput("RA", host, assignDynamic: false, page: null);
            CobolFile.SetRelativeKey("RA", 0);
            Assert.Equal(Stuck, CobolFile.WriteShared("RA", "AAAAA", -1, FileRecordLock.WithLock, FileRetryKind.None, 0, page: null));   // §14.9.51.4 GR29 b)
            CobolFile.SetRelativeKey("RA", 2);
            Assert.Equal(Stuck, CobolFile.WriteShared("RA", "BBBBB", -1, FileRecordLock.WithLock, FileRetryKind.None, 0, page: null));   // would be '00' and lock record 2
            Assert.False(CobolFile.IsLockedByOther("RB", "2"));
            Assert.Equal(FileStatusCode.Success, CobolFile.LockRecord("RB", "2"));      // RB may lock it: RA holds nothing
            CobolFile.ReleaseAllForConnector("RB");

            CobolFile.Close("RA");
            CobolFile.OpenIO("RA", host, assignDynamic: false, page: null);
            CobolFile.SetRelativeKey("RA", 2);
            Assert.Equal(FileStatusCode.Success, CobolFile.WriteShared("RA", "BBBBB", -1, FileRecordLock.WithLock, FileRetryKind.None, 0, page: null));
            Assert.True(CobolFile.IsLockedByOther("RB", "2"));                                // the control: this WRITE does lock record 2
            Assert.Equal(FileStatusCode.RecordLocked, CobolFile.LockRecord("RB", "2"));
            CobolFile.Close("RA");
        }
        finally { FileHarness.TryDelete(host); }
    }
}
