// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Net.Sockets;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Exceptions;
using WiseOwl.Cobol.Runtime.Mcs;
using WiseOwl.Cobol.Runtime.Messaging;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE MESSAGING PEER OVER REAL SOCKETS (kb/Work PB4742; docs/rearchitecture/DESIGN-message-control.md §5.4 to §5.6, §7.2 "In-process", §9 S2b): each
/// <see cref="MessagingPeer"/> is one run unit with its own <see cref="MessagingSettings"/> against one fresh MCS directory, a requestor on the test's thread
/// and a server on another, ordered by the protocol itself, so nothing sleeps. The kill is <see cref="MessagingPeer.Abort"/>. A <see cref="ManualTime"/> supplies
/// the CONTINUE AFTER deadline, so a deadline passes when the test says so.
/// </summary>
[Trait("Host", "ReFS")]
public sealed class MessagingPeerTests : IDisposable
{
    /// <summary>Only ever the failure of a hang, and one harness unit (kb/Work PB3108): the transport's accept loops and pumps run on the thread pool, so a fixed limit measures the host
    /// (gate run 000003-20261107T040901Z, on a host also running a Linux gate: 61 s and 91 s, red, for cases that take a fraction of a second, the first two of the class to use a pump).</summary>
    private static readonly TimeSpan Guard = TimeSpan.FromMilliseconds(ObservationBudget.UnitMs);

    private readonly string _work = ScratchDirectory.Create("pb4742p-");
    private readonly List<MessagingPeer> _peers = [];

    public void Dispose()
    {
        foreach (var peer in _peers) peer.End(abnormal: false);
        ScratchDirectory.Remove(McsSocketDirectory.Resolve(McsDirectory.Resolve(_work).Identity).Path);
        ScratchDirectory.Remove(_work);
    }

    private MessagingPeer Peer(string? served, TimeProvider? time = null, string? notify = null, Func<string, McsSocketDirectory>? sockets = null)
    {
        var settings = new MessagingSettings { Directory = _work, ServedName = served, NotifyPath = notify, Time = time ?? TimeProvider.System };
        var peer = new MessagingPeer(() => settings, sockets ?? McsSocketDirectory.Resolve);
        _peers.Add(peer);
        peer.StartServing();
        return peer;
    }

    /// <summary>Runs <paramref name="statement"/> on its own thread, never one of the pool's, because a statement blocks and the pumps it waits for need the pool;
    /// the task's result is the statement's, and a hang is the failure of the guard.</summary>
    private static Task<StatementResult> OnThread(Func<StatementResult> statement) =>
        Task.Factory.StartNew(statement, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).WaitAsync(Guard);

    private static StatementResult Result(Task<StatementResult> task) => task.GetAwaiter().GetResult();

    /// <summary><c>&lt;S&gt;</c>. A socket a test binds or names lives here, as <c>n/&lt;r&gt;</c> does (design 5.1), because the budget covers <c>&lt;S&gt;</c>:
    /// the test's scratch directory is deep under the temp directory, and how deep depends on the host's user name and process id.</summary>
    private string SocketDirectory => McsSocketDirectory.Resolve(McsDirectory.Resolve(_work).Identity).Path;

    private string SocketFile(string kind, string spelling, string runId) => Path.Combine(SocketDirectory, kind, spelling, runId);

    [Fact]
    public void ARequestAndItsReplyCrossTheSocketsAndTheTagsRouteThem()
    {
        var server = Peer("SRV");
        var requestor = Peer(null);
        var served = OnThread(() => server.Receive(default, 100, MessageWait.Forever));

        var sent = requestor.Send("srv  ", MessageBody.Of("PING"));
        Assert.True(sent.Successful);
        Assert.Equal(MessageParty.Server, sent.Tag.Party);

        var received = Result(served);
        Assert.True(received.Successful);
        Assert.Equal("PING", received.Image);
        Assert.Equal(MessageParty.Requestor, received.Tag.Party);

        var replied = server.SendResponse(received.Tag, MessageBody.Of("PONG"), null);
        Assert.True(replied.Successful);
        var reply = requestor.Receive(sent.Tag, 100, MessageWait.Forever);
        Assert.True(reply.Successful);
        Assert.Equal("PONG", reply.Image);
    }

