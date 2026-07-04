# COBOL.NET — Edition Validation (the EditionValidator subsystem)

> **Status: AS-BUILT (Wave 1 complete, 2026-07-03 — DEVLOG 583–590; the canonical PLAN it implements is
> `docs/VERSION_TEST_MATRIX_DESIGN.md` "Phase-2 implementation plan" P2.1–P2.7 + the ratified roadmap
> `docs/COMPLETION_ROADMAP_COUNCIL.md` Phase-1/2 amendments).** This is the canonical deep-dive for the
> validation subsystem (`src/Cobol.Net.Compiler/Validation/`). Wave 2–3 remainders are listed at the end.

## 1. What it is

The syntax-side half of the four-compilers-in-one obligation (`--std 85|2002|2014|2023`, default 2023): every
construct carries (1) its full ISO behavior in every edition that HAS it and (2) the correct DIAGNOSTIC in
every edition that LACKS it — not-yet-introduced (COBOLNET0900), reserved spelling (0901), removed (0902),
obsolete/archaic (0903). Severity is two-axis: **strict** (default; removals reject) vs **`--permissive`**
(the documented migration mode, owner decision 4: removals compile with warnings and the pre-removal
semantics).

## 2. Architecture (as built)

- **Channels** (`Binding/EditionContext.cs`, P2.1): `Diagnostics` is ERRORS-ONLY (any entry fails the
  compile); `Warnings` never fails; **`Removed(code,msg)` is THE severity seam** — error strict / warning
  permissive; one policy, every emit site. Carriers: `CompilerDriver.Options.Permissive`,
  `Result.Warnings` (every outcome), CLI `--permissive`, warnings always printed to stderr.
- **The pass** (`Validation/EditionValidator.cs`, P2.2): a visitor over the generated
  `CobolParserCoreBaseVisitor<object?>` (no listener is generated), hooked in `CompilerDriver.Compile`
  between `EditionContext` construction and Emit, **fail-fast before Emit** (removed constructs may have no
  emit path). Syntax-only gating lives here; bind/type-dependent gating stays binder-side — but ALL severity
  routes through `Removed()`/the registry.
- **The band** (`Validation/EditionCodes.cs`, P2.3): 0900 introduction / 0901 reserved word / 0902 removed /
  0903 obsolete-archaic. Pinned pre-band codes kept: 0801/0802 (digit capacity), 0873 (DATA RECORDS),
  0810/0811 (ALTER / bare GO TO), 0882 (CALL ON OVERFLOW) — their sites migrated onto `Removed()` unrenumbered.
- **Reserved words** (P2.4, derivation revised — DEVLOG 585): `scripts/gen-reserved-words.ps1` derives the
  per-edition table from FOUR sources — the in-repo ISO 2023 §8.9 list (authoritative for 2023), VCR row 32
  (the Annex E.2 item-25 additions), and GnuCOBOL's per-standard 85/2002/2014 word lists (curl disk-to-disk
  into the gitignored `.cache/`; GPL files stay out — only derived FACTS with provenance are committed).
  Outputs: `Validation/ReservedWords.Table.cs` + `tests/version-matrix/reserved-words.json`, drift-tested
  both directions. Conservative policy: only `confidence: high` rows reject. ISO Annex E overrides source
  disagreements; continuity interpolation covers single-source gaps; CCVS-conforming usage PROVES a word
  un-reserved (the ORDER override). Consumers go through **`ReservedWordSet`** (the per-unit D9 seam — the
  2023 COBOL-WORDS directive mutates the effective set per compilation group, roadmap Phase 7).
  **The funnel** (`VisitCobolWord`) checks IDENTIFIER occurrences (the whole newly-reserved payload) + the six
  EC-band tokens; the screen/report allowlist band is EXCLUDED pending position-aware checking — the
  permissive grammar can bind those keywords into optional entry-NAME slots (the RW104A COLUMN case).
  ⛔ **Content-filter rule (tripped 4×):** no word list ever transits a conversation stream in any form —
  scripts print counts only; regeneration is disk-to-disk.
- **The registry** (`Validation/ConstructDialectStatus.cs`, P2.5): the in-code rendering of the canonical
  `tests/version-matrix/constructs.json`; `ConstructRegistry.Check(edition, id, where)` is THE gating entry
  point (introduction → error both axes; removal → `Removed()`; obsolete → 0903 warning; dual-obligation
  WINDOW rows use 0900 for the introduction edge and their code for the removal edge). The drift test makes a
  gate unable to land without its matrix row and vice versa. `status: "pending"` rows are catalogued/frozen
  but compile-asserted only when their owning roadmap phase lands (ONE pending mechanism, shared with the
  corpus manifests).
