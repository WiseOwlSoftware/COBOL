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
}
