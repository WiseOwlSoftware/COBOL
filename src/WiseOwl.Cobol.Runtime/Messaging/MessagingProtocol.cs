// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime.Mcs;

namespace WiseOwl.Cobol.Runtime.Messaging;

/// <summary>
/// ⛔ THE 2023 MESSAGE CONTROL PROTOCOL AS A STATE MACHINE (docs/rearchitecture/DESIGN-message-control.md design 5.2 to design 5.6; ISO §14.9.38.4 SEND, §14.9.31.4
/// RECEIVE, §14.6.11 item 7). It takes the current statement (<see cref="BeginSend"/>, <see cref="BeginSendResponse"/>, <see cref="BeginReceive"/>) and
/// the events of <see cref="ProtocolEvent"/> (<see cref="Handle"/>), and answers each with the <see cref="ProtocolAction"/>s its peer executes on an
/// <see cref="IPeerTransport"/>. It calls nothing, owns no thread and reads no clock, so one scripted scheduler can drive every interleaving the
/// design lists (§7.2 "Protocol"). It runs on the run unit's thread only; at most one statement is current.
/// <para><b>State, in memory only</b> (design 5.2). A requestor holds <c>Pending</c> (<see cref="SendState"/>, its one SEND in flight), <c>Sent</c> (messages whose
/// reply is still due) and <c>Replies</c> (per serial: a reply, or "server ended"); a server holds <c>Offers</c> (per link, oldest first) and <c>Active</c>
/// (per message taken: requestor, serial, receiving size). A run unit may be both. Links are kept per server across SENDs.</para>
/// <para><b>Rounds.</b> A requestor numbers the offers it makes: a link is offered the current round once it is ready, the first CLAIM of that round gets
/// the GRANT and every other offered link a WITHDRAW, so every link offered a round receives one GRANT or WITHDRAW for it before the SEND returns, or ends
/// (design 5.4.1's invariant). A granted link that ends before ACK means no program saw the message, since a server writes ACK before its RECEIVE returns, so a new
/// round starts on the links that remain.</para>
/// <para><b>Frames that cannot be.</b> A frame a role never receives, or one out of order (an OFFER before HELLO), closes the link (design 6: "A reader that meets a kind
/// it does not expect closes the link"); the protocol treats the link as ended.</para>
/// </summary>
internal sealed class MessagingProtocol
{
    private enum OutPhase { AwaitHello, Ready, Offered, Granted }

    private enum SendPhase { Scanning, Waiting, Activating }

    private sealed class OutLink(PeerLinkId id, string runId, string name)
    {
        internal PeerLinkId Id { get; } = id;
        internal string RunId { get; } = runId;
        internal string Name { get; } = name;
        internal OutPhase Phase { get; set; } = OutPhase.AwaitHello;
    }

    private sealed class InLink(PeerLinkId id)
    {
        internal PeerLinkId Id { get; } = id;
        internal string? RequestorRunId { get; set; }
    }

    private sealed class Offer(long sequence, InLink link, long serial, int round)
    {
        internal long Sequence { get; } = sequence;
        internal InLink Link { get; } = link;
        internal long Serial { get; } = serial;
        internal int Round { get; } = round;
    }

    private sealed class SendState(long serial, string name, string spelling, MessageBody body, int round)
    {
        internal long Serial { get; } = serial;
        internal string Name { get; } = name;
        internal string Spelling { get; } = spelling;
        internal MessageBody Body { get; } = body;
        internal int Round { get; set; } = round;
        internal OutLink? Granted { get; set; }
        internal SendPhase Phase { get; set; } = SendPhase.Scanning;
        internal int Outstanding { get; set; }
        internal bool Activated { get; set; }
        internal bool BellPublished { get; set; }

