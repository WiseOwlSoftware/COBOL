// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// The in-memory sort/merge store (ISO/IEC 1989:2023 §14.9.40 SORT / §14.9.24 MERGE): one record-IMAGE buffer per
/// sort-merge (SD) file, keyed by COBOL file-name. Records cross this boundary as their character image (a
/// <see cref="string"/> at the record's released length) — the typed record ↔ image conversion stays in the
/// generated code, exactly like <see cref="CobolFile"/> (the substrate stays typed; COBOLNET_DESIGN §8.2: the sort
/// store holds serialized images, key windows are computed at compile time). The compiler emits
/// <c>Init → {Release… | NextInput+Release…} → Sort|Merge → {Return… (Rewind per GIVING file)} → Close</c>
/// around each SORT/MERGE statement — the three GR9 phases.
/// </summary>
public static class CobolSort
{
    /// <summary>One compile-time key descriptor (ISO §14.9.40.3 SR6a/SR6e — keys are fixed character windows of
    /// the SD record; the same positions are the key in EVERY record): the window (<paramref name="Offset"/>,
    /// <paramref name="Length"/>), the direction (GR8a/b), and the comparison kind — a NUMERIC key decodes its
    /// zoned/separate-sign image and compares ALGEBRAICALLY (GR8 → §8.8.4.2.4; never through a collating
    /// sequence), an alphanumeric key compares character-wise under the statement's resolved sequence (GR5 /
    /// §8.8.4.2.7).</summary>
    public readonly record struct Key(int Offset, int Length, bool Descending, bool Numeric, NumProfile Profile);

    /// <summary>The per-SD store: released images (in release order — the stability anchor GR3 requires), the
    /// USING stream boundaries for MERGE, and the return cursor.</summary>
    private sealed class Store
    {
        public readonly List<string> Records = [];
        public readonly List<int> StreamStarts = [];   // MERGE: index where each USING file's records begin
        public int Cursor;
        public int LastReturnedLength;
        /// <summary>The collating sequence snapshotted at statement start (ISO §14.6.6 r5) — see <see cref="Init(string, CobolCollation?)"/>.</summary>
        public CobolCollation? Collation;
        // The EC-FLOW-RELEASE / EC-FLOW-RETURN / EC-SORT-MERGE-ACTIVE phase tracking (§14.9.32 GR1 / §14.9.34 GR1 /
        // §14.9.40 GR10+GR13) attaches here when the EC model lands — checking is OFF by default (SSOT §18.16).
    }

    private static readonly Dictionary<string, Store> Files = new(StringComparer.OrdinalIgnoreCase);

    private static Store Get(string name)
    {
        if (!Files.TryGetValue(name, out var f)) Files[name] = f = new Store();
        return f;
    }

    /// <summary>Begin a SORT/MERGE statement on <paramref name="name"/>: a fresh, empty store (ISO §14.9.40 GR9a —
    /// the release phase starts). Re-executing a SORT on the same SD reuses the connector with a clean buffer.</summary>
    public static void Init(string name) => Init(name, null);

    /// <summary>Begin a SORT/MERGE statement with its COLLATING SEQUENCE (or the program collating sequence):
    /// the sequence is SNAPSHOTTED here, at statement start (ISO §14.6.6 r5 — "A locale switch during execution of a
    /// SORT or MERGE statement has no effect on the processing of that SORT or MERGE statement" — a SET LOCALE in an
    /// INPUT PROCEDURE must not move the sequence the sort phase uses), and <see cref="Sort"/> / <see cref="Merge"/>
    /// use it.</summary>
    public static void Init(string name, CobolCollation? collation)
    {
        var f = Get(name);
        f.Records.Clear();
        f.StreamStarts.Clear();
        f.Cursor = 0;
        f.LastReturnedLength = 0;
        f.Collation = collation?.Snapshot();
    }

