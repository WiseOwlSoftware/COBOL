// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.IO;
using System.Linq;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// A diagnostic that quotes a QUALIFIED procedure-name quotes it as written, <c>DP1 OF D1</c>, never the glued
/// <c>DP1OFD1</c> of <c>GetText()</c> (kb/Work PB4277; the reader is <c>ProcedureNameShape.AsWritten</c> and
/// <see cref="ProcedureNameReaderDriftTests"/> refuses the next <c>GetText()</c>). The two declaratives-boundary rules are
/// ISO §14.9.49.3 SR4 (an error) and SR3 (a warning), and RESUME AT's target rule is §14.9.33.3 SR3; each message
/// names the target the program wrote.
/// </summary>
public sealed class ProcedureNameMessageTests
{
    private const string Sr4Error = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PB4277A.
        PROCEDURE DIVISION.
        DECLARATIVES.
        D1 SECTION.
            USE AFTER STANDARD EXCEPTION PROCEDURE ON INPUT.
        DP1.
            DISPLAY "X".
        END DECLARATIVES.
        S1 SECTION.
        MAIN-P.
            GO TO DP1 OF D1.
            STOP RUN.
        """;

    private const string Sr3Warning = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PB4277B.
        PROCEDURE DIVISION.
        DECLARATIVES.
        D1 SECTION.
            USE AFTER STANDARD EXCEPTION PROCEDURE ON INPUT.
        DP1.
            GO TO MAIN-P OF S1.
            DISPLAY "X".
        END DECLARATIVES.
        S1 SECTION.
        MAIN-P.
            STOP RUN.
        """;

    private const string ResumeIntoDeclarative = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PB4277C.
        PROCEDURE DIVISION.
        DECLARATIVES.
        D1 SECTION.
            USE AFTER EXCEPTION CONDITION EC-ALL.
        DP1.
            RESUME AT DP2 OF D1.
        DP2.
            DISPLAY "X".
        END DECLARATIVES.
        S1 SECTION.
        MAIN-P.
            STOP RUN.
        """;

    [Theory]
    [InlineData(Sr4Error, "COBOL2376", true, "GO TO 'DP1 OF D1': 'DP1 OF D1' is a procedure of declarative section 'D1'")]
    [InlineData(Sr3Warning, "COBOL2375", false, "GO TO 'MAIN-P OF S1': 'MAIN-P OF S1' is in the nondeclarative portion")]
    [InlineData(ResumeIntoDeclarative, "COBOL0714", true, "RESUME AT 'DP2 OF D1': the procedure shall be in the nondeclarative portion")]
    public void AQualifiedProcedureNameIsQuotedAsWritten(string source, string code, bool isError, string expectedText)
    {
        string dir = ScratchDirectory.Create("WiseOwlCobol_PB4277_");
        try
        {
            string src = Path.Combine(dir, "pn.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(src, DialectLevel: 2023, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            var line = (isError ? r.Errors : r.Warnings).Single(l => l.Contains(code, StringComparison.Ordinal));
            Assert.Contains(expectedText, line, StringComparison.Ordinal);
            Assert.DoesNotContain("OFD1", line, StringComparison.Ordinal);
            Assert.DoesNotContain("OFS1", line, StringComparison.Ordinal);
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
