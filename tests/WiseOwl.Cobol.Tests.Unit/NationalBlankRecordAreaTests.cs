// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime.IO;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// kb/Work PB1880 item 5 (the PB679 sibling of <see cref="IndexedNationalKeyPadTests"/>): the BLANK RECORD AREA an
/// unsuccessful READ hands back, for the RELATIVE and the record SEQUENTIAL connectors. ISO §14.9.30.4 GR18 makes the
/// content of the record area undefined after an unsuccessful READ, so this is WiseOwl COBOL's determination
/// (docs/FILES_DESIGN.md D30: "the blank area an unsuccessful READ returns (<c>BlankRecordArea</c>)" is padded
/// through the one <c>FileConnector.FitRecord</c>), and the determination is that a NATIONAL record area (§14.9.30.4 GR15:
/// "specified implicitly or explicitly as national") is blank with the NATIONAL space — the two bytes 00 20 (D-N1) —
/// never with a string of U+2020 positions. The image is internal to the connector (the generated READ leaves the record
/// area alone on an unsuccessful READ, which the <c>pb1880_national_pad_record_sequential_and_relative</c> golden
/// cannot observe), so these are runtime-level tests like their indexed sibling. Each organization has the alphanumeric
/// control (the area keeps the 0x20 space) and each unsuccessful status the organization can answer: the invalid key
/// '23' (relative), AT END '10' (sequential and relative) and the not-open '47'.
/// </summary>
public sealed class NationalBlankRecordAreaTests
{
    private const int Dynamic = 2;   // KeyedAccess.Dynamic
    private const string NationalBlank = "\0 \0 \0 \0 ";    // four national positions, 00 20 each
    private const string AlphanumericBlank = "        ";    // eight alphanumeric positions

    private static (FileRegistry reg, string host) Relative(bool national)
    {
        var reg = new FileRegistry();
        string host = FileHarness.Tmp("pb1880nb");
        reg.RegisterRelative("X", host, 8, false, Dynamic, 4, -1, -1);
        if (national) reg.RegisterNationalArea("X");
        reg.OpenStatic("X", FileOpenMode.Output);
        reg.SetRelativeKey("X", 1);
        Assert.Equal("00", reg.WriteKeyed("X", "ABCDEFGH", -1));
        reg.Close("X");
        reg.OpenStatic("X", FileOpenMode.IO);
        return (reg, host);
    }

    private static (FileRegistry reg, string host) SequentialOpenedOnOneRecord(bool national)
    {
        var reg = new FileRegistry();
        string host = FileHarness.Tmp("pb1880nb");
        reg.Register("S", host, 8, lineSequential: false, optional: false, -1, -1);
        if (national) reg.RegisterNationalArea("S");
        reg.OpenStatic("S", FileOpenMode.Output);
        reg.Write("S", "ABCDEFGH", -1, null);
        Assert.Equal("00", reg.Status("S"));
        reg.Close("S");
        reg.OpenStatic("S", FileOpenMode.Input);
        return (reg, host);
    }

    [Theory]
    [InlineData(true, NationalBlank)]
    [InlineData(false, AlphanumericBlank)]
    public void Relative_InvalidKey_BlankAreaIsTheAreasOwnSpace(bool national, string expected)
    {
        var (reg, host) = Relative(national);
        try
        {
            reg.SetRelativeKey("X", 2);
            Assert.Equal("23", reg.ReadKeyed("X", -1, "", out string image));
            Assert.Equal(expected, image);
        }
        finally { reg.Close("X"); FileHarness.TryDelete(host); }
    }

    [Theory]
    [InlineData(true, NationalBlank)]
    [InlineData(false, AlphanumericBlank)]
    public void Relative_AtEnd_BlankAreaIsTheAreasOwnSpace(bool national, string expected)
    {
        var (reg, host) = Relative(national);
        try
        {
            Assert.Equal("00", reg.ReadKeyedNext("X", out string first));
            Assert.Equal("ABCDEFGH", first);
            Assert.Equal("10", reg.ReadKeyedNext("X", out string image));   // past the only record: §14.9.30.4 GR24
            Assert.Equal(expected, image);
        }
        finally { reg.Close("X"); FileHarness.TryDelete(host); }
    }

    [Theory]
    [InlineData(true, NationalBlank)]
    [InlineData(false, AlphanumericBlank)]
    public void Sequential_AtEnd_BlankAreaIsTheAreasOwnSpace(bool national, string expected)
    {
        var (reg, host) = SequentialOpenedOnOneRecord(national);
        try
        {
            Assert.Equal("00", reg.ReadShared("S", false, FileRecordLock.None, false, false, FileRetryKind.None, 0, out string first));
            Assert.Equal("ABCDEFGH", first);
            Assert.Equal("10", reg.ReadShared("S", false, FileRecordLock.None, false, false, FileRetryKind.None, 0, out string image));
            Assert.Equal(expected, image);
        }
        finally { reg.Close("S"); FileHarness.TryDelete(host); }
    }

    [Theory]
    [InlineData(true, NationalBlank)]
    [InlineData(false, AlphanumericBlank)]
    public void Sequential_ReadOfAFileThatIsNotOpen_BlankAreaIsTheAreasOwnSpace(bool national, string expected)
    {
        var reg = new FileRegistry();
        string host = FileHarness.Tmp("pb1880nb");
        reg.Register("S", host, 8, lineSequential: false, optional: false, -1, -1);
        if (national) reg.RegisterNationalArea("S");
        Assert.Equal("47", reg.ReadShared("S", false, FileRecordLock.None, false, false, FileRetryKind.None, 0, out string image));   // §14.9.30.4 GR2
        Assert.Equal(expected, image);
    }
}
