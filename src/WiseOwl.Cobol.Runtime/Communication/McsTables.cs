// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;
using System.Text;
using WiseOwl.Cobol.Runtime.IO.Store;

namespace WiseOwl.Cobol.Runtime.Communication;

/// <summary>The paged store refused a read or a write of a statement's rows (a torn page, a refused read, a store the statement cannot land), or a row it holds is not
/// one this MCS wrote. An <see cref="IOException"/>, so <see cref="MessageStore"/> answers it as it answers a host refusal: status 92, or 93 when the statement may have landed.</summary>
internal sealed class McsRefusalException(string message) : IOException(message);

/// <summary>What an endpoint of the MCS is, the second byte of its <c>Endpoints</c> row's key.</summary>
internal enum EndpointKind : byte
{
    Queue = (byte)'Q',
    Source = (byte)'S',
    Destination = (byte)'D',
    Terminal = (byte)'T',
}

/// <summary>An <c>Endpoints</c> row: whether the endpoint is enabled, and what the definitions last said of it (docs/rearchitecture/DESIGN-communication-facility.md §6 "The store").</summary>
internal sealed record EndpointRow(bool Enabled, string? Key, int? Capacity, string? DeliverTo, string? Source)
{
    internal byte[] Encode()
    {
        using var buffer = new MemoryStream();
        using (var w = new BinaryWriter(buffer, Encoding.UTF8, leaveOpen: true))
        {
            w.Write(Enabled);
            WriteOptional(w, Key);
            w.Write(Capacity ?? -1);
            WriteOptional(w, DeliverTo);
            WriteOptional(w, Source);
        }
        return buffer.ToArray();
    }

    internal static EndpointRow Decode(byte[] value)
    {
        using var r = new BinaryReader(new MemoryStream(value), Encoding.UTF8);
        bool enabled = r.ReadBoolean();
        string? key = ReadOptional(r);
        int capacity = r.ReadInt32();
        return new EndpointRow(enabled, key, capacity < 0 ? null : capacity, ReadOptional(r), ReadOptional(r));
    }

    private static void WriteOptional(BinaryWriter w, string? text)
    {
        w.Write(text is not null);
        if (text is not null) w.Write(text);
    }

    private static string? ReadOptional(BinaryReader r) => r.ReadBoolean() ? r.ReadString() : null;
}

/// <summary>The <c>Meta</c> row: the store's identity, the next message serial, the SHA-256 of the <c>mcs.json</c> the store was seeded from, and the three bell counters
/// (docs/rearchitecture/DESIGN-communication-facility.md §6 "The store"; the counters are the bell's, §9 item 4).</summary>
internal sealed record McsMeta(byte[] Identity, ulong NextSerial, byte[] DefinitionsHash, ulong BackstopFires, ulong BackstopFinds, ulong BellUnavailable)
{
    private const int IdentityBytes = 16, HashBytes = 32, Size = IdentityBytes + sizeof(ulong) + HashBytes + 3 * sizeof(ulong);

    internal byte[] Encode()
    {
        var value = new byte[Size];
        Identity.CopyTo(value, 0);
        BinaryPrimitives.WriteUInt64BigEndian(value.AsSpan(IdentityBytes), NextSerial);
        DefinitionsHash.CopyTo(value, IdentityBytes + sizeof(ulong));
        int counters = IdentityBytes + sizeof(ulong) + HashBytes;
        BinaryPrimitives.WriteUInt64BigEndian(value.AsSpan(counters), BackstopFires);
        BinaryPrimitives.WriteUInt64BigEndian(value.AsSpan(counters + 8), BackstopFinds);
        BinaryPrimitives.WriteUInt64BigEndian(value.AsSpan(counters + 16), BellUnavailable);
        return value;
    }

    internal static McsMeta Decode(byte[] value)
    {
        if (value.Length != Size) throw new McsRefusalException("the store's Meta row is not the size this MCS writes");
        int counters = IdentityBytes + sizeof(ulong) + HashBytes;
        return new McsMeta(value[..IdentityBytes], BinaryPrimitives.ReadUInt64BigEndian(value.AsSpan(IdentityBytes)),
            value[(IdentityBytes + sizeof(ulong))..counters], BinaryPrimitives.ReadUInt64BigEndian(value.AsSpan(counters)),
            BinaryPrimitives.ReadUInt64BigEndian(value.AsSpan(counters + 8)), BinaryPrimitives.ReadUInt64BigEndian(value.AsSpan(counters + 16)));
    }
}

