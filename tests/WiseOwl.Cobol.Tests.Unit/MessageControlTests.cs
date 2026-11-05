// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Runtime.Communication;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The message control system's failure paths and its lifetime rules, which a compiled program's one run unit cannot reach: an unreadable definition file and a store the host refuses
/// (ISO/IEC 1989:2002 Table A.1's implementor-defined 9x, docs/CONFORMANCE.md), a message crossing run units (E.5.5.1), and ISO/IEC 1989:2002 14.5.10 rule 2's end of a partly sent or
/// partly received message. Each area below is the image a compiled program hands over, built by position from A.1.2.3 GR2 and GR16, so a wrong offset fails here too.
/// </summary>
public sealed class MessageControlTests : IDisposable
{
    private readonly string _root = ScratchDirectory.Create("WiseOwlCobol_Mcs_");

    public void Dispose() => ScratchDirectory.Remove(_root);

    private const string Definitions = """
        {
          "queues": [ { "name": "Q1", "messages": [ { "segments": [ "SEED" ], "source": "T1", "date": "260102", "time": "03040506" } ] }, { "name": "Q2" } ],
          "destinations": [ { "name": "D2", "deliverTo": "Q2", "source": "T1" }, { "name": "T9", "deliverTo": "terminal:T1" } ],
          "terminals": [ { "name": "T1" } ]
        }
        """;

    private MessageControlSession Session(string definitions = Definitions, params string[] scheduled)
    {
        File.WriteAllText(Path.Combine(_root, "mcs.json"), definitions);
        return new MessageControlSession(MessageStore.OpenAt(_root, scheduled));
    }

    /// <summary>GR2's 87 positions: the four queue levels, the date and time, the source, then the text length, end key, status key and message count.</summary>
    private static string InputArea(string queue) => queue.PadRight(48) + new string('0', 14) + new string(' ', 12) + "0000" + " " + "99" + "000000";

    private static string InputStatus(string area) => area.Substring(79, 2);

    private static string InputCount(string area) => area.Substring(81, 6);

    /// <summary>GR16: destination count, text length, status key, then one error key and destination per occurrence.</summary>
    private static string OutputArea(int textLength, string destination) => "0001" + textLength.ToString("D4") + "99" + " " + destination.PadRight(12);

    private static string OutputStatus(string area) => area.Substring(8, 2);

    /// <summary>GR24's 33 positions: date, time, terminal, text length, end key, status key.</summary>
    private static string InOutArea(string terminal, int textLength = 0) =>
        new string('0', 14) + terminal.PadRight(12) + textLength.ToString("D4") + " " + "99";

    private static string InOutStatus(string area) => area.Substring(31, 2);

