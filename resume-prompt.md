# COBOL.NET — Next-Session Kickoff Prompt

⛔🔥 **What this project is:** a blank-slate compiler translating COBOL → idiomatic typed-native C# compiled by
Roslyn — a COBOL record IS a .NET `record struct`, an elementary item IS a native field. **There is NO byte
`ProgramState` substrate — never reintroduce it, never "fall back" to the legacy byte engine** (`CobolSharp.Compiler`
is a differential oracle only, deleted at G8; a `byte[]` appears only at a genuine REDEFINES Tier-C / file boundary).

**Mission:** a commercial-quality, decades-sustainable, FULL ISO/IEC 1989:2023 COBOL compiler with correct support for
all prior editions (1985 / 2002 / 2014), validated as N per-edition compilers by the VERSION TEST MATRIX. Default
`--std` = 2023.

## Where to read (SSOT)
- **`docs/COBOLNET_DESIGN.md`** — the decision-complete design SSOT (locked invariants §1, data model, bound-tree
  pipeline [no lowered IR], native scaled-integer numerics, REDEFINES/files/OO/EC/intrinsics, §18 settled decisions,
  no-god-class structure). Deep-dives per subsystem are listed in its §0.5.
- **`docs/COBOLNET_REARCHITECTURE_PLAN.md`** — the go-forward roadmap (17-phase, resumable; **§0 RESUME PROTOCOL**;
  **§4.1 execution resequence**; §6 owner decisions D1–D12 ALL resolved). Obey its STATUS banner → the current
  `docs/rearchitecture/PHASE-NN-*.md` STATUS line → execute its numbered steps.
- **`specs/ISO_COBOL.md`** — the ISO spec is THE authority for ALL syntax AND behavior (submodule;
  `git submodule update --init`). See the NON-NEGOTIABLE rules below.
- **`DEVLOG.md`** — DESCENDING (newest entry first, under the preamble); add each entry at the TOP with a real
  `date "+%Y-%m-%d %H:%M %Z"` stamp. The full session history lives here (this banner stays lean).

## ⛔🔀 RESUME AT — PHASE-13 IN PROGRESS (M4 / COBOL-2023 deltas + EC remnants + behavior-row burn-down)

**⏭ PHASE-13 IS IN PROGRESS on branch `phase-13-m4-2023` (NOT merged to `main`). DO NOT re-create the branch or
re-run the audit — check it out and continue. Battery at HEAD: greenfield conformance **3696** · unit **313** ·
characterization **33** · legacy/NIST unaffected (no `.g4` change yet). **🎯 WAVE E COMPLETE (VCR 63/16/18/31/15 —
the EC-EXTERNAL-* raises landed 2026-07-19, DEVLOG 908) + Wave G CLASS A complete + REF-MOD-ZERO-LENGTH landed.**
**⚖ THE COMPREHENSIVE PLAN-VS-SPEC REVIEW RAN 2026-07-19 (DEVLOG 906): its findings ledger
`docs/rearchitecture/PHASE-13-plan-vs-spec-review.md` is LIVE — 28 confirmed (1 critical: the COBOLNET1573
collision, FIXED → 1576, DEVLOG 907) + 37 unverified (Wave I re-verifies) + 3 unrun spec-area finders (A3 §9/§12,
A4 §13, C4 Wave C — Wave I re-runs). Work its §6 disposition batches alongside the remaining waves.**
**REMAINING P13 (see the "REMAINING P13 WORK" list below): ① the GRAMMAR BATCH (shared `.g4` → ONE full legacy
guard; now + RW SUPPRESS per review C5) · ② the review FIX-NOW/spec-derive batches (ledger §6) · ③ Wave D
directives (now + the review's directive fixes) · ④ Wave I adversarial review (+ the 37 unverified + 3 unrun
finders) → phase-close → merge.** ⚠ Every acceptance/semantics change gates with the FULL Conformance project
(not a CorpusRunner-only filter — the `04c32a93`/`cf1fcaa2` lessons).**

> **⚡ SESSION 2026-07-18 LANDED (8 commits, each battery-gated; the tiered/batched/parallel execution model in
> `resume-prompt.md` §"EXECUTION MODEL" + [[feedback_execution_model_tiered_parallel]] was adopted + PROVEN this
> session):** STOP/GOBACK status→exit-code (VCR 75, `0fd9f6ac`) · **EC-BOUND-OVERFLOW** (§8.5.1.9.6 GR1, `e574af41`)
> · **EC-BOUND-REF-MOD raise** (§8.4.3.3.4 — the EC-BOUND surface is now CLOSED; `3eebcfd1`) · Wave G **8 pin-to-spec
> dispositions** in CONFORMANCE.md + '04' false-claim fix (`2265c33a`) · **Wave F USE FOR DEBUGGING + DEBUG-ITEM**
> (VCR 7.17 — built in a parallel worktree, 5-lens adversarial review caught 2 blockers [DEBUG-LINE causing-statement;
> a DebugRegisterPlace neutrality leak] fixed + re-verified before integration; `0efb0d88`). Plus: owner improvements
> **I1–I4 folded into the roadmap §7** + the seam-proof anchor reconciled to P16 M0. ⚠ Spec-first caught 2 citation
> drifts BOTH the audit AND the earlier scout had wrong: ref-mod is **§8.4.3.3.4** (not §8.4.2.3/.4); the '04'
> CONFORMANCE claim was FALSE (not emitted). ⚠ Diag band: main used none new; Wave F took **1571**; next free **1572**
> (Wave G CLASS A → 1570/1572).

### ▶ HOW TO RESUME P13 (read these, in order)
1. `git checkout phase-13-m4-2023` (the live P13 branch; `main` is at the P12 merge `e95dd92c`).
2. **`docs/rearchitecture/PHASE-13-audit.md` — THE WORKLIST.** The 6-auditor as-built audit (durable, spec-cited)
   classified all **71** P13 scope items: **18 DONE** (verify-and-flip only), **21 PARTIAL**, **31 MISSING**. Full
   evidence in `docs/rearchitecture/PHASE-13-scout-notes.json`. Work the MISSING + PARTIAL rows; the DONE rows just
   need a matrix/VCR flip. TRUST the audit over the (2026-07-07-authored, drift-prone) phase plan's step list.
