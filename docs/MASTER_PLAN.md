> ⛔ **OBSOLETE / SUPERSEDED (2026-06-09, DEVLOG 521) — pre-PIVOT byte-engine sub-plan. DO NOT FOLLOW.**
> The project PIVOTED (DEVLOG 457) to a blank-slate GREENFIELD rewrite — COBOL → typed-native C# via Roslyn, with NO
> byte engine and NO data-model "migration". This document predates / contradicts that pivot. The live plan is
> `resume-prompt.md` + the SSOT `docs/COBOLNET_DESIGN.md`; multi-edition support is driven by
> `docs/VERSION_TEST_MATRIX_DESIGN.md` against `docs/VERSION_CHANGE_REFERENCE.md`. Retained only as a historical
> record (DEVLOG-article source material).

# COBOL.NET — Master Plan to a Commercial-Quality, Decades-Sustainable, Full ISO-2023 Compiler

> **THE top-level SSOT and autonomous-execution playbook.** Authored 2026-06-07. This is the single entry point: it
> sequences ALL remaining work to reach the goal and tells a fresh autonomous session exactly how to execute it with
> maximum practical parallelism. Detailed per-area plans are sub-documents (§9). Work *from* this document.

## 0. North Star (owner directive, emphatic + repeated)

A **commercial-quality, production-level COBOL compiler, sustainable for DECADES**, with **full ISO/IEC 1989:2023**
conformance. **There is NO backward-compatibility constraint** — this is a brand-new compiler; **break, rewrite, and
re-architect anything** when the cleanest long-term design demands it. Quality and longevity beat minimal blast
radius, always (memory `feedback_commercial_quality_north_star`, `feedback_production_refactor`). No hacks, no
transitional shims, no dead code (PROMPT.md).

**Execution mandate (owner):** the next session must **implement ALL remaining work in one autonomous session, with
maximum autonomy and maximum practical parallelism, driving to the goal.**

## 1. The autonomous-session operating manual (read first)

**Autonomy.** Run continuously; never stop to ask unless genuinely blocked on an owner-only design decision. With
pending work, continue immediately in the same turn — do NOT end a turn waiting on a loop tick
(`feedback_continue_dont_wait`). Commit each guard-green increment with a DEVLOG entry
(`feedback_devlog_per_commit`); every post-1985 feature ships a `tests/conformance/<ver>/` test **in the same commit**
(`feedback_conformance_tests_per_feature`).

**The guard is the law.** Iterate with **`bash scripts/guard-fast.sh`** (~3.3 min, parallel, **proven byte-identical**
to the serial `scripts/guard.sh` via `scripts/guard-verify.sh`). Nothing lands unless the guard is **ALL GREEN**
(1196+ unit / 509+ integration / 364 NIST, 0 FAIL*). After any change to the test corpus or the guard's grouping, run
`guard-verify.sh` to re-prove equivalence. Grammar changes: add rules **incrementally, guard-fast after EACH**, and
**version-factor** them into a `Core/CobolXXXX.g4` fragment with `{isYYYY()}?`-gated hooks — never inline a post-85
feature into the COBOL-85 base (memory `feedback_grammar_version_factoring`; DEVLOG 439). A clean ANTLR regen needs
`rm -rf src/CobolSharp.Compiler/Generated` (the timestamp check can ship a corrupt partial generate).

**Parallelism strategy — maximize it where practical.** Three tiers:
1. **Parallel investigation / design / adversarial review** — ALWAYS use `Workflow` to fan out read-only Explore
   agents (mapping, design panels, multi-lens review). No isolation needed; safe.
2. **Parallel implementation of INDEPENDENT features** — the big throughput lever. **BLOCKER:** in this repo
   `isolation:'worktree'` branches from a STALE commit (memory `feedback_worktree_workflows_stale`), so worktree
   implementation is currently unreliable. **Phase A fixes this** (see §3) — it is the first enabler so the rest of
   the program can run many independent features concurrently, each integrated onto `main` guard-gated.
3. **Sequential spine on `main`** — anything touching shared core files (the grammar, `CilEmitter`, `Binder`,
   `SemanticBuilder`, the pipeline) is implemented sequentially on `main`, guard-gated, with the fast guard keeping
   iteration tight. Use parallel review to keep it confident.

**Confidence discipline.** For any non-trivial change, fan out an adversarial-verify panel (independent skeptics) —
it has repeatedly caught silent-corruption the conformance test missed (the dispatch-site / mis-classification class
of bug). The verdict-diff in `guard-verify.sh` is the model: a mis-grouping surfaces as a detectable RED, never a
false GREEN.

## 2. Current state (honest baseline, 2026-06-08, DEVLOG 456; latest commit `a10bda9`, tree clean)

> ⛔ **OWNER DIRECTIVE (2026-06-08): OO is TYPED-NATIVE per ADR §7 — object data → per-instance typed .NET fields,
> methods → real .NET methods, INVOKE → callvirt; NEVER the byte `State`. The `EnableTypedFields` default-OFF gate is
> migration-safety for the LEGACY corpus, NOT design intent — OO is an always-on consumer of the typed pipeline.**
> (Memory `feedback_oo_typed_native_not_byte`.)

