// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Runtime.InteropServices;

namespace WiseOwl.Cobol.Binding.Model;

// The three restoring shapes of docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1) (kb/Work PB2296). They hold
// no binder type, so they live in compiler.model, below both the binder (ProcedureBindState) and code generation
// (EmitContext.Options), each of which keeps its own instance.

/// <summary>A SCOPED OVERRIDE (DESIGN-architecture-review §8.3 (1)): the values pushed over a root by the scopes now open. <see cref="Push"/>
/// returns the scope whose disposal restores the value the push replaced, so an override can only end where it began.
/// The root is not held here: it is the unit's own value (its OPTIONS slice, written by its producer), which the reader
/// passes to <see cref="Over"/>, so the override is a value with no reference to the state it overrides.</summary>
public sealed class Scoped<T>
{
    private T _pushed = default!;
    private int _depth;

    /// <summary>The innermost pushed value, else <paramref name="root"/>.</summary>
    public T Over(T root) => _depth > 0 ? _pushed : root;

    /// <summary>Override the value with <paramref name="value"/> until the returned scope is disposed.</summary>
    public RestoreScope Push(T value)
    {
        var scope = new RestoreScope(this, _pushed, _depth);
        _pushed = value;
        _depth++;
        return scope;
    }

    /// <summary>The scope of one <see cref="Push"/>: disposing it restores the value the push replaced.</summary>
    public readonly struct RestoreScope(Scoped<T> owner, T previous, int depth) : IDisposable
    {
        /// <inheritdoc/>
        public void Dispose()
        {
            owner._pushed = previous;
            owner._depth = depth;
        }
    }
}

/// <summary>A SCOPE CURSOR (DESIGN-architecture-review §8.3 (1)): a position in the walk that a scope enters and its exit restores.
/// <see cref="Enter"/> returns the scope whose disposal puts back the value the entry replaced; <see cref="Advance"/>
/// moves the cursor inside the scope now open (an operand-evaluation count growing as the statement drains its steps),
/// and that scope's exit discards the move with the rest of the scope.</summary>
public sealed class Cursor<T>(T initial)
{
    /// <summary>The cursor's value now.</summary>
    public T Current { get; private set; } = initial;

    /// <summary>Set the cursor to <paramref name="value"/> until the returned scope is disposed.</summary>
    public RestoreScope Enter(T value)
    {
        var scope = new RestoreScope(this, Current);
        Current = value;
        return scope;
    }

    /// <summary>Move the cursor to <paramref name="value"/> within the scope now open; that scope's exit restores the value
    /// it entered over.</summary>
    public void Advance(T value) => Current = value;

    /// <summary>The scope of one <see cref="Enter"/>: disposing it restores the value the entry replaced.</summary>
    public readonly struct RestoreScope(Cursor<T> owner, T previous) : IDisposable
    {
        /// <inheritdoc/>
        public void Dispose() => owner.Current = previous;
    }
}

/// <summary>An ORDERED DRAIN QUEUE (DESIGN-architecture-review §8.3 (1)): entries in registration order, taken by the binder that marked the queue
/// before they were registered. <see cref="Mark"/> is the position the next entry takes; <see cref="DrainFrom"/> returns and
/// removes every entry after a mark, in registration order. The queue admits no other removal: an entry leaves only with
/// the suffix of the mark it was registered after. An entry may be placed at an earlier position of the queue
/// (<see cref="InsertAt"/>), the evaluation-order placement a freeze needs (§2023:14.6.4: an operand's evaluation precedes a
/// later operand's).</summary>
public sealed class DrainQueue<T>
{
    private readonly List<T> _entries = [];

    /// <summary>The position the next registered entry takes — the mark a binder takes before the operands whose entries it
    /// owns bind.</summary>
    public int Mark() => _entries.Count;

    /// <summary>True when some entry was registered after <paramref name="mark"/>.</summary>
    public bool AnyAfter(int mark) => _entries.Count > mark;

    /// <summary>Register <paramref name="entry"/> at the end of the queue.</summary>
    public void Add(T entry) => _entries.Add(entry);

    /// <summary>Register <paramref name="entries"/>, in their order, at the end of the queue.</summary>
    public void AddRange(IEnumerable<T> entries) => _entries.AddRange(entries);

    /// <summary>Place <paramref name="entry"/> at <paramref name="position"/>, ahead of the entries registered after that
    /// position.</summary>
    public void InsertAt(int position, T entry) => _entries.Insert(position, entry);

    /// <summary>Place <paramref name="entries"/>, in their order, at <paramref name="position"/>.</summary>
    public void InsertAt(int position, IEnumerable<T> entries) => _entries.InsertRange(position, entries);

    /// <summary>Take every entry registered after <paramref name="mark"/>, in registration order, removing them from the
    /// queue. Empty when none was.</summary>
    public List<T> DrainFrom(int mark)
    {
        if (_entries.Count <= mark) return [];
        var taken = _entries.GetRange(mark, _entries.Count - mark);
        _entries.RemoveRange(mark, taken.Count);
        return taken;
    }

    /// <summary>The entries registered after <paramref name="mark"/>, in registration order, left in the queue (a binder
    /// that reads what an operand registered without taking it).</summary>
    public ReadOnlySpan<T> Since(int mark) => CollectionsMarshal.AsSpan(_entries)[mark..];
}
