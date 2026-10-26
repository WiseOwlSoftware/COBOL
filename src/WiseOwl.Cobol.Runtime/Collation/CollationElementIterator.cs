// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime.Collation;

/// <summary>
/// Walks a (canonically ordered) UTF-16 text and yields its collation elements one at a time — the UTS #10 S2
/// "produce array" step, streamed. WHICH mapping each step of the text takes (S2.1: code point decoding, the
/// LONGEST-MATCH contraction lookup including the DISCONTIGUOUS match over unblocked following non-starters,
/// S2.1.1–S2.1.3) is the one <see cref="ContractionWalker"/>'s; this type fetches what that mapping is (S2.2):
/// explicit mappings and expansions, Hangul syllable decomposition into conjoining jamo, and the computed implicit
/// weights of everything the table does not list. Allocation-free except in the discontiguous-match case, where the
/// code units consumed ahead of the cursor are remembered.
/// <para>The caller normalizes first when the text holds a non-starter (<see cref="Collator"/> does); this type never
/// normalizes.</para>
/// </summary>
internal ref struct CollationElementIterator
{
    private readonly CollationTable _table;
    private ContractionWalker _walker;

    // Pending elements of the current mapping: either a pool slice …
    private int _poolOffset, _poolCount, _poolIndex;
    // … or the two computed implicit-weight elements.
    private CollationElement _computed0, _computed1;
    private int _computedCount, _computedIndex;
    // Pending jamo of a decomposed Hangul syllable.
    private int _jamo0, _jamo1, _jamo2, _jamoCount, _jamoIndex;

    public CollationElementIterator(ReadOnlySpan<char> text, CollationTable table)
    {
        _table = table;
        _walker = new ContractionWalker(text, table);
    }

    /// <summary>The next collation element, or false at the end of the text.</summary>
    public bool TryNext(out CollationElement element)
    {
        while (true)
        {
            if (TryNextOfMapping(out element)) return true;
            if (!TryNextMapping())
            {
                element = default;
                return false;
            }
        }
    }

    /// <summary>The char index of the text where the current MAPPING begins: the first code point of the single
    /// character, Hangul syllable or contraction <see cref="TryNextMapping"/> last took.</summary>
    public int MappingStart { get; private set; }

    /// <summary>The char index just past the last position the current mapping consumed (a discontiguous contraction's
    /// furthest mark included; a mark it skipped inside that span is a mapping of its own, walked later).</summary>
    public int MappingEnd { get; private set; }

    /// <summary>Advance to the next MAPPING of the text (UTS #10 S2.1: one code point, a Hangul syllable or the longest
    /// contraction), whose elements <see cref="TryNextOfMapping"/> then yields — none for a mapping to no element. False
    /// at the end of the text. Any element of the previous mapping not yet yielded is dropped.</summary>
    public bool TryNextMapping()
    {
        _poolCount = _poolIndex = _computedCount = _computedIndex = _jamoCount = _jamoIndex = 0;
        if (!_walker.NextCodePoint(out int cp, out int after)) return false;
        MappingStart = after - (cp > 0xFFFF ? 2 : 1);
        MappingEnd = after;
        var candidates = _table.ContractionsStartingWith(cp);
        if (candidates is not null && _walker.TryMatchContraction(after, candidates, out var contraction))
        {
            SetPool(contraction.Offset, contraction.Count);
            MappingEnd = _walker.MatchEnd;
        }
        else if (CollationTable.IsHangulSyllable(cp))
            _jamoCount = CollationTable.DecomposeHangul(cp, out _jamo0, out _jamo1, out _jamo2);
        else Load(cp);
        return true;
    }

    /// <summary>The next element of the current mapping, or false when it has no more.</summary>
    public bool TryNextOfMapping(out CollationElement element)
    {
        while (true)
        {
            if (_poolIndex < _poolCount)
            {
                element = _table.ElementAt(_poolOffset + _poolIndex);
                _poolIndex++;
                return true;
            }
            if (_computedIndex < _computedCount)
            {
                element = _computedIndex == 0 ? _computed0 : _computed1;
                _computedIndex++;
                return true;
            }
            if (_jamoIndex < _jamoCount)
            {
                int jamo = _jamoIndex switch { 0 => _jamo0, 1 => _jamo1, _ => _jamo2 };
                _jamoIndex++;
                Load(jamo);
                continue;
            }
            element = default;
            return false;
        }
    }

    private void Load(int codePoint)
    {
        if (_table.TryGetSingle(codePoint, out int offset, out int count))
        {
            SetPool(offset, count);
            return;
        }
        _table.GetImplicit(codePoint, out _computed0, out _computed1);
        _computedCount = 2;
        _computedIndex = 0;
    }

    private void SetPool(int offset, int count)
    {
        _poolOffset = offset;
        _poolCount = count;
        _poolIndex = 0;
    }
}
