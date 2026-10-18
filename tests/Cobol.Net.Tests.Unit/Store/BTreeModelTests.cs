// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolNet.Runtime;
using CobolNet.Runtime.IO.Store;
using Xunit;

namespace CobolNet.Tests.Unit.Store;

/// <summary>
/// ⛔ THE ENGINE'S TREE AGREES WITH A SORTED MODEL UNDER EVERY KEY ORDER, THROUGH SPLITS AND MERGES AT EVERY LEVEL
/// (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.12, "the engine's tree"; kb/Work PB3332).
/// <para>Each run drives a <see cref="BTree"/> over a <see cref="PagedFile"/> and a <see cref="SortedDictionary{TKey,
/// TValue}"/> ordered by the same <see cref="IKeyOrder"/> with the same seeded operations — insert, replace, upsert,
/// delete, many to a statement and one to a statement — growing the tree to three levels and shrinking it back to one
/// and then to nothing, with values inline, at the overflow threshold (1 016 and 1 017 bytes), past one overflow page
/// (4 065 bytes) and of 1 MiB. After every statement the tree is read back from the MEDIUM through a fresh handle:
/// every entry forward and backward, every seek relation and a START prefix against the model's answer, and its shape
/// by the auditor's own decoder (key order within each node, every key inside its separators, every leaf at one depth,
/// no page over its body); the page layer's R2 and R3 hold throughout. The orders: ORDINAL bytes, the NUMERIC order of
/// relative record numbers (8 bytes big-endian under the ordinal order), WEIGHTED (a declared alphabet "ZYX…A", whose
/// order is not the bytes'), LOCALE (the root locale's CLDR/UCA order, §8.8.4.2.11, which is not prefix-consistent, so a
/// START prefix is the sequential search of §14.9.41.4 GR17 c) and d) — the model's own <c>FindIndex</c> and
/// <c>FindLastIndex</c>) and COMPOSITE (an alternate key with duplicates, then its release ordinal).</para>
/// </summary>
public sealed class BTreeModelTests
{
    /// <summary>The key orders the model runs under.</summary>
    public enum Order
    {
        Ordinal,
        Numeric,
        Weighted,
        Locale,
        Composite,
    }

    [Theory]
    [InlineData(Order.Ordinal, 3301)]
    [InlineData(Order.Numeric, 3302)]
    [InlineData(Order.Weighted, 3303)]
    [InlineData(Order.Composite, 3304)]
    public void The_tree_agrees_with_a_sorted_model_through_splits_and_merges_at_three_levels(Order order, int seed)
    {
        var run = new ModelRun(order, seed);
        // Grow until the tree is three levels deep.
        while (run.Depth < 3) run.Statement(ops: 60, insertBias: 0.85);
        run.Statement(ops: 1, insertBias: 1);                       // one change to a statement, at depth 3
        Assert.True(run.Model.Count > 1000, $"three levels with {run.Model.Count} entries");
        // Shrink until one leaf is left: merges and borrows at every level, the root collapsing twice (the odd insert
        // may grow it back a level on the way; every change of depth is one level).
        var depths = new List<int> { run.Depth };
        while (run.Depth > 1 || run.Model.Count > 3)
        {
            run.Statement(ops: Math.Clamp(run.Model.Count / 20, 1, 60), insertBias: 0.1);
            if (depths[^1] != run.Depth) depths.Add(run.Depth);
        }
        Assert.Equal(1, depths[^1]);
        Assert.All(depths.Zip(depths.Skip(1)), d => Assert.Equal(1, Math.Abs(d.First - d.Second)));
        // Empty the tree: its root is no page at all.
        while (run.Model.Count > 0) run.Statement(ops: 1, insertBias: 0);
        Assert.True(run.Store.Meta.PrimeRoot.IsNone);
        Assert.True(run.Overflowed >= 3, "the run stored overflow values");
        StoreKit.AssertNoStampReused(run.Medium);
        StoreKit.AssertR2(run.Medium);
    }

