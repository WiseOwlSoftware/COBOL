// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A PERMANENT ERROR IN EFFECT OUTRANKS THE RECORD LOCK A REWRITE OR A DELETE WOULD HAVE MET, AND THE RULE IS WRITTEN IN
/// ONE PLACE (kb/Work PB3416, the REWRITE and DELETE arms of PB3336 review R1-2). ISO §9.1.13.1: the permanent error
/// condition <i>"remains in effect for all subsequent input-output operations on the file unless an implementor-defined
/// technique is invoked to correct the permanent error condition"</i>; this processor's technique is CLOSE
/// (docs/CONFORMANCE.md DOC-A.1-105), which also says the replayed statement <i>"does not touch the physical file, it
/// sets the same I-O status again"</i> and that the condition outranks every other status the statement would have taken.
/// So a REWRITE or DELETE of a record ANOTHER file connector holds locked answers the status that began the condition,
/// not '51' (§14.9.35.4 GR11, §14.9.10.4 GR6, §9.1.13.8 item 1), waits out no RETRY phrase, rewrites or deletes nothing
/// and releases nobody's lock; once the CLOSE has ended the condition, the same statement is '51' again.
/// <para>The cells are the three organizations, the holder in the same run unit and in another one (a second
/// <see cref="FileRegistry"/>, whose record locks reach the first through the host's region locks, kb/Work PB2660, on
/// both region hosts), and each verb the organization has (§14.9.10.3 SR1: DELETE RECORD is not for a sequential file). The
/// producer is <see cref="FileRegistry.ReadExceedsRecordMaximum"/>, the '34' of §9.1.13.6 item 4 b), which starts the
/// condition on an OPEN connector exactly as a host failure's '30' does.</para>
/// </summary>
public sealed class PermanentErrorOutranksRecordLockTests
{
    public enum Org { Sequential, Relative, Indexed }
    public enum Place { SameRunUnit, OtherRunUnit, OtherRunUnitSidecarHost }
    public enum Verb { Rewrite, Delete }

    private const string Stuck = FileStatusCode.PermanentBoundary;   // '34'

    private static string Tmp(Org org) => Path.Combine(Path.GetTempPath(), $"pb3416-{org}-{Guid.NewGuid():N}.dat");

    public static TheoryData<Org, Place, Verb> Cells()
    {
        var data = new TheoryData<Org, Place, Verb>();
        foreach (var org in Enum.GetValues<Org>())
            foreach (var place in Enum.GetValues<Place>())
                foreach (var verb in Enum.GetValues<Verb>())
                    if (org != Org.Sequential || verb == Verb.Rewrite)   // §14.9.10.3 SR1
                        data.Add(org, place, verb);
        return data;
    }

    private static void Register(FileRegistry reg, string name, string host, Org org, bool shared)
    {
        switch (org)
        {
            case Org.Sequential:
                reg.Register(name, host, recordWidth: 4, lineSequential: false, optional: false, varyMin: -1, varyMax: -1);
                break;
            case Org.Relative:
                reg.RegisterRelative(name, host, recordWidth: 8, optional: false, accessMode: (int)KeyedAccess.Dynamic,
                    relativeKeyDigits: 4, varyMin: -1, varyMax: -1);
                break;
            default:
                reg.RegisterIndexed(name, host, recordWidth: 8, optional: false, accessMode: (int)KeyedAccess.Dynamic,
                    primeKey: [new(0, 4)], varyMin: -1, varyMax: -1);
                break;
        }
        if (shared) reg.RegisterSharing(name, FileSharing.AllOther, FileLockMode.Manual, multiple: false);
    }

    private static string Image(Org org, int n, string payload) => org == Org.Sequential ? payload : $"{n:0000}{payload}";

    private static readonly string[] Payloads = ["AAAA", "BBBB", "CCCC"];

    private static void Seed(string host, Org org)
    {
        var seed = new FileRegistry();
        Register(seed, "S", host, org, shared: false);
        seed.OpenStatic("S", FileOpenMode.Output);
        for (int n = 1; n <= Payloads.Length; n++)
        {
            if (org == Org.Relative) seed.SetRelativeKey("S", n);
            Assert.Equal("00", seed.WriteShared("S", Image(org, n, Payloads[n - 1]), -1, FileRecordLock.None,
                FileRetryKind.None, 0, page: null));
        }
        seed.Close("S");
        Assert.Equal("00", seed.Status("S"));
    }

    private static string ReadNext(FileRegistry reg, string name, FileRecordLock phrase, bool ignoringLock, out string image) =>
        reg.ReadShared(name, previous: false, phrase, advancingOnLock: false, ignoringLock, FileRetryKind.None, 0, out image);

    /// <summary>Every record of the file, as the next run unit's OPEN INPUT and READ NEXT loop sees it.</summary>
    private static List<string> Records(string host, Org org)
    {
        var reader = new FileRegistry();
        Register(reader, "R", host, org, shared: false);
        reader.OpenStatic("R", FileOpenMode.Input);
        Assert.Equal("00", reader.Status("R"));
        var all = new List<string>();
        while (ReadNext(reader, "R", FileRecordLock.None, ignoringLock: false, out string image) == "00") all.Add(image);
        reader.Close("R");
        return all;
    }

