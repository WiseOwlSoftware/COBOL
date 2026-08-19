// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Collation;

/// <summary>
/// Walks a (canonically ordered) UTF-16 text and yields its collation elements one at a time — the UTS #10 S2
/// "produce array" step, streamed: code point decoding (an unpaired surrogate is walked as its own code unit so
/// ill-formed input still orders), LONGEST-MATCH contraction lookup (S2.1) including the DISCONTIGUOUS match over
/// unblocked following non-starters (S2.1.1–S2.1.3), Hangul syllable decomposition into conjoining jamo, explicit
/// expansions, and the computed implicit weights of everything the table does not list. Allocation-free except in
/// the discontiguous-match case, where the code units consumed ahead of the cursor are remembered.
/// <para>The caller normalizes first when the text holds a non-starter (<see cref="Collator"/> does); this type never
/// normalizes.</para>
/// </summary>
internal ref struct CollationElementIterator
{
    private readonly ReadOnlySpan<char> _text;
    private readonly CollationTable _table;
    private int _pos;

    // Pending elements of the current mapping: either a pool slice …
    private int _poolOffset, _poolCount, _poolIndex;
    // … or the two computed implicit-weight elements.
    private CollationElement _computed0, _computed1;
    private int _computedCount, _computedIndex;
    // Pending jamo of a decomposed Hangul syllable.
    private int _jamo0, _jamo1, _jamo2, _jamoCount, _jamoIndex;
    // Char indices consumed early by a discontiguous contraction match (rare; lazily allocated).
    private List<int>? _removed;

    public CollationElementIterator(ReadOnlySpan<char> text, CollationTable table)
    {
        _text = text;
        _table = table;
    }

    /// <summary>The next collation element, or false at the end of the text.</summary>
    public bool TryNext(out CollationElement element)
    {
        while (true)
        {
            if (_poolIndex < _poolCount)
            {
                element = _table.Slice(_poolOffset + _poolIndex, 1)[0];
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
            if (!NextCodePoint(out int cp, out int after))
            {
                element = default;
                return false;
            }
            var candidates = _table.ContractionsStartingWith(cp);
            if (candidates is not null && TryMatchContraction(cp, after, candidates, out int offset, out int count))
            {
                SetPool(offset, count);
                continue;
            }
            if (CollationTable.IsHangulSyllable(cp))
            {
                _jamoCount = CollationTable.DecomposeHangul(cp, out _jamo0, out _jamo1, out _jamo2);
                _jamoIndex = 0;
                continue;
            }
            Load(cp);
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

    /// <summary>Decode the code point at the cursor (skipping any position a discontiguous match already consumed)
    /// and advance past it. <paramref name="after"/> is the char index following it.</summary>
    private bool NextCodePoint(out int codePoint, out int after)
    {
        while (_pos < _text.Length)
        {
            int start = _pos;
            int cp = DecodeAt(start, out int len);
            _pos = start + len;
            if (_removed is { Count: > 0 } && _removed.Remove(start)) continue;   // consumed by an earlier discontiguous match
            codePoint = cp;
            after = _pos;
            return true;
        }
        codePoint = 0;
        after = _pos;
        return false;
    }

    private int DecodeAt(int index, out int length)
    {
        char c = _text[index];
        if (char.IsHighSurrogate(c) && index + 1 < _text.Length && char.IsLowSurrogate(_text[index + 1]))
        {
            length = 2;
            return char.ConvertToUtf32(c, _text[index + 1]);
        }
        length = 1;
        return c;   // a BMP character — or an unpaired surrogate walked as its own code unit
    }

    /// <summary>UTS #10 S2.1: the LONGEST contiguous contraction starting at the current code point, then S2.1.1–S2.1.3:
    /// extend it with each following unblocked non-starter for which the table has the longer contraction (a
    /// non-starter is blocked when an intervening non-starter shares its combining class); a non-starter consumed that
    /// way is skipped when the cursor reaches it.</summary>
    private bool TryMatchContraction(int first, int after, CollationTable.Contraction[] candidates, out int offset, out int count)
    {
        // Contiguous, longest first (the candidate array is sorted longest-first).
        int[]? matchedRest = null;
        int end = after;
        offset = count = 0;
        foreach (var cand in candidates)
        {
            if (MatchesAt(after, cand.Rest, out int matchEnd))
            {
                matchedRest = cand.Rest;
                offset = cand.Offset;
                count = cand.Count;
                end = matchEnd;
                break;
            }
        }
        // Discontiguous extension over the following non-starters.
        int scan = end;
        Span<bool> seen = stackalloc bool[256];
        while (scan < _text.Length)
        {
            int cp = DecodeAt(scan, out int len);
            int here = scan;
            scan += len;
            if (_removed is { Count: > 0 } && _removed.Contains(here)) continue;
            int ccc = _table.CombiningClass(cp);
            if (ccc == 0) break;
            if (seen[ccc]) continue;                                   // blocked (S2.1.2 note)
            int restLength = matchedRest?.Length ?? 0;
            var grown = FindContraction(candidates, matchedRest, restLength, cp);
            if (grown is { } g)
            {
                matchedRest = g.Rest;
                offset = g.Offset;
                count = g.Count;
                (_removed ??= new List<int>(2)).Add(here);
                // The consumed mark no longer blocks anything; its class is NOT recorded.
            }
            else seen[ccc] = true;
        }
        if (matchedRest is null) return false;
        _pos = end;      // past the contiguous part; discontiguously consumed marks are skipped when the cursor reaches them
        return true;
    }

    /// <summary>The candidate whose Rest is <paramref name="prefix"/> (length <paramref name="prefixLength"/>) followed
    /// by <paramref name="next"/>, or null.</summary>
    private static CollationTable.Contraction? FindContraction(CollationTable.Contraction[] candidates, int[]? prefix, int prefixLength, int next)
    {
        foreach (var cand in candidates)
        {
            if (cand.Rest.Length != prefixLength + 1 || cand.Rest[prefixLength] != next) continue;
            bool same = true;
            for (int i = 0; i < prefixLength && same; i++) same = cand.Rest[i] == prefix![i];
            if (same) return cand;
        }
        return null;
    }

    /// <summary>Do the code points starting at <paramref name="index"/> (skipping consumed positions) spell
    /// <paramref name="rest"/>? <paramref name="end"/> is the char index just past the match.</summary>
    private bool MatchesAt(int index, int[] rest, out int end)
    {
        int i = index;
        foreach (int expected in rest)
        {
            while (true)
            {
                if (i >= _text.Length) { end = i; return false; }
                int cp = DecodeAt(i, out int len);
                int here = i;
                i += len;
                if (_removed is { Count: > 0 } && _removed.Contains(here)) continue;
                if (cp != expected) { end = i; return false; }
                break;
            }
        }
        end = i;
        return true;
    }
}