    /// <summary>⛔ THE LOCALE ORDER'S START PREFIX IS THE SEQUENTIAL SEARCH (review w1122-a R1-1): the same model run under
    /// the root locale's CLDR/UCA order (§8.8.4.2.11), whose qualifying entries for a partial key are not one run of the key
    /// order, so <see cref="BTree.Seek"/> walks the tree in the search's direction (§14.9.41.4 GR17 c) and d)) and every
    /// relation must still agree with the model's <c>FindIndex</c> and <c>FindLastIndex</c>. Several leaves under one or two
    /// internal levels are enough to make the walk cross leaves; the three-level shape is the theory's, and a locale
    /// comparison costs a decoding and a collation key per key, so this run stays at a few hundred entries.</summary>
    [Fact]
    public void The_tree_agrees_with_a_sorted_model_under_a_locale_order_whose_prefixes_are_not_consistent()
    {
        var run = new ModelRun(Order.Locale, 3305);
        while (run.Model.Count < 500) run.Statement(ops: 40, insertBias: 0.85);
        Assert.True(run.Depth >= 2, $"several leaves: depth {run.Depth}");
        while (run.Model.Count > 0) run.Statement(ops: Math.Clamp(run.Model.Count / 10, 1, 40), insertBias: 0.1);
        Assert.True(run.Store.Meta.PrimeRoot.IsNone);
        StoreKit.AssertNoStampReused(run.Medium);
        StoreKit.AssertR2(run.Medium);
    }

    /// <summary>One model run: the store, the tree's order, the model, the seeded source of operations.</summary>
    private sealed class ModelRun
    {
        private readonly Random _random;
        private readonly Order _kind;
        private ulong _ordinal;
        private int _bigValues;
        private int _statements;

        public ModelRun(Order kind, int seed)
        {
            _kind = kind;
            _random = new Random(seed);
            (Medium, _, Store) = StoreKit.NewStore();
            KeyOrder = kind switch
            {
                Order.Weighted => new WeightedKeyOrder(null, ReversedAlphabet(), WeightedKeyLength),
                Order.Locale => new WeightedKeyOrder(null, new LocaleCollation("root"), LocaleKeyLength),
                Order.Composite => new CompositeKeyOrder(KeyOrders.Ordinal),
                _ => KeyOrders.Ordinal,
            };
            Model = new SortedDictionary<byte[], byte[]>(Comparer<byte[]>.Create((a, b) => KeyOrder.Compare(a, b)));
        }

        public MemoryPageMedium Medium { get; }

        public PagedFile Store { get; }

        public IKeyOrder KeyOrder { get; }

        public SortedDictionary<byte[], byte[]> Model { get; }

        public int Depth { get; private set; }

        public int Overflowed { get; private set; }

        /// <summary>One statement of <paramref name="ops"/> changes, each an insert or upsert with probability
        /// <paramref name="insertBias"/>, else a replace or a delete of a present key; then the whole check.</summary>
        public void Statement(int ops, double insertBias)
        {
            Assert.Equal(StoreStatus.Ok, Store.Enter());
            var tree = new BTree(Store, KeyOrder, Store.Meta.PrimeRoot);
            for (int i = 0; i < ops; i++)
            {
                if (Model.Count > 0 && _random.NextDouble() >= insertBias)
                {
                    var present = Model.Keys.ElementAt(_random.Next(Model.Count));
                    if (_random.Next(3) == 0)
                    {
                        var value = NewValue();
                        Assert.Equal(StoreStatus.Ok, tree.Replace(present, value, out bool replaced));
                        Assert.True(replaced);
                        Model[present] = value;
                    }
                    else
                    {
                        Assert.Equal(StoreStatus.Ok, tree.Delete(present, out bool deleted));
                        Assert.True(deleted);
                        Model.Remove(present);
                    }
                    continue;
                }
                var key = NewKey();
                var fresh = NewValue();
                if (_random.Next(4) == 0)
                {
                    Assert.Equal(StoreStatus.Ok, tree.Put(key, fresh));
                    Model[key] = fresh;
                    continue;
                }
                Assert.Equal(StoreStatus.Ok, tree.Insert(key, fresh, out bool inserted));
                Assert.Equal(!Model.ContainsKey(key), inserted);
                if (inserted) Model[key] = fresh;
            }
            // The pending edit is the statement's view before anything is written.
            if (Model.Count > 0)
            {
                var probe = Model.Keys.ElementAt(_random.Next(Model.Count));
                Assert.Equal(StoreStatus.Ok, tree.TryGet(probe, out var pending));
                Assert.Equal(Model[probe], pending);
            }
            Assert.Equal(StoreStatus.Ok, StoreKit.Commit(Store, tree));
            Check(values: ++_statements % 8 == 0 || Model.Count < 4);
        }

