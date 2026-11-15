// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// COBOL-85's mandatory exception phrase and its record-area operand rule (kb/Work PB5076, PB5077;
/// <c>ExceptionPhraseRequirement</c>, <c>StatementValidation.CheckPhraseOperandIsNotRecordArea</c>). Every case compiles
/// at --std 85, where X3.23-1985 prints both rules in each I-O module, and where the sibling edition says so, at 2002
/// (the operand rule stays, §2002:14.8.29.2 SR2; the phrase rule goes) and 2023 (neither).
/// <para>The one-verb-per-file goldens in <c>conformance-suite/cases</c> pin the refusal of each statement; THIS class pins the
/// rule's SCOPE, the questions a golden cannot ask cheaply: which USE declarative is "applicable" to which verb
/// (§1985:VII.4.6.4 GR5: the named file, or any open mode), a containing program's
/// GLOBAL declarative (§1985:X.5.5.4 GR1 b), the access-mode qualification of DELETE and REWRITE, and which operands are "the
/// same storage area" as the record area.</para>
/// </summary>
public sealed class IoExceptionPhraseRequirementTests
{
    private static string L(string programText) => "       " + programText;

    private static CompilerDriver.Result Check(int edition, params string[] lines)
    {
        string dir = ScratchDirectory.Create("iophrase");
        try
        {
            string src = Path.Combine(dir, "MAIN.cob");
            File.WriteAllText(src, string.Join("\n", lines.Select(L)) + "\n");
            return CompilerDriver.Compile(new CompilerDriver.Options(src, DialectLevel: edition, CheckOnly: true,
                SourceFormat: InitialReferenceFormat.Fixed));
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    private static void AssertRefused(CompilerDriver.Result r, string code)
    {
        Assert.False(r.Success, "the program compiled");
        Assert.Contains(r.Errors, e => e.Contains(code, StringComparison.Ordinal));
    }

    private static void AssertAccepted(CompilerDriver.Result r) => Assert.True(r.Success, string.Join("\n", r.Errors));

    // ── PB5076: an indexed file in DYNAMIC access, one statement, an optional USE declarative ──────────────────────

    private static string[] Indexed(string? use, string statement)
    {
        var head = new List<string>
        {
            "IDENTIFICATION DIVISION.", "PROGRAM-ID. IOP1.", "ENVIRONMENT DIVISION.", "INPUT-OUTPUT SECTION.", "FILE-CONTROL.",
            "    SELECT XF ASSIGN TO \"XF.DAT\" ORGANIZATION IS INDEXED", "        ACCESS MODE IS DYNAMIC RECORD KEY IS XK.",
            "    SELECT OF2 ASSIGN TO \"OF2.DAT\" ORGANIZATION IS SEQUENTIAL.",
            "DATA DIVISION.", "FILE SECTION.", "FD  XF.", "01  XREC.", "    05 XK PIC X(2).", "    05 XD PIC X(2).",
            "FD  OF2.", "01  OREC PIC X(4).", "PROCEDURE DIVISION.",
        };
        if (use is not null)
            head.AddRange(["DECLARATIVES.", "U1 SECTION.", $"    USE AFTER STANDARD EXCEPTION PROCEDURE ON {use}.",
                "U1-P.", "    EXIT.", "END DECLARATIVES.", "MAIN-SECT SECTION."]);
        head.AddRange(["MAIN-PARA.", "    OPEN I-O XF.", "    " + statement, "    CLOSE XF.", "    STOP RUN."]);
        return [.. head];
    }

    [Theory]
    [InlineData("READ XF.")]
    [InlineData("WRITE XREC.")]
    [InlineData("REWRITE XREC.")]
    [InlineData("DELETE XF.")]
    [InlineData("START XF KEY IS EQUAL TO XK.")]
    public void KeyedStatementWithoutItsPhraseAndWithoutAUse_IsRefusedAt85Only(string statement)
    {
        AssertRefused(Check(85, Indexed(null, statement)), "COBOL6826");
        AssertAccepted(Check(2002, Indexed(null, statement)));
        AssertAccepted(Check(2023, Indexed(null, statement)));
    }

    [Theory]
    [InlineData("READ XF INVALID KEY DISPLAY \"A\".")]
    [InlineData("READ XF NEXT AT END DISPLAY \"A\".")]
    [InlineData("WRITE XREC INVALID KEY DISPLAY \"A\".")]
    [InlineData("REWRITE XREC INVALID KEY DISPLAY \"A\".")]
    [InlineData("DELETE XF INVALID KEY DISPLAY \"A\".")]
    [InlineData("START XF KEY IS EQUAL TO XK INVALID KEY DISPLAY \"A\".")]
    public void KeyedStatementWithItsPhrase_IsAcceptedAt85(string statement)
        => AssertAccepted(Check(85, Indexed(null, statement)));

    // §1985:VII.4.6.4 GR5: a USE for the named file covers every statement of it, and a USE for an open mode covers every
    // statement of every file: the file may be in any mode when a statement runs, one the statement does not suit included
    // (a READ of a file open OUTPUT raises status 47, which a USE ON OUTPUT procedure takes; CCVS-85 SQ148A). A USE for
    // another file covers nothing.
    public static TheoryData<string, string> UseAndStatement()
    {
        var data = new TheoryData<string, string>();
        foreach (string use in new[] { "XF", "I-O", "INPUT", "OUTPUT", "EXTEND" })
            foreach (string statement in new[] { "READ XF.", "WRITE XREC.", "REWRITE XREC.", "DELETE XF.", "START XF KEY IS EQUAL TO XK." })
                data.Add(use, statement);
        return data;
    }

    [Theory]
    [MemberData(nameof(UseAndStatement))]
    public void UseDeclarativeForTheFileOrAMode_CoversEveryStatement(string useOn, string statement)
        => AssertAccepted(Check(85, Indexed(useOn, statement)));

    [Theory]
    [InlineData("READ XF.")]
    [InlineData("WRITE XREC.")]
    [InlineData("START XF KEY IS EQUAL TO XK.")]
    public void UseDeclarativeForAnotherFile_CoversNothing(string statement)
        => AssertRefused(Check(85, Indexed("OF2", statement)), "COBOL6826");

    // The access mode qualifies DELETE and REWRITE of a relative file (§1985:VIII.4.3.3 SR2, §1985:VIII.4.6.3 SR4): where
    // the phrase cannot be written (sequential access) there is nothing to require; a sequential file never raises an
    // invalid-key condition, so its WRITE and REWRITE need no phrase (§1985:VII.4.7.3, §1985:VII.4.5.3).
    private static string[] Relative(string access, string statement) =>
    [
        "IDENTIFICATION DIVISION.", "PROGRAM-ID. IOP2.", "ENVIRONMENT DIVISION.", "INPUT-OUTPUT SECTION.", "FILE-CONTROL.",
        "    SELECT RELF ASSIGN TO \"RELF.DAT\" ORGANIZATION IS RELATIVE", $"        ACCESS MODE IS {access} RELATIVE KEY IS RK.",
        "DATA DIVISION.", "FILE SECTION.", "FD  RELF.", "01  RREC PIC X(4).", "WORKING-STORAGE SECTION.", "01  RK PIC 9(2).",
        "PROCEDURE DIVISION.", "MAIN-PARA.", "    OPEN I-O RELF.", "    " + statement, "    CLOSE RELF.", "    STOP RUN.",
    ];

    [Theory]
    [InlineData("SEQUENTIAL", "DELETE RELF.")]
    [InlineData("SEQUENTIAL", "REWRITE RREC.")]
    public void RelativeSequentialAccessDeleteAndRewrite_NeedNoPhrase(string access, string statement)
        => AssertAccepted(Check(85, Relative(access, statement)));

    [Theory]
    [InlineData("RANDOM", "DELETE RELF.")]
    [InlineData("DYNAMIC", "DELETE RELF.")]
    [InlineData("RANDOM", "REWRITE RREC.")]
    [InlineData("DYNAMIC", "REWRITE RREC.")]
    [InlineData("SEQUENTIAL", "WRITE RREC.")]         // §1985:VIII.4.9.3 SR3 is unqualified by access mode
    [InlineData("SEQUENTIAL", "START RELF.")]
    public void RelativeStatementsTheRuleReaches_AreRefused(string access, string statement)
        => AssertRefused(Check(85, Relative(access, statement)), "COBOL6826");

    private static string[] Sequential(string statement) =>
    [
        "IDENTIFICATION DIVISION.", "PROGRAM-ID. IOP3.", "ENVIRONMENT DIVISION.", "INPUT-OUTPUT SECTION.", "FILE-CONTROL.",
        "    SELECT SF ASSIGN TO \"SF.DAT\" ORGANIZATION IS SEQUENTIAL.",
        "DATA DIVISION.", "FILE SECTION.", "FD  SF.", "01  SREC PIC X(4).",
        "PROCEDURE DIVISION.", "MAIN-PARA.", "    OPEN I-O SF.", "    " + statement, "    CLOSE SF.", "    STOP RUN.",
    ];

    [Theory]
    [InlineData("WRITE SREC.")]
    [InlineData("REWRITE SREC.")]
    [InlineData("READ SF AT END DISPLAY \"E\".")]
    public void SequentialFileStatementsTheRuleDoesNotReach_AreAccepted(string statement)
        => AssertAccepted(Check(85, Sequential(statement)));

    [Fact]
    public void SequentialReadWithoutAtEnd_IsRefused_AndInvalidKeyIsNotItsPhrase()
        => AssertRefused(Check(85, Sequential("READ SF.")), "COBOL6826");

    // §1985:X.5.5.4 GR1: a contained program's statement selects its own declaratives (a), then "The declarative in which
    // the GLOBAL phrase is specified and which is within the program directly containing the program which was last
    // examined" (b), outward (c). So a containing program's USE GLOBAL is applicable and its USE without GLOBAL is not.
    private static string[] ContainerWithUse(string useVerbPhrase, string useOn) =>
    [
        "IDENTIFICATION DIVISION.", "PROGRAM-ID. PARENT.", "ENVIRONMENT DIVISION.", "INPUT-OUTPUT SECTION.", "FILE-CONTROL.",
        "    SELECT SF ASSIGN TO \"SF.DAT\" ORGANIZATION IS SEQUENTIAL.",
        "DATA DIVISION.", "FILE SECTION.", "FD  SF GLOBAL.", "01  SREC PIC X(4).",
        "PROCEDURE DIVISION.", "DECLARATIVES.", "U1 SECTION.", $"    {useVerbPhrase} STANDARD EXCEPTION PROCEDURE ON {useOn}.",
        "U1-P.", "    EXIT.", "END DECLARATIVES.", "MAIN-SECT SECTION.", "MAIN-PARA.", "    OPEN INPUT SF.", "    CALL \"CHILD\".",
        "    CLOSE SF.", "    STOP RUN.",
        "IDENTIFICATION DIVISION.", "PROGRAM-ID. CHILD.", "PROCEDURE DIVISION.", "CHILD-PARA.", "    READ SF.",
        "    EXIT PROGRAM.", "END PROGRAM CHILD.", "END PROGRAM PARENT.",
    ];

    [Theory]
    [InlineData("SF")]
    [InlineData("INPUT")]
    public void ContainingProgramsGlobalUseDeclarative_CoversAContainedProgramsStatement(string useOn)
        => AssertAccepted(Check(85, ContainerWithUse("USE GLOBAL AFTER", useOn)));

    [Theory]
    [InlineData("SF")]
    [InlineData("INPUT")]
    public void ContainingProgramsUseDeclarativeWithoutGlobal_CoversNoContainedProgramsStatement(string useOn)
        => AssertRefused(Check(85, ContainerWithUse("USE AFTER", useOn)), "COBOL6826");

    [Fact]
    public void ContainedProgramsStatementWithNoUseAnywhere_IsRefused()
    {
        string[] source =
        [
            "IDENTIFICATION DIVISION.", "PROGRAM-ID. PARENT.", "ENVIRONMENT DIVISION.", "INPUT-OUTPUT SECTION.", "FILE-CONTROL.",
            "    SELECT SF ASSIGN TO \"SF.DAT\" ORGANIZATION IS SEQUENTIAL.",
            "DATA DIVISION.", "FILE SECTION.", "FD  SF GLOBAL.", "01  SREC PIC X(4).",
            "PROCEDURE DIVISION.", "MAIN-PARA.", "    OPEN INPUT SF.", "    CALL \"CHILD\".", "    CLOSE SF.", "    STOP RUN.",
            "IDENTIFICATION DIVISION.", "PROGRAM-ID. CHILD.", "PROCEDURE DIVISION.", "CHILD-PARA.", "    READ SF.",
            "    EXIT PROGRAM.", "END PROGRAM CHILD.", "END PROGRAM PARENT.",
        ];
        AssertRefused(Check(85, source), "COBOL6826");
    }

    // ── PB5077: which operands are "the same storage area" as the file's record area ───────────────────────────────

    private static string[] Records(string statement, string recordLines, string extraFile = "", string extraSelect = "") =>
    [
        "IDENTIFICATION DIVISION.", "PROGRAM-ID. IOP4.", "ENVIRONMENT DIVISION.", "INPUT-OUTPUT SECTION.", "FILE-CONTROL.",
        "    SELECT SF ASSIGN TO \"SF.DAT\" ORGANIZATION IS SEQUENTIAL.",
        extraSelect.Length == 0 ? "    SELECT SF2 ASSIGN TO \"SF2.DAT\" ORGANIZATION IS SEQUENTIAL." : extraSelect,
        "DATA DIVISION.", "FILE SECTION.", "FD  SF.", recordLines, "FD  SF2.", "01  OTHER-REC PIC X(8).",
        "WORKING-STORAGE SECTION.", "01  WS-REC PIC X(8).",
        "PROCEDURE DIVISION.", "MAIN-PARA.", "    OPEN I-O SF.", "    " + statement, "    CLOSE SF.", "    STOP RUN.",
    ];

    private const string OneRecord = "01  SREC.\n           05 SA PIC X(3).\n           05 SB PIC X(5).";

    [Theory]
    [InlineData("WRITE SREC FROM SREC.")]
    [InlineData("REWRITE SREC FROM SREC.")]
    [InlineData("READ SF INTO SREC AT END DISPLAY \"E\".")]
    public void OperandIsTheRecordItself_IsRefusedBefore2014(string statement)
    {
        AssertRefused(Check(85, Records(statement, OneRecord)), "COBOL6827");
        AssertRefused(Check(2002, Records(statement, OneRecord)), "COBOL6827");
        AssertAccepted(Check(2014, Records(statement, OneRecord)));   // §2014:E.3 drops the rule (kb/Work PB5270)
        AssertAccepted(Check(2023, Records(statement, OneRecord)));   // §2023:14.6.10 governs the overlap
    }

    [Theory]
    [InlineData("WRITE SREC FROM WS-REC.")]
    [InlineData("REWRITE SREC FROM WS-REC.")]
    [InlineData("READ SF INTO WS-REC AT END DISPLAY \"E\".")]
    [InlineData("WRITE SREC FROM OTHER-REC.")]                 // another file's record: disjoint storage
    [InlineData("READ SF INTO OTHER-REC AT END DISPLAY \"E\".")]
    [InlineData("WRITE SREC FROM SA.")]                        // a part of the record is not the whole area
    [InlineData("WRITE SREC FROM SREC(1:3).")]                 // a reference-modified record is not the whole area
    [InlineData("READ SF INTO SB AT END DISPLAY \"E\".")]
    public void OperandOutsideTheRecordArea_IsAccepted(string statement)
        => AssertAccepted(Check(85, Records(statement, OneRecord)));

    [Fact]
    public void AnotherRecordOfTheSameFile_IsTheSameArea()
    {
        const string two = "01  SREC.\n           05 SA PIC X(3).\n           05 SB PIC X(5).\n       01  SREC2 PIC X(8).";
        AssertRefused(Check(85, Records("WRITE SREC FROM SREC2.", two)), "COBOL6827");   // 01 entries under an FD redefine one area
        AssertRefused(Check(85, Records("READ SF INTO SREC2 AT END DISPLAY \"E\".", two)), "COBOL6827");
    }

    [Fact]
    public void SingleSubordinateAsLongAsTheRecord_IsTheSameArea()
    {
        const string sub = "01  SREC.\n           05 SALL PIC X(8).";
        AssertRefused(Check(85, Records("WRITE SREC FROM SALL.", sub)), "COBOL6827");
        AssertRefused(Check(85, Records("READ SF INTO SALL AT END DISPLAY \"E\".", sub)), "COBOL6827");
    }

    [Fact]
    public void RecordOfAFileSharingTheRecordArea_IsTheSameArea()
    {
        string[] source =
        [
            "IDENTIFICATION DIVISION.", "PROGRAM-ID. IOP5.", "ENVIRONMENT DIVISION.", "INPUT-OUTPUT SECTION.", "FILE-CONTROL.",
            "    SELECT SF ASSIGN TO \"SF.DAT\" ORGANIZATION IS SEQUENTIAL.",
            "    SELECT SF2 ASSIGN TO \"SF2.DAT\" ORGANIZATION IS SEQUENTIAL.",
            "I-O-CONTROL.", "    SAME RECORD AREA FOR SF SF2.",
            "DATA DIVISION.", "FILE SECTION.", "FD  SF.", "01  SREC PIC X(8).", "FD  SF2.", "01  OTHER-REC PIC X(8).",
            "PROCEDURE DIVISION.", "MAIN-PARA.", "    OPEN I-O SF.", "    WRITE SREC FROM OTHER-REC.", "    CLOSE SF.", "    STOP RUN.",
        ];
        AssertRefused(Check(85, source), "COBOL6827");
    }
}