3. `PHASE-13-m4-2023-ec-remnants-behavior-rows.md` STATUS line + the newest DEVLOG entries (886→880).
4. Confirm the branch battery is green at HEAD before touching code (greenfield conformance + unit; the FULL legacy
   guard for a `.g4`/preprocessor change — re-prove a file-I/O-suite flake by SOLO rerun).

### ▶ WHAT P13 HAS LANDED (branch commits)
- **Step 1 — the as-built audit** (`08acae23`): `PHASE-13-audit.md` + `-scout-notes.json`.
- **Wave B — EC-SIZE-TRUNCATION** (`a4cca7ae`, DEVLOG 886): verified ALREADY-RAISED on a ROUNDED MODE IS PROHIBITED
  inexact store (§14.7.5) + golden `tests/conformance/2023/ec_size_truncation_prohibited` (GreenfieldOnly). **EC-BOUND-
  OVERFLOW / EC-BOUND-REF-MOD are STAGED** (catalogued, not raised — needs the ambient-gate pipeline generalized:
  `ExceptionState.BoundOverflowChecking`/`BoundRefModChecking` flags set by `EcEmitter` from `TurnState`, `EcBinder`
  wrapping every table-grow/ref-mod statement in a `BoundEcChecked`, + runtime raise at `CobolDynTable`
  grow-past-capacity and `CobolString.RefMod` zero-length; LOUD-not-silently-wrong today).
- **Wave C — 8 of 10 COBOL-2023 grammar constructs** (`82be7e50` batch 1 + `a1704318` WRITE; DEVLOG 887–888), each
  spec-first from the persisted re-scout `docs/rearchitecture/PHASE-13-wave-c-scout.md` (which caught the
  SET-SIZE-OF-not-LENGTH-OF + EC-STORAGE-not-EC-BOUND + boolean-shift-precedence audit drifts), CLI-probed, golden +
  below-2023 negative each: GOBACK status (presence-only), USAGE PACKED-DECIMAL WITH NO SIGN, 63-char words, SET
  [SIZE OF] dyn-length, CONTINUE AFTER SECONDS + EC-CONTINUE, PERFORM UNTIL EXIT, boolean shift B-SHIFT-L/R/LC/RC
  (Table A.2 oracle byte-exact), WRITE BEFORE AND AFTER ADVANCING. Diag band 1565–1568 consumed (next free 1569).
  **⚠ LESSON: the generated ANTLR parser is SHARED with the frozen legacy compiler** — a grammar RESTRUCTURE needs
  the legacy binder fixed + full legacy guard (an additive change does not break it, but a restructure does).
- **Wave H (start) — `docs/CONFORMANCE.md`** (`6e9d1d12`, DEVLOG 889): the §4.2.16 conformance record + Annex A.3
  46-item disposition + the four documented-non-support facilities. **The SCREEN SECTION §4.2.7 non-support WARNING
  landed** (`e42ee330`, DEVLOG 891 — the silent-drop replaced by `warning COBOLNET1560` via `EditionContext.Warning`,
  establishing the §4.2.6 warning band). **REMAINING Wave H code half:** the recognize-and-name diagnostics for MCS
  (SEND/RECEIVE), COMMIT/ROLLBACK, VALIDATE (today a generic COBOL0001) — needs shared-parser keyword recognition
  (→ full legacy guard).
- **Wave I (partial) — adversarial review of the 8 Wave C constructs** (`ad6bf79a`, DEVLOG 890): a 17-agent
  find→verify workflow found **9 confirmed defects**; the 5 that reject/miscompute VALID input are FIXED
  (statusPhrase grammar's WITH-required/STATUS-binding/ERROR-NORMAL-optional bugs — a pre-existing STOP-status bug
  exposed by sharing the rule; CONTINUE fractional-negative EC miss; WRITE LINAGE double-advance; boolean-shift
  silent mis-grouping → loud COBOLNET1569; the NO SIGN §-citation). 2 accept-invalid gate-refinements documented as
  follow-ons (SET SIZE SR34 compile-time check; PERFORM UNTIL EXIT SR8 nested-under-VARYING). Diag band now 1569
  used, **next free 1570**. The Wave-I review still owes: the remaining Wave C constructs + waves D–H once landed.

### ▶ THE REMAINING P13 WORK (batch by GATE type; owner directive: optimize/combine, not at the risk of accuracy)
> **DONE 2026-07-18 (pushed through `82db4562`):** STOP/GOBACK exit-code (VCR 75) · EC-BOUND-OVERFLOW + EC-BOUND-REF-MOD
> raise (**EC-BOUND surface CLOSED**) · Wave F USE FOR DEBUGGING + DEBUG-ITEM (VCR 7.17) · Wave G's 8 pin-to-spec
> dispositions (VCR 22/24/78/33/37/14/17/20/49). The scout `docs/rearchitecture/PHASE-13-remaining-waves-scout.md`
> (D–H) + `PHASE-13-wave-c-scout.md` (grammar) are the decision-complete worklists. REMAINING:

