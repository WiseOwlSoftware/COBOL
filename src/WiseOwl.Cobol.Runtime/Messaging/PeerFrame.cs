// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;
using System.Text;
using WiseOwl.Cobol.Runtime.Mcs;

namespace WiseOwl.Cobol.Runtime.Messaging;

/// <summary>A message in flight (docs/rearchitecture/DESIGN-message-control.md design 5.3): its image, its extent table (§4.4; null for a message with no
/// variable-length component) and its slots, each a <see cref="MessageTag"/> (SEND SR3 and RECEIVE SR2 refuse every other slot class at compile time).</summary>
internal sealed record MessageBody(string Image, RecordExtents? Extents, object?[] Slots)
{
    /// <summary>A body of no slots and no extent table.</summary>
    internal static MessageBody Of(string image) => new(image, null, []);

    /// <summary>The same message cut to its first <paramref name="capacity"/> character positions (§0 Q7).</summary>
    internal MessageBody Truncated(int capacity) => Image.Length <= capacity ? this : this with { Image = Image[..capacity] };
}

/// <summary>One frame of the 2023 facility's protocol, decoded (design 5.3). <see cref="PeerFrameCodec"/> maps it to and from an <see cref="McsFrame"/> payload.</summary>
internal abstract record PeerFrame
{
    /// <summary>The kind this frame travels as.</summary>
    internal abstract McsFrameKind Kind { get; }
}

/// <summary>A link's opening, written by both ends. A requestor's <paramref name="ServedName"/> is empty; a server's is the normalized name it serves,
/// which a requestor compares with the one it asked for, since two names whose hashes collide share a directory (design 5.1).</summary>
internal sealed record HelloFrame(int Version, string Mcs, string RunId, int Pid, string ServedName) : PeerFrame
{
    internal override McsFrameKind Kind => McsFrameKind.Hello;
}

/// <summary>A requestor offers message <paramref name="Serial"/> in offer round <paramref name="Round"/>.</summary>
internal sealed record OfferFrame(long Serial, int Round) : PeerFrame
{
    internal override McsFrameKind Kind => McsFrameKind.Offer;
}

/// <summary>A server claims an offer.</summary>
internal sealed record ClaimFrame(long Serial, int Round) : PeerFrame
{
    internal override McsFrameKind Kind => McsFrameKind.Claim;
}

/// <summary>The requestor hands the message to the first claimant.</summary>
internal sealed record GrantFrame(long Serial, int Round, MessageBody Message) : PeerFrame
{
    internal override McsFrameKind Kind => McsFrameKind.Grant;
}

/// <summary>The requestor takes an offer back from a server that was not granted it.</summary>
internal sealed record WithdrawFrame(long Serial, int Round) : PeerFrame
{
    internal override McsFrameKind Kind => McsFrameKind.Withdraw;
}

/// <summary>The server has taken message <paramref name="Serial"/>; written before the server's RECEIVE returns.</summary>
internal sealed record AckFrame(long Serial) : PeerFrame
{
    internal override McsFrameKind Kind => McsFrameKind.Ack;
}

/// <summary>The server's answer to message <paramref name="Serial"/>, with the exception-name it raises (§0 Q6), or null.</summary>
internal sealed record ReplyFrame(long Serial, MessageBody Message, string? ExceptionName) : PeerFrame
{
    internal override McsFrameKind Kind => McsFrameKind.Reply;
}

/// <summary>
/// ⛔ THE ONE PAYLOAD CODEC OF THE 2023 FACILITY'S FRAMES (docs/rearchitecture/DESIGN-message-control.md design 5.3), little-endian over
/// <see cref="McsFrame"/>'s envelope. A message travels as three parts: its image as UTF-16 code units, so no encoding is chosen; its extent table
/// (a flag, then the component count and the <c>FixedAt</c> and <c>Lengths</c> arrays, which is what a file frame records of a
/// <see cref="RecordExtents"/>); and its slots, each a <see cref="MessageTag"/> as its four fields. A payload that is truncated, overlong, names an
/// unknown party or carries a negative count is <see cref="InvalidDataException"/>, which the caller answers by closing the link.
/// </summary>
internal static class PeerFrameCodec
{
    /// <summary>The protocol version a <see cref="HelloFrame"/> carries.</summary>
    internal const int ProtocolVersion = 1;

    /// <summary>The payload of <paramref name="frame"/>.</summary>
    internal static byte[] Encode(PeerFrame frame)
    {
        var w = new Writer();
        switch (frame)
        {
            case HelloFrame f: w.Int(f.Version); w.Text(f.Mcs); w.Text(f.RunId); w.Int(f.Pid); w.Text(f.ServedName); break;
            case OfferFrame f: w.Long(f.Serial); w.Int(f.Round); break;
            case ClaimFrame f: w.Long(f.Serial); w.Int(f.Round); break;
            case GrantFrame f: w.Long(f.Serial); w.Int(f.Round); w.Body(f.Message); break;
            case WithdrawFrame f: w.Long(f.Serial); w.Int(f.Round); break;
            case AckFrame f: w.Long(f.Serial); break;
            case ReplyFrame f: w.Long(f.Serial); w.Body(f.Message); w.NullableText(f.ExceptionName); break;
            default: throw new ArgumentException($"no payload codec for {frame.GetType().Name}", nameof(frame));
        }
        return w.ToArray();
    }

