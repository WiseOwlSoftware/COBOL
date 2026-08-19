// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections;

namespace CobolNet.Runtime.Unicode.Segmentation;

/// <summary>
/// The grapheme clusters of one text, in order — an <see cref="IEnumerable{T}"/> of <see cref="GraphemeCluster"/>
/// whose <c>foreach</c> form allocates nothing (a struct enumerator over the source string; each cluster is a value
/// naming a slice). <c>GraphemeBreaker.Enumerate(text)</c> creates one; <see cref="Count"/> and <see cref="ToArray"/>
/// are the eager forms.
/// </summary>
public readonly struct GraphemeEnumerator : IEnumerable<GraphemeCluster>
{
    private readonly string _text;

    /// <summary>The clusters of <paramref name="text"/> (null = empty).</summary>
    public GraphemeEnumerator(string? text)
    {
        _text = text ?? "";
    }

    /// <summary>The text being segmented.</summary>
    public string Text => _text ?? "";

    /// <summary>The number of clusters (walks the text once).</summary>
    public int Count => GraphemeBreaker.Count(Text);

    /// <summary>Every cluster, materialized.</summary>
    public GraphemeCluster[] ToArray()
    {
        var list = new List<GraphemeCluster>(Text.Length);
        foreach (var c in this) list.Add(c);
        return list.ToArray();
    }

    /// <summary>The allocation-free enumerator <c>foreach</c> binds to.</summary>
    public Enumerator GetEnumerator() => new(Text);

    IEnumerator<GraphemeCluster> IEnumerable<GraphemeCluster>.GetEnumerator() => new Enumerator(Text);

    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(Text);

    /// <summary>Walks the clusters of a text: each <see cref="MoveNext"/> finds the next boundary.</summary>
    public struct Enumerator : IEnumerator<GraphemeCluster>
    {
        private readonly string _text;
        private int _pos;

        internal Enumerator(string text)
        {
            _text = text;
            _pos = 0;
            Current = default;
        }

        /// <summary>The current cluster.</summary>
        public GraphemeCluster Current { get; private set; }

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_pos >= _text.Length) return false;
            int end = GraphemeBreaker.NextBoundary(_text, _pos);
            Current = new GraphemeCluster(_text, _pos, end - _pos);
            _pos = end;
            return true;
        }

        public void Reset()
        {
            _pos = 0;
            Current = default;
        }

        public void Dispose()
        {
        }
    }
}
