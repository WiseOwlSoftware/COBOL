// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// kb/Work PB4950 and the notes it gathered: at <c>--std 85</c> a refusal names the rule of the 1985 text, and one fault draws one
/// verdict. Each expected citation was checked with <c>python scripts/spec/cite.py --check</c> on the words the case's comment
/// quotes; the counts are the rule's, one fault in each program.
/// </summary>
public sealed class EditionCitationTests
{
    private static string Program(string environment, string data, string procedure) => $"""
               IDENTIFICATION DIVISION.
               PROGRAM-ID. ECT.
               ENVIRONMENT DIVISION.
        {environment}
               DATA DIVISION.
        {data}
               PROCEDURE DIVISION.
               MAIN-P.
        {procedure}
                   STOP RUN.
        """;

    /// <summary>One construct valid in every edition, refused under each with its own edition's rule (PB4950's planted case):
    /// SEARCH ALL of a table with no KEY phrase. §1985:VI.6.22.3 SR1 "The description of identifier-1 in format 2 must also
    /// contain the KEY IS phrase in its OCCURS clause"; §2002:14.8.34.2 SR5 and §2014:14.9.34.2 SR5 "shall contain the KEY
    /// phrase"; §2023:14.9.37.3 SR7.</summary>
    [Theory]
    [InlineData(85, "ISO §1985:VI.6.22.3 SR1", "§2023:")]
    [InlineData(2002, "ISO §2002:14.8.34.2 SR5", "§1985:")]
    [InlineData(2014, "ISO §2014:14.9.34.2 SR5", "§1985:")]
    [InlineData(2023, "ISO §2023:14.9.37.3 SR7", "§1985:")]
    public void SearchAllWithoutKey_CitesTheRuleOfTheEditionInForce(int edition, string cites, string never)
    {
        string source = Program("",
            """
                   WORKING-STORAGE SECTION.
                   01  T.
                       05 E OCCURS 5 INDEXED BY IX PIC 9.
            """,
            """
                       SEARCH ALL E AT END DISPLAY "NONE"
                           WHEN E (IX) = 1 DISPLAY "ONE".
            """);
        var (ok, errors) = EditionHarness.Compile(source, edition);
        Assert.False(ok);
        string error = Assert.Single(errors, e => e.Contains("COBOL1964", StringComparison.Ordinal));
        Assert.Contains(cites, error, StringComparison.Ordinal);
        Assert.DoesNotContain(never, error, StringComparison.Ordinal);
    }

    /// <summary>kb/Work PB5667: a function-identifier in SEND WITH at 85 is one fault (COBOL-85 has no function-identifier
    /// outside an arithmetic expression, §1985:IV.4.3.8.3.3 SR4), so the operand draws the placement refusal alone and not
    /// also SR2's "one-character integer" (§1985:XIV.3.6.3 SR2).</summary>
    [Fact]
    public void SendWithFunctionIdentifier_At85_IsOneVerdict()
    {
        string source = Program("",
            """
                   WORKING-STORAGE SECTION.
                   COMMUNICATION SECTION.
                   CD CO FOR OUTPUT
                       DESTINATION COUNT IS DC
                       ERROR KEY IS EK
                       SYMBOLIC DESTINATION IS SY.
            """,
            """
                       SEND CO FROM EK WITH FUNCTION LENGTH(SY).
            """);
        var (ok, errors) = EditionHarness.Compile(source, 85);
        Assert.False(ok);
        string error = Assert.Single(errors);
        Assert.Contains("COBOL0900", error, StringComparison.Ordinal);
    }

    /// <summary>kb/Work PB5636: a 19-digit VALUE literal at 85 is refused as a literal (§1985:IV.4.2.2.2.2 "The implementor
    /// must allow for numeric literals of 1 through 18 digits in length", this compiler's limit there: COBOL0802), and the
    /// VALUE clause's PICTURE range is not asked of it as well.</summary>
    [Fact]
    public void OverlongValueLiteral_At85_IsOneVerdict()
    {
        string source = Program("",
            """
                   WORKING-STORAGE SECTION.
                   01  A PIC 9(5) VALUE 1234567890123456789.
            """,
            """
                       DISPLAY A.
            """);
        var (ok, errors) = EditionHarness.Compile(source, 85);
        Assert.False(ok);
        string error = Assert.Single(errors);
        Assert.Contains("COBOL0802", error, StringComparison.Ordinal);
    }

    /// <summary>kb/Work PB5905, PB5920: a literal written as SOURCE-COMPUTER's computer-name reads as an unrecognized paragraph,
    /// which heads no area-A item, so no area-A message (COBOL3734) joins it, and it is refused as what the user wrote, a
    /// computer-name that is no system-name (COBOL7654), not as "a paragraph of the CONFIGURATION SECTION" (COBOL1971).</summary>
    [Fact]
    public void LiteralComputerName_At85_IsOneVerdict()
    {
        string source = Program(
            """
                   CONFIGURATION SECTION.
                   SOURCE-COMPUTER. "ABC".
            """, "", """
                       DISPLAY "X".
            """);
        var (ok, errors) = EditionHarness.Compile(source, 85);
        Assert.False(ok);
        string error = Assert.Single(errors);
        Assert.Contains("COBOL7654", error, StringComparison.Ordinal);
        Assert.Contains("computer-name-1 stands", error, StringComparison.Ordinal);
        Assert.Contains("§1985:VI.4.3.3 SR1", error, StringComparison.Ordinal);
    }

    /// <summary>kb/Work PB5906: LINAGE-COUNTER OF a file of the containing program that is not GLOBAL names a file this program
    /// cannot reference; the message says that, citing the 1985 rule of local names (§1985:II.6.2.1 "A local name, however,
    /// may be used only to refer to the object with which it is associated from within the program in which the local name
    /// is declared"), and not that the file lacks the LINAGE clause it has.</summary>
    [Fact]
    public void LinageCounterOfALocalFileOfTheContainer_At85_SaysItIsNotVisible()
    {
        string source = """
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. OUTER.
                   ENVIRONMENT DIVISION.
                   INPUT-OUTPUT SECTION.
                   FILE-CONTROL.
                       SELECT F ASSIGN TO "ECTF.TXT".
                   DATA DIVISION.
                   FILE SECTION.
                   FD  F LINAGE IS 10 LINES.
                   01  FR PIC X(10).
                   PROCEDURE DIVISION.
                   P0.
                       CALL "INNER".
                       STOP RUN.
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. INNER.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01  N PIC 99.
                   PROCEDURE DIVISION.
                   P1.
                       MOVE LINAGE-COUNTER OF F TO N.
                       EXIT PROGRAM.
                   END PROGRAM INNER.
                   END PROGRAM OUTER.
            """;
        var (ok, errors) = EditionHarness.Compile(source, 85);
        Assert.False(ok);
        string error = Assert.Single(errors, e => e.Contains("COBOL0863", StringComparison.Ordinal));
        Assert.Contains("names no file this program can reference", error, StringComparison.Ordinal);
        Assert.Contains("ISO §1985:II.6.2.1", error, StringComparison.Ordinal);
    }
}
