// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Exceptions;
using WiseOwl.Cobol.Runtime.Mcs;
using WiseOwl.Cobol.Runtime.Messaging;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE 2023 MESSAGE CONTROL PROTOCOL OVER THE IN-MEMORY TRANSPORT (kb/Work PB4741; docs/rearchitecture/DESIGN-message-control.md §7.2 "Protocol", §9 S2a).
/// Each <see cref="Peer"/> is one run unit: a <see cref="MessagingProtocol"/> whose actions run on its own <see cref="InMemoryTransport"/>, under a scripted
/// scheduler (<see cref="Peer.Step"/>, <see cref="Pump"/>) that decides when each event is delivered, so every interleaving the design lists is one test.
/// </summary>
public sealed class MessagingProtocolTests
{
    private const string Mcs = "0123456789abcdef";

    private sealed class Peer
    {
        private readonly Queue<ProtocolEvent> _answers = new();

        internal Peer(InMemoryNetwork network, string runId, string? served = null, Func<string, ServerDefinition?>? definitions = null)
        {
            Transport = new InMemoryTransport(network, runId);
            Protocol = new MessagingProtocol(Mcs, runId, 100 + runId.Length, served, definitions ?? (_ => null));
            RunId = runId;
            Spelling = served is null ? null : McsServerName.Spell(McsServerName.Normalize(served));
        }

        internal string RunId { get; }
        internal string? Spelling { get; }
        internal InMemoryTransport Transport { get; }
        internal MessagingProtocol Protocol { get; }
        internal List<StatementResult> Results { get; } = [];
        internal List<ProtocolAction> Log { get; } = [];
        internal List<ArmDeadline> Deadlines { get; } = [];
        internal List<Activate> Activations { get; } = [];
        internal Func<Activate, bool>? OnActivate { get; set; }
        internal Action<ProtocolAction>? AfterAction { get; set; }

        internal PeerEndpoint ListenerEndpoint => new(PeerEndpointKind.Server, Spelling!, RunId);

        internal StatementResult Last => Results[^1];

        internal void Serve() => Run(Protocol.Serve());
        internal void Send(string server, string image) => Run(Protocol.BeginSend(server, MessageBody.Of(image)));
        internal void Send(string server, MessageBody body) => Run(Protocol.BeginSend(server, body));
        internal void SendResponse(MessageTag to, string image, string? raising = null) => Run(Protocol.BeginSendResponse(to, MessageBody.Of(image), raising));
        internal void Receive(MessageTag from, int capacity = 100, MessageWait? wait = null) => Run(Protocol.BeginReceive(from, capacity, wait ?? MessageWait.Forever));
        internal void End(bool abnormal = false) => Run(Protocol.End(abnormal));
        internal void Post(ProtocolEvent protocolEvent) => Run(Protocol.Handle(protocolEvent));

        internal void Run(IReadOnlyList<ProtocolAction> actions)
        {
            foreach (var action in actions)
            {
                Log.Add(action);
                switch (action)
                {
                    case Write w: Transport.Write(w.Link, w.Frame.Kind, PeerFrameCodec.Encode(w.Frame)); break;
                    case CloseLink c: Transport.Close(c.Link); break;
                    case Publish p: Transport.Publish(p.Endpoint); break;
                    case Unpublish u: Transport.Unpublish(u.Endpoint); break;
                    case Enumerate e: _answers.Enqueue(new Enumerated(Transport.EnumerateServers(e.Spelling))); break;
                    case Connect c: _answers.Enqueue(new Connected(c.Endpoint, Transport.Connect(c.Endpoint))); break;
                    case RingBells r: Transport.RingBells(r.Spelling); break;
                    case ArmDeadline d: Deadlines.Add(d); break;
                    case Activate a:
                        Activations.Add(a);
                        if (OnActivate is not null) _answers.Enqueue(new ActivationEnded(OnActivate(a), null));
                        break;
                    case Complete c: Results.Add(c.Result); break;
                }
                AfterAction?.Invoke(action);
            }
        }

        /// <summary>Delivers one thing: an answer to an earlier action first, else one transport event. False when there is none.</summary>
        internal bool Step()
        {
            if (!_answers.TryDequeue(out var answer)) return StepTransport();
            Post(answer);
            return true;
        }

        internal bool StepTransport()
        {
            if (!Transport.TryTake(out var transportEvent)) return false;
            Post(transportEvent switch
            {
                FrameReceived f => new FrameArrived(f.Link, PeerFrameCodec.Decode(f.Kind, f.Payload)),
                LinkLost l => new LinkEnded(l.Link),
                LinkAccepted a => new LinkOpened(a.Link),
                BellRung => new BellRang(),
                _ => throw new InvalidOperationException(),
            });
            return true;
        }

        /// <summary>Drops the oldest answer owed to an action: the answer to an action that failed is <see cref="ActionFailed"/> instead.</summary>
        internal void DropAnswer() => _answers.Dequeue();
    }

    /// <summary>Runs every peer until none has anything left to deliver.</summary>
    private static void Pump(params Peer[] peers)
    {
        for (int guard = 0; guard < 10_000; guard++)
        {
            bool any = false;
            foreach (var peer in peers) any |= peer.Step();
            if (!any) return;
        }
        throw new InvalidOperationException("the protocol did not settle");
    }

    private static InMemoryNetwork Network() => new();

    private static (InMemoryNetwork Network, Peer Server, Peer Requestor) Pair(string name = "SRV")
    {
        var network = Network();
        var server = new Peer(network, "srv1", name);
        var requestor = new Peer(network, "req1");
        server.Serve();
        return (network, server, requestor);
    }

    private static PeerEndpoint BellOf(string spelling, string runId) => new(PeerEndpointKind.Bell, spelling, runId);

    // ---------------------------------------------------------------- the server name (Q3)

    [Fact]
    public void ANameIsNormalizedAndSpelledAsItselfWhenItIsShortAndPlain()
    {
        Assert.Equal("BAL-SRV1", McsServerName.Normalize("bal-srv1   "));
        Assert.Equal("BAL-SRV1", McsServerName.Spell("BAL-SRV1"));
        Assert.Equal("ABCDEFGHIJKLMN", McsServerName.Spell("ABCDEFGHIJKLMN"));            // 14 kept
    }

