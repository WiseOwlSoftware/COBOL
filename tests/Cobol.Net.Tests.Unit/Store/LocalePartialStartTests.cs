// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolNet.Runtime;
using CobolNet.Runtime.Collation;
using CobolNet.Runtime.IO.Store;
using Xunit;
using Fault = CobolNet.Tests.Unit.Store.MemoryPageMedium.Fault;

namespace CobolNet.Tests.Unit.Store;

/// <summary>
/// ⛔ A START'S PARTIAL KEY UNDER A LOCALE-BASED SEQUENCE IS THE SEQUENTIAL SEARCH OF §14.9.41.4 GR17, BOUNDED AT THE
/// PRIMARY LEVEL (docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.4; kb/Work PB3332; review w1122-a R2-1, R3-1).
/// GR17 c) and d) truncate each stored key to the partial key's length and search the file sequentially, forward for
/// EQUAL and the GREATER relations and in reverse for the LESS ones; §8.8.4.2.11 compares the truncation by the locale's
/// multi-level algorithm over its trimmed operands. The tree starts that search at the bound its two high-water marks
/// make exact and leaves it at the end of the run of entries that tie the bound.
/// <para>THE MODEL is GR17 as written — <c>FindIndex</c> / <c>FindLastIndex</c> of the first (or last) sorted key whose
/// truncation satisfies the relation — and the engine must agree with it for every relation, every partial-key length,
/// over key sets built from the collator's real inventory: accent and case variants of one base, embedded controls (a
/// completely ignorable character) and soft hyphens, interior and trailing spaces, tabs (which weigh below a space),
/// expansions ("æ", "ß"), and the contractions of Czech ("ch") and Danish ("aa"), truncated in the middle. A divergence
/// is a wrong bound, never a wrong model.</para>
/// <para>THE PAGES: ordinary text has no deficit and no overhang, so its partial key reads the tree's depth plus the
/// leaves of the run it walks — never the file; a probe above every key reads the depth alone. THE MARKS live on the
/// root page, survive CLOSE and OPEN, a process death or a refusal at every write of the statement that raises them, a
/// refused read at every read of the search, and another run unit's statement adopted between two of this one's.</para>
/// </summary>
public sealed class LocalePartialStartTests
{
    // ── The prefix shape: what a truncation can lose (Collator.PrefixShapeOf, LocaleCollation's operand rule) ─────────

    /// <summary>The shape is its definition: the largest (cut − level-1 weights before it) and the longest run between
    /// two cuts, less one. Ordinary letters weigh one primary each; a control (U+0001) and a soft hyphen are completely
    /// ignorable; "æ" and "ß" expand to two primaries; Czech "ch" and Danish "aa" are contractions of two characters
    /// into one primary; a TAB weighs like a space; §8.8.4.2.11's trailing spaces are not part of the operand.</summary>
    [Theory]
    [InlineData("root", "abc", 0, 0)]
    [InlineData("root", "a\u0001b", 1, 0)]
    [InlineData("root", "\u0001\u0001ab", 2, 0)]
    [InlineData("root", "ab\u00AD", 1, 0)]
    [InlineData("root", "æb", 0, 0)]
    [InlineData("root", "\u0001æ", 1, 0)]
    [InlineData("root", "ßa\u0001\u0001\u0001", 2, 0)]
    [InlineData("root", "a b\tc", 0, 0)]
    [InlineData("root", "ab      ", 0, 0)]
    [InlineData("root", "        ", 0, 0)]
    [InlineData("cs", "chata", 1, 1)]
    [InlineData("cs", "cesta", 0, 0)]
    [InlineData("cs", "chch", 2, 1)]
    [InlineData("da", "aab", 1, 1)]
    public void The_prefix_shape_is_what_a_truncation_can_lose(string locale, string text, int deficit, int overhang) =>
        Assert.Equal(new PrefixShape(deficit, overhang), new LocaleCollation(locale).PrefixShapeOf(text));

    /// <summary>A text the engine walks in NFD (identical strength decomposes "á"; a text with marks needing canonical
    /// reordering) is measured in positions of the text it was given: "á" is one position whose walk yields one primary,
    /// and marks reordered across each other merge their positions into one run.</summary>
    [Fact]
    public void A_text_walked_in_NFD_is_measured_in_its_own_positions()
    {
        var identical = CollationEngine.ForLocale("root", CollationStrength.Identical);
        Assert.Equal(new PrefixShape(0, 0), identical.PrefixShapeOf("áb"));
        Assert.Equal(new PrefixShape(0, 0), identical.PrefixShapeOf("ab"));
        // U+0301 (class 230) then U+0316 (class 220) reorder: the two marks share one run with the base they follow.
        Assert.Equal(new PrefixShape(2, 2), Collator.Root.PrefixShapeOf("a\u0301\u0316"));
        Assert.Equal(new PrefixShape(2, 2), Collator.Root.PrefixShapeOf("a\u0301\u0316b"));
        Assert.Equal(new PrefixShape(0, 0), Collator.Root.PrefixShapeOf(""));
    }

