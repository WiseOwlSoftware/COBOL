// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime.IO;

/// <summary>
/// ⛔ THE ONE KEY COMPARISON of SORT and MERGE (ISO §14.9.40 GR8 / §14.9.24 GR3 — the relation-condition comparison
/// rules per key), over a fixed number of SLOTS, each holding one record's keys DECODED ONCE: a chunk being sorted has
/// one slot per record, a merge one per run head (<see cref="SortWork"/>, kb/Work PB3339). A NUMERIC key column holds
/// the records' algebraic values (GR8 / §8.8.4.2.4 — a collating sequence NEVER applies to a numeric comparison); a
/// NATIONAL key column holds the records' DECODED national character positions (§8.8.4.2.9 compares national
/// POSITIONS, and the record image carries each as a UTF-16BE byte pair — §13.18.60.4 GR8 / D-N1); a collated column
/// under a sequence that supports keys (<see cref="CobolCollation.SupportsKeys"/> — the LOCALE arm) holds the records'
/// materialized <see cref="Collation.CollationKey"/>s (built through the collator's
/// <see cref="Collation.Cache.CollationKeyCache"/>, so records sharing a value share one key); any other character
/// column holds the records' key windows, sliced once, compared under that key's own sequence (an ALPHABET table) or
/// the native order. <see cref="Compare"/> compares two slots most significant key first; DESCENDING inverts the
/// per-key result (GR8b); 0 ⇔ all keys equal (GR8c — the caller's stability or run order then decides, GR3/GR4).
/// <para>⛔ THE SEQUENCE IS PER KEY, NOT PER STATEMENT (ISO §14.9.40.4 GR5 / §14.9.24.4 GR5): one SORT resolves an
/// alphanumeric AND a national sequence and each key takes the one its CLASS names. A single per-statement sequence
/// applied the alphanumeric weights to a national key's UTF-16BE bytes — right by accident in the native order (byte
/// order IS code point order) and silently wrong under any declared alphabet (kb/Work PB678).</para>
/// </summary>
internal sealed class SortKeyColumns
{
    private readonly CobolSort.Key[] _keys;
    private readonly Int128[]?[] _numeric;
    /// <summary>The unsigned 16-byte binary key columns (kb/Work PB186) — see <see cref="NumProfile.ImageExceedsInt128"/>.</summary>
    private readonly UInt128[]?[] _unsignedWide;
    private readonly double[]?[] _float;
    private readonly Collation.CollationKey[]?[] _collationKeys;
    private readonly string[]?[] _slices;
    /// <summary>The GR5-selected sequence of each key, parallel to <see cref="_keys"/> — null for the native order and
    /// for the two classes GR5 names no sequence for (numeric, boolean).</summary>
    private readonly CobolCollation?[] _seq;

    /// <summary>Columns of <paramref name="slots"/> slots for <paramref name="keys"/> under the statement's two GR5
    /// sequences.</summary>
    public SortKeyColumns(CobolSort.Key[] keys, CobolCollation? collation, CobolCollation? national, int slots)
    {
        _keys = keys;
        _numeric = new Int128[]?[keys.Length];
        _unsignedWide = new UInt128[]?[keys.Length];
        _float = new double[]?[keys.Length];
        _collationKeys = new Collation.CollationKey[]?[keys.Length];
        _slices = new string[]?[keys.Length];
        _seq = new CobolCollation?[keys.Length];
        for (int k = 0; k < keys.Length; k++)
        {
            var key = keys[k];
            _seq[k] = SequenceFor(key.Class, collation, national);
            switch (LaneOf(key, _seq[k]))
            {
                case Lane.Float: _float[k] = new double[slots]; break;
                case Lane.UnsignedWide: _unsignedWide[k] = new UInt128[slots]; break;
                case Lane.Numeric: _numeric[k] = new Int128[slots]; break;
                case Lane.CollationKey: _collationKeys[k] = new Collation.CollationKey[slots]; break;
                default: _slices[k] = new string[slots]; break;
            }
        }
    }

    /// <summary>How a key's column holds its decoded values.</summary>
    private enum Lane { Float, UnsignedWide, Numeric, CollationKey, Slice }

