// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Security.Cryptography;
using WiseOwl.Cobol.Runtime.IO.Store;

namespace WiseOwl.Cobol.Runtime.Communication;

/// <summary>
/// ⛔ SEEDING (docs/rearchitecture/DESIGN-communication-facility.md §6 "Seeding"): the first statement of a run unit compares the SHA-256 of its <c>mcs.json</c> with the
/// <c>Meta</c> row's, and a store that is new, or whose hash differs, is seeded in that statement. Every defined endpoint gets its <c>Endpoints</c> row; an endpoint the
/// store already held keeps its enabled state and its messages, and takes its definition's key, capacity, <c>deliverTo</c> and source; an endpoint new to the store takes
/// its definition's enabled state and its <c>messages</c>; an endpoint the definitions no longer name is deleted with its messages and counts. The queue counts are then
/// recounted from the messages, the one pass in the MCS that reads more than a statement's own rows, made only when the definitions changed.
/// </summary>
internal static class McsSeeding
{
    /// <summary>Seed <paramref name="tables"/> from <paramref name="definitions"/>, the file; seed messages without a date and time are dated <paramref name="now"/>.</summary>
    internal static void Seed(McsTables tables, DefinitionFile definitions, DateTimeOffset now)
    {
        var parsed = MessageDefinitions.Parse(definitions.Json, now);
        if (tables.Meta is null) tables.Meta = new McsMeta(RandomNumberGenerator.GetBytes(16), 1, new byte[32], 0, 0, 0);   // minted at creation; a message draws its serial from it
        var defined = new List<(EndpointKind Kind, string[] Path, EndpointRow Row, IReadOnlyList<StoredMessage> Messages)>();
        foreach (var queue in parsed.AllQueues) defined.Add((EndpointKind.Queue, queue.Path, new EndpointRow(queue.Enabled, queue.Key, null, null, null), queue.Messages));
        foreach (var source in parsed.Sources) defined.Add((EndpointKind.Source, [source.Name], new EndpointRow(source.Enabled, source.Key, null, null, null), []));
        foreach (var destination in parsed.Destinations)
            defined.Add((EndpointKind.Destination, [destination.Name], new EndpointRow(destination.Enabled, destination.Key, destination.Capacity, destination.DeliverTo, destination.Source), []));
        foreach (var terminal in parsed.Terminals) defined.Add((EndpointKind.Terminal, [terminal.Name], new EndpointRow(terminal.Enabled, terminal.Key, null, null, null), terminal.Messages));

        var named = defined.Select(d => Convert.ToHexString(McsTables.KeyOfEndpoint(d.Kind, d.Path))).ToHashSet(StringComparer.Ordinal);
        foreach (byte[] held in tables.EndpointKeys())
            if (!named.Contains(Convert.ToHexString(held))) tables.DeleteEndpoint(held);

        foreach (var (kind, path, row, messages) in defined)
        {
            if (tables.Endpoint(kind, path) is { } kept)
            {
                tables.PutEndpoint(kind, path, row with { Enabled = kept.Enabled });
                continue;
            }
            tables.PutEndpoint(kind, path, row);
            foreach (var message in messages)
            {
                if (kind is EndpointKind.Queue) tables.AddQueueMessage(path, message);
                else tables.AddTerminalMessage(path[0], message);
            }
        }
        tables.RecountQueues();
        tables.Meta = tables.Meta! with { DefinitionsHash = definitions.Hash };
    }
}
