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
/// <para>The table must not keep storage alive that the program has let go: an entry for a cell life that has ENDED (FREE, CANCEL)
/// is dropped by a sweep that runs each time the table has doubled, and an addressless pointer's identity is held weakly, so
/// a long batch that images a million ALLOCATEd and FREEd cells retains none of them (review R1-2).</para>
/// </summary>
internal sealed class SlotImageTable
{
    private const int FirstSweepAt = 1024;

    private readonly object _gate = new();
    private readonly Dictionary<long, object> _bases = [];   // CellLife and NamedAddress held strongly, an identity as a WeakReference
    private readonly Dictionary<(string? Mcs, MessageParty Party, string? Name, long Message), long> _tagIds = [];
    private readonly Dictionary<long, MessageTag> _tags = [];
    private int _sweepAt = FirstSweepAt;

    /// <summary>Record that <paramref name="entry"/> (a <c>PointerImage.CellLife</c>, a <c>PointerImage.NamedAddress</c> or the
    /// identity of an addressless pointer) was imaged at <paramref name="baseAddress"/>.</summary>
    internal void Note(long baseAddress, object entry)
    {
        lock (_gate)
        {
            if (_bases.TryGetValue(baseAddress, out var held) && (held is not WeakReference { IsAlive: false })) return;
            if (_bases.Count >= _sweepAt) Sweep();
            _bases[baseAddress] = entry is PointerImage.CellLife or PointerImage.NamedAddress ? entry : new WeakReference(entry);
        }
    }

    /// <summary>The entry imaged at <paramref name="baseAddress"/>, or null when this run unit never imaged one (or it has ended).</summary>
    internal object? Entry(long baseAddress)
    {
        lock (_gate)
        {
            object? entry = _bases.GetValueOrDefault(baseAddress);
            return entry is WeakReference weak ? weak.Target : entry;
        }
    }

    /// <summary>Drop the entries whose storage life ended or whose identity was collected, then set the next sweep at twice what is left.</summary>
    private void Sweep()
    {
        foreach (var (key, entry) in _bases.ToArray())
        {
            if (entry is PointerImage.CellLife life ? life.Generation != life.Cell.Generation
                : entry is WeakReference { IsAlive: false })
                _bases.Remove(key);
        }
        _sweepAt = Math.Max(FirstSweepAt, _bases.Count * 2);
    }

    /// <summary>The identifier of <paramref name="tag"/> in this run unit, assigned on first use (the process epoch in the high half,
    /// 1, 2, 3 ... in the low); the message serial number is part of the key, because the tag read back must reply to the message it
    /// was sent for.</summary>
    internal long TagId(MessageTag tag)
    {
        var key = (tag.Mcs, tag.Party, tag.Name, tag.Message);
        lock (_gate)
        {
            if (!_tagIds.TryGetValue(key, out long id))
            {
                id = (PointerImage.Epoch << 32) | (uint)(_tagIds.Count + 1);
                _tagIds[key] = id;
                _tags[id] = tag;
            }
            return id;
        }
    }

    /// <summary>The tag identified by <paramref name="id"/> in this run unit, or null when it never assigned that identifier.</summary>
    internal MessageTag? TagOf(long id)
    {
        lock (_gate) return _tags.TryGetValue(id, out var tag) ? tag : null;
    }
}
