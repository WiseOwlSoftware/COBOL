// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ ONE FAULT, ONE DIAGNOSTIC, AND EVERY LATER FAULT STILL REPORTED (kb/Work PB4299, PB4382, PB4300;
/// <c>CobolErrorStrategy</c>). Three ways the recovery after a syntax error used to cost the user a true diagnostic or
/// add a false one:
/// <list type="bullet">
/// <item>a user-defined word in the faulted sentence's tail, not followed by a period, stopped the resynchronization as
/// a possible paragraph header (ISO §14.4.3: "A paragraph consists of a paragraph-name followed by a separator period"),
/// and the procedure division's own recovery then swallowed every later sentence, its faults with it (PB4299);</item>
/// <item>a fault caught by the rule of a statement an explicit scope terminator closes ended that rule, and its END-x
/// met no open statement, a false COBOL0306 for a statement the source holds (§14.5.3.2; PB4382, PB4299 R10);</item>
/// <item>a WHEN OTHER phrase written in its place drew COBOL2073 "at most once, and only after every WHEN phrase" where
/// the fault was before it: a statement missing its operand, or a WHEN group with no statement (§14.9.13.2; PB4300,
/// PB4382).</item>
/// </list>
/// Each row asserts the WHOLE list of diagnostics, in order: its code and the word it names.
/// </summary>
public sealed class ParseRecoveryCascadeTests
{
    private static void AssertDiagnostics(string statements, params string[] codeAtWord)
    {
        var errors = RecoverySources.Errors(RecoverySources.Program(statements));
        Assert.True(errors.Count == codeAtWord.Length,
            $"expected {codeAtWord.Length} diagnostic(s) ({string.Join(", ", codeAtWord)}), got:\n{string.Join("\n", errors)}");
        for (int i = 0; i < codeAtWord.Length; i++)
        {
            var (code, word) = (codeAtWord[i].Split('@')[0], codeAtWord[i].Split('@')[1]);
            Assert.True(errors[i].Contains(code, StringComparison.Ordinal) && errors[i].Contains($"'{word}'", StringComparison.Ordinal),
                $"diagnostic {i + 1} should be {code} at '{word}', got:\n{string.Join("\n", errors)}");
        }
    }

    /// <summary>PB4299: the faulted sentence's tail holds a user-defined word that no period follows; the sentences after
    /// it are parsed afresh, so the MOVE that omits its TO (ISO §14.9.25.2) is the second diagnostic.</summary>
    [Theory]
    [InlineData("IF W-N = 1 THEN ADD TO W-N END-IF. MOVE 1 W-N.")]                       // G4/V2
    [InlineData("IF W-N = 1 THEN ADD TO W-N END-IF. DISPLAY \"X\" \"Y\". MOVE 1 W-N.")]  // G4/V18: a clean sentence between
    [InlineData("IF W-N = 1 THEN ADD TO W-N END-IF. DISPLAY W-K. MOVE 1 W-N.")]          // G4/V19, G4-refute/R3
    [InlineData("ADD TO W-N W-K. MOVE 1 W-N.")]                                            // G4-refute/R14: `W-K.` is no header
    [InlineData("IF W-N = 1 THEN ADD TO \"X\" END-IF. MOVE 1 W-N.")]                     // G4-refute/R15: no word in the tail
    public void AWordInTheFaultsTail_IsNoParagraphHeader_AndTheNextFaultIsReported(string statements)
        => AssertDiagnostics(statements, "COBOL5506@TO", "COBOL5506@W-N");

