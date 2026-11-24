// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.IO;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EACH EDITION'S OWN TEXT DECIDES A SECOND FILE CONNECTOR'S OPEN OF A PHYSICAL FILE THE RUN UNIT HOLDS OPEN (kb/Work PB6161,
/// the owner's decision of 2026-11-24, which replaced PB670's option (i) at COBOL-85; <see cref="DialectBehavior.FileSharingFacility"/>).
///
/// <para>COBOL-85: the standard has no file sharing facility and keys its status 41 to the PHYSICAL file. <i>"The terms 'file
/// referenced by file-name' and 'file' mean the physical file regardless of the file-name used in the COBOL program"</i>
/// (§1985:II.2); <i>"each subsequent OPEN statement execution for that same file must be preceded by the execution of a CLOSE
/// statement"</i> (§1985:VII.4.3.4 GR5); and status 41 is <i>"An OPEN statement is attempted for a file in the open mode"</i>
/// (§1985:VII.1.3.5). Two SELECTs whose ASSIGN values reach one host path name one physical file (WiseOwl's mapping,
/// §1985:VII.2.3.3 SR3), so the second connector's OPEN answers '41' in every organization and every pair of modes, and the
/// file is not affected. From 2002 the facility arbitrates the same OPEN (§2002:9.1.14, §2023:9.1.15; Table 19) and its refusal
/// is '61'; '41' is keyed to the file connector there (§2002:9.1.12.6, §2023:9.1.13.7 GR1) and never answers another
/// connector's OPEN.</para>
///
/// <para>What each test fails on: the first on any pair of opens at 85 that is admitted, answers anything but '41', disturbs the
/// holder or the file, or leaves the refused connector unable to open once the holder closes; the second on a pair from 2002
/// that is not Table 19's cell; then a third connector; a process outside the run unit (the host's refusal, '30' at 85, '61'
/// from 2002); a second RUN UNIT, which no file lock refuses at 85; and the COBOL-85 writer, which buffers.</para>
/// </summary>
[Trait("Host", "ReFS")]
public sealed class SharingFacilityEditionTests
{
    private const int Cobol85 = 85;

    public static TheoryData<FileOrg, FileOpenMode, FileOpenMode> EveryPairOfOpens()
    {
        var data = new TheoryData<FileOrg, FileOpenMode, FileOpenMode>();
        foreach (var org in Enum.GetValues<FileOrg>())
            foreach (var existing in Enum.GetValues<FileOpenMode>())
                foreach (var incoming in Enum.GetValues<FileOpenMode>())
                    data.Add(org, existing, incoming);
        return data;
    }

    /// <summary>The two statuses of one pair of opens of one physical file by two clause-less connectors of one run
    /// unit, then the CLOSE of each: the second OPEN's status and the two CLOSE statuses.</summary>
    private static (string First, string Second, string CloseFirst, string CloseSecond) OpenPair(
        FileOrg org, FileOpenMode existing, FileOpenMode incoming, int edition)
    {
        string host = FileHarness.Tmp($"pb670-{org}-{existing}{incoming}-{edition}");
        try
        {
            FileHarness.Seed(host, org, ["SEED"]);
            var reg = new FileRegistry();
            FileHarness.Register(reg, "A", host, org, sharing: null, access: FileHarness.AccessFor(existing), edition: edition);
            FileHarness.Register(reg, "B", host, org, sharing: null, access: FileHarness.AccessFor(incoming), edition: edition);
            FileHarness.Open(reg, "A", existing);
            string first = reg.Status("A");
            FileHarness.Open(reg, "B", incoming);
            string second = reg.Status("B");
            string closeSecond = "";
            if (second[0] == '0') { reg.Close("B"); closeSecond = reg.Status("B"); }
            reg.Close("A");
            return (first, second, closeSecond, reg.Status("A"));
        }
        finally { FileHarness.TryDelete(host); }
    }