    [Fact]
    public void AnyOtherNameIsSpelledUnderscoreAndTheBase32OfItsHash()
    {
        Assert.Equal("_AFY2SNDY4ZSLC", McsServerName.Spell("A-VERY-LONG-SERVER-NAME"));   // 23 characters
        Assert.Equal("_IEIBNXWQJINZK", McsServerName.Spell("BAL.SRV"));                  // a character outside [A-Z0-9-]
        Assert.StartsWith("_", McsServerName.Spell(string.Empty));
        Assert.Equal(14, McsServerName.Spell("BAL.SRV").Length);
    }

    // ---------------------------------------------------------------- the codec

    [Fact]
    public void EveryFrameRoundTripsThroughItsPayload()
    {
        var extents = new RecordExtents([0, 4], [2, 3]);
        var tag = new MessageTag(Mcs, MessageParty.Requestor, "run1", 7);
        var body = new MessageBody("héllo 😀", extents, [tag, MessageTag.Null]);
        PeerFrame[] frames =
        [
            new HelloFrame(1, Mcs, "run1", 42, "SRV"), new OfferFrame(9, 3), new ClaimFrame(9, 3), new GrantFrame(9, 3, body),
            new WithdrawFrame(9, 3), new AckFrame(9), new ReplyFrame(9, body, "EC-SIZE"), new ReplyFrame(9, MessageBody.Of("x"), null),
        ];
        foreach (var frame in frames)
        {
            var back = PeerFrameCodec.Decode(frame.Kind, PeerFrameCodec.Encode(frame));
            Assert.Equal(frame.GetType(), back.GetType());
            Assert.Equal(PeerFrameCodec.Encode(frame), PeerFrameCodec.Encode(back));
        }
        var grant = (GrantFrame)PeerFrameCodec.Decode(McsFrameKind.Grant, PeerFrameCodec.Encode(frames[3]));
        Assert.Equal(body.Image, grant.Message.Image);
        Assert.Equal([0, 4], grant.Message.Extents!.FixedAt);
        Assert.Equal([2, 3], grant.Message.Extents.Lengths);
        Assert.True(MessageTag.SameParty(tag, (MessageTag)grant.Message.Slots[0]!));
        Assert.Equal(7, ((MessageTag)grant.Message.Slots[0]!).Message);
        Assert.True(((MessageTag)grant.Message.Slots[1]!).IsNull);
    }

    [Fact]
    public void APayloadThatDoesNotParseIsRefused()
    {
        var good = PeerFrameCodec.Encode(new GrantFrame(1, 1, MessageBody.Of("abc")));
        Assert.Throws<InvalidDataException>(() => PeerFrameCodec.Decode(McsFrameKind.Grant, good[..^1]));                 // truncated
        Assert.Throws<InvalidDataException>(() => PeerFrameCodec.Decode(McsFrameKind.Grant, [.. good, 0]));              // overlong
        Assert.Throws<InvalidDataException>(() => PeerFrameCodec.Decode(McsFrameKind.Ack, []));
        Assert.Throws<InvalidDataException>(() => PeerFrameCodec.Decode(McsFrameKind.Ring, []));                         // a bell's kind is no protocol frame
        var negative = (byte[])good.Clone();
        negative[12 + 3] = 0x80;                                                                                          // the image's count goes negative
        Assert.Throws<InvalidDataException>(() => PeerFrameCodec.Decode(McsFrameKind.Grant, negative));
        var badParty = PeerFrameCodec.Encode(new GrantFrame(1, 1, new MessageBody("", null, [new MessageTag(Mcs, MessageParty.Server, "S", 1)])));
        badParty[badParty.Length - 8 - 7 - 1] = 9;                                                                        // the party byte
        Assert.Throws<InvalidDataException>(() => PeerFrameCodec.Decode(McsFrameKind.Grant, badParty));
        Assert.Throws<ArgumentException>(() => PeerFrameCodec.Encode(new GrantFrame(1, 1, new MessageBody("", null, ["not a tag"]))));
    }

    [Fact]
    public void TheExceptionNamesAreTheCatalogsOwn()
    {
        foreach (string name in new[]
        {
            ExceptionCatalog.McsNoServer, ExceptionCatalog.McsNoRequester, ExceptionCatalog.McsInvalidTag, ExceptionCatalog.McsMessageLength, ExceptionCatalog.McsRequestorFailed,
            ExceptionCatalog.McsAbnormalTermination, ExceptionCatalog.McsNormalTermination, ExceptionCatalog.McsImp,
        })
            Assert.True(ExceptionCatalog.TryGet(name, out _), name);
    }

    // ---------------------------------------------------------------- the in-memory transport

    [Fact]
    public void NoListenerIsVisibleBeforeItListens()
    {
        var network = Network();
        var server = new InMemoryTransport(network, "srv1");
        var requestor = new InMemoryTransport(network, "req1");
        var endpoint = new PeerEndpoint(PeerEndpointKind.Server, "SRV", "srv1");
        List<string> seenInWindow = [];
        network.BetweenListenAndRename = _ =>
        {
            seenInWindow.AddRange(requestor.EnumerateServers("SRV"));
            Assert.Equal(ConnectOutcome.Refused, requestor.Connect(endpoint).Outcome);     // nothing at the final name yet
        };
        server.Publish(endpoint);
        Assert.Empty(seenInWindow);
        Assert.Equal(["srv1"], requestor.EnumerateServers("SRV"));
        Assert.Equal(ConnectOutcome.Connected, requestor.Connect(endpoint).Outcome);
    }

