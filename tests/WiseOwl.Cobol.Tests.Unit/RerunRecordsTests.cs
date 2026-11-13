// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime.IO;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE X3.23-1985 RERUN CLAUSE'S INTEGER-1 RECORDS FORM, at the connector (§1985:VII.2.12.4 GR2 d: the rerun information
/// is written on the device of implementor-name-1 "whenever integer-1 records of file-name-2 have been processed"; kb/Work
/// PB5198). The conformance case <c>pb5198_rerun_records_device_85</c> holds the program-level success path; this holds
/// what a program cannot reach: a statement that did not complete is not a record processed, the count belongs to the OPEN,
/// and a device the host refuses is the file's own I-O status (§2023:9.1.13.6 item 1, '30'), the record the statement
/// wrote staying written.
/// </summary>
public sealed class RerunRecordsTests
{
    private const string File = "F";

    /// <summary>The registry over a scratch directory, file F sequential with 4-character records and a rerun point every
    /// <paramref name="every"/> records onto <c>rr.dev</c> in the directory.</summary>
    private static (FileRegistry Reg, string Data, string Device) Sequential(string dir, long every, IHostMedium? medium = null)
    {
        var reg = medium is null ? new FileRegistry() : new FileRegistry { Medium = medium };
        string data = Path.Combine(dir, "data.dat"), device = Path.Combine(dir, "rr.dev");
        reg.Register(File, data, recordWidth: 4, lineSequential: false, optional: false, varyMin: -1, varyMax: -1);
        reg.RegisterRerunRecords(File, every, device, File);
        return (reg, data, device);
    }

    private static string Write(FileRegistry reg, string record)
    {
        reg.WriteShared(File, record, -1, FileRecordLock.None, FileRetryKind.None, 0, page: null);
        return reg.Status(File);
    }

    private static string Read(FileRegistry reg) =>
        reg.ReadShared(File, previous: false, FileRecordLock.None, advancingOnLock: false, ignoringLock: false, FileRetryKind.None, 0, out _);

    [Fact]
    public void EveryNthRecordProcessed_AppendsOneLine_CountedFromTheOpen()
    {
        string dir = ScratchDirectory.Create("pb5198-");
        try
        {
            var (reg, _, device) = Sequential(dir, every: 2);
            reg.OpenStatic(File, FileOpenMode.Output);
            for (int i = 0; i < 5; i++) Assert.Equal("00", Write(reg, "ABCD"));
            reg.Close(File);
            Assert.Equal("RERUN F 2\nRERUN F 4\n", System.IO.File.ReadAllText(device));

            reg.OpenStatic(File, FileOpenMode.Input);   // a new OPEN starts the count again
            for (int i = 0; i < 3; i++) Assert.Equal("00", Read(reg));
            reg.Close(File);
            Assert.Equal("RERUN F 2\nRERUN F 4\nRERUN F 2\n", System.IO.File.ReadAllText(device));
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    [Fact]
    public void AStatementThatDidNotComplete_IsNotARecordProcessed()
    {
        string dir = ScratchDirectory.Create("pb5198-");
        try
        {
            var (reg, data, device) = Sequential(dir, every: 1);
            System.IO.File.WriteAllText(data, "ABCD");
            reg.OpenStatic(File, FileOpenMode.Input);
            Assert.Equal("48", Write(reg, "WXYZ"));          // §2023:9.1.13.7 item 8: not open for output
            Assert.False(System.IO.File.Exists(device));
            Assert.Equal("00", Read(reg));
            Assert.Equal("10", Read(reg));                    // the end of the file is no record
            Assert.Equal("RERUN F 1\n", System.IO.File.ReadAllText(device));
            reg.Close(File);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    [Fact]
    public void ARelativeFile_CountsItsWriteRewriteAndDelete()
    {
        string dir = ScratchDirectory.Create("pb5198-");
        try
        {
            var reg = new FileRegistry();
            string device = Path.Combine(dir, "rr.dev");
            reg.RegisterRelative("R", Path.Combine(dir, "r.dat"), 4, false, (int)KeyedAccess.Random, 4, -1, -1);
            reg.RegisterRerunRecords("R", 1, device, "R");
            reg.OpenStatic("R", FileOpenMode.Output);   // creates the file; the I-O OPEN below needs it
            reg.Close("R");
            reg.OpenStatic("R", FileOpenMode.IO);
            reg.SetRelativeKey("R", 1);
            Assert.Equal("00", reg.WriteKeyed("R", "ABCD", -1));
            Assert.Equal("00", reg.RewriteKeyed("R", "WXYZ", -1));
            Assert.Equal("00", reg.DeleteRecord("R", "WXYZ"));
            Assert.Equal("23", reg.DeleteRecord("R", "WXYZ"));   // not there: no record processed
            reg.Close("R");
            Assert.Equal("RERUN R 1\nRERUN R 2\nRERUN R 3\n", System.IO.File.ReadAllText(device));
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    [Fact]
    public void AnIndexedFile_CountsItsWriteRewriteAndDelete()
    {
        string dir = ScratchDirectory.Create("pb5198-");
        try
        {
            var reg = new FileRegistry();
            string device = Path.Combine(dir, "rr.dev");
            reg.RegisterIndexed("I", Path.Combine(dir, "i.dat"), 8, false, (int)KeyedAccess.Random, [new(0, 4)], -1, -1);
            reg.RegisterRerunRecords("I", 1, device, "I");
            reg.OpenStatic("I", FileOpenMode.Output);
            reg.Close("I");
            reg.OpenStatic("I", FileOpenMode.IO);
            Assert.Equal("00", reg.WriteKeyed("I", "KEY1ABCD", -1));
            Assert.Equal("22", reg.WriteKeyed("I", "KEY1ABCD", -1));   // duplicate key: no record processed
            Assert.Equal("00", reg.RewriteKeyed("I", "KEY1WXYZ", -1));
            Assert.Equal("00", reg.DeleteRecord("I", "KEY1WXYZ"));
            reg.Close("I");
            Assert.Equal("RERUN I 1\nRERUN I 2\nRERUN I 3\n", System.IO.File.ReadAllText(device));
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>A device the host refuses is told to the program as the file's I-O status, and the record the statement
    /// wrote is written.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ARefusedDevice_IsTheFilesIoStatus30(bool unauthorized)
    {
        string dir = ScratchDirectory.Create("pb5198-");
        try
        {
            var medium = new FaultHostMedium
            {
                BeforeOperation = op =>
                {
                    if (!op.Contains("rr.dev", StringComparison.Ordinal)) return;
                    throw unauthorized ? new UnauthorizedAccessException("refused") : new IOException("refused");
                },
            };
            var (reg, data, device) = Sequential(dir, every: 1, medium);
            reg.OpenStatic(File, FileOpenMode.Output);
            Assert.Equal("30", Write(reg, "ABCD"));
            Assert.False(System.IO.File.Exists(device));
            reg.Close(File);
            Assert.Equal("ABCD", System.IO.File.ReadAllText(data));
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
