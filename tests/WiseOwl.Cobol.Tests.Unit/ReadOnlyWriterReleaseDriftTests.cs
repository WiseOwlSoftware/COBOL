// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime.IO;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A RECORD A WRITE RELEASED IS IN THE PHYSICAL FILE WHEN THE WRITE RETURNS, FOR EVERY WRITER WHOSE SHARING MODE
/// ADMITS ANY OTHER FILE CONNECTOR — A READER AS WELL AS A WRITER (kb/Work PB1587).
///
/// <para><b>The derivation.</b> §14.9.51.4 GR12: <i>"The successful execution of a WRITE statement releases a logical
/// record to the operating environment."</i> §9.1.15 2): <i>"The sharing with read only mode restricts concurrent
/// access to a physical file through file connectors other than this one, to input mode."</i> — so the INPUT
/// connector Table 19 admits beside a <c>SHARING WITH READ ONLY</c> writer exists, and §14.9.30.4 GR21 makes its READ
/// select from <i>"the physical file"</i>. The standard prints no buffering rule for a WRITE: the release is the
/// whole of it.</para>
///
/// <para><b>What PB1587 was.</b> <c>SequentialConnector.ReleaseRecord</c> flushed the writer only when the posture
/// admitted another WRITER (<see cref="FileLockPosture.AdmitsAnotherWriter"/>, <c>SHARING WITH ALL OTHER</c>), so a
/// <c>READ ONLY</c> writer's records sat in its 4,096-byte input-output area and the reader met AT END ('10') on a
/// file whose writer had answered '00' for each of them. The RELATIVE organization, which writes at every
/// statement, answered the same program the other way: two arms of one rule. The flush now follows
/// <see cref="FileLockPosture.AdmitsAnotherRunUnit"/> — every mode but <c>NO OTHER</c>.</para>
///
/// <para><b>The exception paths.</b> An empty file is the at end condition ('10', §14.9.30.4 GR21) and a READ after
/// an unsuccessful READ is '46'; the reader Table 19 refuses beside the writer is '61' and leaves the writer as it
/// was; and a writer whose medium refuses the flush answers its refusal at the FIRST WRITE ('34'), where it used to
/// answer '00' until the area filled (the same boundary <see cref="FileBoundaryTests"/> pins for the sole writer).</para>
/// </summary>
public sealed class ReadOnlyWriterReleaseDriftTests
{
    private static string Rec(int n) => $"R{n % 1000:000}";

    /// <summary>Every sequential framing × the two modes that add records to a file × the two sharing modes whose
    /// writer admits a reader. <c>NO OTHER</c> is absent on purpose: it admits no reader, so nothing can observe
    /// whether its records have left its area, and Table 19 refuses the reader (<see cref="ARefusedReader_Is61_AndTheWriterIsUnaffected"/>).</summary>
    public static TheoryData<SharedExtendOpenDriftTests.Framing, FileOpenMode, FileSharing> WriterShapes()
    {
        var data = new TheoryData<SharedExtendOpenDriftTests.Framing, FileOpenMode, FileSharing>();
        foreach (var f in Enum.GetValues<SharedExtendOpenDriftTests.Framing>())
            foreach (var m in new[] { FileOpenMode.Output, FileOpenMode.Extend })
                foreach (var s in new[] { FileSharing.ReadOnly, FileSharing.AllOther })
                    data.Add(f, m, s);
        return data;
    }

    private static void RegisterSequential(FileRegistry reg, string name, string host,
        SharedExtendOpenDriftTests.Framing framing) =>
        reg.Register(name, host, recordWidth: 4,
            lineSequential: framing == SharedExtendOpenDriftTests.Framing.LineSequential,
            optional: false,
            varyMin: framing == SharedExtendOpenDriftTests.Framing.VaryingRecordSequential ? 4 : -1,
            varyMax: framing == SharedExtendOpenDriftTests.Framing.VaryingRecordSequential ? 8 : -1);

    /// <summary>SHARING WITH ALL OTHER needs a LOCK MODE clause (§14.9.27.3 SR8); READ ONLY needs none.</summary>
    private static void Share(FileRegistry reg, string name, FileSharing sharing) =>
        reg.RegisterSharing(name, sharing, FileHarness.LockModeRequiredBy(sharing), multiple: false);

    private static string Read(FileRegistry reg, string name, out string image) =>
        reg.ReadShared(name, false, FileRecordLock.None, false, false, FileRetryKind.None, 0, out image);

    private static string Write(FileRegistry reg, string name, string image) =>
        reg.WriteShared(name, image, -1, FileRecordLock.None, FileRetryKind.None, 0, page: null);

