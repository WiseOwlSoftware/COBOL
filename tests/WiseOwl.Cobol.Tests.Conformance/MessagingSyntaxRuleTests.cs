// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ THE SYNTAX RULES OF SEND AND RECEIVE ARE ASKED OF EVERY OPERAND, ARM BY ARM (kb/Work PB4743; docs/rearchitecture/DESIGN-message-control.md §4.3).
/// <para>ISO §14.9.38.3 SR3 and §14.9.31.3 SR2 list the clauses identifier-1 and its subordinates shall not contain: ANY LENGTH, BASED, DYNAMIC-LENGTH,
/// OBJECT-REFERENCE, POINTER, FUNCTION-POINTER, PROGRAM-POINTER, and an OCCURS DEPENDING ON whose object is not within identifier-1 (RECEIVE's list adds
/// CONSTANT RECORD). <c>MessagingBinder.ForbiddenClause</c> is one switch over them, so a conformance case per arm would be eleven programs that differ in
/// one line; the conformance cases (<c>conformance-suite/wiseowl/cases/14.9.38-SEND/pb4743-*</c>) pin the refusals through the harness, and this class
/// pins every arm, both statements, and the legal neighbours of each (an OCCURS DEPENDING ON whose object is within identifier-1, a CONSTANT RECORD that SEND
/// reads).</para>
/// </summary>
public sealed class MessagingSyntaxRuleTests
{
    private const string OperandClass = "COBOL6265";
    private const string MessageArea = "COBOL6266";

    /// <summary>A main program with a message-tag item <c>MT</c>, the ordinary items the statements need, the items under test and the statements; a function
    /// the main program names (a FUNCTION-POINTER item's prototype) is <paramref name="head"/>.</summary>
    private static string Prog(string pid, string items, string body, string head = "") => $"""
        {head}
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        {(head.Length > 0 ? "ENVIRONMENT DIVISION.\n        CONFIGURATION SECTION.\n        REPOSITORY.\n            FUNCTION PB4743FN." : "")}
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 MT USAGE MESSAGE-TAG.
        01 WS-MSG PIC X(8).
        01 WS-LEN PIC 9(4).
        {items}
        PROCEDURE DIVISION.
        MAIN-P.
            {body}
            STOP RUN.
        """;

    private const string SendArea = "SEND TO \"SRV\" FROM MSG-AREA RETURNING MT END-SEND.";
    private const string ReceiveArea = "RECEIVE MT GIVING MSG-AREA WS-LEN END-RECEIVE.";

    /// <summary>One forbidden-clause arm: its name and the data description entries that declare MSG-AREA.</summary>
    public static TheoryData<string, string> ForbiddenAreas => new()
    {
        { "POINTER", "01 MSG-AREA USAGE POINTER." },
        { "PROGRAM-POINTER", "01 MSG-AREA USAGE PROGRAM-POINTER." },
        { "OBJECT-REFERENCE", "01 MSG-AREA USAGE OBJECT REFERENCE." },
        { "DYNAMIC-LENGTH", "01 MSG-AREA PIC X DYNAMIC LENGTH." },
        { "BASED", "01 MSG-AREA BASED PIC X(8)." },
        { "OCCURS-DEPENDING-ON-OUTSIDE", "01 CNT PIC 9 VALUE 2.\n        01 MSG-AREA.\n           05 T PIC X OCCURS 1 TO 5 DEPENDING ON CNT." },
    };

    [Theory]
    [MemberData(nameof(ForbiddenAreas))]
    public void SendFrom_ARefusedClause_IsRefusedWithItsRule(string arm, string items)
    {
        var diagnostics = EditionHarness.GetDiagnostics(Prog("PB4743T1", items, SendArea), 2023);
        EditionHarness.AssertHasDiagnostic(diagnostics, MessageArea);
        EditionHarness.AssertHasDiagnostic(diagnostics, "ISO §14.9.38.3 SR3");
        Assert.True(diagnostics.Count(d => d.Contains(MessageArea, StringComparison.Ordinal)) == 1, $"{arm}: the statement is refused once");
    }

    [Theory]
    [MemberData(nameof(ForbiddenAreas))]
    public void ReceiveGiving_ARefusedClause_IsRefusedWithItsRule(string arm, string items)
    {
        var diagnostics = EditionHarness.GetDiagnostics(Prog("PB4743T2", items, ReceiveArea), 2023);
        EditionHarness.AssertHasDiagnostic(diagnostics, MessageArea);
        EditionHarness.AssertHasDiagnostic(diagnostics, "ISO §14.9.31.3 SR2");
        Assert.True(diagnostics.Count(d => d.Contains(MessageArea, StringComparison.Ordinal)) == 1, $"{arm}: the statement is refused once");
    }