- **Corpus runners** (`CorpusRunnerTests`, Phase-1 shells): per-edition `tests/conformance/<ed>/manifest.json`
  discovery (enabled compile-asserted strict; pending catalogued; integrity facts forbid silent
  non-discovery) + the `tests/conformance/negative/` must-reject corpus (`.cob` + `.err` + a `*> reject-at:`
  edition header). Seeding: Phase-2 W2.

## 3. Wave-1 coverage (live gates)

85→2002 removals (0902): LABEL RECORDS · VALUE OF · DATA RECORDS (FD+SD, pinned 0873; the DataBinder SD site
migrated) · MULTIPLE FILE [TAPE] · MEMORY SIZE · SEGMENT-LIMIT · WITH DEBUGGING MODE (token-scans of the
`computerAttributes` sink) · the five identification comment paragraphs · REMARKS (≥2002 only — CCVS carve-out)
· STOP literal (85 semantics implemented: `BoundStopLiteral` → operator channel/stderr + continue) · OPEN
REVERSED · **the W3-④ notInGrammar batch (DEVLOG 599, VCR Table 7 rows 7.15–7.18): RERUN (parsed-and-ignored)
· ENTER (BoundNop; system-name operands outside the funnel) · USE FOR DEBUGGING (the '85 dual posture —
comment-treated without WITH DEBUGGING MODE [DB103M], compiled-never-triggered with it; DEBUG-* register
references under the switch diagnose 0899 not-implemented, never the false 0901) · section-header
segment-numbers (both header rules)**. 2014→2023: CLOSE WITH LOCK · CALL ON OVERFLOW (binder, 0882). Windows:
EXIT METHOD/FUNCTION (2002→2023). Archaic 0903 @≥2023: EXIT PROGRAM · NEXT SENTENCE. Reserved-word intervals:
COMMIT@2023, RAISING@2002, RECEIVE + END-RECEIVE (85-reserved → free 2002/2014 → re-reserved 2023). Binder
`Removed()` sites: ALTER 0810 · bare GO TO 0811 · CALL ON OVERFLOW 0882.

## 4. The measurable G7 exit criteria (roadmap Phase-1 docs item; Phase 8 audits them as counts/exit codes)

1. **INV-1 permissive:** `scripts/version-continuity-sweep.sh` reports 0 BREAKS (in CI since DEVLOG 590);
   every ≥2002 STRICT failure of an 85-green traces to a recognized edition-band code —
   0801/0802/0810/0811/0873/0875–0879/0882/0893/09xx.
2. **INV-1-STRONG at the default edition:** `COBOLNET_NIST_STD=2023 COBOLNET_NIST_PERMISSIVE=1` over the
   golden run = **349/349 byte-exact** (seeded DEVLOG 588; a G7 exit criterion at Phase 8).
3. **INV-2 both ways:** every constructs.json row's f(case,V) matrix green (strict cells + the removed-
   permissive theory + the obsolete-warning theory); reject cells carry their `expectDiagnostic`.
4. **Census:** ≥357 GREEN on the full NIST census at 85 (the Phase-8 in-repo guard re-basis).
5. **Drift:** both drift disciplines green (registry↔constructs.json; word table↔reserved-words.json).
6. **Negative corpus:** every registry gate ≥1 enabled negative case (Phase-2 W2 seeds; Phase-8 completes
   with the registry-coverage unit test).
7. **§4.2.2:** the selectable conformance-checking suboption ships with OO interfaces (roadmap Phase 3) /
   prototypes (Phase 4c) and joins these criteria then.

## 5. Performance

The validator is one visitor pass over the already-built parse tree: full-pipeline CLI compiles of a large
NIST program (SQ207M) measure ~1.3–1.4 s wall (parse+validate+bind+emit+Roslyn, Debug), and the full-corpus
guard wall time is unchanged across the Wave-1 landing (~3.3 min before P2.1 and after P2.6) — the pass is
noise (feedback_guard_speed satisfied).

## 6. Wave 2–3 remainders (P2.8; roadmap Phase 2)

W2 (parallel agents, disjoint files): the MOVE rows (VCR 1, 92/128) + the MOVE ALL-digit fix (§14.9.25 **SR5**,
integer receiver only — the corrected citation) · the loud-guard silent-misbind sweep (PicInfo silent DISPLAY
fallback, PIC N/E/1, the CallCollectUnits class-unit skip, UsageKeyword strip) + the national/boolean skeleton
· negative-corpus seeding (≥1 case per Wave-1 gate + the reserved-word interval witnesses) · position-aware
checking for the screen/report allowlist band (the RW104A exclusion) · VCR status flips · adversarial review.
W1.5: upgrade the ~24 grammar introduction-gate parse errors to edition-naming 0900 diagnostics (serial or
fragment-merge). W3 (single serialized grammar batch + FULL legacy guard): XOR/EXCLUSIVE-OR regating to 2023
per Annex E · the notInGrammar 85-acceptance set · preprocessor DialectLevel threading (VCR 2/4/94) · the
cobolWord EC-band comment fix · the 2002-corpus edition audit.
