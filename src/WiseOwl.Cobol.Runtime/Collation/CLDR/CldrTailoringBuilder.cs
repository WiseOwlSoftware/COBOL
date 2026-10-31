// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace WiseOwl.Cobol.Runtime.Collation.Cldr;

/// <summary>What <see cref="CldrTailoringBuilder.Build"/> produced for one locale collation.</summary>
/// <param name="Table">The tailored table (the root table itself when the collation has no rules).</param>
/// <param name="Options">The engine settings the collation (and the tag's <c>-u-</c> keys) asked for.</param>
/// <param name="Unsupported">What could not be honored — settings the engine lacks, rules that had to be skipped.</param>
/// <param name="Notes">Informational: how the builder represented constructs (prefix contexts as contractions, …).</param>
/// <param name="RulesApplied">The number of resets + relations processed (imports expanded).</param>
public sealed record CldrBuildResult(CollationTable Table, CollationOptions Options, IReadOnlyList<string> Unsupported,
    IReadOnlyList<string> Notes, int RulesApplied);

/// <summary>
/// The CLDR TAILORING BUILDER: turns a CLDR collation's rules (<see cref="CldrCollation"/>, imports expanded through the
/// <see cref="CldrLocaleLoader"/>) into a tailored <see cref="CollationTable"/> over the root table, plus the
/// <see cref="CollationOptions"/> the rules' settings ask for. This is the derivation the shipped
/// <c>.tailor</c> files were made by hand for (Spanish ñ), done mechanically for every CLDR locale.
/// <para><b>How a relation becomes weights.</b> Each weight level of the root table is an ordered line of DISTINCT
/// weights. A reset (<c>&amp;X</c>) reads X's collation elements as the current position: the NFD of X walked by
/// UTS #10 S2.1 over the mappings these rules have built so far, else the root's (<see cref="BuiltSoFar"/> — the
/// same read an extension and a prefix take, as ICU's CollationBuilder does); a relation of strength N (<c>&lt;</c>=1, <c>&lt;&lt;</c>=2, <c>&lt;&lt;&lt;</c>=3) takes the
/// last element of the position and gives the tailored string a copy of it whose level-N weight is a NEW slot inserted
/// immediately after the anchor's on that level's line and whose lower levels are the common weights; <c>=</c> copies
/// the position unchanged, and <c>&lt;&lt;&lt;&lt;</c> copies it with a NEW slot after the anchor's on the fourth line
/// (<see cref="CollationElement.Quaternary"/>: 0 on every root element); <c>[before N]</c> resets to the slot just before X's;
/// <c>/extension</c> appends the extension's elements to the tailored string (never to the position the next
/// relation of the chain is placed after); <c>prefix|string</c> becomes the contraction prefix+string
/// (with the prefix's own elements first — the same order for every text). When all rules are in, each line is
/// NUMBERED: a slot inserted between two adjacent root weights takes one of the free values between them (the root's
/// primaries are spaced 16 apart for exactly this), and where more slots were inserted than the gap holds, every
/// higher root weight is shifted up — the RENUMBERING the table records in its <see cref="WeightMap"/>s. A
/// <c>[reorder …]</c> then permutes the reordering groups' tiles of the primary line. The result is a
/// <see cref="TailoringPlan"/> and <see cref="CollationTable.Rebuild"/>.</para>
/// <para><b>Fidelity.</b> Everything CLDR release-48-2's files use is applied: the five operators, starred relations
/// with ranges, prefixes, extensions, contractions, <c>[before 1/2/3]</c>, every logical reset position,
/// <c>[import]</c>, <c>[suppressContractions]</c>, <c>[reorder]</c>, and the settings <c>strength</c>,
/// <c>alternate</c>, <c>maxVariable</c>, <c>caseFirst</c>, <c>backwards 2</c>, <c>normalization</c>, and the
/// quaternary (<c>&lt;&lt;&lt;&lt;</c>) relation. Not implemented and reported: <c>hiraganaQ</c>.</para>
/// </summary>
public static class CldrTailoringBuilder
{
    /// <summary>Build the tailored table and options for what <see cref="CldrLocaleLoader.ResolveCollation"/> selected.</summary>
    /// <param name="selection">The selected collation (null collation → the root order with the tag's settings).</param>
    /// <param name="name">The name the resulting table carries (the locale tag).</param>
    /// <param name="baseTable">The table to tailor (the root by default).</param>
    public static CldrBuildResult Build(CldrCollationSelection selection, string name, CollationTable? baseTable = null)
    {
        ArgumentNullException.ThrowIfNull(selection);
        var table = baseTable ?? CollationTable.Root;
        var b = new Builder(table, name);
        return b.Run(selection);
    }

    // =========================================================================================================
    // The weight lines and slots
    // =========================================================================================================

    /// <summary>One inserted or root position on a weight line.</summary>
    private sealed class Slot
    {
        public required Line Line { get; init; }
        /// <summary>The root-scale weight this slot IS (a root slot) or is anchored after (an inserted slot).</summary>
        public required int Root { get; init; }
        public bool IsInserted { get; init; }
        public LinkedListNode<Slot>? Node { get; set; }
        /// <summary>Assigned by <see cref="Line.Number"/> (this table's scale, before any reordering).</summary>
        public int Value { get; set; } = -1;
        public bool IsZero => !IsInserted && Root == 0;
    }

    /// <summary>The ordered weights of one level: the root's distinct weights plus the slots inserted after them.</summary>
    private sealed class Line
    {
        private readonly int[] _roots;                                          // ascending, includes 0; shared (BaseRoots), never written
        private readonly Dictionary<int, Slot> _rootSlots = new();
        private readonly Dictionary<int, LinkedList<Slot>> _after = new();      // anchor root value → inserted slots after it, in order
        private readonly int _fixedRoom;                                        // room after an anchor that is not a root (implicit BBBB)
        private readonly int _reservedTop;                                      // values at the top of each root's gap that no inserted slot takes

