# Version Test Matrix — Design (testing COBOL.NET as N per-edition compilers)

> **STATUS BANNER — DESIGN, in build-out: Phase 0 ✅ harness scaffold (DEVLOG 519) + default-edition flip ✅
> (DEVLOG 520); Phases 1–3 ahead. LIVE design reference.**
>
> **Premise (owner, 2026-06-09):** *"Conceptually we have a different COBOL compiler for each ISO edition (1985 /
> 2002 / 2014 / 2023). We should test it as such."* A single corpus run at one dialect cannot validate that. This
> document designs the **(construct × target-edition) test matrix** and the test-infrastructure rework to drive it,
> sourced from [`VERSION_CHANGE_REFERENCE.md`](VERSION_CHANGE_REFERENCE.md). Companion memories:
> `feedback_version_test_matrix`, `feedback_version_targeted_semantics`, `project_dialect_strictness`.
>
> **Scope:** the **greenfield** compiler (`src/Cobol.Net.*`, the active engine) is the target. The legacy
> `CobolSharp.Compiler` is the **blueprint** (it already has the per-edition machinery) and is retired at G8.
>
> **Scope honesty (don't over-read this):** this matrix validates the edition **deltas / boundaries** — where gating
> bugs live (the right ~80%): "feature introduced in E is rejected below E", "feature removed by E is rejected at E",
> "85 program still compiles later unless removed or word-collided", per-edition behavior where it differs. It is NOT
> comprehensive per-edition *construct* coverage — exhaustively re-validating every 2002/2014/2023 construct stays
> gated on building those **positive corpora** (`tests/conformance/<ver>/`), which the survey confirms are young
> (≈34 × 2002, ~0 × 2014, 1 × 2023). Delta-correctness first; corpus breadth is a parallel, longer effort.

---

## 1. The conceptual model

`cobol --std V prog.cob` **is** the "COBOL-V compiler." Correctness is per-edition: for every COBOL construct
and every target edition V ∈ {85, 2002, 2014, 2023}, the compiler must produce the **edition-correct outcome** — accept
with the V behavior, or reject with an edition-appropriate diagnostic. We therefore test four logical compilers, not
one, over a shared matrix.

## 2. The matrix

**Axes:** `{construct/feature test case}` × `{target edition 85 | 2002 | 2014 | 2023}`.

Each construct case carries metadata (from the reference doc, see §4):
- `introducedIn` — the edition the construct first became standard (85 = baseline).
- `removedIn` / `deprecatedIn` — the edition that removed / obsoleted it (if any).
- `behaviorVariants` — for constructs whose *behavior* changed across editions, the expected result per edition.

**Expected-outcome function** `f(case, V)` — *computed*, never hand-maintained per cell:

| Condition | Expected outcome at edition V |
|---|---|
| `V < introducedIn` | **REJECT** — diagnostic "feature requires COBOL-`introducedIn`" (introduction-gating) |
| `removedIn ≤ V` | **REJECT / removed** — diagnostic per the reference-doc row (error in strict V; or warn, per policy §8) |
| `introducedIn ≤ V < removedIn` | **COMPILE** — and, if the construct has `behaviorVariants`, emit `behaviorVariants[V]` |

A cell is *green* when the actual outcome equals `f(case, V)`.

## 3. The three correctness invariants (as property tests)

- **INV-1 — Continuity.** ∀ COBOL-85 program P (the NIST CCVS85 corpus) and ∀ V ≥ 85: P **compiles** at V, *unless* P
  hits one of **two** legitimate-breakage classes documented in the reference doc: **(a)** P uses a construct with
  `removedIn ≤ V` (a removed/obsoleted feature); or **(b)** P uses, as a user-defined word, a word that became a
  **reserved word** at some edition ≤ V (Annex E.3.2 — "changes possibly affecting because of the addition of new
  words/names"). A compile failure at a later edition is conformant **only if it traces to a reference-doc removal row
  (a) or new-reserved-word row (b)**; any other failure is a **regression**, not conformance. (Owner's rule: "the ones
  that do not [work later] must have been deprecated by that later version for the failure to be correct.")
  > **Why class (b) matters (and a corrected datum):** a program using a word newly reserved in 2023 (Row 32 —
  > B-SHIFT-*, COMMIT, RECEIVE, SEND, XOR, …) as a user-defined name must compile pre-2023 and be **rejected at 2023**;
  > the naive "unless `removedIn ≤ V`" form would wrongly flag it as a regression. ⚠ An earlier note claimed `RECEIVE`
  > is a data name in 4 NIST-85 programs — that was a **grep artifact** (`SPACING-RECEIVE`, hyphenated → a word-boundary
  > false match), NOT a bare collision; the DEVLOG-520 continuity sweep found **zero** NC breaks at 2023, consistent
  > with no live collision. So class (b) is currently **latent** in the NC corpus (the greenfield also does not yet
  > edition-gate reserved words — DEVLOG 520) — exercise it with a synthetic case and confirm against the full corpus
  > once the `EditionValidator` reserves words by edition.
- **INV-2 — Introduction-gating.** ∀ construct C introduced in edition E and ∀ V < E: C is **rejected** at V with the
  edition diagnostic. (A word newly reserved in 2023 is still usable as a user-defined name at 85/2002/2014.)
- **INV-3 — Behavior-correctness.** ∀ behavior-variant construct and ∀ valid V: the output equals `behaviorVariants[V]`
  (e.g. a hypothetical edition-dependent rounding/sign rule — none confirmed yet; the three investigated de-sign/DISPLAY
  differences are version-INVARIANT, DEVLOG 517).

## 4. Construct catalogue — sourced from the reference doc

**⚠ Source the catalogue from THREE places, not just the reference doc — the 85↔non-85 boundary (the owner's #1
priority) is the part the 2023 reference doc covers *least*** (its Annex E is the 2014→2023 delta only). Harvest the
edition metadata mechanically from the structured in-code sources that already encode it:

| Metadata | Authoritative source | Covers |
|---|---|---|
| `introducedIn` (2002/2014/2023) | the **grammar `is2002()/is2014()/is2023()` gates** (39, per the survey) — each gated rule's predicate IS its introducing edition | the post-85 introduction points — the 85-rejects-post-85-feature half of the owner's #1 |
| `removedIn` + 85↔2002 deltas | the **legacy `FlagsFeaturesRemovedAfter85` + `DialectStrictnessChecks` (ALTER, OPEN REVERSED, L1–L5) + FLAG-02** | constructs removed after 85; the 2002 incompatibilities the ref doc only points at |
| 2014→2023 deltas, obsolete/archaic, new reserved words | **`VERSION_CHANGE_REFERENCE.md`** | the latest delta + the flagging rows |

Each source feeds the same catalogue; the reference doc is canonical for the 2014→2023 slice, the grammar+legacy for
the earlier slices. (This closes the gap the survey flagged: only ~30–40 ref-doc rows are mechanically testable, but
the grammar gates + legacy registry supply the 85↔2002/2014 boundary directly.)

A single **construct catalogue** (data, not code) is the source of truth for the matrix. Each entry:

```
{ id, title, refDocRows[], category,            // from VERSION_CHANGE_REFERENCE.md
  introducedIn, removedIn?, deprecatedIn?,        // edition metadata
  snippet,                                        // a minimal COBOL program exercising the construct
  behaviorVariants?: { "85": out, "2002": out, … },
  expectDiagnostic?: "CBL####" }                  // the code expected on rejection
```

**Mapping reference-doc `gatingImplication` → matrix obligation:**

| `gatingImplication` | `introducedIn` / `removedIn` | Matrix obligation |
|---|---|---|
| `new-feature-gate` | `introducedIn = <delta-to edition>` | positive at ≥ intro; **negative (reject) at intro−1** |
| `new-reserved-word` | `introducedIn = <edition>` | user-name OK at < intro; **reject as reserved at ≥ intro** (INV-2 inverted: words are *additive* — collision only at ≥ intro) |
| `flag-obsolete` | `removedIn` / `deprecatedIn` | accepted+warn while archaic; **reject (or warn) once removed**, per policy |
| `gate-behavior-by-dialect` | behavior change | `behaviorVariants` per edition; same snippet, different expected output |
| `none` | — | informational; no matrix row |

**Tag-ability (from the survey):** ~30–40 of the 130 rows are mechanically test-able now (new reserved words → Row 32's
16 words; removed constructs → ALTER/EXIT METHOD/OPEN REVERSED; etc.). Intrinsic-function, EC-condition, and
Unicode-table rows need curated snippets. **Highest-value seed rows:** 1, 3, 9, 27, 28, 29, 30, 32, 35, 65, 89, 90 —
they cover ~70% of real dialect collisions and exercise all five gating patterns.

## 5. Corpora

- **Positive — COBOL-85 baseline:** the NIST CCVS85 corpus (`tests/nist/programs` + `tests/nist/valid`, 364 programs)
  is the authoritative 85 positive set. `NistDifferentialTests` passes **DialectLevel 85 explicitly** (intentional —
  the corpus is COBOL-85 and edition-targeting harnesses name their edition, §10 #2). **INV-1 extends this:** run
  each NIST program at 2002/2014/2023 too and
  assert it still compiles (output need not re-match the golden at a later edition unless behavior is edition-invariant,
  which for the nucleus it is — so re-matching the golden is the strong form; start with "still compiles").
- **Positive — per-edition:** `tests/conformance/<ver>/*.cob` + `.out`, auto-discovered by the legacy `ConformanceTests`
  (dir → `DialectMode`). The greenfield needs the analogous discovery. Each program compiles/runs at ≥ its edition.
- **Negative — NEW (the main build):** for each `new-feature-gate` / removed / reserved-word row, a case asserting the
  **rejection diagnostic** at the wrong edition. Proposed layout: `tests/conformance/<ver>/_negative/<name>.cob` +
  `<name>.err` (the expected `CBL####`), discovered alongside the positives.
- **Behavior-variant:** the same snippet with a per-edition `.out` (only where INV-3 actually applies).

## 6. Harness design

**6.1 Make the compiler-under-test edition-parameterized (prerequisite).**
- `CompilerDriver.Options.DialectLevel` already exists and flows to the front-end. **Thread it into the BINDER only**
  (dual-backend discipline, G4: ALL semantics live in the binder/bound tree behind `ICodeGenBackend`; emitters only
  render): an edition-variant behavior is resolved at bind time and carried in the bound tree in edition-resolved
  form, so the Roslyn emitter AND the future Cecil/CIL backend satisfy INV-3 for free. Do NOT pass `dialectLevel`
  into `CSharpEmitter`/`OperandText`/`ConditionRenderer` for semantic decisions (the §3 INV-3 hook; not needed until
  a real behavior-variant appears, but the seam belongs bind-side).
- **`CobolNetCompiler.CompileAndRun` and `NistDifferentialTests` hard-code `DialectLevel: 85`** — add a target-edition
  parameter (the differential `ICompilerUnderTest` gains a dialect). `CobolNetTestBase` already exposes a
  `dialectLevel` param — generalize its use.

**6.2 Port the legacy per-edition model to the greenfield (the core rework).** The legacy already solved this; reuse
the design (do not re-invent — `feedback_singular_pattern`):
- **`DialectConfig` (two-axis, one cached object queried everywhere)** — strictness (`IsStrict`) × version thresholds
  (`IsCobol2002OrLater`…), `FlagsFeaturesRemovedAfter85`, `DisplayName` for diagnostics. Port from
  `CobolSharp.Compiler/Semantics/DialectConfig.cs`.
- **A post-parse `EditionValidator`** (the missing greenfield piece) implementing the **validator pattern** from
  `DialectStrictnessChecks`: the grammar parses the permissive superset; the validator decides accept / warn / reject
  per `(construct, DialectConfig)` and emits the diagnostic. This is where `introducedIn`/`removedIn` gating lives.
- **A `ConstructDialectStatus` registry** (survey recommendation): one table mapping each construct → per-edition
  severity {ERROR | WARNING | SILENT}. The registry is the code-side twin of the construct catalogue (§4) and is
  generated/checked against `VERSION_CHANGE_REFERENCE.md` so the doc and the compiler cannot drift.
- **Version diagnostics:** wire the already-defined-but-unused `CBL3501/3502` (greenfield) + add "feature requires
  COBOL-YYYY" / "removed in COBOL-YYYY" codes, mirroring legacy `CBL3601/3602/3607/3611…3618`.

**6.3 The matrix test (the payoff).** A parameterized xUnit `[Theory]` over the catalogue × editions:
```
[Theory] [MemberData(nameof(MatrixCases))]   // (caseId, targetEdition)
public void Construct_MatchesEditionExpectation(string caseId, int edition) {
    var case = Catalogue[caseId];
    var expected = ExpectedOutcome(case, edition);     // the f(case,V) of §2
    var actual = CobolNet.CompileAndRun(case.snippet, edition);
    if (expected.Rejected) AssertRejectedWith(actual, expected.Diagnostic);
    else { AssertCompiles(actual); if (case.behaviorVariants is {}) AssertOutput(actual, expected.Output); }
}
```
plus the INV-1 continuity property test (NIST corpus × {2002,2014,2023}) and a negative-corpus discovery test.

When the Cecil/CIL backend lands (G4, `--backend cil`), INV-3 behavior-variant cells run per `--backend` (they
exercise codegen); INV-1/INV-2 gating cells are front-end-only and run once — keep the harness's compile-and-run
seam backend-parameterizable.

**6.4 Negative-test harness (greenfield):** port `DiagnosticTestBase.GetDiagnostics(source, edition)` +
`AssertHasDiagnostic(code)` / `AssertNoDiagnostic(code)` to the greenfield (it has no diagnostic-assertion harness
today — only output differential). Reuse the legacy `FlaggingConformanceTests` shape for obsolete-element flagging.

## 7. The matrix is the worklist

Because the greenfield does **no** edition gating yet, the matrix starts mostly **RED** for V ≠ 85. **Each red cell is a
version-gating task, driven by its reference-doc row** — the matrix is simultaneously the worklist and the acceptance
test for the gating implementation (TDD). "Done" = the row's cells are green at every edition. This makes the
`VERSION_CHANGE_REFERENCE.md` `Status` column actionable: `TODO` → `done` as cells go green.

## 8. Phased rollout (the "substantial rework")

- **Phase 0 — harness scaffold ✅ DONE (DEVLOG 519).** `tests/Cobol.Net.Tests.Conformance/VersionMatrixTests.cs`
  stands up the matrix `[Theory]` over (construct × edition) with the computed `f(case,V)` and an inline seed catalogue,
  plus the INV-1 continuity `[Theory]`. Proven end-to-end on current greenfield capability: **introduction-gating both
  directions** (DELETE FILE, introduced 2023 — rejected at 85/2002/2014, compiles at 2023) and **continuity**
  (NC101A/NC211A/NC136A compile at later editions). 13 cells green; conformance 294→307. (ALTER was the design's
  example, but ALTER is a lexer token with no greenfield statement rule — removed-construct gating needs the
  EditionValidator, Phase 2; DELETE FILE is the cleaner proof since it is fully grammar-gated AND compiles at its intro
  edition.) **Decision #2 ✅ IMPLEMENTED (DEVLOG 520):** the `CompilerDriver.Options.DialectLevel` default is now
  **2023** (`src/Cobol.Net.Compiler/CompilerDriver.cs`), pinned by a `CompilerDriverTests` regression test; the CLI
  `--std` defaults to 2023 and `--nist` without an explicit `--std` targets 85 (`src/Cobol.Net.Cli/CliOptions.cs`).
  Still to scaffold (Phase 1): the canonical `constructs.json`, the greenfield diagnostic-assertion harness, threading
  `DialectLevel` into the binder (bind-side only — see §6.1: per the dual-backend discipline, emitters render, they
  never decide edition semantics).
- **Phase 1 — seed + continuity.** Encode the ~12 highest-value rows (§4); add the INV-1 continuity property test
  (NIST × later editions, "still compiles unless removed"). This catches the biggest regressions immediately.
- **Phase 2 — backfill + implement gating.** Grow the catalogue across all mechanically-testable rows; build the
  `EditionValidator` + `ConstructDialectStatus` registry; turn red cells green per row. Each new feature ships its
  matrix rows (extends `feedback_conformance_tests_per_feature`).
- **Phase 3 — negative corpus + flagging complete.** Full `_negative/` corpus; obsolete/archaic/new-reserved-word
  flagging; behavior-variant rows where INV-3 applies; auto-check the registry against the reference doc (drift guard).

## 9. Reuse / do-not-duplicate map

| Need | Existing (reuse / port) |
|---|---|
| Edition config object | legacy `DialectConfig` (two-axis) |
| Accept/warn/reject-by-edition validator | legacy `DialectStrictnessChecks` (validator pattern) + `ControlFlowBinder` gating |
| Per-edition positive corpus discovery | legacy `ConformanceTests` (`tests/conformance/<ver>/`) |
| Obsolete-element flagging conformance | legacy `FlaggingConformanceTests` + `CompileNistDiagnostics(name, dialect)` |
| Diagnostic-assertion harness | legacy `DiagnosticTestBase.GetDiagnostics/AssertHasDiagnostic` |
| 85 positive + differential | `NistDifferentialTests` + the `ICompilerUnderTest` differential harness (add a dialect param) |

## 10. Decisions (owner-resolved 2026-06-09 + remaining defaults)

1. **Removed-construct outcome — RESOLVED:** **ERROR under a strict `--std NNNN`; WARNING under the permissive
   `Default`/`--nist` mode** (mirrors legacy `FlagsFeaturesRemovedAfter85`). The matrix's reject cells for removed
   constructs assert an error at strict editions ≥ `removedIn`, a warning under permissive.
2. **Default `--std` — RESOLVED:** **COBOL-2023** (the latest) when no `--std` is given. ⚠ This DIFFERS from
   the legacy default (`StrictCobol85`): the greenfield `CompilerDriver.Options.DialectLevel` default is now 2023
   (✅ implemented, DEVLOG 520). Consequence: an unflagged compile of legacy source may hit new-2023 reserved words / removed
   constructs by default (that is intended — newest-standard-by-default). The test harnesses that target a specific
   edition (NIST at 85, the differential harness, per-edition conformance) **pass the edition explicitly**, so the
   default flip does not affect them. ✅ Implemented (DEVLOG 520): CLI `--std` defaults to 2023; `--nist` without an
   explicit `--std` targets 85 (the CCVS corpus's edition). The permissive/strict axis (the legacy `Default` mode)
   does not exist in the greenfield yet — it arrives with the §6.2 `DialectConfig` port; until then the strict-vs-
   permissive split in #1 is design-forward, not current behavior.
3. **Next step — RESOLVED:** build **Phase 0** now (this session).
4. **INV-1 strong vs weak form:** "still compiles" (weak) vs "re-matches the 85 golden" (strong) at later editions.
   *Default:* weak first (compiles), strengthen to golden-match where behavior is edition-invariant.
5. **Catalogue ↔ registry source of truth:** make ONE **structured data file** canonical — e.g.
   `tests/version-matrix/constructs.json` (or `.yaml`) holding every catalogue entry (§4) with its edition metadata.
   BOTH the human-readable `VERSION_CHANGE_REFERENCE.md` table AND the in-code `ConstructDialectStatus` registry are
   derived/diffed from that file (a CI drift check). The "cannot drift" guarantee must **not** parse markdown — the doc
   is a rendering of the data, not the source.
