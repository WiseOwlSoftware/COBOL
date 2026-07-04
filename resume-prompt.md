# COBOL.NET — Next-Session Kickoff Prompt

> ⛔🔥 **PIVOT (2026-06-08, DEVLOG 457 — THIS supersedes the byte-engine plan below).** The owner directed a
> **blank-slate rewrite**: a NEW compiler that translates COBOL → **idiomatic typed-native C# source, compiled by
> Roslyn** (a COBOL record IS a .NET `record struct`; an elementary item IS a native field). **There is NO byte
> `ProgramState` substrate** — do not reintroduce it, do not "fall back" to the legacy byte engine. The legacy
> `CobolSharp.Compiler` is kept ONLY as a differential oracle until cut-over. **READ `docs/COBOLNET_DESIGN.md` FIRST**
> — the decision-complete SSOT (bound-tree pipeline [NO lowered IR], data model, native scaled-integer numerics,
> PC-dispatcher control flow, REDEFINES, strings, files, interprogram, OO, conditions/EC, intrinsics, project
> reorg/rename to Cobol.NET/cobol.exe, no-god-class structure, C# 14 usage, the §18 settled decisions, and the G0–G8
> build order). `COBOLNET_ARCHITECTURE.md` is the brief overview. Memory: `feedback_complete_dotnet_migration_no_byte`,
> `feedback_fully_autonomous_push`. Tests may break mid-transition; the bar is 100% green at completion.
>
> **MISSION (restated 2026-06-09): drive to FULL ISO/IEC 1989:2023 AND correct support for ALL PRIOR EDITIONS
> (1985 / 2002 / 2014), validated by the VERSION TEST MATRIX (test the compiler as N per-edition compilers).** Two
> interlocking tracks: (T1) the **version-correctness framework** — `docs/VERSION_CHANGE_REFERENCE.md` (the 130-row
> edition-change checklist) + `docs/VERSION_TEST_MATRIX_DESIGN.md` (the (construct × edition) matrix); and (T2) the
> **feature/corpus drive** — green the NIST corpus + implement the M2/M3/M4 catalog. Every behavior that changed across
> editions is gated by `DialectLevel`; new features enabled only at ≥ their edition; obsolete/removed flagged. (Memories
> `feedback_version_test_matrix`, `feedback_version_targeted_semantics`.)
>
> **CI NOTE (RETIRED at DEVLOG 596 — kept for history): the DEVLOG-554 rule ("commit the regenerated parser
> with any .g4 change") is OBSOLETE. The regen has been path-portable + fail-hard since DEVLOG 555/556, and
> as of DEVLOG 596 `src/Cobol.Net.Frontend/Generated/` is fully UNTRACKED (the 8 leftover tracked files were
> removed — their checkout mtimes were suppressing CI regeneration, which is how the d6c8143 stale-TYPE-gate
> incident happened, DEVLOG 591). A fresh checkout ALWAYS regenerates (java + pwsh are build prerequisites,
> both preinstalled on the GitHub runner images); a failed generation FAILS the build. There is no committed
> parser to keep in sync anymore.**
>
> **STATE (DEVLOG 595, 2026-07-03 21:10): ⛔🎉 ROADMAP PHASE 2 W2 + W1.5 COMPLETE (commits a3e29b6 + 1f2156b) —
> the four-track W2 wave (A: the MOVE rows 1/92/128 + both latent bugs on the StoreAsImage substrate ·
> B: the loud-guard misbind sweep, ParseUsage/Analyze now (EditionContext,where), COBOLNET0899/0808, the
> allocate/free/invoke false-greens exposed→pending · C: the 18-case negative corpus, all CLI-verified ·
> D: position-aware reserved words, `IsProvableUserWordPosition`, the RW104A hazard closed — only 7 of 34
> band tokens even had §8.9 rows) + the ADVERSARIAL REVIEW (6 confirmed / 0 refuted, ALL fixed: SR1
> class-index 0809, the ref-mod round-trip loss `MarkRefModStoreImage`, the QUOTE dual row
> move-quote-numeric-obsolete-2014 [E.2 item 21], the ';' single-strip + `PIC ;` leak, '$'-under-custom-
> currency, .err specificity ×18) + W1.5 (all 17 reachable intro-gate sites → COBOLNET0900 edition-naming
> via the new frontend `EditionGateHints` parse-layer mapping; JSON/XML → vendor COBOL0313; the 0860/0861
> double allocation resolved by registry migration; 5 new rows; expectDiagnostic on 21 rows). Battery:
> conformance 1353/1353 · unit 102/102 · sweep 419 OK/0 BREAKS · INV-1-STRONG 349/349 byte-exact at
> 2023-permissive · FULL legacy guard-fast ALL GREEN. VCR: rows 1/5/6/7/32/89/90/92/126/127/128 GATED,
> 28 PARTIAL (QUOTE leg), Table 7 grown 7.4–7.14 (7.14 = the trailing-`,` KNOWN MISBIND — the `;` twin —
> queued to the W3 lexer cure; 7.13 = the multi-char-ALL 2002-edge research row).
> **W3 PART 1 DONE (DEVLOG 596, commit follows 035d42f):** ① `Generated/` fully UNTRACKED — the DEVLOG-554
> rule is RETIRED (fresh checkouts always regenerate; the tracked files' checkout mtimes were suppressing CI
> regen — the d6c8143 incident's root); ② the PIC_STRING separator cure LANDED (VCR 7.14 FIXED — trims a
> trailing `,`/`;` only when LA(1) is whitespace, the §8.3.5 r2 shape; the LA-guard is load-bearing: NC125A's
> legal SR7 `…9,.` mask broke the naive version); ③ XOR/EXCLUSIVE-OR REGATED to 2023 (VCR 41 GATED:
> `{is2023()}?` operator + cobolWord/_dataNameTokens/CheckedTokenTypes admission + 3 registry rows + the
> corpus re-edition + the M4-2a doc correction + the legacy XOR test retargeted to Cobol2023). Battery:
> conformance 1367/1367 · sweep 419/0 · INV-1-STRONG 349/349 · FULL legacy guard green (353 MATCH,
> 1204 unit, 537 integration).
> **RESUME = W3 part 2:** ④ the notInGrammar 85-acceptance set (RERUN, ENTER, USE FOR DEBUGGING, section
> segment-numbers — accepted-inert at 85 + 0902 ≥2002, Table-7 rows); ⑤ preprocessor DialectLevel threading
> (VCR 2/4/94 — CopyProcessor/ReferenceFormatProcessor); ⑥ the 2002-corpus edition audit (33 pending
> programs vs the VCR). Then Phase 3 = the M2 OO port (docs/COBOLNET_OO_DESIGN.md carries the port map; the
> OO grammar grant is standing). Deferred-from-review (queued, documented): the §13.18.40.6 PICTURE
> precedence-table pass; Tier-A/BINARY figurative-MOVE receivers stay runtime-loud; exit-window conforming
> witnesses need METHOD-ID/FUNCTION-ID units (Phase 3/4c).**
>
> **(superseded) STATE (DEVLOG 590, 2026-07-03 21:10): ⛔🎉 ROADMAP PHASE 1 COMPLETE — the EditionValidator Wave 1 landed
> END-TO-END in one autonomous session (DEVLOG 583–590, commits c0cf723…): P2.1 channels + `Removed()` seam ·
> P2.2 validator pass (fail-fast pre-Emit) · P2.3 the 0900–0903 band · P2.4 the FOUR-SOURCE reserved-word
> tables (GnuCOBOL per-standard lists disk-to-disk + spec §8.9 + VCR row 32; the funnel LIVE, token-type
> restricted per the RW104A hazard; THREE recall corrections incl. the third re-reserved word END-RECEIVE and
> the EC words being 2002-reserved) · pre-P2.5 scrub + the TYPE `{is2002()}?` gate fix · P2.5 registry + both
> drift disciplines · P2.6 the full Wave-1 gate batch (15 rows, 12 overrides, the STOP-literal BoundStopLiteral
> fix, the 0873 FD+SD migration, the 0903 archaic flags) · P2.7 THE FLIP (permissive continuity restated;
> **INV-1-STRONG AT 2023: 349/349 goldens BYTE-EXACT at `--std 2023 --permissive`, zero behavioral diffs** —
> the roadmap's fatal-challenge criterion, seeded via `COBOLNET_NIST_STD`/`COBOLNET_NIST_PERMISSIVE`) · the
> permissive sweep 419 OK/0 BREAKS wired into CI · `docs/COBOLNET_VALIDATION_DESIGN.md` (as-built + the
> measurable G7 exit criteria) · the corpus runner shells + manifests (2002: 0/33, 2014 seeded, negative/
> shell). Suites: conformance 1195/1195 + unit 38/38 + guard 353 MATCH. ⚠ Content filter tripped a 4TH time
> (even file-Writes of word lists) — the standing rule is now ABSOLUTE: word lists move disk-to-disk only.
> ⚠ guard-fast showed 2 transient single-program flakes (re-run clean; JOBS=32 race suspected, watched).
> **RESUME = roadmap Phase 2: W2 (MOVE rows SR5 · the loud-guard misbind sweep + national/boolean skeleton ·
> negative-corpus seeds · position-aware token checking · VCR flips · adversarial review) → W1.5 (~24 0900
> upgrades) → W3 (the serialized grammar batch + FULL legacy guard). Then Phase 3 = the M2 OO port (the
> refreshed `docs/COBOLNET_OO_DESIGN.md` carries the regenerated port map).**
>
> **(superseded) STATE (DEVLOG 582, 2026-07-03 14:28): ✅ roadmap Phase 0 COMPLETE — the DEVLOG-579 baseline reproduces
> green after the idle gap (fresh full build 0W/0E; 1074 conformance + 29 unit; guard-fast 353 MATCH,
> 0 regressions). ✅ The roadmap is now ISO-VALIDATED against `specs/ISO_COBOL.md` (12-agent workflow: 39
> claims — 30 confirmed / 9 partial / **0 refuted**; 10 serious coverage gaps) — corrections D1–D16 + minors
> APPLIED INLINE (newly-owned mandatory surface: boolean OPERATIONS, `&`-concat, CONSTANT, DYNAMIC-LENGTH
> items, §4.2.2 suboption, EXTERNAL cluster; >>PROPAGATE re-editioned ≤2014; the §4.2.16 seven-leg conformance
> doc; the Phase-1 scrub gains the float-edge split + CONSTANT/`&` row seeds; word tables designed per-unit
> overridable for COBOL-WORDS); the audit report is the roadmap's Appendix. RESUME superseded above.**
>
> **(superseded) STATE (DEVLOG 581, 2026-07-03): ⛔ the G7→G8 EXECUTION ROADMAP is `docs/COMPLETION_ROADMAP_COUNCIL.md` —
> RATIFIED by the owner (all 11 §5 decisions resolved: #1 = NO standards acquisition, the in-repo 2023 spec
> [`specs/` PDF + MD] is the sole ISO authority with Annex E + the legacy inventory adjudicating prior-edition
> edges; #2–#11 = council defaults, incl. the #6 OO/M2 grammar grant). Docs-only checkpoints (580/581); the
> tree is otherwise at the DEVLOG-577 code state. The roadmap upholds the SSOT §16 spine + 7 ratified deltas
> (its §3) incl. the behavioral leg at `--std 2023` on the P2.7 flip, the pre-P2.5 constructs.json scrub
> [JSON/XML rows are non-ISO → vendor-dialect post-G8; the CobolData.g4:246 `{is2023()}?` TYPE gate is
> provably wrong → provisional 2002; XOR = 2023 per Annex E], and G8 in 3 serial cuts. **RESUME = the roadmap's
> Phase 0 (clean full rebuild + baseline re-verification after the idle gap) → Phase 1 (EditionValidator Wave 1
> = P2.1–P2.7 per the VERSION_TEST_MATRIX_DESIGN "Phase-2 implementation plan" section + the roadmap's Phase-1
> additions).** ⚠ The `/e/tmp/g7` + `/e/tmp/nc-sweep` convenience copies are VERIFIED GONE — the in-repo doc
> sections are the ONLY source.**
>
> **(superseded) STATE (DEVLOG 579, 2026-06-11 13:42): ② G7 Phase-2 EditionValidator — DESIGN COMPLETE, IMPLEMENTATION NOT
> STARTED (docs-only checkpoints 5be1983 + this; tree otherwise at the DEVLOG-577 state). ⛔ RESUME = read
> **`docs/VERSION_TEST_MATRIX_DESIGN.md` → "Phase-2 implementation plan" (below its §8)** — the IN-REPO canonical
> plan (P2.1 EditionContext warning-channel + Permissive axis + Removed() seam; P2.2 the visitor hook [no listener
> — `CobolParserCoreBaseVisitor<object?>`]; P2.3 the COBOLNET0900–0903 band; P2.4 the cobolWord single-funnel
> reserved check + scripted §8.9 table generation incl. OCR fixes/METHOD omission + the conservative-confidence
> policy; P2.5 registry+drift; P2.6 the Wave-1 construct checklist; P2.7 harness changes + the
> NIST-continuity→permissive flip; P2.8 Waves 2–3 incl. the latent bugs: STOP-literal mis-bind, MOVE ALL-digit
> loud gap, the XOR/EXCLUSIVE-OR 2023-words-as-unconditional-tokens hole). Convenience copies + raw scout
> artifacts: `/e/tmp/g7/` (RESUME-G7-PHASE2.md, words-2023.json, removal-inventory.json, validator-arch.md) —
> the doc section is SSOT if they diverge. ⚠ Do NOT delegate word-list output to agents — content-filter trips
> on it (2nd occurrence, DEVLOG 578); build tables in-session by script from spec §8.9 (lines 10306–10788).
> Then implement Wave 1 exactly per P2.1–P2.7.**
>
> **(superseded) STATE (DEVLOG 577, 2026-06-11 12:09): ✅ the EC EXCEPTION-CONDITION MODEL (SSOT §16 step ①) is DONE END TO
> END — ISO §14.6.13 + §7.3.25 >>TURN (the compile-time TurnState fold; zero-scaffolding when off) + §14.9.29
> RAISE / §14.9.33 RESUME (both forms, the ResumeSignal/dispatch-result protocol) / §14.9.49 Format-3 USE
> (GR3c–g `__EcDispatch` tiers) / §14.9.18+§14.9.14 GOBACK/EXIT RAISING propagation (CALL-site pickup + the
> ProgramRegistry boundary default; ¶27403 SR2) / the §9.1.13.1 status→EC bridge (`__IoCheckEc`) / the EC-SIZE,
> EC-OVERFLOW, EC-PROGRAM, EC-ARGUMENT-FUNCTION raise points / §15.28–33 EXCEPTION-* functions / SET LAST
> EXCEPTION TO OFF / the full 0710–0719+0875–0879 diagnostics band with per-edition gating. The deep-dive
> (`COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md`) carries the as-built section. **1074 conformance (48 new EC
> facts) + 29 unit (13 new TurnState/catalog facts) ALL GREEN; FULL legacy guard ALL GREEN (353 MATCH + 11
> LEGACY_DIVERGENT, 0 regressions) on the shared-frontend grammar change.** Later-wave EC remnants (listed in
> the deep-dive's as-built tail): PERFORM…WHEN + >>PROPAGATE (2023), exception OBJECTS (OO wave), the
> EXCEPTION-FILE 2023 file-connector arg, national -N twins, GLOBAL-walkable F3.
> **RESUME AT: ② G7 per-edition correctness — Track-1 Phase-2 EditionValidator (removal/reserved-word gating
> per `docs/VERSION_CHANGE_REFERENCE.md` + the negative corpus) and the M2 2002 catalog (OO is the largest —
> reuse the legacy OO design per `project_oo_reuse_legacy`; then UDF prototypes, national/boolean,
> pointers/ALLOCATE, SHARING/LOCK) → M3 2014 (dynamic tables, TYPEDEF, JSON/XML) → M4 2023 deltas;
> ③ reserved-word tables (scout failed on content-filter — re-run; the `RF` find → 0900+ band); ④ G8 cut-over.**
> *(The Phase-1 banner below is history.)*
>
> **(superseded) STATE (DEVLOG 575, 2026-06-11 02:15): ⛔🎉 PHASE 1 COMPLETE — the COBOL-85 corpus drive (G5/G6) is CLOSED.
> Every golden-bearing NIST program is locked byte-exact: 318 = 93 NC + 29 ST + 32 RL + 40 IX + 23 IC + 69 SQ
> + 42 IF + 15 SM + 4 RW + 2 OBSQ. The final census (403 programs, `/e/tmp/nc-sweep/census-phase1-final.txt`):
> 357 GREEN, ZERO DIFF/CMPL_FAIL; residue is golden-less by NIST design (33 NO_GOLDEN, 6 chain-intermediate
> NO_REPORT, the 5-program cross-assembly subprogram-as-main RUNERR family + IC401M's no-golden companion, and
> IC113A = a CONFORMING infinite loop per §14.9.14 GR2). 1026 conformance + 16 unit; legacy guard 353 MATCH +
> 11 `LEGACY_DIVERGENT` (citations in `scripts/guard.sh`), 0 regressions. Landed in Phase 1 (DEVLOG 571–575,
> six parallel scouts + five implementation agents): SM/COPY (the NIST default copy library §7.2.3.4 GR3),
> the FULL intrinsic catalog (§15, `IntrinsicCatalog` + `CobolIntrinsics.*`/`CobolDate`), the Tier-C
> mixed-usage record codec (zoned digit images §13.18.60 GR4), LINAGE (§13.18.34 end-to-end + 3 re-baselines),
> the IC residue (CALL ON EXCEPTION gate, EXTERNAL FD `::EXT::` connectors, cross-program GLOBAL USE,
> cross-assembly CALL sibling probe, GR4 group-sender MOVE), PROGRAM-ID IS-noise-words (§11.4.2, guard-gated),
> and the REPORT WRITER subsystem (§13.24 `CobolReport` engine + USE BEFORE REPORTING).
> **RESUME AT (the SSOT §16 order): ① the EC exception-condition model (§11 / the conditions-exceptions
> deep-dive: >>TURN, the EC-* hierarchy, RAISE/RESUME, USE AFTER EXCEPTION CONDITION; the seams are placed,
> checking is OFF per §18.16); ② G7 per-edition correctness — Track-1 Phase-2 EditionValidator
> (removal/reserved-word gating per `docs/VERSION_CHANGE_REFERENCE.md` + the negative corpus) and the M2
> 2002 catalog (OO is the largest; then UDF prototypes, national/boolean, pointers/ALLOCATE, SHARING/LOCK)
> → M3 2014 (dynamic tables, TYPEDEF, JSON/XML) → M4 2023 deltas; ③ reserved-word tables (scout failed on
> content-filter — re-run; the `RF` find → 0900+ band); ④ G8 cut-over. Removed-from-residue notes: NC214M/
> NC303M/RW301M/302M/IF401M-403M/SM301M/401M etc. are NO_GOLDEN run-only; the sign-off/divergence protocol
> is established (verify → re-baseline → `LEGACY_DIVERGENT` + citation → lock).**
> *(Pre-Phase-1 banner below for history.)*
>
> **(superseded) STATE (DEVLOG 570, 2026-06-10 22:30): 273 NIST programs locked (92 NC + 25 ST + 32 RL + 40 IX + 18 IC +
> 64 SQ + 2 OBSQ — the authoritative `[InlineData]` census; earlier running tallies were undercounts);
> 827 conformance + 15 unit; FULL legacy guard ALL GREEN = 356 MATCH + 8 `LEGACY_DIVERGENT` (the owner-approved
> ISO re-baselines, DEVLOG 569/570: IX111A/IX210A/IX214A/IX215A/NC235A/NC236A/SQ207M = legacy HOLES;
> ST146A = an UNDEFINED-CHOICE — §14.9.30 GR18 leaves the record area undefined after an unsuccessful READ and
> COBOL.NET's canonical refinement is UNCHANGED, the spec's own pattern for the other I-O verbs; the list +
> per-program citations live in `scripts/guard.sh`). The fresh chain-aware census
> (`/e/tmp/nc-sweep/census-current.txt`, pre-567): 268/361 GREEN — ⚠ regenerate it: the sweep's binary
> false-green is fixed (`diff -a`) and chains have grown. Landed in the prior session
> (560–570): ① CHAIN-CONSUMER harness support —
> `tests/nist/chains.tsv` is the ONE chain topology (consumer→producers), `NistDifferentialTests.RunNist` runs
> predecessors in the consumer's own temp dir, the sweep gained CHAINERR + the `-s` nonempty-dll check → 22
> programs locked with zero compiler change (560); ② secondary-record SORT keys SR6e + SR6g diag 0874 + the
> ONE record-area accessor `FileModel.AreaRecord` (§13.4.2 — sequential READ, both KeyedIo sites, sort RETURN,
> WRITE-filename fallback) → ST111A/ST124A (561); ③ I-O-CONTROL SAME RECORD AREA → synthesized cross-file
> REDEFINES (§12.4.6.4 GR2; Formats 1/3 conformant no-ops; SR2–SR11 staged to EditionValidator) → ST131A/
> IX205A/IX206A (562); ④ variable-length records END-TO-END §13.18.43 — FD RECORD VARYING/m-TO-n binding,
> GR13a DEPENDING length on WRITE/REWRITE, GR15 read-back stores, '44' GR14/GR16/GR20 checks, 4-byte-LE length
> framing on varying connectors, GR8a ODO-minimum record sizes, OPEN I-O ReadWrite + logical-offset rewrites,
> the LAST-trivial-exit termination-tail fix → SQ212A/SQ228A/RL206A/RL211A/IX106A (563); ⑤ qualified
> RECORD/ALTERNATE KEY operands (§8.4.2.2 `FindQualified`) → IX215A compiles, all 39 PASS (564); ⑥ no 0-byte
> dll on failed emit + the Tier-C record fence → hostpolicy-phantom class retired, SQ203A locked (565);
> ⑦ XXXXX064 + ST144A re-baselined from the legacy (566); ⑧ the IX109A chain family — TF024 has TWO producers
> with different key universes; 8 status-test consumers locked behind IX109A (567); ⑨ FILE-NAME qualification
> §8.4.2.2 + ADVANCING mnemonic-name = 0 lines (568 — the legacy DROPS the AFTER-mnemonic write, a hole; SQ207M
> DIFF 4 = exactly those rows → sign-off queue).
> **RESUME AT — the census residue by family** (regenerate `census-current.txt` first — the driver below):
> ① NC105A last rows (`nc105a-brief.md`, untouched); ② SQ101M (DIFF 11, extra blank lines — print-control
> corner) + the LINAGE subsystem (§13.18.36 + LINAGE-COUNTER §8.4.3.14 — SQ201M/208M/209M/210M all loud on it);
> ③ the Tier-C COMP-record codec (COBOLNET_DESIGN §4.2/§8 — ST108A/127A/133A/134A, honestly loud now);
> ④ IC residue: cross-FILE dynamic CALL (IC109A/110A/117M/205A/210A `EC-PROGRAM-NOT-FOUND` — the pre-G8
> cross-assembly CALL item), IC113A timeout, IC222A (CALL ON EXCEPTION gated 2002+ yet used by a CCVS-85
> program — version-targeted investigation), IC233A/234A cross-program GLOBAL USE (designed §5.6 of the
> declaratives brief), IC207A/227A EXTERNAL FD, IC401M parse; ⑤ SM = COPY support (the whole suite);
> ⑥ ~~owner sign-off queue~~ DONE (569/570 — all eight re-baselined + locked, ST146A included after the
> definitive spec search; the legacy guard reports them as `LEGACY DIVERGENT`, list + citations in
> `scripts/guard.sh`);
> ⑦ reserved-word tables (scout FAILED on content-filter — re-run; the `RF` find → 0900+ band);
> ⑧ CALL follow-ups: GR3a subscripted-BY-REFERENCE capture, ContainsNextSentence arm for ON-phrase bodies,
> OMITTED args + header mode phrases (grammar). Then steps ④–⑦ (EC model → Phase-2 EditionValidator → 2002
> OO/UDF → 2014 JSON/XML → 2023 → G8). ⚠ Apply agent edits with the index-based python pattern; the Bash
> transport mangles backslash escapes — never inline them in heredocs.
> **SESSION ARTIFACTS (off-repo, E:\tmp):** scout briefs in `/e/tmp/verb-briefs/`; sweep infra + censuses in
> `/e/tmp/nc-sweep/` (driver: `xargs -P 14 -I {} bash sweep-one.sh {} < all-census-list2.txt` — sweep-one.sh
> is now chain-aware via `tests/nist/chains.tsv` and judges compiles by NON-EMPTY dll); a frozen CLI at commit
> e591da2 in `/e/tmp/cobolnet-frozen/` (pre-560; rebuild or use the live CLI for new scouting). Memory
> `project_greenfield_state.md` mirrors this map.**
>
> > **STATE (DEVLOG 552, 2026-06-10 17:30): 88/95 NC byte-match + 14 ST locked; 605 conformance + 15 unit; legacy
> guard ALL GREEN (re-proved on the RETURN grammar change). Landed since 545: ref-mod completion (548 — numeric
> items via NumericImagePlace + the parsed refModSpec form), null-table benign chains (549), RENAMES/REDEFINES
> layout closeout (550 — nested-class dissolution, no-THRU alias forward, StoreDisplay storage-form bridge,
> redefiner-excluded group width), ODO (551 — wave-2 agent; NC235A spec-pinned EXCEEDING the golden), SORT/MERGE
> (552 — wave-2 pipeline; 6 facts spec-pinned over a legacy GO-TO-loop hole; VERSION_CHANGE_REFERENCE Table 7 =
> the first 85→2002 rows). **RESUME AT: integrate the two PARKED wave-2 families from `/e/tmp/wave2-hold/`** —
> (1) KeyedIO (RELATIVE/INDEXED files; 19 anchored edits in `/e/tmp/verb-briefs/wave2result-keyedio.json`;
> targets RL/IX suites), then (2) CALL (11 STRUCTURAL edits in wave2result-call.json — multi-unit emission,
> instance program classes/__Activate, FieldEmitter instance fields; targets the IC suite; integrate ALONE and
> verify hard). Then: NC107A/108M (DECIMAL-POINT COMMA — brief at `/e/tmp/verb-briefs/wave2result-decimalPointBrief.txt`),
> NC105A's last 8 group-move rows, ST chain-consumer harness support, the ST105A/110A residuals (chain + an
> over-broad SR6 key check), then steps ④–⑦. Apply edits with the index-based python pattern (the Bash transport
> mangles backslash escapes — never inline `
` in heredoc scripts).**
>
> > **STATE (DEVLOG 545, 2026-06-10 13:20): 82/95 NC byte-match (locked), 564 conformance + 15 unit green. STEP ①
> ('85 verbs) COMPLETE: EVALUATE (542), the SIX-FAMILY VERB WAVE (543 — INSPECT, STRING/UNSTRING, INITIALIZE,
> ACCEPT, CORRESPONDING, ALTER-85-only + switches/SET-F3/switch-conditions, via 6 parallel agents over disjoint
> `StatementBinder.<Verb>.cs`/`CSharpEmitter.<Verb>.cs` partials), the ARITHMETIC WAVES (544 — §14.7.7 single
> evaluation/Snapshot, edited SIZE ERROR TryFormat, P-scale images, BFS sign-condition operands, figurative 88s),
> and the MOVE/PICTURE closeout (545 — CR/DB classification, group VALUE distribution, BLANK WHEN ZERO, AN→NE
> editing, P-scaled edited masks). Plus: JUSTIFIED, user-defined classes, zoned IS NUMERIC on character-backed
> views, level-66 RENAMES places, de-editing GR5, VARYING-AFTER augment order. Remaining 13 NC: collating
> NC215A/219A, DECIMAL-POINT COMMA NC107A/108M, ODO NC235A/247A, REDEFINES-tier NC252A (REDEF11 emission),
> NC105A/224A/401M louds, NC236A spec-pinned, NC214M/303M no-golden. **RESUME AT: the wave-2 scout briefs
> (CALL/inter-program, SORT/MERGE, RL/IX files, collating, ODO, DECIMAL-POINT COMMA) — workflow w7euzpx9q output at
> `C:\Users\brent\AppData\Local\Temp\claude\E--CobolSharp\de401450-b4ae-4dd4-acdd-ad95a132ce21\tasks\w7euzpx9q.output`;
> repeat the DEVLOG-543 pattern: 6 implementation agents over disjoint partial files, serial integration,
> per-cluster sweeps (`/e/tmp/nc-sweep/sweep-one.sh`, pipes .dat + COBOL_SWITCH_1), suites, locks, commit+push.**
> Then steps ④–⑦: EC model → Phase-2 EditionValidator + 85→2002 ledger → 2002 OO/UDF → 2014 JSON/XML → 2023 →
> G8 cut-over.** (Pre-verb-wave STATE below for history.)
>
> **(superseded) STATE (DEVLOG 530, session of 2026-06-10):** G1 ✅, G0 ✅, **G2 ✅, G3-core ✅, G4 ✅, G5 SEQUENTIAL FILE I/O ✅,
> G6-core ✅, REDEFINES Tier A+B ✅, ON SIZE ERROR ✅, PICTURE P ✅, de-sign ✅, **SIGN-clause inheritance ✅ (525),
> SET + index machinery + USAGE INDEX ✅ (526), sections-as-procedure-targets + qualified procedure-names +
> TIMES-once ✅ (527), PERFORM VARYING complete + ALL-to-group repeat + benign OOR subscripts via CobolTable.At ✅
> (528), CobolEdit numeric-edited receivers + DIVIDE REMAINDER + alphanumeric→numeric MOVE ✅ (530).**
> **54 NC programs byte-match the golden, all locked into `NistDifferentialTests`. 437 conformance + 15 unit
> green (DEVLOG 533–538: B2 Tier-B layout + GAP-1 subscripted views + NEXT SENTENCE + GAP-2 qualified subscripts
> + qualified/subscripted 88s + B3 + alphanumeric-edited pictures + Int128 division kernel + SEARCH F1+ALL +
> literal-vs-group comparisons + §14.9.12 GR6c subsidiary quotient + print-file plain WRITE). Legacy-oracle hole
> #4: legacy SEARCH VARYING-other-index falls through to DE-LETE — NC236A SPEC-PINNED, golden re-baselines at G8.
> **The 10-agent DIFF-diagnosis output (full per-row maps) is at
> `C:\Users\brent\AppData\Local\Temp\claude\E--CobolSharp\de401450-b4ae-4dd4-acdd-ad95a132ce21\tasks\wl4xosa5z.output`
> — remaining DIFFs:** NC172A/173A/175A/170A = expression-pipeline products beyond long (the FULL CobolInt/Int128
> carrier in NumericRenderer — multiplications and aligned sums, not the kernels); NC114M (16) edited corners;
> NC219A (16) collating; NC215A (34), NC104A (34), NC235A (44, +ODO), NC250A (50), NC124A (28). RUNERR remainder
> (~29): CORR (NC202A/207A/208A/209A/253A), INSPECT/STRING/UNSTRING/EVALUATE/INITIALIZE/ACCEPT verbs, ALTER,
> switch-status conditions (NC174A/254A — SPECIAL-NAMES SWITCH + COBOL_SWITCH_n env, see legacy SwitchRuntime),
> NC105A group-in-numeric. Then Track-1 Phase 2 EditionValidator.** **TRACK-1 PHASE 1 ✅ (531):** canonical
> `tests/version-matrix/constructs.json` (12 rows, 48 cells green) + `EditionHarness` + the FULL INV-1 continuity
> sweep (`scripts/version-continuity-sweep.sh`) — **342/342 85-compiling NIST programs clean at 2002/2014/2023,
> zero breaks**; first matrix catch fixed the placeholder JSON/XML grammar stubs to the real seam surface (legacy
> guard re-ran ALL GREEN). **Serial SEARCH F1 ✅ (532)** — label-loop emission; its cluster now gates on the
> wave-4 resolver gaps (subscripted Tier-B views needs B2 first) + NEXT SENTENCE. **NEXT: Phase 2
> EditionValidator** (removal/reserved-word gating + edition-NAMING diagnostics, the diagnose-correctly half). The docs corpus was GOAL-ALIGNED (529) to the
> owner's four restated goals (4-compilers-in-one + diagnostics co-equal; commercial/.NET 10/C#14-or-later with
> .NET 11-preview pre-authorized; legacy = reference/oracle ONLY; ICodeGenBackend dual-backend discipline).
> Greenfield-only; the shared front-end + legacy are untouched (re-run `scripts/guard-fast.sh` before touching
> `Cobol.Net.Frontend` or `CobolSharp.*`). **Default `--std` is COBOL-2023** (`--nist` ⇒ 85).
> ⚠ Legacy-oracle holes found this session (use SPEC-PINNED tests for these): same-section duplicate paragraph
> resolution (§8.4.2.2.1 r6), omitted-BY multi-AFTER VARYING (legacy binder crashes), DISPLAY trailing-trim (known).
>
> **VERSION-CORRECTNESS FRAMEWORK (the path to multi-edition support — NEW, DEVLOG 512–520):**
> - **The investigation rule (`feedback_version_targeted_semantics`):** when the legacy oracle ≠ ISO-2023, deep-
>   investigate whether it's a cross-EDITION change; if so gate by `DialectLevel`; only pin-to-spec for version-invariant
>   legacy bugs. (Investigated DEVLOG 517: DISPLAY-trim / comparison-de-sign / group-de-sign are all version-invariant
>   bugs → pinned to spec, all dialects.)
> - **`docs/VERSION_CHANGE_REFERENCE.md`** — 130-row checklist of every edition change the 2023 spec documents (Annex
>   E.2/E.3 = 2014→2023, Annex F archaic/obsolete, FLAG-02/14, inline NOTES). Scope limit: 2014→2023 complete; 85→2014
>   under-documented (harvest those from the grammar `is2002/2014/2023` gates + the legacy `DialectStrictnessChecks`).
> - **`docs/VERSION_TEST_MATRIX_DESIGN.md`** — test as N per-edition compilers: (construct × edition) matrix, expected
>   outcome computed from introducedIn/removedIn/behaviorVariants; 3 invariants (continuity / introduction-gating /
>   behavior-correctness); phased rollout. **Phase 0 DONE** (`VersionMatrixTests.cs`): introduction-gating proven both
>   ways (DELETE FILE rejected <2023, compiles @2023) + continuity (NC101A/211A/136A compile at later editions).
> - **KEY FINDING (DEVLOG 520):** the greenfield's edition-gating is almost entirely "ENABLE post-85 features"
>   (introduction, via 39 grammar predicates); **REMOVAL / reserved-word gating is essentially ABSENT** — an 85 program
>   using a 2023-removed construct or later-reserved word compiles unchanged at 2023 (continuity sweep: 89/89 NC compile
>   at both 85 and 2023, zero breaks). That ABSENCE is the Phase-2 worklist: a greenfield **`EditionValidator`** (port
>   the legacy `DialectConfig` + `DialectStrictnessChecks` validator pattern) that rejects-strict/warns-permissive per
>   the reference doc. (Owner policy: removed → error strict / warn permissive.)
> **CI fix (DEVLOG 512):** the recurring Linux-CI flake was a compiler data race — `BoundTreeValidator` held its
> `SemanticModel` in a STATIC field, clobbered under the suite's parallel in-process compilation → a spurious CBL1603
> on START-with-KEY programs → compile fail. Fixed by instance-izing the validator; `ConcurrentCompilationTests`
> reproduces+pins it (reproduce Linux-only flakes via WSL — see memory `reference_wsl_linux_repro`).
> **CI gap CLOSED (DEVLOG 515):** the greenfield `Cobol.Net.Tests.Conformance`/`Unit` now run in CI (both the Linux
> guard job and the Windows job) — they were legacy-only before; validated green on Linux. ⚠ **Don't interleave WSL
> (Linux) and Windows builds in the same tree without a clean — it contaminates `obj/bin` and a Windows incremental
> `dotnet test` can silently reuse a pre-edit Linux compiler (bit me in 516; CI is unaffected, fresh per-platform).**
>
> **THIS SESSION (498→504), the G5 milestone + the corpus-drive start:** (498) nested Tier-B REDEFINES backing path
> qualified (the NC101A blocker — `ReferenceResolver.BackingPath`); (499) **the sequential file I/O subsystem** —
> `Cobol.Net.Runtime/IO/` `SequentialFile` connector (OPEN INPUT/OUTPUT/EXTEND/I-O + OPTIONAL, CLOSE, WRITE plain +
> AFTER ADVANCING, READ, REWRITE; ISO §9.1.13 status + §14.9.30/35 read-state machine ported, string-image substrate,
> no byte State) + `CobolFile` facade; `DataBinder` binds FILE-CONTROL SELECT + FILE SECTION FD records (`FileModel`;
> multi-01 shared area = synthesized REDEFINES → existing tier machinery); `BoundOpen/Close/Write/Read/Rewrite` +
> binder + emitter (register at Main, CloseAll in finally). (500) **PICTURE P scaling** as ONE signed scale (removed
> the vestigial `NumProfile` P fields + the FractionScale clamp; `UnscaledAtScale` negative-scale) → **NC101A GREEN**.
> (501) mapped the NC series + locked the 7 green + `Pow10D` negative-scale. (502) **fixed 3 compiler HANGS** — the
> deeply-nested-group O(2^depth) emission blowup, now memoized (`FieldEmitter.PhysicalChildrenOf`). (503) **level-77 is
> a root** (independent item) — cleared 12 NC compile-errors. (504) **figurative constants in numeric / VALUE contexts**
> (ZERO in arithmetic → 0; `ALL ZEROS` VALUE init) — cleared 5 more. (505) **figurative ZERO in a level-88 VALUE**
> (numeric) — cleared NC250A's `ZERO00L`; **identified the systematic Tier-B `.BB` blocker** (RESUME AT #1).
>
> **✅ OWNER DIRECTIVE DELIVERED (DEVLOG 539–541): "numerics properly implemented for each language version, end
> to end" — ALL FOUR WAVES SHIPPED.** N1 Int128 carrier (D1+D2); N2 wide tier (PIC 9(19..31) → Int128); N3
> edition gates (digit/literal caps 18@85 / 31@2002+ / never>31, OPTIONS paragraph + ROUNDED MODE IS 2014+,
> §14.7 composite-31 — the 85-tightening-to-18 was REFUTED by CCVS-85/NC101A itself); N4 ARITHMETIC modes
> (NATIVE = documented Int128 engine; **STANDARD-DECIMAL implemented** — CobolDec SDIDI, decimal128/34-digit
> per-op semantics with all four INTERMEDIATE ROUNDING modes; STANDARD-BINARY documented-unsupported
> COBOLNET0806; plain STANDARD dropped@2023 COBOLNET0807). **474 conformance + 15 unit green; 64 matrix cells.**
> Numerics follow-ups (small, when touched next): COMP-5 binary-wrap store discipline; float↔fixed mixed
> expressions (D7); de-editing senders (§14.9.25 GR5); FLOAT-BINARY/-DECIMAL OPTIONS clauses. Resume the corpus
> waves below.
>
> **RESUME AT (refreshed after DEVLOG 530 — the 6-agent diagnosis workflow's wave plan is the worklist; its full
> output is in the session log of 2026-06-10):**
> - **(1) The remaining DIFF programs' single root causes:** NC203A/251A/170A/172A/173A/175A + NC171A = the
>   **deferred G3 Int128 intermediate-arithmetic** (18-digit dividends, π-precision quotients — build the CobolInt/
>   Int128 carrier per the numeric design); NC114M = **alphanumeric-EDITED insertion** (B/0,// in PIC X-edited —
>   extend CobolEdit/alphanumeric path); NC219A = the **PROGRAM COLLATING SEQUENCE subsystem** (ALPHABET … ALSO,
>   custom ordinals, HIGH/LOW-VALUE re-association — diagnosis wave 12 has the full inventory); NC124A (diff 382).
> - **(2) The RUNERR clusters (49 programs), by yield:** SEARCH F1+F2 (§14.9.37, ~8 programs, needs OCCURS KEY
>   capture for SEARCH ALL); subscripted/qualified condition-names (NC235A/250A); the remaining reference-resolver
>   gaps (diagnosis wave 4: subscripted Tier-B views GAP-1 — REQUIRES the B2 offset×OCCURS fixes first —, qualified
>   subscripts GAP-2, ref-mod-on-numeric GAP-3, RENAMES) → NC125A/132A/204M/206A/210A/224A/246A/252A; CORR +
>   NEXT SENTENCE (wave 10) → NC202A/207A/208A/209A/253A/174A/254A; INSPECT/STRING/UNSTRING/EVALUATE/INITIALIZE/
>   ACCEPT verbs (NC115A/216A/217A/218A/223A/225A/109M/214M/221A/122A); DECIMAL-POINT IS COMMA + BLANK WHEN ZERO
>   (wave 11) → NC107A/108M; ALTER (NC302M/303M); remaining Wave-0 bugs B2/B3/B4 (Tier-B accounting — mandatory
>   before GAP-1) + NC105A/103A/126A/215A singles.
> - **(3) Track 1 Phase 1** (unstarted this session): full INV-1 continuity sweep SM/IC/SQ/RL/IX/ST × editions;
>   `tests/version-matrix/constructs.json`; the `GetDiagnostics(source, edition)` harness; then Phase 2
>   EditionValidator (the diagnose-correctly half of the four-compilers mission — co-equal, owner-emphasized).
>
> **The original two-track framing (kept for context):**
>
> **TRACK 1 — VERSION-CORRECTNESS ROLLOUT (the systematic path to multi-edition support):**
> - **Phase 1 (next):** (a) the FULL INV-1 continuity sweep — every NIST-85 program × {2002,2014,2023} asserts "still
>   compiles" (the NC sweep already shows 89/89 at 2023; extend to SM/IC/SQ/RL/IX/ST and classify any break as a
>   reference-doc removal/reserved-word row vs a regression); (b) move the inline catalogue to the canonical
>   `tests/version-matrix/constructs.json` (doc §10 #5) and seed the ~12 highest-value reference-doc rows; (c) port the
>   greenfield **diagnostic-assertion harness** (`GetDiagnostics(source, edition)` / `AssertHasDiagnostic`).
> - **Phase 2:** build the greenfield **`EditionValidator`** + `ConstructDialectStatus` registry (port the legacy
>   `DialectConfig` two-axis model + `DialectStrictnessChecks` validator pattern) so removed/obsolete constructs and
>   later-reserved words are REJECTED (strict) / WARNED (permissive) per the reference doc — this is the currently-absent
>   half (DEVLOG 520 finding). First target: standalone `END` removal (the grammar already over-restricts it to 85-only
>   via `{is85()}?` though it should be valid through 2014 — a logged matrix red to fix); then the reserved-word and
>   removed-construct rows. Thread `DialectLevel` into bind/emit for the (future) behavior-variant rows.
> - The matrix IS the worklist: each red cell = a reference-doc-driven gating task. (`feedback_version_test_matrix`.)
>
> **TRACK 2 — FEATURE / NIST CORPUS DRIVE (`NistDifferentialTests` is the net; add each newly-green program's
> `[InlineData]`):** Snapshot after 520: **89/95 NC compile; 15 byte-match the golden** (513–514 found free wins + the
> PERFORM-THRU-range control fix; 516 completed the de-sign rule). **The closest single green is NC116A — ONE failure
> left (diff 10), GF-17 "PRECEDENCE OF SUBORDINATE SIGN CLAUSE" (ISO §13.18.45):** a subordinate group's `SIGN LEADING
> SEPARATE` must OVERRIDE an ancestor's `SIGN TRAILING` for the items below it (nearest-ancestor wins), and a SIGN
> SEPARATE item stores its sign in its own leading/trailing CHARACTER position so a REDEFINES over it reads the `+`/`-`.
> The fix = **inherit the nearest-ancestor SIGN clause** in `DataBinder` (it currently reads only the item's OWN
> `signClause`, line ~326): capture the group-level SIGN on every `DataItem`, then before the REDEFINES classification
> pass re-derive each signed-numeric item's SignKind (and the SEPARATE +1 image width, which feeds the redefines class
> width — mind the pass ORDERING). The separate-sign IMAGE storage already works (`S9(5) SIGN LEADING SEPARATE` →
> image `+91275`); only the inheritance is missing. **Other closest mismatches:** NC219A (diff 16 — the PROGRAM
> COLLATING SEQUENCE subsystem: `ALPHABET … ALSO … ALSO`, custom ordinals, redefining figurative LOW/HIGH-VALUE,
> applied to alphanumeric comparison — a sizable but single-root-cause feature that greens NC219A); NC114M (diff 34 —
> PICTURE insertion editing `B`/`0`/`/` in alphanumeric-/numeric-edited, plus the de-sign already done for MOVE-TEST-16);
> NC171A (diff 34 — DIVIDE INTO needs **Int128 intermediate arithmetic**: `Divide` scales the dividend by 10^9 and
> overflows `long` for 18-significant-digit operands → wrong result + spurious size error; this is the deferred G3
> Int128 work); NC124A (diff 850). Plus **67 RUNERR** (a runtime loud guard for an unimplemented verb — the INSPECT /
> EVALUATE / SEARCH / STRING / UNSTRING / INITIALIZE / ACCEPT backlog) and **6 CMPL_FAIL** (backend C# type-mismatch:
> NC104A/107A/108M/222A/247A/252A, e.g. NC252A's numeric Tier-B-view `string == long`). Recommended order: **(1) TARGET THE NEXT NC PROGRAM** —
> pick one (or two) compile-but-mismatch NC programs (run the compile/run-vs-golden sweep to pick the closest), implement
> the UNION of verbs/features they need, finish at a NEW GREEN program. NC211A's stack is DONE (506 level-88 over a
> REDEFINES view / group; 507 abbreviated combined relation conditions §8.8.4.12; 508 whole-group image over OCCURS
> §14.9; 509 signed→alphanumeric de-sign §14.9.25.4 GR6a; 510 `ALL "literal"` §8.3.3.6.4; 511 IS NUMERIC rule 2
> §8.8.4.4). **Still-open known gaps (each a likely next-program blocker):** the genuine Tier-C mixed-USAGE group
> (`byte[]`+`RedefCodec`, COBOLNET_DESIGN §4.2 — a COMP/binary leaf moved as a whole, currently loud); NEXT SENTENCE
> (loud); NC252A's numeric Tier-B-view 88 (`string == long` today) + nested-Tier-B-through-suppressed-parent
> [`REDEF11`/`RDF3`] + level-66 RENAMES. **(2) Then the high-frequency string/table VERBS** (the ~60 compile-but-mismatch
> programs each hit an
> unimplemented verb's loud guard): **INSPECT** (TALLYING/REPLACING/CONVERTING), **EVALUATE**, **SEARCH**/SET-for-index,
> **STRING**/**UNSTRING**, **INITIALIZE**, **ACCEPT** — each via `CobolStrings`/the bound tree + a differential test.
> ⚠ **A single verb greens NO program (each MISS needs several) — so TARGET A PROGRAM: pick one (or two) NC programs,
> implement the UNION of verbs they need, and finish at a NEW GREEN program** (a real integration milestone), not a
> stack of individually-tested verbs. **(3) Then G5 relative+indexed files** (`FileConnector` for SQ/RL/IX) + SORT/MERGE,
> and **CobolEdit** numeric-edited (needed once a FAIL path prints COMPUTED=). **Pattern that works:** map via the
> compile/run sweeps (compile-only first — a few NIST programs hang at *runtime*: use `timeout -k`), implement to the
> spec + design, differential-test, guard-green, commit, tick. **Known-latent (Int128/G3):** intermediate overflow
> beyond the long range / additive-scaling overflow / COMP-5 width bounds not size-error-checked; no-phrase EC-SIZE-fatal
> awaits the EC model. **OPTIONS clauses parsed but not yet applied:** ARITHMETIC mode, ENTRY-CONVENTION,
> FLOAT-BINARY/DECIMAL, INTERMEDIATE ROUNDING, INITIALIZE. Earlier history since 488:
> (489) deep-dive doc-sync to SSOT §14; (490) **whole-group MOVE/DISPLAY/compare over numeric-DISPLAY leaves** — a
> numeric-DISPLAY leaf in a whole-group-referenced group stores its CHARACTER IMAGE (`DataItem.StoreAsImage`, no
> byte[]; ISO §14.9 MOVE GR4 — line 28901: group move = char copy, no conversion); (491–493) **REDEFINES/RENAMES the
> 4-tier one-canonical-backing model** — classification pass (anchor closure SR7/11, tier cascade D>C>B>A, class-max
> width SR8, view suppression SR9) + **Tier A** (same-storage alias: numeric-over-numeric shares one `long`, views
> forward) + **Tier B** (string-canonical: `RedefViewPlace` `(offset,width)` window over ONE backing, numeric views
> ride `StoreAsImage`; root-level AND in-group via `FieldEmitter.PhysicalFields`); (494) ADD…TO…GIVING includes the
> TO operand. **`CobolNum.ParseDisplay`/`FormatDisplaySigned` confirmed implemented** (the design's "overpunch
> deferred" note is discharged). **(The RESUME AT that was here — (1) ROUNDED — is DONE; the current RESUME AT is in
> the STATE banner at the top: (1) ON SIZE ERROR → (2) G5 file I/O.)** **REDEFINES follow-ups (off NC101A's path, the
> design's later commits):** RENAMES no-THRU forward + THRU composition (`RenamesSpanPlace`, binder already binds
> level-66 + resolves FROM/THRU); Tier C (a class-scoped `byte[]` + `RedefCodec` for genuine mixed-USAGE COMP puns —
> currently loud-rejected, conformant interim); explicit Tier-D reject reasons. The decision-complete REDEFINES plan
> is in the DEVLOG-493 workflow output. Also pending: `CobolInt`/Int128 (>18 digits), INSPECT/STRING/UNSTRING
> (`CobolStrings`), `CobolEdit` numeric-edited. Known-latent: MOVE-signed→alphanumeric de-signing (§14.9.24 GR4d);
> EXIT SECTION / NEXT SENTENCE / ALTER / GO-TO-out-of-inline-PERFORM (loud). The (now-outdated) STATE blocks below are
> earlier snapshots, kept for architecture detail.
>
> ## ⛔ NON-NEGOTIABLE PROCESS RULES (owner-emphasized — repeatedly corrected this session; obey these BEFORE writing any code)
> These govern HOW you work and are the #1 way to go wrong if ignored. Durable copies:
> `feedback_use_the_spec`, `feedback_follow_design_docs_and_spec`, `feedback_spec_scopes_not_tests`.
> 1. **The ISO/IEC 1989:2023 spec (`specs/ISO_COBOL.md`) defines the correct behavior for EVERY case.** Whenever any
>    question of semantics / syntax / output / edge-case arises, READ the spec and CITE the § in code+DEVLOG — never
>    guess, never infer behavior from the legacy oracle (it is a regression net, NOT authority; it has known
>    non-conformances, e.g. the DISPLAY trailing-trim).
> 2. **Implement each feature FROM its subsystem deep-dive design doc** (`docs/COBOLNET_DESIGN.md` §0.5 lists all of
>    them — pipeline/data-model/redefines/control-flow/numeric/strings/files/interprogram/OO/conditions/intrinsics/
>    project-org). Read the doc, FOLLOW it; do NOT improvise an approach. The deep-dives are decision-complete SSOTs;
>    the SSOT `COBOLNET_DESIGN.md` wins for locked invariants (§1), cross-cutting (§14), settled decisions (§18),
>    build order (§16).
> 3. **Implement the COMPLETE feature to the spec + design — NEVER scope to what a test references.** The NIST /
>    differential corpus VERIFIES correctness; it does NOT bound what to build. Do not grep a test (e.g. "which
>    REDEFINES views does NC101A use") to decide scope — read the spec section + the deep-dive and implement the whole
>    feature (e.g. REDEFINES = all 4 tiers + RENAMES + every SR/GR rule). Legitimate STAGING is by spec/design
>    structure (e.g. the design's own G/commit order), never by test coverage.
> 4. **Keep the deep-dive docs CURRENT.** Whenever the SSOT (or a new decision/finding) supersedes a deep-dive's
>    design, UPDATE that deep-dive in the SAME change set — state the current design AND why the original was not
>    followed (cite the SSOT §/DEVLOG). A reader following a stale deep-dive would implement the rejected approach.
>
> **Standing operating rules (already in memory — still in force):** guard-green (`scripts/guard-fast.sh`, or the
> CobolNet differential+unit suites for greenfield work) before EVERY commit; a `tests/...` conformance/differential
> test + a DEVLOG entry ship in the SAME commit as each feature (**`DEVLOG.md` is DESCENDING — add the new entry at the
> TOP, just under the preamble's `Ordering: DESCENDING` note, with a real date+time header stamp
> `## Entry NNN — YYYY-MM-DD HH:MM TZ — Title` from `date "+%Y-%m-%d %H:%M %Z"`; the latest entry is always there,
> never hunt for it**); commit AND push every checkpoint (never ask "should
> I continue/push"); run autonomously and continue immediately when work is pending (don't stop to ask, don't
> ScheduleWakeup to wait); no byte `ProgramState` substrate (typed-native; a `byte[]` only at a genuine REDEFINES
> Tier-C / file boundary); adversarially-review non-trivial features. (Memories: `feedback_fully_autonomous_push`,
> `feedback_continue_dont_wait`, `feedback_conformance_tests_per_feature`, `feedback_devlog_per_commit`,
> `feedback_complete_dotnet_migration_no_byte`, `feedback_commercial_quality_north_star`.)
>
> **(superseded) STATE (DEVLOG 485):** G1 ✅, G0 ✅, **G2 FOUNDATION ✅, G3-core (partial) ✅, G4 ✅.** Differential harness LIVE,
> **116 G2/G3/G4-scope tests green.** G3-core landed: **figurative-constant operands** (MOVE/DISPLAY/comparison ZERO/
> SPACE…) + **compiler crash-proofing** (a group-in-numeric-context NPE → loud-failure; §1.4). **G4 = the PC
> dispatcher is DONE** (`__Dispatch(start,exit)` with paragraphs as pc cases; GO TO / GO TO DEPENDING / fall-through /
> out-of-line PERFORM [THRU/TIMES/UNTIL] as recursive bounded dispatch / EXIT PARAGRAPH; dead-code suppression;
> dispatcher internals `__`-prefixed so they never collide with COBOL fields). **The out-of-line-PERFORM
> double-execution known-latent is FIXED.** Empirical scope (the advisor's "pull real NC programs in"): **NC101A now
> compiles with only 3 unsupported statements** (OPEN/CLOSE/WRITE = file I/O, G5) + 106 whole-group MOVE (G6) + 2 group
> DISPLAY (G6) — G4 cleared all ~88 GO TO/EXIT. **RESUME AT → G5 (file I/O: `FileConnector`/`IRecordCodec`/FILE STATUS/
> OPEN-CLOSE-READ-WRITE-REWRITE/the file state machines, COBOLNET_DESIGN §8) + G6 (whole-group MOVE/compare/DISPLAY via
> the generated `AsImage()`/`FromImage()` per record struct, §14.4)** — together these unblock the FIRST full NC
> program through the differential harness. Known-latent now: MOVE-signed→alphanumeric de-signing (ISO §14.9.24 GR4d);
> >18-digit numerics (Int128 deferred); EXIT SECTION / NEXT SENTENCE / ALTER / GO-TO-out-of-inline-PERFORM (loud).
> Class conditions (IS NUMERIC/ALPHABETIC/-UPPER/-LOWER) DONE (DEVLOG 486, `CobolClass`); the one remaining small G2
> tail is ref-mod `(s:l)` (needs `CobolString.RefMod`/`SpliceInto`; the binder already detects the depth-0 SUB_COLON).
> The lines below are the earlier (DEVLOG 483) snapshot, kept for the architecture detail.
>
> **STATE (DEVLOG 483):** G1 ✅, G0 ✅. **G2 FOUNDATION COMPLETE (DEVLOG 475–483):** the parse-tree-walk emitter the
> DESIGN superseded is RETIRED and replaced by the real **bound semantic tree** (`Binding/Bound/` — `BoundProgram` +
> `StatementBinder` + bound nodes + `Bound*Error` loud-failure) rendered by a §17 §2.2-decomposed backend
> (`CodeGen/Emit/` — `EmissionContext` + `NumericRenderer` / `ConditionRenderer` / `OperandText` / `FieldEmitter` +
> a 239-line orchestrator; no god class). The **data model** is typed-native: groups→`record struct`,
> OCCURS→`T[]` + subscripts (ported SUB_* split), the **`Place`**/`ReferenceResolver` lvalue (unqualified +
> OF/IN-qualified), figurative VALUE, signed-DISPLAY over-punch/separate/binary-minus (`NumProfile.SignKind`),
> level-88 + sign conditions, INDEXED BY fields, loud-failure (§1.4 `NotImplemented`). **Verification backbone:** the
> **differential harness is LIVE** (`tests/Cobol.Net.Tests.Conformance/` — `LegacyCompiler` oracle vs
> `CobolNetCompiler`, compared on the NIST acceptance basis; **93 hand-picked G2-scope tests green**). ⚠ ISO finding
> (memory `feedback_use_the_spec`): legacy DISPLAY trims trailing spaces of an alphanumeric operand — **non-conforming
> per ISO §14.9.11.4 GR6**; COBOL.NET emits the full field (spec-pinned where the quirk shows). Numerics are still
> `long`-only (Int128/`CobolInt` deferred to the wave that first needs >18 digits per §18 #4). **RESUME AT →** (a)
> small G2 tails: **ref-mod** `(s:l)` (G2-1c — needs `CobolString.RefMod`/`SpliceInto` runtime; binder already detects
> the depth-0 `SUB_COLON`) + **class conditions** (IS NUMERIC/ALPHABETIC); then **G3** (the `CobolInt`/`Int128`
> value engine + `TryStore` + ROUNDED + ON SIZE ERROR + INSPECT/STRING/UNSTRING + `CobolEdit` numeric-edited) and
> **G4** (the PC dispatcher — replaces the sequential-paragraph stopgap; out-of-line PERFORM + fall-through is
> double-executing today) → **G5** drive the 364-NIST corpus via the differential harness. Both G3 and G4 now land
> against the bound tree, written once. Reuse ONLY the front-end + the clean typed substrates.
>
> ⚠ **VERIFICATION-CONFIDENCE (advisor, DEVLOG 483):** the 93 green tests are **hand-picked G2-scope fragments** —
> "the slice I chose works," NOT "G2 is correct." By construction the net dodges the known-wrong spots. The REAL
> verification is the 364-program NIST corpus via the differential harness. **Highest-leverage next move = G3-core +
> G4 so the FIRST real NC program runs through the harness** (the frontend already does NIST X-card preprocessing via
> `CompilerDriver.NistTestName`); the moment G4 lands, pull a handful of real NC programs in rather than waiting for
> all of G5 — that's what converts the net from "what I thought to test" into "what the corpus exercises." Do the
> small G2 tails (ref-mod, class conditions) opportunistically, not as the focus. **KNOWN LATENT (a real NC program
> will trip these):** (1) out-of-line PERFORM + fall-through **double-executes** (the sequential-paragraph stopgap —
> G4 fixes it); (2) **MOVE of a signed numeric → alphanumeric** moves the sign-aware image, but ISO §14.9.24 GR4d
> wants the **de-signed** digits (`OperandText.FieldAsString` is shared by DISPLAY [correct] and the MOVE source
> path [wrong for signed]); (3) numerics >18 digits (Int128 deferred, §18 #4).
>
>
>
> *(The historical byte-engine kickoff §0–§9 was removed 2026-06-09 — it is obsolete and superseded by the
> greenfield pivot; the live plan is THIS file + `docs/COBOLNET_DESIGN.md`. See DEVLOG 521.)*