    [Fact]
    public void NoDefinitionFile_EverySymbolicNameIsUnknown()
    {
        var session = new MessageControlSession(MessageStore.OpenAt(_root, []));
        Assert.Equal("20", InputStatus(session.AcceptMessageCount(InputArea("Q1"))));
        Assert.Equal("20", OutputStatus(session.Send("C", CommunicationFormat.Output, OutputArea(1, "D1"), "A", SendIndicator.EndOfMessage)));
        Assert.Equal("21", InOutStatus(session.Receive("C", CommunicationFormat.InputOutput, InOutArea("T1"), false, "X", noDataPhrase: false).Area));
        Assert.False(File.Exists(Path.Combine(_root, "mcs-state.json")), "an MCS nobody defined writes nothing");
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("[]")]
    [InlineData("""{ "queues": [ { "key": "NO NAME" } ] }""")]
    [InlineData("""{ "queues": [ { "name": "THIRTEENCHARS" } ] }""")]
    [InlineData("""{ "queues": [ { "name": "Q" }, { "name": "Q" } ] }""")]
    [InlineData("""{ "queues": [ { "name": "A", "subQueues": [ { "name": "B", "subQueues": [ { "name": "C", "subQueues": [ { "name": "D", "subQueues": [ { "name": "E" } ] } ] } ] } ] } ] }""")]
    [InlineData("""{ "sources": [ { "name": "S" }, { "name": "S" } ] }""")]
    [InlineData("""{ "terminals": [ { "name": "T" }, { "name": "T" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "D" }, { "name": "D" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "D", "deliverTo": "NOWHERE" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "D", "deliverTo": "terminal:NOWHERE" } ] }""")]
    [InlineData("""{ "queues": [ { "name": "Q", "messages": [ { "segments": [] } ] } ] }""")]
    [InlineData("""{ "queues": [ { "name": "Q1" } ], "queues": [ { "name": "Q2" } ] }""")]
    [InlineData("""{ "queues": [ { "name": "Q", "name": "R" } ] }""")]
    [InlineData("""{ "queues": [ { "name": "Q", "messages": [ { "segments": [ "A" ], "segments": [ "B" ] } ] } ] }""")]
    [InlineData("""{ "queues": [ { "name": "A/B" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "../EVIL" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "C:EVIL" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "D:" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "A:B" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "a<b" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "a>b" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "a|b" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "a?b" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "a*b" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "a\"b" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "a\u0001b" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "a\u001Fb" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "a\u0000b" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "CON" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "con" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "Prn" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "AUX" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "NUL" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "COM1" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "com9" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "COM0" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "LPT1" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "Lpt9" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "LPT0" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "COM¹" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "LPT³" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "CONIN$" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "CONOUT$" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "NUL.x" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "Con.Out" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "AUX " } ] }""")]
    [InlineData("""{ "queues": [ { "name": "C:Q" } ] }""")]
    [InlineData("""{ "queues": [ { "name": "A", "subQueues": [ { "name": "NUL" } ] } ] }""")]
    [InlineData("""{ "sources": [ { "name": "S:1" } ] }""")]
    [InlineData("""{ "terminals": [ { "name": "COM3" } ] }""")]
    public void UnusableDefinitionFile_IsStatus91_OnEveryStatement(string definitions)
    {
        var session = Session(definitions);
        Assert.Equal("91", InputStatus(session.AcceptMessageCount(InputArea("Q"))));
        Assert.Equal("91", InputStatus(session.SetPath("C", CommunicationFormat.Input, CommunicationPath.Input, InputArea("Q"), null, enable: true)));
        Assert.Equal("91", OutputStatus(session.Purge("C", CommunicationFormat.Output, OutputArea(1, "D"))));
        Assert.Equal("91", OutputStatus(session.Send("C", CommunicationFormat.Output, OutputArea(1, "D"), "A", SendIndicator.EndOfMessage)));
        var receipt = session.Receive("C", CommunicationFormat.Input, InputArea("Q"), false, "XY", noDataPhrase: true);
        Assert.Equal("91", InputStatus(receipt.Area));
        Assert.True(receipt.DataAvailable, "an MCS that cannot answer takes no action and does not run NO DATA");
        Assert.Equal("XY", receipt.Into);
    }

    /// <summary>A symbolic name is a file-name component on every host (a device file <c>&lt;name&gt;.out</c> is built from it), so the screen is the same on
    /// Windows and Linux: a name that merely resembles a separator-free, non-device one is still a name.</summary>
    [Theory]
    [InlineData("CONSOLE")]
    [InlineData("COM10")]
    [InlineData("COMX")]
    [InlineData("LPT")]
    [InlineData("NULL")]
    [InlineData("A.B")]
    [InlineData("A B")]
    [InlineData("A$")]
    public void NameResemblingAReservedOne_IsDefined(string name)
    {
        var session = Session($$"""{ "queues": [ { "name": "{{name}}" } ] }""");
        Assert.Equal("00", InputStatus(session.AcceptMessageCount(InputArea(name))));
    }

    [Fact]
    public void StateTheHostRefuses_IsStatus92_AndTakesNoAction()
    {
        var session = Session();
        Directory.CreateDirectory(Path.Combine(_root, "mcs-state.json"));   // the store's file cannot be read: a directory stands in its place
        var receipt = session.Receive("C", CommunicationFormat.Input, InputArea("Q1"), false, "XXXX", noDataPhrase: false);
        Assert.Equal("92", InputStatus(receipt.Area));
        Assert.Equal("XXXX", receipt.Into);
        Assert.Equal("92", OutputStatus(session.Send("C", CommunicationFormat.Output, OutputArea(1, "D2"), "A", SendIndicator.EndOfMessage)));
    }