        /// <param name="reservedTop">How many values just below each root stay free of inserted slots. The primary line
        /// reserves one: <c>root − 1</c> is where <see cref="CollationTable.NumericLead"/> stands under the digit zero,
        /// a primary no character may carry.</param>
        public Line(int level, int[] roots, int fixedRoom, int reservedTop = 0)
        {
            Level = level;
            _roots = roots;
            _fixedRoom = fixedRoom;
            _reservedTop = reservedTop;
        }

        public int Level { get; }
        public int InsertedCount { get; private set; }
        public bool Overflowed { get; private set; }

        /// <summary>The root weights of the line, ascending (the shared array: read, never written).</summary>
        public int[] Roots => _roots;

        public Slot RootSlot(int value)
        {
            if (!_rootSlots.TryGetValue(value, out var s))
                _rootSlots[value] = s = new Slot { Line = this, Root = value };
            return s;
        }

        /// <summary>A NEW slot immediately after <paramref name="anchor"/>.</summary>
        public Slot InsertAfter(Slot anchor)
        {
            var slot = new Slot { Line = this, Root = anchor.Root, IsInserted = true };
            if (!_after.TryGetValue(anchor.Root, out var list)) _after[anchor.Root] = list = new LinkedList<Slot>();
            slot.Node = anchor.IsInserted ? list.AddAfter(anchor.Node!, slot) : list.AddFirst(slot);
            InsertedCount++;
            return slot;
        }

        /// <summary>The slot just BEFORE <paramref name="s"/> on this line (for <c>[before N]</c>).</summary>
        public Slot Predecessor(Slot s)
        {
            if (s.IsInserted)
                return s.Node!.Previous is { } prev ? prev.Value : RootSlot(s.Root);
            int i = Array.BinarySearch(_roots, s.Root);
            if (i < 0) i = ~i;   // an anchor that is not a root: the previous root
            // A rule-level problem (the reset names the first weight of its level): Run's per-rule catch skips the rule
            // and lists it, like every other rule the builder cannot apply (kb/Work PB2773).
            if (i <= 0) throw new NotSupportedException($"[before {Level}] has no position before weight {s.Root:X}");
            int prevRoot = _roots[i - 1];
            return _after.TryGetValue(prevRoot, out var list) && list.Last is { } last ? last.Value : RootSlot(prevRoot);
        }

        /// <summary>Assign final values: root weights keep their value unless an overflowed gap below them forced a
        /// shift; inserted slots take the values after their anchor. Returns the root → new mapping (null when identity).
        /// SPARSE: only the anchors that have slots inserted after them, and the root slots the rules created, are
        /// visited — the line itself (tens of thousands of weights) is walked only when a gap overflowed and the whole
        /// map must be written.</summary>
        public WeightMap? Number()
        {
            // The anchors that are roots, ascending; each overflow shifts every root above its anchor.
            var anchors = new List<(int Index, int Root, LinkedList<Slot> Slots)>();
            foreach (var (anchor, list) in _after)
            {
                if (list.Count == 0) continue;
                int at = Array.BinarySearch(_roots, anchor);
                if (at >= 0) anchors.Add((at, anchor, list));
            }
            anchors.Sort((x, y) => x.Index.CompareTo(y.Index));
            var shifts = new List<(int FromIndex, long Shift)>();       // the shift in force from this root index on
            long shift = 0;
            foreach (var (index, root, list) in anchors)
            {
                int value = (int)(root + shift);
                long gap = (index + 1 < _roots.Length ? _roots[index + 1] - root : long.MaxValue / 4) - _reservedTop;
                int v = value;
                foreach (var slot in list) slot.Value = ++v;
                if (list.Count >= gap)
                {
                    Overflowed = true;
                    shift += list.Count - gap + 1;
                    shifts.Add((index + 1, shift));
                }
            }
            // Anchors that are not roots (implicit BBBB weights): fixed room, never shift anything.
            foreach (var (anchor, list) in _after)
            {
                if (Array.BinarySearch(_roots, anchor) >= 0) continue;
                if (list.Count > _fixedRoom)
                    throw new NotSupportedException($"more than {_fixedRoom} relations after an implicit-weight position (level {Level}) are not supported");
                int v = anchor;
                foreach (var slot in list) slot.Value = ++v;
            }
            // The root slots the rules created: a root keeps its value plus the shift in force at it; a weight that is not
            // on the line (the implicit BBBB weights, read from a reset) keeps its value.
            foreach (var slot in _rootSlots.Values)
            {
                int at = shifts.Count == 0 ? -1 : Array.BinarySearch(_roots, slot.Root);
                slot.Value = at < 0 ? slot.Root : (int)(slot.Root + ShiftAt(shifts, at));
            }
            if (shifts.Count == 0) return null;
            var to = new int[_roots.Length];
            int s = 0;
            long current = 0;
            for (int i = 0; i < _roots.Length; i++)
            {
                while (s < shifts.Count && shifts[s].FromIndex <= i) current = shifts[s++].Shift;
                to[i] = (int)(_roots[i] + current);
            }
            return new WeightMap((int[])_roots.Clone(), to);
        }

        private static long ShiftAt(List<(int FromIndex, long Shift)> shifts, int rootIndex)
        {
            long shift = 0;
            foreach (var (from, amount) in shifts)
            {
                if (from > rootIndex) break;
                shift = amount;
            }
            return shift;
        }
    }

