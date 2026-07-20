# DESIGN — Test, Build & CI Architecture (+ Migration Safety)

Status: DESIGN (rearchitecture dimension). Author: rearchitecture review. Date: 2026-07-07.
Scope: the harness/build/CI machinery that KEEPS THE BATTERY GREEN through the clean-slate rearchitecture of
`src/Cobol.Net.*`, plus the diagnostic-code registry, the characterization strategy, and the roadmap/DEVLOG
discipline that make each phase resumable and behavior-neutral.

This dimension underpins every other rearchitecture dimension: none of the god-class splits, pass-pipeline
extractions, or storage-form unifications can proceed safely unless the test net can PROVE, per phase, that
observable behavior did not change. Today the net can prove agreement with a *frozen legacy engine that is deleted
at G8* — so the net itself must be rearchitected first, or it evaporates mid-migration.

---

## 1. Current state (as-built, grounded)

### 1.1 Test projects (four, two stacks)
| Project | Files | Role |
|---|---|---|
| `tests/Cobol.Net.Tests.Conformance` | ~80 `.cs` | ACTIVE greenfield net: differential feature tests (`*DifferentialTests.cs`), `NistDifferentialTests` (318 goldens as inline `[InlineData]`), `VersionMatrixTests`, `CorpusRunnerTests`, `EditionHarness`, `CompilerUnderTest`/`CutRunner`. |
| `tests/Cobol.Net.Tests.Unit` | ~17 `.cs` | Greenfield unit: runtime kernels, `ConstructRegistryDriftTests`, `ReservedWordsDriftTests`, `CheckOnlyCompileTests`, CLI parser, `EditionContextTests`. |
| `tests/CobolSharp.Tests.Integration` | legacy | FROZEN legacy oracle + the post-85 `ConformanceTests` corpus runner with the `GreenfieldOnly` / `LegacyDivergent` skip sets. |
| `tests/CobolSharp.Tests.Unit` | legacy | FROZEN legacy unit tests. |

### 1.2 The differential oracle (the load-bearing risk)
`CompilerUnderTest.cs` defines `ICompilerUnderTest` with two impls: `LegacyCompiler` (drives the frozen
`CobolSharp.Compiler.Compilation`) and `CobolNetCompiler` (drives `CompilerDriver`). ~60 `*DifferentialTests.cs`
compile the SAME source with both and assert byte-identical normalized stdout (`CutRunner.Normalize` = the guard's
`normalize()`: drop CR, per-line trailing-trim). **The legacy engine is deleted at G8** (design SSOT + the csproj
comment on the legacy `ProjectReference`). At that moment every dynamic differential test loses its oracle.

`NistDifferentialTests` is DIFFERENT and safe: it compares COBOL.NET output to committed goldens
(`tests/nist/valid/*.txt`), not to a live legacy run. But the green NIST set is a hand-maintained ~318-row
`[InlineData]` list — a THIRD copy of "which programs are green" (the others: `scripts/guard.sh` `NIST_TESTS`, and
implicitly `tests/nist/chains.tsv`).

### 1.3 Guard scripts (bash, Linux-only NIST loop, over the LEGACY CLI)
- `scripts/guard.sh` — serial: builds the legacy `cobolsharp.dll`, runs legacy unit+integration, then compiles+runs
  ~353 NIST programs THROUGH THE LEGACY ENGINE and diffs against `tests/nist/valid/`. Carries `LEGACY_DIVERGENT`
  (11 ISO-rebaselined goldens the legacy legitimately differs on) and the golden-cleanliness sweep.
- `scripts/guard-fast.sh` — parallel version (per-suite isolation), proven equivalent by `guard-verify.sh`.
- `scripts/guard-run-group.sh`, `guard-verify.sh` — grouping + equivalence proof.
- `scripts/version-continuity-sweep.sh` — INV-1: one warm `cobol check-batch` over ~350 programs × 4 editions
  (no Roslyn), fails on any `BREAKS`. THIS one drives the greenfield CLI.
- `scripts/compliance.sh`, `nist-batch.sh`, `run-suite.sh` — legacy dashboards.

The authoritative NIST regression (`guard.sh`) exercises the **frozen legacy engine**, not the compiler under
active development. The greenfield NIST coverage lives entirely in the in-process `NistDifferentialTests`.