        /// <summary>Servers this SEND does not connect to again: refused (the transport deleted the file), or closed by the protocol after a HELLO or a
        /// frame that cannot be, which a rescan would find again and loop on. A server whose link merely ended is not burned: a killed one's file is
        /// found by the next enumeration, refused and deleted (design 5.4.1 "The refusal rule").</summary>
        internal HashSet<string> Burned { get; } = [];
    }

    private enum ReceiveKind { AnyRequestor, FromRequestor, FromServer }

    private sealed class ReceiveState(int token, ReceiveKind kind, int capacity)
    {
        internal int Token { get; } = token;
        internal ReceiveKind Kind { get; } = kind;
        internal int Capacity { get; } = capacity;
        internal string? RequestorRunId { get; set; }
        internal long Serial { get; set; }
        internal bool DeadlinePassed { get; set; }
        internal Offer? Claim { get; set; }
    }

    private readonly string _mcs;
    private readonly string _runId;
    private readonly int _pid;
    private readonly string? _served;
    private readonly Func<string, ServerDefinition?> _definitions;
    private readonly List<ProtocolAction> _actions = [];

    // Requestor side.
    private readonly Dictionary<PeerLinkId, OutLink> _outByLink = [];
    private readonly Dictionary<string, OutLink> _outByRun = new(StringComparer.Ordinal);
    private readonly HashSet<string> _connecting = new(StringComparer.Ordinal);
    private readonly Dictionary<long, OutLink> _sent = [];
    private readonly Dictionary<long, (MessageBody? Body, string? Exception)> _replies = [];
    private SendState? _send;
    private int _staleAnswers;
    private long _serial;
    private int _roundSequence;

    // Server side.
    private readonly Dictionary<PeerLinkId, InLink> _inByLink = [];
    private readonly Dictionary<string, InLink> _inByRequestor = new(StringComparer.Ordinal);
    private readonly List<Offer> _offers = [];
    private readonly Dictionary<(string Requestor, long Serial), int> _active = [];
    private long _offerSequence;

    private ReceiveState? _receive;
    private int _token;
    private bool _ended;

    /// <summary>A protocol for the run unit <paramref name="runId"/> of the MCS <paramref name="mcs"/> (its <c>&lt;h&gt;</c>), which serves
    /// <paramref name="servedName"/> when that is not null. <paramref name="definitions"/> answers what <c>mcs.json</c> defines for a normalized server name.</summary>
    internal MessagingProtocol(string mcs, string runId, int pid, string? servedName, Func<string, ServerDefinition?> definitions)
    {
        _mcs = mcs;
        _runId = runId;
        _pid = pid;
        _served = servedName is null ? null : McsServerName.Normalize(servedName);
        _definitions = definitions;
    }

    /// <summary>True while a SEND or RECEIVE is current.</summary>
    internal bool Busy => _send is not null || _receive is not null;

    // ---------------------------------------------------------------- statements

    /// <summary>A run unit that serves a name publishes its listener, then rings the pending bells (design 5.4.1 "The pending bell").</summary>
    internal IReadOnlyList<ProtocolAction> Serve()
    {
        if (_served is null) throw new InvalidOperationException("this run unit serves no name");
        string spelling = McsServerName.Spell(_served);
        _actions.Add(new Publish(new PeerEndpoint(PeerEndpointKind.Server, spelling, _runId)));
        _actions.Add(new RingBells(spelling));
        return Flush();
    }

    /// <summary>SEND Format 1 (design 5.4.1): <paramref name="server"/> is the name as the program spells it.</summary>
    internal IReadOnlyList<ProtocolAction> BeginSend(string server, MessageBody message)
    {
        EnsureIdle();
        string name = McsServerName.Normalize(server);
        int? maxLength = _definitions(name)?.MaxLength;
        if (message.Image.Length == 0 || (maxLength is { } limit && message.Image.Length > limit))
        {
            _actions.Add(new Complete(StatementResult.Failed(McsException.MessageLength)));   // GR1, §0 Q2: nothing has been published yet
            return Flush();
        }
        var send = _send = new SendState(++_serial, name, McsServerName.Spell(name), message, ++_roundSequence);
        _actions.Add(new Publish(Bell(send)));
        send.BellPublished = true;
        OfferToReadyLinks(send);
        Scan(send);
        return Flush();
    }

