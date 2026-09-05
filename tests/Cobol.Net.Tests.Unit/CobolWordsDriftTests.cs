// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using CobolNet.Frontend.Generated;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The PHASE-04 Group-A drift check (parallel to <see cref="ReservedWordsDriftTests"/>): the context-sensitive
/// word set is single-sourced from <c>tests/version-matrix/cobol-words.json</c> and GENERATED into two grammar
/// artifacts by <c>scripts/gen-cobol-words.ps1</c> —
/// <list type="bullet">
///   <item>the parser <c>cobolWord</c> rule (<c>Grammar/Core/CobolWords.g4</c>, the <c>nameSlot=true</c> rows), and</item>
///   <item>the lexer <c>_dataNameTokens</c> subscript-trigger set (<c>Parsing/CobolLexerWordSet.g.cs</c>, the
///   <c>subscriptTrigger=true</c> rows).</item>
/// </list>
/// These tests prove the two generated artifacts cannot silently desync from the JSON (a hand edit to either, or
/// a regen that touched only one, fails here), and cross-check the JSON against <c>reserved-words.json</c>. All
/// checks are set-based (order-independent). Content-filter rule: never print a full word list — mismatches
/// report counts and at most a few offending tokens.
/// </summary>
public sealed class CobolWordsDriftTests
{
    private sealed record WordRow(string Token, bool NameSlot, bool SubscriptTrigger, bool ReservationGated);

    // token -> COBOL word spelling: ANTLR '_' becomes '-'; a trailing '_' is a generator-clash guard (FULL_ = "FULL").
    private static string ToWord(string token) => token.Replace('_', '-').TrimEnd('-');

    private static List<WordRow> LoadJsonWords()
    {
        string path = TestRepo.VersionMatrix("cobol-words.json");
        Assert.True(File.Exists(path), $"canonical json missing: {path} — run scripts/gen-cobol-words.ps1");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.GetProperty("words").EnumerateArray()
            .Select(e => new WordRow(
                e.GetProperty("token").GetString()!,
                e.GetProperty("nameSlot").GetBoolean(),
                e.GetProperty("subscriptTrigger").GetBoolean(),
                e.TryGetProperty("reservationGated", out var g) && g.GetBoolean()))
            .ToList();
    }

    /// <summary>The parser's generated <c>cobolWord</c> alternatives equal the JSON <c>nameSlot=true</c> tokens.</summary>
    [Fact]
    public void CobolWordsG4_CobolWord_Matches_Json_NameSlot()
    {
        var json = LoadJsonWords().Where(w => w.NameSlot).Select(w => w.Token).ToHashSet(StringComparer.Ordinal);
        var g4 = ParseCobolWordAlternatives();

        var onlyJson = json.Where(w => !g4.Contains(w)).Take(5).ToList();
        var onlyG4 = g4.Where(w => !json.Contains(w)).Take(5).ToList();
        Assert.True(onlyJson.Count == 0 && onlyG4.Count == 0,
            $"cobolWord drift: json={json.Count} g4={g4.Count} json-only=[{string.Join(",", onlyJson)}] g4-only=[{string.Join(",", onlyG4)}]");
    }

