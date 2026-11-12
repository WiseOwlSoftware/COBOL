// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime.Communication;

/// <summary>What a queue, source, destination or terminal has in common: the logical path ENABLE and DISABLE act on, and the COBOL-85 password.</summary>
internal interface IMessageEndpoint
{
    bool Enabled { get; set; }

    /// <summary>Whether the KEY phrase's value opens this endpoint: no KEY phrase asks nothing, and an endpoint defined without a key opens to any.</summary>
    bool Admits(string? key);
}

/// <summary>An endpoint as one statement sees it: the user's definition and a view over its <c>Endpoints</c> row, which holds whether it is enabled
/// (docs/rearchitecture/DESIGN-communication-facility.md §6 "The store"). A row the store lacks reads as the definition's own enabled state, so an endpoint a changed
/// <c>mcs.json</c> added after this run unit seeded answers as defined, and writing the state writes the whole row.</summary>
internal abstract class MessageEndpoint(EndpointKind kind, string[] path, string? key, bool defined, Func<bool, EndpointRow> row, McsTables tables) : IMessageEndpoint
{
    internal string Name => path[^1];

    /// <summary>The tables of the statement this view belongs to.</summary>
    protected McsTables Tables => tables;

    public bool Enabled
    {
        get => tables.Endpoint(kind, path)?.Enabled ?? defined;
        set => tables.PutEndpoint(kind, path, row(value));
    }

    /// <summary>§1985:XIV.3.2.4 GR7 and §1985:XIV.3.3.4 GR6: the KEY's value "match[es] the system password". The standard leaves the rules for a match to the implementor
    /// (its own rationale for the KEY phrase: they "are not specified"), so the two are compared as alphanumeric operands (§2023:8.8.4.2.7: the shorter extended with spaces, so trailing spaces
    /// count on neither side. A password of spaces matches any key of spaces. A KEY phrase omitted gives nothing to match and is honored (docs/CONFORMANCE.md §3.3).</summary>
    public bool Admits(string? written) =>
        written is null || key is null || string.Equals(written.TrimEnd(' '), key.TrimEnd(' '), StringComparison.Ordinal);
}

/// <summary>One level of the queue hierarchy (E.5.5.4). A level that is not named at RECEIVE is chosen "in the alphabetical order", the
/// algorithm E.5.5.4 illustrates, and a level's messages are first in, first out (E.5.5.3): both are the order of the store's keys.</summary>
internal sealed class QueueNode(QueueDefinition definition, McsTables tables)
    : MessageEndpoint(EndpointKind.Queue, definition.Path, definition.Key, definition.Enabled,
        enabled => new EndpointRow(enabled, definition.Key, null, null, null), tables)
{
    internal string[] Path => definition.Path;

    /// <summary>E.5.5: "Interrogation of the queue depth ... reflects only the number of complete messages", and this store holds no other.</summary>
    internal int CompleteMessages => (int)Math.Min(int.MaxValue, Tables.CompleteMessages(definition.Path));
}

internal sealed class SourceEndpoint(SourceDefinition definition, McsTables tables)
    : MessageEndpoint(EndpointKind.Source, [definition.Name], definition.Key, definition.Enabled,
        enabled => new EndpointRow(enabled, definition.Key, null, null, null), tables);

/// <summary>A symbolic destination and its output queue (E.5.5: "Input queues are logically separate from output queues"). A destination
/// with <c>deliverTo</c> feeds a queue or terminal of this MCS; any other's device is its <c>.out</c> file.</summary>
internal sealed class DestinationEndpoint(DestinationDefinition definition, McsTables tables)
    : MessageEndpoint(EndpointKind.Destination, [definition.Name], definition.Key, definition.Enabled,
        enabled => new EndpointRow(enabled, definition.Key, definition.Capacity, definition.DeliverTo, definition.Source), tables)
{
    /// <summary>Table A.2 error key 6, "Output queue capacity exceeded": the capacity bounds the messages waiting in the output queue.</summary>
    internal bool TryEnqueue(StoredMessage message)
    {
        if (definition.Capacity is { } capacity && Tables.OutboundCount(definition.Name) >= (ulong)capacity) return false;
        Tables.AddOutbound(definition.Name, message);
        return true;
    }
}

