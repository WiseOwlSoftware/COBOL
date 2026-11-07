// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Net.Sockets;
using WiseOwl.Cobol.Runtime.Mcs;
using WiseOwl.Cobol.Runtime.Messaging;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ <see cref="SocketTransport"/> OVER REAL AF_UNIX SOCKETS (kb/Work PB4742; docs/rearchitecture/DESIGN-message-control.md §5.1, §5.4.1, §5.5, §9 S2b): each transport
/// stands for one run unit with its own run id under one socket directory of a fresh MCS identity. Nothing sleeps: a test waits on the signal the transport's pumps
/// raise, and the wait's limit only guards a hang. The protocol over these transports is <see cref="MessagingProtocolTests"/>' subject (over <see cref="InMemoryTransport"/>);
/// this class holds the medium to the same <see cref="IPeerTransport"/> contract, plus what only a socket has: the publication window, a kill, a refused connect.
/// </summary>
public sealed class SocketTransportTests : IDisposable
{
    /// <summary>Only ever the failure of a hang, and one harness unit (kb/Work PB3108): the transport's accept loops and pumps run on the thread pool, which a host busy with the whole
    /// population can hold back for a minute and more, so a fixed limit measures the host (gate run 000003-20261107T040901Z: 61 s and 91 s, red, for cases that take a fraction of a second).</summary>
    private static readonly TimeSpan Guard = TimeSpan.FromMilliseconds(ObservationBudget.UnitMs);

    private readonly string _work = ScratchDirectory.Create("pb4742-");
    private readonly McsSocketDirectory _sockets;
    private readonly List<(SocketTransport Transport, ManualResetEventSlim Wake)> _made = [];

    public SocketTransportTests()
    {
        string identity = McsDirectory.Resolve(_work).Identity;
        _sockets = McsSocketDirectory.Resolve(identity);
    }

    public void Dispose()
    {
        foreach (var (transport, wake) in _made)
        {
            transport.Dispose();
            wake.Dispose();
        }
        ScratchDirectory.Remove(_sockets.Path);
        ScratchDirectory.Remove(_work);
    }

    private (SocketTransport Transport, ManualResetEventSlim Wake, string RunId) Run(string? runId = null)
    {
        var wake = new ManualResetEventSlim(false);
        runId ??= McsSocketDirectory.NewRunId();
        var transport = new SocketTransport(_sockets, runId, wake.Set);
        _made.Add((transport, wake));
        return (transport, wake, runId);
    }

    /// <summary>The next event, waiting for a pump to post it; the failure of a hang, never a measurement.</summary>
    private static TransportEvent Next(SocketTransport transport, ManualResetEventSlim wake)
    {
        while (true)
        {
            wake.Reset();
            if (transport.TryTake(out var taken)) return taken;
            Assert.True(wake.Wait(Guard), "no event arrived");
        }
    }

    private static PeerEndpoint Server(string spelling, string runId) => new(PeerEndpointKind.Server, spelling, runId);

    private static PeerEndpoint Bell(string spelling, string runId) => new(PeerEndpointKind.Bell, spelling, runId);

    private string FileOf(string kind, string spelling, string runId) => Path.Combine(_sockets.Path, kind, spelling, runId);

    [Fact]
    public void APublishedListenerIsEnumeratedConnectedAndExchangesFramesBothWays()
    {
        var (server, serverWake, serverRun) = Run();
        var (requestor, requestorWake, _) = Run();
        server.Publish(Server("SRV", serverRun));
        Assert.True(File.Exists(FileOf("s", "SRV", serverRun)));
        Assert.Equal([serverRun], requestor.EnumerateServers("SRV"));

        var connected = requestor.Connect(Server("SRV", serverRun));
        Assert.Equal(ConnectOutcome.Connected, connected.Outcome);
        var accepted = Assert.IsType<LinkAccepted>(Next(server, serverWake));

        Assert.True(requestor.Write(connected.Link, McsFrameKind.Offer, [1, 2, 3]));
        var arrived = Assert.IsType<FrameReceived>(Next(server, serverWake));
        Assert.Equal(accepted.Link, arrived.Link);
        Assert.Equal(McsFrameKind.Offer, arrived.Kind);
        Assert.Equal(new byte[] { 1, 2, 3 }, arrived.Payload);

        Assert.True(server.Write(accepted.Link, McsFrameKind.Claim, []));
        var reply = Assert.IsType<FrameReceived>(Next(requestor, requestorWake));
        Assert.Equal(connected.Link, reply.Link);
        Assert.Equal(McsFrameKind.Claim, reply.Kind);
    }