    [Fact]
    public void CorruptState_IsStatus92()
    {
        var session = Session();
        File.WriteAllText(Path.Combine(_root, "mcs-state.json"), "{ truncated");
        Assert.Equal("92", InputStatus(session.AcceptMessageCount(InputArea("Q1"))));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("""{ "queues": [ null ], "sources": [], "destinations": [], "terminals": [] }""")]
    [InlineData("""{ "queues": [ { "path": "Q1" } ], "sources": [], "destinations": [], "terminals": [] }""")]
    [InlineData("""{ "queues": [], "queues": [], "sources": [], "destinations": [], "terminals": [] }""")]
    [InlineData("""{ "queues": [ { "path": "Q1", "path": "Q2", "enabled": true, "messages": [] } ], "sources": [], "destinations": [], "terminals": [] }""")]
    public void StateOfTheWrongShape_IsStatus92(string state)
    {
        var session = Session();
        File.WriteAllText(Path.Combine(_root, "mcs-state.json"), state);
        Assert.Equal("92", InputStatus(session.AcceptMessageCount(InputArea("Q1"))));
    }

    /// <summary>Table A.1's PURGE column and Table A.2's error keys, on the arms the goldens do not reach: a disabled destination with a portion is 10 and key 2, and with an unknown destination beside it 80;
    /// an input-output CD's disabled terminal is 10; a store that refuses a SEND leaves the portion the run unit had.</summary>
    [Fact]
    public void Purge_FollowsTheTables()
    {
        var session = Session("""{ "destinations": [ { "name": "OFF", "enabled": false } ], "terminals": [ { "name": "T1" } ] }""");
        Assert.Equal("10", OutputStatus(session.Send("C", CommunicationFormat.Output, OutputArea(1, "OFF"), "A", SendIndicator.None)));
        string purged = session.Purge("C", CommunicationFormat.Output, OutputArea(1, "OFF"));
        Assert.Equal("10", OutputStatus(purged));
        Assert.Equal('2', purged[10]);
        Assert.Equal("70", OutputStatus(session.Purge("C", CommunicationFormat.Output, OutputArea(1, "OFF"))));
        session.Send("C", CommunicationFormat.Output, OutputArea(1, "OFF"), "A", SendIndicator.None);
        string two = "0002" + "0001" + "99" + " " + "NOPE".PadRight(12) + " " + "OFF".PadRight(12);
        string combined = session.Purge("C", CommunicationFormat.Output, two);
        Assert.Equal("80", OutputStatus(combined));
        Assert.Equal('1', combined[10]);
        Assert.Equal('2', combined[23]);
        session.Send("I", CommunicationFormat.InputOutput, InOutArea("T1", 1), "A", SendIndicator.None);
        session.SetPath("I", CommunicationFormat.InputOutput, CommunicationPath.InputOutputTerminal, InOutArea("T1"), null, enable: false);
        Assert.Equal("10", InOutStatus(session.Purge("I", CommunicationFormat.InputOutput, InOutArea("T1"))));
    }

    [Fact]
    public void SendTheStoreRefuses_KeepsThePortionTheRunUnitHad()
    {
        var session = Session();
        Assert.Equal("00", OutputStatus(session.Send("C", CommunicationFormat.Output, OutputArea(1, "D2"), "A", SendIndicator.None)));
        using (RefusedState())
            Assert.Equal("92", OutputStatus(session.Send("C", CommunicationFormat.Output, OutputArea(1, "D2"), "B", SendIndicator.EndOfMessage)));
        Assert.Equal("00", OutputStatus(session.Send("C", CommunicationFormat.Output, OutputArea(1, "D2"), "C", SendIndicator.EndOfMessage)));
        Assert.Equal("ACX", session.Receive("R", CommunicationFormat.Input, InputArea("Q2"), false, "XXX", noDataPhrase: true).Into);
    }