    /// <summary>The distinct weights of each level of one base table's pool (and the implicit AAAA primaries), ascending
    /// with 0 included — the "roots" the lines are built on. They are a property of the BASE table, which every
    /// build of every locale shares, so they are computed once per table instead of by a scan of its 41,724 elements
    /// on every build (kb/Work PB107: that scan was a third of a locale's build).</summary>
    private sealed record BaseRoots(int[] Primaries, int[] Secondaries, int[] Tertiaries, int[] Quaternaries);

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<CollationTable, BaseRoots> s_baseRoots = new();

    private static BaseRoots RootsOf(CollationTable table) => s_baseRoots.GetValue(table, static t =>
    {
        int shift = t.PrimaryShift;
        var primaries = new HashSet<int>();
        var secondaries = new HashSet<int>();
        var tertiaries = new HashSet<int>();
        var quaternaries = new HashSet<int>();
        foreach (var e in t.Pool)
        {
            if (e.Primary != 0 && !CollationTable.IsImplicitSecond(e, shift)) primaries.Add(e.Primary);
            if (e.Secondary != 0) secondaries.Add(e.Secondary);
            if (e.Tertiary != 0) tertiaries.Add(e.Tertiary);
            if (e.Quaternary != 0) quaternaries.Add(e.Quaternary);
        }
        // The implicit AAAA primaries are computed, not pooled: make them roots so a reset on a Han/Tangut character,
        // [last regular], [first implicit] … can anchor an insertion, and so the map covers them (GetImplicit maps AAAA).
        for (int aaaa = ImplicitFirst; aaaa <= ImplicitLastAaaa; aaaa++)
            primaries.Add(t.PrimaryMap is { } m ? m.Map(aaaa << shift) : aaaa << shift);
        return new BaseRoots(Sorted(primaries), Sorted(secondaries), Sorted(tertiaries), Sorted(quaternaries));

        static int[] Sorted(HashSet<int> weights)
        {
            weights.Add(0);
            var array = new int[weights.Count];
            weights.CopyTo(array);
            Array.Sort(array);
            return array;
        }
    });

    private const int ImplicitFirst = 0xFB00, ImplicitLastAaaa = 0xFBC0 + (0x10FFFF >> 15);   // FBE1

    /// <summary>A collation element while the rules are being processed: three slots plus the flags.</summary>
    private readonly record struct BuildElement(Slot P, Slot S, Slot T, Slot Q, bool Variable, ElementCase Case)
    {
        public Slot At(int level) => level switch { 1 => P, 2 => S, 3 => T, _ => Q };
        public bool IgnorableAt(int level) => At(level).IsZero;
        public BuildElement With(int level, Slot slot) => level switch
        {
            1 => this with { P = slot },
            2 => this with { S = slot },
            3 => this with { T = slot },
            _ => this with { Q = slot },
        };
    }

    // =========================================================================================================
    // The mappings built so far
    // =========================================================================================================

    /// <summary>
    /// The mappings the rules have built SO FAR, over the base table, in the shape the UTS #10 S2.1 walk reads — what
    /// a reset string (<c>&amp;X</c>), an extension (<c>x/y</c>) and a prefix (<c>p|x</c>) take their current elements
    /// from. ICU's CollationBuilder reads each of the three as <c>getCEs</c> of the string's NFD over the data built so
    /// far (<c>addReset</c>, <c>addRelation</c>), so this does the same: the string is normalized to NFD and walked by
    /// the one <see cref="ContractionWalker"/> — the longest contraction, tailored or base, then the discontiguous
    /// extension over unblocked non-starters — and each step takes the tailored mapping when these rules made one,
    /// else the base table's. A tailored string is keyed by its NFD spelling (ICU's data builder maps the NFD string;
    /// the precomposed spellings are canonical closure's, <see cref="CollationTable.Rebuild"/>). A first code point's
    /// base contractions are copied in when it is first walked or first given a tailored contraction (ICU's
    /// <c>copyFromBaseCE32</c>), so a tailored contraction is matched beside them, longest first; the base
    /// contractions of a <c>[suppressContractions]</c> code point are never copied.
    /// </summary>
    private sealed class BuiltSoFar(CollationTable baseTable, Func<CollationElement, BuildElement> fromBase)
    {
        private readonly List<BuildElement> _pool = [];
        private readonly Dictionary<int, (int Offset, int Count)> _singles = new();
        private readonly Dictionary<int, CollationTable.Contraction[]?> _contractions = new();   // null: none
        private HashSet<int>? _suppressed;

        /// <summary>The code points whose base contractions <c>[suppressContractions]</c> removes. Set before the first
        /// rule, as CLDR writes the setting before its rules.</summary>
        public void Suppress(IReadOnlyList<int>? codePoints) =>
            _suppressed = codePoints is { Count: > 0 } ? new HashSet<int>(codePoints) : null;

        /// <summary>Record that these rules map <paramref name="key"/> (a relation's string, a prefix context's
        /// prefix+string) to <paramref name="elements"/>, replacing any earlier mapping of its NFD spelling. A key whose
        /// NFD spelling the built table does not map (<see cref="CollationTable.MapsItsNfdSpelling"/>: one led by a
        /// Hangul syllable) is never met by a walk over NFD text, so it is not recorded.</summary>
        public void Map(string key, List<BuildElement> elements)
        {
            if (!CollationTable.MapsItsNfdSpelling(CodePoints(key))) return;
            var cps = CodePoints(Nfd(key));
            int offset = _pool.Count;
            _pool.AddRange(elements);
            if (cps.Length == 1)
            {
                _singles[cps[0]] = (offset, elements.Count);
                return;
            }
            var existing = ContractionsStartingWith(cps[0]) ?? [];
            _contractions[cps[0]] = CollationTable.WithContraction(existing, cps[1..], offset, elements.Count, out _);
        }

