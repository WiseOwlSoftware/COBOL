// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime.Mcs;

namespace WiseOwl.Cobol.Runtime.Messaging;

/// <summary>
/// ⛔ THE IN-PROCESS SOCKET DIRECTORY (docs/rearchitecture/DESIGN-message-control.md §7.2 "Protocol", §9 S2a): the shared medium of several
/// <see cref="InMemoryTransport"/>s, each standing for one run unit. It holds the published endpoints as the socket directory holds their files,
/// so a transport that is <see cref="InMemoryTransport.Abort"/>ed (a killed process) leaves its files behind and a later connect to one is refused
/// and deletes it, as §5.4.1's refusal rule has it on real sockets.
/// </summary>
internal sealed class InMemoryNetwork
{
    private readonly SortedDictionary<(PeerEndpointKind Kind, string Spelling), SortedDictionary<string, InMemoryTransport>> _entries = [];

    /// <summary>Called by a publishing transport after its endpoint is bound and listening and before it becomes visible under its final name: the
    /// window §5.4.1 closes. A test that enumerates, connects or rings here proves that no one can see a listener that is not yet listening.</summary>
    internal Action<PeerEndpoint>? BetweenListenAndRename { get; set; }

    /// <summary>Whether <paramref name="endpoint"/> is visible (its file exists), live or left behind by a killed run unit.</summary>
    internal bool Exists(PeerEndpoint endpoint) =>
        _entries.TryGetValue((endpoint.Kind, endpoint.Spelling), out var owners) && owners.ContainsKey(endpoint.RunId);

    /// <summary>The run ids visible under one directory, in ordinal order.</summary>
    internal IReadOnlyList<string> RunIds(PeerEndpointKind kind, string spelling) =>
        _entries.TryGetValue((kind, spelling), out var owners) ? [.. owners.Keys] : [];

    internal void Add(PeerEndpoint endpoint, InMemoryTransport owner)
    {
        if (!_entries.TryGetValue((endpoint.Kind, endpoint.Spelling), out var owners))
            _entries[(endpoint.Kind, endpoint.Spelling)] = owners = new SortedDictionary<string, InMemoryTransport>(StringComparer.Ordinal);
        if (!owners.TryAdd(endpoint.RunId, owner)) throw new IOException($"{endpoint} is already published");
    }

    internal void Remove(PeerEndpoint endpoint)
    {
        if (!_entries.TryGetValue((endpoint.Kind, endpoint.Spelling), out var owners)) return;
        owners.Remove(endpoint.RunId);
        if (owners.Count == 0) _entries.Remove((endpoint.Kind, endpoint.Spelling));
    }

    internal InMemoryTransport? OwnerOf(PeerEndpoint endpoint) =>
        _entries.TryGetValue((endpoint.Kind, endpoint.Spelling), out var owners) && owners.TryGetValue(endpoint.RunId, out var owner) ? owner : null;
}

/// <summary>
/// ⛔ <see cref="IPeerTransport"/> OVER AN <see cref="InMemoryNetwork"/> (docs/rearchitecture/DESIGN-message-control.md §7.2, §9 S2a): no socket, no
/// thread, no clock. Frames cross as real <see cref="McsFrame"/> bytes, so the codecs run; events queue per receiving transport in the order they
/// happened, and the test's scheduler decides when each is taken. <see cref="Abort"/> ends every link as a kill does (the other ends read the end
/// of the stream, the published files stay); <see cref="Dispose"/> ends them as <c>Terminate</c> does (links closed, endpoints unpublished).
/// </summary>
internal sealed class InMemoryTransport : IPeerTransport
{
    private sealed class Half(InMemoryTransport owner, PeerLinkId id)
    {
        internal InMemoryTransport Owner { get; } = owner;
        internal PeerLinkId Id { get; } = id;
        internal Half? Other { get; set; }
        internal bool Open { get; set; } = true;
    }

    private readonly InMemoryNetwork _network;
    private readonly Queue<TransportEvent> _events = new();
    private readonly Dictionary<PeerLinkId, Half> _links = [];
    private readonly HashSet<PeerEndpoint> _published = [];
    private int _nextLink = 1;

    internal InMemoryTransport(InMemoryNetwork network, string runId)
    {
        _network = network;
        RunId = runId;
    }

