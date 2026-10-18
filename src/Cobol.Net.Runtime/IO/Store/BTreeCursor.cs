// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO.Store;

internal sealed partial class BTree
{
    /// <summary>Position a cursor by <paramref name="relation"/> against <paramref name="key"/> — the whole key, or with
    /// <paramref name="prefix"/> the leading positions of each stored key (a START's partial key, §14.9.41.4 GR17 c) to
    /// e): the LESS relations search <i>"in reverse order"</i>, so they land on the LAST qualifying entry). The cursor has
    /// no current entry when none qualifies. A prefix the order does not narrow (<see cref="IKeyOrder.PrefixNarrows"/>: a
    /// partial key under a locale-based sequence) is the search GR17 c) and d) state, entry by entry from the first (or,
    /// for the LESS relations, the last); a prefix as long as the key is a binary search under every order.</summary>
    public StoreStatus Seek(ReadOnlySpan<byte> key, SeekRelation relation, out Cursor cursor, bool prefix = false)
    {
        var c = cursor = new Cursor(this);
        var probe = key.ToArray();
        return c.Guard(() => c.Position(probe, relation, prefix));
    }

    /// <summary>A cursor on the first entry (none in an empty tree).</summary>
    public StoreStatus First(out Cursor cursor)
    {
        var c = cursor = new Cursor(this);
        return c.Guard(c.PositionFirst);
    }

    /// <summary>A cursor on the last entry (none in an empty tree).</summary>
    public StoreStatus Last(out Cursor cursor)
    {
        var c = cursor = new Cursor(this);
        return c.Guard(c.PositionLast);
    }

    /// <summary>⛔ A CURSOR OVER THE TREE (2.2.1.4: READ NEXT and READ PREVIOUS are cursor steps): the root-to-leaf path
    /// of its current entry. A step within a leaf reads nothing; a step across leaves climbs the path to the first
    /// ancestor with a further child and descends from there, reading only the pages it descends through (the tree has
    /// no sibling pointers, <see cref="BTree"/>). A cursor whose tree changed since it was positioned (an edit, a flush,
    /// a discard, an adoption — the tree's change counter moved, whether or not its root did) re-seeks from its current
    /// key — strictly above it for <see cref="Next"/>, strictly below for <see cref="Previous"/> — so a step never walks
    /// a superseded path, nor a node an edit changed in place under it.</summary>
    internal sealed class Cursor
    {
        private readonly BTree _tree;
        private readonly List<(Node Node, int Index)> _path = [];
        private long _positionedAt;

        internal Cursor(BTree tree) => _tree = tree;

        /// <summary>Whether the cursor is on an entry.</summary>
        public bool HasCurrent { get; private set; }

        /// <summary>The current entry's key (empty with no current entry).</summary>
        public byte[] Key { get; private set; } = [];

        /// <summary>The current entry's value, its overflow chain read and verified when it has one. When the tree changed
        /// since the cursor was positioned the value is looked up by the current key (the path's nodes may have changed
        /// in place); a current entry the change deleted is the caller's defect and is refused loudly.</summary>
        public StoreStatus ReadValue(out byte[] value)
        {
            if (!HasCurrent) throw new InvalidOperationException("the cursor has no current entry");
            if (_tree._version != _positionedAt)
            {
                var looked = _tree.TryGet(Key, out var current);
                if (looked == StoreStatus.Ok && current is null)
                    throw new InvalidOperationException("the cursor's current entry was deleted after it was positioned");
                value = current ?? [];
                return looked;
            }
            byte[] read = [];
            var status = Guard(() =>
            {
                var (leaf, at) = _path[^1];
                read = _tree.ValueOf(leaf.Slots[at]);
            }, keepPosition: true);
            value = read;
            return status;
        }

        /// <summary>Step to the next entry in key order (none past the last).</summary>
        public StoreStatus Next() => Step(forward: true);

        /// <summary>Step to the previous entry in key order (none before the first).</summary>
        public StoreStatus Previous() => Step(forward: false);

        private StoreStatus Step(bool forward)
        {
            if (!HasCurrent) return StoreStatus.Ok;
            var key = Key;
            if (_tree._version != _positionedAt)
                return Guard(() => Position(key, forward ? SeekRelation.GreaterThan : SeekRelation.LessThan, prefix: false));
            return Guard(() =>
            {
                var (leaf, at) = _path[^1];
                _path[^1] = (leaf, forward ? at + 1 : at - 1);
                Settle(forward);
            });
        }

        internal StoreStatus Guard(Action move, bool keepPosition = false)
        {
            try
            {
                move();
                return StoreStatus.Ok;
            }
            catch (TornPage)
            {
                if (!keepPosition) Clear();
                return StoreStatus.PermanentError;
            }
        }

