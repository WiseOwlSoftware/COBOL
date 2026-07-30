// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// VERSION TEST MATRIX (docs/VERSION_TEST_MATRIX_DESIGN.md; Phase 1). Tests the compiler as N per-ISO-edition
/// compilers: a (construct × target-edition) matrix whose expected outcome is COMPUTED from each construct's
/// edition metadata — compiles iff <c>introducedIn ≤ V</c> and not removed by V (the design's <c>f(case, V)</c>).
///
/// The catalogue is the CANONICAL <c>tests/version-matrix/constructs.json</c> (design §10 #5 — the
/// VERSION_CHANGE_REFERENCE.md tables and the future in-code registry are renderings of that file; extend it,
/// never fork). Compilation goes through <see cref="EditionHarness"/> — the one per-edition compile path.
///
/// Covered today: INTRODUCTION-GATING both ways (INV-2) and CONTINUITY (INV-1, representative NIST rows per suite
/// family — the FULL sweep is <c>scripts/version-continuity-sweep.sh</c>). Not yet: removed-construct gating and
/// behavior variants (INV-3) — those await the EditionValidator (Phase 2); their rows join constructs.json with
/// <c>removedIn</c>/variant metadata as that lands.
/// </summary>
public sealed class VersionMatrixTests
{
    private sealed record Construct(string Id, string Description, int IntroducedIn, int? RemovedIn, string Vcr,
        string Source, string Status, string? ExpectDiagnostic, int? ObsoleteIn, string? ExpectDiagnosticBelow);

    private static readonly IReadOnlyList<Construct> Catalogue = LoadCatalogue();