- **① GRAMMAR BATCH (shared `.g4` ⇒ ONE full legacy guard — do together, batch by this gate):**
  - **SUPPRESS WHEN on ALTERNATE RECORD KEY** (§12.4.5.6 / §14.9.51 GR41 / §14.9.30 GR21c) — indexed-file
    per-alternate-key suppression across Write/Rewrite/ReadSequential/ReadRandom/START in `IndexedConnector`, no-DUPLICATES
    '22' + GR27 '02' lookahead bypass. Scout `PHASE-13-wave-c-scout.md` §C6-B (decision-complete). High blast radius, IX suite.
  - **PICTURE EDITING phrase** (§13.18.40, VCR 62) — EDITING character-1 IS/FOR NEGATIVE/POSITIVE → `PictureAnalyzer.Analyze`
    + a sign-sensitive `CobolEdit`. ⚠ the remaining-waves scout caught: renders via **Table 9 (not 8)**, intro **E.3.3 item 19**,
    **EDITING is a NEW 2023 reserved word** (cobolWord funnel + COBOLNET0901, like XOR/COMMIT). NO PICMODE change.
  - **PERFORM Format 3 (exception-checking PERFORM … WHEN)** (§14.9.28.2) — staged large: 2 new tokens (FINALLY, LOCATION)
    + a new statement + deep EC integration (GR14/GR17/GR20). PERFORM UNTIL EXIT already landed. Scout §C5.
  - **RW SUPPRESS statement (§14.9.45; review finding C5 — CONFIRMED, found independently by 2 finders):** the only
    RW verb with NO grammar rule (hard parse error today, violating the RW §5 staged-loud convention). Small: rule
    `SUPPRESS PRINTING?` (PRINTING is OPTIONAL — non-underlined per the format notation; the finder's own
    "mandatory" claim was verifier-corrected), SR1 USE-BEFORE-REPORTING-only bind check, a per-presentation
    suppression flag on the ReportWriter engine (the `ReportWriter.cs:53` hook is pre-staged) per GR3a-e. Golden +
    CONFORMANCE.md row. Batch here (shared `.g4`). Also cover the three phrase-level no-grammar holes the same
    review sentence names (COLUMN LEFT/CENTER/RIGHT §13.18.14 · PAGE COLS · LAST CONTROL HEADING) — staged loud at
    minimum.
  - **Wave H code half** — recognize-and-name §4.2.6 non-support for MCS (SEND/RECEIVE), COMMIT/ROLLBACK, VALIDATE (today a
    generic COBOL0001) via shared-parser keyword recognition + the COBOLNET156x-band WARNING (the SCREEN §4.2.7 warning is
    the pattern, already landed). Batch here (shared parser). Scout Wave H — **but READ its ⛔ MECHANISM CORRECTION banner
    (2026-07-19) FIRST:** the scout's IDENTIFIER-led `{facilityWord(...)}? mcsFacilityStatement` seam was implemented and
    **empirically poisons the boolean-factor prediction DFA** (broke `COMPUTE R = B-NOT A.` at all editions — DEVLOG-621
    class), so it was reverted to green. **Corrected singular mechanism (matches RAISE):** real lexer tokens
    `RECEIVE`/`SEND`/`VALIDATE` + `cobol-words.json` nameSlot rows (regen `CobolWords.g4`) + a **keyword-led**
    `{facilityWord(...)}? (RECEIVE|SEND) (~DOT)*` alternative (DFA-safe — distinct-token lead). COMMIT/ROLLBACK stay the
    diagnostic-layer 0901→1571 refinement (no grammar rule, unaffected). This shares the same token+cobolWord machinery as
    PICTURE-EDITING (`EDITING`) and PERFORM-Fmt3 (`FINALLY`/`LOCATION`) — do all three tokens under the ONE legacy guard.
    DEVLOG 903.
