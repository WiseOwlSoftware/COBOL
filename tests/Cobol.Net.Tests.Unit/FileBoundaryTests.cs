// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolNet.Runtime;
using CobolNet.Runtime.IO;
using CobolNet.Runtime.IO.Store;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The runtime's two MEDIUM boundaries, pinned where a COBOL golden cannot reach them (kb/Work PB1192, PB690).
/// <list type="bullet">
/// <item>The externally-defined boundaries of ISO §9.1.13.5 item 4 ('24', a relative or indexed file — Annex A.1
/// item 107: the host's maximum file size and the keyed store engine's 2^32 pages, met at the statement whose pages the
/// host refuses to grow the file for) and §9.1.13.6 item 3 ('34', a sequential file — Annex A.1 item 108, the host
/// medium's capacity, <see cref="HostFile.IsMediumBoundary"/>).</item>
/// <item>The file coded character set of Annex A.1 item 31 (owner decision kb/Work R47): ISO/IEC 8859-1, with a
/// strict encoding so an unrepresentable character can never become a silent <c>?</c>.</item>
/// </list>
/// </summary>
public sealed class FileBoundaryTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "CobolNet_FBT_" + Guid.NewGuid().ToString("N")[..8]);

    public FileBoundaryTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    private string Host(string name) => Path.Combine(_dir, name);

    /// <summary>Whether a write of <paramref name="page"/> grows the file: the page at or past the medium's end in bytes —
    /// the writes a full medium and a file at its maximum size refuse.</summary>
    private static bool Grows(IPageMedium medium, uint page) => (long)page * PageHeader.PageSize >= medium.Length;

    private static readonly char Euro = (char)0x20AC;
    private static readonly char EAcute = (char)0x00E9;

    // ── §9.1.13.6 item 3 / item 1: the host's write refusal ────────────────────────────────────────────────

    /// <summary>The host's medium-exhaustion refusals are the sequential boundary ('34'); every other write
    /// failure is §9.1.13.6 item 1's '30'. The Windows HRESULTs are negative and can never be a Unix errno, so
    /// they classify on every host; the raw errnos classify only where the host reports errnos.</summary>
    [Fact]
    public void WriteFailure_MediumExhaustionIs34_AnyOtherFailureIs30()
    {
        foreach (int hr in new[] { unchecked((int)0x80070027), unchecked((int)0x80070070), unchecked((int)0x800700DF), unchecked((int)0x8007050F) })
            Assert.Equal(FileStatusCode.PermanentBoundary, FileStatusCode.ForWriteFailure(new IOException("full", hr)));
        string unixExpected = OperatingSystem.IsWindows() ? FileStatusCode.PermanentError : FileStatusCode.PermanentBoundary;
        int quota = OperatingSystem.IsLinux() ? 122 : 69;
        foreach (int errno in new[] { 27, 28, quota })
            Assert.Equal(unixExpected, FileStatusCode.ForWriteFailure(new IOException("errno", errno)));
        Assert.Equal(FileStatusCode.PermanentError, FileStatusCode.ForWriteFailure(new IOException("io", unchecked((int)0x80131620))));
        Assert.Equal(FileStatusCode.PermanentError, FileStatusCode.ForWriteFailure(new IOException("sharing", unchecked((int)0x80070020))));
    }

    /// <summary>§14.9.51.4 GR20 end to end, on the one host medium that is full by construction: Linux's
    /// <c>/dev/full</c> refuses every write with ENOSPC. The writer buffers, so the refusal surfaces on the WRITE
    /// whose record fills the buffer — that WRITE is '34', never an escaping IOException. A host without the device
    /// must not be Linux (so this cannot pass on the CI host by not running).</summary>
    [Fact]
    public void SequentialWrite_OnAFullMedium_Is34_AtTheWrite()
    {
        const string full = "/dev/full";
        if (!File.Exists(full))
        {
            Assert.False(OperatingSystem.IsLinux(), "a Linux host provides /dev/full; the boundary was not exercised");
            return;
        }
        var c = new SequentialConnector(full, recordWidth: 512, lineSequential: false);
        Assert.Equal(FileStatusCode.Success, c.Open(FileOpenMode.Output));
        string record = new('X', 512);
        string status = FileStatusCode.Success;
        int writes = 0;
        while (status == FileStatusCode.Success && writes < 100_000)
        {
            status = c.Write(record, -1, page: null);
            writes++;
        }
        Assert.Equal(FileStatusCode.PermanentBoundary, status);
        c.Close();   // its own final flush is refused too: §9.1.13.6 item 1's '30' through Close's catch
    }

    /// <summary>§9.1.13.6 item 1 on the READ side (kb/Work PB1512): a host failure while a sequential READ or
    /// START FIRST/LAST reads the medium is the permanent error '30' — never an escaping IOException — and the
    /// unsuccessful READ leaves no valid file position, so the next sequential READ is '46' (§14.9.30.4 GR18,
    /// §9.1.13.7 item 6). The failure is produced by the host itself, measured per host: Linux refuses a read of
    /// <c>/proc/self/mem</c> at address 0 with EIO, and Windows refuses a read of a byte range another handle has
    /// LOCKED. A host offering neither must be neither of them, so the test cannot pass on a CI host by not
    /// running.</summary>
    [Fact]
    public void SequentialRead_HostFailure_Is30_AndRemainsInEffect()
    {
        FileStream? locker = null;
        string path;
        if (OperatingSystem.IsLinux() && File.Exists("/proc/self/mem")) path = "/proc/self/mem";
        else if (OperatingSystem.IsWindows())
        {
            path = Host("locked.dat");
            File.WriteAllBytes(path, Encoding.Latin1.GetBytes(new string('A', 64)));
        }
        else
        {
            Assert.False(OperatingSystem.IsLinux() || OperatingSystem.IsWindows(), "the host failure was not exercised");
            return;
        }
        var c = new SequentialConnector(path, recordWidth: 16, lineSequential: false);
        Assert.Equal(FileStatusCode.Success, c.Open(FileOpenMode.Input));
        try
        {
            if (OperatingSystem.IsWindows())
            {
                locker = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                locker.Lock(0, 64);
            }
            Assert.False(c.Read(previous: false, out _));
            Assert.Equal(FileStatusCode.PermanentError, c.Status);
            // §9.1.13.1 (kb/Work PB1541): the '30' is a permanent error and REMAINS IN EFFECT for every later
            // statement, so the next READ answers it again — not GR21's '46', which would say the previous READ
            // merely left no valid position. (The failed READ still invalidated the position, §14.9.30.4 GR18; it is
            // moot until a CLOSE ends the condition, DOC-A.1-105, and the next OPEN repositions.)
            Assert.False(c.Read(previous: false, out _));
            Assert.Equal(FileStatusCode.PermanentError, c.Status);
            Assert.Equal(FileStatusCode.PermanentError, c.StartFirstLast(last: false));
        }
        finally
        {
            locker?.Dispose();   // releases the region lock with the handle
            c.Close();
        }
    }

    // ── §9.1.13.5 item 4: the store's capacity ───────────────────────────────────────────────────────────

    /// <summary>⛔ AN INDEXED RECORD IS n + 1 ENTRIES, ITS ORDINALS IN ITS FRAME AND IN ITS COMPOSITE KEYS
    /// (docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.4; kb/Work PB3334, PB2693). Read back from the file's own pages
    /// (the committed meta through a fresh adoption, the trees by a walk): the prime tree holds each record's frame under
    /// its prime key's channel bytes, carrying one release ordinal per key the meta page records — a WRITE stamps one
    /// fresh ordinal into every slot (§14.9.51.4 GR40), a REWRITE re-stamps only the key it changed (§14.9.35.4 GR24) —
    /// and the alternate tree holds (the key's bytes, the record's ordinal under it) → its prime key, in that order, so a
    /// tree's order IS its duplicate order. A record whose alternate key equals its SUPPRESS WHEN value has no alternate
    /// entry (§12.4.5.6.4 GR6).</summary>
    [Fact]
    public void IndexedRecord_IsNPlusOneEntries_TheOrdinalsInItsFrameAndInItsCompositeKeys()
    {
        string host = Host("ix-entries.dat");
        var c = new IndexedConnector(host, 8, KeyedAccess.Dynamic, [new(0, 4)]);
        c.AddAlternateKey([new(4, 4)], duplicates: true, suppress: "ZZZZ");
        Assert.Equal(FileStatusCode.Success, c.Open(FileOpenMode.Output));
        Assert.Equal(FileStatusCode.Success, c.Write("K002DDDD"));
        Assert.Equal(FileStatusCode.DuplicateAlternateKey, c.Write("K001DDDD"));   // '02': a permitted duplicate
        Assert.Equal(FileStatusCode.Success, c.Write("K003ZZZZ"));                 // suppressed: no alternate entry
        Assert.Equal(FileStatusCode.Success, c.Close());
        Assert.Equal(FileStatusCode.Success, c.Open(FileOpenMode.IO));
        Assert.Equal(FileStatusCode.Success, c.Rewrite("K002EEEE"));               // GR24 b): last among its new duplicates
        Assert.Equal(FileStatusCode.Success, c.Close());

        using var handle = File.OpenHandle(host, FileMode.Open, FileAccess.Read, FileShare.Read);
        var medium = new CobolNet.Runtime.IO.Store.HandlePageMedium(handle);
        Assert.Equal(CobolNet.Runtime.IO.Store.StoreStatus.Ok,
            CobolNet.Runtime.IO.Store.PagedFile.Open(medium, writable: false, coherent: false, out var file));
        var meta = file!.Meta;
        Assert.Equal(2, meta.Attributes.Keys.Count);
        Assert.Single(meta.AlternateRoots);
        var prime = Entries(new CobolNet.Runtime.IO.Store.BTree(file, CobolNet.Runtime.IO.Store.KeyOrders.Ordinal, meta.PrimeRoot))
            .Select(e => (Key: Encoding.Latin1.GetString(e.Key), Frame: RecordFraming.DecodeFrame(e.Value, 2, null)!.Value))
            .ToList();
        Assert.Equal(["K001", "K002", "K003"], prime.Select(p => p.Key));
        Assert.Equal(["K001DDDD", "K002EEEE", "K003ZZZZ"], prime.Select(p => p.Frame.Image));
        // The release mint is the file's: 0, 1, 2 for the three WRITEs, 3 for the REWRITE's re-stamp of key 1 alone.
        Assert.Equal([1L, 1L], prime[0].Frame.Ordinals!);
        Assert.Equal([0L, 3L], prime[1].Frame.Ordinals!);
        Assert.Equal([2L, 2L], prime[2].Frame.Ordinals!);
        var alternate = Entries(new CobolNet.Runtime.IO.Store.BTree(file,
                new CobolNet.Runtime.IO.Store.CompositeKeyOrder(CobolNet.Runtime.IO.Store.KeyOrders.Ordinal), meta.AlternateRoots[0]))
            .Select(e => (IndexedStore.Split(e.Key), Encoding.Latin1.GetString(e.Value)))
            .Select(e => $"{Encoding.Latin1.GetString(e.Item1.KeyPart)}#{e.Item1.Ordinal}->{e.Item2}")
            .ToList();
        Assert.Equal(["DDDD#1->K001", "EEEE#3->K002"], alternate);

        static List<(byte[] Key, byte[] Value)> Entries(CobolNet.Runtime.IO.Store.BTree tree)
        {
            var all = new List<(byte[], byte[])>();
            Assert.Equal(CobolNet.Runtime.IO.Store.StoreStatus.Ok, tree.First(out var cursor));
            while (cursor.HasCurrent)
            {
                Assert.Equal(CobolNet.Runtime.IO.Store.StoreStatus.Ok, cursor.ReadValue(out var value));
                all.Add((cursor.Key, value));
                Assert.Equal(CobolNet.Runtime.IO.Store.StoreStatus.Ok, cursor.Next());
            }
            return all;
        }
    }

    /// <summary>The host's two refusals at an INDEXED statement (2.2.1.9; the record's n + 1 entries are one statement): a
    /// page write the host refuses because the file cannot GROW past its maximum size is '24' — §14.9.51.4 GR42 d),
    /// <i>"When the record that is to be released to the operating environment would reside outside the externally
    /// defined boundaries of the physical file"</i> — and a write refused because the medium is FULL is '30' (§9.1.13.6
    /// item 1), in effect until the CLOSE (§9.1.13.1). Neither releases the record under any key: the next OPEN reads the
    /// earlier records by the prime key and by the alternate key alike.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IndexedWrite_TheHostsRefusalIsAtTheStatement_24ForTooLarge_30ForFull(bool tooLarge)
    {
        string host = Host($"ix-refuse-{tooLarge}.dat");
        int hresult = tooLarge
            ? OperatingSystem.IsWindows() ? unchecked((int)0x800700DF) : 27     // ERROR_FILE_TOO_LARGE / EFBIG
            : OperatingSystem.IsWindows() ? unchecked((int)0x80070070) : 28;    // ERROR_DISK_FULL / ENOSPC
        bool refusing = false;
        KeyedStore.MediumObserver.Value = inner => new CobolNet.Tests.Unit.Store.RefusingPageMedium(inner,
            write: page => refusing && Grows(inner, page) ? HandlePageMedium.Refusal(new IOException("refused by the host", hresult)) : null);
        try
        {
            var c = Indexed(host);
            Assert.Equal(FileStatusCode.Success, c.Open(FileOpenMode.Output));
            Assert.Equal(FileStatusCode.Success, c.Write("K001AAAA"));
            refusing = true;
            Assert.Equal(tooLarge ? FileStatusCode.BoundaryViolation : FileStatusCode.PermanentError, c.Write("K002BBBB"));
            refusing = false;
            // '24' is the invalid key condition and the connector goes on; '30' is a permanent error in effect.
            Assert.Equal(tooLarge ? FileStatusCode.Success : FileStatusCode.PermanentError, c.Write("K003CCCC"));
            Assert.Equal(FileStatusCode.Success, c.Close());
        }
        finally { KeyedStore.MediumObserver.Value = null; }
        var r = Indexed(host);
        Assert.Equal(FileStatusCode.Success, r.Open(FileOpenMode.Input));
        Assert.Equal(FileStatusCode.RecordNotFound, r.ReadRandom(0, "    BBBB", out _));   // not released under key 1
        Assert.Equal(FileStatusCode.RecordNotFound, r.ReadRandom(-1, "K002    ", out _));   // nor under the prime key
        Assert.Equal(tooLarge ? FileStatusCode.Success : FileStatusCode.RecordNotFound, r.ReadRandom(0, "    CCCC", out _));
        Assert.Equal(FileStatusCode.Success, r.ReadRandom(-1, "K001    ", out string first));
        Assert.Equal("K001AAAA", first);
        Assert.Equal(FileStatusCode.Success, r.Close());

        static IndexedConnector Indexed(string host)
        {
            var c = new IndexedConnector(host, 8, KeyedAccess.Dynamic, [new(0, 4)]);
            c.AddAlternateKey([new(4, 4)], duplicates: false);
            return c;
        }
    }

    /// <summary>⛔ THE BOUNDARY OF A RELATIVE FILE IS THE HOST'S (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.9,
    /// docs/CONFORMANCE.md DOC-A.1-107; kb/Work PB3333). The record number's own range is §14.9.51.4 GR29 b)'s: a key
    /// below 1 or above 2 147 483 647 is '34', and a key anywhere inside it is one leaf entry of the tree — the record at
    /// the highest permitted number and one in the billions answer '00' and cost a page each, where the whole store
    /// answered '24' for every key past 536 870 887 (its image's capacity). A WRITE whose pages the host refuses to GROW
    /// the file for (<c>ERROR_FILE_TOO_LARGE</c>, <c>EFBIG</c>) is outside the externally defined boundaries — GR33 b)'s
    /// '24', the invalid key condition — and a medium with no ROOM answers §9.1.13.6 item 1's '30' at that statement;
    /// both leave the file as the previous statement left it, so the next OPEN reads exactly the records written
    /// before.</summary>
    [Fact]
    public void RelativeWrite_BoundaryIsTheHosts_AndTheKeyRangeIs34()
    {
        string host = Host("rel.dat");
        var c = new RelativeConnector(host, 4, KeyedAccess.Random, 18);
        Assert.Equal(FileStatusCode.Success, c.Open(FileOpenMode.Output));
        foreach (long key in new long[] { 536_870_888, RelativeConnector.HighestRelativeRecordNumber, 5 })
        {
            c.SetPendingKey(key);
            Assert.Equal(FileStatusCode.Success, c.Write("K" + (key % 1000).ToString("000", System.Globalization.CultureInfo.InvariantCulture)));
        }
        c.SetPendingKey(5);
        Assert.Equal(FileStatusCode.DuplicateKey, c.Write("ABCD"));                 // '22' GR33 a), the file unchanged
        // The permanent errors LAST: under Annex A.1 item 105 a '3x' stays in effect until the CLOSE, which runs
        // normally and ends it.
        c.SetPendingKey(RelativeConnector.HighestRelativeRecordNumber + 1);
        Assert.Equal(FileStatusCode.PermanentBoundary, c.Write("ABCD"));
        c.SetPendingKey(0);
        Assert.Equal(FileStatusCode.PermanentBoundary, c.Write("ABCD"));
        Assert.Equal(FileStatusCode.Success, c.Close());
        // Three meta pages and a handful of tree and bookkeeping pages: the key's size costs nothing.
        Assert.True(new FileInfo(host).Length <= 16 * CobolNet.Runtime.IO.Store.PageHeader.PageSize, $"{new FileInfo(host).Length} bytes");
        Assert.Equal(["5=K005", "536870888=K888", "2147483647=K647"], Records(host));
    }

    /// <summary>The host's two refusals, each at the statement that meets it (2.2.1.9): a page write the host refuses
    /// because the file cannot GROW past its maximum size is '24' — <i>"An attempt is made to write outside the
    /// externally-defined boundaries of a physical relative or indexed file"</i>, §9.1.13.5 item 4 — and a write the host refuses
    /// because the medium is FULL is '30' (§9.1.13.6 item 1), which stays in effect until the CLOSE (§9.1.13.1). Neither
    /// releases the record, and the next OPEN reads the records the earlier statements wrote, byte for byte.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RelativeWrite_TheHostsRefusalIsAtTheStatement_24ForTooLarge_30ForFull(bool tooLarge)
    {
        string host = Host($"refuse-{tooLarge}.dat");
        int hresult = tooLarge
            ? OperatingSystem.IsWindows() ? unchecked((int)0x800700DF) : 27     // ERROR_FILE_TOO_LARGE / EFBIG
            : OperatingSystem.IsWindows() ? unchecked((int)0x80070070) : 28;    // ERROR_DISK_FULL / ENOSPC
        bool refusing = false;
        // The host refuses to GROW the file — a write at or past its end — as a full medium and a file at its maximum
        // size both do; the pages already in the file (page 2 among them) are written as ever.
        KeyedStore.MediumObserver.Value = inner => new CobolNet.Tests.Unit.Store.RefusingPageMedium(inner,
            write: page => refusing && Grows(inner, page) ? HandlePageMedium.Refusal(new IOException("refused by the host", hresult)) : null);
        try
        {
            var c = new RelativeConnector(host, 4, KeyedAccess.Random, 4);
            Assert.Equal(FileStatusCode.Success, c.Open(FileOpenMode.Output));
            c.SetPendingKey(1);
            Assert.Equal(FileStatusCode.Success, c.Write("AAAA"));
            refusing = true;
            c.SetPendingKey(2);
            Assert.Equal(tooLarge ? FileStatusCode.BoundaryViolation : FileStatusCode.PermanentError, c.Write("BBBB"));
            refusing = false;
            c.SetPendingKey(3);
            // '24' is the invalid key condition and the connector goes on; '30' is a permanent error in effect.
            Assert.Equal(tooLarge ? FileStatusCode.Success : FileStatusCode.PermanentError, c.Write("CCCC"));
            Assert.Equal(FileStatusCode.Success, c.Close());
        }
        finally { KeyedStore.MediumObserver.Value = null; }
        Assert.Equal(tooLarge ? ["1=AAAA", "3=CCCC"] : ["1=AAAA"], Records(host));
    }

    /// <summary>⛔ THE BOUNDARY'S '24' IS A RECORD WRITE'S ALONE (review finding R1-1 of kb/Work PB3333). §9.1.13.5 item 4's
    /// invalid key condition is <i>"An attempt is made to write outside the externally-defined boundaries of a physical
    /// relative or indexed file"</i>, and §14.9.10.4 gives DELETE no boundary condition, so a DELETE whose copy-on-write
    /// pages the host refuses to grow the file for (<c>ERROR_FILE_TOO_LARGE</c>, <c>EFBIG</c>) is §9.1.13.6 item 1's '30'
    /// (<i>"A permanent error exists and no further information is available concerning the input-output operation"</i>),
    /// never '24' with the INVALID KEY branch taken; so is an OPEN OUTPUT whose creation the host refuses to grow the file
    /// for. Both leave the file as it was: the DELETE's record is still there at the next OPEN.</summary>
    [Fact]
    public void RelativeDeleteAndCreate_AtTheHostsBoundary_Are30_NeverTheInvalidKey()
    {
        string host = Host("refuse-delete.dat");
        int tooLarge = OperatingSystem.IsWindows() ? unchecked((int)0x800700DF) : 27;   // ERROR_FILE_TOO_LARGE / EFBIG
        bool refusing = false;
        int refused = 0;
        RelativeStore.MediumObserver.Value = inner => new CobolNet.Tests.Unit.Store.RefusingPageMedium(inner,
            write: page =>
            {
                if (!refusing || !Grows(inner, page)) return null;
                refused++;
                return HandlePageMedium.Refusal(new IOException("refused by the host", tooLarge));
            });
        try
        {
            var c = new RelativeConnector(host, 4, KeyedAccess.Random, 4);
            Assert.Equal(FileStatusCode.Success, c.Open(FileOpenMode.Output));
            c.SetPendingKey(1);
            Assert.Equal(FileStatusCode.Success, c.Write("AAAA"));
            Assert.Equal(FileStatusCode.Success, c.Close());
            Assert.Equal(FileStatusCode.Success, c.Open(FileOpenMode.IO));   // §14.9.10.4 GR1: DELETE needs the I-O mode
            refusing = true;
            c.SetPendingKey(1);
            Assert.Equal(FileStatusCode.PermanentError, c.Delete());
            Assert.True(refused > 0, "the DELETE grew the file, and the host refused it");
            refusing = false;
            Assert.Equal(FileStatusCode.PermanentError, c.Delete());   // §9.1.13.1: the permanent error stays in effect
            Assert.Equal(FileStatusCode.Success, c.Close());

            string created = Host("refuse-create.dat");
            var o = new RelativeConnector(created, 4, KeyedAccess.Random, 4);
            refusing = true;
            refused = 0;
            Assert.Equal(FileStatusCode.PermanentError, o.Open(FileOpenMode.Output));
            Assert.True(refused > 0, "the creation grew the file, and the host refused it");
            refusing = false;
        }
        finally { RelativeStore.MediumObserver.Value = null; }
        Assert.Equal(["1=AAAA"], Records(host));
    }

    /// <summary>⛔ THE INDEXED SIBLING OF THE RELATIVE CASE ABOVE (review finding R1-1 of kb/Work PB3334): the boundary's
    /// '24' is a record WRITE's or REWRITE's alone in an indexed file too (<c>KeyedConnector.RecordWriteFailed</c>, the one
    /// mapping both organizations share). A DELETE whose copy-on-write pages — the prime entry's leaf and the alternate
    /// entry's — the host refuses to grow the file for (<c>ERROR_FILE_TOO_LARGE</c>, <c>EFBIG</c>) is §9.1.13.6 item 1's
    /// '30', never §9.1.13.5 item 4's '24' with the INVALID KEY branch taken (§14.9.10.4 gives DELETE the invalid key
    /// condition only for a record the file does not contain), and the permanent error stays in effect (§9.1.13.1); an
    /// OPEN OUTPUT whose creation the host refuses to grow the file for is '30' as well. The file is unchanged by both:
    /// the record the DELETE named is still there under the prime key AND the alternate key at the next OPEN.</summary>
    [Fact]
    public void IndexedDeleteAndCreate_AtTheHostsBoundary_Are30_NeverTheInvalidKey()
    {
        string host = Host("ix-refuse-delete.dat");
        int tooLarge = OperatingSystem.IsWindows() ? unchecked((int)0x800700DF) : 27;   // ERROR_FILE_TOO_LARGE / EFBIG
        bool refusing = false;
        int refused = 0;
        KeyedStore.MediumObserver.Value = inner => new CobolNet.Tests.Unit.Store.RefusingPageMedium(inner,
            write: page =>
            {
                if (!refusing || !Grows(inner, page)) return null;
                refused++;
                return HandlePageMedium.Refusal(new IOException("refused by the host", tooLarge));
            });
        try
        {
            var c = Indexed(host);
            Assert.Equal(FileStatusCode.Success, c.Open(FileOpenMode.Output));
            Assert.Equal(FileStatusCode.Success, c.Write("K001AAAA"));
            Assert.Equal(FileStatusCode.Success, c.Write("K002BBBB"));
            Assert.Equal(FileStatusCode.Success, c.Close());
            Assert.Equal(FileStatusCode.Success, c.Open(FileOpenMode.IO));   // §14.9.10.4 GR1: DELETE needs the I-O mode
            refusing = true;
            Assert.Equal(FileStatusCode.PermanentError, c.Delete("K001    "));
            Assert.True(refused > 0, "the DELETE grew the file, and the host refused it");
            refusing = false;
            Assert.Equal(FileStatusCode.PermanentError, c.Delete("K002    "));   // §9.1.13.1: the permanent error stays in effect
            Assert.Equal(FileStatusCode.Success, c.Close());

            var o = Indexed(Host("ix-refuse-create.dat"));
            refusing = true;
            refused = 0;
            Assert.Equal(FileStatusCode.PermanentError, o.Open(FileOpenMode.Output));
            Assert.True(refused > 0, "the creation grew the file, and the host refused it");
            refusing = false;
        }
        finally { KeyedStore.MediumObserver.Value = null; }
        var r = Indexed(host);
        Assert.Equal(FileStatusCode.Success, r.Open(FileOpenMode.Input));
        Assert.Equal(FileStatusCode.Success, r.ReadRandom(-1, "K001    ", out string byPrime));
        Assert.Equal("K001AAAA", byPrime);
        Assert.Equal(FileStatusCode.Success, r.ReadRandom(0, "    AAAA", out string byAlternate));
        Assert.Equal("K001AAAA", byAlternate);
        Assert.Equal(FileStatusCode.Success, r.ReadRandom(-1, "K002    ", out _));
        Assert.Equal(FileStatusCode.Success, r.Close());

        static IndexedConnector Indexed(string host)
        {
            var c = new IndexedConnector(host, 8, KeyedAccess.Dynamic, [new(0, 4)]);
            c.AddAlternateKey([new(4, 4)], duplicates: false);
            return c;
        }
    }

    /// <summary>Every record of the relative file at <paramref name="host"/> as "rrn=image", read by a fresh connector
    /// (the next OPEN).</summary>
    private static List<string> Records(string host)
    {
        var r = new RelativeConnector(host, 4, KeyedAccess.Sequential, 18);
        Assert.Equal(FileStatusCode.Success, r.Open(FileOpenMode.Input));
        var all = new List<string>();
        while (r.ReadNext(out string image) == FileStatusCode.Success) all.Add($"{r.LastSlot}={image}");
        Assert.Equal(FileStatusCode.Success, r.Close());
        return all;
    }

    // ── Annex A.1 item 31: the file coded character set ──────────────────────────────────────────────────

    /// <summary>U+0000–U+00FF have a byte image and nothing above does; the strict encoding round-trips all 256
    /// and REFUSES the rest instead of writing '?'.</summary>
    [Fact]
    public void FileCharacterSet_IsLatin1_AndRefusesRatherThanReplaces()
    {
        Assert.False(FileCharacterSet.HasCharacterWithoutByteImage("A" + EAcute + (char)0xFF));
        Assert.True(FileCharacterSet.HasCharacterWithoutByteImage("A" + Euro));
        Assert.True(FileCharacterSet.HasCharacterWithoutByteImage(((char)0x100).ToString()));
        Assert.True(FileCharacterSet.HasCharacterWithoutByteImage(((char)0xFFFE).ToString()));
        var all = new string(Enumerable.Range(0, 256).Select(i => (char)i).ToArray());
        byte[] bytes = FileCharacterSet.Medium.GetBytes(all);
        Assert.Equal(Enumerable.Range(0, 256).Select(i => (byte)i), bytes);
        Assert.Equal(all, FileCharacterSet.Medium.GetString(bytes));
        Assert.Throws<EncoderFallbackException>(() => FileCharacterSet.Medium.GetBytes("A" + Euro));
    }

    /// <summary>The REWRITE arms of the keyed organizations ask the same refusal as their WRITEs ('91'), and the
    /// record in the store is left as it was (§14.9.35.4: an unsuccessful REWRITE replaces nothing).</summary>
    [Fact]
    public void KeyedRewrite_OfARecordWithoutAByteImage_Is91_AndReplacesNothing()
    {
        var r = new RelativeConnector(Host("rw.dat"), 4, KeyedAccess.Random, 4);
        Assert.Equal(FileStatusCode.Success, r.Open(FileOpenMode.Output));
        r.SetPendingKey(1);
        Assert.Equal(FileStatusCode.Success, r.Write("ABCD"));
        Assert.Equal(FileStatusCode.Success, r.Close());
        Assert.Equal(FileStatusCode.Success, r.Open(FileOpenMode.IO));
        r.SetPendingKey(1);
        Assert.Equal(FileStatusCode.CharacterWithoutByteImage, r.Rewrite("A" + Euro + "CD"));
        Assert.Equal(FileStatusCode.Success, r.ReadRandom(out string rel));
        Assert.Equal("ABCD", rel);
        Assert.Equal(FileStatusCode.Success, r.Close());

        var ix = new IndexedConnector(Host("ix.dat"), 8, KeyedAccess.Random, [new(0, 4)]);
        Assert.Equal(FileStatusCode.Success, ix.Open(FileOpenMode.Output));
        Assert.Equal(FileStatusCode.Success, ix.Write("K001DATA"));
        Assert.Equal(FileStatusCode.Success, ix.Close());
        Assert.Equal(FileStatusCode.Success, ix.Open(FileOpenMode.IO));
        Assert.Equal(FileStatusCode.CharacterWithoutByteImage, ix.Rewrite("K001D" + Euro + "TA"));
        Assert.Equal(FileStatusCode.Success, ix.ReadRandom(-1, "K001    ", out string idx));
        Assert.Equal("K001DATA", idx);
        Assert.Equal(FileStatusCode.Success, ix.Close());
    }

    /// <summary>The '91' test asks what the WRITE TRANSFERS: a varying record's area positions past its length
    /// never reach the medium, so a character there refuses nothing (§13.18.43 GR13 — the record is written at
    /// its length).</summary>
    [Fact]
    public void SequentialWrite_TestsOnlyTheTransferredLength_OfAVaryingRecord()
    {
        var c = new SequentialConnector(Host("v.dat"), recordWidth: 6, lineSequential: false, varyMin: 1, varyMax: 6);
        Assert.Equal(FileStatusCode.Success, c.Open(FileOpenMode.Output));
        Assert.Equal(FileStatusCode.Success, c.Write("ABC" + Euro + "EF", 3, page: null));
        Assert.Equal(FileStatusCode.CharacterWithoutByteImage, c.Write("ABC" + Euro + "EF", 4, page: null));
        Assert.Equal(FileStatusCode.Success, c.Close());
    }

    /// <summary>§12.4.5.7.4 GR1 — "Each collating sequence is a fixed file attribute" — over the WHOLE native
    /// repertoire (kb/Work PB1093): two sequences that order every Latin-1 character identically but U+0100 / U+0101
    /// differently are different attributes, and the fingerprint says so (it hashed only U+0000–U+00FF before).</summary>
    [Fact]
    public void CollatingFingerprint_SeesADifferenceAboveLatin1()
    {
        var ab = new AlphanumericCollation([0x100, 0x101], [0, 1], [0x100, 0x101], 2);
        var ba = new AlphanumericCollation([0x100, 0x101], [1, 0], [0x101, 0x100], 2);
        for (int c = 0; c <= 0xFF; c++) Assert.Equal(ab.Weight((char)c), ba.Weight((char)c));
        Assert.NotEqual(FixedFileAttributes.Fingerprint(ab), FixedFileAttributes.Fingerprint(ba));
        Assert.Equal(FixedFileAttributes.Fingerprint(ab),
            FixedFileAttributes.Fingerprint(new AlphanumericCollation([0x100, 0x101], [0, 1], [0x100, 0x101], 2)));
    }

    /// <summary>With no CODE-SET a line sequential file is UTF-8 text (kb/Work PB1760), so its character set is every
    /// Unicode scalar value from U+0020 in BOTH record-area classes: U+20AC, a surrogate PAIR and HIGH-VALUE (U+FFFF)
    /// are members; a C0 control and an unpaired surrogate (including a non-UTF-8 byte's U+DC80–U+DCFF escape) are
    /// not. A national area's characters reach it as UTF-16BE byte pairs.</summary>
    [Fact]
    public void LineSequentialCharacterSet_IsTheUtf8ScalarValues_FromSpace()
    {
        Assert.False(LineSequentialCharacterSet.HasCharacterOutside("A" + EAcute + Euro + "\uFFFF", national: false, codeSet: null));
        Assert.False(LineSequentialCharacterSet.HasCharacterOutside("A\uD83D\uDE00", national: false, codeSet: null));   // one pair
        Assert.True(LineSequentialCharacterSet.HasCharacterOutside("A\uD83D", national: false, codeSet: null));          // unpaired
        Assert.True(LineSequentialCharacterSet.HasCharacterOutside("A\uDCE9", national: false, codeSet: null));          // byte escape
        Assert.True(LineSequentialCharacterSet.HasCharacterOutside("A\t", national: false, codeSet: null));
        // N"€" as its UTF-16BE bytes: 0x20 0xAC.
        Assert.False(LineSequentialCharacterSet.HasCharacterOutside(new string([(char)0x20, (char)0xAC]), national: true, codeSet: null));
        Assert.True(LineSequentialCharacterSet.HasCharacterOutside(new string([(char)0x00, (char)0x0A]), national: true, codeSet: null));
    }

    /// <summary>The UTF-8 line codec (kb/Work PB1760): every member round-trips through the byte channel as its UTF-8
    /// bytes; a byte that is not UTF-8 decodes to its U+DC80–U+DCFF escape (so the record keeps the byte and the READ
    /// reports '09'); a national area travels as its characters, not its pair bytes.</summary>
    [Fact]
    public void LineSequentialEncoding_IsUtf8_WithAByteEscape()
    {
        string text = "h" + EAcute + Euro + "\uFFFF\uD83D\uDE00";
        string channel = LineSequentialEncoding.ToChannel(text, nationalArea: false);
        Assert.Equal(System.Text.Encoding.UTF8.GetBytes(text), channel.Select(c => (byte)c).ToArray());
        Assert.Equal(text, LineSequentialEncoding.FromChannel(channel, nationalArea: false));
        Assert.Equal("caf\uDCE9", LineSequentialEncoding.FromChannel("caf\u00E9", nationalArea: false));   // Latin-1 é
        Assert.Equal("\uDCE2\uDC82", LineSequentialEncoding.FromChannel("\u00E2\u0082", nationalArea: false));   // torn €
        string pairs = CobolBits.NatBytes(Euro + "A");
        Assert.Equal("\u00E2\u0082\u00ACA", LineSequentialEncoding.ToChannel(pairs, nationalArea: true));
        Assert.Equal(pairs, LineSequentialEncoding.FromChannel("\u00E2\u0082\u00ACA", nationalArea: true));
        Assert.Throws<InvalidOperationException>(() => LineSequentialEncoding.ToChannel("A\uD83D", nationalArea: false));
    }

    /// <summary>kb/Work PB2834 — <see cref="LineSequentialEncoding.ChannelLength"/> is <c>ToChannel</c>'s length where an
    /// image exists, and TOTAL: a lone surrogate (no UTF-8 form, so no image) counts at the fewest bytes any image
    /// could take, one, which is what lets REWRITE's GR17 b) size rule run before d)'s character-set rule.</summary>
    [Fact]
    public void LineSequentialEncoding_ChannelLength_IsToChannelsLength_AndTotal()
    {
        string text = "h" + EAcute + Euro + "￿😀";
        Assert.Equal(LineSequentialEncoding.ToChannel(text, nationalArea: false).Length,
            LineSequentialEncoding.ChannelLength(text, nationalArea: false));
        string pairs = CobolBits.NatBytes(Euro + "A");
        Assert.Equal(LineSequentialEncoding.ToChannel(pairs, nationalArea: true).Length,
            LineSequentialEncoding.ChannelLength(pairs, nationalArea: true));
        Assert.Equal(2, LineSequentialEncoding.ChannelLength("A\uD83D", nationalArea: false));       // lone high surrogate: 1 byte
        Assert.Equal(5, LineSequentialEncoding.ChannelLength("\uDCE9😀", nationalArea: false));   // 1 + a 4-byte pair
        Assert.Equal(0, LineSequentialEncoding.ChannelLength("", nationalArea: false));
    }

    /// <summary>⛔ THE STORAGE-BYTE LAW (kb/Work PB1759, design D29): byte 0xFF is HIGH-VALUE (U+FFFF), every other byte
    /// b is U+00bb, and a character's byte is its low byte — so U+00FF and U+FFFF share 0xFF, and the fixed-record
    /// channel writes HIGH-VALUE as 0xFF and reads 0xFF back as HIGH-VALUE.</summary>
    [Fact]
    public void StorageByteLaw_HighValueIsTheByteFF_OnTheChannelToo()
    {
        Assert.Equal('\uFFFF', NativeCollatingSequence.HighValue);
        Assert.Equal('\uFFFF', StorageByte.ToChar(0xFF));
        Assert.Equal('\u00FE', StorageByte.ToChar(0xFE));
        Assert.Equal((byte)0xFF, StorageByte.ToByte('\uFFFF'));
        Assert.Equal((byte)0xFF, StorageByte.ToByte('\u00FF'));
        Assert.True(StorageByte.HasByte('\uFFFF'));
        Assert.False(StorageByte.HasByte('\u0100'));
        Assert.False(StorageByte.HasByte('\uFFFE'));
        Assert.Equal("A\u00FF", FileCharacterSet.ToChannel("A\uFFFF", codeSet: null));
        Assert.Equal("A\uFFFF", FileCharacterSet.FromChannel("A\u00FF", codeSet: null));
        Assert.False(FileCharacterSet.HasCharacterWithoutByteImage("A\uFFFF"));
    }
}