    /// <summary>The order's three answers: marks from the key's shape (zeros for an order whose prefixes narrow), the
    /// bound cut at the margin and before the first weight below a space's, and the bound compare monotone in the key
    /// order with a shorter run of weights below a longer one it begins.</summary>
    [Fact]
    public void The_key_order_bounds_a_partial_key_by_its_margin_and_below_a_space()
    {
        var order = new WeightedKeyOrder(null, new LocaleCollation("root"), 6);
        Assert.Equal(new TreeMarks(1, 0), order.MarksOf(Latin1("a\u0001bc  ")));
        Assert.Equal(default, KeyOrders.Ordinal.MarksOf(Latin1("a\u0001bc")));
        Assert.Equal(default, new WeightedKeyOrder(null, BTreeModelTests.ReversedAlphabet(), 6).MarksOf(Latin1("a\u0001bc")));
        Assert.Equal(new TreeMarks(1, 0), new CompositeKeyOrder(order).MarksOf(CompositeKeyOrder.Compose(Latin1("a\u0001bc"), 9)));
        var abc = order.BoundOf(Latin1("abc"), default);
        Assert.Equal(3, abc.Length);
        Assert.Equal(2, order.BoundOf(Latin1("abc"), new TreeMarks(1, 0)).Length);
        Assert.Single(order.BoundOf(Latin1("abc"), new TreeMarks(1, 1)));
        Assert.Empty(order.BoundOf(Latin1("abc"), new TreeMarks(2, 1)));
        Assert.Empty(order.BoundOf(Latin1("abc"), new TreeMarks(ushort.MaxValue, 0)));
        Assert.Equal(2, order.BoundOf(Latin1("ab\tc"), default).Length);      // a TAB weighs below a space: cut there
        Assert.Equal(4, order.BoundOf(Latin1("ab c"), default).Length);       // a space is not below itself
        Assert.Equal(3, order.BoundOf(Latin1("æbc"), default).Length);        // the margin, of the expansion's four
        Assert.Equal(0, order.CompareBound(Latin1("abcz"), abc));
        Assert.Equal(0, order.CompareBound(Latin1("ÁBCz"), abc));            // accent and case are not level 1
        Assert.True(order.CompareBound(Latin1("ab"), abc) < 0);               // a shorter run below the one it begins
        Assert.True(order.CompareBound(Latin1("abd"), abc) > 0);
        Assert.True(order.CompareBound(Latin1("abb"), abc) < 0);
        Assert.Equal(0, order.CompareBound(Latin1("zzz"), []));
        Assert.Empty(KeyOrders.Ordinal.BoundOf(Latin1("abc"), default));
        Assert.Throws<ArgumentException>(() => order.BoundOf(Latin1("abcdefg"), default));
    }

    // ── The model: GR17 as written, against the engine ────────────────────────────────────────────────────────────

    /// <summary>The pieces a locale's keys are built of: base letters, their accent and case variants, the characters
    /// that move the marks (controls, a soft hyphen, expansions, the locale's contractions) and the ones the trimming
    /// and the bound's space cut answer (spaces, tabs).</summary>
    private static string[] PiecesOf(string locale) => locale switch
    {
        "cs" => ["c", "h", "ch", "ch", "C", "H", "Ch", "a", "á", "e", "é", "s", "t", " ", "\t", "\u0001", "č", "r"],
        "da" => ["a", "aa", "aa", "å", "æ", "ø", "A", "Aa", "b", "e", "é", " ", "\u0001", "\t"],
        _ => ["a", "á", "à", "A", "b", "B", "c", "ç", "e", "é", "s", "ß", "æ", " ", "\t", "\u0001", "\u00AD", "\u0085"],
    };

    [Theory]
    [InlineData("root", 3361)]
    [InlineData("cs", 3362)]
    [InlineData("da", 3363)]
    public void The_bounded_walk_agrees_with_the_sequential_search_of_GR17_for_every_relation_and_length(string locale, int seed)
    {
        const int keyLength = 8;
        var random = new Random(seed);
        var pieces = PiecesOf(locale);
        var order = new WeightedKeyOrder(null, new LocaleCollation(locale), keyLength);
        string Build(int length)
        {
            var sb = new StringBuilder();
            while (sb.Length < length) sb.Append(pieces[random.Next(pieces.Length)]);
            return sb.ToString(0, length);
        }
        // Keys: shared stems with variants after them, a few padded with trailing spaces, a few with a variant early.
        var stems = Enumerable.Range(0, 12).Select(_ => Build(1 + random.Next(4))).ToArray();
        var sorted = new SortedSet<byte[]>(Comparer<byte[]>.Create((x, y) => order.Compare(x, y)));
        while (sorted.Count < 480)
        {
            string key = stems[random.Next(stems.Length)] + Build(keyLength);
            key = key[..(1 + random.Next(keyLength))];
            if (random.Next(5) == 0) key = key.PadRight(keyLength);
            sorted.Add(Latin1(key));
        }
        var (_, _, store) = StoreKit.NewStore(coherent: false);
        Assert.Equal(StoreStatus.Ok, store.Enter());
        var tree = new BTree(store, order, store.Meta.PrimeRoot);
        foreach (var key in sorted.OrderBy(_ => random.Next())) Assert.Equal(StoreStatus.Ok, tree.Insert(key, new byte[120], out _));
        Assert.Equal(StoreStatus.Ok, StoreKit.Commit(store, tree));
        var keys = sorted.ToList();
        var read = new BTree(store, order, store.Meta.PrimeRoot);
        Assert.Equal(StoreStatus.Ok, read.ReadMarks(out var marks));
        var expectedMarks = keys.Aggregate(default(TreeMarks), (m, k) => m.Max(order.MarksOf(k)));
        Assert.Equal(expectedMarks, marks);
        Assert.True(marks.Deficit > 0, "the keys move the deficit");
        if (locale != "root") Assert.True(marks.Overhang > 0, "the locale's contractions move the overhang");

        int bounded = 0, seeks = 0;
        for (int k = 1; k <= keyLength; k++)
            for (int p = 0; p < 24; p++)
            {
                string probe = (p % 4) switch
                {
                    0 => Encoding.Latin1.GetString(keys[random.Next(keys.Count)]).PadRight(keyLength)[..k],
                    1 => Mutate(Encoding.Latin1.GetString(keys[random.Next(keys.Count)]).PadRight(keyLength)[..k]),
                    2 => Build(k),
                    _ => (stems[random.Next(stems.Length)] + Build(k))[..k],
                };
                var bytes = Latin1(probe);
                if (order.BoundOf(bytes, marks).Length > 0) bounded++;
                foreach (var relation in Enum.GetValues<SeekRelation>())
                {
                    seeks++;
                    AssertSeek(read, order, keys, bytes, relation, $"{locale} k={k} probe \"{Show(probe)}\" {relation}");
                }
            }
        Assert.True(bounded > 0 && bounded < seeks / 5, $"{bounded} of {seeks / 5} probes had a bound: both arms run");

        string Mutate(string s)
        {
            int at = random.Next(s.Length);
            return (s[..at] + pieces[random.Next(pieces.Length)] + s[(at + 1)..])[..s.Length].PadRight(s.Length);
        }
    }

