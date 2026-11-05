// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace WiseOwl.Cobol.Runtime.Communication;

/// <summary>
/// ⛔ THE MESSAGE CONTROL SYSTEM (ISO/IEC 1989:2002 E.5.1), WiseOwl COBOL's own: "The structures, formats, and symbolic names of the
/// queues are defined by the user to the message control system at some time prior to the execution of the run unit." The user defines
/// them in the file <c>mcs.json</c> of the MCS directory (the environment variable <c>WISEOWL_COBOL_MCS</c>, else the current directory);
/// what the MCS holds lives beside it in <c>mcs-state.json</c>, so run units on one machine communicate through it and a message outlives
/// the run unit that sent it (E.5.5.1). docs/rearchitecture/DESIGN-communication-facility.md gives the file's format, and
/// docs/CONFORMANCE.md the determinations: with no definition file every symbolic name is unknown (status 20 or 21); an unreadable
/// definition file is status 91 on every statement and a store the host refuses is status 92 (Table A.1's implementor-defined 9x).
/// <para>Every statement takes the store's lock, reads the state, acts and writes the state back atomically, so two run units never
/// see a half-applied statement and a crash leaves the state of the last completed statement. An obsolete facility's traffic does not
/// justify anything cleverer.</para>
/// </summary>
internal sealed class MessageStore
{
    /// <summary>The environment variable that names the MCS directory.</summary>
    internal const string DirectoryVariable = "WISEOWL_COBOL_MCS";

    /// <summary>The environment variable that says what the MCS scheduled the run unit for (ISO/IEC 1989:2002 A.1.2.3 GR6, GR28).</summary>
    internal const string ScheduledVariable = "WISEOWL_COBOL_MCS_SCHEDULED";

    private const string DefinitionFile = "mcs.json", StateFile = "mcs-state.json", LockFile = "mcs.lock";
    internal const string UnreadableDefinitions = "91", StoreRefused = "92";
    private static readonly TimeSpan LockRetry = TimeSpan.FromMilliseconds(20);

    private readonly string? _root;
    private readonly JsonObject? _definitions;
    private readonly bool _broken;

    /// <summary>A.1.2.3 GR6, GR28: what the MCS scheduled the run unit for, the levels of a queue structure for an input CD or a terminal for an
    /// input-output CD; empty when anything else started the run unit.</summary>
    internal string[] Scheduled { get; }

    private MessageStore(string? root, JsonObject? definitions, bool broken, string[] scheduled)
    {
        _root = root;
        _definitions = definitions;
        _broken = broken;
        Scheduled = scheduled;
    }

    /// <summary>The store this run unit's environment names: the directory <c>WISEOWL_COBOL_MCS</c>, else the current one, and the levels of
    /// <c>WISEOWL_COBOL_MCS_SCHEDULED</c> separated by '/'.</summary>
    internal static MessageStore Open() => OpenAt(
        Environment.GetEnvironmentVariable(DirectoryVariable) is { Length: > 0 } named ? named : Directory.GetCurrentDirectory(),
        Environment.GetEnvironmentVariable(ScheduledVariable) is { Length: > 0 } scheduled ? scheduled.Split('/') : []);

    /// <summary>The store of one MCS directory. The definition file is read once: it is defined "prior to the execution".</summary>
    internal static MessageStore OpenAt(string root, string[] scheduled)
    {
        string path = Path.Combine(root, DefinitionFile);
        if (!File.Exists(path)) return new MessageStore(null, null, broken: false, scheduled);
        try
        {
            var definitions = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
            MessageStoreState.FromDefinitions(definitions, DateTimeOffset.UnixEpoch);   // a definition that cannot be used is refused here, once
            return new MessageStore(root, definitions, broken: false, scheduled);
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or FormatException or IOException or UnauthorizedAccessException or NullReferenceException)
        {
            return new MessageStore(root, null, broken: true, scheduled);
        }
    }

    /// <summary>One statement's transaction. Returns the action's status key, or the 9x this store reports instead of acting.</summary>
    internal string Run(Func<MessageStoreState, string> action)
    {
        if (_broken) return UnreadableDefinitions;
        if (_root is null) return action(MessageStoreState.Empty());
        try
        {
            using var held = AcquireLock();
            var state = Load();
            string status = action(state);
            if (state.Dirty)
            {
                state.Deliver(Transmit);
                Save(state);
            }
            return status;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or FormatException)
        {
            return StoreRefused;
        }
    }