    /// <summary>⛔ THE BOTH-HALVES PIN (kb/Work PB300). A <c>reservationGated</c> row must appear in the generated
    /// grammar TWICE and with OPPOSITE predicates: in <c>cobolWord</c> under <c>{!reservedHere("W")}?</c> (the
    /// word is a user word exactly where §8.9 leaves it) and in <c>reservedGatedWord</c> under
    /// <c>{reservedHere("W")}?</c> (a DECLARATION of it still parses where §8.9 reserves it, so the funnel's
    /// targeted COBOLNET0901 names the word instead of a raw COBOL0001).
    /// <para>The second half used to be a HAND-WRITTEN list of two words inside <c>CobolData.g4</c>'s
    /// <c>dataName</c>, and it had already rotted: CRT and CURSOR were reservation-gated by kb/Work PB301 and
    /// never added, so <c>01 CRT PIC X.</c> at <c>--std 2002</c> answered "no viable alternative". Generating both
    /// halves from the ONE flag is the structural cure (CLAUDE.md rule 5); THIS test is what keeps "automatic"
    /// true — set-equality in BOTH directions, so neither a JSON row without a rule alternative nor a rule
    /// alternative without a JSON row survives.</para></summary>
    [Fact]
    public void CobolWordsG4_ReservedGatedWord_Matches_Json_ReservationGated()
    {
        var json = LoadJsonWords().Where(w => w.ReservationGated).Select(w => w.Token).ToHashSet(StringComparer.Ordinal);
        Assert.NotEmpty(json);   // dataName references the rule; an empty one is invalid ANTLR

        var g4 = ParseGatedAlternatives("reservedGatedWord", negatedGate: false);
        var onlyJson = json.Where(w => !g4.Contains(w)).Take(5).ToList();
        var onlyG4 = g4.Where(w => !json.Contains(w)).Take(5).ToList();
        Assert.True(onlyJson.Count == 0 && onlyG4.Count == 0,
            $"reservedGatedWord drift: json={json.Count} g4={g4.Count} json-only=[{string.Join(",", onlyJson)}] "
            + $"g4-only=[{string.Join(",", onlyG4)}] — re-run scripts/gen-cobol-words.ps1");

        // The other half, and the reason the two are asserted together: the SAME rows carry the INVERTED gate in
        // cobolWord. A row gated in one rule and ungated (or absent) in the other is the desync this pins.
        var cobolWordGated = ParseGatedAlternatives("cobolWord", negatedGate: true);
        Assert.True(json.SetEquals(cobolWordGated),
            $"cobolWord's !reservedHere gates ({cobolWordGated.Count}) do not equal the reservationGated rows "
            + $"({json.Count}) — the two halves of the gate must name the same words");

        // Every gated word must be a REAL §8.9 word with a per-edition split, or the gate is a no-op in one
        // direction: reserved at every edition ⇒ cobolWord never admits it; reserved at none ⇒ reservedGatedWord
        // never fires. Both would be silent (feedback_a_dead_lookup_is_also_unverified).
        var reserved = LoadReservedIntervals();
        foreach (string t in json.OrderBy(w => w, StringComparer.Ordinal))
        {
            Assert.True(reserved.TryGetValue(ToWord(t), out var flags),
                $"reservationGated word '{t}' has no reserved-words.json row — reservedHere() answers false everywhere");
            Assert.True(flags.Contains(true) && flags.Contains(false),
                $"reservationGated word '{t}' is reserved at {(flags[0] ? "every" : "no")} edition, so one half of "
                + "its gate can never fire — reservation gating only means something across a §8.9 boundary");
        }
    }