    /// <summary>⛔ THE OVERHANG IS WHAT A CONTRACTION OF SEVERAL PRIMARIES NEEDS. Hungarian's doubled digraphs ("ggy",
    /// "ccs", "ssz", "nny": three characters, two primaries) raise the deficit by one only, so a truncation in the middle
    /// of one ("gg") changes the primary just before the cut too: the bound must stop the overhang short of it. The key
    /// set holds one doubled digraph per key and nothing else that moves the marks, so g = 1 and every probe cut inside
    /// a digraph meets the bound's edge.</summary>
    [Fact]
    public void The_bound_stops_short_of_a_contraction_of_several_primaries_cut_by_the_truncation()
    {
        const int keyLength = 8;
        var random = new Random(3370);
        var order = new WeightedKeyOrder(null, new LocaleCollation("hu"), keyLength);
        string[] doubled = ["ggy", "ccs", "ssz", "nny", "gy", "cs"], letters = ["a", "e", "o", "b", "d", "g", "c", "s", "n", "y", "z"];
        var sorted = new SortedSet<byte[]>(Comparer<byte[]>.Create((x, y) => order.Compare(x, y)));
        while (sorted.Count < 400)
        {
            var sb = new StringBuilder();
            for (int i = random.Next(4); i > 0; i--) sb.Append(letters[random.Next(letters.Length)]);
            sb.Append(doubled[random.Next(4)]);
            while (sb.Length < keyLength) sb.Append(letters[random.Next(letters.Length)]);
            var key = Latin1(sb.ToString(0, keyLength));
            if (order.MarksOf(key) == new TreeMarks(1, 2)) sorted.Add(key);   // one doubled digraph, no other contraction
        }
        var (tree, keys, marks) = Load(order, sorted, random);
        Assert.Equal(new TreeMarks(1, 2), marks);
        int edges = 0;
        foreach (var key in keys)
        {
            var text = Encoding.Latin1.GetString(key);
            foreach (var digraph in doubled[..4])
            {
                int at = text.IndexOf(digraph, StringComparison.Ordinal);
                if (at < 0) continue;
                for (int k = at + 1; k < at + 3; k++)
                {
                    edges++;
                    foreach (var relation in Enum.GetValues<SeekRelation>())
                        AssertSeek(tree, order, keys, key[..k], relation, $"hu \"{text[..k]}\" {relation}");
                }
            }
            if (edges > 300) break;
        }
        Assert.True(edges > 100, $"{edges} probes cut a doubled digraph");
    }

    /// <summary>⛔ THE BOUND STOPS BEFORE A WEIGHT BELOW A SPACE'S (§8.8.4.2.11's trimming). A stored key whose truncation
    /// ends in spaces of its interior loses them to the trimming, so its truncation is a proper prefix of a probe that has
    /// a TAB (a weight below a space's) at that place: below the probe, while the stored key's own space weights place it
    /// above a bound that kept the TAB. The keys have no ignorable and no contraction (g = 0), so only the cut keeps the
    /// LESS relations from skipping them.</summary>
    [Fact]
    public void The_bound_stops_before_a_weight_below_a_space_so_an_interior_space_cut_off_by_the_trimming_is_not_skipped()
    {
        const int keyLength = 7;
        var random = new Random(3371);
        var order = new WeightedKeyOrder(null, new LocaleCollation("root"), keyLength);
        string[] pieces = ["a", "b", " ", " ", "\t", "c"];
        var sorted = new SortedSet<byte[]>(Comparer<byte[]>.Create((x, y) => order.Compare(x, y)));
        while (sorted.Count < 300)
            sorted.Add(Latin1(string.Concat(Enumerable.Range(0, keyLength).Select(_ => pieces[random.Next(pieces.Length)]))));
        var (tree, keys, marks) = Load(order, sorted, random);
        Assert.Equal(default, marks);
        int cut = 0;
        for (int p = 0; p < 400; p++)
        {
            int k = 2 + random.Next(keyLength - 2);
            var probe = string.Concat(Enumerable.Range(0, k - 1).Select(_ => pieces[random.Next(pieces.Length)])) + "\t";
            if (order.BoundOf(Latin1(probe), marks).Length < k) cut++;
            foreach (var relation in Enum.GetValues<SeekRelation>())
                AssertSeek(tree, order, keys, Latin1(probe), relation, $"\"{Show(probe)}\" {relation}");
        }
        Assert.True(cut > 300, $"{cut} probes were cut at a TAB");
    }

