# COBOL.NET — Next-Session Kickoff Prompt

*Drive ISO/IEC 1989:2023 implementation **and** validation to completion — one large, autonomous, maximally-parallel
session. (This file IS the prompt; updated 2026-06-07, DEVLOG 443.)*

---

## 0. Your mission this session

You are continuing a multi-session build of a **commercial-quality, production-level, decades-sustainable, full
ISO/IEC 1989:2023 COBOL compiler** — `CobolSharp` (to be renamed **COBOL.NET**, exe `cobol.exe`), written in **C# 14 on
.NET 10**, compiling COBOL → **.NET CIL via Mono.Cecil**.

**Continue the COBOL-2002 → 2014 → 2023 implementation AND its conformance validation from the current state through to
completion, in this one (likely large) autonomous session, with maximum practical parallelism.** There is **no
backward-compatibility constraint** — break, rewrite, and re-architect anything when the cleanest long-term design
demands it (quality + longevity beat minimal blast radius, always). Run **continuously**; with pending work, continue
immediately — never end a turn to "wait." Only stop for a genuine **owner-only** decision (and prefer a sensible
default + proceed).

## 1. Read first (in order)

1. **`docs/MASTER_PLAN.md`** — THE top-level SSOT + execution playbook (North Star, phased roadmap A–F, parallelism
   map §4, grounded assessment §10, early-resolve decisions §11).
2. **`docs/DOC_INDEX.md`** — the map of all 126 docs (subject · type · maintenance guide). Use it to find the right
   reference fast, and keep it in sync.
3. **`docs/ISO2023_CONFORMANCE_PLAN.md`** — THE conformance work-breakdown (ranked M2/M3/M4 + execution waves). **This
   is your worklist. Execute it; do NOT re-run the gap analysis. Tick items in §3 and update §1 status as work lands.**
4. **`docs/DATA_MODEL_ARCHITECTURE.md`** (the typed-native ADR — settled) + **`docs/RECORD_STRUCT_STORAGE_DESIGN.md`**
   (the 7-stage migration roadmap).
5. **`docs/OO_IMPLEMENTATION_DESIGN.md` §6.6** — the turnkey OO semantic/emit plan (your first big feature, B1).
6. **`PROMPT.md`** — non-negotiable architectural doctrine + the anti-pattern catalog.
7. Recent **`DEVLOG.md`** (entries 439–443) — exactly what just landed and why.

## 2. Current state (honest baseline — 2026-06-07, DEVLOG 443)

- **Stack:** **.NET 10 / C# 14**. **Backend: CIL-only via Mono.Cecil** (a Roslyn C# backend is a FUTURE additive
  Stage-5 option — Cecil stays the shipping backend + the differential oracle; deferred beyond OO). **Pointers = ONE
  `ManagedPointer`** (GC-tracked; no native heap / no `unsafe` / no 8-byte handle / no `PointerRegistry` — settled).
- **Guard ALL GREEN: 1204 unit / 511 integration / 364 NIST** (was 1196/509 pre-session: +8 CLI-dialect unit tests,
  +2 OO conformance). Iterate with **`bash scripts/guard-fast.sh`** (~3.3 min, parallel, **proven byte-identical**
  to the serial `scripts/guard.sh` via `scripts/guard-verify.sh`).
- **Phase A DONE** (DEVLOG 445–446): CLI `--standard` verified to reach the parser `DialectLevel` + a regression net;
  CI enabled (`.github/workflows/build-and-test.yml` gates the full guard on push/PR); parallel-dev = patch-integrate.
- **M1 (COBOL-85) COMPLETE** — NIST CCVS85 (364 baselines) is the '85 validation backbone.
- **Data-model migration CORE done through Stage-4** (gated behind `EnableTypedFields`, **default OFF** → corpus
  byte-identical; each flip pinned by a flag-on≡flag-off differential test): character→`string`, numeric→`long`/
  `decimal`, flat+nested groups→`record struct`, fixed OCCURS→`T[]`, pointers→`ManagedPointer`. Every byte-trigger
  (REDEFINES/RENAMES/edited/file/EXTERNAL/LINKAGE/ref-mod/ODO) correctly stays byte.
