// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A COHERENT STATEMENT'S ENTRY HAS ONLY I-O STATUSES AS OUTCOMES — never an escaping exception
/// (kb/Work PB2842). For both keyed organizations the entry is the keyed store engine's read of page 2
/// (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.7; kb/Work PB3333, PB3334): a page 2 that does not validate
/// under a live store is answered '30' by a writer, which cannot know what another run unit completed. ISO §9.1.13.6 1):
/// <i>"A permanent error exists and no further information is available"</i> is I-O status '30'. A keyed connector whose
/// file another run unit may write (every sharing mode but <c>SHARING WITH NO OTHER</c>) reads page 2 at the start of a
/// statement (<c>KeyedConnector.BeginStatement</c>), and a page 2 torn under it used to be an escaping exception's
/// shape: the refusal is a permanent error (§9.1.13.1: <i>"The permanent error condition remains in effect for all
/// subsequent input-output operations on the file"</i>), so the statement runs its ordinary permanent-error replay —
/// position invalidation for READ and START included — and touches no record; a CLOSE ends the condition. A page write
/// or a sync the host refuses is the statement's (or the CLOSE's) own '30' in both organizations, never deferred.
/// </summary>
public sealed class KeyedStoreReloadFailureDriftTests
{
    public enum Org { Relative, Indexed }

    public enum Verb { ReadNext, ReadRandom, Write, Rewrite, Delete, StartFirstLast, StartKey, PlainReadNext, PlainWrite }

    private static string Tmp(string tag) =>
        Path.Combine(Path.GetTempPath(), $"pb2842-{tag}-{Guid.NewGuid():N}.dat");

    private static void Register(FileRegistry reg, string name, string host, Org org, FileSharing? sharing = FileSharing.AllOther)
    {
        if (org == Org.Relative)
            reg.RegisterRelative(name, host, recordWidth: 8, optional: false, accessMode: (int)KeyedAccess.Dynamic,
                relativeKeyDigits: 4, varyMin: -1, varyMax: -1);
        else
            reg.RegisterIndexed(name, host, recordWidth: 8, optional: false, accessMode: (int)KeyedAccess.Dynamic,
                primeKey: [new(0, 4)], varyMin: -1, varyMax: -1);
        if (sharing is { } clause) reg.RegisterSharing(name, clause, FileLockMode.Manual, multiple: false);
    }

    private static string Rec(int n, string payload) => $"{n:0000}{payload}";

    private static string Write(FileRegistry reg, string name, Org org, int n, string payload)
    {
        if (org == Org.Relative) reg.SetRelativeKey(name, n);
        return reg.WriteShared(name, Rec(n, payload), -1, FileRecordLock.None, FileRetryKind.None, 0, page: null);
    }

    private static void Seed(string host, Org org)
    {
        var seed = new FileRegistry();
        Register(seed, "S", host, org, sharing: null);
        seed.OpenStatic("S", FileOpenMode.Output);
        Assert.Equal("00", Write(seed, "S", org, 1, "AAAA"));
        Assert.Equal("00", Write(seed, "S", org, 2, "BBBB"));
        seed.Close("S");
    }

