// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// What one run unit has imaged: the inverse of the slot-image tokens <see cref="PointerImage"/> and
/// <see cref="MessageTagImage"/> render (kb/Work PB4838). A file record carries a pointer or message-tag leaf as its 8-position
/// token (docs/CONFORMANCE.md §7 DOC-A.1-216, DOC-A.1-212) and READ, RETURN and SORT GIVING restore the reference the token names
/// — which only the run unit that rendered it can do, so the table lives on <see cref="RunUnit"/> and a token another
/// run unit rendered is not in it. The bases themselves are allocated per PROCESS (<see cref="PointerImage"/>), so such a
/// token can never collide with one of this run unit's.
/// </summary>
internal sealed class SlotImageTable
{
    private readonly object _gate = new();
    private readonly Dictionary<long, object> _bases = [];
    private readonly Dictionary<(string? Mcs, MessageParty Party, string? Name, long Message), long> _tagIds = [];
    private readonly Dictionary<long, MessageTag> _tags = [];

    /// <summary>Record that <paramref name="entry"/> (a <c>PointerImage.CellLife</c>, a <c>PointerImage.NamedAddress</c> or the
    /// identity of an addressless pointer) was imaged at <paramref name="baseAddress"/>.</summary>
    internal void Note(long baseAddress, object entry)
    {
        lock (_gate) _bases.TryAdd(baseAddress, entry);
    }

    /// <summary>The entry imaged at <paramref name="baseAddress"/>, or null when this run unit never imaged one.</summary>
    internal object? Entry(long baseAddress)
    {
        lock (_gate) return _bases.GetValueOrDefault(baseAddress);
    }

    /// <summary>The identifier of <paramref name="tag"/> in this run unit, assigned on first use (1, 2, 3 ...); the message
    /// serial number is part of the key, because the tag read back must reply to the message it was sent for.</summary>
    internal long TagId(MessageTag tag)
    {
        var key = (tag.Mcs, tag.Party, tag.Name, tag.Message);
        lock (_gate)
        {
            if (!_tagIds.TryGetValue(key, out long id))
            {
                id = _tagIds.Count + 1;
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
