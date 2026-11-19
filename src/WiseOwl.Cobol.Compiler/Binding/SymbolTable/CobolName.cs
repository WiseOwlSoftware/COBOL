// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// The key of every name table of <see cref="SymbolTable"/> (docs/rearchitecture/DESIGN-listing.md section 6.7.1, "The name
/// key is a type"): a COBOL word, equal to another exactly when the two spellings are the same name under the Annex C fold
/// (<see cref="CobolNames.Comparer"/>, the equality every string-keyed table of the binder used before it). A table keyed by
/// <c>CobolName</c> has a type a test can see, so "a member that takes a name" is visible to
/// <c>NameTableOwnershipDriftTests</c>; and only <see cref="SymbolTable"/> makes one (its rule (2)), so a name key is never
/// minted beside the table.
/// </summary>
public readonly struct CobolName : IEquatable<CobolName>
{
    /// <summary>The word as the declaration (or the reference) spelled it.</summary>
    public string Word { get; }

    internal CobolName(string word) => Word = word;

    /// <summary>The two spellings are one name: the one definition of COBOL name equality (Annex C).</summary>
    public bool Equals(CobolName other) => CobolNames.Comparer.Equals(Word, other.Word);

    public override bool Equals(object? obj) => obj is CobolName other && Equals(other);

    public override int GetHashCode() => CobolNames.Comparer.GetHashCode(Word);

    public static bool operator ==(CobolName left, CobolName right) => left.Equals(right);

    public static bool operator !=(CobolName left, CobolName right) => !left.Equals(right);

    public override string ToString() => Word;
}