    /// <summary>SEND Format 2 (design 5.4.2), tested in general-rule order. It never waits.</summary>
    internal IReadOnlyList<ProtocolAction> BeginSendResponse(MessageTag to, MessageBody message, string? raising)
    {
        EnsureIdle();
        StatementResult? refused = null;
        if (message.Image.Length == 0) refused = StatementResult.Failed(McsException.MessageLength);
        else if (to.Party != MessageParty.Requestor) refused = StatementResult.Failed(McsException.NoRequester);
        else if (!string.Equals(to.Mcs, _mcs, StringComparison.Ordinal)) refused = StatementResult.Failed(McsException.InvalidTag);
        else if (!_active.Remove((to.Name!, to.Message), out _)) refused = StatementResult.Failed(McsException.NoRequester);
        if (refused is not null)
        {
            _actions.Add(new Complete(refused));
            return Flush();
        }
        // To an ended link the SEND succeeds and the reply is dropped.
        if (_inByRequestor.TryGetValue(to.Name!, out var link))
            _actions.Add(new Write(link.Id, new ReplyFrame(to.Message, message, raising)));
        _actions.Add(new Complete(StatementResult.SentResponse()));
        return Flush();
    }

    /// <summary>RECEIVE (design 5.4.2, design 5.4.3): <paramref name="from"/> NULL takes the oldest message from any requestor, a requestor tag the next from that
    /// requestor, a server tag the reply to that message. <paramref name="capacity"/> is identifier-1's maximum length in character positions.</summary>
    internal IReadOnlyList<ProtocolAction> BeginReceive(MessageTag from, int capacity, MessageWait wait)
    {
        EnsureIdle();
        ReceiveState receive;
        if (from.IsNull)
        {
            if (_served is null)
                return Done(StatementResult.Failed(McsException.NoRequester));                 // §0 Q2: no requestor can address this run unit
            receive = new ReceiveState(++_token, ReceiveKind.AnyRequestor, capacity);
        }
        else
        {
            if (!string.Equals(from.Mcs, _mcs, StringComparison.Ordinal)) return Done(StatementResult.Failed(McsException.InvalidTag));
            if (from.Party == MessageParty.Requestor)
            {
                // §14.9.31.4 GR1 b: a missing or ended link is EC-MCS-REQUESTOR-FAILED.
                if (!_inByRequestor.ContainsKey(from.Name!)) return Done(StatementResult.Failed(McsException.RequestorFailed));
                receive = new ReceiveState(++_token, ReceiveKind.FromRequestor, capacity) { RequestorRunId = from.Name };
            }
            else
            {
                // A reply already taken, or a serial never sent, is EC-MCS-INVALID-TAG.
                if (!_replies.ContainsKey(from.Message) && !_sent.ContainsKey(from.Message)) return Done(StatementResult.Failed(McsException.InvalidTag));
                receive = new ReceiveState(++_token, ReceiveKind.FromServer, capacity) { Serial = from.Message };
            }
        }
        _receive = receive;
        if (wait.AsksOnce) receive.DeadlinePassed = true;
        else if (wait.Kind == MessageWaitKind.Seconds) _actions.Add(new ArmDeadline(receive.Token, wait.Seconds));
        AdvanceReceive(receive);
        return Flush();
    }