    /// <summary>Mark the start of the next USING stream (MERGE only): records released after this call belong to
    /// the next file-name-2/-3 in statement order — the tie-break order ISO §14.9.24 GR4 prescribes.</summary>
    public static void NextInput(string name)
    {
        var f = Get(name);
        f.StreamStarts.Add(f.Records.Count);
    }

    /// <summary>RELEASE one record image at its released length (ISO §14.9.32 GR2; §14.9.40 GR12b for the implicit
    /// USING release). Seam: a RELEASE outside the active SORT's input procedure is EC-FLOW-RELEASE (§14.9.32 GR1)
    /// and a record size outside the SD's record range is EC-SORT-MERGE-RELEASE (§14.9.40 GR12b) — exception
    /// checking is OFF by default (COBOLNET_DESIGN §18.16), so the store accepts the record as written.</summary>
    public static void Release(string name, string image) => Get(name).Records.Add(image ?? "");

    /// <summary>The sequence phase (ISO §14.9.40 GR9b): a STABLE key sort. Stability realizes GR3's DUPLICATES IN
    /// ORDER (equal keys keep USING-file / RELEASE order — the buffer holds them in exactly that order); without
    /// the phrase the relative order is undefined (GR4), so the stable result is conformant there too —
    /// <paramref name="duplicatesInOrder"/> is accepted for the call-site's traceability.</summary>
    public static void Sort(string name, Key[] keys, CobolCollation? collation, bool duplicatesInOrder)
    {
        _ = duplicatesInOrder;   // stability is unconditional — GR3 satisfied, GR4 (undefined) safely refined
        var f = Get(name);
        int n = f.Records.Count;
        var idx = new int[n];
        for (int i = 0; i < n; i++) idx[i] = i;
        var columns = KeyColumns.Build(f.Records, keys, f.Collation ?? collation?.Snapshot());
        Array.Sort(idx, (x, y) =>
        {
            int c = columns.Compare(x, y);
            return c != 0 ? c : x - y;   // tie → original (release) order: the stable sort
        });
        var sorted = new List<string>(n);
        foreach (int i in idx) sorted.Add(f.Records[i]);
        f.Records.Clear();
        f.Records.AddRange(sorted);
        f.Cursor = 0;
    }

    /// <summary>The merge operation (ISO §14.9.24 GR1): a k-way merge of the pre-sorted USING streams. Equal keys
    /// take the record from the EARLIEST stream first, and within one stream the records keep their file order —
    /// exactly GR4a/GR4b. Seam: input NOT ordered per the KEY phrases is EC-SORT-MERGE-SEQUENCE (GR6 — Fatal,
    /// files closed, result undefined); checking is OFF by default (COBOLNET_DESIGN §18.16), and the k-way merge
    /// then yields a deterministic stream-merge order, conformant within "undefined".</summary>
    public static void Merge(string name, Key[] keys, CobolCollation? collation)
    {
        var f = Get(name);
        int streams = f.StreamStarts.Count;
        var pos = new int[streams];
        var end = new int[streams];
        for (int s = 0; s < streams; s++)
        {
            pos[s] = f.StreamStarts[s];
            end[s] = s + 1 < streams ? f.StreamStarts[s + 1] : f.Records.Count;
        }
        var merged = new List<string>(f.Records.Count);
        var columns = KeyColumns.Build(f.Records, keys, f.Collation ?? collation?.Snapshot());
        while (true)
        {
            int best = -1;
            for (int s = 0; s < streams; s++)
            {
                if (pos[s] >= end[s]) continue;
                // STRICT less-than: an equal head never displaces an earlier stream's record (GR4a).
                if (best < 0 || columns.Compare(pos[s], pos[best]) < 0)
                    best = s;
            }
            if (best < 0) break;
            merged.Add(f.Records[pos[best]++]);
        }
        f.Records.Clear();
        f.Records.AddRange(merged);
        f.Cursor = 0;
    }