### 1.4 CI (`.github/workflows/build-and-test.yml`, 4 concurrent jobs)
`guard` (legacy parallel NIST, ubuntu) · `greenfield-tests` (conformance+unit, ubuntu) ·
`inv1-sweep` (permissive continuity, ubuntu) · `windows-build-test` (Release warnings-as-errors + all four suites).
NuGet cached; `Generated/` regenerated per checkout (java+pwsh prerequisites).

### 1.5 Drift discipline (a genuine strength — preserve it)
`ConstructRegistryDriftTests` binds `constructs.json` ↔ in-code `ConstructRegistry` both directions;
`CorpusRunnerTests.Manifest_CoversEveryProgram_NoOverlap` proves every on-disk `.cob` is manifest-listed;
`ReservedWordsDriftTests` binds `reserved-words.json`. Nothing silently undiscovered.

### 1.6 Diagnostics
161 distinct `COBOLNET####` codes exist as **bare string literals**; only 4 are named consts (`EditionCodes.cs`).
`COBOLNET0899` (catch-all "recognized-not-implemented") appears at ~47 sites; several codes are reused for
unrelated rules (e.g. `1533` ×3). There is no registry, no `docs/DIAGNOSTICS.md`, no per-code metadata, so tests
and `--suppress` cannot target a specific rule and the version matrix cannot enumerate diagnostics.

---

## 2. Problems this design must fix

1. **The net evaporates at G8.** ~60 differential tests are oracle-coupled to code that gets deleted.
2. **No behavior-neutrality proof for a refactor.** A rearchitecture phase that splits `StatementBinder` or unifies
   `StoreAsImage` has NO characterization gate: it relies on output goldens for the subset of programs that happen
   to be in the corpus, and nothing snapshots the generated C# to catch an unintended emit change.
3. **"Which programs are green" has three sources of truth** (`guard.sh` `NIST_TESTS`, `NistDifferentialTests`
   `[InlineData]`, `chains.tsv`) that can silently disagree.
4. **The authoritative NIST gate tests frozen code.** CI's heaviest job (`guard`) exercises the legacy engine; the
   compiler under development is gated only by the in-process differential suite.
5. **NIST loop is Linux-only bash**, so the Windows job cannot run it; the authoritative regression has an OS gap.
6. **Diagnostics are unaddressable.** No registry ⇒ the version matrix, `--suppress`, and per-rule tests cannot key
   on a stable id; a renumber/reuse is invisible.
7. **`CheckOnly` runs full Emit** (no Bind phase boundary) ⇒ the fast verdict path is not actually fast, and a
   characterization harness cannot snapshot the bound tree independently of emission.
8. **Roslyn reference set uncached** ⇒ the in-process battery pays ~180 `MetadataReference.CreateFromFile` per
   compile (thousands of times per run).
9. **Roadmap SSOT is a 100 KB `resume-prompt.md`** — excellent for session resume, but there is no compact,
   phase-structured, exit-criteria-bearing rearchitecture roadmap doc that a future engineer resumes the
   MIGRATION (not the feature drive) from.

---

## 3. Target design

### 3.0 Guiding principle: three neutrality gates, ranked
A rearchitecture phase is "behavior-neutral" iff, in order of authority:
1. **Output goldens** (primary, spec-anchored): NIST `valid/*.txt` + corpus `.out` + negative `.err`. These are the
   ISO-conforming oracle and MUST stay byte-identical across every refactor phase. Non-negotiable.
2. **Diagnostic goldens** (primary): every emitted diagnostic (code + message + location) for a fixed corpus of
   positive and negative programs is snapshotted; a refactor must not change the diagnostic surface.
