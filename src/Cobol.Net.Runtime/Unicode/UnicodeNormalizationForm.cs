// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Unicode;

/// <summary>
/// Which of the two CANONICAL normalization forms a text is put into. Both forms name the same equivalence class —
/// two texts that mean the same sequence of characters have the SAME NFC and the SAME NFD — they differ only in
/// whether a base letter and its marks are written as one code point or as several, and every text has exactly one
/// of each form. (The compatibility forms NFKC/NFKD are deliberately absent: they change the text's meaning —
/// "ﬁ" becomes "fi", a superscript becomes its digit — which no collating or key-building caller here may do.)
/// </summary>
public enum UnicodeNormalizationForm
{
    /// <summary>COMPOSED. Every base letter that has a single-code-point spelling for the marks following it is
    /// written that way: "e" + COMBINING ACUTE ACCENT becomes the one code point "é" (U+00E9). This is the shortest
    /// canonical spelling and the one most text on the web and in Windows file names arrives in.</summary>
    NFC,

    /// <summary>DECOMPOSED. Every precomposed character is replaced by its base and its marks — "é" (U+00E9) becomes
    /// "e" + U+0301, a Hangul syllable becomes its jamo — and the marks that follow a base are put into a fixed
    /// (canonical) order, so two spellings of the same text that differ only in the order of their marks become
    /// identical. This is the form the collation engine works in.</summary>
    NFD,
}
