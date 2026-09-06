// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE DRIFT GUARD ON <see cref="FileControlKeyRules"/>'S TABLE (kb/Work PB699).
/// <para>The table is the compiler's whole statement of what the FILE CONTROL ENTRY's key clauses require
/// (ISO/IEC 1989:2023 §12.4.5.1 · §12.4.5.2 SR10 · §12.4.5.6.3 · §12.4.5.12.3 · §12.4.5.13.3). CLAUDE.md rule 1
/// names the failure mode it is exposed to: a clause number is INHERITED rather than re-derived, and
/// <c>cite.py --check</c> on the number alone passes because the quoted sentence really is somewhere in the
/// standard. This guard re-derives EVERY row from <c>specs/ISO_COBOL.md</c> on every run — the clause region, and
/// where the row names a printed ordinal, that ordinal's own sentence — so the row cannot carry a number the
/// standard does not agree with, and a transcription repair flows through instead of going stale.</para>
/// <para>It also closes the loop rule 5 asks for: adding the next key syntax rule must be adding a ROW, not
/// writing a new <c>if</c> somewhere else. <see cref="EveryScreenedRuleInTheInventory_IsARowInTheTable"/>
/// asserts the traceability inventory and the table name the same set of rules, so a rule screened here without a
/// row (or a row claimed there without an implementation) is red.</para>
/// </summary>
public sealed class FileControlKeyRuleDriftTests
{
    /// <summary>Compare on words only — punctuation, quoting and line wrapping are typography, not content.
    /// The same normalization <c>scripts/spec/cite.py</c> uses, so the two agree about what "contains" means.</summary>
    private static string Norm(string s) =>
        Regex.Replace(Regex.Replace(s, @"[^\w\s]", " "), @"\s+", " ").Trim().ToLowerInvariant();

    /// <summary>The lines of one clause's OWN region: from its heading to the next heading of any depth — the
    /// region <c>cite.py --check</c> asserts against, which is what makes a wrong clause number fail.</summary>
    private static string[] ClauseRegion(string[] lines, string clause)
    {
        var heading = new Regex(@"^#{2,6}\s+([0-9]+(?:\.[0-9]+)*|[A-Z](?:\.[0-9]+)+)(\s|$)");
        int start = Array.FindIndex(lines, l => heading.Match(l) is { Success: true } m && m.Groups[1].Value == clause);
        Assert.True(start >= 0, $"§{clause} is missing from specs/ISO_COBOL.md — a table row cites a clause the transcription does not have.");
        int end = Array.FindIndex(lines, start + 1, l => heading.IsMatch(l));
        return lines[start..(end < 0 ? lines.Length : end)];
    }

    /// <summary>The printed, numbered rules of a clause region, keyed by their printed number. The transcription
    /// escapes the delimiter (<c>1\)</c>) so Markdown does not eat it as a list; both forms are matched.</summary>
    private static Dictionary<int, string> NumberedRules(string[] region)
    {
        var rules = new Dictionary<int, string>();
        foreach (string l in region)
            if (Regex.Match(l, @"^(\d+)\\?\)\s+(.*)$") is { Success: true } m)
                rules[int.Parse(m.Groups[1].Value)] = m.Groups[2].Value.Trim();
        return rules;
    }

    private static string[] SpecLines() => File.ReadAllLines(TestRepo.Specs("ISO_COBOL.md"));

    // ── The guard itself ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>⛔ THE SCANNER IS PROVEN TO BE ABLE TO FAIL BEFORE ANY ROW IS TRUSTED (a green check that never
    /// looked at anything is not evidence). §12.4.5.13.3 has exactly three printed syntax rules; the region is
    /// bounded by the NEXT heading, so a sentence from the neighbouring clause must NOT be found in it.</summary>
    [Fact]
    public void TheClauseScanner_FindsTheRulesAndRejectsAForeignSentence()
    {
        var lines = SpecLines();
        var rules = NumberedRules(ClauseRegion(lines, "12.4.5.13.3"));
        Assert.Equal(3, rules.Count);
        Assert.Contains("shall not be subject to any OCCURS clauses", rules[1]);

        // §12.4.5.12.3 SR1's sentence names data-name-2 as well; §12.4.5.13.3 SR1 does not. A region that
        // silently swallowed its neighbours would find it here.
        string foreign = Norm("Data-name-1 and data-name-2 shall not be subject to any OCCURS clauses");
        Assert.DoesNotContain(ClauseRegion(lines, "12.4.5.13.3"), l => Norm(l).Contains(foreign));
        // …and it IS in the clause that really states it, so the check is not vacuous.
        Assert.Contains(ClauseRegion(lines, "12.4.5.12.3"), l => Norm(l).Contains(foreign));
    }

