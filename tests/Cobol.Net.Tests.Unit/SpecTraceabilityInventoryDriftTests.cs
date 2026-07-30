// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The battery gate over the PHASE-14 Step-0 spec-traceability inventory — the artifact whose GAP count DEFINES
/// v1.0 (owner decision D13: zero GAP = P14 done).
/// </summary>
/// <remarks>
/// <para>
/// A completion metric that nothing audits is a self-report. Every row of
/// <c>tests/version-matrix/traceability-inventory.json</c> claims three things — that a rule was located in the
/// code, that a verdict was reached against the spec text, and that a spec-derived test covers it — and each of
/// those claims is cheap to write and expensive to earn. These tests make the cheap version fail:
/// <c>state</c> is RECOMPUTED here rather than believed, a <c>code-location</c> must name a file and a symbol that
/// still exist, and a <c>test-ref</c> must resolve to a test that is really on disk.
/// </para>
/// <para>
/// ⛔ The referential half is deliberately HERE and nowhere else. <c>record_verdicts.py</c> validates an incoming
/// batch's SHAPE and stops; it does not re-check that references resolve, because that predicate has to keep
/// holding as the tree changes underneath a row recorded sessions ago — which only something running every build
/// can do. One rule, one place (<c>feedback_one_rule_one_place</c>).
/// </para>
/// <para>
/// The vocabulary these tests enforce is not written down here either: verdicts, their <c>resolves</c> flag, their
/// required evidence and the test-ref forms are read from <c>tests/version-matrix/inventory-schema.json</c>, the
/// same file the Python side reads. What exists twice is the evaluator, not the rule — and
/// <see cref="EveryRowState_IsDerived_NotAsserted"/> is what would catch the two evaluators drifting apart.
/// </para>
/// <para>
/// ⚠ <see cref="TheseChecks_ActuallyFail_OnAFabricatedInventory"/> is not a formality. A gate that has only ever
/// been observed passing is indistinguishable from one that inspects nothing
/// (<c>feedback_green_gates_arent_evidence</c>), and this one will spend most of its life green over rows nobody
/// has touched. It runs every check above against rows built to break it, one defect class at a time.
/// </para>
/// </remarks>
public sealed class SpecTraceabilityInventoryDriftTests
{
    // ── the artifacts under test ─────────────────────────────────────────────────────────────────────

    private sealed record Row(
        string RuleId, string Section, string Kind, int Ordinal, string Subject,
        string Verdict, string CodeLocation, string TestRef, string Editions, string Notes, string State);

    private sealed record CatalogRule(string Id, string Section, string Kind, int Ordinal, string Subject);

    private sealed record Verdict(string Name, bool Resolves, string[] Requires);

    private sealed record TestRefForm(string Scheme, string? PathTemplate, string? TestDir, bool SpecDerived);

    private sealed record Schema(
        IReadOnlyDictionary<string, Verdict> Verdicts,
        string[] Editions,
        Regex CodeLocationPattern,
        string CodeLocationSeparator,
        string TestRefSeparator,
        IReadOnlyDictionary<string, TestRefForm> TestRefForms,
        bool SpecDerivedRequired,
        Regex[] DisqualifyingMethods);

    private static string Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";

    private static Schema LoadSchema()
    {
        string path = TestRepo.VersionMatrix("inventory-schema.json");
        Assert.True(File.Exists(path), $"inventory schema missing: {path}");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;

        var verdicts = new Dictionary<string, Verdict>(StringComparer.Ordinal);
        foreach (var p in root.GetProperty("verdicts").EnumerateObject())
        {
            verdicts[p.Name] = new Verdict(
                p.Name,
                p.Value.GetProperty("resolves").GetBoolean(),
                [.. p.Value.GetProperty("requires").EnumerateArray().Select(x => x.GetString()!)]);
        }

        var testRef = root.GetProperty("test-ref");
        var forms = new Dictionary<string, TestRefForm>(StringComparer.Ordinal);
        foreach (var p in testRef.GetProperty("forms").EnumerateObject())
        {
            string tpl = Str(p.Value, "path-template");
            string dir = Str(p.Value, "test-dir");
            forms[p.Name] = new TestRefForm(p.Name, tpl.Length > 0 ? tpl : null, dir.Length > 0 ? dir : null,
                p.Value.GetProperty("spec-derived").GetBoolean());
        }

        var loc = root.GetProperty("code-location");
        return new Schema(
            verdicts,
            [.. root.GetProperty("editions").EnumerateArray().Select(x => x.GetString()!)],
            new Regex(loc.GetProperty("pattern").GetString()!, RegexOptions.Compiled),
            loc.GetProperty("separator").GetString()!,
            testRef.GetProperty("separator").GetString()!,
            forms,
            testRef.GetProperty("spec-derived-required").GetBoolean(),
            [.. testRef.GetProperty("disqualifying-method-patterns").EnumerateArray()
                .Select(x => new Regex(x.GetString()!, RegexOptions.Compiled))]);
    }

