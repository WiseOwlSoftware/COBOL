# PHASE 00 — Migration Safety Net

- **Phase:** P0
- **Title:** Migration safety net — characterization harness, oracle bake-out, corpus consolidation, ref caching
- **Track:** foundation
- **Risk:** LOW (net/test/doc changes + one behavior-neutral production perf edit; no god-class splits, no rename, no semantic change)
- **Depends on:** none (this is the first rearchitecture phase; it de-risks every later one)

## STATUS
`IN PROGRESS @ step 12` — steps 1–11 DONE. Baseline green (2028 conformance · 213 unit · guard 353 MATCH). Characterization
net complete (gates 2+3, 32 tests). Step 7: `tests/nist/corpus.tsv` generated mechanically — 459 rows (338 green + 11
divergent + 110 pending; folds `[InlineData]` 349 + `chains.tsv` + `LEGACY_DIVERGENT`) + `CorpusManifest` loader + a
5-assertion drift guard (green∪divergent == the committed 349-name baseline; every green/divergent has a golden; every
divergent cites §; no dupes; every on-disk program listed) — all green. Step 8: `NistDifferentialTests` repointed at
`[MemberData(CorpusManifest.GreenData)]` + `CorpusManifest.Chains` (487-line `[InlineData]` block + the private `Chains`
lazy deleted) — **349/349 pass, verdict-equivalent**. Step 9: `DifferentialGolden` helper (bake/verify/golden modes;
content-hash goldens under `tests/differential/<TestClass>/<hash>.out`; edition+source hash so many `[InlineData]`
sources per funnel never collide) — builds clean. Step 10: 46-file conversion (parallel workflow, one agent/file, 0
errors) — 42 funnels routed through `DifferentialGolden.Assert` + 360 goldens baked under `tests/differential/<Class>/`;
4 files correctly untouched (MoveEdition/SignedAlphanumericMove/AllLiteral/AbbreviatedCondition — spec-pinned-only, no
`lout==cout` funnel); fields kept where a spec-pinned funnel still uses them (ClassCondition/ControlFlow/…). Bake 644/0,
golden-mode 644/0 (no legacy). Step 11: `DifferentialGoldenDriftTests` (folder-level orphan guard — every converted
class has a non-empty golden folder; every golden folder maps to a live converted class; 3/3 green) + completeness
sweep: a fresh whole-suite re-bake left `git status tests/differential` CLEAN → the 360 goldens are complete and
idempotent (byte-identical regen). `chains.tsv` KEPT (bash guard/sweep still read it). Step 2 ref-cache ~40–55% faster.
Finding for PHASE-02: undefined-data-name + JUSTIFIED-on-numeric not caught at bind time.
Step 1 (migration-SSOT banner) DONE: the migration SSOT is `docs/COBOLNET_REARCHITECTURE_PLAN.md` (the master plan —
STATE banner + P0–P16 index + exit criteria + the resume-vs-migration pointer); its banner is flipped to "P0 IN
PROGRESS" and `docs/DOC_INDEX.md` carries its row.
<!-- The executing session updates this line: NOT STARTED  |  IN PROGRESS @ step N  |  DONE.
     Keep a one-line note of the last green commit hash when you pause. -->

---

## Goal (one paragraph)
Stand up the neutrality net so every later rearchitecture phase can *prove* it changed no observable behavior, and
bake the frozen legacy differential oracle into committed goldens so the differential net survives the eventual G8
deletion of the legacy engine. Concretely: (1) a new `Cobol.Net.Tests.Characterization` project that snapshots the
current emitter's generated C# and diagnostic surface for a curated corpus (gates 2 & 3), seeded ONCE from today's
compiler; (2) a one-time bake of the ~60 legacy-oracle differential tests into committed `tests/differential/**/*.out`
goldens, with the tests rewritten to golden comparison and the live legacy comparison retained as an opt-in CI
cross-check; (3) consolidation of the three "which NIST programs are green" lists into one `tests/nist/corpus.tsv`
(folding `chains.tsv`) with a drift test; (4) caching the Roslyn reference set (the single highest-leverage battery
speedup). No compiler behavior changes. The full battery stays green at every commit boundary.

## Exit criteria (all must hold)
1. Full battery green (see §5): Unit + Conformance + the new Characterization project + the legacy guard (`guard-fast.sh` NIST 353 MATCH).
2. `tests/Cobol.Net.Tests.Characterization` exists, is in the solution + CI, and its `Snapshots/` goldens are committed and seeded from the pre-refactor emitter.
3. `tests/differential/**/*.out` baked goldens are committed for every currently-GREEN differential case; the `*DifferentialTests` assert against goldens (legacy comparison retained only under an opt-in env/CI job).
4. `tests/nist/corpus.tsv` exists (folds `chains.tsv` + the green/divergent/pending sets); `CorpusManifestTests` drift test is green; `NistDifferentialTests` is driven by `[MemberData]` over it (no hand-maintained `[InlineData]` green list).
5. `RoslynBackend.ReferenceAssemblies()` is cached in a `static Lazy<ImmutableArray<MetadataReference>>`; a measurable battery speedup is observed and noted in the DEVLOG.
6. The resumable migration SSOT — `docs/COBOLNET_REARCHITECTURE_PLAN.md` (the master plan) — carries a STATE banner reflecting the current phase; `docs/DOC_INDEX.md` has its row and it indexes the sibling `DESIGN-*.md` / `PHASE-*.md` set.
7. No behavior-changing production-source edit landed. The ONLY `src/` change is the ref-cache (perf-only, behavior-neutral, proven by the battery).

