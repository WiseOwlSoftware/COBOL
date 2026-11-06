// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime.Mcs;

namespace WiseOwl.Cobol.Runtime.Messaging;

/// <summary>How a RECEIVE waits (ISO §14.9.31.4 GR2; §0 Q5).</summary>
internal enum MessageWaitKind
{
    /// <summary>No CONTINUE phrase: until a message or an exception arrives.</summary>
    Forever,

    /// <summary>CONTINUE AFTER MESSAGE RECEIVED: ask once.</summary>
    MessageReceived,

    /// <summary>CONTINUE AFTER n SECONDS, n already evaluated by <c>CobolTiming</c>'s rule; zero and a negative value ask once.</summary>
    Seconds,
}

/// <summary>How long a RECEIVE waits (the protocol-side twin of the bound <c>MessageWait</c>; slice S4 widens it to the public surface of design 5.7).</summary>
internal readonly record struct MessageWait(MessageWaitKind Kind, long Seconds)
{
    internal static readonly MessageWait Forever = new(MessageWaitKind.Forever, 0);
    internal static readonly MessageWait Poll = new(MessageWaitKind.MessageReceived, 0);

    /// <summary>True when the RECEIVE asks once and never waits.</summary>
    internal bool AsksOnce => Kind == MessageWaitKind.MessageReceived || (Kind == MessageWaitKind.Seconds && Seconds <= 0);
}

/// <summary>What <c>mcs.json</c> defines for one server name (§0 Q3): its <c>maxLength</c>, and whether it has a <c>start</c> command.</summary>
internal sealed record ServerDefinition(int? MaxLength, bool HasStart);

/// <summary>
/// How one SEND or RECEIVE ended (docs/rearchitecture/DESIGN-message-control.md design 5.7's <c>MessageOutcome</c> in the protocol's terms).
/// <paramref name="ExceptionName"/> is null exactly when the statement is successful; an unsuccessful RECEIVE of a reply that raised, or of a message
/// longer than identifier-1, still carries its <paramref name="Image"/> (§0 Q6, Q7). <paramref name="Length"/> is what data-name-2 receives, or null
/// when it stays unchanged. A RECEIVE that timed out or polled with nothing waiting is successful with the empty image, zero length and a null
/// <paramref name="Tag"/> (§14.9.31.4 GR2: identifier-1 becomes spaces); <paramref name="Detail"/> explains an EC-MCS-IMP.
/// </summary>
internal sealed record StatementResult(
    string? ExceptionName, MessageTag Tag, string? Image, RecordExtents? Extents, object?[]? Slots, long? Length, string? Detail = null)
{
    /// <summary>True when no exception condition was set.</summary>
    internal bool Successful => ExceptionName is null;

    internal static StatementResult Failed(string exceptionName, string? detail = null) => new(exceptionName, default, null, null, null, null, detail);

    internal static StatementResult Sent(MessageTag serverTag) => new(null, serverTag, null, null, null, null);

    internal static StatementResult SentResponse() => new(null, default, null, null, null, null);

    internal static StatementResult TimedOut() => new(null, default, "", null, null, 0);
}

/// <summary>What <see cref="MessagingProtocol"/> asks its peer to do, in order (docs/rearchitecture/DESIGN-message-control.md design 5.4). The protocol calls nothing itself.</summary>
internal abstract record ProtocolAction;

/// <summary>Write <paramref name="Frame"/> on <paramref name="Link"/> (<see cref="PeerFrameCodec.Encode"/>, then <see cref="IPeerTransport.Write"/>).</summary>
internal sealed record Write(PeerLinkId Link, PeerFrame Frame) : ProtocolAction;

/// <summary>Close <paramref name="Link"/>: the protocol has dropped it.</summary>
internal sealed record CloseLink(PeerLinkId Link) : ProtocolAction;

