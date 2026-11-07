// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Net.Sockets;
using WiseOwl.Cobol.Runtime.Mcs;

namespace WiseOwl.Cobol.Runtime.Messaging;

/// <summary>
/// ⛔ ONE CONNECTED AF_UNIX STREAM AND ITS READ PUMP (docs/rearchitecture/DESIGN-message-control.md §5.5). The pump is an awaited loop on the thread pool, so a link
/// whose peer is silent holds no thread; it posts each whole <see cref="McsFrame"/> as a <see cref="FrameReceived"/> and, when the stream ends, exactly one
/// <see cref="LinkLost"/>: end-of-stream (<c>Socket.Receive</c> "returns zero ... because the peer socket performed a graceful shutdown"), any
/// <see cref="SocketException"/> or <see cref="IOException"/> (a killed process's sockets are reset, which PlatformAssumptionTests witnesses on each OS), or a
/// frame that does not decode (<see cref="InvalidDataException"/>; the link is closed, as design 6 has a reader that meets a frame it cannot read). Nothing is
/// posted for an end this side asked for (<see cref="Close"/>, <see cref="Abort"/>), as <see cref="InMemoryTransport"/> posts nothing to the closer.
/// The pump touches no protocol state: it only posts to the transport's event queue.
/// </summary>
internal sealed class PeerLink
{
    private readonly Socket _socket;
    private readonly NetworkStream _stream;
    private readonly Action<TransportEvent> _post;
    private readonly CancellationTokenSource _stop = new();
    private int _ended;

    /// <summary>A link over the connected <paramref name="socket"/>; the pump starts with <see cref="StartPump"/>, after the transport has posted what must precede the first frame.</summary>
    internal PeerLink(PeerLinkId id, Socket socket, Action<TransportEvent> post)
    {
        Id = id;
        _socket = socket;
        _stream = new NetworkStream(socket, ownsSocket: true);
        _post = post;
    }

    internal PeerLinkId Id { get; }

    /// <summary>Starts the read pump. Frames are posted in arrival order: the pump awaits each read before the next.</summary>
    internal void StartPump() => _ = Task.Run(PumpAsync);

    private async Task PumpAsync()
    {
        try
        {
            while (await McsFrame.ReadAsync(_stream, _stop.Token).ConfigureAwait(false) is { } frame)
            {
                _post(new FrameReceived(Id, frame.Kind, frame.Payload));
            }
        }
        catch (Exception)
        {
            // Any fault ends the link: an abnormal end (IOException, SocketException, ObjectDisposedException, OperationCanceledException), an unreadable frame
            // (InvalidDataException) or a legal length that cannot be allocated (OutOfMemoryException). The LinkLost below is posted for every one.
        }
        if (Interlocked.Exchange(ref _ended, 1) == 0)
        {
            Dispose();
            _post(new LinkLost(Id));
        }
    }

    /// <summary>One whole frame as one write; false when the link has ended (its <see cref="LinkLost"/> follows or has been delivered). Called from the run unit's thread only.</summary>
    internal bool Write(McsFrameKind kind, ReadOnlySpan<byte> payload)
    {
        if (Volatile.Read(ref _ended) != 0) return false;
        try
        {
            McsFrame.Write(_stream, kind, payload);
            return true;
        }
        catch (Exception e) when (e is IOException or SocketException or ObjectDisposedException)
        {
            return false;                  // the pump reads the same failure and posts the LinkLost
        }
    }

    /// <summary>Closes the stream gracefully (the other end reads end-of-stream) and posts nothing; an already ended link is skipped.</summary>
    internal void Close()
    {
        if (Interlocked.Exchange(ref _ended, 1) != 0) return;
        try { _socket.Shutdown(SocketShutdown.Both); }
        catch (Exception e) when (e is SocketException or ObjectDisposedException) { /* the peer is already gone: the close below is all that is left */ }
        Dispose();
    }

    /// <summary>The kill: the stream is dropped with no shutdown handshake and nothing is posted.</summary>
    internal void Abort()
    {
        Interlocked.Exchange(ref _ended, 1);
        Dispose();
    }

    private void Dispose()
    {
        _stop.Cancel();
        _stream.Dispose();
    }
}
