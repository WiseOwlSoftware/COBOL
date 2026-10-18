// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO.Store;

internal sealed partial class BTree
{
    /// <summary>Position a cursor by <paramref name="relation"/> against <paramref name="key"/> — the whole key, or with
    /// <paramref name="prefix"/> the leading positions of each stored key (a START's partial key, §14.9.41.4 GR17 c) to
    /// e): the LESS relations search <i>"in reverse order"</i>, so they land on the LAST qualifying entry). The cursor has
    /// no current entry when none qualifies.</summary>
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
    /// no sibling pointers, <see cref="BTree"/>). A cursor whose tree changed since it was positioned (an edit, a flush)
    /// re-seeks from its current key — strictly above it for <see cref="Next"/>, strictly below for
    /// <see cref="Previous"/> — so a step never walks a superseded path.</summary>
    internal sealed class Cursor
    {
        private readonly BTree _tree;
        private readonly List<(Node Node, int Index)> _path = [];
        private Child _positionedUnder;

        internal Cursor(BTree tree) => _tree = tree;

        /// <summary>Whether the cursor is on an entry.</summary>
        public bool HasCurrent { get; private set; }

        /// <summary>The current entry's key (empty with no current entry).</summary>
        public byte[] Key { get; private set; } = [];

        /// <summary>The current entry's value, its overflow chain read and verified when it has one.</summary>
        public StoreStatus ReadValue(out byte[] value)
        {
            if (!HasCurrent) throw new InvalidOperationException("the cursor has no current entry");
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
            if (_tree._root != _positionedUnder)
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
            _positionedUnder = _tree._root;
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