        /// <summary>The current elements of <paramref name="text"/>: its NFD walked over the mappings built so far.
        /// Never empty for a non-empty text (a completely ignorable code point yields one all-zero element).</summary>
        public List<BuildElement> ElementsOf(string text)
        {
            string nfd = Nfd(text);
            var result = new List<BuildElement>(nfd.Length + 1);
            var walker = new ContractionWalker(nfd, baseTable);
            while (walker.NextCodePoint(out int cp, out int after))
            {
                if (ContractionsStartingWith(cp) is { } candidates && walker.TryMatchContraction(after, candidates, out var c))
                    result.AddRange(CollectionsMarshal.AsSpan(_pool).Slice(c.Offset, c.Count));
                else if (_singles.TryGetValue(cp, out var single))
                    result.AddRange(CollectionsMarshal.AsSpan(_pool).Slice(single.Offset, single.Count));
                else
                    foreach (var e in baseTable.GetElements(cp).Span) result.Add(fromBase(e));
            }
            return result;
        }

        /// <summary>The NFD of a string (the string itself when nothing in it decomposes or combines).</summary>
        private string Nfd(string s) =>
            Normalizer.NeedsNfd(s, baseTable, forIdentical: true) ? Normalizer.ToNfd(s, baseTable) : s;

        /// <summary>The contractions beginning with <paramref name="codePoint"/>, longest first, or null: the
        /// tailored ones and the base table's (copied in on first use).</summary>
        private CollationTable.Contraction[]? ContractionsStartingWith(int codePoint)
        {
            if (_contractions.TryGetValue(codePoint, out var known)) return known;
            CollationTable.Contraction[]? copied = null;
            if (_suppressed?.Contains(codePoint) != true && baseTable.ContractionsStartingWith(codePoint) is { } fromTable)
            {
                copied = new CollationTable.Contraction[fromTable.Length];
                for (int i = 0; i < fromTable.Length; i++)
                {
                    var b = fromTable[i];
                    int offset = _pool.Count;
                    foreach (var e in baseTable.Slice(b.Offset, b.Count)) _pool.Add(fromBase(e));
                    copied[i] = new CollationTable.Contraction(b.Rest, offset, b.Count);
                }
            }
            _contractions[codePoint] = copied;
            return copied;
        }
    }

    // =========================================================================================================
    // The builder
    // =========================================================================================================

    private sealed class Builder
    {
        private const int CommonSecondary = 0x0020, CommonTertiary = 0x0002;
        private const int HanImplicitFirst = 0xFB40;

        private readonly CollationTable _base;
        private readonly string _name;
        private readonly int _shift;
        private readonly Line _p, _s, _t, _q;
        private readonly Dictionary<string, List<BuildElement>> _tailored = new(StringComparer.Ordinal);
        private readonly List<string> _tailoredOrder = [];
        private readonly Dictionary<string, (string Prefix, List<BuildElement> Elements)> _prefixed = new(StringComparer.Ordinal);   // key → its `p|x` parts
        private readonly BuiltSoFar _built;
        private readonly List<string> _unsupported = [];
        private readonly List<string> _notes = [];
        private readonly HashSet<string> _importing = new(StringComparer.OrdinalIgnoreCase);
        private List<BuildElement> _position = [];
        private int _rulesApplied, _prefixContexts;
        private (int First, int Last) _variableRootRange;      // root scale: space.first .. punct.last

        public Builder(CollationTable baseTable, string name)
        {
            _base = baseTable;
            _name = name;
            _shift = baseTable.PrimaryShift;
            var roots = RootsOf(baseTable);
            _p = new Line(1, roots.Primaries, fixedRoom: (1 << _shift) - 1, reservedTop: 1);
            _s = new Line(2, roots.Secondaries, fixedRoom: 0);
            _t = new Line(3, roots.Tertiaries, fixedRoom: 0);
            _q = new Line(4, roots.Quaternaries, fixedRoom: 0);
            int spaceFirst = 0, punctLast = 0;
            if (baseTable.TryGetReorderGroup("space", out var sp)) spaceFirst = sp.FirstPrimary;
            if (baseTable.TryGetReorderGroup("punct", out var pu)) punctLast = pu.LastPrimary;
            _variableRootRange = (spaceFirst, punctLast);
            _built = new BuiltSoFar(baseTable, FromBase);
        }

        /// <summary>A base-table element as a build element: its weights as the lines' root slots.</summary>
        private BuildElement FromBase(CollationElement e) =>
            new(_p.RootSlot(e.Primary), _s.RootSlot(e.Secondary), _t.RootSlot(e.Tertiary), _q.RootSlot(e.Quaternary), e.IsVariable, e.Case);

        /// <summary>A base-scale primary of the base table (the base is normally the root; a base that is itself
        /// renumbered maps its own AAAA values through its map, as GetImplicit does).</summary>
        private int Mapped(int rootPrimary) => _base.PrimaryMap is { } m ? m.Map(rootPrimary) : rootPrimary;