    /// <summary>A tree of <paramref name="keys"/> inserted in a random order, committed, and read back with its marks.</summary>
    private static (BTree Tree, List<byte[]> Keys, TreeMarks Marks) Load(IKeyOrder order, SortedSet<byte[]> keys, Random random)
    {
        var (_, _, store) = StoreKit.NewStore(coherent: false);
        Assert.Equal(StoreStatus.Ok, store.Enter());
        var tree = new BTree(store, order, store.Meta.PrimeRoot);
        foreach (var key in keys.OrderBy(_ => random.Next())) Assert.Equal(StoreStatus.Ok, tree.Insert(key, new byte[120], out _));
        Assert.Equal(StoreStatus.Ok, StoreKit.Commit(store, tree));
        var read = new BTree(store, order, store.Meta.PrimeRoot);
        Assert.Equal(StoreStatus.Ok, read.ReadMarks(out var marks));
        return (read, keys.ToList(), marks);
    }

    /// <summary>The same agreement through the composite order of an ALTERNATE tree (the key part, then the ordinal):
    /// duplicates of one key value are one run of the key part, and the bound compares the key part only.</summary>
    [Fact]
    public void The_bounded_walk_agrees_with_GR17_under_the_composite_order_of_an_alternate_tree()
    {
        const int keyLength = 6;
        var random = new Random(3364);
        var pieces = PiecesOf("cs");
        var order = new CompositeKeyOrder(new WeightedKeyOrder(null, new LocaleCollation("cs"), keyLength));
        var sorted = new SortedSet<byte[]>(Comparer<byte[]>.Create((x, y) => order.Compare(x, y)));
        ulong ordinal = 0;
        while (sorted.Count < 400)
        {
            var sb = new StringBuilder();
            while (sb.Length < keyLength) sb.Append(pieces[random.Next(pieces.Length)]);
            var part = Latin1(sb.ToString(0, 1 + random.Next(keyLength)));
            for (int d = random.Next(3); d >= 0; d--) sorted.Add(CompositeKeyOrder.Compose(part, ++ordinal));
        }
        var (_, _, store) = StoreKit.NewStore(coherent: false);
        Assert.Equal(StoreStatus.Ok, store.Enter());
        var tree = new BTree(store, order, store.Meta.PrimeRoot);
        foreach (var key in sorted.OrderBy(_ => random.Next())) Assert.Equal(StoreStatus.Ok, tree.Insert(key, new byte[100], out _));
        Assert.Equal(StoreStatus.Ok, StoreKit.Commit(store, tree));
        var keys = sorted.ToList();
        var read = new BTree(store, order, store.Meta.PrimeRoot);
        for (int k = 1; k < keyLength; k++)
            for (int p = 0; p < 20; p++)
            {
                var probe = keys[random.Next(keys.Count)][..^CompositeKeyOrder.OrdinalBytes];
                var bytes = probe.Length >= k ? probe[..k] : [.. probe, .. Latin1(new string(' ', k - probe.Length))];
                foreach (var relation in Enum.GetValues<SeekRelation>())
                    AssertSeek(read, order, keys, bytes, relation, $"composite k={k} {relation}");
            }
    }

    /// <summary>One START against GR17 as written over <paramref name="keys"/> (sorted by the tree's order): the entry
    /// the sequential search lands on, or none; and READ NEXT / PREVIOUS from it continues in key order.</summary>
    private static void AssertSeek(BTree tree, IKeyOrder order, List<byte[]> keys, byte[] probe, SeekRelation relation, string at)
    {
        int expected = Gr17(order, keys, probe, relation);
        Assert.Equal(StoreStatus.Ok, tree.Seek(probe, relation, out var cursor, prefix: true));
        Assert.True(expected >= 0 == cursor.HasCurrent, $"{at}: expected {(expected >= 0 ? Show(keys[expected]) : "none")}, got {(cursor.HasCurrent ? Show(cursor.Key) : "none")}");
        if (expected < 0) return;
        Assert.True(keys[expected].AsSpan().SequenceEqual(cursor.Key), $"{at}: expected {Show(keys[expected])}, got {Show(cursor.Key)}");
        bool forward = relation is SeekRelation.Equal or SeekRelation.GreaterOrEqual or SeekRelation.GreaterThan;
        Assert.Equal(StoreStatus.Ok, forward ? cursor.Next() : cursor.Previous());
        int next = forward ? expected + 1 : expected - 1;
        Assert.Equal(next >= 0 && next < keys.Count, cursor.HasCurrent);
        if (cursor.HasCurrent) Assert.True(keys[next].AsSpan().SequenceEqual(cursor.Key), $"{at}: the step after the START");
    }

    /// <summary>§14.9.41.4 GR17 c) to e) as written: the index of the first sorted key (the last, for the LESS relations)
    /// whose truncation to the probe's length satisfies the relation; -1 when none does (e) 2., the invalid key).</summary>
    private static int Gr17(IKeyOrder order, List<byte[]> keys, byte[] probe, SeekRelation relation) => relation switch
    {
        SeekRelation.Equal => keys.FindIndex(k => order.ComparePrefix(k, probe) == 0),
        SeekRelation.GreaterOrEqual => keys.FindIndex(k => order.ComparePrefix(k, probe) >= 0),
        SeekRelation.GreaterThan => keys.FindIndex(k => order.ComparePrefix(k, probe) > 0),
        SeekRelation.LessOrEqual => keys.FindLastIndex(k => order.ComparePrefix(k, probe) <= 0),
        _ => keys.FindLastIndex(k => order.ComparePrefix(k, probe) < 0),
    };

    // ── The pages: the depth and the run, never the file ──────────────────────────────────────────────────────────

