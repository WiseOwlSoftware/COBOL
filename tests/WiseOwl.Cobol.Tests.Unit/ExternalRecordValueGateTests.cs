// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>kb/Work PB5072 (review finding R1-4): the COBOL-85 VALUE-under-EXTERNAL gate asks of the RECORD, and the record
/// is decided once per level-01 or level-77 entry as the walk reaches it, never by a backward scan of the section per VALUE
/// clause. §1985:VI.5.15.3 SR4: "The VALUE clause must not be specified in any entry which is part of the description or
/// redefinition of an external data record". This pins every transition of that per-walk state: a subordinate entry of an
/// external record draws the gate, a level-01 REDEFINES of it draws the gate, and the records that follow (a plain level-01
/// record and a level-77 item) close the external record and draw nothing.</summary>
public sealed class ExternalRecordValueGateTests
{
    private const string Source =
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB5072R.\n       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n"
        + "       01  EXT-REC EXTERNAL.\n           05  EXT-FLD PIC X VALUE \"A\".\n"
        + "       01  RED-REC REDEFINES EXT-REC.\n           05  RED-FLD PIC X VALUE \"B\".\n"
        + "       01  LOC-REC.\n           05  LOC-FLD PIC X VALUE \"C\".\n"
        + "       77  LOC-77 PIC X VALUE \"D\".\n"
        + "       PROCEDURE DIVISION.\n       MAIN-PARA.\n           DISPLAY EXT-FLD LOC-FLD LOC-77.\n           STOP RUN.\n";

    [Theory]
    [InlineData(85, 2)]
    [InlineData(2002, 0)]
    [InlineData(2023, 0)]
    public void TheGateFollowsTheRecord_NotTheEntry(int dialect, int expected)
    {
        string dir = ScratchDirectory.Create("WiseOwlCobol_PB5072R_");
        try
        {
            string src = Path.Combine(dir, "pb5072r.cob");
            File.WriteAllText(src, Source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "pb5072r.dll"), DialectLevel: dialect, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            Assert.Equal(expected, r.Errors.Count(e => e.Contains("COBOL0900") && e.Contains("an EXTERNAL data item")));
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