    private static List<Row> LoadInventory()
    {
        string path = TestRepo.VersionMatrix("traceability-inventory.json");
        Assert.True(File.Exists(path),
            $"inventory missing: {path} — run python scripts/spec/build_inventory.py");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return [.. doc.RootElement.EnumerateArray().Select(e => new Row(
            Str(e, "rule-id"), Str(e, "section"), Str(e, "kind"), e.GetProperty("ordinal").GetInt32(),
            Str(e, "subject"), Str(e, "verdict"), Str(e, "code-location"), Str(e, "test-ref"),
            Str(e, "editions"), Str(e, "notes"), Str(e, "state")))];
    }

    private static List<CatalogRule> LoadCatalog()
    {
        string path = TestRepo.Docs("rearchitecture", "spec-rule-catalog.json");
        Assert.True(File.Exists(path),
            $"rule catalog missing: {path} — run python scripts/spec/extract_rule_catalog.py");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return [.. doc.RootElement.GetProperty("rules").EnumerateArray().Select(e => new CatalogRule(
            Str(e, "id"), Str(e, "section"), Str(e, "kind"), e.GetProperty("ordinal").GetInt32(),
            Str(e, "subject")))];
    }

    private static string[] Split(string value, string separator) =>
        [.. value.Split(separator.Trim(), StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];

    // ── the checks, as pure functions so the self-test can drive the SAME code ───────────────────────

    /// <summary>
    /// The state a row is ENTITLED to, per the schema. Mirrors <c>Schema.state_for</c> in Python — and
    /// <see cref="EveryRowState_IsDerived_NotAsserted"/> is what catches the two drifting apart.
    /// </summary>
    /// <remarks>
    /// The spec-derived clause is separate from <c>requires</c> on purpose: it is what makes
    /// CONFORMS-but-untested expressible (the rule verified against the code, no test yet pinning it), a category
    /// the design doc §3 Phase C names outright.
    /// </remarks>
    private static string DerivedState(Row r, Schema s)
    {
        if (!s.Verdicts.TryGetValue(r.Verdict, out var v) || !v.Resolves) return "GAP";
        if (!v.Requires.All(f => Field(r, f).Length > 0)) return "GAP";
        if (!s.SpecDerivedRequired) return r.TestRef.Length > 0 ? "OK" : "GAP";
        return Split(r.TestRef, s.TestRefSeparator).Any(x => IsSpecDerived(x, s)) ? "OK" : "GAP";
    }

    private static string Field(Row r, string name) => name switch
    {
        "verdict" => r.Verdict,
        "code-location" => r.CodeLocation,
        "test-ref" => r.TestRef,
        "editions" => r.Editions,
        "notes" => r.Notes,
        _ => throw new InvalidOperationException(
            $"inventory-schema.json requires a field '{name}' that no inventory row has — "
            + "add it to the row schema in build_inventory.py and to Field() here."),
    };

    private static List<string> BadVerdicts(IEnumerable<Row> rows, Schema s) =>
        [.. rows.Where(r => r.Verdict.Length > 0 && !s.Verdicts.ContainsKey(r.Verdict))
                .Select(r => $"{r.RuleId}: verdict '{r.Verdict}' is not in the schema vocabulary")];

    private static List<string> BadStates(IEnumerable<Row> rows, Schema s) =>
        [.. rows.Where(r => r.State != DerivedState(r, s))
                .Select(r => $"{r.RuleId}: state '{r.State}' but the schema derives '{DerivedState(r, s)}' "
                             + $"from verdict '{r.Verdict}'")];

    private static List<string> MissingEvidence(IEnumerable<Row> rows, Schema s) =>
        [.. from r in rows
            where s.Verdicts.TryGetValue(r.Verdict, out _)
            let v = s.Verdicts[r.Verdict]
            from f in v.Requires
            where Field(r, f).Length == 0
            select $"{r.RuleId}: verdict {r.Verdict} requires a non-empty '{f}'"];

    /// <summary>
    /// Is this one ref an acceptable BASIS for closing a row — spec-derived, not a differential?
    /// </summary>
    /// <remarks>
    /// Two ways to fail. The FORM can be inherently differential (a NIST CCVS golden, a characterization
    /// snapshot). Or the form can be spec-derived-capable while the specific test is not: an xUnit test named
    /// <c>*_MatchesLegacy</c> says in its own name that its expected value came from the legacy engine, which
    /// CLAUDE.md rule 1 forbids as authority. Keying on the repo's own naming convention is narrow, but it is
    /// exact where it applies, and it fails LOUDLY rather than quietly accepting a differential as coverage.
    /// </remarks>
    private static bool IsSpecDerived(string reference, Schema s)
    {
        int colon = reference.IndexOf(':');
        string scheme = colon < 0 ? reference : reference[..colon];
        if (!s.TestRefForms.TryGetValue(scheme, out var form) || !form.SpecDerived) return false;
        string body = colon < 0 ? "" : reference[(colon + 1)..].Trim();
        string method = body[(body.LastIndexOf('.') + 1)..];
        return !s.DisqualifyingMethods.Any(rx => rx.IsMatch(method));
    }

    /// <summary>
    /// A row that is CLOSED (state OK) but rests only on differential evidence. <see cref="DerivedState"/> already
    /// refuses to close such a row, so in a healthy tree this is unreachable — it is kept as a named, independent
    /// assertion of the RULE, so that a future bug in the derivation cannot quietly close rows on NIST goldens.
    /// </summary>
    private static List<string> DifferentialOnlyCoverage(IEnumerable<Row> rows, Schema s)
    {
        if (!s.SpecDerivedRequired) return [];
        return [.. from r in rows
                   where r.State == "OK"
                   let refs = Split(r.TestRef, s.TestRefSeparator)
                   where !refs.Any(x => IsSpecDerived(x, s))
                   select $"{r.RuleId}: state OK but covered ONLY by non-spec-derived test(s) "
                          + $"[{string.Join(", ", refs)}] — a differential cannot close a row (design doc §1c)"];
    }

    private static List<string> BadEditions(IEnumerable<Row> rows, Schema s) =>
        [.. from r in rows
            where r.Editions.Length > 0
            from e in Split(r.Editions, ",")
            where !s.Editions.Contains(e)
            select $"{r.RuleId}: editions names '{e}', not one of {string.Join('/', s.Editions)}"];

    private static List<string> UnresolvedCodeLocations(IEnumerable<Row> rows, Schema s, string root)
    {
        var text = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var bad = new List<string>();
        foreach (var r in rows.Where(r => r.CodeLocation.Length > 0))
        {
            foreach (string loc in Split(r.CodeLocation, s.CodeLocationSeparator))
            {
                if (!s.CodeLocationPattern.IsMatch(loc))
                {
                    bad.Add($"{r.RuleId}: code-location '{loc}' is not '<repo-relative-path>[#Symbol]'");
                    continue;
                }

                string[] parts = loc.Split('#', 2);
                string file = Path.Combine(root, parts[0].Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(file))
                {
                    bad.Add($"{r.RuleId}: code-location '{loc}' — no such file");
                    continue;
                }
                if (parts.Length == 1) continue;

                if (!text.TryGetValue(file, out string? body))
                    text[file] = body = File.ReadAllText(file);
                // A symbol, not a line number: it survives every edit that does not delete the thing it names,
                // which is the only event this gate should go red for.
                if (!Regex.IsMatch(body, $@"\b{Regex.Escape(parts[1])}\b"))
                    bad.Add($"{r.RuleId}: code-location '{loc}' — '{parts[1]}' is no longer in that file");
            }
        }
        return bad;
    }

    private static List<string> UnresolvedTestRefs(IEnumerable<Row> rows, Schema s, string root)
    {
        // Both caches matter at full scale: 3,790 rows will eventually cite a few hundred distinct test classes,
        // and the uncached form re-globs the directory AND re-reads the same .cs file once per citing row.
        var sources = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        var text = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var bad = new List<string>();
        foreach (var r in rows.Where(r => r.TestRef.Length > 0))
        {
            foreach (string reference in Split(r.TestRef, s.TestRefSeparator))
            {
                int colon = reference.IndexOf(':');
                string scheme = colon < 0 ? reference : reference[..colon];
                string body = colon < 0 ? "" : reference[(colon + 1)..].Trim();

                if (!s.TestRefForms.TryGetValue(scheme, out var form))
                {
                    bad.Add($"{r.RuleId}: test-ref '{reference}' — unknown form '{scheme}', expected one of "
                            + string.Join('/', s.TestRefForms.Keys.Order()));
                    continue;
                }
                if (body.Length == 0)
                {
                    bad.Add($"{r.RuleId}: test-ref '{reference}' has an empty body");
                    continue;
                }

                if (form.PathTemplate is { } template)
                {
                    string rel = template.Replace("{0}", body);
                    if (!File.Exists(Path.Combine(root, rel.Replace('/', Path.DirectorySeparatorChar))))
                        bad.Add($"{r.RuleId}: test-ref '{reference}' — no such case, expected {rel}");
                    continue;
                }

                int dot = body.LastIndexOf('.');
                if (dot <= 0 || dot == body.Length - 1)
                {
                    bad.Add($"{r.RuleId}: test-ref '{reference}' — expected <Class>.<Method>");
                    continue;
                }
                string cls = body[..dot], method = body[(dot + 1)..];
                string dir = Path.Combine(root, form.TestDir!.Replace('/', Path.DirectorySeparatorChar));
                if (!sources.TryGetValue(cls + '|' + dir, out string[]? files))
                    sources[cls + '|' + dir] = files = Directory.Exists(dir)
                        ? Directory.GetFiles(dir, cls + ".cs", SearchOption.AllDirectories) : [];

                if (files.Length == 0)
                {
                    bad.Add($"{r.RuleId}: test-ref '{reference}' — no {cls}.cs under {form.TestDir}");
                    continue;
                }
                if (!files.Any(f =>
                {
                    if (!text.TryGetValue(f, out string? src)) text[f] = src = File.ReadAllText(f);
                    return Regex.IsMatch(src, $@"\b{Regex.Escape(method)}\s*\(");
                }))
                {
                    bad.Add($"{r.RuleId}: test-ref '{reference}' — {cls}.cs has no method '{method}'");
                }
            }
        }
        return bad;
    }

    private static string Report(string what, List<string> violations, int scanned)
    {
        string head = $"{violations.Count} {what} over {scanned} inventory row(s):";
        return head + Environment.NewLine
            + string.Join(Environment.NewLine, violations.Take(20).Select(v => "    " + v))
            + (violations.Count > 20 ? $"{Environment.NewLine}    … and {violations.Count - 20} more" : "");
    }

    // ── the gate ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The inventory covers the catalog EXACTLY — no orphan row, no unreviewed rule hidden by omission.</summary>
    [Fact]
    public void Inventory_CoversTheCatalog_Exactly()
    {
        var rows = LoadInventory();
        var catalog = LoadCatalog();

        var inRows = rows.Select(r => r.RuleId).ToHashSet(StringComparer.Ordinal);
        var inCatalog = catalog.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);

        var missing = inCatalog.Except(inRows).Order().Take(10).ToList();
        var orphan = inRows.Except(inCatalog).Order().Take(10).ToList();
        Assert.True(missing.Count == 0 && orphan.Count == 0,
            $"inventory {rows.Count} rows vs catalog {catalog.Count} rules — "
            + $"rules with no row: [{string.Join(", ", missing)}]; rows with no rule: [{string.Join(", ", orphan)}] "
            + "— re-run python scripts/spec/build_inventory.py");
        Assert.Equal(rows.Count, rows.Select(r => r.RuleId).Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>A row's catalog-owned fields are REGENERATED, so a hand-edit to one is drift, not data.</summary>
    [Fact]
    public void EveryRow_MatchesItsCatalogEntry()
    {
        var catalog = LoadCatalog().ToDictionary(c => c.Id, StringComparer.Ordinal);
        var bad = new List<string>();
        foreach (var r in LoadInventory())
        {
            if (!catalog.TryGetValue(r.RuleId, out var c)) continue;   // covered by the coverage test
            if (r.Section != c.Section || r.Kind != c.Kind || r.Ordinal != c.Ordinal || r.Subject != c.Subject)
                bad.Add($"{r.RuleId}: row ({r.Section}/{r.Kind}/{r.Ordinal}) disagrees with the catalog "
                        + $"({c.Section}/{c.Kind}/{c.Ordinal})");
        }
        Assert.True(bad.Count == 0, Report("row(s) disagree with the catalog", bad, catalog.Count));
    }

    /// <summary>Every recorded verdict is one the schema defines.</summary>
    [Fact]
    public void EveryVerdict_IsInTheSchemaVocabulary()
    {
        var rows = LoadInventory();
        var bad = BadVerdicts(rows, LoadSchema());
        Assert.True(bad.Count == 0, Report("unknown verdict(s)", bad, rows.Count));
    }

    /// <summary>
    /// <c>state</c> is DERIVED from the verdict and its evidence — never taken at face value. This is the test that
    /// makes the burn-down honest: without it, closing a GAP costs one hand-edited string.
    /// </summary>
    [Fact]
    public void EveryRowState_IsDerived_NotAsserted()
    {
        var rows = LoadInventory();
        var bad = BadStates(rows, LoadSchema());
        Assert.True(bad.Count == 0, Report("row(s) whose stored state is not the derived one", bad, rows.Count));
    }

    /// <summary>Each verdict carries the evidence the schema says that verdict requires.</summary>
    [Fact]
    public void EveryVerdict_CarriesTheEvidenceItRequires()
    {
        var rows = LoadInventory();
        var bad = MissingEvidence(rows, LoadSchema());
        Assert.True(bad.Count == 0, Report("verdict(s) missing required evidence", bad, rows.Count));
    }

    /// <summary>
    /// ⛔ A row closes on a SPEC-DERIVED test or it does not close. The definition of DONE
    /// (<c>DESIGN-spec-conformance-review.md</c> §1c) requires the expected value to be computed from the spec,
    /// and CLAUDE.md rule 1 makes NIST, the legacy and GnuCOBOL regression nets rather than authority — a
    /// differential is structurally blind to a violation both sides share, which is exactly the case a
    /// conformance review exists to catch.
    /// </summary>
    [Fact]
    public void EveryResolvedRow_CitesASpecDerivedTest()
    {
        var rows = LoadInventory();
        var bad = DifferentialOnlyCoverage(rows, LoadSchema());
        Assert.True(bad.Count == 0, Report("row(s) closed on differential evidence alone", bad, rows.Count));
    }

    /// <summary>An <c>editions</c> field names only the four editions the compiler implements.</summary>
    [Fact]
    public void EveryEditionsField_NamesOnlyKnownEditions()
    {
        var rows = LoadInventory();
        var bad = BadEditions(rows, LoadSchema());
        Assert.True(bad.Count == 0, Report("bad edition name(s)", bad, rows.Count));
    }

    /// <summary>Every traceability link still points at code that exists — the link is the whole point of it.</summary>
    [Fact]
    public void EveryCodeLocation_ResolvesInTheTree()
    {
        var rows = LoadInventory();
        var bad = UnresolvedCodeLocations(rows, LoadSchema(), TestRepo.Root);
        Assert.True(bad.Count == 0, Report("unresolved code-location(s)", bad, rows.Count));
    }

    /// <summary>Every claimed covering test is really on disk — "tested" is checked, never asserted.</summary>
    [Fact]
    public void EveryTestRef_ResolvesToARealTest()
    {
        var rows = LoadInventory();
        var bad = UnresolvedTestRefs(rows, LoadSchema(), TestRepo.Root);
        Assert.True(bad.Count == 0, Report("unresolved test-ref(s)", bad, rows.Count));
    }

    /// <summary>
    /// ⛔ THE EVIDENCE THAT THIS GATE INSPECTS ANYTHING. Each check above is run against a row built to violate it;
    /// a check that returned clean here would be a check that never looked.
    /// </summary>
    [Fact]
    public void TheseChecks_ActuallyFail_OnAFabricatedInventory()
    {
        var s = LoadSchema();
        string root = TestRepo.Root;

        Row Base(string id) => new(id, "15.7", "AR", 1, "ABS function", "", "", "", "", "", "GAP");

        // A verdict outside the vocabulary.
        Assert.Single(BadVerdicts([Base("X-1") with { Verdict = "PROBABLY-FINE" }], s));

        // A GAP row hand-promoted to OK with nothing behind it — the cheapest possible way to fake the burn-down.
        Assert.Single(BadStates([Base("X-2") with { Verdict = "CONFORMS", State = "OK" }], s));

        // The mirror: a fully-evidenced CONFORMS row left marked GAP, so the metric under-reports.
        Assert.Single(BadStates([Base("X-3") with
        {
            Verdict = "CONFORMS",
            CodeLocation = "scripts/spec/build_inventory.py",
            TestRef = "unit:SpecTraceabilityInventoryDriftTests.EveryRowState_IsDerived_NotAsserted",
            State = "GAP",
        }], s));

        // CONFORMS with no code-location: the one field that verdict requires. (test-ref is NOT required to
        // record a CONFORMS — only to CLOSE the row — which is what makes CONFORMS-but-untested expressible.)
        Assert.Single(MissingEvidence([Base("X-4") with { Verdict = "CONFORMS" }], s));

        // An edition the compiler does not have.
        Assert.Single(BadEditions([Base("X-5") with { Editions = "85,1974" }], s));

        // ⛔ DIFFERENTIAL-ONLY COVERAGE. A NIST golden alone, and a *_MatchesLegacy test alone, are both real
        // shapes — the first Phase-B batch produced seven of them before this rule existed. Neither may close a
        // row, so the derived state is GAP and a row asserting OK on that basis is caught twice over.
        Row Covered(string id, string testRef) => Base(id) with
        {
            Verdict = "CONFORMS", CodeLocation = "scripts/spec/inventory_schema.py", TestRef = testRef, State = "OK",
        };
        Assert.Single(DifferentialOnlyCoverage([Covered("X-5a", "nist:IF128A")], s));
        Assert.Single(DifferentialOnlyCoverage(
            [Covered("X-5b", "conformance-test:IntrinsicFunctionDifferentialTests.ExactFamily_MatchesLegacy")], s));
        Assert.Single(BadStates([Covered("X-5c", "nist:IF128A")], s));   // OK asserted; GAP derived

        // …but the SAME row also citing one spec-derived test is fine: corroboration is welcome, it just cannot
        // be the whole basis.
        Assert.Empty(DifferentialOnlyCoverage(
            [Covered("X-5d", "nist:IF128A; conformance:2023/da2_function_as_text")], s));
        Assert.Empty(BadStates([Covered("X-5e", "nist:IF128A; conformance:2023/da2_function_as_text")], s));

        // CONFORMS-but-untested: verdict recordable, row NOT closed. This is the category the split exists for.
        Assert.Empty(MissingEvidence(
            [Base("X-5f") with { Verdict = "CONFORMS", CodeLocation = "scripts/spec/inventory_schema.py" }], s));
        Assert.Empty(BadStates(
            [Base("X-5g") with { Verdict = "CONFORMS", CodeLocation = "scripts/spec/inventory_schema.py" }], s));

        // A code-location whose FILE is gone, and one whose SYMBOL is gone from a file that still exists.
        Assert.Single(UnresolvedCodeLocations(
            [Base("X-6") with { CodeLocation = "src/Cobol.Net.Compiler/NoSuchFile.cs" }], s, root));
        Assert.Single(UnresolvedCodeLocations(
            [Base("X-7") with { CodeLocation = "scripts/spec/build_inventory.py#NoSuchSymbolAnywhere" }], s, root));

        // Test refs: an unknown form, a golden that does not exist, and an xUnit method that does not exist.
        Assert.Single(UnresolvedTestRefs([Base("X-8") with { TestRef = "vibes:it-looked-right" }], s, root));
        Assert.Single(UnresolvedTestRefs([Base("X-9") with { TestRef = "conformance:2023/no_such_case" }], s, root));
        Assert.Single(UnresolvedTestRefs(
            [Base("X-10") with { TestRef = "unit:SpecTraceabilityInventoryDriftTests.NoSuchMethod" }], s, root));

        // And the positive controls — the same checkers must PASS on references that really do resolve, or the
        // failures above would prove only that the checker rejects everything.
        Assert.Empty(UnresolvedCodeLocations(
            [Base("X-11") with { CodeLocation = "scripts/spec/inventory_schema.py#state_for" }], s, root));
        Assert.Empty(UnresolvedTestRefs(
            [Base("X-12") with
            {
                TestRef = "unit:SpecTraceabilityInventoryDriftTests.EveryRowState_IsDerived_NotAsserted",
            }], s, root));
    }
}
