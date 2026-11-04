// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>kb/Work PB4577 (review R1-2): the COBOL0900 gate on a subscripted SEARCH identifier-1 quotes the operand as the
/// programmer wrote it (<c>WrittenSource.Of</c>), never the token concatenation <c>GetText()</c> yields
/// (<c>HOFG(GIHI)</c> for <c>H OF G (GI HI)</c>). ISO §14.9.37.3 SR2 and SR3 admit the enclosing tables' subscripts from
/// COBOL-2014; the 2002 text refuses any subscript on identifier-1.</summary>
public sealed class SearchIdentifier1GateTests
{
    private const string Source =
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB4577W.\n       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n"
        + "       01  T-VALS PIC X(6) VALUE \"123789\".\n       01  T REDEFINES T-VALS.\n"
        + "           05  G OCCURS 2 TIMES INDEXED BY GI.\n           10  H PIC 9 OCCURS 3 TIMES ASCENDING KEY IS H INDEXED BY HI.\n"
        + "       PROCEDURE DIVISION.\n       MAIN-PARA.\n           SET GI TO 2.\n           SET HI TO 1.\n"
        + "           SEARCH H OF G (GI)\n               AT END DISPLAY \"NOT FOUND\"\n               WHEN H (GI, HI) > 7 DISPLAY \"FOUND\"\n"
        + "           END-SEARCH.\n           STOP RUN.\n";

    [Fact]
    public void ASubscriptedSearchIdentifier1_BelowTheIntroduction_IsQuotedAsWritten()
    {
        string dir = ScratchDirectory.Create("WiseOwlCobol_PB4577W_");
        try
        {
            string src = Path.Combine(dir, "pb4577w.cob");
            File.WriteAllText(src, Source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "pb4577w.dll"), DialectLevel: 85, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            var gate = Assert.Single(r.Errors, e => e.Contains("COBOL0900"));
            Assert.Contains("SEARCH H OF G (GI): identifier-1 is subscripted", gate);
            Assert.DoesNotContain("HOFG", gate);
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