- **Branch `main`, guard ALL GREEN: 1204 unit / 535 integration / 364 NIST.** DEVLOG at entry 456. (Baseline at
  this plan's authoring was 1196/509/364 @ DEVLOG 441; since then: Phase A enablers + OO slices 1–3b + the OO
  typed-native object-data model + multi-method + INVOKE SELF — see §8.)
- **Platform: targets .NET 10 / C# 14** (`Directory.Build.props` `net10.0`/LangVersion 14, `global.json` SDK 10.0.x;
  retargeted from .NET 9 this session, full guard re-proven green on .NET 10 — DEVLOG 441).
- **M1 (COBOL-85) COMPLETE** — the 8 core NIST CCVS modules + Report Writer baselined; NIST CCVS85 is the '85
  validation backbone.
- **#1-priority data-model migration (typed-native .NET), CORE done:**
  - Stage 0/1 numeric substrate (`CobolNum`/`CobolDecimal` over `BigInteger`) + differential oracle; Stage-0 char
    substrate (`CobolString`) + oracle.
  - Stage 2 classifier (`RecordClassificationPass`, A+B+C) complete.
  - Stage 3 typed flips (gated `EnableTypedFields`, default OFF → corpus byte-identical, pinned by flag-on≡flag-off
    differential tests): character→`string`; numeric→`long`/`decimal`; flat+nested groups→`record struct`s; fixed
    OCCURS→`T[]`; every byte-trigger correctly stays byte.
  - **Stage 4 POINTERS COMPLETE** — `USAGE POINTER`/`BASED`/`ADDRESS OF`/`SET ADDRESS OF`/`NULL`/`=`/`SET UP|DOWN BY`/
    `ALLOCATE`/`FREE`, all on ONE managed `ManagedPointer` (no native heap, no `unsafe`, no 8-byte handle). 4
    conformance tests.
  - **Stage 4 OO — TYPED-NATIVE object-data model COMPLETE + multi-method + INVOKE SELF (DEVLOG 447–456):** object
    data → per-instance typed .NET fields (PIC X→`string` 453, PIC 9→`long`/`decimal` 454, `01` group→`record struct`
    + `OCCURS`→typed array 456; byte `State` empty on a fully-typed class; instance dimension via the
    `CilDataEmitter.EmitTypedFieldOwner` chokepoint); N `METHOD-ID`/class (each its own .NET method + exit-bounded
    dispatch; per-method paragraph scope fixes CBL3104) + `INVOKE SELF`→callvirt incl. polymorphic across INHERITS
    (455, COBOL0112 lifted); plus the earlier NEW/INVOKE-USING-RETURNING/OBJECT-REFERENCE/INHERITS+virtual/SUPER
    (447–451, now on the typed path). 3-agent panel → edge gaps fail loud (COBOL0116 SECTION-in-method, COBOL0117
    multi-method-with-params); deferred loud COBOL0111/0113/0114/0115. 9 `oo_*` conformance + `OoTests`. **Next OO =
    (a) subclass own typed object data (lift COBOL0113); (b) per-method LINKAGE (lifts COBOL0117) + typed-field INVOKE
    args; (c) FACTORY; then PROPERTY / universal-ref+EC.** See `docs/OO_IMPLEMENTATION_DESIGN.md` §5.
- **Tooling:** parallel guard (`guard-fast.sh`) ~3.3× faster, proven equivalent.
- **Known debt (the "commercial/decades" gap — detailed in §10).** Note the core is healthier than "god classes
  everywhere": the big decompositions are **already DONE** — M001 (IR-expression hierarchy, zero `BoundExpression`
  leakage), M003 (`CilEmitter` 4083-line god class → 11 focused emitters + `EmissionContext`), M004 (`BoundTreeBuilder`
  → 9 focused binders, 234-line orchestrator). Remaining debt is **targeted**: the byte-engine ↔ typed-model duality
  (endpoint: byte engine becomes a classifier-scoped island), DEAD code (the unwired overlay grammars
  `CobolParserOO/JsonXml/Generics/Dialect.g4`; possibly `RecordLayoutBuilder`), single-PIC-pipeline consolidation, doc
  consolidation, a CLI dialect bug (`--standard cobol2002` may not reach the parser `DialectLevel`), and — the big one
  — the **entire production/product surface that today exists only as design** (debugger, LSP, CI, packaging, security,
  perf, docs; see §10.2).

## 3. The phased roadmap — dependency-ordered, parallelism-maximized

Each phase: **goal · parallel fan-out · sequential spine · exit criteria.** Phases B–E overlap where files don't
conflict (the parallel-dev harness from Phase A makes that safe).

