// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Concurrent;
using WiseOwl.Cobol.Runtime.Exceptions;
using WiseOwl.Cobol.Runtime.Mcs;

namespace WiseOwl.Cobol.Runtime.Messaging;

/// <summary>
/// ⛔ A RUN UNIT'S MESSAGING PEER: THE ONE EXECUTOR OF <see cref="MessagingProtocol"/>'S ACTIONS (docs/rearchitecture/DESIGN-message-control.md §5.4, §5.5, §5.6,
/// §9 S2b). It owns the protocol, the <see cref="SocketTransport"/> and the deadline timers of one <see cref="RunUnit"/> and runs every statement to its
/// completion on the run unit's thread: it starts the statement, then takes transport events (decoded, in the order they happened), the answers to its own
/// actions and then the deadlines that have passed, until the protocol completes the statement. The protocol stays single-threaded; the pumps behind the
/// transport only queue events and signal <see cref="_wake"/>.
/// <para><b>Lazy.</b> <see cref="RunUnit.Messaging"/> is a cheap shell: the socket directory, the transport and the protocol exist from the run unit's first
/// messaging statement, or from <see cref="StartServing"/> when the run unit serves a name (design 5.5). A directory the host refuses, a path too long for an
/// AF_UNIX socket or a notify socket that refuses is a start failure: every statement is then EC-MCS-IMP with the reason (§0 Q8, design 5.1), never an exception.</para>
/// <para><b>Answers.</b> An action's answer is queued in the order the actions were issued, and an action that fails answers with <see cref="ActionFailed"/> in
/// place of its own answer (design 5.4); a <see cref="Write"/> to a link that ended answers nothing, because the link's own <see cref="LinkEnded"/> follows.</para>
/// <para><b>Deadlines.</b> <see cref="ArmDeadline"/> arms a timer on <see cref="MessagingSettings.Time"/>, never on the run unit's clock. The timer only queues its
/// token, and the token is taken after every frame already held, so a reply that arrived before the deadline is delivered (design 5.4.4).</para>
/// </summary>
internal sealed class MessagingPeer
{
    private readonly Func<MessagingSettings> _settings;

    // Created by Start, so a run unit that never messages pays for none of them.
    private ManualResetEventSlim? _wake;
    private Queue<ProtocolEvent> _answers = null!;
    private ConcurrentQueue<int> _deadlines = null!;
    private Dictionary<int, ITimer> _timers = null!;
    private SocketTransport? _transport;
    private MessagingProtocol? _protocol;
    private string? _failure;
    private bool _started, _ended;
    private StatementResult? _result;

    /// <summary>A peer whose settings are read when it starts: <paramref name="settings"/> is the run unit's, which a host may replace until the first statement.</summary>
    internal MessagingPeer(Func<MessagingSettings> settings) => _settings = settings;

    /// <summary>The run id <c>&lt;r&gt;</c> of this run unit, once it has started (null before, and when the start failed).</summary>
    internal string? RunId { get; private set; }

    /// <summary>Why the start failed, or null.</summary>
    internal string? StartFailure => _failure;

    /// <summary>Starts the peer if this run unit serves a name (design 5.5: "or when it begins if <c>MessagingSettings.ServedName</c> is set"). A run unit that serves none starts nothing.</summary>
    internal void StartServing()
    {
        if (_settings().ServedName is not null) Start();
    }

    // ---------------------------------------------------------------- statements

    /// <summary>SEND Format 1 (design 5.4.1).</summary>
    internal StatementResult Send(string server, MessageBody message) =>
        Run(protocol => protocol.BeginSend(server, message));

    /// <summary>SEND Format 2 (design 5.4.2).</summary>
    internal StatementResult SendResponse(MessageTag to, MessageBody message, string? raising) =>
        Run(protocol => protocol.BeginSendResponse(to, message, raising));

    /// <summary>RECEIVE (design 5.4.2, 5.4.3): blocks until the protocol completes it.</summary>
    internal StatementResult Receive(MessageTag from, int capacity, MessageWait wait) =>
        Run(protocol => protocol.BeginReceive(from, capacity, wait));

