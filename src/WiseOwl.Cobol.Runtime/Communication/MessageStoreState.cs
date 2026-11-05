// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers;
using System.Globalization;
using System.Text.Json.Nodes;

namespace WiseOwl.Cobol.Runtime.Communication;

/// <summary>The readers of <c>mcs.json</c> and <c>mcs-state.json</c>: a member of the wrong shape, or absent where it is required, is a <see cref="FormatException"/>, which
/// <see cref="MessageStore"/> answers with status 91 for the user's definitions and 92 for its own state.</summary>
internal static class MessageJson
{
    internal static JsonNode Node(JsonNode? node) => node ?? throw new FormatException("a null where the MCS's files need a value");

    internal static JsonObject Object(JsonNode? node) => Node(node).AsObject();

    internal static JsonArray Array(JsonObject o, string member) => Node(o[member]).AsArray();

    internal static string Text(JsonNode? node) => Node(node).GetValue<string>();

    internal static bool Flag(JsonObject o) => Node(o["enabled"]).GetValue<bool>();

    internal static IEnumerable<StoredMessage> Messages(JsonObject o, DateTimeOffset now) => Array(o, "messages").Select(m => StoredMessage.Parse(Node(m), now));
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
        var segments = MessageJson.Array(o, "segments").Select(MessageJson.Text).ToArray();
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
    /// 12 characters, A.1.2.3 GR2), a name that would address a directory (a queue path and a device file are built from names), a name defined
    /// twice, or a <c>deliverTo</c> that names nothing.</summary>
    internal static MessageStoreState FromDefinitions(JsonObject definitions, DateTimeOffset now)
    {
        var state = new MessageStoreState();
        foreach (var node in definitions["queues"]?.AsArray() ?? [])
            AddQueue(MessageJson.Object(node), state._queues, [], now);
        foreach (var node in definitions["sources"]?.AsArray() ?? [])
        {
            var o = MessageJson.Object(node);
            string name = NameOf(o);
            if (!state._sources.TryAdd(name, new SourceEndpoint(name, KeyOf(o), EnabledOf(o)))) throw new FormatException($"source '{name}' is defined twice");
        }
        foreach (var node in definitions["terminals"]?.AsArray() ?? [])
        {
            var o = MessageJson.Object(node);
            string name = NameOf(o);
            var terminal = new TerminalEndpoint(name, KeyOf(o), EnabledOf(o));
            if (!state._terminals.TryAdd(name, terminal)) throw new FormatException($"terminal '{name}' is defined twice");
            foreach (var m in o["messages"]?.AsArray() ?? []) terminal.Inbox.Add(StoredMessage.Parse(MessageJson.Node(m), now));
        }
        foreach (var node in definitions["destinations"]?.AsArray() ?? [])
        {
            var o = MessageJson.Object(node);
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
        foreach (var m in o["messages"]?.AsArray() ?? []) queue.Messages.Add(StoredMessage.Parse(MessageJson.Node(m), now));
        foreach (var child in o["subQueues"]?.AsArray() ?? []) AddQueue(MessageJson.Object(child), queue.Children, path, now);
    }

    private static string NameOf(JsonObject o)
    {
        string name = MessageJson.Text(o["name"]);
        if (name.Length is 0 or > CommunicationArea.NameLength) throw new FormatException($"'{name}' is no symbolic name of 1 to 12 characters");
        if (name.AsSpan().ContainsAny(NotInAFileName)) throw new FormatException($"the symbolic name '{name}' holds a character no file name may hold");
        if (ReservedDeviceNames.Contains(DeviceStem(name))) throw new FormatException($"the symbolic name '{name}' is a device name of the host");
        return name;
    }

    /// <summary>The characters no host lets a file name hold: the union of <see cref="Path.GetInvalidFileNameChars"/> and the Windows set (the control characters
    /// and <c>" * / : &lt; &gt; ? \ |</c>, Microsoft "Naming Files, Paths, and Namespaces"), so a definition file that is usable on one OS is usable on the other. The colon
    /// is the one the host call omits on Linux and the one that names an NTFS alternate stream on Windows.</summary>
    private static readonly SearchValues<char> NotInAFileName = SearchValues.Create(
        [.. Path.GetInvalidFileNameChars(), .. Enumerable.Range(0, 32).Select(c => (char)c), .. "\"*/:<>?\\|"]);

    /// <summary>The names Windows reads as a device whatever the extension (<c>NUL.out</c> is the NUL device): the three-letter ones, the console handles, and COM and LPT
    /// followed by a digit or a superscript digit. Compared without case.</summary>
    private static readonly HashSet<string> ReservedDeviceNames = new(
        ["CON", "PRN", "AUX", "NUL", "CONIN$", "CONOUT$",
         .. from port in new[] { "COM", "LPT" } from digit in "0123456789¹²³" select port + digit],
        StringComparer.OrdinalIgnoreCase);

    /// <summary>The part of a name that Windows tests against the device names: up to the first dot, without trailing spaces. The device file <c>&lt;name&gt;.out</c> keeps it.</summary>
    private static string DeviceStem(string name)
    {
        int dot = name.IndexOf('.');
        return (dot < 0 ? name : name[..dot]).TrimEnd(' ');
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
    /// queue it feeds where that queue and the source's path are enabled ("provided that input queue is enabled", E.5.5.1), and otherwise to its device.
    /// Returns what the devices are to take: nothing here reaches outside the state, so the store commits the two together.</summary>
    internal IReadOnlyList<(string Device, StoredMessage Message)> Deliver()
    {
        foreach (var destination in _destinations.Values)
        {
            if (!destination.Enabled || destination.Pending.Count == 0) continue;
            if (destination.DeliverTo is not { } target)
            {
                _transmissions.AddRange(destination.Pending.Select(message => (destination.Name, message)));
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
        return _transmissions;
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
        foreach (var entry in MessageJson.Array(saved, "queues"))
        {
            var o = MessageJson.Object(entry);
            if (FindQueue(MessageJson.Text(o["path"]).Split('/')) is not { } node) continue;
            node.Enabled = MessageJson.Flag(o);
            node.Messages.Clear();
            node.Messages.AddRange(MessageJson.Messages(o, now));
        }
        foreach (var entry in MessageJson.Array(saved, "sources"))
        {
            var o = MessageJson.Object(entry);
            if (FindSource(MessageJson.Text(o["name"])) is { } source) source.Enabled = MessageJson.Flag(o);
        }
        foreach (var entry in MessageJson.Array(saved, "destinations"))
        {
            var o = MessageJson.Object(entry);
            if (FindDestination(MessageJson.Text(o["name"])) is not { } destination) continue;
            destination.Enabled = MessageJson.Flag(o);
            destination.Pending.AddRange(MessageJson.Messages(o, now));
        }
        foreach (var entry in MessageJson.Array(saved, "terminals"))
        {
            var o = MessageJson.Object(entry);
            if (FindTerminal(MessageJson.Text(o["name"])) is not { } terminal) continue;
            terminal.Enabled = MessageJson.Flag(o);
            terminal.Inbox.Clear();
            terminal.Inbox.AddRange(MessageJson.Messages(o, now));
        }
    }
}
