// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using WiseOwl.Cobol.Runtime.Mcs;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ THE ASYNCHRONOUS MESSAGING FACILITY ACROSS REAL RUN UNITS (kb/Work PB4744; docs/rearchitecture/DESIGN-message-control.md §7.2 "Across processes", §2023:14.9.38 SEND,
/// §2023:14.9.31 RECEIVE, §2023:14.6.11 item 7): a server run unit (<c>WISEOWL_COBOL_MCS_SERVER</c>) and a requestor are two compiled programs in two .NET processes sharing one MCS
/// directory, ordered by the protocol and by lines each prints and each reads, so nothing sleeps. A <c>Process.Kill</c> is the kill. Every expected value is derived from the
/// rule it cites; the exchange's byte-level and interleaving cases are <c>MessagingProtocolTests</c> and <c>MessagingPeerTests</c> (Unit).
/// </summary>
public sealed class MessagingProcessTests : IDisposable
{
    private readonly string _dir = ScratchDirectory.Create("pb4744p-");
    private readonly List<LiveProcess> _live = [];

    public void Dispose()
    {
        foreach (var p in _live) p.Dispose();
        ScratchDirectory.Remove(McsSocketDirectory.Resolve(McsDirectory.Resolve(_dir).Identity).Path);
        ScratchDirectory.Remove(_dir);
    }

    /// <summary>The server program of the exchange cases: prints READY once started (the listener is published before the first statement, so a requestor started after the
    /// line finds it), RECEIVEs one message into a buffer of <paramref name="size"/> positions, prints what it got, then runs <paramref name="after"/>.</summary>
    private static string Server(string name, int size, string after, string before = "") => $"""
       IDENTIFICATION DIVISION.
       PROGRAM-ID. {name}.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RT        USAGE MESSAGE-TAG.
       01 BUF       PIC X({size}).
       01 LEN       PIC 9(4).
       01 REPLY     PIC X(6) VALUE "PONG!!".
       01 HOLD      PIC X.
       PROCEDURE DIVISION.
       MAIN-P.
{before}
           DISPLAY "READY"
           RECEIVE RT GIVING BUF LEN
               ON EXCEPTION DISPLAY "SRV EXC " FUNCTION TRIM(FUNCTION EXCEPTION-STATUS)
               NOT ON EXCEPTION DISPLAY "SRV OK"
           END-RECEIVE
           DISPLAY "SRV GOT " BUF "/" LEN
{after}
           STOP RUN.
""";

    private const string ReplyAfter = """
           SEND TO RT FROM REPLY
               ON EXCEPTION DISPLAY "SRV SEND-EXC"
           END-SEND
           DISPLAY "SRV REPLIED".
""";

    private const string HoldAfter = """
           DISPLAY "SRV HOLDING"
           ACCEPT HOLD.
""";

    private const string ExitAfter = """
           DISPLAY "SRV LEAVING".
""";

    /// <summary>A requestor that SENDs, prints SENT, waits for a line on stdin (so a test can act between the two), RECEIVEs the reply and prints what happened.</summary>
    private static string Requestor(string name, string turn = "") => $"""
       IDENTIFICATION DIVISION.
       PROGRAM-ID. {name}.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MT        USAGE MESSAGE-TAG.
       01 REQ       PIC X(8) VALUE "HELLO".
       01 BUF       PIC X(12) VALUE "NOTHING".
       01 LEN       PIC 9(4) VALUE 9.
       01 GO-ON     PIC X.
       PROCEDURE DIVISION.
       MAIN-P.
{turn}
           SEND TO "srv " FROM REQ RETURNING MT
               ON EXCEPTION DISPLAY "CLI SEND-EXC " FUNCTION TRIM(FUNCTION EXCEPTION-STATUS)
           END-SEND
           DISPLAY "SENT"
           ACCEPT GO-ON
           RECEIVE MT GIVING BUF LEN
               ON EXCEPTION DISPLAY "CLI EXC " FUNCTION TRIM(FUNCTION EXCEPTION-STATUS)
               NOT ON EXCEPTION DISPLAY "CLI OK"
           END-RECEIVE
           DISPLAY "CLI GOT " BUF "/" LEN
           STOP RUN.
""";

    private LiveProcess Start(string module, string? serves = null)
    {
        var psi = new ProcessStartInfo("dotnet", $"\"{Path.Combine(_dir, module + ".dll")}\"") { WorkingDirectory = _dir };
        psi.Environment["COBOL_USER_LOCALE"] = CutRunner.LocalePin;
        psi.Environment["COBOL_SYSTEM_LOCALE"] = CutRunner.LocalePin;
        psi.Environment[CutRunner.McsDirectoryVariable] = _dir;
        if (serves is not null) psi.Environment["WISEOWL_COBOL_MCS_SERVER"] = serves;
        var live = LiveProcess.Start(psi);
        _live.Add(live);
        return live;
    }

    /// <summary>The sources are written in fixed-form columns and compiled as free form (their statements run past column 72): the seven leading columns are dropped.</summary>
    private void Compile(string module, string source) => NonCobolElement.CompileCobol(_dir, module,
        string.Join("\n", source.ReplaceLineEndings("\n").Split('\n').Select(line => line.StartsWith("       ", StringComparison.Ordinal) ? line[7..] : line)));

