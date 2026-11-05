// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ A SELECTION OBJECT'S OWN FAULTS ARE INDICATED WHETHER OR NOT ITS PAIR IS PERMITTED (kb/Work PB4446). ISO
/// §14.9.13.3 SR10 refuses a pair Table 15 leaves blank (COBOL1634); SR2 refuses a phrase whose object count differs from
/// the subject count (COBOL2106). Each is a fault of the pair or the count. The object's own reference and expression
/// faults are independent violations of the general formats and syntax rules (§4.2.2): an undefined or ambiguous name
/// (§8.4.2.1), a subscript on a scalar (§8.4.2.3.3 SR2), an unsubscripted table element (SR5), a reference-modification
/// position 0, an alphanumeric operand in an arithmetic expression (§8.8.1.1), a bad function argument and a range whose
/// ends differ in class (§14.9.13.3 SR4). The binder used to return before it bound a refused pair's object, and never
/// bound the surplus objects of a count breach at all, so each of these was hidden by the verdict about the pair.
/// <para>Each case pins the EXACT set of diagnostics, so the pair's secondary checks (the SR7 a) relation checkpoint, a
/// TRUE/FALSE word bound as a condition) stay suppressed: the guards at the end are the refusals that must keep drawing
/// ONE diagnostic.</para>
/// </summary>
public sealed class EvaluateObjectOperandFaultTests
{
    private static string Program(string statement) =>
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. EOF4446.\n"
        + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n"
        + "       01 RELF PIC 9 VALUE 3.\n"
        + "          88 C88 VALUE 3.\n"
        + "       01 A PIC X(3) VALUE \"ABC\".\n"
        + "       01 N PIC 9 VALUE 1.\n"
        + "       01 BW PIC 1(2) VALUE B\"10\".\n"
        + "       01 G.\n          05 T OCCURS 2 PIC 9.\n"
        + "       01 Q1.\n          05 DUP PIC 9.\n"
        + "       01 Q2.\n          05 DUP PIC 9.\n"
        + $"       PROCEDURE DIVISION.\n{statement}\n           STOP RUN.\n";

    /// <summary>The error codes of the compile, one per diagnostic, sorted.</summary>
    private static string[] Codes(string statement)
    {
        var (ok, errors) = EditionHarness.Compile(Program(statement), 2023);
        Assert.False(ok);
        return [.. errors.Select(e => Regex.Match(e, @"COBOL\d{4}").Value).Order(StringComparer.Ordinal)];
    }

    /// <summary>A permitted pair would draw the object's fault alone; the refused pair draws it AND the Table 15
    /// verdict, one of each.</summary>
    [Theory]
    [InlineData("           EVALUATE TRUE WHEN RELF (1) DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL2096")]
    [InlineData("           EVALUATE TRUE WHEN UNDEFX DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL1639")]
    [InlineData("           EVALUATE TRUE WHEN DUP DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL1639")]
    [InlineData("           EVALUATE TRUE WHEN T DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL2270")]
    [InlineData("           EVALUATE TRUE WHEN A (0:1) DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL2670")]
    [InlineData("           EVALUATE TRUE WHEN RELF + A DISPLAY \"B\" END-EVALUATE.", "COBOL0844 COBOL1634")]
    [InlineData("           EVALUATE TRUE WHEN FUNCTION ABS (A) DISPLAY \"B\" END-EVALUATE.", "COBOL1627 COBOL1634")]
    [InlineData("           EVALUATE TRUE WHEN ADDRESS OF UNDEFX DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL1639")]
    public void ARefusedPair_StillIndicatesTheObjectsOwnReferenceFault(string statement, string expected) =>
        Assert.Equal(expected.Split(' '), Codes(statement));

    /// <summary>The range, the partial expression and the condition rows of Table 15 hold operands too.</summary>
    [Theory]
    [InlineData("           EVALUATE TRUE WHEN RELF (1) THRU 5 DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL2096")]
    [InlineData("           EVALUATE TRUE WHEN 1 THRU \"A\" DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL2107")]
    [InlineData("           EVALUATE TRUE WHEN UNDEFX THRU 3 DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL1639")]
    [InlineData("           EVALUATE TRUE WHEN > RELF (1) DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL2096")]
    [InlineData("           EVALUATE TRUE WHEN > 1 AND < RELF (1) DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL2096")]
    [InlineData("           EVALUATE TRUE WHEN NUMERIC AND UNDEFX = 1 DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL1639")]
    [InlineData("           EVALUATE N WHEN RELF (1) = 1 DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL2096")]
    public void ARefusedPair_StillBindsARangeAPartialExpressionAndACondition(string statement, string expected) =>
        Assert.Equal(expected.Split(' '), Codes(statement));

