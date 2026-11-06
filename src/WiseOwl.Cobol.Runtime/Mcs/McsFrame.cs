// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;

namespace WiseOwl.Cobol.Runtime.Mcs;

/// <summary>The kinds of <see cref="McsFrame"/>: one enumeration for both messaging facilities (docs/rearchitecture/DESIGN-message-control.md §6).
/// <see cref="Ring"/> is the only kind a bell receives; the 2023 facility's kinds (§5.3) join it in the slice that builds that transport.</summary>
internal enum McsFrameKind : byte
{
    /// <summary>A bell's wake-up: no payload. Both facilities send it.</summary>
    Ring = 1,
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
        uint length = BinaryPrimitives.ReadUInt32LittleEndian(header);
        if (length >= PayloadLimit) throw new InvalidDataException($"a frame announces {length} payload bytes, which reaches the {PayloadLimit}-byte guard");
        var kind = (McsFrameKind)header[4];
        if (!Enum.IsDefined(kind)) throw new InvalidDataException($"frame kind {header[4]} is none this build knows");
        CheckPayload(kind, (int)length);
        var payload = new byte[length];
        if (Fill(stream, payload) < payload.Length) throw new InvalidDataException("the stream ended inside a frame payload");
        return (kind, payload);
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
}