    /// <summary>The one lane selection — shared by the columns and by the resident estimate
    /// (<see cref="ResidentBytesPerRecord"/>), so the two cannot disagree about what a record's keys cost.</summary>
    private static Lane LaneOf(in CobolSort.Key key, CobolCollation? seq) =>
        // A FLOAT key (an Ieee-form profile, kb/Work PB164 wave 2) decodes through the IEEE lane and compares as its
        // ALGEBRAIC double value (§14.9.40.4 GR8 → §8.8.4.2.4 — a numeric key compares by value regardless of usage;
        // its raw big-endian IEEE bytes would order every negative after every positive). double.CompareTo is a total
        // order, so an exotic NaN payload cannot throw.
        key.Class is CobolSort.KeyClass.Numeric && key.Profile.ByteForm is NumericByteForm.Ieee32 or NumericByteForm.Ieee64 ? Lane.Float
        // An UNSIGNED 16-byte binary key (kb/Work PB186): its [0, 2^128) range does not fit the Int128 column — the
        // signed decode returns a value at or above 2^127 as a NEGATIVE Int128, so ALL-ONES would sort before 7.
        // §14.9.40.4 GR8 / §14.9.24.4 GR3 compare keys "according to the rules for comparison of operands in a
        // relation condition", which for numeric operands is §8.8.4.2.4's algebraic value, so the key decodes through
        // the unsigned lane and orders as a UInt128.
        : key.Class is CobolSort.KeyClass.Numeric && key.Profile.ImageExceedsInt128 ? Lane.UnsignedWide
        : key.Class is CobolSort.KeyClass.Numeric ? Lane.Numeric
        : seq is { SupportsKeys: true } ? Lane.CollationKey
        : Lane.Slice;

    /// <summary>Decode <paramref name="record"/>'s keys into <paramref name="slot"/>.</summary>
    public void Set(int slot, in StoredFrame record)
    {
        for (int k = 0; k < _keys.Length; k++)
        {
            ref readonly CobolSort.Key key = ref _keys[k];
            if (_float[k] is { } fs) fs[slot] = CobolNum.ParseImageFloat(Slice(record, key), key.Profile);
            else if (_unsignedWide[k] is { } uws) uws[slot] = CobolNum.ParseImageU128(Slice(record, key), key.Profile);
            else if (_numeric[k] is { } nums) nums[slot] = NumericKey(record, key);
            else if (_collationKeys[k] is { } ck) ck[slot] = _seq[k]!.KeyOf(Operand(record, key))!;
            else _slices[k]![slot] = Operand(record, key);
        }
    }

    /// <summary>Compare slots <paramref name="x"/> and <paramref name="y"/> on every key, most significant first.</summary>
    public int Compare(int x, int y)
    {
        for (int k = 0; k < _keys.Length; k++)
        {
            int c;
            if (_numeric[k] is { } nums) c = nums[x].CompareTo(nums[y]);
            else if (_unsignedWide[k] is { } uws) c = uws[x].CompareTo(uws[y]);
            else if (_float[k] is { } fs) c = fs[x].CompareTo(fs[y]);
            else if (_collationKeys[k] is { } ck) c = ck[x].CompareTo(ck[y]);
            else
            {
                var s = _slices[k]!;
                c = _seq[k] is { } q ? q.Compare(s[x], s[y]) : CobolString.Compare(s[x], s[y]);
            }
            if (c != 0) return _keys[k].Descending ? -c : c;
        }
        return 0;
    }

    /// <summary>⛔ WHAT ONE RECORD'S DECODED KEYS COST IN A SLOT, in bytes — the part of <see cref="SortWork"/>'s resident
    /// estimate the chunk's sort adds (kb/Work PB3339): a numeric value is its column element, a window or a national
    /// operand its string (two bytes per character plus the object), a collation key its object and sort-key bytes.
    /// An estimate that errs high, so the limit is kept, not merely approached.</summary>
    public static long ResidentBytesPerRecord(CobolSort.Key[] keys, CobolCollation? collation, CobolCollation? national)
    {
        long bytes = 0;
        foreach (var key in keys)
            bytes += LaneOf(key, SequenceFor(key.Class, collation, national)) switch
            {
                Lane.Float => sizeof(double),
                Lane.UnsignedWide or Lane.Numeric => 16,
                Lane.CollationKey => 64 + (4L * key.Length),   // the key object, its sort-key array (a few bytes a character)
                _ => 8 + StringObjectBytes + (2L * key.Length),
            };
        return bytes;
    }