3. **Emitted-C# snapshots** (secondary, advisory): the generated `.g.cs` for a representative program set is
   snapshotted. A refactor that is a pure reorg SHOULD leave these byte-identical; a refactor that intentionally
   changes emission form (e.g. structured `Place` → different but equivalent C#) RE-BASELINES the snapshot **with
   explicit review**, and gate (1) proves the re-baseline is behavior-preserving.

Gates (1) and (2) are hard (red = stop). Gate (3) is a reviewed diff, never auto-accepted, never silently drifting.

### 3.1 Test project taxonomy (target)
Collapse to a coherent, layered set under one namespace root `CobolNet.Tests.*`:

| Project | Replaces | Contents |
|---|---|---|
| `tests/Cobol.Net.Tests.Unit` | (kept) | Pure unit: runtime kernels, binder/emitter component units, CLI/driver, drift tests. Fast, no Roslyn where avoidable. |
| `tests/Cobol.Net.Tests.Conformance` | (kept, refocused) | End-to-end program behavior: NIST goldens, per-edition corpus, version matrix, negative corpus. Compares to COMMITTED goldens only — no live legacy oracle. |
| `tests/Cobol.Net.Tests.Characterization` | NEW | The behavior-neutrality harness: diagnostic snapshots + emitted-C# snapshots (gates 2 & 3) over a fixed "characterization corpus". Runs on every phase. |
| `tests/CobolSharp.Tests.*` | DELETE at G8 | Frozen legacy. Retired once the oracle is baked (see 3.4). |

Rationale: the legacy `ConformanceTests` corpus runner and its `GreenfieldOnly`/`LegacyDivergent` sets are made
redundant by `CorpusRunnerTests` once the legacy oracle is baked out — one corpus runner, not two.

### 3.2 The green-corpus single source of truth: `tests/nist/corpus.tsv`
Replace the three copies with ONE declarative manifest, consumed everywhere.

```
# tests/nist/corpus.tsv — the ONE list of NIST programs and their status.
# name  suite  status         chain-preds(space-joined, '-' if none)   golden(valid|none)   note
NC101A  NC     green          -                                        valid                first NC program
ST103A  ST     green          ST101A ST102A                            valid                SORT USING/GIVING chain
IX999Z  IX     pending        -                                        none                 not yet compiling
```

- `status ∈ {green, pending, divergent}`. `green` ⇒ a `[Theory]` row asserting golden match. `divergent` ⇒ the
  `LEGACY_DIVERGENT` set (compiled+run, output NOT compared to a legacy run — the golden is already ISO-conforming;
  the note carries the ISO citation). `pending` ⇒ catalogued, not asserted (the mass-red guard).
- `chain-preds` REPLACES `chains.tsv` (folded in — one file).
- `NistDifferentialTests` reads `corpus.tsv` via `[MemberData]`, NOT a hand-maintained `[InlineData]` list.
- The bash guard (while it survives) reads the same file for its `NIST_TESTS` and `LEGACY_DIVERGENT`.
- A drift test (`CorpusManifestTests`) asserts: every `tests/nist/programs/*.cob` is listed; every `green` row has a
  `valid/<name>.txt`; every `divergent` row has a non-empty note containing a `§` citation.

This kills smell #3 and makes "add a green program" a one-line manifest edit.

### 3.3 The characterization harness (`Cobol.Net.Tests.Characterization`)
The behavior-neutrality proof the rearchitecture needs. Structure:

```
tests/Cobol.Net.Tests.Characterization/
  CharacterizationCorpus.cs      # discovers tests/characterization/**/*.cob (+ per-file .std directive)
  DiagnosticSnapshotTests.cs     # gate (2)
  EmittedCSharpSnapshotTests.cs  # gate (3)
  Snapshots/                     # committed golden snapshots (.diag.txt, .g.cs.txt)
tests/characterization/
  positive/*.cob                 # programs that compile clean — snapshot their emitted C#
  negative/*.cob                 # programs that must diagnose — snapshot their diagnostic list
  README.md
```

Signatures (concrete):

```csharp
public interface ICompilerProbe {
    // Bind-only: no Roslyn. Returns the diagnostic list (code+message+location) and, optionally, the emitted C#.
    ProbeResult Probe(string source, int edition, bool emit);
}
public sealed record ProbeResult(
    bool Ok,
    IReadOnlyList<Diagnostic> Diagnostics,   // structured — see 3.5
    string? EmittedCSharp);                   // null unless emit:true
```

- `DiagnosticSnapshotTests` compiles each `characterization/**` program, formats its diagnostics deterministically
  (`{code}\t{severity}\t{line}:{col}\t{message}`), and asserts equality with `Snapshots/<name>.diag.txt`.
- `EmittedCSharpSnapshotTests` emits the C# for each `positive/*.cob` and asserts equality with
  `Snapshots/<name>.g.cs.txt` (line-ending normalized). Roslyn is NOT invoked — this snapshots the emitter output
  string, which is cheap and deterministic.
- Re-baselining: `COBOLNET_UPDATE_SNAPSHOTS=1 dotnet test …/Characterization` rewrites the `Snapshots/` files. This
  is a DELIBERATE, reviewed action (the diff shows in the PR). Gate (1) — output goldens — proves any re-baseline
  is behavior-preserving. Never run in CI; CI always compares.

The characterization corpus is seeded ONCE, at the START of the rearchitecture, from the current (pre-refactor)
emitter, so the first snapshot is "how the emitter emits today." Every subsequent phase must match it (or
review-re-baseline). This is the missing "prove I changed nothing" gate.

### 3.4 Oracle bake-out: converting the differential net to goldens (the G8-survival migration)
The ~60 `*DifferentialTests.cs` currently assert `cobolnet == legacy` at RUN time. Before the legacy engine is
deleted, run a ONE-TIME bake that freezes the legacy output as a committed golden, then rewrite each test to assert
`cobolnet == golden`.

Mechanism:
1. Add `CutRunner`-driven `DifferentialBakeTool` (a `[Fact(Skip=…)]`-gated maintenance test, or a small
   console tool in `tools/`) that, for every differential test's source, runs `LegacyCompiler`, captures normalized
   stdout, and writes `tests/differential/<TestClass>/<case>.out`.
2. Rewrite the differential base to a golden-comparison base:
   ```csharp
   protected void AssertMatchesGolden(string source, string goldenName, int edition = 85)
       => Assert.Equal(ReadGolden(goldenName), Cn.CompileAndRun(source).stdout);
   ```
   The legacy `ICompilerUnderTest`/`LegacyCompiler` and the `CobolNet==Legacy` asserts are deleted.
3. A `divergent`-style escape hatch stays for the handful of legacy-non-conforming cases (already tracked in
   `LegacyDivergent`) — those goldens are hand-authored to the ISO value.

After the bake, the entire greenfield net is self-standing: it depends on committed goldens, not on the legacy
`ProjectReference`. The legacy test projects and the `guard.sh` NIST loop can be deleted at G8 with zero net loss.
**This bake is a PREREQUISITE gate for G8 and for retiring the legacy CI job.**

### 3.5 Diagnostic-code registry (`Cobol.Net.Diagnostics`)
A first-class subsystem (also required by the Editions dimension). Minimal shape:

```csharp
public sealed record DiagnosticDescriptor(
    string Code,           // "COBOLNET1533" — one code, one rule
    Severity DefaultSeverity,
    string SpecRef,        // "ISO §8.4.3.9.4" — the citation
    string MessageTemplate,// "strong TYPE mismatch: {0} vs {1}"
    string? SuppressKey);  // stable --suppress token

public static class Diag {                    // the catalogue (generated-friendly consts)
    public static readonly DiagnosticDescriptor StrongTypeMoveMismatch = new("COBOLNET1533", …);
    // … one entry per distinct rule …
}
public readonly record struct Diagnostic(
    DiagnosticDescriptor Descriptor, string Message, SourceSpan Location, Severity Severity);
```

- `EditionContext.Error("COBOLNET####", text)` → `sink.Report(Diag.X, args…)`. The 47 `0899` sites split into
  named unimplemented-feature descriptors behind a tracked list; `1533`-style reuse is un-merged into distinct
  codes.
- `DiagnosticRegistryDriftTests` (in `Tests.Unit`): every `Diag.*` descriptor has a unique code; every code emitted
  anywhere in the compiler is a registered descriptor (a source-scan or a runtime-collected set). Mirrors the
  existing `ConstructRegistryDriftTests` discipline.
- A build step (or a `[Fact]` that writes when `COBOLNET_UPDATE_DOCS=1`) generates `docs/DIAGNOSTICS.md` from the
  catalogue — the single human-readable code table understandability #1 asks for.

This makes gate (2) precise (snapshots key on stable codes) and lets the version matrix and `--suppress` target a
rule.

### 3.6 Build & driver seams the harness depends on
Two build-side changes this dimension REQUIRES (owned by the driver/emitter dimensions but gated here):
- **Real Bind phase boundary.** Extract the binder passes — reached today through the `CSharpEmitter.Bind` host
  facade — into a standalone `Binding.BindPipeline.Bind(tree, edition) → BoundCompilation`. Then: `CheckOnly`/`--check-batch` stop after Bind
  (fast verdict, no Emit); `ICompilerProbe.Probe(emit:false)` returns diagnostics from Bind only; the
  characterization emitter snapshot is a pure `Emit(BoundCompilation)`.
- **Cache the Roslyn reference set.** `RoslynBackend.ReferenceAssemblies()` →
  `static readonly Lazy<ImmutableArray<MetadataReference>>`. Single highest-leverage battery-throughput fix.

### 3.7 Guard consolidation (cross-platform, greenfield-first)
Target: the authoritative regression is the in-process `dotnet test` battery (runs on every OS), NOT a Linux bash
loop over frozen code. Scripts collapse to:

- `scripts/guard.ps1` + `scripts/guard.sh` (thin, equivalent wrappers): `dotnet build -warnaserror` →
  `dotnet test Unit Conformance Characterization` → `cobol check-batch` continuity sweep. Cross-platform, ~one
  command. No NIST bash loop (the in-process `NistDifferentialTests` IS the NIST net).
- KEEP `scripts/version-continuity-sweep.sh` (the check-batch INV-1 sweep — it drives the greenfield CLI and is
  already fast/portable) but add a `.ps1` sibling.
- DELETE (at G8, after the bake) `guard-fast.sh`, `guard-run-group.sh`, `guard-verify.sh` (they exist to parallelize
  the legacy NIST loop, which is gone), `compliance.sh`, `nist-batch.sh`, `run-suite.sh` (legacy dashboards).
- Until G8, KEEP `guard.sh`/`guard-fast.sh` as the oracle-agreement check that the bake was faithful.

### 3.8 CI (target `build-and-test.yml`)
An OS matrix, greenfield-authoritative, with the characterization gate:

```yaml
strategy: { matrix: { os: [ubuntu-latest, windows-latest] } }
jobs:
  build-test:      # per-OS: build -warnaserror; dotnet test Unit + Conformance + Characterization --no-build
  version-sweep:   # ubuntu: cobol check-batch INV-1 (permissive continuity), fail on BREAKS
  legacy-oracle:   # TEMPORARY (pre-G8 only): guard-fast.sh — proves the bake still matches legacy; deleted at G8
```

Post-G8 the `legacy-oracle` job and the two legacy test projects are removed; `build-test` becomes the whole gate,
identical on both OSes (NIST now runs in-process, closing smell #5). `Generated/` continues to regenerate per
checkout (a failed regen fails the build — keep).

### 3.9 Roadmap SSOT + DEVLOG discipline
- **`docs/COBOLNET_REARCHITECTURE_PLAN.md`** (the master plan) — the resumable migration SSOT / ROADMAP: an ordered
  P0–P16 phase index, per-phase exit criteria (universal clause: "gates (1) & (2) green; gate (3) either unchanged or
  reviewed-re-baselined in this change set"), a top STATE banner naming the current phase, and the owner-decisions
  table. This is what a future engineer resumes the REARCH from; the plan §0 banner is the feature-drive live
  state and cross-links to it.
- **DEVLOG.md** — unchanged discipline (descending, real timestamp, one entry per commit) per the existing
  `feedback_devlog*` memories. Each rearch phase commit references its ROADMAP phase id.
- **`docs/DOC_INDEX.md`** — add rows for `COBOLNET_REARCHITECTURE_PLAN.md` (the migration SSOT / ROADMAP),
  `DIAGNOSTICS.md`, this doc, and the sibling `DESIGN-*` / `PHASE-*` docs; keep the "one canonical doc per subsystem" rule.

---

## 4. Current → target module changes

| Action | From | To | Why |
|---|---|---|---|
| create | — | `tests/Cobol.Net.Tests.Characterization/` (+ `tests/characterization/`, `Snapshots/`) | The behavior-neutrality harness (gates 2 & 3) — the missing "prove I changed nothing" proof for every refactor phase. |
| create | — | `tests/nist/corpus.tsv` | ONE source of truth for the green NIST set + chains; kills the 3-way triplication. |
| create | — | `src/Cobol.Net.Compiler/Diagnostics/` (`DiagnosticDescriptor`, `Diag`, `Diagnostic`, `Severity`) | Registry so codes are addressable, snapshot-keyable, doc-generable, suppress-targetable. |
| (exists) | — | `docs/COBOLNET_REARCHITECTURE_PLAN.md` | Resumable migration SSOT + roadmap with per-phase exit criteria = battery-green gate (the master plan already in the repo). |
| create | — | `docs/DIAGNOSTICS.md` (generated) | The single human-readable code table (understandability #1). |
| create | — | `tools/DifferentialBakeTool` (or a skip-gated maintenance test) | One-time freeze of legacy output → committed goldens (the G8-survival bake). |
| create | — | `scripts/guard.ps1` | Cross-platform authoritative guard (Windows parity for the regression). |
| split | `NistDifferentialTests.cs` (318 `[InlineData]`) | `[MemberData]` over `corpus.tsv` + a thin theory | Remove the hand-maintained green list; single source of truth. |
| merge | `tests/nist/chains.tsv` | into `tests/nist/corpus.tsv` | One manifest, not two. |
| refactor | ~60 `*DifferentialTests.cs` (assert `cobolnet==legacy`) | golden-comparison base (`AssertMatchesGolden`) + `tests/differential/**/*.out` | Sever the oracle coupling so the net survives G8. |
| refactor | `EditionContext.Error(code,msg)` string-concat; 161 bare codes | `sink.Report(Diag.X, args)` | Route all diagnostics through the registry. |
| split | `COBOLNET0899` (~47 sites) | distinct unimplemented-feature descriptors behind a tracked list | One code = one rule; snapshot precision. |
| move | `CompilerUnderTest.LegacyCompiler` / `ICompilerUnderTest` | DELETE after the bake | Legacy oracle gone once goldens are baked. |
| refactor | `RoslynBackend.ReferenceAssemblies()` (uncached) | `static Lazy<ImmutableArray<MetadataReference>>` | Battery throughput (thousands of rebuilds → one). |
| refactor | binder passes behind the `CSharpEmitter.Bind` host facade | `Binding.BindPipeline → BoundCompilation` (standalone) | Bind phase boundary: fast `CheckOnly`, probe-able bound tree, clean Emit snapshot. |
| rewrite | `.github/workflows/build-and-test.yml` (4 jobs, legacy-authoritative) | OS-matrix `build-test` + `version-sweep` + temporary `legacy-oracle` | Greenfield-authoritative, cross-platform NIST, characterization gated. |
| delete (G8) | `tests/CobolSharp.Tests.Unit`, `tests/CobolSharp.Tests.Integration` | — | Frozen legacy retired after the bake. |
| delete (G8) | `scripts/guard.sh`, `guard-fast.sh`, `guard-run-group.sh`, `guard-verify.sh`, `compliance.sh`, `nist-batch.sh`, `run-suite.sh` | — | All exist to run/parallelize the legacy NIST loop or legacy dashboards — dead once NIST runs in-process. |
| delete | `CobolParserJsonXml.g4`, `CobolExtensionsJsonXml.g4` (dead JSON/XML) | — | Non-ISO (0 spec occurrences) — hard invariant #5; remove from any test/build surface. |
| move | legacy `ConformanceTests` corpus + `GreenfieldOnly`/`LegacyDivergent` sets | folded into `CorpusRunnerTests` | One corpus runner over committed goldens. |

---

## 5. Migration notes — keeping the battery green through the phases

The rearchitecture proceeds so the net is STRENGTHENED before it is relied on, then legacy is severed:

- **Phase R0 — Net-first (do BEFORE any refactor).**
  1. Land `corpus.tsv` + `CorpusManifestTests`; repoint `NistDifferentialTests` at it (behavior identical, coverage
     unchanged — pure de-duplication). 2. Stand up `Cobol.Net.Tests.Characterization` and SEED `Snapshots/` from the
     CURRENT emitter (captures "today's behavior"). 3. Cache the Roslyn reference set (safe, pure speed). 4. Land the
     diagnostic registry + `DiagnosticSnapshotTests` seeded from today's output. After R0 the battery can prove
     behavior-neutrality; no compiler behavior changed.
- **Phase R1 — Bake the oracle.** Run `DifferentialBakeTool`, commit `tests/differential/**/*.out`, rewrite the
  differential base to golden comparison, DELETE the `cobolnet==legacy` asserts. Keep the legacy test projects and
  `guard-fast.sh` in CI as a `legacy-oracle` job proving the goldens still equal a live legacy run. The net is now
  self-standing but still cross-checked.
- **Phases R2…Rn — the actual rearchitecture** (god-class splits, pass pipeline, storage-form unification, per the
  sibling DESIGN-* docs). EACH phase: run the full battery; gates (1)+(2) MUST stay green; gate (3) stays green or
  is reviewed-re-baselined IN THE SAME change set (with a DEVLOG note citing the intended emit change and the
  gate-(1) proof). No phase merges red. Small phases (feedback_iterate_one_at_a_time) so a snapshot diff is legible.
- **Phase G8 — Sever legacy.** Once R1's `legacy-oracle` job has stayed green across the rearch, delete the legacy
  test projects, the legacy `ProjectReference`s, the legacy guard scripts, and the `legacy-oracle` CI job. The
  `build-test` matrix is now the whole gate, cross-platform.

Throughout: `Generated/` remains a build output (regen per checkout, failed regen fails the build); warnings-as-
errors stays on the Release/CI build; the drift tests (`ConstructRegistry`, `ReservedWords`, `CorpusManifest`,
`DiagnosticRegistry`) are the "nothing silently added/dropped" backstop.

---

## 6. Risks

1. **Snapshot brittleness.** Emitted-C# snapshots (gate 3) will diff on almost every emitter refactor. MITIGATION:
   gate (3) is advisory/reviewed, never a hard CI red on its own IF gates (1)+(2) are green — but a gate-(3) diff
   with NO corresponding source change in the PR IS a red (unexpected drift). Keep the characterization corpus small
   and representative (one program per feature family), not the whole NIST set.
2. **Bake faithfulness.** If the bake captures a legacy output that COBOL.NET already diverges from (an
   intended ISO fix), the golden would wrongly pin the legacy value. MITIGATION: bake only cases where the
   differential test is currently GREEN (cobolnet already == legacy); a currently-red/skip differential case is
   hand-authored to the ISO value and marked `divergent`.
3. **Two runtimes side-by-side until G8.** The conformance project references both runtimes; a program loads only
   its own. Low risk (proven today) but the characterization/probe path must use the greenfield driver ONLY.
4. **`corpus.tsv` chain semantics** must exactly reproduce `chains.tsv` + `guard.sh` ordering. MITIGATION: a
   one-time `guard-verify`-style diff proving the manifest-driven run matches the current verdict list before
   deleting the old sources.
5. **Diagnostic registry churn** touches ~161 call sites. MITIGATION: mechanical, one code family at a time, each
   change set battery-green; the drift test catches any orphaned/duplicate code.
6. **CI time** could rise if characterization emits Roslyn-compile every program. MITIGATION: gate (3) snapshots the
   emitter STRING only (no Roslyn); the reference-set cache offsets the rest.

---

## 7. Open questions for the owner

1. **Snapshot tooling:** hand-rolled `Assert.Equal(File)` + `COBOLNET_UPDATE_SNAPSHOTS` env, or adopt Verify
   (`Verify.Xunit`)? Verify gives received/verified diffing + review workflow for free but adds a dependency. Lean:
   hand-rolled (zero new deps, matches the repo's existing golden-file idiom) unless you want the Verify UX.
2. **Characterization corpus size:** one program per feature family (~120 programs, fast) vs. snapshot the full NIST
   corpus's emitted C# (maximal coverage, slower, noisier diffs). Recommend the curated family set for gate (3), NIST
   goldens already cover gate (1) breadth.
3. **When to sever legacy (G8 timing):** delete the legacy oracle the moment R1's bake lands and `legacy-oracle`
   goes green once, or keep it running through the WHOLE rearch as a live cross-check (costs one CI job)? Recommend
   keeping it through the rearch (cheap insurance), delete at true G8.
4. **`--suppress` granularity:** per-code, per-family, or per-descriptor `SuppressKey`? Affects the registry shape;
   default proposed is per-code with an optional family key.
5. **Migration-SSOT vs resume-prompt.md ownership — RESOLVED (2026-07-07):** the migration roadmap is the standalone
   `docs/COBOLNET_REARCHITECTURE_PLAN.md` (since 2026-07-19 THE ONE consolidated plan incl. the §0 live banner);
   `resume-prompt.md` remains the feature-drive state.
6. **Do we snapshot the runtime deploy?** The runtime DLL is copied per emit; characterization ignores it. Confirm
   the runtime is out of the neutrality scope (it is typed-native and separately unit-tested) — assumed yes.