### Phase A — ENABLE MAXIMUM PARALLELISM (the first enabler; do this FIRST)
**Goal:** make parallel feature implementation reliable + fast, so the rest of the program runs many features
concurrently. **Work:**
- **Fix the worktree base** so `Workflow isolation:'worktree'` branches from current `main` (HEAD), not the stale
  e577e32 — OR establish a reliable alternative parallel-dev harness (e.g. a "parallel-produce-patch, sequential-
  integrate" pattern: independent agents implement on isolated copies and return diffs the main loop applies +
  guard-checks one at a time). Investigate the root cause first (parallel Explore), then implement + prove it with a
  2-feature concurrent dry-run integrated guard-green.
- **Fix the CLI dialect bug** (`--standard cobol2002` not reaching `DialectLevel` — verify against the conformance
  harness path) so CLI users get 2002+ features; add a regression test.
- **Stand up CI** (a green-guard gate on every change) — minimal but real.
**Exit:** two independent features built concurrently and integrated guard-green via the harness; CLI honors
`--standard`; CI runs the guard.

### Phase B — COMPLETE THE DATA-MODEL MIGRATION (the #1 priority)
**Goal:** finish the typed-native re-architecture (ADR `docs/DATA_MODEL_ARCHITECTURE.md`). **Sequence (mostly
sequential — shared core):**
- **B1 Stage-4 OO semantic/emit** — implement the turnkey §6.6 plan (`OO_IMPLEMENTATION_DESIGN.md`): route
  `classDefinition` through the existing semantic/binder (reuses the whole layer); emit the class with per-instance
  `ProgramState` (one chokepoint: `EmissionContext.StateIsInstance` at `CilLocationEmitter:476`) + ctor (NEW) +
  instance methods; bind+emit `INVOKE` (`newobj`/`callvirt`, cross-type resolution via a two-pass / assembly-wide
  type+method registry); `OBJECT REFERENCE` storage. Then OO slices 2–6 (USING/RETURNING; INHERITS+SELF/SUPER;
  FACTORY; PROPERTY; polymorphism+EC-OO) — each guard-green + conformance test, parallelizable where independent.
- **B2 Stage-5 Roslyn C# backend** — emit the typed record-structs / pointers / classes as steppable `.cs` behind
  `--backend csharp`, with **Cecil retained as a differential oracle** (run the whole corpus both ways, diff all
  baselines). Flip default to C# only after a sustained green run.
- **B3 Stage-6 finalize** — flip `EnableTypedFields` ON by default; retire the byte engine to its classifier-scoped
  island role (`docs/RECORD_STRUCT_STORAGE_DESIGN.md` §1.6); prove the whole corpus stays correct; remove the
  now-dead gated-OFF byte paths and `RecordLayoutBuilder` vestiges.
**Exit:** the corpus compiles typed-native by default, byte engine is an island, Roslyn backend passes the corpus
diff-clean vs Cecil.

### Phase C — FULL ISO-2023 CONFORMANCE (the M2/M3/M4 catalog — MAX PARALLELISM)
**Goal:** complete every remaining ISO-2023 feature. **Source of truth:** `docs/ISO2023_CONFORMANCE_PLAN.md` §3 (do
NOT re-run the gap analysis — execute it). **Parallel fan-out:** independent features (no shared-core conflict) are
built concurrently in the Phase-A harness, each integrated guard-green + a `tests/conformance/<ver>/` test. The big
subsystems (EC/exception handling, VALIDATE, the remaining file-2002, OPTIONS arithmetic, report-writer edges,
2014/2023 features) are each their own track; sequence only where they share core grammar/emit. Every feature:
spec-cited, conformance-tested, adversarial-reviewed.
**Exit:** every in-scope ISO-2023 feature implemented + conformance-tested; the conformance corpus is comprehensive.

### Phase D — ARCHITECTURE CLEANUP (decades-sustainability — interleave with C; TARGETED, not a teardown)
**Goal:** the codebase satisfies PROMPT.md's canonical-compiler architecture. **Reality (per §10):** the big
decompositions are **already done** (M001 IR-expr, M003 `CilEmitter`→11 emitters, M004 `BoundTreeBuilder`→9 binders),
so this phase is **targeted cleanup, not a god-class teardown.** **Work (from §10.4 + `docs/ARCHITECTURE_ASSESSMENT.md`):**
excise the dead overlay grammars (`CobolParserOO/JsonXml/Generics/Dialect.g4`) + dead producers (`RecordLayoutBuilder`
vestiges, Option-B per DEVLOG 400); finish the single-PIC-semantics pipeline; island the byte engine
(`RECORD_STRUCT_STORAGE_DESIGN.md` §1.6); tighten layering (binder→bound nodes only, lowering→IR, emission→CIL);
**consolidate the duplicate design essays to one canonical per subsystem** (§10.4) + retire the pre-ANTLR review;
exhaustive XML-doc + comments. Each refactor guard-green (differential oracles + corpus prove behavior-preservation).
**Exit:** zero dead code; PROMPT.md anti-pattern catalog clean; doc corpus de-duplicated; architecture review passes.

