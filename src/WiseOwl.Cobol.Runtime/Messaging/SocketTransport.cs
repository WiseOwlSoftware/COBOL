// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Concurrent;
using System.Net.Sockets;
using WiseOwl.Cobol.Runtime.Mcs;

namespace WiseOwl.Cobol.Runtime.Messaging;

/// <summary>
/// ⛔ <see cref="IPeerTransport"/> OVER AF_UNIX STREAM SOCKETS AND THE SOCKET DIRECTORY (docs/rearchitecture/DESIGN-message-control.md §5.1, §5.4.1, §5.5, §9 S2b).
/// A run unit's endpoints are files under <c>&lt;S&gt;</c>: a server's listener <c>s/&lt;N&gt;/&lt;r&gt;</c> and a requestor's pending bell <c>p/&lt;N&gt;/&lt;r&gt;</c>.
/// <para><b>Publishing.</b> .NET's <c>Bind</c> and <c>Listen</c> are separate calls, and a connect that lands between them finds a socket that is not
/// listening (refused on Linux and Windows, witnessed by <c>PlatformAssumptionTests</c>). So a listener is bound at <c>t/&lt;r&gt;</c>, listens, and only then
/// has its file renamed to the final name with <see cref="File.Move(string, string)"/>, which the same class witnesses on each OS (the renamed
/// file reaches the listener; the old name finds none). A file under <c>s/</c> or <c>p/</c> therefore always belonged to a listening socket, and a connect to it
/// that is refused means its owner has ended: <b>the refusal rule</b> deletes the file (a file already gone is skipped).</para>
/// <para><b>Events.</b> Pumps and accept loops run on the thread pool and only post <see cref="TransportEvent"/>s to a queue and call the wake action the
/// peer waits on; every member below runs on the run unit's thread, as <see cref="IPeerTransport"/> requires. A <see cref="LinkLost"/> is taken once, and
/// then the link's entry is dropped.</para>
/// <para><b>Ends.</b> <see cref="Dispose"/> is the run unit's normal end (links closed, listeners unpublished, their files deleted); <see cref="Abort"/> is a
/// kill (every socket dropped, the published files left behind for a later connect to refuse and delete).</para>
/// </summary>
internal sealed class SocketTransport : IPeerTransport
{
    /// <summary>How long one connect may take before it is given up as an <see cref="IOException"/> (a listener accepts on the thread pool, so its backlog never stays full).</summary>
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);

    /// <summary>How long a bell's connection may take to deliver its one <see cref="McsFrameKind.Ring"/> before it is dropped.</summary>
    private static readonly TimeSpan RingTimeout = TimeSpan.FromSeconds(5);

    private const int ListenBacklog = 128;

    private sealed class Listener(Socket socket, CancellationTokenSource stop, string path)
    {
        internal Socket Socket { get; } = socket;
        internal CancellationTokenSource Stop { get; } = stop;
        internal string Path { get; } = path;

        internal void Close()
        {
            Stop.Cancel();
            Socket.Dispose();
        }
    }

    private readonly McsSocketDirectory _directory;
    private readonly string _runId;
    private readonly Action _wake;
    private readonly ConcurrentQueue<TransportEvent> _events = new();
    private readonly ConcurrentDictionary<int, PeerLink> _links = new();
    private readonly Dictionary<PeerEndpoint, Listener> _listeners = [];
    private int _nextLink;
    private bool _dead;

    /// <summary>A transport for the run unit <paramref name="runId"/> under <paramref name="directory"/>; <paramref name="wake"/> is called from a pump whenever an event is queued.</summary>
    internal SocketTransport(McsSocketDirectory directory, string runId, Action wake)
    {
        _directory = directory;
        _runId = runId;
        _wake = wake;
    }

    /// <summary>Called by <see cref="Publish"/> after the listener is bound and listening and before its file takes its final name: the window design 5.4.1
    /// closes. A test that enumerates, connects or rings here proves that no one sees a listener that is not yet listening.</summary>
    internal Action<PeerEndpoint>? BetweenListenAndRename { get; set; }

    public void Publish(PeerEndpoint endpoint)
    {
        ThrowIfDead();
        if (endpoint.RunId != _runId) throw new ArgumentException("a transport publishes only its own run id", nameof(endpoint));
        if (_listeners.ContainsKey(endpoint)) throw new IOException($"{endpoint} is already published");
        string final = PathOf(endpoint), temp = Path.Combine(_directory.Path, "t", _runId);
        CreateDirectory(Path.GetDirectoryName(final)!);
        CreateDirectory(Path.GetDirectoryName(temp)!);
        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        try
        {
            socket.Bind(new UnixDomainSocketEndPoint(temp));
            socket.Listen(ListenBacklog);
            BetweenListenAndRename?.Invoke(endpoint);
            File.Move(temp, final);
        }
        catch (Exception e) when (e is SocketException or UnauthorizedAccessException or IOException)
        {
            socket.Dispose();
            DeleteQuietly(temp);
            throw e as IOException ?? new IOException($"cannot publish {endpoint}: {e.Message}", e);
        }
        var listener = new Listener(socket, new CancellationTokenSource(), final);
        _listeners[endpoint] = listener;
        _ = endpoint.Kind == PeerEndpointKind.Server
            ? Task.Run(() => AcceptServerAsync(listener))
            : Task.Run(() => AcceptBellAsync(listener, endpoint.Spelling));
    }

    public void Unpublish(PeerEndpoint endpoint)
    {
        if (!_listeners.Remove(endpoint, out var listener)) return;
        listener.Close();
        DeleteQuietly(listener.Path);
    }

    public IReadOnlyList<string> EnumerateServers(string spelling)
    {
        ThrowIfDead();
        return EntriesUnder(PeerEndpointKind.Server, spelling);
    }

    public ConnectResult Connect(PeerEndpoint endpoint)
    {
        ThrowIfDead();
        var socket = ConnectTo(PathOf(endpoint));
        if (socket is null) return new ConnectResult(ConnectOutcome.Refused, default);
        var link = NewLink(socket);
        link.StartPump();
        return new ConnectResult(ConnectOutcome.Connected, link.Id);
    }

    public bool Write(PeerLinkId link, McsFrameKind kind, ReadOnlySpan<byte> payload)
    {
        ThrowIfDead();
        return _links.TryGetValue(link.Value, out var found) && found.Write(kind, payload);
    }

    public void Close(PeerLinkId link)
    {
        if (_links.TryRemove(link.Value, out var found)) found.Close();
    }

    public void RingBells(string spelling)
    {
        ThrowIfDead();
        byte[] ring = McsFrame.Encode(McsFrameKind.Ring, []);
        foreach (string runId in EntriesUnder(PeerEndpointKind.Bell, spelling))
        {
            if (runId == _runId) continue;                // this run unit's own bell: it is no one to wake
            try
            {
                using var socket = ConnectTo(PathOf(new PeerEndpoint(PeerEndpointKind.Bell, spelling, runId)));
                socket?.Send(ring);                       // refused = the bell's owner has ended, and the refusal rule deleted its file
            }
            catch (Exception e) when (e is IOException or SocketException or ObjectDisposedException)
            {
                // design 5.4.1: a ring decides nothing, and a lost one costs one enumeration
            }
        }
    }

    public bool TryTake(out TransportEvent transportEvent)
    {
        if (!_events.TryDequeue(out transportEvent!)) return false;
        if (transportEvent is LinkLost lost) _links.TryRemove(lost.Link.Value, out _);
        return true;
    }

    /// <summary>Connects to a notify socket another process listens on and closes it again (design 5.4.5: the server tells the requestor that started it that
    /// its listener is published). A notify socket that refuses is an <see cref="IOException"/>: the requestor that bound it is gone, or never listened.</summary>
    internal void Notify(string path)
    {
        ThrowIfDead();
        using var socket = ConnectTo(path) ?? throw new IOException($"the notify socket '{path}' refused the connection");
    }

    /// <summary>The kill: every socket is dropped as the OS drops a killed process's, nothing is unpublished, and the transport takes no further call.</summary>
    internal void Abort()
    {
        if (_dead) return;
        _dead = true;
        foreach (var listener in _listeners.Values) listener.Close();     // the files stay: the next connect to one is refused and deletes it
        _listeners.Clear();
        foreach (var link in _links.Values) link.Abort();
        _links.Clear();
        _events.Clear();
    }

    /// <summary>Normal end: links closed, listeners unpublished and their files deleted. Nothing is left under the socket directory but other run units' files.</summary>
    public void Dispose()
    {
        if (_dead) return;
        _dead = true;
        foreach (var link in _links.Values) link.Close();
        _links.Clear();
        foreach (var endpoint in _listeners.Keys.ToArray())
        {
            var listener = _listeners[endpoint];
            listener.Close();
            DeleteQuietly(listener.Path);
        }
        _listeners.Clear();
    }

    // ---------------------------------------------------------------- the socket directory

    private string PathOf(PeerEndpoint endpoint) =>
        Path.Combine(_directory.Path, endpoint.Kind == PeerEndpointKind.Server ? "s" : "p", endpoint.Spelling, endpoint.RunId);

    private IReadOnlyList<string> EntriesUnder(PeerEndpointKind kind, string spelling)
    {
        string directory = Path.GetDirectoryName(PathOf(new PeerEndpoint(kind, spelling, "x")))!;
        try
        {
            List<string> names = [];
            foreach (string entry in Directory.EnumerateFileSystemEntries(directory)) names.Add(Path.GetFileName(entry));
            names.Sort(StringComparer.Ordinal);
            return names;
        }
        catch (DirectoryNotFoundException)
        {
            return [];
        }
    }

    private static void CreateDirectory(string path)
    {
        if (OperatingSystem.IsWindows()) Directory.CreateDirectory(path);
        else Directory.CreateDirectory(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }

    private static void DeleteQuietly(string path)
    {
        try { File.Delete(path); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { /* a file already gone is skipped (the refusal rule); one that cannot be deleted stays for the next refusal */ }
    }

    /// <summary>Connects to the socket file <paramref name="path"/>. Null when its owner has ended: a refused connect deletes the file and a file already gone is skipped
    /// (the refusal rule, design 5.4.1). Any other failure is an <see cref="IOException"/>.</summary>
    private static Socket? ConnectTo(string path)
    {
        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        try
        {
            using var timeout = new CancellationTokenSource(ConnectTimeout);
            socket.ConnectAsync(new UnixDomainSocketEndPoint(path), timeout.Token).AsTask().GetAwaiter().GetResult();
            return socket;
        }
        catch (SocketException e)
        {
            socket.Dispose();
            if (e.SocketErrorCode == SocketError.ConnectionRefused)
            {
                DeleteQuietly(path);
                return null;
            }
            if (!File.Exists(path)) return null;
            throw new IOException($"cannot connect to '{path}': {e.Message}", e);
        }
        catch (OperationCanceledException e)
        {
            socket.Dispose();
            throw new IOException($"connecting to '{path}' did not finish within {ConnectTimeout.TotalSeconds:0} seconds", e);
        }
    }

    // ---------------------------------------------------------------- links, accept loops

    private PeerLink NewLink(Socket socket)
    {
        var link = new PeerLink(new PeerLinkId(Interlocked.Increment(ref _nextLink)), socket, Post);
        _links[link.Id.Value] = link;
        return link;
    }

    private void Post(TransportEvent transportEvent)
    {
        _events.Enqueue(transportEvent);
        _wake();
    }

    /// <summary>A server's listener accepts for as long as it is published; each connection is a link, announced before its first frame can be.</summary>
    private async Task AcceptServerAsync(Listener listener)
    {
        try
        {
            while (true)
            {
                var accepted = await listener.Socket.AcceptAsync(listener.Stop.Token).ConfigureAwait(false);
                var link = NewLink(accepted);
                Post(new LinkAccepted(link.Id));
                link.StartPump();
            }
        }
        catch (Exception e) when (e is SocketException or ObjectDisposedException or OperationCanceledException)
        {
            // the listener was closed: Unpublish, Dispose or Abort
        }
    }

    /// <summary>A bell's listener: each connection carries one <see cref="McsFrameKind.Ring"/> and is then closed; anything else is dropped, so a stalled or malformed
    /// connection costs a pending read, never the accept loop.</summary>
    private async Task AcceptBellAsync(Listener listener, string spelling)
    {
        try
        {
            while (true)
            {
                var accepted = await listener.Socket.AcceptAsync(listener.Stop.Token).ConfigureAwait(false);
                _ = Task.Run(() => ReadRingAsync(accepted, spelling, listener.Stop.Token));
            }
        }
        catch (Exception e) when (e is SocketException or ObjectDisposedException or OperationCanceledException)
        {
            // the bell was closed
        }
    }

    private async Task ReadRingAsync(Socket accepted, string spelling, CancellationToken stopped)
    {
        using var stream = new NetworkStream(accepted, ownsSocket: true);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stopped);
        timeout.CancelAfter(RingTimeout);
        try
        {
            if (await McsFrame.ReadAsync(stream, timeout.Token).ConfigureAwait(false) is { Kind: McsFrameKind.Ring }) Post(new BellRung(spelling));
        }
        catch (Exception e) when (e is IOException or SocketException or ObjectDisposedException or InvalidDataException or OperationCanceledException)
        {
            // a connection that does not ring is dropped
        }
    }

    private void ThrowIfDead()
    {
        if (_dead) throw new ObjectDisposedException(nameof(SocketTransport), "the run unit was killed or has ended");
    }
}
