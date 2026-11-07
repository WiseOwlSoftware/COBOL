// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;

namespace WiseOwl.Cobol.Runtime.Mcs;

/// <summary>The kinds of <see cref="McsFrame"/>: one enumeration for both messaging facilities (docs/rearchitecture/DESIGN-message-control.md §6).
/// <see cref="Ring"/> is the only kind a bell receives; the rest are the 2023 facility's (§5.3), whose payloads
/// <c>PeerFrameCodec</c> (Runtime/Messaging) encodes.</summary>
internal enum McsFrameKind : byte
{
    /// <summary>A bell's wake-up: no payload. Both facilities send it.</summary>
    Ring = 1,

    /// <summary>A link's opening, both ways: protocol version, MCS identity, run id, process id, served name.</summary>
    Hello = 2,

    /// <summary>A requestor offers its message (serial, offer round) to a server; the message stays with the requestor.</summary>
    Offer = 3,

    /// <summary>A server claims an offered message.</summary>
    Claim = 4,

    /// <summary>The requestor grants the message to the first claimant, and carries it.</summary>
    Grant = 5,

    /// <summary>The requestor withdraws an offer from a server that was not granted it.</summary>
    Withdraw = 6,

    /// <summary>The server has the message; written before its RECEIVE returns.</summary>
    Ack = 7,

    /// <summary>The server's answer: a message and the exception-name it raises, if any.</summary>
    Reply = 8,
}

/// <summary>
/// ⛔ THE ONE FRAME CODEC OF THE TWO MESSAGING FACILITIES (docs/rearchitecture/DESIGN-message-control.md §6): <c>u32 length | u8 kind |
/// payload</c>, little-endian, where <c>length</c> is the PAYLOAD's byte count (the kind byte is not counted) and a payload of
/// 2^31 bytes or more is refused on both sides (the guard). A reader that meets a kind it does not expect closes the link:
/// <see cref="Read"/> answers the kind and the caller compares it, and a byte that names no <see cref="McsFrameKind"/>, a truncated
/// frame and an oversize length are all <see cref="InvalidDataException"/>, which the caller answers by closing the link.
/// </summary>
internal static class McsFrame
{
    /// <summary>The bytes before the payload: the u32 length and the u8 kind.</summary>
    internal const int HeaderBytes = 5;

    /// <summary>The guard: a payload this long or longer is refused.</summary>
    internal const long PayloadLimit = 1L << 31;

    /// <summary>The frame as bytes, for a single write (a bell's ring must reach the socket as one send).</summary>
    internal static byte[] Encode(McsFrameKind kind, ReadOnlySpan<byte> payload)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind), kind, "no such frame kind");
        CheckPayload(kind, payload.Length);
        var frame = new byte[HeaderBytes + payload.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(frame, (uint)payload.Length);
        frame[4] = (byte)kind;
        payload.CopyTo(frame.AsSpan(HeaderBytes));
        return frame;
    }

    /// <summary>Writes one frame as a single <see cref="Stream.Write(ReadOnlySpan{byte})"/>.</summary>
    internal static void Write(Stream stream, McsFrameKind kind, ReadOnlySpan<byte> payload) => stream.Write(Encode(kind, payload));

    /// <summary>Reads one frame; null when the stream ends before its first byte (the peer closed between frames).</summary>
    /// <exception cref="InvalidDataException">The stream ends inside a frame, the length reaches <see cref="PayloadLimit"/>, the kind byte names no
    /// <see cref="McsFrameKind"/>, or the kind forbids the payload it carries.</exception>
    internal static (McsFrameKind Kind, byte[] Payload)? Read(Stream stream)
    {
        Span<byte> header = stackalloc byte[HeaderBytes];
        int got = Fill(stream, header);
        if (got == 0) return null;
        if (got < HeaderBytes) throw new InvalidDataException("the stream ended inside a frame header");
        var (kind, length) = ParseHeader(header);
        var payload = new byte[length];
        if (Fill(stream, payload) < payload.Length) throw new InvalidDataException("the stream ended inside a frame payload");
        return (kind, payload);
    }

    /// <summary><see cref="Read"/> for a link's read pump, which must not hold a thread while its peer is silent: the same frame rules, awaited.</summary>
    /// <exception cref="InvalidDataException">As <see cref="Read"/>.</exception>
    internal static async ValueTask<(McsFrameKind Kind, byte[] Payload)?> ReadAsync(Stream stream, CancellationToken cancellation)
    {
        var header = new byte[HeaderBytes];
        int got = await FillAsync(stream, header, cancellation).ConfigureAwait(false);
        if (got == 0) return null;
        if (got < HeaderBytes) throw new InvalidDataException("the stream ended inside a frame header");
        var (kind, length) = ParseHeader(header);
        var payload = new byte[length];
        if (await FillAsync(stream, payload, cancellation).ConfigureAwait(false) < payload.Length)
            throw new InvalidDataException("the stream ended inside a frame payload");
        return (kind, payload);
    }

    /// <summary>The one statement of a header's rules: the length guard, the kind, and the kind's own payload rule.</summary>
    private static (McsFrameKind Kind, int Length) ParseHeader(ReadOnlySpan<byte> header)
    {
        uint length = BinaryPrimitives.ReadUInt32LittleEndian(header);
        if (length >= PayloadLimit) throw new InvalidDataException($"a frame announces {length} payload bytes, which reaches the {PayloadLimit}-byte guard");
        var kind = (McsFrameKind)header[4];
        if (!Enum.IsDefined(kind)) throw new InvalidDataException($"frame kind {header[4]} is none this build knows");
        CheckPayload(kind, (int)length);
        return (kind, (int)length);
    }

    private static void CheckPayload(McsFrameKind kind, int payloadBytes)
    {
        if (kind == McsFrameKind.Ring && payloadBytes != 0) throw new InvalidDataException("a Ring frame carries no payload");
    }

    /// <summary>Reads until <paramref name="buffer"/> is full or the stream ends; the count read.</summary>
    private static int Fill(Stream stream, Span<byte> buffer)
    {
        int total = 0;
        while (total < buffer.Length)
        {
            int n = stream.Read(buffer[total..]);
            if (n == 0) break;
            total += n;
        }
        return total;
    }

    private static async ValueTask<int> FillAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellation)
    {
        int total = 0;
        while (total < buffer.Length)
        {
            int n = await stream.ReadAsync(buffer[total..], cancellation).ConfigureAwait(false);
            if (n == 0) break;
            total += n;
        }
        return total;
    }
}