    /// <summary>A string object's fixed cost beyond its characters (header, method table, length, terminator).</summary>
    internal const int StringObjectBytes = 32;

    /// <summary>⛔ THE ONE class → collating-sequence selection (ISO §14.9.40.4 GR5 / §14.9.24.4 GR5): the ALPHANUMERIC
    /// sequence for keys of class alphabetic and alphanumeric, the NATIONAL sequence for keys of class national, and NO
    /// sequence for the two classes GR5 does not name — numeric keys compare algebraically (§8.8.4.2.4) and boolean
    /// keys compare by boolean value "regardless of their usage" (§8.8.4.2.8), so a declared alphabet must not reach
    /// either.</summary>
    private static CobolCollation? SequenceFor(CobolSort.KeyClass cls, CobolCollation? alnum, CobolCollation? national) => cls switch
    {
        CobolSort.KeyClass.Alphanumeric => alnum,
        CobolSort.KeyClass.National => national,
        _ => null,
    };

    /// <summary>The key's COMPARISON OPERAND out of a record image — the window for every class but national, and for a
    /// NATIONAL key the window's UTF-16BE byte pairs decoded back to national character positions through the ONE
    /// decoder <see cref="CobolBits.NatReadWindow"/>. §8.8.4.2.9 states the comparison over national character positions
    /// ("the length of an operand is the number of national character positions in the operand") while the record
    /// image stores two bytes per position (§13.18.60.4 GR8; D-N1), so comparing the raw window would weigh BYTES — and
    /// the high byte of every Latin national character is U+0000, which is how a national key silently compared EQUAL
    /// across whole records before kb/Work PB678.</summary>
    private static string Operand(in StoredFrame record, in CobolSort.Key k) =>
        k.Class is CobolSort.KeyClass.National
            ? CobolBits.NatReadWindow(record.Image ?? "", k.At(record.Image ?? "", record.Extents),
                k.Length / CobolBits.BytesPerNational)
            : Slice(record, k);

    /// <summary>The key's character window of a record image. A record shorter than the window (a varying record —
    /// §14.9.40.3 SR6g requires keys within the MINIMUM size, so a conforming program never hits this; the lenient path
    /// space-extends, matching the §8.8.4.2.1 shorter-operand rule) is padded with spaces.</summary>
    private static string Slice(in StoredFrame record, in CobolSort.Key k)
    {
        string image = record.Image ?? "";
        int at = k.At(image, record.Extents);
        int needed = at + k.Length;
        if (image.Length < needed) image = image.PadRight(needed);
        return image.Substring(at, k.Length);
    }

    /// <summary>Decode a fixed-point numeric key's window to its algebraic value through the KEY ITEM'S OWN profile —
    /// the ONE description of what those bytes are (<see cref="CobolNum.ParseImage"/>: zoned digits for USAGE DISPLAY,
    /// radix-2 / BCD for BINARY / PACKED, V59). A profile rebuilt from the window width alone could only ever describe a
    /// zoned key, which is how a COMP key would have sorted by its digit characters instead of its value. Scale is
    /// irrelevant for ordering: both operands of one key share one PICTURE, so the unscaled values order identically to
    /// the scaled ones. A profile with NO byte form (<see cref="NumericByteForm.None"/> — no shipping usage since R40)
    /// throws the codec's loud invariant break rather than yielding an invented ordering; float keys and UNSIGNED
    /// 16-byte binary keys take their own lanes (<see cref="LaneOf"/>), never this one.</summary>
    private static Int128 NumericKey(in StoredFrame record, in CobolSort.Key k) => CobolNum.ParseImage(Slice(record, k), k.Profile);
}