    /// <summary>The lock another run unit holds for the length of one statement; a sharing violation is that, and is waited out.</summary>
    private FileStream AcquireLock()
    {
        string path = Path.Combine(_root!, LockFile);
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException) when (attempt < 500)
            {
                Thread.Sleep(LockRetry);
            }
        }
    }

    private MessageStoreState Load()
    {
        var now = RunUnit.Current.Clock.Now();
        var state = MessageStoreState.FromDefinitions(_definitions!, now);
        string path = Path.Combine(_root!, StateFile);
        if (File.Exists(path)) state.Overlay(JsonNode.Parse(File.ReadAllText(path))!.AsObject());
        else state.Touch();   // the definitions' messages enter the queues once, when the store first answers
        return state;
    }

    private void Save(MessageStoreState state)
    {
        string path = Path.Combine(_root!, StateFile);
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, state.ToJson().ToJsonString());
        File.Move(temporary, path, overwrite: true);
    }

    /// <summary>A message a device takes: the communication device of this MCS is the file <c>&lt;name&gt;.out</c> of the MCS directory, oriented to
    /// variable-length messages (A.2.6.3 GR2), so vertical positioning "is not applicable" and the MCS ignores it (GR12).</summary>
    private void Transmit(string device, StoredMessage message) =>
        File.AppendAllText(Path.Combine(_root!, device + ".out"), string.Concat(message.Segments) + Environment.NewLine);
}

/// <summary>What a queue, source, destination or terminal has in common: the logical path ENABLE and DISABLE act on, and the COBOL-85 password.</summary>
internal interface IMessageEndpoint
{
    bool Enabled { get; set; }

    /// <summary>Whether the KEY phrase's value opens this endpoint: no KEY phrase asks nothing, and an endpoint defined without a key opens to any.</summary>
    bool Admits(string? key);
}

internal abstract class MessageEndpoint(string name, string? key, bool enabled) : IMessageEndpoint
{
    internal string Name => name;
    public bool Enabled { get; set; } = enabled;

    public bool Admits(string? written) =>
        written is null || key is null || string.Equals(written.TrimEnd(' '), key, StringComparison.Ordinal);
}

/// <summary>One complete message (E.5.5: "The message control system logically places in queues or removes from queues only complete
/// messages"): its segments, whether an EGI ended it, and when the MCS "recognized that the message was complete" (A.1.2.3 GR8, GR9).</summary>
internal sealed record StoredMessage(IReadOnlyList<string> Segments, bool EndOfGroup, string Date, string Time, string Source)
{
    internal static StoredMessage Complete(IReadOnlyList<string> segments, bool endOfGroup, DateTimeOffset now, string source = "") =>
        new(segments, endOfGroup,
            now.ToString("yyMMdd", CultureInfo.InvariantCulture),
            now.ToString("HHmmss", CultureInfo.InvariantCulture) + (now.Millisecond / 10).ToString("D2", CultureInfo.InvariantCulture),
            source);

    internal StoredMessage From(string source) => this with { Source = source };

    /// <summary>A definition's or the state file's message: a bare string is one segment.</summary>
    internal static StoredMessage Parse(JsonNode node, DateTimeOffset now)
    {
        if (node is JsonValue) return Complete([node.GetValue<string>()], endOfGroup: false, now);
        var o = node.AsObject();
        var segments = o["segments"]!.AsArray().Select(s => s!.GetValue<string>()).ToArray();
        if (segments.Length == 0) throw new FormatException("a message has at least one segment");
        var made = Complete(segments, o["group"]?.GetValue<bool>() ?? false, now, o["source"]?.GetValue<string>() ?? "");
        return made with { Date = o["date"]?.GetValue<string>() ?? made.Date, Time = o["time"]?.GetValue<string>() ?? made.Time };
    }

    internal JsonObject ToJson() => new()
    {
        ["segments"] = new JsonArray(Segments.Select(s => (JsonNode?)JsonValue.Create(s)).ToArray()),
        ["group"] = EndOfGroup,
        ["date"] = Date,
        ["time"] = Time,
        ["source"] = Source,
    };
}