    [Fact]
    public void FramesArriveInTheOrderTheyWereWrittenAndAWholeFrameIsOneEvent()
    {
        var (server, serverWake, serverRun) = Run();
        var (requestor, _, _) = Run();
        server.Publish(Server("ORDER", serverRun));
        var link = requestor.Connect(Server("ORDER", serverRun)).Link;
        Next(server, serverWake);                          // LinkAccepted
        var big = new byte[300_000];
        big.AsSpan().Fill(7);
        Assert.True(requestor.Write(link, McsFrameKind.Offer, [1]));
        Assert.True(requestor.Write(link, McsFrameKind.Grant, big));
        Assert.True(requestor.Write(link, McsFrameKind.Withdraw, [2]));
        Assert.Equal(McsFrameKind.Offer, Assert.IsType<FrameReceived>(Next(server, serverWake)).Kind);
        var grant = Assert.IsType<FrameReceived>(Next(server, serverWake));
        Assert.Equal(McsFrameKind.Grant, grant.Kind);
        Assert.Equal(big, grant.Payload);
        Assert.Equal(McsFrameKind.Withdraw, Assert.IsType<FrameReceived>(Next(server, serverWake)).Kind);
    }

    [Fact]
    public void NoListenerIsVisibleUnderItsFinalNameBeforeItListens()
    {
        // design 5.4.1 "Publishing a listener": bound and listening at t/<r>, renamed into place after.
        var (server, _, serverRun) = Run();
        var (requestor, _, _) = Run();
        bool visibleInWindow = true;
        IReadOnlyList<string> enumeratedInWindow = ["x"];
        server.BetweenListenAndRename = endpoint =>
        {
            visibleInWindow = File.Exists(FileOf("s", endpoint.Spelling, endpoint.RunId));
            enumeratedInWindow = requestor.EnumerateServers(endpoint.Spelling);
        };
        server.Publish(Server("WINDOW", serverRun));
        Assert.False(visibleInWindow);
        Assert.Empty(enumeratedInWindow);
        Assert.Equal([serverRun], requestor.EnumerateServers("WINDOW"));
        Assert.False(File.Exists(Path.Combine(_sockets.Path, "t", serverRun)));     // the temporary name is gone
    }

    [Fact]
    public void PublishingAgainThrowsAndAFailedPublishLeavesNoTemporaryFile()
    {
        var (server, _, serverRun) = Run();
        server.Publish(Server("TWICE", serverRun));
        Assert.Throws<IOException>(() => server.Publish(Server("TWICE", serverRun)));
        Assert.Throws<ArgumentException>(() => server.Publish(Server("TWICE", "someoneelse")));
        Assert.Equal([serverRun], server.EnumerateServers("TWICE"));
    }

    [Fact]
    public void AKilledServersFileIsRefusedOnceAndDeletedByTheRefusal()
    {
        var (server, _, serverRun) = Run();
        var (requestor, _, _) = Run();
        server.Publish(Server("KILLED", serverRun));
        server.Abort();
        Assert.True(File.Exists(FileOf("s", "KILLED", serverRun)));                 // a kill leaves the file behind
        Assert.Equal([serverRun], requestor.EnumerateServers("KILLED"));

        var refused = requestor.Connect(Server("KILLED", serverRun));
        Assert.Equal(ConnectOutcome.Refused, refused.Outcome);
        Assert.False(File.Exists(FileOf("s", "KILLED", serverRun)));                 // the refusal rule deleted it
        Assert.Empty(requestor.EnumerateServers("KILLED"));
        Assert.Equal(ConnectOutcome.Refused, requestor.Connect(Server("KILLED", serverRun)).Outcome);   // a file already gone is skipped
    }

    [Fact]
    public void AConnectToAnEntryThatNeverExistedIsRefused()
    {
        var (requestor, _, _) = Run();
        Assert.Equal(ConnectOutcome.Refused, requestor.Connect(Server("NOBODY", "0000000000000000")).Outcome);
        Assert.Empty(requestor.EnumerateServers("NOBODY"));
    }

