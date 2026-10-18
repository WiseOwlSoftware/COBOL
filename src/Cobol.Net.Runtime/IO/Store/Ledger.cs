// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;

namespace CobolNet.Runtime.IO.Store;

/// <summary>The state of a page the LEDGER lists (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.6 R2).</summary>
internal enum LedgerState : byte
{
    /// <summary>Allocated since the committed generation and referenced by the work tree.</summary>
    Fresh = 1,

    /// <summary>Allocated since the committed generation and no longer referenced: reusable by any LATER statement
    /// than the one that released it.</summary>
    Released = 2,

    /// <summary>A committed page released since the committed generation: QUARANTINED until the promotion that makes
    /// its freeing durable (R4), because the committed slot still references it.</summary>
    Freed = 3,
}

/// <summary>One page's ledger entry: the generation of its last write and its state.</summary>
internal readonly record struct LedgerEntry(ulong Generation, LedgerState State);

/// <summary>The GENERATION LEDGER (§2.2.1.6 R2): every page allocated since the committed generation (the work meta's
/// base), with the generation of its last write and its <see cref="LedgerState"/>, plus the committed pages released
/// since (<see cref="LedgerState.Freed"/>). It does not list its own pages: they are verified through their stamped
/// pointers when it is read. The in-memory map; <see cref="Encode"/> / <see cref="Decode"/> are its interim page
/// serialization (<see cref="PageChain"/>), which kb/Work PB3332's tree replaces.</summary>
internal sealed class Ledger
{
    private const int EntryBytes = 13;   // page (4) + generation (8) + state (1)

    private readonly SortedDictionary<uint, LedgerEntry> _entries;

    /// <summary>An empty ledger: the one a retirement starts from.</summary>
    public Ledger() => _entries = [];

    private Ledger(SortedDictionary<uint, LedgerEntry> entries) => _entries = entries;

    /// <summary>The entries, by page.</summary>
    public IReadOnlyDictionary<uint, LedgerEntry> Entries => _entries;

    /// <summary>The number of pages listed (R5's checkpoint compares it with <see cref="PagedFile.LedgerCheckpoint"/>).</summary>
    public int Count => _entries.Count;

    /// <summary>The pages in state <paramref name="state"/>, ascending.</summary>
    public IEnumerable<uint> Pages(LedgerState state) =>
        _entries.Where(e => e.Value.State == state).Select(e => e.Key);

    /// <summary>Record <paramref name="page"/>.</summary>
    public void Set(uint page, ulong generation, LedgerState state) => _entries[page] = new(generation, state);

    /// <summary>The entry of <paramref name="page"/>, if listed.</summary>
    public bool TryGet(uint page, out LedgerEntry entry) => _entries.TryGetValue(page, out entry);

    /// <summary>A copy a statement works on, so a refused statement leaves the store's ledger as it was.</summary>
    public Ledger Clone() => new(new SortedDictionary<uint, LedgerEntry>(_entries));

    /// <summary>The bytes <paramref name="entries"/> entries occupy in the serialization.</summary>
    public static long BytesFor(long entries) => EntryBytes * entries;

    /// <summary>The interim serialization: each entry as page, generation and state, ascending by page.</summary>
    public byte[] Encode()
    {
        var bytes = new byte[EntryBytes * _entries.Count];
        int at = 0;
        foreach (var (page, entry) in _entries)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at), page);
            BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(at + 4), entry.Generation);
            bytes[at + 12] = (byte)entry.State;
            at += EntryBytes;
        }
        return bytes;
    }

    /// <summary>The inverse of <see cref="Encode"/>; null for a payload that is not one.</summary>
    public static Ledger? Decode(ReadOnlySpan<byte> payload)
    {
        if (payload.Length % EntryBytes != 0) return null;
        var entries = new SortedDictionary<uint, LedgerEntry>();
        for (int at = 0; at < payload.Length; at += EntryBytes)
        {
            var state = (LedgerState)payload[at + 12];
            if (state is not (LedgerState.Fresh or LedgerState.Released or LedgerState.Freed)) return null;
            if (!entries.TryAdd(BinaryPrimitives.ReadUInt32LittleEndian(payload[at..]),
                    new LedgerEntry(BinaryPrimitives.ReadUInt64LittleEndian(payload[(at + 4)..]), state)))
                return null;
        }
        return new Ledger(entries);
    }
}
