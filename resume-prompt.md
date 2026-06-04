# CobolSharp — Session Resume Prompt (2026-06-04)

Paste this to start a new session. **Mission (REFRAMED 2026-06-04): a multi-version COBOL compiler (ISO 85→2023),
dialect-gated — implement features LIVE across all versions, parse+flag features REMOVED after '85.** NIST CCVS85
remains the validation backbone for the core modules + Report Writer.
This file is the authoritative, current orientation; linked docs hold the detail. Current as of DEVLOG 323
(guard 1040 unit / 347 int / **360 NIST**).

**═══ THIS SESSION (DEVLOG 322–323): scope reframe + Report Writer workstream started ═══**
The owner reframed the end goal to **multi-version (85→2023), live-features-first** (see
`docs/COBOL85_COMPLIANCE_PLAN.md` §4 + `scripts/compliance.sh`): the 8 core NIST modules are COMPLETE; the only
LIVE-in-2023 unimplemented module is **Report Writer** (ISO 2023 §A.4.11) — IMPLEMENT it. The removed modules
(DB/SG/CM/obsolete) become **parse + dialect-flag only** (WS-DIALECT), NOT runtime — they're non-conformant in
2002+, so building their runtime serves only '85. Report Writer build is staged in `docs/REPORT_WRITER_ROADMAP.md`
(design in `docs/REPORT_WRITER_DESIGN.json`). **Done this session (both committed, guard ALL GREEN):**
- **RW Stage 0 (322):** the obsolete IDENTIFICATION comment-entry paragraphs (AUTHOR/INSTALLATION/…) have
  free-form bodies un-parseable by a token grammar (embedded periods, number lines, reserved words in address
  text). Fixed **column-aware in `ReferenceFormatProcessor.ConvertFixedToFree`** — comment out the whole obsolete
  paragraph until the next Area-A header. No grammar change. Also unblocks DB/SG/CM old-vintage headers.
- **RW Stage 1a (323):** the **declarative** REPORT SECTION grammar — FD `reportClause` (REPORT IS) +
  `CobolReportWriter.g4` rewritten (RD + PAGE LIMIT/CONTROL/CODE; report groups with TYPE/LINE/COLUMN/SOURCE/SUM/
  GROUP INDICATE, reusing picture/usage/etc.). Added lexer tokens `GROUP` + `PLUSWORD` (the word PLUS; fixed a
  screen-section IDENTIFIER hack the guard caught). **RW101A's entire data division now parses; fails only at the
  executable verbs (`INITIATE`, line 346).**
