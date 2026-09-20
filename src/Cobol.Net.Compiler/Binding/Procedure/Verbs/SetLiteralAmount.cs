// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Expressions;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>The bound a SET literal amount shall satisfy, as the format that writes the literal states it —
/// the whole rule as data, so each call site names its own operand and its own syntax rule and NOTHING is
/// re-derived. <paramref name="Max"/> null means the rule states no upper bound for this statement.</summary>
/// <param name="Min">The least conforming value.</param>
/// <param name="Max">The greatest conforming value, or null when the rule states none here.</param>
/// <param name="MinWhat">What <paramref name="Min"/> IS, in the standard's words ("nonnegative", "the minimum
/// capacity defined in the corresponding OCCURS clause").</param>
/// <param name="MaxWhat">What <paramref name="Max"/> IS, in the standard's words.</param>
/// <param name="Operand">The general format's own name for the literal — "integer-1", "integer-2".</param>
/// <param name="Rule">The citation, e.g. <c>ISO §14.9.39.3 SR30</c>.</param>
internal readonly record struct SetAmountBound(
    System.Int128 Min, System.Int128? Max, string MinWhat, string MaxWhat, string Operand, string Rule);

/// <summary>⛔ THE ONE COMPILE-TIME SCREEN FOR A SET AMOUNT WRITTEN AS A LITERAL (kb/Work PB458).
/// <para>§14.9.39.2 writes each amount-taking SET format as a CHOICE between a literal and an arithmetic
/// expression, and puts a SYNTAX rule on the literal alternative (§14.9.39.3 SR30 for Format 14's integer-1,
/// SR34 for Format 16's integer-2) beside a GENERAL rule on the expression alternative (§14.9.39.4 GR29/GR30,
/// GR37/GR38). Before this, only the general rules existed and the literal alternative fell through to them, so
/// the compile-time REJECTION the standard requires was replaced by a run-time CLAMP that produces a DIFFERENT
/// value: <c>SET WS-CAP TO 1</c> over <c>FROM 2 TO 10</c> ran and answered 2, and <c>SET SIZE OF D TO 99</c>
/// over <c>LIMIT IS 8</c> ran and answered 8 — each a program the standard requires the compiler to refuse.</para>
/// <para>⛔ The screen is ONE function over a declared <see cref="SetAmountBound"/>, not an <c>if</c> per format:
/// the literal alternative appears in exactly two printed formats today (Format 14's integer-1 and Format 16's
/// integer-2 — Format 1's, Format 2's and Format 10's amounts are arithmetic-expression-1/-2/-3 with NO literal
/// alternative and no syntax rule), and the next amount-taking format is a <see cref="SetAmountBound"/> at its
/// bind site rather than a third copy of the comparison.</para></summary>
internal static class SetLiteralAmount
{
    /// <summary>The value of <paramref name="amount"/> when it is integer-1 / integer-2 — an operand that
    /// CONSISTS OF a single integer literal — or null when it is arithmetic-expression-4 / -5 and the format's
    /// general rule governs instead.
    /// <para>§5.5 rule 1 is what makes this a literal test and not a value test: "When the term 'integer-n'
    /// (n = 1, 2, …) is used in a general format and associated rules, it refers to a fixed-point integer
    /// literal that shall be unsigned and nonzero unless otherwise specified in the associated rules." SR30's
    /// "shall be nonnegative" and SR34's "shall be non-negative" are that otherwise-specification, which is why
    /// a negative literal is a VIOLATION of them rather than a non-match; and it is why an amount written as a
    /// DATA ITEM is arithmetic-expression-4/-5, governed by the general rule at run time, however small its
    /// value.</para>
    /// <para>⛔ The literal test is <see cref="SoleOperand.NumericLiteral"/>, THE one §8.3.3.3.2 rule-2 reading
    /// shared with DEFINE, CONSTANT, EVALUATE and the relation condition: a sign ADJACENT to the digits is part
    /// of the literal (so <c>TO -1</c> IS integer-1 and violates "shall be nonnegative"), while a separated
    /// <c>TO - 1</c> is a unary operator over a literal — an arithmetic expression, which the format's general
    /// rule screens at run time. A second contiguity test here is exactly how one spelling becomes a literal on
    /// one path and an expression on another (kb/Work PB400).</para></summary>
    public static System.Int128? Of(Core.ArithmeticExpressionContext? amount)
    {
        if (SoleOperand.NumericLiteral(amount) is not { } text) return null;
        string t = text.Trim();
        bool negative = t.StartsWith('-');
        if (negative || t.StartsWith('+')) t = t[1..];
        // §8.3.3.3.2 — an INTEGER literal has no decimal point (nor the DECIMAL-POINT IS COMMA spelling, nor an
        // exponent): those spell a non-integer literal, which is arithmetic-expression-4/-5 for these formats.
        if (t.Length == 0 || !System.Int128.TryParse(t, out var v)) return null;
        return negative ? -v : v;
    }

    /// <summary>Why <paramref name="value"/> violates <paramref name="bound"/>, in the rule's own words, or null
    /// when it conforms.</summary>
    public static string? Violation(System.Int128 value, in SetAmountBound bound) =>
        value < bound.Min ? $"{bound.Operand} is {value} and shall be {bound.MinWhat}"
        : bound.Max is { } max && value > max ? $"{bound.Operand} is {value} and shall be {bound.MaxWhat}"
        : null;
}