    private static string Open(FileRegistry reg, string name, FileOpenMode mode)
    {
        reg.OpenStatic(name, mode);
        return reg.Status(name);
    }

    /// <summary>⛔ THE RULE. A reader open beside the writer reads each record the moment its WRITE returns, in every
    /// framing, in both adding modes and under both sharing modes that admit it — and meets the at end condition after
    /// the last. The reader stays OPEN across the writes, so a stale read-ahead would be caught too (the writer's
    /// release announces itself to the reader's buffer, <c>SequentialConnector.NoteRelease</c>).</summary>
    [Theory]
    [MemberData(nameof(WriterShapes))]
    public void AWriteIsInThePhysicalFileWhenItReturns(
        SharedExtendOpenDriftTests.Framing framing, FileOpenMode mode, FileSharing writerSharing)
    {
        string host = FileHarness.Tmp($"w-{framing}-{mode}-{writerSharing}");
        try
        {
            var reg = new FileRegistry();
            RegisterSequential(reg, "W", host, framing);
            RegisterSequential(reg, "R", host, framing);
            Share(reg, "W", writerSharing);
            Share(reg, "R", FileSharing.AllOther);
            if (mode == FileOpenMode.Extend)
            {
                // EXTEND needs a file: seed it through a connector of its own, closed before the writer opens.
                RegisterSequential(reg, "SEED", host, framing);
                Assert.Equal(FileStatusCode.Success, Open(reg, "SEED", FileOpenMode.Output));
                Assert.Equal(FileStatusCode.Success, Write(reg, "SEED", "SEED"));
                reg.Close("SEED");
            }

            Assert.Equal(FileStatusCode.Success, Open(reg, "W", mode));
            Assert.Equal(FileStatusCode.Success, Open(reg, "R", FileOpenMode.Input));
            if (mode == FileOpenMode.Extend)
            {
                Assert.Equal(FileStatusCode.Success, Read(reg, "R", out string seed));
                Assert.Equal("SEED", seed.TrimEnd());
            }

            for (int i = 1; i <= 3; i++)
            {
                Assert.Equal(FileStatusCode.Success, Write(reg, "W", Rec(i)));
                Assert.Equal(FileStatusCode.Success, Read(reg, "R", out string got));
                Assert.Equal(Rec(i), got.TrimEnd());
            }

            Assert.Equal(FileStatusCode.AtEnd, Read(reg, "R", out _));   // nothing past what was released
            reg.Close("R");
            reg.Close("W");
        }
        finally { FileHarness.TryDelete(host); }
    }

    /// <summary>The reader that has read ahead: it takes record 1 (filling its buffer with the whole file), the
    /// writer releases two more, and the reader's next READs are those records, not an end of file served from the
    /// buffer (§14.9.30.4 GR21: the selection is made from the physical file at the READ).</summary>
    [Theory]
    [MemberData(nameof(WriterShapes))]
    public void AReaderThatHasReadAheadSeesWhatTheWriterThenReleases(
        SharedExtendOpenDriftTests.Framing framing, FileOpenMode mode, FileSharing writerSharing)
    {
        string host = FileHarness.Tmp($"ra-{framing}-{mode}-{writerSharing}");
        try
        {
            var reg = new FileRegistry();
            RegisterSequential(reg, "W", host, framing);
            RegisterSequential(reg, "R", host, framing);
            Share(reg, "W", writerSharing);
            Share(reg, "R", FileSharing.AllOther);
            if (mode == FileOpenMode.Extend)
            {
                RegisterSequential(reg, "SEED", host, framing);
                Assert.Equal(FileStatusCode.Success, Open(reg, "SEED", FileOpenMode.Output));
                Assert.Equal(FileStatusCode.Success, Write(reg, "SEED", "SEED"));
                reg.Close("SEED");
            }

            Assert.Equal(FileStatusCode.Success, Open(reg, "W", mode));
            Assert.Equal(FileStatusCode.Success, Write(reg, "W", Rec(1)));
            Assert.Equal(FileStatusCode.Success, Open(reg, "R", FileOpenMode.Input));
            if (mode == FileOpenMode.Extend) Assert.Equal(FileStatusCode.Success, Read(reg, "R", out _));
            Assert.Equal(FileStatusCode.Success, Read(reg, "R", out string first));   // the reader's buffer now holds the file
            Assert.Equal(Rec(1), first.TrimEnd());

            Assert.Equal(FileStatusCode.Success, Write(reg, "W", Rec(2)));
            Assert.Equal(FileStatusCode.Success, Write(reg, "W", Rec(3)));
            Assert.Equal(FileStatusCode.Success, Read(reg, "R", out string second));
            Assert.Equal(Rec(2), second.TrimEnd());
            Assert.Equal(FileStatusCode.Success, Read(reg, "R", out string third));
            Assert.Equal(Rec(3), third.TrimEnd());
            Assert.Equal(FileStatusCode.AtEnd, Read(reg, "R", out _));
            reg.Close("R");
            reg.Close("W");
        }
        finally { FileHarness.TryDelete(host); }
    }

