// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using Microsoft.Win32.SafeHandles;
using WiseOwl.Cobol.Runtime.Communication;
using WiseOwl.Cobol.Runtime.IO;
using WiseOwl.Cobol.Runtime.IO.Store;
using WiseOwl.Cobol.Tests.Shared;
using WiseOwl.Cobol.Tests.Unit.Store;
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
    private readonly List<MessageControlSession> _sessions = [];
    private MediumFault? _fault;

    /// <summary>The bytes of a file a session may hold open: read with the sharing the store's own handle admits.</summary>
    private static byte[] Bytes(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    /// <summary>The end of the run unit for every session the case opened (<c>RunUnit.Terminate</c> does it for a compiled program): the store's handle is closed before the directory goes.</summary>
    public void Dispose()
    {
        foreach (var session in _sessions) session.Dispose();
        _fault?.Dispose();
        ScratchDirectory.Remove(_root);
    }

    /// <summary>A session over the MCS directory <paramref name="root"/> (the case's own by default), closed when the case ends. The first one also installs the case's
    /// <see cref="MediumFault"/> on the engine's one medium seam, so every store the case opens runs through it.</summary>
    private MessageControlSession Open(string? root = null, string[]? scheduled = null, Action? awaitData = null)
    {
        _fault ??= new MediumFault();
        var session = new MessageControlSession(MessageStore.OpenAt(root ?? _root, scheduled ?? []), awaitData);
        _sessions.Add(session);
        return session;
    }

    internal const string Definitions = """
        {
          "queues": [ { "name": "Q1", "messages": [ { "segments": [ "SEED" ], "source": "T1", "date": "260102", "time": "03040506" } ] }, { "name": "Q2" } ],
          "destinations": [ { "name": "D2", "deliverTo": "Q2", "source": "T1" }, { "name": "T9", "deliverTo": "terminal:T1" } ],
          "terminals": [ { "name": "T1" } ]
        }
        """;

    private MessageControlSession Session(string definitions = Definitions, params string[] scheduled)
    {
        File.WriteAllText(Path.Combine(_root, "mcs.json"), definitions);
        return Open(scheduled: scheduled);
    }

    /// <summary>GR2's 87 positions: the four queue levels, the date and time, the source, then the text length, end key, status key and message count.</summary>
    internal static string InputArea(string queue) => queue.PadRight(48) + new string('0', 14) + new string(' ', 12) + "0000" + " " + "99" + "000000";

    internal static string InputStatus(string area) => area.Substring(79, 2);

    internal static string InputCount(string area) => area.Substring(81, 6);

    /// <summary>GR16: destination count, text length, status key, then one error key and destination per occurrence.</summary>
    internal static string OutputArea(int textLength, string destination) => "0001" + textLength.ToString("D4") + "99" + " " + destination.PadRight(12);

    internal static string OutputStatus(string area) => area.Substring(8, 2);

    /// <summary>GR24's 33 positions: date, time, terminal, text length, end key, status key.</summary>
    internal static string InOutArea(string terminal, int textLength = 0) =>
        new string('0', 14) + terminal.PadRight(12) + textLength.ToString("D4") + " " + "99";

    internal static string InOutStatus(string area) => area.Substring(31, 2);

    [Fact]
    public void NoDefinitionFile_EverySymbolicNameIsUnknown()
    {
        var session = Open();
        Assert.Equal("20", InputStatus(session.AcceptMessageCount(InputArea("Q1"))));
        Assert.Equal("20", OutputStatus(session.Send("C", CommunicationFormat.Output, OutputArea(1, "D1"), "A", SendIndicator.EndOfMessage)));
        Assert.Equal("21", InOutStatus(session.Receive("C", CommunicationFormat.InputOutput, InOutArea("T1"), false, "X", noDataPhrase: false).Area));
        Assert.False(File.Exists(Path.Combine(_root, "mcs.store")), "an MCS nobody defined creates no store");
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
    [InlineData("""{ "destinations": [ { "name": "D1" }, { "name": "d1" } ] }""")]
    [InlineData("""{ "terminals": [ { "name": "T1" }, { "name": "t1" } ] }""")]
    [InlineData("""{ "terminals": [ { "name": "X" } ], "destinations": [ { "name": "x" } ] }""")]
    [InlineData("""{ "terminals": [ { "name": "X" } ], "destinations": [ { "name": "X" } ] }""")]
    [InlineData("""{ "destinations": [ { "name": "D", "capacity": -1 } ] }""")]
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
    /// Windows and Linux, and it refuses a name only by a character or by a whole device name: one that merely resembles a device name (CONSOLE, COM10, COMX, LPT,
    /// NULL) is still a name.</summary>
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

    /// <summary>Only a device file can be shared: a destination that feeds a queue has none, and a queue, a source and a device may hold names that differ only in case.</summary>
    [Theory]
    [InlineData("""{ "queues": [ { "name": "Q" } ], "destinations": [ { "name": "D1" }, { "name": "d1", "deliverTo": "Q" } ] }""")]
    [InlineData("""{ "queues": [ { "name": "d1" } ], "sources": [ { "name": "D1" } ], "destinations": [ { "name": "D1" } ] }""")]
    public void NamesThatShareNoDeviceFile_DifferingOnlyInCaseAreDefined(string definitions)
    {
        var session = Session(definitions);
        Assert.Equal("00", OutputStatus(session.Send("C", CommunicationFormat.Output, OutputArea(1, "D1"), "A", SendIndicator.EndOfMessage)));
    }

    [Fact]
    public void StoreTheHostRefuses_IsStatus92_AndTakesNoAction()
    {
        var session = Session();
        using (RefusedState())
        {
            var receipt = session.Receive("C", CommunicationFormat.Input, InputArea("Q1"), false, "XXXX", noDataPhrase: false);
            Assert.Equal("92", InputStatus(receipt.Area));
            Assert.Equal("XXXX", receipt.Into);
            Assert.Equal("92", OutputStatus(session.Send("C", CommunicationFormat.Output, OutputArea(1, "D2"), "A", SendIndicator.EndOfMessage)));
        }
        Assert.Equal("SEED", session.Receive("C", CommunicationFormat.Input, InputArea("Q1"), false, "XXXX", noDataPhrase: true).Into);
        Assert.Equal("000000", InputCount(session.AcceptMessageCount(InputArea("Q2"))));
    }

    /// <summary>A file at <c>mcs.store</c> that no message store wrote, or that a refused flush left damaged: the engine finds no committed meta, and the statement answers 92.</summary>
    [Fact]
    public void CorruptStore_IsStatus92()
    {
        var session = Session();
        File.WriteAllText(Path.Combine(_root, "mcs.store"), "{ truncated");
        Assert.Equal("92", InputStatus(session.AcceptMessageCount(InputArea("Q1"))));
    }

    /// <summary>A keyed file someone put at <c>mcs.store</c> is refused, never misread (DESIGN-communication-facility.md §8): the engine will not adopt a store of the other family.</summary>
    [Fact]
    public void KeyedFileAtTheStorePath_IsStatus92_AndIsLeftAsItWas()
    {
        string path = Path.Combine(_root, "mcs.store");
        Session();
        using (var stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            Assert.Equal(StoreStatus.Ok, PagedFile.Create(new HandlePageMedium(stream.SafeFileHandle), StoreKind.Of(StoreKit.Relative), coherent: true,
                HoldingsKeyOrder.RecordNumbers, out _, out _));
        byte[] before = Bytes(path);
        var session = Open();
        Assert.Equal("92", InputStatus(session.AcceptMessageCount(InputArea("Q1"))));
        Assert.Equal("92", OutputStatus(session.Send("C", CommunicationFormat.Output, OutputArea(1, "D2"), "A", SendIndicator.EndOfMessage)));
        Assert.Equal(before, Bytes(path));
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

    /// <summary>The host refuses every read, write and flush of <c>mcs.store</c> until the scope ends: the store can neither be entered nor landed on. The end of the scope lets the
    /// host answer again.</summary>
    private RefusedScope RefusedState()
    {
        _fault ??= new MediumFault();
        _fault.Refusing = true;
        return new RefusedScope(_fault);
    }

    private sealed class RefusedScope(MediumFault fault) : IDisposable
    {
        public void Dispose() => fault.Refusing = false;
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

    /// <summary>Device files are output already delivered (DESIGN-communication-facility.md §6 "Devices"): a new store, as after its deletion, adopts the length of the file a device
    /// has, so a statement that never transmits to it leaves it whole and the first transmission appends after it.</summary>
    [Fact]
    public void NewStoreOverAnExistingDeviceFile_KeepsTheDeliveredOutput()
    {
        var session = Session("""{ "queues": [ { "name": "Q" } ], "destinations": [ { "name": "D1" } ] }""");
        string device = Path.Combine(_root, "D1.out");
        File.WriteAllText(device, "DELIVERED" + Environment.NewLine);
        Assert.Equal("00", InputStatus(session.AcceptMessageCount(InputArea("Q"))));
        Assert.Equal("DELIVERED" + Environment.NewLine, File.ReadAllText(device));
        Assert.Equal("00", OutputStatus(session.Send("C", CommunicationFormat.Output, OutputArea(3, "D1"), "NEW", SendIndicator.EndOfMessage)));
        Assert.Equal("DELIVERED" + Environment.NewLine + "NEW" + Environment.NewLine, File.ReadAllText(device));
    }

    /// <summary>A device the host will not open for write refuses no statement that does not transmit to it: a file at its committed end is never opened for write, and the cut of a
    /// longer one is best effort (the tail stays until the next transmission overwrites it).</summary>
    [Fact]
    public void DeviceTheHostWillNotOpenForWrite_RefusesNoStatementThatDoesNotTransmitToIt()
    {
        var session = Session("""{ "queues": [ { "name": "Q" } ], "destinations": [ { "name": "D1" } ] }""");
        string device = Path.Combine(_root, "D1.out");
        Assert.Equal("00", OutputStatus(session.Send("C", CommunicationFormat.Output, OutputArea(3, "D1"), "ONE", SendIndicator.EndOfMessage)));
        string delivered = File.ReadAllText(device);
        session.Dispose();
        File.SetAttributes(device, FileAttributes.ReadOnly);
        try
        {
            var atItsEnd = Open();
            Assert.Equal("00", InputStatus(atItsEnd.AcceptMessageCount(InputArea("Q"))));
            Assert.Equal(delivered, File.ReadAllText(device));
            atItsEnd.Dispose();
            File.SetAttributes(device, FileAttributes.Normal);
            File.AppendAllText(device, "DEAD TAIL");
            File.SetAttributes(device, FileAttributes.ReadOnly);
            var pastItsEnd = Open();
            Assert.Equal("00", InputStatus(pastItsEnd.AcceptMessageCount(InputArea("Q"))));
        }
        finally
        {
            File.SetAttributes(device, FileAttributes.Normal);
        }
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

    /// <summary>ACCEPT MESSAGE COUNT with a store the host refuses: the count stays the program's.</summary>
    [Fact]
    public void AcceptTheStoreRefuses_LeavesTheCount()
    {
        var session = Session();
        string area = InputArea("Q1")[..81] + "123456";
        using (RefusedState())
            Assert.Equal(area[..79] + "92" + area[81..], session.AcceptMessageCount(area));
        Assert.Equal("000001", InputCount(session.AcceptMessageCount(InputArea("Q1"))));
    }

    /// <summary>⛔ ONE STATEMENT IS ONE STATEMENT OF THE STORE, UNDER ITS MUTEX (DESIGN-communication-facility.md §6 "The transaction"): while a statement reads the store, another run unit's
    /// endpoint on the same store finds the mutex held, and after the statement it is free. The engine's medium seam observes the read, so the check is made inside the statement and no delay orders it.</summary>
    [Fact]
    public void Statement_HoldsTheStoreMutex_UntilItIsDone()
    {
        var session = Session();
        session.AcceptMessageCount(InputArea("Q1"));
        using var other = new FileStream(Path.Combine(_root, "mcs.store"), FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        using var endpoint = HostRegionLocks.Open(other.SafeFileHandle, other.Name, writable: true);
        RegionLockResult? seen = null;
        _fault!.OnRead = () => seen ??= endpoint.Hold(HostRegionLocks.StoreMutexByte, exclusive: true, wait: false);
        Assert.Equal("00", InputStatus(session.AcceptMessageCount(InputArea("Q1"))));
        _fault.OnRead = null;
        Assert.Equal(RegionLockResult.Held, seen);
        Assert.Equal(RegionLockResult.Ok, endpoint.Hold(HostRegionLocks.StoreMutexByte, exclusive: true, wait: false));
    }

    /// <summary>⛔ THE REGION-LOCK REFUSAL (DESIGN-communication-facility.md §6 "A host whose locks answer Unavailable"): a host that cannot carry the store mutex is refused with 92 before the statement reads
    /// anything, for every statement kind, and mcs.store and every device are left byte for byte as they were. The MCS never runs unlocked and never relies on a share-none open.</summary>
    [Fact]
    public void HostWhoseLocksAreUnavailable_IsStatus92_ForEveryStatement_AndLeavesTheFilesAsTheyWere()
    {
        var first = Session("""{ "queues": [ { "name": "Q1", "messages": [ "SEED" ] } ], "destinations": [ { "name": "DEV" } ], "terminals": [ { "name": "T1" } ] }""");
        Assert.Equal("00", OutputStatus(first.Send("C", CommunicationFormat.Output, OutputArea(3, "DEV"), "OLD", SendIndicator.EndOfMessage)));
        first.Dispose();
        byte[] store = Bytes(Path.Combine(_root, "mcs.store")), device = Bytes(Path.Combine(_root, "DEV.out"));
        using (HostRegionLocks.Forcing(new UnavailableHost()))
        {
            var session = Open();
            string output = OutputArea(3, "DEV"), input = InputArea("Q1"), inOut = InOutArea("T1", 2);
            Assert.Equal(output[..8] + "92" + output[10..], session.Send("C", CommunicationFormat.Output, output, "NEW", SendIndicator.EndOfMessage));
            Assert.Equal(output[..8] + "92" + output[10..], session.SetPath("C", CommunicationFormat.Output, CommunicationPath.Output, output, null, enable: false));
            Assert.Equal(output[..8] + "92" + output[10..], session.Purge("C", CommunicationFormat.Output, output));
            Assert.Equal(input[..79] + "92" + input[81..], session.AcceptMessageCount(input));
            Assert.Equal(input[..79] + "92" + input[81..], session.SetPath("C", CommunicationFormat.Input, CommunicationPath.Input, input, null, enable: false));
            var receipt = session.Receive("C", CommunicationFormat.Input, input, false, "XXXX", noDataPhrase: true);
            Assert.Equal(input[..79] + "92" + input[81..], receipt.Area);
            Assert.Equal("XXXX", receipt.Into);
            Assert.Equal(inOut[..31] + "92", session.Send("I", CommunicationFormat.InputOutput, inOut, "XY", SendIndicator.EndOfMessage));
        }
        Assert.Equal(store, Bytes(Path.Combine(_root, "mcs.store")));
        Assert.Equal(device, Bytes(Path.Combine(_root, "DEV.out")));
        Assert.False(File.Exists(Path.Combine(_root, "T1.out")));
        var after = Open();
        Assert.Equal("SEED", after.Receive("C", CommunicationFormat.Input, InputArea("Q1"), false, "....", noDataPhrase: true).Into);
    }

    /// <summary>A store file the host cannot open (the MCS directory is gone) is status 92 at once, and nothing waits.</summary>
    [Fact]
    public void StoreFileTheHostRefuses_IsStatus92()
    {
        string gone = Path.Combine(_root, "gone");
        Directory.CreateDirectory(gone);
        File.WriteAllText(Path.Combine(gone, "mcs.json"), Definitions);
        var session = Open(gone);
        ScratchDirectory.Remove(gone);
        Assert.Equal("92", OutputStatus(session.Send("C", CommunicationFormat.Output, OutputArea(4, "D2"), "ABCD", SendIndicator.EndOfMessage)));
    }

    /// <summary>⛔ A STATEMENT REFUSED AT ANY WRITE ANSWERS COMPLETED OR NO ACTION, NEVER BOTH (§6 "Status 92 takes no action"): for every write of a SEND, the host refuses it and every later
    /// one. The statement then answers 92 and the store holds no message, or answers 00 and holds exactly one; it never answers 92 for a message that landed, nor 00 for one that did not.</summary>
    [Fact]
    public void SendRefusedAtEachWrite_AnswersNoActionOrCompleted_NeverBoth()
    {
        const string definitions = """{ "queues": [ { "name": "Q" } ], "destinations": [ { "name": "D", "deliverTo": "Q" } ] }""";
        int refused = 0;
        for (int from = 0; ; from++)
        {
            string root = Path.Combine(_root, "at" + from);
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "mcs.json"), definitions);
            var session = Open(root);
            Assert.Equal("00", InputStatus(session.AcceptMessageCount(InputArea("Q"))));
            _fault!.RefuseWritesFrom = from;
            string status = OutputStatus(session.Send("C", CommunicationFormat.Output, OutputArea(1, "D"), "M", SendIndicator.EndOfMessage));
            _fault.RefuseWritesFrom = null;
            string count = InputCount(session.AcceptMessageCount(InputArea("Q")));
            if (status == "00")
            {
                Assert.Equal("000001", count);
                break;
            }
            Assert.Equal("92", status);
            Assert.Equal("000000", count);
            refused++;
            Assert.True(from < 50, "the statement never completed");
        }
        Assert.True(refused > 0, "the matrix refused no write");
    }

    /// <summary>The reopen that decides a refused statement is itself refused: the session cannot tell whether the statement landed, and answers 93. The next statement opens the store again.</summary>
    [Fact]
    public void RefusedStatementWhoseReopenIsRefusedToo_IsStatus93_AndTheSessionOpensTheStoreAgain()
    {
        var session = Session("""{ "queues": [ { "name": "Q" } ], "destinations": [ { "name": "D", "deliverTo": "Q" } ] }""");
        session.AcceptMessageCount(InputArea("Q"));
        _fault!.RefuseWritesFrom = 0;
        _fault.ReadsFailAfterARefusedWrite = true;
        string area = OutputArea(1, "D");
        Assert.Equal(area[..8] + "93" + area[10..], session.Send("C", CommunicationFormat.Output, area, "M", SendIndicator.EndOfMessage));
        _fault.RefuseWritesFrom = null;
        _fault.ReadsFailAfterARefusedWrite = false;
        Assert.Equal("00", InputStatus(session.AcceptMessageCount(InputArea("Q"))));
    }

    private sealed class UnavailableHost : IRegionLockHost
    {
        public RegionLocks Open(SafeFileHandle handle, string hostPath, bool writable) => new Endpoint(writable);

        /// <summary>An endpoint whose every operation answers <see cref="RegionLockResult.Unavailable"/>, as a file system with no locks does.</summary>
        private sealed class Endpoint(bool writable) : RegionLocks(writable)
        {
            internal override bool ExclusiveNeedsWritableHandle => false;

            internal override RegionLockResult Hold(long offset, bool exclusive, bool wait) => RegionLockResult.Unavailable;

            internal override RegionLockResult HeldByAnother(long offset) => RegionLockResult.Unavailable;

            internal override void Release(long offset) { }

            public override void Dispose() { }
        }
    }

    /// <summary>⛔ SEEDING (DESIGN-communication-facility.md §6 "Seeding"): a changed <c>mcs.json</c> keeps a kept endpoint's enabled state and messages and takes its new definition, seeds a new endpoint
    /// once, and deletes a removed one with its messages and counts. The store holds no row of the removed queue, and a run unit that opens the store again seeds nothing a second time.</summary>
    [Fact]
    public void ChangedDefinitions_KeepWhatIsKept_SeedWhatIsNew_AndDeleteWhatIsRemoved()
    {
        File.WriteAllText(Path.Combine(_root, "mcs.json"),
            """{ "queues": [ { "name": "Q1", "key": "OLD", "messages": [ "A" ] }, { "name": "Q2", "messages": [ "GONE" ] } ] }""");
        var first = Open();
        Assert.Equal("00", InputStatus(first.SetPath("C", CommunicationFormat.Input, CommunicationPath.Input, InputArea("Q1"), "OLD", enable: false)));
        Assert.Equal("000001", InputCount(first.AcceptMessageCount(InputArea("Q2"))));
        File.WriteAllText(Path.Combine(_root, "mcs.json"),
            """{ "queues": [ { "name": "Q1", "key": "NEW", "messages": [ "IGNORED" ] }, { "name": "Q3", "messages": [ "N" ] } ] }""");
        var second = Open();
        Assert.Equal("40", InputStatus(second.SetPath("C", CommunicationFormat.Input, CommunicationPath.Input, InputArea("Q1"), "OLD", enable: true)));   // the new key
        Assert.Equal("00", InputStatus(second.SetPath("C", CommunicationFormat.Input, CommunicationPath.Input, InputArea("Q1"), "NEW", enable: true)));   // it was kept disabled
        Assert.Equal("000001", InputCount(second.AcceptMessageCount(InputArea("Q1"))));   // A, not IGNORED
        Assert.Equal("000001", InputCount(second.AcceptMessageCount(InputArea("Q3"))));
        Assert.Equal("20", InputStatus(second.AcceptMessageCount(InputArea("Q2"))));
        string[] queues = [.. McsInspector.Rows(_root).Where(r => r.Key[0] is (byte)'M' or (byte)'N').Select(r => System.Text.Encoding.UTF8.GetString(r.Key, 1, r.Key.Length - 1))];
        Assert.DoesNotContain(queues, q => q.StartsWith("Q2", StringComparison.Ordinal));
        McsInspector.AssertCountsEqualARecount(_root);
        Assert.Equal("N", second.Receive("C", CommunicationFormat.Input, InputArea("Q3"), false, ".", noDataPhrase: true).Into);
        Assert.False(Open().Receive("C", CommunicationFormat.Input, InputArea("Q3"), false, ".", noDataPhrase: true).DataAvailable);   // the third run unit seeded nothing again
    }

    /// <summary>⛔ DEVICES (§6 "Devices"): a message is written at the device's committed end, so a tail a dead statement left past it is overwritten by the next transmission, and the first statement of a
    /// run unit cuts it.</summary>
    [Fact]
    public void DeviceTail_PastTheCommittedEnd_IsOverwrittenByTheNextTransmission_AndCutByTheNextRunUnit()
    {
        string device = Path.Combine(_root, "DEV.out");
        var first = Session("""{ "destinations": [ { "name": "DEV" } ] }""");
        Assert.Equal("00", OutputStatus(first.Send("C", CommunicationFormat.Output, OutputArea(3, "DEV"), "ONE", SendIndicator.EndOfMessage)));
        File.AppendAllText(device, "JUNK-OF-A-DEAD-STATEMENT");
        Assert.Equal("00", OutputStatus(first.Send("C", CommunicationFormat.Output, OutputArea(3, "DEV"), "TWO", SendIndicator.EndOfMessage)));
        Assert.Equal("ONE" + Environment.NewLine + "TWO" + Environment.NewLine, File.ReadAllText(device));
        File.AppendAllText(device, "MORE-JUNK");
        Assert.Equal("000000", InputCount(Open().AcceptMessageCount(InputArea("Q"))));
        Assert.Equal("ONE" + Environment.NewLine + "TWO" + Environment.NewLine, File.ReadAllText(device));
    }

    /// <summary>The byte <see cref="HostRegionLocks.McsPresenceByte"/> says a run unit has the store open: held shared from its first statement to the end of the run unit (<c>Dispose</c>, which
    /// <c>RunUnit.Terminate</c> calls), and visible to another handle on the same store.</summary>
    [Fact]
    public void SessionHoldsThePresenceByte_UntilTheRunUnitEnds()
    {
        var session = Session();
        session.AcceptMessageCount(InputArea("Q1"));
        using var other = new FileStream(Path.Combine(_root, "mcs.store"), FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        using var endpoint = HostRegionLocks.Open(other.SafeFileHandle, other.Name, writable: true);
        Assert.Equal(RegionLockResult.Held, endpoint.HeldByAnother(HostRegionLocks.McsPresenceByte));
        session.Dispose();
        Assert.Equal(RegionLockResult.Ok, endpoint.HeldByAnother(HostRegionLocks.McsPresenceByte));
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
        var waiter = Open(awaitData: () =>
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
        string scheduled = Session(Definitions, "Q1", "SUB").InitialArea("C", CommunicationFormat.Input, InputArea("OLD"), scheduledProgram: true);
        Assert.Equal("Q1".PadRight(12) + "SUB".PadRight(12) + new string(' ', 24), scheduled[..48]);
        string notScheduled = Session().InitialArea("C", CommunicationFormat.Input, InputArea("OLD"), scheduledProgram: true);
        Assert.Equal(new string(' ', 48), notScheduled[..48]);
        Assert.Equal(new string(' ', 12), Session().InitialArea("I", CommunicationFormat.InputOutput, InOutArea("OLD"), scheduledProgram: true).Substring(14, 12));
    }

    /// <summary>§1985:XIV.2.2.4 GR28 and GR6: only "a program scheduled by the MCS" - the one that establishes the run unit - receives the scheduling names; a CALLed program's INITIAL CD is
    /// "initialized to spaces", and the terminal is not locked to it (kb/Work PB5282).</summary>
    [Fact]
    public void InitialArea_OfAProgramTheMcsDidNotSchedule_IsSpaces_AndTakesNoTerminalLock()
    {
        var session = Session(Definitions, "T1");
        Assert.Equal(new string(' ', 12), session.InitialArea("S", CommunicationFormat.InputOutput, InOutArea("OLD"), scheduledProgram: false).Substring(14, 12));
        Assert.Equal(new string(' ', 48), session.InitialArea("C", CommunicationFormat.Input, InputArea("OLD"), scheduledProgram: false)[..48]);
    }

    /// <summary>A.1.2.3 GR30: when the MCS scheduled the run unit, the terminal's name "shall not be changed by the program" from the first statement on.</summary>
    [Fact]
    public void ScheduledTerminal_ChangedBeforeTheFirstStatement_IsUnknown()
    {
        var session = Session(Definitions, "T1");
        Assert.Equal("T1".PadRight(12), session.InitialArea("I", CommunicationFormat.InputOutput, InOutArea(""), scheduledProgram: true).Substring(14, 12));
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
