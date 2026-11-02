// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Editions;

/// <summary>
/// ⛔ THE ONE COMPARISON OF A WRITTEN WORD WITH A KEYWORD — a reserved word, a compiler-directive word (§8.12), a phrase
/// word of a directive or of a text-manipulation statement — on the BASIC LETTERS ALONE (kb/Work PB4240, PB4312;
/// DESIGN-frontend-grammar §3.13). ISO §8.1.3.2 3) a): "COBOL basic letters appearing elsewhere within the compilation
/// group are treated in a case-insensitive manner"; 4) opens "Extended letters in user-defined words are subject to the
/// following rules", so the Annex C fold of an extended letter (4) b)) governs USER-DEFINED WORDS only. A keyword is
/// basic letters, digits and hyphens, so a word that holds any other character is not that keyword: <c>DİSPLAY</c>
/// (U+0130) and <c>KEY</c> spelled with the Kelvin sign U+212A, which Annex C.2 folds to the basic i and k, are not the
/// keywords DISPLAY and KEY (owner decision 2026-10-31, answering GAP-REVIEW question 2). As user-defined words they DO
/// fold (E.2 14) records the deletion of (0131,0069) as a change affecting existing programs), so such a word, folding to
/// a reserved word, is refused as one: that question is <c>CobolNames</c>', never this class's.
/// <para>The lexer's keyword rules read exactly this (the grammar's <c>caseInsensitive</c> option folds A-Z and a-z and
/// nothing else); every stage that reads a keyword out of text the lexer does not see — the directive line, the directive
/// operands, the conditional-compilation expression, the division cursor, the COPY and REPLACE phrases — and the lexer's
/// one keyword its rules do not spell (the directive expression's DEFINED) asks this class.
/// <c>CobolNameFoldDriftTests</c> holds that no stage compares a keyword by the host's ordinal-ignore-case, which
/// upper-cases the dotless i U+0131 and the long s U+017F to I and S.</para>
/// </summary>
public static class CobolKeywords
{
    /// <summary>Whether <paramref name="written"/> is the keyword <paramref name="keyword"/>: the same length, and each
    /// character the same, a basic letter of either case matching its other case. Any other character matches only
    /// itself, and every keyword is written in basic characters, so no extended letter is ever a keyword's letter. (An
    /// equivalence, so it is also the <see cref="Comparer"/>'s equality.)</summary>
    public static bool Same(ReadOnlySpan<char> written, ReadOnlySpan<char> keyword)
    {
        if (written.Length != keyword.Length) return false;
        for (int i = 0; i < written.Length; i++)
            if (!SameBasic(written[i], keyword[i])) return false;
        return true;
    }

    /// <summary>Whether <paramref name="text"/> begins with the keyword <paramref name="keyword"/>, compared as
    /// <see cref="Same"/>.</summary>
    public static bool StartsWith(ReadOnlySpan<char> text, ReadOnlySpan<char> keyword) =>
        text.Length >= keyword.Length && Same(text[..keyword.Length], keyword);

    /// <summary>Whether <paramref name="text"/> ends with the keyword <paramref name="keyword"/>, compared as
    /// <see cref="Same"/>.</summary>
    public static bool EndsWith(ReadOnlySpan<char> text, ReadOnlySpan<char> keyword) =>
        text.Length >= keyword.Length && Same(text[^keyword.Length..], keyword);

    /// <summary>The word with its basic letters a-z raised to A-Z and every other character as written: the spelling a
    /// keyword is matched in by ordinal (a <c>switch</c> over directive words) and the spelling a diagnostic prints, so
    /// <c>&gt;&gt;İf</c> reads <c>İF</c>, which is no directive word. A word with no lowercase basic letter is returned
    /// itself.</summary>
    public static string Spelling(string word)
    {
        int i = word.AsSpan().IndexOfAnyInRange('a', 'z');
        if (i < 0) return word;
        return string.Create(word.Length, word, static (chars, w) =>
        {
            for (int k = 0; k < w.Length; k++) chars[k] = w[k] is >= 'a' and <= 'z' ? (char)(w[k] & ~0x20) : w[k];
        });
    }

    /// <summary>The comparer of a table keyed by keywords (the directive catalog, a directive's operand words):
    /// <see cref="Same"/>, with a hash of the <see cref="Spelling"/>.</summary>
    public static StringComparer Comparer { get; } = new KeywordComparer();

    private static bool SameBasic(char a, char b) => a == b || ((a | 0x20) == (b | 0x20) && (a | 0x20) is >= 'a' and <= 'z');

    private sealed class KeywordComparer : StringComparer
    {
        public override int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x is null) return -1;
            if (y is null) return 1;
            return string.CompareOrdinal(Spelling(x), Spelling(y));
        }

        public override bool Equals(string? x, string? y) => ReferenceEquals(x, y) || (x is not null && y is not null && Same(x, y));

        public override int GetHashCode(string obj)
        {
            var hash = new HashCode();
            foreach (char c in obj) hash.Add(c is >= 'a' and <= 'z' ? (char)(c & ~0x20) : c);
            return hash.ToHashCode();
        }
    }
}