    /// <summary>The frame of kind <paramref name="kind"/> whose payload is <paramref name="payload"/>.</summary>
    /// <exception cref="InvalidDataException">The payload does not parse as that kind, or the kind is a bell's <see cref="McsFrameKind.Ring"/>.</exception>
    internal static PeerFrame Decode(McsFrameKind kind, byte[] payload)
    {
        var r = new Reader(payload);
        PeerFrame frame = kind switch
        {
            McsFrameKind.Hello => new HelloFrame(r.Int(), r.Text(), r.Text(), r.Int(), r.Text()),
            McsFrameKind.Offer => new OfferFrame(r.Long(), r.Int()),
            McsFrameKind.Claim => new ClaimFrame(r.Long(), r.Int()),
            McsFrameKind.Grant => new GrantFrame(r.Long(), r.Int(), r.Body()),
            McsFrameKind.Withdraw => new WithdrawFrame(r.Long(), r.Int()),
            McsFrameKind.Ack => new AckFrame(r.Long()),
            McsFrameKind.Reply => new ReplyFrame(r.Long(), r.Body(), r.NullableText()),
            _ => throw new InvalidDataException($"frame kind {kind} carries no protocol frame"),
        };
        r.End();
        return frame;
    }

    private sealed class Writer
    {
        private readonly MemoryStream _bytes = new();

        internal byte[] ToArray() => _bytes.ToArray();

        internal void Byte(byte value) => _bytes.WriteByte(value);

        internal void Int(int value)
        {
            Span<byte> b = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(b, value);
            _bytes.Write(b);
        }

        internal void Long(long value)
        {
            Span<byte> b = stackalloc byte[8];
            BinaryPrimitives.WriteInt64LittleEndian(b, value);
            _bytes.Write(b);
        }

        /// <summary>A string as its UTF-16 code-unit count and the units.</summary>
        internal void Text(string value)
        {
            Int(value.Length);
            var units = new byte[2 * value.Length];
            for (int i = 0; i < value.Length; i++) BinaryPrimitives.WriteUInt16LittleEndian(units.AsSpan(2 * i), value[i]);
            _bytes.Write(units);
        }

        internal void NullableText(string? value)
        {
            Byte(value is null ? (byte)0 : (byte)1);
            if (value is not null) Text(value);
        }

        internal void Body(MessageBody body)
        {
            Text(body.Image);
            if (body.Extents is { } extents)
            {
                Byte(1);
                Int(extents.Count);
                for (int k = 0; k < extents.Count; k++) Int(extents.FixedAt[k]);
                for (int k = 0; k < extents.Count; k++) Int(extents.Lengths[k]);
            }
            else Byte(0);
            Int(body.Slots.Length);
            foreach (object? slot in body.Slots)
            {
                if (slot is not MessageTag tag)
                    throw new ArgumentException($"a message slot is a message tag, not {slot?.GetType().Name ?? "null"}", nameof(body));
                NullableText(tag.Mcs);
                Byte((byte)tag.Party);
                NullableText(tag.Name);
                Long(tag.Message);
            }
        }
    }

    private sealed class Reader(byte[] payload)
    {
        private int _at;

        private ReadOnlySpan<byte> Take(long count)
        {
            if (count < 0 || count > payload.Length - _at) throw new InvalidDataException("the frame payload ends inside a field");
            var span = payload.AsSpan(_at, (int)count);
            _at += (int)count;
            return span;
        }

        internal byte Byte() => Take(1)[0];

        internal int Int() => BinaryPrimitives.ReadInt32LittleEndian(Take(4));

        internal long Long() => BinaryPrimitives.ReadInt64LittleEndian(Take(8));

        internal string Text()
        {
            int count = Int();
            if (count < 0) throw new InvalidDataException("a text field announces a negative length");
            var units = Take(2L * count);
            return string.Create(count, units.ToArray(), static (chars, bytes) =>
            {
                for (int i = 0; i < chars.Length; i++) chars[i] = (char)BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(2 * i));
            });
        }

        internal string? NullableText() => Flag() ? Text() : null;

        private bool Flag() => Byte() switch
        {
            0 => false,
            1 => true,
            var other => throw new InvalidDataException($"a flag byte is 0 or 1, not {other}"),
        };

        internal MessageBody Body()
        {
            string image = Text();
            RecordExtents? extents = null;
            if (Flag())
            {
                int count = Int();
                if (count < 0 || 8L * count > payload.Length - _at) throw new InvalidDataException("an extent table announces more components than the payload holds");
                var fixedAt = new int[count];
                var lengths = new int[count];
                for (int k = 0; k < count; k++) fixedAt[k] = Int();
                for (int k = 0; k < count; k++) lengths[k] = Int();
                try { extents = new RecordExtents(fixedAt, lengths); }
                catch (ArgumentException e) { throw new InvalidDataException(e.Message, e); }
            }
            int slotCount = Int();
            if (slotCount < 0 || slotCount > payload.Length - _at) throw new InvalidDataException("a message announces more slots than the payload holds");
            var slots = new object?[slotCount];
            for (int k = 0; k < slotCount; k++)
            {
                string? mcs = NullableText();
                byte party = Byte();
                if (party > (byte)MessageParty.Requestor) throw new InvalidDataException($"a message tag names party {party}, which is none");
                string? name = NullableText();
                slots[k] = new MessageTag(mcs!, (MessageParty)party, name!, Long());
            }
            return new MessageBody(image, extents, slots);
        }

        internal void End()
        {
            if (_at != payload.Length) throw new InvalidDataException("the frame payload carries bytes past its last field");
        }
    }
}