    /// <summary>Both faults of one sentence and the next sentence's are reported, and no END-x is orphaned.</summary>
    [Theory]
    // G4-refute/R10: the ADD's own fault, then the ELSE branch's MOVE, then the next sentence's; the END-IF is the IF's.
    [InlineData("IF W-N = 1 THEN ADD TO \"X\" ELSE MOVE 1 W-N END-IF. MOVE 2 W-N.", "COBOL5506@TO", "COBOL5506@W-N", "COBOL5506@W-N")]
    // G4-refute/R11: two faults of one sentence (cobc reports both).
    [InlineData("ADD TO \"X\" MOVE 1 W-N. MOVE 2 W-N.", "COBOL5506@TO", "COBOL5506@W-N", "COBOL5506@W-N")]
    // A fault in an IF's condition ends the IF's rule; its END-IF is still the IF's own.
    [InlineData("IF W-N NOT <> 1 DISPLAY \"C\" END-IF. MOVE 1 W-N.", "COBOL0001@<>", "COBOL5506@W-N")]
    // A terminator an open statement owns is never taken for the left one's: the second IF's empty block is its own fault.
    [InlineData("IF W-N NOT <> 1 DISPLAY \"C\" END-IF IF W-K = 1 END-IF. MOVE 1 W-N.", "COBOL0001@<>", "COBOL2072@END-IF", "COBOL5506@W-N")]
    // Two IFs left by two faults: both END-IFs are theirs, and the third IF's empty block is still its own fault.
    [InlineData("IF W-N NOT <> 1 IF W-K NOT <> 2 DISPLAY \"C\" END-IF END-IF IF W-K = 1 END-IF. MOVE 1 W-N.",
                "COBOL0001@<>", "COBOL0001@<>", "COBOL2072@END-IF", "COBOL5506@W-N")]
    public void ATerminatorOfAStatementTheRecoveryLeft_IsThatStatementsOwn(string statements, params string[] diagnostics)
        => AssertDiagnostics(statements, diagnostics);

    /// <summary>PB4382: a branch with no statement is one COBOL2072 naming that branch's statement, and the construct's
    /// terminator is its own.</summary>
    [Theory]
    [InlineData("IF W-N = 1 ELSE DISPLAY W-A END-IF. MOVE 1 W-N.", "COBOL2072@ELSE", "COBOL5506@W-N")]          // e1.cob
    [InlineData("IF W-N = 1 THEN ELSE DISPLAY W-A END-IF. MOVE 1 W-N.", "COBOL2072@ELSE", "COBOL5506@W-N")]
    [InlineData("IF W-N = 1 IF W-K = 1 ELSE DISPLAY W-A END-IF END-IF. MOVE 1 W-N.", "COBOL2072@ELSE", "COBOL5506@W-N")]
    [InlineData("PERFORM WHEN EC-ALL DISPLAY \"B\" END-PERFORM. MOVE 1 W-N.", "COBOL2072@WHEN", "COBOL5506@W-N")]
    // e2.cob: WHEN 1 has no statement; the WHEN OTHER after it is in its place.
    [InlineData("EVALUATE W-N WHEN 1 WHEN OTHER DISPLAY W-A END-EVALUATE. MOVE 1 W-N.", "COBOL2072@OTHER", "COBOL5506@W-N")]
    [InlineData("EVALUATE W-N WHEN 1 DISPLAY W-A WHEN 2 EVALUATE W-K WHEN 1 WHEN OTHER DISPLAY W-A END-EVALUATE "
                + "WHEN OTHER DISPLAY W-B END-EVALUATE. MOVE 1 W-N.", "COBOL2072@OTHER", "COBOL5506@W-N")]
    public void ABranchWithNoStatement_IsOneCOBOL2072_AndItsConstructCloses(string statements, params string[] diagnostics)
        => AssertDiagnostics(statements, diagnostics);

    /// <summary>PB4382, review R1-1: a fault reported AT a statement's own terminator (the empty block it ends) leaves no
    /// record of that statement, so a terminator met later in the sentence is a stray one (ISO §14.5.3.2 1)): COBOL0306.</summary>
    [Theory]
    [InlineData("IF W-N = 1 END-IF DISPLAY \"X\" END-IF. MOVE 1 W-N.", "COBOL2072@END-IF", "COBOL0306@END-IF", "COBOL5506@W-N")]
    [InlineData("IF W-N = 1 END-IF IF W-K = 2 DISPLAY \"X\" END-IF END-IF. MOVE 1 W-N.", "COBOL2072@END-IF", "COBOL0306@END-IF", "COBOL5506@W-N")]
    public void ATerminatorAfterTheFaultAtTheStatementsOwn_IsStray(string statements, params string[] diagnostics)
        => AssertDiagnostics(statements, diagnostics);