    /// <summary>§1985:VII.4.3.4 GR5 and §1985:VII.1.3.5 at COBOL-85, for every organization and every pair of open modes: a second
    /// connector's OPEN of a physical file another connector holds open answers '41'; the file is not affected (OUTPUT does not
    /// truncate it); the holder still reads its record and closes normally; the refused connector was never associated with the
    /// file (§1985:VII.4.3.4 GR1), so its CLOSE does not close the holder's; and once the holder has closed, the same OPEN
    /// succeeds.</summary>
    [Theory]
    [MemberData(nameof(EveryPairOfOpens))]
    public void AtCobol85_ASecondConnectorsOpenOfAnOpenPhysicalFileAnswers41(FileOrg org, FileOpenMode existing, FileOpenMode incoming)
    {
        string host = FileHarness.Tmp($"pb6161-{org}-{existing}{incoming}");
        try
        {
            FileHarness.Seed(host, org, ["SEED"]);
            byte[] before = File.ReadAllBytes(host);
            var reg = new FileRegistry();
            FileHarness.Register(reg, "A", host, org, sharing: null, access: FileHarness.AccessFor(existing), edition: Cobol85);
            FileHarness.Register(reg, "B", host, org, sharing: null, access: FileHarness.AccessFor(incoming), edition: Cobol85);
            FileHarness.Open(reg, "A", existing);
            Assert.Equal("00", reg.Status("A"));
            FileHarness.Open(reg, "B", incoming);
            Assert.True(reg.Status("B") == FileStatusCode.FileAlreadyOpen,
                $"{org} OPEN {existing} then a second connector's OPEN {incoming} at --std 85 answered '{reg.Status("B")}'; "
                + "1985 keys status 41 to the physical file (§1985:VII.1.3.5).");
            reg.Close("B");
            Assert.Equal(FileStatusCode.FileNotOpen, reg.Status("B"));   // B never entered an open mode
            if (existing is FileOpenMode.Input or FileOpenMode.IO)
            {
                Assert.Equal("00", FileHarness.ReadNext(reg, "A", out string image));   // the holder is undisturbed
                Assert.Equal(FileHarness.Rec(1, "SEED"), image);
            }
            reg.Close("A");
            Assert.Equal("00", reg.Status("A"));
            if (existing == FileOpenMode.Input) Assert.Equal(before, File.ReadAllBytes(host));   // a reader's file is byte-identical

            FileHarness.Open(reg, "B", incoming);   // OPEN after the holder's CLOSE is a normal open
            Assert.Equal("00", reg.Status("B"));
            reg.Close("B");
            Assert.Equal("00", reg.Status("B"));
        }
        finally { FileHarness.TryDelete(host); }
    }

    /// <summary>The same pair at each edition that has the facility, derived from that edition's own text: '41' is keyed to the file
    /// connector (§2002:9.1.12.6, §2014:9.1.12.6, §2023:9.1.13.7 GR1), so it never answers the second connector; the facility
    /// arbitrates instead under the implementor default (INPUT is SHARING WITH READ ONLY, every other mode SHARING WITH NO OTHER;
    /// §2002:14.8.26.3 GR22, DOC-A.1-77), and the file sharing conflict condition '61' refuses every pair but two readers: a NO OTHER
    /// holder refuses every opener and a NO OTHER opener every holder (§2002:9.1.12.8 items a and b, §2014:9.1.12.8,
    /// §2023:9.1.13.9 GR1).</summary>
    [Theory]
    [MemberData(nameof(EveryPairOfOpens))]
    public void From2002_TheSamePairIsArbitratedByTheSharingFacility(FileOrg org, FileOpenMode existing, FileOpenMode incoming)
    {
        foreach (int edition in new[] { 2002, 2014, 2023 })
        {
            var r = OpenPair(org, existing, incoming, edition);
            Assert.Equal("00", r.First);
            string expected = existing == FileOpenMode.Input && incoming == FileOpenMode.Input ? "00" : FileStatusCode.FileSharingConflict;
            Assert.True(r.Second == expected,
                $"{org} OPEN {existing} then a second connector's OPEN {incoming} at {edition} answered '{r.Second}', expected '{expected}'.");
        }
    }

    /// <summary>The default mode of a COBOL-85 connector is the one that restricts nobody, for every open mode
    /// (<see cref="FileRegistry.ImplementorDefaultSharing"/>), and the edition decides it at no other place: the same call
    /// at 2002 answers READ ONLY for INPUT and NO OTHER for the rest (DOC-A.1-77).</summary>
    [Fact]
    public void TheImplementorDefaultIsAFunctionOfTheEdition()
    {
        foreach (var mode in Enum.GetValues<FileOpenMode>())
        {
            Assert.Equal(FileSharing.AllOther, FileRegistry.ImplementorDefaultSharing(mode, Cobol85));
            Assert.Equal(mode == FileOpenMode.Input ? FileSharing.ReadOnly : FileSharing.NoOther,
                         FileRegistry.ImplementorDefaultSharing(mode, 2002));
        }
    }

    public static TheoryData<FileOrg, FileOpenMode> EveryOpen()
    {
        var data = new TheoryData<FileOrg, FileOpenMode>();
        foreach (var org in Enum.GetValues<FileOrg>())
            foreach (var mode in Enum.GetValues<FileOpenMode>())
                data.Add(org, mode);
        return data;
    }

