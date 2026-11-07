// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime.Communication;
using WiseOwl.Cobol.Runtime.IO.Store;
using WiseOwl.Cobol.Tests.Shared;
using WiseOwl.Cobol.Tests.Unit.Store;
using Xunit;
using static WiseOwl.Cobol.Tests.Unit.MessageControlTests;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE MCS'S STORE UNDER A DEAD RUN UNIT AND A REFUSING HOST (docs/rearchitecture/DESIGN-communication-facility.md §6 "Status 92 takes no action", §7). Each statement kind
/// of the facility (SEND to a queue, to a terminal, to a device, to several destinations at once; the RECEIVE take of a queue and of a terminal; ENABLE and DISABLE of a queue, a
/// source and a destination, one that moves the messages a closed path held; an input-output CD's SEND; the seeding of a changed <c>mcs.json</c>) runs on the engine's one medium seam
/// (<see cref="PageMediumObserver"/>) with the run unit killed after every page write it hands over (<see cref="DyingPageMedium"/>), and again with the host refusing every write from
/// the nth on (<see cref="MediumFault"/>). A fresh run unit then reads the store, and no matter where the statement was cut: the store is the state before the statement or the state
/// after it, whole; no message is lost and none duplicated; every count row equals a recount; every device holds what its committed end says; and a statement that answered 92 left the
/// state before it, while one that answered as completed left the state after it, so a program that retries on 92 delivers once.
/// </summary>
public sealed class McsStoreCrashMatrixTests : IDisposable
{
    private readonly string _base = ScratchDirectory.Create("WiseOwlCobol_McsCrash_");
    private int _next;

    public void Dispose() => ScratchDirectory.Remove(_base);

    private const string Definitions = """
        {
          "queues": [ { "name": "Q", "subQueues": [ { "name": "S" } ] }, { "name": "CLOSED", "enabled": false } ],
          "sources": [ { "name": "SRC" } ],
          "destinations": [
            { "name": "DQ", "deliverTo": "Q/S", "source": "SRC" }, { "name": "DT", "deliverTo": "terminal:TT" }, { "name": "DEV" },
            { "name": "DC", "deliverTo": "CLOSED" }, { "name": "DOFF", "enabled": false }
          ],
          "terminals": [ { "name": "TT" }, { "name": "T2", "messages": [ "HELLO" ] } ]
        }
        """;

    private const string ChangedDefinitions = """
        {
          "queues": [ { "name": "Q", "subQueues": [ { "name": "S" } ] }, { "name": "CLOSED", "enabled": false }, { "name": "NEWQ", "messages": [ "FRESH" ] } ],
          "sources": [ { "name": "SRC" } ],
          "destinations": [
            { "name": "DQ", "deliverTo": "Q/S", "source": "SRC" }, { "name": "DEV" }, { "name": "DC", "deliverTo": "CLOSED" }, { "name": "DOFF", "enabled": false }
          ],
          "terminals": [ { "name": "T2", "messages": [ "HELLO" ] } ]
        }
        """;

    private static string Sent(MessageControlSession s, string destination, string text) =>
        OutputStatus(s.Send("C", CommunicationFormat.Output, OutputArea(text.Length, destination), text, SendIndicator.EndOfMessage));

    private static string Received(MessageControlSession s, string queue) =>
        InputStatus(s.Receive("R", CommunicationFormat.Input, InputArea(queue), false, "......", noDataPhrase: true).Area);

    private static string Path(MessageControlSession s, string queue, bool enable) =>
        InputStatus(s.SetPath("C", CommunicationFormat.Input, CommunicationPath.Input, InputArea(queue), null, enable));

