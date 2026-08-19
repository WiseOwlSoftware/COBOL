// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolNet.Runtime.Collation;
using CobolNet.Runtime.Unicode;
using Xunit;
using Xunit.Abstractions;

namespace CobolNet.Tests.Unit.Unicode;

/// <summary>
/// The public normalization subsystem (Runtime/Unicode/): NFD from the collation table's own data (host-independent),
/// NFC from the host (with the invariant-globalization fallback), <c>IsNormalized</c>, and <c>CompareNormalized</c>'s
/// agreement with the collation engine. The last test is a CROSS-CHECK of our NFD against .NET's over every code
/// point the derived table calls decomposable — a difference is tolerated only where the host does not know the
/// character decomposes at all (an older Unicode version), and those are counted and reported.
/// </summary>
public sealed class UnicodeNormalizerTests(ITestOutputHelper output)
{
    private const string CombiningAcute = "\U00000301";        // ccc 230
    private const string CombiningCircumflex = "\U00000302";   // ccc 230
    private const string CombiningDotBelow = "\U00000323";     // ccc 220 — sorts BEFORE the two above under NFD
    private const string CombiningRingAbove = "\U0000030A";    // ccc 230
    private const string EAcute = "\U000000E9";                // é   → e + U+0301
    private const string EAcuteCapital = "\U000000C9";         // É
    private const string ECircumflexDotBelow = "\U00001EC7";   // ệ   → e + U+0323 + U+0302
    private const string ECircumflex = "\U000000EA";           // ê
    private const string EDotBelow = "\U00001EB9";             // ẹ
    private const string ACapitalRing = "\U000000C5";          // Å   → A + U+030A
    private const string AngstromSign = "\U0000212B";          // Å   (the SIGN) → A + U+030A as well
    private const string OhmSign = "\U00002126";               // Ω   (the SIGN) → U+03A9
    private const string GreekOmega = "\U000003A9";            // Ω   — its own NFD
    private const string HangulGag = "\U0000AC01";             // 각  → U+1100 U+1161 U+11A8
    private const string HangulGa = "\U0000AC00";              // 가  → U+1100 U+1161
    private const string HangulJamoGag = "\U00001100\U00001161\U000011A8";

    private static string Nfd(string s) => UnicodeNormalizer.Normalize(s, UnicodeNormalizationForm.NFD);
    private static string Nfc(string s) => UnicodeNormalizer.Normalize(s, UnicodeNormalizationForm.NFC);
    private static string Show(string s) => string.Join(" ", s.EnumerateRunes().Select(r => $"U+{r.Value:X4}"));

    /// <summary>The NFC half of a test is inert on a host with no normalizer (InvariantGlobalization) — say so
    /// rather than passing silently on a missing observation.</summary>
    private bool NfcUsable([System.Runtime.CompilerServices.CallerMemberName] string caller = "")
    {
        if (UnicodeNormalizer.IsNfcAvailable) return true;
        output.WriteLine($"{caller}: the host cannot normalize (InvariantGlobalization) — NFC is the identity here, assertions skipped");
        return false;
    }

    // ---- NFD: the table's own decomposition ---------------------------------------------------------------------

    [Fact]
    public void Nfd_DecomposesPrecomposedCharacters()
    {
        Assert.Equal("e" + CombiningAcute, Nfd(EAcute));
        Assert.Equal("A" + CombiningRingAbove, Nfd(ACapitalRing));
        Assert.Equal("A" + CombiningRingAbove, Nfd(AngstromSign));           // the SIGN is canonically equivalent to Å
        Assert.Equal(GreekOmega, Nfd(OhmSign));                              // …and the OHM SIGN to Ω
        Assert.Equal("Cafe" + CombiningAcute + " 1", Nfd("Caf" + EAcute + " 1"));   // in context, the rest untouched

        // A recursive decomposition, reached from all four of its spellings — the canonical closure.
        string nfd = "e" + CombiningDotBelow + CombiningCircumflex;
        Assert.Equal(nfd, Nfd(ECircumflexDotBelow));
        Assert.Equal(nfd, Nfd(ECircumflex + CombiningDotBelow));
        Assert.Equal(nfd, Nfd(EDotBelow + CombiningCircumflex));
        Assert.Equal(nfd, Nfd("e" + CombiningCircumflex + CombiningDotBelow));      // …including the "wrong" mark order
    }

