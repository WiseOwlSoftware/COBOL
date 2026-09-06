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
    private sealed record WordRow(string Token, bool NameSlot, bool SubscriptTrigger);

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
                e.GetProperty("subscriptTrigger").GetBoolean()))
            .ToList();
    }

    /// <summary>⛔ THE DERIVED RESERVATION GATE (kb/Work PB693). The set of <c>cobolWord</c> alternatives that
    /// MUST carry <c>{userWordHere("W")}?</c>, computed here the way <c>gen-cobol-words.ps1</c> step 4b computes
    /// it: a name-slot word that ISO §8.9 reserves at ANY edition. §8.3.2.1 rule 1 — "Reserved words shall not be
    /// used as user-defined words or system-names" — and <c>cobolWord</c> IS the user-defined-word slot, so an
    /// ungated admission lets an operand list absorb the word at the editions that reserve it.
    /// <para>Recomputed from <c>reserved-words.json</c> rather than read off a flag ON PURPOSE: the gate used to
    /// be a hand-set <c>reservationGated</c> row property and fifty-one §8.9-straddling words never got one, so
    /// this test would have been asserting the mistake against itself.</para></summary>
    private static HashSet<string> DerivedGateSet()
    {
        var reserved = LoadReservedIntervals();
        var functionNames = FunctionNameTokens();
        return LoadJsonWords()
            .Where(w => w.NameSlot && w.Token != "IDENTIFIER" && !functionNames.Contains(w.Token))
            .Where(w => reserved.TryGetValue(ToWord(w.Token), out var f) && f.Contains(true))
            .Select(w => w.Token)
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>The single-token alternatives of the grammar's <c>functionName</c> rule — the §15 intrinsic
    /// function names that collide with a reserved word. They are the gate's ONE exclusion (kb/Work PB693): a
    /// <c>cobolWord</c> occurrence of one is the KEYWORD-OMITTED function reference §15 permits
    /// (<c>COMPUTE N = LENGTH(A)</c>), a use OF the reserved word rather than a user-defined-word use, and gating
    /// them turned five conforming 2023 goldens into COBOL0001. Read from the grammar, not listed here, so the
    /// exclusion cannot drift from the rule it is about.</summary>
    private static HashSet<string> FunctionNameTokens()
    {
        string path = TestRepo.Src("Cobol.Net.Frontend", "Grammar", "Core", "CobolExpressions.g4");
        Assert.True(File.Exists(path), $"grammar missing: {path}");
        var alts = new HashSet<string>(StringComparer.Ordinal);
        bool inRule = false;
        foreach (var raw in File.ReadAllLines(path))
        {
            string line = raw.Trim();
            if (line == "functionName") { inRule = true; continue; }
            if (!inRule) continue;
            if (line == ";") break;
            if (Regex.Match(line, @"^[:|]\s*([A-Z][A-Z0-9_]*)\s*$") is { Success: true } m) alts.Add(m.Groups[1].Value);
        }
        Assert.True(alts.Count >= 2, $"the functionName rule yielded {alts.Count} tokens — the parse broke");
        return alts;
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

    /// <summary>⛔ THE §8.3.2.1 INVARIANT (kb/Work PB693) — every name-slot word that ISO §8.9 reserves at ANY
    /// edition carries the reservation gate. "Reserved words shall not be used as user-defined words or
    /// system-names" (§8.3.2.1 rule 1) and <c>cobolWord</c> IS the user-defined-word slot, so an UNGATED
    /// admission of a reserved word lets an operand list absorb it: <c>MOVE "ZZ" TO FS</c> followed by a
    /// period-less <c>UNLOCK F1</c> parsed as a three-receiver MOVE and legal COBOL-2002 source was rejected.
    /// Fifty-one further words had the same §8.9 straddle and the same ungated admission.
    /// <para>The gate was a hand-set <c>reservationGated</c> JSON flag; it is now DERIVED by
    /// <c>gen-cobol-words.ps1</c> step 4b from <c>reserved-words.json</c> (CLAUDE.md rule 5 — never a
    /// hand-maintained list where a structure belongs). This test recomputes the derivation independently and
    /// pins BOTH halves of the emitted gate: <c>cobolWord</c> under <c>{userWordHere("W")}?</c> (a user word
    /// exactly where §8.9 leaves it free) and <c>reservedGatedWord</c> under <c>{!userWordHere("W")}?</c> (a
    /// DECLARATION still parses where §8.9 reserves it, so the funnel's targeted COBOLNET0901 names the word
    /// instead of a raw COBOL0001). Set-equality in both directions, so neither a missing gate nor a stray one
    /// survives.</para></summary>
    [Fact]
    public void CobolWordsG4_ReservationGate_Is_Derived_From_Section89()
    {
        var expected = DerivedGateSet();
        Assert.NotEmpty(expected);   // dataName references reservedGatedWord; an empty rule is invalid ANTLR

        var gatedInCobolWord = ParseGatedAlternatives("cobolWord", negatedGate: false);
        var missing = expected.Where(w => !gatedInCobolWord.Contains(w)).OrderBy(w => w, StringComparer.Ordinal).ToList();
        var stray = gatedInCobolWord.Where(w => !expected.Contains(w)).OrderBy(w => w, StringComparer.Ordinal).ToList();
        Assert.True(missing.Count == 0 && stray.Count == 0,
            $"cobolWord reservation-gate drift: §8.9 requires {expected.Count} gated alternatives, the grammar has "
            + $"{gatedInCobolWord.Count}. UNGATED but §8.9-reserved ({missing.Count}): "
            + $"[{string.Join(",", missing.Take(8))}{(missing.Count > 8 ? ",…" : "")}] — each one lets an operand "
            + $"list absorb a reserved word (kb/Work PB693). Gated but never reserved ({stray.Count}): "
            + $"[{string.Join(",", stray.Take(8))}{(stray.Count > 8 ? ",…" : "")}]. Re-run scripts/gen-cobol-words.ps1.");

        // The other half, and the reason the two are asserted together: the SAME words carry the INVERTED gate in
        // reservedGatedWord. A word gated in one rule and absent from the other is the desync this pins — it is
        // exactly what happened when the declaration half was a hand-written list (CRT/CURSOR, kb/Work PB300).
        var gatedInDeclarationRule = ParseGatedAlternatives("reservedGatedWord", negatedGate: true);
        Assert.True(expected.SetEquals(gatedInDeclarationRule),
            $"reservedGatedWord ({gatedInDeclarationRule.Count}) does not equal the derived gate set "
            + $"({expected.Count}) — the two halves of the gate must name the same words");

        // Confidence alignment (the generator's RW-3 throw, asserted from the other side): the grammar gate keys
        // on userWordHere() = !IsReservedAt (outside the migration mode), which is confidence-blind, while the §8.9 funnel only REPORTS
        // high-confidence rows. A gated lower-confidence word would be rejected with a bare parse error and no
        // COBOLNET0901 to explain it.
        var confidence = LoadConfidence();
        var lowConfidence = expected.Where(t => !(confidence.TryGetValue(ToWord(t), out var c) && c == "high"))
                                    .OrderBy(w => w, StringComparer.Ordinal).Take(5).ToList();
        Assert.True(lowConfidence.Count == 0,
            $"gated word(s) not high-confidence: [{string.Join(",", lowConfidence)}] — the gate would reject the "
            + "declaration with no COBOLNET0901 (the funnel reports high-confidence rows only)");
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
            // '| {userWordHere("X")}? X' (kb/Work PB137/PB693 — the generator emits it for every gated word).
            var m = Regex.Match(line, @"^[:|]\s*(?:\{[^}]*\}\?\s*)?([A-Z][A-Z0-9_]*)\s*$");
            if (m.Success) alts.Add(m.Groups[1].Value);
        }
        return alts;
    }

    /// <summary>The tokens of one generated rule's GATED alternatives — <c>| {userWordHere("X")}? X</c> when
    /// <paramref name="negatedGate"/> is false, <c>| {!userWordHere("X")}? X</c> when it is true. Ungated
    /// alternatives are ignored, so this reads the gate itself rather than the rule's membership. Note the
    /// POLARITY: <c>cobolWord</c> carries the PLAIN predicate (admit the word where it is a user word) and
    /// <c>reservedGatedWord</c> the NEGATED one, so <paramref name="negatedGate"/> is true for the declaration
    /// half — the mirror image of the retired <c>reservedHere</c> spelling (kb/Work PB693).</summary>
    private static HashSet<string> ParseGatedAlternatives(string ruleName, bool negatedGate)
    {
        string path = TestRepo.Src("Cobol.Net.Frontend", "Grammar", "Core", "CobolWords.g4");
        Assert.True(File.Exists(path), $"generated grammar missing: {path} — run scripts/gen-cobol-words.ps1");
        var pattern = new Regex(@"^[:|]\s*\{(!?)userWordHere\(""([A-Z][A-Z0-9_]*)""\)\}\?\s*([A-Z][A-Z0-9_]*)\s*$");
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

    /// <summary>word → its reserved-words.json confidence band ("high" / "medium").</summary>
    private static Dictionary<string, string> LoadConfidence()
    {
        string path = TestRepo.VersionMatrix("reserved-words.json");
        Assert.True(File.Exists(path), $"reserved-words.json missing: {path}");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.GetProperty("words").EnumerateArray()
            .ToDictionary(e => e.GetProperty("word").GetString()!,
                          e => e.GetProperty("confidence").GetString()!, StringComparer.Ordinal);
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