    /// <summary>The run unit ends (design 5.6): each active message is answered with EC-MCS-NORMAL-TERMINATION or EC-MCS-ABNORMAL-TERMINATION and spaces of the
    /// size its RECEIVE recorded, because "temp is a temporary data item that is the size of the receiving data item in the associated RECEIVE statement"
    /// (ISO §14.6.11 item 7) that no statement set; then the links, the listener and the pending bell are closed, in that order. No statement completes.</summary>
    internal IReadOnlyList<ProtocolAction> End(bool abnormal)
    {
        if (_ended) return [];
        _ended = true;
        string exception = abnormal ? McsException.AbnormalTermination : McsException.NormalTermination;
        foreach (var ((requestor, serial), capacity) in _active)
            if (_inByRequestor.TryGetValue(requestor, out var link))
                _actions.Add(new Write(link.Id, new ReplyFrame(serial, MessageBody.Of(new string(' ', capacity)), exception)));
        _active.Clear();
        foreach (var id in _outByLink.Keys) _actions.Add(new CloseLink(id));
        foreach (var id in _inByLink.Keys) _actions.Add(new CloseLink(id));
        if (_served is not null) _actions.Add(new Unpublish(new PeerEndpoint(PeerEndpointKind.Server, McsServerName.Spell(_served), _runId)));
        if (_send is { BellPublished: true } send) _actions.Add(new Unpublish(Bell(send)));
        _outByLink.Clear();
        _outByRun.Clear();
        _inByLink.Clear();
        _inByRequestor.Clear();
        _offers.Clear();
        _send = null;
        _receive = null;
        return Flush();
    }

    // ---------------------------------------------------------------- events

    /// <summary>One event, and the actions it causes. An event for a link or statement the protocol no longer holds is dropped.</summary>
    internal IReadOnlyList<ProtocolAction> Handle(ProtocolEvent protocolEvent)
    {
        if (_ended) return [];
        switch (protocolEvent)
        {
            case FrameArrived f: OnFrame(f.Link, f.Frame); break;
            case LinkEnded e: OnLinkEnded(e.Link); break;
            case LinkOpened o: OnLinkOpened(o.Link); break;
            case BellRang: OnBell(); break;
            case DeadlinePassed d: OnDeadline(d.Token); break;
            case Enumerated e: OnEnumerated(e.RunIds); break;
            case Connected c: OnConnected(c.Endpoint, c.Result); break;
            case ActivationEnded a: OnActivationEnded(a); break;
            case ActionFailed f: OnActionFailed(f.Detail); break;
            default: throw new ArgumentException($"no handler for {protocolEvent.GetType().Name}", nameof(protocolEvent));
        }
        return Flush();
    }

    private void OnFrame(PeerLinkId link, PeerFrame frame)
    {
        if (_outByLink.TryGetValue(link, out var outLink)) OnRequestorFrame(outLink, frame);
        else if (_inByLink.TryGetValue(link, out var inLink)) OnServerFrame(inLink, frame);
    }

    private void OnLinkEnded(PeerLinkId link)
    {
        if (_outByLink.TryGetValue(link, out var outLink)) EndOut(outLink, closeIt: false);
        else if (_inByLink.TryGetValue(link, out var inLink)) EndIn(inLink, closeIt: false);
    }

    private void OnLinkOpened(PeerLinkId link)
    {
        if (_served is null)
        {
            _actions.Add(new CloseLink(link));     // nothing is published here: a connection that arrives is none of ours
            return;
        }
        _inByLink[link] = new InLink(link);
        _actions.Add(new Write(link, Hello(_served)));
        if (_send is { Phase: not SendPhase.Activating } send) Scan(send);     // design 5.4.1 step 7: "On BellRang or LinkOpened, repeat step 3"
    }

    private void OnBell()
    {
        if (_send is { Phase: not SendPhase.Activating } send) Scan(send);
    }

    private void OnDeadline(int token)
    {
        if (_receive is not { } receive || receive.Token != token) return;
        receive.DeadlinePassed = true;
        AdvanceReceive(receive);
    }