    [Fact]
    public void ARequestAndItsReplyCrossTwoProcesses_AndTheServerNameMatchesWithoutRegardToCase()
    {
        // §2023:14.9.38.4 GR4 (the SEND waits until the server run unit receives the message), GR6 (the tag), §2023:14.9.31.4 GR1 a) (the server's RECEIVE of NULL names its requestor),
        // GR3 (the length of the message), GR7 (the server answers the requestor it received) and §2023:14.9.31.4 GR1 b) (the requestor receives the answer by the server tag).
        // D-MCS-SERVER: a server name is compared with trailing spaces removed and without regard to case ("srv " serves SRV).
        Compile("PB4744S1", Server("PB4744S1", 20, ReplyAfter));
        Compile("PB4744C1", Requestor("PB4744C1"));
        var server = Start("PB4744S1", serves: "SRV");
        Assert.Equal("READY", server.ExpectLine());
        var client = Start("PB4744C1");
        Assert.Equal("SENT", client.ExpectLine());
        Assert.Equal("SRV OK", server.ExpectLine());
        Assert.Equal("SRV GOT HELLO               /0008", server.ExpectLine());   // GR3: eight positions; identifier-1 is space-filled to its 20
        Assert.Equal("SRV REPLIED", server.ExpectLine());
        client.Send("go");
        Assert.Equal("CLI OK", client.ExpectLine());
        Assert.Equal("CLI GOT PONG!!      /0006", client.ExpectLine());
        Assert.Equal(0, client.WaitForExit());
        Assert.Equal(0, server.WaitForExit());
    }

    [Fact]
    public void AServerKilledWithAnActiveMessage_IsEcMcsAbnormalTerminationAtTheRequestor()
    {
        // §2023:14.6.12 / Table 13: "Server failed before replying to message". The kill gives the server no epilogue, so the MCS answers for it (DESIGN 5.5).
        Compile("PB4744S2", Server("PB4744S2", 20, HoldAfter));
        Compile("PB4744C2", Requestor("PB4744C2", turn: "       >>TURN EC-MCS-ABNORMAL-TERMINATION CHECKING ON"));
        var server = Start("PB4744S2", serves: "SRV");
        Assert.Equal("READY", server.ExpectLine());
        var client = Start("PB4744C2");
        Assert.Equal("SENT", client.ExpectLine());
        Assert.Equal("SRV OK", server.ExpectLine());
        Assert.StartsWith("SRV GOT HELLO", server.ExpectLine());
        Assert.Equal("SRV HOLDING", server.ExpectLine());
        server.Kill();
        client.Send("go");
        Assert.Equal("CLI EXC EC-MCS-ABNORMAL-TERMINATION", client.ExpectLine());
        Assert.StartsWith("CLI GOT", client.ExpectLine());
        Assert.Equal(0, client.WaitForExit());
    }

    [Fact]
    public void AServerThatEndsNormallyWithoutReplying_AnswersEcMcsNormalTerminationOfSpaces()
    {
        // §2023:14.6.11 item 7: an implicit SEND ... RAISING EXCEPTION EC-MCS-NORMAL-TERMINATION for each message the run unit has not replied to, from a temporary
        // item "the size of the receiving data item in the associated RECEIVE statement" (here 12 spaces, the length of the requestor's own buffer so the reply fits), which no statement set.
        Compile("PB4744S3", Server("PB4744S3", 12, ExitAfter));
        Compile("PB4744C3", Requestor("PB4744C3", turn: "       >>TURN EC-MCS-NORMAL-TERMINATION CHECKING ON"));
        var server = Start("PB4744S3", serves: "SRV");
        Assert.Equal("READY", server.ExpectLine());
        var client = Start("PB4744C3");
        Assert.Equal("SENT", client.ExpectLine());
        Assert.Equal(0, server.WaitForExit());
        client.Send("go");
        Assert.Equal("CLI EXC EC-MCS-NORMAL-TERMINATION", client.ExpectLine());
        Assert.Equal("CLI GOT             /0012", client.ExpectLine());
        Assert.Equal(0, client.WaitForExit());
    }

    [Fact]
    public void AServerEndingWithoutReplyingToAnAreaWithNoAllocatedMaximum_AnswersSpacesOfTheSizeTheAreaTook()
    {
        // §2023:14.6.11 item 7 names the temporary's size, "the size of the receiving data item in the associated RECEIVE statement" (review R3-2 of PB4744). The server's
        // area holds an OCCURS DYNAMIC table FROM 3, so its RECEIVE recreates the table to the message's one occurrence raised to the minimum of 3 (§2023:14.6.9.2): the
        // area is 3 + 3 = 6 positions after a 4-position message, and the implicit SEND answers 6 spaces - neither the message's 4 nor an empty reply.
        const string server = """
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4744S7.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RT        USAGE MESSAGE-TAG.
       01 LEN       PIC 9(4).
       01 SV.
          05 SA PIC X(3).
          05 SE PIC X OCCURS DYNAMIC FROM 3 TO 9.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "READY"
           RECEIVE RT GIVING SV LEN
               ON EXCEPTION DISPLAY "SRV EXC " FUNCTION TRIM(FUNCTION EXCEPTION-STATUS)
           END-RECEIVE
           DISPLAY "SRV GOT " SA "/" LEN "/" FUNCTION LENGTH(SV)
           STOP RUN.
""";
        Compile("PB4744S7", server);
        Compile("PB4744C7", Requestor("PB4744C7", turn: "       >>TURN EC-MCS-NORMAL-TERMINATION CHECKING ON").Replace("VALUE \"HELLO\"", "VALUE \"HELL\"").Replace("PIC X(8)", "PIC X(4)"));
        var srv = Start("PB4744S7", serves: "SRV");
        Assert.Equal("READY", srv.ExpectLine());
        var client = Start("PB4744C7");
        Assert.Equal("SENT", client.ExpectLine());
        Assert.Equal("SRV GOT HEL/0004/000000006", srv.ExpectLine());
        Assert.Equal(0, srv.WaitForExit());
        client.Send("go");
        Assert.Equal("CLI EXC EC-MCS-NORMAL-TERMINATION", client.ExpectLine());
        Assert.Equal("CLI GOT             /0006", client.ExpectLine());
        Assert.Equal(0, client.WaitForExit());
    }