        private void Check(bool values)
        {
            var reread = new BTree(Store, KeyOrder, Store.Meta.PrimeRoot);
            var expected = Model.Keys.ToList();
            // Forward and backward, every key; every value on every eighth statement.
            Assert.Equal(StoreStatus.Ok, reread.First(out var forward));
            foreach (var key in expected)
            {
                Assert.True(forward.HasCurrent);
                Assert.Equal(0, KeyOrder.Compare(key, forward.Key));
                if (values)
                {
                    Assert.Equal(StoreStatus.Ok, forward.ReadValue(out var value));
                    Assert.Equal(Model[key], value);
                }
                Assert.Equal(StoreStatus.Ok, forward.Next());
            }
            Assert.False(forward.HasCurrent);
            Assert.Equal(StoreStatus.Ok, reread.Last(out var backward));
            for (int i = expected.Count - 1; i >= 0; i--)
            {
                Assert.True(backward.HasCurrent);
                Assert.Equal(0, KeyOrder.Compare(expected[i], backward.Key));
                Assert.Equal(StoreStatus.Ok, backward.Previous());
            }
            Assert.False(backward.HasCurrent);
            // Every relation, whole keys and START prefixes, present and absent probes.
            for (int i = 0; i < 6; i++)
            {
                bool prefix = i % 3 == 2;
                var probe = prefix ? NewPrefix() : i % 3 == 0 && expected.Count > 0 ? expected[_random.Next(expected.Count)] : NewKey();
                foreach (var relation in Enum.GetValues<SeekRelation>()) CheckSeek(reread, expected, probe, relation, prefix);
            }
            // A point lookup reads exactly the entry.
            if (expected.Count > 0)
            {
                var key = expected[_random.Next(expected.Count)];
                Assert.Equal(StoreStatus.Ok, reread.TryGet(key, out var got));
                Assert.Equal(Model[key], got);
            }
            Depth = AuditShape(Medium, Store.Meta.PrimeRoot, KeyOrder, out int entries, out int overflow);
            Assert.Equal(Model.Count, entries);
            Overflowed = Math.Max(Overflowed, overflow);
        }

        private void CheckSeek(BTree tree, List<byte[]> keys, byte[] probe, SeekRelation relation, bool prefix)
        {
            int Cmp(byte[] k) => prefix ? KeyOrder.ComparePrefix(k, probe) : KeyOrder.Compare(k, probe);
            int expected = relation switch
            {
                SeekRelation.Equal => keys.FindIndex(k => Cmp(k) == 0),
                SeekRelation.GreaterOrEqual => keys.FindIndex(k => Cmp(k) >= 0),
                SeekRelation.GreaterThan => keys.FindIndex(k => Cmp(k) > 0),
                SeekRelation.LessOrEqual => keys.FindLastIndex(k => Cmp(k) <= 0),
                _ => keys.FindLastIndex(k => Cmp(k) < 0),
            };
            Assert.Equal(StoreStatus.Ok, tree.Seek(probe, relation, out var cursor, prefix));
            Assert.Equal(expected >= 0, cursor.HasCurrent);
            if (expected < 0) return;
            Assert.Equal(0, KeyOrder.Compare(keys[expected], cursor.Key));
            // One step on from the seek's position, in the direction the relation searches.
            bool forward = relation is SeekRelation.Equal or SeekRelation.GreaterOrEqual or SeekRelation.GreaterThan;
            Assert.Equal(StoreStatus.Ok, forward ? cursor.Next() : cursor.Previous());
            int next = forward ? expected + 1 : expected - 1;
            Assert.Equal(next >= 0 && next < keys.Count, cursor.HasCurrent);
            if (cursor.HasCurrent) Assert.Equal(0, KeyOrder.Compare(keys[next], cursor.Key));
        }