    private static IReadOnlyList<Construct> LoadCatalogue()
    {
        string path = TestRepo.VersionMatrix("constructs.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var list = new List<Construct>();
        foreach (var e in doc.RootElement.GetProperty("constructs").EnumerateArray())
            list.Add(new Construct(
                e.GetProperty("id").GetString()!,
                e.GetProperty("description").GetString()!,
                e.GetProperty("introducedIn").GetInt32(),
                e.GetProperty("removedIn").ValueKind == JsonValueKind.Null ? null : e.GetProperty("removedIn").GetInt32(),
                e.GetProperty("vcr").GetString()!,
                e.GetProperty("source").GetString()!,
                e.TryGetProperty("status", out var s) ? s.GetString()! : "active",
                e.TryGetProperty("expectDiagnostic", out var d) ? d.GetString() : null,
                e.TryGetProperty("obsoleteIn", out var o) && o.ValueKind != JsonValueKind.Null ? o.GetInt32() : null,
                e.TryGetProperty("expectDiagnosticBelow", out var db) ? db.GetString() : null));
        return list;
    }

    /// <summary>The matrix expected-outcome function f(case, V) (design §2).</summary>
    private static bool ExpectCompiles(Construct c, int edition)
        => edition >= c.IntroducedIn && (c.RemovedIn is null || edition < c.RemovedIn);

    public static IEnumerable<object[]> Matrix()
    {
        // status:"pending" rows are catalogued (their edition metadata is frozen by the registry drift tests)
        // but not yet implemented — their compile assertions activate when the owning roadmap phase lands.
        foreach (var c in Catalogue.Where(c => c.Status == "active"))
            foreach (int v in EditionHarness.Editions)
                yield return [c.Id, v];
    }

    /// <summary>Every pending row must carry its activation contract: an owning-phase note in the description
    /// and edition metadata good enough to freeze — pending is a scheduling state, never a metadata hole.</summary>
    [Fact]
    public void PendingRows_AreCataloguedWithActivationContracts()
    {
        foreach (var c in Catalogue.Where(c => c.Status == "pending"))
        {
            Assert.Contains("PENDING", c.Description, StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(c.Vcr), $"{c.Id}: pending row without a vcr citation");
            Assert.False(string.IsNullOrWhiteSpace(c.Source), $"{c.Id}: pending row without a source program");
        }
        Assert.All(Catalogue, c => Assert.True(c.Status is "active" or "pending", $"{c.Id}: bad status '{c.Status}'"));
    }

    [Theory]
    [MemberData(nameof(Matrix))]
    public void Construct_MatchesEditionExpectation(string constructId, int edition)
    {
        var c = Catalogue.First(x => x.Id == constructId);
        bool expectCompiles = ExpectCompiles(c, edition);
        var (ok, diagnostics) = EditionHarness.Compile(c.Source, edition);

        if (expectCompiles)
            Assert.True(ok, $"[{constructId}] expected to COMPILE at COBOL-{edition} (introduced {c.IntroducedIn}"
                + $"{(c.RemovedIn is { } r ? $", removed {r}" : "")}; {c.Vcr}) but failed:\n{string.Join("\n", diagnostics)}");
        else
        {
            Assert.False(ok, $"[{constructId}] expected to be REJECTED at COBOL-{edition} (not valid until "
                + $"{c.IntroducedIn}{(c.RemovedIn is { } r ? $" / removed {r}" : "")}; {c.Vcr}) but it compiled.");
            // The two-obligation rule's diagnostic half: reject cells assert the QUALITY of the rejection
            // once their edition-band code exists (P2.7 — expectDiagnostic in constructs.json). A DUAL-window
            // row (introduced ≥2002 AND removed — e.g. EXIT METHOD, method WS) rejects with DIFFERENT codes at
            // the two edges: expectDiagnosticBelow (0900 not-yet-introduced) below introducedIn,
            // expectDiagnostic (0902 removed) at/after removedIn (the exit-method-window reactivation
            // contract, landed with OO slice 2).
            string? code = edition < c.IntroducedIn ? c.ExpectDiagnosticBelow ?? c.ExpectDiagnostic : c.ExpectDiagnostic;
            if (code is not null)
                EditionHarness.AssertHasDiagnostic(diagnostics, code);
        }
    }

    /// <summary>The introduction contract's SECOND axis (CA14): a construct the targeted edition has NOT YET
    /// acquired is an error under <c>--permissive</c> too, never a warning. <c>--permissive</c> is the §10 #1
    /// MIGRATION mode — it exists so a program written against an older edition still compiles against a newer
    /// one, which is meaningful only for constructs an edition REMOVED. No pre-existing program can legally
    /// contain a construct its own edition had not yet acquired, so there is nothing to migrate and nothing to
    /// be lenient about; <see cref="EditionSeverityPolicy"/> already says so
    /// (<c>For(NotYetIntroduced) =&gt; Error</c> on both axes).
    /// <para>⛔ THIS TEST IS THE REASON THE POLICY IS NOW AUTOMATIC. Until CA14 the permissive axis was tested
    /// ONLY for removal rows, so the ONE site that routed an introduction through the removed-severity seam
    /// (SYNCHRONIZED on a group item) accepted it with a warning under <c>--permissive</c> for two phases with
    /// nothing red. A new introduction row joins this theory the moment it enters constructs.json.</para></summary>
    public static IEnumerable<object[]> IntroducedMatrix()
    {
        foreach (var c in Catalogue.Where(c => c.Status == "active"))
            foreach (int v in EditionHarness.Editions.Where(v => v < c.IntroducedIn))
                yield return [c.Id, v];
    }

    [Theory]
    [MemberData(nameof(IntroducedMatrix))]
    public void IntroducedConstruct_IsRejectedUnderPermissive(string constructId, int edition)
    {
        var c = Catalogue.First(x => x.Id == constructId);
        var (ok, errors, warnings) = EditionHarness.CompileFull(c.Source, edition, permissive: true);
        Assert.False(ok, $"[{constructId}] is a COBOL-{c.IntroducedIn} introduction and must be REJECTED at "
            + $"COBOL-{edition} under --permissive as well as strict — permissive is the migration mode for "
            + $"REMOVED constructs, not a licence for future ones ({c.Vcr}). It compiled"
            + (warnings.Count > 0 ? $", warning only:\n{string.Join("\n", warnings)}" : " clean."));
        // Same code selection as the strict theory: a dual-window row rejects with its below-edge code.
        if ((c.ExpectDiagnosticBelow ?? c.ExpectDiagnostic) is { } code)
            EditionHarness.AssertHasDiagnostic(errors, code);
    }

    /// <summary>The §10 #1 migration contract (P2.7): a construct the targeted edition REMOVED must COMPILE
    /// under <c>--permissive</c> — with its edition-band diagnostic carried as a WARNING — preserving the
    /// pre-removal semantics. One theory row per (removed-construct × edition ≥ removedIn).</summary>
    public static IEnumerable<object[]> RemovedMatrix()
    {
        foreach (var c in Catalogue.Where(c => c.Status == "active" && c.RemovedIn is not null))
            foreach (int v in EditionHarness.Editions.Where(v => v >= c.RemovedIn))
                yield return [c.Id, v];
    }

    [Theory]
    [MemberData(nameof(RemovedMatrix))]
    public void RemovedConstruct_CompilesPermissive_WithWarning(string constructId, int edition)
    {
        var c = Catalogue.First(x => x.Id == constructId);
        var (ok, errors, warnings) = EditionHarness.CompileFull(c.Source, edition, permissive: true);
        Assert.True(ok, $"[{constructId}] permissive at COBOL-{edition} must COMPILE (the §10 #1 migration "
            + $"contract): {string.Join("\n", errors)}");
        if (c.ExpectDiagnostic is { } code)
            EditionHarness.AssertHasDiagnostic(warnings, code);
    }

    /// <summary>The archaic/obsolete flag contract (P2.6, ISO §4.2.12/§4.2.13): an <c>obsoleteIn</c> row
    /// COMPILES throughout its availability window (the element remains conforming) and carries the FIXED
    /// 0903 band WARNING exactly at editions ≥ obsoleteIn — never below (no NIST-85 noise). A DUAL row
    /// (obsolete then later REMOVED — QUOTE→numeric: obsolete 2014 per Annex E.2 item 21, removed 2023) is
    /// bounded below its <c>removedIn</c>; the removal edge is the ValidAt/RemovedMatrix theories' job and
    /// carries the row's <c>expectDiagnostic</c> (0902), while the obsolete verdict's code is ALWAYS 0903
    /// (<c>ConstructRegistry.Check</c> emits <c>EditionCodes.ObsoleteFlag</c> for every Obsolete verdict).</summary>
    public static IEnumerable<object[]> ObsoleteMatrix()
    {
        foreach (var c in Catalogue.Where(c => c.Status == "active" && c.ObsoleteIn is not null))
            foreach (int v in EditionHarness.Editions.Where(v => v >= c.IntroducedIn && v < (c.RemovedIn ?? int.MaxValue)))
                yield return [c.Id, v];
    }

    [Theory]
    [MemberData(nameof(ObsoleteMatrix))]
    public void ObsoleteConstruct_CompilesEverywhere_WarnsFromObsoleteEdition(string constructId, int edition)
    {
        var c = Catalogue.First(x => x.Id == constructId);
        var (ok, errors, warnings) = EditionHarness.CompileFull(c.Source, edition);
        Assert.True(ok, $"[{constructId}] must COMPILE at COBOL-{edition} (archaic ≠ removed): {string.Join("\n", errors)}");
        if (edition >= c.ObsoleteIn)
            EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET0903");
        else
            EditionHarness.AssertNoDiagnostic(warnings, "COBOLNET0903");
    }

    /// <summary>INV-1 (continuity), RESTATED at the P2.7 flip (the §10 #1 migration posture) and PROMOTED at rearch
    /// P3 step 8 from a 13-row seed to the FULL in-process sweep — the authoritative gate, replacing the out-of-band
    /// bash sweep (kept only as a CLI convenience). Runs over the corpus witness set (every green∪divergent program
    /// compiles at 85 by definition) × {2002,2014,2023} via the SAME <c>[Theory][MemberData]</c> mechanism as
    /// <see cref="NistProgram_MatchesGolden"/>. A program that compiles at 85 must STILL compile at each later
    /// edition **UNDER PERMISSIVE** — a permissive break is a regression. Under STRICT a rejection is legitimate
    /// (the removed '85 elements every NIST program carries — LABEL RECORDS in every FD, …) but must trace to a
    /// recognized edition-band code (COBOLNET08xx/09xx), never a generic parse error. Uses <c>CheckOnly</c> (parse +
    /// edition-validate + bind, no Roslyn backend — the verdict is settled pre-backend, DEVLOG 627); xUnit runs the
    /// cells in parallel.</summary>
    [Theory]
    [MemberData(nameof(ContinuityCells))]
    public void Cobol85Program_StillCompilesAtLaterEdition(string testName, int edition)
    {
        // Permissive: the §10 #1 migration contract — a documented removal warns, never breaks.
        var (permOk, permDiag) = EditionHarness.CompileNist(testName, edition, permissive: true, checkOnly: true);
        Assert.True(permOk, $"[INV-1 continuity] {testName} (compiles at 85) failed PERMISSIVE at COBOL-{edition}; "
            + $"permissive must accept documented removals with warnings (§10 #1) — this is a regression:\n"
            + string.Join("\n", permDiag));

        // Strict: a rejection is legitimate, but a rejection with NO edition-band code (a generic COBOL0001 / a
        // non-edition bind error) is the co-equal-diagnostic violation.
        var (strictOk, strictDiag) = EditionHarness.CompileNist(testName, edition, permissive: false, checkOnly: true);
        if (!strictOk)
            Assert.True(strictDiag.Any(d => EditionBandCode.IsMatch(d)),
                $"[INV-1 diagnosis] {testName} strict@COBOL-{edition} was rejected WITHOUT a recognized edition-band "
                + "code (COBOLNET08xx/09xx):\n" + string.Join("\n", strictDiag));
    }

    /// <summary>The continuity witness set × later editions: every corpus green∪divergent program crossed with
    /// 2002/2014/2023.</summary>
    public static IEnumerable<object[]> ContinuityCells() =>
        from name in CorpusManifest.Green().Select(r => r.Name)
        from edition in new[] { 2002, 2014, 2023 }
        select new object[] { name, edition };

    /// <summary>A recognized edition diagnostic (the 0900–0903 band or any pinned 08xx gate, plus the 15xx
    /// conformance-requirement band) — as opposed to a generic <c>COBOL0001</c> parse error. The point is "a COBOLNET
    /// code diagnosed the edition delta." The 15xx band is included because a version-conditioned structural SR read
    /// directly in the binder (the CheckDigitCapacity/binder-reads-edition doctrine — e.g. VCR 18/31's external-file
    /// FILE-STATUS/RELATIVE-KEY consistency, COBOLNET1573/1575) reports in that band, not 08xx/09xx: for a CONTINUITY
    /// witness (green at 85 by construction), ANY 15xx ERROR surfacing only at a later edition is necessarily
    /// edition-conditioned — an edition-invariant 15xx SR would have errored at 85 too and excluded the program from
    /// the green set — so it is a legitimate recognized-edition rejection, never a generic parse error.</summary>
    private static readonly System.Text.RegularExpressions.Regex EditionBandCode =
        new(@"COBOLNET(0[89]|15)\d\d", System.Text.RegularExpressions.RegexOptions.Compiled);
    /// <summary>The ST representative is a DOCUMENTED removal, not a continuity witness: NIST programs carry
    /// obsolete '85 elements DELETED by ISO/IEC 1989:2002 — ST101A's FDs write LABEL RECORDS (0902, the
    /// validator's first gate, DEVLOG 588) and its SD writes DATA RECORDS (0873, still binder-side) — so it
    /// must compile at 85 and REJECT at 2002+ STRICT with a recognized edition-band code. The validator
    /// fail-fasts BEFORE Emit, so today only the 0902s surface; once P2.6 migrates the SD 0873 gate into the
    /// validator (one enforcement site), BOTH appear in the one pass — re-assert 0873 then. Under PERMISSIVE
    /// it must COMPILE (the removals warn), which the continuity theory above already witnesses.</summary>
    [Fact]
    public void St101A_DocumentedRemovals_RejectStrictAt2002Plus()
    {
        var (ok85, _) = EditionHarness.CompileNist("ST101A", 85);
        Assert.True(ok85, "ST101A must compile at --std 85");
        var (ok, diagnostics) = EditionHarness.CompileNist("ST101A", 2023);
        Assert.False(ok, "ST101A's removed '85 elements must be rejected at COBOL-2023 strict");
        Assert.Contains(diagnostics, d => d.Contains("COBOLNET0902"));   // the FD LABEL RECORDS gate
        // Both removed elements surface in the ONE validator pass now that the SD DATA RECORDS 0873 gate
        // migrated validator-side (P2.6 / Table-7 row 7.1 — the fail-fast no longer hides it behind Emit).
        Assert.Contains(diagnostics, d => d.Contains("COBOLNET0873"));
    }

}