/// <summary><see cref="IPeerTransport.Publish"/> an endpoint: a server's listener or a requestor's pending bell.</summary>
internal sealed record Publish(PeerEndpoint Endpoint) : ProtocolAction;

/// <summary><see cref="IPeerTransport.Unpublish"/> an endpoint.</summary>
internal sealed record Unpublish(PeerEndpoint Endpoint) : ProtocolAction;

/// <summary><see cref="IPeerTransport.EnumerateServers"/>, answered by <see cref="Enumerated"/>.</summary>
internal sealed record Enumerate(string Spelling) : ProtocolAction;

/// <summary><see cref="IPeerTransport.Connect"/>, answered by <see cref="Connected"/>.</summary>
internal sealed record Connect(PeerEndpoint Endpoint) : ProtocolAction;

/// <summary><see cref="IPeerTransport.RingBells"/>: a server that has published its listener wakes the requestors waiting for its name.</summary>
internal sealed record RingBells(string Spelling) : ProtocolAction;

/// <summary>Arm a one-shot deadline of <paramref name="Seconds"/>; when it passes the peer posts <see cref="DeadlinePassed"/> with the same <paramref name="Token"/>,
/// after the frames it already holds (a reply that arrived is delivered, design 5.4.4). The statement's completion cancels it.</summary>
internal sealed record ArmDeadline(int Token, long Seconds) : ProtocolAction;

/// <summary>Start the server named <paramref name="Name"/> (design 5.4.5), answered by <see cref="ActivationEnded"/>.</summary>
internal sealed record Activate(string Name) : ProtocolAction;

/// <summary>The current statement is over.</summary>
internal sealed record Complete(StatementResult Result) : ProtocolAction;

/// <summary>What the peer tells <see cref="MessagingProtocol"/>: a transport event decoded, or the answer to an action (docs/rearchitecture/DESIGN-message-control.md design 5.4).</summary>
internal abstract record ProtocolEvent;

/// <summary>A decoded frame arrived on <paramref name="Link"/>.</summary>
internal sealed record FrameArrived(PeerLinkId Link, PeerFrame Frame) : ProtocolEvent;

/// <summary>The link ended (<see cref="LinkLost"/>, or a frame that did not decode).</summary>
internal sealed record LinkEnded(PeerLinkId Link) : ProtocolEvent;

/// <summary>A connection arrived at this run unit's listener.</summary>
internal sealed record LinkOpened(PeerLinkId Link) : ProtocolEvent;

/// <summary>A ring arrived at this run unit's pending bell.</summary>
internal sealed record BellRang : ProtocolEvent;

/// <summary>The deadline armed with <paramref name="Token"/> passed.</summary>
internal sealed record DeadlinePassed(int Token) : ProtocolEvent;

/// <summary>The answer to <see cref="Enumerate"/>: the run ids of the servers' listeners.</summary>
internal sealed record Enumerated(IReadOnlyList<string> RunIds) : ProtocolEvent;

/// <summary>The answer to <see cref="Connect"/>.</summary>
internal sealed record Connected(PeerEndpoint Endpoint, ConnectResult Result) : ProtocolEvent;

/// <summary>The answer to <see cref="Activate"/>: the server was started and has published its listener, or it was not.</summary>
internal sealed record ActivationEnded(bool Succeeded, string? Detail) : ProtocolEvent;

/// <summary><paramref name="Action"/> failed for a reason that is not a partner's end (a socket error, a directory the host refuses), and EC-MCS-IMP
/// for the statement it served (§0 Q8). A <see cref="Enumerate"/>, <see cref="Connect"/> or <see cref="Activate"/> answers with this in place of its own
/// answer; the protocol needs the action to know which answer is owed (a failed <see cref="Connect"/> frees its server for the next SEND), and a
/// <see cref="Publish"/> or <see cref="RingBells"/> that fails has no answer to replace.</summary>
internal sealed record ActionFailed(ProtocolAction Action, string Detail) : ProtocolEvent;