- **OO: grammar DONE + SLICES 1–2 DONE end-to-end** (DEVLOG 447–448): `CLASS-ID` → an instance .NET type with a
  per-instance `ProgramState`, `INVOKE class "NEW" RETURNING o`, `INVOKE o "method" USING … RETURNING …` (the CALL
  `ManagedPointer[]` ABI — an OO method is an instance "Entry"; static cross-type resolution), `USAGE OBJECT
  REFERENCE` storage, PERFORM inside an OBJECT method; per-instance state proven (two objects accumulate
  independently). Conformance `oo_hello` + `oo_method_perform` + `oo_method_args`; slice-1 adversarially reviewed
  (non-OO corpus byte-identical). **NEXT OO = slice 3 (INHERITS FROM + SELF/SUPER), then 4 FACTORY / 5 PROPERTY /
  6 polymorphism** (`docs/OO_IMPLEMENTATION_DESIGN.md` §5). Single-method-per-class today; slice-2 USING = BY REFERENCE.
- **Architecture refactors already DONE** (do not treat as god classes): **M001** (IR-expression hierarchy),
  **M003** (`CilEmitter` → 11 focused emitters + `EmissionContext`), **M004** (`BoundTreeBuilder` → 9 focused binders),
  9 lowering classes.
- **M2 already partly landed** (see ISO2023_CONFORMANCE_PLAN): free-form source + `>>` directives + conditional
  compilation, UDF, NATIONAL (UTF-16), BOOLEAN/BIT, POINTERS, FLOAT-*/BINARY-*, INSPECT BACKWARD, ROUNDED MODE,
  `GOBACK RETURNING`, etc. — each conformance-tested under `tests/conformance/2002/`.
- **Docs** consolidated to 126 (one canonical per subsystem) + `DOC_INDEX.md`; design-references carry status banners.

## 3. The goal and the work sequence

**Goal:** every in-scope ISO-2023 feature **implemented + conformance-tested**; `cobol --standard cobol2023 prog.cob`
compiles to a working .NET assembly; the guard is all-green including the full M2/M3/M4 conformance corpus;
`ISO2023_CONFORMANCE_PLAN` §3 fully ticked.

Work in dependency order (high-level in MASTER_PLAN §3; exact items in ISO2023_CONFORMANCE_PLAN §3):

**Phase A — ENABLE MAXIMUM PARALLELISM (do this FIRST; ~hours).** The throughput lever for everything after.
- Stand up a reliable **parallel-dev harness**. In this repo `Workflow isolation:'worktree'` branches from a **STALE**
  commit (memory `feedback_worktree_workflows_stale`) — do **not** rely on it. **Default:** a *"agents implement on
  isolated copies and return patches; the main loop integrates them onto `main` one-at-a-time, guard-gated"* pattern.
  Prove it by building 2 independent features concurrently and integrating both guard-green.
- Fix the **CLI dialect bug**: `--standard cobol2002|2014|2023` may not reach the parser `DialectLevel` (only the
  conformance-harness path is known-good). Verify and fix so CLI users get the right feature set; add a regression test.
- **Enable CI**: flesh out `.github/workflows/build-and-test.yml.disabled` (already bumped to SDK `10.0.x`) to gate the
  guard on every push.

**Phase B — FINISH THE DATA-MODEL MIGRATION (mostly sequential — shared core).**
- **B1 — OO semantic/emit** (turnkey `OO_IMPLEMENTATION_DESIGN.md` §6.6; this is BOTH the largest M2 feature AND the
  Stage-4 OO step): route `classDefinition` through the existing semantic/binder (reuses the whole layer — no new
  `ClassSymbol` machinery); emit the class with **per-instance `ProgramState`** (one chokepoint:
  `EmissionContext.StateIsInstance`, ~`CilLocationEmitter:476` — verify) + a constructor (NEW) + instance methods;
  **bind + emit `INVOKE`** (`newobj`/`callvirt`, cross-type resolution via a two-pass / assembly-wide type+method
  registry); `OBJECT REFERENCE` storage. Ship a `tests/conformance/2002/` OO test in the same commit. Then OO slices
  2–6 (USING/RETURNING; INHERITS + SELF/SUPER; FACTORY; PROPERTY; polymorphism + EC-for-OO) — each guard-green +
  conformance-tested, parallelizable where independent.
- **B2 — Roslyn C# backend:** DEFERRED (owner: proceed later; Cecil stays shipping + oracle). Not required for
  conformance — skip unless conformance is done and time remains.
- **B3 — finalize:** once the typed flips are complete, decide/flip `EnableTypedFields` ON by default, island the byte
  engine (`RECORD_STRUCT_STORAGE_DESIGN.md` §1.6), prove the whole corpus stays correct, remove dead gated-OFF paths.
  (Can follow Phase C.)

