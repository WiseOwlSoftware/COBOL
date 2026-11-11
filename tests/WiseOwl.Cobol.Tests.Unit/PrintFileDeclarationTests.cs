// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using WiseOwl.Cobol.Runtime.IO;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A FILE ONE OF WHOSE WRITES NAMES ADVANCING IS A PRINT FILE FROM ITS OPEN (kb/Work PB5201). §1985:VII.4.7.4 GR16 / §2023:14.9.51.4 GR25: a WRITE
/// without the phrase acts as AFTER ADVANCING 1 LINE, so the plain WRITE that runs BEFORE the file's first ADVANCING write is advanced too, whichever
/// of the program's WRITE statements runs first. The compiler tells the connector at registration (<c>printFile</c>); an EXTERNAL connector the first
/// describing program registered without it is told by a later describer that has one.
/// </summary>
public sealed class PrintFileDeclarationTests
{
    private static string Written(string key, bool firstDescriberPrints, bool secondDescriberPrints, bool external)
    {
        string host = FileHarness.Tmp("prt");
        try
        {
            var reg = new FileRegistry();
            reg.Register(key, host, recordWidth: 3, lineSequential: false, optional: false, varyMin: -1, varyMax: -1, printFile: firstDescriberPrints);
            if (external) reg.Register(key, host, recordWidth: 3, lineSequential: false, optional: false, varyMin: -1, varyMax: -1, printFile: secondDescriberPrints);
            reg.OpenStatic(key, FileOpenMode.Output);
            Assert.Equal("00", reg.Status(key));
            foreach (string image in new[] { "AAA", "BBB" })
                Assert.Equal("00", reg.WriteShared(key, image, -1, FileRecordLock.None, FileRetryKind.None, 0, page: null));
            reg.CloseAll();
            return Encoding.Latin1.GetString(File.ReadAllBytes(host)).Replace("\n", "/");
        }
        finally { FileHarness.TryDelete(host); }
    }

    [Theory]
    [InlineData("F", true, false, false, "/AAA/BBB/")]         // the program names ADVANCING on some WRITE: every plain WRITE advances
    [InlineData("F", false, false, false, "AAABBB")]           // no WRITE names it: nothing positions the file
    [InlineData("::EXT::F", false, true, true, "/AAA/BBB/")]   // a later describer of the EXTERNAL file prints
    [InlineData("::EXT::F", true, false, true, "/AAA/BBB/")]   // the first describer prints; a later one does not undo it
    [InlineData("::EXT::F", false, false, true, "AAABBB")]
    public void APlainWrite_IsTheAutomaticAdvance_WhenAnyWriteOfTheFileNamesAdvancing(string key, bool first, bool second, bool external, string expected) =>
        Assert.Equal(expected, Written(key, first, second, external));
}
