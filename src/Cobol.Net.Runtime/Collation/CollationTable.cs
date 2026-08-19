// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;
using System.IO.Compression;
using System.Reflection;

namespace CobolNet.Runtime.Collation;

/// <summary>
/// COBOL.NET's DERIVED collation table: every Unicode code point (or contraction — a sequence of code points that
/// collates as a unit) → its sequence of <see cref="CollationElement"/>s. The <see cref="Root"/> table is generated
/// from the Unicode CLDR root collation and UCA data by <c>scripts/collation/generate-collation-table.py</c>
/// (sources, versions and hashes in <c>Data/root-collation.manifest.json</c>; the pinned inputs live under
/// <c>data/unicode/</c>) and is embedded in this assembly, so every COBOL.NET program on every host orders text
/// identically — the order does not depend on the ICU build the operating system happens to ship.
/// <para>A table answers three questions the collation engine asks while walking a string: the elements of a code
/// point (<see cref="GetElements"/>; explicit mappings, plus the two computed families — Hangul syllables through
/// their conjoining jamo, and the UTS #10 Table 16 IMPLICIT weights of Han/Tangut/Nushu/Khitan ideographs and of
/// unassigned code points), the contractions that begin with a code point (Thai/Lao prevowel + consonant, tailored
/// digraphs …), and whether a code point is a NON-STARTER (canonical combining class ≠ 0 — the trigger for canonical
/// reordering and discontiguous contraction matching).</para>
/// <para>Tables are IMMUTABLE. A tailoring (<see cref="TailoringRules"/>) does not mutate a table; it produces a
/// new one through <see cref="WithTailoring"/> that shares nothing mutable with its base, so a run unit can hold
/// the root table and any number of locale-tailored tables side by side.</para>
/// <para>⚖ No ISO/IEC 14651 text or table is embedded here or read by the generator; the data is Unicode's
/// (data/unicode/LICENSE-UNICODE.txt). COBOL.NET's conformance statement, verbatim: "Implements collation behavior
/// consistent with ISO/IEC 14651 through derived tables and CLDR/UCA data."</para>
/// </summary>
public sealed class CollationTable
{
    /// <summary>The embedded resource holding the generated root table (LogicalName in Cobol.Net.Runtime.csproj).</summary>
    public const string RootResourceName = "Collation/Data/root-collation.bin";

    private const int Hangul_SBase = 0xAC00, Hangul_LBase = 0x1100, Hangul_VBase = 0x1161, Hangul_TBase = 0x11A7;
    private const int Hangul_LCount = 19, Hangul_VCount = 21, Hangul_TCount = 28;
    private const int Hangul_NCount = Hangul_VCount * Hangul_TCount, Hangul_SCount = Hangul_LCount * Hangul_NCount;