    [Fact]
    public void AKilledRunUnitLeavesItsFileAndTheFirstConnectIsRefusedAndDeletesIt()
    {
        var network = Network();
        var server = new InMemoryTransport(network, "srv1");
        var requestor = new InMemoryTransport(network, "req1");
        var endpoint = new PeerEndpoint(PeerEndpointKind.Server, "SRV", "srv1");
        server.Publish(endpoint);
        var link = requestor.Connect(endpoint).Link;
        server.Abort();
        Assert.True(network.Exists(endpoint));                                          // the kill left the file
        Assert.Contains(requestor.TakeAll(), e => e is LinkLost l && l.Link == link);    // and ended the link for its other side
        Assert.False(requestor.Write(link, McsFrameKind.Ack, PeerFrameCodec.Encode(new AckFrame(1))));
        Assert.Equal(ConnectOutcome.Refused, requestor.Connect(endpoint).Outcome);
        Assert.False(network.Exists(endpoint));                                         // the refusal rule deleted it
        Assert.Equal(ConnectOutcome.Refused, requestor.Connect(endpoint).Outcome);       // a file already gone is skipped
    }

    [Fact]
    public void RingingDeletesTheBellOfAKilledRequestorAndWakesALiveOne()
    {
        var network = Network();
        var dead = new InMemoryTransport(network, "dead");
        var live = new InMemoryTransport(network, "live");
        var server = new InMemoryTransport(network, "srv1");
        dead.Publish(BellOf("SRV", "dead"));
        live.Publish(BellOf("SRV", "live"));
        dead.Abort();
        server.RingBells("SRV");
        Assert.Equal(["live"], network.RunIds(PeerEndpointKind.Bell, "SRV"));
        Assert.Contains(live.TakeAll(), e => e is BellRung);
    }

    // ---------------------------------------------------------------- the exchange

    [Fact]
    public void ARequestAndItsReplyTravelBetweenRequestorAndServer()
    {
        var (network, server, requestor) = Pair();
        requestor.Send("srv  ", "HELLO SERVER");                      // a name is any case, trailing spaces ignored
        Pump(requestor, server);
        Assert.Empty(requestor.Results);                               // a SEND waits until the message is received (ISO 14.9.38.4 GR4)
        Assert.True(network.Exists(BellOf("SRV", "req1")));            // its pending bell
        server.Receive(default, capacity: 100);
        Pump(requestor, server);
        var received = server.Last;
        Assert.True(received.Successful);
        Assert.Equal("HELLO SERVER", received.Image);
        Assert.Equal(12L, received.Length);
        Assert.Equal(MessageParty.Requestor, received.Tag.Party);
        var sent = requestor.Last;
        Assert.True(sent.Successful);
        Assert.Equal(MessageParty.Server, sent.Tag.Party);
        Assert.False(network.Exists(BellOf("SRV", "req1")));           // the bell is deleted at completion
        requestor.Receive(sent.Tag, 50);
        Assert.Single(requestor.Results);                              // the reply is not there yet
        server.SendResponse(received.Tag, "BYE");
        Pump(requestor, server);
        Assert.True(server.Last.Successful);
        Assert.Equal("BYE", requestor.Last.Image);
        Assert.Equal(3L, requestor.Last.Length);
        Assert.True(requestor.Last.Successful);
    }

    [Fact]
    public void ASlotAndAnExtentTableTravelWithTheMessage()
    {
        var (_, server, requestor) = Pair();
        var tag = new MessageTag(Mcs, MessageParty.Requestor, "someone", 4);
        requestor.Send("SRV", new MessageBody("AAKEY", new RecordExtents([0], [2]), [tag]));
        server.Receive(default);
        Pump(requestor, server);
        var got = server.Last;
        Assert.Equal([0], got.Extents!.FixedAt);
        Assert.Equal([2], got.Extents.Lengths);
        Assert.True(MessageTag.SameParty(tag, (MessageTag)got.Slots![0]!));
    }

    [Fact]
    public void ASendToANameNobodyServesIsNoServerAndPublishesAndDeletesItsBell()
    {
        var network = Network();
        var requestor = new Peer(network, "req1");
        int windows = 0;
        network.BetweenListenAndRename = _ => windows++;
        requestor.Send("NOBODY", "x");
        Pump(requestor);
        Assert.Equal(ExceptionCatalog.McsNoServer, requestor.Last.ExceptionName);
        Assert.Equal(1, windows);                                      // the bell was published, once
        Assert.Empty(network.RunIds(PeerEndpointKind.Bell, "NOBODY"));
        Assert.False(requestor.Protocol.Busy);
    }

    [Fact]
    public void AZeroLengthMessageAndOneOverMaxLengthAreRefusedBeforeAnythingIsPublished()
    {
        var network = Network();
        int windows = 0;
        network.BetweenListenAndRename = _ => windows++;
        var requestor = new Peer(network, "req1", definitions: n => n == "LIMITED" ? new ServerDefinition(4, false) : null);
        requestor.Send("SRV", string.Empty);
        requestor.Send("LIMITED", "12345");
        Assert.Equal([ExceptionCatalog.McsMessageLength, ExceptionCatalog.McsMessageLength], requestor.Results.Select(r => r.ExceptionName));
        Assert.Equal(0, windows);
        requestor.Send("LIMITED", "1234");                              // exactly maxLength is allowed: it goes on to find no server
        Pump(requestor);
        Assert.Equal(ExceptionCatalog.McsNoServer, requestor.Last.ExceptionName);
    }

    [Fact]
    public void TwoServersRaceAClaimAndExactlyOneIsGrantedWhileTheOtherIsWithdrawn()
    {
        var network = Network();
        var a = new Peer(network, "srvA", "SRV");
        var b = new Peer(network, "srvB", "SRV");
        var requestor = new Peer(network, "req1");
        a.Serve();
        b.Serve();
        requestor.Send("SRV", "ONE");
        Pump(requestor, a, b);                                          // both links open and both servers hold the offer
        a.Receive(default);
        b.Receive(default);                                             // both CLAIM before the requestor reads either
        Pump(requestor, a, b);
        Assert.Single(requestor.Results, r => r.Successful);
        Assert.Equal(1, new[] { a, b }.Count(p => p.Results.Count == 1 && p.Last.Successful));
        var loser = a.Results.Count == 0 ? a : b;
        Assert.Empty(loser.Results);                                    // withdrawn: still waiting, never sees the message
        requestor.Send("SRV", "TWO");
        Pump(requestor, a, b);
        Assert.Equal("TWO", loser.Last.Image);                          // and takes the next one, on its persistent link
    }

