// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ AT COBOL-85 A PROGRAM IS TOLD ABOUT THE 1985 TEXT, ONCE (kb/Work PB6023, PB6028).
///
/// <para><b>Run time (PB6023).</b> A breach the run time reports at <c>--std 85</c> names the 1989 amendment's or the 1985
/// standard's rule (<c>ExceptionEngine.EditionRule</c>), and a reference modification out of range terminates without
/// naming EC-BOUND-REF-MOD, an exception-name COBOL-85 does not have (it has no exception conditions). The standard fixes no
/// message text and a program cannot read it, so the termination surface is asserted here; from 2002 the text is unchanged.</para>
///
/// <para><b>Compile time (PB6028).</b> A construct the edition lacks is refused once, by its edition gate (or by the rule that
/// refused it), and no rule a later edition wrote about that construct speaks after it: the codes each program draws at 85
/// are the whole set expected, and the later code still answers at the edition that has the rule.</para>
/// </summary>
public sealed class Cobol85EditionVocabularyTests
{
    private static string Program(string id, string data, string procedure) => $"""
               IDENTIFICATION DIVISION.
               PROGRAM-ID. {id}.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
        {data}
               PROCEDURE DIVISION.
               P1.
                   DISPLAY "BEFORE".
        {procedure}
                   DISPLAY "AFTER".
                   STOP RUN.
        """;

    /// <summary>Each 1989 function's own argument rule, at 85, in the termination message (§1989:A.2.14.3 AR1 "Argument-1 must
    /// be an integer greater than or equal to zero"; A.2.5.3 AR2 the ACOS domain; A.2.28.3 AR2 "The value of argument-2 must not
    /// be zero"; A.2.29.3 AR1 the NUMVAL formats).</summary>
    [Theory]
    [InlineData("Z6023A", "       01 N PIC S9 VALUE -1.\n       01 R PIC 9(4).", "           COMPUTE R = FUNCTION FACTORIAL (N).", "§1989:A.2.14.3 AR1")]
    [InlineData("Z6023B", "       01 N PIC S9 VALUE 2.\n       01 R PIC 9V9(4).", "           COMPUTE R = FUNCTION ACOS (N).", "§1989:A.2.5.3 AR2")]
    [InlineData("Z6023C", "       01 N PIC S9 VALUE 0.\n       01 R PIC 9(4).", "           COMPUTE R = FUNCTION MOD (5 N).", "§1989:A.2.28.3 AR2")]
    [InlineData("Z6023D", "       01 T PIC X(4) VALUE \"ABCD\".\n       01 R PIC 9(4).", "           COMPUTE R = FUNCTION NUMVAL (T).", "§1989:A.2.29.3 AR1")]
    public void AtCobol85_ABreachCitesThe1989Rule(string id, string data, string procedure, string rule)
    {
        var (exit, stdout, detail) = new WiseOwlCobolCompiler(85).CompileAndRunExit(Program(id, data, procedure));
        Assert.Equal("BEFORE", stdout);
        Assert.NotEqual(0, exit);
        Assert.Contains(rule, detail);
        Assert.DoesNotContain("§2023:", detail.Replace("§1989:A.2.2 p3", ""));
    }

    /// <summary>§1985:IV.4.3.8.4.4 GR4: "The evaluation of length must result in a positive nonzero integer." A zero length at 85
    /// terminates the run, citing that rule and naming no exception condition; from 2002 the message still names EC-BOUND-REF-MOD.</summary>
    [Fact]
    public void AtCobol85_ARefModOutOfRangeNamesNoExceptionCondition()
    {
        const string data = "       01 N PIC S9 VALUE 0.\n       01 X PIC X(4) VALUE \"ABCD\".\n       01 Y PIC X(4).";
        const string proc = "           MOVE X (1 : N) TO Y.";
        var (exit85, out85, detail85) = new WiseOwlCobolCompiler(85).CompileAndRunExit(Program("Z6023E", data, proc));
        Assert.Equal("BEFORE", out85);
        Assert.NotEqual(0, exit85);
        Assert.Contains("§1985:IV.4.3.8.4.4 GR4", detail85);
        Assert.DoesNotContain("EC-BOUND-REF-MOD", detail85);
        var (_, out23, detail23) = new WiseOwlCobolCompiler(2023).CompileAndRunExit(Program("Z6023F", data, proc));
        Assert.Equal("BEFORE", out23);
        Assert.Contains("EC-BOUND-REF-MOD (fatal)", detail23);
        Assert.Contains("ISO §2023:8.4.3.3.4 item 5c", detail23);
    }

