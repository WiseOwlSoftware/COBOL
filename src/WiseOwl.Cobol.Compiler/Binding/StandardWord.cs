// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;

namespace WiseOwl.Cobol.Compiler.Binding;

/// <summary>
/// A word the STANDARD or the IMPLEMENTOR defines, never the programmer: an intrinsic-function-name (§2023:8.11) or an
/// implementor-name this implementation specifies. It is the key of the catalogs of such words (<see cref="IntrinsicCatalog"/>,
/// <see cref="IntrinsicArgumentRules.Verified"/>, <see cref="ImplementorNameRegistry"/>), so those tables are keyed by a type of
/// their own and never by a <c>string</c> that could hold a COBOL user word: the program's names are <see cref="SymbolTable"/>'s
/// alone (docs/rearchitecture/DESIGN-listing.md sections 6.7.1 and 6.7.2's last row; kb/Work PB5932). Two spellings are the same
/// word on the BASIC LETTERS ALONE (<see cref="CobolKeywords"/>, §2023:8.1.3.2 GR3a): such a word is written in basic letters,
/// so a spelling holding an extended letter is no such word.
/// </summary>
public readonly struct StandardWord : IEquatable<StandardWord>
{
    /// <summary>The word as written.</summary>
    public string Word { get; }

    public StandardWord(string word) => Word = word;

    public bool Equals(StandardWord other) => CobolKeywords.Comparer.Equals(Word, other.Word);

    public override bool Equals(object? obj) => obj is StandardWord other && Equals(other);

    public override int GetHashCode() => CobolKeywords.Comparer.GetHashCode(Word);

    public static bool operator ==(StandardWord left, StandardWord right) => left.Equals(right);

    public static bool operator !=(StandardWord left, StandardWord right) => !left.Equals(right);

    public override string ToString() => Word;
}
