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

    public bool Admits(string? written) =>
        written is null || key is null || string.Equals(written.TrimEnd(' '), key, StringComparison.Ordinal);
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
    private readonly List<(string Device, StoredMessage Message)> _transmissions = [];

    internal MessageStoreState(MessageDefinitions definitions, McsTables? tables)
    {
        _definitions = definitions;
        _tables = tables;
    }

    /// <summary>The state of an MCS nobody defined: every symbolic name is unknown, so no finder reaches a table.</summary>
    internal static MessageStoreState Undefined() => new(MessageDefinitions.None, tables: null);

    /// <summary>A.1.2.3 GR5 (<see cref="MessageDefinitions.FindQueue"/>): the queue the path designates. Null when the structure is unknown to the MCS.</summary>
    internal QueueNode? FindQueue(string[] path) => _definitions.FindQueue(path) is { } queue ? new QueueNode(queue, _tables!) : null;

    internal SourceEndpoint? FindSource(string name) => _definitions.Source(name) is { } source ? new SourceEndpoint(source, _tables!) : null;

    internal DestinationEndpoint? FindDestination(string name) => _definitions.Destination(name) is { } destination ? new DestinationEndpoint(destination, _tables!) : null;

    internal TerminalEndpoint? FindTerminal(string name) => _definitions.Terminal(name) is { } terminal ? new TerminalEndpoint(terminal, _tables!) : null;

    /// <summary>The next message of the designated structure, removed from its queue, with the level it was found at (E.5.5.3, E.5.5.4: the first key at or after the structure's prefix).</summary>
    internal (string[] Path, StoredMessage Message)? TakeFromQueue(string[] path) =>
        _definitions.FindQueue(path) is { } queue ? _tables!.TakeQueueMessage(queue.Path) : null;

    internal (string[] Path, StoredMessage Message)? TakeFromTerminal(string name) =>
        _definitions.Terminal(name) is not null && _tables!.TakeTerminalMessage(name) is { } message ? ([name], message) : null;

    internal void TransmitLater(string device, StoredMessage message) => _transmissions.Add((device, message));

    /// <summary>Whether the statement gave a device a message of its own (an input-output CD's SEND), which changes no row until the device's committed end moves.</summary>
    internal bool HasTransmissions => _transmissions.Count > 0;

    /// <summary>The MCS's own work after each statement that changed anything: each enabled destination's output queue moves on, into the input
    /// queue it feeds where that queue and the source's path are enabled ("provided that input queue is enabled", E.5.5.1), and otherwise to its device.
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
                foreach (var message in _tables.TakeOutbound(definition.Name))
                    _tables.AddTerminalMessage(target[MessageDefinitions.TerminalTarget.Length..], message.From(definition.SourceName));
            }
            else if (FindQueue(target.Split('/')) is { Enabled: true } queue && FindSource(definition.SourceName) is not { Enabled: false })
            {
                foreach (var message in _tables.TakeOutbound(definition.Name))
                    _tables.AddQueueMessage(queue.Path, message.From(definition.SourceName));
            }
        }
        return _transmissions;
    }
}