    [Fact]
    public void TwoRequestorsAndOneServerEachGetTheirOwnReply()
    {
        var server = Peer("TWO");
        var first = Peer(null);
        var second = Peer(null);
        var firstSent = OnThread(() => first.Send("TWO", MessageBody.Of("ONE")));
        var secondSent = OnThread(() => second.Send("TWO", MessageBody.Of("TWO")));
        var takenA = server.Receive(default, 100, MessageWait.Forever);
        var takenB = server.Receive(default, 100, MessageWait.Forever);
        Assert.Equal(["ONE", "TWO"], new[] { takenA.Image, takenB.Image }.Order());
        foreach (var taken in new[] { takenA, takenB })
            Assert.True(server.SendResponse(taken.Tag, MessageBody.Of("RE-" + taken.Image), null).Successful);
        Assert.Equal("RE-ONE", first.Receive(Result(firstSent).Tag, 100, MessageWait.Forever).Image);
        Assert.Equal("RE-TWO", second.Receive(Result(secondSent).Tag, 100, MessageWait.Forever).Image);
    }

    [Fact]
    public void ASendToANameNoRunUnitServesIsNoServerAndLeavesNoBellBehind()
    {
        var requestor = Peer(null);
        var result = requestor.Send("NOBODY", MessageBody.Of("HELLO"));
        Assert.Equal(ExceptionCatalog.McsNoServer, result.ExceptionName);
        Assert.False(File.Exists(SocketFile("p", "NOBODY", requestor.RunId!)));       // design 5.4.1: every completion of the SEND deletes its bell
    }

    [Fact]
    public void AServerAbortedBeforeASendIsRefusedAndItsFileDeleted()
    {
        var server = Peer("DEAD");
        string runId = server.RunId!;
        server.Abort();
        Assert.True(File.Exists(SocketFile("s", "DEAD", runId)));                     // a kill leaves its listener's file
        var requestor = Peer(null);
        Assert.Equal(ExceptionCatalog.McsNoServer, requestor.Send("DEAD", MessageBody.Of("X")).ExceptionName);
        Assert.False(File.Exists(SocketFile("s", "DEAD", runId)));                    // the refusal rule deleted it
    }

    [Fact]
    public void AReceiveWithAPassedDeadlineIsSuccessfulWithSpacesAndTheDeadlineRunsOnTheSettingsTimeNotTheClock()
    {
        var time = new ManualTime();
        var server = Peer("WAIT", time);
        var waiting = OnThread(() => server.Receive(default, 100, new MessageWait(MessageWaitKind.Seconds, 5)));
        Assert.True(time.Armed.Wait(Guard), "the deadline was never armed");
        Assert.Equal(TimeSpan.FromSeconds(5), time.Due);
        Assert.False(waiting.IsCompleted);                                            // nothing has passed yet
        time.Advance();
        var result = Result(waiting);
        Assert.True(result.Successful);
        Assert.Equal("", result.Image);
        Assert.Equal(0, result.Length);
    }

    [Fact]
    public void APollAsksOnceAndWaitsForNothing()
    {
        var server = Peer("POLL");
        var result = server.Receive(default, 100, MessageWait.Poll);
        Assert.True(result.Successful);
        Assert.Equal(0, result.Length);
    }

    [Fact]
    public void AReceiveFromNullInARunUnitThatServesNothingIsNoRequesterWithoutTouchingTheSocketDirectory()
    {
        var requestor = Peer(null);
        Assert.Equal(ExceptionCatalog.McsNoRequester, requestor.Receive(default, 100, MessageWait.Forever).ExceptionName);
    }

    [Theory]
    [InlineData(false, ExceptionCatalog.McsNormalTermination)]
    [InlineData(true, ExceptionCatalog.McsAbnormalTermination)]
    public void ARunUnitThatEndsWithAnActiveMessageAnswersItWithTheTerminationException(bool abnormal, string expected)
    {
        var server = Peer("ENDS");
        var requestor = Peer(null);
        var served = OnThread(() => server.Receive(default, 40, MessageWait.Forever));
        var sent = requestor.Send("ENDS", MessageBody.Of("Q"));
        Assert.True(Result(served).Successful);
        server.End(abnormal);                                                         // design 5.6: ISO §14.6.11 item 7, §14.6.12
        var reply = requestor.Receive(sent.Tag, 100, MessageWait.Forever);
        Assert.Equal(expected, reply.ExceptionName);
        Assert.Equal(new string(' ', 40), reply.Image);                               // spaces of the size the RECEIVE recorded
    }