    /// <summary>⛔ ORDINARY TEXT NEVER DEGRADES (kb/Work PB3330's bar: no statement reads the whole file). Keys of plain
    /// letters have no deficit and no overhang, so a partial key of k positions is bounded by all its k primaries: the
    /// search reads the descent to the bound (the depth; a LESS relation's step back and, past the last key, its descent
    /// to the last entry) and one page per leaf the run crosses before its answer. A probe above every key answers the
    /// invalid key condition in the depth exactly (the reviewer's scenario).</summary>
    [Fact]
    public void A_partial_START_over_ordinary_text_reads_the_depth_and_the_leaves_of_its_run()
    {
        var (medium, _, store, order, keys) = LoadTree(6, 3000, 3365, letters: "abcdefgh", variants: "");
        Assert.Equal(StoreStatus.Ok, new BTree(store, order, store.Meta.PrimeRoot).ReadMarks(out var marks));
        Assert.Equal(default, marks);
        var leaves = LeafOf(medium, store, keys.Count);
        int depth = Depth(medium, store, order);
        Assert.Equal(2, depth);
        Assert.True(leaves.Max() > 40, $"{leaves.Max() + 1} leaves");
        var random = new Random(3366);
        int widest = 0;
        for (int i = 0; i < 300; i++)
        {
            int k = 1 + random.Next(5);
            var probe = Latin1(string.Concat(Enumerable.Range(0, k).Select(_ => "abcdefgh"[random.Next(8)])));
            foreach (var relation in Enum.GetValues<SeekRelation>())
                widest = Math.Max(widest, AssertPages(store, order, keys, leaves, depth, marks, probe, relation));
        }
        Assert.True(widest < leaves.Max() / 4, $"a partial START read {widest} pages of a tree of {leaves.Max() + 1} leaves");
        long before = store.Counters.PagesRead;
        Assert.Equal(StoreStatus.Ok, new BTree(store, order, store.Meta.PrimeRoot).Seek(Latin1("zzz"), SeekRelation.GreaterThan, out var none, prefix: true));
        Assert.False(none.HasCurrent);
        Assert.Equal(depth, store.Counters.PagesRead - before);
    }

    /// <summary>⛔ A RUN OF VARIANTS IS READ UP TO ITS ANSWER. Every base word carries its accent and case variants, which
    /// tie a probe of its first letters at the primary level and are ordered by their continuation before their accents,
    /// so GR17 c) has to examine them: the search reads the depth and the leaves of the variants ahead of its answer.</summary>
    [Fact]
    public void A_partial_START_over_accent_and_case_variants_reads_the_variants_ahead_of_its_answer()
    {
        var (medium, _, store, order, keys) = LoadTree(6, 1600, 3367, letters: "abcdefgh", variants: "áàâÁA");
        Assert.Equal(StoreStatus.Ok, new BTree(store, order, store.Meta.PrimeRoot).ReadMarks(out var marks));
        Assert.Equal(default, marks);
        var leaves = LeafOf(medium, store, keys.Count);
        int depth = Depth(medium, store, order);
        var random = new Random(3368);
        for (int i = 0; i < 200; i++)
        {
            var source = Encoding.Latin1.GetString(keys[random.Next(keys.Count)]);
            var probe = Latin1(source[..(1 + random.Next(4))]);
            foreach (var relation in Enum.GetValues<SeekRelation>())
                AssertPages(store, order, keys, leaves, depth, marks, probe, relation);
        }
    }

    /// <summary>One START's answer against GR17 and its pages against the bound: the descent (twice for a LESS relation
    /// that falls past the last key), a step back, and one page per leaf boundary its walk crosses (a depth-2 tree's
    /// path holds the root) from the bound's seat to the answer, or to the first entry past the run.</summary>
    private static int AssertPages(PagedFile store, IKeyOrder order, List<byte[]> keys, int[] leafOf, int depth, TreeMarks marks,
        byte[] probe, SeekRelation relation)
    {
        int expected = Gr17(order, keys, probe, relation);
        var bound = order.BoundOf(probe, marks);
        Assert.True(bound.Length == probe.Length, "ordinary text bounds a partial key by every one of its primaries");
        bool forward = relation is SeekRelation.Equal or SeekRelation.GreaterOrEqual or SeekRelation.GreaterThan;
        int seat = forward ? keys.FindIndex(k => order.CompareBound(k, bound) >= 0) : keys.FindLastIndex(k => order.CompareBound(k, bound) <= 0);
        int stop = -1;
        if (seat >= 0 && expected >= 0) stop = expected;
        else if (seat >= 0)
        {
            // No answer: the walk left the run at its first entry past it, or read the run to the end of the tree.
            int exit = forward ? keys.FindIndex(seat, k => order.CompareBound(k, bound) != 0) : keys.FindLastIndex(seat, k => order.CompareBound(k, bound) != 0);
            stop = exit >= 0 ? exit : forward ? keys.Count - 1 : 0;
        }
        int crossings = seat < 0 || stop < 0 ? 0 : Math.Abs(leafOf[stop] - leafOf[seat]);
        long before = store.Counters.PagesRead;
        Assert.Equal(StoreStatus.Ok, new BTree(store, order, store.Meta.PrimeRoot).Seek(probe, relation, out var cursor, prefix: true));
        int read = (int)(store.Counters.PagesRead - before);
        Assert.Equal(expected >= 0, cursor.HasCurrent);
        if (expected >= 0) Assert.True(keys[expected].AsSpan().SequenceEqual(cursor.Key));
        Assert.True(read <= 2 * depth + 2 + crossings, $"\"{Show(probe)}\" {relation}: {read} pages, {crossings} leaf crossings");
        return read;
    }