    /// <summary>A directory in the state file's place until the scope ends: the store reads no state and cannot commit one. The end of the scope puts the file back.</summary>
    private RefusedStateScope RefusedState() => new(Path.Combine(_root, "mcs-state.json"));

    private sealed class RefusedStateScope : IDisposable
    {
        private readonly string _state;
        private readonly string? _saved;

        internal RefusedStateScope(string state)
        {
            _state = state;
            _saved = File.Exists(state) ? File.ReadAllText(state) : null;
            File.Delete(state);
            Directory.CreateDirectory(state);
        }

        public void Dispose()
        {
            Directory.Delete(_state);
            if (_saved is not null) File.WriteAllText(_state, _saved);
        }
    }

    /// <summary>The refusal Table A.1's 9x names: no action. A program that retries a SEND on 92 delivers the message once.</summary>
    [Fact]
    public void SendTheStoreRefuses_TransmitsNothing_AndItsRetryDeliversOnce()
    {
        var session = Session("""{ "destinations": [ { "name": "DEV" } ] }""");
        string device = Path.Combine(_root, "DEV.out");
        Assert.Equal("00", OutputStatus(session.Send("C", CommunicationFormat.Output, OutputArea(5, "DEV"), "FIRST", SendIndicator.EndOfMessage)));
        string first = "FIRST" + Environment.NewLine;
        Assert.Equal(first, File.ReadAllText(device));
        string area = OutputArea(5, "DEV");
        using (RefusedState())
            for (int attempt = 0; attempt < 2; attempt++)
            {
                Assert.Equal(area[..8] + "92" + area[10..], session.Send("C", CommunicationFormat.Output, area, "HELLO", SendIndicator.EndOfMessage));
                Assert.Equal(first, File.ReadAllText(device));
            }
        Assert.Equal("00", OutputStatus(session.Send("C", CommunicationFormat.Output, area, "HELLO", SendIndicator.EndOfMessage)));
        Assert.Equal(first + "HELLO" + Environment.NewLine, File.ReadAllText(device));
    }

    /// <summary>A device that did not exist before the refused statement does not exist after it.</summary>
    [Fact]
    public void SendTheStoreRefuses_LeavesNoNewDevice()
    {
        var session = Session("""{ "terminals": [ { "name": "T1" } ] }""");
        using (RefusedState())
        {
            Assert.Equal("92", InOutStatus(session.Send("I", CommunicationFormat.InputOutput, InOutArea("T1", 2), "XY", SendIndicator.EndOfMessage)));
            Assert.False(File.Exists(Path.Combine(_root, "T1.out")));
        }
    }

    /// <summary>A device the host refuses: the statement answers 92, what the devices before it took goes back, and the state stays, so a retry finds the message unsent.</summary>
    [Fact]
    public void DeviceTheHostRefuses_IsStatus92_AndTakesBackTheOtherDevices()
    {
        var session = Session("""{ "destinations": [ { "name": "GOOD" }, { "name": "BAD" } ] }""");
        string good = Path.Combine(_root, "GOOD.out"), bad = Path.Combine(_root, "BAD.out");
        string two = "0002" + "0002" + "99" + " " + "GOOD".PadRight(12) + " " + "BAD".PadRight(12);
        Assert.Equal("00", OutputStatus(session.Send("C", CommunicationFormat.Output, OutputArea(1, "GOOD"), "0", SendIndicator.EndOfMessage)));
        Directory.CreateDirectory(bad);
        Assert.Equal(two[..8] + "92" + two[10..], session.Send("C", CommunicationFormat.Output, two, "AB", SendIndicator.EndOfMessage));
        Assert.Equal("0" + Environment.NewLine, File.ReadAllText(good));
        Directory.Delete(bad);
        Assert.Equal("00", OutputStatus(session.Send("C", CommunicationFormat.Output, two, "AB", SendIndicator.EndOfMessage)));
        Assert.Equal("0" + Environment.NewLine + "AB" + Environment.NewLine, File.ReadAllText(good));
        Assert.Equal("AB" + Environment.NewLine, File.ReadAllText(bad));
    }

