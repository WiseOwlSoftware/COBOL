// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;

namespace WiseOwl.Cobol.Frontend.Preprocessor;

/// <summary>
/// ⛔ THE SUBSCRIPT RESTRICTION OF A COPY REPLACING IDENTIFIER OPERAND (kb/Work PB5700): §2002:7.1.2.2 SR9 and §2014:7.2.2.2 SR9 —
/// "If subscripting is specified, it shall not include any arithmetic expressions as subscripts with the exception of a single
/// literal or identifier, or an identifier plus or minus an integer." The screen reads the text-words of one text-1 or text-2
/// operand (<see cref="CopyProcessor"/>'s identifier arm) and finds the first subscript group that holds an arithmetic expression
/// (<c>T(I * 2)</c>, <c>T(I + J)</c>, <c>T(1 + 2)</c>). A function-identifier's argument list is no subscript (its arguments are
/// arithmetic expressions), a group with a colon is a reference modifier, and a group is a subscript only where it stands behind a
/// word or a closing parenthesis — a parenthesis that opens an argument or a grouping is not one.
/// </summary>
internal static class ReplacingIdentifierSubscripts
{
    /// <summary>The span (<c>Start</c> inclusive, <c>End</c> exclusive, in the text the words were scanned from) of the first
    /// subscript group's content that holds an arithmetic expression, or null when every subscript is a single literal or
    /// identifier or an identifier plus or minus an integer.</summary>
    public static (int Start, int End)? FirstArithmeticSubscript(IReadOnlyList<TextWord> words)
    {
        bool function = words.Count > 0 && words[0].IsWord("FUNCTION");
        for (int open = 1; open < words.Count; open++)
        {
            if (!IsOpen(words[open]) || !StandsBehindAWord(words[open - 1])) continue;
            if (function && open == 2) continue;   // FUNCTION name (arguments): an argument list, not subscripts
            int close = Matching(words, open);
            var items = new List<TextWord>();
            bool referenceModifier = false;
            for (int i = open + 1, depth = 0; i < close; i++)
            {
                var w = words[i];
                if (w.Kind == TextWordKind.Separator && w.Span[0] == '(') depth++;
                else if (w.Kind == TextWordKind.Separator && w.Span[0] == ')') depth--;
                else if (depth == 0 && w.Kind == TextWordKind.Separator && w.Span[0] == ':') referenceModifier = true;
                else if (depth == 0) items.Add(w);
            }
            if (!referenceModifier && !IsSingleLiteralIdentifierOrOffset(items) && close > open + 1)
                return (words[open + 1].Start, words[close - 1].End);
        }
        return null;
    }

    private static bool IsOpen(in TextWord w) => w.Kind == TextWordKind.Separator && w.Span[0] == '(';

    /// <summary>A group is a subscript list, a reference modifier or an argument list where it follows a word that is no
    /// operator, or another group; behind an operator or an opening parenthesis it only groups an expression.</summary>
    private static bool StandsBehindAWord(in TextWord before)
        => before.Kind == TextWordKind.Literal
            || (before.Kind == TextWordKind.Separator && before.Span[0] == ')')
            || (before.Kind == TextWordKind.CharacterString && !IsOperator(before.Span));

    /// <summary>The index of the closing parenthesis of the group opened at <paramref name="open"/>; the last word when the text
    /// ends inside the group (the statement's own end reports it).</summary>
    private static int Matching(IReadOnlyList<TextWord> words, int open)
    {
        int depth = 0;
        for (int i = open; i < words.Count; i++)
        {
            if (words[i].Kind != TextWordKind.Separator) continue;
            if (words[i].Span[0] == '(') depth++;
            else if (words[i].Span[0] == ')' && --depth == 0) return i;
        }
        return words.Count;
    }

    /// <summary>Whether each subscript of the list (separated by spaces or a separator comma) is a literal, an identifier — a data-name
    /// with its qualifiers — or an identifier plus or minus an integer: the only subscripts §2002:7.1.2.2 SR9 leaves.</summary>
    private static bool IsSingleLiteralIdentifierOrOffset(List<TextWord> items)
    {
        bool identifier = false;      // the subscript so far is an identifier, which may take one offset
        bool offsetGiven = false;     // … and has taken it
        bool awaitsInteger = false;   // a + or - stands, an integer must follow
        bool qualifierNext = false;   // OF or IN stands, a qualifier follows
        foreach (var w in items)
        {
            if (w.Kind == TextWordKind.SeparatorCommaOrSemicolon) continue;
            if (awaitsInteger)
            {
                if (!IsUnsignedInteger(w.Span)) return false;
                awaitsInteger = false;
                offsetGiven = true;
                continue;
            }
            if (w.Kind == TextWordKind.Literal)
            {
                (identifier, offsetGiven) = (false, false);
                continue;
            }
            if (w.Kind != TextWordKind.CharacterString) return false;
            var span = w.Span;
            if (IsOperator(span))
            {
                if (span is not ("+" or "-") || !identifier || offsetGiven) return false;
                awaitsInteger = true;
                continue;
            }
            if (qualifierNext)
            {
                qualifierNext = false;
                continue;
            }
            if (identifier && !offsetGiven && IsSignedInteger(span))
            {
                offsetGiven = true;   // I -2: the sign belongs to the integer written after the space
                continue;
            }
            if (identifier && (w.IsWord("OF") || w.IsWord("IN")))
            {
                qualifierNext = true;
                continue;
            }
            identifier = IsName(span);
            offsetGiven = false;
        }
        return !awaitsInteger;
    }

    private static bool IsOperator(ReadOnlySpan<char> span) => span is "+" or "-" or "*" or "/" or "**";

    private static bool IsUnsignedInteger(ReadOnlySpan<char> span) => span.Length > 0 && !span.ContainsAnyExcept("0123456789");

    private static bool IsSignedInteger(ReadOnlySpan<char> span)
        => span.Length > 1 && span[0] is '+' or '-' && IsUnsignedInteger(span[1..]);

    /// <summary>A data-name, as against ALL or a numeric literal.</summary>
    private static bool IsName(ReadOnlySpan<char> span)
        => span.ContainsAnyExcept("+-.0123456789") && !CobolKeywords.Same(span, "ALL");
}
