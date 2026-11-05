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

    /// <summary>A.2.5.3 GR5 b): "If the NO DATA phrase is not specified ... execution of the run unit is suspended until data is made available". Another run unit makes it available.</summary>
    [Fact]
    public async Task ReceiveWithoutNoData_WaitsForAnotherRunUnit()
    {
        var waiter = Session();
        var received = Task.Run(() => waiter.Receive("C", CommunicationFormat.Input, InputArea("Q2"), false, "....", noDataPhrase: false));
        Assert.Equal("00", OutputStatus(Session().Send("C", CommunicationFormat.Output, OutputArea(4, "D2"), "ABCD", SendIndicator.EndOfMessage)));
        var receipt = await received.WaitAsync(TimeSpan.FromMinutes(2));
        Assert.Equal("ABCD", receipt.Into);
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