    /// <summary>A store of <paramref name="count"/> distinct keys of <paramref name="length"/> positions over
    /// <paramref name="letters"/>, one in five of their letters replaced by one of <paramref name="variants"/>.</summary>
    private static (MemoryPageMedium Medium, MemoryPageMedium.Handle Handle, PagedFile Store, IKeyOrder Order, List<byte[]> Keys) LoadTree(int length, int count, int seed,
        string letters, string variants)
    {
        var random = new Random(seed);
        var order = new WeightedKeyOrder(null, new LocaleCollation("root"), length);
        var sorted = new SortedSet<byte[]>(Comparer<byte[]>.Create((x, y) => order.Compare(x, y)));
        while (sorted.Count < count)
            sorted.Add(Latin1(string.Create(length, random, (span, r) =>
            {
                for (int i = 0; i < span.Length; i++)
                    span[i] = variants.Length > 0 && r.Next(5) == 0 ? variants[r.Next(variants.Length)] : letters[r.Next(letters.Length)];
            })));
        var (medium, handle, store) = StoreKit.NewStore(coherent: false);
        Assert.Equal(StoreStatus.Ok, store.Enter());
        var tree = new BTree(store, order, store.Meta.PrimeRoot);
        foreach (var key in sorted.OrderBy(_ => random.Next())) Assert.Equal(StoreStatus.Ok, tree.Insert(key, new byte[60], out _));
        Assert.Equal(StoreStatus.Ok, StoreKit.Commit(store, tree));
        return (medium, handle, store, order, sorted.ToList());
    }

    /// <summary>The leaf (counted left to right) that holds each of the tree's entries in key order.</summary>
    private static int[] LeafOf(MemoryPageMedium medium, PagedFile store, int count)
    {
        var leafOf = new List<int>(count);
        int leaf = 0;
        void Walk(PagePointer at)
        {
            var node = StoreKit.ReadNode(medium, at)!;
            if (node.Kind == PageKind.Internal)
            {
                foreach (var child in node.Children) Walk(child);
                return;
            }
            leafOf.AddRange(Enumerable.Repeat(leaf++, node.Keys.Count));
        }
        Walk(store.Meta.PrimeRoot);
        Assert.Equal(count, leafOf.Count);
        return [.. leafOf];
    }

    private static int Depth(MemoryPageMedium medium, PagedFile store, IKeyOrder order) =>
        BTreeModelTests.AuditShape(medium, store.Meta.PrimeRoot, order, out _, out _);

    // ── The marks: on the root, through CLOSE and OPEN, deaths, refusals and another run unit ─────────────────────

    /// <summary>⛔ THE MARKS ARE THE ROOT PAGE'S (2.2.1.4: header bytes 24..27): raised by an INSERT, carried to a new root
    /// when the root splits, zeros on every other page, kept by a DELETE, gone with the last key, and read back by a tree
    /// opened on the root after CLOSE and OPEN.</summary>
    [Fact]
    public void The_marks_are_on_the_root_page_and_survive_close_and_open()
    {
        var order = new WeightedKeyOrder(null, new LocaleCollation("cs"), 8);
        var (medium, _, store) = StoreKit.NewStore();
        Assert.Equal(StoreStatus.Ok, store.Enter());
        var tree = new BTree(store, order, store.Meta.PrimeRoot);
        Assert.Equal(StoreStatus.Ok, tree.ReadMarks(out var empty));
        Assert.Equal(default, empty);
        Assert.Equal(StoreStatus.Ok, tree.Insert(Latin1("abc"), [1], out _));
        Assert.Equal(StoreStatus.Ok, tree.ReadMarks(out var plain));
        Assert.Equal(default, plain);
        Assert.Equal(StoreStatus.Ok, tree.Insert(Latin1("a\u0001b"), [2], out _));
        Assert.Equal(StoreStatus.Ok, tree.Insert(Latin1("chata"), [3], out _));
        Assert.Equal(StoreStatus.Ok, tree.ReadMarks(out var pending));
        Assert.Equal(new TreeMarks(1, 1), pending);                                  // the pending edit's view
        Assert.Equal(StoreStatus.Ok, StoreKit.Commit(store, tree));
        Assert.Equal(new TreeMarks(1, 1), PageHeader.Decode(medium.Snapshot(store.Meta.PrimeRoot.Page)).Marks);
        // Grow past one leaf: the new root carries the marks, every other page zeros.
        for (int i = 0; i < 400; i++)
            Assert.Equal(StoreStatus.Ok, StoreKit.CommitOne(store, order, Latin1($"k{i:D4}"), new byte[40]));
        Assert.Equal(StoreStatus.Ok, StoreKit.CommitOne(store, order, Latin1("x\u0001\u0001y"), new byte[40]));
        AssertMarksOnTheRootOnly(medium, store, new TreeMarks(2, 1));
        Assert.Equal(StoreStatus.Ok, store.Close());
        var (_, opened, reopened) = StoreKit.OpenAs(medium, "B");
        Assert.Equal(StoreStatus.Ok, opened);
        Assert.Equal(StoreStatus.Ok, new BTree(reopened!, order, reopened!.Meta.PrimeRoot).ReadMarks(out var after));
        Assert.Equal(new TreeMarks(2, 1), after);
        // A DELETE never lowers them (they bound every key the tree was given); the last key's DELETE leaves no tree.
        Assert.Equal(StoreStatus.Ok, reopened.Enter());
        var edit = new BTree(reopened, order, reopened.Meta.PrimeRoot);
        Assert.Equal(StoreStatus.Ok, edit.Delete(Latin1("x\u0001\u0001y"), out bool deleted));
        Assert.True(deleted);
        Assert.Equal(StoreStatus.Ok, StoreKit.Commit(reopened, edit));
        AssertMarksOnTheRootOnly(medium, reopened, new TreeMarks(2, 1));
        Assert.Equal(StoreStatus.Ok, reopened.Enter());
        var drain = new BTree(reopened, order, reopened.Meta.PrimeRoot);
        Assert.Equal(StoreStatus.Ok, drain.First(out var walk));
        var all = new List<byte[]>();
        for (; walk.HasCurrent; Assert.Equal(StoreStatus.Ok, walk.Next())) all.Add(walk.Key);
        foreach (var key in all) Assert.Equal(StoreStatus.Ok, drain.Delete(key, out _));
        Assert.Equal(StoreStatus.Ok, drain.ReadMarks(out var gone));
        Assert.Equal(default, gone);
        Assert.Equal(StoreStatus.Ok, StoreKit.Commit(reopened, drain));
        Assert.True(reopened.Meta.PrimeRoot.IsNone);
    }