    [Fact]
    public void Nfd_ReordersCombiningMarks()
    {
        // Two marks that do not interact may be typed in either order; NFD puts them in canonical-class order
        // (dot below, ccc 220, before acute, ccc 230) so the two spellings become the SAME code point sequence.
        string canonical = "a" + CombiningDotBelow + CombiningAcute;
        Assert.Equal(canonical, Nfd("a" + CombiningAcute + CombiningDotBelow));
        Assert.Equal(canonical, Nfd(canonical));
        Assert.Equal(Nfd("a" + CombiningAcute + CombiningDotBelow), Nfd("a" + CombiningDotBelow + CombiningAcute));
        // A mark ordering is NOT a text difference: the engine already agrees.
        Assert.Equal(0, CollationEngine.Compare("a" + CombiningAcute + CombiningDotBelow, canonical));
    }

    [Fact]
    public void Nfd_DecomposesHangulSyllables_AndNfcComposesThemBack()
    {
        Assert.Equal(HangulJamoGag, Nfd(HangulGag));
        Assert.Equal("\U00001100\U00001161", Nfd(HangulGa));
        Assert.Equal(0, CollationEngine.Compare(HangulGag, HangulJamoGag));   // one text, two spellings

        if (!NfcUsable()) return;
        Assert.Equal(HangulGag, Nfc(HangulJamoGag));
        Assert.Equal(HangulGag, Nfc(HangulGag));
    }

    [Fact]
    public void Nfd_ReturnsTheInputByReference_WhenItIsAlreadyItsOwnNfd()
    {
        // The fast path: no decomposable code point, no Hangul syllable, no combining mark ⇒ no work, no allocation.
        foreach (string s in new[] { "", "ABC", "12345 HELLO, WORLD.", "\U000000DF", GreekOmega, "\U000065E5\U0000672C" })
            Assert.Same(s, Nfd(s));

        // …and the check is not vacuous: a text that DOES decompose gets a new string.
        Assert.NotSame(EAcute, Nfd(EAcute));
        Assert.NotSame(HangulGag, Nfd(HangulGag));
        // An already-decomposed text holds a combining mark, so the predicate says "look" — the answer is equal
        // content, and the caller may not assume identity.
        Assert.Equal("e" + CombiningAcute, Nfd("e" + CombiningAcute));
    }

    [Fact]
    public void Nfd_IsIdempotent_OverTheCorpus()
    {
        foreach (string s in Corpus())
        {
            string once = Nfd(s);
            Assert.Equal(once, Nfd(once));
            Assert.True(UnicodeNormalizer.IsNormalized(once, UnicodeNormalizationForm.NFD), $"NFD of {Show(s)} is not normalized");
        }
    }

    // ---- NFC: the host's composition ----------------------------------------------------------------------------

    [Fact]
    public void Nfc_ComposesDecomposedCharacters()
    {
        if (!NfcUsable()) return;
        Assert.Equal(EAcute, Nfc("e" + CombiningAcute));
        Assert.Equal(EAcuteCapital, Nfc("E" + CombiningAcute));
        Assert.Equal(ECircumflexDotBelow, Nfc("e" + CombiningDotBelow + CombiningCircumflex));
        Assert.Equal(ECircumflexDotBelow, Nfc(Nfd(ECircumflexDotBelow)));
        Assert.Equal(EAcute, Nfc(EAcute));                                   // already composed — unchanged
        Assert.Equal("Caf" + EAcute, Nfc("Cafe" + CombiningAcute));
        Assert.Equal("ABC", Nfc("ABC"));
    }