    [Fact]
    public void AServerKilledWithAnActiveMessageIsAbnormalTerminationAtTheRequestor()
    {
        var server = Peer("KILLED");
        var requestor = Peer(null);
        var served = OnThread(() => server.Receive(default, 100, MessageWait.Forever));
        var sent = requestor.Send("KILLED", MessageBody.Of("Q"));
        Assert.True(Result(served).Successful);
        server.Abort();
        Assert.Equal(ExceptionCatalog.McsAbnormalTermination, requestor.Receive(sent.Tag, 100, MessageWait.Forever).ExceptionName);   // design 5.4.3: a link that ended with no reply
    }

    [Fact]
    public void EndDeletesTheListenerAndTheBellFilesAndIsIdempotent()
    {
        var server = Peer("TIDY");
        string runId = server.RunId!;
        Assert.True(File.Exists(SocketFile("s", "TIDY", runId)));
        server.End(abnormal: false);
        server.End(abnormal: true);
        Assert.False(File.Exists(SocketFile("s", "TIDY", runId)));
        Assert.Throws<ObjectDisposedException>(() => server.Receive(default, 1, MessageWait.Poll));
    }

    [Fact]
    public void APeerThatNeverMessagedStartsNothingAndEndsQuietly()
    {
        var peer = Peer(null);
        Assert.Null(peer.RunId);
        Assert.Null(peer.StartFailure);
        peer.End(abnormal: false);
    }

    [Fact]
    public void AServerStartedByARequestorConnectsToItsNotifySocketOncePublished()
    {
        string notify = Path.Combine(SocketDirectory, "n-test");
        using var listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        listener.Bind(new UnixDomainSocketEndPoint(notify));
        listener.Listen(1);
        var server = Peer("NOTE", notify: notify);
        Assert.Null(server.StartFailure);                                             // first: a start that failed connected to nothing, and the Accept would wait for ever
        using var accepted = listener.Accept();
        Assert.True(File.Exists(SocketFile("s", "NOTE", server.RunId!)));             // the listener was published before the notification
    }

    [Fact]
    public void ANotifySocketThatRefusesIsAStartFailureAndEveryStatementIsImpWithTheReason()
    {
        var server = Peer("NOTIFAIL", notify: Path.Combine(SocketDirectory, "n-none"));
        Assert.Contains("refused", server.StartFailure);                              // the refusal itself, not a path the socket could not hold
        var result = server.Receive(default, 1, MessageWait.Poll);
        Assert.Equal(ExceptionCatalog.McsImp, result.ExceptionName);
        Assert.Equal(server.StartFailure, result.Detail);
        Assert.False(File.Exists(SocketFile("s", "NOTIFAIL", server.RunId ?? "")));   // the half-started listener was unpublished
        Assert.Equal(ExceptionCatalog.McsImp, server.Send("ANY", MessageBody.Of("X")).ExceptionName);
    }

    [Fact]
    public void ANotifyPathOverTheAfUnixLimitIsAStartFailureWithItsReasonNeverAnException()
    {
        string tooLong = Path.Combine(SocketDirectory, new string('n', McsSocketDirectory.MaxPathBytes));      // over the limit on every host, whatever <S> is
        var server = Peer("NOTELONG", notify: tooLong);
        Assert.Contains(tooLong, server.StartFailure);
        Assert.Contains(McsSocketDirectory.MaxPathBytes.ToString(System.Globalization.CultureInfo.InvariantCulture), server.StartFailure);
        var result = server.Receive(default, 1, MessageWait.Poll);
        Assert.Equal(ExceptionCatalog.McsImp, result.ExceptionName);
        Assert.Equal(server.StartFailure, result.Detail);
        Assert.Empty(Directory.EnumerateFileSystemEntries(Path.GetDirectoryName(SocketFile("s", "NOTELONG", "x"))!));   // the half-started listener was unpublished
    }

    [Fact]
    public void ASocketDirectoryWithNoRoomOnALongTempRootIsAStartFailureAndCreatesNothing()
    {
        string longTemp = Path.Combine(_work, new string('t', McsSocketDirectory.MaxPathBytes)) + Path.DirectorySeparatorChar;     // a host whose temp path is long
        foreach (bool serves in new[] { true, false })
        {
            var peer = Peer(serves ? "LONGROOT" : null, sockets: identity => McsSocketDirectory.Under(McsSocketDirectory.WindowsDirectory(longTemp, identity)));
            var result = peer.Send("ANY", MessageBody.Of("X"));                       // a run unit that serves nothing starts at its first statement
            Assert.Equal(ExceptionCatalog.McsImp, result.ExceptionName);
            Assert.Contains("leaves no room", result.Detail);
            Assert.Contains(longTemp, result.Detail);
            Assert.Equal(peer.StartFailure, result.Detail);
            Assert.Null(peer.RunId);
        }
        Assert.False(Directory.Exists(longTemp));
    }

