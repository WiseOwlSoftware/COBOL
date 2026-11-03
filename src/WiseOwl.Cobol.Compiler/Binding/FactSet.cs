// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections;

namespace WiseOwl.Cobol.Binding;

/// <summary>The APPEND view of a <see cref="FactSet{T}"/>: a pass that records facts of this set holds only this, so it can
/// add a fact and do nothing else (docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1), kb/Work PB2296).</summary>
public interface IAppends<in T>
{
    /// <summary>Record one fact. Recording a fact already recorded changes nothing.</summary>
    void Add(T fact);
}

/// <summary>The MID-BIND read of a <see cref="FactSet{T}"/>: "recorded so far". A fact recorded by an earlier statement's
/// bind is visible to a later statement's and one recorded later is not, because the binder appends and reads in the
/// order it binds; that statement-order visibility is the contract of this view, and a reader that needs the complete set
/// holds <see cref="IReads{T}"/> instead (§8.3 (1)).</summary>
public interface IReadsEarly<in T>
{
    /// <summary>True when <paramref name="fact"/> has been recorded at this point of binding.</summary>
    bool RecordedSoFar(T fact);
}

/// <summary>The WHOLE read of a <see cref="FactSet{T}"/>: every fact the set will ever hold. It is legal only after the
/// last phase that appends to the set (§8.3 (1); <c>BindPipeline.ValidateDag</c> checks a pass's declared views against
/// the phase order once the passes declare them, kb/Work PB2352).</summary>
public interface IReads<T> : IReadOnlyCollection<T>
{
    /// <summary>True when <paramref name="fact"/> is in the set.</summary>
    bool Contains(T fact);
}

/// <summary>One APPEND-ONLY fact set of <see cref="DataBindState"/> (§8.3 (1)): facts that accumulate across bind phases,
/// which no single-producer value slice can hold. It admits no removal and no reset, so a fact once recorded stays
/// recorded; each view (<see cref="IAppends{T}"/>, <see cref="IReadsEarly{T}"/>, <see cref="IReads{T}"/>) is the one
/// thing its holder may do with the set.</summary>
public sealed class FactSet<T> : IAppends<T>, IReadsEarly<T>, IReads<T>
{
    private readonly HashSet<T> _facts;

    /// <summary>An empty set whose facts are equal under <paramref name="comparer"/> (the type's own equality when null).</summary>
    public FactSet(IEqualityComparer<T>? comparer = null) => _facts = new HashSet<T>(comparer);

    /// <inheritdoc/>
    public void Add(T fact) => _facts.Add(fact);

    /// <inheritdoc/>
    public bool RecordedSoFar(T fact) => _facts.Contains(fact);

    /// <inheritdoc/>
    public bool Contains(T fact) => _facts.Contains(fact);

    /// <inheritdoc/>
    public int Count => _facts.Count;

    /// <summary>The facts, in the underlying <see cref="HashSet{T}"/>'s enumeration order, unchanged from before the set
    /// had views.</summary>
    public HashSet<T>.Enumerator GetEnumerator() => _facts.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _facts.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _facts.GetEnumerator();
}