    private void OnEnumerated(IReadOnlyList<string> runIds)
    {
        if (ConsumeStale()) return;
        if (_send is not { } send) return;
        send.Outstanding--;
        foreach (string runId in runIds)
        {
            if (_outByRun.ContainsKey(runId) || _connecting.Contains(runId) || send.Burned.Contains(runId)) continue;
            _connecting.Add(runId);
            send.Outstanding++;
            _actions.Add(new Connect(new PeerEndpoint(PeerEndpointKind.Server, send.Spelling, runId)));
        }
        Quiesce(send);
    }

    private void OnConnected(PeerEndpoint endpoint, ConnectResult result)
    {
        _connecting.Remove(endpoint.RunId);
        if (ConsumeStale() || _send is not { } send)
        {
            if (result.Outcome == ConnectOutcome.Connected) _actions.Add(new CloseLink(result.Link));   // the statement that asked is over
            return;
        }
        send.Outstanding--;
        if (result.Outcome == ConnectOutcome.Connected)
        {
            var link = new OutLink(result.Link, endpoint.RunId, send.Name);
            _outByLink[link.Id] = link;
            _outByRun[link.RunId] = link;
            _actions.Add(new Write(link.Id, Hello(string.Empty)));
        }
        else send.Burned.Add(endpoint.RunId);                      // refused: its owner has ended, and the transport deleted the file
        Quiesce(send);
    }

    private void OnActivationEnded(ActivationEnded ended)
    {
        if (_send is not { Phase: SendPhase.Activating } send) return;
        if (!ended.Succeeded) FinishSend(send, StatementResult.Failed(McsException.Imp, ended.Detail));
        else
        {
            send.Phase = SendPhase.Scanning;
            Scan(send);                                            // design 5.4.5: the requestor enumerates again; none found is EC-MCS-NO-SERVER
        }
    }

    private void OnActionFailed(string detail)
    {
        if (ConsumeStale()) return;
        // The answer to one action failed: EC-MCS-IMP for the statement it served (§0 Q8); the other answers of that statement are stale.
        if (_send is { } send)
        {
            send.Outstanding--;
            FinishSend(send, StatementResult.Failed(McsException.Imp, detail));
        }
        else if (_receive is not null) Done(StatementResult.Failed(McsException.Imp, detail), flush: false);
    }

    /// <summary>The answers owed to a statement that has since ended arrive first, in the order their actions were issued; they are consumed, not applied.</summary>
    private bool ConsumeStale()
    {
        if (_staleAnswers == 0) return false;
        _staleAnswers--;
        return true;
    }

    // ---------------------------------------------------------------- requestor

    private PeerEndpoint Bell(SendState send) => new(PeerEndpointKind.Bell, send.Spelling, _runId);

    private HelloFrame Hello(string servedName) => new(PeerFrameCodec.ProtocolVersion, _mcs, _runId, _pid, servedName);

    /// <summary>design 5.4.1 step 3: enumerate the servers' listeners; each answer connects to the entries with no link.</summary>
    private void Scan(SendState send)
    {
        send.Outstanding++;
        _actions.Add(new Enumerate(send.Spelling));
    }

    private int LiveLinks(SendState send)
    {
        int live = 0;
        foreach (var link in _outByLink.Values)
            if (link.Name == send.Name) live++;
        return live;
    }

    /// <summary>Every scan answered: wait while a link is held; none held is step 4 (activate, or EC-MCS-NO-SERVER).</summary>
    private void Quiesce(SendState send)
    {
        if (send.Phase == SendPhase.Activating || send.Outstanding > 0) return;
        if (LiveLinks(send) > 0)
        {
            send.Phase = SendPhase.Waiting;
            return;
        }
        if (!send.Activated && _definitions(send.Name) is { HasStart: true })
        {
            send.Activated = true;
            send.Phase = SendPhase.Activating;
            _actions.Add(new Activate(send.Name));
            return;
        }
        FinishSend(send, StatementResult.Failed(McsException.NoServer));      // §14.9.38.4 GR5
    }

