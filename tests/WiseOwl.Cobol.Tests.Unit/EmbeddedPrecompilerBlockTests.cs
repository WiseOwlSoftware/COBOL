// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
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
/// cannot read the block as COBOL, the block becomes one token (<c>EmbeddedBlockReading</c>), a statement where a
/// statement may begin and nothing elsewhere, and the pass is run again so every prediction sees it; at a syntax error
/// inside a block the decision read, the report covers the block and the recovery consumes it whole. Like
/// <see cref="ParseRecoveryDeletionTests"/>, these assert the COUNT. Where EXEC, SQL and END-EXEC are the program's own
/// data items, nothing is refused: the conformance case <c>pb4341-exec-is-a-user-word</c> compiles and runs that program
/// at all four editions.
/// </summary>
public sealed class EmbeddedPrecompilerBlockTests
{
    private const string Code = "COBOL5818";

    /// <summary>A block where a sentence, a statement or an entry would stand: one COBOL5818 at the EXEC, naming the
    /// embedded language, and nothing else. The DELETE row is a word of the SQL text that is also a COBOL verb, which
    /// must not resume the parse as a statement; the MOVE row is the second arm (the block read as the receiving list
    /// until the SQL text's SELECT failed). ⛔ The rows from the IF ... ELSE on are a block that is the only statement of a
    /// construct's branch (kb/Work PB4341, review R1-1), which the recovery consumed as nothing, so the construct lacked its
    /// statement (ISO §14.9.19.3 1)) and drew a false COBOL0306 at END-IF, a false COBOL2073 at WHEN OTHER (after
    /// <c>WHEN 1</c>, whose prediction read "1 IS EXEC" as a class condition), or lost the blocks after it.</summary>
    [Theory]
    [InlineData("EXEC SQL SELECT A INTO :W-A FROM T END-EXEC.", "SQL")]
    [InlineData("EXEC SQL DELETE FROM T WHERE C = 'Q' END-EXEC.", "SQL")]
    [InlineData("EXEC SQL COMMIT END-EXEC DISPLAY W-A.", "SQL")]
    [InlineData("EXEC CICS RETURN END-EXEC.", "CICS")]
    [InlineData("IF W-N = 1 EXEC SQL COMMIT END-EXEC END-IF.", "SQL")]
    [InlineData("MOVE W-A TO EXEC SQL SELECT A INTO :W-B FROM T END-EXEC.", "SQL")]
    [InlineData("IF W-N = 1 EXEC SQL OPEN C1 END-EXEC ELSE DISPLAY W-A END-IF.", "SQL")]
    [InlineData("IF W-N = 1 DISPLAY W-A ELSE EXEC SQL OPEN C1 END-EXEC END-IF.", "SQL")]
    [InlineData("EVALUATE W-N WHEN 1 EXEC SQL OPEN C1 END-EXEC WHEN OTHER DISPLAY W-A END-EVALUATE.", "SQL")]
    [InlineData("EVALUATE TRUE WHEN W-N = 1 EXEC SQL OPEN C1 END-EXEC WHEN OTHER DISPLAY W-A END-EVALUATE.", "SQL")]
    [InlineData("EVALUATE W-N WHEN 1 DISPLAY W-A WHEN OTHER EXEC SQL ROLLBACK END-EXEC END-EVALUATE.", "SQL")]
    [InlineData("PERFORM UNTIL W-N > 3 EXEC SQL FETCH C1 INTO :W-A END-EXEC END-PERFORM.", "SQL")]
    [InlineData("READ F1 AT END EXEC SQL CLOSE C1 END-EXEC END-READ.", "SQL")]
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