        private byte[] NewKey()
        {
            switch (_kind)
            {
                case Order.Numeric:
                    return KeyOrders.RecordNumberKey(_random.Next(4) == 0 ? (ulong)_random.NextInt64(1, 1L << 40) : (ulong)_random.Next(1, 200_000));
                case Order.Weighted:
                    return Encoding.Latin1.GetBytes(Letters(1 + _random.Next(WeightedKeyLength), "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123"));
                case Order.Locale:
                    return Encoding.Latin1.GetBytes(Letters(1 + _random.Next(LocaleKeyLength), LocaleLetters));
                case Order.Composite:
                    return CompositeKeyOrder.Compose(Encoding.Latin1.GetBytes(Letters(1 + _random.Next(5), "ABCD")), ++_ordinal);
                default:
                    // Bytes from a small alphabet (shared prefixes, a key that begins another), now and then a long key.
                    int length = _random.Next(50) == 0 ? _random.Next(500, BTree.MaxKeyBytes + 1) : 1 + _random.Next(48);
                    var key = new byte[length];
                    for (int i = 0; i < length; i++) key[i] = (byte)(_random.Next(4) switch { 0 => 0x00, 1 => 0x41, 2 => 0x42, _ => 0xFF });
                    return key;
            }
        }

        private byte[] NewPrefix() => _kind switch
        {
            Order.Numeric => NewKey()[..(1 + _random.Next(7))],
            Order.Weighted => Encoding.Latin1.GetBytes(Letters(1 + _random.Next(3), "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123")),
            // A partial key mostly, and now and then one as long as the key, which the tree narrows by binary search.
            Order.Locale => Encoding.Latin1.GetBytes(Letters(_random.Next(4) == 0 ? LocaleKeyLength : 1 + _random.Next(3), LocaleLetters)),
            Order.Composite => Encoding.Latin1.GetBytes(Letters(1 + _random.Next(3), "ABCD")),
            _ => NewKey()[..1],
        };

        /// <summary>The longest WEIGHTED and LOCALE keys (the key's length its order is declared with).</summary>
        private const int WeightedKeyLength = 36;

        private const int LocaleKeyLength = 12;

        /// <summary>Letters that differ at the primary (a, b, c), secondary (á, à, ç) and tertiary (A, B) levels, so a
        /// key's order and its prefix's disagree often.</summary>
        private const string LocaleLetters = "aáàAbBcç";

        private string Letters(int length, string alphabet) =>
            string.Create(length, (this, alphabet), static (span, s) =>
            {
                for (int i = 0; i < span.Length; i++) span[i] = s.alphabet[s.Item1._random.Next(s.alphabet.Length)];
            });

        /// <summary>A value: mostly short, often at and past the inline threshold and past one overflow page, twice 1 MiB.</summary>
        private byte[] NewValue()
        {
            int pick = _random.Next(1000);
            int length = pick switch
            {
                < 2 when _bigValues < 2 => 1 << 20,
                < 25 => BTree.MaxInlineValueBytes + 1,
                < 45 => OverflowChain.PayloadBytes + 9,
                < 60 => BTree.MaxInlineValueBytes,
                < 75 => 0,
                _ => 1 + _random.Next(120),
            };
            if (length == 1 << 20) _bigValues++;
            var value = new byte[length];
            _random.NextBytes(value);
            return value;
        }
    }