    /// <summary>The at end path. An OUTPUT file with nothing released is empty: the reader's READ is '10' (§14.9.30.4
    /// GR21: no next logical record), and the READ after that unsuccessful READ is '46' even when the writer has
    /// released a record in between — the program's sequence, not the file, is what is refused, and a re-OPEN reads
    /// the record.</summary>
    [Theory]
    [InlineData(SharedExtendOpenDriftTests.Framing.FixedRecordSequential)]
    [InlineData(SharedExtendOpenDriftTests.Framing.LineSequential)]
    [InlineData(SharedExtendOpenDriftTests.Framing.VaryingRecordSequential)]
    public void AnEmptyFile_IsAtEnd_AndTheNextRead_Is46(SharedExtendOpenDriftTests.Framing framing)
    {
        string host = FileHarness.Tmp($"end-{framing}");
        try
        {
            var reg = new FileRegistry();
            RegisterSequential(reg, "W", host, framing);
            RegisterSequential(reg, "R", host, framing);
            Share(reg, "W", FileSharing.ReadOnly);
            Share(reg, "R", FileSharing.AllOther);
            Assert.Equal(FileStatusCode.Success, Open(reg, "W", FileOpenMode.Output));
            Assert.Equal(FileStatusCode.Success, Open(reg, "R", FileOpenMode.Input));

            Assert.Equal(FileStatusCode.AtEnd, Read(reg, "R", out _));
            Assert.Equal(FileStatusCode.Success, Write(reg, "W", Rec(1)));
            Assert.Equal(FileStatusCode.NoValidNextRecord, Read(reg, "R", out _));   // '46'

            reg.Close("R");
            Assert.Equal(FileStatusCode.Success, Open(reg, "R", FileOpenMode.Input));
            Assert.Equal(FileStatusCode.Success, Read(reg, "R", out string got));
            Assert.Equal(Rec(1), got.TrimEnd());
            reg.Close("R");
            reg.Close("W");
        }
        finally { FileHarness.TryDelete(host); }
    }

    /// <summary>Table 19 (§14.9.27.4): a reader that states <c>SHARING WITH READ ONLY</c> is refused beside a READ
    /// ONLY writer, and ANY reader is refused beside a <c>NO OTHER</c> writer — '61' (§9.1.13.9 item 1), the file
    /// not affected (GR25): the writer's records are still released and an admitted reader still reads them.</summary>
    [Fact]
    public void ARefusedReader_Is61_AndTheWriterIsUnaffected()
    {
        string host = FileHarness.Tmp("refused");
        try
        {
            var reg = new FileRegistry();
            const SharedExtendOpenDriftTests.Framing framing = SharedExtendOpenDriftTests.Framing.FixedRecordSequential;
            RegisterSequential(reg, "W", host, framing);
            RegisterSequential(reg, "RO", host, framing);
            RegisterSequential(reg, "R", host, framing);
            Share(reg, "W", FileSharing.ReadOnly);
            Share(reg, "RO", FileSharing.ReadOnly);
            Share(reg, "R", FileSharing.AllOther);
            Assert.Equal(FileStatusCode.Success, Open(reg, "W", FileOpenMode.Output));

            Assert.Equal(FileStatusCode.FileSharingConflict, Open(reg, "RO", FileOpenMode.Input));   // '61'
            Assert.Equal(FileStatusCode.Success, Write(reg, "W", Rec(1)));
            Assert.Equal(FileStatusCode.Success, Open(reg, "R", FileOpenMode.Input));
            Assert.Equal(FileStatusCode.Success, Read(reg, "R", out string got));
            Assert.Equal(Rec(1), got.TrimEnd());
            reg.Close("R");
            reg.Close("W");

            string noOtherHost = FileHarness.Tmp("refused-no-other");
            try
            {
                RegisterSequential(reg, "N", noOtherHost, framing);
                RegisterSequential(reg, "NR", noOtherHost, framing);
                reg.RegisterSharing("N", FileSharing.NoOther, FileLockMode.None, multiple: false);
                Share(reg, "NR", FileSharing.AllOther);
                Assert.Equal(FileStatusCode.Success, Open(reg, "N", FileOpenMode.Output));
                Assert.Equal(FileStatusCode.FileSharingConflict, Open(reg, "NR", FileOpenMode.Input));
                Assert.Equal(FileStatusCode.Success, Write(reg, "N", Rec(9)));
                reg.Close("N");
                Assert.Equal(FileStatusCode.Success, Open(reg, "NR", FileOpenMode.Input));
                Assert.Equal(FileStatusCode.Success, Read(reg, "NR", out string after));
                Assert.Equal(Rec(9), after.TrimEnd());
                reg.Close("NR");
            }
            finally { FileHarness.TryDelete(noOtherHost); }
        }
        finally { FileHarness.TryDelete(host); }
    }