- **② GREENFIELD-ONLY (no legacy guard; sequential-in-one-tree, ONE comprehensive gate per batch — [[feedback_execution_model_tiered_parallel]]):**
  - **Wave G CLASS A** — partially landed:
    - ✅ **VCR 35 + 86 DONE 2026-07-18 (`1123a77f`, DEVLOG 898)** — figurative-ZERO edited-zero `DialectLevel` branch (ValueInitializer)
      + non-zero-numeric-literal intro gate COBOLNET0900 (DataBinder; `value-numeric-literal-numeric-edited-2023` row). SR6 literal-zero
      exemption honored; one blast-radius hit (`func_expr_arg.cob` `PIC Z9 VALUE 34`) spec-verified non-conforming @2014 + fixed at source.
    - ⏳ **VCR 34 DEFERRED (scout drift)** — scout's `length == pic.Length` is wrong (SR4/SR5 = "shall not exceed", `<=`); national-class
      mismatch already caught by COBOLNET0898. Genuine surface = an ≥2023 length-`<=` check for an alphanumeric edited-image literal
      (COBOLNET1570). Needs precise spec derivation. (VCR 36 numeric auto-supply fell out of 86; its VALUE-EDITING FLAG-14 twin is Wave D/H.)
    - ✅ **VCR 21 DONE 2026-07-18 (DEVLOG 899)** — I-O status '04' on a record-sequential READ whose physical record is outside
      min/max (§14.9.35 GR14). Runtime-only `SequentialConnector.Read` (`shortLong` flag; fixed `n<RecordWidth`, varying
      `n<VaryMin||n>VaryMax`; line-seq excluded). Golden `2002/io_status_04` (self-contained, GreenfieldOnly).
    - ✅ **VCR 27 DONE 2026-07-18 (DEVLOG 900)** — MERGE-in-SORT/MERGE-procedure prohibition (§14.9.24 / E.2 item 20; **COBOLNET1572**
      at ≥2023). Bind-time procedure-range cross-pass `VersionConformancePass.GateMergeInSortMergeProc` (pc-alignment verified:
      `BoundProgram.Paragraphs[i]` pc == the ProcedureTable pc `SortRange` uses). Matrix row `merge-in-sort-merge-proc-removed-2023`
      (removedIn 2023). CLI-probed all quadrants (no false positive on a standalone MERGE).
    - ✅ **VCR 68/69 DONE 2026-07-18 (DEVLOG 901)** — EXCEPTION-FILE(connector)/EXCEPTION-FILE-N(connector) arg form (§15.28.4 r2 /
      §15.29.4 r2). New binder path `IntrinsicBinder.BindExceptionFileArg` (file-name → FileModel, carried on `BoundIntrinsicCall.FileArg`;
      renderer passes `FileKeyExpr`) + runtime `EcFunctions.File(key)`/`FileN(key)` + `FileRegistry.ExceptionFile` + `FileConnector.EverAccessed`.
      Intro gate COBOLNET0900 (2 constructs rows); non-file arg → COBOLNET1574. Golden `2023/exception_file_arg` (GreenfieldOnly).
      **⟹ Wave G CLASS A COMPLETE** except VCR 34 (deferred).
    - ✅ **VCR 34 DONE 2026-07-19 (DEVLOG 902)** — the ≥2023 over-size check (§13.18.63 SR4/SR5) for an alphanumeric edited-image
      literal VALUE on a numeric-edited item → **COBOLNET1570** (permissive-aware, removedIn-2023 posture). Scout's `==` was drift
      (SR4/SR5 = "shall not exceed", `>`); national-class already COBOLNET0898. **⟹ WAVE G CLASS A FULLY COMPLETE — no deferred residue.**
    - ⚠ **Gate EVERY acceptance/semantics change with the FULL Conformance project** (differential + matrix + permissive suites), never a
      CorpusRunner-only filter (the `04c32a93` + `cf1fcaa2` lessons: the filter missed the differential-test regressions AND the
      matrix's `RemovedConstruct_CompilesPermissive` permissive-severity leg).
  - **Wave E — EXTERNAL cluster + EC-EXTERNAL-\*** (§13.18.22 [NOT §13.18.27 — scout drift], VCR 15/16/18/31/63) — Scout Wave E.
    - ✅ **VCR 63 + 16 + the §13.6.2 GR7 init fix DONE 2026-07-19 (DEVLOG 904)** — (63) the **STRONGLY-TYPED** external
      type declaration intro gate (COBOLNET0900; STRONG+EXTERNAL is the 2023 add per E.3 item 10 — a WEAK `TYPEDEF IS
      EXTERNAL` stays COBOL-2002, §13.18.58.3 SR3; the STRONG-less first cut regressed the 2002 `typedef_external`
      golden, caught by the FULL Conformance gate); (16) the EXTERNAL-CONSTANT-RECORD-requires-strong-TYPE dialect flip
      (COBOLNET1549 gated ≥2023); (init) external CONSTANT RECORDs now seed their run-unit cell with the VALUE-composed
      image (`GroupImageCodec.ImageInitOf`) per §13.6.2 GR7, while plain externals stay blank (§13.18.63 GR4a). Battery:
      Conformance **3685** · Unit 311 · char 33 all green.
    - ✅ **VCR 18 + 31 DONE 2026-07-19 (DEVLOG 905)** — the two ≥2023 cross-unit external-file consistency checks
      (FILE STATUS = COBOLNET1573; RELATIVE KEY = COBOLNET1575) via ONE post-bind cross-unit pass
      `BinderDriver.CheckExternalFileConsistency` (dialect-gated ≥2023, binder-reads-edition doctrine; `ExternalItemIdentity`
      = the external-root qualified path). 6 goldens (neg/pos/2014-continuity). (VCR 63/16 consumed no new 15xx — 0900/1549 reused.)
    - ✅ **VCR 15 DONE 2026-07-19 (DEVLOG 908) — WAVE E COMPLETE.** The EC-EXTERNAL-FORMAT-CONFLICT /
      -DATA-MISMATCH / -FILE-MISMATCH raises at the §14.9.4.4 GR3e CALL raise point: activation-entry
      `ExternalStore.Describe` descriptor registration + cross-describer compare on the run-unit `ExternalTable`
      (kind-bucketed record vs file-connector); the §14.8.4.1 both-elements gate = CALL-site per-statement TURN mask
      (`ExceptionState.ExternalCheckMask`, latched by `ProgramTable.CallProgram` into `ActivatorExternalMask`)
      AND the activated unit's before-Environment-division mask — bitwise, each condition pairs independently.
      Raise = `CobolCallException` → the CALL catch (widened to EC-EXTERNAL-*) → ON EXCEPTION per GR3h #1.
      EC-EXTERNAL-IMP has no raise site by definition (no implementor-defined checks — conforming). 3 goldens +
      1 negative; zero-scaffolding proven by characterization. ⚠ 2 scout drifts caught: the before-Env-division
      rule is ACTIVATED-side only; the scout's golden lacked the ON EXCEPTION a fatal raise needs (GR3h #2 →
      §14.6.13.1.3 termination). Named residues: UDF/INVOKE activation boundaries carry site-mask 0 (no raise
      there; the INVOKE GR7d leg needs the OO activation seam).
  - ~~**REF-MOD-ZERO-LENGTH directive** (§7.3.23)~~ **✅ DONE 2026-07-18 (`420eb720`, DEVLOG 897)** — the `>>REF-MOD-ZERO-LENGTH
    {ON|OFF}` directive that allows a zero-length ref-mod result (§8.4.3.3.4 item 5c). Landed the `RefModZeroLengthDirectiveProcessor`
    (mirror of TurnDirectiveProcessor, intro-gated via the ONE ConstructRegistry) + `RefModZeroLengthState` line-fold + init-only
    `RefModPlace.AllowZeroLength` + the `ref-mod-zero-length-2023` constructs.json row (COBOLNET0900) + **COBOLNET1573** malformed-operand.
    ⚠ The full legacy guard surfaced + FIXED **two pre-existing latent GreenfieldOnly misses** (`ec_bound_ref_mod` from `3eebcfd1`,
    `ec_bound_overflow` from `e574af41` — landed without their legacy-exclusions; [[feedback_legacy_suite_on_shared_corpus]]).
- **②b THE REVIEW BATCHES (`docs/rearchitecture/PHASE-13-plan-vs-spec-review.md` §6 — the ledger's disposition
  lines are the live to-do state):** FIX NOW docs/citations (CONFORMANCE.md '04' + four-facility rewording ·
  wrong-§ corrections [§13.6.2→§11.9.10.4 GR7 · 'E.3 item 10'→E.2 · §14.9.35→§14.9.30 GR14 · the A8 citation trio
  · MCS 'COMMUNICATION SECTION' wording] · stale notes [data-model EC-BOUND-OVERFLOW · DebugItem · CC-processor
  comment] · 1550-1552 earmark cleanup · 0849 double-claim reconciliation) + the SPEC-DERIVE fixes (VCR 16
  strong-TYPE strength leg §13.16.3 SR13 ¶2 · VCR 86 below-2023 SR6 re-derivation · GOBACK RAISING main-program
  normal-termination · ref-mod negative-length −1-sentinel collision · COBOLNET1570 scope § · EXCEPTION-FILE r2a
  EverAccessed/SD legs — serial greenfield, full-Conformance gated). DONE so far: the 1573→1576 collision fix +
  catalog-completeness drift guards (DEVLOG 907) · banner refresh (this edit).