    /// <summary>Make the next coherent entry of a writer unable to read the file: page 2 torn (its checksum fails,
    /// docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.6 R3) — the keyed store engine's one entry read, for both
    /// organizations.</summary>
    private static void MakeTheEntryUnreadable(string host)
    {
        using var page2 = File.OpenHandle(host, FileMode.Open, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
        var torn = new byte[CobolNet.Runtime.IO.Store.PageHeader.PageSize];
        Array.Fill(torn, (byte)0xAB);
        RandomAccess.Write(page2, torn, 2L * torn.Length);
    }

    private static string Run(FileRegistry reg, Verb verb, Org org, out string image)
    {
        image = "";
        if (org == Org.Relative && verb is not Verb.StartFirstLast) reg.SetRelativeKey("A", 2);
        switch (verb)
        {
            case Verb.ReadNext:
                return reg.ReadShared("A", previous: false, FileRecordLock.None, advancingOnLock: false,
                    ignoringLock: false, FileRetryKind.None, 0, out image);
            case Verb.ReadRandom:
                return reg.ReadKeyedShared("A", keyIndex: -1, Rec(2, "    "), FileRecordLock.None,
                    ignoringLock: false, FileRetryKind.None, 0, out image);
            case Verb.Write:
                return reg.WriteShared("A", Rec(9, "ZZZZ"), -1, FileRecordLock.None, FileRetryKind.None, 0, page: null);
            case Verb.Rewrite:
                return reg.RewriteShared("A", Rec(2, "ZZZZ"), -1, FileRecordLock.None, FileRetryKind.None, 0);
            case Verb.Delete:
                return reg.DeleteShared("A", Rec(2, "    "), FileRetryKind.None, 0);
            case Verb.StartFirstLast:
                return reg.StartFirstLast("A", last: false);
            case Verb.StartKey:
                return org == Org.Relative
                    ? reg.StartRelative("A", ">=", 2)
                    : reg.StartIndexed("A", keyIndex: -1, ">=", Rec(2, "    "), StartKeyLength.OfWidth(4));
            case Verb.PlainReadNext:
                return reg.ReadKeyedNext("A", out image);
            case Verb.PlainWrite:
                return reg.WriteKeyed("A", Rec(9, "ZZZZ"), -1);
            default:
                throw new ArgumentOutOfRangeException(nameof(verb));
        }
    }

    public static TheoryData<Org, Verb, RegionHostKind> Cases()
    {
        var data = new TheoryData<Org, Verb, RegionHostKind>();
        foreach (var org in Enum.GetValues<Org>())
            foreach (var verb in Enum.GetValues<Verb>())
                data.Add(org, verb, RegionHostKind.OfThisProcess);
        // The lock-file host carries the store mutex differently; one verb per organization is enough to cross it.
        foreach (var org in Enum.GetValues<Org>())
            data.Add(org, Verb.ReadNext, RegionHostKind.LockFiles);
        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void AStoreReloadTheFileNoLongerAllows_IsAPermanentError30_NotAnEscapingException(
        Org org, Verb verb, RegionHostKind regionHost)
    {
        using var forced = RegionHostSeam.Use(regionHost);
        string host = Tmp($"{org}-{verb}");
        try
        {
            Seed(host, org);
            var a = new FileRegistry();
            var b = new FileRegistry();
            Register(a, "A", host, org);
            Register(b, "B", host, org);
            a.OpenStatic("A", FileOpenMode.IO);
            b.OpenStatic("B", FileOpenMode.IO);
            Assert.Equal("00", a.Status("A"));
            Assert.Equal("00", b.Status("B"));

            // Another run unit moves the file's generation, then the file becomes one the entry cannot read.
            Assert.Equal("00", Write(b, "B", org, 3, "CCCC"));
            MakeTheEntryUnreadable(host);

            string status = Run(a, verb, org, out string image);
            Assert.Equal("30", status);
            Assert.Equal("30", a.Status("A"));
            Assert.Equal("", image.Trim());   // no record was delivered (the content is undefined, §14.9.30.4 GR18)

            // §9.1.13.1 — the condition remains in effect for every later statement, and the statement is not retried
            // into success by the stale store.
            Assert.Equal("30", Run(a, Verb.ReadNext, org, out image));
            Assert.Equal("", image.Trim());   // no record was delivered (the content is undefined, §14.9.30.4 GR18)

            // A CLOSE ends the condition (docs/CONFORMANCE.md DOC-A.1-105) and a connector that changed nothing writes
            // nothing back over the other run unit's file.
            a.Close("A");
            Assert.Equal("00", a.Status("A"));
            b.Close("B");
        }
        finally { try { File.Delete(host); } catch (IOException) { } }
    }

    /// <summary>The RETRY wait leaves the store mutex and re-enters it (<c>KeyedConnector.WaitOutsideStatement</c>), and
    /// the re-entry reloads like any statement's entry: a reload it cannot make is the same permanent error, not an
    /// exception out of the wait.</summary>
    [Theory]
    [InlineData(Org.Relative)]
    [InlineData(Org.Indexed)]
    public void ARetryWaitsReentryThatCannotReload_IsAPermanentError30(Org org)
    {
        using var forced = RegionHostSeam.Use(RegionHostKind.OfThisProcess);
        string host = Tmp($"retry-{org}");
        try
        {
            Seed(host, org);
            var a = new FileRegistry();
            var b = new FileRegistry();
            Register(a, "A", host, org);
            Register(b, "B", host, org);
            a.OpenStatic("A", FileOpenMode.IO);
            b.OpenStatic("B", FileOpenMode.IO);
            if (org == Org.Relative) a.SetRelativeKey("A", 1);
            Assert.Equal("00", a.ReadKeyedShared("A", -1, Rec(1, "    "), FileRecordLock.WithLock,
                ignoringLock: false, FileRetryKind.None, 0, out _));

            FileRegistry.PauseObserver.Value = _ =>
            {
                Assert.Equal("00", Write(a, "A", org, 3, "CCCC"));   // moves the generation
                MakeTheEntryUnreadable(host);
                a.Unlock("A", records: true);                          // the conflict is over
            };
            try
            {
                if (org == Org.Relative) b.SetRelativeKey("B", 1);
                Assert.Equal("30", b.ReadKeyedShared("B", -1, Rec(1, "    "), FileRecordLock.WithLock,
                    ignoringLock: false, FileRetryKind.Times, 3, out string image));
                Assert.Equal("", image.Trim());   // no record was delivered (the content is undefined, §14.9.30.4 GR18)
                Assert.Equal("30", b.Status("B"));
            }
            finally { FileRegistry.PauseObserver.Value = null; }
            b.Close("B");
            a.Close("A");
        }
        finally { try { File.Delete(host); } catch (IOException) { } }
    }

    private static void AssertFileHoldsThreeRecords(string host, Org org)
    {
        var check = new FileRegistry();
        Register(check, "C", host, org, sharing: null);
        check.OpenStatic("C", FileOpenMode.Input);
        Assert.Equal("00", check.Status("C"));
        for (int n = 1; n <= 3; n++)
        {
            if (org == Org.Relative) check.SetRelativeKey("C", n);
            Assert.Equal("00", check.ReadKeyedShared("C", -1, Rec(n, "    "), FileRecordLock.None, ignoringLock: false,
                FileRetryKind.None, 0, out string image));
            Assert.StartsWith(Rec(n, ""), image);
        }
        check.Close("C");
    }

    /// <summary>The sharing modes a refused page write or sync is answered under: <c>ALL OTHER</c>, <c>READ ONLY</c>
    /// (kb/Work PB1587: a READ ONLY writer is coherent, §9.1.15 2) admits another run unit's INPUT reader beside it, and
    /// its WRITE releases the record to the file at the statement, §14.9.51.4 GR12) and the clause-less
    /// <c>NO OTHER</c> default (null).</summary>
    private static readonly FileSharing?[] Sharings = [FileSharing.AllOther, FileSharing.ReadOnly, null];

    public static TheoryData<Org, Verb, FileSharing?> RefusedStatements()
    {
        var data = new TheoryData<Org, Verb, FileSharing?>();
        foreach (var org in Enum.GetValues<Org>())
            foreach (var verb in new[] { Verb.PlainWrite, Verb.Rewrite, Verb.Delete })
                foreach (var sharing in Sharings)
                    data.Add(org, verb, sharing);
        return data;
    }

    public static TheoryData<Org, FileSharing?> RefusedCloses()
    {
        var data = new TheoryData<Org, FileSharing?>();
        foreach (var org in Enum.GetValues<Org>())
            foreach (var sharing in Sharings)
                data.Add(org, sharing);
        return data;
    }

    /// <summary>⛔ THE ENGINE WRITES AT EVERY STATEMENT, SO A WRITE THE HOST REFUSES IS THE STATEMENT'S '30', WHATEVER ITS
    /// SHARING MODE (docs/rearchitecture/DESIGN-runtime-library.md sections 2.2.1.3 (decision 6) and 2.2.1.9; kb/Work PB3333): §9.1.13.6 1)'s
    /// permanent error at the WRITE, REWRITE or DELETE that met it, never deferred to a CLOSE, and in effect until the
    /// CLOSE (§9.1.13.1). The file is as the previous statement left it: the next OPEN reads the seeded records
    /// unchanged. A refused SYNC at the CLOSE's promotion is the CLOSE's '30', and the records the statements wrote
    /// survive it (page 2 already named them).</summary>
    [Theory]
    [MemberData(nameof(RefusedStatements))]
    public void APageWriteTheHostRefuses_Is30_AtTheStatement(Org org, Verb verb, FileSharing? sharing)
    {
        using var forced = RegionHostSeam.Use(RegionHostKind.OfThisProcess);
        string host = Tmp($"engine-write-{org}-{verb}-{sharing}");
        bool refusing = false;
        try
        {
            Seed(host, org);
            CobolNet.Runtime.IO.KeyedStore.MediumObserver.Value = inner => new CobolNet.Tests.Unit.Store.RefusingPageMedium(
                inner, write: _ => refusing ? new IOException("refused by the host") : null);
            var a = new FileRegistry();
            Register(a, "A", host, org, sharing);
            a.OpenStatic("A", FileOpenMode.IO);
            Assert.Equal("00", a.Status("A"));
            refusing = true;
            Assert.Equal("30", verb == Verb.PlainWrite ? Write(a, "A", org, 9, "ZZZZ") : Run(a, verb, org, out _));
            refusing = false;
            Assert.Equal("30", Run(a, Verb.ReadNext, org, out string image));   // §9.1.13.1: still in effect
            Assert.Equal("", image.Trim());
            a.Close("A");
            Assert.Equal("00", a.Status("A"));   // the CLOSE ends it (DOC-A.1-105)
        }
        finally
        {
            CobolNet.Runtime.IO.KeyedStore.MediumObserver.Value = null;
            try { File.Delete(host); } catch (IOException) { }
        }
    }

    [Theory]
    [MemberData(nameof(RefusedCloses))]
    public void ACloseWhosePromotionTheHostRefuses_Is30_AndKeepsTheRecords(Org org, FileSharing? sharing)
    {
        using var forced = RegionHostSeam.Use(RegionHostKind.OfThisProcess);
        string host = Tmp($"engine-close-{org}-{sharing}");
        bool refusing = false;
        try
        {
            Seed(host, org);
            CobolNet.Runtime.IO.KeyedStore.MediumObserver.Value = inner => new CobolNet.Tests.Unit.Store.RefusingPageMedium(
                inner, sync: () => refusing ? new IOException("refused by the host") : null);
            var a = new FileRegistry();
            Register(a, "A", host, org, sharing);
            a.OpenStatic("A", FileOpenMode.IO);
            Assert.Equal("00", Write(a, "A", org, 3, "CCCC"));
            refusing = true;
            a.Close("A");
            Assert.Equal("30", a.Status("A"));
        }
        finally { CobolNet.Runtime.IO.KeyedStore.MediumObserver.Value = null; }
        try { AssertFileHoldsThreeRecords(host, org); }
        finally { try { File.Delete(host); } catch (IOException) { } }
    }
}
