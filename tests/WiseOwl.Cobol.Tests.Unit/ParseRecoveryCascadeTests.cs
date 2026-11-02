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

    /// <summary>PB4448: a statement that begins the next statement of a block and whose own parse dies later (an IF whose
    /// statement-1 is missing) is refused where it dies, COBOL2072 (ISO §5.2.6.3: "the syntax element contained within
    /// the braces or one of the alternatives contained within the braces shall be explicitly specified"), never at its
    /// first token as a word "not expected here": the IF begins a statement where the block stands, so the block of an
    /// inline PERFORM, the one that ENDS the construct before it, is not the place that refuses it.</summary>
    [Theory]
    [InlineData("PERFORM UNTIL W-N = 2 DISPLAY \"A\" IF W-K = 2 END-IF END-IF END-PERFORM. MOVE 1 W-N.",
                "COBOL2072@END-IF", "COBOL0306@END-IF", "COBOL5506@W-N")]
    [InlineData("PERFORM UNTIL W-N = 2 DISPLAY \"A\" IF W-K = 2 END-IF END-PERFORM. MOVE 1 W-N.", "COBOL2072@END-IF", "COBOL5506@W-N")]
    [InlineData("PERFORM 3 TIMES DISPLAY \"A\" IF W-K = 2 END-IF END-PERFORM. MOVE 1 W-N.", "COBOL2072@END-IF", "COBOL5506@W-N")]
    [InlineData("PERFORM DISPLAY \"A\" WHEN EC-ALL DISPLAY \"B\" IF W-K = 2 END-IF END-PERFORM. MOVE 1 W-N.", "COBOL2072@END-IF", "COBOL5506@W-N")]
    [InlineData("PERFORM UNTIL W-N = 2 DISPLAY \"A\" EVALUATE W-N WHEN 1 END-EVALUATE END-PERFORM. MOVE 1 W-N.",
                "COBOL2072@END-EVALUATE", "COBOL5506@W-N")]
    [InlineData("PERFORM UNTIL W-N = 2 DISPLAY \"A\" IF W-K = 2 ELSE DISPLAY \"B\" END-IF END-PERFORM. MOVE 1 W-N.",
                "COBOL2072@ELSE", "COBOL5506@W-N")]
    public void AStatementThatDiesAfterAnotherInABlock_IsRefusedWhereItDies(string statements, params string[] diagnostics)
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

    /// <summary>PB4447: a statement whose own optional terminator would match the one a left statement is due (an outer IF's
    /// END-IF, which every statement inside it may be followed by) never takes it: the END-IF belongs to "the
    /// most-recently preceding unterminated statement" with that name (ISO §14.5.3.2 1)), the left inner IF, which began
    /// after the open one. The statements between go on as the source wrote them: the EVALUATE (or SEARCH, or ADD with
    /// an ON SIZE ERROR phrase) whose block the IF began holds the DISPLAY after it and ends in its own terminator, so that
    /// terminator is no stray one, and the END-IF after it is the outer IF's. The ELSE of the left IF is its own phrase
    /// word (§14.5.3.3 4)), never the outer IF's: the nearest unterminated IF it may be associated with is the left one.</summary>
    [Theory]
    [InlineData("IF W-N = 1 EVALUATE W-N WHEN 1 IF W-K = 1 ELSE DISPLAY W-A END-IF DISPLAY \"X\" END-EVALUATE END-IF. MOVE 1 W-N.",
                "COBOL2072@ELSE", "COBOL5506@W-N")]
    [InlineData("IF W-N = 1 EVALUATE W-N WHEN 1 IF W-K = 1 ELSE DISPLAY W-A END-IF DISPLAY \"X\" END-EVALUATE. MOVE 1 W-N.",
                "COBOL2072@ELSE", "COBOL5506@W-N")]
    [InlineData("IF W-N = 1 SEARCH W-A WHEN W-N = 1 IF W-K = 1 ELSE DISPLAY W-A END-IF DISPLAY \"X\" END-SEARCH END-IF. MOVE 1 W-N.",
                "COBOL2072@ELSE", "COBOL5506@W-N")]
    [InlineData("IF W-N = 1 ADD 1 TO W-K ON SIZE ERROR IF W-K = 1 ELSE DISPLAY W-A END-IF DISPLAY \"X\" END-ADD END-IF. MOVE 1 W-N.",
                "COBOL2072@ELSE", "COBOL5506@W-N")]
    [InlineData("IF W-N = 1 DISPLAY \"Q\" ELSE EVALUATE W-N WHEN 1 IF W-K = 1 ELSE DISPLAY W-A END-IF DISPLAY \"X\" END-EVALUATE END-IF. "
                + "MOVE 1 W-N.", "COBOL2072@ELSE", "COBOL5506@W-N")]
    [InlineData("IF W-N = 1 IF W-K = 2 EVALUATE W-N WHEN 1 IF W-K = 1 ELSE DISPLAY W-A END-IF DISPLAY \"X\" END-EVALUATE END-IF END-IF. "
                + "MOVE 1 W-N.", "COBOL2072@ELSE", "COBOL5506@W-N")]
    // The true strays are still drawn, after the statements that own their terminators.
    [InlineData("IF W-N = 1 EVALUATE W-N WHEN 1 IF W-K = 1 ELSE DISPLAY W-A END-IF DISPLAY \"X\" END-EVALUATE END-EVALUATE END-IF. "
                + "MOVE 1 W-N.", "COBOL2072@ELSE", "COBOL0306@END-EVALUATE", "COBOL5506@W-N")]
    [InlineData("IF W-N = 1 EVALUATE W-N WHEN 1 IF W-K = 1 ELSE DISPLAY W-A END-IF DISPLAY \"X\" END-EVALUATE END-IF END-IF. "
                + "MOVE 1 W-N.", "COBOL2072@ELSE", "COBOL0306@END-IF", "COBOL5506@W-N")]
    // The left IF's ELSE phrase is its own, not the outer IF's, whose ELSE then follows its own block.
    [InlineData("IF W-N = 1 IF W-K = 1 ELSE DISPLAY W-A END-IF ELSE DISPLAY \"X\" END-IF. MOVE 1 W-N.", "COBOL2072@ELSE", "COBOL5506@W-N")]
    public void ATerminatorAnOlderOpenStatementCouldTake_IsTheLeftStatementsOwn(string statements, params string[] diagnostics)
        => AssertDiagnostics(statements, diagnostics);

    /// <summary>PB4447 review R1-1: "the next phrase of any containing statement" implicitly terminates a conditional statement
    /// contained within it (ISO §14.5.3.3 4) b)), so the statement the recovery left, which has taken the one phrase its
    /// format permits, is ended by the next phrase of the open statement around it: the END-IF after that phrase is the
    /// open statement's own, never the left statement's, and a second END-IF is the true stray (COBOL0306), as it is where
    /// the parse did not leave the inner IF. The phrase the left statement's format still permits is its own, and its
    /// END-IF is its own.</summary>
    [Theory]
    // The inner IF has its ELSE; the second ELSE is the outer IF's, and the second END-IF the stray one.
    [InlineData("IF W-N = 1 IF W-K = 2 ELSE DISPLAY 'X' ELSE DISPLAY 'Y' END-IF END-IF. MOVE 1 W-N.",
                "COBOL2072@ELSE", "COBOL0306@END-IF", "COBOL5506@W-N")]
    // The WHEN of the open EVALUATE ends the left IF.
    [InlineData("IF W-N = 1 EVALUATE W-N WHEN 1 IF W-K = 2 ELSE DISPLAY 'X' WHEN 2 DISPLAY 'Y' END-IF END-IF. MOVE 1 W-N.",
                "COBOL2072@ELSE", "COBOL0306@END-IF", "COBOL5506@W-N")]
    // Whole programs: the left IF's END-IF is the left IF's, and the outer statements end in their own terminators.
    [InlineData("IF W-N = 1 IF W-K = 2 ELSE DISPLAY 'X' ELSE DISPLAY 'Y' END-IF. MOVE 1 W-N.", "COBOL2072@ELSE", "COBOL5506@W-N")]
    [InlineData("IF W-N = 1 EVALUATE W-N WHEN 1 IF W-K = 2 ELSE DISPLAY 'X' WHEN 2 DISPLAY 'Y' END-EVALUATE END-IF. MOVE 1 W-N.",
                "COBOL2072@ELSE", "COBOL5506@W-N")]
    // The condition's fault leaves the inner IF with no phrase yet: the ELSE is its own (the nearest IF it may be associated
    // with, §14.5.3.3 4)), so both END-IFs are written for the two IFs.
    [InlineData("IF W-N = 1 IF W-K NOT <> 1 DISPLAY 'C' ELSE DISPLAY 'D' END-IF END-IF. MOVE 1 W-N.", "COBOL0001@<>", "COBOL5506@W-N")]
    public void TheNextPhraseOfAContainingStatement_EndsTheStatementTheRecoveryLeft(string statements, params string[] diagnostics)
        => AssertDiagnostics(statements, diagnostics);

    /// <summary>PB4447, sibling: a fault deep inside a statement ends the lists around it early, not only the nearest
    /// one. The prediction that died inside the IF C ended the IF A before the IF B that holds it, so the IF A is left too
    /// (ISO §14.5.3.2 1)): its ELSE phrase and its END-IF are its own (§14.5.3.3 4)), where they drew a COBOL0001 and a
    /// false COBOL0306.</summary>
    [Theory]
    [InlineData("IF W-N = 1 DISPLAY \"Z\" IF W-K = 2 IF W-K = 1 ELSE DISPLAY W-A END-IF END-IF ELSE DISPLAY \"Y\" END-IF. MOVE 1 W-N.",
                "COBOL2072@ELSE", "COBOL5506@W-N")]
    [InlineData("IF W-N = 1 DISPLAY \"Z\" IF W-K = 2 IF W-K = 1 ELSE DISPLAY W-A END-IF DISPLAY \"Q\" END-IF ELSE DISPLAY \"Y\" END-IF. "
                + "MOVE 1 W-N.", "COBOL2072@ELSE", "COBOL5506@W-N")]
    [InlineData("IF W-N = 1 DISPLAY \"Z\" IF W-K = 2 PERFORM UNTIL W-N = 2 IF W-K = 1 ELSE DISPLAY W-A END-IF END-PERFORM END-IF "
                + "ELSE DISPLAY \"Y\" END-IF. MOVE 1 W-N.", "COBOL2072@ELSE", "COBOL5506@W-N")]
    [InlineData("IF W-N = 1 DISPLAY \"Z\" IF W-K = 2 EVALUATE W-N WHEN 1 IF W-K = 1 ELSE DISPLAY W-A END-IF DISPLAY \"X\" END-EVALUATE "
                + "END-IF ELSE DISPLAY \"Y\" END-IF. MOVE 1 W-N.", "COBOL2072@ELSE", "COBOL5506@W-N")]
    public void AFaultDeepInsideAStatement_LeavesEveryStatementItEndedEarly(string statements, params string[] diagnostics)
        => AssertDiagnostics(statements, diagnostics);

    /// <summary>PB4436 review R1-1: the ELSE, WHEN or WHEN OTHER that follows the terminator a recovery consumed belongs to the
    /// statement the recovery left, whose block the prediction ended before the malformed IF (ISO §14.5.3.3 4): "Any phrase
    /// encountered is the next phrase of the most-recently preceding unterminated statement with which that phrase may be
    /// syntactically associated"; the IF's statement-1 and statement-2, the WHEN groups of one EVALUATE or SEARCH, are the
    /// phrases of one statement). The one fault was reported; the word draws no COBOL0001 or COBOL2073 of its own, and a
    /// stray terminator after the statement is still drawn COBOL0306.</summary>
    [Theory]
    [InlineData("IF W-N = 1 DISPLAY \"Q\" IF W-K = 1 END-IF ELSE DISPLAY \"Z\" END-IF. MOVE 1 W-N.", "COBOL2072@END-IF", "COBOL5506@W-N")]
    [InlineData("IF W-N = 1 DISPLAY \"Q\" IF W-K = 1 ELSE DISPLAY W-A END-IF ELSE DISPLAY \"Z\" END-IF. MOVE 1 W-N.", "COBOL2072@ELSE", "COBOL5506@W-N")]
    [InlineData("IF W-N = 1 DISPLAY \"Q\" IF W-K = 1 END-IF ELSE DISPLAY \"Z\" END-IF END-IF. MOVE 1 W-N.",
                "COBOL2072@END-IF", "COBOL0306@END-IF", "COBOL5506@W-N")]
    [InlineData("EVALUATE W-N WHEN 1 DISPLAY \"Q\" IF W-K = 1 END-IF WHEN 2 DISPLAY \"Y\" END-EVALUATE. MOVE 1 W-N.", "COBOL2072@END-IF", "COBOL5506@W-N")]
    [InlineData("EVALUATE W-N WHEN 1 DISPLAY \"Q\" IF W-K = 1 END-IF WHEN OTHER DISPLAY \"Y\" END-EVALUATE. MOVE 1 W-N.", "COBOL2072@END-IF", "COBOL5506@W-N")]
    [InlineData("EVALUATE W-N WHEN 1 DISPLAY \"Q\" IF W-K = 1 ELSE DISPLAY W-A END-IF WHEN 2 DISPLAY \"Y\" END-EVALUATE. MOVE 1 W-N.",
                "COBOL2072@ELSE", "COBOL5506@W-N")]
    [InlineData("EVALUATE W-N WHEN 1 IF W-K = 1 ELSE DISPLAY W-A END-IF WHEN 2 DISPLAY \"Y\" END-EVALUATE. MOVE 1 W-N.", "COBOL2072@ELSE", "COBOL5506@W-N")]
    [InlineData("EVALUATE W-N WHEN 1 DISPLAY \"Q\" IF W-K = 1 END-IF WHEN 2 DISPLAY \"Y\" END-EVALUATE END-EVALUATE. MOVE 1 W-N.",
                "COBOL2072@END-IF", "COBOL0306@END-EVALUATE", "COBOL5506@W-N")]
    [InlineData("SEARCH W-A WHEN W-N = 1 DISPLAY \"Q\" IF W-K = 1 ELSE DISPLAY W-A END-IF WHEN W-N = 2 DISPLAY \"Y\" END-SEARCH. MOVE 1 W-N.",
                "COBOL2072@ELSE", "COBOL5506@W-N")]
    // The condition's fault leaves the IF; the WHEN after its END-IF is the EVALUATE's.
    [InlineData("EVALUATE W-N WHEN 1 DISPLAY \"Q\" IF W-K NOT <> 1 DISPLAY \"C\" END-IF WHEN 2 DISPLAY \"Y\" END-EVALUATE. MOVE 1 W-N.",
                "COBOL0001@<>", "COBOL5506@W-N")]
    // A statement still short of its operand is refused at the word by its own fault, not taken for the left IF's phrase.
    [InlineData("IF W-N = 1 DISPLAY \"Q\" IF W-K = 1 END-IF ADD 1 TO ELSE DISPLAY \"Z\".", "COBOL2072@END-IF", "COBOL5506@ELSE")]
    // A phrase word met after the sentence's period is the next sentence's: no left statement awaits it.
    [InlineData("IF W-N = 1 DISPLAY \"Q\" IF W-K = 1 END-IF. ELSE DISPLAY \"Z\".", "COBOL2072@END-IF", "COBOL0001@ELSE")]
    public void APhraseWordOfAStatementTheRecoveryLeft_IsItsOwn_AndDrawsNoSecondReport(string statements, params string[] diagnostics)
        => AssertDiagnostics(statements, diagnostics);

    /// <summary>PB4449: "If all permitted occurrences of a phrase have already been specified for a given statement, a
    /// subsequent occurrence of that phrase is not syntactically associated with that statement" (ISO §14.5.3.3 4)), so
    /// the second ELSE of the IF the recovery left, and a WHEN OTHER after its WHEN OTHER, are no phrases of it: each is
    /// reported as the same word is after a statement the parse did not leave (COBOL2269, COBOL2073). A WHEN after any
    /// number of WHENs, and the first WHEN OTHER, are still the statement's own.</summary>
    [Theory]
    [InlineData("IF W-N = 1 DISPLAY \"Q\" IF W-K = 1 END-IF ELSE DISPLAY \"Z\" ELSE DISPLAY \"Y\" END-IF. MOVE 1 W-N.",
                "COBOL2072@END-IF", "COBOL2269@ELSE", "COBOL5506@W-N")]
    [InlineData("EVALUATE W-N WHEN 1 DISPLAY \"Q\" IF W-K = 1 END-IF WHEN OTHER DISPLAY \"Y\" WHEN OTHER DISPLAY \"Z\" END-EVALUATE. MOVE 1 W-N.",
                "COBOL2072@END-IF", "COBOL2073@WHEN", "COBOL5506@W-N")]
    [InlineData("EVALUATE W-N WHEN 1 DISPLAY \"Q\" IF W-K = 1 END-IF WHEN OTHER DISPLAY \"Y\" WHEN 3 DISPLAY \"Z\" END-EVALUATE. MOVE 1 W-N.",
                "COBOL2072@END-IF", "COBOL2269@WHEN", "COBOL5506@W-N")]
    [InlineData("PERFORM DISPLAY \"A\" WHEN EC-ALL DISPLAY \"B\" IF W-K = 1 END-IF WHEN OTHER DISPLAY \"C\" WHEN OTHER DISPLAY \"D\" END-PERFORM. "
                + "MOVE 1 W-N.", "COBOL2072@END-IF", "COBOL2073@WHEN", "COBOL5506@W-N")]
    // The occurrences before the repeat are the statement's own: each WHEN, then the one WHEN OTHER.
    [InlineData("EVALUATE W-N WHEN 1 DISPLAY \"Q\" IF W-K = 1 END-IF WHEN 2 WHEN 3 DISPLAY \"Y\" WHEN 4 DISPLAY \"X\" WHEN OTHER DISPLAY \"Z\" "
                + "END-EVALUATE. MOVE 1 W-N.", "COBOL2072@END-IF", "COBOL5506@W-N")]
    // An IF whose own ELSE was the fault takes no second one, and an outer IF's ELSE after it is the outer IF's.
    [InlineData("IF W-N = 1 IF W-K = 1 ELSE DISPLAY W-A END-IF ELSE DISPLAY \"X\" END-IF. MOVE 1 W-N.", "COBOL2072@ELSE", "COBOL5506@W-N")]
    public void ASecondOccurrenceOfAPhraseIsNoPhraseOfTheStatementTheRecoveryLeft(string statements, params string[] diagnostics)
        => AssertDiagnostics(statements, diagnostics);

    /// <summary>PB4449: the word is named by the statement whose format prints it after a statement it holds, here the IF
    /// the recovery left, not by the DISPLAY before it (hint 0h's statement).</summary>
    [Fact]
    public void TheRepeatedElse_NamesTheIfTheRecoveryLeft()
    {
        var errors = RecoverySources.Errors(RecoverySources.Program(
            "IF W-N = 1 DISPLAY \"Q\" IF W-K = 1 END-IF ELSE DISPLAY \"Z\" ELSE DISPLAY \"Y\" END-IF."));
        Assert.Contains(errors, e => e.Contains("COBOL2269", StringComparison.Ordinal)
                                     && e.Contains("The IF statement ends before this token", StringComparison.Ordinal));
    }

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
