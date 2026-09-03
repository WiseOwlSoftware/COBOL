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
/// required evidence, the test-ref forms, the per-KIND evidence rules and the anchored-file spaces are read from
/// <c>tests/version-matrix/inventory-schema.json</c>, the same file the Python side reads. What exists twice is
/// the evaluator, not the rule — and <see cref="EveryRowState_IsDerived_NotAsserted"/> is what would catch the two
/// evaluators drifting apart.
/// </para>
/// <para>
/// ⚖ WITHIN this side the predicate exists ONCE. <see cref="AnchorFor"/> and <see cref="IsObservable"/> are the
/// only places that decide what a kind-anchored row's anchor is and whether it names an implementing site;
/// <see cref="DerivedState"/> and <see cref="MisanchoredRows"/> both route through them. Three sites each keying
/// on the same regex independently would have meant that a single typo in <c>implementation-pattern</c> silently
/// reclassified every DOC row while three "independent" assertions stayed green.
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

    /// <summary>
    /// The per-KIND evidence rules (<c>kinds</c> in the schema): what a row of this kind costs, as opposed to
    /// what its VERDICT costs. <c>AnchorTemplate</c> expands the row's own rule-id into the register anchor it
    /// must carry; <c>Implementation</c> is the predicate that decides, from the row's own locations, whether
    /// there is anything in the compiler to observe; <c>AnchorExemptVerdicts</c> names the verdicts that do NOT
    /// claim a determination and therefore owe no anchor (a declined facility withdraws the A.1 item).
    /// </summary>
    private sealed record KindRule(string AnchorTemplate, Regex? Implementation, string[] AnchorExemptVerdicts);

    private sealed record Schema(
        IReadOnlyDictionary<string, Verdict> Verdicts,
        string[] Editions,
        Regex CodeLocationPattern,
        string CodeLocationSeparator,
        string TestRefSeparator,
        IReadOnlyDictionary<string, TestRefForm> TestRefForms,
        bool SpecDerivedRequired,
        Regex[] DisqualifyingMethods,
        IReadOnlyDictionary<string, KindRule> Kinds,
        IReadOnlyDictionary<string, Regex> AnchoredFiles);

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

        // `kinds` and `anchored-files` both carry $-prefixed DOCUMENTATION siblings inside the same object, so
        // both readers skip them by prefix rather than by name — a new comment key must never read as a kind.
        var kinds = new Dictionary<string, KindRule>(StringComparer.Ordinal);
        if (root.TryGetProperty("kinds", out var kindsElem))
        {
            foreach (var p in kindsElem.EnumerateObject())
            {
                if (p.Name.StartsWith('$')) continue;
                string impl = Str(p.Value, "implementation-pattern");
                string[] exempt = p.Value.TryGetProperty("anchor-exempt-verdicts", out var ex)
                    ? [.. ex.EnumerateArray().Select(x => x.GetString()!)]
                    : [];
                kinds[p.Name] = new KindRule(
                    Str(p.Value, "anchor-template"),
                    impl.Length > 0 ? new Regex(impl, RegexOptions.Compiled) : null,
                    exempt);
            }
        }

        var anchored = new Dictionary<string, Regex>(StringComparer.Ordinal);
        if (loc.TryGetProperty("anchored-files", out var anchoredElem))
        {
            foreach (var p in anchoredElem.EnumerateObject())
            {
                if (p.Name.StartsWith('$')) continue;
                anchored[p.Name] = new Regex(p.Value.GetString()!, RegexOptions.Compiled);
            }
        }

        return new Schema(
            verdicts,
            [.. root.GetProperty("editions").EnumerateArray().Select(x => x.GetString()!)],
            new Regex(loc.GetProperty("pattern").GetString()!, RegexOptions.Compiled),
            loc.GetProperty("separator").GetString()!,
            testRef.GetProperty("separator").GetString()!,
            forms,
            testRef.GetProperty("spec-derived-required").GetBoolean(),
            [.. testRef.GetProperty("disqualifying-method-patterns").EnumerateArray()
                .Select(x => new Regex(x.GetString()!, RegexOptions.Compiled))],
            kinds,
            anchored);
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
        // A kind that declares evidence rules pays them too, and BOTH are extra COSTS rather than an escape: the
        // computed anchor must be present (a determination filed under another item is not evidence for this
        // row), and something in the compiler must implement it. A DOC row whose only location is its own §7
        // anchor therefore stays a GAP — closing it would widen the design doc §1(a) definition of DONE, which
        // is the owner's to widen and not an agent's (kb/Work PB280 Q2). ⚠ Both costs are charged only to a
        // verdict that CLAIMS a determination: a DOCUMENTED-NON-SUPPORT DOC row declines the facility, which
        // withdraws the A.1 item (A.1 preamble) — there is no §7 row to anchor and nothing implemented to
        // observe. It still owes its WITNESS test, which is what keeps such a row a GAP here.
        if (AnchorObliged(r, s) && (!IsAnchored(r, s) || !IsObservable(r, s))) return "GAP";
        if (!s.SpecDerivedRequired) return r.TestRef.Length > 0 ? "OK" : "GAP";
        return Split(r.TestRef, s.TestRefSeparator).Any(x => IsSpecDerived(x, s)) ? "OK" : "GAP";
    }

    /// <summary>
    /// The register anchor this row's KIND obliges, COMPUTED from the row's own rule-id — or <c>null</c> when the
    /// kind declares no rule. Mirrors <c>Schema.anchor_for</c> in Python.
    /// </summary>
    /// <remarks>
    /// ⛔ Written ONCE on this side and read by <see cref="DerivedState"/> and <see cref="MisanchoredRows"/>, so
    /// the two cannot disagree about what a row's anchor is. The anchor being a FUNCTION of the row is the whole
    /// mechanism: a determination cannot be filed under the wrong item the way <c>kb/Work/A11</c> recorded — the
    /// §15.3.3.2 fractional-seconds determination sat under item 87, whose obligation is FORMATTED-CURRENT-DATE's
    /// accuracy, because the NUMBER was inherited and never re-derived.
    /// </remarks>
    private static string? AnchorFor(Row r, Schema s) =>
        s.Kinds.TryGetValue(r.Kind, out var k) && k.AnchorTemplate.Length > 0
            ? k.AnchorTemplate.Replace("{rule-id}", r.RuleId, StringComparison.Ordinal)
            : null;

    private static bool IsAnchored(Row r, Schema s) =>
        AnchorFor(r, s) is { } anchor
        && Split(r.CodeLocation, s.CodeLocationSeparator).Contains(anchor, StringComparer.Ordinal);

    /// <summary>
    /// Does this row OWE the anchor <see cref="AnchorFor"/> computes — i.e. does its verdict claim a
    /// determination? Mirrors <c>Schema.anchor_obliged</c> in Python.
    /// </summary>
    /// <remarks>
    /// CONFORMS, PARTIAL and DIVERGES each assert something about what the register says, so each owes its own §7
    /// row. DOCUMENTED-NON-SUPPORT asserts the opposite: the conditioning facility is not implemented, so Annex
    /// A.1's own preamble withdraws the item ("the item is not required if the optional or processor-dependent
    /// feature is not implemented") and there is no determination left to anchor. The exempt verdicts are DATA on
    /// the kind (<c>anchor-exempt-verdicts</c>), so this side and the Python writer read ONE rule.
    /// </remarks>
    private static bool AnchorObliged(Row r, Schema s) =>
        AnchorFor(r, s) is not null
        && s.Kinds.TryGetValue(r.Kind, out var k)
        && !k.AnchorExemptVerdicts.Contains(r.Verdict, StringComparer.Ordinal);

    /// <summary>
    /// Does this row name a site in the compiler through which a program could observe the determination?
    /// </summary>
    /// <remarks>
    /// The predicate is the row's OWN <c>code-location</c> list read through its kind's
    /// <c>implementation-pattern</c> — the greenfield <c>src/Cobol.Net.*</c> predicate
    /// <c>scripts/spec/audit_annex_a1.py</c>'s source sweep already applies. A PATH is falsifiable by an
    /// independent reader who goes looking for the code; an opinion is not. A kind that declares no pattern is
    /// vacuously observable, so adding a kind never tightens an existing one.
    /// </remarks>
    private static bool IsObservable(Row r, Schema s)
    {
        if (!s.Kinds.TryGetValue(r.Kind, out var k) || k.Implementation is null) return true;
        string? anchor = AnchorFor(r, s);
        return Split(r.CodeLocation, s.CodeLocationSeparator)
            .Any(x => !string.Equals(x, anchor, StringComparison.Ordinal) && k.Implementation.IsMatch(x));
    }

    /// <summary>
    /// A row whose KIND obliges a register anchor and whose locations do not carry the anchor its own rule-id
    /// computes — either none at all, or, the <c>kb/Work/A11</c> shape, the anchor of a DIFFERENT item.
    /// </summary>
    private static List<string> MisanchoredRows(IEnumerable<Row> rows, Schema s) =>
        [.. from r in rows
            where (r.Verdict.Length > 0 || r.CodeLocation.Length > 0)
                  && AnchorObliged(r, s) && !IsAnchored(r, s)
            select $"{r.RuleId}: kind {r.Kind} requires the computed register anchor '{AnchorFor(r, s)}' among "
                   + $"its code-location(s) [{r.CodeLocation}] — the anchor is derived from the rule-id, never "
                   + "chosen, so a determination filed under another item cannot be spelled"];

    /// <summary>
    /// Any <c>code-location</c> naming a file whose fragments are a NAMED ANCHOR SPACE, with a fragment outside
    /// that space — or with no fragment at all.
    /// </summary>
    /// <remarks>
    /// ⛔ This is the check that turns <c>docs/CONFORMANCE.md#7</c> red. The resolver below satisfies a
    /// <c>#Symbol</c> by a word search over the file body: exact for a C# identifier, worthless for a number —
    /// <c>#7</c> matches the digit 7 anywhere in a 790-line document, and five live rows were <c>state: OK</c> on
    /// that basis. A BARE citation of an anchored file is the same defect one step weaker, resolving on
    /// <c>File.Exists</c> alone, so it is a violation too; three more live rows were doing it.
    /// </remarks>
    private static List<string> BadAnchorFragments(IEnumerable<Row> rows, Schema s) =>
        [.. from r in rows
            where r.CodeLocation.Length > 0
            from loc in Split(r.CodeLocation, s.CodeLocationSeparator)
            let parts = loc.Split('#', 2)
            where s.AnchoredFiles.ContainsKey(parts[0])
            let rx = s.AnchoredFiles[parts[0]]
            where parts.Length == 1 || !rx.IsMatch(parts[1])
            select $"{r.RuleId}: code-location '{loc}' — '{parts[0]}' is an anchored file, so its fragment must "
                   + $"match {rx} ({(parts.Length == 1 ? "there is no fragment at all" : $"got '{parts[1]}'")})"];

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

    /// <summary>
    /// ⛔ Every row of a kind that carries a register anchor anchors ITS OWN register entry. The anchor is
    /// computed from the row's rule-id, so this fails on the <c>kb/Work/A11</c> shape — a determination filed
    /// under a neighbouring item — which no amount of reading the prose had caught.
    /// </summary>
    [Fact]
    public void EveryKindAnchoredRow_AnchorsItsOwnRegisterEntry()
    {
        var rows = LoadInventory();
        var s = LoadSchema();
        Assert.True(s.Kinds.Count > 0,
            "inventory-schema.json declares no `kinds` — this gate would then measure nothing, so the schema, "
            + "not the gate, is what changed.");
        var bad = MisanchoredRows(rows, s);
        Assert.True(bad.Count == 0, Report("misanchored row(s)", bad, rows.Count));
    }

    /// <summary>
    /// Every fragment on a file whose fragments are a named anchor space is a legal anchor of that space — and an
    /// anchored file is never cited bare.
    /// </summary>
    [Fact]
    public void EveryRegisterAnchor_IsAWellFormedAnchorOfItsFile()
    {
        var rows = LoadInventory();
        var s = LoadSchema();
        Assert.True(s.AnchoredFiles.Count > 0,
            "inventory-schema.json lists no `anchored-files` — the mechanism that turns `#7` red is gone.");
        var bad = BadAnchorFragments(rows, s);
        Assert.True(bad.Count == 0, Report("ill-formed register anchor(s)", bad, rows.Count));
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

        // ── kind-DOC rows: the register anchor, and what a DOC row costs ─────────────────────────────
        //
        // A DOC row is an obligation to DOCUMENT an implementor-defined element (ISO §4.2.5), so its evidence is
        // a §7 determination filed under its own item PLUS a site in the compiler PLUS a spec-derived test. Each
        // of the three is driven separately below, and each positive control shows the checker discriminating.
        const string Anchor = "docs/CONFORMANCE.md#DOC-A.1-19";
        const string Src = "src/Cobol.Net.Runtime/Control/ProgramTable.cs#CancelNode";
        Row Doc(string id) => new(id, "A.1", "DOC", 19,
            "CANCEL statement (result of canceling a non-COBOL program)", "CONFORMS", "", "", "", "", "GAP");

        // A src site and no register anchor at all: the determination is unlocatable.
        Assert.Single(MisanchoredRows([Doc("DOC-A.1-19") with { CodeLocation = Src }], s));
        // ⛔ THE A11 SHAPE — the anchor of a DIFFERENT item. `#DOC-A.1-18` is a real anchor of a real row, so
        // nothing about it is malformed; it is simply not THIS row's, and only a COMPUTED anchor can see that.
        Assert.Single(MisanchoredRows(
            [Doc("DOC-A.1-19") with { CodeLocation = $"docs/CONFORMANCE.md#DOC-A.1-18; {Src}" }], s));
        // …and the control: its own anchor passes, so the two failures above are not blanket rejection.
        Assert.Empty(MisanchoredRows([Doc("DOC-A.1-19") with { CodeLocation = $"{Anchor}; {Src}" }], s));
        // A non-DOC row is untouched by the anchor rule — adding a kind must not retroactively bind 4,089 others.
        Assert.Empty(MisanchoredRows([Base("X-13") with { Verdict = "CONFORMS", CodeLocation = Src }], s));

        // ⛔ `#7`: the fragment five live rows carried, which the resolver satisfies against the digit 7 anywhere
        // in the file. And the bare path, which is the same defect resolving on File.Exists alone.
        Assert.Single(BadAnchorFragments([Doc("DOC-A.1-19") with { CodeLocation = "docs/CONFORMANCE.md#7" }], s));
        Assert.Single(BadAnchorFragments([Doc("DOC-A.1-19") with { CodeLocation = "docs/CONFORMANCE.md" }], s));
        // The controls: every anchor space the document really has stays legal — §7's item rows, §1's §4.2.16
        // summary, §5's Annex A.4 rows — and a file that is not anchored is not policed.
        Assert.Empty(BadAnchorFragments([Doc("DOC-A.1-19") with { CodeLocation = Anchor }], s));
        Assert.Empty(BadAnchorFragments(
            [Base("X-14") with { CodeLocation = "docs/CONFORMANCE.md#4.2.16" }], s));
        Assert.Empty(BadAnchorFragments(
            [Base("X-15") with { CodeLocation = "docs/CONFORMANCE.md#A.4.9" }], s));
        Assert.Empty(BadAnchorFragments([Base("X-16") with { CodeLocation = Src }], s));

        // OBSERVABLE but untested, asserted OK — the ordinary §1(c) failure, on a DOC row.
        Assert.Single(BadStates(
            [Doc("DOC-A.1-19") with { CodeLocation = $"{Anchor}; {Src}", State = "OK" }], s));
        // ⛔ NO POLICY ARM. A row whose ONLY location is its register anchor does not close — not with no test,
        // and not with a spec-derived one either. Closing it would widen the design doc §1(a) definition of DONE
        // (a traceability link into src/Cobol.Net.*, or a recorded owner non-support decision), and that is the
        // owner's completion metric, not an agent's (kb/Work PB280 Q2). Both arms are driven, because the
        // second is the one a future "it has a test, surely it closes" reading would break.
        Assert.Single(BadStates([Doc("DOC-A.1-19") with { CodeLocation = Anchor, State = "OK" }], s));
        Assert.Single(BadStates(
            [Doc("DOC-A.1-19") with
            {
                CodeLocation = Anchor, TestRef = "conformance:2023/pb154_cancel_active", State = "OK",
            }], s));
        // The LEGACY engine is not an implementing site: CobolSharp.* is the oracle being retired, not the
        // compiler, so a row resting on it is as unobservable as one resting on nothing.
        Assert.Single(BadStates(
            [Doc("DOC-A.1-19") with
            {
                CodeLocation = $"{Anchor}; src/CobolSharp.Runtime/Intrinsics/IntrinsicFunctions.cs#Cancel",
                TestRef = "conformance:2023/pb154_cancel_active", State = "OK",
            }], s));
        // ✔ And the shape that DOES close: own anchor + a greenfield site + a spec-derived test.
        Assert.Empty(BadStates(
            [Doc("DOC-A.1-19") with
            {
                CodeLocation = $"{Anchor}; {Src}", TestRef = "conformance:2023/pb154_cancel_active", State = "OK",
            }], s));
        // …which is also the row `DifferentialOnlyCoverage` must leave alone — no kind exemption exists there,
        // and none is needed, because every closing DOC row carries a spec-derived test like every other row.
        Assert.Empty(DifferentialOnlyCoverage(
            [Doc("DOC-A.1-19") with
            {
                CodeLocation = $"{Anchor}; {Src}", TestRef = "conformance:2023/pb154_cancel_active", State = "OK",
            }], s));

        // ── the anchor is owed by the verdicts that CLAIM a determination (`anchor-exempt-verdicts`) ──
        //
        // ⛔ A DOC row a DECLINED module withdraws has no determination to anchor and nothing implemented to
        // observe: A.1's own preamble makes the item "not required if the optional or processor-dependent
        // feature is not implemented". Found only by landing the A.1 lane and the derived-verdict lane onto one
        // tree (2026-09-02): the derived lane stamps items 84, 85, 173 and 86 DOCUMENTED-NON-SUPPORT, and the
        // anchor rule — written where every DOC row was a determination — refused the whole 308-record batch.
        Row Declined(string id) => Doc(id) with
        {
            Verdict = "DOCUMENTED-NON-SUPPORT",
            Notes = "the conditioning module is Not claimed (docs/CONFORMANCE.md §5)",
            CodeLocation = "",
        };
        Assert.Empty(MisanchoredRows([Declined("DOC-A.1-19")], s));
        // …AND THE EXEMPTION IS NOT AN ESCAPE. Without its module's WITNESS the row still does not close, so a
        // module cannot move the burn-down by being declined — the §1(c) rule survives the carve-out intact.
        Assert.Single(BadStates([Declined("DOC-A.1-19") with { State = "OK" }], s));
        // The pairing that proves the exemption DISCRIMINATES rather than blanket-disabling the check: the same
        // absent anchor on a CONFORMS row is still caught (driven above), and the declined row closes exactly
        // when its witness arrives.
        Assert.Single(MisanchoredRows([Doc("DOC-A.1-19") with { CodeLocation = "" }], s));
        Assert.Empty(BadStates(
            [Declined("DOC-A.1-19") with
            {
                TestRef = "conformance:2023/pb154_cancel_active", State = "OK",
            }], s));
    }
}