/// <summary>A terminal of a transaction dialogue (E.5.6): what it has sent, for the RECEIVE of an input-output CD to take.</summary>
internal sealed class TerminalEndpoint(TerminalDefinition definition, McsTables tables)
    : MessageEndpoint(EndpointKind.Terminal, [definition.Name], definition.Key, definition.Enabled,
        enabled => new EndpointRow(enabled, definition.Key, null, null, null), tables)
{
    internal void Transmit(StoredMessage message, MessageStoreState state) => state.TransmitLater(Name, message);
}

/// <summary>
/// The MCS as one statement sees it: the user's definitions and the store's tables (<see cref="McsTables"/>), whose rows the finders read and the takes and deliveries
/// change, in the statement's one page statement. It holds no copy of what the store holds, so nothing of a statement outlives it
/// (docs/rearchitecture/DESIGN-communication-facility.md §6 "The transaction").
/// </summary>
internal sealed class MessageStoreState
{
    private readonly MessageDefinitions _definitions;
    private readonly McsTables? _tables;
    private readonly ulong _runUnit;
    private readonly Func<ulong, bool> _ownerIsAlive;
    private readonly List<(string Device, StoredMessage Message)> _transmissions = [];

    /// <param name="runUnit">The run unit this statement is for: the owner of the partly received messages it records.</param>
    /// <param name="ownerIsAlive">Whether another run unit's session is alive (<see cref="McsStore"/>'s liveness byte); a partly received message whose owner is not is dropped.</param>
    internal MessageStoreState(MessageDefinitions definitions, McsTables? tables, ulong runUnit, Func<ulong, bool> ownerIsAlive)
    {
        _definitions = definitions;
        _tables = tables;
        _runUnit = runUnit;
        _ownerIsAlive = ownerIsAlive;
    }

    /// <summary>The state of an MCS nobody defined: every symbolic name is unknown, so no finder reaches a table.</summary>
    internal static MessageStoreState Undefined() => new(MessageDefinitions.None, tables: null, runUnit: 0, ownerIsAlive: _ => false);

    /// <summary>A.1.2.3 GR5 (<see cref="MessageDefinitions.FindQueue"/>): the queue the path designates. Null when the structure is unknown to the MCS.</summary>
    internal QueueNode? FindQueue(string[] path) => _definitions.FindQueue(path) is { } queue ? new QueueNode(queue, _tables!) : null;

    /// <summary>The deepest known level of <paramref name="path"/> (<see cref="MessageDefinitions.FindQueuePrefix"/>), when the path as a whole is unknown to the MCS.</summary>
    internal QueueNode? FindQueuePrefix(string[] path) => _definitions.FindQueuePrefix(path) is { } queue ? new QueueNode(queue, _tables!) : null;

    internal SourceEndpoint? FindSource(string name) => _definitions.Source(name) is { } source ? new SourceEndpoint(source, _tables!) : null;

    internal DestinationEndpoint? FindDestination(string name) => _definitions.Destination(name) is { } destination ? new DestinationEndpoint(destination, _tables!) : null;

    /// <summary>§1985:XIV.2.2.4 GR10: whether <paramref name="name"/> is "the symbolic name of the communication terminal" the MCS knows: a source or a terminal of its definitions.
    /// A destination's own name, which the MCS gives a message the destination delivers when no source is defined for it, is neither.</summary>
    internal bool KnowsCommunicationTerminal(string name) => _definitions.Source(name) is not null || _definitions.Terminal(name) is not null;

    /// <summary>Whether the logical path between <paramref name="sourceName"/> and the queue <paramref name="queue"/> is open: what a statement last set for that path, and
    /// else the queue's and the source's own enabled states. A name that is no defined source (a destination's own, §1985:XIV.2.2.4 GR10) has no path to close, so the queue alone decides.</summary>
    private bool PathOpen(string sourceName, QueueDefinition queue) =>
        _definitions.Source(sourceName) is not { } source
            ? new QueueNode(queue, _tables!).Enabled
            : _tables!.Path(source.Name, queue.Path) ?? (new QueueNode(queue, _tables).Enabled && new SourceEndpoint(source, _tables).Enabled);