### Phase E — BUILD THE PRODUCT SURFACE (the largest NET-NEW program — "commercial-quality, decades")
**Goal:** turn a production-grade *compiler* into a production-grade *product*. **Reality (per §10.2):** almost all of
this exists today only as design essays (~0 lines) — it is the **bulk of the North-Star gap** and is highly parallel.
**Work, by track (each its own SSOT essay in §9.8 + the testing/production assessment):**
- **IDE & debug:** Debugger (PDB + sequence points + DAP adapter) → LSP/IDE integration (the IDE experience).
- **Delivery:** enable+flesh `.github/workflows/build-and-test.yml.disabled` (CI, coverage gate, reproducible builds,
  SBOM/signing); packaging (NuGet, single-file `cobol.exe`, installers, cross-platform); AOT/WASM publish.
- **Hardening:** security enforcement (path-traversal, interop allow-list, WASM sandbox); diagnostics polish (spans,
  recovery, `--warnings-as-errors`); PERFORMANCE benchmark harness + regression gate; test maturity (coverage targets,
  fuzz/property/mutation, conformance expansion to 50+).
- **Enterprise/ops/docs:** multi-tenant isolation + observability; compliance/audit tooling; operational CLI tools;
  user manual + ISO-2023 language reference + internal architecture guide; licensing (BSL) finalized.
**Exit:** clean install, documented, benchmarked, CI-gated, IDE-debuggable, secured, diagnostics at a commercial bar.

### Phase F — FINALIZE & RENAME
**Goal:** the product is `COBOL.NET`. Rename `CobolSharp` → **COBOL.NET** (produced exe **`cobol.exe`**) per
`project_post_conformance_goals`; final full-conformance + quality gate; tag a release.
**Exit:** `COBOL.NET` builds + ships `cobol.exe`; full ISO-2023 green; all phases' exit criteria met.

## 4. Parallelism map (what fans out vs the sequential spine)

| Work | Mode | Why |
|---|---|---|
| Investigation / design panels / adversarial review | **Parallel (always)** | Read-only; no isolation needed. |
| Independent conformance features (Phase C) | **Parallel (Phase-A harness)** | Disjoint files; integrate guard-gated. |
| Independent production tasks (docs, packaging, perf bench) | **Parallel** | Mostly disjoint from compiler core. |
| Grammar, `CilEmitter`, `Binder`, `SemanticBuilder`, pipeline (Phases B, D) | **Sequential on `main`** | Shared core; high conflict + regression risk. Use the fast guard + parallel review. |
| The migration stages (B1→B2→B3) | **Sequential** | Hard dependency chain. |

## 5. Definition of done (the commercial bar)

Full ISO/IEC 1989:2023 conformance (NIST CCVS85 green + a comprehensive post-85 conformance corpus, all guard-green);
typed-native data model the default with the byte engine an island; Roslyn C# backend diff-clean vs the Cecil oracle;
zero dead code / no god class / PROMPT.md anti-patterns clean; commercial-grade diagnostics, performance, packaging,
CI, and documentation; renamed `COBOL.NET` shipping `cobol.exe`. A team can maintain and extend it for decades.

## 6. Risks & guardrails

- **Grammar regressions** — version-factor + `{isYYYY()}?`-gate + incremental + guard-after-each (DEVLOG 439).
- **Typed-flip silent corruption** — every flip hard-gated behind a flag-on≡flag-off differential test + the
  numeric/string oracles; adversarial review per flip.
- **Parallel-integration drift** — every worktree feature integrated one-at-a-time, guard-green, with `guard-verify`
  re-proving the parallel guard.
- **Scope vs one-session** — the program is large; the plan is dependency-ordered + parallelism-maximized to drive as
  far as possible autonomously and is fully resumable at any guard-green commit. Tick this plan + the sub-SSOTs as
  work lands.

## 7. How to start the next session

Read, in order: this file → `docs/DATA_MODEL_ARCHITECTURE.md` (ADR) → `docs/RECORD_STRUCT_STORAGE_DESIGN.md` (migration
roadmap) → `docs/OO_IMPLEMENTATION_DESIGN.md` (§6.6 turnkey) → `docs/ISO2023_CONFORMANCE_PLAN.md` (conformance SSOT) →
`PROMPT.md` (doctrine) → recent `DEVLOG.md`. Then execute Phase A, autonomously, maximally parallel, guard-green at
every commit.

## 8. Status / progress log (tick as phases land)

- 2026-06-07: Master plan authored + grounded. Baseline: guard 1196/509/364; pointers complete; OO grammar
  factored+green; parallel guard live. §9 indexes all ~170 prior docs; §10/§11 folded in from two parallel workflows
  (an 11-agent synthesis of the ~159 docs vs `src/`, and a 4-dimension commercial-bar assessment). Key grounded
  findings: core compiler/runtime ~85–90% (god-class decompositions M001/M003/M004 already DONE); the long-titled
  essay corpus is complete-but-design-only and is the bulk of the commercial gap (Phase E); conformance executes
  `ISO2023_CONFORMANCE_PLAN.md` (do not re-audit).