- **RESUME AT → RW Increment B** (one atomic unit, parse+bind+emit together): the verbs INITIATE/GENERATE/
  TERMINATE — grammar (Stage 1b) + ReportSymbol/ReportGroupSymbol model + LINE-COUNTER/PAGE-COUNTER (Stage 2) +
  `ReportWriterRuntime` (Stage 3: INITIATE resets counters, GENERATE places SOURCE by COLUMN/advances by LINE,
  page-fit vs PAGE LIMIT, TERMINATE) + bound nodes/IR/**CilEmitter dispatch cases** (Stage 4) + baseline RW101A…RW6
  (Stage 5). RW101A is the MINIMAL case (1 DETAIL group, no control breaks/SUM); it verifies LINE-COUNTER. See
  `project_reportwriter` memory + `docs/REPORT_WRITER_ROADMAP.md` Stages 2–5.

**Latest session (DEVLOG 311–321): NIST 349→360 (+11) — SQ212A + the ENTIRE remaining file-I/O/IC backlog, via
two parallel multi-agent workflows.** A 9-agent **diagnosis** workflow root-caused the backlog, then a 4-agent
**worktree-isolated implementation** workflow implemented + full-guard-verified the four fixes in parallel; I
integrated each onto main sequentially (3-way merge, guard-gated). Integrated DEVLOG 317–320: **RL213A** (OPTIONAL
cross-program consumer shares the producer's `TF021` — NistPreprocessor allow-list), **IC114A** (a CALLed
subprogram registers its own file connectors at `Entry`, not in its dead `Main`), **IC227A** (`FD … IS EXTERNAL`
record area shared across programs — EXTERNAL machinery extended to FileSection with an Area discriminator),
**IC233A/IC234A** (the GLOBAL-FILE feature: inherit ancestor `FD … IS GLOBAL` files into contained programs +
cross-program GLOBAL USE-declarative dispatch via a new run-unit `GlobalUseDeclarativeRegistry`). **The diagnosed
file-I/O/IC backlog is COMPLETE** plus IX108A (321 — WRITE was the only I/O statement not emitting its INVALID/NOT-INVALID blocks; also un-skipped 2 latent RL111A tests). Earlier in the session (DEVLOG 311–316): First SQ212A (311–312): variable-length WRITE/REWRITE out-of-bounds → **I-O status 44**
(ISO §9.1.13) instead of crashing, which exposed+fixed a USE-procedure **declaratives-return** bug (return at
the declarative's designated exit, not its CLOSE-FILES/footer/STOP-RUN termination tail; the right rule is "last
trivial exit-point paragraph only when a terminating paragraph follows it" — two wrong heuristics each passed
SQ212A alone but the guard caught them breaking 17 SQ tests). Then a **9-agent parallel root-cause diagnosis**
(isolated scratch dirs) of the whole remaining RL+IC backlog drove four sequential, guard-gated fixes:
- **(313) RL208A** — no implicit CLOSE at run-unit termination → REWRITEs to a never-explicitly-CLOSEd file were
  discarded. `CilEmitter` Self.Entry branch (Main only) now calls `FileRuntime.CloseAll()` after `Entry()` (ISO
  §14.6.11). (Diagnosis also corrected the chain: RL208A's producer is RL206A, not RL212A.)
- **(314) RL205A** — bare relative `START` ignored the implicit RELATIVE KEY (LowerStart fallback, §14.9.41.4 GR8);
  AND a **systemic** bug — the standalone `NOT INVALID KEY` phrase was mis-bound as the INVALID-KEY block in all
  five `FileIoBinder` key-phrase binders (one extracted `BindInvalidKeyBlocks` helper; discriminator
  `blocks==1 && NOT()!=null`).
- **(315) RL111A** — USE handler doing `CLOSE` on its own file → status 42 → re-fired itself → ∞ stack overflow.
  Added a USE-declarative **re-entrancy guard** (`FileRuntime._activeUseDeclaratives` + Enter/ExitUseDeclarative
  bracketing the declarative PERFORM, §14.9.49.4 GR2).
- **(316) IC235A** — the three per-program tree-walk passes had no `VisitNestedProgram` override, so a CONTAINING
  program absorbed its contained programs' declarations (CBL3107/3101/3108). Each visitor now stops the walk at
  contained-program boundaries (ISO §8.4.6) — descend a nested program only when it is the walk root.

**Latest session (DEVLOG 296–302): 299 → 350 → 347 honest NIST baselines.** Phase-1 quick wins, IC
self-contained CALL tests (+12, full IC suite mapped), an index-name-in-LINKAGE compiler fix (IC106A/IC207A),
SQ tail (+24) and RL tail (+7). Then the owner reframed the goal to **production/commercial COBOL-85 +
extensibility, rewrite-on-the-table**; two evidence-based workflows (10-dim architecture audit + 11-test
root-cause diagnosis) answered it: **NO REWRITE — 8× targeted-refactor / 2× incremental / 0× rewrite.** Full
synthesis + prioritized commercial-hardening roadmap in **`docs/ARCHITECTURE_ASSESSMENT.md`** (read it first).
**P0 done (DEVLOG 302):** the guard was lying — it never parsed the CCVS footer total, so 3 false-greens hid in
"350" (SQ212A 0-byte *crash*, IX108A footer 001 FAILED, NC303M 0-byte flag module). Hardened the guard (footer
+ 0-byte checks) and removed them → **honest 347**. **Method lessons baked in:** `run-suite.sh` reports chain
consumers "CLEAN" off STALE `TF###`, and a 0-byte/stale report hides a crash — verify every candidate from a
clean dir with **rc=0 + freshly-written report + footer "NO TEST(S) FAILED" + EXECUTED>0**, never just a
0-`FAIL*` grep. **Then started the diagnosed feature fixes (DEVLOG 303): RL119A (OPEN I-O missing-non-optional
→35) + RL106A (varying relative records size by MAX not first-01) → 349.**

**This session (DEVLOG 304–310): P1 commercial-hardening "diagnostics on invalid input" — ALL 6 ITEMS DONE,
then CBL3128 flipped default-on (310).** Guard ALL GREEN throughout: **1040 unit / 347 integration / 349 NIST** (NIST count unchanged — these are
diagnostics on *invalid* input, gated so valid programs are unaffected). Owner directives: **sequential +
guard-gated** (one item, full guard, commit, repeat) and **new strictness dialect-gated to named-strict modes
first** so the permissive Default/--nist path (= the 349 baselines) is unaffected *by construction*. (The CLI
defaults to `--standard cobol85` = strict, so ordinary `cobolsharp foo.cob` users DO get the new checks.)
- **#7 (304)** real source path in *every* diagnostic + retired the bare `"SEM"` code → descriptors CBL3120–3127.
- **#5 (305)** undefined data-name **CBL3128** — one centralized `ReferenceResolver` pass (not 66 sites),
  strict-gated. Default-flip dry-run: **348/349 clean; only IC228A** false-positives (GLOBAL data inherited
  from a *containing* program, since `InheritGlobalItems` runs after `ReferenceResolver`) = the lone flip blocker.
- **#8 (306)** PICTURE-validity **CBL0814** (strict-gated; dry-run **0/349 clean**) + level-number **CBL0815**
  (unconditional — replaces a crash-prone `int.Parse`).
- **#9 (307)** CopyProcessor diagnostics — missing **CBL3620** (gated) / circular **CBL3621** / depth **CBL3622**.
- **#6 (308)** CLI top-level try/catch → internal-compiler-error, **exit 70** (EX_SOFTWARE).
- **#10 (309)** runtime arg guards + **CobolRuntimeException** (RT####) on FileRuntime/PicRuntime/SortRuntime.

**Next (priority order):** (a) **Default-flip follow-ups** — **CBL3128 is now flipped to default-on (DEVLOG
310)**: the IC228A ordering + 6 more false-positive sources an adversarial sweep found (the 0/349 dry-run was
INSUFFICIENT — always run the adversarial false-positive sweep when flipping a gated check). **CBL0814 (PIC)
is next** (dry-run already 0/349 clean). (b) **Deferred P1 sub-items** — the PIC
structural rules (V/., P-run, S-first, Z/*); CopyProcessor REPLACE-malformed (CBL3623–5) + copybook
source-mapping; StorageArea ref-mod-aware guards; the **emitted-Main top-level catch (Layer 2)**, which pairs
with the P2 `Dispatch` recursion guard. (c) **P2 codegen hardening** (IL verify in guard, `IrRuntimeCall`
fail-fast, `Dispatch` recursion = ~~RL111A done DEVLOG 315~~). (d) **Diagnosed feature fixes** (~~SQ212A 311–312,
RL208A 313, RL205A 314, RL111A 315, IC235A 316 — all DONE~~; **remaining 4 below, all root-caused by the
9-agent diagnosis**). **The dead `FlowAnalysis/PerformRangeChecker` + `ParagraphReachabilityAnalyzer`
(bare "FLOW" code) are a known verify-then-delete cleanup (PROMPT.md zero-dead-code).**

### File-I/O/IC backlog — ✅ COMPLETE (DEVLOG 317–321)
The whole DEVLOG-311 diagnosed file-I/O/IC backlog is integrated and baselined (NIST 354→359): **RL213A**
(OPTIONAL cross-program consumer share, 317), **IC114A** (subprogram file-reg at Entry, 318), **IC227A**
(FD-EXTERNAL record sharing, 319), **IC233A/IC234A** (GLOBAL-FILE inheritance + cross-program GLOBAL USE
dispatch, 320). Implemented in parallel worktree-isolated agents, integrated onto main one-at-a-time, guard-gated.
- **IX108A** ✅ DONE (321) — `LowerWrite` never emitted the invalid-key branch (every other I/O statement did);
  WRITE's INVALID/NOT-INVALID blocks were dropped compiler-wide. Adding the branch → 032 OF 032, and un-skipped 2
  latent RL111A tests (022→024, baseline regenerated). The diagnosed backlog is now fully cleared. Other long-tail known-fails: SQ212A's IX108A-class, the deferred RL/IC GLOBAL follow-ups (subscripted/ref-modded
  inherited globals, level-88 under a global group). Next-session candidates: Phase 5 stretch (DB/RW modules) or
  the dead-code cleanup (`FlowAnalysis/PerformRangeChecker` + `ParagraphReachabilityAnalyzer`).

## Read first
- **CLAUDE.md** (root) → **PROMPT.md** (non-negotiable doctrine), **PROJECT_PLAN.md** (status + session log),
  **DEVLOG.md** (decision narrative — now at entry 303), **docs/ARCHITECTURE_ASSESSMENT.md** (the
  no-rewrite verdict + commercial-hardening roadmap — READ THIS for the strategic direction).
- **specs/ISO_COBOL.md** is the authoritative spec (submodule — `git submodule update --init --recursive` if
  absent). **Implement from the spec, not from assumptions.** The markdown preserves required-keyword
  underlining as `<u>…</u>` in figure-style formats but NOT inside ``` code blocks — check the figure form
  when a keyword's optionality matters.
- **docs/dialect-strictness.md** — the two-axis dialect model (version `--standard` vs strictness) and the
  registry of CCVS non-conformant constructs (leniencies L1–L5; L4 deferred). **Discipline rule: every
  grammar leniency is dialect-gated through `DialectStrictnessChecks` from the moment it is added — never an
  unconditional grammar relaxation.** `--nist` implies `--standard default` (permissive); named-strict modes
  (e.g. `--standard cobol2023`) reject the leniencies.
- Memory index loads automatically. Key entries: `project_nist_progress` (suite-by-suite), `project_fileio_remaining`
  (file-I/O history + RL208A), `project_dialect_strictness` (leniency registry), `reference_nist_xcards`
  (X-card model), `project_collating_gap`.

## What "operational" means (the finish line)
Every one of the **459 NIST programs** in `tests/nist/programs/` is in exactly one accounted-for class:
1. **Baselined** — in `NIST_TESTS` (scripts/guard.sh) with a `tests/nist/valid/<T>.txt` at **0 FAIL\***,
   and its CCVS report is **non-vacuous** (tests EXECUTED > 0, footer "TEST(S) FAILED" = 0).
2. **NO_OUTPUT producer/builder** — runs in the guard ahead of a baselined consumer to build/sort a shared
   file (e.g. ST115A/ST116A; the 10 ST producers). Not baselined, but feeds a chain.
3. **Documented exclusion** — a flagging "M" module (compile-time FLAG test, no CCVS report: IF401M, IX301M/401M,
   SQ303M/401M, RL301M/401M, ST301M, SM301M/401M, …), or a non-deterministic test (NC214M, ACCEPT FROM
   DATE/TIME), or an out-of-scope obsolete/optional module (see Phase 5).
A new test enters the guard ONLY at 0 FAIL\* AND non-vacuous. Baselines must stay 0 FAIL\* forever.

## Current coverage (branch `main`, all committed, guard GREEN: 1040 unit / 347 integration / 360 NIST honest)
| Suite | Present | Baselined | Status |
|-------|--:|--:|--|
| **NC** nucleus            | 95 | 93 | ✅ COMPLETE (NC214M non-deterministic; NC303M 0-byte flag module removed DEVLOG 302) |
| **IF** intrinsics         | 45 | 42 | ✅ COMPLETE (IF401M/402M/403M flagging) |
| **IX** indexed I/O        | 42 | 40 | ✅ +IX108A (DEVLOG 321 — WRITE invalid-key branch); IX301M/401M flagging |
| **ST** sort/merge         | 40 | 29 +10 prod | ✅ COMPLETE (ST301M flagging) — every program accounted for |
| **SM** COPY/REPLACE       | 17 | 15 | ✅ COMPLETE (SM104A=SM103A chain; SM301M/401M flagging) |
| **SQ** sequential I/O     | 85 | 83 | ✅ +SQ212A (var-length WRITE/REWRITE→status 44 + USE-return fix, DEVLOG 311–312); SQ303M/401M flagging |
| **OBSQ** obsolete-seq     |  4 |  3 | ✅ COMPLETE (OBSQ1A/4A/5A; OBSQ3A = producer) |
| **RL** relative I/O       | 35 | 32 | ✅ +RL208A/205A/111A/213A (DEVLOG 313–317). RL212A producer; RL301M/401M flagging |
| **IC** inter-program CALL | 47 | 23 | ✅ +IC235A/114A/227A/233A/234A (DEVLOG 316–320 — nested scoping, subprogram file-reg, FD EXTERNAL, GLOBAL FILE). All in-scope callers baselined; ~23 callee halves + IC116M/401M excluded |
| **DB** debug              | 15 |  0 | ✗ whole Debug module unimplemented (Phase 5) |
| **SG** segmentation       | 13 |  0 | ✗ whole Segmentation module (OBSOLETE in COBOL-2002) (Phase 5) |
| **CM** communication      |  9 |  0 | ✗ whole Communication module (OBSOLETE in COBOL-2002) (Phase 5) |
| **RW** report writer      |  6 |  0 | ◐ IMPLEMENTING (LIVE module). Stage 0+1a done (DEVLOG 322–323): data division parses; resume at Increment B (verbs+runtime+codegen). See `project_reportwriter` |
| **OBNC/OBIC** obsolete    |  5 |  0 | ✗ obsolete-feature NC/IC variants (Phase 5) |
| **EXEC** EXEC85           |  1 |  0 | ✗ EXEC85 driver (COMPILE_FAIL) (Phase 5) |

Survey any suite: `bash scripts/run-suite.sh <PREFIX>` → per-test `CLEAN | N FAIL* | COMPILE_FAIL | NO_OUTPUT | RUNTIME(rc)` + a footer `=== PREFIX: total=… ===`. It does NOT create baselines.

## Roadmap to full coverage (priority order)

### Phase 1 — Quick baseline wins. ✅ DONE (DEVLOG 296–298).
SM104A/105A/205A + OBSQ1A/4A/5A baselined; the 18 self-contained IC callers baselined (incl. IC106A/IC207A
after the index-name-in-LINKAGE compiler fix, DEVLOG 298). **The vacuous-trap caution proved real and is now
the standing method (see header):** verify from a clean dir, rc=0 + fresh report, callee-effect-in-COMPUTED.

### Phase 2 — SQ/RL un-surveyed tail. ◐ SQ ✅ DONE, RL mostly done (DEVLOG 299–300).
**SQ COMPLETE** (+24, all self-contained — the maturing FILE-CONTROL/status subsystem had silently made them
pass). **RL +7** (RL104A/112A/113A/114A/115A/116A/204A). The proven method for the rest: **FAIL\*** → read
COMPUTED vs CORRECT, trace to `FileRuntime`/`*FileHandler`, cite ISO §9.1.13, fix the pattern across all 3
handlers, baseline. Remaining RL bugs to chase: **RL106A** (2 FAIL\*), **RL119A** (1 FAIL\*), **RL205A**
(9 FAIL\*), **RL213A** (20 FAIL\*), **RL111A** (real FAIL\* "WRITE TO FILE OPENED INPUT" + a `D-CLOSE-FILES`
infinite-recursion **stack overflow** in the dispatch — a control-flow bug worth fixing on its own).

### Phase 3 — RL208A (the ONE long-known open file-I/O compiler bug).
5 FAIL\*, gap in the RL207A→RL208A delete/update chain (it consumes XXXXD021 from producer RL212A). Latent:
`RelativeFileHandler.Rewrite` pads a varying record to max length instead of the ACTUAL length — needs a
variable-REWRITE path mirroring `WriteVariable`. RL207A is baselined, so change carefully + full-guard.

### Phase 4 — IC genuine remaining (the real inter-program work). The IC suite is fully MAPPED (DEVLOG 297):
24 standalone callers (18 baselined) + ~23 callee-only halves (excluded) + IC116M/401M flagging. Remaining
callers all need real features:
- **IC233A/234A** — `USE GLOBAL AFTER ERROR PROCEDURE` declaratives on a file error. Blocker today is a
  COMPILE_FAIL: "OPEN target 'TEST-FILE' is not a declared file" — the contained program IC233A-1 OPENs/READs
  a **`FD … GLOBAL` file declared in the containing program**, which we don't inherit. Needs GLOBAL FILE
  inheritance into contained programs (extend the GLOBAL-data mechanism from DEVLOG 236 to FILE SECTION) +
  the USE GLOBAL declarative firing on the contained program's I/O error. (IC233A also omits the spec-required
  `STANDARD` — accept only as a documented leniency, ISO §14.9.49.2.)
- **IC227A/235A** — EXTERNAL clause. IC227A 16/23 (3 FAIL\*) needs EXTERNAL **file** semantics; IC235A
  COMPILE_FAIL "Name 'PRINT-FILE' conflicts" (multi-program EXTERNAL naming).
- **IC114A** — file-I/O chain consumer (1 FAIL\* + binary report output).
- Deferred GLOBAL follow-ups: subscripted/ref-modded inherited globals, level-88 under a global group.

### Phase 5 — Whole unimplemented modules. ✅ SCOPE REVISED (DEVLOG 322, 2026-06-04): multi-version, live-first.
Supersedes the 2026-06-03 "defer DB/RW" decision. The end goal is a compiler for **every ISO COBOL 85→2023**,
so the question is "is the feature LIVE in 2023?" (full `docs/COBOL85_COMPLIANCE_PLAN.md` §4):
- **IMPLEMENT — Report Writer (RW, 6)**: LIVE in ISO 2023 (§A.4.11, optional element), not a dead-end. The
  flagship live-module work. ◐ IN PROGRESS (Stage 0+1a done; resume at Increment B — see top of file).
- **PARSE + DIALECT-FLAG ONLY (WS-DIALECT), no runtime** — features REMOVED after '85: **DB** (Debug,
  `USE FOR DEBUGGING`/`DEBUG-ITEM`, removed 2002), **SG** (Segmentation, gone), **CM** (Communication, removed
  2002), **OBNC/OBIC** obsolete elements. Grammar ACCEPTS them under `--standard cobol85`; `DialectStrictnessChecks`
  flags-as-removed under `cobol2002+` (satisfies the `…M` flagging tests). Building their RUNTIME is out of scope
  (serves only '85, non-conformant later). Their full `…A` tests do NOT baseline (documented: parse+flag only).
- **EXCLUDED**: **EXEC85** (1 — non-standard test driver).
**⇒ Baseline target (live) = core 8 modules + Report Writer.** `scripts/compliance.sh` reports it (currently
~86% = 360/416; → ~100% when RW baselines). Long horizon: **WS-FORWARD** — the dialect/version architecture +
COBOL-2002/2014/2023 features with a custom conformance corpus (no NIST suite exists past '85).

### Remaining genuine in-scope work (the actual next-session targets)
- **RL runtime bugs** (Phase 2/3): RL106A (2 FAIL\*), RL119A (1 FAIL\*), RL205A (9 FAIL\*), RL213A (20 FAIL\*),
  RL111A (real FAIL\* "WRITE TO FILE OPENED INPUT" + a `D-CLOSE-FILES` infinite-recursion **stack overflow**),
  RL208A (the known `RelativeFileHandler.Rewrite` pads-varying-to-max bug). Use the proven per-test method.
- **IC inter-program features** (Phase 4): IC233A/234A (`USE GLOBAL AFTER ERROR` + **GLOBAL FILE inheritance**
  into a contained program — extend the DEVLOG-236 GLOBAL-data mechanism to FILE SECTION; the blocker is the
  COMPILE_FAIL "OPEN target 'TEST-FILE' is not a declared file"), IC227A/235A (EXTERNAL file/data), IC114A
  (file-chain + binary report).

## Process rules (non-negotiable — from PROMPT.md + memory feedback_*)
- **Run `bash scripts/guard.sh` after meaningful changes; it must stay ALL GREEN, baselines 0 FAIL\*.**
- **NEVER edit a NIST `.cob` source to dodge a compiler bug — fix the compiler.** (Verified clean: no
  `tests/nist/programs/*.cob` has ever been modified.) X-card placeholder substitution happens in
  `NistPreprocessor` at compile time (source untouched) — that is required, not a workaround.
- **Grammar/semantic changes are pre-authorized for the NIST effort** — log in DEVLOG, rely on the full guard,
  commit. But every leniency must be **dialect-gated** (strict modes still error).
- **Every commit needs ≥1 DEVLOG entry**; write it as you go, not batched. Commit messages end with the
  `Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>` trailer.
- **Verify output is CORRECT, not just that it ran** — the vacuous-pass trap (a test self-certifying PASS with
  degenerate data, e.g. ST147A's blank-vs-blank collating checks before XXXXX063 was substituted).
- Keep PROJECT_PLAN.md, this file, and memory synced. Don't touch global NuGet config.
- **Guard soundness note:** the guard asserts 0 `FAIL\*` *detail* lines but does NOT check the footer
  `NNN TEST(S) FAILED` total — a suppressed-detail failure can slip through (NC208A did once). A footer-total
  sweep of existing baselines is a worthwhile hardening task.

## Architecture quick-reference (the pipeline + the files you'll touch most)
Grammar (`src/CobolSharp.Compiler/Grammar/Core/*.g4`) → `SemanticBuilder` (builds symbols; FD/RECORD clauses) →
`StorageLayoutComputer` (per-FD byte layout) → `Binder`/`FileIoBinder` (`BindingContext`, `BoundTreeValidator`,
`DialectStrictnessChecks`) → IR lowering (`FileIoLowerer`, `LocationResolver`, `ControlFlowLowerer`) → CIL
emission (`CilEmitter`, `CilFileIoEmitter`, `CilLocationEmitter`) → `CobolSharp.Runtime` (`FileRuntime`,
`IO/{Sequential,Indexed,Relative}FileHandler`, `SortRuntime`, `StorageHelpers`).
- **Single source of truth for variable-length records:** `SemanticModel.IsVariableLengthSequential`
  (= `IsRecordVarying || HasMultipleRecordSizes`); `FileSymbol.IsRecordVarying` is set in
  `SemanticBuilder.VisitRecordClause` for BOTH `RECORD IS VARYING` (Format 3) AND `RECORD CONTAINS m TO n`
  (Format 2, m≠n, ISO §13.18.43). Binder + FileIoLowerer both derive from it, so they can't disagree.
- **Key resolution (qualified / position-based):** `SemanticModel.ResolveQualifiedData` (base + OF/IN quals —
  NEVER `dataReference().GetText()`, which concatenates "A OF B"→"AOFB"), `ResolveKeyData`, position-based
  `ResolveKeyOfReference` (ISO §14.9.41).
- **Paragraph dispatch:** symbol-based control transfer (dup names OK) + return-address `Dispatch(startPc,exitPc)`
  helper (`CilEmitter.EmitDispatchHelper`) for PERFORM…THRU; declaratives are in `ParagraphDispatchOrder`, main
  loop starts at `EntryParagraphIndex`.
- **X-cards** (`NistPreprocessor` + `ReferenceFormatProcessor`): `XXXXX###`/`XXXXP###`(produce)/`XXXXD###`(consume)
  placeholders → substituted at compile time; column-7 indicator letters select TPF/X-card line variants. See
  `reference_nist_xcards`. **Boundary-anchor** any new substitution (`(?<![A-Za-z0-9])XXXXX0NN(?![A-Za-z0-9])`)
  so it can't corrupt an embedded test-data literal (cf. IX106A's `…XXXXXXXX065A…`); use a `MatchEvaluator` if
  the replacement contains regex-special chars like `$`.
- **GOTCHA (costs an afternoon):** a NEW emitted `IrRuntimeCall`/IR node needs an explicit `CilEmitter` dispatch
  case, or it falls through to the `// NOP` tail with its args left on the stack → `InvalidProgramException` at
  Main. A stale `tests/nist/output/<t>.txt` can mask it — check the exit code and `rm` the output first.

## Tooling
- Build: `dotnet build src/CobolSharp.CLI/CobolSharp.CLI.csproj`  (CLI dll: `src/CobolSharp.CLI/bin/Debug/net9.0/cobolsharp.dll`)
- Compile one test: `dotnet <cli.dll> --nist tests/nist/programs/<T>.cob -o tests/nist/output/<T>.dll`
- Run it: `(cd tests/nist/output && timeout 30 dotnet <T>.dll)` — always copy `CobolSharp.Runtime.dll` into
  `tests/nist/output/` first; use a `timeout` + a file-size guard for SORT/build tests (an unsubstituted
  record-count X-card once grew an 8.2 GB report).
- Survey a suite: `bash scripts/run-suite.sh <PREFIX>`. NIST run convention: `export COBOL_SWITCH_1=ON`.
  Capture guard output to a file + `echo "guard exit=$?"` (piping to grep masks the exit code).
- Preprocess (debug COPY/REPLACE + X-cards): `cobolsharp preprocess <file> -o <out>`.

## How we got here (one line)
NC/IF complete early; collating subsystem (DEVLOG 224–227); spec-audit follow-ups (228–232); IC to its
non-file-I/O ceiling (233–236); the file-I/O FILE-CONTROL "wall" broken as over-strict-grammar-vs-spec
(237–268); paragraph-dispatch engine reworked (259–260); IX suite complete (269–282); ST suite complete
(283–294); Phase-1 quick wins + IC self-contained callers + index-name-in-LINKAGE fix + SQ/RL tails
(296–300, 299→350). Full detail in DEVLOG.md + `project_nist_progress` / `project_fileio_remaining`.