    [Fact]
    public void AWithdrawCrossingAClaimLeavesTheLateClaimantWaiting()
    {
        var network = Network();
        var a = new Peer(network, "srvA", "SRV");
        var b = new Peer(network, "srvB", "SRV");
        var requestor = new Peer(network, "req1");
        a.Serve();
        b.Serve();
        requestor.Send("SRV", "ONE");
        Pump(requestor, a, b);
        a.Receive(default);
        b.Receive(default);
        requestor.Step();                                               // A's CLAIM wins: GRANT to A, WITHDRAW to B ...
        requestor.Step();                                               // ... and B's CLAIM, already written, reaches a link that was withdrawn
        Assert.DoesNotContain(requestor.Log, x => x is CloseLink);      // a late CLAIM is ignored, never a protocol violation
        Pump(requestor, a, b);
        Assert.Equal("ONE", a.Last.Image);
        Assert.Empty(b.Results);                                        // B waits on, its offer withdrawn
        Assert.Single(requestor.Results);
    }

    [Fact]
    public void TheGrantedServerEndingBeforeAckMeansANewRoundOnTheLinksThatRemain()
    {
        var network = Network();
        var a = new Peer(network, "srvA", "SRV");
        var b = new Peer(network, "srvB", "SRV");
        var requestor = new Peer(network, "req1");
        a.Serve();
        b.Serve();
        requestor.Send("SRV", "ONCE");
        Pump(requestor, a, b);
        a.Receive(default);                                             // A claims
        requestor.Step();                                               // the requestor grants A and withdraws B
        a.Transport.Abort();                                            // A dies holding the GRANT: it never wrote ACK
        Pump(requestor, b);
        Assert.Empty(requestor.Results);                                // no program saw the message, so the SEND goes on
        b.Receive(default);
        Pump(requestor, b);
        Assert.Equal("ONCE", b.Last.Image);
        Assert.True(requestor.Last.Successful);
        Assert.Empty(a.Results);                                        // delivered exactly once
    }

    [Fact]
    public void EveryOfferedLinkEndingWithNoneGrantedIsNoServerAfterOneMoreEnumeration()
    {
        var network = Network();
        var a = new Peer(network, "srvA", "SRV");
        var requestor = new Peer(network, "req1");
        a.Serve();
        requestor.Send("SRV", "X");
        Pump(requestor, a);
        a.Transport.Abort();
        int enumerations = requestor.Log.OfType<Enumerate>().Count();
        Pump(requestor);
        Assert.Equal(ExceptionCatalog.McsNoServer, requestor.Last.ExceptionName);
        Assert.Equal(enumerations + 1, requestor.Log.OfType<Enumerate>().Count());
        Assert.False(network.Exists(a.ListenerEndpoint));               // the stale file was refused at and deleted
    }

    [Fact]
    public void ANewServerAppearingBeforeThatEnumerationIsFoundByIt()
    {
        var network = Network();
        var a = new Peer(network, "srvA", "SRV");
        var c = new Peer(network, "srvC", "SRV");
        var requestor = new Peer(network, "req1");
        a.Serve();
        requestor.Send("SRV", "X");
        Pump(requestor, a);
        a.Transport.Abort();
        c.Serve();                                                      // C publishes before the requestor reads the end of A's link
        c.Receive(default);
        Pump(requestor, c);                                             // the enumeration it then makes finds C beside A's stale file
        Assert.Equal("X", c.Last.Image);
        Assert.True(requestor.Last.Successful);
    }

    [Fact]
    public void AServerStartingWhileASendWaitsRingsTheBellAndTakesTheRequest()
    {
        var network = Network();
        var a = new Peer(network, "srvA", "SRV");                       // serves but never receives
        var c = new Peer(network, "srvC", "SRV");
        var requestor = new Peer(network, "req1");
        a.Serve();
        requestor.Send("SRV", "LATE");
        Pump(requestor, a);
        Assert.Empty(requestor.Results);
        c.Serve();                                                      // publishes, then rings the pending bell under p/SRV/
        c.Receive(default);
        Pump(requestor, a, c);
        Assert.Equal("LATE", c.Last.Image);
        Assert.True(requestor.Last.Successful);
        a.Receive(default, wait: MessageWait.Poll);                     // A's offer was withdrawn: nothing is left to take
        Assert.Equal("", a.Last.Image);
    }

    [Fact]
    public void ALostOrRepeatedRingCostsAtMostOneEnumerationAndNeverDuplicatesALink()
    {
        var (network, server, requestor) = Pair();
        requestor.Send("SRV", "X");
        Pump(requestor, server);
        int links = requestor.Log.OfType<Connect>().Count();
        server.Transport.RingBells("SRV");
        server.Transport.RingBells("SRV");
        Pump(requestor, server);
        Assert.Equal(links, requestor.Log.OfType<Connect>().Count());
        server.Receive(default);
        Pump(requestor, server);
        Assert.Equal("X", server.Last.Image);
        Assert.False(network.Exists(BellOf("SRV", "req1")));
        server.Transport.RingBells("SRV");                              // a ring after completion finds no bell
        Pump(requestor, server);
    }

    [Fact]
    public void ARingBetweenThePublishedBellAndTheEnumerationFindsTheServerWithoutADuplicateLink()
    {
        var network = Network();
        var requestor = new Peer(network, "req1");
        var server = new Peer(network, "srv1", "SRV");
        requestor.AfterAction = action =>
        {
            if (action is Publish { Endpoint.Kind: PeerEndpointKind.Bell }) server.Serve();     // publishes and rings the bell that just became visible
        };
        requestor.Send("SRV", "X");
        Pump(requestor, server);
        Assert.Single(requestor.Log.OfType<Connect>());                 // the enumeration's connect and the ring's scan share one link
        server.Receive(default);
        Pump(requestor, server);
        Assert.Equal("X", server.Last.Image);
    }