    /// <summary>The run unit ends (design 5.6): the protocol answers each active message and closes its links; then the transport unpublishes its files. Idempotent, and a peer that
    /// never started has nothing to end. It never throws: the run unit's end must not hang on a socket.</summary>
    internal void End(bool abnormal)
    {
        if (_ended) return;
        _ended = true;
        try
        {
            if (_protocol is not null && _transport is not null)
            {
                Execute(_protocol.End(abnormal));
                _answers.Clear();
            }
        }
        finally
        {
            CancelTimers();
            _transport?.Dispose();
            _wake?.Dispose();
        }
    }

    /// <summary>The kill, for a test (design 7.2): every socket is dropped as the OS drops a killed process's and the published files are left behind. The peer takes no further statement.</summary>
    internal void Abort()
    {
        if (_ended) return;
        _ended = true;
        CancelTimers();
        _transport?.Abort();
        SignalWake();                                    // a statement blocked on another thread's kill stops waiting and sees the end
    }

    // ---------------------------------------------------------------- the loop

    private StatementResult Run(Func<MessagingProtocol, IReadOnlyList<ProtocolAction>> begin)
    {
        if (_ended) throw new ObjectDisposedException(nameof(MessagingPeer), "the run unit's messaging has ended");
        Start();
        if (_failure is not null) return StatementResult.Failed(ExceptionCatalog.McsImp, _failure);
        var protocol = _protocol!;
        _result = null;
        Pump(until: false);                              // what arrived between statements is known before this one begins
        Execute(begin(protocol));
        while (_result is null)
        {
            if (_ended) throw new ObjectDisposedException(nameof(MessagingPeer), "the run unit was killed");
            _wake!.Reset();                              // an event queued after this reset sets the wake again, so none is missed
            if (!Pump(until: true)) _wake.Wait();
        }
        var result = _result;
        _result = null;
        CancelTimers();
        return result;
    }

    /// <summary>Takes everything that is ready, in order: the answers owed to earlier actions, then transport events, then deadlines. True when anything was taken.
    /// With <paramref name="until"/> the pass ends as soon as the current statement has completed.</summary>
    private bool Pump(bool until)
    {
        bool any = false;
        while (!(until && _result is not null))
        {
            ProtocolEvent next;
            if (_answers.TryDequeue(out var answer)) next = answer;
            else if (_transport!.TryTake(out var transportEvent)) next = Decode(transportEvent);
            else if (_deadlines.TryDequeue(out int token)) next = new DeadlinePassed(token);
            else break;
            any = true;
            Execute(_protocol!.Handle(next));
        }
        return any;
    }

    /// <summary>The protocol's view of a transport event; a frame that does not decode closes its link here and reaches the protocol as that link's end.</summary>
    private ProtocolEvent Decode(TransportEvent transportEvent) => Decode(transportEvent, _transport!);

    /// <summary>The one mapping from a <see cref="TransportEvent"/> to the <see cref="ProtocolEvent"/> the protocol sees, over any <see cref="IPeerTransport"/>.</summary>
    internal static ProtocolEvent Decode(TransportEvent transportEvent, IPeerTransport transport)
    {
        switch (transportEvent)
        {
            case FrameReceived f:
                try { return new FrameArrived(f.Link, PeerFrameCodec.Decode(f.Kind, f.Payload)); }
                catch (InvalidDataException)
                {
                    transport.Close(f.Link);                   // design 6: a reader that meets a frame it cannot read closes the link
                    return new LinkEnded(f.Link);
                }
            case LinkLost l: return new LinkEnded(l.Link);
            case LinkAccepted a: return new LinkOpened(a.Link);
            case BellRung: return new BellRang();
            default: throw new InvalidOperationException($"no protocol event for {transportEvent.GetType().Name}");
        }
    }