    private static SortedSet<string> CodesAt(int edition, string source)
    {
        var (exit, _, detail) = new WiseOwlCobolCompiler(edition).CompileAndRunExit(source);
        Assert.Equal(-1, exit);   // refused
        return new SortedSet<string>(Regex.Matches(detail, @"\bCOBOL\d{4}\b").Select(m => m.Value), StringComparer.Ordinal);
    }

    /// <summary>The construct each program uses is refused at 85 by its edition gate (COBOL0900) or by the one rule that refused it
    /// (COBOL2423, a second CURRENCY SIGN clause: §1985:VI.4.5.4 GR11 limits SPECIAL-NAMES to one), and nothing else is reported;
    /// at the later edition the later rule still speaks.</summary>
    [Theory]
    // ALPHABET ... IS UTF-8: the national coded sets are 2002's; COBOL5986 (the FOR NATIONAL branch rule) is 2002's too.
    [InlineData("""
               IDENTIFICATION DIVISION.
               PROGRAM-ID. Z6028A.
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               SPECIAL-NAMES.
                   ALPHABET A1 IS UTF-8.
               PROCEDURE DIVISION.
               P1.
                   STOP RUN.
        """, "COBOL0900", 2002, "COBOL5986")]
    // A hexadecimal literal with a non-hexadecimal digit: the format is 2002's, and so is its repertoire rule (COBOL2630).
    [InlineData("""
               IDENTIFICATION DIVISION.
               PROGRAM-ID. Z6028B.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01  X1 PIC X(2) VALUE X"ZZ".
               PROCEDURE DIVISION.
               P1.
                   STOP RUN.
        """, "COBOL0900", 2002, "COBOL2630")]
    // A boolean literal seeding an alphanumeric item: the literal is 2002's, and so is the VALUE rule about it (COBOL5982).
    [InlineData("""
               IDENTIFICATION DIVISION.
               PROGRAM-ID. Z6028C.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01  X2 PIC X(2) VALUE B"10".
               PROCEDURE DIVISION.
               P1.
                   STOP RUN.
        """, "COBOL0900", 2002, "COBOL5982")]
    // UPPER-CASE with a malformed LOCALE phrase: the phrase is 2014's; its form rule (COBOL1504) speaks only where it exists.
    [InlineData("""
               IDENTIFICATION DIVISION.
               PROGRAM-ID. Z6028D.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01  A PIC X(4) VALUE "abcd".
               PROCEDURE DIVISION.
               P1.
                   MOVE FUNCTION UPPER-CASE (A LOCALE) TO A.
                   STOP RUN.
        """, "COBOL0900", 2023, "COBOL1504")]
    // Two CURRENCY SIGN clauses and a NUMVAL-C without argument-2: the second clause is the 1985 refusal (COBOL2423);
    // §2023:15.68.3 AR3's "only one currency string" (COBOL1644) is 2002's rule.
    [InlineData("""
               IDENTIFICATION DIVISION.
               PROGRAM-ID. Z6028E.
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               SPECIAL-NAMES.
                   CURRENCY SIGN IS "F"
                   CURRENCY SIGN IS "G".
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               77 N PIC 9(4).
               PROCEDURE DIVISION.
               MAIN.
                   COMPUTE N = FUNCTION NUMVAL-C ("F12").
                   STOP RUN.
        """, "COBOL2423", 2002, "COBOL1644")]
    public void AtCobol85_TheRefusalIsTheOnlyReport(string source, string only85, int later, string laterCode)
    {
        Assert.Equal(new SortedSet<string>([only85], StringComparer.Ordinal), CodesAt(85, source));
        Assert.Contains(laterCode, CodesAt(later, source));
    }
}
