// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime.IO;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ TWO RUN UNITS SHARING A SEQUENTIAL FILE UNDER <c>SHARING WITH ALL OTHER</c> SEE EACH OTHER'S RECORD LOCKS, NAME
/// EACH RECORD ALIKE AND READ THE RECORDS THE FILE HOLDS NOW (kb/Work PB2692, the sequential arm of PB2660). ISO
/// §2023:9.1.16: <i>"While locked by a given file connector, a record is not accessible to another file connector in the
/// same or a different run unit, except by the execution of a READ statement with the IGNORING LOCK phrase"</i>;
/// §2023:9.1.13.8 GR1: <i>"I-O status = 51. The input-output statement is unsuccessful due to an attempt to access a record
/// that is currently locked by another file connector"</i>.
/// <para>Before PB2692 the sequential connector published no record lock (it had no <c>RecordLocks</c> endpoint), so a
/// second run unit's READ WITH LOCK of a record the first held answered '00'; its reader served a record another run
/// unit had rewritten from its read-ahead buffer; and a sharing writer numbered its released records from a mint only
/// its own run unit advanced, so after another run unit's append its lock guarded a different record. A second
/// <see cref="FileRegistry"/> IS a second run unit here (<see cref="CrossRunUnitKeyedStoreDriftTests"/>).</para>
/// </summary>
public sealed class CrossRunUnitSequentialLockDriftTests
{
    public enum Framing { Fixed, Line, Varying }

    private static void Register(FileRegistry reg, string name, string host, Framing framing, bool shared = true)
    {
        reg.Register(name, host, recordWidth: 4, lineSequential: framing == Framing.Line, optional: false,
            varyMin: framing == Framing.Varying ? 1 : -1, varyMax: framing == Framing.Varying ? 4 : -1);
        if (shared) reg.RegisterSharing(name, FileSharing.AllOther, FileLockMode.Manual, multiple: false);
    }

    private static string Read(FileRegistry reg, string name, FileRecordLock phrase, out string image,
        bool ignoringLock = false) =>
        reg.ReadShared(name, previous: false, phrase, advancingOnLock: false, ignoringLock, FileRetryKind.None, 0, out image);

    private static string Write(FileRegistry reg, string name, string image, FileRecordLock phrase = FileRecordLock.None) =>
        reg.WriteShared(name, image, -1, phrase, FileRetryKind.None, 0, page: null);

    private static void Seed(string host, Framing framing, params string[] records)
    {
        var seed = new FileRegistry();
        Register(seed, "S", host, framing, shared: false);
        seed.OpenStatic("S", FileOpenMode.Output);
        foreach (string r in records) Assert.Equal("00", Write(seed, "S", r));
        seed.Close("S");
    }

    private static List<string> Records(string host, Framing framing)
    {
        var reader = new FileRegistry();
        Register(reader, "R", host, framing, shared: false);
        reader.OpenStatic("R", FileOpenMode.Input);
        var all = new List<string>();
        while (Read(reader, "R", FileRecordLock.None, out string image) == "00") all.Add(image);
        reader.Close("R");
        return all;
    }

    /// <summary>A record one run unit holds is '51' to the other's READ WITH LOCK and REWRITE, IGNORING LOCK still
    /// reads it, and the holder's UNLOCK gives it to the other run unit — which then rewrites the record the FILE
    /// holds, and the first run unit's next READ sees that rewrite rather than its own read-ahead.</summary>
    [Theory]
    [InlineData(Framing.Fixed, RegionHostKind.OfThisProcess)]
    [InlineData(Framing.Line, RegionHostKind.OfThisProcess)]
    [InlineData(Framing.Varying, RegionHostKind.OfThisProcess)]
    [InlineData(Framing.Fixed, RegionHostKind.LockFiles)]
    [InlineData(Framing.Line, RegionHostKind.LockFiles)]
    [InlineData(Framing.Varying, RegionHostKind.LockFiles)]
    public void AnotherRunUnitsRecordLock_Answers51_AndItsRewriteIsReadFromTheFile(Framing framing, RegionHostKind regionHost)
    {
        using var forced = RegionHostSeam.Use(regionHost);
        string host = FileHarness.Tmp($"lock-{framing}");
        try
        {
            Seed(host, framing, "AAAA", "BBBB", "CCCC");
            var a = new FileRegistry();
            var b = new FileRegistry();
            Register(a, "A", host, framing);
            Register(b, "B", host, framing);
            a.OpenStatic("A", FileOpenMode.IO);
            b.OpenStatic("B", FileOpenMode.IO);
            Assert.Equal("00", a.Status("A"));
            Assert.Equal("00", b.Status("B"));

            Assert.Equal("00", Read(a, "A", FileRecordLock.WithLock, out string first));   // A's buffer now holds the file
            Assert.Equal("AAAA", first);
            // §2023:9.1.16 / §2023:9.1.13.8 GR1 — record 1 is not accessible to B, in another run unit.
            Assert.Equal("51", Read(b, "B", FileRecordLock.WithLock, out _));
            // §2023:14.9.30.4 GR12 — IGNORING LOCK makes it available "even if it is locked".
            Assert.Equal("00", Read(b, "B", FileRecordLock.None, out string ignored, ignoringLock: true));
            Assert.Equal("AAAA", ignored);
            // Record 2 nobody holds: B rewrites it, and A's next READ delivers the record the file holds now.
            Assert.Equal("00", Read(b, "B", FileRecordLock.WithLock, out _));
            Assert.Equal("00", b.RewriteShared("B", "YYYY", -1, FileRecordLock.None, FileRetryKind.None, 0));
            Assert.Equal("00", Read(a, "A", FileRecordLock.None, out string second));
            Assert.Equal("YYYY", second);

            a.Close("A");   // the CLOSE gives A's locks back to every run unit
            b.Close("B");
            var c = new FileRegistry();
            Register(c, "C", host, framing);
            c.OpenStatic("C", FileOpenMode.IO);
            Assert.Equal("00", Read(c, "C", FileRecordLock.WithLock, out _));
            c.Close("C");
            Assert.Equal(["AAAA", "YYYY", "CCCC"], Records(host, framing));
        }
        finally { FileHarness.TryDelete(host); }
    }