    [Fact]
    public void Nfc_IsIdempotent_OverTheCorpus()
    {
        if (!NfcUsable()) return;
        foreach (string s in Corpus())
        {
            string once = Nfc(s);
            Assert.Equal(once, Nfc(once));
        }
    }

    [Fact]
    public void Nfc_OnAHostWithoutANormalizer_IsTheIdentity()
    {
        // The documented fallback. On a normalizing host there is nothing to observe here beyond the flag itself;
        // the assertion below is the CONTRACT that holds either way.
        output.WriteLine($"IsNfcAvailable = {UnicodeNormalizer.IsNfcAvailable}; NFD data version = {UnicodeNormalizer.NfdUnicodeVersion}");
        Assert.Equal(CollationTable.Root.UcaVersion, UnicodeNormalizer.NfdUnicodeVersion);
        if (UnicodeNormalizer.IsNfcAvailable) return;
        foreach (string s in Corpus())
        {
            Assert.Same(s, Nfc(s));
            Assert.True(UnicodeNormalizer.IsNormalized(s, UnicodeNormalizationForm.NFC));
        }
    }

    // ---- IsNormalized -------------------------------------------------------------------------------------------

    [Fact]
    public void IsNormalized_Nfd()
    {
        Assert.True(UnicodeNormalizer.IsNormalized("", UnicodeNormalizationForm.NFD));
        Assert.True(UnicodeNormalizer.IsNormalized("PLAIN ASCII", UnicodeNormalizationForm.NFD));
        Assert.True(UnicodeNormalizer.IsNormalized("e" + CombiningAcute, UnicodeNormalizationForm.NFD));
        Assert.True(UnicodeNormalizer.IsNormalized("a" + CombiningDotBelow + CombiningAcute, UnicodeNormalizationForm.NFD));
        Assert.True(UnicodeNormalizer.IsNormalized(HangulJamoGag, UnicodeNormalizationForm.NFD));

        Assert.False(UnicodeNormalizer.IsNormalized(EAcute, UnicodeNormalizationForm.NFD));
        Assert.False(UnicodeNormalizer.IsNormalized(HangulGag, UnicodeNormalizationForm.NFD));
        Assert.False(UnicodeNormalizer.IsNormalized(AngstromSign, UnicodeNormalizationForm.NFD));
        Assert.False(UnicodeNormalizer.IsNormalized("a" + CombiningAcute + CombiningDotBelow, UnicodeNormalizationForm.NFD));
    }

    [Fact]
    public void IsNormalized_Nfc()
    {
        if (!NfcUsable()) return;
        Assert.True(UnicodeNormalizer.IsNormalized("", UnicodeNormalizationForm.NFC));
        Assert.True(UnicodeNormalizer.IsNormalized("PLAIN ASCII", UnicodeNormalizationForm.NFC));
        Assert.True(UnicodeNormalizer.IsNormalized(EAcute, UnicodeNormalizationForm.NFC));
        Assert.True(UnicodeNormalizer.IsNormalized(HangulGag, UnicodeNormalizationForm.NFC));

        Assert.False(UnicodeNormalizer.IsNormalized("e" + CombiningAcute, UnicodeNormalizationForm.NFC));
        Assert.False(UnicodeNormalizer.IsNormalized(HangulJamoGag, UnicodeNormalizationForm.NFC));
    }

    [Fact]
    public void IsNormalized_AgreesWithNormalize_OverTheCorpus()
    {
        foreach (string s in Corpus())
        {
            Assert.Equal(string.Equals(s, Nfd(s), StringComparison.Ordinal), UnicodeNormalizer.IsNormalized(s, UnicodeNormalizationForm.NFD));
            Assert.Equal(string.Equals(s, Nfc(s), StringComparison.Ordinal), UnicodeNormalizer.IsNormalized(s, UnicodeNormalizationForm.NFC));
        }
    }

    // ---- CompareNormalized --------------------------------------------------------------------------------------

