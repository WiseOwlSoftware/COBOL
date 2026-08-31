// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE INVARIANT THAT KEEPS THE BURN-DOWN AND THE WORK REGISTER THE SAME LIST: every P14 inventory row
/// carrying a DEFECTIVE verdict is claimed by a LIVE note in <c>kb/Work/</c>.
/// </summary>
/// <remarks>
/// <para>
/// The two artifacts answer the same question from opposite ends. <c>tests/version-matrix/traceability-inventory.json</c>
/// says which spec rules this compiler does not yet implement correctly; <c>kb/Work/</c> — the ONE work register
/// (CLAUDE.md rule 8) — says what is left to do. Nothing held them together, and the drift was measured on
/// 2026-08-31: of 138 rows verdicted PARTIAL / NOT-IMPLEMENTED / DIVERGES, <b>131 were invisible to
/// <c>work.py next</c></b>. A defect the register cannot see cannot be ranked, cannot be scheduled, and reads to
/// every later session as work nobody owes — while the number the owner watches counted it.
/// </para>
/// <para>
/// The link is a note's <c>inventory_rows</c> frontmatter list, and it is deliberately the NOTE→ROW direction.
/// The reverse (scraping a note id out of a row's prose <c>notes</c> field) is how the 131 came to be measured,
/// and it is the wrong direction twice over: the row's notes are forensic narrative, so a note id can appear in
/// one for reasons that are not ownership; and a note that lands keeps naming its rows, so a landed note whose
/// rows were never re-verdicted turns this gate RED — which is the event most worth failing on, and the one a
/// prose scrape cannot see.
/// </para>
/// <para>
/// ⚠ NOTHING HERE CARRIES ITS OWN COPY OF THE VOCABULARY. "Defective" is DERIVED from
/// <c>tests/version-matrix/inventory-schema.json</c> — a verdict whose <c>resolves</c> flag is false — so adding a
/// verdict to the schema extends this gate with no edit here, which is the one-rule-one-place discipline the
/// schema itself is built on (<c>feedback_one_rule_one_place</c>). The status vocabulary is the mirror image:
/// only <c>landed</c> and <c>retired</c> are TERMINAL, and every other status — including one added to
/// <c>work.py</c> tomorrow — counts as live. That default is chosen to fail SAFE: an unrecognized status keeps
/// a note holding its rows rather than silently dropping them out of coverage.
/// </para>
/// <para>
/// ⛔ <see cref="TheseChecks_ActuallyFail_OnAFabricatedRegister"/> is not a formality
/// (<c>feedback_green_gates_arent_evidence</c>): this gate will spend most of its life green, and a green gate
/// that never looked at anything is indistinguishable from one that works. It drives the same pure functions
/// with a register built to break them, and — the half that is easy to omit — with a register built to PASS
/// them, so the failures prove discrimination rather than blanket rejection.
/// </para>
/// </remarks>
public sealed class DefectiveRowCoverageDriftTests
{
    /// <summary>A <c>kb/Work/</c> note, reduced to the three facts this gate needs.</summary>
    private sealed record WorkNote(string File, string Id, string Status, string[] InventoryRows)
    {
        /// <summary>Terminal statuses — the only two that stop a note from holding its rows.</summary>
        public bool IsLive => Status is not ("landed" or "retired");
    }

    private sealed record InventoryRow(string RuleId, string Verdict);

    // ── the artifacts under test ─────────────────────────────────────────────────────────────────────

    /// <summary>The verdicts the schema marks as NOT resolving — the ones that leave a rule outstanding.</summary>
    private static HashSet<string> LoadDefectiveVerdicts()
    {
        string path = TestRepo.VersionMatrix("inventory-schema.json");
        Assert.True(File.Exists(path), $"inventory schema missing: {path}");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var defective = doc.RootElement.GetProperty("verdicts").EnumerateObject()
            .Where(p => !p.Value.GetProperty("resolves").GetBoolean())
            .Select(p => p.Name)
            .ToHashSet(StringComparer.Ordinal);
        Assert.True(defective.Count > 0,
            "inventory-schema.json defines no non-resolving verdict — this gate would then be vacuous, so the "
            + "schema, not the gate, is what changed.");
        return defective;
    }

    private static List<InventoryRow> LoadInventory()
    {
        string path = TestRepo.VersionMatrix("traceability-inventory.json");
        Assert.True(File.Exists(path),
            $"inventory missing: {path} — run python scripts/spec/build_inventory.py");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return [.. doc.RootElement.EnumerateArray().Select(e => new InventoryRow(
            e.GetProperty("rule-id").GetString()!,
            e.TryGetProperty("verdict", out var v) ? v.GetString() ?? "" : ""))];
    }

