// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers;
using System.Collections.Frozen;
using System.Text.Json.Nodes;

namespace WiseOwl.Cobol.Runtime.Communication;

/// <summary>The reader of <c>mcs.json</c>: a member of the wrong shape, or absent where it is required, is a <see cref="FormatException"/>, which
/// <see cref="MessageStore"/> answers with status 91 for the user's definitions.</summary>
internal static class MessageJson
{
    internal static JsonNode Node(JsonNode? node) => node ?? throw new FormatException("a null where the MCS's files need a value");

    internal static JsonObject Object(JsonNode? node) => Node(node).AsObject();

    internal static JsonArray Array(JsonObject o, string member) => Node(o[member]).AsArray();

    internal static string Text(JsonNode? node) => Node(node).GetValue<string>();
}

/// <summary>A queue as the user defined it (ISO/IEC 1989:2002 E.5.5.4): one level of the hierarchy, its seed messages and its sub-queues.</summary>
internal sealed class QueueDefinition(string name, string? key, bool enabled, string[] path, IReadOnlyList<StoredMessage> messages)
{
    internal string Name => name;
    internal string? Key => key;
    internal bool Enabled => enabled;
    internal string[] Path => path;
    internal IReadOnlyList<StoredMessage> Messages => messages;
    internal SortedDictionary<string, QueueDefinition> Children { get; } = new(StringComparer.Ordinal);

    internal IEnumerable<QueueDefinition> SelfAndDescendants()
    {
        yield return this;
        foreach (var child in Children.Values)
            foreach (var node in child.SelfAndDescendants())
                yield return node;
    }
}

/// <summary>A source as the user defined it.</summary>
internal sealed record SourceDefinition(string Name, string? Key, bool Enabled);

/// <summary>A destination as the user defined it: its capacity, the queue path or <c>terminal:&lt;name&gt;</c> it feeds, and the source its messages carry.</summary>
internal sealed record DestinationDefinition(string Name, string? Key, bool Enabled, int? Capacity, string? DeliverTo, string? Source)
{
    internal string SourceName => Source ?? Name;
}

/// <summary>A terminal as the user defined it, with the messages it holds when the store first holds it.</summary>
internal sealed record TerminalDefinition(string Name, string? Key, bool Enabled, IReadOnlyList<StoredMessage> Messages);

/// <summary>
/// ⛔ WHAT THE USER DEFINED to the message control system (<c>mcs.json</c>, docs/rearchitecture/DESIGN-communication-facility.md §6): immutable, read once per run
/// unit, and never what the MCS currently holds, which is the store's (<see cref="McsStore"/>). Building it refuses a definition that cannot be used: a name no CD
/// could hold (a symbolic name is at most 12 characters, A.1.2.3 GR2), a name that would address a directory (a queue path and a device file are built from names), a
/// name defined twice, a <c>deliverTo</c> that names nothing, or two device-bearing names (a destination without <c>deliverTo</c>, a terminal) that would share one
/// device file because they are equal ignoring case.
/// </summary>
internal sealed class MessageDefinitions
{
    private const string TerminalPrefix = "terminal:";
    private readonly SortedDictionary<string, QueueDefinition> _queues = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SourceDefinition> _sources = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DestinationDefinition> _destinations = new(StringComparer.Ordinal);
    private readonly List<DestinationDefinition> _destinationOrder = [];
    private readonly Dictionary<string, TerminalDefinition> _terminals = new(StringComparer.Ordinal);

    /// <summary>The definitions of an MCS nobody defined: every symbolic name is unknown.</summary>
    internal static MessageDefinitions None { get; } = new();

    /// <summary>The root queues in the alphabetical order E.5.5.4 illustrates.</summary>
    internal IEnumerable<QueueDefinition> RootQueues => _queues.Values;

    /// <summary>Every queue, each level before its sub-queues.</summary>
    internal IEnumerable<QueueDefinition> AllQueues => _queues.Values.SelectMany(q => q.SelfAndDescendants());

    internal IEnumerable<SourceDefinition> Sources => _sources.Values;

    /// <summary>The destinations in the order the user wrote them, which is the order the MCS delivers in.</summary>
    internal IReadOnlyList<DestinationDefinition> Destinations => _destinationOrder;

    internal IEnumerable<TerminalDefinition> Terminals => _terminals.Values;

    /// <summary>The names of the device-bearing endpoints: the destinations that deliver to no queue and the terminals, each the file <c>&lt;name&gt;.out</c>.</summary>
    internal IEnumerable<string> Devices => _terminals.Keys.Concat(_destinationOrder.Where(d => d.DeliverTo is null).Select(d => d.Name));

    internal SourceDefinition? Source(string name) => _sources.GetValueOrDefault(name);

    internal DestinationDefinition? Destination(string name) => _destinations.GetValueOrDefault(name);

    internal TerminalDefinition? Terminal(string name) => _terminals.GetValueOrDefault(name);