        public CldrBuildResult Run(CldrCollationSelection selection)
        {
            var settings = selection.Settings;
            _unsupported.AddRange(selection.Unsupported);
            if (selection.Collation is { } collation)
            {
                var (rules, effectiveSettings) = Expand(collation, selection.Found?.Tag ?? "root");
                settings = effectiveSettings.Merge(selection.Tag.Settings);
                _built.Suppress(settings.SuppressContractions);
                foreach (string u in collation.Unsupported) _unsupported.Add(u);
                foreach (var rule in rules)
                {
                    try
                    {
                        switch (rule)
                        {
                            case CldrReset r: Reset(r); _rulesApplied++; break;
                            case CldrRelation rel: Relation(rel); _rulesApplied++; break;
                        }
                    }
                    catch (NotSupportedException ex)
                    {
                        _unsupported.Add($"line {rule.Line}: {rule} — {ex.Message}");
                    }
                }
            }
            _unsupported.AddRange(settings.UnsupportedSettings());
            if (_prefixContexts > 0) _notes.Add($"{_prefixContexts} prefix-context relation(s) (prefix|string) represented as contractions of prefix+string");
            var options = settings.ToOptions(CollationOptions.Default);

            if (_tailored.Count == 0 && settings.Reorder is null && settings.SuppressContractions is null)
                return new CldrBuildResult(_base, options, _unsupported, _notes, _rulesApplied);

            // ---- number the lines --------------------------------------------------------------------------
            // Numbering is the whole collation's, not one rule's: a line that cannot be numbered leaves no table to
            // build, so it is the resolution failure (kb/Work PB2773), never an exception of another type.
            WeightMap? pMap, sMap, tMap, qMap;
            try
            {
                pMap = _p.Number();
                sMap = _s.Number();
                tMap = _t.Number();
                qMap = _q.Number();
            }
            catch (NotSupportedException ex)
            {
                throw new LocaleCollationException(selection.Found?.Source ?? _name, null, ex.Message, ex);
            }
            Func<int, int> mapP = pMap is null ? x => x : pMap.Map;
            Func<int, int> mapS = sMap is null ? x => x : sMap.Map;
            Func<int, int> mapT = tMap is null ? x => x : tMap.Map;
            Func<int, int> mapQ = qMap is null ? x => x : qMap.Map;

            // ---- reordering: a permutation of the groups' tiles on the numbered primary scale ---------------
            var groups = _base.ReorderGroups;
            ReorderGroup[]? newGroups = null;
            Func<int, int> perm = x => x;
            if (settings.Reorder is { } codes)
            {
                var order = ReorderOrder(codes, groups);
                if (order is not null)
                {
                    // Tiles in the numbered scale: group i runs from its first primary up to the next group's first − 1,
                    // EXCEPT that everything between the last regular script's last primary and the Han implicit range —
                    // the space CLDR reserves for tailoring, where a `&[last regular]<…` pinyin/stroke/zhuyin order puts
                    // the Han characters — belongs to the Hani tile, so those tailored primaries move WITH Hani.
                    var tiles = new (int First, int Last)[groups.Count];
                    for (int i = 0; i < groups.Count; i++)
                    {
                        bool nextIsHani = i + 1 < groups.Count && groups[i + 1].Code == "Hani";
                        int first = i > 0 && groups[i].Code == "Hani" ? mapP(groups[i - 1].LastPrimary) + 1 : mapP(groups[i].FirstPrimary);
                        int last = i + 1 < groups.Count
                            ? (nextIsHani ? mapP(groups[i].LastPrimary) : mapP(groups[i + 1].FirstPrimary) - 1)
                            : mapP(groups[i].LastPrimary);
                        tiles[i] = (first, last);
                    }
                    var newFirst = new int[groups.Count];
                    int cursor = tiles[0].First;
                    foreach (int gi in order)
                    {
                        newFirst[gi] = cursor;
                        cursor += tiles[gi].Last - tiles[gi].First + 1;
                    }
                    bool identity = true;
                    for (int i = 0; i < groups.Count; i++) if (newFirst[i] != tiles[i].First) { identity = false; break; }
                    if (!identity)
                    {
                        var tilesCopy = tiles;
                        var firstCopy = newFirst;
                        perm = x =>
                        {
                            for (int i = 0; i < tilesCopy.Length; i++)
                                if (x >= tilesCopy[i].First && x <= tilesCopy[i].Last) return firstCopy[i] + (x - tilesCopy[i].First);
                            return x;
                        };
                        newGroups = new ReorderGroup[groups.Count];
                        int k = 0;
                        foreach (int gi in order)
                            newGroups[k++] = new ReorderGroup(groups[gi].Codes, perm(mapP(groups[gi].FirstPrimary)), perm(mapP(groups[gi].LastPrimary)));
                    }
                }
            }
            bool primaryChanged = pMap is not null || newGroups is not null;
            Func<int, int> finalP = primaryChanged ? x => perm(mapP(x)) : x => x;

            // The root → this-table maps recorded on the table (a later .tailor layer translates through them).
            WeightMap? primaryMap = null;
            if (primaryChanged)
            {
                var from = _p.Roots;                  // the base table's primary roots, 0 and the implicit AAAA weights included
                var to = new int[from.Length];
                for (int i = 0; i < to.Length; i++) to[i] = finalP(from[i]);
                primaryMap = new WeightMap((int[])from.Clone(), to);
            }
            if (newGroups is null && pMap is not null)
            {
                newGroups = new ReorderGroup[groups.Count];
                for (int i = 0; i < groups.Count; i++)
                    newGroups[i] = new ReorderGroup(groups[i].Codes, mapP(groups[i].FirstPrimary), mapP(groups[i].LastPrimary));
            }

            // ---- the plan ------------------------------------------------------------------------------------
            var suppress = settings.SuppressContractions is { Count: > 0 } sc ? new HashSet<int>(sc) : null;
            var entries = new List<(int[] CodePoints, CollationElement[] Elements)>(_tailored.Count);
            foreach (string key in _tailoredOrder)
            {
                var cps = CodePoints(key);
                if (suppress is not null && cps.Length > 1 && suppress.Contains(cps[0])) continue;
                var ces = PrefixedElements(key) ?? _tailored[key];
                var elements = new CollationElement[ces.Count];
                for (int i = 0; i < elements.Length; i++)
                {
                    var ce = ces[i];
                    elements[i] = new CollationElement(
                        ce.P.IsZero ? 0 : perm(ce.P.Value),
                        ce.S.IsZero ? 0 : ce.S.Value,
                        ce.T.IsZero ? 0 : ce.T.Value,
                        ce.Variable, ce.Case,
                        ce.Q.IsZero ? 0 : ce.Q.Value);
                }
                entries.Add((cps, elements));
            }

            ElementRemap? remap = null;
            if (primaryChanged || sMap is not null || tMap is not null || qMap is not null)
            {
                var roots = RootsOf(_base);
                remap = new ElementRemap(
                    primaryChanged ? finalP : null,
                    LookupOf(sMap, roots.Secondaries), LookupOf(tMap, roots.Tertiaries), LookupOf(qMap, roots.Quaternaries),
                    _shift);
            }
            var plan = new TailoringPlan
            {
                Name = _name,
                Description = $"{_base.Description} + CLDR {selection.Found?.Tag ?? "root"}/{selection.Type}",
                Entries = entries,
                Remap = remap,
                SuppressContractionsStartingWith = suppress,
                Groups = newGroups,
                PrimaryMap = primaryMap,
                SecondaryMap = sMap,
                TertiaryMap = tMap,
            };
            var table = _base.Rebuild(plan);   // canonically closed there, as every tailoring is
            if (table.ClosedComposites > 0) _notes.Add($"{table.ClosedComposites} precomposed character(s) re-derived from tailored components (canonical closure)");
            if (_p.Overflowed || _s.Overflowed || _t.Overflowed)
                _notes.Add($"renumbered: {(_p.Overflowed ? "primary " : "")}{(_s.Overflowed ? "secondary " : "")}{(_t.Overflowed ? "tertiary " : "")}gap(s) widened");
            return new CldrBuildResult(table, options, _unsupported, _notes, _rulesApplied);
        }