    private bool SetPaths(QueueDefinition queue, IEnumerable<SourceDefinition> sources, bool enable)
    {
        bool changed = false;
        foreach (var source in sources)
        {
            changed |= PathOpen(source.Name, queue) != enable;
            _tables!.PutPath(source.Name, queue.Path, enable);
        }
        return changed;
    }

    /// <summary>Whether every queue, every queue below it and every path between one of them and a source already has the state <see cref="SetQueuePaths"/> would give it;
    /// changes nothing. The 15 condition of a structure (§1985:B.2.2).</summary>
    internal bool QueuePathsAre(QueueNode queue, bool enabled) =>
        _definitions.FindQueue(queue.Path)!.SelfAndDescendants().All(level =>
            new QueueNode(level, _tables!).Enabled == enabled && _definitions.Sources.All(source => PathOpen(source.Name, level) == enabled));

    /// <summary>§1985:XIV.3.2.4 GR3 and §1985:XIV.3.3.4 GR2: DISABLE or ENABLE INPUT acts on "the logical paths between the queue and sub-queues specified ... and all the
    /// associated ... sources". A queue designated by fewer levels than the CD has is a structure, so the queue, every queue below it and every path
    /// between one of them and a source change. Returns whether anything did not already have the state asked for.</summary>
    internal bool SetQueuePaths(QueueNode queue, bool enable)
    {
        bool changed = false;
        foreach (var level in _definitions.FindQueue(queue.Path)!.SelfAndDescendants())
        {
            var node = new QueueNode(level, _tables!);
            if (node.Enabled != enable)
            {
                node.Enabled = enable;
                changed = true;
            }
            changed |= SetPaths(level, _definitions.Sources, enable);
        }
        return changed;
    }

    /// <summary>§1985:XIV.3.2.4 GR4 and §1985:XIV.3.3.4 GR3: DISABLE or ENABLE INPUT TERMINAL acts on "the logical paths between the source ... and all of its associated
    /// queues and sub-queues", every queue of the definitions. Returns whether anything did not already have the state asked for.</summary>
    internal bool SetSourcePaths(SourceEndpoint source, bool enable)
    {
        bool changed = source.Enabled != enable;
        source.Enabled = enable;
        var definition = _definitions.Source(source.Name)!;
        foreach (var queue in _definitions.AllQueues) changed |= SetPaths(queue, [definition], enable);
        return changed;
    }

    internal TerminalEndpoint? FindTerminal(string name) => _definitions.Terminal(name) is { } terminal ? new TerminalEndpoint(terminal, _tables!) : null;

    /// <summary>The next message of the designated structure, removed from its queue, with the level it was found at (E.5.5.3, E.5.5.4: the first key at or after the structure's prefix).</summary>
    internal (string[] Path, StoredMessage Message)? TakeFromQueue(string[] path) =>
        _definitions.FindQueue(path) is { } queue ? _tables!.TakeQueueMessage(queue.Path) : null;

    /// <summary>Whether <paramref name="level"/>, the queue level a message was found at, lies in the queue structure <paramref name="specified"/> designates: "If less than all the
    /// levels of the queue hierarchy are specified, the MCS determines the next message" (A.1.2.3 GR5), so the specified levels (up to the first blank one) are a prefix.</summary>
    internal static bool LevelIsUnder(string[] level, string[] specified)
    {
        for (int at = 0; at < specified.Length && specified[at].Length > 0; at++)
            if (at >= level.Length || !McsNames.Comparer.Equals(specified[at], level[at]))
                return false;
        return true;
    }

    /// <summary>A RECEIVE of the run unit has taken the message numbered <paramref name="serial"/> from the queue at <paramref name="level"/> and returned only its first portion. The
    /// message leaves <c>QueueMessages</c> but "exists in the queue structure" until its last portion is returned (§1985:XIV.3.1.4 GR1), to every run unit's ACCEPT MESSAGE COUNT.</summary>
    internal void BeginPartialReceive(ulong serial, string[] level) => _tables!.PutPartialReceive(_runUnit, serial, level);