    /// <summary><c>ALPHABET IS "ZYX…A"</c>: the 26 letters specified in reverse, every other character after them in native
    /// order (§12.3.7.4 GR7 k), so the WEIGHTED order differs from the bytes' order for every pair of letters.</summary>
    internal static AlphanumericCollation ReversedAlphabet()
    {
        var codes = Enumerable.Range('A', 26).Select(c => (ushort)c).ToArray();
        var positions = codes.Select(c => (ushort)('Z' - c)).ToArray();
        var representatives = Enumerable.Range(0, 26).Select(i => (ushort)('Z' - i)).ToArray();
        return new AlphanumericCollation(codes, positions, representatives, 26, 'A', 'Z');
    }

    /// <summary>The auditor's check of a tree's SHAPE from the raw medium: every node decodes and fits its page; keys
    /// ascend within each node; every key lies within its separators (at or above the one before its child, below the one
    /// after); every leaf is at one depth; no leaf but the root is empty; an inline value obeys the inline rule and an
    /// overflow value exceeds it; the root's header carries marks at or above every key's (<see cref="IKeyOrder.MarksOf"/>,
    /// 2.2.1.4) and every other page zeros. The depth (0 for an empty tree), with the entry and overflow-value counts.</summary>
    internal static int AuditShape(MemoryPageMedium medium, PagePointer root, IKeyOrder order, out int entries, out int overflow)
    {
        entries = overflow = 0;
        if (root.IsNone) return 0;
        int leafDepth = -1, count = 0, overflowed = 0;
        TreeMarks rootMarks = default, keyMarks = default;
        void Walk(PagePointer at, int depth, byte[]? low, byte[]? high, bool isRoot)
        {
            var node = StoreKit.ReadNode(medium, at) ?? throw new Xunit.Sdk.XunitException($"tree page {at.Page} does not verify");
            Assert.True(node.UsedBytes <= PageHeader.BodyBytes);
            if (isRoot) rootMarks = node.Marks;
            else Assert.True(node.Marks == default, $"page {at.Page}: a page that is not the root carries marks");
            foreach (var key in node.Keys) keyMarks = keyMarks.Max(order.MarksOf(key));
            for (int i = 0; i < node.Keys.Count; i++)
            {
                if (i > 0) Assert.True(order.Compare(node.Keys[i - 1], node.Keys[i]) < 0, $"page {at.Page}: keys out of order");
                if (low is not null) Assert.True(order.Compare(node.Keys[i], low) >= 0, $"page {at.Page}: a key below its separator");
                if (high is not null) Assert.True(order.Compare(node.Keys[i], high) < 0, $"page {at.Page}: a key at or above the next separator");
            }
            if (node.Kind == PageKind.Leaf)
            {
                if (leafDepth < 0) leafDepth = depth;
                Assert.Equal(leafDepth, depth);
                Assert.True(isRoot || node.Keys.Count > 0, $"leaf {at.Page} is empty");
                for (int i = 0; i < node.Keys.Count; i++)
                {
                    var v = node.Values[i];
                    if (v.Inline is { } inline)
                        Assert.True(inline.Length <= BTree.MaxInlineValueBytes && 6 + node.Keys[i].Length + inline.Length <= BTree.MaxLeafEntryBytes);
                    else
                    {
                        overflowed++;
                        Assert.True(v.Total > BTree.MaxInlineValueBytes || 6 + node.Keys[i].Length + v.Total > BTree.MaxLeafEntryBytes);
                    }
                }
                count += node.Keys.Count;
                return;
            }
            Assert.True(node.Keys.Count > 0, $"internal page {at.Page} has no key");
            for (int c = 0; c < node.Children.Count; c++)
                Walk(node.Children[c], depth + 1, c == 0 ? low : node.Keys[c - 1], c == node.Keys.Count ? high : node.Keys[c], isRoot: false);
        }
        Walk(root, 1, null, null, isRoot: true);
        Assert.True(rootMarks.Deficit >= keyMarks.Deficit && rootMarks.Overhang >= keyMarks.Overhang,
            $"the root's marks {rootMarks} are below a key's {keyMarks}");
        entries = count;
        overflow = overflowed;
        return leafDepth;
    }
}