**Phase C — FULL ISO-2023 CONFORMANCE (the bulk; MAX PARALLELISM).** Execute `ISO2023_CONFORMANCE_PLAN` §3. Independent
features (no shared-core conflict) fan out via the Phase-A harness, each integrated guard-green + a
`tests/conformance/<ver>/` test. Recommended order (value/tractability):
- **M2 (2002):** OO (B1) → **FILE-2002** (FILE STATUS as a variable, OPTIONAL, file sharing) → **arithmetic OPTIONS**
  (intermediate rounding / scale-preservation) → remaining **preprocessor** (PRE-1) → **UDF** completion →
  **VALIDATE** statement (+ validation clauses) → **EC / exception model** (the `EC-*` category codification, ON
  EXCEPTION, routing to declaratives/USE) → **SCREEN SECTION** (the 12-doc terminal design set — one owner-gated Q on
  form-level ACCEPT semantics: defer Screen to late Phase C and ask then) → **JSON/XML PARSE/GENERATE lowering +
  runtime** (the grammar overlay exists; emit + runtime are design-only).
- **M3 (2014):** dynamic-capacity tables (OCCURS DYNAMIC), TYPEDEF / user-defined types, file sharing & locking,
  Report-Writer-as-standard, function-pointers, misc.
- **M4 (2023):** new intrinsics, enhanced bit/boolean data, the remaining 2023 deltas; complete per-version gating.
Every post-'85 feature: **spec-cited** (`specs/ISO_COBOL.md`), **version-factored** grammar, **conformance-tested in
the same commit**, **adversarially reviewed**.