    /// <summary>⛔ The blocks of both branches of an IF, the block after it and the program's own fault after them are each
    /// reported, in source order (kb/Work PB4341, review R1-1: the ELSE block, the COMMIT block and the MOVE fault were all
    /// lost, the one diagnostic left being the first block's). The commonest embedded-SQL shapes, IF SQLCODE = 0 COMMIT
    /// ELSE ROLLBACK and EVALUATE SQLCODE, are this one.</summary>
    [Fact]
    public void TheBlocksOfBothBranches_AndEverythingAfterThem_AreEachReported()
    {
        var errors = RecoverySources.Errors(RecoverySources.Program(
            "IF W-N = 1 EXEC SQL COMMIT END-EXEC ELSE EXEC SQL ROLLBACK END-EXEC END-IF.\nDISPLAY W-A.\n"
            + "EXEC SQL COMMIT END-EXEC.\nMOVE 1 W-N."));
        Assert.True(errors.Count == 4, $"expected three blocks and the MOVE, got:\n{RecoverySources.Listing(errors)}");
        Assert.All(errors.Take(3), e => Assert.Contains($"{Code}:", e));
        Assert.Contains("'W-N'", errors[3]);
    }

    /// <summary>⛔ A block read as one token stays one when the §8.9 reservation gate parses the source again (kb/Work
    /// PB655: at COBOL-85, ALLOCATE is a user word this program declares, so the first pass's tokens are retyped and the
    /// source is parsed again): the block is reported once, by the pass that describes the source.</summary>
    [Fact]
    public void AReparseOfTheGate_ReportsTheBlockOnce()
    {
        var errors = RecoverySources.Errors(RecoverySources.Program(
            "MOVE W-A TO ALLOCATE.\nIF W-N = 1 EXEC SQL COMMIT END-EXEC END-IF.", "01 ALLOCATE PIC X(10)."), edition: 85);
        Assert.True(errors.Count == 1, $"expected exactly one diagnostic, got:\n{RecoverySources.Listing(errors)}");
        Assert.Contains($"{Code}:", errors[0]);
    }

    /// <summary>⛔ A source that writes EXEC as a COBOL word anywhere (here a data item, which no edition's §8.9 list
    /// reserves) is read as COBOL throughout, as the standard reads it: no EXEC of it opens a block, so the text after
    /// <c>EXEC SQL</c> draws the COBOL reading's own diagnostics and no COBOL5818. Where every EXEC opens a block, each is
    /// one (the rows above). The program whose EXEC, SQL and END-EXEC are data items compiles and runs at all four
    /// editions: the conformance case <c>pb4341-exec-is-a-user-word</c>.</summary>
    [Fact]
    public void ASourceThatWritesExecAsAWord_HasNoBlock()
    {
        var errors = RecoverySources.Errors(RecoverySources.Program("EXEC SQL COMMIT END-EXEC.", "01 EXEC PIC X."));
        Assert.True(errors.Count > 0 && !errors.Any(e => e.Contains($"{Code}:")),
            $"expected the COBOL reading's diagnostics and no {Code}, got:\n{RecoverySources.Listing(errors)}");
    }

    /// <summary>⛔ The period of a qualified SQL name is text of the block, not the end of its sentence (kb/Work PB4341,
    /// review R1-2): "The COBOL character period, when followed by a space, is a separator" (ISO §8.3.5 rule 3), and the
    /// block's scan ended at ANY period, so <c>T.A</c> or <c>:W-REC.W-X</c> left the block unrecognized, with the PB4341
    /// symptom (COBOL0001 near 'SQL') and the rest of the program lost. Each block is now one COBOL5818, the MOVE fault
    /// after them is reported, and nothing else: the separator screen skips the block's text (another language, whose
    /// <c>T.A</c> and <c>(A,B)</c> drew COBOL2632 and COBOL2630 as if they were COBOL), as the literal, PICTURE and
    /// word-character screens do (<c>ProgramText</c>).</summary>
    [Fact]
    public void AQualifiedSqlName_StaysInsideItsBlock()
    {
        var errors = RecoverySources.Errors(RecoverySources.Program(
            "EXEC SQL SELECT T.A INTO :W-A FROM T END-EXEC.\nDISPLAY W-A.\n"
            + "EXEC SQL INSERT INTO S.T (A,B) VALUES (:W-REC.W-X,'Q') END-EXEC.\nMOVE 1 W-N."));
        Assert.True(errors.Count == 3, $"expected two blocks and the MOVE, got:\n{RecoverySources.Listing(errors)}");
        Assert.All(errors.Take(2), e => Assert.Contains($"{Code}:", e));
        Assert.Contains("'W-N'", errors[2]);
    }
}