    /// <summary>The keyed organizations, in ANOTHER RUN UNIT (a second <see cref="FileRegistry"/> shares neither the
    /// physical-file table nor a host handle with the first, as <c>RunUnitFileLockDriftTests</c> relies on): a record a
    /// <c>SHARING WITH READ ONLY</c> writer's WRITE answered '00' for is READ by the reader at once, before the
    /// writer's CLOSE — the same rule as the sequential arms, §14.9.51.4 GR12 and §9.1.15 2). A rejected WRITE (the
    /// duplicate key, '22', §14.9.51.4 GR33 a) on a relative file, GR42 b) on an indexed one) releases nothing, so the reader meets
    /// exactly the records that were accepted.</summary>
    [Theory]
    [InlineData(FileOrg.Relative)]
    [InlineData(FileOrg.Indexed)]
    public void AKeyedReadOnlyWriter_IsReadByAnotherRunUnit_AtTheWrite(FileOrg org)
    {
        string host = FileHarness.Tmp($"keyed-{org}");
        try
        {
            var a = new FileRegistry();
            var b = new FileRegistry();
            FileHarness.Register(a, "A", host, org, FileSharing.ReadOnly, FileHarness.LockModeRequiredBy(FileSharing.ReadOnly));
            FileHarness.Register(b, "B", host, org, FileSharing.AllOther, FileHarness.LockModeRequiredBy(FileSharing.AllOther));
            Assert.Equal(FileStatusCode.Success, Open(a, "A", FileOpenMode.Output));
            Assert.Equal(FileStatusCode.Success, Open(b, "B", FileOpenMode.Input));

            Assert.Equal(FileStatusCode.Success, FileHarness.Write(a, "A", org, 1, "DATA"));
            Assert.Equal(FileStatusCode.Success, Read(b, "B", out string first));
            Assert.Equal("0001DATA", first);
            Assert.Equal(FileStatusCode.DuplicateKey, FileHarness.Write(a, "A", org, 1, "DATA"));   // '22': nothing released
            Assert.Equal(FileStatusCode.Success, FileHarness.Write(a, "A", org, 2, "DATA"));
            Assert.Equal(FileStatusCode.Success, Read(b, "B", out string second));
            Assert.Equal("0002DATA", second);
            Assert.Equal(FileStatusCode.AtEnd, Read(b, "B", out _));
            b.Close("B");
            a.Close("A");
        }
        finally { FileHarness.TryDelete(host); }
    }

    /// <summary>§14.9.51.4 GR20 / §9.1.13.6 item 3 at the release: the medium that is full by construction
    /// (<c>/dev/full</c>, Linux) refuses the flush a <c>READ ONLY</c> writer now makes at each WRITE, so the FIRST
    /// WRITE answers '34' — the record was not released, and the statement did not answer '00' for it. A host without
    /// the device must not be Linux, so this cannot pass on the CI host by not running
    /// (<see cref="FileBoundaryTests.SequentialWrite_OnAFullMedium_Is34_AtTheWrite"/> is the sole writer's twin, which
    /// reports the same refusal only when its area fills).</summary>
    [Fact]
    public void AReadOnlyWriter_OnAFullMedium_Is34_AtTheFirstWrite()
    {
        const string full = "/dev/full";
        if (!File.Exists(full))
        {
            Assert.False(OperatingSystem.IsLinux(), "a Linux host provides /dev/full; the boundary was not exercised");
            return;
        }
        // Built directly, as FileBoundaryTests builds the sole writer's, so the registry's arbitration (and its
        // cross-run-unit region lock) is not between the test and the medium: the sharing mode is the one thing under
        // test, and it is stated.
        var c = new SequentialConnector(full, recordWidth: 512, lineSequential: false) { HostSharing = FileSharing.ReadOnly };
        Assert.Equal(FileStatusCode.Success, c.Open(FileOpenMode.Output));
        Assert.Equal(FileStatusCode.PermanentBoundary, c.Write(new string('X', 512), -1, page: null));
        c.Close();   // the area holds nothing the medium has not already refused
    }
}