    /// <summary>The statements whose every cut point the matrix covers: each answers its status key, which is 00 when it completes.</summary>
    private static readonly (string Name, Func<MessageControlSession, string> Run, bool Seeding)[] Statements =
    [
        ("send-to-a-queue", s => Sent(s, "DQ", "NEW"), false),
        ("send-to-a-terminal", s => Sent(s, "DT", "TERM"), false),
        ("send-to-a-device", s => Sent(s, "DEV", "DEVMSG"), false),
        ("send-to-two-destinations", s => OutputStatus(s.Send("C", CommunicationFormat.Output,
            "0002" + "0003" + "99" + " " + "DQ".PadRight(12) + " " + "DEV".PadRight(12), "TWO", SendIndicator.EndOfMessage)), false),
        ("send-to-a-closed-path", s => Sent(s, "DC", "LATER"), false),
        ("receive-from-a-queue", s => Received(s, "Q"), false),
        ("receive-from-a-terminal", s => InOutStatus(s.Receive("I", CommunicationFormat.InputOutput, InOutArea("T2"), false, "......", noDataPhrase: true).Area), false),
        ("enable-a-queue-that-moves-the-outbound", s => Path(s, "CLOSED", enable: true), false),
        ("disable-a-queue", s => Path(s, "Q", enable: false), false),
        ("disable-a-source", s => InputStatus(s.SetPath("C", CommunicationFormat.Input, CommunicationPath.InputTerminal,
            InputArea("Q")[..62] + "SRC".PadRight(12) + InputArea("Q")[74..], null, enable: false)), false),
        ("enable-a-destination-that-moves-the-outbound", s => OutputStatus(s.SetPath("C", CommunicationFormat.Output, CommunicationPath.Output,
            OutputArea(0, "DOFF"), null, enable: true)), false),
        ("send-from-an-input-output-cd", s => InOutStatus(s.Send("I", CommunicationFormat.InputOutput, InOutArea("T2", 2), "XY", SendIndicator.EndOfMessage)), false),
        ("seeding-a-changed-definition-file", s => InputStatus(s.AcceptMessageCount(InputArea("Q"))), true),
    ];

    public static TheoryData<string> Names() => [.. Statements.Select(s => s.Name)];

    /// <summary>A fresh MCS directory whose store holds messages in a queue, an outbound queue of a closed path and of a disabled destination, and a device with output; the run unit that
    /// made them has ended.</summary>
    private string Prepared()
    {
        string directory = System.IO.Path.Combine(_base, "d" + _next++);
        Directory.CreateDirectory(directory);
        File.WriteAllText(System.IO.Path.Combine(directory, "mcs.json"), Definitions);
        using var setup = new Session(directory);
        foreach (var (destination, text) in new[] { ("DQ", "ONE"), ("DQ", "TWO"), ("DC", "WAIT"), ("DOFF", "HELD"), ("DEV", "OLD") })
            Sent(setup.Control, destination, text);
        return directory;
    }

    private sealed class Session : IDisposable
    {
        internal Session(string directory) => Control = new MessageControlSession(MessageStore.OpenAt(directory, []));

        internal MessageControlSession Control { get; }

        public void Dispose() => Control.Dispose();
    }

    private static void Define(string directory, bool seeding)
    {
        if (seeding) File.WriteAllText(System.IO.Path.Combine(directory, "mcs.json"), ChangedDefinitions);
    }

    /// <summary>The state the statement leaves when nothing cuts it, and the state before it.</summary>
    private (List<string> Before, List<string> After) Reference(Func<MessageControlSession, string> run, bool seeding)
    {
        string untouched = Prepared(), done = Prepared();
        var before = McsInspector.Content(untouched);
        Define(done, seeding);
        using (var s = new Session(done))
            Assert.Equal("00", run(s.Control));
        var after = McsInspector.Content(done);
        Assert.NotEqual(before, after);
        return (before, after);
    }

