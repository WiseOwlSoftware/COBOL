// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE GATE THAT RUNS THE ANNEX A.1 REGISTER AUDIT — <c>scripts/spec/audit_annex_a1.py</c>.
/// </summary>
/// <remarks>
/// <para>
/// ISO §4.2.5 requires the implementor to SPECIFY every A.1 element identified as required and to DOCUMENT every
/// element identified as requiring user documentation; owner decision D13 makes those part of v1.0.
/// <c>docs/CONFORMANCE.md</c> §7 is that register, and the audit re-derives every item number in it against the
/// A.1 catalog parsed straight out of the standard — the check that caught the §15.3.3.2 fractional-seconds
/// determination filed under item 87 (kb/Work A11).
/// </para>
/// <para>
/// ⛔ IT WAS RUN ONLY WHEN A HUMAN REMEMBERED. Measured 2026-09-01: <c>audit_annex_a1.py</c> appeared nowhere in
/// <c>scripts/battery.sh</c>, <c>.github/workflows/build-and-test.yml</c> or <c>scripts/build-local.*</c> — the
/// register's own correctness was enforced by habit. This class is the wiring, and it follows the repo's
/// established precedent for a Python gate that runs every build:
/// <see cref="ExternalCorpusPopulationDriftTests"/> shells <c>scripts/corpus_sweep.py</c> from the Unit project.
/// That placement is strictly broader than a battery line would be — the Unit assembly runs in the per-commit
/// wave-local gate, in battery phase 1, and in both CI unit jobs (which run the project unfiltered, so this
/// class is picked up with no workflow edit at all).
/// </para>
/// <para>
/// A missing interpreter is a LOUD failure and not a skip: a silent green from a check that never ran is the
/// exact failure mode under repair (<c>feedback_verdict_evidence_invariant</c>).
/// </para>
/// </remarks>
public sealed class AnnexA1RegisterDriftTests
{
    /// <summary>
    /// Run the audit. The interpreter probe itself is <see cref="PythonInstrument"/> — shared, because
    /// <see cref="DerivedVerdictDriftTests"/> shells the selector engine's self-test the same way and two
    /// private copies of "which python launches here" is one rule written down twice.
    /// </summary>
    private static ProcessObservation RunAudit(params string[] args)
    {
        string script = TestRepo.Scripts("spec", "audit_annex_a1.py");
        Assert.True(File.Exists(script), $"the register audit is missing: {script}");
        return PythonInstrument.Run(script, args);
    }

    private static JsonElement JsonLineOf(ProcessObservation r)
    {
        string? line = r.Stdout.Split('\n').Select(l => l.Trim())
            .FirstOrDefault(l => l.StartsWith("JSON ", StringComparison.Ordinal));
        Assert.True(line is not null,
            $"audit_annex_a1.py --json emitted no JSON line.\nstdout:\n{r.Stdout}\nstderr:\n{r.Stderr}");
        return JsonDocument.Parse(line!["JSON ".Length..]).RootElement.Clone();
    }

    /// <summary>
    /// ⛔ THE INVARIANT: every §7 determination is filed under the A.1 item it actually names, no
    /// <c>DOC-A.1-&lt;n&gt;</c> token appears outside a row key, and each row's <c>Pinned by</c> cell agrees with
    /// its inventory row's own spec-derived <c>test-ref</c>.
    /// </summary>
    /// <remarks>
    /// The population is asserted before the verdict is believed: a run that filed nothing would satisfy
    /// "no findings" while measuring nothing at all, and a MISSING observation is not a NEGATIVE one.
    /// </remarks>
    [Fact]
    public void EveryDocRow_IsFiledUnderTheItemAnnexA1Names()
    {
        var r = RunAudit("--check", "--json");
        var j = JsonLineOf(r);

        int items = j.GetProperty("items").GetInt32();
        var filed = j.GetProperty("filed").EnumerateArray().Select(x => x.GetInt32()).ToList();
        Assert.True(items > 200, $"the A.1 catalog parsed {items} items — the register did not load");
        Assert.True(filed.Count > 0,
            "the audit filed ZERO §7 determinations, so 'no findings' is a statement about nothing. The §7 table "
            + $"or its parser is what changed.\n{r.Stdout}{r.Stderr}");

        var findings = j.GetProperty("findings").EnumerateArray().Select(x => x.GetString()!).ToList();
        Assert.True(findings.Count == 0,
            $"{findings.Count} Annex A.1 register finding(s):\n  " + string.Join("\n  ", findings.Take(20)));
        Assert.Equal(0, r.ExitCode);
    }