/// <summary>One level of the queue hierarchy (E.5.5.4). A level that is not named at RECEIVE is chosen "in the alphabetical order", the
/// algorithm E.5.5.4 illustrates, and a level's messages are first in, first out (E.5.5.3).</summary>
internal sealed class QueueNode(string name, string? key, bool enabled, string[] path) : MessageEndpoint(name, key, enabled)
{
    internal string[] Path => path;
    internal readonly SortedDictionary<string, QueueNode> Children = new(StringComparer.Ordinal);
    internal readonly List<StoredMessage> Messages = [];

    /// <summary>E.5.5: "Interrogation of the queue depth ... reflects only the number of complete messages", and this store holds no other.</summary>
    internal int CompleteMessages => Messages.Count + Children.Values.Sum(c => c.CompleteMessages);

    internal IEnumerable<QueueNode> SelfAndDescendants()
    {
        yield return this;
        foreach (var child in Children.Values)
            foreach (var node in child.SelfAndDescendants())
                yield return node;
    }
}

internal sealed class SourceEndpoint(string name, string? key, bool enabled) : MessageEndpoint(name, key, enabled);

/// <summary>A symbolic destination and its output queue (E.5.5: "Input queues are logically separate from output queues"). A destination
/// with <c>deliverTo</c> feeds a queue or terminal of this MCS; any other's device is its <c>.out</c> file.</summary>
internal sealed class DestinationEndpoint(string name, string? key, bool enabled, int? capacity, string? deliverTo, string? source)
    : MessageEndpoint(name, key, enabled)
{
    internal string? DeliverTo => deliverTo;
    internal string SourceName => source ?? Name;
    internal readonly List<StoredMessage> Pending = [];

    /// <summary>Table A.2 error key 6, "Output queue capacity exceeded": the capacity bounds the messages waiting in the output queue.</summary>
    internal bool TryEnqueue(StoredMessage message, MessageStoreState state)
    {
        if (Pending.Count >= capacity) return false;
        Pending.Add(message);
        state.Touch();
        return true;
    }
}

/// <summary>A terminal of a transaction dialogue (E.5.6): what it has sent, for the RECEIVE of an input-output CD to take.</summary>
internal sealed class TerminalEndpoint(string name, string? key, bool enabled) : MessageEndpoint(name, key, enabled)
{
    internal readonly List<StoredMessage> Inbox = [];

    internal void Transmit(StoredMessage message, MessageStoreState state) => state.TransmitLater(Name, message);
}

/// <summary>The MCS's definitions and what it currently holds, for the length of one statement.</summary>
internal sealed class MessageStoreState
{
    private const string TerminalPrefix = "terminal:";
    private readonly SortedDictionary<string, QueueNode> _queues = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SourceEndpoint> _sources = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DestinationEndpoint> _destinations = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TerminalEndpoint> _terminals = new(StringComparer.Ordinal);
    private readonly List<(string Device, StoredMessage Message)> _transmissions = [];

    internal bool Dirty { get; private set; }

    internal void Touch() => Dirty = true;

    internal static MessageStoreState Empty() => new();

    /// <summary>Build the state the definitions describe, refusing one that cannot be used: a name no CD could hold (a symbolic name is at most
    /// 12 characters, A.1.2.3 GR2), a name defined twice, or a <c>deliverTo</c> that names nothing.</summary>
    internal static MessageStoreState FromDefinitions(JsonObject definitions, DateTimeOffset now)
    {
        var state = new MessageStoreState();
        foreach (var node in definitions["queues"]?.AsArray() ?? [])
            AddQueue(node!.AsObject(), state._queues, [], now);
        foreach (var node in definitions["sources"]?.AsArray() ?? [])
        {
            var o = node!.AsObject();
            string name = NameOf(o);
            if (!state._sources.TryAdd(name, new SourceEndpoint(name, KeyOf(o), EnabledOf(o)))) throw new FormatException($"source '{name}' is defined twice");
        }
        foreach (var node in definitions["terminals"]?.AsArray() ?? [])
        {
            var o = node!.AsObject();
            string name = NameOf(o);
            var terminal = new TerminalEndpoint(name, KeyOf(o), EnabledOf(o));
            if (!state._terminals.TryAdd(name, terminal)) throw new FormatException($"terminal '{name}' is defined twice");
            foreach (var m in o["messages"]?.AsArray() ?? []) terminal.Inbox.Add(StoredMessage.Parse(m!, now));
        }
        foreach (var node in definitions["destinations"]?.AsArray() ?? [])
        {
            var o = node!.AsObject();
            string name = NameOf(o);
            var destination = new DestinationEndpoint(name, KeyOf(o), EnabledOf(o), o["capacity"]?.GetValue<int>(),
                o["deliverTo"]?.GetValue<string>(), o["source"]?.GetValue<string>());
            if (!state._destinations.TryAdd(name, destination)) throw new FormatException($"destination '{name}' is defined twice");
        }
        foreach (var destination in state._destinations.Values)
            if (destination.DeliverTo is { } target && !state.DeliveryTargetExists(target))
                throw new FormatException($"destination '{destination.Name}' delivers to '{target}', which is not defined");
        return state;
    }