    /// <summary>RETURN the next record in key order (ISO §14.9.34 GR3): <see langword="true"/> with the record's
    /// image at its own length, or <see langword="false"/> at end. Seams: a RETURN outside the active output
    /// procedure is EC-FLOW-RETURN (GR1) and a RETURN after the at-end condition is EC-SORT-MERGE-RETURN (GR3,
    /// result undefined) — checking OFF (COBOLNET_DESIGN §18.16), so a post-end RETURN deterministically reports
    /// at-end again (a conformant refinement of "undefined").</summary>
    public static bool Return(string name, out string image)
    {
        var f = Get(name);
        if (f.Cursor >= f.Records.Count)
        {
            image = "";
            f.LastReturnedLength = 0;
            return false;   // at end — the record area's content is undefined (GR3); the caller leaves it as-is
        }
        image = f.Records[f.Cursor++];
        f.LastReturnedLength = image.Length;
        return true;
    }

    /// <summary>The length of the most recently RETURNed record — the value a varying SD's RECORD VARYING
    /// DEPENDING ON item receives (ISO §13.18.43 GR15: each returned record restores its own length).</summary>
    public static int LastReturnedLength(string name) => Get(name).LastReturnedLength;

    /// <summary>Rewind the return cursor to the first record — emitted before EACH GIVING file's write-out, so
    /// every file-name-3/-4 receives the FULL sorted/merged result (ISO §14.9.40 GR15 / §14.9.24 GR12).</summary>
    public static void Rewind(string name) => Get(name).Cursor = 0;

    /// <summary>End the SORT/MERGE statement: drop the buffered records (the sort file has no persistent storage —
    /// ISO §9 sort-merge file model: only RELEASE/RETURN/SORT/MERGE ever reference it).</summary>
    public static void Close(string name) => Files.Remove(name);

    // ── Key comparison (ISO §14.9.40 GR8 / §14.9.24 GR3 — ONE policy for SORT and MERGE) ─────────────────────

    /// <summary>The key columns of a record buffer, DECODED ONCE per record before the sort or merge compares
    /// anything (ISO §14.9.40 GR8 — the relation-condition comparison rules per key): a NUMERIC key column holds the
    /// records' algebraic values (GR8 / §8.8.4.2.4 — a collating sequence NEVER applies to a numeric comparison); an
    /// alphanumeric key column under a sequence that supports keys (<see cref="CobolCollation.SupportsKeys"/> — the
    /// LOCALE arm) holds the records' materialized <see cref="Collation.CollationKey"/>s (built through the collator's
    /// <see cref="Collation.Cache.CollationKeyCache"/>, so records sharing a value share one key); any other
    /// alphanumeric key column holds the records' key windows, sliced once, compared under the sequence (an ALPHABET
    /// table) or the native order. <see cref="Compare"/> then compares two records most significant key first;
    /// DESCENDING inverts the per-key result (GR8b); 0 ⇔ all keys equal (GR8c — the caller's stability or stream
    /// order then decides, GR3/GR4). Compared to re-slicing and re-walking every key at every comparison, an
    /// n-record sort does n key decodes instead of 2·n·log n.</summary>
    private sealed class KeyColumns
    {
        private readonly Key[] _keys;
        private readonly Int128[]?[] _numeric;
        private readonly double[]?[] _float;
        private readonly Collation.CollationKey[]?[] _collationKeys;
        private readonly string[]?[] _slices;
        private readonly CobolCollation? _collation;

        private KeyColumns(Key[] keys, Int128[]?[] numeric, double[]?[] floats, Collation.CollationKey[]?[] collationKeys, string[]?[] slices, CobolCollation? collation)
        {
            _keys = keys;
            _numeric = numeric;
            _float = floats;
            _collationKeys = collationKeys;
            _slices = slices;
            _collation = collation;
        }

