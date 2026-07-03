> **Status: ADVISORY — council report, NOT yet owner-ratified.** Produced 2026-07-03 by an 8-agent council
> workflow (5 advisors — edition-gating, feature-catalog, architecture-readiness, verification, sequencing —
> → 2 adversarial critics [26 challenges, 14 gaps] → chair synthesis with in-repo re-verification of contested
> claims), evaluating the tree at commit `f73595f` (the DEVLOG-579 state). **The live plan remains
> `resume-prompt.md` over the SSOT `docs/COBOLNET_DESIGN.md` §16** — this report *upholds* that spine and
> proposes the deltas in its §3; §5 is the 11-item owner decision packet (with the council's recommended
> defaults). Once the owner ratifies/amends, fold the ratified deltas into the canonical docs
> (resume-prompt.md STATE, VERSION_TEST_MATRIX_DESIGN.md, COBOLNET_DESIGN.md §16) per process rule #4 and
> update this banner. Provenance: workflow run `wf_15c4eba5-547` (session b43d1d01), DEVLOG 580.

# COBOL.NET Completion Roadmap — Council Report

## 1. Where the project stands

The greenfield compiler (src/Cobol.Net.*, ~26k handwritten LOC) is a complete, byte-exact COBOL-85 compiler: Phase 1 (G0–G6) closed with all 318 golden-bearing NIST programs locked, 357/403 census GREEN with zero diffs, 1074 conformance + 29 unit tests green, and the full EC exception model (ISO §14.6.13) landed. The legacy guard stands at 353 MATCH + 11 LEGACY_DIVERGENT (ISO re-baselines cited in scripts/guard.sh), 0 regressions. G7 Phase-2 EditionValidator is design-complete (P2.1–P2.8, docs/VERSION_TEST_MATRIX_DESIGN.md after §8) but **zero-implemented**: no Validation/ directory, no permissive axis, no negative corpus, and 131 of 134 VERSION_CHANGE_REFERENCE.md rows still TODO. The tree is 22 days idle since commit f73595f (2026-06-11), and every off-repo scout artifact (/e/tmp/g7, /e/tmp/phase2-briefs, /e/tmp/nc-sweep) is verified gone — the in-repo docs are the sole surviving plan. Essentially the entire M2/M3/M4 positive catalog remains to implement greenfield (the ISO2023_CONFORMANCE_PLAN §3 checkmarks record the retiring legacy engine, per that doc's own banner). Chair-verified honest budget to G8: **~22–32 sessions** (the sequencing lens's 16–24 omitted ~5–10 sessions of council-added work and under-rated OO; corrections below).

## 2. The recommended path to done

The council unanimously upholds the canonical spine — validator first, then M2 (OO largest) → M3 → M4 → closure → G8 — with the reorders below. Chair adjudications of advisor conflicts and critic challenges are inline, each grounded in repo evidence I re-verified today.

### Phase 0 — Session-0 re-entry + owner decision packet — **S** (serial)
**Lands:** Fresh full clean build (never incremental after the gap — the DEVLOG-577 MSBuild-masking lesson), guard-fast + full conformance (1074) + unit (29) suites, spec-submodule check, end-to-end smoke; DEVLOG entry recording the verified /e/tmp artifact loss. Post the complete owner decision packet (Section 5) in one message — the critics are right that scope decisions currently parked "before M3" change **M2 and M4 sizing** and the standards-acquisition request is the plan's only external-lead-time dependency, so both move to day one. Proceed autonomously on the council's recommended defaults where no reply arrives (feedback_continue_dont_wait).
**Why now:** cheapest insurance against 22 days of environment drift; unblocks everything downstream.
**Exit:** DEVLOG-579 state reproduces green; decision packet posted.