    [Fact]
    public void AKilledPartnerEndsTheOtherEndsLinkAndNothingElse()
    {
        var (server, serverWake, serverRun) = Run();
        var (requestor, requestorWake, _) = Run();
        server.Publish(Server("KILL", serverRun));
        var link = requestor.Connect(Server("KILL", serverRun)).Link;
        Next(server, serverWake);
        server.Abort();
        Assert.Equal(link, Assert.IsType<LinkLost>(Next(requestor, requestorWake)).Link);
        Assert.False(requestor.TryTake(out _));
        Assert.False(requestor.Write(link, McsFrameKind.Offer, [1]));                // a write to an ended link is false, not an exception
        Assert.Throws<ObjectDisposedException>(() => server.EnumerateServers("KILL")); // a killed transport takes no further call
    }

    [Fact]
    public void AGracefulCloseEndsTheOtherEndAndPostsNothingToTheCloser()
    {
        var (server, serverWake, serverRun) = Run();
        var (requestor, requestorWake, _) = Run();
        server.Publish(Server("CLOSE", serverRun));
        var link = requestor.Connect(Server("CLOSE", serverRun)).Link;
        var accepted = Assert.IsType<LinkAccepted>(Next(server, serverWake));
        Assert.True(requestor.Write(link, McsFrameKind.Offer, [9]));
        requestor.Close(link);
        requestor.Close(link);                                                        // closing an ended link is skipped
        Assert.Equal(McsFrameKind.Offer, Assert.IsType<FrameReceived>(Next(server, serverWake)).Kind);   // what was written before the close arrives
        Assert.Equal(accepted.Link, Assert.IsType<LinkLost>(Next(server, serverWake)).Link);
        Assert.False(requestor.TryTake(out _));
        Assert.False(requestor.Write(link, McsFrameKind.Offer, [9]));
    }

    [Fact]
    public void ALinkThatCarriesAMalformedFrameIsClosedAndLost()
    {
        var (server, serverWake, serverRun) = Run();
        server.Publish(Server("BAD", serverRun));
        using var raw = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        raw.Connect(new UnixDomainSocketEndPoint(FileOf("s", "BAD", serverRun)));
        var accepted = Assert.IsType<LinkAccepted>(Next(server, serverWake));
        raw.Send([0, 0, 0, 0, 200]);                                                  // a kind byte no build knows
        Assert.Equal(accepted.Link, Assert.IsType<LinkLost>(Next(server, serverWake)).Link);
    }

    [Fact]
    public void ALinkWhoseAnnouncedLengthCannotBeAllocatedIsLostNotLeftWaiting()
    {
        var (server, serverWake, serverRun) = Run();
        server.Publish(Server("HUGE", serverRun));
        using var raw = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        raw.Connect(new UnixDomainSocketEndPoint(FileOf("s", "HUGE", serverRun)));
        var accepted = Assert.IsType<LinkAccepted>(Next(server, serverWake));
        raw.Send([0xF0, 0xFF, 0xFF, 0x7F, (byte)McsFrameKind.Offer]);                 // below the 2^31 guard, above Array.MaxLength: new byte[length] throws OutOfMemoryException
        Assert.Equal(accepted.Link, Assert.IsType<LinkLost>(Next(server, serverWake)).Link);
    }

    [Fact]
    public void DisposeClosesLinksUnpublishesAndDeletesTheFiles()
    {
        var (server, serverWake, serverRun) = Run();
        var (requestor, requestorWake, _) = Run();
        server.Publish(Server("END", serverRun));
        server.Publish(Bell("END", serverRun));
        var link = requestor.Connect(Server("END", serverRun)).Link;
        Next(server, serverWake);
        server.Dispose();
        Assert.Equal(link, Assert.IsType<LinkLost>(Next(requestor, requestorWake)).Link);
        Assert.False(File.Exists(FileOf("s", "END", serverRun)));
        Assert.False(File.Exists(FileOf("p", "END", serverRun)));
        Assert.Empty(requestor.EnumerateServers("END"));
    }

    [Fact]
    public void UnpublishDeletesTheFileAndSkipsAnEndpointNotPublished()
    {
        var (server, _, serverRun) = Run();
        var endpoint = Server("UNPUB", serverRun);
        server.Unpublish(endpoint);                                                   // not published: skipped
        server.Publish(endpoint);
        server.Unpublish(endpoint);
        Assert.False(File.Exists(FileOf("s", "UNPUB", serverRun)));
        server.Publish(endpoint);                                                     // and it can be published again
        Assert.True(File.Exists(FileOf("s", "UNPUB", serverRun)));
    }