    private static void AddQueue(JsonObject o, SortedDictionary<string, QueueNode> siblings, string[] parentPath, DateTimeOffset now)
    {
        string name = NameOf(o);
        string[] path = [.. parentPath, name];
        if (path.Length > 4) throw new FormatException($"queue '{string.Join('/', path)}' is deeper than the four levels of E.5.5.4");
        var queue = new QueueNode(name, KeyOf(o), EnabledOf(o), path);
        if (!siblings.TryAdd(name, queue)) throw new FormatException($"queue '{string.Join('/', path)}' is defined twice");
        foreach (var m in o["messages"]?.AsArray() ?? []) queue.Messages.Add(StoredMessage.Parse(m!, now));
        foreach (var child in o["subQueues"]?.AsArray() ?? []) AddQueue(child!.AsObject(), queue.Children, path, now);
    }

    private static string NameOf(JsonObject o)
    {
        string name = o["name"]!.GetValue<string>();
        if (name.Length is 0 or > CommunicationArea.NameLength) throw new FormatException($"'{name}' is no symbolic name of 1 to 12 characters");
        return name;
    }

    private static string? KeyOf(JsonObject o) => o["key"]?.GetValue<string>();

    private static bool EnabledOf(JsonObject o) => o["enabled"]?.GetValue<bool>() ?? true;

    private bool DeliveryTargetExists(string target) =>
        target.StartsWith(TerminalPrefix, StringComparison.Ordinal)
            ? _terminals.ContainsKey(target[TerminalPrefix.Length..])
            : FindQueue(target.Split('/')) is not null;

    /// <summary>A.1.2.3 GR5: "When a given level of the queue structure is specified all higher levels shall also be specified", so a level
    /// named below a blank one designates nothing, and neither does a blank queue. Null when the structure is unknown to the MCS.</summary>
    internal QueueNode? FindQueue(string[] path)
    {
        if (path.Length == 0 || path[0].Length == 0) return null;
        QueueNode? node = null;
        var level = _queues;
        for (int i = 0; i < path.Length; i++)
        {
            if (path[i].Length == 0)
                return path.Skip(i).All(p => p.Length == 0) ? node : null;
            if (!level.TryGetValue(path[i], out node)) return null;
            level = node.Children;
        }
        return node;
    }

    internal SourceEndpoint? FindSource(string name) => _sources.GetValueOrDefault(name);

    internal DestinationEndpoint? FindDestination(string name) => _destinations.GetValueOrDefault(name);

    internal TerminalEndpoint? FindTerminal(string name) => _terminals.GetValueOrDefault(name);

    /// <summary>The next message of the designated structure, removed from its queue, with the level it was found at.</summary>
    internal (string[] Path, StoredMessage Message)? TakeFromQueue(string[] path)
    {
        foreach (var node in FindQueue(path)?.SelfAndDescendants() ?? [])
            if (node.Messages.Count > 0)
            {
                var message = node.Messages[0];
                node.Messages.RemoveAt(0);
                Touch();
                return (node.Path, message);
            }
        return null;
    }

    internal (string[] Path, StoredMessage Message)? TakeFromTerminal(string name)
    {
        if (FindTerminal(name) is not { Inbox.Count: > 0 } terminal) return null;
        var message = terminal.Inbox[0];
        terminal.Inbox.RemoveAt(0);
        Touch();
        return ([name], message);
    }

    internal void TransmitLater(string device, StoredMessage message)
    {
        _transmissions.Add((device, message));
        Touch();
    }