        // ---- imports ------------------------------------------------------------------------------------------

        /// <summary>The collation's rules with every <c>[import]</c> replaced by the imported collation's (recursively
        /// expanded) rules, and the effective settings (imports' first, the collation's own on top).</summary>
        private (List<CldrRule> Rules, CldrSettings Settings) Expand(CldrCollation collation, string ownerTag)
        {
            _importing.Add(ImportKey(collation, ownerTag));
            var settings = new CldrSettings();
            var rules = new List<CldrRule>();
            foreach (var rule in collation.Rules)
            {
                if (rule is CldrImportRule imp)
                {
                    // Exactly the named type along the imported locale's chain: a type the chain lacks is NOT
                    // replaced by the locale's default collation (a misspelt [import es-u-co-tradtional] would
                    // otherwise splice es/standard in silently).
                    if (CldrLocaleLoader.FindCollation(imp.Import.LocaleTag, imp.Import.Type) is not (var found, var imported))
                    {
                        _unsupported.Add($"line {rule.Line}: {imp} — no such collation; nothing imported");
                        continue;
                    }
                    // A collation already being expanded: a rule-level problem of THIS import, listed like the missing
                    // type above (it used to throw NotSupportedException past Run's per-rule catch; kb/Work PB2773).
                    if (_importing.Contains(ImportKey(imported, found.Tag)))
                    {
                        _unsupported.Add($"line {rule.Line}: {imp} — circular [import] through {ImportKey(imported, found.Tag)}; nothing imported");
                        continue;
                    }
                    var (inner, innerSettings) = Expand(imported, found.Tag);
                    rules.AddRange(inner);
                    settings = settings.Merge(innerSettings);
                    foreach (string u in imported.Unsupported) _unsupported.Add($"(imported {imp}) {u}");
                }
                else rules.Add(rule);
            }
            _importing.Remove(ImportKey(collation, ownerTag));
            return (rules, settings.Merge(collation.Settings));
        }

        /// <summary>The identity of one collation of one file, as the import-cycle check compares it.</summary>
        private static string ImportKey(CldrCollation collation, string ownerTag) =>
            ownerTag + "/" + collation.Type + "/" + (collation.Alt ?? "");

        // ---- resets and relations ------------------------------------------------------------------------------

        private void Reset(CldrReset reset)
        {
            List<BuildElement> pos = reset.Position is { } sp ? [SpecialPosition(sp)] : _built.ElementsOf(reset.Text!);
            if (reset.BeforeLevel > 0)
            {
                int n = reset.BeforeLevel;
                int idx = LastIndexForLevel(pos, n);
                var slot = pos[idx].At(n);
                var line = LineOf(n);
                pos = pos.Take(idx + 1).ToList();
                pos[idx] = pos[idx].With(n, line.Predecessor(slot));
            }
            _position = pos;
        }

