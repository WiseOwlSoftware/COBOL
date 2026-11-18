// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// What one run unit has imaged: the inverse of the slot-image tokens <see cref="PointerImage"/> and
/// <see cref="MessageTagImage"/> render (kb/Work PB4838). A file record carries a pointer or message-tag leaf as its 8-position
/// token (docs/CONFORMANCE.md §7 DOC-A.1-216, DOC-A.1-212) and READ, RETURN and SORT GIVING restore the reference the token names
/// — which only the run unit that rendered it can do, so the table lives on <see cref="RunUnit"/> and a token another
/// run unit rendered is not in it. Both token spaces start at a random per-process epoch (<see cref="PointerImage.Epoch"/>),
/// so a token another PROCESS wrote collides with one of this run unit's only by a chance of about n/2^30 (review R1-1).
/// <para>⛔ EVERY ENTRY IS HELD STRONGLY UNTIL THE STORAGE IT NAMES ENDS (kb/Work PB5928). A token on a file is the program still holding the
/// reference: READ must restore it for as long as the thing it names lives, so no entry is ever weak. Each is a <see cref="PointerImage.ImagedEntry"/>,
/// and one whose life has ENDED (a cell life at FREE or CANCEL) is dropped by a sweep that runs each time the table has doubled, so a long batch that
/// images a million ALLOCATEd and FREEd cells retains none of them (review R1-2). An entry that names no storage life (a program name, an addressless
/// identity such as a saved-locale handle) lives as long as the run unit, which owns it. Held weakly, as a far pointer's and an identity's once were,
/// a collection between the WRITE and the READ dropped the entry while the thing it named was alive, and the token read back foreign.</para>
/// </summary>
internal sealed class SlotImageTable
{
    private const int FirstSweepAt = 1024;
    private const int SerialBits = 8;                        // the message-serial bits a token carries itself (kb/Work PB4926)
    private const long SerialMask = (1L << SerialBits) - 1;
    private static long s_lastTagBlock;   // PROCESS-wide, like the pointer bases: two run units of one process never issue the same tag block (review R2-1)

    private readonly object _gate = new();
    private readonly Dictionary<long, PointerImage.ImagedEntry> _bases = [];
    private readonly Dictionary<(string? Mcs, MessageParty Party, string? Name, long High), long> _blockIds = [];   // (party, serial >> SerialBits) -> block token
    private readonly Dictionary<long, (string? Mcs, MessageParty Party, string? Name, long High)> _blocks = [];
    private int _sweepAt = FirstSweepAt;

    /// <summary>Record that <paramref name="entry"/> was imaged at <paramref name="baseAddress"/>, held until the storage it names ends.</summary>
    internal void Note(long baseAddress, PointerImage.ImagedEntry entry)
    {
        lock (_gate)
        {
            if (_bases.ContainsKey(baseAddress)) return;
            if (_bases.Count >= _sweepAt) Sweep();
            _bases[baseAddress] = entry;
        }
    }

    /// <summary>The entry imaged at <paramref name="baseAddress"/>, or null when this run unit never imaged one (or it has ended and been swept).</summary>
    internal PointerImage.ImagedEntry? Entry(long baseAddress)
    {
        lock (_gate) return _bases.GetValueOrDefault(baseAddress);
    }

    /// <summary>Drop the entries whose storage life ended, then set the next sweep at twice what is left.</summary>
    private void Sweep()
    {
        foreach (var (key, entry) in _bases.ToArray())
        {
            if (entry.Ended) _bases.Remove(key);
        }
        _sweepAt = Math.Max(FirstSweepAt, _bases.Count * 2);
    }

    /// <summary>The identifier of <paramref name="tag"/> in this run unit: the process epoch in the high half, and in the low half a block number
    /// (a process-wide serial, so no two run units of a process share one) above the low <see cref="SerialBits"/> bits of the message serial number.
    /// The message serial number rides IN the token, because the tag read back must reply to the message it was sent for; the table holds
    /// one entry per (party, serial block), never one per message, so a long SEND/RECEIVE batch grows it by one entry per
    /// 2^<see cref="SerialBits"/> messages of a party (kb/Work PB4926).</summary>
    internal long TagId(MessageTag tag)
    {
        long high = tag.Message >> SerialBits;
        var key = (tag.Mcs, tag.Party, tag.Name, high);
        lock (_gate)
        {
            if (!_blockIds.TryGetValue(key, out long block))
            {
                long next = Interlocked.Increment(ref s_lastTagBlock);
                if (next >= 1L << (32 - SerialBits)) throw new InvalidOperationException("the process has issued every message-tag block number (2^24 - 1, one per party per 256 messages) it can image");
                block = (PointerImage.Epoch << 32) | (next << SerialBits);
                _blockIds[key] = block;
                _blocks[block] = key;
            }
            return block | (tag.Message & SerialMask);
        }
    }

    /// <summary>The tag identified by <paramref name="id"/> in this run unit, or null when it never assigned that identifier.</summary>
    internal MessageTag? TagOf(long id)
    {
        lock (_gate)
        {
            if (!_blocks.TryGetValue(id & ~SerialMask, out var key)) return null;
            return new MessageTag(key.Mcs!, key.Party, key.Name!, (key.High << SerialBits) | (id & SerialMask));
        }
    }

    /// <summary>How many (party, serial block) entries the run unit holds; a test reads it to pin that imaging N messages leaves it bounded.</summary>
    internal int TagBlockCount
    {
        get { lock (_gate) return _blocks.Count; }
    }
}