    /// <summary>The lexer's runtime <c>_dataNameTokens</c> set equals the JSON <c>subscriptTrigger=true</c> tokens.
    /// This reads the ONE real compiled set (the hand-written HashSet was deleted from CobolLexer.g4 @members).</summary>
    [Fact]
    public void LexerRuntimeSet_Matches_Json_SubscriptTrigger()
    {
        var json = LoadJsonWords().Where(w => w.SubscriptTrigger).Select(w => w.Token).ToHashSet(StringComparer.Ordinal);

        var field = typeof(CobolLexer).GetField("_dataNameTokens", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(field);
        var ints = (HashSet<int>)field!.GetValue(null)!;
        var runtime = ints.Select(t => CobolLexer.DefaultVocabulary.GetSymbolicName(t) ?? $"<{t}>")
                          .ToHashSet(StringComparer.Ordinal);

        var onlyJson = json.Where(w => !runtime.Contains(w)).Take(5).ToList();
        var onlyRuntime = runtime.Where(w => !json.Contains(w)).Take(5).ToList();
        Assert.True(onlyJson.Count == 0 && onlyRuntime.Count == 0,
            $"_dataNameTokens drift: json={json.Count} runtime={runtime.Count} json-only=[{string.Join(",", onlyJson)}] runtime-only=[{string.Join(",", onlyRuntime)}]");
    }

    /// <summary>Reconciliation + reserved-words cross-check. The two documented asymmetries are pinned, and the
    /// SOUND reserved-words linkage (RW-1) holds. See the DESIGN-DEVIATION note in <c>gen-cobol-words.ps1</c> for
    /// why the plan's naive "every trigger word is user-legal at >=1 edition" predicate was rejected as unsound
    /// (COLUMN/LENGTH are §8.9-reserved at all editions yet name-slot-admitted; the six functionName collisions
    /// are reserved keywords).</summary>
    [Fact]
    public void Reconciliation_And_ReservedWords_CrossCheck()
    {
        var words = LoadJsonWords();
        var nameSlot = words.Where(w => w.NameSlot).Select(w => w.Token).ToHashSet(StringComparer.Ordinal);
        var subTrig = words.Where(w => w.SubscriptTrigger).Select(w => w.Token).ToHashSet(StringComparer.Ordinal);

        // Both asymmetry sides are PINNED to their documented membership (FU-1) — symmetric, so a one-sided flip of a
        // currently-SHARED word is drift. Without the subscriptTrigger-only pin, flipping a shared+2023-reserved word
        // (COLUMN/LENGTH/SCREEN) to nameSlot=false would silently drop its cobolWord admission yet pass RW-1 (it stays
        // reserved) — the false-green gap the adversarial review flagged.
        // AS joined BIT at P10 Step 15 BY DESIGN: the §13.10 constant entry's `AS (arith-expr)` must lex its
        // parenthesized expression in NORMAL mode, so AS cannot be a subscript trigger (the FU-1 ledger).
        var nameSlotOnly = nameSlot.Where(w => !subTrig.Contains(w)).OrderBy(w => w, StringComparer.Ordinal).ToList();
        Assert.True(nameSlotOnly.SequenceEqual(new[] { "AS", "BIT" }),
            $"nameSlot-only expected [AS,BIT] but was [{string.Join(",", nameSlotOnly)}] — update the FU-1 ledger if intended");

        var subTrigOnly = subTrig.Where(w => !nameSlot.Contains(w)).OrderBy(w => w, StringComparer.Ordinal).ToList();
        Assert.True(subTrigOnly.SequenceEqual(new[] { "DISPLAY", "MERGE", "RANDOM", "SIGN", "SORT", "SUM" }),
            $"subscriptTrigger-only expected the six functionName collisions but was [{string.Join(",", subTrigOnly)}] — a shared word flipped to nameSlot=false lands here; update the FU-1 ledger if intended");

        // RW-1 (sound reserved-words linkage): each subscriptTrigger-only word is a genuine 2023-reserved keyword.
        var reserved2023 = LoadReserved2023();
        var rwViolations = subTrigOnly.Where(t => !(reserved2023.TryGetValue(ToWord(t), out var r) && r)).Take(5).ToList();
        Assert.True(rwViolations.Count == 0,
            $"RW-1: subscriptTrigger-only word(s) not 2023-reserved keywords: [{string.Join(",", rwViolations)}] — a stray user-word belongs in cobolWord too");
    }

    /// <summary>Structural invariants the generator guarantees (a regression here means the JSON or script broke).</summary>
    [Fact]
    public void Structural_Sanity()
    {
        var words = LoadJsonWords();
        Assert.Equal(words.Count, words.Select(w => w.Token).Distinct(StringComparer.Ordinal).Count());   // unique tokens
        Assert.All(words, w => Assert.Matches("^[A-Z][A-Z0-9_]*$", w.Token));
        Assert.All(words, w => Assert.True(w.NameSlot || w.SubscriptTrigger, $"{w.Token} in neither set"));
        var ident = Assert.Single(words, w => w.Token == "IDENTIFIER");
        Assert.True(ident.NameSlot && ident.SubscriptTrigger, "IDENTIFIER must be nameSlot=true AND subscriptTrigger=true");
    }

    private static HashSet<string> ParseCobolWordAlternatives()
    {
        string path = TestRepo.Src("Cobol.Net.Frontend", "Grammar", "Core", "CobolWords.g4");
        Assert.True(File.Exists(path), $"generated grammar missing: {path} — run scripts/gen-cobol-words.ps1");
        var alts = new HashSet<string>(StringComparer.Ordinal);
        bool inRule = false;
        foreach (var raw in File.ReadAllLines(path))
        {
            string line = raw.Trim();
            if (line == "cobolWord") { inRule = true; continue; }
            if (!inRule) continue;
            if (line == ";") break;
            // One alternative per line: ': X' or '| X', optionally behind a reservation-gate predicate
            // '| {!reservedHere("X")}? X' (kb/Work PB137 — the generator emits it for reservationGated rows).
            var m = Regex.Match(line, @"^[:|]\s*(?:\{[^}]*\}\?\s*)?([A-Z][A-Z0-9_]*)\s*$");
            if (m.Success) alts.Add(m.Groups[1].Value);
        }
        return alts;
    }

    /// <summary>The tokens of one generated rule's GATED alternatives — <c>| {reservedHere("X")}? X</c> when
    /// <paramref name="negatedGate"/> is false, <c>| {!reservedHere("X")}? X</c> when it is true. Ungated
    /// alternatives are ignored, so this reads the gate itself rather than the rule's membership.</summary>
    private static HashSet<string> ParseGatedAlternatives(string ruleName, bool negatedGate)
    {
        string path = TestRepo.Src("Cobol.Net.Frontend", "Grammar", "Core", "CobolWords.g4");
        Assert.True(File.Exists(path), $"generated grammar missing: {path} — run scripts/gen-cobol-words.ps1");
        var pattern = new Regex(@"^[:|]\s*\{(!?)reservedHere\(""([A-Z][A-Z0-9_]*)""\)\}\?\s*([A-Z][A-Z0-9_]*)\s*$");
        var alts = new HashSet<string>(StringComparer.Ordinal);
        bool inRule = false;
        foreach (var raw in File.ReadAllLines(path))
        {
            string line = raw.Trim();
            if (line == ruleName) { inRule = true; continue; }
            if (!inRule) continue;
            if (line == ";") break;
            if (pattern.Match(line) is not { Success: true } m) continue;
            if ((m.Groups[1].Value == "!") != negatedGate) continue;
            Assert.Equal(m.Groups[2].Value, m.Groups[3].Value);   // the gate names the token it guards
            alts.Add(m.Groups[3].Value);
        }
        Assert.True(inRule, $"rule '{ruleName}' not found in {path} — run scripts/gen-cobol-words.ps1");
        return alts;
    }

    /// <summary>word → its four §8.9 reservation flags, in edition order {85, 2002, 2014, 2023}.</summary>
    private static Dictionary<string, bool[]> LoadReservedIntervals()
    {
        string path = TestRepo.VersionMatrix("reserved-words.json");
        Assert.True(File.Exists(path), $"reserved-words.json missing: {path}");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.GetProperty("words").EnumerateArray()
            .ToDictionary(e => e.GetProperty("word").GetString()!,
                          e => new[] { e.GetProperty("r85").GetBoolean(), e.GetProperty("r2002").GetBoolean(),
                                       e.GetProperty("r2014").GetBoolean(), e.GetProperty("r2023").GetBoolean() },
                          StringComparer.Ordinal);
    }

    private static Dictionary<string, bool> LoadReserved2023()
    {
        string path = TestRepo.VersionMatrix("reserved-words.json");
        Assert.True(File.Exists(path), $"reserved-words.json missing: {path}");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.GetProperty("words").EnumerateArray()
            .ToDictionary(e => e.GetProperty("word").GetString()!, e => e.GetProperty("r2023").GetBoolean(),
                          StringComparer.Ordinal);
    }
}
