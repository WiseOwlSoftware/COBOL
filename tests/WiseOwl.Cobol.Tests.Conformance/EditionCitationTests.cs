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
    [InlineData(85, "§1985:VI.6.22.3 SR1", "§2023:")]
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
        Assert.Contains("§1985:II.6.2.1", error, StringComparison.Ordinal);
    }

    /// <summary>The programs of kb/Work PB6010, each refused at every edition by one code, each once citing the wrong edition's
    /// rule (or the wrong 1985 module) at 85. Keyed by a name; the source is in <see cref="Pb6010Source"/>.</summary>
    public static TheoryData<string, string, string, string> Pb6010Cases => new()
    {
        // §1985:X.4.6.3 SR1 "The GLOBAL clause may be specified only in data description entries whose level-number is 01 in
        // the File Section or the Working-Storage Section, file description entries, or report description entries."
        { "cd-record-global", "COBOL2404", "§1985:X.4.6.3 SR1", "§1985:X.4.3.3 SR4" },
        // §1985:IX.4.8.3 SR1 (the indexed I-O module's USE) "A USE statement, when present, must immediately follow a section
        // header in the declaratives portion of the Procedure Division".
        { "use-outside-indexed", "COBOL2377", "§1985:IX.4.8.3 SR1", "§1985:VII.4.6.3" },
        // §1985:VI.4.5.4 GR4: alphabet-name-1 referenced "in the COLLATING SEQUENCE phrase of a SORT or MERGE statement".
        { "sort-collating-not-alphabet", "COBOL5989", "§1985:VI.4.5.4 GR4", "§2023:" },
        // §1985:XIII.3.19.3 SR1 "If identifier-1 is defined in the Report Section, identifier-1 must reference a sum counter."
        { "sum-page-counter", "COBOL0899", "§1985:XIII.3.19.3 SR1", "§2023:" },
        // §1985:X.4.6.3 SR1, the edition gate's relaxation row linkage-global-2002 (rule1985).
        { "linkage-global", "COBOL0900", "§1985:X.4.6.3 SR1", "§2023:" },
        // The areas rows (COBOL3734/3735) are asked of the registry itself, ConstructRegistryDriftTests
        // .EveryRelaxationRow_CitesIts1985RuleAt85: this harness compiles no fixed-form area check.
    };

    private static string Pb6010Source(string name) => name switch
    {
        "cd-record-global" => Program("", """
                   COMMUNICATION SECTION.
                   CD  CDIN FOR INPUT
                       SYMBOLIC QUEUE IS IN-QUEUE
                       MESSAGE COUNT IS IN-COUNT.
                   01  CDIN-REC GLOBAL PIC X(87).
            """, """
                       DISPLAY "X".
            """),
        "use-outside-indexed" => """
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. ECTU.
                   ENVIRONMENT DIVISION.
                   INPUT-OUTPUT SECTION.
                   FILE-CONTROL.
                       SELECT IX ASSIGN TO "ECTIX"
                           ORGANIZATION IS INDEXED
                           ACCESS MODE IS RANDOM
                           RECORD KEY IS K.
                   DATA DIVISION.
                   FILE SECTION.
                   FD  IX.
                   01  IX-REC.
                       05  K PIC X(4).
                   PROCEDURE DIVISION.
                   MAIN-P.
                       USE AFTER STANDARD EXCEPTION PROCEDURE ON IX.
                       STOP RUN.
            """,
        "sort-collating-not-alphabet" => """
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. ECTS.
                   ENVIRONMENT DIVISION.
                   INPUT-OUTPUT SECTION.
                   FILE-CONTROL.
                       SELECT SF ASSIGN TO "ECTSF".
                   DATA DIVISION.
                   FILE SECTION.
                   SD  SF.
                   01  SR.
                       05  SK PIC X(4).
                   PROCEDURE DIVISION.
                   MAIN-P.
                       SORT SF ON ASCENDING KEY SK COLLATING SEQUENCE IS ALPH
                           INPUT PROCEDURE IS IN-P OUTPUT PROCEDURE IS OUT-P.
                       STOP RUN.
                   IN-P.
                       EXIT.
                   OUT-P.
                       EXIT.
            """,
        "sum-page-counter" => """
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. ECTR.
                   ENVIRONMENT DIVISION.
                   INPUT-OUTPUT SECTION.
                   FILE-CONTROL.
                       SELECT PRT ASSIGN TO "ECTRF".
                   DATA DIVISION.
                   FILE SECTION.
                   FD  PRT REPORT IS R-1.
                   WORKING-STORAGE SECTION.
                   01  W PIC X VALUE "A".
                   REPORT SECTION.
                   RD  R-1 CONTROL IS FINAL
                       PAGE LIMIT 20 LINES HEADING 1 FIRST DETAIL 3
                       LAST DETAIL 15 FOOTING 18.
                   01  DE-1 TYPE DE LINE PLUS 1.
                       02  COLUMN 1 PIC X SOURCE W.
                   01  CF-1 TYPE CF FINAL LINE PLUS 1.
                       02  SC COLUMN 1 PIC 99 SUM PAGE-COUNTER.
                   PROCEDURE DIVISION.
                   MAIN-P.
                       STOP RUN.
            """,
        "linkage-global" => """
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. ECTL.
                   DATA DIVISION.
                   LINKAGE SECTION.
                   01  L PIC X GLOBAL.
                   PROCEDURE DIVISION USING L.
                   MAIN-P.
                       EXIT PROGRAM.
            """,
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "no such PB6010 program"),
    };

    /// <summary>kb/Work PB6010: at <c>--std 85</c> each refusal cites the rule of the 1985 text (the module of the file it names,
    /// for a USE), never another edition's clause; from 2002 the same refusal cites the 2023 rule it always did.</summary>
    [Theory]
    [MemberData(nameof(Pb6010Cases))]
    public void Pb6010Refusal_At85_CitesThe1985Rule(string name, string code, string cites, string never)
    {
        var (ok, errors) = EditionHarness.Compile(Pb6010Source(name), 85);
        Assert.False(ok);
        string error = Assert.Single(errors, e => e.Contains(code, StringComparison.Ordinal) && e.Contains("§1985:", StringComparison.Ordinal)
            || e.Contains(code, StringComparison.Ordinal) && e.Contains(never, StringComparison.Ordinal));
        Assert.Contains(cites, error, StringComparison.Ordinal);
        Assert.DoesNotContain(never, error, StringComparison.Ordinal);
    }

    /// <summary>kb/Work PB6009: an out-of-order comment-entry paragraph is quoted by its header as written, never with the
    /// period the reference-format conversion writes for its discarded entry (<c>'AUTHOR. .'</c>).</summary>
    [Fact]
    public void OutOfOrderCommentParagraph_At85_IsQuotedByItsHeader()
    {
        string source = """
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. ECTC.
                   DATE-WRITTEN. 1985-01-01.
                   AUTHOR. A WRITER.
                   PROCEDURE DIVISION.
                   MAIN-P.
                       STOP RUN.
            """;
        var (ok, errors) = EditionHarness.Compile(source, 85);
        Assert.False(ok);
        string error = Assert.Single(errors, e => e.Contains("COBOL2987", StringComparison.Ordinal));
        Assert.Contains("'AUTHOR.' is written after 'DATE-WRITTEN.'", error, StringComparison.Ordinal);
        Assert.DoesNotContain(". .'", error, StringComparison.Ordinal);
    }

    /// <summary>kb/Work PB5985, PB6011: a reserved word written where computer-name-1 stands is the name written as something that
    /// is none (COBOL7654), at every edition, citing the computer-name rule and the reserved-word rule of the edition in force:
    /// §1985:VI.4.4.3 SR1 / §1985:VI.4.3.3 SR1 "Computer-name is a system-name" and §1985:III.2 (a reserved word "must not appear
    /// in the program as user-defined words or system-names"); §2023:8.3.2.1 SR1 "Reserved words shall not be used as
    /// user-defined words or system-names".</summary>
    [Theory]
    [InlineData("OBJECT-COMPUTER. GLOBAL.", 85, "§1985:VI.4.4.3 SR1", "§1985:III.2")]
    [InlineData("SOURCE-COMPUTER. MERGE.", 85, "§1985:VI.4.3.3 SR1", "§1985:III.2")]
    [InlineData("OBJECT-COMPUTER. GLOBAL.", 2023, "§2023:8.3.2.3.1", "§2023:8.3.2.1 SR1")]
    [InlineData("SOURCE-COMPUTER. MERGE.", 2023, "§2023:8.3.2.3.1", "§2023:8.3.2.1 SR1")]
    public void ReservedWordAsComputerName_IsNoSystemName(string paragraph, int edition, string computerNameRule, string reservedRule)
    {
        string source = Program($"""
                   CONFIGURATION SECTION.
                   {paragraph}
            """, "", """
                       DISPLAY "X".
            """);
        var (ok, errors) = EditionHarness.Compile(source, edition);
        Assert.False(ok);
        string error = Assert.Single(errors);
        Assert.Contains("COBOL7654", error, StringComparison.Ordinal);
        Assert.Contains("computer-name-1 stands", error, StringComparison.Ordinal);
        Assert.Contains(computerNameRule, error, StringComparison.Ordinal);
        Assert.Contains(reservedRule, error, StringComparison.Ordinal);
    }
}