    /// <summary>PB4382, review R2-1: an empty IF inside another statement faults at its own END-IF, which the IF takes
    /// (ISO §14.5.3.2 1)); the ENCLOSING statement's own terminator is its own, not a stray one. The enclosing IF is left
    /// too (the prediction ended its block before the malformed one), so a terminator beyond it is the stray.</summary>
    [Theory]
    [InlineData("IF W-N = 1 IF W-K = 2 END-IF END-IF.", "COBOL2072@END-IF")]
    [InlineData("IF W-N = 1 DISPLAY \"A\" IF W-K = 2 END-IF DISPLAY \"B\" END-IF. MOVE 1 W-N.", "COBOL2072@END-IF", "COBOL5506@W-N")]
    [InlineData("IF W-N = 1 IF W-K = 2 END-IF DISPLAY \"Y\" END-IF DISPLAY \"X\" END-IF. MOVE 1 W-N.", "COBOL2072@END-IF", "COBOL0306@END-IF", "COBOL5506@W-N")]
    [InlineData("IF W-N = 1 IF W-K = 2 IF W-N = 3 END-IF END-IF END-IF END-IF. MOVE 1 W-N.", "COBOL2072@END-IF", "COBOL0306@END-IF", "COBOL5506@W-N")]
    [InlineData("IF W-N = 1 DISPLAY \"A\" ELSE IF W-K = 2 END-IF DISPLAY \"B\" END-IF DISPLAY \"C\". MOVE 1 W-N.", "COBOL2072@END-IF", "COBOL5506@W-N")]
    public void AnEmptyIfInsideAnotherStatement_TakesItsOwnTerminator(string statements, params string[] diagnostics)
        => AssertDiagnostics(statements, diagnostics);

    /// <summary>PB4382, review R2-2: a statement the recovery left is terminated with the statement ENCLOSING it, by that
    /// statement's terminator (ISO §14.5.3.2 2)), so a terminator of its kind met after that is a stray one: COBOL0306.</summary>
    [Theory]
    [InlineData("PERFORM UNTIL W-N = 2 IF W-N = 1 END-PERFORM DISPLAY \"X\" END-IF. MOVE 1 W-N.", "COBOL2072@END-PERFORM", "COBOL0306@END-IF", "COBOL5506@W-N")]
    [InlineData("EVALUATE W-N WHEN 1 IF W-N = 1 END-EVALUATE DISPLAY \"X\" END-IF. MOVE 1 W-N.", "COBOL2072@END-EVALUATE", "COBOL0306@END-IF", "COBOL5506@W-N")]
    [InlineData("PERFORM UNTIL W-N = 2 IF W-N = 1 ELSE DISPLAY \"A\" END-PERFORM DISPLAY \"X\" END-IF. MOVE 1 W-N.", "COBOL2072@ELSE", "COBOL0306@END-IF", "COBOL5506@W-N")]
    // The enclosing statement's end does not end an IF that encloses IT.
    [InlineData("IF W-N = 1 PERFORM UNTIL W-N = 2 IF W-K = 3 END-PERFORM DISPLAY \"X\" END-IF. MOVE 1 W-N.", "COBOL2072@END-PERFORM", "COBOL5506@W-N")]
    public void AStatementTheEnclosingTerminatorEnded_IsNotLeftForALaterTerminator(string statements, params string[] diagnostics)
        => AssertDiagnostics(statements, diagnostics);

