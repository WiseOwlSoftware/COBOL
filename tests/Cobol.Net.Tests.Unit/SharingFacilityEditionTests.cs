// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Runtime.IO;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE FILE SHARING FACILITY IS A COBOL-2002 MODULE, SO AT <c>--std 85</c> NO CONNECTOR RESTRICTS ANOTHER AND NO OPEN
/// ANSWERS '61' (kb/Work PB670, the owner's determination of 2026-10-23, option (i); docs/CONFORMANCE.md DOC-A.1-77 and
/// DOC-A.1-131; <see cref="DialectBehavior.FileSharingFacility"/>).
///
/// <para>The rule: ISO §9.1.15 — <i>"If no specification is made in either location, the implementor defines the sharing
/// mode in which the file is opened; the implementor-defined sharing mode may be one of the modes specified in this Working
/// Draft International Standard or may be a mode completely specified by the implementor"</i> — and §A.4.1, which makes
/// <i>"any associated syntax rules, general rules, other rules, exception conditions, and I-O status values"</i> of an
/// optional module optional with it (file sharing and record locking is Annex A.4.7). A COBOL-85 program is compiled for a
/// processor on which the module is not in effect: the mode its connectors hold restricts nobody, Table 19 (§14.9.27.4) is
/// not asked, and the file sharing conflict condition (§9.1.13.9 item 1) does not exist. GnuCOBOL's measured
/// <c>-std=cobol85</c> behaviour is the same: every OPEN below answers '00'.</para>
///
/// <para>What each test fails on: the first on any pair of opens (every organization, every pair of open modes) that is
/// refused at 85 or admitted by mistake at 2002; the second on a '61' where a process outside the run unit holds the file
/// (a status 1985 does not define: the host's refusal is §9.1.13.6 item 1's '30') and on a file that a refused open
/// affected; the third on a second RUN UNIT being refused by a file lock (there is none at 85); the fourth on a record a
/// connector appended and a sibling's append losing it; the last on the CLOSE and the re-OPEN that follow a refusal.</para>
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

    /// <summary>§9.1.15 with §A.4.1 at COBOL-85: every pair of opens of one physical file by two clause-less connectors
    /// of one run unit is a normal open, in every organization, whatever the two modes — OUTPUT beside a reader and I-O
    /// beside I-O included, the cells Table 19 prints "Unsuccessful open" for (§9.1.13.9 item 1 e)).</summary>
    [Theory]
    [MemberData(nameof(EveryPairOfOpens))]
    public void AtCobol85_EveryPairOfOpensIsANormalOpen(FileOrg org, FileOpenMode existing, FileOpenMode incoming)
    {
        var r = OpenPair(org, existing, incoming, Cobol85);
        Assert.True(r.First == "00" && r.Second == "00",
            $"{org} OPEN {existing} then a second connector's OPEN {incoming} at --std 85 answered '{r.First}' and "
            + $"'{r.Second}'; with the file sharing facility not in effect both are normal opens ('00').");
        Assert.True(r.CloseSecond == "00" && r.CloseFirst == "00",
            $"{org}: the CLOSEs after the pair answered '{r.CloseSecond}' and '{r.CloseFirst}'");
    }

    /// <summary>The control: the same programs at the editions that have the facility answer Table 19's cell — the edition
    /// is the only difference — so a '00' above cannot be a pair the table admits anyway.</summary>
    [Theory]
    [MemberData(nameof(EveryPairOfOpens))]
    public void From2002_TheSamePairIsArbitratedByTable19(FileOrg org, FileOpenMode existing, FileOpenMode incoming)
    {
        var r = OpenPair(org, existing, incoming, 2002);
        Assert.Equal("00", r.First);
        bool refused = Table19.Conflicts((FileRegistry.ImplementorDefaultSharing(incoming, 2002), incoming),
                                         (FileRegistry.ImplementorDefaultSharing(existing, 2002), existing));
        Assert.Equal(refused ? FileStatusCode.FileSharingConflict : "00", r.Second);
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
    /// §9.1.13.9 item 1's '61' (kb/Work PB860); at COBOL-85 '61' is a status the edition does not define, and the host's
    /// refusal is §9.1.13.6 item 1's '30'. §14.9.27.4 GR25 holds at both: the unsuccessful OPEN leaves the file as it
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
                Assert.Equal(FileStatusCode.FileNotOpen, reg.Status("F"));   // §9.1.13.7 item 2: nothing was opened
                Assert.Equal(before, File.ReadAllBytes(host));               // §14.9.27.4 GR25
                // The refusal ends with the handle: the connector's next OPEN succeeds.
                FileHarness.Open(reg, "F", mode);
                Assert.Equal("00", reg.Status("F"));
                reg.Close("F");
            }
            finally { FileHarness.TryDelete(host); }
        }
    }

    /// <summary>The cross-run-unit half of the same fact (§9.1.15's file lock, <c>"thereby preventing other run units
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

    /// <summary>§14.9.27.4 GR18 at COBOL-85, for every organization: <i>"If the OUTPUT phrase is specified, the successful
    /// execution of the OPEN statement creates the file. After the creation of the file, the file contains no records."</i>
    /// A connector that held the file open when another's OPEN OUTPUT created it finds no record at its next READ ('10',
    /// the at end condition), every statement of its answers an I-O status (an OPEN, a READ, a CLOSE: never an escaping
    /// exception from the keyed store engine, which Table 19 used to keep out of this state), and the file holds exactly the
    /// records written after the creation.</summary>
    [Theory]
    [InlineData(FileOrg.Sequential, FileOpenMode.Input)]
    [InlineData(FileOrg.Sequential, FileOpenMode.IO)]
    [InlineData(FileOrg.Relative, FileOpenMode.Input)]
    [InlineData(FileOrg.Relative, FileOpenMode.IO)]
    [InlineData(FileOrg.Indexed, FileOpenMode.Input)]
    [InlineData(FileOrg.Indexed, FileOpenMode.IO)]
    public void AtCobol85_AHolderOfTheFileMeetsAnEmptyFileAfterAnotherConnectorsOutput(FileOrg org, FileOpenMode holder)
    {
        string host = FileHarness.Tmp($"pb670-created-{org}-{holder}");
        try
        {
            FileHarness.Seed(host, org, ["SEED"]);
            var reg = new FileRegistry();
            FileHarness.Register(reg, "A", host, org, sharing: null, access: FileHarness.AccessFor(holder), edition: Cobol85);
            FileHarness.Register(reg, "B", host, org, sharing: null, access: FileHarness.AccessFor(FileOpenMode.Output), edition: Cobol85);
            FileHarness.Open(reg, "A", holder);
            Assert.Equal("00", reg.Status("A"));
            FileHarness.Open(reg, "B", FileOpenMode.Output);
            Assert.Equal("00", reg.Status("B"));
            Assert.Equal("10", FileHarness.ReadNext(reg, "A", out _));   // GR18: the created file has no records
            Assert.Equal("00", FileHarness.Write(reg, "B", org, 1, "NEWB"));
            reg.Close("A");
            Assert.Equal("00", reg.Status("A"));
            reg.Close("B");
            Assert.Equal("00", reg.Status("B"));

            var fresh = new FileRegistry();
            FileHarness.Register(fresh, "R", host, org, sharing: null, access: FileHarness.AccessFor(FileOpenMode.Input), edition: Cobol85);
            FileHarness.Open(fresh, "R", FileOpenMode.Input);
            Assert.Equal("00", fresh.Status("R"));
            var records = new List<string>();
            while (FileHarness.ReadNext(fresh, "R", out string image) == "00") records.Add(image);
            fresh.Close("R");
            Assert.Equal([FileHarness.Rec(1, "NEWB")], records);
        }
        finally { FileHarness.TryDelete(host); }
    }

    /// <summary>§14.9.10.4 GR5 at COBOL-85, the golden <c>pb143_shared_visibility</c> of 2002 without its SHARING clause: a record
    /// one of two clause-less I-O connectors writes or deletes is, at once, written or <i>"logically removed from the physical
    /// file"</i> for the other, because the keyed store is one per physical file whatever the sharing mode.</summary>
    [Theory]
    [InlineData(FileOrg.Relative)]
    [InlineData(FileOrg.Indexed)]
    public void AtCobol85_TwoUpdatersSeeEachOthersWritesAndDeletes(FileOrg org)
    {
        string host = FileHarness.Tmp($"pb670-visible-{org}");
        try
        {
            FileHarness.Seed(host, org, ["ONE"]);
            var reg = new FileRegistry();
            foreach (string name in new[] { "P", "Q" })
                FileHarness.Register(reg, name, host, org, sharing: null, access: FileHarness.AccessFor(FileOpenMode.IO), edition: Cobol85);
            FileHarness.Open(reg, "P", FileOpenMode.IO);
            FileHarness.Open(reg, "Q", FileOpenMode.IO);
            Assert.Equal("00", reg.Status("P"));
            Assert.Equal("00", reg.Status("Q"));
            Assert.Equal("00", FileHarness.Delete(reg, "Q", org, 1));
            Assert.Equal("23", FileHarness.Read(reg, "P", org, 1, out _));   // GR5: no longer accessible through any connector
            Assert.Equal("00", FileHarness.Write(reg, "P", org, 3, "THREE"));
            Assert.Equal("00", FileHarness.Read(reg, "Q", org, 3, out string image));
            Assert.Equal(FileHarness.Rec(3, "THREE"), image);
            reg.Close("P");
            reg.Close("Q");
            Assert.Equal("00", reg.Status("P"));
            Assert.Equal("00", reg.Status("Q"));
        }
        finally { FileHarness.TryDelete(host); }
    }

    /// <summary>Two sequential connectors EXTENDing one physical file at COBOL-85 keep both records (the sharing writers'
    /// append of <c>SharedExtendWriteDriftTests</c> serves every connector there, whatever mode it holds), and the OUTPUT
    /// that follows beside a reader creates the empty file §14.9.27.4 GR18 describes.</summary>
    [Fact]
    public void AtCobol85_TwoExtendingConnectorsKeepBothRecords_AndOutputCreatesTheEmptyFile()
    {
        string host = FileHarness.Tmp("pb670-extend");
        try
        {
            FileHarness.Seed(host, FileOrg.Sequential, ["SEED"]);
            var reg = new FileRegistry();
            foreach (string name in new[] { "A", "B", "R" })
                FileHarness.Register(reg, name, host, FileOrg.Sequential, sharing: null,
                    access: FileHarness.AccessFor(FileOpenMode.Extend), edition: Cobol85);
            FileHarness.Open(reg, "A", FileOpenMode.Extend);
            FileHarness.Open(reg, "B", FileOpenMode.Extend);
            Assert.Equal("00", reg.Status("A"));
            Assert.Equal("00", reg.Status("B"));
            Assert.Equal("00", FileHarness.Write(reg, "A", FileOrg.Sequential, 2, "AAAA"));
            Assert.Equal("00", FileHarness.Write(reg, "B", FileOrg.Sequential, 3, "BBBB"));
            Assert.Equal("00", FileHarness.Write(reg, "A", FileOrg.Sequential, 4, "CCCC"));
            reg.Close("A");
            reg.Close("B");
            Assert.Equal("00", reg.Status("A"));
            Assert.Equal("00", reg.Status("B"));
            Assert.Equal(4 * FileHarness.Rec(1, "SEED").Length, new FileInfo(host).Length);

            FileHarness.Open(reg, "R", FileOpenMode.Input);
            Assert.Equal("00", reg.Status("R"));
            FileHarness.Open(reg, "A", FileOpenMode.Output);
            Assert.Equal("00", reg.Status("A"));   // Table 19 would print "Unsuccessful open" for every OUTPUT request
            reg.Close("A");
            reg.Close("R");
            Assert.Equal(0, new FileInfo(host).Length);
        }
        finally { FileHarness.TryDelete(host); }
    }
}