    /// <summary>A sharing writer's WRITE WITH LOCK guards the record at its PHYSICAL place: after another run unit
    /// has appended, the record is the file's fourth, so another run unit's READ WITH LOCK of record 4 answers '51'
    /// and of record 3 (the other appender's) '00'. A mint advanced only by the writer's own run unit called it 3.</summary>
    [Theory]
    [InlineData(Framing.Fixed, RegionHostKind.OfThisProcess)]
    [InlineData(Framing.Line, RegionHostKind.OfThisProcess)]
    [InlineData(Framing.Varying, RegionHostKind.OfThisProcess)]
    [InlineData(Framing.Fixed, RegionHostKind.LockFiles)]
    [InlineData(Framing.Line, RegionHostKind.LockFiles)]
    [InlineData(Framing.Varying, RegionHostKind.LockFiles)]
    public void ASharingWritersLock_GuardsTheRecordAtItsPhysicalPlace(Framing framing, RegionHostKind regionHost)
    {
        using var forced = RegionHostSeam.Use(regionHost);
        string host = FileHarness.Tmp($"write-{framing}");
        try
        {
            Seed(host, framing, "AAAA", "BBBB");
            var a = new FileRegistry();
            var b = new FileRegistry();
            Register(a, "A", host, framing);
            Register(b, "B", host, framing);
            a.OpenStatic("A", FileOpenMode.Extend);
            b.OpenStatic("B", FileOpenMode.Extend);
            Assert.Equal("00", a.Status("A"));
            Assert.Equal("00", b.Status("B"));
            Assert.Equal("00", Write(a, "A", "CCCC"));                            // record 3, A's run unit
            Assert.Equal("00", Write(b, "B", "DDDD", FileRecordLock.WithLock));   // record 4, locked by B

            var c = new FileRegistry();
            Register(c, "C", host, framing);
            c.OpenStatic("C", FileOpenMode.IO);
            for (int n = 1; n <= 3; n++) Assert.Equal("00", Read(c, "C", FileRecordLock.WithLock, out _));
            Assert.Equal("51", Read(c, "C", FileRecordLock.WithLock, out _));

            b.Close("B");
            Assert.Equal("00", Read(c, "C", FileRecordLock.WithLock, out string fourth));
            Assert.Equal("DDDD", fourth);
            a.Close("A");
            c.Close("C");
            Assert.Equal(["AAAA", "BBBB", "CCCC", "DDDD"], Records(host, framing));
        }
        finally { FileHarness.TryDelete(host); }
    }