    /// <summary>A BARE operand after a connective (kb/Work PB4606, ISO §8.8.4.12: <c>&gt; 1 AND UNDEFX</c> is
    /// <c>&gt; 1 AND &gt; UNDEFX</c> over the selection subject) is the program's own reference like the operand of a
    /// stated relation, so a refused pair indicates its fault too. A level-88 condition-name is a condition of its
    /// own and has no value operand to fault: it keeps the one Table 15 verdict.</summary>
    [Theory]
    [InlineData("           EVALUATE TRUE WHEN > 1 AND UNDEFX DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL1639")]
    [InlineData("           EVALUATE TRUE WHEN > 1 OR NOT UNDEFX DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL1639")]
    [InlineData("           EVALUATE TRUE WHEN > 1 AND (UNDEFX) DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL1639")]
    [InlineData("           EVALUATE TRUE WHEN > 1 AND RELF (1) DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL2096")]
    [InlineData("           EVALUATE TRUE WHEN > 1 AND T DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL2270")]
    [InlineData("           EVALUATE TRUE WHEN > 1 AND A (0:1) DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL2670")]
    [InlineData("           EVALUATE TRUE WHEN > 1 AND A + 1 DISPLAY \"B\" END-EVALUATE.", "COBOL0844 COBOL1634")]
    [InlineData("           EVALUATE TRUE WHEN > 1 AND FUNCTION ABS (A) DISPLAY \"B\" END-EVALUATE.", "COBOL1627 COBOL1634")]
    [InlineData("           EVALUATE TRUE WHEN > 1 AND ADDRESS OF UNDEFX DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL1639")]
    [InlineData("           EVALUATE TRUE WHEN NUMERIC AND UNDEFX DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL1639")]
    [InlineData("           EVALUATE TRUE WHEN > 1 AND C88 DISPLAY \"B\" END-EVALUATE.", "COBOL1634")]
    public void ARefusedPair_StillBindsABareOperandAfterAConnective(string statement, string expected) =>
        Assert.Equal(expected.Split(' '), Codes(statement));

    /// <summary>⛔ THE INSERTION ENDS AT A COMPLETE SIMPLE CONDITION (kb/Work PB4627, ISO §8.8.4.12.4 GR1: "The insertion
    /// of an omitted subject and/or relational operator terminates once a complete simple condition is encountered
    /// within a complex condition"). A bare value after a condition-name, a class or sign condition, a parenthesized
    /// group or a truth word is no object of the carried relation and no condition either (COBOL2318, §8.8.4.2.1); an
    /// abbreviated relation there has no subject (COBOL2552, §8.8.4.12.1); a STATED relation restarts the insertion,
    /// so the value after it is its object. The refused pair draws these beside its Table 15 verdict, as the same
    /// condition in an IF draws them alone.</summary>
    [Theory]
    [InlineData("           EVALUATE TRUE WHEN > 1 AND C88 AND 5 DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL2318")]
    [InlineData("           EVALUATE TRUE WHEN > 1 AND C88 AND A DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL2318")]
    [InlineData("           EVALUATE TRUE WHEN > 1 AND NOT C88 AND 5 DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL2318")]
    [InlineData("           EVALUATE TRUE WHEN > 1 AND (C88) AND 5 DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL2318")]
    [InlineData("           EVALUATE TRUE WHEN > 1 AND (N = 2) AND 5 DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL2318")]
    [InlineData("           EVALUATE TRUE WHEN > 1 AND N NUMERIC AND 5 DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL2318")]
    [InlineData("           EVALUATE TRUE WHEN > 1 AND N POSITIVE AND 5 DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL2318")]
    [InlineData("           EVALUATE TRUE WHEN NUMERIC AND 5 DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL2318")]
    [InlineData("           EVALUATE TRUE WHEN POSITIVE AND 5 DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL2318")]
    [InlineData("           EVALUATE TRUE WHEN NUMERIC AND ADDRESS OF A DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL2318")]
    [InlineData("           EVALUATE TRUE WHEN > 1 AND TRUE DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL2318")]
    [InlineData("           EVALUATE TRUE WHEN > 1 AND C88 AND UNDEFX DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL1639")]
    [InlineData("           EVALUATE TRUE WHEN NUMERIC AND > 1 DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL2552")]
    [InlineData("           EVALUATE TRUE WHEN > 1 AND C88 OR < 2 DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL2552")]
    [InlineData("           EVALUATE TRUE WHEN > 1 AND (N = 2) AND < 3 DISPLAY \"B\" END-EVALUATE.", "COBOL1634 COBOL2552")]
    [InlineData("           EVALUATE TRUE WHEN > 1 AND N = 2 AND 5 DISPLAY \"B\" END-EVALUATE.", "COBOL1634")]
    [InlineData("           EVALUATE TRUE WHEN > 1 AND N = 2 OR < 5 DISPLAY \"B\" END-EVALUATE.", "COBOL1634")]
    [InlineData("           EVALUATE TRUE WHEN > 1 AND 5 DISPLAY \"B\" END-EVALUATE.", "COBOL1634")]
    [InlineData("           EVALUATE TRUE WHEN > 1 AND < 5 AND ADDRESS OF A DISPLAY \"B\" END-EVALUATE.", "COBOL1634")]
    public void ARefusedPair_JudgesABareOperandAndAnAbbreviatedRelationByTheInsertion(string statement, string expected) =>
        Assert.Equal(expected.Split(' '), Codes(statement));

