// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using CobolNet.Runtime.IO.Store;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ AN OPEN'S FENCE AT THE END OF THE KEYED FORMAT IS REFUSED ONLY FOR THE PAGES IT WRITES (kb/Work PB3396;
/// docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.9). An OPEN I-O of a relative or indexed file retires the file's
/// adopted state before its first statement (2.2.1.6 R5, R6), and the retirement is a promotion, which has no '24'
/// (§9.1.13.5 item 4 is a record WRITE's; docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.9). Its reservation is an
/// upper bound (R2), and the format ends at 2^32 pages (<see cref="PagedFile.FormatPages"/>), so a store a few pages
/// short of the end was once refused its OPEN I-O with §9.1.13.6 item 1's '30' <i>"A permanent error exists and no
/// further information is available concerning the input-output operation"</i> although the fence would have written
/// one page. These cases run the OPEN through the registry, as a program declares the file, over a file that WROTE one
/// record and was never closed (the fence then writes ONE page, the ledger's) and whose meta pages record the format's
/// 2^32 pages as used: with no page left the OPEN I-O answers '30' and an OPEN INPUT, which writes nothing, still reads
/// the record. Both organizations share the store, and both are asked. The other half — the one page left, and the
/// OPEN I-O completes having taken exactly it — is <c>PagedFileBoundaryTests</c>': a real host file cannot grow to
/// 2^32 pages, so only the page layer's memory medium can write it. The compiled program's transfers of control after
/// that '30' are <c>IndexedEnginePermanentErrorWitnessTests</c>' <c>FenceRefused</c> cases.
/// </summary>
public sealed class KeyedFenceBoundaryTests : CobolNetTestBase
{
    private const string IndexedRecord = "0001AAAA";

    internal static void RegisterIndexed(FileRegistry reg, string host)
    {
        reg.RegisterIndexed("IX", host, recordWidth: 8, optional: false, accessMode: (int)KeyedAccess.Dynamic,
            primeKey: [new(0, 4)], varyMin: -1, varyMax: -1);
        reg.AddAlternateKey("IX", [new(4, 4)], duplicates: true);
    }

    private static void RegisterRelative(FileRegistry reg, string host) =>
        reg.RegisterRelative("RV", host, recordWidth: 8, optional: false, accessMode: (int)KeyedAccess.Dynamic,
            relativeKeyDigits: 4, varyMin: -1, varyMax: -1);

    /// <summary>OPEN OUTPUT under SHARING WITH ALL OTHER, so that the host handle admits a reader beside it
    /// (<see cref="FileConnector.HostShare"/>) and <see cref="ReadWhileOpen"/> sees the file as it is while open.</summary>
    private static void OpenSharedOutput(FileRegistry reg, string name)
    {
        reg.OpenShared(name, FileOpenMode.Output, hasSharingOverride: true, FileSharing.AllOther, FileRetryKind.None, 0,
            OpenTapePhrase.None, reg.HostPathOf(name), assignDynamic: false, page: null);
        Assert.Equal("00", reg.Status(name));
    }

    /// <summary>The bytes of the physical file at <paramref name="host"/> as they are NOW, beside the run unit that holds
    /// it open (its handle is shared for reading).</summary>
    private static byte[] ReadWhileOpen(string host)
    {
        using var stream = new FileStream(host, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);
        return bytes;
    }

    /// <summary>Every meta page of <paramref name="file"/> rewritten to record <c>FormatPages - headroom</c> pages.</summary>
    private static byte[] Recorded(byte[] file, ulong headroom)
    {
        var forged = (byte[])file.Clone();
        for (uint p = 0; p < MetaPage.MetaPages; p++)
        {
            var page = forged.AsSpan((int)p * PageHeader.PageSize, PageHeader.PageSize);
            if (MetaPage.Decode(page, p) is not { } meta) continue;
            (meta with { PageCount = PagedFile.FormatPages - headroom }).Encode(p).CopyTo(page);
        }
        return forged;
    }

    /// <summary>An INDEXED file of the one record, written and NOT closed (the bytes are read beside the run unit that
    /// holds it, so the file is as a process death left it), recording <c>FormatPages - headroom</c> pages.</summary>
    internal static string UnclosedIndexed(string host, ulong headroom)
    {
        var reg = new FileRegistry();
        RegisterIndexed(reg, host);
        OpenSharedOutput(reg, "IX");
        Assert.Equal("00", reg.WriteShared("IX", IndexedRecord, -1, FileRecordLock.None, FileRetryKind.None, 0, page: null));
        byte[] unclosed = ReadWhileOpen(host);
        reg.Close("IX");
        File.WriteAllBytes(host, Recorded(unclosed, headroom));
        return host;
    }

    private static string UnclosedRelative(string host, ulong headroom)
    {
        var reg = new FileRegistry();
        RegisterRelative(reg, host);
        OpenSharedOutput(reg, "RV");
        reg.SetRelativeKey("RV", 4);
        Assert.Equal("00", reg.WriteKeyed("RV", "0004DDDD", 8));
        byte[] unclosed = ReadWhileOpen(host);
        reg.Close("RV");
        File.WriteAllBytes(host, Recorded(unclosed, headroom));
        return host;
    }

    private string NewFile(string tag) => Path.Combine(TempDir, $"{tag}.dat");

    [Fact]   // §9.1.13.6 item 1: no page for the fence's one write is '30'; OPEN INPUT writes nothing and reads the record.
    public void Indexed_OpenIo_With_No_Page_For_The_Fence_Answers_30_And_OpenInput_Reads_The_File()
    {
        string host = UnclosedIndexed(NewFile("fence-ix-0"), headroom: 0);
        var refused = new FileRegistry();
        RegisterIndexed(refused, host);
        refused.OpenStatic("IX", FileOpenMode.IO);
        Assert.Equal("30", refused.Status("IX"));

        var reader = new FileRegistry();
        RegisterIndexed(reader, host);
        reader.OpenStatic("IX", FileOpenMode.Input);
        Assert.Equal("00", reader.Status("IX"));
        Assert.Equal("00", reader.ReadKeyedShared("IX", -1, "0001    ", FileRecordLock.None, ignoringLock: false,
            FileRetryKind.None, 0, out string record));
        Assert.Equal(IndexedRecord, record);
        reader.Close("IX");
        Assert.Equal("00", reader.Status("IX"));
    }

    [Fact]   // The relative twin: the same store, the same fence, the same status.
    public void Relative_OpenIo_With_No_Page_For_The_Fence_Answers_30_And_OpenInput_Reads_The_File()
    {
        string host = UnclosedRelative(NewFile("fence-rv-0"), headroom: 0);
        var refused = new FileRegistry();
        RegisterRelative(refused, host);
        refused.OpenStatic("RV", FileOpenMode.IO);
        Assert.Equal("30", refused.Status("RV"));

        var reader = new FileRegistry();
        RegisterRelative(reader, host);
        reader.OpenStatic("RV", FileOpenMode.Input);
        Assert.Equal("00", reader.Status("RV"));
        reader.SetRelativeKey("RV", 4);
        Assert.Equal("00", reader.ReadKeyedShared("RV", -1, "", FileRecordLock.None, ignoringLock: false,
            FileRetryKind.None, 0, out string record));
        Assert.Equal("0004DDDD", record);
        reader.Close("RV");
    }
}