    /// <summary>RECEIVE with a store that refuses: the receiver and the area stay as they were but for the status key, and the message stays the store's, whole.</summary>
    [Fact]
    public void ReceiveTheStoreRefuses_ReportsNoTransfer()
    {
        var session = Session();
        string area = InputArea("Q1");
        using (RefusedState())
        {
            var receipt = session.Receive("C", CommunicationFormat.Input, area, false, "XXXX", noDataPhrase: true);
            Assert.Equal(area[..79] + "92" + area[81..], receipt.Area);
            Assert.Equal("XXXX", receipt.Into);
            Assert.True(receipt.DataAvailable);
        }
        var healed = session.Receive("C", CommunicationFormat.Input, area, false, "XXXX", noDataPhrase: true);
        Assert.Equal("00", InputStatus(healed.Area));
        Assert.Equal("SEED", healed.Into);
    }

    /// <summary>The same for the remainder the run unit holds: a refused RECEIVE does not advance it.</summary>
    [Fact]
    public void ReceiveOfARemainderTheStoreRefuses_KeepsItsPlace()
    {
        var session = Session();
        Assert.Equal("SE", session.Receive("C", CommunicationFormat.Input, InputArea("Q1"), false, "..", noDataPhrase: true).Into);
        using (RefusedState())
            Assert.Equal("..", session.Receive("C", CommunicationFormat.Input, InputArea("Q1"), false, "..", noDataPhrase: true).Into);
        Assert.Equal("ED", session.Receive("C", CommunicationFormat.Input, InputArea("Q1"), false, "..", noDataPhrase: true).Into);
    }

    /// <summary>PURGE with a store that refuses keeps the portion and the error keys.</summary>
    [Fact]
    public void PurgeTheStoreRefuses_KeepsThePortion()
    {
        var session = Session();
        string area = OutputArea(1, "D2");
        session.Send("C", CommunicationFormat.Output, area, "A", SendIndicator.None);
        using (RefusedState())
            Assert.Equal(area[..8] + "92" + area[10..], session.Purge("C", CommunicationFormat.Output, area));
        Assert.Equal("00", OutputStatus(session.Purge("C", CommunicationFormat.Output, area)));
        Assert.Equal("70", OutputStatus(session.Purge("C", CommunicationFormat.Output, area)));
    }

    /// <summary>ENABLE and DISABLE with a store that refuses: the path stays as it was, an output CD's error keys are not written, and A.1.2.3 GR30's "first statement" is not one that took no action.</summary>
    [Fact]
    public void PathsTheStoreRefuses_StayAsTheyWere()
    {
        var session = Session();
        string area = OutputArea(0, "D2");
        using (RefusedState())
        {
            Assert.Equal(area[..8] + "92" + area[10..], session.SetPath("C", CommunicationFormat.Output, CommunicationPath.Output, area, null, enable: false));
            Assert.Equal("92", InOutStatus(session.SetPath("I", CommunicationFormat.InputOutput, CommunicationPath.InputOutputTerminal, InOutArea("NOPE"), null, enable: false)));
        }
        Assert.Equal("15", OutputStatus(session.SetPath("C", CommunicationFormat.Output, CommunicationPath.Output, area, null, enable: true)));
        Assert.Equal("15", InOutStatus(session.SetPath("I", CommunicationFormat.InputOutput, CommunicationPath.InputOutputTerminal, InOutArea("T1"), null, enable: true)));
    }

    /// <summary>ACCEPT MESSAGE COUNT with a store that cannot read its state: the count stays the program's.</summary>
    [Fact]
    public void AcceptTheStoreRefuses_LeavesTheCount()
    {
        var session = Session();
        File.WriteAllText(Path.Combine(_root, "mcs-state.json"), "{ truncated");
        string area = InputArea("Q1")[..81] + "123456";
        Assert.Equal(area[..79] + "92" + area[81..], session.AcceptMessageCount(area));
    }