    [Fact]
    public void ARequestorWithAStartCommandActivatesOnceAndThenFindsTheServerOrNone()
    {
        var network = Network();
        var requestor = new Peer(network, "req1", definitions: n => new ServerDefinition(null, HasStart: true));
        var started = new Peer(network, "srvS", "SRV");
        requestor.OnActivate = _ =>
        {
            started.Serve();
            return true;
        };
        requestor.Send("SRV", "WAKE");
        Pump(requestor, started);
        Assert.Single(requestor.Activations);
        started.Receive(default);
        Pump(requestor, started);
        Assert.Equal("WAKE", started.Last.Image);

        var none = new Peer(network, "req2", definitions: n => new ServerDefinition(null, HasStart: true)) { OnActivate = _ => true };
        none.Send("OTHER", "x");
        Pump(none);
        Assert.Single(none.Activations);                                 // activated once; the server it started never appeared
        Assert.Equal(ExceptionCatalog.McsNoServer, none.Last.ExceptionName);

        var failing = new Peer(network, "req3", definitions: n => new ServerDefinition(null, HasStart: true)) { OnActivate = _ => false };
        failing.Send("OTHER", "x");
        Pump(failing);
        Assert.Equal(ExceptionCatalog.McsImp, failing.Last.ExceptionName);
        Assert.Empty(network.RunIds(PeerEndpointKind.Bell, "OTHER"));
    }

    // ---------------------------------------------------------------- RECEIVE's waiting

    [Fact]
    public void APollWithNothingWaitingIsSuccessfulWithSpacesAndZeroAndArmsNoTimer()
    {
        var network = Network();
        var server = new Peer(network, "srv1", "SRV");
        server.Serve();
        server.Receive(default, wait: MessageWait.Poll);
        server.Receive(default, wait: new MessageWait(MessageWaitKind.Seconds, 0));
        server.Receive(default, wait: new MessageWait(MessageWaitKind.Seconds, -1));
        Assert.Equal(3, server.Results.Count);
        Assert.All(server.Results, r =>
        {
            Assert.True(r.Successful);
            Assert.Equal("", r.Image);
            Assert.Equal(0L, r.Length);
            Assert.True(r.Tag.IsNull);
        });
        Assert.Empty(server.Deadlines);
    }

    [Fact]
    public void APollTakesAMessageThatIsWaiting()
    {
        var (_, server, requestor) = Pair();
        requestor.Send("SRV", "HERE");
        Pump(requestor, server);
        server.Receive(default, wait: MessageWait.Poll);
        Pump(requestor, server);
        Assert.Equal("HERE", server.Last.Image);
    }

    [Fact]
    public void ADeadlineWithNothingInFlightCompletesEmptyAndAStaleTimerDoesNothing()
    {
        var (_, server, _) = Pair();
        server.Receive(default, wait: new MessageWait(MessageWaitKind.Seconds, 2));
        var armed = Assert.Single(server.Deadlines);
        Assert.Equal(2, armed.Seconds);
        server.Post(new DeadlinePassed(armed.Token));
        Assert.Equal("", server.Last.Image);
        Assert.Equal(0L, server.Last.Length);
        server.Post(new DeadlinePassed(armed.Token));                   // a stale timer does nothing
        Assert.Single(server.Results);
    }

    [Fact]
    public void ADeadlineWithAClaimInFlightWaitsForTheGrantOrWithdraw()
    {
        var (_, server, requestor) = Pair();
        requestor.Send("SRV", "RACE");
        Pump(requestor, server);
        server.Receive(default, wait: new MessageWait(MessageWaitKind.Seconds, 1));
        Assert.Empty(server.Results);                                    // CLAIM written, GRANT not yet read
        server.Post(new DeadlinePassed(server.Deadlines[0].Token));
        Assert.Empty(server.Results);                                    // the requestor still holds the message: the server waits for it
        Pump(requestor, server);
        Assert.Equal("RACE", server.Last.Image);
    }

    [Fact]
    public void ADeadlineAfterAWithdrawWithNothingElseCompletesEmpty()
    {
        var network = Network();
        var a = new Peer(network, "srvA", "SRV");
        var b = new Peer(network, "srvB", "SRV");
        var requestor = new Peer(network, "req1");
        a.Serve();
        b.Serve();
        requestor.Send("SRV", "X");
        Pump(requestor, a, b);
        a.Receive(default);
        b.Receive(default, wait: new MessageWait(MessageWaitKind.Seconds, 5));
        Pump(requestor);                                                 // A's claim is first: GRANT to A, WITHDRAW to B
        b.Post(new DeadlinePassed(b.Deadlines[0].Token));                // B's deadline passes with its CLAIM still in flight
        Assert.Empty(b.Results);
        Pump(requestor, a, b);                                           // B reads the WITHDRAW and, with nothing else, gives up
        Assert.Equal("X", a.Last.Image);
        Assert.Equal("", b.Last.Image);
        Assert.Equal(0L, b.Last.Length);
    }

    [Fact]
    public void AReplyThatArrivedBeforeTheDeadlineIsDeliveredNotTimedOut()
    {
        var (_, server, requestor) = Pair();
        requestor.Send("SRV", "Q");
        Pump(requestor, server);
        server.Receive(default);
        Pump(requestor, server);
        var received = server.Last.Tag;
        requestor.Receive(requestor.Last.Tag, 10, new MessageWait(MessageWaitKind.Seconds, 3));
        server.SendResponse(received, "A");                              // queued on the requestor's transport, not yet read
        while (requestor.StepTransport()) { }                            // the peer drains its frames before it posts the deadline (5.4.4)
        Assert.Equal("A", requestor.Last.Image);
        requestor.Post(new DeadlinePassed(requestor.Deadlines[0].Token));
        Assert.Equal(2, requestor.Results.Count);                        // the late deadline is stale
    }

    [Fact]
    public void ARequestorsReceiveWithNoReplyTimesOutEmpty()
    {
        var (_, server, requestor) = Pair();
        requestor.Send("SRV", "Q");
        Pump(requestor, server);
        server.Receive(default);
        Pump(requestor, server);
        requestor.Receive(requestor.Last.Tag, 10, new MessageWait(MessageWaitKind.Seconds, 1));
        requestor.Post(new DeadlinePassed(requestor.Deadlines[0].Token));
        Assert.True(requestor.Last.Successful);
        Assert.Equal("", requestor.Last.Image);
        Assert.Equal(0L, requestor.Last.Length);
        Assert.True(requestor.Last.Tag.IsNull);
    }