    private static readonly Lazy<CollationTable> s_root = new(LoadRoot, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>The root table — the CLDR root collation order (UCA <see cref="UcaVersion"/>), loaded once per process.</summary>
    public static CollationTable Root => s_root.Value;

    /// <summary>A contraction: the code points AFTER the first one, and the element sequence the whole contraction maps to.</summary>
    internal readonly record struct Contraction(int[] Rest, int Offset, int Count);

    private readonly record struct ImplicitRange(int First, int Last, int Base, int Subtract, byte Kind);

    private readonly CollationElement[] _elements;               // the element pool (primaries already shifted)
    private readonly Dictionary<int, int> _singles;               // code point → (offset << 8) | count
    private readonly Dictionary<int, Contraction[]> _contractions; // first code point → candidates, longest first
    private readonly Dictionary<int, byte> _ccc;                  // non-starters only (ccc ≠ 0)
    private readonly ImplicitRange[] _implicit;                   // UTS #10 Table 16 rows, sorted by First
    private readonly Dictionary<int, int[]> _nfd;                 // code point → its FULL canonical decomposition (expanded, reordered)
    private readonly int _primaryShift;

    private CollationTable(string name, string ucaVersion, string sourceTag, int primaryShift, CollationElement[] elements,
        Dictionary<int, int> singles, Dictionary<int, Contraction[]> contractions, Dictionary<int, byte> ccc,
        ImplicitRange[] implicitRanges, Dictionary<int, int[]> nfd, TailoringRules? tailoring)
    {
        Name = name;
        UcaVersion = ucaVersion;
        SourceTag = sourceTag;
        _primaryShift = primaryShift;
        _elements = elements;
        _singles = singles;
        _contractions = contractions;
        _ccc = ccc;
        _implicit = implicitRanges;
        _nfd = nfd;
        Tailoring = tailoring;
    }

    /// <summary>"root", or the tailoring's name (its <c>@locale</c>, else its source name).</summary>
    public string Name { get; }

    /// <summary>The UCA / CLDR data version the table derives from (e.g. "17.0.0").</summary>
    public string UcaVersion { get; }

    /// <summary>The generator's description of the source data.</summary>
    public string SourceTag { get; }

    /// <summary>The tailoring this table was built with, or null for the root table.</summary>
    public TailoringRules? Tailoring { get; }

    /// <summary>The number of low-order bits every root primary is shifted left by (tailoring room): a root primary
    /// is <c>source &lt;&lt; PrimaryShift</c>, and the values strictly between two adjacent root primaries are free.</summary>
    public int PrimaryShift => _primaryShift;

    /// <summary>The number of explicit single-code-point mappings.</summary>
    public int MappingCount => _singles.Count;

    /// <summary>The number of contractions (multi-code-point mappings).</summary>
    public int ContractionCount { get; private init; }

    /// <summary>The primary weight the highest possible explicit or implicit primary cannot exceed — the level-4
    /// "no variable here" filler of UTS #10 Table 12 (0xFFFF in source scale, shifted like every primary).</summary>
    public int MaxPrimary => 0xFFFF << _primaryShift;

    /// <summary>The FIRST collation element of <paramref name="codePoint"/>'s sequence — the (primary, secondary,
    /// tertiary) triple that decides its coarse position. Sufficient for the single-element majority (letters, digits,
    /// punctuation, precomposed letters' base weight); use <see cref="GetElements"/> when the whole sequence matters
    /// (expansions such as æ or ß, Hangul syllables, implicit-weight ideographs).</summary>
    public CollationElement Lookup(int codePoint)
    {
        if (_singles.TryGetValue(codePoint, out int packed))
            return _elements[packed >> 8];
        if (IsHangulSyllable(codePoint))
            return Lookup(Hangul_LBase + (codePoint - Hangul_SBase) / Hangul_NCount);
        GetImplicit(codePoint, out var first, out _);
        return first;
    }

    /// <summary>The complete collation element sequence of one code point: its explicit mapping, else (Hangul
    /// syllable) the concatenated elements of its conjoining jamo, else the two-element UTS #10 Table 16 implicit
    /// weight. Never empty; a completely ignorable code point yields one all-zero element.</summary>
    public ReadOnlyMemory<CollationElement> GetElements(int codePoint)
    {
        if (TryGetSingle(codePoint, out int offset, out int count))
            return new ReadOnlyMemory<CollationElement>(_elements, offset, count);
        if (IsHangulSyllable(codePoint))
        {
            int n = DecomposeHangul(codePoint, out int l, out int v, out int t);
            var list = new List<CollationElement>(4);
            foreach (int jamo in n == 3 ? [l, v, t] : new[] { l, v })
            {
                if (TryGetSingle(jamo, out int jo, out int jc)) list.AddRange(_elements.AsSpan(jo, jc));
                else { GetImplicit(jamo, out var a, out var b); list.Add(a); list.Add(b); }
            }
            return list.ToArray();
        }
        GetImplicit(codePoint, out var first, out var second);
        return new[] { first, second };
    }

    /// <summary>True when the table carries an explicit mapping for <paramref name="codePoint"/> (as opposed to a
    /// computed Hangul or implicit weight).</summary>
    public bool HasExplicitMapping(int codePoint) => _singles.ContainsKey(codePoint);

    /// <summary>True when the code point is a NON-STARTER — canonical combining class ≠ 0 (combining marks): the
    /// presence of one is what makes canonical reordering (NFD) and discontiguous contraction matching necessary.</summary>
    public bool IsNonStarter(int codePoint) => codePoint >= 0x300 && _ccc.ContainsKey(codePoint);

    /// <summary>The canonical combining class of <paramref name="codePoint"/> (0 for every starter).</summary>
    public int CombiningClass(int codePoint) => codePoint >= 0x300 && _ccc.TryGetValue(codePoint, out byte c) ? c : 0;

    /// <summary>The FULL canonical decomposition of a code point (recursively expanded and canonically ordered —
    /// what NFD maps the character to), or false for a code point that is its own NFD. Hangul syllables are
    /// algorithmic (<see cref="DecomposeHangul"/>) and answer false here.</summary>
    public bool TryGetCanonicalDecomposition(int codePoint, out ReadOnlySpan<int> decomposition)
    {
        if (codePoint >= 0xC0 && _nfd.TryGetValue(codePoint, out var d)) { decomposition = d; return true; }
        decomposition = default;
        return false;
    }

    /// <summary>The number of code points with a canonical decomposition mapping (Hangul syllables excluded).</summary>
    public int CanonicalDecompositionCount => _nfd.Count;

    /// <summary>The contractions that begin with <paramref name="codePoint"/>, LONGEST FIRST, or null.</summary>
    internal Contraction[]? ContractionsStartingWith(int codePoint) =>
        _contractions.Count != 0 && _contractions.TryGetValue(codePoint, out var c) ? c : null;

    /// <summary>The explicit mapping of a code point as a slice of the element pool.</summary>
    internal bool TryGetSingle(int codePoint, out int offset, out int count)
    {
        if (_singles.TryGetValue(codePoint, out int packed))
        {
            offset = packed >> 8;
            count = packed & 0xFF;
            return true;
        }
        offset = count = 0;
        return false;
    }

    /// <summary>A slice of the element pool.</summary>
    internal ReadOnlySpan<CollationElement> Slice(int offset, int count) => _elements.AsSpan(offset, count);

    internal static bool IsHangulSyllable(int codePoint) => (uint)(codePoint - Hangul_SBase) < Hangul_SCount;

    /// <summary>Algorithmic Hangul syllable → conjoining jamo (L V [T]) decomposition (The Unicode Standard §3.12) —
    /// the NFD of a precomposed syllable, without a normalization call. Returns the jamo count (2 or 3; <paramref name="t"/>
    /// is 0 for an LV syllable).</summary>
    internal static int DecomposeHangul(int syllable, out int l, out int v, out int t)
    {
        int s = syllable - Hangul_SBase;
        l = Hangul_LBase + s / Hangul_NCount;
        v = Hangul_VBase + s % Hangul_NCount / Hangul_TCount;
        int tIndex = s % Hangul_TCount;
        if (tIndex == 0) { t = 0; return 2; }
        t = Hangul_TBase + tIndex;
        return 3;
    }

    /// <summary>The UTS #10 (rev. 53) Table 16 IMPLICIT weight of a code point with no explicit mapping: two elements
    /// [.AAAA.0020.0002][.BBBB.0000.0000] where the (AAAA, BBBB) pair is computed from the code point's script family
    /// (siniform scripts by block, core Han, other Han, everything else — including unassigned code points and, for
    /// robustness over ill-formed input, an unpaired surrogate code unit). Sorts after every explicit primary and
    /// before the U+FFFF maximum.</summary>
    internal void GetImplicit(int codePoint, out CollationElement first, out CollationElement second)
    {
        int aaaa, bbbb;
        int lo = 0, hi = _implicit.Length - 1, found = -1;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            var r = _implicit[mid];
            if (codePoint < r.First) hi = mid - 1;
            else if (codePoint > r.Last) lo = mid + 1;
            else { found = mid; break; }
        }
        if (found >= 0)
        {
            var r = _implicit[found];
            if (r.Kind == 0)   // siniform: AAAA = base, BBBB = (CP − start) | 0x8000
            {
                aaaa = r.Base;
                bbbb = (codePoint - r.Subtract) | 0x8000;
            }
            else               // Han core/other: AAAA = base + (CP >> 15), BBBB = (CP & 0x7FFF) | 0x8000
            {
                aaaa = r.Base + (codePoint >> 15);
                bbbb = (codePoint & 0x7FFF) | 0x8000;
            }
        }
        else                   // "any other code point"
        {
            aaaa = 0xFBC0 + (codePoint >> 15);
            bbbb = (codePoint & 0x7FFF) | 0x8000;
        }
        first = new CollationElement(aaaa << _primaryShift, 0x0020, 0x0002);
        second = new CollationElement(bbbb << _primaryShift, 0, 0);
    }