    [Fact]
    public void APublishThatFailsTellsTheNotifySocketNothingAndTheReceiveThatNeedsARequestorIsImp()
    {
        string notify = Path.Combine(SocketDirectory, "n-test");
        using var listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        listener.Bind(new UnixDomainSocketEndPoint(notify));
        listener.Listen(1);
        File.WriteAllText(Path.Combine(SocketDirectory, "t"), "");                    // a file where Publish needs its directory t/: every Publish is refused
        var server = Peer("NOPUB", notify: notify);
        Assert.Null(server.StartFailure);                                             // design 5.4: a Publish owes no answer; the listener never published
        Assert.False(listener.Poll(0, SelectMode.SelectRead));                        // Start has returned, so a notification would already be pending
        var result = server.Receive(default, 1, MessageWait.Poll);
        Assert.Equal(ExceptionCatalog.McsImp, result.ExceptionName);
        Assert.False(File.Exists(SocketFile("s", "NOPUB", server.RunId!)));
    }

    [Fact]
    public void ARunUnitThatServesANamePublishesWhenItBeginsAndTerminateEndsThePeer()
    {
        string runId = "";
        string file = "";
        RunUnit.Run(ru =>
        {
            ru.MessagingSettings = new MessagingSettings { Directory = _work, ServedName = "RUNUNIT" };
            ru.Messaging.StartServing();
            runId = ru.Messaging.RunId!;
            file = SocketFile("s", "RUNUNIT", runId);
            Assert.True(File.Exists(file));
        });
        Assert.False(File.Exists(file));                                              // Terminate called End (design 5.6)
    }

    [Fact]
    public void TheNextRunUnitInheritsTheSettingsAndStartsServingWhenItBegins()
    {
        string file = "";
        RunUnit.Run(ru =>
        {
            var settings = new MessagingSettings { Directory = _work, ServedName = "INHERIT" };
            ru.MessagingSettings = settings;
            var next = RunUnit.Begin();                                               // the emitted driver's run-unit start
            Assert.Same(settings, next.MessagingSettings);
            Assert.NotNull(next.Messaging.RunId);                                     // design 5.5: serving from its beginning
            file = SocketFile("s", "INHERIT", next.Messaging.RunId!);
            Assert.True(File.Exists(file));
            Assert.Null(ru.Messaging.RunId);                                          // the run unit it replaced started nothing
        });
        Assert.False(File.Exists(file));
    }

    [Fact]
    public void TheAmbientReplacementOfAHostEndedRunUnitPublishesNothing()
    {
        string file = "";
        RunUnit.Run(ru =>
        {
            ru.MessagingSettings = new MessagingSettings { Directory = _work, ServedName = "HOSTEND" };
            ru.Messaging.StartServing();
            file = SocketFile("s", "HOSTEND", ru.Messaging.RunId!);
            Assert.True(File.Exists(file));
            ru.EndForHost();                                                          // a STOP RUN reached the host: the serving run unit ended (14.9.38.4 GR5)
            Assert.False(File.Exists(file));
            Assert.Null(RunUnit.Current.Messaging.RunId);                             // no program runs in the replacement, so it serves nothing
        });
    }

    [Fact]
    public void ARunUnitThatSendsNothingCreatesNoSocketDirectory()
    {
        string socketDirectory = McsSocketDirectory.Resolve(McsDirectory.Resolve(_work).Identity).Path;
        ScratchDirectory.Remove(socketDirectory);
        RunUnit.Run(ru => ru.MessagingSettings = new MessagingSettings { Directory = _work, ServedName = null });
        Assert.False(Directory.Exists(socketDirectory));
    }

    /// <summary>A <see cref="TimeProvider"/> whose one timer fires when the test advances it: the deadline's due time is recorded, and nothing sleeps.</summary>
    private sealed class ManualTime : TimeProvider
    {
        private TimerCallback? _callback;
        private object? _state;

        internal ManualResetEventSlim Armed { get; } = new(false);
        internal TimeSpan Due { get; private set; }

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            _callback = callback;
            _state = state;
            Due = dueTime;
            Armed.Set();
            return new Handle();
        }

        internal void Advance() => _callback!(_state);

        private sealed class Handle : ITimer
        {
            public bool Change(TimeSpan dueTime, TimeSpan period) => true;

            public void Dispose()
            {
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
