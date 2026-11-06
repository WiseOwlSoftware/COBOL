// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

namespace WiseOwl.Cobol.Runtime.Communication;

/// <summary>One complete message (E.5.5: "The message control system logically places in queues or removes from queues only complete
/// messages"): its segments, whether an EGI ended it, and when the MCS "recognized that the message was complete" (A.1.2.3 GR8, GR9).
/// The store holds it in the binary codec of <see cref="Encode"/> and <see cref="Decode"/> (docs/rearchitecture/DESIGN-communication-facility.md §6 "The store").</summary>
internal sealed record StoredMessage(IReadOnlyList<string> Segments, bool EndOfGroup, string Date, string Time, string Source)
{
    internal static StoredMessage Complete(IReadOnlyList<string> segments, bool endOfGroup, DateTimeOffset now, string source = "") =>
        new(segments, endOfGroup,
            now.ToString("yyMMdd", CultureInfo.InvariantCulture),
            now.ToString("HHmmss", CultureInfo.InvariantCulture) + (now.Millisecond / 10).ToString("D2", CultureInfo.InvariantCulture),
            source);

    internal StoredMessage From(string source) => this with { Source = source };

    /// <summary>A definition's message: a bare string is one segment.</summary>
    internal static StoredMessage Parse(JsonNode node, DateTimeOffset now)
    {
        if (node is JsonValue) return Complete([node.GetValue<string>()], endOfGroup: false, now);
        var o = node.AsObject();
        var segments = MessageJson.Array(o, "segments").Select(MessageJson.Text).ToArray();
        if (segments.Length == 0) throw new FormatException("a message has at least one segment");
        var made = Complete(segments, o["group"]?.GetValue<bool>() ?? false, now, o["source"]?.GetValue<string>() ?? "");
        return made with { Date = o["date"]?.GetValue<string>() ?? made.Date, Time = o["time"]?.GetValue<string>() ?? made.Time };
    }

    /// <summary>The store's value: the segment count, each segment, the end-of-group flag, the date, the time and the source, as <see cref="BinaryWriter"/> writes them
    /// (strings as a 7-bit length and UTF-8).</summary>
    internal byte[] Encode()
    {
        using var buffer = new MemoryStream();
        using (var w = new BinaryWriter(buffer, Encoding.UTF8, leaveOpen: true))
        {
            w.Write7BitEncodedInt(Segments.Count);
            foreach (string segment in Segments) w.Write(segment);
            w.Write(EndOfGroup);
            w.Write(Date);
            w.Write(Time);
            w.Write(Source);
        }
        return buffer.ToArray();
    }

    /// <summary>The message <see cref="Encode"/> wrote. A value that is short or malformed is a <see cref="FormatException"/> (or the reader's <see cref="EndOfStreamException"/>, an
    /// <see cref="IOException"/>), which the store answers with status 92.</summary>
    internal static StoredMessage Decode(byte[] value)
    {
        using var r = new BinaryReader(new MemoryStream(value), Encoding.UTF8);
        int count = r.Read7BitEncodedInt();
        if (count is < 1 or > ushort.MaxValue) throw new FormatException("a stored message has at least one segment");
        var segments = new string[count];
        for (int i = 0; i < count; i++) segments[i] = r.ReadString();
        return new StoredMessage(segments, r.ReadBoolean(), r.ReadString(), r.ReadString(), r.ReadString());
    }
}