    /// <summary>⛔ A READ'S LOCK GRANT IS ITS CONFLICT TEST, TAKEN BEFORE THE POSITION MOVES (kb/Work PB5770, review R1-1). A
    /// sequential connector has no store statement, so another run unit can take the record between A's conflict test and
    /// A's lock; the forced interleaving (<see cref="FileRegistry.ReadLockWindow"/>, at each attempt of A's grant) has B lock
    /// record 1 in that instant. §2023:14.9.30.4 GR11c: under automatic locking the lock of a successfully accessed record "is
    /// set", and §2023:9.1.16 makes the record inaccessible to another run unit's connector, so A cannot both succeed and not
    /// hold it: A's READ is '51' (§2023:9.1.13.8 GR1) and makes no record available. It is the record operation conflict
    /// condition, so §2023:14.9.30.4 GR10a holds: "The file position indicator is unchanged" — once B gives the record back,
    /// A's next READ makes record 1 available, and A then holds its lock.</summary>
    [Theory]
    [InlineData(Framing.Fixed, RegionHostKind.OfThisProcess)]
    [InlineData(Framing.Line, RegionHostKind.OfThisProcess)]
    [InlineData(Framing.Fixed, RegionHostKind.LockFiles)]
    public void ALockGrantAnotherRunUnitTookFirst_IsTheReadsStatus(Framing framing, RegionHostKind regionHost)
    {
        using var forced = RegionHostSeam.Use(regionHost);
        string host = FileHarness.Tmp($"window-{framing}");
        try
        {
            Seed(host, framing, "AAAA", "BBBB");
            var a = AutomaticReader(host, framing);
            var b = new FileRegistry();
            Register(b, "B", host, framing);
            b.OpenStatic("B", FileOpenMode.IO);

            string inWindow = "";
            a.ReadLockWindow = _ =>
            {
                a.ReadLockWindow = null;   // once
                inWindow = Read(b, "B", FileRecordLock.WithLock, out _);   // B takes record 1 after A's test, before A's lock
            };
            Assert.Equal("51", Read(a, "A", FileRecordLock.None, out string image));
            Assert.Equal("00", inWindow);
            Assert.Equal("", image);
            Assert.Equal("51", a.Status("A"));

            // B still holds record 1, and A took nothing: another run unit's READ WITH LOCK of it is refused while B lives.
            var c = new FileRegistry();
            Register(c, "C", host, framing);
            c.OpenStatic("C", FileOpenMode.IO);
            Assert.Equal("51", Read(c, "C", FileRecordLock.WithLock, out _));
            c.Close("C");

            // GR10a: A's position did not move, so once B gives record 1 back A's next READ makes record 1 available — and A
            // holds its lock (a fourth run unit is refused it).
            b.Close("B");
            Assert.Equal("00", Read(a, "A", FileRecordLock.None, out string first));
            Assert.Equal("AAAA", first);
            var d = new FileRegistry();
            Register(d, "D", host, framing);
            d.OpenStatic("D", FileOpenMode.IO);
            Assert.Equal("51", Read(d, "D", FileRecordLock.WithLock, out _));
            d.Close("D");
            a.Close("A");
        }
        finally { FileHarness.TryDelete(host); }
    }

    /// <summary>⛔ A REFUSED READ-LOCK GRANT HONORS THE READ'S RETRY PHRASE (kb/Work PB5770, review R1-1). §2023:14.9.30.4 GR9: for
    /// a record locked by another file connector, "If the RETRY phrase is specified, additional attempts may be made to read the
    /// record as specified in the rules in 14.7.9". B takes record 1 at A's first grant attempt and gives it back before the
    /// second; A's READ RETRY 2 TIMES waits one interval against that run unit and then makes record 1 available, holding its
    /// lock. Without the phrase the same interleaving is '51' (the case above): the outcome is the phrase's, not the
    /// timing's.</summary>
    [Theory]
    [InlineData(Framing.Fixed, RegionHostKind.OfThisProcess)]
    [InlineData(Framing.Fixed, RegionHostKind.LockFiles)]
    public void ARefusedLockGrant_IsRetriedUnderTheRetryPhrase(Framing framing, RegionHostKind regionHost)
    {
        using var forced = RegionHostSeam.Use(regionHost);
        string host = FileHarness.Tmp($"window-retry-{framing}");
        int pauses = 0;
        FileRegistry.PauseObserver.Value = _ => pauses++;
        try
        {
            Seed(host, framing, "AAAA", "BBBB");
            var a = AutomaticReader(host, framing);
            var b = new FileRegistry();
            Register(b, "B", host, framing);
            b.OpenStatic("B", FileOpenMode.IO);

            var inWindow = new List<string>();
            a.ReadLockWindow = _ =>
            {
                if (inWindow.Count == 0) inWindow.Add(Read(b, "B", FileRecordLock.WithLock, out _));   // attempt 1: B takes it
                else if (inWindow.Count == 1) { b.Close("B"); inWindow.Add("released"); }             // attempt 2: B gives it back
            };
            Assert.Equal("00", a.ReadShared("A", previous: false, FileRecordLock.None, advancingOnLock: false,
                ignoringLock: false, FileRetryKind.Times, 2, out string image));
            Assert.Equal(["00", "released"], inWindow);
            Assert.Equal("AAAA", image);
            Assert.Equal(1, pauses);   // one interval against the other run unit's holder (§2023:14.7.9.3 GR1)
            a.ReadLockWindow = null;

            var c = new FileRegistry();
            Register(c, "C", host, framing);
            c.OpenStatic("C", FileOpenMode.IO);
            Assert.Equal("51", Read(c, "C", FileRecordLock.WithLock, out _));   // A holds record 1
            c.Close("C");
            a.Close("A");
        }
        finally
        {
            FileRegistry.PauseObserver.Value = null;
            FileHarness.TryDelete(host);
        }
    }

    /// <summary>Run unit A: the shared file open I-O under SHARING WITH ALL OTHER and LOCK MODE AUTOMATIC, so every READ sets
    /// its record's lock (§2023:12.4.5.9.4 GR4).</summary>
    private static FileRegistry AutomaticReader(string host, Framing framing)
    {
        var a = new FileRegistry();
        Register(a, "A", host, framing, shared: false);
        a.RegisterSharing("A", FileSharing.AllOther, FileLockMode.Automatic, multiple: false);
        a.OpenStatic("A", FileOpenMode.IO);
        Assert.Equal("00", a.Status("A"));
        return a;
    }
}