- **③ Wave D — directives** (preprocessor; VERIFY whether it touches the shared `.g4` → guard accordingly): `>>COBOL-WORDS`
  (per-unit ReservedWordSet mutation), `>>PUSH`/`>>POP` (directive-state stack), `>>DISPLAY` (compile-log line), `>>FLAG-14`
  (wire the GR4 a–l twins §7.3.15.4 to the behavior rows — the FLAG-14 twins named in Wave G land here), `>>FLAG-02`-obsolete. Scout Wave D.
  **+ from the review (confirmed):** `>>DEFINE AS PARAMETER` + the §7.3.6/§7.3.7 compile-time expression evaluator
  (LIFT the existing `DataBinder.Constants.cs` §7.3.6 evaluator, don't rebuild — verifier note) + the SR2
  no-OVERRIDE check · `>>SOURCE FORMAT` mid-file switching (§7.3.24.3 GR1) · CC-directives-inside-COPY (§7.2.1
  Step 1/2 ordering) · the CC/SOURCE-FORMAT intro gate at --std 85 · FLAG-85/FLAG-NATIVE-ARITHMETIC E.2-item-21
  handling · >>CALL-CONVENTION / >>LEAP-SECOND dispositions (unverified U1 — verify first).
- **④ Wave I — adversarial review** (Workflow 5-lens find→verify over the P13 landed constructs — the Wave F review proved the
  pattern; prior phases found 6–7 real defects) → fix confirmed defects → EXIT CRITERIA check → phase-close doc sweep → merge to `main`.
- **Diagnostic band (THE one next-free claim — do not duplicate it elsewhere; review finding C7):** 1565–1569
  Wave C · 1570 VCR 34 · 1571 Wave F · 1572 VCR 27 · **1573 external-file-status-consistency (VCR 18) — its
  ONLY meaning since the DEVLOG-907 collision fix** · 1574 EXCEPTION-FILE not-a-file (VCR 68/69) · 1575
  external-relative-key-consistency (VCR 31) · **1576 REF-MOD-ZERO-LENGTH malformed-operand (renumbered from the
  colliding bare-literal 1573)**. **NEXT FREE = 1577** — allocate ONLY after BOTH scans agree:
  `grep -rho 'COBOLNET15[0-9][0-9]' src | sort -u` AND the `DiagnosticCatalog` descriptor list (the
  `EveryEmittedCode_IsACatalogDescriptor` drift test now forces the frontend channel through the catalog;
  1550–1552 remain unallocated mid-band holes — usable, but note the catalog comment). Introduction gates
  COBOLNET0900; new-reserved-word user-word gates COBOLNET0901; obsolete 0903; §4.2.6 non-support WARNING band 1560.

**Then: PHASE-14** (matrix closure + in-repo greenfield guard `scripts/guard.ps1`/`greenfield-guard` + one-time
legacy-equivalence proof) → **PHASE-15** (G8 legacy retirement — the three cuts DELETE `CobolSharp.Compiler`/the
differential oracle + §4.2.16 conformance docs + runtime namespace flip + D10 SUBSCRIPT-mode removal "CUT 2.5") →
**PHASE-16** (CIL/Cecil backend `--backend cil` + the backend-neutrality equivalence harness).

### ▶ THE PROVEN WAVE PATTERN (used through P10–P13; keep it)
persisted anchor re-scout/audit (Workflow, parallel spec-first agents → a repo doc, TRUSTED over the drift-prone
phase plan — it caught the P11 CONCATENATE + P12 IEEE-fidelity inversions BEFORE coding) → supervised implement
(one feature at a time, CLI-probe each, golden + below-edition negative in the SAME commit) → batch by gate type
(grammar constructs share ONE full legacy guard) → full battery + solo-rerun flake adjudication → verdict-gated
commit + push → adversarial find→verify review at phase end (catches what the re-scout can't — it found 6 real
P12 defects incl. a shipped spec misreading) → phase-close doc sweep → merge to `main`.

**PHASE-12 IS COMPLETE (2026-07-17, DEVLOG 880–884 — 6 battery-gated commits `fb17f98f`→`9afde9f3`+close, merged
to `main` `e95dd92c`).** Read `docs/COBOLNET_REARCHITECTURE_PLAN.md` banner + §4 index for the phase pointer.

**PHASE-12 — M3 (COBOL-2014) surface deltas — DONE.** The waves (each spec-first from the persisted anchor
re-scout `PHASE-12-scout-notes.md`, line-reviewed, full-battery + full-legacy-guard-gated): **DYNAMIC LENGTH
elementary items** (§8.5.1.10 / §13.18.19 — a variable-length min-0 `PIC X`/`N` native string; `CobolDynString`
+ `StorageForm.DynamicString`; the 1561-1563 SR band) · the **IEEE float USAGE family** (§13.18.60.4 GR14-18 —
`FLOAT-BINARY-32`→`float`, `FLOAT-BINARY-64`→`double` LIVE; `FLOAT-BINARY-128`/`FLOAT-DECIMAL-16/34`
processor-dependent NON-support per Annex A.3 17/19, COBOLNET1564) · pointer §8.5.2.7→§8.5.2.15 comment fixes ·
the **`>>PROPAGATE`** introduction gate (§7.3.21, provisional 2002, COBOLNET0883; `PropagateDirectiveProcessor`
on the `>>TURN` pattern) · the **`TYPE TO`** re-anchor (the restricted `USAGE POINTER [TO type]`, row
`usage-pointer-to-type-2014` pending). **⚠ THE P12 SCOPE CHANGE / re-scout catch: the IEEE-754 fidelity claim
was INVERTED** — the plan called `double`-backed binary128 "a conforming implementor choice per GR13"; GR14-18
PIN the standard usages to ISO/IEC 60559:2020, so they are refused loudly where .NET has no type, never silently
mis-backed. **Deferred residues (documented):** the external-float `E` PICTURE (§13.18.40.4 GR13b, staged 0899),
the FUNCTION-POINTER runtime + restricted PROGRAM-POINTER + `ADDRESS OF` spellings (staged 0899), the
`>>PROPAGATE` runtime semantics + its §7.3.21.3 SR1 placement rule (→ P13), and the DYNAMIC LENGTH national
FUNCTION LENGTH / BYTE-LENGTH runtime paths (staged loud). **The Step-12 adversarial review found 6 real defects,
0 refuted** — incl. a spec MISREADING shipped in wave 2 (MOVE SPACE → length 0; §8.3.3.6.4 GR3b makes it length
1) — all fixed + locked (DEVLOG 884). Final battery **3582 conformance · 311 unit · 33 characterization · legacy
1196+646 · NIST 353 MATCH**. The verified anchors + the IEEE-fidelity derivation are retained in
`docs/rearchitecture/PHASE-12-scout-notes.md`. ⚠ Guard-flake note (recurred 2× in P12): a 35x+n guard-fast
verdict naming a file-I/O suite (SQ/IC/IX/ST/OB) under JOBS=32 is the environmental flake class — re-prove by
SOLO rerun before treating as real.

**PHASE-11 — deferred-intrinsics backlog → zero + the Tier-C decision — DONE.** Every ISO §15 intrinsic is
now LIVE (`IntrinsicBind.Deferred` = **zero**; every row `Runtime`/`Fold`/`Unsupported`). The waves (each
spec-first from the persisted anchor re-scout, line-reviewed, full-battery-gated): BOOLEAN-OF-INTEGER/
INTEGER-OF-BOOLEAN §15.13/§15.45 (the Boolean result-category channel made real — `ResultCategory` →
`PicCategory.Boolean` + four widened string-channel seams) · the Y2K windowing trio §15.23/25/100 on ONE
`YearToYyyy` core + SECONDS-PAST-MIDNIGHT §15.80 on the `RunUnit.Clock` seam · the TEST validator quartet
§15.90/91/93/94 (date verdict chains + NUMVAL positional scanners + the ONE `BindNumvalCFamily` bind with the
shared `Anycase` flag) · BYTE-LENGTH §15.14 (compile-time fold over the new `DataItem.ByteWidth`, pinned
implementor byte widths) + the SMALLEST-ALGEBRAIC golden · the A.4.9 locale module → documented non-support
(COBOLNET1518 — the five functions + the LOCALE keyword variants) · the Tier-C decision (rejection
single-sourced: the class reject was already ONE `RedefinesClass.Classify`; Step C collapsed the ~12 scattered
classless-group emit guards onto the ONE `TierCIsland.Reason`, predicates preserved). **⚠ THE P11 SCOPE
CHANGE: CONCATENATE is NOT an ISO function at any edition** (the re-scout found zero spec occurrences; CONCAT
§15.18 is new-in-2023) — the "window [2002,2023)" plan was audit drift; the row is DELETED (a reference draws
COBOLNET1501); CONCATENATE-as-a-vendor-extension is a separate future call. **Step D (the confined `byte[]`
codec) is DEFERRED** as a scheduled increment (DESIGN-data-model §2.3 — its design needs re-basing; no NIST
program requires it). All five exit criteria met. Final battery **3521 conformance · 301 unit · 33
characterization · legacy 1196+636 · NIST 353 MATCH**. Per-function verified anchors + hand-derived golden
values + the Tier-C guard-site inventory are retained in `docs/rearchitecture/PHASE-11-scout-notes.md`
(a durable spec-to-code reference). ⚠ Guard-flake note (still in force): a 35x+n guard-fast verdict naming
a file-I/O suite (SQ/IC/IX/ST) under JOBS=32 is the environmental flake class (DEVLOG 870/872/873/875/877) —
re-prove by SOLO rerun before treating as real.

**PHASE-10 IS COMPLETE (2026-07-17, DEVLOG 854–870 — 14 battery-gated commits `7436a1ef`→`a0fd3f68`+close, CI
green on each; the P10 doc's STATUS banner carries the exit-criteria confirmation + the NAMED forward-residue
ledger).** Every M2 non-OO track is landed on the greenfield substrate or staged loud by name. The waves, in
landing order (each spec-first, line-reviewed, full-battery-gated): NATIONAL intrinsics (the ONE `Repertoire`
translator) · EC `-N` twins + CHAR-NATIONAL · `&`-CONCAT (compile-time fold, `ConcatFolder`) · ALLOCATE
INITIALIZED (§14.9.3 GR7 through the ONE InitializeBinder) · UDF category-RETURNING (§8.4.3.2.4 GR1) ·
CONSTANT entries + CONSTANT RECORD (§13.10/§13.18.15 — the compile-time substitution table
`DataBinder.Constants.cs`; CONSTANT+AS joined the §8.9 interval machinery, AS nameSlot-ONLY [FU-1]) ·
PROGRAM-POINTER + qualified/subscripted ADDRESS OF (the `ProgramPointer` outermost-identity carrier on the ONE
`ProgramTable`; `ResolveForAddressOf` on the ONE cell-offset formula) · FILE-LOCK (§9.1.16 record locks on
EVERY organization [sequential = ordinal identity]; RETRY on the mutating verbs; DELETE FILE **62** = the
sharing conflict §9.1.13.9 item 2 — the audit had mislabeled it; the ONE `LockGoverned` neutrality hinge) ·
UDF BY VALUE + PER-EVALUATION activation (§14.2.3 GR10 detached cells on the ONE CALL ABI;
`BoundUdfEvaluated` IIFE windows — C# short-circuit IS the COBOL rule; 1509 narrowed to 3 named shapes) ·
RECURSIVE-WS (§13.5.4 GR1 static WS for `Recursive&&!Initial` incl. every FUNCTION, `__ResetStatics` at
registration/CANCEL; program LOCAL-STORAGE was silently UNBOUND — now binds per §13.6.4 GR1) ·
ARITH-STANDARD (the six SDIDI residuals: `CobolDec.Pow`, decimal128 range ECs, float→SDIDI operands, MEAN;
the ARITHMETIC gates RE-TIMED to 2002 with the full 0900/0903/0807 lifecycle; the ENTRY-CONVENTION funnel
bug fixed) · TYPEDEF residue (SAME AS §13.18.49 on the ONE `CloneItem`; EXTERNAL type §13.18.22 LIVE [the
1534 stage's § citation was wrong]; strong groups compare element-wise §8.8.4.2.12; ExpandType was DROPPING
template root clauses — fixed via the shared `CopyEntryDescription`) · RW-2002 (PRESENT WHEN §13.18.41 +
VARYING §13.18.64 — whose SR1 pulled multiple/relative COLUMN in; the absent-entry line-collapse model;
1559) · ALPHABET-NATIONAL (FOR NATIONAL + UCS-4/UTF-8/UTF-16 on the ONE Collating subsystem; the sparse
`NationalCollatingTable`; **UCS-4 ≡ NATIVE proven via §8.5.1.4** — surrogate pairs are two character
positions by spec, so the divergence is unreachable; GR7 Table 6 coded-set-only rejections).
**⚠ THE P10 RECURRING LESSON: the Step-1 audit itself drifted in ~5 of 6 re-checked claims** — wrong §
citations (EXTERNAL-type "GR5", RW "§13.18.44"), already-landed "gaps" (strong relations, ARITH consumption,
per-occurrence UDF activation), a mislabeled status code (62 vs 71) — EVERY wave must re-scout its anchors
spec-first before implementing. Battery at close: **3467 conformance · 292 unit · 33 characterization
byte-identical · legacy 1196+636 · NIST 353 MATCH.**
**NEXT: PHASE-11** (`docs/rearchitecture/PHASE-11-intrinsics-backlog-tierc-codec.md`, STATUS: NOT STARTED —
read its §0/steps; it can run parallel to P12 per its header): every remaining `IntrinsicBind.Deferred`
catalog row → Runtime (BYTE-LENGTH unblocks the staged `CONSTANT AS BYTE-LENGTH OF`; the EC-N 2023 arg
forms live there too) + the Tier-C REDEFINES confined-byte codec. The P10 forward residues (per-shape 1510,
OPTIONAL formals, recursive-WS stages, OO class-unit BASED, INITIALIZE-over-pointers, line-seq 06/09/71,
keyed GR10a FPI, cross-run-unit sharing, multiple-LINE, narrowed-1509 shapes, signed-leaf strong ordering,
MAX/MIN-under-collating) are ledgered IN the P10 doc's STATUS banner; B-SHIFT/BX + STANDARD-BINARY are
2023/2014 items for P12/P13.

**Working discipline in force:** BATCHED cycles (multiple sub-steps per battery run) with PIPELINING (batch N's
conformance runs on the prebuilt binaries in the background while batch N+1's edits are authored in the
worktree; the INDEX is staged at battery launch so the committed tree is EXACTLY the tested tree); commits are
verdict-gated as separate actions, never `&&`-chained. P7 pickups still queued: SymbolTableBuilder-owned storage;
route `ReferenceResolver.ResolveUnqualified` + the StatementBinder condition lookup through the `SymbolTable`; the
image-fact caching (the O(subtree) perf work).

**⚡ EXECUTION MODEL — tiered testing + batching + parallelism (owner-directed 2026-07-18; use it every session).**
The comprehensive battery is ~12–15 min/run, so gate by BLAST RADIUS, not reflexively:
- **Tiered testing.** *Wave-local gate* (~2–3 min, run per wave): a FRESH `CobolSharp.sln` build, then only the
  targeted subset — characterization (emit byte-identity), `CorpusRunnerTests` FILTERED to the wave's subsystem +
  its new golden, the wave's targeted unit tests, and a CLI probe of the feature. *Comprehensive gate* (~15–18 min):
  the FULL greenfield conformance + FULL legacy guard (NIST) — run once per BATCH of waves and **mandatory before
  any merge to `main` and before the P15 legacy cut**. GUARDRAILS that FORCE the comprehensive gate regardless:
  (1) any SHARED-`.g4`/preprocessor/lexer change → full legacy guard (the parser is shared with the frozen legacy
  compiler); (2) any bound-tree SHAPE change or shared-infra refactor → full conformance; (3) any config-divergent
  code (`#if DEBUG`/`[Conditional]`) → a `-c Release` leg (CI tests Release, local is Debug — [[feedback_guard_fast_not_ci_complete]]);
  (4) enabling a shared-corpus golden → the legacy suite too ([[feedback_legacy_suite_on_shared_corpus]]).
- **Batching.** Land 3–4 INDEPENDENT greenfield waves sequentially in ONE tree (fast; no merge conflict), gate each
  wave-local, run ONE comprehensive battery for the whole batch, then commit each wave SEPARATELY off the green tree
  (verdict-gated) and push the batch.
- **Parallelism (worktrees + agents; owner directs max parallelism where beneficial).** RELIABLY parallel: the
  scouts/re-scouts, adversarial reviews, and MECHANICAL/DISJOINT bulk (the P15 migration cuts, P16 bound-node
  neutralization sites) — fan out via `Workflow` (`isolation:'worktree'`), then INTEGRATE + comprehensive-gate.
  DISJOINT feature waves (e.g. Wave F USE-DEBUGGING is independent of the EC waves) may run in parallel worktrees.
  KEEP SERIAL / SUPERVISED: waves sharing the EC/gate hot files (`ExceptionState`/`ExceptionCatalog`/`EcEmitter`/
  `EcBinder`/`constructs.json`/`VersionConformancePass`) — parallelizing them just manufactures merge conflicts + a
  diagnostic-code-counter collision (two waves both grabbing the next free `COBOLNET15xx`); and the grammar batch
  (shared `.g4` → one legacy guard); and the strict phase chain **13→14→15** (can't close the matrix before features
  land; can't delete the oracle before P14 proves equivalence — prove-then-delete). Before any parallel fan-out:
  PARTITION by disjoint file-sets and PRE-ALLOCATE a diagnostic-code range per worker. Spec-first FEATURE
  implementation stays supervised (the [[feedback_use_the_spec]]/complete-not-scoped rules are exacting — subagents
  drift); delegate only well-scoped mechanical/disjoint work, and REVIEW+integrate every agent diff against the spec.

**Execution order (§4.1, TOOLING-FIRST):**
- **A ✅** — the source-generated exhaustive bound-tree visitor (PHASE-07 Step 6): the 7 generated `IBound*Visitor` +
  `Accept` + `BoundStatementTree.StatementChildren`; every completeness-critical dispatch converted; every switch
  ISO-§-classified.
- **B ✅** — the Real Binder (P6): `Binding/BinderDriver.Bind` → immutable `Binding/Model/BoundCompilation`; the
  middle-end is the declared `BindPipeline.GroupTail` manifest (ProcedureBinding → UsageCollectionPass →
  StorageFormPass → the `VersionConformancePass` terminal pass), ONE validated DAG; 14 CodeGen-read binder
  collections sealed `IReadOnly`; the ONE scope-aware `SymbolTable` (`TryResolve`/`TryResolveIndex`/`IndexCellOf`,
  explicit `Scope`, per binder). the P6-era `IOoBindHost` OO seam is
  DELETED (P9 Step 4 — `Oo/OoDriver` owns the bind bodies).
- **C ✅** — PHASE 05 the unified data model: the `StoreAsImage` FLAG is gone — `Storage` (the ONE `StorageForm`) is
  computed once by the group-tail `StorageFormPass` from collected facts, the name kept as the read-only projection;
  `RecordLayout` is the ONE phase-free width/offset authority (§13.18.44.3 SR8 enforced, COBOLNET1539); the data
  model in `Binding/Model/` (`PlaceDecorator` base; `StrongTypeModel` + `PictureAnalyzer`; sentinels → `DataItem.
  Pending`); the tier verdict single-sourced through `RedefinesClass.Classify`; ONE `UsageInheritancePass`.
- **D ✅** — PHASE-07 complete (Steps 1–12: both god classes dissolved; structural `Place`; FUNCTION-arg grammar +
  the `IntrinsicRenderer` static-channel deletion).
- **E ✅** — edition-gate remediation complete: the ~19 inline gates folded into the two-arm
  `VersionConformancePass` (20 registry rows, all in the matrix); `GateId` deleted; claims reconciled onto the
  §1.1 gating-exception ledger.
- **F ◐ (NOW)** — PHASE 08–16: **PHASE-08 ✅** (runtime reorg onto `RunUnit`) · **PHASE-09 ✅** (M2 OO
  rearchitect + completion; see the RESUME banner); next PHASE-10 (M2 residual catalog) → 11–14 (feature
  waves + matrix closure) → 15 (G8 legacy cut) → 16 (CIL backend).

**Done:** Phases 00–09 (migration safety net · frontend rename · `Cobol.Net.Editions` leaf + diagnostic registry ·
version-conformance pipeline [the two-arm `VersionConformancePass` is the SOLE edition gate] · frontend consolidation
· the unified data model · the Real Binder · the visitor/god-class/`Place`/FUNCTION-arg decomposition · the
runtime-library reorg onto `RunUnit` · the M2 OO rearchitect+completion).
D10 SUBSCRIPT-mode
removal is RELOCATED → PHASE 15 §"CUT 2.5" (D10.4 mostly pre-empted by P7 Step 12). 
**Battery (keep green + pushed at EVERY commit):** 3275 conformance · 281 unit · 33 characterization (32 snapshots
byte-exact + the RuntimeApi ratchet) · FULL legacy guard NIST 353 MATCH. ⚠ Build `CobolSharp.sln` before `dotnet
test --no-build` — a stale test-bin compiler DLL hides greenfield regressions.

---

## ⛔ NON-NEGOTIABLE PROCESS RULES (owner-emphasized — obey BEFORE writing any code)
Durable copies: `feedback_use_the_spec`, `feedback_follow_design_docs_and_spec`, `feedback_spec_scopes_not_tests`.
1. **The ISO/IEC 1989:2023 spec (`specs/ISO_COBOL.md`) defines the correct behavior for EVERY case.** Whenever any
   question of semantics / syntax / output / edge-case arises, READ the spec — the SPECIFIC governing §/GR, not the
   nearest general sentence — and CITE it in code + DEVLOG. Never guess, never infer behavior from the legacy oracle or
   a green corpus (they are regression nets with known non-conformances, NOT authority). **A "faithful, byte-neutral"
   refactor proves NO-REGRESSION, never CORRECTNESS — validate the logic against the §.**
2. **Implement each feature FROM its subsystem deep-dive design doc** (`docs/COBOLNET_DESIGN.md` §0.5 lists them). Read
   the doc, FOLLOW it; do NOT improvise. The deep-dives are decision-complete SSOTs; `COBOLNET_DESIGN.md` wins for locked
   invariants (§1), cross-cutting (§14), settled decisions (§18), build order (§16).
3. **Implement the COMPLETE feature to the spec + design — NEVER scope to what a test references.** The corpus VERIFIES;
   it does NOT bound what to build. Legitimate STAGING is by spec/design structure, never by test coverage.
4. **Keep the deep-dive docs CURRENT.** When the SSOT (or a new decision/finding) supersedes a deep-dive, UPDATE it in
   the SAME change set to state the current design. Docs describe the CURRENT compiler — the historical narrative lives
   only in `DEVLOG.md`.
5. **Never work around or hack — REDESIGN/rearchitect for the best possible design; leverage the tooling.** Any new tree
   traversal uses the ONE generated/shared visitor (bound tree: `IBound*Visitor`/`StatementChildren`) or the ANTLR
   generated visitor/listener (CST), never a fresh bespoke `switch`. After finishing work, retroactively audit related
   prior work for similar shortcuts. ([[feedback_no_workarounds_root_cause]], [[project_path_a_leverage_tooling]],
   [[feedback_singular_pattern]].)

**Standing operating rules (in memory — still in force):** guard-green (the CobolNet differential+unit suites for
greenfield work) before EVERY commit; a `tests/…` conformance test + a DEVLOG entry ship in the SAME commit as each
feature; commit AND push every checkpoint (never ask "should I continue/push"); run autonomously and continue
immediately when work is pending (don't stop to ask, don't ScheduleWakeup to wait); adversarially-review non-trivial
features. (Memories: `feedback_fully_autonomous_push`, `feedback_continue_dont_wait`,
`feedback_conformance_tests_per_feature`, `feedback_devlog_per_commit`, `feedback_complete_dotnet_migration_no_byte`,
`feedback_commercial_quality_north_star`.)