    [Fact]
    public void CompareNormalized_AgreesWithTheEngine_OnACorpus()
    {
        var corpus = Corpus();
        int pairs = 0, equivalentPairs = 0;
        var disagreements = new List<string>();
        for (int i = 0; i < corpus.Length; i++)
            for (int j = 0; j < corpus.Length; j++)
            {
                string a = corpus[i], b = corpus[j];
                int engine = Math.Sign(CollationEngine.Compare(a, b));
                pairs++;
                // Canonically equivalent but differently spelled — the pairs this helper must not disturb.
                if (engine == 0 && !string.Equals(a, b, StringComparison.Ordinal)) equivalentPairs++;
                foreach (var form in new[] { UnicodeNormalizationForm.NFD, UnicodeNormalizationForm.NFC })
                {
                    int ours = Math.Sign(UnicodeNormalizer.CompareNormalized(a, b, form));
                    if (ours != engine)
                        disagreements.Add($"{form}: {Show(a)} vs {Show(b)} — normalized {ours}, engine {engine}");
                }
            }
        output.WriteLine($"CompareNormalized: {pairs} pairs, {equivalentPairs} canonically equivalent but differently spelled");
        Assert.True(equivalentPairs >= 20, $"the corpus must exercise canonical equivalence; it held {equivalentPairs} such pairs");
        Assert.True(disagreements.Count == 0, $"{disagreements.Count} disagreement(s):\n" + string.Join("\n", disagreements.Take(25)));
    }

    [Fact]
    public void CompareNormalized_OrdersAndTreatsNullAsEmpty()
    {
        Assert.True(UnicodeNormalizer.CompareNormalized("a", "b", UnicodeNormalizationForm.NFD) < 0);
        Assert.True(UnicodeNormalizer.CompareNormalized("b", "a", UnicodeNormalizationForm.NFC) > 0);
        Assert.Equal(0, UnicodeNormalizer.CompareNormalized(EAcute, "e" + CombiningAcute, UnicodeNormalizationForm.NFD));
        Assert.Equal(0, UnicodeNormalizer.CompareNormalized(EAcute, "e" + CombiningAcute, UnicodeNormalizationForm.NFC));
        Assert.Equal(0, UnicodeNormalizer.CompareNormalized(null, "", UnicodeNormalizationForm.NFD));
        Assert.Equal(0, UnicodeNormalizer.CompareNormalized(null, null, UnicodeNormalizationForm.NFC));
        Assert.True(UnicodeNormalizer.CompareNormalized(null, "a", UnicodeNormalizationForm.NFD) < 0);
    }

    // ---- argument handling and ill-formed text ------------------------------------------------------------------

