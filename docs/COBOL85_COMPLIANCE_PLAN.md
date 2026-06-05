# CobolSharp — Plan to 100% COBOL-85 (ISO/IEC 1989:1985) Compliance & Implementation

Status: 2026-06-04 (DEVLOG 325). Guard GREEN: 1040 unit / 347 integration / **364 NIST baselines** of 459
programs. Authoritative execution plan; supersedes prior planning notes. Scope decision **locked by owner**
2026-06-04 (see §1).

> **This is Milestone 1 of [`docs/MULTIVERSION_ROADMAP.md`](MULTIVERSION_ROADMAP.md)** — the overarching mission
> is one compiler supporting **every ISO COBOL standard 1985→2023, selected by the `--standard` CLI option**.
> COBOL-85 is first only because NIST CCVS85 is the sole external conformance suite. The version engine (M0) and
> the forward versions (M2 COBOL-2002 · M3 COBOL-2014 · M4 COBOL-2023) live in the roadmap.

---

## 1. Scope — what "100% COBOL-85" means here (LOCKED)

The end goal is a **multi-version COBOL compiler (ISO 1985 → 2023), dialect-gated.** COBOL-85 is the first
milestone because **NIST CCVS85 is the only conformance suite that exists** for any COBOL version. The owner's
locked decision (2026-06-04):

- **LIVE feature set** (present in some version 1985→2023) → **fully implement + validate.** This is the 8 core
  modules (Nucleus, the four I-O modules, Inter-Program, Sort-Merge, Source-Text/COPY-REPLACE, Intrinsic
  Functions) **+ Report Writer** (removed in 2002 but **re-added in 2014, live in ISO 2023 §A.4.11**).
- **REMOVED-after-'85 features** (Debug module, Segmentation, Communication, obsolete elements like `ALTER` /
  `GO TO` without a procedure-name / `ENTER`) → **parse + dialect-flag ONLY, no runtime.** Grammar ACCEPTS them
  under `--standard cobol85`; `DialectStrictnessChecks` flags-them-as-removed under `--standard cobol2002+`.
  Rationale: building their runtime serves only the '85 dialect, is non-conformant in every later version, and
  directly violates this project's "zero legacy dead-weight / decades of maintenance" doctrine (PROMPT.md).