    /// <summary>
    /// Every A.1 item the INVENTORY records a verdict for has a §7 row filed for it. The two artifacts are the
    /// same claim from opposite ends — a DOC row's whole evidence is its determination — so a verdict recorded
    /// against an item the register does not document would be a claim with nothing behind it.
    /// </summary>
    /// <remarks>
    /// ⚠ EXCEPT an item a DECLINED module withdraws. Annex A.1's preamble makes an item "not required if the
    /// optional or processor-dependent feature is not implemented", so a DOC row stamped DOCUMENTED-NON-SUPPORT
    /// by a `derived-verdicts` selector has no determination to file and must not be expected to have one. The
    /// excluded set is the audit's own <c>unreachable</c>, DERIVED from those selectors — not a list here, so a
    /// module declined tomorrow needs no edit in this file.
    /// </remarks>
    [Fact]
    public void EveryVerdictedDocRow_HasItsRegisterDetermination()
    {
        var json = JsonLineOf(RunAudit("--json"));
        var filed = json.GetProperty("filed").EnumerateArray().Select(x => x.GetInt32()).ToHashSet();
        var withdrawn = json.GetProperty("unreachable").EnumerateArray().Select(x => x.GetInt32()).ToHashSet();

        string path = TestRepo.VersionMatrix("traceability-inventory.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var verdicted = new List<int>();
        foreach (var e in doc.RootElement.EnumerateArray())
        {
            if (e.GetProperty("kind").GetString() != "DOC") continue;
            string verdict = e.TryGetProperty("verdict", out var v) ? v.GetString() ?? "" : "";
            if (verdict.Length == 0) continue;
            string id = e.GetProperty("rule-id").GetString()!;
            verdicted.Add(int.Parse(id[(id.LastIndexOf('-') + 1)..]));
        }

        Assert.True(verdicted.Count > 0,
            "no kind-DOC inventory row carries a verdict — this gate would then be vacuous, and the A.1 "
            + "back-fill is what changed.");
        var orphan = verdicted.Where(n => !filed.Contains(n) && !withdrawn.Contains(n)).Order().ToList();
        Assert.True(orphan.Count == 0,
            $"{orphan.Count} A.1 item(s) carry an inventory verdict with no docs/CONFORMANCE.md §7 row: "
            + $"[{string.Join(", ", orphan.Take(20))}] — a DOC row's evidence IS its determination, so the "
            + "verdict rests on nothing. (An item a declined module withdraws is exempt and is reported by the "
            + "audit's own `unreachable`; these are not in it.)");
    }

    /// <summary>
    /// ⛔ THE EVIDENCE THAT THE AUDIT INSPECTS ANYTHING — its own <c>--self-test</c>, which drives every check
    /// against input built to break it plus positive controls, and names each case in its output.
    /// </summary>
    /// <remarks>
    /// Asserting the NAMES, not just the exit code, is what catches a self-test case being silently deleted:
    /// a shrinking self-test still exits 0.
    /// </remarks>
    [Fact]
    public void TheRegisterAudit_ProvesEveryCheckCanFail()
    {
        var r = RunAudit("--self-test");
        Assert.Equal(0, r.ExitCode);
        Assert.Contains("ALL GREEN (every check proven able to fail)", r.Stdout, StringComparison.Ordinal);
        foreach (string mustDrive in new[]
                 {
                     "a wrong item number is caught",
                     "an item number outside A.1 is caught",
                     "an unnumbered row is caught",
                     "the OLD bare-number key form is caught",
                     "a DOC-A.1 token for an ITEM WITH NO ROW is caught",
                     "a DOC-A.1 token DUPLICATING an item that does have a row is caught",
                     "a test the row does not name is caught",
                     "an inventory row closing with no §7 row at all is caught",
                     // ⛔ The PB280 Q1 arm: a DECLINED item §7 DOES document still owes the `Pinned by`
                     // agreement. Keyed on the verdict rather than on the register, that case passes
                     // vacuously — a gate reading as working while inspecting nothing.
                     "a DECLINED item that §7 DOES document still owes the agreement",
                     "a citation of a nonexistent A.1 item is caught",
                     "a citation §7 does not discharge is caught",
                 })
        {
            Assert.Contains(mustDrive, r.Stdout, StringComparison.Ordinal);
        }

        // …and the controls, so the failures above cannot come from a checker that rejects everything.
        Assert.Contains("control: a correct row passes", r.Stdout, StringComparison.Ordinal);
        Assert.Contains("a discharged citation passes", r.Stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("SELF-TEST FAILED", r.Stdout, StringComparison.Ordinal);
    }
}