**Phase D (interleave):** targeted architecture cleanup — excise the dead overlay grammars
(`CobolParserOO/JsonXml/Generics/Dialect.g4`) + `RecordLayoutBuilder` vestiges; finish the single-PIC-semantics
pipeline. (The big "product surface" — debugger/LSP/packaging/security/perf — is Phase E, out of scope for
*conformance* completion; note gaps but don't block on them.)

**Phase F — rename** `CobolSharp` → **COBOL.NET** (exe `cobol.exe`) only after full conformance + a quality gate.

## 4. Operating rules (non-negotiable)

- **The guard is law.** `guard-fast.sh` GREEN before every commit; nothing lands red. After any change to the test
  corpus or the guard's grouping, re-prove equivalence with `guard-verify.sh`.
- **A conformance test ships with every post-'85 feature, in the same commit** — `tests/conformance/<2002|2014|2023>/
  <name>.cob` + `.out` (auto-discovered by `ConformanceTests`, run inside the guard). This is the post-'85
  NIST-equivalent (memory `feedback_conformance_tests_per_feature`).
- **A DEVLOG entry per commit** (memory `feedback_devlog_per_commit`); tick `ISO2023_CONFORMANCE_PLAN` §3 + update its
  §1 status/NEXT-UP. **Do NOT create new resume/handoff/plan docs — update the existing ones** (THIS file, MASTER_PLAN,
  the conformance plan, DEVLOG, DOC_INDEX).
- **Grammar discipline:** version-factor each post-85 feature into a `Core/CobolXXXX.g4` fragment with a minimal
  `{isYYYY()}?`-gated hook in the core rule; add rules **INCREMENTALLY, guard-fast after EACH**; never inline a
  post-85 feature into the COBOL-85 base (memory `feedback_grammar_version_factoring`; inlining all of OO at once once
  regressed ~9 NC tests). A clean ANTLR regen needs `rm -rf src/CobolSharp.Compiler/Generated` (the timestamp check can
  ship a corrupt partial generate). `is2002()/is2014()/is2023()` = `DialectLevel >= N` in `Parsing/CobolParserCoreBase.cs`.
- **Adversarially verify** every non-trivial feature — a 2–3-agent skeptic panel catches the silent-corruption a
  conformance test misses (the dispatch-site / `IsGroup`/`Category` mis-classification class of bug).
- **Doc hygiene:** one canonical per subsystem (see `DOC_INDEX.md`); when you change a subsystem, update its
  design-reference + its `DOC_INDEX` row + keep its status banner accurate; a NEW subsystem ⇒ a new doc (with a status
  banner) + a new index row. Reference docs **stand on their own** — no provenance / dead-end / deleted-doc mentions.
- **No stale facts:** `.NET 10` / `C# 14`, CIL-only via Mono.Cecil, one `ManagedPointer`. Never reintroduce `.NET 9` /
  `C# 13` / custom-VM / 8-byte pointer handle / `PointerRegistry` / `CobolDataPointer` / "CilEmitter god class".

## 5. Parallelism playbook (maximize it)

- **Always parallel (safe, read-only):** investigation/mapping, design panels, adversarial review — fan out via
  `Workflow`. No isolation needed.
- **Parallel feature implementation** (the big lever): once the Phase-A harness is up, build independent conformance
  features concurrently and integrate them onto `main` **one-at-a-time, guard-gated**.
- **Sequential spine on `main`:** anything touching the grammar, `CilEmitter`, `Binder`, `SemanticBuilder`, or the
  pipeline — serial, kept tight by the fast guard, made confident by parallel review.
- **Workflow dispatch lesson (memory `feedback_workflow_agent_dispatch`):** when fanning out N agents over a
  partitioned worklist, give **each agent its OWN input file** (e.g. `mc0.json … mcN.json`) — **never** "read element
  `[k]` of a shared array" (agents miscount → double-process / skip; this cost rework in DEVLOG 442). Write unicode
  slices via Python; **reconcile returned results against the worklist** before acting; prefer whole-file Writes over
  blind appends; agents editing **disjoint** files need no worktree.

## 6. Settled — do NOT re-litigate

- The typed-native data-model ADR (owner co-authored + adversarially reviewed).
- Pointers = `ManagedPointer`; `PointerRegistry` REJECTED; no 8-byte handle.
- Backend CIL-only via Mono.Cecil; Roslyn = future additive Stage-5 (Cecil = oracle), deferred beyond OO.
- **High-subset only** (NO `--subset` flag); CM / DB / SG modules are **parse + flag only** (no runtime).
- Target the **latest .NET / C#** (currently .NET 10 / C# 14; memory `feedback_target_latest_dotnet`).

## 7. Early-resolve decisions (settle in the first hour; defaults given — MASTER_PLAN §11)

1. **Parallel-dev harness** — *default:* the patch-integrate pattern (don't trust worktrees here).
2. **CI** — *default:* enable the existing template in Phase A.
3. **Screen form-level ACCEPT semantics** (one-field-at-a-time vs whole-form loop) — owner Q; *default:* defer Screen
   to late Phase C and ask then; everything else proceeds without it.
4. **Test-maturity targets** + fuzz oracle — *default:* use the existing internal differential oracles; GnuCOBOL
   optional.
5. **Parallel conformance-test authoring** — author in disjoint per-feature dirs, integrate one-at-a-time guard-gated.

## 8. Definition of done

- Every in-scope ISO-2023 (M2 + M3 + M4) feature **implemented + conformance-tested**; `ISO2023_CONFORMANCE_PLAN` §3
  fully ticked.
- `cobol --standard cobol2023 prog.cob` → a working .NET assembly; `--standard {cobol85|cobol2002|cobol2014|cobol2023}`
  selects the correct feature set + per-version diagnostics.
- **Guard ALL GREEN:** 1196+ unit / 509+ integration / 364 NIST **+ the complete M2/M3/M4 conformance corpus**.
- Typed-native flips complete + the `EnableTypedFields`-on decision made; byte engine islanded.
- **Honest scope note:** full ISO-2023 is large. **Drive to completion, but stay guard-green and resumable at EVERY
  commit** so no progress is ever lost. If you cannot finish, leave `ISO2023_CONFORMANCE_PLAN` §1 status + the latest
  DEVLOG entry pointing **exactly** at the next item to pick up.

## 9. Quick reference

- **Iterate:** `bash scripts/guard-fast.sh` (~3.3 min). **Authority:** `bash scripts/guard.sh`. **Equivalence proof:**
  `bash scripts/guard-verify.sh`.
- **Build:** `dotnet build CobolSharp.sln` — .NET 10 SDK is installed; `TreatWarningsAsErrors=true` (a new C# 14
  analyzer warning fails the build). **Clean ANTLR regen:** `rm -rf src/CobolSharp.Compiler/Generated`.
- **Tests:** post-'85 → `tests/conformance/<ver>/`; COBOL-85 → `tests/nist/`. Spec authority → `specs/ISO_COBOL.md`
  (submodule: `git submodule update --init --recursive`).
- **Branch:** `main` — this project commits work **directly to `main`, guard-gated** (the established convention;
  parallel features integrate one-at-a-time).
- **Pipeline:** Preprocess → ANTLR Lex/Parse → `SemanticBuilder` → `StorageLayoutComputer` → `Binder` (bound tree) →
  Lowering (IR) → `CilEmitter` (CIL via Mono.Cecil).