    /// <summary>A bare class-name after a stated or leading relation is read as the carried relation's object, as an IF
    /// reads it, so the operand binder refuses the word (COBOL1639); after a complete simple condition it is no
    /// condition (COBOL2318, §8.8.4.2.1), and a class-name that LEADS the object is the class condition without its
    /// identifier (§14.9.13.3 SR5) and draws nothing of its own.</summary>
    [Theory]
    [InlineData("WHEN > 1 AND MYCLS", "COBOL1634 COBOL1639")]
    [InlineData("WHEN MYCLS AND 5", "COBOL1634 COBOL2318")]
    [InlineData("WHEN MYCLS AND MYCLS", "COBOL1634 COBOL2318")]
    [InlineData("WHEN MYCLS", "COBOL1634")]
    public void ARefusedPair_JudgesABareClassNameByTheInsertion(string when, string expected)
    {
        string source = "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. EOF4627.\n"
            + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SPECIAL-NAMES.\n"
            + "           CLASS MYCLS IS \"A\" THRU \"C\".\n"
            + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 N PIC 9 VALUE 1.\n"
            + $"       PROCEDURE DIVISION.\n           EVALUATE TRUE {when} DISPLAY \"B\" END-EVALUATE.\n           STOP RUN.\n";
        var (ok, errors) = EditionHarness.Compile(source, 2023);
        Assert.False(ok);
        Assert.Equal(expected.Split(' '), errors.Select(e => Regex.Match(e, @"COBOL\d{4}").Value).Order(StringComparer.Ordinal));
    }

    /// <summary>The surplus objects of an SR2 breach have no subject, so no Table 15 verdict, and each is bound.</summary>
    [Theory]
    [InlineData("           EVALUATE TRUE WHEN TRUE ALSO UNDEFX DISPLAY \"B\" END-EVALUATE.", "COBOL1639 COBOL2106")]
    [InlineData("           EVALUATE N WHEN 1 ALSO UNDEFX THRU 3 DISPLAY \"B\" END-EVALUATE.", "COBOL1639 COBOL2106")]
    [InlineData("           EVALUATE N WHEN 1 ALSO > RELF (1) DISPLAY \"B\" END-EVALUATE.", "COBOL2096 COBOL2106")]
    [InlineData("           EVALUATE TRUE WHEN RELF (1) ALSO RELF (1) DISPLAY \"B\" END-EVALUATE.",
        "COBOL1634 COBOL2096 COBOL2096 COBOL2106")]
    [InlineData("           EVALUATE N WHEN 1 ALSO > 1 AND UNDEFX DISPLAY \"B\" END-EVALUATE.", "COBOL1639 COBOL2106")]
    [InlineData("           EVALUATE N WHEN 1 ALSO > 1 AND RELF (1) DISPLAY \"B\" END-EVALUATE.", "COBOL2096 COBOL2106")]
    [InlineData("           EVALUATE N WHEN 1 ALSO > 1 AND C88 AND 5 DISPLAY \"B\" END-EVALUATE.", "COBOL2106 COBOL2318")]
    [InlineData("           EVALUATE N WHEN 1 ALSO > 1 AND N = 2 AND 5 DISPLAY \"B\" END-EVALUATE.", "COBOL2106")]
    public void ASurplusObject_IsBoundByItsOwnRow(string statement, string expected) =>
        Assert.Equal(expected.Split(' '), Codes(statement));

    /// <summary>The refusal that ends the PAIR's checks keeps ending them: one diagnostic, never the relation
    /// checkpoint's or the "TRUE is used as a condition" cascade about an object the table has just refused.</summary>
    [Theory]
    [InlineData("           EVALUATE TRUE WHEN 3 DISPLAY \"B\" END-EVALUATE.")]
    [InlineData("           EVALUATE TRUE WHEN \"X\" THRU \"Z\" DISPLAY \"B\" END-EVALUATE.")]
    [InlineData("           EVALUATE RELF WHEN TRUE DISPLAY \"B\" END-EVALUATE.")]
    [InlineData("           EVALUATE BW WHEN TRUE DISPLAY \"B\" END-EVALUATE.")]
    [InlineData("           EVALUATE TRUE WHEN N DISPLAY \"B\" END-EVALUATE.")]
    public void ARefusedPairWithNothingElseWrong_StillDrawsOnlyTheTable15Verdict(string statement) =>
        Assert.Equal(["COBOL1634"], Codes(statement));
}