    // ---------------------------------------------------------------- SEND Format 2

    [Fact]
    public void ASendResponseTestsItsTagInGeneralRuleOrder()
    {
        var (_, server, requestor) = Pair();
        requestor.Send("SRV", "Q");
        Pump(requestor, server);
        server.Receive(default);
        Pump(requestor, server);
        var real = server.Last.Tag;
        var foreign = new MessageTag("ffffffffffffffff", MessageParty.Requestor, "req1", 1);
        var serverTag = requestor.Results[0].Tag;
        server.SendResponse(real, string.Empty);                         // GR1: zero length first, whatever the tag
        server.SendResponse(MessageTag.Null, "A");
        server.SendResponse(serverTag, "A");
        server.SendResponse(foreign, "A");
        server.SendResponse(new MessageTag(Mcs, MessageParty.Requestor, "req1", 99), "A");     // never received
        Assert.Equal(
            [ExceptionCatalog.McsMessageLength, ExceptionCatalog.McsNoRequester, ExceptionCatalog.McsNoRequester, ExceptionCatalog.McsInvalidTag, ExceptionCatalog.McsNoRequester],
            server.Results.Skip(1).Select(r => r.ExceptionName));
        server.SendResponse(real, "A");
        Assert.True(server.Last.Successful);
        server.SendResponse(real, "A");                                  // answered: the message is no longer active
        Assert.Equal(ExceptionCatalog.McsNoRequester, server.Last.ExceptionName);
    }

    [Fact]
    public void AReplyToARequestorThatEndedSucceedsAndIsDropped()
    {
        var (_, server, requestor) = Pair();
        requestor.Send("SRV", "Q");
        Pump(requestor, server);
        server.Receive(default);
        Pump(requestor, server);
        var tag = server.Last.Tag;
        requestor.Transport.Abort();
        Pump(server);
        int writes = server.Log.OfType<Write>().Count();
        server.SendResponse(tag, "A");
        Assert.True(server.Last.Successful);
        Assert.Equal(writes, server.Log.OfType<Write>().Count());        // nothing was written
        server.Receive(tag, wait: MessageWait.Poll);
        Assert.Equal(ExceptionCatalog.McsRequestorFailed, server.Last.ExceptionName);
    }

    [Fact]
    public void AnOversizeMessageIsTakenTruncatedAndStillAnswerable()
    {
        var (_, server, requestor) = Pair();
        requestor.Send("SRV", "TOOLONGFORME");
        Pump(requestor, server);
        server.Receive(default, capacity: 3);
        Pump(requestor, server);
        var received = server.Last;
        Assert.Equal(ExceptionCatalog.McsMessageLength, received.ExceptionName);
        Assert.Equal("TOO", received.Image);
        Assert.Null(received.Length);                                    // data-name-2 unchanged (Q7)
        Assert.True(requestor.Last.Successful);                          // the SEND itself completed: the message was taken
        server.SendResponse(received.Tag, "OK");
        Assert.True(server.Last.Successful);
    }

    // ---------------------------------------------------------------- the exception conditions

    [Fact]
    public void ReceiveFromNullInARunUnitThatServesNothingIsNoRequester()
    {
        var requestor = new Peer(Network(), "req1");
        requestor.Receive(default);
        Assert.Equal(ExceptionCatalog.McsNoRequester, requestor.Last.ExceptionName);
    }

    [Fact]
    public void ReceiveFromATagOfAnotherMcsOrAMissingRequestorIsInvalidTagOrRequestorFailed()
    {
        var (_, server, _) = Pair();
        server.Receive(new MessageTag("ffffffffffffffff", MessageParty.Requestor, "req1", 1));
        server.Receive(new MessageTag(Mcs, MessageParty.Requestor, "nobody", 1));
        server.Receive(new MessageTag("ffffffffffffffff", MessageParty.Server, "SRV", 1));
        Assert.Equal([ExceptionCatalog.McsInvalidTag, ExceptionCatalog.McsRequestorFailed, ExceptionCatalog.McsInvalidTag], server.Results.Select(r => r.ExceptionName));
    }

    [Fact]
    public void ReceiveFromAServerTagDeliversOnceAndThenIsInvalidTag()
    {
        var (_, server, requestor) = Pair();
        requestor.Send("SRV", "Q");
        Pump(requestor, server);
        server.Receive(default);
        Pump(requestor, server);
        var tag = requestor.Last.Tag;
        server.SendResponse(server.Last.Tag, "A1");
        Pump(requestor, server);
        requestor.Receive(tag);
        Assert.Equal("A1", requestor.Last.Image);
        requestor.Receive(tag);                                          // already taken
        requestor.Receive(new MessageTag(Mcs, MessageParty.Server, "SRV", 77));   // never sent
        Assert.Equal([ExceptionCatalog.McsInvalidTag, ExceptionCatalog.McsInvalidTag], requestor.Results.Skip(2).Select(r => r.ExceptionName));
    }

    [Fact]
    public void AServerThatEndsWithNoReplyIsAbnormalTerminationEvenForAReceiveAlreadyWaiting()
    {
        var (_, server, requestor) = Pair();
        requestor.Send("SRV", "Q");
        Pump(requestor, server);
        server.Receive(default);
        Pump(requestor, server);
        var tag = requestor.Last.Tag;
        requestor.Receive(tag);
        server.Transport.Abort();
        Pump(requestor);
        Assert.Equal(ExceptionCatalog.McsAbnormalTermination, requestor.Last.ExceptionName);
        requestor.Receive(tag);
        Assert.Equal(ExceptionCatalog.McsInvalidTag, requestor.Last.ExceptionName);   // the ended marker was taken
    }