    [Fact]
    public void AMessageLongerThanIdentifier1_IsTakenAndEcMcsMessageLength_AndTheServerCanStillAnswerIt()
    {
        // §2023:14.9.31.4 GR3: a length exceeding identifier-1's "is set to exist and the execution of the RECEIVE statement is unsuccessful" (Q7: the message is taken, identifier-1
        // holds its first positions, data-name-1 identifies the sender so the server can answer).
        Compile("PB4744S4", Server("PB4744S4", 4, ReplyAfter, before: "       >>TURN EC-MCS-MESSAGE-LENGTH CHECKING ON"));
        Compile("PB4744C4", Requestor("PB4744C4"));
        var srv = Start("PB4744S4", serves: "SRV");
        Assert.Equal("READY", srv.ExpectLine());
        var client = Start("PB4744C4");
        Assert.Equal("SENT", client.ExpectLine());
        Assert.Equal("SRV EXC EC-MCS-MESSAGE-LENGTH", srv.ExpectLine());
        Assert.Equal("SRV GOT HELL/0000", srv.ExpectLine());                      // data-name-2 is unchanged (LEN has no VALUE: 0000) and identifier-1 holds the first 4 positions
        Assert.Equal("SRV REPLIED", srv.ExpectLine());
        client.Send("go");
        Assert.Equal("CLI OK", client.ExpectLine());
        Assert.Equal(0, client.WaitForExit());
        Assert.Equal(0, srv.WaitForExit());
    }

    [Fact]
    public void ARepliedExceptionNameReachesTheRequestorsReceive_AsAnUnsuccessfulRECEIVEWithThatCondition()
    {
        // §2023:14.9.38.4 GR8: "exception-name-1 ... is sent to the requestor program" (Q6): the requestor's RECEIVE moves the reply and is unsuccessful with the condition,
        // set to exist because the requestor's checking for it is enabled.
        const string reply = """
           SEND TO RT FROM REPLY RAISING EXCEPTION EC-SIZE-OVERFLOW
               ON EXCEPTION DISPLAY "SRV SEND-EXC"
           END-SEND
           DISPLAY "SRV REPLIED".
""";
        Compile("PB4744S5", Server("PB4744S5", 20, reply));
        Compile("PB4744C5", Requestor("PB4744C5", turn: "       >>TURN EC-SIZE-OVERFLOW CHECKING ON"));
        var server = Start("PB4744S5", serves: "SRV");
        Assert.Equal("READY", server.ExpectLine());
        var client = Start("PB4744C5");
        Assert.Equal("SENT", client.ExpectLine());
        Assert.Equal("SRV OK", server.ExpectLine());
        Assert.StartsWith("SRV GOT", server.ExpectLine());
        Assert.Equal("SRV REPLIED", server.ExpectLine());
        client.Send("go");
        Assert.Equal("CLI EXC EC-SIZE-OVERFLOW", client.ExpectLine());
        Assert.Equal("CLI GOT PONG!!      /0006", client.ExpectLine());
        Assert.Equal(0, client.WaitForExit());
    }