    /// <summary>PB4438, PB4436: the terminator a recovery consumed as a left statement's own ends the recovery, so a stray
    /// terminator written directly after it is met by the parse and drawn COBOL0306 (ISO §14.5.3.2 1)), and the
    /// statement enclosing an empty IF takes its own END-IF without the recovery swallowing it, so the true stray later in
    /// the sentence is drawn too.</summary>
    [Theory]
    // PB4438: the empty IF's own END-IF, then a stray one directly after it.
    [InlineData("IF W-N = 1 END-IF END-IF. MOVE 1 W-N.", "COBOL2072@END-IF", "COBOL0306@END-IF", "COBOL5506@W-N")]
    [InlineData("IF W-N = 1 DISPLAY \"A\". IF W-N = 2 END-IF END-IF. MOVE 1 W-N.", "COBOL2072@END-IF", "COBOL0306@END-IF", "COBOL5506@W-N")]
    [InlineData("PERFORM UNTIL W-N = 2 END-PERFORM END-PERFORM. MOVE 1 W-N.", "COBOL2072@END-PERFORM", "COBOL0306@END-PERFORM", "COBOL5506@W-N")]
    // The same through the loop recovery: a fault in the condition leaves the IF, whose END-IF the recovery takes.
    [InlineData("IF W-N NOT <> 1 DISPLAY \"C\" END-IF END-IF. MOVE 1 W-N.", "COBOL0001@<>", "COBOL0306@END-IF", "COBOL5506@W-N")]
    // PB4436: the empty inner IF takes its own END-IF, the enclosing IF's follows directly, and the stray one is last.
    [InlineData("IF W-N = 1 DISPLAY \"A\" IF W-K = 2 END-IF END-IF DISPLAY \"X\" END-IF. MOVE 1 W-N.",
                "COBOL2072@END-IF", "COBOL0306@END-IF", "COBOL5506@W-N")]
    [InlineData("IF W-N = 1 DISPLAY \"A\" IF W-K = 2 END-IF END-IF END-IF. MOVE 1 W-N.", "COBOL2072@END-IF", "COBOL0306@END-IF", "COBOL5506@W-N")]
    public void ATerminatorDirectlyAfterOneTheRecoveryTook_IsStray_AndTheNextFaultIsReported(string statements, params string[] diagnostics)
        => AssertDiagnostics(statements, diagnostics);

    /// <summary>PB4437: an IF that is the first statement of a WHEN group (EVALUATE, SEARCH) and faults at its ELSE is left
    /// by the recovery, and the EVALUATE or SEARCH ends before the statement after it. The IF's own END-IF, and the
    /// EVALUATE's or SEARCH's END-x after the statements the group's block could not take, are theirs (ISO §14.5.3.2 1)):
    /// not COBOL0306. A terminator beyond them is the stray one.</summary>
    [Theory]
    [InlineData("EVALUATE W-N WHEN 1 IF W-K = 1 ELSE DISPLAY W-A END-IF DISPLAY \"X\" END-EVALUATE. MOVE 1 W-N.", "COBOL2072@ELSE", "COBOL5506@W-N")]
    [InlineData("SEARCH W-A WHEN W-N = 1 IF W-K = 1 ELSE DISPLAY W-A END-IF END-SEARCH. MOVE 1 W-N.", "COBOL2072@ELSE", "COBOL5506@W-N")]
    [InlineData("EVALUATE W-N WHEN 1 DISPLAY \"Q\" WHEN 2 IF W-K = 1 ELSE DISPLAY W-A END-IF DISPLAY \"X\" END-EVALUATE. MOVE 1 W-N.",
                "COBOL2072@ELSE", "COBOL5506@W-N")]
    [InlineData("EVALUATE W-N WHEN 1 IF W-K = 1 ELSE DISPLAY W-A END-IF END-IF DISPLAY \"X\" END-EVALUATE. MOVE 1 W-N.",
                "COBOL2072@ELSE", "COBOL0306@END-IF", "COBOL5506@W-N")]
    [InlineData("EVALUATE W-N WHEN 1 IF W-K = 1 ELSE DISPLAY W-A END-IF DISPLAY \"X\" END-EVALUATE END-IF. MOVE 1 W-N.",
                "COBOL2072@ELSE", "COBOL0306@END-IF", "COBOL5506@W-N")]
    [InlineData("EVALUATE W-N WHEN 1 IF W-K = 1 ELSE DISPLAY W-A END-IF DISPLAY \"X\" END-EVALUATE END-EVALUATE. MOVE 1 W-N.",
                "COBOL2072@ELSE", "COBOL0306@END-EVALUATE", "COBOL5506@W-N")]
    public void AnIfFirstInAWhenGroupFaultingAtElse_KeepsItsOwnEndIf(string statements, params string[] diagnostics)
        => AssertDiagnostics(statements, diagnostics);