        private void Relation(CldrRelation rel)
        {
            if (_position.Count == 0) throw new NotSupportedException("no reset position");
            List<BuildElement> ces;
            if (rel.Strength == CldrRelationStrength.Identity) ces = new List<BuildElement>(_position);
            else
            {
                int n = (int)rel.Strength;
                // A quaternary difference changes the last element's level-4 weight and nothing above it: the string is
                // the position's copy, its fourth-level slot a new one after the anchor's (UTS #35 Part 5).
                int idx = n == 4 ? _position.Count - 1 : LastIndexForLevel(_position, n);
                var anchor = _position[idx];
                var line = LineOf(n);
                var slot = line.InsertAfter(anchor.At(n));
                var ce = anchor.With(n, slot);
                if (n < 2) ce = ce with { S = _s.RootSlot(CommonSecondary) };
                if (n < 3) ce = ce with { T = _t.RootSlot(CommonTertiary) };
                if (n == 1) ce = ce with { Variable = IsVariableRoot(anchor.P.Root) };
                if (n < 4) ce = ce with { Case = CaseOf(rel.Text) };
                ces = _position.Take(idx).ToList();
                ces.Add(ce);
            }
            // The position moves to the tailored string's OWN relation elements: neither the extension nor a prefix
            // is part of it (UTS #35 Part 5 "Expansions": in &T<<þ/h<<<Þ/h the second relation is placed after þ's
            // T-secondary element, not after its trailing h; ICU's CollationBuilder restores the element count it had
            // before the extension once the relation is added). The extension is appended to a COPY, so the
            // position never carries it into the next relation.
            _position = ces;
            if (rel.Extension is { } ext) ces = [.. ces, .. _built.ElementsOf(ext)];
            string key = rel.Text;
            if (rel.Prefix is { } prefix)
            {
                // `p|x` is x AFTER p: the contraction p+x, whose elements are p's followed by x's. p's are read when the
                // table is planned (PrefixedElements), not now — a later rule may still re-place p itself (ja.xml puts
                // ァ after the rule `ァ|ー`), and ICU matches the prefix against p's FINAL mapping. The mappings-so-far
                // take the elements as they stand now, which is all a reset or an extension in between can read.
                _prefixContexts++;
                key = prefix + rel.Text;
                _prefixed[key] = (prefix, ces);
                var withPrefix = _built.ElementsOf(prefix);
                withPrefix.AddRange(ces);
                ces = withPrefix;
            }
            else _prefixed.Remove(key);
            if (!_tailored.ContainsKey(key)) _tailoredOrder.Add(key);
            _tailored[key] = ces;
            _built.Map(key, ces);
        }

        /// <summary>The elements of a `p|x` relation's contraction p+x once every rule is in: p's FINAL elements, then x's;
        /// null when <paramref name="key"/> is not a prefix context.</summary>
        private List<BuildElement>? PrefixedElements(string key)
        {
            if (!_prefixed.TryGetValue(key, out var parts)) return null;
            var ces = _built.ElementsOf(parts.Prefix);
            ces.AddRange(parts.Elements);
            return ces;
        }

        /// <summary>The index of the element a level-N relation modifies: the LAST element, unless it is ignorable at
        /// level N and an earlier element is not (then that one — the trailing ignorables are dropped); when every
        /// element is ignorable at N (a reset on a combining mark with <c>&lt;</c>, or a tertiary-ignorable position
        /// with <c>&lt;&lt;&lt;</c>) the last element, whose zero weight anchors the insertion at the line's start.</summary>
        private static int LastIndexForLevel(List<BuildElement> ces, int level)
        {
            int last = ces.Count - 1;
            if (!ces[last].IgnorableAt(level)) return last;
            for (int i = last - 1; i >= 0; i--)
                if (!ces[i].IgnorableAt(level)) return i;
            return last;
        }

        private Line LineOf(int level) => level switch { 1 => _p, 2 => _s, 3 => _t, _ => _q };

        /// <summary>A map of one level as a direct lookup array over that level's weights (they are small: secondaries
        /// reach 0x127, tertiaries 0x1E) — the remap of the base pool reads it once per element instead of searching.</summary>
        private static int[]? LookupOf(WeightMap? map, int[] levelRoots)
        {
            if (map is null) return null;
            var lookup = new int[levelRoots[^1] + 1];
            foreach (int w in levelRoots) lookup[w] = w == 0 ? 0 : map.Map(w);
            return lookup;
        }

        private bool IsVariableRoot(int rootPrimary) => rootPrimary >= _variableRootRange.First && rootPrimary <= _variableRootRange.Last;

        /// <summary>The case bits of a tailored string (ICU's rule): Upper when every cased letter is uppercase, Lower when
        /// every cased letter is lowercase (or there is none), Mixed otherwise ("Aa", "Cs").</summary>
        private static ElementCase CaseOf(string text)
        {
            bool upper = false, lower = false;
            foreach (var r in text.EnumerateRunes())
            {
                switch (Rune.GetUnicodeCategory(r))
                {
                    case UnicodeCategory.UppercaseLetter: upper = true; break;
                    case UnicodeCategory.TitlecaseLetter: upper = lower = true; break;
                    case UnicodeCategory.LowercaseLetter: lower = true; break;
                }
            }
            return upper && lower ? ElementCase.Mixed : upper ? ElementCase.Upper : ElementCase.Lower;
        }