    [Fact]
    public void AForwardedExceptionNameMakesTheReceiveUnsuccessfulButDeliversTheReply()
    {
        var (_, server, requestor) = Pair();
        requestor.Send("SRV", "Q");
        Pump(requestor, server);
        server.Receive(default);
        Pump(requestor, server);
        server.SendResponse(server.Last.Tag, "WHY", raising: "EC-SIZE");
        Pump(requestor, server);
        requestor.Receive(requestor.Results[0].Tag);
        Assert.Equal("EC-SIZE", requestor.Last.ExceptionName);
        Assert.False(requestor.Last.Successful);
        Assert.Equal("WHY", requestor.Last.Image);
        Assert.Equal(3L, requestor.Last.Length);
    }

    // ---------------------------------------------------------------- run-unit end

    [Fact]
    public void AServerEndingWithAnActiveMessageRepliesTheTerminationExceptionAndSpacesAndUnpublishes()
    {
        foreach (bool abnormal in new[] { false, true })
        {
            var (network, server, requestor) = Pair();
            requestor.Send("SRV", "Q");
            server.Receive(default, capacity: 6);
            Pump(requestor, server);
            var tag = requestor.Last.Tag;
            server.End(abnormal);
            Assert.False(network.Exists(server.ListenerEndpoint));
            Pump(requestor);
            requestor.Receive(tag, 10);
            Assert.Equal(abnormal ? ExceptionCatalog.McsAbnormalTermination : ExceptionCatalog.McsNormalTermination, requestor.Last.ExceptionName);
            Assert.Equal("      ", requestor.Last.Image);                  // the recorded size, as spaces
        }
    }

    [Fact]
    public void ARequestorEndingDuringItsSendDeletesItsBell()
    {
        var (network, server, requestor) = Pair();
        requestor.Send("SRV", "Q");
        Pump(requestor, server);
        Assert.True(network.Exists(BellOf("SRV", "req1")));
        requestor.End();
        Assert.False(network.Exists(BellOf("SRV", "req1")));
        Pump(server);
        server.Receive(default, wait: MessageWait.Poll);                  // its offer ended with it
        Assert.Equal("", server.Last.Image);
    }

    [Fact]
    public void ARequestorKilledWhileTheServerHoldsItsOfferLeavesTheServerWaiting()
    {
        var (_, server, requestor) = Pair();
        requestor.Send("SRV", "Q");
        Pump(requestor, server);
        requestor.Transport.Abort();
        Pump(server);
        server.Receive(default, wait: new MessageWait(MessageWaitKind.Seconds, 1));
        Assert.Empty(server.Results);
        server.Post(new DeadlinePassed(server.Deadlines[0].Token));
        Assert.Equal("", server.Last.Image);
    }

    // ---------------------------------------------------------------- links that do not open properly

    [Fact]
    public void AServerWhoseHelloNamesAnotherServerIsClosedAndNotTriedAgain()
    {
        var network = Network();
        var fake = new InMemoryTransport(network, "srvX");
        fake.Publish(new PeerEndpoint(PeerEndpointKind.Server, "SRV", "srvX"));   // a hash-collided directory: its server is not SRV
        var requestor = new Peer(network, "req1");
        requestor.Send("SRV", "Q");
        bool answered = false;
        for (int guard = 0; guard < 20 && requestor.Results.Count == 0; guard++)
        {
            requestor.Step();
            while (fake.TryTake(out var e))
            {
                if (e is not LinkAccepted a) continue;
                answered = true;
                fake.Write(a.Link, McsFrameKind.Hello, PeerFrameCodec.Encode(new HelloFrame(1, Mcs, "srvX", 1, "OTHER")));
            }
        }
        Assert.True(answered);
        Assert.Equal(ExceptionCatalog.McsNoServer, requestor.Last.ExceptionName);
        Assert.Single(requestor.Log.OfType<Connect>());
    }

    [Fact]
    public void AFrameOutOfOrderClosesTheLink()
    {
        var network = Network();
        var server = new Peer(network, "srv1", "SRV");
        server.Serve();
        var rogue = new InMemoryTransport(network, "rogue");
        var link = rogue.Connect(server.ListenerEndpoint).Link;
        rogue.Write(link, McsFrameKind.Offer, PeerFrameCodec.Encode(new OfferFrame(1, 1)));      // an OFFER before HELLO
        Pump(server);
        Assert.Contains(rogue.TakeAll(), e => e is LinkLost);
        Assert.Single(server.Log.OfType<CloseLink>());
    }

    [Fact]
    public void AConnectionToARunUnitThatServesNothingIsClosed()
    {
        var network = Network();
        var requestor = new Peer(network, "req1");
        var link = new PeerLinkId(5);
        requestor.Post(new LinkOpened(link));
        Assert.Equal(new CloseLink(link), Assert.Single(requestor.Log.OfType<CloseLink>()));
    }

    // ---------------------------------------------------------------- failures of the medium

    [Fact]
    public void AnActionThatFailsIsImpAndItsSiblingsAnswersAreStaleForTheNextStatement()
    {
        var network = Network();
        var a = new Peer(network, "srvA", "SRV");
        var b = new Peer(network, "srvB", "SRV");
        var requestor = new Peer(network, "req1");
        a.Serve();
        b.Serve();
        requestor.Send("SRV", "X");
        requestor.Step();                                                // the enumeration answered: two connects issued
        Assert.Equal(2, requestor.Log.OfType<Connect>().Count());
        requestor.DropAnswer();                                          // the first connect failed: its answer is ActionFailed
        requestor.Post(new ActionFailed(requestor.Log.OfType<Connect>().First(), "socket error"));
        Assert.Equal(ExceptionCatalog.McsImp, requestor.Last.ExceptionName);
        Assert.Equal("socket error", requestor.Last.Detail);
        Assert.Empty(network.RunIds(PeerEndpointKind.Bell, "SRV"));
        requestor.Send("SRV", "Y");                                      // the answer still owed belongs to the first statement
        Pump(requestor, a, b);
        Assert.Single(requestor.Results);                                // the second SEND waits for its server
        a.Receive(default);
        b.Receive(default);
        Pump(requestor, a, b);
        Assert.Equal(2, requestor.Results.Count);
        Assert.True(requestor.Last.Successful);
        Assert.Equal(1, new[] { a, b }.Count(p => p.Results.Count == 1 && p.Last.Image == "Y"));
    }