    /// <summary>The MCS's own work after each statement that changed anything: each enabled destination's output queue moves on, into the input
    /// queue it feeds where that queue and the source's path are enabled ("provided that input queue is enabled", E.5.5.1), and otherwise to its device.</summary>
    internal void Deliver(Action<string, StoredMessage> transmit)
    {
        foreach (var (device, message) in _transmissions) transmit(device, message);
        _transmissions.Clear();
        foreach (var destination in _destinations.Values)
        {
            if (!destination.Enabled || destination.Pending.Count == 0) continue;
            if (destination.DeliverTo is not { } target)
            {
                foreach (var message in destination.Pending) transmit(destination.Name, message);
                destination.Pending.Clear();
            }
            else if (target.StartsWith(TerminalPrefix, StringComparison.Ordinal))
            {
                _terminals[target[TerminalPrefix.Length..]].Inbox.AddRange(destination.Pending.Select(m => m.From(destination.SourceName)));
                destination.Pending.Clear();
            }
            else if (FindQueue(target.Split('/')) is { Enabled: true } queue
                     && FindSource(destination.SourceName) is not { Enabled: false })
            {
                queue.Messages.AddRange(destination.Pending.Select(m => m.From(destination.SourceName)));
                destination.Pending.Clear();
            }
        }
    }

    internal JsonObject ToJson()
    {
        var queues = new JsonArray();
        foreach (var root in _queues.Values)
            foreach (var node in root.SelfAndDescendants())
                queues.Add((JsonNode)new JsonObject { ["path"] = string.Join('/', node.Path), ["enabled"] = node.Enabled, ["messages"] = Messages(node.Messages) });
        var sources = new JsonArray();
        foreach (var source in _sources.Values) sources.Add((JsonNode)new JsonObject { ["name"] = source.Name, ["enabled"] = source.Enabled });
        var destinations = new JsonArray();
        foreach (var destination in _destinations.Values)
            destinations.Add((JsonNode)new JsonObject { ["name"] = destination.Name, ["enabled"] = destination.Enabled, ["messages"] = Messages(destination.Pending) });
        var terminals = new JsonArray();
        foreach (var terminal in _terminals.Values)
            terminals.Add((JsonNode)new JsonObject { ["name"] = terminal.Name, ["enabled"] = terminal.Enabled, ["messages"] = Messages(terminal.Inbox) });
        return new JsonObject { ["queues"] = queues, ["sources"] = sources, ["destinations"] = destinations, ["terminals"] = terminals };

        static JsonArray Messages(IEnumerable<StoredMessage> messages) => new(messages.Select(m => (JsonNode?)m.ToJson()).ToArray());
    }

    /// <summary>Lay the state file over the definitions. The definitions' seed messages are replaced: they entered the queues when the store first
    /// answered, and the state now says what is left of them. An entry the definitions no longer name is dropped with its name.</summary>
    internal void Overlay(JsonObject saved)
    {
        var now = DateTimeOffset.UnixEpoch;
        foreach (var entry in saved["queues"]!.AsArray())
        {
            var o = entry!.AsObject();
            if (FindQueue(o["path"]!.GetValue<string>().Split('/')) is not { } node) continue;
            node.Enabled = o["enabled"]!.GetValue<bool>();
            node.Messages.Clear();
            foreach (var m in o["messages"]!.AsArray()) node.Messages.Add(StoredMessage.Parse(m!, now));
        }
        foreach (var entry in saved["sources"]!.AsArray())
            if (FindSource(entry!["name"]!.GetValue<string>()) is { } source)
                source.Enabled = entry["enabled"]!.GetValue<bool>();
        foreach (var entry in saved["destinations"]!.AsArray())
            if (FindDestination(entry!["name"]!.GetValue<string>()) is { } destination)
            {
                destination.Enabled = entry["enabled"]!.GetValue<bool>();
                foreach (var m in entry["messages"]!.AsArray()) destination.Pending.Add(StoredMessage.Parse(m!, now));
            }
        foreach (var entry in saved["terminals"]!.AsArray())
            if (FindTerminal(entry!["name"]!.GetValue<string>()) is { } terminal)
            {
                terminal.Enabled = entry["enabled"]!.GetValue<bool>();
                terminal.Inbox.Clear();
                foreach (var m in entry["messages"]!.AsArray()) terminal.Inbox.Add(StoredMessage.Parse(m!, now));
            }
    }
}
