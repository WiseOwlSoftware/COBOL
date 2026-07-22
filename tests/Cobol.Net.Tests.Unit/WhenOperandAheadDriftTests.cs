// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.RegularExpressions;
using CobolNet.Frontend.Generated;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The completeness guard for <see cref="CobolParserCoreBase.WhenOperandStopTokens"/> (the PERFORM Format-3 WHEN
/// operand-list CONTINUATION stop-set; design §1.6). Correctness of the greedy-safe grammar depends on this set
/// containing EVERY token that is BOTH a <c>cobolWord</c> (so a WHEN operand list would annex it) AND the leading
/// token of a dispatcher statement (so annexing it would swallow imperative-statement-2's verb). A future name-slot
/// statement verb added to the grammar without updating the stop-set fails here.
/// </summary>
public sealed class WhenOperandAheadDriftTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "tests", "version-matrix")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return dir!.FullName;
    }

    private static HashSet<string> StopTokenNames() =>
        CobolParserCoreBase.WhenOperandStopTokens
            .Select(t => CobolLexer.DefaultVocabulary.GetSymbolicName(t))
            .ToHashSet(StringComparer.Ordinal);

    private static HashSet<string> NameSlotTokens()
    {
        string path = Path.Combine(RepoRoot(), "tests", "version-matrix", "cobol-words.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.GetProperty("words").EnumerateArray()
            .Where(e => e.GetProperty("nameSlot").GetBoolean())
            .Select(e => e.GetProperty("token").GetString()!)
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>The stop-set is pinned: exactly the 9 current cobolWord∩statement-leader verbs plus the two
    /// anticipatory forward verbs (GET/PARSE). A change to the method's array must update this pin (and the FU
    /// reasoning in the method doc-comment).</summary>
    [Fact]
    public void StopSet_IsPinned()
    {
        var expected = new HashSet<string>(StringComparer.Ordinal)
        { "RESUME", "RAISE", "VALIDATE", "UNLOCK", "SEND", "RECEIVE", "COMMIT", "ROLLBACK", "ENTER", "GET", "PARSE" };
        var actual = StopTokenNames();
        Assert.True(expected.SetEquals(actual),
            $"WhenOperandStopTokens drift: expected [{string.Join(",", expected.OrderBy(x => x))}] "
            + $"got [{string.Join(",", actual.OrderBy(x => x))}]");
    }

    /// <summary>Every stop token is a genuine <c>cobolWord</c> — a non-cobolWord verb needs no entry (the cobolWord
    /// grammar element itself stops the loop at it), so a non-cobolWord in the set is dead weight / a mistake.
    /// (GET/PARSE are cobolWords carried forward for when they become statement verbs.)</summary>
    [Fact]
    public void EveryStopToken_IsACobolWord()
    {
        var nameSlot = NameSlotTokens();
        var strays = StopTokenNames().Where(t => !nameSlot.Contains(t)).ToList();
        Assert.True(strays.Count == 0, $"stop-set contains non-cobolWord token(s): [{string.Join(",", strays)}]");
    }

    /// <summary>COMPLETENESS: every <c>cobolWord</c> that leads a KEYWORD-led dispatcher <c>statement</c> arm is in
    /// the stop-set. Derived from the grammar — the <c>statement</c> rule's arm rule-names, resolved to each rule's
    /// first ALL-CAPS token. KNOWN GAP (documented, masked while the F3 runtime is staged): the one cobolWord-LED
    /// statement, <c>inlineMethodInvocationStatement : dataReference …</c> (2023), has no bare-token leader, so a
    /// bare inline-method-invocation as imperative-statement-2 (`WHEN EC-X Y(args)`) can still be annexed — the
    /// interceptor wave must add an LA(2)==LPAREN guard (or equivalent). This guard covers only the keyword-led
    /// arms; it does NOT cover that case.</summary>
    [Fact]
    public void EveryKeywordLedCobolWordStatementLeader_IsInStopSet()
    {
        string grammarDir = Path.Combine(RepoRoot(), "src", "Cobol.Net.Frontend", "Grammar");
        var g4 = Directory.GetFiles(grammarDir, "*.g4", SearchOption.AllDirectories)
            .Select(File.ReadAllText).Aggregate((a, b) => a + "\n" + b);

        // rule-name -> first ALL-CAPS token of its first alternative (null when the first element is a lowercase
        // rule-ref or a predicate — those aren't single-token leaders).
        var firstToken = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (Match m in Regex.Matches(g4, @"(?m)^([a-z]\w*)\s*\r?\n\s*:\s*(.+)$"))
            firstToken[m.Groups[1].Value] = FirstToken(m.Groups[2].Value);
        foreach (Match m in Regex.Matches(g4, @"(?m)^([a-z]\w*)\s*:\s*(.+)$"))
            firstToken.TryAdd(m.Groups[1].Value, FirstToken(m.Groups[2].Value));

        // The dispatcher: the `statement` rule's arm rule-names (strip '|' and any {..}? predicate + comments).
        var body = Regex.Match(g4, @"(?ms)^statement\s*\r?\n\s*:(.+?)\r?\n\s*;").Groups[1].Value;
        var leaders = new HashSet<string>(StringComparer.Ordinal);
        foreach (var raw in body.Split('|'))
        {
            var line = Regex.Replace(raw, @"//.*", "").Replace("{", " {").Trim();
            var mref = Regex.Match(line, @"([a-z]\w*Statement)\b");   // the arm's rule-ref
            if (mref.Success && firstToken.TryGetValue(mref.Groups[1].Value, out var tok) && tok is not null)
                leaders.Add(tok);
        }

        // Guard against a vacuous pass: if the grammar reformat ever broke the scrape, `leaders` would be empty and
        // this completeness test would silently become a no-op (finding F14).
        Assert.True(leaders.Count > 20, $"statement-leader scrape produced only {leaders.Count} leaders — the "
            + "grammar regex likely broke; the completeness check would pass vacuously");

        var nameSlot = NameSlotTokens();
        var stop = StopTokenNames();
        var missing = leaders.Where(t => nameSlot.Contains(t) && !stop.Contains(t)).ToList();
        Assert.True(missing.Count == 0,
            $"cobolWord statement-leader(s) missing from WhenOperandStopTokens: [{string.Join(",", missing)}] — "
            + "add them or a WHEN operand list will annex imperative-statement-2's verb");
    }

    // The first grammar element of an alternative body, when it is a bare ALL-CAPS token; else null.
    private static string? FirstToken(string body)
    {
        var first = body.TrimStart().Split([' ', '\t', '('], 2)[0].Trim();
        return Regex.IsMatch(first, @"^[A-Z][A-Z0-9_]*$") ? first : null;
    }
}
