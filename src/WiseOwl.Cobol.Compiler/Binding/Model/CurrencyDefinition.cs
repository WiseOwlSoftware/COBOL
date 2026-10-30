// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Common;

namespace WiseOwl.Cobol.Binding.Model;

/// <summary>
/// What one currency PICTURE SYMBOL of a source unit stands for (ISO §12.3.7.3, the CURRENCY SIGN clause): the
/// currency STRING (literal-7's characters) AND the CLASS of that literal. The class is half of the definition, not an
/// afterthought: SR28 says "If literal-7 is of class alphanumeric, the associated currency symbol may be used only to
/// define a numeric-edited item with usage display. If literal-7 is of class national, the associated currency symbol
/// may be used only to define a numeric-edited item with usage national", so a set that recorded symbol → string alone
/// (kb/Work PB1089) made the rule unenforceable at every PICTURE consumer.
/// <para>Record equality IS the identity SR21 asks of two clauses that specify equivalent symbols ("unless they specify
/// identical currency strings"): two strings that spell the same characters in different classes are not identical,
/// for SR28 would then forbid every use of the symbol.</para>
/// </summary>
/// <param name="Text">The currency string — literal-7's characters.</param>
/// <param name="LiteralClass">The class of literal-7: alphanumeric or national (a boolean literal is refused before it
/// reaches the set). The clause SR25 implies, <c>CURRENCY SIGN '$' PICTURE SYMBOL '$'</c>, has an alphanumeric
/// literal-7.</param>
public readonly record struct CurrencyDefinition(string Text, LiteralClass LiteralClass)
{
    /// <summary>The currency symbol <c>$</c> as SR25's implied clause defines it: the string "$", alphanumeric.</summary>
    public static CurrencyDefinition ImpliedDollar { get; } = new("$", LiteralClass.Alphanumeric);

    /// <summary>True when the clause that named this PICTURE SYMBOL was REFUSED for its currency string (literal-7
    /// broke SR18, SR23 a) or SR23 b)), so the string and its class are unknown (kb/Work PB1920). The symbol stays a
    /// currency symbol — a PICTURE that writes it is the program the user meant, and reading it as an invalid PICTURE
    /// character (COBOL0808 / COBOL0819) would bury the clause's one diagnostic under a cascade — but no rule that
    /// reads the string or the class (SR21, SR28, §15.68.3 r3) is asked of it.</summary>
    public bool ClauseRefused { get; init; }

    /// <summary>The placeholder a refused clause registers for <paramref name="symbol"/>: the symbol as its own
    /// one-character string, which only sizes a PICTURE of a compilation that has already failed.</summary>
    public static CurrencyDefinition OfRefusedClause(char symbol) =>
        new(symbol.ToString(), LiteralClass.Alphanumeric) { ClauseRefused = true };
}