    /// <summary>The two arms that need a declaration beside the item: a FUNCTION-POINTER names a function-prototype, and an ANY LENGTH item is a formal of
    /// a contained program (§13.18.2.3 SR2: it may not be specified in an outermost program).</summary>
    [Fact]
    public void AFunctionPointerArea_IsRefused_ForSendAndReceive()
    {
        const string function = """
            IDENTIFICATION DIVISION.
            FUNCTION-ID. PB4743FN.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-ARG PIC 9(4).
            01 L-RES PIC 9(4).
            PROCEDURE DIVISION USING L-ARG RETURNING L-RES.
                MOVE L-ARG TO L-RES
                GOBACK.
            END FUNCTION PB4743FN.
            """;
        foreach (var (statement, rule) in new[] { (SendArea, "ISO §14.9.38.3 SR3"), (ReceiveArea, "ISO §14.9.31.3 SR2") })
        {
            var diagnostics = EditionHarness.GetDiagnostics(
                Prog("PB4743T3", "01 MSG-AREA USAGE FUNCTION-POINTER TO PB4743FN.", statement, head: function), 2023);
            EditionHarness.AssertHasDiagnostic(diagnostics, MessageArea);
            EditionHarness.AssertHasDiagnostic(diagnostics, rule);
            EditionHarness.AssertHasDiagnostic(diagnostics, "FUNCTION-POINTER clause");
        }
    }