    [Fact]
    public void AReceiveThatPollsOrTimesOut_IsSuccessfulWithSpaces_AndANegativeIntervalIsEcContinueLessThanZero()
    {
        // §2023:14.9.31.4 GR2: with the CONTINUE phrase the statement completes when the CONTINUE parameters are completed, "the content of identifier-1 will be all space characters
        // and the RECEIVE statement is assumed to be successful" (Q5: MESSAGE RECEIVED polls; a zero interval polls; a negative interval is zero and sets
        // EC-CONTINUE-LESS-THAN-ZERO as §2023:14.9.9.4 GR1 b) does for CONTINUE).
        const string source = """
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4744P1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RT        USAGE MESSAGE-TAG.
       01 BUF       PIC X(6) VALUE "KEPT".
       01 LEN       PIC 9(4) VALUE 7.
       01 NEG       PIC S9 VALUE -1.
       PROCEDURE DIVISION.
       MAIN-P.
           >>TURN EC-CONTINUE-LESS-THAN-ZERO CHECKING ON
           RECEIVE RT GIVING BUF LEN CONTINUE AFTER MESSAGE RECEIVED
               ON EXCEPTION DISPLAY "A EXC"
               NOT ON EXCEPTION DISPLAY "A OK " BUF "/" LEN
           END-RECEIVE
           MOVE "KEPT" TO BUF
           MOVE 7 TO LEN
           RECEIVE RT GIVING BUF LEN CONTINUE AFTER 0 SECONDS
               ON EXCEPTION DISPLAY "B EXC"
               NOT ON EXCEPTION DISPLAY "B OK " BUF "/" LEN
           END-RECEIVE
           MOVE "KEPT" TO BUF
           MOVE 7 TO LEN
           RECEIVE RT GIVING BUF LEN CONTINUE AFTER NEG SECONDS
               ON EXCEPTION DISPLAY "C EXC"
               NOT ON EXCEPTION DISPLAY "C OK " BUF "/" LEN
           END-RECEIVE
           DISPLAY "STATUS " FUNCTION TRIM(FUNCTION EXCEPTION-STATUS)
           STOP RUN.
""";
        Compile("PB4744P1", source);
        var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(_dir, "PB4744P1.dll"), _dir, env: new Dictionary<string, string?> { ["WISEOWL_COBOL_MCS_SERVER"] = "POLLSRV" });
        Assert.Equal(0, exit);
        Assert.Equal("", stderr);
        Assert.Equal("A OK       /0000\nB OK       /0000\nC OK       /0000\nSTATUS EC-CONTINUE-LESS-THAN-ZERO", stdout);
    }

    private const string TaggedGroup = """
       01 TG IS TYPEDEF STRONG.
          05 TA PIC X(3).
          05 TT USAGE MESSAGE-TAG.
          05 TU USAGE MESSAGE-TAG OCCURS 2.
          05 TN.
             10 TM USAGE MESSAGE-TAG.
             10 TZ PIC X(2).
""";

    [Fact]
    public void AGroupHoldingMessageTags_CarriesItsTagsBesideItsImage_InStorageOrder()
    {
        // §2023:14.9.38.3 SR3 and §2023:14.9.31.3 SR2 do not refuse a message-tag item in identifier-1, and §2023:14.9.38.4 GR4 makes the message the content of it: the tags
        // (managed slots, no part of the storage image, DOC-A.1-212) travel beside the image and a RECEIVE stores them back in storage order (design section 4.4). The tag
        // the requestor sets is the server tag of its first message, so it is not null; the entries it leaves alone are null.
        string client = $"""
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4744C5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MT        USAGE MESSAGE-TAG.
       01 MT2       USAGE MESSAGE-TAG.
       01 REQ       PIC X(8) VALUE "FIRST".
{TaggedGroup}
       01 G1 TYPE TG.
       PROCEDURE DIVISION.
       MAIN-P.
           SEND TO "srv" FROM REQ RETURNING MT
               ON EXCEPTION DISPLAY "CLI SEND-EXC " FUNCTION TRIM(FUNCTION EXCEPTION-STATUS)
           END-SEND
           MOVE "ABC" TO TA OF G1
           MOVE "ZZ" TO TZ OF G1
           SET TT OF G1 TO MT
           SET TU OF G1 (2) TO MT
           SEND TO "srv" FROM G1 RETURNING MT2
               ON EXCEPTION DISPLAY "CLI SEND2-EXC " FUNCTION TRIM(FUNCTION EXCEPTION-STATUS)
           END-SEND
           DISPLAY "SENT"
           STOP RUN.
""";
        string server = $"""
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4744S5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RT        USAGE MESSAGE-TAG.
       01 BUF       PIC X(8).
       01 LEN       PIC 9(4).
{TaggedGroup}
       01 G2 TYPE TG.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "READY"
           RECEIVE RT GIVING BUF LEN
           RECEIVE RT GIVING G2 LEN
               ON EXCEPTION DISPLAY "SRV EXC " FUNCTION TRIM(FUNCTION EXCEPTION-STATUS)
           END-RECEIVE
           DISPLAY "TA=" TA OF G2 " TZ=" TZ OF G2 " LEN=" LEN
           IF TT OF G2 = NULL DISPLAY "TT NULL" ELSE DISPLAY "TT SET" END-IF
           IF TU OF G2 (1) = NULL DISPLAY "TU1 NULL" ELSE DISPLAY "TU1 SET" END-IF
           IF TU OF G2 (2) = NULL DISPLAY "TU2 NULL" ELSE DISPLAY "TU2 SET" END-IF
           IF TM OF G2 = NULL DISPLAY "TM NULL" ELSE DISPLAY "TM SET" END-IF
           IF TT OF G2 = TU OF G2 (2) DISPLAY "TT SAME AS TU2" END-IF
           STOP RUN.
""";
        Compile("PB4744S5", server);
        Compile("PB4744C5", client);
        var srv = Start("PB4744S5", serves: "SRV");
        Assert.Equal("READY", srv.ExpectLine());
        var cli = Start("PB4744C5");
        Assert.Equal("SENT", cli.ExpectLine());
        Assert.Equal("TA=ABC TZ=ZZ LEN=0037", srv.ExpectLine());
        Assert.Equal("TT SET", srv.ExpectLine());
        Assert.Equal("TU1 NULL", srv.ExpectLine());
        Assert.Equal("TU2 SET", srv.ExpectLine());
        Assert.Equal("TM NULL", srv.ExpectLine());
        Assert.Equal("TT SAME AS TU2", srv.ExpectLine());
        Assert.Equal(0, cli.WaitForExit());
        Assert.Equal(0, srv.WaitForExit());
    }

    private const string TaggedVarGroup = """
       01 TV IS TYPEDEF STRONG.
          05 VA PIC X(3).
          05 VT USAGE MESSAGE-TAG.
          05 VI PIC X OCCURS DYNAMIC FROM 2 TO 4.
""";

    [Fact]
    public void AVariableLengthGroupHoldingAMessageTag_CarriesItsTagBesideItsCurrentImage()
    {
        // §2023:14.9.38.3 SR3 and §2023:14.9.31.3 SR2 list neither OCCURS DYNAMIC nor the message tag, so a variable-length group that holds a tag is a legal message area:
        // the message is its current-extent image (§2023:14.9.38.4 GR4), 3 + 8 (the tag's token positions, which the receiver skips) + 2 elements, and the tag arrives beside it as a slot.
        string client = $"""
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4744C6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MT        USAGE MESSAGE-TAG.
       01 MT2       USAGE MESSAGE-TAG.
       01 REQ       PIC X(8) VALUE "FIRST".
{TaggedVarGroup}
       01 G1 TYPE TV.
       PROCEDURE DIVISION.
       MAIN-P.
           SEND TO "srv" FROM REQ RETURNING MT
               ON EXCEPTION DISPLAY "CLI SEND-EXC " FUNCTION TRIM(FUNCTION EXCEPTION-STATUS)
           END-SEND
           MOVE "ABC" TO VA OF G1
           SET VT OF G1 TO MT
           SEND TO "srv" FROM G1 RETURNING MT2
               ON EXCEPTION DISPLAY "CLI SEND2-EXC " FUNCTION TRIM(FUNCTION EXCEPTION-STATUS)
           END-SEND
           DISPLAY "SENT"
           STOP RUN.
""";
        string server = $"""
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4744S6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RT        USAGE MESSAGE-TAG.
       01 BUF       PIC X(8).
       01 LEN       PIC 9(4).
{TaggedVarGroup}
       01 G2 TYPE TV.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "READY"
           RECEIVE RT GIVING BUF LEN
           RECEIVE RT GIVING G2 LEN
               ON EXCEPTION DISPLAY "SRV EXC " FUNCTION TRIM(FUNCTION EXCEPTION-STATUS)
           END-RECEIVE
           DISPLAY "VA=" VA OF G2 " LEN=" LEN
           IF VT OF G2 = NULL DISPLAY "VT NULL" ELSE DISPLAY "VT SET" END-IF
           STOP RUN.
""";
        Compile("PB4744S6", server);
        Compile("PB4744C6", client);
        var srv = Start("PB4744S6", serves: "SRV");
        Assert.Equal("READY", srv.ExpectLine());
        var cli = Start("PB4744C6");
        Assert.Equal("SENT", cli.ExpectLine());
        Assert.Equal("VA=ABC LEN=0013", srv.ExpectLine());
        Assert.Equal("VT SET", srv.ExpectLine());
        Assert.Equal(0, cli.WaitForExit());
        int srvExit = srv.WaitForExit(); Assert.True(srvExit == 0, srv.Stderr);
    }

    [Fact]
    public void AnElementaryMessageTag_IsAnEightPositionMessage_WhoseTagTravelsAsItsSlot()
    {
        // An elementary message tag has 8 character positions (DOC-A.1-212; kb/Work PB4838 gave them the tag's token), so §2023:14.9.38.4 GR1's "shall not be zero" holds
        // and the message is 8 positions long (§2023:14.9.31.4 GR3); its content, the tag, travels beside the token as the message's one slot, as a tag inside a group does,
        // because the token names a tag of the sending run unit. The receiving tag is set; a tag of 8 positions receives a message of 8.
        const string client = """
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4744C9.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MT        USAGE MESSAGE-TAG.
       01 MT2       USAGE MESSAGE-TAG.
       01 REQ       PIC X(8) VALUE "FIRST".
       PROCEDURE DIVISION.
       MAIN-P.
           SEND TO "srv" FROM REQ RETURNING MT
               ON EXCEPTION DISPLAY "CLI SEND-EXC " FUNCTION TRIM(FUNCTION EXCEPTION-STATUS)
           END-SEND
           SEND TO "srv" FROM MT RETURNING MT2
               ON EXCEPTION DISPLAY "CLI SEND2-EXC " FUNCTION TRIM(FUNCTION EXCEPTION-STATUS)
           END-SEND
           DISPLAY "SENT"
           STOP RUN.
""";
        const string server = """
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4744S9.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RT        USAGE MESSAGE-TAG.
       01 T2        USAGE MESSAGE-TAG.
       01 BUF       PIC X(8).
       01 LEN       PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "READY"
           RECEIVE RT GIVING BUF LEN
           RECEIVE RT GIVING T2 LEN
               ON EXCEPTION DISPLAY "SRV EXC " FUNCTION TRIM(FUNCTION EXCEPTION-STATUS)
           END-RECEIVE
           DISPLAY "LEN=" LEN
           IF T2 = NULL DISPLAY "T2 NULL" ELSE DISPLAY "T2 SET" END-IF
           STOP RUN.
""";
        Compile("PB4744S9", server);
        Compile("PB4744C9", client);
        var srv = Start("PB4744S9", serves: "SRV");
        Assert.Equal("READY", srv.ExpectLine());
        var cli = Start("PB4744C9");
        Assert.Equal("SENT", cli.ExpectLine());
        Assert.Equal("LEN=0008", srv.ExpectLine());
        Assert.Equal("T2 SET", srv.ExpectLine());
        Assert.Equal(0, cli.WaitForExit());
        int srvExit = srv.WaitForExit(); Assert.True(srvExit == 0, srv.Stderr);
    }

    [Fact]
    public void ADynamicTableWhoseElementsHoldMessageTags_CarriesEachTagBesideItsElement()
    {
        // Review R3-1 of PB4744: §2023:14.9.38.3 SR3 and §2023:14.9.31.3 SR2 list neither OCCURS DYNAMIC nor the message tag, so a tag in an ELEMENT of a dynamic-capacity
        // table is a legal message area too. The message is the current-extent image, 3 + 2 elements of (8 + 1) = 21 positions; each element is recreated by its own
        // FromMessageImage (a tag's token positions name the sender's tag and are skipped), and the tags arrive beside the image in storage order: element 1's NULL, element 2's set.
        const string type = """
       01 TD IS TYPEDEF STRONG.
          05 DA PIC X(3).
          05 DL OCCURS DYNAMIC FROM 1 TO 4.
             10 DT USAGE MESSAGE-TAG.
             10 DX PIC X.
""";
        string client = $"""
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4744C8.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MT        USAGE MESSAGE-TAG.
       01 MT2       USAGE MESSAGE-TAG.
       01 REQ       PIC X(8) VALUE "FIRST".
{type}
       01 G1 TYPE TD.
       PROCEDURE DIVISION.
       MAIN-P.
           SEND TO "srv" FROM REQ RETURNING MT
               ON EXCEPTION DISPLAY "CLI SEND-EXC " FUNCTION TRIM(FUNCTION EXCEPTION-STATUS)
           END-SEND
           MOVE "ABC" TO DA OF G1
           MOVE "P" TO DX OF G1 (1)
           MOVE "Q" TO DX OF G1 (2)
           SET DT OF G1 (2) TO MT
           SEND TO "srv" FROM G1 RETURNING MT2
               ON EXCEPTION DISPLAY "CLI SEND2-EXC " FUNCTION TRIM(FUNCTION EXCEPTION-STATUS)
           END-SEND
           DISPLAY "SENT"
           STOP RUN.
""";
        string server = $"""
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4744S8.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RT        USAGE MESSAGE-TAG.
       01 BUF       PIC X(8).
       01 LEN       PIC 9(4).
{type}
       01 G2 TYPE TD.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "READY"
           RECEIVE RT GIVING BUF LEN
           RECEIVE RT GIVING G2 LEN
               ON EXCEPTION DISPLAY "SRV EXC " FUNCTION TRIM(FUNCTION EXCEPTION-STATUS)
           END-RECEIVE
           DISPLAY "DA=" DA OF G2 " DX=" DX OF G2 (1) DX OF G2 (2) " LEN=" LEN
           IF DT OF G2 (1) = NULL DISPLAY "DT1 NULL" ELSE DISPLAY "DT1 SET" END-IF
           IF DT OF G2 (2) = NULL DISPLAY "DT2 NULL" ELSE DISPLAY "DT2 SET" END-IF
           STOP RUN.
""";
        Compile("PB4744S8", server);
        Compile("PB4744C8", client);
        var srv = Start("PB4744S8", serves: "SRV");
        Assert.Equal("READY", srv.ExpectLine());
        var cli = Start("PB4744C8");
        Assert.Equal("SENT", cli.ExpectLine());
        Assert.Equal("DA=ABC DX=PQ LEN=0021", srv.ExpectLine());
        Assert.Equal("DT1 NULL", srv.ExpectLine());
        Assert.Equal("DT2 SET", srv.ExpectLine());
        Assert.Equal(0, cli.WaitForExit());
        int srvExit = srv.WaitForExit(); Assert.True(srvExit == 0, srv.Stderr);
    }

    [Fact]
    public void ASuccessfulReceiveWhoseNonfatalConditionADeclarativeHandled_DoesNotRunItsNotOnExceptionPhrase()
    {
        // §2023:14.6.13.1.4 3): "If a conditional phrase with a NOT phrase is specified in the interrupted statement, the imperative-statement in that phrase is not executed"
        // after a USE declarative handled the nonfatal condition (here EC-CONTINUE-LESS-THAN-ZERO of §2023:14.9.31.4 GR2 on a negative interval); without the declarative the
        // condition interrupts nothing and the NOT phrase runs.
        const string source = """
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4744P2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RT        USAGE MESSAGE-TAG.
       01 BUF       PIC X(6).
       01 LEN       PIC 9(4).
       01 NEG       PIC S9 VALUE -1.
       PROCEDURE DIVISION.
       DECLARATIVES.
       LTZ SECTION.
           USE AFTER EXCEPTION CONDITION EC-CONTINUE-LESS-THAN-ZERO.
       LTZ-P.
           DISPLAY "DECL LTZ".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           >>TURN EC-CONTINUE-LESS-THAN-ZERO CHECKING ON
           RECEIVE RT GIVING BUF LEN CONTINUE AFTER NEG SECONDS
               NOT ON EXCEPTION DISPLAY "NOT-PHRASE RAN"
           END-RECEIVE
           RECEIVE RT GIVING BUF LEN CONTINUE AFTER 0 SECONDS
               NOT ON EXCEPTION DISPLAY "POLL OK"
           END-RECEIVE
           DISPLAY "END"
           STOP RUN.
""";
        Compile("PB4744P2", source);
        var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(_dir, "PB4744P2.dll"), _dir, env: new Dictionary<string, string?> { ["WISEOWL_COBOL_MCS_SERVER"] = "POLLSRV2" });
        Assert.Equal(0, exit);
        Assert.Equal("", stderr);
        Assert.Equal("DECL LTZ\nPOLL OK\nEND", stdout);
    }

    // ---------------------------------------------------------------- activation (kb/Work PB4745; DESIGN-message-control.md §5.4.5, §7.2 "Across processes")

    /// <summary>Writes the MCS directory's <c>mcs.json</c>: one server <paramref name="name"/> whose <c>start</c> runs <paramref name="module"/>'s assembly with <c>dotnet</c>, and
    /// <paramref name="extra"/> (a JSON object's members, comma first) beside it.</summary>
    private void DefineServer(string name, string? module, string extra = "")
    {
        string start = module is null ? "" : $", \"start\": [\"dotnet\", {System.Text.Json.JsonSerializer.Serialize(Path.Combine(_dir, module + ".dll"))}]";
        File.WriteAllText(Path.Combine(_dir, "mcs.json"), $"{{ \"servers\": [ {{ \"name\": \"{name}\"{start}{extra} }} ] }}");
    }

    /// <summary>The logs of the servers a requestor started (<c>servers/&lt;N&gt;.&lt;r&gt;.log</c>, DESIGN-message-control.md §5.4.5).</summary>
    private string[] ServerLogs(string spelling) =>
        Directory.Exists(Path.Combine(_dir, "servers")) ? Directory.GetFiles(Path.Combine(_dir, "servers"), spelling + ".*.log") : [];

    /// <summary>A server log as it stands: the server that writes it may still be running, so it is read without excluding its writer.</summary>
    private static string ReadLog(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        return new StreamReader(stream).ReadToEnd();
    }

    /// <summary>A requestor that SENDs once and reports the SEND's outcome: <c>SENT</c>, or the exception condition set, which <paramref name="condition"/> enables.</summary>
    private static string OneSend(string name, string condition, string to = "srv", string message = "HELLO") => $"""
       IDENTIFICATION DIVISION.
       PROGRAM-ID. {name}.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MT        USAGE MESSAGE-TAG.
       01 REQ       PIC X(8) VALUE "{message}".
       PROCEDURE DIVISION.
       MAIN-P.
       >>TURN {condition} CHECKING ON
           SEND TO "{to}" FROM REQ RETURNING MT
               ON EXCEPTION DISPLAY "CLI SEND-EXC " FUNCTION TRIM(FUNCTION EXCEPTION-STATUS)
               NOT ON EXCEPTION DISPLAY "SENT"
           END-SEND
           STOP RUN.
""";

    /// <summary>A server that answers <paramref name="count"/> requests, one after the other, each with a reply.</summary>
    private static string TwoRequestServer(string name, int count) => $"""
       IDENTIFICATION DIVISION.
       PROGRAM-ID. {name}.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RT        USAGE MESSAGE-TAG.
       01 BUF       PIC X(8).
       01 LEN       PIC 9(4).
       01 REPLY     PIC X(6) VALUE "PONG!!".
       01 N         PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "READY"
           PERFORM {count} TIMES
               SET RT TO NULL
               RECEIVE RT GIVING BUF LEN
               END-RECEIVE
               SEND TO RT FROM REPLY
               END-SEND
               ADD 1 TO N
               DISPLAY "SRV SERVED " N
           END-PERFORM
           STOP RUN.
""";

    [Fact]
    public void ASendToAServerNobodyServes_StartsItFromItsDefinition_AndTheServerLogsToItsLog()
    {
        // §2023:14.9.38.4 GR4 sends the message to the server run unit; the MCS finds none, and the definition's start command (DESIGN §5.4.5, §0 Q3) runs in the MCS directory
        // with WISEOWL_COBOL_MCS_SERVER naming the server, _NOTIFY the requestor's notify socket and _LOG the server's log. The started run unit serves "SRV", takes the request, and
        // answers it. Its standard output is the log (servers/<N>.<r>.log), not the requestor's console.
        Compile("PB4745S1", Server("PB4745S1", 20, ReplyAfter));
        Compile("PB4745C1", Requestor("PB4745C1"));
        DefineServer("Srv", "PB4745S1");
        var client = Start("PB4745C1");
        Assert.Equal("SENT", client.ExpectLine());
        client.Send("go");
        Assert.Equal("CLI OK", client.ExpectLine());
        Assert.Equal("CLI GOT PONG!!      /0006", client.ExpectLine());
        Assert.Equal(0, client.WaitForExit());
        string[] logs = ServerLogs("SRV");
        Assert.Single(logs);
        string log = ReadLog(logs[0]);
        Assert.Contains("READY", log);                                   // printed before the reply that the requestor has now received
        Assert.Contains("SRV OK", log);
        Assert.Contains("SRV GOT HELLO               /0008", log);
        Assert.Empty(client.Stderr);
    }

    [Fact]
    public void TwoRequestorsRacingToActivateOneName_LaunchItOnce()
    {
        // DESIGN §5.4.5: the requestor that holds start/<N>.lock launches the server and keeps the lock until the server has published its listener; the other, which waited
        // for the lock, enumerates again and finds that server. One log is one launch, and the one server answers both.
        Compile("PB4745S2", TwoRequestServer("PB4745S2", 2));
        Compile("PB4745C2A", Requestor("PB4745C2A"));
        Compile("PB4745C2B", Requestor("PB4745C2B"));
        DefineServer("SRV", "PB4745S2");
        var first = Start("PB4745C2A");
        var second = Start("PB4745C2B");
        Assert.Equal("SENT", first.ExpectLine());
        Assert.Equal("SENT", second.ExpectLine());
        first.Send("go");
        second.Send("go");
        Assert.Equal("CLI OK", first.ExpectLine());
        Assert.Equal("CLI OK", second.ExpectLine());
        Assert.Equal("CLI GOT PONG!!      /0006", first.ExpectLine());
        Assert.Equal("CLI GOT PONG!!      /0006", second.ExpectLine());
        Assert.Equal(0, first.WaitForExit());
        Assert.Equal(0, second.WaitForExit());
        Assert.Single(ServerLogs("SRV"));
    }

    [Fact]
    public void AStartCommandThatEndsBeforeItsServerIsPublished_IsEcMcsImp_AndWhatItPrintedIsInTheLog()
    {
        // DESIGN §5.4.5: "a process that exits first is EC-MCS-IMP" (§0 Q8: every failure of the MCS itself, a server that cannot be started among them). The start command is dotnet
        // run on an assembly that does not exist, which ends at once with a message the requestor drains into the server's log.
        Compile("PB4745C3", OneSend("PB4745C3", "EC-MCS-IMP"));
        File.WriteAllText(Path.Combine(_dir, "mcs.json"),
            $"{{ \"servers\": [ {{ \"name\": \"SRV\", \"start\": [\"dotnet\", {System.Text.Json.JsonSerializer.Serialize(Path.Combine(_dir, "NoSuchServer.dll"))}] }} ] }}");
        var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(_dir, "PB4745C3.dll"), _dir);
        Assert.Equal("", stderr);
        Assert.Equal(0, exit);
        Assert.Equal("CLI SEND-EXC EC-MCS-IMP", stdout);
        string[] logs = ServerLogs("SRV");
        Assert.Single(logs);
        Assert.NotEmpty(ReadLog(logs[0]));
    }

    [Fact]
    public void AStartCommandThatCannotBeStarted_IsEcMcsImp()
    {
        // DESIGN §5.4.5 and §0 Q8: a program the host cannot start is a server that cannot be started.
        Compile("PB4745C4", OneSend("PB4745C4", "EC-MCS-IMP"));
        File.WriteAllText(Path.Combine(_dir, "mcs.json"), """{ "servers": [ { "name": "SRV", "start": "no-such-program-pb4745 --serve" } ] }""");
        var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(_dir, "PB4745C4.dll"), _dir);
        Assert.Equal("", stderr);
        Assert.Equal(0, exit);
        Assert.Equal("CLI SEND-EXC EC-MCS-IMP", stdout);
    }

    [Theory]
    [InlineData("{ \"servers\": [ ")]
    [InlineData("""{ "servers": [ { "name": "SRV", "maxLength": 0 } ] }""")]
    [InlineData("""{ "servers": [ { "name": "SRV" }, { "name": "srv " } ] }""")]
    [InlineData("""{ "servers": [ { "start": "x" } ] }""")]
    public void AnUnusableServersDefinition_IsEcMcsImpAtTheFirstMessagingStatement(string json)
    {
        // §0 Q8: "EC-MCS-IMP covers every failure of the MCS itself: an unusable mcs.json". The definitions are read when the run unit's messaging starts, so the SEND is
        // unsuccessful whatever name it addresses.
        Compile("PB4745C5", OneSend("PB4745C5", "EC-MCS-IMP"));
        File.WriteAllText(Path.Combine(_dir, "mcs.json"), json);
        var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(_dir, "PB4745C5.dll"), _dir);
        Assert.Equal("", stderr);
        Assert.Equal(0, exit);
        Assert.Equal("CLI SEND-EXC EC-MCS-IMP", stdout);
    }

    [Fact]
    public void AMessageLongerThanTheServersMaxLength_IsEcMcsMessageLength_BeforeAnyServerIsLookedFor()
    {
        // §2023:14.9.38.4 GR1: the SEND is unsuccessful when the message is "larger than the message server allows"; this implementation's server allows what mcs.json defines
        // as the name's maxLength (docs/CONFORMANCE.md D-MCS-LENGTH). The message is 8 positions and the limit 4, and no server runs and none can be started.
        Compile("PB4745C6", OneSend("PB4745C6", "EC-MCS-MESSAGE-LENGTH"));
        DefineServer("SRV", null, ", \"maxLength\": 4");
        var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(_dir, "PB4745C6.dll"), _dir);
        Assert.Equal("", stderr);
        Assert.Equal(0, exit);
        Assert.Equal("CLI SEND-EXC EC-MCS-MESSAGE-LENGTH", stdout);
    }
}
