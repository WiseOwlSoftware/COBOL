// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ AN EMBEDDED PRECOMPILER BLOCK IS REFUSED BY NAME, ONCE PER BLOCK, AND THE PARSE GOES ON AFTER IT (kb/Work PB4341).
/// ISO/IEC 1989 defines no EXEC construct: <c>EXEC SQL … END-EXEC</c> is the input of a vendor's precompiler, which ISO
/// §4.2.14 lets an implementation require ("This permits an implementation to require components outside the COBOL
/// implementation, such as precompilers"), and this implementation has none. The block drew
/// <c>COBOL0001: cannot parse construct near 'SQL'</c>, the message of a typo, and a block in WORKING-STORAGE or at the
/// head of a paragraph's sentences failed in a decision so high in the parse that its recovery lost the rest of the
/// program. <c>CobolErrorStrategy</c> now refuses the block with COBOL5818 at its EXEC: at a decision where the parse
/// cannot read the block as COBOL (consumed there, with a period after it the parse cannot take), and at a syntax error
/// inside a block the decision read (the report covers the block, and the recovery consumes it whole). Like
/// <see cref="ParseRecoveryDeletionTests"/>, these assert the COUNT.
/// </summary>
public sealed class EmbeddedPrecompilerBlockTests
{
    private const string Code = "COBOL5818";

    /// <summary>A block where a sentence, a statement or an entry would stand: one COBOL5818 at the EXEC, naming the
    /// embedded language, and nothing else. The DELETE row is a word of the SQL text that is also a COBOL verb, which
    /// must not resume the parse as a statement; the MOVE row is the second arm (the block read as the receiving list
    /// until the SQL text's SELECT failed).</summary>
    [Theory]
    [InlineData("EXEC SQL SELECT A INTO :W-A FROM T END-EXEC.", "SQL")]
    [InlineData("EXEC SQL DELETE FROM T WHERE C = 'Q' END-EXEC.", "SQL")]
    [InlineData("EXEC SQL COMMIT END-EXEC DISPLAY W-A.", "SQL")]
    [InlineData("EXEC CICS RETURN END-EXEC.", "CICS")]
    [InlineData("IF W-N = 1 EXEC SQL COMMIT END-EXEC END-IF.", "SQL")]
    [InlineData("MOVE W-A TO EXEC SQL SELECT A INTO :W-B FROM T END-EXEC.", "SQL")]
    public void AnEmbeddedBlock_IsTheOneDiagnostic(string statements, string language)
    {
        var errors = RecoverySources.Errors(RecoverySources.Program(statements));
        Assert.True(errors.Count == 1, $"expected exactly one diagnostic, got:\n{RecoverySources.Listing(errors)}");
        Assert.Contains($"{Code}: EXEC {language}: ISO/IEC 1989 defines no EXEC construct.", errors[0]);
    }

    /// <summary>Every block of a program is reported, in WORKING-STORAGE and in the procedure division, a run of blocks
    /// one after another included, and a fault of the program's own after them is still reported.</summary>
    [Fact]
    public void EveryBlock_AndTheFaultAfterThem_AreEachReported()
    {
        string ws = "EXEC SQL INCLUDE SQLCA END-EXEC.\nEXEC SQL BEGIN DECLARE SECTION END-EXEC.\n01 W-H PIC X(4).\n"
                    + "EXEC SQL END DECLARE SECTION END-EXEC.";
        var errors = RecoverySources.Errors(RecoverySources.Program(
            "EXEC SQL SELECT A INTO :W-H FROM T END-EXEC\nDISPLAY W-H.\nMOVE 1 W-N.", ws));
        Assert.True(errors.Count == 5, $"expected four blocks and the MOVE, got:\n{RecoverySources.Listing(errors)}");
        Assert.All(errors.Take(4), e => Assert.Contains($"{Code}:", e));
        Assert.Contains("'W-N'", errors[4]);
    }

    /// <summary>EXEC, SQL and END-EXEC are user-defined words (no edition's §8.9 list reserves them): where the parse
    /// reads the text as COBOL, nothing is refused, and the program compiles and runs.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void ExecNamingDataItems_Compiles(int edition)
    {
        string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PBEXECU.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 EXEC PIC X VALUE "E".
            01 SQL PIC X VALUE "S".
            01 END-EXEC PIC X VALUE "X".
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY EXEC SQL END-EXEC.
                STOP RUN.

            """;
        string dir = ScratchDirectory.Create("WiseOwlCobol_PBEXEC_");
        try
        {
            string src = Path.Combine(dir, "pbexec.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "pbexec.dll"), DialectLevel: edition, CheckOnly: true, SourceFormat: InitialReferenceFormat.Free));
            Assert.True(r.Success, $"EXEC, SQL and END-EXEC are user words here, got:\n{string.Join("\n", r.Errors)}");
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