/// <summary>
/// ⛔ THE MCS'S LOGICAL TABLES over the one B+ tree of the paged store (docs/rearchitecture/DESIGN-communication-facility.md §6 "The store"): the first key byte is a tag
/// and each tag is one table. Keys are big-endian, names are UTF-8, a queue path is its level names each followed by the byte 0x01, and a serial is the 8-byte big-endian
/// value of <see cref="Meta"/>'s next serial. A node's own messages (path, 0x00, serial) sort before its children's (path, a name, 0x01, ...), children sort by name and
/// serials first in, first out, so the next message of a designated structure is the first <c>QueueMessages</c> key at or after the structure's prefix: one seek
/// (E.5.5.3 and E.5.5.4 are the tree's own order). The tree is edited in memory for one statement; <see cref="StatementLanding.Complete"/> lands it.
/// Every read or write the store refuses is an <see cref="McsRefusalException"/>.
/// </summary>
internal sealed class McsTables(BTree tree)
{
    private const byte MetaTag = (byte)'C', EndpointsTag = (byte)'E', QueueMessagesTag = (byte)'M', TerminalMessagesTag = (byte)'I',
        QueueCountsTag = (byte)'N', OutboundTag = (byte)'O', OutboundCountsTag = (byte)'P', DeviceEndsTag = (byte)'D';
    private const byte PathEnd = 0x01, OwnerEnd = 0x00;

    private McsMeta? _meta;
    private bool _metaLoaded, _metaChanged;

    /// <summary>The tree this statement edits.</summary>
    internal BTree Tree => tree;

    /// <summary>The <c>Meta</c> row, or null in a store no statement has seeded.</summary>
    internal McsMeta? Meta
    {
        get
        {
            if (!_metaLoaded)
            {
                _meta = Get([MetaTag]) is { } value ? McsMeta.Decode(value) : null;
                _metaLoaded = true;
            }
            return _meta;
        }
        set
        {
            _meta = value;
            _metaLoaded = true;
            _metaChanged = true;
        }
    }

    /// <summary>Write the <c>Meta</c> row when a statement changed it (a message serial was drawn, the seeding moved the hash).</summary>
    internal void FlushMeta()
    {
        if (_metaChanged && _meta is { } meta) Put([MetaTag], meta.Encode());
        _metaChanged = false;
    }

    // ── The tree ───────────────────────────────────────────────────────────────────────────────────────────────

    private static void Check(StoreStatus status)
    {
        if (status != StoreStatus.Ok) throw new McsRefusalException($"the store refused a statement's rows ({status})");
    }

    private byte[]? Get(byte[] key)
    {
        Check(tree.TryGet(key, out var value));
        return value;
    }

    private void Put(byte[] key, byte[] value) => Check(tree.Put(key, value));

    private void Delete(byte[] key) => Check(tree.Delete(key, out _));

    /// <summary>Every row whose key starts with <paramref name="prefix"/>, in key order, read before the caller changes any.</summary>
    private List<(byte[] Key, byte[] Value)> Scan(byte[] prefix, int limit = int.MaxValue)
    {
        var rows = new List<(byte[], byte[])>();
        Check(tree.Seek(prefix, SeekRelation.GreaterOrEqual, out var cursor));
        while (rows.Count < limit && cursor.HasCurrent && cursor.Key.AsSpan().StartsWith(prefix))
        {
            byte[] key = cursor.Key;
            Check(cursor.ReadValue(out var value));
            rows.Add((key, value));
            Check(cursor.Next());
        }
        return rows;
    }

    // ── Keys ───────────────────────────────────────────────────────────────────────────────────────────────────

    private static byte[] Join(params ReadOnlySpan<byte[]> parts)
    {
        var key = new byte[parts.ToArray().Sum(p => p.Length)];
        int at = 0;
        foreach (var part in parts)
        {
            part.CopyTo(key, at);
            at += part.Length;
        }
        return key;
    }

    /// <summary>A queue path: each level name followed by 0x01.</summary>
    internal static byte[] PathBytes(string[] path) => Join([.. path.Select(level => Join(Encoding.UTF8.GetBytes(level), [PathEnd]))]);