    /// <summary>Connector A takes record 1's lock (§14.9.30.4 GR11 b)) and holds it.</summary>
    private static void HoldRecordOne(FileRegistry a, Org org)
    {
        string status;
        if (org == Org.Sequential)
            status = ReadNext(a, "A", FileRecordLock.WithLock, ignoringLock: false, out _);
        else
        {
            if (org == Org.Relative) a.SetRelativeKey("A", 1);
            status = a.ReadKeyedShared("A", -1, Image(org, 1, "    "), FileRecordLock.WithLock, ignoringLock: false,
                FileRetryKind.None, 0, out _);
        }
        Assert.Equal("00", status);
    }

    /// <summary>Make record 1 the record B's REWRITE or DELETE identifies: a sequential statement follows a successful READ
    /// (§14.9.35.4 GR5, §14.9.10.4 GR2) and takes the record that READ made current, which B can read only IGNORING LOCK
    /// (§14.9.30.4 GR12) while A holds it; a keyed statement names it in the key.</summary>
    private static void AimAtRecordOne(FileRegistry b, Org org)
    {
        if (org == Org.Sequential) Assert.Equal("00", ReadNext(b, "B", FileRecordLock.None, ignoringLock: true, out _));
        else if (org == Org.Relative) b.SetRelativeKey("B", 1);
    }

    private static string Mutate(FileRegistry b, Org org, Verb verb, FileRetryKind retry, long amount) => verb == Verb.Rewrite
        ? b.RewriteShared("B", Image(org, 1, "ZZZZ"), -1, FileRecordLock.None, retry, amount)
        : b.DeleteShared("B", Image(org, 1, "    "), retry, amount);