    /// <summary>Step 9: no link remains while the SEND waits, so it repeats step 3 once; the scan's answer decides.</summary>
    private void AfterLinkLoss(SendState send)
    {
        if (send.Phase == SendPhase.Waiting && send.Outstanding == 0 && LiveLinks(send) == 0)
        {
            send.Phase = SendPhase.Scanning;
            Scan(send);
        }
    }

    private void OfferToReadyLinks(SendState send)
    {
        if (send.Granted is not null) return;
        foreach (var link in _outByLink.Values)
            if (link.Name == send.Name && link.Phase == OutPhase.Ready) OfferTo(send, link);
    }

    private void OfferTo(SendState send, OutLink link)
    {
        link.Phase = OutPhase.Offered;
        _actions.Add(new Write(link.Id, new OfferFrame(send.Serial, send.Round)));
    }

    private void OnRequestorFrame(OutLink link, PeerFrame frame)
    {
        var send = _send;
        switch (frame)
        {
            case HelloFrame hello when link.Phase == OutPhase.AwaitHello:
                if (hello.Version != PeerFrameCodec.ProtocolVersion || hello.Mcs != _mcs || hello.RunId != link.RunId
                    || McsServerName.Normalize(hello.ServedName) != link.Name)
                {
                    EndOut(link, closeIt: true);          // a collided directory name, another MCS or version: not our server
                    break;
                }
                link.Phase = OutPhase.Ready;
                if (send is not null && send.Name == link.Name && send.Granted is null) OfferTo(send, link);
                break;
            case ClaimFrame claim when link.Phase != OutPhase.AwaitHello:
                // The round's first CLAIM is granted; every other offered link has a WITHDRAW, so a CLAIM that crossed it, or is of a past round, is
                // ignored: it is no violation, the server has not yet read the WITHDRAW.
                if (send is null || link.Phase != OutPhase.Offered || send.Granted is not null
                    || claim.Serial != send.Serial || claim.Round != send.Round) break;
                send.Granted = link;
                link.Phase = OutPhase.Granted;
                _actions.Add(new Write(link.Id, new GrantFrame(send.Serial, send.Round, send.Body)));
                WithdrawOffers(send);
                break;
            case AckFrame ack when link.Phase == OutPhase.Granted:
                if (send is null || send.Granted != link || ack.Serial != send.Serial) break;
                link.Phase = OutPhase.Ready;
                _sent[send.Serial] = link;
                FinishSend(send, StatementResult.Sent(new MessageTag(_mcs, MessageParty.Server, send.Name, send.Serial)));
                break;
            case ReplyFrame reply:
                if (!_sent.TryGetValue(reply.Serial, out var holder) || holder != link) break;     // a reply to nothing sent on this link is dropped
                _sent.Remove(reply.Serial);
                _replies[reply.Serial] = (reply.Message, reply.ExceptionName);
                if (_receive is { Kind: ReceiveKind.FromServer } receive && receive.Serial == reply.Serial) AdvanceReceive(receive);
                break;
            default:
                EndOut(link, closeIt: true);
                break;
        }
    }

    private void WithdrawOffers(SendState send)
    {
        foreach (var link in _outByLink.Values)
        {
            if (link.Phase != OutPhase.Offered) continue;
            link.Phase = OutPhase.Ready;
            _actions.Add(new Write(link.Id, new WithdrawFrame(send.Serial, send.Round)));
        }
    }