        internal void Position(byte[] probe, SeekRelation relation, bool prefix)
        {
            if (prefix && !_tree._order.PrefixNarrows(probe.Length))
            {
                Search(probe, relation);
                return;
            }
            Begin();
            if (IsEmpty(_tree._root)) return;
            // GreaterThan and LessOrEqual find the first entry ABOVE the probe; the others the first AT OR ABOVE it.
            bool above = relation is SeekRelation.GreaterThan or SeekRelation.LessOrEqual;
            var node = _tree.Load(_tree._root);
            while (!node.IsLeaf)
            {
                if (_path.Count > MaxDepth) throw new TornPage(PageCheck.ChecksumFails);
                // A separator s heads the keys at or above it; a prefix at or below the probe's can still precede s, so
                // the descent toward the first entry AT OR ABOVE a PREFIX counts only the separators strictly below it.
                int child = _tree.Bound(node.Keys, probe, orEqual: above || !prefix, prefix);
                _path.Add((node, child));
                node = _tree.Load(node.Children[child]);
            }
            _path.Add((node, _tree.Bound(node.Keys, probe, orEqual: above, prefix)));
            Settle(forward: true);
            switch (relation)
            {
                case SeekRelation.Equal:
                    if (HasCurrent && Compare(probe, prefix) != 0) Clear();
                    break;
                case SeekRelation.LessThan or SeekRelation.LessOrEqual:
                    if (HasCurrent)
                    {
                        var (leaf, at) = _path[^1];
                        _path[^1] = (leaf, at - 1);
                        Settle(forward: false);
                    }
                    else PositionLast();
                    break;
            }
        }

        /// <summary>⛔ THE SEQUENTIAL SEARCH OF §14.9.41.4 GR17 c) and d), for a prefix whose order a binary search cannot
        /// narrow (<see cref="IKeyOrder.PrefixNarrows"/>): from the first entry forward for EQUAL and the GREATER
        /// relations, from the last backward for the LESS ones, stopping on the first entry whose prefix satisfies the
        /// relation (GR17 e) 1.); none satisfies it and the cursor has no current entry (GR17 e) 2.).</summary>
        private void Search(byte[] probe, SeekRelation relation)
        {
            bool forward = relation is SeekRelation.Equal or SeekRelation.GreaterThan or SeekRelation.GreaterOrEqual;
            if (forward) PositionFirst();
            else PositionLast();
            while (HasCurrent)
            {
                int c = Compare(probe, prefix: true);
                bool satisfied = relation switch
                {
                    SeekRelation.Equal => c == 0,
                    SeekRelation.GreaterThan => c > 0,
                    SeekRelation.GreaterOrEqual => c >= 0,
                    SeekRelation.LessThan => c < 0,
                    _ => c <= 0,
                };
                if (satisfied) return;
                var (leaf, at) = _path[^1];
                _path[^1] = (leaf, forward ? at + 1 : at - 1);
                Settle(forward);
            }
        }

        internal void PositionFirst()
        {
            Begin();
            if (IsEmpty(_tree._root)) return;
            Descend(_tree._root, leftmost: true);
            Settle(forward: true);
        }

        internal void PositionLast()
        {
            Begin();
            if (IsEmpty(_tree._root)) return;
            Descend(_tree._root, leftmost: false);
            Settle(forward: false);
        }

        private int Compare(byte[] probe, bool prefix) =>
            prefix ? _tree._order.ComparePrefix(Key, probe) : _tree._order.Compare(Key, probe);

        private void Begin()
        {
            Clear();
            _positionedAt = _tree._version;
        }

        private void Clear()
        {
            _path.Clear();
            HasCurrent = false;
            Key = [];
        }

        /// <summary>From a leaf position that may lie past either end of its leaf, move to the nearest entry in the
        /// direction of travel, crossing leaves (empty ones included) through the path.</summary>
        private void Settle(bool forward)
        {
            while (true)
            {
                var (leaf, at) = _path[^1];
                if (at >= 0 && at < leaf.Keys.Count)
                {
                    HasCurrent = true;
                    Key = leaf.Keys[at];
                    return;
                }
                if (!(forward ? CrossForward() : CrossBackward()))
                {
                    Clear();
                    return;
                }
            }
        }

        private bool CrossForward()
        {
            _path.RemoveAt(_path.Count - 1);
            while (_path.Count > 0)
            {
                var (node, at) = _path[^1];
                if (at + 1 < node.Children.Count)
                {
                    _path[^1] = (node, at + 1);
                    Descend(node.Children[at + 1], leftmost: true);
                    return true;
                }
                _path.RemoveAt(_path.Count - 1);
            }
            return false;
        }

        private bool CrossBackward()
        {
            _path.RemoveAt(_path.Count - 1);
            while (_path.Count > 0)
            {
                var (node, at) = _path[^1];
                if (at > 0)
                {
                    _path[^1] = (node, at - 1);
                    Descend(node.Children[at - 1], leftmost: false);
                    return true;
                }
                _path.RemoveAt(_path.Count - 1);
            }
            return false;
        }

        private void Descend(Child from, bool leftmost)
        {
            var node = _tree.Load(from);
            while (!node.IsLeaf)
            {
                if (_path.Count > MaxDepth) throw new TornPage(PageCheck.ChecksumFails);
                int child = leftmost ? 0 : node.Children.Count - 1;
                _path.Add((node, child));
                node = _tree.Load(node.Children[child]);
            }
            _path.Add((node, leftmost ? 0 : node.Keys.Count - 1));
        }
    }
}
