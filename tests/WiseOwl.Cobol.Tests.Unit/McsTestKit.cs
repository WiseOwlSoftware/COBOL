// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;
using System.Text;
using WiseOwl.Cobol.Runtime.Communication;
using WiseOwl.Cobol.Runtime.IO.Store;
using WiseOwl.Cobol.Tests.Unit.Store;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>The case's wrapper of the engine's medium (<see cref="PageMediumObserver"/>, the one seam both clients of the engine open their store through): a real medium that
/// refuses the reads, writes and syncs of the page store while <see cref="Refusing"/>, and, with <see cref="RefuseWritesFrom"/>, refuses every write from the nth on while
/// reads and flushes answer, so a refusal can come at any page of one statement.</summary>
internal sealed class MediumFault : IDisposable
{
    private readonly Func<IPageMedium, IPageMedium>? _previous = PageMediumObserver.Value;
    private int _writes;
    private int _syncs;
    private bool _writeRefused;

    internal MediumFault() => PageMediumObserver.Value = medium =>
        new RefusingPageMedium(medium, write: _ => WriteRefusal(), sync: SyncRefusal, read: _ =>
        {
            OnRead?.Invoke();
            return Refusal() ?? (ReadsFailAfterARefusedWrite && _writeRefused ? new IOException("refused by the host") : null);
        });

    /// <summary>Every read, write and sync is refused while true.</summary>
    internal bool Refusing { get; set; }

    /// <summary>Called before every page read: what another run unit could see while a statement reads the store.</summary>
    internal Action? OnRead { get; set; }

    /// <summary>Once a write has been refused every read is too, so the reopen that decides a refused statement is refused as well.</summary>
    internal bool ReadsFailAfterARefusedWrite
    {
        get;
        set
        {
            field = value;
            _writeRefused = false;
        }
    }

    /// <summary>When set, the write with this ordinal (counted from the call that set it) and every later one is refused.</summary>
    internal int? RefuseWritesFrom
    {
        get;
        set
        {
            field = value;
            _writes = 0;
        }
    }

    /// <summary>When set, the sync with this ordinal (counted from the call that set it) and every later one is refused, while every read and write answers: a host whose flush
    /// fails alone.</summary>
    internal int? RefuseSyncsFrom
    {
        get;
        set
        {
            field = value;
            _syncs = 0;
        }
    }

    private IOException? SyncRefusal() => Refusing || RefuseSyncsFrom is { } from && _syncs++ >= from ? new IOException("refused by the host") : null;

    private IOException? Refusal() => Refusing ? new IOException("refused by the host") : null;

    private IOException? WriteRefusal()
    {
        if (!(Refusing || RefuseWritesFrom is { } from && _writes++ >= from)) return null;
        _writeRefused = true;
        return new IOException("refused by the host");
    }

    public void Dispose() => PageMediumObserver.Value = _previous;
}

/// <summary>What the message store holds, read through a READ-ONLY <see cref="PagedFile"/> over a handle of its own (the adoption rule of 2.2.1.6 R6, in memory): the rows by tag, and
/// the logical content a crash or a refusal must leave whole, with the devices beside it.</summary>
internal static class McsInspector
{
    /// <summary>Every row of the store, in key order.</summary>
    internal static List<(byte[] Key, byte[] Value)> Rows(string directory)
    {
        using var stream = new FileStream(Path.Combine(directory, "mcs.store"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        Assert.Equal(StoreStatus.Ok, PagedFile.Open(new HandlePageMedium(stream.SafeFileHandle), writable: false, coherent: true, KeyOrders.Ordinal, out var file,
            messageStore: true));
        var tree = new BTree(file!, KeyOrders.Ordinal, file!.Meta.PrimeRoot);
        var rows = new List<(byte[], byte[])>();
        Assert.Equal(StoreStatus.Ok, tree.First(out var cursor));
        while (cursor.HasCurrent)
        {
            Assert.Equal(StoreStatus.Ok, cursor.ReadValue(out var value));
            rows.Add((cursor.Key, value));
            Assert.Equal(StoreStatus.Ok, cursor.Next());
        }
        return rows;
    }

    /// <summary>The store's logical content, one line per row but the <c>Meta</c> row: the messages of a queue, terminal or destination as their text in first-in, first-out order
    /// (a serial and a date are no content), every other row as its key and value. Then each device file whole. Equal lines are equal states.</summary>
    internal static List<string> Content(string directory)
    {
        var lines = new List<string>();
        foreach (var (key, value) in Rows(directory))
        {
            char tag = (char)key[0];
            if (tag == 'C') continue;
            if (tag is 'M' or 'I' or 'O')
                lines.Add(tag + ":" + Convert.ToHexString(key[..^8]) + "=" + string.Concat(StoredMessage.Decode(value).Segments));
            else
                lines.Add(tag + ":" + Convert.ToHexString(key) + "=" + Convert.ToHexString(value));
        }
        // A device file of no bytes is no device content: a transmission writes at least a line end, so an empty file is a dead statement's creation, which the first statement of the next run unit cuts to its committed end, 0.
        foreach (string device in Directory.GetFiles(directory, "*.out").Order(StringComparer.Ordinal).Where(d => new FileInfo(d).Length > 0))
            lines.Add("device " + Path.GetFileName(device) + "=" + Encoding.UTF8.GetString(File.ReadAllBytes(device)));
        return lines;
    }

    /// <summary>⛔ NO MESSAGE LOST AND NONE DUPLICATED, THE COUNTS TRUE: every <c>QueueCounts</c> row equals a recount of the queue messages under its prefix, every <c>OutboundCounts</c>
    /// row equals a recount of the destination's outbound messages, and no count row stands without a message.</summary>
    internal static void AssertCountsEqualARecount(string directory)
    {
        var rows = Rows(directory);
        var queueCounts = new Dictionary<string, ulong>();
        var outboundCounts = new Dictionary<string, ulong>();
        foreach (var (key, _) in rows)
        {
            if (key[0] == 'M')
            {
                int end = Array.IndexOf(key, (byte)0, 1);
                var levels = Encoding.UTF8.GetString(key, 1, end - 1).Split((char)1, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 1; i <= levels.Length; i++)
                {
                    string prefix = string.Join('/', levels[..i]);
                    queueCounts[prefix] = queueCounts.GetValueOrDefault(prefix) + 1;
                }
            }
            else if (key[0] == 'O')
            {
                string destination = Encoding.UTF8.GetString(key, 1, Array.IndexOf(key, (byte)0, 1) - 1);
                outboundCounts[destination] = outboundCounts.GetValueOrDefault(destination) + 1;
            }
        }
        var storedQueues = rows.Where(r => r.Key[0] == 'N')
            .ToDictionary(r => string.Join('/', Encoding.UTF8.GetString(r.Key, 1, r.Key.Length - 1).Split((char)1, StringSplitOptions.RemoveEmptyEntries)),
                r => BinaryPrimitives.ReadUInt64BigEndian(r.Value));
        var storedOutbound = rows.Where(r => r.Key[0] == 'P')
            .ToDictionary(r => Encoding.UTF8.GetString(r.Key, 1, r.Key.Length - 1), r => BinaryPrimitives.ReadUInt64BigEndian(r.Value));
        Assert.Equal(queueCounts.OrderBy(p => p.Key, StringComparer.Ordinal), storedQueues.OrderBy(p => p.Key, StringComparer.Ordinal));
        Assert.Equal(outboundCounts.OrderBy(p => p.Key, StringComparer.Ordinal), storedOutbound.OrderBy(p => p.Key, StringComparer.Ordinal));
    }
}
