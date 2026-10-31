// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A TOKEN THE MISSING ELEMENT WOULD LET FOLLOW IS NOT DELETED (kb/Work PB4133). ANTLR's single-token deletion drops
/// the token at a required element when the token after it is what the rule expects, so a statement written where a
/// condition, a selection object or a key is required read as a stray word: <c>IF DISPLAY "X" END-IF</c> lost the
/// DISPLAY, took <c>"X"</c> for the condition and drew, after the true COBOL0001, a false COBOL2072 for the empty block
/// the deletion had left; <c>MERGE SW ON ASCENDING KEY USING F1 F2 GIVING F3</c> took F1 for the key and drew a second
/// COBOL0001 (COBOL5506 since PB3428) at the GIVING it met where USING was due. The strategy now declines the deletion when the
/// token may follow the element (<c>CobolErrorStrategy.TheMissingElementLetsItFollow</c>: ANTLR's own insertion test
/// for a token, the block's end for a block, the rule's follow for a rule that has consumed nothing), so the element
/// is the one fault and the rest of the statement parses as written. Like <see cref="ParseRecoveryTailTests"/>, these
/// assert the COUNT.
/// </summary>
public sealed class ParseRecoveryDeletionTests
{
    /// <summary>The four rows of kb/Work PB4133 (the PBGF04, 07, 10 and 32 programs of the general-format sweep) and the
    /// statements that share their shape: a statement, or the next phrase of the same statement, where a header
    /// operand is required.</summary>
    [Theory]
    [InlineData("IF DISPLAY \"X\" END-IF", "DISPLAY")]
    [InlineData("IF DISPLAY \"X\" ELSE DISPLAY \"Y\" END-IF", "DISPLAY")]
    [InlineData("IF W-N = 1 AND DISPLAY \"X\" END-IF", "DISPLAY")]
    [InlineData("IF MOVE 1 TO W-N END-IF", "MOVE")]
    [InlineData("EVALUATE W-N WHEN DISPLAY \"X\" END-EVALUATE", "DISPLAY")]
    [InlineData("EVALUATE W-N WHEN DISPLAY \"X\" WHEN OTHER DISPLAY \"Y\" END-EVALUATE", "DISPLAY")]
    [InlineData("EVALUATE DISPLAY \"X\" WHEN 1 DISPLAY \"Y\" END-EVALUATE", "DISPLAY")]
    [InlineData("PERFORM UNTIL DISPLAY \"X\" END-PERFORM", "DISPLAY")]
    [InlineData("MERGE SW ON ASCENDING KEY USING F1 F2 GIVING F3", "USING")]
    public void AWordWhereAnOperandIsRequired_IsTheOneFaultNotAStrayWord(string statement, string word)
    {
        var errors = RecoverySources.Errors(RecoverySources.Program(statement));
        Assert.True(errors.Count == 1, $"expected exactly one diagnostic, got:\n{RecoverySources.Listing(errors)}");
        Assert.Contains($"'{word}'", errors[0]);
    }

    /// <summary>The repair leaves the statement as written, so the next fault is a fault of its own and is reported.</summary>
    [Fact]
    public void ASecondFaultAfterTheDeclinedDeletion_IsStillReported()
    {
        var errors = RecoverySources.Errors(RecoverySources.Program("IF DISPLAY \"X\" END-IF. MOVE 1 W-N."));
        Assert.True(errors.Count == 2, $"expected two diagnostics, got:\n{RecoverySources.Listing(errors)}");
        Assert.Contains("'DISPLAY'", errors[0]);
        Assert.Contains("'W-N'", errors[1]);
    }

    /// <summary>A token that no element may be followed by is still deleted: a repeated TO or USING is the one fault,
    /// and the statement parses on past it.</summary>
    [Theory]
    [InlineData("MOVE 1 TO TO W-N.", "TO")]
    [InlineData("ADD 1 TO TO W-N.", "TO")]
    [InlineData("IF W-N = 1 THEN THEN DISPLAY \"A\" END-IF.", "THEN")]
    [InlineData("CALL \"P\" USING USING W-A.", "USING")]
    public void ARepeatedWord_IsStillDeleted(string statement, string word)
    {
        var errors = RecoverySources.Errors(RecoverySources.Program(statement + " MOVE 1 W-N."));
        Assert.True(errors.Count == 2, $"expected the repeated word and the MOVE, got:\n{RecoverySources.Listing(errors)}");
        Assert.Contains($"'{word}'", errors[0]);
        Assert.Contains("'W-N'", errors[1]);
    }
}
