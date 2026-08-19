// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text;
using BenchmarkDotNet.Attributes;
using CobolNet.Benchmarks.Collation;
using CobolNet.Runtime.Unicode;
using CobolNet.Runtime.Unicode.Segmentation;

namespace CobolNet.Benchmarks.Unicode;

/// <summary>
/// The Unicode subsystems: NORMALIZATION (<c>Runtime/Unicode/UnicodeNormalizer</c> — NFD is the engine's own
/// table-driven decomposition, NFC the host's) and grapheme SEGMENTATION (<c>Runtime/Unicode/Segmentation/</c> —
/// the derived-table UAX #29 breaker), each against the host's implementation of the same job
/// (<see cref="string.Normalize(NormalizationForm)"/>, <see cref="StringInfo"/>) as the baseline: the numbers that
/// justify carrying our own data are "same class as the host, version-stable everywhere".
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class UnicodeBenchmarks
{
    private const string NfdAscii = "NFD-ASCII";
    private const string NfdAccented = "NFD-Accented";
    private const string Nfc = "NFC";
    private const string SegAscii = "Segment-ASCII";
    private const string SegMixed = "Segment-Mixed";
    private const string SegLong = "Segment-Long";
    private const int Ops = 32;

    private static readonly string[] Ascii = Enumerable.Range(0, Ops).Select(i => $"customer record {i:D4} of the ledger").ToArray();
    private static readonly string[] Accented =
    [
        "Résumé", "café", "naïve", "Ångström", "señor", "Zürich", "coöperate", "façade", "élève", "crème brûlée", "Ærø", "øre", "Łódź", "Dvořák", "Ĉu", "Ǆemal",
        "ệ", "Việt Nam", "e\U00000323\U00000302", "A\U0000030A", "각", "각", "가나다", "Ḁ", "ẖ", "ǟ", "ṏ", "ậ", "ữ", "ǻ", "ḉ", "ǚ",
    ];
    private static readonly string[] Mixed =
    [
        "e\U00000301", "\U0001F468‍\U0001F469‍\U0001F467", "\U0001F44D\U0001F3FD", "\U0001F1FA\U0001F1F8\U0001F1EC\U0001F1E7", "각가", "각", "क्त", "क़्त", "कः", "\r\n", "؀١", "a\tb",
        "Zoë", "naïve", "\U0001F600\U0001F601", "한국어", "日本語", "中文", "ابجد", "אבגד", "αβγ", "абв", "e\U00000301\U00000302x", "\U0001F468‍\U0001F469", "\U0001F1EC\U0001F1E7", "ﬁ", "ǆ", "\U0001F926\U0001F3FC‍\U00002642\U0000FE0F", "क्‍त", "๊", "‍", "a‍b",
    ];
    private string _long = "";

    [GlobalSetup]
    public void Setup()
    {
        _long = string.Concat(Enumerable.Repeat("The quick brown fox — résumé, café, naïve — jumps over the lazy dog 각 \U0001F468‍\U0001F469‍\U0001F467 क्त. ", 20));
        _ = UnicodeNormalizer.NfdUnicodeVersion;
        _ = GraphemeBreaker.UnicodeVersion;
        _ = UnicodeNormalizer.IsNfcAvailable;
    }

    // ---- normalization -----------------------------------------------------------------------------------------

    /// <summary>NFD of ASCII text: the fast path (nothing decomposable) returns the input by reference.</summary>
    [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory(NfdAscii)]
    public int Nfd_Ascii()
    {
        int acc = 0;
        for (int i = 0; i < Ascii.Length; i++) acc += UnicodeNormalizer.Normalize(Ascii[i], UnicodeNormalizationForm.NFD).Length;
        return acc;
    }

    /// <summary>The host's NFD of the same ASCII text.</summary>
    [Benchmark(OperationsPerInvoke = Ops, Baseline = true), BenchmarkCategory(NfdAscii)]
    public int Nfd_Ascii_Host()
    {
        int acc = 0;
        for (int i = 0; i < Ascii.Length; i++) acc += Ascii[i].Normalize(NormalizationForm.FormD).Length;
        return acc;
    }

    /// <summary>NFD of accented / composed text: the table-driven decomposition and canonical reordering.</summary>
    [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory(NfdAccented)]
    public int Nfd_Accented()
    {
        int acc = 0;
        for (int i = 0; i < Accented.Length; i++) acc += UnicodeNormalizer.Normalize(Accented[i], UnicodeNormalizationForm.NFD).Length;
        return acc;
    }

    /// <summary>The host's NFD of the same text.</summary>
    [Benchmark(OperationsPerInvoke = Ops, Baseline = true), BenchmarkCategory(NfdAccented)]
    public int Nfd_Accented_Host()
    {
        int acc = 0;
        for (int i = 0; i < Accented.Length; i++) acc += Accented[i].Normalize(NormalizationForm.FormD).Length;
        return acc;
    }

    /// <summary>NFC through the subsystem (the host's composer behind the invariant-mode fallback).</summary>
    [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory(Nfc)]
    public int Nfc_Accented()
    {
        int acc = 0;
        for (int i = 0; i < Accented.Length; i++) acc += UnicodeNormalizer.Normalize(Accented[i], UnicodeNormalizationForm.NFC).Length;
        return acc;
    }

    /// <summary>The host's NFC directly.</summary>
    [Benchmark(OperationsPerInvoke = Ops, Baseline = true), BenchmarkCategory(Nfc)]
    public int Nfc_Accented_Host()
    {
        int acc = 0;
        for (int i = 0; i < Accented.Length; i++) acc += Accented[i].Normalize(NormalizationForm.FormC).Length;
        return acc;
    }

    // ---- segmentation --------------------------------------------------------------------------------------------

    /// <summary>Cluster count of ASCII text: one property lookup per code point, no state.</summary>
    [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory(SegAscii)]
    public int Segment_Ascii()
    {
        int acc = 0;
        for (int i = 0; i < Ascii.Length; i++) acc += GraphemeBreaker.Count(Ascii[i]);
        return acc;
    }

    /// <summary>The host's grapheme count (<see cref="StringInfo.LengthInTextElements"/>) of the same text.</summary>
    [Benchmark(OperationsPerInvoke = Ops, Baseline = true), BenchmarkCategory(SegAscii)]
    public int Segment_Ascii_Host()
    {
        int acc = 0;
        for (int i = 0; i < Ascii.Length; i++) acc += new StringInfo(Ascii[i]).LengthInTextElements;
        return acc;
    }

    /// <summary>Cluster count of mixed text: marks, emoji sequences, flags, Hangul jamo, Indic conjuncts.</summary>
    [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory(SegMixed)]
    public int Segment_Mixed()
    {
        int acc = 0;
        for (int i = 0; i < Mixed.Length; i++) acc += GraphemeBreaker.Count(Mixed[i]);
        return acc;
    }

    /// <summary>The host's grapheme count of the same mixed text.</summary>
    [Benchmark(OperationsPerInvoke = Ops, Baseline = true), BenchmarkCategory(SegMixed)]
    public int Segment_Mixed_Host()
    {
        int acc = 0;
        for (int i = 0; i < Mixed.Length; i++) acc += new StringInfo(Mixed[i]).LengthInTextElements;
        return acc;
    }

    /// <summary>Enumerating the clusters of a ~2 KB mixed text (the allocation-free foreach).</summary>
    [Benchmark, BenchmarkCategory(SegLong)]
    public int Segment_Long_Enumerate()
    {
        int n = 0;
        foreach (var c in GraphemeBreaker.Enumerate(_long)) n += c.Length;
        return n;
    }

    /// <summary>The host's enumerator over the same text.</summary>
    [Benchmark(Baseline = true), BenchmarkCategory(SegLong)]
    public int Segment_Long_Host()
    {
        int n = 0;
        var e = StringInfo.GetTextElementEnumerator(_long);
        while (e.MoveNext()) n += e.GetTextElement().Length;
        return n;
    }
}