    /// <summary>PB4300: COBOL2073 is drawn by a WHEN OTHER phrase that repeats one or stands before every WHEN group,
    /// never by the one phrase written in its place after an incomplete statement.</summary>
    [Theory]
    // G4/i14, G4/S10: the statement before the only WHEN OTHER misses its operand; no misplaced phrase is named.
    [InlineData("EVALUATE TRUE WHEN W-N = 2 DISPLAY WHEN OTHER DISPLAY \"O\" END-EVALUATE. MOVE 1 W-N.", "COBOL5506@WHEN", "COBOL5506@W-N")]
    [InlineData("EVALUATE W-N WHEN 1 MOVE 1 TO WHEN OTHER DISPLAY \"Y\" END-EVALUATE. MOVE 1 W-N.", "COBOL5506@WHEN", "COBOL5506@W-N")]
    [InlineData("PERFORM DISPLAY \"A\" WHEN EC-ALL DISPLAY WHEN OTHER DISPLAY \"C\" END-PERFORM. MOVE 1 W-N.", "COBOL5506@WHEN", "COBOL5506@W-N")]
    // A missing operand inside a nested EVALUATE that stands in the outer one's WHEN OTHER: the inner phrase is in place.
    [InlineData("EVALUATE W-N WHEN 1 DISPLAY W-A WHEN OTHER EVALUATE W-K WHEN 1 DISPLAY WHEN OTHER DISPLAY \"Y\" END-EVALUATE "
                + "END-EVALUATE. MOVE 1 W-N.", "COBOL5506@WHEN", "COBOL5506@W-N")]
    public void TheOnlyWhenOther_AfterAnIncompleteStatement_IsNoRepeat(string statements, params string[] diagnostics)
        => AssertDiagnostics(statements, diagnostics);

    /// <summary>PB4300: a true repeat and a phrase before every WHEN group draw COBOL2073, whatever the line layout (the
    /// G4/S11 and W2 probes that drew COBOL2269 ran past column 72 of fixed form, where `WHEN OTHER` was cut to `WHEN
    /// OTH`: the layout dependence was the probes').</summary>
    [Theory]
    [InlineData("EVALUATE W-N WHEN 1 DISPLAY \"X\" WHEN OTHER DISPLAY \"Y\" WHEN OTHER DISPLAY \"Z\" END-EVALUATE. MOVE 1 W-N.", "WHEN")]
    [InlineData("EVALUATE W-N WHEN OTHER DISPLAY \"Y\" WHEN 1 DISPLAY \"Z\" END-EVALUATE. MOVE 1 W-N.", "OTHER")]
    [InlineData("PERFORM DISPLAY \"A\" WHEN EC-ALL DISPLAY \"B\" WHEN OTHER DISPLAY \"C\" WHEN OTHER DISPLAY \"D\" END-PERFORM. "
                + "MOVE 1 W-N.", "OTHER")]
    public void ARepeatedOrLeadingWhenOther_IsCOBOL2073(string statements, string word)
        => AssertDiagnostics(statements, $"COBOL2073@{word}", "COBOL5506@W-N");
}
