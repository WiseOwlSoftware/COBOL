// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Compiler.Binding;

/// <summary>
/// ⛔ A REPEAT-EXPANDED PICTURE CHARACTER-STRING AS ITS RUNS OF IDENTICAL CHARACTERS (kb/Work PB5913). A repetition factor is
/// a count: §2023:13.18.40.3 SR4 (§1985:VI.5.9.3 SR4) bounds character-string-1 AS WRITTEN, so <c>PIC X(67108864)</c> is
/// a legal eleven-character string whose expansion holds 64 M characters. Every question the PICTURE rules ask of the
/// expansion (does it contain a symbol, how many of one, do all of them belong to a set, where is the first or last of
/// one) is a question about which characters occur and in what order, so it is answered over the runs, whose number is
/// bounded by the written string, never by a factor. Asked of the 64 M characters one at a time, the eleven such passes
/// of <c>PictureAnalyzer.Analyze</c> and the composition rules cost a 30-second compile (measured 2026-11-17).
/// <para>Built once from the expansion with a vectorized scan (<see cref="MemoryExtensions.IndexOfAnyExcept{T}(ReadOnlySpan{T}, T)"/>),
/// which is the one pass over the characters; <c>PictureAnalyzer.TryExpandRepeats</c> still builds the string, because
/// the edit mask, the clause identity and the run-time image are per-position by nature and as large as the item.</para>
/// </summary>
internal sealed class PictureSymbolRuns
{
    private readonly (char Symbol, int Count)[] _runs;

    private PictureSymbolRuns((char Symbol, int Count)[] runs, int length) { _runs = runs; Length = length; }

    /// <summary>The runs in order: each a character and how many times it repeats; no two adjacent runs share a
    /// character.</summary>
    public IReadOnlyList<(char Symbol, int Count)> Runs => _runs;

    /// <summary>The number of characters of the expansion.</summary>
    public int Length { get; }

    /// <summary>The runs of <paramref name="expanded"/>, a PICTURE expansion (picture symbols, never a COBOL name).</summary>
    public static PictureSymbolRuns Of(ReadOnlySpan<char> expanded)
    {
        var runs = new List<(char, int)>();
        ReadOnlySpan<char> s = expanded;
        for (int i = 0; i < s.Length;)
        {
            char c = s[i];
            int next = s[i..].IndexOfAnyExcept(c);
            int count = next < 0 ? s.Length - i : next;
            runs.Add((c, count));
            i += count;
        }
        return new PictureSymbolRuns([.. runs], s.Length);
    }

    /// <summary>Whether any character is <paramref name="c"/>.</summary>
    public bool Contains(char c)
    {
        foreach (var (symbol, _) in _runs) if (symbol == c) return true;
        return false;
    }

    /// <summary>How many characters satisfy <paramref name="predicate"/>.</summary>
    public int Count(Func<char, bool> predicate)
    {
        int k = 0;
        foreach (var (symbol, count) in _runs) if (predicate(symbol)) k += count;
        return k;
    }

    /// <summary>Whether any character satisfies <paramref name="predicate"/>.</summary>
    public bool Any(Func<char, bool> predicate)
    {
        foreach (var (symbol, _) in _runs) if (predicate(symbol)) return true;
        return false;
    }

    /// <summary>Whether every character satisfies <paramref name="predicate"/>.</summary>
    public bool All(Func<char, bool> predicate)
    {
        foreach (var (symbol, _) in _runs) if (!predicate(symbol)) return false;
        return true;
    }
}
