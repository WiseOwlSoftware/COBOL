// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime.IO;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ OPEN INPUT … REVERSED IS IGNORED ON A MEDIUM THAT CANNOT BE POSITIONED (kb/Work PB5199). §1985:VII.4.3.4 GR10: "The NO REWIND and
/// REVERSED phrases will be ignored if they do not apply to the storage medium on which the file resides." A FIFO cannot deliver its
/// records in reversed order (§1985:p.XVII-94 item 14), so the file reads forward with status 00, never the permanent error 30 of a
/// backward READ the stream cannot seek for. A COBOL program cannot make a FIFO, so the case runs here over the fault-injecting medium's
/// non-seekable read handle; the control is the same file on a medium that can be positioned, read last record first.
/// </summary>
public sealed class ReversedUnpositionableMediumTests
{
    private static List<string> ReadAll(FileRegistry reg, out string endStatus)
    {
        var records = new List<string>();
        string status;
        while ((status = reg.ReadShared("F", previous: false, FileRecordLock.None, advancingOnLock: false, ignoringLock: false,
                   FileRetryKind.None, 0, out string image)) is "00")
            records.Add(image);
        endStatus = status;
        return records;
    }

    [Theory]
    [InlineData(false, true, "REC1,REC2,REC3", "10")]    // a medium that cannot be positioned: the phrase is ignored
    [InlineData(false, false, "REC1,REC2,REC3", "10")]   // (control) the same medium, no phrase
    [InlineData(true, false, "REC1,REC2,REC3", "10")]    // (control) a positionable medium, no phrase
    [InlineData(true, true, "REC3,REC2,REC1", "10")]     // (control) a positionable medium honours the phrase
    public void Reversed_AppliesOnlyWhereTheMediumCanBePositioned(bool positionable, bool reversed, string expected, string endStatus)
    {
        string host = FileHarness.Tmp("rev");
        try
        {
            File.WriteAllBytes(host, "REC1REC2REC3"u8.ToArray());
            var reg = new FileRegistry { Medium = new FaultHostMedium { NonSeekableReads = !positionable } };
            reg.Register("F", host, recordWidth: 4, lineSequential: false, optional: false, varyMin: -1, varyMax: -1, edition: 1985);
            reg.OpenTapeStatic("F", FileOpenMode.Input, reversed ? OpenTapePhrase.Reversed : OpenTapePhrase.None);
            Assert.Equal("00", reg.Status("F"));
            var records = ReadAll(reg, out string end);
            Assert.Equal(expected, string.Join(',', records));
            Assert.Equal(endStatus, end);
            reg.Close("F");
        }
        finally { FileHarness.TryDelete(host); }
    }
}