**This is a legitimate conformance statement, not a compromise.** COBOL-85 is defined as functional *modules*,
each implemented at a declared *level*; Communication, Debug, and Segmentation are modules a processor may
implement at the null ("not provided") level while remaining a conforming COBOL-85 processor at a stated profile.
NIST CCVS85 certification was always performed against a declared profile. **Our declared profile: all mandatory
modules + Report Writer fully implemented; Communication / Debug / Segmentation parsed-and-diagnosed (null
runtime level).** (The exact '85 module/level conformance text should be confirmed before publication — the repo
carries only the ISO 2023 spec; the '85 conformance wording is inferred.)

**Decisive supporting evidence (found 2026-06-04):** the *entire* NIST Communication module is flagging-only —
**all 9 CM programs are `…M` modules** (CM101M…CM401M), zero executable `…A` programs. Parse+flag is therefore
not merely the pragmatic choice for CM; it is the *only* thing NIST ever tests for that module. WS-DIALECT banks
the complete CM suite.

---

## 2. Current state — the honest ledger (verified 2026-06-04)

`scripts/compliance.sh` reports **364 / 416 = 87%** of live-target programs baselined. **That denominator
understates true completion**, because the 52 unbaselined live programs are *all* documented exclusions, not
missing features. Verified this session by suite:

| Suite | Present | Baselined | Unbaselined — and why each is accounted for |
|---|--:|--:|---|
| **NC** Nucleus | 95 | 93 | NC214M (non-deterministic ACCEPT FROM DATE/TIME), NC303M (flagging `…M`) |
| **IF** Intrinsic | 45 | 42 | IF401M/402M/403M (flagging `…M`, no CCVS report) |
| **SM** COPY/REPLACE | 17 | 15 | SM301M/401M (flagging `…M`) |
| **IC** Inter-Program | 47 | 23 | **20 callee-halves** (`PROCEDURE DIVISION USING` — run only when CALLed; verified IC115A/206A/109A/110A/111A/205A/210A/211A/212A all have `USING`) + IC116M/117M/118M/401M (flagging) |
| **SQ** Sequential I-O | 85 | 83 | SQ303M/401M (flagging `…M`) |
| **OBSQ** Seq (obsolete) | 4 | 3 | OBSQ3A (NO_OUTPUT producer feeding a chain) |
| **IX** Indexed I-O | 42 | 40 | IX301M/401M (flagging `…M`) |
| **RL** Relative I-O | 35 | 32 | RL301M/401M (flagging `…M`), RL212A (NO_OUTPUT producer) |
| **ST** Sort-Merge | 40 | 29 | 10 NO_OUTPUT producers (build/sort shared files) + ST301M (flagging) |
| **RW** Report Writer | 6 | 4 | RW301M/302M (flagging `…M`) |
| — removed (DB/SG/CM/OBNC/OBIC) | 43 | 0 | parse+flag scope; see §3 WS-DIALECT |
| — EXEC85 | 1 | 0 | excluded (non-standard test driver) |

**Conclusion — the executable baseline target is, in practice, COMPLETE.** Every live module passes every
executable program that can stand alone. **There are no remaining "make a failing NIST `…A` test pass" gaps in
the live set.** What remains for true 100% **compliance + spec implementation** is three things NIST baselining
does not by itself prove:

1. **Flagging conformance** — the `…M` modules verify the compiler *flags* obsolete / non-conforming constructs.
   The `…M` modules that emit a CCVS report are already baselined; the **no-report flagging `…M` modules are
   currently merely *excluded*, never actually verified to flag correctly.** ~19 live-suite + 20 removed-module
   `…M` modules = **~39 flagging behaviors unverified.** This is the single largest concrete "NIST says we must
   flag X" gap.
2. **Spec completeness** — NIST exercises a *subset* of COBOL-85. Many spec features / option-combinations /
   PICTURE forms / statement formats are implemented-but-NIST-untested, or partially implemented. "100%
   implementation of the spec" requires a test for each, authored where NIST is silent.
3. **Verification rigor** — proving each of the 52 exclusions above is *legitimately* an exclusion (not a hidden
   bug), as an auditable ledger.

---

## 3. Workstreams

Proven model: **parallel design / audit → worktree-isolated parallel implementation → sequential, guard-gated
integration onto `main`.** Every leniency dialect-gated; every commit ≥1 DEVLOG entry; guard stays ALL GREEN.

### WS-VERIFY — Exclusion ledger (foundational, read-only)
Audit all 52 unbaselined live programs; prove each is exactly one of {flagging `…M` / NO_OUTPUT producer /
`PROCEDURE DIVISION USING` callee-half / non-deterministic}, or surface it as a hidden gap. Cross-check each ST
producer feeds a baselined consumer; each IC callee-half is CALLed by a baselined caller.
**Deliverable:** `docs/EXCLUSION_LEDGER.md` (one line per program → class + evidence). **Done:** every live
program is in exactly one accounted-for class. *Independent; run first; small.*

### WS-FLAG — Flagging-conformance harness + live-suite flag diagnostics
**REFINED 2026-06-04 (Wave-2 investigation, see DEVLOG 328).** The Wave-1 manifests revealed the live `…M`
modules are **two distinct flag classes**, and CobolSharp's conformance profile decides which apply:

- **Class A — subset-level flags** ("NON-CONFORMING STANDARD — feature X is above the minimum/Level-1 subset"):
  IF401M/402M/403M (high-subset intrinsics), IX301M/401M, RL301M/401M, SQ401M, SM301M/401M, and the ST/IC
  subset `…M`. These flag *standard* features (INDEXED org, ACCESS RANDOM, NOT INVALID KEY, SELECT OPTIONAL,
  RESERVE, COPY, the intrinsic library, …) that are only "non-conforming" **relative to a claimed lower subset.**
  **CobolSharp implements the full language (HIGH subset), so these features are native and emitting ZERO flags
  is the *correct* conformance behaviour.** ⇒ **N/A at high subset — documented conformance-profile exclusion**
  (the same kind of declared-profile call as DB/SG/CM; satisfying them would require an archaic
  `--subset minimum|intermediate` mode that flags above-subset features — see §1, owner decision pending).
- **Class B — obsolete-element flags** ("OBSOLETE — removed after COBOL-85"): **NC303M** (DATE-COMPILED, ALTER,
  bare/altered GO TO), **SQ303M** (MULTIPLE FILE TAPE, OPEN … REVERSED). A conforming compiler flags obsolete
  elements regardless of subset. ⇒ **IMPLEMENT** (an obsolete-element *warning* under `--standard cobol85`,
  distinct from the `cobol2002+` *deletion error* already emitted for ALTER/bare-GO-TO via CBL3601/CBL3605).

1. **Harness** (`tests/nist/flagging/` + a guard hook): compile each Class-B `…M` under `--standard cobol85`
   and assert its expected OBSOLETE diagnostics appear (manifest in `docs/FLAG_MANIFESTS.md`); assert Class-A
   `…M` emit **no** subset flag at high subset.
2. **Class-B diagnostics:** add obsolete-element flagging — DATE-COMPILED paragraph, ALTER (warn under '85),
   bare/altered GO TO (warn under '85), MULTIPLE FILE TAPE, OPEN … REVERSED. Plus the removed-feature flags
   (CM/DB/SG/OBNC) handled by WS-DIALECT.

**Done:** Class-B `…M` flag exactly their obsolete elements under cobol85; Class-A `…M` are documented N/A at
high subset; harness green in guard. *Harness blocks the Class-B diagnostics and WS-DIALECT.*

### WS-DIALECT — Parse + dialect-flag the removed features (no runtime)
Grammar ACCEPTS, under `--standard cobol85`: `USE FOR DEBUGGING` / `DEBUG-ITEM` / `WITH DEBUGGING MODE`;
`CD` / `SEND` / `RECEIVE` / `ENABLE` / `DISABLE` / `PURGE`; section segment-numbers / `SEGMENT-LIMIT`; obsolete
`ALTER` (built) / `GO TO` without procedure-name / `ENTER`. `DialectStrictnessChecks` flags each as removed under
`cobol2002+`. Validated via the WS-FLAG harness → banks the flagging tests: **CM (9), DB `…M` (6: DB103M,
DB301M-305M), SG `…M` (3), OBNC (2)** = 20 removed-module flag tests. The removed-module `…A` tests
(DB101A/102A/104A/105A/201A-205A; SG101A-106A/201A-204A; OBIC1A-3A) **do not baseline** — documented "null
runtime level." (Bonus: OBIC1A-3A use only obsolete verbs that may already work — investigate; baseline if clean.)
**Done:** cobol85 dialect parses every removed construct; cobol2002+ flags each; CM/DB-M/SG-M/OBNC flag tests
pass. *Depends on WS-FLAG harness; parallel with the live-suite diagnostics.*

### WS-SPEC — Spec-completeness corpus (the long pole; fans out by module)
Walk the ISO COBOL-85 live feature set module-by-module; for every feature / option / format / PICTURE form /
edge case NIST does not exercise on a *passing* path, author a focused test (`tests/nist/extra/<module>/` and/or
integration tests) and make it pass. Elevates "NIST-validated" → "spec-implemented." Sub-streams (parallel):
WS-SPEC-NC, -SEQ (SQ/RL/IX), -IC, -ST, -SM, -IF, **-RW** (complete the live Report-Writer spec NIST under-tests:
**SUM counters, control breaks (`CONTROL`/`CONTROL FOOTING`), `GROUP INDICATE`, `NEXT GROUP`, `RESET`** — implement
+ test), -DATA (PICTURE / USAGE / REDEFINES / OCCURS-DEPENDING / SIGN / SYNC / class-conditions), -ENV
(SPECIAL-NAMES / ALPHABET / CURRENCY / DECIMAL-POINT / collating). Reference is the repo's ISO 2023 spec, scoped
to '85-level features (a known limitation: no literal '85 text — see §6).
**Done per sub-stream:** a checklist of that module's spec features, each with a passing test. *Largest; mostly
independent; integrate sequentially guard-gated.*

### WS-DASH — Compliance dashboard v2
Extend `scripts/compliance.sh` (or a sibling) to report the **three axes**: baseline % (have), flagging-
conformance % (WS-FLAG/WS-DIALECT manifests passing), and spec-checklist % (WS-SPEC features covered). Single
command answers "% to 100%" and makes regressions visible. **Done:** dashboard reports all three; 100% target is
literal and measurable. *Last; consumes the other workstreams' ledgers.*

---

## 4. Execution waves

1. **Wave 1 (parallel, mostly read-only — safe to run now):**
   - WS-VERIFY (exclusion ledger).
   - WS-SPEC *audit phase* — per-module fan-out producing each module's feature-vs-NIST gap inventory + the
     proposed extra-test list (no code yet).
   - WS-FLAG *harness design* + per-`…M` expected-flags manifest extraction.
2. **Wave 2 (guard-gated implementation):**
   - WS-FLAG live-suite diagnostics + WS-DIALECT parse+flag (parallel; integrate sequentially).
   - WS-SPEC *implementation* — per-module extra tests + any feature gaps surfaced (worktree-isolated agents,
     integrated one-at-a-time, guard-gated). Includes WS-SPEC-RW (SUM / control breaks / GROUP INDICATE).
3. **Wave 3:** WS-DASH; final 100% verification sweep; update PROJECT_PLAN/resume-prompt/memory.

**Long horizon (separate track, not part of "100% '85"):** WS-FORWARD — the version/dialect architecture as the
centerpiece, then COBOL-2002/2014/2023 additions (free-form source, user-defined functions, dynamic-capacity
tables, bit/boolean, FUNCTION growth, …) with a **custom** conformance corpus (no NIST suite exists past '85).

---

## 5. Definition of done (100%)

- **Baseline axis:** every in-scope executable live program baselined at 0 `FAIL*` + non-vacuous (✅ already
  met — WS-VERIFY proves it).
- **Flagging axis:** every `…M` module (live + removed CM/DB/SG/OBNC) verified by the WS-FLAG harness to flag
  exactly its expected constructs under the strict dialect.
- **Spec axis:** every live COBOL-85 spec feature has a passing test (NIST where it reaches; `tests/nist/extra/`
  + integration where NIST is silent). Report Writer spec complete (SUM / control breaks / GROUP INDICATE).
- **Measurement:** `scripts/compliance.sh` reports 100% on all three axes.
- **Guard:** ALL GREEN throughout; baselines stay 0 `FAIL*` forever.

---

## 6. Risks & known limitations

- **No literal ISO 1985 text** — the repo carries ISO 2023. The live '85 feature set is a subset of 2023; WS-SPEC
  scopes to '85-level features. The '85-specific *obsolete/removed* classifications (needed for WS-FLAG/WS-DIALECT
  flag wording and the §1 conformance-profile statement) are inferred and should be confirmed against an '85
  conformance reference before publication.
- **Flagging-test expected output** — NIST `…M` modules emit no CCVS report; "pass" = correct *diagnostics*. The
  expected-flags manifests are derived from the test source and may need hand-curation per module.
- **WS-SPEC scope is open-ended** — "every spec feature" is a large surface. The audit phase (Wave 1) bounds it
  into a finite, tracked checklist before any implementation, so progress is measurable and the tail is visible.
