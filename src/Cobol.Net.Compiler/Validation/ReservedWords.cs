// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Validation;

/// <summary>
/// One per-edition reserved-word row (VERSION_TEST_MATRIX_DESIGN "Phase-2 implementation plan" P2.4): the
/// four-edition reservation flags for a word, its confidence, and its provenance. Rows come from the generated
/// table (<c>ReservedWords.Table.cs</c>, emitted by <c>scripts/gen-reserved-words.ps1</c> from the in-repo ISO
/// 2023 §8.9 list + VCR row 32 + the GnuCOBOL per-standard 85/2002/2014 word lists — derived facts with
/// provenance); <c>tests/version-matrix/reserved-words.json</c> is the same data in its canonical form and a
/// drift test asserts they agree both directions.
/// </summary>
/// <param name="Word">The word, uppercase.</param>
/// <param name="R85">Reserved in ANSI X3.23-1985.</param>
/// <param name="R2002">Reserved in ISO/IEC 1989:2002.</param>
/// <param name="R2014">Reserved in ISO/IEC 1989:2014.</param>
/// <param name="R2023">Reserved in ISO/IEC 1989:2023 (§8.9).</param>
/// <param name="Confidence">"high" = the row may REJECT; anything lower is present but inert (the conservative
/// policy — a wrong entry must never reject a valid program; VCR scope-limit rule).</param>
/// <param name="Provenance">Where the classification comes from (spec §, annex item, or the documented
/// presumption pending older-standard evidence — the decision-1 provisional policy).</param>
public sealed record ReservedWordEntry(
    string Word, bool R85, bool R2002, bool R2014, bool R2023, string Confidence, string Provenance)
{
    /// <summary>Whether the word is reserved at <paramref name="edition"/> (85/2002/2014/2023).</summary>
    public bool IsReservedAt(int edition) => edition switch
    {
        85 => R85,
        2002 => R2002,
        2014 => R2014,
        _ => R2023,
    };
}

/// <summary>The generated §8.9 word table (the partial half is <c>ReservedWords.Table.cs</c>).</summary>
public static partial class ReservedWords
{
    private static Dictionary<string, ReservedWordEntry>? _byWord;

    /// <summary>Look up <paramref name="upperWord"/> (already uppercase) in the generated table.</summary>
    public static ReservedWordEntry? Find(string upperWord) =>
        (_byWord ??= Entries.ToDictionary(e => e.Word, StringComparer.Ordinal)).GetValueOrDefault(upperWord);
}

/// <summary>
/// The per-compilation-unit EFFECTIVE reserved-word set — the seam the validator consults, never the raw table
/// (roadmap ISO-validation D9): the 2023 COBOL-WORDS directive (ISO Annex E.3.3 item 12) adds/removes/renames
/// words per compilation group, so the generated table is only the DEFAULT layer. Today the default is the only
/// layer; the COBOL-WORDS pass (roadmap Phase 7) composes modified instances over it.
/// </summary>
public sealed class ReservedWordSet
{
    /// <summary>The default set: exactly the generated table.</summary>
    public static ReservedWordSet Default { get; } = new();

    /// <summary>The entry for <paramref name="upperWord"/>, or null when the word is not in the effective set.</summary>
    public ReservedWordEntry? Find(string upperWord) => ReservedWords.Find(upperWord);

    /// <summary>True when <paramref name="upperWord"/> is a HIGH-CONFIDENCE reserved word at
    /// <paramref name="edition"/> — the only case that may emit COBOLNET0901 (the conservative policy).</summary>
    public bool RejectsAt(string upperWord, int edition) =>
        Find(upperWord) is { Confidence: "high" } e && e.IsReservedAt(edition);
}