    /// <summary>The frontmatter block: everything between the opening <c>---</c> line and the closing one.</summary>
    private static readonly Regex FrontMatter =
        new(@"\A---\r?\n(?<body>.*?)\r?\n---\r?\n", RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex Scalar =
        new(@"^(?<key>[a-z_]+):[ \t]*(?<value>.*?)[ \t]*$", RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>
    /// Parse one note's frontmatter. Deliberately NOT a YAML parser: the register's frontmatter is a flat block
    /// of <c>key: scalar</c> and <c>key: [a, b]</c> lines that <c>scripts/spec/work.py</c> reads with the same
    /// shape, and pulling a YAML dependency in to read three fields would put the two readers on different
    /// grammars — the drift this gate exists to prevent, reproduced in the gate.
    /// </summary>
    private static WorkNote? ParseNote(string file, string text)
    {
        var fm = FrontMatter.Match(text);
        if (!fm.Success) return null;

        string id = "", status = "";
        string[] rows = [];
        foreach (Match m in Scalar.Matches(fm.Groups["body"].Value))
        {
            string value = m.Groups["value"].Value.Trim();
            switch (m.Groups["key"].Value)
            {
                case "id": id = value.Trim('"'); break;
                case "status": status = value.Trim('"'); break;
                case "inventory_rows":
                    rows = value.StartsWith('[') && value.EndsWith(']')
                        ? [.. value[1..^1].Split(',', StringSplitOptions.TrimEntries
                                                     | StringSplitOptions.RemoveEmptyEntries)
                                          .Select(x => x.Trim('"'))]
                        : [];
                    break;
            }
        }

        return id.Length == 0 ? null : new WorkNote(file, id, status, rows);
    }

    private static List<WorkNote> LoadRegister()
    {
        string dir = TestRepo.Kb("Work");
        Assert.True(Directory.Exists(dir),
            $"the work register is missing: {dir} — it is THE work register (CLAUDE.md rule 8), and a gate that "
            + "cannot find it must fail rather than pass vacuously.");
        var notes = new List<WorkNote>();
        foreach (string path in Directory.EnumerateFiles(dir, "*.md"))
        {
            if (ParseNote(Path.GetFileName(path), File.ReadAllText(path)) is { } n) notes.Add(n);
        }
        return notes;
    }

    // ── the checks, as pure functions so the self-test can drive the SAME code ───────────────────────

    /// <summary>Defective rows that no LIVE note claims — the set this gate holds at empty.</summary>
    private static List<string> UnclaimedDefectiveRows(
        IEnumerable<InventoryRow> rows, IEnumerable<WorkNote> register, HashSet<string> defective)
    {
        var claimed = register.Where(n => n.IsLive)
                              .SelectMany(n => n.InventoryRows)
                              .ToHashSet(StringComparer.Ordinal);
        return [.. from r in rows
                   where defective.Contains(r.Verdict) && !claimed.Contains(r.RuleId)
                   orderby r.RuleId
                   select $"{r.RuleId}: verdict {r.Verdict}, and no open kb/Work note names it in "
                          + "`inventory_rows` — register it (a note), or record the verdict it has earned"];
    }

    /// <summary>
    /// A claim on a rule-id the inventory does not have. A typo here is worse than useless: it satisfies nothing
    /// while LOOKING like ownership, so the row it meant to claim stays uncovered and the note reads as done.
    /// </summary>
    private static List<string> ClaimsNamingNoRow(IEnumerable<InventoryRow> rows, IEnumerable<WorkNote> register)
    {
        var known = rows.Select(r => r.RuleId).ToHashSet(StringComparer.Ordinal);
        return [.. from n in register
                   from claim in n.InventoryRows
                   where !known.Contains(claim)
                   orderby n.Id, claim
                   select $"{n.File}: inventory_rows names '{claim}', which is not a rule-id in the inventory"];
    }

    private static string Report(string what, List<string> bad, int scale) =>
        $"{bad.Count} {what} (of {scale} examined):\n  " + string.Join("\n  ", bad.Take(25))
        + (bad.Count > 25 ? $"\n  … and {bad.Count - 25} more" : "");

    // ── the gate ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// ⛔ THE INVARIANT. Every defective-verdict row is claimed by a live work note.
    /// </summary>
    /// <remarks>
    /// Two ways to go green, and both are correct outcomes: register the row on a note (it is real work), or
    /// record the verdict the rule has actually earned (CONFORMS with its spec-derived witness, or
    /// DOCUMENTED-NON-SUPPORT against a recorded owner decision / <c>docs/CONFORMANCE.md</c> §4 licence). What
    /// is no longer available is the third option that produced the 131 — leaving a row defective and writing
    /// the reason in a paragraph.
    /// </remarks>
    [Fact]
    public void EveryDefectiveRow_IsClaimedByALiveWorkNote()
    {
        var rows = LoadInventory();
        var register = LoadRegister();
        var defective = LoadDefectiveVerdicts();

        // ⛔ POPULATION FIRST. A MISSING observation is not a NEGATIVE one (feedback_verdict_evidence_invariant):
        // an inventory that failed to parse, or a register directory that came back empty, would satisfy the
        // assertion below while measuring nothing at all.
        Assert.True(rows.Count > 1000, $"only {rows.Count} inventory rows parsed — the inventory did not load");
        Assert.True(register.Count > 100, $"only {register.Count} work notes parsed — the register did not load");
        Assert.True(register.Any(n => n.InventoryRows.Length > 0),
            "no work note carries an `inventory_rows` claim — the frontmatter key this gate reads is gone, and "
            + "the gate would pass by measuring nothing.");
        Assert.Contains(rows, r => defective.Contains(r.Verdict));

        var bad = UnclaimedDefectiveRows(rows, register, defective);
        Assert.True(bad.Count == 0, Report("defective row(s) with no live work note", bad, rows.Count));
    }

    /// <summary>Every <c>inventory_rows</c> claim names a rule-id the inventory really has.</summary>
    [Fact]
    public void EveryInventoryRowsClaim_NamesARealRule()
    {
        var rows = LoadInventory();
        var register = LoadRegister();
        var bad = ClaimsNamingNoRow(rows, register);
        Assert.True(bad.Count == 0, Report("claim(s) naming no inventory rule", bad, register.Count));
    }

    /// <summary>
    /// ⛔ THE EVIDENCE THAT THIS GATE INSPECTS ANYTHING — a register built to break each check, and one built
    /// to pass it.
    /// </summary>
    [Fact]
    public void TheseChecks_ActuallyFail_OnAFabricatedRegister()
    {
        var defective = new HashSet<string>(StringComparer.Ordinal) { "PARTIAL", "NOT-IMPLEMENTED", "DIVERGES" };
        InventoryRow Row(string id, string verdict) => new(id, verdict);
        WorkNote Note(string id, string status, params string[] rows) => new($"{id}.md", id, status, rows);

        var inventory = new[]
        {
            Row("GR-1.1-1", "PARTIAL"), Row("GR-1.1-2", "CONFORMS"), Row("GR-1.1-3", "DIVERGES"),
        };

        // A defective row nothing claims.
        Assert.Equal(2, UnclaimedDefectiveRows(inventory, [], defective).Count);

        // Claimed by a note that has LANDED — the exact event worth failing on: the fix shipped and the row was
        // never re-verdicted, so the inventory still publishes a defect the compiler no longer has.
        Assert.Single(UnclaimedDefectiveRows(
            inventory, [Note("PB1", "landed", "GR-1.1-1"), Note("PB2", "open", "GR-1.1-3")], defective));
        Assert.Single(UnclaimedDefectiveRows(
            inventory, [Note("PB1", "retired", "GR-1.1-1"), Note("PB2", "open", "GR-1.1-3")], defective));

        // Claimed by live notes of every non-terminal status — all of these are real work, and all count.
        foreach (string live in new[] { "open", "half", "owner", "blocked" })
        {
            Assert.Empty(UnclaimedDefectiveRows(
                inventory, [Note("PB1", live, "GR-1.1-1"), Note("PB2", "open", "GR-1.1-3")], defective));
        }

        // A NON-defective row needs no claim, and a claim on one is not an error either — a note may legitimately
        // own a row it has already fixed, or one it is about to break.
        Assert.Empty(UnclaimedDefectiveRows(
            [Row("GR-1.1-2", "CONFORMS")], [Note("PB1", "open", "GR-1.1-2")], defective));

        // A typo'd claim satisfies nothing while looking like ownership: BOTH checks must notice.
        Assert.Single(ClaimsNamingNoRow(inventory, [Note("PB1", "open", "GR-1.1-99")]));
        Assert.Single(UnclaimedDefectiveRows(inventory,
            [Note("PB1", "open", "GR-1.1-99"), Note("PB2", "open", "GR-1.1-3")], defective));
        Assert.Empty(ClaimsNamingNoRow(inventory, [Note("PB1", "open", "GR-1.1-1", "GR-1.1-2")]));

        // The frontmatter reader, against the shapes the register actually contains: LF and CRLF, quoted and
        // bare list members, a note with no claim at all, and a file that is not a note.
        Assert.Equal(new[] { "GR-1.1-1", "GR-1.1-2" },
            ParseNote("a.md", "---\nid: PB9\nstatus: open\ninventory_rows: [\"GR-1.1-1\", \"GR-1.1-2\"]\n---\nbody")!
                .InventoryRows);
        Assert.Equal(new[] { "GR-1.1-1" },
            ParseNote("b.md", "---\r\nid: PB9\r\nstatus: open\r\ninventory_rows: [GR-1.1-1]\r\n---\r\nbody")!
                .InventoryRows);
        Assert.Empty(ParseNote("c.md", "---\nid: PB9\nstatus: open\n---\nbody")!.InventoryRows);
        Assert.Null(ParseNote("d.md", "# not a note\n"));
        Assert.False(ParseNote("e.md", "---\nid: PB9\nstatus: landed\n---\n")!.IsLive);
        Assert.True(ParseNote("f.md", "---\nid: PB9\nstatus: open\n---\n")!.IsLive);
    }
}