    /// <summary>A NEW table: this table's mappings with <paramref name="rules"/>' entries layered over them (an entry
    /// REPLACES the whole element sequence of its code point / contraction). Canonical closure is automatic: a
    /// tailored single code point whose canonical decomposition is a different sequence gets that decomposed
    /// sequence registered as a contraction with the same elements, so the precomposed and decomposed spellings
    /// keep collating identically after the tailoring (a duty the CLDR rule syntax discharges for its authors and a
    /// numeric-weight file would otherwise silently drop).</summary>
    public CollationTable WithTailoring(TailoringRules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        if (rules.UcaVersion is { } v && !string.Equals(v, UcaVersion, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"tailoring '{rules.Name}' was written for UCA {v} but the base table is UCA {UcaVersion} — its numeric weights are not comparable");
        if (rules.Entries.Count == 0) return this;   // a header-only tailoring (en-US, fr-FR: "the root order is valid") IS this table

        var singles = new Dictionary<int, int>(_singles);
        var contractions = new Dictionary<int, Contraction[]>(_contractions);
        var pool = new List<CollationElement>(_elements.Length + rules.Entries.Count * 2);
        pool.AddRange(_elements);
        int extra = 0;
        foreach (var entry in rules.Entries)
        {
            int offset = pool.Count;
            pool.AddRange(entry.Elements);
            AddMapping(singles, contractions, entry.CodePoints, offset, entry.Elements.Length, ref extra);
            // Canonical closure of a single tailored code point (Hangul syllables are algorithmic; skipped).
            if (entry.CodePoints.Length == 1 && TryGetCanonicalDecomposition(entry.CodePoints[0], out var nfd)
                && nfd.Length > 1 && !rules.Defines(nfd.ToArray()))
                AddMapping(singles, contractions, nfd.ToArray(), offset, entry.Elements.Length, ref extra);
        }
        return new CollationTable(rules.Name, UcaVersion, SourceTag, _primaryShift, pool.ToArray(), singles, contractions,
            _ccc, _implicit, _nfd, rules) { ContractionCount = ContractionCount + extra };
    }

    private static void AddMapping(Dictionary<int, int> singles, Dictionary<int, Contraction[]> contractions,
        int[] codePoints, int offset, int count, ref int newContractions)
    {
        if (codePoints.Length == 1)
        {
            singles[codePoints[0]] = (offset << 8) | count;
            return;
        }
        int[] rest = codePoints[1..];
        var existing = contractions.TryGetValue(codePoints[0], out var c) ? c : [];
        int i = Array.FindIndex(existing, e => e.Rest.AsSpan().SequenceEqual(rest));
        Contraction[] updated;
        if (i >= 0)
        {
            updated = (Contraction[])existing.Clone();
            updated[i] = new Contraction(rest, offset, count);
        }
        else
        {
            updated = new Contraction[existing.Length + 1];
            existing.CopyTo(updated, 0);
            updated[^1] = new Contraction(rest, offset, count);
            Array.Sort(updated, (a, b) => b.Rest.Length.CompareTo(a.Rest.Length));   // longest first
            newContractions++;
        }
        contractions[codePoints[0]] = updated;
    }

    // ---- loading -------------------------------------------------------------------------------------------------

    private static CollationTable LoadRoot()
    {
        using var stream = typeof(CollationTable).Assembly.GetManifestResourceStream(RootResourceName)
            ?? throw new InvalidOperationException($"embedded collation table '{RootResourceName}' is missing from {typeof(CollationTable).Assembly.GetName().Name}");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return Decode(ms.ToArray(), "root");
    }

    /// <summary>Decode a table blob written by <c>generate-collation-table.py</c> (format 1: "CNCT", u32 raw length,
    /// raw-deflate payload). Public so a test can round-trip a regenerated file without touching the resource.</summary>
    public static CollationTable Decode(ReadOnlySpan<byte> blob, string name)
    {
        if (blob.Length < 8 || blob[0] != (byte)'C' || blob[1] != (byte)'N' || blob[2] != (byte)'C' || blob[3] != (byte)'T')
            throw new InvalidDataException("collation table: bad magic");
        int rawLength = BinaryPrimitives.ReadInt32LittleEndian(blob[4..]);
        var raw = new byte[rawLength];
        using (var inflate = new DeflateStream(new MemoryStream(blob[8..].ToArray()), CompressionMode.Decompress))
            inflate.ReadExactly(raw, 0, rawLength);

        var r = new Reader(raw);
        int format = r.U16();
        if (format != 1) throw new InvalidDataException($"collation table: unsupported format {format}");
        int shift = r.U8();
        string version = r.Str();
        string sourceTag = r.Str();

        int elementCount = r.I32();
        var elements = new CollationElement[elementCount];
        for (int i = 0; i < elementCount; i++)
        {
            int p = r.U16(), s = r.U16(), t = r.U8(), flags = r.U8();
            elements[i] = new CollationElement(p << shift, s, t, (flags & 1) != 0);
        }
        int singleCount = r.I32();
        var singles = new Dictionary<int, int>(singleCount);
        for (int i = 0; i < singleCount; i++)
        {
            int cp = r.I32(), offset = r.I32(), count = r.U8();
            singles.Add(cp, (offset << 8) | count);
        }
        int contractionCount = r.I32();
        var byFirst = new Dictionary<int, List<Contraction>>();
        for (int i = 0; i < contractionCount; i++)
        {
            int n = r.U8();
            var cps = new int[n];
            for (int k = 0; k < n; k++) cps[k] = r.I32();
            int offset = r.I32(), count = r.U8();
            if (!byFirst.TryGetValue(cps[0], out var list)) byFirst[cps[0]] = list = [];
            list.Add(new Contraction(cps[1..], offset, count));
        }
        var contractions = new Dictionary<int, Contraction[]>(byFirst.Count);
        foreach (var (first, list) in byFirst)
        {
            list.Sort((a, b) => b.Rest.Length.CompareTo(a.Rest.Length));   // longest first
            contractions[first] = list.ToArray();
        }
        int cccCount = r.I32();
        var ccc = new Dictionary<int, byte>(cccCount);
        for (int i = 0; i < cccCount; i++)
        {
            int cp = r.I32();
            ccc[cp] = (byte)r.U8();
        }
        int rangeCount = r.U16();
        var ranges = new ImplicitRange[rangeCount];
        for (int i = 0; i < rangeCount; i++)
        {
            int first = r.I32(), last = r.I32(), @base = r.U16(), subtract = r.I32(), kind = r.U8();
            ranges[i] = new ImplicitRange(first, last, @base, subtract, (byte)kind);
        }
        Array.Sort(ranges, (a, b) => a.First.CompareTo(b.First));
        int nfdCount = r.I32();
        var nfd = new Dictionary<int, int[]>(nfdCount);
        for (int i = 0; i < nfdCount; i++)
        {
            int cp = r.I32(), n = r.U8();
            var seq = new int[n];
            for (int k = 0; k < n; k++) seq[k] = r.I32();
            nfd.Add(cp, seq);
        }
        if (!r.AtEnd) throw new InvalidDataException("collation table: trailing bytes");
        return new CollationTable(name, version, sourceTag, shift, elements, singles, contractions, ccc, ranges, nfd, tailoring: null)
            { ContractionCount = contractionCount };
    }

    private ref struct Reader(byte[] data)
    {
        private readonly byte[] _d = data;
        private int _pos;
        public bool AtEnd => _pos == _d.Length;
        public int U8() => _d[_pos++];
        public int U16() { int v = BinaryPrimitives.ReadUInt16LittleEndian(_d.AsSpan(_pos)); _pos += 2; return v; }
        public int I32() { int v = BinaryPrimitives.ReadInt32LittleEndian(_d.AsSpan(_pos)); _pos += 4; return v; }
        public string Str() { int n = U8(); string s = System.Text.Encoding.UTF8.GetString(_d, _pos, n); _pos += n; return s; }
    }
}