    /// <summary>Runs <paramref name="actions"/> in order on the transport. Each answer, or the <see cref="ActionFailed"/> that replaces it, is queued in that order.</summary>
    private void Execute(IReadOnlyList<ProtocolAction> actions)
    {
        var transport = _transport!;
        foreach (var action in actions)
        {
            try
            {
                switch (action)
                {
                    case Write w: transport.Write(w.Link, w.Frame.Kind, PeerFrameCodec.Encode(w.Frame)); break;
                    case CloseLink c: transport.Close(c.Link); break;
                    case Publish p: transport.Publish(p.Endpoint); break;
                    case Unpublish u: transport.Unpublish(u.Endpoint); break;
                    case Enumerate e: _answers.Enqueue(new Enumerated(transport.EnumerateServers(e.Spelling))); break;
                    case Connect c: _answers.Enqueue(new Connected(c.Endpoint, transport.Connect(c.Endpoint))); break;
                    case RingBells r: transport.RingBells(r.Spelling); break;
                    case ArmDeadline d: Arm(d); break;
                    case Activate: throw new IOException("a server cannot be started from this run unit yet");   // slice S5 (PB4745): no definition has a start command until then
                    case Complete c: _result = c.Result; break;
                    default: throw new InvalidOperationException($"no executor for {action.GetType().Name}");
                }
            }
            catch (Exception e) when (e is IOException or System.Net.Sockets.SocketException or UnauthorizedAccessException)
            {
                _answers.Enqueue(new ActionFailed(action, e.Message));
            }
        }
    }

    private void Arm(ArmDeadline deadline)
    {
        // A timer waits at most uint.MaxValue - 1 milliseconds (about 49 days), so a longer CONTINUE AFTER is armed for that long.
        TimeSpan due = deadline.Seconds >= MaxSeconds ? TimeSpan.FromSeconds(MaxSeconds) : TimeSpan.FromSeconds(Math.Max(deadline.Seconds, 0));
        int token = deadline.Token;
        _timers[token] = _settings().Time.CreateTimer(_ => { _deadlines.Enqueue(token); SignalWake(); }, null, due, Timeout.InfiniteTimeSpan);
    }

    private const long MaxSeconds = (uint.MaxValue - 1L) / 1000;

    private void SignalWake()
    {
        try { _wake?.Set(); }
        catch (ObjectDisposedException) { /* the run unit ended while a pump or a timer signalled */ }
    }

    private void CancelTimers()
    {
        if (_timers is null) return;
        foreach (var timer in _timers.Values) timer.Dispose();
        _timers.Clear();
        _deadlines.Clear();
    }

    // ---------------------------------------------------------------- start

    private void Start()
    {
        if (_started) return;
        _started = true;
        _wake = new ManualResetEventSlim(false);
        _answers = new Queue<ProtocolEvent>();
        _deadlines = new ConcurrentQueue<int>();
        _timers = [];
        var settings = _settings();
        try
        {
            var directory = McsDirectory.Resolve(settings.Directory);
            string identity = directory.Identity;
            var sockets = McsSocketDirectory.Resolve(identity);
            if (!sockets.Fits(McsSocketDirectory.LongestEntryBytes))
                throw new IOException($"the socket directory '{sockets.Path}' leaves no room for a socket entry: an AF_UNIX path holds at most {McsSocketDirectory.MaxPathBytes} bytes, and the longest entry adds {McsSocketDirectory.LongestEntryBytes}");
            string runId = McsSocketDirectory.NewRunId();
            _transport = new SocketTransport(sockets, runId, SignalWake);
            _protocol = new MessagingProtocol(identity, runId, Environment.ProcessId, settings.ServedName, settings.Definitions);
            RunId = runId;
            if (settings.ServedName is not null)
            {
                Execute(_protocol.Serve());                // publish the listener, then ring the pending bells (design 5.4.1)
                if (settings.NotifyPath is { } notify) _transport.Notify(notify);   // design 5.4.5: the requestor that started this server learns its listener is published
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            _failure = e.Message;
            _transport?.Dispose();
            _transport = null;
            _protocol = null;
        }
    }
}