- 2026-06-07 (DEVLOG 445–451, guard 1196/509 → **1204/527**/364): **Phase A DONE** — CLI `--standard`→DialectLevel
  verified + regression net (445); **CI enabled**, full guard gates push/PR (446); parallel-dev = patch-integrate.
  **Phase B1 OO slices 1–3b DONE** (447–451), each landed on `main` with an **Agent adversarial review** that caught
  + fixed real silent-miscompile/wrong-output bugs the conformance tests missed: slice 1 CLASS/NEW/INVOKE/OBJECT
  REFERENCE + per-instance `ProgramState` (447); slice 2 INVOKE USING/RETURNING — per-instance state proven (448),
  unsupported arg forms made loud COBOL0111 (449); slice 3a INHERITS + virtual/polymorphism (450); slice 3b INVOKE
  SUPER (451). 5 OO conformance tests + `OoTests`.
- 2026-06-08 (DEVLOG 453–456, guard **1204/527 → 1204/535**/364): **Phase B1 — OO goes TYPED-NATIVE (ADR §7) + the
  multi-method keystone.** Owner course-correction: OO object data must be per-instance typed .NET fields, NOT the
  byte `State` (the `EnableTypedFields` gate is migration-safety; OO is an always-on consumer — memory
  `feedback_oo_typed_native_not_byte`). Landed, each guard-green + conformance-tested: **453** object char →
  per-instance `string`; **454** object numeric → per-instance `long`/`decimal` + typed-numeric→byte MOVE cell;
  **455** multi-method classes (N `METHOD-ID`/class, exit-bounded per-method dispatch, per-method paragraph scope
  fixes CBL3104) + `INVOKE SELF`→callvirt incl. polymorphic (COBOL0112 lifted) — 3-agent adversarial panel → edge
  gaps fail loud (COBOL0116 SECTION-in-method, COBOL0117 multi-method-with-params); **456** object group→`record
  struct` + `OCCURS`→typed array (byte `State` now empty on a fully-typed class). +4 `oo_*` conformance tests.
  **NEXT = (a) subclass own typed object data (lift COBOL0113); (b) per-method LINKAGE (lifts COBOL0117) + typed-field
  INVOKE args; (c) FACTORY; then PROPERTY/universal-ref+EC; then the rest of the M2/M3/M4 catalog
  (ISO2023_CONFORMANCE_PLAN §3).**

## 9. Sub-document index (post-consolidation, 2026-06-07)

**Doc-corpus consolidation DONE (2026-06-07, DEVLOG 442):** the sprawling prior corpus (179 files) was consolidated to
**126** — the ~100 long-titled `CobolSharp COBOL … Architecture.md` design essays were merged into **one canonical
design-reference per subsystem** (each absorbs all unique content from its duplicates, carries a status banner with
real implementation status + stack `.NET 10`/`C# 14`, and is stale-fact-corrected); **53 redundant/abandoned docs were
removed** (49 merged-in duplicates + 4 abandoned/broken: 2 zero-byte stubs, the rejected "Custom VM Instruction Set
Spec", and a misnamed Debugger=Runtime byte-dup). Every doc is now a valid reference. (The §10.4 consolidation
recommendation is now executed.)

**9.1 LIVE top-level SSOTs / doctrine (binding — work from these):**
- `docs/MASTER_PLAN.md` (this) · `PROMPT.md` (doctrine + anti-pattern catalog) · `DEVLOG.md` (narrative, entry 442) ·
  `PROJECT_PLAN.md` · `README.md` · `CONSTRAINTS.md`.
- `docs/DATA_MODEL_ARCHITECTURE.md` (typed-native ADR — settled, do not re-litigate) + `docs/DATA_MODEL_REVIEW.md`
  (adversarial review).
- `docs/RECORD_STRUCT_STORAGE_DESIGN.md` (7-stage migration roadmap; pointers §10 done; Stage 5/6 + island §1.6 ahead).
- `docs/OO_IMPLEMENTATION_DESIGN.md` (LIVE OO turnkey design + §6.6 slice-1 map; also the consolidated OO canonical).
- `docs/ISO2023_CONFORMANCE_PLAN.md` (ranked M2/M3/M4 conformance work-breakdown + execution waves — Phase C SSOT).
- `docs/MULTIVERSION_ROADMAP.md` (high-level M0→M4 milestone view).
- `docs/ARCHITECTURE_ASSESSMENT.md` (architecture audit + commercial-hardening roadmap — Phase D/E seed).

**9.2 Architecture-cleanup turnkey (Phase D — LIVE decomposition guides; the M001/M003/M004 splits are DONE):**
- `docs/cilemitter/CilEmitter-Decomposition.md` · `docs/binder/Binder-Decomposition.md` ·
  `docs/boundtree/BoundTreeBuilder-Decomposition.md` · `docs/ir/IR-Expression-Contract.md` ·
  `docs/batch3-architectural-summary.md` · `docs/PARSER-ARCHITECTURE-REVIEW.md` (historical, pre-ANTLR).

