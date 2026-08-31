// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// A DERIVED verdict is one that follows mechanically from a single owner determination plus the rule TEXT —
/// and this is what stops the derived population drifting back into a spread of hand-adjudicated states.
///
/// <para><b>The case that motivated it.</b> ARITHMETIC IS STANDARD-BINARY is declined (kb/Work PB198). Ten
/// catalog rules are conditioned SOLELY on that mode, so every one of them is unreachable and there is nothing
/// left to adjudicate row by row — yet before this landed they carried <b>four different verdicts</b>:
/// NOT-IMPLEMENTED (3), blank (2), NEEDS-OWNER-DECISION (4) and CONFORMS (1). Four answers to one question.</para>
///
/// <para><b>Why the predicate is data and not code.</b> The selector lives in <c>inventory-schema.json</c> under
/// <c>derived-verdicts</c>, and BOTH the batch generator (Python) and this test read it. Writing the same
/// predicate twice in two languages is exactly the drift kb/Work PB194 records, where a mode SET was spelled one
/// way in two files and the other way in a third.</para>
///
/// <para><b>The selector is subtle, so the test asserts it is still SHARP</b> — a predicate that quietly widened
/// to select everything, or narrowed to select nothing, would keep this test green while meaning nothing.</para>
/// </summary>
public sealed class DerivedVerdictDriftTests
{
    private sealed record Rule(string Id, string Section, string Text);

    private static JsonElement Schema() =>
        JsonDocument.Parse(File.ReadAllText(TestRepo.VersionMatrix("inventory-schema.json"))).RootElement;

    private static List<Rule> Catalog()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(
            TestRepo.At("docs", "rearchitecture", "spec-rule-catalog.json")));
        return [.. doc.RootElement.GetProperty("rules").EnumerateArray()
            .Select(r => new Rule(r.GetProperty("id").GetString()!,
                                  r.TryGetProperty("section", out var s) ? s.GetString() ?? "" : "",
                                  r.TryGetProperty("text", out var t) ? t.GetString() ?? "" : ""))];
    }

    private static Dictionary<string, JsonElement> Inventory()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(TestRepo.VersionMatrix("traceability-inventory.json")));
        var map = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var row in doc.RootElement.EnumerateArray())
            map[row.GetProperty("rule-id").GetString()!] = row.Clone();
        return map;
    }

    /// <summary>Apply one <c>derived-verdicts</c> selector to the catalog — the same predicate the generator
    /// applies. A rule is named EITHER by its text (<c>requires-pattern</c>) OR by its clause
    /// (<c>requires-sections</c>), and is then dropped if it matches any <c>excludes-patterns</c>.
    ///
    /// <para>The clause arm is not decoration: the six rules of §8.8.1.4 never repeat the phrase
    /// "standard-binary" — GR-8.8.1.4.2-1 reads "Any operand of an arithmetic expression that is not already in
    /// SBIDI is converted into SBIDI form" — and a text-only predicate missed every one of them.</para></summary>
    private static (List<string> ids, string verdict) Select(string name)
    {
        var sel = Schema().GetProperty("derived-verdicts").GetProperty(name);
        var require = new Regex(sel.GetProperty("requires-pattern").GetString()!, RegexOptions.IgnoreCase);
        var sections = sel.TryGetProperty("requires-sections", out var s)
            ? s.EnumerateArray().Select(x => x.GetString()!).ToList() : [];
        var excludes = sel.GetProperty("excludes-patterns").EnumerateArray()
            .Select(p => new Regex(p.GetString()!, RegexOptions.IgnoreCase)).ToList();
        var ids = Catalog()
            .Where(r => (require.IsMatch(r.Text) || sections.Any(p => r.Section.StartsWith(p, StringComparison.Ordinal)))
                        && !excludes.Any(x => x.IsMatch(r.Text)))
            .Select(r => r.Id).ToList();
        return (ids, sel.GetProperty("verdict").GetString()!);
    }

    [Fact]
    public void NoStandardBinaryConditionedRow_Diverges()
    {
        var (ids, verdict) = Select("standard-binary-only");
        var inv = Inventory();

        var wrong = new List<string>();
        foreach (string id in ids)
        {
            if (!inv.TryGetValue(id, out var row)) { wrong.Add($"{id}: no inventory row"); continue; }
            string got = row.TryGetProperty("verdict", out var v) ? v.GetString() ?? "" : "";
            if (!string.Equals(got, verdict, StringComparison.Ordinal))
                wrong.Add($"{id}: {(got.Length == 0 ? "(blank)" : got)}");
        }

        Assert.True(wrong.Count == 0,
            $"every rule conditioned SOLELY on the declined ARITHMETIC IS STANDARD-BINARY mode must carry the ONE "
            + $"derived verdict '{verdict}'. Diverging rows:\n  " + string.Join("\n  ", wrong)
            + "\nThe decision is kb/Work PB198; re-derive with the schema's derived-verdicts selector rather than "
            + "adjudicating one row.");
    }

    [Fact]
    public void TheSelector_IsStillSharp_NeitherEverythingNorNothing()
    {
        // A predicate is only evidence about what it RETURNED (feedback_measure_the_selectors_complement), so
        // pin both edges. It selected 16 of 4,311 rows when it landed — 10 by text and 6 by clause; the bounds
        // are generous enough to survive ordinary catalog growth and tight enough that a pattern typo which
        // matched everything, or a regex that silently compiled to a never-match, would fail here.
        var (ids, _) = Select("standard-binary-only");
        Assert.InRange(ids.Count, 8, 45);

        // The CLAUSE arm must still be live: these six never say "standard-binary" in their text, and a
        // text-only predicate (the first two drafts) silently dropped all of them.
        foreach (string id in (string[])["GR-8.8.1.4.2-1", "GR-8.8.1.4.2-2", "GR-8.8.1.4.4-1",
                                         "GR-8.8.1.4.4-2", "GR-8.8.1.4.4-3", "GR-8.8.1.4.4-4"])
            Assert.Contains(id, ids);

        // And it must still be MODE-sense, not word-sense: the FLOAT-BINARY usage rules of §11.9.8 name
        // "standard-binary" and have nothing to do with the arithmetic mode. They were selected by the first
        // draft of this predicate; if they ever come back, the exclusion has lost the usage/mode distinction.
        Assert.DoesNotContain("SR-11.9.8.3-1", ids);
        Assert.DoesNotContain("SR-11.9.8.3-2", ids);
        Assert.DoesNotContain("SR-11.9.8.3-3", ids);

        // Conversely, a rule with BOTH arms keeps its own verdict — it still has reachable content under a mode
        // we support. §15.67.3 r4 caps digits at 35 under standard-binary AND at 34 under standard-decimal.
        Assert.DoesNotContain("AR-15.67.3-4", ids);

        // And the rule whose only "standard decimal" is a USAGE must still be IN — dropping it was the bug in
        // the second draft of this predicate.
        Assert.Contains("AR-15.43.3-3", ids);
    }
}