    /// <summary>A requestor's link is over. A granted link that ends before ACK starts a new round on the links that remain (design 5.4.1 step 9).</summary>
    private void EndOut(OutLink link, bool closeIt)
    {
        if (closeIt) _actions.Add(new CloseLink(link.Id));
        _outByLink.Remove(link.Id);
        _outByRun.Remove(link.RunId);
        foreach (var serial in _sent.Where(p => p.Value == link).Select(p => p.Key).ToArray())
        {
            _sent.Remove(serial);
            _replies[serial] = (null, null);                      // "server ended"
            if (_receive is { Kind: ReceiveKind.FromServer } receive && receive.Serial == serial) AdvanceReceive(receive);
        }
        if (_send is not { } send || send.Name != link.Name) return;
        if (closeIt) send.Burned.Add(link.RunId);
        if (send.Granted == link)
        {
            send.Granted = null;
            send.Round = ++_roundSequence;
            OfferToReadyLinks(send);
        }
        AfterLinkLoss(send);
    }

    /// <summary>Every completion of the SEND closes its bell and deletes <c>p/&lt;N&gt;/&lt;r&gt;</c> (design 5.4.1), and withdraws what is still offered.</summary>
    private void FinishSend(SendState send, StatementResult result)
    {
        if (!ReferenceEquals(_send, send)) return;
        WithdrawOffers(send);
        if (send.BellPublished) _actions.Add(new Unpublish(Bell(send)));
        _staleAnswers += send.Outstanding;
        _send = null;
        _actions.Add(new Complete(result));
    }

    // ---------------------------------------------------------------- server

    private void OnServerFrame(InLink link, PeerFrame frame)
    {
        switch (frame)
        {
            case HelloFrame hello when link.RequestorRunId is null:
                if (hello.Version != PeerFrameCodec.ProtocolVersion || hello.Mcs != _mcs || _inByRequestor.ContainsKey(hello.RunId))
                {
                    EndIn(link, closeIt: true);
                    break;
                }
                link.RequestorRunId = hello.RunId;
                _inByRequestor[hello.RunId] = link;
                break;
            case OfferFrame offer when link.RequestorRunId is not null:
                _offers.Add(new Offer(++_offerSequence, link, offer.Serial, offer.Round));
                if (_receive is { Kind: not ReceiveKind.FromServer } receive) AdvanceReceive(receive);
                break;
            case WithdrawFrame withdraw when link.RequestorRunId is not null:
                _offers.RemoveAll(o => o.Link == link && o.Serial == withdraw.Serial && o.Round == withdraw.Round);
                if (_receive is { Claim: { } claim } held && claim.Link == link && claim.Serial == withdraw.Serial && claim.Round == withdraw.Round)
                {
                    held.Claim = null;
                    AdvanceReceive(held);
                }
                break;
            case GrantFrame grant when link.RequestorRunId is not null:
                if (_receive is { Claim: { } granted } taking && granted.Link == link && granted.Serial == grant.Serial && granted.Round == grant.Round)
                    Take(taking, granted, grant.Message);
                break;
            default:
                EndIn(link, closeIt: true);
                break;
        }
    }

    /// <summary>A GRANT answers the claim in flight: write ACK, record the message in <c>Active</c> with identifier-1's maximum length, and only then complete
    /// with the requestor tag (design 5.4.2). A message longer than identifier-1 is taken and answered EC-MCS-MESSAGE-LENGTH (§0 Q7).</summary>
    private void Take(ReceiveState receive, Offer offer, MessageBody message)
    {
        _offers.Remove(offer);
        receive.Claim = null;
        string requestor = offer.Link.RequestorRunId!;
        _actions.Add(new Write(offer.Link.Id, new AckFrame(offer.Serial)));
        _active[(requestor, offer.Serial)] = receive.Capacity;
        var tag = new MessageTag(_mcs, MessageParty.Requestor, requestor, offer.Serial);
        bool tooLong = message.Image.Length > receive.Capacity;
        var body = message.Truncated(receive.Capacity);
        CompleteReceive(receive, tooLong
            ? new StatementResult(McsException.MessageLength, tag, body.Image, body.Extents, body.Slots, null)
            : new StatementResult(null, tag, body.Image, body.Extents, body.Slots, body.Image.Length));
    }