    /// <summary>The definitions <paramref name="definitions"/> describe, its messages dated <paramref name="now"/> unless they carry a date and a time.</summary>
    internal static MessageDefinitions Parse(JsonObject definitions, DateTimeOffset now)
    {
        var result = new MessageDefinitions();
        foreach (var node in definitions["queues"]?.AsArray() ?? [])
            AddQueue(MessageJson.Object(node), result._queues, [], now);
        foreach (var node in definitions["sources"]?.AsArray() ?? [])
        {
            var o = MessageJson.Object(node);
            string name = NameOf(o);
            if (!result._sources.TryAdd(name, new SourceDefinition(name, KeyOf(o), EnabledOf(o)))) throw new FormatException($"source '{name}' is defined twice");
        }
        foreach (var node in definitions["terminals"]?.AsArray() ?? [])
        {
            var o = MessageJson.Object(node);
            string name = NameOf(o);
            var messages = (o["messages"]?.AsArray() ?? []).Select(m => StoredMessage.Parse(MessageJson.Node(m), now)).ToArray();
            if (!result._terminals.TryAdd(name, new TerminalDefinition(name, KeyOf(o), EnabledOf(o), messages))) throw new FormatException($"terminal '{name}' is defined twice");
        }
        foreach (var node in definitions["destinations"]?.AsArray() ?? [])
        {
            var o = MessageJson.Object(node);
            string name = NameOf(o);
            var destination = new DestinationDefinition(name, KeyOf(o), EnabledOf(o), o["capacity"]?.GetValue<int>(),
                o["deliverTo"]?.GetValue<string>(), o["source"]?.GetValue<string>());
            if (!result._destinations.TryAdd(name, destination)) throw new FormatException($"destination '{name}' is defined twice");
            result._destinationOrder.Add(destination);
        }
        RefuseSharedDeviceFiles(result);
        foreach (var destination in result._destinationOrder)
            if (destination.DeliverTo is { } target && !result.DeliveryTargetExists(target))
                throw new FormatException($"destination '{destination.Name}' delivers to '{target}', which is not defined");
        return result;
    }

    /// <summary>A device is the file <c>&lt;name&gt;.out</c> of the MCS directory (<see cref="McsStore"/>), and the file system, not the definition file, says
    /// which names are one file: Windows and macOS fold case, so <c>D1</c> and <c>d1</c> would append to one file and each SEND would answer 00 for a device the other
    /// feeds. The device-bearing endpoints, the destinations that deliver to no queue and the terminals, are therefore distinct without case, on every host, so a
    /// definition file usable on one OS is usable on the other (as <see cref="NameOf"/> does for the characters of a name). The same name as a terminal and as a
    /// destination is the same file and is refused by the same rule.</summary>
    private static void RefuseSharedDeviceFiles(MessageDefinitions definitions)
    {
        var devices = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);   // not a COBOL word: a host file name
        foreach (var name in definitions._terminals.Keys.Concat(definitions._destinationOrder.Where(d => d.DeliverTo is null).Select(d => d.Name)))
            if (!devices.TryAdd(name, name))
                throw new FormatException($"'{devices[name]}' and '{name}' name one device file, which a host that ignores case cannot tell apart");
    }

    private static void AddQueue(JsonObject o, SortedDictionary<string, QueueDefinition> siblings, string[] parentPath, DateTimeOffset now)
    {
        string name = NameOf(o);
        string[] path = [.. parentPath, name];
        if (path.Length > 4) throw new FormatException($"queue '{string.Join('/', path)}' is deeper than the four levels of E.5.5.4");
        var messages = (o["messages"]?.AsArray() ?? []).Select(m => StoredMessage.Parse(MessageJson.Node(m), now)).ToArray();
        var queue = new QueueDefinition(name, KeyOf(o), EnabledOf(o), path, messages);
        if (!siblings.TryAdd(name, queue)) throw new FormatException($"queue '{string.Join('/', path)}' is defined twice");
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

    /// <summary>The characters some host refuses in a file name: the union of <see cref="Path.GetInvalidFileNameChars"/> and the Windows set (the control characters
    /// and <c>" * / : &lt; &gt; ? \ |</c>, Microsoft "Naming Files, Paths, and Namespaces"), so a definition file that is usable on one OS is usable on the other. The host
    /// call on Linux omits all but the slash and NUL; the colon in particular names an NTFS alternate stream on Windows.</summary>
    private static readonly SearchValues<char> NotInAFileName = SearchValues.Create(
        [.. Path.GetInvalidFileNameChars(), .. Enumerable.Range(0, 32).Select(c => (char)c), .. "\"*/:<>?\\|"]);

    /// <summary>The names Windows reads as a device whatever the extension (<c>NUL.out</c> is the NUL device): the three-letter ones, the console handles, and COM and LPT
    /// followed by a digit or a superscript digit. Compared without case.</summary>
    private static readonly FrozenSet<string> ReservedDeviceNames =
        (from port in new[] { "COM", "LPT" } from digit in "0123456789¹²³" select port + digit)
        .Concat(["CON", "PRN", "AUX", "NUL", "CONIN$", "CONOUT$"])
        .ToFrozenSet(StringComparer.OrdinalIgnoreCase);   // not a COBOL word: a host device name

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

    /// <summary>The prefix of a <c>deliverTo</c> that names a terminal.</summary>
    internal static string TerminalTarget => TerminalPrefix;

    /// <summary>A.1.2.3 GR5: "When a given level of the queue structure is specified all higher levels shall also be specified", so a level
    /// named below a blank one designates nothing, and neither does a blank queue. Null when the structure is unknown to the MCS.</summary>
    internal QueueDefinition? FindQueue(string[] path)
    {
        if (path.Length == 0 || path[0].Length == 0) return null;
        QueueDefinition? node = null;
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
}

/// <summary>The <c>mcs.json</c> a run unit read: its parsed JSON (seeding dates the seed messages when it runs), the SHA-256 of its bytes (<c>Meta</c>'s hash) and the
/// definitions built from it.</summary>
internal sealed record DefinitionFile(JsonObject Json, byte[] Hash, MessageDefinitions Parsed);