        public static KeyColumns Build(List<string> records, Key[] keys, CobolCollation? collation)
        {
            int n = records.Count;
            var numeric = new Int128[]?[keys.Length];
            var floats = new double[]?[keys.Length];
            var collationKeys = new Collation.CollationKey[]?[keys.Length];
            var slices = new string[]?[keys.Length];
            bool useKeys = collation is { SupportsKeys: true };
            for (int k = 0; k < keys.Length; k++)
            {
                var key = keys[k];
                // A FLOAT key (an Ieee-form profile, kb/Work PB164 wave 2) decodes through the IEEE lane and
                // compares as its ALGEBRAIC double value (§14.9.40.4 GR8 → §8.8.4.2.4 — a numeric key compares
                // by value regardless of usage; its raw big-endian IEEE bytes would order every negative after
                // every positive). double.CompareTo is a total order, so an exotic NaN payload cannot throw.
                if (key.Numeric && key.Profile.ByteForm is NumericByteForm.Ieee32 or NumericByteForm.Ieee64)
                {
                    var col = new double[n];
                    for (int i = 0; i < n; i++) col[i] = CobolNum.ParseImageFloat(Slice(records[i], key), key.Profile);
                    floats[k] = col;
                }
                else if (key.Numeric)
                {
                    var col = new Int128[n];
                    for (int i = 0; i < n; i++) col[i] = NumericKey(records[i], key);
                    numeric[k] = col;
                }
                else if (useKeys)
                {
                    var col = new Collation.CollationKey[n];
                    for (int i = 0; i < n; i++) col[i] = collation!.KeyOf(Slice(records[i], key))!;
                    collationKeys[k] = col;
                }
                else
                {
                    var col = new string[n];
                    for (int i = 0; i < n; i++) col[i] = Slice(records[i], key);
                    slices[k] = col;
                }
            }
            return new KeyColumns(keys, numeric, floats, collationKeys, slices, collation);
        }

        /// <summary>Compare records <paramref name="x"/> and <paramref name="y"/> on every key, most significant first.</summary>
        public int Compare(int x, int y)
        {
            for (int k = 0; k < _keys.Length; k++)
            {
                int c;
                if (_numeric[k] is { } nums) c = nums[x].CompareTo(nums[y]);
                else if (_float[k] is { } fs) c = fs[x].CompareTo(fs[y]);
                else if (_collationKeys[k] is { } ck) c = ck[x].CompareTo(ck[y]);
                else
                {
                    var s = _slices[k]!;
                    c = _collation is null ? CobolString.Compare(s[x], s[y]) : _collation.Compare(s[x], s[y]);
                }
                if (c != 0) return _keys[k].Descending ? -c : c;
            }
            return 0;
        }
    }

    /// <summary>The key's character window of a record image. A record shorter than the window (a varying record
    /// — §14.9.40.3 SR6g requires keys within the MINIMUM size, so a conforming program never hits this; the
    /// lenient path space-extends, matching the §8.8.4.2.1 shorter-operand rule) is padded with spaces.</summary>
    private static string Slice(string image, in Key k)
    {
        image ??= "";
        int needed = k.Offset + k.Length;
        if (image.Length < needed) image = image.PadRight(needed);
        return image.Substring(k.Offset, k.Length);
    }

    /// <summary>Decode a numeric key window to its algebraic value through the KEY ITEM'S OWN profile — the ONE
    /// description of what those bytes are (<see cref="CobolNum.ParseImage"/>: zoned digits for USAGE DISPLAY,
    /// radix-2 / BCD for BINARY / PACKED, V59). A profile rebuilt from the window width alone could only ever
    /// describe a zoned key, which is how a COMP key would have sorted by its digit characters instead of its
    /// value. Scale is irrelevant for ordering: both operands of one key share one PICTURE, so the unscaled
    /// values order identically to the scaled ones.</summary>
    /// <summary>Decode a fixed-point numeric key's window with the leaf's own profile (zoned/radix-2/BCD —
    /// V59). A profile with NO byte form (<see cref="NumericByteForm.None"/> — USAGE INDEX) throws the codec's
    /// loud invariant break rather than yielding an invented ordering; float keys take the algebraic lane in
    /// <see cref="KeyColumns.Build"/>, never this one.</summary>
    private static Int128 NumericKey(string image, in Key k) => CobolNum.ParseImage(Slice(image, k), k.Profile);
}