    private void EndIn(InLink link, bool closeIt)
    {
        if (closeIt) _actions.Add(new CloseLink(link.Id));
        _inByLink.Remove(link.Id);
        if (link.RequestorRunId is { } requestor && _inByRequestor.TryGetValue(requestor, out var held) && held == link) _inByRequestor.Remove(requestor);
        _offers.RemoveAll(o => o.Link == link);
        if (_receive is not { } receive || receive.Kind == ReceiveKind.FromServer) return;
        if (receive.Claim?.Link == link) receive.Claim = null;
        if (receive.Kind == ReceiveKind.FromRequestor && receive.RequestorRunId == link.RequestorRunId)
            CompleteReceive(receive, StatementResult.Failed(McsException.RequestorFailed));
        else AdvanceReceive(receive);
    }

    // ---------------------------------------------------------------- receive

    /// <summary>Moves the current RECEIVE as far as the state allows: claim the oldest eligible offer; deliver a held reply; or, once the deadline has
    /// passed with nothing in flight, complete empty (design 5.4.4: at the deadline the holder of the message decides, and a server with a CLAIM in flight waits).</summary>
    private void AdvanceReceive(ReceiveState receive)
    {
        if (!ReferenceEquals(_receive, receive)) return;
        if (receive.Kind == ReceiveKind.FromServer)
        {
            if (_replies.Remove(receive.Serial, out var reply)) DeliverReply(receive, reply);
            else if (receive.DeadlinePassed) CompleteReceive(receive, StatementResult.TimedOut());
            return;
        }
        if (receive.Claim is null)
        {
            Offer? next = null;
            foreach (var offer in _offers)
            {
                if (receive.Kind == ReceiveKind.FromRequestor && offer.Link.RequestorRunId != receive.RequestorRunId) continue;
                if (next is null || offer.Sequence < next.Sequence) next = offer;
            }
            if (next is not null)
            {
                receive.Claim = next;
                _actions.Add(new Write(next.Link.Id, new ClaimFrame(next.Serial, next.Round)));
            }
        }
        if (receive.Claim is null && receive.DeadlinePassed) CompleteReceive(receive, StatementResult.TimedOut());
    }

    /// <summary>§0 Q6: the reply moves into identifier-1 and its length into data-name-2, then the RECEIVE is unsuccessful with the exception the server
    /// raised, or with EC-MCS-ABNORMAL-TERMINATION when the server ended with no reply (design 5.4.3).</summary>
    private void DeliverReply(ReceiveState receive, (MessageBody? Body, string? Exception) reply)
    {
        if (reply.Body is null)
        {
            CompleteReceive(receive, StatementResult.Failed(McsException.AbnormalTermination));
            return;
        }
        bool tooLong = reply.Body.Image.Length > receive.Capacity;
        var body = reply.Body.Truncated(receive.Capacity);
        string? exception = reply.Exception ?? (tooLong ? McsException.MessageLength : null);
        CompleteReceive(receive, new StatementResult(exception, default, body.Image, body.Extents, body.Slots, tooLong ? null : body.Image.Length));
    }

    private void CompleteReceive(ReceiveState receive, StatementResult result)
    {
        if (!ReferenceEquals(_receive, receive)) return;
        _receive = null;
        _actions.Add(new Complete(result));
    }

    // ---------------------------------------------------------------- plumbing

    private void EnsureIdle()
    {
        if (_ended) throw new InvalidOperationException("the run unit has ended its messaging");
        if (Busy) throw new InvalidOperationException("a messaging statement is already current");
    }

    private IReadOnlyList<ProtocolAction> Done(StatementResult result, bool flush = true)
    {
        _receive = null;
        _actions.Add(new Complete(result));
        return flush ? Flush() : [];
    }

    private IReadOnlyList<ProtocolAction> Flush()
    {
        var actions = _actions.ToArray();
        _actions.Clear();
        return actions;
    }
}
