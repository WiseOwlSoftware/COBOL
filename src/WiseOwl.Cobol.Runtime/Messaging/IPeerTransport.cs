// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime.Mcs;

namespace WiseOwl.Cobol.Runtime.Messaging;

/// <summary>A link of one <see cref="IPeerTransport"/>: the local handle of one connected stream.</summary>
internal readonly record struct PeerLinkId(int Value)
{
    public override string ToString() => $"link {Value}";
}

/// <summary>What an endpoint of the socket directory is (docs/rearchitecture/DESIGN-message-control.md design 5.1).</summary>
internal enum PeerEndpointKind
{
    /// <summary>A server's listener, <c>s/&lt;N&gt;/&lt;r&gt;</c>.</summary>
    Server,

    /// <summary>A requestor's pending bell, <c>p/&lt;N&gt;/&lt;r&gt;</c>.</summary>
    Bell,
}

/// <summary>One entry of the socket directory: <paramref name="Spelling"/> is the name's directory spelling (<see cref="McsServerName.Spell"/>) and
/// <paramref name="RunId"/> the publishing run unit's <c>&lt;r&gt;</c>.</summary>
internal readonly record struct PeerEndpoint(PeerEndpointKind Kind, string Spelling, string RunId);

/// <summary>How a <see cref="IPeerTransport.Connect"/> ended.</summary>
internal enum ConnectOutcome
{
    /// <summary>Connected: the result carries the link.</summary>
    Connected,

    /// <summary>Refused: the owner of the entry has ended, and the transport has applied the refusal rule (deleted the file, or found it already gone).</summary>
    Refused,
}

/// <summary>The answer of <see cref="IPeerTransport.Connect"/>; a failure that is not a refusal is an <see cref="IOException"/>.</summary>
internal readonly record struct ConnectResult(ConnectOutcome Outcome, PeerLinkId Link);

/// <summary>What a transport tells its peer, in the order it happened. The peer feeds them to <see cref="MessagingProtocol"/>.</summary>
internal abstract record TransportEvent;

/// <summary>A whole frame arrived on <paramref name="Link"/>.</summary>
internal sealed record FrameReceived(PeerLinkId Link, McsFrameKind Kind, byte[] Payload) : TransportEvent;

/// <summary>The link ended: the peer closed it, the process holding the other end was killed, or the stream carried a malformed frame
/// (docs/rearchitecture/DESIGN-message-control.md design 5.5). Nothing follows on it.</summary>
internal sealed record LinkLost(PeerLinkId Link) : TransportEvent;

/// <summary>A connection arrived at this run unit's published listener.</summary>
internal sealed record LinkAccepted(PeerLinkId Link) : TransportEvent;

/// <summary>A <see cref="McsFrameKind.Ring"/> arrived at this run unit's pending bell.</summary>
internal sealed record BellRung(string Spelling) : TransportEvent;

/// <summary>
/// ⛔ THE SEAM BETWEEN THE PROTOCOL AND ITS MEDIUM (docs/rearchitecture/DESIGN-message-control.md design 5.4, §9 S2a): what <see cref="MessagingProtocol"/>'s
/// actions are executed on. <c>SocketTransport</c> (slice S2b) answers it with AF_UNIX sockets and the socket directory, <c>InMemoryTransport</c>
/// with a shared in-process network the tests script; the protocol runs identically over both because it sees only frames, links and
/// endpoints. One run unit owns one transport and drives it from one thread: every member is called from that thread, and the events arrive
/// through <see cref="TryTake"/> in the order they happened per link.
/// </summary>
internal interface IPeerTransport : IDisposable
{
    /// <summary>Makes <paramref name="endpoint"/> reachable: it is bound and listening before it is visible under its final name, so no connect ever
    /// finds a file that is not listening (design 5.4.1 "Publishing a listener"). Publishing an endpoint again replaces nothing and throws <see cref="IOException"/>.</summary>
    void Publish(PeerEndpoint endpoint);

    /// <summary>Closes the listener of <paramref name="endpoint"/> and deletes its file; an endpoint not published, or already gone, is skipped.</summary>
    void Unpublish(PeerEndpoint endpoint);

    /// <summary>The run ids of the servers' listeners under <c>s/&lt;spelling&gt;/</c>, in a stable order.</summary>
    IReadOnlyList<string> EnumerateServers(string spelling);

    /// <summary>Connects to <paramref name="endpoint"/>, applying the refusal rule of design 5.4.1.</summary>
    ConnectResult Connect(PeerEndpoint endpoint);

    /// <summary>Writes one frame; false when the link has ended (its <see cref="LinkLost"/> follows or has been delivered).</summary>
    bool Write(PeerLinkId link, McsFrameKind kind, ReadOnlySpan<byte> payload);

    /// <summary>Closes <paramref name="link"/> gracefully; its other end reads end-of-stream. A link already ended is skipped.</summary>
    void Close(PeerLinkId link);

    /// <summary>Rings every pending bell under <c>p/&lt;spelling&gt;/</c> (connect, write one <see cref="McsFrameKind.Ring"/>, close), applying the refusal rule to each.</summary>
    void RingBells(string spelling);

    /// <summary>The next event in arrival order, or false when none is queued.</summary>
    bool TryTake(out TransportEvent transportEvent);
}