    [Fact]
    public void AConnectThatFailsFreesItsServerForTheNextSend()
    {
        var network = Network();
        var a = new Peer(network, "srvA", "SRV");
        var requestor = new Peer(network, "req1");
        a.Serve();
        requestor.Send("SRV", "X");
        requestor.Step();                                                // the enumeration answered: one connect issued
        requestor.DropAnswer();
        requestor.Post(new ActionFailed(requestor.Log.OfType<Connect>().Single(), "socket error"));
        Assert.Equal(ExceptionCatalog.McsImp, requestor.Last.ExceptionName);
        requestor.Send("SRV", "Y");                                      // the server is alive and published: it is connected to again, not skipped
        Pump(requestor, a);
        Assert.Equal(2, requestor.Log.OfType<Connect>().Count());
        Assert.Single(requestor.Results);
        a.Receive(default);
        Pump(requestor, a);
        Assert.True(requestor.Last.Successful);
        Assert.Equal("Y", a.Last.Image);
    }

    [Fact]
    public void ASendThatEndsImpWithALinkGrantedClosesThatLinkSoTheNextSendIsNotStuckOnIt()
    {
        var network = Network();
        var a = new Peer(network, "srvA", "SRV");
        var requestor = new Peer(network, "req1");
        a.Serve();
        requestor.Send("SRV", "X");
        Pump(requestor, a);                                              // the offer is made and held
        a.Receive(default);                                              // the claim
        while (!requestor.Log.OfType<Write>().Any(w => w.Frame is GrantFrame)) Assert.True(requestor.Step());
        requestor.Post(new BellRang());                                  // the rescan issues an enumeration ...
        requestor.DropAnswer();
        requestor.Post(new ActionFailed(requestor.Log.OfType<Enumerate>().Last(), "socket error"));   // ... which fails with the grant not yet acknowledged
        Assert.Equal(ExceptionCatalog.McsImp, requestor.Last.ExceptionName);
        Assert.Contains(requestor.Log, action => action is CloseLink);
        Pump(requestor, a);
        requestor.Send("SRV", "Y");
        Pump(requestor, a);
        a.Receive(default);
        Pump(requestor, a);
        Assert.True(requestor.Last.Successful);                          // the second SEND did not hang on the granted link
        Assert.Equal("Y", a.Last.Image);
    }

    [Fact]
    public void AListenerThatNeverPublishedMakesTheReceiveImpAtTheStatement()
    {
        var network = Network();
        var server = new Peer(network, "srvA", "SRV");
        server.Serve();
        server.Post(new ActionFailed(server.Log.OfType<Publish>().Single(), "directory refused"));    // no statement is current
        Assert.Empty(server.Results);
        server.Receive(default);
        Assert.Equal(ExceptionCatalog.McsImp, server.Last.ExceptionName);
        Assert.Equal("directory refused", server.Last.Detail);
        Assert.False(server.Protocol.Busy);
    }

    [Fact]
    public void AListenerThatFailsToPublishWhileAReceiveWaitsCompletesThatReceiveImp()
    {
        var network = Network();
        var server = new Peer(network, "srvA", "SRV");
        server.Serve();
        server.Receive(default);
        Assert.Empty(server.Results);
        server.Post(new ActionFailed(server.Log.OfType<RingBells>().Single(), "bell directory refused"));
        Assert.Equal(ExceptionCatalog.McsImp, server.Last.ExceptionName);
    }

    [Fact]
    public void ABellThatFailsToPublishLeavesTheEnumerationAnswerStaleForTheNextSend()
    {
        var network = Network();
        var a = new Peer(network, "srvA", "SRV");                        // not serving yet: the first enumeration answers with nothing
        var requestor = new Peer(network, "req1");
        requestor.Send("SRV", "X");
        requestor.Post(new ActionFailed(requestor.Log.OfType<Publish>().Single(), "bell directory refused"));
        Assert.Equal(ExceptionCatalog.McsImp, requestor.Last.ExceptionName);
        a.Serve();
        requestor.Send("SRV", "Y");                                      // the first SEND's empty answer must not be read as this SEND's
        Pump(requestor, a);
        Assert.Single(requestor.Results);
        a.Receive(default);
        Pump(requestor, a);
        Assert.True(requestor.Last.Successful);
        Assert.Equal("Y", a.Last.Image);
    }

    [Fact]
    public void ARunUnitThatSendsToTheNameItServesFindsNoServerInItself()
    {
        var network = Network();
        var own = new Peer(network, "srvA", "SRV");
        own.Serve();
        own.Send("SRV", "X");
        Pump(own);
        Assert.Equal(ExceptionCatalog.McsNoServer, own.Last.ExceptionName);
    }

    [Fact]
    public void AServerTagOfAnotherServerIsAnInvalidTagEvenForASerialThisRunUnitSent()
    {
        var (_, server, requestor) = Pair();
        requestor.Send("SRV", "Q");
        Pump(requestor, server);
        server.Receive(default);
        Pump(requestor, server);
        Assert.Equal(1, requestor.Last.Tag.Message);
        requestor.Receive(new MessageTag(Mcs, MessageParty.Server, "OTHER", requestor.Last.Tag.Message));
        Assert.Equal(ExceptionCatalog.McsInvalidTag, requestor.Last.ExceptionName);
        Assert.False(requestor.Protocol.Busy);
    }

    [Fact]
    public void AStatementWhileAnotherIsCurrentIsAProgrammingError()
    {
        var (_, _, requestor) = Pair();
        requestor.Send("SRV", "Q");
        Assert.Throws<InvalidOperationException>(() => requestor.Send("SRV", "R"));
        Assert.Throws<InvalidOperationException>(() => requestor.Receive(default));
    }
}

internal static class TransportTestExtensions
{
    internal static List<TransportEvent> TakeAll(this InMemoryTransport transport)
    {
        List<TransportEvent> events = [];
        while (transport.TryTake(out var e)) events.Add(e);
        return events;
    }
}