    private static void AssertMarksOnTheRootOnly(MemoryPageMedium medium, PagedFile store, TreeMarks marks)
    {
        Assert.Equal(marks, PageHeader.Decode(medium.Snapshot(store.Meta.PrimeRoot.Page)).Marks);
        var root = StoreKit.ReadNode(medium, store.Meta.PrimeRoot)!;
        Assert.Equal(PageKind.Internal, root.Kind);
        foreach (var child in root.Children) Assert.Equal(default, PageHeader.Decode(medium.Snapshot(child.Page)).Marks);
    }

    /// <summary>⛔ A STATEMENT THAT RAISES THE MARKS, KILLED OR REFUSED AT EVERY ONE OF ITS WRITES (2.2.1.16, "A changing
    /// statement"), leaves the marks of the records it left: the next OPEN finds either the previous tree with its marks
    /// or the statement's tree with the raised ones — never the statement's keys under the old marks — and every partial
    /// START over what it finds agrees with GR17.</summary>
    [Theory]
    [InlineData(nameof(Fault.Death))]
    [InlineData(nameof(Fault.DeathAfter))]
    [InlineData(nameof(Fault.Refuse))]
    public void A_statement_raising_the_marks_faulted_at_every_write_leaves_the_marks_of_the_tree_it_left(string faultName)
    {
        var fault = Enum.Parse<Fault>(faultName);
        var order = new WeightedKeyOrder(null, new LocaleCollation("root"), 6);
        var baseKeys = Enumerable.Range(0, 300).Select(i => Latin1($"b{i:D3}")).ToList();
        var added = Enumerable.Range(0, 40).Select(i => Latin1($"n{i:D3}")).Append(Latin1("a\u0001\u0001bc")).ToList();
        (MemoryPageMedium Medium, MemoryPageMedium.Handle Handle, PagedFile Store) Scene()
        {
            var (medium, _, store) = StoreKit.NewStore();
            Assert.Equal(StoreStatus.Ok, store.Enter());
            var tree = new BTree(store, order, store.Meta.PrimeRoot);
            foreach (var key in baseKeys) Assert.Equal(StoreStatus.Ok, tree.Insert(key, new byte[50], out _));
            Assert.Equal(StoreStatus.Ok, StoreKit.Commit(store, tree));
            Assert.Equal(StoreStatus.Ok, store.Close());
            var (handle, status, opened) = StoreKit.OpenAs(medium, "A");
            Assert.Equal(StoreStatus.Ok, status);
            return (medium, handle, opened!);
        }
        StoreStatus Statement(PagedFile store)
        {
            if (store.Enter() != StoreStatus.Ok) return StoreStatus.PermanentError;
            var tree = new BTree(store, order, store.Meta.PrimeRoot);
            foreach (var key in added)
                if (tree.Insert(key, new byte[50], out _) != StoreStatus.Ok) return StoreStatus.PermanentError;
            return StoreKit.Commit(store, tree);
        }
        var counting = Scene();
        int start = counting.Handle.WritesPerformed;
        Assert.Equal(StoreStatus.Ok, Statement(counting.Store));
        int writes = counting.Handle.WritesPerformed - start;
        Assert.True(writes >= 5, $"the statement writes {writes} pages");
        var afterKeys = baseKeys.Concat(added).OrderBy(k => k, Comparer<byte[]>.Create((x, y) => order.Compare(x, y))).ToList();
        var beforeKeys = baseKeys.OrderBy(k => k, Comparer<byte[]>.Create((x, y) => order.Compare(x, y))).ToList();
        for (int n = 1; n <= writes; n++)
        {
            var (medium, handle, store) = Scene();
            handle.FaultAtWrite(n, fault);
            if (fault == Fault.Refuse) Assert.Equal(StoreStatus.PermanentError, Statement(store));
            else Assert.Throws<SimulatedDeath>(() => Statement(store));
            var (_, status, next) = StoreKit.OpenAs(medium, "C");
            Assert.Equal(StoreStatus.Ok, status);
            var tree = new BTree(next!, order, next!.Meta.PrimeRoot);
            Assert.Equal(StoreStatus.Ok, tree.First(out var walk));
            var present = new List<byte[]>();
            for (; walk.HasCurrent; Assert.Equal(StoreStatus.Ok, walk.Next())) present.Add(walk.Key);
            bool completed = fault == Fault.DeathAfter && n == writes;
            var expected = completed ? afterKeys : beforeKeys;
            Assert.True(expected.Count == present.Count && expected.Zip(present).All(p => p.First.AsSpan().SequenceEqual(p.Second)),
                $"{fault} at write {n} of {writes}: {present.Count} keys");
            Assert.Equal(StoreStatus.Ok, tree.ReadMarks(out var marks));
            Assert.Equal(completed ? new TreeMarks(2, 0) : default, marks);
            foreach (var probe in new[] { "a", "ab", "b1", "n0", "n03", "zz" })
                foreach (var relation in Enum.GetValues<SeekRelation>())
                    AssertSeek(tree, order, present, Latin1(probe), relation, $"{fault} at write {n}: \"{probe}\" {relation}");
        }
    }