        /// <summary>The element of a logical reset position (UTS #35 Part 5 "Logical Reset Positions"), read from the base table.</summary>
        private BuildElement SpecialPosition(CldrSpecialPosition position)
        {
            var pool = _base.Pool;
            BuildElement Make(int p, int s, int t) => new(_p.RootSlot(p), _s.RootSlot(s), _t.RootSlot(t), _q.RootSlot(0), IsVariableRoot(p), ElementCase.Lower);
            switch (position)
            {
                case CldrSpecialPosition.FirstTertiaryIgnorable:
                case CldrSpecialPosition.LastTertiaryIgnorable:
                    return Make(0, 0, 0);
                case CldrSpecialPosition.FirstSecondaryIgnorable:
                case CldrSpecialPosition.LastSecondaryIgnorable:
                {
                    int min = int.MaxValue, max = 0;
                    foreach (var e in pool)
                        if (e.Primary == 0 && e.Secondary == 0 && e.Tertiary != 0) { min = Math.Min(min, e.Tertiary); max = Math.Max(max, e.Tertiary); }
                    if (max == 0) return Make(0, 0, 0);   // none in the root: the tertiary-ignorable position
                    return Make(0, 0, position == CldrSpecialPosition.FirstSecondaryIgnorable ? min : max);
                }
                case CldrSpecialPosition.FirstPrimaryIgnorable:
                case CldrSpecialPosition.LastPrimaryIgnorable:
                {
                    int min = int.MaxValue, max = 0;
                    foreach (var e in pool)
                        if (e.Primary == 0 && e.Secondary != 0) { min = Math.Min(min, e.Secondary); max = Math.Max(max, e.Secondary); }
                    return Make(0, position == CldrSpecialPosition.FirstPrimaryIgnorable ? min : max, CommonTertiary);
                }
                case CldrSpecialPosition.FirstVariable:
                    return Make(_variableRootRange.First, CommonSecondary, CommonTertiary);
                case CldrSpecialPosition.LastVariable:
                    return Make(_variableRootRange.Last, CommonSecondary, CommonTertiary);
                case CldrSpecialPosition.FirstRegular:
                    return Make(_base.TryGetReorderGroup("symbol", out var sym) ? sym.FirstPrimary : _variableRootRange.Last + 1, CommonSecondary, CommonTertiary);
                case CldrSpecialPosition.LastRegular:
                {
                    // The last primary below the Han implicit range: the group before Hani (Khitan Small Script).
                    int last = 0;
                    var groups = _base.ReorderGroups;
                    for (int i = 0; i < groups.Count; i++)
                        if (groups[i].Code == "Hani") { last = i > 0 ? groups[i - 1].LastPrimary : groups[i].FirstPrimary - 1; break; }
                    if (last == 0) last = Mapped((HanImplicitFirst - 1) << _shift);
                    return Make(last, CommonSecondary, CommonTertiary);
                }
                case CldrSpecialPosition.FirstImplicit:
                    return Make(Mapped(HanImplicitFirst << _shift), CommonSecondary, CommonTertiary);
                case CldrSpecialPosition.LastImplicit:
                    return Make(Mapped(ImplicitLastAaaa << _shift), CommonSecondary, CommonTertiary);
                case CldrSpecialPosition.FirstTrailing:
                {
                    int limit = Mapped(ImplicitLastAaaa << _shift), min = int.MaxValue;
                    foreach (var e in pool) if (e.Primary > limit && !CollationTable.IsImplicitSecond(e, _shift)) min = Math.Min(min, e.Primary);
                    return Make(min == int.MaxValue ? limit + 1 : min, CommonSecondary, CommonTertiary);
                }
                default:   // LastTrailing
                {
                    int max = 0;
                    foreach (var e in pool) if (!CollationTable.IsImplicitSecond(e, _shift)) max = Math.Max(max, e.Primary);
                    return Make(max, CommonSecondary, CommonTertiary);
                }
            }
        }

        // ---- reordering ------------------------------------------------------------------------------------------

        /// <summary>The group indices in the order a <c>[reorder …]</c> asks for (UTS #35 Part 5 "Collation
        /// Reordering"): the special groups not named stay first in their default order; the named codes follow in
        /// order; <c>others</c> stands for every group not named (default order), at its place or at the end.
        /// Null = no reordering (an empty list, or nothing that names a group).</summary>
        private int[]? ReorderOrder(IReadOnlyList<string> codes, IReadOnlyList<ReorderGroup> groups)
        {
            if (codes.Count == 0) return null;
            var listed = new List<int>();
            int othersAt = -1;
            var seen = new HashSet<int>();
            foreach (string code in codes)
            {
                if (code.Equals("others", StringComparison.OrdinalIgnoreCase)) { othersAt = listed.Count; listed.Add(-1); continue; }
                if (code is "Zyyy" or "Zinh" or "Zzzz")
                {
                    _unsupported.Add($"[reorder {code}]: Common/Inherited/Unknown are not reorderable groups");
                    continue;
                }
                if (!_base.TryGetReorderGroup(code, out var g))
                {
                    _unsupported.Add($"[reorder {code}]: no such reordering group in the derived table");
                    continue;
                }
                int gi = IndexOfGroup(groups, g);
                if (seen.Add(gi)) listed.Add(gi);
            }
            if (listed.Count == 0) return null;
            var order = new List<int>();
            for (int i = 0; i < groups.Count; i++)
                if (groups[i].IsSpecial && !seen.Contains(i)) order.Add(i);
            var others = new List<int>();
            for (int i = 0; i < groups.Count; i++)
                if (!groups[i].IsSpecial && !seen.Contains(i)) others.Add(i);
            bool othersPlaced = false;
            foreach (int gi in listed)
            {
                if (gi == -1) { order.AddRange(others); othersPlaced = true; }
                else order.Add(gi);
            }
            if (!othersPlaced) order.AddRange(others);
            return order.ToArray();

            static int IndexOfGroup(IReadOnlyList<ReorderGroup> gs, ReorderGroup g)
            {
                for (int i = 0; i < gs.Count; i++) if (gs[i].FirstPrimary == g.FirstPrimary) return i;
                return 0;
            }
        }
    }

    // =========================================================================================================
    // Text helpers
    // =========================================================================================================

    /// <summary>The code points of a string; an unpaired surrogate is its own code point (as the walk reads it).</summary>
    private static int[] CodePoints(string s)
    {
        var list = new List<int>(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            if (char.IsHighSurrogate(s[i]) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1])) { list.Add(char.ConvertToUtf32(s[i], s[i + 1])); i++; }
            else list.Add(s[i]);
        }
        return list.ToArray();
    }
}