    /// <summary>A process outside the run unit holds the file exclusively. Where the facility is in effect that is
    /// §2023:9.1.13.9 GR1's '61' (kb/Work PB860); at COBOL-85 '61' is a status the edition does not define, and the host's
    /// refusal is §2023:9.1.13.6 GR1's '30'. §2023:14.9.27.4 GR25 holds at both: the unsuccessful OPEN leaves the file as it
    /// was, OUTPUT's truncation included.</summary>
    [Theory]
    [MemberData(nameof(EveryOpen))]
    public void AnOutsiderHoldingTheFile_RefusesWith30AtCobol85And61From2002(FileOrg org, FileOpenMode mode)
    {
        Assert.True(HostCapability.Sharing.EnforcesExclusiveAccess, HostCapability.Sharing.Because);
        foreach (var (edition, expected) in new[] { (Cobol85, FileStatusCode.PermanentError), (2002, FileStatusCode.FileSharingConflict) })
        {
            string host = FileHarness.Tmp($"pb670-outsider-{org}-{mode}-{edition}");
            try
            {
                FileHarness.Seed(host, org, ["SEED"]);
                byte[] before = File.ReadAllBytes(host);
                var reg = new FileRegistry();
                FileHarness.Register(reg, "F", host, org, sharing: null, access: FileHarness.AccessFor(mode), edition: edition);
                string status;
                using (new FileStream(host, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    FileHarness.Open(reg, "F", mode);
                    status = reg.Status("F");
                }
                Assert.True(status == expected,
                    $"{org} OPEN {mode} at {edition} against an outside process's exclusive handle answered '{status}'; "
                    + $"expected '{expected}'.");
                reg.Close("F");
                Assert.Equal(FileStatusCode.FileNotOpen, reg.Status("F"));   // §2023:9.1.13.7 GR2: nothing was opened
                Assert.Equal(before, File.ReadAllBytes(host));               // §2023:14.9.27.4 GR25
                // The refusal ends with the handle: the connector's next OPEN succeeds.
                FileHarness.Open(reg, "F", mode);
                Assert.Equal("00", reg.Status("F"));
                reg.Close("F");
            }
            finally { FileHarness.TryDelete(host); }
        }
    }

    /// <summary>The cross-run-unit half of the same fact (§2023:9.1.15's file lock, <c>"thereby preventing other run units
    /// from opening that file with incompatible sharing rules"</c>, belongs to the facility): a second run unit's OPEN of a
    /// file the first holds open I-O, or OUTPUT, is a normal open at COBOL-85 and '61' from 2002.</summary>
    [Theory]
    [InlineData(FileOrg.Sequential)]
    [InlineData(FileOrg.Relative)]
    [InlineData(FileOrg.Indexed)]
    public void ASecondRunUnitIsNotRefusedAtCobol85(FileOrg org)
    {
        foreach (var (edition, expected) in new[] { (Cobol85, "00"), (2002, FileStatusCode.FileSharingConflict) })
        {
            string host = FileHarness.Tmp($"pb670-rununits-{org}-{edition}");
            try
            {
                FileHarness.Seed(host, org, ["SEED"]);
                var runUnit1 = new FileRegistry();
                var runUnit2 = new FileRegistry();
                FileHarness.Register(runUnit1, "A", host, org, sharing: null, edition: edition);
                FileHarness.Register(runUnit2, "B", host, org, sharing: null, edition: edition);
                FileHarness.Open(runUnit1, "A", FileOpenMode.IO);
                Assert.Equal("00", runUnit1.Status("A"));
                FileHarness.Open(runUnit2, "B", FileOpenMode.IO);
                Assert.True(runUnit2.Status("B") == expected,
                    $"{org} at {edition}: the second run unit's OPEN I-O beside the first's answered '{runUnit2.Status("B")}', "
                    + $"expected '{expected}'.");
                runUnit2.CloseAll();
                runUnit1.CloseAll();
            }
            finally { FileHarness.TryDelete(host); }
        }
    }

    /// <summary>A third SELECT of the same physical file at COBOL-85: while one connector holds the file, every other connector's OPEN,
    /// in every mode, answers '41' (§1985:VII.1.3.5), and an OPEN of the holder itself answers '41' as at every edition; after the
    /// holder's CLOSE the first of the others to OPEN becomes the holder, and the rest are refused in turn.</summary>
    [Theory]
    [InlineData(FileOrg.Sequential)]
    [InlineData(FileOrg.Relative)]
    [InlineData(FileOrg.Indexed)]
    public void AtCobol85_EveryOtherConnectorIsRefusedWhileOneHoldsTheFile(FileOrg org)
    {
        string host = FileHarness.Tmp($"pb6161-third-{org}");
        try
        {
            FileHarness.Seed(host, org, ["SEED"]);
            var reg = new FileRegistry();
            foreach (string name in new[] { "A", "B", "C" })
                FileHarness.Register(reg, name, host, org, sharing: null, access: FileHarness.AccessFor(FileOpenMode.IO), edition: Cobol85);
            FileHarness.Open(reg, "A", FileOpenMode.Input);
            Assert.Equal("00", reg.Status("A"));
            FileHarness.Open(reg, "A", FileOpenMode.Input);
            Assert.Equal(FileStatusCode.FileAlreadyOpen, reg.Status("A"));   // the connector's own re-OPEN
            foreach (string other in new[] { "B", "C" })
                foreach (var mode in Enum.GetValues<FileOpenMode>().Where(m => org == FileOrg.Sequential || m != FileOpenMode.Extend))
                {
                    FileHarness.Open(reg, other, mode);
                    Assert.True(reg.Status(other) == FileStatusCode.FileAlreadyOpen, $"{org} {other} OPEN {mode}: '{reg.Status(other)}'");
                }
            reg.Close("A");
            Assert.Equal("00", reg.Status("A"));
            FileHarness.Open(reg, "C", FileOpenMode.IO);
            Assert.Equal("00", reg.Status("C"));
            FileHarness.Open(reg, "B", FileOpenMode.Input);
            Assert.Equal(FileStatusCode.FileAlreadyOpen, reg.Status("B"));
            reg.Close("C");
            Assert.Equal("00", reg.Status("C"));
        }
        finally { FileHarness.TryDelete(host); }
    }

    /// <summary>The COST side at COBOL-85 (review R1-1 of PB670): with no sibling possible in the run unit and no other run unit
    /// promised its records (§2023:A.4.1), a writer keeps its records in its input-output areas until the CLOSE
    /// (§2023:14.9.51.4 GR12 releases a record to the operating environment), and a sibling's refused OPEN takes none of them.</summary>
    [Fact]
    public void AtCobol85_AWriterBuffers_AndARefusedSiblingReleasesNothing()
    {
        string host = FileHarness.Tmp("pb6161-lazy");
        try
        {
            int record = FileHarness.Rec(1, "AAAA").Length;
            var reg = new FileRegistry();
            foreach (string name in new[] { "A", "B" })
                FileHarness.Register(reg, name, host, FileOrg.Sequential, sharing: null, edition: Cobol85);
            FileHarness.Open(reg, "A", FileOpenMode.Output);
            Assert.Equal("00", FileHarness.Write(reg, "A", FileOrg.Sequential, 1, "AAAA"));
            Assert.Equal(0, new FileInfo(host).Length);                 // buffered, no syscall per record
            FileHarness.Open(reg, "B", FileOpenMode.Input);
            Assert.Equal(FileStatusCode.FileAlreadyOpen, reg.Status("B"));
            Assert.Equal(0, new FileInfo(host).Length);
            Assert.Equal("00", FileHarness.Write(reg, "A", FileOrg.Sequential, 2, "BBBB"));
            reg.Close("A");
            Assert.Equal("00", reg.Status("A"));
            Assert.Equal(2 * record, new FileInfo(host).Length);        // CLOSE releases them
        }
        finally { FileHarness.TryDelete(host); }
    }

    /// <summary>The controls of <see cref="AtCobol85_AWriterBuffers_AndARefusedSiblingReleasesNothing"/>: from
    /// 2002 the posture is fixed at OPEN by the sharing mode, so a clause-less OUTPUT (NO OTHER) buffers whether or not a sibling
    /// could exist, and SHARING WITH ALL OTHER releases every record at once, alone or not.</summary>
    [Theory]
    [InlineData(null, 0)]
    [InlineData(FileSharing.AllOther, 1)]
    public void From2002_TheSharingModeFixesWhetherAWriterReleasesAtOnce(FileSharing? sharing, int expectedRecordsInTheFile)
    {
        string host = FileHarness.Tmp($"pb670-lazy-2002-{sharing}");
        try
        {
            var reg = new FileRegistry();
            FileHarness.Register(reg, "A", host, FileOrg.Sequential, sharing: sharing, edition: 2002);
            FileHarness.Open(reg, "A", FileOpenMode.Output);
            Assert.Equal("00", reg.Status("A"));
            Assert.Equal("00", FileHarness.Write(reg, "A", FileOrg.Sequential, 1, "AAAA"));
            Assert.Equal(expectedRecordsInTheFile * FileHarness.Rec(1, "AAAA").Length, new FileInfo(host).Length);
            reg.Close("A");
        }
        finally { FileHarness.TryDelete(host); }
    }
}