    [Fact]
    public void Normalize_RejectsNullAndAnUnknownForm()
    {
        Assert.Throws<ArgumentNullException>(() => UnicodeNormalizer.Normalize(null!, UnicodeNormalizationForm.NFD));
        Assert.Throws<ArgumentNullException>(() => UnicodeNormalizer.IsNormalized(null!, UnicodeNormalizationForm.NFC));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodeNormalizer.Normalize("a", (UnicodeNormalizationForm)7));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodeNormalizer.IsNormalized("a", (UnicodeNormalizationForm)7));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodeNormalizer.CompareNormalized("a", "b", (UnicodeNormalizationForm)7));
    }

    [Fact]
    public void IllFormedText_IsReturnedUnchanged_ByBothForms()
    {
        // An unpaired surrogate: the table's NFD passes it through as itself, and NFC must not throw either —
        // ill-formed text still gets a deterministic answer (Collation/README.md §3).
        string lone = "a" + (char)0xD800 + "b";
        Assert.Equal(lone, Nfd(lone));
        Assert.Equal(lone, Nfc(lone));
        Assert.True(UnicodeNormalizer.IsNormalized(lone, UnicodeNormalizationForm.NFD));
        Assert.True(UnicodeNormalizer.IsNormalized(lone, UnicodeNormalizationForm.NFC));

        string trailing = EAcute + (char)0xDC00;
        Assert.Equal("e" + CombiningAcute + (char)0xDC00, Nfd(trailing));
        Assert.Equal(trailing, Nfc(trailing));
    }

    // ---- the cross-check against the host's normalizer -----------------------------------------------------------

    [Fact]
    public void Nfd_AgreesWithTheHostNormalizer_OnEveryDecomposableCodePoint()
    {
        if (!NfcUsable()) return;   // no host normalizer at all — nothing to cross-check against
        var table = CollationTable.Root;
        int examined = 0, agreed = 0, hostUnaware = 0, hostRefused = 0;
        var disagreements = new List<string>();
        var unaware = new List<int>();
        for (int cp = 0; cp <= 0x10FFFF; cp++)
        {
            if (cp is >= 0xD800 and <= 0xDFFF) continue;                     // surrogates are not characters
            if (!table.TryGetCanonicalDecomposition(cp, out _)) continue;
            examined++;
            string s = char.ConvertFromUtf32(cp);
            string ours = Nfd(s);
            string theirs;
            try { theirs = s.Normalize(NormalizationForm.FormD); }
            catch (ArgumentException) { hostRefused++; continue; }           // unassigned in the host's Unicode version
            if (string.Equals(ours, theirs, StringComparison.Ordinal)) { agreed++; continue; }
            if (string.Equals(theirs, s, StringComparison.Ordinal)) { hostUnaware++; unaware.Add(cp); continue; }  // the host does not decompose it
            disagreements.Add($"U+{cp:X4}: ours [{Show(ours)}], .NET [{Show(theirs)}]");
        }
        output.WriteLine($"NFD cross-check: {examined} decomposable code points — {agreed} identical to .NET, " +
                         $"{hostUnaware} the host does not decompose, {hostRefused} the host refused, {disagreements.Count} disagreements " +
                         $"(table Unicode {UnicodeNormalizer.NfdUnicodeVersion})");
        if (unaware.Count != 0)
            output.WriteLine("    the host does not decompose: " + string.Join(" ", unaware.Take(40).Select(c => $"U+{c:X4}")) +
                             (unaware.Count > 40 ? $" (+{unaware.Count - 40} more)" : ""));
        Assert.True(examined >= 2000, $"the table should report 2,000+ decomposable code points; it reported {examined}");
        Assert.Equal(examined, table.CanonicalDecompositionCount);
        Assert.True(agreed > examined * 3 / 4, $"only {agreed} of {examined} agreed with the host — that is a defect, not a version gap");
        Assert.True(disagreements.Count == 0, $"{disagreements.Count} disagreement(s):\n" + string.Join("\n", disagreements.Take(25)));
    }

    // ---- the corpus ---------------------------------------------------------------------------------------------

    /// <summary>Texts covering every source of canonical variation: precomposition, mark order, recursive
    /// decomposition, singleton decompositions (the Ohm and Angstrom signs), Hangul, and text that must be left
    /// alone (ASCII, ß, a compatibility ligature, ill-formed UTF-16).</summary>
    private static string[] Corpus() =>
    [
        "", "a", "A", "ab", "abc", "b", "PLAIN ASCII", "12345 HELLO, WORLD.",
        EAcute, "e" + CombiningAcute, EAcuteCapital, "E" + CombiningAcute,
        ECircumflexDotBelow, ECircumflex + CombiningDotBelow, EDotBelow + CombiningCircumflex,
        "e" + CombiningDotBelow + CombiningCircumflex, "e" + CombiningCircumflex + CombiningDotBelow,
        "a" + CombiningAcute + CombiningDotBelow, "a" + CombiningDotBelow + CombiningAcute,
        ACapitalRing, AngstromSign, "A" + CombiningRingAbove, OhmSign, GreekOmega,
        HangulGag, HangulJamoGag, HangulGa, "\U00001100\U00001161",
        "caf" + EAcute, "cafe" + CombiningAcute, "cafe", "Caf" + EAcute,
        "r" + EAcute + "sum" + EAcute, "resume", "\U000000DF", "ss", "\U0000FB01", "fi",
        "\U000065E5\U0000672C", "\U0001D400", "a" + ((char)0xD800) + "b",
    ];
}