### Phase 1 — EditionValidator Wave 1 (P2.1–P2.7) + metadata adjudications + runner shells — **M**
**Lands (main-session serial):**
- P2.1 EditionContext warning channel + Permissive axis + Removed() seam (verified errors-only today, EditionContext.cs:13-40); P2.2 visitor validator with the fail-fast-before-Emit reorder at CompilerDriver.cs:80-85; P2.3 the COBOLNET0900-0903 band; P2.4 scripted reserved-word tables generated **in-session** from spec §8.9 (~line 10306) — never agent-emitted (content filter tripped twice, DEVLOG 578).
- **Pre-P2.5 metadata scrub (critic fix, adopted):** before the ConstructDialectStatus registry + drift tests freeze constructs.json behind a guard, correct it. Chair-verified defects: (a) rows `json-generate-2014`/`xml-generate-2014` cite "ISO 2014 §14.9.x" while **json/xml have 0 hits in the entire 2023 spec** — remove or re-tag as vendor-extension rows pending the Phase-0 decision; (b) the TYPE IS row is seeded 2023, but TYPEDEF has ~33 spec hits yet **zero Annex E (2014→2023 change) rows** — proof it predates 2023, so the `{is2023()}?` gate at CobolData.g4:246 is provably wrong; set a **provisional 2002** edge (feature-catalog's lineage claim; final edge blocked on the older standards) with a provisional-confidence marker. This overrules the architecture lens's "resolve via VCR lookup, scope S" — the VCR is a 2014→2023 ledger and cannot answer (critic 2 confirmed); (c) align the XOR rows to Annex E (see Phase 2).
- P2.5 registry + both drift tests; P2.6 gates (~14) + 3 binder migrations, **plus** the 85-side runtime-loud fix for the STOP-literal mis-bind (StatementBinder.cs:168) in the same commit as its ≥2002 gate — the critic is right that splitting the two halves across waves leaves a silent 85 semantics bug live.
- P2.7 as a **one-time global** permissive flip (not per-gate — critic's decomposition fix adopted): P2.1 axis → flip + first LABEL RECORDS gate in one commit (243/459 NIST programs affected, verified) → remaining gates ordinary. **Attached to the flip commit: re-run the 318 goldens at --std 2023 permissive and triage every diff against VCR behavior rows.** This answers the critics' one **fatal** challenge — the shipping default edition would otherwise never be behaviorally executed before G8. Golden re-match at the default edition becomes a G7 exit criterion (INV-1-strong at 2023; other editions best-effort triage).
- Wire scripts/version-continuity-sweep.sh into .github/workflows/build-and-test.yml (verified: CI runs legacy guard.sh + both greenfield suites but **not** the sweep — an unowned gap the council adopts).
- Docs in the same change set: Validation/ deep-dive banner + DOC_INDEX row (CLAUDE.md doc-map rule); the measurable G7/G8 exit criteria (verification rec 5) with the critic's wording fixes — traceability = "any recognized edition-band code" (0801/0802/0810/0811/0873/0875-0879/0882/0893/0900-band), census criterion "**≥**357 GREEN"; a validator compile-time perf measurement (feedback_guard_speed).

**Parallel agent waves:** read-only OO re-scout (2-3 agents: legacy algorithm mining per DEVLOG 447-456, regenerating the lost oo-plan brief **into** docs/COBOLNET_OO_DESIGN.md, folding the four spec-verified corrections from memory project_oo_reuse_legacy); the positive-corpus discovery runner and the negative-corpus (.cob + .err) runner as **shells with an explicit pending manifest** — the critic's serious challenge to verification rec 2 is upheld: tests/conformance/2002 is full of unimplemented-feature programs (national_data, boolean_data, based_pointer, float_usage, logical_xor — verified by listing), so unconditional discovery today is mass-red; entries are enabled per feature as waves land. Create tests/conformance/2014/ (empty + manifest) now so the directory gap stops being invisible.
**Exit:** P2.1-P2.7 green; continuity sweep green permissive at all four editions with every strict failure tracing to a recognized edition-band code; 2023-permissive golden triage complete; drift tests green over **scrubbed** metadata; runners discovering with manifests.

### Phase 2 — Validator W2 (parallel) + W1.5, then W3 (serial grammar batch) — **M**
**Lands:**
- **W2 (parallel agents, disjoint files, per-agent worklist files):** MOVE VCR rows (1, 92/128); the MOVE ALL-digit latent bug; the **loud-guard silent-misbind sweep** (architecture rec 2, scheduled here per its own P2.8 fit): PicInfo.ParseUsage's silent DISPLAY fallback for 2002+ usages, PIC symbol whitelist (N/E/1 loud), classDefinition silent drop in CallCollectUnits, the UsageKeyword string-strip fallback — plus the national/boolean **skeleton** (PicCategory/Usage enum entries, edition gates, loud not-implemented) so the holes close permanently; negative-corpus seeding **≥1 .err case per Wave-1 gate** (verification's per-gate timing wins over edition-gating's W2-pull — the wrong-reason-rejection argument applies to the first gate) plus the reserved-word interval witnesses; VCR status flips; adversarial review.
- **W1.5:** upgrade the grammar introduction-gate rejections to edition-naming 0900 diagnostics. Chair-verified count: **~24 actual gate sites** (44 grep hits minus 20 comment-only doctrine lines in CobolDialect.g4) — edition-gating's "39, scope M, highly parallel" is overruled: the doc figure is stale, and its fan-out has every agent writing the same visitor class + constructs.json (the exact shared-file race verification's own risk names). Run it **serially or as fragment-merge**, scope S-M, noting the critic's mechanism caveat: predicate failures surface as parse errors pre-bind, so some constructs need parse-error mapping or binder-side gates.
- **W3 (single serialized end-of-session grammar batch — sequencing's serialization wins over edition-gating's W2∥W3 concurrency; the waves contend on VCR rows and constructs.json):** the XOR/EXCLUSIVE-OR regating. **Chair adjudication of the in-repo contradiction:** VCR rows 32/41 cite the spec's own change annex (E.2 item 25 / E.3.2 item 4: these were user-defined words before 2023); ISO2023_CONFORMANCE_PLAN M4-2a's "(2002-era)" is an unsourced parenthetical — presence in the 2023 spec body (§8.7.6/§8.8.4.9, verified) proves nothing about 2002. **Annex E wins: the operators are 2023.** W3 therefore also corrects the M4-2a note, re-editions tests/conformance/2002/logical_xor to 2023, and runs the **2002-corpus edition audit** (33 programs' introduction claims vs the VCR — the critics' unowned gap, now owned here). Plus the notInGrammar 85-acceptance set, preprocessor DialectLevel threading (VCR 2/4/94), full legacy guard + committed regenerated parser (DEVLOG-554 rule), and the queued path-portable regen fix.
**Exit:** all W2/W3 VCR rows flipped; every gate has a negative witness pinning its code; corpus audit clean; full legacy guard green.

### Phase 3 — M2 OO port — **XL** (upgraded from sequencing's L; feature-catalog wins)
The DEVLOG record (entries 447-456, multiple sessions) covered only slices 1-3b, and FACTORY is not even in the grammar (CobolOO.g4:17); PROPERTY, INTERFACE-ID, universal object reference, and EC-OO are net-new mandatory surface (A.4.10 makes only multiple inheritance + parametric polymorphism optional).
**Lands:** (1) **Serial spine first** (sequencing's ordering wins over feature-catalog's immediate-parallel M2 data waves): the GOBACK-vs-STOP-RUN bind-time split + the CSharpEmitter emit-into-a-type parameterization. Chair note on the advisor conflict: the deep-dive (COBOLNET_OO_DESIGN.md:18, D8, :155-157 — verified) still requires the **binder-side** BoundMethodReturn/BoundStopRun split; the runtime ProgramReturn carrier architecture cites does not resolve it — the deep-dive stands, and it must be reconciled with the as-built state in the first docs commit (process rule 4). (2) Slice waves in the proven legacy order (port algorithms, never the byte substrate — owner directive), then FACTORY → PROPERTY (§13.18.42) → INTERFACE-ID (§11.6) → universal reference → EC-OO + exception objects (RAISE identifier) + GLOBAL-walkable F3 declaratives (the deep-dive's recorded deferral homes). Each slice ships its conformance programs (oo_* pairs enabled in the manifest), its reject-at-85 matrix rows, and its negative cases in the same commit. **OO grammar increments require the Phase-0 owner grant** — the critic is right that feedback_autonomous_grammar_nist covers NIST work only; sequencing's "pre-authorized" claim is overruled pending that grant.
**Parallel:** slice implementation + corpus authoring fan out after the spine; FACTORY/PROPERTY are disjoint once type emission exists.
**Exit:** legacy slice-order parity + the mandatory net-new surface; 14 OoTests re-landed as greenfield facts; multi-base INHERITS rejected loudly per SSOT §18 item 18.

### Phase 4 — M2 residual catalog — **L** (after the OO spine; can overlap late OO slices with verified file partitions)
**Lands:** half-session greenfield-vs-catalog reconciliation audit first (the §3 checkmarks are legacy-era; several items already landed greenfield — intrinsic catalog, ROUNDED modes, standard-decimal, EC), adding a greenfield-status column so waves are sized against truth. Then parallel disjoint tracks: (a) national/boolean data end-to-end (skeleton from Phase 2; char-vs-byte boundary design at AsImage/IRecordCodec first) + the EC -N twins + EXCEPTION-FILE-N; (b) pointers/ALLOCATE/FREE/BASED/SET ADDRESS on the settled ManagedPointer carrier (COBOLNET_INTERPROGRAM_DESIGN D1/D5); (c) UDF units/prototypes/REPOSITORY (§8.13/§12.3.8); (d) SHARING/LOCK/RETRY + line-sequential (contingent on the A.4.7 decision); (e) **ARITHMETIC IS STANDARD positive behavior at 2002/2014** + the M2-ARITH-2 recognize-and-ignore residuals — the critics' orphaned behavior obligation, now owned. Alongside: the **interim** 85→2002 delta work — grow VCR Table 7 from the legacy FlagsFeaturesRemovedAfter85/DialectStrictnessChecks inventory (S-M, scheduled), with the full delta-research track carried as **blocked on standards acquisition** (edition-gating's "L track, depends on W1 registry" is overruled — Table 7 rows are markdown; the only real blocker is sourcing the documents).
**Exit:** every track's positive corpus discovered by the greenfield runner + matrix rows + negative cases; catalog marks flipped to greenfield truth.

### Phase 5 — Deferred-intrinsics backlog — **M** (runs parallel with Phase 4/6; leaf functions, disjoint from emitter core)
**Lands:** the 43 IntrinsicBind.Deferred rows by family (2002 set, 2014 FORMATTED-* dates, seven 2023 rows), each with **window-enforcement negative rows** (a later-edition function under an earlier --std emits the per-edition diagnostic — M4-3's mandate, the critics' dangling gap), firming the provisional windows as each lands. -N twins wait on Phase 4(a).
**Exit:** zero Deferred rows; windows non-provisional or explicitly blocked on standards.

### Phase 6 — M3 2014 — **L**
**Lands:** the OCCURS DYNAMIC deep-dive **before** implementation (fixed-physical-capacity is load-bearing across DataItem.Occurs/OdoModel/image facility — architecture rec 6; a scout inventories assumption sites in parallel), then dynamic-capacity tables as the serial spine; TYPEDEF/SAME AS/TYPE TO (provisional 2002/2014 edges per Phase 1) parallel; the **M3-4 catchall** — IEEE-754 float usages (currently silent-misbinding, made loud in Phase 2), function/method pointers, increased limits, conditional-expression enhancements — now explicitly owned here (critics' gap). **JSON/XML is removed from M3** — sequencing rec 6 is overruled on feature-catalog's verified evidence (0 spec hits; the M3-3 "§14.9" citation is wrong): it is vendor-dialect work, default-deferred post-G8 per the Phase-0 decision. The 2014 positive corpus grows with every feature (directory seeded in Phase 1).
**Exit:** 2014 corpus non-empty and discovered; dynamic-table matrix rows green at all editions.

### Phase 7 — M4 2023 deltas + EC remnants + behavior-row burn-down — **L** (upgraded from sequencing's 1-2 sessions per the critic; contingent on Phase-0 scope decisions)
**Lands:** PERFORM...WHEN exception-checking + >>PROPAGATE (§7.3.21), the EXCEPTION-FILE 2023 connector argument, SMALLEST-ALGEBRAIC + EXCEPTION-FILE-N, bit/boolean 2023 additions, group SYNCHRONIZED / NO SIGN packed rows; the flag-obsolete warning rows (VALIDATE family 117-125/129 etc.) on the Phase-1 warnings channel; and — the critics' serious orphan fix — an explicit **disposition/sizing pass then implementation wave for the ~44 Table 1+5 behavior rows** on already-complete subsystems (I-O status, VALUE semantics, FLAG-02/FLAG-14 directives), which no feature wave would otherwise touch. MCS and commit/rollback ride the Phase-0 decision (default: documented non-support — MCS via **Annex A.3**, not A.4.3, per the critic's verified correction; A.4.3 is commit/rollback only).
**Exit:** VCR Tables 2/3 rows dispositioned green or documented; Table 1/5 rows implemented or per-row dispositioned.

### Phase 8 — Matrix closure + greenfield guard + equivalence proof — **M**
**Lands:** drive all VCR rows to zero TODO (green or written disposition); full INV-1/INV-2/INV-3 sweeps, strict + permissive legs, **including golden re-match at --std 2023** (INV-1-strong at the default edition — the fatal-challenge criterion); negative-corpus completion with the registry-coverage unit test (every registry entry ≥1 case); build the **in-repo greenfield guard** (rebuilding the lost /e/tmp/nc-sweep census tooling; full 403-census basis, run-only/chain-intermediate handling ported from guard.sh), run the one-time verdict-diff **equivalence proof against the legacy guard while it still runs** — the single irreversible ordering constraint in the plan — and migrate the 11 LEGACY_DIVERGENT ISO citations into the new guard/a LEDGER doc.
**Exit:** the Phase-1 G7 exit criteria all satisfied, as counts/exit codes.

### Phase 9 — G8 in three serial cuts — **M** (strictly serial; adopted over the SSOT's implied single step)
**Cut 1:** drop legacy from the test graph (convert the ~47 differential files per the Q13 decision — pinned goldens recommended, amortized opportunistically from Phase 3 onward), replace the CI guard.sh step with the greenfield guard. **Cut 2:** delete the byte engine (legacy preserved at a git tag). **Cut 3:** rename CobolSharp→COBOL.NET/cobol.exe as one atomic commit with regenerated committed Generated/* (the regen-path portability fix landed in Phase 2), the final doc/DOC_INDEX pass, and the **ISO §4.2 conformance documentation** (which optional elements are supported — the artifact that makes A.4 documented non-support conformance-legal; the critics' gap, now a named G8 deliverable).
**Exit:** grep-clean of legacy references; one greenfield guard exits 0 covering goldens + census (≥357 GREEN) + per-edition discovery + negative corpus + sweeps + dotnet test.

## 3. Changes vs the current canonical plan

The council **confirms** the SSOT §16 / resume-prompt.md backbone (EditionValidator → M2 with OO largest → M3 → M4 → G8) and the P2.1-P2.8 plan as written. Deltas:

1. **Owner decisions and standards acquisition move to Session-0** (canonically parked mid-plan/"before M3"). They gate M2/M4 sizing and TYPEDEF/RW-130d adjudication.
2. **A metadata-adjudication step is inserted before P2.5**: constructs.json scrub (JSON/XML fiction, TYPE IS edition, XOR alignment) so the drift tests never fossilize wrong data. Not in the canonical plan.
3. **JSON/XML is removed from M3** (SSOT §16 lists it): not ISO (verified 0 spec hits); vendor-dialect disposition to the owner, default post-G8.
4. **A behavioral leg at the default edition attaches to the P2.7 flip** (318 goldens at 2023 permissive) and INV-1-strong-at-2023 joins the G7 exit criteria — the canonical plan never executes behavior at 3 of 4 editions before G8.
5. **New named workstreams** absent from §16: W1.5 (~24 intro-gate diagnostic upgrades), the silent-misbind loud-guard sweep, the positive/negative discovery runners with pending manifests, the 43-row intrinsics backlog, the Table 1/5 behavior-row wave, the greenfield guard + equivalence proof, CI wiring, and the §4.2 conformance document.
6. **G8 splits into three serial cuts** (test-graph → deletion → rename) vs the implied single step, with the equivalence proof hard-ordered before Cut 1.
7. **Budget re-baselined to ~22-32 sessions**; OO re-rated XL.

## 4. Top risks

1. **Content-filter kill during reserved-word work** (occurred twice). *Baked in:* Phase 1 generates all tables in-session via scripts/gen-reserved-words.ps1 from spec §8.9; every G7 agent brief forbids emitting word lists — agents return counts and file paths only.
2. **A removal gate lands before the permissive axis, turning the guard corpus-red** (243/459 NIST programs carry the affected clause). *Baked in:* Phase 1 hard-sequences P2.1-P2.5 before any P2.6 gate; the one-time flip + first gate ship in a single commit with the sweep green as the commit gate.
3. **Drift tests fossilize wrong edition metadata** (non-ISO JSON/XML rows, a provably wrong 2023 TYPE gate, the XOR mislabel), forcing multi-wave rework of registry + grammar + matrix + regen. *Baked in:* the pre-P2.5 scrub and the three chair adjudications land inside Phase 1/2, before the registry freezes.
4. **The G8 equivalence window closes unproven** — census tooling is already lost off-repo; deleting the oracle first makes the greenfield guard unverifiable forever and destroys the 11 re-baseline citations. *Baked in:* Phase 8 builds the in-repo guard, runs the verdict-diff while legacy still runs, and migrates the citations; Phase 9 Cut 1 cannot start until Phase 8 exits.
5. **Silent mis-binds produce false greens through the M2/M3 waves** (2002+ usages → DISPLAY, PIC N → numeric, dropped class definitions — pattern-mates of the STOP-literal bug). *Baked in:* the Phase 2 loud-guard sweep closes the family before any feature wave exercises those paths, each closure with an adversarial negative test in the same commit.

## 5. Decisions needed from the owner

| # | Decision | Council recommendation |
|---|---|---|
| 1 | Acquire ISO 1989:2002 and :2014 (or their change annexes) for specs/? | **Yes — request at Session-0** (only external-lead-time dependency; gates TYPEDEF edge, VCR Table-7 growth, RW row 130d, 2014 reserved-word confidence). Until then: legacy inventory = interim authority, provisional markers on affected edges. |
| 2 | JSON/XML GENERATE/PARSE — not ISO (0 spec hits): keep/defer/drop? | **Re-tag as vendor-dialect extension, defer post-G8**; scrub from the conformance matrix now; if built later, pick a dialect model (IBM Enterprise) and add a dialect axis. |
| 3 | Annex A.4 optional-module line (screen A.4.2, commit/rollback A.4.3, dynamic tables A.4.4, dynamic length A.4.5, extended letters A.4.6, sharing/locking A.4.7, FORMAT/SELECT WHEN A.4.8, locale A.4.9, REWRITE/WRITE FILE A.4.13, VALIDATE A.4.14; MCS via A.3)? | **Implement:** dynamic tables, dynamic length, sharing/locking. **Documented non-support** (with the §4.2 conformance doc + uniform not-supported diagnostics): screen, MCS, commit/rollback, locale, extended letters, A.4.8, A.4.13, and **VALIDATE** (obsolete-in-2023, VCR row 95: "never implemented; no interest" — the feature-catalog's implement recommendation is overruled). |
| 4 | --permissive: supported CLI surface or internal affordance? | **Supported, documented migration mode** (a modest fifth persona); warning-text stability guaranteed only for the 09xx codes, not prose. |
| 5 | Strict-by-default UX at --std 2023 once removal gates land? | **Keep strict** (§10 decision 2 stands); diagnostic text names the removing edition and both escape hatches (--std 85, --permissive). No W1 block — logged, not gating. |
| 6 | Grammar-change approval for OO/M2 increments (FACTORY, INTERFACE-ID, PROPERTY…)? | **Explicit grant requested** — the NIST pre-authorization does not cover them; same logging + full-guard discipline as feedback_autonomous_grammar_nist. |
| 7 | OO v1: multiple inheritance + parametric polymorphism? | **Documented non-support per A.4.10**, loud rejection of 2+ bases; SSOT §18 item 18 stays deferred until a real program triggers it. |
| 8 | Q13 — fate of the ~47 differential test files at G8? | **Convert to pinned goldens** (amortized from Phase 3 onward); archive legacy at a git tag with a WSL run recipe rather than irrecoverable deletion. |
| 9 | COMMUNICATION module? | **Documented non-support** + clean per-edition diagnostics (a validator row, not an implementation). |
| 10 | INV-1 strong form scope for G7 exit? | **Required at the default edition (2023)** — golden re-match, seeded by the Phase-1 flip-commit run; other editions triaged best-effort, promoted at Phase 8. |
| 11 | G8 = finish line, or does the full architectural review live inside it? | **Separate post-G8 phase**, along with packaging/distribution (dotnet tool vs installer, runtime redistribution, licensing) — currently owned by no phase; schedule after cut-over. |

## 6. Coverage ledger

| Obligation | Phase |
|---|---|
| Session-0 rebuild/baseline, decision packet, standards request | 0 |
| Validator P2.1-P2.5 (permissive axis, visitor, 0900 band, word-table generation P2.4, registry + drift tests) | 1 |
| constructs.json metadata scrub (JSON/XML, TYPE IS, XOR rows) — pre-P2.5 | 1 |
| P2.6 removal gates + binder migrations (+ STOP-literal 85-side fix) | 1 |
| P2.7 one-time permissive flip + continuity restatement | 1 |
| Behavioral leg at default edition (318 goldens @ 2023 permissive; INV-1-strong seed) | 1 (promoted at 8) |
| Positive-corpus discovery runner (pending manifest) + negative (.err) runner + 2014 dir seed | 1 |
| CI wiring of the continuity sweep | 1 |
| Validation/ deep-dive + DOC_INDEX row; measurable G7/G8 exit criteria; validator perf budget | 1 |
| OO re-scout + deep-dive corrections folded in | 1 (parallel) |
| W2: MOVE rows, MOVE ALL-digit bug, VCR flips, adversarial review | 2 |
| Silent-misbind loud-guard sweep + national/boolean skeleton gates | 2 |
| Negative-corpus seeds (per Wave-1 gate + reserved-word interval witnesses) | 2 (grows every phase) |
| W1.5: ~24 intro-gate diagnostic upgrades | 2 |
| W3: XOR regating per Annex E + M4-2a correction + logical_xor re-edition + notInGrammar set + preprocessor threading + regen-path fix | 2 |
| 2002-corpus edition audit | 2 |
| M2 OO: spine (GOBACK split, type-parameterized emitter), legacy slices 1-6, FACTORY, PROPERTY, INTERFACE-ID, universal reference | 3 |
| EC remnants: exception objects, GLOBAL-walkable F3 | 3 |
| Greenfield-vs-catalog reconciliation audit | 4 |
| M2: national/boolean data (+ EC -N twins, EXCEPTION-FILE-N) | 4 |
| M2: pointers/ALLOCATE/FREE/BASED/SET ADDRESS | 4 |
| M2: UDF prototypes/REPOSITORY; SHARING/LOCK + line-sequential (per decision 3) | 4 |
| ARITHMETIC IS STANDARD behavior @ 2002/2014 + M2-ARITH residuals | 4 |
| Interim 85→2002 Table-7 growth (legacy inventory); full delta research + RW row 130d | 4 (interim); blocked track per decision 1 |
| 43 deferred intrinsics + window-enforcement negative rows | 5 |
| M3: OCCURS DYNAMIC deep-dive then dynamic tables; TYPEDEF/SAME AS/TYPE TO; M3-4 catchall (IEEE-754 floats, function pointers, limits) | 6 |
| JSON/XML | Owner decision 2 (default post-G8) |
| M4: PERFORM...WHEN, >>PROPAGATE, EXCEPTION-FILE 2023 arg, SMALLEST-ALGEBRAIC/EXCEPTION-FILE-N, bit/boolean additions | 7 |
| VCR Table 1/5 behavior rows (~44) disposition + implementation | 7 |
| Flag-obsolete warning rows (VALIDATE family etc.); MCS/commit-rollback disposition diagnostics | 7 |
| Matrix closure: all VCR rows dispositioned; INV-1/2/3 sweeps strict+permissive | 8 |
| Greenfield guard + census rebuild + equivalence proof + LEGACY_DIVERGENT citation migration | 8 |
| G8 Cut 1: test-graph cut + CI guard replacement + Q13 execution | 9 |
| G8 Cut 2: byte-engine deletion (legacy tagged) | 9 |
| G8 Cut 3: rename + committed regen + final doc pass + §4.2 conformance document + DOC_INDEX reconcile | 9 |
| Architectural review; packaging/distribution/licensing | Post-G8 (decision 11) |