    /// <summary>⛔ A REFUSED READ ANYWHERE IN THE SEARCH ANSWERS '30' (§9.1.13.6 item 1; 2.2.1.4, a refused read is no
    /// verdict on the page): the root the marks are read from, the descent to the bound, each leaf the run crosses. The
    /// cursor is left with no current entry, and the same START once the medium heals answers GR17's entry.</summary>
    [Fact]
    public void A_refused_read_at_every_read_of_a_bounded_search_answers_30_and_the_retry_answers_GR17()
    {
        var (_, medium, store, order, keys) = LoadTree(6, 1600, 3369, letters: "abcd", variants: "áÁ");
        var probe = Latin1("ab");
        foreach (var relation in Enum.GetValues<SeekRelation>())
        {
            int start = medium.ReadsPerformed;
            Assert.Equal(StoreStatus.Ok, new BTree(store, order, store.Meta.PrimeRoot).Seek(probe, relation, out _, prefix: true));
            int reads = medium.ReadsPerformed - start;
            Assert.True(reads >= 2, $"{relation}: {reads} reads");
            for (int n = 1; n <= reads; n++)
            {
                medium.FaultAtRead(n, Fault.Refuse);
                Assert.Equal(StoreStatus.PermanentError, new BTree(store, order, store.Meta.PrimeRoot).Seek(probe, relation, out var refused, prefix: true));
                Assert.False(refused.HasCurrent);
                medium.Heal();
            }
            AssertSeek(new BTree(store, order, store.Meta.PrimeRoot), order, keys, probe, relation, $"{relation} after the refusals");
        }
    }

    /// <summary>⛔ ANOTHER RUN UNIT'S STATEMENT BETWEEN TWO OF THIS ONE'S (2.2.1.17): the marks travel with the root, so a
    /// tree that adopts the root another run unit's statement left (its coherent entry, 2.2.1.7) bounds its next START by
    /// the marks that statement raised, finds the key it added, and its own next INSERT raises them from there.</summary>
    [Fact]
    public void A_tree_that_adopts_another_run_units_root_takes_the_marks_that_statement_raised()
    {
        var order = new WeightedKeyOrder(null, new LocaleCollation("root"), 6);
        var (medium, _, store) = StoreKit.NewStore();
        Assert.Equal(StoreStatus.Ok, store.Enter());
        var tree = new BTree(store, order, store.Meta.PrimeRoot);
        foreach (var i in Enumerable.Range(0, 300)) Assert.Equal(StoreStatus.Ok, tree.Insert(Latin1($"b{i:D3}"), new byte[50], out _));
        Assert.Equal(StoreStatus.Ok, StoreKit.Commit(store, tree));
        Assert.Equal(StoreStatus.Ok, store.Close());
        var (_, sa, a) = StoreKit.OpenAs(medium, "A");
        var (_, sb, b) = StoreKit.OpenAs(medium, "B");
        Assert.Equal(StoreStatus.Ok, sa);
        Assert.Equal(StoreStatus.Ok, sb);
        Assert.Equal(StoreStatus.Ok, a!.Enter());
        var mine = new BTree(a, order, a.Meta.PrimeRoot);
        Assert.Equal(StoreStatus.Ok, mine.Seek(Latin1("b0"), SeekRelation.GreaterOrEqual, out var cursor, prefix: true));
        Assert.Equal(Latin1("b000"), cursor.Key);
        // B's statement adds a key whose two leading controls raise the deficit to 2.
        Assert.Equal(StoreStatus.Ok, StoreKit.CommitOne(b!, order, Latin1("\u0001\u0001b0"), new byte[50]));
        Assert.Equal(StoreStatus.Ok, a.Enter());
        mine.Adopt(a.Meta.PrimeRoot);
        Assert.Equal(StoreStatus.Ok, mine.ReadMarks(out var adopted));
        Assert.Equal(new TreeMarks(2, 0), adopted);
        var keys = Enumerable.Range(0, 300).Select(i => Latin1($"b{i:D3}")).Append(Latin1("\u0001\u0001b0"))
            .OrderBy(k => k, Comparer<byte[]>.Create((x, y) => order.Compare(x, y))).ToList();
        foreach (var probe in new[] { "b0", "b00", "b", "\u0001b", "c" })
            foreach (var relation in Enum.GetValues<SeekRelation>())
                AssertSeek(mine, order, keys, Latin1(probe), relation, $"adopted: \"{Show(probe)}\" {relation}");
        // A's own statement raises them from B's.
        Assert.Equal(StoreStatus.Ok, mine.Insert(Latin1("c\u0001\u0001\u0001d"), new byte[50], out _));
        Assert.Equal(StoreStatus.Ok, mine.ReadMarks(out var raised));
        Assert.Equal(new TreeMarks(3, 0), raised);
        Assert.Equal(StoreStatus.Ok, StoreKit.Commit(a, mine));
        Assert.Equal(StoreStatus.Ok, b!.Enter());
        Assert.Equal(StoreStatus.Ok, new BTree(b, order, b.Meta.PrimeRoot).ReadMarks(out var seen));
        Assert.Equal(new TreeMarks(3, 0), seen);
        StoreKit.AssertR1(medium);
        StoreKit.AssertNoStampReused(medium);
    }

    private static byte[] Latin1(string text) => Encoding.Latin1.GetBytes(text);

    private static string Show(byte[] key) => Show(Encoding.Latin1.GetString(key));

    private static string Show(string text) => string.Concat(text.Select(c => c < ' ' || c is >= '\u007F' and < '\u00A1' || c == '\u00AD' ? $"\\u{(int)c:X4}" : c.ToString()));
}