    /// <summary>The run id <c>&lt;r&gt;</c> of the run unit this transport stands for.</summary>
    internal string RunId { get; }

    /// <summary>True once <see cref="Abort"/> ran: the run unit is dead and its files, if any remain, are stale.</summary>
    internal bool Aborted { get; private set; }

    /// <summary>The frames the peer wrote to links that were already ended, which the test can assert were dropped.</summary>
    internal int DroppedWrites { get; private set; }

    public void Publish(PeerEndpoint endpoint)
    {
        ThrowIfDead();
        if (endpoint.RunId != RunId) throw new ArgumentException("a transport publishes only its own run id", nameof(endpoint));
        if (_network.Exists(endpoint)) throw new IOException($"{endpoint} is already published");
        _network.BetweenListenAndRename?.Invoke(endpoint);
        _network.Add(endpoint, this);
        _published.Add(endpoint);
    }

    public void Unpublish(PeerEndpoint endpoint)
    {
        if (_published.Remove(endpoint)) _network.Remove(endpoint);
    }

    public IReadOnlyList<string> EnumerateServers(string spelling)
    {
        ThrowIfDead();
        return _network.RunIds(PeerEndpointKind.Server, spelling);
    }

    public ConnectResult Connect(PeerEndpoint endpoint)
    {
        ThrowIfDead();
        var owner = _network.OwnerOf(endpoint);
        if (owner is null) return new ConnectResult(ConnectOutcome.Refused, default);
        if (owner.Aborted)
        {
            _network.Remove(endpoint);
            return new ConnectResult(ConnectOutcome.Refused, default);
        }
        var mine = new Half(this, new PeerLinkId(_nextLink++));
        var theirs = new Half(owner, new PeerLinkId(owner._nextLink++)) { Other = mine };
        mine.Other = theirs;
        _links[mine.Id] = mine;
        owner._links[theirs.Id] = theirs;
        owner._events.Enqueue(new LinkAccepted(theirs.Id));
        return new ConnectResult(ConnectOutcome.Connected, mine.Id);
    }

    public bool Write(PeerLinkId link, McsFrameKind kind, ReadOnlySpan<byte> payload)
    {
        ThrowIfDead();
        if (!_links.TryGetValue(link, out var half) || !half.Open || half.Other is not { Open: true } other)
        {
            DroppedWrites++;
            return false;
        }
        var frame = McsFrame.Read(new MemoryStream(McsFrame.Encode(kind, payload)))!.Value;
        other.Owner._events.Enqueue(new FrameReceived(other.Id, frame.Kind, frame.Payload));
        return true;
    }

    public void Close(PeerLinkId link)
    {
        if (_links.TryGetValue(link, out var half)) End(half);
    }

    public void RingBells(string spelling)
    {
        ThrowIfDead();
        foreach (string runId in _network.RunIds(PeerEndpointKind.Bell, spelling))
        {
            var bell = new PeerEndpoint(PeerEndpointKind.Bell, spelling, runId);
            var owner = _network.OwnerOf(bell);
            if (owner is null) continue;
            if (owner.Aborted) _network.Remove(bell);
            else owner._events.Enqueue(new BellRung(spelling));
        }
    }

    public bool TryTake(out TransportEvent transportEvent) => _events.TryDequeue(out transportEvent!);

    /// <summary>The kill: every open link ends for its other side, nothing is unpublished, and the transport takes no further call.</summary>
    internal void Abort()
    {
        if (Aborted) return;
        Aborted = true;
        foreach (var half in _links.Values.ToArray()) End(half);
        _events.Clear();
    }

    /// <summary>Normal end: links closed, endpoints unpublished.</summary>
    public void Dispose()
    {
        if (Aborted) return;
        foreach (var half in _links.Values.ToArray()) End(half);
        foreach (var endpoint in _published.ToArray()) Unpublish(endpoint);
    }

    private static void End(Half half)
    {
        if (!half.Open) return;
        half.Open = false;
        if (half.Other is { Open: true } other)
        {
            other.Open = false;
            other.Owner._events.Enqueue(new LinkLost(other.Id));
        }
    }

    private void ThrowIfDead()
    {
        if (Aborted) throw new ObjectDisposedException(nameof(InMemoryTransport), "the run unit was killed");
    }
}