    /// <summary>A fresh run unit's first statement: it adopts the store, cuts every device to its committed end, and reads.</summary>
    private static void Probe(string directory)
    {
        using var fresh = new Session(directory);
        Assert.Equal("00", InputStatus(fresh.Control.AcceptMessageCount(InputArea("Q"))));
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void RunUnitKilledAfterEveryWrite_LeavesTheStateBeforeOrAfter_WithTrueCounts(string name)
    {
        var (_, run, seeding) = Statements.Single(s => s.Name == name);
        var (before, after) = Reference(run, seeding);
        bool sawBefore = false, sawAfter = false;
        for (int dieAfter = 0; ; dieAfter++)
        {
            string directory = Prepared();
            Define(directory, seeding);
            DyingPageMedium? dying = null;
            var previous = PageMediumObserver.Value;
            PageMediumObserver.Value = medium => dying = new DyingPageMedium(medium) { DieAfter = dieAfter };
            try
            {
                using var s = new Session(directory);
                run(s.Control);
            }
            finally
            {
                PageMediumObserver.Value = previous;
            }
            Probe(directory);
            McsInspector.AssertCountsEqualARecount(directory);
            var content = McsInspector.Content(directory);
            if (content.SequenceEqual(after)) sawAfter = true;
            else if (!seeding && content.SequenceEqual(before)) sawBefore = true;
            else Assert.Fail($"cut after write {dieAfter} of '{name}' left a state that is neither before nor after it:{Environment.NewLine}{string.Join(Environment.NewLine, content)}");
            if (!dying!.Dead) break;
            Assert.True(dieAfter < 200, "the statement never completed");
        }
        Assert.True(sawAfter, "no cut point let the statement land");
        Assert.True(seeding || sawBefore, "no cut point stopped the statement before it landed");
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void HostRefusingEveryWriteFromTheNth_AnswersNoActionOrCompleted_NeverBoth(string name)
    {
        var (_, run, seeding) = Statements.Single(s => s.Name == name);
        var (before, after) = Reference(run, seeding);
        bool refused = false;
        using var fault = new MediumFault();
        for (int from = 0; ; from++)
        {
            string directory = Prepared();
            Define(directory, seeding);
            string status;
            using (var s = new Session(directory))
            {
                if (!seeding) Assert.Equal("00", InputStatus(s.Control.AcceptMessageCount(InputArea("Q"))));
                fault.RefuseWritesFrom = from;
                status = run(s.Control);
                fault.RefuseWritesFrom = null;
                if (status == "00") Assert.Equal(after, McsInspector.Content(directory));
                else if (status == "92")
                {
                    // The seeding lands as a statement of its own and is then promoted (§6 "Devices"): a refusal of the promotion answers 92 with the seeding landed, which has no action
                    // to undo (the writable reopen of the next statement fences it durable); every other statement leaves the state before it.
                    var content = McsInspector.Content(directory);
                    if (seeding && content.SequenceEqual(after)) { }
                    else Assert.Equal(before, content);
                }
                else Assert.Equal("93", status);
            }
            McsInspector.AssertCountsEqualARecount(directory);
            if (status == "00") break;
            refused = true;
            Assert.True(from < 200, "the statement never completed");
        }
        Assert.True(refused, "the matrix refused no write");
    }

    /// <summary>The statements a brand-new store's first run unit runs: its creation and seeding on the first, a transmission to a device on the second.</summary>
    private static void FirstRunUnit(string directory)
    {
        using var s = new Session(directory);
        InputStatus(s.Control.AcceptMessageCount(InputArea("Q")));
        Sent(s.Control, "DEV", "FIRST");
        Sent(s.Control, "DQ", "QUEUED");
    }

    /// <summary>A fresh directory with a definition file and no store.</summary>
    private string Fresh()
    {
        string directory = System.IO.Path.Combine(_base, "f" + _next++);
        Directory.CreateDirectory(directory);
        File.WriteAllText(System.IO.Path.Combine(directory, "mcs.json"), Definitions);
        return directory;
    }

    /// <summary>What a recovering run unit must find after any cut of a first run unit: the store opens, answers, keeps true counts, and takes a transmission whose device holds
    /// exactly the delivered lines, none torn and none twice.</summary>
    private static void AssertRecovers(string directory)
    {
        Probe(directory);
        McsInspector.AssertCountsEqualARecount(directory);
        using (var s = new Session(directory))
            Assert.Equal("00", Sent(s.Control, "DEV", "AFTER"));
        McsInspector.AssertCountsEqualARecount(directory);
        string device = System.IO.Path.Combine(directory, "DEV.out");
        string text = File.ReadAllText(device);
        string line = Environment.NewLine;
        Assert.True(text == "AFTER" + line || text == "FIRST" + line + "AFTER" + line, $"the device holds '{text}'");
    }

    /// <summary>The creation of the store and its first seeding, killed after every page write (§6 "Seeding", §7).</summary>
    [Fact]
    public void FirstRunUnitKilledAfterEveryWrite_LeavesAStoreTheNextRunUnitUses()
    {
        for (int dieAfter = 0; ; dieAfter++)
        {
            string directory = Fresh();
            DyingPageMedium? dying = null;
            var previous = PageMediumObserver.Value;
            PageMediumObserver.Value = medium => dying = new DyingPageMedium(medium) { DieAfter = dieAfter };
            try
            {
                FirstRunUnit(directory);
            }
            finally
            {
                PageMediumObserver.Value = previous;
            }
            AssertRecovers(directory);
            if (!dying!.Dead) break;
            Assert.True(dieAfter < 200, "the first run unit never completed");
        }
    }

    /// <summary>A host that refuses the flush alone, from the nth sync on (the creation, the fence of the writable open every run unit's first statement runs, a checkpoint, the
    /// close): each statement answers 00, 92 or 93, never anything else, and the next run unit finds a usable store with true counts. Only the NEW store's syncs are refused here: the fence of an existing
    /// store and its later syncs are the next test's.</summary>
    [Fact]
    public void HostRefusingEverySyncFromTheNth_LeavesAStoreTheNextRunUnitUses()
    {
        bool refused = false;
        using var fault = new MediumFault();
        for (int from = 0; ; from++)
        {
            string directory = Fresh();
            var statuses = new List<string>();
            fault.RefuseSyncsFrom = from;
            using (var s = new Session(directory))
            {
                statuses.Add(InputStatus(s.Control.AcceptMessageCount(InputArea("Q"))));
                statuses.Add(Sent(s.Control, "DEV", "FIRST"));
                statuses.Add(Sent(s.Control, "DQ", "QUEUED"));
            }
            fault.RefuseSyncsFrom = null;
            Assert.All(statuses, status => Assert.Contains(status, new[] { "00", "92", "93" }));
            if (new FileInfo(System.IO.Path.Combine(directory, "mcs.store")) is { Exists: true, Length: > 0 }) McsInspector.AssertCountsEqualARecount(directory);
            using (var s = new Session(directory))
                Assert.Equal("00", InputStatus(s.Control.AcceptMessageCount(InputArea("Q"))));
            McsInspector.AssertCountsEqualARecount(directory);
            if (statuses.All(status => status == "00")) break;
            refused = true;
            Assert.True(from < 200, "the statements never completed");
        }
        Assert.True(refused, "the matrix refused no sync");
    }

    /// <summary>A host that refuses the flush alone, from the nth sync on, under a run unit that meets an EXISTING store holding messages (§7): the fence of the writable open its first
    /// statement runs, every promotion and the close are each the refused sync at some n. Each statement answers 00, 92 or 93; the messages the store held are whole after it, none lost
    /// or duplicated, the counts true; and the next run unit uses the store.</summary>
    [Fact]
    public void HostRefusingEverySyncFromTheNth_OverAStoreHoldingMessages_KeepsThemWhole()
    {
        bool refused = false;
        using var fault = new MediumFault();
        for (int from = 0; ; from++)
        {
            string directory = Prepared();
            var before = Messages(directory);
            var statuses = new List<string>();
            fault.RefuseSyncsFrom = from;
            using (var s = new Session(directory))
            {
                statuses.Add(InputStatus(s.Control.AcceptMessageCount(InputArea("Q"))));
                for (int i = 0; i < 40; i++) statuses.Add(Sent(s.Control, i % 2 == 0 ? "DQ" : "DEV", "N" + i));
            }
            fault.RefuseSyncsFrom = null;
            Assert.All(statuses, status => Assert.Contains(status, new[] { "00", "92", "93" }));
            McsInspector.AssertCountsEqualARecount(directory);
            var held = Messages(directory);
            foreach (var (line, count) in before) Assert.True(held.GetValueOrDefault(line) >= count, $"sync refusal from {from} lost or tore '{line}'");
            Probe(directory);
            using (var s = new Session(directory))
                Assert.Equal("00", Sent(s.Control, "DEV", "AFTER"));
            McsInspector.AssertCountsEqualARecount(directory);
            if (statuses.All(status => status == "00")) break;
            refused = true;
            Assert.True(from < 200, "the statements never completed");
        }
        Assert.True(refused, "the matrix refused no sync");
    }

    /// <summary>The prime tree of the COMMITTED meta slot (pages 0 and 1, the state a power loss returns the store to) and of the work slot (page 2), read through a handle of their own.</summary>
    private static (PagePointer Committed, PagePointer Work) PrimeRoots(string directory)
    {
        using var stream = new FileStream(System.IO.Path.Combine(directory, "mcs.store"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var medium = new HandlePageMedium(stream.SafeFileHandle);
        MetaPage? Slot(uint page)
        {
            var bytes = new byte[PageHeader.PageSize];
            medium.Read(page, bytes);
            return MetaPage.Decode(bytes, page);
        }
        var committed = MetaPage.Committed(Slot(MetaPage.SlotA), Slot(MetaPage.SlotB))!.Value.Meta;
        return (committed.PrimeRoot, Slot(MetaPage.WorkSlot)!.PrimeRoot);
    }

    /// <summary>The seeding's DeviceEnds row is durable before any device is written (§6 "Devices", PB4862): the seeding is PROMOTED, so the committed slot, which is all a power loss keeps,
    /// already names the seeded tree when the run unit's first statement, which changed nothing else, has answered.</summary>
    [Fact]
    public void Seeding_IsPromoted_SoAPowerLossKeepsItsDeviceEndsRows()
    {
        string directory = Fresh();
        using var s = new Session(directory);
        Assert.Equal("00", InputStatus(s.Control.AcceptMessageCount(InputArea("Q"))));
        var (committed, work) = PrimeRoots(directory);
        Assert.Equal(work, committed);
        Assert.Contains(McsInspector.Rows(directory), row => row.Key[0] == (byte)'D' && row.Key.Length > 1);
    }

    /// <summary>A device renamed only by case is one file on a case-insensitive host, and its old row must not outlive the rename (§6 "Devices", PB4863): seeding retires the row of a
    /// device the definitions no longer name, so the new name's output is never cut to the old end, and a device removed keeps its file.</summary>
    [Fact]
    public void DeviceRenamedOnlyByCase_LeavesNoStaleRow_AndItsDeliveredOutputIsNotCut()
    {
        string directory = Fresh();
        string definitions = System.IO.Path.Combine(directory, "mcs.json");
        File.WriteAllText(definitions, """{ "destinations": [ { "name": "dev" }, { "name": "GONE" } ] }""");
        using (var s = new Session(directory))
        {
            Assert.Equal("00", Sent(s.Control, "dev", "ONE"));
            Assert.Equal("00", Sent(s.Control, "GONE", "OLD"));
        }
        File.WriteAllText(definitions, """{ "destinations": [ { "name": "DEV" } ] }""");
        using (var s = new Session(directory))
            Assert.Equal("00", Sent(s.Control, "DEV", "TWO"));
        var devices = McsInspector.Rows(directory).Where(row => row.Key[0] == (byte)'D').Select(row => System.Text.Encoding.UTF8.GetString(row.Key, 1, row.Key.Length - 1)).ToList();
        Assert.Equal(["DEV"], devices);
        Assert.True(File.Exists(System.IO.Path.Combine(directory, "GONE.out")));
        // A run unit that follows cuts every device the store claims first; on a case-insensitive host a stale "dev" row at the old end would have cut TWO, one file with DEV.
        using (var s = new Session(directory))
            Assert.Equal("00", Sent(s.Control, "DEV", "THREE"));
        string line = Environment.NewLine;
        string text = File.ReadAllText(System.IO.Path.Combine(directory, "DEV.out"));
        Assert.EndsWith("TWO" + line + "THREE" + line, text);
    }

    /// <summary>A run unit that began before a seeding retired a device's row still holds the old definitions and may send to it (§6 "Devices", "Seeding"): with no row its committed end is
    /// the file's length, so what the other run unit delivered is appended to, never overwritten from offset 0.</summary>
    [Fact]
    public void RunUnitHoldingARetiredDevice_AppendsToItsFile_NeverOverwrites()
    {
        string directory = Fresh();
        string definitions = System.IO.Path.Combine(directory, "mcs.json");
        File.WriteAllText(definitions, """{ "destinations": [ { "name": "X" }, { "name": "KEEP" } ] }""");
        using var a = new Session(directory);
        Assert.Equal("00", Sent(a.Control, "X", "ONE"));
        Assert.Equal("00", Sent(a.Control, "X", "TWO"));
        File.WriteAllText(definitions, """{ "destinations": [ { "name": "KEEP" } ] }""");
        using (var b = new Session(directory))
            Assert.Equal("00", Sent(b.Control, "KEEP", "B"));
        Assert.DoesNotContain(McsInspector.Rows(directory), row => row.Key[0] == (byte)'D' && System.Text.Encoding.UTF8.GetString(row.Key, 1, row.Key.Length - 1) == "X");
        Assert.Equal("00", Sent(a.Control, "X", "THREE"));
        string line = Environment.NewLine;
        Assert.Equal("ONE" + line + "TWO" + line + "THREE" + line, File.ReadAllText(System.IO.Path.Combine(directory, "X.out")));
    }

    /// <summary>A destination renamed only by case, with a run unit still holding the old spelling (§6 "Devices"): on a host whose file names ignore case both spellings are one file, so the
    /// old spelling's send advances the stored row of the new one, creates no row of its own, and the next run unit's cut loop never cuts what it delivered. On a case-sensitive host
    /// the two are two files and the old one has no row.</summary>
    [Fact]
    public void RunUnitHoldingACaseRenamedDevice_NeverHasItsDeliveredOutputCut()
    {
        string directory = Fresh();
        string definitions = System.IO.Path.Combine(directory, "mcs.json");
        File.WriteAllText(definitions, """{ "destinations": [ { "name": "dev" }, { "name": "KEEP" } ] }""");
        using var a = new Session(directory);
        Assert.Equal("00", Sent(a.Control, "dev", "ONE"));
        File.WriteAllText(definitions, """{ "destinations": [ { "name": "DEV" }, { "name": "KEEP" } ] }""");
        using (var b = new Session(directory))
            Assert.Equal("00", Sent(b.Control, "DEV", "TWO"));
        Assert.Equal("00", Sent(a.Control, "dev", "THREE"));
        Assert.DoesNotContain(McsInspector.Rows(directory), row => row.Key[0] == (byte)'D' && System.Text.Encoding.UTF8.GetString(row.Key, 1, row.Key.Length - 1) == "dev");
        string line = Environment.NewLine;
        string before = File.ReadAllText(System.IO.Path.Combine(directory, "dev.out"));
        Assert.EndsWith("THREE" + line, before);
        using (var c = new Session(directory))
            Assert.Equal("00", Sent(c.Control, "KEEP", "K"));
        Assert.Equal(before, File.ReadAllText(System.IO.Path.Combine(directory, "dev.out")));
    }

    /// <summary>A dead statement's tail in a device the definitions drop is cut to the committed end before the row is retired, so it is never left as delivered output (§6 "Devices").</summary>
    [Fact]
    public void RetiredDevice_HasItsDeadTailCut_BeforeItsRowGoes()
    {
        string directory = Fresh();
        string definitions = System.IO.Path.Combine(directory, "mcs.json");
        File.WriteAllText(definitions, """{ "destinations": [ { "name": "X" }, { "name": "KEEP" } ] }""");
        using (var s = new Session(directory))
            Assert.Equal("00", Sent(s.Control, "X", "ONE"));
        string file = System.IO.Path.Combine(directory, "X.out");
        long delivered = new FileInfo(file).Length;
        File.AppendAllText(file, "DEAD");
        File.WriteAllText(definitions, """{ "destinations": [ { "name": "KEEP" } ] }""");
        using (var s = new Session(directory))
            Assert.Equal("00", Sent(s.Control, "KEEP", "B"));
        Assert.Equal(delivered, new FileInfo(file).Length);
    }

    /// <summary>The queue, terminal and outbound messages of a store by their stored text, with how many of each (a serial is no content).</summary>
    private static Dictionary<string, int> Messages(string directory) =>
        McsInspector.Content(directory).Where(l => l[0] is 'M' or 'I' or 'O').GroupBy(l => l).ToDictionary(g => g.Key, g => g.Count());
}
