// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections;
using System.Collections.Immutable;

namespace WiseOwl.Cobol.Binding.Model;

/// <summary>
/// An immutable list that compares by its ELEMENTS, so a record holding one is a value (DESIGN-external-repository §17.4, §17.7). An
/// <see cref="ImmutableArray{T}"/> compares by reference, which would make two encodings of one unit unequal. It lives in the model
/// layer because the repository's records (<c>Binding.Repository</c>) and the model types the emitter reads (the expansion identity
/// and the skeleton record, <c>SkeletonRecord.cs</c>) both hold lists and the emitter may read only the model.
/// </summary>
internal readonly struct RecordList<T>(ImmutableArray<T> items) : IReadOnlyList<T>, IEquatable<RecordList<T>>
{
    private readonly ImmutableArray<T> _items = items.IsDefault ? [] : items;

    /// <summary>The empty list.</summary>
    public static RecordList<T> Empty { get; } = new([]);

    public T this[int index] => _items[index];

    public int Count => _items.IsDefault ? 0 : _items.Length;

    public bool Equals(RecordList<T> other) =>
        Count == other.Count && (Count == 0 || _items.SequenceEqual(other._items, EqualityComparer<T>.Default));

    public override bool Equals(object? obj) => obj is RecordList<T> other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in this) hash.Add(item);
        return hash.ToHashCode();
    }

    public static bool operator ==(RecordList<T> left, RecordList<T> right) => left.Equals(right);

    public static bool operator !=(RecordList<T> left, RecordList<T> right) => !left.Equals(right);

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)(_items.IsDefault ? [] : _items)).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
