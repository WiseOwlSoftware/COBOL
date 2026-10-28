// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler;
using Xunit;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// kb/Work PB75 — a size error raised OUTSIDE an arithmetic statement's own catch is the fatal exception condition
/// ISO §14.7.5's no-phrase rules name ("EC-SIZE-OVERFLOW … is set to exist, and processing proceeds as specified in
/// 14.6.13.1.3"), never a raw CLR crash. The golden <c>pb75_sdidi_overflow_outside_arithmetic</c> pins #4/#5 (USE /
/// PERFORM WHEN dispatch with RESUME); this class pins the two TERMINATING dispositions: #7 (checking enabled, no
/// handler resumes) and #8 (checking NOT enabled — the implementor's choice, documented as loud termination). Before
/// this landing every one of these was an unhandled <c>CobolSizeError</c> stack trace, exit 127.
/// </summary>
public sealed class SizeErrorDispositionTests
{
    private static string Prog(string turn, string body) => $$"""
        {{turn}}
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB75DISP.
               OPTIONS.
                   ARITHMETIC IS STANDARD-DECIMAL.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 WS-X PIC 9(5).
               PROCEDURE DIVISION.
               MAIN-P.
                   DISPLAY "BEFORE".
                   {{body}}
                   DISPLAY "AFTER".
                   STOP RUN.
        """;