    /// <summary>A sharing violation on the lock is another run unit's statement, and is waited out: the statement completes once the lock is free.</summary>
    [Fact]
    public void LockAnotherRunUnitHolds_IsWaitedOut()
    {
        Session();
        var other = new FileStream(Path.Combine(_root, "mcs.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        int waits = 0;
        var session = new MessageControlSession(MessageStore.OpenAt(_root, [], new LockWait(5, () =>
        {
            waits++;
            other.Dispose();
        })));
        Assert.Equal("00", OutputStatus(session.Send("C", CommunicationFormat.Output, OutputArea(4, "D2"), "ABCD", SendIndicator.EndOfMessage)));
        Assert.Equal(1, waits);
        Assert.Equal("ABCD", session.Receive("R", CommunicationFormat.Input, InputArea("Q2"), false, "....", noDataPhrase: true).Into);
    }

    /// <summary>A lock never released is status 92 after the retries, never a run unit hung for ever, and the statement took no action.</summary>
    [Fact]
    public void LockNeverReleased_IsStatus92_AfterTheRetries()
    {
        Session();
        int waits = 0;
        var session = new MessageControlSession(MessageStore.OpenAt(_root, [], new LockWait(3, () => waits++)));
        string area = OutputArea(4, "D2");
        using (new FileStream(Path.Combine(_root, "mcs.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            Assert.Equal(area[..8] + "92" + area[10..], session.Send("C", CommunicationFormat.Output, area, "ABCD", SendIndicator.EndOfMessage));
        Assert.Equal(3, waits);
        Assert.False(session.Receive("R", CommunicationFormat.Input, InputArea("Q2"), false, "....", noDataPhrase: true).DataAvailable);
    }

    /// <summary>A refusal that is no sharing violation is no other run unit's statement: status 92 at once, with no wait.</summary>
    [Fact]
    public void LockTheHostRefuses_IsStatus92_WithNoWait()
    {
        string gone = Path.Combine(_root, "gone");
        Directory.CreateDirectory(gone);
        File.WriteAllText(Path.Combine(gone, "mcs.json"), Definitions);
        int waits = 0;
        var session = new MessageControlSession(MessageStore.OpenAt(gone, [], new LockWait(3, () => waits++)));
        ScratchDirectory.Remove(gone);
        Assert.Equal("92", OutputStatus(session.Send("C", CommunicationFormat.Output, OutputArea(4, "D2"), "ABCD", SendIndicator.EndOfMessage)));
        Assert.Equal(0, waits);
    }

    [Fact]
    public void Message_OutlivesTheRunUnitThatSentIt()
    {
        Session().Send("C", CommunicationFormat.Output, OutputArea(4, "D2"), "ABCD", SendIndicator.EndOfMessage);
        var other = Session();
        var receipt = other.Receive("C", CommunicationFormat.Input, InputArea("Q2"), false, "......", noDataPhrase: true);
        Assert.True(receipt.DataAvailable);
        Assert.Equal("ABCD..", receipt.Into);
        Assert.Equal("00", InputStatus(receipt.Area));
        Assert.Equal("T1".PadRight(12), receipt.Area.Substring(62, 12));
        Assert.Equal("000000", InputCount(other.AcceptMessageCount(InputArea("Q2"))));
    }

    [Fact]
    public void SeededMessage_EntersTheQueueOnce()
    {
        var first = Session();
        var receipt = first.Receive("C", CommunicationFormat.Input, InputArea("Q1"), false, "....", noDataPhrase: true);
        Assert.Equal("SEED", receipt.Into);
        Assert.Equal("260102", receipt.Area.Substring(48, 6));
        Assert.Equal("03040506", receipt.Area.Substring(54, 8));
        Assert.False(Session().Receive("C", CommunicationFormat.Input, InputArea("Q1"), false, "....", noDataPhrase: true).DataAvailable);
    }

    /// <summary>ISO/IEC 1989:2002 14.5.10 rule 2: "Any portions of messages transferred from the run unit by a SEND statement, and not terminated by an EMI or EGI, are purged from the system."</summary>
    [Fact]
    public void PortionNotEnded_IsPurgedWithTheRunUnit()
    {
        Assert.Equal("00", OutputStatus(Session().Send("C", CommunicationFormat.Output, OutputArea(1, "D2"), "A", SendIndicator.None)));
        var next = Session();
        Assert.Equal("00", OutputStatus(next.Send("C", CommunicationFormat.Output, OutputArea(1, "D2"), "B", SendIndicator.EndOfMessage)));
        Assert.Equal("BX", next.Receive("C", CommunicationFormat.Input, InputArea("Q2"), false, "XX", noDataPhrase: true).Into);
    }

    /// <summary>The same rule's other half: "remaining portions of any partially-received messages are eliminated from the queue."</summary>
    [Fact]
    public void RemainderNotReceived_IsEliminatedWithTheRunUnit()
    {
        Assert.Equal("SE", Session().Receive("C", CommunicationFormat.Input, InputArea("Q1"), false, "..", noDataPhrase: true).Into);
        Assert.False(Session().Receive("C", CommunicationFormat.Input, InputArea("Q1"), false, "..", noDataPhrase: true).DataAvailable);
    }

    /// <summary>A.2.5.3 GR5 b): "If the NO DATA phrase is not specified ... execution of the run unit is suspended until data is made available". Another run unit makes it available while this
    /// one waits: the send happens at the wait, so the RECEIVE has found the queue empty first.</summary>
    [Fact]
    public void ReceiveWithoutNoData_WaitsForAnotherRunUnit()
    {
        var sender = Session();
        int waits = 0;
        var waiter = new MessageControlSession(MessageStore.OpenAt(_root, []), awaitData: () =>
        {
            waits++;
            Assert.Equal("00", OutputStatus(sender.Send("C", CommunicationFormat.Output, OutputArea(4, "D2"), "ABCD", SendIndicator.EndOfMessage)));
        });
        var receipt = waiter.Receive("C", CommunicationFormat.Input, InputArea("Q2"), false, "....", noDataPhrase: false);
        Assert.Equal(1, waits);
        Assert.Equal("ABCD", receipt.Into);
        Assert.True(receipt.DataAvailable);
    }

    [Fact]
    public void DeliveryToATerminal_ReachesItsReceive()
    {
        var session = Session();
        Assert.Equal("00", OutputStatus(session.Send("C", CommunicationFormat.Output, OutputArea(2, "T9"), "HI", SendIndicator.EndOfGroup)));
        var receipt = session.Receive("I", CommunicationFormat.InputOutput, InOutArea("T1"), false, "..", noDataPhrase: true);
        Assert.Equal("HI", receipt.Into);
        Assert.Equal('3', receipt.Area[30]);
        Assert.Equal("0002", receipt.Area.Substring(26, 4));
    }

    /// <summary>A device of this MCS is its file; a terminal's and a destination's message arrives whole, and an empty segment adds nothing to it.</summary>
    [Fact]
    public void Transmission_AppendsTheWholeMessageToTheDevice()
    {
        var session = Session("""{ "destinations": [ { "name": "DEV" } ], "terminals": [ { "name": "T1" } ] }""");
        session.Send("C", CommunicationFormat.Output, OutputArea(2, "DEV"), "AB", SendIndicator.EndOfSegment);
        session.Send("C", CommunicationFormat.Output, OutputArea(1, "DEV"), "C", SendIndicator.EndOfMessage);
        session.Send("I", CommunicationFormat.InputOutput, InOutArea("T1", 2), "XY", SendIndicator.EndOfMessage);
        Assert.Equal("ABC" + Environment.NewLine, File.ReadAllText(Path.Combine(_root, "DEV.out")));
        Assert.Equal("XY" + Environment.NewLine, File.ReadAllText(Path.Combine(_root, "T1.out")));
    }

    /// <summary>A.1.2.3 GR6: "the symbolic names of the queue structure that demanded this activity will be placed" in the INITIAL CD; "In all other cases" spaces.</summary>
    [Fact]
    public void InitialArea_NamesWhatScheduledTheRunUnit_OrSpaces()
    {
        string scheduled = Session(Definitions, "Q1", "SUB").InitialArea("C", CommunicationFormat.Input, InputArea("OLD"));
        Assert.Equal("Q1".PadRight(12) + "SUB".PadRight(12) + new string(' ', 24), scheduled[..48]);
        string notScheduled = Session().InitialArea("C", CommunicationFormat.Input, InputArea("OLD"));
        Assert.Equal(new string(' ', 48), notScheduled[..48]);
        Assert.Equal(new string(' ', 12), Session().InitialArea("I", CommunicationFormat.InputOutput, InOutArea("OLD")).Substring(14, 12));
    }

    /// <summary>A.1.2.3 GR30: when the MCS scheduled the run unit, the terminal's name "shall not be changed by the program" from the first statement on.</summary>
    [Fact]
    public void ScheduledTerminal_ChangedBeforeTheFirstStatement_IsUnknown()
    {
        var session = Session(Definitions, "T1");
        Assert.Equal("T1".PadRight(12), session.InitialArea("I", CommunicationFormat.InputOutput, InOutArea("")).Substring(14, 12));
        Assert.Equal("21", InOutStatus(session.Receive("I", CommunicationFormat.InputOutput, InOutArea("T2"), false, ".", noDataPhrase: true).Area));
        Assert.Equal("00", InOutStatus(session.Receive("I", CommunicationFormat.InputOutput, InOutArea("T1"), false, ".", noDataPhrase: true).Area));
    }

    /// <summary>A.1.2.3 GR5: "When a given level of the queue structure is specified all higher levels shall also be specified."</summary>
    [Fact]
    public void LevelBelowABlankLevel_DesignatesNothing()
    {
        string area = InputArea("Q1");
        area = area[..24] + "SUB".PadRight(12) + area[36..];
        Assert.Equal("20", InputStatus(Session().AcceptMessageCount(area)));
    }

    /// <summary>Table A.3: "Any character other than 1, 2, or 3 will be interpreted as 0."</summary>
    [Theory]
    [InlineData("0", SendIndicator.None)]
    [InlineData("1", SendIndicator.EndOfSegment)]
    [InlineData("2", SendIndicator.EndOfMessage)]
    [InlineData("3", SendIndicator.EndOfGroup)]
    [InlineData("4", SendIndicator.None)]
    [InlineData("X", SendIndicator.None)]
    public void Indicator_FollowsTableA3(string image, SendIndicator expected) =>
        Assert.Equal(expected, MessageControl.IndicatorOf(image));

    /// <summary>The compiler's implicit description and the runtime's positions are written from the same three rules, once each: the sizes A.1.2.3 GR2, GR16 and GR24 state hold of the table.</summary>
    [Theory]
    [InlineData(CommunicationFormat.Input, 87)]
    [InlineData(CommunicationFormat.InputOutput, 33)]
    [InlineData(CommunicationFormat.Output, 10 + 13)]
    public void ImplicitDescription_HasTheAreaTheRulesState(CommunicationFormat format, int size) =>
        Assert.Equal(size, CommunicationFormats.Header(format).Concat(CommunicationFormats.Entry(format)).Sum(s => s.Size));

    /// <summary>Every clause the grammar reads names a field of a format, and the words a message quotes are distinct, so a clause added to the grammar has a row or fails here.</summary>
    [Fact]
    public void EveryField_BelongsToAFormat()
    {
        var printed = new[] { CommunicationFormat.Input, CommunicationFormat.InputOutput, CommunicationFormat.Output }
            .SelectMany(f => CommunicationFormats.Header(f).Concat(CommunicationFormats.Entry(f)))
            .Select(s => s.Field).ToHashSet();
        foreach (var field in Enum.GetValues<CommunicationField>())
            Assert.Contains(field, printed);
        Assert.Equal(Enum.GetValues<CommunicationField>().Length,
            Enum.GetValues<CommunicationField>().Select(f => CommunicationFormats.ClauseWords(f)).Distinct().Count());
    }
}