    [Theory]
    [MemberData(nameof(Cells))]
    public void AReplayedPermanentError_OutranksTheOtherConnectorsRecordLock_OnRewriteAndDelete(Org org, Place place, Verb verb)
    {
        using var forced = RegionHostSeam.Use(place == Place.OtherRunUnitSidecarHost ? RegionHostKind.LockFiles : RegionHostKind.OfThisProcess);
        string host = Tmp(org);
        var pauses = new List<TimeSpan>();
        FileRegistry.PauseObserver.Value = pauses.Add;
        try
        {
            Seed(host, org);
            var a = new FileRegistry();
            var b = place == Place.SameRunUnit ? a : new FileRegistry();
            Register(a, "A", host, org, shared: true);
            Register(b, "B", host, org, shared: true);
            a.OpenStatic("A", FileOpenMode.IO);
            b.OpenStatic("B", FileOpenMode.IO);
            Assert.Equal("00", a.Status("A"));
            Assert.Equal("00", b.Status("B"));
            HoldRecordOne(a, org);

            // The control: with no condition in effect the statement meets A's lock — '51' (§14.9.35.4 GR11 / §14.9.10.4
            // GR6 b), §9.1.13.8 item 1). A RETRY phrase re-attempts its bounded number of times (§14.7.9.3 GR4)
            // and pauses between attempts only for a holder outside the run unit — the one case a wait
            // can change the answer (FileRegistry.Reattempt).
            AimAtRecordOne(b, org);
            Assert.Equal("51", Mutate(b, org, verb, FileRetryKind.None, 0));
            Assert.Empty(pauses);
            Assert.Equal("51", Mutate(b, org, verb, FileRetryKind.Times, 2));
            Assert.Equal(place == Place.SameRunUnit ? 0 : 2, pauses.Count);
            pauses.Clear();

            // The condition begins (§9.1.13.6 item 4 b)); the SAME statements now answer it: no '51', no wait, no effect.
            Assert.Equal(Stuck, b.ReadExceedsRecordMaximum("B"));
            Assert.Equal(Stuck, Mutate(b, org, verb, FileRetryKind.None, 0));
            Assert.Equal(Stuck, b.Status("B"));
            Assert.Equal(Stuck, Mutate(b, org, verb, FileRetryKind.Times, 2));
            Assert.Equal(Stuck, b.Status("B"));
            Assert.Empty(pauses);

            // Nothing was rewritten or deleted, and A's lock is untouched: a third connector still meets it.
            var c = new FileRegistry();
            Register(c, "C", host, org, shared: true);
            c.OpenStatic("C", FileOpenMode.IO);
            Assert.Equal("00", c.Status("C"));
            Assert.Equal("51", ReadNext(c, "C", FileRecordLock.WithLock, ignoringLock: false, out _));
            c.Close("C");

            // CLOSE is the technique that ends the condition (DOC-A.1-105): the next OPEN starts clean, and the statement
            // meets the lock again.
            b.Close("B");
            Assert.Equal("00", b.Status("B"));
            b.OpenStatic("B", FileOpenMode.IO);
            Assert.Equal("00", b.Status("B"));
            AimAtRecordOne(b, org);
            Assert.Equal("51", Mutate(b, org, verb, FileRetryKind.None, 0));
            b.Close("B");
            a.Close("A");

            Assert.Equal(Payloads.Select((p, i) => Image(org, i + 1, p)).ToList(), Records(host, org));
        }
        finally
        {
            FileRegistry.PauseObserver.Value = null;
            try { File.Delete(host); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }

    public enum Statement { ReadNext, ReadKey, Rewrite, Delete }

    public static TheoryData<Org, Statement> HostQuestionCells()
    {
        var data = new TheoryData<Org, Statement>();
        foreach (var org in Enum.GetValues<Org>())
            foreach (var statement in Enum.GetValues<Statement>())
                if (org != Org.Sequential || statement is Statement.ReadNext or Statement.Rewrite)   // no keyed READ or DELETE RECORD on a sequential file
                    data.Add(org, statement);
        return data;
    }

    private static string Run(FileRegistry b, Org org, Statement statement)
    {
        switch (statement)
        {
            case Statement.ReadNext:
                return ReadNext(b, "B", FileRecordLock.None, ignoringLock: false, out _);
            case Statement.ReadKey:
                if (org == Org.Relative) b.SetRelativeKey("B", 1);
                return b.ReadKeyedShared("B", -1, Image(org, 1, "    "), FileRecordLock.None, ignoringLock: false,
                    FileRetryKind.None, 0, out _);
            default:
                return Mutate(b, org, statement == Statement.Rewrite ? Verb.Rewrite : Verb.Delete, FileRetryKind.None, 0);
        }
    }

    /// <summary>⛔ THE HOST IS ASKED NOTHING. Another run unit's record locks are a question put to the host — the presence
    /// byte, then the record's byte (<c>PhysicalFileTable.AnotherRunUnitHoldsLocks</c>, <c>HolderOf</c>; kb/Work PB2660) —
    /// and a statement the permanent error has settled owes it none (§9.1.13.1; DOC-A.1-105: it <i>"does not touch the
    /// physical file"</i>). The record lock governance used to ask the presence byte before the REWRITE or DELETE replayed
    /// the condition (kb/Work PB3416). A host that counts <c>HeldByAnother</c> shows it: a healthy statement asks (the
    /// control), the same statement under the condition asks nothing and answers the status that began it, for every
    /// governed READ, REWRITE and DELETE on every organization.</summary>
    [Theory]
    [MemberData(nameof(HostQuestionCells))]
    public void AReplayedPermanentError_PutsNoQuestionToTheHost_OnAnyGovernedStatement(Org org, Statement statement)
    {
        var counting = new CountingHost(SidecarRegionLocks.Host);
        using var forced = HostRegionLocks.Forcing(counting);
        string host = Tmp(org);
        try
        {
            Seed(host, org);
            var a = new FileRegistry();
            var b = new FileRegistry();
            Register(a, "A", host, org, shared: true);
            Register(b, "B", host, org, shared: true);
            a.OpenStatic("A", FileOpenMode.IO);
            b.OpenStatic("B", FileOpenMode.IO);
            Assert.Equal("00", a.Status("A"));
            Assert.Equal("00", b.Status("B"));
            HoldRecordOne(a, org);

            // The control: a healthy statement of another run unit's connector asks the host about A's locks.
            AimAtRecordOne(b, org);
            int asked = counting.HeldByAnotherCalls;
            string healthy = Run(b, org, statement);
            Assert.True(counting.HeldByAnotherCalls > asked, $"the control asked the host (status '{healthy}')");
            if (statement is Statement.Rewrite or Statement.Delete) Assert.Equal("51", healthy);

            Assert.Equal(Stuck, b.ReadExceedsRecordMaximum("B"));
            asked = counting.HeldByAnotherCalls;
            Assert.Equal(Stuck, Run(b, org, statement));
            Assert.Equal(Stuck, b.Status("B"));
            Assert.Equal(Stuck, Run(b, org, statement));
            Assert.Equal(asked, counting.HeldByAnotherCalls);

            b.Close("B");
            a.Close("A");
        }
        finally { try { File.Delete(host); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }

    /// <summary>A region-lock host that delegates to <paramref name="inner"/> and counts the questions put to it.</summary>
    private sealed class CountingHost(IRegionLockHost inner) : IRegionLockHost
    {
        private int _heldByAnotherCalls;

        public int HeldByAnotherCalls => Volatile.Read(ref _heldByAnotherCalls);

        public RegionLocks Open(SafeFileHandle handle, string hostPath, bool writable) =>
            new Counting(inner.Open(handle, hostPath, writable), this);

        private sealed class Counting(RegionLocks endpoint, CountingHost owner) : RegionLocks(endpoint.Writable)
        {
            internal override bool ExclusiveNeedsWritableHandle => endpoint.ExclusiveNeedsWritableHandle;

            internal override RegionLockResult Hold(long offset, bool exclusive, bool wait) => endpoint.Hold(offset, exclusive, wait);

            internal override RegionLockResult HeldByAnother(long offset)
            {
                Interlocked.Increment(ref owner._heldByAnotherCalls);
                return endpoint.HeldByAnother(offset);
            }

            internal override void Release(long offset) => endpoint.Release(offset);

            public override void Dispose() => endpoint.Dispose();
        }
    }
}