    private static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);

    private static byte[] Serial(ulong serial)
    {
        var bytes = new byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64BigEndian(bytes, serial);
        return bytes;
    }

    private static byte[] OwnerPrefix(byte tag, byte[] owner) => Join([tag], owner, [OwnerEnd]);

    private static byte[] EndpointKey(EndpointKind kind, byte[] body) => Join([EndpointsTag, (byte)kind], body);

    private static byte[] EndpointBody(EndpointKind kind, string[] path) => kind is EndpointKind.Queue ? PathBytes(path) : Utf8(path[0]);

    private static ulong ReadCount(byte[] value) => BinaryPrimitives.ReadUInt64BigEndian(value);

    /// <summary>The path a <c>QueueMessages</c> key names: the level names between the tag and the 0x00.</summary>
    private static string[] PathOf(byte[] messageKey)
    {
        int end = Array.IndexOf(messageKey, OwnerEnd, 1);
        if (end < 0) throw new McsRefusalException("a QueueMessages key holds no path end");
        return Encoding.UTF8.GetString(messageKey, 1, end - 1).Split((char)PathEnd, StringSplitOptions.RemoveEmptyEntries);
    }

    // ── Endpoints ──────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The <c>Endpoints</c> row of <paramref name="kind"/> at <paramref name="path"/> (a queue's levels, else the name alone), or null.</summary>
    internal EndpointRow? Endpoint(EndpointKind kind, string[] path) =>
        Get(EndpointKey(kind, EndpointBody(kind, path))) is { } value ? EndpointRow.Decode(value) : null;

    internal void PutEndpoint(EndpointKind kind, string[] path, EndpointRow row) => Put(EndpointKey(kind, EndpointBody(kind, path)), row.Encode());

    /// <summary>Every <c>Endpoints</c> row's key, for the seeding to compare with the definitions.</summary>
    internal List<byte[]> EndpointKeys() => [.. Scan([EndpointsTag]).Select(row => row.Key)];

    /// <summary>The key of an endpoint's row, as <see cref="EndpointKeys"/> lists them.</summary>
    internal static byte[] KeyOfEndpoint(EndpointKind kind, string[] path) => EndpointKey(kind, EndpointBody(kind, path));

    /// <summary>Delete the endpoint whose row is <paramref name="endpointKey"/> with everything the store holds for it: a queue's messages and its descendants', a destination's
    /// outbound messages and count, a terminal's messages. The queue counts are recounted by the seeding, which is the only caller.</summary>
    internal void DeleteEndpoint(byte[] endpointKey)
    {
        var kind = (EndpointKind)endpointKey[1];
        byte[] body = endpointKey[2..];
        switch (kind)
        {
            case EndpointKind.Queue:
                foreach (var (key, _) in Scan(Join([QueueMessagesTag], body))) Delete(key);
                break;
            case EndpointKind.Destination:
                foreach (var (key, _) in Scan(OwnerPrefix(OutboundTag, body))) Delete(key);
                Delete(Join([OutboundCountsTag], body));
                break;
            case EndpointKind.Terminal:
                foreach (var (key, _) in Scan(OwnerPrefix(TerminalMessagesTag, body))) Delete(key);
                break;
        }
        Delete(endpointKey);
    }

    // ── Serials and queue messages ─────────────────────────────────────────────────────────────────────────────

    private ulong DrawSerial()
    {
        var meta = Meta ?? throw new McsRefusalException("a message is added to a store no statement has seeded");
        Meta = meta with { NextSerial = meta.NextSerial + 1 };
        return meta.NextSerial;
    }

    /// <summary>Add a complete message to the queue at <paramref name="path"/>: its row, and one count for every prefix of the path.</summary>
    internal void AddQueueMessage(string[] path, StoredMessage message)
    {
        Put(Join([QueueMessagesTag], PathBytes(path), [OwnerEnd], Serial(DrawSerial())), message.Encode());
        AdjustCounts(path, +1);
    }

    /// <summary>The next message under <paramref name="path"/> in E.5.5.4's order, removed, with the level it was found at; null when the structure holds none.</summary>
    internal (string[] Path, StoredMessage Message)? TakeQueueMessage(string[] path)
    {
        var first = Scan(Join([QueueMessagesTag], PathBytes(path)), limit: 1);
        if (first.Count == 0) return null;
        var (key, value) = first[0];
        string[] level = PathOf(key);
        var message = StoredMessage.Decode(value);
        Delete(key);
        AdjustCounts(level, -1);
        return (level, message);
    }

    /// <summary>The complete messages under <paramref name="path"/> (E.5.5: "Interrogation of the queue depth ... reflects only the number of complete messages"): one row.</summary>
    internal ulong CompleteMessages(string[] path) => Get(Join([QueueCountsTag], PathBytes(path))) is { } value ? ReadCount(value) : 0;

    private void AdjustCounts(string[] path, int delta)
    {
        for (int levels = 1; levels <= path.Length; levels++)
        {
            byte[] key = Join([QueueCountsTag], PathBytes(path[..levels]));
            long count = (Get(key) is { } value ? (long)ReadCount(value) : 0) + delta;
            if (count <= 0) Delete(key);
            else Put(key, Serial((ulong)count));
        }
    }

    /// <summary>Rebuild every <c>QueueCounts</c> row from the messages: the seeding's, which changes messages by the endpoint.</summary>
    internal void RecountQueues()
    {
        foreach (var (key, _) in Scan([QueueCountsTag])) Delete(key);
        var counts = new SortedDictionary<string, (string[] Path, ulong Count)>(StringComparer.Ordinal);
        foreach (var (key, _) in Scan([QueueMessagesTag]))
        {
            string[] path = PathOf(key);
            for (int levels = 1; levels <= path.Length; levels++)
            {
                string[] prefix = path[..levels];
                string name = string.Join('/', prefix);
                counts[name] = (prefix, counts.TryGetValue(name, out var seen) ? seen.Count + 1 : 1);
            }
        }
        foreach (var (path, count) in counts.Values) Put(Join([QueueCountsTag], PathBytes(path)), Serial(count));
    }

    // ── Terminals' messages ────────────────────────────────────────────────────────────────────────────────────

    internal void AddTerminalMessage(string terminal, StoredMessage message) =>
        Put(Join(OwnerPrefix(TerminalMessagesTag, Utf8(terminal)), Serial(DrawSerial())), message.Encode());

    /// <summary>The oldest message <paramref name="terminal"/> has sent, removed; null when it holds none.</summary>
    internal StoredMessage? TakeTerminalMessage(string terminal)
    {
        var first = Scan(OwnerPrefix(TerminalMessagesTag, Utf8(terminal)), limit: 1);
        if (first.Count == 0) return null;
        Delete(first[0].Key);
        return StoredMessage.Decode(first[0].Value);
    }

    // ── Outbound messages ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The messages <paramref name="destination"/> holds while its path is closed, for the capacity check (Table A.2 error key 6): one row.</summary>
    internal ulong OutboundCount(string destination) => Get(Join([OutboundCountsTag], Utf8(destination))) is { } value ? ReadCount(value) : 0;

    internal void AddOutbound(string destination, StoredMessage message)
    {
        ulong count = OutboundCount(destination);
        Put(Join(OwnerPrefix(OutboundTag, Utf8(destination)), Serial(DrawSerial())), message.Encode());
        Put(Join([OutboundCountsTag], Utf8(destination)), Serial(count + 1));
    }

    /// <summary>Every message <paramref name="destination"/> holds, oldest first, removed with the count.</summary>
    internal List<StoredMessage> TakeOutbound(string destination)
    {
        var rows = Scan(OwnerPrefix(OutboundTag, Utf8(destination)));
        foreach (var (key, _) in rows) Delete(key);
        Delete(Join([OutboundCountsTag], Utf8(destination)));
        return [.. rows.Select(row => StoredMessage.Decode(row.Value))];
    }

    // ── Devices ────────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The committed byte length of the device file <c>&lt;device&gt;.out</c>; 0 for a device the store has never transmitted to.</summary>
    internal long DeviceEnd(string device) => Get(Join([DeviceEndsTag], Utf8(device))) is { } value ? (long)ReadCount(value) : 0;

    internal void SetDeviceEnd(string device, long end) => Put(Join([DeviceEndsTag], Utf8(device)), Serial((ulong)end));

    /// <summary>Every device the store has transmitted to, with the length of its committed output.</summary>
    internal List<(string Device, long End)> DeviceEnds() =>
        [.. Scan([DeviceEndsTag]).Select(row => (Encoding.UTF8.GetString(row.Key, 1, row.Key.Length - 1), (long)ReadCount(row.Value)))];
}