    /// <summary>Every row's RULE TEXT is inside the clause the row cites — the mechanical
    /// <c>cite.py --check</c> guarantee, applied to the table itself rather than to a comment about it.</summary>
    [Fact]
    public void EveryRow_QuotesTextThatIsInsideTheClauseItCites()
    {
        var lines = SpecLines();
        Assert.True(FileControlKeyRules.Catalog.Count >= 9,
            $"only {FileControlKeyRules.Catalog.Count} rows — the table lost rules; fix the table, do not lower the floor.");
        foreach (var rule in FileControlKeyRules.Catalog)
        {
            string needle = Norm(rule.RuleText);
            Assert.True(ClauseRegion(lines, rule.Clause).Any(l => Norm(l).Contains(needle)),
                $"§{rule.Clause} does not contain \"{rule.RuleText}\" — the row's clause number is wrong or the text drifted.");
        }
    }

    /// <summary>⛔ THE DIAGRAM HALF of the §12.4.5.1 Format 1 row, which no sentence can carry: the requirement
    /// is that the printed indexed format writes the RECORD KEY clause with NO bracket around it. Asserted on the
    /// RAW transcription line, because the general row check normalizes punctuation away and the diagram is
    /// nothing but punctuation. The FILE STATUS and ALTERNATE RECORD KEY clauses of the same figure are the
    /// contrast: both ARE bracketed, so a figure that had lost its brackets altogether would fail here too.
    /// <para>⚠ If this ever fails, RENDER THE PDF PAGE (<c>scripts/render-spec-page.py</c>) before changing the
    /// rule — the OCR'd diagrams were systematically lossy toward falsely-restrictive syntax (CLAUDE.md rule 1).</para></summary>
    [Fact]
    public void IndexedFormat_PrintsTheRecordKeyClauseUnbracketed()
    {
        var region = ClauseRegion(SpecLines(), "12.4.5.1");
        // The PRIME key clause is the one whose line BEGINS with the clause (nothing to its left but blanks) —
        // the ALTERNATE clause's line begins with the '│' of its own optional bracket, which is the contrast.
        Assert.Single(region, l => l.TrimStart().StartsWith("<u>RECORD</u> KEY IS", StringComparison.Ordinal));

        // The contrast, in the same figure: an OPTIONAL clause is written inside a bracket.
        Assert.Contains(region, l => l.TrimStart().StartsWith("[ FILE <u>STATUS</u> IS", StringComparison.Ordinal));
        Assert.Contains(region, l => l.Contains("<u>ALTERNATE</u> <u>RECORD</u> KEY IS", StringComparison.Ordinal)
                                     && l.TrimStart().StartsWith('│'));
    }

    /// <summary>Where a row names a printed ORDINAL (its inventory rule-id carries one), the text it quotes is
    /// that rule's own sentence — not merely somewhere in the clause. This is the half a clause-only check misses:
    /// "a real clause can answer a different question".</summary>
    [Fact]
    public void EveryRow_QuotesTheOrdinalItNames()
    {
        var lines = SpecLines();
        int checkedRows = 0;
        foreach (var rule in FileControlKeyRules.Catalog)
        {
            if (rule.RuleId is null) continue;
            var m = Regex.Match(rule.RuleId, @"^SR-(?<clause>[0-9.]+)-(?<n>\d+)$");
            Assert.True(m.Success, $"rule-id '{rule.RuleId}' is not the inventory's SR-<clause>-<n> shape.");
            Assert.Equal(rule.Clause, m.Groups["clause"].Value);
            var numbered = NumberedRules(ClauseRegion(lines, rule.Clause));
            int n = int.Parse(m.Groups["n"].Value);
            Assert.True(numbered.ContainsKey(n), $"§{rule.Clause} has no printed syntax rule {n}.");
            Assert.Contains(Norm(rule.RuleText), Norm(numbered[n]));
            checkedRows++;
        }
        Assert.True(checkedRows >= 8, $"only {checkedRows} rows carry an ordinal — the guard is measuring almost nothing.");
    }