    private static (int Exit, string Stdout, string Stderr) Run(string source)
    {
        string dir = ScratchDirectory.Create("pb75");
        try
        {
            string src = Path.Combine(dir, "prog.cob"), dll = Path.Combine(dir, "prog.dll");
            src = CompiledProgramCache.StageSource(src, source);
            var r = CompiledProgramCache.Compile(new CompilerDriver.Options(src, dll, DialectLevel: 2023, SourceFormat: InitialReferenceFormat.Auto));
            Assert.True(r.Success, "[compile] " + string.Join("\n", r.Errors));
            return CutRunner.RunExit(dll, dir);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>The NATIVE-arithmetic twin (kb/Work PB639): the size error is on the RESULT's transfer into the
    /// receiver (§14.7.5 case 3), not on an intermediate, so the receiver is a fixed-point picture the aligned
    /// value cannot hold rather than a decimal128 escape.</summary>
    private static string ProgNative(string turn, string body) => $$"""
        {{turn}}
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB639DISP.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 WC PIC S9(9)V9(9) VALUE 7.
               PROCEDURE DIVISION.
               MAIN-P.
                   DISPLAY "BEFORE".
                   {{body}}
                   DISPLAY "AFTER".
                   STOP RUN.
        """;

    /// <summary>kb/Work PB639 — §14.7.5 NO-PHRASE RULE 4 over the RESULT: "if the result of the arithmetic
    /// statement is a value further from zero than permitted for the associated resultant data item, the
    /// EC-SIZE-TRUNCATION exception condition is set to exist", disposed by §14.6.13.1.3 #7 (checking enabled,
    /// nothing resumes) — the run unit terminates abnormally NAMING that condition, never EC-SIZE-OVERFLOW (which
    /// is the INTERMEDIATE's condition, no-phrase rule 3) and never silence.
    /// <para>⛔ The second row is the discriminator. 340282366920938463463374607432 is a legal 30-digit literal
    /// (§8.3.3.3.2) equal to ceil(2^128 / 10^9), so aligning it at the receiver's scale 9 WRAPS to 231788544 —
    /// well inside <c>PIC S9(9)V9(9)</c>. The store's capacity check looked at that wrap, found it fitting, and
    /// the program ran to "AFTER" with no exception at all: an exception condition the source asked to be told
    /// about, silently absent. Both rows are checked so a fix that only handles the obvious magnitude is
    /// visible.</para></summary>
    [Theory]
    [InlineData("COMPUTE WC = 1000000000000000000000000000000.")]
    [InlineData("COMPUTE WC = 340282366920938463463374607432.")]
    [InlineData("MULTIPLY 1000000000000000000000 BY 1000000000 GIVING WC.")]
    public void NoPhraseResultOverflow_UnderChecking_TerminatesNamingEcSizeTruncation(string body)
    {
        var (exit, stdout, stderr) = Run(ProgNative(">>TURN EC-SIZE CHECKING ON", body));
        Assert.Equal(1, exit);
        Assert.Contains("BEFORE", stdout);
        Assert.DoesNotContain("AFTER", stdout);
        Assert.Contains("EC-SIZE-TRUNCATION", stderr);
        Assert.DoesNotContain("Unhandled exception", stderr);
        Assert.DoesNotContain("   at ", stderr);   // no CLR stack trace
    }

    /// <summary>The same two results with checking NOT enabled: §14.6.13.1.3 #8 hands the disposition to the
    /// implementor and CONFORMANCE.md DOC-A.1-70 documents it as "execution continues and the resultant
    /// identifier receives the LOW-ORDER digits of the result aligned at its scale" — so the program MUST reach
    /// "AFTER" and exit 0. This is the other half of the same rule: rule 4 sets the condition in both cases; only
    /// the disposition differs. (The stored digits themselves are pinned by the golden
    /// <c>pb639_store_landing_low_order_digits</c>.)</summary>
    [Theory]
    [InlineData("COMPUTE WC = 1000000000000000000000000000000.")]
    [InlineData("COMPUTE WC = 340282366920938463463374607432.")]
    public void NoPhraseResultOverflow_CheckingOff_ContinuesPerTheDocumentedDetermination(string body)
    {
        var (exit, stdout, stderr) = Run(ProgNative("", body));
        Assert.Equal(0, exit);
        Assert.Contains("AFTER", stdout);
        Assert.DoesNotContain("EC-SIZE", stderr);
    }

    /// <summary>§14.6.13.1.3 #8 — checking NOT enabled: the SDIDI range overflow in a CONDITION reaches the run-unit
    /// boundary and terminates loudly (this implementation's documented choice) — the abnormal-termination surface
    /// names the condition, exit 1; never a .NET stack trace / exit 127.</summary>
    [Theory]
    [InlineData("IF 10 ** 100000 > 5 DISPLAY \"GT\" ELSE DISPLAY \"LE\" END-IF.")]
    [InlineData("DISPLAY \"V=\" FUNCTION ABS(10 ** 100000).")]
    [InlineData("MOVE FUNCTION INTEGER-PART(10 ** 100000) TO WS-X.")]
    [InlineData("COMPUTE WS-X = 10 ** 100000.")]
    public void CheckingOff_TerminatesLoudly_NeverARawCrash(string body)
    {
        var (exit, stdout, stderr) = Run(Prog("", body));
        Assert.Equal(1, exit);
        Assert.Contains("BEFORE", stdout);
        Assert.DoesNotContain("AFTER", stdout);
        Assert.Contains("abnormal run-unit termination: EC-SIZE-OVERFLOW (fatal)", stderr);
        Assert.DoesNotContain("Unhandled exception", stderr);
        Assert.DoesNotContain("   at ", stderr);   // no CLR stack trace
    }

    /// <summary>kb/Work PB1605 — the ZERO DIVISOR over every value lane and every divide shape. §14.7.5 case 2 is the
    /// size error condition wherever a divisor is zero, and no-phrase rule 2 sets EC-SIZE-ZERO-DIVIDE; with checking
    /// not enabled §14.6.13.1.3 item 8 hands the disposition to the implementor, and CONFORMANCE.md DOC-A.1-70's is
    /// that a quotient which cannot be formed has no value to store: the run unit terminates abnormally naming the
    /// condition, exit 1. <c>ARITHMETIC</c> is the lane switch — native (the scaled Int128 carrier, and binary64
    /// wherever a FLOAT-LONG operand or receiver is present) or STANDARD-DECIMAL (the SDIDI intermediate, which
    /// alone terminated before PB1605). The scaled rows stored 000 and ran on; the binary64 rows stored +Infinity
    /// (FR), and the float-to-fixed row stored 000.</summary>
    [Theory]
    [InlineData("NATIVE", "COMPUTE A = B / Z.")]
    [InlineData("NATIVE", "COMPUTE A = 1 + B / Z.")]                  // a nested (non-final) quotient
    [InlineData("NATIVE", "DIVIDE Z INTO A.")]
    [InlineData("NATIVE", "DIVIDE B BY Z GIVING A REMAINDER R.")]
    [InlineData("NATIVE", "IF B / Z > 1 DISPLAY \"GT\" END-IF.")]     // receiverless: a condition
    [InlineData("NATIVE", "COMPUTE FR = FL / FZ.")]
    [InlineData("NATIVE", "DIVIDE FZ INTO FR.")]
    [InlineData("NATIVE", "COMPUTE A = FL / FZ.")]
    [InlineData("NATIVE", "IF FL / FZ > 1 DISPLAY \"GT\" END-IF.")]
    [InlineData("STANDARD-DECIMAL", "COMPUTE A = B / Z.")]
    public void ZeroDivisor_CheckingOff_TerminatesNamingEcSizeZeroDivide(string arithmetic, string body)
    {
        var (exit, stdout, stderr) = Run(ProgZeroDivisor(arithmetic, body));
        Assert.Equal(1, exit);
        Assert.Contains("BEFORE", stdout);
        Assert.DoesNotContain("AFTER", stdout);
        Assert.Contains("abnormal run-unit termination: EC-SIZE-ZERO-DIVIDE (fatal)", stderr);
        Assert.DoesNotContain("   at ", stderr);   // no CLR stack trace
    }

    /// <summary>The same zero divisors under the SIZE ERROR phrase: §14.7.5 phrase rule 1 leaves every resultant
    /// unchanged and rule 3 runs the phrase, then execution continues — the unchecked kernels' raise must not
    /// change the checked path. A (VALUE 7) and FR (VALUE 9) are displayed as their initial values.</summary>
    [Theory]
    [InlineData("NATIVE", "COMPUTE A = B / Z ON SIZE ERROR DISPLAY \"SE\" END-COMPUTE.")]
    [InlineData("NATIVE", "COMPUTE FR = FL / FZ ON SIZE ERROR DISPLAY \"SE\" END-COMPUTE.")]
    [InlineData("STANDARD-DECIMAL", "COMPUTE A = B / Z ON SIZE ERROR DISPLAY \"SE\" END-COMPUTE.")]
    public void ZeroDivisor_UnderThePhrase_LeavesTheResultantsAndContinues(string arithmetic, string body)
    {
        var (exit, stdout, _) = Run(ProgZeroDivisor(arithmetic, body +" DISPLAY \"A=\" A \" FR=\" FR."));
        Assert.Equal(0, exit);
        Assert.Contains("SE", stdout);
        Assert.Contains("A=007 FR=9", stdout);
        Assert.Contains("AFTER", stdout);
    }

    /// <summary>kb/Work PB3869 (retired) — a statement written with ONLY a NOT ON SIZE ERROR phrase meets a zero divisor
    /// with checking off. §14.7.5 sends it to the no-phrase rules ("If a NOT SIZE ERROR phrase is specified, it is
    /// ignored"), which end in §14.6.13.1.3 item 8: the implementor defines whether execution continues and how the
    /// receivers are affected. CONFORMANCE.md DOC-A.1-70 follows GnuCOBOL there (CLAUDE.md rule 1): a statement carrying a
    /// SIZE ERROR or a NOT SIZE ERROR phrase leaves every resultant unchanged and continues, and the NOT imperative does
    /// not run. A (VALUE 7), R (VALUE 9) and FR (VALUE 9) are displayed as their initial values.</summary>
    [Theory]
    [InlineData("NATIVE", "DIVIDE B BY Z GIVING A NOT ON SIZE ERROR DISPLAY \"NSE\" END-DIVIDE.")]
    [InlineData("NATIVE", "COMPUTE A = B / Z NOT ON SIZE ERROR DISPLAY \"NSE\" END-COMPUTE.")]
    [InlineData("NATIVE", "DIVIDE Z INTO A NOT ON SIZE ERROR DISPLAY \"NSE\" END-DIVIDE.")]
    [InlineData("NATIVE", "DIVIDE B BY Z GIVING A REMAINDER R NOT ON SIZE ERROR DISPLAY \"NSE\" END-DIVIDE.")]
    [InlineData("NATIVE", "COMPUTE FR = FL / FZ NOT ON SIZE ERROR DISPLAY \"NSE\" END-COMPUTE.")]
    [InlineData("STANDARD-DECIMAL", "COMPUTE A = B / Z NOT ON SIZE ERROR DISPLAY \"NSE\" END-COMPUTE.")]
    public void ZeroDivisor_NotSizeErrorAlone_LeavesTheResultantsAndContinues(string arithmetic, string body)
    {
        var (exit, stdout, _) = Run(ProgZeroDivisor(arithmetic, body + " DISPLAY \"A=\" A \" R=\" R \" FR=\" FR."));
        Assert.Equal(0, exit);
        Assert.DoesNotContain("NSE", stdout);
        Assert.Contains("A=007 R=009 FR=9", stdout);
        Assert.Contains("AFTER", stdout);
    }

    /// <summary>kb/Work PB3869 (retired) — the same NOT-only statement over a RESULT past its receiver, checking off, on
    /// every receiver store (fixed-point, edited, float) and both value lanes. DOC-A.1-70's determination for a statement
    /// carrying a NOT SIZE ERROR phrase (§14.6.13.1.3 item 8, following GnuCOBOL): the resultant keeps its value and the
    /// NOT imperative is skipped; a result that fits lands and the imperative runs (§14.7.5's closing paragraph). A holds
    /// 7 and E holds 0 before each statement; the displays are the verdict.</summary>
    [Theory]
    [InlineData("NATIVE", "COMPUTE A = 1234", "A=007 E=  0 R=0 F=-")]
    [InlineData("NATIVE", "COMPUTE A = 123", "A=123 E=  0 R=0 F=N")]
    [InlineData("STANDARD-DECIMAL", "COMPUTE A = 1234", "A=007 E=  0 R=0 F=-")]
    [InlineData("STANDARD-DECIMAL", "COMPUTE A = 123", "A=123 E=  0 R=0 F=N")]
    [InlineData("NATIVE", "ADD 1000 TO A", "A=007 E=  0 R=0 F=-")]
    [InlineData("NATIVE", "ADD 10 TO A", "A=017 E=  0 R=0 F=N")]
    [InlineData("NATIVE", "MULTIPLY 200 BY A", "A=007 E=  0 R=0 F=-")]
    [InlineData("NATIVE", "DIVIDE 5 INTO A", "A=001 E=  0 R=0 F=N")]
    [InlineData("NATIVE", "COMPUTE A ROUNDED = 999.6", "A=007 E=  0 R=0 F=-")]
    [InlineData("NATIVE", "COMPUTE E = 12345", "A=007 E=  0 R=0 F=-")]
    [InlineData("NATIVE", "COMPUTE E = 123", "A=007 E=123 R=0 F=N")]
    [InlineData("NATIVE", "COMPUTE A = FL * 400", "A=007 E=  0 R=0 F=-")]
    [InlineData("NATIVE", "COMPUTE A = FL * 2", "A=010 E=  0 R=0 F=N")]
    [InlineData("NATIVE", "COMPUTE FR = FL * 2", "A=007 E=  0 R=0 F=N")]
    // §14.9.12.4 6) c): the remainder is a resultant too; 59 / 30 leaves 29, past PIC 9, so R keeps 0 and NOT is skipped.
    [InlineData("NATIVE", "DIVIDE 59 BY 30 GIVING A REMAINDER R", "A=001 E=  0 R=0 F=-")]
    [InlineData("NATIVE", "DIVIDE 66 BY 30 GIVING A REMAINDER R", "A=002 E=  0 R=6 F=N")]
    public void NotSizeErrorAlone_CheckingOff_LeavesTheResultant_AndRunsOnlyWithoutASizeError(
        string arithmetic, string statement, string verdict)
    {
        var (exit, stdout, _) = Run($$"""
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB3869NSE.
               OPTIONS.
                   ARITHMETIC IS {{arithmetic}}.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 A  PIC 9(3) VALUE 7.
               01 E  PIC ZZ9 VALUE 0.
               01 R  PIC 9 VALUE 0.
               01 F  PIC X VALUE "-".
               01 FL USAGE FLOAT-LONG VALUE 5.
               01 FR USAGE FLOAT-LONG VALUE 9.
               PROCEDURE DIVISION.
               MAIN-P.
                   {{statement}} NOT ON SIZE ERROR MOVE "N" TO F END-{{statement[..statement.IndexOf(' ')]}}.
                   DISPLAY "A=" A " E=" E " R=" R " F=" F.
                   STOP RUN.
            """);
        Assert.Equal(0, exit);
        Assert.Equal(verdict, stdout.Trim());
    }

    /// <summary>kb/Work PB3869 (retired) — the NOT-only statement with EC-SIZE checking ENABLED: with no size error
    /// condition the NOT phrase runs (§14.7.5's closing paragraph); with one, no SIZE ERROR phrase being written,
    /// §14.6.13.1.3 #7 ends the run unit naming EC-SIZE-TRUNCATION and the NOT phrase is ignored.</summary>
    [Theory]
    [InlineData("COMPUTE WS-X = 5 NOT ON SIZE ERROR DISPLAY \"NSE\" END-COMPUTE.", 0, true)]
    [InlineData("COMPUTE WS-X = 123456 NOT ON SIZE ERROR DISPLAY \"NSE\" END-COMPUTE.", 1, false)]
    public void NotSizeErrorAlone_CheckingOn_RunsOnlyWithoutASizeError(string body, int exit, bool phraseRuns)
    {
        var (actualExit, stdout, stderr) = Run(Prog(">>TURN EC-SIZE CHECKING ON", body));
        Assert.Equal(exit, actualExit);
        Assert.Equal(phraseRuns, stdout.Contains("NSE", StringComparison.Ordinal));
        if (exit == 1) Assert.Contains("EC-SIZE-TRUNCATION", stderr);
    }

    private static string ProgZeroDivisor(string arithmetic, string body) => $$"""
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB1605ZD.
               OPTIONS.
                   ARITHMETIC IS {{arithmetic}}.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 B  PIC 9 VALUE 5.
               01 Z  PIC 9 VALUE 0.
               01 A  PIC 9(3) VALUE 7.
               01 R  PIC 9(3) VALUE 9.
               01 FL USAGE FLOAT-LONG VALUE 5.
               01 FZ USAGE FLOAT-LONG VALUE 0.
               01 FR USAGE FLOAT-LONG VALUE 9.
               PROCEDURE DIVISION.
               MAIN-P.
                   DISPLAY "BEFORE".
                   {{body}}
                   DISPLAY "AFTER".
                   STOP RUN.
        """;

    /// <summary>§14.6.13.1.3 #7 — checking enabled and nothing resumes: the guarded statement sets the status,
    /// finds no USE / WHEN, and the run unit terminates abnormally naming the condition.</summary>
    [Fact]
    public void CheckingOn_NoHandler_TerminatesAbnormally()
    {
        var (exit, stdout, stderr) = Run(Prog(">>TURN EC-SIZE-OVERFLOW CHECKING ON",
            "IF 10 ** 100000 > 5 DISPLAY \"GT\" ELSE DISPLAY \"LE\" END-IF."));
        Assert.Equal(1, exit);
        Assert.Contains("BEFORE", stdout);
        Assert.DoesNotContain("AFTER", stdout);
        Assert.Contains("EC-SIZE-OVERFLOW", stderr);
        Assert.DoesNotContain("Unhandled exception", stderr);
    }

    /// <summary>ONE dispatch per raise (§14.6.13.1.3 #5 → #7): a fatal condition raised by a statement INSIDE a
    /// PERFORM is processed by that statement's guard — the USE declarative runs once — and, unresumed, terminates
    /// the run unit; the enclosing PERFORM's guard lets the already-dispatched condition pass. Before this landing
    /// every enclosing statement re-dispatched it: the declarative ran once per nesting level.</summary>
    [Fact]
    public void FatalRaiseInsideAPerform_IsDispatchedOnce_ThenTerminates()
    {
        string src = """
                  >>TURN EC-BOUND-REF-MOD CHECKING ON
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB75ONCE.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 X PIC X(5) VALUE "HELLO".
                   01 Y PIC X(2).
                   PROCEDURE DIVISION.
                   DECLARATIVES.
                   H SECTION.
                       USE AFTER EXCEPTION CONDITION EC-BOUND-REF-MOD.
                   H-P.
                       DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
                   END DECLARATIVES.
                   MAIN SECTION.
                   MAIN-P.
                       DISPLAY "R1".
                       PERFORM 2 TIMES
                           MOVE X(9:1) TO Y
                           DISPLAY "IN-LOOP"
                       END-PERFORM.
                       DISPLAY "R2".
                       STOP RUN.
            """;
        var (exit, stdout, stderr) = Run(src);
        Assert.Equal(1, exit);
        Assert.Equal(1, stdout.Split("CAUGHT=").Length - 1);   // exactly ONE dispatch
        Assert.DoesNotContain("IN-LOOP", stdout);
        Assert.Contains("abnormal run-unit termination: EC-BOUND-REF-MOD (fatal)", stderr);
    }

    /// <summary>The native carrier's receiverless-lane raise (§8.8.1 alignment past the Int128 escape boundary —
    /// <c>CobolNum.RescaleEscape</c>, kb/Work PB69's "stays loud") is the same condition and takes the same
    /// disposition: a subscript expression whose alignment overflows terminates loudly, not with a stack trace.
    /// The statement that reaches the lane is a numeric CONDITION: DISPLAY of a selecting function no longer aligns its
    /// arguments - it shows the selected argument's own item (kb/Work PB3514), so <c>DISPLAY FUNCTION MAX(BIG F)</c> prints
    /// BIG whole where it used to terminate for a value that is perfectly representable.</summary>
    [Fact]
    public void NativeReceiverlessOverflow_TerminatesLoudly()
    {
        string src = """
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB75NAT.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 BIG PIC 9(31) VALUE 9999999999999999999999999999999.
                   01 T.
                      05 E PIC X OCCURS 3.
                   01 F PIC 9V9(9) VALUE 1.5.
                   PROCEDURE DIVISION.
                   MAIN-P.
                       DISPLAY "BEFORE".
                       IF FUNCTION MAX(BIG F) > 0 DISPLAY "POSITIVE" END-IF.
                       DISPLAY "AFTER".
                       STOP RUN.
            """;
        var (exit, stdout, stderr) = Run(src);
        Assert.Equal(1, exit);
        Assert.Contains("BEFORE", stdout);
        Assert.Contains("abnormal run-unit termination: EC-SIZE-OVERFLOW (fatal)", stderr);
        Assert.DoesNotContain("Unhandled exception", stderr);
    }
}