    [Fact]
    public void ARingReachesEveryOtherRunUnitsBellAndOnlyThatSpellingsBells()
    {
        var (ringer, _, ringerRun) = Run();
        var (first, firstWake, firstRun) = Run();
        var (second, secondWake, secondRun) = Run();
        var (other, otherWake, otherRun) = Run();
        first.Publish(Bell("RING", firstRun));
        second.Publish(Bell("RING", secondRun));
        other.Publish(Bell("OTHER", otherRun));
        ringer.Publish(Bell("RING", ringerRun));
        ringer.RingBells("RING");
        Assert.Equal("RING", Assert.IsType<BellRung>(Next(first, firstWake)).Spelling);
        Assert.Equal("RING", Assert.IsType<BellRung>(Next(second, secondWake)).Spelling);
        Assert.False(ringer.TryTake(out _));                                          // a ringer does not wake its own bell
        ringer.RingBells("OTHER");
        Assert.Equal("OTHER", Assert.IsType<BellRung>(Next(other, otherWake)).Spelling);
        Assert.False(first.TryTake(out _));
    }

    [Fact]
    public void ADeadBellIsRefusedAndItsFileDeletedWhileTheLiveOnesStillRing()
    {
        var (ringer, _, _) = Run();
        var (live, liveWake, liveRun) = Run();
        var (dead, _, deadRun) = Run();
        live.Publish(Bell("DEADBELL", liveRun));
        dead.Publish(Bell("DEADBELL", deadRun));
        dead.Abort();
        ringer.RingBells("DEADBELL");
        Assert.IsType<BellRung>(Next(live, liveWake));
        Assert.False(File.Exists(FileOf("p", "DEADBELL", deadRun)));
        Assert.True(File.Exists(FileOf("p", "DEADBELL", liveRun)));
    }

    [Fact]
    public void ARingToNoBellsIsHarmless()
    {
        var (ringer, _, _) = Run();
        ringer.RingBells("NOBELLS");
    }