    [Theory]
    [InlineData("SEND TO \"SRV\" FROM L-ANY RETURNING MT END-SEND.", "ISO §14.9.38.3 SR3")]
    [InlineData("RECEIVE MT GIVING L-ANY WS-LEN END-RECEIVE.", "ISO §14.9.31.3 SR2")]
    public void AnAnyLengthArea_IsRefused_ForSendAndReceive(string statement, string rule)
    {
        string source = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB4743T4.
            PROCEDURE DIVISION.
                CALL "PB4743IN" USING "ABC".
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB4743IN.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 MT USAGE MESSAGE-TAG.
            01 WS-LEN PIC 9(4).
            LINKAGE SECTION.
            01 L-ANY PIC X ANY LENGTH.
            PROCEDURE DIVISION USING L-ANY.
                {statement}
                GOBACK.
            END PROGRAM PB4743IN.
            END PROGRAM PB4743T4.
            """;
        var diagnostics = EditionHarness.GetDiagnostics(source, 2023);
        EditionHarness.AssertHasDiagnostic(diagnostics, MessageArea);
        EditionHarness.AssertHasDiagnostic(diagnostics, rule);
        EditionHarness.AssertHasDiagnostic(diagnostics, "ANY LENGTH clause");
    }

    /// <summary>The legal neighbours: an OCCURS DEPENDING ON whose object is WITHIN identifier-1 is none of the forbidden clauses, and a CONSTANT RECORD is
    /// a message SEND reads (SEND's list does not name it) but a RECEIVE cannot write — the receiving funnel refuses it with COBOL1548 (§13.18.15.3 SR2),
    /// never with the message-area code.</summary>
    [Fact]
    public void LegalNeighbours_AreAccepted_AndAConstantRecordIsNotReceivedInto()
    {
        const string odoInside = "01 MSG-AREA.\n           05 CNT PIC 9 VALUE 2.\n           05 T PIC X OCCURS 1 TO 5 DEPENDING ON CNT.";
        foreach (string statement in new[] { SendArea, ReceiveArea })
        {
            var diagnostics = EditionHarness.GetDiagnostics(Prog("PB4743T5", odoInside, statement), 2023);
            EditionHarness.AssertNoDiagnostic(diagnostics, MessageArea);
            EditionHarness.AssertNoDiagnostic(diagnostics, OperandClass);
        }
        const string constantRecord = "01 MSG-AREA CONSTANT RECORD.\n           05 CC PIC X(8) VALUE \"ABC\".";
        var send = EditionHarness.GetDiagnostics(Prog("PB4743T6", constantRecord, SendArea), 2023);
        EditionHarness.AssertNoDiagnostic(send, MessageArea);
        EditionHarness.AssertNoDiagnostic(send, "COBOL1548");
        var receive = EditionHarness.GetDiagnostics(Prog("PB4743T7", constantRecord, ReceiveArea), 2023);
        EditionHarness.AssertHasDiagnostic(receive, "COBOL1548");
        EditionHarness.AssertNoDiagnostic(receive, MessageArea);
    }

    /// <summary>The class screens: SR1 (literal-1), SR4 (data-name-1, data-name-2) and RECEIVE's SR1 and SR3, each with the operand that passes beside the
    /// one that does not. A packed or binary integer is an integer data item of class numeric; a scaled item, a floating-point item and an alphanumeric
    /// item are not.</summary>
    [Theory]
    [InlineData("SEND TO \"SRV\" FROM WS-MSG RETURNING MT END-SEND.", false)]
    [InlineData("SEND TO N\"SRV\" FROM WS-MSG RETURNING MT END-SEND.", false)]
    [InlineData("SEND TO 5 FROM WS-MSG RETURNING MT END-SEND.", true)]
    [InlineData("SEND TO 5.5 FROM WS-MSG RETURNING MT END-SEND.", true)]
    [InlineData("SEND TO \"SRV\" FROM WS-MSG RETURNING WS-LEN END-SEND.", true)]
    [InlineData("SEND TO MT FROM WS-MSG END-SEND.", false)]
    [InlineData("SEND TO WS-LEN FROM WS-MSG RAISING LAST END-SEND.", true)]
    [InlineData("RECEIVE MT GIVING WS-MSG WS-LEN END-RECEIVE.", false)]
    [InlineData("RECEIVE MT GIVING WS-MSG WS-BIN END-RECEIVE.", false)]
    [InlineData("RECEIVE MT GIVING WS-MSG WS-PACKED END-RECEIVE.", false)]
    [InlineData("RECEIVE MT GIVING WS-MSG WS-FRAC END-RECEIVE.", true)]
    [InlineData("RECEIVE MT GIVING WS-MSG WS-FLOAT END-RECEIVE.", true)]
    [InlineData("RECEIVE MT GIVING WS-MSG WS-MSG END-RECEIVE.", true)]
    [InlineData("RECEIVE WS-LEN GIVING WS-MSG WS-LEN END-RECEIVE.", true)]
    public void TheClassScreens(string statement, bool refused)
    {
        const string items = "01 WS-BIN PIC S9(4) COMP.\n        01 WS-PACKED PIC 9(5) COMP-3.\n        01 WS-FRAC PIC 9V99.\n        01 WS-FLOAT COMP-2.";
        var diagnostics = EditionHarness.GetDiagnostics(Prog("PB4743T8", items, statement), 2023);
        if (refused) EditionHarness.AssertHasDiagnostic(diagnostics, OperandClass);
        else EditionHarness.AssertNoDiagnostic(diagnostics, OperandClass);
    }

    /// <summary>The word after CONTINUE AFTER MESSAGE is RECEIVED (§14.9.31.2): any other word is a parse error, and the data item named MESSAGE still
    /// reads as arithmetic-expression-1 (kb/Work PB3794).</summary>
    [Theory]
    [InlineData("CONTINUE AFTER MESSAGE RECEIVED", false)]
    [InlineData("CONTINUE MESSAGE RECEIVED", false)]
    [InlineData("CONTINUE AFTER MESSAGE FOO", true)]
    [InlineData("CONTINUE AFTER MESSAGE SENT", true)]
    public void ContinueAfterMessage_NeedsTheWordReceived(string phrase, bool refused)
    {
        var diagnostics = EditionHarness.GetDiagnostics(Prog("PB4743T9", "", $"RECEIVE MT GIVING WS-MSG WS-LEN {phrase} END-RECEIVE."), 2023);
        if (refused) EditionHarness.AssertHasDiagnostic(diagnostics, "COBOL0001");
        else EditionHarness.AssertNoDiagnostic(diagnostics, "COBOL0001");
    }

    /// <summary>Below the edition that introduced the facility the edition pass's COBOL0900 is the whole answer (kb/Work PB374): the operands are not
    /// asked, so a program of an earlier edition draws no message-tag screen for a statement its edition does not have.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    public void BelowTheIntroduction_TheStatementsAreRefusedAsNotYetIntroduced_AndTheirOperandsAreNotScreened(int edition)
    {
        const string items = "01 TAG PIC X(8).";
        var diagnostics = EditionHarness.GetDiagnostics(Prog("PB4743TA", items,
            "RECEIVE TAG GIVING WS-MSG WS-LEN END-RECEIVE.\n            SEND TO \"SRV\" FROM WS-MSG RETURNING TAG END-SEND."), edition);
        EditionHarness.AssertHasDiagnostic(diagnostics, "COBOL0900");
        EditionHarness.AssertNoDiagnostic(diagnostics, OperandClass);
        EditionHarness.AssertNoDiagnostic(diagnostics, MessageArea);
    }
}