---

## 1. Rationale — the problems this phase fixes
Grounded in `docs/rearchitecture/DESIGN-test-build-ci.md` §1–§2 and the as-built code:

1. **The net evaporates at G8.** ~60 `*DifferentialTests.cs` assert `cobolnet == legacy` at RUN time via
   `CompilerUnderTest.cs` (`ICompilerUnderTest` / `LegacyCompiler` drives `CobolSharp.Compiler.Compilation`; the
   `ProjectReference` to `src/CobolSharp.Compiler` in `Cobol.Net.Tests.Conformance.csproj:34` is "Deleted at the G8
   cut-over"). When the legacy engine is deleted, every one of those tests loses its oracle and the net collapses
   mid-migration. (DESIGN §1.2, §2.1.)
2. **No behavior-neutrality proof for a refactor.** A phase that splits `StatementBinder`/`CSharpEmitter` or unifies
   `StoreAsImage` has NO characterization gate today: only the subset of programs in the golden corpus is checked, and
   nothing snapshots the generated C# to catch an unintended emit change. (DESIGN §2.2.)
3. **"Which programs are green" has three sources of truth** that can silently diverge:
   `scripts/guard.sh:89` `NIST_TESTS` (legacy loop), `NistDifferentialTests.cs` (349 hand-maintained `[InlineData]`
   rows — verified count 349, DESIGN said ~318), and `tests/nist/chains.tsv`. (DESIGN §1.2, §2.3.)
4. **Roslyn reference set is uncached.** `RoslynBackend.ReferenceAssemblies()` (`RoslynBackend.cs:73`) does
   ~180 `MetadataReference.CreateFromFile` on EVERY compile; the in-process battery compiles thousands of times per
   run. This is the single highest-leverage throughput fix, and it matters more once the battery runs more (DESIGN §2.8, §3.6).
5. **No resumable migration SSOT.** `resume-prompt.md` is the feature-drive state; there is no compact, phase-structured,
   exit-criteria-bearing rearchitecture roadmap a future engineer resumes the MIGRATION from. (DESIGN §2.9, §3.9.)

Out of scope here (deferred to later phases, per the P0 brief): the diagnostic-code registry build-out (P2/its own
dimension), the Bind-phase boundary extraction, the namespace rename, any god-class split, the CI rewrite to an
OS-matrix greenfield-authoritative gate (P0 only ADDS the characterization job + an optional legacy-oracle cross-check
job; it does not delete the legacy guard).

---

## 2. Target end-state for this phase (concrete)
When P0 is DONE the repository contains:

**New test project** `tests/Cobol.Net.Tests.Characterization/`:
- `Cobol.Net.Tests.Characterization.csproj` — references `Cobol.Net.Compiler` + `Cobol.Net.Runtime` ONLY (NOT the legacy projects; the probe drives the greenfield `CompilerDriver` only — DESIGN §6 risk 3).
- `CharacterizationCorpus.cs` — corpus discovery + the `ICompilerProbe` over `CompilerDriver` (bind+emit via the public API; reads the `.g.cs` sidecar for the emitted C#).
- `DiagnosticSnapshotTests.cs` — gate (2): snapshots each program's diagnostic list.
- `EmittedCSharpSnapshotTests.cs` — gate (3): snapshots each positive program's generated C#.
- `Snapshots/*.diag.txt` and `Snapshots/*.g.cs.txt` — committed goldens, seeded from today's emitter.

**New corpus tree** `tests/characterization/`:
- `positive/*.cob` (+ optional `<name>.std` edition sidecar), `negative/*.cob`, `README.md`.

**Corpus consolidation:**
- `tests/nist/corpus.tsv` — the ONE green-NIST manifest (folds `chains.tsv`).
- `tests/Cobol.Net.Tests.Conformance/CorpusManifest.cs` — loader (name/suite/status/chain-preds/golden/note).
- `tests/Cobol.Net.Tests.Conformance/CorpusManifestTests.cs` — drift test.
- `NistDifferentialTests.cs` — rewired to `[MemberData]` over `CorpusManifest`; the 349 `[InlineData]` rows removed.
- `tests/nist/chains.tsv` — KEPT until the manifest-driven run is proven equivalent, then its content lives only in `corpus.tsv` (the file may be retained as a generated view or deleted — see step 8).

**Oracle bake-out:**
- `tests/Cobol.Net.Tests.Conformance/DifferentialGolden.cs` — the 3-mode (`golden`/`bake`/`verify`) assert helper.
- `tests/differential/<TestClass>/<hash>.out` — committed baked goldens.
- ~60 `*DifferentialTests.cs` — each converted to call `DifferentialGolden.Assert(...)` instead of `new LegacyCompiler()` / `AssertSameAsLegacy`.

**Production (perf-only, behavior-neutral):**
- `src/Cobol.Net.Compiler/CodeGen/RoslynBackend.cs` — `ReferenceAssemblies()` cached in a `static Lazy<ImmutableArray<MetadataReference>>`.

**Docs / solution / CI:**
- `docs/COBOLNET_REARCHITECTURE_PLAN.md` (the migration SSOT + roadmap): its STATE banner is flipped to P0-IN-PROGRESS.
- `docs/DOC_INDEX.md` (its row already exists).
- `CobolSharp.sln` — the Characterization project added.
- `.github/workflows/build-and-test.yml` — a `characterization` step added to the greenfield job; an opt-in `legacy-oracle` bake-verify job added (does not replace the existing `guard` job).

---

## 3. STEP-BY-STEP

> Conventions: run every command from the repo root `E:/CobolSharp`. The Bash tool runs Git Bash (POSIX); the
> PowerShell tool runs pwsh. `dotnet` commands are shell-agnostic. Keep the battery green at every **COMMIT
> BOUNDARY**. Commit messages end with the standard Co-Authored-By / Claude-Session trailers (see repo git config).
> Each commit needs a DEVLOG entry (newest-first, real timestamp) per `feedback_devlog`.

### Step 1 — Migration-SSOT banner + DOC_INDEX rows (docs only)  ★ COMMIT BOUNDARY — ✅ DONE
The resumable migration SSOT is `docs/COBOLNET_REARCHITECTURE_PLAN.md` (the master plan) — it carries the top STATE
banner, the ordered P0–P16 phase index, per-phase exit criteria, the owner-decisions table, and the `resume-prompt.md`
(feature-drive) vs migration-state pointer.
**Files:** `docs/COBOLNET_REARCHITECTURE_PLAN.md` (flip its STATE banner to `PHASE 00 — IN PROGRESS`); `docs/DOC_INDEX.md`
(its LIVE row already exists and indexes the `docs/rearchitecture/` set).
**Why:** gives the phase work a home and makes the migration resumable (DESIGN §3.9).
**Verify:** `grep -q "PHASE 00 .*IN PROGRESS" docs/COBOLNET_REARCHITECTURE_PLAN.md && echo OK`. No build impact.
**Commit:** folded into the P0 steps 1–2 commit (DEVLOG 666).

### Step 2 — Cache the Roslyn reference set (the one production edit)  ★ COMMIT BOUNDARY
**File:** `src/Cobol.Net.Compiler/CodeGen/RoslynBackend.cs`.
**Change:** convert `ReferenceAssemblies()` from a per-call rebuild to a process-lifetime cache. Add
`using System.Collections.Immutable;` and:
```csharp
private static readonly Lazy<ImmutableArray<MetadataReference>> _referenceAssemblies =
    new(BuildReferenceAssemblies, LazyThreadSafetyMode.ExecutionAndPublication);

private static ImmutableArray<MetadataReference> ReferenceAssemblies() => _referenceAssemblies.Value;

private static ImmutableArray<MetadataReference> BuildReferenceAssemblies()
{
    // (existing body — split TPA .dlls into MetadataReference; add the runtime ref if present)
    ...
    return [.. refs];
}
```
The runtime DLL path (`RuntimePath`) is stable per process and exists by test time (deployed at build), so caching the
"add if `File.Exists`" result once is correct.
**Why:** the battery about to grow (characterization + more NIST theory rows) pays ~180 `CreateFromFile` per compile;
cache once (DESIGN §2.8/§3.6). Behavior-neutral: identical reference set, computed once.
**Verify (behavior-neutral + speedup):**
```
dotnet build src/Cobol.Net.Compiler/Cobol.Net.Compiler.csproj -c Debug
dotnet test tests/Cobol.Net.Tests.Conformance --filter "FullyQualifiedName~NistDifferentialTests" -c Debug
```
Expect: all green (unchanged verdicts). Time the Conformance run before/after (record wall-clock in the DEVLOG note —
expect a measurable drop). Then confirm the greenfield battery + legacy guard still green (§5).
**Commit:** `perf(cobolnet): cache Roslyn ReferenceAssemblies() in a static Lazy — behavior-neutral (P0 step 2)`.

### Step 3 — Create the Characterization project skeleton + wire it in  ★ COMMIT BOUNDARY
**Files:** create `tests/Cobol.Net.Tests.Characterization/Cobol.Net.Tests.Characterization.csproj`; edit `CobolSharp.sln`.
**Change:** mirror `tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj` (same package refs) but reference ONLY:
```xml
<ProjectReference Include="..\..\src\Cobol.Net.Compiler\Cobol.Net.Compiler.csproj" />
<ProjectReference Include="..\..\src\Cobol.Net.Runtime\Cobol.Net.Runtime.csproj" />
```
(NO legacy projects — the probe drives the greenfield only.) Add the project to `CobolSharp.sln`
(`dotnet sln CobolSharp.sln add tests/Cobol.Net.Tests.Characterization/Cobol.Net.Tests.Characterization.csproj`).
Add one build-anchoring smoke `[Fact]` so the project is real and non-empty.
**Why:** the harness needs a home in the build/solution before the tests land.
**Verify:** `dotnet test tests/Cobol.Net.Tests.Characterization -c Debug` → the smoke fact passes;
`dotnet build CobolSharp.sln -c Debug` succeeds.
**Commit:** `test(cobolnet): add Cobol.Net.Tests.Characterization project skeleton (P0 step 3)`.

### Step 4 — The `ICompilerProbe` + characterization corpus discovery
**File:** `tests/Cobol.Net.Tests.Characterization/CharacterizationCorpus.cs`.
**Change:** implement a probe over the PUBLIC `CompilerDriver` API only (no production seam needed):
```csharp
public sealed record ProbeResult(bool Ok, IReadOnlyList<string> Diagnostics, string? EmittedCSharp);

public static class CompilerProbe
{
    // Full compile into an isolated temp dir; capture Errors+Warnings (diagnostics) and the emitted .g.cs sidecar.
    public static ProbeResult Probe(string sourcePath, int edition, bool emit)
    {
        string dir = /* fresh temp dir */;
        string dll = Path.Combine(dir, "p.dll");
        var r = CompilerDriver.Compile(new CompilerDriver.Options(
            sourcePath, dll, DialectLevel: edition, CheckOnly: !emit));
        var diags = r.Errors.Concat(r.Warnings).ToList();   // deterministic order: errors then warnings
        string? cs = emit && r.GeneratedCsPath is { } p && File.Exists(p) ? File.ReadAllText(p) : null;
        return new ProbeResult(r.Success, diags, cs);
    }
}
```
- Corpus discovery: enumerate `tests/characterization/positive/*.cob` and `negative/*.cob`; per file read an optional
  `<name>.std` sidecar (else default 2023 for positive-new-feature, 85 for core — pick a documented default and record
  it in the corpus README).
- Deterministic diagnostic formatting helper `FormatDiagnostics(IReadOnlyList<string>)` → sorted-stable joined text
  (the current diagnostics are bare strings; P0 snapshots them AS-IS — the structured registry is a later phase).
- Snapshot IO helper: `AssertSnapshot(string snapshotFile, string actual)` that, when
  `COBOLNET_UPDATE_SNAPSHOTS=1`, writes the file (creating dirs) and passes; otherwise asserts
  `File.ReadAllText(snapshotFile).ReplaceLineEndings("\n") == actual.ReplaceLineEndings("\n")`.
**Why:** one probe + one snapshot mechanism both snapshot tests share; uses only public API so P0 needs no production
change (DESIGN §3.3). CheckOnly gives diagnostics without Roslyn; a full compile writes the `.g.cs` the emit snapshot reads.
**Verify:** builds; no test asserts yet (covered by steps 5–6). `dotnet build tests/Cobol.Net.Tests.Characterization`.
(Not a commit boundary on its own — bundle with step 5.)

### Step 5 — Seed the characterization corpus + diagnostic snapshots (gate 2)  ★ COMMIT BOUNDARY
**Files:** create `tests/characterization/positive/*.cob`, `tests/characterization/negative/*.cob`,
`tests/characterization/README.md`, `tests/Cobol.Net.Tests.Characterization/DiagnosticSnapshotTests.cs`, and the
committed `Snapshots/*.diag.txt`.
**Change:**
- **Seed the corpus (curated, small, one-per-family — DESIGN §3.3 / §6 risk 1):** copy the already-curated small
  programs from `tests/conformance/{2002,2014,2023}/*.cob` (positive) and `tests/conformance/negative/*.cob` (negative)
  into `tests/characterization/`, AND add ~12 authored compact 85-core programs covering the families the conformance
  corpus under-samples: MOVE (elementary/group/edited/figurative), arithmetic + ON SIZE ERROR, IF/EVALUATE,
  PERFORM (inline/out-of-line/VARYING), OCCURS + subscript/index, REDEFINES (Tier-A/B), RENAMES, sequential file I/O,
  CALL/LINKAGE, INSPECT/STRING/UNSTRING, INITIALIZE, intrinsic FUNCTION. Keep each program tiny (deterministic emit,
  legible diff). Give each an appropriate `.std` sidecar. Record the family list in the README.
- `DiagnosticSnapshotTests` — a `[Theory]` over every corpus program (positive + negative): probe with `emit:false`,
  format diagnostics, assert against `Snapshots/<name>.<edition>.diag.txt`.
- **Seed the snapshots ONCE from today's compiler:** run with `COBOLNET_UPDATE_SNAPSHOTS=1` to WRITE the `.diag.txt`
  files, then commit them. This captures "how the god-class diagnoses today" (the behavior-neutrality baseline).
**Why:** gate (2) — the diagnostic surface a later refactor must not change (DESIGN §3.0/§3.3).
**Verify:**
```
COBOLNET_UPDATE_SNAPSHOTS=1 dotnet test tests/Cobol.Net.Tests.Characterization --filter "FullyQualifiedName~DiagnosticSnapshotTests"
# review the written Snapshots/*.diag.txt in the diff, then re-run WITHOUT the env var:
dotnet test tests/Cobol.Net.Tests.Characterization --filter "FullyQualifiedName~DiagnosticSnapshotTests"
```
Expect: the second (compare) run is green against the committed snapshots.
**Commit:** `test(cobolnet): characterization corpus + diagnostic snapshots seeded from today's emitter (P0 step 5)`.

### Step 6 — Emitted-C# snapshots (gate 3)  ★ COMMIT BOUNDARY
**Files:** create `tests/Cobol.Net.Tests.Characterization/EmittedCSharpSnapshotTests.cs`; committed `Snapshots/*.g.cs.txt`.
**Change:** a `[Theory]` over every `positive/*.cob`: probe with `emit:true`, assert the emitted C# (line-ending
normalized) against `Snapshots/<name>.<edition>.g.cs.txt`. (Negative programs emit no C# — skip them here; they are
covered by gate 2.) Seed the `.g.cs.txt` snapshots ONCE with `COBOLNET_UPDATE_SNAPSHOTS=1`, review, commit.
**Why:** gate (3) — advisory/reviewed proof that a pure-reorg refactor left emission byte-identical; an intentional
emit change re-baselines with review while gate (1) proves it behavior-preserving (DESIGN §3.0 #3, §6 risk 1).
**Verify:**
```
COBOLNET_UPDATE_SNAPSHOTS=1 dotnet test tests/Cobol.Net.Tests.Characterization --filter "FullyQualifiedName~EmittedCSharpSnapshotTests"
dotnet test tests/Cobol.Net.Tests.Characterization --filter "FullyQualifiedName~EmittedCSharpSnapshotTests"   # compare run green
```
**Commit:** `test(cobolnet): emitted-C# characterization snapshots (gate 3) seeded from today's emitter (P0 step 6)`.

### Step 7 — Consolidate the green-NIST corpus into `tests/nist/corpus.tsv`  ★ COMMIT BOUNDARY
**Files:** create `tests/nist/corpus.tsv`, `tests/Cobol.Net.Tests.Conformance/CorpusManifest.cs`,
`tests/Cobol.Net.Tests.Conformance/CorpusManifestTests.cs`.
**Change:** author `corpus.tsv` (tab-separated; `#` comments) with one row per program:
```
# name   suite   status(green|divergent|pending)   chain-preds(space-joined or '-')   golden(valid|none)   note
NC101A   NC      green      -                 valid   first NC program
ST103A   ST      green      ST101A ST102A     valid   SORT USING/GIVING chain
IX111A   IX      divergent  -                 valid   ISO §14.9.49.4 GR3a — legacy non-conforming (LEGACY_DIVERGENT)
```
Populate it MECHANICALLY, not by hand, to guarantee it reproduces today's verdicts:
- `green` rows = the 349 distinct `[InlineData]` names currently in `NistDifferentialTests.cs`.
- `chain-preds` = each name's predecessors from `tests/nist/chains.tsv` (fold it in verbatim).
- `divergent` = the 11 names in `scripts/guard.sh:145` `LEGACY_DIVERGENT` (with an ISO `§` citation note each — reuse
  the citations already inline in `NistDifferentialTests.cs`/`guard.sh`). A `divergent` row is still a `green`-style
  golden assertion in the greenfield harness (COBOL.NET already matches the ISO golden); the flag only records that a
  live LEGACY run would differ.
- `pending` = every `tests/nist/programs/*.cob` (459 on disk) not covered above and lacking a green golden — catalogued,
  not asserted.
- `golden` = `valid` iff `tests/nist/valid/<name>.txt` exists, else `none`.

`CorpusManifest.cs` — a loader returning `IReadOnlyList<CorpusRow>` (record `CorpusRow(string Name, string Suite,
string Status, string[] ChainPreds, bool HasGolden, string Note)`), plus a `Chains` view
(`name → preds`) that replaces the private `Chains` lazy in `NistDifferentialTests`.
`CorpusManifestTests.cs` — assert: every `tests/nist/programs/*.cob` is listed; every `green`/`divergent` row has a
`valid/<name>.txt`; every `divergent` row's note contains `§`; no name appears twice; the `green∪divergent` set equals
today's `[InlineData]` set (pin this against a committed snapshot of the 349 names so the fold is provably lossless).
**Why:** kills the 3-way triplication (DESIGN §3.2); makes "add a green program" a one-line manifest edit.
**Verify:** `dotnet test tests/Cobol.Net.Tests.Conformance --filter "FullyQualifiedName~CorpusManifestTests"` → green.
**Commit:** `test(cobolnet): consolidate green-NIST + chains into tests/nist/corpus.tsv + drift test (P0 step 7)`.

### Step 8 — Repoint `NistDifferentialTests` at the manifest  ★ COMMIT BOUNDARY
**File:** `tests/Cobol.Net.Tests.Conformance/NistDifferentialTests.cs`.
**Change:** replace the 349 `[InlineData]` rows with `[MemberData]` over
`CorpusManifest.Green()` (green ∪ divergent). Replace the private `Chains` lazy with
`CorpusManifest.Chains`. `RunNist` logic is unchanged (chain predecessors, X-card preprocessing, print-file discovery,
`NistStd`/`NistPermissive` overrides all stay). Keep the `Normalize`/`RepoRoot` helpers.
**Why:** single source of truth; a green program is a manifest row, not a code edit (DESIGN §3.2).
**Verify (equivalence — DESIGN §6 risk 4):** the run must reproduce the SAME per-program verdicts as before. Compare
the passing test list to the pre-change list:
```
dotnet test tests/Cobol.Net.Tests.Conformance --filter "FullyQualifiedName~NistDifferentialTests" --logger "trx;LogFileName=after.trx"
# assert: same set of PASSED display names as the pre-step run (349), zero new failures.
```
**Commit:** `test(cobolnet): drive NistDifferentialTests via [MemberData] over corpus.tsv (P0 step 8)`.
> After this commit, `tests/nist/chains.tsv` is no longer read by the harness. KEEP the file for now (the bash guard
> and off-repo sweep still read it) — its retirement is a G8 concern. Note this in the DEVLOG.

### Step 9 — The differential golden helper (bake / verify / golden modes)  ★ COMMIT BOUNDARY
**File:** create `tests/Cobol.Net.Tests.Conformance/DifferentialGolden.cs`.
**Change:** one helper that all differential tests funnel through. Mode from env `COBOLNET_DIFF_MODE`
(`golden` default | `bake` | `verify`):
```csharp
public static class DifferentialGolden
{
    private static readonly string Mode =
        Environment.GetEnvironmentVariable("COBOLNET_DIFF_MODE") ?? "golden";
    private static readonly LegacyCompiler Legacy = new();

    // goldenName defaults to a stable SHA1(source)[..16]; the folder is derived from the caller's file name.
    public static void Assert(string source, int edition = 85,
        [CallerFilePath] string file = "", string? goldenName = null)
    {
        string path = GoldenPath(file, goldenName ?? Hash(source));
        var (cok, cout, cdetail) = new CobolNetCompiler(edition).CompileAndRun(source);
        Xunit.Assert.True(cok, $"COBOL.NET failed: {cdetail}");

        if (Mode is "bake" or "verify")
        {
            var (lok, lout, ldetail) = Legacy.CompileAndRun(source);
            Xunit.Assert.True(lok, $"legacy oracle failed: {ldetail}");
            Xunit.Assert.Equal(lout, cout);                       // the historic cobolnet==legacy assert
            if (Mode == "bake") { Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, cout); return; }
        }
        Xunit.Assert.Equal(File.ReadAllText(path).ReplaceLineEndings("\n"), cout.ReplaceLineEndings("\n")); // cobolnet==golden
    }
}
```
- `golden` (default / CI normal): assert `cobolnet == committed golden` (no legacy run — survives G8).
- `bake` (one-time, local): assert `cobolnet == legacy` (so only GREEN cases bake — DESIGN §6 risk 2), then WRITE the golden.
- `verify` (opt-in `legacy-oracle` CI job): assert `cobolnet == legacy` AND `cobolnet == golden` (proves the goldens still match a live legacy run).
- `GoldenPath` → `tests/differential/<TestClassFromFileName>/<hash>.out`; content-hash naming needs no per-case scheme.
**Why:** severs the run-time legacy coupling for the normal battery while keeping legacy as an opt-in cross-check —
exactly the "keep legacy running, NOT deleted" P0 posture (DESIGN §3.4, §5 R1).
**Verify:** builds. (Exercised in steps 10–11.)
**Commit:** `test(cobolnet): add DifferentialGolden bake/verify/golden helper (P0 step 9)`.

### Step 10 — Convert the differential tests to the helper, in batches  ★ COMMIT BOUNDARY (per batch)
**Files:** the ~60 `*DifferentialTests.cs` that construct `new LegacyCompiler()` / call `AssertSameAsLegacy`
(enumerate exactly with: `grep -rl "new LegacyCompiler\|AssertSameAsLegacy\|new CobolNetCompiler" tests/Cobol.Net.Tests.Conformance/*.cs`).
Exclude the spec-pinned tests that compare to HAND-AUTHORED expected strings (e.g. `WideNumericTests`,
`NationalBoolean*`, `Typedef*`, `StandardDecimalTests`, `SpecPinnedNistTests`) — they are already self-standing goldens
and must NOT be routed through the legacy oracle.
**Change (mechanical, per file):** replace the file's private funnel helper body so it delegates to
`DifferentialGolden.Assert(source, edition)`; delete the `new LegacyCompiler()`/`new CobolNetCompiler()` static fields.
Worked example — `VerbDifferentialTests.cs`:
```csharp
// before: two ICompilerUnderTest fields + AssertSameAsLegacy running both and comparing.
// after:
private static void AssertSameAsLegacy(string source) => DifferentialGolden.Assert(source);   // edition 85 default
```
(Method name kept to minimize churn; only its body changes. Tests passing a non-85 edition pass it through.)
Do this in **batches of ~10 files**, each batch a commit boundary.
**Why:** routes every differential case through the golden helper so the normal battery no longer runs the legacy engine.
**Verify (per batch):** first BAKE, then run in golden mode:
```
COBOLNET_DIFF_MODE=bake dotnet test tests/Cobol.Net.Tests.Conformance --filter "FullyQualifiedName~<BatchClass>"   # writes tests/differential/**/*.out
dotnet test tests/Cobol.Net.Tests.Conformance --filter "FullyQualifiedName~<BatchClass>"                           # golden mode, green
```
Commit the converted files AND their baked `tests/differential/**/*.out` together.
**Commit (per batch):** `test(cobolnet): bake+convert differential batch N/6 to golden comparison (P0 step 10)`.
> Bake determinism: run the bake on ONE machine/OS for the whole set so the goldens are consistent; the normalize
> basis (`CutRunner.Normalize`) already neutralizes CRLF/trailing-space so cross-OS golden compare stays green.

### Step 11 — Full bake sweep + differential drift guard  ★ COMMIT BOUNDARY
**Files:** commit any remaining `tests/differential/**/*.out`; add
`tests/Cobol.Net.Tests.Conformance/DifferentialGoldenDriftTests.cs`.
**Change:** a drift test asserting every `tests/differential/**/*.out` is reachable by a live test (no orphaned
goldens) and — cheaply — that the golden set is non-empty for each converted class. Do a final whole-project bake to
catch any case missed by the batches:
```
COBOLNET_DIFF_MODE=bake dotnet test tests/Cobol.Net.Tests.Conformance --filter "FullyQualifiedName~Differential"
git status tests/differential   # any new/changed .out ⇒ a case was not yet baked; commit it
```
**Why:** guarantees the bake is complete before the phase closes; the drift test is the "nothing silently orphaned"
backstop (mirrors the existing `CorpusManifestTests`/`ConstructRegistryDriftTests` discipline).
**Verify:** `dotnet test tests/Cobol.Net.Tests.Conformance` fully green in golden mode (default env).
**Commit:** `test(cobolnet): complete differential bake sweep + orphan-golden drift guard (P0 step 11)`.

### Step 12 — CI wiring  ★ COMMIT BOUNDARY
**File:** `.github/workflows/build-and-test.yml`.
**Change (additive only — no deletion of the legacy guard in P0):**
- Add `dotnet test tests/Cobol.Net.Tests.Characterization --no-build` to the existing `greenfield-tests` job (and the
  Windows job). CI always COMPARES snapshots (never sets `COBOLNET_UPDATE_SNAPSHOTS`).
- Add an opt-in `legacy-oracle` job that runs the differential suite with `COBOLNET_DIFF_MODE=verify` (proves the baked
  goldens still equal a live legacy run). This is the "kept as a live CI cross-check" posture; it is DELETED at G8, not now.
- Leave the existing `guard` (legacy NIST loop), `inv1-sweep`, and `windows-build-test` jobs untouched.
**Why:** the characterization gate runs on every PR; the legacy oracle keeps cross-checking the bake through the rearch
(DESIGN §3.8, §5 R1, §7 Q3).
**Verify:** CI dry-run / local equivalents:
```
dotnet test tests/Cobol.Net.Tests.Characterization --no-build
COBOLNET_DIFF_MODE=verify dotnet test tests/Cobol.Net.Tests.Conformance --filter "FullyQualifiedName~Differential"
```
Both green.
**Commit:** `ci(cobolnet): add characterization gate + opt-in legacy-oracle bake-verify job (P0 step 12)`.

### Step 13 — Close-out: master-plan STATE + DEVLOG + DOC_INDEX  ★ COMMIT BOUNDARY
**Files:** `docs/COBOLNET_REARCHITECTURE_PLAN.md` (flip its STATE banner to `P0 — DONE`, name the next phase P1; tick P0 in
the §4 phase-index checklist), `DEVLOG.md` (a phase
summary entry with the measured battery speedup from step 2), this file's STATUS line → `DONE`.
**Verify:** the full §5 battery green.
**Commit:** `docs(rearch): close P0 migration safety net — battery green, net self-standing (P0 step 13)`.

---

## 4. Ordering & commit-boundary summary
1. Master-plan STATE banner + DOC_INDEX (docs) ★
2. Roslyn ref cache (prod, perf-only) ★
3. Characterization project skeleton + sln ★
4. Probe + corpus discovery (bundled with 5)
5. Corpus seed + diagnostic snapshots (gate 2) ★
6. Emitted-C# snapshots (gate 3) ★
7. `corpus.tsv` + `CorpusManifestTests` ★
8. Repoint `NistDifferentialTests` ★
9. `DifferentialGolden` helper ★
10. Convert differential tests + bake, in batches ★ (×~6)
11. Full bake sweep + drift guard ★
12. CI wiring ★
13. Close-out ★

Every ★ leaves the full battery green.

---

## 5. Verification — the full battery (run at every ★ and at phase end)
```
# 1) Build with warnings-as-errors (CI parity).
dotnet build CobolSharp.sln -c Debug

# 2) Greenfield unit + conformance + characterization.
dotnet test tests/Cobol.Net.Tests.Unit          -c Debug
dotnet test tests/Cobol.Net.Tests.Conformance    -c Debug     # default env ⇒ differential in GOLDEN mode
dotnet test tests/Cobol.Net.Tests.Characterization -c Debug   # compares committed snapshots (never UPDATE in CI)

# 3) Legacy differential oracle cross-check (proves the bake is faithful) — opt-in.
COBOLNET_DIFF_MODE=verify dotnet test tests/Cobol.Net.Tests.Conformance --filter "FullyQualifiedName~Differential"

# 4) Legacy NIST guard — MUST stay 353 MATCH (unchanged by P0; P0 touches no production semantics).
bash scripts/guard-fast.sh
```
**Behavior-neutrality checks specific to P0:**
- Step 2 (ref cache): identical NIST/conformance verdicts before/after; record the wall-clock speedup in the DEVLOG.
- Step 8 (manifest repoint): the SET of passing `NistDifferentialTests` display names is identical to the pre-change 349.
- Steps 10–11 (bake): in `verify` mode `cobolnet == legacy == golden` for every differential case — proves no golden pins a wrong value.
- Characterization snapshots: seeded once from today's emitter; the compare run is green with ZERO diffs (any diff at seed-time review that is NOT explained by the corpus content is a bug in the harness, not the compiler).

---

## 6. Rollback / resumability
- **Resume point:** read the STATUS line at the top of this file + the master plan's (`docs/COBOLNET_REARCHITECTURE_PLAN.md`) STATE banner; the last green commit hash
  is recorded there. Every step is its own commit, so `git log --oneline` shows exactly how far P0 got.
- **Mid-step interruption:** all steps except step 10 are single-commit and idempotent — re-run the step's verify
  command; if red, `git checkout -- <files>` and redo. Step 10 is batched: each batch's converted files + baked `.out`
  commit together, so an interrupted step 10 resumes at the next unconverted batch
  (`grep -rl "new LegacyCompiler" tests/Cobol.Net.Tests.Conformance/*.cs` lists what remains).
- **Snapshot re-seed:** if a characterization snapshot was seeded wrong, delete the offending `Snapshots/*.txt`, re-run
  with `COBOLNET_UPDATE_SNAPSHOTS=1`, review the diff, re-commit. Never set that env var in CI.
- **Bake re-do:** goldens are pure derived data; `rm -rf tests/differential && COBOLNET_DIFF_MODE=bake dotnet test …~Differential`
  regenerates them (only from GREEN cases, so a regen can never bake a wrong value — it fails loudly on any
  `cobolnet != legacy`).

### Risks + mitigations (from DESIGN §6)
1. **Snapshot brittleness (gate 3).** Emitted-C# will diff on almost every emitter refactor. MITIGATION: gate (3) is
   advisory — a diff is a RED only if gates (1)+(2) are red OR there is no corresponding source change in the PR. Keep
   the characterization corpus small (one-per-family), not the whole NIST set.
2. **Bake faithfulness.** MITIGATION: bake only GREEN differential cases (the helper asserts `cobolnet==legacy` before
   writing); a currently-red/skip case is hand-authored to the ISO value, never baked.
3. **Two runtimes side-by-side.** The Conformance project references both runtimes; the characterization/probe path
   uses the greenfield `CompilerDriver` ONLY (the Characterization project does not reference the legacy projects at all).
4. **`corpus.tsv` chain semantics.** MITIGATION: step 7 folds `chains.tsv` verbatim + pins the green set against a
   committed 349-name snapshot; step 8's equivalence check proves the manifest-driven run reproduces today's verdicts
   before the old list is removed.
5. **Cross-OS golden compare.** `CutRunner.Normalize` (drop CR, per-line trailing-trim) already neutralizes CRLF; bake
   on one OS, compare on any.

---

## 7. ISO feature work in this phase
**NONE.** P0 is pure test/build/doc infrastructure plus one behavior-neutral perf edit. No spec section is implemented,
no edition behavior changes, no conformance test asserts a NEW semantic. The spec's role here is only as the AUTHORITY
that the baked/divergent goldens already conform to: the 11 `LEGACY_DIVERGENT` rows in `corpus.tsv` each carry their
existing ISO `§` citation (e.g. IX111A → §14.9.49.4 GR3a; NC235A → §14.9.37 F2 + §13.18.38 GR7 — citations already
inline in `NistDifferentialTests.cs`/`scripts/guard.sh`), copied into the manifest note column so the "why does this
golden differ from a live legacy run" answer travels with the data. Any golden touched by the bake must equal both the
legacy output AND (for divergent rows) the ISO-conforming value — enforced by `verify` mode.