**9.3 LIVE frontend / grounded design (binding):**
- `docs/GRAMMAR-AUDIT.md` + `GRAMMAR_AUDIT.md` · `docs/GRAMMAR-REFERENCE.md` · `docs/ANTLR4-RATIONALE.md` ·
  `ANTLR4_VSCODE_IMPORT_BUG.md` (+RESPONSE) · `docs/BINDER-DESIGN.md` · `docs/CATEGORY-RULES.md` · `docs/SCOPE-RULES.md`
  · `docs/DATA-DIVISION-LAYOUT-DESIGN.md`.

**9.4 Consolidated subsystem design-references (one canonical each — design targets w/ status banners):**
- *Frontend:* `ANTLR4-GRAMMAR-ARCHITECTURE.md` (grammar/parsing, LIVE+essay) · `SEMANTIC-ANALYSIS-ARCHITECTURE.md`
  (semantic, LIVE+essay) · `…Semantic Rules & Edge-Case Behavior Specification` (the distinct behavior "constitution").
- *Backend:* `…CIL Backend & Code Generation Architecture` · `…Optimizer & Intermediate Representation (IR)
  Architecture` · LIVE `docs/IL-BYTECODE-GENERATION-DESIGN.md`.
- *Runtime / memory / numeric:* `…Runtime — ExecutionContext, StorageBlocks, ObjectTable, FileManager & Engine
  Integration` (absorbed 5) · `…Memory Model — StorageBlocks, Offsets, REDEFINES, OCCURS & DEPENDING-ON` (absorbed 4) ·
  `…Numeric Engine & Packed Decimal` (arithmetic + numeric).
- *Language features:* `…EVALUATE, Branching & Control-Flow Semantics` · `…PERFORM, Control-Flow, Looping & Structured
  Execution` · `…INSPECT, STRING, UNSTRING & Text-Processing Engine` · `…File IO, FD-SD, Sequential-Indexed-Relative &
  Record-Buffer` · `…Error Model — Declaratives, ExceptionState, USE AFTER & Structured Recovery` · `…CALL Convention,
  Parameter Passing, LINKAGE & BY VALUE/REFERENCE` (call/program-model) · `Cobolsharp COBOL JSON & XML Processing` ·
  `…Standard Library — Intrinsics, FUNCTION Resolution, Runtime Helpers` · `…Interop Architecture — .NET Types,
  Assemblies, INVOKE .NET & Type Mapping` · `OO_IMPLEMENTATION_DESIGN.md` · `REPORT_WRITER_ROADMAP.md`.
- *Product / system:* `CobolSharp Master Architecture Document` (system overview → defers to this plan) · `…Packaging &
  Distribution Architecture` · `…Debugger Architecture — Breakpoints, StorageBlock Inspection, Step Semantics`.
- *LIVE per-feature plans (kept separate):* `docs/REPORT_WRITER_CONTROL_DESIGN.md` · `docs/collating-subsystem-plan.md`
  · `collating-gap2-turnkey.md` · `collating-baseline-finding.md` · `docs/dialect-strictness.md` ·
  `docs/M407-currency-sign-picmode-refactor.md` · `docs/CobolSharp COBOL-to-C# Interop Cookbook.md`.

**9.5 Remaining product-surface design-references (Phase E — design-only, each banner-marked):**
- LSP/IDE Integration · Security Architecture · Compliance & Governance · Operational Runbook · Determinism Model ·
  Concurrency Model · Performance Architecture · Enterprise Deployment · Build & Release Pipeline / Build System ·
  Test Harness & Validation + Testing & Verification · Contributor & Maintainer Guide · Internal Compiler API Reference
  · Modernization & Migration Toolkit · Future Extensions Roadmap (SQL/CICS/distributed) · Complete Developer Guide ·
  Cookbook (100+ patterns) · End-User Handbook · End-to-End DX Flow · Formal Specification · ISO Compatibility Matrix ·
  Language Feature Support Matrix · `docs/USER-GUIDE.md`. (Plus the per-statement feature essays not in a dup cluster:
  MOVE/CORRESPONDING · Condition-Names · SORT-MERGE · ACCEPT/DISPLAY/Console-IO · Expression-Evaluation · National/
  Unicode · COPY-REPLACE + REPLACE-preprocessor · Compiler-Directive/Conditional-Compilation · Paragraph-Section ·
  Program-Lifecycle · Program-Registry · File-Status · Declaratives/USE.)

**9.6 Terminal / SCREEN SECTION subsystem (Phase C — a self-contained LIVE design set, 12 docs):**
- `docs/M411-screen-section-grammar-island.md` + `docs/TERMINAL-*.md` ×11 (ABSTRACTION-DESIGN, ACCEPT-INPUT-LOOP,
  BUFFER-ATTRIBUTE-MODEL, CRT-STATUS-MAPPING, CURSOR-ENCODING, DEVICE-BACKEND, MULTI-FIELD-NAVIGATION,
  RUNTIME-CLASS-LAYOUT, RUNTIME-ENTRY-POINTS, SESSION-API, TEST-HARNESS).