    /// <summary>The message a row ships names the row's OWN citation. A row whose sentence and whose printed §
    /// disagree is exactly the defect this file exists for, and the message is what a user reads.</summary>
    [Fact]
    public void EveryRow_ShipsAMessageThatNamesItsOwnCitation()
    {
        var file = new FileModel { CobolName = "F", SelectName = "F", AssignTarget = "F" };
        foreach (var rule in FileControlKeyRules.Catalog)
        {
            var op = new FileKeyOperand(rule.Role, "KEY", "K", null, default);
            string message = rule.Message(file, op);
            Assert.Contains(rule.Citation, message);
        }
    }

    /// <summary>A row's ORGANIZATION and its ROLE agree with the §12.4.5.1 format that carries the clause: a
    /// prime or alternate RECORD KEY belongs to Format 1 (indexed), a RELATIVE KEY to Format 2 (relative). This
    /// is the invariant that makes the organization column load-bearing rather than decorative.</summary>
    [Fact]
    public void EveryRow_PairsItsRoleWithTheOrganizationWhoseFormatCarriesTheClause()
    {
        foreach (var rule in FileControlKeyRules.Catalog)
            Assert.Equal(
                rule.Role is FileKeyRole.RelativeKey ? FileOrganization.Relative : FileOrganization.Indexed,
                rule.Organization);
    }

    /// <summary>⭐ THE "NEXT RULE IS A ROW" CLAMP, in both directions.
    /// <list type="bullet">
    /// <item>A rule the inventory says is screened HERE must have a row — otherwise it is enforced somewhere else,
    /// which is the shape PB699 was.</item>
    /// <item>A row's rule-id must be a REAL inventory rule-id, and if that row has already been credited to a
    /// code-location, that location must be this file — a row claiming an id the inventory files against a
    /// different site is a claim on the burn-down the code does not back.</item>
    /// </list>
    /// A table row whose inventory row carries NO code-location yet is legal and deliberate: a rule may be
    /// screened before it has earned a verdict (the two SR2s are screened only in their within-a-record arm, so
    /// neither CONFORMS nor a non-resolving promise would be true of them — see the class's "NOT HERE"
    /// paragraph). Demanding a verdict here would push the register into recording one it has not earned.</summary>
    [Fact]
    public void EveryScreenedRuleInTheInventory_IsARowInTheTable()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(
            TestRepo.At("tests", "version-matrix", "traceability-inventory.json")));
        var claimed = new SortedSet<string>(StringComparer.Ordinal);       // credited to THIS file
        var locatedElsewhere = new Dictionary<string, string>(StringComparer.Ordinal);
        var allIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in doc.RootElement.EnumerateArray())              // a top-level array of rows
        {
            string id = row.GetProperty("rule-id").GetString()!;
            allIds.Add(id);
            string where = row.TryGetProperty("code-location", out var cl) ? cl.GetString() ?? "" : "";
            if (where.Contains("FileControlKeyRules", StringComparison.Ordinal)) claimed.Add(id);
            else if (where.Length > 0) locatedElsewhere[id] = where;
        }
        var inTable = new SortedSet<string>(
            FileControlKeyRules.Catalog.Where(r => r.RuleId is not null).Select(r => r.RuleId!), StringComparer.Ordinal);

        Assert.True(claimed.Count > 0,
            "no inventory row points at FileControlKeyRules — either the batch was never applied or the screen moved.");
        Assert.Empty(claimed.Except(inTable));                             // screened here per the register, no row
        foreach (string id in inTable)
        {
            Assert.True(allIds.Contains(id), $"row '{id}' is not a rule-id the traceability inventory knows.");
            Assert.False(locatedElsewhere.TryGetValue(id, out string? where),
                $"row '{id}' is screened here, but the inventory credits it to '{where}'.");
        }
    }
}