    [Fact]
    public void ABellConnectionThatDoesNotRingRingsNothing()
    {
        var (bell, bellWake, bellRun) = Run();
        var (ringer, _, _) = Run();
        bell.Publish(Bell("QUIET", bellRun));
        using (var raw = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
        {
            raw.Connect(new UnixDomainSocketEndPoint(FileOf("p", "QUIET", bellRun)));
            raw.Send(McsFrame.Encode(McsFrameKind.Offer, [1]));                       // a frame that is not a Ring
        }
        ringer.RingBells("QUIET");                                                    // a real ring after it still arrives
        Assert.IsType<BellRung>(Next(bell, bellWake));
        Assert.False(bell.TryTake(out _));
    }

    [Fact]
    public void ANotifyConnectsToTheListenerAndAClosedOneIsAnIoFailure()
    {
        string path = Path.Combine(_sockets.Path, "n-test");
        using var listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        listener.Bind(new UnixDomainSocketEndPoint(path));
        listener.Listen(1);
        var (notifier, _, _) = Run();
        notifier.Notify(path);
        using var accepted = listener.Accept();                                       // the connection arrived
        listener.Dispose();
        File.Delete(path);
        Assert.Throws<IOException>(() => notifier.Notify(path));
    }

    [Fact]
    public void APathNoAfUnixSocketCanHoldIsAnIoFailureOnEveryPathAndLeavesNothingBehind()
    {
        var (transport, _, runId) = Run();
        string tooLong = new('n', McsSocketDirectory.MaxPathBytes);                   // too long under any <S>, whatever this host's temp path is
        int room = McsSocketDirectory.MaxPathBytes - System.Text.Encoding.UTF8.GetByteCount(_sockets.Path);

        Assert.Contains("AF_UNIX", Assert.Throws<IOException>(() => transport.Notify(Path.Combine(_sockets.Path, tooLong))).Message);
        Assert.Contains("AF_UNIX", Assert.Throws<IOException>(() => transport.Notify("")).Message);
        Assert.Contains("AF_UNIX", Assert.Throws<IOException>(() => transport.Notify("a\0b")).Message);
        Assert.Contains("AF_UNIX", Assert.Throws<IOException>(() => transport.Connect(Server(tooLong, runId))).Message);
        Assert.Contains("AF_UNIX", Assert.Throws<IOException>(() => transport.Publish(Server(tooLong, runId))).Message);
        Assert.False(Directory.Exists(Path.Combine(_sockets.Path, "s")));             // refused before anything was made
        Assert.False(Directory.Exists(Path.Combine(_sockets.Path, "t")));

        // At the limit the path reaches the socket: nobody listens there, so the answer is the refusal.
        Assert.Contains("refused", Assert.Throws<IOException>(() => transport.Notify(_sockets.Path + new string('n', room))).Message);

        // A ring decides nothing (design 5.4.1): an entry too long to reach under p/ costs that one ring.
        Directory.CreateDirectory(Path.Combine(_sockets.Path, "p", "RING"));
        File.WriteAllText(Path.Combine(_sockets.Path, "p", "RING", tooLong), "");
        transport.RingBells("RING");

        // The refusals left the transport whole.
        transport.Publish(Server("WHOLE", runId));
        Assert.True(File.Exists(FileOf("s", "WHOLE", runId)));
    }

    [Fact]
    public void ATransportUnderALongTempRootRefusesEveryPathAndCreatesNothing()
    {
        string longRoot = Path.Combine(_work, new string('t', McsSocketDirectory.MaxPathBytes)) + Path.DirectorySeparatorChar;     // a host whose temp path is long
        string runId = McsSocketDirectory.NewRunId();
        using var transport = new SocketTransport(McsSocketDirectory.Under(longRoot), runId, () => { });
        Assert.Throws<IOException>(() => transport.Publish(Server("SRV", runId)));
        Assert.Throws<IOException>(() => transport.Publish(Bell("SRV", runId)));
        Assert.Throws<IOException>(() => transport.Connect(Server("SRV", runId)));
        Assert.Empty(transport.EnumerateServers("SRV"));
        transport.RingBells("SRV");
        Assert.False(Directory.Exists(longRoot));
    }

    [Theory]
    [InlineData(new byte[] { })]                                         // the stream ends between frames: null, not an error
    [InlineData(new byte[] { 3, 0, 0, 0, 3, 1, 2, 3 })]                  // an Offer with three payload bytes
    [InlineData(new byte[] { 0, 0, 0, 0, 1 })]                           // a Ring
    [InlineData(new byte[] { 2, 0, 0 })]                                 // a header cut short
    [InlineData(new byte[] { 3, 0, 0, 0, 3, 1 })]                        // a payload cut short
    [InlineData(new byte[] { 0, 0, 0, 0, 200 })]                         // a kind no build knows
    [InlineData(new byte[] { 1, 0, 0, 0, 1, 9 })]                        // a Ring with a payload
    [InlineData(new byte[] { 0, 0, 0, 128, 3 })]                         // a length at the 2^31 guard
    public async Task TheAwaitedReadAnswersWhatTheSynchronousReadAnswers(byte[] bytes)
    {
        (McsFrameKind Kind, byte[] Payload)? sync = null, awaited = null;
        Exception? syncFailure = null, awaitedFailure = null;
        try { sync = McsFrame.Read(new MemoryStream(bytes)); }
        catch (InvalidDataException e) { syncFailure = e; }
        try { awaited = await McsFrame.ReadAsync(new MemoryStream(bytes), CancellationToken.None); }
        catch (InvalidDataException e) { awaitedFailure = e; }
        Assert.Equal(syncFailure is null, awaitedFailure is null);
        Assert.Equal(sync?.Kind, awaited?.Kind);
        Assert.Equal(sync?.Payload, awaited?.Payload);
        if (syncFailure is not null) Assert.Equal(syncFailure.Message, awaitedFailure!.Message);
    }

    [Fact]
    public void ManyLinksOnOneListenerKeepTheirFramesApart()
    {
        var (server, serverWake, serverRun) = Run();
        var (requestor, _, _) = Run();
        server.Publish(Server("MANY", serverRun));
        var links = Enumerable.Range(0, 5).Select(_ => requestor.Connect(Server("MANY", serverRun)).Link).ToList();
        var accepted = Enumerable.Range(0, 5).Select(_ => Assert.IsType<LinkAccepted>(Next(server, serverWake)).Link).ToList();
        for (int i = 0; i < links.Count; i++) Assert.True(requestor.Write(links[i], McsFrameKind.Offer, [(byte)i]));
        var seen = new Dictionary<PeerLinkId, byte>();
        for (int i = 0; i < links.Count; i++)
        {
            var frame = Assert.IsType<FrameReceived>(Next(server, serverWake));
            seen[frame.Link] = frame.Payload[0];
        }
        Assert.Equal(links.Count, seen.Count);
        Assert.All(accepted, a => Assert.Contains(a, seen.Keys));
        Assert.Equal(Enumerable.Range(0, 5).Select(i => (byte)i).Order(), seen.Values.Order());
    }
}