**9.7 Spec text & process/status ledgers (reference):**
- Spec: `specs/ISO_COBOL.md` (submodule, authoritative) + `docs/spec-text/{cobol-spec,full-spec,section-14-procedure}.md`.
- Conformance/gap ledgers (LIVE): `docs/CONFORMANCE.md` · `docs/SPEC_FIX_BACKLOG.md` · `docs/SPEC_FIX_RECIPES_M1.md` ·
  `docs/SPEC_GAP_INVENTORY.md` · `docs/spec-gaps.md` · `docs/EXCLUSION_LEDGER.md` · `docs/FLAG_MANIFESTS.md` ·
  `docs/FUTURES.md` · `docs/COBOL85_COMPLIANCE_PLAN.md` · `AUDIT_REPORT.md` · `MIGRATION_LEDGER.md` ·
  `NIST_TEST_REPORT.md` · `docs/BATCH5-*.md` (grammar-leniency approval ledgers).
- Meta: `docs/Resume-Prompt-For-Docs.md` (the prior doc-gen session's resume note — explains the now-consolidated essays).

## 10. Grounded assessment (commercial/decades bar)

Folded in from two parallel workflows (2026-06-07): an **11-agent synthesis of the ~159 prior docs** cross-checked
against `src/`, and a **4-dimension commercial-bar assessment** (architecture/maintainability · conformance/completeness
· testing-CI-parallelism · production-readiness). The two reconcile to one honest picture below. (Caveat: a couple of
assessment sub-agents read stale state — e.g. "DEVLOG at 223" — so this section trusts §2's current baseline + the
synthesis for status; the assessment is trusted for its forward analysis.)

**10.1 What is REAL — the compiler core + runtime are production-grade (~85–90% of a COBOL-85 compiler):**
The pipeline (Preprocess → ANTLR Lex/Parse → 10-pass Semantics → BoundTree → IR → Lowering → Cecil CIL) emits running
.NET for COBOL-85 + landed dialect features; **364 NIST CCVS85 baselines green; M1 complete.** Crucially, **the
architecture refactors the "god class" worry implies are ALREADY DONE and tested:** M001 (IR-expression hierarchy, zero
`BoundExpression` leakage into IR), **M003** (`CilEmitter` 4083-line god class → **11 focused emitters** <500 lines +
`EmissionContext`), **M004** (`BoundTreeBuilder` → **9 focused binders**, 234-line orchestrator), plus 9 focused
lowering files. The numeric substrate is unified + oracle-proven (`CobolDecimal`/`NumProfile`/`CobolNum.Store`, 8 ISO
rounding modes, SIZE ERROR via `TryStore` not exceptions). The data-model migration core is **done through Stage-4**
(char→`string`, numeric→`long`/`decimal`, flat+nested groups→`record struct`, OCCURS→`T[]`, pointers→`ManagedPointer`),
each guard-green + byte-identical under the `EnableTypedFields` gate. P1 commercial diagnostics exist (**206 diagnostic
codes** with precise locations; CLI top-level try/catch → exit 70). A **disabled CI template already exists**
(`.github/workflows/build-and-test.yml.disabled`); the CLI csproj is `PackAsTool` (missing `SingleFile`).

**10.2 What is DESIGN-ONLY — the entire commercial *product* surface (~0 lines; the real decades-bar gap):**
The ~100 long-titled `CobolSharp COBOL … Architecture.md` essays are a prior doc-generation pass — **thorough, valuable
*target designs* but NOT implemented.** Zero-code today, in rough priority for "commercial-quality, decades-sustainable":
- **Debugger** (PDB emission, sequence points, DAP adapter) and **LSP/IDE integration** (hover/complete/goto/rename/
  diagnostics, workspace indexing) — the IDE experience is design-only.
- **CI/CD** (enable + flesh out the disabled template; coverage gate, reproducible/deterministic builds, SBOM, signing),
  **packaging/distribution** (NuGet, single-file `cobol.exe`, installers, cross-platform).
- **Security enforcement** (path-traversal guards, `.NET`-interop allow-list, WASM sandbox), **AOT/WASM publish**.
- **Performance** benchmark harness + regression gate; **enterprise** (multi-tenant isolation, observability),
  **compliance/audit** tooling, **operational** CLI tools.
- **JSON/XML PARSE/GENERATE lowering + runtime** (grammar overlay exists; emit/runtime not wired) and the
  **Screen/Terminal subsystem** (a complete 12-doc design, ~12–18h, one owner design Q on form-level ACCEPT) — these
  two are conformance features that live at the back of Phase C.
- User docs (manual, ISO-2023 language reference, architecture guide).
→ **This is the bulk of the North-Star gap and is mostly NET-NEW build, not refactor.** It maps to Phase E (and the
JSON/XML + Screen items to Phase C). Phase E is a first-class, large, highly-parallel program — **not a polish pass.**

**10.3 Conformance reality (Phase C):** `docs/ISO2023_CONFORMANCE_PLAN.md` is the SSOT — **execute it, do not re-audit.**
M1 '85 is complete on the baseline axis; a finer 3-axis '85 model (baseline ✔ / flagging-harness / spec-completeness
tests) per `COBOL85_COMPLIANCE_PLAN.md` still has WS-SPEC test-authoring + a Report-Writer Stage-0 parse unblock. The
M2 backlog, ordered by value/tractability (assessment's conformance agent): **OO slice-1 first (largest, unblocks M2)
→ FILE-2002 → ARITH-2 → PRE-1 → UDF → VALIDATE → EC/exceptions** (VALIDATE+EC are the big subsystems); then M3
(OCCURS DYNAMIC, TYPEDEF, JSON/XML-standard, file-sharing) and M4 (new intrinsics, bit-data). Owner-locked scope:
high-subset only (**no `--subset` flag**); CM/DB/SG parse+flag only (no runtime). Every post-'85 feature ships a
`tests/conformance/<ver>/` test in the same commit.

**10.4 Doc-corpus hygiene — DONE (2026-06-07, DEVLOG 442).** The essay corpus's heavy duplication was consolidated to
ONE canonical per subsystem (INSPECT 3→1, PERFORM 2→1, EVALUATE 2→1, Report-Writer 3→1, Debugger 4→1,
Runtime/ExecutionContext 5→1, Memory-Model 4→1, OO 3→1, Interop 2→1, File-IO 4→1, JSON/XML 5→1, intrinsics 3→1,
arithmetic/numeric 3→1, CIL-backend 3→1, system-overview 6→1, packaging/build 4→1, CALL/program-model 3→1, +grammar/
semantic essays folded into the LIVE SSOTs), each absorbing its duplicates' unique content + a status banner; **corpus
179 → 126 docs** (53 removed). See §9 for the resulting index. Remaining code-side Phase-D debt is **targeted, not a
teardown**: excise dead overlay grammars + `RecordLayoutBuilder` vestiges (Option-B per DEVLOG 400), finish the
single-PIC pipeline, island the byte engine, exhaustive XML-doc.

**10.5 Honest verdict + what it means for §3.** `AUDIT_REPORT.md`'s "**DO NOT REWRITE**" verdict still holds and is
**consistent** with the North Star: the owner's "rewrite/re-architect anything, no back-compat" is a *license* to
re-architect targeted subsystems for decades-quality (byte→typed model; future Roslyn backend; byte-engine islanding) —
**not** a mandate to greenfield-restart a sound, well-decomposed core. The assessment's optimistic read ("~85% ready;
one session can reach commercial delivery") is true **only for the narrow scope** of "an OO-complete CLI compiler with
packaging + user docs." The **full** commercial/decades/full-ISO-2023 bar is a **multi-phase program** whose single
largest remaining chunk is the design-only **product surface of §10.2** (debugger, LSP, CI, security, perf, packaging),
plus the conformance long-tail. So: §3's phases stand; the next session drives **as far as possible** autonomously and
maximally parallel (realistically: Phase A enablers → Phase B1 OO vertical → as much of the Phase-C/E parallel fan-out
as fits), committing guard-green increments, and the plan stays **fully resumable at any commit** — reaching the entire
North Star is the program's goal, not a one-session expectation.

## 11. Early-resolve decisions (surfaced by the syntheses — settle these in the first hour)

These are the small set of genuinely owner-or-design-gated choices that unblock the most downstream parallelism. Pick
defaults and proceed where the owner has not pre-decided; only stop if truly blocked.

1. **Parallel-dev harness (Phase A):** fix the stale-worktree base so `isolation:'worktree'` branches from HEAD, **or**
   adopt a "copy-implement-return-patch, sequentially integrate guard-gated" pattern (memory
   `feedback_worktree_workflows_stale` favors not trusting worktrees here). *Default: build the patch-integrate pattern
   — it is reliable now and needs no git-internals fix.*
2. **CI:** enable + flesh out `.github/workflows/build-and-test.yml.disabled` to gate the guard on every push. *Default:
   enable it in Phase A.*
3. **Screen/Terminal form-level ACCEPT semantics** (`TERMINAL-MULTI-FIELD-NAVIGATION.md`): one field-at-a-time vs whole-
   form loop. *Owner design Q — defer Screen to late Phase C and ask then; everything else proceeds without it.*
4. **Test maturity targets:** coverage tiers (compiler ~85% / runtime ~80%), and the **fuzz/differential oracle**
   (is GnuCOBOL available as an external oracle, or use the Cecil/byte differential + golden baselines?). *Default:
   use the existing internal differential oracles; treat GnuCOBOL as optional.*
5. **Parallel conformance-test authoring:** agents writing many `.cob`/`.out` files concurrently can git-conflict —
   author in disjoint per-feature dirs and integrate one-at-a-time guard-gated (same discipline as feature code).
6. **Roslyn backend (Stage-5/B2):** owner already chose "proceed, deferred beyond Stage-4 OO; Cecil stays the shipping
   backend + differential oracle" (ADR §12 Q#2) — not re-litigated.