    /// <summary>The last portion of the message numbered <paramref name="serial"/> was returned: it no longer exists in the queue structure.</summary>
    internal void EndPartialReceive(ulong serial) => _tables!.DeletePartialReceive(_runUnit, serial);

    /// <summary>The messages partly received, by any live run unit, from the queue structure <paramref name="specified"/> designates (§1985:XIV.3.1.4 GR1). A run unit that is gone took
    /// its remainders with it (§1985:VI.6.24.4 GR3: STOP RUN removes "any message partially received by that run unit" from the queue; ISO/IEC 1989:2002 14.5.10 rule 2 for the
    /// abnormal end alike), so its rows are dropped here, where they are found, and never counted.</summary>
    internal int PartlyReceivedFrom(string[] specified)
    {
        int count = 0;
        foreach (var (owner, serial, level) in _tables!.PartialReceives())
        {
            if (owner != _runUnit && !_ownerIsAlive(owner)) _tables.DeletePartialReceive(owner, serial);
            else if (LevelIsUnder(level, specified)) count++;
        }
        return count;
    }

    /// <summary>The next message the terminal holds, removed. Whether the terminal is enabled is not asked here: DISABLE I-O TERMINAL deactivates the terminal's link to the MCS
    /// (§1985:XIV.3.2.4 GR5), while "the logical path for the transfer of data between the COBOL programs and the MCS is not affected by the DISABLE statement" (§1985:XIV.3.2.4 GR1),
    /// so a message the MCS already holds is still the program's to receive. Where a disabled terminal acts is where its input enters the MCS (<see cref="Deliver"/>).</summary>
    internal (string[] Path, StoredMessage Message)? TakeFromTerminal(string name) =>
        FindTerminal(name) is { } terminal && _tables!.TakeTerminalMessage(terminal.Name) is { } message ? ([terminal.Name], message) : null;

    internal void TransmitLater(string device, StoredMessage message) => _transmissions.Add((device, message));

    /// <summary>Whether the statement gave a device a message of its own (an input-output CD's SEND), which changes no row until the device's committed end moves.</summary>
    internal bool HasTransmissions => _transmissions.Count > 0;

    /// <summary>The MCS's own work after each statement that changed anything: each enabled destination's output queue moves on, into the input
    /// queue it feeds where the path between that queue and the source is enabled ("provided that input queue is enabled", E.5.5.1), and otherwise to its device.
    /// Returns what the devices are to take: nothing here reaches outside the store's rows, so the store lands the rows and the devices' committed ends together.</summary>
    internal IReadOnlyList<(string Device, StoredMessage Message)> Deliver()
    {
        foreach (var definition in _definitions.Destinations)
        {
            if (_tables!.OutboundCount(definition.Name) == 0 || FindDestination(definition.Name) is not { Enabled: true }) continue;
            if (definition.DeliverTo is not { } target)
            {
                _transmissions.AddRange(_tables.TakeOutbound(definition.Name).Select(message => (definition.Name, message)));
            }
            else if (target.StartsWith(MessageDefinitions.TerminalTarget, StringComparison.Ordinal))
            {
                // §1985:XIV.3.3.4 GR4: until ENABLE I-O TERMINAL activates the path between the terminal and the program, a message bound for the terminal stays in the output queue.
                string terminal = target[MessageDefinitions.TerminalTarget.Length..];
                if (FindTerminal(terminal) is { Enabled: false }) continue;
                foreach (var message in _tables.TakeOutbound(definition.Name))
                    _tables.AddTerminalMessage(_definitions.Terminal(target[MessageDefinitions.TerminalTarget.Length..])!.Name, message.From(definition.SourceName));
            }
            else if (_definitions.FindQueue(target.Split('/')) is { } queue && PathOpen(definition.SourceName, queue))
            {
                foreach (var message in _tables.TakeOutbound(definition.Name))
                    _tables.AddQueueMessage(queue.Path, message.From(definition.SourceName));
            }
        }
        return _transmissions;
    }
}
