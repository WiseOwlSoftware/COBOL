// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>kb/Work PB4577 (review R1-2), PB4609: the COBOL0900 gate on a subscripted SEARCH identifier-1 quotes the operand as the
/// programmer wrote it (<c>WrittenSource.Of</c>), never the token concatenation <c>GetText()</c> yields
/// (<c>HOFG(GIHI)</c> for <c>H OF G (GI HI)</c>). ISO §2023:14.9.37.3 SR2 and SR3 admit the enclosing tables' subscripts from
/// COBOL-2023; the 2002 text, the 2009 committee draft and the 2014 FCD 1.0 refuse any subscript on identifier-1, so every
/// edition below 2023 (85, 2002, 2014) draws the gate.</summary>
public sealed class SearchIdentifier1GateTests
{
    private const string Source =
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB4577W.\n       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n"
        + "       01  T-VALS PIC X(6) VALUE \"123789\".\n       01  T REDEFINES T-VALS.\n"
        + "           05  G OCCURS 2 TIMES INDEXED BY GI.\n           10  H PIC 9 OCCURS 3 TIMES ASCENDING KEY IS H INDEXED BY HI.\n"
        + "       PROCEDURE DIVISION.\n       MAIN-PARA.\n           SET GI TO 2.\n           SET HI TO 1.\n"
        + "           SEARCH H OF G (GI)\n               AT END DISPLAY \"NOT FOUND\"\n               WHEN H (GI, HI) > 7 DISPLAY \"FOUND\"\n"
        + "           END-SEARCH.\n           STOP RUN.\n";

    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    public void ASubscriptedSearchIdentifier1_BelowTheIntroduction_IsQuotedAsWritten(int dialect)
    {
        string dir = ScratchDirectory.Create("WiseOwlCobol_PB4577W_");
        try
        {
            string src = Path.Combine(dir, "pb4577w.cob");
            File.WriteAllText(src, Source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "pb4577w.dll"), DialectLevel: dialect, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            var gate = Assert.Single(r.Errors, e => e.Contains("COBOL0900"));
            Assert.Contains("SEARCH H OF G (GI): identifier-1 is subscripted", gate);
            Assert.DoesNotContain("HOFG", gate);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    // kb/Work PB4609 (review R1-1, R1-2): the COBOL2075 advice names the edge the gate above enforces. A subscript the SEARCH
    // supplies itself (SR2) is written on identifier-1 from COBOL-2023 only, so below it the advice offers the WHEN phrases alone.
    private const string TableSource =
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB4609A.\n       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n"
        + "       01  T.\n           05  E PIC X OCCURS 3 TIMES INDEXED BY IX.\n"
        + "       PROCEDURE DIVISION.\n       MAIN-PARA.\n           SEARCH E (IX)\n               AT END CONTINUE\n"
        + "               WHEN E (IX) = \"A\" CONTINUE\n           END-SEARCH.\n           STOP RUN.\n";

    private const string DynamicSource =
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB4609B.\n       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n"
        + "       01  G.\n           05  GR OCCURS 2 TIMES.\n"
        + "               10  EP PIC X OCCURS DYNAMIC CAPACITY IN ECAP INDEXED BY EX.\n       01  J PIC 9 VALUE 1.\n"
        + "       PROCEDURE DIVISION.\n       MAIN-PARA.\n           SEARCH EP\n               AT END CONTINUE\n"
        + "               WHEN J = 5 CONTINUE\n           END-SEARCH.\n           STOP RUN.\n";

    [Theory]
    [InlineData(TableSource, 85, false)]
    [InlineData(TableSource, 2002, false)]
    [InlineData(TableSource, 2014, false)]
    [InlineData(TableSource, 2023, true)]
    [InlineData(DynamicSource, 2014, false)]
    [InlineData(DynamicSource, 2023, true)]
    public void TheCobol2075Advice_OffersIdentifier1SubscriptsOnlyFrom2023(string source, int dialect, bool offersSubscripts)
    {
        string dir = ScratchDirectory.Create("WiseOwlCobol_PB4609A_");
        try
        {
            string src = Path.Combine(dir, "pb4609a.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "pb4609a.dll"), DialectLevel: dialect, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            string advice = Assert.Single(r.Errors, e => e.Contains("COBOL2075"));
            Assert.Equal(offersSubscripts, advice.Contains("may be written here") || advice.Contains("Write the enclosing subscripts on identifier-1"));
            Assert.Equal(!offersSubscripts, advice.Contains("COBOL-2023 first lets identifier-1 write"));
            Assert.DoesNotContain("COBOL-2014 first lets", advice);
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
